using System;
using Meta.Types;
using Meta.Execution;
using System.Collections;
using System.IO;
public class @base {
	public static IKeyValue GetKeys(IKeyValue map) {
		int i=1;
		Map keys=new Map();
		foreach(DictionaryEntry entry in map) {
			keys[new Integer(i)]=entry.Key;
		}
		return keys;
	}
	public static object Catch(Map function) {
		try {
			function.Call((Map)Interpreter.callers[Interpreter.callers.Count-1]); // caller is wrong
		}
		catch(Exception e) {
			return e;
		}
		return ""; // really?
	}
	public static void SetBreakMethod(BreakMethodDelegate breakMethod) {
		Interpreter.breakMethod=breakMethod;
	}
	public static char Character(string c) {
		return Convert.ToChar(c);
	}
	public static string Insert(string original,int index,string insert) {
		return original.Insert(index,insert);
	}
	public static Map Concat(params Map[] maps) {
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
	public static object Run(string path) {
		return Interpreter.Run(new StreamReader(path),new Map());
	}
	public static object RunString(string code) {
		return Interpreter.Run(new StringReader(code),new Map());
	}
	public static IKeyValue Merge() {
		return (Map)Interpreter.Merge((Map)Interpreter.Arg);
	}
	public static void Write(string s) {
		Console.WriteLine(s);
	}
	public static string Read() {
		return Console.ReadLine();
	}
	public static bool And(bool a,bool b) {
		return a && b;
	}
	public static bool Or(bool a,bool b) {
		return a || b;
	}
	public static bool Not(bool a) {
		return !a;
	}
	public static Integer Add(Integer x,Integer y) {
		return x+y;
	}
	public static Integer Subtract(Integer x,Integer y) {
		return x-y;		
	}
	public static Integer Multiply(Integer x,Integer y) {
		return x*y;
	}
	public static Integer Divide(Integer x,Integer y) {
		return x/y;
	}
	public static bool Smaller(Integer x,Integer y) {
		return x<y;
	}
	public static bool Greater(Integer x,Integer y) {
		return x>y;
	}
	public static bool Equal(object a,object b) {
		return a.Equals(b);
	}
	public static Enum BinaryOr(params Enum[] enums) {
		int val=(int)Enum.Parse(enums[0].GetType(),enums[0].ToString());
		for(int i=1;i<enums.Length;i++) {
			int newVal=(int)Enum.Parse(enums[i].GetType(),enums[i].ToString());
			val|=newVal;
		}
		return (Enum)Enum.ToObject(enums[0].GetType(),val);
	}
	public static Map For() {
		Map arg=((Map)Interpreter.Arg);
		int times=(int)((Integer)arg[new Integer(1)]).IntValue();
		Map function=(Map)arg[new Integer(2)];
		Map result=new Map();
		for(int i=0;i<times;i++) {
			Map argument=new Map();
			argument["i"]=new Integer(i);
			Interpreter.arguments.Add(argument);
			result[new Integer(i+1)]=((IExpression)function.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
			Interpreter.arguments.Remove(argument);
		}
		return result;
	}
	public static void Load() {
		Map caller=(Map)Interpreter.callers[Interpreter.callers.Count-1];
		foreach(DictionaryEntry entry in Interpreter.LoadAssembly((Map)Interpreter.Arg,true)) {
			caller[entry.Key]=entry.Value;
		}
	}
	public static Map Each() {
		Map arg=((Map)Interpreter.Arg);
		Map over=(Map)arg[new Integer(1)];
		Map function=(Map)arg[new Integer(2)];
		Map result=new Map();
		int i=0;
		foreach(DictionaryEntry entry in over) {
			Map argument=new Map();
			argument["key"]=entry.Key;
			argument["value"]=entry.Value;
			Interpreter.arguments.Add(argument);
			result[new Integer(i+1)]=((IExpression)function.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
			Interpreter.arguments.Remove(argument);
			i++;
		}
		return result;
	}
	public static bool IsMap(object o) {
		return o is Map;
	}
	public static void Switch() {
		Map arg=((Map)Interpreter.Arg);
		object val=arg[new Integer(1)];
		Map cases=(Map)arg["case"];
		Map def=(Map)arg["default"];
		if(cases.ContainsKey(val)) {
			((IExpression)((Map)cases[val]).Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
		}
		else if(def!=null) {
			((IExpression)def.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
		}				
	}
	public static void If() {
		Map arg=((Map)Interpreter.Arg);
		bool test=(bool)arg[new Integer(1)];
		Map then=(Map)arg["then"];
		Map _else=(Map)arg["else"];
		if(test) {
			if(then!=null) {
				((IExpression)then.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
			}
		}
		else {
			if(_else!=null) {
				((IExpression)_else.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
			}
		}	
	}
}