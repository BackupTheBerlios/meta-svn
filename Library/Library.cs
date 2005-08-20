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
		//			BreakPoint(new StrategyMap("stuff"));
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
		IMap map=(IMap)arg[new StrategyMap(new Integer(1))];
		object obj=arg[new StrategyMap(new Integer(2))];

		IMap result=new StrategyMap();
		int counter=1;
		foreach(object o in map.Array) 
		{
			if(obj.Equals(o)) 
			{
				result[new StrategyMap(new Integer(counter))]=o;
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
		IMap keys=new StrategyMap();
		foreach(DictionaryEntry entry in map) 
		{
			keys[new StrategyMap(new Integer(i))]=entry.Key;
			i++;
		}
		return keys;
	}
	public static IMap Join(IMap arg) 
	{ // rename to "append"
		ArrayList maps=arg.Array;
		int i=1;
		IMap combined=new StrategyMap();
		foreach(IMap map in maps) 
		{ // TODO: eigentlich nur die Arrays verwenden
			foreach(object val in map.Array) 
			{
				combined[new StrategyMap(new Integer(i))]=val;
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
		IMap mResult=new StrategyMap();
		int iCounter=1;
		foreach(object oIntegerKeyValue in mArray.Array)
		{
			if(!oIntegerKeyValue.Equals(oToRemove))
			{
				mResult[new StrategyMap(new Integer(iCounter))]=oIntegerKeyValue;
				iCounter++;
			}
		}
		return mResult;
	}
	public static IMap Foreach(IMap mArray,IMap mFunction) // TODO: use MapAdapters here
	{
		IMap mResult=new StrategyMap();
		int iCounter=1;
		foreach(object oIntegerKeyValue in mArray.Array)
		{
			mResult[new StrategyMap(new Integer(iCounter))]=mFunction.Call(oIntegerKeyValue);
			iCounter++;
		}
		return mResult;
	}
	public static IMap Apply(IMap mFunction,IMap mArray) 
	{ // switch this the arguments around
		IMap mResult=new StrategyMap();
		int counter=1;
		foreach(object oKey in mArray.Keys) 
		{
			IMap mArgument=new StrategyMap();
			mArgument[new StrategyMap("key")]=oKey;
			mArgument[new StrategyMap("value")]=mArray[oKey];
			mResult[new StrategyMap(new Integer(counter))]=mFunction.Call(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object If(IMap argM) 
	{ // maybe automatically convert Maps to MapAdapters??
		bool conditionB=(bool)System.Convert.ToBoolean(Meta.Convert.ToDotNet(argM[new StrategyMap(new Integer(1))]));//,typeof(bool));
		IMap thenF=(IMap)argM[new StrategyMap("then")];
		IMap elseF=(IMap)argM[new StrategyMap("else")];
		if(conditionB) 
		{
			return thenF.Call(new StrategyMap());
		}
		else if(elseF!=null) 
		{
			return elseF.Call(new StrategyMap());
		}
		else 
		{
			return new StrategyMap();
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
	public static IMap Add(IMap x,IMap y) // TODO: decide whether to use native types in library or not, and apply everywhere
	{
		return new StrategyMap(x.Number+y.Number);
	}
	public static IMap Subtract(IMap x,IMap y) 
	{
		return new StrategyMap(x.Number-y.Number);		
	}
	public static IMap Multiply(IMap x,IMap y) 
	{
		return new StrategyMap(x.Number*y.Number);
	}
	public static IMap Divide(IMap x,IMap y) 
	{
		return new StrategyMap(x.Number/y.Number);
	}
	public static bool Smaller(IMap x,IMap y) 
	{
		return x.Number<y.Number;
	}
	public static bool Greater(IMap x,IMap y) 
	{
		return x.Number>y.Number;
	}
	public static StrategyMap BitwiseOr(params StrategyMap[] integers)
	{
		Integer result=new Integer(0);
		foreach(StrategyMap i in integers)
		{
			result|=i.Number;
		}
		return new StrategyMap(result);
	}
//	public static Integer BitwiseOr(params Integer[] integers)
//	{
//		Integer result=new Integer(0);
//		foreach(Integer i in integers)
//		{
//			result|=i;
//		}
//		return result;
//	}
	//	public static Integer BitwiseOr(Integer x,Integer y) {
	//		return x|y;
	//	}
}