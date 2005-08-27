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
using System.Collections;
using System.Collections.Specialized;
using System.CodeDom.Compiler;
using System.Xml;
using System.Runtime.InteropServices;
using System.Reflection;
using System.GAC;
using System.Text;
using System.Threading;
using Microsoft.CSharp;
using System.Windows.Forms;
using System.Globalization;
using antlr;
using antlr.collections;
using Meta.Parser;
using Meta.TestingFramework;
using System.Text.RegularExpressions;

namespace Meta
{

	public class CodeKeys
	{
		public readonly static IMap Literal=new NormalMap("literal");
		public readonly static IMap Run=new NormalMap("run");
		public readonly static IMap Call=new NormalMap("call");
		public readonly static IMap Function=new NormalMap("function");
		public readonly static IMap Argument=new NormalMap("argument");
		public static readonly IMap Select=new NormalMap("select");
		public static readonly IMap Search=new NormalMap("search");
		public static readonly IMap Key=new NormalMap("key");
		public static readonly IMap Program=new NormalMap("program");
		public static readonly IMap Delayed=new NormalMap("delayed");
		public static readonly IMap Lookup=new NormalMap("lookup");
		public static readonly IMap Value=new NormalMap("value");
	}
	public class SpecialKeys
	{
		public static readonly IMap Parent=new NormalMap("parent"); 
		public static readonly IMap Arg=new NormalMap("arg");
		public static readonly IMap This=new NormalMap("this");
	}
	public class IntegerKeys
	{
		public static readonly IMap Denominator=new NormalMap("denominator");
		public static readonly IMap Numerator=new NormalMap("numerator");
		public static readonly IMap Negative=new NormalMap("negative");
		public static readonly IMap EmptyMap=new NormalMap();
	}
	public abstract class Expression
	{
		public static BreakPoint BreakPoint
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
		private static BreakPoint breakPoint;

		public virtual bool Stop()
		{
			bool stop=false;
			if(BreakPoint!=null)
			{
				if(BreakPoint.Position.ContainedIn(Extent.Start,Extent.End))
				{
					stop=true;
				}
			}
			return stop;
		}
		public IMap Evaluate(IMap parent)
		{
			IMap result=EvaluateImplementation(parent);
			if(Stop())
			{
				Interpreter.CallDebug(result);
			}
			return result;
		}

		public abstract IMap EvaluateImplementation(IMap parent);
		Extent extent;
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
	public class Call: Expression
	{
		public override bool Stop()
		{
			return argument.Stop();
		}

		public override IMap EvaluateImplementation(IMap parent)
		{
			object function=callable.Evaluate(parent);
			if(function is ICallable)
			{
				return ((ICallable)function).Call(argument.Evaluate(parent));
			}
			throw new MetaException("Object to be called is not callable.",this.Extent);
		}
		public Call(IMap code)
		{
			this.callable=code[CodeKeys.Function].GetExpression();
			this.argument=code[CodeKeys.Argument].GetExpression();
		}
		public Expression argument;
		public Expression callable;
	}
	public class Delayed: Expression
	{
		public override bool Stop()
		{
			return false;
		}

		public readonly IMap delayed;
		public Delayed(IMap code)
		{
			this.delayed=code;
		}
		public override IMap EvaluateImplementation(IMap parent)
		{
			IMap result=delayed;
			result.Parent=parent;
			return result;
		}
	}
	public class ReverseException:ApplicationException
	{
	}
	public class Program: Expression
	{
		public override bool Stop()
		{
			bool stop=false;
			if(BreakPoint!=null)
			{
				if(BreakPoint.Position.Line==Extent.End.Line+1 && BreakPoint.Position.Column==1)
				{
					stop=true;
				}
			}
			return stop;
		}
		public override IMap EvaluateImplementation(IMap parent)
		{
			IMap local=new NormalMap();
			Evaluate(parent,ref local);
			return local;
		}
		public void Evaluate(IMap parent,ref IMap local)
		{
			local.Parent=parent;
			for(int i=0;i<statements.Count && i>=0;)
			{
				if(Interpreter.reverseDebug)
				{
					bool stopReverse=((Statement)statements[i]).Undo(); // Statement should have separate Stop() function
					if(stopReverse)
					{
						Interpreter.reverseDebug=false;
						continue;
					}
				}
				else
				{
					try
					{
						((Statement)statements[i]).Realize(ref local);
					}
					catch(ReverseException e)
					{
						int asdf=0;
					}	
				}
				if(Interpreter.reverseDebug)
				{
					i--;
				}
				else
				{
					i++;
				}
			}
		}
		public Program(IMap code)
		{
			object a=code.Array;
			foreach(IMap statement in code.Array)
			{
				this.statements.Add(new Statement(statement));
			}
		}
		public readonly ArrayList statements=new ArrayList();
	}
	public class BreakPoint
	{
		public BreakPoint(string fileName,Position position)
		{
			this.fileName=fileName;
			this.position=position;
		}
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		public Position Position
		{
			get
			{
				return position;
			}
		}

		private Position position;
		string fileName;
	}

	public abstract class Filter // TODO: rename, remove??
	{
		public abstract IMap Detect(string text);
	}
	public class Filters
	{
		public class DecimalFilter: Filter
		{
			public override IMap Detect(string text)
			{
				IMap result=null;
				int pointPos=text.IndexOf(".");
				if(pointPos!=-1)
				{
					if(text.IndexOf(".",pointPos+1)==-1)
					{
						Integer numerator=IntegerFilter.ParseInteger(text.Replace(".",""));
						if(numerator!=null)
						{
							Integer denominator=System.Convert.ToInt32(Math.Pow(10,text.Length-pointPos-1));
							result=new NormalMap();
							result[IntegerKeys.Numerator]=new NormalMap(numerator);
							result[IntegerKeys.Denominator]=new NormalMap(denominator);
						}
					}
				}
				return result;
			}
		}
		public class FractionFilter: Filter
		{
			public override IMap Detect(string text)
			{
				IMap result=null;
				int pointPos=text.IndexOf("/");
				if(pointPos!=-1)
				{
					if(text.IndexOf("/",pointPos+1)==-1)
					{
						Integer numerator=IntegerFilter.ParseInteger(text.Substring(0,text.Length-pointPos-2));
						if(numerator!=null)
						{
							Integer denominator=IntegerFilter.ParseInteger(text.Substring(pointPos+1,text.Length-pointPos-1));
							if(denominator!=null)
							{
								result=new NormalMap();
								result[IntegerKeys.Numerator]=new NormalMap(numerator);
								result[IntegerKeys.Denominator]=new NormalMap(denominator);
							}
						}
					}
				}
				return result;
			}
		}
		public class IntegerFilter: Filter 
		{
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
					if(text[0]=='-')
					{
						index++;
					}
					for(;index<text.Length;index++)
					{
						if(char.IsDigit(text[index]))
						{
							result=result*10+(text[index]-'0');
						}
						else
						{
							return null;
						}
					}
					if(text[0]=='-')
					{
						result=-result;
					}
				}
				return result;
			}
			public override IMap Detect(string text)
			{ 
				IMap recognized;
				Integer integer=ParseInteger(text);
				if(integer!=null)
				{
					recognized=new NormalMap(integer);
				}
				else
				{
					recognized=null;
				}
				return recognized;
			}
		}
		public class StringFilter:Filter
		{
			public override IMap Detect(string text)
			{
				return new NormalMap(text);
			}
		}
	}
	public class Literal: Expression
	{
		public static ArrayList recognitions=new ArrayList();
		static Literal()
		{
			foreach(Type recognition in typeof(Filters).GetNestedTypes())
			{
				recognitions.Add((Filter)recognition.GetConstructor(new Type[]{}).Invoke(new object[]{}));
			}
		}
		public override bool Stop()
		{
			return false;
		}

		public override IMap EvaluateImplementation(IMap parent)
		{
			if(literal.Equals(new NormalMap("testSubDir")))
			{
				int asdf=0;
			}
			return literal;
		}
		public Literal(IMap code)
		{
			this.literal=Filter((string)code.String);
		}
		public IMap literal=null;
		public static IMap Filter(string text)
		{
			foreach(Filter recognition in recognitions)
			{
				IMap recognized=recognition.Detect(text);
				if(recognized!=null)
				{
					return recognized;
				}
			}
			return null;
		}
	}
	public class Search: Expression
	{
		public Search(IMap code)
		{
			this.search=code.GetExpression();
		}
		public Expression search;
		public override IMap EvaluateImplementation(IMap parent)
		{
			IMap key=search.Evaluate(parent);
			if(key.Equals(new NormalMap("helloh")))
			{
				int asdf=0;
			}
			IMap selected=parent;
			while(!selected.ContainsKey(key))
			{
				selected=selected.Parent;
				if(selected==null)
				{
					throw new KeyNotFoundException(key,this.Extent);
				}
			}
			return selected[key];
		}
	}
	public class Select: Expression
	{
		public ArrayList keys=new ArrayList();
		public Expression firstKey;
		public Select(IMap code)
		{
			firstKey=((IMap)code.Array[0]).GetExpression();
			foreach(IMap key in code.Array.GetRange(1,code.Array.Count-1))
			{
				keys.Add(key.GetExpression());
			}
		}
		public override IMap EvaluateImplementation(IMap parent)
		{
			IMap selected=firstKey.Evaluate(parent);
			for(int i=0;i<keys.Count;i++)
			{
				IMap key=((Expression)keys[i]).Evaluate(parent);
				IMap selection=selected[key];
				if(selection==null)
				{
					object test=selected[key];
					throw new KeyDoesNotExistException(key,this.Extent,selected);
				}
				selected=selection;
			}
			return selected;
		}	}
	public class Statement
	{
		private IMap replaceValue;
		private IMap replaceMap;
		private IMap replaceKey;
		public bool Undo()
		{
			if(replaceMap!=null) // TODO: handle "this" specially
			{
				replaceMap[replaceKey]=replaceValue;
			}

			bool stopReverse=false;
			if(this.expression.Stop())
			{
				stopReverse=true;
			}
			else
			{
				foreach(Expression key in keys)
				{
					if(key.Stop())
					{
						stopReverse=true;
						break;
					}
				}
			}
			return stopReverse;
		}
		public void Realize(ref IMap parent)
		{
			IMap selected=parent;
			IMap key;
			
			if(searchFirst)
			{
				IMap firstKey=((Expression)keys[0]).Evaluate(parent); 
				if(firstKey.Equals(new NormalMap("instanceEventChanged")))
				{
					int asdf=0;
				}
				while(!selected.ContainsKey(firstKey))
				{
					selected=(selected).Parent;
					if(selected==null)
					{
						throw new KeyNotFoundException(firstKey,((Expression)keys[0]).Extent);
					}
				}
			}
			for(int i=0;i<keys.Count-1;i++)
			{
				key=((Expression)keys[i]).Evaluate(parent);
				IMap selection=selected[key];
				if(selection==null)
				{
					throw new KeyDoesNotExistException(key,((Expression)keys[i]).Extent,selected);
				}
				selected=selection;
			}
			IMap lastKey=((Expression)keys[keys.Count-1]).Evaluate(parent);
			IMap val=expression.Evaluate(parent);
			if(lastKey.Equals(SpecialKeys.This))
			{
				val.Parent=parent.Parent;
				parent=val;
			}
			else
			{
				if(selected.ContainsKey(lastKey))
				{
					replaceValue=selected[lastKey];
				}
				else
				{
					replaceValue=null;
				}
				replaceMap=selected;
				replaceKey=lastKey;

				selected[lastKey]=val;
			}
		}
		public Statement(IMap code) 
		{
			if(code.ContainsKey(CodeKeys.Search))
			{
				searchFirst=true;
			}
			foreach(IMap key in code[CodeKeys.Key].Array)
			{
				keys.Add(key.GetExpression());
			}
			this.expression=code[CodeKeys.Value].GetExpression();
		}
		public ArrayList keys=new ArrayList();
		public Expression expression;
		
		bool searchFirst=false;
	}
	public class Interpreter
	{

		private static object debugValue="";
		public static object DebugValue
		{
			get
			{
				return debugValue;
			}
		}
		public static event MethodInvoker DebugBreak;

		public static bool reverseDebug=false;
		public static void CallDebug(object stuff)
		{
			debugValue=stuff;
			if(DebugBreak!=null)
			{
				DebugBreak();
				Thread.CurrentThread.Suspend();
				if(reverseDebug)
				{
					throw new ReverseException();
				}
			}
		}

		public static IMap Merge(params IMap[] arkvlToMerge)
		{
			return MergeCollection(arkvlToMerge);
		}
		// TODO: integrate merging into the IMap, and specialise, I think this is still buggy, how far down do we want to merge, anyway, 
		// TODO: overwriting should be disallowed, must decide on exception handling first, though
		// or allow overwriting??, probably must be allowed, for default arguments
		// however, doesnt work as expected for integers, maybe
		// whhere does overwriting start, where to draw the line

		//ugly hack would be, dont merge if it is an integer, or a class or an object, or whatever
		public static IMap MergeCollection(ICollection collection)
		{
			IMap result=new NormalMap();
			foreach(IMap current in collection)
			{
				foreach(DictionaryEntry entry in current)
				{
					if(!(entry.Value is DotNetClass) && result.ContainsKey((IMap)entry.Key) // TODO: this is a mess
						 && !(result[(IMap)entry.Key] is DotNetClass))
					{
						result[(IMap)entry.Key]=Merge(result[(IMap)entry.Key],(IMap)entry.Value);
					}
					else
					{
						result[(IMap)entry.Key]=(IMap)entry.Value;
					}
				}
			}
			return result;
		}
		public static IMap Run(string fileName,IMap argument)
		{
			IMap program=Interpreter.Compile(fileName);
			return CallProgram(program,argument,GetPersistantMaps(fileName));
		}
		public static IMap GetPersistantMaps(string fileName)
		{
			DirectoryInfo directory=new DirectoryInfo(Path.GetDirectoryName(fileName));
			IMap root=new PersistantMap(directory);
			IMap current=root;
			while(true)
			{
				if(String.Compare(directory.FullName,Interpreter.LibraryPath.FullName,true)==0)
				{
					current.Parent=GAC.library;
					break;
				}
				current.Parent=new PersistantMap(directory.Parent);
				current=current.Parent;
			}
			return root;
		}
		public static IMap RunWithoutLibrary(string fileName,IMap argument)
		{
			IMap program=Compile(fileName);
			return CallProgram(program,argument,null);
		}
		public static IMap CallProgram(IMap program,IMap argument,IMap current)
		{
			IMap callable=new NormalMap();
			callable[CodeKeys.Run]=program;
			callable.Parent=current;
			return callable.Call(argument);
		}
		public static IMap Compile(string fileName)
		{
			return (new MetaTreeParser()).map(ParseToAst(fileName));
		}
		public static AST ParseToAst(string fileName) 
		{
			FileStream file=new FileStream(fileName,FileMode.Open);
			ExtentLexerSharedInputState sharedInputState = new ExtentLexerSharedInputState(file,fileName); 
			MetaLexer metaLexer = new MetaLexer(sharedInputState);
	
			metaLexer.setTokenObjectClass("MetaToken");
	
			MetaParser metaParser = new MetaParser(new IndentationStream(metaLexer));

			metaParser.setASTNodeClass("MetaAST");
			metaParser.map();
			AST ast=metaParser.getAST();
			file.Close();
			return ast;
		}
		private static void ExecuteInThread()
		{
			Interpreter.Run(executeFileName,new NormalMap());
		}
		private static string executeFileName="";
		public static void StartDebug(string fileName)
		{
			executeFileName=fileName;
			debugThread=new Thread(new ThreadStart(ExecuteInThread));
			debugThread.Start();
		}
		private static Thread debugThread;
		public static void ContinueDebug()
		{
			reverseDebug=false;
			debugThread.Resume();
		}
		public static void ReverseDebug()
		{
			reverseDebug=true;
			debugThread.Resume();
		}
		static Interpreter()
		{
			Assembly metaAssembly=Assembly.GetAssembly(typeof(IMap));

			installationPath=@"c:\_projectsupportmaterial\meta";
		}
		public static string installationPath;
		public static DirectoryInfo LibraryPath
		{
			get
			{
				return new DirectoryInfo(@"c:\_projectsupportmaterial\meta\library");
			}
		}
		public static ArrayList loadedAssemblies=new ArrayList();
	}
	public class MetaException:ApplicationException
	{
		protected string message="";
		public MetaException(Extent extent)
		{
			this.extent=extent;
		}
		public MetaException(string message,Extent extent)
		{
			this.extent=extent;
			this.message=message;
		}
		public MetaException(Exception exception,Extent extent):base(exception.Message,exception)
		{
			this.extent=extent;
		}
		Extent extent;
		public override string Message
		{
			get
			{
				return message+" In file "+extent.FileName+", line: "+extent.Start.Line+", column: "+extent.Start.Column+".";
			}
		}
	}
	public class MapException:ApplicationException
	{
		IMap map;
		public MapException(IMap map,string message):base(message)
		{
			this.map=map;
		}
	}
	public abstract class KeyException:MetaException
	{ 
		public KeyException(IMap key,Extent extent):base(extent)
		{
			message="Key ";
			if(key.IsString)
			{
				message+=key.String;
			}
			else
			{
				message+=DirectoryStrategy.SerializeKey(key);
			}
			if(this is KeyDoesNotExistException)
			{
				message+=" does not exist.";
			}
			else if(this is KeyNotFoundException)
			{
				message+=" not found.";
			}
		}
	}
	public class KeyNotFoundException:KeyException
	{
		public KeyNotFoundException(IMap key,Extent extent):base(key,extent)
		{
		}
	}
	public class KeyDoesNotExistException:KeyException
	{
		private object selected;
		public KeyDoesNotExistException(IMap key,Extent extent,object selected):base(key,extent)
		{
			this.selected=selected;
		}
	}
	public interface ICallable
	{
		IMap Call(IMap argument);
	}
	public abstract class IMap: ICallable, IEnumerable
	{
		public virtual bool IsInteger
		{
			get
			{
				return Integer!=null;
			}
		}
		public abstract Integer Integer
		{
			get;
		}
		public IMap Argument
		{
			get
			{
				return arg;
			}
			set
			{ 
				arg=value;
			}
		}
		IMap arg=null;
		public virtual bool IsString
		{
			get
			{
				return String!=null;
			}
		}
		public static string GetString(IMap map)
		{
			string text="";
			foreach(IMap key in map.Keys)
			{
				if(key.Integer!=null && map[key].Integer!=null)
				{
					try
					{
						text+=System.Convert.ToChar(map[key].Integer.Int);
					}
					catch
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}
			return text;
		}
		public virtual string String
		{
			get
			{
				return GetString(this);
			}
		}
		public virtual IMap Parent
		{
			get
			{
				return parent;
			}
			set
			{
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
		public virtual ArrayList Array
		{ 
			get
			{
				ArrayList array=new ArrayList();
				foreach(IMap key in this.Keys) // TODO: need to sort the keys, by integer?? or require that keys are already sorted
				{
					if(key.IsInteger)
					{
						array.Add(this[key]);
					}
				}
				return array;
			}
		}
		public abstract IMap this[IMap key] 
		{
			get;
			set;
		}
		public virtual IMap Call(IMap argument)
		{
			Argument=argument;
			Expression function=(Expression)this[CodeKeys.Run].GetExpression();
			IMap result;
			result=function.Evaluate(this);
			return result;
		}
		public abstract ArrayList Keys
		{
			get;
		}
		public abstract IMap Clone();
		public virtual Expression GetExpression()
		{
			Expression expression;
			if(this.ContainsKey(CodeKeys.Call))
			{
				expression=new Call(this[CodeKeys.Call]);
			}
			else if(this.ContainsKey(CodeKeys.Delayed))
			{ 
				expression=new Delayed(this[CodeKeys.Delayed]);
			}
			else if(this.ContainsKey(CodeKeys.Program))
			{
				expression=new Program(this[CodeKeys.Program]);
			}
			else if(this.ContainsKey(CodeKeys.Literal))
			{
				expression=new Literal(this[CodeKeys.Literal]);
			}
			else if(this.ContainsKey(CodeKeys.Search))
			{
				expression=new Search(this[CodeKeys.Search]);
			}
			else if(this.ContainsKey(CodeKeys.Select))
			{
				expression=new Select(this[CodeKeys.Select]);
			}
			else
			{
				throw new ApplicationException("Cannot compile non-code map.");
			}
			((Expression)expression).Extent=this.Extent;
			return expression;
		}
		public bool ContainsKey(IMap key)
		{
			bool containsKey;
			if(key.Equals(SpecialKeys.Arg))
			{
				containsKey=this.Argument!=null;
			}
			else if(key.Equals(SpecialKeys.Parent))
			{
				containsKey=this.Parent!=null;
			}
			else if(key.Equals(SpecialKeys.This))
			{
				containsKey=true;
			}
			else
			{
				containsKey=ContainsKeyImplementation(key);
			}
			return containsKey;
		}
		protected virtual bool ContainsKeyImplementation(IMap key)
		{
			return Keys.Contains(key);
		}
		public virtual IEnumerator GetEnumerator()
		{
			return new MapEnumerator(this);
		}
		public override int GetHashCode() 
		{
			int hash=0;
			foreach(IMap key in this.Keys)
			{
				unchecked
				{
					hash+=key.GetHashCode()*this[key].GetHashCode();
				}
			}
			return hash;
		}
		Extent extent;
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
		}		private IMap parent;
	}
	
	public abstract class StrategyMap: IMap, ISerializeSpecial
	{
		public override Integer Integer
		{
			get
			{
				return strategy.Integer;
			}
		}
		public override string String
		{
			get
			{
				return strategy.String;
			}
		}

		public override int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public override ArrayList Array
		{
			get
			{
				return strategy.Array;
			}
		}
		public override IMap this[IMap key] 
		{
			get
			{
				IMap result;
				if(key.Equals(SpecialKeys.Parent))
				{
					result=Parent;
				}
				else if(key.Equals(SpecialKeys.Arg))
				{
					result=Argument;
				}
				else if(key.Equals(SpecialKeys.This))
				{
					result=this;
				}
				else
				{
					result=strategy[key];
				}
				return result;
			}
			set
			{
				if(value!=null)
				{
					isHashCached=false;
					if(key.Equals(SpecialKeys.This))
					{
						this.strategy=((NormalMap)value).strategy.Clone();
					}
					else
					{
						IMap val;
						val=value.Clone();
						val.Parent=this;
						strategy[key]=val;
					}
				}
			}
		}
		public override IMap Call(IMap argument)
		{
			return strategy.Call(argument);
		}
		public override ArrayList Keys
		{
			get
			{
				return strategy.Keys;
			}
		}
//		protected abstract StrategyMap CloneImplementation();
		public override IMap Clone()
		{
			IMap clone=strategy.CloneMap();
			clone.Parent=Parent;
			clone.Extent=Extent;
			return clone;
		}
		protected override bool ContainsKeyImplementation(IMap key)
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
		public StrategyMap(MapStrategy strategy)
		{
			this.strategy=strategy;
			this.strategy.map=this;
		}
		public MapStrategy strategy;

		public void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
		{
			strategy.Serialize(indentation,functions,stringBuilder);
		}
	}
	public class NormalMap:StrategyMap
	{
//		protected override StrategyMap CloneImplementation()
//		{
//			return new NormalMap(strategy.Clone());
//		}

		public NormalMap(MapStrategy strategy):base(strategy) // TOOD: NormalMap should only accept Strategies that make sense for the normal map
		{
		}
		public NormalMap():this(new HybridDictionaryStrategy())
		{
		}
		public NormalMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public NormalMap(string namespaceName,Hashtable subNamespaces,ArrayList assemblies):this(new LazyNamespace(namespaceName,subNamespaces,assemblies))
		{
		}
		public NormalMap(string text):this(new StringStrategy(text))
		{
		}
	}
	public class PersistantMap:StrategyMap
	{
//		protected override StrategyMap CloneImplementation()
//		{
//			return new NormalMap(strategy.Clone());
//		}

		public PersistantMap(PersistantStrategy strategy):base(strategy)
		{
		}
		public PersistantMap(FileInfo file):this(new FileStrategy(file))
		{
		}
		public PersistantMap(DirectoryInfo directory):this(new DirectoryStrategy(directory))
		{
		}
	}
	public abstract class PersistantStrategy:MapStrategy
	{
	}
	public class DirectoryStrategy:PersistantStrategy
	{
		public static void SaveToFile(IMap meta,string path)
		{
			StreamWriter streamWriter=new StreamWriter(path);
			//streamWriter.Write(SerializeValue(meta));
			streamWriter.Write(SerializeValue(meta).Trim(new char[]{'\n'}));
			//streamWriter.Write(SerializeValue(meta).TrimStart(new char[]{'\n'}));
			streamWriter.Close();
		}
		public static string SerializeKey(IMap key)
		{
			return SerializeKey(key,"");
		}
		private static string SerializeKey(IMap key,string indentation)
		{
			string text;
			if(key.IsString)
			{
				text=SerializeStringKey(key,indentation);
			}
			else if(key.IsInteger)
			{
				text=SerializeIntegerKey(key);
			} 
			else
			{
				text=SerializeMapKey(key,indentation);
			}
			return text;			
		}
		public static string SerializeValue(IMap val)
		{
			return SerializeValue(val,"");
		}
		private static string SerializeValue(IMap val,string indentation)
		{
			string text;
			if(val.IsString)
			{
				text=SerializeStringValue(val,indentation);
			}
			else if(val.IsInteger)
			{
				text=SerializeIntegerValue(val);
			}
			else
			{
				text=SerializeMapValue(val,indentation);
			}
			return text;
		}
		private static string SerializeIntegerKey(IMap number)
		{
			return number.Integer.ToString();
		}
		private static string SerializeIntegerValue(IMap number)
		{
			return literalDelimiter+number.ToString()+literalDelimiter;
		}

		private static string SerializeStringKey(IMap key,string indentation)
		{
			string text;
			if(IsLiteralKey(key.String))
			{
				text=key.String;
			}
			else
			{
				text=leftBracket + SerializeStringValue(key,indentation) + rightBracket;
			}
			return text;
		}
//		private static string MakeEscapeSequence(string longestEscape,string text)
//		{
//
//		}
//		private static string FindLongestEscapeSequence(string text)
//		{
//			return longestEscape;
//		}

		private static string SerializeStringValue(IMap val,string indentation)
		{
			string text;
			if(Literal.Filter(val.String).IsString)
			{
				string longestEscape="\"";
				foreach(Match match in Regex.Matches(val.String,"(')?(\"')*\""))
				{
					if(match.ToString().Length>longestEscape.Length)
					{
						longestEscape=match.ToString();
					}
				}
				int delimiterLength=longestEscape.Length;
				if(val.String.StartsWith("\""))
				{
					if(delimiterLength%2==0)
					{
						delimiterLength++;
					}
				}
				else if(val.String.StartsWith("'"))
				{
					if(delimiterLength%2==1)
					{
						delimiterLength++;
					}
				}
				string startDelimiter="";
				for(int i=0;i<delimiterLength;i++)
				{
					if(i%2==0)
					{
						startDelimiter+="\"";
					}
					else
					{
						startDelimiter+="'";
					}
				}

				string endDelimiter=Helper.ReverseString(startDelimiter);
				text=startDelimiter+val.String+endDelimiter;
			}
			else
			{
				text=SerializeMapValue(val,indentation);
			}
			return text;
		}
		private static string SerializeMapKey(IMap map,string indentation)
		{
			return indentation + leftBracket + newLine + SerializeMapValue(map,indentation) + rightBracket;
		}
		private static string SerializeMapValue(IMap map,string indentation)
		{
			string text=newLine;
			foreach(DictionaryEntry entry in map)
			{
				text+=indentation + SerializeKey((IMap)entry.Key,indentation)	+ "=" + SerializeValue((IMap)entry.Value,indentation+'\t');
				if(!text.EndsWith(newLine))
				{
					text+=newLine;
				}
			}
			return text;
		}
		private const string leftBracket="[";
		private const string rightBracket="]";
		private const string newLine="\n";
		private const string literalDelimiter="\"";

		private static bool IsLiteralKey(string text)
		{
			return -1==text.IndexOfAny(new char[] {'@',' ','\t','\r','\n','=','.','/','\'','"','(',')','[',']','*',':','#','!'});
		}





		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public override IMap CloneMap()
		{
			return this.map; // TODO: completely buggy, I dont understand what CloneMap should be good for
		}
		private DirectoryInfo directory;
		public DirectoryStrategy(DirectoryInfo directory)
		{
			this.directory=directory;
			//fileSystemPath=Path.Combine(Interpreter.installationPath,"Library");
			ArrayList assemblies=new ArrayList();
			assemblyPath=Path.Combine(directory.FullName,"assembly");
//			assemblies=GlobalAssemblyCache.Assemblies;
			if(Directory.Exists(assemblyPath))
			{
				foreach(string dllPath in System.IO.Directory.GetFiles(assemblyPath,"*.dll"))
				{
					assemblies.Add(Assembly.LoadFrom(dllPath));
				}
				foreach(string exePath in System.IO.Directory.GetFiles(assemblyPath,"*.exe"))
				{
					assemblies.Add(Assembly.LoadFrom(exePath));
				}
			}
//			string cachedAssemblyPath=Path.Combine(Interpreter.installationPath,"cachedAssemblyInfo.meta");
//			if(File.Exists(cachedAssemblyPath))
//			{
//				cachedAssemblyInfo=Interpreter.RunWithoutLibrary(cachedAssemblyPath,new NormalMap());
//			}
			cache=LoadNamespaces(assemblies);
			//DirectoryStrategy.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
		}
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				foreach(DirectoryInfo subDirectory in directory.GetDirectories())
				{
					if(ValidName(subDirectory.Name))
					{
						keys.Add(new NormalMap(subDirectory.Name));
					}
				}
				foreach(FileInfo file in directory.GetFiles("*.meta"))
				{
					if(ValidName(file.Name))
					{
						keys.Add(new NormalMap(Path.GetFileNameWithoutExtension(file.FullName)));
					}
				}
				foreach(DictionaryEntry entry in cache)
				{
					if(!keys.Contains(entry.Key))
					{
						keys.Add(entry.Key);
					}
				}
				return keys;
			}
		}
//		public override ArrayList Keys
//		{
//			get
//			{
//				ArrayList keys=new ArrayList();
//				foreach(DirectoryInfo subDirectory in directory.GetDirectories())
//				{
//					if(ValidName(subDirectory.Name))
//					{
//						keys.Add(new NormalMap(subDirectory.Name));
//					}
//				}
//				foreach(FileInfo file in directory.GetFiles("*.meta"))
//				{
//					if(ValidName(file.Name))
//					{
//						keys.Add(new NormalMap(Path.GetFileNameWithoutExtension(file.FullName)));
//					}
//				}
//				return keys;
//			}
//		}
		private bool ValidName(string key)
		{
			return !key.StartsWith(".");
		}
		public override Integer Integer
		{
			get
			{
				return null; // TODO: is this really correct, move it up, no sense to put it here
			}
		}

		public override IMap this[IMap key]
		{
			// TODO: refactor
			get
			{
				IMap result;
				if(key.IsString)
				{
					if(ValidName(key.String))
					{
						string path=Path.Combine(directory.FullName,key.String);
						FileInfo file=new FileInfo(path+".meta");
						if(file.Exists)
						{
							result=new PersistantMap(file);
						}
						else
						{
							DirectoryInfo subDirectory=new DirectoryInfo(path);
							if(subDirectory.Exists)
							{
								result=new PersistantMap(subDirectory);
							}
							else
							{
								result=null;
							}
						}
					}
					else
					{
						result=null;
					}
				}
				else
				{
					result=null;
				}
				if(result==null)
				{
					if(cache.ContainsKey(key))
					{
						result=cache[key];
					}
				}
				return result;
			}
			set
			{
				int asdf=0;
				// TODO: implement
			}
		}
		
//			public override IMap this[IMap key]
//			{
//				get
//				{
//					if(cache.ContainsKey(key))
//					{
//						return cache[key];
//					}
//					else
//					{
//						return null;
//					}
//				}
//				set
//				{
//					throw new ApplicationException("Cannot set key "+key.ToString()+" in library.");
//				}
//			}
//			public override ArrayList Keys
//			{
//				get
//				{
//					return cache.Keys;
//				}
//			}
//			public override IMap Clone() // TODO: doesnt work correctly yet
//			{
//				return this;
//			}
//			public override int Count
//			{
//				get
//				{
//					return cache.Count;
//				}
//			}
//			protected override bool ContainsKeyImplementation(IMap key)
//			{
//				return cache.ContainsKey(key);
//			}
//			public override ArrayList Array
//			{
//				get
//				{
//					return new ArrayList();
//				}
//			}
			public static IMap LoadAssemblies(IEnumerable assemblies)
			{
				IMap root=new NormalMap();
				foreach(Assembly currentAssembly in assemblies)
				{
					foreach(Type type in currentAssembly.GetExportedTypes()) 
					{
						if(type.DeclaringType==null) 
						{
							IMap position=root;
							ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
							subPaths.RemoveAt(subPaths.Count-1);
							foreach(string subPath in subPaths) 
							{
								if(!position.ContainsKey(new NormalMap(subPath))) 
								{
									position[new NormalMap(subPath)]=new NormalMap();
								}
								position=position[new NormalMap(subPath)];
							}
							position[new NormalMap(type.Name)]=new DotNetClass(type);
						}
					}
					Interpreter.loadedAssemblies.Add(currentAssembly.Location);
				}
				return root;
			}
		private string assemblyPath;
//			private string fileSystemPath;


			private IMap cachedAssemblyInfo=new NormalMap();
			public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
			{ 
				ArrayList nameSpaces=new ArrayList();
				MapInfo cached=new MapInfo(cachedAssemblyInfo);
				if(cached.ContainsKey(assembly.Location))
				{
					MapInfo info=new MapInfo((IMap)cached[assembly.Location]);
					string timestamp=(string)info["timestamp"];
					if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
					{
						MapInfo namespaces=new MapInfo((IMap)info["namespaces"]);
						foreach(DictionaryEntry entry in namespaces)
						{
							nameSpaces.Add((string)entry.Value);
						}
						return nameSpaces;
					}
				}
				foreach(Type type in assembly.GetExportedTypes())
				{
					if(!nameSpaces.Contains(type.Namespace))
					{
						if(type.Namespace==null)
						{
							if(!nameSpaces.Contains(""))
							{
								nameSpaces.Add("");
							}
						}
						else
						{
							nameSpaces.Add(type.Namespace);
						}
					}
				}
				IMap cachedAssemblyInfoMap=new NormalMap();
				IMap nameSpace=new NormalMap(); 
				Integer counter=new Integer(1);
				foreach(string na in nameSpaces)
				{
					nameSpace[new NormalMap(counter)]=new NormalMap(na);
					counter++;
				}
				cachedAssemblyInfoMap[new NormalMap("namespaces")]=nameSpace;
				cachedAssemblyInfoMap[new NormalMap("timestamp")]=new NormalMap(File.GetLastWriteTime(assembly.Location).ToString());
				cachedAssemblyInfo[new NormalMap(assembly.Location)]=cachedAssemblyInfoMap;
				return nameSpaces;
			}
			// TODO: refactor this
			public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
			{
				NormalMap root=new NormalMap("",new Hashtable(),new ArrayList());
				foreach(Assembly assembly in assemblies)
				{
					ArrayList nameSpaces=NameSpaces(assembly);
					CachedAssembly cachedAssembly=new CachedAssembly(assembly);
					foreach(string nameSpace in nameSpaces)
					{
						if(nameSpace=="System")
						{
							int asdf=0;
						}
						LazyNamespace selected=(LazyNamespace)root.strategy; // TODO: this sucks quite a bit!!
						if(nameSpace=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.installationPath,"library")))
						{
							continue;
						}
						if(nameSpace!="")
						{
							foreach(string subString in nameSpace.Split('.'))
							{
								if(!selected.namespaces.ContainsKey(subString))
								{
									string fullName=selected.fullName;
									if(fullName!="")
									{
										fullName+=".";
									}
									fullName+=subString;
									selected.namespaces[subString]=new NormalMap(fullName,new Hashtable(),new ArrayList());
								}
								selected=(LazyNamespace)((NormalMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
							}
						}
						selected.AddAssembly(cachedAssembly);
					}
				}
				((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
				return root; // TODO: is this correct?
			}
//			public static IMap library=new GAC(); // TODO: is this the right way to do it??
			private IMap cache=new NormalMap();
//			public static string libraryPath="library"; 

	}
	public class FileStrategy:PersistantStrategy
	{
		public FileStrategy(FileInfo file)
		{
			this.file=file;
		}
		private FileInfo file;
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public override IMap CloneMap()
		{
			return this.map; // TODO: wrong, wrong, wrong
		}
//		IMap data;
		public override ArrayList Keys
		{
			get
			{
				return GetMap().Keys;
			}
		}
		public override Integer Integer // TODO: implement a default implementation in MapStrategy
		{
			get
			{
				return GetMap().Integer; // TODO: not entirely correct, this COULD be a number
			}
		}
		private IMap GetMap()
		{
			IMap data=Interpreter.Compile(this.file.FullName);
			data.Parent=this.map;
			return data;
		}
		private void SaveMap(IMap map)
		{
			DirectoryStrategy.SaveToFile(map,file.FullName);
		}
		public override IMap this[IMap key]
		{
			get
			{
				IMap data=GetMap();
				return data[key];
			}
			set
			{
				IMap data=GetMap();
				data[key]=value;
				SaveMap(data);
			}
		}
	}
	public class MetaLibrary // TODO: integrate into MetaFile
	{
		public object Load()
		{
			return Interpreter.Run(path,new NormalMap());
		}
		public MetaLibrary(string path)
		{
			this.path=path;
		}
		string path;
	}
	public class LazyNamespace: MapStrategy // TODO: integrate into Directory
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public override IMap CloneMap()
		{
			return new NormalMap(this);
		}

		public override MapStrategy Clone()
		{
			return this;
		}
		public override IMap this[IMap key]
		{
			get
			{
				if(cache==null)
				{
					Load();
				}
				return cache[key];
			}
			set
			{
				namespaces[key]=value; // TODO: this is totally wrong, remove when the file system stuff is sorted out
////				throw new ApplicationException("Cannot set key "+key.ToString()+" in .NET namespace.");
			}
		}
		public override ArrayList Keys
		{
			get
			{
				if(cache==null)
				{
					Load();
				}
				return cache.Keys;
			}
		}
		public override int Count
		{
			get
			{
				if(cache==null)
				{
					Load();
				}
				return cache.Count;
			}
		}
		public string fullName; // TODO: make private
		public void AddAssembly(CachedAssembly assembly)
		{
			cachedAssemblies.Add(assembly);
		}
		public ArrayList cachedAssemblies=new ArrayList(); // TODO: rename
		public Hashtable namespaces=new Hashtable(); // TODO: make this stuff private, initialize in constructor

		public LazyNamespace(string fullName,Hashtable subNamespaces,ArrayList assemblies)// TODO: actually use those arguments
		{
			this.fullName=fullName;
		}
		public void Load() // TODO: do this automatically, when the indexer is used, or any of the other functions depending on it
		{
			cache=new NormalMap();
			foreach(CachedAssembly cachedAssembly in cachedAssemblies)
			{
				cache=Interpreter.Merge(cache,cachedAssembly.NamespaceContents(fullName));
			}
			foreach(DictionaryEntry entry in namespaces)
			{
				cache[new NormalMap((string)entry.Key)]=(IMap)entry.Value;
			}
		}
		public IMap cache;
		public override bool ContainsKey(IMap key)
		{
			if(cache==null)
			{
				Load();
			}
			return cache.ContainsKey(key);
		}
	}
	public class CachedAssembly // TODO: integrate into Directory
	{  
		private Assembly assembly;
		public CachedAssembly(Assembly assembly)
		{
			this.assembly=assembly;
		}
		public IMap NamespaceContents(string nameSpace)
		{
			if(assemblyContent==null)
			{
				assemblyContent=GAC.LoadAssemblies(new object[] {assembly});
			}
			IMap selected=assemblyContent;
			if(nameSpace!="")
			{
				foreach(string subString in nameSpace.Split('.'))
				{
					selected=selected[new NormalMap(subString)];
				}
			}
			return selected;
		}			
		private IMap assemblyContent;
	}
	// TODO: refactor
	public class GAC: IMap// TODO: split into GAC and Directory
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override IMap this[IMap key]
		{
			get
			{
				if(cache.ContainsKey(key))
				{
					return cache[key];
				}
				else
				{
					return null;
				}
			}
			set
			{
				throw new ApplicationException("Cannot set key "+key.ToString()+" in library.");
			}
		}
		public override ArrayList Keys
		{
			get
			{
				return cache.Keys;
			}
		}
		public override IMap Clone() // TODO: doesnt work correctly yet
		{
			return this;
		}
		public override int Count
		{
			get
			{
				return cache.Count;
			}
		}
		protected override bool ContainsKeyImplementation(IMap key)
		{
			return cache.ContainsKey(key);
		}
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public static IMap LoadAssemblies(IEnumerable assemblies)
		{
			IMap root=new NormalMap();
			foreach(Assembly currentAssembly in assemblies)
			{
				foreach(Type type in currentAssembly.GetExportedTypes()) 
				{
					if(type.DeclaringType==null) 
					{
						IMap position=root;
						ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
						subPaths.RemoveAt(subPaths.Count-1);
						foreach(string subPath in subPaths) 
						{
							if(!position.ContainsKey(new NormalMap(subPath))) 
							{
								position[new NormalMap(subPath)]=new NormalMap();
							}
							position=position[new NormalMap(subPath)];
						}
						position[new NormalMap(type.Name)]=new DotNetClass(type);
					}
				}
				Interpreter.loadedAssemblies.Add(currentAssembly.Location);
			}
			return root;
		}
		private string fileSystemPath;

		public GAC()
		{
			fileSystemPath=Path.Combine(Interpreter.installationPath,"Library"); // TODO: has to be renamed to??? root, maybe, or just Meta, installation will look different anyway
			ArrayList assemblies=new ArrayList();
			libraryPath=Path.Combine(Interpreter.installationPath,"library");
			assemblies=GlobalAssemblyCache.Assemblies;
//			foreach(string dll in System.IO.Directory.GetFiles(libraryPath,"*.dll"))
//			{
//				assemblies.Add(Assembly.LoadFrom(dll));
//			}
//			foreach(string exe in System.IO.Directory.GetFiles(libraryPath,"*.exe"))
//			{
//				assemblies.Add(Assembly.LoadFrom(exe));
//			}
			string cachedAssemblyPath=Path.Combine(Interpreter.installationPath,"cachedAssemblyInfo.meta");
			if(File.Exists(cachedAssemblyPath))
			{
				cachedAssemblyInfo=Interpreter.RunWithoutLibrary(cachedAssemblyPath,new NormalMap());
			}
		
			cache=LoadNamespaces(assemblies);
			DirectoryStrategy.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
		}
		private IMap cachedAssemblyInfo=new NormalMap();
		public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
		{ 
			ArrayList nameSpaces=new ArrayList();
			MapInfo cached=new MapInfo(cachedAssemblyInfo);
			if(cached.ContainsKey(assembly.Location))
			{
				MapInfo info=new MapInfo((IMap)cached[assembly.Location]);
				string timestamp=(string)info["timestamp"];
				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
				{
					MapInfo namespaces=new MapInfo((IMap)info["namespaces"]);
					foreach(DictionaryEntry entry in namespaces)
					{
						nameSpaces.Add((string)entry.Value);
					}
					return nameSpaces;
				}
			}
			foreach(Type type in assembly.GetExportedTypes())
			{
				if(!nameSpaces.Contains(type.Namespace))
				{
					if(type.Namespace==null)
					{
						if(!nameSpaces.Contains(""))
						{
							nameSpaces.Add("");
						}
					}
					else
					{
						nameSpaces.Add(type.Namespace);
					}
				}
			}
			IMap cachedAssemblyInfoMap=new NormalMap();
			IMap nameSpace=new NormalMap(); 
			Integer counter=new Integer(1);
			foreach(string na in nameSpaces)
			{
				nameSpace[new NormalMap(counter)]=new NormalMap(na);
				counter++;
			}
			cachedAssemblyInfoMap[new NormalMap("namespaces")]=nameSpace;
			cachedAssemblyInfoMap[new NormalMap("timestamp")]=new NormalMap(File.GetLastWriteTime(assembly.Location).ToString());
			cachedAssemblyInfo[new NormalMap(assembly.Location)]=cachedAssemblyInfoMap;
			return nameSpaces;
		}
		// TODO: refactor this
		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
		{
			NormalMap root=new NormalMap("",new Hashtable(),new ArrayList());
			foreach(Assembly assembly in assemblies)
			{
				ArrayList nameSpaces=NameSpaces(assembly);
				CachedAssembly cachedAssembly=new CachedAssembly(assembly);
				foreach(string nameSpace in nameSpaces)
				{
					if(nameSpace=="System")
					{
						int asdf=0;
					}
					LazyNamespace selected=(LazyNamespace)root.strategy; // TODO: this sucks quite a bit!!
					if(nameSpace=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.installationPath,"library")))
					{
						continue;
					}
					if(nameSpace!="")
					{
						foreach(string subString in nameSpace.Split('.'))
						{
							if(!selected.namespaces.ContainsKey(subString))
							{
								string fullName=selected.fullName;
								if(fullName!="")
								{
									fullName+=".";
								}
								fullName+=subString;
								selected.namespaces[subString]=new NormalMap(fullName,new Hashtable(),new ArrayList());
							}
							selected=(LazyNamespace)((NormalMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
						}
					}
					selected.AddAssembly(cachedAssembly);
				}
			}
			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
			return root; // TODO: is this correct?
		}
		public static IMap library=new GAC(); // TODO: is this the right way to do it??
		private IMap cache=new NormalMap();
		public static string libraryPath="library"; 
	}
//	public class GAC: IMap// TODO: split into GAC and Directory
//	{
//		public override Integer Integer
//		{
//			get
//			{
//				return null;
//			}
//		}
//
//		public override IMap this[IMap key]
//		{
//			get
//			{
//				if(cache.ContainsKey(key))
//				{
//					return cache[key];
//				}
//				else
//				{
//					return null;
//				}
//			}
//			set
//			{
//				throw new ApplicationException("Cannot set key "+key.ToString()+" in library.");
//			}
//		}
//		public override ArrayList Keys
//		{
//			get
//			{
//				return cache.Keys;
//			}
//		}
//		public override IMap Clone() // TODO: doesnt work correctly yet
//		{
//			return this;
//		}
//		public override int Count
//		{
//			get
//			{
//				return cache.Count;
//			}
//		}
//		protected override bool ContainsKeyImplementation(IMap key)
//		{
//			return cache.ContainsKey(key);
//		}
//		public override ArrayList Array
//		{
//			get
//			{
//				return new ArrayList();
//			}
//		}
//		public static IMap LoadAssemblies(IEnumerable assemblies)
//		{
//			IMap root=new NormalMap();
//			foreach(Assembly currentAssembly in assemblies)
//			{
//				foreach(Type type in currentAssembly.GetExportedTypes()) 
//				{
//					if(type.DeclaringType==null) 
//					{
//						IMap position=root;
//						ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
//						subPaths.RemoveAt(subPaths.Count-1);
//						foreach(string subPath in subPaths) 
//						{
//							if(!position.ContainsKey(new NormalMap(subPath))) 
//							{
//								position[new NormalMap(subPath)]=new NormalMap();
//							}
//							position=position[new NormalMap(subPath)];
//						}
//						position[new NormalMap(type.Name)]=new DotNetClass(type);
//					}
//				}
//				Interpreter.loadedAssemblies.Add(currentAssembly.Location);
//			}
//			return root;
//		}
//		private string fileSystemPath;
//
//		public GAC()
//		{
//			fileSystemPath=Path.Combine(Interpreter.installationPath,"Library"); // TODO: has to be renamed to??? root, maybe, or just Meta, installation will look different anyway
//			ArrayList assemblies=new ArrayList();
//			libraryPath=Path.Combine(Interpreter.installationPath,"library");
//			assemblies=GlobalAssemblyCache.Assemblies;
//			foreach(string dll in System.IO.Directory.GetFiles(libraryPath,"*.dll"))
//			{
//				assemblies.Add(Assembly.LoadFrom(dll));
//			}
//			foreach(string exe in System.IO.Directory.GetFiles(libraryPath,"*.exe"))
//			{
//				assemblies.Add(Assembly.LoadFrom(exe));
//			}
//			string cachedAssemblyPath=Path.Combine(Interpreter.installationPath,"cachedAssemblyInfo.meta");
//			if(File.Exists(cachedAssemblyPath))
//			{
//				cachedAssemblyInfo=Interpreter.RunWithoutLibrary(cachedAssemblyPath,new NormalMap());
//			}
//		
//			cache=LoadNamespaces(assemblies);
//			DirectoryStrategy.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
//		}
//		private IMap cachedAssemblyInfo=new NormalMap();
//		public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
//		{ 
//			ArrayList nameSpaces=new ArrayList();
//			MapInfo cached=new MapInfo(cachedAssemblyInfo);
//			if(cached.ContainsKey(assembly.Location))
//			{
//				MapInfo info=new MapInfo((IMap)cached[assembly.Location]);
//				string timestamp=(string)info["timestamp"];
//				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
//				{
//					MapInfo namespaces=new MapInfo((IMap)info["namespaces"]);
//					foreach(DictionaryEntry entry in namespaces)
//					{
//						nameSpaces.Add((string)entry.Value);
//					}
//					return nameSpaces;
//				}
//			}
//			foreach(Type type in assembly.GetExportedTypes())
//			{
//				if(!nameSpaces.Contains(type.Namespace))
//				{
//					if(type.Namespace==null)
//					{
//						if(!nameSpaces.Contains(""))
//						{
//							nameSpaces.Add("");
//						}
//					}
//					else
//					{
//						nameSpaces.Add(type.Namespace);
//					}
//				}
//			}
//			IMap cachedAssemblyInfoMap=new NormalMap();
//			IMap nameSpace=new NormalMap(); 
//			Integer counter=new Integer(1);
//			foreach(string na in nameSpaces)
//			{
//				nameSpace[new NormalMap(counter)]=new NormalMap(na);
//				counter++;
//			}
//			cachedAssemblyInfoMap[new NormalMap("namespaces")]=nameSpace;
//			cachedAssemblyInfoMap[new NormalMap("timestamp")]=new NormalMap(File.GetLastWriteTime(assembly.Location).ToString());
//			cachedAssemblyInfo[new NormalMap(assembly.Location)]=cachedAssemblyInfoMap;
//			return nameSpaces;
//		}
//		// TODO: refactor this
//		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
//		{
//			NormalMap root=new NormalMap("",new Hashtable(),new ArrayList());
//			foreach(Assembly assembly in assemblies)
//			{
//				ArrayList nameSpaces=NameSpaces(assembly);
//				CachedAssembly cachedAssembly=new CachedAssembly(assembly);
//				foreach(string nameSpace in nameSpaces)
//				{
//					if(nameSpace=="System")
//					{
//						int asdf=0;
//					}
//					LazyNamespace selected=(LazyNamespace)root.strategy; // TODO: this sucks quite a bit!!
//					if(nameSpace=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.installationPath,"library")))
//					{
//						continue;
//					}
//					if(nameSpace!="")
//					{
//						foreach(string subString in nameSpace.Split('.'))
//						{
//							if(!selected.namespaces.ContainsKey(subString))
//							{
//								string fullName=selected.fullName;
//								if(fullName!="")
//								{
//									fullName+=".";
//								}
//								fullName+=subString;
//								selected.namespaces[subString]=new NormalMap(fullName,new Hashtable(),new ArrayList());
//							}
//							selected=(LazyNamespace)((NormalMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
//						}
//					}
//					selected.AddAssembly(cachedAssembly);
//				}
//			}
//			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
//			return root; // TODO: is this correct?
//		}
//		public static IMap library=new GAC(); // TODO: is this the right way to do it??
//		private IMap cache=new NormalMap();
//		public static string libraryPath="library"; 
//	}
	public class Convert
	{
		static Convert()
		{
			foreach(Type conversionType in typeof(ToMetaConversions).GetNestedTypes())
			{
				ToMeta conversion=((ToMeta)conversionType.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				toMeta[conversion.source]=conversion;
			}
			foreach(Type conversionType in typeof(ToDotNetConversions).GetNestedTypes())
			{
				ToDotNet conversion=(ToDotNet)conversionType.GetConstructor(new Type[]{}).Invoke(new object[]{});
				if(!toDotNet.ContainsKey(conversion.target))
				{
					toDotNet[conversion.target]=new Hashtable();
				}
				((Hashtable)toDotNet[conversion.target])[conversion.source]=conversion;
			}
		}
		public static object ToDotNet(IMap meta,Type target,out bool isConverted)
		{
			if(target.IsSubclassOf(typeof(Enum)) && meta.IsInteger)
			{ 
				isConverted=true;
				return Enum.ToObject(target,meta.Integer.Int);
			}
			Hashtable conversions=(Hashtable)toDotNet[target];
			if(conversions!=null)
			{
				ToDotNet conversion=(ToDotNet)conversions[meta.GetType()];
				if(conversion!=null)
				{
					return conversion.Convert(meta,out isConverted);
				}
			}
			isConverted=false;
			return null;
		}
		// TODO: maybe refactor with above
		public static object ToDotNet(IMap meta,Type target)
		{ 
			if(target==typeof(System.Int32))
			{
				int asdf=0;
			}
			object result=meta;
			if(toDotNet.ContainsKey(target))
			{
				Hashtable conversions=(Hashtable)toDotNet[target];
				if(conversions.ContainsKey(meta.GetType()))
				{
					ToDotNet conversion=(ToDotNet)conversions[meta.GetType()];
					bool isConverted;
					object converted= conversion.Convert(meta,out isConverted); // TODO: Why ignore isConverted here?, Should really loop through all the possibilities -> no not necessary here, type determines conversion
					if(isConverted)
					{
						result=converted;
					}
				}
			}
			return result;
		}
		public static IMap ToMeta(object oDotNet)
		{ 
			IMap meta;
			if(oDotNet==null)
			{
				meta=null;
			}
			else if(oDotNet.GetType().IsSubclassOf(typeof(Enum)))
			{
				meta=new NormalMap(new Integer((int)System.Convert.ToInt32((Enum)oDotNet)));
			}
			else
			{
				ToMeta conversion=(ToMeta)toMeta[oDotNet.GetType()];
				if(conversion==null)
				{
					if(oDotNet is IMap)
					//if(oDotNet is IMap || IsInteger(oDotNet))
					{
						meta=(IMap)oDotNet;
					}
					else
					{
						meta=new DotNetObject(oDotNet);
					}
				}
				else
				{
					meta=conversion.Convert(oDotNet);
				}
			}
			return meta;
		}
		public static object ToDotNet(IMap meta) 
		{
			if(meta.IsInteger)
			{
				return meta.Integer.Int;
			}
			else if(meta.IsString)
			//else if(meta is IMap && meta.IsString)
			{
				return meta.String;
			}
			else
			{
				return meta;
			}
		}
		private static Hashtable toDotNet=new Hashtable();
		private static Hashtable toMeta=new Hashtable();
	}
	public abstract class ToMeta
	{ 
		public Type source;
		public abstract IMap Convert(object obj);
	}
	public abstract class ToDotNet
	{
		public Type source;
		public Type target;
		public abstract object Convert(IMap obj,out bool converted);
	}
	abstract class ToMetaConversions // TODO: refactor??, no I think these work just fine
	{
		public class ConvertStringToMap: ToMeta
		{
			public ConvertStringToMap()  
			{
				this.source=typeof(string);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap((string)toConvert);
			}
		}
		public class ConvertBoolToInteger: ToMeta
		{
			public ConvertBoolToInteger()
			{
				this.source=typeof(bool);
			}
			public override IMap Convert(object toConvert)
			{
				return (bool)toConvert? new NormalMap(1): new NormalMap(0);
			}

		}
		public class ConvertByteToInteger: ToMeta
		{
			public ConvertByteToInteger()
			{
				this.source=typeof(Byte);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Byte)toConvert));
			}
		}
		public class ConvertSByteToInteger: ToMeta
		{
			public ConvertSByteToInteger()
			{
				this.source=typeof(SByte);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((SByte)toConvert));
			}
		}
		public class ConvertCharToInteger: ToMeta
		{
			public ConvertCharToInteger()
			{
				this.source=typeof(Char);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Char)toConvert));
			}
		}
		public class ConvertInt32ToInteger: ToMeta
		{
			public ConvertInt32ToInteger()
			{
				this.source=typeof(Int32);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Int32)toConvert));
			}
		}
		public class ConvertUInt32ToInteger: ToMeta
		{
			public ConvertUInt32ToInteger()
			{
				this.source=typeof(UInt32);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((UInt32)toConvert));
			}
		}
		public class ConvertInt64ToInteger: ToMeta
		{
			public ConvertInt64ToInteger()
			{
				this.source=typeof(Int64);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Int64)toConvert));
			}
		}
		public class ConvertUInt64ToInteger: ToMeta
		{
			public ConvertUInt64ToInteger()
			{
				this.source=typeof(UInt64);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Int64)(UInt64)toConvert));
			}
		}
		public class ConvertInt16ToInteger: ToMeta
		{
			public ConvertInt16ToInteger()
			{
				this.source=typeof(Int16);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((Int16)toConvert));
			}
		}
		public class ConvertUInt16ToInteger: ToMeta // TODO: get rid of Convert-prefix
		{
			public ConvertUInt16ToInteger()
			{
				this.source=typeof(UInt16);
			}
			public override IMap Convert(object toConvert)
			{
				return new NormalMap(new Integer((UInt16)toConvert));
			}
		}
	}
	// TODO: refactor this whole mess, doesnt work very well the way it is now, because there is only one Meta-type
	// target type determines everything, methods must be sorted by preference
	// additional preferences between types, maybe, maybe prefer smaller types to larger types
	// must test if actual conversion is possible, not just whether some types match, has never worked all that well anyway
	abstract class ToDotNetConversions // TODO: make this a single function??? somehow combine, old method doesnt work, put conversion methods right here??, consolidate them
	{
		public class IntegerToByte: ToDotNet
		{
			public IntegerToByte()
			{
				this.source=typeof(NormalMap); // TODO: this isnt quite accurate, should be able to convert every IMap
				this.target=typeof(Byte);
			}
			public override object Convert(IMap toConvert, out bool isConverted) // TODO: rename toConvert to meta
			{
				isConverted=true;
				return System.Convert.ToByte(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToBool: ToDotNet
		{
			public IntegerToBool()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(bool);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				object result;
				int i=toConvert.Integer.Int;
				if(i==0)
				{
					isConverted=true;
					result=false;
				}
				else if(i==1)
				{
					isConverted=true;
					result=true;
				}
				else
				{
					isConverted=false;
					result=null;
				}
				return result;
			}
		}
		public class IntegerToSByte: ToDotNet
		{
			public IntegerToSByte()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(SByte);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToSByte(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToChar: ToDotNet
		{
			public IntegerToChar()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(Char);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToChar(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToInt32: ToDotNet
		{
			public IntegerToInt32()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(Int32);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				if(toConvert.Integer!=null)
				{
					isConverted=true;
					return System.Convert.ToInt32(toConvert.Integer.LongValue());
				}
				else
				{
					isConverted=false;
					return null;
				}
			}
		}
		public class IntegerToUInt32: ToDotNet
		{
			public IntegerToUInt32()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(UInt32);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				if(toConvert.Integer!=null)
				{
					isConverted=true;
					return System.Convert.ToUInt32(toConvert.Integer.LongValue());
				}
				else
				{
					isConverted=false;
					return null;
				}
			}
		}
		public class IntegerToInt64: ToDotNet
		{
			public IntegerToInt64()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(Int64);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToInt64(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToUInt64: ToDotNet
		{
			public IntegerToUInt64()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(UInt64);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToUInt64(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToInt16: ToDotNet
		{
			public IntegerToInt16()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(Int16);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToInt16(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToUInt16: ToDotNet
		{
			public IntegerToUInt16()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(UInt16);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToUInt16(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToDecimal: ToDotNet
		{
			public IntegerToDecimal()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(decimal);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return (decimal)(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToDouble: ToDotNet
		{
			public IntegerToDouble()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(double);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return (double)(toConvert.Integer.LongValue());
			}
		}
		public class IntegerToFloat: ToDotNet
		{
			public IntegerToFloat()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(float);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				isConverted=true;
				return (float)(toConvert.Integer.LongValue());
			}
		}
		public class MapToString: ToDotNet
		{
			public MapToString()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(string);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				if(toConvert.IsString)
				{
					isConverted=true;
					return toConvert.String;
				}
				else
				{
					isConverted=false;
					return null;
				}
			}
		}
		public class FractionToDecimal: ToDotNet
		{
			public FractionToDecimal()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(decimal); 
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				IMap map=toConvert;
				if(map[new NormalMap("numerator")].IsInteger && map[new NormalMap("denominator")].IsInteger)
				//if(Helper.IsInteger(map[new NormalMap("numerator")]) && Helper.IsInteger(map[new NormalMap("denominator")]))
				{
					isConverted=true;
					return ((decimal)(map[new NormalMap("numerator")]).Integer.LongValue())/((decimal)(map[new NormalMap("denominator")]).Integer.LongValue());
				}
				else
				{
					isConverted=false;
					return null;
				}
			}

		}
		public class FractionToDouble: ToDotNet
		{
			public FractionToDouble()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(double);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				IMap map=toConvert;
				if(map[new NormalMap("numerator")].IsInteger && map[new NormalMap("denominator")].IsInteger)
				//if(Helper.IsInteger(map[new NormalMap("numerator")]) && Helper.IsInteger(map[new NormalMap("denominator")]))
				{
					isConverted=true;
					return ((double)(map[new NormalMap("numerator")]).Integer.LongValue())/((double)(map[new NormalMap("denominator")]).Integer.LongValue());
				}
				else
				{
					isConverted=false;
					return null;
				}
			}

		}
		public class FractionToFloat: ToDotNet
		{
			public FractionToFloat()
			{
				this.source=typeof(NormalMap);
				this.target=typeof(float);
			}
			public override object Convert(IMap toConvert, out bool isConverted)
			{
				IMap map=toConvert;
				if(map[new NormalMap("numerator")].IsInteger && map[new NormalMap("denominator")].IsInteger)
				{
					isConverted=true;
					return ((float)(map[new NormalMap("numerator")]).Integer.LongValue())/((float)(map[new NormalMap("denominator")]).Integer.LongValue());
				}
				else
				{
					isConverted=false;
					return null;
				}
			}
		}
	}
	public class MapInfoEnumerator: IEnumerator
	{
		private MapInfo MapInfo;
		public MapInfoEnumerator(MapInfo MapInfo)
		{
			this.MapInfo=MapInfo;
		}
		public object Current
		{
			get
			{
				return new DictionaryEntry(MapInfo.Keys[index],MapInfo[MapInfo.Keys[index]]);
			}
		}
		public bool MoveNext()
		{
			index++;
			return index<MapInfo.Count;
		}
		public void Reset()
		{
			index=-1;
		}
		private int index=-1;
	}
	public class MapInfo
	{ 
		private IMap map;
		public MapInfo(IMap map)
		{
			this.map=map;
		}

		public MapInfo()
		{
			this.map=new NormalMap();
		}
		public bool ContainsKey(object key)
		{
			return map.ContainsKey(Convert.ToMeta(key));
		}

		public object this[object key]
		{
			get
			{
				return Convert.ToDotNet(map[Convert.ToMeta(key)]);
			}
			set
			{
				this.map[Convert.ToMeta(key)]=Convert.ToMeta(value);
			}
		}
		public IMap Parent
		{
			get
			{
				return (IMap)Convert.ToMeta(map.Parent);
			}
			set
			{
				map.Parent=(IMap)Convert.ToDotNet(value);
			}
		}
		public ArrayList Array
		{
			get
			{
				return ConvertToMeta(map.Array);
			}
		}
		private ArrayList ConvertToMeta(ArrayList list)
		{
			ArrayList result=new ArrayList();
			foreach(IMap obj in list)
			{
				result.Add(Convert.ToDotNet(obj));
			}
			return result;
		}
		public ArrayList Keys
		{
			get
			{
				return ConvertToMeta(map.Keys);
			}
		}
		public int Count
		{
			get
			{
				return map.Count;
			}
		}
		public IEnumerator GetEnumerator()
		{
			return new MapInfoEnumerator(this);
		}
	}
	public class MapEnumerator: IEnumerator
	{
		private IMap map; 
		public MapEnumerator(IMap map)
		{
			this.map=map;
		}
		public object Current
		{
			get
			{
				return new DictionaryEntry(map.Keys[index],map[(IMap)map.Keys[index]]);
			}
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
	public class DotNetMethod: IMap,ICallable
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override IMap Clone()
		{
			return new DotNetMethod(this.name,this.target,this.targetType);
		}
		public override ArrayList Keys
		{
			get
			{
				return new ArrayList();
			}
		}
		public override IMap this[IMap key]
		{
			get
			{
				return null;
			}
			set
			{
				throw new ApplicationException("Cannot set key in DotNetMethod");
			}
		}




//		public static object AssignCollection(IMap map,object collection,out bool isSuccess) // TODO: of somewhat doubtful value, only for control and form initialization, should be removed, I think, need to write more extensive wrappers around the .NET libraries anyway
//		{ 
//			if(map.Array.Count==0)
//			{
//				isSuccess=false;
//				return null;
//			}
//			Type targetType=collection.GetType();
//			MethodInfo add=targetType.GetMethod("Add",new Type[]{map.Array[0].GetType()});
//			if(add!=null)
//			{
//				foreach(object entry in map.Array)
//				{ 
//					// TODO: combine this with Library function "Init"
//					add.Invoke(collection,new object[]{entry});//  call add from above!
//				}
//				isSuccess=true;
//			}
//			else
//			{
//				isSuccess=false;
//			}
//			return collection;
//		}
		// TODO: refactor
		public static object ConvertParameter(IMap meta,Type parameter,out bool isConverted)
		{
			isConverted=true;
			if(parameter.IsAssignableFrom(meta.GetType()))
			{
				return meta;
			}
			else if((parameter.IsSubclassOf(typeof(Delegate))
				||parameter.Equals(typeof(Delegate))) && (meta is IMap))
			{
				MethodInfo invoke=parameter.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate function=CreateDelegateFromCode(parameter,invoke,meta);
				return function;
			}
			else if(parameter.IsArray && meta.Array.Count!=0)
			{
				try
				{
					Type type=parameter.GetElementType();
					IMap argument=meta;
					Array arguments=System.Array.CreateInstance(type,argument.Array.Count);
					for(int i=0;i<argument.Count;i++)
					{
						arguments.SetValue(argument[new NormalMap(new Integer(i+1))],i);
					}
					return arguments;
				}
				catch
				{
				}
			}
			else
			{
				bool converted;
				object result=Convert.ToDotNet(meta,parameter,out converted);
				if(converted)
				{
					return result;
				}
			}
			isConverted=false;
			return null;
		}
		public class ArgumentComparer: IComparer
		{
			public int Compare(object x, object y)
			{
				int result;
				MethodBase first=(MethodBase)x;
				MethodBase second=(MethodBase)y;
				ParameterInfo[] firstParameters=first.GetParameters();
				ParameterInfo[] secondParameters=second.GetParameters();
				if(firstParameters.Length==1 && firstParameters[0].ParameterType==typeof(string)
					&& !(secondParameters.Length==1 && secondParameters[0].ParameterType==typeof(string)))
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
		public override IMap Call(IMap argument) // TODO: rename to Invoke???
		{
			object result=null;

			ArrayList oneArgumentMethods=new ArrayList();
			foreach(MethodBase method in overloadedMethods)
			{
				if(method.GetParameters().Length==1)
				{ 
					oneArgumentMethods.Add(method);
				}
			}
			bool isExecuted=false;
			oneArgumentMethods.Sort(new ArgumentComparer());
			foreach(MethodBase method in oneArgumentMethods)
			{
				bool isConverted;
				object parameter=ConvertParameter(argument,method.GetParameters()[0].ParameterType,out isConverted);
				if(isConverted)
				{
					if(method is ConstructorInfo)
					{
						result=((ConstructorInfo)method).Invoke(new object[] {parameter});
					}
					else
					{
						result=method.Invoke(target,new object[] {parameter});
					}
					isExecuted=true;
					break;
				}
			}
			if(!isExecuted)
			{
				ArrayList rightIntegerArgumentMethods=new ArrayList();
				foreach(MethodBase method in overloadedMethods)
				{
					if(argument.Array.Count==method.GetParameters().Length)
					{ 
						if(argument.Array.Count==((IMap)argument).Keys.Count)
						{ 
							rightIntegerArgumentMethods.Add(method);
						}
					}
				}
				if(rightIntegerArgumentMethods.Count==0)
				{
					throw new ApplicationException("Method "+this.name+": No methods with the right number of arguments.");
				}
				foreach(MethodBase method in rightIntegerArgumentMethods)
				{
					ArrayList arguments=new ArrayList();
					bool argumentsMatched=true;
					ParameterInfo[] arPrmtifParameters=method.GetParameters();
					for(int i=0;argumentsMatched && i<arPrmtifParameters.Length;i++)
					{
						arguments.Add(ConvertParameter((IMap)argument.Array[i],arPrmtifParameters[i].ParameterType,out argumentsMatched));
					}
					if(argumentsMatched)
					{
						if(method is ConstructorInfo)
						{
							result=((ConstructorInfo)method).Invoke(arguments.ToArray());
						}
						else
						{
							if(this.name=="Invoke")
							{
								int asdf=0;
							}
							result=method.Invoke(target,arguments.ToArray());
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
			return Convert.ToMeta(result);
		}
		// TODO: refactor
		public static Delegate CreateDelegateFromCode(Type delegateType,MethodInfo method,IMap code)
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
			source+="public class EventHandlerContainer{public "+returnType+" EventHandlerMethod";
			int counter=1;
			string argumentList="(";
			string argumentBuiling="IMap arg=new NormalMap();";
			if(method!=null)
			{
				foreach(ParameterInfo parameter in method.GetParameters())
				{
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentBuiling+="arg[new NormalMap(new Integer("+counter+"))]=Meta.Convert.ToMeta(arg"+counter+");";
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
			source+="IMap result=callable.Call(arg);";
			if(method!=null)
			{
				if(!method.ReturnType.Equals(typeof(void)))
				{
					source+="return ("+returnType+")";
					source+="Meta.Convert.ToDotNet(result,typeof("+returnType+"));"; 
				}
			}
			else 
			{
				source+="return";
				source+=" result;";
			}
			source+="}";
			source+="private IMap callable;";
			source+="public EventHandlerContainer(IMap callable) {this.callable=callable;}}";
			string metaDllLocation=Assembly.GetAssembly(typeof(IMap)).Location;
			ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
			assemblyNames.AddRange(Interpreter.loadedAssemblies);
			CompilerParameters compilerParameters=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
			CompilerResults compilerResults=compiler.CompileAssemblyFromSource(compilerParameters,source);
			Type containerType=compilerResults.CompiledAssembly.GetType("EventHandlerContainer",true);
			object container=containerType.GetConstructor(new Type[]{typeof(IMap)}).Invoke(new object[] {code});
			if(method==null)
			{
				delegateType=typeof(DelegateCreatedForGenericDelegates);
			}
			Delegate result=Delegate.CreateDelegate(delegateType,
				container,"EventHandlerMethod");
			return result;
		}
		private void Initialize(string name,object target,Type targetType)
		{
			this.name=name;
			this.target=target;
			this.targetType=targetType;
			ArrayList methods;
			if(name==".ctor")
			{
				methods=new ArrayList(targetType.GetConstructors());
			}
			else
			{
				methods=new ArrayList(targetType.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
			}
			overloadedMethods=(MethodBase[])methods.ToArray(typeof(MethodBase));
		}
		public DotNetMethod(string name,object target,Type targetType)
		{
			this.Initialize(name,target,targetType);
		}
		public DotNetMethod(Type targetType)
		{
			this.Initialize(".ctor",null,targetType);
		}
		public override bool Equals(object toCompare)
		{
			if(toCompare is DotNetMethod)
			{
				DotNetMethod DotNetMethod=(DotNetMethod)toCompare;
				if(DotNetMethod.target==target && DotNetMethod.name.Equals(name) && DotNetMethod.targetType.Equals(targetType))
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
				int hash=name.GetHashCode()*targetType.GetHashCode();
				if(target!=null)
				{
					hash=hash*target.GetHashCode();
				}
				return hash;
			}
		}
		private string name;
		protected object target;
		protected Type targetType;

		public MethodBase[] overloadedMethods;
	}
	public class DotNetClass: DotNetContainer
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override IMap Clone()
		{
			return new DotNetClass(type);
		}
		protected DotNetMethod constructor;
		public DotNetClass(Type targetType):base(null,targetType)
		{
			this.constructor=new DotNetMethod(this.type);
		}
		public override IMap Call(IMap argument)
		{
			if(this.type.Name.IndexOf("Font")!=-1)
			{
				int asdf=0;
			}
			return constructor.Call(argument);
		}

	}
	public class DotNetObject: DotNetContainer
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public DotNetObject(object target):base(target,target.GetType())
		{
		}
		public override string ToString()
		{
			return obj.ToString();
		}
		public override IMap Clone()
		{
			return new DotNetObject(obj); // TODO: is this correct?
		}
	}
	public abstract class MapStrategy:ISerializeSpecial
	{
		public abstract Integer Integer
		{
			get;
		}
		// TODO: why in MapStrategy??
		public virtual void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
		{
			if(this.String!=null)
			{
				stringBuilder.Append(indentation+"\""+this.String+"\""+"\n");
			}
			else if(this.Integer!=null)
			{
				stringBuilder.Append(indentation+"\""+this.Integer.ToString()+"\""+"\n");
			}
			else
			{
				foreach(object entry in map)
				{
					stringBuilder.Append(indentation+"Entry ("+entry.GetType().Name+")\n");
					ExecuteTests.Serialize(entry,indentation+"  ",functions,stringBuilder);
				}
			}
		}
		public virtual IMap Call(IMap argument)
		{
			map.Argument=argument;
			Expression function=(Expression)this[CodeKeys.Run].GetExpression();
			IMap result;
			result=function.Evaluate(map);
			return result;
		}
		public StrategyMap map;

		public virtual MapStrategy Clone()
		{
			MapStrategy strategy=new HybridDictionaryStrategy();
			foreach(IMap key in this.Keys)
			{
				strategy[key]=this[key];
			}
			return strategy;	
		}
		public abstract IMap CloneMap();// TODO: why is this even needed
		public abstract ArrayList Array
		{
			get;
		}
		public virtual string String
		{
			get
			{
				return null;
			}
		}
		public abstract ArrayList Keys
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
		public abstract IMap this[IMap key] 
		{
			get;
			set;
		}

		public virtual bool ContainsKey(IMap key)
		{
			return Keys.Contains(key);
		}
		public override int GetHashCode()  // TODO: combine with IMap.GetHashCode
		{
			int hash=0;
			foreach(IMap key in this.Keys)
			{
				unchecked
				{
					hash+=key.GetHashCode()*this[key].GetHashCode();
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
			else if(!(strategy is MapStrategy))
			{
				isEqual=false;
			}
			else if(((MapStrategy)strategy).Count!=this.Count)
			{
				isEqual=false;
			}
			else
			{
				isEqual=true;
				foreach(IMap key in this.Keys) 
				{
					if(!((MapStrategy)strategy).ContainsKey(key)||!((MapStrategy)strategy)[key].Equals(this[key]))
					{
						isEqual=false;
					}
				}
			}
			return isEqual;
		}
	}
	public class StringStrategy:MapStrategy
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override int GetHashCode()
		{
			int hash=0;
			for(int i=0;i<text.Length;i++)
			{
				hash+=(i+1)*text[i];
			}
			return hash;
		}
		public override bool Equals(object strategy)
		{
			bool isEqual;
			if(strategy is StringStrategy)
			{	
				isEqual=((StringStrategy)strategy).text.Equals(this.text);
			}
			else
			{
				isEqual=base.Equals(strategy);
			}
			return isEqual;
		}
		public override IMap CloneMap()
		{
			return new NormalMap(new StringStrategy(this));
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList list=new ArrayList();
				foreach(char iChar in text)
				{
					list.Add(new NormalMap(new Integer(iChar)));
				}
				return list;
			}
		}
		public override string String
		{
			get
			{
				return text;
			}
		}
		public override ArrayList Keys
		{
			get
			{
				return keys;
			}
		}
		private ArrayList keys=new ArrayList();
		private string text;
		public StringStrategy(StringStrategy clone)
		{
			this.text=clone.text;
			this.keys=(ArrayList)clone.keys.Clone();
		}
		public StringStrategy(string text)
		{
			this.text=text;
			for(int i=1;i<=text.Length;i++)
			{ 
				keys.Add(new NormalMap(new Integer(i)));			
			}
		}
		public override int Count
		{
			get
			{
				return text.Length;
			}
		}
		public override IMap this[IMap key]
		{
			get
			{
				if(key.IsInteger)
				{
					int iInteger=key.Integer.Int;
					if(iInteger>0 && iInteger<=this.Count)
					{
						return new NormalMap(new Integer(text[iInteger-1]));
					}
				}
				return null;
			}
			set
			{
				map.strategy=this.Clone();
				map.strategy[key]=value;
			}
		}
		public override bool ContainsKey(IMap key) 
		{
			if(key.IsInteger)
			{
				return key.Integer>0 && key.Integer<=this.Count;
			}
			else
			{
				return false;
			}
		}
	}
	public class HybridDictionaryStrategy:MapStrategy
	{
		public override Integer Integer
		{
			get
			{
				Integer number;
				if(this.map.Equals(IntegerKeys.EmptyMap))
				{
					number=0;
				}
				else if((this.Count==1 || (this.Count==2 && this.ContainsKey(IntegerKeys.Negative))) && this.ContainsKey(IntegerKeys.EmptyMap))
				{
					if(this[IntegerKeys.EmptyMap].Integer!=null)
					{
						number=this[IntegerKeys.EmptyMap].Integer+1;
						if(this.ContainsKey(IntegerKeys.Negative))
						{
							number=-number;
						}
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
		}
		ArrayList keys;
		private HybridDictionary dictionary;
		public HybridDictionaryStrategy():this(2)
		{
		}
		public HybridDictionaryStrategy(int Count)
		{
			this.keys=new ArrayList(Count);
			this.dictionary=new HybridDictionary(Count);
		}
		public override IMap CloneMap()
		{
			IMap clone=new NormalMap(new HybridDictionaryStrategy(this.keys.Count));
			foreach(IMap key in keys)
			{
				clone[key]=(IMap)dictionary[key];
			}
			return clone;
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList list=new ArrayList();
				for(Integer iInteger=new Integer(1);ContainsKey(new NormalMap(iInteger));iInteger++)
				{
					list.Add(this[new NormalMap(iInteger)]);
				}
				return list;
			}
		}
		public override string String
		{
			get
			{
				string text="";
				if(Array.Count!=Keys.Count)
				{
					text=null;
				}
				else
				{
					foreach(IMap val in this.Array)
					{
						if(val.IsInteger)
						{
							try
							{
								text+=System.Convert.ToChar(val.Integer.Int);
							}
							catch
							{
								text=null;
								break;
							}
						}
						else
						{
							text=null;
							break;
						}
					}
				}
				return text;
			}
		}
		public override ArrayList Keys
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
		public override IMap this[IMap key] 
		{
			get
			{
				return (IMap)dictionary[key];
			}
			set
			{
				if(!this.ContainsKey(key))
				{
					keys.Add(key);
				}
				dictionary[key]=value;
			}
		}
		public override bool ContainsKey(IMap key) 
		{
			return dictionary.Contains(key);
		}
	}
	public abstract class DotNetContainer: IMap, ISerializeSpecial
	{
		public void Serialize(string indentation, string[] functions, StringBuilder stringBuilder)
		{
			ExecuteTests.Serialize(obj!=null?this.obj:this.type,indentation,functions,stringBuilder);
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList array=new ArrayList();
				foreach(IMap key in Keys)
				{
					if(key.IsInteger)
					{
						array.Add(this[key]);
					}
				}
				return array;
			}
		}
		protected override bool ContainsKeyImplementation(IMap key)
		{
			if(key.IsString)
			{
				string text=key.String;
				if(type.GetMember(key.String,
					BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
				{
					return true;
				}
			}
			DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
			IMap argument=new NormalMap(); // TODO: call indexer directly, combine with other indexer calls
			argument[new NormalMap(new Integer(1))]=key;
			try
			{
				indexer.Call(argument);
				return true;
			}
			catch(Exception)
			{
				return false;
			}
		}
		public override ArrayList Keys
		{
			get
			{
				return new ArrayList(MTable.Keys);
			}
		}
		public override int Count 
		{
			get
			{
				return MTable.Count;
			}
		}
		public override IMap this[IMap key] 
		{
			get
			{
				IMap result;
				if(key.IsString && type.GetMember(key.String,bindingFlags).Length>0)
				{
					string text=key.String; // TODO: Remove empty parent maps
					MemberInfo[] members=type.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						result=new DotNetMethod(text,obj,type);
					}
					else if(members[0] is FieldInfo)
					{
						result=Convert.ToMeta(type.GetField(text).GetValue(obj));
					}
					else if(members[0] is PropertyInfo)
					{
						result=Convert.ToMeta(type.GetProperty(text).GetValue(obj,new object[]{}));
					}
					else if(members[0] is EventInfo)
					{
						try // TODO: refactor
						{
							Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
								BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
							if(eventDelegate==null)
							{
								result=null;
							}
							else
							{
								result=new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
						}
						catch
						{
							result=null;
						}
					}
					else if(members[0] is Type)
					{
						result=new DotNetClass((Type)members[0]);
					}
					else
					{
						result=null;
					}
				}
				else if(this.obj!=null && key.IsInteger && this.type.IsArray)
				{
					result=Convert.ToMeta(((Array)obj).GetValue(key.Integer.Int));
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
					IMap argument=new NormalMap(); // refactor
					argument[new NormalMap(new Integer(1))]=key;
					try
					{
						result=Convert.ToMeta(indexer.Call(argument));
					}
					catch(Exception e)
					{
						result=null;
					}
				}
				return result;
			}
			set
			{
				if(key.IsString && type.GetMember(key.String,bindingFlags).Length!=0)
				{
					string text=key.String;
					MemberInfo member=type.GetMember(text,bindingFlags)[0];
					if(member is FieldInfo)
					{
						FieldInfo field=(FieldInfo)member;
						bool isConverted;
						object val=DotNetMethod.ConvertParameter(value,field.FieldType,out isConverted);
						if(isConverted)
						{
							field.SetValue(obj,val);
						}
						else
						{
							throw new ApplicationException("Field "+field.Name+"could not be assigned because the value cannot be converted.");
						}
					}
					else if(member is PropertyInfo)
					{
						PropertyInfo property=(PropertyInfo)member;
						bool isConverted;
						object val=DotNetMethod.ConvertParameter(value,property.PropertyType,out isConverted);
						if(isConverted)
						{
							property.SetValue(obj,val,new object[]{});
						}
						else
						{
							throw new ApplicationException("Property "+property.Name+"could not be assigned because the value cannot be converted.");
						}
						return;
					}
					else if(member is EventInfo)
					{
						((EventInfo)member).AddEventHandler(obj,CreateEventDelegate(text,value));
					}
					else if(member is MethodBase)
					{
						throw new ApplicationException("Cannot assign to method "+member.Name+".");
					}					 
					else
					{
						throw new ApplicationException("Could not assign "+text+" .");
					}
				}
				else if(obj!=null && key.IsInteger && type.IsArray)
				{
					bool isConverted; 
					object converted=Convert.ToDotNet(value,type.GetElementType(),out isConverted);
					if(isConverted)
					{
						((Array)obj).SetValue(converted,key.Integer.Int);
						return;
					}
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("set_Item",obj,type); // TODO: refactor
					IMap argument=new NormalMap();// TODO: refactor
					argument[new NormalMap(new Integer(1))]=key;
					argument[new NormalMap(new Integer(2))]=value;
					try
					{
						indexer.Call(argument);
					}
					catch(Exception e)
					{
						throw new ApplicationException("Cannot set "+Convert.ToDotNet(key).ToString()+".");// TODO: change exception
					}
				}
			}
		}
		public string Serialize(string indent,string[] functions)
		{
			return indent;
		}
		public Delegate CreateEventDelegate(string name,IMap code)
		{
			EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
				BindingFlags.Static|BindingFlags.Instance);
			MethodInfo invoke=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
				|BindingFlags.Public|BindingFlags.NonPublic);
			Delegate eventDelegate=DotNetMethod.CreateDelegateFromCode(eventInfo.EventHandlerType,invoke,code);
			return eventDelegate;
		}
		private IDictionary MTable
		{ 
			get
			{
				HybridDictionary table=new HybridDictionary();
				foreach(FieldInfo field in type.GetFields(bindingFlags))
				{
					table[new NormalMap(field.Name)]=field.GetValue(obj);
				}
				foreach(MethodInfo invoke in type.GetMethods(bindingFlags)) 
				{
					if(!invoke.IsSpecialName)
					{
						table[new NormalMap(invoke.Name)]=new DotNetMethod(invoke.Name,obj,type);
					}
				}
				foreach(PropertyInfo property in type.GetProperties(bindingFlags))
				{
					if(property.Name!="Item" && property.Name!="Chars")
					{
						table[new NormalMap(property.Name)]=property.GetValue(obj,new object[]{});
					}
				}
				foreach(EventInfo eventInfo in type.GetEvents(bindingFlags))
				{
					table[new NormalMap(eventInfo.Name)]=new DotNetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
				}
				foreach(Type nestedType in type.GetNestedTypes(bindingFlags))
				{ 
					table[new NormalMap(nestedType.Name)]=new DotNetClass(nestedType);
				}
				int counter=1;
				if(obj!=null && obj is IEnumerable && !(obj is String))
				{ 
					foreach(object entry in (IEnumerable)obj)
					{
						if(entry is DictionaryEntry)
						{
							table[Convert.ToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
						}
						else
						{
							table[new NormalMap(new Integer(counter))]=entry;
							counter++;
						}
					}
				}
				return table;
			}
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
		public object obj;
		public Type type;
	}
	public class IntegerStrategy:MapStrategy
	{
		public override int GetHashCode()
		{
			return base.GetHashCode ();
		}
		private Integer number;
		public override Integer Integer
		{
			get
			{
				return number;
			}
		}
		public override bool Equals(object obj)
		{
			bool isEqual;
			if(obj is IntegerStrategy)
			{
				if(((IntegerStrategy)obj).Integer==Integer)
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

		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public IntegerStrategy(Integer number)
		{
			this.number=new Integer(number);
		}
		public override MapStrategy Clone()
		{
			return new IntegerStrategy(number);
		}
		public override IMap CloneMap()
		{
			return new NormalMap(new IntegerStrategy(number));
		}
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				if(number!=0)
				{
					keys.Add(IntegerKeys.EmptyMap);
				}
				if(number<0)
				{
					keys.Add(IntegerKeys.Negative);
				}
				return keys;
			}
		}
		public override IMap this[IMap key]
		{
			get
			{
				IMap result;
				if(key.Equals(IntegerKeys.EmptyMap))
				{
					if(number==0)
					{
						result=new NormalMap(new Integer(0));
					}
					else
					{
						Integer newInteger;
						Integer absoluteOfNewInteger=number.abs()-1;
						if(number>0)
						{
							newInteger=absoluteOfNewInteger;
						}
						else
						{
							newInteger=-absoluteOfNewInteger;
						}
						result=new NormalMap(newInteger);
					}
				}
				else if(key.Equals(IntegerKeys.Negative))
				{
					if(number<0)
					{
						result=new NormalMap();
					}
					else
					{
						result=null;
					}
				}
				else
				{
					result=null;
				}
				return result;
			}
			set
			{
				if(key.Equals(IntegerKeys.EmptyMap)) // TODO: implement
				{
					IMap map=value;// TODO: remove
//					if(map.Integer!=null)
//					{
//						int asdf=0; // TODO: implement
//					}
				}
				else if(key.Equals(IntegerKeys.Negative))
				{
					if(value==null)
					{
						number=number.abs();
					}
					else if(value.Equals(IntegerKeys.EmptyMap))
					{
						number=-number.abs();
					}
					else
					{
						Panic(key,value);
					}
				}
				else
				{
					Panic(key,value);
				}
			}
		}
		private void Panic(IMap key,IMap val)
		{
			map.strategy=this.Clone();
			map.strategy[key]=val;
		}
	}
	namespace Parser 
	{
		public class IndentationStream: TokenStream
		{
			public IndentationStream(TokenStream tokenStream) 
			{
				this.tokenStream=tokenStream;
				Indent(0,new Token());
			}
			public Token nextToken() 
			{
				if(tokenBuffer.Count==0) 
				{
					Token token=tokenStream.nextToken();
					switch(token.Type)
					{
						case MetaLexerTokenTypes.EOF:
							Indent(-1,token);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							Indent(token.getText().Length,token);
							break;
						case MetaLexerTokenTypes.LITERAL:
							string indentation="";
							for(int i=0;i<indentationDepth+1;i++)
							{
								indentation+='\t';
							}
							string text=token.getText();
							text=text.Replace(Environment.NewLine,"\n");
							string[] lines=text.Split('\n');
							string result="";
							for(int i=0;i<lines.Length;i++)
							{
								if(i!=0 && lines[i].StartsWith(indentation))
								{
									result+=lines[i].Remove(0,indentationDepth+1);
								}
								else
								{
									result+=lines[i];
								}
								if(i!=lines.Length-1)
								{
									result+=Environment.NewLine;
								}
							}
							token.setText(result);
							tokenBuffer.Enqueue(token);
							break;
						default:
							tokenBuffer.Enqueue(token);
							break;
					}
				}
				return (Token)tokenBuffer.Dequeue();
			}
			protected void Indent(int newIndentationDepth,Token currentToken) 
			{
				int difference=newIndentationDepth-indentationDepth; 
				if(difference==0)
				{
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(difference==1)
				{
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				}
				else if(difference<0)
				{
					for(int i=difference;i<0;i++)
					{
						tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(difference>1)
				{
					throw new RecognitionException("Incorrect indentation.",currentToken.getFilename(),currentToken.getLine(),currentToken.getColumn());
				}
				indentationDepth=newIndentationDepth;
			}
			protected Queue tokenBuffer=new Queue();
			protected TokenStream tokenStream;
			protected int indentationDepth=-1;
		}
	}
	public class Helper
	{
		public static void WriteFile(string fileName,string text)
		{
			StreamWriter writer=new StreamWriter(fileName);
			writer.Write(text);
			writer.Close();
		}
		public static string ReadFile(string fileName)
		{
			StreamReader reader=new StreamReader(fileName);
			string result=reader.ReadToEnd();
			reader.Close();
			return result;
		}
		public static string ReverseString(string text)
		{
			string result="";
			foreach(char c in text)
			{
				result=c+result;
			}
			return result;
		}
	}
	namespace TestingFramework
	{
		public interface ISerializeSpecial
		{
			void Serialize(string indent,string[] functions,StringBuilder builder);
		}
		public abstract class TestCase
		{
			public abstract object Run();
		}
		public class ExecuteTests
		{	
			public ExecuteTests(Type testContainerType,string fnResults)
			{ 
				bool isWaitAtEnd=false;
				Type[] testTypes=testContainerType.GetNestedTypes();
				foreach(Type testType in testTypes)
				{
					object[] serializeMethodsAttributes=testType.GetCustomAttributes(typeof(SerializeMethodsAttribute),false);
					string[] methodNames=new string[0];
					if(serializeMethodsAttributes.Length!=0)
					{
						methodNames=((SerializeMethodsAttribute)serializeMethodsAttributes[0]).methods;
					}
					Console.Write(testType.Name + "...");
					DateTime start=DateTime.Now;
					string output="";
					object result=((TestCase)testType.GetConstructors()[0].Invoke(new object[]{})).Run();
					TimeSpan timespan=DateTime.Now-start;
					bool wasSuccessful=CompareResult(Path.Combine(fnResults,testType.Name),result,methodNames);
					if(!wasSuccessful)
					{
						output=output + " failed";
						isWaitAtEnd=true;
					}
					else
					{
						output+=" succeeded";
					}
					output=output + "  " + timespan.TotalSeconds.ToString() + " s";
					Console.WriteLine(output);
				}
				if(isWaitAtEnd)
				{
					Console.ReadLine();
				}
			}
			private bool CompareResult(string path,object toSerialize,string[] functions)
			{
				System.IO.Directory.CreateDirectory(path);
				if(!File.Exists(Path.Combine(path,"check.txt")))
				{
					File.Create(Path.Combine(path,"check.txt")).Close();
				}
				StringBuilder stringBuilder=new StringBuilder();
				Serialize(toSerialize,"",functions,stringBuilder);

				string result=stringBuilder.ToString();

				Helper.WriteFile(Path.Combine(path,"result.txt"),result);
				Helper.WriteFile(Path.Combine(path,"resultCopy.txt"),result);
				string check=Helper.ReadFile(Path.Combine(path,"check.txt"));
				return result.Equals(check);
			}
			public static void Serialize(object toSerialize,string indent,string[] methods,StringBuilder stringBuilder) 
			{
				if(toSerialize==null) 
				{
					stringBuilder.Append(indent+"null\n");
				}
				else if(toSerialize is ISerializeSpecial)
				{
					((ISerializeSpecial)toSerialize).Serialize(indent,methods,stringBuilder);
				}
				else if(toSerialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) 
				{
					stringBuilder.Append(indent+"\""+toSerialize.ToString()+"\""+"\n");
				}
				else if(toSerialize is IEnumerable)
				{
					foreach(object entry in (IEnumerable)toSerialize)
					{
						stringBuilder.Append(indent+"Entry ("+entry.GetType().Name+")\n");
						Serialize(entry,indent+"  ",methods,stringBuilder);
					}
				}
				else
				{
					ArrayList members=new ArrayList();
					members.AddRange(toSerialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
					members.AddRange(toSerialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
					foreach(string method in methods)
					{
						MethodInfo methodInfo=toSerialize.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);
						if(methodInfo!=null)
						{ 
							members.Add(methodInfo);
						}
					}
					members.Sort(new CompareMemberInfos());
					foreach(MemberInfo member in members) 
					{
						if(member.Name!="Item") 
						{
							if(member.GetCustomAttributes(typeof(DontSerializeFieldOrPropertyAttribute),false).Length==0) 
							{				
								if(toSerialize.GetType().Namespace!="System.Windows.Forms")
								{ 
									object val=toSerialize.GetType().InvokeMember(member.Name,BindingFlags.Public
										|BindingFlags.Instance|BindingFlags.GetProperty|BindingFlags.GetField
										|BindingFlags.InvokeMethod,null,toSerialize,null);
									stringBuilder.Append(indent+member.Name);
									if(val!=null)
									{
										stringBuilder.Append(" ("+val.GetType().Name+")");
									}
									stringBuilder.Append(":\n");
									Serialize(val,indent+"  ",methods,stringBuilder);
								}
							}
						}
					}
				}
			}
		}
		class CompareMemberInfos:IComparer
		{
			public int Compare(object first,object second)
			{
				if(first==null || second==null || ((MemberInfo)first).Name==null || ((MemberInfo)second).Name==null)
				{
					return 0;}
				else
				{
					return ((MemberInfo)first).Name.CompareTo(((MemberInfo)second).Name);
				}
			}
		}
		[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
		public class DontSerializeFieldOrPropertyAttribute:Attribute
		{
		}
		[AttributeUsage(AttributeTargets.Class)]
		public class SerializeMethodsAttribute:Attribute
		{
			public string[] methods;
			public SerializeMethodsAttribute(string[] methods)
			{
				this.methods=methods;
			}
		}
	}
	public class Position 
	{
		public bool ContainedIn(Position start,Position end)
		{
			return Line>=start.Line && Line<=end.Line && Column>=start.Column && Column<=end.Column;
		}
		private int line;
		private int column;
		public Position(int line,int column)
		{
			this.line=line;
			this.column=column;

		}
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
		public static ArrayList GetEvents(string fileName,int firstLine,int lastLine)
		{
			ArrayList result=new ArrayList();
			foreach(DictionaryEntry entry in Extents)
			{
				Extent extent=(Extent)entry.Value;
				if(extent.FileName==fileName && extent.Start.Line>=firstLine && extent.End.Line<=lastLine)
				{
					result.Add(extent);
				}
			}
			return result;
		}
		public static Hashtable Extents
		{
			get
			{
				return extents;
			}
		}
		private static Hashtable extents=new Hashtable();
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
					extent.End.Column==End.Column && 
					extent.FileName==FileName)
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
				return fileName.GetHashCode()*Start.Line.GetHashCode()*Start.Column.GetHashCode()*End.Line.GetHashCode()*End.Column.GetHashCode();
			}
		}

		public Position Start
		{
			get
			{
				return start;
			}
		}
		public Position End
		{
			get
			{
				return end;
			}
		}
		private Position start;
		private Position end;
		public string FileName
		{
			get
			{
				return fileName;
			}
			set
			{
				fileName=value;
			}
		}
		string fileName;
		public Extent(int startLine,int startColumn,int endLine,int endColumn,string fileName) 
		{
			this.start=new Position(startLine,startColumn);
			this.end=new Position(endLine,endColumn);
			this.fileName=fileName;

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
}