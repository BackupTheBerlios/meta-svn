using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using Meta;


public class map 
{
	public class interpreter 
	{
		//		public static event BreakPointDelegate BreakPoint;
		public static int line=0; // only needed because of test in basicTest.meta
		//		public static void breakPointCallBack(IMap mCallback) {
		//			Interpreter.OnBreak();
		//		}
		//		public static int column=0;
		//		public static void run() {
		//			BreakPoint(new IMap("stuff"));
		//		}
	}
	public static bool ContainsKey(IMap map,object key) 
	{
		string a="";
		return map.ContainsKey(key);
	}
	public static int Length(IMap map) 
	{
		return map.Count;
	}
	public static IMap TrimStart(IMap arg) 
	{
		IMap map=(IMap)arg[new Integer(1)];
		object obj=arg[new Integer(2)];

		IMap result=new IMap();
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
	//	public static int CountStart(IMap map,object obj) { // TODO: dumb name
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
	public static IMap Keys(IMap map) 
	{
		int i=1;
		IMap keys=new IMap();
		foreach(DictionaryEntry entry in map) 
		{
			keys[new Integer(i)]=entry.Key;
			i++;
		}
		return keys;
	}
	public static IMap Join(IMap arg) 
	{ // rename to "append"
		ArrayList maps=arg.Array;
		int i=1;
		IMap combined=new IMap();
		foreach(IMap map in maps) 
		{ // TODO: eigentlich nur die Arrays verwenden
			foreach(object val in map.Array) 
			{
				combined[new Integer(i)]=val;
				i++;
			}
		}
		return combined;
	}
	public static IMap Merge(IMap arg) // TODO: replace all Maps with IMaps
	{
		return (IMap)Interpreter.MergeCollection(arg.Array);
	}
	public static object Init(object obj,IMap map) 
	{ // make merge general enough to replace this
		DotNetObject DotNetObject=new DotNetObject(obj);
		foreach(DictionaryEntry entry in map) 
		{
			DotNetObject[entry.Key]=entry.Value;
		}
		return obj;
	}
	public static IMap Remove(object oToRemove,IMap mArray)
	{
		IMap mResult=new IMap();
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
	public static IMap Foreach(IMap mArray,IMap mFunction)
	{
		IMap mResult=new IMap();
		int iCounter=1;
		foreach(object oIntegerKeyValue in mArray.Array)
		{
			mResult[new Integer(iCounter)]=mFunction.Call(oIntegerKeyValue);
			iCounter++;
		}
		return mResult;
	}
	public static IMap Apply(IMap mFunction,IMap mArray) 
	{ // switch this the arguments around
		IMap mResult=new IMap();
		int counter=1;
		foreach(object oKey in mArray.Keys) 
		{
			IMap mArgument=new IMap();
			mArgument[new IMap("key")]=oKey;
			mArgument[new IMap("value")]=mArray[oKey];
			mResult[new Integer(counter)]=mFunction.Call(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object If(IMap argM) 
	{ // maybe automatically convert Maps to MapAdapters??
		bool conditionB=(bool)System.Convert.ToBoolean(Meta.Convert.ToDotNet(argM[new Integer(1)]));//,typeof(bool));
		IMap thenF=(IMap)argM[new IMap("then")];
		IMap elseF=(IMap)argM[new IMap("else")];
		if(conditionB) 
		{
			return thenF.Call(new IMap());
		}
		else if(elseF!=null) 
		{
			return elseF.Call(new IMap());
		}
		else 
		{
			return new IMap();
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
	public static bool And(IMap arg) 
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
	public static bool Or(IMap arg) 
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