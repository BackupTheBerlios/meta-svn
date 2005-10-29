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
using System.Text;
using System.Threading;
using Microsoft.CSharp;
using System.Windows.Forms;
using System.Globalization;
using Meta.TestingFramework;
using System.Text.RegularExpressions;
using System.Net;

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
		public static readonly Map Search="search";
		public static readonly Map Key="key";
		public static readonly Map Program="program";
		public static readonly Map Lookup="lookup";
		public static readonly Map Value="value";
	}
	public class SpecialKeys
	{
		public static readonly Map Parent="parent";
		public static readonly Map Parameter="parameter";
		public static readonly Map Current="current";
	}
	public class DotNetKeys
	{
		public static readonly Map Add="add";
		public static readonly Map Remove="remove";
		public static readonly Map Get="get";
		public static readonly Map Set="set";
	}
	public class NumberKeys
	{
		public static readonly Map Denominator="denominator";
		public static readonly Map Numerator="numerator";
		public static readonly Map EmptyMap=new NormalMap();
	}


	public class MetaException:ApplicationException
	{
		public MetaException(string message,Extent extent):base(message)
		{
			this.extent=extent;
		}
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
	public class BreakPoint
	{
		public BreakPoint(string fileName,SourcePosition position)
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
		public SourcePosition Position
		{
			get
			{
				return position;
			}
		}
		private SourcePosition position;
		string fileName;
	}
	public class Argument
	{
		public static void ContainsKey(Map map,Map key)
		{
			if(!map.ContainsKey(key))
			{
				throw new ApplicationException("Functions expects keyword argument "+Serialize.Value(key));
			}
		}
		public static void Integer(Map arg)
		{
			if(!arg.IsInteger)
			{
				throw new ApplicationException("arg is not an integer");
			}
		}
		public static void IntegerArray(Map arg)
		{
			foreach(Map map in arg.Array)
			{
				if(!map.IsInteger)
				{
					throw new ApplicationException("not all array elements in argument are integers");
				}
			}
		}
		public static void ExactArrayCount(Map parameter,int count)
		{
			if(parameter.Array.Count!=count)
			{
				throw new ApplicationException("did not pass array of length "+count.ToString()+" to function");
			}
		}
		public static void MinimalArrayCount(Map arg,int count)
		{
			if(arg.Array.Count<count)
			{
				throw new ApplicationException("count is too small");
			}
		}
		public static void Boolean(Map arg)
		{
			if(!arg.IsBoolean)
			{
				throw new ApplicationException("argument is not boolean");
			}
		}
		public static void BooleanArray(Map arg)
		{
			foreach(Map map in arg.Array)
			{
				if(!map.IsBoolean)
				{
					throw new ApplicationException("one of the argument array elements is not boolean");
				}
			}
		}

	}
	public class Process
	{
		// rename
		private string path;
		private Map parameter;
		public Process(string path):this(path,new NormalMap())
		{
		}
		public Process(string path,Map parameter)
		{
			// start a new thread here
			this.path=path;
			this.parameter=parameter;
			processes[Thread.CurrentThread]=this;
			TextReader reader=new StreamReader(path,Encoding.Default);
			Map program=Compile(reader);
			reader.Close();
		}
		private Map program;
		public Process(string path,Map program,Map parameter)
		{
			this.program=program;
			this.parameter=parameter;
			this.path=path;
		}
		public Map Run()
		{
//			TextReader reader=new StreamReader(path,Encoding.Default);
//			Map program=Compile(reader);
//			reader.Close();
//			program=CallProgram(program,new NormalMap(),null);
			program.Parent=GetPersistantMaps(path);
			return program.Call(parameter);
		}
		public Map RunWithoutLibrary()
		{
			using(TextReader reader=new StreamReader(path,Encoding.Default))
			{
				return RunWithoutLibrary(path,reader);
			}
		}
		// should be removed
		public Map RunWithoutLibrary(string fileName,TextReader textReader)
		{
			Map program=Compile(textReader);
			return CallProgram(program,new NormalMap(),null);
		}
		// refactor
		public Map CallProgram(Map program,Map argument,Map current)
		{
			Map callable=new NormalMap();
			callable[CodeKeys.Function]=program;
			callable.Parent=current;
			return callable.Call(argument);
		}
		public Map Compile()
		{
			using(TextReader reader=new StreamReader(this.path,Encoding.Default))
			{
				return Compile(reader);
			}
		}
		public Map Compile(TextReader textReader)
		{
			return new MetaCustomParser(textReader.ReadToEnd(),this.path).Program();
		}
		public void Start()
		{
		}
		public void Stop()
		{
		}
		public void Pause()
		{
		}
		public void Resume()
		{
		}
		public static Process Current
		{
			get
			{
				return (Process)processes[Thread.CurrentThread];
			}
		}
		private static Hashtable processes=new Hashtable();
		private bool reversed=false;

		public BreakPoint BreakPoint
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
		private BreakPoint breakPoint=new BreakPoint("",new SourcePosition(0,0));
		public Map Evaluate(Map code,Map context)
		{
			Map val;
			if(code.ContainsKey(CodeKeys.Call))
			{
				val=Call(code[CodeKeys.Call],context);
			}
			else if(code.ContainsKey(CodeKeys.Program))
			{
				val=Program(code[CodeKeys.Program],context);
			}
			else if(code.ContainsKey(CodeKeys.Literal))
			{
				val=Literal(code[CodeKeys.Literal],context);
			}
			else if(code.ContainsKey(CodeKeys.Select))
			{
				val=Select(code[CodeKeys.Select],context);
			}
			else
			{
				throw new ApplicationException("Cannot compile map.");
			}
			return val;
		}
		public Map Call(Map code,Map context)
		{
			Map function=Evaluate(code[CodeKeys.Callable],context);
			Map argument=Evaluate(code[CodeKeys.Argument],context);
			return function.Call(argument);
		}
		public Map Program(Map code,Map context)
		{
			Map local=new NormalMap();
			Program(code,context,ref local);
			return local;
		}
		private bool Reversed
		{
			get
			{
				return reversed;
			}
		}
		private bool ResumeAfterReverse(Map code)
		{
			return code.Extent.End.smaller(BreakPoint.Position);
		}
		private void Program(Map code,Map context,ref Map local)
		{
			local.Parent=context;
			for(int i=0;i<code.Array.Count && i>=0;i++)
			{
				if(Reversed)
				{
					if(!ResumeAfterReverse((Map)code.Array[i]))
					{
						// ugly hack
						i-=2;
						continue;
					}
					else
					{
						reversed=false;
					}
				}
				Statement((Map)code.Array[i],ref local);
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
		public void Statement(Map code,ref Map context)
		{
			Map selected=context;
			Map key;
			for(int i=0;i<code[CodeKeys.Key].Array.Count-1;i++)
			{
				key=Evaluate((Map)code[CodeKeys.Key].Array[i],context);
				if(key.Equals(new NormalMap("TestClass")))
				{
					int asdf=0;
				}
				Map selection=selected[key];
				if(selection==null)
				{
					object x=selected[key];
					Throw.KeyDoesNotExist(key,((Map)code[CodeKeys.Key].Array[i]).Extent);
				}
				selected=selection;
				if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code[CodeKeys.Key].Array[i]).Extent))
				{
					CallBreak(selected);
				}
			}
			Map lastKey=Evaluate((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1],context);
			if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1]).Extent))
			{
				Map oldValue;
				if(selected.ContainsKey(lastKey))
				{
					oldValue=selected[lastKey];
				}
				else
				{
					oldValue=null;//new NormalMap("<null>");
				}
				CallBreak(oldValue);
			}
			
			Map val=Evaluate(code[CodeKeys.Value],context);

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
		public ArrayList recognitions=new ArrayList();
		public Map Literal(Map code,Map context)
		{
			return code;
		}
		public Map Select(Map code,Map context)
		{
			Map selected=FindFirstKey(code,context);
			for(int i=1;i<code.Array.Count;i++)
			{
				Map key=Evaluate((Map)code.Array[i],context);
				Map selection=selected[key];
				if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code.Array[i]).Extent))
				{
					CallBreak(selection);
				}
				if(selection==null)
				{
					object test=selected[key];
					Throw.KeyDoesNotExist(key,key.Extent);
				}
				selected=selection;
			}
			return selected;
		}
		private Map FindFirstKey(Map code,Map context)
		{
			Map key=Evaluate((Map)code.Array[0],context);
			if(key.Equals(new NormalMap("TestClass")))
			{
				int asdf=0;
			}
			Map selected=context;
			while(!selected.ContainsKey(key))
			{
				selected=selected.Parent;
				if(selected==null)
				{
					Throw.KeyNotFound(key,key.Extent);
				}
			}
			Map val=selected[key];
			if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code.Array[0]).Extent))
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
				Break(data);
				Thread.CurrentThread.Suspend();
			}
		}
		public Map GetPersistantMaps(string fileName)
		{
			DirectoryInfo directory=new DirectoryInfo(Path.GetDirectoryName(fileName));
			Map root=new PersistantMap(directory);
			Map current=root;
			while(true)
			{
				if(String.Compare(directory.FullName,Process.LibraryPath.FullName,true)==0)
				{
					current.Parent=GACMap.singleton;
					break;
				}
				current.Parent=new PersistantMap(directory.Parent);
				current=current.Parent;
			}
			return root;
		}
//		public Map Run(string fileName)
//		{
//			return Run(fileName,new NormalMap());
//		}
//		public Map Run(string fileName,Map argument)
//		{
//			Map program=Compile(fileName);
//			program=CallProgram(program,new NormalMap(),null);
//			program.Parent=GetPersistantMaps(fileName);
//			return program.Call(argument);
//		}
//		public Map RunWithoutLibrary(string fileName,TextReader textReader)
//		{
//			Map program=Compile(fileName, textReader);
//			return CallProgram(program,new NormalMap(),null);
//		}
//		public Map RunWithoutLibrary(string fileName)
//		{
//			return RunWithoutLibrary(fileName,new StringReader(Helper.ReadFile(fileName)));
//		}
//		public Map CallProgram(Map program,Map argument,Map current)
//		{
//			Map callable=new NormalMap();
//			callable[CodeKeys.Function]=program;
//			callable.Parent=current;
//			return callable.Call(argument);
//		}
//		// kinda pointless
//		public Map Compile(string fileName)
//		{
//			return Compile(fileName,new StringReader(Helper.ReadFile(fileName)));
//		}
//		public Map Compile(string fileName,TextReader textReader)
//		{
//			return new MetaCustomParser(textReader.ReadToEnd(),fileName).Program();
//		}
		public static DirectoryInfo LibraryPath
		{
			get
			{
				// TODO: localize this, infer this from the exe path, maybe
				return new DirectoryInfo(@"c:\Projects\meta\library");
			}
		}
		// should this be static?
		public static ArrayList loadedAssemblies=new ArrayList();
	}
	// TODO: rename remove
	public class Interpreter
	{
		public static Map Or(Map arg) 
		{
			Argument.BooleanArray(arg);
			bool or=false;
			foreach(Map map in arg.Array)
			{
				if(map.GetBoolean())
				{
					or=true;
					break;
				}
			}
			return or;
		}
		public static Map Add(Map arg)
		{
			Argument.IntegerArray(arg);
			Integer sum=0;
			foreach(Map map in arg.Array)
			{
				sum+=map.GetInteger();
			}
			return sum;
		}
		public static Map Multiply(Map arg) 
		{
			Argument.IntegerArray(arg);
			Integer product=1;
			foreach(Map map in arg.Array)
			{
				product*=map.GetInteger();
			}
			return product;
		}
		public static Map Greater(Map parameter)
		{
			Argument.IntegerArray(parameter);
			Argument.ExactArrayCount(parameter,2);
			return parameter[1].GetInteger()>parameter[2].GetInteger();
		}
		public static Map Smaller(Map parameter)
		{
			Argument.IntegerArray(parameter);
			Argument.ExactArrayCount(parameter,2);
			return parameter[1].GetInteger()<parameter[2].GetInteger();
		}
		public static Map BitwiseOr(Map arg)
		{
			Argument.IntegerArray(arg);
			Integer or=0;
			foreach(Map map in arg.Array)
			{
				or|=map.GetInteger();
			}
			return or;
		}
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
		public static Map Apply(Map arg)
		{
			// TODO: ensure "function" is callable, maybe?
			Argument.ContainsKey(arg,"function");
			Argument.ContainsKey(arg,"array");
			Map application=new NormalMap();
			int counter=1;
			foreach(Map element in arg["array"].Array)
			{
				application[counter]=arg["function"].Call(element);
				counter++;
			}
			return application;
		}
		public static Map And(Map arg) 
		{
			Argument.BooleanArray(arg);
			bool and=true;
			foreach(Map map in arg.Array)
			{
				if(!map.GetBoolean())
				{
					and=false;
					break;
				}
			}
			return and;
		}
		public static Map Equal(Map arg) 
		{
			bool equal=true;
			for(int i=0;i+1<arg.Array.Count;i++)
			{
				if(!arg.Array[i].Equals(arg.Array[i+1]))
				{
					equal=false;
					break;
				}
			}
			return equal;
		}
		// refactor
		public static Map Merge(Map map)
		{
			return MergeCollection(map.Array);
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

	}
//	public class Interpreter
//	{
//		// this should start everything at once
//		// we should have another class called a process that does everything i think
//		private Interpreter()
//		{
//		}
//		public void Start()
//		{
//		}
//		public void Stop()
//		{
//		}
//		public void Pause()
//		{
//		}
//		public void Resume()
//		{
//		}
//		// rename this to process, maybe, or so
//		public static Interpreter CreateInterpreter()
//		{
//			Interpreter interpreter=new Interpreter();
//			processes[Thread.CurrentThread]=interpreter;
//			return interpreter;
//		}
//		public static Interpreter Current
//		{
//			get
//			{
//				return (Interpreter)processes[Thread.CurrentThread];
//			}
//		}
//		private static Hashtable processes=new Hashtable();
//		private bool reverse=false;
//
//		public BreakPoint BreakPoint
//		{
//			get
//			{
//				return breakPoint;
//			}
//			set
//			{
//				breakPoint=value;
//			}
//		}
//		private BreakPoint breakPoint=new BreakPoint("",new SourcePosition(0,0));
//		public Map Evaluate(Map code,Map context)
//		{
//			Map val;
//			if(code.ContainsKey(CodeKeys.Call))
//			{
//				val=Call(code[CodeKeys.Call],context);
//			}
//			else if(code.ContainsKey(CodeKeys.Program))
//			{
//				val=Program(code[CodeKeys.Program],context);
//			}
//			else if(code.ContainsKey(CodeKeys.Literal))
//			{
//				val=Literal(code[CodeKeys.Literal],context);
//			}
//			else if(code.ContainsKey(CodeKeys.Select))
//			{
//				val=Select(code[CodeKeys.Select],context);
//			}
//			else
//			{
//				throw new ApplicationException("Cannot compile map.");
//			}
//			return val;
//		}
//		public Map Call(Map code,Map context)
//		{
//			Map function=Evaluate(code[CodeKeys.Callable],context);
//			Map argument=Evaluate(code[CodeKeys.Argument],context);
//			return function.Call(argument);
//		}
//		public Map Program(Map code,Map context)
//		{
//			Map local=new NormalMap();
//			Program(code,context,ref local);
//			return local;
//		}
//		private bool Reverse
//		{
//			get
//			{
//				return reverse;
//			}
//		}
//		private bool ResumeAfterReverse(Map code)
//		{
//			return code.Extent.End.smaller(BreakPoint.Position);
//		}
//		private void Program(Map code,Map context,ref Map local)
//		{
//			local.Parent=context;
//			for(int i=0;i<code.Array.Count && i>=0;i++)
//			{
//				if(Reverse)
//				{
//					if(!ResumeAfterReverse((Map)code.Array[i]))
//					{
//						i-=2;
//						continue;
//					}
//					else
//					{
//						reverse=false;
//					}
//				}
//				Statement((Map)code.Array[i],ref local);
//			}
//		}
//		public class Change
//		{
//			private Map map;
//			private Map key;
//			private Map oldValue;
//			public Change(Map map,Map key,Map oldValue)
//			{
//				this.map=map;
//				this.key=key;
//				this.oldValue=oldValue;
//			}
//			public void Undo(ref Map current)
//			{
//				if(key.Equals(SpecialKeys.Current))
//				{
//					current=oldValue;
//				}
//				else
//				{
//					this.map[key]=oldValue;
//				}
//			}
//		}
//		public void Statement(Map code,ref Map context)
//		{
//			Map selected=context;
//			Map key;
//			for(int i=0;i<code[CodeKeys.Key].Array.Count-1;i++)
//			{
//				key=Evaluate((Map)code[CodeKeys.Key].Array[i],context);
//				Map selection=selected[key];
//				if(selection==null)
//				{
//					object x=selected[key];
//					Throw.KeyDoesNotExist(key,((Map)code[CodeKeys.Key].Array[i]).Extent);
//				}
//				selected=selection;
//				if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code[CodeKeys.Key].Array[i]).Extent))
//				{
//					CallBreak(selected);
//				}
//			}
//			Map lastKey=Evaluate((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1],context);
//			if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code[CodeKeys.Key].Array[code[CodeKeys.Key].Array.Count-1]).Extent))
//			{
//				Map oldValue;
//				if(selected.ContainsKey(lastKey))
//				{
//					oldValue=selected[lastKey];
//				}
//				else
//				{
//					oldValue=null;//new NormalMap("<null>");
//				}
//				CallBreak(oldValue);
//			}
//			
//			Map val=Evaluate(code[CodeKeys.Value],context);
//
//			if(lastKey.Equals(SpecialKeys.Current))
//			{
//				val.Parent=context.Parent;
//				context=val;
//			}
//			else
//			{
//				selected[lastKey]=val;
//			}
//		}
//		public ArrayList recognitions=new ArrayList();
//		public Map Literal(Map code,Map context)
//		{
//			return code;
//		}
//
//		public Map Select(Map code,Map context)
//		{
//			Map selected=FindFirstKey(code,context);
//			for(int i=1;i<code.Array.Count;i++)
//			{
//				Map key=Evaluate((Map)code.Array[i],context);
//				Map selection=selected[key];
//				if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code.Array[i]).Extent))
//				{
//					CallBreak(selection);
//				}
//				if(selection==null)
//				{
//					object test=selected[key];
//					Throw.KeyDoesNotExist(key,key.Extent);
//				}
//				selected=selection;
//			}
//			return selected;
//		}
//		private Map FindFirstKey(Map code,Map context)
//		{
//			Map key=Evaluate((Map)code.Array[0],context);
//			Map selected=context;
//			while(!selected.ContainsKey(key))
//			{
//				selected=selected.Parent;
//				if(selected==null)
//				{
//					Throw.KeyNotFound(key,key.Extent);
//				}
//			}
//			Map val=selected[key];
//			if(BreakPoint!=null && BreakPoint.Position.IsBetween(((Map)code.Array[0]).Extent))
//			{
//				CallBreak(val);
//			}
//			return val;
//		}
//
//		public event DebugBreak Break;
//
//		public delegate void DebugBreak(Map data);
//		public void CallBreak(Map data)
//		{
//			if(Break!=null)
//			{
//				Break(data);
//				Thread.CurrentThread.Suspend();
//			}
//		}
//
//
//		public static Map Or(Map arg) 
//		{
//			Argument.BooleanArray(arg);
//			bool or=false;
//			foreach(Map map in arg.Array)
//			{
//				if(map.GetBoolean())
//				{
//					or=true;
//					break;
//				}
//			}
//			return or;
//		}
//		public static Map Add(Map arg)
//		{
//			Argument.IntegerArray(arg);
//			Integer sum=0;
//			foreach(Map map in arg.Array)
//			{
//				sum+=map.GetInteger();
//			}
//			return sum;
//		}
//		public static Map Multiply(Map arg) 
//		{
//			Argument.IntegerArray(arg);
//			Integer product=1;
//			foreach(Map map in arg.Array)
//			{
//				product*=map.GetInteger();
//			}
//			return product;
//		}
//		public static Map Greater(Map parameter)
//		{
//			Argument.IntegerArray(parameter);
//			Argument.ExactArrayCount(parameter,2);
//			return parameter[1].GetInteger()>parameter[2].GetInteger();
//		}
//		public static Map Smaller(Map parameter)
//		{
//			Argument.IntegerArray(parameter);
//			Argument.ExactArrayCount(parameter,2);
//			return parameter[1].GetInteger()<parameter[2].GetInteger();
//		}
//
//		public static Map BitwiseOr(Map arg)
//		{
//			Argument.IntegerArray(arg);
//			Integer or=0;
//			foreach(Map map in arg.Array)
//			{
//				or|=map.GetInteger();
//			}
//			return or;
//		}
//		public static Map Join(Map arg) 
//		{
//			Integer i=1;
//			Map array=new NormalMap();
//			foreach(Map map in arg.Array) 
//			{ 
//				foreach(Map val in map.Array) 
//				{
//					array[i]=val;
//					i+=1;
//				}
//			}
//			return array;
//		}
//		public static Map Apply(Map arg)
//		{
//			// TODO: ensure "function" is callable, maybe?
//			Argument.ContainsKey(arg,"function");
//			Argument.ContainsKey(arg,"array");
//			Map application=new NormalMap();
//			int counter=1;
//			foreach(Map element in arg["array"].Array)
//			{
//				application[counter]=arg["function"].Call(element);
//				counter++;
//			}
//			return application;
//		}
//		public static Map And(Map arg) 
//		{
//			Argument.BooleanArray(arg);
//			bool and=true;
//			foreach(Map map in arg.Array)
//			{
//				if(!map.GetBoolean())
//				{
//					and=false;
//					break;
//				}
//			}
//			return and;
//		}
//		public static Map Equal(Map arg) 
//		{
//			bool equal=true;
//			for(int i=0;i+1<arg.Array.Count;i++)
//			{
//				if(!arg.Array[i].Equals(arg.Array[i+1]))
//				{
//					equal=false;
//					break;
//				}
//			}
//			return equal;
//		}
//		// refactor
//		public static Map Merge(Map map)
//		{
//			return MergeCollection(map.Array);
//		}
//		public static Map Merge(params Map[] arkvlToMerge)
//		{
//			return MergeCollection(arkvlToMerge);
//		}
//		public static Map MergeCollection(ICollection collection)
//		{
//			Map result=new NormalMap();
//			foreach(Map current in collection)
//			{
//				foreach(DictionaryEntry entry in current)
//				{
//					result[(Map)entry.Key]=(Map)entry.Value;
//				}
//			}
//			return result;
//		}
//		public Map Run(string fileName)
//		{
//			return Run(fileName,new NormalMap());
//		}
//		public Map Run(string fileName,Map argument)
//		{
//			Map program=Compile(fileName);
//			program=CallProgram(program,new NormalMap(),null);
//			program.Parent=GetPersistantMaps(fileName);
//			return program.Call(argument);
//		}
//		public Map GetPersistantMaps(string fileName)
//		{
//			DirectoryInfo directory=new DirectoryInfo(Path.GetDirectoryName(fileName));
//			Map root=new PersistantMap(directory);
//			Map current=root;
//			while(true)
//			{
//				if(String.Compare(directory.FullName,Interpreter.LibraryPath.FullName,true)==0)
//				{
//					current.Parent=GACMap.singleton;
//					break;
//				}
//				current.Parent=new PersistantMap(directory.Parent);
//				current=current.Parent;
//			}
//			return root;
//		}
//		public Map RunWithoutLibrary(string fileName,TextReader textReader)
//		{
//			Map program=Compile(fileName, textReader);
//			return CallProgram(program,new NormalMap(),null);
//		}
//		public Map RunWithoutLibrary(string fileName)
//		{
//			return RunWithoutLibrary(fileName,new StringReader(Helper.ReadFile(fileName)));
//		}
//		public Map CallProgram(Map program,Map argument,Map current)
//		{
//			Map callable=new NormalMap();
//			callable[CodeKeys.Function]=program;
//			callable.Parent=current;
//			return callable.Call(argument);
//		}
//		// kinda pointless
//		public Map Compile(string fileName)
//		{
//			return Compile(fileName,new StringReader(Helper.ReadFile(fileName)));
//		}
//		public Map Compile(string fileName,TextReader textReader)
//		{
//			return new MetaCustomParser(textReader.ReadToEnd(),fileName).Program();
//		}
//		public static DirectoryInfo LibraryPath
//		{
//			get
//			{
//				// TODO: localize this, infer this from the exe path, maybe
//				return new DirectoryInfo(@"c:\Projects\meta\library");
//			}
//		}
//		// should this be static?
//		public static ArrayList loadedAssemblies=new ArrayList();
//	}

	public abstract class Map: IEnumerable
	{	
		// TODO: not really accurate
		public bool IsFunction
		{
			get
			{
				return ContainsKey(CodeKeys.Function);
			}
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
		public virtual bool IsFraction
		{
			get
			{
				return this.ContainsKey(NumberKeys.Numerator) && this[NumberKeys.Numerator].IsInteger && this.ContainsKey(NumberKeys.Denominator) && this[NumberKeys.Denominator].IsInteger;
			}
		}
		public virtual double GetFraction()
		{
			double fraction;
			if(IsFraction)
			{
				fraction=((double)(this["numerator"]).GetInteger().GetInt64())/((double)(this["denominator"]).GetInteger().GetInt64());
			}
			else
			{
				throw new ApplicationException("Map is not a fraction");
			}
			return fraction;
		}
		public virtual bool IsInteger
		{
			get
			{
				// TODO: this is incorrect, GetInteger should throw if it isnt an integer
				return GetInteger()!=null;
			}
		}
		public abstract Integer GetInteger();
		public Map Parameter
		{
			get
			{
				return parameter;
			}
			set
			{ 
				parameter=value;
			}
		}
		Map parameter=null;
		public virtual bool IsString
		{
			get
			{
				return GetString()!=null;
			}
		}
		public static string GetString(Map map)
		{
			string text="";
			foreach(Map key in map.Keys)
			{
				if(key.GetInteger()!=null && map[key].GetInteger()!=null)
				{
					try
					{
						text+=Convert.ToChar(map[key].GetInteger().GetInt32());
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
		public virtual string GetString()
		{
			return GetString(this);
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
		public virtual Map Call(Map parameter)
		{
			this.Parameter=parameter;
			Map function=this[CodeKeys.Function];
			Map result;
			result=Process.Current.Evaluate(function,this);
			return result;
		}

		public abstract ArrayList Keys
		{
			get;
		}
		public abstract Map Clone();
		public bool ContainsKey(Map key)
		{
			bool containsKey;
			if(key.Equals(SpecialKeys.Parameter))
			{
				containsKey=this.Parameter!=null;
			}
			else if(key.Equals(SpecialKeys.Parent))
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
		[Serialize(2)]
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
	}

	public abstract class StrategyMap: Map, ISerializeSpecial
	{
		// TODO: no deep cloning necessary??
		public void InitFromStrategy(MapStrategy clone) 
		{
			foreach(Map key in clone.Keys)
			{
				this[key]=clone[key];
			}
		}
		public override Integer GetInteger()
		{
			return strategy.Integer;
		}
		public override string GetString()
		{
			return strategy.String;
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
				Map val;
				if(key.Equals(SpecialKeys.Parent))
				{
					val=Parent;
				}
				else if(key.Equals(SpecialKeys.Parameter))
				{
					val=Parameter;
				}
				else if(key.Equals(SpecialKeys.Current))
				{
					val=this;
				}
				else
				{
					val=strategy[key];
				}
				return val;
			}
			set
			{
				if(value!=null)
				{
					isHashCached=false;
					if(key.Equals(SpecialKeys.Current))
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

		public void Serialize(string indentation,string[] functions,StringBuilder stringBuilder,int level)
		{
			strategy.Serialize(indentation,functions,stringBuilder,level);
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
		public Map ClassesFromAssemblies(IEnumerable assemblies)
		{
			Map root=new NormalMap();
			foreach(Assembly assembly in assemblies)
			{
				ArrayList assemblyNamespaces=new ArrayList();
				foreach(Type type in assembly.GetExportedTypes())
				{
					if(type.DeclaringType==null)
					{
						root[type.Name]=new DotNetClass(type);
					}
				}
			}
			return root;
		}
		protected Map cachedAssemblyInfo=new NormalMap();
		protected Map cache=new NormalMap();
	}
	public class GACMap: AssemblyStrategy
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
				Map val;
				try
				{
					// some caching might be in order here
					val=ClassesFromAssemblies(new Assembly[] {Assembly.LoadWithPartialName(key.GetString())});
				}
				catch
				{
					val=null;
				}
				return val;
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
				ArrayList assemblies=GAC.AssembliesNames;
				foreach(string dllPath in Directory.GetFiles(Process.LibraryPath.FullName,"*.dll"))
				{
					assemblies.Add(new NormalMap(Path.GetFileNameWithoutExtension(dllPath)));
				}
				foreach(string exePath in Directory.GetFiles(Process.LibraryPath.FullName,"*.exe"))
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
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
		static GACMap()
		{
			GACMap gac=new GACMap();
			gac.cache["web"]=Web.singleton;
			singleton=new PersistantMap(gac);
		}
		public static Map singleton;
	}

	public class RemoteStrategy:NormalStrategy
	{
		public override ArrayList Array
		{
			get
			{
				throw new ApplicationException("not implemented.");
			}
		}
		public override Integer Integer
		{
			get
			{
				return null;
			}
		}
		public override ArrayList Keys
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
				if(!key.IsString)
				{
					throw new ApplicationException("key is not a string");
				}
				WebClient webClient=new WebClient();
				Uri fullPath=new Uri(new Uri("http://"+address),key.GetString()+".meta");
				Stream stream=webClient.OpenRead(fullPath.ToString());
				StreamReader streamReader=new StreamReader(stream);
				return Process.Current.RunWithoutLibrary(fullPath.ToString(),streamReader);
			}
			set
			{
				throw new ApplicationException("Cannot set key in remote map.");
			}
		}
		private string address;
		public RemoteStrategy(string address)
		{
			this.address=address;
		}
	}
	public class Web:Map
	{
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
				// TODO: use the Argument checking stuff here too, or something similar
				if(!key.IsString)
				{
					throw new ApplicationException("need a string here");
				}
				// TODO: maybe check the host name here
				return new NormalMap(new RemoteStrategy(key.GetString()));
			}
			set
			{
				throw new ApplicationException("Cannot set key in Web.");
			}
		}
		public override Integer GetInteger()
		{
			return null;
		}

		public override Map Clone()
		{
			return this;
		}

		private Web()
		{
		}
		public static Web singleton=new Web();
	}
	public class DirectoryStrategy:AssemblyStrategy
	{
		public DirectoryStrategy(DirectoryInfo directory)
		{
			this.directory=directory;
			assemblyPath=Path.Combine(directory.FullName,"assembly");
//			ArrayList assemblies=new ArrayList();
//			if(Directory.Exists(assemblyPath))
//			{
//				foreach(string dllPath in Directory.GetFiles(assemblyPath,"*.dll"))
//				{
//					assemblies.Add(Assembly.LoadFrom(dllPath));
//				}
//				foreach(string exePath in Directory.GetFiles(assemblyPath,"*.exe"))
//				{
//					assemblies.Add(Assembly.LoadFrom(exePath));
//				}
//			}
//			cache=ClassesFromAssemblies(assemblies);
		}
		private string assemblyPath;
		public override ArrayList Array
		{
			get
			{
				return new ArrayList();
			}
		}
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
				if(key.Equals(new NormalMap("TestClass")))
				{
					int asdf=0;
				}
				Map val;
				if(key.IsString && ValidName(key.GetString()))
				{
					string path=Path.Combine(directory.FullName,key.GetString());
					FileInfo file=new FileInfo(path+".meta");
					DirectoryInfo subDirectory=new DirectoryInfo(path);
					if(file.Exists)
					{
						val=new PersistantMap(file);
						val.Parent=map;
					}
					else if(subDirectory.Exists)
					{
						val=new PersistantMap(subDirectory);
						val.Parent=map;
					}
					else if(cache.ContainsKey(key))
					{
						// TODO: assign parent here, too
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
				if(key.IsString && ValidName(key.GetString()))
				{
					SaveToFile(value,Path.Combine(directory.FullName,key.GetString()+".meta"));
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
			string text=Meta.Serialize.MapValue(meta,"").Trim(new char[]{'\n'});
			// TODO: use constants here
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
			return number.GetInteger().ToString();
		}
		private static string IntegerValue(Map number)
		{
			return literalStartDelimiter+number.ToString()+literalEndDelimiter;
		}
		private static string StringKey(Map key,string indentation)
		{
			string text;
			if(IsLiteralKey(key.GetString()))
			{
				text=key.GetString();
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
			if(val.IsString)
			{
				int longest=0;
				foreach(Match match in Regex.Matches(val.GetString(),"(>)?(\\\\)*"))
				{
					if(match.ToString().Length>longest)
					{
						longest=match.Length;
					}
				}
				string escape="";
				for(int i=0;i<longest;i++)
				{
					escape+='\\';
				}
				text=escape+literalStartDelimiter+val.GetString()+literalEndDelimiter+escape;
			}
			else
			{
				text=MapValue(val,indentation);
			}
			return text;
		}
		private static string MapKey(Map map,string indentation)
		{
			return indentation + leftBracket + newLine + MapValue(map,indentation) + rightBracket;
		}
		public static string MapValue(Map map,string indentation)
		{
			string text;
			text=newLine;
			foreach(DictionaryEntry entry in map)
			{
				text+=indentation + Key((Map)entry.Key,indentation)	+ assignment + Value((Map)entry.Value,indentation+'\t');
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
		private const string literalStartDelimiter="\"";
		private const string assignment="=";
		private const string literalEndDelimiter="\"";

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
				return GetMap().GetInteger();
			}
		}
		private Map GetMap()
		{
			Map data;
			using(TextReader reader=new StreamReader(this.file.FullName,Encoding.Default))
			{
				data=Process.Current.RunWithoutLibrary(this.file.FullName,reader);
				data.Parent=this.map;
				return data;
			}
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

//	public class NamespaceStrategy: NormalStrategy
//	{
//		public override MapStrategy Clone()
//		{
//			return new NamespaceStrategy(FullName,cache,namespaces,assemblies);
//		}
//
//		public override Integer Integer
//		{
//			get
//			{
//				return null;
//			}
//		}
//		public override ArrayList Array
//		{
//			get
//			{
//				return new ArrayList();
//			}
//		}
//		private ListDictionary Cache
//		{
//			get
//			{
//				if(cache==null)
//				{
//					Load();
//				}
//				return cache;
//			}
//		}
//		public override bool ContainsKey(Map key)
//		{
//			return Cache.Contains(key);
//		}
//		public override Map this[Map key]
//		{
//			get
//			{
//				return (Map)Cache[key];
//			}
//			set
//			{
//				// TODO: make an overload for initalization with map, maybe
//				map.strategy=new HybridDictionaryStrategy();
//				map.strategy.map=map;
//				map.InitFromStrategy(this);
//				map.strategy[key]=value;
//			}
//		}
//		public override ArrayList Keys
//		{
//			get
//			{
//				return new ArrayList(Cache.Keys);
//			}
//		}
//		public override int Count
//		{
//			get
//			{
//				return Cache.Count;
//			}
//		}
//		public string FullName
//		{
//			get
//			{
//				return fullName;
//			}
//		}
//		private string fullName;
//		private ArrayList assemblies;
//		private Hashtable namespaces;
//		private ListDictionary cache;
//
//		public ArrayList Assemblies
//		{
//			get
//			{
//				return assemblies;
//			}
//		}
//		public Hashtable Namespaces
//		{
//			get
//			{
//				return namespaces;
//			}
//		}
//		public NamespaceStrategy(string fullName)
//		{
//			this.fullName=fullName;
//			this.assemblies=new ArrayList();
//			this.namespaces=new Hashtable();
//		}
//		public NamespaceStrategy(string fullName,ListDictionary cache,Hashtable namespaces,ArrayList assemblies)
//		{
//			this.fullName=fullName;
//			this.cache=cache;
//			this.assemblies=assemblies;
//			this.namespaces=namespaces;
//		}
//		public void Load()
//		{
//			cache=new ListDictionary();
//			foreach(Assembly assembly in assemblies)
//			{
//				foreach(Type type in assembly.GetExportedTypes())
//				{
//					if(type.DeclaringType==null && this.FullName==type.Namespace) 
//					{
//						cache[new NormalMap(type.Name)]=new DotNetClass(type);
//					}
//				}
//				Process.loadedAssemblies.Add(assembly.Location);
//			}
//			foreach(DictionaryEntry entry in namespaces)
//			{
//				cache[new NormalMap((string)entry.Key)]=(Map)entry.Value;
//			}
//		}
//	}
	public class Transform
	{

		public static object ToDotNet(Map meta,Type target)
		{
			bool isConverted;
			return ToDotNet(meta,target,out isConverted);
		}
		public static object ToDotNet(Map meta,Type target,out bool isConverted)
		{
			object dotNet=null;
			if((target.IsSubclassOf(typeof(Delegate))
				||target.Equals(typeof(Delegate)))&& meta.IsFunction)
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
						isConverted=false;
						break;
					case TypeCode.DBNull:
						if(meta.IsInteger && meta.GetInteger()==0)
						{
							dotNet=DBNull.Value;
						}
						break;
					case TypeCode.Decimal:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble((double)decimal.MinValue),Helper.IntegerFromDouble((double)decimal.MaxValue)))
						{
							dotNet=(decimal)(meta.GetInteger().GetInt64());
						}
						else if(IsFractionInRange(meta,(double)decimal.MinValue,(double)decimal.MaxValue))
						{
							dotNet=(decimal)meta.GetFraction();
						}
						break;
					case TypeCode.Double:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble(double.MinValue),Helper.IntegerFromDouble(double.MaxValue)))
						{
							dotNet=(double)(meta.GetInteger().GetInt64());
						}
						else if(IsFractionInRange(meta,double.MinValue,double.MaxValue))
						{
							dotNet=meta.GetFraction();
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
						if(IsIntegerInRange(meta,(Integer)SByte.MinValue,(Integer)SByte.MaxValue))
						{
							dotNet=Convert.ToSByte(meta.GetInteger().GetInt64());
						}
						break;
					case TypeCode.Single:
						if(IsIntegerInRange(meta,Helper.IntegerFromDouble(Single.MinValue),Helper.IntegerFromDouble(Single.MaxValue)))
						{
							dotNet=(float)meta.GetInteger().GetInt64();
						}
						else if(IsFractionInRange(meta,Single.MinValue,Single.MaxValue))
						{
							dotNet=(float)meta.GetFraction();
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
			return meta.IsInteger && meta.GetInteger()>=minValue && meta.GetInteger()<=maxValue;
		}
		private static bool IsFractionInRange(Map meta,double minValue,double maxValue)
		{
			return meta.IsFraction && meta.GetFraction()>=minValue && meta.GetFraction()<=maxValue;
		}
		public static Map ToMap(ArrayList list)
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
	public class MetaLibrary
	{
	}
	public class DotNetMethod: Map
	{
		public override Integer GetInteger()
		{
			// TODO: throw exception, make this the default implementation?, no better not
			return null;
		}
		public override Map Clone()
		{
			return new DotNetMethod(this.name,this.obj,this.type);
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
		// TODO: properly support sorting of multiple argument methods
		public class ArgumentComparer: IComparer
		{
			public int Compare(object x, object y)
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
			object result=null;
			bool isExecuted=false;

			if(type.IsSubclassOf(typeof(MetaLibrary)))
			{
				ArrayList oneArgumentMethods=new ArrayList();
				foreach(MethodBase method in overloadedMethods)
				{
					if(method.GetParameters().Length==1)
					{ 
						oneArgumentMethods.Add(method);
					}
				}
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
							try
							{
								result=method.Invoke(obj,new object[] {parameter});
							}
							catch(TargetInvocationException e)
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
					throw new ApplicationException("Method "+this.type.Name+"."+this.name+": No methods with the right number of arguments.");
				}
				rightNumberArgumentMethods.Sort(new ArgumentComparer());
				foreach(MethodBase method in rightNumberArgumentMethods)
				{
					ArrayList arguments=new ArrayList();
					bool argumentsMatched=true;
					ParameterInfo[] parameters=method.GetParameters();
					for(int i=0;argumentsMatched && i<parameters.Length;i++)
					{
						arguments.Add(Transform.ToDotNet((Map)argument.Array[i],parameters[i].ParameterType,out argumentsMatched));
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
			assemblyNames.AddRange(Process.loadedAssemblies);
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
		private void Initialize(string name,object obj,Type type)
		{
			this.name=name;
			this.obj=obj;
			this.type=type;
			ArrayList methods;
			if(name==".ctor")
			{
				methods=new ArrayList(type.GetConstructors());
			}
			else
			{
				methods=new ArrayList(type.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
			}
			overloadedMethods=(MethodBase[])methods.ToArray(typeof(MethodBase));
		}
		public DotNetMethod(string name,object obj,Type type)
		{
			this.Initialize(name,obj,type);
		}
		public DotNetMethod(Type type)
		{
			this.Initialize(".ctor",null,type);
		}
		public override bool Equals(object toCompare)
		{
			if(toCompare is DotNetMethod)
			{
				DotNetMethod DotNetMethod=(DotNetMethod)toCompare;
				if(DotNetMethod.obj==obj && DotNetMethod.name.Equals(name) && DotNetMethod.type.Equals(type))
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
		protected object obj;
		protected Type type;

		public MethodBase[] overloadedMethods;
	}
	// TODO: maybe rename to DotNetType, or TypeMap?, DotNet is a bit stupid
	public class DotNetClass: DotNetContainer
	{
		public Type Type
		{
			get
			{
				return type;
			}
		}
		public override Integer GetInteger()
		{
			// TODO: Throw exception
			return null;
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
		public override Integer GetInteger()
		{
			// TODO: throw exception
			return null;
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
		public virtual void Serialize(string indentation,string[] functions,StringBuilder stringBuilder,int level)
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
					ExecuteTests.Serialize(entry,indentation+ExecuteTests.indentationText,functions,stringBuilder,level);
				}
			}
		}
//		public virtual Map Call(Map argument)
//		{
//			map.Parameter=argument;
//			Map function=this[CodeKeys.Function];
//			Map result;
//			result=Process.Current.Evaluate(function,map);
//			return result;
//		}
		public StrategyMap map;
		public virtual MapStrategy Clone()
		{
			return null;
		}
		public virtual Map CloneMap() // TODO: move into Map??
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
				ArrayList keys=new ArrayList();
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
		public override Map this[Map key]
		{
			get
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
				return key.GetInteger()>0 && key.GetInteger()<=(Integer)this.Count;
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
				else if(this.Count==1 && this.ContainsKey(NumberKeys.EmptyMap))
//				else if((this.Count==1 || (this.Count==2 && this.ContainsKey(NumberKeys.Negative) && this[NumberKeys.Negative]==new NormalMap(new Integer(1)))) && this.ContainsKey(NumberKeys.EmptyMap))
				{
					if(this[NumberKeys.EmptyMap].GetInteger()!=null)
					{
						number=this[NumberKeys.EmptyMap].GetInteger()+1;
//						if(this[NumberKeys.Negative]==new NormalMap(new Integer(1)))
//						{
//							number=-number;
//						}
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
				for(Integer iInteger=new Integer(1);ContainsKey(new NormalMap(iInteger));iInteger+=1)
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
								text+=Convert.ToChar(val.GetInteger().GetInt32());
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
		// TODO: refactor
		public override Map Call(Map argument)
		{
			Map result;
			try
			{
				Delegate eventDelegate=(Delegate)type.GetField(eventInfo.Name,BindingFlags.Public|
					BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
				if(eventDelegate!=null)
				{
					DotNetMethod invoke=new DotNetMethod("Invoke",eventDelegate,eventDelegate.GetType());
					// TODO: use GetRaiseMethod???
					result=invoke.Call(argument);
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

		public override Map Clone()
		{
			return new Event(eventInfo,obj,type);
		}
		public override Integer GetInteger()
		{
			// TODO: throw exception
			return null;
		}
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				if(eventInfo.GetAddMethod()!=null)
				{
					keys.Add(DotNetKeys.Add);
				}

				return keys;
			}
		}
		public override Map this[Map key]
		{
			get
			{
				Map val;
				if(key.Equals(DotNetKeys.Add))
				{
					val=new DotNetMethod(eventInfo.GetAddMethod().Name,obj,type);
				}
				else
				{
					val=null;
				}
				return val;
			}
			set
			{
				throw new ApplicationException("Cannot assign in event "+eventInfo.Name+".");
			}
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
		public override Map Clone()
		{
			return new Property(property,obj,type);
		}
		public override Integer GetInteger()
		{
			// TODO: throw exception
			return null;
		}
		public override ArrayList Keys
		{
			get
			{

				ArrayList keys=new ArrayList();
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
		public override Map this[Map key]
		{
			get
			{
				if(this.property.Name=="Item")
				{
					int asdf=0;
				}	
				Map val;
				if(key.Equals(DotNetKeys.Get))
				{
					val=new DotNetMethod(property.GetGetMethod().Name,obj,type);
				}
				else if(key.Equals(DotNetKeys.Set))
				{
					val=new DotNetMethod(property.GetSetMethod().Name,obj,type);
				}
				else
				{
					// TODO: add extent in the code that called this stuff
					// dont know how to do this yet
					// TODO: maybe add info about where this was looked up, must be a map
					// maybe in caller too though
//					throw new KeyDoesNotExistException(key);
					val=null;
				}
				return val;
			}
			set
			{
				if(this.property.Name=="Item")
				{
					int asdf=0;
				}	
				throw new ApplicationException("Cannot assign in property "+property.Name+".");
			}
		}
	}
	public abstract class DotNetContainer: Map, ISerializeSpecial
	{
		public void Serialize(string indentation, string[] functions, StringBuilder stringBuilder,int level)
		{
			ExecuteTests.Serialize(obj!=null?this.obj:this.type,indentation,functions,stringBuilder,level);
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
				string text=key.GetString();
				if(type.GetMember(key.GetString(),
					BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0)
				{
					return true;
				}
			}
			return false;
		}
		public override ArrayList Keys
		{
			get
			{
				ArrayList keys=new ArrayList();
				foreach(MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					keys.Add(new NormalMap(member.Name));
				}
				return keys;
			}
		}
		public override Map this[Map key] 
		{
			get
			{
				Map val;
				if(key.Equals(SpecialKeys.Parent))
				{
					val=Parent;
				}
				else if(key.IsString && type.GetMember(key.GetString(),bindingFlags).Length>0)
				{
					string text=key.GetString();
					MemberInfo[] members=type.GetMember(text,bindingFlags);
					if(members[0] is MethodBase)
					{
						val=new DotNetMethod(text,obj,type);
					}
					else if(members[0] is FieldInfo)
					{
						val=Transform.ToMeta(type.GetField(text).GetValue(obj));
					}
					else if(members[0] is PropertyInfo)
					{
						val=new Property(type.GetProperty(text),this.obj,type);// TODO: set parent here, too
					}
					else if(members[0] is EventInfo)
					{
						val=new Event(((EventInfo)members[0]),obj,type);
						val.Parent=this;
					}
					else if(members[0] is Type)
					{
						val=new DotNetClass((Type)members[0]);
					}
					else
					{
						val=null;
					}
				}
				else if(this.obj!=null && key.IsInteger && this.type.IsArray)
				{
					val=Transform.ToMeta(((Array)obj).GetValue(key.GetInteger().GetInt32()));
				}
				else
				{
					val=null;
				}
				// TODO: what if value is null?
				return val;
			}
			set
			{
				if(key.IsString && type.GetMember(key.GetString(),bindingFlags).Length!=0)
				{
					string text=key.GetString();
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
							throw new ApplicationException("Field "+field.Name+" could not be assigned because the value cannot be converted.");
						}
					}
					else if(member is PropertyInfo)
					{
						throw new ApplicationException("Cannot set property "+member.Name+" directly. Use its set method instead.");
					}
					else if(member is EventInfo)
					{
						throw new ApplicationException("Cannot set event "+member.Name+" directly. Use its add method instead.");
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
						((Array)obj).SetValue(converted,key.GetInteger().GetInt32());
						return;
					}
				}
				else
				{
					throw new ApplicationException("Cannot set "+Meta.Serialize.Key(key)+".");
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
//				if(number<0)
//				{
//					keys.Add(NumberKeys.Negative);
//				}
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
						result=null;
					}
					else
					{
						result=number-1;
//						result=number.abs()-1;
					}
				}
//				else if(key.Equals(NumberKeys.Negative))
//				{
//					if(number<0)
//					{
//						result=new NormalMap(new Integer(1));
//					}
//					else
//					{
//						result=null;
//					}
//				}
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
					Panic(key,value);
				}
//				else if(key.Equals(NumberKeys.Negative))
//				{
//					if(value==null)
//					{
//						number=number.abs();
//					}
//					else if(value.Equals(new NormalMap(new Integer(1))))
//					{
//						number=-number.abs();
//					}
//					else
//					{
//						Panic(key,value);
//					}
//				}
				else
				{
					Panic(key,value); // TODO: dont do assignment in panic, do it here
				}
			}
		}
		// TODO: rename, refactor, make general function, extract assignment
		private void Panic(Map key,Map val)
		{
			map.strategy=new HybridDictionaryStrategy();
			map.strategy.map=map;
			map.InitFromStrategy(this);
			map.strategy[key]=val;
		}
	}
	// TODO: rename
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
			integer*=(Integer)Convert.ToInt32(val);
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
			StreamWriter writer=new StreamWriter(fileName,false,Encoding.Default);
			writer.Write(text);
			writer.Close();
		}
		public static string ReadFile(string fileName)
		{
			StreamReader reader=new StreamReader(fileName,Encoding.Default);
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
		// TODO: refactor special serialization
		public interface ISerializeSpecial
		{
			void Serialize(string indent,string[] functions,StringBuilder builder,int level);
		}
		public abstract class TestCase
		{
			public abstract object Run(ref int level);
		}
		public class ExecuteTests
		{	
			public const string indentationText="\t";
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
					int level=1;
					object result=((TestCase)testType.GetConstructors()[0].Invoke(new object[]{})).Run(ref level);
					TimeSpan timespan=DateTime.Now-start;
					bool wasSuccessful=CompareResult(Path.Combine(fnResults,testType.Name),result,methodNames,level);
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
			// TODO: remove functions!!
			private bool CompareResult(string path,object toSerialize,string[] functions,int level)
			{				
				System.IO.Directory.CreateDirectory(path);
				if(!File.Exists(Path.Combine(path,"check.txt")))
				{
					File.Create(Path.Combine(path,"check.txt")).Close();
				}
				StringBuilder stringBuilder=new StringBuilder();
				Serialize(toSerialize,"",functions,stringBuilder,level);

				string result=stringBuilder.ToString();

				Helper.WriteFile(Path.Combine(path,"result.txt"),result);
				Helper.WriteFile(Path.Combine(path,"resultCopy.txt"),result);
				string check=Helper.ReadFile(Path.Combine(path,"check.txt"));
				return result.Equals(check);
			}
			public static void Serialize(object toSerialize,string indent,string[] methods,StringBuilder stringBuilder,int level) 
			{
				if(toSerialize==null) 
				{
					stringBuilder.Append(indent+"null\n");
				}
				else if(toSerialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) 
				{
					stringBuilder.Append(indent+"\""+toSerialize.ToString()+"\""+"\n");
				}
				else
				{
					ArrayList members=new ArrayList();
					members.AddRange(toSerialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
					members.AddRange(toSerialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
					foreach(string method in methods)
					{
						MethodInfo methodInfo=toSerialize.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
						if(methodInfo!=null)
						{ 
							members.Add(methodInfo);
						}
					}
					members.Sort(new MemberInfoComparer());
					foreach(MemberInfo member in members) 
					{
						if(member.Name!="Item" && member.Name!="SyncRoot") 
						{
							if(Assembly.GetAssembly(member.DeclaringType)!=Assembly.GetExecutingAssembly() || member is MethodInfo || (member.GetCustomAttributes(typeof(SerializeAttribute),false).Length==1 && ((SerializeAttribute)member.GetCustomAttributes(typeof(SerializeAttribute),false)[0]).Level>=level)) 
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
									Serialize(val,indent+indentationText,methods,stringBuilder,level);
								}
							}
						}
					}
					if(toSerialize is ISerializeSpecial)
					{
						((ISerializeSpecial)toSerialize).Serialize(indent,methods,stringBuilder,level);
					}
					else if(toSerialize is IEnumerable)
					{
						foreach(object entry in (IEnumerable)toSerialize)
						{
							stringBuilder.Append(indent+"Entry ("+entry.GetType().Name+")\n");
							Serialize(entry,indent+indentationText,methods,stringBuilder,level);
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
		public class SerializeAttribute:Attribute
		{
			public SerializeAttribute():this(1)
			{
			}
			private int level;
			public SerializeAttribute(int level)
			{
				this.level=level;
			}
			public int Level
			{
				get
				{
					return level;
				}
			}
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
	public class SourcePosition
	{
		public bool smaller(SourcePosition other)
		{
			return this.Line<other.Line || (this.Line==other.Line && this.Column<other.Column);
		}
		public bool greater(SourcePosition other)
		{
			return this.Line>other.Line || (this.Line==other.Line && this.Column>other.Column);
		}
		public bool IsBetween(Extent extent)
		{
			return false;
//			return IsBetween(extent.Start,extent.End);
		}
//		public bool IsBetween(Extent extent)
//		{
//			return IsBetween(extent.Start,extent.End);
//		}
		public bool IsBetween(SourcePosition start,SourcePosition end)
		{
			return false;
//			return Line>=start.Line && Line<=end.Line && Column>=start.Column && Column<=end.Column;
		}
//		public bool IsBetween(SourcePosition start,SourcePosition end)
//		{
//			return Line>=start.Line && Line<=end.Line && Column>=start.Column && Column<=end.Column;
//		}
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
//		public override string ToString()
//		{
//			return "line "+Start.Line+" column "+Start.Column;
//		}

		public static ArrayList GetExtents(string fileName,int firstLine,int lastLine)
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
		[Serialize]
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
			this.start=new SourcePosition(startLine,startColumn);
			this.end=new SourcePosition(endLine,endColumn);
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
	public class MetaCustomParser
	{
		private string text;
		private int index;
		private string filePath;
		public MetaCustomParser(string text,string filePath)
		{
			if(filePath.EndsWith("testLib.meta"))
			{
				int asdf=0;
			}
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
				index+=characters.Length;
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
		private bool TryConsume(char character)
		{
			bool consumed;
			if(index<text.Length && text[index]==character)
			{
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
		public const char commentChar='#';

		private bool Comment()
		{
			bool isComment;
			if(TryConsume(commentChar))
			{
				isComment=true;
				// TODO: make this a try consume for the newline, simply use newline, maybe should refactor everything
				while(Look()!='\n' && Look()!=endOfFileChar)
//					while(Look()!='\n' && Look()!=endOfFileChar)
					{
					Consume();
				}
			}
			else
			{
				isComment=false;
			}
			return isComment;
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
		private int Line
		{
			get
			{
				return text.Substring(0,index).Split('\n').Length;
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
		// TODO: what is CodeKeys.Function good for?
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
						if(!Comment())
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
//							TryConsume('\n'); // this should not be eaten
							while(Comment())
							{
								NewLine();
//								TryConsume('\n');
							}
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
						else
						{
							NewLine();
//							TryConsume('\n');
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
				extent.FileName=this.filePath;
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
				while(true)
				{
					if(LookAny(integerChars)) // should be one function
					{
						integerString+=ConsumeGet();
					}
					else
					{
						break;
					}
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
		// maybe combine literals
		public const char stringEscapeChar='\\';
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
//					if(!LookExcept(new char[] {stringChar}))
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
//				@string=new NormalMap(stringText);
				ArrayList lines=new ArrayList();
				// TODO: make these constants
				stringText=stringText.Replace("\r\n","\n");
				string[] originalLines=stringText.Split('\n');
				string realText;
//				string realText=(string)originalLines[0]+'\n';
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
//				string realText=(string)originalLines[0]+'\n';
//				foreach(string line in originalLines.GetRange(1,originalLines.Count-1))
//				{
//					lines.Add(line.Remove(0,Math.Min(indentationCount+1,line.Length-line.TrimStart(indentationChar).Length)));
//				}
				realText=string.Join("\n",(string[])lines.ToArray(typeof(string)));
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
		// TODO: use the other constants to create this array
		public char[] lookupStringForbiddenChars=new char[] {' ','\t','\r','\n','=','.','\\','|','#','"','[',']','*'};
		public char[] lookupStringFirstForbiddenChars=new char[] {' ','\t','\r','\n','=','.','\\','|','#','"','[',']','*','1','2','3','4','5','6','7','8','9'};
		private Map LookupString()
		{
			string lookupString="";
			Extent extent=StartExpression();
			if(LookExcept(lookupStringFirstForbiddenChars))
			{
				while(LookExcept(lookupStringForbiddenChars))
				{
					lookupString+=Look();
					Consume(Look());
				}
			}
			else
			{
				int asdf=0;
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
			ArrayList list=new ArrayList(exceptions);
			list.Add(endOfFileChar);
			return Look().ToString().IndexOfAny((char[])list.ToArray(typeof(char)))==-1;
		}
		private Map LookupAnything()
		{
			Map lookupAnything;
			if(TryConsume(lookupStartChar)) // separate into TryConsume and Consume, only try, and throw
			{
				lookupAnything=Expression();
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
			Map lookup=LookupString();
			if(lookup==null)
			{
				lookup=LookupAnything();
			}
			return lookup;
		}
		const char callChar=' ';
		const char selectChar='.';
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
		const char statementChar='=';
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

	public class Integer
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
				//				if(text[0]=='-')
				//				{
				//					index++;
				//				}
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
				//				if(text[0]=='-')
				//				{
				//					result=-result;
				//				}
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
		// maybe remove, or rename to TryParseInteger
		public Integer(string value, int radix)
		{
			Integer multiplier = new Integer(1);
			Integer result = new Integer();
			value = (value.ToUpper()).Trim();
			int limit = 0;

			if(value[0] == '-')
				limit = 1;

			for(int i = value.Length - 1; i >= limit ; i--)
			{
				int posVal = (int)value[i];

				if(posVal >= '0' && posVal <= '9')
					posVal -= '0';
				else if(posVal >= 'A' && posVal <= 'Z')
					posVal = (posVal - 'A') + 10;
				else
					posVal = 9999999;       // arbitrary large


				if(posVal >= radix)
					throw(new ArithmeticException("Invalid string in constructor."));
				else
				{
					if(value[0] == '-')
						posVal = -posVal;

					result = result + (multiplier * posVal);

					if((i - 1) >= limit)
						multiplier = multiplier * radix;
				}
			}

			if(value[0] == '-')     // negative values
			{
				if((result.data[maxLength-1] & 0x80000000) == 0)
					throw(new ArithmeticException("Negative underflow in constructor."));
			}
			else    // positive values
			{
				if((result.data[maxLength-1] & 0x80000000) != 0)
					throw(new ArithmeticException("Positive overflow in constructor."));
			}

			data = new uint[maxLength];
			for(int i = 0; i < result.dataLength; i++)
				data[i] = result.data[i];

			dataLength = result.dataLength;
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

// TODO: get a copyright statement

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
	public class GAC
	{// TODO: put this into the real GAC-class, make this a private class
		public static ArrayList AssembliesNames
		{
			get
			{
				ArrayList assemblies=new ArrayList();
				assemblies.Add(new NormalMap("mscorlib"));

				IAssemblyEnum assemblyEnum=CreateGACEnum();
				IAssemblyName iname; 
				while (GetNextAssembly(assemblyEnum, out iname) == 0)
				{
					try
					{
						string assemblyName=GetAssemblyName(iname);
						if(assemblyName!="Microsoft.mshtml")
						{
							assemblies.Add(new NormalMap(assemblyName));
//							assemblies.Add(Assembly.LoadWithPartialName(assemblyName));
						}
					}
					catch(Exception e)
					{
					}
				}
				return assemblies;
			}
		}
//		public static ArrayList Assemblies
//		{
//			get
//			{
//				ArrayList assemblies=new ArrayList();
//				assemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
//
//				IAssemblyEnum assemblyEnum=CreateGACEnum();
//				IAssemblyName iname; 
//				while (GetNextAssembly(assemblyEnum, out iname) == 0)
//				{
//					try
//					{
//						string assemblyName=GetAssemblyName(iname);
//						if(assemblyName!="Microsoft.mshtml")
//						{
//							assemblies.Add(Assembly.LoadWithPartialName(assemblyName));
//						}
//					}
//					catch(Exception e)
//					{
//					}
//				}
//				return assemblies;
//			}
//		}
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

			GAC.CreateAssemblyEnum(out ae, (IntPtr)0, null, ASM_CACHE_FLAGS.ASM_CACHE_GAC, (IntPtr)0);

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