using System;
using Meta.Types;
using Meta.Execution;

public class logic
{
	public static bool equal(object a,object b) {
		return a.Equals(b);
	}
	public static bool not(bool a) {
		return !a;
	}
	[MetaLibraryMethod]
	public static bool and(Map arg) {
		foreach(bool val in arg.IntKeyValues) {
			if(!val) {
				return false;
			}
		}
		return true;
	}//	[MetaLibraryMethod]
//	public static bool and() {
//		foreach(bool val in ((Map)Interpreter.Arg).IntKeyValues) {
//			if(!val) {
//				return false;
//			}
//		}
//		return true;
//	}
//	public static bool and(bool a,bool b) {
//		return a && b;
//	}
	[MetaLibraryMethod]
	public static bool or(Map arg) {
		foreach(bool val in arg.IntKeyValues) {
			if(val) {
				return true;
			}
		}
		return false;
		//		return a || b;
	}
	//	[MetaLibraryMethod]
//	public static bool or() {
//		foreach(bool val in ((Map)Interpreter.Arg).IntKeyValues) {
//			if(val) {
//				return true;
//			}
//		}
//		return false;
////		return a || b;
//	}
//	public static bool or(bool a,bool b) {
//		return a || b;
//	}
//	public static bool not(bool a) {
//		return !a;
//	}
}
