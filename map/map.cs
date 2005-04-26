using System;
using System.IO;
using System.Collections;
using Meta.Types;
using Meta.Execution;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

public class map {
	public static void run(string fileName) {
		Process process=new Process();
		process.StartInfo.FileName=Application.ExecutablePath;
		process.StartInfo.Arguments=fileName;
		process.Start();
//		return Interpreter.Run(fileName,new Map());
	}
	public static bool contains(object key,Map map) {
		string a="";
		return map.ContainsKey(key);
	}
	public static bool hasKey(object key,Map map) {
		string a="";
		return map.ContainsKey(key);
	}
	public static int length(Map map) {
		return map.Count;
	}
	[MetaLibraryMethod]
	public static Map trimStart(Map arg) {
		Map map=(Map)arg[new Integer(1)];
		object obj=arg[new Integer(2)];

		Map result=new Map();
		int counter=1;
		foreach(object o in map.IntKeyValues) {
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
//	[MetaLibraryMethod]
//	public static Map trimStart() {
//		Map map=(Map)arg[new Integer(1)];
//		object obj=arg[new Integer(2)];
//
//		Map result=new Map();
//		int counter=1;
//		foreach(object o in map.IntKeyValues) {
//			if(obj.Equals(o)) {
//				result[new Integer(counter)]=o;
//				counter++;
//			}
//			else {
//				break;
//			}
//		}
//		return result;
//	}
	public static int countStart(Map map,object obj) {
		int count=0;
		foreach(object o in map.IntKeyValues) {
			if(obj.Equals(o)) {
				count++;
			}
			else {
				break;
			}
		}
		return count;
	}
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
	public static Map concatenate(Map arg) {
		ArrayList maps=arg.IntKeyValues;
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
	public static IKeyValue merge(Map arg) {
		return (Map)Interpreter.MergeCollection(arg.IntKeyValues);
	}
	// integrate this into .NET constructor in NetClass, which is cleaner
	// possibly remove completely, let's see how the editor turns out
	public static object with(object obj,IMap map) {
		NetObject netObject=new NetObject(obj);
		foreach(DictionaryEntry entry in map) {
			netObject[entry.Key]=entry.Value;
		}
		return obj;
	}
	public static Map storage(Map location) {
		string path="";
		foreach(Map loc in location.IntKeyValues) {
			path=Path.Combine(path,((Map)loc).GetDotNetString());
		}
		return new Storage(path);
	}
	public class Storage:Map {
		string path;
		public Storage(string path){
			this.path=path;
		}
	}
	public class StorageMap:Map {
		Storage storage;
		public StorageMap(Storage storage) {
			this.storage=storage;
		}
		public override object this[object key] {
			get {
				return base[key];
			}
			set {
				base[key]=value;
			}
		}
	}
//	public static Map storage(string path) {
//		new Storage(path);
//	}
//	//make only uppermost class a Storage class?
//	// could be combined with clone propagation. Not every assignment
//	// should cause propagation, though. Maybe have separate Propagation
//	// property. Only problem with Storage class is that I need a Factory
//	// and that it is another class entirely, which doesn't feel good somehow.
//
//	//What should happen if a map within Storage is changed? The assignment
//	//should cause the thing to be saved. Maps must save line from-to, which
//	//they have been read from. If map is changed and saved, all lines must
//	//be corrected. Simply add the difference up. Maybe compute the lines
//	//lazily in the first place. Reading in the entire file and saving the
//	//entire file is not acceptable. So, will need separate class anyway.
//	//Only question is, inherit Map or simply implement IMap interface.
//	//Implement IMap probably cleaner. Maybe create base class that has a
//	//Map and passes on all method calls. Inheriting Map is also ok. Line
//	//numbers must be included when parsing. Who should have the Writer.
//	
//	//Storage is a normal Map that has line info, possibly combine with debug
//	//stuff or similar. Storage "mama" class corresponds to a storage file.
//	//Storage maps become normal maps when copied. When assigned to, Storage 
//	//maps alarm the "mama" class, which writes them to the text file and
//	//updates all line numbers. Writing should be reasonably fast. Look into
//	//some more best practices here, maybe cache in memory? Probably not,
//	//because too insecure. Keep it simple at first. Writing should be
//	//finished when the function returns. No support for transactions, this
//	//is way overkill for medium sized applications, plus they introduce
//	//so much complexity as to offer less security rather than more, usually.
//	//Several entries can be updated at once by assigning a whole map instead
//	//of just an integer.
//	private class Storage:Map {
//		StreamWriter writer;
//		public Storage(string path) {
//			Interpreter.RunWithoutLibrary(path,new Map());
//			this.writer=new StreamWriter(path);
//		}
//		private 
//	}
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
//		ArrayList maps=arg.IntKeyValues;
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
//		return (Map)Interpreter.MergeCollection(arg.IntKeyValues);
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
//		return GetTheStructure(arg.IntKeyValues);
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
