using System;
using Meta.Types;

public class math
{
	public static Number Add(Number x,Number y) {
		return x+y;
	}
	public static Number Subtract(Number x,Number y) {
		return x-y;		
	}
	public static Number Multiply(Number x,Number y) {
		return x*y;
	}
	public static Number Divide(Number x,Number y) {
		return x/y;
	}
	public static bool Smaller(Number x,Number y) {
		return x<y;
	}
	public static bool Greater(Number x,Number y) {
		return x>y;
	}
}
