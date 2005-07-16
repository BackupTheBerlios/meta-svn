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
using Meta.Types;
using Meta.Execution;
using System.Reflection;
namespace testClasses
{
	public class MemberTest {
		public static string classField="default";
		public string instanceField="default";

		public static string ClassProperty {
			get {
				return classField;
			}
			set {
				classField=value;
			}
		}
		public string InstanceProperty {
			get {
				return this.instanceField;
			}
			set {
				this.instanceField=value;
			}
		}
	}
	public delegate int IntEvent (int intArg);
	public delegate object NormalEvent (object sender);
	public class TestClass {
		public TestClass(){
		}
		public event IntEvent instanceEvent;
		public static event NormalEvent staticEvent;
		protected string x="unchangedX";
		protected string y="unchangedY";
		protected string z="unchangedZ";

		public static bool boolTest=false;

		public static object TestClass_staticEvent(object sender) {
			MethodBase[] m=typeof(TestClass).GetMethods();
			return null;
		}
		public static Delegate del;
		public static void TakeDelegate(Delegate d) {
			del=d;
		}
		public static object GetResultFromDelegate() {
			return del.GetType().GetMethod("Invoke").Invoke(del,new object[]{});
		}
		public double doubleValue=0.0;
		public float floatValue=0.0F;
		public decimal decimalValue=0.0M;
	}
	public class PositionalNoConversion:TestClass {
		public PositionalNoConversion(string p1,string b,string p2) {
			this.x=p1;
			this.y=b;
			this.z=p2;
		}
		public string Concatenate(string p1,string b,string c) {
			return p1+b+c+this.x+this.y+this.z;
		}
	}
	public class NamedNoConversion:TestClass { //refactor
		public NamedNoConversion(Map arg) {
			Map def=new Map();
			def[new Integer(1)]=new Map("null");
			def[new Map("y")]=new Map("null");
			def[new Map("p2")]=new Map("null");
			arg=(Map)Interpreter.Merge(def,arg);
			this.x=(string)((Map)arg[new Integer(1)]).String;
			this.y=(string)((Map)arg[new Map("y")]).String;
			this.z=(string)((Map)arg[new Map("p2")]).String;
		}
		public string Concatenate(Map arg) {
			Map def=new Map();
			def[new Integer(1)]=new Map("null");
			def[new Map("b")]=new Map("null");
			def[new Map("c")]=new Map("null");
			arg=(Map)Interpreter.Merge(def,arg);
			return (string)((Map)arg[new Integer(1)]).String+
				(string)((Map)arg[new Map("b")]).String+
				(string)((Map)arg[new Map("c")]).String+
				this.x+this.y+this.z;
		}
	}
	public class IndexerNoConversion:TestClass {
		public string this[string a] {
			get {
				return this.x+this.y+this.z+a;
			}
			set {
				this.x=a+value;
			}
		}
	}
}
