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

using System;
using Meta.TestingFramework;
using System.Threading;

namespace Test 
{
	class Test 
	{
		public static string path="";
		private static void Run(string file)
		{
			if(file=="") 
			{
				ExecuteTests test=new ExecuteTests(typeof(Tests),Path.Combine(Interpreter.LibraryPath.Parent.FullName,"Test"));
			}
			else 
			{
				if(!File.Exists(file))
				{
					throw new ApplicationException("File "+file+" not found.");
				}

				Map result=Interpreter.CreateInterpreter().Run(file,new NormalMap());
				if(result.IsString)
				{
					Console.Write("Content-Type: text/html\n\n");
					Console.Write(result.GetString());
				}
			}
		}
		[STAThread]
		public static void Main(string[] args) 
		{
//			args=new string[] {@"-debug",@"C:\Projects\Meta\Library\editor.meta"};


			Hashtable options=new Hashtable();
			string fileName="";
			for(int i=0;i<args.Length;i++)
			{
				if(args[i].StartsWith("-"))
				{
					string data="";
					string key=args[i].TrimStart('-');
					if(i+1<args.Length-1)
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
					fileName=args[i];
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
		private static string filename=@"C:\Projects\Meta\Library\basicTest.meta";
//		public class CompileToMap:TestCase 
//		{
//			public static Map map;
//			public override object Run() 
//			{
//				MetaCustomParser parser=new MetaCustomParser(Helper.ReadFile(filename),filename);
//				map=parser.Program();
//				return map;
//			}
//		}
//		public class CompileToMap:TestCase 
//		{
//			public static Map map;
//			public override object Run() 
//			{
//				map=Interpreter.Compile(Path.Combine(
//					Test.path,filename));
//				return map;
//			}
//		}

		public class Execute:TestCase
		{
			public override object Run()
			{
				Map argument=new NormalMap();
				argument[1]="first arg";
				argument[2]="second=arg";
				return Interpreter.CreateInterpreter().Run(@"C:\Projects\Meta\Library\basicTest.meta",argument);
			}
		}
//		public class Library:TestCase
//		{
//			public override object Run()
//			{
//				return Interpreter.CreateInterpreter().Run(@"C:\Projects\Meta\Library\libraryTest.meta",new NormalMap());
//			}
//		}
	}
}
namespace testClasses
{
	public class MemberTest 
	{
		public static string classField="default";
		public string instanceField="default";

		public static string ClassProperty 
		{
			get 
			{
				return classField;
			}
			set 
			{
				classField=value;
			}
		}
		public string InstanceProperty 
		{
			get 
			{
				return this.instanceField;
			}
			set 
			{
				this.instanceField=value;
			}
		}
	}
	public delegate int IntEvent (int intArg);
	public delegate object NormalEvent (object sender);
	public class TestClass 
	{
		public class NestedClass// TODO: rename, only used for testing purposes
		{
			public static int field=0;
		}
		public TestClass()
		{
		}
		public event IntEvent instanceEvent;
		public static event NormalEvent staticEvent;
		protected string x="unchangedX";
		protected string y="unchangedY";
		protected string z="unchangedZ";

		public static bool boolTest=false;

		public static object TestClass_staticEvent(object sender) 
		{
			MethodBase[] m=typeof(TestClass).GetMethods();
			return null;
		}
		public static Delegate del;
		public static void TakeDelegate(Delegate d) 
		{
			del=d;
		}
		public static object GetResultFromDelegate() 
		{
			return del.GetType().GetMethod("Invoke").Invoke(del,new object[]{});
		}
		public double doubleValue=0.0;
		public float floatValue=0.0F;
		public decimal decimalValue=0.0M;
	}
	public class PositionalNoConversion:TestClass 
	{
		public PositionalNoConversion(string p1,string b,string p2) 
		{
			this.x=p1;
			this.y=b;
			this.z=p2;
		}
		public string Concatenate(string p1,string b,string c) 
		{
			return p1+b+c+this.x+this.y+this.z;
		}
	}
	public class NamedNoConversion:TestClass 
	{ 
		public NamedNoConversion(Map arg) 
		{
			Map def=new NormalMap();
			def[1]="null";
			def["y"]="null";
			def["p2"]="null";
			arg=(Map)Interpreter.Merge(def,arg);

			this.x=arg[1].GetString();
			this.y=arg["y"].GetString();
			this.z=arg["p2"].GetString();
		}
		public string Concatenate(Map arg) 
		{
			Map def=new NormalMap();
			def[1]="null";
			def["b"]="null";
			def["c"]="null";
			arg=(Map)Interpreter.Merge(def,arg);
			return arg[1].GetString()+arg["b"].GetString()+arg["c"].GetString()+
				this.x+this.y+this.z;
		}
	}
	public class IndexerNoConversion:TestClass 
	{
		public string this[string a] 
		{
			get 
			{
				return this.x+this.y+this.z+a;
			}
			set 
			{
				this.x=a+value;
			}
		}
	}
}
