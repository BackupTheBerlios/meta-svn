using System;
using Meta.Types;

public class math
{
	public static Integer add(Integer x,Integer y) {
		return x+y;
	}
	public static Integer subtract(Integer x,Integer y) {
		return x-y;		
	}
	public static Integer multiply(Integer x,Integer y) {
		return x*y;
	}
	public static Integer divide(Integer x,Integer y) {
		return x/y;
	}
	public static bool smaller(Integer x,Integer y) {
		return x<y;
	}
	public static bool greater(Integer x,Integer y) {
		return x>y;
	}
	public static Integer bitwiseOr(Integer x,Integer y) {
		return x|y;
	}
}
