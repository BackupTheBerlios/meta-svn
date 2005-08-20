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

namespace Meta
{

	public class CodeKeys
	{
		public readonly static IMap Literal=new StrategyMap("literal");
		public readonly static IMap Run=new StrategyMap("run");
		public readonly static IMap Call=new StrategyMap("call");
		public readonly static IMap Function=new StrategyMap("function");
		public readonly static IMap Argument=new StrategyMap("argument");
		public static readonly IMap Select=new StrategyMap("select");
		public static readonly IMap Search=new StrategyMap("search");
		public static readonly IMap Key=new StrategyMap("key");
		public static readonly IMap Program=new StrategyMap("program");
		public static readonly IMap Delayed=new StrategyMap("delayed");
		public static readonly IMap Lookup=new StrategyMap("lookup");
		public static readonly IMap Value=new StrategyMap("value");
	}
	public class SpecialKeys
	{
		public static readonly IMap Parent=new StrategyMap("parent"); 
		public static readonly IMap Arg=new StrategyMap("arg");
		public static readonly IMap This=new StrategyMap("this");
	}
	public class NumberKeys
	{
		public static readonly IMap Denominator=new StrategyMap("denominator");
		public static readonly IMap Numerator=new StrategyMap("numerator");
		public static readonly IMap Negative=new StrategyMap("negative");
		public static readonly IMap EmptyMap=new StrategyMap(); // TODO: move somewhere else, reorganize SpecialKeys and so on
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
				if(BreakPoint.Position.Line>=Extent.Start.Line && BreakPoint.Position.Line<=Extent.End.Line)// TODO: put this functionality into Position
				{
					if(BreakPoint.Position.Column>=Extent.Start.Column && BreakPoint.Position.Column<=Extent.End.Column)
					{
						stop=true;
					}
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
			this.callable=((IMap)code[CodeKeys.Function]).GetExpression();
			this.argument=((IMap)code[CodeKeys.Argument]).GetExpression();
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
			IMap local=new StrategyMap();
			Evaluate(parent,ref local);
			return local;
		}
		public void Evaluate(IMap parent,ref IMap local)
		{
			((IMap)local).Parent=parent;
			for(int i=0;i<statements.Count && i>=0;)
			{
				if(i==85)
				{
					int asdf=0;
				}
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
				if(statement==null)
				{
					object x=code.Array;
					int asdf=0;
				}
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

	public abstract class Recognition
	{
		public abstract IMap Recognize(string text);
	}
	public class Recognitions
	{
		public class IntegerRecogition: Recognition 
		{
			public override IMap Recognize(string text) 
			{ 
				if(text.Equals(""))
				{
					return null;
				}
				else
				{
					Integer result=new Integer(0); // TODO: dont use explicit Integer creation when not necessary
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
					return new StrategyMap(result);
				}
			}
		}
		public class StringRecognition:Recognition
		{
			public override IMap Recognize(string text)
			{
				return new StrategyMap(text);
			}
		}
	}
	public class Literal: Expression
	{
		public static ArrayList recognitions=new ArrayList();
		static Literal()
		{
			foreach(Type recognition in typeof(Recognitions).GetNestedTypes())
			{
				recognitions.Add((Recognition)recognition.GetConstructor(new Type[]{}).Invoke(new object[]{}));
			}
		}
		public override bool Stop()
		{
			return false;
		}

		public override IMap EvaluateImplementation(IMap parent)
		{
			if(literal.Equals(new StrategyMap("EnabledChanged")))
			{
				int asdf=0;
			}
			return literal;
		}
		public Literal(IMap code)
		{
			this.literal=Recognition((string)code.String);
		}
		public IMap literal=null; // TODO: should always be IMap
		public static IMap Recognition(string text) // TODO: rename
		{
			foreach(Recognition recognition in recognitions)
			{
				IMap recognized=recognition.Recognize(text);
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
			if(key.Equals(new StrategyMap("helloh")))
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
				IMap selection=((IMap)selected)[key];
				if(selection==null)
				{
					object test=((IMap)selected)[key];
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
			object selected=parent;
			IMap key;
			
			if(searchFirst)
			{
				IMap firstKey=((Expression)keys[0]).Evaluate((IMap)parent); 
				if(firstKey.Equals(new StrategyMap("instanceEventChanged")))
				{
					int asdf=0;
				}
				while(!((IMap)selected).ContainsKey(firstKey))
				{
					selected=((IMap)selected).Parent;
					if(selected==null)
					{
						throw new KeyNotFoundException(firstKey,((Expression)keys[0]).Extent);
					}
				}
			}
			for(int i=0;i<keys.Count-1;i++)
			{
				key=(IMap)((Expression)keys[i]).Evaluate((IMap)parent);
				object selection=((IMap)selected)[key];
				if(selection==null)
				{
					throw new KeyDoesNotExistException(key,((Expression)keys[i]).Extent,selected);
				}
				selected=selection;
			}
			IMap lastKey=(IMap)((Expression)keys[keys.Count-1]).Evaluate((IMap)parent);
			IMap val=expression.Evaluate((IMap)parent);
			if(lastKey.Equals(SpecialKeys.This))
			{
				if(val is IMap)
				{
					((IMap)val).Parent=((IMap)parent).Parent;
				}
				parent=val;
			}
			else
			{
				if(((IMap)selected).ContainsKey(lastKey))
				{
					replaceValue=((IMap)selected)[lastKey];
				}
				else
				{
					replaceValue=null;
				}
				replaceMap=(IMap)selected;
				replaceKey=lastKey;

				((IMap)selected)[lastKey]=val;
			}
		}
		public Statement(IMap code) 
		{
			if(code.ContainsKey(CodeKeys.Search))
			{
				searchFirst=true;
			}
			foreach(IMap key in ((IMap)code[CodeKeys.Key]).Array)
			{
				keys.Add(key.GetExpression());
			}
			this.expression=(Expression)((IMap)code[CodeKeys.Value]).GetExpression();
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


		public static void SaveToFile(object meta,string path)// TODO: move into Directory
		{
			StreamWriter streamWriter=new StreamWriter(path);
			streamWriter.Write(SaveToFile(meta,"",true).TrimEnd(new char[]{'\n'}));
			streamWriter.Close();
		}
		public static string Serialize(object meta)
		{
			return SaveToFile(meta,"",true);
		}
		// TODO: integrate into Directory
		public static string SaveToFile(object meta,string indent,bool isRightSide)
		{
			if(meta is IMap)
			{
				string text="";
				IMap map=(IMap)meta;
				if(map.IsString)
				{
					text+="\""+(map).String+"\"";
				}
				else if(map.Count==0)
				{
					text+='\'';
				}
				else
				{
					if(!isRightSide)
					{
						text+="("; // TODO: correct this to use indentation instead of parentheses
						foreach(DictionaryEntry entry in map)
						{
							text+='['+SaveToFile(entry.Key,indent,true)+']'+'='+SaveToFile(entry.Value,indent,true)+",";
						}
						if(map.Count!=0)
						{
							text=text.Remove(text.Length-1,1);
						}
						text+=")";
					}
					else
					{
						foreach(DictionaryEntry entry in map)
						{
							text+=indent+'['+SaveToFile(entry.Key,indent,false)+']'+'=';
							if(entry.Value is IMap && ((IMap)entry.Value).Count!=0 && !((IMap)entry.Value).IsString)
							{
								text+="\n";
							}
							text+=SaveToFile(entry.Value,indent+'\t',true);
							if(!(entry.Value is IMap && ((IMap)entry.Value).Count!=0 && !((IMap)entry.Value).IsString))
							{
								text+="\n";
							}
						}
					}
				}
				return text;
			}
			if(meta is IMap && ((IMap)meta).Number!=null) // TODO: refactor
			{
				Integer integer=((IMap)meta).Number;
				return "\""+integer.ToString()+"\"";
			}
//			else if(meta is Integer)
//			{
//				Integer integer=(Integer)meta;
//				return "\""+integer.ToString()+"\"";
//			}
			else
			{
				return "\""+meta.ToString()+"\"";
				//throw new ApplicationException("Serialization not implemented for type "+meta.GetType().ToString()+".");
			}
		}
		public static IMap Merge(params IMap[] arkvlToMerge)
		{
			return MergeCollection(arkvlToMerge);
		}
		// TODO: integrate merging into the IMap, and specialise
		public static IMap MergeCollection(ICollection collection)
		{
			IMap result=new StrategyMap();
			foreach(IMap current in collection)
			{
				foreach(DictionaryEntry entry in (IMap)current)
				{
					if(entry.Value is IMap && !(entry.Value is DotNetClass)&& result.ContainsKey((IMap)entry.Key) 
						/*&& result[(IMap)entry.Key] is IMap*/ && !(result[(IMap)entry.Key] is DotNetClass))
					{
						result[(IMap)entry.Key]=Merge((IMap )result[(IMap)entry.Key],(IMap)entry.Value);
					}
					else
					{
						result[(IMap)entry.Key]=(IMap)entry.Value;
					}
				}
			}
			return result;
		}
		public static object Run(string fileName,IMap argument)
		{
			IMap program=Interpreter.Compile(fileName);
			return CallProgram(program,argument,GAC.library);
		}
		public static object RunWithoutLibrary(string fileName,IMap argument)
		{
			IMap program=Compile(fileName);
			return CallProgram(program,argument,null);
		}
		public static object CallProgram(IMap program,IMap argument,IMap parent)
		{
			IMap callable=new StrategyMap();
			callable[CodeKeys.Run]=program;
			callable.Parent=parent;
			return callable.Call(argument);
		}
		public static IMap Compile(string fileName) // TODO: move this into MetaFile, will be implicit
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
			Interpreter.Run(executeFileName,new StrategyMap());
			int asdf=0;
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
		public KeyException(object key,Extent extent):base(extent)
		{
			message="Key ";
			if(key is IMap && ((IMap)key).IsString)
			{
				message+=((IMap)key).String;
			}
			else if(key is IMap)
			{
				message+=Interpreter.SaveToFile(key,"",true);
			}
			else
			{
				message+=key;
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
		public KeyNotFoundException(object key,Extent extent):base(key,extent)
		{
		}
	}
	public class KeyDoesNotExistException:KeyException
	{
		private object selected;
		public KeyDoesNotExistException(object key,Extent extent,object selected):base(key,extent)
		{
			this.selected=selected;
		}
	}
	public interface ICallable
	{
		IMap Call(IMap argument);
	}
	public abstract class IMap: ICallable, IEnumerable //, ISerializeSpecial
	{
		public virtual bool IsNumber
		{
			get
			{
				return Number!=null;
			}
		}
//		public virtual bool IsNumber
//		{
//			get
//			{
//				return false;
//			}
//		}
		public virtual Integer Number // TODO: IMap should be able to be a Number, too, problem is duplication with HybridDictionaryStrategy, need default implementation usable from Strategy
		{
			get
			{
				return null;
			}
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
		public virtual bool IsString // TODO: dont do try-catch, instead return null from all Strings and check for that
		{
			get
			{
				bool isString=false;
				if(Array.Count>0) // TODO: this might not be quite correct
				{
					try
					{
						object o=String;
						isString=true;
					}
					catch
					{
					}
				}
				return isString;
			}
		}
		// TODO: this is duplicated in MapStrategy
		public virtual string String // TODO: put this into a separate function, with IEnumerable as argument, maybe
		{
			get
			{
				string text="";
				foreach(IMap key in this.Keys)
				{
					if(key is IMap && ((IMap)key).Number!=null && this[key] is IMap && ((IMap)this[key]).Number!=null) // TODO: refactor, when IMap only returns IMap, and keys can only be IMaps
//					if(key is Integer && this[key] is Integer)
					{
						try
						{
							text+=System.Convert.ToChar(((IMap)this[key]).Number.Int);
						}
						catch
						{
							throw new MapException(this,"Map is not a string");
						}
					}
					else
					{
						throw new MapException(this,"Map is not a string");
					}
				}
				return text;
			
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
					if(Helper.IsNumber(key))
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
//		{
//			get
//			{
//				object result;
//				if(key.Equals(SpecialKeys.Parent))
//				{
//					result=Parent;
//				}
//				else if(key.Equals(SpecialKeys.Arg))
//				{
//					result=Argument;
//				}
//				else if(key.Equals(SpecialKeys.This))
//				{
//					result=this;
//				}
//				else
//				{
//					result=strategy[key];
//				}
//				return result;
//			}
//			set
//			{
//				if(value!=null)
//				{
//					isHashCached=false;
//					if(key.Equals(SpecialKeys.This))
//					{
//						this.strategy=((IMap)value).strategy.Clone();
//					}
//					else
//					{
//						object val;
//						if(value is IMap)
//						{
//							val=((IMap)value).Clone();
//							((IMap)val).Parent=this;
//						}
//						else
//						{
//							val=value;
//						}
//						strategy[key]=val;
//					}
//				}
//			}
//		}
		public virtual IMap Call(IMap argument)
		{
			Argument=argument;
			Expression function=(Expression)((IMap)this[CodeKeys.Run]).GetExpression();
			IMap result;
			result=function.Evaluate(this);
			return result;
		}
//		public abstract object Call(object argument);
////		{
////			return strategy.Call(argument);
////		}
		public abstract ArrayList Keys
		{
			get;
		}
//		{
//			get
//			{
//				return strategy.Keys;
//			}
//		}
		public abstract IMap Clone();
//		{
//			IMap clone=strategy.CloneMap();
//			clone.Parent=Parent;
//			clone.Extent=Extent;
//			return clone;
//		}
		public virtual Expression GetExpression()
		{
			Expression expression;
			if(this.ContainsKey(CodeKeys.Call))
			{
				expression=new Call((IMap)this[CodeKeys.Call]);
			}
			else if(this.ContainsKey(CodeKeys.Delayed))
			{ 
				expression=new Delayed((IMap)this[CodeKeys.Delayed]);
			}
			else if(this.ContainsKey(CodeKeys.Program))
			{
				expression=new Program((IMap)this[CodeKeys.Program]);
			}
			else if(this.ContainsKey(CodeKeys.Literal))
			{
				expression=new Literal((IMap)this[CodeKeys.Literal]);
			}
			else if(this.ContainsKey(CodeKeys.Search))
			{
				expression=new Search((IMap)this[CodeKeys.Search]);
			}
			else if(this.ContainsKey(CodeKeys.Select))
			{
				expression=new Select((IMap)this[CodeKeys.Select]);
			}
			else
			{
				throw new ApplicationException("Cannot compile non-code map.");
			}
			((Expression)expression).Extent=this.Extent;
			return expression;
		}
		public virtual bool ContainsKey(IMap key) 
		{
			if(key is IMap)
			{
				if(key.Equals(SpecialKeys.Arg))
				{
					return this.Argument!=null;
				}
				else if(key.Equals(SpecialKeys.Parent))
				{
					return this.Parent!=null;
				}
				else if(key.Equals(SpecialKeys.This))
				{
					return true;
				}
			}
			return Keys.Contains(key);
		}
//		{
//			if(key is IMap)
//			{
//				if(key.Equals(SpecialKeys.Arg))
//				{
//					return this.Argument!=null;
//				}
//				else if(key.Equals(SpecialKeys.Parent))
//				{
//					return this.Parent!=null;
//				}
//				else if(key.Equals(SpecialKeys.This))
//				{
//					return true;
//				}
//			}
//			return strategy.ContainsKey(key);
//		}


		//public abstract bool Equals(object toCompare); // TODO: maybe provide default implementation



//		{
//			bool isEqual;
//			if(Object.ReferenceEquals(toCompare,this))
//			{
//				isEqual=true;
//			}
//			else if(toCompare is IMap)
//			{
//				isEqual=((IMap)toCompare).strategy.Equals(strategy);
//			}
//			else
//			{
//				isEqual=false;
//			}
//			return isEqual;
//		}
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
//		public abstract int GetHashCode();
//		{
//			if(!isHashCached)
//			{
//				hash=this.strategy.GetHashCode();
//				isHashCached=true;
//			}
//			return hash;
//		}
		private bool isHashCached=false;
		private int hash;
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
		//		public IMap(Integer Number):this(new Integer(number))
		//		{
		//		}
		//		public IMap(Integer number):this(new IntegerStrategy(number))
		//		{
		//		}


//		public IMap(string namespaceName,Hashtable subNamespaces,ArrayList assemblies):this(new LazyNamespace(namespaceName,subNamespaces,assemblies))
//		{
//		}
//		public IMap(object obj):this(new DotNetObject(obj))
//		{
//		}
//		public IMap(Type type):this(new DotNetClass(type))
//		{
//		}
//		public IMap(string text):this(new StringStrategy(text))
//		{
//		}
//		public IMap():this(new HybridDictionaryStrategy())
//		{
//		}
//		public IMap(MapStrategy strategy)
//		{
//			this.strategy=strategy;
//			this.strategy.map=this;
//		}
//		public MapStrategy strategy;



		private IMap parent;
//
//		public void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
//		{
//			strategy.Serialize(indentation,functions,stringBuilder);
//		}
	}
	public class StrategyMap: IMap, ISerializeSpecial // TODO: make this an abstract class
	{
//		public override bool IsNumber
//		{
//			get
//			{
//				return strategy.IsNumber;
//			}
//		}
		public override Integer Number
		{
			get
			{
				return strategy.Number;
			}
		}


//		public StrategyMap(string namespaceName,Hashtable subNamespaces,ArrayList assemblies):this(new LazyNamespace(namespaceName,subNamespaces,assemblies))
//	{
//	}
//		public StrategyMap(object obj):this(new DotNetObject(obj))
//	{
//	}
//		public StrategyMap(Type type):this(new DotNetClass(type))
//	{
//	}
//		public StrategyMap(string text):this(new StringStrategy(text))
//	{
//	}
//		public StrategyMap():this(new HybridDictionaryStrategy())
//	{
//	}
//		public StrategyMap(MapStrategy strategy)
//		{
//			this.strategy=strategy;
//			this.strategy.map=this.map;
//		}
//		public MapStrategy strategy;
//		public void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
//		{
//			strategy.Serialize(indentation,functions,stringBuilder);
//		}
//		public object Argument
//		{
//			get
//			{
//				return arg;
//			}
//			set
//			{ 
//				arg=value;
//			}
//		}
//		object arg=null;
		public override bool IsString
		{
			get
			{
				return strategy.IsString;
			}
		}
		public override string String
		{
			get
			{
				return strategy.String;
			}
		}
//		public virtual IMap Parent
//		{
//			get
//			{
//				return parent;
//			}
//			set
//			{
//				parent=value;
//			}
//		}
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
						this.strategy=((StrategyMap)value).strategy.Clone();
					}
					else
					{
						IMap val;
						if(value is IMap)
						{
							val=((IMap)value).Clone();
							((IMap)val).Parent=this;
						}
						else
						{
							val=value;
						}
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
		public override IMap Clone()
		{
			IMap clone=strategy.CloneMap();
			clone.Parent=Parent;
			clone.Extent=Extent;
			return clone;
		}
		public override Expression GetExpression() // maybe move up????
		{
			Expression expression;
			if(this.ContainsKey(CodeKeys.Call))
			{
				expression=new Call((IMap)this[CodeKeys.Call]);
			}
			else if(this.ContainsKey(CodeKeys.Delayed))
			{ 
				expression=new Delayed((IMap)this[CodeKeys.Delayed]);
			}
			else if(this.ContainsKey(CodeKeys.Program))
			{
				expression=new Program((IMap)this[CodeKeys.Program]);
			}
			else if(this.ContainsKey(CodeKeys.Literal))
			{
				expression=new Literal((IMap)this[CodeKeys.Literal]);
			}
			else if(this.ContainsKey(CodeKeys.Search))
			{
				expression=new Search((IMap)this[CodeKeys.Search]);
			}
			else if(this.ContainsKey(CodeKeys.Select))
			{
				expression=new Select((IMap)this[CodeKeys.Select]);
			}
			else
			{
				throw new ApplicationException("Cannot compile non-code map.");
			}
			((Expression)expression).Extent=this.Extent;
			return expression;
		}
		public override bool ContainsKey(IMap key) 
		{
			if(key is IMap) // TODO: duplicated with IMap
			{
				if(key.Equals(SpecialKeys.Arg))
				{
					return this.Argument!=null;
				}
				else if(key.Equals(SpecialKeys.Parent))
				{
					return this.Parent!=null;
				}
				else if(key.Equals(SpecialKeys.This))
				{
					return true;
				}
			}
			return strategy.ContainsKey(key);
		}
		public override bool Equals(object toCompare)
		{
			bool isEqual;
			if(Object.ReferenceEquals(toCompare,this))
			{
				isEqual=true;
			}
			else if(toCompare is StrategyMap)
			{
				isEqual=((StrategyMap)toCompare).strategy.Equals(strategy);
			}
			else
			{
				isEqual=false;
			}
			return isEqual;
		}
//		public virtual IEnumerator GetEnumerator()
//		{
//			return new MapEnumerator(this);
//		}
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
//		Extent extent;
//		public Extent Extent
//		{
//			get
//			{
//				return extent;
//			}
//			set
//			{
//				extent=value;
//			}
//		}
//		public IMap(Integer Number):this(new Integer(number))
//		{
//		}
//		public IMap(Integer number):this(new IntegerStrategy(number))
//		{
//		}
		public StrategyMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public StrategyMap(string namespaceName,Hashtable subNamespaces,ArrayList assemblies):this(new LazyNamespace(namespaceName,subNamespaces,assemblies))
		{
		}
//		public StrategyMap(object obj):this(new DotNetObject(obj))
//		{
//		}
//		public StrategyMap(Type type):this(new DotNetClass(type))
//		{
//		}
		public StrategyMap(string text):this(new StringStrategy(text))
		{
		}
		public StrategyMap(MapStrategy strategy)
		{
			this.strategy=strategy;
			this.strategy.map=this;
		}
		public StrategyMap():this(new HybridDictionaryStrategy())
		{
		}
		private IMap parent;
		public MapStrategy strategy;

		public void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
		{
			strategy.Serialize(indentation,functions,stringBuilder);
		}
	}
	public class MetaLibrary // TODO: integrate into MetaFile
	{
		public object Load()
		{
			return Interpreter.Run(path,new StrategyMap());
		}
		public MetaLibrary(string path)
		{
			this.path=path;
		}
		string path;
	}
	public class LazyNamespace: MapStrategy // TODO: integrate into Directory
	{
		public override Integer Number
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
			return new StrategyMap(this);
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
			cache=new StrategyMap();
			foreach(CachedAssembly cachedAssembly in cachedAssemblies)
			{
				cache=(IMap)Interpreter.Merge(cache,cachedAssembly.NamespaceContents(fullName));
			}
			foreach(DictionaryEntry entry in namespaces)
			{
				cache[new StrategyMap((string)entry.Key)]=(IMap)entry.Value;
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
					selected=(IMap)selected[new StrategyMap(subString)];
				}
			}
			return selected;
		}			
		private IMap assemblyContent;
	}
		// TODO: refactor
	public class GAC: IMap// TODO: split into GAC and Directory
	{
//		public override IMap CloneMap()
//		{
//			return new StrategyMap(this);
//			//return new StrategyMap(true); // TODO: this is definitely a bug!!!
//		}

		public override IMap this[IMap key]
		{
			get
			{
				if(cache.ContainsKey(key))
				{
//					if(cache[key] is MetaLibrary)
//					{
//						cache[key]=((MetaLibrary)cache[key]).Load();
//					}
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
//		public override MapStrategy Clone() // TODO: not sure this is correct
//		{
//			return this;
//		}
		public override int Count
		{
			get
			{
				return cache.Count;
			}
		}
		public override bool ContainsKey(IMap key)
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
			IMap root=new StrategyMap();
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
							if(!position.ContainsKey(new StrategyMap(subPath))) 
							{
								position[new StrategyMap(subPath)]=new StrategyMap();
							}
							position=(IMap)position[new StrategyMap(subPath)];
						}
						position[new StrategyMap(type.Name)]=new DotNetClass(type);
						//position[new StrategyMap(type.Name)]=new StrategyMap(type);
						//position[new IMap(type.Name)]=new DotNetClass(type);
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
			foreach(string dll in System.IO.Directory.GetFiles(libraryPath,"*.dll"))
			{
				assemblies.Add(Assembly.LoadFrom(dll));
			}
			foreach(string exe in System.IO.Directory.GetFiles(libraryPath,"*.exe"))
			{
				assemblies.Add(Assembly.LoadFrom(exe));
			}
			string cachedAssemblyPath=Path.Combine(Interpreter.installationPath,"cachedAssemblyInfo.meta");
			if(File.Exists(cachedAssemblyPath))
			{
				cachedAssemblyInfo=(IMap)Interpreter.RunWithoutLibrary(cachedAssemblyPath,new StrategyMap());
			}
		
			cache=LoadNamespaces(assemblies);
			Interpreter.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
//			foreach(string meta in System.IO.Directory.GetFiles(libraryPath,"*.meta"))
//			{
//				cache[new StrategyMap(Path.GetFileNameWithoutExtension(meta))]=new MetaLibrary(meta);
//			}
		}
		private IMap cachedAssemblyInfo=new StrategyMap();
		public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
		{ 
			ArrayList nameSpaces=new ArrayList();
			MapAdapter cached=new MapAdapter(cachedAssemblyInfo);
			if(cached.ContainsKey(assembly.Location))
			{
				MapAdapter info=new MapAdapter((IMap)cached[assembly.Location]);
				string timestamp=(string)info["timestamp"];
				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
				{
					MapAdapter namespaces=new MapAdapter((IMap)info["namespaces"]);
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
			IMap cachedAssemblyInfoMap=new StrategyMap();
			IMap nameSpace=new StrategyMap(); 
			Integer counter=new Integer(0);
			foreach(string na in nameSpaces)
			{
				nameSpace[new StrategyMap(counter)]=new StrategyMap(na);
				counter++;
			}
			cachedAssemblyInfoMap[new StrategyMap("namespaces")]=nameSpace;
			cachedAssemblyInfoMap[new StrategyMap("timestamp")]=new StrategyMap(File.GetLastWriteTime(assembly.Location).ToString());
			cachedAssemblyInfo[new StrategyMap(assembly.Location)]=cachedAssemblyInfoMap;
			return nameSpaces;
		}
		// TODO: refactor this
		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
		{
			StrategyMap root=new StrategyMap("",new Hashtable(),new ArrayList());
			//LazyNamespace root=new LazyNamespace("",new Hashtable(),new ArrayList());
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
								selected.namespaces[subString]=new StrategyMap(fullName,new Hashtable(),new ArrayList());
								//selected.namespaces[subString]=new LazyNamespace(fullName,new Hashtable(),new ArrayList());
							}
							selected=(LazyNamespace)((StrategyMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
						}
					}
					selected.AddAssembly(cachedAssembly);
					//selected.cachedAssemblies.Add(cachedAssembly);
				}
			}
			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
			return root; // TODO: is this correct?
		}
		//		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
		//		{
		//			IMap root=new StrategyMap("",new Hashtable(),new ArrayList());
		//			//LazyNamespace root=new LazyNamespace("",new Hashtable(),new ArrayList());
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
		//								selected.namespaces[subString]=new IMap(fullName,new Hashtable(),new ArrayList());
		//								//selected.namespaces[subString]=new LazyNamespace(fullName,new Hashtable(),new ArrayList());
		//							}
		//							selected=(LazyNamespace)((IMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
		//						}
		//					}
		//					selected.AddAssembly(cachedAssembly);
		//					//selected.cachedAssemblies.Add(cachedAssembly);
		//				}
		//			}
		//			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
		//			return root; // TODO: is this correct?
		//		}
		public static IMap library=new GAC(); // TODO: is this the right way to do it??
		//public static IMap library=new StrategyMap(new GAC()); // TODO: is this the right way to do it??
		private IMap cache=new StrategyMap();
		//private IMap cache=new StrategyMap();
		public static string libraryPath="library"; 
	}
//	public class GAC: MapStrategy// TODO: split into GAC and Directory
//	{
//		public override IMap CloneMap()
//		{
//			return new StrategyMap(this);
//			//return new StrategyMap(true); // TODO: this is definitely a bug!!!
//		}
//
//		public override object this[object key]
//		{
//			get
//			{
//				if(cache.ContainsKey(key))
//				{
//					if(cache[key] is MetaLibrary)
//					{
//						cache[key]=((MetaLibrary)cache[key]).Load();
//					}
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
//		public override MapStrategy Clone() // TODO: not sure this is correct
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
//		public override bool ContainsKey(object key)
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
//			IMap root=new StrategyMap();
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
//							if(!position.ContainsKey(new StrategyMap(subPath))) 
//							{
//								position[new StrategyMap(subPath)]=new StrategyMap();
//							}
//							position=(IMap)position[new StrategyMap(subPath)];
//						}
//						position[new StrategyMap(type.Name)]=new DotNetClass(type);
//						//position[new StrategyMap(type.Name)]=new StrategyMap(type);
//						//position[new IMap(type.Name)]=new DotNetClass(type);
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
//				cachedAssemblyInfo=(IMap)Interpreter.RunWithoutLibrary(cachedAssemblyPath,new StrategyMap());
//			}
//		
//			cache=LoadNamespaces(assemblies);
//			Interpreter.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
//			foreach(string meta in System.IO.Directory.GetFiles(libraryPath,"*.meta"))
//			{
//				cache[new StrategyMap(Path.GetFileNameWithoutExtension(meta))]=new MetaLibrary(meta);
//			}
//		}
//		private IMap cachedAssemblyInfo=new StrategyMap();
//		public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
//		{ 
//			ArrayList nameSpaces=new ArrayList();
//			MapAdapter cached=new MapAdapter(cachedAssemblyInfo);
//			if(cached.ContainsKey(assembly.Location))
//			{
//				MapAdapter info=new MapAdapter((IMap)cached[assembly.Location]);
//				string timestamp=(string)info["timestamp"];
//				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
//				{
//					MapAdapter namespaces=new MapAdapter((IMap)info["namespaces"]);
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
//			IMap cachedAssemblyInfoMap=new StrategyMap();
//			IMap nameSpace=new StrategyMap(); 
//			Integer counter=new Integer(0);
//			foreach(string na in nameSpaces)
//			{
//				nameSpace[counter]=new StrategyMap(na);
//				counter++;
//			}
//			cachedAssemblyInfoMap[new StrategyMap("namespaces")]=nameSpace;
//			cachedAssemblyInfoMap[new StrategyMap("timestamp")]=new StrategyMap(File.GetLastWriteTime(assembly.Location).ToString());
//			cachedAssemblyInfo[new StrategyMap(assembly.Location)]=cachedAssemblyInfoMap;
//			return nameSpaces;
//		}
//		// TODO: refactor this
//		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
//		{
//			StrategyMap root=new StrategyMap("",new Hashtable(),new ArrayList());
//			//LazyNamespace root=new LazyNamespace("",new Hashtable(),new ArrayList());
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
//								selected.namespaces[subString]=new StrategyMap(fullName,new Hashtable(),new ArrayList());
//								//selected.namespaces[subString]=new LazyNamespace(fullName,new Hashtable(),new ArrayList());
//							}
//							selected=(LazyNamespace)((StrategyMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
//						}
//					}
//					selected.AddAssembly(cachedAssembly);
//					//selected.cachedAssemblies.Add(cachedAssembly);
//				}
//			}
//			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
//			return root; // TODO: is this correct?
//		}
////		public IMap LoadNamespaces(ArrayList assemblies) // TODO: rename
////		{
////			IMap root=new StrategyMap("",new Hashtable(),new ArrayList());
////			//LazyNamespace root=new LazyNamespace("",new Hashtable(),new ArrayList());
////			foreach(Assembly assembly in assemblies)
////			{
////				ArrayList nameSpaces=NameSpaces(assembly);
////				CachedAssembly cachedAssembly=new CachedAssembly(assembly);
////				foreach(string nameSpace in nameSpaces)
////				{
////					if(nameSpace=="System")
////					{
////						int asdf=0;
////					}
////					LazyNamespace selected=(LazyNamespace)root.strategy; // TODO: this sucks quite a bit!!
////					if(nameSpace=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.installationPath,"library")))
////					{
////						continue;
////					}
////					if(nameSpace!="")
////					{
////						foreach(string subString in nameSpace.Split('.'))
////						{
////							if(!selected.namespaces.ContainsKey(subString))
////							{
////								string fullName=selected.fullName;
////								if(fullName!="")
////								{
////									fullName+=".";
////								}
////								fullName+=subString;
////								selected.namespaces[subString]=new IMap(fullName,new Hashtable(),new ArrayList());
////								//selected.namespaces[subString]=new LazyNamespace(fullName,new Hashtable(),new ArrayList());
////							}
////							selected=(LazyNamespace)((IMap)selected.namespaces[subString]).strategy; // TODO: this sucks!
////						}
////					}
////					selected.AddAssembly(cachedAssembly);
////					//selected.cachedAssemblies.Add(cachedAssembly);
////				}
////			}
////			((LazyNamespace)root.strategy).Load(); // TODO: remove, integrate into indexer, is this even necessary???
////			return root; // TODO: is this correct?
////		}
//		public static IMap library=new StrategyMap(new GAC()); // TODO: is this the right way to do it??
//		private IMap cache=new StrategyMap();
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
		public static object ToDotNet(object meta,Type target,out bool isConverted)
		{
			if(target.IsSubclassOf(typeof(Enum)) && Helper.IsNumber(meta))
			{ 
				isConverted=true;
				return Enum.ToObject(target,((IMap)meta).Number.Int);
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
		public static object ToDotNet(object meta,Type target)
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
		// TODO: maybe convert .NET arrays to maps
		public static IMap ToMeta(object oDotNet) // TODO: refactor, make single-exit
		{ 
			if(oDotNet==null)
			{
				return null;
			}
			else if(oDotNet.GetType().IsSubclassOf(typeof(Enum)))
			{
				return new StrategyMap(new Integer((int)System.Convert.ToInt32((Enum)oDotNet)));
			}
			ToMeta conversion=(ToMeta)toMeta[oDotNet.GetType()];
			if(conversion==null)
			{
				if(oDotNet is IMap || Helper.IsNumber(oDotNet))
				{
					return (IMap)oDotNet;
				}
				else
				{
					return new DotNetObject(oDotNet);
					//return new StrategyMap(oDotNet);
				}
			}
			else
			{
				return conversion.Convert(oDotNet);
			}
		}
		public static object ToDotNet(object meta) 
		{
			if(Helper.IsNumber(meta))
			{
				return ((IMap)meta).Number.Int;
			}
			else if(meta is IMap && ((IMap)meta).IsString)
			{
				return ((IMap)meta).String;
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
		public abstract object Convert(object obj,out bool converted);
	}
	abstract class ToMetaConversions
	{
		public class ConvertStringToMap: ToMeta
		{
			public ConvertStringToMap()  
			{
				this.source=typeof(string);
			}
			public override IMap Convert(object toConvert)
			{
				return new StrategyMap((string)toConvert);
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
				return (bool)toConvert? new StrategyMap(1): new StrategyMap(0);
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
				return new StrategyMap(new Integer((Byte)toConvert));
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
				return new StrategyMap(new Integer((SByte)toConvert));
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
				return new StrategyMap(new Integer((Char)toConvert));
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
				return new StrategyMap(new Integer((Int32)toConvert));
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
				return new StrategyMap(new Integer((UInt32)toConvert));
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
				return new StrategyMap(new Integer((Int64)toConvert));
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
				return new StrategyMap(new Integer((Int64)(UInt64)toConvert));
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
				return new StrategyMap(new Integer((Int16)toConvert));
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
				return new StrategyMap(new Integer((UInt16)toConvert));
			}
		}
	}
		abstract class ToDotNetConversions
		{
			public class IntegerToByte: ToDotNet
			{
				public IntegerToByte()
				{
					this.source=typeof(StrategyMap); // TODO: this isnt quite accurate, should be able to convert every IMap
					//this.source=typeof(Integer);
					this.target=typeof(Byte);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToByte(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToBool: ToDotNet
			{
				public IntegerToBool()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(bool);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					object result;
					int i=((IMap)toConvert).Number.Int;
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
					this.source=typeof(StrategyMap);
					this.target=typeof(SByte);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToSByte(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToChar: ToDotNet
			{
				public IntegerToChar()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(Char);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToChar(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToInt32: ToDotNet
			{
				public IntegerToInt32()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(Int32);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					if(((IMap)toConvert).Number!=null)
					{
						isConverted=true;
//						isConverted=true;
						return System.Convert.ToInt32(((IMap)toConvert).Number.LongValue());
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
					this.source=typeof(StrategyMap);
					this.target=typeof(UInt32);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					if(((IMap)toConvert).Number!=null)
					{
						isConverted=true;
						return System.Convert.ToUInt32(((IMap)toConvert).Number.LongValue());
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
					this.source=typeof(StrategyMap);
					this.target=typeof(Int64);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToInt64(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToUInt64: ToDotNet
			{
				public IntegerToUInt64()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(UInt64);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToUInt64(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToInt16: ToDotNet
			{
				public IntegerToInt16()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(Int16);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToInt16(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToUInt16: ToDotNet
			{
				public IntegerToUInt16()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(UInt16);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToUInt16(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToDecimal: ToDotNet
			{
				public IntegerToDecimal()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(decimal);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (decimal)(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToDouble: ToDotNet
			{
				public IntegerToDouble()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(double);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (double)(((IMap)toConvert).Number.LongValue());
				}
			}
			public class IntegerToFloat: ToDotNet
			{
				public IntegerToFloat()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(float);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (float)(((IMap)toConvert).Number.LongValue());
				}
			}
			public class MapToString: ToDotNet
			{
				public MapToString()
				{
					this.source=typeof(StrategyMap);
					this.target=typeof(string);
				}
//
//				public MapToString()
//				{
//					this.source=typeof(IMap);
//					this.target=typeof(string);
//				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					if(((IMap)toConvert).IsString)
					{
						isConverted=true;
						return ((IMap)toConvert).String;
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
					this.source=typeof(StrategyMap);  // TODO: think about whether this should really be StrategyMap?? better would be IsSubClass test, or something like that
					this.target=typeof(decimal); 
				}
//				public FractionToDecimal()
//				{
//					this.source=typeof(IMap); 
//					this.target=typeof(decimal); 
//				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					IMap map=(IMap)toConvert;
					if(Helper.IsNumber(map[new StrategyMap("numerator")]) && Helper.IsNumber(map[new StrategyMap("denominator")]))
					{
						isConverted=true;
						return ((decimal)((IMap)map[new StrategyMap("numerator")]).Number.LongValue())/((decimal)((IMap)map[new StrategyMap("denominator")]).Number.LongValue());
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
					this.source=typeof(StrategyMap);
					this.target=typeof(double);
				}
//				public FractionToDouble()
//				{
//					this.source=typeof(IMap);
//					this.target=typeof(double);
//				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					IMap map=(IMap)toConvert;
					if(Helper.IsNumber(map[new StrategyMap("numerator")]) && Helper.IsNumber(map[new StrategyMap("denominator")]))
					{
						isConverted=true;
						return ((double)((IMap)map[new StrategyMap("numerator")]).Number.LongValue())/((double)((IMap)map[new StrategyMap("denominator")]).Number.LongValue());
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
					this.source=typeof(StrategyMap);
					this.target=typeof(float);
				}
//				public FractionToFloat()
//				{
//					this.source=typeof(IMap);
//					this.target=typeof(float);
//				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					IMap map=(IMap)toConvert;
					if(Helper.IsNumber(map[new StrategyMap("numerator")]) && Helper.IsNumber(map[new StrategyMap("denominator")]))
					{
						isConverted=true;
						return ((float)((IMap)map[new StrategyMap("numerator")]).Number.LongValue())/((float)((IMap)map[new StrategyMap("denominator")]).Number.LongValue());
					}
					else
					{
						isConverted=false;
						return null;
					}
				}
			}
		}
	public class MapAdapterEnumerator: IEnumerator
	{
		private MapAdapter map; // TODO: rename
		public MapAdapterEnumerator(MapAdapter map)
		{
			this.map=map;
		}
		public object Current
		{
			get
			{
				return new DictionaryEntry(map.Keys[index],map[map.Keys[index]]);
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
	public class MapAdapter
	{ 
		private IMap map;
		public MapAdapter(IMap map)
		{
			this.map=map;
		}
		//		public override object Call(object argument)
		//		{
		//			return map.Call(argument);
		//		}

		public MapAdapter()
		{
			this.map=new StrategyMap();
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
//		public IMap Clone()
//		{
//			return new MapAdapter((IMap)map.Clone());
//		}
		private ArrayList ConvertToMeta(ArrayList list)
		{
			ArrayList result=new ArrayList();
			foreach(object obj in list)
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
		public IEnumerator GetEnumerator() // TODO: shouldnt be necessary to override this
		{
			return new MapAdapterEnumerator(this);
		}
	}
//	public class MapAdapter:IMap
//	{ 
//		private IMap map;
//		public MapAdapter(IMap map)
//		{
//			this.map=map;
//		}
////		public override object Call(object argument)
////		{
////			return map.Call(argument);
////		}
//
//		public MapAdapter()
//		{
//			this.map=new StrategyMap();
//		}
//		public override bool ContainsKey(object key)
//		{
//			return map.ContainsKey(Convert.ToMeta(key));
//		}
//
//		public override object this[object key]
//		{
//			get
//			{
//				return Convert.ToDotNet(map[Convert.ToMeta(key)]);
//			}
//			set
//			{
//				this.map[Convert.ToMeta(key)]=Convert.ToMeta(value);
//			}
//		}
//		public override IMap Parent
//		{
//			get
//			{
//				return (IMap)Convert.ToMeta(map.Parent);
//			}
//			set
//			{
//				map.Parent=(IMap)Convert.ToDotNet(value);
//			}
//		}
//		public override ArrayList Array
//		{
//			get
//			{
//				return ConvertToMeta(map.Array);
//			}
//		}
//		public override IMap Clone()
//		{
//			return new MapAdapter((IMap)map.Clone());
//		}
//		private ArrayList ConvertToMeta(ArrayList list)
//		{
//			ArrayList result=new ArrayList();
//			foreach(object obj in list)
//			{
//				result.Add(Convert.ToDotNet(obj));
//			}
//			return result;
//		}
//		public override ArrayList Keys
//		{
//			get
//			{
//				return ConvertToMeta(map.Keys);
//			}
//		}
//		public override int Count
//		{
//			get
//			{
//				return map.Count;
//			}
//		}
//		public override IEnumerator GetEnumerator()
//		{
//			return new MapEnumerator(this);
//		}
//	}
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
//	public class MapEnumerator: IEnumerator
//	{
//		private IMap map; 
//		public MapEnumerator(IMap map)
//		{
//			this.map=map;
//		}
//		public object Current
//		{
//			get
//			{
//				return new DictionaryEntry(map.Keys[index],map[map.Keys[index]]);
//			}
//		}
//		public bool MoveNext()
//		{
//			index++;
//			return index<map.Count;
//		}
//		public void Reset()
//		{
//			index=-1;
//		}
//		private int index=-1;
//	}
	public delegate object DelegateCreatedForGenericDelegates(); // TODO: rename?
	public class DotNetMethod: IMap,ICallable
	{
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




		public static object AssignCollection(IMap map,object collection,out bool isSuccess) // TODO: of somewhat doubtful value, only for control and form initialization, should be removed, I think, need to write more extensive wrappers around the .NET libraries anyway
		{ 
			if(map.Array.Count==0)
			{
				isSuccess=false;
				return null;
			}
			Type targetType=collection.GetType();
			MethodInfo add=targetType.GetMethod("Add",new Type[]{map.Array[0].GetType()});
			if(add!=null)
			{
				foreach(object entry in map.Array)
				{ 
					// TODO: combine this with Library function "Init"
					add.Invoke(collection,new object[]{entry});//  call add from above!
				}
				isSuccess=true;
			}
			else
			{
				isSuccess=false;
			}
			return collection;
		}
		public static object ConvertParameter(object meta,Type parameter,out bool isConverted)
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
				Delegate function=CreateDelegateFromCode(parameter,invoke,(IMap)meta);
				return function;
			}
			else if(parameter.IsArray && meta is IMap && ((IMap)meta).Array.Count!=0)
			{
				try
				{
					Type type=parameter.GetElementType();
					IMap argument=((IMap)meta);
					Array arguments=System.Array.CreateInstance(type,argument.Array.Count);
					for(int i=0;i<argument.Count;i++)
					{
						arguments.SetValue(argument[new StrategyMap(new Integer(i+1))],i);
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
		public IMap Call(IMap argument)
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
				ArrayList rightNumberArgumentMethods=new ArrayList();
				foreach(MethodBase method in overloadedMethods)
				{
					if(((IMap)argument).Array.Count==method.GetParameters().Length)
					{ 
						if(((IMap)argument).Array.Count==((IMap)argument).Keys.Count)
						{ 
							rightNumberArgumentMethods.Add(method);
						}
					}
				}
				if(rightNumberArgumentMethods.Count==0)
				{
					throw new ApplicationException("Method "+this.name+": No methods with the right number of arguments.");
				}
				foreach(MethodBase method in rightNumberArgumentMethods)
				{
					ArrayList arguments=new ArrayList();
					bool argumentsMatched=true;
					ParameterInfo[] arPrmtifParameters=method.GetParameters();
					for(int i=0;argumentsMatched && i<arPrmtifParameters.Length;i++)
					{
						arguments.Add(ConvertParameter(((IMap)argument).Array[i],arPrmtifParameters[i].ParameterType,out argumentsMatched));
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
			string argumentBuiling="IMap arg=new StrategyMap();";
			if(method!=null)
			{
				foreach(ParameterInfo parameter in method.GetParameters()) // TODO: maybe iterate here twice
				{
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentBuiling+="arg[new StrategyMap(new Integer("+counter+"))]=Meta.Convert.ToMeta(arg"+counter+");"; // TODO: not sure a DotNetObject should always be created
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
			source+="object result=callable.Call(arg);";
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
//	public class DotNetMethod: ICallable
//	{
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
//		public static object ConvertParameter(object meta,Type parameter,out bool isConverted)
//		{
//			isConverted=true;
//			if(parameter.IsAssignableFrom(meta.GetType()))
//			{
//				return meta;
//			}
//			else if((parameter.IsSubclassOf(typeof(Delegate))
//				||parameter.Equals(typeof(Delegate))) && (meta is IMap))
//			{
//				MethodInfo invoke=parameter.GetMethod("Invoke",BindingFlags.Instance
//					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
//				Delegate function=CreateDelegateFromCode(parameter,invoke,(IMap)meta);
//				return function;
//			}
//			else if(parameter.IsArray && meta is IMap && ((IMap)meta).Array.Count!=0)
//			{
//				try
//				{
//					Type type=parameter.GetElementType();
//					IMap argument=((IMap)meta);
//					Array arguments=Array.CreateInstance(type,argument.Array.Count);
//					for(int i=0;i<argument.Count;i++)
//					{
//						arguments.SetValue(argument[new StrategyMap(new Integer(i+1))],i);
//					}
//					return arguments;
//				}
//				catch
//				{
//				}
//			}
//			else
//			{
//				bool converted;
//				object result=Convert.ToDotNet(meta,parameter,out converted);
//				if(converted)
//				{
//					return result;
//				}
//			}
//			isConverted=false;
//			return null;
//		}
//		public class ArgumentComparer: IComparer
//		{
//			public int Compare(object x, object y)
//			{
//				int result;
//				MethodBase first=(MethodBase)x;
//				MethodBase second=(MethodBase)y;
//				ParameterInfo[] firstParameters=first.GetParameters();
//				ParameterInfo[] secondParameters=second.GetParameters();
//				if(firstParameters.Length==1 && firstParameters[0].ParameterType==typeof(string)
//					&& !(secondParameters.Length==1 && secondParameters[0].ParameterType==typeof(string)))
//				{
//					result=-1;
//				}
//				else
//				{
//					result=0;
//				}
//				return result;
//			}
//		}
//		public object Call(object argument)
//		{
//			object result=null;
//
//			ArrayList oneArgumentMethods=new ArrayList();
//			foreach(MethodBase method in overloadedMethods)
//			{
//				if(method.GetParameters().Length==1)
//				{ 
//					oneArgumentMethods.Add(method);
//				}
//			}
//			bool isExecuted=false;
//			oneArgumentMethods.Sort(new ArgumentComparer());
//			foreach(MethodBase method in oneArgumentMethods)
//			{
//				bool isConverted;
//				object parameter=ConvertParameter(argument,method.GetParameters()[0].ParameterType,out isConverted);
//				if(isConverted)
//				{
//					if(method is ConstructorInfo)
//					{
//						result=((ConstructorInfo)method).Invoke(new object[] {parameter});
//					}
//					else
//					{
//						result=method.Invoke(target,new object[] {parameter});
//					}
//					isExecuted=true;
//					break;
//				}
//			}
//			if(!isExecuted)
//			{
//				ArrayList rightNumberArgumentMethods=new ArrayList();
//				foreach(MethodBase method in overloadedMethods)
//				{
//					if(((IMap)argument).Array.Count==method.GetParameters().Length)
//					{ 
//						if(((IMap)argument).Array.Count==((IMap)argument).Keys.Count)
//						{ 
//							rightNumberArgumentMethods.Add(method);
//						}
//					}
//				}
//				if(rightNumberArgumentMethods.Count==0)
//				{
//					throw new ApplicationException("Method "+this.name+": No methods with the right number of arguments.");
//				}
//				foreach(MethodBase method in rightNumberArgumentMethods)
//				{
//					ArrayList arguments=new ArrayList();
//					bool argumentsMatched=true;
//					ParameterInfo[] arPrmtifParameters=method.GetParameters();
//					for(int i=0;argumentsMatched && i<arPrmtifParameters.Length;i++)
//					{
//						arguments.Add(ConvertParameter(((IMap)argument).Array[i],arPrmtifParameters[i].ParameterType,out argumentsMatched));
//					}
//					if(argumentsMatched)
//					{
//						if(method is ConstructorInfo)
//						{
//							result=((ConstructorInfo)method).Invoke(arguments.ToArray());
//						}
//						else
//						{
//							if(this.name=="Invoke")
//							{
//								int asdf=0;
//							}
//							result=method.Invoke(target,arguments.ToArray());
//						}
//						isExecuted=true;
//						break;
//					}
//				}
//			}
//			if(!isExecuted)
//			{
//				throw new ApplicationException("Method "+this.name+" could not be called.");
//			}
//			return Convert.ToMeta(result);
//		}
//		public static Delegate CreateDelegateFromCode(Type delegateType,MethodInfo method,IMap code)
//		{
//			CSharpCodeProvider codeProvider=new CSharpCodeProvider();
//			ICodeCompiler compiler=codeProvider.CreateCompiler();
//			string returnType;
//			if(method==null)
//			{
//				returnType="object";
//			}
//			else
//			{
//				returnType=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
//			}
//			string source="using System;using Meta;";
//			source+="public class EventHandlerContainer{public "+returnType+" EventHandlerMethod";
//			int counter=1;
//			string argumentList="(";
//			string argumentBuiling="IMap arg=new StrategyMap();";
//			if(method!=null)
//			{
//				foreach(ParameterInfo parameter in method.GetParameters())
//				{
//					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
//					argumentBuiling+="arg[new StrategyMap(new Integer("+counter+"))]=arg"+counter+";";
//					if(counter<method.GetParameters().Length)
//					{
//						argumentList+=",";
//					}
//					counter++;
//				}
//			}
//			argumentList+=")";
//			source+=argumentList+"{";
//			source+=argumentBuiling;
//			source+="object result=callable.Call(arg);";
//			if(method!=null)
//			{
//				if(!method.ReturnType.Equals(typeof(void)))
//				{
//					source+="return ("+returnType+")";
//					source+="Meta.Convert.ToDotNet(result,typeof("+returnType+"));"; 
//				}
//			}
//			else 
//			{
//				source+="return";
//				source+=" result;";
//			}
//			source+="}";
//			source+="private IMap callable;";
//			source+="public EventHandlerContainer(IMap callable) {this.callable=callable;}}";
//			string metaDllLocation=Assembly.GetAssembly(typeof(IMap)).Location;
//			ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
//			assemblyNames.AddRange(Interpreter.loadedAssemblies);
//			CompilerParameters compilerParameters=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
//			CompilerResults compilerResults=compiler.CompileAssemblyFromSource(compilerParameters,source);
//			Type containerType=compilerResults.CompiledAssembly.GetType("EventHandlerContainer",true);
//			object container=containerType.GetConstructor(new Type[]{typeof(IMap)}).Invoke(new object[] {code});
//			if(method==null)
//			{
//				delegateType=typeof(DelegateCreatedForGenericDelegates);
//			}
//			Delegate result=Delegate.CreateDelegate(delegateType,
//				container,"EventHandlerMethod");
//			return result;
//		}
//		private void Initialize(string name,object target,Type targetType)
//		{
//			this.name=name;
//			this.target=target;
//			this.targetType=targetType;
//			ArrayList methods;
//			if(name==".ctor")
//			{
//				methods=new ArrayList(targetType.GetConstructors());
//			}
//			else
//			{
//				methods=new ArrayList(targetType.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
//			}
//			overloadedMethods=(MethodBase[])methods.ToArray(typeof(MethodBase));
//		}
//		public DotNetMethod(string name,object target,Type targetType)
//		{
//			this.Initialize(name,target,targetType);
//		}
//		public DotNetMethod(Type targetType)
//		{
//			this.Initialize(".ctor",null,targetType);
//		}
//		public override bool Equals(object toCompare)
//		{
//			if(toCompare is DotNetMethod)
//			{
//				DotNetMethod DotNetMethod=(DotNetMethod)toCompare;
//				if(DotNetMethod.target==target && DotNetMethod.name.Equals(name) && DotNetMethod.targetType.Equals(targetType))
//				{
//					return true;
//				}
//				else
//				{
//					return false;
//				}
//			}
//			else
//			{
//				return false;
//			}
//		}
//		public override int GetHashCode()
//		{
//			unchecked
//			{
//				int hash=name.GetHashCode()*targetType.GetHashCode();
//				if(target!=null)
//				{
//					hash=hash*target.GetHashCode();
//				}
//				return hash;
//			}
//		}
//		private string name;
//		protected object target;
//		protected Type targetType;
//
//		public MethodBase[] overloadedMethods;
//	}
	public class DotNetClass: DotNetContainer
	{
		public override IMap Clone()
		{
			return new DotNetClass(type);
		}

//		public override IMap CloneMap()
//		{
//			return new StrategyMap(type);
//		}
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
//		public override IMap CloneMap()
//		{
//			return new StrategyMap(obj);
//		}

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
//		public override MapStrategy Clone()
//		{
//			return new DotNetObject(obj); // TODO: is this correct?
//		}

	}
	public abstract class MapStrategy:ISerializeSpecial // TODO: maybe rename to MapImplementation, look at Patterns Book
	{
//		public virtual bool IsNumber
//		{
//			get
//			{
//				return false;
//			}
//		}
		public abstract Integer Number
		{
			get;
		}

//		public virtual Integer Number
//		{
//			get
//			{
//				return null;
//				//throw new ApplicationException("Map is not a number.");
//			}
//		}

//		public MapStrategy(string namespaceName,Hashtable subNamespaces,ArrayList assemblies):this(new LazyNamespace(namespaceName,subNamespaces,assemblies))
//		{
//		}
//		public MapStrategy(object obj):this(new DotNetObject(obj))
//		{
//		}
//		public MapStrategy(Type type):this(new DotNetClass(type))
//		{
//		}
//		public MapStrategy(string text):this(new StringStrategy(text))
//		{
//		}
//		public MapStrategy():this(new HybridDictionaryStrategy())
//		{
//		}
//		public MapStrategy(MapStrategy strategy)
//		{
//			this.strategy=strategy;
//			this.strategy.map=this.map;
//		}
//		public MapStrategy strategy;
		public virtual void Serialize(string indentation,string[] functions,StringBuilder stringBuilder)
		{
			if(this.IsString)
			{
				stringBuilder.Append(indentation+"\""+this.String+"\""+"\n");
			}
			else if(this.Number!=null)
			{
				stringBuilder.Append(indentation+"\""+this.Number.ToString()+"\""+"\n"); // TODO: this should maybe be moved into IMap
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
			Expression function=(Expression)((IMap)this[CodeKeys.Run]).GetExpression();
			IMap result;
			result=function.Evaluate(map);
			return result;
		}
		public StrategyMap map;

		// TODO: think about clone and CloneMap
		public virtual MapStrategy Clone() // TODO: maybe make this abstract??? really not very reliable
		{
			MapStrategy strategy=new HybridDictionaryStrategy();
			foreach(IMap key in this.Keys)
			{
				strategy[key]=this[key];
			}
			return strategy;	
		}
//		public MapStrategy Clone()
//		{
//			MapStrategy strategy=new HybridDictionaryStrategy();
//			foreach(object key in this.Keys)
//			{
//				strategy[key]=this[key];
//			}
//			return strategy;	
//		}
		public abstract IMap CloneMap();// TODO: why is this needed
		public abstract ArrayList Array
		{
			get;
		}
		public virtual bool IsString
		{
			get
			{
				return false;
			}
		}
		public virtual string String
		{
			get
			{
				return "";
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
		public override int GetHashCode()  // TODO: duplicated with IMap
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

			if(Object.ReferenceEquals(strategy,this))
			{ 
				return true;
			}
			else if(!(strategy is MapStrategy))
			{
				return false;
			}
			else if(((MapStrategy)strategy).Count!=this.Count)
			{
				return false;
			}
			foreach(IMap key in this.Keys) 
			{
				if(!((MapStrategy)strategy).ContainsKey(key)||!((MapStrategy)strategy)[key].Equals(this[key]))
				{
					return false;
				}
			}
			return true;
		}
	}
	public class StringStrategy:MapStrategy
	{
		public override Integer Number
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
			return new StrategyMap(new StringStrategy(this));
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList list=new ArrayList();
				foreach(char iChar in text)
				{
					list.Add(new StrategyMap(new Integer(iChar)));
				}
				return list;
			}
		}
		public override bool IsString
		{
			get
			{
				return true;
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
				keys.Add(new StrategyMap(new Integer(i)));			
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
				if(Helper.IsNumber(key))
				{
					int iInteger=((IMap)key).Number.Int;
					if(iInteger>0 && iInteger<=this.Count)
					{
						return new StrategyMap(new Integer(text[iInteger-1]));
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
			if(Helper.IsNumber(key))
			{
				return ((IMap)key).Number>0 && ((IMap)key).Number<=this.Count;
			}
			else
			{
				return false;
			}
		}
	}
	public class HybridDictionaryStrategy:MapStrategy
	{
//		public override bool IsNumber
//		{
//			get
//			{
//				bool isNumber;
//				if(this.map.Equals(NumberKeys.EmptyMap))
//				{
//					isNumber=true;
//				}
//				else if(this.Count==1 && this.ContainsKey(NumberKeys.EmptyMap))
//				{
//					isNumber=true;
//				}
//				else
//				{
//					isNumber=false;
//				}
//				return isNumber;
//			}
//		}
		public override Integer Number
		{
			get
			{
				Integer number;
				if(this.map.Equals(NumberKeys.EmptyMap))
				{
					number=0;
				}
				else if((this.Count==1 || (this.Count==2 && this.ContainsKey(NumberKeys.Negative))) && this.ContainsKey(NumberKeys.EmptyMap))
				{
					if(this[NumberKeys.EmptyMap] is IMap)
					{
						if(((IMap)this[NumberKeys.EmptyMap]).Number!=null)
						{
							number=((IMap)this[NumberKeys.EmptyMap]).Number+1;
							if(this.ContainsKey(NumberKeys.Negative))
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
				}
				else
				{
					number=null;
				}
				return number;
			}
		}


		ArrayList keys;
		private HybridDictionary strategy;
		public HybridDictionaryStrategy():this(2)
		{
		}
		public HybridDictionaryStrategy(int Count)
		{
			this.keys=new ArrayList(Count);
			this.strategy=new HybridDictionary(Count);
		}
		public override IMap CloneMap()
		{
			IMap clone=new StrategyMap(new HybridDictionaryStrategy(this.keys.Count));
			foreach(IMap key in keys)
			{
				clone[key]=(IMap)strategy[key];
			}
			return clone;
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList list=new ArrayList();
				for(Integer iInteger=new Integer(1);ContainsKey(new StrategyMap(iInteger));iInteger++)
				{
					list.Add(this[new StrategyMap(iInteger)]);
				}
				return list;
			}
		}
		public override bool IsString
		{
			get
			{
				bool isString=false;;
				if(Array.Count>0)
				{
					try
					{
						object o=String;
						isString=true;
					}
					catch
					{
					}
				}
				return isString;
			}
		}
		public override string String
		{
			get
			{
				string text="";
				foreach(object key in this.Keys)
				{
					if(Helper.IsNumber(key) && Helper.IsNumber(this.strategy[key]))
					{
						try
						{
							text+=System.Convert.ToChar(((IMap)this.strategy[key]).Number.Int);
						}
						catch
						{
							throw new MapException(this.map,"Map is not a string");// TODO: exception throwing isnt great
						}
					}
					else
					{
						throw new MapException(this.map,"Map is not a string");
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
				return strategy.Count;
			}
		}
		public override IMap this[IMap key] 
		{
			get
			{
				return (IMap)strategy[key];
			}
			set
			{
				if(!this.ContainsKey(key))
				{
					keys.Add(key);
				}
				strategy[key]=value;
			}
		}
		public override bool ContainsKey(IMap key) 
		{
			return strategy.Contains(key);
		}
	}
	public abstract class DotNetContainer: IMap, ISerializeSpecial
	{
		public void Serialize(string indentation, string[] functions, StringBuilder stringBuilder)
		{
			ExecuteTests.Serialize(obj!=null?this.obj:this.type,indentation,functions,stringBuilder);
			//stringBuilder.Append(indentation+this.type.FullName);
		}
		public override ArrayList Array
		{
			get
			{
				ArrayList array=new ArrayList();
				foreach(IMap key in Keys)
				{
					if(Helper.IsNumber(key))
					{
						array.Add(this[key]);
					}
				}
				return array;
			}
		}
		public override bool ContainsKey(IMap key)
		{
			if(key is IMap)
			{
				if(((IMap)key).IsString)
				{
					string text=((IMap)key).String;
					if(type.GetMember(((IMap)key).String,
						BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
					{
						return true;
					}
				}
			}
			DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
			IMap argument=new StrategyMap(); // TODO: refactor
			argument[new StrategyMap(new Integer(1))]=key;
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
				if(key is IMap && ((IMap)key).IsString && type.GetMember(((IMap)key).String,bindingFlags).Length>0)
				{
					string text=((IMap)key).String; // TODO: There are a few too many empty parent maps around here
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
						try // TODO: fix this? refactor? what is this even supposed to do?
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
						//result=new StrategyMap((Type)members[0]);
					}
					else
					{
						result=null;
					}
				}
				else if(this.obj!=null && Helper.IsNumber(key) && this.type.IsArray)
				{
					result=Convert.ToMeta(((Array)obj).GetValue(((IMap)key).Number.Int));
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
					IMap argument=new StrategyMap(); // refactor
					argument[new StrategyMap(new Integer(1))]=key;
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
			// TODO: refactor
			set
			{
				if(key is IMap && ((IMap)key).IsString && type.GetMember(((IMap)key).String,bindingFlags).Length!=0)
				{
					string text=((IMap)key).String;
					if(text.Equals("Text"))
					{
						int asdf=0;
					}
					MemberInfo[] members=type.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						throw new ApplicationException("Cannot set MethodeBase "+key+".");
					}
					else if(members[0] is FieldInfo)
					{
						FieldInfo field=(FieldInfo)members[0];
						bool isConverted;
						object val;
						val=DotNetMethod.ConvertParameter(value,field.FieldType,out isConverted);
						if(isConverted)
						{
							field.SetValue(obj,val);
						}
						else
						{
							if(value is IMap)
							{
								// TODO: do not reuse isConverted
								// TODO: really do this? does not make much sense
								val=DotNetMethod.AssignCollection((IMap)value,field.GetValue(obj),out isConverted);
							}
						}
						if(!isConverted)
						{
							throw new ApplicationException("Field "+field.Name+"could not be assigned because it cannot be converted.");
						}
					}
					else if(members[0] is PropertyInfo)
					{
						PropertyInfo property=(PropertyInfo)members[0];
						bool isConverted;
						object val=DotNetMethod.ConvertParameter(value,property.PropertyType,out isConverted);
						if(isConverted)
						{
							property.SetValue(obj,val,new object[]{});
						}
						else
						{
							if(value is IMap)
							{
								DotNetMethod.AssignCollection((IMap)value,property.GetValue(obj,new object[]{}),out isConverted);
							}
							if(!isConverted)
							{
								throw new ApplicationException("Property "+this.type.Name+"."+Interpreter.SaveToFile(key,"",false)+" could not be set to "+value.ToString()+". The value can not be isConverted.");
							}
						}
						return;
					}
					else if(members[0] is EventInfo)
					{
						((EventInfo)members[0]).AddEventHandler(obj,CreateEvent(text,(IMap)value));
					}
					else
					{
						throw new ApplicationException("Could not assign "+text+" .");
					}
				}
				else if(obj!=null && Helper.IsNumber(key) && type.IsArray)
				{
					bool isConverted; 
					object converted=Convert.ToDotNet(value,type.GetElementType(),out isConverted);
					if(isConverted)
					{
						((Array)obj).SetValue(converted,((IMap)key).Number.Int);
						return;
					}
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("set_Item",obj,type); // TODO: refactor
					IMap argument=new StrategyMap();// TODO: refactor
					argument[new StrategyMap(new Integer(1))]=key;
					argument[new StrategyMap(new Integer(2))]=value;
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
		public Delegate CreateEvent(string name,IMap code)
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
					table[new StrategyMap(field.Name)]=field.GetValue(obj);
				}
				foreach(MethodInfo invoke in type.GetMethods(bindingFlags)) 
				{
					if(!invoke.IsSpecialName)
					{
						table[new StrategyMap(invoke.Name)]=new DotNetMethod(invoke.Name,obj,type);
					}
				}
				foreach(PropertyInfo property in type.GetProperties(bindingFlags))
				{
					if(property.Name!="Item" && property.Name!="Chars")
					{
						table[new StrategyMap(property.Name)]=property.GetValue(obj,new object[]{});
					}
				}
				foreach(EventInfo eventInfo in type.GetEvents(bindingFlags))
				{
					table[new StrategyMap(eventInfo.Name)]=new DotNetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
				}
				foreach(Type nestedType in type.GetNestedTypes(bindingFlags))
				{ 
					table[new StrategyMap(nestedType.Name)]=new DotNetClass(nestedType);
					//table[new StrategyMap(nestedType.Name)]=new StrategyMap(nestedType);
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
							table[new StrategyMap(new Integer(counter))]=entry;
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
//	public abstract class DotNetContainer: MapStrategy, ISerializeSpecial
//	{
//		public override void Serialize(string indentation, string[] functions, StringBuilder stringBuilder)
//		{
//			ExecuteTests.Serialize(obj!=null?this.obj:this.type,indentation,functions,stringBuilder);
//			//stringBuilder.Append(indentation+this.type.FullName);
//		}
//		public override ArrayList Array
//		{
//			get
//			{
//				ArrayList array=new ArrayList();
//				foreach(object key in Keys)
//				{
//					if(key is Integer)
//					{
//						array.Add(this[key]);
//					}
//				}
//				return array;
//			}
//		}
//		public override bool ContainsKey(object key)
//		{
//			if(key is IMap)
//			{
//				if(((IMap)key).IsString)
//				{
//					string text=((IMap)key).String;
//					if(type.GetMember(((IMap)key).String,
//						BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
//					{
//						return true;
//					}
//				}
//			}
//			DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
//			IMap argument=new StrategyMap(); // TODO: refactor
//			argument[new Integer(1)]=key;
//			try
//			{
//				indexer.Call(argument);
//				return true;
//			}
//			catch(Exception)
//			{
//				return false;
//			}
//		}
//		public override ArrayList Keys
//		{
//			get
//			{
//				return new ArrayList(MTable.Keys);
//			}
//		}
//		public override int Count 
//		{
//			get
//			{
//				return MTable.Count;
//			}
//		}
//		public override object this[object key] 
//		{
//			get
//			{
//				object result;
//				if(key is IMap && ((IMap)key).IsString && type.GetMember(((IMap)key).String,bindingFlags).Length>0)
//				{
//					string text=((IMap)key).String; // TODO: There are a few too many empty parent maps around here
//					MemberInfo[] members=type.GetMember(text,bindingFlags);
//					if(members[0] is MethodBase)
//					{
//						result=new DotNetMethod(text,obj,type);
//					}
//					else if(members[0] is FieldInfo)
//					{
//						result=Convert.ToMeta(type.GetField(text).GetValue(obj));
//					}
//					else if(members[0] is PropertyInfo)
//					{
//						result=Convert.ToMeta(type.GetProperty(text).GetValue(obj,new object[]{}));
//					}
//					else if(members[0] is EventInfo)
//					{
//						try // TODO: fix this? refactor? what is this even supposed to do?
//						{
//							Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
//								BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
//							if(eventDelegate==null)
//							{
//								result=null;
//							}
//							else
//							{
//								result=new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
//							}
//						}
//						catch
//						{
//							result=null;
//						}
//					}
//					else if(members[0] is Type)
//					{
//						result=new StrategyMap((Type)members[0]);
//					}
//					else
//					{
//						result=null;
//					}
//				}
//				else if(this.obj!=null && key is Integer && this.type.IsArray)
//				{
//					result=Convert.ToMeta(((Array)obj).GetValue(((Integer)key).Int));
//				}
//				else
//				{
//					DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
//					IMap argument=new StrategyMap(); // refactor
//					argument[new Integer(1)]=key;
//					try
//					{
//						result=Convert.ToMeta(indexer.Call(argument));
//					}
//					catch(Exception e)
//					{
//						result=null;
//					}
//				}
//				return result;
//			}
//			// TODO: refactor
//			set
//			{
//				if(key is IMap && ((IMap)key).IsString && type.GetMember(((IMap)key).String,bindingFlags).Length!=0)
//				{
//					string text=((IMap)key).String;
//					if(text.Equals("Text"))
//					{
//						int asdf=0;
//					}
//					MemberInfo[] members=type.GetMember(text,bindingFlags);
//					if(members[0] is MethodBase)
//					{
//						throw new ApplicationException("Cannot set MethodeBase "+key+".");
//					}
//					else if(members[0] is FieldInfo)
//					{
//						FieldInfo field=(FieldInfo)members[0];
//						bool isConverted;
//						object val;
//						val=DotNetMethod.ConvertParameter(value,field.FieldType,out isConverted);
//						if(isConverted)
//						{
//							field.SetValue(obj,val);
//						}
//						else
//						{
//							if(value is IMap)
//							{
//								// TODO: do not reuse isConverted
//								// TODO: really do this? does not make much sense
//								val=DotNetMethod.AssignCollection((IMap)value,field.GetValue(obj),out isConverted);
//							}
//						}
//						if(!isConverted)
//						{
//							throw new ApplicationException("Field "+field.Name+"could not be assigned because it cannot be converted.");
//						}
//					}
//					else if(members[0] is PropertyInfo)
//					{
//						PropertyInfo property=(PropertyInfo)members[0];
//						bool isConverted;
//						object val=DotNetMethod.ConvertParameter(value,property.PropertyType,out isConverted);
//						if(isConverted)
//						{
//							property.SetValue(obj,val,new object[]{});
//						}
//						else
//						{
//							if(value is IMap)
//							{
//								DotNetMethod.AssignCollection((IMap)value,property.GetValue(obj,new object[]{}),out isConverted);
//							}
//							if(!isConverted)
//							{
//								throw new ApplicationException("Property "+this.type.Name+"."+Interpreter.SaveToFile(key,"",false)+" could not be set to "+value.ToString()+". The value can not be isConverted.");
//							}
//						}
//						return;
//					}
//					else if(members[0] is EventInfo)
//					{
//						((EventInfo)members[0]).AddEventHandler(obj,CreateEvent(text,(IMap)value));
//					}
//					else
//					{
//						throw new ApplicationException("Could not assign "+text+" .");
//					}
//				}
//				else if(obj!=null && key is Integer && type.IsArray)
//				{
//					bool isConverted; 
//					object converted=Convert.ToDotNet(value,type.GetElementType(),out isConverted);
//					if(isConverted)
//					{
//						((Array)obj).SetValue(converted,((Integer)key).Int);
//						return;
//					}
//				}
//				else
//				{
//					DotNetMethod indexer=new DotNetMethod("set_Item",obj,type); // TODO: refactor
//					IMap argument=new StrategyMap();// TODO: refactor
//					argument[new Integer(1)]=key;
//					argument[new Integer(2)]=value;
//					try
//					{
//						indexer.Call(argument);
//					}
//					catch(Exception e)
//					{
//						throw new ApplicationException("Cannot set "+Convert.ToDotNet(key).ToString()+".");// TODO: change exception
//					}
//				}
//			}
//		}
//		public string Serialize(string indent,string[] functions)
//		{
//			return indent;
//		}
//		public Delegate CreateEvent(string name,IMap code)
//		{
//			EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
//				BindingFlags.Static|BindingFlags.Instance);
//			MethodInfo invoke=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
//				|BindingFlags.Public|BindingFlags.NonPublic);
//			Delegate eventDelegate=DotNetMethod.CreateDelegateFromCode(eventInfo.EventHandlerType,invoke,code);
//			return eventDelegate;
//		}
//		private IDictionary MTable
//		{ 
//			get
//			{
//				HybridDictionary table=new HybridDictionary();
//				foreach(FieldInfo field in type.GetFields(bindingFlags))
//				{
//					table[new StrategyMap(field.Name)]=field.GetValue(obj);
//				}
//				foreach(MethodInfo invoke in type.GetMethods(bindingFlags)) 
//				{
//					if(!invoke.IsSpecialName)
//					{
//						table[new StrategyMap(invoke.Name)]=new DotNetMethod(invoke.Name,obj,type);
//					}
//				}
//				foreach(PropertyInfo property in type.GetProperties(bindingFlags))
//				{
//					if(property.Name!="Item" && property.Name!="Chars")
//					{
//						table[new StrategyMap(property.Name)]=property.GetValue(obj,new object[]{});
//					}
//				}
//				foreach(EventInfo eventInfo in type.GetEvents(bindingFlags))
//				{
//					table[new StrategyMap(eventInfo.Name)]=new DotNetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
//				}
//				foreach(Type nestedType in type.GetNestedTypes(bindingFlags))
//				{ 
//					table[new StrategyMap(nestedType.Name)]=new StrategyMap(nestedType);
//				}
//				int counter=1;
//				if(obj!=null && obj is IEnumerable && !(obj is String))
//				{ 
//					foreach(object entry in (IEnumerable)obj)
//					{
//						if(entry is DictionaryEntry)
//						{
//							table[Convert.ToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
//						}
//						else
//						{
//							table[new Integer(counter)]=entry;
//							counter++;
//						}
//					}
//				}
//				return table;
//			}
//		}
//		public DotNetContainer(object obj,Type type)
//		{
//			if(obj==null)
//			{
//				this.bindingFlags=BindingFlags.Public|BindingFlags.Static;
//			}
//			else
//			{
//				this.bindingFlags=BindingFlags.Public|BindingFlags.Instance;
//			}
//			this.obj=obj;
//			this.type=type;
//		}
//		private BindingFlags bindingFlags;
//		public object obj;
//		public Type type;
//	}
	public class IntegerStrategy:MapStrategy
	{
		private Integer number;
		public override Integer Number
		{
			get
			{
				return number;
			}
		}
		public override bool Equals(object obj)
		{
			bool equals;
			if(obj is IntegerStrategy)
			{
				if(((IntegerStrategy)obj).Number==Number)
				{
					equals=true;
				}
				else
				{
					equals=false;
				}
			}
			else
			{
				equals=base.Equals(obj);
			}
			return equals;
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
			return new StrategyMap(new IntegerStrategy(number));
		}
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				if(number!=0)
				{
					keys.Add(NumberKeys.EmptyMap);
				}
				if(number<0)
				{
					keys.Add(NumberKeys.Negative);
				}
				return keys;
			}
		}
		public override IMap this[IMap key]
		{
			get
			{
				IMap result;
				if(key.Equals(NumberKeys.EmptyMap))
				{
					if(number==0)
					{
						result=new StrategyMap(new Integer(0));
					}
					else
					{
						Integer newNumber;
						Integer absoluteOfNewNumber=number.abs()-1;
						if(number>0)
						{
							newNumber=absoluteOfNewNumber;
						}
						else
						{
							newNumber=-absoluteOfNewNumber;
						}
						result=new StrategyMap(newNumber);
					}
				}
				else if(key.Equals(NumberKeys.Negative))
				{
					if(number<0)
					{
						result=new StrategyMap();
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
				if(key.Equals(NumberKeys.EmptyMap))
				{
					if(value is IMap)// TODO: remove this test, must always be IMap
					{
						IMap map=(IMap)value;
						if(map.Number!=null)
						{

						}
					}
				}
				else if(key.Equals(NumberKeys.Negative))
				{
					if(value==null)
					{
						number=number.abs();
					}
					else if(value.Equals(NumberKeys.EmptyMap))
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
		private void Panic(IMap key,IMap val)// TODO: remove, put into MapStrategy
		{
			map.strategy=this.Clone(); // TODO: move Clone into MapStrategy???, or at least rename
			map.strategy[key]=val;
		}
//		private void Panic(object key,object val)// TODO: remove, put into MapStrategy
//		{
//			map.strategy=this.Clone(); // TODO: move Clone into MapStrategy???, or at least rename
//			map.strategy[key]=val;
//		}
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
		public static bool IsNumber(object obj)
		{
			return obj is IMap && ((IMap)obj).Number!=null;
		}
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
			// TODO: refactor
			// TODO: maybe add class information for DotNetObject, DotNetClass
			// TODO: maybe split this up a bit, we probably need more customization for the serialization
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
								if(toSerialize.GetType().Namespace==null ||!toSerialize.GetType().Namespace.Equals("System.Windows.Forms")) // ugly hack to avoid some srange behaviour of some classes in System.Windows.Forms
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
			return (Extent)extents[extent]; // return the unique extent not the extent itself 
		}
	}
}