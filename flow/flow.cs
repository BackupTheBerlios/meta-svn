using System;
using System.Collections;
using Meta.Types;
using Meta.Execution;
public class flow
{
	public static Map For() {
		Map arg=((Map)Interpreter.Arg);
		int times=(int)((Integer)arg[new Integer(1)]).IntValue();
		Map function=(Map)arg[new Integer(2)];
		Map result=new Map();
		for(int i=0;i<times;i++) {
			Map argument=new Map();
			argument[Interpreter.StringToMap("i")]=new Integer(i);
			result[new Integer(i+1)]=function.Call(argument);
		}
		return result;
	}
	public static Map Foreach() {
		Map arg=((Map)Interpreter.Arg);
		Map over=(Map)arg[new Integer(1)];
		Map function=(Map)arg[new Integer(2)];
		Map result=new Map();
		int i=0;
		foreach(DictionaryEntry entry in over) {
			Map argument=new Map();
			argument[Interpreter.StringToMap("key")]=entry.Key;
			argument[Interpreter.StringToMap("value")]=entry.Value;
			result[new Integer(i+1)]=function.Call(argument);
			i++;
		}
		return result;
	}
	public static void Switch() {
		Map arg=((Map)Interpreter.Arg);
		object val=arg[new Integer(1)];
		Map cases=(Map)arg[Interpreter.StringToMap("case")];
		Map def=(Map)arg[Interpreter.StringToMap("default")];
		if(cases.ContainsKey(val)) {
			((Map)cases[val]).Call(new Map());
		}
		else if(def!=null) {
			def.Call(new Map());
		}				
	}
	public static void If() {
		Map arg=((Map)Interpreter.Arg);
		bool test=(bool)arg[new Integer(1)];
		Map then=(Map)arg[Interpreter.StringToMap("then")];
		Map _else=(Map)arg[Interpreter.StringToMap("else")];
		if(test) {
			if(then!=null) {
				then.Call(new Map());
			}
		}
		else {
			if(_else!=null) {
				_else.Call(new Map());
			}
		}	
	}
	}
