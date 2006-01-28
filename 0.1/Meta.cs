//	Copyright (c) 2005 Christian Staudenmeyer
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
using System.Collections.Generic;
using System.Reflection.Emit;
using Meta;
using Meta.Test;
using Microsoft.Win32;
using System.Windows.Forms;

namespace Meta
{
	public class CodeKeys
	{

		public static readonly Map Search = "search";
		public static readonly Map Lookup = "lookup";

		public static readonly Map Current="current";
		public static readonly Map Scope="scope";
		public static readonly Map Argument="argument";


		public static readonly Map Literal="literal";
		public static readonly Map Function="function";
		public static readonly Map Call="call";
		public static readonly Map Callable="callable";
		public static readonly Map Parameter="parameter";
		public static readonly Map Select="select";
		public static readonly Map Program="program";
		public static readonly Map Key="key";
		public static readonly Map Value="value";
	}
	public class SpecialKeys
	{
		public static readonly Map Scope = "scope";
		public static readonly Map Arg="arg";
		public static readonly Map This="this";
	}
	public class DotNetKeys
	{
		public static readonly Map Add="add";
		public static readonly Map Remove="remove";
		public static readonly Map Get="get";
		public static readonly Map Set="set";
	}
	public class NumberKeys
	{
		public static readonly Map Negative="negative";
		public static readonly Map Denominator="denominator";
	}
	public class Mono
	{
		public static string ReadAllText(string path)
		{
			using (StreamReader reader = new StreamReader(path))
			{
				return reader.ReadToEnd();
			}
		}
		public static void WriteAllText(string path, string text,Encoding encoding)
		{
			using (StreamWriter writer = new StreamWriter(path,false,encoding))
			{
				writer.Write(text);
			}
		}
	}
	public class SyntaxException:MetaException
	{
		public SyntaxException(string message, Parser parser)
			: this(message, parser.File, parser.Line, parser.Column)
		{
		}
		public SyntaxException(string message,string fileName, int line, int column)
			: base(message, new Extent(line, column, line, column,fileName))
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
	public abstract class MetaException:ApplicationException
	{
		public List<Extent> InvocationList
		{
			get
			{
				return stack;
			}
		}
		private List<Extent> stack = new List<Extent>();
		public override string ToString()
		{
			string message = Message;
			if (stack.Count != 0)
			{
				message+="\n\nStack trace:";
			}
			foreach(Extent extent in stack)
			{
				message+="\n" + GetExtentText(extent);
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
		public MetaException(string message, Extent extent)
		{
			this.message = message;
			this.extent = extent;
		}
		public override string Message
		{
			get
			{
				return GetExtentText(extent) + ": " + message;
			}
		}
        private string message;
		private Extent extent;
	}
	public class Throw
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
		public static void KeyDoesNotExist(Map key,Map map,Extent extent)
		{
			string text = "Key does not exist: " + Serialize.Value(key);
			if (Leaves(map) < 1000)
			{
				text+="\nin: "+Serialize.Value(map);
			}
			throw new ExecutionException(text,extent,map);
		}
		public static void KeyNotFound(Map key,Extent extent,Map context)
		{
			throw new ExecutionException("Key could not be found: "+Serialize.Value(key),extent,context);
		}
	}
	public abstract class Expression
	{
		public Map Evaluate(Map context)
		{
			return Evaluate(context, null);
		}
		public Map Evaluate(Map context,Map argument)
		{
			Map current = new StrategyMap();
			current.Scope = context;
			current.Argument = argument;
			return EvaluateImplementation(current,argument);
		}
		public abstract Map EvaluateImplementation(Map context,Map arg);
	}
	public class Call : Expression
	{
		private Map callable;
		public Map parameter;
		public Call(Map code)
		{
			this.callable = code[CodeKeys.Callable];
			this.parameter = code[CodeKeys.Parameter];
		}
		public override Map EvaluateImplementation(Map current,Map arg)
		{
			Map function = callable.GetExpression().Evaluate(current);
			if (!function.IsFunction)
			{
				object x=function.IsFunction;
				throw new ExecutionException("Called map is not a function.", callable.Extent,current);
			}
			Map argument = parameter.GetExpression().Evaluate(current);
			Map result;
			try
			{
				result = function.Call(argument);
			}
			catch (MetaException e)
			{
				e.InvocationList.Add(callable.Extent);
				throw e;
			}
			catch (Exception e)
			{
				throw new ExecutionException(e.ToString(), callable.Extent,current);
			}
			if (result == null)
			{
				result = Map.Empty;
			}
			return result;
		}
	}
	public class Program : Expression
	{
		private List<Map> statements;
		public Program(Map code)
		{
			statements = code.Array;
		}
		public override Map EvaluateImplementation(Map current,Map arg)
		{
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
		public override Map EvaluateImplementation(Map context,Map arg)
		{
			return literal.Copy();
		}
	}

	public abstract class Subselect
	{
		public abstract Map EvaluateImplementation(Map context, Map executionContext);
		public abstract void Assign(ref Map context, Map value, ref Map executionContext);
	}
	public class Current:Subselect
	{
		public override void Assign(ref Map context, Map value,ref  Map executionContext)
		{
			value.Scope = context.Scope;
			value.Argument = context.Argument;
			executionContext = value;
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			return context;
		}
	}
	public class Scope:Subselect
	{
		public override void Assign(ref Map context, Map value,ref Map executionContext)
		{
			throw new Exception("Cannot assign to scope.");
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			return context.Scope;
		}
	}
	public class Argument:Subselect
	{
		// maybe this should be caught in the parser, or even allow it?, at least for scope
		// ref is not really necessary for selected
		public override void Assign(ref Map selected, Map value,ref Map executionContext)
		{
			throw new Exception("Cannot assign to argument.");
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			Map scope=context;
			while (scope!=null && scope.Argument == null)
			{
				scope = scope.Scope;
			}
			return scope.Argument;
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
			return context[keyExpression.GetExpression().Evaluate(executionContext)];
		}
	}
	public class Search:Subselect
	{
		public override void Assign(ref Map selected, Map value,ref Map executionContext)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		private Map keyExpression;
		public Search(Map keyExpression)
		{
			this.keyExpression = keyExpression;
		}
		public override Map EvaluateImplementation(Map context, Map executionContext)
		{
			Map scope = context;
			Map key = keyExpression.GetExpression().Evaluate(executionContext);
			while (scope != null && !scope.ContainsKey(key))
			{
				scope = scope.Scope;
			}
			if (scope == null)
			{
				Throw.KeyNotFound(key, keyExpression.Extent, context);
			}
			return scope[key];
		}
	}
	public class Select : Expression
	{
		private Map GetSpecialKey(Map context, Map key)
		{
			Map val;
			if (key.Equals(SpecialKeys.Scope))
			{
				val = context.Scope;
			}
			else if (key.Equals(SpecialKeys.Arg))
			{
				val = context.Argument;
			}
			else if (key.Equals(SpecialKeys.This))
			{
				val = context.Scope.Scope.Scope;
			}
			else
			{
				val = null;
			}
			return val;
		}
		private Map FindFirstKey(Map keyExpression, Map context)
		{
			Map key = keyExpression.GetExpression().Evaluate(context);
			Map val = GetSpecialKey(context, key);
			if (val == null)
			{
				Map selected = context;
				while (!selected.ContainsKey(key))
				{
					selected = selected.Scope;

					if (selected == null)
					{
						Throw.KeyNotFound(key, keyExpression.Extent, context);
					}
				}
				val = selected[key];
			}
			return val;
		}
		private List<Map> keys;
		public Select(Map code)
		{
			this.keys = code.Array;
		}
		public override Map EvaluateImplementation(Map context, Map arg)
		{
			Map selected=context;
			foreach (Map key in keys)
			{
				selected=key.GetSubselect().EvaluateImplementation(selected, context);
				if (selected == null)
				{
				}
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
			this.keys = code[CodeKeys.Key].Array;
			if (keys.Count == 0)
			{
			}
			this.value = code[CodeKeys.Value];
		}
		public void Assign(ref Map context)
		{
			Map selected = context;
			for (int i = 0; i + 1 < keys.Count; i++)
			{
				Map oldSelected=selected;
				selected = keys[i].GetSubselect().EvaluateImplementation(selected, context);
			}
			Map val = value.GetExpression().Evaluate(context);
			keys[keys.Count - 1].GetSubselect().Assign(ref selected, val, ref context);
		}
	}
	public class Library
	{
		public static string writtenText = "";
		public static void Write(string text)
		{
			writtenText += text;
			Console.Write(text);
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
			List<Map> array=arg["array"].Array;
			array.Sort(new Comparison<Map>(delegate(Map a, Map b)
			{
				int x=arg["compare"].Call(new StrategyMap("a", a, "b", b)).GetNumber().GetInt32();
				// refactor
				return x != 0 ? -1 : 0;
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
		public static Map Join(Map arg)
		{
			Map result = Map.Empty;
			Number counter = 1;
			foreach (Map map in arg.Array)
			{
				result.AppendMap(map);
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
	public class Process
	{
		[STAThread]
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Meta 0.1 interactive mode");
				Map map = new StrategyMap();
				map.Scope = FileSystem.fileSystem;
				string code;

				Parser parser = new Parser("", "Interactive console");
				parser.indentationCount = 0;
				parser.functions++;
				parser.defaultKeys.Push(1);
				while (true)
				{
					code = "";
					Console.Write(parser.Line + "> ");
					int lines = 0;
					while (true)
					{
						string input = Console.ReadLine();
						if (input.Trim().Length != 0)
						{
							code += input + Syntax.unixNewLine;
							char character = input[input.TrimEnd().Length - 1];
							if (!(Char.IsLetter(character) || character == ']') && !input.StartsWith("\t") && character != '=')
							{
								break;
							}
							else if(lines==0)
							{
								SendKeys.SendWait("{TAB}");
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
						Console.Write(parser.Line + lines + ". ");
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
			else
			{
				if (args[0] == "-test")
				{
					new MetaTest().Run();
				}
				else if (args[0] == "-profile")
				{
					Console.WriteLine("profiling");
					object x = FileSystem.fileSystem["basicTest"];
				}
				else
				{
					Map function = FileSystem.ParseFile(args[0]);
					int autoKeys = 0;
					Map argument = new StrategyMap();
					for (int i = 1; i < args.Length; i++)
					{
						string arg = args[i];

						Map key;
						Map value;
						if (arg.StartsWith("-"))
						{
							string nextArg;
							// move down
							if (i + 1 < args.Length)
							{
								nextArg = args[i + 1];
							}
							else
							{
								nextArg = null;
							}
							key = arg.Remove(0, 1);
							if (nextArg != null)
							{
								if (nextArg.StartsWith("-"))
								{
									value = Map.Empty;
								}
								else
								{
									value = nextArg;
									i++;

								}
							}
							else
							{
								value = Map.Empty;
							}
						}
						else
						{
							autoKeys++;
							key = autoKeys;
							value = arg;
						}
						argument[key] = value;
					}
					function.Call(argument);
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
					//installationPath = (string)Registry.LocalMachine.OpenSubKey(@"Software\Meta 0.1").GetValue("InstallationPath");
					if (installationPath == null)
					{
						Uri uri = new Uri(Assembly.GetAssembly(typeof(Map)).CodeBase);
						installationPath = Path.GetDirectoryName(uri.AbsolutePath);
					}
				}
				return installationPath;
			}
			set
			{
				installationPath = value;
			}
		}
	}
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public Map Argument
		{
			get
			{
				Map arg;
				if (argument != null)
				{
					arg = argument;
				}
				else if (Scope != null)
				{
					arg = Scope.Argument;
				}
				else
				{
					arg = null;
				}
				return arg;
			}
			set
			{
				argument = value;
			}
		}
		public Map argument=null;
		public abstract bool IsFunction
		{
			get;
		}

		// rename
		public virtual void AppendMap(Map array)
		{
			AppendMapDefault(array);
		}
		public virtual void AppendMapDefault(Map array)
		{
			int counter = ArrayCount + 1;
			foreach (Map map in array.Array)
			{
				this[counter] = map;
				counter++;
			}
		}
		private Statement statement;
		public Statement GetStatement()
		{
			if (statement == null)
			{
				statement = new Statement(this);
			}
			return statement;
		}
		private Subselect subselect;
		public Subselect GetSubselect()
		{
			if (subselect == null)
			{
				if (ContainsKey(CodeKeys.Current))
				{
					subselect = new Current();
				}
				else if (ContainsKey(CodeKeys.Argument))
				{
					subselect = new Argument();
				}
				else if (ContainsKey(CodeKeys.Scope))
				{
					subselect = new Scope();
				}
				else if (ContainsKey(CodeKeys.Search))
				{
					subselect = new Search(this[CodeKeys.Search]);
				}
				else if (ContainsKey(CodeKeys.Lookup))
				{
					subselect = new Lookup(this[CodeKeys.Lookup]);
				}
				else
				{
					throw new Exception("Map is not a subselect.");
				}
			}
			return subselect;
		}
		public Expression GetExpression()
		{
			if (expression == null)
			{
				if (ContainsKey(CodeKeys.Call))
				{
					expression = new Call(this[CodeKeys.Call]);
				}
				else if (ContainsKey(CodeKeys.Program))
				{
					expression = new Program(this[CodeKeys.Program]);
				}
				else if (ContainsKey(CodeKeys.Literal))
				{
					expression = new Literal(this[CodeKeys.Literal]);
				}
				else if (ContainsKey(CodeKeys.Select))
				{
					expression = new Select(this[CodeKeys.Select]);
				}
				else
				{
					throw new ApplicationException("Cannot compile map.");
				}
			}
			return expression;
		}
		private Expression expression;

		public string GetKeyStrings()
		{
			string text="";
			foreach (Map key in this.Keys)
			{
				text += Meta.Serialize.Key(key,"") + " ";
			}
			return text;
		}
		public Map This
		{
			get
			{
				return this;
			}
		}
		public void Append(Map map)
		{
			this[Array.Count + 1] = map;
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
			if (this.IsString)
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
		public virtual bool GetBoolean()
		{
			bool boolean;
			if(GetNumber()==0)
			{
				boolean=false;
			}
			else if(GetNumber()==1)
			{
				boolean=true;
			}
			else
			{
				throw new ApplicationException("Map is not a boolean.");
			}
			return boolean;
		}
		public virtual bool IsNumber
		{
			get
			{
				return IsNumberDefault;
			}
		}
		public bool IsNumberDefault
		{
			get
			{
				try
				{
					return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this[Map.Empty].IsNumber);
				}
				catch(Exception e)
				{
					int asfd;
					return ContainsKey(Map.Empty) && this[Map.Empty].IsNumber;
				}
			}
		}
		public virtual Number GetNumber()
		{
			return GetNumberDefault();
		}
		public Number GetNumberDefault()
		{
			Number number;
			if(this.Equals(Map.Empty))
			{
				number=0;
			}
			else if(this.Count==1 && this.ContainsKey(Map.Empty) && this[Map.Empty].GetNumber()!=null)
			{
				number = 1 + this[Map.Empty].GetNumber();
			}
			else
			{
				throw new ApplicationException("Map is not an integer");
			}
			return number;
		}
		public virtual bool IsString
		{
			get
			{
				return IsStringDefault;
			}
		}
		public bool IsStringDefault
		{
			get
			{
				bool isString;
				if (ArrayCount == Count)
				{
					isString = this.Array.TrueForAll(
						delegate(Map map)
						{
							return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);
						});
				}
				else
				{
					isString = false;
				}
				return isString;
			}
		}
		public virtual string GetString()
		{
			return GetStringDefault();
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
        public Map Scope
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
        private Map scope;
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
		// refactor
		public Map GetForAssignment(Map key)
		{
			return Get(key);
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
					expression = null;
					statement = null;
					Map val = value;
					//Map val = value.Copy();//.Copy();
					if (val.scope == null)
					{
						val.scope = this;
					}
                    Set(key, val);
                }
             }
        }
        protected abstract Map Get(Map key);
        protected abstract void Set(Map key, Map val);
		public virtual Map Call(Map arg)
		{
			Map function = this[CodeKeys.Function];

			//Map current = new StrategyMap();
			//current.Scope = this;
			//current.Argument = arg;

			//Map result = function.GetExpression().Evaluate(current, arg);
			Map result = function.GetExpression().Evaluate(this, arg);
			return result;
		}
		public abstract ICollection<Map> Keys
		{
			get;
		}
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
		private Map parent;
		public static implicit operator Map(Number integer)
		{
			return new StrategyMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new StrategyMap(new Number((int)(boolean?1:0)));
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
	}

	public class StrategyMap:Map
	{
		public StrategyMap(params Map[] keysAndValues):this()
		{
			for (int i = 0; i + 2 <= keysAndValues.Length; i += 2)
			{
				this[keysAndValues[i]] = keysAndValues[i + 1];
			}
		}
		public override bool IsFunction
		{
			get
			{
				Map function=this[CodeKeys.Function];
				bool isFunction;
				if (function != null && (function.ContainsKey(CodeKeys.Call) || function.ContainsKey(CodeKeys.Literal) || function.ContainsKey(CodeKeys.Program) || function.ContainsKey(CodeKeys.Select)))
				{
					isFunction = true;
				}
				else
				{
					isFunction = false;
				}
				return isFunction;
			}
		}
		public override int ArrayCount
		{
			get
			{
				return strategy.GetArrayCount();
			}
		}
		public override void AppendMap(Map array)
		{
			strategy.AppendMap(array);
		}
		public bool Persistant
		{
			get
			{
				return persistant;
			}
			set
			{
				persistant = value;
			}
		}
		private bool persistant=false;

		public override Map Call(Map arg)
		{
			return base.Call(arg);
		}
		public void InitFromStrategy(MapStrategy clone)
		{
			foreach (Map key in clone.Keys)
			{
				this[key] = clone.Get(key);
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
			if (key.Equals(new StrategyMap("Meta")))
			{
			}
			return strategy.Get(key);
		}
		protected override void Set(Map key, Map value)
		{
			strategy.Set(key, value);
			if (Persistant && !FileSystem.Parsing)
			{
				//FileSystem.Save();
			}
		}

		public override ICollection<Map> Keys
		{
			get
			{
				return strategy.Keys;
			}
		}
		protected override Map CopyData()
		{
			return strategy.CopyData();
		}
		public override bool ContainsKey(Map key)
		{
			return strategy.ContainsKey(key);
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
		public StrategyMap(Number number):this(new NumberStrategy(number))
		{
		}
		public StrategyMap(string text)
			: this(new ListStrategy(text))
		{
		}
	}
	//public class RemoteStrategy : MapStrategy
	//{
	//    public override MapStrategy CopyImplementation()
	//    {
	//        throw new Exception("The method or operation is not implemented.");
	//    }
	//    public override ICollection<Map> Keys
	//    {
	//        get
	//        {
	//            return null;
	//        }
	//    }
	//    public override Map Get(Map key)
	//    {
	//        if(!key.IsString)
	//        {
	//            throw new ApplicationException("key is not a string");
	//        }
	//        WebClient webClient=new WebClient();
	//        Uri fullPath=new Uri(new Uri("http://"+address),key.GetString()+".meta");
	//        Stream stream=webClient.OpenRead(fullPath.ToString());
	//        StreamReader streamReader=new StreamReader(stream);
	//        return FileSystem.Parse(streamReader,"Web");
	//    }
	//    public override void Set(Map key,Map val)
	//    {
	//        throw new ApplicationException("Cannot set key in remote map.");
	//    }
	//    private string address;
	//    public RemoteStrategy(string address)
	//    {
	//        this.address=address;
	//    }
	//}
	//public class NetStrategy:MapStrategy
	//{
	//    public static readonly StrategyMap Net = new StrategyMap(new NetStrategy());
	//    private NetStrategy()
	//    {
	//    }
	//    public override ICollection<Map> Keys
	//    {
	//        get
	//        {
	//            return new List<Map>();
	//        }
	//    }
	//    public override Map  Get(Map key)
	//    {
	//        if (!key.IsString)
	//        {
	//            throw new ApplicationException("need a string here");
	//        }
	//        Map val;
	//        if (key.Equals(SpecialKeys.Local))
	//        {
	//            val = FileSystem.fileSystem;
	//        }
	//        else
	//        {
	//            val=new StrategyMap(new RemoteStrategy(key.GetString()));
	//        }
	//        return val;
	//    }
	//    public override void Set(Map key, Map val)
	//    {
	//        throw new ApplicationException("Cannot set key in Web.");
	//    }
	//    public override MapStrategy CopyImplementation()
	//    {
	//        throw new ApplicationException("Not implemented.");
	//    }
	//    public static NetStrategy singleton=new NetStrategy();
	//}

	public class Transform
	{
		public static object ToDotNet(Map meta, Type target)
		{
			object dotNet = null;
			if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
				&& meta.ContainsKey(CodeKeys.Function))
			{
				MethodInfo invoke = target.GetMethod("Invoke",
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				Delegate function = MethodOverload.CreateDelegateFromCode(target, meta);
				dotNet = function;
			}
			if (meta is TypeMap && target == typeof(Type))
			{
				dotNet = ((TypeMap)meta).type;
			}
			else if (target.IsSubclassOf(typeof(Enum)) && meta.IsNumber)
			{
				dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());
			}
			else
			{
				switch (Type.GetTypeCode(target))
				{
					case TypeCode.Boolean:
						if (IsIntegerInRange(meta, 0, 1))
						{
							if (meta.GetNumber() == 0)
							{
								dotNet = false;
							}
							else if (meta.GetNumber() == 1)
							{
								dotNet = true;
							}
						}
						break;
					case TypeCode.Byte:
						if (IsIntegerInRange(meta, new Number(Byte.MinValue), new Number(Byte.MaxValue)))
						{
							dotNet = Convert.ToByte(meta.GetNumber().GetInt32());
						}
						break;
					case TypeCode.Char:
						if (IsIntegerInRange(meta, (int)Char.MinValue, (int)Char.MaxValue))
						{
							dotNet = Convert.ToChar(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.DateTime:
						dotNet = null;
						break;
					case TypeCode.DBNull:
						if (meta.IsNumber && meta.GetNumber() == 0)
						{
							dotNet = DBNull.Value;
						}
						break;
					case TypeCode.Decimal:
						if (IsIntegerInRange(meta, new Number((double)decimal.MinValue), new Number((double)decimal.MaxValue)))
						{
							dotNet = (decimal)(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Double:
						if (IsIntegerInRange(meta, new Number(double.MinValue), new Number(double.MaxValue)))
						{
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
						if (IsIntegerInRange(meta, (Number)Int32.MinValue, Int32.MaxValue))
						{
							dotNet = meta.GetNumber().GetInt32();
						}
						//// TODO: add this for all integers, and other types
						//else if (meta is ObjectMap && ((ObjectMap)meta).Object is Int32)
						//{
						//    dotNet = (Int32)((ObjectMap)meta).Object;
						//}
						break;
					case TypeCode.Int64:
						if (IsIntegerInRange(meta, new Number(Int64.MinValue), new Number(Int64.MaxValue)))
						{
							dotNet = Convert.ToInt64(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Object:
						if (target.Name == "Point")
						{
						}
						if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).type))
						{
							dotNet = ((ObjectMap)meta).obj;
						}
						else if (target.IsAssignableFrom(meta.GetType()))
						{
							dotNet = meta;
						}
						else
						{
							ConstructorInfo constructor = target.GetConstructor(BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
							ObjectMap result;
							if (constructor != null)
							{
								// should this always be an ObjectMap? What if we are calling the constructor of a Map?
								result = new ObjectMap(target.GetConstructor(new Type[] { }).Invoke(new object[] { }));
							}
							else if (target.IsValueType)
							{
								object x = target.InvokeMember(".ctor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Static, null, null, new object[] { });
								result = new ObjectMap(x);
							}
							else
							{
								break;
							}
							foreach (KeyValuePair<Map, Map> pair in meta)
							{
								((Property)result[pair.Key])[DotNetKeys.Set].Call(pair.Value);
							}
							dotNet = result.Object;
						}
						break;
					case TypeCode.SByte:
						if (IsIntegerInRange(meta, (Number)SByte.MinValue, (Number)SByte.MaxValue))
						{
							dotNet = Convert.ToSByte(meta.GetNumber().GetInt64());
						}
						break;
					case TypeCode.Single:
						if (IsIntegerInRange(meta, new Number(Single.MinValue), new Number(Single.MaxValue)))
						{
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
						if (IsIntegerInRange(meta, new Number(UInt16.MinValue), new Number(UInt16.MaxValue)))
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
			Map meta;
			// i'm not sure about this
			if (dotNet == null)
			{
				meta = new ObjectMap(null, typeof(Object));
			}
			else if (dotNet is Map)
			{
				meta = (Map)dotNet;
			}
			else
			{
				meta = new ObjectMap(dotNet);
			}
			return meta;
		}
		public static Map ToMeta(object dotNet)
		{
			Map meta;
			if(dotNet==null)
			{
				meta=null;
			}
			else
			{
				switch(Type.GetTypeCode(dotNet.GetType()))
				{
					case TypeCode.Boolean:
						meta=((bool)dotNet)? 1:0;
						break;
					case TypeCode.Byte:
						meta=(byte)dotNet;
						break;
					case TypeCode.Char:
						meta=(char)dotNet;
						break;
					case TypeCode.DateTime:
						meta=new ObjectMap(dotNet);
						break;
					case TypeCode.DBNull:
						meta=new ObjectMap(dotNet);
						break;
					case TypeCode.Decimal:
						meta=(int)dotNet;
						break;
					case TypeCode.Double:
						meta=(int)dotNet;
						break;
					case TypeCode.Int16:
						meta=(short)dotNet;
						break;
					case TypeCode.Int32:
						meta=(int)dotNet;
						break;
					case TypeCode.Int64:
						meta=(long)dotNet;
						break;
					case TypeCode.Object:
						if(dotNet.GetType().IsSubclassOf(typeof(Enum)))
						{
							meta=(int)Convert.ToInt32((Enum)dotNet);
						}
						else if(dotNet is Map)
						{
							meta=(Map)dotNet;
						}
						else
						{
							meta=new ObjectMap(dotNet);
						}
						break;
					case TypeCode.SByte:
						meta=(sbyte)dotNet;
						break;
					case TypeCode.Single:
						meta=(int)dotNet;
						break;
					case TypeCode.String:
						meta=(string)dotNet;
						break;
					case TypeCode.UInt32:
						meta=(uint)dotNet;
						break;
					case TypeCode.UInt64:
						meta=(ulong)dotNet;
						break;
					case TypeCode.UInt16:
						meta=(ushort)dotNet;
						break;
					default:
						throw new ApplicationException("not implemented");
				}
			}
			return meta;
		}
	}
	public delegate object DelegateCreatedForGenericDelegates();


	public abstract class MethodImplementation:Map
	{
		public MethodImplementation(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
		}
		protected MethodBase method;
		protected object obj;
		protected Type type;
		public override Map Call(Map argument)
		{
			object result;
			List<object> arguments = new List<object>();
			bool argumentsMatched = true;
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 1)
			{
				if (parameters[0].ParameterType.Name == "Int32")
				{
				}
				if (this.method.Name.StartsWith("set_Instance"))
				{
				}
				object arg = Transform.ToDotNet(argument, parameters[0].ParameterType);
				if (arg != null)
				{
					arguments.Add(arg);
				}
				else
				{
					throw new ApplicationException("Cannot convert argument.");
				}
			}
			else
			{
				// refactor
				for (int i = 0; argumentsMatched && i < parameters.Length; i++)
				{
					object arg = Transform.ToDotNet(argument[i + 1], parameters[i].ParameterType);
					if (arg != null)
					{
						arguments.Add(arg);
					}
					else
					{
						throw new ApplicationException("Cannot convert argument.");
					}
				}
			}
			if (method is ConstructorInfo)
			{
				try
				{
					result = ((ConstructorInfo)method).Invoke(arguments.ToArray());
				}
				catch (Exception e)
				{
					throw e.InnerException;
				}
			}
			else
			{
				try
				{
					result = method.Invoke(obj, arguments.ToArray());
				}
				catch (Exception e)
				{
					throw e.InnerException;
				}
			}
			return Transform.ToMeta(result);
		}
	}
	public class Method : MethodImplementation
	{
		// refactor
		public override Map Call(Map arg)
		{
			if (IsFunction)
			{
				return base.Call(arg);
			}
			else
			{
				throw new ApplicationException("Method cannot be called directly, it is overloaded.");
			}
		}
	    public override bool IsFunction
	    {
	        get
	        {
	            return method!=null;
	        }
	    }
	    private Dictionary<Map, MethodOverload> overloads;
	    private Method(Dictionary<Map, MethodOverload> overloads,MethodBase method,object obj,Type type):base(method,obj,type)
	    {
	        this.overloads = overloads;
	    }
		private static MethodBase GetSingleMethod(string name, object obj, Type type)
		{
			MemberInfo[] members = type.GetMember(name, GetBindingFlags(obj,name));
			MethodBase singleMethod;
			if (members.Length == 1)
			{
				singleMethod = (MethodBase)members[0];
			}
			else
			{
				singleMethod = null;
			}
			return singleMethod;
		}
		private static BindingFlags GetBindingFlags(object obj,string name)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
			if (obj != null || name==".ctor")
			{
				bindingFlags |= BindingFlags.Instance;
			}
			else
			{
				bindingFlags |= BindingFlags.Static;
			}
			return bindingFlags;
		}
		public Method(string name, object obj, Type type):base(GetSingleMethod(name,obj,type), obj,type)
	    {
			if (name=="set_Location")
			{
			}
			MemberInfo[] methods=type.GetMember(name, GetBindingFlags(obj,name));
			// refactor
			if(methods.Length>1)
			{
				this.overloads=new Dictionary<Map,MethodOverload>();
				foreach (MethodBase method in methods)
				{
					Map key;

					ParameterInfo[] parameters=method.GetParameters();
					if (parameters.Length == 1)
					{
						key = new TypeMap(parameters[0].ParameterType);
					}
					else
					{
						key=new StrategyMap();
						foreach (ParameterInfo parameter in parameters)
						{
							key.Append(new TypeMap(parameter.ParameterType));
						}
					}
					MethodOverload overload = new MethodOverload(method, obj, type);
					overloads[key]=overload;
				}
			}
			else
			{
				this.overloads=null;
			}
	    }
		public Method(Type type):this(".ctor", null, type)
		{
		}
	    protected override Map CopyData()
	    {
	        return new Method(overloads,method,obj,type);
	    }
	    public override ICollection<Map> Keys
	    {
	        get
	        {
				ICollection<Map> keys;
				if (overloads != null)
				{
					keys = overloads.Keys;
				}
				else
				{
					keys = new List<Map>();
				}
				return keys;
	        }
	    }
	    protected override Map Get(Map key)
	    {
	        MethodOverload value;
			if (key is TypeMap && ((TypeMap)key).Type.Name.StartsWith("ICollection"))
			{
			}
			if (overloads == null)
			{
				value = null;
			}
			else
			{
				overloads.TryGetValue(key, out value);
			}
	        return value;
	    }
	    protected override void Set(Map key, Map val)
	    {
	        throw new ApplicationException("Cannot set key in Method.");
	    }
	}
	public class MethodOverload : MethodImplementation
	{
	    public override bool IsFunction
	    {
	        get
	        {
	            return true;
	        }
	    }
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
		public MethodOverload(MethodBase method, object obj, Type type):base(method,obj,type)
		{
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
	            return Meta.Transform.ToDotNet(result, this.returnType);
	        }
	    }
	}
	public class TypeMap: DotNetMap
	{
		protected override Map Get(Map key)
		{
			Map value;
			if (key is TypeMap && ((TypeMap)key).type == typeof(Map) && this.type==typeof(ObjectMap))
			{
			}
			if (type.IsGenericTypeDefinition && key.Array.TrueForAll(delegate(Map map) { return map is TypeMap; }))
			{
				List<Type> types;
				if (type.GetGenericArguments().Length == 1)
				{
					types = new List<Type>();
					types.Add(((TypeMap)key).Type);
				}
				else
				{
					types = key.Array.ConvertAll<Type>(new Converter<Map, Type>(delegate(Map map) { return ((TypeMap)map).type; }));
				}
				value = new TypeMap(type.MakeGenericType(types.ToArray()));
			}
			else
			{
				value = base.Get(key);
			}
			if (value == null)
			{
				value = this.Constructor[key];
			}
			return value;
		}
		public override bool IsFunction
		{
			get
			{
				return Constructor.IsFunction;
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
			bool equal;
			if (obj is TypeMap)
			{
				TypeMap typeMap = (TypeMap)obj;
				equal = typeMap.type == this.type;
				if (equal)
				{
				}
			}
			else
			{
				equal = false;
			}
			return equal;
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
		public TypeMap(Type targetType):base(null,targetType)
		{
		}
		public override Map Call(Map argument)
		{
			return Constructor.Call(argument);
		}
	}
	public class ObjectMap: DotNetMap
	{
		public override bool IsFunction
		{
			get
			{
				return false;
			}
		}
		public object Object
		{
			get
			{
				return obj;
			}
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
		public override bool IsNumber
		{
			get
			{
				return true;
			}
		}
		public override Number GetNumber()
		{
			return data;
		}
		protected override bool SameEqual(Number otherData)
		{
			return otherData == data;
		}
		public NumberStrategy(Number number)
		{
			this.data = new Number(number);
		}
		public override Map CopyData()
		{
			return new StrategyMap(new NumberStrategy(data));
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
					keys.Add(NumberKeys.Negative);
				}
				if (data.Denominator != 1.0d)
				{
					keys.Add(NumberKeys.Denominator);
				}
				return keys;
			}
		}
		public override Map Get(Map key)
		{
			Map value;
			if (ContainsKey(key))
			{
				if (key.Equals(Map.Empty))
				{
					value = data - 1;
				}
				else if(key.Equals(NumberKeys.Negative))
				{
					value=Map.Empty;
				}
				else if (key.Equals(NumberKeys.Denominator))
				{
					value = new StrategyMap(new Number(data.Denominator));
				}
				else
				{
					throw new ApplicationException("Error.");
				}
			}
			else
			{
				value = null;
			}
			return value;
		}
		public override void Set(Map key, Map value)
		{
			if (key.Equals(Map.Empty) && value.IsNumber)
			{
				this.data = value.GetNumber() + 1;
			}
			else if (key.Equals(NumberKeys.Negative) && value.Equals(Map.Empty) && data!=0)
			{
				if (data > 0)
				{
					data = 0 - data;
				}
			}
			else if (key.Equals(NumberKeys.Denominator) && value.IsNumber)
			{
				this.data = new Number(data.Numerator, value.GetNumber().GetInt32());
			}
			else
			{
				Panic(key, value);
			}
		}
	}
	public class ListStrategy : DataStrategy<List<Map>>
	{
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
		public override int Count
		{
			get
			{
				return data.Count;
			}
		}
		public override void AppendMap(Map array)
		{
			foreach (Map map in array.Array)
			{
				this.data.Add(map.Copy());
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
	}
	public class DictionaryStrategy:DataStrategy<Dictionary<Map,Map>>
	{
		protected override bool SameEqual(Dictionary<Map, Map> otherData)
		{
			bool equal;
			if (data.Count == otherData.Count)
			{
				equal = true;
				foreach (KeyValuePair<Map, Map> pair in data)
				{
					Map value;
					otherData.TryGetValue(pair.Key,out value);
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
		public DictionaryStrategy():this(2)
		{
			if (this.map!= null &&this.IsString && this.GetString() == "abc, ")
			{
			}
		}
		public override Map CopyData()
		{
			StrategyMap copy=new StrategyMap();
			foreach (KeyValuePair<Map, Map> pair in this.map)
			{
				copy[pair.Key] = pair.Value;
			}
			return copy;
		}
		public DictionaryStrategy(int Count)
		{
			this.data=new Dictionary<Map,Map>(Count);
		}
		public override List<Map> Array
		{
			get
			{
				List<Map> list=new List<Map>();
				for(Number iInteger=new Number(1);ContainsKey(new StrategyMap(iInteger));iInteger+=1)
				{
					list.Add(this.Get(new StrategyMap(iInteger)));
				}
				return list;
			}
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
		public override Map  Get(Map key)
		{
			Map val;
            data.TryGetValue(key,out val);
            return val;
		}
		public override void Set(Map key,Map value)
		{
			data[key]=value;
		}
		public override bool ContainsKey(Map key) 
		{
			return data.ContainsKey(key);
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

		public virtual int GetArrayCount()
		{
			return map.GetArrayCountDefault();
		}
		public virtual void AppendMap(Map array)
		{
			map.AppendMapDefault(array);
		}
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

		public StrategyMap map;


		public abstract Map CopyData();
		//public virtual Map CopyDataShallow()
		//{
		//    StrategyMap copy;
		//    MapStrategy strategy = (MapStrategy)this.CopyData();
		//    copy = new StrategyMap(strategy);
		//    strategy.map = copy;
		//    return copy;
		//}

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
		public abstract ICollection<Map> Keys
		{
			get;
		}
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		public abstract void Set(Map key, Map val);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}
		public abstract bool Equal(MapStrategy strategy);

		public virtual bool EqualDefault(MapStrategy strategy)
		{
			bool isEqual;
			if (Object.ReferenceEquals(strategy, this))
			{
				isEqual = true;
			}
			else if (((MapStrategy)strategy).Count != this.Count)
			{
				isEqual = false;
			}
			else
			{
				isEqual = true;
				foreach (Map key in this.Keys)
				{
					Map otherValue = strategy.Get(key);
					Map thisValue = Get(key);
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue))
					{
						isEqual = false;
					}
				}
			}
			return isEqual;
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
		protected abstract bool SameEqual(T otherData);
	}
	public class Event:Map
	{
		public override bool IsFunction
		{
			get
			{
				return true;
			}
		}
		EventInfo eventInfo;
		object obj;
		Type type;
		public Event(EventInfo eventInfo,object obj,Type type)
		{
			this.eventInfo=eventInfo;
			this.obj=obj;
			this.type=type;
		}
		public override Map Call(Map argument)
		{
			Map result;
			Delegate eventDelegate = (Delegate)type.GetField(eventInfo.Name, BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(obj);
			if (eventDelegate != null)
			{
				List<object> arguments = new List<object>();
				// refactor, with MethodImplementation
				ParameterInfo[] parameters = eventDelegate.Method.GetParameters();
				if (parameters.Length == 2)
				{
					arguments.Add(Transform.ToDotNet(argument, parameters[1].ParameterType));
				}
				else
				{
					for (int i = 1; i < parameters.Length; i++)
					{
						arguments.Add(Transform.ToDotNet(argument[i], parameters[i].ParameterType));
					}
				}
				result = new ObjectMap(eventDelegate.DynamicInvoke(arguments.ToArray()));
			}
			else
			{
				result = null;
			}
			return result;
		}
		protected override Map CopyData()
		{
			return new Event(eventInfo,obj,type);
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				if(eventInfo.GetAddMethod()!=null)
				{
					keys.Add(DotNetKeys.Add);
				}

				return keys;
			}
		}
		protected override Map Get(Map key)
		{
			Map val;
			if (key.Equals(DotNetKeys.Add))
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
	}
	public class Property:Map
	{
		public override bool IsFunction
		{
			get
			{
				return false;
			}
		}
		PropertyInfo property;
		object obj;
		Type type;
		public Property(PropertyInfo property,object obj,Type type)
		{
			this.property=property;
			this.obj=obj;
			this.type=type;
		}
		protected override Map CopyData()
		{
			return new Property(property,obj,type);
		}
		public override ICollection<Map> Keys
		{
			get
			{

				List<Map> keys=new List<Map>();
				if(property.GetGetMethod()!=null)
				{
					keys.Add(DotNetKeys.Get);
				}
				if(property.GetSetMethod()!=null)
				{
					keys.Add(DotNetKeys.Set);
				}
				return keys;
			}
		}
		protected override Map Get(Map key)
		{
			Map val;
			if(key.Equals(DotNetKeys.Get))
			{
				val=new Method(property.GetGetMethod().Name,obj,type);
			}
			else if(key.Equals(DotNetKeys.Set))
			{
				val=new Method(property.GetSetMethod().Name,obj,type);
			}
			else
			{
				val=null;
			}
			return val;
		}
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot assign in property "+property.Name+".");
		}
	}
	public abstract class DotNetMap: Map, ISerializeEnumerableSpecial
	{
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
		public override List<Map> Array
		{
			get
			{
				List<Map> array=new List<Map>();
				foreach(Map key in Keys)
				{
					if(key.IsNumber)
					{
						array.Add(this[key]);
					}
				}
				return array;
			}
		}
		public override bool ContainsKey(Map key)
		{
			if(key.IsString)
			{
				string text=key.GetString();
				if(type.GetMember(key.GetString(),
					BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
				{
					return true;
				}
			}
			return false;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				foreach(MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					keys.Add(new StrategyMap(member.Name));
				}
				return keys;
			}
		}
		protected override Map Get(Map key)
		{
			Map val;
			if (key.IsString)
			{
				string text = key.GetString();

				MemberInfo[] members = type.GetMember(text, bindingFlags);
				if (members.Length != 0)
				{
					if (members[0] is MethodBase)
					{
						val = new Method(text, obj, type);
					}
					else if (members[0] is PropertyInfo)
					{
						val = new Property(type.GetProperty(text), this.obj, type);
					}
					else if (members[0] is FieldInfo)
					{
						val = Transform.ToMeta(type.GetField(text).GetValue(obj));
					}
					else if (members[0] is EventInfo)
					{
						val = new Event(((EventInfo)members[0]), obj, type);
					}
					else if (members[0] is Type)
					{
						val = new TypeMap((Type)members[0]);
					}
					else
					{
						val = null;
					}
				}
				else
				{
					val = null;
				}
			}
			else
			{
				val = null;
			}
			return val;
		}
		protected override void Set(Map key,Map value)
		{
			if (key.IsString && type.GetMember(key.GetString(), bindingFlags).Length != 0)
			{
				string text = key.GetString();
				MemberInfo member = type.GetMember(text, bindingFlags)[0];
				if (member is FieldInfo)
				{
					FieldInfo field = (FieldInfo)member;
					object val = Transform.ToDotNet(value, field.FieldType);
					if (val!=null)
					{
						field.SetValue(obj, val);
					}
					else
					{
						throw new ApplicationException("Field " + field.Name + " could not be assigned because the value cannot be converted.");
					}
				}
				else if (member is PropertyInfo)
				{
					throw new ApplicationException("Cannot set property " + member.Name + " directly. Use its set method instead.");
				}
				else if (member is EventInfo)
				{
					throw new ApplicationException("Cannot set event " + member.Name + " directly. Use its add method instead.");
				}
				else if (member is MethodBase)
				{
					throw new ApplicationException("Cannot assign to method " + member.Name + ".");
				}
				else
				{
					throw new ApplicationException("Could not assign " + text + " .");
				}
			}
			else if (obj != null && key.IsNumber && type.IsArray)
			{
				object converted = Transform.ToDotNet(value, type.GetElementType());
				if (converted!=null)
				{
					((Array)obj).SetValue(converted, key.GetNumber().GetInt32());
					return;
				}
			}
			else
			{
				throw new ApplicationException("Cannot set key " + Meta.Serialize.Value(key) + ".");
			}
		}
		public string Serialize(string indent,string[] functions)
		{
			return indent;
		}
		public Delegate CreateEventDelegate(string name,Map code)
		{
			EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
				BindingFlags.Static|BindingFlags.Instance);
			Delegate eventDelegate=MethodOverload.CreateDelegateFromCode(eventInfo.EventHandlerType,code);
			return eventDelegate;
		}
		public DotNetMap(object obj,Type type)
		{
			if(obj==null)
			{
				this.bindingFlags=BindingFlags.Public|BindingFlags.Static;
			}
			else
			{
				this.bindingFlags=BindingFlags.Public|BindingFlags.Instance;
			}
			this.obj=obj;
			this.type=type;
		}
		private BindingFlags bindingFlags;
		[NonSerialized]
		public object obj;
		[NonSerialized]
		public Type type;
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
					string resultCopyPath = Path.Combine(testDirectory, "resultCopy.txt");
					string checkPath = Path.Combine(testDirectory, "check.txt");

					Directory.CreateDirectory(testDirectory);
					if (!File.Exists(checkPath))
					{
						File.Create(checkPath).Close();
					}

					StringBuilder stringBuilder = new StringBuilder();
					Serialize(result, "", stringBuilder, level);

					string resultText = stringBuilder.ToString();
					File.WriteAllText(resultPath, resultText, Encoding.Default);
					File.WriteAllText(resultCopyPath, resultText, Encoding.Default);

					bool successful = Mono.ReadAllText(resultPath).Equals(Mono.ReadAllText(checkPath));
					//bool successful = File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath));

					if (!successful)
					{
						allTestsSucessful = false;
					}

					string durationText = duration.TotalSeconds.ToString();
					string successText;
					if (!successful)
					{
						successText = "failed";
					}
					else
					{
						successText = "succeeded";
					}
					Console.WriteLine(" " + successText + "  " + durationText + " s");
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
					new ParameterModifier[] { })!=null
			;
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
		public bool IsSmaller(SourcePosition other)
		{
			return this.Line<other.Line || (this.Line==other.Line && this.Column<other.Column);
		}
		public bool IsGreater(SourcePosition other)
		{
			return this.Line>other.Line || (this.Line==other.Line && this.Column>other.Column);
		}
		public bool IsBetween(Extent extent)
		{
			if(extent!=null)
			{
				if(this.IsGreater(extent.Start) || this.Equals(extent.Start))
				{
					if(this.IsSmaller(extent.End) || this.Equals(extent.End))
					{
						return true;
					}
				}
			}
			return false;

		}
		private int line;
		private int column;
		public SourcePosition(int line,int column)
		{
			this.line=line;
			this.column=column;

		}
		public override bool Equals(object obj)
		{
			return obj is SourcePosition && ((SourcePosition)obj).Line==Line && ((SourcePosition)obj).Column==Column;
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
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		public static List<Extent> GetExtents(string fileName,int firstLine,int lastLine)
		{
			List<Extent> result=new List<Extent>();
			foreach(KeyValuePair<Extent,Extent> entry in Extents)
			{
				Extent extent=(Extent)entry.Value;
				if(extent.Start.Line>=firstLine && extent.End.Line<=lastLine)
				{
					result.Add(extent);
				}
			}
			return result;
		}
		public static Dictionary<Extent,Extent> Extents
		{
			get
			{
				return extents;
			}
		}
		private static Dictionary<Extent,Extent> extents=new Dictionary<Extent,Extent>();
		public override bool Equals(object obj)
		{	
			bool isEqual=false;
			if(obj != null && obj is Extent)
			{
				Extent extent=(Extent)obj;
				if(
					extent.Start.Line==Start.Line && 
					extent.Start.Column==Start.Column && 
					extent.End.Line==End.Line && 
					extent.End.Column==End.Column
				)
				{
					isEqual=true;
				}
			}
			return isEqual;
		}
		public override int GetHashCode()
		{
			unchecked
			{
				return Start.Line.GetHashCode()*Start.Column.GetHashCode()*End.Line.GetHashCode()*End.Column.GetHashCode();
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
		private SourcePosition start;
		private SourcePosition end;
		public Extent(SourcePosition start, SourcePosition end,string fileName)
		{
			this.start = start;
			this.end = end;
			this.fileName = fileName;
		}
		private string fileName;
		public Extent(int startLine,int startColumn,int endLine,int endColumn,string fileName):this(new SourcePosition(startLine,startColumn),new SourcePosition(endLine,endColumn),fileName)
		{
		}
		public Extent CreateExtent(int startLine,int startColumn,int endLine,int endColumn,string fileName)
		{
			Extent extent=new Extent(startLine,startColumn,endLine,endColumn,fileName);
			if(!extents.ContainsKey(extent))
			{
				extents.Add(extent,extent);
			}
			return (Extent)extents[extent];
		}
	}
	public abstract class PersistantStrategy:MapStrategy
	{
		public abstract void Replace(Map value);
	}
	public class Syntax
	{
		static Syntax()
		{
			List<char> list = new List<char>(Syntax.lookupStringForbidden);
			list.AddRange(Syntax.lookupStringFirstForbiddenAdditional);
			Syntax.lookupStringFirstForbidden = list.ToArray();
		}
		public const char current='&';
		public const char scope='%';
		public const char argument='@';
		public const char negative='-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char indentation = '\t';
		public const char unixNewLine = '\n';
		public const string windowsNewLine = "\r\n";
		public const char function = '|';
		public const char @string = '\"';
		public static char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public const char lookupStart = '[';
		public const char lookupEnd = ']';
		public static char[] lookupStringForbidden = new char[] { call, indentation, '\r', '\n', statement, select, stringEscape, function, @string, lookupStart, lookupEnd, emptyMap, current, scope, argument };

		// remove???
		public static char[] lookupStringFirstForbiddenAdditional = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char[] lookupStringFirstForbidden;
		public const char emptyMap = '*';
		public const char call = ' ';
		public const char select = '.';

		public const char stringEscape = '\'';
		public const char statement = '=';
		public const char space = ' ';
		public const char tab = '\t';
	}
	public delegate Map ParseFunction(Parser parser, out bool matched);

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
		public Parser(string text, string filePath)
		{
			this.index = 0;
			this.text = text;
			this.file = filePath;
		}
		public bool isStartOfFile = true;
		public int functions = 0;
		public int indentationCount = -1;
		public abstract class Rule
		{

			public static implicit operator Rule(string text)
			{
				return new StringRule(text);
			}
			public static implicit operator Rule(char[] characters)
			{
				return new Character(characters);
			}
			public static implicit operator Rule(char character)
			{
				return new Character(character);
			}
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
			protected abstract Map MatchImplementation(Parser parser,out bool match);
		}
		public abstract class CharacterRule : Rule
		{
			public CharacterRule(char[] chars)
			{
				this.characters = chars;
			}
			protected char[] characters;
			protected abstract bool MatchCharacer(char c);
			protected override Map MatchImplementation(Parser parser,out bool matched)
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
			protected override Map MatchImplementation(Parser parser,out bool matched)
			{
				pre(parser);
				Map result = rule.Match(parser,out matched);
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
			protected override Map MatchImplementation(Parser parser,out bool matched)
			{
				List<Action> actions = new List<Action>();
				foreach (char c in text)
				{
					actions.Add(new Match(c));
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
			protected override Map MatchImplementation(Parser parser,out bool matched)
			{
				return parseFunction(parser,out matched);
			}
		}
		public delegate Rule RuleFunction();
		public class DelayedRule: Rule
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
				Map result=rule.Match(parser, out matched);
				match = !matched;
				return result;
			}
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
				Map result = new StrategyMap();
				bool success = true;
				foreach (Action action in actions)
				{
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
				Map list = new StrategyMap(new ListStrategy());
				while (true)
				{
					if(!action.Execute(parser, ref list))
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
		public class N: Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = new StrategyMap(new ListStrategy());
				int counter=0;
				while (true)
				{
					if(!action.Execute(parser, ref list))
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
			public N(int n,Action action)
			{
				this.n = n;
				this.action = action;
			}
		}
		public class OneOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = new StrategyMap(new ListStrategy());
				matched = false;
				while (true)
				{
					if(!action.Execute(parser, ref list))
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
			public static implicit operator Action(char character)
			{
				return new Match(new Character(character));
			}
			public static implicit operator Action(Rule rule)
			{
				return new Match(rule);
			}
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

		public static Rule Expression = new DelayedRule(delegate(){
			return new Alternatives(EmptyMap, Number, String, Program, Call, Select); });

		public Stack<int> defaultKeys = new Stack<int>();
		private int escapeCharCount = 0;
		private int GetEscapeCharCount()
		{
			return escapeCharCount;
		}

		private static Rule String = new Sequence(
			new Assignment(
				CodeKeys.Literal,
				new PrePost(
					delegate(Parser p) { },
					new Sequence(
						new ZeroOrMore(
							new Sequence(
								Syntax.stringEscape,
								new CustomRule(
									delegate(Parser p, out bool matched) {
										p.escapeCharCount++;
										matched = true;
										return null; }))),
						Syntax.@string,
						new ReferenceAssignment(
							new ZeroOrMore(
								new Autokey(
									new Sequence(
										new Not(
											new Sequence(
												Syntax.@string,
												new CustomRule(delegate(Parser p,out bool matched) {
													return new N(
														// use reflection to get at this? or do assignments in constructor, should be a property or a function that can be called, incremented or whatever
														p.escapeCharCount,
														Syntax.stringEscape).Match(p,out matched);}))),
										new ReferenceAssignment(new CharacterExcept()))))),
						Syntax.@string,
						new CustomRule(delegate(Parser p, out bool matched) { 
							return new StringRule("".PadLeft(p.escapeCharCount, Syntax.stringEscape)).Match(p, out matched); })),
					delegate(Parser p) { 
						p.escapeCharCount = 0; })));

		public static Rule Function = new PrePost(
			delegate(Parser parser) { parser.functions++; },
			new Sequence(
				Syntax.function,
				new Assignment(CodeKeys.Key, new LiteralRule(new StrategyMap(1, new StrategyMap(CodeKeys.Lookup, new StrategyMap(CodeKeys.Literal, CodeKeys.Function))))),
				new Assignment(CodeKeys.Value,
					new Sequence(new Assignment(CodeKeys.Literal, Expression)))), delegate(Parser parser) { parser.functions--; });

		private static Rule EndOfLine = 
			new Sequence(
				new ZeroOrMore(
					new Alternatives(
						Syntax.space,
						Syntax.tab)),
				new Alternatives(
					Syntax.unixNewLine,
					Syntax.windowsNewLine));

		private static Rule Indentation = 
			new Alternatives(
				new CustomRule(delegate(Parser p,out bool matched) {
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
					}}),
				new Sequence(
					new Sequence(
						EndOfLine,
						new CustomRule(delegate(Parser p,out bool matched) { 
							return new StringRule("".PadLeft(p.indentationCount + 1, Syntax.indentation)).Match(p,out matched); })),
					new CustomRule(delegate(Parser p,out bool matched){
						p.indentationCount++;
						matched = true;
						return null;})));

		private Rule Whitespace = 
			new ZeroOrMore(
				new Alternatives(
					Syntax.tab,
					Syntax.space));

		private static Rule EmptyMap = new Sequence(
			new Assignment(
				CodeKeys.Literal,
				new Sequence(
					Syntax.emptyMap,
					new ReferenceAssignment(
						new LiteralRule(Map.Empty)))));

		private static Rule LookupAnything = 
			new Sequence(
				Syntax.lookupStart,
				new ReferenceAssignment(Expression),
				new ZeroOrMore(Syntax.indentation),
				Syntax.lookupEnd);


		private static Rule Integer = 
			new Sequence(
				new Do(
					new Optional(Syntax.negative),
					delegate(Parser p, Map map, ref Map result) {
						p.negative = map != null;
						return null; }),
				new ReferenceAssignment(
					new Sequence(
						new ReferenceAssignment(
							new OneOrMore(new Do(Syntax.integer, delegate(Parser p, Map map, ref Map result){
								if (result == null)
								{
									result = new StrategyMap();
								}
								result = result.GetNumber() * 10 + (Number)map.GetNumber().GetInt32() - '0';
								return result;}))),
							new Do(
								new Nothing(),
								delegate(Parser p, Map map, ref Map result){
									if (result.GetNumber() > 0 && p.negative)
									{
										result = 0 - result.GetNumber();
									}
									return null;}))));


		private static Rule Number = 
			new Sequence(
				new Assignment(
					CodeKeys.Literal,
					new Sequence(
						new ReferenceAssignment(
							Integer),
						new OptionalAssignment(
							NumberKeys.Denominator,
							new Optional(
								new Sequence(
									Syntax.fraction,
									new ReferenceAssignment(
										Integer)))))));

		
		private static Rule LookupString = 
			new Sequence(
				new Assignment(
					CodeKeys.Literal,
					new OneOrMore(
						new Autokey(
							new CharacterExcept(
								Syntax.lookupStringForbidden)))));

		private static Rule Current = new Sequence(
			Syntax.current,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Current, Map.Empty))));


		private static Rule Scope = new Sequence(
			Syntax.scope,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Scope, Map.Empty))));

		private static Rule Argument = new Sequence(
			Syntax.argument,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Argument, Map.Empty))));


		// remove
		private static Rule CurrentLeft = new Sequence(
			Syntax.current,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Literal, SpecialKeys.This))));


		private static Rule ScopeLeft = new Sequence(
			Syntax.scope,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Literal, SpecialKeys.Scope))));

		private static Rule ArgumentLeft = new Sequence(
			Syntax.argument,
			new ReferenceAssignment(new LiteralRule(new StrategyMap(CodeKeys.Literal, SpecialKeys.Arg))));

		private static Rule LookupLeft =
			new Alternatives(
				CurrentLeft,
				ScopeLeft,
				ArgumentLeft,
				LookupString,
				LookupAnything);



		private static Rule Lookup = 
			new Alternatives(
				Current,
				Scope,
				Argument,
				new Sequence(
					new Assignment(
						CodeKeys.Lookup,
						new Alternatives(
							LookupString,
							LookupAnything))));

		private static Rule Search = new Sequence(
			new Assignment(
				CodeKeys.Search,
				new Alternatives(
					LookupString,
					LookupAnything)));


		private static Rule Select = new Sequence(
			new Assignment(
				CodeKeys.Select,
				new Sequence(
					new Assignment(
						1,
						new Alternatives(
							Search,
							Lookup)),
					new Appending(
						new ZeroOrMore(
							new Autokey(
								new Sequence(
									Syntax.select,
									new ReferenceAssignment(
										Lookup))))))));

		private static Rule Keys = new Sequence(
			new Assignment(
				1,
				Lookup),
			new Appending(
				new ZeroOrMore(
					new Autokey(
						new Sequence(
							Syntax.select,
							new ReferenceAssignment(
								Lookup))))));

		public static Rule Statement = new Sequence(
			new ReferenceAssignment(
				new Alternatives(Function,
					new Alternatives(
						new Sequence(
							new Assignment(
								CodeKeys.Key,
								Keys),
							Syntax.statement,
							new Assignment(
								CodeKeys.Value,
								Expression)),
						new Sequence(
							new Optional(
								Syntax.statement),
							new Assignment(
								CodeKeys.Value,
								Expression),
							new Assignment(
								CodeKeys.Key,
								new CustomRule(delegate(Parser p, out bool matched){
									Map map = new StrategyMap(1, new StrategyMap(CodeKeys.Lookup, new StrategyMap(CodeKeys.Literal, p.defaultKeys.Peek())));
									p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
									matched = true;
									return map;})))))),
			new CustomRule(delegate(Parser p, out bool matched){
				// i dont understand this
				if (EndOfLine.Match(p, out matched) == null && p.Look() != Syntax.endOfFile)
				{
					p.index -= 1;
					if (EndOfLine.Match(p, out matched) == null)
					{
						p.index -= 1;
						if (EndOfLine.Match(p, out matched) == null)
						{
							p.index += 2;
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
				return null;}));

		public static Rule Program = 
			new Sequence(
				Indentation,
				new Assignment(
					CodeKeys.Program,
					new PrePost(
						delegate(Parser p) {
							p.defaultKeys.Push(1); },
						new Sequence(
							new Assignment(
								1,
								Statement),
							new Appending(
								new ZeroOrMore(
									new Autokey(
										new Sequence(
											new Alternatives(
												new CustomRule(delegate(Parser pa,out bool matched) {
														return new StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa,out matched);}),
												new CustomRule(delegate(Parser pa,out bool matched) {
														pa.indentationCount--;
														matched = false;
														return null; })),
											new ReferenceAssignment(Statement)))))),
						delegate(Parser p) { 
							p.defaultKeys.Pop(); })));

		public static Rule Call = new Sequence(
			new Assignment(
				CodeKeys.Call,
				new Sequence(
					new Assignment(
						CodeKeys.Callable,
						Select),
					new Assignment(
						CodeKeys.Parameter,
						new Alternatives(
							new Sequence(
								Syntax.call, 
								new ReferenceAssignment(Expression)),
							Program)),
					new CustomRule(delegate(Parser p,out bool matched) {
						matched = p.functions != 0;
						return null; }))));
	}

	public class Serialize
	{
		public abstract class Rule
		{
			public abstract string Match(Map map, string indentation, out bool matched);
		}
		public static string Value(Map val)
		{
			return Value(val, null);
		}
		public class Literal:Rule
		{
			private Rule rule;
			private string literal;
			public Literal(char c,Rule rule):this(c.ToString(),rule)
			{
			}
			public Literal(string literal,Rule rule)
			{
				this.literal=literal;
				this.rule=rule;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				rule.Match(map,indentation,out matched);
				string text;
				if(matched)
				{
					text=literal;
				}
				else
				{
					text=null;
				}
				return text;
			}
		}
		private static Rule EmptyMap = new Literal(Syntax.emptyMap, new Set());
		private static string Value(Map val, string indentation)
		{
			string text;
			if (val is StrategyMap)
			{
				bool matched;
				if (val.Equals(Map.Empty))
				{
				}
				text = EmptyMap.Match(val,indentation, out matched);
				if (matched)
				{
				}
				//if (val.Equals(Map.Empty))
				//{
				//    text = Syntax.emptyMap.ToString();
				//}
				else if (val.IsString)
				{
					text = StringValue(val, indentation);
				}
				else if (val.IsNumber)
				{
					text = IntegerValue(val);
				}
				else
				{
					text = MapValue(val, indentation);
				}
			}
			else
			{
				text = val.ToString();
			}
			return text;
		}
		//private static string Value(Map val, string indentation)
		//{
		//    string text;
		//    if (val is StrategyMap)
		//    {
		//        if (val.Equals(Map.Empty))
		//        {
		//            text = Syntax.emptyMap.ToString();
		//        }
		//        else if (val.IsString)
		//        {
		//            text = StringValue(val, indentation);
		//        }
		//        else if (val.IsNumber)
		//        {
		//            text = IntegerValue(val);
		//        }
		//        else
		//        {
		//            text = MapValue(val, indentation);
		//        }
		//    }
		//    else
		//    {
		//        text = val.ToString();
		//    }
		//    return text;
		//}
		private static string IntegerValue(Map number)
		{
			return number.GetNumber().ToString();
		}
		public static string Key(Map key, string indentation)
		{
			string text;
			if (key.IsString && !key.Equals(Map.Empty))
			{
				text = StringKey(key, indentation);
			}
			else
			{

				text = Syntax.lookupStart.ToString();
				if (key.Equals(Map.Empty))
				{
					text += Syntax.emptyMap;
				}
				else if (key.IsNumber)
				{
					text += IntegerValue(key.GetNumber());
				}
				else
				{
					text += MapValue(key, indentation) + indentation;
				}
				text += Syntax.lookupEnd;
			}
			return text;
		}
		private static string StringKey(Map key, string indentation)
		{
			bool matched;
			string text=LiteralKey.Match(key,indentation,out matched);
			if (!matched)
			{
				text = Syntax.lookupStart + StringValue(key, indentation) + Syntax.lookupEnd;
			}
			return text;
		}
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
		public static  Rule LiteralKey = new OneOrMore(new CharacterExcept(Syntax.lookupStringForbidden));
		public static string MapValue(Map map, string indentation)
		{
			string text;
			text = Syntax.unixNewLine.ToString();
			if (indentation == null)
			{
				indentation = "";
			}
			else
			{
				indentation += Syntax.indentation;
			}
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				if (entry.Key.Equals(CodeKeys.Function) && entry.Value.Count == 1 && (entry.Value.ContainsKey(CodeKeys.Call) || entry.Value.ContainsKey(CodeKeys.Literal) || entry.Value.ContainsKey(CodeKeys.Program) || entry.Value.ContainsKey(CodeKeys.Select)))
				{
					bool matched;
					text += indentation + Syntax.function + Expression.Match(entry.Value, indentation,out matched);
					//text += indentation + Syntax.function + Expression(entry.Value, indentation);
					if (!text.EndsWith(Syntax.unixNewLine.ToString()))
					{
						text += Syntax.unixNewLine;
					}
				}
				else
				{
					text += indentation + Key((Map)entry.Key, indentation) + Syntax.statement + Value((Map)entry.Value, (indentation));
					if (!text.EndsWith(Syntax.unixNewLine.ToString()))
					{
						text += Syntax.unixNewLine;
					}
				}
			}
			return text;
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
		public static Rule Expression = new CustomRule(delegate(Map code,string indentation, out bool matched)
		{
			string text;
			if (code == null)
			{
			}
			if (code.ContainsKey(CodeKeys.Call))
			{
				text = Call(code[CodeKeys.Call], indentation);
			}
			else if (code.ContainsKey(CodeKeys.Program))
			{
				text = Program(code[CodeKeys.Program], indentation);
			}
			else if (code.ContainsKey(CodeKeys.Literal))
			{
				text = LiteralFunction(code[CodeKeys.Literal], indentation);
			}
			else if (code.ContainsKey(CodeKeys.Select))
			{
				text = Select(code[CodeKeys.Select], indentation);
			}
			else
			{
				throw new ApplicationException("Cannot serialize map.");
			}
			matched = true;
			return text;
		});
		//public static string Expression(Map code, string indentation)
		//{
		//    string text;
		//    if (code == null)
		//    {
		//    }
		//    if (code.ContainsKey(CodeKeys.Call))
		//    {
		//        text = Call(code[CodeKeys.Call], indentation);
		//    }
		//    else if (code.ContainsKey(CodeKeys.Program))
		//    {
		//        text = Program(code[CodeKeys.Program], indentation);
		//    }
		//    else if (code.ContainsKey(CodeKeys.Literal))
		//    {
		//        text = LiteralFunction(code[CodeKeys.Literal], indentation);
		//    }
		//    else if (code.ContainsKey(CodeKeys.Select))
		//    {
		//        text = Select(code[CodeKeys.Select], indentation);
		//    }
		//    else
		//    {
		//        throw new ApplicationException("Cannot serialize map.");
		//    }
		//    return text;
		//}
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
		public static string Call(Map code, string indentation)
		{
			Map callable = code[CodeKeys.Callable];
			Map argument = code[CodeKeys.Parameter];
			bool matched;
			string text = Expression.Match(callable, indentation,out matched);

			if (!(argument.ContainsKey(CodeKeys.Program) && argument[CodeKeys.Program].Count != 0))
			{
				text += Syntax.call;
			}
			else
			{
			}
			text += Expression.Match(argument, indentation,out matched);
			return text;
		}
		public static string Program(Map code, string indentation)
		{
			string text;
			if (code.Array.Count == 0)
			{
				text = "*";
			}
			else
			{
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
			}
			return text;
		}
		public static string Statement(Map code, string indentation, ref int autoKeys)
		{
			Map key = code[CodeKeys.Key];
			string text;
			if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Lookup) && key[1][CodeKeys.Lookup].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Lookup][CodeKeys.Literal].Equals(CodeKeys.Function) && code[CodeKeys.Value].ContainsKey(CodeKeys.Literal))
			{
				bool matched;
				text = indentation + Syntax.function + Expression.Match(code[CodeKeys.Value][CodeKeys.Literal], indentation,out matched);
			}
			else
			{
				Map autoKey;
				text = indentation;
				Map value = code[CodeKeys.Value];
				if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Lookup) && key[1][CodeKeys.Lookup].ContainsKey(CodeKeys.Literal) && (autoKey = key[1][CodeKeys.Lookup][CodeKeys.Literal]) != null && autoKey.IsNumber && autoKey.GetNumber() == autoKeys + 1)
				{
					autoKeys++;
					if (value.ContainsKey(CodeKeys.Program) && value[CodeKeys.Program].Count != 0)
					{
						text += Syntax.statement;
					}
				}
				else
				{
					text += Select(code[CodeKeys.Key], indentation) + Syntax.statement;
				}
				bool matched;
				text += Expression.Match(value, indentation,out matched);
			}
			return text;
		}
		public static string LiteralFunction(Map code, string indentation)
		{
			return Value(code, indentation);
		}
		public static string Select(Map code, string indentation)
		{
			string text = Lookup(code[1], indentation);
			for (int i = 2; code.ContainsKey(i); i++)
			{
				text += Syntax.select + Lookup(code[i], indentation);
			}
			return text;
		}

		public static string Lookup(Map code, string indentation)
		{
			string text;
			Map lookup;
			if ((lookup = code[CodeKeys.Search]) != null || (lookup = code[CodeKeys.Lookup]) != null)
			{
				if (lookup.ContainsKey(CodeKeys.Literal))
				{
					text = Key(lookup[CodeKeys.Literal], indentation);
				}
				else
				{
					bool matched;
					text = Syntax.lookupStart + Expression.Match(lookup, indentation,out matched);
					if (lookup.ContainsKey(CodeKeys.Program) && lookup[CodeKeys.Program].Count != 0)
					{
						text += indentation;
					}
					text += Syntax.lookupEnd;
				}
			}
			else
			{
				bool matched;
				text = Current.Match(code,indentation, out matched);
				if (!matched)
				{
					text = Argument.Match(code,indentation, out matched);
				}
				if (!matched)
				{
					text = Scope.Match(code,indentation, out matched);
				}
				if (matched)
				{
				}
				else if (code.ContainsKey(CodeKeys.Literal))
				{
					text = Key(code[CodeKeys.Literal], indentation);
				}
				else
				{
					text = Syntax.lookupStart + Expression.Match(code, indentation,out matched) + Syntax.lookupEnd;
				}
			}
			return text;
		}

		public static Rule Current = new Equal(new StrategyMap(CodeKeys.Current, Map.Empty), Syntax.current.ToString());
		public static Rule Argument = new Equal(new StrategyMap(CodeKeys.Argument, Map.Empty), Syntax.argument.ToString());
		public static Rule Scope = new Equal(new StrategyMap(CodeKeys.Scope, Map.Empty), Syntax.scope.ToString());
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




		private static string StringValue(Map val, string indentation)
		{
			string text;
			if (val.IsString)
			{
				int longestMatch = 0;
				if (val.GetString().IndexOf("'n'") != -1)
				{
				}
				string mapString = val.GetString();
				string[] split = mapString.Split(Syntax.@string);
				for (int i = 1; i < split.Length; i++)
				{
					int matchLength = split[i].Length - split[i].TrimStart(Syntax.stringEscape).Length + 1;
					if (matchLength > longestMatch)
					{
						longestMatch = matchLength;
					}
				}
				string escape = "";
				for (int i = 0; i < longestMatch; i++)
				{
					escape += Syntax.stringEscape;
				}
				text = escape + Syntax.@string;
				string[] lines = val.GetString().Split(new string[] { Syntax.unixNewLine.ToString(), Syntax.windowsNewLine }, StringSplitOptions.None);
				text += lines[0];
				for (int i = 1; i < lines.Length; i++)
				{
					text += Syntax.unixNewLine + indentation + Syntax.indentation + lines[i];
				}
				text += Syntax.@string + escape;
			}
			else
			{
				text = MapValue(val, indentation);
			}
			return text;
		}

	}

	public class FileSystem
	{
		// refactor
		private static bool parsing=false;
		public static bool Parsing
		{
			get
			{
				return parsing;
			}
			set
			{
				parsing = value;
			}
		}
		//private static void MakePersistant(StrategyMap map)
		//{
		//    map.Persistant = true;
		//    foreach (KeyValuePair<Map, Map> pair in map)
		//    {
		//        if (pair.Value is StrategyMap)
		//        {
		//            StrategyMap normalMap = (StrategyMap)pair.Value;
		//            if (normalMap.Strategy is DictionaryStrategy || (normalMap.Strategy is CloneStrategy && ((CloneStrategy)normalMap.Strategy).data is DictionaryStrategy))
		//            {
		//                MakePersistant((StrategyMap)pair.Value);
		//            }
		//        }
		//    }
		//}
		// rename, refactor
		public static Map ParseFile(string filePath)
		{
			using (TextReader reader = new StreamReader(filePath, Encoding.Default))
			{
				Map parsed = ParseFile(reader,filePath);
				// todo: reintroduce this
				//MakePersistant((StrategyMap)parsed);
				parsed.Scope = FileSystem.fileSystem;
				//parsed.Scope = null;
				return parsed;
			}
		}
		public static Map ParseFile(TextReader textReader,string fileName)
		{
			Parsing = true;
			Map result=Compile(textReader,fileName).GetExpression().Evaluate(Map.Empty);
			Parsing = false;
			return result;
		}
		public static Map Compile(TextReader textReader,string fileName)
		{
			string text=textReader.ReadToEnd();
			Map result;
			if (text == "")
			{
				result = new StrategyMap(CodeKeys.Program, Map.Empty);
			}
			else
			{
				Parser parser = new Parser(text, fileName);
				bool matched;
				result = Parser.Program.Match(parser,out matched);
				if (parser.index != parser.text.Length)
				{
					throw new SyntaxException("Expected end of file.", parser);
				}
			}
			return result;
		}
		public static Map fileSystem;
		private static Map LoadDirectory(string path)
		{
			Map map = new StrategyMap();
			foreach (string fileName in Directory.GetFiles(path, "*.meta"))
			{
				map[Path.GetFileNameWithoutExtension(fileName)] = FileSystem.ParseFile(fileName);
			}
			foreach (string directoryName in Directory.GetDirectories(path))
			{
				if ((new DirectoryInfo(directoryName).Attributes & FileAttributes.Hidden) == 0)
				{
					map[new DirectoryInfo(directoryName).Name] = LoadDirectory(directoryName);
				}
			}
			return map;
		}
		static FileSystem()
		{
			fileSystem = LoadDirectory(Path.Combine(Process.InstallationPath, "Library"));
			fileSystem.Scope = Gac.gac;
			Gac.gac["local"] = fileSystem;
		}
		public static void Save()
		{
			string text = Serialize.Value(fileSystem).Trim(new char[] { '\n' });
			if (text == "\"\"")
			{
				text = "";
			}
			File.WriteAllText(System.IO.Path.Combine(Process.InstallationPath,"meta.meta"), text,Encoding.Default);
		}
	}
	public class Gac: StrategyMap
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
					Assembly assembly = Assembly.LoadWithPartialName(assemblyName);
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
		private Map LoadAssembly(Assembly assembly)
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
						name=type.Name.Split('`')[0];
					}
					else
					{
						name=type.Name;
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
				val = null;
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
			return Load(key);
		}
		protected Map cachedAssemblyInfo = new StrategyMap();
	}
	public class Number
	{
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
		private readonly double numerator;
		private readonly double denominator;



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
			if (a.Denominator != 1.0)
			{
			}
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
			private static string BasicTest
			{
				get
				{
					return Path.Combine(Process.InstallationPath, "basicTest.meta");
				}
			}
			private static string LibraryTest
			{
				get
				{
					return Path.Combine(Process.InstallationPath, "libraryTest.meta");
				}
			}
			//public class Extents : Test
			//{
			//    public override object GetResult(out int level)
			//    {
			//        level = 1;
			//        Map argument = Map.Empty;
			//        return FileSystem.ParseFile(BasicTest);
			//    }
			//}
			//public class Basic : Test
			//{
			//    public override object GetResult(out int level)
			//    {
			//        level = 2;
			//        Map argument = new StrategyMap();
			//        argument[1] = "first arg";
			//        argument[2] = "second=arg";
			//        return FileSystem.ParseFile(BasicTest).Call(argument);
			//    }
			//}
			//public class Library : Test
			//{
			//    public override object GetResult(out int level)
			//    {
			//        level = 2;
			//        return FileSystem.ParseFile(LibraryTest).Call(Map.Empty);
			//    }
			//}
			public class Serialization : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					Map map = FileSystem.ParseFile(BasicTest);
					bool matched;
					return Meta.Serialize.Value(map);
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

