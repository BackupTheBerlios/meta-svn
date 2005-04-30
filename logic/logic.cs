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
	}
	[MetaLibraryMethod]
	public static bool or(Map arg) {
		foreach(bool val in arg.IntKeyValues) {
			if(val) {
				return true;
			}
		}
		return false;
	}
}
