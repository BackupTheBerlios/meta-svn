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
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Drawing;
using System.Security.Cryptography;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Windows;
using SdlDotNet;

namespace Meta
{
	public abstract class Expression
	{
		public abstract Map Evaluate(Map context);
	}
	internal class HiPerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime, stopTime;
        private long freq;

        public HiPerfTimer()
        {
            startTime = 0;
            stopTime  = 0;

            if (QueryPerformanceFrequency(out freq) == false)
            {
                throw new ApplicationException();
            }
        }
        public void Start()
        {
            Thread.Sleep(0);
            QueryPerformanceCounter(out startTime);
        }
        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }
        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double) freq;
            }
        }
    }
	public class Call : Expression
	{
		private Map parameterName;
		List<Map> expressions;
		Map code;
		public Call(Map code, Map parameterName)
		{
			this.code = code;
			this.expressions = code.Array;
			if (expressions.Count == 1)
			{
				expressions.Add(new StrategyMap(CodeKeys.Literal, Map.Empty));
			}
			this.parameterName = parameterName;
		}
		public static Dictionary<string, double> calls = new Dictionary<string, double>();
		public override Map Evaluate(Map current)
		{
			try
			{
				//HiPerfTimer timer = null;
				//if (Interpreter.profiling)
				//{
				//    timer = new HiPerfTimer();
				//    timer.Start();
				//}
				Map callable = expressions[0].GetExpression().Evaluate(current);
				for (int i = 1; i < expressions.Count; i++)
				{
					Map arg = expressions[i].GetExpression().Evaluate(current);
					//Position scope = callable.Scope;
					//if (scope == null)
					//{
					//    scope = callable.Parent;
					//}
					callable = callable.Call(arg);
					//callable = callable.Call(arg, callable);
					//callable = callable.Call(arg, callable);
				}
				//if (Interpreter.profiling)
				//{
				//    timer.Stop();
				//    string special = SpecialString(current);
				//    if (!calls.ContainsKey(special))
				//    {
				//        calls[special] = 0;
				//    }
				//    calls[special] += timer.Duration;
				//}
				return callable;
			}
			catch (MetaException e)
			{
				//e.InvocationList.Add(new ExceptionLog(expressions[0].Extent, current));
				throw e;
			}
			catch (Exception e)
			{
				throw new MetaException(e.ToString(), this.expressions[0].Extent);
			}
		}
		//private string SpecialString(Position position)
		//{
		//    string result="";
		//    foreach (Map key in position.Keys)
		//    {
		//        result += key.ToString();
		//    }
		//    return result;
		//}
	}
	public class Program : Expression
	{
		public bool IsFunction
		{
			get
			{
				return code.ContainsKey(CodeKeys.Parameter);
			}
		}
		private Map code;
		private List<StatementBase> statements;
		public Program(Map code)
		{
			this.code = code;
			statements=code.Array.ConvertAll(new Converter<Map,StatementBase>(delegate(Map map) {return map.GetStatement();}));
		}
		public override Map Evaluate(Map parent)
		{
			Map context=new StrategyMap();
			context.Scope = parent;
			foreach (StatementBase statement in statements)
			{
				statement.Assign(context);
			}
			//contextPosition.Get().Scope = parent;
			return context;
		}
		//public override Map Evaluate(Position parent)
		//{
		//    Position contextPosition=parent.AddCall(new StrategyMap());
		//    foreach (StatementBase statement in statements)
		//    {
		//        statement.Assign(contextPosition);
		//    }
		//    contextPosition.Get().Scope = parent;
		//    return contextPosition;
		//}
	}
	public class Literal : Expression
	{
		public Map Map
		{
			get
			{
				return literal;
			}
		}
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		private Map literal;
		public Literal(Map code)
		{
			if (code.Count!=0 && code.IsString)
			{
				this.literal = code.GetString();
			}
			else
			{
				this.literal = code.Copy();
			}
		}
		public override Map Evaluate(Map context)
		{
			return literal.Copy();
		}
		//public override Map Evaluate(Map context)
		//{
		//    Position position=context.AddCall(literal);
		//    position.Get().Scope = position.Parent;
		//    return position;
		//}
	}
	public class Root : Expression
	{
		public override Map Evaluate(Map selected)
		{
			return Gac.gac;
			//return RootPosition.rootPosition;
		}
	}
	public class Search : Expression
	{
		public Expression Key
		{
			get
			{
				return keyExpression.GetExpression();
			}
		}
		private Map keyExpression;
		public Search(Map keyExpression)
		{
			this.keyExpression = keyExpression;
		}
		public override Map Evaluate(Map context)
		{
			Map key = keyExpression.GetExpression().Evaluate(context);
			//Map key = keyPosition.Get();

			Map selection = context;
			while (!selection.ContainsKey(key))
			{
				//if (selection.Parent == null)
				////if (selection.Parent == null)
				//{
				//    selection = null;
				//    break;
				//}
				//else
				//{
				if (selection.Scope != null)
				{
					selection = selection.Scope;
				}
				else
				{
					selection = null;
					break;
					//selection = selection.Parent;
				}
				//}
			}
			if (selection == null)
			{
				throw new KeyNotFound(key, keyExpression.Extent, null);
			}
			else
			{
				return selection[key];
				//Position lastEvaluated = new Position(selection, key);
				//return selection.AddCall(lastEvaluated.Get());
			}
		}
		//public override Map Evaluate(Map context)
		//{
		//    Position keyPosition = keyExpression.GetExpression().Evaluate(context);
		//    Map key = keyPosition.Get();

		//    Position selection = context;
		//    while (!selection.Get().ContainsKey(key))
		//    {
		//        if (selection.Parent == null)
		//        {
		//            selection = null;
		//            break;
		//        }
		//        else
		//        {
		//            if (selection.Get().Scope != null)
		//            {
		//                selection = selection.Get().Scope;
		//            }
		//            else
		//            {
		//                selection = selection.Parent;
		//            }
		//        }
		//    }
		//    if (selection == null)
		//    {
		//        throw new KeyNotFound(key, keyExpression.Extent, null);
		//    }
		//    else
		//    {
		//        Position lastEvaluated = new Position(selection, key);
		//        return selection.AddCall(lastEvaluated.Get());
		//    }
		//}
	}
	public class Select : Expression
	{
		public List<Map> Subselects
		{
			get
			{
				return subselects;
			}
		}
		private List<Map> subselects;
		public Select(Map code)
		{
			this.subselects = code.Array;
		}
		public override Map Evaluate(Map context)
		{
			Map selected = subselects[0].GetExpression().Evaluate(context);
			for (int i = 1; i < subselects.Count; i++)
			{
				selected = selected[subselects[i].GetExpression().Evaluate(context)];
			}
			return selected;
		}
		//public override Map Evaluate(Map context)
		//{
		//    Position selected = subselects[0].GetExpression().Evaluate(context);
		//    for (int i = 1; i < subselects.Count; i++)
		//    {
		//        selected = selected[subselects[i].GetExpression().Evaluate(context).Get()];
		//    }
		//    return selected;
		//}
	}
	public class KeyStatement : StatementBase
	{
		private Map key;
		private Map value;
		public KeyStatement(Map code)
		{
			this.key = code[CodeKeys.Key];
			this.value = code[CodeKeys.Value];
		}
		public override void Assign(Map context)
		{
			context[key.GetExpression().Evaluate(context)] = value.GetExpression().Evaluate(context);
		}
		//public override void Assign(Map context)
		//{
		//    Position selected = context;
		//    context.Get()[key.GetExpression().Evaluate(context).Get()] = value.GetExpression().Evaluate(context).Get();
		//}
	}
	public class CurrentStatement : StatementBase
	{
		public CurrentStatement(Map code)
		{
			this.expression = code[CodeKeys.Value];
		}
		private Map expression;
		public override void Assign(Map context)
		{
			Map value = expression.GetExpression().Evaluate(context);

			if (value.Scope != null)
			{
				// completely unlogical, actually
				if (context.Scope != null)
				{
					value.Scope = context.Scope;
				}
				else
				{
					//value.Scope = context.Parent;
				}
			}
			context.Nuke(value);
		}
		//public override void Assign(Map context)
		//{
		//    Map value = expression.GetExpression().Evaluate(context).Get();

		//    if (value.Scope != null)
		//    {
		//        if (context.Get().Scope != null)
		//        {
		//            value.Scope = context.Get().Scope;
		//        }
		//        else
		//        {
		//            value.Scope = context.Parent;
		//        }
		//    }
		//    context.Assign(value);
		//}
	}
	public abstract class StatementBase
	{
		public abstract void Assign(Map context);
	}
	public class Statement : StatementBase
	{
		public Expression Value
		{
			get
			{
				return value.GetExpression();
			}
		}
		private List<Map> keys;
		public List<Map> Keys
		{
			get
			{
				return keys;
			}
		}
		private Map value;
		public Statement(Map code)
		{
			this.keys = code[CodeKeys.Keys].Array;
			this.value = code[CodeKeys.Value];
		}
		public override void Assign(Map context)
		{
			Map selection = context;
			Map key = keys[0].GetExpression().Evaluate(context);
			while (!selection.ContainsKey(key))
			{
				//if (selection.Parent == null)
				//{
				//    selection = null;
				//    break;
				//}
				//else
				//{
					if (selection.Scope != null)
					{
						selection = selection.Scope;
					}
					else
					{
						//selection = selection.Parent;
					}
				//}
			}
			Map selected;
			if (selection == null)
			{
				throw new KeyNotFound(key, keys[0].Extent, null);
			}
			else
			{
				selected = selection;
			}
			for (int i = 1; i < keys.Count; i++)
			{
				selected = selected[key];
				key = keys[i].GetExpression().Evaluate(context);
			}
			Map val = value.GetExpression().Evaluate(context);
			selected[key]=val;
			//selected.Assign(key, val);
		}
		//public override void Assign(Map context)
		//{
		//    Position selected = context;

		//    Position selection = selected;
		//    Map key = keys[0].GetExpression().Evaluate(context).Get();
		//    while (!selection.Get().ContainsKey(key))
		//    {
		//        if (selection.Parent == null)
		//        {
		//            selection = null;
		//            break;
		//        }
		//        else
		//        {
		//            if (selection.Get().Scope != null)
		//            {
		//                selection = selection.Get().Scope;
		//            }
		//            else
		//            {
		//                selection = selection.Parent;
		//            }
		//        }
		//    }
		//    if (selection == null)
		//    {
		//        throw new KeyNotFound(key, keys[0].Extent, null);
		//    }
		//    else
		//    {
		//        selected = selection;
		//    }
		//    for (int i = 1; i < keys.Count; i++)
		//    {
		//        selected = selected[key];
		//        key = keys[i].GetExpression().Evaluate(context).Get();
		//    }
		//    Map val = value.GetExpression().Evaluate(context).Get();
		//    selected.Assign(key, val);
		//}
	}
	public class Library
	{
		public static Map EnumerableToArray(Map map)
		{
			Map result = new StrategyMap();
			foreach (object entry in (IEnumerable)((ObjectMap)map).Object)
			{
				result.Append(Transform.ToMeta(entry));
			}
			return result;
		}
		public static Map Reverse(Map arg)
		{
			List<Map> list = new List<Map>(arg.Array);
			list.Reverse();
			return new StrategyMap(list);
		}
		public static Map Try(Map tryFunction, Map catchFunction)
		{
			try
			{
				return tryFunction.Call(Map.Empty);
			}
			catch (Exception e)
			{
				return catchFunction.Call(new ObjectMap(e));
			}
		}
		//public static Map Try(Map tryFunction, Map catchFunction)
		//{
		//    try
		//    {
		//        return tryFunction.Call(Map.Empty, tryFunction.Scope).Get();
		//    }
		//    catch (Exception e)
		//    {
		//        return catchFunction.Call(new ObjectMap(e), catchFunction.Scope).Get();
		//    }
		//}
		public static Map With(Map obj, Map values)
		{
			foreach (KeyValuePair<Map, Map> entry in values)
			{
				obj[entry.Key] = entry.Value;
			}
			return obj;
		}
		public static Map Merge(Map arg, Map map)
		{
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				arg[pair.Key] = pair.Value;
			}
			return arg;
		}
		public static Map Join(Map arg,Map map)
		{
			arg.AppendRange(map.Array);
			return arg;
		}
		public static Map Range(Map arg)
		{
			int end = arg.GetNumber().GetInt32();
			Map result = new StrategyMap();
			for (int i = 1; i <= end; i++)
			{
				result.Append(i);
			}
			return result;
		}
	}
	public class Interpreter
	{
		static Interpreter()
		{
			try
			{

				Gac.gac["library"] = Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta")).Call(Map.Empty);
				//Gac.gac["library"] = Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta")).Call(Map.Empty, RootPosition.rootPosition).Get();
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public static bool profiling=false;
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				//UseConsole();
				//MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
				//return;
				switch (args[0])
				{
					case "-test":
						UseConsole();
						new MetaTest().Run();
						break;
					case "-profile":
						profiling = true;
						DateTime start = DateTime.Now;
						AllocConsole();
						int level;
						Console.WriteLine((DateTime.Now - start).TotalSeconds);
						List<KeyValuePair<string, double>> profiled = new List<KeyValuePair<string, double>>(Call.calls);
						profiled.Sort(new Comparison<KeyValuePair<string, double>>(delegate(KeyValuePair<string, double> a, KeyValuePair<string, double> b)
						{
							return a.Value.CompareTo(b.Value);
						}));
						profiled.Reverse();
						for (int i = 0; i < profiled.Count; i++)
						{
							KeyValuePair<string, double> entry = profiled[i];
							Console.WriteLine(entry.Key + " " + new TimeSpan(0, 0, Convert.ToInt32(entry.Value)).ToString());
						}
						break;
					default:
						break;
				}
			}
			catch (MetaException e)
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
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();

		public static bool useConsole = false;
		public static void UseConsole()
		{
			AllocConsole();
			useConsole = true;
			Console.SetBufferSize(80, 1000);
		}
		public static string InstallationPath
		{
			get
			{
				return @"D:\Meta\0.2\";
			}
		}
	}
	[Serializable]
	//public class Position
	//{
	//    public Position AddCall(Map map)
	//    {
	//        return new TemporaryPosition(map,this, Map.Empty);
	//    }
	//    public override bool Equals(object obj)
	//    {
	//        Position position=(Position)obj;
	//        return position.key != null && position.key.Equals(key) && position.parent.Equals(parent);
	//    }
	//    protected Position()
	//    {
	//    }
	//    private Map key;
	//    private Position parent;

	//    public void Assign(Map key, Map value)
	//    {
	//        Get()[key] = value;
	//    }
	//    public virtual void Assign(Map value)
	//    {
	//        Parent.Assign(key, value);
	//    }
	//    public Position this[Map key]
	//    {
	//        get
	//        {
	//            if (Get().ContainsKey(key))
	//            {
	//                return new Position(this, key);
	//            }
	//            else
	//            {
	//                throw new Exception("Position does not exist "+key.ToString()+" in "+this.ToString());
	//            }
	//        }
	//    }
	//    public Position(Position parent, Map key)
	//    {
	//        this.parent = parent;
	//        this.key = key;
	//    }
	//    public Position Parent
	//    {
	//        get
	//        {
	//            return parent;
	//        }
	//    }
	//    public virtual Map Get()
	//    {
	//        return DetermineMap();
	//    }
	//    public virtual Map DetermineMap()
	//    {
	//        Map map = parent.Get();
	//        Map result = map[key];
	//        if (result == null)
	//        {
	//            throw new ApplicationException("Position does not exist");
	//        }
	//        return result;
	//    }
	//    public List<Map> Keys
	//    {
	//        get
	//        {
	//            Position position = this;
	//            List<Map> keys = new List<Map>();
	//            while (position != null && position.key != null)
	//            {
	//                keys.Add(position.key);
	//                position = position.Parent;
	//            }
	//            keys.Reverse();
	//            return keys;
	//        }
	//    }
	//    public override string ToString()
	//    {
	//        string text = "";
	//        foreach (Map map in Keys)
	//        {
	//            text += map.ToString();
	//        }
	//        return text;
	//    }
	//}
	//[Serializable]
	//public class TemporaryPosition : Position
	//{
	//    public override Map Get()
	//    {
	//        return map;
	//    }
	//    private Map map;
	//    public TemporaryPosition(Map map,Position parent,Map key):base(parent,key)
	//    {
	//        this.map = map.Copy();
	//        //this.map = map.Copy();
	//    }
	//    public override void Assign(Map value)
	//    {
	//        map = value;
	//    }
	//}
	//[Serializable]
	//public class RootPosition : Position
	//{
	//    public override bool Equals(object obj)
	//    {
	//        return obj is RootPosition;
	//    }
	//    public static RootPosition rootPosition=new RootPosition();
	//    private RootPosition()
	//    {
	//    }
	//    public override Map Get()
	//    {
	//        return Gac.gac;
	//    }
	//}
	public class KeyChangedEventArgs : EventArgs
	{
		public KeyChangedEventArgs(Map key)
		{
			this.key = key;
		}
		private Map key;
		public Map Key
		{
			get
			{
				return key;
			}
		}
	}
	public delegate void KeyChangedEventHandler(KeyChangedEventArgs e);
	[Serializable]
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public virtual void Nuke(Map map)
		{
			throw new Exception("not implemented");
		}
		public List<Map> Values
		{
			get
			{
				List<Map> values = new List<Map>();
				foreach (Map key in Keys)
				{
					values.Add(this[key]);
				}
				return values;
			}
		}
		public override string ToString()
		{
			return Meta.Serialize.ValueFunction(this);
		}
		public Map TryGetValue(Map key)
		{
			return Get(key);
		}
		public Map this[Map key]
		{
			get
			{
				Map value = TryGetValue(key);
				if (value == null)
				{
					throw new KeyDoesNotExist(key, null, this);
				}
				return value;
			}
			set
			{
				if (value != null)
				{
					compiledCode = null;
					Map val;
					val = value;
					//val = value;
					//val = value.Copy();
					Set(key, val);
					if (KeyChanged != null)
					{
						this.KeyChanged(new KeyChangedEventArgs(key));
						this.KeyChanged = null;
					}
				}
			}
		}
		public event KeyChangedEventHandler KeyChanged;
		protected abstract Map Get(Map key);
		protected abstract void Set(Map key, Map val);
		public int numCalls = 0;
		public virtual void AppendRange(IEnumerable<Map> array)
		{
			AppendRangeDefault(array);
		}
		public virtual void AppendRangeDefault(IEnumerable<Map> array)
		{
			int counter = ArrayCount + 1;
			foreach (Map map in array)
			{
				this[counter] = map;
				counter++;
			}
		}
		private object compiledCode;

		public StatementBase GetStatement()
		{
			if (compiledCode == null)
			{
				if (ContainsKey(CodeKeys.Keys))
				{
					compiledCode = new Statement(this);

				}
				else if (ContainsKey(CodeKeys.Current))
				{
					compiledCode = new CurrentStatement(this);
				}
				else if (ContainsKey(CodeKeys.Key))
				{
					compiledCode = new KeyStatement(this);
				}
				else
				{
					throw new ApplicationException("Cannot compile map");
				}
			}
			return (StatementBase)compiledCode;
		}
		public Expression GetExpression()
		{
			if (compiledCode == null)
			{
				compiledCode = CreateExpression();
			}
			return (Expression)compiledCode;
		}
		public Expression CreateExpression()
		{
			if (ContainsKey(CodeKeys.Call))
			{
				return new Call(this[CodeKeys.Call],this.TryGetValue(CodeKeys.Parameter));
			}
			else if (ContainsKey(CodeKeys.Program))
			{
				return new Program(this[CodeKeys.Program]);
			}
			else if (ContainsKey(CodeKeys.Literal))
			{
				return new Literal(this[CodeKeys.Literal]);
			}
			else if (ContainsKey(CodeKeys.Select))
			{
				return new Select(this[CodeKeys.Select]);
			}
			else if (ContainsKey(CodeKeys.Search))
			{
				return new Search(this[CodeKeys.Search]);
			}
			else if (ContainsKey(CodeKeys.Root))
			{
				return new Root();
			}
			else
			{
				throw new ApplicationException("Cannot compile map "+Meta.Serialize.ValueFunction(this));
			}
		}
		public virtual void Append(Map map)
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
			StringBuilder text = new StringBuilder("");
			foreach (Map key in Keys)
			{
				text.Append(Convert.ToChar(this[key].GetNumber().GetInt32()));
			}
			return text.ToString();
		}
		public Number GetNumberDefault()
		{
			Number number;
            if (Count==0)
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
		//public Position Scope
		//{
		//    get
		//    {
		//        return scope;
		//    }
		//    set
		//    {
		//        scope = value;
		//    }
		//}
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

		public virtual void Remove(Map key)
		{
			throw new ApplicationException("Method not implemented");
		}
		public virtual Map Call(Map arg)
		{
			if (ContainsKey(CodeKeys.Function))
			{
				Map argumentScope = new StrategyMap(this[CodeKeys.Function][CodeKeys.Parameter], arg);
				argumentScope.Scope = this.Scope;
				return this[CodeKeys.Function][CodeKeys.Expression].GetExpression().Evaluate(argumentScope);
				//return this[CodeKeys.Function][CodeKeys.Expression].GetExpression().Evaluate(bodyPosition);
			}
			else
			{
				throw new ApplicationException("Map is not a function: " + Meta.Serialize.ValueFunction(this));
			}
		}
		//public virtual Position Call(Map arg, Position position)
		//{
		//    if (ContainsKey(CodeKeys.Function))
		//    {
		//        Position bodyPosition = position.AddCall(new StrategyMap(this[CodeKeys.Function][CodeKeys.Parameter], arg));
		//        return this[CodeKeys.Function][CodeKeys.Expression].GetExpression().Evaluate(bodyPosition);
		//    }
		//    else
		//    {
		//        throw new ApplicationException("Map is not a function: "+Meta.Serialize.ValueFunction(this));
		//    }
		//}
		public ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				foreach (Map key in KeysImplementation)
				{
					keys.Add(key);
				}
				return keys;
			}
		}
		protected abstract ICollection<Map> KeysImplementation
		{
			get;
		}
		public Map Copy()
		{
			Map clone = CopyData();
			clone.numCalls = numCalls;
			clone.Scope = Scope;
			clone.Extent = Extent;
			return clone;
		}
		protected abstract Map CopyData();
		public bool ContainsKey(Map key)
		{
			return ContainsKeyImplementation(key);
		}
		protected abstract bool ContainsKeyImplementation(Map key);
		public override int GetHashCode()
		{
			if (IsNumber)
			{
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
		public static implicit operator Map(Number integer)
		{
			return new StrategyMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new StrategyMap(new Number((double)(boolean ? 1 : 0)));
		}
		public static implicit operator Map(char character)
		{
			return new StrategyMap(new Number((double)character));
		}
		public static implicit operator Map(byte integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(int integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(long integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new StrategyMap(new Number((double)integer));
		}
		public static implicit operator Map(string text)
		{
			return new StrategyMap(text);
		}
		[NonSerialized]
		private Map scope;
		//private Position scope;
	}
	[Serializable]
	public class StrategyMap:Map
	{
		public override void Nuke(Map map)
		{
			this.strategy = new EmptyStrategy();
			foreach (Map key in map.Keys)
			{
				this[key] = map[key];
			}
			//this.strategy = ((StrategyMap)map).strategy;
		}
		public override void Append(Map map)
		{
			strategy.Append(map,this);
		}
		public override void Remove(Map key)
		{
			strategy.Remove(key,this);
		}
		//public StrategyMap(Map scope)
		//    : this()
		//{
		//    this.Scope = scope;
		//}
		//public StrategyMap(Position scope)
		//    : this()
		//{
		//    this.Scope = scope;
		//}
		protected MapStrategy strategy;
		public StrategyMap(bool boolean)
			: this(new Number((double)Convert.ToInt32(boolean)))
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
		}
		public StrategyMap()
			: this(new EmptyStrategy())
		{
		}
		public StrategyMap(Number number)
			: this(new NumberStrategy(number))
		{
		}
		public StrategyMap(string text)
			: this(new StringStrategy(text))
		{
		}
		//public StrategyMap(params Map[] keysAndValues)
		//    : this(keysAndValues)
		//{
		//    //this.Scope = scope;
		//}
		//public StrategyMap(Map scope, params Map[] keysAndValues)
		//    : this(keysAndValues)
		//{
		//    this.Scope = scope;
		//}
		//public StrategyMap(Position scope, params Map[] keysAndValues)
		//    : this(keysAndValues)
		//{
		//    this.Scope = scope;
		//}
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
			strategy.Set(key, value,this); 
		}
		protected override Map CopyData() 
		{ 
			return strategy.CopyData(); 
		}
		protected override bool ContainsKeyImplementation(Map key) 
		{ 
			return strategy.ContainsKey(key); 
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
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
				isEqual = ((StrategyMap)toCompare).strategy.EqualStrategy(strategy);
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
			}
		}
	}
	public class Transform
	{
		public static object ToDotNet(Map meta, Type target)
		{
			bool converted;
			if (target == typeof(void))
			{
				return null;
			}
			else
			{
				object dotNet = TryToDotNet(meta, target, out converted);
				if (!converted)
				{
					object abc = TryToDotNet(meta, target, out converted);
					throw new ApplicationException("Cannot convert " + Serialize.ValueFunction(meta) + " to " + target.ToString() + ".");
				}
				return dotNet;
			}
		}
		public static Delegate CreateDelegateFromCode(Type delegateType, Map code)
		{
			MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters)
			{
				arguments.Add(parameter.ParameterType);
			}
			DynamicMethod method = new DynamicMethod("EventHandler",
				invokeMethod.ReturnType,
				arguments.ToArray(),
				typeof(Map).Module);
			ILGenerator il = method.GetILGenerator();

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

			if (invokeMethod.ReturnType == typeof(void))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Castclass, invokeMethod.ReturnType);
				il.Emit(OpCodes.Ret);
			}
			Delegate del = (Delegate)method.CreateDelegate(delegateType, new MetaDelegate(code, invokeMethod.ReturnType));
			return del;
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
				Map pos = this.callable;
				foreach (object argument in arguments)
				{
					pos = pos.Call(Transform.ToSimpleMeta(argument));
				}
				return Meta.Transform.ToDotNet(pos, this.returnType);
			}
			//public object Call(object[] arguments)
			//{
			//    Map arg = new StrategyMap();
			//    Map pos = this.callable;
			//    foreach (object argument in arguments)
			//    {
			//        pos = pos.Call(Transform.ToSimpleMeta(argument),pos.Scope).Get();
			//    }
			//    return Meta.Transform.ToDotNet(pos, this.returnType);
			//}
		}
		public static object TryToDotNet(Map meta, Type target,out bool wasConverted)
		{
			object dotNet = null;
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
					FieldInfo[] fields = target.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

					if (target == typeof(Number) && meta.IsNumber)
					{
						dotNet = meta.GetNumber();
					}
					else if (target!=typeof(void) && target.IsValueType && meta.ArrayCount == meta.Count && meta.Count == 2 && Library.Join(meta[1], meta[2]).ArrayCount == fields.Length)
					{
						dotNet = target.InvokeMember(".ctor", BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public, null, null, new object[] { });
						meta = Library.Join(meta[1], meta[2]);
						int index = 1;
						foreach (FieldInfo field in fields)
						{
							field.SetValue(dotNet, Transform.ToDotNet(meta[index], field.FieldType));
							index++;
						}

					}
					else if (target!=typeof(void) && target.IsValueType && meta.ArrayCount == meta.Count && meta.Count == fields.Length)
					{
						dotNet = target.InvokeMember(".ctor", BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public, null, null, new object[] { });
						int index = 1;
						foreach (FieldInfo field in fields)
						{
							field.SetValue(dotNet, Transform.ToDotNet(meta[index], field.FieldType));
							index++;
						}

					}
					else if (target == typeof(Type) && meta is TypeMap)
					{
						dotNet = ((TypeMap)meta).Type;
					}
					else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
							&& meta.ContainsKey(CodeKeys.Function))
					{
						dotNet = CreateDelegateFromCode(target, meta);
					}
					else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).Type))
					{
						dotNet = ((ObjectMap)meta).Object;
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
							bool c;
							object o = Transform.TryToDotNet(m, elementType,out c);
							if (c)
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
			//}
			if (dotNet == null)
			{
				if(meta is ObjectMap && ((ObjectMap)meta).Type==target)
				{
					dotNet = ((ObjectMap)meta).Object;
				}
			}
			wasConverted = dotNet != null;
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
						return Convert.ToInt32(dotNet);
					case TypeCode.Double:
						return Convert.ToInt32(dotNet);
					case TypeCode.Int16:
						return (short)dotNet;
					case TypeCode.Int32:
						return (int)dotNet;
					case TypeCode.Int64:
						return (long)dotNet;
					case TypeCode.Object:
						if (dotNet is Number)
						{
							return new StrategyMap((Number)dotNet);
						}
						if(dotNet is Map)
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
						return Convert.ToInt32(dotNet);
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
	public delegate Map CallDelegate(Map argument);
	[Serializable]
	public abstract class MethodImplementation : Map
	{
		//public static Position currentPosition;
		protected MethodBase method;
		protected object obj;
		protected Type type;
		public MethodImplementation(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
			if (method != null)
			{
				this.parameters = method.GetParameters();
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
		ParameterInfo[] parameters;
		public override Map Call(Map argument)
		{
			return DecideCall(argument, new List<object>());
			//return DecideCall(argument, new List<object>(), position);
		}
		private Map DecideCall(Map argument, List<object> oldArguments)
		{
			List<object> arguments = new List<object>(oldArguments);
			if (parameters.Length != 0)
			{
				bool converted;
				object arg = Transform.TryToDotNet(argument, parameters[arguments.Count].ParameterType, out converted);
				if (!converted)
				{
					throw new Exception("Could not convert argument " + Meta.Serialize.ValueFunction(argument) + "\n to " + parameters[arguments.Count].ParameterType.ToString());
				}
				else
				{
					arguments.Add(arg);
				}
			}
			if (arguments.Count >= parameters.Length)
			{
				return Invoke(argument, arguments.ToArray());
				//return Invoke(argument, arguments.ToArray(), position);
			}
			else
			{
				return new ObjectMap(new CallDelegate(delegate(Map map)
				{
					return DecideCall(map, arguments);
				}));
				//return position.AddCall(new ObjectMap(new CallDelegate(delegate(Map map)
				//{
				//    return DecideCall(map, arguments, position).Get();
				//})));
			}
		}
		//private Position DecideCall(Map argument, List<object> oldArguments, Position position)
		//{
		//    List<object> arguments = new List<object>(oldArguments);
		//    if (parameters.Length != 0)
		//    {
		//        bool converted;
		//        object arg = Transform.TryToDotNet(argument, parameters[arguments.Count].ParameterType, out converted);
		//        if (!converted)
		//        {
		//            throw new Exception("Could not convert argument " + Meta.Serialize.ValueFunction(argument) + "\n to " + parameters[arguments.Count].ParameterType.ToString());
		//        }
		//        else
		//        {
		//            arguments.Add(arg);
		//        }
		//    }
		//    if (arguments.Count >= parameters.Length)
		//    {
		//        return Invoke(argument, arguments.ToArray(), position);
		//    }
		//    else
		//    {
		//        return position.AddCall(new ObjectMap(new CallDelegate(delegate(Map map)
		//        {
		//            return DecideCall(map, arguments, position).Get();
		//        })));
		//    }
		//}
		private Map Invoke(Map argument, object[] arguments)
		{
			//currentPosition = position;
			bool converted;
			try
			{
				Map result = Transform.ToMeta(
					method is ConstructorInfo ?
						((ConstructorInfo)method).Invoke(arguments) :
						 method.Invoke(obj, arguments));
				return result;
				//return position.AddCall(result);
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					throw e.InnerException;
				}
				else
				{
					throw new ApplicationException("implementation exception: " + e.InnerException.ToString() + e.StackTrace, e.InnerException);
				}
			}
		}
		//private Position Invoke(Map argument, object[] arguments, Position position)
		//{
		//    currentPosition = position;
		//    bool converted;
		//    try
		//    {
		//        Map result = Transform.ToMeta(
		//            method is ConstructorInfo ?
		//                ((ConstructorInfo)method).Invoke(arguments) :
		//                 method.Invoke(obj, arguments));
		//        return position.AddCall(result);
		//    }
		//    catch (Exception e)
		//    {
		//        if (e.InnerException != null)
		//        {
		//            throw e.InnerException;
		//        }
		//        else
		//        {
		//            throw new ApplicationException("implementation exception: " + e.InnerException.ToString() + e.StackTrace, e.InnerException);
		//        }
		//    }
		//}
	}
	//public delegate Position Partial(Map argument);
	[Serializable]
	public class Method : MethodImplementation
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return overloads.ContainsKey(key);
		}
		protected override ICollection<Map> KeysImplementation
		{
			get 
			{
				return overloads.Keys;
			}
		}
		protected override void Set(Map key, Map val)
		{
			overloads[key] = val;
		}
		private Dictionary<Map, Map> overloads = new Dictionary<Map, Map>();
		protected override Map Get(Map key)
		{
			Map value;
			overloads.TryGetValue(key,out value);
			return value;
		}
		public static Map MethodData(string name, object obj, Type type)
		{
			Map map = new StrategyMap();
			if (name == ".ctor" && type.Name=="Point")
			{
			}
			List<MethodBase> members = new List<MethodBase>((MethodBase[])new ArrayList(type.GetMember(name, GetBindingFlags(obj, name))).ToArray(typeof(MethodBase)));
			members.Sort(new Comparison<MethodBase>(delegate(MethodBase a, MethodBase b)
			{
				return a.GetParameters().Length.CompareTo(b.GetParameters().Length);
			}));
			Map result = new StrategyMap();
			foreach (MethodBase methodBase in members)
			{
				Map current = result;
				ParameterInfo[] parameters = methodBase.GetParameters();
				Map method = new Method(methodBase, obj, type);
				if (parameters.Length == 0)
				{
					result = method;
				}
				else
				{
					for (int i = 0; i < parameters.Length; i++)
					{
						Map typeMap = new TypeMap(parameters[i].ParameterType);
						if (i == parameters.Length - 1)
						{
							current[typeMap] = method;
						}
						else
						{
							if (!current.ContainsKey(typeMap))
							{
								current[typeMap] = new StrategyMap();
							}
							else
							{
							}
							current = current[typeMap];
						}
					}
				}
			}
			return result;
		}
		public Method(MethodBase method, object obj, Type type)
			: this(method, obj, type,new Dictionary<Map,Map>())
		{
		}
		public Method(MethodBase method, object obj, Type type,Dictionary<Map,Map> overloads)
			: base(method, obj, type)
		{
			this.overloads = new Dictionary<Map, Map>(overloads);
		}
		private static BindingFlags GetBindingFlags(object obj, string name)
		{
			if (name == ".ctor" || obj != null)
			{
				return BindingFlags.Public | BindingFlags.Instance;
			}
			else
			{
				return BindingFlags.Public | BindingFlags.Static;
			}
		}

		protected override Map CopyData()
		{
			return new Method(method, obj, type,overloads);
		}
	}
	[Serializable]
	public class TypeMap: DotNetMap
	{
		public override string ToString()
		{
			return Type.ToString();
		}
		protected override object GlobalKey
		{
			get
			{

				return this;
			}
		}
		public TypeMap(Type targetType)
			: base(null, targetType)
		{
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
		protected override Map Get(Map key)
		{
			if (Type.IsGenericTypeDefinition)
			{
				List<Type> types=new List<Type>();
				if (Type.GetGenericArguments().Length == 1)
				{
					types.Add(((TypeMap)key).Type);
				}
				else
				{
					foreach (Map map in key.Array)
					{
						types.Add(((TypeMap)map).Type);
					}
				}
				return new TypeMap(Type.MakeGenericType(types.ToArray()));
			}
			else if (Type == typeof(Array) && key is TypeMap)
			{
				return new TypeMap(((TypeMap)key).Type.MakeArrayType());
			}
			else if(base.Get(key)!=null)
			{
				return base.Get(key);
			}
			else
			{
				Map value;
				Data.TryGetValue(key, out value);
				return value;
			}
		}
		private Dictionary<Map, Map> data;
		private Dictionary<Map, Map> Data
		{
			get
			{
				if (data == null)
				{
					data = new Dictionary<Map, Map>();
					foreach (ConstructorInfo constructor in Type.GetConstructors())
					{
						string name = GetConstructorName(constructor);
						data[name] = new Method(constructor, this.Object, Type);
					}
				}
				return data;
			}
		}

		public static string GetConstructorName(ConstructorInfo constructor)
		{
			string name = constructor.DeclaringType.Name;
			foreach (ParameterInfo parameter in constructor.GetParameters())
			{
				name += "_" + parameter.ParameterType.Name;
			}
			return name;
		}

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return obj is TypeMap && ((TypeMap)obj).Type == this.Type;
		}
		protected override Map CopyData()
		{
			return new TypeMap(this.Type);
		}
		private Map constructor;
		private Map Constructor
		{
			get
			{
				if (constructor == null)
				{
					constructor = new Method(Type.GetConstructor(new Type[] { }),Object,Type);
				}
				return constructor;
			}
		}
		public override Map Call(Map argument)
		{
			Map item = Constructor.Call(Map.Empty);
			//Map item = Constructor.Call(Map.Empty, position).Get();
			Map result = Library.With(item, argument);
			return result;
			//return position.AddCall(result);
		}
	}
	[Serializable]
	public class ObjectMap: DotNetMap
	{
		protected override object GlobalKey
		{
			get 
			{
				return Object;
			}
		}
		public override Map Call(Map arg)
		{
			if (this.Type.IsSubclassOf(typeof(Delegate)))
			{
				return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg);
				//return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg, position);
			}
			else
			{
				throw new ApplicationException("Object is not callable.");
			}
		}
		public override bool Equals(object obj)
		{
			return obj is ObjectMap && ((ObjectMap)obj).Object.Equals(this.Object);
		}
		public override int GetHashCode()
		{
			return Object.GetHashCode();
		}
		public ObjectMap(string text)
			: this(text, text.GetType())
		{
		}
		public ObjectMap(Map target)
			: this(target, target.GetType())
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
			return Object.ToString();
		}
		protected override Map CopyData()
		{
			return new ObjectMap(Object);
		}
	}
	[Serializable]
	public class EmptyStrategy : MapStrategy
	{
		public override bool IsNumber
		{
			get 
			{
				return true;
			}
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public override void Remove(Map key,StrategyMap map)
		{
			throw new Exception("Key cannot be removed because it does not exist.");
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			return obj.Count == 0;
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
		public override void Set(Map key, Map val,StrategyMap map)
		{
			if (key.IsNumber)
			{
				if (key.Equals(Map.Empty) && val.IsNumber)
				{
					Panic(key, val, new NumberStrategy(0),map);
				}
				else
				{
					// that is not logical, maybe ListStrategy is not suitable for this, only if key==1
					Panic(key, val, new ListStrategy(),map);
				}
			}
			else
			{
				Panic(key, val,map);
			}
		}
		public override Map Get(Map key)
		{
			return null;
		}
	}
	[Serializable]
	public class NumberStrategy : MapStrategy
	{
		public override int GetArrayCount()
		{
			return 0;
		}
		public override void  Remove(Map key,StrategyMap map)
		{
			if(key.Equals(Map.Empty))
			{
				this.number=new Number(0);
			}
			else
			{
 				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			return obj.IsNumber && obj.GetNumber().Equals(number);
		}
		private Number number;
		public NumberStrategy(Number number)
		{
			this.number = number;
		}
		public override Map Get(Map key)
		{
			if (ContainsKey(key))
			{
				if (key.Equals(Map.Empty))
				{
					return number - 1;
				}
				else if(key.Equals(NumberKeys.Negative))
				{
					return Map.Empty;
				}
				else if (key.Equals(NumberKeys.Denominator))
				{
					return new StrategyMap(new Number(number.Denominator));
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
		public override void Set(Map key, Map value,StrategyMap map)
		{
			if (key.Equals(Map.Empty) && value.IsNumber)
			{
				this.number = value.GetNumber() + 1;
			}
			else if (key.Equals(NumberKeys.Negative) && value.Equals(Map.Empty) && number!=0)
			{
				if (number > 0)
				{
					number = 0 - number;
				}
			}
			else if (key.Equals(NumberKeys.Denominator) && value.IsNumber)
			{
				this.number = new Number(number.Numerator, value.GetNumber().GetInt32());
			}
			else
			{
				Panic(key, value,map);
			}
		}

		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				if (number != 0)
				{
					keys.Add(Map.Empty);
				}
				if (number < 0)
				{
					keys.Add(NumberKeys.Negative);
				}
				if (number.Denominator != 1.0d)
				{
					keys.Add(NumberKeys.Denominator);
				}
				return keys;
			}
		}
		public override Map CopyData()
		{
			return new StrategyMap(new NumberStrategy(number));
		}
		public override bool IsNumber
		{
			get
			{
				return true;
			}
		}
		public override Number GetNumber()
		{
			return number;
		}
		public override int GetHashCode()
		{
			return (int)(number.Numerator % int.MaxValue);
		}
	}
	[Serializable]
	public class StringStrategy : ArrayStrategy
	{
		protected override Map GetIndex(int i)
		{
			return text[i];
		}
		private string text;
		public StringStrategy(string text)
		{
			this.text = text;
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			if(obj is StringStrategy)
			{
				return ((StringStrategy)obj).text == text;
			}
			else
			{
				return base.EqualStrategy(obj);
			}
		}
		public override void Remove(Map key,StrategyMap map)
		{
			Panic(new ListStrategy(),map);
			map.Strategy.Remove(key,map);
		}
		public override void Set(Map key, Map val,StrategyMap map)
		{
			Panic(key, val,map);
		}
		public override int Count
		{
			get
			{
				return text.Length;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}
		public override bool IsString
		{
			get
			{
				return true;
			}
		}
		public override string GetString()
		{
			return text;
		}
		public override Map Get(Map key)
		{
			if (key.IsNumber)
			{
				Number number = key.GetNumber();
				if (number.IsNatural && number > 0 && number <= Count)
				{
					return text[number.GetInt32()-1];
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
		public override bool ContainsKey(Map key)
		{
			if (key.IsNumber)
			{
				Number number = key.GetNumber();
				if (number.IsNatural)
				{
					return number > 0 && number <= text.Length;
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
		public override int GetArrayCount()
		{
			return text.Length;
		}
		public override Map CopyData()
		{
			return new StrategyMap(new StringStrategy(text));
			//return new StrategyMap(new CloneStrategy(this));
		}
		public override ICollection<Map> Keys
		{
			get 
			{
				List<Map> keys = new List<Map>(text.Length);
				for (int i = 1; i <= text.Length; i++)
				{
					keys.Add(i);
				}
				return keys;
			}
		}
	}
	[Serializable]
	public abstract class ArrayStrategy : MapStrategy
	{
		protected abstract Map GetIndex(int i);
		public override bool EqualStrategy(MapStrategy obj)
		{
			if (obj is ArrayStrategy)
			{
				return EqualArrayStrategy((ArrayStrategy)obj);
			}
			//else if (obj is CloneStrategy)
			//{
			//    return ((CloneStrategy)obj).EqualStrategy(this);
			//}
			else
			{
				return EqualDefault(obj);
			}
		}
		private bool EqualArrayStrategy(ArrayStrategy strategy)
		{
			bool equal;
			if (Count == strategy.Count)
			{
				equal = true;
				for (int i = 0; i < strategy.Count; i++)
				{
					if (!GetIndex(i).Equals(strategy.GetIndex(i)))
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
	[Serializable]
	public class ListStrategy : ArrayStrategy
	{
		protected override Map GetIndex(int i)
		{
			return list[i];
		}
		public override void Append(Map map,StrategyMap parent)
		{
			list.Add(map);
		}
		public override void Remove(Map key,StrategyMap map)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		private List<Map> list;

		public ListStrategy():this(5)
		{
		}
		public ListStrategy(int capacity)
		{
			this.list = new List<Map>(capacity);
		}
		public ListStrategy(ListStrategy original)
		{
			this.list = new List<Map>(original.list);
		}
		public override Map Get(Map key)
		{
			Map value = null;
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count)
				{
					value = list[integer - 1];
				}
			}
			return value;
		}
		public override void Set(Map key, Map val,StrategyMap map)
		{
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count)
				{
					list[integer - 1] = val;
				}
				else if (integer == list.Count + 1)
				{
					list.Add(val);
				}
				else
				{
					Panic(key, val,map);
				}
			}
			else
			{
				Panic(key, val,map);
			}
		}

		public override int Count
		{
			get
			{
				return list.Count;
			}
		}
		public override List<Map> Array
		{
			get
			{
				return this.list;
			}
		}

		public override int GetArrayCount()
		{
			return this.list.Count;
		}
		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsNumber)
			{
				Number integer = key.GetNumber();
				if (integer >= 1 && integer <= list.Count)
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
				foreach (Map value in list)
				{
					keys.Add(new StrategyMap(counter));
					counter++;
				}
				return keys;
			}
		}
		//public override Map CopyData()
		//{
		//    //return new StrategyMap(new CloneStrategy(this));
		//}
	}
	[Serializable]
	public class DictionaryStrategy:MapStrategy
	{
		public override bool IsNumber
		{
			get
			{
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);
			}
		}

		public override int GetArrayCount()
		{
			int i = 1;
			for (; this.ContainsKey(i); i++)
			{
			}
			return i - 1;
		}
		public override void Remove(Map key,StrategyMap map)
		{
			dictionary.Remove(key);
		}
		private Dictionary<Map, Map> dictionary;
		public DictionaryStrategy():this(2)
		{
		}
		public DictionaryStrategy(int Count)
		{
			this.dictionary = new Dictionary<Map, Map>(Count);
		}
		public DictionaryStrategy(Dictionary<Map, Map> data)
		{
			this.dictionary = data;
		}
		public override Map Get(Map key)
		{
			Map val;
			dictionary.TryGetValue(key, out val);
			return val;
		}
		public override void Set(Map key, Map value,StrategyMap map)
		{
			dictionary[key] = value;
		}
		public override bool ContainsKey(Map key)
		{
			return dictionary.ContainsKey(key);
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
		//public override Map CopyData()
		//{
		//    return new StrategyMap(new CloneStrategy(this));
		//}
	}
	//[Serializable]
	//public class CloneStrategy : MapStrategy
	//{
	//    public override int GetArrayCount()
	//    {
	//        return original.GetArrayCount();
	//    }
	//    public override void Remove(Map key,StrategyMap map)
	//    {
	//        Panic(new DictionaryStrategy(),map);
	//        map.Remove(key);
	//    }
	//    private MapStrategy original;
	//    public CloneStrategy(MapStrategy original)
	//    {
	//        this.original = original;
	//    }
	//    public override List<Map> Array
	//    {
	//        get
	//        {
	//            return original.Array;
	//        }
	//    }
	//    public override bool ContainsKey(Map key)
	//    {
	//        return original.ContainsKey(key);
	//    }
	//    public override int Count
	//    {
	//        get
	//        {
	//            return original.Count;
	//        }
	//    }
	//    public override Map CopyData()
	//    {
	//        MapStrategy clone = new CloneStrategy(this.original);
	//        return new StrategyMap(clone);
	//    }
	//    public override bool EqualStrategy(MapStrategy obj)
	//    {
	//        return obj.EqualStrategy(original);
	//    }
	//    public override int GetHashCode()
	//    {
	//        return original.GetHashCode();
	//    }
	//    public override Number GetNumber()
	//    {
	//        return original.GetNumber();
	//    }
	//    public override string GetString()
	//    {
	//        return original.GetString();
	//    }
	//    public override bool IsNumber
	//    {
	//        get
	//        {
	//            return original.IsNumber;
	//        }
	//    }
	//    public override bool IsString
	//    {
	//        get
	//        {
	//            return original.IsString;
	//        }
	//    }
	//    public override ICollection<Map> Keys
	//    {
	//        get
	//        {
	//            return original.Keys;
	//        }
	//    }
	//    public override Map Get(Map key)
	//    {
	//        return original.Get(key);
	//    }
	//    public override void Set(Map key, Map value,StrategyMap map)
	//    {
	//        Panic(key, value,map);
	//    }
	//}

	[Serializable]
	public abstract class MapStrategy
	{
		public virtual void Append(Map map,StrategyMap parent)
		{
		    this.Set(GetArrayCount() + 1,map,parent);
		}
		public abstract void Remove(Map key,StrategyMap map);
		public abstract void Set(Map key, Map val,StrategyMap map);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}

		public abstract int GetArrayCount();
		public virtual Map CopyData()
		{
			Map map = new StrategyMap();
			foreach (Map key in Keys)
			{
				map[key] = this.Get(key).Copy();
			}
			return map;
		}

		//public abstract Map CopyData();

		// refactor
		public void Panic(MapStrategy newStrategy,StrategyMap map)
		{
			map.Strategy = newStrategy;
			map.InitFromStrategy(this);
		}

		protected void Panic(Map key, Map val,StrategyMap map)
		{
			Panic(key, val, new DictionaryStrategy(),map);
		}
		protected void Panic(Map key, Map val, MapStrategy newStrategy,StrategyMap map)
		{
			Panic(newStrategy,map);
			map.Strategy.Set(key, val,map); // why do it like this? this wont assign the parent, which is problematic!!!
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
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);
			}
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
				return GetArrayCount() == Count && this.Array.TrueForAll(delegate(Map map)
				{
					return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);
				});
			}
		}
		public string GetStringDefault()
		{
			StringBuilder text = new StringBuilder("");
			foreach (Map key in Keys)
			{
				text.Append(Convert.ToChar(this.Get(key).GetNumber().GetInt32()));
			}
			return text.ToString();
		}
		public virtual string GetString()
		{
			return GetStringDefault();
		}
		public virtual Number GetNumber()
		{
			return GetNumberDefault();
		}
		public Number GetNumberDefault()
		{
			Number number;
			if (Count==0)
			{
				number = 0;
			}
			else if (this.Count == 1 && this.ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber)
			{
				number = 1 + this.Get(Map.Empty).GetNumber();
			}
			else
			{
				throw new ApplicationException("Map is not an integer");
			}
			return number;
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
		public virtual bool EqualStrategy(MapStrategy obj)
		{
			return EqualDefault((MapStrategy)obj);
		}
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
	[Serializable]
	public class IndexedProperty : Map
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
		private object obj;
		private Type type;
		private PropertyInfo property;
		private ParameterInfo[] parameters;
		public IndexedProperty(PropertyInfo property, object obj, Type type)
		{
			this.property = property;
			this.obj = obj;
			this.type = type;
			this.parameters = property.GetIndexParameters();
			if (parameters.Length != 1)
			{
				throw new Exception("invalid numbers of indexer parameters.");
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
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				throw new Exception("not implemented");
			}
		}
		protected override Map Get(Map key)
		{
			return Transform.ToMeta(property.GetValue(obj,new object[] {Transform.ToDotNet(key,parameters[0].ParameterType)}));
		}
		protected override void Set(Map key, Map val)
		{
			property.SetValue(obj, Transform.ToDotNet(val, property.PropertyType), new object[] { Transform.ToDotNet(key, parameters[0].ParameterType) });
		}
		protected override Map CopyData()
		{
			return new IndexedProperty(property, obj, type);
		}
	}

	public class MethodCache
	{
		private static Dictionary<KeyValuePair<Type, BindingFlags>, Dictionary<Map, MethodInfo>> cache = new Dictionary<KeyValuePair<Type, BindingFlags>, Dictionary<Map, MethodInfo>>();
		public static Dictionary<Map, MethodInfo> GetMethodData(Type type, BindingFlags bindingFlags)
	    {
	        KeyValuePair<Type,BindingFlags> key=new KeyValuePair<Type,BindingFlags>(type,bindingFlags);
			if (!cache.ContainsKey(key))
			{
				Dictionary<Map, MethodInfo>data = new Dictionary<Map, MethodInfo>();
				foreach (MethodInfo method in type.GetMethods(bindingFlags))
				{
					string name = TypeMap.GetMethodName(method);
					data[name] = method;
				}
				cache[key] = data;
			}
			return cache[key];
	    }
	}

	[Serializable]
	public abstract class DotNetMap : Map
	{
		public object Object
		{
			get
			{
				return obj;
			}
		}
		public Type Type
		{
			get
			{
				return type;
			}
		}
		protected abstract object GlobalKey
		{
			get;
		}
		public static string GetMethodName(MethodInfo method)
		{
			string name = method.Name;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				name += "_" + parameter.ParameterType.Name;
			}
			return name;
		}
		private Dictionary<Map, MethodInfo> data;
		private Dictionary<Map, MethodInfo> Data
		{
			get
			{
				if (data == null)
				{
					data = MethodCache.GetMethodData(type, bindingFlags);
				}
				return data;
			}
		}
		private object obj;
		private Type type;
		protected BindingFlags bindingFlags;

		public DotNetMap(object obj, Type type)
		{
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
			if (obj!=null && key.Equals(new StrategyMap("this")))
			{
				return this;
			}
			else if (Data.ContainsKey(key))
			{
				return new Method(Data[key],obj,type);
			}
			else if (global.ContainsKey(GlobalKey) && global[GlobalKey].ContainsKey(key))
			{
				return global[GlobalKey][key];
			}
			else if (key.IsString)
			{
				string memberName = key.GetString();
				MemberInfo[] foundMembers = type.GetMember(memberName, bindingFlags);
				if (foundMembers.Length != 0)
				{
					MemberInfo member = foundMembers[0];
					Map result;
					if (member is PropertyInfo)
					{
						PropertyInfo property = (PropertyInfo)member;
						ParameterInfo[] parameters = property.GetIndexParameters();
						if (parameters.Length != 0)
						{
							result = new IndexedProperty(property, obj, type);
						}
						else
						{
							result = Transform.ToMeta(((PropertyInfo)member).GetValue(obj, null));
						}
					}
					else if (member is FieldInfo)
					{
						result = Transform.ToMeta(type.GetField(memberName).GetValue(obj));
					}
					else if (member is Type)
					{
						result = new TypeMap((Type)member);
					}
					else
					{
						result = null;
					}
					return result;
				}
			}
			if (obj != null && obj is IList && key.IsNumber && key.GetNumber().Numerator<((IList)obj).Count)
			{
				return Transform.ToMeta(((IList)obj)[Convert.ToInt32(key.GetNumber().Numerator)]);
			}
			return null;
		}
		protected override void Set(Map key, Map value)
		{
			//if (obj is DependencyObject && key is ObjectMap && ((ObjectMap)key).obj is DependencyProperty)
			//{
			//    DependencyObject o = (DependencyObject)obj;
			//    DependencyProperty property=(DependencyProperty)((ObjectMap)key).obj;
			//    o.SetValue(property,Transform.ToDotNet(value,property.PropertyType));
			//}
			//else
			//{

				string fieldName = key.GetString();
				MemberInfo[] members = type.GetMember(fieldName, bindingFlags);
				if (members.Length != 0)
				{
					MemberInfo member = members[0];
					if (member is FieldInfo)
					{
						FieldInfo field = (FieldInfo)member;
						field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
					}
					else if (member is PropertyInfo)
					{
						PropertyInfo property = (PropertyInfo)member;
						if (property.PropertyType.Name.Contains("UIElementCollection"))
						{
						}
						if (typeof(IList).IsAssignableFrom(property.PropertyType) && !(value is ObjectMap))
						{
							if(value.ArrayCount!=0)
							{
								IList list = (IList)property.GetValue(obj, null);
								list.Clear();
								Type t=GetListAddFunctionType(list, value);
								if (t == null)
								{
									throw new ApplicationException("Cannot convert argument.");
								}
								else
								{
									foreach (Map map in value.Array)
									{
										list.Add(Transform.ToDotNet(map, t));
									}
								}
							}
						}
						else
						{
							object converted = Transform.ToDotNet(value, property.PropertyType);
							property.SetValue(obj, converted, null);
						}

					}
					else if (member is EventInfo)
					{
						EventInfo eventInfo = (EventInfo)member;
						new Method(eventInfo.GetAddMethod(), obj, type).Call(value);
						//new Method(eventInfo.GetAddMethod(), obj, type).Call(value, MethodImplementation.currentPosition);
					}
					else
					{
						throw new Exception("unknown member type");
					}
				}
				else
				{

					if (!global.ContainsKey(GlobalKey))
					{
						global[GlobalKey] = new Dictionary<Map, Map>();
					}
					global[GlobalKey][key] = value;
				}
			//}
		}
		private static Type GetListAddFunctionType(IList list, Map value)
		{
			foreach (MemberInfo member in list.GetType().GetMember("Add"))
			{
				if (member is MethodInfo)
				{
					MethodInfo method=(MethodInfo)member;
					ParameterInfo[] parameters=method.GetParameters();
					if (parameters.Length == 1)
					{
						ParameterInfo parameter = parameters[0];
						bool c=true;
						foreach (Map entry in value.Array)
						{
							Transform.TryToDotNet(entry,parameter.ParameterType,out c);
							if (!c)
							{
								break;
							}
						}
						if (c)
						{
							return parameter.ParameterType;
						}
					}
				}
			}
			return null;
		}
		public static Dictionary<object,Dictionary<Map,Map>> global=new Dictionary<object,Dictionary<Map,Map>>();
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				List<Map> keys = new List<Map>();
				foreach (MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					string name;
					if (member is MethodInfo)
					{
						name=GetMethodName((MethodInfo)member);
					}
					else if (member is ConstructorInfo)
					{
						continue;
					}
					else
					{
						name = member.Name;
					}
					keys.Add(name);
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
			if (obj != null)
			{
				return this.obj.ToString();
			}
			else
			{
				return this.type.ToString();
			}
		}
		public Delegate CreateEventDelegate(string name, Map code)
		{
			EventInfo eventInfo = type.GetEvent(name, BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static | BindingFlags.Instance);
			Delegate eventDelegate = Transform.CreateDelegateFromCode(eventInfo.EventHandlerType, code);
			return eventDelegate;
		}
	}
	public interface ISerializeEnumerableSpecial
	{
		string Serialize();
	}
	[Serializable]
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
		public static string TestDirectory
		{
			get
			{
				return Path.Combine(Interpreter.InstallationPath, "Test");
			}
		}
		public abstract class Test
		{
			public bool RunTest()
			{
				int level;
				Console.Write(this.GetType().Name + "...");


				DateTime startTime = DateTime.Now;
				object result = GetResult(out level);
				TimeSpan duration = DateTime.Now - startTime;


				string testDirectory = Path.Combine(TestDirectory, this.GetType().Name);

				string resultPath = Path.Combine(testDirectory, "result.txt");
				string checkPath = Path.Combine(testDirectory, "check.txt");

				Directory.CreateDirectory(testDirectory);
				if (!File.Exists(checkPath))
				{
					File.Create(checkPath).Close();
				}

				StringBuilder stringBuilder = new StringBuilder();
				Serialize(result, "", stringBuilder, level);

				File.WriteAllText(resultPath, stringBuilder.ToString(), Encoding.UTF8);
				string successText;
				bool success = !File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath));
				if (success)
				{
					successText = "failed";
				}
				else
				{
					successText = "succeeded";
				}
				Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
				return success;
			}
			public abstract object GetResult(out int level);
		}
		public void Run()
		{
			bool allTestsSucessful = true;
			foreach (Type testType in this.GetType().GetNestedTypes())
			{
				if (testType.IsSubclassOf(typeof(Test)))
				{
					Test test = (Test)testType.GetConstructor(new Type[] { }).Invoke(null);
					int level;
					if (!test.RunTest())
					{
						allTestsSucessful = false;
					}
				}
			}
			if (!allTestsSucessful)
			{
				Console.ReadLine();
			}
		}
		public const char indentationChar = '\t';

		private static bool UseToStringMethod(Type type)
		{
			return (!type.IsValueType || type.IsPrimitive)
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();
		}
		private static bool UseProperty(PropertyInfo property,int level)
		{
			object[] attributes=property.GetCustomAttributes(typeof(SerializeAttribute), false);
			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public static void Serialize(object obj,string indent,StringBuilder builder,int level) 
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
	[Serializable]
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
	[Serializable]
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
		public const char lastArgument = '@';
		public const char autokey = '.';
		public const char callStart = '(';
		public const char callEnd = ')';
		public const char root = '/';
		public const char negative='-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char indentation = '\t';
		public const char unixNewLine = '\n';
		public const string windowsNewLine = "\r\n";
		public const char function = '|';
		public const char @string = '\"';
		public const char lookupStart = '[';
		public const char lookupEnd = ']';
		public const char emptyMap = '0';
		public const char explicitCall = '-';
		public const char select = '.';
		public const char character = '\'';
		public const char assignment = ' ';
		public const char space = ' ';
		public const char tab = '\t';
		public const char current = '&';
		public static char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char[] lookupStringForbidden = new char[] { current, lastArgument, explicitCall, indentation, '\r', '\n', assignment, select, function, @string, lookupStart, lookupEnd, emptyMap, '!', root, callStart, callEnd, character, ',', '*', '$', '\\', '<', '=', '+', '-',':'};
		public static char[] lookupStringForbiddenFirst = new char[] { current, lastArgument, explicitCall, indentation, '\r', '\n', assignment, select, function, @string, lookupStart, lookupEnd, emptyMap, '!', root, callStart, callEnd, character, ',', '*', '$', '\\', '<', '=', '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	}
	public class Gac : Map
	{
		public static readonly Map gac = new Gac();
		private Gac()
		{
			this["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
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
					foreach (ConstructorInfo constructor in type.GetConstructors())
					{
						if (constructor.GetParameters().Length != 0)
						{
							selected[TypeMap.GetConstructorName(constructor)] = new Method(constructor,null,type);
						}

					}
				}
			}
			return val;
		}
		protected override Map Get(Map key)
		{
			Map value;
			if (!cache.ContainsKey(key))
			{
				if (key.IsString)
				{
					try
					{
						value = LoadAssembly(Assembly.LoadWithPartialName(key.GetString()));
						this[key] = value;
					}
					catch(Exception e)
					{
						value = null;
					}
				}
				else
				{
					if (key.ContainsKey("version") && key.ContainsKey("publicKeyToken") &&
						key.ContainsKey("culture") &&
						key.ContainsKey("name"))
					{
						Map version = key["version"];
						Map publicKeyToken = key["publicKeyToken"];
						Map culture = key["culture"];
						Map name = key["name"];
						if (version != null && version.IsString && publicKeyToken != null && publicKeyToken.IsString && culture != null && culture.IsString && name != null && name.IsString)
						{
							Assembly assembly = Assembly.Load(name.GetString() + ",Version=" + version.GetString() + ",Culture=" + culture.GetString() + ",Name=" + name.GetString());
							value = LoadAssembly(assembly);
							this[key] = value;
						}
						else
						{
							value = null;
						}
					}
					else
					{
						value = null;
					}
				}
			}
			else
			{
				value = cache[key];
			}
			return value;
		}
		protected override void Set(Map key, Map val)
		{
			cache[key] = val;
		}
		protected override Map CopyData()
		{
			return this;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				throw new ApplicationException("not implemented.");
			}
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
	}
	[Serializable]
	public class Number
	{
		public bool IsNatural
		{
			get
			{
				return denominator == 1.0d;
			}
		}
		private readonly double numerator;
		private readonly double denominator;
		public static Number Parse(string text)
		{
			try
			{
				string[] parts = text.Split('/');
				int numerator = Convert.ToInt32(parts[0]);
				int denominator;
				if (parts.Length > 2)
				{
					denominator = Convert.ToInt32(parts[2]);
				}
				else
				{
					denominator = 1;
				}
				return new Number(numerator, denominator);
			}
			catch (Exception e)
			{
				return null;
			}
		}
		public Number(double integer)
			: this(integer, 1)
		{
		}
		public Number(Number i)
			: this(i.numerator, i.denominator)
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
			this.numerator = numerator / greatestCommonDivisor;
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
			return new Number((double)integer);
		}
		public static bool operator ==(Number a, Number b)
		{
			return !ReferenceEquals(b, null) && a.numerator == b.numerator && a.denominator == b.denominator;
		}
		public static bool operator !=(Number a, Number b)
		{
			return !(a == b);
		}
		private static double GreatestCommonDivisor(double a, double b)
		{
			a = Math.Abs(a);
			b = Math.Abs(b);
			while (a != 0 && b != 0)
			{
				if (a > b)
					a = a % b;
				else
					b = b % a;
			}
			if (a == 0)
				return b;
			else
				return a;
		}
		private static double LeastCommonMultiple(Number a, Number b)
		{
			return a.denominator * b.denominator / GreatestCommonDivisor(a.denominator, b.denominator);
		}
		public static Number operator %(Number a, Number b)
		{
			return Convert.ToInt32(a.Numerator)%Convert.ToInt32(b.Numerator);
		}
		public static Number operator +(Number a, Number b)
		{
			return new Number(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a, b));
		}
		public static Number operator /(Number a, Number b)
		{
			return new Number(a.numerator * b.denominator, a.denominator * b.numerator);
		}
		public static Number operator -(Number a, Number b)
		{
			return new Number(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a, b));
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
			return b.numerator == numerator && b.denominator == denominator;
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
			return Convert.ToInt32(numerator);
		}
		public long GetInt64()
		{
			return Convert.ToInt64(numerator);
		}
	}
	public class RealParser
	{
		public RealParser(string text, int index)
		{
			this.text = text;
			this.index = index;
		}
		public string text;
		public int index;

		private bool MatchAny(string chars, out char c)
		{
			c = text[index];
			if (chars.Contains(c.ToString()))
			{
				index++;
				return true;
			}
			else
			{
				return false;
			}
		}
		private bool MatchExcept(string chars, out char c)
		{
			c = Look();
			if (!chars.Contains(c.ToString()))
			{
				index++;
				return false;
			}
			else
			{
				return true;
			}

		}
		private char Look()
		{
			return text[index];
		}
		private bool Match(char c)
		{
			return Match(c.ToString());
		}
		private bool Match(string s)
		{
			return text.Substring(index, s.Length) == s;
		}
		public bool NewLine()
		{
			return Match(Syntax.unixNewLine.ToString()) || Match(Syntax.windowsNewLine);
		}
		public bool EndOfLine()
		{
			Whitespace();
			return NewLine();
		}
		private void Whitespace()
		{
			while (Match('\t') || Match(' ')) ;
		}
		public Map Integer()
		{
			string s = "";
			if (Match('-'))
			{
				s += "-";
			}
			char c;
			while (MatchAny("0123456789", out c))
			{
				s += c;
			}
			return Convert.ToInt32(s);
		}
		public bool Indentation()
		{
			if (EndOfLine())
			{
				if (Match("".PadLeft(indentationCount + 1, '\t')))
				{
					indentationCount++;
					return true;
				}
			}
			return false;
		}
		private int indentationCount = -1;
		public bool SameIndentation()
		{
			return Match("".PadLeft(indentationCount, '\t'));
		}
		public bool StringDedentation()
		{
			EndOfLine();
			if (Match("".PadLeft(indentationCount - 1, '\t')))
			{
				indentationCount--;
				return true;
			}
			else
			{
				return false;
			}
		}
		public void Dedentation()
		{
			indentationCount--;
		}
		private Map ShortString()
		{
			if (Match('"'))
			{
				string text = "";
				char c;
				while (MatchExcept("\n\r\"", out c))
				{
					text += c;
				}
				if (Look() == '"')
				{
					return text;
				}
			}
			return null;
		}
		public delegate Map Parse();
		public Map Try(params Parse[] parsers)
		{
			foreach (Parse parse in parsers)
			{
				Map value = parse();
				if (value != null)
				{
					return value;
				}
			}
			return null;
		}
		public Map String()
		{
			return Try(ShortString, LongString);
		}
		public Map LongString()
		{
			if (Match('"'))
			{
				if (Indentation())
				{
					string text = "";
					while (true)
					{
						char c;
						while (MatchExcept("\n\r", out c))
						{
							text += c;
						}
						if (EndOfLine() && SameIndentation())
						{
							text += '\n';
						}
						else
						{
							StringDedentation();
							if (Match('"'))
							{
								return text;
							}
						}
					}
				}
			}
			return null;
		}
		private bool EndOfFile
		{
			get
			{
				return index >= text.Length;
			}
		}
		public Map Map()
		{
			if (Indentation())
			{
				Map result = new StrategyMap();
				for (int count = 1; ; count++)
				{
					Map entry = Entry();
					if (entry != null)
					{
						result[count] = entry;
					}
					if (EndOfFile)
					{
						break;
					}
					if (!SameIndentation())
					{
						Dedentation();
						break;
					}
				}
				return result;
			}
			return null;
		}
		public Map Value()
		{
			return Try(Map, String, Integer);
		}
		public Map Entry()
		{
			Map key = Value();
			if (key != null && Match('='))
			{
				Map value = Value();
				if (value != null)
				{
					Map result = new StrategyMap();
					result[key] = value;
					return result;
				}
			}
			return null;
		}
		private Map EmptyMap()
		{
			return Match("*") ? new StrategyMap() : null;
		}
	}
	public class Binary
	{
		public static void Serialize(Map map,string path)
		{
			using (FileStream stream = File.Create(path))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(stream,map);
			}
		}
		public static Map Deserialize(string path)
		{
			using (FileStream stream = File.Open(path, FileMode.Open))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				object obj=formatter.Deserialize(stream);
				return (StrategyMap)obj;
			}
		}
	}
	public class Parser
	{
		public static Map Parse(string file)
		{
			return ParseString(System.IO.File.ReadAllText(file), file);
		}
		public static Map ParseString(string text, string fileName)
		{
			Parser parser = new Parser(text, fileName);
			bool matched;
			Map result = Parser.File.Match(parser, out matched);
			if (parser.index != parser.text.Length)
			{
				throw new SyntaxException("Expected end of file.", parser);
			}
			return result;
		}
		public bool End
		{
			get
			{
				return index >= text.Length - 1;
			}
		}
		private bool negative = false;
		public string text;
		public int index;
		private string file;
		private int line = 1;
		private int column = 1;
		private bool isStartOfFile = true;
		private int indentationCount = -1;
		public Stack<int> defaultKeys = new Stack<int>();

		public Parser(string text, string filePath)
		{
			this.index = 0;
			this.text = text;
			this.file = filePath;
		}
		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(LiteralExpression, Call, Program, List, Search, Select, ExplicitCall);
		});

		public static Rule NewLine =
			new Alternatives(
				new Character(Syntax.unixNewLine),
				StringRule(Syntax.windowsNewLine));

		public static Rule EndOfLine =
			new Sequence(
				new Action(new Match(), new ZeroOrMore(
					new Action(new Match(), new Alternatives(
						new Character(Syntax.space),
						new Character(Syntax.tab))))),
				new Action(new Match(), NewLine));

		public static Rule Integer =
			new Sequence(
				new Action(new CustomProduction(
					delegate(Parser p, Map map, ref Map result)
					{
						p.negative = map != null;
						return null;
					}),
					new Optional(new Character(Syntax.negative))),
				new Action(
					new ReferenceAssignment(),
					new Sequence(
						new Action(
							new ReferenceAssignment(),
							new OneOrMore(
								new Action(
									new CustomProduction(
									delegate(Parser p, Map map, ref Map result)
									{
										if (result == null)
										{
											result = p.CreateMap();
										}
										result = result.GetNumber() * 10 + (Number)map.GetNumber().GetInt32() - '0';
										return result;
									}),
									new Character(Syntax.integer)))),
						new Action(
							new CustomProduction(delegate(Parser p, Map map, ref Map result)
							{
								if (result.GetNumber() > 0 && p.negative)
								{
									result = 0 - result.GetNumber();
								}
								return null;
							}),
							new CustomRule(delegate(Parser p, out bool matched) { 
								matched = true; return null; })
							)
			)));
		public static Rule StartOfFile =
			
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
	});

		private static Rule EndOfLinePreserve =
			new Sequence(
				new Action(new Match(),
					new ZeroOrMore(
							new Action(new Autokey(), new Alternatives(
								new Character(Syntax.space),
								new Character(Syntax.tab))))),
				new Action(new Append(),
					new Alternatives(
						new Character(Syntax.unixNewLine),
						StringRule(Syntax.windowsNewLine))));

		private static Rule SmallIndentation = new Sequence(

			new Action(new Match(), new CustomRule(delegate(Parser p, out bool matched)
			{
				p.indentationCount++;
				matched = true;
				return null;
			})));


		public static Rule FullIndentation = new Alternatives(
				StartOfFile,
				new Sequence(
				new Action(new Match(), EndOfLine),
				new Action(new Match(), SmallIndentation)
				));

		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			return StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa, out matched);
		});
		private static Rule StringLine = new ZeroOrMore(new Action(new Autokey(), new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0])));
		public static Rule StringDedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			Map map = new Sequence(
				new Action(
					new Match(),
					new Optional(EndOfLine)),
				new Action(new Match(), StringRule("".PadLeft(pa.indentationCount - 1, Syntax.indentation)))).Match(pa, out matched);
			if (matched)
			{
				pa.indentationCount--;
			}
			return map;
		});
		public static Rule CharacterDataExpression = new Sequence(
			new Action(
				new Match(),
				new Character(Syntax.character)),
			new Action(
				new ReferenceAssignment(),
				new CharacterExcept(Syntax.character)),
			new Action(
				new Match(),
				new Character(Syntax.character)));

		public static Rule Dedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			pa.indentationCount--;
			matched = true;
			return null;
		});
		private static void MatchStringLine(Parser parser, StringBuilder text)
		{
			bool matching = true;
			for (; matching && parser.index < parser.text.Length; parser.index++)
			{
				char c = parser.Look();
				switch (c)
				{
					case '\n':
					case '\r':
						matching = false;
						break;
					default:
						text.Append(c);
						break;
				}
			}
		}
		private static Rule StringBeef = new CustomRule(delegate(Parser parser, out bool matched)
		{
			StringBuilder result = new StringBuilder(100);
			MatchStringLine(parser, result);
			matched = true;
			while (true)
			{
				bool lineMatched;
				new Sequence(new Action(new Match(), EndOfLine),
					new Action(new Match(), SameIndentation)).Match(parser, out lineMatched);
				if (lineMatched)
				{
					result.Append('\n');
					MatchStringLine(parser, result);
				}
				else
				{
					break;
				}
			}
			return result.ToString();
		});
		private static Rule SingleString = new OneOrMore(
			new Action(
				new Autokey(),
				new CharacterExcept(
					Syntax.unixNewLine,
					Syntax.windowsNewLine[0],
					Syntax.@string)));

		public static Rule String = new Sequence(
			new Action(new Match(), new Character(Syntax.@string)),
			new Action(new ReferenceAssignment(), new Alternatives(
				SingleString,
				new Sequence(
					new Action(new Match(), FullIndentation),
					new Action(new Match(), SameIndentation),
					new Action(new ReferenceAssignment(), StringBeef),
					new Action(new Match(), StringDedentation)))),
			new Action(new Match(), new Character(Syntax.@string)));

		public static Rule Number = new Sequence(
			new Action(new ReferenceAssignment(),
				Integer),
			new Action(
				new Assignment(
					NumberKeys.Denominator),
					new Optional(
						new Sequence(
							new Action(
								new Match(),
								new Character(Syntax.fraction)),
							new Action(
								new ReferenceAssignment(),
								Integer)))));

		public static Rule LookupString = new Sequence(
			new Action(
				new Assignment(1),
				new CharacterExcept(Syntax.lookupStringForbiddenFirst)),
			new Action(new Append(), new ZeroOrMore(
				new Action(
					new Autokey(),
						new CharacterExcept(
						Syntax.lookupStringForbidden)))));


		
		public static Rule ExpressionData = new DelayedRule(delegate()
		{
			return Parser.Expression;
		});

		public static Rule Value = new DelayedRule(delegate
		{
			return new Alternatives(
			Map,
			ListMap,
			String,
			Number,
			CharacterDataExpression

			);
		});
		private static Rule LookupAnything =
			new Sequence(
				new Action(new Match(), new Character(('<'))),
				new Action(new ReferenceAssignment(), Value));

		public static Rule Function = new Sequence(
			new Action(new Assignment(
				CodeKeys.Parameter),
				new ZeroOrMore(
				new Action(new Autokey(),
					new CharacterExcept(
						Syntax.@string,
						Syntax.function,
						Syntax.windowsNewLine[0],
						Syntax.unixNewLine)))),
			new Action(
				new Match(),
					new Character(
						Syntax.function)),
				new Action(new Assignment(CodeKeys.Expression),
				Expression),
			new Action(new Match(), new Optional(EndOfLine)));

		public static Rule Entry = new Alternatives(
			new Sequence(
				new Action(
					new Assignment(CodeKeys.Function),
					new Sequence(new Action(new ReferenceAssignment(), Function)))),
			new Sequence(
				new Action(new Assignment(1), new Alternatives(
					Number,
					LookupString,
					LookupAnything)),
				new Action(new Match(), new Character('=')),
				new Action(new CustomProduction(
					delegate(Parser parser, Map map, ref Map result)
					{
						result = new StrategyMap(result[1], map);
						return result;
					})

					, Value),
			 new Action(new Match(), new Optional(EndOfLine))));

		public static Rule Map = new Sequence(
			new Action(new Match(), new Optional(new Character(','))),
			new Action(new Match(),
				FullIndentation),
				new Action(new ReferenceAssignment(), new PrePost(
				delegate(Parser p)
				{
					p.defaultKeys.Push(1);
				},
				new Sequence(
					new Action(new ReferenceAssignment(),
						new OneOrMore(new Action(
						new Merge(),
						new Sequence(
							new Action(
								new Match(), SameIndentation),
							new Action(
								new ReferenceAssignment(),
								Entry))))),
					new Action(new Match(), Dedentation)),
				delegate(Parser p)
				{
					p.defaultKeys.Pop();
				})));


		public static Rule File = new Sequence(
			new Action(new Match(),
				new Optional(
					new Sequence(
						new Action(new Match(),
							StringRule("#!")),
						new Action(new Match(),
							new ZeroOrMore(
								new Action(new Match(),
									new CharacterExcept(Syntax.unixNewLine)))),
						new Action(new Match(), EndOfLine)))),
			new Action(new ReferenceAssignment(), Map));

		public static Rule ExplicitCall = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Match(), new Character(Syntax.callStart)),
				new Action(new ReferenceAssignment(), Call),
				new Action(new Match(), new Character(Syntax.callEnd)));
		});
		public static Rule Call = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Match(), new Character(Syntax.explicitCall)),
				new Action(new Assignment(
					CodeKeys.Call),
					new Sequence(
						new Action(new Match(), FullIndentation),
						new Action(
							new ReferenceAssignment(),
							new OneOrMore(
								new Action(
									new Autokey(),
									new Sequence(
										new Action(new Match(), new Optional(EndOfLine)),
										new Action(new Match(), SameIndentation),
										new Action(new ReferenceAssignment(), Expression))))),
							new Action(new Match(), new Optional(EndOfLine)),
							new Action(new Match(), new Optional(Dedentation)))));
		});

		public static Rule FunctionExpression = new Sequence(
			new Action(new Assignment(CodeKeys.Key), new LiteralRule(new StrategyMap(CodeKeys.Literal, CodeKeys.Function))),
			new Action(new Assignment(CodeKeys.Value), new Sequence(
				new Action(new Assignment(CodeKeys.Literal), Function))));


		private static Rule Whitespace =
			new ZeroOrMore(
				new Action(new Match(),
					new Alternatives(
						new Character(Syntax.tab),
						new Character(Syntax.space))));

		private static Rule EmptyMap =
			new Sequence(
				new Action(new Match(),
					new Character(Syntax.emptyMap)),
				new Action(new ReferenceAssignment(),
					new LiteralRule(Meta.Map.Empty)));

		private static Rule LiteralExpression = new Sequence(
			new Action(
				new Assignment(
					CodeKeys.Literal),
				new Alternatives(
					Number,
					EmptyMap,
					String,
					CharacterDataExpression)));

		private static Rule LookupAnythingExpression =
			new Sequence(
				new Action(new Match(), new Character('<')),
				new Action(new ReferenceAssignment(), Expression)
			);

		private static Rule LookupStringExpression =
			new Sequence(
				new Action(new Assignment(
					CodeKeys.Literal),
					LookupString));

		private static Rule Current = new Sequence(
			new Action(new Match(), new Character(Syntax.current)),
			new Action(new ReferenceAssignment(), new LiteralRule(new StrategyMap(CodeKeys.Current, Meta.Map.Empty))));

		private static Rule Root = new Sequence(
			new Action(new Match(), new Character(Syntax.root)),
			new Action(new ReferenceAssignment(), new LiteralRule(new StrategyMap(CodeKeys.Root, Meta.Map.Empty))));

		private static Rule Search = new Sequence(
			new Action(
		new Assignment(
				CodeKeys.Search), new Alternatives(
			new Sequence(
				new Action(new Match(), new Character('!')),
				new Action(
					new ReferenceAssignment(),
					Expression)),
			new Alternatives(LookupStringExpression, LookupAnythingExpression))));

		public static Rule ProgramDelayed = new DelayedRule(delegate()
		{
			return Program;
		});
		private static Rule Select = new Sequence(
			new Action(new Assignment(
				CodeKeys.Select),
				new Sequence(
					new Action(new Match(), new Character('.')),
					new Action(new Match(), FullIndentation),
					new Action(new Match(), SameIndentation),
					new Action(new Assignment(1),
						new Alternatives(
							ProgramDelayed,
							LiteralExpression,
							Root,
							Search,
							Call)),
					new Action(new Append(),
						new ZeroOrMore(new Action(new Autokey(), new Sequence(
							new Action(new Match(), new Optional(EndOfLine)),
							new Action(new Match(), SameIndentation),
							new Action(new ReferenceAssignment(), new Alternatives(LookupAnythingExpression, LookupStringExpression, Expression)))))),

					new Action(new Match(), new Optional(Dedentation))
			)));

		private static Rule KeysSearch = new Sequence(
	new Action(
new Assignment(
		CodeKeys.Search),
	new Sequence(
		new Action(new Match(), new Character('!')),
		new Action(
			new ReferenceAssignment(),
			new Alternatives(LookupStringExpression, LookupAnythingExpression, Expression)))));


		
		private static Rule Keys = new Alternatives(
			new Sequence(new Action(
				new Assignment(
					1),
					new Alternatives(
						new Sequence(new Action(new Assignment(CodeKeys.Literal), new Alternatives(LookupString, Number))),
						KeysSearch,
						new Sequence(new Action(
							new ReferenceAssignment(),

							LiteralExpression)),
			new Sequence(new Action(new Assignment(CodeKeys.Literal), new Alternatives(LookupString, Number))),
			EmptyMap,
			String,
			LookupStringExpression
			))),

			new Sequence(
			new Action(new Match(), new Character('.')),
			new Action(new Match(), FullIndentation),
			new Action(new Append(),
				new ZeroOrMore(
					new Action(new Autokey(),
						new Sequence(
				new Action(new Match(), new Optional(EndOfLine)),
			new Action(new Match(), SameIndentation),
			new Action(new ReferenceAssignment(),
				new Alternatives(
						KeysSearch,
						new Sequence(new Action(
							new ReferenceAssignment(),
							LiteralExpression)),
			EmptyMap,
			String,
			LookupStringExpression
			)))))),
			new Action(new Match(), new Optional(EndOfLine)),
			new Action(new Match(), new Optional(Dedentation)),
			new Action(new Match(), new Optional(SameIndentation))));


		public static Rule CurrentStatement = new Sequence(
			new Action(new Match(), StringRule("&=")),
			new Action(new Assignment(CodeKeys.Current), new LiteralRule(Meta.Map.Empty)),
			new Action(new Assignment(CodeKeys.Value), Expression),
			new Action(new Match(), new Optional(EndOfLine))
			);


		public static Rule KeysStatement = new Sequence(
			new Action(new Assignment(CodeKeys.Key), 
			new Alternatives(
				new Sequence(new Action(new Assignment(CodeKeys.Literal),LookupString)),

				Expression)),
			new Action(new Match(), new Optional(EndOfLine)),
			new Action(new Match(), new Optional(SameIndentation)),
			new Action(new Match(), StringRule("=")),
			new Action(new Assignment(CodeKeys.Value), Expression),
			new Action(new Match(), new Optional(EndOfLine)));


		public static Rule Statement = new Sequence(
			new Action(new ReferenceAssignment(),
				new Alternatives(
					FunctionExpression,
					new Sequence(
						new Action(new Assignment(
							CodeKeys.Keys),
							Keys),
						new Action(new Match(), new Optional(EndOfLine)),
						new Action(new Match(), new Optional(SameIndentation)),
						new Action(new Match(), new Character(':')),
						new Action(new Assignment(
							CodeKeys.Value),
							Expression),
						new Action(new Match(), new Optional(EndOfLine)))
			)));



		public static Rule ListMap = new Sequence(
			new Action(new Match(), new Character('+')),
			new Action(
				new ReferenceAssignment(),
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
					new Sequence(
			new Action(new Match(), new Optional(EndOfLine)),
			new Action(new Match(), SmallIndentation),
						new Action(
							new ReferenceAssignment(),
							new ZeroOrMore(
								new Action(new Autokey(),
									new Sequence(
										new Action(new Match(), new Optional(EndOfLine)),
										new Action(new Match(), SameIndentation),
										new Action(new ReferenceAssignment()
			, Value)
			)))
			)
			,
				new Action(new Match(), new Optional(EndOfLine)),
				new Action(new Match(), new Optional(new Alternatives(Dedentation)))
			),
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));

		public static Rule List = new Sequence(
		new Action(new Match(), new Character('+')),
		new Action(
			new Assignment(CodeKeys.Program),
			new PrePost(
				delegate(Parser p)
				{
					p.defaultKeys.Push(1);
				},
				new Sequence(
		new Action(new Match(), new Optional(EndOfLine)),
		new Action(new Match(), SmallIndentation),
					new Action(
						new Append(),
						new ZeroOrMore(
							new Action(new Autokey(),
								new Sequence(
									new Action(new Match(), new Optional(EndOfLine)),
									new Action(new Match(), SameIndentation),
									new Action(
						new CustomProduction(
						delegate(Parser p, Map map, ref Map result)
						{
							result = new StrategyMap(
								CodeKeys.Key, new StrategyMap(
										CodeKeys.Literal, p.defaultKeys.Peek()),
								CodeKeys.Value, map);
							p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
							return result;
						}
		), Expression)
		)))
		)
		,
			new Action(new Match(), new Optional(EndOfLine)),
			new Action(new Match(), new Optional(new Alternatives(Dedentation)))
		),
				delegate(Parser p)
				{
					p.defaultKeys.Pop();
				})));

		public static Rule Program = new Sequence(
	new Action(new Match(), new Character(',')),
	new Action(
		new Assignment(CodeKeys.Program),
			new Sequence(
				new Action(
				new Match(),
			EndOfLine),

				new Action(
					new Match(),
					SmallIndentation),
				new Action(new ReferenceAssignment(),
					new ZeroOrMore(
						new Action(new Autokey(),
							new Sequence(
								new Action(new Match(), new Alternatives(
									SameIndentation,
									Dedentation)),
								new Action(new ReferenceAssignment(), new Alternatives(CurrentStatement,KeysStatement, Statement)))))))));
		public abstract class Production
		{
			public abstract void Execute(Parser parser, Map map, ref Map result);
		}

		public class Action
		{
			private Rule rule;
			private Production production;
			public Action(Production production, Rule rule)
			{
				this.rule = rule;
				this.production = production;
			}
			public bool Execute(Parser parser, ref Map result)
			{
				bool matched;
				Map map = rule.Match(parser, out matched);
				if (matched)
				{
					production.Execute(parser, map, ref result);
				}
				return matched;
			}
		}
		public class Autokey : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result.Append(map);
			}
		}
		public class Assignment : Production
		{
			private Map key;
			public Assignment(Map key)
			{
				this.key = key;
			}
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result[key] = map;
			}
		}
		public class Match : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
			}
		}
		public class ReferenceAssignment : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = map;
			}
		}
		public class Append : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				foreach (Map m in map.Array)
				{
					result.Append(m);
				}
			}
		}
		public class Join : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = Library.Join(result, map);
			}
		}
		public class Merge : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = Library.Merge(result, map);
			}
		}
		public class CustomProduction : Production
		{
			private CustomActionDelegate action;
			public CustomProduction(CustomActionDelegate action)
			{
				this.action = action;
			}
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				this.action(parser, map, ref result);
			}
		}
		public delegate Map CustomActionDelegate(Parser p, Map map, ref Map result);

		public abstract class Rule
		{
			public Map Match(Parser parser, out bool matched)
			{
				int oldIndex = parser.index;
				int oldLine = parser.line;
				int oldColumn = parser.column;
				bool isStartOfFile = parser.isStartOfFile;
				Map result = MatchImplementation(parser, out matched);
				if (!matched)
				{
					parser.index = oldIndex;
					parser.line = oldLine;
					parser.column = oldColumn;
					parser.isStartOfFile = isStartOfFile;
				}
				else
				{
					if (result != null)
					{
						result.Extent = new Extent(oldLine, oldColumn, parser.line, parser.column, parser.file);
					}
				}
				return result;
			}
			protected abstract Map MatchImplementation(Parser parser, out bool match);
		}
		public class Character : CharacterRule
		{
			public Character(params char[] characters)
				: base(characters)
			{
			}
			protected override bool MatchCharacer(char c)
			{
				return c.ToString().IndexOfAny(characters) != -1 && c != Syntax.endOfFile;
			}
		}
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
				char character = parser.Look();
				if (MatchCharacer(character))
				{
					matched = true;
					parser.index++;
					parser.column++;
					if (character == Syntax.unixNewLine)
					{
						parser.line++;
						parser.column = 1;
					}
					return character;
				}
				else
				{
					matched = false;
					return null;
				}
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
				Map result = rule.Match(parser, out matched);
				post(parser);
				return result;
			}
		}
		public static Rule StringRule(string text)
		{
			List<Action> actions = new List<Action>();
			foreach (char c in text)
			{
				actions.Add(new Action(new Match(), new Character(c)));
			}
			return new Sequence(actions.ToArray());
		}
		public delegate Map ParseFunction(Parser parser, out bool matched);
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
					result = (Map)expression.Match(parser, out matched);
					if (matched)
					{
						break;
					}
				}
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
				Map result = parser.CreateMap();
				bool success = true;
				foreach (Action action in actions)
				{
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
		public class ZeroOrMoreString : ZeroOrMore
		{
			public ZeroOrMoreString(Action action)
				: base(action)
			{
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map result = base.MatchImplementation(parser, out match);
				if (match && result.IsString)
				{
					result = result.GetString();
				}
				return result;
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
		public class TwoOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = parser.CreateMap(new ListStrategy());
				int count = 0;
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
					count++;
				}
				matched = count >= 2;
				return list;
			}
			private Action action;
			public TwoOrMore(Action action)
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
		public string FileName
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
		private string Rest
		{
			get
			{
				return text.Substring(index);
			}
		}
		public int Column
		{
			get
			{
				return column;
			}
		}
		private char Look()
		{
			if (index < text.Length)
			{
				return text[index];
			}
			else
			{
				return Syntax.endOfFile;
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
	}
	public class Serialize
	{
		public static string ValueFunction(Map map)
		{
			return DoSerialize(map);
		}
		public static string DoSerialize(Map map)
		{
			return DoSerialize(map,-1).Trim();
		}
		private static string GetIndentation(int indentation)
		{
			return "".PadLeft(indentation, '\t');
		}
		private static string DoSerialize(Map map, int indentation)
		{
			try
			{
				if (map is Gac)
				{
					return "Gac";
				}
				if (map is DotNetMap)
				{
					return map.ToString();
				}
				else if (map.Count == 0)
				{
					if (indentation < 0)
					{
						return "";
					}
					else
					{
						return "0";
					}
				}
				else if (map.IsNumber)
				{
					return map.GetNumber().ToString();
				}
				else if (map.IsString)
				{

					string text=map.GetString();
					if (text.Contains("\"") || text.Contains("\n"))
					{
						string result = "\"" + Environment.NewLine;
						foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
						{
							result += GetIndentation(indentation+1) + "\t" + line + Environment.NewLine;
						}
						return result.Trim('\n', '\r') + Environment.NewLine + GetIndentation(indentation) + "\"";
					}
					else
					{
						return "\"" + text + "\"";
					}
				}
				else
				{
					string text;
					if (indentation < 0)
					{
						text = "";
					}
					else
					{
						text = "," + Environment.NewLine;
					}
					foreach (KeyValuePair<Map, Map> entry in map)
					{
						text +=
							GetIndentation(indentation + 1);
						string key;
						if (entry.Key.Count != 0 && entry.Key.IsString && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbidden) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst) != 0)
						{
							key = entry.Key.GetString();
						}
						else
						{
							key = "<" + DoSerialize(entry.Key, indentation + 1);
						}
						text += key +
							"=" +
							DoSerialize(entry.Value, indentation + 1) + Environment.NewLine;
						text = text.TrimEnd('\r', '\n') + Environment.NewLine;
					}
					return text;
				}
			}
			catch (Exception e)
			{
				int asdf = 0;
				throw e;
			}

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
			public static string TestPath
			{
				get
				{
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public class Serialization : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return Meta.Serialize.ValueFunction(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
				}
			}
			public class Basic : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"), new StrategyMap(1, "first argument", 2, "second argument"));
				}
			}
			public class Library : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);
				}
			}
			public class Performance : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
				}
			}
			public static Map Run(string path, Map argument)
			{
				//argument.Scope = Gac.gac["library"];
				Map callable = Parser.Parse(path);
				callable.Scope = Gac.gac["library"];
				return callable.Call(argument);
				//return Parser.Parse(path).Call(argument);
				//return Parser.Parse(path).Call(argument, new Position(RootPosition.rootPosition, "library")).Get();
			}
		}
		namespace TestClasses
		{
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
			public class TestClass
			{
				public class NestedClass
				{
					public static int field = 0;
				}
				public TestClass()
				{
				}
				public object CallInstanceEvent(object intArg)
				{
					return instanceEvent(intArg);
				}
				public static object CallStaticEvent(object sender)
				{
					return staticEvent(sender);
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

	public class CodeKeys
	{
		public static readonly Map Key = "key";
		public static readonly Map Expression = "expression";
		public static readonly Map Parameter = "parameter";
		public static readonly Map Root = "root";
		public static readonly Map Search = "search";
		public static readonly Map Current = "current";
		public static readonly Map Scope = "scope";
		public static readonly Map Literal = "literal";
		public static readonly Map Function = "function";
		public static readonly Map Call = "call";
		public static readonly Map Select = "select";
		public static readonly Map Program = "program";
		public static readonly Map Keys = "keys";
		public static readonly Map Value = "value";
	}
	public class NumberKeys
	{
		public static readonly Map Negative = "negative";
		public static readonly Map Denominator = "denominator";
	}
	public class ExceptionLog
	{
		public ExceptionLog(Extent extent)
		{
			this.extent = extent;
			//this.position = position;
		}
		public Extent extent;
		//public Position position;
	}
	//public class ExceptionLog
	//{
	//    public ExceptionLog(Extent extent, Position position)
	//    {
	//        this.extent = extent;
	//        this.position = position;
	//    }
	//    public Extent extent;
	//    public Position position;
	//}
	public class MetaException : Exception
	{
		private string message;
		private Extent extent;
		private List<ExceptionLog> invocationList = new List<ExceptionLog>();
		public MetaException(string message, Extent extent)
		{
			this.message = message;
			this.extent = extent;
		}
		public List<ExceptionLog> InvocationList
		{
			get
			{
				return invocationList;
			}
		}
		public override string ToString()
		{
			string message = Message;
			if (invocationList.Count != 0)
			{
				message += "\n\nStack trace:";
			}
			foreach (ExceptionLog log in invocationList)
			{
				message += "\n" + GetExtentText(log.extent);
				//message += "\n" + log.position.ToString() + "   " + GetExtentText(log.extent);
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
	public class SyntaxException : MetaException
	{
		public SyntaxException(string message, Parser parser)
			: base(message, new Extent(parser.Line, parser.Column, parser.Line, parser.Column, parser.FileName))
		{
		}
	}
	public class ExecutionException : MetaException
	{
		private Map context;
		public ExecutionException(string message, Extent extent, Map context)
			: base(message, extent)
		{
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException
	{
		public KeyDoesNotExist(Map key, Extent extent, Map map)
			: base("Key does not exist: " + Serialize.ValueFunction(key) + " in " + Serialize.ValueFunction(map), extent, map)
		{
		}
	}
	public class KeyNotFound : ExecutionException
	{
		public KeyNotFound(Map key, Extent extent, Map map)
			: base("Key not found: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
}
