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
using Microsoft.VisualStudio.DebuggerVisualizers;
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
		public MetaException(string message,Extent extent)
		{
			this.extent=extent;
            this.message = message;
		}
        public override string Message
        {
            get
            {
				string text = message;
				if (extent != null)
				{
					text += " In line " + extent.Start.Line + ", column " + extent.Start.Column;
				}
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
			throw new MetaException("The key "+Serialize.Value(key)+" does not exist.",extent);
		}
		public static void KeyNotFound(Map key,Extent extent)
		{
			throw new MetaException("The key "+Serialize.Value(key)+" could not be found.",extent);
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
		public Process() : this(LocalStrategy.singleton.Map, new NormalMap())
		{
		}
		public void Run()
		{
			program.Call(parameter);
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
		public static Map Compile(TextReader textReader)
		{
			return new Parser(textReader.ReadToEnd(),LocalStrategy.Path).Program();
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
		public Map Call(Map code, Map current, Map arg)
		{
			Map function = Expression(code[CodeKeys.Callable], current, arg);
			Map argument = Expression(code[CodeKeys.Argument], current, arg);
			Map result = function.Call(argument);
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
		public Map Program(Map code,Map current,Map arg)
		{
			Map local=new NormalMap();
			Program(code,current,ref local,arg);
			return local;
		}
		private void Program(Map code,Map parent,ref Map current,Map arg)
		{
			current.Parent=parent;
			for(int i=1;code.ContainsKey(i) && i>0;i++)
			{
				Statement((Map)code[i],ref current,arg);
			}
		}
		public void Statement(Map code, ref Map context, Map arg)
		{
			Map selected = context;
			Map key;
			Map keys = code[CodeKeys.Key];
			int i = 1;
			for (; keys.ContainsKey(i + 1);)
			{
				key = Expression((Map)keys[i], context, arg);
				Map selection;
				if (key.Equals(new NormalMap("testSubDir")))
				{
				}
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
					Throw.KeyDoesNotExist(key, code.Extent);
				}
				selected = selection;
				i++;
			}
			Map lastKey = Expression((Map)keys[i], context, arg);
			if (lastKey.Equals(new NormalMap("html")))
			{
				int asdf = 0;
			}

			Map val = Expression(code[CodeKeys.Value], context, arg);

			if (lastKey.Equals(SpecialKeys.Current))
			{
				val.Parent = context.Parent;
				context = val;
			}
			else
			{
				selected[lastKey] = val;
			}
		}
		public Map Literal(Map code,Map context)
		{
			return code.Copy();
		}
		public Map Select(Map code, Map context, Map arg)
		{
			Map selected = FindFirstKey(code, context, arg);
			for (int i = 2;code.ContainsKey(i); i++)
			{
				Map key = Expression((Map)code[i], context, arg);
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
					object x = selected["Item"];
					Throw.KeyDoesNotExist(key, key.Extent);
				}
				selected = selection;
			}
			return selected;
		}
		private Map FindFirstKey(Map code, Map context, Map arg)
		{
			Map key = Expression((Map)code[1], context, arg);
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
					selected = selected.Scope;

					if (selected == null)
					{
						Throw.KeyNotFound(key, code.Extent);
					}
				}
				val = selected[key];
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
		// rename
		public static string InstallationPath
		{
			get
			{
				return @"c:\Projects\meta\Meta";
			}
		}
		public static List<string> loadedAssemblies=new List<string>();
	}
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public string GetKeyStrings()
		{
			string text="";
			foreach (Map key in this.Keys)
			{
				text += Meta.Serialize.Key(key,"") + " ";
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
				if (Array.Count == Keys.Count)
				{
					isString=this.Array.TrueForAll(
						delegate(Map map) {
							return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);});
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
		public virtual int ArrayCount
		{
			get
			{
				return Array.Count;
			}
		}
		public virtual List<Map> Array
		{
			get
			{
				List<Map> array = new List<Map>();
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
                    Map val = value.Copy();
                    val.Parent = this;
                    Set(key, val);
                }
            }
        }
        protected abstract Map Get(Map key);
        protected abstract void Set(Map key, Map val);
		public virtual Map Call(Map arg)
		{
			Map function = this[CodeKeys.Function];
			Map result = Process.Current.Expression(function, this, arg);
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
			Map clone = new NormalMap();
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
			return new NormalMap(new Integer((int)(boolean?1:0)));
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
	public class NormalMap:Map
	{
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
			if (key.Equals(SpecialKeys.Current))
			{
				// refactor
				this.strategy = ((NormalMap)value).strategy.CopyImplementation();
				this.strategy.map = this;
			}
			else
			{
				strategy.Set(key, value);
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
			else if (toCompare is NormalMap)
			{
				isEqual = ((NormalMap)toCompare).strategy.Equals(strategy);
			}
			else
			{
				isEqual = false;
			}
			return isEqual;
		}
		public override int GetHashCode()
		{
			if (!isHashCached)
			{
				hash = this.strategy.GetHashCode();
				isHashCached = true;
			}
			return hash;
		}
		private bool isHashCached = false;
		private int hash;
		public MapStrategy strategy;
		public NormalMap(ICollection<Map> list):this()
		{
			int index = 1;
			foreach (object entry in list)
			{
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}
		public NormalMap(MapStrategy strategy)
		{
			this.strategy = strategy;
			this.strategy.map = this;
		}
		public NormalMap():this(new DictionaryStrategy())
		{
		}
		public NormalMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public NormalMap(string text):this(new StringStrategy(text))
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
	public class NetStrategy:MapStrategy
	{
		public static readonly NormalMap Net = new NormalMap(new NetStrategy());
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
				val = LocalStrategy.singleton.Map;
			}
			else
			{
				val=new NormalMap(new RemoteStrategy(key.GetString()));
			}
			return val;
        }
        public override void Set(Map key, Map val)
        {
			if (key.Equals(SpecialKeys.Local))
			{
				((LocalStrategy)LocalStrategy.singleton).Replace(val);
			}
			else
			{
				throw new ApplicationException("Cannot set key in Web.");
			}
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
			if (count == ((Map)argument).Count)
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
					object arg = Transform.ToDotNet((Map)argument.Array[i], parameters[i].ParameterType);
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
				Map arg = new NormalMap();
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
	public class TypeMap: ContainerMap
	{
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
	public class ObjectMap: ContainerMap
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
		protected override Map CopyImplementation()
		{
			return new ObjectMap(obj);
		}
	}
	public abstract class MapStrategy
	{
		public void Panic()
		{
			map.strategy = new DictionaryStrategy();
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

		public NormalMap map;

		public abstract MapStrategy CopyImplementation();
		public virtual Map Copy()
		{
			NormalMap clone;
			MapStrategy strategy = (MapStrategy)this.CopyImplementation();
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
			else if (!(strategy is MapStrategy))
			{
				isEqual = false;
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
					if(!((MapStrategy)strategy).ContainsKey(key)||!((MapStrategy)strategy).Get(key).Equals(this.Get(key)))
					{
						isEqual=false;
					}
				}
			}
			return isEqual;
		}
	}
	public class CloneStrategy:MapStrategy
	{
		private MapStrategy original;
		public CloneStrategy(MapStrategy original)
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
		public override MapStrategy CopyImplementation()
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
	[Serializable]
	public class StringStrategy:MapStrategy
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


		public override MapStrategy CopyImplementation()
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
			if (strategy is StringStrategy)
			{
				isEqual = ((StringStrategy)strategy).text.Equals(this.text);
			}
			else
			{
				isEqual = base.Equals(strategy);
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
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				for(int i=1;i<=text.Length;i++)
				{ 
					keys.Add(new NormalMap(i));			
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
			map.strategy=new DictionaryStrategy();
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
	public class DictionaryStrategy:MapStrategy
	{
		private Dictionary<Map,Map> dictionary;
		public DictionaryStrategy():this(2)
		{
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
				for(Integer iInteger=new Integer(1);ContainsKey(new NormalMap(iInteger));iInteger+=1)
				{
					list.Add(this.Get(new NormalMap(iInteger)));
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
	public abstract class ContainerMap: Map, ISerializeEnumerableSpecial
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
					keys.Add(new NormalMap(member.Name));
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
						val = new Property(type.GetProperty(text), this.obj, type);// TODO: set parent here, too
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
			Delegate eventDelegate=Method.CreateDelegateFromCode(eventInfo.EventHandlerType,code);
			return eventDelegate;
		}
		public ContainerMap(object obj,Type type)
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
			if (obj is IntegerStrategy)
			{
				isEqual = ((IntegerStrategy)obj).number == number;
			}
			else
			{
				isEqual = base.Equals(obj);
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
		public static void Write(string fileName, string text)
		{
			StreamWriter writer = new StreamWriter(fileName, false, Encoding.Default);
			writer.Write(text);
			writer.Close();
		}
		public static string Read(string fileName)
		{
			StreamReader reader = new StreamReader(fileName, Encoding.Default);
			string result = reader.ReadToEnd();
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

					Directory.CreateDirectory(testDirectory);
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
		public Extent(SourcePosition start, SourcePosition end)//, string fileName)
		{
			this.start = start;
			this.end = end;
		}
		public Extent(int startLine,int startColumn,int endLine,int endColumn):this(new SourcePosition(startLine,startColumn),new SourcePosition(endLine,endColumn))
		{
			//this.start=new SourcePosition(startLine,startColumn);
			//this.end=new SourcePosition(endLine,endColumn);

		}
		public Extent CreateExtent(int startLine,int startColumn,int endLine,int endColumn)//,string fileName) 
		{
			Extent extent=new Extent(startLine,startColumn,endLine,endColumn);//,fileName);
			if(!extents.ContainsKey(extent))
			{
				extents.Add(extent,extent);
			}
			return (Extent)extents[extent];
		}
	}

	//public class ParserException:ApplicationException
	//{
	//    public override string Message
	//    {
	//        get
	//        {
	//            return this.text+" in line "+position.Line+", column "+position.Column+".";
	//        }
	//    }
	//    private string text;
	//    private SourcePosition position;
	//    public ParserException(string text,SourcePosition position)
	//    {
	//        this.text=text;
	//        this.position=position;
	//    }
	//}
	public class LocalStrategy : MapStrategy
	{
		public Map Map
		{
			get
			{
				return map;
			}
		}
		public Map local;
		public static LocalStrategy singleton = new LocalStrategy();
		//public static NormalMap fileSystem;
		public override MapStrategy CopyImplementation()
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public void Replace(Map val)
		{
			this.map = val;
			this.Save();

		}
		// remove
		public static void Set(string text)
		{
			FileAccess.Write(Path, text);
			((LocalStrategy)LocalStrategy.singleton).Load();
		}
		// make this a property
		//public static FileSystem singleton;
		//static FileSystem()
		//{
		//    fileSystem = new NormalMap(singleton);
		//    fileSystem.Parent = Gac.singleton;
		//    fileSystem.Scope = Gac.singleton;
		//    //singleton = new FileSystem();
		//}
		public Map map;
		public static string Path
		{
			get
			{
				return System.IO.Path.Combine(Process.InstallationPath, "meta.meta");
			}
		}
		private LocalStrategy()
		{
			map = new NormalMap(this);
			Load();
			map.Parent = GacStrategy.Gac;
			map.Scope = GacStrategy.Gac;
			map.Parent = GacStrategy.Gac;
			map.Scope = GacStrategy.Gac;
		}
		private void Load()
		{
			//try
			//{
				this.map = Process.Current.Parse(Path);
			//}
			//catch (Exception e)
			//{
			//    Console.WriteLine(e.ToString());
			//}
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return map.Keys;
			}
		}
		public override Map Get(Map key)
		{
			return map[key];
		}
		public override void Set(Map key, Map val)
		{
			map[key] = val;
			Save();
		}
		public void Save()
		{
			string text = Meta.Serialize.MapValue(map, "").Trim(new char[] { '\n' });
			if (text == "\"\"")
			{
				text = "";
			}
			FileAccess.Write(Process.InstallationPath, text);
		}
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
				throw new ApplicationException("Unexpected token " + text[index] + " ,expected " + character);
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
		private bool Look(int lookAhead,char character)
		{
			return Look(lookAhead)==character;
		}
		private bool Look(char character)
		{
			return Look(0,character);
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
		private int indentationCount = -1;
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


		private bool Indentation()
		{
			string indentationString = "".PadLeft(indentationCount + 1, indentationChar);
			bool isIndentation;
			if (TryConsume(unixNewLine + indentationString) || TryConsume(windowsNewLine + indentationString))
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
		private bool Dedentation()
		{
			int indent = 0;
			while (Look(indent) == indentationChar)
			{
				indent++;
			}
			bool isDedentation;
			if (indent < indentationCount)
			{
				Consume(indentationChar);
				isDedentation = true;
				indentationCount--;
			}
			else
			{
				isDedentation = false;
			}
			return isDedentation;
		}
		private Map Expression()
		{
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
			return expression;
		}
		private Map Call(Map select)
		{
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
				call = new NormalMap();
				Map callCode = new NormalMap();
				callCode[CodeKeys.Callable] = select;
				callCode[CodeKeys.Argument] = argument;
				call[CodeKeys.Call] = callCode;
			}
			else
			{
				call = null;
			}
			EndExpression(extent, call);
			return call;
		}
		bool isStartOfFile = true;
		private void Whitespace()
		{
			while (TryConsume('\t') || TryConsume(' '))
			{
			}
		}
		private Map NormalProgram()
		{
			Extent extent = StartExpression();
			Map program;
			if (Indentation())
			{
				program = new NormalMap();
				int counter = 1;
				int defaultKey = 1;
				Map statements = new NormalMap();
				while (!Look(endOfFileChar))
				{
					Map statement = Function();
					if (statement == null)
					{
						statement = Statement(ref defaultKey);
					}
					statements[counter] = statement;
					counter++;
					NewLine();
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
						throw new MetaException("incorrect indentation",extent);
					}
				}
				program[CodeKeys.Program] = statements;
			}
			else
			{
				program=null;
			}
			EndExpression(extent,program);
			return program;
		}
		private Map EmptyProgram()
		{
			Extent extent = StartExpression();
			Map program;
			if (TryConsume(emptyMapChar))
			{
				program = new NormalMap();
				program[CodeKeys.Program] = new NormalMap();
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
			return TryConsume('\n') || TryConsume("\r\n");
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
		private void SameIndentation()
		{
			string sameIndentationString = "".PadLeft(indentationCount, indentationChar);
			TryConsume(sameIndentationString);
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
			return new Extent(Line, Column, 0, 0);//, "");
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
				Map literal = new NormalMap(Meta.Integer.ParseInteger(integerString));
				integer = new NormalMap();
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
				while (true) // factor this out
				{
					if (Look(stringChar))
					{
						int foundEscapeCharCount = 0;
						while (foundEscapeCharCount<escapeCharCount && Look(foundEscapeCharCount + 1,stringEscapeChar))
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
				Map literal = new NormalMap(realText);
				@string = new NormalMap();
				@string[CodeKeys.Literal] = literal;
			}
			else
			{
				@string = null;
			}
			EndExpression(extent, @string);
			return @string;
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
				lookup = new NormalMap();
				lookup[CodeKeys.Literal] = new NormalMap(lookupString);
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
				// support maps as keys cleanly
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
				select = new NormalMap();
				select[CodeKeys.Select] = keys;
			}
			else
			{
				select = null;
			}
			EndExpression(extent, select);
			return select;
		}
		private Map Select()
		{
			return Select(Keys());
		}
		private Map Keys()
		{
			Extent extent = StartExpression();
			Map lookups = new NormalMap();
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
					function = new NormalMap();
					function[CodeKeys.Key] = CreateDefaultKey(CodeKeys.Function);
					Map literal = new NormalMap();
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
				if (val == null)
				{
					SourcePosition position=new SourcePosition(Line, Column);
					throw new MetaException("Expected value of statement", new Extent(position,position));
				}
				key = CreateDefaultKey(new NormalMap((Integer)count));
				count++;
			}
			Map statement = new NormalMap();
			statement[CodeKeys.Key] = key;
			statement[CodeKeys.Value] = val;
			EndExpression(extent, statement);
			return statement;
		}
		private const char space=' ';
		private const char tab = '\t';
		private Map CreateDefaultKey(Map literal)
		{
			Map key = new NormalMap();
			Map firstKey = new NormalMap();
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
				int startPos=Math.Min(index,text.Length-1);
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
			if(val.Equals(Map.Empty))
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
				if (entry.Key.Equals(CodeKeys.Function))
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
			Map callable=code[CodeKeys.Callable];
			Map argument=code[CodeKeys.Argument];
			string text = Expression(callable, indentation);
			if (!(argument.ContainsKey(CodeKeys.Program) && argument[CodeKeys.Program].Count!=0))
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
			if (code.Extent != null && code.Extent.Start.Line == 57)
			{
			}
			if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Literal].Equals(CodeKeys.Function))
			{
				if (code[CodeKeys.Value][CodeKeys.Literal]==null)
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
					if (code.Extent.Start.Line>500)
					{
					}
					autoKeys++;
					if (value.ContainsKey(CodeKeys.Program) && value[CodeKeys.Program].Count!=0)
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
				string[] split=mapString.Split(Parser.stringChar);
				for(int i=1;i<split.Length;i++)
				{
					int matchLength=split[i].Length - split[i].TrimStart(Parser.stringEscapeChar).Length + 1;
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
				string[] lines=val.GetString().Split(new string[] {Parser.unixNewLine.ToString(),Parser.windowsNewLine},StringSplitOptions.None);
				text+=lines[0];
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
	public class GacStrategy : MapStrategy
	{
		public static readonly NormalMap Gac = new NormalMap(new GacStrategy());

		private Map cache = new NormalMap();
		public override MapStrategy CopyImplementation()
		{
			return this;
		}
		private bool LoadAssembly(string assemblyName)
		{
			Map key = new NormalMap(assemblyName);
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
					Map val = new NormalMap();
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
				List<Map> assemblies = Fusion.Assemblies;
				foreach (string dllPath in Directory.GetFiles(Process.InstallationPath, "*.dll"))
				{
					assemblies.Add(new NormalMap(Path.GetFileNameWithoutExtension(dllPath)));
				}
				foreach (string exePath in Directory.GetFiles(Process.InstallationPath, "*.exe"))
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
		protected Map cachedAssemblyInfo = new NormalMap();
		//static Gac()
		//{
		//    Gac gac = new Gac();
		//    gac.cache["net"] = SpecialMaps.Net;
		//    //gac.cache["net"] = NetStrategy.singleton;
		//    singleton = gac;
		//}
		//public static GacStrategy singleton=new GacStrategy();



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
					List<Map> assemblies = new List<Map>();
					IAssemblyEnum assemblyEnum = CreateGACEnum();
					IAssemblyName iname;

					assemblies.Add(new NormalMap("mscorlib"));
					while (GetNextAssembly(assemblyEnum, out iname) == 0)
					{
						try
						{
							string assemblyName = GetAssemblyName(iname);
							if (assemblyName != "Microsoft.mshtml")
							{
								assemblies.Add(new NormalMap(assemblyName));
							}
						}
						catch (Exception e)
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
			[DllImport("fusion.dll", SetLastError = true, PreserveSig = false)]
			static extern void CreateAssemblyEnum(out IAssemblyEnum pEnum, IntPtr pUnkReserved, IAssemblyName pName,
				ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved);
			private static String GetDisplayName(IAssemblyName name, ASM_DISPLAY_FLAGS which)
			{
				uint bufferSize = 255;
				StringBuilder buffer = new StringBuilder((int)bufferSize);
				name.GetDisplayName(buffer, ref bufferSize, which);
				return buffer.ToString();
			}
			private static String GetName(IAssemblyName name)
			{
				uint bufferSize = 255;
				StringBuilder buffer = new StringBuilder((int)bufferSize);
				name.GetName(ref bufferSize, buffer);
				return buffer.ToString();
			}
			private static Version GetVersion(IAssemblyName name)
			{
				uint major;
				uint minor;
				name.GetVersion(out major, out minor);
				return new Version((int)major >> 16, (int)major & 0xFFFF, (int)minor >> 16, (int)minor & 0xFFFF);
			}
			private static byte[] GetPublicKeyToken(IAssemblyName name)
			{
				byte[] result = new byte[8];
				uint bufferSize = 8;
				IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
				name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY_TOKEN, buffer, ref bufferSize);
				for (int i = 0; i < 8; i++)
					result[i] = Marshal.ReadByte(buffer, i);
				Marshal.FreeHGlobal(buffer);
				return result;
			}
			private static byte[] GetPublicKey(IAssemblyName name)
			{
				uint bufferSize = 512;
				IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
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
				IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
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
				int SetProperty(ASM_NAME PropertyId, IntPtr pvProperty, uint cbProperty);
				[PreserveSig]
				int GetProperty(ASM_NAME PropertyId, IntPtr pvProperty, ref uint pcbProperty);
				[PreserveSig]
				int Finalize();
				[PreserveSig]
				int GetDisplayName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szDisplayName,
					ref uint pccDisplayName, ASM_DISPLAY_FLAGS dwDisplayFlags);
				[PreserveSig]
				int BindToObject(ref Guid refIID, [MarshalAs(UnmanagedType.IUnknown)] object pUnkSink,
					 [MarshalAs(UnmanagedType.IUnknown)] object pUnkContext,
					 [MarshalAs(UnmanagedType.LPWStr)] string szCodeBase,
					 long llFlags, IntPtr pvReserved, uint cbReserved, out IntPtr ppv);
				[PreserveSig]
				int GetName(ref uint lpcwBuffer, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzName);
				[PreserveSig]
				int GetVersion(out uint pdwVersionHi, out uint pdwVersionLow);
				[PreserveSig]
				int IsEqual(IAssemblyName pName, ASM_CMP_FLAGS dwCmpFlags);
				[PreserveSig]
				int Clone(out IAssemblyName pName);
			}
			[ComImport, Guid("21b8916c-f28e-11d2-a473-00c04f8ef448"),
				InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			private interface IAssemblyEnum
			{
				[PreserveSig()]
				int GetNextAssembly(IntPtr pvReserved, out IAssemblyName ppName, uint dwFlags);
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
		private double integer;

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