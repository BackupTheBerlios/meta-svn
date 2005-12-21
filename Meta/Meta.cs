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
using System.CodeDom.Compiler;
using System.Xml;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CSharp;
using System.Windows.Forms;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Meta
{
	public class CodeKeys
	{
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
	public class SpecialKeys
	{
		public static readonly Map Scope = "scope";
		public static readonly Map Parent="parent";
		public static readonly Map Arg="arg";
		public static readonly Map Current="current";
		public static readonly Map Net = "net";
		public static readonly Map Local = "local";
	}
	public class DotNetKeys
	{
		public static readonly Map Add="add";
		public static readonly Map Remove="remove";
		public static readonly Map Get="get";
		public static readonly Map Set="set";
	}
	public class MetaException:ApplicationException
	{
		public List<string> Stack
		{
			get
			{
				return stack;
			}
		}
		private List<string> stack = new List<string>();
		public override string ToString()
		{
			return Message + "\n" +string.Join("\n",stack.ToArray());
		}
		public static string GetExtentText(Extent extent)
		{
			return "Line " + extent.Start.Line + ", column " + extent.Start.Column + ": ";
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
				string text;
				if (extent != null)
				{
					text = GetExtentText(extent);
				}
				else
				{
					text = "Unknown location: ";
				}
				text += message;
				return text;
            }
        }
        private string message;
		private Extent extent;
	}
	public class Throw
	{
		public static void KeyDoesNotExist(Map key,Extent extent)
		{
			throw new MetaException("The key "+FileSystem.Serialize.Value(key)+" does not exist.",extent);
		}
		public static void KeyNotFound(Map key,Extent extent)
		{
			throw new MetaException("The key "+FileSystem.Serialize.Value(key)+" could not be found.",extent);
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
			//Map current = Map.Empty;
			Map current = new StrategyMap();
			current.Parent = context;
			current.Argument = argument;
			return EvaluateImplementation(current);
		}
		public abstract Map EvaluateImplementation(Map context);//, Map arg);
		//public abstract Map Evaluate(Map context, Map arg);
	}
	public class Call : Expression
	{
		private Map callable;
		public Map parameter;
		public Call(Map code)
		{
			this.callable = code[CodeKeys.Callable];
			this.parameter = code[CodeKeys.Argument];
		}
		public override Map EvaluateImplementation(Map current)//, Map arg)
		{
			Map function = callable.GetExpression().Evaluate(current);
			//Map function = callable.GetExpression().Evaluate(current, arg);
			if (!function.IsFunction)
			{
				throw new MetaException("Called map is not a function.", callable.Extent);
			}
			Map argument = parameter.GetExpression().Evaluate(current);
			Map result;
			try
			{
				result = function.Call(argument);
			}
			catch (MetaException e)
			{
				e.Stack.Add(MetaException.GetExtentText(callable.Extent)+"Function has thrown an exception.");
				throw e;
			}
			if (result == null)
			{
				result = Map.Empty.Copy();
			}
			// messy
			Map clone = result.Copy();
			clone.Parent = null;
			clone.Scope = null;
			return clone;
		}
	}
	public class Program : Expression
	{
		private List<Map> statements;
		public Program(Map code)
		{
			statements = code.Array;
		}
		//public override Map EvaluateImplementation(Map context)//, Map arg)
		//{
		//    (context, ref local);
		//}
		public override Map EvaluateImplementation(Map current)//, ref Map current)//, Map arg)
		{
			//current.Parent = parent;
			foreach (Map statement in statements)
			{
				statement.GetStatement().Assign(ref current);
			}
			return current;
		}
		//public override Map EvaluateImplementation(Map context)//, Map arg)
		//{
		//    Map local = new StrategyMap();
		//    EvaluateImplementation(context, ref local);
		//    return local;
		//}
		//private void EvaluateImplementation(Map parent, ref Map current)//, Map arg)
		//{
		//    current.Parent = parent;
		//    foreach (Map statement in statements)
		//    {
		//        statement.GetStatement().Assign(ref current);
		//    }
		//}
	}
	public class Literal : Expression
	{
		private Map literal;
		public Literal(Map code)
		{
			this.literal = code;
		}
		public override Map EvaluateImplementation(Map context)//, Map arg)
		{
			return literal.Copy();
		}
	}
	public class Select : Expression
	{
		private Map FindFirstKey(Map keyExpression, Map context)//, Map arg)
		{
			Map key = keyExpression.GetExpression().Evaluate(context);//, arg);
			Map val;
			//if (key.Equals(new StrategyMap("library")))
			//{
			//}
			if (key.Equals(SpecialKeys.Scope))
			{
				val = context.Scope;
			}
			else if (key.Equals(SpecialKeys.Arg))
			{
				val = context.Argument;
				//val = arg;
			}
			else if (key.Equals(SpecialKeys.Parent))
			{
				val = context.Parent;
			}
			else if (key.Equals(SpecialKeys.Current))
			{
				val = context;
			}
			else
			{
				Map selected = context;
				while (!selected.ContainsKey(key))
				{
					selected = selected.Scope;

					if (selected == null)
					{
						Throw.KeyNotFound(key, keyExpression.Extent);
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
		public override Map EvaluateImplementation(Map context)//, Map arg)
		{
			Map selected = FindFirstKey(keys[0], context);//, arg);
			for (int i = 1; i<keys.Count; i++)
			{
				Map key = keys[i].GetExpression().Evaluate(context);//, arg);
				Map selection;

				// maybe combine this stuff with the stuff in FindFirstKey???
				if (key.Equals(SpecialKeys.Scope))
				{
					selection = selected.Scope;
				}
				else if (key.Equals(SpecialKeys.Arg))
				{
					selection = selected.Argument;
					//selection = arg;
				}
				else if (key.Equals(SpecialKeys.Parent))
				{
					selection = selected.Parent;
				}
				else
				{
					selection = selected[key];
				}
				if (selection == null)
				{
					object x = selected["Item"];
					Throw.KeyDoesNotExist(key, keys[i].Extent);
				}
				selected = selection;
			}
			return selected;
		}
	}
	public class Statement
	{
		// not quite correct, we should rather just cache the array in maps
		List<Map> keys;
		public Map value;
		public Statement(Map code)
		{
			this.keys = code[CodeKeys.Key].Array;
			this.value = code[CodeKeys.Value];
		}
		public void Assign(ref Map context)//, Map arg)
		{
			Map selected = context;
			Map key;
			int i = 0;
			for (; i+1<keys.Count;)
			{
				key = keys[i].GetExpression().Evaluate(context);//, arg);
				Map selection;
				if (key.Equals(SpecialKeys.Parent))
				{
					selection = selected.Parent;
				}
				else
				{
					selection = selected[key];
				}

				if (selection == null)
				{
					Throw.KeyDoesNotExist(key, keys[0].Extent);
				}
				selected = selection;
				i++;
			}
			Map lastKey = keys[i].GetExpression().Evaluate(context);//, arg);
			Map val = value.GetExpression().Evaluate(context);//, arg);

			if (lastKey.Equals(SpecialKeys.Current))
			{
				val.Scope = context.Scope;
				val.Parent = context.Parent;
				val.Argument = context.Argument;
				context = val;
			}
			else
			{
				selected[lastKey] = val;
			}
		}
	}
	public class Library
	{
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
			Integer counter = 1;
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
				arg["with"].Call(Map.Empty);
			}
			return Map.Empty;
		}
	}
	public class Process
	{
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
            string metaDllLocation = Assembly.GetAssembly(typeof(Map)).Location;
            loadedAssemblies.AddRange(new string[] { metaDllLocation });
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
		public static string InstallationPath
		{
			get
			{
				Uri uri=new Uri(Assembly.GetAssembly(typeof(Map)).CodeBase);
				return Path.GetDirectoryName(uri.AbsolutePath);
				//return Application.StartupPath;
			}
		}
		//public static string InstallationPath
		//{
		//    get
		//    {
		//        return Application.StartupPath;
		//    }
		//}
		//public static string InstallationPath
		//{
		//    get
		//    {
		//        return @"c:\Projects\meta\Meta";
		//    }
		//}
		public static List<string> loadedAssemblies=new List<string>();
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
		private Map argument=null;
		public abstract bool IsFunction
		{
			get;
		}
		//public bool IsInterned
		//{
		//    get
		//    {
		//        return isInterned;
		//    }
		//}
		//private bool isInterned = false;
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
				text += FileSystem.Serialize.Key(key,"") + " ";
			}
			return text;
		}
		public Map Current
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
			else if (this.IsInteger)
			{
				text = this.GetInteger().ToString();
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
				return IsInteger && (GetInteger()==0 || GetInteger()==1);
			}
		}
		public virtual bool GetBoolean()
		{
			bool boolean;
			if(GetInteger()==0)
			{
				boolean=false;
			}
			else if(GetInteger()==1)
			{
				boolean=true;
			}
			else
			{
				throw new ApplicationException("Map is not a boolean.");
			}
			return boolean;
		}
		public virtual bool IsInteger
		{
			get
			{
				return IsIntegerDefault;
			}
		}
		public bool IsIntegerDefault
		{
			get
			{
				try
				{
					return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this[Map.Empty].IsInteger);
				}
				catch(Exception e)
				{
					int asfd;
					return ContainsKey(Map.Empty) && this[Map.Empty].IsInteger;
				}
			}
		}
		public virtual Integer GetInteger()
		{
			return GetIntegerDefault();
		}
		public Integer GetIntegerDefault()
		{
			Integer number;
			if(this.Equals(Map.Empty))
			{
				number=0;
			}
			else if(this.Count==1 && this.ContainsKey(Map.Empty) && this[Map.Empty].GetInteger()!=null)
			{
				number = 1 + this[Map.Empty].GetInteger();
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
				text+=Convert.ToChar(this[key].GetInteger().GetInt32());
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
		public virtual Map Parent
		{
			get
			{
				return parent;
			}
			set
			{
                if (parent == null && Scope== null)
                {
                    Scope = value;
                }
				parent=value;
			}
		}
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		// rename to Length?
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
					expression = null;
					statement = null;
                    Map val = value.Copy();
                    val.Parent = this;
                    Set(key, val);
                }
            }
        }
		private static Dictionary<Map,Map>internedKeys=new Dictionary<Map,Map>();
		//private static Map Intern(Map key)
		//{
		//    if (!internedKeys.ContainsKey(key))
		//    {
		//        key.isInterned=true;
		//        internedKeys[key] = key;
		//    }
		//    else
		//    {
		//    }
		//    Map result=internedKeys[key];
		//    if (!result.isInterned)
		//    {
		//    }
		//    return result;
		//}
        protected abstract Map Get(Map key);
        protected abstract void Set(Map key, Map val);
		public virtual Map Call(Map arg)
		{
			Map function = this[CodeKeys.Function];
			Map result = function.GetExpression().Evaluate(this,arg);//, arg);
			//Map result = function.GetExpression().Evaluate(this);//, arg);
			return result;
		}
		public abstract ICollection<Map> Keys
		{
			get;
		}
		public Map Copy()
		{
			Map clone = CopyImplementation();
			clone.Scope = Scope;
			clone.Parent = Parent;
			clone.Extent = Extent;
			return clone;
		}
		protected virtual Map CopyImplementation()
		{
			Map clone = new StrategyMap();
			foreach (Map key in this.Keys)
			{
				clone[key] = this[key];
			}
			return clone;
		}
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
			if (IsInteger)
			{
				return (int)(GetInteger().integer % int.MaxValue);
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
		public static implicit operator Map(Integer integer)
		{
			return new StrategyMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new StrategyMap(new Integer((int)(boolean?1:0)));
		}
		public static implicit operator Map(char character)
		{
			return new StrategyMap(new Integer(character));
		}
		public static implicit operator Map(byte integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(int integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(long integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new StrategyMap(new Integer(integer));
		}
		public static implicit operator Map(string text)
		{
			return new StrategyMap(text);
		}
	}
	public class ListStrategy : MapStrategy
	{
		public override bool Equal(MapStrategy strategy)
		{
		    bool equal;
		    if (strategy is ListStrategy)
		    {
				if (Count == strategy.Count)
				{
					equal = true;
					ListStrategy listStrategy = (ListStrategy)strategy;
					for (int i = 0; i < values.Count; i++)
					{
						if (!this.values[i].Equals(listStrategy.values[i]))
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
		    }
		    else
		    {
		        equal = base.Equal(strategy);
		    }
		    return equal;
		}
		public override int Count
		{
			get
			{
				return values.Count;
			}
		}
		public override void AppendMap(Map array)
		{
			foreach (Map map in array.Array)
			{
				this.values.Add(map.Copy());
			}
		}
		public override List<Map> Array
		{
			get
			{
				return this.values;
			}
		}
		public override int GetArrayCount()
		{
			return this.values.Count;
		}
		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsInteger)
			{
				Integer integer = key.GetInteger();
				if (integer>=1 && integer <= values.Count)
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
		private List<Map> values;
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				int counter=1;
				foreach (Map value in values)
				{
					keys.Add(new StrategyMap(counter));
					counter++;
				}
				return keys;
			}
		}
		public ListStrategy()
		{
			this.values=new List<Map>();
		}
		public ListStrategy(string text)
		{
			this.values = new List<Map>(text.Length);
			foreach (char c in text)
			{
				this.values.Add(new StrategyMap(c));
			}
		}
		public ListStrategy(ListStrategy original)
		{
			this.values=new List<Map>(original.values);
		}
		public override MapStrategy CopyImplementation()
		{
			return new ListStrategy(this);
		}
		public override Map Get(Map key)
		{
			Map value=null;
			if (key.IsInteger)
			{
				int integer = key.GetInteger().GetInt32();
				if (integer >= 1 && integer <= values.Count)
				{
					value = values[integer - 1];
				}
			}
			return value;
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsInteger)
			{
				int integer = key.GetInteger().GetInt32();
				if (integer >= 1 && integer <= values.Count)
				{
					values[integer-1] = val;
				}
				else if (integer == values.Count + 1)
				{
					values.Add(val);
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
	public class EmptyStrategy : MapStrategy
	{
		public override ICollection<Map> Keys
		{
			get
			{
				return new List<Map>(0);
			}
		}
		public override MapStrategy CopyImplementation()
		{
			return new EmptyStrategy();
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsInteger)
			{
				if (key.Equals(Map.Empty) && val.IsInteger)
				{
					Panic(key, val, new IntegerStrategy(0));
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
	public class StrategyMap:Map
	{
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
		public override Integer GetInteger()
		{
			return strategy.GetInteger();
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
		public override bool IsInteger
		{
			get
			{
				return strategy.IsInteger;
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
			return strategy.Get(key);
		}
		protected override void Set(Map key, Map value)
		{
			isHashCached = false;
			//if (key.Equals(SpecialKeys.Current))
			//{
			//    // refactor
			//    this.strategy = ((StrategyMap)value).strategy.CopyImplementation();
			//    this.strategy.map = this;
			//}
			//else
			//{
				strategy.Set(key, value);
			//}
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
		protected override Map CopyImplementation()
		{
			// why does the strategy do the cloning?
			return strategy.Copy();
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
		private bool isHashCached = false;
		private int hash;
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
		private MapStrategy strategy;
		// should use EmptyStrategy by default
		public StrategyMap(ICollection<Map> list)
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
		public StrategyMap():this(new DictionaryStrategy())
		{
		}
		public StrategyMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public StrategyMap(string text)
			: this(new ListStrategy(text))
		{
		}
	}
	public class RemoteStrategy : MapStrategy
	{
		public override MapStrategy CopyImplementation()
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return null;
			}
		}
		public override Map Get(Map key)
		{
			if(!key.IsString)
			{
				throw new ApplicationException("key is not a string");
			}
			WebClient webClient=new WebClient();
			Uri fullPath=new Uri(new Uri("http://"+address),key.GetString()+".meta");
			Stream stream=webClient.OpenRead(fullPath.ToString());
			StreamReader streamReader=new StreamReader(stream);
			return FileSystem.Parse(streamReader);
		}
		public override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot set key in remote map.");
		}
		private string address;
		public RemoteStrategy(string address)
		{
			this.address=address;
		}
	}
	// this ist not used yet
	public class NetStrategy:MapStrategy
	{
		public static readonly StrategyMap Net = new StrategyMap(new NetStrategy());
		private NetStrategy()
		{
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return new List<Map>();
			}
		}
        public override Map  Get(Map key)
        {
            if (!key.IsString)
            {
                throw new ApplicationException("need a string here");
            }
			Map val;
			if (key.Equals(SpecialKeys.Local))
			{
				val = FileSystem.fileSystem;
			}
			else
			{
				val=new StrategyMap(new RemoteStrategy(key.GetString()));
			}
			return val;
        }
        public override void Set(Map key, Map val)
        {
			throw new ApplicationException("Cannot set key in Web.");
        }
		public override MapStrategy CopyImplementation()
		{
			throw new ApplicationException("Not implemented.");
		}
		public static NetStrategy singleton=new NetStrategy();
	}

	public class Transform
	{
		public static object ToDotNet(Map meta,Type target)
		{
			object dotNet=null;
			if((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
				&& meta.ContainsKey(CodeKeys.Function))
			{
				MethodInfo invoke=target.GetMethod("Invoke",
					BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate function=Method.CreateDelegateFromCode(target,meta);
				dotNet=function;
			}
			else if(target.IsArray && meta.Array.Count!=0)
			{
				Type type=target.GetElementType();
				Array arguments=System.Array.CreateInstance(type,meta.Array.Count);
				bool isElementConverted=true;
				for(int i=0;i<meta.Count;i++)
				{
					object element = Transform.ToDotNet(meta[i + 1], type);
					if (element!=null)
					{
						arguments.SetValue(element,i);
					}
					else
					{
						isElementConverted = false;
						break;
					}
				}
				if(isElementConverted)
				{
					dotNet=arguments;
				}
			}
			else if(target.IsSubclassOf(typeof(Enum)) && meta.IsInteger)
			{ 
				dotNet=Enum.ToObject(target,meta.GetInteger().GetInt32());
			}
			else 
			{
				switch(Type.GetTypeCode(target))
				{
					case TypeCode.Boolean:
						if(IsIntegerInRange(meta,0,1))
						{
							if(meta.GetInteger()==0)
							{
								dotNet=false;
							}
							else if(meta.GetInteger()==1)
							{
								dotNet=true;
							}
						}
						break;
					case TypeCode.Byte:
						if(IsIntegerInRange(meta,new Integer(Byte.MinValue),new Integer(Byte.MaxValue)))
						{
							dotNet=Convert.ToByte(meta.GetInteger().GetInt32());
						}
						break;
					case TypeCode.Char:
						if(IsIntegerInRange(meta,(int)Char.MinValue,(int)Char.MaxValue))
						{
							dotNet=Convert.ToChar(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.DateTime:
						dotNet = null;
						break;
					case TypeCode.DBNull:
						if(meta.IsInteger && meta.GetInteger()==0)
						{
							dotNet=DBNull.Value;
						}
						break;
					case TypeCode.Decimal:
						if(IsIntegerInRange(meta,new Integer((double)decimal.MinValue),new Integer((double)decimal.MaxValue)))
						{
							dotNet=(decimal)(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Double:
						if(IsIntegerInRange(meta,new Integer(double.MinValue),new Integer(double.MaxValue)))
						{
							dotNet=(double)(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Int16:
						if(IsIntegerInRange(meta,Int16.MinValue,Int16.MaxValue))
						{
							dotNet=Convert.ToInt16(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Int32:
						if(IsIntegerInRange(meta,(Integer)Int32.MinValue,Int32.MaxValue))
							{
							dotNet=meta.GetInteger().GetInt32();
						}
						break;
					case TypeCode.Int64:
						if(IsIntegerInRange(meta,new Integer(Int64.MinValue),new Integer(Int64.MaxValue)))
						{
							dotNet=Convert.ToInt64(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Object:
						if(meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).type))
						{
							dotNet=((ObjectMap)meta).obj;
						}
						else if(target.IsAssignableFrom(meta.GetType()))
						{
							dotNet=meta;
						}
						break;
					case TypeCode.SByte:
						if(IsIntegerInRange(meta,(Integer)SByte.MinValue,(Integer)SByte.MaxValue))
						{
							dotNet=Convert.ToSByte(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Single:
						if(IsIntegerInRange(meta,new Integer(Single.MinValue),new Integer(Single.MaxValue)))
						{
							dotNet=(float)meta.GetInteger().GetInt64();
						}
						break;
					case TypeCode.String:
						if(meta.IsString)
						{
							dotNet=meta.GetString();
						}
						break;
					case TypeCode.UInt16:
						if(IsIntegerInRange(meta,new Integer(UInt16.MinValue),new Integer(UInt16.MaxValue)))
						{
							dotNet=Convert.ToUInt16(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.UInt32:
						if(IsIntegerInRange(meta,new Integer(UInt32.MinValue),new Integer(UInt32.MaxValue)))
						{
							dotNet=Convert.ToUInt32(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.UInt64:
						if(IsIntegerInRange(meta,new Integer(UInt64.MinValue),new Integer(UInt64.MaxValue)))
						{
							dotNet=Convert.ToUInt64(meta.GetInteger().GetInt64());
						}
						break;
					default:
						throw new ApplicationException("not implemented");
				}
			}
			return dotNet;
		}
		public static bool IsIntegerInRange(Map meta,Integer minValue,Integer maxValue)
		{
			return meta.IsInteger && meta.GetInteger()>=minValue && meta.GetInteger()<=maxValue;
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

	public class Method: Map
	{
		public override bool IsFunction
		{
			get
			{
				return true;
			}
		}
		protected override Map CopyImplementation()
		{
			return new Method(this.name,this.obj,this.type);
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
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot set key in Method");
		}
		// refactor
		public class ArgumentComparer: IComparer<MethodBase>
		{
			public static ArgumentComparer singleton = new ArgumentComparer();
			public int Compare(MethodBase x, MethodBase y)
			{
				int result;
				MethodBase first=(MethodBase)x;
				MethodBase second=(MethodBase)y;
				ParameterInfo[] firstParameters=first.GetParameters();
				ParameterInfo[] secondParameters=second.GetParameters();
				if(firstParameters.Length>=1 && firstParameters[0].ParameterType==typeof(string)
					&& !(secondParameters.Length>=1 && secondParameters[0].ParameterType==typeof(string)))
				{
					result=-1;
				}
				else
				{
					result=0;
				}
				return result;
			}
		}
		public override Map Call(Map argument)
		{
			object result = null;
			bool isExecuted = false;
			List<MethodBase> rightNumberArgumentMethods = new List<MethodBase>();
			int count = argument.ArrayCount;
			if (count == argument.Count)
			{
				foreach (MethodBase method in overloadedMethods)
				{
					if (count == method.GetParameters().Length)
					{
						rightNumberArgumentMethods.Add(method);
					}
				}
			}
			if (rightNumberArgumentMethods.Count == 0)
			{
				throw new ApplicationException("Method " + this.type.Name + "." + this.name + ": No methods with the right number of arguments.");
			}
			if (rightNumberArgumentMethods.Count > 1)
			{
				rightNumberArgumentMethods.Sort(ArgumentComparer.singleton);
			}
			foreach (MethodBase method in rightNumberArgumentMethods)
			{
				List<object> arguments = new List<object>();
				bool argumentsMatched = true;
				ParameterInfo[] parameters = method.GetParameters();
				for (int i = 0; argumentsMatched && i < parameters.Length; i++)
				{
					object arg = Transform.ToDotNet(argument[i+1], parameters[i].ParameterType);
					if (arg != null)
					{
						arguments.Add(arg);
					}
					else
					{
						argumentsMatched = false;
						break;
					}
				}
				if (argumentsMatched)
				{
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
					isExecuted = true;
					break;
				}
			}
			if (!isExecuted)
			{
				throw new ApplicationException("Method " + this.name + " could not be called.");
			}
			return Transform.ToMeta(result);
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
			public MetaDelegate(Map callable,Type returnType)
			{
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments)
			{
				Map arg = new StrategyMap();
				foreach (object argument in arguments)
				{
					arg.Append(Transform.ToMeta(argument));
				}
				Map result = this.callable.Call(arg);
				return Meta.Transform.ToDotNet(result, this.returnType);
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
		private void Initialize(string name,object obj,Type type)
		{
			this.name=name;
			this.obj=obj;
			this.type=type;
			List<MemberInfo> methods;
			if(name==".ctor")
			{
				methods=new List<MemberInfo>(type.GetConstructors());
			}
			else
			{
				methods=new List<MemberInfo>(type.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
			}
			overloadedMethods=methods.ToArray();
		}
		public Method(string name,object obj,Type type)
		{
			this.Initialize(name,obj,type);
		}
		public Method(Type type)
		{
			this.Initialize(".ctor",null,type);
		}
		public override bool Equals(object toCompare)
		{
			if(toCompare is Method)
			{
				Method Method=(Method)toCompare;
				if(Method.obj==obj && Method.name.Equals(name) && Method.type.Equals(type))
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
				int hash=name.GetHashCode()*type.GetHashCode();
				if(obj!=null)
				{
					hash=hash*obj.GetHashCode();
				}
				return hash;
			}
		}
		private string name;
		[NonSerialized]
		protected object obj;
		[NonSerialized]
		protected Type type;
		[NonSerialized]
		public MemberInfo[] overloadedMethods;
	}

	[Serializable]
	public class TypeMap: DotNetMap
	{
		public override bool IsFunction
		{
			get
			{
				return true;
			}
		}
		public Type Type
		{
			get
			{
				return type;
			}
		}
		protected override Map CopyImplementation()
		{
			return new TypeMap(type);
		}
		[NonSerialized]
		protected Method constructor;
		public TypeMap(Type targetType):base(null,targetType)
		{
			this.constructor=new Method(this.type);
		}
		public override Map Call(Map argument)
		{
			return constructor.Call(argument);
		}
	}
	[Serializable]
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
		public ObjectMap(object target):base(target,target.GetType())
		{
		}
		public override string ToString()
		{
			return obj.ToString();
		}
		protected override Map CopyImplementation()
		{
			return new ObjectMap(obj);
		}
	}
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
		protected void Panic(Map key,Map val)
		{
			Panic(key, val, new DictionaryStrategy());
		}
		protected void Panic(Map key, Map val,MapStrategy newStrategy)
		{
			Panic(newStrategy);
			map.Strategy.Set(key, val); // why do it like this? this wont assign the parent, which is problematic!!!
		}
		public virtual bool IsInteger
		{
			get
			{
				return map.IsIntegerDefault;
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
		public virtual Integer GetInteger()
		{
			return map.GetIntegerDefault();
		}

		public StrategyMap map;

		public abstract MapStrategy CopyImplementation();
		public virtual Map Copy()
		{
			StrategyMap clone;
			MapStrategy strategy = (MapStrategy)this.CopyImplementation();
			clone=new StrategyMap(strategy);
            strategy.map = clone;
			return clone;
		}

		public virtual List<Map> Array
		{
			get
			{
				List<Map> array=new List<Map>();
				for(int i=1;this.ContainsKey(i);i++)
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
		// rename, only used internally
		public virtual bool Equal(MapStrategy strategy)
		{
			bool isEqual;
			if(Object.ReferenceEquals(strategy,this))
			{ 
				isEqual=true;
			}
			else if(((MapStrategy)strategy).Count!=this.Count)
			{
				isEqual=false;
			}
			else
			{
				isEqual=true;
				foreach(Map key in this.Keys) 
				{
					Map otherValue = strategy.Get(key);
					Map thisValue = Get(key);
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue))
					{
					//if (!((MapStrategy)strategy).ContainsKey(key) || !((MapStrategy)strategy).Get(key).Equals(this.Get(key)))
					//{
						isEqual=false;
					}
				}
			}
			return isEqual;
		}
	}
	public class CloneStrategy:MapStrategy
	{
		public MapStrategy original;


		public CloneStrategy(MapStrategy original)
		{
			this.original = original;
		}
		public override List<Map> Array
		{
			get
			{
				return original.Array;
			}
		}
		public override bool ContainsKey(Map key)
		{
			return original.ContainsKey (key);
		}
		public override int Count
		{
			get
			{
				return original.Count;
			}
		}
		public override MapStrategy CopyImplementation()
		{
			MapStrategy clone=new CloneStrategy(this.original);
			map.Strategy = new CloneStrategy(this.original);
			return clone;

		}
		// ????
		public override bool Equal(MapStrategy obj)
		{
			bool equal;
			if (object.ReferenceEquals(this.original, obj))
			{
				equal = true;
			}
			else
			{
				CloneStrategy cloneStrategy = obj as CloneStrategy;
				if (cloneStrategy != null && object.ReferenceEquals(cloneStrategy.original, this.original))
				{
					equal = true;
				}
				else
				{
					//|| object.ReferenceEquals(this.original)
					equal = original.Equal(obj);
				}
			}
			return equal;
		}
		//public override bool Equal(MapStrategy obj)
		//{
		//    return original.Equal(obj);
		//}
		public override int GetHashCode()
		{
			return original.GetHashCode();
		}
		public override Integer GetInteger()
		{
			return original.GetInteger();
		}
		public override string GetString()
		{
			return original.GetString();
		}
		public override bool IsInteger
		{
			get
			{
				return original.IsInteger;
			}
		}
		public override bool IsString
		{
			get
			{
				return original.IsString;
			}
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return original.Keys;
			}
		}
		public override Map  Get(Map key)
		{
			return original.Get(key);
		}
		public override void Set(Map key, Map value)
		{
			Panic(key,value);
		}
	}
	public class DictionaryStrategy:MapStrategy
	{
		private Dictionary<Map,Map> dictionary;
		public DictionaryStrategy():this(2)
		{
			if (this.map!= null &&this.IsString && this.GetString() == "abc, ")
			{
			}
		}
		public override MapStrategy CopyImplementation()
		{
			return new CloneStrategy(this);
		}
		public DictionaryStrategy(int Count)
		{
			this.dictionary=new Dictionary<Map,Map>(Count);
		}
		public override List<Map> Array
		{
			get
			{
				List<Map> list=new List<Map>();
				for(Integer iInteger=new Integer(1);ContainsKey(new StrategyMap(iInteger));iInteger+=1)
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
				return dictionary.Keys;
			}
		}
		public override int Count
		{
			get
			{
				return dictionary.Count;
			}
		}
		public override Map  Get(Map key)
		{
			Map val;
            dictionary.TryGetValue(key,out val);
            return val;
		}
		public override void Set(Map key,Map value)
		{
			dictionary[key]=value;
		}
		public override bool ContainsKey(Map key) 
		{
			return dictionary.ContainsKey(key);
		}
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
			try
			{
				Delegate eventDelegate = (Delegate)type.GetField(eventInfo.Name, BindingFlags.Public |
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(obj);
				if (eventDelegate != null)
				{
					List<object> arguments = new List<object>();
					ParameterInfo[] parameters = eventDelegate.Method.GetParameters();
					for (int i = 1; i < parameters.Length; i++)
					{
						arguments.Add(Transform.ToDotNet((Map)argument[i], parameters[i].ParameterType));
					}
					result = Transform.ToMeta(eventDelegate.DynamicInvoke(arguments.ToArray()));
				}
				else
				{
					result = null;
				}
			}
			catch (Exception e)
			{
				result = null;
			}
			return result;
		}
		protected override Map CopyImplementation()
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
		protected override Map CopyImplementation()
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
			if(this.property.Name=="Item")
			{
				int asdf=0;
			}	
			throw new ApplicationException("Cannot assign in property "+property.Name+".");
		}
	}
	public abstract class DotNetMap: Map, ISerializeEnumerableSpecial
	{
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
					if(key.IsInteger)
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
				if (text == "Serialize")
				{
				}
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
						val.Parent = this;
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
			else if (obj != null && key.IsInteger && type.IsArray)
			{
				object converted = Transform.ToDotNet(value, type.GetElementType());
				if (converted!=null)
				{
					((Array)obj).SetValue(converted, key.GetInteger().GetInt32());
					return;
				}
			}
			else
			{
				throw new ApplicationException("Cannot set key " + FileSystem.Serialize.Value(key) + ".");
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
			Delegate eventDelegate=Method.CreateDelegateFromCode(eventInfo.EventHandlerType,code);
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
	public class IntegerStrategy:MapStrategy
	{
		private Integer number;
		public override bool IsInteger
		{
			get
			{
				return true;
			}
		}
		public override Integer GetInteger()
		{
			return number;
		}
		public override bool Equal(MapStrategy obj)
		{
			bool isEqual;
			if (obj is IntegerStrategy)
			{
				isEqual = ((IntegerStrategy)obj).number == number;
			}
			else
			{
				isEqual = base.Equal(obj);
			}
			return isEqual;
		}
		public IntegerStrategy(Integer number)
		{
			this.number = new Integer(number);
		}
		public override MapStrategy CopyImplementation()
		{
			return new IntegerStrategy(number);
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				if(number!=0)
				{
					keys.Add(Map.Empty);
				}
				return keys;
			}
		}
		public override Map Get(Map key)
		{
			Map result;
			if(key.Equals(Map.Empty))
			{
				if(number==0)
				{
					result=null;
				}
				else
				{
					result=number-1;
				}
			}

			else
			{
				result=null;
			}
				return result;
		}
		public override void Set(Map key, Map value)
		{
			if (key.Equals(Map.Empty) && value.IsInteger)
			{
				this.number += 1;
			}
			else
			{
				Panic(key, value);
			}
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

					bool successful = File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath));

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
		public Extent(SourcePosition start, SourcePosition end)
		{
			this.start = start;
			this.end = end;
		}
		public Extent(int startLine,int startColumn,int endLine,int endColumn):this(new SourcePosition(startLine,startColumn),new SourcePosition(endLine,endColumn))
		{
		}
		public Extent CreateExtent(int startLine,int startColumn,int endLine,int endColumn)
		{
			Extent extent=new Extent(startLine,startColumn,endLine,endColumn);
			if(!extents.ContainsKey(extent))
			{
				extents.Add(extent,extent);
			}
			return (Extent)extents[extent];
		}
	}

	public class FileSystem
	{
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
		private static void MakePersistant(StrategyMap map)
		{
			map.Persistant = true;
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				if (pair.Value is StrategyMap)
				{
					StrategyMap normalMap = (StrategyMap)pair.Value;
					if (normalMap.Strategy is DictionaryStrategy || (normalMap.Strategy is CloneStrategy && ((CloneStrategy)normalMap.Strategy).original is DictionaryStrategy))
					{
						MakePersistant((StrategyMap)pair.Value);
					}
				}
			}
		}
		// rename, refactor
		public static Map Parse(string filePath)
		{

			using (TextReader reader = new StreamReader(filePath, Encoding.Default))
			{
				Map parsed = Parse(reader);
				// reintroduce this
				//MakePersistant((StrategyMap)parsed);
				return parsed;
			}
		}
		public static Map Parse(TextReader textReader)
		{
			Parsing = true;
			Map result=Compile(textReader).GetExpression().Evaluate(Map.Empty);//, Map.Empty);
			Parsing = false;
			return result;
		}
		public static Map Compile(TextReader textReader)
		{
			return new Parser(textReader.ReadToEnd(), FileSystem.Path).Program();
		}
		public static Map fileSystem;
		static FileSystem()
		{
			fileSystem=Parse(Path);
			fileSystem.Parent = GacStrategy.Gac;
			fileSystem.Scope = GacStrategy.Gac;
			// messy, experimental
			((GacStrategy)GacStrategy.Gac.Strategy).cache["local"] = fileSystem;
		}
		public static string Path
		{
			get
			{
				return System.IO.Path.Combine(Process.InstallationPath, "meta.meta");
			}
		}
		public static void Save()
		{
			string text = Serialize.MapValue(fileSystem, null).Trim(new char[] { '\n' });
			if (text == "\"\"")
			{
				text = "";
			}
			File.WriteAllText(System.IO.Path.Combine(Process.InstallationPath,"meta.meta"), text,Encoding.Default);
		}
		public class Parser
		{
			private string text;
			private int index;
			private string filePath;
			public Parser(string text, string filePath)
			{
				this.index = 0;
				this.text = text;
				this.filePath = filePath;
			}
			private void Consume(string characters)
			{
				foreach (char character in characters)
				{

					Consume(character);
				}
			}
			private void Consume()
			{
				Consume(Look());
			}
			private void Consume(char character)
			{
				if (!TryConsume(character))
				{
					throw new ApplicationException("Unexpected token " + Look() + " ,expected " + character);
					//throw new ApplicationException("Unexpected token " + text[index] + " ,expected " + character);
				}
			}
			private bool TryConsume(string characters)
			{
				bool consumed;
				if (index + characters.Length < text.Length && text.Substring(index, characters.Length) == characters)
				{

					consumed = true;
					foreach (char c in characters)
					{
						Consume(c);
					}
				}
				else
				{
					consumed = false;
				}
				return consumed;
			}
			private bool TryConsume(char character)
			{
				bool consumed;
				if (index < text.Length && text[index] == character)
				{
					if (character == unixNewLine)
					{
						line++;
					}
					index++;
					consumed = true;
				}
				else
				{
					consumed = false;
				}
				return consumed;
			}
			private bool Look(int lookAhead, char character)
			{
				return Look(lookAhead) == character;
			}
			private bool Look(char character)
			{
				return Look(0, character);
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
					character = endOfFileChar;
				}
				return character;
			}


			public char endOfFileChar = (char)65535;
			public const char indentationChar = '\t';
			public int indentationCount = -1;
			public const char unixNewLine = '\n';
			public const string windowsNewLine = "\r\n";
			public const char functionChar = '|';
			public const char stringChar = '\"';
			public char[] integerChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			public char[] firstIntegerChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			public const char lookupStartChar = '[';
			public const char lookupEndChar = ']';
			public static char[] lookupStringForbiddenChars = new char[] { callChar, indentationChar, '\r', '\n', statementChar, selectChar, stringEscapeChar, functionChar, stringChar, lookupStartChar, lookupEndChar, emptyMapChar };
			public char[] lookupStringFirstCharAdditionalForbiddenChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			public const char emptyMapChar = '*';
			public const char callChar = ' ';
			public const char selectChar = '.';


			private bool TryConsumeNewLine(string text)
			{
				string whitespace = "";
				for (int i = 0; Look(i) == space || Look(i) == tab; i++)
				{
					whitespace += Look(i);
				}
				return TryConsume(whitespace + unixNewLine + text) || TryConsume(whitespace + windowsNewLine + text);
			}
			private bool Indentation()
			{
				string indentationString = "".PadLeft(indentationCount + 1, indentationChar);
				bool isIndentation;
				if (TryConsumeNewLine(indentationString))
				{
					indentationCount++;
					isIndentation = true;
				}
				else if (isStartOfFile)
				{
					isStartOfFile = false;
					indentationCount++;
					isIndentation = true;
				}
				else
				{
					isIndentation = false;
				}
				return isIndentation;
			}
			public Map Expression()
			{
				if (line > 878)
				{
				}
				Map expression = Integer();
				if (expression == null)
				{
					expression = String();
					if (expression == null)
					{
						expression = Program();
						if (expression == null)
						{
							Map select = Select();
							if (select != null)
							{
								Map call = Call(select);
								if (call != null)
								{
									expression = call;
								}
								else
								{
									expression = select;
								}
							}
							else
							{
								expression = null;
							}
						}
					}
				}
				if (expression != null && expression.Extent == null)
				{
				}
				return expression;
			}
			public Map Call(Map select)
			{
				if (line > 880)
				{
				}
				Map call;
				Extent extent = StartExpression();
				Map argument;
				if (TryConsume(callChar))
				{
					argument = Expression();
				}
				else
				{
					argument = NormalProgram();
				}
				if (argument != null)
				{
					call = new StrategyMap();
					Map callCode = new StrategyMap();
					callCode[CodeKeys.Callable] = select;
					callCode[CodeKeys.Argument] = argument;
					EndExpression(extent,callCode);
					call[CodeKeys.Call] = callCode;
				}
				else
				{
					call = null;
				}
				EndExpression(extent, call);
				return call;
			}
			public bool isStartOfFile = true;
			private void Whitespace()
			{
				while (TryConsume('\t') || TryConsume(' '))
				{
				}
			}// rename to Program, make emptyMap a literal
			private Map NormalProgram()
			{
				Extent extent = StartExpression();
				Map program;
				if (Indentation())
				{
					program = new StrategyMap();
					int counter = 1;
					int defaultKey = 1;
					Map statements = new StrategyMap();
					while (!Look(endOfFileChar))
					{
						Map statement = Function();
						if (statement == null)
						{
							statement = Statement(ref defaultKey);
						}
						statements[counter] = statement;
						counter++;
						if (!NewLine() && !Look(endOfFileChar))
						{
							//refactor
							index -= 1;
							if (!NewLine())
							{
								index -= 1;
								if (!NewLine())
								{
									index += 2;
									throw new MetaException("Expected newline.", new Extent(Position, Position));
								}
								else
								{
									line--;
								}
							}
							else
							{
								line--;
							}
						}
						string newIndentation = GetIndentation();
						if (newIndentation.Length < indentationCount)
						{
							indentationCount--;
							break;
						}
						else if (newIndentation.Length == indentationCount)
						{
							Consume(newIndentation);
						}
						else
						{
							throw new MetaException("incorrect indentation", extent);
						}
					}
					EndExpression(extent, statements);
					program[CodeKeys.Program] = statements;
				}
				else
				{
					program = null;
				}
				EndExpression(extent, program);
				return program;
			}
			private SourcePosition Position
			{
				get
				{
					return new SourcePosition(line, Column);
				}
			}
			private Map EmptyProgram()
			{
				Extent extent = StartExpression();
				Map program;
				if (TryConsume(emptyMapChar))
				{
					program = new StrategyMap();
					program[CodeKeys.Program] = new StrategyMap();
				}
				else
				{
					program = null;
				}
				EndExpression(extent, program);
				return program;
			}
			public Map Program()
			{
				Map program = EmptyProgram();
				if (program == null)
				{
					program = NormalProgram();
				}
				return program;
			}
			private bool NewLine()
			{
				return TryConsumeNewLine("");
			}
			private string GetIndentation()
			{
				int i = 0;
				string indentation = "";
				while (Look(i) == indentationChar)
				{
					indentation += Look(i);
					i++;
				}
				return indentation;
			}
			private bool LookAny(char[] any)
			{
				return Look().ToString().IndexOfAny(any) != -1;
			}
			private char ConsumeGet()
			{
				char character = Look();
				Consume(character);
				return character;
			}
			private Extent StartExpression()
			{
				return new Extent(Line, Column, 0, 0);
			}
			private void EndExpression(Extent extent, Map expression)
			{
				if (expression != null)
				{
					extent.End.Line = Line;
					extent.End.Column = Column;
					expression.Extent = extent;
				}
			}
			private Map Integer()
			{
				Map integer;
				Extent extent = StartExpression();
				if (LookAny(firstIntegerChars))
				{
					string integerString = "";
					integerString += ConsumeGet();
					while (LookAny(integerChars))
					{
						integerString += ConsumeGet();
					}
					Map literal = new StrategyMap(Meta.Integer.ParseInteger(integerString));
					integer = new StrategyMap();
					integer[CodeKeys.Literal] = literal;
				}
				else
				{
					integer = null;
				}
				EndExpression(extent, integer);
				return integer;
			}
			public const char stringEscapeChar = '\'';
			private Map String()
			{
				try
				{
					Map @string;
					Extent extent = StartExpression();

					if (Look(stringChar) || Look(stringEscapeChar))
					{
						int escapeCharCount = 0;
						while (TryConsume(stringEscapeChar))
						{
							escapeCharCount++;
						}
						Consume(stringChar);
						string stringText = "";
						while (true)
						{
							if (Look(stringChar))
							{
								int foundEscapeCharCount = 0;
								while (foundEscapeCharCount < escapeCharCount && Look(foundEscapeCharCount + 1, stringEscapeChar))
								{
									foundEscapeCharCount++;
								}
								if (foundEscapeCharCount == escapeCharCount)
								{
									Consume(stringChar);
									Consume("".PadLeft(escapeCharCount, stringEscapeChar));
									break;
								}
							}
							stringText += Look();
							Consume(Look());
						}
						List<string> realLines = new List<string>();
						string[] lines = stringText.Replace(windowsNewLine, unixNewLine.ToString()).Split(unixNewLine);
						for (int i = 0; i < lines.Length; i++)
						{
							if (i == 0)
							{
								realLines.Add(lines[i]);
							}
							else
							{
								realLines.Add(lines[i].Remove(0, Math.Min(indentationCount + 1, lines[i].Length - lines[i].TrimStart(indentationChar).Length)));
							}
						}
						string realText = string.Join("\n", realLines.ToArray());
						Map literal = new StrategyMap(realText);
						@string = new StrategyMap();
						@string[CodeKeys.Literal] = literal;
					}
					else
					{
						@string = null;
					}
					EndExpression(extent, @string);
					return @string;
				}
				catch (Exception e)
				{
					return null;
				}
			}
			private Map LookupString()
			{
				string lookupString = "";
				Extent extent = StartExpression();
				if (LookExcept(lookupStringForbiddenChars) && LookExcept(lookupStringFirstCharAdditionalForbiddenChars))
				{
					while (LookExcept(lookupStringForbiddenChars))
					{
						lookupString += Look();
						Consume(Look());
					}
				}
				Map lookup;
				if (lookupString.Length > 0)
				{
					lookup = new StrategyMap();
					lookup[CodeKeys.Literal] = new StrategyMap(lookupString);
				}
				else
				{
					lookup = null;
				}
				EndExpression(extent, lookup);
				return lookup;
			}
			private bool LookExcept(char[] exceptions)
			{
				List<char> list = new List<char>(exceptions);
				list.Add(endOfFileChar);
				return Look().ToString().IndexOfAny(list.ToArray()) == -1;
			}
			private Map LookupAnything()
			{
				Map lookupAnything;
				if (TryConsume(lookupStartChar))
				{
					lookupAnything = Expression();
					while (TryConsume(indentationChar)) ;
					Consume(lookupEndChar);
				}
				else
				{
					lookupAnything = null;
				}
				return lookupAnything;
			}

			private Map Lookup()
			{
				Extent extent = StartExpression();
				Map lookup = LookupString();
				if (lookup == null)
				{
					lookup = LookupAnything();
				}
				EndExpression(extent, lookup);
				return lookup;
			}

			private Map Select(Map keys)
			{
				Map select;
				Extent extent = StartExpression();
				if (keys != null)
				{
					select = new StrategyMap();
					select[CodeKeys.Select] = keys;
				}
				else
				{
					select = null;
				}
				EndExpression(extent, select);
				return select;
			}
			public Map Select()
			{
				return Select(Keys());
			}
			private Map Keys()
			{
				Extent extent = StartExpression();
				Map lookups = new StrategyMap();
				int counter = 1;
				Map lookup;
				while (true)
				{
					lookup = Lookup();
					if (lookup != null)
					{
						lookups[counter] = lookup;
						counter++;
					}
					else
					{
						break;
					}
					if (!TryConsume(selectChar))
					{
						break;
					}
				}
				Map keys;
				if (counter > 1)
				{
					keys = lookups;
				}
				else
				{
					keys = null;
				}
				EndExpression(extent, lookups);
				return keys;
			}
			public Map Function()
			{
				Extent extent = StartExpression();
				Map function = null;
				if (TryConsume(functionChar))
				{
					Map expression = Expression();
					if (expression != null)
					{
						function = new StrategyMap();
						function[CodeKeys.Key] = CreateDefaultKey(CodeKeys.Function);
						Map literal = new StrategyMap();
						literal[CodeKeys.Literal] = expression;
						function[CodeKeys.Value] = literal;
					}
				}
				EndExpression(extent, function);
				return function;
			}
			public const char statementChar = '=';
			public Map Statement(ref int count)
			{
				Extent extent = StartExpression();
				Map key = Keys();
				Map val;
				if (key != null && TryConsume(statementChar))
				{
					val = Expression();
				}
				else
				{
					TryConsume(statementChar);
					if (key != null)
					{
						Map select = Select(key);
						Map call = Call(select);
						if (call != null)
						{
							val = call;
						}
						else
						{
							val = select;
						}
					}
					else
					{
						val = Expression();
					}
					key = CreateDefaultKey(new StrategyMap((Integer)count));
					count++;
				}
				if (val == null)
				{
					SourcePosition position = new SourcePosition(Line, Column);
					throw new MetaException("Expected value of statement", new Extent(position, position));
				}
				Map statement = new StrategyMap();
				statement[CodeKeys.Key] = key;
				statement[CodeKeys.Value] = val;
				EndExpression(extent, statement);
				return statement;
			}
			private const char space = ' ';
			private const char tab = '\t';
			private Map CreateDefaultKey(Map literal)
			{
				Map key = new StrategyMap();
				Map firstKey = new StrategyMap();
				firstKey[CodeKeys.Literal] = literal;
				key[1] = firstKey;
				return key;
			}
			private int line = 1;
			private int Line
			{
				get
				{
					return line;
				}
			}
			private int Column
			{
				get
				{
					int startPos = Math.Min(index, text.Length - 1);
					return index - text.LastIndexOf('\n', startPos);
				}
			}
		}
		public class Serialize
		{
			public static string Value(Map val)
			{
				return Value(val, null);
			}
			private static string Value(Map val, string indentation)
			{
				string text;
				if (val.Equals(Map.Empty))
				{
					text = Parser.emptyMapChar.ToString();
				}
				else if (val.IsString)
				{
					text = StringValue(val, indentation);
				}
				else if (val.IsInteger)
				{
					text = IntegerValue(val);
				}
				else
				{
					text = MapValue(val, indentation);
				}
				return text;
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

					text = Parser.lookupStartChar.ToString();
					if (key.Equals(Map.Empty))
					{
						text += Parser.emptyMapChar;
					}
					else if (key.IsInteger)
					{
						text += IntegerValue(key.GetInteger());
					}
					else
					{
						text += MapValue(key, indentation) + indentation;
					}
					text += Parser.lookupEndChar;
				}
				return text;
			}
			private static string StringKey(Map key, string indentation)
			{
				string text;
				if (IsLiteralKey(key.GetString()))
				{
					text = key.GetString();
				}
				else
				{
					text = Parser.lookupStartChar + StringValue(key, indentation) + Parser.lookupEndChar;
				}
				return text;
			}
			private static bool IsLiteralKey(string text)
			{
				return -1 == text.IndexOfAny(Parser.lookupStringForbiddenChars);
			}
			public static string MapValue(Map map, string indentation)
			{
				string text;
				text = Parser.unixNewLine.ToString();
				if (indentation == null)
				{
					indentation = "";
				}
				else
				{
					indentation += Parser.indentationChar;
				}
				foreach (KeyValuePair<Map, Map> entry in map)
				{
					//if (key.Count == 1 && (literal = key[1][CodeKeys.Literal]) != null && (function = literal[CodeKeys.Function]) != null && function.Count == 1 && (function.ContainsKey(CodeKeys.Call) || function.ContainsKey(CodeKeys.Literal) || function.ContainsKey(CodeKeys.Program) || function.ContainsKey(CodeKeys.Select)))
					if (entry.Key.Equals(CodeKeys.Function) && entry.Value.Count == 1 && (entry.Value.ContainsKey(CodeKeys.Call) || entry.Value.ContainsKey(CodeKeys.Literal) || entry.Value.ContainsKey(CodeKeys.Program) || entry.Value.ContainsKey(CodeKeys.Select)))
					{
						text += indentation + Parser.functionChar + Expression(entry.Value, indentation);
						if (!text.EndsWith(Parser.unixNewLine.ToString()))
						{
							text += Parser.unixNewLine;
						}
					}
					else
					{
						text += indentation + Key((Map)entry.Key, indentation) + Parser.statementChar + Value((Map)entry.Value, (indentation));
						if (!text.EndsWith(Parser.unixNewLine.ToString()))
						{
							text += Parser.unixNewLine;
						}
					}
				}
				return text;
			}
			public static string Expression(Map code, string indentation)
			{
				string text;
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
					text = Literal(code[CodeKeys.Literal], indentation);
				}
				else if (code.ContainsKey(CodeKeys.Select))
				{
					text = Select(code[CodeKeys.Select], indentation);
				}
				else
				{
					throw new ApplicationException("Cannot serialize map.");
				}
				return text;
			}
			public static string Call(Map code, string indentation)
			{
				Map callable = code[CodeKeys.Callable];
				Map argument = code[CodeKeys.Argument];
				string text = Expression(callable, indentation);
				if (!(argument.ContainsKey(CodeKeys.Program) && argument[CodeKeys.Program].Count != 0))
				{
					text += Parser.callChar;
				}
				else
				{
				}
				text += Expression(argument, indentation);
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
					text = Parser.unixNewLine.ToString();
					int autoKeys = 0;
					foreach (Map statement in code.Array)
					{
						text += Statement(statement, indentation + Parser.indentationChar, ref autoKeys);
						if (!text.EndsWith(Parser.unixNewLine.ToString()))
						{
							text += Parser.unixNewLine;
						}
					}
				}
				return text;
			}
			public static string Statement(Map code, string indentation, ref int autoKeys)
			{
				Map key = code[CodeKeys.Key];
				string text;
				//if (code.Extent != null && code.Extent.Start.Line == 57)
				//{
				//}
				//Map literal;
				//Map function;
				//if (key.Count == 1 && (literal = key[1][CodeKeys.Literal]) != null && (function = literal[CodeKeys.Function]) != null && function.Count == 1 && (function.ContainsKey(CodeKeys.Call) || function.ContainsKey(CodeKeys.Literal) || function.ContainsKey(CodeKeys.Program) || function.ContainsKey(CodeKeys.Select)))
				if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Literal].Equals(CodeKeys.Function))
				{
					if (code[CodeKeys.Value][CodeKeys.Literal] == null)
					{
					}
					text = indentation + Parser.functionChar + Expression(code[CodeKeys.Value][CodeKeys.Literal], indentation);
				}
				else
				{
					Map autoKey;
					text = indentation;
					Map value = code[CodeKeys.Value];
					if (key.Count == 1 && (autoKey = key[1][CodeKeys.Literal]) != null && autoKey.IsInteger && autoKey.GetInteger() == autoKeys + 1)
					{
						if (code.Extent.Start.Line > 500)
						{
						}
						autoKeys++;
						if (value.ContainsKey(CodeKeys.Program) && value[CodeKeys.Program].Count != 0)
						{
							text += Parser.statementChar;
						}
					}
					else
					{
						text += Select(code[CodeKeys.Key], indentation) + Parser.statementChar;
					}
					text += Expression(value, indentation);
				}
				return text;
			}
			//public static string Statement(Map code, string indentation, ref int autoKeys)
			//{
			//    Map key = code[CodeKeys.Key];
			//    string text;
			//    //if (code.Extent != null && code.Extent.Start.Line == 57)
			//    //{
			//    //}
			//    if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Literal].Equals(CodeKeys.Function))
			//    //if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Literal].Equals(CodeKeys.Function))
			//    {
			//        if (code[CodeKeys.Value][CodeKeys.Literal] == null)
			//        {
			//        }
			//        text = indentation + Parser.functionChar + Expression(code[CodeKeys.Value][CodeKeys.Literal], indentation);
			//    }
			//    else
			//    {
			//        Map autoKey;
			//        text = indentation;
			//        Map value = code[CodeKeys.Value];
			//        if (key.Count == 1 && (autoKey = key[1][CodeKeys.Literal]) != null && autoKey.IsInteger && autoKey.GetInteger() == autoKeys + 1)
			//        {
			//            if (code.Extent.Start.Line > 500)
			//            {
			//            }
			//            autoKeys++;
			//            if (value.ContainsKey(CodeKeys.Program) && value[CodeKeys.Program].Count != 0)
			//            {
			//                text += Parser.statementChar;
			//            }
			//        }
			//        else
			//        {
			//            text += Select(code[CodeKeys.Key], indentation) + Parser.statementChar;
			//        }
			//        text += Expression(value, indentation);
			//    }
			//    return text;
			//}
			public static string Literal(Map code, string indentation)
			{
				return Value(code, indentation);
			}
			public static string Select(Map code, string indentation)
			{
				string text = Lookup(code[1], indentation);
				for (int i = 2; code.ContainsKey(i); i++)
				{
					text += Parser.selectChar + Lookup(code[i], indentation);
				}
				return text;
			}
			public static string Lookup(Map code, string indentation)
			{
				string text;
				if (code.ContainsKey(CodeKeys.Literal))
				{
					text = Key(code[CodeKeys.Literal], indentation);
				}
				else
				{
					text = Parser.lookupStartChar + Expression(code, indentation);
					if (code.ContainsKey(CodeKeys.Program) && code[CodeKeys.Program].Count != 0)
					{
						text += indentation;
					}
					text += Parser.lookupEndChar;
				}
				return text;
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
					string[] split = mapString.Split(Parser.stringChar);
					for (int i = 1; i < split.Length; i++)
					{
						int matchLength = split[i].Length - split[i].TrimStart(Parser.stringEscapeChar).Length + 1;
						if (matchLength > longestMatch)
						{
							longestMatch = matchLength;
						}
					}
					string escape = "";
					for (int i = 0; i < longestMatch; i++)
					{
						escape += Parser.stringEscapeChar;
					}
					text = escape + Parser.stringChar;
					string[] lines = val.GetString().Split(new string[] { Parser.unixNewLine.ToString(), Parser.windowsNewLine }, StringSplitOptions.None);
					text += lines[0];
					for (int i = 1; i < lines.Length; i++)
					{
						text += Parser.unixNewLine + indentation + Parser.indentationChar + lines[i];
					}
					text += Parser.stringChar + escape;
				}
				else
				{
					text = MapValue(val, indentation);
				}
				return text;
			}
			private static string IntegerValue(Map number)
			{
				return number.GetInteger().ToString();
			}

		}
	}
	public class GacStrategy : MapStrategy
	{
		public static readonly StrategyMap Gac = new StrategyMap(new GacStrategy());

		public Map cache = new StrategyMap();

		public override MapStrategy CopyImplementation()
		{
			return this;
		}
		private bool LoadAssembly(string assemblyName)
		{
			Map key = new StrategyMap(assemblyName);
			bool loaded;
			if (cache.ContainsKey(key))
			{
				loaded = true;
			}
			else
			{
				Assembly assembly = Assembly.LoadWithPartialName(assemblyName);
				if (assembly != null)
				{
					Map val = new StrategyMap();
					foreach (Type type in assembly.GetExportedTypes())
					{
						if (type.DeclaringType == null)
						{
							val[type.Name] = new TypeMap(type);
						}
					}
					if (!Process.loadedAssemblies.Contains(assembly.Location))
					{
						Process.loadedAssemblies.Add(assembly.Location);
					}
					cache[key] = val;
					loaded = true;
				}
				else
				{
					loaded = false;
				}
			}
			return loaded;
		}
		public override Map Get(Map key)
		{
			Map val;
			if (key.IsString && cache.ContainsKey(key) || LoadAssembly(key.GetString()))
			{
				val = cache[key];
			}
			else
			{
				val = null;
			}
			return val;
		}
		public override void Set(Map key, Map val)
		{
			throw new ApplicationException("Cannot set key " + key.ToString() + " in library.");
		}
		public override ICollection<Map> Keys
		{
			get
			{
				throw new ApplicationException("not implemented.");
			}
		}
		public override int Count
		{
			get
			{
				return cache.Count;
			}
		}

		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsString)
			{
				containsKey = LoadAssembly(key.GetString());
			}
			else
			{
				containsKey = false;
			}
			return containsKey;
		}
		protected Map cachedAssemblyInfo = new StrategyMap();
	}
	public class Integer
	{
		public static Integer operator | (Integer a, Integer b)
		{
			return Convert.ToInt32(a.integer) | Convert.ToInt32(b.integer);
		}
		public Integer(Integer i)
		{
			this.integer = i.integer;
		}
		public override string ToString()
		{
			return integer.ToString();
		}
		public Integer(Map map)
		{
			this.integer = map.GetInteger().integer;
		}
		public Integer Clone()
		{
			return new Integer(integer);
		}
		public Integer(int integer)
			: this((double)integer)
		{
		}
		public Integer(long integer)
			: this((double)integer)
		{
		}
		public Integer(double integer)
		{
			this.integer = integer;
		}
		public Integer(ulong integer)
		{
			this.integer = integer;
		}
		public readonly double integer;

		public static implicit operator Integer(int integer)
		{
			return new Integer(integer);
		}
		public static bool operator ==(Integer a, Integer b)
		{
			return !ReferenceEquals(b,null) && a.integer == b.integer;
		}
		public static bool operator !=(Integer a, Integer b)
		{
			return !(a == b);
		}
		public static Integer operator +(Integer a, Integer b)
		{
			return new Integer(a.integer + b.integer);
		}
		public static Integer operator -(Integer a, Integer b)
		{
			return new Integer(a.integer - b.integer);
		}
		public static Integer operator *(Integer a, Integer b)
		{
			return new Integer(a.integer * b.integer);
		}
		public static bool operator >(Integer a, Integer b)
		{
			return a.integer > b.integer;
		}
		public static bool operator <(Integer a, Integer b)
		{
			return a.integer < b.integer;
		}
		public static bool operator >=(Integer a, Integer b)
		{
			return a.integer >= b.integer;
		}
		public static bool operator <=(Integer a, Integer b)
		{
			return a.integer <= b.integer;
		}
		public override bool Equals(object o)
		{
			if (!(o is Integer))
			{
				return false;
			}
			Integer bi = (Integer)o;
			return bi.integer == integer;
		}
		public override int GetHashCode()
		{
			Integer x = new Integer(this);
			while (x > int.MaxValue)
			{
				x = x - int.MaxValue;
			}
			return x.GetInt32();
		}
		public int GetInt32()
		{
			return Convert.ToInt32(integer);
		}
		public long GetInt64()
		{
			return Convert.ToInt64(integer);
		}
		public static Integer ParseInteger(string text)
		{
			Integer result = new Integer(0);
			if (text.Equals(""))
			{
				result = null;
			}
			else
			{
				int index = 0;
				for (; index < text.Length; index++)
				{
					if (char.IsDigit(text[index]))
					{
						result = result * 10 + (Integer)(text[index] - '0');
					}
					else
					{
						return null;
					}
				}

			}
			return result;
		}
	}
}
