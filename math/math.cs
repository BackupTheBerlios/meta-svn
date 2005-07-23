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
	public static Integer BitwiseOr(params Integer[] integers)
	{
		Integer result=new Integer(0);
		foreach(Integer i in integers)
		{
			result|=i;
		}
		return result;
	}
//	public static Integer BitwiseOr(Integer x,Integer y) {
//		return x|y;
//	}
}
