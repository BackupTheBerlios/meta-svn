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
//				object result=Interpreter.Run(args[0],new Map());
//				if(result is Map && ((Map)result).IsString)
//				{
//					Console.Write("Content-Type: text/html\n\n");
//					Console.Write(((Map)result).String);
//				}
//			}
//		}
		private static void Run(string file)
		{
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

				object result=Interpreter.Run(file,new Map());
				if(result is Map && ((Map)result).IsString)
				{
					Console.Write("Content-Type: text/html\n\n");
					Console.Write(((Map)result).String);
				}
			}
		}
		[STAThread]
		public static void Main(string[] args) 
		{
			args[0]=@"C:\_ProjectSupportMaterial\Meta\Editor\editor.meta";
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
//		[STAThread]
//		public static void Main(string[] args) 
//		{
//			//			args=new string[]{@"C:\_ProjectSupportMaterial\Editor\editor.meta"};
//			//args=new string[]{@"C:\_ProectSupportMaterial\Meta\library\function.meta"};
//			//args=new string[]{@"C:\Dokumente und Einstellungen\Christian\Desktop\editor.meta"};
//			//args=new string[]{@"..\..\basicTest.meta"};
//			try 
//			{
//				if(args.Length==0) 
//				{
//					Directory.SetCurrentDirectory(
//						".."+Path.DirectorySeparatorChar+"Test"+Path.DirectorySeparatorChar);
//					ExecuteTests test=new ExecuteTests(typeof(Tests),path);
//				}
//				else 
//				{
//					if(!File.Exists(args[0])) // add parameter for debugging purposes
//						// make this thingy a Map/parsing machine
//					{
//						throw new ApplicationException("File "+args[0]+" not found.");
//					}
//
//					object result=Interpreter.Run(args[0],new Map());
//					if(result is Map && ((Map)result).IsString)
//					{
//						Console.Write("Content-Type: text/html\n\n");
//						Console.Write(((Map)result).String);
//					}
//				}
//			}
//			catch(CharStreamException e) 
//			{// put this into "Run" ???, no don't, every caller can do this differently
//				Console.WriteLine(e.Message); //put all this error printing into one method
//				Console.ReadLine();
//			}
//			catch(RecognitionException e) 
//			{
//				Console.WriteLine(e.Message+" line:"+e.line+"+ column:"+e.column);
//				Console.ReadLine();
//			}
//			catch(TokenStreamRecognitionException e) 
//			{
//				Console.WriteLine(e.recog.Message+" line:"+e.recog.line+" column:"+e.recog.column);
//				Console.ReadLine();
//			}
//			catch(TokenStreamException e) 
//			{
//				Console.WriteLine(e.Message);
//				Console.ReadLine();
//			}
//			catch(Exception e) 
//			{
//				string text="";
//				do 
//				{
//					text+=e.Message+"\n"+e.TargetSite+"\n";
//					e=e.InnerException;
//				} 
//				while(e!=null);
//				Console.WriteLine(text);
//				Console.ReadLine();
//			}
//			//			DateTime start=DateTime.Now;
//			//			string file=@"C:\Dokumente und Einstellungen\Christian\Desktop\performance.meta";
//			//			Interpreter.Run(new StreamReader(file),new Map());
//			//			DateTime end=DateTime.Now;
//			//			TimeSpan span=end-start;
//			//			Console.WriteLine(span.TotalSeconds);
//			//			Console.ReadLine();
//		}
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
			public static Map map;
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
		public class Execute:TestCase 
		{
			public override object Run() 
			{
				Map argument=new Map();
				argument[new Integer(1)]="first arg";
				argument[new Integer(2)]="second=arg";
				return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			}
		}
	}
}
//
//
//namespace testClasses
//{
//	public class MemberTest 
//	{
//		public static string classField="default";
//		public string instanceField="default";
//
//		public static string ClassProperty 
//		{
//			get 
//			{
//				return classField;
//			}
//			set 
//			{
//				classField=value;
//			}
//		}
//		public string InstanceProperty 
//		{
//			get 
//			{
//				return this.instanceField;
//			}
//			set 
//			{
//				this.instanceField=value;
//			}
//		}
//	}
//	public delegate int IntEvent (int intArg);
//	public delegate object NormalEvent (object sender);
//	public class TestClass 
//	{
//		public TestClass()
//		{
//		}
//		public event IntEvent instanceEvent;
//		public static event NormalEvent staticEvent;
//		protected string x="unchangedX";
//		protected string y="unchangedY";
//		protected string z="unchangedZ";
//
//		public static bool boolTest=false;
//
//		public static object TestClass_staticEvent(object sender) 
//		{
//			MethodBase[] m=typeof(TestClass).GetMethods();
//			return null;
//		}
//		public static Delegate del;
//		public static void TakeDelegate(Delegate d) 
//		{
//			del=d;
//		}
//		public static object GetResultFromDelegate() 
//		{
//			return del.GetType().GetMethod("Invoke").Invoke(del,new object[]{});
//		}
//		public double doubleValue=0.0;
//		public float floatValue=0.0F;
//		public decimal decimalValue=0.0M;
//	}
//	public class PositionalNoConversion:TestClass 
//	{
//		public PositionalNoConversion(string p1,string b,string p2) 
//		{
//			this.x=p1;
//			this.y=b;
//			this.z=p2;
//		}
//		public string Concatenate(string p1,string b,string c) 
//		{
//			return p1+b+c+this.x+this.y+this.z;
//		}
//	}
//	public class NamedNoConversion:TestClass 
//	{ //refactor
//		public NamedNoConversion(Map arg) 
//		{
//			Map def=new Map();
//			def[new Integer(1)]=new Map("null");
//			def[new Map("y")]=new Map("null");
//			def[new Map("p2")]=new Map("null");
//			arg=(Map)Interpreter.Merge(def,arg);
//			this.x=(string)((Map)arg[new Integer(1)]).String;
//			this.y=(string)((Map)arg[new Map("y")]).String;
//			this.z=(string)((Map)arg[new Map("p2")]).String;
//		}
//		public string Concatenate(Map arg) 
//		{
//			Map def=new Map();
//			def[new Integer(1)]=new Map("null");
//			def[new Map("b")]=new Map("null");
//			def[new Map("c")]=new Map("null");
//			arg=(Map)Interpreter.Merge(def,arg);
//			return (string)((Map)arg[new Integer(1)]).String+
//				(string)((Map)arg[new Map("b")]).String+
//				(string)((Map)arg[new Map("c")]).String+
//				this.x+this.y+this.z;
//		}
//	}
//	public class IndexerNoConversion:TestClass 
//	{
//		public string this[string a] 
//		{
//			get 
//			{
//				return this.x+this.y+this.z+a;
//			}
//			set 
//			{
//				this.x=a+value;
//			}
//		}
//	}
//}
