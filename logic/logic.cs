using System;

public class logic
{
	public static bool equal(object a,object b) {
		return a.Equals(b);
	}
	public static bool and(bool a,bool b) {
		return a && b;
	}
	public static bool or(bool a,bool b) {
		return a || b;
	}
	public static bool not(bool a) {
		return !a;
	}
}
