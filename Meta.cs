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
using SdlDotNet.Sprites;
using SdlDotNet.Windows;
using SdlDotNet;
using System.GACManagedAccess;

namespace Meta {
	public delegate Map Compiled(Map map);
	public class Dict<TKey, TValue>:Dictionary<TKey,TValue> {
		public Dict() {
		}
		public TKey key;
		public static Dict<TKey, TValue> operator -(Dict<TKey, TValue> dict, TKey key) {
			dict.key = key;
			return dict;
		}
		public static Dict<TKey, TValue> operator +(Dict<TKey, TValue> dict, TValue value) {
			dict[dict.key] = value;
			return dict;
		}
	}
	public abstract class Expression {
		public static Expression LastArgument(Map code, Expression parent) {
			return new CustomExpression(
				code.Source,
				parent,
				delegate { return null; },
				delegate(Expression p) {
					return new Compiled(delegate(Map map) {
						return Map.arguments.Peek();
					});
				}
			);
		}
		public Compiled GetCompiled() {	
			if(compiled==null) {	
				compiled=Compile();
			}
			return compiled;
		}
		public Compiled compiled;
		public bool isFunction = false;
		public Extent Source;
		public readonly Expression Parent;
		public Statement Statement;
		public Expression(Extent source, Expression parent) {	
			this.Source = source;
			this.Parent = parent;
		}
		private bool evaluated = false;
		private Map structure;
		public Map GetConstant() {
			Map s = EvaluateStructure();
			return s != null && s.IsConstant ? s : null;
		}
		public Map EvaluateMapStructure() {
			return EvaluateStructure();
		}
		public Map EvaluateStructure() {
			if (!evaluated) {
				structure = GetStructure();
				evaluated = true;
			}
			return structure;
		}
		public abstract Map GetStructure();
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
		public abstract Compiled GetCompiled(Expression parent);
	}
	public delegate Map StructureDelegate();
	public delegate Compiled CompiledDelegate(Expression parent);
	public class CustomExpression : Expression {
		private StructureDelegate structure;
		private CompiledDelegate compiled;
		public CustomExpression(Extent extent, Expression parent,StructureDelegate structure,CompiledDelegate compiled):base(extent,parent) {
			this.structure = structure;
			this.compiled = compiled;
		}
		public override Compiled GetCompiled(Expression parent) {
			return compiled(parent);
		}
		public override Map GetStructure() {
			return structure();
		}
	}
	public class Call : Expression {
		public List<Expression> calls;
		public Call(Map code, Expression parent): base(code.Source, parent) {
			this.calls = new List<Expression>();
			foreach (Map m in code.Array) {
				calls.Add(m.GetExpression(this));
			}
			if (calls.Count == 1) {
				calls.Add(new Literal(Map.Empty, this));
			}
		}
		public override Map GetStructure() {
			List<object> arguments;
			MethodBase method;
			if (CallStuff(out arguments, out method)) {
				if (method is ConstructorInfo) {
					Dictionary<Map, Member> type = ObjectMap.cache.GetMembers(method.DeclaringType);
					Map result = new DictionaryMap();
					result.IsConstant=false;
					foreach (Map key in type.Keys) {
						Member member = type[key];
						Map value;
						if (member is MethodMember) {
							value = member.Get(null);
						}
						else {
							value = Map.Empty;
						}
						result[key] = value;

					}
					return result;
				}
				else if (arguments != null && method.GetCustomAttributes(typeof(CompilableAttribute), false).Length != 0) {
					Map result = (Map)method.Invoke(null, arguments.ToArray());
					result.IsConstant = false;
					return result;
				}
				else if(method is MethodInfo) {
					Type type = ((MethodInfo)method).ReturnType;
					if (type != typeof(Map) && !type.IsSubclassOf(typeof(Map))) {
						return GetInstanceStructure(type);
					}
				}
			}
			return null;
		}
		public static Map GetInstanceStructure(Type t) {
			Dictionary<Map, Member> type = ObjectMap.cache.GetMembers(t);
			Map result = new DictionaryMap();
			result.IsConstant = false;
			foreach (Map key in type.Keys) {
				Member member = type[key];
				Map value;
				if (member is MethodMember) {
					value = member.Get(null);
				}
				else {
					value = Map.Empty;
				}
				result[key] = value;
			}
			return result;
		}
		public bool CallStuff(out List<object> arguments, out MethodBase m) {
			Map first = (Map)calls[0].GetConstant();
			if (first != null) {
				Method method;
				if (first is TypeMap) {
					method = (Method)((TypeMap)first).Constructor;
				}
				else if (first is Method) {
					method = (Method)first;
				}
				else {
					method = null;
				}
				if (method != null) {
					if (method.parameters.Length == calls.Count - 1 || (calls.Count == 2 && method.parameters.Length == 0)) {
						arguments = new List<object>();
						for (int i = 0; i < method.parameters.Length; i++) {
							Map arg = calls[i + 1].EvaluateMapStructure();
							if (arg == null) {
								m = method.method;
								return true;}
							else if(method.method.GetCustomAttributes(typeof(CompilableAttribute),false).Length!=0) {
								arguments.Add(Transform.ToDotNet(arg, method.parameters[i].ParameterType));
							}
						}
						m = method.method;
						return true;
					}
				}
			}
			arguments = null;
			m = null;
			return false;
		}
		public override Compiled GetCompiled(Expression parent) {
			List<object> arguments;
			MethodBase method;
			if (CallStuff(out arguments, out method)) {
				if (method.IsStatic) {
					List<Compiled> c=calls.GetRange(1, calls.Count - 1).ConvertAll<Compiled>(delegate(Expression e) {return e.Compile();});

					Compiled[] args = c.ToArray();
					ParameterInfo[] parameters = method.GetParameters();
					DynamicMethod m;
					MethodInfo methodInfo = (MethodInfo)method;
					Type[] param = new Type[] { typeof(Compiled[]), typeof(Map) };
					m = new DynamicMethod("Optimized", typeof(Map), param, typeof(Map).Module);
					ILGenerator il = m.GetILGenerator();
					for(int i=0;i<parameters.Length;i++) {
						Type type=parameters[i].ParameterType;
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldc_I4, i);
						il.Emit(OpCodes.Ldelem_Ref);
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Callvirt, typeof(Compiled).GetMethod("Invoke"));
						Transform.GetConversion(type, il);
					}
					if(method.IsStatic) {
						il.Emit(OpCodes.Call,methodInfo);
					}
					else {
						il.Emit(OpCodes.Callvirt,methodInfo);
					}
					Transform.GetMetaConversion(methodInfo.ReturnType,il);
					il.Emit(OpCodes.Ret);
					FastCall fastCall=(FastCall)m.CreateDelegate(typeof(FastCall),args);
					return delegate(Map context) {
						return fastCall(context);
					};
				}
			}
			if(calls.Count==2 && calls[0].GetConstant()!=null) {
				Map s = calls[1].EvaluateStructure();
			}
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
	public delegate object Conversion(Map map);
	
	public delegate Map FastCall(Map context);
	public class Search : Expression {
		public override Map GetStructure() {
			Map key;
			int count;
			Map value;
			if (FindStuff(out count, out key, out value)) {
				if(value!=null) {
					return value;
				}
				else {
					return null;
				}
			}
			else {
				return null;
			}
		}
		private bool FindStuff(out int count, out Map key, out Map value) {
			Expression current = this;
			key = expression.EvaluateStructure();
			count = 0;
			int programCounter=0;
			if (key != null && key.IsConstant) {
				bool hasCrossedFunction = false;
				while (true) {
					while (current.Statement == null) {
						if (current.isFunction) {
							hasCrossedFunction = true;
							count++;
						}
						current = current.Parent;
						if (current == null) {
							break;
						}
					}
					if (current == null) {
						break;
					}
					Statement statement = current.Statement;
					Map structure = statement.PreMap();
					if (structure == null) {
						statement.Pre();
						break;
					}
					if (structure.ContainsKey(key)) {
						value = structure[key];
						return true;
					}
					else if(programCounter<1 && statement is KeyStatement) {
						if(hasCrossedFunction) {
							Map map=statement.CurrentMap();
							if(map!=null && map.IsConstant) {
								if(map.ContainsKey(key)) {
									value=map[key];
									if(value.IsConstant) {
										return true;
									}
								}
							}
						}
					}
					if (hasCrossedFunction) {
						if (!statement.NeverAddsKey(key)) {
							break;}
					}
					count++;
					if(current.Statement!=null && current.Statement.program!=null && !current.Statement.program.isFunction) {
						programCounter++;
					}
					current = current.Parent;
				}
			}
			value = null;
			return false;
		}
		private Expression expression;
		public Search(Map code, Expression parent)
			: base(code.Source, parent) {
			this.expression = code.GetExpression(this);
		}
		public override Compiled GetCompiled(Expression parent) {
			int count;
			Map key;
			Map value;
			if (FindStuff(out count, out key, out value)) {
			    if (value != null && value.IsConstant) {
					return delegate(Map context) {
						return value;
					};
				}
			    else {
					return delegate(Map context) {
						Map selected = context;
						for (int i = 0; i < count; i++) {
							selected = selected.Scope;
						}
						Map result=selected[key];
						if (result==null) {
							selected = context;
							int realCount = 0;
							while (!selected.ContainsKey(key)) {
								selected = selected.Scope;
								realCount++;
								if (selected == null) {
									throw new KeyNotFound(key, Source.Start, null);
								}
							}
							return selected[key];
						}
						return result;
					};
				}}
			else {
			    FindStuff(out count, out key, out value);
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
	}
	public class FunctionArgument:Map {
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
					this.value=value;
				}
				else {
					throw new Exception("The method or operation is not implemented.");
				}
			}
		}
		public override IEnumerable<Map> Keys {
			get { throw new Exception("The method or operation is not implemented."); }
		}
		public override bool IsNormal {
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
		public FunctionArgument(Map key,Map value) {
			this.key=key;
			this.value=value;
		}
		public override bool ContainsKey(Map k) {
			return k.Equals(this.key);
		}
		public override Number GetNumber() {
			throw new Exception("The method or operation is not implemented.");
		}
		public override int Count {
			get { throw new Exception("The method or operation is not implemented."); }
		}
	}
	public class Function:Program {
		public override Compiled GetCompiled(Expression parent) {
			Compiled e = expression.Compile();
			Map parameter = key;
			return delegate (Map p) {
				Map context = new FunctionArgument(parameter, Map.arguments.Peek());
				context.Scope = p;
				return e(context);
			};
		}
		public Expression expression;
		public Map key;
		public Function(Expression parent,Map code):base(code.Source,parent) {
			isFunction = true;
			Map parameter = code[CodeKeys.Parameter];
			if (parameter.Count != 0) {
				Literal para=new Literal(parameter, this);
				para.Source=code.Source;
				KeyStatement s = new KeyStatement(
					para,
					LastArgument(Map.Empty, this), this, 0);
				statementList.Add(s);
				this.key=parameter;
			}
			this.expression=code[CodeKeys.Expression].GetExpression(this);
			CurrentStatement c = new CurrentStatement(null,expression, this, statementList.Count);
			statementList.Add(c);
		}
	}
	public class FunctionMap:Map {
		public override Map Copy() {
			return DeepCopy();
		}
		public override Map this[Map key] {
			get {
				if(object.ReferenceEquals(key,CodeKeys.Function) || key.Equals(CodeKeys.Function)) {
					return value;
				}
				else {
					return null;
				}
			}
			set {
				if(key.Equals(CodeKeys.Function)) {
					this.value=value;
				}
				else {
					throw new Exception("The method or operation is not implemented.");
				}
			}
		}
		public override IEnumerable<Map> Keys {
			get { 
				yield return CodeKeys.Function;
			}
		}
		public override bool IsNormal {
			get { 
				return true;
			}
		}
		public override string GetString() {
			return null;
		}
		public override void Append(Map map) {
			throw new Exception("The method or operation is not implemented.");
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
		private Map value;
		public FunctionMap(Map value) {
			this.value=value;
		}
		public override bool ContainsKey(Map k) {
			return k.Equals(CodeKeys.Function);
		}
		public override Number GetNumber() {
			return null;
		}
		public override int Count {
			get { 
				return 1;
			}
		}
	}
	public class Program : ScopeExpression {
		public override Map GetStructure() {
			if (statementList.Count == 0) {
				return new DictionaryMap();
			}
			else {
				return statementList[statementList.Count - 1].Current();
			}
		}
		public override Compiled GetCompiled(Expression parent) {
			if(statementList.Count==1) {
				KeyStatement statement=statementList[0] as KeyStatement;
				if(statement!=null) {
					Map key=statement.key.GetConstant();
					Map value=statement.value.GetConstant();
					CompiledStatement compiled=statement.Compile();
					if(key!=null && value!=null && statement.value is Literal && key.Equals(CodeKeys.Function)) {
						return delegate(Map context) {
							Map map=new FunctionMap(value);
							map.Scope=context;
							return map;
						};
					}
				}
			}
			List<CompiledStatement> list=statementList.ConvertAll<CompiledStatement>(delegate(Statement s) {
				return s.Compile();});

			return delegate(Map p) {
				Map context = new DictionaryMap();
				context.Scope = p;
				foreach (CompiledStatement statement in list) {
					statement.Assign(ref context);
					//if (Interpreter.stopping) {

					//}
				}
				return context;
			};
		}
		public List<Statement> statementList= new List<Statement>();
		public Program(Extent source,Expression parent):base(source,parent) {
		}
		public Program(Map code, Expression parent) : base(code.Source, parent) {
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
			foreach (Source breakpoint in Interpreter.breakpoints) {
				if (start.FileName == breakpoint.FileName) {
					if (start<=breakpoint) {
						if (end >= breakpoint) {
							bool x = end >= breakpoint;
							if (Interpreter.Breakpoint != null) {
								Interpreter.Breakpoint(context);
							}
						}
					}
				}
			}
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
		public Map PreMap() {
			Map s = Pre();
			return s;
		}
		public IEnumerable<Map> PreKeys() {
			if(PreMap()!=null) {
				return PreMap().Keys;
			}
			else {
				return new List<Map>();
			}
		}
		public virtual Map Pre() {
			if (!preEvaluated) {
				if (Previous == null) {
					pre = new DictionaryMap();
				}
				else {
					pre = Previous.Current();
				}
			}
			preEvaluated = true;
			return pre;
		}
		public Map CurrentMap() {
			Map s = Current();
			return s;
		}
		public Map Current() {
			if (!currentEvaluated) {
				Map pre = Pre();
				if (pre != null) {
					current = CurrentImplementation(pre);}
				else {
					current = null;
				}
			}
			currentEvaluated = true;
			return current;
		}
		public virtual IEnumerable<Map> CurrentKeys() {
			Map m=CurrentMap();
			if(m!=null) {
				return m.Keys;
			}
			else {
				return null;
			}
		}
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
		public virtual bool DoesNotAddKey(Map key) {
			return true;
		}
		public bool NeverAddsKey(Map key) {
			Statement current = this;
			while (true) {
				current = current.Next;
				if (current == null || current is CurrentStatement) {
					break;
				}
				if (!current.DoesNotAddKey(key)) {
					return false;
				}
			}
			return true;
		}
		protected abstract Map CurrentImplementation(Map previous);
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
		public readonly Expression value;
		public readonly int Index;
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
		protected override Map CurrentImplementation(Map previous) {
			return previous;
		}
		public DiscardStatement(Expression discard, Expression value, Program program, int index): base(program, value, index) {}
		public override CompiledStatement Compile() {
			return new CompiledStatement(value.Source.Start,value.Source.End,value.Compile(), delegate { });
		}
	}
	public class KeyStatement : Statement {
		public override IEnumerable<Map> CurrentKeys() {
			if(this.key.GetConstant()!=null && PreKeys()!=null)  {
				List<Map> list=new List<Map>(PreKeys());
				list.Add(key.GetConstant());
				return list;
			}
			return null;
		}
		public static bool intellisense = false;
		public override bool DoesNotAddKey(Map key) {
			Map k = this.key.EvaluateStructure();
			if (k != null && k.IsConstant && !k.Equals(key)) {
				return true;
			}
			return false;
		}
		protected override Map CurrentImplementation(Map previous) {
			Map k=key.GetConstant();
			if (k != null) {
				Map val=value.EvaluateMapStructure();
				if (val == null) {
				    val = new DictionaryMap();
					val.IsConstant=false;
				}
				if (value is Search || value is Call || (intellisense && (value is Literal || value is Program))) {
					previous[k] = val;
				}
				else {
					Map m=new DictionaryMap();
					m.IsConstant=false;
					previous[k] = m;
				}
				return previous;
			}
			return null;
		}
		public override CompiledStatement Compile() {
			Map k = key.GetConstant();
			if (k != null && k.Equals(CodeKeys.Function)) {
				if (value is Literal) {
					if(program.statementList.Count == 1) {
						((Literal)value).literal.Compile(program);
					}
				}
			}
			Compiled s=key.Compile();
			return new CompiledStatement(key.Source.Start,value.Source.End,value.Compile(), delegate(ref Map context, Map v) {
				context[s(context)] = v;
			});
		}
		public Expression key;
		public KeyStatement(Expression key, Expression value, Program program, int index)
			: base(program, value, index) {
			this.key = key;
			key.Statement = this;
		}
	}
	public class CurrentStatement : Statement {
		protected override Map CurrentImplementation(Map previous) {
			return value.EvaluateStructure();
		}
		public override CompiledStatement Compile() {
			return new CompiledStatement(value.Source.Start,value.Source.End,value.Compile(), delegate(ref Map context, Map v) {
				if (this.Index == 0) {
					if(!(v is DictionaryMap))  {
						context = v.Copy();
					}
					else {
						((DictionaryMap)context).dictionary = ((DictionaryMap)v).dictionary;
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
		protected override Map CurrentImplementation(Map previous) {
			return previous;
		}
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
	public class Literal : Expression {
		public override Map GetStructure() {
			return literal;
		}
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		public Map literal;
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				if (literal != null) {
					literal.Source = Source;
				}
				return literal;
			};
		}
		public Literal(Map code, Expression parent): base(code.Source, parent) {
			this.literal = code;
		}
	}
	public class Root : Expression {
		public override Map GetStructure() {
			return Gac.gac;
		}
		public Root(Map code, Expression parent): base(code.Source, parent) {
		}
		public override Compiled GetCompiled(Expression parent) {
			return delegate {
				return Gac.gac;
			};
		}
	}
	public class Select : Expression {
		public override Map GetStructure() {
			// maybe wrong
			Map selected = subs[0].GetStructure();
			for (int i = 1; i < subs.Count; i++) {
				Map key = subs[i].GetConstant();
				if (key != null && key.Equals(new StringMap("get_Parent"))) {
					subs[0].GetStructure();
					Console.WriteLine("hello");
				}
				if (selected == null || key == null || !selected.ContainsKey(key)) {
					return null;
				}
				selected = selected[key];
			}
			return selected;
		}
		public override Compiled GetCompiled(Expression parent) {
			List<Compiled> s=subs.ConvertAll<Compiled>(delegate(Expression e) {return e.Compile();});
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
		private List<Expression> subs = new List<Expression>();
		public Select(Map code, Expression parent): base(code.Source, parent) {
			foreach (Map m in code.Array) {
				subs.Add(m.GetExpression(this));
			}
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
			Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
			Map callable = Parser.Parse(path);
			callable.Scope = Gac.gac["library"];
			LiteralExpression gac = new LiteralExpression(Gac.gac, null);
			LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
			lib.Statement = new LiteralStatement(gac);
			callable[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
			callable[CodeKeys.Function].Compile(lib);
			Gac.gac.Scope = new DirectoryMap(Path.GetDirectoryName(path));
			return callable.Call(argument);
		}
		public static bool profiling = false;
		static Interpreter() {
			Map map = Parser.Parse(LibraryPath);
			map.Scope = Gac.gac;
			LiteralExpression gac = new LiteralExpression(Gac.gac, null);
			map[CodeKeys.Function].GetExpression(gac).Statement = new LiteralStatement(gac);
			map[CodeKeys.Function].Compile(gac);
			Gac.gac["library"] = map.Call(new DictionaryMap());
			Gac.gac["library"].Scope = Gac.gac;
		}
		[STAThread]
		public static void Main(string[] args) {
			string multilineliteral = @"part of the text
                                       other part of the text";

			StringMap s = new StringMap("hellh");
			ListMap l = new ListMap(new List<Map>(Array.ConvertAll<char,Map>("hellh".ToCharArray(),delegate(char c) {return c;})));
			l.GetHashCode();
			s.Equals(l);
			DateTime start = DateTime.Now;
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
						Console.WriteLine((DateTime.Now - start).TotalSeconds);
						return;
					}
					else {
						Console.WriteLine("File " + fileName + " not found.");
					}
				}
			}
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
				Console.SetBufferSize(80, 1000);
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
		public static Dictionary<int,Type> types=new Dictionary<int,Type>();
		public static Delegate CreateDelegateFromCode(Map code, int typeToken) {
			Type type=types[typeToken];
			return CreateDelegateFromCode(code,type);
		}
		public static Delegate CreateDelegateFromCode(Map code, Type delegateType) {
			MethodInfo invoke = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters) {
				arguments.Add(parameter.ParameterType);
			}
			DynamicMethod method = new DynamicMethod("EventHandler",
				invoke.ReturnType,
				arguments.ToArray(),
				typeof(Map).Module);
			ILGenerator il = method.GetILGenerator();
			LocalBuilder local = il.DeclareLocal(typeof(object[]));
			il.Emit(OpCodes.Ldc_I4, parameters.Length);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, local);
			for (int i = 0; i < parameters.Length; i++) {
				il.Emit(OpCodes.Ldloc, local);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldarg, i + 1);
				il.Emit(OpCodes.Stelem_Ref);
			}
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Call, typeof(MetaDelegate).GetMethod("Call"));
			if (invoke.ReturnType == typeof(void)) {
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else {
				il.Emit(OpCodes.Castclass, invoke.ReturnType);
				il.Emit(OpCodes.Ret);
			}
			return (Delegate)method.CreateDelegate(delegateType, new MetaDelegate(code, invoke.ReturnType));
		}
		public class MetaDelegate {
			private Map callable;
			private Type returnType;
			public MetaDelegate(Map callable, Type returnType) {
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments) {
				Map pos = this.callable;
				foreach (object argument in arguments) {
					pos = pos.Call(Transform.ToMeta(argument));}
				if (returnType != typeof(void)) {
					return Meta.Transform.ToDotNet(pos, this.returnType);
				}
				else {
					return null;
				}
			}
		}
		public static void GetMetaConversion(Type type,ILGenerator il) {
			if(!type.IsSubclassOf(typeof(Map)) && !type.Equals(typeof(Map))) {
				if(type.Equals(typeof(Boolean))) {
					il.Emit(OpCodes.Call,typeof(Convert).GetMethod("ToInt32",new Type[] {typeof(Boolean)}));
					il.Emit(OpCodes.Newobj,typeof(Integer32).GetConstructor(new Type[] {typeof(int)}));
				}
				else if(type.Equals(typeof(void))) {
					il.Emit(OpCodes.Ldsfld,typeof(Map).GetField("Empty"));
				}
				else if(type.Equals(typeof(string))) {
					il.Emit(OpCodes.Newobj,typeof(StringMap).GetConstructor(new Type[] {typeof(string)}));
				}
				else {
					switch (Type.GetTypeCode(type)) {
						case TypeCode.Int32:
							il.Emit(OpCodes.Newobj,typeof(Integer32).GetConstructor(new Type[] {typeof(int)}));
							break;
						default:
							if(type.IsValueType) {
								il.Emit(OpCodes.Box,type);
							}
							il.Emit(OpCodes.Newobj,typeof(ObjectMap).GetConstructor(new Type[] {typeof(object)}));
							break;
					}
				}
			}
		}
		public static object MakeEnum(int value,int type) {
			object x=Enum.ToObject(types[type], value);
			return x;
		}
		public static void GetConversion(Type target,ILGenerator il) {
			if (target.IsEnum) {
				int token=(int)target.TypeHandle.Value;
				if(!Transform.types.ContainsKey(token)) {
				    Transform.types[token]=target;
				}
				il.Emit(OpCodes.Callvirt,typeof(Map).GetMethod("GetInt32"));
			}
			else if(target.Equals(typeof(double))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetNumber"));
				il.Emit(OpCodes.Callvirt, typeof(Number).GetMethod("GetDouble"));
			}
		    else if(target.Equals(typeof(Number))) {
				il.Emit(OpCodes.Callvirt,typeof(Map).GetMethod("GetNumber"));
		    }
			else if (target.Equals(typeof(Stream))) {
				il.Emit(OpCodes.Castclass, typeof(FileMap));
				il.Emit(OpCodes.Callvirt, typeof(FileMap).GetMethod("GetStream"));
			}
			else if (target.Equals(typeof(Map))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetObject"));
			}
			else if (target.Equals(typeof(Boolean))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetInt32"));
				il.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToBoolean", new Type[] { typeof(int) }));
			}
			else if (target.Equals(typeof(String))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetString", BindingFlags.Instance | BindingFlags.Public));
			}
			else if (target.Equals(typeof(int))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetNumber"));
				il.Emit(OpCodes.Callvirt, typeof(Number).GetMethod("GetInt32"));
			}
			else if (target.Equals(typeof(Single))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetNumber"));
				il.Emit(OpCodes.Callvirt, typeof(Number).GetMethod("GetSingle"));
			}
			else if (target.Equals(typeof(object))) {
			}
			else if (target.Equals(typeof(Type))) {
				il.Emit(OpCodes.Callvirt, typeof(Map).GetMethod("GetClass"));
			}
			else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))) {
				int token = (int)target.TypeHandle.Value;
				if (!Transform.types.ContainsKey(token)) {
					Transform.types[token] = target;
				}
				il.Emit(OpCodes.Ldc_I4, (int)token);
				il.Emit(OpCodes.Call, typeof(Transform).GetMethod("CreateDelegateFromCode", new Type[] { typeof(Map), typeof(int) }));
			}
			else {
				il.Emit(OpCodes.Castclass, typeof(ObjectMap));
				il.Emit(OpCodes.Callvirt, typeof(ObjectMap).GetMethod("get_Object"));
				il.Emit(OpCodes.Castclass, target);
			}
		}
		public static object ToDotNet(Map meta,Type target) {
			if(target.Equals(typeof(Map))) {
				return meta;
			}
			else if (target.Equals(typeof(Stream)) && meta is FileMap) {
				return File.Open(((FileMap)meta).Path,FileMode.Open);
			}
			else if (target.Equals(typeof(object)) && meta is ObjectMap) {
				return ((ObjectMap)meta).Object;
			}
			else {
				Type type = meta.GetType();
				if (type.IsSubclassOf(target)) {
					return meta;
				}
				else {
					TypeCode typeCode = Type.GetTypeCode(target);
					if (typeCode == TypeCode.Object) {

						if (target == typeof(Number) && meta.IsNumber) {
							return meta.GetNumber();
						}
						if (target == typeof(System.Drawing.Point) && meta.IsNormal) {
							return new System.Drawing.Point(meta[1].GetInt32(), meta[2].GetInt32());
						}
						if (target == typeof(System.Windows.Point) && meta.IsNormal) {
							return new System.Windows.Point(meta[1].GetInt32(), meta[2].GetInt32());
						}
						if (target == typeof(Rectangle) && meta.IsNormal) {
							int x = meta[1][1].GetInt32();
							int y = meta[1][2].GetInt32();
							return new Rectangle(x,
								y,
								meta[2][1].GetInt32() - x,
								meta[2][2].GetInt32() - y);
						}
						if (target == typeof(Color) && meta.IsNormal) {
							return Color.FromArgb(meta[1].GetInt32(), meta[2].GetInt32(), meta[3].GetInt32());
						}
						if (target == typeof(Type) && meta is TypeMap) {
							return ((TypeMap)meta).Type;
						}
						// remove?
						else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).Type)) {
							return ((ObjectMap)meta).Object;
						}
						else if (target.IsAssignableFrom(type)) {
							return meta;
						}
						else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
						   && meta.ContainsKey(CodeKeys.Function)) {
							return CreateDelegateFromCode(meta, target);
						}
						if (target.IsArray) {
							List<object> list = new List<object>();
							foreach (Map map in meta.Array) {
								list.Add(ToDotNet(map, target.GetElementType()));
							}
							Array array = Array.CreateInstance(target.GetElementType(), meta.ArrayCount);
							list.ToArray().CopyTo(array, 0);
							return array;
						}
					}
					else if (target.IsEnum) {
						return Enum.ToObject(target, meta.GetNumber().GetInt32());
					}
					else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).Type)) {
						return ((ObjectMap)meta).Object;
					}
					else {
						switch (typeCode) {
							case TypeCode.Boolean:
								return Convert.ToBoolean(meta.GetNumber().GetInt32());
							case TypeCode.Byte:
								return Convert.ToByte(meta.GetNumber().GetInt32());
							case TypeCode.Char:
								return Convert.ToChar(meta.GetNumber().GetInt32());
							case TypeCode.DateTime:
								return null;
							case TypeCode.DBNull:
								return null;
							case TypeCode.Decimal:
								return (decimal)(meta.GetNumber().GetInt64());
							case TypeCode.Double:
								return (double)(meta.GetNumber().GetDouble());
								//return (double)(meta.GetNumber().GetInt64());
							case TypeCode.Int16:
								return Convert.ToInt16(meta.GetNumber().GetRealInt64());
							case TypeCode.Int32:
								return meta.GetNumber().GetInt32();
							case TypeCode.Int64:
								return Convert.ToInt64(meta.GetNumber().GetInt64());
							case TypeCode.SByte:
								return Convert.ToSByte(meta.GetNumber().GetInt64());
							case TypeCode.Single:
								return (float)meta.GetNumber().GetInt64();
							case TypeCode.String:
								return meta.GetString();
							case TypeCode.UInt16:
								return Convert.ToUInt16(meta.GetNumber().GetInt64());
							case TypeCode.UInt32:
								return Convert.ToUInt32(meta.GetNumber().GetInt64());
							case TypeCode.UInt64:
								return Convert.ToUInt64(meta.GetNumber().GetInt64());
							default:
								throw new ApplicationException("not implemented");
						}
					}
				}
			}
			throw new ApplicationException("Cannot convert " + Serialization.Serialize(meta) + " to " + target.ToString() + ".");
		}
		public static Map ToMeta(object dotNet) {
			if (dotNet == null) {
				return Map.Empty;
			}
			else {
				Type type = dotNet.GetType();
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Boolean:
						return new Integer32(Convert.ToInt32((Boolean)dotNet));
					case TypeCode.Byte:
						return (Byte)dotNet;
					case TypeCode.Char:
						return (Char)dotNet;
					case TypeCode.SByte:
						return (SByte)dotNet;
					case TypeCode.Single:
						return (Single)dotNet;
					case TypeCode.UInt16:
						return (UInt16)dotNet;
					case TypeCode.UInt32:
						return (UInt32)dotNet;
					case TypeCode.UInt64:
						return (UInt64)dotNet;
					case TypeCode.String:
						return (String)dotNet;
					case TypeCode.Decimal:
						return new Rational((double)(Decimal)dotNet);
					case TypeCode.Double:
						return (Double)dotNet;
					case TypeCode.Int16:
						return (Int16)dotNet;
					case TypeCode.Int32:
						return (Int32)dotNet;
					case TypeCode.Int64:
						return (Int64)dotNet;
					case TypeCode.DateTime:
						return new ObjectMap(dotNet);
					case TypeCode.DBNull:
						return new ObjectMap(dotNet);
					case TypeCode.Object:
						if(dotNet is Number) {
							return (Number)dotNet;
						}
						else if (dotNet is Map) {
							return (Map)dotNet;
						}
						else {
							return new ObjectMap(dotNet);
						}
					default:
						throw new ApplicationException("Cannot convert object.");
				}
			}
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
		public override bool IsNormal {
			get {
				return false;
			}
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
		}
		public ParameterInfo[] parameters;
		public override Map Call(Map argument) {
		    return DecideCall(argument, new List<object>());
		}
		private Map DecideCall(Map argument, List<object> oldArguments) {
			List<object> arguments = new List<object>(oldArguments);
			if (parameters.Length != 0) {
				arguments.Add(Transform.ToDotNet(argument, parameters[arguments.Count].ParameterType));
			}
			if (arguments.Count >= parameters.Length) {
				return Invoke(argument, arguments.ToArray());
			}
			else {
				CallDelegate call = new CallDelegate(delegate(Map map) {
					return DecideCall(map, arguments);});
				return new Method(invokeMethod, call, typeof(CallDelegate),call.Method);
			}
		}
		MethodInfo invokeMethod = typeof(CallDelegate).GetMethod("Invoke");
		private Map Invoke(Map argument, object[] arguments) {
			object result;
			if (method is ConstructorInfo) {
				result = ((ConstructorInfo)method).Invoke(arguments);
			}
			else {
				result = method.Invoke(obj, arguments);
			}
			return Transform.ToMeta(result);
		}
	}
	public class TypeMap : DotNetMap {
		public override Type GetClass() {
			return this.Type;
		}
		private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
		protected override BindingFlags BindingFlags {
			get {
				return bindingFlags;
			}
		}
		public static MemberCache cache = new MemberCache(bindingFlags);
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
					if (method == null) {
						return null;
					}
					constructor = new Method(method, Object, Type,method);
				}
				return constructor;
			}
		}
		public override Map Call(Map argument) {
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
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
		protected override BindingFlags BindingFlags {
			get {
				return bindingFlags;
			}
		}
		public static MemberCache cache = new MemberCache(bindingFlags);
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
		public ObjectMap(object target, Type type): base(target, type) 
		{
		}
		public ObjectMap(object target): base(target, target.GetType()) 
		{
		}
		public override string ToString() {
			return Object.ToString();
		}
		public override Map Copy() {
			return this;
		}
	}
	public class DirectoryMap : Map {
		public override int Count {
			get {
				return new List<Map>(Keys).Count;
			}
		}
		public override Map this[Map key] {
			get {
				//if (key.Equals(new StringMap("OdeDotNet"))) {
				//}
				if (ContainsKey(key)) {
					string p=Path.Combine(path,key.GetString());
					if (key.Equals(new StringMap("GuiExample"))) {
					}
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

		public override IEnumerable<Map> Keys {
			get {
				foreach (string dll in Directory.GetFiles(path,"*.dll")) {
					Assembly assembly=null;
					try {
						assembly=Assembly.LoadFrom(dll);
					}
					catch(Exception e) {
					};
					if (assembly != null) {
						yield return Path.GetFileNameWithoutExtension(dll);
					}
				}
				foreach (string file in Directory.GetFiles(path)) {
					yield return Path.GetFileName(file);
				}
				foreach (string directory in Directory.GetDirectories(path)) {
					yield return new DirectoryInfo(directory).Name;
				}
			}
		}
		public override IEnumerable<Map> Array {
			get {
				yield break;
			}
		}
		private string path;
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
	public class FileMap : Map {
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
		private string path;
		public FileMap(string path) {
			this.path = path;
		}
	}
	public class DictionaryMap : Map {
		private Expression expression;
		public override Expression Expression {
			get {
				return expression;
			}
			set {
				expression=value;
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
		public override bool IsConstant {
			get {
				return isConstant;
			}
			set {
				isConstant = value;
			}
		}
		private bool isConstant = true;

		public override bool IsNormal {
			get {
				return true;
			}
		}
		public override int GetHashCode() {
			if (IsNumber) {
				return (int)(GetNumber().Numerator % int.MaxValue);
			}
			else {
				return Count;
			}
		}
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
		public DictionaryMap(params Map[] keysAndValues){
		    for (int i = 0; i <= keysAndValues.Length - 2; i += 2) {
		        this[keysAndValues[i]] = keysAndValues[i + 1];
		    }
		}
		public DictionaryMap(System.Collections.Generic.ICollection<Map> list) {
			int index = 1;
			foreach (object entry in list) {
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}
		public DictionaryMap(IEnumerable<Map> list) {
			foreach(Map map in list) {
				this.Append(map);
			}
		}
		public override IEnumerable<Map> Array {
			get {
				for (int i = 1;; i++) {
					Map m=this[i];
					if(m!=null) {
						yield return m;
					}
					else {
						yield break;
					}
				}
			}
		}
		public override Map Copy() {
			return DeepCopy();
		}
		public override void Append(Map map) {
			this[ArrayCount + 1]=map;
		}

		public override Number GetNumber() {
			if (Count == 0) {
				return 0;
			}
			else if (this.Count == 1) {
				if(this.ContainsKey(Map.Empty)) {
					if(this[Map.Empty].IsNumber) {
						return this[Map.Empty].GetNumber()+1;
					}
				}
			}
			return null;
		}
		public override string Serialize() {
			if (this.Count == 0) {
				return "0";
			}
			else if (this.IsString) {
				return "\"" + this.GetString() + "\"";
			}
			else if (this.IsNumber) {
				return this.GetNumber().ToString();
			}
			return null;
		}
		public override int ArrayCount {
			get {
				int i = 1;
				while (this.ContainsKey(i)) {
					i++;}
				return i - 1;
			}
		}
		public Dictionary<Map, Map> dictionary=new Dictionary<Map, Map>();
		public override Map this[Map key] {
			get {
				Map val;
				dictionary.TryGetValue(key, out val);
				return val;
			}
			set {
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
	public abstract class DotNetMap : Map {
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
		public override bool IsNormal {
			get {
				return false;
			}
		}
		public override int GetHashCode() {
			if (obj != null) {
				return obj.GetHashCode();
			}
			else {
				return type.GetHashCode();
			}
		}
		public override bool Equals(object obj) {
			DotNetMap dotNet = obj as DotNetMap;
			if (dotNet != null) {
				return dotNet.Object == Object && dotNet.Type == Type;
			}
			return false;
		}
		protected abstract BindingFlags BindingFlags {
			get;
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
						global[GlobalKey] = new Dictionary<Map, Map>();}
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
		public Delegate CreateEventDelegate(string name, Map code) {
			return Transform.CreateDelegateFromCode(code, type.GetEvent(name, BindingFlags).EventHandlerType);
		}
	}
	public interface ISerializeEnumerableSpecial {
		string Serialize();
	}
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
					File.Create(checkPath).Close();}
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
				return success;}
			public abstract object GetResult(out int level);
		}
		public void Run() {
			bool allTestsSucessful = true;
			foreach (Type testType in this.GetType().GetNestedTypes()) {
				if (testType.IsSubclassOf(typeof(Test))) {
					Test test = (Test)testType.GetConstructor(new Type[] {}).Invoke(null);
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
	public class Extent {
		public readonly Source Start;
		public readonly Source End;
		public Extent(Source start, Source end) {
			this.Start = start;
			this.End = end;
		}
		public override int GetHashCode() {
			return Start.GetHashCode() * End.GetHashCode();}
		public override bool Equals(object obj) {
			Extent extent = obj as Extent;
			return extent != null && Start.Equals(extent.Start) && End.Equals(extent.End);
		}
	}
	public class Source {
		public static bool operator <(Source a,Source b) {
			return a.Line < b.Line || (a.Line == b.Line && a.Column < b.Column);
		}
		public static bool operator >(Source a, Source b) {
			return a.Line > b.Line || (a.Line == b.Line && a.Column > b.Column);
		}
		public static bool operator <=(Source a, Source b) {
			return a < b || a == b;
		}
		public static bool operator >=(Source a, Source b) {
			return a > b || a == b;
		}
		public static bool operator ==(Source a, Source b) {
			return Object.ReferenceEquals(a,b) || a.Equals(b);
		}
		public static bool operator !=(Source a, Source b) {
			return !(a == b);
		}

		public override string ToString() {
			return FileName + ", " + "line " + Line + ", column " + Column;}
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
	public abstract class SpecialMap:Map {
		public override Number GetNumber() {
			return null;
		}
		public override string GetString() {
			return null;
		}
		public override IEnumerable<Map> Array {
			get { 
				yield break;
			}
		}
		public override bool IsNormal {
			get {
				return false;
			}
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override Map Copy() {
			return this;
		}
		
		public override bool ContainsKey(Map key) {
			return this[key] != null;
		}
		public override IEnumerable<Map> Keys {
			get {
				throw new Exception("The method or operation is not implemented.");
			}
		}
	}
	public class AssemblyMap : Map {
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
			return LoadAssembly(assembly);
		}
		private Map data;
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
		public AssemblyMap(Assembly assembly) {
			this.assembly = assembly;
		}
	}
	public class Gac : Map {
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
			cache["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
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
		public override Map this[Map key] {
			get {
				Map value;
				if (!cache.ContainsKey(key)) {
					if (key.IsString) {
						Assembly assembly;
						string path = Path.Combine(Interpreter.InstallationPath, key.GetString() + ".dll");
						if (File.Exists(path)) {
							assembly = Assembly.LoadFile(path);
						}
						else {
							try {
								assembly = Assembly.LoadWithPartialName(key.GetString());
							}
							catch (Exception e) {
								return null;
							}
						}
						if (assembly != null) {
							value = new AssemblyMap(assembly);
							cache[key] = value;
						}
						else {
							value = null;
						}
					}
					else {
						value = null;
					}
				}
				else {
					value = cache[key];
				}
				return value;
			}
			set {
				cache[key] = value;
			}
		}
	}
	public class StringMap : Map {
		public override bool IsNormal {
			get {
				return true;
			}
		}
		public override Number GetNumber() {
			return null;
		}
		public override string Serialize() {
			return "\""+text+"\"";
		}
		private string text;
		public StringMap(string text) {
			this.text = text;
		}
		public override int GetHashCode() {
		    return text.Length;
		}
		public override Map this[Map key] {
			get {
				if (key.IsNumber) {
					Number number = key.GetNumber();
					if (number.GetInteger()!=null && number>0 && number<=Count) {
						return text[number.GetInt32() - 1];
					}
					else {
						return null;
					}
				}
				else {
					return null;
				}
			}
			set {
				int index=key.GetNumber().GetInt32()-1;
				char c=Convert.ToChar(value.GetNumber().GetInt32());
				text.Remove(index,1).Insert(index,c.ToString());
			}
		}
		public override bool Equals(object obj) {
			if (obj is StringMap) {
				return ((StringMap)obj).text == text;}
			else {
				return base.Equals(obj);
			}
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
			if (key.IsNumber) {
				Number number = key.GetNumber();
				if (number==1) {
					return number > 0 && number <= text.Length;}
				else {
					return false;
				}
			}
			else {
				return false;
			}
		}
		public override int ArrayCount {
			get {
				return text.Length;
			}
		}
		public override Map Copy() {
			if (Equals(new StringMap("apply"))) {
			}
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
	public abstract class Number:Map {
		public static Number operator +(Number a,int b) {
			return a.Add(new Integer32(b));
		}
		public static bool operator ==(Number a,int b) {
			return a.Equals(new Integer32(b));
		}
		public static bool operator !=(Number a,int b) {
			return !(a==b);
		}
		public static bool operator ==(Number a,Number b) {
			return ReferenceEquals(a,b) || a.Equals(b);
		}
		public static bool operator !=(Number a,Number b) {
			return !(a==b);
		}
		public override string GetString() {
			return null;
		}
		public static implicit operator Number(double number) {
			return new Rational(number);
		}
		public static implicit operator Number(decimal number) {
			return new Rational((double)number);
		}
		public static implicit operator Number(int integer) {
			return new Integer32(integer);
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
			get {return 0;}
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
					if (key.Count==0) {
						return this-1;
					}
					else if (key.Equals(NumberKeys.Negative)) {
						return Map.Empty;
					}
					else if (key.Equals(NumberKeys.Denominator)) {
						return new Rational(Denominator);
					}
					else {
						throw new ApplicationException("Error.");
					}
				}
				else {
					return null;
				}
			}
			set 
			{
			}
		}
		public override IEnumerable<Map> Keys {
			get {
				if (this!=0) {
					yield return Map.Empty;
				}
				if (this<0) {
					yield return NumberKeys.Negative;
				}
				if (Denominator != 1.0d) {
					yield return NumberKeys.Denominator;
				}
			}
		}
		public override Number GetNumber() {
			return this;
		}
		public float GetSingle() {
			return (float)GetDouble();
		}
		public override string ToString() {
			if (Denominator == 1) {
				return Numerator.ToString();
			}
			else {
				return Numerator.ToString() + Syntax.fraction + Denominator.ToString();
			}
		}
		public override bool Equals(object obj) {
			Map map = obj as Map;
			if(map!=null && map.IsNumber) {
				Number b=map.GetNumber();
				return b!=null && b.Numerator == Numerator && b.Denominator == Denominator;
			}
			else {
				return false;
			}
		}
		public override int GetHashCode() {
			return (int)(Numerator % int.MaxValue);
		}
		public abstract double Numerator {
			get;
		}
		public abstract double Denominator {
			get;
		}
		public abstract long GetInt64();
		public abstract long GetRealInt64();

		public static double GreatestCommonDivisor(double a, double b) {
			if(a==b) {
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
		public static double LeastCommonMultiple(Number a, Number b) {
			return a.Denominator * b.Denominator / GreatestCommonDivisor(a.Denominator, b.Denominator);
		}
		public virtual Number Subtract(Number b) {
			return new Rational(Expand(b) - b.Expand(this), LeastCommonMultiple(this, b));
		}
		public virtual bool LessThan(Number b) {
			return Expand(b) < b.Expand(this);
		}
		public virtual bool LessThan(int b) {
		    return LessThan(new Integer32(b));
		}
		public virtual Number Add(Number b) {
			 return new Rational(Expand(b) + b.Expand(this), LeastCommonMultiple(this, b));
		}

		public double Expand(Number b) {
			return Numerator * (LeastCommonMultiple(this, b) / Denominator);
		}
		public static Number operator +(Number a, Number b) {
		    return a.Add(b);
		}
		public static Number operator -(Number a, Number b) {
			return a.Subtract(b);
		}
		public static Number operator /(Number a, Number b) {
			return new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
		}
		public static Number operator *(Number a, Number b) {
			return new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
		}
		public static bool operator >(Number a, Number b) {
			return a.Expand(b) > b.Expand(a);
		}
		public static bool operator <(Number a, Number b) {
			return a.LessThan(b);
		}
		public static bool operator >=(Number a, Number b) {
			return a.Expand(b) >= b.Expand(a);
		}
		public static bool operator <=(Number a, Number b) {
			return a.Expand(b) <= b.Expand(a);
		}
		public static Number operator |(Number a, Number b) {
			return Convert.ToInt32(a.Numerator) | Convert.ToInt32(b.Numerator);
		}
		public static Number operator %(Number a, Number b) {
			return Convert.ToInt32(a.Numerator) % Convert.ToInt32(b.Numerator);
		}
		public int CompareTo(Number number) {
			return GetDouble().CompareTo(number.GetDouble());
		}
		public abstract double GetDouble();
	}
	public abstract class IntegerBase:Number {
	}
	public class Integer:IntegerBase {
		public override double Denominator {
			get {
				return 1.0d;
			}
		}
		public override double Numerator {
			get { 
				return integer.doubleValue();
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
		public Integer(int i) {
			this.integer=new BigInteger(i.ToString());
		}
		public Integer(string text) {
			this.integer=new BigInteger(text);
		}
		public override int GetInt32() {
			return integer.intValue();
		}
	}
	public class Integer32:IntegerBase{
		public override Integer32 GetInteger() {
			return this;
		}
	    private int integer;
	    public Integer32(int integer) {
	        this.integer=integer;
	    }
	    public override double GetDouble() {
	        return integer;
	    }
		public override Number Subtract(Number b) {
		    Integer32 i=b.GetInteger();
		    if(i!=null) {
		        try {
		            return integer-i.integer;
		        }
				catch(OverflowException) {}
		    }
		    return base.Subtract(b);
		}
	    public override bool LessThan(Number b) {
			Integer32 i=b.GetInteger();
	        if(i!=null) {
                try {
                    return integer<i.integer;
                }
                catch(OverflowException) {}
	        }
	        return base.LessThan(b);
	    }
		public override Number Add(Number b) {
		    Integer32 i=b.GetInteger();
		    if(i!=null) {
		        try {
		            return integer+i.integer;
		        }
		        catch(OverflowException) {}
		    }
		    return base.Add(b);
		}
	    public override double Denominator {
	        get {
	            return 1;
	        }
	    }
	    public override int GetInt32() {
	        return integer;
	    }
	    public override long GetInt64() {
	        return integer;
	    }
	    public override long GetRealInt64() {
	        return integer;
	    }
	    public override double Numerator {
	        get {
	            return integer;
	        }
	    }
	}
	public class Rational:Number {
		public override Integer32 GetInteger() {
			if(Denominator==1.0d && Numerator<int.MaxValue && Numerator>int.MinValue) {
				return new Integer32(GetInt32());
			}
			return null;
		}
		private readonly Integer numerator;
		private readonly Integer denominator;
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
		public Rational(Number i) : this(i.Numerator, i.Denominator) {}
		public Rational(double numerator, double denominator) {
			double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
			if (denominator < 0) {
				numerator = -numerator;
				denominator = -denominator;
			}
			this.numerator = new Integer(Convert.ToInt64((numerator / greatestCommonDivisor)).ToString());
			this.denominator = new Integer(Convert.ToInt64(denominator / greatestCommonDivisor).ToString());
		}
		public override double Numerator {
			get {
				return numerator.GetDouble();
			}
		}
		public override double Denominator {
			get {
				return denominator.GetDouble();
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
	public class Parser {
		static Parser() {
			Console.WriteLine("test");
		}
		public static List<Dictionary<State, CachedResult>> allCached = new List<Dictionary<State, CachedResult>>();
		public struct State {
			public Error[] Errors;
			public override bool Equals(object obj) {
				State state = (State)obj;
				return state.Column == Column && state.index == index && state.Line == Line &&
					state.FileName == FileName;
			}
			public override int GetHashCode() {
				unchecked {
					return index.GetHashCode() * Line.GetHashCode() * Column.GetHashCode() * FileName.GetHashCode();
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
		}
		public Stack<int> defaultKeys = new Stack<int>();
		public State state;
		public Parser(string text, string filePath) {
			state = new State(filePath, text+Syntax.endOfFile);
			Root.precondition=delegate(Parser p) {
				return p.Look()==Syntax.root;
			};
			Program.precondition=delegate(Parser p) {
				return p.Look()==Syntax.programStart;
			};
			LiteralExpression.precondition=delegate(Parser p) {
				switch(p.Look()){
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
			};
		}
		public static Rule Comment = DelayedRule(delegate {
			return Sequence(Syntax.comment,Syntax.comment,StringRule(ZeroOrMoreChars(CharsExcept(Syntax.windowsNewLine))), EndOfLine);
		});

		public static Rule Empty = ZeroOrMore(
			Alternatives(
				Comment,
				Syntax.unixNewLine, Syntax.windowsNewLine[0], Syntax.tab, Syntax.space));

		public static Rule Whitespace = Alternatives(Comment,Empty);
		public static Rule Expression = DelayedRule(delegate() {
			return CachedRule(Alternatives(List,LiteralExpression, Call, CallSelect, Select, FunctionProgram,
				Search, Program, LastArgument));
		});
		public static Rule NewLine = Alternatives(Syntax.unixNewLine, Syntax.windowsNewLine);
		public static Rule EndOfLine = Sequence(
			StringRule(ZeroOrMoreChars(Chars(""+Syntax.space+Syntax.tab))),
			Alternatives(Syntax.unixNewLine,Syntax.windowsNewLine));

		public static Rule Integer = Sequence(new Action(
	        StringRule(OneOrMoreChars(Chars(Syntax.integer))), 
	        delegate(Parser p, Map map, ref Map result) {
				Rational rational=new Rational(double.Parse(map.GetString()),1.0);
				if(rational.GetInteger()!=null) {
				    result=new Integer32(rational.GetInt32());
					result.Source = rational.Source;
				}
				else {
				    result=rational;
					result.Source = rational.Source;
				}
			}));

		public static Rule StringLine=StringRule(ZeroOrMoreChars(CharsExcept("\n\r")));
		public static Rule CharacterDataExpression = Sequence(
			Syntax.character,
			ReferenceAssignment(ReallyOneChar(CharsExcept(Syntax.character.ToString()))),
			Syntax.character
		);
		public static Rule String = Sequence(
			Syntax.@string,
			ReferenceAssignment(
			Alternatives(
				Sequence(
					ReferenceAssignment(
						StringRule(OneOrMoreChars(CharsExcept(""+Syntax.@string)))),
					OptionalError(Syntax.@string)))));

		public static Rule Number = Sequence(
			ReferenceAssignment(Integer),
			new Action(
			Optional(Sequence(
				Syntax.fraction,
				ReferenceAssignment(Integer))), delegate(Parser p, Map map, ref Map result) {
				if(map!=null) {
					result=new Rational(result.GetNumber().GetDouble(),map.GetNumber().GetDouble());
					result.Source = map.Source;
				}
			}));

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
		public static Rule LookupString = CachedRule(
			StringRule(StringSequence(
		    OneChar(CharsExcept(Syntax.lookupStringForbiddenFirst)),
		    ZeroOrMoreChars(CharsExcept(Syntax.lookupStringForbidden)))));

		public static Rule Value = DelayedRule(delegate {
			return Alternatives(ListMap,FunctionMap, MapRule, String, Number, CharacterDataExpression);
		});
		private static Rule LookupAnything = Sequence(Syntax.lookupAnythingStart,ReferenceAssignment(Value));

		public static Rule Function = Sequence(
			Assign(
				CodeKeys.Parameter,
				StringRule(ZeroOrMoreChars(
						CharsExcept(""+Syntax.@string+Syntax.function+Syntax.indentation+
							Syntax.windowsNewLine[0]+Syntax.unixNewLine)))),
			Syntax.function,
			Assign(CodeKeys.Expression,Expression),
			Whitespace);

		public static Rule Entry = Alternatives(
			Sequence(Assign(CodeKeys.Function,Function)),
			Sequence(
				Assign(1,Alternatives(Number,LookupString,LookupAnything)),
				Syntax.statement,
				new Action(Value, delegate(Parser parser, Map map, ref Map result) {
						result = new DictionaryMap(result[1], map);
				}),
			 Whitespace));

		public static Rule MapRule = Sequence(
			Syntax.programStart,
			Whitespace,
			ReferenceAssignment(
				OneOrMore(
					new Action(
						Sequence(
							ReferenceAssignment(Entry), Whitespace, OptionalError(Syntax.programSeparator), Whitespace),
						delegate(Parser parser, Map map, ref Map result) {
							result = Library.Merge(result, map);
						}
			))),
			OptionalError(Syntax.programEnd),
			Whitespace
		);
		Dictionary<State, string> errors = new Dictionary<State, string>();
		private static Rule Arg=DelayedRule(delegate {
			return Alternatives(List,
				LastArgument, FunctionProgram,
				LiteralExpression, Call, Select,
				Search, Program);
		});
		public static Rule Call = DelayedRule(delegate() {
			return Sequence(
				Assign(CodeKeys.Call,
				Sequence(
					Assign(1,
						Alternatives(List,FunctionProgram, LiteralExpression, CallSelect, Select, Search, Program, LastArgument)),
						Whitespace,
						Syntax.callStart,
						Append(
							Alternatives(
								Sequence(
									Whitespace,
									Assign(1,Alternatives(Arg,LiteralRule(new DictionaryMap(CodeKeys.Literal,Map.Empty)))),
									Append(
										ZeroOrMore(
											Autokey(
												Sequence(
													Whitespace,
													Syntax.callSeparator,
													Whitespace,
													ReferenceAssignment(Arg))))),
									Whitespace
									, OptionalError(Syntax.callEnd)
									)
									)))
								));
		});
		// combine with above!!!!!!!!!!!!!
		public static Rule SimpleCall = DelayedRule(delegate() {
			return Sequence(
				Assign(CodeKeys.Call,
				Sequence(
					Assign(1,
						Alternatives(List,FunctionProgram, LiteralExpression, SmallSelect, Search, Program, LastArgument)),
						Whitespace,
						Syntax.callStart,
						Append(
							Alternatives(
								Sequence(
									Whitespace,
									Append(
										ZeroOrMore(
											Autokey(
												Sequence(
													Whitespace,
													ReferenceAssignment(Alternatives(List,
														LastArgument, FunctionProgram, LiteralExpression,
														Call, Select, Search, Program)),
													// should be optional error
													Optional(Syntax.callSeparator)
													)))),
									Whitespace
									, Syntax.callEnd
									)
									)))
								));
		});
		public static Rule FunctionExpression = Sequence(
			Assign(CodeKeys.Key,LiteralRule(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))),
			Assign(CodeKeys.Value,Sequence(Assign(CodeKeys.Literal,Function)))
		);

		private static Rule Simple(char c, Map literal) {
			return Sequence(c,ReferenceAssignment(LiteralRule(literal)));
		}
		private static Rule EmptyMap = Simple(Syntax.emptyMap,Map.Empty);
		private static Rule Current = Simple(Syntax.current,new DictionaryMap(CodeKeys.Current, Map.Empty));
		public static Rule LastArgument = Simple(
			Syntax.lastArgument,
			new DictionaryMap(CodeKeys.LastArgument, Map.Empty)
		);
		private static Rule Root = Simple(Syntax.root,new DictionaryMap(CodeKeys.Root,Map.Empty));
		private static Rule LiteralExpression = Sequence(
			Assign(CodeKeys.Literal,Alternatives(EmptyMap,Number,String,CharacterDataExpression))
		);
		private static Rule LookupAnythingExpression = Sequence(
			Syntax.lookupAnythingStart,ReferenceAssignment(Expression),OptionalError(Syntax.lookupAnythingEnd)
		);
		private static Rule LookupStringExpression = Sequence(Assign(CodeKeys.Literal,LookupString));
		private static Rule Search = CachedRule(Sequence(
			Assign(
				CodeKeys.Search,
				Alternatives(
					Prefix(Syntax.search,Expression),
					Alternatives(LookupStringExpression,LookupAnythingExpression)))));

		private static Rule SmallSelect = DelayedRule(delegate {
			return CachedRule(Sequence(
				Assign(
					CodeKeys.Select,
					Sequence(
						Assign(1,
							Alternatives(Root,Search,LastArgument,Program,LiteralExpression)),
						Append(
							OneOrMore(Autokey(Prefix(Syntax.select, Alternatives(
								LookupStringExpression,LookupAnythingExpression,LiteralExpression)))))))));
		});

		private static Rule CallSelect = DelayedRule(delegate{
			return CachedRule(Sequence(
				Assign(
					CodeKeys.Select,
					Sequence(
						Assign(1,Alternatives(SimpleCall)),
						Append(
							OneOrMore(Autokey(
								Prefix(Syntax.select, Alternatives(LookupStringExpression,LookupAnythingExpression,LiteralExpression)))))))));
		});

		private static Rule Select = DelayedRule(delegate{
			return CachedRule(Sequence(
				Assign(
					CodeKeys.Select,
					Sequence(
						Assign(1,
							Alternatives(Root,Search,LastArgument,Program,LiteralExpression)),
						Append(
							OneOrMore(Autokey(Prefix(Syntax.select, Alternatives(
								LookupStringExpression,LookupAnythingExpression,LiteralExpression)))))))));
		});

		public static Rule ListMap = Sequence(
			Syntax.arrayStart,
			ReferenceAssignment(
				PrePost(
					delegate(Parser p) {p.defaultKeys.Push(1);},
					Sequence(
						Whitespace,
						Assign(1,Value),
						Whitespace,
						Append(
							ZeroOrMore(
								Autokey(
									Sequence(
										OptionalError(Syntax.arraySeparator),
										ReferenceAssignment(Sequence(Whitespace, ReferenceAssignment(Value))))))),
						Whitespace,
						OptionalError(Syntax.arrayEnd)),
						delegate(Parser p) {
							p.defaultKeys.Pop();
						})));

		public static Rule ListEntry = new Rule(delegate(Parser p, ref Map map) {
			if (Parser.Expression.Match(p, ref map)) {
				Map key = new DictionaryMap(CodeKeys.Literal, new Integer32(p.defaultKeys.Peek()));
				// not really correct
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
		public static Rule ComplexList() {
			Action entryAction = ReferenceAssignment(ListEntry);
			return Sequence(
				Assign(CodeKeys.Program,
					Sequence(
						Whitespace,
						Syntax.arrayStart,
						Append(
							Alternatives(
								Sequence(Whitespace,Assign(1,ListEntry),
									Whitespace,
									Append(
										ZeroOrMore(
											Autokey(
												Sequence(OptionalError(Syntax.arraySeparator),Whitespace,entryAction)))),
									Whitespace,
									Syntax.arrayEnd))))));
		}
		public static Rule List = PrePost(
			delegate(Parser p) {p.defaultKeys.Push(1);},
			ComplexList(),
			delegate(Parser p) {p.defaultKeys.Pop();}
		);
		public static Rule ComplexStatement(Rule rule, Action action) {
			return Sequence(
				action,
				rule != null ? (Action)rule : null,
				Assign(CodeKeys.Value, Expression),
				Whitespace
			);
		}
		public static Rule DiscardStatement = ComplexStatement(
			null,Assign(CodeKeys.Discard, LiteralRule(Map.Empty))
		);
		public static Rule CurrentStatement = ComplexStatement(
			Syntax.current,Assign(CodeKeys.Current, LiteralRule(Map.Empty))
		);
		public static Rule Prefix(Rule pre,Rule rule) {
			return Sequence(pre,ReferenceAssignment(rule));
		}
		public static Rule NormalStatement = ComplexStatement(
			Syntax.statement,
			Assign(
				CodeKeys.Key,
				Alternatives(
					Prefix(Syntax.lookupAnythingStart, Sequence(ReferenceAssignment(Expression), OptionalError(Syntax.lookupAnythingEnd))),
					Select,
					Sequence(Assign(CodeKeys.Literal, LookupString)),
					Expression)));

		public static Rule Statement = ComplexStatement(
			Syntax.searchStatement,
			Assign(
				CodeKeys.Keys,
				Alternatives(
					Sequence(Assign(CodeKeys.Literal,LookupString)),Expression)));

		public static Rule AllStatements = Sequence(
			ReferenceAssignment(
				Alternatives(FunctionExpression,CurrentStatement,NormalStatement,Statement,DiscardStatement)),
			Whitespace,
			OptionalError(Syntax.statementEnd)
		);
		public static Rule FunctionMap = Sequence(
			Assign(CodeKeys.Function,
				Sequence(
					Assign(CodeKeys.Parameter,StringRule(ZeroOrMoreChars(CharsExcept(Syntax.lookupStringForbiddenFirst)))),
					Syntax.functionAlternativeStart,
						Whitespace,
						Assign(CodeKeys.Expression, Expression),
					Whitespace,
					OptionalError(Syntax.functionAlternativeEnd),
			Whitespace)));

		public static Rule FunctionProgram = Sequence(
			Assign(CodeKeys.Program,
				Sequence(
					Assign(1,
						Sequence(
							Assign(CodeKeys.Key, LiteralRule(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))),
							Assign(CodeKeys.Value, Sequence(
								Assign(CodeKeys.Literal,
									Sequence(
										Assign(
											CodeKeys.Parameter,
											StringRule(ZeroOrMoreChars(CharsExcept(Syntax.lookupStringForbiddenFirst)))),
										Syntax.functionAlternativeStart,Whitespace,
											Assign(CodeKeys.Expression, Expression),
										Whitespace,
										OptionalError(Syntax.functionAlternativeEnd))))))))));

		public static Rule Program = DelayedRule(delegate {
			return Sequence(
				Assign(CodeKeys.Program,
					Sequence(
						Syntax.programStart,
						Append(
							Alternatives(
								Sequence(
									Whitespace,
									Append(
										ZeroOrMore(
											Autokey(
												Sequence(
													Whitespace,
													ReferenceAssignment(AllStatements))))),
									Whitespace,OptionalError(Syntax.programEnd))
									)))
			));
		});
		public static Rule OptionalError(char c) {
			return Alternatives(c, Error("Missing '" + c + "'"));
		}
		public static Rule Error(string text) {
			return new Rule(delegate (Parser parser,ref Map map) {
				if (text.Contains(";")) {
				}
				Error[] errors=new Error[parser.state.Errors.Length+1];
				parser.state.Errors.CopyTo(errors, 0);
				errors[errors.Length-1]=new Error(text, new Source(parser.state.Line, parser.state.Column, parser.state.FileName),parser.state);
				parser.state.Errors = errors;
				return true;
			});
		}
		public class Action {
			public static implicit operator Action(char c) {
				return StringRule(OneChar(SingleChar(c)));
			}
			public static implicit operator Action(Rule rule) {
				return new Action(rule,delegate { });
			}
			private Rule rule;
			public Action(Rule rule, CustomActionDelegate action) { 
				this.rule = rule;
				this.action = action;
			}
			public bool Execute(Parser parser, ref Map result) {
				Map map=null;
				if (rule.Match(parser,ref map)) {
					action(parser, map, ref result);
					return true;
				}
				return false;
			}
			private CustomActionDelegate action;
		}
		public static Action Autokey(Rule rule) {
			return new Action(rule,delegate(Parser parser, Map map, ref Map result) {
				result.Append(map);
			});
		}
		public static Action Assign(Map key, Rule rule) {
			return new Action(rule, delegate(Parser parser, Map map, ref Map result) {
				if (map != null) {
					result[key] = map;
				}
			});
		}
		public static Action ReferenceAssignment(Rule rule) {
			return new Action(rule,delegate(Parser parser, Map map, ref Map result) {
				result = map;
			});
		}
		public static Action Append(Rule rule) {
			return new Action(rule, delegate(Parser parser, Map map, ref Map result) {
				foreach (Map m in map.Array) {
					result.Append(m);
				}
			});
		}
		public delegate void CustomActionDelegate(Parser p, Map map, ref Map result);
		public delegate bool Precondition(Parser p);
		public class CachedResult{
			public CachedResult(Map map,State state) {
				this.map=map;
				this.state=state;
			}
			public Map map;
			public State state;
		}
		public static Rule CachedRule(Rule rule) {
			Dictionary<State, CachedResult> cached = new Dictionary<State, CachedResult>();
			allCached.Add(cached);
			return new Rule(delegate(Parser parser, ref Map map) {
				CachedResult cachedResult;
				State state = parser.state;
				if (cached.TryGetValue(state, out cachedResult)) {
					map = cachedResult.map;
					if (parser.state.Text.Length == parser.state.index + 1) {
						return false;
					}
					parser.state = cachedResult.state;
					return true;
				}
				if (rule.Match(parser, ref map)) {
					cached[state] = new CachedResult(map, parser.state);
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
			public static implicit operator Rule(string s) {
				List<Action> actions = new List<Action>();
				foreach (char c in s) {
					actions.Add(c);
				}
				return Sequence(actions.ToArray());
			}
			public static implicit operator Rule(char c) {
			    return StringRule(OneChar(SingleChar(c)));
			}
			public int mismatches=0;
			public int calls=0;

			public bool Match(Parser parser, ref Map map) {
				if(precondition!=null) { 
					if(!precondition(parser)) {
						return false;
					}
				}
				calls++;
				State oldState=parser.state;
				bool matched;
				Map result=null;
				matched=parseFunction(parser, ref result);
				if (!matched) {
					mismatches++;
					parser.state=oldState;
				}
				else {
					if (result != null) {
						result.Source = new Extent(
							new Source(oldState.Line, oldState.Column, parser.state.FileName),
							new Source(parser.state.Line, parser.state.Column, parser.state.FileName));
						if (result.Equals(new StringMap("apply"))) {
						}
					}
				}
				map=result;
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
			string s = characters + Syntax.endOfFile;
			return new CharRule(delegate(char c) {
				return s.IndexOf(c) == -1;
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
					if (parser.Look(offset).Equals(Syntax.unixNewLine)) {
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
				if (rule(next)) {
					map = next;
					parser.state.index++;
					parser.state.Column++;
					if (next.Equals(Syntax.unixNewLine)) {
						parser.state.Line++;
						parser.state.Column = 1;
					}
					return true;
				}
				else {
					return false;
				}
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
		        if (del(parser, ref s)) {
		            map = s;
		            return true;
		        }
		        else {
		            return false;
		        }
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
			Rule rule=null;
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
					bool matched = expression.Match(parser, ref map);
					if (matched) {
						return true;
					}
				}
				return false;
			});
		}
		public static Rule Sequence(params Action[] actions) {
			return new Rule(delegate(Parser parser, ref Map match) {
				Map result = new DictionaryMap();
				bool success = true;
				foreach (Action action in actions) {
					if (action != null) {
						bool matched = action.Execute(parser, ref result);
						if (!matched) {
							success = false;
							break;
						}
					}
				}
				if (!success) {
					match = null;
					return false;
				}
				else {
					match = result;
					return true;
				}
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
				Map list = new DictionaryMap();
				while (true) {
					if (!action.Execute(parser, ref list)) {
						break;
					}
				}
				map = list;
				return true;
			});
		}
		public static Rule OneOrMore (Action action) {
			return new Rule(delegate (Parser parser, ref Map map) {
				Map list = new DictionaryMap();
				bool matched = false;
				while (true) {
					if (!action.Execute(parser, ref list)) {
						break;
					}
					matched = true;
				}
				map=list;
				return matched;
			});
		}
		public static Rule Optional(Rule rule) {
			return new Rule(delegate(Parser parser, ref Map match) {
				Map matched = null;
				rule.Match(parser, ref matched);
				if (matched == null) {
					match = null;
					return true;
				}
				else {
					match = matched;
					return true;
				}
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
			Map result=null;
			Parser.Value.Match(parser, ref result);
			if (parser.state.index != parser.state.Text.Length - 1 || parser.state.Errors.Length!=0) {
				string t = "";
				if (parser.state.index != parser.state.Text.Length - 1) {
					t += "Expected end of file. " + new Source(parser.state.Line, parser.state.Column, parser.state.FileName).ToString()+"\n";
				}
				foreach (Error error in parser.state.Errors) {
					t += error.Text + error.Source.ToString()+"\n";
				}
				throw new SyntaxException(t, parser);
			}
			foreach (Dictionary<State, CachedResult> cached in Parser.allCached) {
				cached.Clear();
			}
			return result;
		}
	}
	public class Syntax {
		public const char searchStatement=':';
		public const char search='!';
		public const char comment='/';
		public const char statementEnd=';';
		public const char statement='=';
		public const char programStart = '[';
		public const char programEnd = ']';
		public const char functionAlternativeStart = '{';
		public const char functionAlternativeEnd = '}';
		public const char arrayStart = '<';
		public const char arrayEnd = '>';
		public const char arraySeparator = ',';
		public const char programSeparator = ';';
		public const char lastArgument = '@';
		public const char autokey = '.';
		public const char callSeparator = ',';
		public const char callStart = '(';
		public const char callEnd = ')';
		public const char root = '/';
		public const char negative = '-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char indentation = '\t';
		public const char unixNewLine = '\n';
		public const string windowsNewLine = "\r\n";
		public const char function = '|';
		public const char @string = '\"';
		public const char emptyMap = '0';
		public const char explicitCall = '-';
		public const char select = '.';
		public const char character = '\'';
		public const char space = ' ';
		public const char tab = '\t';
		public const char current = '&';
		public const char lookupAnythingStart='<';
		public const char lookupAnythingEnd='>';
		public static readonly string integer = "0123456789-";
		public static readonly string lookupStringForbidden =

			"" + current + functionAlternativeStart + functionAlternativeEnd + lastArgument + explicitCall + indentation + '\r' + '\n' +
			function+ @string+emptyMap+ search + root+ callStart+ callEnd+ 
			character+ programStart+ '*'+ '$'+ '\\'+ lookupAnythingStart+ statement+ arrayStart+
			'-'+ searchStatement+ select+ ' '+ '-'+ arrayStart+ arrayEnd+ '*'+ lookupAnythingEnd+ 
			programStart+ programSeparator +callSeparator+programEnd+arrayEnd+statementEnd;
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
			if(map.IsString) {
				return String(map,indentation);
			}
			if (map is DotNetMap) {
				return map.ToString();
			}
			else if (map.Count == 0) {
				if (indentation < 0) {
					return "";
				}
				else {
					return "0";
				}
			}
			else if (map.IsNumber) {
				return Number(map);
			}
			else if (map.IsString) {
				return String(map, indentation);
			}
			else {
				return Map(map, indentation);
			}
		}
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
			if (entry.Key.Equals(CodeKeys.Function)) {
				return Function(entry.Value, indentation + 1);
			}
			else {
				return (Indentation(indentation + 1)
					+ Key(indentation, entry) +
					"=" +
					Serialize(entry.Value, indentation + 1)
					+ Environment.NewLine).TrimEnd('\r', '\n')
					+ Environment.NewLine;
			}
		}
		private static string Literal(Map value, int indentation) {
			if (value.IsNumber) {
				return Number(value);
			}
			else if (value.IsString) {
				return String(value, indentation);
			}
			else {
				return "error";
			}
		}
		private static string Function(Map value, int indentation) {
			return value[CodeKeys.Parameter].GetString() + "|" + Expression(value[CodeKeys.Expression], indentation);
		}
		private static string Root() {
			return "/";
		}
		private static string Expression(Map map, int indentation) {
			if (map.ContainsKey(CodeKeys.Literal)) {
				return Literal(map[CodeKeys.Literal], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Root)) {
				return Root();}
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
		private static string FunctionStatement(Map map, int indentation) {
			return map[CodeKeys.Parameter].GetString() + "|" +
				Expression(map[CodeKeys.Expression], indentation);
		}
		private static string KeyStatement(Map map, int indentation) {
			Map key = map[CodeKeys.Key];
			if (key.Equals(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))) {
				return FunctionStatement(map[CodeKeys.Value][CodeKeys.Literal], indentation);
			}
			else {
				return Expression(map[CodeKeys.Key], indentation) + "="
					+ Expression(map[CodeKeys.Value], indentation);
			}
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
				text += Indentation(indentation) + Trim(Statement(m, indentation)) + NewLine();}
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
				text += Indentation(indentation) +
					Trim(Expression(m, indentation)) + NewLine();
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
				}
			}
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
	namespace Test {
		public class MetaTest : TestRunner {
			public static int Leaves(Map map) {
				int count = 0;
				foreach (KeyValuePair<Map, Map> pair in map) {
					if (pair.Value.IsNumber) {
						count++;}
					else {
						count += Leaves(pair.Value);}}
				return count;
			}
			public static string TestPath {
				get {
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public class Serialization : Test {
				public override object GetResult(out int level) {
					level = 1;
					return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
				}
			}
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
			public class Fibo : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"fibo.meta"), new DictionaryMap());
				}
			}
			public class MergeSort : Test {
				public override object GetResult(out int level) {
					level = 2;
					return Interpreter.Run(Path.Combine(Interpreter.InstallationPath, @"mergeSort.meta"), new DictionaryMap());
				}
			}
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
		public static readonly Map Expression = "expression";
		public static readonly Map Parameter = "parameter";
		public static readonly Map Root = "root";
		public static readonly Map Search = "search";
		public static readonly Map Current = "current";
		public static readonly Map Scope = "scope";
		public static readonly Map Literal = "literal";
		public static readonly Map Function = "function";
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
				else if (pair.Value.IsNumber) {
					count++;
				}
				else {
					count += CountLeaves(pair.Value);
				}
			}
			return count;
		}
	}
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
	public class ListMap : Map {
		public override int GetHashCode() {
			return list.Count;
		}
		public override Number GetNumber() {
			if(Count==0) {
				return 0;
			}
			return null;
		}
	    public override Map Copy() {
	        return this;
	    }
	    public override void Append(Map map){
	        list.Add(map);
	    }
	    private List<Map> list;

	    public ListMap(): this(5) {
	    }
	    public ListMap(List<Map> list) {
	        this.list = list;
	    }
	    public ListMap(int capacity) {
	        this.list = new List<Map>(capacity);
	    }
	    public override Map this[Map key] {
	        get {
	            Map value = null;
	            if (key.IsNumber) {
	                int integer = key.GetNumber().GetInt32();
	                if (integer >= 1 && integer <= list.Count) {
	                    value = list[integer - 1];
	                }
	            }
	            return value;
	        }
	        set {
	            if (key.IsNumber) {
	                int integer = key.GetNumber().GetInt32();
	                if (integer >= 1 && integer <= list.Count) {
	                    list[integer - 1] = value;
	                    return;
	                }
	                else if (integer == list.Count + 1) {
	                    list.Add(value);
	                    return;
	                }
	            }
	            throw new Exception("Method or operation not implemented.");
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
	        bool containsKey;
	        if (key.IsNumber) {
	            Number integer = key.GetNumber();
				if (integer >= 1 && integer <= list.Count) {
	                containsKey = true;
	            }
	            else {
	                containsKey = false;
	            }
	        }
	        else {
	            containsKey = false;
	        }
	        return containsKey;
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
		public static Map Slice(Map array,int start,int end) {
			return new DictionaryMap(new List<Map>(array.Array).GetRange(start-1,Math.Max(end-start+1,0)));
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
		public static Number Floor(Number n){
			return new Rational(n.GetRealInt64());
		}
		public static Map While(Map condition,Map body) {
			while(Convert.ToBoolean(condition.Call(Map.Empty).GetNumber().GetInt32())) {
				body.Call(Map.Empty);
			}
			return Map.Empty;
		}
		public static Map Double(Map d) {
			return new ObjectMap((object)(double)d.GetNumber().GetDouble());
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
		public static Map Raise(Number a, Number b) {
			return new Rational(Math.Pow(a.GetDouble(), b.GetDouble()));
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
				return then.Call(Map.Empty);}
			else {
				return els.Call(Map.Empty);
			}
		}
		public static Map Sum(Map func, Map arg) {
			IEnumerator<Map> enumerator = arg.Array.GetEnumerator();
			if (enumerator.MoveNext()) {
				Map result = enumerator.Current.Copy();
				while (enumerator.MoveNext()) {
					result = func.Call(result).Call(enumerator.Current);}
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
			Map result = new DictionaryMap();
			for (int i = 1; i<=arg; i++) {
				result.Append(i);
			}
			return result;
		}
	}
	public class LiteralExpression : Expression {
		private Map literal;
		public LiteralExpression(Map literal, Expression parent) : base(null, parent) {
			this.literal = literal;
		}
		public override Map GetStructure() {
			return literal;
		}
		public override Compiled GetCompiled(Expression parent) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class LiteralStatement : Statement {
		private LiteralExpression program;
		public LiteralStatement(LiteralExpression program)
			: base(null, null, 0) {
			this.program = program;}
		public override Map Pre() {
			return program.EvaluateStructure();
		}
		protected override Map CurrentImplementation(Map previous) {
			return program.EvaluateStructure();
		}
		public override CompiledStatement Compile() {
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public abstract class ScopeExpression : Expression {
		public ScopeExpression(Extent source, Expression parent) : base(source, parent) {
		}
	}
	public delegate T SingleDelegate<T>(T t);
	public class MergeCompile:CompilableAttribute {
		public override Map GetStructure() {
			return null;
		}
	}
	public abstract class CompilableAttribute : Attribute {
		public abstract Map GetStructure();
	}
	public class EmptyMap:Map {
		public override Map Mutable() {
			return new DictionaryMap();
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
		public override bool IsNormal {
			get {
				return true;
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

		public Number zero=new Integer32(0);
		public string emptyString="";
		public override string GetString() {
			return emptyString;
		}
		public override Map Copy() {
			return new DictionaryMap();
		}
		public override Number GetNumber() {
			return zero;
		}
	}
	public abstract class Map:IEnumerable<KeyValuePair<Map, Map>>, ISerializeEnumerableSpecial {
		public virtual object GetObject() {
			return this;
		}
		public override bool Equals(object obj) {
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
			return copy;
		}
		public virtual Integer32 GetInteger() {
			Number number=GetNumber();
			if(number!=null) {
				return number.GetInteger();
			}
			throw new Exception("Map is not a number.");
		}
		public virtual int GetInt32() {
			Number number=GetNumber();
			if(number!=null) {
				return number.GetInt32();
			}
			throw new Exception("Map is not a number.");
		}
		public virtual string GetString() {
			if(ArrayCount ==Count ) {
				StringBuilder text = new StringBuilder("");
				foreach (Map map in Array) {
					Number number=map.GetNumber();
					if(number==null) {
						return null;
					}
					else {
						if(number.GetInt32()>Char.MinValue && number.GetInt32() <Char.MaxValue) {
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
		public virtual Map Mutable() {
			return this;
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
		public virtual Map Call(Map argument) {
			Map.arguments.Push(argument);
			Map result;
		    Map function=this[CodeKeys.Function];
		    if (function!=null) {
		        result=function.GetExpression(null).GetCompiled()(this);
			}
		    else {
		        throw new ApplicationException("Map is not a function: " + Meta.Serialization.Serialize(this));
		    }
			Map.arguments.Pop();
			return result;
		}
		public static Map Empty=new EmptyMap();
		public Map DeepCopy() {
			Map clone = new DictionaryMap();
			clone.Scope = Scope;
			clone.Source = Source;
			clone.Expression=Expression;
			clone.IsConstant = this.IsConstant;
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
		public virtual bool IsNormal {
			get {
				return true;
			}
		}
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
			else if (this.IsNumber) {
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
			return new Integer32(integer);
		}
		private Extent source;
		public virtual Extent Source {
			get {
				return source;
			}
			set {
				source = value;
			}
		}
		public virtual bool IsConstant {
			get {
				return true;
			}
			set {
			}
		}
		public abstract int ArrayCount {
			get;
		}
		public bool IsString {
		    get {
				return GetString()!=null;
			}
		}
		public bool IsNumber {
		    get {
				return GetNumber()!=null;
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
		public Map Scope;
		public void Compile(Expression parent) {
			GetExpression(parent).Compile();
		}
		public Expression GetExpression() {
			return GetExpression(null);
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
		public static Dict<Map, Type> expressions = new Dict<Map, Type>()
			- CodeKeys.Call + typeof(Call) - CodeKeys.Program + typeof(Program) - CodeKeys.Literal + typeof(Literal)
			- CodeKeys.Select + typeof(Select) - CodeKeys.Root + typeof(Root)
			- CodeKeys.Search + typeof(Search);
		public static Dict<Map, Type> statements = new Dict<Map, Type>()
			- CodeKeys.Keys + typeof(SearchStatement) - CodeKeys.Current + typeof(CurrentStatement)
			- CodeKeys.Key + typeof(KeyStatement) - CodeKeys.Discard + typeof(DiscardStatement);
		public Expression CreateExpression(Expression parent) {
		    if(this.Count==1) {
				if (ContainsKey(CodeKeys.LastArgument)) {
					return Expression.LastArgument(this[CodeKeys.LastArgument], parent);
				}
				foreach(Map key in Keys) {
				    if(expressions.ContainsKey(key)) {
				        Expression x=(Expression)expressions[key].GetConstructor(new Type[] {typeof(Map),typeof(Expression)}
				        ).Invoke(new object[] {this[key],parent});
						x.Source = Source;
						return x;
				    }
				}
			}
			if (ContainsKey(CodeKeys.Expression)) {
		        return new Function(parent,this);
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
}