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
	public class interpreter {
//		public static event BreakPointDelegate BreakPoint;
		public static int line=0; // only needed because of test in basicTest.meta
//		public static void breakPointCallBack(Map mCallback) {
//			Interpreter.OnBreak();
//		}
//		public static int column=0;
//		public static void run() {
//			BreakPoint(new Map("stuff"));
//		}
	}
	public static void execute(string fileName) { // TODO: not a good name, takes a file name, not a string after all, 
		// adding a character "f" for files doesn't make sense, though
		// After all, the file system will be integrated anyway.
		// Once the file system is integrated, one can simpley select the file one
		// wants to run and call it. This function won't be needed anymore then.
		Process process=new Process();
		process.StartInfo.FileName=Application.ExecutablePath;
		process.StartInfo.Arguments=fileName;
		process.Start();
	}
	public static bool contains(Map map,object key) {
		string a="";
		return map.bContainsO(key);
	}
	public static int length(Map map) {
		return map.iCount;
	}
	public static Map sTrimStartS(Map arg) {
		Map map=(Map)arg[new Integer(1)];
		object obj=arg[new Integer(2)];

		Map result=new Map();
		int counter=1;
		foreach(object o in map.aIntegerKeyValues) {
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
		foreach(object o in map.aIntegerKeyValues) {
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
	public static Map join(Map arg) { // rename to "append"
		ArrayList maps=arg.aIntegerKeyValues;
		int i=1;
		Map combined=new Map();
		foreach(Map map in maps) { // TODO: eigentlich nur die Arrays verwenden
			foreach(object val in map.aIntegerKeyValues) {
				combined[new Integer(i)]=val;
				i++;
			}
		}
		return combined;
	}
	public static IKeyValue merge(Map arg) {
		return (Map)Interpreter.MergeCollection(arg.aIntegerKeyValues);
	}
	public static object init(object obj,IMap map) { // make merge general enough to replace this
		NetObject netObject=new NetObject(obj);
		foreach(DictionaryEntry entry in map) {
			netObject[entry.Key]=entry.Value;
		}
		return obj;
	}
//	public static Map AApplyFA(Map mFunction,Map mArray) {
//		Map mResult=new Map();
//		int counter=1;
//		foreach(object oArgument in mArray.aIntegerKeyValues) {
//			mResult[new Integer(counter)]=mFunction.oCallO(oArgument);
//			counter++;
//		}
//		return mResult;
//	}
	public static Map apply(Map mFunction,Map mArray) { // switch this the arguments around
		Map mResult=new Map();
		int counter=1;
		foreach(object oKey in mArray.aKeys) {
			Map mArgument=new Map();
			mArgument[new Map("key")]=oKey;
			mArgument[new Map("value")]=mArray[oKey];
			mResult[new Integer(counter)]=mFunction.oCallO(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object @if(Map argM) { // maybe automatically convert Maps to MapAdapters??
		bool conditionB=(bool)Interpreter.ODotNetFromMetaO(argM[new Integer(1)],typeof(bool));
		Map thenF=(Map)argM[new Integer(2)];
		Map elseF=(Map)argM[new Integer(3)];
		if(conditionB) {
			return thenF.oCallO(new Map());
		}
		else if(elseF!=null) {
			return elseF.oCallO(new Map());
		}
		else {
			return new Map();
		}		
	}
}
