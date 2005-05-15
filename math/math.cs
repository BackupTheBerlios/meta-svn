//	Meta is a simple programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License
//	as published by the Free Software Foundation; either version 2
//	of the License, or (at your option) any later version.
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
