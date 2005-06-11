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
using Meta.Execution;

public class logic
{
	public static bool equal(object a,object b) {
		return a.Equals(b);
	}
	public static bool not(bool a) {
		return !a;
	}
//	[MetaLibraryMethod]
	public static bool and(Map arg) {
		foreach(bool val in arg.ArlojIntegerKeyValues) {
			if(!val) {
				return false;
			}
		}
		return true;
	}
//	[MetaLibraryMethod]
	public static bool or(Map arg) {
		foreach(bool val in arg.ArlojIntegerKeyValues) {
			if(val) {
				return true;
			}
		}
		return false;
	}
}
