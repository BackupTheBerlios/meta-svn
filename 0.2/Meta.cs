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
			this.Source = source;
		}
		public MapBase Evaluate(MapBase context) {
			return EvaluateImplementation(context);
		}
		public abstract MapBase EvaluateImplementation(MapBase context);}
	public abstract class Expression {
		public Compiled compiled;
		public bool isFunction = false;
		public readonly Extent Source;
		public readonly Expression Parent;
		public Statement Statement;
		public Expression(Extent source, Expression parent) {
			this.Source = source;
			this.Parent = parent;
		}
		private bool evaluated = false;
		private Structure structure;
		public MapBase GetConstant() {
			Structure s=EvaluateStructure();
			return s !=null && s.IsConstant? ((LiteralStructure)s).Literal:null;
		}
		public MapBase EvaluateMapStructure() {
			Structure s=EvaluateStructure();
			MapBase m;
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
					sources[Source.end] = new List<Expression>();
				}
				sources[Source.end].Add(this);
			}
			return result;
		}
		public static Dictionary<Source, List<Expression>> sources = new Dictionary<Source, List<Expression>>();
		public abstract Compiled CompileImplementation(Expression parent);}
	public class LastArgument : Expression {
		public LastArgument(MapBase code, Expression parent)
			: base(code.Source, parent) {}
		public override Structure StructureImplementation() {
			return null;
		}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledLastArgument(Source);
		}
	}
	public class CompiledLastArgument : Compiled {
		public CompiledLastArgument(Extent source): base(source) {}
		public override MapBase EvaluateImplementation(MapBase context) {
			return MapBase.arguments.Peek();
		}
	}
	public class Call : Expression {
		public List<Expression> calls;
		public Call(MapBase code, MapBase parameterName, Expression parent): base(code.Source, parent) {
			this.calls = new List<Expression>();
			foreach (MapBase m in code.Array) {
				calls.Add(m.GetExpression(this));
			}
			if(calls.Count==0) 
			{
			}
			if (calls.Count == 1) {
				calls.Add(new Literal(MapBase.Empty, this));
			}
		}
		public override Structure StructureImplementation() {
			List<object> arguments;
			MethodBase method;
			if (CallStuff(out arguments, out method)) {
				if (method is ConstructorInfo) {
					Dictionary<MapBase, Member> type = ObjectMap.cache.GetMembers(method.DeclaringType);
					MapBase result = new DictionaryMap();
					result.IsConstant=false;
					foreach (MapBase key in type.Keys) {
						result[key] = MapBase.Empty;
						//result[key] = new DictionaryMap();
					}
					return new LiteralStructure(result);
				}
				else if (arguments != null && method.GetCustomAttributes(typeof(CompilableAttribute), false).Length != 0) {
					try {
						MapBase result = (MapBase)method.Invoke(null, arguments.ToArray());
						result.IsConstant = false;
						return new LiteralStructure(result);
					}
					catch (Exception e) {}
				}
			}
			return null;
		}
		public bool CallStuff(out List<object> arguments, out MethodBase m) {
			MapBase first = (MapBase)calls[0].GetConstant();
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
						if (method.method.IsStatic || method.method is ConstructorInfo) {
							arguments = new List<object>();
							for (int i = 0; i < method.parameters.Length; i++) {
								MapBase arg = calls[i + 1].EvaluateMapStructure();
								if (arg == null) {
									m = method.method;
									return true;}
								else {
									object nextArg;
									if (Transform.TryToDotNet(arg, method.parameters[i].ParameterType, out nextArg)) {
										arguments.Add(nextArg);}
									else {
										m = method.method;
										return false;
									}
								}
							}
							m = method.method;
							return true;
						}
					}
				}
			}
			arguments = null;
			m = null;
			return false;
		}
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
			}
			return new CompiledCall(calls.ConvertAll<Compiled>(delegate(Expression e) {
				return e.Compile(this);}), Source);
		}
	}
	public delegate MapBase MetaConversion(object obj);
	public delegate object Conversion(MapBase map);
	public class EmittedCall : Compiled {
		private List<Compiled> arguments;
		private List<Conversion> conversions;
		private MetaConversion returnConversion;
		private ParameterInfo[] parameters;
		public EmittedCall(MethodInfo method, List<Compiled> arguments, Extent source)
			: base(source) {
			this.method = method;
			this.arguments = arguments;
			this.parameters = method.GetParameters();
			this.conversions=new List<Conversion>();
			this.returnConversion=Transform.GetMetaConversion(method.ReturnType);
			for(int i=0;i<parameters.Length;i++) {
				conversions.Add(Transform.GetConversion(parameters[i].ParameterType));
			}
		}
		private MethodInfo method;
		public override MapBase EvaluateImplementation(MapBase context) {
			object[] args=new object[parameters.Length];
			for (int index = 0; index < parameters.Length; index++) {
				args[index]=conversions[index](arguments[index].Evaluate(context));
			}
			return returnConversion(method.Invoke(null, args));
		}
		//public override MapBase EvaluateImplementation(MapBase context) {
		//    List<object> args = new List<object>();
		//    for (int index = 0; index < parameters.Length; index++) {
		//        args.Add(conversions[index](arguments[index].Evaluate(context)));
		//    }
		//    return returnConversion(method.Invoke(null, args.ToArray()));
		//}
	}
	public class CompiledCall : Compiled {
		List<Compiled> calls;
		public CompiledCall(List<Compiled> calls, Extent source)
			: base(source) {
			this.calls = calls;
		}
		public override MapBase EvaluateImplementation(MapBase current) {
			MapBase result = calls[0].Evaluate(current);
			for (int i = 1; i < calls.Count; i++) {
				try {
					result = result.Call(calls[i].Evaluate(current));}
				catch (MetaException e) {
					e.InvocationList.Add(new ExceptionLog(Source.start));
					throw e;
				}
				catch (Exception e) {
					while(e.InnerException!=null) {
						e=e.InnerException;
					}
					throw new MetaException(e.Message+"\n"+e.StackTrace, Source.start);}}
			return result;
		}
	}
	public class Search : Expression {
		public override Structure StructureImplementation() {
			MapBase key;
			int count;
			MapBase value;
			if (FindStuff(out count, out key, out value)) {
				return new LiteralStructure(value);
			}
			else {
				return null;
			}
		}
		private bool FindStuff(out int count, out MapBase key, out MapBase value) {
			Expression current = this;
			Structure keyStructure = expression.EvaluateStructure();
			if(keyStructure!=null ) {
				key=((LiteralStructure)keyStructure).Literal;
			}
			else {
				key=null;
			}
			count = 0;
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
					MapBase structure = statement.PreMap();
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
					current = current.Parent;
				}
			}
			value = null;
			return false;
		}
		private Expression expression;
		public Search(MapBase code, Expression parent)
			: base(code.Source, parent) {
			this.expression = code.GetExpression(this);
		}
		public override Compiled CompileImplementation(Expression parent) {
			int count;
			MapBase key;
			MapBase value;
			if (FindStuff(out count, out key, out value)) {
			    if (value != null && value.IsConstant) {
			        return new OptimizedSearch(value, Source);}
			    else {
			        return new FastSearch(key, count, Source);}}
			else {
			    FindStuff(out count, out key, out value);
				return new CompiledSearch(expression.Compile(this), Source);
			}
		}
	}
	public class FastSearch : Compiled {
		private int count;
		private MapBase key;
		public FastSearch(MapBase key, int count, Extent source)
			: base(source) {
			this.key = key;
			this.count = count;}
		public override MapBase EvaluateImplementation(MapBase context) {
			MapBase selected = context;
			for (int i = 0; i < count; i++) {
				selected = selected.Scope;
			}
			if (!selected.ContainsKey(key)) {
				selected = context;
				int realCount = 0;
				while (!selected.ContainsKey(key)) {
					selected = selected.Scope;
					realCount++;
					if (selected == null) {
						throw new KeyNotFound(key, Source.start, null);
					}
				}
			}
			return selected[key];
			//return selected[key].Copy();
		}
	}
	public class OptimizedSearch : Compiled {
		private MapBase literal;
		public OptimizedSearch(MapBase literal, Extent source)
			: base(source) {
			this.literal = literal;
		}
		public override MapBase EvaluateImplementation(MapBase context) {
			return literal;
			//return literal.Copy();
		}
	}
	public class CompiledSearch : Compiled {
		private Compiled expression;
		public CompiledSearch(Compiled expression, Extent source): base(source) {
			this.expression = expression;
		}
		public override MapBase EvaluateImplementation(MapBase context) {
			MapBase key = expression.Evaluate(context);
			MapBase selected = context;
			while (!selected.ContainsKey(key)) {
				if (selected.Scope != null) {
					selected = selected.Scope;
				}
				else {
					MapBase m=expression.Evaluate(context);
					bool b=context.ContainsKey(m);
					throw new KeyNotFound(key, Source.start, null);
				}
			}
			return selected[key];
			//return selected[key].Copy();
		}
	}
	public class CompiledProgram : Compiled {
		private List<CompiledStatement> statementList;
		public CompiledProgram(List<CompiledStatement> statementList, Extent source): base(source) {
			this.statementList = statementList;
		}
		public override MapBase EvaluateImplementation(MapBase parent) {
			MapBase context = new DictionaryMap();
			context.Scope = parent;
			foreach (CompiledStatement statement in statementList) {
				statement.Assign(ref context);
			}
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
		public Program(MapBase code, Expression parent)
			: base(code.Source, parent) {
			int index = 0;
			foreach (MapBase m in code.Array) {
				statementList.Add(m.GetStatement(this, index));
				index++;}}}
	public abstract class CompiledStatement {
		public CompiledStatement(Compiled value) {
			this.value = value;}
		public void Assign(ref MapBase context) {
			AssignImplementation(ref context, value.Evaluate(context));}
		public abstract void AssignImplementation(ref MapBase context, MapBase value);
		public readonly Compiled value;}
	public abstract class Statement {
		bool preEvaluated = false;
		bool currentEvaluated = false;
		private Structure pre;
		private Structure current;
		public MapBase PreMap() {
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
					pre = new LiteralStructure(new DictionaryMap());
				}
				else {
					pre = Previous.Current();}}
			preEvaluated = true;
			return pre;
		}
		public MapBase CurrentMap() {
			Structure s=Current();
			return s!=null?((LiteralStructure)s).Literal:null;
		}
		public Structure Current() {
			if (!currentEvaluated) {
				Structure pre = Pre();
				if (pre != null) {
					current = CurrentImplementation(pre);}
				else {
					current = null;
				}
			}
			currentEvaluated = true;
			return current;}
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
		public virtual bool DoesNotAddKey(MapBase key) {
			return true;
		}
		public bool NeverAddsKey(MapBase key) {
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
		protected abstract Structure CurrentImplementation(Structure previous);

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
	public class CompiledDiscardStatement : CompiledStatement {
		public CompiledDiscardStatement(Compiled value)
			: base(value) {}
		public override void AssignImplementation(ref MapBase context, MapBase value) {}
	}
	public class DiscardStatement : Statement {
		protected override Structure CurrentImplementation(Structure previous) {
			return previous;
		}
		public DiscardStatement(Program program, Expression value, int index): base(program, value, index) {}
		public override CompiledStatement Compile() {
			return new CompiledDiscardStatement(value.Compile(program));
		}
	}
	public class CompiledKeyStatement : CompiledStatement {
		private Compiled key;
		public CompiledKeyStatement(Compiled key, Compiled value): base(value) {
			this.key = key;
		}
		public override void AssignImplementation(ref MapBase context, MapBase value) {
			context[key.Evaluate(context)] = value;
		}
	}
	public class KeyStatement : Statement {
		public static bool intellisense = false;
		public override bool DoesNotAddKey(MapBase key) {
			Structure structure=this.key.EvaluateStructure();
			MapBase k;
			if(structure!=null) {
				k=((LiteralStructure)structure).Literal;
			}
			else {
				k=null;
			}
			if (k != null && k.IsConstant && !k.Equals(key)) {
				return true;
			}
			return false;
		}
		protected override Structure CurrentImplementation(Structure previous) {
			MapBase k=key.GetConstant();
			if (k != null) {
				MapBase val=value.EvaluateMapStructure();
				if (val == null) {
				    val = new DictionaryMap();
					val.IsConstant=false;
				}
				// not general enough
				if (value is Search || value is Call || (intellisense && (value is Literal || value is Program))) {
					((LiteralStructure)previous).Literal[k] = val;
				}
				else {
					MapBase m=new DictionaryMap();
					m.IsConstant=false;
					((LiteralStructure)previous).Literal[k] = m;
				}
				return previous;
			}
			return null;
		}
		public override CompiledStatement Compile() {
			MapBase k = key.GetConstant();
			if (k != null && k.Equals(CodeKeys.Function)) {
				if (value is Literal) {
					if(program.statementList.Count == 1) {
						((Literal)value).literal.Compile(program);
					}
				}
			}
			return new CompiledKeyStatement(key.Compile(program), value.Compile(program));
		}
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
		public override void AssignImplementation(ref MapBase context, MapBase value) {
			// fix this
			if (index == 0) {
				if(!(value is DictionaryMap)) 
				{
					context = value.Copy();
				}
				else {
					((DictionaryMap)context).dictionary = ((DictionaryMap)value).dictionary;
				}
			}
			else {
				context = value.Copy();
			}
		}
	}
	public class CurrentStatement : Statement {
		protected override Structure CurrentImplementation(Structure previous) {
			return value.EvaluateStructure();
		}
		public override CompiledStatement Compile() {
			return new CompiledCurrentStatement(value.Compile(program), Index);
		}
		public CurrentStatement(Expression value, Program program, int index): base(program, value, index) {}
	}
	public class CompiledSearchStatement : CompiledStatement {
		private Compiled key;
		public CompiledSearchStatement(Compiled key, Compiled value)
			: base(value) {
			this.key = key;
		}
		public override void AssignImplementation(ref MapBase context, MapBase value) {
			MapBase selected = context;
			MapBase key = this.key.Evaluate(context);
			while (!selected.ContainsKey(key)) {
				selected = selected.Scope;
				if (selected == null) {
					throw new KeyNotFound(key, key.Source.start, null);}}
			selected[key] = value;
		}
	}
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
			key.Statement = this;
		}
	}
	public class CompiledLiteral : Compiled {
		private MapBase literal;
		public CompiledLiteral(MapBase literal, Extent source)
			: base(source) {
			this.literal = literal;
		}
		public override MapBase EvaluateImplementation(MapBase context) {
			return literal;
			//return literal.Copy();
		}
	}
	public class Literal : Expression {
		public override Structure StructureImplementation() {
			return new LiteralStructure(literal);
		}
		private static Dictionary<MapBase, MapBase> cached = new Dictionary<MapBase, MapBase>();
		public MapBase literal;
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledLiteral(literal, Source);
		}
		public Literal(MapBase code, Expression parent): base(code.Source, parent) {
			this.literal = code;
		}
	}
	public class CompiledRoot : Compiled {
		public CompiledRoot(Extent source)
			: base(source) {}
		public override MapBase EvaluateImplementation(MapBase selected) {
			return Gac.gac;
		}
	}
	public class Root : Expression {
		public override Structure StructureImplementation() {
			return new LiteralStructure(Gac.gac);
		}
		public Root(MapBase code, Expression parent): base(code.Source, parent) {}
		public override Compiled CompileImplementation(Expression parent) {
			return new CompiledRoot(Source);
		}
	}
	public class CompiledSelect : Compiled {
		List<Compiled> subs;
		public CompiledSelect(List<Compiled> subs, Extent source): base(source) {
			this.subs = subs;
			if (subs[0] == null) {}
		}
		public override MapBase EvaluateImplementation(MapBase context) {
			MapBase selected = subs[0].Evaluate(context);
			for (int i = 1; i < subs.Count; i++) {
				MapBase key = subs[i].Evaluate(context);
				MapBase value = selected[key];
				//MapBase value = selected.TryGetValue(key);
				if (value == null) {
					MapBase x=selected[key];
					//selected.TryGetValue(key);
					throw new KeyDoesNotExist(key, subs[i].Source.start, selected);}
				else {
					selected = value;
				}
			}
			return selected;
		}
	}
	public class Select : Expression {
		public override Structure StructureImplementation() {
			MapBase selected = subs[0].GetConstant();
			for (int i = 1; i < subs.Count; i++) {
				MapBase key = subs[i].GetConstant();
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
		public Select(MapBase code, Expression parent): base(code.Source, parent) {
			foreach (MapBase m in code.Array) {
				subs.Add(m.GetExpression(this));
			}
		}
	}
	public class Interpreter {
		public static bool profiling = false;
		static Interpreter() {
	        MapBase map = Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta"));
	        map.Scope = Gac.gac;

	        LiteralExpression gac = new LiteralExpression(Gac.gac, null);

	        map[CodeKeys.Function].GetExpression(gac).Statement = new LiteralStatement(gac);
	        map[CodeKeys.Function].Compile(gac);

	        Gac.gac["library"] = map.Call(new DictionaryMap());
	        Gac.gac["library"].Scope = Gac.gac;
		}
		[STAThread]
		public static void Main(string[] args) {
			DateTime start = DateTime.Now;
			if (args.Length != 0) {
				if (args[0] == "-test") {
					try {
						UseConsole();
						new MetaTest().Run();
					}
					catch (Exception e) {
						string text=e.ToString();
						if(text.Length>1000) {
							text=text.Substring(0,1000)+"...";
						}
						DebugPrint(text);
					}
				}
				else if (args[0] == "-nprof") {
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), new DictionaryMap());
				}
				else if (args[0] == "-profile") {
					UseConsole();
					Interpreter.profiling = true;
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"game.meta"), new DictionaryMap());
					List<object> results = new List<object>(MapBase.calls.Keys);
					results.Sort(delegate(object a, object b) {
						return MapBase.calls[b].time.CompareTo(MapBase.calls[a].time);});
					foreach (object e in results) {
						Console.WriteLine(e.ToString() + "    " + MapBase.calls[e].time + "     " + MapBase.calls[e].calls);
					}
				}
				else if (args[0] == "-performance") {
					UseConsole();
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), new DictionaryMap());
				}
				else {
					string fileName = args[0].Trim('"');
					if (File.Exists(fileName)) {
						try {
							MetaTest.Run(fileName, new DictionaryMap());
						}
						catch (Exception e) {
							Console.WriteLine(e.ToString());
						}
						Console.WriteLine((DateTime.Now - start).TotalSeconds);
						return;
					}
					else {
						Console.WriteLine("File " + fileName + " not found.");
					}
				}
			}
			//Console.WriteLine((DateTime.Now - start).TotalSeconds);
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
			useConsole = true;}
		public static string InstallationPath {
			get {
				return @"D:\Meta\0.2\";
			}
		}
	}
	public class Transform {
		public static object ToDotNet(MapBase meta, Type target) {
			object dotNet;
			if (TryToDotNet(meta, target, out dotNet)) {
				return dotNet;
			}
			TryToDotNet(meta, target, out dotNet);
			throw new ApplicationException("Cannot convert " + Serialization.Serialize(meta) + " to " + target.ToString() + ".");
		}
		public static Delegate CreateDelegateFromCode(Type delegateType, MapBase code) {
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
				typeof(MapBase).Module);
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
			private MapBase callable;
			private Type returnType;
			public MetaDelegate(MapBase callable, Type returnType) {
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments) {
				//MapBase arg = new DictionaryMap();
				MapBase pos = this.callable;
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
		public static MetaConversion GetMetaConversion(Type type) {
			if(type.IsSubclassOf(typeof(MapBase)) || type.Equals(typeof(MapBase))) {
				return delegate(object obj) {
					return (MapBase)obj;
				};
			}
			else if(type.Equals(typeof(Boolean))) {
				return delegate(object obj) {
					return new Integer(Convert.ToInt32((bool)obj));
				};
			}
			else {
				return delegate(object obj) {
					return Transform.ToMeta(obj);
				};
			}
		}
		public static Conversion GetConversion(Type target) {
			if(target.Equals(typeof(Number))) {
				return delegate(MapBase map) {
					return map.GetNumber();
				};
			}
			else if(target.Equals(typeof(MapBase))) {
				return delegate(MapBase map) {
					return map;
				};
			}
			else if(target.Equals(typeof(Boolean))) {
				return delegate(MapBase map) {
					return Convert.ToBoolean(map.GetNumber().GetInt32());
				};
			}
			else {
				return delegate(MapBase map) {
					return Transform.ToDotNet(map,target);
				};
			}
		}
		public static bool TryToDotNet(MapBase meta, Type target, out object dotNet) {
			try {
				dotNet = null;
				if(target.Equals(typeof(MapBase))) {
					dotNet=meta;
				}
				else
				{
					Type type=meta.GetType();
					if (type.IsSubclassOf(target)) {
						dotNet = meta;
					}
					else {
						TypeCode typeCode = Type.GetTypeCode(target);
						if (typeCode == TypeCode.Object) {
							if (target == typeof(Number) && meta.IsNumber) {
								dotNet = meta.GetNumber();
							}
							if (dotNet == null && target == typeof(Type) && meta is TypeMap) {
								dotNet = ((TypeMap)meta).Type;
							}
							// remove?
							else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).Type)) {
								dotNet = ((ObjectMap)meta).Object;
							}
							else if (target.IsAssignableFrom(type)) {
								dotNet = meta;
							}
							else if (target.IsArray) {
								ArrayList list = new ArrayList();
								bool converted = true;
								Type elementType = target.GetElementType();
								foreach (MapBase m in meta.Array) {
									object o;
									if (Transform.TryToDotNet(m, elementType, out o)) {
										list.Add(o);}
									else {
										converted = false;
										break;
									}
								}
								if (converted) {
									dotNet = list.ToArray(elementType);
								}
							}
							else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
							   && meta.ContainsKey(CodeKeys.Function)) {
								dotNet = CreateDelegateFromCode(target, meta);}}
						else if (target.IsEnum) {
							dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());}
						else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).Type)) {
							dotNet = ((ObjectMap)meta).Object;
						}
						else {
							switch (typeCode) {
								case TypeCode.Boolean:
									if (meta.IsNumber && (meta.GetNumber().GetInt32() == 1 || meta.GetNumber().GetInt32() == 0)) {
										dotNet = Convert.ToBoolean(meta.GetNumber().GetInt32());
									}
									break;
								case TypeCode.Byte:
									if (IsIntegerInRange(meta, Byte.MinValue, Byte.MaxValue)) {
										dotNet = Convert.ToByte(meta.GetNumber().GetInt32());
									}
									break;
								case TypeCode.Char:
									if (IsIntegerInRange(meta, Char.MinValue, Char.MaxValue)) {
										dotNet = Convert.ToChar(meta.GetNumber().GetInt32());
									}
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
										dotNet = (decimal)(meta.GetNumber().GetInt64());
									}
									break;
								case TypeCode.Double:
									if (IsIntegerInRange(meta, double.MinValue, double.MaxValue)) {
										dotNet = (double)(meta.GetNumber().GetInt64());
									}
									break;
								case TypeCode.Int16:
									if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue)) {
										dotNet = Convert.ToInt16(meta.GetNumber().GetRealInt64());
									}
									break;
								case TypeCode.Int32:
									if (IsIntegerInRange(meta, Int32.MinValue, Int32.MaxValue)) {
										dotNet = meta.GetNumber().GetInt32();
									}
									break;
								case TypeCode.Int64:
									if (IsIntegerInRange(meta, new Rational(Int64.MinValue), new Rational(Int64.MaxValue))) {
										dotNet = Convert.ToInt64(meta.GetNumber().GetInt64());
									}
									break;
								case TypeCode.SByte:
									if (IsIntegerInRange(meta, SByte.MinValue, SByte.MaxValue)) {
										dotNet = Convert.ToSByte(meta.GetNumber().GetInt64());}
									break;
								case TypeCode.Single:
									if (IsIntegerInRange(meta, Single.MinValue, Single.MaxValue)) {
										dotNet = (float)meta.GetNumber().GetInt64();
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
										dotNet = Convert.ToUInt32(meta.GetNumber().GetInt64());
									}
									break;
								case TypeCode.UInt64:
									if (IsIntegerInRange(meta, new Rational(UInt64.MinValue), new Rational(UInt64.MaxValue))) {
										dotNet = Convert.ToUInt64(meta.GetNumber().GetInt64());
									}
									break;
								default:
									throw new ApplicationException("not implemented");
							}
						}
					}
				}
				return dotNet != null;
			}
			catch (Exception e) {
				throw e;
			}
		}
		public static bool IsIntegerInRange(MapBase meta, Number minValue, Number maxValue) {
			return meta.IsNumber && meta.GetNumber() >= minValue && meta.GetNumber() <= maxValue;
		}
		public static MapBase ToMeta(object dotNet) {
			if (dotNet == null) {
				return MapBase.Empty;
				//return new DictionaryMap();
			}
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
						return new ObjectMap(dotNet);
					case TypeCode.DBNull:
						return new ObjectMap(dotNet);
					case TypeCode.Object:
						if(dotNet is Number) {
							return (Number)dotNet;
						}
						else if (dotNet is MapBase) {
							return (MapBase)dotNet;
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
	public delegate MapBase CallDelegate(MapBase argument);
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
	public class Method : MapBase {
		public override void Append(MapBase map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override IEnumerable<MapBase> Array {
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
		//public override object UniqueKey {
		//    get {
		//        return method;
		//    }
		//}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override bool ContainsKey(MapBase key) {
			return false;
		}
		public override IEnumerable<MapBase> Keys {
			get {
				yield break;
			}
		}
		public override MapBase this[MapBase key] {
			get {
				return null;
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override MapBase Copy() {
			return this;
		}
		public MethodBase method;
		protected object obj;
		protected Type type;
		public Method(MethodBase method, object obj, Type type) {
			this.method = method;
			this.obj = obj;
			this.type = type;
			this.parameters = method.GetParameters();
		}
		public ParameterInfo[] parameters;
		public override MapBase Call(MapBase argument) {
		    return DecideCall(argument, new List<object>());
		}
		private MapBase DecideCall(MapBase argument, List<object> oldArguments) {
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
				CallDelegate call = new CallDelegate(delegate(MapBase map) {
					return DecideCall(map, arguments);});
				return new Method(invokeMethod, call, typeof(CallDelegate));
			}
		}
		MethodInfo invokeMethod = typeof(CallDelegate).GetMethod("Invoke");
		private MapBase Invoke(MapBase argument, object[] arguments) {
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
		private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
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
		public TypeMap(Type targetType): base(null, targetType) {}
		public override bool ContainsKey(MapBase key) {
			return this[key] != null;
		}
		public override MapBase this[MapBase key] {
			get {
				if (Type.IsGenericTypeDefinition && key is TypeMap) {
					List<Type> types = new List<Type>();
					if (Type.GetGenericArguments().Length == 1) {
						types.Add(((TypeMap)key).Type);
					}
					else {
						foreach (MapBase map in key.Array) {
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
			return name;}
		public override MapBase Copy() {
			return new TypeMap(this.Type);
		}
		private Method constructor;
		public Method Constructor {
			get {
				if (constructor == null) {
					ConstructorInfo method = Type.GetConstructor(new Type[] {});
					if (method == null) {
						throw new Exception("Default constructor for " + Type + " not found.");}
					constructor = new Method(method, Object, Type);
				}
				return constructor;
			}
		}
		public override MapBase Call(MapBase argument) {
			return Library.With(Constructor.Call(new DictionaryMap()), argument);
		}
	}
	public class ObjectMap : DotNetMap {
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
		protected override BindingFlags BindingFlags {
			get {
				return bindingFlags;}}
		public static MemberCache cache = new MemberCache(bindingFlags);
		protected override MemberCache MemberCache {
			get {
				return cache;
			}
		}
		protected override object GlobalKey {
			get {
				return Object;}}
		public override MapBase Call(MapBase arg) {
			if (this.Type.IsSubclassOf(typeof(Delegate))) {
				return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg);}
			else {
				throw new Exception("Object is not callable.");
			}
		}
		public override int GetHashCode() {
			return Object.GetHashCode();}
		public override bool Equals(object o) {
			return o is ObjectMap && Object.Equals(((ObjectMap)o).Object);}
		public ObjectMap(string text)
			: this(text, text.GetType()) {}
		public ObjectMap(MapBase target)
			: this(target, target.GetType()) {}
		public ObjectMap(object target, Type type)
			: base(target, type) 
		{
		}
		public ObjectMap(object target)
			: base(target, target.GetType()) 
		{
		}
		public override string ToString() {
			return Object.ToString();
		}
		public override MapBase Copy() {
			return new ObjectMap(Object);
		}
	}
	public class CloneMap:MapBase {
		private MapBase original;
		public CloneMap(MapBase original) {
			this.original=original;
		}
		public override void Append(MapBase map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override IEnumerable<MapBase> Array {
			get {
				return original.Array;
			}
		}
		public override int ArrayCount {
			get {
				return original.ArrayCount;
			}
		}
		public override MapBase Call(MapBase arg) {
			return original.Call(arg);
		}
		public override bool ContainsKey(MapBase key) {
			return original.ContainsKey(key);
		}
		public override MapBase Copy() {
			return new CloneMap(original);
		}
		public override int Count {
			get {
				return original.Count;
			}
		}
		public override bool Equals(object obj) {
			return original.Equals(obj);
		}
		public override int GetHashCode() {
			return original.GetHashCode();
		}
		public override Number GetNumber() {
			return original.GetNumber();
		}
		public override string GetString() {
			return original.GetString();
		}
		public override bool IsNormal {
			get {
				return original.IsNormal;
			}
		}
		public override IEnumerable<MapBase> Keys {
			get {
				return original.Keys;
			}
		}
		public override string Serialize() {
			return original.Serialize();
		}
		public override MapBase this[MapBase key] {
			get {
				return original[key];
			}
			set {
				throw new Exception("The method or operation is not implemented.");
			}
		}
	}
	public class DictionaryMap : MapBase {
		//private MapBase scope;
		//public override MapBase Scope {
		//    get {
		//        return scope;
		//    }
		//    set {
		//        scope=value;
		//    }
		//}

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
			MapBase map=obj as MapBase;
			if (map!=null && map.Count==Count) {
				foreach (MapBase key in this.Keys) {
					MapBase otherValue = map[key];
					MapBase thisValue = this[key];
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue)) {
						return false;
					}
				}
				return true;
			}
			return false;
		}
		public DictionaryMap(params MapBase[] keysAndValues){
		    for (int i = 0; i <= keysAndValues.Length - 2; i += 2) {
		        this[keysAndValues[i]] = keysAndValues[i + 1];
		    }
		}
		public DictionaryMap(System.Collections.Generic.ICollection<MapBase> list) {
			int index = 1;
			foreach (object entry in list) {
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}
		public DictionaryMap(IEnumerable<MapBase> list) {
			foreach(MapBase map in list) {
				this.Append(map);
			}
		}


		public override IEnumerable<MapBase> Array {
			get {
				for (int i = 1; this.ContainsKey(i); i++) {
					yield return this[i];
				}
			}
		}
		public override MapBase Copy() {
			return DeepCopy();
		}
		public override void Append(MapBase map) {
			this[ArrayCount + 1]=map;
		}

		public virtual MapBase CallImplementation(MapBase argument) {
		    MapBase function=this[CodeKeys.Function];
		    if (function!=null) {
		        if (function.Expression!=null && function.Expression.compiled!= null) {
		            return function.Expression.compiled.Evaluate(this);
		        }
		        else {
		            return function.GetExpression(null).Compile(null).Evaluate(this);
				}
			}
		    else {
		        throw new ApplicationException("Map is not a function: " + Meta.Serialization.Serialize(this));
		    }
		}
		public override MapBase Call(MapBase argument) {
			long start = 0;
			//if (Interpreter.profiling && UniqueKey != null) {
			//    QueryPerformanceCounter(out start);
			//    if (!MapBase.calls.ContainsKey(UniqueKey)) {
			//        MapBase.calls[UniqueKey] = new Profile();}
			//    MapBase.calls[UniqueKey].calls++;
			//    MapBase.calls[UniqueKey].recursive++;
			//}
			MapBase.arguments.Push(argument);
			MapBase result = CallImplementation(argument);
			MapBase.arguments.Pop();
			//MapBase result = CallImplementation(argument, parent);
			//Map result = CallImplementation(argument, parent);

			//if (Interpreter.profiling) {
			//    if (UniqueKey != null) {
			//        long stop;
			//        QueryPerformanceCounter(out stop);
			//        double duration = (double)(stop - start) / (double)freq;
			//        MapBase.calls[UniqueKey].recursive--;
			//        if (MapBase.calls[UniqueKey].recursive == 0) {
			//            MapBase.calls[UniqueKey].time += duration;}}}
			return result;
		}
		public override Number GetNumber() {
			Number number;
			if (Count == 0) {
				number = 0;}
			else if (this.Count == 1 && this.ContainsKey(MapBase.Empty) && this[MapBase.Empty].IsNumber) {
				number = 1 + this[MapBase.Empty].GetNumber();
				//number = 1 + this[new DictionaryMap()].GetNumber();
			}
			else {
				number =null;
			}
			return number;
		}
		public override string GetString() {
			StringBuilder text = new StringBuilder("");
			if(ArrayCount==0 || ArrayCount !=Count ) {
				return null;
			}
			foreach (MapBase map in Array) {
				Number number=map.GetNumber();
				if(number==null) {
					return null;
				}
				else {
					if(Transform.IsIntegerInRange(number, (int)Char.MinValue, (int)Char.MaxValue)) {
						text.Append(Convert.ToChar(map.GetNumber().GetInt32()));
					}
					else {
						return null;
					}
				}
			}
			return text.ToString();
		}
		public override string Serialize() {
			string text;
			if (this.Count == 0) {
				text = "0";}
			else if (this.IsString) {
				text = "\"" + this.GetString() + "\"";}
			else if (this.IsNumber) {
				text = this.GetNumber().ToString();}
			else {
				text = null;}
			return text;
		}
		public override int ArrayCount {
			get {
				int i = 1;
				while (this.ContainsKey(i)) {
					i++;}
				return i - 1;
			}
		}
		public Dictionary<MapBase, MapBase> dictionary=new Dictionary<MapBase, MapBase>();
		public override MapBase this[MapBase key] {
			get {
				MapBase val;
				dictionary.TryGetValue(key, out val);
				return val;
			}
			set {
				if(key==null) 
				{
				}
				dictionary[key] = value;
			}
		}
		public override bool ContainsKey(MapBase key) {
			return dictionary.ContainsKey(key);}
		public override IEnumerable<MapBase> Keys {
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
		public int recursive;}
	public abstract class Member {
		public abstract void Set(object obj, MapBase value);
		public abstract MapBase Get(object obj);
	}
	public class TypeMember : Member {
		public override void Set(object obj, MapBase value) {
			throw new Exception("The method or operation is not implemented.");}
		private Type type;
		public TypeMember(Type type) {
			this.type = type;}
		public override MapBase Get(object obj) {
			return new TypeMap(type);
		}
	}
	public class FieldMember : Member {
		private FieldInfo field;
		public FieldMember(FieldInfo field) {
			this.field = field;}
		public override void Set(object obj, MapBase value) {
			field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));}
		public override MapBase Get(object obj) {
			return Transform.ToMeta(field.GetValue(obj));
		}
	}
	public class MethodMember : Member {
		private MethodBase method;
		public MethodMember(MethodInfo method) {
			this.method = method;}
		public override void Set(object obj, MapBase value) {
			throw new Exception("The method or operation is not implemented.");}
		public override MapBase Get(object obj) {
			return new Method(method, obj, method.DeclaringType);
		}
	}
	public class MemberCache {
		private BindingFlags bindingFlags;
		public MemberCache(BindingFlags bindingFlags) {
			this.bindingFlags = bindingFlags;}
		public Dictionary<MapBase, Member> GetMembers(Type type) {
			if (!cache.ContainsKey(type)) {
				Dictionary<MapBase, Member> data = new Dictionary<MapBase, Member>();
				foreach (MemberInfo member in type.GetMembers(bindingFlags)) {
					MethodInfo method = member as MethodInfo;
					if (method != null) {
						string name = TypeMap.GetMethodName(method);
						data[name] = new MethodMember(method);}
					FieldInfo field = member as FieldInfo;
					if (field != null) {
						data[field.Name] = new FieldMember(field);
					}
					Type t = member as Type;
					if (t != null) {
						data[t.Name] = new TypeMember(t);
					}
				}
				cache[type] = data;
			}
			else {}
			return cache[type];
		}
		private Dictionary<Type, Dictionary<MapBase, Member>> cache = new Dictionary<Type, Dictionary<MapBase, Member>>();}
	public abstract class DotNetMap : MapBase {
		public override IEnumerable<MapBase> Array {
			get {
				yield break;
			}
		}
		public override void Append(MapBase map) {
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
				return obj.GetHashCode();}
			else {
				return type.GetHashCode();
			}
		}
		public override bool Equals(object obj) {
			DotNetMap dotNet = obj as DotNetMap;
			if (dotNet != null) {
				return dotNet.Object == Object && dotNet.Type == Type;}
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
				name += "_" + parameter.ParameterType.Name;}
			return name;
		}
		private Dictionary<MapBase, Member> data;
		private Dictionary<MapBase, Member> Members {
			get {
				if (data == null) {
					data = MemberCache.GetMembers(type);}
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
		public override MapBase this[MapBase key] {
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
						global[GlobalKey] = new Dictionary<MapBase, MapBase>();}
					global[GlobalKey][key] = value;
				}
			}
		}
		public static Type GetListAddFunctionType(IList list, MapBase value) {
			foreach (MemberInfo member in list.GetType().GetMember("Add")) {
				if (member is MethodInfo) {
					MethodInfo method = (MethodInfo)member;
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1) {
						ParameterInfo parameter = parameters[0];
						bool c = true;
						foreach (MapBase entry in value.Array) {
							object o;
							if (!Transform.TryToDotNet(entry, parameter.ParameterType, out o)) {
								c = false;
								break;
							}
						}
						if (c) {
							return parameter.ParameterType;}}}}
			return null;
		}
		public static Dictionary<object, Dictionary<MapBase, MapBase>> global = new Dictionary<object, Dictionary<MapBase, MapBase>>();
		public override bool ContainsKey(MapBase key) {
			return this[key] != null;
		}
		public override int Count {
			get { 
				return new List<MapBase>(Keys).Count;
			}
		}
		public override IEnumerable<MapBase> Keys {
			get {
				foreach (MapBase key in Members.Keys) {
					yield return key;
				}
				if (global.ContainsKey(GlobalKey)) {
					foreach (MapBase key in global[GlobalKey].Keys) {
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
		public Delegate CreateEventDelegate(string name, MapBase code) {
			EventInfo eventInfo = type.GetEvent(name, BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static | BindingFlags.Instance);
			Delegate eventDelegate = Transform.CreateDelegateFromCode(eventInfo.EventHandlerType, code);
			return eventDelegate;
		}
	}
	public interface ISerializeEnumerableSpecial {
		string Serialize();
	}
	public class TestAttribute : Attribute {
		public TestAttribute(): this(1) {}
		public TestAttribute(int level) {
			this.level = level;
		}
		private int level;
		public int Level {
			get {
				return level;
			}
		}
	}
	[AttributeUsage(AttributeTargets.Property)]
	public class SerializeAttribute : Attribute {
		public SerializeAttribute(): this(1) {}
		public SerializeAttribute(int level) {
			this.level = level;}
		private int level;
		public int Level {
			get {
				return level;
			}
		}
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
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();}
		private static bool UseProperty(PropertyInfo property, int level) {
			object[] attributes = property.GetCustomAttributes(typeof(SerializeAttribute), false);
			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public static void Serialize(object obj, string indent, StringBuilder builder, int level) {
			if (obj == null) {
				builder.Append(indent + "null\n");
			}
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
						Serialize(entry, indent + indentationChar, builder, level);
					}
				}
			}
		}
	}
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
	public class Gac : MapBase{
		public override IEnumerable<MapBase> Array {
			get { 
				yield break;
			}
		}
		public override int Count {
			get { throw new Exception("The method or operation is not implemented."); }
		}
		public override void Append(MapBase map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override MapBase Call(MapBase arg) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override Number GetNumber() {
			return null;
		}
		public override string GetString() {
			return null;
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
		public static readonly MapBase gac = new Gac();
		private Gac() {
			cache["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private Dictionary<MapBase, MapBase> cache = new Dictionary<MapBase, MapBase>();
		public static MapBase LoadAssembly(Assembly assembly) {
			MapBase val = new DictionaryMap();
			foreach (Type type in assembly.GetExportedTypes()) {
				if (type.DeclaringType == null) {
					MapBase selected = val;
					string name;
					if (type.IsGenericTypeDefinition) {
						name = type.Name.Split('`')[0];}
					else {
						name = type.Name;}
					selected[type.Name] = new TypeMap(type);
					foreach (ConstructorInfo constructor in type.GetConstructors()) {
						if (constructor.GetParameters().Length != 0) {
							selected[TypeMap.GetConstructorName(constructor)] = new Method(constructor, null, type);
						}
					}
				}
			}
			return val;
		}
		public override MapBase this[MapBase key] {
			get {
				MapBase value;
				if (!cache.ContainsKey(key)) {
					if (key.IsString) {
						Assembly assembly;
						string path = Path.Combine(Interpreter.InstallationPath, key.GetString() + ".dll");
						if (File.Exists(path)) {
							assembly = Assembly.LoadFile(path);}
						else {
							try {
								assembly = Assembly.LoadWithPartialName(key.GetString());
							}
							catch(Exception e) {
								return null;
							}
						}
						if (assembly != null) {
							value = LoadAssembly(assembly);
							cache[key] = value;}
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
		public override MapBase Copy() {
			return this;
		}
		public override IEnumerable<MapBase> Keys {
			get {
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override bool ContainsKey(MapBase key) {
			return this[key] != null;
		}
	}
	public class StringMap : MapBase {
		public override bool IsNormal {
			get {
				return true;
			}
		}
		public override void Append(MapBase map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override MapBase Call(MapBase arg) {
			throw new Exception("The method or operation is not implemented.");
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
			return text.Length;}
		public override MapBase this[MapBase key] {
			get {
				if (key.IsNumber) {
					Number number = key.GetNumber();
					if (number.IsNatural && number > 0 && number <= Count) {
						return text[number.GetInt32() - 1];}
					else {
						return null;}}
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
				return text.Length;}
		}
		public override string GetString() {
			return text;
		}
		public override bool ContainsKey(MapBase key) {
			if (key.IsNumber) {
				Number number = key.GetNumber();
				if (number.IsNatural) {
					return number > 0 && number <= text.Length;}
				else {
					return false;}}
			else {
				return false;}}
		public override int ArrayCount {
			get {
				return text.Length;
			}
		}
		public override MapBase Copy() {
			return this;
		}
		public override IEnumerable<MapBase> Array {
			get { 
				foreach(char c in text) {
					yield return c;
				}
			}
		}
		public override IEnumerable<MapBase> Keys {
			get {
				for (int i = 1; i <= text.Length; i++) {
					yield return i;
				}
			}
		}
	}
	public abstract class Number:MapBase {
		public override bool IsNormal {
			get {
				return true;
			}
		}
		public override string GetString() {
			return null;
		}
		public override MapBase Call(MapBase arg) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override void Append(MapBase map) {
			throw new Exception("The method or operation is not implemented.");
		}
		public override IEnumerable<MapBase> Array {
			get { 
				yield break;
			}
		}
		public override int Count {
			get { 
				return new List<MapBase>(Keys).Count;
			}
		}
		public override MapBase Copy() {
			return this;
		}
		public override int ArrayCount {
			get {
				return 0;
			}
		}
		public override string Serialize() {
			return this.ToString();
		}
		public override bool ContainsKey(MapBase key) {
			return new List<MapBase>(Keys).Contains(key);
		}
		public override MapBase this[MapBase key] {
			get {
				if (ContainsKey(key)) {
					if (key.Count==0) {
					//if (key.Equals(new DictionaryMap())) {
						return this - 1;
					}
					else if (key.Equals(NumberKeys.Negative)) {
						return MapBase.Empty;
						//return new DictionaryMap();
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

		public override IEnumerable<MapBase> Keys {
			get {
				if (this!= 0) {
					yield return MapBase.Empty;
				}
				if (this< 0) {
					yield return NumberKeys.Negative;
				}
				if (Denominator != 1.0d) {
					yield return NumberKeys.Denominator;
				}
			}
		}
		//public override bool IsNumber {
		//    get {
		//        return true;
		//    }
		//}
		public override Number GetNumber() {
			return this;
		}
		public override string ToString() {
			if (Denominator == 1) {
				return Numerator.ToString();
			}
			else {
				return Numerator.ToString() + Syntax.fraction + Denominator.ToString();
			}
		}
		public static Number operator |(Number a, Number b) {
			return Convert.ToInt32(a.Numerator) | Convert.ToInt32(b.Numerator);
		}
		public override bool Equals(object o) {
			MapBase map = o as MapBase;
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
		public abstract int GetInt32();
		public abstract long GetInt64();
		public abstract long GetRealInt64();
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
			if(ReferenceEquals(a,null) && ReferenceEquals(b,null)) {
				return true;
			}
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
		public virtual Number Subtract(Number b) {
			return new Rational(Expand(b) - b.Expand(this), LeastCommonMultiple(this, b));
		}
		public virtual bool LessThan(Number b) {
			return Expand(b) < b.Expand(this);
		}
		public virtual Number Add(int b) {
		    return Add(new Integer(b));
		}
		public virtual Number Subtract(int b) {
		    return Add(new Integer(b));
		}
		public virtual bool LessThan(int b) {
		    return LessThan(new Integer(b));
		}
		public virtual Number Add(Number b) {
			 return new Rational(Expand(b) + b.Expand(this), LeastCommonMultiple(this, b));
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
		public double Expand(Number b) {
			return Numerator * (LeastCommonMultiple(this, b) / Denominator);
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
		public int CompareTo(Number number) {
			return GetDouble().CompareTo(number.GetDouble());
		}
		public abstract bool IsNatural {
			get;
		}
		public virtual bool IsInt32 {
			get {
				return IsNatural && Numerator<int.MaxValue && Numerator>int.MinValue; 
			}
		}
		public abstract double GetDouble();
	}
	public class Integer:Number {

		public override bool IsNormal {
			get {
				return true;
			}
		}
	    public override bool IsInt32 {
	        get {
	            return true;
	        }
	    }
	    private int integer;
	    public Integer(int integer) {
	        this.integer=integer;
	    }
	    public override double GetDouble() {
	        return integer;
	    }
	    public override Number Subtract(Number b) {
	        if(b.IsInt32) {
	            checked {
	                try {
	                    return new Integer(integer-b.GetInt32());
	                }
	                catch(OverflowException) {
	                    return base.Subtract(b);
	                }
	            }
	        }
	        return base.Subtract(b);
	    }
	    public override bool LessThan(Number b) {
	        if(b.IsInt32) {
	            checked {
	                try {
	                    return integer<b.GetInt32();
	                }
	                catch(OverflowException) {
	                    return base.LessThan(b);
	                }
	            }
	        }
	        return base.LessThan(b);
	    }
	    public override Number Add(Number b) {
	        if(b.IsInt32) {
	            checked {
	                try {
	                    return new Integer(integer+b.GetInt32());
	                }
	                catch(OverflowException) {
	                    return base.Add(b);
	                }
	            }
	        }
	        return base.Add(b);
	    }
	    public override Number Subtract(int b) {
	        checked {
	            try {
	                return new Integer(integer-b);
	            }
	            catch(OverflowException) {
	                return base.Subtract(b);
	            }
	        }
	    }
	    public override bool LessThan(int b) {
	        return integer<b;
	    }
	    public override Number Add(int b) {
	        checked {
	            try {
	                return new Integer(integer+b);
	            }
	            catch(OverflowException) {
	                return base.Add(b);
	            }
	        }
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
	    public override bool IsNatural {
	        get {
	            return true;
	        }
	    }
	    public override double Numerator {
	        get {
	            return integer;
	        }
	    }
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
		public Number Clone() {
			return new Rational(this);
		}
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
			protected override bool MatchImplementation(Parser parser, ref MapBase map, bool keep) {
				return rule.Match(parser,ref map,false);}
		}
		public static Rule EndOfLine = new Ignore(new Sequence(
			new ZeroOrMoreChars(new Chars(""+Syntax.space+Syntax.tab)),
			new Alternatives(Syntax.unixNewLine,Syntax.windowsNewLine)));

		public static Rule Integer = new Sequence(new CustomProduction(
		        delegate(Parser p, MapBase map, ref MapBase result) {
					Rational rational=new Rational(double.Parse(map.GetString()),1.0);
					Number number;
					if(rational.IsInt32) {
						number=new Integer(rational.GetInt32());
					}
					else {
						number=rational;
					}
					result=number;
				},
		        new OneOrMoreChars(new Chars(Syntax.integer))));
		public static Rule StartOfFile = new CustomRule(delegate(Parser p, ref MapBase map) {
			if (p.State.indentationCount == -1) {
				p.State.indentationCount++;
				return true;}
			else {return false;}});
		private static CustomRule SmallIndentation = new CustomRule(delegate(Parser p, ref MapBase map) {
			p.State.indentationCount++;
			return true;});
		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, ref MapBase map) {
			return StringRule2("".PadLeft(pa.State.indentationCount, Syntax.indentation)).Match(pa, ref map);});
		public static Rule Dedentation = new CustomRule(delegate(Parser pa, ref MapBase map) {
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
				MapBase map=null;
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
			new CustomProduction(delegate(Parser p, MapBase map, ref MapBase result) {
				if(map!=null) {
					result=new Rational(result.GetNumber().GetDouble(),map.GetNumber().GetDouble());
				}
			},new Optional(new Sequence(
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
					delegate(Parser parser, MapBase map, ref MapBase result) {
						if(!result.ContainsKey(1))
						{
						}
						result = new DictionaryMap(result[1], map);},
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
		public static Rule ComplexStuff(MapBase key, char start, char end, Rule separator, Rule entry, Rule first) {
			return ComplexStuff(key, start, end, separator, new Assignment(1, entry), new ReferenceAssignment(entry), first);}
		public static Rule ComplexStuff(MapBase key, char start, char end, Rule separator, Action firstAction, Action entryAction, Rule first) {
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
				new LiteralRule(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))),
			new Assignment(
				CodeKeys.Value,
				new Sequence(
					new Assignment(
						CodeKeys.Literal,
						Function))));

		private static Rule Simple(char c, MapBase literal) {
			return new Sequence(
				c,
				new ReferenceAssignment(new LiteralRule(literal)));}

		private static Rule EmptyMap = Simple(
			Syntax.emptyMap,
			MapBase.Empty
		);
		private static Rule Current = Simple(
			Syntax.current,
			new DictionaryMap(CodeKeys.Current, MapBase.Empty));
			//new DictionaryMap(CodeKeys.Current, new DictionaryMap()));
		public static Rule LastArgument = Simple(
			Syntax.lastArgument,
			new DictionaryMap(CodeKeys.LastArgument, new DictionaryMap()));
		private static Rule Root = Simple(
			Syntax.root,
			new DictionaryMap(CodeKeys.Root,new DictionaryMap()));

		private static Rule LiteralExpression = new Sequence(
			new Assignment(CodeKeys.Literal, new Alternatives(
				EmptyMap,
				Number,
				String,
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
			delegate(Parser p, MapBase map, ref MapBase result) {
				result = new DictionaryMap(
					CodeKeys.Key, 
					new DictionaryMap(CodeKeys.Literal, new Integer(p.defaultKeys.Peek())),
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
			new Assignment(CodeKeys.Discard, new LiteralRule(new DictionaryMap())));
		public static Rule CurrentStatement = ComplexStatement(
			'&',
			new Assignment(CodeKeys.Current, new LiteralRule(new DictionaryMap())));
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
							new Assignment(CodeKeys.Key, new LiteralRule(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))),
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
			protected abstract void Effect(Parser parser, MapBase map, ref MapBase result);


			public Action(Rule rule) {this.rule = rule;}
			public bool Execute(Parser parser, ref MapBase result,bool keep) {
				MapBase map=null;
				bool matched=rule.Match(parser,ref map);
				if (matched) {Effect(parser, map, ref result);}
				return matched;}}
		public class Autokey : Action {
			public Autokey(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				result.Append(map);}}
		public class Assignment : Action {
			private MapBase key;
			public Assignment(MapBase key, Rule rule): base(rule) {this.key = key;}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				if (map != null) {result[key] = map;}}}
		public class Match : Action {
			public Match(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {}}
		public class ReferenceAssignment : Action {
			public ReferenceAssignment(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				result = map;}}
		public class Append : Action {
			public Append(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				foreach (MapBase m in map.Array) {result.Append(m);}}}
		public class Join : Action {
			public Join(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				result = Library.Join(result, map);}}
		public class Merge : Action {
			public Merge(Rule rule): base(rule) {}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				result = Library.Merge(result, map);
			}
		}
		public class CustomProduction : Action {
			private CustomActionDelegate action;
			public CustomProduction(CustomActionDelegate action, Rule rule): base(rule) {
				this.action = action;}
			protected override void Effect(Parser parser, MapBase map, ref MapBase result) {
				this.action(parser, map, ref result);}}
		public delegate void CustomActionDelegate(Parser p, MapBase map, ref MapBase result);
		public delegate bool Precondition(Parser p);
		public class CachedResult
		{
			public CachedResult(MapBase map,State state)
			{
				this.map=map;
				this.state=state;
			}
			public MapBase map;
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
		    protected override bool MatchImplementation(Parser parser, ref MapBase map, bool keep) {
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
			public bool Match(Parser parser, ref MapBase map) {
				return Match(parser,ref map,true);}
			public int mismatches=0;
			public int calls=0;

			public bool Match(Parser parser, ref MapBase map,bool keep) {
				if(precondition!=null) { if(!precondition(parser)) {return false;}}
				calls++;
				State oldState=parser.State;
				bool matched;
				MapBase result=null;
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
			protected abstract bool MatchImplementation(Parser parser, ref MapBase map,bool keep);}

		public class Characters : CharacterRule {
		    private string chars;
		    public Characters(params char[] characters){chars=new string(characters);}
		    protected override bool MatchCharacter(char next) {
		        return chars.IndexOf(next)!=-1;}}
		
		public abstract class CharacterRule : Rule {
		    public static int calls;
		    protected abstract bool MatchCharacter(char c);
		    protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
		        char character = parser.Look();
		        calls++;
		        if (MatchCharacter(character)) {
		            parser.State.index++;
		            parser.State.Column++;
		            if (character.Equals(Syntax.unixNewLine)) {
		                parser.State.Line++;
		                parser.State.Column = 1;}
		            map=keep?new Integer(character):null;
		            return true;}
		        else {
		            map=null;
		            return false;}}}

		public abstract class CharRule:Rule {
			public abstract bool CheckNext(char next);
			protected override bool MatchImplementation(Parser parser, ref MapBase map, bool keep) {
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
			protected override bool MatchImplementation(Parser parser, ref MapBase map, bool keep) {
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
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				pre(parser);
				bool matched=rule.Match(parser, ref map);
				post(parser);
				return matched;}}
		public static Rule StringRule2(string text) {
			List<Action> actions = new List<Action>();
			foreach (char c in text) {
				actions.Add(c);}
			return new Sequence(actions.ToArray());}
		public delegate bool ParseFunction(Parser parser, ref MapBase map);
		public class CustomRule : Rule {
			private ParseFunction parseFunction;
			public CustomRule(ParseFunction parseFunction) {
				this.parseFunction = parseFunction;}
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				return parseFunction(parser, ref map);}}
		public delegate Rule RuleFunction();
		public class DelayedRule : Rule {
			private RuleFunction ruleFunction;
			private Rule rule;
			public DelayedRule(RuleFunction ruleFunction) {
				this.ruleFunction = ruleFunction;}
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				if (rule == null) {
					rule = ruleFunction();}
				return rule.Match(parser,ref map);}}
		public class Alternatives : Rule {
			private Rule[] cases;
			public Alternatives(params Rule[] cases) {
				this.cases = cases;}
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				foreach (Rule expression in cases) {
					bool matched=expression.Match(parser, ref map);
					if (matched) {
						return true;}}
				return false;}}
		public class Sequence : Rule {
			private Action[] actions;
			public Sequence(params Action[] rules) {
				this.actions = rules;}
			protected override bool MatchImplementation(Parser parser, ref MapBase match,bool keep) {
				MapBase result = new DictionaryMap();
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
			private MapBase literal;
			public LiteralRule(MapBase literal) {
				this.literal = literal;}
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				map=literal;
				//map=literal.Copy();
				return true;
			}
		}
		public class ZeroOrMoreString : ZeroOrMore {
			public ZeroOrMoreString(Action action)
				: base(action) {}
			protected override bool MatchImplementation(Parser parser, ref MapBase result,bool keep) {
				bool match=base.MatchImplementation(parser, ref result,keep);
				if (match && result.IsString) {
					result = result.GetString();}
				return match;}}
		public class ZeroOrMore : Rule {
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				MapBase list = new DictionaryMap();
				while (true) {
					if (!action.Execute(parser, ref list,keep)) {
						break;}}
				map=list;
				return true;}
			private Action action;
			public ZeroOrMore(Action action) {
				this.action = action;}}
		public class OneOrMore : Rule {
			protected override bool MatchImplementation(Parser parser, ref MapBase map,bool keep) {
				MapBase list = new DictionaryMap();
				bool matched = false;
				while (true) {
					if (!action.Execute(parser, ref list,keep)) {
						break;}
					matched = true;}
				map=list;
				return matched;}
			private Action action;
			public OneOrMore(Action action) {
				this.action = action;
			}
		}
		public class Optional : Rule {
			private Rule rule;
			public Optional(Rule rule) {
				this.rule = rule;}
			protected override bool MatchImplementation(Parser parser, ref MapBase match,bool keep) {
				MapBase matched=null;
				rule.Match(parser, ref matched);
				if (matched == null) {
					match=null;
					return true;}
				else {
					match=matched;
					return true;
				}
			}
		}
		private char Look(int offset) {
			return Text[State.index+offset];
		}
		private char Look() {
			return Text[State.index];
		}
		public static MapBase Parse(string file) {
			return ParseString(System.IO.File.ReadAllText(file), file);
		}
		public static MapBase ParseString(string text, string fileName) {
			Parser parser = new Parser(text, fileName);
			MapBase result=null;
			Parser.File.Match(parser, ref result);
			if (parser.State.index != parser.Text.Length-1) {
				throw new SyntaxException("Expected end of file.", parser);
			}
			return result;
		}
	}
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
		public static readonly string lookupStringForbidden =
			""+current+ lastArgument+ explicitCall+ indentation+ '\r'+ '\n'+
			function+ @string+emptyMap+ '!'+ root+ callStart+ callEnd+ 
			character+ programStart+ '*'+ '$'+ '\\'+ '<'+ '='+ arrayStart+
			'-'+ ':'+ functionProgram+ select+ ' '+ '-'+ '['+ ']'+ '*'+ '>'+ 
			programStart+ programSeparator +callSeparator+programEnd+
			arrayEnd;
		public static readonly string lookupStringForbiddenFirst = lookupStringForbidden+integer;
	}
	public class Serialization {
		public static string Serialize(MapBase map) {
			return Serialize(map, -1).Trim();
		}
		private static string Number(MapBase map) {
			return map.GetNumber().ToString();
		}
		private static string Serialize(MapBase map, int indentation) {
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
		private static string Map(MapBase map, int indentation) {
			string text;
			if (indentation < 0) {
				text = "";
			}
			else {
				text = "," + Environment.NewLine;
			}
			foreach (KeyValuePair<MapBase, MapBase> entry in map) {
				text += Entry(indentation, entry);
			}
			return text;
		}
		private static string Entry(int indentation, KeyValuePair<MapBase, MapBase> entry) {
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
		private static string Literal(MapBase value, int indentation) {
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
		private static string Function(MapBase value, int indentation) {
			return value[CodeKeys.Parameter].GetString() + "|" + Expression(value[CodeKeys.Expression], indentation);
		}
		private static string Root() {
			return "/";
		}
		private static string Expression(MapBase map, int indentation) {
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
		private static string FunctionStatement(MapBase map, int indentation) {
			return map[CodeKeys.Parameter].GetString() + "|" +
				Expression(map[CodeKeys.Expression], indentation);
		}
		private static string KeyStatement(MapBase map, int indentation) {
			MapBase key = map[CodeKeys.Key];
			if (key.Equals(new DictionaryMap(CodeKeys.Literal, CodeKeys.Function))) {
				return FunctionStatement(map[CodeKeys.Value][CodeKeys.Literal], indentation);
			}
			else {
				return Expression(map[CodeKeys.Key], indentation) + "="
					+ Expression(map[CodeKeys.Value], indentation);
			}
		}
		private static string CurrentStatement(MapBase map, int indentation) {
			return "&=" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string SearchStatement(MapBase map, int indentation) {
			return Expression(map[CodeKeys.Keys], indentation) + ":" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string DiscardStatement(MapBase map, int indentation) {
			return Expression(map[CodeKeys.Value], indentation);
		}
		private static string Statement(MapBase map, int indentation) {
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
		private static string Program(MapBase map, int indentation) {
			string text = "," + NewLine();
			indentation++;
			foreach (MapBase m in map.Array) {
				text += Indentation(indentation) + Trim(Statement(m, indentation)) + NewLine();}
			return text;
		}
		private static string Trim(string text) {
			return text.TrimEnd('\n', '\r');
		}
		private static string NewLine() {
			return Environment.NewLine;
		}
		private static string Call(MapBase map, int indentation) {
			string text = "-" + NewLine();
			indentation++;
			foreach (MapBase m in map.Array) {
				text += Indentation(indentation) +
					Trim(Expression(m, indentation)) + NewLine();
			}
			return text;
		}
		private static string Select(MapBase map, int indentation) {
			string text = "." + NewLine();
			indentation++;
			foreach (MapBase sub in map.Array) {
				text += Indentation(indentation) +
					Trim(Expression(sub, indentation)) + NewLine();}
			return text;
		}
		private static string Search(MapBase map, int indentation) {
			return "!" + Expression(map, indentation);
		}
		private static string Key(int indentation, KeyValuePair<MapBase, MapBase> entry) {
			if (entry.Key.Count != 0 && entry.Key.IsString) {
				string key = entry.Key.GetString();
				if (key.IndexOfAny(Syntax.lookupStringForbidden.ToCharArray()) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst.ToCharArray()) != 0) {
					return entry.Key.GetString();
				}
			}
			return Serialize(entry.Key, indentation + 1);
		}
		private static string String(MapBase map, int indentation) {
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
			public static int Leaves(MapBase map) {
				int count = 0;
				foreach (KeyValuePair<MapBase, MapBase> pair in map) {
					if (pair.Value.IsNumber) {
						count++;}
					else {
						count += Leaves(pair.Value);}}
				return count;}
			public static string TestPath {
				get {
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public class Fibo: Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"fibo.meta"), new DictionaryMap());
				}
			}
			public class Serialization : Test {
				public override object GetResult(out int level) {
					level = 1;
					return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
				}
			}
			public class Basic : Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"), new DictionaryMap(1, "first argument", 2, "second argument"));
				}
			}
			public class Library : Test {
			    public override object GetResult(out int level) {
			        level = 2;
			        return Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), new DictionaryMap());
			    }
			}

			//public class MergeSort: Test {
			//    public override object GetResult(out int level) {
			//        level = 2;
			//        return Run(Path.Combine(Interpreter.InstallationPath, @"mergeSort.meta"), new DictionaryMap());
			//    }
			//}

			public static MapBase Run(string path, MapBase argument) {
				MapBase callable = Parser.Parse(path);
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
				public NamedNoConversion(MapBase arg) {
					MapBase def = new DictionaryMap();
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
				public string Concatenate(MapBase arg) {
					MapBase def = new DictionaryMap();
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
		public static readonly MapBase LastArgument = "lastArgument";
		public static readonly MapBase Discard = "discard";
		public static readonly MapBase Key = "key";
		public static readonly MapBase Expression = "expression";
		public static readonly MapBase Parameter = "parameter";
		public static readonly MapBase Root = "root";
		public static readonly MapBase Search = "search";
		public static readonly MapBase Current = "current";
		public static readonly MapBase Scope = "scope";
		public static readonly MapBase Literal = "literal";
		public static readonly MapBase Function = "function";
		public static readonly MapBase Call = "call";
		public static readonly MapBase Select = "select";
		public static readonly MapBase Program = "program";
		public static readonly MapBase Keys = "keys";
		public static readonly MapBase Value = "value";}
	public class NumberKeys {
		public static readonly MapBase Negative = "negative";
		public static readonly MapBase Denominator = "denominator";}
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
				return source;
			}
		}
		public static int CountLeaves(MapBase map) {
			int count = 0;
			foreach (KeyValuePair<MapBase, MapBase> pair in map) {
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
		private MapBase context;
		public ExecutionException(string message, Source source, MapBase context)
			: base(message, source) {
			this.context = context;}}
	public class KeyDoesNotExist : ExecutionException {
		public KeyDoesNotExist(MapBase key, Source source, MapBase map)
			: base("Key does not exist: " + Serialization.Serialize(key) + " in " + Serialization.Serialize(map), source, map) {}}
	public class KeyNotFound : ExecutionException {
		public KeyNotFound(MapBase key, Source source, MapBase map)
			: base("Key not found: " + Serialization.Serialize(key), source, map) {}}
	public class Library {
		public static MapBase Slice(MapBase array,int start,int end) {
			return new DictionaryMap(new List<MapBase>(array.Array).GetRange(start-1,Math.Max(end-start+1,0)));
		}
		public static MapBase Select(MapBase array,MapBase function) {
			foreach(MapBase m in array.Array) {
				if(Convert.ToBoolean(function.Call(m).GetNumber().GetInt32())) {
					return m;
				}
			}
			throw new Exception("Predicate was not false for all items in the array.");
		}
		public static MapBase Rest(MapBase m) {
			return new DictionaryMap(new List<MapBase>(m.Array).GetRange(1,m.ArrayCount-1));
		}
		public static Number Floor(Number n){
			return new Rational(n.GetRealInt64());
		}
		public static MapBase While(MapBase condition,MapBase body) {
			while(Convert.ToBoolean(condition.Call(new DictionaryMap()).GetNumber().GetInt32())) {
				body.Call(new DictionaryMap());
			}
			return new DictionaryMap();
		}
		public static MapBase Double(MapBase d) {
			return new ObjectMap((object)(float)d.GetNumber().GetDouble());
		}
		public static void WriteLine(string s) {
			Console.WriteLine(s);}
		private static Random random = new Random();
		public static int Random(int lower,int upper) {
			return lower+Convert.ToInt32((random.NextDouble()*(upper-lower)));
		}
		public static string Trim(string text) {
			return text.Trim();}
		public static MapBase Modify(MapBase map, MapBase func) {
			MapBase result = new DictionaryMap();
			foreach (KeyValuePair<MapBase, MapBase> entry in map) {
				result[entry.Key] = func.Call(entry.Value);
			}
			return result;
		}
		public static MapBase StringToNumber(MapBase map) {
			return Convert.ToInt32(map.GetString());
		}
		public static MapBase Foreach(MapBase map, MapBase func) {
			List<MapBase> result = new List<MapBase>();
			foreach (KeyValuePair<MapBase, MapBase> entry in map) {
				result.Add(func.Call(entry.Key).Call(entry.Value));
			}
			return new DictionaryMap(result);
		}
		public static MapBase Switch(MapBase map, MapBase cases) {
			foreach (KeyValuePair<MapBase, MapBase> entry in cases) {
				if (Convert.ToBoolean(entry.Key.Call(map).GetNumber().GetInt32())) {
					return entry.Value.Call(map);
				}
			}
			return new DictionaryMap();
		}
		public static MapBase Raise(Number a, Number b) {
			return new Rational(Math.Pow(a.GetDouble(), b.GetDouble()));}
		public static int CompareNumber(Number a, Number b) {
			return a.CompareTo(b);}
		public static MapBase Sort(MapBase array, MapBase function) {
			List<MapBase> result = new List<MapBase>(array.Array);
			result.Sort(delegate(MapBase a, MapBase b) {
				return (int)Transform.ToDotNet(function.Call(a).Call(b), typeof(int));});
			return new DictionaryMap(result);
		}
		public static bool Equal(object a, object b) {
			if( a is MapBase ) {
				if(((MapBase)a).IsNumber) 
				{
				}
			}
			return a.Equals(b);
		}
		public static MapBase Filter(MapBase array, MapBase condition) {
			List<MapBase> result = new List<MapBase>();
			foreach (MapBase m in array.Array) {
				if (Convert.ToBoolean(condition.Call(m).GetNumber().GetInt32())) {
					result.Add(m);
				}
			}
			return new DictionaryMap(result);
		}
		public static MapBase ElseIf(bool condition, MapBase then, MapBase els) {
			if (condition) {
				return then.Call(new DictionaryMap());}
			else {
				return els.Call(new DictionaryMap());}}
		public static MapBase Sum(MapBase func, MapBase arg) {
			IEnumerator<MapBase> enumerator = arg.Array.GetEnumerator();
			if (enumerator.MoveNext()) {
				MapBase result = enumerator.Current.Copy();
				while (enumerator.MoveNext()) {
					result = func.Call(result).Call(enumerator.Current);}
				return result;}
			else {
				return new DictionaryMap();
			}
		}
		public static MapBase JoinAll(MapBase arrays) {
			List<MapBase> result = new List<MapBase>();
			foreach (MapBase array in arrays.Array) {
				result.AddRange(array.Array);
			}
			return new DictionaryMap(result);
		}
		public static MapBase If(bool condition, MapBase then) {
			if (condition) {
				return then.Call(new DictionaryMap());}
			return new DictionaryMap();}
		public static MapBase Map(MapBase array, MapBase func) {
			List<MapBase> result = new List<MapBase>();
			foreach (MapBase map in array.Array) {
				result.Add(func.Call(map));}
			return new DictionaryMap(result);
		}
		public static MapBase Append(MapBase array, MapBase item) {
			array.Append(item);
			return array;
		}
		public static MapBase EnumerableToArray(MapBase map) {
			List<MapBase> result = new List<MapBase>();
			foreach (object entry in (IEnumerable)((ObjectMap)map).Object) {
				result.Add(Transform.ToMeta(entry));}
			return new DictionaryMap(result);
		}
		public static MapBase Reverse(MapBase arg) {
			List<MapBase> list = new List<MapBase>(arg.Array);
			list.Reverse();
			return new DictionaryMap(list);
		}
		public static MapBase Try(MapBase tryFunction, MapBase catchFunction) {
			try {
				return tryFunction.Call(new DictionaryMap());
			}
			catch (Exception e) {
				return catchFunction.Call(new ObjectMap(e));
			}
		}
		public static MapBase With(MapBase o, MapBase values) {
			object obj = ((ObjectMap)o).Object;
			Type type = obj.GetType();
			foreach (KeyValuePair<MapBase, MapBase> entry in values) {
				MapBase value = entry.Value;
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
						if (typeof(IList).IsAssignableFrom(property.PropertyType) && !(value is ObjectMap)) {
							if (value.ArrayCount != 0) {
								IList list = (IList)property.GetValue(obj, null);
								list.Clear();
								Type t = DotNetMap.GetListAddFunctionType(list, value);
								if (t == null) {
									t = DotNetMap.GetListAddFunctionType(list, value);
									throw new ApplicationException("Cannot convert argument.");}
								else {
									foreach (MapBase map in value.Array) {
										list.Add(Transform.ToDotNet(map, t));}}}}
						else {
							object converted = Transform.ToDotNet(value, property.PropertyType);
							property.SetValue(obj, converted, null);}}
					else if (member is EventInfo) {
						EventInfo eventInfo = (EventInfo)member;
						new Method(eventInfo.GetAddMethod(), obj, type).Call(value);
					}
					else {
						throw new Exception("unknown member type");
					}
				}
				else {
					o[entry.Key] = entry.Value;
				}
				//}
			}
			return o;}
		[MergeCompile]
		public static MapBase MergeAll(MapBase array) {
			MapBase result = new DictionaryMap();
			foreach (MapBase map in array.Array) {
				foreach (KeyValuePair<MapBase, MapBase> pair in map) {
					result[pair.Key] = pair.Value;}}
			return result;
		}
		[MergeCompile]
		public static MapBase Merge(MapBase arg, MapBase map) {
			arg=arg.Copy();
			foreach (KeyValuePair<MapBase, MapBase> pair in map) {
				arg[pair.Key] = pair.Value;
			}
			return arg;
		}
		//public static MapBase Merge(MapBase arg, MapBase map) {
		//    foreach (KeyValuePair<MapBase, MapBase> pair in map) {
		//        arg[pair.Key] = pair.Value;
		//    }
		//    return arg;
		//}
		public static MapBase Join(MapBase arg, MapBase map) {
			foreach (MapBase m in map.Array) {
				arg.Append(m);}
			return arg;}
		public static MapBase Range(Number arg) {
			MapBase result = new DictionaryMap();
			for (int i = 1; i <= arg; i++) {
				result.Append(i);
			}
			return result;
		}
	}
	public class LiteralExpression : Expression {
		private MapBase literal;
		public LiteralExpression(MapBase literal, Expression parent)
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
		public MapBase Literal {
			get {
				return literal;
			}
		}
		private MapBase literal;
		public LiteralStructure(MapBase literal) {
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
		public override MapBase GetStructure() {
			return null;
		}
	}
	public abstract class CompilableAttribute : Attribute {
		public abstract MapBase GetStructure();
	}
	public abstract class MapBase:IEnumerable<KeyValuePair<MapBase, MapBase>>, ISerializeEnumerableSpecial {
		public static MapBase Empty=new DictionaryMap();
		public MapBase DeepCopy() {
			MapBase clone = new DictionaryMap();
			clone.Scope = Scope;
			clone.Source = Source;
			clone.Expression=Expression;
			clone.IsConstant = this.IsConstant;
			foreach (MapBase key in Keys) {
				clone[key] = this[key].Copy();
			}
			return clone;
		}
		public abstract MapBase Copy();

		public static Stack<MapBase> arguments = new Stack<MapBase>();
		public abstract bool IsNormal {
			get;
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
		public static implicit operator MapBase(string text) {
			return new StringMap(text);
		}
		public static implicit operator MapBase(double number) {
		    return new Rational(number);
		}
		public static implicit operator MapBase(decimal number) {
		    return (double)number;
		}
		public static implicit operator MapBase(float number) {
		    return (double)number;
		}
		public static implicit operator MapBase(bool boolean) {
		    return Convert.ToInt32(boolean);
		}
		public static implicit operator MapBase(char character) {
		    return (int)character;
		}
		public static implicit operator MapBase(byte integer) {
		    return (int)integer;
		}
		public static implicit operator MapBase(sbyte integer) {
		    return (int)integer;
		}
		public static implicit operator MapBase(uint integer) {
		    return (double)integer;
		}
		public static implicit operator MapBase(ushort integer) {
		    return (int)integer;
		}
		public static implicit operator MapBase(int integer) {
		    return new Integer(integer);
		}
		public static implicit operator MapBase(long integer) {
		    return (double)integer;
		}
		public static implicit operator MapBase(ulong integer) {
		    return (double)integer;
		}
		public virtual Extent Source {
			get {
				return null;
			}
			set {
			}
		}
		public virtual bool IsConstant {
			get {
				return true;
			}
			set {
			}
		}
		public abstract int Count {
			get;
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
		public abstract IEnumerable<MapBase> Array {
			get;
		}
		public abstract Number GetNumber();
		public abstract string GetString();
		public abstract MapBase Call(MapBase arg);
		public abstract void Append(MapBase map);
		public abstract bool ContainsKey(MapBase key);
		public abstract IEnumerable<MapBase> Keys {
			get;
		}
		public abstract MapBase this[MapBase key] {
			get;
			set;
		}
		public MapBase Scope;

		public void Compile(Expression parent) {
			GetExpression(parent).compiled = this.GetExpression(parent).Compile(parent);
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
		public Expression CreateExpression(Expression parent) {
			if (ContainsKey(CodeKeys.Call)) {
				return new Call(this[CodeKeys.Call], this[CodeKeys.Parameter], parent);
			}
			else if (ContainsKey(CodeKeys.Program)) {
				return new Program(this[CodeKeys.Program], parent);
			}
			else if (ContainsKey(CodeKeys.Literal)) {
				return new Literal(this[CodeKeys.Literal], parent);
			}
			else if (ContainsKey(CodeKeys.Select)) {
				return new Select(this[CodeKeys.Select], parent);
			}
			else if (ContainsKey(CodeKeys.Search)) {
				return new Search(this[CodeKeys.Search], parent);
			}
			else if (ContainsKey(CodeKeys.Root)) {
				return new Root(this[CodeKeys.Root], parent);
			}
			else if (ContainsKey(CodeKeys.LastArgument)) {
				return new LastArgument(this[CodeKeys.LastArgument], parent);
			}
			else if (ContainsKey(CodeKeys.Expression)) {
				Program program = new Function(this.Source, parent);
				program.isFunction = true;
				MapBase parameter = this[CodeKeys.Parameter];
				if (parameter.Count != 0) {
					KeyStatement s = new KeyStatement(
						new Literal(parameter, program),
						new LastArgument(MapBase.Empty, program), program, 0);
					program.statementList.Add(s);
				}
				CurrentStatement c = new CurrentStatement(this[CodeKeys.Expression].GetExpression(program), program, program.statementList.Count);
				program.statementList.Add(c);
				return program;
			}
			else {
				throw new ApplicationException("Cannot compile map " + Meta.Serialization.Serialize(this));
			}
		}
		public Statement GetStatement(Program program, int index) {
			if (ContainsKey(CodeKeys.Keys)) {
				return new SearchStatement(this[CodeKeys.Keys].GetExpression(program), this[CodeKeys.Value].GetExpression(program), program, index);
			}
			else if (ContainsKey(CodeKeys.Current)) {
				return new CurrentStatement(this[CodeKeys.Value].GetExpression(program), program, index);
			}
			else if (ContainsKey(CodeKeys.Key)) {
				return new KeyStatement(this[CodeKeys.Key].GetExpression(program), this[CodeKeys.Value].GetExpression(program), program, index);
			}
			else if (ContainsKey(CodeKeys.Discard)) {
				return new DiscardStatement(program, this[CodeKeys.Value].GetExpression(program), index);
			}
			else {
				throw new ApplicationException("Cannot compile map");
			}
		}
		public IEnumerator<KeyValuePair<MapBase, MapBase>> GetEnumerator() {
			foreach (MapBase key in Keys) {
				yield return new KeyValuePair<MapBase, MapBase>(key, this[key]);
			}
		}
	}
}