using System;
using System.Collections;
using Meta.Types;
using Meta.Execution;
public class flow
{
	public static Map For() {
		Map arg=((Map)Interpreter.Arg);
		int times=(int)((Number)arg[new Number(1)]).IntValue();
		Map function=(Map)arg[new Number(2)];
		Map result=new Map();
		for(int i=0;i<times;i++) {
			Map argument=new Map();
<<<<<<< .mine
			argument[new Map("i")]=new Integer(i);
			result[new Integer(i+1)]=function.Call(argument);
=======
			argument[Interpreter.StringToMap("i")]=new Number(i);
			result[new Number(i+1)]=function.Call(argument);
>>>>>>> .r97
		}
		return result;
	}
	public static Map Foreach() {
		Map arg=((Map)Interpreter.Arg);
		Map over=(Map)arg[new Number(1)];
		Map function=(Map)arg[new Number(2)];
		Map result=new Map();
		int i=0;
		foreach(DictionaryEntry entry in over) {
			Map argument=new Map();
			argument[new Map("key")]=entry.Key;
			argument[new Map("value")]=entry.Value;
			result[new Number(i+1)]=function.Call(argument);
			i++;
		}
		return result;
	}
	public static void Switch() {
		Map arg=((Map)Interpreter.Arg);
		object val=arg[new Number(1)];
		Map cases=(Map)arg[new Map("case")];
		Map def=(Map)arg[new Map("default")];
		if(cases.ContainsKey(val)) {
			((Map)cases[val]).Call(new Map());
		}
		else if(def!=null) {
			def.Call(new Map());
		}				
	}
	public static void If() {
		Map arg=((Map)Interpreter.Arg);
		bool test=(bool)arg[new Number(1)];
		Map then=(Map)arg[new Map("then")];
		Map _else=(Map)arg[new Map("else")];
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
