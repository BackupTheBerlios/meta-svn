//	Copyright (c) 2005, 2006 Christian Staudenmeyer
//
//	Permission is hereby granted, free of charge, to any person obtaining
//	a copy of this software and associated documentation files (the
//	"Software"), to deal in the Software without restriction, including
//	without limitation the rights to use, copy, modify, merge, publish,
//	distribute, sublicense, and/or sell copies of the Software, and to
//	permit persons to whom the Software is furnished to do so, subject to
//	the following conditions:
//
//	The above copyright notice and this permission notice shall
//	included in all copies or substantial portions of the Software.
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
//	BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
//	ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//	SOFTWARE.

using Meta;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using java.math;
using System.Globalization;
using Meta.Fusion;
using System.Collections;
using SDILReader;
using System.Diagnostics;

using System.Runtime.InteropServices;


namespace Meta {
	public delegate Map Compiled(Map map);

	public abstract class ChildExpression : Expression {
		public Expression child;
		public ChildExpression(Map map, Expression child)
			: base(map) {
			this.child = child;
		}
	}
	public class ToMeta : ChildExpression {
		public override void Emit(Emitter emitter, Expression parent, OpCode context) {
			child.Emit(emitter, parent, context);
			Transform.GetMetaConversion(type,emitter);
		}
		public Type type;
		public ToMeta(Expression expression,Type type,Map map):base(map,expression) {
			this.type = type;
		}
	}
	public class ToDotNet : ChildExpression {
		public override void Emit(Emitter emitter, Expression parent, OpCode context) {
			child.Emit(emitter, parent, context);
			Transform.GetDotNetConversion(type, emitter);
		}
		public Type type;
		public ToDotNet(Expression expression,Type type,Map map):base(map,expression) {
			this.type = type;
		}
	}
	public class Branch : Expression {
		public Expression condition;
		public Expression first;
		public Expression second;
		public Branch(Map map, Expression condition, Expression first, Expression second):base(map) {
			this.condition = new ToDotNet(condition, typeof(bool), null);
			this.first = first;
			this.second = second;
		}
		public override void Emit(Emitter emitter, Expression parent, OpCode context) {
		}
	}
	public class Constant:Expression {
		private Map constant;
		public Constant(Map constant)
			: base(constant) {
			this.constant = constant;
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				return constant;
			};
		}
	}
	public class SimpleLiteral:Expression {
		private Map literal;
		public SimpleLiteral(Map literal):base(literal) {
			this.literal = literal;
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				return literal;
			};
		}
	}
	// TODO: save parent
	public abstract class Expression {
		public delegate Expression Optimization(Expression expression);
		public Expression(Map map) {//:this(new Extent(new Source(0,0,""),new Source(0,0,"")),null) {
			this.map = map;
			this.Source = map.Source;
		}
		public static Optimization[] optimizations = new Optimization[] {
			SimpleLiteral,
			ConstantSearch,
			PerfectSearch,
			FixedSearch,
			NativeCall
		};
		public static Expression SimpleLiteral(Expression expression) {
			Literal literal=expression as Literal;
			if(literal!=null) {
				if (literal.literal.Expression == null) {
					try {
						return new SimpleLiteral(literal.literal);
					}
					catch (Exception e) {
						return new SimpleLiteral(literal.literal);
					}
				}
			}
			return expression;
		}
		public static Expression Optimize(Expression expression) {
			foreach (Optimization optimization in optimizations) {
				expression = Optimize(expression, optimization);
			}
			return expression;
		}
		public static void InvokeOnChildren(object obj, Optimization optimization) {
			Literal literal = obj as Literal;
			if (literal!=null) {
				Expression e=literal.literal.GetExpression((Literal)obj);
				if (e != null) {
					literal.literal.Expression = Optimize(e, optimization);
				}
			}
			foreach (FieldInfo field in obj.GetType().GetFields()) {
				if (field.FieldType.Equals(typeof(Expression)) && field.Name != "Parent" && field.Name!="program") {
					field.SetValue(obj, Optimize((Expression)field.GetValue(obj),optimization));
				}
				else if (field.FieldType.Equals(typeof(List<Expression>))) {
					List<Expression> list = (List<Expression>)field.GetValue(obj);
					for (int index = 0; index < list.Count; index++) {
						list[index] = Optimize(list[index],optimization);
					}
				}
				else if (field.FieldType.Equals(typeof(List<Statement>))) {
					List<Statement> list = (List<Statement>)field.GetValue(obj);
					for (int index = 0; index < list.Count; index++) {
						InvokeOnChildren(list[index], optimization);
					}
				}
			}
		}
		public static Expression Optimize(Expression expression,Optimization optimization) {
			InvokeOnChildren(expression, optimization);
			expression = optimization(expression);
			return expression;
		}
		public static Expression ConstantSearch(Expression expression) {
			Search search = expression as Search;
			if (search != null) {
				Constant constant=search.expression as Constant;
				//Expression current=search;
				//while(true) {
				//    if(current is Literal) {
				//    // -> this is inside a function, need special handling from here on
				//    }
				//    current=current.Parent;
				//    if (current == null) {
				//        break;
				//    }
				//    Statement statement = current.Statement;
				//    Map structure = statement.PreMap();
				//    if (structure == null) {
				//        statement.Pre();
				//        break;
				//    }
				//    if (structure.ContainsKey(key)) {
				//        value = structure[key];
				//        map = structure;
				//        s = statement;
				//        return true;
				//    }
				//    else if (programCounter < 1 && statement is KeyStatement) {
				//        if (hasCrossedFunction) {
				//            map = statement.CurrentMap();
				//            if (map != null) {
				//                //if (map != null && map.IsConstant) {
				//                if (map.ContainsKey(key)) {
				//                    value = map[key];
				//                    //if (value.IsConstant) {
				//                    //if (value.IsConstant) {
				//                    s = statement;
				//                    return true;
				//                    //}
				//                }
				//            }
				//        }
				//    }
				//    if (hasCrossedFunction) {
				//        if (!statement.NeverAddsKey(key)) {
				//            break;
				//        }
				//    }
				//    count++;
				//    if (current.Statement != null && current.Statement.program != null && !current.Statement.program.IsFunction) {
				//        programCounter++;
				//    }
				//    current = current.Parent;
				//}
			}
			return expression;
		}
		//public static Expression ConstantSearch(Expression expression) {
		//    Search search = expression as Search;
		//    if (search != null) {
		//        int count;
		//        Map key;
		//        Map value;
		//        Map map;
		//        Statement statement;
		//        //if (search.FindStuff(out count, out key, out value, out map, out statement)) {
		//        //    if (value != null && value.IsConstant) {
		//        //        return new Constant(value);
		//        //    }
		//        //}
		//    }
		//    return expression;
		//}
		public static Expression PerfectSearch(Expression expression) {
			Search search = expression as Search;
			if (search != null) {
				int count;
				Map key;
				Map value;
				Map map;
				Statement statement;
				//if (search.FindStuff(out count, out key, out value, out map, out statement)) {
				//    int index = -1;
				//    Mapping mapping = statement.program.UsePerfectMap();
				//    if (mapping != null && mapping.mapping.ContainsKey(key)) {
				//        index = mapping.mapping[key];
				//    }
				//    if (index != -1) {
				//        return new PerfectSearch(count,key,index);
				//    }
				//}
			}
			return expression;
		}
		public static Expression NativeCall(Expression expression) {
			Call call = expression as Call;
			if (call != null) {
				List<object> arguments;
				MethodBase method;
				//if (call.CallStuff(out arguments, out method)) {
				//    if (method.IsStatic && method.GetParameters().Length==call.calls.Count-1) {
				//        return new NativeCall(method, call.calls.GetRange(1, call.calls.Count - 1));
				//    }
				//}
			}
			return expression;
		}
		public static Expression FixedSearch(Expression expression){
			Search search = expression as Search;
			if (search != null) {
				int count;
				Map key;
				Map value;
				Map map;
				Statement statement;
				//if (search.FindStuff(out count, out key, out value, out map, out statement)) {
				//    return new FixedSearch(null, key, count);
				//}
			}
			return expression;
		}
		public static void GetCachedCompile(Emitter emitter,Compiled c) {
			emitter.LoadField(typeof(Expression), "compiles");
			emitter.LoadConstant(GetIndex(c));
			emitter.Call(typeof(List<Compiled>), "get_Item");
		}
		public static int GetIndex(Compiled c) {
			compiles.Add(c);
			return compiles.Count - 1;
		}
		public static List<Compiled> compiles = new List<Compiled>();
		public virtual void Emit(Emitter emitter,Expression parent,OpCode context) {
			GetCachedCompile(emitter,GetCompiled(parent));
		}
		public Compiled GetCompiled() {
			if(compiled==null) {
				compiled = Compile();
			}
			return compiled;
		}
		public Compiled compiled;
		public virtual bool IsFunction {
			get {
				return false;
			}
		}
		public Extent Source;
		// parent should be determinded automatically
		// should not be too difficult, actually
		// make it a property
		private Expression parent;
		private Map map;
		public Expression Parent {
			get {
				if(parent==null) {
					Map current = map;
					while (parent == null && current!=null) {
						parent = current.GetExpression();
						current = current.Scope;
					}
				}
				return parent;
			}
			set {
				parent = value;
				//throw new 
			}
		}
		public Statement Statement;
		public virtual Map CallFast(Map scope) {
			return GetCompiled()(scope);
		}
		public virtual Map Call(Map argument, Map scope) {
			return GetCompiled()(scope);
		}
		//public Expression(Extent source, Expression parent) {	
		//    this.Source = source;
		//    this.Parent = parent;
		//}
		private bool evaluated = false;
		private Map structure;
		//public Map GetConstant() {
		//    Map s = EvaluateStructure();
		//    return s != null && s.IsConstant ? s : null;
		//}
		public Map EvaluateMapStructure() {
			return EvaluateStructure();
		}
		public Map EvaluateStructure() {
			if (!evaluated) {
				structure = null;
				evaluated = true;
			}
			return structure;
		}
		public Compiled Compile() {
			Compiled result = GetCompiled(this.Parent);
			if (Source != null) {
				if (!sources.ContainsKey(Source.End)) {
					sources[Source.End] = new List<Expression>();
				}
				sources[Source.End].Add(this);
			}
			return result;
		}
		public static Dictionary<Source, List<Expression>> sources = new Dictionary<Source, List<Expression>>();
		public virtual Compiled GetCompiled(Expression parent) {
			Emitter emitter = new Emitter(typeof(Compiled));
			Emit(emitter, parent, OpCodes.Ldarg_0);
			emitter.Return();
			return (Compiled)emitter.GetDelegate();
		}
	}
	public class LastArgument : Expression {
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				return Map.arguments.Pop();
			};
		}
		public LastArgument(Map code)
		//public LastArgument(Map code, Expression parent)
			: base(code) {
		}
	}
	public class Call : Expression {
		public List<Expression> calls;
		public Call(Map code)
			: base(code) {
		//public Call(Map code, Expression parent): base(code.Source, parent) {
			this.calls = new List<Expression>();
			foreach (Map m in code.Array) {
				calls.Add(m.GetExpression(this));
			}
			if (calls.Count == 1) {
				calls.Add(new Literal(Map.Empty));
				//calls.Add(new Literal(Map.Empty, this));
			}
		}
		//implement Constant optimization for some special methods ( and implement expression for special methods, maybe)

		//public override Map GetStructure() {
		//    List<object> arguments;
		//    MethodBase method;
		//    if (CallStuff(out arguments, out method)) {
		//        if (method is ConstructorInfo) {
		//            Dictionary<Map, Member> type = ObjectMap.cache.GetMembers(method.DeclaringType);
		//            Map result = new DictionaryMap();
		//            result.IsConstant=false;
		//            foreach (Map key in type.Keys) {
		//                Member member = type[key];
		//                Map value;
		//                if (member is MethodMember) {
		//                    value = member.Get(null);
		//                }
		//                else {
		//                    value = Map.Empty;
		//                }
		//                result[key] = value;

		//            }
		//            return result;
		//        }
		//        else if (arguments != null && method.GetCustomAttributes(typeof(CompilableAttribute), false).Length != 0) {
		//            Map result = (Map)method.Invoke(null, arguments.ToArray());
		//            result.IsConstant = false;
		//            return result;
		//        }
		//        else if(method is MethodInfo) {
		//            Type type = ((MethodInfo)method).ReturnType;
		//            if (type != typeof(Map) && !type.IsSubclassOf(typeof(Map))) {
		//                return GetInstanceStructure(type);
		//            }
		//        }
		//    }
		//    return null;
		//}

		//public static Map GetInstanceStructure(Type t) {
		//    Dictionary<Map, Member> type = ObjectMap.cache.GetMembers(t);
		//    Map result = new DictionaryMap();
		//    //result.IsConstant = false;
		//    foreach (Map key in type.Keys) {
		//        Member member = type[key];
		//        Map value;
		//        if (member is MethodMember) {
		//            value = member.Get(null);
		//        }
		//        else {
		//            value = Map.Empty;
		//        }
		//        result[key] = value;
		//    }
		//    return result;
		//}

		//public bool CallStuff(out List<object> arguments, out MethodBase m) {
		//    Map first = (Map)calls[0].GetConstant();
		//    if (first != null) {
		//        Method method;
		//        if (first is TypeMap) {
		//            method = (Method)((TypeMap)first).Constructor;
		//        }
		//        else if (first is Method) {
		//            method = (Method)first;
		//        }
		//        else {
		//            method = null;
		//        }
		//        if (method != null) {
		//            if (method.parameters.Length == calls.Count - 1 || (calls.Count == 2 && method.parameters.Length == 0)) {
		//                arguments = new List<object>();
		//                for (int i = 0; i < method.parameters.Length; i++) {
		//                    Map arg = calls[i + 1].EvaluateMapStructure();
		//                    if (arg == null) {
		//                        m = method.method;
		//                        return true;}
		//                    else if(method.method.GetCustomAttributes(typeof(CompilableAttribute),false).Length!=0) {
		//                        arguments.Add(Transform.ToDotNet(arg, method.parameters[i].ParameterType));
		//                    }
		//                }
		//                m = method.method;
		//                return true;
		//            }
		//        }
		//    }
		//    arguments = null;
		//    m = null;
		//    return false;
		//}
		public override Compiled GetCompiled(Expression parent) {
			List<object> arguments;
			MethodBase method;
			//if (CallStuff(out arguments, out method)) {
			//    if (method.IsStatic) {
			//        List<Compiled> c = calls.GetRange(1, calls.Count - 1).ConvertAll<Compiled>(delegate(Expression e) { return e.Compile(); });
			//        Compiled[] args = c.ToArray();
			//        ParameterInfo[] parameters = method.GetParameters();
			//        MethodInfo methodInfo = (MethodInfo)method;
			//        Emitter emitter = new Emitter(typeof(Compiled));
			//        for (int i = 0; i < parameters.Length; i++) {
			//            Expression e = calls[i + 1];

			//            Type type = parameters[i].ParameterType;
			//            compiles.Add(c[i]);
			//            int index = compiles.Count - 1;
			//            if (calls[i + 1] is Literal) {
			//                calls[i + 1].Emit(emitter, this, OpCodes.Ldarg_0);
			//            }
			//            else {
			//                emitter.LoadField(typeof(Expression).GetField("compiles"));
			//                emitter.LoadConstant(index);
			//                emitter.Call(typeof(List<Compiled>).GetMethod("get_Item"));
			//                emitter.LoadArgument(0);
			//                emitter.Call(typeof(Compiled).GetMethod("Invoke"));
			//            }
			//            Transform.GetDotNetConversion(type, emitter);
			//        }
			//        emitter.Call(methodInfo);
			//        Transform.GetMetaConversion(methodInfo.ReturnType, emitter);
			//        emitter.Return();
			//        return (Compiled)emitter.GetDelegate();
			//    }
			//}
			//if (calls.Count == 2 && calls[0].GetConstant() != null) {
			//    Map s = calls[1].EvaluateStructure();
			//}
			List<Compiled> compiled = calls.ConvertAll<Compiled>(delegate(Expression e) {
				return e.Compile();
			});
			return delegate(Map current) {
				Map result = compiled[0](current);
				for (int i = 1; i < compiled.Count; i++) {
					try {
						result = result.Call(compiled[i](current));
					}
					catch (MetaException e) {
						e.InvocationList.Add(new ExceptionLog(Source.Start));
						throw e;
					}
					catch (Exception e) {
						while (e.InnerException != null) {
							e = e.InnerException;
						}
						throw new MetaException(e.Message + "\n" + e.StackTrace, Source.Start);
					}
				}
				return result;
			};
		}
	}

	public delegate Map MetaConversion(object obj);
	public delegate object DotNetConversion(Map map);
	
	public delegate Map FastCall(Map context);
	public class PerfectSearch : Expression {
		private int count;
		private Map key;
		private int index;
		public PerfectSearch(int count,Map key,int index,Map code):base(code) {
			this.count = count;
			this.key = key;
			this.index = index;
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate(Map context) {
				Map selected = context;
				for (int i = 0; i < count; i++) {
					selected = selected.Scope;
				}
				Map result = selected.GetFromIndex(index);
				if (result == null) {
					throw new KeyNotFound(key, null, null);
					//throw new KeyNotFound(key, expression.Source.Start, null);
				}
				return result;
			};
		}
	}
	public class NativeCall : Expression {
		private MethodBase method;
		private List<Expression> arguments;
		public NativeCall(MethodBase method,List<Expression> arguments,Map code)
			: base(code) {
			this.method = method;
			this.arguments = arguments;
		}
		public override Compiled GetCompiled(Expression parent) {
			List<Compiled> c = arguments.ConvertAll<Compiled>(delegate(Expression e) { return e.Compile(); });
			//List<Compiled> c = calls.GetRange(1, calls.Count - 1).ConvertAll<Compiled>(delegate(Expression e) { return e.Compile(); });
			Compiled[] args = c.ToArray();
			ParameterInfo[] parameters = method.GetParameters();
			MethodInfo methodInfo = (MethodInfo)method;
			Emitter il = new Emitter(typeof(Compiled));
			for (int i = 0; i < parameters.Length; i++) {
				Expression e = arguments[i];
				//Expression e = calls[i + 1];

				Type type = parameters[i].ParameterType;
				compiles.Add(c[i]);
				int index = compiles.Count - 1;
				if (arguments[i] is Literal) {
					arguments[i].Emit(il, this, OpCodes.Ldarg_0);
				}
				//if (calls[i + 1] is Literal) {
				//    calls[i + 1].Emit(il, this, OpCodes.Ldarg_0);
				//}
				else {
					il.LoadField(typeof(Expression).GetField("compiles"));
					il.LoadConstant(index);
					il.Call(typeof(List<Compiled>).GetMethod("get_Item"));
					il.LoadArgument(0);
					il.Call(typeof(Compiled).GetMethod("Invoke"));
				}
				Transform.GetDotNetConversion(type, il);
			}
			il.Call(methodInfo);
			Transform.GetMetaConversion(methodInfo.ReturnType, il);
			il.Return();
			return (Compiled)il.GetDelegate();
			//}
			//}
			//}
		}
	}
	public class FixedSearch:Expression {
		private int count;
		private Map key;
		public FixedSearch(Map map,Map key,int count):base(map) {
			this.count = count;
			this.key = key;
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate(Map context) {
				Map selected = context;
				for (int i = 0; i < count; i++) {
					selected = selected.Scope;
				}
				Map result = selected[key];
				if (result == null) {
					throw new KeyNotFound(key, null, null);
					//throw new KeyNotFound(key, expression.Source.Start, null);
				}
				return result;
			};
		}
	}
	public class Search : Expression {
		public static Dictionary<Map, int> search = new Dictionary<Map, int>();
		// this is a mess
		//public bool FindStuff(out int count, out Map key, out Map value, out Map map, out Statement s) {
		//    Expression current = this;
		//    key = expression.EvaluateStructure();
		//    count = 0;
		//    int programCounter = 0;
		//    if (key != null) {
		//        //if (key != null && key.IsConstant) {
		//        bool hasCrossedFunction = false;
		//        while (true) {
		//            while (current.Statement == null || current is Program) {
		//                if (current.IsFunction) {
		//                    hasCrossedFunction = true;
		//                    // TERRIBLE hack
		//                    if (current.Parent is Call) {
		//                    }
		//                    else {
		//                        count++;
		//                    }
		//                }
		//                current = current.Parent;
		//                if (current == null) {
		//                    break;
		//                }
		//            }
		//            if (current == null) {
		//                break;
		//            }
		//            Statement statement = current.Statement;
		//            Map structure = statement.PreMap();
		//            if (structure == null) {
		//                statement.Pre();
		//                break;
		//            }
		//            if (structure.ContainsKey(key)) {
		//                value = structure[key];
		//                map = structure;
		//                s = statement;
		//                return true;
		//            }
		//            else if (programCounter < 1 && statement is KeyStatement) {
		//                if (hasCrossedFunction) {
		//                    map = statement.CurrentMap();
		//                    if (map != null) {
		//                        //if (map != null && map.IsConstant) {
		//                        if (map.ContainsKey(key)) {
		//                            value = map[key];
		//                            //if (value.IsConstant) {
		//                            //if (value.IsConstant) {
		//                            s = statement;
		//                            return true;
		//                            //}
		//                        }
		//                    }
		//                }
		//            }
		//            if (hasCrossedFunction) {
		//                if (!statement.NeverAddsKey(key)) {
		//                    break;
		//                }
		//            }
		//            count++;
		//            if (current.Statement != null && current.Statement.program != null && !current.Statement.program.IsFunction) {
		//                programCounter++;
		//            }
		//            current = current.Parent;
		//        }
		//    }
		//    value = null;
		//    map = null;
		//    s = null;
		//    return false;
		//}
		public Expression expression;
		public Search(Map code)
		//public Search(Map code, Expression parent)
			: base(code) {
			//: base(code.Source, parent) {
			this.expression = code.GetExpression(this);
		}
		public override Compiled GetCompiled(Expression parent) {
			Compiled compiled = expression.Compile();
			return delegate(Map context) {
				Map k = compiled(context);
				Map selected = context;
				while (!selected.ContainsKey(k)) {
					if (selected.Scope != null) {
						selected = selected.Scope;
					}
					else {
						Map m = compiled(context);
						bool b = context.ContainsKey(m);
						throw new KeyNotFound(k, Source.Start, null);
					}
				}
				return selected[k];
			};
		}
	}
	// is this necessary?
	public class FunctionArgument : DictionaryBaseMap {
		public override Map GetFromIndex(int i) {
			return value;
		}
		public override Map this[Map key] {
			get {
				if(key.Equals(this.key)) {
					return value;
				}
				else {
					return null;
				}
			}
			set {
				if(key.Equals(this.key)) {
					this.value = value;
				}
				else {
					throw new Exception("The method or operation is not implemented.");
				}
			}
		}
		public override IEnumerable<Map> Keys {
			get { throw new Exception("The method or operation is not implemented."); }
		}
		public override string GetString() {
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map Copy() {
			throw new Exception("The method or operation is not implemented.");
		}
		public override void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override IEnumerable<Map> Array {
			get { 
				throw new Exception("The method or operation is not implemented."); 
			}
		}
		public override int ArrayCount {
			get { 
				throw new Exception("The method or operation is not implemented."); 
			}
		}
		public override Map Call(Map arg) {
			throw new Exception("The method or operation is not implemented.");
		}
		private Map key;
		private Map value;
		public FunctionArgument(Map key,Map value,Map scope) {
			this.key=key;
			this.value = value;
			this.scope = scope;
		}
		public override bool ContainsKey(Map k) {
			return k.Equals(this.key);
		}
		public override Number GetNumber() {
			throw new Exception("The method or operation is not implemented.");
		}
		public override int Count {
			get { return 1; }
		}
	}
	public class Program : Expression {
		public override bool IsFunction {
			get {
				if (!_isFunctionChecked) {
					_isFunction = CheckIsFunction();
					_isFunctionChecked = true;
				}
				return _isFunction;
			}
		}
		private bool _isFunctionChecked = false;
		private bool _isFunction = false;
		public override Map Call(Map argument, Map scope) {
			Map.arguments.Push(argument);
			return GetCompiled()(scope);
		}
		//public override Map GetStructure() {
		//    if (statementList.Count == 0) {
		//        return new DictionaryMap();
		//    }
		//    else {
		//        return statementList[statementList.Count - 1].Current();
		//    }
		//}
		public Mapping UsePerfectMap() {
			bool usePerfectMap = true;
			List<Map> keys = new List<Map>();
			int index = 0;
			foreach (Statement statement in statementList) {
				KeyStatement keyStatement = statement as KeyStatement;
				if (keyStatement != null) {
					Literal literal = keyStatement.key as Literal;
					if (literal != null) {
						keys.Add(literal.literal);
					}
					else {
						usePerfectMap = false;
					}
				}
				if (statement is CurrentStatement) {
					if (index != statementList.Count - 1) {
						usePerfectMap = false;
					}
				}
				index++;
			}
			Mapping mapping=null;
			if (usePerfectMap) {
				return new Mapping(keys);
			}
			else {
				return null;
			}
		}
		private Map cachedKey;
		private Compiled comp;
		public override Compiled GetCompiled(Expression parent) {
			List<CompiledStatement> list=statementList.ConvertAll<CompiledStatement>(delegate(Statement s) {
				return s.Compile();
			});
			bool useList = true;
			int count = 1;
			int listCount = 0;
			bool usePerfectMap = true;
			List<Map> keys = new List<Map>();
			foreach (Statement statement in statementList) {
				KeyStatement keyStatement = statement as KeyStatement;
				if (keyStatement != null) {
					listCount++;
					Literal literal = keyStatement.key as Literal;
					if (literal != null) {
						keys.Add(literal.literal);
						if (literal.literal.Equals(new SmallInteger(count))) {
							count++;
							continue;
						}
					}
					else {
						usePerfectMap = false;
					}
				}
				useList = false;
			}
			//if (statementList.Count == 2) {
			//    KeyStatement keyStatement = statementList[0] as KeyStatement;
			//    CurrentStatement currentStatement = statementList[1] as CurrentStatement;
			//    if (keyStatement != null && currentStatement != null && keyStatement.value is LastArgument) {
			//        Literal literal = keyStatement.key as Literal;
			//        if (literal != null && literal.GetStructure().IsConstant) {
			//            Map key=literal.GetStructure();
			//            cachedKey = key;
			//            comp = currentStatement.value.GetCompiled(this);
			//            this._isFunction = true;
			//            return delegate(Map context) {
			//                return comp(new FunctionArgument(key, Map.arguments.Peek(), context));
			//            };
			//        }
			//    }
			//}
			Mapping mapping=UsePerfectMap();
			return delegate(Map p) {
				Map context;
				if (useList) {
				    context = new ListMap(statementList.Count);
				}
				else {
					if (mapping!=null) {
						context = new PerfectMap(mapping);
					}
					else {
						context = new DictionaryMap(listCount);
					}
				}


				context.Scope = p;
				foreach (CompiledStatement statement in list) {
					statement.Assign(ref context);
				}
				return context;
			};
		}
		public List<Statement> statementList= new List<Statement>();
		private bool CheckIsFunction() {
			if (statementList.Count == 2) {
				KeyStatement keyStatement = statementList[0] as KeyStatement;
				CurrentStatement currentStatement = statementList[1] as CurrentStatement;
				if (keyStatement != null && currentStatement != null && keyStatement.value is LastArgument) {
					Literal literal = keyStatement.key as Literal;
					//if (literal != null && literal.GetStructure().IsConstant) {
					//    return true;
					//}
				}
			}
			return false;
		}
//        public Program(Map code)
//            : base(code) {
//                        //: base(source, parent) {
////public Program(Extent source,Expression parent):base(source,parent) {
//        }
		public Program(Map code)
			: base(code) {
		//public Program(Map code, Expression parent) : base(code.Source, parent) {
			int index = 0;
			foreach (Map m in code.Array) {
				statementList.Add(m.GetStatement(this, index));
				index++;
			}
		}
	}
	public delegate void StatementDelegate(ref Map context,Map value);
	public class CompiledStatement {
		private StatementDelegate s;
		private Source start;
		private Source end;
		public void Assign(ref Map context) {
			//foreach (Source breakpoint in Interpreter.breakpoints) {
			//    if (start.FileName == breakpoint.FileName) {
			//        if (start<=breakpoint) {
			//            if (end >= breakpoint) {
			//                bool x = end >= breakpoint;
			//                if (Interpreter.Breakpoint != null) {
			//                    Interpreter.Breakpoint(context);
			//                }
			//            }
			//        }
			//    }
			//}
			s(ref context, value(context));
		}
		public CompiledStatement(Source start,Source end,Compiled value, StatementDelegate s) {
			this.start = start;
			this.end = end;
			this.value = value;
			this.s = s;
		}
		public readonly Compiled value;
	}
	public abstract class Statement {
		bool preEvaluated = false;
		bool currentEvaluated = false;
		private Map pre;
		private Map current;
		//public Map PreMap() {
		//    Map s = Pre();
		//    return s;
		//}
		// refactor
		//public IEnumerable<Map> PreKeys() {
		//    if(PreMap()!=null) {
		//        return PreMap().Keys;
		//    }
		//    else {
		//        return new List<Map>();
		//    }
		//}
		//public virtual Map Pre() {
		//    if (!preEvaluated) {
		//        if (Previous == null) {
		//            pre = new DictionaryMap();
		//        }
		//        else {
		//            pre = Previous.Current();
		//        }
		//    }
		//    preEvaluated = true;
		//    return pre;
		//}
		//public Map CurrentMap() {
		//    Map s = Current();
		//    return s;
		//}
		//public Map Current() {
		//    if (!currentEvaluated) {
		//        Map pre = Pre();
		//        if (pre != null) {
		//            current = CurrentImplementation(pre);}
		//        else {
		//            current = null;
		//        }
		//    }
		//    currentEvaluated = true;
		//    return current;
		//}
		//public virtual IEnumerable<Map> CurrentKeys() {
		//    Map m=CurrentMap();
		//    if(m!=null) {
		//        return m.Keys;
		//    }
		//    else {
		//        return null;
		//    }
		//}
		public Statement Next {
			get {
				if (program == null || Index >= program.statementList.Count - 1) {
					return null;
				}
				else {
					return program.statementList[Index + 1];
				}
			}
		}
		//public virtual bool DoesNotAddKey(Map key) {
		//    return true;
		//}
		//public bool NeverAddsKey(Map key) {
		//    Statement current = this;
		//    while (true) {
		//        current = current.Next;
		//        if (current == null || current is CurrentStatement) {
		//            break;
		//        }
		//        if (!current.DoesNotAddKey(key)) {
		//            return false;
		//        }
		//    }
		//    return true;
		//}
		//protected abstract Map CurrentImplementation(Map previous);
		public Statement Previous {
			get {
				if (Index == 0) {
					return null;
				}
				else {
					return program.statementList[Index - 1];
				}
			}
		}
		public abstract CompiledStatement Compile();
		public Program program;
		public Expression value;
		public int Index;
		public Statement(Program program, Expression value, int index) {
			this.program = program;
			this.Index = index;
			this.value = value;
			if (value != null) {
				value.Statement = this;
			}
		}
	}
	public class DiscardStatement : Statement {
		//protected override Map CurrentImplementation(Map previous) {
		//    return previous;
		//}
		public DiscardStatement(Expression discard, Expression value, Program program, int index): base(program, value, index) {}
		public override CompiledStatement Compile() {
			return new CompiledStatement(value.Source.Start,value.Source.End,value.Compile(), delegate { });
		}
	}
	public class KeyStatement : Statement {
		//public override IEnumerable<Map> CurrentKeys() {
		//    if(this.key.GetConstant()!=null && PreKeys()!=null)  {
		//        List<Map> list=new List<Map>(PreKeys());
		//        list.Add(key.GetConstant());
		//        return list;
		//    }
		//    return null;
		//}
		public static bool intellisense = false;
		//public override bool DoesNotAddKey(Map key) {
		//    Map k = this.key.EvaluateStructure();
		//    if (k != null && k.IsConstant && !k.Equals(key)) {
		//    //if (k != null && k.IsConstant && !k.Equals(key)) {
		//        return true;
		//    }
		//    return false;
		//}

		//protected override Map CurrentImplementation(Map previous) {
		//    Map k=key.GetConstant();
		//    if (k != null) {
		//        Map val=value.EvaluateMapStructure();
		//        if (val == null) {
		//            val = new DictionaryMap();
		//            val.IsConstant=false;
		//        }
		//        if (value is Search || value is Call || (intellisense && (value is Literal || value is Program))) {
		//            previous[k] = val;
		//        }
		//        else {
		//            Map m=new DictionaryMap();
		//            m.IsConstant=false;
		//            previous[k] = m;
		//        }
		//        return previous;
		//    }
		//    return null;
		//}

		public override CompiledStatement Compile() {
			//Map k = key.GetConstant();
			// this should be done for all statements, not just for the key statement
			//if (k != null && k.Equals(CodeKeys.Function)) {

				if (value is Literal && ((Literal)value).literal.GetExpression(program)!=null) {
					((Literal)value).literal.GetExpression(program).Statement=this;
					//((Literal)value).literal.Compile(program);
				}
			// we could do this, too:
			//if (k != null && k.Equals(CodeKeys.Function)) {
			//    if (value is Literal) {
			//        if(program.statementList.Count == 1) {
			//            ((Literal)value).literal.Compile(program);
			//        }
			//    }
			//}
			Compiled s=key.Compile();
			//try {
			// refactor, remove key.Source.Start
				return new CompiledStatement(key.Source.Start, value.Source.End, value.Compile(), delegate(ref Map context, Map v) {
					context[s(context)] = v;
				});
			//}
			//catch (Exception e) {
			//    return new CompiledStatement(key.Source.Start, value.Source.End, value.Compile(), delegate(ref Map context, Map v) {
			//        context[s(context)] = v;
			//    });
			//}
		}
		public Expression key;
		public KeyStatement(Expression key, Expression value, Program program, int index)
			: base(program, value, index) {
			this.key = key;
			key.Statement = this;
		}
	}
	public class CurrentStatement : Statement {
		//protected override Map CurrentImplementation(Map previous) {
		//    return value.EvaluateStructure();
		//}
		public override CompiledStatement Compile() {
			return new CompiledStatement(value.Source.Start,value.Source.End,value.Compile(), delegate(ref Map context, Map v) {
				if (this.Index == 0) {
					if (!(v is DictionaryMap && context is DictionaryMap)) {
					    Map scope=context.Scope;
					    context = v.Copy();
						context.Scope = scope;
					}
					else {
						context.CopyInternal(v);
					}
				}
				else {
					context = v;
				}
			});
		}
		public CurrentStatement(Expression current,Expression value, Program program, int index): base(program, value, index) {
		}
	}
	public class SearchStatement : Statement {
		//protected override Map CurrentImplementation(Map previous) {
		//    return previous;
		//}
		public override CompiledStatement Compile() {
			Compiled k = key.Compile();
			return new CompiledStatement(key.Source.Start,value.Source.End,value.Compile(), delegate(ref Map context, Map v) {
				Map selected = context;
				Map eKey = k(context);
				while (!selected.ContainsKey(eKey)) {
					selected = selected.Scope;
					if (selected == null) {
						throw new KeyNotFound(eKey, key.Source.Start, null);
					}
				}
				selected[eKey] = v;
			});
		}
		private Expression key;
		public SearchStatement(Expression key, Expression value, Program program, int index) : base(program, value, index) {
			this.key = key;
			key.Statement = this;
		}
	}
	// derive Literal from Constant???
	public class Literal : Expression {
		private int index = -1;
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		public Map literal;
		public override Compiled GetCompiled(Expression parent) {
			return delegate(Map context) {
				return literal.Copy(context);
			};
		}
		public Literal(Map code)
			: base(code) {
			this.literal = code;
		}
		//public Literal(Map code, Expression parent): base(code.Source, parent) {
		//    this.literal = code;
		//}
	}
	// derive root from Constant
	public class Root : Expression {
		public Root(Map code)
			: base(code) {
		//public Root(Map code, Expression parent): base(code.Source, parent) {
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				return Gac.gac;
			};
		}
	}
	// implement constant optimization for select
	public class Select : Expression {
		//public override Map GetStructure() {
		//    // maybe wrong
		//    Map selected = subs[0].GetStructure();
		//    for (int i = 1; i < subs.Count; i++) {
		//        Map key = subs[i].GetConstant();
		//        if (selected == null || key == null || !selected.ContainsKey(key)) {
		//            return null;
		//        }
		//        selected = selected[key];
		//    }
		//    return selected;
		//}
		public override Compiled GetCompiled(Expression parent) {
			List<Compiled> s = subs.ConvertAll<Compiled>(delegate(Expression e) { return e.Compile(); });
			return delegate(Map context) {
				Map selected = s[0](context);
				for (int i = 1; i < s.Count; i++) {
					Map key = s[i](context);
					Map value = selected[key];
					if (value == null) {
						throw new KeyDoesNotExist(key, Source != null ? Source.Start : null, selected);
					}
					else {
						selected = value;
					}
				}
				return selected;
			};
		}
		public List<Expression> subs = new List<Expression>();
		public Select(Map code): base(code) {
		//public Select(Map code, Expression parent): base(code.Source, parent) {
			foreach (Map m in code.Array) {
				subs.Add(m.GetExpression(this));
			}
		}
	}
	public class Emitter {
		public void MarkLabel(Label label) {
			il.MarkLabel(label);
		}
		public void Duplicate() {
			il.Emit(OpCodes.Dup);
		}
		public void BranchTrue(Label label) {
			il.Emit(OpCodes.Brtrue, label);
		}
		public void Pop() {
			il.Emit(OpCodes.Pop);
		}
		public Label DefineLabel() {
			return il.DefineLabel();
		}
		public void Box(Type type) {
			il.Emit(OpCodes.Box, type);
		}
		public void StoreLocal(LocalBuilder local) {
			StoreLocal(local.LocalIndex);
		}
		public void StoreLocal(int index) {
			il.Emit(OpCodes.Stloc, index);
		}
		public void LoadLocal(LocalBuilder local) {
			LoadLocal(local.LocalIndex);
		}
		public void LoadLocal(int index) {
			il.Emit(OpCodes.Ldloc, index);
		}
		public void Break(Label label) {
			il.Emit(OpCodes.Br, label);
		}
		public void Emit(OpCode code, int operand) {
			il.Emit(code, operand);
		}
		public LocalBuilder DeclareLocal(Type type) {
			return il.DeclareLocal(type);
		}
		public void Emit(OpCode code) {
			il.Emit(code);
		}
		public void BreakFalse(Label label) {
			il.Emit(OpCodes.Brfalse, label);
		}
		public void BreakTrue(Label label) {
			il.Emit(OpCodes.Brtrue, label);
		}
		public void Return() {
			il.Emit(OpCodes.Ret);
		}
		public void UnboxAny(Type type) {
			il.Emit(OpCodes.Unbox_Any, type);
		}
		public void NewArray(Type type) {
			il.Emit(OpCodes.Newarr, type);
		}
		public void LoadConstant(int i) {
			il.Emit(OpCodes.Ldc_I4, i);
		}
		public void NewObject(Type type) {
			NewObject(type, new Type[] { });
		}
		public void NewObject(Type type, params Type[] parameters) {
			NewObject(type.GetConstructor(parameters));
		}
		public void NewObject(ConstructorInfo constructor) {
			il.Emit(OpCodes.Newobj, constructor);
		}
		public void Emit(OpCode code, byte b) {
			il.Emit(code, b);
		}
		public void CastClass(Type type) {
			il.Emit(OpCodes.Castclass, type);
		}
		public void Call(Type type, string method) {
			Call(type.GetMethod(method));
		}
		public void Call(Type type, string method, params Type[] parameters) {
			Call(type.GetMethod(method, parameters));
		}
		public void Call(MethodInfo method) {
			il.Emit(
				method.IsStatic ?
					OpCodes.Call :
					OpCodes.Callvirt,
				method);
		}
		public void LoadField(Type type, string field) {
			LoadField(type.GetField(field));
		}
		public void LoadField(FieldInfo field) {
			il.Emit(
				field.IsStatic ?
					OpCodes.Ldsfld :
					OpCodes.Ldfld,
				field);
		}
		public void StoreElementReference() {
			il.Emit(OpCodes.Stelem_Ref);
		}
		public void LoadArgument(int index) {
			il.Emit(OpCodes.Ldarg, index);
		}
		DynamicMethod dynamicMethod;
		public ILGenerator il;
		private StaticEmitter emitter;
		private Type type;
		public Emitter(Type type) {
			this.type = type;
			MethodInfo invoke = type.GetMethod("Invoke");
			List<Type> parameters = new List<ParameterInfo>(invoke.GetParameters()).ConvertAll<Type>(delegate(ParameterInfo p) { return p.ParameterType; });
			this.dynamicMethod = new DynamicMethod("Optimized", invoke.ReturnType, parameters.ToArray(), typeof(Map).Module);
			this.il = dynamicMethod.GetILGenerator();
		}
		private AssemblyBuilder ab;
		private TypeBuilder tb;
		const string assemblyPath = @"d:\meta\bin\Release\";
		public Emitter(Type type, Type instance) {
			this.type = type;

			MethodInfo invoke = type.GetMethod("Invoke");
			List<Type> parameters = new List<ParameterInfo>(invoke.GetParameters()).ConvertAll<Type>(delegate(ParameterInfo p) { return p.ParameterType; });
			parameters.Insert(0, instance);

			AppDomain cd = System.Threading.Thread.GetDomain();
			AssemblyName an = new AssemblyName();
			an.Name = "HelloClass";
			ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave, assemblyPath);
			ModuleBuilder mb = ab.DefineDynamicModule("HelloModule", "HelloModule.dll", true);
			tb = mb.DefineType("Hello", TypeAttributes.Class | TypeAttributes.Public);
			MethodBuilder meth = tb.DefineMethod("HelloWorld", MethodAttributes.Public | MethodAttributes.Static, invoke.ReturnType, parameters.ToArray());
			il = meth.GetILGenerator();
			this.dynamicMethod = new DynamicMethod("Optimized", invoke.ReturnType, parameters.ToArray(), typeof(Map).Module);
		}
		public object GetDelegate() {
			return dynamicMethod.CreateDelegate(type);
		}
		public string DynamicPath {
			get {
				return Path.Combine(assemblyPath, "MetaDynamic.dll");
			}
		}
		public string ModulePath {
			get {
				return Path.Combine(assemblyPath, "HelloModule.dll");
			}
		}
		public object GetDelegate(object instance) {
			Type myType = tb.CreateType();
			ab.Save("MetaDynamic.dll");
			Delegate del = Delegate.CreateDelegate(type, instance, myType.GetMethod("HelloWorld"));
			ProcessStartInfo start = new ProcessStartInfo(@"C:\Programme\Microsoft Visual Studio 8\Common7\IDE\peverify.exe", DynamicPath);
			start.UseShellExecute = false;
			start.RedirectStandardError = true;
			start.RedirectStandardInput = true;
			start.RedirectStandardOutput = true;
			start.CreateNoWindow = true;
			Process process = Process.Start(start);
			StreamReader reader = process.StandardOutput;
			Directory.SetCurrentDirectory(assemblyPath);
			process.WaitForExit();
			if (process.ExitCode == 1) {
				Console.WriteLine(reader.ReadToEnd());
				Process.Start(@"C:\Programme\Microsoft Visual Studio 8\SDK\v2.0\Bin\ildasm.exe", ModulePath);
				throw new Exception("code generation error");
			}
			return del;
		}
	}
	public class StaticEmitter {
		public ILGenerator il;
		private AssemblyBuilder ab;
		private TypeBuilder tb;
		public StaticEmitter(Type returnType, Type[] parameters) {
			AppDomain cd = System.Threading.Thread.GetDomain();
			AssemblyName an = new AssemblyName();
			an.Name = "HelloClass";
			ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder mb = ab.DefineDynamicModule("HelloModule", "HelloModule.dll", true);
			tb = mb.DefineType("Hello", TypeAttributes.Class | TypeAttributes.Public);
			MethodBuilder meth = tb.DefineMethod("HelloWorld", MethodAttributes.Public | MethodAttributes.Static, returnType, parameters);
			il = meth.GetILGenerator();
		}
		public void Generate() {
			Type myType = tb.CreateType();
			ab.Save("MetaDynamic.dll");
			return;
		}
	}
	public delegate void DebugDelegate(Map context);
	public class Interpreter {
		public static bool stopping = false;
		public static Application Application {
			get {
				return application;
			}
			set {
				application = value;
			}
		}
		private static Application application;
		public static DebugDelegate Breakpoint;
		public static List<Source> breakpoints = new List<Source>();
		public static string LibraryPath {
			get {
				return Path.Combine(Interpreter.InstallationPath, @"library.meta");
			}
		}
		public static Map Run(string path, Map argument) {
			// refactor
			Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
			Map callable = Parser.Parse(path);
			callable.Scope = Gac.gac["library"];
			LiteralExpression gac = new LiteralExpression(Gac.gac);
			//LiteralExpression gac = new LiteralExpression(Gac.gac, null);
			LiteralExpression lib = new LiteralExpression(Gac.gac["library"]);
			lib.Parent = gac;
			//LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
			lib.Statement = new LiteralStatement(gac);
			callable.GetExpression(lib).Statement = new LiteralStatement(lib);
			Gac.gac.Scope = new DirectoryMap(Path.GetDirectoryName(path));
			return callable.Call(argument);
		}
		public static bool profiling = false;
		static Interpreter() {
			// refactor
			Map map = Parser.Parse(LibraryPath);
			map.Scope = Gac.gac;
			LiteralExpression gac = new LiteralExpression(Gac.gac);
			//LiteralExpression gac = new LiteralExpression(Gac.gac, null);
			map.GetExpression(gac).Statement = new LiteralStatement(gac);
			Gac.gac["library"] = map.Call(new DictionaryMap());
			Gac.gac["library"].Scope = Gac.gac;
		}
		public static int Fibo(int x) {
			if (x < 2) {
				return 1;
			}
			else {
				return Fibo(x - 1) + Fibo(x - 2);
			}
		}
		public static Number Fibo(Number x) {
			if (x.LessThan(SmallInteger.Two)) {
				return SmallInteger.One;
			}
			else {
				return Fibo(x.Subtract(SmallInteger.One)).Add(Fibo(x.Subtract(SmallInteger.Two)));
			}
		}
		[STAThread]
		public static void Main(string[] args) {
			//Map.arguments.Push(Map.Empty);
			DateTime start = DateTime.Now;
			//Fibo(new Integer32(32));
			//Console.WriteLine((DateTime.Now - start).TotalSeconds);
			//return;
			if (args.Length != 0) {
				if (args[0] == "-test") {
					try {
						UseConsole();
						new Test.MetaTest().Run();
					}
					catch (Exception e) {
						string text = e.ToString();
						if(text.Length>1000) {
							text=text.Substring(0,1000)+"...";
						}
						DebugPrint(text);
					}
				}
				else if (args[0] == "-nprof") {
					Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), new DictionaryMap());
				}
				else if (args[0] == "-profile") {
					UseConsole();
					Interpreter.profiling = true;
					Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"game.meta"), new DictionaryMap());
					List<object> results = new List<object>(Map.calls.Keys);
					results.Sort(delegate(object a, object b) {
						return Map.calls[b].time.CompareTo(Map.calls[a].time);});
					foreach (object e in results) {
						Console.WriteLine(e.ToString() + "    " + Map.calls[e].time + "     " + Map.calls[e].calls);
					}
				}
				else if (args[0] == "-performance") {
					UseConsole();
					Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), new DictionaryMap());
				}
				else {
					string fileName = args[0].Trim('"');
					if (File.Exists(fileName)) {
						try {
							Interpreter.Run(fileName, new DictionaryMap());
						}
						catch (Exception e) {
							Console.WriteLine(e.ToString());
							Console.ReadLine();
						}
					}
					else {
						Console.WriteLine("File " + fileName + " not found.");
					}
				}
			}
			Console.WriteLine((DateTime.Now - start).TotalSeconds);
		}
		private static void DebugPrint(string text) {
			if (useConsole) {
				Console.WriteLine(text);
				Console.ReadLine();
			}
			else {
				System.Windows.Forms.MessageBox.Show(text, "Meta exception");
			}
		}
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();
		public static bool useConsole = false;
		public static void UseConsole() {
			if (!useConsole) {
				AllocConsole();
			}
			useConsole = true;
		}
		public static string InstallationPath {
			get {
				return @"D:\Meta\";
			}
		}
	}
	public class Transform {
		public static Dictionary<int, Type> types = new Dictionary<int, Type>();
		public static Delegate CreateDelegateFromCode(Map code, int typeToken) {
			Type type = types[typeToken];
			return CreateDelegateFromCode(code, type);
		}
		public static Delegate CreateDelegateFromCode(Map code, Type delegateType) {
			MethodInfo invoke = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			Emitter il = new Emitter(delegateType, typeof(Map));
			LocalBuilder current = il.DeclareLocal(typeof(Map));
			il.LoadArgument(0);
			il.StoreLocal(current);

			for (int i = 0; i < parameters.Length; i++) {
				il.LoadLocal(current);
				il.LoadArgument(i+1);
				Transform.GetMetaConversion(parameters[i].ParameterType, il);
				il.Call(typeof(Map), "Call");
				il.StoreLocal(current);
			}

			if (invoke.ReturnType == typeof(void)) {
				il.Return();
			}
			else {
				il.LoadLocal(current);
				Transform.GetDotNetConversion(invoke.ReturnType, il);
				il.Return();
			}
			return (Delegate)il.GetDelegate(code);
		}
		// think about GetNumber().GetInt32() or GetInt32(), maybe differentiate between lossy and lossless conversions
		public static void GetMetaConversion(Type type, Emitter il) {
			Label end=il.DefineLabel();
			if (type.Equals(typeof(void))) {
				il.NewObject(typeof(DictionaryMap));
			}
			else if (!type.IsSubclassOf(typeof(Map)) && !type.Equals(typeof(Map))) {
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Boolean:
						il.Call(typeof(Convert),"ToInt32", typeof(Boolean));
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.Byte:
						il.Call(typeof(Convert),"ToInt32",typeof(Byte));
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.Char:
						il.Call(typeof(Convert),"ToInt32", typeof(Char));
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.SByte:
						il.Call(typeof(Convert),"ToInt32", typeof(SByte));
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.Single:
						il.NewObject(typeof(Rational),typeof(double));
						break;
					case TypeCode.UInt16:
						il.Call(typeof(Convert),"ToInt32", typeof(UInt16));
						il.NewObject(typeof(SmallInteger),new Type[] { typeof(int) });
						break;
					case TypeCode.UInt32:
						il.Call(typeof(Convert),"ToString", typeof(UInt32) );
						il.NewObject(typeof(LargeInteger),typeof(string));
						break;
					case TypeCode.UInt64:
						il.Call(typeof(Convert),"ToString", typeof(UInt64));
						il.NewObject(typeof(LargeInteger),typeof(string));
						break;
					case TypeCode.String:
						il.NewObject(typeof(StringMap),typeof(string));
						break;
					case TypeCode.Decimal:
						il.Call(typeof(Convert),"ToDouble",typeof(Decimal));
						il.NewObject(typeof(Rational),typeof(double));
						break;
					case TypeCode.Double:
						il.NewObject(typeof(Rational),typeof(double));
						break;
					case TypeCode.Int16:
						il.Call(typeof(Convert),"ToInt32", typeof(Int16));
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.Int32:
						il.NewObject(typeof(SmallInteger),typeof(int));
						break;
					case TypeCode.Int64:
						il.Call(typeof(Convert),"ToString", typeof(Int64));
						il.NewObject(typeof(LargeInteger),typeof(string));
						break;
					case TypeCode.DateTime:
					case TypeCode.DBNull:
					case TypeCode.Object:
						if (type.Equals(typeof(void))) {
							il.LoadField(typeof(Map),"Empty");
						}
						else {
							if (!type.IsValueType) {
								Label label = il.DefineLabel();
								il.Duplicate();
								il.BranchTrue(label);
								il.Pop();
								il.LoadField(typeof(Map),"Empty");
								il.Break(end);
								il.MarkLabel(label);
							}
							if (type.IsValueType) {
								il.Box(type);
							}
							il.NewObject(typeof(ObjectMap),typeof(object));
						}
						break;
					default:
						throw new ApplicationException("Cannot convert object.");
					}
				}
				il.MarkLabel(end);
			}
		public static Dictionary<Type, MetaConversion> metaConversions = new Dictionary<Type, MetaConversion>();
		public static MetaConversion GetMetaConversion(Type type) {
			MetaConversion conversion;
			if (!metaConversions.TryGetValue(type, out conversion)) {
				Emitter il = new Emitter(typeof(MetaConversion));
				il.LoadArgument(0);
				if (type.IsValueType) {
					il.UnboxAny(type);
				}
				GetMetaConversion(type, il);
				il.Return();
				conversion = (MetaConversion) il.GetDelegate();
				metaConversions[type] = conversion;
			}
			return conversion;
		}
		public static Map ToMeta(object dotNet) {
			if (dotNet == null) {
				return Map.Empty;
			}
			else {
				return GetMetaConversion(dotNet.GetType())(dotNet);
			}
		}
		public static bool GetDotNetConversion(Type target, Emitter il) {
			if (target.IsEnum) {
				int token = (int)target.TypeHandle.Value;
				if (!Transform.types.ContainsKey(token)) {
					Transform.types[token] = target;
				}
				il.Call(typeof(Map),"GetInt32");
			}
			else if (target.Equals(typeof(double))) {
				il.Call(typeof(Map),"GetNumber");
				il.Call(typeof(Number),"GetDouble");
			}
			else if (target.Equals(typeof(Number))) {
				il.Call(typeof(Map),"GetNumber");
			}
			else if (target.Equals(typeof(Stream))) {
				il.CastClass(typeof(FileMap));
				il.Call(typeof(FileMap),"GetStream");
			}
			else if (target.Equals(typeof(Map))) {
			}
			else if (target.Equals(typeof(Boolean))) {
				il.Call(typeof(Map),"GetInt32");
				il.Call(typeof(Convert),"ToBoolean", typeof(int));
			}
			else if (target.Equals(typeof(String))) {
				il.Call(typeof(Map),"GetString");
			}
			else if (target.Equals(typeof(int))) {
				il.Call(typeof(Map), "GetInt32");
			}
			else if (target.Equals(typeof(decimal))) {
				il.Call(typeof(Map),"GetNumber");
				il.Call(typeof(Number),"GetInt64");
				il.Call(typeof(Convert), "ToDecimal", typeof(long));
			}
			else if (target.Equals(typeof(Single))) {
				il.Call(typeof(Map),"GetNumber");
				il.Call(typeof(Number),"GetSingle");
			}
			else if (target.Equals(typeof(object))) {
			}
			else if (target.Equals(typeof(Type))) {
				il.Call(typeof(Map),"GetClass");
			}
			else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))) {
				int token = (int)target.TypeHandle.Value;
				if (!Transform.types.ContainsKey(token)) {
					Transform.types[token] = target;
				}
				
				il.LoadConstant((int)token);
				il.Call(typeof(Transform),"CreateDelegateFromCode", typeof(Map), typeof(int));
			}
			else {
				il.CastClass(typeof(ObjectMap));
				il.Call(typeof(ObjectMap),"get_Object");
				il.CastClass(target);
				if (target.IsValueType) {
					il.UnboxAny(target);
				}
			}
			return true;
		}
		public static Dictionary<Type,DotNetConversion> dotNetConversions=new Dictionary<Type,DotNetConversion>();
		public static DotNetConversion GetDotNetConversion(Type type) {
			DotNetConversion conversion;
			if (!dotNetConversions.TryGetValue(type, out conversion)) {
				Emitter emitter = new Emitter(typeof(DotNetConversion));
				emitter.LoadArgument(0);
				GetDotNetConversion(type, emitter);
				if (type.IsValueType) {
					emitter.Box(type);
				}
				emitter.Return();
				conversion = (DotNetConversion) emitter.GetDelegate();
				dotNetConversions[type] = conversion;
			}
			return conversion;
		}
		public static object ToDotNet(Map meta, Type type) {
			return GetDotNetConversion(type)(meta);
		}
	}
	public delegate Map CallDelegate(Map argument);
	public class Method : Map {
		public override void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override IEnumerable<Map> Array {
			get { 
				yield break;
			}
		}
		public override int Count {
			get { 
				return 0;
			}
		}
		public override Number GetNumber() {
			return null;
		}
		public override string GetString() {
			return null;
		}
		public override string Serialize() {
			return method.ToString();
		}
		public override int GetHashCode() {
			return method.GetHashCode();
		}
		public override bool Equals(object obj) {
			Method method = obj as Method;
			if (method != null) {
				return this.method.Equals(method.method);
			}
			return false;
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return false;
		}
		public override IEnumerable<Map> Keys {
			get {
				yield break;
			}
		}
		public override Map this[Map key] {
			get {
				return null;
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override Map Copy() {
			return this;
		}
		public MethodBase method;
		protected object obj;
		protected Type type;
		public MemberInfo original;
		public Method(MethodBase method, object obj, Type type,MemberInfo original) {
			this.method = method;
			this.obj = obj;
			this.type = type;
			this.parameters = method.GetParameters();
			this.original = original;
			if (method.IsConstructor) {
				this.returnType = method.ReflectedType;
			}
			else {
				this.returnType = ((MethodInfo)method).ReturnType;
			}
			this.metaConversion = Transform.GetMetaConversion(returnType);
			foreach (ParameterInfo parameter in parameters) {
				conversions.Add(Transform.GetDotNetConversion(parameter.ParameterType));
			}
		}
		private List<DotNetConversion> conversions=new List<DotNetConversion>();
		private MetaConversion metaConversion;
		public ParameterInfo[] parameters;
		public override Map Call(Map argument) {
		    return DecideCall(argument, new List<object>());
		}
		private Map DecideCall(Map argument, List<object> oldArguments) {
			List<object> arguments = new List<object>(oldArguments);
			if (parameters.Length != 0) {
				arguments.Add(conversions[arguments.Count](argument));
			}
			if (arguments.Count >= parameters.Length) {
				return Invoke(argument, arguments.ToArray());
			}
			else {
				CallDelegate call = new CallDelegate(delegate(Map map) {
					return DecideCall(map, arguments);
				});
				return new Method(invokeMethod, call, typeof(CallDelegate), method);
			}
		}
		Type returnType;
		MethodInfo invokeMethod = typeof(CallDelegate).GetMethod("Invoke");
		private Map Invoke(Map argument, object[] arguments) {
			object result;
			if (method is ConstructorInfo) {
				result = ((ConstructorInfo)method).Invoke(arguments);
			}
			else {
				result = method.Invoke(obj, arguments);
			}
			if (result == null) {
				return Map.Empty;
			}
			else {
				return metaConversion(result);
			}
		}
	}
	public class TypeMap : DotNetMap {
		public override Type GetClass() {
			return this.Type;
		}
		public static MemberCache cache = new MemberCache(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
		protected override MemberCache MemberCache {
			get {
				return cache;
			}
		}
		public override string ToString() {
			return Type.ToString();
		}
		protected override object GlobalKey {
			get {
				return this;
			}
		}
		public TypeMap(Type type): base(null, type) {
		}
		public override bool ContainsKey(Map key) {
			return this[key] != null;
		}
		public override Map this[Map key] {
			get {
				if (Type.IsGenericTypeDefinition && key is TypeMap) {
					List<Type> types = new List<Type>();
					if (Type.GetGenericArguments().Length == 1) {
						types.Add(((TypeMap)key).Type);
					}
					else {
						foreach (Map map in key.Array) {
							types.Add(((TypeMap)map).Type);
						}
					}
					return new TypeMap(Type.MakeGenericType(types.ToArray()));
				}
				else if (Type == typeof(Array) && key is TypeMap) {
					return new TypeMap(((TypeMap)key).Type.MakeArrayType());
				}
				else if (base[key] != null) {
					return base[key];
				}
				else {
					return null;
				}
			}
		}
		public static string GetConstructorName(ConstructorInfo constructor) {
			string name = constructor.DeclaringType.Name;
			foreach (ParameterInfo parameter in constructor.GetParameters()) {
				name += "_" + parameter.ParameterType.Name;}
			return name;
		}
		public override Map Copy() {
			return new TypeMap(this.Type);
		}
		private Method constructor;
		public Method Constructor {
			get {
				if (constructor == null) {
					ConstructorInfo method = Type.GetConstructor(new Type[] {});
					if (method != null) {
						constructor = new Method(method, Object, Type, method);
					}
				}
				return constructor;
			}
		}
		public override Map Call(Map argument) {
			// special case for debugging in WPF-Editor
			if (this.Type.Equals(typeof(Application)) && Interpreter.Application!=null) {
				return Library.With(new ObjectMap(Interpreter.Application), argument);
			}
			if (Constructor != null) {
				return Library.With(Constructor.Call(new DictionaryMap()), argument);
			}
			else {
				int asdf = 0;
				throw new MetaException("Type does not have a default constructor.", null);
			}
		}
	}
	public class ObjectMap : DotNetMap {
		public override object GetObject() {
			return Object;
		}
		public static MemberCache cache = new MemberCache(BindingFlags.Public | BindingFlags.Instance);
		protected override MemberCache MemberCache {
			get {
				return cache;
			}
		}
		protected override object GlobalKey {
			get {
				return Object;
			}
		}
		public override Map Call(Map arg) {
			if (this.Type.IsSubclassOf(typeof(Delegate))) {
				MethodInfo invoke=Type.GetMethod("Invoke");
				return new Method(invoke, this.Object, this.Type, invoke).Call(arg);
			}
			else {
				throw new Exception("The object is not callable.");
			}
		}
		public override int GetHashCode() {
			return Object.GetHashCode();
		}
		public override bool Equals(object o) {
			return o is ObjectMap && Object.Equals(((ObjectMap)o).Object);
		}
		public ObjectMap(string text): this(text, text.GetType()) {}
		public ObjectMap(Map target): this(target, target.GetType()) {}
		public ObjectMap(object target, Type type): base(target, type) {
		}
		public ObjectMap(object target): base(target, target.GetType()) {
		}
		public override string ToString() {
			return Object.ToString();
		}
		public override Map Copy() {
			return this;
		}
	}
	public class DirectoryMap : ScopeMap {
		public override int Count {
			get {
				return new List<Map>(Keys).Count;
			}
		}
		// rethink behaviour of file system, and whether it is necessary at all
		public override Map this[Map key] {
			get {
				if (ContainsKey(key)) {
					string p=Path.Combine(path,key.GetString());
					string dll = Path.Combine(path,key.GetString() + ".dll");
					if (File.Exists(dll)) {
						return new AssemblyMap(Assembly.LoadFrom(dll));
					}
					if (File.Exists(p)) {
						return new FileMap(p);
					}
					else if (Directory.Exists(p)) {
						return new DirectoryMap(p);
					}
				}
				return null;
			}
			set {
				throw new ApplicationException("Cannot set key in directory.");
			}
		}

		private List<Map> keys;
		public override IEnumerable<Map> Keys {
			get {
				if (keys == null) {
					keys = new List<Map>();
					foreach (string dll in Directory.GetFiles(path, "*.dll")) {
						Assembly assembly = null;
						try {
							assembly = Assembly.LoadFrom(dll);
						}
						catch (Exception e) {
						};
						if (assembly != null) {
							keys.Add(Path.GetFileNameWithoutExtension(dll));
						}
					}
					foreach (string file in Directory.GetFiles(path)) {
						keys.Add(Path.GetFileName(file));
					}
					foreach (string directory in Directory.GetDirectories(path)) {
						keys.Add(new DirectoryInfo(directory).Name);
					}
				}
				return keys;
			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public readonly string path;
		public DirectoryMap(string path) {
			this.path = path;
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return new List<Map>(Keys).Contains(key);
		}
		public override Number GetNumber() {
			return null;
		}
		public override Map Copy() {
			return CopyMap(this);
		}
	}
	// does integrating the file system make sense?
	public class FileMap : ScopeMap {
		public override int Count {
			get { 
				throw new Exception("The method or operation is not implemented."); 
			}
		}
		public Stream GetStream() {
			return File.Open(path, FileMode.Open);
		}
		public override Map this[Map key] {
			get {
				return null;
			}
			set {
				throw new Exception("Cannot set key in file.");
			}
		}
		public string Path {
			get {
				return path;
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				yield break;
			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return false;
		}
		public override Number GetNumber() {
			return null;
		}
		public override Map Copy() {
			return this;
		}
		public readonly string path;
		public FileMap(string path) {
			this.path = path;
		}
	}
	public abstract class ScopeMap : Map {
		protected Map scope;
		public override Map Scope {
			get {
				return scope;
			}
			set {
				scope = value;
			}
		}
	}
	// ideally, would not be necessary
	// in general, cut back on the number of map types
	public class CopyMap : ScopeMap {
		public override Expression Optimized {
			get {
				if (copy.Optimized != null) {
					return copy.Optimized;
				}
				return base.Optimized;
			}
			set {
				base.Optimized = value;
				copy.Optimized = value;
			}
		}
		//public override bool FirstCall {
		//    get {
		//        return copy.FirstCall||_firstCall;
		//    }
		//    set {
		//        _firstCall = value;
		//    }
		//}
		public override Extent Source {
			get {
				return copy.Source;
			}
			set {
				throw new Exception("not implemented");
			}
		}
		public override Map this[Map key] {
			get {
				return copy[key];
			}
			set {
				copy[key] = value;
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				return copy.Keys;
			}
		}
		public override IEnumerable<Map> Array {
			get {
				return copy.Array;
			}
		}
		public override int Count {
			get {
				return copy.Count;
			}
		}
		public override int ArrayCount {
			get {
				return copy.ArrayCount;
			}
		}
		public override Map Copy() {
			return this;
		}
		private Map copy;
		public CopyMap(Map scope, Map original) {
			this.scope = scope;
			this.copy = original;
		}
		private Expression expression;
		public override Expression Expression {
			get {
				if (expression != null) {
					return expression;
				}
				else {
					return copy.Expression;
				}
			}
			set {
				expression = value;
			}
		}
		public override Number GetNumber() {
			return copy.GetNumber();
		}
		public override bool ContainsKey(Map key) {
			return copy.ContainsKey(key);
		}
	}
	public class Mapping {
		public Dictionary<Map, int> mapping = new Dictionary<Map, int>();
		public Mapping(IEnumerable<Map> keys) {
			int index=0;
			foreach (Map key in keys) {
				// make sure keys are unique?
				mapping[key]=index;
				index++;
			}
		}
	}
	// maybe completely unnecessary
	// preferably get the performance by proving that dictionary is not necessary
	public class PerfectMap : DictionaryBaseMap {
		public override Map GetFromIndex(int i) {
			return values[i];
		}
		public Map[] values;
		public override Map this[Map key] {
			get {
				int index;
				if(!mapping.mapping.TryGetValue(key,out index)) {
					return null;
					//throw new KeyDoesNotExist(key,this.Source.Start,this);
				}
				return values[index];
			}
			set {
				values[mapping.mapping[key]] = value;
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				foreach (Map key in mapping.mapping.Keys) {
					if (values[mapping.mapping[key]] != null) {
						yield return key;
					}
				}
			}
		}
		public override bool ContainsKey(Map key) {
			return mapping.mapping.ContainsKey(key) && values[mapping.mapping[key]] != null;
		}
		public override int Count {
			get {
				int i = 0;
				for (; i < values.Length; i++) {
					if (values[i] == null) {
						break;
					}
				}
				return i;
			}
		}
		private Mapping mapping;
		public PerfectMap(Mapping mapping) {
			this.mapping = mapping;
			this.values = new Map[mapping.mapping.Count];
		}
		public override Map Copy() {
			return new CopyMap(this.scope, this);
		}
	}
	public abstract class DictionaryBaseMap : ScopeMap {
		// put into Map
		// only an abstract method so one does not forget to implement it
		public override IEnumerable<Map> Array {
			get {
				for (int i = 1; ; i++) {
					Map m = this[i];
					if (m != null) {
						yield return m;
					}
					else {
						yield break;
					}
				}
			}
		}
		// should this not be in Map?
		public override Number GetNumber() {
			if (Count == 0) {
				return new SmallInteger(0);
			}
			else if (this.Count == 1) {
				if (this.ContainsKey(Map.Empty)) {
					if (this[Map.Empty].GetNumber()!=null) {
						return this[Map.Empty].GetNumber().Add(new SmallInteger(1));
					}
				}
			}
			return null;
		}
		public override int ArrayCount {
			get {
				int i = 1;
				while (this.ContainsKey(i)) {
					i++;
				}
				return i - 1;
			}
		}
	}
	public class DictionaryMap : DictionaryBaseMap {
		// put this into some base class?
		private Expression expression;
		public override Expression Expression {
			get {
				//if (expression == null) {
				//    expression = GetExpression();
				//}
				return expression;
			}
			set {
				expression = value;
			}
		}
		public override void CopyInternal(Map map) {
			foreach (Map key in map.Keys) {
				this.dictionary[key] = map[key];
			}
		}
		public override Extent Source {
			get {
				return source;
			}
			set {
				source=value;
			}
		}
		private Extent source;
		public override bool Equals(object obj) {
			Map map=obj as Map;
			if (map!=null && map.Count==Count) {
				foreach (Map key in this.Keys) {
					Map otherValue = map[key];
					Map thisValue = this[key];
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue)) {
						return false;
					}
				}
				return true;
			}
			return false;
		}
		public DictionaryMap() {
			this.dictionary = new Dictionary<Map, Map>();
		}
		public DictionaryMap(int capacity) {
			this.dictionary=new Dictionary<Map, Map>(capacity);
		}
		public DictionaryMap(Dictionary<Map, Map> dictionary) {
			this.dictionary = dictionary;
		}
		public DictionaryMap(params Map[] keysAndValues):this(keysAndValues.Length/2){
		    for (int i = 0; i <= keysAndValues.Length - 2; i += 2) {
		        this[keysAndValues[i]] = keysAndValues[i + 1];
		    }
		}
		public DictionaryMap(System.Collections.Generic.ICollection<Map> list):this(list.Count) {
			int index = 1;
			foreach (Map entry in list) {
				this[index] = entry;
				index++;
			}
		}
		public DictionaryMap(IEnumerable<Map> list):this() {
			foreach(Map map in list) {
				this.Append(map);
			}
		}
		public override Map Copy(Map scope) {
			return new CopyMap(scope, this);
		}
		public override Map Copy() {
			Map clone = new CopyMap(null, this);
			return clone;
		}
		public override void Append(Map map) {
			this[ArrayCount + 1]=map;
		}
		public override string Serialize() {
			if (this.Count == 0) {
				return "0";
			}
			else if (this.IsString) {
				return "\"" + this.GetString() + "\"";
			}
			else if (this.GetNumber()!=null) {
				return this.GetNumber().ToString();
			}
			return null;
		}
		public Dictionary<Map, Map> dictionary;
		public override Map this[Map key] {
			get {
				Map val;
				dictionary.TryGetValue(key, out val);
				return val;
			}
			set {
				if (value.Scope == null) {
					value.Scope = this;
				}
				dictionary[key] = value;
			}
		}
		public override bool ContainsKey(Map key) {
			return dictionary.ContainsKey(key);
		}
		public override IEnumerable<Map> Keys {
			get {
				return dictionary.Keys;
			}
		}
		public override int Count {
			get {
				return dictionary.Count;
			}
		}
	}
	public class Profile {
		public double time;
		public int calls;
		public int recursive;
	}
	public abstract class Member {
		public abstract MemberInfo Original {
			get;
		}
		public abstract void Set(object obj, Map value);
		public abstract Map Get(object obj);
	}
	public class TypeMember : Member {
		public override MemberInfo Original {
			get {
				return type;
			}
		}
		public override void Set(object obj, Map value) {
			throw new Exception("The method or operation is not implemented.");
		}
		private Type type;
		public TypeMember(Type type) {
			this.type = type;
		}
		public override Map Get(object obj) {
			return new TypeMap(type);
		}
	}
	public class FieldMember : Member {
		public override MemberInfo Original {
			get {
				return field;
			}
		}
		private FieldInfo field;
		public FieldMember(FieldInfo field) {
			this.field = field;
		}
		public override void Set(object obj, Map value) {
			field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
		}
		public override Map Get(object obj) {
			return Transform.ToMeta(field.GetValue(obj));
		}
	}
	public class MethodMember : Member {
		// is this really needed? only for Editor, maybe overkill
		public override MemberInfo Original {
			get {
				return original;
			}
		}
		private MemberInfo original;
		private MethodBase method;
		public MethodMember(MethodInfo method,MemberInfo original) {
			this.method = method;
			this.original = original;
		}
		public override void Set(object obj, Map value) {
			throw new Exception("The method or operation is not implemented.");}
		public override Map Get(object obj) {
			return new Method(method, obj, method.DeclaringType,original);
		}
	}
	public class MemberCache {
		private BindingFlags bindingFlags;
		public MemberCache(BindingFlags bindingFlags) {
			this.bindingFlags = bindingFlags;
		}
		public Dictionary<Map, Member> GetMembers(Type type) {
			if (!cache.ContainsKey(type)) {
				Dictionary<Map, Member> data = new Dictionary<Map, Member>();
				foreach (MemberInfo member in type.GetMembers(bindingFlags)) {
					MethodInfo method = member as MethodInfo;
					if (method != null) {
						string name = TypeMap.GetMethodName(method);
						data[name] = new MethodMember(method,method);
					}
					FieldInfo field = member as FieldInfo;
					if (field != null && field.IsPublic) {
						data[field.Name] = new FieldMember(field);
					}
					EventInfo e = member as EventInfo;
					if (e != null) {
						MethodInfo add=e.GetAddMethod();
						if (add != null) {
							data[TypeMap.GetMethodName(add)] = new MethodMember(add, e);
						}
					}
					PropertyInfo property = member as PropertyInfo;
					if (property != null) {
						MethodInfo get=property.GetGetMethod();
						if (get != null) {
							data[TypeMap.GetMethodName(get)] = new MethodMember(get,property);
						}
						MethodInfo set = property.GetSetMethod();
						if (set != null) {
							data[TypeMap.GetMethodName(set)] = new MethodMember(set,property);
						}
					}
					Type t = member as Type;
					if (t != null) {
						data[t.Name] = new TypeMember(t);
					}
				}
				cache[type] = data;
			}
			return cache[type];
		}
		private Dictionary<Type, Dictionary<Map, Member>> cache = new Dictionary<Type, Dictionary<Map, Member>>();
	}
	public abstract class DotNetMap : ScopeMap {
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override string GetString() {
			return null;
		}
		public override Number GetNumber() {
			return null;
		}
		public override int GetHashCode() {
			if (obj != null) {
				return obj.GetHashCode();
			}
			return type.GetHashCode();
		}
		public override bool Equals(object obj) {
			DotNetMap dotNet = obj as DotNetMap;
			return dotNet != null && dotNet.Object == Object && dotNet.Type == Type;
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public object Object {
			get {
				return obj;
			}
		}
		public Type Type {
			get {
				return type;
			}
		}
		protected abstract object GlobalKey {
			get;
		}
		public static string GetMethodName(MethodInfo method) {
			string name = method.Name;
			foreach (ParameterInfo parameter in method.GetParameters()) {
				name += "_" + parameter.ParameterType.Name;
			}
			return name;
		}
		private Dictionary<Map, Member> data;
		public Dictionary<Map, Member> Members {
			get {
				if (data == null) {
					data = MemberCache.GetMembers(type);
				}
				return data;
			}
		}
		protected abstract MemberCache MemberCache {
			get;
		}
		private object obj;
		private Type type;
		public DotNetMap(object obj, Type type) {
			this.obj = obj;
			this.type = type;
		}
		public override Map this[Map key] {
			get {
				if (Members.ContainsKey(key)) {
					return Members[key].Get(obj);
				}
				if (global.ContainsKey(GlobalKey) && global[GlobalKey].ContainsKey(key)) {
					return global[GlobalKey][key];
				}
				return null;
			}
			set {
				if (Members.ContainsKey(key)) {
					Members[key].Set(obj, value);
				}
				else {
					if (!global.ContainsKey(GlobalKey)) {
						global[GlobalKey] = new Dictionary<Map, Map>();
					}
					global[GlobalKey][key] = value;
				}
			}
		}
		public static Dictionary<object, Dictionary<Map, Map>> global = new Dictionary<object, Dictionary<Map, Map>>();
		public override bool ContainsKey(Map key) {
			return this[key] != null;
		}
		public override int Count {
			get { 
				return new List<Map>(Keys).Count;
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				foreach (Map key in Members.Keys) {
					yield return key;
				}
				if (global.ContainsKey(GlobalKey)) {
					foreach (Map key in global[GlobalKey].Keys) {
						yield return key;
					}
				}
			}
		}
		public override string Serialize() {
			if (obj != null) {
				return this.obj.ToString();}
			else {
				return this.type.ToString();
			}
		}
	}
	public interface ISerializeEnumerableSpecial {
		string Serialize();
	}
	public class Extent {
		public readonly Source Start;
		public readonly Source End;
		public Extent(Source start, Source end) {
			this.Start = start;
			this.End = end;
		}
		public override int GetHashCode() {
			unchecked {
				return Start.GetHashCode() * End.GetHashCode();
			}
		}
		public override bool Equals(object obj) {
			Extent extent = obj as Extent;
			return extent != null && Start.Equals(extent.Start) && End.Equals(extent.End);
		}
	}
	public class Source {
		public bool Less(Source source) {
			return Line < source.Line || (Line == source.Line && Column < source.Column);
		}
		public bool Greater(Source source) {
			return Line > source.Line || (Line == source.Line && Column > source.Column);
		}
		public override string ToString() {
			return FileName + ", " + "line " + Line + ", column " + Column;
		}
		public readonly int Line;
		public readonly int Column;
		public readonly string FileName;
		public Source(int line, int column, string fileName) {
			this.Line = line;
			this.Column = column;
			this.FileName = fileName;
		}
		public override int GetHashCode() {
			unchecked {
				return Line.GetHashCode() * Column.GetHashCode() * FileName.GetHashCode();
			}
		}
		public int CompareTo(Source source) {
			int line = Line.CompareTo(source.Line);
			if (line != 0) {
				return line;
			}
			else {
				return Column.CompareTo(source.Column);
			}
		}
		public override bool Equals(object obj) {
			Source source = obj as Source;
			return source != null && Line == source.Line && Column == source.Column && FileName == source.FileName;
		}
	}
	public class AssemblyMap : ScopeMap {
		// does lazy loading make sense?
		private Map data;
		public override int Count {
			get {
				return new List<Map>(Keys).Count;
			}
		}
		public override Map this[Map key] {
			get {
				return Data[key];
			}
			set {
				Data[key] = value;
			}
		}
		private Map Data {
			get {
				if (data == null) {
					data = GetData();
				}
				return data;
			}
		}
		public static Map LoadAssembly(Assembly assembly) {
			Map val = new DictionaryMap();
			foreach (Type type in assembly.GetExportedTypes()) {
				if (type.DeclaringType == null) {
					Map selected = val;
					string name;
					if (type.IsGenericTypeDefinition) {
						name = type.Name.Split('`')[0];
					}
					else {
						name = type.Name;
					}
					selected[type.Name] = new TypeMap(type);
					foreach (ConstructorInfo constructor in type.GetConstructors()) {
						if (constructor.GetParameters().Length != 0) {
							selected[TypeMap.GetConstructorName(constructor)] = new Method(constructor, null, type, constructor);
						}
					}
				}
			}
			return val;
		}
		private Map GetData() {
			return LoadAssembly(Assembly);
		}
		public override IEnumerable<Map> Keys {
			get {
				return Data.Keys;
			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return Data.ContainsKey(key);
		}
		public override Number GetNumber() {
			return null;
		}
		public override Map Copy() {
			return CopyMap(this);
		}
		private Assembly assembly;
		public Assembly Assembly {
			get {
				if (assembly == null) {
					assembly = Assembly.LoadWithPartialName(name);
				}
				return assembly;
			}
		}
		private string name;
		public AssemblyMap(Assembly assembly) {
			this.assembly = assembly;
		}
		public AssemblyMap(string name) {
			this.name = name;
		}
	}
	public class Gac : ScopeMap {
		public override int Count {
			get {
				return new List<Map>(Keys).Count;
			}
		}
		List<Map> keys = null;
		public override IEnumerable<Map> Keys {
			get {
				if (keys == null) {
					keys = new List<Map>();
					AssemblyCacheEnum assemblies = new AssemblyCacheEnum(null);
					while (true) {
						string name = assemblies.GetNextAssembly();
						if (name == null) {
							break;
						}
						if (!name.Contains("DirectX")) {
							AssemblyName n = new AssemblyName(name);
							keys.Add(n.Name);
						}
					}
				}
				foreach (Map key in keys) {
					yield return key;
				}
				// do we need this cache? remove if possible
				foreach (Map key in cache.Keys) {
					yield return key;
				}

			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return new List<Map>(Keys).Contains(key);
		}
		// fix copying, in general
		public override Map Copy() {
			Map copy = new DictionaryMap();
			foreach (Map key in Keys) {
				copy[key] = this[key];
			}
			copy.Scope = Scope;
			return copy;
		}
		public override Number GetNumber() {
			return null;
		}
		public static readonly Map gac = new Gac();
		private Gac() {
			cache["Meta"] = new AssemblyMap(Assembly.GetExecutingAssembly());
		}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		public override Map this[Map key] {
			get {
				if (!cache.ContainsKey(key)) {
					if (!ContainsKey(key)) {
						return null;
					}
					cache[key] = new AssemblyMap(key.GetString());
				}
				return cache[key];
			}
			set {
				cache[key] = value;
			}
		}
	}
	// is this necessary?
	// probably, for fast conversion
	public class StringMap : ScopeMap {
		private int hashCode;
		private string text;
		public override int GetHashCode() {
			return hashCode;
		}
		public override Number GetNumber() {
			if (text.Length == 0) {
				return SmallInteger.Zero;
			}
			return null;
		}
		public override string Serialize() {
			return "\""+text+"\"";
		}
		public StringMap(string text) {
			this.text = text;
			if (text.Length == 0) {
				hashCode=0;
			}
			else {
				unchecked {
					hashCode=int.MaxValue / text.Length + (text[0] % int.MaxValue);
				}
			}
		}
		public override Map this[Map key] {
			get {
				// use TryGetInt32
				// use underlying type directly, or as a map???
				// maybe map, but provide integer as property, or so
				// rename SmallInteger to Integer32, maybe, so we can also have an Integer64
				Integer integer = key.GetInteger();
				if (integer!= null) {
					if (Number.Greater(integer, SmallInteger.Zero) && Number.LessEqual(integer, new SmallInteger(Count))) {
						return text[integer.GetInt32() - 1];
					}
				}
				return null;
			}
			// should never be assigned to current in a program!!
			set {
				throw new Exception("not implemented");
			}
		}
		public override bool Equals(object obj) {
			StringMap s = obj as StringMap;
			if (s!=null) {
				return text.Length == s.text.Length && s.text.Equals(text);
			}
			return base.Equals(obj);
		}
		public override int Count {
			get {
				return text.Length;
			}
		}
		public override string GetString() {
			return text;
		}
		public override bool ContainsKey(Map key) {
			// use GetInteger
			if (key.GetNumber()!=null) {
				Number number = key.GetNumber();
				return number.Equals(Map.One) &&
					Number.Greater(number, SmallInteger.Zero) && Number.LessEqual(number, new SmallInteger(text.Length));
			}
			return false;
		}
		public override int ArrayCount {
			get {
				return text.Length;
			}
		}
		public override Map Copy() {
			return this;
		}
		public override IEnumerable<Map> Array {
			get { 
				foreach(char c in text) {
					yield return c;
				}
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				for (int i = 1; i <= text.Length; i++) {
					yield return i;
				}
			}
		}
	}
	public abstract class Number : Map {
		public virtual Number Multiply(Number b) {
			return new Rational((Numerator.Multiply(b.Numerator)).GetDouble(), (Denominator.Multiply(b.Denominator)).GetDouble());
		}
		public static Number Multiply(Number a, Number b) {
			return a.Multiply(b);
		}
		public int CompareTo(Number number) {
			return this.GetDouble().CompareTo(number.GetDouble());
		}
		public override bool Equals(object obj) {
			Number num = obj as Number;
			if (num != null) {
				return num.Numerator == Numerator && num.Denominator == Denominator;
			}
			else {
				return base.Equals(obj);
			}
		}
		// incorrect implementation, not general enough
		// where to put all those math methods?
		// maybe math.int32.add
		public static Map Raise(Number a, Number b) {
			return new Rational(Math.Pow(a.GetDouble(), b.GetDouble()));
		}
		public static double GreatestCommonDivisor(double a, double b) {
			if (a == b) {
				return a;
			}
			a = Math.Abs(a);
			b = Math.Abs(b);
			while (a != 0 && b != 0) {
				if (a > b) {
					a = a % b;
				}
				else {
					b = b % a;
				}
			}
			if (a == 0) {
				return b;
			}
			else {
				return a;
			}
		}
		public double Expand(Number b) {
			return Numerator.GetDouble() * (LeastCommonMultiple(this, b) / Denominator.GetDouble());
		}
		public static Number Add(Number a, Number b) {
			return a.Add(b);
		}
		public static Number Subtract(Number a, Number b) {
			return a.Subtract(b);
		}
		public virtual Number Divide(Number b) {
			return new Rational((Numerator.Multiply(b.Denominator)).GetDouble(), Denominator.GetDouble() * b.Numerator.GetDouble());
		}
		public static Number Divide(Number a, Number b) {
			return a.Divide(b);
		}
		// implement this as instance method
		public static bool Greater(Number a, Number b) {
			return a.Expand(b) > b.Expand(a);
		}
		public static bool SpecialLess(Map a, Map b) {
			return a.LessThan(b);
		}
		public static bool Less(Map a, Map b) {
			return a.LessThan(b);
		}
		public static bool GreaterEqual(Number a, Number b) {
			return a.Expand(b) >= b.Expand(a);
		}
		public static bool LessEqual(Number a, Number b) {
			return a.LessEqual(b);
		}
		public virtual bool LessEqual(Number number) {
			return Expand(number) <= number.Expand(this);
		}
		public static Number Modulo(Number a, Number b) {
			return new SmallInteger(Convert.ToInt32(a.Numerator) % Convert.ToInt32(b.Numerator));
		}
		public float GetSingle() {
			return (float)GetDouble();
		}
		public abstract Integer Numerator {
			get;
		}
		public abstract Integer Denominator {
			get;
		}
		public abstract long GetInt64();
		public abstract long GetRealInt64();
		public static double LeastCommonMultiple(Number a, Number b) {
			return (a.Denominator.Multiply(b.Denominator)).GetDouble() / GreatestCommonDivisor(a.Denominator.GetDouble(), b.Denominator.GetDouble());
		}
		public virtual Number Subtract(Number b) {
			return new Rational(Expand(b) - b.Expand(this), LeastCommonMultiple(this, b));
		}
		public override bool LessThan(Map b) {
			return Expand(b.GetNumber()) < b.GetNumber().Expand(this);
		}
		public virtual Number Add(Number b) {
			return new Rational(Expand(b) + b.Expand(this), LeastCommonMultiple(this, b));
		}
		public abstract double GetDouble();
		public Number GetNum() {
			return this;
		}
		public override string GetString() {
			return null;
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override int Count {
			get {
				return new List<Map>(Keys).Count;
			}
		}
		public override Map Copy() {
			return this;
		}
		public override int ArrayCount {
			get { return 0; }
		}
		public override string Serialize() {
			return this.ToString();
		}
		public override bool ContainsKey(Map key) {
			return new List<Map>(Keys).Contains(key);
		}
		public override Map this[Map key] {
			get {
				if (ContainsKey(key)) {
					if (key.Count == 0) {
						return Number.Subtract(this.GetNum(), Number.One);
					}
					else if (key.Equals(NumberKeys.Negative)) {
						return Map.Empty;
					}
					else if (key.Equals(NumberKeys.Denominator)) {
						return new Rational(GetNum().Denominator);
					}
					else {
						throw new ApplicationException("Error.");
					}
				}
				else {
					return null;
				}
			}
			set {
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				if (!this.Equals(Number.Zero)) {
					yield return Map.Empty;
				}
				if (Number.Less(this.GetNum(), Number.Zero)) {
					yield return NumberKeys.Negative;
				}
				if (!Denominator.Equals(Number.One)) {
					yield return NumberKeys.Denominator;
				}
			}
		}
		public override Number GetNumber() {
			return this;
		}
		public override string ToString() {
			if (Denominator.Equals(Number.One)) {
				return Numerator.ToString();
			}
			else {
				return Numerator.ToString() + Syntax.fraction + Denominator.ToString();
			}
		}
	}
	public abstract class Integer : Number {
		// override more methods???
		public override Number Multiply(Number b) {
			Integer integer = b as Integer;
			if (integer != null) {
				return new LargeInteger(integer.GetBigInteger().multiply(GetBigInteger()));
			}
			return base.Multiply(b);
		}
		public override bool Equals(object obj) {
			Integer integer = obj as Integer;
			if (integer != null) {
				return GetBigInteger().Equals(integer.GetBigInteger());
			}
			return base.Equals(obj);
		}
		public abstract BigInteger GetBigInteger();
	}
	// refactor numbers
	public class LargeInteger:Integer {
		public override string ToString() {
			return integer.ToString();
		}
		public override BigInteger GetBigInteger() {
			return integer;
		}
		public override Integer GetInteger() {
			return this;
		}
		public override Integer Denominator {
			get {
				return One;
			}
		}
		public override Integer Numerator {
			get {
				return this;
			}
		}
		public override double GetDouble() {
			return integer.doubleValue();
		}

		public override long GetRealInt64() {
			return integer.longValue();
		}
		public override long GetInt64() {
			return integer.longValue();
		}
		private BigInteger integer;
		public LargeInteger(int i) {
			this.integer=new BigInteger(i.ToString());
		}
		public LargeInteger(BigInteger integer) {
			this.integer = integer;
		}
		public LargeInteger(string text) {
			this.integer=new BigInteger(text);
		}
		public override int GetInt32() {
			return integer.intValue();
		}
	}
	public class SmallInteger:Integer{
		public override int GetInt32() {
			return integer;
		}
		public override Number Multiply(Number b) {
			SmallInteger i = b as SmallInteger;
			if (i != null) {
				try {
					return new SmallInteger(i.integer * integer);
				}
				catch (Exception e) {
				}
			}
			return base.Multiply(b);
		}
		public override bool LessEqual(Number number) {
			SmallInteger i = number as SmallInteger;
			if (i != null) {
				return integer <= i.integer;
			}
			return base.LessEqual(number);
		}
		public override Number Subtract(Number b) {
			SmallInteger i = b as SmallInteger;
			if (i != null) {
				try {
					return new SmallInteger(integer - i.integer);
				}
				catch (OverflowException) { }
			}
			return base.Subtract(b);
		}
		public override bool LessThan(Map b) {
			SmallInteger i = b as SmallInteger;
			if (i != null) {
				try {
					return integer < i.integer;
				}
				catch (OverflowException) { }
			}
			return base.LessThan(b);
		}
		public override Number Add(Number b) {
			SmallInteger i = b as SmallInteger;
			if (i != null) {
				try {
					return new SmallInteger(integer + i.integer);
				}
				catch (OverflowException) { }
			}
			return base.Add(b);
		}
		public override string ToString() {
			return integer.ToString();
		}
		public override bool Equals(object obj) {
			SmallInteger i = obj as SmallInteger;
			if (i != null) {
				return i.integer==this.integer;
			}
			return base.Equals(obj);
		}
		public override BigInteger GetBigInteger() {
			return new BigInteger(integer.ToString());
		}
		public override Integer GetInteger() {
			return this;
		}
	    private int integer;
	    public SmallInteger(int integer) {
	        this.integer=integer;
	    }
	    public override double GetDouble() {
	        return integer;
	    }
		public override Integer Denominator {
	        get {
	            return LargeInteger.One;
	        }
	    }
	    public override long GetInt64() {
	        return integer;
	    }
	    public override long GetRealInt64() {
	        return integer;
	    }
		public override Integer Numerator {
	        get {
				return this;
			}
	    }
	}
	public class Rational: Number {
		public override Integer GetInteger() {
			if (Denominator.Equals(One)) {
				return Numerator;
			}
			return null;
		}
		//public override BigInteger GetInteger() {
		//    if (Denominator.GetDouble() == 1.0d && Numerator.GetDouble() < int.MaxValue && Numerator.GetDouble() > int.MinValue) {
		//        return new BigInteger(Denominator.GetDouble().ToString());
		//    }
		//    return null;
		//}
		private readonly LargeInteger numerator;
		private readonly LargeInteger denominator;
		public static Number Parse(string text) {
			try {
				string[] parts = text.Split('/');
				int numerator = Convert.ToInt32(parts[0]);
				int denominator;
				if (parts.Length > 2) {
					denominator = Convert.ToInt32(parts[2]);
				}
				else {
					denominator = 1;
				}
				return new Rational(numerator, denominator);
			}
			catch (Exception e) {
				return null;
			}
		}
		public Rational(double integer): this(integer, 1) {}
		public Rational(Number i) : this(i.Numerator.GetDouble(), i.Denominator.GetDouble()) { }
		public Rational(double numerator, double denominator) {
			double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
			if (denominator < 0) {
				numerator = -numerator;
				denominator = -denominator;
			}
			this.numerator = new LargeInteger(Convert.ToInt64((numerator / greatestCommonDivisor)).ToString());
			this.denominator = new LargeInteger(Convert.ToInt64(denominator / greatestCommonDivisor).ToString());
		}
		public override Integer Numerator {
			get {
				return numerator;
			}
		}
		public override Integer Denominator {
			get {
				return denominator;
			}
		}
		public Number Clone() {
			return new Rational(this);
		}
		public override double GetDouble() {
			return numerator.GetDouble() / denominator.GetDouble();
		}
		public override int GetInt32() {
			return Convert.ToInt32(numerator.GetDouble() / denominator.GetDouble());
		}
		public override long GetRealInt64() {
			return Convert.ToInt64(numerator.GetDouble() / denominator.GetDouble());
		}
		public override long GetInt64() {
			return Convert.ToInt64(numerator.GetDouble());
		}
	}
	public class Error {
		public Parser.State State;
		public string Text;
		public Source Source;
		public Error(string text,Source source,Parser.State state) {
			this.State = state;
			this.Text = text;
			this.Source = source;
		}
	}
	public class Syntax {
		public const char functionSeparator=',';
		public const char decimalSeparator = '.';
		public const char searchStatement=':';
		public const char search='!';
		public const char comment='/';
		public const char statement='=';
		public const char programStart = '{';
		public const char programEnd = '}';
		public const char functionStart = '(';
		public const char functionEnd = ')';
		public const char arrayStart = '[';
		public const char arrayEnd = ']';
		public const char arraySeparator = ',';
		public const char programSeparator = ';';
		public const char callSeparator = ',';
		public const char callStart = '(';
		public const char callEnd = ')';
		public const char root = '/';
		public const char negative = '-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char lineFeed = '\n';
		public const char carriageReturn = '\r';
		public const char @string = '\"';
		public const char emptyMap = '0';
		public const char select = '.';
		public const char character = '\'';
		public const char space = ' ';
		public const char tab = '\t';
		public const char current = '&';
		public const char lookupAnythingStart='<';
		public const char lookupAnythingEnd='>';
		public static readonly string integer = "0123456789-";
		public static readonly string lookupStringForbidden =
			"" + current + '\r' + '\n' + functionStart+
			functionEnd+ @string+emptyMap+ search + root+ callStart+ callEnd+ 
			character+ programStart+ '*'+ '$'+ '\\'+ lookupAnythingStart+ statement+ arrayStart+
			'-'+ searchStatement+ select+ ' '+ '-'+ arrayStart+ arrayEnd+ '*'+ lookupAnythingEnd+ 
			programStart+ programSeparator +callSeparator+programEnd+arrayEnd;
		public static readonly string lookupStringForbiddenFirst = lookupStringForbidden+integer;
	}
	public class Serialization {
		public static string Serialize(Map map) {
			return Serialize(map, -1).Trim();
		}
		private static string Number(Map map) {
			return map.GetNumber().ToString();
		}
		private static string Serialize(Map map, int indentation) {
			if (map.Count == 0) {
				if (indentation < 0) {
					return "";
				}
				else {
					return "0";
				}
			}
			else if(map.IsString) {
				return String(map,indentation);
			}
			if (map is DotNetMap) {
				return map.ToString();
			}
			else if (map.GetNumber()!=null) {
				return Number(map);
			}
			else if (map.IsString) {
				return String(map, indentation);
			}
			else {
				return Map(map, indentation);
		}}
		private static string Map(Map map, int indentation) {
			string text;
			if (indentation < 0) {
				text = "";
			}
			else {
				text = "," + Environment.NewLine;
			}
			foreach (KeyValuePair<Map, Map> entry in map) {
				text += Entry(indentation, entry);
			}
			return text;
		}
		private static string Entry(int indentation, KeyValuePair<Map, Map> entry) {
			//if (entry.Key.Equals(CodeKeys.Function)) {
			//    return Function(entry.Value, indentation + 1);
			//}
			//else {
				return (Indentation(indentation + 1)
					+ Key(indentation, entry) +
					"=" +
					Serialize(entry.Value, indentation + 1)
					+ Environment.NewLine).TrimEnd('\r', '\n')
					+ Environment.NewLine;
			//}
		}
		private static string Literal(Map value, int indentation) {
			if (value.GetNumber()!=null) {
				return Number(value);
			}
			else if (value.IsString) {
				return String(value, indentation);
			}
			//else if(value.ContainsKey(CodeKeys.Function)) {
			//    return ","+NewLine()+ Indentation(indentation+1)+ Function(value[CodeKeys.Function], indentation+1);
			//}
			else {
				return "," + NewLine() + Indentation(indentation + 1) + Expression(value, indentation + 1);
				//return "error";
		}}
		//private static string Function(Map value, int indentation) {
		//    return value[CodeKeys.Parameter].GetString() + "|" + Expression(value[CodeKeys.Expression], indentation);
		//}
		private static string Root() {
			return "/";
		}
		private static string Expression(Map map, int indentation) {
			if (map.ContainsKey(CodeKeys.Literal)) {
				return Literal(map[CodeKeys.Literal], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Root)) {
				return Root();
			}
			if (map.ContainsKey(CodeKeys.Call)) {
				return Call(map[CodeKeys.Call], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Program)) {
				return Program(map[CodeKeys.Program], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Select)) {
				return Select(map[CodeKeys.Select], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Search)) {
				return Search(map[CodeKeys.Search], indentation);
			}
			return Serialize(map, indentation);
		}
		private static string KeyStatement(Map map, int indentation) {
			Map key = map[CodeKeys.Key];
			//if (key.Equals(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))) {
			//    return FunctionStatement(map[CodeKeys.Value][CodeKeys.Literal], indentation);
			//}
			//else {
				return Expression(map[CodeKeys.Key], indentation) + "="
					+ Expression(map[CodeKeys.Value], indentation);
			//}
		}
		private static string CurrentStatement(Map map, int indentation) {
			return "&=" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string SearchStatement(Map map, int indentation) {
			return Expression(map[CodeKeys.Keys], indentation) + ":" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string DiscardStatement(Map map, int indentation) {
			return Expression(map[CodeKeys.Value], indentation);
		}
		private static string Statement(Map map, int indentation) {
			if (map.ContainsKey(CodeKeys.Key)) {
				return KeyStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Current)) {
				return CurrentStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Keys)) {
				return SearchStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Value)) {
				return DiscardStatement(map, indentation);
			}
			throw new Exception("not implemented");
		}
		private static string Program(Map map, int indentation) {
			string text = "," + NewLine();
			indentation++;
			foreach (Map m in map.Array) {
				text += Indentation(indentation) + Trim(Statement(m, indentation)) + NewLine();
			}
			return text;
		}
		private static string Trim(string text) {
			return text.TrimEnd('\n', '\r');
		}
		private static string NewLine() {
			return Environment.NewLine;
		}
		private static string Call(Map map, int indentation) {
			string text = "-" + NewLine();
			indentation++;
			foreach (Map m in map.Array) {
				text += Indentation(indentation) +Trim(Expression(m, indentation)) + NewLine();
			}
			return text;
		}
		private static string Select(Map map, int indentation) {
			string text = "." + NewLine();
			indentation++;
			foreach (Map sub in map.Array) {
				text += Indentation(indentation) +
					Trim(Expression(sub, indentation)) + NewLine();}
			return text;
		}
		private static string Search(Map map, int indentation) {
			return "!" + Expression(map, indentation);
		}
		private static string Key(int indentation, KeyValuePair<Map, Map> entry) {
			if (entry.Key.Count != 0 && entry.Key.IsString) {
				string key = entry.Key.GetString();
				if (key.IndexOfAny(Syntax.lookupStringForbidden.ToCharArray()) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst.ToCharArray()) != 0) {
					return entry.Key.GetString();
			}}
			return Serialize(entry.Key, indentation + 1);
		}
		private static string String(Map map, int indentation) {
			string text = map.GetString();
			if (text.Contains("\"") || text.Contains("\n")) {
				string result = "\"" + Environment.NewLine;
				foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
					result += Indentation(indentation) + "\t" + line + Environment.NewLine;}
				return result.Trim('\n', '\r') + Environment.NewLine + Indentation(indentation) + "\"";}
			else {
				return "\"" + text + "\"";
			}
		}
		private static string Indentation(int indentation) {
			return "".PadLeft(Math.Max(0,indentation), '\t');
		}
	}
	public class Parser {
		public static Rule Condition(Precondition precondition, Rule rule) {
			rule.precondition = precondition;
			return rule;
		}
		public static Rule Condition(char c, Rule rule) {
			return Condition(
				delegate(Parser p) {
					return p.Look() == c;
				},
				rule
			);
		}
		public static List<Dictionary<State, CachedResult>> allCached = new List<Dictionary<State, CachedResult>>();
		public struct State {
			public override bool Equals(object obj) {
				return ((State)obj).index == index && ((State)obj).FileName == FileName;
			}
			public override int GetHashCode() {
				unchecked {
					return index.GetHashCode() * FileName.GetHashCode();
				}
			}
			public State(string fileName, string Text) {
				this.index = 0;
				this.FileName = fileName;
				this.Line = 1;
				this.Column = 0;
				this.Text = Text;
				this.Errors = new Error[] { };
			}
			public string Text;
			public string FileName;
			public int index;
			public int Line;
			public int Column;
			public Error[] Errors;
		}
		public Stack<int> defaultKeys = new Stack<int>();
		public State state;
		public Parser(string text, string filePath) {
			state = new State(filePath, text + Syntax.endOfFile);
		}
		public static Rule NewLine = Alternatives(
			Syntax.lineFeed,
			Syntax.carriageReturn
		);
		public static Rule EndOfLine = FastSequence(
			StringRule(ZeroOrMoreChars(Chars("" + Syntax.space + Syntax.tab))),
			Alternatives(Syntax.lineFeed, Syntax.carriageReturn)
		);
		public static Rule Program = Condition(
			Syntax.programStart,
			DelayedRule(
				delegate {
					return Cache(
						Sequence(
							Assign(
								CodeKeys.Program,
								SequenceList(
									Syntax.programStart,
									Whitespace,
									new FastAction(
										ZeroOrMore(
											Autokey(
												FastSequence(
													Whitespace,
													new FastAction(AllStatements)
							)))))),
							Whitespace,
							OptionalError(Syntax.programEnd)
		));}));
		public static Rule Comment = FastSequence(
			Syntax.comment,
			Syntax.comment,
			StringRule(ZeroOrMoreChars(CharsExcept(Syntax.carriageReturn.ToString())))
		);
		public static Rule Whitespace = ZeroOrMore(
			Alternatives(
				Comment,
				Syntax.lineFeed,
				Syntax.carriageReturn,
				Syntax.tab,
				Syntax.space
		));
		public static Rule Expression = DelayedRule(delegate() {
			return Cache(
				Alternatives(
					List,
					LiteralExpression,
					Call,
					CallSelect,
					Select,
					Program,
					FunctionProgram,
					Search
		));});
		public static Rule Integer = FastSequence(
			new Action(
				StringRule(OneOrMoreChars(Chars(Syntax.integer))),
				delegate(Parser p, Map map, ref Map result) {
					string text = map.GetString();
					int smallInteger;
					if (int.TryParse(text, out smallInteger)) {
						result = new SmallInteger(smallInteger);
					}
					else {
						result = new LargeInteger(text);
					}
					result.Source = map.Source;
				}
		));
		public static Rule StringLine = StringRule(ZeroOrMoreChars(CharsExcept("\n\r")));
		public static Rule CharacterDataExpression = FastSequence(
			Syntax.character,
			new FastAction(ReallyOneChar(CharsExcept(Syntax.character.ToString()))),
			Syntax.character
		);
		public static Rule String = FastSequence(
			Syntax.@string,
			new FastAction(StringRule(OneOrMoreChars(CharsExcept("" + Syntax.@string)))),
			OptionalError(Syntax.@string)
		);
		public static Rule Decimal = FastSequence(
			new Action(
				StringRule(
					StringSequence(
						OneOrMoreChars(Chars(Syntax.integer)),
			// ugly hack
						OneOrMoreChars(Chars(Syntax.decimalSeparator.ToString())),
						OneOrMoreChars(Chars(Syntax.integer))
				)),
				delegate(Parser p, Map map, ref Map result) {
					Rational rational = new Rational(double.Parse(map.GetString(), CultureInfo.InvariantCulture));
					if (rational.GetInteger() != null) {
						result = new SmallInteger(rational.GetInt32());
						result.Source = map.Source;
					}
					else {
						result = rational;
						result.Source = map.Source;
					}
				}
		));
		public static Rule Number = FastSequence(
			new FastAction(Integer),
			new Action(
				Optional(
					FastSequence(
						Syntax.fraction,
						new FastAction(Integer)
				)),
				delegate(Parser p, Map map, ref Map result) {
					if (map != null) {
						result = new Rational(result.GetNumber().GetDouble(), map.GetNumber().GetDouble());
						result.Source = map.Source;
					}
				}
		));
		public static StringDelegate StringSequence(params StringDelegate[] rules) {
			return delegate(Parser parser, ref string s) {
				s = "";
				State oldState = parser.state;
				foreach (StringDelegate rule in rules) {
					string result = null;
					if (rule(parser, ref result)) {
						s += result;
					}
					else {
						parser.state = oldState;
						return false;
					}
				}
				return true;
			};
		}
		public static Rule LookupString = Condition(
			delegate(Parser p) {
				return Syntax.lookupStringForbiddenFirst.IndexOf(p.Look()) == -1;
			},
			Cache(
				StringRule(
					StringSequence(
						OneChar(CharsExcept(Syntax.lookupStringForbiddenFirst)),
						ZeroOrMoreChars(CharsExcept(Syntax.lookupStringForbidden))
		))));
		public static Rule Value = DelayedRule(delegate {
			return FastSequence(
				Whitespace,
				new FastAction(
					Alternatives(
						ListMap,
						FunctionMap,
						MapRule,
						String,
						Number,
						CharacterDataExpression
				)),
				Whitespace
		);
		});
		private static Rule LookupAnything = FastSequence(
			Syntax.lookupAnythingStart,
			Whitespace,
			new FastAction(Value)
		);
		public static Rule Entry = FastSequence(
			new FastAction(
				Alternatives(
					Number,
					LookupString,
					LookupAnything
			)),
			Syntax.statement,
			new Action(
				Value,
				delegate(Parser parser, Map map, ref Map result) {
					result = new DictionaryMap(result, map);
				}),
			Whitespace
		);
		public static Rule MapRule = FastSequence(
			Syntax.programStart,
			Whitespace,
			new FastAction(
				OneOrMore(
					new Action(
						FastSequence(
							new FastAction(Entry),
							Whitespace,
							OptionalError(Syntax.programSeparator),
							Whitespace
						),
						delegate(Parser parser, Map map, ref Map result) {
							result = Library.Merge(result, map);
						}))),
			OptionalError(Syntax.programEnd),
			Whitespace
		);
		Dictionary<State, string> errors = new Dictionary<State, string>();
		private static Rule Arg = DelayedRule(
			delegate {
				return Alternatives(
					List,
					FunctionProgram,
					LiteralExpression,
					Call,
					Select,
					Search,
					Program
			);
			}
		);
		public static Rule MakeCall(Rule m) {
			return DelayedRule(
				delegate() {
					return Sequence(
						Assign(
							CodeKeys.Call,
							SequenceList(
								Autokey(m),
								Whitespace,
								Syntax.callStart,
								Whitespace,
								Autokey(
									Alternatives(
										Arg,
										LiteralRule(new DictionaryMap(CodeKeys.Literal, Map.Empty))
								)),
								new FastAction(
									ZeroOrMore(
										Autokey(
											FastSequence(
												Whitespace,
												Syntax.callSeparator,
												Whitespace,
												new FastAction(Arg)
								)))),
								Whitespace,
								OptionalError(Syntax.callEnd)
		)));});}
		public static Rule Call = MakeCall(
			DelayedRule(
				delegate {
					return Alternatives(
						List,
						FunctionProgram,
						LiteralExpression,
						CallSelect,
						Select,
						Search,
						Program
		);
				}));
		public static Rule SimpleCall = MakeCall(
			DelayedRule(
				delegate() {
					return Alternatives(
						List,
						FunctionProgram,
						LiteralExpression,
						Program,
						SmallSelect,
						Search
		);
				}));
		private static Rule Simple(char c, Map literal) {
			return FastSequence(c, new FastAction(LiteralRule(literal)));
		}
		private static Rule EmptyMap = Simple(Syntax.emptyMap, Map.Empty);
		private static Rule Current = Simple(Syntax.current, new DictionaryMap(CodeKeys.Current, Map.Empty));
		private static Rule Root = Simple(Syntax.root, new DictionaryMap(CodeKeys.Root, Map.Empty));
		private static Rule LiteralExpression = Condition(
			delegate(Parser p) {
				switch (p.Look()) {
					case Syntax.@string:
					case Syntax.emptyMap:
					case Syntax.negative:
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '\'':
						return true;
					default:
						return false;
				}
			},
			Sequence(
				Assign(
					CodeKeys.Literal,
					Alternatives(
						EmptyMap,
						Decimal,
						Number,
						String,
						CharacterDataExpression
		))));
		private static Rule LookupAnythingExpression = FastSequence(
			Syntax.lookupAnythingStart,
			Whitespace,
			new FastAction(Expression),
			OptionalError(Syntax.lookupAnythingEnd)
		);
		private static Rule LookupStringExpression = Condition(
			delegate(Parser p) {
				return Syntax.lookupStringForbiddenFirst.IndexOf(p.Look()) == -1;
			},
			Sequence(Assign(CodeKeys.Literal, LookupString))
		);
		private static Rule Search = Condition(
			delegate(Parser p) {
				char c = p.Look();
				return c == Syntax.search || Syntax.lookupStringForbiddenFirst.IndexOf(c) == -1 || c == Syntax.lookupAnythingStart;
			},
			Cache(Sequence(
				Assign(
					CodeKeys.Search,
					Alternatives(
						Prefix(Syntax.search, Expression),
						Alternatives(LookupStringExpression, LookupAnythingExpression)
		)))));
		private static Rule MakeSelect(Rule m) {
			return DelayedRule(
				delegate {
					return Cache(Sequence(
						Assign(
							CodeKeys.Select,
							SequenceList(
								Autokey(m),
								new FastAction(
									OneOrMore(
										Autokey(
											FastSequence(
												Syntax.select,
												new FastAction(
													Alternatives(
														LookupStringExpression,
														LookupAnythingExpression,
														LiteralExpression
		))))))))));
				});
		}
		private static Rule SmallSelect = MakeSelect(
			DelayedRule(
				delegate {
					return Alternatives(Root, Search, Program, LiteralExpression);
				}));
		private static Rule CallSelect = MakeSelect(SimpleCall);
		private static Rule Select = MakeSelect(Alternatives(Root, Search, Program, LiteralExpression));
		public static Rule ListMap = FastSequence(
			Syntax.arrayStart,
			new FastAction(
				PrePost(
					delegate(Parser p) { p.defaultKeys.Push(1); },
					SequenceList(
						Whitespace,
						Autokey(Value),
						Whitespace,
						new FastAction(
							ZeroOrMore(
								Autokey(
									FastSequence(
										OptionalError(Syntax.arraySeparator),
										new FastAction(FastSequence(Whitespace, new FastAction(Value)))
						)))),
						Whitespace,
						OptionalError(Syntax.arrayEnd)
					),
					delegate(Parser p) {
						p.defaultKeys.Pop();
					})));
		public static Rule ListEntry = new Rule(
			delegate(Parser p, ref Map map) {
				if (Parser.Expression.Match(p, ref map)) {
					Map key = new DictionaryMap(CodeKeys.Literal, new SmallInteger(p.defaultKeys.Peek()));
					key.Source = map.Source;
					map = new DictionaryMap(
						CodeKeys.Key,
						key,
						CodeKeys.Value,
						map
					);
					p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
					return true;
				}
				else {
					return false;
				}
			});
		public static Rule List = Condition(
			Syntax.arrayStart,
			PrePost(
				delegate(Parser p) { p.defaultKeys.Push(1); },
				Sequence(
					Assign(
						CodeKeys.Program,
						Optional(
							SequenceList(
								Whitespace,
								Syntax.arrayStart,
								Whitespace,
								Autokey(ListEntry),
								Whitespace,
								new FastAction(
									ZeroOrMore(
										Autokey(
											FastSequence(
												OptionalError(Syntax.arraySeparator),
												Whitespace,
												new FastAction(ListEntry)
								)))),
								Whitespace,
								Syntax.arrayEnd
				)))),
				delegate(Parser p) { p.defaultKeys.Pop(); }
		));
		// refactor
		public static Rule ComplexStatement(Rule rule, Action action) {
			if (rule == null) {
				return Sequence(
					action,
					Assign(CodeKeys.Value, Expression),
					Whitespace
				);
			}
			else {
				return Sequence(
					action,
					rule,
					Assign(CodeKeys.Value, Expression),
					Whitespace
				);
			}
		}
		public static Rule DiscardStatement = ComplexStatement(
			null,
			Assign(
				CodeKeys.Discard,
				LiteralRule(Map.Empty)
		));
		public static Rule CurrentStatement = ComplexStatement(
			Syntax.current, Assign(CodeKeys.Current, LiteralRule(Map.Empty))
		);
		public static Rule Prefix(Rule pre, Rule rule) {
			return FastSequence(pre, new FastAction(rule));
		}
		public static Rule NormalStatement = ComplexStatement(
			Syntax.statement,
			Assign(
				CodeKeys.Key,
				Alternatives(
					FastSequence(
						Syntax.lookupAnythingStart,
						Whitespace,
						new FastAction(Expression),
						OptionalError(Syntax.lookupAnythingEnd)
					),
					Select,
					Sequence(Assign(CodeKeys.Literal, LookupString)),
					Expression
		)));
		public static Rule Statement = ComplexStatement(
			Syntax.searchStatement,
			Assign(
				CodeKeys.Keys,
				Alternatives(
					Sequence(Assign(CodeKeys.Literal, LookupString)), Expression
		)));
		public static Rule AllStatements = FastSequence(
			new FastAction(
				Alternatives(
					CurrentStatement,
					NormalStatement,
					Statement,
					DiscardStatement
			)),
			Whitespace,
			OptionalError(Syntax.programSeparator),
			Whitespace
		);
		public static Rule FunctionMap = Sequence(
			Syntax.functionStart,
			Assign(
				CodeKeys.Program,
				SequenceList(
					Autokey(
						Sequence(
							Assign(
								CodeKeys.Key,
								Sequence(
									Assign(
										CodeKeys.Literal,
										StringRule(ZeroOrMoreChars(CharsExcept(Syntax.lookupStringForbiddenFirst)))
							))),
							Assign(
								CodeKeys.Value,
								Sequence(Assign(CodeKeys.LastArgument, LiteralRule(0))
					)))),
					Autokey(
						Sequence(
							Assign(CodeKeys.Current, LiteralRule(0)),
							Whitespace,
							OptionalError(Syntax.functionEnd),
							Assign(CodeKeys.Value, Expression)
			)))),
			Whitespace
		);

		// somehow buggy
		public static Rule FunctionPart = DelayedRule(delegate {
			return Alternatives(
				Sequence(
					Assign(
						CodeKeys.Literal,
						Alternatives(
							Sequence(
								Assign(
									CodeKeys.Program,
									Sequence(
										Autokey(
											Sequence(
												Assign(
													CodeKeys.Key,
													Sequence(
														Assign(
															CodeKeys.Literal,
															StringRule(OneOrMoreChars(CharsExcept(Syntax.lookupStringForbiddenFirst)))
												))),
												Assign(
													CodeKeys.Value,
													Sequence(
														Assign(
															CodeKeys.LastArgument,
															LiteralRule(0)
										))))),
										Autokey(
											Sequence(
												Assign(
													CodeKeys.Current,
													LiteralRule(0)
												),
												Assign(
													CodeKeys.Value,
													Alternatives(
														Sequence(
															Syntax.functionSeparator,
															Assign(
																CodeKeys.Literal,
																FunctionPart)
														),
														FastSequence(
															Syntax.functionEnd,
															Whitespace,
															new FastAction(Expression)
										))))),
										Whitespace
				)))))),
				Sequence(
					Assign(
						CodeKeys.Literal,
						FastSequence(
							Syntax.functionEnd,
							Whitespace,
							new FastAction(Expression)
		))));
		});

		public static Rule FunctionProgram = FastSequence(
			Syntax.functionStart,
			new FastAction(FunctionPart)
		);


		public static Rule OptionalError(char c) {
			return Alternatives(c, Error("Missing '" + c + "'"));
		}
		public static Rule Error(string text) {
			return new Rule(delegate(Parser parser, ref Map map) {
				Error[] errors = new Error[parser.state.Errors.Length + 1];
				parser.state.Errors.CopyTo(errors, 0);
				errors[errors.Length - 1] = new Error(text, new Source(parser.state.Line, parser.state.Column, parser.state.FileName), parser.state);
				parser.state.Errors = errors;
				return true;
			});
		}
		public class FastAction : Action {
			public FastAction(Rule rule)
				: base(rule, delegate { }) {
			}
			public override bool Execute(Parser parser, ref Map result) {
				if (rule.MatchFast(parser, ref result)) {
					return true;
				}
				return false;
			}
		}
		public class Action {
			public static implicit operator Action(char c) {
				return StringRule(OneChar(SingleChar(c)));
			}
			public static implicit operator Action(Rule rule) {
				return new Action(rule, delegate { });
			}
			protected Rule rule;
			public Action(Rule rule, ActionDelegate action) {
				this.rule = rule;
				this.action = action;
			}
			public virtual bool Execute(Parser parser, ref Map result) {
				Map map = null;
				if (rule.Match(parser, ref map)) {
					action(parser, map, ref result);
					return true;
				}
				return false;
			}
			protected ActionDelegate action;
		}
		public static Action Autokey(Rule rule) {
			return new Action(rule, delegate(Parser parser, Map map, ref Map result) {
				result.Append(map);
			});
		}
		public static Action Assign(Map key, Rule rule) {
			return new Action(rule, delegate(Parser parser, Map map, ref Map result) {
				result[key] = map;
			});
		}
		public static Rule OneOrMore(Action action) {
			return new Rule(delegate(Parser parser, ref Map map) {
				bool matched = false;
				while (action.Execute(parser, ref map)) {
					matched = true;
				}
				return matched;
			});
		}
		public delegate void ActionDelegate(Parser p, Map map, ref Map result);
		public delegate bool Precondition(Parser p);
		public class CachedResult {
			public CachedResult(Map map, State state) {
				this.map = map;
				this.state = state;
			}
			public Map map;
			public State state;
		}
		public static Rule Cache(Rule rule) {
			Dictionary<State, CachedResult> cached = new Dictionary<State, CachedResult>();
			allCached.Add(cached);
			return new Rule(delegate(Parser parser, ref Map map) {
				if (parser.state.Text.Length == parser.state.index + 1) {
					return false;
				}
				CachedResult result;
				if (cached.TryGetValue(parser.state, out result)) {
					map = result.map;
					parser.state = result.state;
					return true;
				}
				State startState = parser.state;
				if (rule.Match(parser, ref map)) {
					cached[startState] = new CachedResult(map, parser.state);
					return true;
				}
				return false;
			});
		}
		public class Rule {
			public Rule(ParseFunction parseFunction) {
				this.parseFunction = parseFunction;
			}
			public Precondition precondition;
			public static implicit operator Rule(char c) {
				return StringRule(OneChar(SingleChar(c)));
			}
			public bool MatchFast(Parser parser, ref Map map) {
				if (precondition != null) {
					if (!precondition(parser)) {
						return false;
					}
				}
				State oldState = parser.state;
				bool matched;
				matched = parseFunction(parser, ref map);
				if (!matched) {
					parser.state = oldState;
				}
				else {
					if (map != null) {
						map.Source = new Extent(
							new Source(oldState.Line, oldState.Column, parser.state.FileName),
							new Source(parser.state.Line, parser.state.Column, parser.state.FileName));
					}
				}
				return matched;
			}
			public bool Match(Parser parser, ref Map map) {
				if (precondition != null) {
					if (!precondition(parser)) {
						return false;
					}
				}
				State oldState = parser.state;
				bool matched;
				Map result = null;
				matched = parseFunction(parser, ref result);
				if (!matched) {
					parser.state = oldState;
				}
				else {
					if (result != null) {
						result.Source = new Extent(
							new Source(oldState.Line, oldState.Column, parser.state.FileName),
							new Source(parser.state.Line, parser.state.Column, parser.state.FileName));
					}
				}
				map = result;
				return matched;
			}
			private ParseFunction parseFunction;
		}
		public delegate bool CharRule(char next);
		public static CharRule Chars(string chars) {
			return new CharRule(delegate(char next) {
				return chars.IndexOf(next) != -1;
			});
		}
		public static CharRule CharsExcept(string characters) {
			string chars = characters + Syntax.endOfFile;
			return new CharRule(delegate(char c) {
				return chars.IndexOf(c) == -1;
			});
		}
		public delegate bool StringDelegate(Parser parser, ref string s);
		public static StringDelegate CharLoop(CharRule rule, int min, int max) {
			return delegate(Parser parser, ref string s) {
				int offset = 0;
				int column = parser.state.Column;
				int line = 0;
				while ((max == -1 || offset < max) && rule(parser.Look(offset))) {
					offset++;
					column++;
					if (parser.Look(offset).Equals(Syntax.lineFeed)) {
						line++;
						column = 1;
					}
				}
				s = parser.state.Text.Substring(parser.state.index, offset);
				if (offset >= min && (max == -1 || offset <= max)) {
					parser.state.index += offset;
					parser.state.Column = column;
					parser.state.Line += line;
					return true;
				}
				return false;
			};
		}
		public static Rule ReallyOneChar(CharRule rule) {
			return new Rule(delegate(Parser parser, ref Map map) {
				char next = parser.Look();
				bool matched = rule(next);
				if (matched) {
					map = next;
					parser.state.index++;
					parser.state.Column++;
					if (next.Equals(Syntax.lineFeed)) {
						parser.state.Line++;
						parser.state.Column = 1;
					}
				}
				return matched;
			});
		}
		public static StringDelegate OneChar(CharRule rule) {
			return CharLoop(rule, 1, 1);
		}
		public static CharRule SingleChar(char c) {
			return new CharRule(delegate(char next) {
				return next.Equals(c);
			});
		}
		public static StringDelegate OneOrMoreChars(CharRule rule) {
			return CharLoop(rule, 1, -1);
		}
		public static StringDelegate ZeroOrMoreChars(CharRule rule) {
			return CharLoop(rule, 0, -1);
		}
		public static Rule StringRule(StringDelegate del) {
			return new Rule(delegate(Parser parser, ref Map map) {
				string s = null;
				bool success = del(parser, ref s);
				map = s;
				return success;
			});
		}
		public delegate void PrePostDelegate(Parser parser);
		public static Rule PrePost(PrePostDelegate pre, Rule rule, PrePostDelegate post) {
			return new Rule(delegate(Parser parser, ref Map map) {
				pre(parser);
				bool matched = rule.Match(parser, ref map);
				post(parser);
				return matched;
			});
		}
		public delegate bool ParseFunction(Parser parser, ref Map map);
		public delegate Rule RuleFunction();
		public static Rule DelayedRule(RuleFunction ruleFunction) {
			Rule rule = null;
			return new Rule(delegate(Parser parser, ref Map map) {
				if (rule == null) {
					rule = ruleFunction();
				}
				return rule.Match(parser, ref map);
			});
		}
		public static Rule Alternatives(params Rule[] cases) {
			return new Rule(delegate(Parser parser, ref Map map) {
				foreach (Rule expression in cases) {
					if (expression.Match(parser, ref map)) {
						return true;
					}
				}
				return false;
			});
		}
		public static Rule Sequence(char c, params Action[] actions) {
			List<Action> a = new List<Action>(actions);
			a.Insert(0, c);
			return Condition(c, Sequence(false, a.ToArray()));
		}
		public static Rule Sequence(params Action[] actions) {
			return Sequence(false, actions);
		}
		public static Rule SequenceList(params Action[] actions) {
			return Sequence(true, actions);
		}
		public static Rule FastSequence(char c, params Action[] actions) {
			List<Action> a = new List<Action>(actions);
			a.Insert(0, c);
			return Condition(c, FastSequence(a.ToArray()));
		}
		// rename?, make standard?
		public static Rule FastSequence(params Action[] actions) {
			return new Rule(delegate(Parser parser, ref Map match) {
				foreach (Action action in actions) {
					if (!action.Execute(parser, ref match)) {
						match = null;
						return false;
					}
				}
				return true;
			});
		}
		public static Rule Sequence(bool list, params Action[] actions) {
			return new Rule(delegate(Parser parser, ref Map match) {
				if (list) {
					match = new ListMap();
				}
				else {
					match = new DictionaryMap();
				}
				foreach (Action action in actions) {
					if (!action.Execute(parser, ref match)) {
						match = null;
						return false;
					}
				}
				return true;
			});
		}
		public static Rule LiteralRule(Map literal) {
			return new Rule(delegate(Parser parser, ref Map map) {
				map = literal;
				return true;
			});
		}
		public static Rule ZeroOrMore(Action action) {
			return new Rule(delegate(Parser parser, ref Map map) {
				while (action.Execute(parser, ref map))
					;
				return true;
			});
		}
		public static Rule Optional(Rule rule) {
			return new Rule(delegate(Parser parser, ref Map match) {
				rule.Match(parser, ref match);
				return true;
			});
		}
		private char Look(int offset) {
			return state.Text[state.index + offset];
		}
		private char Look() {
			return Look(0);
		}
		public static Map Parse(string file) {
			return ParseString(System.IO.File.ReadAllText(file), file);
		}
		public static Map ParseString(string text, string fileName) {
			Parser parser = new Parser(text, fileName);
			Map result = null;
			Parser.Value.Match(parser, ref result);
			if (parser.state.index != parser.state.Text.Length - 1 || parser.state.Errors.Length != 0) {
				string t = "";
				if (parser.state.index != parser.state.Text.Length - 1) {
					t += "Expected end of file. " + new Source(parser.state.Line, parser.state.Column, parser.state.FileName).ToString() + "\n";
				}
				foreach (Error error in parser.state.Errors) {
					t += error.Text + error.Source.ToString() + "\n";
				}
				throw new SyntaxException(t, parser);
			}
			foreach (Dictionary<State, CachedResult> cached in Parser.allCached) {
				cached.Clear();
			}
			return result;
		}
	}
	namespace Test {
		public abstract class TestRunner {
			public static string TestDirectory {
				get {
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public abstract class Test {
				public bool RunTest() {
					int level;
					Console.Write(this.GetType().Name + "...");
					DateTime startTime = DateTime.Now;
					object result = GetResult(out level);
					TimeSpan duration = DateTime.Now - startTime;
					string testDirectory = Path.Combine(TestDirectory, this.GetType().Name);
					string resultPath = Path.Combine(testDirectory, "result.txt");
					string checkPath = Path.Combine(testDirectory, "check.txt");
					Directory.CreateDirectory(testDirectory);
					if (!File.Exists(checkPath)) {
						File.Create(checkPath).Close();
					}
					StringBuilder stringBuilder = new StringBuilder();
					Serialize(result, "", stringBuilder, level);
					File.WriteAllText(resultPath, stringBuilder.ToString(), Encoding.UTF8);
					string successText;
					bool success = File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath));
					if (success) {
						successText = "succeeded";
					}
					else {
						successText = "failed";
					}
					Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
					return success;
				}
				public abstract object GetResult(out int level);
			}
			public void Run() {
				bool allTestsSucessful = true;
				foreach (Type testType in this.GetType().GetNestedTypes()) {
					if (testType.IsSubclassOf(typeof(Test))) {
						Test test = (Test)testType.GetConstructor(new Type[] { }).Invoke(null);
						if (!test.RunTest()) {
							allTestsSucessful = false;
						}
					}
				}
				if (!allTestsSucessful) {
					Console.ReadLine();
				}
			}
			public const char indentationChar = '\t';
			private static bool UseToStringMethod(Type type) {
				return (!type.IsValueType || type.IsPrimitive)
					&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();
			}
			private static bool UseProperty(PropertyInfo property, int level) {
				return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly();
			}
			public static void Serialize(object obj, string indent, StringBuilder builder, int level) {
				if (obj == null) {
					builder.Append(indent + "null\n");
				}
				else if (UseToStringMethod(obj.GetType())) {
					builder.Append(indent + "\"" + obj.ToString() + "\"" + "\n");
				}
				else {
					foreach (PropertyInfo property in obj.GetType().GetProperties()) {
						if (UseProperty((PropertyInfo)property, level)) {
							object val = property.GetValue(obj, null);
							builder.Append(indent + property.Name);
							if (val != null) {
								builder.Append(" (" + val.GetType().Name + ")");
							}
							builder.Append(":\n");
							Serialize(val, indent + indentationChar, builder, level);
						}
					}
					string specialEnumerableSerializationText;
					if (obj is ISerializeEnumerableSpecial && (specialEnumerableSerializationText = ((ISerializeEnumerableSpecial)obj).Serialize()) != null) {
						builder.Append(indent + specialEnumerableSerializationText + "\n");
					}
					else if (obj is System.Collections.IEnumerable) {
						foreach (object entry in (System.Collections.IEnumerable)obj) {
							builder.Append(indent + "Entry (" + entry.GetType().Name + ")\n");
							Serialize(entry, indent + indentationChar, builder, level);
						}
					}
				}
			}
		}
		public class MetaTest : TestRunner {
			public static int Leaves(Map map) {
				int count = 0;
				foreach (KeyValuePair<Map, Map> pair in map) {
					if (pair.Value.GetNumber()!=null) {
						count++;
					}
					else {
						count += Leaves(pair.Value);}}
				return count;
			}
			public static string TestPath {
				get {
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			//public class Serialization : Test {
			//    public override object GetResult(out int level) {
			//        level = 1;
			//        string text = File.ReadAllText(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"));
			//        for (int i = 0; i < 5; i++) {
			//            Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"));
			//            foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
			//                cached.Clear();
			//            }
			//        }
			//        return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
			//    }
			//}
			public class LibraryCode : Test {
				public override object GetResult(out int level) {
					level = 1;
					return Meta.Serialization.Serialize(Parser.Parse(Interpreter.LibraryPath));
				}
			}
			public class Basic : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"), new DictionaryMap(1, "first argument", 2, "second argument"));
				}
			}
			public class Library : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), new DictionaryMap());
				}
			}
			//public class FiboFast : Test {
			//    public override object GetResult(out int level) {
			//        level = 2;
			//        return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"fastFibo.meta"), new DictionaryMap());
			//    }
			//}
			public class Fibo : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"fibo.meta"), new DictionaryMap());
				}
			}
			public class Serialization : Test {
				public override object GetResult(out int level) {
					level = 1;
					string fileName = Path.Combine(Interpreter.InstallationPath, @"basicTest.meta");
					string text = File.ReadAllText(fileName);
					for (int i = 0; i < 100; i++) {
						Parser.ParseString(text, fileName);
						foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
							cached.Clear();
						}
					}
					return Meta.Serialization.Serialize(Parser.ParseString(text, fileName));//Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
					//return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
				}
			}
			public class MergeSort : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"mergeSort.meta"), new DictionaryMap());
				}
			}
			//public class FiboSlow : Test {
			//    public override object GetResult(out int level) {
			//        level = 2;
			//        return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"slowFibo.meta"), new DictionaryMap());
			//    }
			//}
		}
		namespace TestClasses {
			public class MemberTest {
				public static void Compile(int i) {
					Console.WriteLine("hello");
				}
				public static string classField = "default";
				public string instanceField = "default";
				public static string OverloadedMethod(string argument) {
					return "string function, argument+" + argument;
				}
				public static string OverloadedMethod(int argument) {
					return "integer function, argument+" + argument;
				}
				public static string OverloadedMethod(MemberTest memberTest, int argument) {
					return "MemberTest function, argument+" + memberTest + argument;
				}
				public static string ClassProperty {
					get {
						return classField;
					}
					set {
						classField = value;
					}
				}
				public string InstanceProperty {
					get {
						return this.instanceField;}
					set {
						this.instanceField = value;
					}
				}
			}
			public delegate object IntEvent(object intArg);
			public delegate object NormalEvent(object sender);
			public class TestClass {
				public class NestedClass {
					public static int field = 0;
				}
				public TestClass() {}
				public object CallInstanceEvent(object intArg) {
					return instanceEvent(intArg);
				}
				public static object CallStaticEvent(object sender) {
					return staticEvent(sender);
				}
				public event IntEvent instanceEvent;
				public static event NormalEvent staticEvent;
				protected string x = "unchangedX";
				protected string y = "unchangedY";
				protected string z = "unchangedZ";
				public static bool boolTest = false;

				public static object TestClass_staticEvent(object sender) {
					MethodBase[] m = typeof(TestClass).GetMethods();
					return null;
				}
				public delegate string TestDelegate(string x);
				public static Delegate del;
				public static void TakeDelegate(TestDelegate d) {
					del = d;
				}
				public static object GetResultFromDelegate() {
					return del.DynamicInvoke(new object[] { "argumentString"});
				}
				public double doubleValue = 0.0;
				public float floatValue = 0.0F;
				public decimal decimalValue = 0.0M;
			}
			public class PositionalNoConversion : TestClass {
				public PositionalNoConversion(string p1, string b, string p2) {
					this.x = p1;
					this.y = b;
					this.z = p2;}
				public string Concatenate(string p1, string b, string c) {
					return p1 + b + c + this.x + this.y + this.z;
				}
			}
			public class NamedNoConversion : TestClass {
				public NamedNoConversion(Map arg) {
					Map def = new DictionaryMap();
					def[1] = "null";
					def["y"] = "null";
					def["p2"] = "null";
					if (arg.ContainsKey(1)) {
						def[1] = arg[1];
					}
					if (arg.ContainsKey("y")) {
						def["y"] = arg["y"];
					}
					if (arg.ContainsKey("p2")) {
						def["y2"] = arg["y2"];
					}
					this.x = def[1].GetString();
					this.y = def["y"].GetString();
					this.z = def["p2"].GetString();
				}
				public string Concatenate(Map arg) {
					Map def = new DictionaryMap();
					def[1] = "null";
					def["b"] = "null";
					def["c"] = "null";
					if (arg.ContainsKey(1)) {
						def[1] = arg[1];
					}
					if (arg.ContainsKey("b")) {
						def["b"] = arg["b"];
					}
					if (arg.ContainsKey("c")) {
						def["c"] = arg["c"];
					}
					return def[1].GetString() + def["b"].GetString() + def["c"].GetString() +
						this.x + this.y + this.z;
				}
			}
			public class IndexerNoConversion : TestClass {
				public string this[string a] {
					get {
						return this.x + this.y + this.z + a;}
					set {
						this.x = a + value;
					}
				}
			}
		}
	}
	public class CodeKeys {
		public static readonly Map LastArgument = "lastArgument";
		public static readonly Map Discard = "discard";
		public static readonly Map Key = "key";
		public static readonly Map Root = "root";
		public static readonly Map Search = "search";
		public static readonly Map Current = "current";
		public static readonly Map Scope = "scope";
		public static readonly Map Literal = "literal";
		public static readonly Map Call = "call";
		public static readonly Map Select = "select";
		public static readonly Map Program = "program";
		public static readonly Map Keys = "keys";
		public static readonly Map Value = "value";
	}
	public class NumberKeys {
		public static readonly Map Negative = "negative";
		public static readonly Map Denominator = "denominator";
	}
	public class ExceptionLog {
		public ExceptionLog(Source source) {
			this.source = source;
		}
		public Source source;
	}
	public class MetaException : Exception {
		private string message;
		private Source source;
		private List<ExceptionLog> invocationList = new List<ExceptionLog>();
		public MetaException(string message, Source source) {
			this.message = message;
			this.source = source;
		}
		public List<ExceptionLog> InvocationList {
			get {
				return invocationList;
			}
		}
		public override string ToString() {
			string message = Message;
			if (invocationList.Count != 0) {
				message += "\n\nStack trace:";
			}
			foreach (ExceptionLog log in invocationList) {
				message += "\n" + GetSourceText(log.source);
			}
			return message;
		}
		public static string GetSourceText(Source source) {
			string text;
			if (source != null) {
				text = source.FileName + ", line ";
				text += source.Line + ", column " + source.Column;
			}
			else {
				text = "Unknown location";
			}
			return text;
		}
		public override string Message {
			get {
				return GetSourceText(source) + ": " + message;
			}
		}
		public Source Source {
			get {
				return source;
			}
		}
		public static int CountLeaves(Map map) {
			int count = 0;
			foreach (KeyValuePair<Map, Map> pair in map) {
				if (pair.Value == null) {
					count++;
				}
				else if (pair.Value.GetNumber()!=null) {
					count++;
				}
				else {
					count += CountLeaves(pair.Value);
				}
			}
			return count;
		}
	}
	// should we throw exceptions because of the syntax??
	// maybe only when parsing additional files?
	public class SyntaxException : MetaException {
		public SyntaxException(string message, Parser parser)
			: base(message, new Source(parser.state.Line, parser.state.Column, parser.state.FileName)) {
		}
	}
	public class ExecutionException : MetaException {
		private Map context;
		public ExecutionException(string message, Source source, Map context)
			: base(message, source) {
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException {
		public KeyDoesNotExist(Map key, Source source, Map map)
			: base("Key does not exist: " + Serialization.Serialize(key) + " in " + Serialization.Serialize(map), source, map) {}
	}
	public class KeyNotFound : ExecutionException {
		public KeyNotFound(Map key, Source source, Map map)
			: base("Key not found: " + Serialization.Serialize(key), source, map) {}
	}
	public class ListMap : ScopeMap {
		public override Number GetNumber() {
			if(list.Count==0) {
				return SmallInteger.Zero;
			}
			return null;
		}
		public override Map Copy() {
			return DeepCopy();
		}
	    public override void Append(Map map){
	        list.Add(map);
	    }
	    private List<Map> list;
		public ListMap(List<Map> list) {
			this.list = list;
		}
		public ListMap(int capacity) : this(new List<Map>(capacity)) {
		}
		public ListMap() : this(5) {
	    }
	    public override Map this[Map key] {
	        get {
	            if (ContainsKey(key)) {
					return list[key.GetNumber().GetInt32() - 1];
	            }
	            return null;
	        }
	        set {
                int integer = key.GetInt32();
                if (integer >= 1 && integer <= list.Count) {
                    list[integer - 1] = value;
                    return;
                }
                else if (integer == list.Count + 1) {
                    list.Add(value);
                    return;
	            }
	            throw new Exception("Cannot set key.");
	        }
	    }
	    public override int Count {
	        get {
	            return list.Count;
	        }
	    }
	    public override IEnumerable<Map> Array {
	        get {
	            return this.list;
	        }
	    }
	    public override int ArrayCount {
	        get {
	            return list.Count;
	        }
	    }
		public override bool ContainsKey(Map key) {
			// use GetInteger here!!
			Number number = key.GetNumber();
			return number!=null && Number.GreaterEqual(number, Number.One) && Number.LessEqual(number, new SmallInteger(list.Count));
		}
	    public override IEnumerable<Map> Keys {
	        get {
	            for (int i = 1; i <= list.Count; i++) {
	                yield return i;
	            }
	        }
	    }
	}
	public class Library {
		public class Single {
			public static float Add(float a, float b) {
				return a + b;
			}
			public static float Subtract(float a, float b) {
				return a - b;
			}
			public static float Multiply(float a, float b) {
				return a * b;
			}
			public static float Divide(float a, float b) {
				return a / b;
			}
			public static bool Equal(float a, float b) {
				return a == b;
			}
			public static bool GreaterEqual(float a, float b) {
				return a >= b;
			}
			public static bool Greater(float a, float b) {
				return a > b;
			}
			public static bool LessEqual(float a, float b) {
				return a <= b;
			}
			public static bool Less(float a, float b) {
				return a < b;
			}
		}
		public class Double {
			public static double Add(double a, double b) {
				return a + b;
			}
			public static double Subtract(double a, double b) {
				return a - b;
			}
			public static double Multiply(double a, double b) {
				return a * b;
			}
			public static double Divide(double a, double b) {
				return a / b;
			}
			public static bool Equal(double a, double b) {
				return a == b;
			}
			public static bool GreaterEqual(double a, double b) {
				return a >= b;
			}
			public static bool Greater(double a, double b) {
				return a > b;
			}
			public static bool LessEqual(double a, double b) {
				return a <= b;
			}
			public static bool Less(double a, double b) {
				return a < b;
			}
		}
		public class Int32 {
			public static int Add(int a, int b) {
				return a + b;
			}
			public static int Subtract(int a, int b) {
				return a - b;
			}
			public static int Multiply(int a, int b) {
				return a * b;
			}
			public static int Divide(int a, int b) {
				return a / b;
			}

			public static int AddUnchecked(int a, int b) {
				unchecked {
					return a + b;
				}
			}
			public static int SubtractUnchecked(int a, int b) {
				unchecked {
					return a - b;
				}
			}
			public static int MultiplyUnchecked(int a, int b) {
				unchecked {
					return a * b;
				}
			}

			public static bool Equal(int a, int b) {
				return a == b;
			}
			public static bool GreaterEqual(int a, int b) {
				return a >= b;
			}
			public static bool Greater(int a, int b) {
				return a > b;
			}
			public static bool LessEqual(int a, int b) {
				return a <= b;
			}
			public static bool Less(int a, int b) {
				return a < b;
			}
		}
		public class Int64 {
			public static long Add(long a, long b) {
				return a + b;
			}
			public static long Subtract(long a, long b) {
				return a - b;
			}
			public static long Multiply(long a, long b) {
				return a * b;
			}
			public static long Divide(long a, long b) {
				return a / b;
			}
			public static long AddUnchecked(long a, long b) {
				unchecked {
					return a + b;
				}
			}
			public static long SubtractUnchecked(long a, long b) {
				unchecked {
					return a - b;
				}
			}
			public static long MultiplyUnchecked(long a, long b) {
				unchecked {
					return a * b;
				}
			}
			public static bool Equal(long a, long b) {
				return a == b;
			}
			public static bool GreaterEqual(long a, long b) {
				return a >= b;
			}
			public static bool Greater(long a, long b) {
				return a > b;
			}
			public static bool LessEqual(long a, long b) {
				return a <= b;
			}
			public static bool Less(long a, long b) {
				return a < b;
			}
		}
		public static Map MergeAll(List<Map> maps) {
			Map s = new DictionaryMap();
			foreach (Map m in maps) {
				foreach (Map key in m.Keys) {
					s[key] = m[key];
				}
			}
			return s;
		}

		public static Map Slice(Map array,int start,int end) {
			return new ListMap(new List<Map>(array.Array).GetRange(start - 1, Math.Max(end - start + 1, 0)));
		}
		public static Map Select(Map array,Map function) {
			foreach(Map m in array.Array) {
				if(Convert.ToBoolean(function.Call(m).GetNumber().GetInt32())) {
					return m;
				}
			}
			throw new Exception("Predicate was false for all items in the array.");
		}
		public static Map Rest(Map m) {
			return new DictionaryMap(new List<Map>(m.Array).GetRange(1,m.ArrayCount-1));
		}
		public static Number Floor(Number n) {
			return new Rational(n.GetRealInt64());
		}
		public static Map While(Map condition,Map body) {
			while(Convert.ToBoolean(condition.Call(Map.Empty).GetNumber().GetInt32())) {
				body.Call(Map.Empty);
			}
			return Map.Empty;
		}
		public static void WriteLine(string s) {
			Console.WriteLine(s);
		}
		private static Random random = new Random();
		public static int Random(int lower,int upper) {
			return lower+Convert.ToInt32((random.NextDouble()*(upper-lower)));
		}
		public static string Trim(string text) {
			return text.Trim();
		}
		public static Map Modify(Map map, Map func) {
			Map result = new DictionaryMap();
			foreach (KeyValuePair<Map, Map> entry in map) {
				result[entry.Key] = func.Call(entry.Key).Call(entry.Value);
			}
			return result;
		}
		public static Map StringToNumber(Map map) {
			return Convert.ToInt32(map.GetString());
		}
		public static Map Foreach(Map map, Map func) {
			List<Map> result = new List<Map>();
			foreach (KeyValuePair<Map, Map> entry in map) {
				result.Add(func.Call(entry.Key).Call(entry.Value));
			}
			return new DictionaryMap(result);
		}
		public static Map Switch(Map map, Map cases) {
			foreach (KeyValuePair<Map, Map> entry in cases) {
				if (Convert.ToBoolean(entry.Key.Call(map).GetNumber().GetInt32())) {
					return entry.Value.Call(map);
				}
			}
			return new DictionaryMap();
		}
		public static int CompareNumber(Number a, Number b) {
			return a.CompareTo(b);
		}
		public static Map Sort(Map array, Map function) {
			List<Map> result = new List<Map>(array.Array);
			result.Sort(delegate(Map a, Map b) {
				return (int)Transform.ToDotNet(function.Call(a).Call(b), typeof(int));});
			return new DictionaryMap(result);
		}
		public static bool Equal(object a, object b) {
			return a.Equals(b);
		}
		public static Map Filter(Map array, Map condition) {
			List<Map> result = new List<Map>();
			foreach (Map m in array.Array) {
				if (Convert.ToBoolean(condition.Call(m).GetNumber().GetInt32())) {
					result.Add(m);
				}
			}
			return new DictionaryMap(result);
		}
		public static Map IfElse(bool condition, Map then, Map els) {
			if (condition) {
				return then.CallFast();
			}
			else {
				return els.CallFast();
			}
		}
		public static Map Sum(Map func, Map arg) {
			IEnumerator<Map> enumerator = arg.Array.GetEnumerator();
			if (enumerator.MoveNext()) {
				Map result = enumerator.Current.Copy();
				while (enumerator.MoveNext()) {
					result = func.Call(result).Call(enumerator.Current);
				}
				return result;}
			else {
				return Map.Empty;
			}
		}
		public static Map JoinAll(Map arrays) {
			List<Map> result = new List<Map>();
			foreach (Map array in arrays.Array) {
				result.AddRange(array.Array);
			}
			return new ListMap(result);
		}
		public static Map If(bool condition, Map then) {
			if (condition) {
				return then.Call(Map.Empty);}
			return Map.Empty;
		}
		public static Map Apply(Map func,Map array) {
			List<Map> result = new List<Map>();
			foreach (Map map in array.Array) {
				result.Add(func.Call(map));
			}
			return new DictionaryMap(result);
		}
		public static Map Append(Map array, Map item) {
			Map result=new ListMap(new List<Map>(array.Array));
			result.Append(item);
			return result;
		}
		public static Map EnumerableToArray(Map map) {
			List<Map> result = new List<Map>();
			foreach (object entry in (System.Collections.IEnumerable)((ObjectMap)map).Object) {
				result.Add(Transform.ToMeta(entry));}
			return new DictionaryMap(result);
		}
		public static Map Reverse(Map arg) {
			List<Map> list = new List<Map>(arg.Array);
			list.Reverse();
			return new DictionaryMap(list);
		}
		public static Map Try(Map tryFunction, Map catchFunction) {
			try {
				return tryFunction.Call(Map.Empty);
			}
			catch (Exception e) {
				return catchFunction.Call(new ObjectMap(e));
			}
		}
		public static Map With(Map o, Map values) {
			object obj = ((ObjectMap)o).Object;
			Type type = obj.GetType();
			foreach (KeyValuePair<Map, Map> entry in values) {
				Map value = entry.Value;
				if (entry.Key is ObjectMap) {
					DependencyProperty key = (DependencyProperty)((ObjectMap)entry.Key).Object;
					type.GetMethod("SetValue", new Type[] { typeof(DependencyProperty), typeof(Object) }).Invoke(obj, new object[] { key, Transform.ToDotNet(value, key.PropertyType) });
				}
				else {
					MemberInfo[] members = type.GetMember(entry.Key.GetString());
					if (members.Length != 0) {
						MemberInfo member = members[0];
						if (member is FieldInfo) {
							FieldInfo field = (FieldInfo)member;
							field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));}
						else if (member is PropertyInfo) {
							PropertyInfo property = (PropertyInfo)member;
							object converted = Transform.ToDotNet(value, property.PropertyType);
							property.SetValue(obj, converted, null);
						}
						else if (member is EventInfo) {
							EventInfo eventInfo = (EventInfo)member;
							new Method(eventInfo.GetAddMethod(), obj, type,eventInfo).Call(value);
						}
						else {
							throw new Exception("unknown member type");
						}
					}
					else {
						o[entry.Key] = entry.Value;
					}
				}
			}
			return o;
		}
		[MergeCompile]
		public static Map MergeAll(Map array) {
			Map result = new DictionaryMap();
			foreach (Map map in array.Array) {
				foreach (KeyValuePair<Map, Map> pair in map) {
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		[MergeCompile]
		public static Map Merge(Map arg, Map map) {
			arg=arg.Copy();
			foreach (KeyValuePair<Map, Map> pair in map) {
				arg[pair.Key] = pair.Value;
			}
			return arg;
		}
		public static Map Join(Map arg, Map map) {
			foreach (Map m in map.Array) {
				arg.Append(m);
			}
			return arg;
		}
		public static Map Range(Number arg) {
			Map result = new ListMap();
			for (int i = 1; Number.LessEqual(new SmallInteger(i), arg); i++) {
				result.Append(i);
			}
			return result;
		}
	}
	public class LiteralExpression : Expression {
		private Map literal;
		public LiteralExpression(Map code)
			: base(code) {
						//: base(null, parent) {
//public LiteralExpression(Map literal) : base(null, parent) {
			this.literal = literal;
		}
		public override Compiled GetCompiled(Expression parent) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
	// remove this, if possible
	// what to do about the library?
	public class LiteralStatement : Statement {
		private LiteralExpression program;
		public LiteralStatement(LiteralExpression program)
			: base(null, null, 0) {
			this.program = program;}
		//public override Map Pre() {
		//    return program.EvaluateStructure();
		//}
		//protected override Map CurrentImplementation(Map previous) {
		//    return program.EvaluateStructure();
		//}
		public override CompiledStatement Compile() {
			throw new Exception("The method or operation is not implemented.");
		}
	}
	// remove/refactor this?
	// make it its own expression
	// merging of constants should be done at compile-times
	public class MergeCompile:CompilableAttribute {
		public override Map GetStructure() {
			return null;
		}
	}
	public abstract class CompilableAttribute : Attribute {
		public abstract Map GetStructure();
	}
	public class EmptyMap:ScopeMap {
		public EmptyMap(Map scope) {
			this.Scope = scope;
		}
		public override int GetHashCode() {
			return 0;
		}
		public override bool Equals(object obj) {
			Map map=obj as Map;
			return map!=null && map.Count==0;
		}
		public override Map this[Map key] {
			get {
				return null;
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				yield break;
			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override int Count {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(Map key) {
			return false;
		}
		public override void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map Call(Map arg) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override string GetString() {
			return "";
		}
		public override Map Copy() {
			return new EmptyMap(Scope);
		}
		public override Number GetNumber() {
			return Zero;
		}
	}
	public abstract class Map:IEnumerable<KeyValuePair<Map, Map>>, ISerializeEnumerableSpecial {
		public virtual Integer GetInteger() {
			Number number = GetNumber();
			if (number != null) {
				return number.GetInteger();
			}
			return null;
		}
		public virtual Map GetFromIndex(int i) {
			throw new Exception("not implemented");
		}
		public virtual bool LessThan(Map map) {
			return GetNumber().LessThan(map.GetNumber());
		}
		public virtual int GetInt32() {
			return GetNumber().GetInt32();
		}
		public virtual Map Copy(Map scope) {
			Map copy = Copy();
			copy.Scope = scope;
			return copy;
		}
		public virtual void CopyInternal(Map map) {
			throw new Exception("not implemented");
		}
		public static int GetHashCode(int i) {
			return i % int.MaxValue;
		}
		public override int GetHashCode() {
			if (GetNumber()!=null) {
				return (int)(GetNumber().Numerator.GetInt32() % int.MaxValue);
			}
			else {
				unchecked {
					int hash = int.MaxValue / Count;
					if (ContainsKey(Map.One)) {
						hash += this[Map.One].GetHashCode();
					}
					return hash;
				}
			}
		}
		public static SmallInteger Zero = new SmallInteger(0);
		public static SmallInteger One = new SmallInteger(1);
		public static SmallInteger Two = new SmallInteger(1);
		public virtual object GetObject() {
			return this;
		}
		public override bool Equals(object obj) {
			if (ReferenceEquals(obj, this)) {
				return true;
			}
			Map map = obj as Map;
			if (map != null && map.Count == Count) {
				foreach (Map key in this.Keys) {
					Map otherValue = map[key];
					Map thisValue = this[key];
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue)) {
						return false;
					}
				}
				return true;
			}
			return false;
		}
		public static Map CopyMap(Map map) {
			Map copy = new DictionaryMap();
			foreach (Map key in map.Keys) {
				copy[key] = map[key];
			}
			copy.Scope = map.Scope;
			//copy.FirstCall = map.FirstCall;
			//copy.firstCall = map.firstCall;
			return copy;
		}
		public virtual string GetString() {
			if (ArrayCount == Count) {
				StringBuilder text = new StringBuilder("");
				foreach (Map map in Array) {
					Number number = map.GetNumber();
					if (number == null) {
						return null;
					}
					else {
						if (number.GetInt32() > Char.MinValue && number.GetInt32() < Char.MaxValue) {
							text.Append(Convert.ToChar(number.GetInt32()));
						}
						else {
							return null;
						}
					}
				}
				return text.ToString();
			}
			return null;
		}
		public abstract int Count {
			get;
		}
		public virtual void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public virtual Type GetClass() {
			return null;
		}
		public virtual Map CallFast() {
			if (Optimized==null) {
				//FirstCall = false;
				Optimized = Expression.Optimize(GetExpression());
			}
			// maybe get an optimized call expression
			return Optimized.CallFast(this.Scope);
		}
		//public virtual Map CallFast() {
		//    if (FirstCall) {
		//        FirstCall = false;
		//        Expression = Expression.Optimize(GetExpression());
		//    }
		//    return GetExpression().CallFast(this.Scope);
		//}
		//public virtual bool FirstCall {
		//    get {
		//        return _firstCall;
		//    }
		//    set {
		//        _firstCall = value;
		//    }
		//}
		private Expression optimized;
		public virtual Expression Optimized {
			get {
				return optimized;
			}
			set {
				optimized = value;
			}
		}
		public virtual Map Call(Map argument) {
			try {
				if (Optimized==null) {
					Optimized = Expression.Optimize(GetExpression());
				}
				return Optimized.Call(argument, this.Scope);
			}
			catch (Exception e) {
			    return GetExpression().Call(argument, this.Scope);
			}
		}
		public static Map Empty=new EmptyMap(null);
		public Map DeepCopy() {
			DictionaryMap clone = new DictionaryMap();
			clone.Scope = Scope;
			clone.Source = Source;
			clone.Expression = Expression;
			clone.Optimized = Optimized;
			//clone.FirstCall = FirstCall;
			//clone.IsConstant = this.IsConstant;
			foreach (Map key in Keys) {
				try {
					clone[key] = this[key].Copy();
				}
				catch {
					clone[key] = this[key].Copy();
				}
			}
			return clone;
		}
		public abstract Map Copy();

		public static Stack<Map> arguments = new Stack<Map>();
		public override string ToString() {
			if (Count == 0) {
				return "0";
			}
			else if (IsString) {
				return GetString();
			}
			else {
				return Meta.Serialization.Serialize(this);
			}
		}
		public static Dictionary<object, Profile> calls = new Dictionary<object, Profile>();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
		public virtual string Serialize() {
			string text;
			if (this.Count == 0) {
				text = "0";
			}
			else if (this.IsString) {
				text = "\"" + this.GetString() + "\"";
			}
			else if (this.GetNumber()!=null) {
				text = this.GetNumber().ToString();
			}
			else {
				text = null;
			}
			return text;
		}
		public static implicit operator Map(string text) {
			return new StringMap(text);
		}
		public static implicit operator Map(double number) {
			return new Rational(number);
		}
		public static implicit operator Map(int integer) {
			return new SmallInteger(integer);
		}

		public static Dictionary<Map, Extent> sources = new Dictionary<Map, Extent>(1000,new Comparer());
		public virtual Extent Source {
			get {
				Extent source;
				sources.TryGetValue(this,out source);
				if (source == null) {
					source = new Extent(new Source(0, 0, ""), new Source(0, 0, ""));
				}
				return source;
			}
			set {
				//sources[this] = value;
			}
		}
		// probably buggy
		//public virtual bool IsConstant {
		//    get {
		//        return true;
		//    }
		//    set {
		//    }
		//}
		public abstract int ArrayCount {
			get;
		}
		public bool IsString {
		    get {
				return GetString()!=null;
			}
		}
		public abstract IEnumerable<Map> Array {
			get;
		}
		public abstract Number GetNumber();
		public abstract bool ContainsKey(Map key);
		public abstract IEnumerable<Map> Keys {
			get;
		}
		public abstract Map this[Map key] {
			get;
			set;
		}
		public virtual Map Scope {
			get {
				return null;
			}
			set {
			}
		}
		public void Compile(Expression parent) {
			GetExpression(parent).Compile();
		}
		public Expression GetExpression() {
			return GetExpression(null);
		}
		public class Comparer : IEqualityComparer<Map> {
			bool IEqualityComparer<Map>.Equals(Map x, Map y) {
				return ReferenceEquals(x, y);
			}
			int IEqualityComparer<Map>.GetHashCode(Map obj) {
				return ((object)obj).GetHashCode();
			}
		}

		public virtual Expression Expression {
			get {
				return null;
			}
			set {
			}
		}
		public virtual Expression GetExpression(Expression parent) {
			if (Expression == null) {
				Expression = CreateExpression(parent);
			}
			return Expression;
		}
		static Map() {
			CodeKeys bla=new CodeKeys();
			statements[CodeKeys.Keys] = typeof(SearchStatement);
			statements[CodeKeys.Current] = typeof(CurrentStatement);
			statements[CodeKeys.Key] = typeof(KeyStatement);
			statements[CodeKeys.Discard] = typeof(DiscardStatement);

			expressions[CodeKeys.Call] = typeof(Call);
			expressions[CodeKeys.Program] = typeof(Program);
			expressions[CodeKeys.Literal] = typeof(Literal);
			expressions[CodeKeys.Select] = typeof(Select);
			expressions[CodeKeys.Root] = typeof(Root);
			expressions[CodeKeys.Search]= typeof(Search);
		}
		public static Dictionary<Map, Type> expressions = new Dictionary<Map, Type>();
		public static Dictionary<Map, Type> statements = new Dictionary<Map, Type>();
		public Expression CreateExpression(Expression parent) {
		    if(this.Count==1) {
				if (ContainsKey(CodeKeys.LastArgument)) {
					return new LastArgument(this[CodeKeys.LastArgument]);
					//return new LastArgument(this[CodeKeys.LastArgument], parent);
				}
				foreach(Map key in Keys) {
				    if(expressions.ContainsKey(key)) {
						Expression x = (Expression)expressions[key].GetConstructor(new Type[] { typeof(Map)}
						).Invoke(new object[] { this[key]});

						//Expression x=(Expression)expressions[key].GetConstructor(new Type[] {typeof(Map),typeof(Expression)}
						//).Invoke(new object[] {this[key],parent});
						x.Source = Source;
						return x;
				    }
				}
			}
			return null;
		}
		public Statement GetStatement(Program program, int index) {
			foreach(KeyValuePair<Map,Type> pair in statements) {
			    if(ContainsKey(pair.Key)) {
			        return (Statement)pair.Value.GetConstructors()[0].Invoke(
			            new object[] {this[pair.Key].GetExpression(program),
							this[CodeKeys.Value].GetExpression(program),
			                program,
			                index
						}
					);
			    }
			}
			return null;
		}
		public IEnumerator<KeyValuePair<Map, Map>> GetEnumerator() {
			foreach (Map key in Keys) {
				yield return new KeyValuePair<Map, Map>(key, this[key]);
			}
		}
	}
	namespace Fusion {
		//-------------------------------------------------------------
		// Copyright Junfeng Zhang
		//
		// This implements managed wrappers to GAC API Interfaces
		//-------------------------------------------------------------

		//-------------------------------------------------------------
		// Interfaces defined by fusion
		//-------------------------------------------------------------
		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
		internal interface IAssemblyCache {
			[PreserveSig()]
			int UninstallAssembly(
								int flags,
								[MarshalAs(UnmanagedType.LPWStr)]
							String assemblyName,
								InstallReference refData,
								out AssemblyCacheUninstallDisposition disposition);

			[PreserveSig()]
			int QueryAssemblyInfo(
								int flags,
								[MarshalAs(UnmanagedType.LPWStr)]
							String assemblyName,
								ref AssemblyInfo assemblyInfo);
			[PreserveSig()]
			int Reserved(
								int flags,
								IntPtr pvReserved,
								out Object ppAsmItem,
								[MarshalAs(UnmanagedType.LPWStr)]
							String assemblyName);
			[PreserveSig()]
			int Reserved(out Object ppAsmScavenger);

			[PreserveSig()]
			int InstallAssembly(
								int flags,
								[MarshalAs(UnmanagedType.LPWStr)]
							String assemblyFilePath,
								InstallReference refData);
		}// IAssemblyCache

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
		internal interface IAssemblyName {
			[PreserveSig()]
			int SetProperty(
					int PropertyId,
					IntPtr pvProperty,
					int cbProperty);

			[PreserveSig()]
			int GetProperty(
					int PropertyId,
					IntPtr pvProperty,
					ref int pcbProperty);

			[PreserveSig()]
			int Finalize();

			[PreserveSig()]
			int GetDisplayName(
					StringBuilder pDisplayName,
					ref int pccDisplayName,
					int displayFlags);

			[PreserveSig()]
			int Reserved(ref Guid guid,
				Object obj1,
				Object obj2,
				String string1,
				Int64 llFlags,
				IntPtr pvReserved,
				int cbReserved,
				out IntPtr ppv);

			[PreserveSig()]
			int GetName(
					ref int pccBuffer,
					StringBuilder pwzName);

			[PreserveSig()]
			int GetVersion(
					out int versionHi,
					out int versionLow);
			[PreserveSig()]
			int IsEqual(
					IAssemblyName pAsmName,
					int cmpFlags);

			[PreserveSig()]
			int Clone(out IAssemblyName pAsmName);
		}// IAssemblyName

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
		internal interface IAssemblyEnum {
			[PreserveSig()]
			int GetNextAssembly(
					IntPtr pvReserved,
					out IAssemblyName ppName,
					int flags);
			[PreserveSig()]
			int Reset();
			[PreserveSig()]
			int Clone(out IAssemblyEnum ppEnum);
		}// IAssemblyEnum

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("582dac66-e678-449f-aba6-6faaec8a9394")]
		internal interface IInstallReferenceItem {
			// A pointer to a FUSION_INSTALL_REFERENCE structure. 
			// The memory is allocated by the GetReference method and is freed when 
			// IInstallReferenceItem is released. Callers must not hold a reference to this 
			// buffer after the IInstallReferenceItem object is released. 
			// This uses the InstallReferenceOutput object to avoid allocation 
			// issues with the interop layer. 
			// This cannot be marshaled directly - must use IntPtr 
			[PreserveSig()]
			int GetReference(
					out IntPtr pRefData,
					int flags,
					IntPtr pvReserced);
		}// IInstallReferenceItem

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f")]
		internal interface IInstallReferenceEnum {
			[PreserveSig()]
			int GetNextInstallReferenceItem(
					out IInstallReferenceItem ppRefItem,
					int flags,
					IntPtr pvReserced);
		}// IInstallReferenceEnum

		public enum AssemblyCommitFlags {
			Default = 1,
			Force = 2
		}// enum AssemblyCommitFlags

		public enum AssemblyCacheUninstallDisposition {
			Unknown = 0,
			Uninstalled = 1,
			StillInUse = 2,
			AlreadyUninstalled = 3,
			DeletePending = 4,
			HasInstallReference = 5,
			ReferenceNotFound = 6
		}

		[Flags]
		internal enum AssemblyCacheFlags {
			GAC = 2,
		}

		internal enum CreateAssemblyNameObjectFlags {
			CANOF_DEFAULT = 0,
			CANOF_PARSE_DISPLAY_NAME = 1,
		}

		[Flags]
		internal enum AssemblyNameDisplayFlags {
			VERSION = 0x01,
			CULTURE = 0x02,
			PUBLIC_KEY_TOKEN = 0x04,
			PROCESSORARCHITECTURE = 0x20,
			RETARGETABLE = 0x80,
			// This enum will change in the future to include
			// more attributes.
			ALL = VERSION
										| CULTURE
										| PUBLIC_KEY_TOKEN
										| PROCESSORARCHITECTURE
										| RETARGETABLE
		}

		[StructLayout(LayoutKind.Sequential)]
		public class InstallReference {
			public InstallReference(Guid guid, String id, String data) {
				cbSize = (int)(2 * IntPtr.Size + 16 + (id.Length + data.Length) * 2);
				flags = 0;
				// quiet compiler warning 
				if (flags == 0) { }
				guidScheme = guid;
				identifier = id;
				description = data;
			}

			public Guid GuidScheme {
				get { return guidScheme; }
			}

			public String Identifier {
				get { return identifier; }
			}

			public String Description {
				get { return description; }
			}

			int cbSize;
			int flags;
			Guid guidScheme;
			[MarshalAs(UnmanagedType.LPWStr)]
			String identifier;
			[MarshalAs(UnmanagedType.LPWStr)]
			String description;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct AssemblyInfo {
			public int cbAssemblyInfo; // size of this structure for future expansion
			public int assemblyFlags;
			public long assemblySizeInKB;
			[MarshalAs(UnmanagedType.LPWStr)]
			public String currentAssemblyPath;
			public int cchBuf; // size of path buf.
		}

		[ComVisible(false)]
		public class InstallReferenceGuid {
			public static bool IsValidGuidScheme(Guid guid) {
				return (guid.Equals(UninstallSubkeyGuid) ||
						guid.Equals(FilePathGuid) ||
						guid.Equals(OpaqueGuid) ||
						guid.Equals(Guid.Empty));
			}

			public readonly static Guid UninstallSubkeyGuid = new Guid("8cedc215-ac4b-488b-93c0-a50a49cb2fb8");
			public readonly static Guid FilePathGuid = new Guid("b02f9d65-fb77-4f7a-afa5-b391309f11c9");
			public readonly static Guid OpaqueGuid = new Guid("2ec93463-b0c3-45e1-8364-327e96aea856");
			// these GUID cannot be used for installing into GAC.
			public readonly static Guid MsiGuid = new Guid("25df0fc1-7f97-4070-add7-4b13bbfd7cb8");
			public readonly static Guid OsInstallGuid = new Guid("d16d444c-56d8-11d5-882d-0080c847b195");
		}

		[ComVisible(false)]
		public static class AssemblyCache {
			public static void InstallAssembly(String assemblyPath, InstallReference reference, AssemblyCommitFlags flags) {
				if (reference != null) {
					if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
						throw new ArgumentException("Invalid reference guid.", "guid");
				}

				IAssemblyCache ac = null;

				int hr = 0;

				hr = Utils.CreateAssemblyCache(out ac, 0);
				if (hr >= 0) {
					hr = ac.InstallAssembly((int)flags, assemblyPath, reference);
				}

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}
			}

			// assemblyName has to be fully specified name. 
			// A.k.a, for v1.0/v1.1 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx".
			// For v2.0 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx, ProcessorArchitecture=xx".
			// If assemblyName is not fully specified, a random matching assembly will be uninstalled. 
			public static void UninstallAssembly(String assemblyName, InstallReference reference, out AssemblyCacheUninstallDisposition disp) {
				AssemblyCacheUninstallDisposition dispResult = AssemblyCacheUninstallDisposition.Uninstalled;
				if (reference != null) {
					if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
						throw new ArgumentException("Invalid reference guid.", "guid");
				}

				IAssemblyCache ac = null;

				int hr = Utils.CreateAssemblyCache(out ac, 0);
				if (hr >= 0) {
					hr = ac.UninstallAssembly(0, assemblyName, reference, out dispResult);
				}

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				disp = dispResult;
			}

			// See comments in UninstallAssembly
			public static String QueryAssemblyInfo(String assemblyName) {
				if (assemblyName == null) {
					throw new ArgumentException("Invalid name", "assemblyName");
				}

				AssemblyInfo aInfo = new AssemblyInfo();

				aInfo.cchBuf = 1024;
				// Get a string with the desired length
				aInfo.currentAssemblyPath = new String('\0', aInfo.cchBuf);

				IAssemblyCache ac = null;
				int hr = Utils.CreateAssemblyCache(out ac, 0);
				if (hr >= 0) {
					hr = ac.QueryAssemblyInfo(0, assemblyName, ref aInfo);
				}
				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				return aInfo.currentAssemblyPath;
			}
		}

		[ComVisible(false)]
		public class AssemblyCacheEnum {
			// null means enumerate all the assemblies
			public AssemblyCacheEnum(String assemblyName) {
				IAssemblyName fusionName = null;
				int hr = 0;

				if (assemblyName != null) {
					hr = Utils.CreateAssemblyNameObject(
							out fusionName,
							assemblyName,
							CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
							IntPtr.Zero);
				}

				if (hr >= 0) {
					hr = Utils.CreateAssemblyEnum(
							out m_AssemblyEnum,
							IntPtr.Zero,
							fusionName,
							AssemblyCacheFlags.GAC,
							IntPtr.Zero);
				}

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}
			}

			public String GetNextAssembly() {
				int hr = 0;
				IAssemblyName fusionName = null;

				if (done) {
					return null;
				}

				// Now get next IAssemblyName from m_AssemblyEnum
				hr = m_AssemblyEnum.GetNextAssembly((IntPtr)0, out fusionName, 0);

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				if (fusionName != null) {
					return GetFullName(fusionName);
				}
				else {
					done = true;
					return null;
				}
			}

			private String GetFullName(IAssemblyName fusionAsmName) {
				StringBuilder sDisplayName = new StringBuilder(1024);
				int iLen = 1024;

				int hr = fusionAsmName.GetDisplayName(sDisplayName, ref iLen, (int)AssemblyNameDisplayFlags.ALL);
				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				return sDisplayName.ToString();
			}

			private IAssemblyEnum m_AssemblyEnum = null;
			private bool done;
		}// class AssemblyCacheEnum

		public class AssemblyCacheInstallReferenceEnum {
			public AssemblyCacheInstallReferenceEnum(String assemblyName) {
				IAssemblyName fusionName = null;

				int hr = Utils.CreateAssemblyNameObject(
							out fusionName,
							assemblyName,
							CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
							IntPtr.Zero);

				if (hr >= 0) {
					hr = Utils.CreateInstallReferenceEnum(out refEnum, fusionName, 0, IntPtr.Zero);
				}

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}
			}

			public InstallReference GetNextReference() {
				IInstallReferenceItem item = null;
				int hr = refEnum.GetNextInstallReferenceItem(out item, 0, IntPtr.Zero);
				if ((uint)hr == 0x80070103) {   // ERROR_NO_MORE_ITEMS
					return null;
				}

				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				IntPtr refData;
				InstallReference instRef = new InstallReference(Guid.Empty, String.Empty, String.Empty);

				hr = item.GetReference(out refData, 0, IntPtr.Zero);
				if (hr < 0) {
					Marshal.ThrowExceptionForHR(hr);
				}

				Marshal.PtrToStructure(refData, instRef);
				return instRef;
			}

			private IInstallReferenceEnum refEnum;
		}

		internal class Utils {
			[DllImport("fusion.dll")]
			internal static extern int CreateAssemblyEnum(
					out IAssemblyEnum ppEnum,
					IntPtr pUnkReserved,
					IAssemblyName pName,
					AssemblyCacheFlags flags,
					IntPtr pvReserved);

			[DllImport("fusion.dll")]
			internal static extern int CreateAssemblyNameObject(
					out IAssemblyName ppAssemblyNameObj,
					[MarshalAs(UnmanagedType.LPWStr)]
				String szAssemblyName,
					CreateAssemblyNameObjectFlags flags,
					IntPtr pvReserved);

			[DllImport("fusion.dll")]
			internal static extern int CreateAssemblyCache(
					out IAssemblyCache ppAsmCache,
					int reserved);

			[DllImport("fusion.dll")]
			internal static extern int CreateInstallReferenceEnum(
					out IInstallReferenceEnum ppRefEnum,
					IAssemblyName pName,
					int dwFlags,
					IntPtr pvReserved);
		}
	}
}