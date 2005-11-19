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
using Meta.Testing;
using System.Threading;
using Microsoft.VisualStudio.DebuggerVisualizers;


namespace Test 
{
	class Test 
	{
		public static string path="";
		[STAThread]
		public static void Main(string[] args) 
		{
			TestRunner.Run(typeof(Tests), Path.Combine(Directory.GetParent(Process.LibraryPath).FullName, "Test"));
			//FileSystem.singleton["editor"].Call(Map.Empty, Map.Empty);
		}
	}
	public class Tests 
	{
		[Test(2)]
		public static object Basic()//ref int level)
		{
			Map argument = new NormalMap();
			argument[1] = "first arg";
			argument[2] = "second=arg";
			//level = 2;
			return FileSystem.singleton["basicTest"].Call(argument, new NormalMap());
		}
		[Test(2)]
		public static object Library()//ref int level)
		{
			//level = 2;
			return FileSystem.singleton["libraryTest"].Call(new NormalMap(), new NormalMap());
		}
		[Test]
		public static object Extents()//ref int level)
		{
			//level = 1;
			Map argument = new NormalMap();
			argument[1] = "first arg";
			argument[2] = "second=arg";
			return FileSystem.singleton["basicTest"];
		}
		//public class Basic : TestCase
		//{
		//    public override object Run(ref int level)
		//    {
		//        Map argument = new NormalMap();
		//        argument[1] = "first arg";
		//        argument[2] = "second=arg";
		//        level = 2;
		//        return FileSystem.singleton["basicTest"].Call(argument, new NormalMap());
		//    }
		//}
		//public class Library:TestCase
		//{
		//    public override object Run(ref int level)
		//    {
		//        level=2;
		//        return FileSystem.singleton["libraryTest"].Call(new NormalMap(),new NormalMap());
		//    }
		//}

		//public class Extents:TestCase
		//{
		//    public override object Run(ref int level)
		//    {
		//        level = 1;
		//        Map argument=new NormalMap();
		//        argument[1]="first arg";
		//        argument[2]="second=arg";
		//        return FileSystem.singleton["basicTest"];
		//    }
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
	public delegate int IntEvent (int intArg);
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
	[Serializable]
	public class NamedNoConversion : TestClass 
	{ 
		public NamedNoConversion(Map arg) 
		{
			Map def=new NormalMap();
			def[1]="null";
			def["y"]="null";
			def["p2"]="null";
            Map toMerge = new NormalMap();
            toMerge[1] = def;
            toMerge[2] = arg;
			arg=(Map)Interpreter.Merge(toMerge);

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
            Map toMerge = new NormalMap();
            toMerge[1] = def;
            toMerge[2] = arg;
            // merging shouldnt be used here, not relevant to the test
			arg=(Map)Interpreter.Merge(toMerge);
			return arg[1].GetString()+arg["b"].GetString()+arg["c"].GetString()+
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
