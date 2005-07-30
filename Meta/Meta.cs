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
	public class Strings
	{
		public readonly static Map Literal=new Map("literal");
		public readonly static Map Run=new Map("run");
		public readonly static Map Call=new Map("call");
		public readonly static Map Function=new Map("function");
		public readonly static Map Argument=new Map("argument");
		public static readonly Map Select=new Map("select");
		public static readonly Map Search=new Map("search");
		public static readonly Map Key=new Map("key");
		public static readonly Map Program=new Map("program");
		public static readonly Map Delayed=new Map("delayed");
		public static readonly Map Lookup=new Map("lookup");
		public static readonly Map Value=new Map("value");
		public static readonly Map Parent=new Map("parent"); 
		public static readonly Map Arg=new Map("arg");
		public static readonly Map This=new Map("this");
	}
	public abstract class Expression
	{
		public virtual bool Stop()
		{
			bool stop=false;
			if(BreakPoint!=null)
			{
				if(BreakPoint.Line>=Extent.Start.Line && BreakPoint.Line<=Extent.End.Line)
				{
					if(BreakPoint.Column>=Extent.Start.Column && BreakPoint.Column<=Extent.End.Column)
					{
						stop=true;
					}
				}
			}
			return stop;
		}

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
		private static BreakPoint breakPoint;//=new BreakPoint(@"c:\_projectsupportmaterial\meta\editor\editor.meta",152,11);

		public object Evaluate(IMap parent)
		{
			object result=EvaluateImplementation(parent);
			if(Stop())
			{
				Interpreter.CallDebug(result);
			}
			return result;
		}

		public abstract object EvaluateImplementation(IMap parent);
		Extent extent;
		public Extent Extent // refactor Extent, it sucks big deal, add Line, Column
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

		public override object EvaluateImplementation(IMap parent)
		{
			object function=callable.Evaluate(parent);
			if(function is ICallable)
			{
				return ((ICallable)function).Call(Interpreter.Clone(argument.Evaluate(parent)));
			}
			throw new MetaException("Object to be called is not callable.",this.Extent);
		}
		public Call(Map code)
		{
			this.callable=((Map)code[Strings.Function]).GetExpression();
			this.argument=((Map)code[Strings.Argument]).GetExpression();
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
		public override object EvaluateImplementation(IMap parent)
		{
			Map result=delayed; // maybe clone here, too, determine where cloning is needed, or, respectively, transplantation into a new environment, this could easily include the assignment of the parent
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
			if(Expression.BreakPoint!=null)
			{
				if(Expression.BreakPoint.Line==Extent.End.Line+1 && Expression.BreakPoint.Column==1)
				{
					stop=true;
				}
			}
			return stop;
		}

		public override object EvaluateImplementation(IMap parent)
		{
			object local=new Map();
			return Evaluate(parent,ref local); // is this logical
		}
		public object Evaluate(IMap parent,ref object local)
		{
			((IMap)local).Parent=parent; // transplanting here, again, do this only once, combine it, make it explicit
			for(int i=0;i<statements.Count && i>=0;)
			{
				if(Interpreter.reverseDebug) // rename to forwardDebug
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
						//maybe set reverse Debug here, threading issue
						int asdf=0; // just go on, here, we already know how to handle reversion, this is just to get out of the currently executing statement and all its expressions and subexpressions
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
			return local;// doesn't make sense to return at all
		}
		public Program(Map code)
		{
			foreach(Map statement in code.Array)
			{
				this.statements.Add(new Statement(statement)); // should we save the original maps instead of statements?
			}
		}
		public readonly ArrayList statements=new ArrayList();
	}
	public class BreakPoint //// use Position class here + Line and Column classes!!!!!
	{
		public BreakPoint(string fileName,int line,int column)
		{
			this.fileName=fileName;
			this.line=line;
			this.column=column;
		}
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		public int Line
		{
			get
			{
				return line;
			}
		}
		public int Column
		{
			get
			{
				return column;
			}
		}
		string fileName;
		int line;
		int column;
	}
	public class Literal: Expression
	{
		public override object EvaluateImplementation(IMap parent)
		{
			return literal;
		}
		public Literal(Map code)
		{
			this.literal=RecognizeLiteral((string)code.String);
		}
		public object literal=null;
		public static object RecognizeLiteral(string text) // somehow put this somewhere else, (Literal?)
		{
			foreach(Interpreter.RecognizeLiteral rcnltrCurrent in Interpreter.recognitions) // move the rest of the pack here
			{
				object recognized=rcnltrCurrent.Recognize(text);
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
		public override object EvaluateImplementation(IMap parent)
		{
			object key=search.Evaluate(parent);
			IMap selected=parent;
			while(!selected.ContainsKey(key)) // mhhm is this needed anywhere else?
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
	public class Select: Expression // define recursively
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
		public override object EvaluateImplementation(IMap parent)
		{
			object selected=firstKey.Evaluate(parent);
			for(int i=0;i<keys.Count;i++)
			{
				object key=((Expression)keys[i]).Evaluate(parent);
				if(!(selected is IKeyValue)) // This is unlogical:
				{
					selected=new DotNetObject(selected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
				}
				object selection=((IKeyValue)selected)[key];
				if(selection==null)
				{
					throw new KeyDoesNotExistException(key,this.Extent,selected);
				}
				selected=selection;
			}
			return selected;
		}	}
	public class Statement
	{
		private object replaceValue;
		private IKeyValue replaceMap;
		private object replaceKey;
		public bool Undo()
		{
			if(replaceMap!=null) // need to handle "this" specially
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
		public void Realize(ref object parent) // should be an IMap for the most part of this method
		{
			object selected=parent;
			object key;
			
			if(searchFirst)
			{
				object firstKey=((Expression)keys[0]).Evaluate((IMap)parent); 
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
				key=((Expression)keys[i]).Evaluate((IMap)parent);
				object selection=((IKeyValue)selected)[key];
				if(selection==null)
				{
					throw new KeyDoesNotExistException(key,((Expression)keys[i]).Extent,selected);
				}
				selected=selection;
				if(!(selected is IKeyValue))
				{
					selected=new DotNetObject(selected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
				}
			}
			object lastKey=((Expression)keys[keys.Count-1]).Evaluate((IMap)parent);
			object val=expression.Evaluate((IMap)parent);
			if(lastKey.Equals(Strings.This))
			{
				if(val is Map)
				{
					((Map)val).Parent=((Map)parent).Parent;
				}
				else
				{
					int asdf=0;
				}
				parent=val;
			}
			else
			{
				///////////////   for debug-undo
				if(((IKeyValue)selected).ContainsKey(lastKey))
				{
					replaceValue=((IKeyValue)selected)[lastKey];
				}
				else
				{
					replaceValue=null;
				}
				replaceMap=(IKeyValue)selected;
				replaceKey=lastKey;
				//////////////

				((IKeyValue)selected)[lastKey]=val;
			}
		}
		public Statement(Map code) 
		{
			if(code.ContainsKey(Strings.Search))
			{
				searchFirst=true;
			}
			foreach(Map key in ((Map)code[Strings.Key]).Array)
			{
				keys.Add(key.GetExpression());
			}
			this.expression=(Expression)((Map)code[Strings.Value]).GetExpression();
		}
		public ArrayList keys=new ArrayList();
		public Expression expression;
		
		bool searchFirst=false;
	}


	public class Interpreter  // what about multiple interpreters? or make interpreter multithreaded, what about events vs. multithreading?
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
		public static event EventHandler Test;
		public static object Clone(object meta)
		{
			if(meta is IMap)
			{
				return ((IMap)meta).Clone();
			}
			else
			{
				return meta;
			}
		}
		public static void SaveToFile(object meta,string path)
		{
			StreamWriter streamWriter=new StreamWriter(path);
			streamWriter.Write(SaveToFile(meta,"",true).TrimEnd(new char[]{'\n'}));
			streamWriter.Close();
		}
		public static string Serialize(object meta)
		{
			return SaveToFile(meta,"",true);
		}
		// make this more efficient and more flexible with respect to special things, like functions
		// maybe add function for serialization that is non-critical, that is for intellisense stuff
		public static string SaveToFile(object meta,string indent,bool isRightSide) // dumb NAME!!!!
		{
			if(meta is Map)
			{
				string text="";
				Map map=(Map)meta;
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
					if(!isRightSide) // TODO: this isn't really allowed anymore, but will maybe be needed for serialization, if allow, use indentation, however
					{
						text+="(";
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
							if(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString)
							{
								text+="\n";
							}
							text+=SaveToFile(entry.Value,indent+'\t',true);
							if(!(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString))
							{
								text+="\n";
							}
						}
					}
				}
				return text;
			}
			else if(meta is Integer)
			{
				Integer integer=(Integer)meta;
				return "\""+integer.ToString()+"\"";
			}
			else
			{
				return "\""+meta.ToString()+"\""; // fix this!!!, for other types
				//throw new ApplicationException("Serialization not implemented for type "+meta.GetType().ToString()+".");
			}
		}
		public static IKeyValue Merge(params IKeyValue[] arkvlToMerge)
		{
			return MergeCollection(arkvlToMerge);
		}
		public static IKeyValue MergeCollection(ICollection collection)
		{
			Map result=new Map();//use clone here?
			foreach(IKeyValue current in collection)
			{
				foreach(DictionaryEntry entry in (IKeyValue)current)
				{
					if(entry.Value is IKeyValue && !(entry.Value is DotNetClass)&& result.ContainsKey(entry.Key) 
						&& result[entry.Key] is IKeyValue && !(result[entry.Key] is DotNetClass))
					{
						result[entry.Key]=Merge((IKeyValue)result[entry.Key],(IKeyValue)entry.Value);
					}
					else
					{
						result[entry.Key]=entry.Value;
					}
				}
			}
			return result;
		}	
		// TODO: Should Arrays be converted to maps, or is it sufficient to have a DotNetObject?
		//    Map would be more convenient, but DotNetObject is ok for now

		public static object MetaFromDotNet(object oDotNet)
		{ 
			if(oDotNet==null)
			{
				return null;
			}
			else if(oDotNet.GetType().IsSubclassOf(typeof(Enum)))
			{
				return new Integer((int)Convert.ToInt32((Enum)oDotNet));
			}
			DotNetToMetaConversion conversion=(DotNetToMetaConversion)toMetaConversions[oDotNet.GetType()];
			if(conversion==null)
			{
				return oDotNet;
			}
			else
			{
				return conversion.Convert(oDotNet);
			}
		}
		public static object DotNetFromMeta(object meta) // there will be name collision with the classes used, but there is only one function, so rename the function
		{
			if(meta is Integer)
			{
				return ((Integer)meta).Int;
			}
			else if(meta is Map && ((Map)meta).IsString)
			{
				return ((Map)meta).String;
			}
			else
			{
				return meta;
			}
		}
		public static object DotNetFromMeta(object meta,Type target)
		{
			object result=meta;
			if(Interpreter.toDotNetConversions.ContainsKey(target))
			{
				Hashtable conversions=(Hashtable)Interpreter.toDotNetConversions[target];
				if(conversions.ContainsKey(meta.GetType()))
				{
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)conversions[meta.GetType()];
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
		public static object Run(string fileName,IMap argument)
		{
			Map program=Interpreter.Compile(fileName);
			return CallProgram(program,argument,Library.library);
		}

		public static object RunWithoutLibrary(string fileName,IMap argument)
		{
			Map program=Compile(fileName); // TODO: rename, is not really a program but a function
			return CallProgram(program,argument,null);
		}
		public static object CallProgram(Map program,IMap argument,IMap parent)
		{
			Map callable=new Map();
			callable[Strings.Run]=program;
			callable.Parent=parent;
			return callable.Call(argument);
		}

		public static Map Compile(string fileName)
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

			metaParser.setASTNodeClass("MetaAST");//
			metaParser.map();
			AST ast=metaParser.getAST();
			file.Close();
			return ast;
		}

		public static object DotNetFromMeta(object meta,Type target,out bool isConverted)
		{
			if(target.IsSubclassOf(typeof(Enum)) && meta is Integer)
			{ 
				isConverted=true;
				return Enum.ToObject(target,((Integer)meta).Int);
			}
			Hashtable toDotNet=(Hashtable)
				Interpreter.toDotNetConversions[target];
			if(toDotNet!=null)
			{
				MetaToDotNetConversion conversion=(MetaToDotNetConversion)toDotNet[meta.GetType()];
				if(conversion!=null)
				{
					return conversion.Convert(meta,out isConverted);
				}
			}
			isConverted=false;
			return null;
		}
		private static void ExecuteInThread()
		{
			Interpreter.Run(executeFileName,new Map());
			int asdf=0;
		}
		private static string executeFileName="";
		public static void StartDebug(string fileName) 
		{
			executeFileName=fileName;
			debugThread=new Thread(new ThreadStart(ExecuteInThread));
			debugThread.Start(); // there can be many debug threads, they also shouldn't be anything special
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

			////////// TODO: installation path still sucks big deal
			installationPath=@"c:\_projectsupportmaterial\meta";//Directory.GetParent(metaAssembly.Location).Parent.FullName; 
//				installationPath=Directory.GetParent(metaAssembly.Location).Parent.Parent.Parent.FullName; 
			foreach(Type recognition in typeof(LiteralRecognitions).GetNestedTypes())
			{
				recognitions.Add((RecognizeLiteral)recognition.GetConstructor(new Type[]{}).Invoke(new object[]{}));
			}
			recognitions.Reverse();
			foreach(Type toMetaConversion in typeof(DotNetToMetaConversions).GetNestedTypes())
			{
				DotNetToMetaConversion conversion=((DotNetToMetaConversion)toMetaConversion.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				toMetaConversions[conversion.source]=conversion;
			}
			foreach(Type toDotNetConversion in typeof(MetaToDotNetConversions).GetNestedTypes())
			{
				MetaToDotNetConversion conversion=(MetaToDotNetConversion)toDotNetConversion.GetConstructor(new Type[]{}).Invoke(new object[]{});
				if(!toDotNetConversions.ContainsKey(conversion.target))
				{
					toDotNetConversions[conversion.target]=new Hashtable();
				}
				((Hashtable)toDotNetConversions[conversion.target])[conversion.source]=conversion; // put the search shit for this into a function
			}
		}
		public static string installationPath;
		public static Hashtable toDotNetConversions=new Hashtable();
		public static Hashtable toMetaConversions=new Hashtable();
		public static ArrayList loadedAssemblies=new ArrayList(); // make this stupid class a bit smaller

		public static ArrayList recognitions=new ArrayList();

		public abstract class RecognizeLiteral
		{
			public abstract object Recognize(string text); // Returns null if not recognized. Null cannot currently be created this way, so, add a variable that indicates success
		}
		public abstract class MetaToDotNetConversion
		{
			public Type source;
			public Type target;
			public abstract object Convert(object obj,out bool converted);
		}
		public abstract class DotNetToMetaConversion
		{ 
			public Type source;
			public abstract object Convert(object obj);
		}
		public class LiteralRecognitions
		{
			// The order of the classes determines the precedence of literal recognitions
			public class RecognizeString:RecognizeLiteral  // rename to LiteralRecognition, or something even better, maybe LiteralType, and LiteralTypes
			{
				public override object Recognize(string text)
				{
					return new Map(text);
				}
			}
			public class RecognizeInteger: RecognizeLiteral 
			{
				public override object Recognize(string text) 
				{ 
					if(text.Equals(""))
					{
						return null;
					}
					else
					{
						Integer result=new Integer(0);
						int index=0;
						if(text[0]=='-')
						{
							index++;
						}
						// TODO: the following is probably incorrect for multi-byte unicode
						// use StringInfo in the future instead, or something like that
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
						return result;
					}
				}
			}

		}
		private abstract class MetaToDotNetConversions  // name is too long, all the names are to long
		{
			public class ConvertIntegerToByte: MetaToDotNetConversion // get rid of "Convert", is redundant
			{
				public ConvertIntegerToByte()
				{
					this.source=typeof(Integer);
					this.target=typeof(Byte);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToByte(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToBool: MetaToDotNetConversion
			{
				public ConvertIntegerToBool()
				{
					this.source=typeof(Integer);
					this.target=typeof(bool);
				}
				// TODO: make this single-exit
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					int i=((Integer)toConvert).Int;
					if(i==0)
					{
						return false;
					}
					else if(i==1)
					{
						return true;
					}
					else
					{
						isConverted=false; // TODO
						return null;
					}
				}

			}
			public class ConvertIntegerToSByte: MetaToDotNetConversion
			{
				public ConvertIntegerToSByte()
				{
					this.source=typeof(Integer);
					this.target=typeof(SByte);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToSByte(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToChar: MetaToDotNetConversion
			{
				public ConvertIntegerToChar()
				{
					this.source=typeof(Integer);
					this.target=typeof(Char);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToChar(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToInt32: MetaToDotNetConversion
			{
				public ConvertIntegerToInt32()
				{
					this.source=typeof(Integer);
					this.target=typeof(Int32);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToInt32(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToUInt32: MetaToDotNetConversion
			{
				public ConvertIntegerToUInt32()
				{
					this.source=typeof(Integer);
					this.target=typeof(UInt32);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToUInt32(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToInt64: MetaToDotNetConversion
			{
				public ConvertIntegerToInt64()
				{
					this.source=typeof(Integer);
					this.target=typeof(Int64);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToInt64(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToUInt64: MetaToDotNetConversion
			{
				public ConvertIntegerToUInt64()
				{
					this.source=typeof(Integer);
					this.target=typeof(UInt64);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToUInt64(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToInt16: MetaToDotNetConversion
			{
				public ConvertIntegerToInt16()
				{
					this.source=typeof(Integer);
					this.target=typeof(Int16);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToInt16(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToUInt16: MetaToDotNetConversion
			{
				public ConvertIntegerToUInt16()
				{
					this.source=typeof(Integer);
					this.target=typeof(UInt16);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return System.Convert.ToUInt16(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToDecimal: MetaToDotNetConversion
			{
				public ConvertIntegerToDecimal()
				{
					this.source=typeof(Integer);
					this.target=typeof(decimal);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (decimal)(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToDouble: MetaToDotNetConversion
			{
				public ConvertIntegerToDouble()
				{
					this.source=typeof(Integer);
					this.target=typeof(double);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (double)(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertIntegerToFloat: MetaToDotNetConversion
			{
				public ConvertIntegerToFloat()
				{
					this.source=typeof(Integer);
					this.target=typeof(float);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					isConverted=true;
					return (float)(((Integer)toConvert).LongValue());
				}
			}
			public class ConvertMapToString: MetaToDotNetConversion
			{
				public ConvertMapToString()
				{
					this.source=typeof(Map);
					this.target=typeof(string);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					if(((Map)toConvert).IsString)
					{
						isConverted=true;
						return ((Map)toConvert).String;
					}
					else
					{
						isConverted=false;
						return null;
					}
				}
			}
			public class ConvertFractionToDecimal: MetaToDotNetConversion
			{
				public ConvertFractionToDecimal()
				{
					this.source=typeof(Map); 
					this.target=typeof(decimal); 
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					Map map=(Map)toConvert;
					if(map[new Map("numerator")] is Integer && map[new Map("denominator")] is Integer)
					{
						isConverted=true;
						return ((decimal)((Integer)map[new Map("numerator")]).LongValue())/((decimal)((Integer)map[new Map("denominator")]).LongValue());
					}
					else
					{
						isConverted=false;
						return null;
					}
				}

			}
			public class ConvertFractionToDouble: MetaToDotNetConversion
			{
				public ConvertFractionToDouble()
				{
					this.source=typeof(Map);
					this.target=typeof(double);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					Map map=(Map)toConvert;
					if(map[new Map("numerator")] is Integer && map[new Map("denominator")] is Integer)
					{
						isConverted=true;
						return ((double)((Integer)map[new Map("numerator")]).LongValue())/((double)((Integer)map[new Map("denominator")]).LongValue());
					}
					else
					{
						isConverted=false;
						return null;
					}
				}

			}
			public class ConvertFractionToFloat: MetaToDotNetConversion
			{
				public ConvertFractionToFloat()
				{
					this.source=typeof(Map);
					this.target=typeof(float);
				}
				public override object Convert(object toConvert, out bool isConverted)
				{
					Map map=(Map)toConvert;
					if(map[new Map("numerator")] is Integer && map[new Map("denominator")] is Integer)
					{
						isConverted=true;
						return ((float)((Integer)map[new Map("numerator")]).LongValue())/((float)((Integer)map[new Map("denominator")]).LongValue());
					}
					else
					{
						isConverted=false;
						return null;
					}
				}
			}
		}
		//TODO: There should be some documentation on what all the abbreviations mean, and the type of Hungarian used
		private abstract class DotNetToMetaConversions
		{
			/* These classes define the conversions that take place when .NET methods,
				* properties and fields return. */
			public class ConvertStringToMap: DotNetToMetaConversion
			{
				public ConvertStringToMap()  
				{
					this.source=typeof(string);
				}
				public override object Convert(object toConvert)
				{
					return new Map((string)toConvert);
				}
			}
			public class ConvertBoolToInteger: DotNetToMetaConversion
			{
				public ConvertBoolToInteger()
				{
					this.source=typeof(bool);
				}
				public override object Convert(object toConvert)
				{
					return (bool)toConvert? new Integer(1): new Integer(0);
				}

			}
			public class ConvertByteToInteger: DotNetToMetaConversion
			{
				public ConvertByteToInteger()
				{
					this.source=typeof(Byte);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Byte)toConvert);
				}
			}
			public class ConvertSByteToInteger: DotNetToMetaConversion
			{
				public ConvertSByteToInteger()
				{
					this.source=typeof(SByte);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((SByte)toConvert);
				}
			}
			public class ConvertCharToInteger: DotNetToMetaConversion
			{
				public ConvertCharToInteger()
				{
					this.source=typeof(Char);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Char)toConvert);
				}
			}
			public class ConvertInt32ToInteger: DotNetToMetaConversion
			{
				public ConvertInt32ToInteger()
				{
					this.source=typeof(Int32);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Int32)toConvert);
				}
			}
			public class ConvertUInt32ToInteger: DotNetToMetaConversion
			{
				public ConvertUInt32ToInteger()
				{
					this.source=typeof(UInt32);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((UInt32)toConvert);
				}
			}
			public class ConvertInt64ToInteger: DotNetToMetaConversion
			{
				public ConvertInt64ToInteger()
				{
					this.source=typeof(Int64);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Int64)toConvert);
				}
			}
			public class ConvertUInt64ToInteger: DotNetToMetaConversion
			{
				public ConvertUInt64ToInteger()
				{
					this.source=typeof(UInt64);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Int64)(UInt64)toConvert);
				}
			}
			public class ConvertInt16ToInteger: DotNetToMetaConversion
			{
				public ConvertInt16ToInteger()
				{
					this.source=typeof(Int16);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((Int16)toConvert);
				}
			}
			public class ConvertUInt16ToInteger: DotNetToMetaConversion
			{
				public ConvertUInt16ToInteger()
				{
					this.source=typeof(UInt16);
				}
				public override object Convert(object toConvert)
				{
					return new Integer((UInt16)toConvert);
				}
			}
		}

	}
	/* Base class of exceptions in Meta. */
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
		{ // not really all that logical, but so what
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
	/* Base class for key exceptions. */
	public abstract class KeyException:MetaException
	{ // TODO: Add proper formatting here, output strings as strings, for example, if possible, as well as integers
		public KeyException(object key,Extent extent):base(extent)
		{
			message="Key ";
			if(key is Map && ((Map)key).IsString)
			{
				message+=((Map)key).String;
			}
			else if(key is Map)
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
	/* Thrown when a searched key was not found. */
	public class KeyNotFoundException:KeyException
	{
		public KeyNotFoundException(object key,Extent extent):base(key,extent)
		{
		}
	}
	/* Thrown when an accessed key does not exist. */
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
		object Call(object argument);
	}
	// TODO: Rename this eventually
	public interface IMap: IKeyValue
	{
		IMap Parent
		{
			get;
			set;
		}
		ArrayList Array
		{
			get;
		}
		IMap Clone();
	}
	// TODO: Does the IKeyValue<->IMap distinction make sense?
	public interface IKeyValue: IEnumerable
	{
		object this[object key]
		{
			get;
			set;
		}
		ArrayList Keys
		{
			get;
		}
		int Count
		{
			get;
		}
		bool ContainsKey(object key);			
	}		
	public class MetaLibrary
	{
		// TODO: Put this into Library class, make base class for everything that gets loaded
		public object Load()
		{
			return Interpreter.Run(path,new Map()); // TODO: Improve this interface, isn't read lazily anyway
		}
		public MetaLibrary(string path)
		{
			this.path=path;
		}
		string path;
	}
	public class LazyNamespace: IKeyValue
	{ 
		// TODO: Put this into library, combine with MetaLibrary
		public object this[object key]
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
				throw new ApplicationException("Cannot set key "+key.ToString()+" in .NET namespace.");
			}
		}
		public ArrayList Keys
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
		public int Count
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
		public string fullName;
		public ArrayList cachedAssemblies=new ArrayList();
		public Hashtable namespaces=new Hashtable();
		public LazyNamespace(string fullName)
		{
			this.fullName=fullName;
		}
		public void Load()
		{
			cache=new Map();
			foreach(CachedAssembly cachedAssembly in cachedAssemblies)
			{
				cache=(Map)Interpreter.Merge(cache,cachedAssembly.NamespaceContents(fullName));
			}
			foreach(DictionaryEntry entry in namespaces)
			{
				cache[new Map((string)entry.Key)]=entry.Value;
			}
		}
		public Map cache;
		public bool ContainsKey(object key)
		{
			if(cache==null)
			{
				Load();
			}
			return cache.ContainsKey(key);
		}
		public IEnumerator GetEnumerator()
		{
			if(cache==null)
			{
				Load();
			}
			return cache.GetEnumerator();
		}
	}
	/* TODO: What's this for? */
	public class CachedAssembly
	{  // TODO: Put this into Library class
		private Assembly assembly;
		public CachedAssembly(Assembly assembly)
		{
			this.assembly=assembly;
		}
		public Map NamespaceContents(string nameSpace)
		{
			if(assemblyContent==null)
			{
				assemblyContent=Library.LoadAssemblies(new object[] {assembly});
			}
			Map selected=assemblyContent;
			if(nameSpace!="")
			{
				foreach(string subString in nameSpace.Split('.'))
				{
					selected=(Map)selected[new Map(subString)];
				}
			}
			return selected;
		}			
		private Map assemblyContent;
	}
	/* The library namespace, containing both Meta libraries as well as .NET libraries
		*  from the "library" path and the GAC. */
	public class Library: IKeyValue,IMap
	{
		public object this[object key]
		{
			get
			{
				if(key.Equals(new Map("testClasses")))
				{
						int asdf=0;
				}
				if(cache.ContainsKey(key))
				{
					if(cache[key] is MetaLibrary)
					{
						cache[key]=((MetaLibrary)cache[key]).Load();
					}
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
		public ArrayList Keys
		{
			get
			{
				return cache.Keys;
			}
		}
		public IMap Clone()
		{
			return this;
		}
		public int Count
		{
			get
			{
				return cache.Count;
			}
		}
		public bool ContainsKey(object key)
		{
			return cache.ContainsKey(key);
		}
		public ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		public IMap Parent
		{
			get
			{
				return null;
			}
			set
			{
				throw new ApplicationException("Cannot set parent of library.");
			}
		}
		public IEnumerator GetEnumerator()
		{ 
			foreach(DictionaryEntry entry in cache)
			{ // TODO: create separate enumerator for efficiency?
				object temp=cache[entry.Key];				  // or remove IEnumerable from IMap (only needed for foreach)
			}														// decide later
			return cache.GetEnumerator();
		}
		public static Map LoadAssemblies(IEnumerable enmrbasbAssmblies)
		{
			Map root=new Map();
			foreach(Assembly currentAssembly in enmrbasbAssmblies)
			{
				foreach(Type type in currentAssembly.GetExportedTypes()) 
				{
					if(type.DeclaringType==null) 
					{
						Map position=root;
						ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
						subPaths.RemoveAt(subPaths.Count-1);
						foreach(string subPath in subPaths) 
						{
							if(!position.ContainsKey(new Map(subPath))) 
							{
								position[new Map(subPath)]=new Map();
							}
							position=(Map)position[new Map(subPath)];
						}
						position[new Map(type.Name)]=new DotNetClass(type);
					}
				}
				Interpreter.loadedAssemblies.Add(currentAssembly.Location);
			}
			return root;
		}
		private static string AssemblyName(IAssemblyName assemblyName)
		{ 
			AssemblyName name = new AssemblyName();
			name.Name = AssemblyCache.GetName(assemblyName);
			name.Version = AssemblyCache.GetVersion(assemblyName);
			name.CultureInfo = AssemblyCache.GetCulture(assemblyName);
			name.SetPublicKeyToken(AssemblyCache.GetPublicKeyToken(assemblyName));
			return name.Name;
		}
		public Library()
		{
			ArrayList assemblies=new ArrayList();
			libraryPath=Path.Combine(Interpreter.installationPath,"library");
			IAssemblyEnum assemblyEnum=AssemblyCache.CreateGACEnum();
			IAssemblyName iname; 
			assemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
			while (AssemblyCache.GetNextAssembly(assemblyEnum, out iname) == 0)
			{
				try
				{
					string assemblyName=AssemblyName(iname);
					assemblies.Add(Assembly.LoadWithPartialName(assemblyName));
				}
				catch(Exception e)
				{
				}
			}
			foreach(string dll in Directory.GetFiles(libraryPath,"*.dll"))
			{
				assemblies.Add(Assembly.LoadFrom(dll));
			}
			foreach(string exe in Directory.GetFiles(libraryPath,"*.exe"))
			{
				assemblies.Add(Assembly.LoadFrom(exe));
			}
			string cachedAssemblyPath=Path.Combine(Interpreter.installationPath,"cachedAssemblyInfo.meta"); // TODO: Use another name that doesn't collide with C# meaning
			if(File.Exists(cachedAssemblyPath))
			{
				cachedAssemblyInfo=(Map)Interpreter.RunWithoutLibrary(cachedAssemblyPath,new Map());
			}
			
			cache=LoadNamespaces(assemblies);
			Interpreter.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
			foreach(string meta in Directory.GetFiles(libraryPath,"*.meta"))
			{
				cache[new Map(Path.GetFileNameWithoutExtension(meta))]=new MetaLibrary(meta);
			}
		}
		private Map cachedAssemblyInfo=new Map();
		public ArrayList NameSpaces(Assembly assembly)
		{ 
			//TODO: refactor, integrate into LoadNamespaces???
			ArrayList nameSpaces=new ArrayList();
			if(cachedAssemblyInfo.ContainsKey(new Map(assembly.Location)))
			{
				Map info=(Map)cachedAssemblyInfo[new Map(assembly.Location)];
				string timestamp=((Map)info[new Map("timestamp")]).String;
				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
				{
					Map namespaces=(Map)info[new Map("namespaces")];// SHIT! Name collision!, why " name collision"?
					foreach(DictionaryEntry entry in namespaces) // TODO: maybe use MapAdapter? Good opportunity to extend MapAdapter a bit
					{
						string text=((Map)entry.Value).String;
						nameSpaces.Add(text);
					}
					return nameSpaces; // BAAAADDD!, make this method single exit
				}
				else
				{
					int asdf=0;
				}
			}
			foreach(Type tType in assembly.GetExportedTypes())
			{
				if(!nameSpaces.Contains(tType.Namespace))
				{
					if(tType.Namespace==null)
					{
						if(!nameSpaces.Contains(""))
						{
							nameSpaces.Add("");
						}
					}
					else
					{
						nameSpaces.Add(tType.Namespace);
					}
				}
			}
			Map cachedAssemblyInfoMap=new Map();
			Map nameSpace=new Map(); 
			Integer counter=new Integer(0);
			foreach(string na in nameSpaces)
			{
				nameSpace[counter]=new Map(na);
				counter++;
			}
			cachedAssemblyInfoMap[new Map("namespaces")]=nameSpace;
			cachedAssemblyInfoMap[new Map("timestamp")]=new Map(File.GetLastWriteTime(assembly.Location).ToString());
			cachedAssemblyInfo[new Map(assembly.Location)]=cachedAssemblyInfoMap;
			return nameSpaces;
		}
		public Map LoadNamespaces(ArrayList assemblies)
		{
			LazyNamespace root=new LazyNamespace("");
			foreach(Assembly assembly in assemblies)
			{
				ArrayList nameSpaces=NameSpaces(assembly);
				CachedAssembly cachedAssembly=new CachedAssembly(assembly);
				foreach(string nameSpace in nameSpaces)
				{
					LazyNamespace selected=root;
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
								selected.namespaces[subString]=new LazyNamespace(fullName);
							}
							selected=(LazyNamespace)selected.namespaces[subString];
						}
					}
					selected.cachedAssemblies.Add(cachedAssembly);
				}
			}
			
			root.Load();
			return root.cache;
		}
		public static Library library=new Library();
		private Map cache=new Map();
		public static string libraryPath="library"; 
	}
	public class MapAdapter
	{ 
		// TODO: Make this a whole IMap implementation?, maybe, but might be too much work, do it if needed by library
		Map map;
		public MapAdapter(Map map)
		{
			this.map=map;
		}
		public MapAdapter()
		{
			this.map=new Map();
		}
		public object this[object key]
		{
			get
			{
				return Interpreter.DotNetFromMeta(map[Interpreter.MetaFromDotNet(key)]);
			}
			set
			{
				this.map[Interpreter.MetaFromDotNet(key)]=Interpreter.MetaFromDotNet(value);
			}
		}
	}
	// TODO: There should be a logical type that encompasses all Meta types
	// and a logical type that encompasses all real .NET types that have been converted, we will see if that is really useful

	//TODO: cache the Array somewhere; put in an "Add" method
	public class Map: IKeyValue, IMap, ICallable, IEnumerable, ISerializeSpecial
	{

		public object argument
		{
			get
			{
				return arg;
			}
			set
			{ 
				// TODO: Remove set, maybe?
				arg=value;
			}
		}
		object arg=null;
		public bool IsString
		{
			get
			{
				return strategy.IsString;
			}
		}
		public string String
		{
			get
			{
				return strategy.String;
			}
		}
		public IMap Parent
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
		public int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public ArrayList Array
		{ 
			get
			{
				return strategy.Array;
			}
		}
		public virtual object this[object key] 
		{
			get
			{
				if(key.Equals(Strings.Parent))
				{
					return Parent;
				}
				else if(key.Equals(Strings.Arg))
				{
					return argument;
				}
				else if(key.Equals(Strings.This))
				{
					return this;
				}
				else
				{
					object result=strategy[key];
					return result;
				}
			}
			set
			{
				if(value!=null)
				{
					isHashCached=false;
					if(key.Equals(Strings.This))
					{
						this.strategy=((Map)value).strategy.Clone();
					}
					else
					{
						object val=value is IMap? ((IMap)value).Clone(): value; // TODO: combine with next line
						if(value is IMap)
						{
							((IMap)val).Parent=this;
						}
						strategy[key]=val;
					}
				}
			}
		}
		public object Call(object argument)
		{
			this.argument=argument;
			Expression function=(Expression)((Map)this[Strings.Run]).GetExpression();
			object result;
			result=function.Evaluate(this);
			return result;
		}
		public ArrayList Keys
		{
			get
			{
				return strategy.Keys;
			}
		}
		public IMap Clone()
		{
			Map clone=strategy.CloneMap();
			clone.Parent=Parent;
			clone.expression=expression;
			clone.Extent=Extent;
			return clone;
		}
		public Expression GetExpression()  // this doesn't belong here, it's just here because of optimization, move the real work out of here
		{ 
			// expression Statements are not cached, only expressions

			// no caching anymore, because of possible issues when reverse-debugging
//			if(expression==null) 
//			{
				if(this.ContainsKey(Strings.Call))
				{
					expression=new Call((Map)this[Strings.Call]);
				}
				else if(this.ContainsKey(Strings.Delayed))
				{ 
					expression=new Delayed((Map)this[Strings.Delayed]);
				}
				else if(this.ContainsKey(Strings.Program))
				{
					expression=new Program((Map)this[Strings.Program]);
				}
				else if(this.ContainsKey(Strings.Literal))
				{
					expression=new Literal((Map)this[Strings.Literal]);
				}
				else if(this.ContainsKey(Strings.Search))
				{
					expression=new Search((Map)this[Strings.Search]);
				}
				else if(this.ContainsKey(Strings.Select))
				{
					expression=new Select((Map)this[Strings.Select]);
				}
				else
				{
					throw new ApplicationException("Cannot compile non-code map.");
				}
//			}
				((Expression)expression).Extent=this.Extent;
			return expression;
		}
		public bool ContainsKey(object key) 
		{
			if(key is Map)
			{
				if(key.Equals(Strings.Arg))
				{
					return this.argument!=null;
				}
				else if(key.Equals(Strings.Parent))
				{
					return this.Parent!=null;
				}
				else if(key.Equals(Strings.This))
				{
					return true;
				}
			}
			return strategy.ContainsKey(key);
		}
		public override bool Equals(object toCompare)
		{
			bool isEqual=false;
			if(Object.ReferenceEquals(toCompare,this))
			{
				isEqual=true;
			}
			else if(toCompare is Map)
			{
				isEqual=((Map)toCompare).strategy.Equals(strategy);
			}
			return isEqual;
		}
		public IEnumerator GetEnumerator()
		{
			return new MapEnumerator(this);
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
		/* TODO: Move some more logic into constructor instead of in Parser?
			* There is no clean separation then. But there isn't anyway. I could make 
			* it so that only the extent gets passed, that's probably best*/
		public Map(string text):this(new StringStrategy(text))
		{
		}
		public Map(MapStrategy strategy)
		{
			this.strategy=strategy;
			this.strategy.map=this;
		}
		public Map():this(new HybridDictionaryStrategy())
		{
		}
		private IMap parent;
		private MapStrategy strategy;
		public Expression expression; // why have this at all, why not for statements? probably a question of performance.
		public string Serialize(string indentation,string[] functions)
		{
			if(this.IsString)
			{
				return indentation+"\""+this.String+"\""+"\n";
			}
			else
			{
				return null;
			}
		}

		public abstract class MapStrategy
		{
			public Map map;
			public MapStrategy Clone()
			{
				MapStrategy strategy=new HybridDictionaryStrategy();
				foreach(object key in this.Keys)
				{
					strategy[key]=this[key];
				}
				return strategy;	
			}
			public abstract Map CloneMap();
			public abstract ArrayList Array
			{
				get;
			}
			public abstract bool IsString
			{
				get;
			}
			public abstract string String
			{
				get;
			}
			public abstract ArrayList Keys
			{
				get;
			}
			public abstract int Count
			{
				get;
			}
			public abstract object this[object key] 
			{
				get;
				set;
			}

			public abstract bool ContainsKey(object key);
			public override int GetHashCode() 
			{
				int hash=0;
				foreach(object key in this.Keys)
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
				// TODO: check whether this is a clone of the other MapStrategy (cloning isn't used yet)
				else if(!(strategy is MapStrategy))
				{
					return false;
				}
				else if(((MapStrategy)strategy).Count!=this.Count)
				{
					return false;
				}
				foreach(object key in this.Keys) 
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
			// is this really identical with the other strategies? See Hashcode of Integer class to make sure
			public override int GetHashCode()
			{
				int hash=0;
				for(int i=0;i<text.Length;i++)
				{
					hash+=(i+1)*text[i];
				}
				return hash;
			}
			// TODO: Make single exit
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

//			public override bool Equals(object strategy)
//			{
//				if(strategy is StringStrategy)
//				{	
//					return ((StringStrategy)strategy).text.Equals(this.text);
//				}
//				else
//				{
//					return base.Equals(strategy);
//				}
//			}
			public override Map CloneMap()
			{
				return new Map(new StringStrategy(this));
			}
			public override ArrayList Array
			{
				get
				{
					ArrayList list=new ArrayList();
					foreach(char iChar in text)
					{
						list.Add(new Integer(iChar));
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
					// TODO: make this lazy? it won't work with unicode anymore then, though
					// TODO: Make this unicode-safe in the first place!
					keys.Add(new Integer(i));			
				}
			}
			public override int Count
			{
				get
				{
					return text.Length;
				}
			}
			public override object this[object key] 
			{
				get
				{
					if(key is Integer)
					{
						int iInteger=((Integer)key).Int;
						if(iInteger>0 && iInteger<=this.Count)
						{
							return new Integer(text[iInteger-1]);
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
			public override bool ContainsKey(object key) 
			{
				if(key is Integer)
				{
					return ((Integer)key)>0 && ((Integer)key)<=this.Count;
				}
				else
				{
					return false;
				}
			}
		}
		public class HybridDictionaryStrategy:MapStrategy
		{
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
			public override Map CloneMap()
			{
				Map clone=new Map(new HybridDictionaryStrategy(this.keys.Count));
				foreach(object key in keys)
				{
					clone[key]=strategy[key];
				}
				return clone;
			}
			public override ArrayList Array
			{
				get
				{
					ArrayList list=new ArrayList();
					for(Integer iInteger=new Integer(1);ContainsKey(iInteger);iInteger++)
					{
						list.Add(this[iInteger]);
					}
					return list;
				}
			}
			public override bool IsString
			{
				get
				{
					if(Array.Count>0)
					{
						try
						{
							object o=String;// TODO: a bit of a hack, just trying if "String" works or not
							return true;
						}
						catch{
						}
					}
					return false;
				}
			}
			public override string String
			{
				get
				{
					string text="";
					foreach(object key in this.Keys)
					{
						if(key is Integer && this.strategy[key] is Integer)
						{
							try
							{
								text+=Convert.ToChar(((Integer)this.strategy[key]).Int);
							}
							catch
							{
								throw new MapException(this.map,"Map is not a string");
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
			public override object this[object key] 
			{
				get
				{
					return strategy[key];
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
			public override bool ContainsKey(object key) 
			{
				return strategy.Contains(key);
			}
		}
	}
	public class MapEnumerator: IEnumerator
	{
		private Map map; public MapEnumerator(Map map)
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
	public delegate object DelegateCreatedForGenericDelegates(); // TODO: rename?
	public class DotNetMethod: ICallable
	{
		// TODO: Move this to "With" ? Move this to DotNetContainer?
		public static object AssignCollection(Map map,object collection,out bool isSuccess)
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
				||parameter.Equals(typeof(Delegate))) && (meta is Map))
			{
				MethodInfo invoke=parameter.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate function=CreateDelegateFromCode(parameter,invoke,(Map)meta);
				return function;
			}
			else if(parameter.IsArray && meta is IMap && ((Map)meta).Array.Count!=0)
			{
				try
				{
					Type type=parameter.GetElementType();
					Map argument=((Map)meta);
					Array arguments=Array.CreateInstance(type,argument.Array.Count);
					for(int i=0;i<argument.Count;i++)
					{
						arguments.SetValue(argument[new Integer(i+1)],i);
					}
					return arguments;
				}
				catch
				{
				}
			}
			else
			{
				bool converted; // TODO: get rid of this, can't really work correctly
				object result=Interpreter.DotNetFromMeta(meta,parameter,out converted);
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
		public object Call(object argument)
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
					isExecuted=true;// remove, use argumentsMatched instead
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
						// don't match if different parameter list length
						if(((IMap)argument).Array.Count==((IMap)argument).Keys.Count)
						{ 
							// only call if there are no non-integer keys ( move this somewhere else)
							rightNumberArgumentMethods.Add(method);
						}
					}
				}
				if(rightNumberArgumentMethods.Count==0)
				{
					throw new ApplicationException("Method "+this.name+": No methods with the right number of arguments.");// TODO: Just a quickfix, really
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
					// TODO: remove Hungarian in this method
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
						isExecuted=true;// remove, use argumentsMatched instead
						break;
					}
				}
			}
			if(!isExecuted)
			{
				throw new ApplicationException("Method "+this.name+" could not be called.");
			}
			return Interpreter.MetaFromDotNet(result);
		}
		// TODO: check use cases of this method, improve
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
			string argumentBuiling="Map arg=new Map();";
			if(method!=null)
			{
				foreach(ParameterInfo parameter in method.GetParameters())
				{
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentBuiling+="arg[new Integer("+counter+")]=arg"+counter+";";
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
					source+="Interpreter.DotNetFromMeta(result,typeof("+returnType+"));"; 
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
			if(method==null) // somewhat unlogical
			{
				delegateType=typeof(DelegateCreatedForGenericDelegates); // TODO: use MethodInvoker here!!
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
//			methods.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove

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
	public class DotNetClass: DotNetContainer, IKeyValue,ICallable
	{
		protected DotNetMethod constructor;
		public DotNetClass(Type targetType):base(null,targetType)
		{
			this.constructor=new DotNetMethod(this.targetType);
		}
		public object Call(object argument)
		{
			return constructor.Call(argument);
		}
	}
	public class DotNetObject: DotNetContainer, IKeyValue
	{
		public DotNetObject(object target):base(target,target.GetType()) // should maybe have a test, whether target is null????
		{
		}
		public override string ToString()
		{
			return target.ToString();
		}
	}
	/* Base class for DotNetObject and DotNetClass. */
	public abstract class DotNetContainer: IKeyValue, IEnumerable,ISerializeSpecial
	{
		public bool ContainsKey(object key)
		{
			if(key is Map)
			{
				if(((Map)key).IsString)
				{
					string text=((Map)key).String;
					if(targetType.GetMember(((Map)key).String,
						BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
					{
						return true;
					}
				}
			}
			DotNetMethod indexer=new DotNetMethod("get_Item",target,targetType);
			Map argument=new Map();
			argument[new Integer(1)]=key;
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
		public IEnumerator GetEnumerator()
		{
			return MTable.GetEnumerator();
		}
		// TODO: why does DotNetContainer have a parent when it isn't ever used?
		public IKeyValue Parent
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
		public ArrayList Keys
		{
			get
			{
				return new ArrayList(MTable.Keys);
			}
		}
		public int Count 
		{
			get
			{
				return MTable.Count;
			}
		}
		public virtual object this[object key] 
		{
			get
			{
				if(key is Map && ((Map)key).IsString)
				{
					string text=((Map)key).String;
					MemberInfo[] members=targetType.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
					if(members.Length>0)
					{
						if(members[0] is MethodBase)
						{
							return new DotNetMethod(text,target,targetType);
						}
						if(members[0] is FieldInfo)
						{
							return Interpreter.MetaFromDotNet(targetType.GetField(text).GetValue(target));
						}
						else if(members[0] is PropertyInfo)
						{
							return Interpreter.MetaFromDotNet(targetType.GetProperty(text).GetValue(target,new object[]{}));
						}
						else if(members[0] is EventInfo)
						{
							Delegate eventDelegate=(Delegate)targetType.GetField(text,BindingFlags.Public|
								BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(target);
							if(eventDelegate==null) // is there anything wrong here??
							{
								return null;
							}
							else
							{
								return new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
						}
						// this should only work in DotNetClass, maybe specify the BindingFlags used above in DotNetClass and DotNetObject
						else if(members[0] is Type)
						{
							return new DotNetClass((Type)members[0]);
						}
					}
				}
				if(this.target!=null && key is Integer && this.targetType.IsArray)
				{
					return Interpreter.MetaFromDotNet(((Array)target).GetValue(((Integer)key).Int)); // TODO: add error handling here
				}
				DotNetMethod indexer=new DotNetMethod("get_Item",target,targetType);
				Map argument=new Map();
				argument[new Integer(1)]=key;
				try
				{
					return indexer.Call(argument);
				}
				catch(Exception e)
				{
					return null;
				}
			}
			set
			{
				if(key is Map && ((Map)key).IsString)
				{
					string text=((Map)key).String;
					if(text.Equals("DebugBreak"))
					{
						int asdf=0;
					}
					if(text.Equals("Test"))
					{
						int asdf=0;
					}
					MemberInfo[] members=targetType.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
					if(members.Length>0)
					{
						if(members[0] is MethodBase)
						{
							throw new ApplicationException("Cannot set invoke "+key+".");
						}
						else if(members[0] is FieldInfo)
						{
							FieldInfo field=(FieldInfo)members[0];
							bool isConverted;
							object val;
							val=DotNetMethod.ConvertParameter(value,field.FieldType,out isConverted);
							if(isConverted)
							{
								field.SetValue(target,val);
							}
							if(!isConverted)
							{
								if(value is Map)
								{
									val=DotNetMethod.AssignCollection((Map)value,field.GetValue(target),out isConverted);
								}
							}
							if(!isConverted)
							{
								throw new ApplicationException("Field "+field.Name+"could not be assigned because it cannot be isConverted.");
							}
							//TODO: refactor
							return;
						}
						else if(members[0] is PropertyInfo)
						{
							PropertyInfo property=(PropertyInfo)members[0];
							bool isConverted;
							object val=DotNetMethod.ConvertParameter(value,property.PropertyType,out isConverted);
							if(isConverted)
							{
								property.SetValue(target,val,new object[]{});
							}
							if(!isConverted)
							{
								if(value is Map)
								{
									DotNetMethod.AssignCollection((Map)value,property.GetValue(target,new object[]{}),out isConverted);
								}
								if(!isConverted)
								{
									throw new ApplicationException("Property "+this.targetType.Name+"."+Interpreter.SaveToFile(key,"",false)+" could not be set to "+value.ToString()+". The value can not be isConverted.");
								}
							}
							return;
						}
						else if(members[0] is EventInfo)
						{
							if(members[0].Name.Equals("BreakPoint"))
							{
								int asdf=0;
							}
							((EventInfo)members[0]).AddEventHandler(target,CreateEvent(text,(Map)value));
							return;
						}
					}
				}
				if(target!=null && key is Integer && targetType.IsArray)
				{
					bool isConverted; 
					object converted=Interpreter.DotNetFromMeta(value,targetType.GetElementType(),out isConverted);
					if(isConverted)
					{
						((Array)target).SetValue(converted,((Integer)key).Int);
						return;
					}
				}
				DotNetMethod indexer=new DotNetMethod("set_Item",target,targetType);
				Map argument=new Map();
				argument[new Integer(1)]=key;
				argument[new Integer(2)]=value;// do this more efficiently?
				try
				{
					indexer.Call(argument);
				}
				catch(Exception e)
				{
					throw new ApplicationException("Cannot set "+Interpreter.DotNetFromMeta(key).ToString()+".");// use a KeyException or something like that here
				}
			}
		}
		public string Serialize(string indent,string[] functions)
		{
			return indent;
		}
		public Delegate CreateEvent(string name,Map code)
		{
			EventInfo eventInfo=targetType.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
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
				BindingFlags bindingFlags;
				if(target==null) 
				{
					bindingFlags=BindingFlags.Public|BindingFlags.Static;
				}
				else 
				{
					bindingFlags=BindingFlags.Public|BindingFlags.Instance;
				}
				foreach(FieldInfo field in targetType.GetFields(bindingFlags))
				{
					table[new Map(field.Name)]=field.GetValue(target);
				}
				foreach(MethodInfo invoke in targetType.GetMethods(bindingFlags)) 
				{
					if(!invoke.IsSpecialName)
					{
						table[new Map(invoke.Name)]=new DotNetMethod(invoke.Name,target,targetType);
					}
				}
				foreach(PropertyInfo property in targetType.GetProperties(bindingFlags))
				{
					if(property.Name!="Item" && property.Name!="Chars")
					{
						table[new Map(property.Name)]=property.GetValue(target,new object[]{});
					}
				}
				foreach(EventInfo eventInfo in targetType.GetEvents(bindingFlags))
				{
					table[new Map(eventInfo.Name)]=new DotNetMethod(eventInfo.GetAddMethod().Name,this.target,this.targetType);
				}
				foreach(Type tNested in targetType.GetNestedTypes(bindingFlags))
				{ 
					// not sure the BindingFlags are correct
					table[new Map(tNested.Name)]=new DotNetClass(tNested);
				}
				int counter=1;
				if(target!=null && target is IEnumerable && !(target is String))
				{ 
					// is this useful?
					foreach(object entry in (IEnumerable)target)
					{
						if(entry is DictionaryEntry)
						{
							table[Interpreter.MetaFromDotNet(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
						}
						else
						{
							table[new Integer(counter)]=entry;
							counter++;
						}
					}
				}
				return table;
			}
		}
		public DotNetContainer(object target,Type targetType)
		{
			this.target=target;
			this.targetType=targetType;
		}
		private IKeyValue parent;
		public object target;
		public Type targetType;
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
						case MetaLexerTokenTypes.LITERAL: // move this into parser, for correct error handling?
							string indentation="";
							for(int i=0;i<indentationDepth+1;i++)
							{
								indentation+='\t';
							}
							string text=token.getText();
							text=text.Replace(Environment.NewLine,"\n"); // replace so we can use Split, which only works with characters
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
				// TODO: use Extent instead of Token, or just the line we're in
				int difference=newIndentationDepth-indentationDepth; 
				if(difference==0)
				{
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));//TODO: use something else here
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
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE)); // TODO: tiny bit unlogical? maybe create this in Parser?
				}
				else if(difference>1)
				{
					// This doesn't get through properly because it is caught by ANTLR
					// TODO: make extra exception later.
					// I don't understand it and the lines are somehow off
					throw new RecognitionException("Incorrect indentation.",currentToken.getFilename(),currentToken.getLine(),currentToken.getColumn());
				}
				indentationDepth=newIndentationDepth;
			}
			protected Queue tokenBuffer=new Queue();
			protected TokenStream tokenStream;
			protected int indentationDepth=-1;
		}
	}
	public class Utility
	{
		public static string ReadFile(string fileName)
		{
			StreamReader reader=new StreamReader(fileName);
			string result=reader.ReadToEnd();
			reader.Close();
			return result;
		}
	}
	namespace TestingFramework
	{
		public interface ISerializeSpecial
		{
			// TODO: add separate function or add "out" parameter, that says whether or not the special serialization has actually worked
			string Serialize(string indent,string[] functions); // TODO: this is much too complicated, and rarely needed, maybe get rid of it completely, if possible
			// TODO: at least add the indent automatically, and do not use the functions
		}
		public abstract class TestCase
		{
			public abstract object Run();
		}
		public class ExecuteTests
		{	
			public ExecuteTests(Type tTescontainerType,string fnResults)
			{ 
				bool isWaitAtEnd=false;
				Type[] testTypes=tTescontainerType.GetNestedTypes();
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
					bool isSuccessful=CompareResult(Path.Combine(fnResults,testType.Name),result,methodNames);
					if(!isSuccessful)
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
			private bool CompareResult(string path,object ttoSerialize,string[] functions)
			{
				Directory.CreateDirectory(path);
				if(!File.Exists(Path.Combine(path,"check.txt")))
				{
					File.Create(Path.Combine(path,"check.txt")).Close();
				}
				StringBuilder stringBuilder=new StringBuilder();
				Serialize(ttoSerialize,"",functions,stringBuilder);
				string result=stringBuilder.ToString();
				StreamWriter resultWriter=new StreamWriter(Path.Combine(path,"result.txt")); // factor this stuff out?, into what class?
				resultWriter.Write(result);
				resultWriter.Close();
				StreamWriter resultCopyWriter=new StreamWriter(Path.Combine(path,"resultCopy.txt"));
				resultCopyWriter.Write(result);
				resultCopyWriter.Close();
				string check=Utility.ReadFile(Path.Combine(path,"check.txt"));
				return result.Equals(check);
			}
			public static void Serialize(object toSerialize,string indent,string[] methods,StringBuilder stringBuilder) 
			{
				if(toSerialize==null) 
				{
					stringBuilder.Append(indent+"null\n");
				}
				else if(toSerialize is ISerializeSpecial && ((ISerializeSpecial)toSerialize).Serialize(indent,methods)!=null) 
				{
					stringBuilder.Append(((ISerializeSpecial)toSerialize).Serialize(indent,methods));
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
		// TODO: maybe add classes "Column" and "Line"
		// TODO: on the other hand, maybe we should simply use the "Point" class, instead of "Position"
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

