using System;

public class logic
{
	public static bool Equal(object a,object b) {
		return a.Equals(b);
	}
	public static bool And(bool a,bool b) {
		return a && b;
	}
	public static bool Or(bool a,bool b) {
		return a || b;
	}
	public static bool Not(bool a) {
		return !a;
	}
}
