using System;
using System.Collections;
using Meta.Types;
using Meta.Execution;

public class map {
	public static IKeyValue GetKeys(IKeyValue map) {
		int i=1;
		Map keys=new Map();
		foreach(DictionaryEntry entry in map) {
			keys[new Integer(i)]=entry.Key;
			i++;
		}
		return keys;
	}
	public static Map Concatenate(params Map[] maps) {
		int i=1;
		Map combined=new Map();
		foreach(Map map in maps) {
			foreach(object val in map.IntKeyValues) {
				combined[new Integer(i)]=val;
				i++;
			}
		}
		return combined;
	}
	public static IKeyValue Merge() {
		return (Map)Interpreter.MergeCollection(((Map)Interpreter.Arg).IntKeyValues);
	}
}
