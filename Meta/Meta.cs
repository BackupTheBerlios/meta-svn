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
using GAC;
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
		public readonly static Map Literal="literal";
		public readonly static Map Run="run";
		public readonly static Map Call="call";
		public readonly static Map Function="function";
		public readonly static Map Argument="argument";
		public static readonly Map Select="select";
		public static readonly Map Search="search";
		public static readonly Map Key="key";
		public static readonly Map Program="program";
		public static readonly Map Delayed="delayed";
		public static readonly Map Lookup="lookup";
		public static readonly Map Value="value";
	}
	public class SpecialKeys
	{
		public static readonly Map Parent="parent";
		public static readonly Map Arg="arg";
		public static readonly Map This="this";
	}
	public class NumberKeys
	{
		public static readonly Map Denominator="denominator";
		public static readonly Map Numerator="numerator"; // TODO: get rid of this
		public static readonly Map Negative="negative";
		public static readonly Map EmptyMap=new NormalMap();
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
		// TODO: refactor the debugging stuff
		private static BreakPoint breakPoint;

		public virtual bool Stop() // TODO: refactor
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
		public Map Evaluate(Map parent)
		{
			Map result=EvaluateImplementation(parent);
			if(Stop())
			{
				Interpreter.CallDebug(result);
			}
			return result;
		}

		public abstract Map EvaluateImplementation(Map parent);
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

		public override Map EvaluateImplementation(Map parent)
		{
			object function=callable.Evaluate(parent);
			if(function is ICallable)
			{
				return ((ICallable)function).Call(argument.Evaluate(parent));
			}
			throw new MetaException("Object to be called is not callable.",this.Extent);
		}
		public Call(Map code)
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

		public readonly Map delayed;
		public Delayed(Map code)
		{
			this.delayed=code;
		}
		public override Map EvaluateImplementation(Map parent)
		{
			Map result=delayed;
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
		public override Map EvaluateImplementation(Map parent)
		{
			Map local=new NormalMap();
			Evaluate(parent,ref local);
			return local;
		}
		public void Evaluate(Map parent,ref Map local)
		{
			local.Parent=parent;
			for(int i=0;i<statements.Count && i>=0;)
			{
				if(Interpreter.reverseDebug)
				{
					// Statement should have separate Stop() function
					// this sucks, its really not good, refactor
					bool stopReverse=((Statement)statements[i]).Undo();
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
		public Program(Map code)
		{
			object a=code.Array;
			foreach(Map statement in code.Array)
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

	public abstract class Filter
	{
		public abstract Map Detect(string text);
	}
	
	public class Filters
	{
		public class DecimalFilter: Filter
		{
			public override Map Detect(string text)
			{
				Map result=null;
				int pointPos=text.IndexOf(".");
				if(pointPos!=-1)
				{
					if(text.IndexOf(".",pointPos+1)==-1)
					{
						Integer numerator=IntegerFilter.ParseInteger(text.Replace(".",""));
						if(numerator!=null)
						{
							Integer denominator=Convert.ToInt32(Math.Pow(10,text.Length-pointPos-1));
							result=new NormalMap();
							result[NumberKeys.Numerator]=new NormalMap(numerator);
							result[NumberKeys.Denominator]=new NormalMap(denominator);
						}
					}
				}
				return result;
			}
		}
		public class FractionFilter: Filter
		{
			public override Map Detect(string text)
			{
				Map result=null;
				int slashPos=text.IndexOf("/");
				if(slashPos!=-1)
				{
					if(text.IndexOf("/",slashPos+1)==-1)
					{
						Integer numerator=IntegerFilter.ParseInteger(text.Substring(0,slashPos));
						if(numerator!=null)
						{
							Integer denominator=IntegerFilter.ParseInteger(text.Substring(slashPos+1,text.Length-slashPos-1));
							if(denominator!=null)
							{
								result=new NormalMap();
								result[NumberKeys.Numerator]=new NormalMap(numerator);
								result[NumberKeys.Denominator]=new NormalMap(denominator);
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
			public override Map Detect(string text)
			{ 
				Map recognized;
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
			public override Map Detect(string text)
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

		public override Map EvaluateImplementation(Map parent)
		{
			return literal;
		}
		public Literal(Map code)
		{
			this.literal=Filter((string)code.String);
		}
		public Map literal=null;
		public static Map Filter(string text)
		{
			if(text=="1/3")
			{
				int asdf=0;
			}
			foreach(Filter recognition in recognitions)
			{
				Map recognized=recognition.Detect(text);
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
		public Search(Map code)
		{
			this.search=code.GetExpression();
		}
		public Expression search;
		public override Map EvaluateImplementation(Map parent)
		{
			Map key=search.Evaluate(parent);
			Map selected=parent;
			while(!selected.ContainsKey(key))
			{
				if(selected.Parent==null)
				{
					selected.ContainsKey(key);
					throw new KeyNotFoundException(key,this.Extent);
				}
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
		public Select(Map code)
		{
			firstKey=((Map)code.Array[0]).GetExpression();
			foreach(Map key in code.Array.GetRange(1,code.Array.Count-1))
			{
				keys.Add(key.GetExpression());
			}
		}
		public override Map EvaluateImplementation(Map parent)
		{
			Map selected=firstKey.Evaluate(parent);
			for(int i=0;i<keys.Count;i++)
			{
				Map key=((Expression)keys[i]).Evaluate(parent);
				Map selection=selected[key];
				if(selection==null)
				{
					object test=selected[key];
					throw new KeyDoesNotExistException(key,this.Extent,selected);
				}
				selected=selection;
			}
			return selected;
		}	
	}
	public class Statement
	{
		private Map replaceValue;
		private Map replaceMap;
		private Map replaceKey;
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
		public void Realize(ref Map parent)
		{
			Map selected=parent;
			Map key;
			
			if(searchFirst)
			{
				Map firstKey=((Expression)keys[0]).Evaluate(parent); 
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
				Map selection=selected[key];
				if(selection==null)
				{
					throw new KeyDoesNotExistException(key,((Expression)keys[i]).Extent,selected);
				}
				selected=selection;
			}
			Map lastKey=((Expression)keys[keys.Count-1]).Evaluate(parent);
			Map val=expression.Evaluate(parent);
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
		public Statement(Map code) 
		{
			if(code.ContainsKey(CodeKeys.Search))
			{
				searchFirst=true;
			}
			foreach(Map key in code[CodeKeys.Key].Array)
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

		public static Map Merge(params Map[] arkvlToMerge)
		{
			return MergeCollection(arkvlToMerge);
		}
		public static Map MergeCollection(ICollection collection)
		{
			Map result=new NormalMap();
			foreach(Map current in collection)
			{
				foreach(DictionaryEntry entry in current)
				{
					result[(Map)entry.Key]=(Map)entry.Value;
				}
			}
			return result;
		}
		public static Map Run(string fileName,Map argument)
		{
			Map program=Interpreter.Compile(fileName);
			return CallProgram(program,argument,GetPersistantMaps(fileName));
		}
		public static Map GetPersistantMaps(string fileName)
		{
			DirectoryInfo directory=new DirectoryInfo(Path.GetDirectoryName(fileName));
			Map root=new PersistantMap(directory);
			Map current=root;
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
		public static Map RunWithoutLibrary(string fileName,Map argument)
		{
			Map program=Compile(fileName);
			return CallProgram(program,argument,null);
		}
		public static Map CallProgram(Map program,Map argument,Map current)
		{
			Map callable=new NormalMap();
			callable[CodeKeys.Run]=program;
			callable.Parent=current;
			return callable.Call(argument);
		}
		public static Map Compile(string fileName)
		{
			return (new MetaTreeParser()).map(ParseToAst(fileName));
		}
		public static AST ParseToAst(string fileName) 
		{
			FileStream file=new FileStream(fileName, FileMode.Open,FileAccess.Read, FileShare.ReadWrite); 
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
			Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
		}
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
		Map map;
		public MapException(Map map,string message):base(message)
		{
			this.map=map;
		}
	}
	public abstract class KeyException:MetaException
	{ 
		public KeyException(Map key,Extent extent):base(extent)
		{
			message="Key ";
			if(key.IsString)
			{
				message+=key.String;
			}
			else
			{
				message+=Serialize.Key(key);
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
		public KeyNotFoundException(Map key,Extent extent):base(key,extent)
		{
		}
	}
	public class KeyDoesNotExistException:KeyException
	{
		private object selected;
		public KeyDoesNotExistException(Map key,Extent extent,object selected):base(key,extent)
		{
			this.selected=selected;
		}
	}
	public interface ICallable
	{
		Map Call(Map argument);
	}
	public abstract class Map: ICallable, IEnumerable
	{	
		public bool IsFunction
		{
			get
			{
				return ContainsKey(CodeKeys.Run);
			}
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


		public static implicit operator Map(double number)
		{
			return new NormalMap(number);
		}
		public static implicit operator Map(float number)
		{
			return new NormalMap(number);
		}
		public static implicit operator Map(decimal number)
		{
			return new NormalMap(Convert.ToDouble(number));
		}

		public static implicit operator Map(string text)
		{
			return new NormalMap(text);
		}

		public virtual bool IsBoolean
		{
			get
			{
				return IsInteger && (Integer==0 || Integer==1);
			}
		}
		public virtual bool Boolean
		{
			get
			{
				bool boolean;
				if(Integer==0)
				{
					boolean=false;
				}
				else if(Integer==1)
				{
					boolean=true;
				}
				else
				{
					throw new ApplicationException("Map is not a boolean.");
				}
				return boolean;
			}
		}
		public virtual bool IsFraction
		{
			get
			{
//				return IsInteger && this.ContainsKey(NumberKeys.Denominator) && this[NumberKeys.Denominator].IsInteger;
				return this.ContainsKey(NumberKeys.Numerator) && this[NumberKeys.Numerator].IsInteger && this.ContainsKey(NumberKeys.Denominator) && this[NumberKeys.Denominator].IsInteger;
			}
		}
		public virtual double Fraction
		{
			get
			{
				double fraction;
				if(IsFraction)
				{
					fraction=((double)(this["numerator"]).Integer.LongValue())/((double)(this["denominator"]).Integer.LongValue());
				}
				else
				{
					throw new ApplicationException("Map is not a fraction");
				}
				return fraction;
			}
		}
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
		public Map Argument
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
		Map arg=null;
		public virtual bool IsString
		{
			get
			{
				return String!=null;
			}
		}
		public static string GetString(Map map)
		{
			string text="";
			foreach(Map key in map.Keys)
			{
				if(key.Integer!=null && map[key].Integer!=null)
				{
					try
					{
						text+=Convert.ToChar(map[key].Integer.Int32);
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
		public virtual Map Parent
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
		public abstract Map this[Map key] 
		{
			get;
			set;
		}
		public virtual Map Call(Map argument)
		{
			Argument=argument;
			Expression function=(Expression)this[CodeKeys.Run].GetExpression();
			Map result;
			result=function.Evaluate(this);
			return result;
		}
		public abstract ArrayList Keys
		{
			get;
		}
		public abstract Map Clone();
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
		public bool ContainsKey(Map key)
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
		protected virtual bool ContainsKeyImplementation(Map key)
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
		}		private Map parent;
	}
	
	public abstract class StrategyMap: Map, ISerializeSpecial
	{
		public void InitFromStrategy(MapStrategy clone)
		{
			foreach(Map key in clone.Keys)
			{
				this[key]=clone[key];
			}
		}
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
		public override Map this[Map key] 
		{
			get
			{
				Map result;
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
						Map val;
						val=value.Clone();
						val.Parent=this;
						strategy[key]=val;
					}
				}
			}
		}
		public override Map Call(Map argument)
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
		public override Map Clone()
		{
			Map clone=strategy.CloneMap();
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
		public NormalMap(NormalStrategy strategy):base(strategy)
		{
		}
		public NormalMap():this(new HybridDictionaryStrategy())
		{
		}
		public NormalMap(Integer number):this(new IntegerStrategy(number))
		{
		}
		public NormalMap(double fraction):this(new HybridDictionaryStrategy(fraction))
		{
		}
		public NormalMap(string text):this(new StringStrategy(text))
		{
		}
	}
	public class PersistantMap:StrategyMap
	{
		public override Map Clone()
		{
			return base.Clone();
		}

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
	public abstract class AssemblyStrategy:PersistantStrategy
	{
		public NormalMap NamespacesFromAssemblies(ArrayList assemblies)
		{
			NamespaceStrategy root=new NamespaceStrategy(null);
			foreach(Assembly assembly in assemblies)
			{
				ArrayList assemblyNamespaces=new ArrayList();
				foreach(Type type in assembly.GetExportedTypes())
				{
					if(!assemblyNamespaces.Contains(type.Namespace))
					{
						assemblyNamespaces.Add(type.Namespace);
						NamespaceStrategy current=root;
						if(type.Namespace!=null)
						{
							foreach(string subNamespace in type.Namespace.Split('.'))
							{
								if(!current.Namespaces.ContainsKey(subNamespace))
								{
									string fullName=current.FullName;
									if(fullName!=null)
									{
										fullName+=".";
									}
									fullName+=subNamespace;
									current.Namespaces[subNamespace]=new NormalMap(new NamespaceStrategy(fullName));
								}
								current=(NamespaceStrategy)((NormalMap)current.Namespaces[subNamespace]).strategy;
							}
						}
						current.Assemblies.Add(assembly);
					}
				}
			}
			return new NormalMap(root);
		}
		protected Map cachedAssemblyInfo=new NormalMap();
		protected NormalMap cache=new NormalMap();
	}
	public class GAC: AssemblyStrategy
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}
		public override Map this[Map key]
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
		public override int Count
		{
			get
			{
				return cache.Count;
			}
		}
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public GAC()
		{
			cache=NamespacesFromAssemblies(GlobalAssemblyCache.Assemblies);
		}
		public static Map library=new PersistantMap(new GAC());
	}
	public class DirectoryStrategy:AssemblyStrategy
	{
		public DirectoryStrategy(DirectoryInfo directory)
		{
			this.directory=directory;
			assemblyPath=Path.Combine(directory.FullName,"assembly");
			ArrayList assemblies=new ArrayList();
			if(Directory.Exists(assemblyPath))
			{
				foreach(string dllPath in Directory.GetFiles(assemblyPath,"*.dll"))
				{
					assemblies.Add(Assembly.LoadFrom(dllPath));
				}
				foreach(string exePath in Directory.GetFiles(assemblyPath,"*.exe"))
				{
					assemblies.Add(Assembly.LoadFrom(exePath));
				}
			}
			cache=NamespacesFromAssemblies(assemblies);
		}
		private string assemblyPath;
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
//		public override MapStrategy Clone()
//		{
//			return base.Clone ();
//		}
		private DirectoryInfo directory;
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
		private bool ValidName(string key)
		{
			return !key.StartsWith(".") && !key.Equals("assembly");
		}
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}
		public override Map this[Map key]
		{
			get
			{
				Map val;
				if(key.IsString && ValidName(key.String))
				{
					string path=Path.Combine(directory.FullName,key.String);
					FileInfo file=new FileInfo(path+".meta");
					DirectoryInfo subDirectory=new DirectoryInfo(path);
					if(file.Exists)
					{
						val=new PersistantMap(file);
					}
					else if(subDirectory.Exists)
					{
						val=new PersistantMap(subDirectory);
					}
					else if(cache.ContainsKey(key))
					{
						val=cache[key];
					}
					else
					{
						val=null;
					}
				}
				else
				{
					val=null;
				}
				return val;
			}
			set
			{
				if(key.IsString && ValidName(key.String))
				{
					SaveToFile(value,Path.Combine(directory.FullName,key.String+".meta"));
				}
				else
				{
					throw new ApplicationException("Cannot set key "+key.ToString()+" in DirectoryStrategy.");
				}
			}
		}
		public static void SaveToFile(Map meta,string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Create(path).Close();
			string text=Meta.Serialize.Value(meta).Trim(new char[]{'\n'});
			if(text=="\"\"")
			{
				text="";
			}
			Helper.WriteFile(path,text);
		}
	}
	public class Serialize
	{
		public static string Key(Map key)
		{
			return Key(key,"");
		}
		private static string Key(Map key,string indentation)
		{
			string text;
			if(key.IsString)
			{
				text=StringKey(key,indentation);
			}
			else if(key.IsInteger)
			{
				text=IntegerKey(key);
			} 
			else
			{
				text=MapKey(key,indentation);
			}
			return text;			
		}
		public static string Value(Map val)
		{
			return Value(val,"");
		}
		private static string Value(Map val,string indentation)
		{
			string text;
			if(val.IsString)
			{
				text=StringValue(val,indentation);
			}
			else if(val.IsInteger)
			{
				text=IntegerValue(val);
			}
			else
			{
				text=MapValue(val,indentation);
			}
			return text;
		}
		private static string IntegerKey(Map number)
		{
			return number.Integer.ToString();
		}
		private static string IntegerValue(Map number)
		{
			return literalDelimiter+number.ToString()+literalDelimiter;
		}
		private static string StringKey(Map key,string indentation)
		{
			string text;
			if(IsLiteralKey(key.String))
			{
				text=key.String;
			}
			else
			{
				text=leftBracket + StringValue(key,indentation) + rightBracket;
			}
			return text;
		}
		private static string StringValue(Map val,string indentation)
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
				text=MapValue(val,indentation);
			}
			return text;
		}
		// TODO: put this into another class
		private static string MapKey(Map map,string indentation)
		{
			return indentation + leftBracket + newLine + MapValue(map,indentation) + rightBracket;
		}
		private static string MapValue(Map map,string indentation)
		{
			string text=newLine;
			foreach(DictionaryEntry entry in map)
			{
				text+=indentation + Key((Map)entry.Key,indentation)	+ "=" + Value((Map)entry.Value,indentation+'\t');
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
	}
	public class FileStrategy:PersistantStrategy
	{
		public FileStrategy(FileInfo file)
		{
			this.file=file;
			if(!file.Exists)
			{
				file.Create().Close();
			}
		}
		private FileInfo file;
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public override ArrayList Keys
		{
			get
			{
				return GetMap().Keys;
			}
		}
		public override Integer Integer
		{
			get
			{
				return GetMap().Integer;
			}
		}
		private Map GetMap()
		{
			Map data=Interpreter.RunWithoutLibrary(this.file.FullName,new NormalMap());
			data.Parent=this.map;
			return data;
		}
		private void SaveMap(Map map)
		{
			DirectoryStrategy.SaveToFile(map,file.FullName);
		}
		public override Map this[Map key]
		{
			get
			{
				Map data=GetMap();
				return data[key];
			}
			set
			{
				Map data=GetMap();
				data[key]=value;
				SaveMap(data);
			}
		}
	}

	public class NamespaceStrategy: NormalStrategy
	{
		public override MapStrategy Clone()
		{
			return new NamespaceStrategy(FullName,cache,namespaces,assemblies);
		}

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
		private ListDictionary Cache
		{
			get
			{
				if(cache==null)
				{
					Load();
				}
				return cache;
			}
		}
		public override bool ContainsKey(Map key)
		{
			return Cache.Contains(key);
		}
		public override Map this[Map key]
		{
			get
			{
				return (Map)Cache[key];
			}
			set
			{
				// TODO: should this be possible??? I think so
				throw new ApplicationException("Cannot set key "+key.ToString()+" in .NET namespace.");
			}
		}
		public override ArrayList Keys
		{
			get
			{
				return new ArrayList(Cache.Keys);
			}
		}
		public override int Count
		{
			get
			{
				return Cache.Count;
			}
		}
		public string FullName
		{
			get
			{
				return fullName;
			}
		}
		private string fullName;
		private ArrayList assemblies;
		private Hashtable namespaces;
		private ListDictionary cache;

		public ArrayList Assemblies
		{
			get
			{
				return assemblies;
			}
		}
		public Hashtable Namespaces
		{
			get
			{
				return namespaces;
			}
		}
		public NamespaceStrategy(string fullName)
		{
			this.fullName=fullName;
			this.assemblies=new ArrayList();
			this.namespaces=new Hashtable();
		}
		public NamespaceStrategy(string fullName,ListDictionary cache,Hashtable namespaces,ArrayList assemblies)
		{
			this.fullName=fullName;
			this.cache=cache;
			this.assemblies=assemblies;
			this.namespaces=namespaces;
		}
		public void Load()
		{
			cache=new ListDictionary();
			foreach(Assembly assembly in assemblies)
			{
				foreach(Type type in assembly.GetExportedTypes())
				{
					if(type.DeclaringType==null && this.FullName==type.Namespace) 
					{
						cache[new NormalMap(type.Name)]=new DotNetClass(type);
					}
				}
				Interpreter.loadedAssemblies.Add(assembly.Location); // TODO: make this a function
			}
			foreach(DictionaryEntry entry in namespaces)
			{
				cache[new NormalMap((string)entry.Key)]=(Map)entry.Value;
			}
		}
	}
	public class Transform
	{
		public static object ToDotNet(Map meta) 
		{
			object dotNet;
			if(meta.IsInteger)
			{
				dotNet=meta.Integer.Int32;
			}
			else if(meta.IsString)
			{
				dotNet=meta.String;
			}
			else
			{
				dotNet=meta;
			}
			return dotNet;
		}
		public static object ToDotNet(Map meta,Type target)
		{
			bool isConverted;
			return ToDotNet(meta,target,out isConverted);
		}
		public static object ToDotNet(Map meta,Type target,out bool isConverted)
		{
			object dotNet=null;
			if((target.IsSubclassOf(typeof(Delegate))
				||target.Equals(typeof(Delegate)))&& meta.IsFunction) // TODO: why Equals(typeof(Delegate))
			{
				MethodInfo invoke=target.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate function=DotNetMethod.CreateDelegateFromCode(target,invoke,meta);
				dotNet=function;
			}
			else if(target.IsArray && meta.Array.Count!=0)
			{
				Type type=target.GetElementType();
				Array arguments=System.Array.CreateInstance(type,meta.Array.Count);
				bool isElementConverted=true;
				for(int i=0;i<meta.Count;i++)
				{
					object element=Transform.ToDotNet(meta[i+1],type,out isElementConverted);
					if(isElementConverted)
					{
						arguments.SetValue(element,i);
					}
					else
					{
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
				dotNet=Enum.ToObject(target,meta.Integer.Int32); // TODO: pick underlying type dynamically, maybe have an object-number in Integer
			}
			else 
			{
				switch(Type.GetTypeCode(target))
				{
					case TypeCode.Boolean:
						if(IsIntegerInRange(meta,0,1))
						{
							if(meta.Integer==0)
							{
								dotNet=false;
							}
							else if(meta.Integer==1)
							{
								dotNet=true;
							}
						}
						break;
					case TypeCode.Byte:
						 // TODO: overload this some??
						if(IsIntegerInRange(meta,new Integer(Byte.MinValue),new Integer(Byte.MaxValue)))
						{
							dotNet=Convert.ToByte(meta.Integer.Int32);
						}
						break;
					case TypeCode.Char:
						if(IsIntegerInRange(meta,(int)Char.MinValue,(int)Char.MaxValue))
						{
							dotNet=Convert.ToChar(meta.Integer.LongValue());
						}
						break;
					case TypeCode.DateTime:
						isConverted=false;
						break;
					case TypeCode.DBNull:
						if(meta.IsInteger && meta.Integer==0)
						{
							dotNet=DBNull.Value;
						}
						break;
					case TypeCode.Decimal:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble((double)decimal.MinValue),Helper.IntegerFromDouble((double)decimal.MaxValue)))
						{
							dotNet=(decimal)(meta.Integer.LongValue());
						}
						else if(IsFractionInRange(meta,(double)decimal.MinValue,(double)decimal.MaxValue))
						{
							dotNet=(decimal)meta.Fraction;
						}
						break;
					case TypeCode.Double:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble(double.MinValue),Helper.IntegerFromDouble(double.MaxValue)))
						{
							dotNet=(double)(meta.Integer.LongValue());
						}
						else if(IsFractionInRange(meta,double.MinValue,double.MaxValue))
						{
							dotNet=meta.Fraction;
						}
						break;
					case TypeCode.Int16:
						if(IsIntegerInRange(meta,Int16.MinValue,Int16.MaxValue))
						{
							dotNet=Convert.ToInt16(meta.Integer.LongValue());
						}
						break;
					case TypeCode.Int32:
						if(IsIntegerInRange(meta,Int32.MinValue,Int32.MaxValue))
						{
							dotNet=meta.Integer.Int32;
						}
						break;
					case TypeCode.Int64:
						if(IsIntegerInRange(meta,Int64.MinValue,Int64.MaxValue))
						{
							dotNet=Convert.ToInt64(meta.Integer.LongValue());
						}
						break;
					case TypeCode.Object:
						if(meta is DotNetObject && target.IsAssignableFrom(((DotNetObject)meta).type))
						{
							dotNet=((DotNetObject)meta).obj;
						}
						else if(target.IsAssignableFrom(meta.GetType()))
						{
							dotNet=meta;
						}
						break;
					case TypeCode.SByte:
						if(IsIntegerInRange(meta,SByte.MinValue,SByte.MaxValue))
						{
							dotNet=Convert.ToSByte(meta.Integer.LongValue());
						}
						break;
					case TypeCode.Single:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble(Single.MinValue),Helper.IntegerFromDouble(Single.MaxValue)))
						{
							dotNet=(float)meta.Integer.LongValue();
						}
						else if(IsFractionInRange(meta,Single.MinValue,Single.MaxValue))
						{
							dotNet=(float)meta.Fraction;
						}
						break;
					case TypeCode.String:
						if(meta.IsString)
						{
							dotNet=meta.String;
						}
						break;
					case TypeCode.UInt16:
						if(IsIntegerInRange(meta,new Integer(UInt16.MinValue),new Integer(UInt16.MaxValue)))
						{
							dotNet=Convert.ToUInt16(meta.Integer.LongValue());
						}
						break;
					case TypeCode.UInt32:
						if(IsIntegerInRange(meta,UInt32.MinValue,UInt32.MaxValue))
						{
							dotNet=Convert.ToUInt32(meta.Integer.LongValue());
						}
						break;
					case TypeCode.UInt64:
						if(IsIntegerInRange(meta,UInt64.MinValue,UInt64.MaxValue))
						{
							dotNet=Convert.ToUInt64(meta.Integer.LongValue());
						}
						break;
					default:
						throw new ApplicationException("not implemented");
				}
			}
			if(dotNet!=null)
			{
				isConverted=true;
			}
			else
			{
				isConverted=false;
			}
			return dotNet;
		}
		private static bool IsIntegerInRange(Map meta,Integer minValue,Integer maxValue)
		{
			return meta.IsInteger && meta.Integer>=minValue && meta.Integer<=maxValue;
		}
		private static bool IsFractionInRange(Map meta,double minValue,double maxValue)
		{
			return meta.IsFraction && meta.Fraction>=minValue && meta.Fraction<=maxValue;
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
						meta=new DotNetObject(dotNet);
						break;
					case TypeCode.DBNull:
						meta=new DotNetObject(dotNet);
						break;
					case TypeCode.Decimal:
						meta=(decimal)dotNet;
						break;
					case TypeCode.Double:
						meta=(double)dotNet;
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
							meta=new DotNetObject(dotNet);
						}
						break;
					case TypeCode.SByte:
						meta=(sbyte)dotNet;
						break;
					case TypeCode.Single:
						meta=(float)dotNet;
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
		private static Hashtable toDotNet=new Hashtable();
		private static Hashtable toMeta=new Hashtable();
	}
	public class MapEnumerator: IEnumerator
	{
		private Map map; 
		public MapEnumerator(Map map)
		{
			this.map=map;
		}
		public object Current
		{
			get
			{
				return new DictionaryEntry(map.Keys[index],map[(Map)map.Keys[index]]);
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
	public class DotNetMethod: Map,ICallable
	{
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override Map Clone()
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
		public override Map this[Map key]
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
		public override Map Call(Map argument)
		{
			if(this.name=="AddRange")
			{
				int asdf=0;
			}
			if(this.targetType!=null && this.targetType.Name=="MenuItem" && argument.Array.Count==2)
			{
				int asdf=0;
			}
			object result=null;

			ArrayList oneArgumentMethods=new ArrayList();// TODO: remove this
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
				object parameter=Transform.ToDotNet(argument,method.GetParameters()[0].ParameterType,out isConverted);
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
					throw new ApplicationException("Method "+this.name+": No methods with the right number of arguments.");
				}
				foreach(MethodBase method in rightNumberArgumentMethods)
				{
					ArrayList arguments=new ArrayList();
					bool argumentsMatched=true;
					ParameterInfo[] arPrmtifParameters=method.GetParameters();// TODO: rename
					for(int i=0;argumentsMatched && i<arPrmtifParameters.Length;i++)
					{
						arguments.Add(Transform.ToDotNet((Map)argument.Array[i],arPrmtifParameters[i].ParameterType,out argumentsMatched));
					}
					if(argumentsMatched)
					{
						if(method is ConstructorInfo)
						{
							result=((ConstructorInfo)method).Invoke(arguments.ToArray());
						}
						else
						{
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
			source+="public class EventHandlerContainer{public "+returnType+" EventHandlerMethod";
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
			source+="Map result=callable.Call(arg);";
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
			string metaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;
			ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
			assemblyNames.AddRange(Interpreter.loadedAssemblies);
			CompilerParameters compilerParameters=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
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
		protected Type targetType; // TODO: rename

		public MethodBase[] overloadedMethods;
	}
	public class DotNetClass: DotNetContainer
	{
		public Type Type
		{
			get
			{
				return type;
			}
		}
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

		public override Map Clone()
		{
			return new DotNetClass(type);
		}
		protected DotNetMethod constructor;
		public DotNetClass(Type targetType):base(null,targetType)
		{
			this.constructor=new DotNetMethod(this.type);
		}
		public override Map Call(Map argument)
		{
			return constructor.Call(argument);
		}

	}
	public class DotNetObject: DotNetContainer
	{
		public object Object
		{
			get
			{
				return obj;
			}
		}
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
		public override Map Clone()
		{
			return new DotNetObject(obj);
		}
	}
	public abstract class MapStrategy:ISerializeSpecial
	{
		public abstract Integer Integer
		{
			get;
		}
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
		public virtual Map Call(Map argument)
		{
			map.Argument=argument;
			Expression function=(Expression)this[CodeKeys.Run].GetExpression();
			Map result;
			result=function.Evaluate(map);
			return result;
		}
		public StrategyMap map;
		public virtual MapStrategy Clone()
		{
			return null;
		}
		public virtual Map CloneMap() // TODO: move into Map
		{
			Map clone;
			NormalStrategy strategy=(NormalStrategy)this.Clone();
			if(strategy!=null)
			{
				clone=new NormalMap(strategy);
			}
			else
			{
				clone=new NormalMap();
				foreach(Map key in Keys)
				{
					clone[key]=this[key];
				}
			}
			return clone;
		}

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
		public abstract Map this[Map key] 
		{
			get;
			set;
		}

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
				foreach(Map key in this.Keys) 
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
	public abstract class NormalStrategy:MapStrategy
	{
	}
	public class StringStrategy:NormalStrategy
	{
		public override MapStrategy Clone()
		{
			return new StringStrategy(this.text);
		}

		public override Integer Integer
		{
			get
			{
				return null;
			}
		}

//		public override int GetHashCode()
//		{
//			int hash=0;
//			for(int i=0;i<text.Length;i++)
//			{
//				hash+=(i+1)*text[i];
//			}
//			return hash;
//		}
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
		// TODO: could be further optimized, maybe with ArrayList.GetRange()
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				for(int i=1;i<=text.Length;i++)
				{ 
					keys.Add(new NormalMap(new Integer(i)));			
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
		public override Map this[Map key]
		{
			get
			{
				if(key.IsInteger)
				{
					int iInteger=key.Integer.Int32;
					if(iInteger>0 && iInteger<=this.Count)
					{
						return text[iInteger-1];
					}
				}
				return null;
			}
			set
			{
				map.strategy=new HybridDictionaryStrategy();
				map.strategy.map=map;
				map.InitFromStrategy(this);
				map.strategy[key]=value;
			}
		}
		public override bool ContainsKey(Map key) 
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
	public class HybridDictionaryStrategy:NormalStrategy
	{
		public override Integer Integer
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
					if(this[NumberKeys.EmptyMap].Integer!=null)
					{
						number=this[NumberKeys.EmptyMap].Integer+1;
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
				return number;
			}
		}
		ArrayList keys;
		private HybridDictionary dictionary;

		public HybridDictionaryStrategy(double fraction):this(2)
		{
			Integer denominator=new Integer(1);
			while(Math.Floor(fraction)!=fraction)
			{
				fraction*=2;
				denominator*=2;
			}
			Integer numerator=Helper.IntegerFromDouble(fraction);
			this[NumberKeys.Numerator]=new NormalMap(numerator);
			this[NumberKeys.Denominator]=new NormalMap(denominator);
		}

		public HybridDictionaryStrategy():this(2)
		{
		}
		public HybridDictionaryStrategy(int Count)
		{
			this.keys=new ArrayList(Count);
			this.dictionary=new HybridDictionary(Count);
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
					foreach(Map val in this.Array)
					{
						if(val.IsInteger)
						{
							try
							{
								text+=Convert.ToChar(val.Integer.Int32);
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
		public override Map this[Map key] 
		{
			get
			{
				return (Map)dictionary[key];
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
		public override bool ContainsKey(Map key) 
		{
			return dictionary.Contains(key);
		}
	}
	public abstract class DotNetContainer: Map, ISerializeSpecial
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
				string text=key.String;
				if(type.GetMember(key.String,
					BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
				{
					return true;
				}
			}
			DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
			Map argument=new NormalMap();
			argument[1]=key;
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
		public override Map this[Map key] 
		{
			get
			{
				Map result;
				if(key.IsString && type.GetMember(key.String,bindingFlags).Length>0)
				{
					string text=key.String;
					MemberInfo[] members=type.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						result=new DotNetMethod(text,obj,type);
					}
					else if(members[0] is FieldInfo)
					{
						result=Transform.ToMeta(type.GetField(text).GetValue(obj));
					}
					else if(members[0] is PropertyInfo)
					{
						result=Transform.ToMeta(type.GetProperty(text).GetValue(obj,new object[]{}));
					}
					else if(members[0] is EventInfo)
					{
						try
						{
							Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
								BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
							if(eventDelegate!=null)
							{
								result=new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
							else
							{
								result=null;
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
					result=Transform.ToMeta(((Array)obj).GetValue(key.Integer.Int32));
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("get_Item",obj,type);
					Map argument=new NormalMap();
					argument[1]=key;
					try
					{
						result=Transform.ToMeta(indexer.Call(argument));
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
						object val=Transform.ToDotNet(value,field.FieldType,out isConverted);
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
						object val=Transform.ToDotNet(value,property.PropertyType,out isConverted);
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
					object converted=Transform.ToDotNet(value,type.GetElementType(),out isConverted);
					if(isConverted)
					{
						((Array)obj).SetValue(converted,key.Integer.Int32);
						return;
					}
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("set_Item",obj,type);
					Map argument=new NormalMap();
					argument[1]=key;
					argument[2]=value;
					try
					{
						indexer.Call(argument);
					}
					catch(Exception e)
					{
						throw new ApplicationException("Cannot set "+Transform.ToDotNet(key).ToString()+".");
					}
				}
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
							table[Transform.ToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
						}
						else
						{
							table[counter]=entry;
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
	public class IntegerStrategy:NormalStrategy
	{
		public override int GetHashCode()
		{
			return 0;
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
		public override Map this[Map key]
		{
			get
			{
				Map result;
				if(key.Equals(NumberKeys.EmptyMap))
				{
					if(number==0)
					{
						result=0;
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
				else if(key.Equals(NumberKeys.Negative))
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
				if(key.Equals(NumberKeys.EmptyMap))
				{
					Map map=value;// TODO: remove
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
		private void Panic(Map key,Map val)
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
		public static Integer IntegerFromDouble(double val)
		{
			Integer integer=new Integer(1);
			while(Math.Abs(val)/(double)int.MaxValue>1.0d)
			{
				val/=int.MaxValue;
				integer*=int.MaxValue;
			}
			integer*=Convert.ToInt32(val);
			return integer;
		}
		public static FileInfo[] FindFiles(DirectoryInfo directory,string fileName)
		{
			ArrayList files=new ArrayList();
			foreach(FileInfo file in directory.GetFiles())
			{
				if(file.Name==fileName)
				{
					files.Add(file);
				}
			}
			foreach(DirectoryInfo subDirectory in directory.GetDirectories())
			{
				files.AddRange(FindFiles(subDirectory,fileName));
			}
			return (FileInfo[])files.ToArray(typeof(FileInfo));
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
					members.Sort(new MemberInfoComparer());
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
		class MemberInfoComparer:IComparer
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