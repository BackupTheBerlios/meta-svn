//	Copyright (c) 2005, 2006 Christian Staudenmeyer
//
//	Permission is hereby granted, free of charge, to any person obtaining
//	a copy of this software and associated documentation files (the
//	"Software"), to deal in the Software without restriction, including
//	without limitation the rights to use, copy, modify, merge, publish,
//	distribute, sublicense, and/or sell copies of the Software, and to
//	permit persons to whom the Software is furnished to do so, subject to
//	the following conditions:
//	
//	The above copyright notice and this permission notice shall be
//	included in all copies or substantial portions of the Software.
//
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
//	BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
//	ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//	SOFTWARE.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Meta;
using Meta.Test;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Net.Sockets;
using ICSharpCode.SharpZipLib.Zip;
using System.Web;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;


namespace Meta
{
	public class Code
	{
		public static readonly Map ParameterName="parameterName";
		public static readonly Map Root = "root";
		public static readonly Map Search = "search";
		public static readonly Map Lookup = "lookup";
		public static readonly Map Current="current";
		public static readonly Map Scope="scope";
		public static readonly Map Literal="literal";
		public static readonly Map Function="function";
		public static readonly Map Call="call";
		public static readonly Map Callable="callable";
		public static readonly Map Argument="argument";
		public static readonly Map Select="select";
		public static readonly Map Program="program";
		public static readonly Map Key="key";
		public static readonly Map Value="value";
	}
	public class DotNet
	{
		public static readonly Map Add="add";
		public static readonly Map Remove="remove";
		public static readonly Map Get="get";
		public static readonly Map Set="set";
	}
	public class Numbers
	{
		public static readonly Map Negative="negative";
		public static readonly Map Denominator="denominator";
	}
	public abstract class MetaException : ApplicationException
	{
		private string message;
		private Extent extent;
		// this should use Positions instead of extents in the long run
		private List<Extent> invocationList = new List<Extent>();


		public MetaException(string message, Extent extent)
		{
			this.message = message;
			this.extent = extent;
		}
		public List<Extent> InvocationList
		{
			get
			{
				return invocationList;
			}
		}
		public override string ToString()
		{
			// TODO: messy, horrible
			string message = Message;
			if (invocationList.Count != 0)
			{
				message += "\n\nStack trace:";
			}
			foreach (Extent extent in invocationList)
			{
				message += "\n" + GetExtentText(extent);
			}
			return message;
		}
		public static string GetExtentText(Extent extent)
		{
			string text;
			if (extent != null)
			{
				text = extent.FileName + ", line ";
				text += extent.Start.Line + ", column " + extent.Start.Column;
			}
			else
			{
				text = "Unknown location";
			}
			return text;
		}
		public override string Message
		{
			get
			{
				return GetExtentText(extent) + ": " + message;
			}
		}
		public Extent Extent
		{
			get
			{
				return extent;
			}
		}
		public static int CountLeaves(Map map)
		{
			int count = 0;
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				if (pair.Value == null)
				{
					count++;
				}
				else if (pair.Value.IsNumber)
				{
					count++;
				}
				else
				{
					count += CountLeaves(pair.Value);
				}
			}
			return count;
		}
	}
	public class SyntaxException:MetaException
	{
		public SyntaxException(string message, Parser parser)
			: base(message, new Extent(parser.Line, parser.Column,parser.Line,parser.Column,parser.File))
		{
		}
	}
	public class ExecutionException : MetaException
	{
		private Map context;
		public ExecutionException(string message, Extent extent,Map context):base(message,extent)
		{
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException
	{
		public KeyDoesNotExist(Map key, Extent extent, Map map)
			:base("Key does not exist: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
	public class KeyNotFound : ExecutionException
	{
		public KeyNotFound(Map key, Extent extent, Map map)
			:base("Key not found: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
	public abstract class Expression
	{
		public abstract Map Evaluate(Map context);
	}
	public class Call : Expression
	{
		private Map callable;
		public Map argument;
		public Call(Map code)
		{
			this.callable = code[Code.Callable];
			this.argument = code[Code.Argument];
		}
		public override Map Evaluate(Map current)
		//public override Map EvaluateImplementation(Map current,Map arg)
		{
			try
			{
				return callable.GetExpression().Evaluate(current).Call(argument.GetExpression().Evaluate(current));
			}
			// TODO: refactor
			catch (MetaException e)
			{
				e.InvocationList.Add(callable.Extent);
				throw e;
			}
			catch (Exception e)
			{
				throw new ExecutionException(e.ToString(), callable.Extent,current);
			}
		}
	}
	public class Program : Expression
	{
		private List<Map> statements;
		public Program(Map code)
		{
			statements = code.Array;
		}
		public override Map Evaluate(Map parent)
		{
			Map current = new StrategyMap(new TemporaryPosition(parent));
			foreach (Map statement in statements)
			{
				statement.GetStatement().Assign(ref current);
			}
			return current;
		}
	}
	public class Literal : Expression
	{
		private Map literal;
		public Literal(Map code)
		{
			this.literal = code;
		}
		public override Map Evaluate(Map context)
		{
			Map result=literal.Copy();
			result.Scope = new TemporaryPosition(context);
			return result;
		}
	}

	public abstract class Subselect
	{
		public abstract Map EvaluateImplementation(Map context, Map executionContext);
		// why is executionContext a ref parameter, i dont get it
		public abstract void Assign(ref Map context, Map value, ref Map executionContext);
	}
	public class Current:Subselect
	{
		public override void Assign(ref Map context, Map value,ref  Map executionContext)
		{
			value.Scope = context.Scope;
			executionContext = value;
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			return context;
		}
	}
	public class CallSubselect : Subselect
	{
		private Call call;
		public CallSubselect(Map code)
		{
			this.call = new Call(code);
		}
		public override void Assign(ref Map context, Map value, ref Map executionContext)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			return call.Evaluate(context);
		}
	}
	public class Root : Subselect
	{
		public override void Assign(ref Map selected, Map value, ref Map executionContext)
		{
			throw new Exception("Cannot assign to argument.");
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			return Gac.gac;
		}
	}
	public class Lookup:Subselect
	{
		public override void Assign(ref Map selected, Map value,ref Map executionContext)
		{
			selected[keyExpression.GetExpression().Evaluate(executionContext)]=value;
		}
		private Map keyExpression;
		public Lookup(Map keyExpression)
		{
			this.keyExpression = keyExpression;
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			Map key=keyExpression.GetExpression().Evaluate(executionContext);
			if (!context.ContainsKey(key))
			{
				throw new KeyDoesNotExist(key, keyExpression.Extent, context);
			}
			return context[key];
		}
	}
	public class Search:Subselect
	{
		public override void Assign(ref Map selected, Map value,ref Map executionContext)
		{
			Map evaluatedKey = key.GetExpression().Evaluate(executionContext);
			Map scope = executionContext;
			while (scope != null && !scope.ContainsKey(evaluatedKey))
			{
				scope = scope.Scope.Get();
			}
			if (scope == null)
			{
				throw new KeyNotFound(evaluatedKey, key.Extent, executionContext);
			}
			else
			{
				scope[evaluatedKey] = value;
			}
		}
		private Map key;
		public Search(Map keyExpression)
		{
			this.key = keyExpression;
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			Map evaluatedKey = key.GetExpression().Evaluate(executionContext);
			Map scope = context;
			while (scope != null && !scope.ContainsKey(evaluatedKey))
			{
				scope = scope.Scope.Get();
			}
			if (scope == null)
			{
				throw new KeyNotFound(evaluatedKey, key.Extent, context);
			}
			else
			{
				return scope[evaluatedKey];
			}
		}
	}
	public class Select : Expression
	{
		private List<Map> keys;
		public Select(Map code)
		{
			this.keys = code.Array;
		}
		public override Map Evaluate(Map context)
		{
			Map selected=context;
			foreach (Map key in keys)
			{
				selected=key.GetSubselect().EvaluateImplementation(selected, context);
			}
			return selected;
		}
	}
	public class Statement
	{
		List<Map> keys;
		public Map value;
		public Statement(Map code)
		{
			this.keys = code[Code.Key].Array;
			this.value = code[Code.Value];
		}
		public void Assign(ref Map context)
		{
			Map selected = context;
			for (int i = 0; i + 1 < keys.Count; i++)
			{
				selected = keys[i].GetSubselect().EvaluateImplementation(selected, context);
			}
			keys[keys.Count - 1].GetSubselect().Assign(ref selected, value.GetExpression().Evaluate(context), ref context);
		}
	}
	public class Library
	{
		// TODO: maybe this stuff could be reduced?
		// simplified
		// some parameters change
		public static Map Product(Map arg)
		{
			Number result = 1;
			foreach (Map number in arg.Array)
			{
				result *= number.GetNumber();
			}
			return result;
		}
		public static Map Foreach(Map arg)
		{
			Map result=new StrategyMap();
			foreach(KeyValuePair<Map,Map> entry in arg[1])
			{
				result.Append(arg.Call(new StrategyMap("key",entry.Key,"value",entry.Value)));
			}
			return result;
		}
		public static Map BinaryOr(Map arg)
		{
		    int binaryOr = 0;
		    foreach (Map map in arg.Array)
		    {
	            binaryOr |= map.GetNumber().GetInt32();
		    }
		    return binaryOr;
		}
		private static Random random = new Random();
		public static Map Random(Map arg)
		{
			return random.Next(1,arg.GetNumber().GetInt32());
		}
		public static Map FindFirst(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			Map array=arg["array"];
			Map value = arg["value"];
			for (int i = 1; i<=array.ArrayCount ; i++)
			{
				for (int k = 1;value[k].Equals(array[i+k-1]);k++)
				{
					if (k == value.ArrayCount)
					{
						return i;
					}

				}
			}
			return 0;
		}
		public static Map Reverse(Map arg)
		{
			List<Map> list=arg.Array;
			list.Reverse();
			return new StrategyMap(list);
		}
		public static Map Range(Map arg)
		{
			int end=arg.GetNumber().GetInt32();
			Map result = new StrategyMap();
			for (int i = 1; i < end; i++)
			{
				result.Append(i);
			}
			return result;
		}
		public static Map If(Map arg)
		{
			Map result;
			if (arg[1].GetBoolean())
			{
				result=arg["then"].Call(Map.Empty);
			}
			else if (arg.ContainsKey("else"))
			{
				result = arg["else"].Call(Map.Empty);
			}
			else
			{
				result = Map.Empty;
			}
			return result;
		}
		public static Map Intersect(Map arg)
		{
			Dictionary<Map, object> keys = new Dictionary<Map, object>();
			foreach (Map map in arg.Array)
			{
				foreach (KeyValuePair<Map, Map> entry in map)
				{
					keys[entry.Key] = null;
				}
			}
			Dictionary<Map,Map> result = new Dictionary<Map,Map>();
			foreach (KeyValuePair<Map, object> entry in keys)
			{
				foreach(Map map in arg.Array)
				{
					if (map.ContainsKey(entry.Key))
					{
						result[entry.Key] = map[entry.Key];
					}
					else
					{
						result.Remove(entry.Key);
						break;
					}
				}
			}
			return new StrategyMap(new DictionaryStrategy(result));
		}
		public static Map Complement(Map arg)
		{
		    Dictionary<Map, Map> found = new Dictionary<Map, Map>();
		    foreach (Map map in arg.Array)
		    {
		        foreach (KeyValuePair<Map, Map> entry in map)
		        {
		            if (found.ContainsKey(entry.Key))
		            {
		                found[entry.Key] = null;
		            }
		            else
		            {
		                found[entry.Key] = entry.Value;
		            }
		        }
		    }
		    Map result = new StrategyMap();
		    foreach (KeyValuePair<Map, Map> entry in found)
		    {
		        if (entry.Value != null)
		        {
					result[entry.Key] = entry.Value;
		        }
		    }
			return result;
		}
		public static Map Slice(Map arg)
		{
			Map array=arg["array"];
			int start;
			if (arg.ContainsKey("start"))
			{
				start = arg["start"].GetNumber().GetInt32();
			}
			else
			{
				start = 1;
			}
			int end;
			if (arg.ContainsKey("end"))
			{
				end = arg["end"].GetNumber().GetInt32();
			}
			else
			{
				end = array.ArrayCount;
			}
			Map result = new StrategyMap(new ListStrategy());
			for (int i = start; i <= end; i++)
			{
				result.Append(array[i]);
			}
			return result;
		}
		public static Map Replace(Map arg)
		{
			throw new ApplicationException("Method not implemented.");
		}
		public static Map Equal(Map arg)
		{
			bool equal = true;
			if (arg.ArrayCount > 3)
			{
			}
			if(arg.ArrayCount>1)
			{
				for(int i=1;arg.ContainsKey(i+1);i++)
				{
					if (!arg[i].Equals(arg[i + 1]))
					{
						equal = false;
						break;
					}
				}
			}
			return equal;
		}
		public static Map Not(Map arg)
		{
			return !arg.GetBoolean();
		}
		public static Map Or(Map arg)
		{
			bool or=false;
			foreach (Map map in arg.Array)
			{
				//or = true;
				if (map.GetBoolean())
				{
					or = true;
					break;
				}
			}
			return or;
		}
		public static Map Apply(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			foreach (Map map in arg["array"].Array)
			{
				result.Append(arg["function"].Call(map));
			}
			return result;
		}
		public static Map Find(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			string text=arg["array"].GetString();
			string value=arg["value"].GetString();
			for (int i = 0; ; i++)
			{
				i = text.IndexOf(value, i);
				if (i == -1)
				{
					break;
				}
				else
				{
					result.Append(i+1);
				}
			}
			return result;
		}
		public static Map Filter(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			foreach (Map map in arg["array"].Array)
			{
				if (arg["function"].Call(map).GetBoolean())
				{
					result.Append(map);
				}
			}
			return result;
		}
		public static Map StringReplace(Map arg)
		{
			return arg["string"].GetString().Replace(arg["old"].GetString(), arg["new"].GetString());
		}
		public static Map UrlDecode(Map arg)
		{
			string[] aSplit;
			string sOutput;
			string sConvert = arg.GetString();

			//' convert all pluses to spaces
			sOutput = sConvert.Replace("+", " ");
			//sOutput = REPLACE(sConvert, "+", " ")

			//' next convert %hexdigits to the character
			aSplit = sOutput.Split('%');

			//If IsArray(aSplit) Then
			sOutput = aSplit[0];
			for(int i=1;i<aSplit.Length;i++)
			{
				sOutput = sOutput + (char)Convert.ToInt32(aSplit[i].Substring(0, 2), 16)+aSplit[i].Substring(2);
			}
			return sOutput;
		}
		public static Map Try(Map arg)
		{
			Map result;
			try
			{
				result=arg["function"].Call(Map.Empty);
			}
			catch (Exception e)
			{
				result=arg["catch"].Call(new ObjectMap(e));
			}
			return result;
		}
		public static Map Split(Map arg)
		{
			Map arrays = new StrategyMap();
			Map subArray = new StrategyMap();
			Map array=arg["array"];
			for(int i=0;i<array.ArrayCount;i++)
			{
				Map map = array.Array[i];
				if (map.Equals(arg["item"]) || i==array.ArrayCount-1)
				{
					if (i == array.ArrayCount - 1)
					{
						subArray.Append(map);
					}
					arrays.Append(subArray);
					subArray = new StrategyMap();
				}
				else
				{
					subArray.Append(map);
				}
			}
			return arrays;
		}
		public static Map CreateConsole(Map arg)
		{
			Process.AllocConsole();
			return Map.Empty;
		}
		public static Map StrictlyIncreasing(Map arg)
		{
			bool increasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (!(arg[i + 1].GetNumber() > arg[i].GetNumber()))
				{
					increasing = false;
					break;
				}
			}
			return increasing;
		}
		public static Map StrictlyDecreasing(Map arg)
		{
			bool decreasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (!(arg[i + 1].GetNumber() < arg[i].GetNumber()))
				{
					decreasing = false;
					break;
				}
			}
			return decreasing;
		}
		public static Map Decreasing(Map arg)
		{
			bool nonincreasing = true;
			for (int i = 1; i  < arg.ArrayCount;i++)
			{
				if (arg[i + 1].GetNumber() > arg[i].GetNumber())
				{
					nonincreasing = false;
					break;
				}
			}
			return nonincreasing;
		}
		public static Map Increasing(Map arg)
		{
			bool nondecreasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (arg[i + 1].GetNumber() < arg[i].GetNumber())
				{
					nondecreasing = false;
					break;
				}
			}
			return nondecreasing;
		}
		public static Map And(Map arg)
		{
			bool and = true; ;
			foreach (Map map in arg.Array)
			{
				if (!map.GetBoolean())
				{
					and = false;
					break;
				}
			}
			return and;
		}
		public static Map Reciprocal(Map arg)
		{
			Number number = arg.GetNumber();
			return new Number(number.Denominator, number.Numerator);
		}
		public static Map Sum(Map arg)
		{
			Number result=0;
			foreach (Map map in arg.Array)
			{
				result += map.GetNumber();
			}
			return result;
		}
		public static string writtenText = "";
		public static void Write(string text)
		{
			writtenText += text;
			Console.Write(text);
		}
		public static Map Maximum(Map arg)
		{
			Number maximum = arg[1].GetNumber();
			foreach (Map map in arg.Array)
			{
				Number number = map.GetNumber();
				if (number > maximum)
				{
					maximum = number;
				}
			}
			return new StrategyMap(maximum);
		}
		public static Map Opposite(Map arg)
		{
			return arg.GetNumber()*-1;
		}
		public static Map Minimum(Map arg)
		{
			Number minumum = arg[1].GetNumber();
			foreach (Map map in arg.Array)
			{
				Number number = map.GetNumber();
				if (number < minumum)
				{
					minumum = number;
				}
			}
			return new StrategyMap(minumum);
		}
		public static Map Merge(Map arg)
		{
			Map result=new StrategyMap();
			foreach (Map map in arg.Array)
			{
				foreach (KeyValuePair<Map,Map> pair in map)
				{
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		public static Map Sort(Map arg)
		{
			List<Map> array = arg["array"].Array;
			array.Sort(new Comparison<Map>(delegate(Map a, Map b)
			{
				return arg["function"].Call(a).GetNumber().GetInt32().CompareTo(arg["function"].Call(b).GetNumber().GetInt32());
			}));
			return new StrategyMap(array);
		}
		public static Map Pop(Map arg)
		{
			Map count=new StrategyMap(arg.Array.Count);
			Map result = Map.Empty;
			foreach (KeyValuePair<Map,Map> pair in arg)
			{
				if (!pair.Key.Equals(count))
				{
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		public static Map Remove(Map arg)
		{
			Map map = arg["map"];
			Map key = arg["key"];
			Map result = Map.Empty;
			foreach (KeyValuePair<Map,Map> pair in map)
			{
				if (!pair.Key.Equals(key))
				{
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		// maybe rename, something more general
		public static Map Join(Map arg)
		{
			Map result = Map.Empty;
			Number counter = 1;
			foreach (Map map in arg.Array)
			{
				result.AppendRange(map);
			}
			return result;
		}
		public static Map While(Map arg)
		{
			while (arg["condition"].Call(Map.Empty).GetBoolean())
			{
				arg["function"].Call(Map.Empty);
			}
			return Map.Empty;
		}
	}
	// TODO: combine this with GAC
	public class WebDirectoryMap : DirectoryMap
	{
		public WebDirectoryMap(DirectoryInfo directory, PersistantPosition scope)
			: base(directory, scope)
		{
			this.Scope = scope;
		}
	}
	public class FileMap : StrategyMap
	{
		private string path;
		public FileMap(string path)
			: this(path, new DictionaryStrategy())
		{
		}
		public FileMap(string path, MapStrategy strategy)
			: base(strategy)
		{
			this.path = path;
		}
		public string Path
		{
			get
			{
				return path;
			}
		}
		// TODO: refactor, dont remember when this should be called
		public void Save()
		{
			File.WriteAllText(path,this.Count==0? "": Meta.Serialize.ValueFunction(this).Trim(Syntax.unixNewLine));
		}
		protected override void Set(Map key, Map value)
		{
			base.Set(key, value);
			// TODO: refactor
			//if (Expression.firstFile == null && path.EndsWith(".meta"))
			//{
			//    Save();
			//}
		}
	}
	// should be create in the new data parser, what about non-file source, though?
	public class FileSubMap : FileMap
	{
		private FileMap fileMap;
		public FileSubMap(FileMap fileMap):base(fileMap.Path)
		{
			this.fileMap = fileMap;
		}
		protected override void Set(Map key, Map value)
		{
			strategy.Set(key, value);
			//if (Expression.firstFile == null && fileMap.Path.EndsWith(".meta"))
			//{
			//    fileMap.Save();
			//}
		}
	}
	public class DrivesMap : Map
	{
		Dictionary<string, string> drives = new Dictionary<string, string>();
		public DrivesMap()
		{
			foreach (string drive in Directory.GetLogicalDrives())
			{
				drives.Add(drive.Remove(2), "");
			}
		}
		public override PersistantPosition Position
		{
			get
			{
				return new PersistantPosition(new Map[] { "localhost" });
			}
		}
		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsString)
			{
				containsKey = drives.ContainsKey(key.GetString());
			}
			else
			{
				containsKey = false;
			}
			return containsKey;
		}
		//public override bool IsCallable
		//{
		//    get { return false; }
		//}
		public override ICollection<Map> Keys
		{
			get 
			{
				// TODO: buggy, why is this called

				return new List<Map>();
				//throw new Exception("The method or operation is not implemented."); 
			}
		}
		protected override Map Get(Map key)
		{
			string name = key.GetString();
			// TODO: incorrect
			return !drives.ContainsKey(name)? null: new DirectoryMap(new DirectoryInfo(name+"\\"), this.Position);
		}
		protected override void Set(Map key, Map val)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		// what does this do? bad name
		protected override Map CopyData()
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class DirectoryMap : Map
	{
		public override PersistantPosition Position
		{
			get
			{
				return position;
			}
		}
		private PersistantPosition position;
		private DirectoryInfo directory;
		private List<Map> keys = new List<Map>();
		private static Map FindParent(DirectoryInfo dir)
		{
			Map parent;
			if (dir.Parent != null)
			{
				parent = new DirectoryMap(dir.Parent);
			}
			else
			{
				parent = new DrivesMap();
				FileSystem.fileSystem["localhost"] = parent;
			}
			return parent;
		}
		public DirectoryMap(DirectoryInfo directory)
			: this(directory, FindParent(directory).Position)
		{
		}
		public DirectoryMap(DirectoryInfo directory, PersistantPosition scope)
		{
			this.directory = directory;
			List<Map> pos = new List<Map>();
			DirectoryInfo dir = directory;
			do
			{
				pos.Add(dir.Name);
				dir = dir.Parent;
			}
			while (dir != null);
			pos.Add("localhost");
			pos.Reverse();
			this.position = new PersistantPosition(pos);
			foreach (DirectoryInfo subdir in directory.GetDirectories())
			{
				keys.Add(subdir.Name);
			}
			foreach (FileInfo file in directory.GetFiles("*.*"))
			{
				string fileName;
				if (file.Extension == ".meta" || file.Extension == ".dll")
				{
					fileName = Path.GetFileNameWithoutExtension(file.FullName);
				}
				else
				{
					fileName = file.Name;
				}
				keys.Add(fileName);
				this.Scope = scope;
			}
		}
		public override ICollection<Map> Keys
		{
			get { return keys; }
		}
		public class InteropSHFileOperation
		{
			public enum FO_Func : uint
			{
				FO_MOVE = 0x0001,
				FO_COPY = 0x0002,
				FO_DELETE = 0x0003,
				FO_RENAME = 0x0004,
			}

			struct SHFILEOPSTRUCT
			{
				public IntPtr hwnd;
				public FO_Func wFunc;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string pFrom;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string pTo;
				public ushort fFlags;
				public bool fAnyOperationsAborted;
				public IntPtr hNameMappings;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpszProgressTitle;

			}

			[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
			static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

			private SHFILEOPSTRUCT _ShFile;
			public FILEOP_FLAGS fFlags;

			public IntPtr hwnd
			{
				set
				{
					this._ShFile.hwnd = value;
				}
			}
			public FO_Func wFunc
			{
				set
				{
					this._ShFile.wFunc = value;
				}
			}

			public string pFrom
			{
				set
				{
					this._ShFile.pFrom = value + '\0' + '\0';
				}
			}
			public string pTo
			{
				set
				{
					this._ShFile.pTo = value + '\0' + '\0';
				}
			}

			public bool fAnyOperationsAborted
			{
				set
				{
					this._ShFile.fAnyOperationsAborted = value;
				}
			}
			public IntPtr hNameMappings
			{
				set
				{
					this._ShFile.hNameMappings = value;
				}
			}
			public string lpszProgressTitle
			{
				set
				{
					this._ShFile.lpszProgressTitle = value + '\0';
				}
			}

			public InteropSHFileOperation()
			{

				this.fFlags = new FILEOP_FLAGS();
				this._ShFile = new SHFILEOPSTRUCT();
				this._ShFile.hwnd = IntPtr.Zero;
				this._ShFile.wFunc = FO_Func.FO_COPY;
				this._ShFile.pFrom = "";
				this._ShFile.pTo = "";
				this._ShFile.fAnyOperationsAborted = false;
				this._ShFile.hNameMappings = IntPtr.Zero;
				this._ShFile.lpszProgressTitle = "";

			}

			public bool Execute()
			{
				this._ShFile.fFlags = this.fFlags.Flag;
				int ReturnValue = SHFileOperation(ref this._ShFile);
				if (ReturnValue == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			public class FILEOP_FLAGS
			{
				[Flags]
				private enum FILEOP_FLAGS_ENUM : ushort
				{
					FOF_MULTIDESTFILES = 0x0001,
					FOF_CONFIRMMOUSE = 0x0002,
					FOF_SILENT = 0x0004,  // don't create progress/report
					FOF_RENAMEONCOLLISION = 0x0008,
					FOF_NOCONFIRMATION = 0x0010,  // Don't prompt the user.
					FOF_WANTMAPPINGHANDLE = 0x0020,  // Fill in SHFILEOPSTRUCT.hNameMappings
					// Must be freed using SHFreeNameMappings
					FOF_ALLOWUNDO = 0x0040,
					FOF_FILESONLY = 0x0080,  // on *.*, do only files
					FOF_SIMPLEPROGRESS = 0x0100,  // means don't show names of files
					FOF_NOCONFIRMMKDIR = 0x0200,  // don't confirm making any needed dirs
					FOF_NOERRORUI = 0x0400,  // don't put up error UI
					FOF_NOCOPYSECURITYATTRIBS = 0x0800,  // dont copy NT file Security Attributes
					FOF_NORECURSION = 0x1000,  // don't recurse into directories.
					FOF_NO_CONNECTED_ELEMENTS = 0x2000,  // don't operate on connected elements.
					FOF_WANTNUKEWARNING = 0x4000,  // during delete operation, warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
					FOF_NORECURSEREPARSE = 0x8000,  // treat reparse points as objects, not containers
				}

				public bool FOF_MULTIDESTFILES = false;
				public bool FOF_CONFIRMMOUSE = false;
				public bool FOF_SILENT = false;
				public bool FOF_RENAMEONCOLLISION = false;
				public bool FOF_NOCONFIRMATION = false;
				public bool FOF_WANTMAPPINGHANDLE = false;
				public bool FOF_ALLOWUNDO = false;
				public bool FOF_FILESONLY = false;
				public bool FOF_SIMPLEPROGRESS = false;
				public bool FOF_NOCONFIRMMKDIR = false;
				public bool FOF_NOERRORUI = false;
				public bool FOF_NOCOPYSECURITYATTRIBS = false;
				public bool FOF_NORECURSION = false;
				public bool FOF_NO_CONNECTED_ELEMENTS = false;
				public bool FOF_WANTNUKEWARNING = false;
				public bool FOF_NORECURSEREPARSE = false;
				public ushort Flag
				{
					get
					{
						ushort ReturnValue = 0;

						if (this.FOF_MULTIDESTFILES == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_MULTIDESTFILES;
						if (this.FOF_CONFIRMMOUSE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_CONFIRMMOUSE;
						if (this.FOF_SILENT == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SILENT;
						if (this.FOF_RENAMEONCOLLISION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_RENAMEONCOLLISION;
						if (this.FOF_NOCONFIRMATION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMATION;
						if (this.FOF_WANTMAPPINGHANDLE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTMAPPINGHANDLE;
						if (this.FOF_ALLOWUNDO == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_ALLOWUNDO;
						if (this.FOF_FILESONLY == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_FILESONLY;
						if (this.FOF_SIMPLEPROGRESS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SIMPLEPROGRESS;
						if (this.FOF_NOCONFIRMMKDIR == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMMKDIR;
						if (this.FOF_NOERRORUI == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOERRORUI;
						if (this.FOF_NOCOPYSECURITYATTRIBS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCOPYSECURITYATTRIBS;
						if (this.FOF_NORECURSION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSION;
						if (this.FOF_NO_CONNECTED_ELEMENTS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NO_CONNECTED_ELEMENTS;
						if (this.FOF_WANTNUKEWARNING == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTNUKEWARNING;
						if (this.FOF_NORECURSEREPARSE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSEREPARSE;

						return ReturnValue;
					}
				}
			}

		}
        public static void TrashFile(string fname)
        {
			InteropSHFileOperation fo = new InteropSHFileOperation();
			fo.wFunc = InteropSHFileOperation.FO_Func.FO_DELETE;
			fo.fFlags.FOF_ALLOWUNDO = true;
			fo.fFlags.FOF_NOCONFIRMATION = true;
			fo.pFrom = fname;
        }
		//public override bool IsCallable
		//{
		//    get { return false; }
		//}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		// fix
		protected override Map Get(Map key)
		{
			Map value = null;
			if (key.IsString)
			{
				if (cache.ContainsKey(key))
				{
					value = cache[key];
				}
				else
				{
					string name = key.GetString();
					if (directory.FullName != Process.LibraryPath)
					{
						Directory.SetCurrentDirectory(directory.FullName);
					}
					string file = Path.Combine(directory.FullName, name);
					string metaFile = Path.Combine(directory.FullName, name + ".meta");
					string dllFile = Path.Combine(directory.FullName, name + ".dll");
					if (key.GetString().StartsWith("Background"))
					{
					}
					if (File.Exists(metaFile))
					{
						string text = File.ReadAllText(metaFile, Encoding.Default);
						//string text = File.ReadAllText(metaFile, Encoding.Default);
						Map result;
						FileMap fileMap = new FileMap(metaFile);
						if (text != "")
						{
							Parser parser = new Parser(text, metaFile,new PersistantPosition(this.Position,name));
							bool matched;
							if(parser.file.Contains("string.meta"))
							{
							}
							if (parser.text.Length == 12)
							{
							}
							result = Parser.Data.File.Match(parser, out matched);
							if (parser.index != parser.text.Length)
							{
								throw new SyntaxException("Expected end of file.", parser);
							}
							//Expression.parsingFile = metaFile;
							value = result;
							//Expression.parsingFile = null;
							//Expression.firstFile = null;
						}
						else
						{
							value = Map.Empty;
						}
						value.Scope = new TemporaryPosition(this);
					}
					else
					{
						bool dllLoaded = false;
						if (File.Exists(dllFile))
						{
							try
							{
								Assembly assembly = Assembly.LoadFile(dllFile);
								value = Gac.LoadAssembly(assembly);
								dllLoaded = true;
							}
							catch (Exception e)
							{
								value = null;
							}
						}
						if (!dllLoaded)
						{
							if (File.Exists(file))
							{
								switch (Path.GetExtension(file))
								{
									case ".txt":
									case ".meta":
										value = new FileMap(file, new ListStrategy());
										// this is problematic, writes the file all the time
										//foreach (char c in File.ReadAllText(file))
										////foreach (char c in File.ReadAllText(file))
										//{
										//    value.Append(c);
										//}
										break;
									default:
										value = new FileMap(file, new ListStrategy());
										// problematic, writes the file
										//foreach (byte b in File.ReadAllBytes(file))
										////foreach (byte b in File.ReadAllBytes(file))
										//{
										//    value.Append(b);
										//}
										break;
								}
							}
							else
							{
								DirectoryInfo subDir = new DirectoryInfo(Path.Combine(directory.FullName, name));
								if (subDir.Exists)
								{
									value = new DirectoryMap(subDir, this.Position);
									//value = new DirectoryMap(subDir, new TemporaryPosition(this));
								}
								else
								{
									value = null;
								}
							}
						}
					}
					if (value != null)
					{
						value.Scope = new TemporaryPosition(this);
						cache[key] = value;
					}
				}
			}
			return value;
		}
		protected override void Set(Map key, Map val)
		{
			if (key.IsString)
			{
				string name = key.GetString();
				string extension = Path.GetExtension(name);
				if (extension == "")
				{
					string directoryPath=Path.Combine(this.directory.FullName,name);
					// somewhat unlogical
					if (Directory.Exists(directoryPath))
					{
						Map subDirectory = this[name];
						foreach (KeyValuePair<Map, Map> entry in val)
						{
							// buggy if there is a Meta file with the same name
							subDirectory[entry.Key] = entry.Value;
						}
					}
					else
					{
						string text = Meta.Serialize.ValueFunction(val);
						if (text == Syntax.emptyMap.ToString())
						{
							text = "";
						}
						else
						{
							text = text.Trim(Syntax.unixNewLine);
						}
						File.WriteAllText(Path.Combine(directory.FullName, name + ".meta"), text);
					}
				}
				else if (extension == ".txt" || extension == ".meta" || extension==".html" || extension==".htm")
				{
					File.WriteAllText(Path.Combine(directory.FullName, name), (string)Transform.TryToDotNet(val, typeof(string)));
				}
				else
				{
					File.WriteAllBytes(Path.Combine(directory.FullName, name), (byte[])Transform.TryToDotNet(val, typeof(byte[])));
				}
				cache[key] = val;
			}
			else
			{
				throw new ApplicationException("Cannot set non-string in directory.");
			}
		}
		protected override Map CopyData()
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class Process
	{
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern IntPtr GetStdHandle(int nStdHandle);
		public static bool useConsole = false;
		public static void UseConsole()
		{
			AllocConsole();
			useConsole = true;
		}
		public class Commands
		{
			public static void Help()
			{
				UseConsole();
				Console.WriteLine("help");
				Console.ReadLine();
			}
			public static void Interactive()
			{
				UseConsole();
				Console.WriteLine("Interactive mode of Meta 0.1");
				Map map = new StrategyMap();
				map.Scope = new TemporaryPosition(FileSystem.fileSystem);
				string code;

				// this is still kinda wrong, interactive mode should exist in filesystem, somehow
				Parser parser = new Parser("", "Interactive console",FileSystem.fileSystem.Position);
				//parser.functions++;
				parser.defaultKeys.Push(1);
				while (true)
				{
					code = "";
					Console.Write(parser.Line + " ");
					int lines = 0;
					while (true)
					{
						string input = Console.ReadLine();
						if (input.Trim().Length != 0)
						{
							code += input + Syntax.unixNewLine;
							char character = input[input.TrimEnd().Length - 1];
							if (!(Char.IsLetter(character) || character == ']' || character == Syntax.lookupStart) && !input.StartsWith("\t") && character != '=')
							{
								break;
							}
						}
						else
						{
							if (lines != 0)
							{
								break;
							}
						}
						lines++;
						Console.Write(parser.Line + lines + " ");
						for (int i = 0; i < input.Length && input[i] == '\t'; i++)
						{
							SendKeys.SendWait("{TAB}");
						}
					}
					try
					{
						bool matched;
						parser.text += code;
						Map statement = Parser.Statement.Match(parser, out matched);
						if (matched)
						{
							if (parser.index == parser.text.Length)
							{
								statement.GetStatement().Assign(ref map);
							}
							else
							{
								parser.index = parser.text.Length;
								// make this more exact
								throw new SyntaxException("Syntax error", parser);
							}
						}
						Console.WriteLine();
					}
					catch (Exception e)
					{
						Console.WriteLine(e.ToString());
					}
				}
			}
			public static void Test()
			{
				UseConsole();
				new MetaTest().Run();
				//Console.ReadLine();
			}
			public static void Run(string[] args)
			{
				int i = 1;
				int fileIndex = 0;
				// bad
				if (args[0] == "-console")
				{
					UseConsole();
					i++;
					fileIndex++;
				}
				string path = args[fileIndex];
				string positionPath = Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(args[fileIndex]));

				string[] position=positionPath.Split(Path.DirectorySeparatorChar);
				Map function=FileSystem.fileSystem["localhost"];
				foreach (string pos in position)
				{
					function = function[pos];
				}

				function.Call(Map.Empty);
			}
		}
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{

				if (args.Length == 0)
				{
					Commands.Interactive();
				}
				else
				{
					switch(args[0])
					{
						case "-interactive":
							Commands.Interactive();
							break;
						case "-test":
							Commands.Test();
							break;
						case "-help":
							Commands.Help();
							break;
						default:
							Commands.Run(args);
							break;
					}
				}
			}
			catch (MetaException e)
			{
				string text = e.ToString();
				System.Diagnostics.Process.Start("devenv",e.Extent.FileName+@" /command ""Edit.GoTo "+e.Extent.Start.Line+"\"");
				if (useConsole)
				{
					Console.WriteLine(text);
					Console.ReadLine();
				}
				else
				{
					MessageBox.Show(text, "Meta exception");
				}
			}
			catch (Exception e)
			{
				string text = e.ToString();
				if (useConsole)
				{
					Console.WriteLine(text);
					Console.ReadLine();
				}
				else
				{
					MessageBox.Show(text, "Meta exception");
				}
			}
		}
		Thread thread;
		private Map parameter;
		private Map program;
		public Process(Map program,Map parameter)
		{
			this.thread=new Thread(new ThreadStart(Run));
			processes[thread]=this;
			this.program=program;
			this.parameter=parameter;
		}
		static Process()
		{
			processes[Thread.CurrentThread]=new Process(null,null);
		}
		public Process() : this(FileSystem.fileSystem, new StrategyMap())
		{
		}
		public void Run()
		{
			program.Call(parameter);
		}
		public void Start()
		{
			thread.Start();
		}
		public void Stop()
		{
			if(thread.ThreadState!=System.Threading.ThreadState.Suspended && thread.ThreadState!=System.Threading.ThreadState.SuspendRequested)
			{
				thread.Suspend();
			}
		}
		public void Pause()
		{
			thread.Suspend();
		}
		public void Continue()
		{
			if(thread.ThreadState==System.Threading.ThreadState.Suspended)
			{
				thread.Resume();
			}
		}
		public static Process Current
		{
			get
			{
				return (Process)processes[Thread.CurrentThread];
			}
		}
		private static Dictionary<Thread,Process> processes=new Dictionary<Thread,Process>();
		private bool reverseDebugging=false;
		private SourcePosition breakPoint=new SourcePosition(0,0);

		public bool ReverseDebugging
		{
			get
			{
				return reverseDebugging;
			}
			set
			{
				reverseDebugging=value;
			}
		}

		public event DebugBreak Break;

		public delegate void DebugBreak(Map data);
		public void CallBreak(Map data)
		{
			if(Break!=null)
			{
				if(data==null)
				{
					data=new StrategyMap("nothing");
				}
				Break(data);
				Thread.CurrentThread.Suspend();
			}
		}
		private static string installationPath=null;
		public static string InstallationPath
		{
			get
			{
				if (installationPath == null)
				{
					// fix this
					// how to find this out?
					// maybe look in registry
					// no other reliable way
					// maybe Meta should be put in the Gac, actually
					// difficult to debug though, maybe
					// but where in the registry
					// who would have to install it?
					// should be installable for non-admins, too
					installationPath = @"C:\Meta\0.1\";
				}
				return installationPath;
			}
			set
			{
				installationPath = value;
			}
		}
		public static string LibraryPath
		{
			get
			{
				// fix this
				return @"C:\Meta\0.1\Library";
			}
		}
	}
	public abstract class Position
	{
		public abstract Map Get();
	}
	public class TemporaryPosition:Position
	{
		private Map map;
		public TemporaryPosition(Map map)
		{
			this.map = map;
		}
		public override Map Get()
		{
			return map;
		}
	}
	public class PersistantPosition : Position
	{
		private List<Map> keys;
		public PersistantPosition(ICollection<Map> keys)
		{
			this.keys = new List<Map>(keys);
		}
		public PersistantPosition(PersistantPosition parent, Map ownKey)
		{
			this.keys = new List<Map>(parent.keys);
			this.keys.Add(ownKey);
		}
		public override Map Get()
		{
			Map position = FileSystem.fileSystem;
			foreach(Map key in keys)
			{
				position = position[key];
			}
			return position;
		}
	}
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public static implicit operator Map(Number integer)
		{
			return new StrategyMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new StrategyMap(new Number((int)(boolean ? 1 : 0)));
		}
		public static implicit operator Map(char character)
		{
			return new StrategyMap(new Number(character));
		}
		public static implicit operator Map(byte integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(int integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(long integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(string text)
		{
			return new StrategyMap(text);
		}
		// fix this
		public virtual PersistantPosition Position
		{
			get
			{
				return null;
			}
		}
		public virtual void AppendRange(Map array)
		{
			AppendRangeDefault(array);
		}
		public virtual void AppendRangeDefault(Map array)
		{
			int counter = ArrayCount + 1;
			foreach (Map map in array.Array)
			{
				this[counter] = map;
				counter++;
			}
		}
		private object compiledCode;

		public Statement GetStatement()
		{
			if (compiledCode == null)
			{
				compiledCode = new Statement(this);
			}
			return (Statement)compiledCode;
		}
		public Subselect GetSubselect()
		{
			if (compiledCode == null)
			{
				compiledCode = CreateSubselect();
			}
			return (Subselect)compiledCode;
		}
		public Expression GetExpression()
		{
			if (compiledCode == null)
			{
				compiledCode = CreateExpression();
			}
			return (Expression)compiledCode;
		}
		public Subselect CreateSubselect()
		{
			if (ContainsKey(Code.Current))
			{
				return new Current();
			}
			else if (ContainsKey(Code.Search))
			{
				return new Search(this[Code.Search]);
			}
			else if (ContainsKey(Code.Lookup))
			{
				return new Lookup(this[Code.Lookup]);
			}
			else if (ContainsKey(Code.Root))
			{
				return new Root();
			}
			else if (ContainsKey(Code.Call))
			{
				return new CallSubselect(this[Code.Call]);
			}
			else
			{
				throw new Exception("Map is not a subselect.");
			}
		}
		public Expression CreateExpression()
		{
			if (ContainsKey(Code.Call))
			{
				return new Call(this[Code.Call]);
			}
			else if (ContainsKey(Code.Program))
			{
				return new Program(this[Code.Program]);
			}
			else if (ContainsKey(Code.Literal))
			{
				return new Literal(this[Code.Literal]);
			}
			else if (ContainsKey(Code.Select))
			{
				return new Select(this[Code.Select]);
			}
			else
			{
				throw new ApplicationException("Cannot compile map.");
			}
		}
		public void Append(Map map)
		{
			this[ArrayCount + 1] = map;
		}
		public static Map Empty
		{
			get
			{
				return new StrategyMap(new EmptyStrategy());
			}
		}
		public virtual string Serialize()
		{
			string text;
			if (this.Count == 0)
			{
				text = "0";
			}
			else if (this.IsString)
			{
				text = "\"" + this.GetString() + "\"";
			}
			else if (this.IsNumber)
			{
				text = this.GetNumber().ToString();
			}
			else
			{
				text = null;
			}
			return text;
		}
		public virtual bool IsBoolean
		{
			get
			{
				return IsNumber && (GetNumber()==0 || GetNumber()==1);
			}
		}
		public virtual bool IsNumber
		{
			get
			{
				return IsNumberDefault;
			}
		}
		public virtual bool IsString
		{
			get
			{
				return IsStringDefault;
			}
		}
		public bool IsNumberDefault
		{
			get
			{
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this[Map.Empty].IsNumber);
			}
		}
		public bool IsStringDefault
		{
			get
			{
				return ArrayCount == Count && this.Array.TrueForAll(delegate(Map map)
				{
					return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);
				});
			}
		}
		public virtual Number GetNumber()
		{
			return GetNumberDefault();
		}
		public virtual string GetString()
		{
			return GetStringDefault();
		}
		public virtual bool GetBoolean()
		{
			bool boolean;
			Number number = GetNumber();
			if (number == 0)
			{
				boolean = false;
			}
			else if (number == 1)
			{
				boolean = true;
			}
			else
			{
				throw new ApplicationException("Map is not a boolean.");
			}
			return boolean;
		}
		public string GetStringDefault()
		{
			string text="";
			foreach(Map key in Keys)
			{
				text+=Convert.ToChar(this[key].GetNumber().GetInt32());
			}
			return text;
		}
		public Number GetNumberDefault()
		{
			Number number;
			if (this.Equals(Map.Empty))
			{
				number = 0;
			}
			else if (this.Count == 1 && this.ContainsKey(Map.Empty) && this[Map.Empty].IsNumber)
			{
				number = 1 + this[Map.Empty].GetNumber();
			}
			else
			{
				throw new ApplicationException("Map is not an integer");
			}
			return number;
		}
		public Position Scope
		{
			get
			{
				return scope;
			}
			set
			{
				scope = value;
			}
		}
		private Position scope;
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		public virtual int ArrayCount
		{
			get
			{
				return GetArrayCountDefault();
			}
		}
		public int GetArrayCountDefault()
		{
			int i = 1;
			for (; this.ContainsKey(i); i++)
			{
			}
			return i - 1;
		}
		public virtual List<Map> Array
		{
			get
			{
				List<Map> array = new List<Map>(Count);
				int index = 1;
				while (this.ContainsKey(index))
				{
					array.Add(this[index]);
				}
				return array;
			}
		}
        public Map this[Map key]
        {
			get
			{
				return Get(key);
			}
			set
			{
				if (value != null)
				{
					// this isnt sufficient
					compiledCode = null;
					Map val = value;
					// refactor
					if (val.scope == null || val.scope.Get() == null)
					{
						val.scope = new TemporaryPosition(this);
					}
					Set(key, val);
				}
			}
        }
        protected abstract Map Get(Map key);
        protected abstract void Set(Map key, Map val);
		public virtual Map Call(Map arg)
		{
			return this[Code.Function].GetExpression().Evaluate(
				new StrategyMap(new TemporaryPosition(this), this[Code.Function][Code.ParameterName], arg));
		}
		public abstract ICollection<Map> Keys
		{
			get;
		}
		// refactor
		public Map Copy()
		{
			Map clone = CopyData();
			clone.Scope = Scope;
			clone.Extent = Extent;
			return clone;
		}
		protected abstract Map CopyData();
		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}
		public override int GetHashCode()
		{
			if (IsNumber)
			{
				//refactor, this is  totally wrong
				return (int)(GetNumber().Numerator % int.MaxValue);
			}
			else
			{
				return Count;
			}
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public virtual IEnumerator<KeyValuePair<Map, Map>> GetEnumerator()
		{
			foreach (Map key in Keys)
			{
				yield return new KeyValuePair<Map, Map>(key, this[key]);
			}
		}
		Extent extent;
		[Serialize(1)]
		public Extent Extent
		{
			get
			{
				return extent;
			}
			set
			{
				extent=value;
			}
		}
	}
	public class StrategyMap:Map
	{
		public StrategyMap(Position scope)
			: this()
		{
			this.Scope = scope;
		}
		protected MapStrategy strategy;
		public StrategyMap(bool boolean)
			: this(new Number(boolean))
		{
		}
		public StrategyMap(System.Collections.Generic.ICollection<Map> list)
			: this(new ListStrategy())
		{
			int index = 1;
			foreach (object entry in list)
			{
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}

		public StrategyMap(MapStrategy strategy)
		{
			this.strategy = strategy;
			this.strategy.map = this;
		}
		public StrategyMap()
			: this(new EmptyStrategy())
		{
		}
		public StrategyMap(int i)
			: this(new Number(i))
		{
		}
		public StrategyMap(Number number)
			: this(new NumberStrategy(number))
		{
		}
		public StrategyMap(string text)
			: this(new ListStrategy(text))
		{
		}
		public StrategyMap(Position scope, params Map[] keysAndValues)
			: this(keysAndValues)
		{
			this.Scope = scope;
		}
		public StrategyMap(params Map[] keysAndValues):this()
		{
			for (int i = 0; i <= keysAndValues.Length - 2; i += 2)
			{
				this[keysAndValues[i]] = keysAndValues[i + 1];
			}
		}
		public override int ArrayCount
		{
			get
			{
				return strategy.GetArrayCount();
			}
		}
		public override void AppendRange(Map array)
		{
			strategy.AppendRange(array);
		}
		public void InitFromStrategy(MapStrategy clone)
		{
			foreach (Map key in clone.Keys)
			{
				this[key] = clone.Get(key);
			}
		}
		public override bool IsString
		{
			get
			{
				return strategy.IsString;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return strategy.IsNumber;
			}
		}
		public override Number GetNumber()
		{
			return strategy.GetNumber();
		}
		public override string GetString()
		{
			return strategy.GetString();
		}
		public override int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public override List<Map> Array
		{
			get
			{
				return strategy.Array;
			}
		}
		protected override Map Get(Map key)
		{
			return strategy.Get(key);
		}
		protected override void Set(Map key, Map value)
		{
			strategy.Set(key, value); 
		}
		protected override Map CopyData() 
		{ 
			return strategy.CopyData(); 
		}
		public override bool ContainsKey(Map key) 
		{ 
			return strategy.ContainsKey(key); 
		}
		public override ICollection<Map> Keys
		{
			get { 
				return strategy.Keys; 
			}
		}
		public override bool Equals(object toCompare)
		{
			bool isEqual;
			if (Object.ReferenceEquals(toCompare, this))
			{
				isEqual = true;
			}
			else if (toCompare is StrategyMap)
			{
				isEqual = ((StrategyMap)toCompare).strategy.Equal(strategy);
			}
			else
			{
				isEqual = false;
			}
			return isEqual;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		public MapStrategy Strategy
		{
			get
			{
				return strategy;
			}
			set
			{
				strategy = value;
				strategy.map = this;
			}
		}
	}
	public class Transform
	{
		public static object ToDotNet(Map meta, Type target)
		{
			bool converted;
			object dotNet=ToDotNet(meta, target, out converted);
			if (!converted)
			{
				throw new ApplicationException("Cannot convert argument.");
			}
			return dotNet;
		}

		// combine with method below
		public static object ToDotNet(Map meta, Type target,out bool converted)
		{
			object dotNet = TryToDotNet(meta, target);
			converted = dotNet != null;
			return dotNet;
		}
		public static Delegate CreateDelegateFromCode(Type delegateType, Map code)
		{
			MethodInfo invoke = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters)
			{
				arguments.Add(parameter.ParameterType);
			}
			DynamicMethod hello = new DynamicMethod("EventHandler",
				invoke.ReturnType,
				arguments.ToArray(),
				typeof(Map).Module);
			ILGenerator il = hello.GetILGenerator();

			LocalBuilder local = il.DeclareLocal(typeof(object[]));
			il.Emit(OpCodes.Ldc_I4, parameters.Length);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, local);

			for (int i = 0; i < parameters.Length; i++)
			{
				il.Emit(OpCodes.Ldloc, local);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldarg, i + 1);
				il.Emit(OpCodes.Stelem_Ref);
			}
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Call, typeof(MetaDelegate).GetMethod("Call"));

			if (invoke.ReturnType == typeof(void))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Castclass, invoke.ReturnType);
				il.Emit(OpCodes.Ret);
			}
			Delegate del = (Delegate)hello.CreateDelegate(delegateType, new MetaDelegate(code, invoke.ReturnType));
			return del;
		}


		public class EventHandlerContainer
		{
			private Map callable;
			public EventHandlerContainer(Map callable)
			{
				this.callable = callable;
			}
			public Map Raise(Map argument)
			{
				return callable.Call(argument);
			}
		}
		public class MetaDelegate
		{
			private Map callable;
			private Type returnType;
			public MetaDelegate(Map callable, Type returnType)
			{
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments)
			{
				Map arg = new StrategyMap();
				foreach (object argument in arguments)
				{
					arg.Append(Transform.ToSimpleMeta(argument));
				}
				Map result = this.callable.Call(arg);
				return Meta.Transform.TryToDotNet(result, this.returnType);
			}
		}
		public static object TryToDotNet(Map meta, Type target)
		{
			object dotNet = null;
			if (target.IsSubclassOf(typeof(Enum)) && meta.IsNumber)
			{
				dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());
			}
			else
			{
				switch (Type.GetTypeCode(target))
				{
					case TypeCode.Boolean:
						if (meta.IsBoolean)
						{
							dotNet = meta.GetBoolean();
						}
						break;
					case TypeCode.Byte:
						if (IsIntegerInRange(meta, Byte.MinValue, Byte.MaxValue))
						{
							dotNet = Convert.ToByte(meta.GetNumber().GetInt32());
						}
						break;
					case TypeCode.Char:
						if (IsIntegerInRange(meta, Char.MinValue, Char.MaxValue))
						{
							dotNet = Convert.ToChar(meta.GetNumber().GetInt32());
						}
						break;
					case TypeCode.DateTime:
						dotNet = null;
						break;
					case TypeCode.DBNull:
						dotNet = null;
						break;
					case TypeCode.Decimal:
						if (IsIntegerInRange(meta, decimal.MinValue, decimal.MaxValue))
						{
							// add function, GetDecimal
							dotNet = (decimal)(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Double:
						if (IsIntegerInRange(meta, double.MinValue, double.MaxValue))
						{
							// fix this
							dotNet = (double)(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Int16:
						if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue))
						{
							dotNet = Convert.ToInt16(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Int32:
						if (IsIntegerInRange(meta, Int32.MinValue, Int32.MaxValue))
						{
							dotNet = meta.GetNumber().GetInt32();
						}
						break;
					case TypeCode.Int64:
						if (IsIntegerInRange(meta, new Number(Int64.MinValue), new Number(Int64.MaxValue)))
						{
							dotNet = Convert.ToInt64(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Object:
						if (target == typeof(Type) && meta is TypeMap)
						{
							dotNet = ((TypeMap)meta).type;
						}
						else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
								&& meta.ContainsKey(Code.Function))
						{
							dotNet = CreateDelegateFromCode(target, meta);
						}
						else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).type))
						{
							dotNet = ((ObjectMap)meta).obj;
						}
						else if (target.IsAssignableFrom(meta.GetType()))
						{
							dotNet = meta;
						}
						// maybe remove
						else if (target.IsArray)
						{
							string[] asdf;
							ArrayList list = new ArrayList();
							bool converted=true;
							Type elementType=target.GetElementType();
							foreach (Map m in meta.Array)
							{
								object o = Transform.TryToDotNet(m, elementType);
								if (o != null)
								{
									list.Add(o);
								}
								else
								{
									converted=false;
									break;
								}
							}
							if (converted)
							{
								dotNet=list.ToArray(elementType);
							}
						}
						else
						{
							ObjectMap result;
							ConstructorInfo constructor = target.GetConstructor(BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
							if (constructor != null)
							{
								result = new ObjectMap(target.GetConstructor(new Type[] { }).Invoke(new object[] { }));
							}
							else if (target.IsValueType)
							{
								if (target.Equals(typeof(void)))
								{
									result = null;
									break;
								}
								else
								{
									result = new ObjectMap(target.InvokeMember(".ctor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Static, null, null, new object[] { }));
								}
							}
							else
							{
								break;
							}
							foreach (KeyValuePair<Map, Map> pair in meta)
							{
								((Property)result[pair.Key])[DotNet.Set].Call(pair.Value);
							}
							dotNet = result.Object;
						}
						break;
					case TypeCode.SByte:
						if (IsIntegerInRange(meta, SByte.MinValue, SByte.MaxValue))
						{
							dotNet = Convert.ToSByte(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Single:
						if (IsIntegerInRange(meta, Single.MinValue, Single.MaxValue))
						{
							// wrong
							dotNet = (float)meta.GetNumber().GetInt64();
						}
						break;
					case TypeCode.String:
						if (meta.IsString)
						{
							dotNet = meta.GetString();
						}
						break;
					case TypeCode.UInt16:
						if (IsIntegerInRange(meta, UInt16.MinValue, UInt16.MaxValue))
						{
							dotNet = Convert.ToUInt16(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.UInt32:
						if (IsIntegerInRange(meta, new Number(UInt32.MinValue), new Number(UInt32.MaxValue)))
						{
							dotNet = Convert.ToUInt32(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.UInt64:
						if (IsIntegerInRange(meta, new Number(UInt64.MinValue), new Number(UInt64.MaxValue)))
						{
							dotNet = Convert.ToUInt64(meta.GetNumber().GetInt64());
						}
						break;
					default:
						throw new ApplicationException("not implemented");
				}
			}
			if (dotNet == null)
			{
				if(meta is ObjectMap && ((ObjectMap)meta).type==target)
				{
					dotNet = ((ObjectMap)meta).Object;
				}
			}
			return dotNet;
		}
		public static bool IsIntegerInRange(Map meta,Number minValue,Number maxValue)
		{
			return meta.IsNumber && meta.GetNumber()>=minValue && meta.GetNumber()<=maxValue;
		}
		public static Map ToSimpleMeta(object dotNet)
		{
			if (dotNet == null)
			{
				return new ObjectMap(null, typeof(Object));
			}
			else if (dotNet is Map)
			{
				return (Map)dotNet;
			}
			else
			{
				return new ObjectMap(dotNet);
			}
		}
		public static Map ToMeta(object dotNet)
		{
			if (dotNet == null)
			{
				return Map.Empty;
			}
			else
			{
				switch(Type.GetTypeCode(dotNet.GetType()))
				{
					case TypeCode.Boolean:
						return Convert.ToInt32(((bool)dotNet));
					case TypeCode.Byte:
						return (byte)dotNet;
					case TypeCode.Char:
						return (char)dotNet;
					case TypeCode.DateTime:
						return new ObjectMap(dotNet);
					case TypeCode.DBNull:
						return new ObjectMap(dotNet);
					case TypeCode.Decimal:
						return (int)dotNet;
					case TypeCode.Double:
						return (int)dotNet;
					case TypeCode.Int16:
						return (short)dotNet;
					case TypeCode.Int32:
						return (int)dotNet;
					case TypeCode.Int64:
						return (long)dotNet;
					case TypeCode.Object:
						if(dotNet.GetType().IsSubclassOf(typeof(Enum)))
						{
							return (int)Convert.ToInt32((Enum)dotNet);
						}
						else if(dotNet is Map)
						{
							return (Map)dotNet;
						}
						else
						{
							return new ObjectMap(dotNet);
						}
					case TypeCode.SByte:
						return (sbyte)dotNet;
					case TypeCode.Single:
						return (int)dotNet;
					case TypeCode.String:
						return (string)dotNet;
					case TypeCode.UInt32:
						return (uint)dotNet;
					case TypeCode.UInt64:
						return (ulong)dotNet;
					case TypeCode.UInt16:
						return (ushort)dotNet;
					default:
						throw new ApplicationException("not implemented");
				}
			}
		}
	}
	public abstract class MethodImplementation:Map
	{
		protected MethodBase method;
		protected object obj;
		protected Type type;
		public MethodImplementation(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
		}
		public override bool IsString
		{
			get
			{
				return false;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}
		public override Map Call(Map argument)
		{
			ParameterInfo[] parameters = method.GetParameters();
			object[] arguments = new object[parameters.Length];
			if (parameters.Length == 1)
			{
				arguments[0] = Transform.ToDotNet(argument, parameters[0].ParameterType);
			}
			else
			{
				for (int i = 0; i < parameters.Length; i++)
				{
					arguments[i]=Transform.ToDotNet(argument[i + 1], parameters[i].ParameterType);
				}
			}
			try
			{
				return Transform.ToMeta(
					method is ConstructorInfo ?
						((ConstructorInfo)method).Invoke(arguments) :
						 method.Invoke(obj, arguments));
			}
			catch (Exception e)
			{
				throw e.InnerException;
			}
		}
	}
	public class Method : MethodImplementation
	{
	    private Dictionary<Map, MethodOverload> overloadedMethods;
		// why overloads and method itself?
	    private Method(Dictionary<Map, MethodOverload> overloadedMethods,MethodBase method,object obj,Type type):base(method,obj,type)
	    {
	        this.overloadedMethods = overloadedMethods;
	    }
		// refactor
		public Method(string name, object obj, Type type)
			: base(GetSingleMethod(name, obj, type), obj, type)
		{
			MemberInfo[] methods = type.GetMember(name, GetBindingFlags(obj, name));
			// refactor
			if (methods.Length > 1)
			{
				this.overloadedMethods = new Dictionary<Map, MethodOverload>();
				foreach (MethodBase method in methods)
				{
					Map key;

					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1)
					{
						key = new TypeMap(parameters[0].ParameterType);
					}
					else
					{
						key = new StrategyMap();
						foreach (ParameterInfo parameter in parameters)
						{
							key.Append(new TypeMap(parameter.ParameterType));
						}
					}
					MethodOverload overload = new MethodOverload(method, obj, type);
					overloadedMethods[key] = overload;
				}
			}
			else
			{
				this.overloadedMethods = null;
			}
		}
		public Method(Type type)
			: this(".ctor", null, type)
		{
		}
		private static MethodBase GetSingleMethod(string name, object obj, Type type)
		{
			MemberInfo[] members = type.GetMember(name, GetBindingFlags(obj,name));
			if (members.Length == 1)
			{
				return (MethodBase)members[0];
			}
			else
			{
				return null;
			}
		}
		private static BindingFlags GetBindingFlags(object obj,string name)
		{
			if (name==".ctor" || obj != null)
			{
				return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			}
			else
			{
				return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			}
		}
	    public override ICollection<Map> Keys
	    {
	        get
	        {
				if (overloadedMethods != null)
				{
					return overloadedMethods.Keys;
				}
				else
				{
					return new List<Map>();
				}
	        }
	    }
	    protected override Map Get(Map key)
	    {
			if (overloadedMethods == null)
			{
				return null;
			}
			else
			{
				MethodOverload value;
				overloadedMethods.TryGetValue(key, out value);
				return value;
			}
	    }
		protected override Map CopyData()
		{
			return new Method(overloadedMethods, method, obj, type);
		}
	    protected override void Set(Map key, Map val)
	    {
	        throw new ApplicationException("Cannot set key in Method.");
	    }
	}
	public class MethodOverload : MethodImplementation
	{
		public MethodOverload(MethodBase method, object obj, Type type)
			: base(method, obj, type)
		{
		}
		// is that correct?
	    protected override Map CopyData()
	    {
	        return new MethodOverload(this.method, this.obj, this.type);
	    }
	    public override ICollection<Map> Keys
	    {
	        get
	        {
	            return new List<Map>();
	        }
	    }
	    protected override Map Get(Map key)
	    {
	        return null;
	    }
	    protected override void Set(Map key, Map val)
	    {
	        throw new ApplicationException("Cannot set key in MethodOverload");
	    }
		public override bool Equals(object toCompare)
		{
			if (toCompare is MethodOverload)
			{
				MethodOverload Method = (MethodOverload)toCompare;
				if (Method.obj == obj && Method.method.Name.Equals(method.Name) && Method.type.Equals(type))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		// why overriden here?
		// and not in MethodImplementation
	    public override int GetHashCode()
	    {
	        unchecked
	        {
	            int hash = method.Name.GetHashCode() * type.GetHashCode();
	            if (obj != null)
	            {
	                hash = hash * obj.GetHashCode();
	            }
	            return hash;
	        }
	    }
	}
	public class TypeMap: DotNetMap
	{
		public TypeMap(Type targetType)
			: base(null, targetType)
		{
		}
		public override bool ContainsKey(Map key)
		{
			return Get(key) != null || base.ContainsKey(key);
		}
		protected override Map Get(Map key)
		{
			// maybe generic types should be a separate class
			if (type.IsGenericTypeDefinition)
			{
				List<Type> types=new List<Type>();
				if (type.GetGenericArguments().Length == 1)
				{
					types.Add(((TypeMap)key).Type);
				}
				else
				{
					foreach (Map map in key.Array)
					{
						types.Add(((TypeMap)map).type);
					}
				}
				return new TypeMap(type.MakeGenericType(types.ToArray()));
			}
			else if (type == typeof(Array) && key is TypeMap)
			{
				return new TypeMap(((TypeMap)key).Type.MakeArrayType());
			}
			else if(base.Get(key)!=null)
			{
				return base.Get(key);
			}
			else
			{
				return this.Constructor[key];
			}
		}
		public Type Type
		{
			get
			{
				return type;
			}
		}
		public override int GetHashCode()
		{
			return 1;
		}
		public override bool Equals(object obj)
		{
			return obj is TypeMap && ((TypeMap)obj).Type == this.type;
		}
		protected override Map CopyData()
		{
			return new TypeMap(this.type);
		}
		private Method Constructor
		{
			get
			{
				return new Method(type);
			}
		}
		public override Map Call(Map argument)
		{
			return Constructor.Call(argument);
		}
	}
	public class ObjectMap: DotNetMap
	{
		public object Object
		{
			get
			{
				return obj;
			}
		}
		public ObjectMap(string text)
			: this(text, text.GetType())
		{
		}
		public ObjectMap(object target, Type type)
			: base(target, type)
		{
		}
		public ObjectMap(object target):base(target,target.GetType())
		{
		}
		public override string ToString()
		{
			return obj.ToString();
		}
		protected override Map CopyData()
		{
			return new ObjectMap(obj);
		}
	}
	public class EmptyStrategy : MapStrategy
	{
		public override bool Equal(MapStrategy strategy)
		{
			return strategy.Count == 0;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return new List<Map>(0);
			}
		}
		public override Map CopyData()
		{
			return new StrategyMap(new EmptyStrategy());
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsNumber)
			{
				if (key.Equals(Map.Empty) && val.IsNumber)
				{
					Panic(key, val, new NumberStrategy(0));
				}
				else
				{
					// that is not logical, maybe ListStrategy is not suitable for this, only if key==1
					Panic(key, val, new ListStrategy());
				}
			}
			else
			{
				Panic(key, val);
			}
		}
		public override Map Get(Map key)
		{
			return null;
		}
	}
	public class NumberStrategy : DataStrategy<Number>
	{
		public NumberStrategy(Number number)
		{
			this.data = new Number(number);
		}
		public override Map Get(Map key)
		{
			if (ContainsKey(key))
			{
				if (key.Equals(Map.Empty))
				{
					return data - 1;
				}
				else if(key.Equals(Numbers.Negative))
				{
					return Map.Empty;
				}
				else if (key.Equals(Numbers.Denominator))
				{
					return new StrategyMap(new Number(data.Denominator));
				}
				else
				{
					throw new ApplicationException("Error.");
				}
			}
			else
			{
				return null;
			}
		}
		public override void Set(Map key, Map value)
		{
			if (key.Equals(Map.Empty) && value.IsNumber)
			{
				this.data = value.GetNumber() + 1;
			}
			else if (key.Equals(Numbers.Negative) && value.Equals(Map.Empty) && data!=0)
			{
				if (data > 0)
				{
					data = 0 - data;
				}
			}
			else if (key.Equals(Numbers.Denominator) && value.IsNumber)
			{
				this.data = new Number(data.Numerator, value.GetNumber().GetInt32());
			}
			else
			{
				Panic(key, value);
			}
		}

		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				if (data != 0)
				{
					keys.Add(Map.Empty);
				}
				if (data < 0)
				{
					keys.Add(Numbers.Negative);
				}
				if (data.Denominator != 1.0d)
				{
					keys.Add(Numbers.Denominator);
				}
				return keys;
			}
		}
		public override Map CopyData()
		{
			return new StrategyMap(new NumberStrategy(data));
		}
		public override bool IsNumber
		{
			get
			{
				return true;
			}
		}
		// is data really useful?
		public override Number GetNumber()
		{
			return data;
		}
		// ???
		protected override bool SameEqual(Number otherData)
		{
			return otherData == data;
		}
	}
	public class ListStrategy : DataStrategy<List<Map>>
	{

		public ListStrategy()
		{
			this.data = new List<Map>();
		}
		public ListStrategy(string text)
		{
			this.data = new List<Map>(text.Length);
			foreach (char c in text)
			{
				this.data.Add(new StrategyMap(c));
			}
		}
		public ListStrategy(ListStrategy original)
		{
			this.data = new List<Map>(original.data);
		}
		public override Map Get(Map key)
		{
			Map value = null;
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= data.Count)
				{
					value = data[integer - 1];
				}
			}
			return value;
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= data.Count)
				{
					data[integer - 1] = val;
				}
				else if (integer == data.Count + 1)
				{
					data.Add(val);
				}
				else
				{
					Panic(key, val);
				}
			}
			else
			{
				Panic(key, val);
			}
		}

		public override int Count
		{
			get
			{
				return data.Count;
			}
		}
		public override List<Map> Array
		{
			get
			{
				return this.data;
			}
		}

		public override int GetArrayCount()
		{
			return this.data.Count;
		}
		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsNumber)
			{
				Number integer = key.GetNumber();
				if (integer >= 1 && integer <= data.Count)
				{
					containsKey = true;
				}
				else
				{
					containsKey = false;
				}
			}
			else
			{
				containsKey = false;
			}
			return containsKey;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				int counter = 1;
				foreach (Map value in data)
				{
					keys.Add(new StrategyMap(counter));
					counter++;
				}
				return keys;
			}
		}
		public override Map CopyData()
		{
			// refactor, combine with DictionaryStrategy?
			StrategyMap copy = new StrategyMap(new ListStrategy());
			foreach(KeyValuePair<Map,Map> pair in this.map)
			{
				copy[pair.Key] = pair.Value;
			}
			return copy;
		}
		public override void AppendRange(Map array)
		{
			foreach (Map map in array.Array)
			{
				this.data.Add(map.Copy());
			}
		}
		protected override bool SameEqual(List<Map> otherData)
		{
			bool equal;
			if (data.Count == otherData.Count)
			{
				equal = true;
				for (int i = 0; i < data.Count; i++)
				{
					if (!this.data[i].Equals(otherData[i]))
					{
						equal = false;
						break;
					}
				}
			}
			else
			{
				equal = false;
			}
			return equal;
		}
	}
	public class DictionaryStrategy:DataStrategy<Dictionary<Map,Map>>
	{
		public DictionaryStrategy():this(2)
		{
		}
		public DictionaryStrategy(int Count)
		{
			this.data = new Dictionary<Map, Map>(Count);
		}
		public DictionaryStrategy(Dictionary<Map, Map> data)
		{
			this.data = data;
		}
		public override Map Get(Map key)
		{
			Map val;
			data.TryGetValue(key, out val);
			return val;
		}
		public override void Set(Map key, Map value)
		{
			data[key] = value;
		}
		public override bool ContainsKey(Map key)
		{
			return data.ContainsKey(key);
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return data.Keys;
			}
		}
		public override int Count
		{
			get
			{
				return data.Count;
			}
		}
		// ??
		public override Map CopyData()
		{
			StrategyMap copy = new StrategyMap();
			foreach (KeyValuePair<Map, Map> pair in this.map)
			{
				copy[pair.Key] = pair.Value;
			}
			return copy;
		}
		// rename
		protected override bool SameEqual(Dictionary<Map, Map> otherData)
		{
			bool equal;
			if (data.Count == otherData.Count)
			{
				equal = true;
				foreach (KeyValuePair<Map, Map> pair in data)
				{
					Map value;
					otherData.TryGetValue(pair.Key, out value);
					if (!pair.Value.Equals(value))
					{
						equal = false;
						break;
					}
				}
			}
			else
			{
				equal = false;
			}
			return equal;
		}
	}
	//public class CloneStrategy : DataStrategy<MapStrategy>
	//{
	//    // always use CloneStrategy, only have logic in one place, too complicated to use
	//    public CloneStrategy(MapStrategy original)
	//    {
	//        this.data = original;
	//    }
	//    public override List<Map> Array
	//    {
	//        get
	//        {
	//            return data.Array;
	//        }
	//    }
	//    public override bool ContainsKey(Map key)
	//    {
	//        return data.ContainsKey(key);
	//    }
	//    public override int Count
	//    {
	//        get
	//        {
	//            return data.Count;
	//        }
	//    }
	//    public override Map CopyData()
	//    {
	//        MapStrategy clone = new CloneStrategy(this.data);
	//        map.Strategy = new CloneStrategy(this.data);
	//        return new StrategyMap(clone);

	//    }
	//    protected override bool SameEqual(MapStrategy otherData)
	//    {
	//        return Object.ReferenceEquals(data, otherData) || otherData.Equal(this.data);
	//    }
	//    public override int GetHashCode()
	//    {
	//        return data.GetHashCode();
	//    }
	//    public override Integer GetInteger()
	//    {
	//        return data.GetInteger();
	//    }
	//    public override string GetString()
	//    {
	//        return data.GetString();
	//    }
	//    public override bool IsInteger
	//    {
	//        get
	//        {
	//            return data.IsInteger;
	//        }
	//    }
	//    public override bool IsString
	//    {
	//        get
	//        {
	//            return data.IsString;
	//        }
	//    }
	//    public override ICollection<Map> Keys
	//    {
	//        get
	//        {
	//            return data.Keys;
	//        }
	//    }
	//    public override Map Get(Map key)
	//    {
	//        return data.Get(key);
	//    }
	//    public override void Set(Map key, Map value)
	//    {
	//        Panic(key, value);
	//    }
	//}

	public abstract class MapStrategy
	{
		public StrategyMap map;

		public abstract void Set(Map key, Map val);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}

		public virtual int GetArrayCount()
		{
			return map.GetArrayCountDefault();
		}
		public virtual void AppendRange(Map array)
		{
			map.AppendRangeDefault(array);
		}



		public abstract Map CopyData();
		//public virtual Map CopyDataShallow()
		//{
		//    StrategyMap copy;
		//    MapStrategy strategy = (MapStrategy)this.CopyData();
		//    copy = new StrategyMap(strategy);
		//    strategy.map = copy;
		//    return copy;
		//}

		// refactor
		public void Panic(MapStrategy newStrategy)
		{
			map.Strategy = newStrategy;
			map.InitFromStrategy(this);
		}

		protected void Panic(Map key, Map val)
		{
			Panic(key, val, new DictionaryStrategy());
		}
		protected void Panic(Map key, Map val, MapStrategy newStrategy)
		{
			Panic(newStrategy);
			map.Strategy.Set(key, val); // why do it like this? this wont assign the parent, which is problematic!!!
		}
		public virtual bool IsNumber
		{
			get
			{
				return map.IsNumberDefault;
			}
		}
		public virtual bool IsString
		{
			get
			{
				return map.IsStringDefault;
			}
		}
		public virtual string GetString()
		{
			return map.GetStringDefault();
		}
		public virtual Number GetNumber()
		{
			return map.GetNumberDefault();
		}
		public abstract ICollection<Map> Keys
		{
			get;
		}
		public virtual List<Map> Array
		{
			get
			{
				List<Map> array = new List<Map>();
				for (int i = 1; this.ContainsKey(i); i++)
				{
					array.Add(this.Get(i));
				}
				return array;
			}
		}
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		public abstract bool Equal(MapStrategy strategy);

		public virtual bool EqualDefault(MapStrategy strategy)
		{
			if (Object.ReferenceEquals(strategy, this))
			{
				return true;
			}
			else if (((MapStrategy)strategy).Count != this.Count)
			{
				return false;
			}
			else
			{
				bool isEqual = true;
				foreach (Map key in this.Keys)
				{
					Map otherValue = strategy.Get(key);
					Map thisValue = Get(key);
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue))
					{
						isEqual = false;
					}
				}
				return isEqual;
			}
		}
	}
	public abstract class DataStrategy<T> : MapStrategy
	{
		public T data;
		public override bool Equal(MapStrategy strategy)
		{
			bool equal;
			if (strategy is DataStrategy<T>)
			{
				equal = SameEqual(((DataStrategy<T>)strategy).data);
			}
			else
			{
				equal = EqualDefault(strategy);
			}
			return equal;
		}
		// rename
		protected abstract bool SameEqual(T otherData);
	}
	public class Event:Map
	{
		Type type;
		object obj;
		EventInfo eventInfo;
		public Event(EventInfo eventInfo,object obj,Type type)
		{
			this.eventInfo=eventInfo;
			this.obj=obj;
			this.type=type;
		}
		public override Map Call(Map argument)
		{
			// binding flags arent really correct, should be different for static and instance events, combine with methodImplementation
			Delegate eventDelegate = (Delegate)type.GetField(eventInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(obj);
			if (eventDelegate != null)
			{
				List<object> arguments = new List<object>();
				ParameterInfo[] parameters = eventDelegate.Method.GetParameters();
				if (parameters.Length == 2)
				{
					arguments.Add(Transform.TryToDotNet(argument, parameters[1].ParameterType));
				}
				else
				{
					for (int i = 1; i < parameters.Length; i++)
					{
						arguments.Add(Transform.TryToDotNet(argument[i], parameters[i].ParameterType));
					}
				}
				return new ObjectMap(eventDelegate.DynamicInvoke(arguments.ToArray()));
			}
			else
			{
				return null;
			}
		}
		protected override Map Get(Map key)
		{
			Map val;
			if (key.Equals(DotNet.Add))
			{
				val = new Method(eventInfo.GetAddMethod().Name, obj, type);
			}
			else
			{
				val = null;
			}
			return val;
		}
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot assign in event " + eventInfo.Name + ".");
		}

		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				if (eventInfo.GetAddMethod() != null)
				{
					keys.Add(DotNet.Add);
				}

				return keys;
			}
		}
		protected override Map CopyData()
		{
			return new Event(eventInfo, obj, type);
		}
	}
	public class Property:Map
	{
		object obj;
		Type type;
		PropertyInfo property;
		public Property(PropertyInfo property,object obj,Type type)
		{
			this.property=property;
			this.obj=obj;
			this.type=type;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				if(property.GetGetMethod()!=null)
				{
					keys.Add(DotNet.Get);
				}
				if(property.GetSetMethod()!=null)
				{
					keys.Add(DotNet.Set);
				}
				return keys;
			}
		}
		protected override Map Get(Map key)
		{
			if(key.Equals(DotNet.Get))
			{
				return new Method(property.GetGetMethod().Name,obj,type);
			}
			else if(key.Equals(DotNet.Set))
			{
				return new Method(property.GetSetMethod().Name,obj,type);
			}
			else
			{
				return null;
			}
		}
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot assign in property.");
		}
		protected override Map CopyData()
		{
			return new Property(property, obj, type);
		}
	}
	public abstract class DotNetMap: Map, ISerializeEnumerableSpecial
	{
		// rename NonSerialized
		[NonSerialized]
		public object obj;
		[NonSerialized]
		public Type type;
		private BindingFlags bindingFlags;

		public DotNetMap(object obj, Type type)
		{
			// combine this with MethodImplementation, maybe add a Reflection class
			if (obj == null)
			{
				this.bindingFlags = BindingFlags.Public | BindingFlags.Static;
			}
			else
			{
				this.bindingFlags = BindingFlags.Public | BindingFlags.Instance;
			}
			this.obj = obj;
			this.type = type;
		}
		protected override Map Get(Map key)
		{
			if (key.IsString)
			{
				string memberName = key.GetString();
				MemberInfo[] foundMembers = type.GetMember(memberName, bindingFlags);
				if (foundMembers.Length != 0)
				{
					MemberInfo member = foundMembers[0];
					if (member is MethodBase)
					{
						return new Method(memberName, obj, type);
					}
					else if (member is PropertyInfo)
					{
						return new Property(type.GetProperty(memberName), this.obj, type);
					}
					else if (member is FieldInfo)
					{
						return Transform.ToMeta(type.GetField(memberName).GetValue(obj));
					}
					else if (member is EventInfo)
					{
						return new Event(((EventInfo)member), obj, type);
					}
					else if (member is Type)
					{
						return new TypeMap((Type)member);
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}
		protected override void Set(Map key, Map value)
		{
			string fieldName = key.GetString();
			FieldInfo field = type.GetField(fieldName, bindingFlags);
			if (field!=null)
			{
				field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
			}
			else
			{
				throw new ApplicationException("Field " + fieldName + " does not exist.");
			}
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				foreach (MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					keys.Add(new StrategyMap(member.Name));
				}
				return keys;
			}
		}
		public override bool IsString
		{
			get
			{
				return false;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}

		public override string Serialize()
		{
			return obj != null ? this.obj.ToString() : this.type.ToString();
		}
		public string Serialize(string indent,string[] functions)
		{
			return indent;
		}
		public Delegate CreateEventDelegate(string name,Map code)
		{
			EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
				BindingFlags.Static|BindingFlags.Instance);
			Delegate eventDelegate=Transform.CreateDelegateFromCode(eventInfo.EventHandlerType,code);
			return eventDelegate;
		}
	}
	public interface ISerializeEnumerableSpecial
	{
		string Serialize();
	}
	public class TestAttribute:Attribute
	{
		public TestAttribute():this(1)
		{
		}
		public TestAttribute(int level)
		{
			this.level = level;
		}
		private int level;
		public int Level
		{
			get
			{
				return level;
			}
		}
	}
	// rename
	[AttributeUsage(AttributeTargets.Property)]
	public class SerializeAttribute : Attribute
	{
		public SerializeAttribute()
			: this(1)
		{
		}
		public SerializeAttribute(int level)
		{
			this.level = level;
		}
		private int level;
		public int Level
		{
			get
			{
				return level;
			}
		}
	}
	public abstract class TestRunner
	{
		protected abstract string TestDirectory
		{
			get;
		}
		public abstract class Test
		{
			public abstract object GetResult(out int level);
		}
		public void Run()
		{
			bool allTestsSucessful = true;
			foreach(Type testType in this.GetType().GetNestedTypes())
			{
				if (testType.IsSubclassOf(typeof(Test)))
				{
					Test test=(Test)testType.GetConstructor(new Type[]{}).Invoke(null);
					int level;
					Console.Write(testType.Name + "...");


					DateTime startTime = DateTime.Now;
					object result=test.GetResult(out level);
					TimeSpan duration = DateTime.Now - startTime;


					string testDirectory = Path.Combine(TestDirectory, testType.Name);
					
					string resultPath = Path.Combine(testDirectory, "result.txt");
					string checkPath = Path.Combine(testDirectory, "check.txt");

					Directory.CreateDirectory(testDirectory);
					if (!File.Exists(checkPath))
					{
						File.Create(checkPath).Close();
					}

					StringBuilder stringBuilder = new StringBuilder();
					Serialize(result, "", stringBuilder, level);

					File.WriteAllText(resultPath, stringBuilder.ToString(), Encoding.Default);
					string successText;
					if (!File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath)))
					{
						successText = "failed";
						allTestsSucessful = false;
					}
					else
					{
						successText = "succeeded";
					}
					Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
				}
			}
			if (!allTestsSucessful)
			{
				Console.ReadLine();
			}
		}
		public const char indentationChar = '\t';

		private bool UseToStringMethod(Type type)
		{
			return (!type.IsValueType || type.IsPrimitive )
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly()
				&& type.GetMethod(
					"ToString",
					BindingFlags.Public|BindingFlags.DeclaredOnly|BindingFlags.Instance,
					null,
					new Type[]{},
					new ParameterModifier[] { })!=null;
		}
		private bool UseProperty(PropertyInfo property,int level)
		{
			object[] attributes=property.GetCustomAttributes(typeof(SerializeAttribute), false);

			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public void Serialize(object obj,string indent,StringBuilder builder,int level) 
		{
			if(obj == null) 
			{
				builder.Append(indent+"null\n");
			}
			else if (UseToStringMethod(obj.GetType()))
			{
				builder.Append(indent+"\""+obj.ToString()+"\""+"\n");
			}
			else
			{
				foreach (PropertyInfo property in obj.GetType().GetProperties())
				{
					if(UseProperty((PropertyInfo)property,level))
					{
						object val=property.GetValue(obj, null);
						builder.Append(indent + property.Name);
						if(val!=null)
						{
							builder.Append(" ("+val.GetType().Name+")");
						}
						builder.Append(":\n");
						Serialize(val,indent+indentationChar,builder,level);
					}
				}
				string specialEnumerableSerializationText;
				if (obj is ISerializeEnumerableSpecial && (specialEnumerableSerializationText = ((ISerializeEnumerableSpecial)obj).Serialize()) != null)
				{
					builder.Append(indent + specialEnumerableSerializationText + "\n");
				}
				else if (obj is System.Collections.IEnumerable)
				{
					foreach (object entry in (System.Collections.IEnumerable)obj)
					{
						builder.Append(indent + "Entry (" + entry.GetType().Name + ")\n");
						Serialize(entry, indent + indentationChar, builder, level);
					}
				}
			}
		}
	}
	public class SourcePosition
	{
		private int line;
		private int column;
		public SourcePosition(int line,int column)
		{
			this.line=line;
			this.column=column;

		}
		[Serialize]
		public int Line
		{
			get
			{
				return line;
			}
			set
			{
				line=value;
			}
		}
		[Serialize]
		public int Column
		{
			get
			{
				return column;
			}
			set
			{
				column=value;
			}
		}
	}
	public class Extent
	{
		private SourcePosition start;
		private SourcePosition end;
		public Extent(SourcePosition start, SourcePosition end, string fileName)
		{
			this.start = start;
			this.end = end;
			this.fileName = fileName;
		}
		private string fileName;
		public Extent(int startLine, int startColumn, int endLine, int endColumn, string fileName)
			: this(new SourcePosition(startLine, startColumn), new SourcePosition(endLine, endColumn), fileName)
		{
		}
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		[Serialize]
		public SourcePosition Start
		{
			get
			{
				return start;
			}
		}
		[Serialize]
		public SourcePosition End
		{
			get
			{
				return end;
			}
		}
	}
	public class Syntax
	{
		static Syntax()
		{
			List<char> list = new List<char>(Syntax.lookupStringForbidden);
			list.AddRange(Syntax.lookupStringFirstForbiddenAdditional);
			Syntax.lookupStringFirstForbidden = list.ToArray();
		}
		public const char callStart = '(';
		public const char callEnd = ')';
		public const char root = '/';
		public const char search='$';
		public const string current = "current";
		public const char negative='-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char indentation = '\t';
		public const char unixNewLine = '\n';
		public const string windowsNewLine = "\r\n";
		public const char function = '|';
		public const char shortFunction = ':';
		// introduce stringForbidden???
		public const char @string = '\"';
		public static char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public const char lookupStart = '[';
		public const char lookupEnd = ']';
		public static char[] lookupStringForbidden = new char[] { call, indentation, '\r', '\n', statement, select, stringEscape,shortFunction, function, @string, lookupStart, lookupEnd, emptyMap, search, root, callStart, callEnd};

		// remove???
		public static char[] lookupStringFirstForbiddenAdditional = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char[] lookupStringFirstForbidden;
		public const char emptyMap = '0';
		public const char call = ' ';
		public const char select = '.';

		public const char stringEscape = '\'';
		public const char statement = '=';
		public const char space = ' ';
		public const char tab = '\t';
	}
	public delegate Map ParseFunction(Parser parser, out bool matched);


	// refactor completely
	public class Parser
	{
		private bool negative = false;
		private string Rest
		{
			get
			{
				return text.Substring(index);
			}
		}
		public string text;
		public int index;
		public string file;

		private int line = 1;
		public string File
		{
			get
			{
				return file;
			}
		}
		public int Line
		{
			get
			{
				return line;
			}
		}
		private int column = 1;
		public int Column
		{
			get
			{
				return column;
			}
		}
		private char Look()
		{
			return Look(0);
		}
		private char Look(int lookahead)
		{
			char character;
			int i = index + lookahead;
			if (i < text.Length)
			{
				character = text[index + lookahead];
			}
			else
			{
				character = Syntax.endOfFile;
			}
			return character;
		}
		private PersistantPosition position;
		public Parser(string text, string filePath,PersistantPosition position)
		{
			this.index = 0;
			this.text = text;
			this.file = filePath;
			this.position = position;
		}
		public bool isStartOfFile = true;
		public int indentationCount = -1;
		public abstract class Rule
		{
			public Map Match(Parser parser, out bool matched)
			{
				Extent extent = new Extent(parser.Line, parser.Column, 0, 0, parser.file);
				// use an extent here, some sort of, maybe clone things instead of creating them all the time
				int oldIndex = parser.index;
				int oldLine = parser.line;
				int oldColumn = parser.column;
				Map result = MatchImplementation(parser, out matched);

				if (!matched)
				{
					parser.index = oldIndex;
					parser.line = oldLine;
					parser.column = oldColumn;
				}
				else
				{
					extent.End.Line = parser.Line;
					extent.End.Column = parser.Column;
					if (result != null)
					{
						result.Extent = extent;
					}
				}
				return result;
			}
			protected abstract Map MatchImplementation(Parser parser, out bool match);
		}
		public abstract class CharacterRule : Rule
		{
			public CharacterRule(char[] chars)
			{
				this.characters = chars;
			}
			protected char[] characters;
			protected abstract bool MatchCharacer(char c);
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map result;
				char character = parser.Look();
				if (MatchCharacer(character))
				{
					result = character;
					matched = true;
					parser.index++;
					parser.column++;
					if (character == Syntax.unixNewLine)
					{
						parser.line++;
						parser.column = 1;
					}
				}
				else
					{
					matched = false;
					result = null;
				}
				return result;
			}
		}
		public class Character : CharacterRule
		{
			public Character(params char[] characters)
				: base(characters)
			{
			}
			protected override bool MatchCharacer(char c)
			{
				if (c == Syntax.fraction)
				{
				}
				return c.ToString().IndexOfAny(characters) != -1 && c != Syntax.endOfFile;
			}
		}
		// refactor, remove
		public class CharacterExcept : CharacterRule
		{
			public CharacterExcept(params char[] characters)
				: base(characters)
			{
			}
			protected override bool MatchCharacer(char c)
			{
				if (characters[0] == Syntax.function)
				{
				}
				return c.ToString().IndexOfAny(characters) == -1 && c != Syntax.endOfFile;
			}
		}
		public delegate void PrePostDelegate(Parser parser);
		public class PrePost : Rule
		{
			private PrePostDelegate pre;
			private PrePostDelegate post;
			private Rule rule;
			public PrePost(PrePostDelegate pre, Rule rule, PrePostDelegate post)
			{
				this.pre = pre;
				this.rule = rule;
				this.post = post;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				pre(parser);
				if (rule == null)
				{
				}
				Map result = rule.Match(parser, out matched);
				post(parser);
				return result;
			}
		}
		public class StringRule : Rule
		{
			private string text;
			public StringRule(string text)
			{
				this.text = text;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				List<Action> actions = new List<Action>();
				foreach (char c in text)
				{
					actions.Add(new Match(new Character(c)));
				}
				return new Sequence(actions.ToArray()).Match(parser, out matched);
			}
		}
		// refactor, remove
		public class CustomRule : Rule
		{
			private ParseFunction parseFunction;
			public CustomRule(ParseFunction parseFunction)
			{
				this.parseFunction = parseFunction;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				return parseFunction(parser, out matched);
			}
		}
		public delegate Rule RuleFunction();
		public class DelayedRule : Rule
		{
			private RuleFunction ruleFunction;
			private Rule rule;
			public DelayedRule(RuleFunction ruleFunction)
			{
				this.ruleFunction = ruleFunction;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				if (rule == null)
				{
					rule = ruleFunction();
				}
				return rule.Match(parser, out matched);
			}
		}
		public class Alternatives : Rule
		{
			private Rule[] cases;
			public Alternatives(params Rule[] cases)
			{
				this.cases = cases;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map result = null;
				matched = false;
				foreach (Rule expression in cases)
				{
					if (expression == null)
					{
					}
					result = (Map)expression.Match(parser, out matched);
					if (matched)
					{
						break;
					}
				}
				return result;
			}
		}
		public class FlattenRule : Rule
		{
			private Rule rule;
			public FlattenRule(Rule rule)
			{
				this.rule = rule;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map map = rule.Match(parser, out match);
				Map result;
				if (match)
				{
					result = Library.Join(map);
				}
				else
				{
					result = null;
				}
				return result;
			}
		}
		public class Not : Rule
		{
			private Rule rule;
			public Not(Rule rule)
			{
				this.rule = rule;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				bool matched;
				Map result = rule.Match(parser, out matched);
				match = !matched;
				return result;
			}
		}
		public Map CreateMap(params Map[] maps)
		{
			return new StrategyMap(maps);
		}
		public Map CreateMap()
		{
			return CreateMap(new EmptyStrategy());
		}
		public Map CreateMap(MapStrategy strategy)
		{
			return new StrategyMap(strategy);
		}
		public class Sequence : Rule
		{
			private Action[] actions;
			public Sequence(params Action[] rules)
			{
				this.actions = rules;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map result = parser.CreateMap();
				bool success = true;
				foreach (Action action in actions)
				{
					if (action == null)
					{
					}
					// refactor
					bool matched = action.Execute(parser, ref result);
					if (!matched)
					{
						success = false;
						break;
					}
				}
				if (!success)
				{
					match = false;
					return null;
				}
				else
				{
					match = true;
					return result;
				}
			}
		}
		public class LiteralRule : Rule
		{
			private Map literal;
			// should convert literal into a FileMap
			public LiteralRule(Map literal)
			{
				this.literal = literal;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				matched = true;
				return literal;
			}
		}
		public class ZeroOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = parser.CreateMap(new ListStrategy());
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
				}
				matched = true;
				return list;
			}
			private Action action;
			public ZeroOrMore(Action action)
			{
				this.action = action;
			}
		}
		public class N : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = parser.CreateMap(new ListStrategy());
				int counter = 0;
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
					else
					{
						counter++;
					}
				}
				matched = counter == n;
				return list;
			}
			private Action action;
			private int n;
			public N(int n, Action action)
			{
				this.n = n;
				this.action = action;
			}
		}
		public class OneOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = parser.CreateMap(new ListStrategy());
				matched = false;
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
					matched = true;
				}
				return list;
			}
			private Action action;
			public OneOrMore(Action action)
			{
				this.action = action;
			}
		}
		public class Optional : Rule
		{
			private Rule rule;
			public Optional(Rule rule)
			{
				this.rule = rule;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map matched = rule.Match(parser, out match);
				if (matched == null)
				{
					match = true;
					return null;
				}
				else
				{
					match = true;
					return matched;
				}
			}
		}
		public class Nothing : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				matched = true;
				return null;
			}
		}
		public abstract class Action
		{
			protected Rule rule;
			public Action(Rule rule)
			{
				if (rule == null)
				{
				}
				this.rule = rule;
			}
			public bool Execute(Parser parser, ref Map result)
			{
				bool matched;
				if (rule == null)
				{
				}
				Map map = rule.Match(parser, out matched);
				if (matched)
				{
					ExecuteImplementation(parser, map, ref result);
				}
				return matched;
			}
			protected abstract void ExecuteImplementation(Parser parser, Map map, ref Map result);
		}
		public class OptionalAssignment : Action
		{
			private Map key;
			public OptionalAssignment(Map key, Rule rule)
				: base(rule)
			{
				this.key = key;
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				if (map != null)
				{
					result[key] = map;

				}
			}
		}
		public class Autokey : Action
		{
			public Autokey(Rule rule)
				: base(rule)
			{
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				result.Append(map);
			}
		}
		public class Assignment : Action
		{
			private Map key;
			public Assignment(Map key, Rule rule)
				: base(rule)
			{
				this.key = key;
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				result[key] = map;
			}
		}
		public class Match : Action
		{

			public Match(Rule rule)
				: base(rule)
			{
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
			}
		}
		public class ReferenceAssignment : Action
		{
			public ReferenceAssignment(Rule rule)
				: base(rule)
			{
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				result = map;
			}
		}
		public class Appending : Action
		{
			public Appending(Rule rule)
				: base(rule)
			{
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				foreach (Map m in map.Array)
				{
					result.Append(m);
				}
			}
		}
		public class Merge : Action
		{
			public Merge(Rule rule)
				: base(rule)
			{
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				result = Library.Merge(new StrategyMap(1, result, 2, map));
			}
		}
		public class Do : Action
		{
			private CustomActionDelegate action;
			public Do(Rule rule, CustomActionDelegate action)
				: base(rule)
			{
				this.action = action;
			}
			protected override void ExecuteImplementation(Parser parser, Map map, ref Map result)
			{
				this.action(parser, map, ref result);
			}
		}


		public delegate Map CustomActionDelegate(Parser p, Map map, ref Map result);

		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(EmptyMap, Number, String, Program, Call,ShortFunction, Select);
		});
		public class Data
		{
			public static Rule NewLine = new Alternatives(
						new Character(Syntax.unixNewLine),
						new StringRule(Syntax.windowsNewLine));

			public static Rule EndOfLine =
				new Sequence(
					new Match(new ZeroOrMore(
						new Match(new Alternatives(
							new Character(Syntax.space),
							new Character(Syntax.tab))))),
					new Match(NewLine));

			public static Rule Integer =
				new Sequence(
					new Do(
						new Optional(new Character(Syntax.negative)),
						delegate(Parser p, Map map, ref Map result)
						{
							p.negative = map != null;
							return null;
						}),
					new ReferenceAssignment(
						new Sequence(
							new ReferenceAssignment(
								new OneOrMore(new Do(new Character(Syntax.integer), delegate(Parser p, Map map, ref Map result)
			{
				if (result == null)
				{
					result = p.CreateMap();
				}
				result = result.GetNumber() * 10 + (Number)map.GetNumber().GetInt32() - '0';
				return result;
			}))),
								new Do(
									new Nothing(),
									delegate(Parser p, Map map, ref Map result)
									{
										if (result.GetNumber() > 0 && p.negative)
										{
											result = 0 - result.GetNumber();
										}
										return null;
									}))));
			public static Rule String = new CustomRule(delegate(Parser parser, out bool matched)
			{
				return new Alternatives(
					new Sequence(
						new Match(new Character(Syntax.@string)),
						new ReferenceAssignment(
							new OneOrMore(
								new Autokey(new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0], Syntax.@string)))),
						new Match(new Character(Syntax.@string))),
					new Sequence(
						new Match(new Character(Syntax.@string)),
						new Match(Indentation),
						new ReferenceAssignment(
							new FlattenRule(
								new Sequence(
									new Autokey(StringLine),
									new Autokey(
										new FlattenRule(
											new ZeroOrMore(
												new Autokey(
													new Sequence(
														new Match(EndOfLinePreserve),
														new Match(SameIndentation),
														new ReferenceAssignment(
															new FlattenRule(
																new Sequence(
																	new Autokey(new LiteralRule(Syntax.unixNewLine.ToString())),
																	new Autokey(StringLine)
																	))))))))))),
						new Match(StringDedentation),
						new Match(new Character(Syntax.@string)))).Match(parser, out matched);
			});
			public static Rule Number = new Sequence(
				new ReferenceAssignment(
					Integer),
				new OptionalAssignment(
					Numbers.Denominator,
					new Optional(
						new Sequence(
							new Match(new Character(Syntax.fraction)),
							new ReferenceAssignment(
								Integer)))));
			public static Rule LookupString=new OneOrMore(
				new Autokey(
					new CharacterExcept(
						Syntax.lookupStringForbidden)));


			public static Rule Map = new CustomRule(delegate(Parser parser,out bool matched)
			{
				Indentation.Match(parser,out matched);
				Map map=new StrategyMap();
				if(matched)
				{
					parser.defaultKeys.Push(1);
					map=Entry.Match(parser, out matched);
					//map.Append(Entry.Match(parser, out matched));
					if (matched)
					{
						while(true)
						{
							if (parser.Rest == "")
							{
								break;
							}
							new Alternatives(
								SameIndentation,
								Dedentation).Match(parser,out matched);
							if (matched)
							{
								map = Library.Merge(new StrategyMap(
									1, map,
									2, Entry.Match(parser, out matched)));
								//map.Append(Entry.Match(parser, out matched));
							}
							else
							{
								matched = true;
								break;
							}
							if (parser.Rest == "")
							{
								break;
							}
						}
					}
					parser.defaultKeys.Pop();
				}
				return map;
			});
			public static Rule ShortFunction = new CustomRule(delegate(Parser p, out bool matched)
			{
				Map result = new Sequence(
					new Assignment(
						Code.Function,
						new Sequence(
						new Merge(
							new Sequence(
								new Assignment(
									Code.ParameterName,
									new ZeroOrMore(
										new Autokey(
											new CharacterExcept(
												Syntax.shortFunction,
												Syntax.unixNewLine)))))),
						new Merge(
							new Sequence(
								new Match(
									new Character(Syntax.shortFunction)),
								new ReferenceAssignment(Expression)))))).Match(p, out matched);
				return result;
			});
			public static Rule Value=new Alternatives(
				Map,
				String,
				Number,
				ShortFunction
				);
			private static Rule LookupAnything =
				new Sequence(
					new Match(new Character((Syntax.lookupStart))),
					new ReferenceAssignment(Value),
					new Match(new ZeroOrMore(
						new Match(new Character(Syntax.indentation)))),
					new Match(new Character(Syntax.lookupEnd)));
			// refactor
			public static Rule Entry = new CustomRule(delegate(Parser parser, out bool matched)
			{
				Map result = new StrategyMap();
				if (parser.Rest.StartsWith("commandLine"))
				{
				}
				Map function=Parser.Function.Match(parser, out matched);
				if (matched)
				{
					result[Code.Function] = function[Code.Value][Code.Literal];
				}
				else
				{
					Map key = new Alternatives(LookupString,LookupAnything).Match(parser, out matched);
					//Map key = LookupString.Match(parser, out matched);
					if (matched)
					{
						new StringRule(Syntax.statement.ToString()).Match(parser, out matched);
						if (matched)
						{
							Map value = Value.Match(parser, out matched);
							result[key] = value;

							// i dont understand this
							bool match;
							if (Data.EndOfLine.Match(parser, out match) == null && parser.Look() != Syntax.endOfFile)
							{
								parser.index -= 1;
								if (Data.EndOfLine.Match(parser, out match) == null)
								{
									parser.index -= 1;
									if (Data.EndOfLine.Match(parser, out match) == null)
									{
										parser.index += 2;
										//matched = false;
										//return null;
										throw new SyntaxException("Expected newline.", parser);
									}
									else
									{
										parser.line--;
									}
								}
								else
								{
									parser.line--;
								}
							}
						}
					}
				}
				return result;
			});

			public static Rule Function = new CustomRule(delegate(Parser p, out bool matched)
			{
				Map result=new Sequence(
					new Merge(
						new Sequence(
							new Assignment(
								Code.ParameterName,
								new ZeroOrMore(
								new Autokey(
									new CharacterExcept(
										Syntax.function,
										Syntax.unixNewLine)))))),
					new Merge(
						new Sequence(
							new Match(
								new Character(
									Syntax.function)),
							new ReferenceAssignment(
								Expression)))).Match(p,out matched);
				return result;
			});
			public static Rule File = new Sequence(
				new Match(
					new Optional(
						new Sequence(
							new Match(
								new StringRule("#!")),
							new Match(
								new ZeroOrMore(
									new Match(
										new CharacterExcept(Syntax.unixNewLine)))),
							new Match(EndOfLine)))),
				new ReferenceAssignment(Map));
		}
		public static Rule ExplicitCall=new DelayedRule(delegate()
		{
			return new Sequence(
				new Assignment(
					Code.Call,
					new Sequence(
						new Match(new Character(Syntax.callStart)),
						new Assignment(
							Code.Callable,
							Select),
						new Assignment(
							Code.Argument,
							new Alternatives(
								new Sequence(
									new Match(new Character(Syntax.call)),
									new ReferenceAssignment(Expression)),
								Program)),
						new Match(new Character(Syntax.callEnd)))));
		});

		public static Rule Call = new DelayedRule(delegate()
		{
			return new Sequence(
				new Assignment(
					Code.Call,
						new Sequence(
							new Assignment(
								Code.Callable,
								new Alternatives(
									Select,
									ExplicitCall)),
							new Assignment(
								Code.Argument,
								new Alternatives(
									new Sequence(
										new Match(
											new Alternatives(
												new Character(Syntax.call),
												new Character(Syntax.indentation))),
										new ReferenceAssignment(Expression)),
									Program)))));
		});
		public Stack<int> defaultKeys = new Stack<int>();
		private int escapeCharCount = 0;
		private int GetEscapeCharCount()
		{
			return escapeCharCount;
		}


		// somehow include the newline here   ???
		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			return new StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa, out matched);
		});
		public static Rule Dedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			pa.indentationCount--;
			matched = false;
			return null;
		});

		private static Rule EndOfLinePreserve = new FlattenRule(
			new Sequence(
				// this is a bug, should be preserved
				new Match(new FlattenRule(
					new ZeroOrMore(
							new Autokey(new Alternatives(
								new Character(Syntax.space),
								new Character(Syntax.tab)))))),
				new Autokey(
					new Alternatives(
						new Character(Syntax.unixNewLine),
						new StringRule(Syntax.windowsNewLine)))));


		public static Rule StringDedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			Map map = new Sequence(
				new Match(Data.EndOfLine),
				new Match(new StringRule("".PadLeft(pa.indentationCount - 1, Syntax.indentation)))).Match(pa, out matched);
			// this should be done automatically
			if (matched)
			{
				pa.indentationCount--;
			}
			return map;
		});

		private static Rule Indentation =
			new Alternatives(
				new CustomRule(delegate(Parser p, out bool matched)
		{
			if (p.isStartOfFile)
			{
				p.isStartOfFile = false;
				p.indentationCount++;
				matched = true;
				return null;
			}
			else
			{
				matched = false;
				return null;
			}
		}),
				new Sequence(
					new Match(new Sequence(
						new Match(Data.EndOfLine),
						new Match(new CustomRule(delegate(Parser p, out bool matched)
		{
			return new StringRule("".PadLeft(p.indentationCount + 1, Syntax.indentation)).Match(p, out matched);
		})))),
					new Match(new CustomRule(delegate(Parser p, out bool matched)
		{
			p.indentationCount++;
			matched = true;
			return null;
		}))));

		private static Rule StringLine = new ZeroOrMore(new Autokey(new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0])));


		private static Rule String = new CustomRule(delegate(Parser parser, out bool matched)
		{
			return new Sequence(
					new Assignment(
						Code.Literal,
						Data.String)).Match(parser,out matched);
		});

		public static Rule ShortFunction = new Sequence(
			new Assignment(
				Code.Literal,
				Data.ShortFunction));

		public static Rule Function = new Sequence(
			new Assignment(Code.Key,new LiteralRule(new StrategyMap(1, new StrategyMap(Code.Lookup, new StrategyMap(Code.Literal, Code.Function))))),
			new Assignment(Code.Value,new Sequence(
				new Assignment(Code.Literal,Data.Function))));
		
		private Rule Whitespace =
			new ZeroOrMore(
				new Match(
					new Alternatives(
						new Character(Syntax.tab),
						new Character(Syntax.space))));

		private static Rule EmptyMap = new Sequence(
			new Assignment(Code.Literal, new Sequence(
				new Match(
					new Character(Syntax.emptyMap)),
				new ReferenceAssignment(
					new LiteralRule(Map.Empty)))));

		private static Rule LookupAnything =
			new Sequence(
				new Match(new Character((Syntax.lookupStart))),
				new ReferenceAssignment(Expression),
				new Match(new ZeroOrMore(
					new Match(new Character(Syntax.indentation)))),
				new Match(new Character(Syntax.lookupEnd)));


		private static Rule Number =
			new Sequence(
				new Assignment(
					Code.Literal,
					Data.Number));

		private static Rule LookupString =
			new Sequence(
				new Assignment(
					Code.Literal,
					Data.LookupString));

		private static Rule Current = new Sequence(
			new Match(new StringRule(Syntax.current)),
			new ReferenceAssignment(new LiteralRule(new StrategyMap(Code.Current, Map.Empty))));



		private static Rule Root = new Sequence(
			new Match(new Character(Syntax.root)),
			new ReferenceAssignment(new LiteralRule(new StrategyMap(Code.Root, Map.Empty))));


		private static Rule Lookup =
			new Alternatives(
				Current,
				new Sequence(
					new Assignment(
						Code.Lookup,
						new Alternatives(
							LookupString,
							LookupAnything))));


		private static Rule Search = new Sequence(
			new Assignment(
				Code.Search,
				new Alternatives(
					LookupString,
					LookupAnything)));


		private static Rule Select = new Sequence(
			new Assignment(
				Code.Select,
				new Sequence(
					new Assignment(
						1,
						new Alternatives(
							Root,
							Search,
							Lookup,
							ExplicitCall)),
					new Appending(
						new ZeroOrMore(
							new Autokey(
								new Sequence(
									new Match(new Character(Syntax.select)),
									new ReferenceAssignment(
										Lookup))))))));


		private static Rule KeysSearch = new Sequence(
				new Match(new Character(Syntax.search)),
				new ReferenceAssignment(Search));


		private static Rule Keys = new Sequence(
			new Assignment(
				1,
				new Alternatives(
					KeysSearch,
					Lookup)),
			new Appending(
				new ZeroOrMore(
					new Autokey(
						new Sequence(
							new Match(new Character(Syntax.select)),
							new ReferenceAssignment(
								Lookup))))));

		public static Rule Statement = new Sequence(
			new ReferenceAssignment(
				new Alternatives(
					Function,
					new Alternatives(
						new Sequence(
							new Assignment(
								Code.Key,
								Keys),
							new Match(new Character(Syntax.statement)),
							new Assignment(
								Code.Value,
								Expression)),
						new Sequence(
							new Match(new Optional(
								new Character(Syntax.statement))),
							new Assignment(
								Code.Value,
								Expression),
							new Assignment(
								Code.Key,
								new CustomRule(delegate(Parser p, out bool matched)
		{
			Map map = p.CreateMap(1, p.CreateMap(Code.Lookup, p.CreateMap(Code.Literal, p.defaultKeys.Peek())));
			p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
			matched = true;
			return map;
		})))))),
			new Match(new CustomRule(delegate(Parser p, out bool matched)
		{
			// i dont understand this
			if (Data.EndOfLine.Match(p, out matched) == null && p.Look() != Syntax.endOfFile)
			{
				p.index -= 1;
				if (Data.EndOfLine.Match(p, out matched) == null)
				{
					p.index -= 1;
					if (Data.EndOfLine.Match(p, out matched) == null)
					{
						p.index += 2;
						//matched = false;
						//return null;
						throw new SyntaxException("Expected newline.", p);
					}
					else
					{
						p.line--;
					}
				}
				else
				{
					p.line--;
				}
			}
			matched = true;
			return null;
		})));
		public static Rule Program = new Sequence(
			new Match(Indentation),
			new Assignment(
				Code.Program,
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
					new Sequence(
						new Assignment(
							1,
							Statement),
						new Appending(
							new ZeroOrMore(
								new Autokey(
									new Sequence(
										new Match(new Alternatives(
											SameIndentation,
											Dedentation)),
										new ReferenceAssignment(Statement)))))),
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));
	}
	// refactor
	public class Serialize
	{
		private static Rule EmptyMap = new Literal(Syntax.emptyMap, new Set());
		private static Rule IntegerValue = new CustomSerialize(
			delegate(Map map)
			{
				return map.IsNumber;
			},
			delegate(Map map)
			{
				return map.GetNumber().ToString();
			});

		public static Rule TypeMap = new CustomSerialize(
			delegate(Map map) {return map is TypeMap;},
			delegate(Map map) {return "TypeMap: " + ((TypeMap)map).Type.ToString();});

		// simply return null if you dont match??
		public static Rule ObjectMap = new CustomSerialize(
			delegate(Map map) { return map is ObjectMap; },
			delegate(Map map) { return "ObjectMap: " + ((ObjectMap)map).Object.ToString(); });


		public static Rule MapValue = new CustomRule(delegate(Map asdf, string indent, out bool ma)
		{
			string t;
			t = Syntax.unixNewLine.ToString();
			if (indent == null)
			{
				indent = "";
			}
			else
			{
				indent += Syntax.indentation;
			}
			t+=new Alternatives(
				TypeMap,
				ObjectMap,
				new CustomRule(delegate(Map map, string indentation, out bool matched)
				{
					string text = "";
					foreach (KeyValuePair<Map, Map> entry in map)
					{
						if (entry.Key.Equals(Code.Function) && entry.Value.Count == 1 && (entry.Value.ContainsKey(Code.Call) || entry.Value.ContainsKey(Code.Literal) || entry.Value.ContainsKey(Code.Program) || entry.Value.ContainsKey(Code.Select)))
						{
							bool m;
							text += indentation + Syntax.function + Expression.Match(entry.Value, indentation, out m);
							if (!text.EndsWith(Syntax.unixNewLine.ToString()))
							{
								text += Syntax.unixNewLine;
							}
						}
						else
						{
							text += indentation + Key.Match((Map)entry.Key, indentation, out matched) + Syntax.statement + Value.Match((Map)entry.Value, (indentation), out matched);
							if (!text.EndsWith(Syntax.unixNewLine.ToString()))
							{
								text += Syntax.unixNewLine;
							}
						}
					}
					matched = true;
					return text;
				})).Match(asdf, indent, out ma);
			ma = true;
			return t;
		});


		private static Rule StringValue = new CustomSerialize(
			delegate(Map map)
			{
				return map.IsString;
			},
			delegate(Map map)
			{
				string text = Syntax.@string.ToString();
				string mapString = map.GetString();
				if (mapString.IndexOf(Syntax.@string) == -1 && mapString.IndexOf(Syntax.unixNewLine) == -1)
				{
					text += mapString;
				}
				else
				{
					string[] lines = map.GetString().Split(new string[] { Syntax.unixNewLine.ToString(), Syntax.windowsNewLine }, StringSplitOptions.None);
					for (int i = 0; i < lines.Length; i++)
					{
						text += Syntax.unixNewLine.ToString() + Syntax.indentation + lines[i];
					}
					text += Syntax.unixNewLine.ToString();
				}
				text += Syntax.@string;
				return text;
			});
		private static Rule Value = new Alternatives(EmptyMap, StringValue, IntegerValue, MapValue);


		public static Rule LiteralKey = new OneOrMore(new CharacterExcept(Syntax.lookupStringForbidden));

		private static Rule StringKey = new Alternatives(
			LiteralKey,
			new Enclose(Syntax.lookupStart.ToString(), StringValue, Syntax.lookupEnd.ToString()));

		
		public static Rule Call = new Set(
			new KeyRule(
				Code.Call,
				new Set(
					new KeyRule(
						Code.Callable,
						Expression),
					new KeyRule(
						Code.Argument,
						new Alternatives(
							Program,
							new Enclose(
								Syntax.call.ToString(),
								new Alternatives(
									EmptyMap,
									Expression),
								""))))));

		public static string Statement(Map code, string indentation, ref int autoKeys)
		{
			Map key = code[Code.Key];
			string text;
			if (key.Count == 1 && key[1].ContainsKey(Code.Lookup) && key[1][Code.Lookup].ContainsKey(Code.Literal) && key[1][Code.Lookup][Code.Literal].Equals(Code.Function) && code[Code.Value].ContainsKey(Code.Literal))
			{
				bool matched;
				text = indentation + Syntax.function + Expression.Match(code[Code.Value][Code.Literal], indentation,out matched);
			}
			else
			{
				Map autoKey;
				text = indentation;
				Map value = code[Code.Value];
				if (key.Count == 1 && key[1].ContainsKey(Code.Lookup) && key[1][Code.Lookup].ContainsKey(Code.Literal) && (autoKey = key[1][Code.Lookup][Code.Literal]) != null && autoKey.IsNumber && autoKey.GetNumber() == autoKeys + 1)
				{
					autoKeys++;
					if (value.ContainsKey(Code.Program) && value[Code.Program].Count != 0)
					{
						text += Syntax.statement;
					}
				}
				else
				{
					bool m;
					text += Keys.Match(code[Code.Key], indentation, out m) + Syntax.statement;
				}
				bool matched;
				text += Expression.Match(value, indentation,out matched);
			}
			return text;
		}
		private static Rule SelectImplementation = new CustomRule(delegate(Map code, string indentation, out bool matched)
		{
			string text = Lookup.Match(code[1], indentation,out matched);
			for (int i = 2; code.ContainsKey(i); i++)
			{
				text += Syntax.select + Lookup.Match(code[i], indentation,out matched);
			}
			matched = true;
			return text;
		});
		public static Rule Keys = SelectImplementation;

		public static Rule Select = new Set(
			new KeyRule(
				Code.Select,
				SelectImplementation));

		public static Rule LookupSearchImplementation = new Alternatives(
			new KeyRule(
				Code.Literal,
				Key),
			new Enclose(
				Syntax.lookupStart.ToString(),
				new Alternatives(
					new Enclose(
						"",
						Program,
						new IndentationProduction()),
					Expression),
				Syntax.lookupEnd.ToString()));


		public static Rule Current = new Equal(
			new StrategyMap(
				Code.Current, 
				Map.Empty),
			Syntax.current.ToString());

		public static Rule LiteralProduction = new Set(new KeyRule(Code.Literal, Value));

		public static Rule Lookup = new Alternatives(
				new Alternatives(
					new Set(
						new KeyRule(
							Code.Search,
							LookupSearchImplementation)),
					new Set(
						new KeyRule(
							Code.Lookup,
							LookupSearchImplementation))),
				new Alternatives(
					Current,
					new Set(new KeyRule(Code.Literal, Key)),
					new Enclose(Syntax.lookupStart.ToString(), Expression, Syntax.lookupEnd.ToString())));


		public static Rule Key = new Alternatives(
			StringKey,
			new Enclose(
				Syntax.lookupStart.ToString(),
				new Alternatives(
					EmptyMap,
					IntegerValue,
					TypeMap,
					ObjectMap,
					MapValue),
			Syntax.lookupEnd.ToString()));

		public class OneOrMore:Rule
		{
			private Rule rule;
			public OneOrMore(Rule rule)
			{
				this.rule = rule;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				matched = false;
				string text = "";
				foreach (Map m in map.Array)
				{
					text += rule.Match(m,indentation, out matched);
					if (!matched)
					{
						break;
					}
				}
				return text;
			}
		}
		public class CharacterExcept : Rule
		{
			private char[] chars;
			public CharacterExcept(params char[] chars)
			{
				this.chars = chars;
			}
			public override string Match(Map map,string indentation,out bool matched)
			{
				string text = null;
				matched = false;
				if (Transform.IsIntegerInRange(map, char.MinValue, char.MaxValue))
				{
					char c = Convert.ToChar((int)map.GetNumber().Numerator);
					if (c.ToString().IndexOfAny(chars) == -1)
					{
						matched = true;
						text = c.ToString();
					}
				}
				return text;
			}
		}
		public delegate string CustomRuleDelegate(Map map,string indentation, out bool matched);
		public class CustomRule : Rule
		{
			private CustomRuleDelegate customRule;
			public CustomRule(CustomRuleDelegate customRule)
			{
				this.customRule = customRule;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				return customRule(map,indentation, out matched);
			}
		}
		public delegate Rule DelayedRuleDelegate();

		public class DelayedRule : Rule
		{
			private Rule rule;
			private DelayedRuleDelegate delayedRule;
			public DelayedRule(DelayedRuleDelegate delayedRule)
			{
				this.delayedRule = delayedRule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				if (rule == null)
				{
					rule = delayedRule();
				}
				return rule.Match(map, indentation, out matched);				
			}
		}
		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(Call,EmptyMap, Program, LiteralProduction,Select);
		});

		public class KeyRule : Rule
		{
			public Map key;
			Rule value;
			public KeyRule(Map key, Rule value)
			{
				this.key = key;
				this.value = value;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				string text;
				if (map.ContainsKey(key))
				{
					text = value.Match(map[key],indentation, out matched);
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}
		public class Set:Rule
		{
			private Rule[] rules;
			public Set(params Rule[] rules)
			{
				this.rules = rules;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				string text = "";
				int keyCount = 0;
				matched = true;
				foreach (Rule rule in rules)
				{
					text += rule.Match(map,indentation, out matched);
					if (rule is KeyRule)
					{
						keyCount++;
					}
					if (!matched)
					{
						break;
					}
				}
				matched = map.Count == keyCount && matched;
				return text;
			}
		}
		public static Rule Program = new CustomRule(delegate(Map code, string indentation, out bool matched)
		{
			string text;			
			if (!code.ContainsKey(Code.Program))
			{
			    matched = false;
				text = null;
			}
			else
			{
				code = code[Code.Program];
				text = Syntax.unixNewLine.ToString();
				int autoKeys = 0;
				foreach (Map statement in code.Array)
				{
					text += Statement(statement, indentation + Syntax.indentation, ref autoKeys);
					if (!text.EndsWith(Syntax.unixNewLine.ToString()))
					{
						text += Syntax.unixNewLine;
					}
				}
				matched = true;
			}
			return text;
		});
		public abstract class Production
		{
			public static implicit operator Production(string text)
			{
				return new StringProduction(text);
			}
			public abstract string Get(string indentation);
		}
		public class StringProduction : Production
		{
			private string text;
			public StringProduction(string text)
			{
				this.text = text;
			}
			public override string Get(string indentation)
			{
				return text;
			}
		}
		public class IndentationProduction:Production
		{
			public override string  Get(string indentation)
			{
				return indentation;
			}
		}

		public class Enclose : Rule
		{
			private Production start;
			private Rule rule;
			private Production end;
			public Enclose(Production start, Rule rule, Production end)
			{
				this.start = start;
				this.rule = rule;
				this.end = end;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				string text = rule.Match(map, indentation, out matched);
				if (matched)
				{
					text = start.Get(indentation) + text + end.Get(indentation);
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}
		// combine with Literal?
		public class Equal : Rule
		{
			private Map map;
			private string literal;
			public Equal(Map map, string literal)
			{
				this.map = map;
				this.literal = literal;
			}
			public override string Match(Map m,string indentation, out bool matched)
			{
				string text;
				if (m.Equals(map))
				{
					text = literal;
					matched = true;
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}

		public abstract class Rule
		{
			public abstract string Match(Map map, string indentation, out bool matched);
		}
		public static string ValueFunction(Map val)
		{
			bool matched;
			return Value.Match(val, null, out matched);
		}
		public class Literal : Rule
		{
			private Rule rule;
			private string literal;
			public Literal(char c, Rule rule)
				: this(c.ToString(), rule)
			{
			}
			public Literal(string literal, Rule rule)
			{
				this.literal = literal;
				this.rule = rule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				rule.Match(map, indentation, out matched);
				string text;
				if (matched)
				{
					text = literal;
				}
				else
				{
					text = null;
				}
				return text;
			}
		}
		public class Alternatives : Rule
		{
			private Rule[] rules;
			public Alternatives(params Rule[] rules)
			{
				this.rules = rules;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				string text = null;
				matched = false;
				foreach (Rule rule in rules)
				{
					text = rule.Match(map, indentation, out matched);
					if (matched)
					{
						break;
					}
				}
				return text;
			}
		}
		public delegate bool Matches(Map map);
		public delegate string GetMatch(Map map);
		public class CustomSerialize : Rule
		{
			private Matches matches;
			private GetMatch getMatch;
			public CustomSerialize(Matches matches, GetMatch getMatch)
			{
				this.matches = matches;
				this.getMatch = getMatch;
			}
			private Rule rule;
			public CustomSerialize(Matches matches, Rule rule)
			{
				this.matches = matches;
				this.rule = rule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				matched = matches(map);
				string text;
				if (matched)
				{
					string unIndented;
					if (getMatch != null)
					{
						unIndented = getMatch(map);
					}
					else
					{
						unIndented = rule.Match(map, indentation, out matched);
					}
					string[] lines = unIndented.Split(new string[] { Syntax.unixNewLine.ToString(), Syntax.windowsNewLine }, StringSplitOptions.None);
					text = lines[0];
					for (int i = 1; i < lines.Length; i++)
					{
						text += Syntax.unixNewLine.ToString() + indentation + lines[i];
					}
				}
				else
				{
					text = null;
				}
				return text;
			}
		}
	}
	public class Gac : StrategyMap
	{
		public static readonly StrategyMap gac = new Gac();
		private Gac()
		{
			this["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private bool Load(Map key)
		{
			bool loaded;
			if (strategy.ContainsKey(key))
			{
				loaded = true;
			}
			else
			{
				if (key.IsString)
				{
					string assemblyName = key.GetString();
					Assembly assembly = null;
					try
					{
						assembly = Assembly.LoadWithPartialName(assemblyName);
					}
					catch
					{
					}
					if (assembly != null)
					{
						this[key] = LoadAssembly(assembly);
						loaded = true;
					}
					else
					{
						loaded = false;
					}
				}
				else
				{
					Map version = key["version"];
					Map publicKeyToken = key["publicKeyToken"];
					Map culture = key["culture"];
					Map name = key["name"];
					if (version != null && version.IsString && publicKeyToken != null && publicKeyToken.IsString && culture != null && culture.IsString && name != null && name.IsString)
					{
						Assembly assembly = Assembly.Load(name.GetString() + ",Version=" + version.GetString() + ",Culture=" + culture.GetString() + ",Name=" + name.GetString());
						this[key] = LoadAssembly(assembly);
						loaded = true;
					}
					else
					{
						loaded = false;
					}
				}
			}
			return loaded;
		}
		public static Map LoadAssembly(Assembly assembly)
		{
			Map val = new StrategyMap();
			foreach (Type type in assembly.GetExportedTypes())
			{
				if (type.DeclaringType == null)
				{
					Map selected = val;
					string name;
					if (type.IsGenericTypeDefinition)
					{
						name = type.Name.Split('`')[0];
					}
					else
					{
						name = type.Name;
					}
					selected[type.Name] = new TypeMap(type);
				}
			}
			return val;
		}
		protected override Map Get(Map key)
		{
			Map val;
			if ((key.IsString && strategy.ContainsKey(key)) || Load(key))
			{
				val = strategy.Get(key);
			}
			else
			{
				val = Web.Get(key);
			}
			return val;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				throw new ApplicationException("not implemented.");
			}
		}
		public override bool ContainsKey(Map key)
		{
			return Get(key) != null;
		}
		protected Map cachedAssemblyInfo = new StrategyMap();
	}
	public class FileSystem
	{
		// combine gac into fileSystem
		public static Map fileSystem;
		static FileSystem()
		{
			fileSystem=new StrategyMap();
			fileSystem = new DirectoryMap(new DirectoryInfo(Process.LibraryPath), null);
			DrivesMap drives = new DrivesMap();
			FileSystem.fileSystem["localhost"] = drives;
			fileSystem.Scope = new TemporaryPosition(Gac.gac);
		}
		public static void Save()
		{
			string text = Serialize.ValueFunction(fileSystem).Trim(new char[] { '\n' });
			if (text == "\"\"")
			{
				text = "";
			}
			File.WriteAllText(System.IO.Path.Combine(Process.InstallationPath, "meta.meta"), text, Encoding.Default);
		}
	}
	public class Web
	{
		const int port = 80;
		public static Map Get(Map key)
		{
			if (!key.IsString)
			{
				throw new ApplicationException("key is not a string");
			}
			string address = key.GetString();

			WebClient webClient = new WebClient();

			string metaPath=Path.Combine(new DirectoryInfo(Application.UserAppDataPath).Parent.Parent.Parent.FullName, "Meta");
			string cacheDirectory = Path.Combine(metaPath, "Cache");
			DirectoryInfo unzipDirectory = new DirectoryInfo(Path.Combine(cacheDirectory, address));
			string zipDirectory=Path.Combine(metaPath,"Zip");
			string zipFile = Path.Combine(zipDirectory, address + ".zip");
			Directory.CreateDirectory(zipDirectory);
			string metaZipAddress = "http://"+address + "/" + "meta.zip";

			try
			{
				webClient.DownloadFile(metaZipAddress, zipFile);
			}
			catch (Exception e)
			{
				return null;
			}
			unzipDirectory.Create();
			Unzip(zipFile, unzipDirectory.FullName);
			// net should be parent
			return new WebDirectoryMap(unzipDirectory, FileSystem.fileSystem.Position);
			//return new WebDirectoryMap(unzipDirectory, new TemporaryPosition(FileSystem.fileSystem));
			//}
		}
		public static void Unzip(string zipFile,string dir)
		{
			ZipInputStream s = new ZipInputStream(File.OpenRead(zipFile));

			ZipEntry theEntry;
			while ((theEntry = s.GetNextEntry()) != null)
			{

				Console.WriteLine(theEntry.Name);

				string directoryName = Path.GetDirectoryName(theEntry.Name);
				string fileName = Path.GetFileName(theEntry.Name);

				// fix this for directory creation

				//Directory.CreateDirectory(directoryName);

				if (fileName != String.Empty)
				{
					FileStream streamWriter = File.Create(Path.Combine(dir,theEntry.Name));

					int size = 2048;
					byte[] data = new byte[2048];
					while (true)
					{
						size = s.Read(data, 0, data.Length);
						if (size > 0)
						{
							streamWriter.Write(data, 0, size);
						}
						else
						{
							break;
						}
					}

					streamWriter.Close();
				}
			}
			s.Close();
		}
	}
	public class Number
	{
		private readonly double numerator;
		private readonly double denominator;

		public Number(Number i):this(i.numerator,i.denominator)
		{
		}
		public Number(Map map):this(map.GetNumber())
		{
		}
		public Number(int integer)
			: this((double)integer)
		{
		}
		public Number(long integer)
			: this((double)integer)
		{
		}
		public Number(double integer):this(integer,1)
		{
		}
		public Number(ulong integer):this((double)integer)
		{
		}
		public Number(double numerator, double denominator)
		{
			double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
			if (denominator < 0)
			{
				numerator = -numerator;
				denominator = -denominator;
			}
			this.numerator=numerator/greatestCommonDivisor;
			this.denominator = denominator / greatestCommonDivisor;
		}
		public double Numerator
		{
			get
			{
				return numerator;
			}
		}
		public double Denominator
		{
			get
			{
				return denominator;
			}
		}
		public static Number operator |(Number a, Number b)
		{
			return Convert.ToInt32(a.numerator) | Convert.ToInt32(b.numerator);
		}
		public override string ToString()
		{
			if (denominator == 1)
			{
				return numerator.ToString();
			}
			else
			{
				return numerator.ToString() + Syntax.fraction + denominator.ToString();
			}
		}
		public Number Clone()
		{
			return new Number(this);
		}
		public static implicit operator Number(double number)
		{
			return new Number(number);
		}
		public static implicit operator Number(decimal number)
		{
			return new Number((double)number);
		}
		public static implicit operator Number(int integer)
		{
			return new Number(integer);
		}
		public static bool operator ==(Number a, Number b)
		{
			return !ReferenceEquals(b, null) && a.numerator == b.numerator && a.denominator==b.denominator;
		}
		public static bool operator !=(Number a, Number b)
		{
			return !(a == b);
		}
		private static double GreatestCommonDivisor(double a, double b)
		{
			a = Math.Abs(a);
			b = Math.Abs(b);
		   while (a!=0 && b!=0) {
				   if(a > b)
						   a = a % b;
				   else
						   b = b % a;
		   }
		   if(a == 0)
				   return b;
		   else
				   return a;
		}
		private static double LeastCommonMultiple(Number a, Number b)
		{
			return a.denominator * b.denominator / GreatestCommonDivisor(a.denominator, b.denominator);
		}
		public static Number operator +(Number a, Number b)
		{
			return new Number(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a,b));
		}

		public static Number operator /(Number a, Number b)
		{
			return new Number(a.numerator*b.denominator,a.denominator*b.numerator);
		}
		public static Number operator -(Number a, Number b)
		{
			return new Number(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a,b));
		}
		public static Number operator *(Number a, Number b)
		{
			return new Number(a.numerator * b.numerator, a.denominator * b.denominator);
		}
		public double Expand(Number b)
		{
			return numerator * (LeastCommonMultiple(this, b) / denominator);
		}
		public static bool operator >(Number a, Number b)
		{
			return a.Expand(b) > b.Expand(a);
		}
		public static bool operator <(Number a, Number b)
		{
			return a.Expand(b) < b.Expand(a);
		}
		public static bool operator >=(Number a, Number b)
		{
			return a.Expand(b) >= b.Expand(a);
		}
		public static bool operator <=(Number a, Number b)
		{
			return a.Expand(b) <= b.Expand(a);
		}
		public override bool Equals(object o)
		{
			if (!(o is Number))
			{
				return false;
			}
			Number b = (Number)o;
			return b.numerator==numerator && b.denominator==denominator;
		}
		public override int GetHashCode()
		{
			Number x = new Number(this);
			while (x > int.MaxValue)
			{
				x = x - int.MaxValue;
			}
			return x.GetInt32();
		}
		public int GetInt32()
		{
			// refactor, incorrect
			return Convert.ToInt32(numerator);
		}
		public long GetInt64()
		{
			// refactor, incorrect
			return Convert.ToInt64(numerator);
		}
	}
	namespace Test
	{
		public class MetaTest : TestRunner
		{
			public static int Leaves(Map map)
			{
				int count = 0;
				foreach (KeyValuePair<Map, Map> pair in map)
				{
					if (pair.Value.IsNumber)
					{
						count++;
					}
					else
					{
						count += Leaves(pair.Value);
					}
				}
				return count;

			}
			protected override string TestDirectory
			{
				get
				{
					return Path.Combine(Process.InstallationPath, "Test");
				}
			}
			public static string TestPath
			{
				get
				{
					return Path.Combine(Process.InstallationPath, "Test");
				}
			}
			private static string BasicTest
			{
				get
				{
					return Path.Combine(TestPath, "basicTest.meta");
				}
			}
			private static string LibraryTest
			{
				get
				{
					return Path.Combine(TestPath, "libraryTest.meta");
				}
			}

			public class Extents : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.1"]["Test"]["basicTest"];
				}
			}
			public class Basic : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					Map argument = new StrategyMap(1, "first arg", 2, "second=arg");
					return FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.1"]["Test"]["basicTest"].Call(argument);
				}
			}
			public class Library : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.1"]["Test"]["libraryTest"].Call(Map.Empty);
				}
			}
			public class Serialization : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return Meta.Serialize.ValueFunction(FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.1"]["Test"]["basicTest"]);
				}
			}

		}
		namespace TestClasses
		{
			[Serializable]
			public class MemberTest
			{
				public static string classField = "default";
				public string instanceField = "default";

				public static string OverloadedMethod(string argument)
				{
					return "string function, argument+" + argument;
				}
				public static string OverloadedMethod(int argument)
				{
					return "integer function, argument+" + argument;
				}
				public static string OverloadedMethod(MemberTest memberTest, int argument)
				{
					return "MemberTest function, argument+" + memberTest + argument;
				}

				public static string ClassProperty
				{
					get
					{
						return classField;
					}
					set
					{
						classField = value;
					}
				}
				public string InstanceProperty
				{
					get
					{
						return this.instanceField;
					}
					set
					{
						this.instanceField = value;
					}
				}
			}
			public delegate object IntEvent(object intArg);
			public delegate object NormalEvent(object sender);
			[Serializable]
			public class TestClass
			{
				public class NestedClass// TODO: rename, only used for testing purposes
				{
					public static int field = 0;
				}
				public TestClass()
				{
				}
				public event IntEvent instanceEvent;
				public static event NormalEvent staticEvent;
				protected string x = "unchangedX";
				protected string y = "unchangedY";
				protected string z = "unchangedZ";

				public static bool boolTest = false;

				public static object TestClass_staticEvent(object sender)
				{
					MethodBase[] m = typeof(TestClass).GetMethods();
					return null;
				}
				public delegate string TestDelegate(string x);
				public static Delegate del;
				public static void TakeDelegate(TestDelegate d)
				{
					del = d;
				}
				public static object GetResultFromDelegate()
				{
					return del.DynamicInvoke(new object[] { "argumentString" });
				}
				public double doubleValue = 0.0;
				public float floatValue = 0.0F;
				public decimal decimalValue = 0.0M;
			}
			[Serializable]
			public class PositionalNoConversion : TestClass
			{
				public PositionalNoConversion(string p1, string b, string p2)
				{
					this.x = p1;
					this.y = b;
					this.z = p2;
				}
				public string Concatenate(string p1, string b, string c)
				{
					return p1 + b + c + this.x + this.y + this.z;
				}
			}
			public class NamedNoConversion : TestClass
			{
				public NamedNoConversion(Map arg)
				{
					Map def = new StrategyMap();
					def[1] = "null";
					def["y"] = "null";
					def["p2"] = "null";
					if (arg.ContainsKey(1))
					{
						def[1] = arg[1];
					}
					if (arg.ContainsKey("y"))
					{
						def["y"] = arg["y"];
					}
					if (arg.ContainsKey("p2"))
					{
						def["y2"] = arg["y2"];
					}
					this.x = def[1].GetString();
					this.y = def["y"].GetString();
					this.z = def["p2"].GetString();
				}
				// refactor, remove
				public string Concatenate(Map arg)
				{
					Map def = new StrategyMap();
					def[1] = "null";
					def["b"] = "null";
					def["c"] = "null";

					if (arg.ContainsKey(1))
					{
						def[1] = arg[1];
					}
					if (arg.ContainsKey("b"))
					{
						def["b"] = arg["b"];
					}
					if (arg.ContainsKey("c"))
					{
						def["c"] = arg["c"];
					}
					return def[1].GetString() + def["b"].GetString() + def["c"].GetString() +
						this.x + this.y + this.z;
				}
			}
			[Serializable]
			public class IndexerNoConversion : TestClass
			{
				public string this[string a]
				{
					get
					{
						return this.x + this.y + this.z + a;
					}
					set
					{
						this.x = a + value;
					}
				}
			}
		}
	}
}

