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
//		[MetaMethod("(1,y,2)")]
		public NamedNoConversion() {
			Map arg=(Map)Interpreter.Arg;
			Map def=new Map();
<<<<<<< .mine
			def[new Integer(1)]=new Map("null");
			def[new Map("y")]=new Map("null");
			def[new Map("p2")]=new Map("null");
=======
			def[new Number(1)]=Interpreter.StringToMap("null");
			def[Interpreter.StringToMap("y")]=Interpreter.StringToMap("null");
			def[Interpreter.StringToMap("p2")]=Interpreter.StringToMap("null");
>>>>>>> .r97
			arg=(Map)Interpreter.Merge(def,arg);
			this.x=(string)Interpreter.MapToString((Map)arg[new Number(1)]);
			this.y=(string)Interpreter.MapToString((Map)arg[new Map("y")]);
			this.z=(string)Interpreter.MapToString((Map)arg[new Map("p2")]);
		}
		public string Concatenate() {
			Map arg=(Map)Interpreter.Arg;
			Map def=new Map();
<<<<<<< .mine
			def[new Integer(1)]=new Map("null");
			def[new Map("b")]=new Map("null");
			def[new Map("c")]=new Map("null");
=======
			def[new Number(1)]=Interpreter.StringToMap("null");
			def[Interpreter.StringToMap("b")]=Interpreter.StringToMap("null");
			def[Interpreter.StringToMap("c")]=Interpreter.StringToMap("null");
>>>>>>> .r97
			arg=(Map)Interpreter.Merge(def,arg);
			return (string)Interpreter.MapToString((Map)arg[new Number(1)])+
				(string)Interpreter.MapToString((Map)arg[new Map("b")])+
				(string)Interpreter.MapToString((Map)arg[new Map("c")])+
				this.x+this.y+this.z;
		}
		//		[MetaMethod("(1,b,c)")]
//		public string Concatenate() {
//			Map arg=(Map)Interpreter.Arg;
//			Map def=new Map();
//			def[1]="null";
//			def["b"]="null";
//			def["c"]="null";
//			arg=(Map)Interpreter.Merge(def,arg);
//			return (string)arg[1]+(string)arg["b"]+(string)arg["c"]+this.x+this.y+this.z;
//		}
//		public NamedNoConversion(Map arg) {
//			Map def=new Map();
//			def[1]="null";
//			def["y"]="null";
//			def["p2"]="null";
//			arg=(Map)Interpreter.MergeTwo(def,arg);
//			this.x=(string)arg[1];
//			this.y=(string)arg["y"];
//			this.z=(string)arg["p2"];
//		}
////		[MetaMethod("(1,b,c)")]
//		public string Concatenate(Map arg) {
//			Map def=new Map();
//			def[1]="null";
//			def["b"]="null";
//			def["c"]="null";
//			arg=(Map)Interpreter.MergeTwo(def,arg);
//			return (string)arg[1]+(string)arg["b"]+(string)arg["c"]+this.x+this.y+this.z;
//		}
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
