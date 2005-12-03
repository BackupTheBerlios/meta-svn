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


using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Meta;

using System;
using System.Threading;
using Microsoft.VisualStudio.DebuggerVisualizers;


namespace Test 
{
	public class MetaTest:TestRunner
	{
		[STAThread]
		public static void Main(string[] args) 
		{
			//DateTime start = DateTime.Now;
			//LocalStrategy.singleton.map["website"].Call(Map.Empty);//, Map.Empty);
			////LocalStrategy.singleton.map["test"].Call(Map.Empty);//, Map.Empty);
			//Console.WriteLine((DateTime.Now - start).TotalSeconds.ToString());
			//Console.ReadLine();
			new MetaTest().Run();
			//FileSystem.fileSystem["website"].Call(Map.Empty);
		}
		protected override string TestDirectory
		{
			get 
			{
				return Path.Combine(Directory.GetParent(Process.InstallationPath).FullName, "Test");
			}
		}
		//[Test]
		//public object Market()
		//{

		//}
		//[Test]
		//public object Parsing()
		//{
		//    Map test;
		//    using (TextReader reader = new StreamReader(LocalStrategy.Path))
		//    {
		//        test = Process.Compile(reader);
		//    }
		//    return test;
		//}
		public class Serialization : Test
		{
			public override object  GetResult(out int level)
			{
				level=1;
				Map map = FileSystem.fileSystem["basicTest"];
				return FileSystem.Serialize.Value(map).TrimStart();
			}
		}
		public class Basic : Test
		{
			public override object GetResult(out int level)
			{
				level = 2;
				Map argument = new StrategyMap();
				argument[1] = "first arg";
				argument[2] = "second=arg";
				return FileSystem.fileSystem["basicTest"].Call(argument);//, Map.Empty);
			}
		}
		public class Library : Test
		{
			public override object GetResult(out int level)
			{
				level = 2;
				return FileSystem.fileSystem["libraryTest"].Call(Map.Empty);//, Map.Empty);
			}
		}
		public class Extents : Test
		{
			public override object  GetResult(out int level)
			{
				level=1;
				Map argument = Map.Empty;
				argument[1] = "first arg";
				argument[2] = "second=arg";
				return FileSystem.fileSystem["basicTest"];
			}
		}
		//[Test]
		//public object Serialization()
		//{
		//    Map map = FileSystem.fileSystem["basicTest"];
		//    return FileSystem.Serialize.Value(map).TrimStart();
		//}
		//[Test(2)]
		//public object Basic()
		//{
		//    Map argument = new StrategyMap();
		//    argument[1] = "first arg";
		//    argument[2] = "second=arg";
		//    return FileSystem.fileSystem["basicTest"].Call(argument);//, Map.Empty);
		//}
		//[Test(2)]
		//public object Library()
		//{
		//    return FileSystem.fileSystem["libraryTest"].Call(Map.Empty);//, Map.Empty);
		//}
		//[Test]
		//public object Extents()
		//{
		//    Map argument = Map.Empty;
		//    argument[1] = "first arg";
		//    argument[2] = "second=arg";
		//    return FileSystem.fileSystem["basicTest"];
		//}

	}
}
namespace testClasses
{
	[Serializable]
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
	public delegate object IntEvent (object intArg);
	public delegate object NormalEvent (object sender);
	[Serializable]
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
		public delegate string TestDelegate(string x);
		public static Delegate del;
		public static void TakeDelegate(TestDelegate d) 
		{
			del=d;
		}
		public static object GetResultFromDelegate() 
		{
			return del.DynamicInvoke(new object[] { "argumentString" });
			//return del.GetType().GetMethod("Invoke").Invoke(del.Target, new object[] { "argumentString" });
		}
		public double doubleValue=0.0;
		public float floatValue=0.0F;
		public decimal decimalValue=0.0M;
	}
	[Serializable]
	public class PositionalNoConversion : TestClass 
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
	public class NamedNoConversion : TestClass 
	{ 
		public NamedNoConversion(Map arg) 
		{
			Map def=new StrategyMap();
			def[1]="null";
			def["y"]="null";
			def["p2"]="null";
			if (arg.ContainsKey(1))
			{
				def[1] = arg[1];
			}
			if (arg.ContainsKey("y"))
			{
				def["y"] = arg["y"];
			}
			if (arg.ContainsKey("p2"))
			{
				def["y2"] = arg["y2"];
			}
			this.x=def[1].GetString();
			this.y=def["y"].GetString();
			this.z=def["p2"].GetString();
		}
		// refactor, remove
		public string Concatenate(Map arg) 
		{
			Map def=new StrategyMap();
			def[1]="null";
			def["b"]="null";
			def["c"]="null";

			if (arg.ContainsKey(1))
			{
				def[1] = arg[1];
			}
			if (arg.ContainsKey("b"))
			{
				def["b"] = arg["b"];
			}
			if (arg.ContainsKey("c"))
			{
				def["c"] = arg["c"];
			}
			return def[1].GetString()+def["b"].GetString()+def["c"].GetString()+
				this.x+this.y+this.z;
		}   
	}
	[Serializable]
	public class IndexerNoConversion : TestClass 
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
