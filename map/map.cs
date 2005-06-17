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
using System.IO;
using System.Collections;
using Meta.Types;
using Meta.Execution;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

public class map {
	public static void oRunS(string fileName) { // TODO: not a good name, takes a file name, not a string after all, 
		// adding a character "f" for files doesn't make sense, though
		// After all, the file system will be integrated anyway.
		// Once the file system is integrated, one can simpley select the file one
		// wants to run and call it. This function won't be needed anymore then.
		Process process=new Process();
		process.StartInfo.FileName=Application.ExecutablePath;
		process.StartInfo.Arguments=fileName;
		process.Start();
	}
	public static bool bHasKeyMO(Map map,object key) {
		string a="";
		return map.BContainsO(key);
	}
	public static int iCountM(Map map) {
		return map.ICount;
	}
	public static Map sTrimStartS(Map arg) {
		Map map=(Map)arg[new Integer(1)];
		object obj=arg[new Integer(2)];

		Map result=new Map();
		int counter=1;
		foreach(object o in map.AoIntegerKeyValues) {
			if(obj.Equals(o)) {
				result[new Integer(counter)]=o;
				counter++;
			}
			else {
				break;
			}
		}
		return result;
	}
	public static int iCountStartMO(Map map,object obj) { // TODO: dumb name
		int count=0;
		foreach(object o in map.AoIntegerKeyValues) {
			if(obj.Equals(o)) {
				count++;
			}
			else {
				break;
			}
		}
		return count;
	}
	public static IKeyValue aKeysM(IKeyValue map) {
		int i=1;
		Map keys=new Map();
		foreach(DictionaryEntry entry in map) {
			keys[new Integer(i)]=entry.Key;
			i++;
		}
		return keys;
	}
	public static Map aJoinAa(Map arg) {
		ArrayList maps=arg.AoIntegerKeyValues;
		int i=1;
		Map combined=new Map();
		foreach(Map map in maps) { // TODO: eigentlich nur die Arrays verwenden
			foreach(object val in map.AoIntegerKeyValues) {
				combined[new Integer(i)]=val;
				i++;
			}
		}
		return combined;
	}
	public static IKeyValue mMergeAm(Map arg) {
		return (Map)Interpreter.MergeCollection(arg.AoIntegerKeyValues);
	}
	public static object oInitOM(object obj,IMap map) {
		NetObject netObject=new NetObject(obj);
		foreach(DictionaryEntry entry in map) {
			netObject[entry.Key]=entry.Value;
		}
		return obj;
	}
//	public static Map AApplyFA(Map mFunction,Map mArray) {
//		Map mResult=new Map();
//		int counter=1;
//		foreach(object oArgument in mArray.AoIntegerKeyValues) {
//			mResult[new Integer(counter)]=mFunction.OCallO(oArgument);
//			counter++;
//		}
//		return mResult;
//	}
	public static Map AoApplyFM(Map mFunction,Map mArray) {
		Map mResult=new Map();
		int counter=1;
		foreach(object oKey in mArray.AoKeys) {
			Map mArgument=new Map();
			mArgument[new Map("key")]=oKey;
			mArgument[new Map("value")]=mArray[oKey];
			mResult[new Integer(counter)]=mFunction.OCallO(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object @if(bool bCondition,Map mThen,Map mElse) {
		if(bCondition) {
			return mThen.OCallO(new Map());
		}
		else if(mElse!=null) {
			return mElse.OCallO(new Map());
		}
		else {
			return new Map();
		}		
	}
}
