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
using System.Threading;
using Microsoft.VisualStudio.DebuggerVisualizers;


namespace Test 
{
	public class Test:TestRunner
	{
		[STAThread]
		public static void Main(string[] args) 
		{
			new Test().Run();
			//Map map = SpecialMaps.Local;
			//File.WriteAllText(@"C:\Projects\Meta\Library\test.txt", ,Encoding.Default);
			//FileSystem.singleton["editor"].Call(Map.Empty, Map.Empty);
		}
		protected override string TestDirectory
		{
			get 
			{
				return Path.Combine(Directory.GetParent(Process.LibraryPath).FullName, "Test");
			}
		}
		[Test]
		public object Serialization()
		{
			Map map = SpecialMaps.Local;
			return Meta.Serialize.Value(map).TrimStart();
		}
		[Test(2)]
		public object Basic()
		{
			Map argument = new NormalMap();
			argument[1] = "first arg";
			argument[2] = "second=arg";
			return SpecialMaps.Local["basicTest"].Call(argument);//, Map.Empty);
		}
		//[Test(2)]
		//public object Basic()
		//{
		//    Map argument = new NormalMap();
		//    argument[1] = "first arg";
		//    argument[2] = "second=arg";
		//    return SpecialMaps.Local["basicTest"].Call(argument, Map.Empty);
		//}
		[Test(2)]
		public object Library()
		{
			return SpecialMaps.Local["libraryTest"].Call(Map.Empty);//, Map.Empty);
		}
		//[Test(2)]
		//public object Library()
		//{
		//    return SpecialMaps.Local["libraryTest"].Call(Map.Empty, Map.Empty);
		//}
		[Test]
		public object Extents()
		{
			Map argument = Map.Empty;
			argument[1] = "first arg";
			argument[2] = "second=arg";
			return SpecialMaps.Local["basicTest"];
		}

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
			Map def=new NormalMap();
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
			Map def=new NormalMap();
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
