//	Meta is a programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License version 2
//	as published by the Free Software Foundation.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

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
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;

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
		public static readonly Map Parent="parent";
		public static readonly Map Arg="arg";
		public static readonly Map Current="current";
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
		public MetaException(string message,Extent extent)
		{
			this.extent=extent;
            this.message = message;
		}
        public override string Message
        {
            get
            {
                return message+" In line "+extent.Start.Line+", column "+extent.Start.Column;
            }
        }
        private string message;
		private Extent extent;
	}
	public class Throw
	{
		public static void KeyDoesNotExist(Map key,Extent extent)
		{
			throw new MetaException("The key "+Serialize.Value(key)+" does not exist.",extent);
		}
		public static void KeyNotFound(Map key,Extent extent)
		{
			throw new MetaException("The key "+Serialize.Value(key)+" could not be found.",extent);
		}
	}
	//public class BreakPoint
	//{
	//    public BreakPoint(SourcePosition position)
	//    {
	//        this.position=position;
	//    }		
	//    public SourcePosition Position
	//    {
	//        get
	//        {
	//            return position;
	//        }
	//    }
	//    private SourcePosition position;
	//}
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
		public Process():this(FileSystem.singleton,new NormalMap())
		{
		}
		public void Run()
		{
			program.Call(parameter,new NormalMap());
		}
		public Map Parse(string filePath)
		{
			using(TextReader reader=new StreamReader(filePath,Encoding.Default))
			{
				return Parse(reader);
			}
		}
		public Map Parse(TextReader textReader)
		{
			return Expression(Compile(textReader),Map.Empty,Map.Empty);
		}
		// maybe make this part of Interpreter, which should be renamed to Meta
		public static Map Compile()
		{
			using(TextReader reader=new StreamReader(FileSystem.Path,Encoding.Default))
			{
				return Compile(reader);
			}
		}
		public static Map Compile(TextReader textReader)
		{
			return new Parser(textReader.ReadToEnd(),FileSystem.Path).Program();
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
			// somewhat dangerous
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
		private bool reversed=false;

		public SourcePosition BreakPoint
		{
			get
			{
				return breakPoint;
			}
			set
			{
				breakPoint=value;
			}
		}
		private SourcePosition breakPoint=new SourcePosition(0,0);
		public Map Expression(Map code,Map context,Map arg)
		{
			Map val;
			if(code.ContainsKey(CodeKeys.Call))
			{
				val=Call(code[CodeKeys.Call],context,arg);
			}
			else if(code.ContainsKey(CodeKeys.Program))
			{
				val=Program(code[CodeKeys.Program],context,arg);
			}
			else if(code.ContainsKey(CodeKeys.Literal))
			{
				val=Literal(code[CodeKeys.Literal],context);
			}
			else if(code.ContainsKey(CodeKeys.Select))
			{
				val=Select(code[CodeKeys.Select],context,arg);
			}
			else
			{
				throw new ApplicationException("Cannot compile map.");
			}
			return val;
		}
		public Map Caller
		{
			get
			{
				Map caller;
				if(callers.Count!=0)
				{
					caller=(Map)callers[callers.Count-1];
				}
				else
				{
					caller=null;
				}
				return caller;
			}
		}
		public List<Map> callers=new List<Map>();
		public Map Call(Map code,Map current,Map arg)
		{
			Map function=Expression(code[CodeKeys.Callable],current,arg);
			Map argument=Expression(code[CodeKeys.Argument],current,arg);
			callers.Add(current);
			Map result=function.Call(argument,current);
			if (result == null)
			{
				result = Map.Empty;
			}
			callers.RemoveAt(callers.Count-1);
			return result;
		}
		public bool Reversed
		{
			get
			{
				return reversed;
			}
			set
			{
				reversed=value;
			}
		}
		private bool ResumeAfterReverse(Map code)
		{
			return code.Extent.End.IsSmaller(BreakPoint);
		}
		public Map Program(Map code,Map current,Map arg)
		{
			Map local=new NormalMap();
			Program(code,current,ref local,arg);
			return local;
		}
		// think about the parameters, combine with above
		private void Program(Map code,Map parent,ref Map current,Map arg)
		{
			current.Parent=parent;
			for(int i=1;code.ContainsKey(i) && i>0;i++)
			{
				if(Reversed)
				{
					if(!ResumeAfterReverse((Map)code[i]))
					{
						// improve
						i-=2;
						continue;
					}
					else
					{
						reversed=false;
					}
				}
				Statement((Map)code[i],ref current,arg);
			}
		}
		public class Change
		{
			private Map map;
			private Map key;
			private Map oldValue;
			public Change(Map map,Map key,Map oldValue)
			{
				this.map=map;
				this.key=key;
				this.oldValue=oldValue;
			}
			public void Undo(ref Map current)
			{
				if(key.Equals(SpecialKeys.Current))
				{
					current=oldValue;
				}
				else
				{
					this.map[key]=oldValue;
				}
			}
		}
		public void Statement(Map code,ref Map context,Map arg)
		{
			Map selected=context;
			Map key;
			for(int i=0;i<code[CodeKeys.Key].Array.Count-1;i++)
			{
				key=Expression((Map)code[CodeKeys.Key].Array[i],context,arg);
				Map selection;

                if (key.Equals(SpecialKeys.Parent))
                {
                    selection = selected.Parent;
                }
				else
				{
					selection=selected[key];
				}

                if (selection == null)
				{
                    Throw.KeyDoesNotExist(key,code.Extent);
                }
				selected=selection;
				if(BreakPoint!=null && BreakPoint.IsBetween(((Map)code[CodeKeys.Key].Array[i]).Extent))
				{

					CallBreak(selected);
				}
			}
			Map lastKey=Expression((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1],context,arg);
			if(BreakPoint!=null && BreakPoint.IsBetween(((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1]).Extent))
			{
				Map oldValue;
				if(selected.ContainsKey(lastKey))
				{
					oldValue=selected[lastKey];
				}
				else
				{
					oldValue=null;
				}
				CallBreak(oldValue);
			}
			
			Map val=Expression(code[CodeKeys.Value],context,arg);

			if(lastKey.Equals(SpecialKeys.Current))
			{
				val.Parent=context.Parent;
				context=val;
			}
			else
			{
				selected[lastKey]=val;
			}
		}
		public Map Literal(Map code,Map context)
		{
			return code.Copy();
		}
		public Map Select(Map code,Map context,Map arg)
		{
			Map selected=FindFirstKey(code,context,arg);
			for(int i=1;i<code.Array.Count;i++)
			{
				Map key=Expression((Map)code.Array[i],context,arg);
				Map selection;
				if (key.Equals(SpecialKeys.Parent))
				{
				    selection = selected.Parent;
				}
				else
				{
					selection = selected[key];
				}
				if(BreakPoint!=null && BreakPoint.IsBetween(((Map)code.Array[i]).Extent))
				{
					CallBreak(selection);
				}
				if(selection==null)
				{
					Throw.KeyDoesNotExist(key,key.Extent);
				}
				selected=selection;
			}
			return selected;
		}
		private Map FindFirstKey(Map code,Map context,Map arg)
		{
			Map key=Expression((Map)code.Array[0],context,arg);
			Map val;
			if (key.Equals(SpecialKeys.Arg))
			{
				val = arg;
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
					selected = selected.FirstParent;

					if (selected == null)
					{
						Throw.KeyNotFound(key, code.Extent);
					}
				}
				val = selected[key];
			}
			if(BreakPoint!=null && BreakPoint.IsBetween(((Map)code.Array[0]).Extent))
			{
				CallBreak(val);
			}
			return val;
		}

		public event DebugBreak Break;

		public delegate void DebugBreak(Map data);
		public void CallBreak(Map data)
		{
			if(Break!=null)
			{
				if(data==null)
				{
					data=new NormalMap("nothing");
				}
				Break(data);
				Thread.CurrentThread.Suspend();
			}
		}
		public static string LibraryPath
		{
			get
			{
				return @"c:\Projects\meta\library";
			}
		}
		public static List<string> loadedAssemblies=new List<string>();
	}
	// rename remove
	public class Interpreter
	{
		// remove
		public static Map Join(Map arg) 
		{
			Integer i=1;
			Map array=new NormalMap();
			foreach(Map map in arg.Array) 
			{ 
				foreach(Map val in map.Array) 
				{
					array[i]=val;
					i+=1;
				}
			}
			return array;
		}
		public static Map Merge(Map map)
		{
			Map result=new NormalMap();
			foreach(Map current in map.Array)
			{
				foreach(KeyValuePair<Map,Map> entry in current)
				{
					result[entry.Key]=entry.Value;
				}
			}
			return result;
		}
	}
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public Map Current
		{
			get
			{
				return this;
			}
		}
        public void Append(Map map)
        {
            this[Array.Count+1] = map;
        }
		public static readonly Map Empty=new NormalMap();
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
				return Count==0 || (Count==1 && ContainsKey(Map.Empty) && this[Map.Empty].IsInteger);
			}
		}
		public virtual Integer GetInteger()
		{
			return GetIntegerDefault();
		}
		// refactor
		public Integer GetIntegerDefault()
		{
			Integer number;
			if(this.Equals(Map.Empty))
			{
				number=0;
			}
			else if(this.Count==1 && this.ContainsKey(Map.Empty))
			{
				if(this[Map.Empty].GetInteger()!=null)
				{
					number=this[Map.Empty].GetInteger()+1;
				}
				else
				{
					number=null;
				}
			}
			else
			{
				number=null;
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
				if (Array.Count == Keys.Count)
				{
					isString=this.Array.TrueForAll(delegate(Map map)
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
        public Map FirstParent
        {
            get
            {
                return firstParent;
            }
            set
            {
                firstParent = value;
            }
        }
        private Map firstParent;
		public virtual Map Parent
		{
			get
			{
				return parent;
			}
			set
			{
                if (parent == null)
                {
                    FirstParent = value;
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
		public virtual List<Map> Array
		{ 
			get
			{
				List<Map> array=new List<Map>();
				foreach(Map key in this.Keys) // TODO: need to sort the keys, by integer?? or require that keys are already sorted
				{
					if(key.IsInteger)
					{
						array.Add(this[key]);
					}
				}
				return array;
			}
		}
        public Map this[Map key]
        {
            get
            {
                Map val;
				//if (key.Equals(SpecialKeys.Parent))
				//{
				//    val = Parent;
				//}
				//else if (key.Equals(SpecialKeys.Current))
				//{
				//    val = this;
				//}
				//else
				//{
                    val = Get(key);
				//}
                return val;
            }
            set
            {
                if (value != null)
                {
                    Map val = value.Copy();
                    val.Parent = this;
                    Set(key, val);
                }
                // what should happen when a .NET method returns null or void, null should maybe be a valid value to assign, or not
                else
                {
                }
            }
        }
        protected abstract Map Get(Map key);
        protected abstract void Set(Map key, Map val);


		public virtual Map Call(Map arg,Map caller)
		{
			//this.Parameter=parameter;
			Map function=this[CodeKeys.Function];
			Map result;
			result=Process.Current.Expression(function,this,arg);
			return result;
		}

		public abstract List<Map> Keys
		{
			get;
		}
		public virtual Map Copy()
		{
			Map clone=new NormalMap();
			foreach(Map key in this.Keys)
			{
				clone[key]=this[key];
			}
			clone.Extent=Extent;
			clone.FirstParent = FirstParent;
			clone.Parent = Parent;
			return clone;
		}
		public bool ContainsKey(Map key)
		{
			bool containsKey;
			//if(key.Equals(SpecialKeys.Parameter))
			//{
			//    containsKey=this.Parameter!=null;
			//}
			//else 
			if(key.Equals(SpecialKeys.Parent))
			{
				containsKey=this.Parent!=null;
			}
			else if(key.Equals(SpecialKeys.Current))
			{
				containsKey=true;
			}
			else
			{
				containsKey=ContainsKeyImplementation(key);
			}
			return containsKey;
		}
		protected virtual bool ContainsKeyImplementation(Map key)
		{
			return Keys.Contains(key);
		}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();

        } 
		public virtual IEnumerator<KeyValuePair<Map,Map>> GetEnumerator()
		{
			return new MapEnumerator(this);
		}
		public override int GetHashCode() 
		{
			int hash=0;
			foreach(Map key in this.Keys)
			{
				unchecked
				{
					hash+=key.GetHashCode()*this[key].GetHashCode();
				}
			}
			return hash;
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
			return new NormalMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new NormalMap(new Integer(boolean?1:0));
		}
		public static implicit operator Map(char character)
		{
			return new NormalMap(new Integer(character));
		}
		public static implicit operator Map(byte integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(int integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(long integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new NormalMap(new Integer(integer));
		}
		public static implicit operator Map(string text)
		{
			return new NormalMap(text);
		}
	}

	public abstract class StrategyMap: Map
	{
		public override Map Call(Map arg, Map caller)
		{
			strategy.Panic();
			return base.Call(arg, caller);
		}
		public void InitFromStrategy(Strategy clone) 
		{
			foreach(Map key in clone.Keys)
			{
				this[key]=clone.Get(key);
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
            if (key.Equals(SpecialKeys.Current))
            {
				// refactor
                this.strategy = ((NormalMap)value).strategy.CopyImplementation();
                this.strategy.map = this;
            }
            else
            {
                strategy.Set(key,value);
            }
        }
		public override List<Map> Keys
		{
			get
			{
				return strategy.Keys;
			}
		}
		public override Map Copy()
		{
			Map clone=strategy.Copy();
			// move all of this into Map
			clone.FirstParent = FirstParent;
			clone.Parent=Parent;
			clone.Extent=Extent;
			return clone;
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return strategy.ContainsKey(key);
		}
		public override bool Equals(object toCompare)
		{
			bool isEqual;
			if(Object.ReferenceEquals(toCompare,this))
			{
				isEqual=true;
			}
			else if(toCompare is NormalMap)
			{
				isEqual=((NormalMap)toCompare).strategy.Equals(strategy);
			}
			else
			{
				isEqual=false;
			}
			return isEqual;
		}
		public override int GetHashCode() 
		{
			if(!isHashCached)
			{
				hash=this.strategy.GetHashCode();
				isHashCached=true;
			}
			return hash;
		}
		private bool isHashCached=false;
		private int hash;
		public StrategyMap(Strategy strategy)
		{
			this.strategy=strategy;
			this.strategy.map=this;
		}
		public Strategy strategy;
	}
	public class NormalMap:StrategyMap
	{
		public NormalMap(Strategy strategy):base(strategy)
		{
		}
		public NormalMap():this(new HybridDictionaryStrategy())
		{
		}
		public NormalMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public NormalMap(string text):this(new StringStrategy(text))
		{
		}
	}
	// think about this a bit, should be a map maybe
	public class RemoteStrategy : Strategy
	{
		public override Strategy CopyImplementation()
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override List<Map> Keys
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
			return Process.Current.Parse(streamReader);
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
	public class Web:Map
	{
		public override List<Map> Keys
		{
			get
			{
				return new List<Map>();
			}
		}
        protected override Map  Get(Map key)
        {
            if (!key.IsString)
            {
                throw new ApplicationException("need a string here");
            }
            return new NormalMap(new RemoteStrategy(key.GetString()));
        }
        protected override void  Set(Map key, Map val)
        {
			throw new ApplicationException("Cannot set key in Web.");
        }
		public override Map Copy()
		{
			return this;
		}
		private Web()
		{
		}
		public static Web singleton=new Web();
	}
	// this should be part of filesystem, just like the parser should be
	public class Serialize
	{
		public static string Value(Map val)
		{
			return Value(val, "");
		}
		private static string Value(Map val, string indentation)
		{
			string text;
			if (val.IsString)
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
		private static string Key(Map key, string indentation)
		{
			string text;
			if (key.IsString)
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
					text += Parser.unixNewLine + MapValue(key, indentation);
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
			return -1 == text.IndexOfAny(Parser.lookupStringForbiddenChars);//.fornew char[] { '@', ' ', '\t', '\r', '\n', '=', '.', '/', '\'', '"', '(', ')', '[', ']', '*', ':', '#', '!' });
		}
		public static string MapValue(Map map, string indentation)
		{
			string text;
			text = Parser.unixNewLine.ToString();
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				if (entry.Key.Equals(CodeKeys.Function))
				{
				    text+= indentation + Parser.functionChar + Expression(entry.Value, indentation);
				    //text+= Program(indentation + Parser.functionChar + Expression(entry.Value, indentation + Parser.indentationChar);
				}
				else
				{
					text += indentation + Key((Map)entry.Key, indentation) + Parser.statementChar + Value((Map)entry.Value, indentation + '\t');
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
			if(code.ContainsKey(CodeKeys.Call))
			{
				text=Call(code[CodeKeys.Call],indentation);
			}
			else if(code.ContainsKey(CodeKeys.Program))
			{
				text=Program(code[CodeKeys.Program],indentation);
			}
			else if(code.ContainsKey(CodeKeys.Literal))
			{
				text=Literal(code[CodeKeys.Literal],indentation);
			}
			else if(code.ContainsKey(CodeKeys.Select))
			{
				text=Select(code[CodeKeys.Select],indentation);
			}
			else
			{
				throw new ApplicationException("Cannot serialize map.");
			}
			return text;
		}
		public static string Call(Map code,string indentation)
		{
			return Expression(code[CodeKeys.Callable],indentation) + Parser.callChar + Expression(code[CodeKeys.Argument],indentation);
		}
		public static string Program(Map code,string indentation)
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
					text += Statement(statement, indentation + Parser.indentationChar,ref autoKeys);
					if (!text.EndsWith(Parser.unixNewLine.ToString()))
					{
						text += Parser.unixNewLine;
					}
				}
			}
			return text;
		}
		public static string Statement(Map code,string indentation,ref int autoKeys)
		{
			Map key = code[CodeKeys.Key];
			string text;
			if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Literal].Equals(CodeKeys.Function))
			{
				text = indentation + Parser.functionChar + Expression(code[CodeKeys.Value][CodeKeys.Literal], indentation);
				//text+= Program(indentation + Parser.functionChar + Expression(entry.Value, indentation + Parser.indentationChar);
			}
			else
			{
				Map autoKey;
				text = indentation;
				Map value=code[CodeKeys.Value];
				if(key.Count == 1 && (autoKey=key[1][CodeKeys.Literal])!=null && autoKey.IsInteger && autoKey.GetInteger()==autoKeys+1)
				{
					autoKeys++;
					if (value.ContainsKey(CodeKeys.Program))
					{
						text += "=";
					}
				}
				else
				{
					text+=Select(code[CodeKeys.Key], indentation) + Parser.statementChar;
				}
				text += Expression(value, indentation);
			}
			return text;
		}
		public static string Literal(Map code,string indentation)
		{
			return Value(code,indentation);
		}
		public static string Select(Map code,string indentation)
		{
			string text = Lookup(code[1],indentation);
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
				text = Parser.lookupStartChar + Expression(code,indentation) + Parser.lookupEndChar;
			}
			return text;
		}


		private static string StringValue(Map val, string indentation)
		{
			string text;
			if (val.IsString)
			{
				int longestMatch = 0;
				foreach (Match match in Regex.Matches(val.GetString(), "(" + Parser.stringChar + ")?(" + Parser.stringEscapeChar + ")*"))
				{
					if (match.ToString().Length > longestMatch)
					{
						longestMatch = match.Length;
					}
				}
				string escape = "";
				for (int i = 0; i < longestMatch; i++)
				{
					escape += Parser.stringEscapeChar;
				}
				text = escape + Parser.stringChar + val.GetString() + Parser.stringChar + escape;
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
	public class Transform
	{
		public static object ToDotNet(Map meta,Type target)//,out bool isConverted)
		{
			object dotNet=null;
			if(target.IsSubclassOf(typeof(Delegate)
				||target.Equals(typeof(Delegate)))&& meta.ContainsKey(CodeKeys.Function))
			{
				MethodInfo invoke=target.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate function=Method.CreateDelegateFromCode(target,invoke,meta);
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
					//object element = Transform.ToDotNet(meta[i + 1], type, out isElementConverted);
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
				dotNet=Enum.ToObject(target,meta.GetInteger().GetInt32()); // TODO: support other underlying types
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
						//isConverted=false;
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
						if(IsIntegerInRange(meta,Int64.MinValue,(Integer)Int64.MaxValue))
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
						if(IsIntegerInRange(meta,UInt32.MinValue,UInt32.MaxValue))
						{
							dotNet=Convert.ToUInt32(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.UInt64:
						if(IsIntegerInRange(meta,UInt64.MinValue,UInt64.MaxValue))
						{
							dotNet=Convert.ToUInt64(meta.GetInteger().GetInt64());
						}
						break;
					default:
						throw new ApplicationException("not implemented");
				}
			}
			//if(dotNet!=null)
			//{
			//    isConverted=true;
			//}
			//else
			//{
			//    isConverted=false;
			//}
			return dotNet;
		}
		public static bool IsIntegerInRange(Map meta,Integer minValue,Integer maxValue)
		{
			return meta.IsInteger && meta.GetInteger()>=minValue && meta.GetInteger()<=maxValue;
		}
        // is this even needed?
		public static Map ToMap(List<Map> list)
		{
			Map map=new NormalMap();
			int index=1;
			foreach(object entry in list)
			{
				map[index]=Transform.ToMeta(entry);
				index++;
			}
			return map;
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
	public class MapEnumerator: IEnumerator<KeyValuePair<Map,Map>>
	{
		private Map map; 
		public MapEnumerator(Map map)
		{
			this.map=map;
		}
        object System.Collections.IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
		public KeyValuePair<Map,Map> Current
		{
			get
			{
				return new KeyValuePair<Map,Map>(map.Keys[index],map[(Map)map.Keys[index]]);
			}
		}
        public void Dispose()
        {
        }
		public bool MoveNext()
		{
			index++;
			return index<map.Count;
		}
		public void Reset()
		{
			index=-1;
		}
		private int index=-1;
	}
	public delegate object DelegateCreatedForGenericDelegates();

	public class Method: Map
	{
		// clone isnt correct, should only override a part of the cloning, the actual cloning, not parent assignment 
		public override Map Copy()
		{
			return new Method(this.name,this.obj,this.type);
		}
        // remove?
		public override List<Map> Keys
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
		// TODO: properly support sorting of multiple argument methods
		public class ArgumentComparer: IComparer<MethodBase>
		{
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
		public override Map Call(Map argument,Map caller)
		{
			object result=null;
			bool isExecuted=false;
            if (this.name == "Equals")
            {
            }
			if(!isExecuted)
			{
				List<MethodBase> rightNumberArgumentMethods=new List<MethodBase>();
				foreach(MethodBase method in overloadedMethods)
				{
					if(argument.Array.Count==method.GetParameters().Length)
					{ 
						if(argument.Array.Count==((Map)argument).Keys.Count)
						{ 
							rightNumberArgumentMethods.Add(method);
						}
					}
				}
				if(rightNumberArgumentMethods.Count==0)
				{
					throw new ApplicationException("Method "+this.type.Name+"."+this.name+": No methods with the right number of arguments.");
				}
				rightNumberArgumentMethods.Sort(new ArgumentComparer());
				foreach(MethodBase method in rightNumberArgumentMethods)
				{
					List<object> arguments=new List<object>();
					bool argumentsMatched=true;
					ParameterInfo[] parameters=method.GetParameters();
					for(int i=0;argumentsMatched && i<parameters.Length;i++)
					{
						object arg=Transform.ToDotNet((Map)argument.Array[i], parameters[i].ParameterType);
						if (arg != null)
						{
							arguments.Add(arg);
						}
						else
						{
							argumentsMatched = false;
							break;
						}
						//arguments.Add(Transform.ToDotNet((Map)argument.Array[i], parameters[i].ParameterType, out argumentsMatched));
					}
					if(argumentsMatched)
					{
						if(method is ConstructorInfo)
						{
							try
							{
								result=((ConstructorInfo)method).Invoke(arguments.ToArray());
							}
							catch(Exception e)
							{
								throw e.InnerException;
							}
						}
						else
						{
							try
							{
								result=method.Invoke(obj,arguments.ToArray());
							}
							catch(Exception e)
							{
								throw e.InnerException;
							}
						}
						isExecuted=true;
						break;
					}
				}
			}
			if(!isExecuted)
			{
				throw new ApplicationException("Method "+this.name+" could not be called.");
			}
			return Transform.ToMeta(result);
		}
		public static Delegate CreateDelegateFromCode(Type delegateType,MethodInfo method,Map code)
		{
			CSharpCodeProvider codeProvider=new CSharpCodeProvider();
			ICodeCompiler compiler=codeProvider.CreateCompiler();
			string returnType;
			if(method==null)
			{
				returnType="object";
			}
			else
			{
				returnType=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
			}
			string source="using System;using Meta;";
			source+=@"	[Serializable]
					public class EventHandlerContainer{public "+returnType+" EventHandlerMethod";
			int counter=1;
			string argumentList="(";
			string argumentBuiling="Map arg=new NormalMap();";
			if(method!=null)
			{
				foreach(ParameterInfo parameter in method.GetParameters())
				{
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentBuiling+="arg["+counter+"]=Meta.Transform.ToMeta(arg"+counter+");";
					if(counter<method.GetParameters().Length)
					{
						argumentList+=",";
					}
					counter++;
				}
			}
			argumentList+=")";
			source+=argumentList+"{";
			source+=argumentBuiling;
			source+="Map result=callable.Call(arg,Process.Current.Caller);";
			if(method!=null)
			{
				if(!method.ReturnType.Equals(typeof(void)))
				{
					source+="return ("+returnType+")";
					source+="Meta.Transform.ToDotNet(result,typeof("+returnType+"));"; 
				}
			}
			else 
			{
				source+="return";
				source+=" result;";
			}
			source+="}";
			source+="private Map callable;";
			source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
            List<string> assemblyNames = new List<string>();//new string[] { "mscorlib.dll", "System.dll", metaDllLocation });
            assemblyNames.AddRange(Process.loadedAssemblies);
			CompilerParameters compilerParameters=new CompilerParameters((string[])assemblyNames.ToArray());//typeof(string)));
			CompilerResults compilerResults=compiler.CompileAssemblyFromSource(compilerParameters,source);
			Type containerType=compilerResults.CompiledAssembly.GetType("EventHandlerContainer",true);
			object container=containerType.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {code});
			if(method==null)
			{
				delegateType=typeof(DelegateCreatedForGenericDelegates);
			}
			Delegate result=Delegate.CreateDelegate(delegateType,
				container,"EventHandlerMethod");
			return result;
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
	public class TypeMap: DotNetContainer
	{
		public Type Type
		{
			get
			{
				return type;
			}
		}
		public override Map Copy()
		{
			return new TypeMap(type);
		}
		[NonSerialized]
		protected Method constructor;
		public TypeMap(Type targetType):base(null,targetType)
		{
			this.constructor=new Method(this.type);
		}
		public override Map Call(Map argument,Map caller)
		{
			return constructor.Call(argument,caller);
		}

	}
	[Serializable]
	public class ObjectMap: DotNetContainer
	{
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
		public override Map Copy()
		{
			return new ObjectMap(obj);
		}
	}
	public abstract class Strategy
	{
		public void Panic()
		{
			map.strategy = new HybridDictionaryStrategy();
			map.strategy.map = map;
			map.InitFromStrategy(this);
		}
		protected void Panic(Map key, Map val)
		{
			Panic();
			map.strategy.Set(key, val);
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
		// remove this if possible
		public abstract Strategy CopyImplementation();
		public virtual Map Copy() // TODO: move into Map??
		{
			StrategyMap clone;
			Strategy strategy = (Strategy)this.CopyImplementation();
			clone=new NormalMap(strategy);
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
		public abstract List<Map> Keys
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
		//public abstract Map this[Map key] 
		//{
		//    get;
		//    set;
		//}
		public abstract void Set(Map key, Map val);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}
		public override int GetHashCode()
		{
			int hash=0;
			foreach(Map key in this.Keys)
			{
				unchecked
				{
					hash+=key.GetHashCode()*this.Get(key).GetHashCode();
				}
			}
			return hash;
		}
		public override bool Equals(object strategy)
		{
			bool isEqual;
			if(Object.ReferenceEquals(strategy,this))
			{ 
				isEqual=true;
			}
			else if(!(strategy is Strategy))
			{
				isEqual=false;
			}
			else if(((Strategy)strategy).Count!=this.Count)
			{
				isEqual=false;
			}
			else
			{
				isEqual=true;
				foreach(Map key in this.Keys) 
				{
					if(!((Strategy)strategy).ContainsKey(key)||!((Strategy)strategy).Get(key).Equals(this.Get(key)))
					{
						isEqual=false;
					}
				}
			}
			return isEqual;
		}
	}
	public class CloneStrategy:Strategy
	{
		private Strategy original;
		public CloneStrategy(Strategy original)
		{
			this.original=original;
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
		public override Strategy CopyImplementation()
		{
			return new CloneStrategy(this.original);
		}
		public override bool Equals(object obj)
		{
			return original.Equals(obj);
		}
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
		public override List<Map> Keys
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
		// refactor, should also panic when called
		public override void Set(Map key, Map value)
		{
			Panic(key,value);
		}
	}
	[Serializable]
	public class StringStrategy:Strategy
	{
		public override bool IsString
		{
			get
			{
				return true;
			}
		}
		public override string GetString()
		{
			return this.text;
		}


		public override Strategy CopyImplementation()
		{
			return new StringStrategy(this.text);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode ();
		}

		public override bool Equals(object strategy)
		{
			bool isEqual;
			if(strategy is StringStrategy)
			{	
				isEqual=((StringStrategy)strategy).text==this.text;
			}
			else
			{
				isEqual=base.Equals(strategy);
			}
			return isEqual;
		}
		public override List<Map> Array
		{
			get
			{
				List<Map> list=new List<Map>();
				foreach(char iChar in text)
				{
					list.Add(new NormalMap(new Integer(iChar)));
				}
				return list;
			}
		}
        // we could use some sort of apply function here??
		public override List<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				for(int i=1;i<=text.Length;i++)
				{ 
					keys.Add(new NormalMap((Integer)i));			
				}
				return keys;
			}
		}
		private string text;
		public StringStrategy(StringStrategy clone)
		{
			this.text=clone.text;
		}
		public StringStrategy(string text)
		{
			this.text=text;
		}
		public override int Count
		{
			get
			{
				return text.Length;
			}
		}
		public override Map  Get(Map key)
		{
			if(key.IsInteger)
			{
				int iInteger=key.GetInteger().GetInt32();
				if(iInteger>0 && iInteger<=this.Count)
				{
					return text[iInteger-1];
				}
			}
			return null;
		}
		public override void  Set(Map key, Map value)
		{
			map.strategy=new HybridDictionaryStrategy();
			map.strategy.map=map;
			map.InitFromStrategy(this);
			map.strategy.Set(key,value);
		}
		public override bool ContainsKey(Map key) 
		{
			if(key.IsInteger)
			{
				return key.GetInteger()>0 && key.GetInteger()<=(Integer)this.Count;
			}
			else
			{
				return false;
			}
		}
	}
	// refactor
	public class HybridDictionaryStrategy:Strategy
	{
        // get rid of this completely, use keys from dictionary
		List<Map> keys;
		private Dictionary<Map,Map> dictionary;

		public HybridDictionaryStrategy():this(2)
		{
		}
		public override Strategy CopyImplementation()
		{
			return new CloneStrategy(this);
		}

		public HybridDictionaryStrategy(int Count)
		{
			this.keys=new List<Map>(Count);
			this.dictionary=new Dictionary<Map,Map>(Count);
		}
		public override List<Map> Array
		{
			get
			{
				List<Map> list=new List<Map>();
				for(Integer iInteger=new Integer(1);ContainsKey(new NormalMap(iInteger));iInteger+=1)
				{
					list.Add(this.Get(new NormalMap(iInteger)));
				}
				return list;
			}
		}
		public override List<Map> Keys
		{
			get
			{
				return keys;
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
			if(!this.ContainsKey(key))
			{
				keys.Add(key);
			}
			dictionary[key]=value;
		}
		public override bool ContainsKey(Map key) 
		{
			return dictionary.ContainsKey(key);
		}
	}
	public class Event:Map
	{
		EventInfo eventInfo;
		object obj;
		Type type;
		public Event(EventInfo eventInfo,object obj,Type type)
		{
			this.eventInfo=eventInfo;
			this.obj=obj;
			this.type=type;
		}
		public override Map Call(Map argument,Map caller)
		{
			Map result;
			try
			{
				Delegate eventDelegate=(Delegate)type.GetField(eventInfo.Name,BindingFlags.Public|
					BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
				if(eventDelegate!=null)
				{
					Method invoke=new Method("Invoke",eventDelegate,eventDelegate.GetType());
					// TODO: use GetRaiseMethod???
					result=invoke.Call(argument,caller);
				}
				else
				{
					result=null;
				}
			}
			catch(Exception e)
			{
				result=null;
			}
			return result;
		}

		public override Map Copy()
		{
			return new Event(eventInfo,obj,type);
		}
		public override List<Map> Keys
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
		PropertyInfo property;
		object obj;
		Type type;
		public Property(PropertyInfo property,object obj,Type type)
		{
			this.property=property;
			this.obj=obj;
			this.type=type;
		}
		public override Map Copy()
		{
			return new Property(property,obj,type);
		}
		public override List<Map> Keys
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
	}// rename to DotNetMap or so
	public abstract class DotNetContainer: Map, ISerializeEnumerableSpecial
	{
		public override string Serialize() //string indentation, StringBuilder stringBuilder,int level)
		{
			return obj != null ? this.obj.ToString() : this.type.ToString();
			//ExecuteTests.Serialize(obj!=null?this.obj:this.type,indentation,stringBuilder,level);
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
		protected override bool ContainsKeyImplementation(Map key)
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
		public override List<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				foreach(MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					keys.Add(new NormalMap(member.Name));
				}
				return keys;
			}
		}
		protected override Map Get(Map key)
		{
			Map val;
			if (key.Equals(new NormalMap("Current")))
			{
				int asdf = 0;
			}
			if (key.Equals(SpecialKeys.Parent))
			{
				val = Parent;
			}
			else if (key.IsString && type.GetMember(key.GetString(), bindingFlags).Length > 0)
			{
				string text = key.GetString();
				MemberInfo[] members = type.GetMember(text, bindingFlags);
				if (members[0] is MethodBase)
				{
					val = new Method(text, obj, type);
				}
				else if (members[0] is FieldInfo)
				{
					val = Transform.ToMeta(type.GetField(text).GetValue(obj));
				}
				else if (members[0] is PropertyInfo)
				{
					val = new Property(type.GetProperty(text), this.obj, type);// TODO: set parent here, too
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
			else if (this.obj != null && key.IsInteger && this.type.IsArray)
			{
				val = Transform.ToMeta(((Array)obj).GetValue(key.GetInteger().GetInt32()));
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
					if (member.Name == "floatValue")
					{
						int asdf = 0;
					}
					FieldInfo field = (FieldInfo)member;
					//bool isConverted;
					object val = Transform.ToDotNet(value, field.FieldType);//, out isConverted);
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
			MethodInfo invoke=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
				|BindingFlags.Public|BindingFlags.NonPublic);
			Delegate eventDelegate=Method.CreateDelegateFromCode(eventInfo.EventHandlerType,invoke,code);
			return eventDelegate;
		}
		public DotNetContainer(object obj,Type type)
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
	public class IntegerStrategy:Strategy
	{
		public override int GetHashCode()
		{
			return 0;
		}
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
		public override bool Equals(object obj)
		{
			bool isEqual;
			if(obj is IntegerStrategy)
			{
				if(((IntegerStrategy)obj).GetInteger()==GetInteger())
				{
					isEqual=true;
				}
				else
				{
					isEqual=false;
				}
			}
			else
			{
				isEqual=base.Equals(obj);
			}
			return isEqual;
		}
		public IntegerStrategy(Integer number)
		{
			this.number=new Integer(number);
		}
		public override Strategy CopyImplementation()
		{
			return new IntegerStrategy(number);
		}
		public override List<Map> Keys
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
		// refactor
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
		public override void Set(Map key,Map value)
		{
			if(key.Equals(Map.Empty))
			{
				Panic(key,value);
			}
			else
			{
				Panic(key,value);
			}
		}
	}
	public class FileAccess
	{
		public static void Write(string fileName,string text)
		{
			StreamWriter writer=new StreamWriter(fileName,false,Encoding.Default);
			writer.Write(text);
			writer.Close();
		}
		public static string Read(string fileName)
		{
			StreamReader reader=new StreamReader(fileName,Encoding.Default);
			string result=reader.ReadToEnd();
			reader.Close();
			return result;
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
		public void Run()
		{
			bool allTestsSucessful = true;
			foreach (MethodInfo test in this.GetType().GetMethods())
			{
				object[] attributes=test.GetCustomAttributes(typeof(TestAttribute),false);
				if (attributes.Length == 1)
				{
					int level = ((TestAttribute)attributes[0]).Level;
					Console.Write(test.Name + "...");
					DateTime startTime = DateTime.Now;
					object result = test.Invoke(this, new object[] {});
					TimeSpan duration = DateTime.Now - startTime;

					string testDirectory = Path.Combine(TestDirectory, test.Name);
					string resultPath = Path.Combine(testDirectory, "result.txt");
					string resultCopyPath = Path.Combine(testDirectory, "resultCopy.txt");
					string checkPath = Path.Combine(testDirectory, "check.txt");

					Directory.CreateDirectory(TestDirectory);
					if (!File.Exists(checkPath))
					{
						File.Create(checkPath).Close();
					}

					StringBuilder stringBuilder = new StringBuilder();
					Serialize(result, "", stringBuilder, level);

					string resultText = stringBuilder.ToString();
					FileAccess.Write(resultPath, resultText);
					FileAccess.Write(resultCopyPath, resultText);

					bool successful=FileAccess.Read(resultPath).Equals(FileAccess.Read(checkPath));
					
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
		public override int GetHashCode()
		{
			return base.GetHashCode ();
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
			if(obj is Extent)
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
		public Extent(int startLine,int startColumn,int endLine,int endColumn,string fileName) 
		{
			this.start=new SourcePosition(startLine,startColumn);
			this.end=new SourcePosition(endLine,endColumn);

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
	public class Parser
	{
		private string text;
		private int index;
		private string filePath;
		public Parser(string text,string filePath)
		{
			this.index=0;
			this.text=text;
			this.filePath=filePath;
		}
		private bool TryConsume(string characters)
		{
			bool consumed;
			if(index+characters.Length<text.Length && text.Substring(index,characters.Length)==characters)
			{

				consumed=true;
				foreach(char c in characters)
				{
					Consume(c);
				}
			}
			else
			{
				consumed=false;
			}
			return consumed;
		}
		private string Rest
		{
			get
			{
				return text.Substring(index);
			}
		}
		private void Consume(string characters)
		{
			foreach(char character in characters)
			{

				Consume(character);
			}
		}
		private void Consume(char character)
		{
			if(!TryConsume(character))
			{
				throw new ApplicationException("Unexpected token "+text[index]+" ,expected "+character);
			}
		}
		private bool TryConsume(char character)
		{
			bool consumed;
			if(index<text.Length && text[index]==character)
			{
				if(character==unixNewLine)
				{
					line++;
				}
				index++;
				consumed=true;
			}
			else
			{
				consumed=false;
			}
			return consumed;
		}
		public char endOfFileChar=(char)65535;
		private char Look(int count)
		{
			char character;
			int i=index+count;
			if(i<text.Length)
			{
				character=text[index+count];
			}
			else
			{
				character=endOfFileChar;
			}
			return character;
		}
		private char Look()
		{
			return Look(0);
		}

		public const char indentationChar='\t';
		private int indentationCount=-1;
		public const char unixNewLine='\n';
		public const string windowsNewLine="\r\n";
		private bool Indentation()
		{
			string indentationString="".PadLeft(indentationCount+1,indentationChar);
			bool isIndentation;
			if(TryConsume(unixNewLine+indentationString) || TryConsume(windowsNewLine+indentationString))
			{
				indentationCount++;
				isIndentation=true;
			}
			else if(isStartOfFile)
			{
				isStartOfFile=false;
				indentationCount++;
				isIndentation=true;
			}
			else
			{
				isIndentation=false;
			}
			return isIndentation;
		}

		private bool Dedentation()
		{
			int indent=0;
			while(Look(indent)==indentationChar)
			{
				indent++;
			}
			bool isDedentation;
			if(indent<indentationCount)
			{
				Consume(indentationChar);
				isDedentation=true;
				indentationCount--;
			}
			else
			{
				isDedentation=false;
			}
			return isDedentation;
		}
		private void Consume()
		{
			Consume(Look());
		}
		public const char functionChar='|';
		public const char stringChar='\"';
		private Map Expression()
		{
			Map expression=Integer();
			if(expression==null)
			{
				expression=String();
				if(expression==null)
				{
					expression=Program();
					if(expression==null)
					{
						Map select=Select();
						if(select!=null)
						{
							Map call=Call(select);
							if(call!=null)
							{
								expression=call;
							}
							else
							{
								expression=select;
							}
						}
						else
						{
							expression=null;
						}
					}
				}
			}
			return expression;
		}
		private int line=1;
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
				return index-text.Substring(0,index).LastIndexOf('\n');
			}
		}
		private Map Call(Map select)
		{
			Map call;
			Extent extent=StartExpression();
			TryConsume(callChar);
			Map argument=Expression();
			if(argument!=null)
			{
				call=new NormalMap();
				Map callCode=new NormalMap();
				callCode[CodeKeys.Callable]=select;
				callCode[CodeKeys.Argument]=argument;
				call[CodeKeys.Call]=callCode;
			}
			else
			{
				call=null;
			}
			EndExpression(extent,call);
			return call;
		}
		bool isStartOfFile=true;
		private void Whitespace()
		{
			while(TryConsume('\t') || TryConsume(' '))
			{
			}
		}
		public Map Program()
		{
			Map program;
			Extent extent=StartExpression();
			if(TryConsume(emptyMapChar))
			{
				program=new NormalMap();
				program[CodeKeys.Program]=new NormalMap();
			}
			else
			{
				if(Indentation())
				{
					program=new NormalMap();
					int counter=1;
					int defaultKey=1;
					Map statements=new NormalMap();
					while(Look()!=endOfFileChar)
					{
						Map statement=Function();
						if(Rest.IndexOf("returnInMap")<40)
						{
							int asdf=0;
						}
						if(statement==null)
						{
							statement=Statement(ref defaultKey);
						}
						statements[counter]=statement;
						counter++;

						//Whitespace();
						// TODO: fix newlines
						NewLine(); // this should not be eaten
						string newIndentation=GetIndentation();
						if(newIndentation.Length<indentationCount)
						{
							indentationCount--; // TODO: make this local variable???
							break;
						}
						else if(newIndentation.Length==indentationCount)
						{
							Consume(newIndentation);
						}
						else
						{
							throw new ApplicationException("incorrect indentation");
						}		
					}
					// TODO: combine???
					program[CodeKeys.Program]=statements;
				}
				else
				{
					program=null;
				}
			}
			EndExpression(extent,program);
			return program;
		}
		private bool NewLine()
		{
			return TryConsume('\n') || TryConsume("\r\n");
		}
		private string GetIndentation()
		{
			int i=0;
			string indentation="";
			while(Look(i)==indentationChar)
			{
				indentation+=Look(i);
				i++;
			}
			return indentation;
		}
		private void SameIndentation()
		{
			string sameIndentationString="".PadLeft(indentationCount,indentationChar);
			TryConsume(sameIndentationString);
		}
		private bool LookAny(char[] any)
		{
			return Look().ToString().IndexOfAny(any)!=-1;
		}
		public char[] integerChars=new char[] {'0','1','2','3','4','5','6','7','8','9'};
		public char[] firstIntegerChars=new char[] {'1','2','3','4','5','6','7','8','9'};
		private char ConsumeGet()
		{
			char character=Look();
			Consume(character);
			return character;
		}
		private Extent StartExpression()
		{
			return new Extent(Line,Column,0,0,"");
		}
		private void EndExpression(Extent extent,Map expression)
		{
			if(expression!=null)
			{
				extent.End.Line=Line;
				extent.End.Column=Column;
				expression.Extent=extent;
			}
		}
		private Map Integer()
		{
			Map integer;
			Extent extent=StartExpression();
			if(LookAny(firstIntegerChars))
			{
				string integerString="";
				integerString+=ConsumeGet();
				while(LookAny(integerChars)) // should be one function
				{
					integerString+=ConsumeGet();
				}
				Map literal=new NormalMap(Meta.Integer.ParseInteger(integerString));
				integer=new NormalMap();
				integer[CodeKeys.Literal]=literal;
			}
			else
			{
				integer=null;
			}
			EndExpression(extent,integer);
			return integer;
		}
		// maybe combine all literal expressions in some way
		public const char stringEscapeChar='\'';
		private Map String()
		{
			Map @string;
			Extent extent=StartExpression();

			// Look should take the expected character as parameter and return bool, just like TryConsume
			// and Consume should be called Parse or so
			if(Look()==stringChar || Look()==stringEscapeChar)
			{
				int escapeCharCount=0;
				while(TryConsume(stringEscapeChar))
				{
					escapeCharCount++;
				}
				Consume(stringChar);
				string stringText="";
				bool loop=true;
				while(true) // factor this out
				{
					if(Look()==stringChar)
					{
						if(escapeCharCount==0)
						{
							Consume(stringChar);
							break;
						}
						else
						{
							int foundEscapeCharCount=0;
							while(Look(foundEscapeCharCount+1)==stringEscapeChar)
							{
								foundEscapeCharCount++;
								if(foundEscapeCharCount==escapeCharCount)
								{
									Consume(stringChar);
									Consume("".PadLeft(escapeCharCount,stringEscapeChar));
									loop=false;
									break;
								}
							}
						}
					}
					if(loop)
					{
						stringText+=Look();
						Consume(Look());
					}
					else
					{
						break;
					}
				}
				List<string> lines=new List<string>();
				// TODO: make these constants
				stringText=stringText.Replace("\r\n","\n");
				string[] originalLines=stringText.Split('\n');
				string realText;
				for(int i=0;i<originalLines.Length;i++)
				{
					if(i==0)
					{
						lines.Add(originalLines[i]);
					}
					else
					{
						lines.Add(originalLines[i].Remove(0,Math.Min(indentationCount+1,originalLines[i].Length-originalLines[i].TrimStart(indentationChar).Length)));
					}
				}
				realText=string.Join("\n",lines.ToArray());
				Map literal=new NormalMap(realText);
				@string=new NormalMap();
				@string[CodeKeys.Literal]=literal;
			}
			else
			{
				@string=null;
			}
			EndExpression(extent,@string);
			return @string;
		}
		public const char lookupStartChar='[';
		public const char lookupEndChar=']';
		public static char[] lookupStringForbiddenChars=new char[] {callChar,indentationChar,'\r','\n',statementChar,selectChar,stringEscapeChar,functionChar,stringChar,lookupStartChar,lookupEndChar,emptyMapChar};
		public char[] lookupStringFirstCharAdditionalForbiddenChars=new char[] {'1','2','3','4','5','6','7','8','9'};
		private Map LookupString()
		{
			string lookupString="";
			Extent extent=StartExpression();
			if(LookExcept(lookupStringForbiddenChars) && LookExcept(lookupStringFirstCharAdditionalForbiddenChars))
			{
				while(LookExcept(lookupStringForbiddenChars))
				{
					lookupString+=Look();
					Consume(Look());
				}
			}
			Map lookup;
			if(lookupString.Length>0)
			{
				lookup=new NormalMap();
				lookup[CodeKeys.Literal]=new NormalMap(lookupString);
			}
			else
			{
				lookup=null;
			}
			EndExpression(extent,lookup);
			return lookup;
		}
		private bool LookExcept(char[] exceptions)
		{
			List<char> list=new List<char>(exceptions);
			list.Add(endOfFileChar);
			return Look().ToString().IndexOfAny(list.ToArray())==-1;
		}
		private Map LookupAnything()
		{
			Map lookupAnything;
			if(TryConsume(lookupStartChar))
			{
				lookupAnything=Expression();
                // ugly hack for lookup of maps
                while (TryConsume(indentationChar)) ;
				Consume(lookupEndChar);
			}
			else
			{
				lookupAnything=null;
			}
			return lookupAnything;
		}

		public const char emptyMapChar='*';
		private Map Lookup()
		{
			Extent extent=StartExpression();
			Map lookup=LookupString();
			if(lookup==null)
			{
				lookup=LookupAnything();
			}
			EndExpression(extent,lookup);
			return lookup;
		}
		public const char callChar=' ';
		public const char selectChar='.';
		private Map Select(Map keys)
		{
			Map select;
			Extent extent=StartExpression();
			if(keys!=null)
			{
				select=new NormalMap();
				select[CodeKeys.Select]=keys;
			}
			else
			{
				select=null;
			}
			EndExpression(extent,select);
			return select;
		}
		private Map Select()
		{
			return Select(Keys());
		}
		private Map Keys()
		{
			Extent extent=StartExpression();
			Map lookups=new NormalMap();
			int counter=1;
			Map lookup;
			while(true)
			{
				lookup=Lookup();
				if(lookup!=null)
				{
					lookups[counter]=lookup;
					counter++;
				}
				else
				{
					break;
				}
				if(!TryConsume(selectChar))
				{
					break;
				}
			}
			Map keys;
			if(counter>1)
			{
				keys=lookups;
			}
			else
			{
				keys=null;
			}
			EndExpression(extent,lookups);
			return keys;
		}
		public Map Function()
		{
			Extent extent=StartExpression();
			Map function=null;
			if(TryConsume(functionChar))
			{
				Map expression=Expression();
				if(expression!=null)
				{
					function=new NormalMap();
					function[CodeKeys.Key]=CreateDefaultKey(CodeKeys.Function);
					Map literal=new NormalMap();
					literal[CodeKeys.Literal]=expression;
					function[CodeKeys.Value]=literal;
				}
			}
			EndExpression(extent,function);
			return function;
		}
		public const char statementChar='=';
		public Map Statement(ref int count)
		{
			Extent extent=StartExpression();
			Map key=Keys();
			Map val;
			if(key!=null && TryConsume(statementChar))
			{
				val=Expression();
			}
			else
			{
				TryConsume(statementChar);
				if(key!=null)
				{
					Map select=Select(key);
					Map call=Call(select);
					if(call!=null)
					{
						val=call;
					}
					else
					{
						val=select;
					}
				}
				else
				{
					val=Expression();
				}
				if(val==null)
				{
					throw new ParserException("Expected value of statement",new SourcePosition(Line,Column));
				}
				key=CreateDefaultKey(new NormalMap((Integer)count));
				count++;
			}
			Map statement=new NormalMap();
			statement[CodeKeys.Key]=key;
			statement[CodeKeys.Value]=val;
			EndExpression(extent,statement);
			return statement;
		}
		private Map CreateDefaultKey(Map literal)
		{
			Map key=new NormalMap();
			Map firstKey=new NormalMap();
			firstKey[CodeKeys.Literal]=literal;
			key[1]=firstKey;
			return key;
		}
	}
	public class ParserException:ApplicationException
	{
		public override string Message
		{
			get
			{
				return this.text+" in line "+position.Line+", column "+position.Column+".";
			}
		}
		private string text;
		private SourcePosition position;
		public ParserException(string text,SourcePosition position)
		{
			this.text=text;
			this.position=position;
		}
	}
	[Serializable]
	public class FileSystem:Map
	{
		public static void Set(string text)
		{
			FileAccess.Write(Path,text);
			singleton.Load();
		}
		// make this a property
		public static FileSystem singleton;
		static FileSystem()
		{
			singleton=new FileSystem();
		}
        private Map map;
		public static string Path
		{
			get
			{
				return System.IO.Path.Combine(Process.LibraryPath,"meta.meta");
			}
		}
		public FileSystem()
		{
			Load();
		}
		private void Load()
		{
			// unlogical
			this.map=Process.Current.Parse(Path);
			this.map.Parent=Gac.singleton;
            // extremely unlogical, why does this already have a parent? it shouldnt
            this.map.FirstParent = Gac.singleton;
            //foreach (KeyValuePair<Map, Map> pair in map)
            //{
            //    pair.Value.FirstParent = this;
            //    pair.Value.Parent = this;
            //}
			// this is a little unlogical
			this.Parent=Gac.singleton;
            // unlogical, not sure whehter this is necessary
            this.FirstParent = Gac.singleton;
		}
		public override List<Map> Keys
		{
			get
			{
				return map.Keys;
			}
		}
        // maps shouldnt override this directly, but only implement the c
		protected override Map Get(Map key)
		{
			return map[key];
		}
		protected override void Set(Map key,Map val)
		{
			map[key]=val;
			Save();
		}
		public void Save()
		{
			string text=Meta.Serialize.MapValue(map,"").Trim(new char[]{'\n'});
			if(text=="\"\"")
			{
				text="";
			}
			FileAccess.Write(Process.LibraryPath,text);
		}

	}
	[Serializable]
	public class Gac:Map
	{
		private Map cache=new NormalMap();
		private bool LoadAssembly(string assemblyName)
		{
			Map key=new NormalMap(assemblyName);
			bool loaded;
			if(cache.ContainsKey(key))
			{
				loaded=true;
			}
			else
			{
				Assembly assembly=Assembly.LoadWithPartialName(assemblyName);
				if(assembly!=null)
				{
					Map val=new NormalMap();
					foreach(Type type in assembly.GetExportedTypes())
					{
						if(type.DeclaringType==null)
						{
							val[type.Name]=new TypeMap(type);
						}
					}
					if(!Process.loadedAssemblies.Contains(assembly.Location))
					{
						Process.loadedAssemblies.Add(assembly.Location);
					}
					cache[key]=val;
					loaded=true;
				}
				else
				{
					loaded=false;
				}
			}
			return loaded;
		}
		protected override Map Get(Map key)
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
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot set key " + key.ToString() + " in library.");
		}
		public override List<Map> Keys
		{
			get
			{
				List<Map> assemblies=Fusion.Assemblies;
				foreach(string dllPath in Directory.GetFiles(Process.LibraryPath,"*.dll"))
				{
					assemblies.Add(new NormalMap(Path.GetFileNameWithoutExtension(dllPath)));
				}
				foreach(string exePath in Directory.GetFiles(Process.LibraryPath,"*.exe"))
				{
					assemblies.Add(new NormalMap(Path.GetFileNameWithoutExtension(exePath)));
				}
				return assemblies;
			}
		}
		public override int Count
		{
			get
			{
				return cache.Count;
			}
		}

		protected override bool ContainsKeyImplementation(Map key)
		{
			bool containsKey;
			if(key.IsString)
			{
				containsKey=LoadAssembly(key.GetString());
			}
			else
			{
				containsKey=false;
			}
			return containsKey;
		}
		protected Map cachedAssemblyInfo=new NormalMap();
		static Gac()
		{
			Gac gac=new Gac();
			gac.cache["web"]=Web.singleton;
			singleton=gac;
		}
		public static Map singleton;


	
		// TODO: get license

		//	Source: Microsoft KB Article KB317540
		//
		//	
		//	SUMMARY
		//	The native code application programming interfaces (APIs) that allow you to interact with the Global Assembly Cache (GAC) are not documented 
		//	in the .NET Framework Software Development Kit (SDK) documentation. 
		//
		//	MORE INFORMATION
		//	CAUTION: Do not use these APIs in your application to perform assembly binds or to test for the presence of assemblies or other run time, 
		//	development, or design-time operations. Only administrative tools and setup programs must use these APIs. If you use the GAC, this directly 
		//	exposes your application to assembly binding fragility or may cause your application to work improperly on future versions of the .NET 
		//	Framework.
		//
		//	The GAC stores assemblies that are shared across all applications on a computer. The actual storage location and structure of the GAC is 
		//	not documented and is subject to change in future versions of the .NET Framework and the Microsoft Windows operating system.
		//
		//	The only supported method to access assemblies in the GAC is through the APIs that are documented in this article.
		//
		//	Most applications do not have to use these APIs because the assembly binding is performed automatically by the common language runtime. 
		//	Only custom setup programs or management tools must use these APIs. Microsoft Windows Installer has native support for installing assemblies
		//	 to the GAC.
		//
		//	For more information about assemblies and the GAC, see the .NET Framework SDK.
		//
		//	Use the GAC API in the following scenarios: 
		//	When you install an assembly to the GAC.
		//	When you remove an assembly from the GAC.
		//	When you export an assembly from the GAC.
		//	When you enumerate assemblies that are available in the GAC.
		//	NOTE: CoInitialize(Ex) must be called before you use any of the functions and interfaces that are described in this specification. 
		//	
		public class Fusion
		{
			public static List<Map> Assemblies
			{
				get
				{
					List<Map> assemblies=new List<Map>();
					IAssemblyEnum assemblyEnum=CreateGACEnum();
					IAssemblyName iname; 

					assemblies.Add(new NormalMap("mscorlib"));
					while (GetNextAssembly(assemblyEnum, out iname) == 0)
					{
						try
						{
							string assemblyName=GetAssemblyName(iname);
							if(assemblyName!="Microsoft.mshtml")
							{
								assemblies.Add(new NormalMap(assemblyName));
							}
						}
						catch(Exception e)
						{
						}
					}
					return assemblies;
				}
			}
			private static string GetAssemblyName(IAssemblyName assemblyName)
			{ 
				AssemblyName name = new AssemblyName();
				name.Name = GetName(assemblyName);
				name.Version = GetVersion(assemblyName);
				name.CultureInfo = GetCulture(assemblyName);
				name.SetPublicKeyToken(GetPublicKeyToken(assemblyName));
				return name.Name;
			}
			[DllImport("fusion.dll", SetLastError=true, PreserveSig=false)]
			static extern void CreateAssemblyEnum(out IAssemblyEnum pEnum, IntPtr pUnkReserved, IAssemblyName pName,
				ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved);
			private static  String GetDisplayName(IAssemblyName name, ASM_DISPLAY_FLAGS which)
			{
				uint bufferSize = 255;
				StringBuilder buffer = new StringBuilder((int) bufferSize);
				name.GetDisplayName(buffer, ref bufferSize, which);
				return buffer.ToString();
			}
			private static  String GetName(IAssemblyName name)
			{
				uint bufferSize = 255;
				StringBuilder buffer = new StringBuilder((int) bufferSize);
				name.GetName(ref bufferSize, buffer);
				return buffer.ToString();
			}
			private static Version GetVersion(IAssemblyName name)
			{
				uint major;
				uint minor;
				name.GetVersion(out major, out minor);
				return new Version((int)major>>16, (int)major&0xFFFF, (int)minor>>16, (int)minor&0xFFFF);
			}
			private static byte[] GetPublicKeyToken(IAssemblyName name)
			{
				byte[] result = new byte[8];
				uint bufferSize = 8;
				IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
				name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY_TOKEN, buffer, ref bufferSize);
				for (int i = 0; i < 8; i++)
					result[i] = Marshal.ReadByte(buffer, i);
				Marshal.FreeHGlobal(buffer);
				return result;
			}
			private static byte[] GetPublicKey(IAssemblyName name)
			{
				uint bufferSize = 512;
				IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
				name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY, buffer, ref bufferSize);
				byte[] result = new byte[bufferSize];
				for (int i = 0; i < bufferSize; i++)
					result[i] = Marshal.ReadByte(buffer, i);
				Marshal.FreeHGlobal(buffer);
				return result;
			}
			private static CultureInfo GetCulture(IAssemblyName name)
			{
				uint bufferSize = 255;
				IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
				name.GetProperty(ASM_NAME.ASM_NAME_CULTURE, buffer, ref bufferSize);
				string result = Marshal.PtrToStringAuto(buffer);
				Marshal.FreeHGlobal(buffer);
				return new CultureInfo(result);
			}
			private static IAssemblyEnum CreateGACEnum()
			{
				IAssemblyEnum ae;

				Fusion.CreateAssemblyEnum(out ae, (IntPtr)0, null, ASM_CACHE_FLAGS.ASM_CACHE_GAC, (IntPtr)0);

				return ae;
			}
			private static int GetNextAssembly(IAssemblyEnum enumerator, out IAssemblyName name)
			{
				return enumerator.GetNextAssembly((IntPtr)0, out name, 0);
			}
			[ComImport, Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E"),
				InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
				private interface IAssemblyName
			{
				[PreserveSig]
				int SetProperty(ASM_NAME PropertyId,IntPtr pvProperty,uint cbProperty);
				[PreserveSig]
				int GetProperty(ASM_NAME PropertyId,IntPtr pvProperty,ref uint pcbProperty);
				[PreserveSig]
				int Finalize();
				[PreserveSig]
				int GetDisplayName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szDisplayName,
					ref uint pccDisplayName,ASM_DISPLAY_FLAGS dwDisplayFlags);
				[PreserveSig]
				int BindToObject(ref Guid refIID,[MarshalAs(UnmanagedType.IUnknown)] object pUnkSink,
					[MarshalAs(UnmanagedType.IUnknown)] object pUnkContext,
					[MarshalAs(UnmanagedType.LPWStr)] string szCodeBase,
					long llFlags,IntPtr pvReserved,uint cbReserved,out IntPtr ppv);
				[PreserveSig]
				int GetName(ref uint lpcwBuffer,[Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzName);
				[PreserveSig]
				int GetVersion(out uint pdwVersionHi,out uint pdwVersionLow);
				[PreserveSig]
				int IsEqual(IAssemblyName pName,ASM_CMP_FLAGS dwCmpFlags);
				[PreserveSig]
				int Clone(out IAssemblyName pName);
			}
			[ComImport, Guid("21b8916c-f28e-11d2-a473-00c04f8ef448"),
				InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
				private interface IAssemblyEnum
			{
				[PreserveSig()]
				int GetNextAssembly(IntPtr pvReserved,out IAssemblyName ppName,uint dwFlags);
				[PreserveSig()]
				int Reset();
				[PreserveSig()]
				int Clone(out IAssemblyEnum ppEnum);
			}

			[Flags]
				public enum ASM_DISPLAY_FLAGS
			{
				VERSION = 0x1,
				CULTURE = 0x2,
				PUBLIC_KEY_TOKEN = 0x4,
				PUBLIC_KEY = 0x8,
				CUSTOM = 0x10,
				PROCESSORARCHITECTURE = 0x20,
				LANGUAGEID = 0x40
			}

			[Flags]
				public enum ASM_CMP_FLAGS
			{
				NAME = 0x1,
				MAJOR_VERSION = 0x2,
				MINOR_VERSION = 0x4,
				BUILD_NUMBER = 0x8,
				REVISION_NUMBER = 0x10,
				PUBLIC_KEY_TOKEN = 0x20,
				CULTURE = 0x40,
				CUSTOM = 0x80,
				ALL = NAME | MAJOR_VERSION | MINOR_VERSION |
					REVISION_NUMBER | BUILD_NUMBER |
					PUBLIC_KEY_TOKEN | CULTURE | CUSTOM,
				DEFAULT = 0x100
			}
			public enum ASM_NAME
			{
				ASM_NAME_PUBLIC_KEY = 0,
				ASM_NAME_PUBLIC_KEY_TOKEN,
				ASM_NAME_HASH_VALUE,
				ASM_NAME_NAME,
				ASM_NAME_MAJOR_VERSION,
				ASM_NAME_MINOR_VERSION,
				ASM_NAME_BUILD_NUMBER,
				ASM_NAME_REVISION_NUMBER,
				ASM_NAME_CULTURE,
				ASM_NAME_PROCESSOR_ID_ARRAY,
				ASM_NAME_OSINFO_ARRAY,
				ASM_NAME_HASH_ALGID,
				ASM_NAME_ALIAS,
				ASM_NAME_CODEBASE_URL,
				ASM_NAME_CODEBASE_LASTMOD,
				ASM_NAME_NULL_PUBLIC_KEY,
				ASM_NAME_NULL_PUBLIC_KEY_TOKEN,
				ASM_NAME_CUSTOM,
				ASM_NAME_NULL_CUSTOM,                
				ASM_NAME_MVID,
				ASM_NAME_MAX_PARAMS
			}
			[Flags]
				public enum ASM_CACHE_FLAGS
			{
				ASM_CACHE_ZAP = 0x1,
				ASM_CACHE_GAC = 0x2,
				ASM_CACHE_DOWNLOAD = 0x4
			}
		}
	}


	//************************************************************************************
	// Integer Class Version 1.03
	//
	// Copyright (c) 2002 Chew Keong TAN
	// All rights reserved.
	//
	// Permission is hereby granted, free of charge, to any person obtaining a
	// copy of this software and associated documentation files (the
	// "Software"), to deal in the Software without restriction, including
	// without limitation the rights to use, copy, modify, merge, publish,
	// distribute, and/or sell copies of the Software, and to permit persons
	// to whom the Software is furnished to do so, provided that the above
	// copyright notice(s) and this permission notice appear in all copies of
	// the Software and that both the above copyright notice(s) and this
	// permission notice appear in supporting documentation.
	//
	// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
	// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
	// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
	// OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
	// HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL
	// INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING
	// FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
	// NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
	// WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
	//
	//
	// Disclaimer
	// ----------
	// Although reasonable care has been taken to ensure the correctness of this
	// implementation, this code should never be used in any application without
	// proper verification and testing.  I disclaim all liability and responsibility
	// to any person or entity with respect to any loss or damage caused, or alleged
	// to be caused, directly or indirectly, by the use of this Integer class.
	[Serializable]
	public class Integer
	{
		public Integer(double val)
		{
			Integer integer = new Integer(1);
			while (Math.Abs(val) / (double)int.MaxValue > 1.0d)
			{
				val /= int.MaxValue;
				integer *= int.MaxValue;
			}
			integer *= (Integer)Convert.ToInt32(val);
			this.data = integer.data;
		}
        // this is not even used, maybe, however it might be in case of overflow
        public Integer(Map map)
        {
            this.data = map.GetInteger().data;
        }
		public static Integer ParseInteger(string text)
		{
			Integer result=new Integer(0);
			if(text.Equals(""))
			{
				result=null;
			}
			else
			{
				int index=0;
				for(;index<text.Length;index++)
				{
					if(char.IsDigit(text[index]))
					{
						result=result*10+(Integer)(text[index]-'0');
					}
					else
					{
						return null;
					}
				}

			}
			return result;
		}
		private const int maxLength = 70;
		private uint[] data = null;
		public int dataLength;

		public Integer()
		{
			data = new uint[maxLength];
			dataLength = 1;
		}

		public Integer(long value)
		{
			data = new uint[maxLength];
			long tempVal = value;

			// copy bytes from long to Integer without any assumption of
			// the length of the long datatype

			dataLength = 0;
			while(value != 0 && dataLength < maxLength)
			{
				data[dataLength] = (uint)(value & 0xFFFFFFFF);
				value >>= 32;
				dataLength++;
			}

			if(tempVal > 0)         // overflow check for +ve value
			{
				if(value != 0 || (data[maxLength-1] & 0x80000000) != 0)
					throw(new ArithmeticException("Positive overflow in constructor."));
			}
			else if(tempVal < 0)    // underflow check for -ve value
			{
				if(value != -1 || (data[dataLength-1] & 0x80000000) == 0)
					throw(new ArithmeticException("Negative underflow in constructor."));
			}

			if(dataLength == 0)
				dataLength = 1;
		}

		public Integer(ulong value)
		{
			data = new uint[maxLength];

			// copy bytes from ulong to Integer without any assumption of
			// the length of the ulong datatype

			dataLength = 0;
			while(value != 0 && dataLength < maxLength)
			{
				data[dataLength] = (uint)(value & 0xFFFFFFFF);
				value >>= 32;
				dataLength++;
			}

			if(value != 0 || (data[maxLength-1] & 0x80000000) != 0)
				throw(new ArithmeticException("Positive overflow in constructor."));

			if(dataLength == 0)
				dataLength = 1;
		}
		public Integer(Integer bi)
		{
			data = new uint[maxLength];

			dataLength = bi.dataLength;

			for(int i = 0; i < dataLength; i++)
				data[i] = bi.data[i];
		}
		public static implicit operator Integer(long value)
		{
			return (new Integer(value));
		}

		public static implicit operator Integer(ulong value)
		{
			return (new Integer(value));
		}

		public static implicit operator Integer(int value)
		{
			return (new Integer((long)value));
		}

		public static implicit operator Integer(uint value)
		{
			return (new Integer((ulong)value));
		}

		public static Integer operator +(Integer bi1, Integer bi2)
		{
			Integer result = new Integer();

			result.dataLength = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

			long carry = 0;
			for(int i = 0; i < result.dataLength; i++)
			{
				long sum = (long)bi1.data[i] + (long)bi2.data[i] + carry;
				carry  = sum >> 32;
				result.data[i] = (uint)(sum & 0xFFFFFFFF);
			}

			if(carry != 0 && result.dataLength < maxLength)
			{
				result.data[result.dataLength] = (uint)(carry);
				result.dataLength++;
			}

			while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
				result.dataLength--;


			// overflow check
			int lastPos = maxLength - 1;
			if((bi1.data[lastPos] & 0x80000000) == (bi2.data[lastPos] & 0x80000000) &&
				(result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
			{
				throw (new ArithmeticException());
			}

			return result;
		}

		public static Integer operator -(Integer bi1, Integer bi2)
		{
			Integer result = new Integer();

			result.dataLength = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

			long carryIn = 0;
			for(int i = 0; i < result.dataLength; i++)
			{
				long diff;

				diff = (long)bi1.data[i] - (long)bi2.data[i] - carryIn;
				result.data[i] = (uint)(diff & 0xFFFFFFFF);

				if(diff < 0)
					carryIn = 1;
				else
					carryIn = 0;
			}

			// roll over to negative
			if(carryIn != 0)
			{
				for(int i = result.dataLength; i < maxLength; i++)
					result.data[i] = 0xFFFFFFFF;
				result.dataLength = maxLength;
			}

			// fixed in v1.03 to give correct datalength for a - (-b)
			while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
				result.dataLength--;

			// overflow check

			int lastPos = maxLength - 1;
			if((bi1.data[lastPos] & 0x80000000) != (bi2.data[lastPos] & 0x80000000) &&
				(result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
			{
				throw (new ArithmeticException());
			}

			return result;
		}

		public static Integer operator *(Integer bi1, Integer bi2)
		{
			int lastPos = maxLength-1;
			bool bi1Neg = false, bi2Neg = false;

			// take the absolute value of the inputs
			try
			{
				if((bi1.data[lastPos] & 0x80000000) != 0)     // bi1 negative
				{
					bi1Neg = true; bi1 = -bi1;
				}
				if((bi2.data[lastPos] & 0x80000000) != 0)     // bi2 negative
				{
					bi2Neg = true; bi2 = -bi2;
				}
			}
			catch(Exception) {}

			Integer result = new Integer();

			// multiply the absolute values
			try
			{
				for(int i = 0; i < bi1.dataLength; i++)
				{
					if(bi1.data[i] == 0)    continue;

					ulong mcarry = 0;
					for(int j = 0, k = i; j < bi2.dataLength; j++, k++)
					{
						// k = i + j
						ulong val = ((ulong)bi1.data[i] * (ulong)bi2.data[j]) +
							(ulong)result.data[k] + mcarry;

						result.data[k] = (uint)(val & 0xFFFFFFFF);
						mcarry = (val >> 32);
					}

					if(mcarry != 0)
						result.data[i+bi2.dataLength] = (uint)mcarry;
				}
			}
			catch(Exception)
			{
				throw(new ArithmeticException("Multiplication overflow."));
			}


			result.dataLength = bi1.dataLength + bi2.dataLength;
			if(result.dataLength > maxLength)
				result.dataLength = maxLength;

			while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
				result.dataLength--;

			// overflow check (result is -ve)
			if((result.data[lastPos] & 0x80000000) != 0)
			{
				if(bi1Neg != bi2Neg && result.data[lastPos] == 0x80000000)    // different sign
				{
					// handle the special case where multiplication produces
					// a max negative number in 2's complement.

					if(result.dataLength == 1)
						return result;
					else
					{
						bool isMaxNeg = true;
						for(int i = 0; i < result.dataLength - 1 && isMaxNeg; i++)
						{
							if(result.data[i] != 0)
								isMaxNeg = false;
						}

						if(isMaxNeg)
							return result;
					}
				}

				throw(new ArithmeticException("Multiplication overflow."));
			}

			// if input has different signs, then result is -ve
			if(bi1Neg != bi2Neg)
				return -result;

			return result;
		}

		public static Integer operator -(Integer bi1)
		{
			// handle neg of zero separately since it'll cause an overflow
			// if we proceed.

			if(bi1.dataLength == 1 && bi1.data[0] == 0)
				return (new Integer());

			Integer result = new Integer(bi1);

			// 1's complement
			for(int i = 0; i < maxLength; i++)
				result.data[i] = (uint)(~(bi1.data[i]));

			// add one to result of 1's complement
			long val, carry = 1;
			int index = 0;

			while(carry != 0 && index < maxLength)
			{
				val = (long)(result.data[index]);
				val++;

				result.data[index] = (uint)(val & 0xFFFFFFFF);
				carry = val >> 32;

				index++;
			}

			if((bi1.data[maxLength-1] & 0x80000000) == (result.data[maxLength-1] & 0x80000000))
				throw (new ArithmeticException("Overflow in not.\n"));

			result.dataLength = maxLength;

			while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
				result.dataLength--;
			return result;
		}

		public static bool operator ==(Integer bi1, Integer bi2)
		{
			return (object.ReferenceEquals(bi1,bi2)) || bi1.Equals(bi2);
		}


		public static bool operator !=(Integer bi1, Integer bi2)
		{
			return !(bi1==bi2);
		}

		public override bool Equals(object o)
		{
			if(!(o is Integer))
			{
				return false;
			}
			Integer bi = (Integer)o;

			if(this.dataLength != bi.dataLength)
				return false;

			for(int i = 0; i < this.dataLength; i++)
			{
				if(this.data[i] != bi.data[i])
					return false;
			}
			return true;
		}

		public override int GetHashCode() 
		{
			Integer x=new Integer(this);
			while(x>int.MaxValue) 
			{
				x=x-int.MaxValue;
			}
			return x.GetInt32();
		}

		public static bool operator >(Integer bi1, Integer bi2)
		{
			int pos = maxLength - 1;

			// bi1 is negative, bi2 is positive
			if((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
				return false;

				// bi1 is positive, bi2 is negative
			else if((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
				return true;

			// same sign
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			for(pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--);

			if(pos >= 0)
			{
				if(bi1.data[pos] > bi2.data[pos])
					return true;
				return false;
			}
			return false;
		}


		public static bool operator <(Integer bi1, Integer bi2)
		{
			int pos = maxLength - 1;

			// bi1 is negative, bi2 is positive
			if((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
				return true;

				// bi1 is positive, bi2 is negative
			else if((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
				return false;

			// same sign
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			for(pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--);

			if(pos >= 0)
			{
				if(bi1.data[pos] < bi2.data[pos])
					return true;
				return false;
			}
			return false;
		}


		public static bool operator >=(Integer bi1, Integer bi2)
		{
			return (bi1 == bi2 || bi1 > bi2);
		}


		public static bool operator <=(Integer bi1, Integer bi2)
		{
			return (bi1 == bi2 || bi1 < bi2);
		}

		private static void singleByteDivide(Integer bi1, Integer bi2,
			Integer outQuotient, Integer outRemainder)
		{
			uint[] result = new uint[maxLength];
			int resultPos = 0;

			// copy dividend to reminder
			for(int i = 0; i < maxLength; i++)
				outRemainder.data[i] = bi1.data[i];
			outRemainder.dataLength = bi1.dataLength;

			while(outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength-1] == 0)
				outRemainder.dataLength--;

			ulong divisor = (ulong)bi2.data[0];
			int pos = outRemainder.dataLength - 1;
			ulong dividend = (ulong)outRemainder.data[pos];


			if(dividend >= divisor)
			{
				ulong quotient = dividend / divisor;
				result[resultPos++] = (uint)quotient;

				outRemainder.data[pos] = (uint)(dividend % divisor);
			}
			pos--;

			while(pos >= 0)
			{

				dividend = ((ulong)outRemainder.data[pos+1] << 32) + (ulong)outRemainder.data[pos];
				ulong quotient = dividend / divisor;
				result[resultPos++] = (uint)quotient;

				outRemainder.data[pos+1] = 0;
				outRemainder.data[pos--] = (uint)(dividend % divisor);
				//Console.WriteLine(">>>> " + bi1);
			}

			outQuotient.dataLength = resultPos;
			int j = 0;
			for(int i = outQuotient.dataLength - 1; i >= 0; i--, j++)
				outQuotient.data[j] = result[i];
			for(; j < maxLength; j++)
				outQuotient.data[j] = 0;

			while(outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength-1] == 0)
				outQuotient.dataLength--;

			if(outQuotient.dataLength == 0)
				outQuotient.dataLength = 1;

			while(outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength-1] == 0)
				outRemainder.dataLength--;
		}

		public static Integer operator |(Integer bi1, Integer bi2)
		{
			Integer result = new Integer();

			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

			for(int i = 0; i < len; i++)
			{
				uint sum = (uint)(bi1.data[i] | bi2.data[i]);
				result.data[i] = sum;
			}

			result.dataLength = maxLength;

			while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
				result.dataLength--;

			return result;
		}

		public Integer abs()
		{
			if((this.data[maxLength - 1] & 0x80000000) != 0)
				return (-this);
			else
				return (new Integer(this));
		}

		public override string ToString()
		{
			return ToString(10);
		}

		// reduce this to radix 10
		public string ToString(int radix)
		{
			if(radix < 2 || radix > 36)
				throw (new ArgumentException("Radix must be >= 2 and <= 36"));

			string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string result = "";

			Integer a = this;

			bool negative = false;
			if((a.data[maxLength-1] & 0x80000000) != 0)
			{
				negative = true;
				try
				{
					a = -a;
				}
				catch(Exception) {}
			}

			Integer quotient = new Integer();
			Integer remainder = new Integer();
			Integer biRadix = new Integer(radix);

			if(a.dataLength == 1 && a.data[0] == 0)
				result = "0";
			else
			{
				while(a.dataLength > 1 || (a.dataLength == 1 && a.data[0] != 0))
				{
					singleByteDivide(a, biRadix, quotient, remainder);

					if(remainder.data[0] < 10)
						result = remainder.data[0] + result;
					else
						result = charSet[(int)remainder.data[0] - 10] + result;

					a = quotient;
				}
				if(negative)
					result = "-" + result;
			}

			return result;
		}

		public int GetInt32()
		{
			return (int)data[0];
		}

		public long GetInt64()
		{
			long val = 0;

			val = (long)data[0];
			try
			{       // exception if maxLength = 1
				val |= (long)data[1] << 32;
			}
			catch(Exception)
			{
				if((data[0] & 0x80000000) != 0) // negative
					val = (int)data[0];
			}

			return val;
		}
	}
}
