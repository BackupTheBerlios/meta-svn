using System;
using System.Collections;
using Meta.Types;
using Meta.Execution;
public class map {
	public static IKeyValue keys(IKeyValue map) {
		int i=1;
		Map keys=new Map();
		foreach(DictionaryEntry entry in map) {
			keys[new Integer(i)]=entry.Key;
			i++;
		}
		return keys;
	}
	[MetaLibraryMethod]
	public static Map concat() {
		ArrayList maps=((Map)Interpreter.Arg).IntKeyValues;
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
	[MetaLibraryMethod]
	public static IKeyValue merge() {
		return (Map)Interpreter.MergeCollection(((Map)Interpreter.Arg).IntKeyValues);
	}
	// integrate this into .NET constructor in NetClass, which is cleaner
	// possibly remove completely, let's see how the editor turns out
	public static object With(object obj,IMap map) {
		NetObject netObject=new NetObject(obj);
		foreach(DictionaryEntry entry in map) {
			netObject[entry.Key]=entry.Value;
		}
		return obj;
	}
}

//public class map {
//	public static IKeyValue keys(IKeyValue map) {
//		int i=1;
//		Map keys=new Map();
//		foreach(DictionaryEntry entry in map) {
//			keys[new Integer(i)]=entry.Key;
//			i++;
//		}
//		return keys;
//	}
//	[MetaLibraryMethod]
//	public static Map concat() {
//		ArrayList maps=((Map)Interpreter.Arg).IntKeyValues;
//		int i=1;
//		Map combined=new Map();
//		foreach(Map map in maps) {
//			foreach(object val in map.IntKeyValues) {
//				combined[new Integer(i)]=val;
//				i++;
//			}
//		}
//		return combined;
//	}
////	public static Map Concatenate(params Map[] maps) {
////		int i=1;
////		Map combined=new Map();
////		foreach(Map map in maps) {
////			foreach(object val in map.IntKeyValues) {
////				combined[new Integer(i)]=val;
////				i++;
////			}
////		}
////		return combined;
////	}
//	[MetaLibraryMethod]
//	public static IKeyValue Merge() {
//		return (Map)Interpreter.MergeCollection(((Map)Interpreter.Arg).IntKeyValues);
//	}
//	public static object With(object obj,IMap map) {
//		NetObject netObject=new NetObject(obj);
//		foreach(DictionaryEntry entry in map) {
//			netObject[entry.Key]=entry.Value;
//		}
//		return obj;
//	}
//	[MetaLibraryMethod]
//	public static Map GetStructure() {
//		return GetTheStructure(((Map)Interpreter.Arg).IntKeyValues);
////		Map structure=new Map();
////		Hashtable keys=new Hashtable();
////		return structure;
//	}
//	private static Map GetTheStructure(ArrayList maps) {
//		//FIXME: Some bug here
//		Map merged=(Map)Interpreter.MergeCollection(maps);
//		if(!IsDeep(merged)) {
//			return merged;
//		}
//		return FixStructure(merged);
//
//	}
//	public static Map FixStructure(Map map) {
//		if(map.IntKeyValues.Count>0) {
//			return GetTheStructure(map.IntKeyValues);
//		}
//		else {
//			foreach(DictionaryEntry entry in map) {
//				if(entry.Value is Map) {
//					map[entry.Key]=FixStructure((Map)entry.Value);
//				}
//			}
//			return map;
//		}
//	}
//	private static bool IsDeep(Map map) {
//		foreach(DictionaryEntry entry in map) {
//			if(entry.Value is Map) {
//				return true;
//			}
//		}
//		return false;
//	}
////	private Map MergeIntKeyValues(Map map) {
////		ArrayList intKeyValues=map.IntKeyValues;
////		if(intKeyValues.Count>0) {
////			foreach(object key in map.IntKeyValues) {
////
////			}
////		}
////		else {
////		}
////	}
////	private Map GetStructure(ArrayList maps) {
////		Map result=Interpreter.MergeCollection(maps);
////		bool hasChildren=false;
////		foreach(DictionaryEntry entry in result) {
////			if(entry.Value is Map) {
////				hasChildren=true;
////				break;
////			}
////		}
////		if(!hasChildren) {
////			return result;
////		}
////		else if(result.IntKeyValues.Count>0)
////			ArrayList intKeyValues=result.IntKeyValues;
////			for(int i=0;i<intKeyValues.Count;i++) {
////
////			}
////			if(result.IntKeyValues.Count>0) {
////			}
////		}
////	}
////		Map result=new Map();
////		Hashtable keys=new Hashtable();
////		foreach(Map map in maps) {
////			foreach(object key in map.Keys) {
////				keys[key]="";
////			}
////		}
////		foreach(object key in keys) {
////			ArrayList keyMaps=new ArrayList();
////			foreach(Map map in maps) {
////				if() {
////				}
////			}
////		}
////	public Map GetStructure(Map maps) {
////		Map structure=new Map();
////		Hashtable keys=new Hashtable();
////		return structure;
////	}
//
//}
