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

using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Meta;
using Meta.Parser;

using System;
using Meta.TestingFramework;
using antlr;
using System.Threading;

namespace Test 
{
	class Test 
	{
		public static string path="";
//		private static void Run(string file)
//		{
//			if(args.Length==0) 
//			{
//				Directory.SetCurrentDirectory(
//					".."+Path.DirectorySeparatorChar+"Test"+Path.DirectorySeparatorChar);
//				ExecuteTests test=new ExecuteTests(typeof(Tests),path);
//			}
//			else 
//			{
//				if(!File.Exists(args[0])) // add parameter for debugging purposes
//					// make this thingy a Map/parsing machine
//				{
//					throw new ApplicationException("File "+args[0]+" not found.");
//				}
//
//				object result=Interpreter.Run(args[0],new NormalMap());
//				if(result is IMap && ((Map)result).IsString)
//				{
//					Console.Write("Content-Type: text/html\n\n");
//					Console.Write(((Map)result).String);
//				}
//			}
//		}
		private static void Run(string file)
		{
//			IMap map=new NormalMap(new Integer(5));
//			int asdf=0;
			if(file=="") 
			{
				Directory.SetCurrentDirectory(
					".."+Path.DirectorySeparatorChar+"Test"+Path.DirectorySeparatorChar);
				ExecuteTests test=new ExecuteTests(typeof(Tests),path);
			}
			else 
			{
				if(!File.Exists(file)) // add parameter for debugging purposes
					// make this thingy a Map/parsing machine
				{
					throw new ApplicationException("File "+file+" not found.");
				}

				object result=Interpreter.Run(file,new NormalMap());
				if(result is IMap && ((IMap)result).IsString)
				{
					Console.Write("Content-Type: text/html\n\n");
					Console.Write(((IMap)result).String);
				}
			}
		}
		[STAThread]
		public static void Main(string[] args) 
		{
//			args=new string[] {@"C:\_ProjectSupportMaterial\Meta\Editor\editor.meta"};
//			args[0]=@"C:\_ProjectSupportMaterial\Meta\Editor\editor.meta";
			//			args[0]=new string[]{@"C:\_ProjectSupportMaterial\Editor\editor.meta"};
			//args=new string[]{@"C:\_ProectSupportMaterial\Meta\library\function.meta"};
			//args=new string[]{@"C:\Dokumente und Einstellungen\Christian\Desktop\editor.meta"};
			//args=new string[]{@"..\..\basicTest.meta"};

			Hashtable options=new Hashtable();
			string fileName="";
			for(int i=0;i<args.Length;i++)
			{
				if(args[i].StartsWith("-"))
				{
					string data="";
					string key=args[i].TrimStart('-');
					if(i+1<args.Length)
					{
						string next=args[i+1];
						if(!next.StartsWith("-"))
						{
							data=next;
						}
						i++;
					}
					options[key]=data;
				}
				else
				{
					if(i==args.Length-1)
					{
						fileName=args[i];
					}
				}
			}
			if(options.ContainsKey("debug"))
			{
				Run(fileName);
			}
			else
			{
				try 
				{
					Run(fileName);
				}
				catch(CharStreamException e) 
				{// put this into "Run" ???, no don't, every caller can do this differently
					Console.WriteLine(e.Message); //put all this error printing into one method
					Console.ReadLine();
				}
				catch(RecognitionException e) 
				{
					Console.WriteLine(e.Message+" line:"+e.line+"+ column:"+e.column);
					Console.ReadLine();
				}
				catch(TokenStreamRecognitionException e) 
				{
					Console.WriteLine(e.recog.Message+" line:"+e.recog.line+" column:"+e.recog.column);
					Console.ReadLine();
				}
				catch(TokenStreamException e) 
				{
					Console.WriteLine(e.Message);
					Console.ReadLine();
				}
				catch(Exception e) 
				{
					string text="";
					do 
					{
						text+=e.Message+"\n"+e.TargetSite+"\n";
						e=e.InnerException;
					} 
					while(e!=null);
					Console.WriteLine(text);
					Console.ReadLine();
				}
			}
		}
	}
	public class Tests 
	{
		private static string filename=@"basicTest.meta";
		// TODO:make it possible to choose between different tests on command line, and whether to test at all
		[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
			public class ParseToAst:TestCase 
		{
			public override object Run() 
			{
				return Interpreter.ParseToAst(Path.Combine(
					Test.path,filename));
			}
		}
		public class CompileToMap:TestCase 
		{
			public static IMap map;
			public override object Run() 
			{
				map=Interpreter.Compile(Path.Combine(
					Test.path,filename));
				return map;
			}
		}
		public class CompileToExpression:TestCase 
		{
			public override object Run() 
			{
				return Interpreter.Compile(Path.Combine(
					Test.path,filename)).GetExpression();
			}
		}
		// should execute twice, once without caching, once with
		public class ExecuteNoCaching:TestCase // TODO: combine with above, only result must be the same, a third file is needed, only one check file!!! ???
		{
			public override object Run()
			{
				string cachePath=@"C:\_ProjectSupportMaterial\Meta\cachedAssemblyInfo.meta";
				if(File.Exists(cachePath))
				{
					File.Delete(cachePath);
				}
				IMap argument=new NormalMap();
				argument[new NormalMap(1)]=new NormalMap("first arg");
				//argument[new Integer(1)]="first arg";
				argument[new NormalMap(2)]=new NormalMap("second=arg");
				//argument[new Integer(2)]="second=arg";
				return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			}
		}	
		// should execute twice, once without caching, once with
		public class Execute:TestCase 
		{
			public override object Run() 
			{
				IMap argument=new NormalMap();
				argument[new NormalMap(1)]=new NormalMap("first arg");
				argument[new NormalMap(2)]=new NormalMap("second=arg");
				return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			}
		}


	}
}