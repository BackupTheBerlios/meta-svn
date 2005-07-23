using System;


using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using Meta;

public class map 
{
	public class interpreter 
	{
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
	public static void Execute(string fileName) 
	{ // TODO: not a good name, takes a file name, not a string after all, 
		// adding a character "f" for files doesn't make sense, though
		// After all, the file system will be integrated anyway.
		// Once the file system is integrated, one can simpley select the file one
		// wants to run and call it. This function won't be needed anymore then.
		Process process=new Process();
		process.StartInfo.FileName=Application.ExecutablePath;
		process.StartInfo.Arguments=fileName;
		process.Start();
	}
	public static bool ContainsKey(Map map,object key) 
	{
		string a="";
		return map.ContainsKey(key);
	}
	public static int Length(Map map) 
	{
		return map.Count;
	}
	public static Map TrimStart(Map arg) 
	{
		Map map=(Map)arg[new Integer(1)];
		object obj=arg[new Integer(2)];

		Map result=new Map();
		int counter=1;
		foreach(object o in map.Array) 
		{
			if(obj.Equals(o)) 
			{
				result[new Integer(counter)]=o;
				counter++;
			}
			else 
			{
				break;
			}
		}
		return result;
	}
	//	public static int CountStart(Map map,object obj) { // TODO: dumb name
	//		int count=0;
	//		foreach(object o in map.Array) {
	//			if(obj.Equals(o)) {
	//				count++;
	//			}
	//			else {
	//				break;
	//			}
	//		}
	//		return count;
	//	}
	public static IKeyValue Keys(IKeyValue map) 
	{
		int i=1;
		Map keys=new Map();
		foreach(DictionaryEntry entry in map) 
		{
			keys[new Integer(i)]=entry.Key;
			i++;
		}
		return keys;
	}
	public static Map Join(Map arg) 
	{ // rename to "append"
		ArrayList maps=arg.Array;
		int i=1;
		Map combined=new Map();
		foreach(Map map in maps) 
		{ // TODO: eigentlich nur die Arrays verwenden
			foreach(object val in map.Array) 
			{
				combined[new Integer(i)]=val;
				i++;
			}
		}
		return combined;
	}
	public static IKeyValue Merge(Map arg) 
	{
		return (Map)Interpreter.MergeCollection(arg.Array);
	}
	public static object Init(object obj,IMap map) 
	{ // make merge general enough to replace this
		NetObject netObject=new NetObject(obj);
		foreach(DictionaryEntry entry in map) 
		{
			netObject[entry.Key]=entry.Value;
		}
		return obj;
	}
	public static Map Remove(object oToRemove,Map mArray)
	{
		Map mResult=new Map();
		int iCounter=1;
		foreach(object oIntegerKeyValue in mArray.Array)
		{
			if(!oIntegerKeyValue.Equals(oToRemove))
			{
				mResult[new Integer(iCounter)]=oIntegerKeyValue;
				iCounter++;
			}
		}
		return mResult;
	}
	public static Map Foreach(Map mArray,Map mFunction)
	{
		Map mResult=new Map();
		int iCounter=1;
		foreach(object oIntegerKeyValue in mArray.Array)
		{
			mResult[new Integer(iCounter)]=mFunction.Call(oIntegerKeyValue);
			iCounter++;
		}
		return mResult;
	}
	public static Map Apply(Map mFunction,Map mArray) 
	{ // switch this the arguments around
		Map mResult=new Map();
		int counter=1;
		foreach(object oKey in mArray.Keys) 
		{
			Map mArgument=new Map();
			mArgument[new Map("key")]=oKey;
			mArgument[new Map("value")]=mArray[oKey];
			mResult[new Integer(counter)]=mFunction.Call(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object If(Map argM) 
	{ // maybe automatically convert Maps to MapAdapters??
		bool conditionB=(bool)Interpreter.DotNetFromMeta(argM[new Integer(1)],typeof(bool));
		Map thenF=(Map)argM[new Map("then")];
		Map elseF=(Map)argM[new Map("else")];
		if(conditionB) 
		{
			return thenF.Call(new Map());
		}
		else if(elseF!=null) 
		{
			return elseF.Call(new Map());
		}
		else 
		{
			return new Map();
		}		
	}
}
public class logic
{
	public static bool Equal(object a,object b) 
	{
		return a.Equals(b);
	}
	public static bool Not(bool a) 
	{
		return !a;
	}
	public static bool And(Map arg) 
	{
		foreach(bool val in arg.Array) 
		{
			if(!val) 
			{
				return false;
			}
		}
		return true;
	}
	public static bool Or(Map arg) 
	{
		foreach(bool val in arg.Array) 
		{
			if(val) 
			{
				return true;
			}
		}
		return false;
	}
}
public class math
{
	public static Integer Add(Integer x,Integer y) 
	{
		return x+y;
	}
	public static Integer Subtract(Integer x,Integer y) 
	{
		return x-y;		
	}
	public static Integer Multiply(Integer x,Integer y) 
	{
		return x*y;
	}
	public static Integer Divide(Integer x,Integer y) 
	{
		return x/y;
	}
	public static bool Smaller(Integer x,Integer y) 
	{
		return x<y;
	}
	public static bool Greater(Integer x,Integer y) 
	{
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