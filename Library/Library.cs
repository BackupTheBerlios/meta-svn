using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using Meta;

public class array:MetaLibrary
{
	/// <summary>
	/// Returns the concatenation of all arguments.
	/// </summary>
	/// // TODO: rename to Catenation or Linking
	public static Map Concatenation(Map arg) 
	{
		Integer i=1;
		Map array=new NormalMap();
		foreach(Map map in arg.Array) 
		{ 
			foreach(Map val in map.Array) 
			{
				array[i]=val;
				i+=1;
			}
		}
		return array;
	}

	/// <summary>
	/// Returns an array that is created by applying a "function" to every element of an "array".
	/// </summary>
	public static Map Application(Map arg)
	{
		// TODO: ensure "function" is callable, maybe?
		Argument.ContainsKey(arg,"function");
		Argument.ContainsKey(arg,"array");
		Map application=new NormalMap();
		int counter=1;
		foreach(Map element in arg["array"].Array)
		{
			application[counter]=arg["function"].Call(element);
			counter++;
		}
		return application;
	}
}
public class map:MetaLibrary
{

	/// <summary>
	/// Determines whether a "map" contains a "key".
	/// </summary>
	public static bool Containment(Map arg) 
	{
		Argument.ContainsKey(arg,"map");
		Argument.ContainsKey(arg,"key");
		return arg["map"].ContainsKey(arg["key"]);
	}
	/// <summary>
	/// Returns the keys of its argument.
	/// </summary>
	public static Map Keys(Map arg) 
	{
		Integer i=1;
		Map keys=new NormalMap();
		foreach(Map key in arg.Keys) 
		{
			keys[i]=key;
			i+=1;
		}
		return keys;
	}

    /// <summary>
    /// Returns the merging of all arguments.
    /// </summary>
	public static Map Merging(Map arg)
	{
		return Interpreter.MergeCollection(arg.Array);
	}

	/// <summary>
	/// Determines whether all arguments are identical to each other.
	/// </summary>
	public static Map Equality(Map arg) 
	{
		bool equal=true;
		for(int i=0;i+1<arg.Array.Count;i++)
		{
			if(!arg.Array[i].Equals(arg.Array[i+1]))
			{
				equal=false;
				break;
			}
		}
		return equal;
	}
}
public class boolean:MetaLibrary
{
	/// <summary>
	/// Returns the logical negation of its argument.
	/// </summary>
	public static Map Negation(Map arg)
	{
		Argument.Boolean(arg);
		return !arg.GetBoolean();
	}
	/// <summary>
	/// Returns the logical conjunction of all arguments.
	/// </summary>
	public static Map Conjunction(Map arg) 
	{
		Argument.BooleanArray(arg);
		bool and=true;
		foreach(Map map in arg.Array)
		{
			if(!map.GetBoolean())
			{
				and=false;
				break;
			}
		}
		return and;
	}
	/// <summary>
	/// Returns the logical disjunction of all arguments.
	/// </summary>
	public static Map Disjunction(Map arg) 
	{
		Argument.BooleanArray(arg);
		bool or=false;
		foreach(Map map in arg.Array)
		{
			if(map.GetBoolean())
			{
				or=true;
				break;
			}
		}
		return or;
	}
}
public class number:MetaLibrary
{
	/// <summary>
	/// Returns the sum of all arguments.
	/// </summary>
	public static Map Sum(Map arg)
	{
		Argument.IntegerArray(arg);
		Integer sum=0;
		foreach(Map map in arg.Array)
		{
			sum+=map.GetInteger();
		}
		return sum;
	}
	/// <summary>
	/// Returns the opposite number of its argument.
	/// </summary>
//	public static Map Opposite(Map arg)
//	{
//		Argument.Integer(arg);
//		return -arg.GetInteger();
//	}
	/// <summary>
	/// Returns the product of all arguments.
	/// </summary>
	public static Map Product(Map arg) 
	{
		Argument.IntegerArray(arg);
		Integer product=1;
		foreach(Map map in arg.Array)
		{
			product*=map.GetInteger();
		}
		return product;
	}
	/// <summary>
	/// Determines whether the first number is greater than the second number
	/// </summary>
	public static Map Greater(Map parameter)
	{
		Argument.IntegerArray(parameter);
		Argument.ExactArrayCount(parameter,2);
		return parameter[1].GetInteger()>parameter[2].GetInteger();
	}
	/// <summary>
	/// Determines whether the first number is smaller than the second number
	/// </summary>
	public static Map Smaller(Map parameter)
	{
		Argument.IntegerArray(parameter);
		Argument.ExactArrayCount(parameter,2);
		return parameter[1].GetInteger()<parameter[2].GetInteger();
	}
}

public class bitwise:MetaLibrary
{
	/// <summary>
	/// Returns the bitwise disjunction of all arguments.
	/// </summary>
	public static Map Disjunction(Map arg)
	{
		Argument.IntegerArray(arg);
		Integer or=0;
		foreach(Map map in arg.Array)
		{
			or|=map.GetInteger();
		}
		return or;
	}
}










// TODO: rename to Parameter
public class Argument
{
	public static void ContainsKey(Map map,Map key)
	{
		if(!map.ContainsKey(key))
		{
			throw new ApplicationException("Functions expects keyword argument "+Serialize.Value(key));
		}
	}
	public static void Integer(Map arg)
	{
		if(!arg.IsInteger)
		{
			throw new ApplicationException("arg is not an integer");
		}
	}
	public static void IntegerArray(Map arg)
	{
		foreach(Map map in arg.Array)
		{
			if(!map.IsInteger)
			{
				throw new ApplicationException("not all array elements in argument are integers");
			}
		}
	}
	public static void ExactArrayCount(Map parameter,int count)
	{
		if(parameter.Array.Count!=count)
		{
			throw new ApplicationException("did not pass array of length "+count.ToString()+" to function");
		}
	}
	public static void MinimalArrayCount(Map arg,int count)
	{
		if(arg.Array.Count<count)
		{
			throw new ApplicationException("count is too small");
		}
	}
	public static void Boolean(Map arg)
	{
		if(!arg.IsBoolean)
		{
			throw new ApplicationException("argument is not boolean");
		}
	}
	public static void BooleanArray(Map arg)
	{
		foreach(Map map in arg.Array)
		{
			if(!map.IsBoolean)
			{
				throw new ApplicationException("one of the argument array elements is not boolean");
			}
		}
	}

}
