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
	}
	public class SpecialKeys
	{
		public static readonly Map Parent=new Map("parent"); 
		public static readonly Map Arg=new Map("arg");
		public static readonly Map This=new Map("this");
	}
	public abstract class Expression
	{
		public virtual bool Stop()
		{
			bool stop=false;
			if(Interpreter.BreakPoint!=null)
			{
				if(Interpreter.BreakPoint.Line>=Extent.Start.Line && Interpreter.BreakPoint.Line<=Extent.End.Line)
				{
					if(Interpreter.BreakPoint.Column>=Extent.Start.Column && Interpreter.BreakPoint.Column<=Extent.End.Column)
					{
						stop=true;
					}
				}
			}
			return stop;
		}
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

		public override object EvaluateImplementation(IMap parent)
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
			this.callable=((Map)code[CodeKeys.Function]).GetExpression();
			this.argument=((Map)code[CodeKeys.Argument]).GetExpression();
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
			if(Interpreter.BreakPoint!=null)
			{
				if(Interpreter.BreakPoint.Line==Extent.End.Line+1 && Interpreter.BreakPoint.Column==1)
				{
					stop=true;
				}
			}
			return stop;
		}
		public override object EvaluateImplementation(IMap parent)
		{
			object local=new Map();
			Evaluate(parent,ref local);
			return local;
		}
		public void Evaluate(IMap parent,ref object local)
		{
			((IMap)local).Parent=parent;
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
		public Program(Map code)
		{
			foreach(Map statement in code.Array)
			{
				this.statements.Add(new Statement(statement));
			}
		}
		public readonly ArrayList statements=new ArrayList();
	}
	public class BreakPoint
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
		// TODO: use Position
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

	public abstract class Recognition
	{
		public abstract object Recognize(string text);
	}
	public class Recognitions
	{
		public class IntegerRecogition: Recognition 
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
					// TODO: make unicode-safe
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
		public class StringRecognition:Recognition
		{
			public override object Recognize(string text)
			{
				return new Map(text);
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

		public override object EvaluateImplementation(IMap parent)
		{
			return literal;
		}
		public Literal(Map code)
		{
			this.literal=Recognition((string)code.String);
		}
		public object literal=null;
		public static object Recognition(string text)
		{
			foreach(Recognition recognition in recognitions)
			{
				object recognized=recognition.Recognize(text);
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
				if(!(selected is IKeyValue))
				{
					selected=new DotNetObject(selected);// TODO: put into Map.this[]
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
			if(replaceMap!=null) // TODO: "this" specially
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
		public void Realize(ref object parent)
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
					selected=new DotNetObject(selected);// TODO: put this into Map.this[]
				}
			}
			object lastKey=((Expression)keys[keys.Count-1]).Evaluate((IMap)parent);
			object val=expression.Evaluate((IMap)parent);
			if(lastKey.Equals(SpecialKeys.This))
			{
				if(val is Map)
				{
					((Map)val).Parent=((Map)parent).Parent;
				}
				parent=val;
			}
			else
			{
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

				((IKeyValue)selected)[lastKey]=val;
			}
		}
		public Statement(Map code) 
		{
			if(code.ContainsKey(CodeKeys.Search))
			{
				searchFirst=true;
			}
			foreach(Map key in ((Map)code[CodeKeys.Key]).Array)
			{
				keys.Add(key.GetExpression());
			}
			this.expression=(Expression)((Map)code[CodeKeys.Value]).GetExpression();
		}
		public ArrayList keys=new ArrayList();
		public Expression expression;
		
		bool searchFirst=false;
	}
	public class IMeta
	{
	}

	public class Interpreter
	{
		
		
		public static BreakPoint BreakPoint // TODO: move to Expression
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

		private static object debugValue="";
		public static object DebugValue// TODO: put into expression
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
		public static void SaveToFile(object meta,string path)// move into core functionality
		{
			StreamWriter streamWriter=new StreamWriter(path);
			streamWriter.Write(SaveToFile(meta,"",true).TrimEnd(new char[]{'\n'}));
			streamWriter.Close();
		}
		public static string Serialize(object meta)
		{
			return SaveToFile(meta,"",true);
		}
		// TODO: extend, integrate into Directory
		public static string SaveToFile(object meta,string indent,bool isRightSide)
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
				return "\""+meta.ToString()+"\"";
				//throw new ApplicationException("Serialization not implemented for type "+meta.GetType().ToString()+".");
			}
		}
		public static IKeyValue Merge(params IKeyValue[] arkvlToMerge)
		{
			return MergeCollection(arkvlToMerge);
		}
		public static IKeyValue MergeCollection(ICollection collection)
		{
			Map result=new Map();
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
		public static object Run(string fileName,IMap argument)
		{
			Map program=Interpreter.Compile(fileName);
			return CallProgram(program,argument,Library.library);
		}
		public static object RunWithoutLibrary(string fileName,IMap argument)
		{
			Map program=Compile(fileName);
			return CallProgram(program,argument,null);
		}
		public static object CallProgram(Map program,IMap argument,IMap parent)
		{
			Map callable=new Map();
			callable[CodeKeys.Run]=program;
			callable.Parent=parent;
			return callable.Call(argument);
		}
		public static Map Compile(string fileName) // TODO: move this into Directory
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
		// TODO: rethinkg these things, not good to put it here, maybe integrate somehow
		private static void ExecuteInThread()
		{
			Interpreter.Run(executeFileName,new Map());
			int asdf=0;
		}
		private static string executeFileName="";
		public static void StartDebug(string fileName) // TODO: make debugging not special case
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

			installationPath=@"c:\_projectsupportmaterial\meta";
		}
		public static string installationPath;
		public static ArrayList loadedAssemblies=new ArrayList();
	}
	public class MetaException:ApplicationException // TODO: revise the exception handling, propagation and output, hierarchy
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
	public abstract class KeyException:MetaException // TODO: improve output
	{ 
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
	public class KeyNotFoundException:KeyException
	{
		public KeyNotFoundException(object key,Extent extent):base(key,extent)
		{
		}
	}
	public class KeyDoesNotExistException:KeyException // TODO: rename
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
	public abstract class IMap: IKeyValue // TODO: rename
	{
		public abstract IMap Parent
		{
			get;
			set;
		}
		public abstract ArrayList Array
		{
			get;
		}
		public abstract IMap Clone();

		public abstract object this[object key]
		{
			get;
			set;
		}

		public abstract ArrayList Keys
		{
			get;
		}

		public abstract int Count
		{
			get;
		}

		public abstract bool ContainsKey(object key);
		public abstract IEnumerator GetEnumerator();

	}
	// TODO: remove IKeyValue - IMap distinction?
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
	public class MetaLibrary // TODO: remove, integrate into Directory
	{
		public object Load()
		{
			return Interpreter.Run(path,new Map());
		}
		public MetaLibrary(string path)
		{
			this.path=path;
		}
		string path;
	}
	public class LazyNamespace: IKeyValue // TODO: integrate into Directory
	{ 
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
	public class CachedAssembly // TODO: integrate into Directory
	{  
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
	public class Library: IMap,IKeyValue // TODO: split into GAC and Directory
	{
		public override object this[object key]
		{
			get
			{
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
		public override ArrayList Keys
		{
			get
			{
				return cache.Keys;
			}
		}
		public override IMap Clone()
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
		public override bool ContainsKey(object key)
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
		public override IMap Parent
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
		public override IEnumerator GetEnumerator()
		{ 
			foreach(DictionaryEntry entry in cache)
			{ 
				object temp=cache[entry.Key];
			}
			return cache.GetEnumerator();
		}
		public static Map LoadAssemblies(IEnumerable assemblies)
		{
			Map root=new Map();
			foreach(Assembly currentAssembly in assemblies)
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

		public Library()
		{
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
				cachedAssemblyInfo=(Map)Interpreter.RunWithoutLibrary(cachedAssemblyPath,new Map());
			}
			
			cache=LoadNamespaces(assemblies);
			Interpreter.SaveToFile(cachedAssemblyInfo,cachedAssemblyPath);
			foreach(string meta in System.IO.Directory.GetFiles(libraryPath,"*.meta"))
			{
				cache[new Map(Path.GetFileNameWithoutExtension(meta))]=new MetaLibrary(meta);
			}
		}
		private Map cachedAssemblyInfo=new Map();
		public ArrayList NameSpaces(Assembly assembly) //TODO: integrate into LoadNamespaces???
		{ 
			ArrayList nameSpaces=new ArrayList();
			MapAdapter cached=new MapAdapter(cachedAssemblyInfo);
			if(cached.ContainsKey(assembly.Location))
			{
				MapAdapter info=new MapAdapter((Map)cached[assembly.Location]);
				string timestamp=(string)info["timestamp"];
				if(timestamp.Equals(File.GetLastWriteTime(assembly.Location).ToString()))
				{
					MapAdapter namespaces=new MapAdapter((Map)info["namespaces"]);
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
//	public class PersistentMap:IMap
//	{
//		#region IMap Members
//
//		public IMap Parent
//		{
//			get
//			{
//				// TODO:  Add PersistentMap.Parent getter implementation
//				return null;
//			}
//			set
//			{
//				// TODO:  Add PersistentMap.Parent setter implementation
//			}
//		}
//
//		public ArrayList Array
//		{
//			get
//			{
//				// TODO:  Add PersistentMap.Array getter implementation
//				return null;
//			}
//		}
//
//		public IMap Clone()
//		{
//			// TODO:  Add PersistentMap.Clone implementation
//			return null;
//		}
//
//		#endregion
//
//		#region IKeyValue Members
//
//		public object this[object key]
//		{
//			get
//			{
//				// TODO:  Add PersistentMap.this getter implementation
//				return null;
//			}
//			set
//			{
//				// TODO:  Add PersistentMap.this setter implementation
//			}
//		}
//
//		public ArrayList Keys
//		{
//			get
//			{
//				// TODO:  Add PersistentMap.Keys getter implementation
//				return null;
//			}
//		}
//
//		public int Count
//		{
//			get
//			{
//				// TODO:  Add PersistentMap.Count getter implementation
//				return 0;
//			}
//		}
//
//		public bool ContainsKey(object key)
//		{
//			// TODO:  Add PersistentMap.ContainsKey implementation
//			return false;
//		}
//
//		#endregion
//
//		#region IEnumerable Members
//
//		public IEnumerator GetEnumerator()
//		{
//			// TODO:  Add PersistentMap.GetEnumerator implementation
//			return null;
//		}
//
//		#endregion
//
//	}
//	public class PersistantMapStrategy
//	{
//	}
//	public class MetaFileStrategy
//	{
//	}
//	public class DirectoryStrategy:PersistantMapStrategy
//	{
//		//		private string path;
//		private DirectoryInfo directory;
//		private IMap parent=null;
//		public DirectoryStrategy(DirectoryInfo directory)
//		{
//			//			directory=new DirectoryInfo(path);
//			if(directory.Parent!=null)
//			{
//				parent=new Directory(directory.Parent);
//			}
//			else
//			{
//				parent=Library.library;
//			}
//			//			this.path=path;
//		}
//		public IMap Parent
//		{
//			get
//			{
//				return parent;
//			}
//			set
//			{
//				throw new ApplicationException("Tried to set parent of directory. "+directory.FullName);
//			}
//		}
//		public ArrayList Array
//		{
//			get
//			{
//				return new ArrayList(); // TODO: maybe we interpret file names 1,2,3 as numbers, too??? would make sense, actually
//			}
//		}
//		public IMap Clone()
//		{
//			throw new ApplicationException("Clone in Directory not implemented yet"); // TODO
//		}
//		public object this[object key]
//		{
//			get
//			{
//				bool hasKey;
//				string fileName="";
//				if(key is Map)
//				{
//					Map map=(Map)key;
//					if(map.IsString)
//					{
//						fileName=map.String;
//					}
//				}
//				if(key is Integer)
//				{
//					fileName=((Integer)key).ToString();
//				}
//				object result;
//				if(fileName!="")
//				{
//					if(System.IO.Directory.Exists(fileName)) // TODO: maybe always use DirectoryInfo instead of System.IO.Directory
//					{
//						result=new Directory(new DirectoryInfo(fileName));
//					}
//					else 
//					{
//						if(fileName.EndsWith(".meta"))
//						{
//							result=new MetaFile(fileName);
//						}
//						else
//						{
//							ArrayList bytes=new ArrayList(); // amazingly slow, but binary reading ist just too stupid
//							FileStream stream=new FileStream(fileName,FileMode.Open);
//							while(true)
//							{
//								int read=stream.ReadByte();
//								if(read>=0)
//								{
//									bytes.Add(read);
//								}
//								else
//								{
//									break;
//								}
//							}
//							Byte[] array=(Byte[])bytes.ToArray(typeof(byte));
//							Integer integer=new Integer(array);
//							result=integer;
//						}
//					}
//				}
//				else
//				{
//					result=null;
//				}
//				return result;
//			}
//			set
//			{
//				// TODO:  Uh, oh complicated, throw for now
//				throw new ApplicationException("Setting in directories not implemented yet.");
//			}
//		}
//		public ArrayList Keys
//		{
//			get
//			{
//				ArrayList keys=new ArrayList();
//				foreach(DirectoryInfo dir in directory.GetDirectories())
//				{
//					keys.Add(Literal.Recognition(dir.Name));
//				}
//				foreach(FileInfo file in directory.GetFiles())
//				{
//					keys.Add(Literal.Recognition(file.Name));
//				}
//				return keys;
//			}
//		}
//
//		public int Count
//		{
//			get
//			{
//				return Keys.Count;
//			}
//		}
//
//		public bool ContainsKey(object key)
//		{
//			return Keys.Contains(key);
//		}
//
//		public IEnumerator GetEnumerator()
//		{
//			return new MapEnumerator(this);
//		}
//	}

//	public class MetaFile
//	{
//	}
//	public class Directory:IMap
//	{
////		private string path;
//		private DirectoryInfo directory;
//		private IMap parent=null;
//		public Directory(DirectoryInfo directory)
//		{
////			directory=new DirectoryInfo(path);
//			if(directory.Parent!=null)
//			{
//				parent=new Directory(directory.Parent);
//			}
//			else
//			{
//				parent=Library.library;
//			}
////			this.path=path;
//		}
//		public IMap Parent
//		{
//			get
//			{
//				return parent;
//			}
//			set
//			{
//				throw new ApplicationException("Tried to set parent of directory. "+directory.FullName);
//			}
//		}
//		public ArrayList Array
//		{
//			get
//			{
//				return new ArrayList(); // TODO: maybe we interpret file names 1,2,3 as numbers, too??? would make sense, actually
//			}
//		}
//		public IMap Clone()
//		{
//			throw new ApplicationException("Clone in Directory not implemented yet"); // TODO
//		}
//		public object this[object key]
//		{
//			get
//			{
//				bool hasKey;
//				string fileName="";
//				if(key is Map)
//				{
//					Map map=(Map)key;
//					if(map.IsString)
//					{
//						fileName=map.String;
//					}
//				}
//				if(key is Integer)
//				{
//					fileName=((Integer)key).ToString();
//				}
//				object result;
//				if(fileName!="")
//				{
//					if(System.IO.Directory.Exists(fileName)) // TODO: maybe always use DirectoryInfo instead of System.IO.Directory
//					{
//						result=new Directory(new DirectoryInfo(fileName));
//					}
//					else 
//					{
//						if(fileName.EndsWith(".meta"))
//						{
//							result=new MetaFile(fileName);
//						}
//						else
//						{
//							ArrayList bytes=new ArrayList(); // amazingly slow, but binary reading ist just too stupid
//							FileStream stream=new FileStream(fileName,FileMode.Open);
//							while(true)
//							{
//								int read=stream.ReadByte();
//								if(read>=0)
//								{
//									bytes.Add(read);
//								}
//								else
//								{
//									break;
//								}
//							}
//							Byte[] array=(Byte[])bytes.ToArray(typeof(byte));
//							Integer integer=new Integer(array);
//							result=integer;
//						}
//					}
//				}
//				else
//				{
//					result=null;
//				}
//				return result;
//			}
//			set
//			{
//				// TODO:  Uh, oh complicated, throw for now
//				throw new ApplicationException("Setting in directories not implemented yet.");
//			}
//		}
//		public ArrayList Keys
//		{
//			get
//			{
//				ArrayList keys=new ArrayList();
//				foreach(DirectoryInfo dir in directory.GetDirectories())
//				{
//					keys.Add(Literal.Recognition(dir.Name));
//				}
//				foreach(FileInfo file in directory.GetFiles())
//				{
//					keys.Add(Literal.Recognition(file.Name));
//				}
//				return keys;
//			}
//		}
//
//		public int Count
//		{
//			get
//			{
//				return Keys.Count;
//			}
//		}
//
//		public bool ContainsKey(object key)
//		{
//			return Keys.Contains(key);
//		}
//
//		public IEnumerator GetEnumerator()
//		{
//			return new MapEnumerator(this);
//		}
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
			if(target.IsSubclassOf(typeof(Enum)) && meta is Integer)
			{ 
				isConverted=true;
				return Enum.ToObject(target,((Integer)meta).Int);
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
		public static object ToMeta(object oDotNet)
		{ 
			if(oDotNet==null)
			{
				return null;
			}
			else if(oDotNet.GetType().IsSubclassOf(typeof(Enum)))
			{
				return new Integer((int)System.Convert.ToInt32((Enum)oDotNet));
			}
			ToMeta conversion=(ToMeta)toMeta[oDotNet.GetType()];
			if(conversion==null)
			{
				return oDotNet;
			}
			else
			{
				return conversion.Convert(oDotNet);
			}
		}
		public static object ToDotNet(object meta) 
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
		private static Hashtable toDotNet=new Hashtable();
		private static Hashtable toMeta=new Hashtable();
	}

	public abstract class ToMeta
	{ 
		public Type source;
		public abstract object Convert(object obj);
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
			public override object Convert(object toConvert)
			{
				return new Map((string)toConvert);
			}
		}
		public class ConvertBoolToInteger: ToMeta
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
		public class ConvertByteToInteger: ToMeta
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
		public class ConvertSByteToInteger: ToMeta
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
		public class ConvertCharToInteger: ToMeta
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
		public class ConvertInt32ToInteger: ToMeta
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
		public class ConvertUInt32ToInteger: ToMeta
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
		public class ConvertInt64ToInteger: ToMeta
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
		public class ConvertUInt64ToInteger: ToMeta
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
		public class ConvertInt16ToInteger: ToMeta
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
		public class ConvertUInt16ToInteger: ToMeta
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
	abstract class ToDotNetConversions
	{
		public class IntegerToByte: ToDotNet
		{
			public IntegerToByte()
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
		public class IntegerToBool: ToDotNet
		{
			public IntegerToBool()
			{
				this.source=typeof(Integer);
				this.target=typeof(bool);
			}
			public override object Convert(object toConvert, out bool isConverted)
			{
				object result;
				int i=((Integer)toConvert).Int;
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
				this.source=typeof(Integer);
				this.target=typeof(SByte);
			}
			public override object Convert(object toConvert, out bool isConverted)
			{
				isConverted=true;
				return System.Convert.ToSByte(((Integer)toConvert).LongValue());
			}
		}
		public class IntegerToChar: ToDotNet
		{
			public IntegerToChar()
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
		public class IntegerToInt32: ToDotNet
		{
			public IntegerToInt32()
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
		public class IntegerToUInt32: ToDotNet
		{
			public IntegerToUInt32()
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
		public class IntegerToInt64: ToDotNet
		{
			public IntegerToInt64()
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
		public class IntegerToUInt64: ToDotNet
		{
			public IntegerToUInt64()
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
		public class IntegerToInt16: ToDotNet
		{
			public IntegerToInt16()
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
		public class IntegerToUInt16: ToDotNet
		{
			public IntegerToUInt16()
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
		public class IntegerToDecimal: ToDotNet
		{
			public IntegerToDecimal()
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
		public class IntegerToDouble: ToDotNet
		{
			public IntegerToDouble()
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
		public class IntegerToFloat: ToDotNet
		{
			public IntegerToFloat()
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
		public class MapToString: ToDotNet
		{
			public MapToString()
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
		public class FractionToDecimal: ToDotNet
		{
			public FractionToDecimal()
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
		public class FractionToDouble: ToDotNet
		{
			public FractionToDouble()
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
		public class FractionToFloat: ToDotNet
		{
			public FractionToFloat()
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
	public class MapAdapter:IMap
	{ 
		private Map map;
		public MapAdapter(Map map)
		{
			this.map=map;
		}
		public MapAdapter()
		{
			this.map=new Map();
		}
		public override bool ContainsKey(object key)
		{
			return map.ContainsKey(Convert.ToMeta(key));
		}

		public override object this[object key]
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
		public override IMap Parent
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
		public override ArrayList Array
		{
			get
			{
				return ConvertToMeta(map.Array);
			}
		}
		public override IMap Clone()
		{
			return new MapAdapter((Map)map.Clone());
		}
		private ArrayList ConvertToMeta(ArrayList list)
		{
			ArrayList result=new ArrayList();
			foreach(object obj in list)
			{
				result.Add(Convert.ToDotNet(obj));
			}
			return result;
		}
		public override ArrayList Keys
		{
			get
			{
				return ConvertToMeta(map.Keys);
			}
		}
		public override int Count
		{
			get
			{
				return map.Count;
			}
		}
		public override IEnumerator GetEnumerator()
		{
			return new MapEnumerator(this);
		}
	}
	public class Map: IMap, IKeyValue, ICallable, IEnumerable, ISerializeSpecial
	{

		public object Argument
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
		public bool IsString // TODO: move to IMap
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
		public override IMap Parent
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
		public override int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public override ArrayList Array		//TODO: cache the Array somewhere; put in an "Add" method
		{ 
			get
			{
				return strategy.Array;
			}
		}
		public override object this[object key] 
		{
			get
			{
				object result;
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
						this.strategy=((Map)value).strategy.Clone();
					}
					else
					{
						object val;
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
		public object Call(object argument)
		{
			this.Argument=argument;
			Expression function=(Expression)((Map)this[CodeKeys.Run]).GetExpression();
			object result;
			result=function.Evaluate(this);
			return result;
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
			Map clone=strategy.CloneMap();
			clone.Parent=Parent;
			//clone.expression=expression;
			clone.Extent=Extent;
			return clone;
		}
		public Expression GetExpression() // TODO: move to Expression
		{
			// expression Statements are not cached, only expressions

			// no caching anymore, because of possible issues when reverse-debugging
//			if(expression==null) 
//			{
			Expression expression;
				if(this.ContainsKey(CodeKeys.Call))
				{
					expression=new Call((Map)this[CodeKeys.Call]);
				}
				else if(this.ContainsKey(CodeKeys.Delayed))
				{ 
					expression=new Delayed((Map)this[CodeKeys.Delayed]);
				}
				else if(this.ContainsKey(CodeKeys.Program))
				{
					expression=new Program((Map)this[CodeKeys.Program]);
				}
				else if(this.ContainsKey(CodeKeys.Literal))
				{
					expression=new Literal((Map)this[CodeKeys.Literal]);
				}
				else if(this.ContainsKey(CodeKeys.Search))
				{
					expression=new Search((Map)this[CodeKeys.Search]);
				}
				else if(this.ContainsKey(CodeKeys.Select))
				{
					expression=new Select((Map)this[CodeKeys.Select]);
				}
				else
				{
					throw new ApplicationException("Cannot compile non-code map.");
				}
//			}
				((Expression)expression).Extent=this.Extent;
			return expression;
		}
		public override bool ContainsKey(object key) 
		{
			if(key is Map)
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
		public override IEnumerator GetEnumerator()
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
//		public Expression expression; // why have this at all, why not for statements? probably a question of performance.
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
				// TODO: make unicode-safe
				for(int i=1;i<=text.Length;i++)
				{ 
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
						if(key is Integer && this.strategy[key] is Integer)
						{
							try
							{
								text+=System.Convert.ToChar(((Integer)this.strategy[key]).Int);
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
		private IMap map; 
		public MapEnumerator(IMap map)
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
		// TODO: remove??
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
					source+="Meta.Convert.ToDotNet(result,typeof("+returnType+"));"; 
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
		public DotNetObject(object target):base(target,target.GetType())
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
				object result;
				if(key is Map && ((Map)key).IsString && targetType.GetMember(((Map)key).String,bindingFlags).Length>0)
				{
					string text=((Map)key).String;
					MemberInfo[] members=targetType.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						result=new DotNetMethod(text,target,targetType);
					}
					else if(members[0] is FieldInfo)
					{
						result=Convert.ToMeta(targetType.GetField(text).GetValue(target));
					}
					else if(members[0] is PropertyInfo)
					{
						result=Convert.ToMeta(targetType.GetProperty(text).GetValue(target,new object[]{}));
					}
					else if(members[0] is EventInfo)
					{
						Delegate eventDelegate=(Delegate)targetType.GetField(text,BindingFlags.Public|
							BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(target);
						if(eventDelegate==null)
						{
							result=null; // TODO: maybe throw?
						}
						else
						{
							result=new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
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
				else if(this.target!=null && key is Integer && this.targetType.IsArray)
				{
					result=Convert.ToMeta(((Array)target).GetValue(((Integer)key).Int)); // TODO: add error handling here
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("get_Item",target,targetType);
					Map argument=new Map();
					argument[new Integer(1)]=key;
					try
					{
						result=indexer.Call(argument);
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
				if(key is Map && ((Map)key).IsString && targetType.GetMember(((Map)key).String,bindingFlags).Length!=0)
				{
					string text=((Map)key).String;
					MemberInfo[] members=targetType.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						throw new ApplicationException("Cannot set MethodeBase "+key+"."); // TODO: change exception
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
						else
						{
							if(value is Map)
							{
								// TODO: do not reuse isConverted
								val=DotNetMethod.AssignCollection((Map)value,field.GetValue(target),out isConverted); // TODO: really do this? does not make much sense
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
						bool isConverted;// TODO: rename
						object val=DotNetMethod.ConvertParameter(value,property.PropertyType,out isConverted);
						if(isConverted)
						{
							property.SetValue(target,val,new object[]{});
						}
						else
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
						((EventInfo)members[0]).AddEventHandler(target,CreateEvent(text,(Map)value));
					}
					else
					{
						throw new ApplicationException("Could not assign "+text+" .");
					}
				}
				else if(target!=null && key is Integer && targetType.IsArray)
				{
					bool isConverted; 
					object converted=Convert.ToDotNet(value,targetType.GetElementType(),out isConverted);
					if(isConverted)
					{
						((Array)target).SetValue(converted,((Integer)key).Int);
						return;
					}
				}
				else
				{
					DotNetMethod indexer=new DotNetMethod("set_Item",target,targetType); // TODO: refactor
					Map argument=new Map();
					argument[new Integer(1)]=key;
					argument[new Integer(2)]=value;
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
				foreach(Type type in targetType.GetNestedTypes(bindingFlags))
				{ 
					table[new Map(type.Name)]=new DotNetClass(type);
				}
				int counter=1;
				if(target!=null && target is IEnumerable && !(target is String))
				{ 
					// TODO: is this useful?
					foreach(object entry in (IEnumerable)target)
					{
						if(entry is DictionaryEntry)
						{
							table[Convert.ToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
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
			if(target==null)
			{
				this.bindingFlags=BindingFlags.Public|BindingFlags.Static;
			}
			else
			{
				this.bindingFlags=BindingFlags.Public|BindingFlags.Instance;
			}
			this.target=target;
			this.targetType=targetType;
		}
		private BindingFlags bindingFlags;
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
	public class Utility // TODO: rename
	{
		public static void WriteFile(string fileName,string text)
		{
			StreamWriter writer=new StreamWriter(fileName); // TODO: move to utility
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
			string Serialize(string indent,string[] functions); // TODO: refactor
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
			private bool CompareResult(string path,object toSerialize,string[] functions)
			{
				System.IO.Directory.CreateDirectory(path);
				if(!File.Exists(Path.Combine(path,"check.txt")))
				{
					File.Create(Path.Combine(path,"check.txt")).Close(); // TODO: move to Utility
				}
				StringBuilder stringBuilder=new StringBuilder();
				Serialize(toSerialize,"",functions,stringBuilder);

				string result=stringBuilder.ToString();

				Utility.WriteFile(Path.Combine(path,"result.txt"),result);
				Utility.WriteFile(Path.Combine(path,"resultCopy.txt"),result);
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