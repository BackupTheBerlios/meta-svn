using System;
using Meta.Types;

public class math
{
	public static Integer Add(Integer x,Integer y) {
		return x+y;
	}
	public static Integer Subtract(Integer x,Integer y) {
		return x-y;		
	}
	public static Integer Multiply(Integer x,Integer y) {
		return x*y;
	}
	public static Integer Divide(Integer x,Integer y) {
		return x/y;
	}
	public static bool Smaller(Integer x,Integer y) {
		return x<y;
	}
	public static bool Greater(Integer x,Integer y) {
		return x>y;
	}
	public static Integer BitwiseOr(Integer x,Integer y) {
		return x|y;
	}
}
