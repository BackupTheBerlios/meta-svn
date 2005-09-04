using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using Meta;


public class map:MetaLibrary
{
	public static bool ContainsKey(Map map,Map key) 
	{
		string a="";
		return map.ContainsKey(key);
	}
	public static int Length(Map map) 
	{
		return map.Count;
	}
	public static Map TrimStart(Map arg)  // TODO: remove this completely
	{
		Map map=arg[1];
		object obj=arg[2];

		Map result=new NormalMap();
		int counter=1;
		foreach(Map o in map.Array) 
		{
			if(obj.Equals(o)) 
			{
				result[counter]=o;
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
	public static Map Keys(Map map) 
	{
		int i=1;
		Map keys=new NormalMap();
		foreach(DictionaryEntry entry in map) 
		{
			keys[i]=(Map)entry.Key;
			i++;
		}
		return keys;
	}
	public static Map Join(Map arg) 
	{ // rename to "append"
		ArrayList maps=arg.Array;
		int i=1;
		Map combined=new NormalMap();
		foreach(Map map in maps) 
		{ // TODO: eigentlich nur die Arrays verwenden
			foreach(Map val in map.Array) 
			{
				combined[i]=val;
				i++;
			}
		}
		return combined;
	}
	public static Map Merge(Map arg) // TODO: replace all Maps with Maps
	{
		return Interpreter.MergeCollection(arg.Array);
	}
	public static object Init(object obj,Map map) 
	{ // make merge general enough to replace this
		DotNetObject DotNetObject=new DotNetObject(obj);
		foreach(DictionaryEntry entry in map) 
		{
			DotNetObject[(Map)entry.Key]=(Map)entry.Value;
		}
		return obj;
	}
	public static Map Remove(object oToRemove,Map mArray)
	{
		Map mResult=new NormalMap();
		int iCounter=1;
		foreach(Map oIntegerKeyValue in mArray.Array)
		{
			if(!oIntegerKeyValue.Equals(oToRemove))
			{
				mResult[iCounter]=oIntegerKeyValue;
				iCounter++;
			}
		}
		return mResult;
	}
	public static Map Foreach(Map mArray,Map mFunction) // TODO: use MapInfos here
	{
		Map mResult=new NormalMap();
		int iCounter=1;
		foreach(Map oIntegerKeyValue in mArray.Array)
		{
			mResult[iCounter]=mFunction.Call(oIntegerKeyValue);
			iCounter++;
		}
		return mResult;
	}
	public static Map Apply(Map mFunction,Map mArray) // TODO: use MapInfo
	{ // switch this the arguments around
		Map mResult=new NormalMap();
		int counter=1;
		foreach(Map oKey in mArray.Keys) 
		{
			Map mArgument=new NormalMap();
			mArgument["key"]=oKey;
			mArgument["value"]=mArray[oKey];
			mResult[counter]=mFunction.Call(mArgument);
			counter++;
		}
		return mResult;
	}
	public static object If(Map argM) 
	{
		bool conditionB=(bool)Convert.ToBoolean(Meta.Transform.ToDotNet(argM[1]));//,typeof(bool));
		Map thenF=argM["then"];
		Map elseF=argM["else"];
		if(conditionB) 
		{
			return thenF.Call(new NormalMap());
		}
		else if(elseF!=null) 
		{
			return elseF.Call(new NormalMap());
		}
		else 
		{
			return new NormalMap();
		}		
	}
}
public class logic:MetaLibrary
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
public class math:MetaLibrary
{
	public static Map Add(Map x,Map y) // TODO: decide whether to use native types in library or not, and apply everywhere
	{
		return new NormalMap(x.Integer+y.Integer);
	}
	public static Map Subtract(Map x,Map y) 
	{
		return new NormalMap(x.Integer-y.Integer);		
	}
	public static Map Multiply(Map x,Map y) 
	{
		return new NormalMap(x.Integer*y.Integer);
	}
	public static Map Divide(Map x,Map y) 
	{
		return new NormalMap(x.Integer/y.Integer);
	}
	public static bool Smaller(Map x,Map y) 
	{
		return x.Integer<y.Integer;
	}
	public static bool Greater(Map x,Map y) 
	{
		return x.Integer>y.Integer;
	}
	public class bitwise:MetaLibrary
	{
		public static NormalMap Or(params NormalMap[] integers)
		{
			Integer result=new Integer(0);
			foreach(NormalMap i in integers)
			{
				result|=i.Integer;
			}
			return new NormalMap(result);
		}	
	}
	public static NormalMap BitwiseOr(params NormalMap[] integers)
	{
		Integer result=new Integer(0);
		foreach(NormalMap i in integers)
		{
			result|=i.Integer;
		}
		return new NormalMap(result);
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