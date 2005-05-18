//	Meta is a simple programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License
//	as published by the Free Software Foundation; either version 2
//	of the License, or (at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.



using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using antlr;
using antlr.collections;
using Meta.Execution;
using Meta.Types;
using Meta.Parser;
using Meta.TestingFramework;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Xml;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.GAC;
using System.Text;

namespace Meta {
	namespace Execution {
		public abstract class Expression {
			public abstract object Evaluate(IMap parent);
			Extent extent;
			public Extent Extent {
				get {
					return extent;
				}
				set {
					extent=value;
				}
			}
		}
//		public interface Expression {
//			object Evaluate(IMap parent);
//		}
		public class Statement {
			public static readonly Map keyString=new Map("key");
			public static readonly Map valueString=new Map("value");
			public void Realize(IMap parent) {
				keyExpression.Assign(parent,this.valueExpression.Evaluate(parent));
			}
			public Statement(Map code) {
				this.keyExpression=(Select)((Map)code[keyString]).Compile();
				this.valueExpression=(Expression)((Map)code[valueString]).Compile();
			}
			public Select keyExpression;
			public Expression valueExpression;
		}
		public class Call: Expression {
			public override object Evaluate(IMap parent) {
				return ((ICallable)callableExpression.Evaluate(parent)).Call(
					((IMap)argumentExpression.Evaluate(parent)).Clone());
			}
			public static readonly Map callString=new Map("call");
			public static readonly Map functionString=new Map("function");
			public static readonly Map argumentString=new Map("argument");
			public Call(Map obj) {
				Map expression=(Map)obj[callString];
				this.callableExpression=(Expression)((Map)expression[functionString]).Compile();
				this.argumentExpression=(Expression)((Map)expression[argumentString]).Compile();
			}
			public Expression argumentExpression;
			public Expression callableExpression;
		}
		public class Delayed: Expression {
			public override object Evaluate(IMap parent) {
				return delayed;
			}
			public static readonly Map delayedString=new Map("delayed");
			public Delayed(Map code) {
				this.delayed=(Map)code[delayedString];
			}
			public Map delayed;
		}
		public class Program: Expression {
			public override object Evaluate(IMap parent) {
				Map local=new Map();
				return Evaluate(parent,local);
				//				local.Parent=parent;
				//				Interpreter.callers.Add(local);
				//				for(int i=0;i<statements.Count;i++) {
				//					local=(Map)Interpreter.Current;
				//					((Statement)statements[i]).Realize(local);
				//				}
				//				object result=Interpreter.Current;
				//				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
				//				return result;
			}
			public object Evaluate(IMap parent,IMap local) {
				//Map local=new Map();
				local.Parent=parent;
				Interpreter.callers.Add(local);
				for(int i=0;i<statements.Count;i++) {
					local=(Map)Interpreter.Current;
					((Statement)statements[i]).Realize(local);
				}
				object result=Interpreter.Current;
				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
				return result;
			}
			//			public override object Evaluate(IMap parent) {
			//				Map local=new Map();
			//				local.Parent=parent;
			//				Interpreter.callers.Add(local);
			//				for(int i=0;i<statements.Count;i++) {
			//					local=(Map)Interpreter.Current;
			//					((Statement)statements[i]).Realize(local);
			//				}
			//				object result=Interpreter.Current;
			//				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
			//				return result;
			//			}
			public static readonly Map programString=new Map("program");
			public Program(Map code) {
				foreach(Map statement in ((Map)code[programString]).IntKeyValues) {
					this.statements.Add(statement.Compile()); // should we save the original maps instead of statements?
				}
			}
			public readonly ArrayList statements=new ArrayList();
		}
		public class Literal: Expression {
			public override object Evaluate(IMap parent) {
				return literal;
			}
			public static readonly Map literalString=new Map("literal");
			public Literal(Map code) {
				this.literal=Interpreter.RecognizeLiteralText((string)((Map)code[literalString]).GetDotNetString());
			}
			public object literal=null;
		}

//		public class Assign {
//		}
//		public class Search: Expression {
//			public readonly Map keyString=new Map("key");
//			protected Expression key;
//			public override object Evaluate(IMap parent) {
//			}
//			public Search(Map data) {
//				key=(Expression)((Map)data[keyString]).Compile();
//			}
//		}
//		public class Lookup: Expression {
//			protected Expression preselection;
//			protected Expression key;
//			public readonly Map keyString=new Map("key");
//			public override object Evaluate(IMap parent) {
//				Map pre=(Map)preselection.Evaluate(parent);
//				return pre[key.Evaluate(parent)];
//			}
//			public Lookup(Map data) {
//				preselection=(Expression)data.Compile(); // TODO: Take Statement out of Compile, integrate statements into Program compilation, since it is clear that they are there and only there and always (almost) there
//				key=(Expression)((Map)data[keyString]).Compile();
//			}
//			public object SearchAndSelectKeysInCurrentMap(ArrayList keys,bool isRightSide,bool isSelectLastKey) {
//				object selection=Interpreter.Current;
//				int i=0;
//
//				if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("this")) { // factor out IsString
//					i++;
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("caller")) {
//					int numCallers=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("caller")) {
//							numCallers++;
//							if(numCallers>Interpreter.callers.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("parent")) {
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("parent")) {
//							selection=((IMap)selection).Parent;
//							if(selection==null) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("arg")) {
//					int numArgs=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("arg")) {
//							numArgs++;
//							if(numArgs>Interpreter.arguments.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.arguments[Interpreter.arguments.Count-numArgs];
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")||isRightSide) {
//					if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")) { // IsString stupidly repeated
//						i++;
//					}
//					while(!((IKeyValue)selection).ContainsKey(keys[i])) {
//						selection=((IMap)selection).Parent;
//						if(selection==null) {
//							throw new KeyNotFoundException(keys[i]);
//						}
//					}
//				}
//				int lastKeySelect=0;
//				if(isSelectLastKey) {
//					lastKeySelect++;
//				}
//				for(;i<keys.Count-1+lastKeySelect;i++) {
//					if(selection is IKeyValue) {
//						selection=((IKeyValue)selection)[keys[i]];
//					}
//					else {
//						selection=new NetObject(selection)[keys[i]];
//					}
//					if(selection==null) {
//						throw new KeyDoesNotExistException(keys[i]);
//					}
//				}
//				return selection;
//			}
//		}
		public class Select: Expression { 
			public override object Evaluate(IMap parent) {
				ArrayList keysToBeSelected=new ArrayList();
				foreach(Expression expression in expressions) {
					keysToBeSelected.Add(expression.Evaluate(parent));
				}
				return SearchAndSelectKeysInCurrentMap(keysToBeSelected,true,true);
			}
			public void Assign(IMap current,object valueToBeAssigned) {  // TODO: Move somewhere else
				ArrayList keysToBeSelected=new ArrayList();
				foreach(Expression expression in expressions) {
					keysToBeSelected.Add(expression.Evaluate(current));
				}
				if(keysToBeSelected.Count==1 && keysToBeSelected[0] is Map && ((Map)keysToBeSelected[0]).IsString && ((Map)keysToBeSelected[0]).GetDotNetString().Equals("this")) {
					if(valueToBeAssigned is IMap) {
						IMap parent=((IMap)Interpreter.Current).Parent;
						Interpreter.Current=((IMap)valueToBeAssigned).Clone();
						((IMap)Interpreter.Current).Parent=parent;
					}
					else {
						Interpreter.Current=valueToBeAssigned;
					}
				}
				else {
					object selectionOfAllKeysExceptLastOne=SearchAndSelectKeysInCurrentMap(keysToBeSelected,false,false);
					IKeyValue mapToAssignIn;
					if(selectionOfAllKeysExceptLastOne is IKeyValue) {
						mapToAssignIn=(IKeyValue)selectionOfAllKeysExceptLastOne;
					}
					else {
						mapToAssignIn=new NetObject(selectionOfAllKeysExceptLastOne);
					}
					mapToAssignIn[keysToBeSelected[keysToBeSelected.Count-1]]=valueToBeAssigned;
				}
			}
			public object SearchAndSelectKeysInCurrentMap(ArrayList keys,bool isRightSide,bool isSelectLastKey) {
				object selection=Interpreter.Current;
				int i=0;

				if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("this")) { // factor out IsString
					i++;
				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("caller")) {
//					int numCallers=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("caller")) {
//							numCallers++;
//							if(numCallers>Interpreter.callers.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
//				}
				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("parent")) {
					foreach(object key in keys) {
						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("parent")) {
							selection=((IMap)selection).Parent;
							if(selection==null) {
								throw new KeyDoesNotExistException(keys[i]);
							}
							i++;
						}
						else {
							break;
						}
					}
				}
				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("arg")) {
//					int numArgs=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("arg")) {
//							numArgs++;
//							if(numArgs>Interpreter.arguments.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
					i++;
					selection=Interpreter.arguments[Interpreter.arguments.Count-1];
				}
					// TODO: remove isRightSide, move out
				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")||isRightSide) { 
					if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")) { // IsString stupidly repeated
						i++;
					}
					while(!((IKeyValue)selection).ContainsKey(keys[i])) {
						selection=((IMap)selection).Parent;
						if(selection==null) {
							throw new KeyNotFoundException(keys[i]);
						}
					}
				}
				int lastKeySelect=0;
				if(isSelectLastKey) {
					lastKeySelect++;
				}
				for(;i<keys.Count-1+lastKeySelect;i++) {
					if(selection is IKeyValue) {
						selection=((IKeyValue)selection)[keys[i]];
					}
					else {
						selection=new NetObject(selection)[keys[i]];
					}
					if(selection==null) {
						throw new KeyDoesNotExistException(keys[i]);
					}
				}
				return selection;
			}

			public static readonly Map selectString=new Map("select");
			public Select(Map code) {
				foreach(Map expression in ((Map)code[selectString]).IntKeyValues) {
					this.expressions.Add(expression.Compile());
				}
				// reverse because order of execution for subselect expressions has been reversed
				this.expressions.Reverse(); 
			}
			public readonly ArrayList expressions=new ArrayList();
			public ArrayList parents=new ArrayList();
		}
//		public class Select: Expression { 
//			public override object Evaluate(IMap parent) {
//				ArrayList keysToBeSelected=new ArrayList();
//				foreach(Expression expression in expressions) {
//					keysToBeSelected.Add(expression.Evaluate(parent));
//				}
//				return SearchAndSelectKeysInCurrentMap(keysToBeSelected,true,true);
//			}
//			// put this into Statement?
//			public void Assign(IMap current,object valueToBeAssigned) { 
//				ArrayList keysToBeSelected=new ArrayList();
//				foreach(Expression expression in expressions) {
//					keysToBeSelected.Add(expression.Evaluate(current));
//				}
//				if(keysToBeSelected.Count==1 && keysToBeSelected[0] is Map && ((Map)keysToBeSelected[0]).IsString && ((Map)keysToBeSelected[0]).GetDotNetString().Equals("this")) {
//					if(valueToBeAssigned is IMap) {
//						IMap parent=((IMap)Interpreter.Current).Parent;
//						Interpreter.Current=((IMap)valueToBeAssigned).Clone();
//						((IMap)Interpreter.Current).Parent=parent;
//					}
//					else {
//						Interpreter.Current=valueToBeAssigned;
//					}
//				}
//				else {
//					object selectionOfAllKeysExceptLastOne=SearchAndSelectKeysInCurrentMap(keysToBeSelected,false,false);
//					IKeyValue mapToAssignIn;
//					if(selectionOfAllKeysExceptLastOne is IKeyValue) {
//						mapToAssignIn=(IKeyValue)selectionOfAllKeysExceptLastOne;
//					}
//					else {
//						mapToAssignIn=new NetObject(selectionOfAllKeysExceptLastOne);
//					}
//					mapToAssignIn[keysToBeSelected[keysToBeSelected.Count-1]]=valueToBeAssigned;
//				}
//			}
//			public object SearchAndSelectKeysInCurrentMap(ArrayList keys,bool isRightSide,bool isSelectLastKey) {
//				object selection=Interpreter.Current;
//				int i=0;
//
//				if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("this")) { // factor out IsString
//					i++;
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("caller")) {
//					int numCallers=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("caller")) {
//							numCallers++;
//							if(numCallers>Interpreter.callers.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("parent")) {
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("parent")) {
//							selection=((IMap)selection).Parent;
//							if(selection==null) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("arg")) {
//					int numArgs=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("arg")) {
//							numArgs++;
//							if(numArgs>Interpreter.arguments.Count) {
//								throw new KeyDoesNotExistException(keys[i]);
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.arguments[Interpreter.arguments.Count-numArgs];
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")||isRightSide) {
//					if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")) { // IsString stupidly repeated
//						i++;
//					}
//					while(!((IKeyValue)selection).ContainsKey(keys[i])) {
//						selection=((IMap)selection).Parent;
//						if(selection==null) {
//							throw new KeyNotFoundException(keys[i]);
//						}
//					}
//				}
//				int lastKeySelect=0;
//				if(isSelectLastKey) {
//					lastKeySelect++;
//				}
//				for(;i<keys.Count-1+lastKeySelect;i++) {
//					if(selection is IKeyValue) {
//						selection=((IKeyValue)selection)[keys[i]];
//					}
//					else {
//						selection=new NetObject(selection)[keys[i]];
//					}
//					if(selection==null) {
//						throw new KeyDoesNotExistException(keys[i]);
//					}
//				}
//				return selection;
//			}
//
//			public static readonly Map selectString=new Map("select");
//			public Select(Map code) {
//				foreach(Map expression in ((Map)code[selectString]).IntKeyValues) {
//					this.expressions.Add(expression.Compile());
//				}
//				// reverse because order of execution for subselect expressions has been reversed
//				this.expressions.Reverse(); 
//			}
//			public readonly ArrayList expressions=new ArrayList();
//			public ArrayList parents=new ArrayList();
//		}
//		public interface Expression {
//			object Evaluate(IMap parent);
//		}
//		public class Statement {
//		public static readonly Map keyString=new Map("key");
//		public static readonly Map valueString=new Map("value");
//			public void Realize(IMap parent) {
//				keyExpression.Assign(parent,this.valueExpression.Evaluate(parent));
//			}
//			public Statement(Map code) {
//				this.keyExpression=(Select)((Map)code[keyString]).Compile();
//				this.valueExpression=(Expression)((Map)code[valueString]).Compile();
//			}
//			public Select keyExpression;
//			public Expression valueExpression;
//		}
//		public class Call: Expression {
//			public override object Evaluate(IMap parent) {
//				return ((ICallable)callableExpression.Evaluate(parent)).Call(
//					(IMap)argumentExpression.Evaluate(parent));
//			}
//			public static readonly Map callString=new Map("call");
//			public static readonly Map functionString=new Map("function");
//			public static readonly Map argumentString=new Map("argument");
//			public Call(Map obj) {
//				Map expression=(Map)obj[callString];
//				this.callableExpression=(Expression)((Map)expression[functionString]).Compile();
//				this.argumentExpression=(Expression)((Map)expression[argumentString]).Compile();
//			}
//			public Expression argumentExpression;
//			public Expression callableExpression;
//		}
//		public class Delayed: Expression {
//			public override object Evaluate(IMap parent) {
//				return delayed;
//			}
//			public static readonly Map delayedString=new Map("delayed");
//			public Delayed(Map code) {
//				this.delayed=(Map)code[delayedString];
//			}
//			public Map delayed;
//		}
//		public class Program: Expression {
//			public override object Evaluate(IMap parent) {
//				Map local=new Map();
//				return Evaluate(parent,local);
////				local.Parent=parent;
////				Interpreter.callers.Add(local);
////				for(int i=0;i<statements.Count;i++) {
////					local=(Map)Interpreter.Current;
////					((Statement)statements[i]).Realize(local);
////				}
////				object result=Interpreter.Current;
////				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
////				return result;
//			}
//			public override object Evaluate(IMap parent,IMap local) {
//				//Map local=new Map();
//				local.Parent=parent;
//				Interpreter.callers.Add(local);
//				for(int i=0;i<statements.Count;i++) {
//					local=(Map)Interpreter.Current;
//					((Statement)statements[i]).Realize(local);
//				}
//				object result=Interpreter.Current;
//				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
//				return result;
//			}
////			public override object Evaluate(IMap parent) {
////				Map local=new Map();
////				local.Parent=parent;
////				Interpreter.callers.Add(local);
////				for(int i=0;i<statements.Count;i++) {
////					local=(Map)Interpreter.Current;
////					((Statement)statements[i]).Realize(local);
////				}
////				object result=Interpreter.Current;
////				Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
////				return result;
////			}
//			public static readonly Map programString=new Map("program");
//			public Program(Map code) {
//				foreach(Map statement in ((Map)code[programString]).IntKeyValues) {
//					this.statements.Add(statement.Compile()); // should we save the original maps instead of statements?
//				}
//			}
//			public readonly ArrayList statements=new ArrayList();
//		}
//		public class Literal: Expression {
//			public override object Evaluate(IMap parent) {
//				return literal;
//			}
//			public static readonly Map literalString=new Map("literal");
//			public Literal(Map code) {
//				this.literal=Interpreter.RecognizeLiteralText((string)((Map)code[literalString]).GetDotNetString());
//			}
//			public object literal=null;
//		}
//		public class Select: Expression { 
//			public override object Evaluate(IMap parent) {
//				ArrayList keysToBeSelected=new ArrayList();
//				foreach(Expression expression in expressions) {
//					keysToBeSelected.Add(expression.Evaluate(parent));
//				}
//				return SearchAndSelectKeysInCurrentMap(keysToBeSelected,true,true);
//			}
//			public void Assign(IMap current,object valueToBeAssigned) { 
//				ArrayList keysToBeSelected=new ArrayList();
//				foreach(Expression expression in expressions) {
//					keysToBeSelected.Add(expression.Evaluate(current));
//				}
//				if(keysToBeSelected.Count==1 && keysToBeSelected[0] is Map && ((Map)keysToBeSelected[0]).IsString && ((Map)keysToBeSelected[0]).GetDotNetString().Equals("this")) {
//					if(valueToBeAssigned is IMap) {
//						IMap parent=((IMap)Interpreter.Current).Parent;
//						Interpreter.Current=((IMap)valueToBeAssigned).Clone();
//						((IMap)Interpreter.Current).Parent=parent;
//					}
//					else {
//						Interpreter.Current=valueToBeAssigned;
//					}
//				}
//				else {
//					object selectionOfAllKeysExceptLastOne=SearchAndSelectKeysInCurrentMap(keysToBeSelected,false,false);
//					IKeyValue mapToAssignIn;
//					if(selectionOfAllKeysExceptLastOne is IKeyValue) {
//						mapToAssignIn=(IKeyValue)selectionOfAllKeysExceptLastOne;
//					}
//					else {
//						mapToAssignIn=new NetObject(selectionOfAllKeysExceptLastOne);
//					}
//					mapToAssignIn[keysToBeSelected[keysToBeSelected.Count-1]]=valueToBeAssigned;
//				}
//			}
//			public object SearchAndSelectKeysInCurrentMap(ArrayList keys,bool isRightSide,bool isSelectLastKey) {
//				if(keys.Count==0) {
//					int asdf=0;
//				}
//				if(keys[0].Equals(new Map("asdf"))) {
//					int asdf=0;
//				}
//				object selection=Interpreter.Current;
//				int i=0;
//
//				if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("this")) { // factor out IsString
//					i++;
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("caller")) {
//					int numCallers=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("caller")) {
//							numCallers++;
//							if(numCallers>Interpreter.callers.Count) {
//								throw new ApplicationException(KeyErrorMessage(keys[i]));
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
//				}
////				else if(keys[0] is Map && ((Map)keys[0]).GetDotNetString().Equals("caller")) {
////					int numCallers=0;
////					foreach(object key in keys) {
////						if(key is Map && ((Map)key).GetDotNetString().Equals("caller")) {
////							numCallers++;
////							i++;
////						}
////						else {
////							break;
////						}
////					}
////					selection=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
////				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("parent")) {
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("parent")) {
//							selection=((IMap)selection).Parent;
//							if(selection==null) {
//								throw new ApplicationException(KeyErrorMessage(keys[i]));
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("arg")) {
//					int numArgs=0;
//					foreach(object key in keys) {
//						if(key is Map && ((Map)key).IsString && ((Map)key).GetDotNetString().Equals("arg")) {
//							numArgs++;
//							if(numArgs>Interpreter.arguments.Count) {
//								throw new ApplicationException(KeyErrorMessage(keys[i]));
//							}
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selection=Interpreter.arguments[Interpreter.arguments.Count-numArgs];
//				}
//				else if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")||isRightSide) {
//					if(keys[0] is Map && ((Map)keys[0]).IsString && ((Map)keys[0]).GetDotNetString().Equals("search")) { // IsString stupidly repeated
//						i++;
//					}
//					while(!((IKeyValue)selection).ContainsKey(keys[i])) {
//						selection=((IMap)selection).Parent;
//						if(selection==null) {
//							throw new ApplicationException(KeyErrorMessage(keys[i]));
//						}
//					}
//						//						string text="Key ";
//						//						if(keys[i] is Map) {
//						//							text+=((Map)keys[i]).GetDotNetString();
//						//						}
//						//						else {
//						//							text+=keys[i];
//						//						}
//						//						text+=" not found.";
//						//						throw new ApplicationException(text);
////					}
////					while(selection!=null && !((IKeyValue)selection).ContainsKey(keys[i])) {
////						selection=((IMap)selection).Parent;
////					}
////					if(selection==null) {
////						throw new ApplicationException(KeyErrorMessage(keys[i]));
//////						string text="Key ";
//////						if(keys[i] is Map) {
//////							text+=((Map)keys[i]).GetDotNetString();
//////						}
//////						else {
//////							text+=keys[i];
//////						}
//////						text+=" not found.";
//////						throw new ApplicationException(text);
////					}
//				}
//				int lastKeySelect=0;
//				if(isSelectLastKey) {
//					lastKeySelect++;
//				}
//				for(;i<keys.Count-1+lastKeySelect;i++) {
////					if(keys[i].Equals(new Map("Value"))) {
////						int asdf=0;
////					}
//					if(selection is IKeyValue) {
//						selection=((IKeyValue)selection)[keys[i]];
//					}
//					else {
//						selection=new NetObject(selection)[keys[i]];
//					}
//					if(selection==null) {
//						throw new ApplicationException(KeyErrorMessage(keys[i]));
//					}
//				}
//				return selection;
//			}
//			public static string KeyErrorMessage(object key) {
//				string text="Key ";
//				if(key is Map && ((Map)key).IsString) {
//					text+=((Map)key).GetDotNetString();
//				}
//				else {
//					text+=key;
//				}
//				text+=" not found.";
//				return text;
//			}
//			public static readonly Map selectString=new Map("select");
//			public Select(Map code) {
//				foreach(Map expression in ((Map)code[selectString]).IntKeyValues) {
//					this.expressions.Add(expression.Compile());
//				}
//			}
//			public readonly ArrayList expressions=new ArrayList();
//			public ArrayList parents=new ArrayList();
//		}
		public delegate void BreakMethodDelegate(IKeyValue obj);
		public class Interpreter  {
			public static void SaveToFile(object meta,string fileName) {
				StreamWriter writer=new StreamWriter(fileName);
				writer.Write(MetaSerialize(meta,"",true));
				writer.Close();
			}
			public static string MetaSerialize(object meta,string indent,bool isRightSide) {
				if(meta is Map) {
					string text="";
					Map map=(Map)meta;
					if(map.IsString) {
						text+="\""+(map).GetDotNetString()+"\"";
					}
					else if(map.Count==0) {
						text+="()";
					}
					else {
						if(!isRightSide) {
							text+="(";
							foreach(DictionaryEntry entry in map) {
								text+='['+MetaSerialize(entry.Key,indent,true)+']'+'='+MetaSerialize(entry.Value,indent,true)+",";
							}
							if(map.Count!=0) {
								text=text.Remove(text.Length-1,1);
							}
							text+=")";
						}
						else {
							foreach(DictionaryEntry entry in map) {
								text+=indent+'['+MetaSerialize(entry.Key,indent,false)+']'+'=';
								if(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString) {
									text+="\n";
								}
								text+=MetaSerialize(entry.Value,indent+'\t',true);
								if(!(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString)) {
									text+="\n";
								}
							}
						}
					}
					return text;
				}
				else if(meta is Integer) {
					Integer integer=(Integer)meta;
					return "\""+integer.ToString()+"\"";
				}
				else {
					throw new ApplicationException("Serialization not implemented for type "+meta.GetType().ToString()+".");
				}
			}
//			public static string MetaSerialize(object meta,string indent,bool isRightSide) {
//				if(meta is Map) {
//					string text="";
//					Map map=(Map)meta;
//					if(map.IsString) {
//						text+="'"+(map).GetDotNetString()+"'";
//					}
//					else if(map.Count==0) {
//						text+="()";
//					}
//					else {
//						if(!isRightSide) {
//							text+="(";
//							foreach(DictionaryEntry entry in map) {
//								text+='['+MetaSerialize(entry.Key,indent,true)+']'+'='+MetaSerialize(entry.Value,indent,true)+",";
//							}
//							if(map.Count!=0) {
//								text=text.Remove(text.Length-1,1);
//							}
//							text+=")";
//						}
//						else {
//							foreach(DictionaryEntry entry in map) {
//								text+=indent+'['+MetaSerialize(entry.Key,indent,false)+']'+'=';
//								if(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString) {
//									text+="\n";
//								}
//								text+=MetaSerialize(entry.Value,indent+"  ",true);
//								if(!(entry.Value is Map && ((Map)entry.Value).Count!=0 && !((Map)entry.Value).IsString)) {
//									text+="\n";
//								}
//							}
//						}
//					}
//					return text;
//				}
//				else if(meta is Integer) {
//					Integer integer=(Integer)meta;
//					return "'"+integer.ToString()+"'";
//				}
//				else {
//					throw new ApplicationException("Serialization not implemented for type "+meta.GetType().ToString()+".");
//				}
//			}
			public static IKeyValue Merge(params IKeyValue[] maps) {
				return MergeCollection(maps);
			}
			// really use IKeyValue?
			public static IKeyValue MergeCollection(ICollection maps) {
				Map result=new Map();//use clone here?
				foreach(IKeyValue map in maps) {
					foreach(DictionaryEntry entry in (IKeyValue)map) {
						if(entry.Value is IKeyValue && !(entry.Value is NetClass)&& result.ContainsKey(entry.Key) 
							&& result[entry.Key] is IKeyValue && !(result[entry.Key] is NetClass)) {
							result[entry.Key]=Merge((IKeyValue)result[entry.Key],(IKeyValue)entry.Value);
						}
						else {
							result[entry.Key]=entry.Value;
						}
					}
				}
				return result;
			}	
			public static object RecognizeLiteralText(string text) {
				for(int i=literalRecognitions.Count-1;i>=0;i--) {
					object recognized=((RecognizeLiteral)literalRecognitions[i]).Recognize(text);
					if(recognized!=null) {
						return recognized;
					}
				}
				return null;
			}
			public static object ConvertDotNetToMeta(object obj) { 
				if(obj==null) {
					return null;
				}
				else if(obj.GetType().IsSubclassOf(typeof(Enum))) {
					return new Integer((int)Convert.ToInt32((Enum)obj));
				}
				DotNetToMetaConversion conversion=(DotNetToMetaConversion)metaConversion[obj.GetType()];
				if(conversion==null) {
					return obj;
				}
				else {
					return conversion.Convert(obj);
				}
			}
			public static object ConvertMetaToDotNet(object obj) {
				if(obj is Integer) {
					return ((Integer)obj).Int;
				}
				else if(obj is Map && ((Map)obj).IsString) {
					return ((Map)obj).GetDotNetString();
				}
				else {
					return obj;
				}
			}
			public static object ConvertMetaToDotNet(object obj,Type targetType) {
				try {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)((Hashtable)
						Interpreter.netConversion[obj.GetType()])[targetType];
					return conversion.Convert(obj);
				}
				catch {
					return obj;
				}
			}
//			public static object Run(string fileName,IMap argument) {
//				StreamReader a=new StreamReader(fileName); //TODO: check this, is it right or wrong???ß
//				string x=a.ReadToEnd();
//				a.Close();
//				object result=Run(new StringReader(x),argument);
//				return result;
//			}
			public static object Run(string fileName,IMap argument) {
//				StreamReader reader=new StreamReader(fileName);
				Map program=CompileToMap(fileName);
				program.Parent=Library.library;
//				reader.Close();
				return program.Call(argument);
			}
//			public static object RunWithoutLibrary(string fileName,IMap argument) {
//				StreamReader reader=new StreamReader(fileName);
//				Map program=CompileToMap(reader);
//				reader.Close();
//				return program.Call(argument);
//			}
			public static object RunWithoutLibrary(string fileName,IMap argument) { // TODO: refactor, combine with Run
//				StreamReader reader=new StreamReader(fileName);
				Map program=CompileToMap(fileName);
//				reader.Close();
				return program.Call(argument);
			}
			//			public static Map CompileToMap(TextReader input) {
			//				return (new MetaTreeParser()).map(ParseToAst(input.ReadToEnd()));
			//			}
			public static Map CompileToMap(string fileName) {
				return (new MetaTreeParser()).map(ParseToAst(fileName));
			}
//			public static object Run(TextReader reader,IMap argument) {
//				Map lastProgram=CompileToMap(reader);
//				lastProgram.Parent=Library.library;
//				object result=lastProgram.Call(argument);
//				return result;
//			}

//			public static AST ParseToAst(string text)  {
//				TextReader stream=new StringReader(Environment.NewLine+text+Environment.NewLine);
//				MetaLexer lexer=new MetaLexer(stream);
//				lexer.setLine(0); // hack to compensate for the newline added in IndentationStream
//				Meta.Parser.MetaParser parser=new Meta.Parser.MetaParser( 
//					new IndentationStream(lexer));
//				//parser.getASTFactory().setASTNodeType("Meta.Parser.LineNumberAST");
//				parser.map();
//				return parser.getAST();
//			}
			public static AST ParseToAst(string fileName)  {
////		// the name of the file to read
////		System.String fileName = args[0];
////		
//		// construct the special shared input state that is needed
//		// in order to annotate MetaTokens properly
//		ExtentLexerSharedInputState lsis = new ExtentLexerSharedInputState(fileName);
//	// construct the lexer
//		AddLexer lex = new AddLexer(lsis);
//		
//		// tell the lexer the token class that we want
//		lex.setTokenObjectClass("ValueMetaToken");
//		
//		// construct the parser
//		AddParser par = new AddParser(lex);
//		
//		// tell the parser the AST class that we want
//		par.setASTNodeType("MetaAST");
//		
//		// construct the interpreter (which is a TreeParser)
//		AddInterpreter aint = new AddInterpreter();
//		
//		// parse the file
//		par.topLevel();
//		
//		// get the tree that resulted from parsing
//		AST ast = par.getAST();
//		
//		// interpret the tree
//		aint.topLevel(ast);	

				// TODO: Add the newlines here somewhere (or do this in IndentationStream?, somewhat easier and more logical maybe), but not possible, must be before lexer
				// construct the special shared input state that is needed
				// in order to annotate MetaTokens properly
				FileStream file=new FileStream(fileName,FileMode.Open);
				ExtentLexerSharedInputState lsis = new ExtentLexerSharedInputState(file,fileName); 
				// construct the lexer
				MetaLexer lex = new MetaLexer(lsis);
		
				// tell the lexer the token class that we want
				lex.setTokenObjectClass("MetaToken");
		
				// construct the parser
				MetaParser par = new MetaParser(new IndentationStream(lex));
				// tell the parser the AST class that we want
				par.setASTNodeClass("MetaAST");//
	//			par.getASTFactory().setASTNodeType("MetaAST");
		
//				// construct the interpreter (which is a TreeParser)
//				AddInterpreter aint = new AddInterpreter();
//		
//				// parse the file
//				par.topLevel();
//		
//				// get the tree that resulted from parsing
//				AST ast = par.getAST();
//		
//				// interpret the tree
//				aint.topLevel(ast);	

////				lexer.setLine(0); // hack to compensate for the newline added above
//				Meta.Parser.MetaParser parser=new Meta.Parser.MetaParser( 
//					new IndentationStream(lex));
				par.map();
				AST ast=par.getAST();
				file.Close();
				return ast;
//				StreamReader file=new StreamReader(fileName);
//				TextReader reader=new StringReader(Environment.NewLine+file.ReadToEnd()+Environment.NewLine);
//				file.Close();
//				MetaLexer lexer=new MetaLexer(reader);
//				lexer.setLine(0); // hack to compensate for the newline added above
//				Meta.Parser.MetaParser parser=new Meta.Parser.MetaParser( 
//					new IndentationStream(lexer));
//				parser.map();
//		
//	
//
//				return parser.getAST();
			}

			public static Map Arg {
				get {
					return (Map)arguments[arguments.Count-1];
				}
			}
			public static object Current {
				get {
					if(callers.Count==0) {
						return null;
					}
					return callers[callers.Count-1];
				}
				set {
					callers[callers.Count-1]=value;
				}
			}
			public static object ConvertMetaToDotNet(object metaObject,Type targetType,out bool isConverted) {
				if(targetType.IsSubclassOf(typeof(Enum)) && metaObject is Integer) { 
					isConverted=true;
					return Enum.ToObject(targetType,((Integer)metaObject).Int);
				}
				Hashtable toDotNet=(Hashtable)
					Interpreter.netConversion[targetType];
				if(toDotNet!=null) {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)toDotNet[metaObject.GetType()];
					if(conversion!=null) {
						isConverted=true;
						return conversion.Convert(metaObject);
					}
				}
				isConverted=false;
				return null;
			}
			static Interpreter() {
				Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
				metaInstallationPath=Directory.GetParent(metaAssembly.Location).Parent.Parent.Parent.FullName; 
				foreach(Type type in typeof(LiteralRecognitions).GetNestedTypes()) {
					literalRecognitions.Add((RecognizeLiteral)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(DotNetToMetaConversions).GetNestedTypes()) {
					DotNetToMetaConversion conversion=((DotNetToMetaConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
					metaConversion[conversion.source]=conversion;
				}
				foreach(Type type in typeof(MetaToDotNetConversions).GetNestedTypes()) {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{});
					if(!netConversion.ContainsKey(conversion.target)) {
						netConversion[conversion.target]=new Hashtable();
					}
					((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
				}
			}
			public static string metaInstallationPath;
			public static ArrayList callers=new ArrayList();
			public static ArrayList arguments=new ArrayList();
			public static Hashtable netConversion=new Hashtable();
			public static Hashtable metaConversion=new Hashtable();
			public static ArrayList compiledMaps=new ArrayList(); 
			public static ArrayList loadedAssemblies=new ArrayList();

			private static ArrayList literalRecognitions=new ArrayList();

			public abstract class RecognizeLiteral {
				public abstract object Recognize(string text); // Returns null if not recognized. Null cannot currently be created this way.
			}
			public abstract class MetaToDotNetConversion {
				public Type source;
				public Type target;
				public abstract object Convert(object obj);
			}
			public abstract class DotNetToMetaConversion {
				public Type source;
				public abstract object Convert(object obj);
			}
			public class LiteralRecognitions {
				// Attention! order of RecognizeLiteral classes matters
				public class RecognizeString:RecognizeLiteral {
					public override object Recognize(string text) {
						return new Map(text);
					}
				}
				// does everything get executed twice?
				public class RecognizeCharacter: RecognizeLiteral {
					public override object Recognize(string text) {
						if(text.StartsWith(@"\")) { // TODO: Choose another character for starting a character
							char result;
							if(text.Length==2) {
								result=text[1]; // not unicode safe, write wrapper that takes care of this stuff
							}
							else if(text.Length==3) {
								switch(text.Substring(1,2))  { // TODO: put this into Parser???
									case @"\'":
										result='\'';
										break;
									case @"\\":
										result='\\';
										break;
									case @"\a":
										result='\a';
										break;
									case @"\b":
										result='\b';
										break;
									case @"\f":
										result='\f';
										break;
									case @"\n":
										result='\n';
										break;
									case @"\r":
										result='\r';
										break;
									case @"\t":
										result='\t';
										break;
									case @"\v":
										result='\v';
										break;
									default:
										throw new RuntimeException("Unrecognized escape sequence "+text);
								}
							}
							else {
								return null;
							}
							return new Integer(result);
						}
						return null;
					}
				}
				public class RecognizeInteger: RecognizeLiteral  {
					public override object Recognize(string text)  { 
						if(text.Equals("")) {
							return null;
						}
						else {
							Integer number=new Integer(0);
							int i=0;
							if(text[0]=='-') {
								i++;
							}
							// TODO: the following is probably incorrect for multi-byte unicode
							// use StringInfo in the future instead
							for(;i<text.Length;i++) {
								if(char.IsDigit(text[i])) {
									number=number*10+(text[i]-'0');
								}
								else {
									return null;
								}
							}
							if(text[0]=='-') {
								number=-number;
							}
							return number;
						}
					}
				}
				public class RecognizeBoolean:RecognizeLiteral {
					public override object Recognize(string text) {
						switch(text) {
							case "true":
								return true;
							case "false":
								return false;
							default:
								return null;
						}
					}
				}
			}
			private abstract class MetaToDotNetConversions {
				/* These classes define the conversions that performed when a .NET method, field, or property
				 * is called/assigned to from Meta. */
				public class ConvertIntegerToByte: MetaToDotNetConversion {
					public ConvertIntegerToByte() {
						this.source=typeof(Integer);
						this.target=typeof(Byte);
					}
					public override object Convert(object obj) {
						return System.Convert.ToByte(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToSByte: MetaToDotNetConversion {
					public ConvertIntegerToSByte() {
						this.source=typeof(Integer);
						this.target=typeof(SByte);
					}
					public override object Convert(object obj) {
						return System.Convert.ToSByte(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToChar: MetaToDotNetConversion {
					public ConvertIntegerToChar() {
						this.source=typeof(Integer);
						this.target=typeof(Char);
					}
					public override object Convert(object obj) {
						return System.Convert.ToChar(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt32: MetaToDotNetConversion {
					public ConvertIntegerToInt32() {
						this.source=typeof(Integer);
						this.target=typeof(Int32);
					}
					public override object Convert(object obj) {
						return System.Convert.ToInt32(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt32: MetaToDotNetConversion {
					public ConvertIntegerToUInt32() {
						this.source=typeof(Integer);
						this.target=typeof(UInt32);
					}
					public override object Convert(object obj) {
						return System.Convert.ToUInt32(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt64: MetaToDotNetConversion {
					public ConvertIntegerToInt64() {
						this.source=typeof(Integer);
						this.target=typeof(Int64);
					}
					public override object Convert(object obj) {
						return System.Convert.ToInt64(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt64: MetaToDotNetConversion {
					public ConvertIntegerToUInt64() {
						this.source=typeof(Integer);
						this.target=typeof(UInt64);
					}
					public override object Convert(object obj) {
						return System.Convert.ToUInt64(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt16: MetaToDotNetConversion {
					public ConvertIntegerToInt16() {
						this.source=typeof(Integer);
						this.target=typeof(Int16);
					}
					public override object Convert(object obj) {
						return System.Convert.ToInt16(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt16: MetaToDotNetConversion {
					public ConvertIntegerToUInt16() {
						this.source=typeof(Integer);
						this.target=typeof(UInt16);
					}
					public override object Convert(object obj) {
						return System.Convert.ToUInt16(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToDecimal: MetaToDotNetConversion {
					public ConvertIntegerToDecimal() {
						this.source=typeof(Integer);
						this.target=typeof(decimal);
					}
					public override object Convert(object obj) {
						return (decimal)(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToDouble: MetaToDotNetConversion {
					public ConvertIntegerToDouble() {
						this.source=typeof(Integer);
						this.target=typeof(double);
					}
					public override object Convert(object obj) {
						return (double)(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToFloat: MetaToDotNetConversion {
					public ConvertIntegerToFloat() {
						this.source=typeof(Integer);
						this.target=typeof(float);
					}
					public override object Convert(object obj) {
						return (float)(((Integer)obj).LongValue());
					}
				}
				public class ConvertMapToString: MetaToDotNetConversion {
					public ConvertMapToString() {
						this.source=typeof(Map);
						this.target=typeof(string);
					}
					public override object Convert(object obj) {
						return ((Map)obj).GetDotNetString();
					}
				}
			}
			private abstract class DotNetToMetaConversions {
				/* These classes define the conversions that are performed on return values of .NET methods,
				 * properties and fields that are called/accessed from Meta. */
				public class ConvertStringToMap: DotNetToMetaConversion {
					public ConvertStringToMap()   {
						this.source=typeof(string);
					}
					public override object Convert(object obj) {
						return new Map((string)obj);
					}
				}
				public class ConvertByteToInteger: DotNetToMetaConversion {
					public ConvertByteToInteger() {
						this.source=typeof(Byte);
					}
					public override object Convert(object obj) {
						return new Integer((Byte)obj);
					}
				}
				public class ConvertSByteToInteger: DotNetToMetaConversion {
					public ConvertSByteToInteger() {
						this.source=typeof(SByte);
					}
					public override object Convert(object obj) {
						return new Integer((SByte)obj);
					}
				}
				public class ConvertCharToInteger: DotNetToMetaConversion {
					public ConvertCharToInteger() {
						this.source=typeof(Char);
					}
					public override object Convert(object obj) {
						return new Integer((Char)obj);
					}
				}
				public class ConvertInt32ToInteger: DotNetToMetaConversion {
					public ConvertInt32ToInteger() {
						this.source=typeof(Int32);
					}
					public override object Convert(object obj) {
						return new Integer((Int32)obj);
					}
				}
				public class ConvertUInt32ToInteger: DotNetToMetaConversion {
					public ConvertUInt32ToInteger() {
						this.source=typeof(UInt32);
					}
					public override object Convert(object obj) {
						return new Integer((UInt32)obj);
					}
				}
				public class ConvertInt64ToInteger: DotNetToMetaConversion {
					public ConvertInt64ToInteger() {
						this.source=typeof(Int64);
					}
					public override object Convert(object obj) {
						return new Integer((Int64)obj);
					}
				}
				public class ConvertUInt64ToInteger: DotNetToMetaConversion {
					public ConvertUInt64ToInteger() {
						this.source=typeof(UInt64);
					}
					public override object Convert(object obj) {
						return new Integer((Int64)(UInt64)obj);
					}
				}
				public class ConvertInt16ToInteger: DotNetToMetaConversion {
					public ConvertInt16ToInteger() {
						this.source=typeof(Int16);
					}
					public override object Convert(object obj) {
						return new Integer((Int16)obj);
					}
				}
				public class ConvertUInt16ToInteger: DotNetToMetaConversion {
					public ConvertUInt16ToInteger() {
						this.source=typeof(UInt16);
					}
					public override object Convert(object obj) {
						return new Integer((UInt16)obj);
					}
				}
			}
		}
		/* Base class of exceptions in Meta. */
		public abstract class MetaException:ApplicationException {
			protected string message="";
			public MetaException() {
			}
			public MetaException(string message) {
				this.message=message;
			}
			public override string Message {
				get {
					return message;
				}
			}
		}
		public class RuntimeException:MetaException {
			public RuntimeException(string message):base(message) {
			}
		}
//		public class RuntimeException:MetaException {
//			string message;
//			string fileName;
//			int line;
//			int column;
//			public RuntimeException(string message,string fileName, int line,int column) {
//				this.message=message;
//				this.fileName=fileName;
//				this.line=line;
//				this.column=column;
//			}
//		}

		/* Base class for key exceptions. */
		public abstract class KeyException:MetaException {
			public KeyException(object key) {
				message="Key ";
				if(key is Map && ((Map)key).IsString) {
					message+=((Map)key).GetDotNetString();
				}
				else {
					message+=key;
				}
				message+=" not found.";
			}
		}
		/* Thrown when a searched key was not found. */
		public class KeyNotFoundException:KeyException {
			public KeyNotFoundException(object key):base(key) {
			}
		}
		/* Thrown when an accessed key does not exist. */
		public class KeyDoesNotExistException:KeyException {
			public KeyDoesNotExistException(object key):base(key) {
			}
		}
	}
	namespace Types  {
		/* Everything implementing this interface can be used in a Call expression */
		public interface ICallable {
			object Call(IMap argument);
		}
		public interface IMap: IKeyValue {
			IMap Parent {
				get;
				set;
			}
			ArrayList IntKeyValues {
				get;
			}
			IMap Clone();
		}
		// TODO: Does the IKeyValue<->IMap distinction make sense?
		public interface IKeyValue: IEnumerable {
			object this[object key] {
				get;
				set;
			}
			ArrayList Keys {
				get;
			}
			int Count {
				get;
			}
			bool ContainsKey(object key);			
		}		
		/* Represents a lazily evaluated "library" Meta file. */
		public class MetaLibrary { // TODO: Put this into Library class, make base class for everything that gets loaded
			public object Load() {
				return Interpreter.Run(path,new Map()); // TODO: Improve this interface, isn't read lazily anyway
			}
//			public object Load() {
//				StreamReader reader=new StreamReader(path); 
//				object result=Interpreter.Run(reader,new Map()); // TODO: Improve this interface, isn't read lazily anyway
//				reader.Close();
//				return result;
//			}
			public MetaLibrary(string path) {
				this.path=path;
			}
			string path;
		}
		/* Represents a lazily loaded .NET namespace. */
		public class LazyNamespace: IKeyValue { // TODO: Put this into library, combine with MetaLibrary
			public object this[object key] {
				get {
					if(cache==null) {
						Load();
					}
					return cache[key];
				}
				set {
					throw new RuntimeException("Cannot set key "+key.ToString()+" in .NET namespace.");
				}
			}
			public ArrayList Keys {
				get {
					if(cache==null) {
						Load();
					}
					return cache.Keys;
				}
			}
			public int Count {
				get {
					if(cache==null) {
						Load();
					}
					return cache.Count;
				}
			}
			public string fullName;
			public ArrayList cachedAssemblies=new ArrayList();
			public Hashtable namespaces=new Hashtable();
			public LazyNamespace(string fullName) {
				this.fullName=fullName;
			}
			public void Load() {
				cache=new Map();
				foreach(CachedAssembly cachedAssembly in cachedAssemblies) {
					cache=(Map)Interpreter.Merge(cache,cachedAssembly.GetNamespaceContents(fullName));
				}
				foreach(DictionaryEntry entry in namespaces) {
					cache[new Map((string)entry.Key)]=entry.Value;
				}
			}
			public Map cache;
			public bool ContainsKey(object key) {
				if(cache==null) {
					Load();
				}
				return cache.ContainsKey(key);
			}
			public IEnumerator GetEnumerator() {
				if(cache==null) {
					Load();
				}
				return cache.GetEnumerator();
			}
		}
		/* TODO: What's this for? */
		public class CachedAssembly {  // TODO: Put this into Library class
			private Assembly assembly;
			public CachedAssembly(Assembly assembly) {
				this.assembly=assembly;
			}
			public Map GetNamespaceContents(string fullName) {
				if(map==null) {
					map=Library.LoadAssemblies(new object[] {assembly});
				}
				Map selected=map;
				if(fullName!="") {
					foreach(string name in fullName.Split('.')) {
						selected=(Map)selected[new Map(name)];
					}
				}
				return selected;
			}			
			private Map map;
		}
		/* The library namespace, containing both Meta libraries as well as .NET libraries
		 *  from the "library" path and the GAC. */
		public class Library: IKeyValue,IMap {
			public object this[object key] {
				get {
					if(cash.ContainsKey(key)) {
						if(cash[key] is MetaLibrary) {
							cash[key]=((MetaLibrary)cash[key]).Load();
						}
						return cash[key];
					}
					else {
						return null;
					}
				}
				set {
					throw new RuntimeException("Cannot set key "+key.ToString()+" in library.");
				}
			}
			public ArrayList Keys {
				get {
					return cash.Keys;
				}
			}
			public IMap Clone() {
				return this;
			}
			public int Count {
				get {
					return cash.Count;
				}
			}
			public bool ContainsKey(object key) {
				return cash.ContainsKey(key);
			}
			public ArrayList IntKeyValues {
				get {
					return new ArrayList();
				}
			}
			public IMap Parent {
				get {
					return null;
				}
				set {
					throw new RuntimeException("Cannot set parent of library.");
				}
			}
			public IEnumerator GetEnumerator() { 
				foreach(DictionaryEntry entry in cash) { // TODO: create separate enumerator for efficiency?
					object o=cash[entry.Key];				  // or remove IEnumerable from IMap (only needed for foreach)
				}														// decide later
				return cash.GetEnumerator();
			}
			public static Map LoadAssemblies(IEnumerable assemblies) {
				Map root=new Map();
				foreach(Assembly assembly in assemblies) {
					foreach(Type type in assembly.GetExportedTypes())  {
						if(type.DeclaringType==null)  {
							Map position=root;
							ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
							subPaths.RemoveAt(subPaths.Count-1);
							foreach(string subPath in subPaths)  {
								if(!position.ContainsKey(new Map(subPath)))  {
									position[new Map(subPath)]=new Map();
								}
								position=(Map)position[new Map(subPath)];
							}
							position[new Map(type.Name)]=new NetClass(type);
						}
					}
					Interpreter.loadedAssemblies.Add(assembly.Location);
				}
				return root;
			}
			private static AssemblyName GetAssemblyName(IAssemblyName nameRef) {
				AssemblyName name = new AssemblyName();
				name.Name = AssemblyCache.GetName(nameRef);
				name.Version = AssemblyCache.GetVersion(nameRef);
				name.CultureInfo = AssemblyCache.GetCulture(nameRef);
				name.SetPublicKeyToken(AssemblyCache.GetPublicKeyToken(nameRef));
				return name;
			}
			public Library() {
				ArrayList assemblies=new ArrayList();
				libraryPath=Path.Combine(Interpreter.metaInstallationPath,"library");
				IAssemblyEnum ae=AssemblyCache.CreateGACEnum();
				IAssemblyName an; 
				AssemblyName name;
				assemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
				while (AssemblyCache.GetNextAssembly(ae, out an) == 0) {
					try {
						name=GetAssemblyName(an);
						assemblies.Add(Assembly.LoadWithPartialName(name.Name));
					}
					catch(Exception e) {
						//Console.WriteLine("Could not load gac assembly :"+System.GAC.AssemblyCache.GetName(an));
					}
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.dll")) {
					assemblies.Add(Assembly.LoadFrom(fileName));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.exe")) {
					assemblies.Add(Assembly.LoadFrom(fileName));
				}
				string infoFileName=Path.Combine(Interpreter.metaInstallationPath,"assemblyInfo.meta"); // TODO: Use another name that doesn't collide with C# meaning
				if(File.Exists(infoFileName)) {
					assemblyInfo=(Map)Interpreter.RunWithoutLibrary(infoFileName,new Map());
				}
				
				cash=LoadNamespaces(assemblies);
				Interpreter.SaveToFile(assemblyInfo,infoFileName);
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.meta")) {
					cash[new Map(Path.GetFileNameWithoutExtension(fileName))]=new MetaLibrary(fileName);
				}
			}
			private Map assemblyInfo=new Map();
			public ArrayList GetNamespaces(Assembly assembly) { //refactor, integrate into LoadNamespaces???
				ArrayList namespaces=new ArrayList();
				if(assemblyInfo.ContainsKey(new Map(assembly.Location))) {
					Map info=(Map)assemblyInfo[new Map(assembly.Location)];
					string timestamp=((Map)info[new Map("timestamp")]).GetDotNetString();
					if(timestamp.Equals(File.GetCreationTime(assembly.Location).ToString())) {
						Map names=(Map)info[new Map("namespaces")];
						foreach(DictionaryEntry entry in names) {
							string text=((Map)entry.Value).GetDotNetString();
							namespaces.Add(text);
						}
						return namespaces;
					}
				}
				foreach(Type type in assembly.GetExportedTypes()) {
					if(!namespaces.Contains(type.Namespace)) {
						if(type.Namespace==null) {
							if(!namespaces.Contains("")) {
								namespaces.Add("");
							}
						}
						else {
							namespaces.Add(type.Namespace);
						}
					}
				}
				Map assemblyInfoMap=new Map();
				Map nameSpaceMap=new Map();
				Integer counter=new Integer(0);
				foreach(string na in namespaces) {
					nameSpaceMap[counter]=new Map(na);
					counter++;
				}
				assemblyInfoMap[new Map("namespaces")]=nameSpaceMap;
				assemblyInfoMap[new Map("timestamp")]=new Map(
					File.GetCreationTime(assembly.Location).ToString());
				assemblyInfo[new Map(assembly.Location)]=assemblyInfoMap;
				return namespaces;
			}
			public Map LoadNamespaces(ArrayList assemblies) {
				LazyNamespace root=new LazyNamespace("");
				foreach(Assembly assembly in assemblies) {
					ArrayList names=GetNamespaces(assembly);
					CachedAssembly cachedAssembly=new CachedAssembly(assembly);
					foreach(string name in names) {
						LazyNamespace selected=root;
						if(name=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.metaInstallationPath,"library"))) {
							continue;
						}
						if(name!="") {
							foreach(string subpath in name.Split('.')) {
								if(!selected.namespaces.ContainsKey(subpath)) {
									string fullName=selected.fullName;
									if(fullName!="") {
										fullName+=".";
									}
									fullName+=subpath;
									selected.namespaces[subpath]=new LazyNamespace(fullName);
								}
								selected=(LazyNamespace)selected.namespaces[subpath];
							}
						}
						selected.cachedAssemblies.Add(cachedAssembly);
					}
				}
				
				root.Load();
				return root.cache;
			}
			public static Library library=new Library();
			private Map cash=new Map();
			public static string libraryPath="library"; 
		}
		/* Adapts the Meta keys of a Map to their .NET counterparts, useful when writing libraries. */
		public class MapAdapter { // TODO: Make this a whole IMap implementation?, if seems useful
			Map map;
			public MapAdapter(Map map) {
				this.map=map;
			}
			public MapAdapter() {
				this.map=new Map();
			}
			public object this[object key] {
				get {
					return Interpreter.ConvertMetaToDotNet(map[Interpreter.ConvertDotNetToMeta(key)]);
				}
				set {
					this.map[Interpreter.ConvertDotNetToMeta(key)]=Interpreter.ConvertDotNetToMeta(value);
				}
			}
		}

		//TODO: cache the IntKeyValues somewhere; put in an "Add" method
		public class Map: IKeyValue, IMap, ICallable, IEnumerable, ISerializeSpecial {
//			public Map Caller {
//				get {
//					return caller;
//				}
//				set {
//					caller=value;
//				}
//			}
//			Map caller=null;
//			public Map Argument {
//				get {
//					return arg;
//				}
//				set { // TODO: Remove set, maybe?
//					arg=caller;
//				}
//			}
//			Map arg=null;
			public bool IsString {
				get {
					return table.IsString;
				}
			}
			public string GetDotNetString() {
				return table.GetDotNetString();
			}
			public IMap Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			public int Count {
				get {
					return table.Count;
				}
			}
			public ArrayList IntKeyValues {
				get {
					return table.IntKeyValues;
				}
			}
			public virtual object this[object key]  {
				get {
					return table[key];
				}
				set {
					if(value!=null) {
						isHashCashed=false;
						object val=value is IMap? ((IMap)value).Clone(): value;
						if(value is IMap) {
							((IMap)val).Parent=this;
						}
						table[key]=val;
					}
				}
			}
			public object Execute() { // TODO: Rename to evaluate
				Expression function=(Expression)Compile();
				object result;
				result=function.Evaluate(this);
				return result;
			}
			public object Call(IMap argument) {
				Expression function=(Expression)Compile();
				object result;
				Interpreter.arguments.Add(argument);
				result=function.Evaluate(this);
				Interpreter.arguments.RemoveAt(Interpreter.arguments.Count-1);
				return result;
			}
			public ArrayList Keys {
				get {
					return table.Keys;
				}
			}
			public IMap Clone() {
				Map clone=table.CloneMap();
				clone.Parent=Parent;
				clone.compiled=compiled;
				clone.Extent=Extent;
				return clone;
			}
			public object Compile()  { // TODO:Split this into CompileStatement and CompileExpression

				if(compiled==null)  {
					if(this.ContainsKey(new Map("call")))
						compiled=new Call(this);
					else if(this.ContainsKey(new Map("delayed")))
						compiled=new Delayed(this);
					else if(this.ContainsKey(new Map("program")))
						compiled=new Program(this);
					else if(this.ContainsKey(new Map("literal")))
						compiled=new Literal(this);
					else if(this.ContainsKey(new Map("select")))
						compiled=new Select(this);
					else if(this.ContainsKey(new Map("value")) && this.ContainsKey(new Map("key")))
						compiled=new Statement(this);
					else
						throw new RuntimeException("Cannot compile non-code map.");
				}
				if(this.Extent!=null) {
					int asdf=0;
				}		
				if(compiled is Expression) {
					((Expression)compiled).Extent=this.Extent;
				}
				return compiled;
			}
			public bool ContainsKey(object key)  {
				return table.ContainsKey(key);
			}
			public override bool Equals(object obj) {
				if(Object.ReferenceEquals(obj,this)) {
					return true;
				}
				if(!(obj is Map)) {
					return false;
				}
				return ((Map)obj).table.Equal(table);
			}
			public IEnumerator GetEnumerator() {
				return new MapEnumerator(this);
			}
			public override int GetHashCode()  {
				if(!isHashCashed) {
					hash=this.table.GetHashCode();
					isHashCashed=true;
				}
				return hash;
			}
			private bool isHashCashed=false;
			private int hash;
//			int line=0;
//			public int Line {
//				get {
//					return line;
//				}
//				set {
//					line=value;
//				}
//			}

			Extent extent;
			public Extent Extent {
				get {
					return extent;
				}
				set {
					extent=value;
				}
			}
			/* TODO: Move some more logic into constructor instead of in Parser?
			 * There is no clean separation then. But there isn't anyway. I could make 
			 * it so that only the extent gets passed, that's probably best*/
			public Map(string text):this(new StringStrategy(text)) {
			}
			public Map(MapStrategy table) {
				this.table=table;
				this.table.map=this;
			}
			public Map():this(new HybridDictionaryStrategy()) {
			}
			private IMap parent;
			private MapStrategy table;
			public object compiled;
			public string Serialize(string indent,string[] functions) {
				if(this.IsString) {
					return indent+"\""+this.GetDotNetString()+"\""+"\n";
				}
				else {
					return null;
				}
			}
			public abstract class MapStrategy {
				public Map map;
				public MapStrategy Clone() {
					MapStrategy strategy=new HybridDictionaryStrategy();
					foreach(object key in this.Keys) {
						strategy[key]=this[key];
					}
					return strategy;	
				}
				public abstract Map CloneMap();
				public abstract ArrayList IntKeyValues {
					get;
				}
				public abstract bool IsString {
					get;
				}
				
				// TODO: Rename. Reason: This really means something more abstract, more along the lines of,
				// "is this a map that only has integers as children, and maybe also only integers as keys?"
				public abstract string GetDotNetString();
				public abstract ArrayList Keys {
					get;
				}
				public abstract int Count {
					get;
				}
				public abstract object this[object key]  {
					get;
					set;
				}

				public abstract bool ContainsKey(object key);
				/* Hashcodes must be exactly the same in all MapStrategies. */
				public override int GetHashCode()  {
					int h=0;
					foreach(object key in this.Keys) {
						unchecked {
							h+=key.GetHashCode()*this[key].GetHashCode();
						}
					}
					return h;
				}
				public virtual bool Equal(MapStrategy obj) {
					if(Object.ReferenceEquals(obj,this)) { // check whether this is a clone of the other MapStrategy (not used yet)
						return true;
					}
					if(obj.Count!=this.Count) {
						return false;
					}
					foreach(object key in this.Keys)  {
						if(!obj.ContainsKey(key)||!obj[key].Equals(this[key])) {
							return false;
						}
					}
					return true;
				}
			}
			// TODO: Make this unicode safe:
			public class StringStrategy:MapStrategy {
				// is this really identical with the other strategies? See Hashcode of Integer class to make sure
				public override int GetHashCode() {
					int hash=0;
					for(int i=0;i<text.Length;i++) {//(char c in this.text) {
						hash+=(i+1)*text[i];
					}
					return hash;
				}
				public override bool Equal(MapStrategy obj) {
					if(obj is StringStrategy) {	// TODO: Decide on single exit for methods, might be useful, especially here
						return ((StringStrategy)obj).text.Equals(this.text);
					}
					else {
						return base.Equal(obj);
					}
				}
				public override Map CloneMap() {
					return new Map(new StringStrategy(this));
				}
				public override ArrayList IntKeyValues {
					get {
						ArrayList list=new ArrayList();
						foreach(char c in text) {
							list.Add(new Integer(c));
						}
						return list;
					}
				}
				public override bool IsString {
					get {
						return true;
					}
				}
				public override string GetDotNetString() {
					return text;
				}
				public override ArrayList Keys {
					get {
						return keys;
					}
				}
				private ArrayList keys=new ArrayList();
				private string text;
				public StringStrategy(StringStrategy clone) {
					this.text=clone.text;
					this.keys=(ArrayList)clone.keys.Clone();
				}
				public StringStrategy(string text) {
					this.text=text;
					for(int i=1;i<=text.Length;i++) { // make this lazy? it won't work with unicode anymore then, though
						keys.Add(new Integer(i));			// TODO: Make this unicode-safe in the first place!
					}
				}
				public override int Count {
					get {
						return text.Length;
					}
				}
				public override object this[object key]  {
					get {
						if(key is Integer) {
							int i=((Integer)key).Int;
							if(i>0 && i<=this.Count) {
								return new Integer(text[i-1]);
							}
						}
						return null;
					}
					set {
						/* StringStrategy gets changed. Fall back on standard strategy because we can't be sure
						 * the map will still be a string afterwards. */
						map.table=this.Clone();
						map.table[key]=value;
					}
				}
				public override bool ContainsKey(object key)  {
					if(key is Integer) {
						return ((Integer)key)>0 && ((Integer)key)<=this.Count;
					}
					else {
						return false;
					}
				}
			}
			/* The standard strategy for maps. */
			public class HybridDictionaryStrategy:MapStrategy {
				ArrayList keys;
				private HybridDictionary table;
				public HybridDictionaryStrategy():this(2) {
				}
				public HybridDictionaryStrategy(int count) {
					this.keys=new ArrayList(count);
					this.table=new HybridDictionary(count);
				}
				public override Map CloneMap() {
					Map clone=new Map(new HybridDictionaryStrategy(this.keys.Count));
					foreach(object key in keys) {
						clone[key]=table[key];
					}
					return clone;
				}
				public override ArrayList IntKeyValues {
					get {
						ArrayList list=new ArrayList();
						for(Integer i=new Integer(1);ContainsKey(i);i++) {
							list.Add(this[i]);
						}
						return list;
					}
				}
				public override bool IsString {
					get {
						if(IntKeyValues.Count>0) {
							try {
								GetDotNetString();// TODO: a bit of a hack
								return true;
							}
							catch{
							}
						}
						return false;
					}
				}
				public override string GetDotNetString() { // TODO: looks too complicated
					string text="";
					foreach(object key in this.Keys) {
						if(key is Integer && this.table[key] is Integer) {
							try {
								text+=Convert.ToChar(((Integer)this.table[key]).Int);
							}
							catch {
								throw new MapException(this.map,"Map is not a string");
							}
						}
						else {
							throw new MapException(this.map,"Map is not a string");
						}
					}
					return text;
				}
				public class MapException:ApplicationException { // TODO: Remove or make sense of this
					Map map;
					public MapException(Map map,string message):base(message) {
						this.map=map;
					}
				}
				public override ArrayList Keys {
					get {
						return keys;
					}
				}
				public override int Count {
					get {
						return table.Count;
					}
				}
				public override object this[object key]  {
					get {
						return table[key];
					}
					set {
						if(!this.ContainsKey(key)) {
							keys.Add(key);
						}
						table[key]=value;
					}
				}
				public override bool ContainsKey(object key)  {
					return table.Contains(key);
				}
			}
		}
		public class MapEnumerator: IEnumerator {
			private Map map; public MapEnumerator(Map map) {
				this.map=map;
			}
			public object Current {
				get {
					return new DictionaryEntry(map.Keys[index],map[map.Keys[index]]);
				}
			}
			public bool MoveNext() {
				index++;
				return index<map.Count;
			}
			public void Reset() {
				index=-1;
			}
			private int index=-1;
		}
		public delegate object DelegateCreatedForGenericDelegates();
		[AttributeUsage(AttributeTargets.Method)]
		public class MetaLibraryMethodAttribute:Attribute {
		}
		public class NetMethod: ICallable {
			public bool isMetaLibraryMethod=false;
			// TODO: Move this to "With" ? Move this to NetContainer?
			public static object DoModifiableCollectionAssignment(Map map,object oldValue,out bool assigned) {

				if(map.IntKeyValues.Count==0) {
					assigned=false;
					return null;
				}
				Type type=oldValue.GetType();
				MethodInfo method=type.GetMethod("Add",new Type[]{map.IntKeyValues[0].GetType()});
				if(method!=null) {
					foreach(object val in map.IntKeyValues) { // combine this with Library function "Init"
						method.Invoke(oldValue,new object[]{val});//  call method from above!
					}
					assigned=true;
				}
				else {
					assigned=false;
				}

				return oldValue;
			}
			public static object ConvertParameter(object meta,Type parameter,out bool converted) {
				converted=true;
				if(parameter.IsAssignableFrom(meta.GetType())) {
					return meta;
				}
				else if((parameter.IsSubclassOf(typeof(Delegate))
					||parameter.Equals(typeof(Delegate))) && (meta is Map)) { // TODO: add check, that the map contains code, not necessarily, think this conversion stuff through completely
					MethodInfo m=parameter.GetMethod("Invoke",BindingFlags.Instance
						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					Delegate del=CreateDelegate(parameter,m,(Map)meta);
					return del;
				}
				else if(parameter.IsArray && meta is IMap && ((Map)meta).IntKeyValues.Count!=0) {// TODO: cheating, not very understandable
					try {
						Type arrayType=parameter.GetElementType();
						Map map=((Map)meta);
						ArrayList mapValues=map.IntKeyValues;
						Array array=Array.CreateInstance(arrayType,mapValues.Count);
						for(int i=0;i<mapValues.Count;i++) {
							array.SetValue(mapValues[i],i);
						}
						return array;
					}
					catch {
					}
				}
				else {
					bool isConverted; // TODO: refactor with converted
					object result=Interpreter.ConvertMetaToDotNet(meta,
						parameter,out isConverted);
					if(isConverted) {
						return result;
					}
				}
				converted=false;
				return null;
			}
			public object Call(IMap argument) {
				object result=null;
				// TODO: check this for every method:
				// introduce own methodinfo class? that does the calling, maybe??? dynamic cast might become a performance
				// problem, but I doubt it, so what?
				if(isMetaLibraryMethod) {
					if(methods[0] is ConstructorInfo) {
						// TODO: Comment this properly: kcall methods without arguments, ugly and redundant, because Invoke is not compatible between
						// constructor and normal method
						result=((ConstructorInfo)methods[0]).Invoke(new object[] {argument}); 

						//						result=((ConstructorInfo)methods[0]).Invoke(new object[] {}); 
					}
					else {
						try {
							result=methods[0].Invoke(target,new object[] {argument});
						}
						catch {
							throw new RuntimeException("Could not invoke "+this.name+".");
						}
						//						result=methods[0].Invoke(target,new object[] {});
					}
				}
				else {
					ArrayList argumentList=argument.IntKeyValues;
					object returnValue=null;
					ArrayList sameLengthMethods=new ArrayList();
					foreach(MethodBase method in methods) {
						if(argumentList.Count==method.GetParameters().Length) { // don't match if different parameter list length
							if(argumentList.Count==argument.Keys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
								sameLengthMethods.Add(method);
							}
						}
					}
					bool executed=false;
					foreach(MethodBase method in sameLengthMethods) {
						ArrayList args=new ArrayList();
						bool argumentsMatched=true;
						ParameterInfo[] parameters=method.GetParameters();
						for(int i=0;argumentsMatched && i<parameters.Length;i++) {
							args.Add(ConvertParameter(argumentList[i],parameters[i].ParameterType,out argumentsMatched));
						}
						if(argumentsMatched) {
							if(method is ConstructorInfo) {
								returnValue=((ConstructorInfo)method).Invoke(args.ToArray());
							}
							else {
								returnValue=method.Invoke(target,args.ToArray());
							}
							executed=true;// remove, use argumentsMatched instead
							break;
						}
					}
					result=returnValue; // mess, why is this here? put in else after the next if
					// make this safe
					if(!executed && methods[0] is ConstructorInfo) {
						object o=new NetMethod(type).Call(new Map());
						result=with(o,((Map)argument));
//						result=with(o,((Map)Interpreter.Arg));
					}
				}		
				//Interpreter.arguments.RemoveAt(Interpreter.arguments.Count-1);// change to RemoveAt
				return Interpreter.ConvertDotNetToMeta(result);
			}
			// TODO: Refactor, include 
			public static object with(object obj,IMap map) {
				NetObject netObject=new NetObject(obj);
				foreach(DictionaryEntry entry in map) {
					netObject[entry.Key]=entry.Value;
				}
				return obj;
			}
			/* Create a delegate of a certain type that calls a Meta function. */
			public static Delegate CreateDelegate(Type delegateType,MethodInfo method,Map code) { // TODO: delegateType, methode, redundant?
				code.Parent=(IMap)Interpreter.callers[Interpreter.callers.Count-1];
				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
				ICodeCompiler compiler=codeProvider.CreateCompiler();
				string returnTypeName;
				if(method==null) {
					returnTypeName="object";
				}
				else {
					returnTypeName=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
				}
				string source="using System;using Meta.Types;using Meta.Execution;";
				source+="public class EventHandlerContainer{public "+returnTypeName+" EventHandlerMethod";
				int counter=1;
				string argumentList="(";
				string argumentAdding="Map arg=new Map();";
				if(method!=null) {
					foreach(ParameterInfo parameter in method.GetParameters()) {
						argumentList+=parameter.ParameterType.FullName+" arg"+counter;
						argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
						if(counter<method.GetParameters().Length) {
							argumentList+=",";
						}
						counter++;
					}
				}
				argumentList+=")";
				source+=argumentList+"{";
				source+=argumentAdding;
				source+="object result=callable.Call(arg);";
				if(method!=null) {
					if(!method.ReturnType.Equals(typeof(void))) {
						source+="return ("+returnTypeName+")";
						source+="Interpreter.ConvertMetaToDotNet(result,typeof("+returnTypeName+"));"; // does conversion even make sense here? Must be converted back anyway.
					}
				}
				else {
					source+="return";
					source+=" result;";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				string metaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
				assemblyNames.AddRange(Interpreter.loadedAssemblies);
				CompilerParameters options=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
																																			  code});
				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
				if(method==null) {
					delegateType=typeof(DelegateCreatedForGenericDelegates);
				}
				Delegate del=Delegate.CreateDelegate(delegateType,
				container,"EventHandlerMethod");
				return del;
			}
			private void Initialize(string name,object target,Type type) {
				this.name=name;
				this.target=target;
				this.type=type;
				ArrayList list;
				if(name==".ctor") {
					list=new ArrayList(type.GetConstructors());
				}
				else {
					list=new ArrayList(type.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
				}
				list.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove
									 // found out, it's for Console.WriteLine, where Console.WriteLine(object)
									 // would otherwise come before Console.WriteLine(string)
									 // not a good solution, though

									// TODO: Get rid of this Reversion shit! Find a fix for this problem. Need to think about
									// it. maybe restrict overloads, create preference list, all quite complicated
									// research the number and nature of such methods as Console.WriteLine
				methods=(MethodBase[])list.ToArray(typeof(MethodBase));
				if(methods.Length==1 && methods[0].GetCustomAttributes(typeof(MetaLibraryMethodAttribute),false).Length!=0) {
					this.isMetaLibraryMethod=true;
				}
			}
			public NetMethod(string name,object target,Type type) {
				this.Initialize(name,target,type);
			}
			public NetMethod(Type type) {
				this.Initialize(".ctor",null,type);
			}
			public override bool Equals(object obj) {
				if(obj is NetMethod) {
					NetMethod method=(NetMethod)obj;
					if(method.target==target && method.name.Equals(name) && method.type.Equals(type)) {
						return true;
					}
				}
				return false;
			}
			public override int GetHashCode() {
				unchecked {
					int hash=name.GetHashCode()*type.GetHashCode();
					if(target!=null) {
						hash=hash*target.GetHashCode();
					}
					return hash;
				}
			}
			private string name;
			protected object target;
			protected Type type;
			public MethodBase[] methods;

		}
		public class NetClass: NetContainer, IKeyValue,ICallable {
			[DontSerializeFieldOrProperty]
			public NetMethod constructor;
			public NetClass(Type type):base(null,type) {
				this.constructor=new NetMethod(this.type);
			}
			public object Call(IMap argument) {
				return constructor.Call(argument);
			}
		}
		/* Representation of a .NET object. */
		public class NetObject: NetContainer, IKeyValue {
			public NetObject(object obj):base(obj,obj.GetType()) {
			}
			public override string ToString() {
				return obj.ToString();
			}
		}
		/* Base class for NetObject and NetClass. */
		public abstract class NetContainer: IKeyValue, IEnumerable,ISerializeSpecial {
			public bool ContainsKey(object key) {
				if(key is Map) {
					if(((Map)key).IsString) {
						string text=((Map)key).GetDotNetString();
						if(type.GetMember((string)key,
							BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
							return true;
						}
					}
				}
				NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
				Map arguments=new Map();
				arguments[new Integer(1)]=key;
				try {
					indexerMethod.Call(arguments);
					return true;
				}
				catch(Exception) {
					return false;
				}
			}
			public IEnumerator GetEnumerator() {
				return Table.GetEnumerator();
			}
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			public ArrayList Keys {
				get {
					return new ArrayList(Table.Keys);
				}
			}
			public int Count  {
				get {
					return Table.Count;
				}
			}
			public virtual object this[object key]  {
				get {
					if(key is Map && ((Map)key).IsString) {
						string text=((Map)key).GetDotNetString();
						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								return new NetMethod(text,obj,type);
							}
							if(members[0] is FieldInfo) {
								// convert arrays to maps here?
								return Interpreter.ConvertDotNetToMeta(type.GetField(text).GetValue(obj));
							}
							else if(members[0] is PropertyInfo) {
								return Interpreter.ConvertDotNetToMeta(type.GetProperty(text).GetValue(obj,new object[]{}));
							}
							else if(members[0] is EventInfo) {
								Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
								return new NetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
						}
					}
					if(this.obj!=null && key is Integer && this.type.IsArray) {
						return Interpreter.ConvertDotNetToMeta(((Array)obj).GetValue(((Integer)key).Int));
					}
					NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
					try {
						return indexerMethod.Call(arguments);
					}
					catch(Exception) {
						return null;
					}
				}
				set {
					if(key is Map && ((Map)key).IsString) {
						string text=((Map)key).GetDotNetString();
						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								throw new RuntimeException("Cannot set method "+key+".");
							}
							else if(members[0] is FieldInfo) {
								FieldInfo field=(FieldInfo)members[0];
								bool converted;
								object val;
								val=NetMethod.ConvertParameter(value,field.FieldType,out converted);
								if(converted) {
									field.SetValue(obj,val);
								}
								if(!converted) {
									if(value is Map) {
										val=NetMethod.DoModifiableCollectionAssignment((Map)value,field.GetValue(obj),out converted);
									}
								}
								if(!converted) {
									throw new RuntimeException("Field value could not be assigned because it cannot be converted.");
								}
								//TODO: refactor
								return;
							}
							else if(members[0] is PropertyInfo) {
								PropertyInfo property=(PropertyInfo)members[0];
								bool converted;
								object val=NetMethod.ConvertParameter(value,property.PropertyType,out converted);
								if(converted) {
									property.SetValue(obj,val,new object[]{});
								}
								if(!converted) {
									if(value is Map) {
										NetMethod.DoModifiableCollectionAssignment((Map)value,property.GetValue(obj,new object[]{}),out converted);
									}
									if(!converted) {
										throw new RuntimeException("Property "+this.type.Name+"."+Interpreter.MetaSerialize(key,"",false)+" could not be set to "+value.ToString()+". The value can not be converted.");
									}
								}
								return;
							}
							else if(members[0] is EventInfo) {
								((EventInfo)members[0]).AddEventHandler(obj,CreateEvent(text,(Map)value));
								return;
							}
						}
					}
					if(obj!=null && key is Integer && type.IsArray) { // use .IsArray
						bool isConverted; 
						object converted=Interpreter.ConvertMetaToDotNet(value,type.GetElementType(),out isConverted);
						if(isConverted) {
							((Array)obj).SetValue(converted,((Integer)key).Int);
							return;
						}
					}
					NetMethod indexer=new NetMethod("set_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
					arguments[new Integer(2)]=value;//lazy
					try {
						indexer.Call(arguments);
					}
					catch(Exception) {
						throw new RuntimeException("Cannot set "+key.ToString()+".");
					}
				}
			}
			public string Serialize(string indent,string[] functions) {
				return indent;
			}
			public Delegate CreateEvent(string name,Map code) {
				EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
															 BindingFlags.Static|BindingFlags.Instance);
				MethodInfo method=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
																						 |BindingFlags.Public|BindingFlags.NonPublic);
				Delegate del=NetMethod.CreateDelegate(eventInfo.EventHandlerType,method,code);
				return del;
			}
			private IDictionary Table { // TODO: strange, what use is this
				get {
					HybridDictionary table=new HybridDictionary();
					BindingFlags bindingFlags;
					if(obj==null)  {
						bindingFlags=BindingFlags.Public|BindingFlags.Static;
					}
					else  {
						bindingFlags=BindingFlags.Public|BindingFlags.Instance;
					}
					foreach(FieldInfo field in type.GetFields(bindingFlags)) {
						table[new Map(field.Name)]=field.GetValue(obj);
					}
					foreach(MethodInfo method in type.GetMethods(bindingFlags))  {
						if(!method.IsSpecialName) {
							table[new Map(method.Name)]=new NetMethod(method.Name,obj,type);
						}
					}
					foreach(PropertyInfo property in type.GetProperties(bindingFlags)) {
						if(property.Name!="Item" && property.Name!="Chars") {
							table[new Map(property.Name)]=property.GetValue(obj,new object[]{});
						}
					}
					foreach(EventInfo eventInfo in type.GetEvents(bindingFlags)) {
						table[new Map(eventInfo.Name)]=new NetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
					}
					int counter=1;
					if(obj!=null && obj is IEnumerable && !(obj is String)) { // is this useful?
						foreach(object entry in (IEnumerable)obj) {
							if(entry is DictionaryEntry) {
								table[Interpreter.ConvertDotNetToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
							}
							else {
								table[new Integer(counter)]=entry;
								counter++;
							}
						}
					}
					return table;
				}
			}
			public NetContainer(object obj,Type type) {
				this.obj=obj;
				this.type=type;
			}
			private IKeyValue parent;
			public object obj;
			public Type type;
		}
	}
	namespace Parser  {
		public class IndentationStream: TokenStream {
			public IndentationStream(TokenStream stream)  {
				this.stream=stream;
				AddIndentationTokensToGetToLevel(0,new Token());
			}
			public Token nextToken()  {
				if(streamBuffer.Count==0)  {
					Token t=stream.nextToken();
					switch(t.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokensToGetToLevel(-1,t);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokensToGetToLevel(t.getText().Length,t);
							break;
						case MetaLexerTokenTypes.LITERAL: // move this into parser, for correct error handling?
							string indentation="";
							for(int i=0;i<presentIndentationLevel+1;i++) {
								indentation+='\t';
							}
							string text=t.getText();
							text=text.Replace(Environment.NewLine,"\n");
							string[] lines=text.Split('\n');
							string result="";
							for(int k=0;k<lines.Length;k++) {
								if(k!=0 && lines[k].StartsWith(indentation)) {
									result+=lines[k].Remove(0,presentIndentationLevel+1);
								}
								else {
									result+=lines[k];
								}
								if(k!=lines.Length-1) {
									result+=Environment.NewLine;
								}
							}
							t.setText(result);
							streamBuffer.Enqueue(t);
							break;
						default:
							streamBuffer.Enqueue(t);
							break;
					}
				}
				return (Token)streamBuffer.Dequeue();
			}
			protected void AddIndentationTokensToGetToLevel(int newIndentationLevel,Token token)  {
				int indentationDifference=newIndentationLevel-presentIndentationLevel; 
				if(indentationDifference==0) {
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(indentationDifference==1) {
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				}
				else if(indentationDifference<0) {
					for(int i=indentationDifference;i<0;i++) {
						streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE)); // TODO: tiny bit unlogical? maybe create this in Parser?
				}
				else if(indentationDifference>1) {
					// This doesn't get through properly because it is caught by ANTLR
					// TODO: make extra exception later.
					// I don't understand it and the lines are somehow off
					throw new RecognitionException("Incorrect indentation.",token.getFilename(),token.getLine(),token.getColumn());
				}
				presentIndentationLevel=newIndentationLevel;
			}
			protected Queue streamBuffer=new Queue();
			protected TokenStream stream;
			protected int presentIndentationLevel=-1;
		}
//		public class LineNumberAST:CommonAST {
//			private int lineNumber;
//			public LineNumberAST() { }
//			public LineNumberAST(Token tok):base(tok){}
//			public override void initialize(AST t) {
//				base.initialize(t);
//				if (t.GetType().Name.Equals("LineNumberAST")) {
//					LineNumberAST t2 = (LineNumberAST)t;
//					lineNumber = t2.lineNumber;
//				}
//			}
//			//		public void initialize(AST t) {
//			//			super.initialize(t);
//			//			if (t.getClass().getName().compareTo("LineNumberAST") == 0) {
//			//				LineNumberAST t2 = (LineNumberAST)t;
//			//				lineNumber = t2.lineNumber;
//			//			}
//			//		}
//			public override void initialize(Token tok) {
//				base.initialize(tok);
//				lineNumber = tok.getLine(); /* store the line number from the token */
//			}
//			//		public void initialize(Token tok) {
//			//			super.initialize(tok);
//			//			lineNumber = tok.getLine(); /* store the line number from the token */
//			//		}
//
//			/* use this function in your tree walker to get the token's line number */
//			public int getLineNumber() {
//				return lineNumber;
//			}
//		}
	}
	namespace TestingFramework {
		public interface ISerializeSpecial {
			string Serialize(string indent,string[] functions);
		}
		public abstract class TestCase {
			public abstract object RunTestCase();
		}
		public class ExecuteTests {	
			public ExecuteTests(Type classThatContainsTests,string pathToSerializeResultsTo) { // refactor -maybe, looks quite ok
				bool waitAtEndOfTestRun=false;
				Type[] testCases=classThatContainsTests.GetNestedTypes();
				foreach(Type testCase in testCases) {
					object[] customSerializationAttributes=testCase.GetCustomAttributes(typeof(SerializeMethodsAttribute),false);
					string[] methodNames=new string[0];
					if(customSerializationAttributes.Length!=0) {
						methodNames=((SerializeMethodsAttribute)customSerializationAttributes[0]).names;
					}
					Console.Write(testCase.Name + "...");
					DateTime timeStarted=DateTime.Now;
					string textToPrint="";
					object result=((TestCase)testCase.GetConstructors()[0].Invoke(new object[]{})).RunTestCase();
					TimeSpan timeSpentInTestCase=DateTime.Now-timeStarted;
					bool testCaseSuccessful=CompareResults(Path.Combine(pathToSerializeResultsTo,testCase.Name),result,methodNames);
					if(!testCaseSuccessful) {
						textToPrint=textToPrint + " failed";
						waitAtEndOfTestRun=true;
					}
					else {
						textToPrint+=" succeeded";
					}
					textToPrint=textToPrint + "  " + timeSpentInTestCase.TotalSeconds.ToString() + " s";
					Console.WriteLine(textToPrint);
				}
				if(waitAtEndOfTestRun) {
					Console.ReadLine();
				}
			}
			private bool CompareResults(string path,object obj,string[] functions) {
				Directory.CreateDirectory(path);
				if(!File.Exists(Path.Combine(path,"check.txt"))) {
					File.Create(Path.Combine(path,"check.txt")).Close();
				}
				string result=Serialize(obj,"",functions);
				StreamWriter writer=new StreamWriter(Path.Combine(path,"result.txt"));
				writer.Write(result);
				writer.Close();
				StreamWriter copyWriter=new StreamWriter(Path.Combine(path,"resultCopy.txt"));
				copyWriter.Write(result);
				copyWriter.Close();
				// TODO: Introduce utility methods
				StreamReader reader=new StreamReader(Path.Combine(path,"check.txt"));
				string check=reader.ReadToEnd();
				reader.Close();
				return result.Equals(check);
			}
			public static string Serialize(object obj) {
				return Serialize(obj,"",new string[]{});
			}
			public static string Serialize(object serialize,string indent,string[] methods) {
				if(serialize==null) {
					return indent+"null\n";
				}
				if(serialize is ISerializeSpecial) {
					string text=((ISerializeSpecial)serialize).Serialize(indent,methods);
					if(text!=null) {
						return text;
					}
				}
				if(serialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) {
					return indent+"\""+serialize.ToString()+"\""+"\n";
				}
				if(serialize is IEnumerable) {
					string text="";
					foreach(object entry in (IEnumerable)serialize) {
						text+=indent+"Entry ("+entry.GetType().Name+")\n"+Serialize(entry,indent+"  ",methods);
					}
					return text;
				}
				string t="";
				ArrayList members=new ArrayList();

				members.AddRange(serialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
				members.AddRange(serialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
				foreach(string methodName in methods) {
					MethodInfo method=serialize.GetType().GetMethod(methodName,BindingFlags.Public|BindingFlags.Instance);
					if(method!=null) { /* Only add method to members if it really exists, this isn't sure because methods are supplied per test not per class. */
						members.Add(method);
					}
				}
				members.Sort(new CompareMemberInfos());
				foreach(MemberInfo member in members) {
					if(member.Name!="Item") {
						if(member.GetCustomAttributes(typeof(DontSerializeFieldOrPropertyAttribute),false).Length==0) {
							if(serialize.GetType().Namespace==null ||!serialize.GetType().Namespace.Equals("System.Windows.Forms")) { // ugly hack to avoid some srange behaviour of some classes in System.Windows.Forms
								object val=serialize.GetType().InvokeMember(member.Name,BindingFlags.Public
									|BindingFlags.Instance|BindingFlags.GetProperty|BindingFlags.GetField
									|BindingFlags.InvokeMethod,null,serialize,null);
								t+=indent+member.Name;
								if(val!=null) {
									t+=" ("+val.GetType().Name+")";
								}
								t+=":\n"+Serialize(val,indent+"  ",methods);
							}
						}
					}
				}
				return t;
			}
		}
		internal class CompareMemberInfos:IComparer {
			public int Compare(object first,object second) {
				if(first==null || second==null || ((MemberInfo)first).Name==null || ((MemberInfo)second).Name==null) {
					int asdf=0;
					return 0;
				}
				else {
					return ((MemberInfo)first).Name.CompareTo(((MemberInfo)second).Name);
				}
			}
		}
		[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
		public class DontSerializeFieldOrPropertyAttribute:Attribute {
		}
		[AttributeUsage(AttributeTargets.Class)]
		public class SerializeMethodsAttribute:Attribute {
			public string[] names;
			public SerializeMethodsAttribute(string[] names) {
				this.names=names;
			}
		}
	}
}
