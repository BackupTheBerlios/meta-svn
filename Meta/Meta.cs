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
		public static readonly Map Arg="arg";
		public static readonly Map This="this";
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
		public SyntaxException(string message,string fileName, int line, int column)
			: base(message, new Extent(line, column, line, column,fileName))
		{
		}
	}
	public class MetaException:ApplicationException
	{
		public List<string> InvocationList
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
			string text;
			if (extent.FileName != null)
			{
				text=extent.FileName + ", line ";
			}
			else
			{
				text = "Line ";
			}
			text+=extent.Start.Line + ", column " + extent.Start.Column + ": ";
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
			throw new MetaException("The key "+FileSystem.Serialize.Value(key)+" cannot be found.",extent);
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
			return EvaluateImplementation(current);
		}
		public abstract Map EvaluateImplementation(Map context);
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
		public override Map EvaluateImplementation(Map current)
		{
			Map function = callable.GetExpression().Evaluate(current);
			if (!function.IsFunction)
			{
				object x=function.IsFunction;
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
				e.InvocationList.Add(MetaException.GetExtentText(callable.Extent) + "Function has thrown an exception.");
				throw e;
			}
			catch (Exception e)
			{
				throw new MetaException(e.ToString(), callable.Extent);
			}
			if (result == null)
			{
				result = Map.Empty;
			}
			result.Scope = null;
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
		public override Map EvaluateImplementation(Map current)
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
		public override Map EvaluateImplementation(Map context)
		{
			return literal.Copy();
		}
	}
	public class Select : Expression
	{
		private Map GetSpecialKey(Map context,Map key)
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
				val = context;
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
			Map val=GetSpecialKey(context, key);
			if (val==null)
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
		public override Map EvaluateImplementation(Map context)
		{
			Map selected = FindFirstKey(keys[0], context);
			for (int i = 1; i<keys.Count; i++)
			{
				Map key = keys[i].GetExpression().Evaluate(context);

				if (key.Equals(SpecialKeys.Scope))
				{
					selected = selected.Scope;
				}
				else if (key.Equals(SpecialKeys.Arg))
				{
					// refactor
					Map x = context;
					for (int k = 0; k < i; k++)
					{
						while (x!= null && x.argument == null)
						{
							x = x.Scope;
						}
						x = x.Scope;
					}
					while (x != null && x.argument == null)
					{
						x = x.Scope;
					}
					if (x != null)
					{
						selected = x.argument;
					}
					else
					{
						selected= null;
					}
				}
				else
				{
					selected = selected[key];
				}
				if (selected == null)
				{
					Throw.KeyDoesNotExist(key, keys[i].Extent);
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
			this.value = code[CodeKeys.Value];
		}
		public void Assign(ref Map context)
		{
			Map selection = context;
			Map key;
			int i = 0;
			for (; i + 1 < keys.Count; )
			{
				key = keys[i].GetExpression().Evaluate(context);
				if (key.Equals(SpecialKeys.Scope))
				{
					selection = selection.Scope;
				}
				else
				{
					selection = selection.GetForAssignment(key);
				}

				if (selection == null)
				{
					Throw.KeyDoesNotExist(key, keys[0].Extent);
				}
				i++;
			}
			Map lastKey = keys[i].GetExpression().Evaluate(context);
			Map val = value.GetExpression().Evaluate(context);
			if (lastKey.Equals((Map)"autoSearch"))
			{
			}
			if (lastKey.Equals(SpecialKeys.This))
			{
				val.Scope = context.Scope;
				//val.Parent = context.Parent;
				val.Argument = context.Argument;
				context = val;
			}
			else
			{
				selection[lastKey] = val;
			}
		}
	}
	public class Library
	{
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
				arg["function"].Call(Map.Empty);
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
				string path;
				if (installationPath == null)
				{
					Uri uri = new Uri(Assembly.GetAssembly(typeof(Map)).CodeBase);
					path = Path.GetDirectoryName(uri.AbsolutePath);
				}
				else
				{
					path = installationPath;
				}
				return path;
			}
			set
			{
				installationPath = value;
			}
		}
	}
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		//public static Map Create(params Map[] maps)
		//{
		//    Map map=new StrategyMap();
		//    for (int i = 0; i + 2 <= maps.Length; i += 2)
		//    {
		//        map[maps[i]] = maps[i + 1];
		//    }
		//    return map;
		//}
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
		public Map GetForAssignment(Map key)
		{
			return Get(key);
		}
        public Map this[Map key]
        {
			get
			{
				return Get(key);
				//Map result;
				//if (value != null)
				//{
				//    result = value;
				//}
				//else
				//{
				//    result = null;
				//}
				//return result;
			}
			//get
			//{
			//    Map value=Get(key);
			//    Map result;
			//    if (value != null)
			//    {
			//        result = value;
			//    }
			//    else
			//    {
			//        result = null;
			//    }
			//    return result;
			//}
            set
            {
                if (value != null)
                {
					expression = null;
					statement = null;
                    Map val = value;//.Copy();
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
			Map result = function.GetExpression().Evaluate(this,arg);
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

	public class StrategyMap:Map
	{
		public StrategyMap(params Map[] maps):this()
		{
			for (int i = 0; i + 2 <= maps.Length; i += 2)
			{
				this[maps[i]] = maps[i + 1];
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
			: this(new Integer(boolean))
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
		public StrategyMap():this(new DictionaryStrategy())
		{
		}
		public StrategyMap(int i)
			: this(new Integer(i))
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
			else if (target.IsSubclassOf(typeof(Enum)) && meta.IsInteger)
			{
				dotNet = Enum.ToObject(target, meta.GetInteger().GetInt32());
			}
			else
			{
				switch (Type.GetTypeCode(target))
				{
					case TypeCode.Boolean:
						if (IsIntegerInRange(meta, 0, 1))
						{
							if (meta.GetInteger() == 0)
							{
								dotNet = false;
							}
							else if (meta.GetInteger() == 1)
							{
								dotNet = true;
							}
						}
						break;
					case TypeCode.Byte:
						if (IsIntegerInRange(meta, new Integer(Byte.MinValue), new Integer(Byte.MaxValue)))
						{
							dotNet = Convert.ToByte(meta.GetInteger().GetInt32());
						}
						break;
					case TypeCode.Char:
						if (IsIntegerInRange(meta, (int)Char.MinValue, (int)Char.MaxValue))
						{
							dotNet = Convert.ToChar(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.DateTime:
						dotNet = null;
						break;
					case TypeCode.DBNull:
						if (meta.IsInteger && meta.GetInteger() == 0)
						{
							dotNet = DBNull.Value;
						}
						break;
					case TypeCode.Decimal:
						if (IsIntegerInRange(meta, new Integer((double)decimal.MinValue), new Integer((double)decimal.MaxValue)))
						{
							dotNet = (decimal)(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Double:
						if (IsIntegerInRange(meta, new Integer(double.MinValue), new Integer(double.MaxValue)))
						{
							dotNet = (double)(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Int16:
						if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue))
						{
							dotNet = Convert.ToInt16(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Int32:
						if (IsIntegerInRange(meta, (Integer)Int32.MinValue, Int32.MaxValue))
						{
							dotNet = meta.GetInteger().GetInt32();
						}
						//// TODO: add this for all integers, and other types
						//else if (meta is ObjectMap && ((ObjectMap)meta).Object is Int32)
						//{
						//    dotNet = (Int32)((ObjectMap)meta).Object;
						//}
						break;
					case TypeCode.Int64:
						if (IsIntegerInRange(meta, new Integer(Int64.MinValue), new Integer(Int64.MaxValue)))
						{
							dotNet = Convert.ToInt64(meta.GetInteger().GetInt64());
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
								//constructor = target.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new Type[] { }, new ParameterModifier[] { });
								//object obj = constructor.Invoke(new object[] { });
								//result = new ObjectMap(obj);
								//int asdf = 0;
								//result = null;
								//new System.Drawing.Point();
								//constructor = target.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new Type[] { }, new ParameterModifier[] { });
								//object obj=constructor.Invoke(new object[] { });
								//result = new ObjectMap(obj);
								//int asdf = 0;
								//result = null;
							}
							else
							{
								break;
							}
							foreach (KeyValuePair<Map, Map> pair in meta)
							{
								((Property)result[pair.Key])[DotNetKeys.Set].Call(pair.Value);
							}
							//System.Windows.Forms.Form form=System.Windows.Forms.Form();
							dotNet = result.Object;
						}
						break;
					case TypeCode.SByte:
						if (IsIntegerInRange(meta, (Integer)SByte.MinValue, (Integer)SByte.MaxValue))
						{
							dotNet = Convert.ToSByte(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Single:
						if (IsIntegerInRange(meta, new Integer(Single.MinValue), new Integer(Single.MaxValue)))
						{
							dotNet = (float)meta.GetInteger().GetInt64();
						}
						break;
					case TypeCode.String:
						if (meta.IsString)
						{
							dotNet = meta.GetString();
						}
						break;
					case TypeCode.UInt16:
						if (IsIntegerInRange(meta, new Integer(UInt16.MinValue), new Integer(UInt16.MaxValue)))
						{
							dotNet = Convert.ToUInt16(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.UInt32:
						if (IsIntegerInRange(meta, new Integer(UInt32.MinValue), new Integer(UInt32.MaxValue)))
						{
							dotNet = Convert.ToUInt32(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.UInt64:
						if (IsIntegerInRange(meta, new Integer(UInt64.MinValue), new Integer(UInt64.MaxValue)))
						{
							dotNet = Convert.ToUInt64(meta.GetInteger().GetInt64());
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
		public static bool IsIntegerInRange(Map meta,Integer minValue,Integer maxValue)
		{
			return meta.IsInteger && meta.GetInteger()>=minValue && meta.GetInteger()<=maxValue;
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
			return Transform.ToSimpleMeta(result);
			//return Transform.ToMeta(result);
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
					//arg.Append(Transform.ToMeta(argument));
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
			//if (key.IsString && key.GetString() == this.type.Name)
			//{
			//    value = this.Constructor;
			//    //value = this.Constructor;

			//}
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
		//protected override Map Get(Map key)
		//{
		//    Map value;
		//    if (key.IsString && key.GetString() == this.type.Name)
		//    {
		//        value = this.Constructor;
		//        //value = this.Constructor;

		//    }
		//    else if (type.IsGenericTypeDefinition && key.Array.TrueForAll(delegate(Map map) { return map is ClassMap; }))
		//    {
		//        List<Type> types;
		//        if (type.GetGenericArguments().Length == 1)
		//        {
		//            types = new List<Type>();
		//            types.Add(((ClassMap)key).Type);
		//        }
		//        else
		//        {
		//            types = key.Array.ConvertAll<Type>(new Converter<Map, Type>(delegate(Map map) { return ((ClassMap)map).type; }));
		//        }
		//        value = new ClassMap(type.MakeGenericType(types.ToArray()));
		//    }
		//    else
		//    {
		//        value = base.Get(key);
		//    }
		//    return value;
		//}
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
		//public TypeMap(object obj, Type type):base(obj,type)
		//{
		//}
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
	public class IntegerStrategy : DataStrategy<Integer>
	{
		public override bool IsInteger
		{
			get
			{
				return true;
			}
		}
		public override Integer GetInteger()
		{
			return data;
		}
		protected override bool SameEqual(Integer otherData)
		{
			return otherData == data;
		}
		public IntegerStrategy(Integer number)
		{
			this.data = new Integer(number);
		}
		public override Map CopyData()
		{
			return new StrategyMap(new IntegerStrategy(data));
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
				return keys;
			}
		}
		public override bool ContainsKey(Map key)
		{
			return key.Equals(Map.Empty) && data != 0;
		}
		public override Map Get(Map key)
		{
			Map value;
			if (ContainsKey(key))
			{
				value = data - 1;
			}
			else
			{
				value = null;
			}
			return value;
		}
		public override void Set(Map key, Map value)
		{
			if (key.Equals(Map.Empty) && value.IsInteger)
			{
				this.data = value.GetInteger()+1;
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
			if (key.IsInteger)
			{
				Integer integer = key.GetInteger();
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
			if (key.IsInteger)
			{
				int integer = key.GetInteger().GetInt32();
				if (integer >= 1 && integer <= data.Count)
				{
					value = data[integer - 1];
				}
			}
			return value;
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsInteger)
			{
				int integer = key.GetInteger().GetInt32();
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
					if(!pair.Value.Equals(otherData[pair.Key]))
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
			//this.map.Strategy = new CloneStrategy(this);
			//return new StrategyMap(new CloneStrategy(this));
		}
		//public override MapStrategy CopyData()
		//{
		//    this.map.Strategy = new CloneStrategy(this);
		//    return new CloneStrategy(this);
		//}
		public DictionaryStrategy(int Count)
		{
			this.data=new Dictionary<Map,Map>(Count);
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
				//result = Transform.ToMeta(eventDelegate.DynamicInvoke(arguments.ToArray()));
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
			if(this.property.Name=="Item")
			{
				int asdf=0;
			}	
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
		// pretty incorrect, i think, remove this if possible
		//public override string GetString()
		//{
		//    return obj != null ? "object: "+obj.ToString() : "type: "+type.ToString();
		//}
		public override bool IsInteger
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
						val = Transform.ToSimpleMeta(type.GetField(text).GetValue(obj));
						//val = Transform.ToMeta(type.GetField(text).GetValue(obj));
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
				// why does this map have a parent?
				//parsed.Parent = GacStrategy.Gac;
				//parsed.Scope = GacStrategy.Gac;
				//parsed.Parent = null;
				parsed.Scope = null;
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
			return new Parser(textReader.ReadToEnd(), fileName).program.Get();
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
			fileSystem = LoadDirectory(Path.Combine(Process.InstallationPath, "Data"));
			fileSystem.Scope = Gac.gac;
			Gac.gac["local"] = fileSystem;
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
		//private class Guesser
		//{
		//    private int guessing;
		//    private bool Guess(char c)
		//    {
		//        return parser.text[guessing] == c;
		//    }
		//    private bool Look()
		//    {
		//    }
		//    private bool Guess(string text)
		//    {

		//    }
		//    public void Whitespace()
		//    {
		//        for (int i = 0; Guess(space) || Guess(tab); i++)
		//        {
		//        }
		//    }
		//    public bool NewLine()
		//    {
		//        return Guess(unixNewLine) || Guess(windowsNewLine);
		//    }
		//    private Parser parser;
		//    public Guesser(Parser parser)
		//    {
		//        this.parser = parser;
		//        this.guessing = parser.index;
		//    }
		//}

		//public class XParse : Tester
		//{
		//}
		public class ParseFunction
		{
			protected Parse parse;
			private Parser parser;
			public ParseFunction(Parser parser,Parse parse)
			{
				this.parse = parse;
			}
			public Map Get()
			{
				return parse(parser);
			}

		}
		public delegate Map Parse(Parser parser);

		//public class Parser
		//{
		//    public Parser(object a, object b)
		//    {
		//        function.Get();
		//        function=new ParseFunction(this,delegate(Parser parser) {
		//            return null;
		//        });
		//    }
		//    public ParseFunction function;










		//    public Map Statement(ref int x)
		//    {
		//        return null;
		//    }
		//    public bool isStartOfFile;
		//    public int indentationCount;
		//    public Map Map()
		//    {
		//        return null;
		//    }
		//    public static char[] lookupStringForbiddenChars = null;
		//    public const char endOfFileChar = (char)65535;
		//    public const char indentationChar = '\t';
		//    public const char unixNewLine = '\n';
		//    public const string windowsNewLine = "\r\n";
		//    public const char functionChar = '|';
		//    public const char stringChar = '\"';
		//    public char[] integerChar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public char[] integerStartChar = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char lookupStartChar = '[';
		//    public const char lookupEndChar = ']';
		//    public static char[] lookupStringForbiddenChar = new char[] { callChar, indentationChar, '\r', '\n', statementChar, selectChar, stringEscapeChar, functionChar, stringChar, lookupStartChar, lookupEndChar, emptyMapChar };
		//    //public char[] lookupStringFirstForbidden = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char emptyMapChar = '*';
		//    public const char callChar = ' ';
		//    public const char selectChar = '.';
		//    public const char stringEscapeChar = '\'';
		//    public const char statementChar = '=';
		//    private const char spaceChar = ' ';
		//    private const char tabChar = '\t';
		////public class ParserTest
		////{
		////    public Map EmptyMap(Parser parser)
		////    {
		////        return null;
		////    }
		////    public Map Expression()
		////    {
		////        //new ParseFunction.Parse(EmptyMap);
		////        EmptyMap;
		////        Integer;
		////        String();
		////        Program();
		////        Call();
		////        Select();
		////    }
		////    //public Map Expression()
		////    //{
		////    //    if ((expression = EmptyMap()) == null)
		////    //    {
		////    //        if ((expression = Integer()) == null)
		////    //        {
		////    //            if ((expression = String()) == null)
		////    //            {
		////    //                if ((expression = Program()) == null)
		////    //                {
		////    //                    if ((expression = Select()) != null)
		////    //                    {
		////    //                        Map call;
		////    //                        if ((call = Call(expression)) != null)
		////    //                        {
		////    //                            expression = call;
		////    //                        }
		////    //                    }
		////    //                }
		////    //            }
		////    //        }
		////    //    }
		////    //    return expression;
		////    //}
		////    // refactor
		////    public Map Call()
		////    {
		////        Or
		////            callChar;
		////            Expression;

		////            Program;

		////            new StrategyMap(
		////                CodeKeys.Call, new StrategyMap(
		////                    CodeKeys.Callable, select,
		////                    CodeKeys.Argument, argument
		////            ));
		////        // refactor
				

		////        call = new StrategyMap();
		////        Map callCode = new StrategyMap();
		////        callCode[CodeKeys.Callable] = select;
		////        callCode[CodeKeys.Argument] = argument;
		////        // refactor
		////        call[CodeKeys.Call] = callCode;
		////    }
		////    //public Map Call(Map select)
		////    //{
		////    //    Map call;
		////    //    Extent extent = BeginExpression();
		////    //    Map argument;
		////    //    if (TryConsume(callChar))
		////    //    {
		////    //        argument = Expression();
		////    //    }
		////    //    else
		////    //    {
		////    //        argument = Program();
		////    //    }
		////    //    if (argument != null)
		////    //    {
		////    //        call = new StrategyMap();
		////    //        Map callCode = new StrategyMap();
		////    //        callCode[CodeKeys.Callable] = select;
		////    //        callCode[CodeKeys.Argument] = argument;
		////    //        // refactor
		////    //        EndExpression(extent, callCode);
		////    //        call[CodeKeys.Call] = callCode;
		////    //        if (!callAllowed)
		////    //        {
		////    //            throw new MetaException("Function may not be called outside of function definition.", argument.Extent);
		////    //        }
		////    //    }
		////    //    else
		////    //    {
		////    //        call = null;
		////    //    }
		////    //    EndExpression(extent, call);
		////    //    return call;
		////    //}
		////    private void Whitespace()
		////    {
		////        tab;
		////        space;
		////    }
		////    public Map Map()
		////    {
		////        Program;
		////        endOfFileChar;
		////        return map;
		////    }
		////    int counter = 1;
		////    int defaultKey = 1;

		////    public Map Program(bool allowed)
		////    {
		////        Indentation;
		////        new StrategyMap(
		////            CodeKeys.Program,
		////            Loop(
		////                endOfFileChar;
		////                Statement(ref defaultKey);
		////                string newIndentation = GetIndentation();
		////                if (newIndentation.Length < indentationCount)
		////                {
		////                    indentationCount--;
		////                    break;
		////                }
		////                else if (newIndentation.Length == indentationCount)
		////                {
		////                    Consume(newIndentation);
		////                }
		////                else
		////                {
		////                    throw new MetaException("incorrect indentation", extent);
		////                }));
		////    }
		////    //public Map Program(bool allowed)
		////    //{
		////    //    bool wasAllowed = this.callAllowed;
		////    //    this.callAllowed = allowed;
		////    //    Extent extent = BeginExpression();
		////    //    Map program;
		////    //    if (Indentation())
		////    //    {
		////    //        program = new StrategyMap();
		////    //        int counter = 1;
		////    //        int defaultKey = 1;
		////    //        Map statements = new StrategyMap();
		////    //        while (!Look(endOfFileChar))
		////    //        {
		////    //            Map statement = Function();
		////    //            if (statement == null)
		////    //            {
		////    //                statement = Statement(ref defaultKey);
		////    //            }
		////    //            statements[counter] = statement;
		////    //            counter++;
		////    //            if (!TryNewLine() && !Look(endOfFileChar))
		////    //            {
		////    //                position -= 1;
		////    //                if (!TryNewLine())
		////    //                {
		////    //                    position -= 1;
		////    //                    if (!TryNewLine())
		////    //                    {
		////    //                        position += 2;
		////    //                        throw new MetaException("Expected newline.", new Extent(Position, Position, filePath));
		////    //                    }
		////    //                    else
		////    //                    {
		////    //                        line--;
		////    //                    }
		////    //                }
		////    //                else
		////    //                {
		////    //                    line--;
		////    //                }
		////    //            }
		////    //            string newIndentation = GetIndentation();
		////    //            //if (LookEmptyLine())
		////    //            //{
		////    //            //    string newIndent = GetIndentation();
		////    //            //    if (newIndent.Length < indentationCount)
		////    //            //    {
		////    //            //        indentationCount--;
		////    //            //        break;
		////    //            //    }
		////    //            //    else if (newIndent.Length == indentationCount)
		////    //            //    {
		////    //            //        EmptyLine();
		////    //            //        Consume(newIndent);
		////    //            //    }
		////    //            //}
		////    //            if (newIndentation.Length < indentationCount)
		////    //            {
		////    //                indentationCount--;
		////    //                break;
		////    //            }
		////    //            else if (newIndentation.Length == indentationCount)
		////    //            {
		////    //                Consume(newIndentation);
		////    //            }
		////    //            else
		////    //            {
		////    //                throw new MetaException("incorrect indentation", extent);
		////    //            }
		////    //        }
		////    //        EndExpression(extent, statements);
		////    //        program[CodeKeys.Program] = statements;
		////    //    }
		////    //    else
		////    //    {
		////    //        program = null;
		////    //    }
		////    //    EndExpression(extent, program);
		////    //    callAllowed = wasAllowed;
		////    //    return program;
		////    //}
		////    private void EmptyLine()
		////    {
		////        NewLine();
		////        NewLine();
		////    }

		////    // guesser should be some kind of parser itself
		////    // when a guess is not atomic, you should use a guesser that keeps track of the guess

		////    //private bool LookEmptyLine()
		////    //{
		////    //    Guesser guess=new Guesser(this);
		////    //    guess.Whitespace();
		////    //    bool emptyLine;
		////    //    if (guess.NewLine())
		////    //    {
		////    //        guess.Whitespace();
		////    //        if (guess.NewLine())
		////    //        {
		////    //            emptyLine = true;
		////    //        }
		////    //        else
		////    //        {
		////    //            emptyLine = false;
		////    //        }
		////    //    }
		////    //    else
		////    //    {
		////    //        emptyLine = false;
		////    //    }
		////    //    return emptyLine;
		////    //    //string whitespace = "";
		////    //    //for (int i = 0; Look(i) == space || Look(i) == tab; i++)
		////    //    //{
		////    //    //    whitespace += Look(i);
		////    //    //}
		////    //    //if(Look(whitespace + unixNewLine + text) || TryConsume(whitespace + windowsNewLine + text));

		////    //}
		////    //public Map Program(bool allowed)
		////    //{
		////    //    bool wasAllowed = this.callAllowed;
		////    //    this.callAllowed = allowed;
		////    //    Extent extent = BeginExpression();
		////    //    Map program;
		////    //    if (Indentation())
		////    //    {
		////    //        program = new StrategyMap();
		////    //        int counter = 1;
		////    //        int defaultKey = 1;
		////    //        Map statements = new StrategyMap();
		////    //        while (!Look(endOfFileChar))
		////    //        {
		////    //            Map statement = Function();
		////    //            if (statement == null)
		////    //            {
		////    //                statement = Statement(ref defaultKey);
		////    //            }
		////    //            statements[counter] = statement;
		////    //            counter++;
		////    //            if (!NewLine() && !Look(endOfFileChar))
		////    //            {
		////    //                index -= 1;
		////    //                if (!NewLine())
		////    //                {
		////    //                    index -= 1;
		////    //                    if (!NewLine())
		////    //                    {
		////    //                        index += 2;
		////    //                        throw new MetaException("Expected newline.", new Extent(Position, Position, filePath));
		////    //                    }
		////    //                    else
		////    //                    {
		////    //                        line--;
		////    //                    }
		////    //                }
		////    //                else
		////    //                {
		////    //                    line--;
		////    //                }
		////    //            }
		////    //            string newIndentation = GetIndentation();

		////    //            if (newIndentation.Length < indentationCount)
		////    //            {
		////    //                indentationCount--;
		////    //                break;
		////    //            }
		////    //            else if (newIndentation.Length == indentationCount)
		////    //            {
		////    //                Consume(newIndentation);
		////    //            }
		////    //            else
		////    //            {
		////    //                throw new MetaException("incorrect indentation", extent);
		////    //            }
		////    //        }
		////    //        EndExpression(extent, statements);
		////    //        program[CodeKeys.Program] = statements;
		////    //    }
		////    //    else
		////    //    {
		////    //        program = null;
		////    //    }
		////    //    EndExpression(extent, program);
		////    //    callAllowed = wasAllowed;
		////    //    return program;
		////    //}
		////    private Map EmptyMap()
		////    {
		////        emptyMapChar;
		////        Map.Make(
		////            CodeKeys.Literal,
		////            Map.Empty);
		////        // vielleicht:
		////        emptyMapChar =
		////        Map.Make(
		////            CodeKeys.Literal,
		////            Map.Empty);
		////    }

		////    //private Map EmptyMap()
		////    //{
		////    //    Extent extent = BeginExpression();
		////    //    Map program;
		////    //    if (TryConsume(emptyMapChar))
		////    //    {
		////    //        program = new StrategyMap();
		////    //        program[CodeKeys.Program] = new StrategyMap();
		////    //    }
		////    //    else
		////    //    {
		////    //        program = null;
		////    //    }
		////    //    EndExpression(extent, program);
		////    //    return program;
		////    //}
		////    private Map Integer()
		////    {
		////        firstIntegerChars;
		////        Map literal = new StrategyMap(Meta.Integer.ParseInteger(integerString));
		////        integer = new StrategyMap();
		////        integer[CodeKeys.Literal] = literal;
		////        Loop
		////            integerChars
		////        EndExpression(extent, integer);
		////        return integer;
		////    }
		////    //private Map Integer()
		////    //{
		////    //    Map integer;
		////    //    Extent extent = BeginExpression();
		////    //    if (LookAny(firstIntegerChars))
		////    //    {
		////    //        string integerString = "";
		////    //        integerString += ConsumeGet();
		////    //        while (LookAny(integerChars))
		////    //        {
		////    //            integerString += ConsumeGet();
		////    //        }
		////    //        Map literal = new StrategyMap(Meta.Integer.ParseInteger(integerString));
		////    //        integer = new StrategyMap();
		////    //        integer[CodeKeys.Literal] = literal;
		////    //    }
		////    //    else
		////    //    {
		////    //        integer = null;
		////    //    }
		////    //    EndExpression(extent, integer);
		////    //    return integer;
		////    //}
		////    public const char stringEscapeChar = '\'';
		////    private Map String()
		////    {
		////        Loop
		////            stringEscapeChar;
		////        stringChar
		////        newLine
		////        Map.Make(
		////            CodeKeys.Literal
		////            Loop
		////                if stringChar
		////                newLine
		////                indentation
		////                Loop
		////                    foundEscapeCharCount == escapeCharCount)
		////                    {
		////                        break;
		////                    }
		////                }
		////    }
		////    //private Map String()
		////    //{
		////    //    try
		////    //    {
		////    //        Map @string;
		////    //        Extent extent = BeginExpression();

		////    //        if (Look(stringChar) || Look(stringEscapeChar))
		////    //        {
		////    //            int escapeCharCount = 0;
		////    //            while (TryConsume(stringEscapeChar))
		////    //            {
		////    //                escapeCharCount++;
		////    //            }
		////    //            Consume(stringChar);
		////    //            string stringText = "";
		////    //            while (true)
		////    //            {
		////    //                if (Look(stringChar))
		////    //                {
		////    //                    int foundEscapeCharCount = 0;
		////    //                    while (foundEscapeCharCount < escapeCharCount && Look(foundEscapeCharCount + 1, stringEscapeChar))
		////    //                    {
		////    //                        foundEscapeCharCount++;
		////    //                    }
		////    //                    if (foundEscapeCharCount == escapeCharCount)
		////    //                    {
		////    //                        Consume(stringChar);
		////    //                        Consume("".PadLeft(escapeCharCount, stringEscapeChar));
		////    //                        break;
		////    //                    }
		////    //                }
		////    //                stringText += Look();
		////    //                Consume(Look());
		////    //            }
		////    //            List<string> realLines = new List<string>();
		////    //            string[] lines = stringText.Replace(windowsNewLine, unixNewLine.ToString()).Split(unixNewLine);
		////    //            for (int i = 0; i < lines.Length; i++)
		////    //            {
		////    //                if (i == 0)
		////    //                {
		////    //                    realLines.Add(lines[i]);
		////    //                }
		////    //                else
		////    //                {
		////    //                    realLines.Add(lines[i].Remove(0, Math.Min(indentationCount + 1, lines[i].Length - lines[i].TrimStart(indentationChar).Length)));
		////    //                }
		////    //            }
		////    //            string realText = string.Join("\n", realLines.ToArray());
		////    //            realText = realText.TrimStart('\n');
		////    //            Map literal = new StrategyMap(realText);
		////    //            @string = new StrategyMap();
		////    //            @string[CodeKeys.Literal] = literal;
		////    //        }
		////    //        else
		////    //        {
		////    //            @string = null;
		////    //        }
		////    //        EndExpression(extent, @string);
		////    //        return @string;
		////    //    }
		////    //    catch (Exception e)
		////    //    {
		////    //        return null;
		////    //    }
		////    //}
		////    private Map LookupString()
		////    {
		////        Except
		////            lookupStringForbiddenChars lookupStringFirstCharAdditionalForbiddenChars


		////        Map.Make 
		////            CodeKeys.Literal
		////            Loop
		////                Except
		////                    lookupStringForbiddenChars
		////    }
		////    //private Map LookupString()
		////    //{
		////    //    string lookupString = "";
		////    //    Extent extent = BeginExpression();
		////    //    if (LookExcept(lookupStringForbiddenChars) && LookExcept(lookupStringFirstCharAdditionalForbiddenChars))
		////    //    {
		////    //        while (LookExcept(lookupStringForbiddenChars))
		////    //        {
		////    //            lookupString += Look();
		////    //            Consume(Look());
		////    //        }
		////    //    }
		////    //    Map lookup;
		////    //    if (lookupString.Length > 0)
		////    //    {
		////    //        lookup = new StrategyMap();
		////    //        lookup[CodeKeys.Literal] = new StrategyMap(lookupString);
		////    //    }
		////    //    else
		////    //    {
		////    //        lookup = null;
		////    //    }
		////    //    EndExpression(extent, lookup);
		////    //    return lookup;
		////    //}
		////    //private Map Select(Map keys)
		////    //{
		////    //    Map select;
		////    //    Extent extent = BeginExpression();
		////    //    if (keys != null)
		////    //    {
		////    //        select = new StrategyMap();
		////    //        select[CodeKeys.Select] = keys;
		////    //    }
		////    //    else
		////    //    {
		////    //        select = null;
		////    //    }
		////    //    EndExpression(extent, select);
		////    //    return select;
		////    //}
		////    private Map Keys()
		////    {
		////        Loop
		////            Lookup();
		////            selectChar;
		////    }
		////    //private Map Keys()
		////    //{
		////    //    Extent extent = BeginExpression();
		////    //    Map lookups = new StrategyMap();
		////    //    int counter = 1;
		////    //    Map lookup;
		////    //    while (true)
		////    //    {
		////    //        lookup = Lookup();
		////    //        if (lookup != null)
		////    //        {
		////    //            lookups[counter] = lookup;
		////    //            counter++;
		////    //        }
		////    //        else
		////    //        {
		////    //            break;
		////    //        }
		////    //        if (!TryConsume(selectChar))
		////    //        {
		////    //            break;
		////    //        }
		////    //    }
		////    //    Map keys;
		////    //    if (counter > 1)
		////    //    {
		////    //        keys = lookups;
		////    //    }
		////    //    else
		////    //    {
		////    //        keys = null;
		////    //    }
		////    //    EndExpression(extent, lookups);
		////    //    return keys;
		////    //}
		////    public Map Function()
		////    {
		////        Map.Make(
		////            CodeKeys.Key,CreateDefaultKey(CodeKeys.Function),
		////            CodeKeys.Value, Map.Make(
		////                CodeKeys.Literal,
		////            functionChar,
		////            Expression));
		////    }
		////    //public Map Function()
		////    //{
		////    //    Extent extent = BeginExpression();
		////    //    bool wasCallAllowed = callAllowed;
		////    //    callAllowed = true;
		////    //    Map function = null;
		////    //    if (TryConsume(functionChar))
		////    //    {
		////    //        Map expression = Expression();
		////    //        if (expression != null)
		////    //        {
		////    //            function = new StrategyMap();
		////    //            function[CodeKeys.Key] = CreateDefaultKey(CodeKeys.Function);
		////    //            Map literal = new StrategyMap();
		////    //            literal[CodeKeys.Literal] = expression;
		////    //            function[CodeKeys.Value] = literal;
		////    //        }
		////    //    }
		////    //    EndExpression(extent, function);
		////    //    callAllowed = wasCallAllowed;
		////    //    return function;
		////    //}
		////    public const char statementChar = '=';
		////    public Map Statement(ref int count)
		////    {
		////        Map.Make(
		////            CodeKeys.Key,key,
		////            CodeKeys.Value, val,

		////        Keys
		////        Or
		////            statementChar
		////            Expression();
					
		////            Or
		////                Optional statementChar;
		////                Or
		////                    Call
		////                    Select
					
		////                Expression();
		////                key = CreateDefaultKey(new StrategyMap((Integer)count)));
		////    }
		////    //public Map Statement(ref int count)
		////    //{
		////    //    if (line > 25)
		////    //    {
		////    //    }
		////    //    Extent extent = BeginExpression();
		////    //    Map key = Keys();
		////    //    Map val;
		////    //    if (key != null && TryConsume(statementChar))
		////    //    {
		////    //        val = Expression();
		////    //    }
		////    //    else
		////    //    {
		////    //        TryConsume(statementChar);
		////    //        if (key != null)
		////    //        {
		////    //            Map select = Select(key);
		////    //            Map call = Call(select);
		////    //            if (call != null)
		////    //            {
		////    //                val = call;
		////    //            }
		////    //            else
		////    //            {
		////    //                val = select;
		////    //            }
		////    //        }
		////    //        else
		////    //        {
		////    //            val = Expression();
		////    //        }
		////    //        key = CreateDefaultKey(new StrategyMap((Integer)count));
		////    //        count++;
		////    //    }
		////    //    if (val == null)
		////    //    {
		////    //        SourcePosition position = new SourcePosition(Line, Column);
		////    //        throw new MetaException("Expected value of statement", new Extent(position, position, filePath));
		////    //    }
		////    //    Map statement = new StrategyMap();
		////    //    statement[CodeKeys.Key] = key;
		////    //    statement[CodeKeys.Value] = val;
		////    //    EndExpression(extent, statement);
		////    //    return statement;
		////    //}
		////    //private Map CreateDefaultKey(Map literal)
		////    //{
		////    //    Map key = new StrategyMap();
		////    //    Map firstKey = new StrategyMap();
		////    //    firstKey[CodeKeys.Literal] = literal;
		////    //    key[1] = firstKey;
		////    //    return key;
		////    //}

		////    }
		//}



		//public class Syntax
		//{
		//    public const char endOfFile = (char)65535;
		//    public const char indentation = '\t';
		//    public const char unixNewLine = '\n';
		//    public const string windowsNewLine = "\r\n";
		//    public const char function = '|';
		//    public const char @string = '\"';
		//    public char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public char[] integerStart = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char lookupStart = '[';
		//    public const char lookupEnd = ']';
		//    public static char[] lookupStringForbidden = new char[] { callChar, indentationChar, '\r', '\n', statementChar, selectChar, stringEscapeChar, functionChar, stringChar, lookupStartChar, lookupEndChar, emptyMapChar };
		//    //public char[] lookupStringFirstForbidden = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char emptyMap = '*';
		//    public const char call = ' ';
		//    public const char select = '.';
		//    public const char stringEscape = '\'';
		//    public const char statement = '=';
		//    private const char space = ' ';
		//    private const char tab = '\t';
		//}



		//public class ParserY
		//{
		//    private string text;
		//    private int position;

		//    public Map Map()
		//    {
		//    }
		//    public Map Expression()
		//    {
		//    }
		//    public Map Call()
		//    {
		//    }
		//    public Map EmptyMap()
		//    {
		//    }
		//    public Map Integer()
		//    {
		//    }
		//    public Map String()
		//    {
		//    }
		//    public Map Select()
		//    {
		//    }
		//    public Map Program()
		//    {
		//    }
		//    public Map Statement()
		//    {
		//    }
		//    public Parser(string text)
		//    {
		//    }
		//    public abstract class Expression
		//    {
		//        private Extent extent;
		//        private Map expression;
		//        protected Parser parser;
		//        public Map Parse()
		//        {
		//            extent=new Extent(Line, Column, 0, 0, filePath);
		//            expression = Parse();
		//            if (expression != null)
		//            {
		//                extent.End.Line = Line;
		//                extent.End.Column = Column;
		//                expression.Extent = extent;
		//            }
		//            return expression;
		//        }
		//        public abstract Map DoParse();
		//    }
		//    private int functions=0;
		//    private Function function;
		//    public class Function:Expression
		//    {
		//        public override Map DoParse()
		//        {
		//            functions++;
		//            Map function = null;
		//            Consume(functionChar);
		//            Expression();


		//            default
		//            if (TryConsume(functionChar))
		//            {
		//                Map expression = Expression();
		//                if (expression != null)
		//                {
		//                    function = new StrategyMap();
		//                    function[CodeKeys.Key] = CreateDefaultKey(CodeKeys.Function);
		//                    Map literal = new StrategyMap();
		//                    literal[CodeKeys.Literal] = expression;
		//                    function[CodeKeys.Value] = literal;
		//                }
		//            }
		//            EndExpression(extent, function);
		//            functions--;
		//            return function;
		//        }
		//    }
		//    public int indentationCount = -1;
		//}






		//public class ParserX
		//{
		//    private string text;
		//    private int position;
		//    private string filePath;
		//    public ParserX(string text, string filePath)
		//    {
		//        this.position = 0;
		//        this.text = text;
		//        this.filePath = filePath;
		//    }
		//    private void Consume(string characters)
		//    {
		//        foreach (char character in characters)
		//        {
		//            Consume(character);
		//        }
		//    }
		//    private void Consume()
		//    {
		//        Consume(Look());
		//    }
		//    private void Consume(char character)
		//    {
		//        if (!TryConsume(character))
		//        {
		//            throw new ApplicationException("Unexpected token " + Look() + " ,expected " + character);
		//        }
		//    }

		//    // refactor
		//    private bool TryConsume(string characters)
		//    {
		//        bool consumed;
		//        if (position + characters.Length < text.Length && text.Substring(position, characters.Length) == characters)
		//        {

		//            consumed = true;
		//            foreach (char c in characters)
		//            {
		//                Consume(c);
		//            }
		//        }
		//        else
		//        {
		//            consumed = false;
		//        }
		//        return consumed;
		//    }
		//    private bool TryConsume(char character)
		//    {
		//        bool consumed;
		//        if (position < text.Length && text[position] == character)
		//        {
		//            if (character == unixNewLine)
		//            {
		//                line++;
		//            }
		//            position++;
		//            consumed = true;
		//        }
		//        else
		//        {
		//            consumed = false;
		//        }
		//        return consumed;
		//    }
		//    private bool Look(int lookAhead, char character)
		//    {
		//        return Look(lookAhead) == character;
		//    }
		//    private bool Look(char character)
		//    {
		//        return Look(0, character);
		//    }
		//    private char Look()
		//    {
		//        return Look(0);
		//    }
		//    private char Look(int lookahead)
		//    {
		//        char character;
		//        int i = position + lookahead;
		//        if (i < text.Length)
		//        {
		//            character = text[position + lookahead];
		//        }
		//        else
		//        {
		//            character = endOfFileChar;
		//        }
		//        return character;
		//    }


		//    public char endOfFileChar = (char)65535;
		//    public const char indentationChar = '\t';
		//    public int indentationCount = -1;
		//    public const char unixNewLine = '\n';
		//    public const string windowsNewLine = "\r\n";
		//    public const char functionChar = '|';
		//    public const char stringChar = '\"';
		//    public char[] integerChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public char[] firstIntegerChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char lookupStartChar = '[';
		//    public const char lookupEndChar = ']';
		//    public static char[] lookupStringForbiddenChars = new char[] { callChar, indentationChar, '\r', '\n', statementChar, selectChar, stringEscapeChar, functionChar, stringChar, lookupStartChar, lookupEndChar, emptyMapChar };
		//    public char[] lookupStringFirstCharAdditionalForbiddenChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		//    public const char emptyMapChar = '*';
		//    public const char callChar = ' ';
		//    public const char selectChar = '.';

		//    // we need some sort of guessing mode
		//    private bool TryConsumeNewLine(string text)
		//    {
		//        string whitespace = "";
		//        for (int i = 0; Look(i) == space || Look(i) == tab; i++)
		//        {
		//            whitespace += Look(i);
		//        }
		//        return TryConsume(whitespace + unixNewLine + text) || TryConsume(whitespace + windowsNewLine + text);
		//    }
		//    private bool Indentation()
		//    {
		//        string indentationString = "".PadLeft(indentationCount + 1, indentationChar);
		//        bool isIndentation;
		//        if (TryConsumeNewLine(indentationString))
		//        {
		//            indentationCount++;
		//            isIndentation = true;
		//        }
		//        else if (isStartOfFile)
		//        {
		//            isStartOfFile = false;
		//            indentationCount++;
		//            isIndentation = true;
		//        }
		//        else
		//        {
		//            isIndentation = false;
		//        }
		//        return isIndentation;
		//    }
		//    public Map Expression()
		//    {
		//        Map expression;
		//        if ((expression = EmptyMap()) == null)
		//        {
		//            if ((expression = Integer()) == null)
		//            {
		//                if ((expression = String()) == null)
		//                {
		//                    if ((expression = Program()) == null)
		//                    {
		//                        if ((expression = Select()) != null)
		//                        {
		//                            Map call;
		//                            if ((call = Call(expression)) != null)
		//                            {
		//                                expression = call;
		//                            }
		//                        }
		//                    }
		//                }
		//            }
		//        }
		//        return expression;
		//    }
		//    // refactor
		//    public Map Call(Map select)
		//    {
		//        Map call;
		//        Extent extent = BeginExpression();
		//        Map argument;
		//        if (TryConsume(callChar))
		//        {
		//            argument = Expression();
		//        }
		//        else
		//        {
		//            argument = Program();
		//        }
		//        if (argument != null)
		//        {
		//            call = new StrategyMap();
		//            Map callCode = new StrategyMap();
		//            callCode[CodeKeys.Callable] = select;
		//            callCode[CodeKeys.Argument] = argument;
		//            // refactor
		//            EndExpression(extent, callCode);
		//            call[CodeKeys.Call] = callCode;
		//            if (!callAllowed)
		//            {
		//                throw new MetaException("Function may not be called outside of function definition.", argument.Extent);
		//            }
		//        }
		//        else
		//        {
		//            call = null;
		//        }
		//        EndExpression(extent, call);
		//        return call;
		//    }
		//    public bool isStartOfFile = true;
		//    private void Whitespace()
		//    {
		//        while (TryConsume('\t') || TryConsume(' '))
		//        {
		//        }
		//    }
		//    public Map Map()
		//    {
		//        Map map = Program(false);
		//        if (!Look(endOfFileChar))
		//        {
		//            throw new MetaException("Expected end of file", new Extent(line, Column, line, Column, filePath));
		//        }
		//        return map;
		//    }
		//    public Map Program()
		//    {
		//        return Program(true);
		//    }
		//    private bool callAllowed;
		//    public Map Program(bool allowed)
		//    {
		//        bool wasAllowed = this.callAllowed;
		//        this.callAllowed = allowed;
		//        Extent extent = BeginExpression();
		//        Map program;
		//        if (Indentation())
		//        {
		//            program = new StrategyMap();
		//            int counter = 1;
		//            int defaultKey = 1;
		//            Map statements = new StrategyMap();
		//            while (!Look(endOfFileChar))
		//            {
		//                Map statement = Function();
		//                if (statement == null)
		//                {
		//                    statement = Statement(ref defaultKey);
		//                }
		//                statements[counter] = statement;
		//                counter++;
		//                if (!TryNewLine() && !Look(endOfFileChar))
		//                {
		//                    position -= 1;
		//                    if (!TryNewLine())
		//                    {
		//                        position -= 1;
		//                        if (!TryNewLine())
		//                        {
		//                            position += 2;
		//                            throw new MetaException("Expected newline.", new Extent(Position, Position, filePath));
		//                        }
		//                        else
		//                        {
		//                            line--;
		//                        }
		//                    }
		//                    else
		//                    {
		//                        line--;
		//                    }
		//                }
		//                string newIndentation = GetIndentation();
		//                //if (LookEmptyLine())
		//                //{
		//                //    string newIndent = GetIndentation();
		//                //    if (newIndent.Length < indentationCount)
		//                //    {
		//                //        indentationCount--;
		//                //        break;
		//                //    }
		//                //    else if (newIndent.Length == indentationCount)
		//                //    {
		//                //        EmptyLine();
		//                //        Consume(newIndent);
		//                //    }
		//                //}
		//                if (newIndentation.Length < indentationCount)
		//                {
		//                    indentationCount--;
		//                    break;
		//                }
		//                else if (newIndentation.Length == indentationCount)
		//                {
		//                    Consume(newIndentation);
		//                }
		//                else
		//                {
		//                    throw new MetaException("incorrect indentation", extent);
		//                }
		//            }
		//            EndExpression(extent, statements);
		//            program[CodeKeys.Program] = statements;
		//        }
		//        else
		//        {
		//            program = null;
		//        }
		//        EndExpression(extent, program);
		//        callAllowed = wasAllowed;
		//        return program;
		//    }
		//    private void EmptyLine()
		//    {
		//        NewLine();
		//        NewLine();
		//    }

		//    // guesser should be some kind of parser itself
		//    // when a guess is not atomic, you should use a guesser that keeps track of the guess

		//    //private bool LookEmptyLine()
		//    //{
		//    //    Guesser guess=new Guesser(this);
		//    //    guess.Whitespace();
		//    //    bool emptyLine;
		//    //    if (guess.NewLine())
		//    //    {
		//    //        guess.Whitespace();
		//    //        if (guess.NewLine())
		//    //        {
		//    //            emptyLine = true;
		//    //        }
		//    //        else
		//    //        {
		//    //            emptyLine = false;
		//    //        }
		//    //    }
		//    //    else
		//    //    {
		//    //        emptyLine = false;
		//    //    }
		//    //    return emptyLine;
		//    //    //string whitespace = "";
		//    //    //for (int i = 0; Look(i) == space || Look(i) == tab; i++)
		//    //    //{
		//    //    //    whitespace += Look(i);
		//    //    //}
		//    //    //if(Look(whitespace + unixNewLine + text) || TryConsume(whitespace + windowsNewLine + text));

		//    //}
		//    //public Map Program(bool allowed)
		//    //{
		//    //    bool wasAllowed = this.callAllowed;
		//    //    this.callAllowed = allowed;
		//    //    Extent extent = BeginExpression();
		//    //    Map program;
		//    //    if (Indentation())
		//    //    {
		//    //        program = new StrategyMap();
		//    //        int counter = 1;
		//    //        int defaultKey = 1;
		//    //        Map statements = new StrategyMap();
		//    //        while (!Look(endOfFileChar))
		//    //        {
		//    //            Map statement = Function();
		//    //            if (statement == null)
		//    //            {
		//    //                statement = Statement(ref defaultKey);
		//    //            }
		//    //            statements[counter] = statement;
		//    //            counter++;
		//    //            if (!NewLine() && !Look(endOfFileChar))
		//    //            {
		//    //                index -= 1;
		//    //                if (!NewLine())
		//    //                {
		//    //                    index -= 1;
		//    //                    if (!NewLine())
		//    //                    {
		//    //                        index += 2;
		//    //                        throw new MetaException("Expected newline.", new Extent(Position, Position, filePath));
		//    //                    }
		//    //                    else
		//    //                    {
		//    //                        line--;
		//    //                    }
		//    //                }
		//    //                else
		//    //                {
		//    //                    line--;
		//    //                }
		//    //            }
		//    //            string newIndentation = GetIndentation();

		//    //            if (newIndentation.Length < indentationCount)
		//    //            {
		//    //                indentationCount--;
		//    //                break;
		//    //            }
		//    //            else if (newIndentation.Length == indentationCount)
		//    //            {
		//    //                Consume(newIndentation);
		//    //            }
		//    //            else
		//    //            {
		//    //                throw new MetaException("incorrect indentation", extent);
		//    //            }
		//    //        }
		//    //        EndExpression(extent, statements);
		//    //        program[CodeKeys.Program] = statements;
		//    //    }
		//    //    else
		//    //    {
		//    //        program = null;
		//    //    }
		//    //    EndExpression(extent, program);
		//    //    callAllowed = wasAllowed;
		//    //    return program;
		//    //}
		//    private Map EmptyMap()
		//    {
		//        Extent extent = BeginExpression();
		//        Map program;
		//        if (TryConsume(emptyMapChar))
		//        {
		//            program = new StrategyMap();
		//            program[CodeKeys.Program] = new StrategyMap();
		//        }
		//        else
		//        {
		//            program = null;
		//        }
		//        EndExpression(extent, program);
		//        return program;
		//    }
		//    private SourcePosition Position
		//    {
		//        get
		//        {
		//            return new SourcePosition(line, Column);
		//        }
		//    }
		//    private void NewLine()
		//    {
		//        if (!TryNewLine())
		//        {
		//            throw new SyntaxException("Excpected newline.", filePath, this.Line, this.Column);
		//        }
		//    }
		//    private bool TryNewLine()
		//    {
		//        return TryConsumeNewLine("");
		//    }
		//    private string GetIndentation()
		//    {
		//        int i = 0;
		//        string indentation = "";
		//        while (Look(i) == indentationChar)
		//        {
		//            indentation += Look(i);
		//            i++;
		//        }
		//        return indentation;
		//    }
		//    // make this more flexible, for example parse all the characters matching these characters
		//    private bool LookAny(char[] any)
		//    {
		//        return Look().ToString().IndexOfAny(any) != -1;
		//    }
		//    private char ConsumeGet()
		//    {
		//        char character = Look();
		//        Consume(character);
		//        return character;
		//    }
		//    private Extent BeginExpression()
		//    {
		//        return new Extent(Line, Column, 0, 0, filePath);
		//    }
		//    private void EndExpression(Extent extent, Map expression)
		//    {
		//        if (expression != null)
		//        {
		//            extent.End.Line = Line;
		//            extent.End.Column = Column;
		//            expression.Extent = extent;
		//        }
		//    }
		//    private Map Integer()
		//    {
		//        Map integer;
		//        Extent extent = BeginExpression();
		//        if (LookAny(firstIntegerChars))
		//        {
		//            string integerString = "";
		//            integerString += ConsumeGet();
		//            while (LookAny(integerChars))
		//            {
		//                integerString += ConsumeGet();
		//            }
		//            Map literal = new StrategyMap(Meta.Integer.ParseInteger(integerString));
		//            integer = new StrategyMap();
		//            integer[CodeKeys.Literal] = literal;
		//        }
		//        else
		//        {
		//            integer = null;
		//        }
		//        EndExpression(extent, integer);
		//        return integer;
		//    }
		//    public const char stringEscapeChar = '\'';
		//    private Map String()
		//    {
		//        try
		//        {
		//            Map @string;
		//            Extent extent = BeginExpression();

		//            if (Look(stringChar) || Look(stringEscapeChar))
		//            {
		//                int escapeCharCount = 0;
		//                while (TryConsume(stringEscapeChar))
		//                {
		//                    escapeCharCount++;
		//                }
		//                Consume(stringChar);
		//                string stringText = "";
		//                while (true)
		//                {
		//                    if (Look(stringChar))
		//                    {
		//                        int foundEscapeCharCount = 0;
		//                        while (foundEscapeCharCount < escapeCharCount && Look(foundEscapeCharCount + 1, stringEscapeChar))
		//                        {
		//                            foundEscapeCharCount++;
		//                        }
		//                        if (foundEscapeCharCount == escapeCharCount)
		//                        {
		//                            Consume(stringChar);
		//                            Consume("".PadLeft(escapeCharCount, stringEscapeChar));
		//                            break;
		//                        }
		//                    }
		//                    stringText += Look();
		//                    Consume(Look());
		//                }
		//                List<string> realLines = new List<string>();
		//                string[] lines = stringText.Replace(windowsNewLine, unixNewLine.ToString()).Split(unixNewLine);
		//                for (int i = 0; i < lines.Length; i++)
		//                {
		//                    if (i == 0)
		//                    {
		//                        realLines.Add(lines[i]);
		//                    }
		//                    else
		//                    {
		//                        realLines.Add(lines[i].Remove(0, Math.Min(indentationCount + 1, lines[i].Length - lines[i].TrimStart(indentationChar).Length)));
		//                    }
		//                }
		//                string realText = string.Join("\n", realLines.ToArray());
		//                realText = realText.TrimStart('\n');
		//                Map literal = new StrategyMap(realText);
		//                @string = new StrategyMap();
		//                @string[CodeKeys.Literal] = literal;
		//            }
		//            else
		//            {
		//                @string = null;
		//            }
		//            EndExpression(extent, @string);
		//            return @string;
		//        }
		//        catch (Exception e)
		//        {
		//            return null;
		//        }
		//    }
		//    private Map LookupString()
		//    {
		//        string lookupString = "";
		//        Extent extent = BeginExpression();
		//        if (LookExcept(lookupStringForbiddenChars) && LookExcept(lookupStringFirstCharAdditionalForbiddenChars))
		//        {
		//            while (LookExcept(lookupStringForbiddenChars))
		//            {
		//                lookupString += Look();
		//                Consume(Look());
		//            }
		//        }
		//        Map lookup;
		//        if (lookupString.Length > 0)
		//        {
		//            lookup = new StrategyMap();
		//            lookup[CodeKeys.Literal] = new StrategyMap(lookupString);
		//        }
		//        else
		//        {
		//            lookup = null;
		//        }
		//        EndExpression(extent, lookup);
		//        return lookup;
		//    }
		//    private bool LookExcept(char[] exceptions)
		//    {
		//        List<char> list = new List<char>(exceptions);
		//        list.Add(endOfFileChar);
		//        return Look().ToString().IndexOfAny(list.ToArray()) == -1;
		//    }
		//    private Map LookupAnything()
		//    {
		//        Map lookupAnything;
		//        if (TryConsume(lookupStartChar))
		//        {
		//            lookupAnything = Expression();
		//            while (TryConsume(indentationChar)) ;
		//            Consume(lookupEndChar);
		//        }
		//        else
		//        {
		//            lookupAnything = null;
		//        }
		//        return lookupAnything;
		//    }
		//    private Map Lookup()
		//    {
		//        Extent extent = BeginExpression();
		//        Map lookup = LookupString();
		//        if (lookup == null)
		//        {
		//            lookup = LookupAnything();
		//        }
		//        EndExpression(extent, lookup);
		//        return lookup;
		//    }
		//    private Map Select(Map keys)
		//    {
		//        Map select;
		//        Extent extent = BeginExpression();
		//        if (keys != null)
		//        {
		//            select = new StrategyMap();
		//            select[CodeKeys.Select] = keys;
		//        }
		//        else
		//        {
		//            select = null;
		//        }
		//        EndExpression(extent, select);
		//        return select;
		//    }
		//    public Map Select()
		//    {
		//        return Select(Keys());
		//    }
		//    private Map Keys()
		//    {
		//        Extent extent = BeginExpression();
		//        Map lookups = new StrategyMap();
		//        int counter = 1;
		//        Map lookup;
		//        while (true)
		//        {
		//            lookup = Lookup();
		//            if (lookup != null)
		//            {
		//                lookups[counter] = lookup;
		//                counter++;
		//            }
		//            else
		//            {
		//                break;
		//            }
		//            if (!TryConsume(selectChar))
		//            {
		//                break;
		//            }
		//        }
		//        Map keys;
		//        if (counter > 1)
		//        {
		//            keys = lookups;
		//        }
		//        else
		//        {
		//            keys = null;
		//        }
		//        EndExpression(extent, lookups);
		//        return keys;
		//    }
		//    public Map Function()
		//    {
		//        Extent extent = BeginExpression();
		//        bool wasCallAllowed = callAllowed;
		//        callAllowed = true;
		//        Map function = null;
		//        if (TryConsume(functionChar))
		//        {
		//            Map expression = Expression();
		//            if (expression != null)
		//            {
		//                function = new StrategyMap();
		//                function[CodeKeys.Key] = CreateDefaultKey(CodeKeys.Function);
		//                Map literal = new StrategyMap();
		//                literal[CodeKeys.Literal] = expression;
		//                function[CodeKeys.Value] = literal;
		//            }
		//        }
		//        EndExpression(extent, function);
		//        callAllowed = wasCallAllowed;
		//        return function;
		//    }
		//    public const char statementChar = '=';
		//    public Map Statement(ref int count)
		//    {
		//        if (line > 25)
		//        {
		//        }
		//        Extent extent = BeginExpression();
		//        Map key = Keys();
		//        Map val;
		//        if (key != null && TryConsume(statementChar))
		//        {
		//            val = Expression();
		//        }
		//        else
		//        {
		//            TryConsume(statementChar);
		//            if (key != null)
		//            {
		//                Map select = Select(key);
		//                Map call = Call(select);
		//                if (call != null)
		//                {
		//                    val = call;
		//                }
		//                else
		//                {
		//                    val = select;
		//                }
		//            }
		//            else
		//            {
		//                val = Expression();
		//            }
		//            key = CreateDefaultKey(new StrategyMap((Integer)count));
		//            count++;
		//        }
		//        if (val == null)
		//        {
		//            SourcePosition position = new SourcePosition(Line, Column);
		//            throw new MetaException("Expected value of statement", new Extent(position, position, filePath));
		//        }
		//        Map statement = new StrategyMap();
		//        statement[CodeKeys.Key] = key;
		//        statement[CodeKeys.Value] = val;
		//        EndExpression(extent, statement);
		//        return statement;
		//    }
		//    private const char space = ' ';
		//    private const char tab = '\t';
		//    private Map CreateDefaultKey(Map literal)
		//    {
		//        Map key = new StrategyMap();
		//        Map firstKey = new StrategyMap();
		//        firstKey[CodeKeys.Literal] = literal;
		//        key[1] = firstKey;
		//        return key;
		//    }
		//    private int line = 1;
		//    private int Line
		//    {
		//        get
		//        {
		//            return line;
		//        }
		//    }
		//    private int Column
		//    {
		//        get
		//        {
		//            // mono
		//            try
		//            {
		//                int startPos = Math.Min(position, text.Length - 1);
		//                return position - text.LastIndexOf('\n', startPos);
		//            }
		//            catch
		//            {
		//                return 0;
		//            }
		//        }
		//    }
		//}
		public class Parser
		{
			public class Expression
			{
				public Map Get()
				{
					Extent extent = parser.BeginExpression();
					Map expression = parseFunction(parser);
					// refactor
					// refactor
					if (expression != null && identifier != null)
					{
						expression = new StrategyMap(identifier, expression);
					}
					parser.EndExpression(extent, expression);
					return expression;
				}
				public Parser parser;
				private Map identifier;
				public Expression(Map identifier, ParseFunction parseFunction)
				{
					this.identifier = identifier;
					this.parseFunction = parseFunction;
				}
				private ParseFunction parseFunction;
			}
			private string text;
			private int index;
			private string filePath;
			public Parser(string text, string filePath)
			{
				this.index = 0;
				this.text = text;
				this.filePath = filePath;
				foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance))
				{
					if (field.FieldType == typeof(Expression))
					{
						((Expression)field.GetValue(this)).parser = this;
					}
				}
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

			public const char stringEscapeChar = '\'';
			public const char statementChar = '=';
			private const char space = ' ';
			private const char tab = '\t';

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
			public Map GetExpression()
			{
				Map expression;
				if ((expression = EmptyMap.Get()) == null)
				{
					if ((expression = Integer.Get()) == null)
					{
						if ((expression = String.Get()) == null)
						{
							if ((expression = program.Get()) == null)
							{
								if ((expression = Call()) == null)
								{
									expression = Select();
								}

							}
						}
					}
				}
				return expression;
			}
			//public Map GetExpression()
			//{
			//    Map expression;
			//    if ((expression = EmptyMap.Get()) == null)
			//    {
			//        if ((expression = Integer.Get()) == null)
			//        {
			//            if ((expression = String.Get()) == null)
			//            {
			//                if ((expression = program.Get()) == null)
			//                {
			//                    if ((expression = Select()) != null)
			//                    {
			//                        Map call;
			//                        if ((call = Call(expression)) != null)
			//                        {
			//                            expression = call;
			//                        }
			//                    }
			//                }
			//            }
			//        }
			//    }
			//    return expression;
			//}
			public Map Call()
			{
				Map call;
				Extent extent = BeginExpression();
				int oldIndex = index;
				int oldLine = line;
				int oldColumn = Column;
				Map select = Select();
				if (select != null)
				{
					Map argument;
					if (TryConsume(callChar))
					{
						argument = GetExpression();
					}
					else
					{
						argument = program.Get();
					}
					if (argument != null)
					{
						Map callCode = new StrategyMap(
							CodeKeys.Callable, select,
							CodeKeys.Argument, argument);
						EndExpression(extent, callCode);
						call = new StrategyMap(CodeKeys.Call, callCode);
						if (functions == 0)
						{
							throw new MetaException("Function may not be called outside of function definition.", argument.Extent);
						}
					}
					else
					{
						index = oldIndex;
						line = oldLine;
						call = null;
					}
					EndExpression(extent, call);
				}
				else
				{
					index = oldIndex;
					line = oldLine;
					call = null;
				}
				return call;
			}
			//public Map Call(Map select)
			//{
			//    Map call;
			//    Extent extent = BeginExpression();
			//    Map argument;
			//    if (TryConsume(callChar))
			//    {
			//        argument = GetExpression();
			//    }
			//    else
			//    {
			//        argument = program.Get();
			//    }
			//    if (argument != null)
			//    {
			//        Map callCode = new StrategyMap(
			//            CodeKeys.Callable, select,
			//            CodeKeys.Argument, argument);
			//        EndExpression(extent, callCode);
			//        call = new StrategyMap(CodeKeys.Call, callCode);
			//        if (functions == 0)
			//        {
			//            throw new MetaException("Function may not be called outside of function definition.", argument.Extent);
			//        }
			//    }
			//    else
			//    {
			//        call = null;
			//    }
			//    EndExpression(extent, call);
			//    return call;
			//}
			public bool isStartOfFile = true;
			private void Whitespace()
			{
				while (TryConsume('\t') || TryConsume(' '))
				{
				}
			}
			int functions = 0;
			public Expression program = new Expression(CodeKeys.Program,delegate(Parser parser)
			{
				Extent extent = parser.BeginExpression();
				Map program;
				Map statements;
				if (parser.Indentation())
				{
					program = new StrategyMap();
					int counter = 1;
					int defaultKey = 1;
					statements = new StrategyMap();
					while (!parser.Look(parser.endOfFileChar))
					{
						Map statement = parser.function.Get();
						if (statement == null)
						{
							statement = parser.Statement(ref defaultKey);
						}
						statements[counter] = statement;
						counter++;
						if (!parser.TryNewLine() && !parser.Look(parser.endOfFileChar))
						{
							parser.index -= 1;
							if (!parser.TryNewLine())
							{
								parser.index -= 1;
								if (!parser.TryNewLine())
								{
									parser.index += 2;
									throw new MetaException("Expected newline.", new Extent(parser.Position, parser.Position, parser.filePath));
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
						string newIndentation = parser.GetIndentation();
						if (newIndentation.Length < parser.indentationCount)
						{
							parser.indentationCount--;
							break;
						}
						else if (newIndentation.Length == parser.indentationCount)
						{
							parser.Consume(newIndentation);
						}
						else
						{
							throw new SyntaxException("incorrect indentation",parser.filePath,parser.line, parser.Column);
						}
					}
					parser.EndExpression(extent, statements);
				}
				else
				{
					statements = null;
				}
				return statements;
			});
			private Expression EmptyMap = new Expression(CodeKeys.Literal, delegate(Parser parser)
			{
				Map program;
				if (parser.TryConsume(emptyMapChar))
				{
					program = Map.Empty;
				}
				else
				{
					program = null;
				}
				return program;
			});
			private SourcePosition Position
			{
				get
				{
					return new SourcePosition(line, Column);
				}
			}
			private void NewLine()
			{
				if (!TryNewLine())
				{
					throw new SyntaxException("Excpected newline.", filePath, this.Line, this.Column);
				}
			}
			private bool TryNewLine()
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
			private Extent BeginExpression()
			{
				return new Extent(Line, Column, 0, 0, filePath);
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
			private Expression Integer = new Expression(CodeKeys.Literal, delegate(Parser parser)
			{
				Map integer;
				if (parser.LookAny(parser.firstIntegerChars))
				{
					string integerString = "";
					integerString += parser.ConsumeGet();
					while (parser.LookAny(parser.integerChars))
					{
						integerString += parser.ConsumeGet();
					}
					integer = Meta.Integer.ParseInteger(integerString);
				}
				else
				{
					integer = null;
				}
				return integer;
			});
			private Expression String = new Expression(CodeKeys.Literal, delegate(Parser parser)
			{
				try
				{
					Map @string;
					if (parser.Look(stringChar) || parser.Look(stringEscapeChar))
					{
						int escapeCharCount = 0;
						while (parser.TryConsume(stringEscapeChar))
						{
							escapeCharCount++;
						}
						parser.Consume(stringChar);
						string stringText = "";
						while (true)
						{
							if (parser.Look(stringChar))
							{
								int foundEscapeCharCount = 0;
								while (foundEscapeCharCount < escapeCharCount && parser.Look(foundEscapeCharCount + 1, stringEscapeChar))
								{
									foundEscapeCharCount++;
								}
								if (foundEscapeCharCount == escapeCharCount)
								{
									parser.Consume(stringChar);
									parser.Consume("".PadLeft(escapeCharCount, stringEscapeChar));
									break;
								}
							}
							stringText += parser.Look();
							parser.Consume(parser.Look());
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
								realLines.Add(lines[i].Remove(0, Math.Min(parser.indentationCount + 1, lines[i].Length - lines[i].TrimStart(indentationChar).Length)));
							}
						}
						string realText = string.Join("\n", realLines.ToArray());
						realText = realText.TrimStart('\n');

						@string = realText;
					}
					else
					{
						@string = null;
					}
					return @string;
				}
				catch (Exception e)
				{
					return null;
				}
			});
			private Expression LookupString = new Expression(CodeKeys.Literal, delegate(Parser parser)
			{
				string lookupString = "";
				if (parser.LookExcept(lookupStringForbiddenChars) && parser.LookExcept(parser.lookupStringFirstCharAdditionalForbiddenChars))
				{
					while (parser.LookExcept(lookupStringForbiddenChars))
					{
						lookupString += parser.Look();
						parser.Consume(parser.Look());
					}
				}
				Map lookup;
				if (lookupString.Length > 0)
				{
					lookup = lookupString;
				}
				else
				{
					lookup = null;
				}
				return lookup;
			});
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
					lookupAnything = GetExpression();
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
				Extent extent = BeginExpression();
				Map lookup = LookupString.Get();
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
				Extent extent = BeginExpression();
				if (keys != null)
				{
					select = new StrategyMap(CodeKeys.Select, keys);
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
				return Select(Keys.Get());
			}
			private Expression Keys = new Expression(null, delegate(Parser parser)
			{
				Map lookups = new StrategyMap();
				int counter = 1;
				Map lookup;
				while (true)
				{
					lookup = parser.Lookup();
					if (lookup != null)
					{
						lookups[counter] = lookup;
						counter++;
					}
					else
					{
						break;
					}
					if (!parser.TryConsume(selectChar))
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
				return keys;
			});
			public delegate Map ParseFunction(Parser parser);

			public Expression function = new Expression(null,delegate(Parser parser)
			{
				parser.functions++;
				Map function = null;
				if (parser.TryConsume(functionChar))
				{
					Map expression = parser.GetExpression();
					if (expression != null)
					{
						function = new StrategyMap(
							CodeKeys.Key, parser.CreateDefaultKey(CodeKeys.Function),
							CodeKeys.Value, new StrategyMap(CodeKeys.Literal, expression));
					}
				}
				parser.functions--;
				return function;
			});
			public Map Statement(ref int count)
			{
				Extent extent = BeginExpression();
				int oldIndex = index;
				Map key = Keys.Get();
				Map val;
				if (key != null && TryConsume(statementChar))
				{
					val = GetExpression();
				}
				else
				{
					index = oldIndex;
					TryConsume(statementChar);
					if (key != null)
					{
						Map call = Call();
						if (call != null)
						{
							val = call;
						}
						else
						{
							val = Select();
						}
					}
					else
					{
						val = GetExpression();
					}
					key = CreateDefaultKey(count);
					count++;
				}
				if (val == null)
				{
					SourcePosition position = new SourcePosition(Line, Column);
					throw new MetaException("Expected value of statement", new Extent(position, position, filePath));
				}
				Map statement = new StrategyMap(
					CodeKeys.Key, key,
					CodeKeys.Value, val);

				EndExpression(extent, statement);
				return statement;
			}
			//public Map Statement(ref int count)
			//{
			//    Extent extent = BeginExpression();
			//    Map key = Keys.Get();
			//    Map val;
			//    if (key != null && TryConsume(statementChar))
			//    {
			//        val = GetExpression();
			//    }
			//    else
			//    {
			//        TryConsume(statementChar);
			//        if (key != null)
			//        {
			//            Map select = Select(key);
			//            Map call = Call(select);
			//            if (call != null)
			//            {
			//                val = call;
			//            }
			//            else
			//            {
			//                val = select;
			//            }
			//        }
			//        else
			//        {
			//            val = GetExpression();
			//        }
			//        key = CreateDefaultKey(count);
			//        count++;
			//    }
			//    if (val == null)
			//    {
			//        SourcePosition position = new SourcePosition(Line, Column);
			//        throw new MetaException("Expected value of statement", new Extent(position, position, filePath));
			//    }
			//    Map statement = new StrategyMap(
			//        CodeKeys.Key, key,
			//        CodeKeys.Value, val);

			//    EndExpression(extent, statement);
			//    return statement;
			//}
			private Map CreateDefaultKey(Map literal)
			{
				return new StrategyMap(1, new StrategyMap(CodeKeys.Literal, literal));
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
