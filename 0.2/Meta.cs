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
using Meta.Test;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows;
using SdlDotNet;

namespace Meta {
	public abstract class Compiled {
		public Extent Source;
		public Compiled(Extent source) {
			this.Source = source;}
		public Map Evaluate(Map context) {
			return EvaluateImplementation(context);}
		public abstract Map EvaluateImplementation(Map context);}
	public abstract class Expression {
		public Compiled compiled;
		//private Dictionary<Type, Compiled> specialized;
		//public Dictionary<Type, Compiled> Specialized {
		//    get {
		//        if (specialized == null) {
		//            specialized = new Dictionary<Type, Compiled>();
		//        }
		//        return specialized;}
		//}
		//public Compiled GetCompiled(Type type) {
		//    try {
		//        if (Specialized.ContainsKey(type)) {
		//            return Specialized[type];}
		//        else if (Specialized.ContainsKey(typeof(Map))) {
		//            return Specialized[typeof(Map)];}
		//        else {
		//            return null;}
		//    }
		//    catch (Exception e) {
		//        throw e;}
		//}

		//private Dictionary<Type, Compiled> specialized;
		//public Dictionary<Type, Compiled> Specialized {
		//    get {
		//        if (specialized == null) {
		//            specialized = new Dictionary<Type, Compiled>();
		//        }
		//        return specialized;}
		//}
		//public Compiled GetCompiled(Type type) {
		//    try {
		//        if (Specialized.ContainsKey(type)) {
		//            return Specialized[type];}
		//        else if (Specialized.ContainsKey(typeof(Map))) {
		//            return Specialized[typeof(Map)];}
		//        else {
		//            return null;}
		//    }
		//    catch (Exception e) {
		//        throw e;}
		//}

		//private Dictionary<Type, Compiled> specialized;
		//private Dictionary<Type, Compiled> Specialized {
		//    get {
		//        if (specialized == null) {
		//            specialized = new Dictionary<Type, Compiled>();
		//        }
		//        return specialized;}
		//}
		//public Compiled GetCompiled(Type type) {
		//    try {
		//        if (Specialized.ContainsKey(type)) {
		//            return Specialized[type];}
		//        else if (Specialized.ContainsKey(typeof(Map))) {
		//            return Specialized[typeof(Map)];}
		//        else {
		//            return null;}
		//    }
		//    catch (Exception e) {
		//        throw e;}
		//}
		public bool isFunction = false;
		public readonly Extent Source;
		public readonly Expression Parent;
		public Statement Statement;
		public Expression(Extent source, Expression parent) {
			this.Source = source;
			this.Parent = parent;}
		private bool evaluated = false;
		private Structure structure;
		public Map GetConstant() {
			Structure s=EvaluateStructure();
			return s !=null && s.IsConstant? ((LiteralStructure)s).Literal:null;
		}
		public Map EvaluateMapStructure() {
			Structure s=EvaluateStructure();
			Map m;
			if(s!=null) {
				m=((LiteralStructure)s).Literal;
			}
			else {
				m=null;
			}
			return m;
		}
		public Structure EvaluateStructure() {
			if (!evaluated) {
				structure = StructureImplementation();
				evaluated = true;
			}
			return structure;
		}
		public abstract Structure StructureImplementation();
		public Compiled Compile(Expression parent) {
			Compiled result = CompileImplementation(parent);
			if (Source != null) {
				if (!sources.ContainsKey(Source.end)) {
					sources[Source.end] = new List<Expression>();}
				sources[Source.end].Add(this);}
			return result;}
		public static Dictionary<Source, List<Expression>> sources = new Dictionary<Source, List<Expression>>();
		public abstract Compiled CompileImplementation(Expression parent);}
	public class LastArgument : Expression {
		public LastArgument(Map code, Expression parent)
			: base(code.Source, parent) {}
		public override Structure StructureImplementation() {
			return null;}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledLastArgument(Source);}}
	public class CompiledLastArgument : Compiled {
		public CompiledLastArgument(Extent source)
			: base(source) {}
		public override Map EvaluateImplementation(Map context) {
			return Map.arguments.Peek();}}
	public class Call : Expression {
		public List<Expression> calls;
		public Call(Map code, Map parameterName, Expression parent)
			: base(code.Source, parent) {
			this.calls = new List<Expression>();
			foreach (Map m in code.Array) {
				calls.Add(m.GetExpression(this));}
			if (calls.Count == 1) {
				calls.Add(new Literal(Map.Empty, this));}}
		public override Structure StructureImplementation() {
			List<object> arguments;
			MethodBase method;
			if (CallStuff(out arguments, out method)) {
				if (method is ConstructorInfo) {
					Dictionary<Map, Member> type = ObjectMap.cache.GetMembers(method.DeclaringType);
					Map result = new Map();
					result.IsConstant=false;
					foreach (Map key in type.Keys) {
						result[key] = new Map();}
					return new LiteralStructure(result);
				}
				else if (arguments != null && method.GetCustomAttributes(typeof(CompilableAttribute), false).Length != 0) {
					try {
						Map result = (Map)method.Invoke(null, arguments.ToArray());
						result.IsConstant = false;
						return new LiteralStructure(result);
					}
					catch (Exception e) {}}}
			return null;}
		public bool CallStuff(out List<object> arguments, out MethodBase m) {
			Map first = calls[0].GetConstant();
			if (first != null) {
				Method method;
				if (first.Strategy is TypeMap) {
					method = (Method)((TypeMap)first.Strategy).Constructor.Strategy;
				}
				else if (first.Strategy is Method) {
					method = (Method)first.Strategy;
				}
				else {
					method = null;}
				if (method != null) {
					if (method.parameters.Length == calls.Count - 1 || (calls.Count == 2 && method.parameters.Length == 0)) {
						if (method.method.IsStatic || method.method is ConstructorInfo) {
							arguments = new List<object>();
							for (int i = 0; i < method.parameters.Length; i++) {
								Map arg = calls[i + 1].EvaluateMapStructure();
								if (arg == null) {
									m = method.method;
									return true;}
								else {
									object nextArg;
									if (Transform.TryToDotNet(arg, method.parameters[i].ParameterType, out nextArg)) {
										arguments.Add(nextArg);}
									else {
										m = method.method;
										return false;}}}
							m = method.method;
							return true;}}}}
			arguments = null;
			m = null;
			return false;}
		public override Compiled CompileImplementation(Expression parent) {
			List<object> arguments;
			MethodBase method;
			if (CallStuff(out arguments, out method)) {
				if (method.IsStatic) {
					return new EmittedCall((MethodInfo)method, calls.GetRange(1, calls.Count - 1).ConvertAll<Compiled>(
						delegate(Expression e) {
							return e.Compile(this);}), Source);
				}
			}
			if(calls.Count==2 && calls[0].GetConstant()!=null) {
			    Structure s=calls[1].EvaluateStructure();
				if(s!=null && s.IsNumber) {
				    //calls[0].GetConstant().Specialize(typeof(Number));
				}
			}
			return new CompiledCall(calls.ConvertAll<Compiled>(delegate(Expression e) {
				return e.Compile(this);}), Source);
		}
		//public override Compiled CompileImplementation(Expression parent) {
		//    List<object> arguments;
		//    MethodBase method;
		//    if (CallStuff(out arguments, out method)) {
		//        if (method.IsStatic) {
		//            return new EmittedCall((MethodInfo)method, calls.GetRange(1, calls.Count - 1).ConvertAll<Compiled>(
		//                delegate(Expression e) {
		//                    return e.Compile(this);}), Source);}}
		//    return new CompiledCall(calls.ConvertAll<Compiled>(delegate(Expression e) {
		//        return e.Compile(this);}), Source);
		//}
	}
	public class EmittedCall : Compiled {
		private List<Compiled> arguments;
		private ParameterInfo[] parameters;
		public EmittedCall(MethodInfo method, List<Compiled> arguments, Extent source)
			: base(source) {
			this.method = method;
			this.arguments = arguments;
			this.parameters = method.GetParameters();}
		private MethodInfo method;
		public override Map EvaluateImplementation(Map context) {
			List<object> args = new List<object>();
			for (int index = 0; index < parameters.Length; index++) {
				args.Add(Transform.ToDotNet(arguments[index].Evaluate(context), parameters[index].ParameterType));}
			try {
				object returnValue = method.Invoke(null, args.ToArray());
				return Transform.ToMeta(returnValue);}
			catch (MetaException e) {
				e.InvocationList.Add(new ExceptionLog(Source.start));
				throw e;}
			catch (Exception e) {
				throw new MetaException(e.Message, Source.start);}}}
	public class CompiledCall : Compiled {
		List<Compiled> calls;
		public CompiledCall(List<Compiled> calls, Extent source)
			: base(source) {
			this.calls = calls;}
		public override Map EvaluateImplementation(Map current) {
			Map result = calls[0].Evaluate(current);
			for (int i = 1; i < calls.Count; i++) {
				try {
					result = result.Call(calls[i].Evaluate(current));}
				catch (MetaException e) {
					e.InvocationList.Add(new ExceptionLog(Source.start));
					throw e;}
				catch (Exception e) {
					throw new MetaException(e.Message, Source.start);}}
			return result;}}
	public class Search : Expression {
		public override Structure StructureImplementation() {
			Map key;
			int count;
			Map value;
			if (FindStuff(out count, out key, out value)) {
				return new LiteralStructure(value);
			}
				//return value;}
			else {
				return null;}}
		private bool FindStuff(out int count, out Map key, out Map value) {
			Expression current = this;
			Structure keyStructure = expression.EvaluateStructure();
			if(keyStructure!=null ) {
				key=((LiteralStructure)keyStructure).Literal;
			}
			else {
				key=null;
			}
			//key = expression.EvaluateStructure();
			count = 0;
			if (key != null && key.IsConstant) {
				bool hasCrossedFunction = false;
				while (true) {
					while (current.Statement == null) {
						if (current.isFunction) {
							hasCrossedFunction = true;
							count++;}
						current = current.Parent;
						if (current == null) {
							break;}}
					if (current == null) {
						break;}
					Statement statement = current.Statement;
					Map structure = statement.PreMap();
					//Map structure = statement.Pre();
					if (structure == null) {
						statement.Pre();
						break;
					}
					if (structure.ContainsKey(key)) {
						value = structure[key];
						return true;
					}
					if (hasCrossedFunction) {
						if (!statement.NeverAddsKey(key)) {
							break;}
					}
					count++;
					current = current.Parent;}}
			value = null;
			return false;}
		private Expression expression;
		public Search(Map code, Expression parent)
			: base(code.Source, parent) {
			this.expression = code.GetExpression(this);}
		public override Compiled CompileImplementation(Expression parent) {
			int count;
			Map key;
			Map value;
			if (FindStuff(out count, out key, out value)) {
				if (value != null && value.IsConstant) {
					return new OptimizedSearch(value, Source);}
				else {
					return new FastSearch(key, count, Source);}}
			else {
				FindStuff(out count, out key, out value);
				return new CompiledSearch(expression.Compile(this), Source);}}}
	public class FastSearch : Compiled {
		private int count;
		private Map key;
		public FastSearch(Map key, int count, Extent source)
			: base(source) {
			this.key = key;
			this.count = count;}
		public override Map EvaluateImplementation(Map context) {
			Map selected = context;
			try {
				for (int i = 0; i < count; i++) {
					selected = selected.Scope;}
				int difference = 0;
				if (!selected.ContainsKey(key)) {
					selected = context;
					int realCount = 0;
					while (!selected.ContainsKey(key)) {
						selected = selected.Scope;
						realCount++;
						if (selected == null) {
							throw new KeyNotFound(key, Source.start, null);}}
					difference = count - realCount;
					if (difference != 0) {
						if (difference == 1) {}
						else if (difference == 2) {}
						else if (difference == 3) {}
						else if (difference == 4) {}
						else {}}}
				return selected[key].Copy();}
			catch (Exception e) {
				throw e;}}}
	public class OptimizedSearch : Compiled {
		private Map literal;
		public OptimizedSearch(Map literal, Extent source)
			: base(source) {
			this.literal = literal;}
		public override Map EvaluateImplementation(Map context) {
			return literal.Copy();}}
	public class CompiledSearch : Compiled {
		private Compiled expression;
		public CompiledSearch(Compiled expression, Extent source)
			: base(source) {
			this.expression = expression;}
		public override Map EvaluateImplementation(Map context) {
			Map key = expression.Evaluate(context);
			Map selected = context;
			while (!selected.ContainsKey(key)) {
				if (selected.Scope != null) {
					selected = selected.Scope;}
				else {
					throw new KeyNotFound(key, Source.start, null);}}
			return selected[key].Copy();}}
	public class CompiledProgram : Compiled {
		private List<CompiledStatement> statementList;
		public CompiledProgram(List<CompiledStatement> statementList, Extent source)
			: base(source) {
			this.statementList = statementList;}
		public override Map EvaluateImplementation(Map parent) {
			Map context = new Map();
			context.Scope = parent;
			foreach (CompiledStatement statement in statementList) {
				statement.Assign(ref context);}
			return context;
		}
	}
	public class Function:Program {
		private Dictionary<Type, Compiled> specialized=new Dictionary<Type, Compiled>();
		public Compiled GetSpecialized(Type type) {
	        if (specialized.ContainsKey(type)) {
	            return specialized[type];
			}
	        else {
	            return null;
			}
		}
		public Function(Extent source,Expression parent):base(source,parent) {
		}
	}
	public class Program : ScopeExpression {
		public override Structure StructureImplementation() {
			return statementList[statementList.Count - 1].Current();
		}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledProgram(statementList.ConvertAll<CompiledStatement>(delegate(Statement s) {
				return s.Compile();}), Source);}
		public List<Statement> statementList= new List<Statement>();
		public Program(Extent source,Expression parent):base(source,parent) {
		}
		public Program(Map code, Expression parent)
			: base(code.Source, parent) {
			int index = 0;
			foreach (Map m in code.Array) {
				statementList.Add(m.GetStatement(this, index));
				index++;}}}
	public abstract class CompiledStatement {
		public CompiledStatement(Compiled value) {
			this.value = value;}
		public void Assign(ref Map context) {
			AssignImplementation(ref context, value.Evaluate(context));}
		public abstract void AssignImplementation(ref Map context, Map value);
		public readonly Compiled value;}
	public abstract class Statement {
		bool preEvaluated = false;
		bool currentEvaluated = false;
		private Structure pre;
		//private Map pre;
		private Structure current;
		//private Map current;
		public Map PreMap() {
			Structure s=Pre();
			if(s!=null) {
				return ((LiteralStructure)s).Literal;
			}
			else {
				return null;
			}
		}
		public virtual Structure Pre() {
			if (!preEvaluated) {
				if (Previous == null) {
					pre = new LiteralStructure(new Map());
				}
				else {
					pre = Previous.Current();}}
			preEvaluated = true;
			return pre;
		}
		public Map CurrentMap() {
			Structure s=Current();
			return s!=null?((LiteralStructure)s).Literal:null;
		}
		public Structure Current() {
			if (!currentEvaluated) {
				Structure pre = Pre();
				//Map pre = Pre();
				if (pre != null) {
					current = CurrentImplementation(pre);}
				else {
					current = null;}}
			currentEvaluated = true;
			return current;}
		public Statement Next {
			get {
				if (program == null || Index >= program.statementList.Count - 1) {
					return null;}
				else {
					return program.statementList[Index + 1];}}}
		public virtual bool DoesNotAddKey(Map key) {
			return true;}
		public bool NeverAddsKey(Map key) {
			Statement current = this;
			while (true) {
				current = current.Next;
				if (current == null || current is CurrentStatement) {
					break;}
				if (!current.DoesNotAddKey(key)) {
					return false;}}
			return true;}
		protected abstract Structure CurrentImplementation(Structure previous);

		public Statement Previous {
			get {
				if (Index == 0) {
					return null;}
				else {
					return program.statementList[Index - 1];}}}
		public abstract CompiledStatement Compile();
		public Program program;
		public readonly Expression value;
		public readonly int Index;
		public Statement(Program program, Expression value, int index) {
			this.program = program;
			this.Index = index;
			this.value = value;
			if (value != null) {
				value.Statement = this;}}}
	public class CompiledDiscardStatement : CompiledStatement {
		public CompiledDiscardStatement(Compiled value)
			: base(value) {}
		public override void AssignImplementation(ref Map context, Map value) {}}
	public class DiscardStatement : Statement {
		protected override Structure CurrentImplementation(Structure previous) {
			return previous;
		}
		public DiscardStatement(Program program, Expression value, int index)
			: base(program, value, index) {}
		public override CompiledStatement Compile() {
			return new CompiledDiscardStatement(value.Compile(program));}}
	public class CompiledKeyStatement : CompiledStatement {
		private Compiled key;
		public CompiledKeyStatement(Compiled key, Compiled value)
			: base(value) {
			this.key = key;}
		public override void AssignImplementation(ref Map context, Map value) {
			context[key.Evaluate(context)] = value;}}
	public class KeyStatement : Statement {
		public static bool intellisense = false;
		public override bool DoesNotAddKey(Map key) {
			Structure structure=this.key.EvaluateStructure();
			Map k;
			if(structure!=null) {
				k=((LiteralStructure)structure).Literal;
			}
			else {
				k=null;
			}
			if (k != null && k.IsConstant && !k.Equals(key)) {
				return true;}
			return false;}
		protected override Structure CurrentImplementation(Structure previous) {
			Map k=key.GetConstant();
			if (k != null) {
				Map val=value.EvaluateMapStructure();
				if (val == null) {
				    val = new Map();
					val.IsConstant=false;
				}
				// not general enough
				if (value is Search || value is Call || (intellisense && (value is Literal || value is Program))) {
					((LiteralStructure)previous).Literal[k] = val;
				}
				else {
					((LiteralStructure)previous).Literal[k] = new Map();
					((LiteralStructure)previous).Literal[k].IsConstant=false;
				}
				return previous;
			}
			return null;
		}
		public override CompiledStatement Compile() {
			Map k = key.GetConstant();
			if (k != null && k.Equals(CodeKeys.Function)) {
				if (value is Literal) {
					//((Literal)value).literal.GetExpression(program);
					if(program.statementList.Count == 1) {
						((Literal)value).literal.Compile(program);}}}
			return new CompiledKeyStatement(key.Compile(program), value.Compile(program));}
		public Expression key;
		public KeyStatement(Expression key, Expression value, Program program, int index)
			: base(program, value, index) {
			this.key = key;
			key.Statement = this;}}
	public class CompiledCurrentStatement : CompiledStatement {
		private int index;
		public CompiledCurrentStatement(Compiled value, int index)
			: base(value) {
			this.index = index;}
		public override void AssignImplementation(ref Map context, Map value) {
			// fix this
			if (index == 0) {
				Map val = value.Copy();
				context.Strategy = val.Strategy;}
			else {
				context = value.Copy();}}}
	public class CurrentStatement : Statement {
		protected override Structure CurrentImplementation(Structure previous) {
			return value.EvaluateStructure();
		}
		public override CompiledStatement Compile() {
			return new CompiledCurrentStatement(value.Compile(program), Index);}
		public CurrentStatement(Expression value, Program program, int index)
			: base(program, value, index) {}}
	public class CompiledSearchStatement : CompiledStatement {
		private Compiled key;
		public CompiledSearchStatement(Compiled key, Compiled value)
			: base(value) {
			this.key = key;}
		public override void AssignImplementation(ref Map context, Map value) {
			Map selected = context;
			Map key = this.key.Evaluate(context);
			while (!selected.ContainsKey(key)) {
				selected = selected.Scope;
				if (selected == null) {
					throw new KeyNotFound(key, key.Source.start, null);}}
			selected[key] = value;}}
	public class SearchStatement : Statement {
		protected override Structure CurrentImplementation(Structure previous) {
			return previous;
		}
		public override CompiledStatement Compile() {
			return new CompiledSearchStatement(key.Compile(program), value.Compile(program));}
		private Expression key;
		public SearchStatement(Expression key, Expression value, Program program, int index)
			: base(program, value, index) {
			this.key = key;
			key.Statement = this;}}
	public class CompiledLiteral : Compiled {
		private Map literal;
		public CompiledLiteral(Map literal, Extent source)
			: base(source) {
			this.literal = literal;}
		public override Map EvaluateImplementation(Map context) {
			return literal.Copy();}}
	public class Literal : Expression {
		public override Structure StructureImplementation() {
			return new LiteralStructure(literal);
			//return literal;
		}
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		public Map literal;
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledLiteral(literal, Source);}
		public Literal(Map code, Expression parent)
			: base(code.Source, parent) {
			if (code.Count != 0 && code.IsString) {
				this.literal = code.GetString();}
			else {
				this.literal = code;}}}
	public class CompiledRoot : Compiled {
		public CompiledRoot(Extent source)
			: base(source) {}
		public override Map EvaluateImplementation(Map selected) {
			return Gac.gac;}}
	public class Root : Expression {
		public override Structure StructureImplementation() {
			return new LiteralStructure(Gac.gac);
			//return Gac.gac;
		}
		public Root(Map code, Expression parent)
			: base(code.Source, parent) {}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledRoot(Source);}}
	public class CompiledSelect : Compiled {
		List<Compiled> subs;
		public CompiledSelect(List<Compiled> subs, Extent source)
			: base(source) {
			this.subs = subs;
			if (subs[0] == null) {}}
		public override Map EvaluateImplementation(Map context) {
			Map selected = subs[0].Evaluate(context);
			for (int i = 1; i < subs.Count; i++) {
				Map key = subs[i].Evaluate(context);
				Map value = selected.TryGetValue(key);
				if (value == null) {
					throw new KeyDoesNotExist(key, subs[i].Source.start, selected);}
				else {
					selected = value;}}
			return selected;}}
	public class Select : Expression {
		public override Structure StructureImplementation() {
			Map selected = subs[0].GetConstant();
			for (int i = 1; i < subs.Count; i++) {
				Map key = subs[i].GetConstant();
				if (selected == null || key == null || !selected.ContainsKey(key)) {
					// compilation error???
					return null;
				}
				selected = selected[key];
			}
			return new LiteralStructure(selected);
		}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledSelect(subs.ConvertAll<Compiled>(delegate(Expression e) {
				return e.Compile(null);}), Source);
		}
		private List<Expression> subs = new List<Expression>();
		public Select(Map code, Expression parent): base(code.Source, parent) {
			foreach (Map m in code.Array) {
				subs.Add(m.GetExpression(this));
			}
		}
	}
	public class Interpreter {
		public static bool profiling = false;
		static Interpreter() {
		    try {
		        Map map = Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta"));
		        map.Scope = Gac.gac;

		        LiteralExpression gac = new LiteralExpression(Gac.gac, null);

		        map[CodeKeys.Function].GetExpression(gac).Statement = new LiteralStatement(gac);
		        map[CodeKeys.Function].Compile(gac);

		        Gac.gac["library"] = map.Call(Map.Empty);
		        Gac.gac["library"].Scope = Gac.gac;

		    }
		    catch (Exception e) {
		        throw e;
			}
		}
		private static Number two=2;
		private static Number one=1;
		public static Number Fibo(Number n) {
		    if(n<two) {
		        return one;
		    }
		    else {
		        //checked{
		        return Fibo(n-one)+Fibo(n-two);
		        //}
		    }
		}
		//public static int Fibo(int n) {
		//    if(n<2) {
		//        return 1;
		//    }
		//    else {
		//        checked{
		//        return Fibo(n-1)+Fibo(n-2);
		//        }
		//    }
		//}
		[STAThread]
		public static void Main(string[] args) {
			//Number two=new Rational(2);
			//Number one =new Rational(1);
			//Number one2=new  Rational(1);
			//Number rest=one+one2;
			//bool x=two.Equals(rest);

			//Map m=new Map();
			//m[1.0]=2.0;
			//m[2.0]=3.0;
			//bool x=m.IsString;
			//string s=m.GetString();

			//Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"library.meta"));
			//Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"));
			//Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"));
			//return;

			DateTime start = DateTime.Now;
			//Console.WriteLine(Fibo(28));
			//Console.WriteLine((DateTime.Now - start).TotalSeconds);
			//return;


			if (args.Length != 0) {
				if (args[0] == "-test") {
					try {
						UseConsole();
						new MetaTest().Run();}
					catch (Exception e) {
						DebugPrint(e.ToString());}}
				else if (args[0] == "-nprof") {
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);}
				else if (args[0] == "-profile") {
					UseConsole();
					Interpreter.profiling = true;
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"game.meta"), Map.Empty);
					List<object> results = new List<object>(Map.calls.Keys);
					results.Sort(delegate(object a, object b) {
						return Map.calls[b].time.CompareTo(Map.calls[a].time);});

					foreach (object e in results) {
						Console.WriteLine(e.ToString() + "    " + Map.calls[e].time + "     " + Map.calls[e].calls);}}
				else if (args[0] == "-performance") {
					UseConsole();
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);}
				else {
					string fileName = args[0].Trim('"');
					if (File.Exists(fileName)) {
						try {
							MetaTest.Run(fileName, Map.Empty);}
						catch (Exception e) {
							Console.WriteLine(e.ToString());}
						Console.WriteLine((DateTime.Now - start).TotalSeconds);
						return;}
					else {
						Console.WriteLine("File " + fileName + " not found.");}}}
			//Console.WriteLine((DateTime.Now - start).TotalSeconds);
		}
		private static void DebugPrint(string text) {
			if (useConsole) {
				Console.WriteLine(text);
				Console.ReadLine();}
			else {
				System.Windows.Forms.MessageBox.Show(text, "Meta exception");}}
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();

		public static bool useConsole = false;
		public static void UseConsole() {
			if (!useConsole) {
				AllocConsole();
				Console.SetBufferSize(80, 1000);}
			useConsole = true;}
		public static string InstallationPath {
			get {
				return @"D:\Meta\0.2\";}}}
	public class Transform {
		public static object ToDotNet(Map meta, Type target) {
			object dotNet;
			if (!TryToDotNet(meta, target, out dotNet)) {
				TryToDotNet(meta, target, out dotNet);
				throw new ApplicationException("Cannot convert " + Serialization.Serialize(meta) + " to " + target.ToString() + ".");}
			return dotNet;}
		public static Delegate CreateDelegateFromCode(Type delegateType, Map code) {
			MethodInfo invoke = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters) {
				arguments.Add(parameter.ParameterType);}
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
				il.Emit(OpCodes.Stelem_Ref);}
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Call, typeof(MetaDelegate).GetMethod("Call"));

			if (invoke.ReturnType == typeof(void)) {
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);}
			else {
				il.Emit(OpCodes.Castclass, invoke.ReturnType);
				il.Emit(OpCodes.Ret);}
			return (Delegate)method.CreateDelegate(delegateType, new MetaDelegate(code, invoke.ReturnType));}
		public class MetaDelegate {
			private Map callable;
			private Type returnType;
			public MetaDelegate(Map callable, Type returnType) {
				this.callable = callable;
				this.returnType = returnType;}
			public object Call(object[] arguments) {
				Map arg = new Map();
				Map pos = this.callable;
				foreach (object argument in arguments) {
					pos = pos.Call(Transform.ToMeta(argument));}
				if (returnType != typeof(void)) {
					return Meta.Transform.ToDotNet(pos, this.returnType);}
				else {
					return null;}}}
		public static bool TryToDotNet(Map meta, Type target, out object dotNet) {
			try {
				dotNet = null;
				if (target.Equals(typeof(Map))) {
					dotNet = meta;}
				else {
					TypeCode typeCode = Type.GetTypeCode(target);
					if (typeCode == TypeCode.Object) {
						if (target == typeof(Number) && meta.IsNumber) {
							dotNet = meta.GetNumber();
						}
						if (dotNet == null && target == typeof(Type) && meta.Strategy is TypeMap) {
							dotNet = ((TypeMap)meta.Strategy).Type;
						}
						// remove?
						else if (meta.Strategy is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta.Strategy).Type)) {
							dotNet = ((ObjectMap)meta.Strategy).Object;
						}
						else if (target.IsAssignableFrom(meta.GetType())) {
							dotNet = meta;
						}
						else if (target.IsArray) {
							ArrayList list = new ArrayList();
							bool converted = true;
							Type elementType = target.GetElementType();
							foreach (Map m in meta.Array) {
								object o;
								if (Transform.TryToDotNet(m, elementType, out o)) {
									list.Add(o);}
								else {
									converted = false;
									break;}}
							if (converted) {
								dotNet = list.ToArray(elementType);}}
						else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
						   && meta.ContainsKey(CodeKeys.Function)) {
							dotNet = CreateDelegateFromCode(target, meta);}}
					else if (target.IsEnum) {
						dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());}
					else if (meta.Strategy is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta.Strategy).Type)) {
						dotNet = ((ObjectMap)meta.Strategy).Object;}
					else {
						switch (typeCode) {
							case TypeCode.Boolean:
								if (meta.IsNumber && (meta.GetNumber().GetInt32() == 1 || meta.GetNumber().GetInt32() == 0)) {
									dotNet = Convert.ToBoolean(meta.GetNumber().GetInt32());}
								break;
							case TypeCode.Byte:
								if (IsIntegerInRange(meta, Byte.MinValue, Byte.MaxValue)) {
									dotNet = Convert.ToByte(meta.GetNumber().GetInt32());}
								break;
							case TypeCode.Char:
								if (IsIntegerInRange(meta, Char.MinValue, Char.MaxValue)) {
									dotNet = Convert.ToChar(meta.GetNumber().GetInt32());}
								break;
							// unlogical
							case TypeCode.DateTime:
								dotNet = null;
								break;
							case TypeCode.DBNull:
								dotNet = null;
								break;
							case TypeCode.Decimal:
								if (IsIntegerInRange(meta, decimal.MinValue, decimal.MaxValue)) {
									dotNet = (decimal)(meta.GetNumber().GetInt64());}
								break;
							case TypeCode.Double:
								if (IsIntegerInRange(meta, double.MinValue, double.MaxValue)) {
									// fix this
									dotNet = (double)(meta.GetNumber().GetInt64());
									//dotNet = (double)(meta.GetNumber().GetDouble());
								}
								break;
							case TypeCode.Int16:
								if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue)) {
									dotNet = Convert.ToInt16(meta.GetNumber().GetRealInt64());
									//dotNet = Convert.ToInt16(meta.GetNumber().GetInt64());
								}
								break;
							case TypeCode.Int32:
								if (IsIntegerInRange(meta, Int32.MinValue, Int32.MaxValue)) {
									dotNet = meta.GetNumber().GetInt32();}
								break;
							case TypeCode.Int64:
								if (IsIntegerInRange(meta, new Rational(Int64.MinValue), new Rational(Int64.MaxValue))) {
								//if (IsIntegerInRange(meta, new Rational(Int64.MinValue), new Rational(Int64.MaxValue))) {
									dotNet = Convert.ToInt64(meta.GetNumber().GetInt64());}
								break;
							case TypeCode.SByte:
								if (IsIntegerInRange(meta, SByte.MinValue, SByte.MaxValue)) {
									dotNet = Convert.ToSByte(meta.GetNumber().GetInt64());}
								break;
							case TypeCode.Single:
								if (IsIntegerInRange(meta, Single.MinValue, Single.MaxValue)) {
									// wrong
									dotNet = (float)meta.GetNumber().GetInt64();
									//dotNet = (float)meta.GetNumber().GetDouble();
								}
								break;
							case TypeCode.String:
								if (meta.IsString) {
									dotNet = meta.GetString();}
								break;
							case TypeCode.UInt16:
								if (IsIntegerInRange(meta, UInt16.MinValue, UInt16.MaxValue)) {
									dotNet = Convert.ToUInt16(meta.GetNumber().GetInt64());}
								break;
							case TypeCode.UInt32:
								if (IsIntegerInRange(meta, new Rational(UInt32.MinValue), new Rational(UInt32.MaxValue))) {
									dotNet = Convert.ToUInt32(meta.GetNumber().GetInt64());}
								break;
							case TypeCode.UInt64:
								if (IsIntegerInRange(meta, new Rational(UInt64.MinValue), new Rational(UInt64.MaxValue))) {
									dotNet = Convert.ToUInt64(meta.GetNumber().GetInt64());}
								break;
							default:
								throw new ApplicationException("not implemented");}}}
				return dotNet != null;}
			catch (Exception e) {
				throw e;}}
		public static bool IsIntegerInRange(Map meta, Number minValue, Number maxValue) {
			return meta.IsNumber && meta.GetNumber() >= minValue && meta.GetNumber() <= maxValue;
		}
		//public static bool IsIntegerInRange(Map meta, Number minValue, Number maxValue) {
		//    return meta.IsNumber && meta.GetNumber() >= minValue && meta.GetNumber() <= maxValue;}
		public static Map ToMeta(object dotNet) {
			if (dotNet == null) {
				return Map.Empty;}
			else {
				Type type = dotNet.GetType();
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Boolean:
						return (Boolean)dotNet;
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
						return (Decimal)dotNet;
					case TypeCode.Double:
						return (Double)dotNet;
					case TypeCode.Int16:
						return (Int16)dotNet;
					case TypeCode.Int32:
						return (Int32)dotNet;
					case TypeCode.Int64:
						return (Int64)dotNet;
					case TypeCode.DateTime:
						return new Map(dotNet);
					case TypeCode.DBNull:
						return new Map(dotNet);
					case TypeCode.Object:
						if (type == typeof(Number)) {
							return (Number)dotNet;
						}
						else if(type==typeof(Rational)) {
							return (Rational)dotNet;
						}
						else if (type == typeof(Map)) {
							return (Map)dotNet;}
						else {
							return new Map(dotNet);}
					default:
						throw new ApplicationException("Cannot convert object.");}}}}

	public delegate Map CallDelegate(Map argument);

	public class MethodExpression:Expression {
		private Method method;
		public MethodExpression(Method method,Expression parent):base(null,parent) {
		}
		public override Structure StructureImplementation() {
			return null;
		}
		public override Compiled CompileImplementation(Expression parent) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class TypeExpression:Expression {
		public TypeExpression(Type type,Expression parent):base(null,parent) {
		}
		public override Structure StructureImplementation() {
			return null;
		}
		public override Compiled CompileImplementation(Expression parent) {
			return null;
		}
	}

	public class Method : MapStrategy {
		public override bool IsNormal {
			get {
				return false;}}
		public override int GetHashCode() {
			return method.GetHashCode();}
		public override bool Equals(object obj) {
			Method method = obj as Method;
			if (method != null) {
				return this.method.Equals(method.method);}
			return false;}
		public override object UniqueKey {
			get {
				return method;}}
		public override int GetArrayCount() {
			return 0;}
		public override bool ContainsKey(Map key) {
			return false;}
		public override IEnumerable<Map> Keys {
			get {
				yield break;}}
		public override void Set(Map key, Map val, Map parent) {
			throw new Exception("The method or operation is not implemented.");}
		public override Map Get(Map key) {
			return null;}
		public Method(MethodBase method, object obj, Type type, Dictionary<Map, Map> overloads)
			: this(method, obj, type) {}
		public override Map CopyData() {
			return new Map(this);}
		public MethodBase method;
		protected object obj;
		protected Type type;
		public Method(MethodBase method, object obj, Type type) {
			this.method = method;
			this.obj = obj;
			this.type = type;
			this.parameters = method.GetParameters();}
		public override bool IsString {
			get {
				return false;
			}
		}
		public override bool IsNumber {
			get {
				return false;
			}
		}
		public ParameterInfo[] parameters;
		//public Map CallImplementation(Map argument, Map parent) {
		//    return DecideCall(argument, new List<object>());
		//}
		public override Map CallImplementation(Map argument, Map parent) {
		    return DecideCall(argument, new List<object>());
		}
		private Map DecideCall(Map argument, List<object> oldArguments) {
			List<object> arguments = new List<object>(oldArguments);
			if (parameters.Length != 0) {
				object arg;
				if (!Transform.TryToDotNet(argument, parameters[arguments.Count].ParameterType, out arg)) {
					throw new Exception("Could not convert argument " + Meta.Serialization.Serialize(argument) + "\n to " + parameters[arguments.Count].ParameterType.ToString());}
				else {
					arguments.Add(arg);
				}
			}
			if (arguments.Count >= parameters.Length) {
				return Invoke(argument, arguments.ToArray());
			}
			else {
				CallDelegate call = new CallDelegate(delegate(Map map) {
					return DecideCall(map, arguments);});
				return new Map(new Method(invokeMethod, call, typeof(CallDelegate)));
			}
		}
		MethodInfo invokeMethod = typeof(CallDelegate).GetMethod("Invoke");
		private Map Invoke(Map argument, object[] arguments) {
			try {
				object result;
				if (method is ConstructorInfo) {
					result = ((ConstructorInfo)method).Invoke(arguments);}
				else {
					result = method.Invoke(obj, arguments);}
				return Transform.ToMeta(result);
			}
			catch (Exception e) {
				if (e.InnerException != null) {
					throw e.InnerException;
				}
				else {
					throw new ApplicationException("implementation exception: " + e.InnerException.ToString() + e.StackTrace, e.InnerException);
				}
			}
		}
	}
	public class TypeMap : DotNetMap {
		private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		protected override BindingFlags BindingFlags {
			get {
				return bindingFlags;}}
		public static MemberCache cache = new MemberCache(bindingFlags);
		protected override MemberCache MemberCache {
			get {
				return cache;}}
		public override string ToString() {
			return Type.ToString();}
		protected override object GlobalKey {
			get {
				return this;}}
		public TypeMap(Type targetType)
			: base(null, targetType) {}
		public override bool ContainsKey(Map key) {
			return Get(key) != null;}
		public override Map Get(Map key) {
			if (Type.IsGenericTypeDefinition && key.Strategy is TypeMap) {
				List<Type> types = new List<Type>();
				if (Type.GetGenericArguments().Length == 1) {
					types.Add(((TypeMap)key.Strategy).Type);}
				else {
					foreach (Map map in key.Array) {
						types.Add(((TypeMap)map.Strategy).Type);}}
				return new Map(new TypeMap(Type.MakeGenericType(types.ToArray())));}
			else if (Type == typeof(Array) && key.Strategy is TypeMap) {
				return new Map(new TypeMap(((TypeMap)key.Strategy).Type.MakeArrayType()));}
			else if (base.Get(key) != null) {
				return base.Get(key);}
			else {
				Map value;
				Data.TryGetValue(key, out value);
				return value;}}
		private Dictionary<Map, Map> data;
		private Dictionary<Map, Map> Data {
			get {
				if (data == null) {
					data = new Dictionary<Map, Map>();
					foreach (ConstructorInfo constructor in Type.GetConstructors()) {
						string name = GetConstructorName(constructor);
						data[name] = new Map(new Method(constructor, this.Object, Type));}}
				return data;}}
		public static string GetConstructorName(ConstructorInfo constructor) {
			string name = constructor.DeclaringType.Name;
			foreach (ParameterInfo parameter in constructor.GetParameters()) {
				name += "_" + parameter.ParameterType.Name;}
			return name;}
		public override Map CopyData() {
			return new Map(new TypeMap(this.Type));}
		private Map constructor;
		public Map Constructor {
			get {
				if (constructor == null) {
					ConstructorInfo method = Type.GetConstructor(new Type[] {});
					if (method == null) {
						throw new Exception("Default constructor for " + Type + " not found.");}
					constructor = new Map(new Method(method, Object, Type));}
				return constructor;}}
		public override Map CallImplementation(Map argument, Map parent) {
			return Library.With(Constructor.Call(Map.Empty), argument);}}

	public class ObjectMap : DotNetMap {
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
		protected override BindingFlags BindingFlags {
			get {
				return bindingFlags;}}
		public static MemberCache cache = new MemberCache(bindingFlags);
		protected override MemberCache MemberCache {
			get {
				return cache;}}
		protected override object GlobalKey {
			get {
				return Object;}}
		public override Map CallImplementation(Map arg, Map parent) {
			if (this.Type.IsSubclassOf(typeof(Delegate))) {
				return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg, parent);}
			else {
				return base.Call(arg, parent);}}
		public override int GetHashCode() {
			return Object.GetHashCode();}
		public override bool Equals(object o) {
			return o is ObjectMap && Object.Equals(((ObjectMap)o).Object);}
		public ObjectMap(string text)
			: this(text, text.GetType()) {}
		public ObjectMap(Map target)
			: this(target, target.GetType()) {}
		public ObjectMap(object target, Type type)
			: base(target, type) {}
		public ObjectMap(object target)
			: base(target, target.GetType()) {}
		public override string ToString() {
			return Object.ToString();}
		public override Map CopyData() {
			return new Map(Object);}}

	public class EmptyStrategy : MapStrategy {
		public override bool ContainsKey(Map key) {
			return false;}
		public override void Append(Map map, Map parent) {
			ListStrategy list = new ListStrategy();
			list.Append(map, parent);
			parent.Strategy = list;}
		public static EmptyStrategy empty = new EmptyStrategy();
		private EmptyStrategy() {}
		public override bool IsNumber {
			get {
				return true;}}
		public override int Count {
			get {
				return 0;}}
		public override int GetArrayCount() {
			return 0;}
		public override int GetHashCode() {
			return 0.GetHashCode();}
		public override bool Equals(object obj) {
			return ((MapStrategy)obj).Count == 0;}
		public override IEnumerable<Map> Keys {
			get {
				yield break;}}
		public override Map CopyData() {
			return new Map(this);}
		public override MapStrategy DeepCopy(Map key, Map value, Map map) {
			if (key.IsNumber) {
				if (key.Count == 0 && value.IsNumber) {
					NumberStrategy number = new NumberStrategy(0);
					number.Set(key, value, map);
					return number;}
				else {
					if (key.Equals(new Map(1))) {
						ListStrategy list = new ListStrategy();
						list.Append(value, map);
						return list;}}}
			DictionaryStrategy dictionary = new DictionaryStrategy();
			dictionary.Set(key, value, map);
			return dictionary;}
		public override void Set(Map key, Map val, Map map) {
			map.Strategy = DeepCopy(key, val, map);}
		public override Map Get(Map key) {
			return null;}}

	public class NumberStrategy : MapStrategy {
		public override int GetArrayCount() {
			return 0;
		}
		public override bool Equals(object obj) {
			MapStrategy strategy = obj as MapStrategy;
			if (strategy != null) {
				if (strategy.IsNumber && strategy.GetNumber().Equals(number)) {
					return true;}
				else {
					return base.Equals(strategy);}}
			return false;}
		private Number number;
		public NumberStrategy(Number number) {
			this.number = number;}
		public override Map Get(Map key) {
			if (ContainsKey(key)) {
				if (key.Equals(Map.Empty)) {
					return number - 1;}
				else if (key.Equals(NumberKeys.Negative)) {
					return Map.Empty;}
				else if (key.Equals(NumberKeys.Denominator)) {
					return new Map(new Rational(number.Denominator));}
				else {
					throw new ApplicationException("Error.");}}
			else {
				return null;
			}
		}
		public override void Set(Map key, Map value, Map map) {
			if (key.Equals(Map.Empty) && value.IsNumber) {
				this.number = value.GetNumber() + 1;}
			else if (key.Equals(NumberKeys.Negative) && value.Equals(Map.Empty) && number != 0) {
				if (number > 0) {
					number = 0 - number;}}
			else if (key.Equals(NumberKeys.Denominator) && value.IsNumber) {
				this.number = new Rational(number.Numerator, value.GetNumber().GetInt32());}
			else {
				Panic(key, value, new DictionaryStrategy(), map);}}
		public override IEnumerable<Map> Keys {
			get {
				if (number != 0) {
					yield return Map.Empty;}
				if (number < 0) {
					yield return NumberKeys.Negative;}
				if (number.Denominator != 1.0d) {
					yield return NumberKeys.Denominator;}}}
		public override Map CopyData() {
			return new Map(new NumberStrategy(number));}
		public override bool IsNumber {
			get {
				return true;}}
		public override Number GetNumber() {
			return number;}
		public override int GetHashCode() {
			return (int)(number.Numerator % int.MaxValue);}}

	public class StringStrategy : MapStrategy {
		private string text;
		public StringStrategy(string text) {
			this.text = text;}
		public override int GetHashCode() {
			return text.Length;}
		public override bool Equals(object obj) {
			if (obj is StringStrategy) {
				return ((StringStrategy)obj).text == text;}
			else {
				return base.Equals(obj);}}
		public override void Set(Map key, Map val, Map map) {
			MapStrategy strategy;
			if (key.Equals(new Map(Count + 1))) {
				strategy = new ListStrategy();}
			else {
				strategy = new DictionaryStrategy();}
			Panic(key, val, strategy, map);}
		public override int Count {
			get {
				return text.Length;}}
		public override bool IsNumber {
			get {
				return text.Length == 0;}}
		public override bool IsString {
			get {
				return true;}}
		public override string GetString() {
			return text;}
		public override Map Get(Map key) {
			if (key.IsNumber) {
				Number number = key.GetNumber();
				if (number.IsNatural && number > 0 && number <= Count) {
					return text[number.GetInt32() - 1];}
				else {
					return null;}}
			else {
				return null;}}
		public override bool ContainsKey(Map key) {
			if (key.IsNumber) {
				Number number = key.GetNumber();
				if (number.IsNatural) {
					return number > 0 && number <= text.Length;}
				else {
					return false;}}
			else {
				return false;}}
		public override int GetArrayCount() {
			return text.Length;}
		public override Map CopyData() {
			return new Map(this);}
		public override IEnumerable<Map> Keys {
			get {
				for (int i = 1; i <= text.Length; i++) {
					yield return i;}}}}
	public class ListStrategy : MapStrategy {
		public override MapStrategy DeepCopy(Map key, Map value, Map map) {
			if (key.Equals(new Map(Count + 1))) {
				List<Map> newList = new List<Map>(this.list);
				newList.Add(value);
				return new ListStrategy(newList);}
			else {
				return base.DeepCopy(key, value, map);}}
		public override Map CopyData() {
			return new Map(new CloneStrategy(this));}
		public override void Append(Map map, Map parent) {
			list.Add(map);}
		private List<Map> list;
		public ListStrategy()
			: this(5) {}
		public ListStrategy(List<Map> list) {
			this.list = list;}
		public ListStrategy(int capacity) {
			this.list = new List<Map>(capacity);}
		public ListStrategy(ListStrategy original) {
			this.list = new List<Map>(original.list);}
		public override Map Get(Map key) {
			Map value = null;
			if (key.IsNumber) {
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count) {
					value = list[integer - 1];}}
			return value;}
		public override void Set(Map key, Map val, Map map) {
			if (key.IsNumber) {
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count) {
					list[integer - 1] = val;}
				else if (integer == list.Count + 1) {
					list.Add(val);}
				else {
					Panic(key, val, new DictionaryStrategy(), map);}}
			else {
				Panic(key, val, new DictionaryStrategy(), map);}}
		public override int Count {
			get {
				return list.Count;}}
		public override IEnumerable<Map> Array {
			get {
				return this.list;}}
		public override int GetArrayCount() {
			return this.list.Count;}
		public override bool ContainsKey(Map key) {
			if (key.IsNumber) {
				Number integer = key.GetNumber();
				return integer >= 1 && integer <= list.Count;}
			else {
				return false;}}
		public override IEnumerable<Map> Keys {
			get {
				for (int i = 1; i <= list.Count; i++) {
					yield return i;}}}}
	public class DictionaryStrategy : MapStrategy {
		public override Map CopyData() {
			return new Map(new CloneStrategy(this));}
		public override bool IsNumber {
			get {
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);}}
		public override int GetArrayCount() {
			int i = 1;
			while (this.ContainsKey(i)) {
				i++;}
			return i - 1;}
		private Dictionary<Map, Map> dictionary;
		public DictionaryStrategy()
			: this(2) {}
		public DictionaryStrategy(int Count) {
			this.dictionary = new Dictionary<Map, Map>(Count);}
		public DictionaryStrategy(Dictionary<Map, Map> data) {
			this.dictionary = data;}
		public override Map Get(Map key) {
			Map val;
			dictionary.TryGetValue(key, out val);
			return val;}
		public override void Set(Map key, Map value, Map map) {
			dictionary[key] = value;}
		public override bool ContainsKey(Map key) {
			return dictionary.ContainsKey(key);}
		public override IEnumerable<Map> Keys {
			get {
				return dictionary.Keys;}}
		public override int Count {
			get {
				return dictionary.Count;}}}
	public class CloneStrategy : MapStrategy {
		public override bool IsNormal {
			get {
				return original.IsNormal;}}
		public override int GetArrayCount() {
			return original.GetArrayCount();}
		private MapStrategy original;
		public CloneStrategy(MapStrategy original) {
			this.original = original;}
		public override IEnumerable<Map> Array {
			get {
				return original.Array;}}
		public override bool ContainsKey(Map key) {
			return original.ContainsKey(key);}
		public override int Count {
			get {
				return original.Count;}}
		public override Map CopyData() {
			MapStrategy clone = new CloneStrategy(this.original);
			return new Map(clone);}
		public override bool Equals(object obj) {
			return obj.Equals(original);}
		public override int GetHashCode() {
			return original.GetHashCode();}
		public override Number GetNumber() {
			return original.GetNumber();}
		public override string GetString() {
			return original.GetString();}
		public override bool IsNumber {
			get {
				return original.IsNumber;}}
		public override bool IsString {
			get {
				return original.IsString;}}
		public override IEnumerable<Map> Keys {
			get {
				return original.Keys;}}
		public override Map Get(Map key) {
			return original.Get(key);}
		public override void Set(Map key, Map value, Map map) {
			map.Strategy = original.DeepCopy(key, value, map);}}
	public class Profile {
		public double time;
		public int calls;
		public int recursive;}
	public abstract class MapStrategy {
		public virtual bool IsNormal {
			get {
				return true;}}
		public override int GetHashCode() {
			if (IsNumber) {
				return (int)(GetNumber().Numerator % int.MaxValue);}
			else {
				return Count;}}
		public override bool Equals(object obj) {
			MapStrategy strategy = obj as MapStrategy;
			if (strategy == null || strategy.Count != this.Count) {
				return false;}
			else {
				bool isEqual = true;
				foreach (Map key in this.Keys) {
					Map otherValue = strategy.Get(key);
					Map thisValue = Get(key);
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue)) {
						isEqual = false;}}
				return isEqual;}}
		[DllImport("Kernel32.dll")]
		protected static extern bool QueryPerformanceCounter(
			out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		protected static extern bool QueryPerformanceFrequency(
			out long lpFrequency);

		private static long freq;

		static MapStrategy() {
			if (QueryPerformanceFrequency(out freq) == false) {
				throw new ApplicationException();}}
		public virtual string Serialize(Map parent) {
			return parent.SerializeDefault();}
		public virtual object UniqueKey {
			get {
				return Get(CodeKeys.Function).Source;}}
		public virtual Map CallImplementation(Map argument, Map parent) {
		    Map function=Get(CodeKeys.Function);
		    if (function!=null) {
		        if (function.expression!=null && function.expression.compiled!= null) {
		            return function.expression.compiled.Evaluate(parent);
		        }
		        else {
		            return function.GetExpression(null).Compile(null).Evaluate(parent);}}
		    else {
		        throw new ApplicationException("Map is not a function: " + Meta.Serialization.Serialize(parent));
		    }
		}


		//public virtual Map CallImplementation(Map argument, Map parent) {
		//    Map function=Get(CodeKeys.Function);
		//    if (function!=null) {
		//        if (function.expression!=null && function.expression.compiled!= null) {
		//            return function.expression.compiled.Evaluate(parent);
		//        }
		//        else {
		//            return function.GetExpression(null).Compile(null).Evaluate(parent);}}
		//    else {
		//        throw new ApplicationException("Map is not a function: " + Meta.Serialization.Serialize(parent));
		//    }
		//}

		public Map Call(Map argument, Map parent) {
			long start = 0;
			if (Interpreter.profiling && UniqueKey != null) {
				QueryPerformanceCounter(out start);
				if (!Map.calls.ContainsKey(UniqueKey)) {
					Map.calls[UniqueKey] = new Profile();}
				Map.calls[UniqueKey].calls++;
				Map.calls[UniqueKey].recursive++;}

			//Map result = parent.GetExpression().compiled.Evaluate(parent);//argument, parent);
			Map result = CallImplementation(argument, parent);
			//Map result = CallImplementation(argument, parent);

			if (Interpreter.profiling) {
				if (UniqueKey != null) {
					long stop;
					QueryPerformanceCounter(out stop);
					double duration = (double)(stop - start) / (double)freq;
					Map.calls[UniqueKey].recursive--;
					if (Map.calls[UniqueKey].recursive == 0) {
						Map.calls[UniqueKey].time += duration;}}}
			return result;
		}
		public virtual void Append(Map map, Map parent) {
			this.Set(GetArrayCount() + 1, map, parent);}
		public abstract void Set(Map key, Map val, Map map);
		public abstract Map Get(Map key);
		public virtual bool ContainsKey(Map key) {
			foreach (Map k in Keys) {
				if (k.Equals(key)) {
					return true;}}
			return false;}
		public abstract int GetArrayCount();
		public virtual Map CopyData() {
			Map map = new Map();
			foreach (Map key in Keys) {
				map[key] = this.Get(key).Copy();}
			return map;}
		protected virtual void Panic(Map key, Map val, MapStrategy strategy, Map map) {
			map.Strategy = strategy;
			foreach (Map k in Keys) {
				strategy.Set(k, Get(k).Copy(), map);}
			map.Strategy.Set(key, val, map);}
		public virtual MapStrategy DeepCopy(Map key, Map value, Map map) {
			MapStrategy strategy = new DictionaryStrategy();
			foreach (Map k in Keys) {
				strategy.Set(k, Get(k).Copy(), map);}
			strategy.Set(key, value, map);
			return strategy;}
		public virtual bool IsNumber {
			get {
				return GetIsNumberDefault();}}
		public bool GetIsNumberDefault() {
			return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);}
		public virtual bool IsString {
			get {
				return IsStringDefault;}}
		public bool IsStringDefault {
			get {
				if (GetArrayCount() != Count) {
					return false;}
				foreach (Map m in Array) {
					if (!Transform.IsIntegerInRange(m, (int)Char.MinValue, (int)Char.MaxValue)) {
						return false;}}
				return true;}}
		public string GetStringDefault() {
			StringBuilder text = new StringBuilder("");
			foreach (Map key in Keys) {
				text.Append(Convert.ToChar(this.Get(key).GetNumber().GetInt32()));}
			return text.ToString();}
		public virtual string GetString() {
			return GetStringDefault();}
		public virtual Number GetNumber() {
			return GetNumberDefault();}
		public Number GetNumberDefault() {
			Number number;
			if (Count == 0) {
				number = 0;}
			else if (this.Count == 1 && this.ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber) {
				number = 1 + this.Get(Map.Empty).GetNumber();}
			else {
				throw new ApplicationException("Map is not an integer");}
			return number;}
		public abstract IEnumerable<Map> Keys {
			get;}
		public virtual IEnumerable<Map> Array {
			get {
				for (int i = 1; this.ContainsKey(i); i++) {
					yield return Get(i);}}}
		public virtual int Count {
			get {
				int count = 0;
				foreach (Map key in Keys) {
					count++;}
				return count;}}}
	public abstract class Member {
		public abstract void Set(object obj, Map value);
		public abstract Map Get(object obj);}
	public class TypeMember : Member {
		public override void Set(object obj, Map value) {
			throw new Exception("The method or operation is not implemented.");}
		private Type type;
		public TypeMember(Type type) {
			this.type = type;}
		public override Map Get(object obj) {
			return new Map(new TypeMap(type));}}
	public class FieldMember : Member {
		private FieldInfo field;
		public FieldMember(FieldInfo field) {
			this.field = field;}
		public override void Set(object obj, Map value) {
			field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));}
		public override Map Get(object obj) {
			return Transform.ToMeta(field.GetValue(obj));}}
	public class MethodMember : Member {
		private MethodBase method;
		public MethodMember(MethodInfo method) {
			this.method = method;}
		public override void Set(object obj, Map value) {
			throw new Exception("The method or operation is not implemented.");}
		public override Map Get(object obj) {
			return new Map(new Method(method, obj, method.DeclaringType));}}
	public class MemberCache {
		private BindingFlags bindingFlags;
		public MemberCache(BindingFlags bindingFlags) {
			this.bindingFlags = bindingFlags;}
		public Dictionary<Map, Member> GetMembers(Type type) {
			if (!cache.ContainsKey(type)) {
				Dictionary<Map, Member> data = new Dictionary<Map, Member>();
				foreach (MemberInfo member in type.GetMembers(bindingFlags)) {
					MethodInfo method = member as MethodInfo;
					if (method != null) {
						string name = TypeMap.GetMethodName(method);
						data[name] = new MethodMember(method);}
					FieldInfo field = member as FieldInfo;
					if (field != null) {
						data[field.Name] = new FieldMember(field);}
					Type t = member as Type;
					if (t != null) {
						data[t.Name] = new TypeMember(t);}}
				cache[type] = data;}
			else {}
			return cache[type];}
		private Dictionary<Type, Dictionary<Map, Member>> cache = new Dictionary<Type, Dictionary<Map, Member>>();}
	public abstract class DotNetMap : MapStrategy {
		public override bool IsNormal {
			get {
				return false;}}
		public override int GetHashCode() {
			if (obj != null) {
				return obj.GetHashCode();}
			else {
				return type.GetHashCode();}}
		public override bool Equals(object obj) {
			DotNetMap dotNet = obj as DotNetMap;
			if (dotNet != null) {
				return dotNet.Object == Object && dotNet.Type == Type;}
			return false;}
		public override object UniqueKey {
			get {
				return null;}}
		protected abstract BindingFlags BindingFlags {
			get;}
		public override int GetArrayCount() {
			return 0;}
		public object Object {
			get {
				return obj;}}
		public Type Type {
			get {
				return type;}}
		protected abstract object GlobalKey {
			get;}
		public static string GetMethodName(MethodInfo method) {
			string name = method.Name;
			foreach (ParameterInfo parameter in method.GetParameters()) {
				name += "_" + parameter.ParameterType.Name;}
			return name;}
		private Dictionary<Map, Member> data;
		private Dictionary<Map, Member> Members {
			get {
				if (data == null) {
					data = MemberCache.GetMembers(type);}
				return data;}}
		protected abstract MemberCache MemberCache {
			get;}
		private object obj;
		private Type type;
		public DotNetMap(object obj, Type type) {
			this.obj = obj;
			this.type = type;}
		public override Map Get(Map key) {
			if (Members.ContainsKey(key)) {
				return Members[key].Get(obj);}
			if (global.ContainsKey(GlobalKey) && global[GlobalKey].ContainsKey(key)) {
				return global[GlobalKey][key];}
			return null;}
		public override void Set(Map key, Map value, Map parent) {
			if (Members.ContainsKey(key)) {
				Members[key].Set(obj, value);}
			else {
				if (!global.ContainsKey(GlobalKey)) {
					global[GlobalKey] = new Dictionary<Map, Map>();}
				global[GlobalKey][key] = value;}}
		public static Type GetListAddFunctionType(IList list, Map value) {
			foreach (MemberInfo member in list.GetType().GetMember("Add")) {
				if (member is MethodInfo) {
					MethodInfo method = (MethodInfo)member;
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1) {
						ParameterInfo parameter = parameters[0];
						bool c = true;
						foreach (Map entry in value.Array) {
							object o;
							if (!Transform.TryToDotNet(entry, parameter.ParameterType, out o)) {
								c = false;
								break;}}
						if (c) {
							return parameter.ParameterType;}}}}
			return null;}
		public static Dictionary<object, Dictionary<Map, Map>> global = new Dictionary<object, Dictionary<Map, Map>>();
		public override bool ContainsKey(Map key) {
			return Get(key) != null;}
		public override IEnumerable<Map> Keys {
			get {
				foreach (Map key in Members.Keys) {
					yield return key;}
				if (global.ContainsKey(GlobalKey)) {
					foreach (Map key in global[GlobalKey].Keys) {
						yield return key;}}}}
		public override bool IsString {
			get {
				return false;}}
		public override bool IsNumber {
			get {
				return false;}}
		public override string Serialize(Map parent) {
			if (obj != null) {
				return this.obj.ToString();}
			else {
				return this.type.ToString();}}
		public Delegate CreateEventDelegate(string name, Map code) {
			EventInfo eventInfo = type.GetEvent(name, BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static | BindingFlags.Instance);
			Delegate eventDelegate = Transform.CreateDelegateFromCode(eventInfo.EventHandlerType, code);
			return eventDelegate;}}
	public interface ISerializeEnumerableSpecial {
		string Serialize();}
	public class TestAttribute : Attribute {
		public TestAttribute()
			: this(1) {}
		public TestAttribute(int level) {
			this.level = level;}
		private int level;
		public int Level {
			get {
				return level;}}}
	[AttributeUsage(AttributeTargets.Property)]
	public class SerializeAttribute : Attribute {
		public SerializeAttribute()
			: this(1) {}
		public SerializeAttribute(int level) {
			this.level = level;}
		private int level;
		public int Level {
			get {
				return level;}}}
	public abstract class TestRunner {
		public static string TestDirectory {
			get {
				return Path.Combine(Interpreter.InstallationPath, "Test");}}
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
					successText = "succeeded";}
				else {
					successText = "failed";}
				Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
				return success;}
			public abstract object GetResult(out int level);}
		public void Run() {
			bool allTestsSucessful = true;
			foreach (Type testType in this.GetType().GetNestedTypes()) {
				if (testType.IsSubclassOf(typeof(Test))) {
					Test test = (Test)testType.GetConstructor(new Type[] {}).Invoke(null);
					if (!test.RunTest()) {
						allTestsSucessful = false;}}}
			if (!allTestsSucessful) {
				Console.ReadLine();}}
		public const char indentationChar = '\t';

		private static bool UseToStringMethod(Type type) {
			return (!type.IsValueType || type.IsPrimitive)
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();}
		private static bool UseProperty(PropertyInfo property, int level) {
			object[] attributes = property.GetCustomAttributes(typeof(SerializeAttribute), false);
			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);}
		public static void Serialize(object obj, string indent, StringBuilder builder, int level) {
			if (obj == null) {
				builder.Append(indent + "null\n");}
			else if (UseToStringMethod(obj.GetType())) {
				builder.Append(indent + "\"" + obj.ToString() + "\"" + "\n");}
			else {
				foreach (PropertyInfo property in obj.GetType().GetProperties()) {
					if (UseProperty((PropertyInfo)property, level)) {
						object val = property.GetValue(obj, null);
						builder.Append(indent + property.Name);
						if (val != null) {
							builder.Append(" (" + val.GetType().Name + ")");}
						builder.Append(":\n");
						Serialize(val, indent + indentationChar, builder, level);}}
				string specialEnumerableSerializationText;
				if (obj is ISerializeEnumerableSpecial && (specialEnumerableSerializationText = ((ISerializeEnumerableSpecial)obj).Serialize()) != null) {
					builder.Append(indent + specialEnumerableSerializationText + "\n");}
				else if (obj is System.Collections.IEnumerable) {
					foreach (object entry in (System.Collections.IEnumerable)obj) {
						builder.Append(indent + "Entry (" + entry.GetType().Name + ")\n");
						Serialize(entry, indent + indentationChar, builder, level);}}}}}
	public class Extent {
		public readonly Source start;
		public readonly Source end;
		public Extent(Source start, Source end) {
			this.start = start;
			this.end = end;}
		public override int GetHashCode() {
			return start.GetHashCode() * end.GetHashCode();}
		public override bool Equals(object obj) {
			Extent extent = obj as Extent;
			return extent != null && start.Equals(extent.start) && end.Equals(extent.end);}}
	public class Source {
		public override string ToString() {
			return FileName + ", " + "line " + Line + ", column " + Column;}
		public readonly int Line;
		public readonly int Column;
		public readonly string FileName;
		public Source(int line, int column, string fileName) {
			this.Line = line;
			this.Column = column;
			this.FileName = fileName;}
		public override int GetHashCode() {
			return Line.GetHashCode() * Column.GetHashCode() * FileName.GetHashCode();}
		public override bool Equals(object obj) {
			Source source = obj as Source;
			return source != null && Line == source.Line && Column == source.Column && FileName == source.FileName;}}
	public class Gac : MapStrategy {
		public override bool IsNormal {
			get {
				return false;}}
		public override int GetArrayCount() {
			return 0;}
		public static readonly Map gac = new Map(new Gac());
		private Gac() {
			cache["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		public static Map LoadAssembly(Assembly assembly) {
			Map val = new Map();
			foreach (Type type in assembly.GetExportedTypes()) {
				if (type.DeclaringType == null) {
					Map selected = val;
					string name;
					if (type.IsGenericTypeDefinition) {
						name = type.Name.Split('`')[0];}
					else {
						name = type.Name;}
					selected[type.Name] = new Map(new TypeMap(type));
					foreach (ConstructorInfo constructor in type.GetConstructors()) {
						if (constructor.GetParameters().Length != 0) {
							selected[TypeMap.GetConstructorName(constructor)] = new Map(new Method(constructor, null, type));}
}}}
			return val;}
		public override Map Get(Map key) {
			Map value;
			if (!cache.ContainsKey(key)) {
				if (key.IsString) {
					Assembly assembly;
					string path = Path.Combine(Interpreter.InstallationPath, key.GetString() + ".dll");
					if (File.Exists(path)) {
						assembly = Assembly.LoadFile(path);}
					else {
						assembly = Assembly.LoadWithPartialName(key.GetString());}
					if (assembly != null) {
						value = LoadAssembly(assembly);
						cache[key] = value;}
					else {
						value = null;}}
				else {
					value = null;}}
			else {
				value = cache[key];}
			return value;}
		public override void Set(Map key, Map val, Map map) {
			cache[key] = val;}
		public override Map CopyData() {
			return new Map(this);}
		public override IEnumerable<Map> Keys {
			get {
				throw new Exception("The method or operation is not implemented.");}}
		public override bool ContainsKey(Map key) {
			return Get(key) != null;}}

	public abstract class Number {
		public override bool Equals(object o) {
			if (!(o is Number)) {
				return false;}
			Number b = (Number)o;
			if(Numerator==117) 
			{
			}
			return b.Numerator == Numerator && b.Denominator == Denominator;
		}
		public override int GetHashCode() {
			Number x = new Rational(this);
			while (x > int.MaxValue) {
				x = x - int.MaxValue;}
			return x.GetInt32();
		}
		public abstract double Numerator {
			get;
		}
		public abstract double Denominator {
			get;
		}
		public abstract int GetInt32();
		public abstract long GetInt64();
		public abstract long GetRealInt64();
		public static Number operator |(Number a, Number b) {
			return Convert.ToInt32(a.Numerator) | Convert.ToInt32(b.Numerator);
		}
		public static implicit operator Number(double number) {
			return new Rational(number);
		}
		public static implicit operator Number(decimal number) {
			return new Rational((double)number);
		}
		public static implicit operator Number(int integer) {
			return new Rational((double)integer);
		}
		public static bool operator ==(Number a, Number b) {
			return !ReferenceEquals(b, null) && a.Numerator == b.Numerator && a.Denominator == b.Denominator;
		}
		public static bool operator !=(Number a, Number b) {
			return !(a == b);
		}
		public static Number operator %(Number a, Number b) {
			return Convert.ToInt32(a.Numerator) % Convert.ToInt32(b.Numerator);
		}
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

		public static Number operator +(Number a, Number b) {
			return new Rational(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a, b));
		}
		public static Number operator /(Number a, Number b) {
			return new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
		}
		public static Number operator -(Number a, Number b) {
			return new Rational(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a, b));
		}
		public static Number operator *(Number a, Number b) {
			return new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
		}
		public double Expand(Number b) {
			return Numerator * (LeastCommonMultiple(this, b) / Denominator);
		}
		public static bool operator >(Number a, Number b) {
			return a.Expand(b) > b.Expand(a);
		}
		public static bool operator <(Number a, Number b) {
			return a.Expand(b) < b.Expand(a);
		}
		public static bool operator >=(Number a, Number b) {
			return a.Expand(b) >= b.Expand(a);
		}
		public static bool operator <=(Number a, Number b) {
			return a.Expand(b) <= b.Expand(a);
		}
		public int CompareTo(Number number) {
			return GetDouble().CompareTo(number.GetDouble());
		}
		public abstract bool IsNatural {
			get;
		}
		public abstract double GetDouble();
	}
	public class Rational:Number {
		public override bool IsNatural {
			get {
				return denominator == 1.0d;
			}
		}
		private readonly double numerator;
		private readonly double denominator;
		public static Number Parse(string text) {
			try {
				string[] parts = text.Split('/');
				int numerator = Convert.ToInt32(parts[0]);
				int denominator;
				if (parts.Length > 2) {
					denominator = Convert.ToInt32(parts[2]);}
				else {
					denominator = 1;}
				return new Rational(numerator, denominator);}
			catch (Exception e) {
				return null;}
		}
		public Rational(double integer): this(integer, 1) {}
		public Rational(Number i)
			: this(i.Numerator, i.Denominator) {}
		public Rational(double numerator, double denominator) {
			double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
			if (denominator < 0) {
				numerator = -numerator;
				denominator = -denominator;
			}
			this.numerator = numerator / greatestCommonDivisor;
			this.denominator = denominator / greatestCommonDivisor;
		}
		public override double Numerator {
			get {
				return numerator;
			}
		}
		public override double Denominator {
			get {
				return denominator;
			}
		}
		public override string ToString() {
			if (denominator == 1) {
				return numerator.ToString();
			}
			else {
				return numerator.ToString() + Syntax.fraction + denominator.ToString();
			}
		}
		public Number Clone() {
			//return new Rational(numerator,denominator);
			return new Rational(this);
		}
		//public override bool Equals(object o) {
		//    if (!(o is Number)) {
		//        return false;}
		//    Number b = (Number)o;
		//    if(Numerator==117) 
		//    {
		//    }
		//    return b.Numerator == numerator && b.Denominator == denominator;
		//}
		//public override int GetHashCode() {
		//    Number x = new Rational(this);
		//    while (x > int.MaxValue) {
		//        x = x - int.MaxValue;}
		//    return x.GetInt32();
		//}
		public override double GetDouble() {
			return numerator / denominator;
		}
		public override int GetInt32() {
			return Convert.ToInt32(numerator / denominator);
		}
		public override long GetRealInt64() {
			return Convert.ToInt64(numerator / denominator);}
		public override long GetInt64() {
			return Convert.ToInt64(numerator);
		}
	}
	//public class Number {
	//    public int CompareTo(Number number) {
	//        return GetDouble().CompareTo(number.GetDouble());}
	//    public bool IsNatural {
	//        get {
	//            return denominator == 1.0d;}}
	//    private readonly double numerator;
	//    private readonly double denominator;
	//    public static Number Parse(string text) {
	//        try {
	//            string[] parts = text.Split('/');
	//            int numerator = Convert.ToInt32(parts[0]);
	//            int denominator;
	//            if (parts.Length > 2) {
	//                denominator = Convert.ToInt32(parts[2]);}
	//            else {
	//                denominator = 1;}
	//            return new Rational(numerator, denominator);}
	//        catch (Exception e) {
	//            return null;}}
	//    public Number(double integer)
	//        : this(integer, 1) {}
	//    public Number(Number i)
	//        : this(i.numerator, i.denominator) {}
	//    public Number(double numerator, double denominator) {
	//        double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
	//        if (denominator < 0) {
	//            numerator = -numerator;
	//            denominator = -denominator;}
	//        this.numerator = numerator / greatestCommonDivisor;
	//        this.denominator = denominator / greatestCommonDivisor;}
	//    public double Numerator {
	//        get {
	//            return numerator;}}
	//    public double Denominator {
	//        get {
	//            return denominator;}}
	//    public static Number operator |(Number a, Number b) {
	//        return Convert.ToInt32(a.numerator) | Convert.ToInt32(b.numerator);}
	//    public override string ToString() {
	//        if (denominator == 1) {
	//            return numerator.ToString();}
	//        else {
	//            return numerator.ToString() + Syntax.fraction + denominator.ToString();}}
	//    public Number Clone() {
	//        return new Rational(this);}
	//    public static implicit operator Number(double number) {
	//        return new Rational(number);}
	//    public static implicit operator Number(decimal number) {
	//        return new Rational((double)number);}
	//    public static implicit operator Number(int integer) {
	//        return new Rational((double)integer);}
	//    public static bool operator ==(Number a, Number b) {
	//        return !ReferenceEquals(b, null) && a.numerator == b.numerator && a.denominator == b.denominator;}
	//    public static bool operator !=(Number a, Number b) {
	//        return !(a == b);}
	//    private static double GreatestCommonDivisor(double a, double b) {
	//        if(a==b) {
	//            return a;
	//        }
	//        a = Math.Abs(a);
	//        b = Math.Abs(b);
	//        while (a != 0 && b != 0) {
	//            if (a > b) {
	//                a = a % b;
	//            }
	//            else {
	//                b = b % a;
	//            }
	//        }
	//        if (a == 0) {
	//            return b;
	//        }
	//        else {
	//            return a;
	//        }
	//    }
	//    private static double LeastCommonMultiple(Number a, Number b) {
	//        return a.denominator * b.denominator / GreatestCommonDivisor(a.denominator, b.denominator);
	//    }
	//    public static Number operator %(Number a, Number b) {
	//        return Convert.ToInt32(a.Numerator) % Convert.ToInt32(b.Numerator);
	//    }
	//    public static Number operator +(Number a, Number b) {
	//        return new Rational(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a, b));
	//    }
	//    public static Number operator /(Number a, Number b) {
	//        return new Rational(a.numerator * b.denominator, a.denominator * b.numerator);
	//    }
	//    public static Number operator -(Number a, Number b) {
	//        return new Rational(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a, b));
	//    }
	//    public static Number operator *(Number a, Number b) {
	//        return new Rational(a.numerator * b.numerator, a.denominator * b.denominator);
	//    }
	//    public double Expand(Number b) {
	//        return numerator * (LeastCommonMultiple(this, b) / denominator);
	//    }
	//    public static bool operator >(Number a, Number b) {
	//        return a.Expand(b) > b.Expand(a);
	//    }
	//    public static bool operator <(Number a, Number b) {
	//        return a.Expand(b) < b.Expand(a);
	//    }
	//    public static bool operator >=(Number a, Number b) {
	//        return a.Expand(b) >= b.Expand(a);
	//    }
	//    public static bool operator <=(Number a, Number b) {
	//        return a.Expand(b) <= b.Expand(a);
	//    }
	//    public override bool Equals(object o) {
	//        if (!(o is Number)) {
	//            return false;}
	//        Number b = (Number)o;
	//        return b.numerator == numerator && b.denominator == denominator;
	//    }
	//    public override int GetHashCode() {
	//        Number x = new Rational(this);
	//        while (x > int.MaxValue) {
	//            x = x - int.MaxValue;}
	//        return x.GetInt32();}
	//    public double GetDouble() {
	//        return numerator / denominator;}
	//    public int GetInt32() {
	//        return Convert.ToInt32(numerator / denominator);}
	//    public long GetRealInt64() {
	//        return Convert.ToInt64(numerator / denominator);}
	//    public long GetInt64() {
	//        return Convert.ToInt64(numerator);
	//    }
	//}
	public struct State{
		public override bool Equals(object obj) {
			State state=(State)obj;
			return state.indentationCount==indentationCount &&
				state.Column ==Column && state.index==index && state.Line==Line &&
				state.FileName==FileName;
		}
		public override int GetHashCode() {
			return index.GetHashCode()*Line.GetHashCode()*Column.GetHashCode()*indentationCount.GetHashCode()*FileName.GetHashCode();
		}
		public State(int index,int Line,int Column,int indentationCount,string fileName){
			this.index=index;
			this.FileName=fileName;
			this.Line=Line;
			this.Column=Column;
			this.indentationCount=indentationCount;}
		public string FileName;
		public int index;
		public int Line;
		public int Column;
		public int indentationCount;}
	public class Parser {
		public class Index {
			private State state;
			private Rule rule;
			private Parser parser;
			public Index(State state,Rule rule,Parser parser)
			{
				this.state=state;
				this.rule=rule;
				this.parser=parser;
			}
			public override int GetHashCode() {
				return state.GetHashCode()*rule.GetHashCode()*parser.GetHashCode();
			}
			public override bool Equals(object obj) {
				Index index=obj as Index;
				return index!=null && index.rule.Equals(rule) && index.parser==parser  && index.state.Equals(state);
			}
		}
		public string Text;
		public readonly string FileName;
		public Stack<int> defaultKeys = new Stack<int>();
		public State State;
		public Parser(string text, string filePath) {
			State=new State(0,1,1,-1,filePath);
			this.Text = text+Syntax.endOfFile;
			this.FileName = filePath;
			Root.precondition=delegate(Parser p) {return p.Look()==Syntax.root;};
			Program.precondition=delegate(Parser p) {return p.Look()==Syntax.programStart;};
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
						return false;}};
		}
		public static Rule Expression = new DelayedRule(delegate() {
		    return new CachedRule(new Alternatives(
		        LiteralExpression,FunctionProgram,Call,Select,
		        Search,List,Program,LastArgument));});
		public class Ignore:Rule
		{
			private Rule rule;
			public Ignore(Rule rule) {
				this.rule=rule;}
			protected override bool MatchImplementation(Parser parser, ref Map map, bool keep) {
				return rule.Match(parser,ref map,false);}
		}
		public static Rule EndOfLine = new Ignore(new Sequence(
			new ZeroOrMoreChars(new Chars(""+Syntax.space+Syntax.tab)),
			new Alternatives(Syntax.unixNewLine,Syntax.windowsNewLine)));

		//public static Rule EndOfLine = new Ignore(new Sequence(
		//    new ZeroOrMoreChars(new Chars(""+Syntax.space+Syntax.tab)),
		//    new Alternatives(Syntax.unixNewLine,Syntax.windowsNewLine)));

		public static Rule Integer = new Sequence(new CustomProduction(
		        delegate(Parser p, Map map, ref Map result) {
		            result=new Map(new Rational(double.Parse(map.GetString()),1.0));},
		        new OneOrMoreChars(new Chars(Syntax.integer))));
		public static Rule StartOfFile = new CustomRule(delegate(Parser p, ref Map map) {
			if (p.State.indentationCount == -1) {
				p.State.indentationCount++;
				return true;}
			else {return false;}});
		private static CustomRule SmallIndentation = new CustomRule(delegate(Parser p, ref Map map) {
			p.State.indentationCount++;
			return true;});
		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, ref Map map) {
			return StringRule2("".PadLeft(pa.State.indentationCount, Syntax.indentation)).Match(pa, ref map);});
		public static Rule Dedentation = new CustomRule(delegate(Parser pa, ref Map map) {
			pa.State.indentationCount--;
			return true;});


		public static StringRule StringLine=new ZeroOrMoreChars(new CharsExcept("\n\r"));
		
		public class StringIgnore:StringRule
		{
			private Rule rule;
			public StringIgnore(Rule rule) {
				this.rule=rule;
			}
			public override bool MatchString(Parser parser, ref string s) {
				Map map=null;
				s="";
				return rule.Match(parser,ref map);
			}
		}
		public class StringLoop:StringRule {
			private StringRule rule;
			public StringLoop(StringRule rule) {
				this.rule=rule;
			}
			public override bool MatchString(Parser parser, ref string s) {
				while(true){
					string result=null;
					if(rule.MatchString(parser,ref result)){
						s+=result;
					}
					else{
						break;
					}
				}
				return true;
			}
		}

		public static Rule CharacterDataExpression = new Sequence(
			Syntax.character,
			new ReferenceAssignment(new CharsExcept(Syntax.character.ToString())),
			Syntax.character);
		public static Rule String = new Sequence(
			Syntax.@string,
			new ReferenceAssignment(new Alternatives(
				new Sequence(
					new ReferenceAssignment(
						new OneOrMoreChars(new CharsExcept(""+
							Syntax.unixNewLine+
							Syntax.windowsNewLine[0]+
							Syntax.@string))),
					new Optional(Syntax.@string)),
				new Sequence(
					SmallIndentation,
					EndOfLine,
					SameIndentation,
					new ReferenceAssignment(new StringSequence(
						StringLine,
						new StringLoop(
							new StringSequence(
								new StringIgnore(EndOfLine),
								new StringIgnore(SameIndentation),
								new LiteralString("\n"),
								StringLine)))),
					EndOfLine,
					Dedentation))));
		public static Rule Number = new Sequence(
			new ReferenceAssignment(Integer),
			new Assignment(
					NumberKeys.Denominator,
					new Optional(
						new Sequence(
							Syntax.fraction,
							new ReferenceAssignment(Integer)))));
		public class StringSequence:StringRule
		{
			private StringRule[] rules;
			public StringSequence(params StringRule[] rules) {
				this.rules=rules;
			}
			public override bool MatchString(Parser parser, ref string s) {
				s="";
				State oldState=parser.State;
				foreach(StringRule rule in rules) {
					string result=null;
					if(rule.MatchString(parser, ref result)) {
						s+=result;}
					else {
						parser.State=oldState;
						return false;
					}
				}
				return true;
			}
		}

		public static Rule LookupString = new CachedRule(new StringSequence(
		    new OneChar(new CharsExcept(Syntax.lookupStringForbiddenFirst)),
		    new ZeroOrMoreChars(new CharsExcept(Syntax.lookupStringForbidden))));

		public static Rule Value = new DelayedRule(delegate {
			return new Alternatives(
				Map,
				ListMap,
				String,
				Number,
				CharacterDataExpression);});
		private static Rule LookupAnything = new Sequence('<',new ReferenceAssignment(Value));
		public static Rule Function = new Sequence(
			new Assignment(
				CodeKeys.Parameter,
				new ZeroOrMoreChars(
						new CharsExcept(""+
							Syntax.@string+
							Syntax.function+
							Syntax.indentation+
							Syntax.windowsNewLine[0]+
							Syntax.unixNewLine))),
			Syntax.function,
			new Assignment(
				CodeKeys.Expression,
				Expression),
			new Optional(EndOfLine));
		public static Rule Entry = new Alternatives(
			new Sequence(
				new Assignment(
					CodeKeys.Function,
					Function)),
			new Sequence(
				new Assignment(
					1,
					new Alternatives(
						Number,
						LookupString,
						LookupAnything)),
				'=',
				new CustomProduction(
					delegate(Parser parser, Map map, ref Map result) {
						result = new Map(result[1], map);},
					Value),
			 new Optional(EndOfLine)));
		public static Rule Map = new Sequence(
			new Optional(Syntax.programStart),
			new Alternatives(
			StartOfFile,
			new Sequence(
				EndOfLine,
				SmallIndentation)),
			new ReferenceAssignment(new PrePost(
				delegate(Parser p) {
					p.defaultKeys.Push(1);},
				new Sequence(
					new ReferenceAssignment(
						new OneOrMore(
							new Merge(
								new Sequence(
									SameIndentation,
									new ReferenceAssignment(Entry))))),
					Dedentation),
				delegate(Parser p) {
					p.defaultKeys.Pop();})));
		public static Rule File = new Ignore(new Sequence(
			new Optional(
				new Sequence('#','!',
					new ZeroOrMoreChars(new CharsExcept(Syntax.unixNewLine.ToString())),
					EndOfLine)),
			new ReferenceAssignment(Map)));
		public static Rule ComplexStuff(Map key, char start, char end, Rule separator, Rule entry, Rule first) {
			return ComplexStuff(key, start, end, separator, new Assignment(1, entry), new ReferenceAssignment(entry), first);}
		public static Rule ComplexStuff(Map key, char start, char end, Rule separator, Action firstAction, Action entryAction, Rule first) {
			return new Sequence(
				new Assignment(key, ComplexStuff(start, end, separator, firstAction, entryAction, first)));}
		public static Rule ComplexStuff(char start, char end, Rule separator, Action firstAction, Action entryAction, Rule first) {
			return new Sequence(
				first != null ? new Assignment(1, first) : null,
				start,
				new Append(
					new Alternatives(
						new Sequence(
							firstAction,
							new Append(
								new ZeroOrMore(
									new Autokey(
										new Sequence(
										separator != null ? new Match(separator) : null,
											entryAction)))),
							new Optional(end)),
					new Sequence(
						SmallIndentation,
						new ReferenceAssignment(
							new ZeroOrMore(
								new Autokey(
									new Sequence(
										new Optional(EndOfLine),
										SameIndentation,
										entryAction)))),
							new Optional(EndOfLine),
							new Optional(Dedentation)))));}
		public static Rule Call = new DelayedRule(delegate() {
			return ComplexStuff(CodeKeys.Call, Syntax.callStart, Syntax.callEnd, Syntax.callSeparator,
				new Alternatives(
					LastArgument,
					FunctionProgram,
					LiteralExpression,
					Call,
					Select,
					Search,
					List,
					Program),
				new Alternatives(
					FunctionProgram,
					LiteralExpression,
					Select,
					Search,
					List,
					Program,
					LastArgument
					));});
		public static Rule FunctionExpression = new Sequence(
			new Assignment(
				CodeKeys.Key,
				new LiteralRule(new Map(CodeKeys.Literal, CodeKeys.Function))),
			new Assignment(
				CodeKeys.Value,
				new Sequence(
					new Assignment(
						CodeKeys.Literal,
						Function))));

		private static Rule Simple(char c, Map literal) {
			return new Sequence(
				c,
				new ReferenceAssignment(new LiteralRule(literal)));}

		private static Rule EmptyMap = Simple(
			Syntax.emptyMap,
			Meta.Map.Empty);

		private static Rule Current = Simple(
			Syntax.current,
			new Map(CodeKeys.Current, Meta.Map.Empty));

		public static Rule LastArgument = Simple(
			Syntax.lastArgument,
			new Map(CodeKeys.LastArgument, new Map()));

		private static Rule Root = Simple(
			Syntax.root,
			new Map(CodeKeys.Root, Meta.Map.Empty));

		private static Rule LiteralExpression = new Sequence(
			new Assignment(CodeKeys.Literal, new Alternatives(
				Number,
				String,
				EmptyMap,
				CharacterDataExpression
			)));

		private static Rule LookupAnythingExpression = new Sequence(
			'<',
			new ReferenceAssignment(Expression),
			new Optional('>'));

		private static Rule LookupStringExpression = new Sequence(
			new Assignment(
				CodeKeys.Literal,
				LookupString));

		private static Rule Search = new CachedRule(new Sequence(
			new Assignment(
				CodeKeys.Search,
				new Alternatives(
					Prefix('!',Expression),
					new Alternatives(
						LookupStringExpression,
						LookupAnythingExpression)))));

		private static Rule Select = new CachedRule(new Sequence(
			new Assignment(
				CodeKeys.Select,
				new Sequence(
					new Assignment(1,
						new Alternatives(
							Root,
							Search,
							LiteralExpression)),
					new Append(
						new OneOrMore(new Autokey(Prefix('.',new Alternatives(
							LookupStringExpression,
							LookupAnythingExpression,
							LiteralExpression)))))))));

		public static Rule ListMap = new Sequence(
			Syntax.arrayStart,
			new ReferenceAssignment(
				new PrePost(
					delegate(Parser p) {
						p.defaultKeys.Push(1);},
					new Sequence(
			new Optional(EndOfLine),
			SmallIndentation,
			new ReferenceAssignment(
				new ZeroOrMore(
					new Autokey(
						new Sequence(
							new Optional(EndOfLine),
							SameIndentation,
							new ReferenceAssignment(Value))))),
				new Optional(EndOfLine),
				new Optional(new Alternatives(Dedentation))),
					delegate(Parser p) {
						p.defaultKeys.Pop();})));

		public static Action ListAction = new CustomProduction(
			delegate(Parser p, Map map, ref Map result) {
				result = new Map(
					CodeKeys.Key, new Map(
							CodeKeys.Literal, p.defaultKeys.Peek()),
					CodeKeys.Value, map);
				p.defaultKeys.Push(p.defaultKeys.Pop() + 1);},
			Expression);

		public static Rule List = new PrePost(
					delegate(Parser p) {
						p.defaultKeys.Push(1);},
			ComplexStuff(
				CodeKeys.Program,
				Syntax.arrayStart,
				Syntax.arrayEnd,
				Syntax.arraySeparator,
				ListAction,
				ListAction,
				null
				),
				delegate(Parser p) {
					p.defaultKeys.Pop();});

		public static Rule ComplexStatement(Rule rule, Action action) {
			return new Sequence(
				action,
				rule != null ? new Match(rule) : null,
				new Assignment(CodeKeys.Value, Expression),
				new Optional(EndOfLine));}

		public static Rule DiscardStatement = ComplexStatement(
			null,
			new Assignment(CodeKeys.Discard, new LiteralRule(new Map())));
		public static Rule CurrentStatement = ComplexStatement(
			'&',
			new Assignment(CodeKeys.Current, new LiteralRule(Meta.Map.Empty)));
		public static Rule Prefix(Rule pre,Rule rule)
		{
			return new Sequence(pre,new ReferenceAssignment(rule));
		}
		public static Rule NormalStatement = ComplexStatement(
			'=',
			new Assignment(
				CodeKeys.Key,
				new Alternatives(
					Prefix('<',Expression),
					new Sequence(
						new Assignment(
							CodeKeys.Literal,
							LookupString)),
					Expression)));

		public static Rule Statement = ComplexStatement(
			':',
			new Assignment(
				CodeKeys.Keys,
				new Alternatives(
					new Sequence(
						new Assignment(
							CodeKeys.Literal,
							LookupString)),
					Expression)));

		public static Rule AllStatements = new Alternatives(
			FunctionExpression,
			CurrentStatement,
			NormalStatement,
			Statement,
			DiscardStatement
		);

		// refactor
		public static Rule FunctionProgram = new Sequence(
			new Assignment(CodeKeys.Program,
				new Sequence(
					new Assignment(1,
						new Sequence(
							new Assignment(CodeKeys.Key, new LiteralRule(new Map(CodeKeys.Literal, CodeKeys.Function))),
							new Assignment(CodeKeys.Value, new Sequence(
								new Assignment(CodeKeys.Literal,
									new Sequence(
										new Assignment(
											CodeKeys.Parameter,
											new ZeroOrMoreChars(new CharsExcept(Syntax.lookupStringForbiddenFirst))),
										Syntax.functionProgram,
											new Assignment(CodeKeys.Expression, Expression),
										new Optional(EndOfLine))))))))));
		public static Rule Program = ComplexStuff(
			CodeKeys.Program,
			Syntax.programStart,
			Syntax.programEnd,
			Syntax.programSeparator,
			AllStatements,
			null);
		public abstract class Action {
			public static implicit operator Action(StringRule rule) {
				return new Match(rule);}
			public static implicit operator Action(string s) {
				return new Match(new Ignore(StringRule2(s)));}
			public static implicit operator Action(char c) {
				return new Match(new Ignore(new OneChar(new SingleChar(c))));}
			public static implicit operator Action(Rule rule) {
				return new Match(new Ignore(rule));}
			private Rule rule;
			protected abstract void Effect(Parser parser, Map map, ref Map result);


			public Action(Rule rule) {this.rule = rule;}
			public bool Execute(Parser parser, ref Map result,bool keep) {
				Map map=null;
				bool matched=rule.Match(parser,ref map);
				if (matched) {Effect(parser, map, ref result);}
				return matched;}}
		public class Autokey : Action {
			public Autokey(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				result.Append(map);}}
		public class Assignment : Action {
			private Map key;
			public Assignment(Map key, Rule rule): base(rule) {this.key = key;}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				if (map != null) {result[key] = map;}}}
		public class Match : Action {
			public Match(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {}}
		public class ReferenceAssignment : Action {
			public ReferenceAssignment(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				result = map;}}
		public class Append : Action {
			public Append(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				foreach (Map m in map.Array) {result.Append(m);}}}
		public class Join : Action {
			public Join(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				result = Library.Join(result, map);}}
		public class Merge : Action {
			public Merge(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				result = Library.Merge(result, map);}}
		public class CustomProduction : Action {
			private CustomActionDelegate action;
			public CustomProduction(CustomActionDelegate action, Rule rule): base(rule) {
				this.action = action;}
			protected override void Effect(Parser parser, Map map, ref Map result) {
				this.action(parser, map, ref result);}}
		public delegate void CustomActionDelegate(Parser p, Map map, ref Map result);
		public delegate bool Precondition(Parser p);
		public class CachedResult
		{
			public CachedResult(Map map,State state)
			{
				this.map=map;
				this.state=state;
			}
			public Map map;
			public State state;
		}
		public class LiteralString:StringRule {
			private string s;
			public LiteralString(string s) {
				this.s=s;
			}
			public override bool MatchString(Parser parser, ref string s) {
				s=this.s;
				return true;
			}
		}
		public class CachedRule:Rule {
		    private Rule rule;
		    public CachedRule(Rule rule) {
		        this.rule=rule;}
		    private Dictionary<State,CachedResult> cached=new Dictionary<State,CachedResult>();
		    protected override bool MatchImplementation(Parser parser, ref Map map, bool keep) {
		        CachedResult cachedResult;
		        State oldState=parser.State;
		        if(cached.TryGetValue(parser.State,out cachedResult)) {
		            map=cachedResult.map;
		            if(parser.Text.Length==parser.State.index+1) {
		                return false;
		            }
					parser.State=cachedResult.state;
		            return true;
		        }
		        if(rule.Match(parser,ref map,keep)) {
		            cached[oldState]=new CachedResult(map,parser.State);
		            return true;
		        }
		        return false;
		    }
		}
		public abstract class Rule {
			public Precondition precondition;
			public static implicit operator Rule(string s) {
				return new Ignore(StringRule2(s));}
			public static implicit operator Rule(char c) {
			    return new Ignore(new OneChar(new SingleChar(c)));}
			public bool Match(Parser parser, ref Map map) {
				return Match(parser,ref map,true);}
			public int mismatches=0;
			public int calls=0;

			public bool Match(Parser parser, ref Map map,bool keep) {
				if(precondition!=null) { if(!precondition(parser)) {return false;}}
				calls++;
				State oldState=parser.State;
				bool matched;
				Map result=null;
				matched= MatchImplementation(parser, ref result,keep);
				if (!matched) {
					mismatches++;
					parser.State=oldState;}
				else {
					if (result != null) {
						result.Source = new Extent(
							new Source(oldState.Line, oldState.Column, parser.FileName),
							new Source(parser.State.Line, parser.State.Column, parser.FileName));}}
				map=result;
				return matched;}
			protected abstract bool MatchImplementation(Parser parser, ref Map map,bool keep);}

		public class Characters : CharacterRule {
		    private string chars;
		    public Characters(params char[] characters){chars=new string(characters);}
		    protected override bool MatchCharacter(char next) {
		        return chars.IndexOf(next)!=-1;}}
		
		public abstract class CharacterRule : Rule {
		    public static int calls;
		    protected abstract bool MatchCharacter(char c);
		    protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
		        char character = parser.Look();
		        calls++;
		        if (MatchCharacter(character)) {
		            parser.State.index++;
		            parser.State.Column++;
		            if (character.Equals(Syntax.unixNewLine)) {
		                parser.State.Line++;
		                parser.State.Column = 1;}
		            map=keep?new Map(character):null;
		            return true;}
		        else {
		            map=null;
		            return false;}}}

		public abstract class CharRule:Rule {
			public abstract bool CheckNext(char next);
			protected override bool MatchImplementation(Parser parser, ref Map map, bool keep) {
				char next=parser.Look();
				if(CheckNext(next)){
					map=next;
					parser.State.index++;
					parser.State.Column++;
					if(next.Equals(Syntax.unixNewLine)) {
						parser.State.Line++;
						parser.State.Column= 1;}
					return true;
				}
				else{return false;}
			}
		}
		public class Chars:CharRule{
			private string chars;
			public Chars(string chars){this.chars=chars;}
			public override bool CheckNext(char next) {
				return chars.IndexOf(next)!=-1;}}
		public class CharsExcept: CharRule {
			private string s;
		    public CharsExcept(string characters){s=characters+Syntax.endOfFile;}
		    public override bool CheckNext(char c) {
		        return s.IndexOf(c)==-1;}}		
		public class CharLoop:StringRule {
			private CharRule rule;
			private int min;
			private int max;
			public CharLoop(CharRule rule,int min,int max){
				this.rule=rule;
				this.min=min;
				this.max=max;}
			public override bool MatchString(Parser parser, ref string s) {
				int offset=0;
				int column=parser.State.Column;
				int line=0;
				while((max==-1 || offset<max) && rule.CheckNext(parser.Look(offset))) {
					offset++;
					column++;
					if(parser.Look(offset).Equals(Syntax.unixNewLine)) {
						line++;
						column= 1;}}
				s=parser.Text.Substring(parser.State.index,offset);
				if(offset>=min && (max==-1 || offset <= max))
				{
					parser.State.index+=offset;
					parser.State.Column=column;
					parser.State.Line+=line;
					return true;
				}
				return false;
			}}
			public class OneChar:CharLoop{
				public OneChar(CharRule rule):base(rule,1,1) {}
			}
			public class SingleChar:CharRule{
				private char c;
				public SingleChar(char c) {this.c=c;}
				public override bool CheckNext(char next) {
					return next.Equals(c);
				}
			}
			public class OneOrMoreChars:CharLoop{
				public OneOrMoreChars(CharRule rule):base(rule,1,-1){}
			}
			public class ZeroOrMoreChars:CharLoop{
				public ZeroOrMoreChars(CharRule rule):base(rule,0,-1){}
			}
		public abstract class StringRule:Rule {
			protected override bool MatchImplementation(Parser parser, ref Map map, bool keep) {
				string s=null;
				if(MatchString(parser,ref s)) {map=s;return true;}
				else {return false;}}
			public abstract bool MatchString(Parser parser,ref string s);}


		public delegate void PrePostDelegate(Parser parser);
		public class PrePost : Rule {
			private PrePostDelegate pre;
			private PrePostDelegate post;
			private Rule rule;
			public PrePost(PrePostDelegate pre, Rule rule, PrePostDelegate post) {
				this.pre = pre;
				this.rule = rule;
				this.post = post;}
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				pre(parser);
				bool matched=rule.Match(parser, ref map);
				post(parser);
				return matched;}}
		public static Rule StringRule2(string text) {
			List<Action> actions = new List<Action>();
			foreach (char c in text) {
				actions.Add(c);}
			return new Sequence(actions.ToArray());}
		public delegate bool ParseFunction(Parser parser, ref Map map);
		public class CustomRule : Rule {
			private ParseFunction parseFunction;
			public CustomRule(ParseFunction parseFunction) {
				this.parseFunction = parseFunction;}
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				return parseFunction(parser, ref map);}}
		public delegate Rule RuleFunction();
		public class DelayedRule : Rule {
			private RuleFunction ruleFunction;
			private Rule rule;
			public DelayedRule(RuleFunction ruleFunction) {
				this.ruleFunction = ruleFunction;}
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				if (rule == null) {
					rule = ruleFunction();}
				return rule.Match(parser,ref map);}}
		public class Alternatives : Rule {
			private Rule[] cases;
			public Alternatives(params Rule[] cases) {
				this.cases = cases;}
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				foreach (Rule expression in cases) {
					bool matched=expression.Match(parser, ref map);
					if (matched) {
						return true;}}
				return false;}}
		public class Sequence : Rule {
			private Action[] actions;
			public Sequence(params Action[] rules) {
				this.actions = rules;}
			protected override bool MatchImplementation(Parser parser, ref Map match,bool keep) {
				Map result = new Map();
				bool success = true;
				foreach (Action action in actions) {
					if (action != null) {
						bool matched = action.Execute(parser, ref result,keep);
						if (!matched) {
							success = false;
							break;}}}
				if (!success) {
					match=null;
					return false;}
				else {
					match=result;
					return true;}}}
		public class LiteralRule : Rule {
			private Map literal;
			public LiteralRule(Map literal) {
				this.literal = literal;}
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				map=literal;
				return true;}}
		public class ZeroOrMoreString : ZeroOrMore {
			public ZeroOrMoreString(Action action)
				: base(action) {}
			protected override bool MatchImplementation(Parser parser, ref Map result,bool keep) {
				bool match=base.MatchImplementation(parser, ref result,keep);
				if (match && result.IsString) {
					result = result.GetString();}
				return match;}}
		public class ZeroOrMore : Rule {
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				Map list = new Map(new ListStrategy());
				while (true) {
					if (!action.Execute(parser, ref list,keep)) {
						break;}}
				map=list;
				return true;}
			private Action action;
			public ZeroOrMore(Action action) {
				this.action = action;}}
		public class OneOrMore : Rule {
			protected override bool MatchImplementation(Parser parser, ref Map map,bool keep) {
				Map list = new Map(new ListStrategy());
				bool matched = false;
				while (true) {
					if (!action.Execute(parser, ref list,keep)) {
						break;}
					matched = true;}
				map=list;
				return matched;}
			private Action action;
			public OneOrMore(Action action) {
				this.action = action;}}
		public class Optional : Rule {
			private Rule rule;
			public Optional(Rule rule) {
				this.rule = rule;}
			protected override bool MatchImplementation(Parser parser, ref Map match,bool keep) {
				Map matched=null;
				rule.Match(parser, ref matched);
				if (matched == null) {
					match=null;
					return true;}
				else {
					match=matched;
					return true;}}}
		private char Look(int offset) {
			return Text[State.index+offset];}
		private char Look() {
			return Text[State.index];}
		public static Map Parse(string file) {
			return ParseString(System.IO.File.ReadAllText(file), file);}
		public static Map ParseString(string text, string fileName) {
			Parser parser = new Parser(text, fileName);
			Map result=null;
			Parser.File.Match(parser, ref result);
			if (parser.State.index != parser.Text.Length-1) {
				throw new SyntaxException("Expected end of file.", parser);}
			return result;}}
	public class Syntax {
		public const char arrayStart = '[';
		public const char arrayEnd = ']';
		public const char arraySeparator = ';';
		public const char programSeparator = ';';
		public const char programStart = '{';
		public const char programEnd ='}';
		public const char functionProgram = '?';
		public const char lastArgument = '@';
		public const char autokey = '.';
		public const char callSeparator = ' ';
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
		public static readonly string integer = "0123456789-";
		public static readonly string lookupStringForbidden = //new char[] {
			""+current+ lastArgument+ explicitCall+ indentation+ '\r'+ '\n'+
			function+ @string+emptyMap+ '!'+ root+ callStart+ callEnd+ 
			character+ programStart+ '*'+ '$'+ '\\'+ '<'+ '='+ arrayStart+
			'-'+ ':'+ functionProgram+ select+ ' '+ '-'+ '['+ ']'+ '*'+ '>'+ 
			programStart+ programSeparator +callSeparator+programEnd+
			arrayEnd;
		public static readonly string lookupStringForbiddenFirst = lookupStringForbidden+integer;}

	public class Serialization {
		public static string Serialize(Map map) {
			try {
				return Serialize(map, -1).Trim();}
			catch (Exception e) {
				throw e;}}
		private static string Number(Map map) {
			return map.GetNumber().ToString();}
		private static string Serialize(Map map, int indentation) {
			if (!map.Strategy.IsNormal) {
				return map.Strategy.ToString();}
			else if (map.Count == 0) {
				if (indentation < 0) {
					return "";}
				else {
					return "0";}}
			else if (map.IsNumber) {
				return Number(map);}
			else if (map.IsString) {
				return String(map, indentation);}
			else {
				return Map(map, indentation);}}
		private static string Map(Map map, int indentation) {
			string text;
			if (indentation < 0) {
				text = "";}
			else {
				text = "," + Environment.NewLine;}
			foreach (KeyValuePair<Map, Map> entry in map) {
				text += Entry(indentation, entry);}
			return text;}

		private static string Entry(int indentation, KeyValuePair<Map, Map> entry) {
			if (entry.Key.Equals(CodeKeys.Function)) {
				return Function(entry.Value, indentation + 1);}
			else {
				return (Indentation(indentation + 1)
					+ Key(indentation, entry) +
					"=" +
					Serialize(entry.Value, indentation + 1)
					+ Environment.NewLine).TrimEnd('\r', '\n')
					+ Environment.NewLine;}}
		private static string Literal(Map value, int indentation) {
			if (value.IsNumber) {
				return Number(value);}
			else if (value.IsString) {
				return String(value, indentation);}
			else {return "error";}}
			//throw new Exception("not implemented");}
		private static string Function(Map value, int indentation) {
			return value[CodeKeys.Parameter].GetString() + "|" + Expression(value[CodeKeys.Expression], indentation);}
		private static string Root() {
			return "/";}
		private static string Expression(Map map, int indentation) {
			if (map.ContainsKey(CodeKeys.Literal)) {
				return Literal(map[CodeKeys.Literal], indentation);}
			else if (map.ContainsKey(CodeKeys.Root)) {
				return Root();}
			if (map.ContainsKey(CodeKeys.Call)) {
				return Call(map[CodeKeys.Call], indentation);}
			else if (map.ContainsKey(CodeKeys.Program)) {
				return Program(map[CodeKeys.Program], indentation);}
			else if (map.ContainsKey(CodeKeys.Select)) {
				return Select(map[CodeKeys.Select], indentation);}
			else if (map.ContainsKey(CodeKeys.Search)) {
				return Search(map[CodeKeys.Search], indentation);}
			return Serialize(map, indentation);}
		private static string FunctionStatement(Map map, int indentation) {
			return map[CodeKeys.Parameter].GetString() + "|" +
				Expression(map[CodeKeys.Expression], indentation);}
		private static string KeyStatement(Map map, int indentation) {
			Map key = map[CodeKeys.Key];
			if (key.Equals(new Map(CodeKeys.Literal, CodeKeys.Function))) {
				return FunctionStatement(map[CodeKeys.Value][CodeKeys.Literal], indentation);}
			else {
				return Expression(map[CodeKeys.Key], indentation) + "="
					+ Expression(map[CodeKeys.Value], indentation);}}
		private static string CurrentStatement(Map map, int indentation) {
			return "&=" + Expression(map[CodeKeys.Value], indentation);}
		private static string SearchStatement(Map map, int indentation) {
			return Expression(map[CodeKeys.Keys], indentation) + ":" + Expression(map[CodeKeys.Value], indentation);}
		private static string DiscardStatement(Map map, int indentation) {
			return Expression(map[CodeKeys.Value], indentation);}
		private static string Statement(Map map, int indentation) {
			if (map.ContainsKey(CodeKeys.Key)) {
				return KeyStatement(map, indentation);}
			else if (map.ContainsKey(CodeKeys.Current)) {
				return CurrentStatement(map, indentation);}
			else if (map.ContainsKey(CodeKeys.Keys)) {
				return SearchStatement(map, indentation);}
			else if (map.ContainsKey(CodeKeys.Value)) {
				return DiscardStatement(map, indentation);}
			throw new Exception("not implemented");}
		private static string Program(Map map, int indentation) {
			string text = "," + NewLine();
			indentation++;
			foreach (Map m in map.Array) {
				text += Indentation(indentation) + Trim(Statement(m, indentation)) + NewLine();}
			return text;}
		private static string Trim(string text) {
			return text.TrimEnd('\n', '\r');}
		private static string NewLine() {
			return Environment.NewLine;}
		private static string Call(Map map, int indentation) {
			string text = "-" + NewLine();
			indentation++;
			foreach (Map m in map.Array) {
				text += Indentation(indentation) +
					Trim(Expression(m, indentation)) + NewLine();}
			return text;}
		private static string Select(Map map, int indentation) {
			string text = "." + NewLine();
			indentation++;
			foreach (Map sub in map.Array) {
				text += Indentation(indentation) +
					Trim(Expression(sub, indentation)) + NewLine();}
			return text;}
		private static string Search(Map map, int indentation) {
			return "!" + Expression(map, indentation);}
		private static string Key(int indentation, KeyValuePair<Map, Map> entry) {
			if (entry.Key.Count != 0 && entry.Key.IsString) {
				string key = entry.Key.GetString();
				if (key.IndexOfAny(Syntax.lookupStringForbidden.ToCharArray()) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst.ToCharArray()) != 0) {
					return entry.Key.GetString();}}
			return Serialize(entry.Key, indentation + 1);}
		private static string String(Map map, int indentation) {
			string text = map.GetString();
			if (text.Contains("\"") || text.Contains("\n")) {
				string result = "\"" + Environment.NewLine;
				foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
					result += Indentation(indentation) + "\t" + line + Environment.NewLine;}
				return result.Trim('\n', '\r') + Environment.NewLine + Indentation(indentation) + "\"";}
			else {
				return "\"" + text + "\"";}}
		private static string Indentation(int indentation) {
			return "".PadLeft(indentation, '\t');}}
	namespace Test {
		public class MetaTest : TestRunner {
			public static int Leaves(Map map) {
				int count = 0;
				foreach (KeyValuePair<Map, Map> pair in map) {
					if (pair.Value.IsNumber) {
						count++;}
					else {
						count += Leaves(pair.Value);}}
				return count;}
			public static string TestPath {
				get {
					return Path.Combine(Interpreter.InstallationPath, "Test");}}
			public class Serialization : Test {
				public override object GetResult(out int level) {
					level = 1;
					return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));}}
			public class Basic : Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"), new Map(1, "first argument", 2, "second argument"));}}
			public class Library : Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);}}
			public class Fibo: Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"fibo.meta"), Map.Empty);}}
			public static Map Run(string path, Map argument) {
				Map callable = Parser.Parse(path);
				callable.Scope = Gac.gac["library"];
				LiteralExpression gac = new LiteralExpression(Gac.gac, null);
				LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
				lib.Statement = new LiteralStatement(gac);

				callable[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
				callable[CodeKeys.Function].Compile(lib);
				return callable.Call(argument);}}
		namespace TestClasses {
			public class MemberTest {
				public static string classField = "default";
				public string instanceField = "default";
				public static string OverloadedMethod(string argument) {
					return "string function, argument+" + argument;}
				public static string OverloadedMethod(int argument) {
					return "integer function, argument+" + argument;}
				public static string OverloadedMethod(MemberTest memberTest, int argument) {
					return "MemberTest function, argument+" + memberTest + argument;}
				public static string ClassProperty {
					get {
						return classField;}
					set {
						classField = value;}}
				public string InstanceProperty {
					get {
						return this.instanceField;}
					set {
						this.instanceField = value;}}}
			public delegate object IntEvent(object intArg);
			public delegate object NormalEvent(object sender);
			public class TestClass {
				public class NestedClass {
					public static int field = 0;}
				public TestClass() {}
				public object CallInstanceEvent(object intArg) {
					return instanceEvent(intArg);}
				public static object CallStaticEvent(object sender) {
					return staticEvent(sender);}
				public event IntEvent instanceEvent;
				public static event NormalEvent staticEvent;
				protected string x = "unchangedX";
				protected string y = "unchangedY";
				protected string z = "unchangedZ";

				public static bool boolTest = false;

				public static object TestClass_staticEvent(object sender) {
					MethodBase[] m = typeof(TestClass).GetMethods();
					return null;}
				public delegate string TestDelegate(string x);
				public static Delegate del;
				public static void TakeDelegate(TestDelegate d) {
					del = d;}
				public static object GetResultFromDelegate() {
					return del.DynamicInvoke(new object[] { "argumentString"});}
				public double doubleValue = 0.0;
				public float floatValue = 0.0F;
				public decimal decimalValue = 0.0M;}
			public class PositionalNoConversion : TestClass {
				public PositionalNoConversion(string p1, string b, string p2) {
					this.x = p1;
					this.y = b;
					this.z = p2;}
				public string Concatenate(string p1, string b, string c) {
					return p1 + b + c + this.x + this.y + this.z;}}
			public class NamedNoConversion : TestClass {
				public NamedNoConversion(Map arg) {
					Map def = new Map();
					def[1] = "null";
					def["y"] = "null";
					def["p2"] = "null";
					if (arg.ContainsKey(1)) {
						def[1] = arg[1];}
					if (arg.ContainsKey("y")) {
						def["y"] = arg["y"];}
					if (arg.ContainsKey("p2")) {
						def["y2"] = arg["y2"];}
					this.x = def[1].GetString();
					this.y = def["y"].GetString();
					this.z = def["p2"].GetString();}
				public string Concatenate(Map arg) {
					Map def = new Map();
					def[1] = "null";
					def["b"] = "null";
					def["c"] = "null";

					if (arg.ContainsKey(1)) {
						def[1] = arg[1];}
					if (arg.ContainsKey("b")) {
						def["b"] = arg["b"];}
					if (arg.ContainsKey("c")) {
						def["c"] = arg["c"];}
					return def[1].GetString() + def["b"].GetString() + def["c"].GetString() +
						this.x + this.y + this.z;}}
			public class IndexerNoConversion : TestClass {
				public string this[string a] {
					get {
						return this.x + this.y + this.z + a;}
					set {
						this.x = a + value;}}}}}
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
		public static readonly Map Value = "value";}
	public class NumberKeys {
		public static readonly Map Negative = "negative";
		public static readonly Map Denominator = "denominator";}
	public class ExceptionLog {
		public ExceptionLog(Source source) {
			this.source = source;}
		public Source source;}
	public class MetaException : Exception {
		private string message;
		private Source source;
		private List<ExceptionLog> invocationList = new List<ExceptionLog>();
		public MetaException(string message, Source source) {
			this.message = message;
			this.source = source;}
		public List<ExceptionLog> InvocationList {
			get {
				return invocationList;}}
		public override string ToString() {
			string message = Message;
			if (invocationList.Count != 0) {
				message += "\n\nStack trace:";}
			foreach (ExceptionLog log in invocationList) {
				message += "\n" + GetSourceText(log.source);}

			return message;}
		public static string GetSourceText(Source source) {
			string text;
			if (source != null) {
				text = source.FileName + ", line ";
				text += source.Line + ", column " + source.Column;}
			else {
				text = "Unknown location";}
			return text;}
		public override string Message {
			get {
				return GetSourceText(source) + ": " + message;}}
		public Source Source {
			get {
				return source;}}
		public static int CountLeaves(Map map) {
			int count = 0;
			foreach (KeyValuePair<Map, Map> pair in map) {
				if (pair.Value == null) {
					count++;}
				else if (pair.Value.IsNumber) {
					count++;}
				else {
					count += CountLeaves(pair.Value);}}
			return count;}}
	public class SyntaxException : MetaException {
		public SyntaxException(string message, Parser parser)
			: base(message, new Source(parser.State.Line, parser.State.Column, parser.FileName)) {}}
	public class ExecutionException : MetaException {
		private Map context;
		public ExecutionException(string message, Source source, Map context)
			: base(message, source) {
			this.context = context;}}
	public class KeyDoesNotExist : ExecutionException {
		public KeyDoesNotExist(Map key, Source source, Map map)
			: base("Key does not exist: " + Serialization.Serialize(key) + " in " + Serialization.Serialize(map), source, map) {}}
	public class KeyNotFound : ExecutionException {
		public KeyNotFound(Map key, Source source, Map map)
			: base("Key not found: " + Serialization.Serialize(key), source, map) {}}
	public class Library {
		public static Map Double(Map d) {
			return new Map((object)(float)d.GetNumber().GetDouble());}
		public static void WriteLine(string s) {
			Console.WriteLine(s);}
		private static Random random = new Random();
		public static int Random(int max) {
			return random.Next(max) + 1;}
		public static string Trim(string text) {
			return text.Trim();}
		public static Map Modify(Map map, Map func) {
			Map result = new Map();
			foreach (KeyValuePair<Map, Map> entry in map) {
				result[entry.Key] = func.Call(entry.Value);}
			return result;}
		public static Map StringToNumber(Map map) {
			return Convert.ToInt32(map.GetString());}
		public static Map Foreach(Map map, Map func) {
			List<Map> result = new List<Map>();
			foreach (KeyValuePair<Map, Map> entry in map) {
				result.Add(func.Call(entry.Key).Call(entry.Value));}
			return new Map(new ListStrategy(result));}
		public static Map Switch(Map map, Map cases) {
			foreach (KeyValuePair<Map, Map> entry in cases) {
				if (Convert.ToBoolean(entry.Key.Call(map).GetNumber().GetInt32())) {
					return entry.Value.Call(map);}}
			return Meta.Map.Empty;}
		public static Map Raise(Number a, Number b) {
			return new Rational(Math.Pow(a.GetDouble(), b.GetDouble()));}
		public static int CompareNumber(Number a, Number b) {
			return a.CompareTo(b);}
		public static Map Sort(Map array, Map function) {
			List<Map> result = new List<Map>(array.Array);
			result.Sort(delegate(Map a, Map b) {
				return (int)Transform.ToDotNet(function.Call(a).Call(b), typeof(int));});
			return new Map(result);}
		public static bool Equal(object a, object b) {
			if( a is Map ) {
				if(((Map)a).IsNumber) 
				{
				}
			}
			return a.Equals(b);
		}
		public static Map Filter(Map array, Map condition) {
			List<Map> result = new List<Map>();
			foreach (Map m in array.Array) {
				if (Convert.ToBoolean(condition.Call(m).GetNumber().GetInt32())) {
					result.Add(m);}}
			return new Map(result);}
		public static Map ElseIf(bool condition, Map then, Map els) {
			if (condition) {
				return then.Call(new Map());}
			else {
				return els.Call(new Map());}}
		public static Map Sum(Map func, Map arg) {
			IEnumerator<Map> enumerator = arg.Array.GetEnumerator();
			if (enumerator.MoveNext()) {
				Map result = enumerator.Current.Copy();
				while (enumerator.MoveNext()) {
					result = func.Call(result).Call(enumerator.Current);}
				return result;}
			else {
				return Meta.Map.Empty;}}
		public static Map JoinAll(Map arrays) {
			List<Map> result = new List<Map>();
			foreach (Map array in arrays.Array) {
				result.AddRange(array.Array);}
			return new Map(result);}
		public static Map If(bool condition, Map then) {
			if (condition) {
				return then.Call(new Map());}
			return new Map();}
		public static Map Map(Map array, Map func) {
			List<Map> result = new List<Map>();
			foreach (Map map in array.Array) {
				result.Add(func.Call(map));}
			return new Map(result);}
		public static Map Append(Map array, Map item) {
			array.Append(item);
			return array;}
		public static Map EnumerableToArray(Map map) {
			List<Map> result = new List<Map>();
			foreach (object entry in (IEnumerable)(((ObjectMap)map.Strategy)).Object) {
				result.Add(Transform.ToMeta(entry));}
			return new Map(result);}
		public static Map Reverse(Map arg) {
			List<Map> list = new List<Map>(arg.Array);
			list.Reverse();
			return new Map(list);}
		public static Map Try(Map tryFunction, Map catchFunction) {
			try {
				return tryFunction.Call(Meta.Map.Empty);}
			catch (Exception e) {
				return catchFunction.Call(new Map(e));}}
		public static Map With(Map o, Map values) {
			object obj = ((ObjectMap)o.Strategy).Object;
			Type type = obj.GetType();
			foreach (KeyValuePair<Map, Map> entry in values) {
				Map value = entry.Value;
				//if (entry.Key.Strategy is ObjectMap)
				//{
				//    DependencyProperty key = (DependencyProperty)((ObjectMap)entry.Key.Strategy).Object;
				//    type.GetMethod("SetValue", new Type[] { typeof(DependencyProperty), typeof(Objec}).Invoke(obj, new object[] { key, Transform.ToDotNet(value, key.PropertyType) });
				//}
				//else
				//{
				MemberInfo[] members = type.GetMember(entry.Key.GetString());
				if (members.Length != 0) {
					MemberInfo member = members[0];
					if (member is FieldInfo) {
						FieldInfo field = (FieldInfo)member;
						field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));}
					else if (member is PropertyInfo) {
						PropertyInfo property = (PropertyInfo)member;
						if (typeof(IList).IsAssignableFrom(property.PropertyType) && !(value.Strategy is ObjectMap)) {
							if (value.ArrayCount != 0) {
								IList list = (IList)property.GetValue(obj, null);
								list.Clear();
								Type t = DotNetMap.GetListAddFunctionType(list, value);
								if (t == null) {
									t = DotNetMap.GetListAddFunctionType(list, value);
									throw new ApplicationException("Cannot convert argument.");}
								else {
									foreach (Map map in value.Array) {
										list.Add(Transform.ToDotNet(map, t));}}}}
						else {
							object converted = Transform.ToDotNet(value, property.PropertyType);
							property.SetValue(obj, converted, null);}}
					else if (member is EventInfo) {
						EventInfo eventInfo = (EventInfo)member;
						new Map(new Method(eventInfo.GetAddMethod(), obj, type)).Call(value);}
					else {
						throw new Exception("unknown member type");}}
				else {
					o[entry.Key] = entry.Value;}
				//}
			}
			return o;}
		[MergeCompile]
		public static Map MergeAll(Map array) {
			Map result = new Map();
			foreach (Map map in array.Array) {
				foreach (KeyValuePair<Map, Map> pair in map) {
					result[pair.Key] = pair.Value;}}
			return result;
		}
		[MergeCompile]
		public static Map Merge(Map arg, Map map) {
			foreach (KeyValuePair<Map, Map> pair in map) {
				arg[pair.Key] = pair.Value;}
			return arg;}
		public static Map Join(Map arg, Map map) {
			foreach (Map m in map.Array) {
				arg.Append(m);}
			return arg;}
		public static Map Range(Number arg) {
			Map result = new Map();
			for (int i = 1; i <= arg; i++) {
				result.Append(i);}
			return result;}}
	public class LiteralExpression : Expression {
		private Map literal;
		public LiteralExpression(Map literal, Expression parent)
			: base(null, parent) {
			this.literal = literal;}
		public override Structure StructureImplementation() {
			return new LiteralStructure(literal);
		}
		public override Compiled CompileImplementation(Expression parent) {
			throw new Exception("The method or operation is not implemented.");}}
	public class LiteralStatement : Statement {
		private LiteralExpression program;
		public LiteralStatement(LiteralExpression program)
			: base(null, null, 0) {
			this.program = program;}
		public override Structure Pre() {
			return program.EvaluateStructure();
		}
		protected override Structure CurrentImplementation(Structure previous) {
			return program.EvaluateStructure();
		}
		public override CompiledStatement Compile() {
			throw new Exception("The method or operation is not implemented.");}}
	public abstract class ScopeExpression : Expression {
		public ScopeExpression(Extent source, Expression parent)
			: base(source, parent) {}}
	public delegate T SingleDelegate<T>(T t);

	public abstract class Structure {
		public abstract bool IsConstant {
			get;
		}
		public abstract bool IsNumber {
			get;
		}

	}
	public class LiteralStructure:Structure {
		public override bool IsConstant {
			get { 
				return literal.IsConstant;
			}
		}
		public override bool IsNumber {
			get {
				return literal.IsNumber;
			}
		}
		public Map Literal {
			get {
				return literal;
			}
		}
		private Map literal;
		public LiteralStructure(Map literal) {
			if(literal==null) 
			{
			}
			this.literal=literal;
		}
	}
	public class MethodStructure { // partially applied?
	}
	public class ObjectStructure {
	}
	public class KeyStructure {
	}
	public class TypeStructure {
	}
	public class NumberStructure :Structure {
		public override bool IsNumber {
			get {
				return true;
			}
		}
		public override bool IsConstant {
			get {
				return false;
			}
		}
	}
	public class MergeCompile:CompilableAttribute {
		public override Map GetStructure() {
			return null;
		}
	}
	public abstract class CompilableAttribute : Attribute {
		public abstract Map GetStructure();
	}
	public class Map : IEnumerable<KeyValuePair<Map, Map>>, ISerializeEnumerableSpecial {
		//public void Specialize(Type type) {
		//    Program p=(Program)this[CodeKeys.Function].CreateExpression(this[CodeKeys.Function].expression);
		//    ((KeyStatement)p.statementList[0]).value=

		//}
		//private Dictionary<Type, Compiled> specialized;
		//private Dictionary<Type, Compiled> Specialized {
		//    get {
		//        if (specialized == null) {
		//            specialized = new Dictionary<Type, Compiled>();
		//        }
		//        return specialized;}
		//}
		//public Compiled GetCompiled(Type type) {
		//    try {
		//        if (Specialized.ContainsKey(type)) {
		//            return Specialized[type];}
		//        else if (Specialized.ContainsKey(typeof(Map))) {
		//            return Specialized[typeof(Map)];}
		//        else {
		//            return null;}
		//    }
		//    catch (Exception e) {
		//        throw e;}
		//}

		//public Compiled GetCompiled(Type type) {
		//    try {
		//        if (Specialized.ContainsKey(type)) {
		//            return Specialized[type];}
		//        else if (Specialized.ContainsKey(typeof(Map))) {
		//            return Specialized[typeof(Map)];}
		//        else {
		//            return null;}
		//    }
		//    catch (Exception e) {
		//        throw e;}
		//}
		public virtual bool IsConstant {
			get {
				return isConstant;}
			set {
				isConstant = value;}}
		private bool isConstant = true;
		private MapStrategy strategy;
		public Map(IEnumerable<Map> list)
			: this(new ListStrategy(new List<Map>(list))) {}
		public Map(System.Collections.Generic.ICollection<Map> list)
			: this(new ListStrategy()) {
			int index = 1;
			foreach (object entry in list) {
				this[index] = Transform.ToMeta(entry);
				index++;}}
		public Map()
			: this(EmptyStrategy.empty) {}
		public Map(Map map)
			: this(new ObjectMap(map)) {}
		public Map(object o)
			: this(new ObjectMap(o)) {}
		public Map(string text)
			: this(new StringStrategy(text)) {}
		public Map(Number number)
			: this(new NumberStrategy(number)) {}
		public Map(MapStrategy strategy) {
			this.strategy = strategy;}
		public Map(params Map[] keysAndValues)
			: this() {
			for (int i = 0; i <= keysAndValues.Length - 2; i += 2) {
				this[keysAndValues[i]] = keysAndValues[i + 1];}}
		public MapStrategy Strategy {
			get {
				return strategy;}
			set {
				strategy = value;}}
		public int Count {
			get {
				return strategy.Count;}}
		public int ArrayCount {
			get {
				return strategy.GetArrayCount();}}
		public bool IsString {
			get {
				return strategy.IsString;}}
		public bool IsNumber {
			get {
				return strategy.IsNumber;}}
		public IEnumerable<Map> Array {
			get {
				return strategy.Array;}}
		public Number GetNumber() {
			return strategy.GetNumber();}
		public string GetString() {
			return strategy.GetString();}
		public static Stack<Map> arguments = new Stack<Map>();
		public Map Call(Map arg) {
			arguments.Push(arg);
			Map result = strategy.Call(arg, this);
			arguments.Pop();
			return result;}
		public static Dictionary<object, Profile> calls = new Dictionary<object, Profile>();
		public void Append(Map map) {
			strategy.Append(map, this);}
		public bool ContainsKey(Map key) {
			return strategy.ContainsKey(key);}
		public override bool Equals(object obj) {
			Map map = obj as Map;
			if (map != null) {
				if (map.strategy.IsNormal == strategy.IsNormal) {
					return strategy.Equals(map.Strategy);}
				return false;}
			return false;}
		public Map TryGetValue(Map key) {
			return strategy.Get(key);}
		public IEnumerable<Map> Keys {
			get {
				return strategy.Keys;}}
		public Map this[Map key] {
			get {
				Map value = TryGetValue(key);
				if (value == null) {
					throw new KeyDoesNotExist(key, null, this);}
				else {
					return value;}}
			set {
				if (value == null) {
					throw new Exception("Value cannot be null.");}
				else {
					strategy.Set(key, value, this);}}}
		public override string ToString() {
			if (Count == 0) {
				return "0";}
			else if (IsString) {
				return GetString();}
			else {
				return Meta.Serialization.Serialize(this);}}
		public Expression expression;
		public Statement GetStatement(Program program, int index) {
			if (ContainsKey(CodeKeys.Keys)) {
				return new SearchStatement(this[CodeKeys.Keys].GetExpression(program), this[CodeKeys.Value].GetExpression(program), program, index);}
			else if (ContainsKey(CodeKeys.Current)) {
				return new CurrentStatement(this[CodeKeys.Value].GetExpression(program), program, index);}
			else if (ContainsKey(CodeKeys.Key)) {
				return new KeyStatement(this[CodeKeys.Key].GetExpression(program), this[CodeKeys.Value].GetExpression(program), program, index);}
			else if (ContainsKey(CodeKeys.Discard)) {
				return new DiscardStatement(program, this[CodeKeys.Value].GetExpression(program), index);}
			else {
				throw new ApplicationException("Cannot compile map");}}
		public void Compile(Expression parent) {
			GetExpression(parent).compiled = this.GetExpression(parent).Compile(parent);
		}
		public Expression GetExpression() {
			return GetExpression(null);
		}
		public Expression GetExpression(Expression parent) {
			if (expression == null) {
				expression = CreateExpression(parent);
			}
			return expression;
		}
		public Expression CreateExpression(Expression parent) {
			if (ContainsKey(CodeKeys.Call)) {
				return new Call(this[CodeKeys.Call], this.TryGetValue(CodeKeys.Parameter), parent);}
			else if (ContainsKey(CodeKeys.Program)) {
				return new Program(this[CodeKeys.Program], parent);}
			else if (ContainsKey(CodeKeys.Literal)) {
				return new Literal(this[CodeKeys.Literal], parent);}
			else if (ContainsKey(CodeKeys.Select)) {
				return new Select(this[CodeKeys.Select], parent);}
			else if (ContainsKey(CodeKeys.Search)) {
				return new Search(this[CodeKeys.Search], parent);}
			else if (ContainsKey(CodeKeys.Root)) {
				return new Root(this[CodeKeys.Root], parent);}
			else if (ContainsKey(CodeKeys.LastArgument)) {
				return new LastArgument(this[CodeKeys.LastArgument], parent);}
			else if (ContainsKey(CodeKeys.Expression)) {
				Program program = new Function(this.Source, parent);
				program.isFunction = true;
				Map parameter = this[CodeKeys.Parameter];
				if (parameter.Count != 0) {
					KeyStatement s = new KeyStatement(
						new Literal(parameter, program),
						new LastArgument(Map.Empty, program), program, 0);
					program.statementList.Add(s);}
				CurrentStatement c = new CurrentStatement(this[CodeKeys.Expression].GetExpression(program), program, program.statementList.Count);
				program.statementList.Add(c);
				return program;
			}
			else {
				throw new ApplicationException("Cannot compile map " + Meta.Serialization.Serialize(this));
			}
		}
		public int GetArrayCountDefault() {
			int i = 1;
			for (;ContainsKey(i);i++);
			return i - 1;}
		public Map Copy() {
			Map clone = strategy.CopyData();
			clone.Scope = Scope;
			clone.Source = Source;
			clone.expression=expression;
			//clone.specialized = specialized;
			clone.IsConstant = this.IsConstant;
			return clone;}
		public override int GetHashCode() {
			return strategy.GetHashCode();}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return this.GetEnumerator();}
		public IEnumerator<KeyValuePair<Map, Map>> GetEnumerator() {
			foreach (Map key in Keys) {
				yield return new KeyValuePair<Map, Map>(key, this[key]);}}
		public Extent Source;
		public static Map Empty = new Map(EmptyStrategy.empty);
		public static implicit operator Map(string text) {
			return new Map(text);}
		public static implicit operator Map(Number integer) {
			return new Map(integer);}
		public static implicit operator Map(double number) {
			return new Rational(number);}
		public static implicit operator Map(decimal number) {
			return (double)number;}
		public static implicit operator Map(float number) {
			return (double)number;}
		public static implicit operator Map(bool boolean) {
			return Convert.ToDouble(boolean);}
		public static implicit operator Map(char character) {
			return (double)character;}
		public static implicit operator Map(byte integer) {
			return (double)integer;}
		public static implicit operator Map(sbyte integer) {
			return (double)integer;}
		public static implicit operator Map(uint integer) {
			return (double)integer;}
		public static implicit operator Map(ushort integer) {
			return (double)integer;}
		public static implicit operator Map(int integer) {
			return (double)integer;}
		public static implicit operator Map(long integer) {
			return (double)integer;}
		public static implicit operator Map(ulong integer) {
			return (double)integer;}
		public Map Scope;
		public string SerializeDefault() {
			string text;
			if (this.Count == 0) {
				text = "0";}
			else if (this.IsString) {
				text = "\"" + this.GetString() + "\"";}
			else if (this.IsNumber) {
				text = this.GetNumber().ToString();}
			else {
				text = null;}
			return text;}
		public string Serialize() {
			return strategy.Serialize(this);}}}