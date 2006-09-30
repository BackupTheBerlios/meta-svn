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
//	The above copyright notice and this permission notice shall be
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

namespace Meta
{
	public abstract class Compiled
	{
		public Source Source;
		public Compiled(Source source)
		{
			this.Source = source;
		}
		public Map Evaluate(Map context)
		{
			return EvaluateImplementation(context);
		}
		public abstract Map EvaluateImplementation(Map context);
	}
	public abstract class Expression
	{
		public bool isFunction = false;
		public readonly Source Source;
		public readonly Expression Parent;
		public Statement Statement;
		public Expression(Source source,Expression parent)
		{
			this.Source = source;
			this.Parent = parent;
		}
		private bool evaluated = false;
		private Map structure;
		public Map EvaluateStructure()
		{
			if (!evaluated)
			{
				structure = StructureImplementation();
			}
			return structure;
		}
		public abstract Map StructureImplementation();
		public abstract Compiled Compile(Expression parent);
	}
	public class LastArgument : Expression
	{
		public LastArgument(Map code,Expression parent)
			: base(code.Source,parent)
		{
		}
		public override Map StructureImplementation()
		{
			return null;
		}
		public override Compiled Compile(Expression parent)
		{
			return new CompiledLastArgument(Source);
		}
	}
	public class CompiledLastArgument : Compiled
	{
		public CompiledLastArgument(Source source)
			: base(source)
		{
		}
		public override Map EvaluateImplementation(Map context)
		{
			return Map.arguments.Peek();
		}
	}
	public class Call : Expression
	{
		public List<Expression> calls;
		public Call(Map code, Map parameterName,Expression parent)
			: base(code.Source,parent)
		{
			this.calls = new List<Expression>();
			foreach (Map m in code.Array)
			{
				calls.Add(m.GetExpression(this));
			}
			if (calls.Count == 1)
			{
				calls.Add(new Literal(Map.Empty,this));
			}
		}
		public override Map StructureImplementation()
		{
			Map first = calls[0].EvaluateStructure();
			if (first != null && first.IsConstant)
			{
				if (first.Strategy is Method)
				{
					Method method = (Method)first.Strategy;
					if (method.method.IsStatic && method.parameters.Length == calls.Count-1)
					{
						if (method.method.GetCustomAttributes(typeof(CompilableAttribute), false).Length != 0)
						{
							List<object> arguments = new List<object>();
							for (int i = 1; i < calls.Count; i++)
							{
								Map arg = calls[i].EvaluateStructure();
								if (arg == null)
								{
									return null;
								}
								else
								{
									arguments.Add(Transform.ToDotNet(arg,method.parameters[i-1].ParameterType));
								}
							}
							return (Map)method.method.Invoke(null, arguments.ToArray());
						}
					}
				}
			}
			return null;
		}
		//public override Map EvaluateStructure()
		//{
		//    Map first=calls[0].EvaluateStructure();
		//    Map arg = calls[1].EvaluateStructure();
		//    if (first != null && first.IsConstant && arg!=null)
		//    {
		//        if (first.Strategy is Method)
		//        {
		//            Method method = (Method)first.Strategy;
		//            if (method.method.IsStatic && method.parameters.Length==1 && calls.Count==2)
		//            {
		//                if (method.method.GetCustomAttributes(typeof(CompilableAttribute),false).Length != 0)
		//                {
		//                    return (Map)method.method.Invoke(null,new object[] {arg});
		//                }
		//            }
		//        }
		//    }
		//    return null;
		//}
		public override Compiled Compile(Expression parent)
		{
			return new CompiledCall(calls.ConvertAll<Compiled>(delegate(Expression e)
			{
				return e.Compile(this);
			}),Source);
		}
	}
	public class CompiledCall : Compiled
	{
		List<Compiled> calls;
		public CompiledCall(List<Compiled> calls,Source source):base(source)
		{
			this.calls = calls;
		}
		public override Map EvaluateImplementation(Map current)
		{
			Map result = calls[0].Evaluate(current);
			for (int i = 1; i < calls.Count; i++)
			{
				try
				{
					result = result.Call(calls[i].Evaluate(current));
				}
				catch (MetaException e)
				{
					throw e;
				}
				catch (Exception e)
				{
					throw new MetaException(e.Message, Source);
				}
			}
			return result;
		}
	}
	public class Search : Expression
	{
		public override Map StructureImplementation()
		{
			Map key;
			int count;
			Map value;
			if (FindStuff(out count,out key,out value))
			{
				return value;
			}
			else
			{
				return null;
			}
		}
		private bool FindStuff(out int count, out Map key,out Map value)
		{
			Expression current = this;
			key = expression.EvaluateStructure();
			count = 0;
			if (key != null && key.IsConstant)
			{
				bool hasCrossedFunction = false;
				while (true)
				{
					while (current.Statement == null)
					{
						if (current.isFunction)
						{
							hasCrossedFunction = true;
							count++;
						}
						current = current.Parent;
						if (current == null)
						{
							break;
						}
					}
					if (current == null)
					{
						break;
					}
					Statement statement = current.Statement;
					Map structure = statement.Pre();
					if (structure == null)
					{
						statement.Pre();
						break;
					}
					if (structure.ContainsKey(key))
					{
						value = structure[key];
						return true;
					}
					if (hasCrossedFunction)
					{
						if (!statement.NeverAddsKey(key))
						{
							break;
						}
					}
					count++;
					current = current.Parent;
				}
			}
			value = null;
			return false;
		}
		private Expression expression;
		public Search(Map code, Expression parent)
			: base(code.Source, parent)
		{
			this.expression = code.GetExpression(this);
		}
		public override Compiled Compile(Expression parent)
		{
			int count;
			Map key;
			Map value;
			if (FindStuff(out count, out key, out value))
			{
				if (value != null && value.IsConstant)
				{
					return new OptimizedSearch(value, Source);
				}
				else
				{
					return new FastSearch(key, count, Source);
				}
			}
			else
			{
				return new CompiledSearch(expression.Compile(this), Source);
			}
		}
	}
	public class FastSearch : Compiled
	{
		private int count;
		private Map key;
		public FastSearch(Map key, int count, Source source)
			: base(source)
		{
			this.key = key;
			this.count = count;
		}
		public override Map EvaluateImplementation(Map context)
		{
			Map selected = context;

			for (int i = 0; i < count; i++)
			{
				selected = selected.Scope;
			}
			int difference = 0;
			if (!selected.ContainsKey(key))
			{
				while (!selected.ContainsKey(key))
				{
					selected = selected.Scope;
					difference++;
				}
			}
			if (difference != 0)
			{
				if (difference == 1)
				{
				}
				if (difference == 2)
				{
				}
				if (difference == 3)
				{
				}
				if (difference == 4)
				{
				}
			}
			else
			{
			}
			return selected[key];
		}
	}
	public class OptimizedSearch : Compiled
	{
		private Map literal;
		public OptimizedSearch(Map literal,Source source):base(source)
		{
			this.literal = literal;
		}
		public override Map EvaluateImplementation(Map context)
		{
			return literal.Copy();
		}
	}
	public class CompiledSearch : Compiled
	{
		private Compiled expression;
		public CompiledSearch(Compiled expression,Source source):base(source)
		{
			this.expression = expression;
		}
		public override Map EvaluateImplementation(Map context)
		{
			Map key = expression.Evaluate(context);
			Map selected = context;
			while (!selected.ContainsKey(key))
			{
				if (selected.Scope != null)
				{
					selected = selected.Scope;
				}
				else
				{
					throw new KeyNotFound(key, Source, null);
				}
			}
			return selected[key].Copy();
		}
	}
	public class CompiledProgram : Compiled
	{
		private List<CompiledStatement> statementList;
		public CompiledProgram(List<CompiledStatement> statementList, Source source)
			: base(source)
		{
			this.statementList = statementList;
		}
		public override Map EvaluateImplementation(Map parent)
		{
			Map context = new Map();
			context.Scope = parent;
			foreach (CompiledStatement statement in statementList)
			{
				statement.Assign(ref context);
			}
			return context;
		}
	}
	public class Program : ScopeExpression
	{
		public override Map StructureImplementation()
		{
			return statementList[statementList.Count - 1].Current();
			//return null;
		}
		public override Compiled Compile(Expression parent)
		{
			return new CompiledProgram(statementList.ConvertAll < CompiledStatement >( delegate(Statement s)
			{
				return s.Compile();
			}), Source);
		}
		public List<Statement> statementList;
		public Program(Map code,Expression parent)
			: base(code.Source,parent)
		{
			statementList = new List<Statement>();
			int index=0;
			foreach (Map m in code.Array)
			{
				statementList.Add(m.GetStatement(this,index));
				index++;
			}
		}
	}
	public abstract class CompiledStatement
	{
		public CompiledStatement(Compiled value)
		{
			this.value = value;
		}
		public void Assign(ref Map context)
		{
			AssignImplementation(ref context, value.Evaluate(context));
		}
		public abstract void AssignImplementation(ref Map context, Map value);
		public readonly Compiled value;
	}
	public abstract class Statement
	{
		bool preEvaluated=false;
		bool currentEvaluated = false;
		private Map pre;
		private Map current;
		public virtual Map Pre()
		{
			if (!preEvaluated)
			{
				if (Previous == null)
				{
					pre=new Map();
				}
				else
				{
					pre=Previous.Current();
				}
			}
			preEvaluated = true;
			return pre;
		}
		public Map Current()
		{
			if (!currentEvaluated)
			{
				Map pre = Pre();
				if (pre != null)
				{
					return CurrentImplementation(pre);
				}
				else
				{
					return null;
				}
			}
			currentEvaluated = true;
			return current;
		}
		//public Map Current()
		//{
		//    Map pre = Pre();
		//    if (pre != null)
		//    {
		//        return CurrentImplementation(pre);
		//    }
		//    else
		//    {
		//        return null;
		//    }
		//}
		//public Map Post()
		//{
		//    return Post(Current());
		//}
		//public Map Post(Map previous)
		//{
		//    if (Next != null)
		//    {
		//        if (Next is CurrentStatement)
		//        {
		//            return previous;
		//        }
		//        else
		//        {
		//            return Next.Current();
		//        }
		//    }
		//    else
		//    {
		//        return previous;
		//    }
		//}
		public Statement Next
		{
			get
			{
				if (program==null || Index >=program.statementList.Count-1)
				{
					return null;
				}
				else
				{
					return program.statementList[Index + 1];
				}
			}
		}
		public virtual bool DoesNotAddKey(Map key)
		{
			return true;
		}
		public bool NeverAddsKey(Map key)
		{
			Statement current = this;
			while (true)
			{
				current=current.Next;
				if (current == null || current is CurrentStatement)
				{
					break;
				}
				if (!current.DoesNotAddKey(key))
				{
					return false;
				}
			}
			return true;
		}
		protected abstract Map CurrentImplementation(Map previous);

		public Statement Previous
		{
			get
			{
				if (Index == 0)
				{
					return null;
				}
				else
				{
					return program.statementList[Index - 1];
				}
			}
		}
		public abstract CompiledStatement Compile();
		public Program program;
		public readonly Expression value;
		public readonly int Index;
		public Statement(Program program, Expression value,int index)
		{
			this.program = program;
			this.Index = index;
			this.value = value;
			if (value != null)
			{
				value.Statement = this;
			}
		}
	}
	public class CompiledDiscardStatement : CompiledStatement
	{
		public CompiledDiscardStatement(Compiled value)
			: base(value)
		{
		}
		public override void AssignImplementation(ref Map context, Map value)
		{
		}
	}
	public class DiscardStatement : Statement
	{
		protected override Map CurrentImplementation(Map previous)
		{
			return previous;
		}
		public DiscardStatement(Program program, Expression value,int index)
			: base(program, value,index)
		{
		}
		public override CompiledStatement Compile()
		{
			return new CompiledDiscardStatement(value.Compile(program));
		}
	}
	public class CompiledKeyStatement : CompiledStatement
	{
		private Compiled key;
		public CompiledKeyStatement(Compiled key,Compiled value):base(value)
		{
			this.key = key;
		}
		public override void AssignImplementation(ref Map context, Map value)
		{
			context[key.Evaluate(context)] = value;
		}
	}
	public class KeyStatement : Statement
	{
		public override bool DoesNotAddKey(Map key)
		{
			Map k=this.key.EvaluateStructure();
			if (k != null && k.IsConstant && !k.Equals(key))
			{
				return true;
			}
			return false;
		}
		protected override Map CurrentImplementation(Map previous)
		{
			Map k = key.EvaluateStructure();
			if (k != null && k.IsConstant)
			{
				Map val=value.EvaluateStructure();
				if (val == null)
				{
				    val = new Unknown();
				}
				previous[k] = new Unknown();
				//previous[k] = new Unknown();
				return previous;
			}
			return null;
		}
		public override CompiledStatement Compile()
		{
			Map k=key.EvaluateStructure();
			if (k != null && k.Equals(CodeKeys.Function))
			{
				if (value is Literal)
				{
					((Literal)value).literal.Compile(program);
				}
			}
			return new CompiledKeyStatement(key.Compile(program),value.Compile(program));
		}
		public Expression key;
		public KeyStatement(Expression key,Expression value, Program program,int index)
			: base(program, value,index)
		{
			this.key = key;
			key.Statement = this;
		}
	}
	public class CompiledCurrentStatement : CompiledStatement
	{
		private int index;
		public CompiledCurrentStatement(Compiled value,int index)
			: base(value)
		{
			this.index = index;
		}
		public override void AssignImplementation(ref Map context, Map value)
		{
			// fix this
			if (index == 0)
			{
				Map val = value.Copy();
				context.Strategy = val.Strategy;
			}
			else
			{
				context = value.Copy();
			}
		}
	}
	public class CurrentStatement : Statement
	{
		protected override Map CurrentImplementation(Map previous)
		{
			return value.EvaluateStructure();
		}
		public override CompiledStatement Compile()
		{
			return new CompiledCurrentStatement(value.Compile(program),Index);
		}
		public CurrentStatement(Expression value, Program program,int index)
			: base(program, value,index)
		{
		}
	}
	public class CompiledSearchStatement : CompiledStatement
	{
		private Compiled key;
		public CompiledSearchStatement(Compiled key,Compiled value):base(value)
		{
			this.key = key;
		}
		public override void AssignImplementation(ref Map context, Map value)
		{
			Map selected = context;
			Map key = this.key.Evaluate(context);
			while (!selected.ContainsKey(key))
			{
				selected = selected.Scope;
				if (selected == null)
				{
					throw new KeyNotFound(key, key.Source, null);
				}
			}
			selected[key] = value;
		}
	}
	public class SearchStatement : Statement
	{
		protected override Map CurrentImplementation(Map previous)
		{
			return previous;
		}
		public override CompiledStatement Compile()
		{
			return new CompiledSearchStatement(key.Compile(program),value.Compile(program));
		}
		private Expression key;
		public SearchStatement(Expression key,Expression value, Program program,int index)
			: base(program, value,index)
		{
			this.key = key;
			key.Statement = this;
		}
	}
	public class CompiledLiteral : Compiled
	{
		private Map literal;
		public CompiledLiteral(Map literal,Source source):base(source)
		{
			this.literal = literal;
		}
		public override Map EvaluateImplementation(Map context)
		{
			return literal.Copy();
		}
	}
	public class Literal : Expression
	{
		public override Map StructureImplementation()
		{
			return literal;
		}
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		public Map literal;
		public override Compiled Compile(Expression parent)
		{
			return new CompiledLiteral(literal,Source);
		}
		public Literal(Map code,Expression parent)
			: base(code.Source,parent)
		{
			if (code.Count != 0 && code.IsString)
			{
				this.literal = code.GetString();
			}
			else
			{
				this.literal = code;
			}
		}
	}
	public class CompiledRoot : Compiled
	{
		public CompiledRoot(Source source)
			: base(source)
		{
		}
		public override Map EvaluateImplementation(Map selected)
		{
			return Gac.gac;
		}
	}
	public class Root : Expression
	{
		public override Map StructureImplementation()
		{
			return Gac.gac;
		}
		public Root(Map code,Expression parent)
			: base(code.Source,parent)
		{
		}
		public override Compiled Compile(Expression parent)
		{
			return new CompiledRoot(Source);
		}
	}
	public class CompiledSelect : Compiled
	{
		List<Compiled> subs;
		public CompiledSelect(List<Compiled> subs,Source source):base(source)
		{
			this.subs= subs;
			if (subs[0] == null)
			{
			}
		}
		public override Map EvaluateImplementation(Map context)
		{
			Map selected = subs[0].Evaluate(context);
			for (int i = 1; i < subs.Count; i++)
			{
				Map key = subs[i].Evaluate(context);
				Map value = selected.TryGetValue(key);
				if (value == null)
				{
					throw new KeyDoesNotExist(key, subs[i].Source, selected);
				}
				else
				{
					selected = value;
				}
			}
			return selected;
		}
	}
	public class Select : Expression
	{
		public override Map StructureImplementation()
		{
			return null;
			//Map selected = subs[0].EvaluateStructure();
			//for (int i = 1; i < subs.Count; i++)
			//{
			//    Map key = subs[i].EvaluateStructure();
			//    if (key==null || key is Structure || key is Unknown || !selected.ContainsKey(key))
			//    {
			//        // compilation error???
			//        return null;
			//    }
			//    selected = selected[key];
			//}
			//return selected;
		}
		public override Compiled Compile(Expression parent)
		{
			return new CompiledSelect(subs.ConvertAll<Compiled>(delegate(Expression e)
			{
				return e.Compile(null);
			}),Source);
		}
		private List<Expression> subs = new List<Expression>();
		public Select(Map code,Expression parent)
			: base(code.Source,parent)
		{
			foreach (Map m in code.Array)
			{
				subs.Add(m.GetExpression(this));
			}
		}
	}
	public class Interpreter
	{
		public static bool profiling = false;
		static Interpreter()
		{
			try
			{
				Map map = Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta"));
				map.Scope = Gac.gac;

				LiteralExpression gac = new LiteralExpression(Gac.gac, null);

				map[CodeKeys.Function].GetExpression(gac).Statement = new LiteralStatement(gac);
				map[CodeKeys.Function].Compile(gac);

				Gac.gac["library"] = map.Call(Map.Empty);
				Gac.gac["library"].Scope = Gac.gac;


			}
			catch (Exception e)
			{
				throw e;
			}
		}
		[STAThread]
		public static void Main(string[] args)
		{
			if (args.Length != 0)
			{
				if (args[0] == "-test")
				{
					try
					{
						UseConsole();
						new MetaTest().Run();
					}
					catch (Exception e)
					{
						DebugPrint(e.ToString());
					}
					Console.ReadLine();
				}
				else if (args[0] == "-nprof")
				{
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);
				}
				else if (args[0] == "-profile")
				{
					UseConsole();
					Interpreter.profiling = true;
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"game.meta"), Map.Empty);
					List<object> results = new List<object>(Map.calls.Keys);
					results.Sort(delegate(object a, object b)
					{
						return Map.calls[b].time.CompareTo(Map.calls[a].time);
					});

					foreach (object e in results)
					{
						Console.WriteLine(e.ToString() + "    " + Map.calls[e].time + "     " + Map.calls[e].calls);
					}
					Console.ReadLine();
				}
				else if (args[0] == "-performance")
				{
					UseConsole();
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
				}
				else
				{
					string fileName = args[0].Trim('"');
					if (File.Exists(fileName))
					{
						try
						{
							MetaTest.Run(fileName, Map.Empty);
						}
						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
							Console.ReadLine();
						}
						return;
					}
					else
					{
						Console.WriteLine("File " + fileName + " not found.");
					}
				}
			}
		}
		private static void DebugPrint(string text)
		{
			if (useConsole)
			{
				Console.WriteLine(text);
				Console.ReadLine();
			}
			else
			{
				System.Windows.Forms.MessageBox.Show(text, "Meta exception");
			}
		}
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();

		public static bool useConsole = false;
		public static void UseConsole()
		{
			AllocConsole();
			useConsole = true;
			Console.SetBufferSize(80, 1000);
		}
		public static string InstallationPath
		{
			get
			{
				return @"D:\Meta\0.2\";
			}
		}
	}
	public class Transform
	{
		public static object ToDotNet(Map meta, Type target)
		{
			object dotNet; ;
			if (!TryToDotNet(meta, target, out dotNet))
			{
				TryToDotNet(meta, target, out dotNet);
				throw new ApplicationException("Cannot convert " + Serialization.Serialize(meta) + " to " + target.ToString() + ".");
			}
			return dotNet;
		}
		public static Delegate CreateDelegateFromCode(Type delegateType, Map code)
		{
			MethodInfo invoke = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters)
			{
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

			for (int i = 0; i < parameters.Length; i++)
			{
				il.Emit(OpCodes.Ldloc, local);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldarg, i + 1);
				il.Emit(OpCodes.Stelem_Ref);
			}
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Call, typeof(MetaDelegate).GetMethod("Call"));

			if (invoke.ReturnType == typeof(void))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Castclass, invoke.ReturnType);
				il.Emit(OpCodes.Ret);
			}
			return (Delegate)method.CreateDelegate(delegateType, new MetaDelegate(code, invoke.ReturnType));
		}
		public class MetaDelegate
		{
			private Map callable;
			private Type returnType;
			public MetaDelegate(Map callable, Type returnType)
			{
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments)
			{
				Map arg = new Map();
				Map pos = this.callable;
				foreach (object argument in arguments)
				{
					pos = pos.Call(Transform.ToMeta(argument));
				}
				if (returnType != typeof(void))
				{
					return Meta.Transform.ToDotNet(pos, this.returnType);
				}
				else
				{
					return null;
				}
			}
		}
		public static bool TryToDotNet(Map meta, Type target, out object dotNet)
		{
			try
			{
				dotNet = null;
				if (target.IsSubclassOf(typeof(Enum)))
				{
					dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());
				}
				else if (meta.Strategy is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta.Strategy).Type))
				{
					dotNet = ((ObjectMap)meta.Strategy).Object;
				}
				else
				{
					switch (Type.GetTypeCode(target))
					{
						case TypeCode.Boolean:
							if (meta.IsNumber && (meta.GetNumber().GetInt32() == 1 || meta.GetNumber().GetInt32() == 0))
							{
								dotNet = Convert.ToBoolean(meta.GetNumber().GetInt32());
							}
							break;
						case TypeCode.Byte:
							if (IsIntegerInRange(meta, Byte.MinValue, Byte.MaxValue))
							{
								dotNet = Convert.ToByte(meta.GetNumber().GetInt32());
							}
							break;
						case TypeCode.Char:
							if (IsIntegerInRange(meta, Char.MinValue, Char.MaxValue))
							{
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
							if (IsIntegerInRange(meta, decimal.MinValue, decimal.MaxValue))
							{
								dotNet = (decimal)(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.Double:
							if (IsIntegerInRange(meta, double.MinValue, double.MaxValue))
							{
								// fix this
								dotNet = (double)(meta.GetNumber().GetInt64());
								//dotNet = (double)(meta.GetNumber().GetDouble());
							}
							break;
						case TypeCode.Int16:
							if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue))
							{
								dotNet = Convert.ToInt16(meta.GetNumber().GetRealInt64());
								//dotNet = Convert.ToInt16(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.Int32:
							if (IsIntegerInRange(meta, Int32.MinValue, Int32.MaxValue))
							{
								dotNet = meta.GetNumber().GetInt32();
							}
							break;
						case TypeCode.Int64:
							if (IsIntegerInRange(meta, new Number(Int64.MinValue), new Number(Int64.MaxValue)))
							{
								dotNet = Convert.ToInt64(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.Object:
							if (target == typeof(Number) && meta.IsNumber)
							{
								dotNet = meta.GetNumber();
							}
							if (dotNet == null && target == typeof(Type) && meta.Strategy is TypeMap)
							{
								dotNet = ((TypeMap)meta.Strategy).Type;
							}
							// remove?
							else if (meta.Strategy is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta.Strategy).Type))
							{
								dotNet = ((ObjectMap)meta.Strategy).Object;
							}
							else if (target.IsAssignableFrom(meta.GetType()))
							{
								dotNet = meta;
							}
							// maybe remove
							else if (target.IsArray)
							{
								ArrayList list = new ArrayList();
								bool converted = true;
								Type elementType = target.GetElementType();
								foreach (Map m in meta.Array)
								{
									object o;
									if (Transform.TryToDotNet(m, elementType, out o))
									{
										list.Add(o);
									}
									else
									{
										converted = false;
										break;
									}
								}
								if (converted)
								{
									dotNet = list.ToArray(elementType);
								}
							}
							else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
							   && meta.ContainsKey(CodeKeys.Function))
							{
								dotNet = CreateDelegateFromCode(target, meta);
							}
							break;
						case TypeCode.SByte:
							if (IsIntegerInRange(meta, SByte.MinValue, SByte.MaxValue))
							{
								dotNet = Convert.ToSByte(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.Single:
							if (IsIntegerInRange(meta, Single.MinValue, Single.MaxValue))
							{
								// wrong
								dotNet = (float)meta.GetNumber().GetInt64();
								//dotNet = (float)meta.GetNumber().GetDouble();
							}
							break;
						case TypeCode.String:
							if (meta.IsString)
							{
								dotNet = meta.GetString();
							}
							break;
						case TypeCode.UInt16:
							if (IsIntegerInRange(meta, UInt16.MinValue, UInt16.MaxValue))
							{
								dotNet = Convert.ToUInt16(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.UInt32:
							if (IsIntegerInRange(meta, new Number(UInt32.MinValue), new Number(UInt32.MaxValue)))
							{
								dotNet = Convert.ToUInt32(meta.GetNumber().GetInt64());
							}
							break;
						case TypeCode.UInt64:
							if (IsIntegerInRange(meta, new Number(UInt64.MinValue), new Number(UInt64.MaxValue)))
							{
								dotNet = Convert.ToUInt64(meta.GetNumber().GetInt64());
							}
							break;
						default:
							throw new ApplicationException("not implemented");
					}
				}
				return dotNet != null;
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public static bool IsIntegerInRange(Map meta, Number minValue, Number maxValue)
		{
			return meta.IsNumber && meta.GetNumber() >= minValue && meta.GetNumber() <= maxValue;
		}
		public static Map ToMeta(object dotNet)
		{
			if (dotNet == null)
			{
				return Map.Empty;
			}
			else
			{
				Type type = dotNet.GetType();
				switch (Type.GetTypeCode(type))
				{
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
						if (type == typeof(Number))
						{
							return (Number)dotNet;
						}
						else if (type == typeof(Map))
						{
							return (Map)dotNet;
						}
						else
						{
							return new Map(dotNet);
						}
					default:
						throw new ApplicationException("Cannot convert object.");
				}
			}
		}
	}

	public delegate Map CallDelegate(Map argument);

	public class Method : MapStrategy
	{
		public override bool IsNormal
		{
			get
			{
				return false;
			}
		}
		public override int GetHashCode()
		{
			return method.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			Method method = obj as Method;
			if (method != null)
			{
				return this.method.Equals(method.method);
			}
			return false;
		}
		public override object UniqueKey
		{
			get
			{
				return method;
			}
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public override bool ContainsKey(Map key)
		{
			return false;
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				yield break;
			}
		}
		public override void Set(Map key, Map val, Map parent)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map Get(Map key)
		{
			return null;
		}
		public Method(MethodBase method, object obj, Type type, Dictionary<Map, Map> overloads)
			: this(method, obj, type)
		{
		}
		private static BindingFlags GetBindingFlags(object obj, string name)
		{
			if (name == ".ctor" || obj != null)
			{
				return BindingFlags.Public | BindingFlags.Instance;
			}
			else
			{
				return BindingFlags.Public | BindingFlags.Static;
			}
		}
		public override Map CopyData()
		{
			return new Map(this);
		}
		public MethodBase method;
		protected object obj;
		protected Type type;
		public Method(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
			this.parameters = method.GetParameters();
		}
		public override bool IsString
		{
			get
			{
				return false;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}
		public ParameterInfo[] parameters;
		public override Map CallImplementation(Map argument, Map parent)
		{
			return DecideCall(argument, new List<object>());
		}
		private Map DecideCall(Map argument, List<object> oldArguments)
		{
			List<object> arguments = new List<object>(oldArguments);
			if (parameters.Length != 0)
			{
				object arg;
				if (!Transform.TryToDotNet(argument, parameters[arguments.Count].ParameterType, out arg))
				{
					throw new Exception("Could not convert argument " + Meta.Serialization.Serialize(argument) + "\n to " + parameters[arguments.Count].ParameterType.ToString());
				}
				else
				{
					arguments.Add(arg);
				}
			}
			if (arguments.Count >= parameters.Length)
			{
				return Invoke(argument, arguments.ToArray());
			}
			else
			{
				CallDelegate call = new CallDelegate(delegate(Map map)
				{
					return DecideCall(map, arguments);
				});
				return new Map(new Method(invokeMethod, call, typeof(CallDelegate)));
			}
		}
		MethodInfo invokeMethod = typeof(CallDelegate).GetMethod("Invoke");
		private Map Invoke(Map argument, object[] arguments)
		{
			try
			{
				object result;
				if (method is ConstructorInfo)
				{
					result = ((ConstructorInfo)method).Invoke(arguments);
				}
				else
				{
					result = method.Invoke(obj, arguments);
				}
				return Transform.ToMeta(result);
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					throw e.InnerException;
				}
				else
				{
					throw new ApplicationException("implementation exception: " + e.InnerException.ToString() + e.StackTrace, e.InnerException);
				}
			}
		}
	}
	public class TypeMap : DotNetMap
	{
		private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
		protected override BindingFlags BindingFlags
		{
			get
			{
				return bindingFlags;
			}
		}
		private static MemberCache cache = new MemberCache(bindingFlags);
		protected override MemberCache MemberCache
		{
			get
			{
				return cache;
			}
		}
		public override string ToString()
		{
			return Type.ToString();
		}
		protected override object GlobalKey
		{
			get
			{
				return this;
			}
		}
		public TypeMap(Type targetType)
			: base(null, targetType)
		{
		}
		public override bool ContainsKey(Map key)
		{
			return Get(key) != null;
		}
		public override Map Get(Map key)
		{
			if (Type.IsGenericTypeDefinition && key.Strategy is TypeMap)
			{
				List<Type> types = new List<Type>();
				if (Type.GetGenericArguments().Length == 1)
				{
					types.Add(((TypeMap)key.Strategy).Type);
				}
				else
				{
					foreach (Map map in key.Array)
					{
						types.Add(((TypeMap)map.Strategy).Type);
					}
				}
				return new Map(new TypeMap(Type.MakeGenericType(types.ToArray())));
			}
			else if (Type == typeof(Array) && key.Strategy is TypeMap)
			{
				return new Map(new TypeMap(((TypeMap)key.Strategy).Type.MakeArrayType()));
			}
			else if (base.Get(key) != null)
			{
				return base.Get(key);
			}
			else
			{
				Map value;
				Data.TryGetValue(key, out value);
				return value;
			}
		}
		private Dictionary<Map, Map> data;
		private Dictionary<Map, Map> Data
		{
			get
			{
				if (data == null)
				{
					data = new Dictionary<Map, Map>();
					foreach (ConstructorInfo constructor in Type.GetConstructors())
					{
						string name = GetConstructorName(constructor);
						data[name] = new Map(new Method(constructor, this.Object, Type));
					}
				}
				return data;
			}
		}
		public static string GetConstructorName(ConstructorInfo constructor)
		{
			string name = constructor.DeclaringType.Name;
			foreach (ParameterInfo parameter in constructor.GetParameters())
			{
				name += "_" + parameter.ParameterType.Name;
			}
			return name;
		}
		public override Map CopyData()
		{
			return new Map(new TypeMap(this.Type));
		}
		private Map constructor;
		private Map Constructor
		{
			get
			{
				if (constructor == null)
				{
					ConstructorInfo method = Type.GetConstructor(new Type[] { });
					if (method == null)
					{
						throw new Exception("Default constructor for " + Type + " not found.");
					}
					constructor = new Map(new Method(method, Object, Type));
				}
				return constructor;
			}
		}
		public override Map CallImplementation(Map argument, Map parent)
		{
			Map item = Constructor.Call(Map.Empty);
			return Library.With(item, argument);
		}
	}

	public class ObjectMap : DotNetMap
	{
		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
		protected override BindingFlags BindingFlags
		{
			get
			{
				return bindingFlags;
			}
		}
		private static MemberCache cache = new MemberCache(bindingFlags);
		protected override MemberCache MemberCache
		{
			get
			{
				return cache;
			}
		}
		protected override object GlobalKey
		{
			get
			{
				return Object;
			}
		}
		public override Map CallImplementation(Map arg, Map parent)
		{
			if (this.Type.IsSubclassOf(typeof(Delegate)))
			{
				return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg, parent);
			}
			else
			{
				return base.Call(arg, parent);
			}
		}
		public override int GetHashCode()
		{
			return Object.GetHashCode();
		}
		public override bool Equals(object o)
		{
			return o is ObjectMap && Object.Equals(((ObjectMap)o).Object);
		}
		public ObjectMap(string text)
			: this(text, text.GetType())
		{
		}
		public ObjectMap(Map target)
			: this(target, target.GetType())
		{
		}
		public ObjectMap(object target, Type type)
			: base(target, type)
		{
		}
		public ObjectMap(object target)
			: base(target, target.GetType())
		{
		}
		public override string ToString()
		{
			return Object.ToString();
		}
		public override Map CopyData()
		{
			return new Map(Object);
		}
	}

	public class EmptyStrategy : MapStrategy
	{
		public override bool ContainsKey(Map key)
		{
			return false;
		}
		public override void Append(Map map, Map parent)
		{
			ListStrategy list = new ListStrategy();
			list.Append(map, parent);
			parent.Strategy = list;
		}
		public static EmptyStrategy empty = new EmptyStrategy();
		private EmptyStrategy()
		{
		}
		public override bool IsNumber
		{
			get
			{
				return true;
			}
		}
		public override int Count
		{
			get
			{
				return 0;
			}
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public override int GetHashCode()
		{
			return 0.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return ((MapStrategy)obj).Count == 0;
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				yield break;
			}
		}
		public override Map CopyData()
		{
			return new Map(this);
		}
		public override MapStrategy DeepCopy(Map key, Map value, Map map)
		{
			if (key.IsNumber)
			{
				if (key.Count == 0 && value.IsNumber)
				{
					NumberStrategy number = new NumberStrategy(0);
					number.Set(key, value, map);
					return number;
				}
				else
				{
					if (key.Equals(new Map(1)))
					{
						ListStrategy list = new ListStrategy();
						list.Append(value, map);
						return list;
					}
				}
			}
			DictionaryStrategy dictionary = new DictionaryStrategy();
			dictionary.Set(key, value, map);
			return dictionary;
		}
		public override void Set(Map key, Map val, Map map)
		{
			map.Strategy = DeepCopy(key, val, map);
		}
		public override Map Get(Map key)
		{
			return null;
		}
	}

	public class NumberStrategy : MapStrategy
	{
		public override int GetArrayCount()
		{
			return 0;
		}
		public override bool Equals(object obj)
		{
			MapStrategy strategy = obj as MapStrategy;
			if (strategy != null)
			{
				if (strategy.IsNumber && strategy.GetNumber().Equals(number))
				{
					return true;
				}
				else
				{
					return base.Equals(strategy);
				}
			}
			return false;
		}
		private Number number;
		public NumberStrategy(Number number)
		{
			this.number = number;
		}
		public override Map Get(Map key)
		{
			if (ContainsKey(key))
			{
				if (key.Equals(Map.Empty))
				{
					return number - 1;
				}
				else if (key.Equals(NumberKeys.Negative))
				{
					return Map.Empty;
				}
				else if (key.Equals(NumberKeys.Denominator))
				{
					return new Map(new Number(number.Denominator));
				}
				else
				{
					throw new ApplicationException("Error.");
				}
			}
			else
			{
				return null;
			}
		}
		public override void Set(Map key, Map value, Map map)
		{
			if (key.Equals(Map.Empty) && value.IsNumber)
			{
				this.number = value.GetNumber() + 1;
			}
			else if (key.Equals(NumberKeys.Negative) && value.Equals(Map.Empty) && number != 0)
			{
				if (number > 0)
				{
					number = 0 - number;
				}
			}
			else if (key.Equals(NumberKeys.Denominator) && value.IsNumber)
			{
				this.number = new Number(number.Numerator, value.GetNumber().GetInt32());
			}
			else
			{
				Panic(key, value, new DictionaryStrategy(), map);
			}
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				if (number != 0)
				{
					yield return Map.Empty;
				}
				if (number < 0)
				{
					yield return NumberKeys.Negative;
				}
				if (number.Denominator != 1.0d)
				{
					yield return NumberKeys.Denominator;
				}
			}
		}
		public override Map CopyData()
		{
			return new Map(new NumberStrategy(number));
		}
		public override bool IsNumber
		{
			get
			{
				return true;
			}
		}
		public override Number GetNumber()
		{
			return number;
		}
		public override int GetHashCode()
		{
			return (int)(number.Numerator % int.MaxValue);
		}
	}

	public class StringStrategy : MapStrategy
	{
		private string text;
		public StringStrategy(string text)
		{
			this.text = text;
		}
		public override int GetHashCode()
		{
			return text.Length;
		}
		public override bool Equals(object obj)
		{
			if (obj is StringStrategy)
			{
				return ((StringStrategy)obj).text == text;
			}
			else
			{
				return base.Equals(obj);
			}
		}
		public override void Set(Map key, Map val, Map map)
		{
			MapStrategy strategy;
			if (key.Equals(new Map(Count + 1)))
			{
				strategy = new ListStrategy();
			}
			else
			{
				strategy = new DictionaryStrategy();
			}
			Panic(key, val, strategy, map);
		}
		public override int Count
		{
			get
			{
				return text.Length;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return text.Length == 0;
			}
		}
		public override bool IsString
		{
			get
			{
				return true;
			}
		}
		public override string GetString()
		{
			return text;
		}
		public override Map Get(Map key)
		{
			if (key.IsNumber)
			{
				Number number = key.GetNumber();
				if (number.IsNatural && number > 0 && number <= Count)
				{
					return text[number.GetInt32() - 1];
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}
		public override bool ContainsKey(Map key)
		{
			if (key.IsNumber)
			{
				Number number = key.GetNumber();
				if (number.IsNatural)
				{
					return number > 0 && number <= text.Length;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		public override int GetArrayCount()
		{
			return text.Length;
		}
		public override Map CopyData()
		{
			return new Map(this);
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				for (int i = 1; i <= text.Length; i++)
				{
					yield return i;
				}
			}
		}
	}
	public class ListStrategy : MapStrategy
	{
		public override MapStrategy DeepCopy(Map key, Map value, Map map)
		{
			if (key.Equals(new Map(Count + 1)))
			{
				List<Map> newList = new List<Map>(this.list);
				newList.Add(value);
				return new ListStrategy(newList);
			}
			else
			{
				return base.DeepCopy(key, value, map);
			}
		}
		public override Map CopyData()
		{
			return new Map(new CloneStrategy(this));
		}
		public override void Append(Map map, Map parent)
		{
			list.Add(map);
		}
		private List<Map> list;
		public ListStrategy()
			: this(5)
		{
		}
		public ListStrategy(List<Map> list)
		{
			this.list = list;
		}
		public ListStrategy(int capacity)
		{
			this.list = new List<Map>(capacity);
		}
		public ListStrategy(ListStrategy original)
		{
			this.list = new List<Map>(original.list);
		}
		public override Map Get(Map key)
		{
			Map value = null;
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count)
				{
					value = list[integer - 1];
				}
			}
			return value;
		}
		public override void Set(Map key, Map val, Map map)
		{
			if (key.IsNumber)
			{
				int integer = key.GetNumber().GetInt32();
				if (integer >= 1 && integer <= list.Count)
				{
					list[integer - 1] = val;
				}
				else if (integer == list.Count + 1)
				{
					list.Add(val);
				}
				else
				{
					Panic(key, val, new DictionaryStrategy(), map);
				}
			}
			else
			{
				Panic(key, val, new DictionaryStrategy(), map);
			}
		}
		public override int Count
		{
			get
			{
				return list.Count;
			}
		}
		public override IEnumerable<Map> Array
		{
			get
			{
				return this.list;
			}
		}
		public override int GetArrayCount()
		{
			return this.list.Count;
		}
		public override bool ContainsKey(Map key)
		{
			bool containsKey;
			if (key.IsNumber)
			{
				Number integer = key.GetNumber();
				return integer >= 1 && integer <= list.Count;
			}
			else
			{
				return false;
			}
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				for (int i = 1; i <= list.Count; i++)
				{
					yield return i;
				}
			}
		}
	}
	public class DictionaryStrategy : MapStrategy
	{
		public override Map CopyData()
		{
			return new Map(new CloneStrategy(this));
		}
		public override bool IsNumber
		{
			get
			{
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);
			}
		}
		public override int GetArrayCount()
		{
			int i = 1;
			while (this.ContainsKey(i))
			{
				i++;
			}
			return i - 1;
		}
		private Dictionary<Map, Map> dictionary;
		public DictionaryStrategy()
			: this(2)
		{
		}
		public DictionaryStrategy(int Count)
		{
			this.dictionary = new Dictionary<Map, Map>(Count);
		}
		public DictionaryStrategy(Dictionary<Map, Map> data)
		{
			this.dictionary = data;
		}
		public override Map Get(Map key)
		{
			Map val;
			dictionary.TryGetValue(key, out val);
			return val;
		}
		public override void Set(Map key, Map value, Map map)
		{
			dictionary[key] = value;
		}
		public override bool ContainsKey(Map key)
		{
			return dictionary.ContainsKey(key);
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				return dictionary.Keys;
			}
		}
		public override int Count
		{
			get
			{
				return dictionary.Count;
			}
		}
	}
	public class CloneStrategy : MapStrategy
	{
		public override bool IsNormal
		{
			get
			{
				return original.IsNormal;
			}
		}
		public override int GetArrayCount()
		{
			return original.GetArrayCount();
		}
		private MapStrategy original;
		public CloneStrategy(MapStrategy original)
		{
			this.original = original;
		}
		public override IEnumerable<Map> Array
		{
			get
			{
				return original.Array;
			}
		}
		public override bool ContainsKey(Map key)
		{
			return original.ContainsKey(key);
		}
		public override int Count
		{
			get
			{
				return original.Count;
			}
		}
		public override Map CopyData()
		{
			MapStrategy clone = new CloneStrategy(this.original);
			return new Map(clone);
		}
		public override bool Equals(object obj)
		{
			return obj.Equals(original);
		}
		public override int GetHashCode()
		{
			return original.GetHashCode();
		}
		public override Number GetNumber()
		{
			return original.GetNumber();
		}
		public override string GetString()
		{
			return original.GetString();
		}
		public override bool IsNumber
		{
			get
			{
				return original.IsNumber;
			}
		}
		public override bool IsString
		{
			get
			{
				return original.IsString;
			}
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				return original.Keys;
			}
		}
		public override Map Get(Map key)
		{
			return original.Get(key);
		}
		public override void Set(Map key, Map value, Map map)
		{
			map.Strategy = original.DeepCopy(key, value, map);
		}
	}
	public class Profile
	{
		public double time;
		public int calls;
		public int recursive;
	}
	public abstract class MapStrategy
	{
		public virtual bool IsNormal
		{
			get
			{
				return true;
			}
		}
		public override int GetHashCode()
		{
			if (IsNumber)
			{
				return (int)(GetNumber().Numerator % int.MaxValue);
			}
			else
			{
				return Count;
			}
		}
		public override bool Equals(object obj)
		{
			MapStrategy strategy = obj as MapStrategy;
			if (strategy == null || strategy.Count != this.Count)
			{
				return false;
			}
			else
			{
				bool isEqual = true;
				foreach (Map key in this.Keys)
				{
					Map otherValue = strategy.Get(key);
					Map thisValue = Get(key);
					if (otherValue == null || otherValue.GetHashCode() != thisValue.GetHashCode() || !otherValue.Equals(thisValue))
					{
						isEqual = false;
					}
				}
				return isEqual;
			}
		}
		[DllImport("Kernel32.dll")]
		protected static extern bool QueryPerformanceCounter(
			out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		protected static extern bool QueryPerformanceFrequency(
			out long lpFrequency);

		private static long freq;

		static MapStrategy()
		{
			if (QueryPerformanceFrequency(out freq) == false)
			{
				throw new ApplicationException();
			}
		}
		public virtual string Serialize(Map parent)
		{
			return parent.SerializeDefault();
		}
		public virtual object UniqueKey
		{
			get
			{
				return Get(CodeKeys.Function).Source;
			}
		}
		public virtual Map CallImplementation(Map argument, Map parent)
		{
			if (ContainsKey(CodeKeys.Function))
			{
				if (this.Get(CodeKeys.Function).Compiled != null)
				{
					return this.Get(CodeKeys.Function).Compiled.Evaluate(parent);
				}
				else
				{
					return this.Get(CodeKeys.Function).GetExpression(null).Compile(null).Evaluate(parent);
				}
			}
			else
			{
				throw new ApplicationException("Map is not a function: " + Meta.Serialization.Serialize(parent));
			}
		}
		public Map Call(Map argument, Map parent)
		{
			long start = 0;
			if (Interpreter.profiling && UniqueKey != null)
			{
				QueryPerformanceCounter(out start);
				if (!Map.calls.ContainsKey(UniqueKey))
				{
					Map.calls[UniqueKey] = new Profile();
				}
				Map.calls[UniqueKey].calls++;
				Map.calls[UniqueKey].recursive++;
			}
			Map result = CallImplementation(argument, parent);
			if (Interpreter.profiling)
			{
				if (UniqueKey != null)
				{
					long stop;
					QueryPerformanceCounter(out stop);
					double duration = (double)(stop - start) / (double)freq;
					Map.calls[UniqueKey].recursive--;
					if (Map.calls[UniqueKey].recursive == 0)
					{
						Map.calls[UniqueKey].time += duration;
					}
				}
			}
			return result;

		}
		public virtual void Append(Map map, Map parent)
		{
			this.Set(GetArrayCount() + 1, map, parent);
		}
		public abstract void Set(Map key, Map val, Map map);
		public abstract Map Get(Map key);
		public virtual bool ContainsKey(Map key)
		{
			foreach (Map k in Keys)
			{
				if (k.Equals(key))
				{
					return true;
				}
			}
			return false;
		}
		public abstract int GetArrayCount();
		public virtual Map CopyData()
		{
			Map map = new Map();
			foreach (Map key in Keys)
			{
				map[key] = this.Get(key).Copy();
			}
			return map;
		}
		protected virtual void Panic(Map key, Map val, MapStrategy strategy, Map map)
		{
			map.Strategy = strategy;
			foreach (Map k in Keys)
			{
				strategy.Set(k, Get(k).Copy(), map);
			}
			map.Strategy.Set(key, val, map);
		}
		public virtual MapStrategy DeepCopy(Map key, Map value, Map map)
		{
			MapStrategy strategy = new DictionaryStrategy();
			foreach (Map k in Keys)
			{
				strategy.Set(k, Get(k).Copy(), map);
			}
			strategy.Set(key, value, map);
			return strategy;
		}
		public virtual bool IsNumber
		{
			get
			{
				return GetIsNumberDefault();
			}
		}
		public bool GetIsNumberDefault()
		{
			return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);
		}
		public virtual bool IsString
		{
			get
			{
				return IsStringDefault;
			}
		}
		public bool IsStringDefault
		{
			get
			{
				if (GetArrayCount() != Count)
				{
					return false;
				}
				foreach (Map m in Array)
				{
					if (!Transform.IsIntegerInRange(m, (int)Char.MinValue, (int)Char.MaxValue))
					{
						return false;
					}
				}
				return true;
			}
		}
		public string GetStringDefault()
		{
			StringBuilder text = new StringBuilder("");
			foreach (Map key in Keys)
			{
				text.Append(Convert.ToChar(this.Get(key).GetNumber().GetInt32()));
			}
			return text.ToString();
		}
		public virtual string GetString()
		{
			return GetStringDefault();
		}
		public virtual Number GetNumber()
		{
			return GetNumberDefault();
		}
		public Number GetNumberDefault()
		{
			Number number;
			if (Count == 0)
			{
				number = 0;
			}
			else if (this.Count == 1 && this.ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber)
			{
				number = 1 + this.Get(Map.Empty).GetNumber();
			}
			else
			{
				throw new ApplicationException("Map is not an integer");
			}
			return number;
		}
		public abstract IEnumerable<Map> Keys
		{
			get;
		}
		public virtual IEnumerable<Map> Array
		{
			get
			{
				for (int i = 1; this.ContainsKey(i); i++)
				{
					yield return Get(i);
				}
			}
		}
		public virtual int Count
		{
			get
			{
				int count = 0;
				foreach (Map key in Keys)
				{
					count++;
				}
				return count;
			}
		}
	}
	public abstract class Member
	{
		public abstract void Set(object obj, Map value);
		public abstract Map Get(object obj);
	}
	public class TypeMember : Member
	{
		public override void Set(object obj, Map value)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		private Type type;
		public TypeMember(Type type)
		{
			this.type = type;
		}
		public override Map Get(object obj)
		{
			return new Map(new TypeMap(type));
		}
	}
	public class FieldMember : Member
	{
		private FieldInfo field;
		public FieldMember(FieldInfo field)
		{
			this.field = field;
		}
		public override void Set(object obj, Map value)
		{
			field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
		}
		public override Map Get(object obj)
		{
			return Transform.ToMeta(field.GetValue(obj));
		}
	}
	public class MethodMember : Member
	{
		private MethodBase method;
		public MethodMember(MethodInfo method)
		{
			this.method = method;
		}
		public override void Set(object obj, Map value)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map Get(object obj)
		{
			return new Map(new Method(method, obj, method.DeclaringType));
		}
	}
	public class MemberCache
	{
		private BindingFlags bindingFlags;
		public MemberCache(BindingFlags bindingFlags)
		{
			this.bindingFlags = bindingFlags;
		}
		public Dictionary<Map, Member> GetMembers(Type type)
		{
			if (!cache.ContainsKey(type))
			{
				Dictionary<Map, Member> data = new Dictionary<Map, Member>();
				foreach (MemberInfo member in type.GetMembers(bindingFlags))
				{
					MethodInfo method = member as MethodInfo;
					if (method != null)
					{
						string name = TypeMap.GetMethodName(method);
						data[name] = new MethodMember(method);
					}
					FieldInfo field = member as FieldInfo;
					if (field != null)
					{
						data[field.Name] = new FieldMember(field);
					}
					Type t = member as Type;
					if (t != null)
					{
						data[t.Name] = new TypeMember(t);
					}
				}
				cache[type] = data;
			}
			else
			{
			}
			return cache[type];
		}
		private Dictionary<Type, Dictionary<Map, Member>> cache = new Dictionary<Type, Dictionary<Map, Member>>();
	}
	public abstract class DotNetMap : MapStrategy
	{
		public override bool IsNormal
		{
			get
			{
				return false;
			}
		}
		public override int GetHashCode()
		{
			// inconsistent with global key, maybe
			if (obj != null)
			{
				return obj.GetHashCode();
			}
			else
			{
				return type.GetHashCode();
			}
		}
		public override bool Equals(object obj)
		{
			DotNetMap dotNet = obj as DotNetMap;
			if (dotNet != null)
			{
				return dotNet.Object == Object && dotNet.Type == Type;
			}
			return false;
		}
		public override object UniqueKey
		{
			get
			{
				return null;
			}
		}
		protected abstract BindingFlags BindingFlags
		{
			get;
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public object Object
		{
			get
			{
				return obj;
			}
		}
		public Type Type
		{
			get
			{
				return type;
			}
		}
		protected abstract object GlobalKey
		{
			get;
		}
		public static string GetMethodName(MethodInfo method)
		{
			string name = method.Name;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				name += "_" + parameter.ParameterType.Name;
			}
			return name;
		}
		private Dictionary<Map, Member> data;
		private Dictionary<Map, Member> Members
		{
			get
			{
				if (data == null)
				{
					data = MemberCache.GetMembers(type);
				}
				return data;
			}
		}
		protected abstract MemberCache MemberCache
		{
			get;
		}
		private object obj;
		private Type type;
		public DotNetMap(object obj, Type type)
		{
			this.obj = obj;
			this.type = type;
		}
		public override Map Get(Map key)
		{
			if (Members.ContainsKey(key))
			{
				return Members[key].Get(obj);
			}
			if (global.ContainsKey(GlobalKey) && global[GlobalKey].ContainsKey(key))
			{
				return global[GlobalKey][key];
			}
			return null;
		}
		public override void Set(Map key, Map value, Map parent)
		{
			if (Members.ContainsKey(key))
			{
				Members[key].Set(obj, value);
			}
			else
			{
				if (!global.ContainsKey(GlobalKey))
				{
					global[GlobalKey] = new Dictionary<Map, Map>();
				}
				global[GlobalKey][key] = value;
			}
		}
		public static Type GetListAddFunctionType(IList list, Map value)
		{
			foreach (MemberInfo member in list.GetType().GetMember("Add"))
			{
				if (member is MethodInfo)
				{
					MethodInfo method = (MethodInfo)member;
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1)
					{
						ParameterInfo parameter = parameters[0];
						bool c = true;
						foreach (Map entry in value.Array)
						{
							object o;
							if (!Transform.TryToDotNet(entry, parameter.ParameterType, out o))
							{
								c = false;
								break;
							}
						}
						if (c)
						{
							return parameter.ParameterType;
						}
					}
				}
			}
			return null;
		}
		public static Dictionary<object, Dictionary<Map, Map>> global = new Dictionary<object, Dictionary<Map, Map>>();
		public override bool ContainsKey(Map key)
		{
			return Get(key) != null;
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				foreach (Map key in Members.Keys)
				{
					yield return key;
				}
				if (global.ContainsKey(GlobalKey))
				{
					foreach (Map key in global[GlobalKey].Keys)
					{
						yield return key;
					}
				}
			}
		}
		public override bool IsString
		{
			get
			{
				return false;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}
		public override string Serialize(Map parent)
		{
			if (obj != null)
			{
				return this.obj.ToString();
			}
			else
			{
				return this.type.ToString();
			}
		}
		public Delegate CreateEventDelegate(string name, Map code)
		{
			EventInfo eventInfo = type.GetEvent(name, BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static | BindingFlags.Instance);
			Delegate eventDelegate = Transform.CreateDelegateFromCode(eventInfo.EventHandlerType, code);
			return eventDelegate;
		}
	}
	public interface ISerializeEnumerableSpecial
	{
		string Serialize();
	}
	public class TestAttribute : Attribute
	{
		public TestAttribute()
			: this(1)
		{
		}
		public TestAttribute(int level)
		{
			this.level = level;
		}
		private int level;
		public int Level
		{
			get
			{
				return level;
			}
		}
	}
	[AttributeUsage(AttributeTargets.Property)]
	public class SerializeAttribute : Attribute
	{
		public SerializeAttribute()
			: this(1)
		{
		}
		public SerializeAttribute(int level)
		{
			this.level = level;
		}
		private int level;
		public int Level
		{
			get
			{
				return level;
			}
		}
	}
	public abstract class TestRunner
	{
		public static string TestDirectory
		{
			get
			{
				return Path.Combine(Interpreter.InstallationPath, "Test");
			}
		}
		public abstract class Test
		{
			public bool RunTest()
			{
				int level;
				Console.Write(this.GetType().Name + "...");
				DateTime startTime = DateTime.Now;
				object result = GetResult(out level);
				TimeSpan duration = DateTime.Now - startTime;
				string testDirectory = Path.Combine(TestDirectory, this.GetType().Name);
				string resultPath = Path.Combine(testDirectory, "result.txt");
				string checkPath = Path.Combine(testDirectory, "check.txt");
				Directory.CreateDirectory(testDirectory);
				if (!File.Exists(checkPath))
				{
					File.Create(checkPath).Close();
				}
				StringBuilder stringBuilder = new StringBuilder();
				Serialize(result, "", stringBuilder, level);
				File.WriteAllText(resultPath, stringBuilder.ToString(), Encoding.UTF8);
				string successText;
				bool success = File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath));
				if (success)
				{
					successText = "succeeded";
				}
				else
				{
					successText = "failed";
				}
				Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
				return success;
			}
			public abstract object GetResult(out int level);
		}
		public void Run()
		{
			bool allTestsSucessful = true;
			foreach (Type testType in this.GetType().GetNestedTypes())
			{
				if (testType.IsSubclassOf(typeof(Test)))
				{
					Test test = (Test)testType.GetConstructor(new Type[] { }).Invoke(null);
					if (!test.RunTest())
					{
						allTestsSucessful = false;
					}
				}
			}
			if (!allTestsSucessful)
			{
				Console.ReadLine();
			}
		}
		public const char indentationChar = '\t';

		private static bool UseToStringMethod(Type type)
		{
			return (!type.IsValueType || type.IsPrimitive)
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();
		}
		private static bool UseProperty(PropertyInfo property, int level)
		{
			object[] attributes = property.GetCustomAttributes(typeof(SerializeAttribute), false);
			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public static void Serialize(object obj, string indent, StringBuilder builder, int level)
		{
			if (obj == null)
			{
				builder.Append(indent + "null\n");
			}
			else if (UseToStringMethod(obj.GetType()))
			{
				builder.Append(indent + "\"" + obj.ToString() + "\"" + "\n");
			}
			else
			{
				foreach (PropertyInfo property in obj.GetType().GetProperties())
				{
					if (UseProperty((PropertyInfo)property, level))
					{
						object val = property.GetValue(obj, null);
						builder.Append(indent + property.Name);
						if (val != null)
						{
							builder.Append(" (" + val.GetType().Name + ")");
						}
						builder.Append(":\n");
						Serialize(val, indent + indentationChar, builder, level);
					}
				}
				string specialEnumerableSerializationText;
				if (obj is ISerializeEnumerableSpecial && (specialEnumerableSerializationText = ((ISerializeEnumerableSpecial)obj).Serialize()) != null)
				{
					builder.Append(indent + specialEnumerableSerializationText + "\n");
				}
				else if (obj is System.Collections.IEnumerable)
				{
					foreach (object entry in (System.Collections.IEnumerable)obj)
					{
						builder.Append(indent + "Entry (" + entry.GetType().Name + ")\n");
						Serialize(entry, indent + indentationChar, builder, level);
					}
				}
			}
		}
	}
	public class Source
	{
		public override string ToString()
		{
			return FileName + ", " + "line " + Line + ", column " + Column;
		}
		public readonly int Line;
		public readonly int Column;
		public readonly string FileName;
		public Source(int line, int column,string fileName)
		{
			this.Line = line;
			this.Column = column;
			this.FileName = fileName;
		}
	}
	public class Gac : MapStrategy
	{
		public override bool IsNormal
		{
			get
			{
				return false;
			}
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public static readonly Map gac = new Map(new Gac());
		private Gac()
		{
			cache["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		public static Map LoadAssembly(Assembly assembly)
		{
			Map val = new Map();
			foreach (Type type in assembly.GetExportedTypes())
			{
				if (type.DeclaringType == null)
				{
					Map selected = val;
					string name;
					if (type.IsGenericTypeDefinition)
					{
						name = type.Name.Split('`')[0];
					}
					else
					{
						name = type.Name;
					}
					selected[type.Name] = new Map(new TypeMap(type));
					foreach (ConstructorInfo constructor in type.GetConstructors())
					{
						if (constructor.GetParameters().Length != 0)
						{
							selected[TypeMap.GetConstructorName(constructor)] = new Map(new Method(constructor, null, type));
						}

					}
				}
			}
			return val;
		}
		public override Map Get(Map key)
		{
			Map value;
			if (!cache.ContainsKey(key))
			{
				if (key.IsString)
				{
					try
					{
						Assembly assembly;
						string path = Path.Combine(Directory.GetCurrentDirectory(), key.GetString() + ".dll");
						if (File.Exists(path))
						{
							assembly = Assembly.LoadFile(path);
						}
						else
						{
							assembly = Assembly.LoadWithPartialName(key.GetString());
						}
						value = LoadAssembly(assembly);
						cache[key] = value;
					}
					catch (Exception e)
					{
						value = null;
					}
				}
				else
				{
					value = null;
				}
			}
			else
			{
				value = cache[key];
			}
			return value;
		}
		public override void Set(Map key, Map val, Map map)
		{
			cache[key] = val;
		}
		public override Map CopyData()
		{
			return new Map(this);
		}
		public override IEnumerable<Map> Keys
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override bool ContainsKey(Map key)
		{
			return Get(key) != null;
		}
	}
	public class Number
	{
		public int CompareTo(Number number)
		{
			return GetDouble().CompareTo(number.GetDouble());
		}
		public bool IsNatural
		{
			get
			{
				return denominator == 1.0d;
			}
		}
		private readonly double numerator;
		private readonly double denominator;
		public static Number Parse(string text)
		{
			try
			{
				string[] parts = text.Split('/');
				int numerator = Convert.ToInt32(parts[0]);
				int denominator;
				if (parts.Length > 2)
				{
					denominator = Convert.ToInt32(parts[2]);
				}
				else
				{
					denominator = 1;
				}
				return new Number(numerator, denominator);
			}
			catch (Exception e)
			{
				return null;
			}
		}
		public Number(double integer)
			: this(integer, 1)
		{
		}
		public Number(Number i)
			: this(i.numerator, i.denominator)
		{
		}
		public Number(double numerator, double denominator)
		{
			double greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
			if (denominator < 0)
			{
				numerator = -numerator;
				denominator = -denominator;
			}
			this.numerator = numerator / greatestCommonDivisor;
			this.denominator = denominator / greatestCommonDivisor;
		}
		public double Numerator
		{
			get
			{
				return numerator;
			}
		}
		public double Denominator
		{
			get
			{
				return denominator;
			}
		}
		public static Number operator |(Number a, Number b)
		{
			return Convert.ToInt32(a.numerator) | Convert.ToInt32(b.numerator);
		}
		public override string ToString()
		{
			if (denominator == 1)
			{
				return numerator.ToString();
			}
			else
			{
				return numerator.ToString() + Syntax.fraction + denominator.ToString();
			}
		}
		public Number Clone()
		{
			return new Number(this);
		}
		public static implicit operator Number(double number)
		{
			return new Number(number);
		}
		public static implicit operator Number(decimal number)
		{
			return new Number((double)number);
		}
		public static implicit operator Number(int integer)
		{
			return new Number((double)integer);
		}
		public static bool operator ==(Number a, Number b)
		{
			return !ReferenceEquals(b, null) && a.numerator == b.numerator && a.denominator == b.denominator;
		}
		public static bool operator !=(Number a, Number b)
		{
			return !(a == b);
		}
		private static double GreatestCommonDivisor(double a, double b)
		{
			a = Math.Abs(a);
			b = Math.Abs(b);
			while (a != 0 && b != 0)
			{
				if (a > b)
				{
					a = a % b;
				}
				else
				{
					b = b % a;
				}
			}
			if (a == 0)
			{
				return b;
			}
			else
			{
				return a;
			}
		}
		private static double LeastCommonMultiple(Number a, Number b)
		{
			return a.denominator * b.denominator / GreatestCommonDivisor(a.denominator, b.denominator);
		}
		public static Number operator %(Number a, Number b)
		{
			return Convert.ToInt32(a.Numerator) % Convert.ToInt32(b.Numerator);
		}
		public static Number operator +(Number a, Number b)
		{
			return new Number(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a, b));
		}
		public static Number operator /(Number a, Number b)
		{
			return new Number(a.numerator * b.denominator, a.denominator * b.numerator);
		}
		public static Number operator -(Number a, Number b)
		{
			return new Number(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a, b));
		}
		public static Number operator *(Number a, Number b)
		{
			return new Number(a.numerator * b.numerator, a.denominator * b.denominator);
		}
		public double Expand(Number b)
		{
			return numerator * (LeastCommonMultiple(this, b) / denominator);
		}
		public static bool operator >(Number a, Number b)
		{
			return a.Expand(b) > b.Expand(a);
		}
		public static bool operator <(Number a, Number b)
		{
			return a.Expand(b) < b.Expand(a);
		}
		public static bool operator >=(Number a, Number b)
		{
			return a.Expand(b) >= b.Expand(a);
		}
		public static bool operator <=(Number a, Number b)
		{
			return a.Expand(b) <= b.Expand(a);
		}
		public override bool Equals(object o)
		{
			if (!(o is Number))
			{
				return false;
			}
			Number b = (Number)o;
			return b.numerator == numerator && b.denominator == denominator;
		}
		public override int GetHashCode()
		{
			Number x = new Number(this);
			while (x > int.MaxValue)
			{
				x = x - int.MaxValue;
			}
			return x.GetInt32();
		}
		public double GetDouble()
		{
			return numerator / denominator;
		}
		public int GetInt32()
		{
			return Convert.ToInt32(numerator / denominator);
		}
		public long GetRealInt64()
		{
			return Convert.ToInt64(numerator / denominator);
		}
		public long GetInt64()
		{
			return Convert.ToInt64(numerator);
		}
	}
	public class Syntax
	{
		public const char arrayStart = '[';
		public const char arrayEnd=']';
		public const char arraySeparator=';';
		public const char programSeparator = ';';
		public const char programStart = '{';
		public const char programEnd = '}';
		public const char functionProgram = '?';
		public const char lastArgument = '@';
		public const char autokey = '.';
		public const char callSeparator=',';
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
		public readonly static char[] integer = new char[] {
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public readonly static char[] lookupStringForbidden = new char[] {
			current, lastArgument, explicitCall, indentation, '\r', '\n',
			function, @string,emptyMap, '!', root, callStart, callEnd, 
			character, programStart, '*', '$', '\\', '<', '=', arrayStart,
			'-', ':', functionProgram, select, ' ', '-', '[', ']', '*', '>', 
			programStart, programSeparator ,callSeparator,programEnd,
			arrayEnd,arraySeparator};
		public readonly static char[] lookupStringForbiddenFirst = new char[] {
			current, lastArgument, explicitCall, indentation, '\r', '\n', select,
			function, @string, emptyMap, '!', root, callStart, callEnd, character,
			programStart, '*', '$', '\\', '<', '=', arrayStart, '-', '0', '1', '2',
			'3', '4', '5', '6', '7', '8', '9', '.', functionProgram, select, ' ',
			'-', '[', ']', '*', '>', programStart, programSeparator ,callSeparator,
			programEnd,arraySeparator,arrayEnd};
	}
	public class Parser
	{
		public bool End
		{
			get
			{
				return index >= text.Length - 1;
			}
		}
		private bool negative = false;
		public string text;
		public int index;
		private string fileName;
		private int line = 1;
		private int column = 1;
		private int indentationCount = -1;
		public Stack<int> defaultKeys = new Stack<int>();
		public Parser(string text, string filePath)
		{
			this.index = 0;
			this.text = text;
			this.fileName = filePath;
		}

		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(
				LastArgument,
				FunctionProgram,
				LiteralExpression,
				Call,
				SelectInline,
				List,
				Search,
				Program
			);
		});
		public static Rule EndOfLine =
			new Sequence(
				new ZeroOrMore(
					new Alternatives(
						Syntax.space,
						Syntax.tab)),
				new Alternatives(
						Syntax.unixNewLine,
						Syntax.windowsNewLine));

		public static Rule Integer =
			new Sequence(
				new CustomProduction(
					delegate(Parser p, Map map, ref Map result)
					{
						p.negative = map != null;
						return null;
					},
					new Optional(Syntax.negative)),
				new ReferenceAssignment(
					new Sequence(
						new ReferenceAssignment(
							new OneOrMore(
								new CustomProduction(
									delegate(Parser p, Map map, ref Map result)
									{
										if (result == null)
										{
											result = new Map();
										}
										result = result.GetNumber() * 10 + (Number)map.GetNumber().GetInt32() - '0';
										return result;
									},
									new Character(Syntax.integer)))),
						new CustomProduction(delegate(Parser p, Map map, ref Map result)
		{
			if (result.GetNumber() > 0 && p.negative)
			{
				result = 0 - result.GetNumber();
			}
			return null;
		},
		new CustomRule(delegate(Parser p, out bool matched)
		{
			matched = true; return null;
		})))));


		public static Rule StartOfFile = new CustomRule(delegate(Parser p, out bool matched)
		{
			if(p.indentationCount==-1)
			{
				p.indentationCount++;
				matched = true;
			}
			else
			{
				matched = false;
			}
			return null;
		});

		private static Rule SmallIndentation = new CustomRule(delegate(Parser p, out bool matched)
		{
			p.indentationCount++;
			matched = true;
			return null;
		});

		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			return StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa, out matched);
		});

		private static Rule StringLine = new ZeroOrMore(new Autokey(new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0])));

		public static Rule CharacterDataExpression = new Sequence(
			Syntax.character,
			new ReferenceAssignment(new CharacterExcept(Syntax.character)),
			Syntax.character);

		public static Rule Dedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			pa.indentationCount--;
			matched = true;
			return null;
		});
		private static void MatchStringLine(Parser parser, StringBuilder text)
		{
			bool matching = true;
			for (; matching && parser.index < parser.text.Length; parser.index++)
			{
				char c = parser.Look();
				switch (c)
				{
					case '\n':
					case '\r':
						matching = false;
						break;
					default:
						text.Append(c);
						break;
				}
			}
		}
		private static Rule StringBeef = new CustomRule(delegate(Parser parser, out bool matched)
		{
			StringBuilder result = new StringBuilder(100);
			MatchStringLine(parser, result);
			matched = true;
			while (true)
			{
				bool lineMatched;
				new Sequence(
					EndOfLine,
					SameIndentation).Match(parser, out lineMatched);
				if (lineMatched)
				{
					result.Append('\n');
					MatchStringLine(parser, result);
				}
				else
				{
					break;
				}
			}
			return result.ToString();
		});

		private static Rule SingleString = new OneOrMore(
			new Autokey(
				new CharacterExcept(
					Syntax.unixNewLine,
					Syntax.windowsNewLine[0],
					Syntax.@string)));

		public static Rule String = new Sequence(
			Syntax.@string,
			new ReferenceAssignment(new Alternatives(
				SingleString,
				new Sequence(
					SmallIndentation,
					EndOfLine,
					SameIndentation,
					new ReferenceAssignment(StringBeef),
					EndOfLine,
					Dedentation,
					SameIndentation
			))),
			Syntax.@string);

		public static Rule Number = new Sequence(
			new ReferenceAssignment(Integer),
			new Assignment(
					NumberKeys.Denominator,
					new Optional(
						new Sequence(
							Syntax.fraction,
							new ReferenceAssignment(Integer)))));

		public static Rule LookupString = new Sequence(
			new Assignment(1,new CharacterExcept(Syntax.lookupStringForbiddenFirst)),
			new Append(new ZeroOrMore(
				new Autokey(
					new CharacterExcept(
					Syntax.lookupStringForbidden)))));

		public static Rule ExpressionData = new DelayedRule(delegate()
		{
			return Parser.Expression;
		});

		public static Rule Value = new DelayedRule(delegate
		{
			return new Alternatives(
				Map,
				ListMap,
				String,
				Number,
				CharacterDataExpression);
		});
		private static Rule LookupAnything = new Sequence(
			'<',
			new ReferenceAssignment(Value));

		public static Rule Function = new Sequence(
			new Assignment(
				CodeKeys.Parameter,
				new ZeroOrMore(
					new Autokey(
						new CharacterExcept(
							Syntax.@string,
							Syntax.function,
							Syntax.indentation,
							Syntax.windowsNewLine[0],
							Syntax.unixNewLine)))),
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
					delegate(Parser parser, Map map, ref Map result)
					{
						result = new Map(result[1], map);
						return result;
					},
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
				delegate(Parser p)
				{
					p.defaultKeys.Push(1);
				},
				new Sequence(
					new ReferenceAssignment(
						new OneOrMore(
							new Merge(
								new Sequence(
									SameIndentation,
									new ReferenceAssignment(Entry))))),
					Dedentation),
				delegate(Parser p)
				{
					p.defaultKeys.Pop();
				})));

		public static Rule File = new Sequence(
			new Optional(
				new Sequence(
					'#',
					'!',
					new ZeroOrMore(new CharacterExcept(Syntax.unixNewLine)),
					EndOfLine)),
			new ReferenceAssignment(Map));
		public static Rule ComplexStuff(Map key, char start, char end, Rule separator, Rule entry, Rule first)
		{
			return ComplexStuff(key, start, end, separator, new Assignment(1, entry), new ReferenceAssignment(entry), first);
		}
		public static Rule ComplexStuff(Map key, char start, char end, Rule separator, Action firstAction, Action entryAction, Rule first)
		{
			return new Sequence(
				new Assignment(key, ComplexStuff(start, end, separator, firstAction, entryAction, first)));
		}

		public static Rule ComplexStuff(char start, char end, Rule separator, Action firstAction, Action entryAction, Rule first)
		{
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
										separator!=null?new Match(separator):null,
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
							new Optional(Dedentation)))));
		}
		//public static Rule ComplexStuff(Map key, char start, char end, Rule separator, Action firstAction,Action entryAction, Rule first)
		//{
		//    return new Sequence(
		//        new Assignment(key,
		//            new Sequence(
		//                first != null ? new Assignment(1, first) : null,
		//                start,
		//                new Append(
		//                    new Alternatives(
		//                        new Sequence(
		//                            firstAction,
		//                            new Append(
		//                                new ZeroOrMore(
		//                                    new Autokey(
		//                                        new Sequence(
		//                                            separator,
		//                                            entryAction)))),
		//                            new Optional(end)),
		//                    new Sequence(
		//                        SmallIndentation,
		//                        new ReferenceAssignment(
		//                            new ZeroOrMore(
		//                                new Autokey(
		//                                    new Sequence(
		//                                        new Optional(EndOfLine),
		//                                        SameIndentation,
		//                                        entryAction)))),
		//                            new Optional(EndOfLine),
		//                            new Optional(Dedentation)))))));
		//}
		public static Rule Call = new DelayedRule(delegate()
		{
			return ComplexStuff(CodeKeys.Call, Syntax.callStart, Syntax.callEnd, Syntax.callSeparator,
				new Alternatives(
                    LastArgument,
                    FunctionProgram,
                    LiteralExpression,
                    Call,
                    SelectInline,
                    List,
                    Search,
                    Program),
				new Alternatives(
					LastArgument,
					FunctionProgram,
					LiteralExpression,
					SelectInline,
					Search,
					List,
					Program));
		});
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

		private static Rule Simple(char c,Map literal)
		{
			return new Sequence(
				c,
				new ReferenceAssignment(new LiteralRule(literal)));
		}

		private static Rule EmptyMap = Simple(
			Syntax.emptyMap,
			Meta.Map.Empty);

		private static Rule Current = Simple(
			Syntax.current,
			new Map(CodeKeys.Current, Meta.Map.Empty));

		public static Rule LastArgument = Simple(
			Syntax.lastArgument,
			new Map(CodeKeys.LastArgument,new Map()));

		private static Rule Root = Simple(
			Syntax.root,
			new Map(CodeKeys.Root, Meta.Map.Empty));

		private static Rule LiteralExpression = new Sequence(
			new Assignment(CodeKeys.Literal,new Alternatives(
				Number,
				EmptyMap,
				String,
				CharacterDataExpression)));

		private static Rule LookupAnythingExpression = new Sequence(
			'<',
			new ReferenceAssignment(Expression),
			new Optional('>'));

		private static Rule LookupStringExpression = new Sequence(
			new Assignment(
				CodeKeys.Literal,
				LookupString));


		private static Rule Search = new Sequence(
			new Assignment(
				CodeKeys.Search,
				new Alternatives(
					new Sequence(
						'!',
						new ReferenceAssignment(Expression)),
					new Alternatives(
						LookupStringExpression,
						LookupAnythingExpression))));


		private static Rule SelectInline = new Sequence(
			new Assignment(
				CodeKeys.Select,
				new Sequence(
					new Assignment(1,
						new Alternatives(
							Root,
							Search,
							LiteralExpression)),
					new Append(
						new OneOrMore(new Autokey(new Sequence(
							'.',
							new ReferenceAssignment(
								new Alternatives(
									LookupStringExpression,
									LookupAnythingExpression,
									LiteralExpression)))))))));





		public static Rule ListMap = new Sequence(
			Syntax.arrayStart,
			new ReferenceAssignment(
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
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
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));

		public static Action ListAction = new CustomProduction(
			delegate(Parser p, Map map, ref Map result)
			{
				result = new Map(
					CodeKeys.Key, new Map(
							CodeKeys.Literal, p.defaultKeys.Peek()),
					CodeKeys.Value, map);
				p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
				return result;
			},
			Expression);

		public static Rule List = new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
			ComplexStuff(
				CodeKeys.Program,
				Syntax.arrayStart,
				Syntax.arrayEnd,
				Syntax.arraySeparator,
				ListAction,
				ListAction,
				null
				),
				delegate(Parser p)
				{
					p.defaultKeys.Pop();
				});

		public static Rule ComplexStatement(Rule rule,Action action)
		{
			return new Sequence(
				action,
				rule!=null?new Match(rule):null,
				new Assignment(CodeKeys.Value, Expression),
				new Optional(EndOfLine));
		}
		public static Rule DiscardStatement = ComplexStatement(
			null,
			new Assignment(CodeKeys.Discard, new LiteralRule(new Map())));
		public static Rule CurrentStatement = ComplexStatement(
			'&',
			new Assignment(CodeKeys.Current, new LiteralRule(Meta.Map.Empty)));

		public static Rule NormalStatement = ComplexStatement(
			'=',
			new Assignment(
				CodeKeys.Key,
				new Alternatives(
					new Sequence(
						'<',
						new ReferenceAssignment(Expression)),
					new Sequence(
						new Assignment(
							CodeKeys.Literal,
							LookupString)),
					Expression)));

		public static Rule Statement = ComplexStatement(
			':',
			new ReferenceAssignment(
				new Sequence(
					new Assignment(
						CodeKeys.Keys,
						new Alternatives(
							new Sequence(
								new Assignment(
									CodeKeys.Literal,
									LookupString)),
							Expression)))));

		public static Rule AllStatements = new Alternatives(
			FunctionExpression,
		CurrentStatement,
		NormalStatement,
		Statement,
		DiscardStatement);

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
											new ZeroOrMore(
											new Autokey(
												new CharacterExcept(Syntax.lookupStringForbiddenFirst)))),
										Syntax.functionProgram,
											new Assignment(CodeKeys.Expression,Expression),
										new Optional(EndOfLine)
			)))))))));



		public static Rule Program = ComplexStuff(
			CodeKeys.Program,
			Syntax.programStart,
			Syntax.programEnd,
			Syntax.programSeparator,
			AllStatements,
			null);

		public abstract class Action
		{
			public static implicit operator Action(string s)
			{
				return new Match(StringRule(s));
			}
			public static implicit operator Action(char c)
			{
				return new Match(new Character(c));
			}
			public static implicit operator Action(Rule rule)
			{
				return new Match(rule);
			}
			private Rule rule;
			protected abstract void Effect(Parser parser, Map map, ref Map result);
			public Action(char c)
				: this(new Character(c))
			{
			}
			public Action(Rule rule)
			{
				this.rule = rule;
			}
			public bool Execute(Parser parser, ref Map result)
			{
				bool matched;
				Map map = rule.Match(parser, out matched);
				if (matched)
				{
					Effect(parser, map, ref result);
				}
				return matched;
			}
		}
		public class Autokey : Action
		{
			public Autokey(Rule rule)
				: base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				result.Append(map);
			}
		}
		public class Assignment : Action
		{
			private Map key;
			public Assignment(Map key,Rule rule):base(rule)
			{
				this.key = key;
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				if (map != null)
				{
					result[key] = map;
				}
			}
		}
		public class Match : Action
		{
			public Match(Rule rule)
				: base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
			}
		}
		public class ReferenceAssignment : Action
		{
			public ReferenceAssignment(Rule rule)
				: base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				result = map;
			}
		}
		public class Append : Action
		{
			public Append(Rule rule)
				: base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				foreach (Map m in map.Array)
				{
					result.Append(m);
				}
			}
		}
		public class Join : Action
		{
			public Join(Rule rule)
				: base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				result = Library.Join(result, map);
			}
		}
		public class Merge : Action
		{
			public Merge(Rule rule):base(rule)
			{
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				result = Library.Merge(result, map);
			}
		}
		public class CustomProduction : Action
		{
			private CustomActionDelegate action;
			public CustomProduction(CustomActionDelegate action,Rule rule):base(rule)
			{
				this.action = action;
			}
			protected override void Effect(Parser parser, Map map, ref Map result)
			{
				this.action(parser, map, ref result);
			}
		}
		public delegate Map CustomActionDelegate(Parser p, Map map, ref Map result);

		public abstract class Rule
		{
			public static implicit operator Rule(string s)
			{
				return StringRule(s);
			}
			public static implicit operator Rule(char c)
			{
				return new Character(c);
			}
			public Map Match(Parser parser, out bool matched)
			{
				int oldIndex = parser.index;
				int oldLine = parser.line;
				int oldColumn = parser.column;
				int oldIndentation = parser.indentationCount;
				Map result = MatchImplementation(parser, out matched);
				if (!matched)
				{
					parser.index = oldIndex;
					parser.line = oldLine;
					parser.column = oldColumn;
					parser.indentationCount = oldIndentation;
				}
				else
				{
					if (result != null)
					{
						if (result.IsString)
						{
							result = new Map(result.GetString());
						}
						result.Source = new Source(oldLine, oldColumn, parser.fileName);
					}
				}
				return result;
			}
			protected abstract Map MatchImplementation(Parser parser, out bool match);
		}
		public class Character : CharacterRule
		{
			public Character(params char[] characters)
				: base(characters)
			{
			}
			protected override bool MatchCharacer(char c)
			{
				return c.ToString().IndexOfAny(characters) != -1 && c != Syntax.endOfFile;
			}
		}
		public class CharacterExcept : CharacterRule
		{
			public CharacterExcept(params char[] characters)
				: base(characters)
			{
			}
			protected override bool MatchCharacer(char c)
			{
				return c.ToString().IndexOfAny(characters) == -1 && c != Syntax.endOfFile;
			}
		}
		public abstract class CharacterRule : Rule
		{
			public CharacterRule(char[] chars)
			{
				this.characters = chars;
			}
			protected char[] characters;
			protected abstract bool MatchCharacer(char c);
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				char character = parser.Look();
				if (MatchCharacer(character))
				{
					matched = true;
					parser.index++;
					parser.column++;
					if (character == Syntax.unixNewLine)
					{
						parser.line++;
						parser.column = 1;
					}
					return character;
				}
				else
				{
					matched = false;
					return null;
				}
			}
		}
		public delegate void PrePostDelegate(Parser parser);
		public class PrePost : Rule
		{
			private PrePostDelegate pre;
			private PrePostDelegate post;
			private Rule rule;
			public PrePost(PrePostDelegate pre, Rule rule, PrePostDelegate post)
			{
				this.pre = pre;
				this.rule = rule;
				this.post = post;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				pre(parser);
				Map result = rule.Match(parser, out matched);
				post(parser);
				return result;
			}
		}
		// remove?
		public static Rule StringRule(string text)
		{
			List<Action> actions = new List<Action>();
			foreach (char c in text)
			{
				actions.Add(c);
			}
			return new Sequence(actions.ToArray());
		}
		public delegate Map ParseFunction(Parser parser, out bool matched);
		public class CustomRule : Rule
		{
			private ParseFunction parseFunction;
			public CustomRule(ParseFunction parseFunction)
			{
				this.parseFunction = parseFunction;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				return parseFunction(parser, out matched);
			}
		}
		public delegate Rule RuleFunction();
		public class DelayedRule : Rule
		{
			private RuleFunction ruleFunction;
			private Rule rule;
			public DelayedRule(RuleFunction ruleFunction)
			{
				this.ruleFunction = ruleFunction;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				if (rule == null)
				{
					rule = ruleFunction();
				}
				return rule.Match(parser, out matched);
			}
		}
		public class Alternatives : Rule
		{
			private Rule[] cases;
			public Alternatives(params Rule[] cases)
			{
				this.cases = cases;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map result = null;
				matched = false;
				foreach (Rule expression in cases)
				{
					result = (Map)expression.Match(parser, out matched);
					if (matched)
					{
						break;
					}
				}
				return result;
			}
		}
		public class Sequence : Rule
		{
			private Action[] actions;
			public Sequence(params Action[] rules)
			{
				this.actions = rules;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map result = new Map();
				bool success = true;
				foreach (Action action in actions)
				{
					if (action != null)
					{
						bool matched = action.Execute(parser, ref result);
						if (!matched)
						{
							success = false;
							break;
						}
					}
				}
				if (!success)
				{
					match = false;
					return null;
				}
				else
				{
					match = true;
					return result;
				}
			}
		}
		public class LiteralRule : Rule
		{
			private Map literal;
			public LiteralRule(Map literal)
			{
				this.literal = literal;
			}
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				matched = true;
				return literal;
			}
		}
		public class ZeroOrMoreString : ZeroOrMore
		{
			public ZeroOrMoreString(Action action)
				: base(action)
			{
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map result = base.MatchImplementation(parser, out match);
				if (match && result.IsString)
				{
					result = result.GetString();
				}
				return result;
			}
		}
		public class ZeroOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = new Map(new ListStrategy());
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
				}
				matched = true;
				return list;
			}
			private Action action;
			public ZeroOrMore(Action action)
			{
				this.action = action;
			}
		}
		public class OneOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = new Map(new ListStrategy());
				matched = false;
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
					matched = true;
				}
				return list;
			}
			private Action action;
			public OneOrMore(Action action)
			{
				this.action = action;
			}
		}
		public class Optional : Rule
		{
			private Rule rule;
			public Optional(Rule rule)
			{
				this.rule = rule;
			}
			protected override Map MatchImplementation(Parser parser, out bool match)
			{
				Map matched = rule.Match(parser, out match);
				if (matched == null)
				{
					match = true;
					return null;
				}
				else
				{
					match = true;
					return matched;
				}
			}
		}
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		public int Line
		{
			get
			{
				return line;
			}
		}
		private string Rest
		{
			get
			{
				return text.Substring(index);
			}
		}
		public int Column
		{
			get
			{
				return column;
			}
		}
		private char Look()
		{
			if (index < text.Length)
			{
				return text[index];
			}
			else
			{
				return Syntax.endOfFile;
			}
		}
		public static Map Parse(string file)
		{
			return ParseString(System.IO.File.ReadAllText(file), file);
		}
		public static Map ParseString(string text, string fileName)
		{
			Parser parser = new Parser(text, fileName);
			bool matched;
			Map result = Parser.File.Match(parser, out matched);
			if (parser.index != parser.text.Length)
			{
				throw new SyntaxException("Expected end of file.", parser);
			}
			return result;
		}
	}
	public class Serialization
	{
		public static string Serialize(Map map)
		{
			try
			{
				return Serialize(map, -1).Trim();
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private static string Number(Map map)
		{
			return map.GetNumber().ToString();
		}

		private static string Serialize(Map map, int indentation)
		{
			if (!map.Strategy.IsNormal)
			{
				return map.Strategy.ToString();
			}
			else if (map.Count == 0)
			{
				if (indentation < 0)
				{
					return "";
				}
				else
				{
					return "0";
				}
			}
			else if (map.IsNumber)
			{
				return Number(map);
			}
			else if (map.IsString)
			{
				return String(map, indentation);
			}
			else
			{
				return Map(map, indentation);
			}
		}
		private static string Map(Map map, int indentation)
		{
			string text;
			if (indentation < 0)
			{
				text = "";
			}
			else
			{
				text = "," + Environment.NewLine;
			}
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				text += Entry(indentation, entry);
			}
			return text;
		}

		private static string Entry(int indentation, KeyValuePair<Map, Map> entry)
		{
			if (entry.Key.Equals(CodeKeys.Function))
			{
				return Function(entry.Value, indentation + 1);
			}
			else
			{
				return (Indentation(indentation + 1)
					+ Key(indentation, entry) +
					"=" +
					Serialize(entry.Value, indentation + 1)
					+ Environment.NewLine).TrimEnd('\r', '\n')
					+ Environment.NewLine;
			}
		}
		private static string Literal(Map value, int indentation)
		{
			if (value.IsNumber)
			{
				return Number(value);
			}
			else if (value.IsString)
			{
				return String(value, indentation);
			}
			throw new Exception("not implemented");
		}
		private static string Function(Map value, int indentation)
		{
			return value[CodeKeys.Parameter].GetString() + "|" + Expression(value[CodeKeys.Expression], indentation);
		}
		private static string Root()
		{
			return "/";
		}
		private static string Expression(Map map, int indentation)
		{
			if (map.ContainsKey(CodeKeys.Literal))
			{
				return Literal(map[CodeKeys.Literal], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Root))
			{
				return Root();
			}
			if (map.ContainsKey(CodeKeys.Call))
			{
				return Call(map[CodeKeys.Call], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Program))
			{
				return Program(map[CodeKeys.Program], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Select))
			{
				return Select(map[CodeKeys.Select], indentation);
			}
			else if (map.ContainsKey(CodeKeys.Search))
			{
				return Search(map[CodeKeys.Search], indentation);
			}
			return Serialize(map, indentation);
		}
		private static string FunctionStatement(Map map, int indentation)
		{
			return map[CodeKeys.Parameter].GetString() + "|" +
				Expression(map[CodeKeys.Expression], indentation);
		}
		private static string KeyStatement(Map map, int indentation)
		{
			Map key = map[CodeKeys.Key];
			if (key.Equals(new Map(CodeKeys.Literal, CodeKeys.Function)))
			{
				return FunctionStatement(map[CodeKeys.Value][CodeKeys.Literal], indentation);
			}
			else
			{
				return Expression(map[CodeKeys.Key], indentation) + "="
					+ Expression(map[CodeKeys.Value], indentation);
			}
		}
		private static string CurrentStatement(Map map, int indentation)
		{
			return "&=" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string SearchStatement(Map map, int indentation)
		{
			return Expression(map[CodeKeys.Keys], indentation) + ":" + Expression(map[CodeKeys.Value], indentation);
		}
		private static string DiscardStatement(Map map, int indentation)
		{
			return Expression(map[CodeKeys.Value], indentation);
		}
		private static string Statement(Map map, int indentation)
		{
			if (map.ContainsKey(CodeKeys.Key))
			{
				return KeyStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Current))
			{
				return CurrentStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Keys))
			{
				return SearchStatement(map, indentation);
			}
			else if (map.ContainsKey(CodeKeys.Value))
			{
				return DiscardStatement(map, indentation);
			}
			throw new Exception("not implemented");
		}
		private static string Program(Map map, int indentation)
		{
			string text = "," + NewLine();
			indentation++;
			foreach (Map m in map.Array)
			{
				text += Indentation(indentation) + Trim(Statement(m, indentation)) + NewLine();
			}
			return text;
		}
		private static string Trim(string text)
		{
			return text.TrimEnd('\n', '\r');
		}
		private static string NewLine()
		{
			return Environment.NewLine;
		}
		private static string EmptyMap()
		{
			return "0";
		}
		private static string Call(Map map, int indentation)
		{
			string text = "-" + NewLine();
			indentation++;
			foreach (Map m in map.Array)
			{
				text += Indentation(indentation) +
					Trim(Expression(m, indentation)) + NewLine();
			}
			return text;
		}
		private static string Select(Map map, int indentation)
		{
			string text = "." + NewLine();
			indentation++;
			foreach (Map sub in map.Array)
			{
				text += Indentation(indentation) +
					Trim(Expression(sub, indentation)) + NewLine();
			}
			return text;
		}
		private static string Search(Map map, int indentation)
		{
			return "!" + Expression(map, indentation);
		}
		private static string Key(int indentation, KeyValuePair<Map, Map> entry)
		{
			if (entry.Key.Count != 0 && entry.Key.IsString)
			{
				string key = entry.Key.GetString();
				if (key.IndexOfAny(Syntax.lookupStringForbidden) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst) != 0)
				{
					return entry.Key.GetString();
				}
			}
			return Serialize(entry.Key, indentation + 1);
		}
		private static string String(Map map, int indentation)
		{
			string text = map.GetString();
			if (text.Contains("\"") || text.Contains("\n"))
			{
				string result = "\"" + Environment.NewLine;
				foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
				{
					result += Indentation(indentation) + "\t" + line + Environment.NewLine;
				}
				return result.Trim('\n', '\r') + Environment.NewLine + Indentation(indentation) + "\"";
			}
			else
			{
				return "\"" + text + "\"";
			}
		}
		private static string Indentation(int indentation)
		{
			return "".PadLeft(indentation, '\t');
		}
	}
	namespace Test
	{
		public class MetaTest : TestRunner
		{
			public static int Leaves(Map map)
			{
				int count = 0;
				foreach (KeyValuePair<Map, Map> pair in map)
				{
					if (pair.Value.IsNumber)
					{
						count++;
					}
					else
					{
						count += Leaves(pair.Value);
					}
				}
				return count;
			}
			public static string TestPath
			{
				get
				{
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public class Serialization : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return Meta.Serialization.Serialize(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
				}
			}
			public class Basic : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta"), new Map(1, "first argument", 2, "second argument"));
				}
			}
			public class Library : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);
				}
			}
			public static Map Run(string path, Map argument)
			{
				Map callable = Parser.Parse(path);
				callable.Scope = Gac.gac["library"];
				LiteralExpression gac = new LiteralExpression(Gac.gac, null);
				LiteralExpression lib=new LiteralExpression(Gac.gac["library"],gac);
				lib.Statement = new LiteralStatement(gac);

				callable[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
				callable[CodeKeys.Function].Compile(lib);
				return callable.Call(argument);
			}
		}
		namespace TestClasses
		{
			public class MemberTest
			{
				public static string classField = "default";
				public string instanceField = "default";
				public static string OverloadedMethod(string argument)
				{
					return "string function, argument+" + argument;
				}
				public static string OverloadedMethod(int argument)
				{
					return "integer function, argument+" + argument;
				}
				public static string OverloadedMethod(MemberTest memberTest, int argument)
				{
					return "MemberTest function, argument+" + memberTest + argument;
				}
				public static string ClassProperty
				{
					get
					{
						return classField;
					}
					set
					{
						classField = value;
					}
				}
				public string InstanceProperty
				{
					get
					{
						return this.instanceField;
					}
					set
					{
						this.instanceField = value;
					}
				}
			}
			public delegate object IntEvent(object intArg);
			public delegate object NormalEvent(object sender);
			public class TestClass
			{
				public class NestedClass
				{
					public static int field = 0;
				}
				public TestClass()
				{
				}
				public object CallInstanceEvent(object intArg)
				{
					return instanceEvent(intArg);
				}
				public static object CallStaticEvent(object sender)
				{
					return staticEvent(sender);
				}
				public event IntEvent instanceEvent;
				public static event NormalEvent staticEvent;
				protected string x = "unchangedX";
				protected string y = "unchangedY";
				protected string z = "unchangedZ";

				public static bool boolTest = false;

				public static object TestClass_staticEvent(object sender)
				{
					MethodBase[] m = typeof(TestClass).GetMethods();
					return null;
				}
				public delegate string TestDelegate(string x);
				public static Delegate del;
				public static void TakeDelegate(TestDelegate d)
				{
					del = d;
				}
				public static object GetResultFromDelegate()
				{
					return del.DynamicInvoke(new object[] { "argumentString" });
				}
				public double doubleValue = 0.0;
				public float floatValue = 0.0F;
				public decimal decimalValue = 0.0M;
			}
			public class PositionalNoConversion : TestClass
			{
				public PositionalNoConversion(string p1, string b, string p2)
				{
					this.x = p1;
					this.y = b;
					this.z = p2;
				}
				public string Concatenate(string p1, string b, string c)
				{
					return p1 + b + c + this.x + this.y + this.z;
				}
			}
			public class NamedNoConversion : TestClass
			{
				public NamedNoConversion(Map arg)
				{
					Map def = new Map();
					def[1] = "null";
					def["y"] = "null";
					def["p2"] = "null";
					if (arg.ContainsKey(1))
					{
						def[1] = arg[1];
					}
					if (arg.ContainsKey("y"))
					{
						def["y"] = arg["y"];
					}
					if (arg.ContainsKey("p2"))
					{
						def["y2"] = arg["y2"];
					}
					this.x = def[1].GetString();
					this.y = def["y"].GetString();
					this.z = def["p2"].GetString();
				}
				public string Concatenate(Map arg)
				{
					Map def = new Map();
					def[1] = "null";
					def["b"] = "null";
					def["c"] = "null";

					if (arg.ContainsKey(1))
					{
						def[1] = arg[1];
					}
					if (arg.ContainsKey("b"))
					{
						def["b"] = arg["b"];
					}
					if (arg.ContainsKey("c"))
					{
						def["c"] = arg["c"];
					}
					return def[1].GetString() + def["b"].GetString() + def["c"].GetString() +
						this.x + this.y + this.z;
				}
			}
			public class IndexerNoConversion : TestClass
			{
				public string this[string a]
				{
					get
					{
						return this.x + this.y + this.z + a;
					}
					set
					{
						this.x = a + value;
					}
				}
			}
		}
	}
	public class CodeKeys
	{
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
	public class NumberKeys
	{
		public static readonly Map Negative = "negative";
		public static readonly Map Denominator = "denominator";
	}
	public class ExceptionLog
	{
		public ExceptionLog(Source source)
		{
			this.source = source;
		}
		public Source source;
	}
	public class MetaException : Exception
	{
		private string message;
		private Source source;
		private List<ExceptionLog> invocationList = new List<ExceptionLog>();
		public MetaException(string message, Source source)
		{
			this.message = message;
			this.source = source;
		}
		public List<ExceptionLog> InvocationList
		{
			get
			{
				return invocationList;
			}
		}
		public override string ToString()
		{
			string message = Message;
			if (invocationList.Count != 0)
			{
				message += "\n\nStack trace:";
			}
			foreach (ExceptionLog log in invocationList)
			{
				message += "\n" + GetSourceText(log.source);
			}

			return message;
		}
		public static string GetSourceText(Source source)
		{
			string text;
			if (source != null)
			{
				text = source.FileName + ", line ";
				text += source.Line + ", column " + source.Column;
			}
			else
			{
				text = "Unknown location";
			}
			return text;
		}
		public override string Message
		{
			get
			{
				return GetSourceText(source) + ": " + message;
			}
		}
		public Source Source
		{
			get
			{
				return source;
			}
		}
		public static int CountLeaves(Map map)
		{
			int count = 0;
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				if (pair.Value == null)
				{
					count++;
				}
				else if (pair.Value.IsNumber)
				{
					count++;
				}
				else
				{
					count += CountLeaves(pair.Value);
				}
			}
			return count;
		}
	}
	public class SyntaxException : MetaException
	{
		public SyntaxException(string message, Parser parser)
			: base(message, new Source(parser.Line, parser.Column, parser.FileName))
		{
		}
	}
	public class ExecutionException : MetaException
	{
		private Map context;
		public ExecutionException(string message, Source source, Map context)
			: base(message, source)
		{
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException
	{
		public KeyDoesNotExist(Map key, Source source, Map map)
			: base("Key does not exist: " + Serialization.Serialize(key) + " in " + Serialization.Serialize(map), source, map)
		{
		}
	}
	public class KeyNotFound : ExecutionException
	{
		public KeyNotFound(Map key, Source source, Map map)
			: base("Key not found: " + Serialization.Serialize(key), source, map)
		{
		}
	}
	public class Library
	{
		public static Map Double(Map d)
		{
			return new Map((object)(float)d.GetNumber().GetDouble());
		}
		private static Random random = new Random();
		public static Map Random(int max)
		{
			return random.Next(max) + 1;
		}
		public static string Trim(string text)
		{
			Events.KeyboardDown += new KeyboardEventHandler(Events_KeyboardDown);
			return text.Trim();
		}
		static void Events_KeyboardDown(object sender, KeyboardEventArgs e)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public static Map SplitString(Map text, Map chars)
		{
			List<Map> result = new List<Map>();
			foreach (string part in text.GetString().Split((char[])Transform.ToDotNet(chars, typeof(char[])), StringSplitOptions.RemoveEmptyEntries))
			{
				result.Add(part);
			}
			return new Map(new ListStrategy(result));
		}
		public static Map Modify(Map map, Map func)
		{
			Map result = new Map();
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				result[entry.Key] = func.Call(entry.Value);
			}
			return result;
		}
		public static Map StringToNumber(Map map)
		{
			return Convert.ToInt32(map.GetString());
		}
		public static Map Foreach(Map map, Map func)
		{
			List<Map> result = new List<Map>();
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				result.Add(func.Call(entry.Key).Call(entry.Value));
			}
			return new Map(new ListStrategy(result));
		}
		public static Map Switch(Map map, Map cases)
		{
			foreach (KeyValuePair<Map, Map> entry in cases)
			{
				if (Convert.ToBoolean(entry.Key.Call(map).GetNumber().GetInt32()))
				{
					return entry.Value.Call(map);
				}
			}
			return Meta.Map.Empty;
		}
		public static Map Raise(Number a, Number b)
		{
			return new Number(Math.Pow(a.GetDouble(), b.GetDouble()));
		}
		public static int CompareNumber(Number a, Number b)
		{
			return a.CompareTo(b);
		}
		public static Map Sort(Map array, Map function)
		{
			List<Map> result = new List<Map>(array.Array);
			result.Sort(delegate(Map a, Map b)
			{
				return (int)Transform.ToDotNet(function.Call(a).Call(b), typeof(int));
			});
			return new Map(result);
		}
		public static Map Equal(Map a, Map b)
		{
			return a.Equals(b);
		}
		public static Map Filter(Map array, Map condition)
		{
			List<Map> result = new List<Map>();
			foreach (Map m in array.Array)
			{
				if (Convert.ToBoolean(condition.Call(m).GetNumber().GetInt32()))
				{
					result.Add(m);
				}
			}
			return new Map(result);
		}
		public static Map ElseIf(Map condition, Map then, Map els)
		{
			if (Convert.ToBoolean(condition.GetNumber().GetInt32()))
			{
				return then.Call(new Map());
			}
			else
			{
				return els.Call(new Map());
			}
		}
		public static Map Sum(Map func, Map arg)
		{
			IEnumerator<Map> enumerator = arg.Array.GetEnumerator();
			if (enumerator.MoveNext())
			{
				Map result = enumerator.Current.Copy();
				while (enumerator.MoveNext())
				{
					result = func.Call(result).Call(enumerator.Current);
				}
				return result;
			}
			else
			{
				return Meta.Map.Empty;
			}
		}
		public static Map JoinAll(Map arrays)
		{
			List<Map> result = new List<Map>();
			foreach (Map array in arrays.Array)
			{
				result.AddRange(array.Array);
			}
			return new Map(result);
		}
		public static Map If(Map condition, Map then)
		{
			if (!condition.Equals(new Map()))
			{
				return then.Call(new Map());
			}
			return new Map();
		}
		public static Map Map(Map array, Map func)
		{
			List<Map> result = new List<Map>();
			foreach (Map map in array.Array)
			{
				result.Add(func.Call(map));
			}
			return new Map(result);
		}
		public static Map Append(Map array, Map item)
		{
			array.Append(item);
			return array;
		}
		public static Map EnumerableToArray(Map map)
		{
			List<Map> result = new List<Map>();
			foreach (object entry in (IEnumerable)(((ObjectMap)map.Strategy)).Object)
			{
				result.Add(Transform.ToMeta(entry));
			}
			return new Map(result);
		}
		public static Map Reverse(Map arg)
		{
			List<Map> list = new List<Map>(arg.Array);
			list.Reverse();
			return new Map(list);
		}
		public static Map Try(Map tryFunction, Map catchFunction)
		{
			try
			{
				return tryFunction.Call(Meta.Map.Empty);
			}
			catch (Exception e)
			{
				return catchFunction.Call(new Map(e));
			}
		}

		public static Map With(Map o, Map values)
		{
			if (!(o.Strategy is ObjectMap))
			{
			}
			object obj = ((ObjectMap)o.Strategy).Object;
			Type type = obj.GetType();
			foreach (KeyValuePair<Map, Map> entry in values)
			{
				Map value = entry.Value;
				//if (entry.Key.Strategy is ObjectMap)
				//{
				//    DependencyProperty key = (DependencyProperty)((ObjectMap)entry.Key.Strategy).Object;
				//    type.GetMethod("SetValue", new Type[] { typeof(DependencyProperty), typeof(Object) }).Invoke(obj, new object[] { key, Transform.ToDotNet(value, key.PropertyType) });
				//}
				//else
				//{
				MemberInfo[] members = type.GetMember(entry.Key.GetString());
				if (members.Length != 0)
				{
					MemberInfo member = members[0];
					if (member is FieldInfo)
					{
						FieldInfo field = (FieldInfo)member;
						field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
					}
					else if (member is PropertyInfo)
					{
						PropertyInfo property = (PropertyInfo)member;
						if (typeof(IList).IsAssignableFrom(property.PropertyType) && !(value.Strategy is ObjectMap))
						{
							if (value.ArrayCount != 0)
							{
								IList list = (IList)property.GetValue(obj, null);
								list.Clear();
								Type t = DotNetMap.GetListAddFunctionType(list, value);
								if (t == null)
								{
									t = DotNetMap.GetListAddFunctionType(list, value);
									throw new ApplicationException("Cannot convert argument.");
								}
								else
								{
									foreach (Map map in value.Array)
									{
										list.Add(Transform.ToDotNet(map, t));
									}
								}
							}
						}
						else
						{
							object converted = Transform.ToDotNet(value, property.PropertyType);
							property.SetValue(obj, converted, null);
						}

					}
					else if (member is EventInfo)
					{
						EventInfo eventInfo = (EventInfo)member;
						new Map(new Method(eventInfo.GetAddMethod(), obj, type)).Call(value);
					}
					else
					{
						throw new Exception("unknown member type");
					}
				}
				else
				{
					o[entry.Key] = entry.Value;
				}
				//}
			}
			return o;
		}
		[Compilable]
		public static Map MergeAll(Map array)
		{
			Map result = new Map();
			foreach (Map map in array.Array)
			{
				foreach (KeyValuePair<Map, Map> pair in map)
				{
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		[Compilable]
		public static Map Merge(Map arg, Map map)
		{
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				arg[pair.Key] = pair.Value;
			}
			return arg;
		}
		public static Map Join(Map arg, Map map)
		{
			foreach (Map m in map.Array)
			{
				arg.Append(m);
			}
			return arg;
		}
		public static Map Range(Map arg)
		{
			int end = arg.GetNumber().GetInt32();
			Map result = new Map();
			for (int i = 1; i <= end; i++)
			{
				result.Append(i);
			}
			return result;
		}
	}
	public class LiteralExpression : Expression
	{
		private Map literal;
		public LiteralExpression(Map literal,Expression parent)
			: base(null, parent)
		{
			this.literal = literal;
		}
		public override Map StructureImplementation()
		{
			return literal;
		}
		public override Compiled Compile(Expression parent)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class LiteralStatement : Statement
	{
		private LiteralExpression program;
		public LiteralStatement(LiteralExpression program):base(null,null,0)
		{
			this.program = program;
		}
		public override Map Pre()
		{
			return program.EvaluateStructure();
		}
		protected override Map CurrentImplementation(Map previous)
		{
			return program.EvaluateStructure();
		}
		public override CompiledStatement Compile()
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class Structure: Map
	{
		public override bool IsConstant
		{
			get
			{
				return false;
			}
		}
	}
	public class Unknown:Map
	{
		public override bool IsConstant
		{
			get
			{
				return false;
			}
		}
	}
	public abstract class ScopeExpression:Expression
	{
		public ScopeExpression(Source source, Expression parent):base(source,parent)
		{
		}
	}
	public delegate T SingleDelegate<T>(T t);
	public class CompilableAttribute:Attribute
	{
		public SingleDelegate<Structure> structure;
	}
	public class Map : IEnumerable<KeyValuePair<Map, Map>>, ISerializeEnumerableSpecial
	{
		public virtual bool IsConstant
		{
			get
			{
				return true;
			}
		}
		private MapStrategy strategy;
		public Map(IEnumerable<Map> list)
			: this(new ListStrategy(new List<Map>(list)))
		{
		}
		public Map(System.Collections.Generic.ICollection<Map> list)
			: this(new ListStrategy())
		{
			int index = 1;
			foreach (object entry in list)
			{
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}
		public Map()
			: this(EmptyStrategy.empty)
		{
		}
		public Map(Map map)
			: this(new ObjectMap(map))
		{
		}
		public Map(object o)
			: this(new ObjectMap(o))
		{
		}
		public Map(string text)
			: this(new StringStrategy(text))
		{
		}
		public Map(Number number)
			: this(new NumberStrategy(number))
		{
		}
		public Map(MapStrategy strategy)
		{
			this.strategy = strategy;
		}
		public Map(params Map[] keysAndValues)
			: this()
		{
			for (int i = 0; i <= keysAndValues.Length - 2; i += 2)
			{
				this[keysAndValues[i]] = keysAndValues[i + 1];
			}
		}
		public MapStrategy Strategy
		{
			get
			{
				return strategy;
			}
			set
			{
				strategy = value;
			}
		}
		public int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public int ArrayCount
		{
			get
			{
				return strategy.GetArrayCount();
			}
		}
		public bool IsString
		{
			get
			{
				return strategy.IsString;
			}
		}
		public bool IsNumber
		{
			get
			{
				return strategy.IsNumber;
			}
		}
		public IEnumerable<Map> Array
		{
			get
			{
				return strategy.Array;
			}
		}
		public Number GetNumber()
		{
			return strategy.GetNumber();
		}
		public string GetString()
		{
			return strategy.GetString();
		}
		public static Stack<Map> arguments = new Stack<Map>();
		public Map Call(Map arg)
		{
			arguments.Push(arg);
			Map result = strategy.Call(arg, this);
			arguments.Pop();
			return result;
		}
		public static Dictionary<object, Profile> calls = new Dictionary<object, Profile>();
		public void Append(Map map)
		{
			strategy.Append(map, this);
		}
		public bool ContainsKey(Map key)
		{
			return strategy.ContainsKey(key);
		}
		public override bool Equals(object obj)
		{
			Map map = obj as Map;
			if (map != null)
			{
				if (map.strategy.IsNormal == strategy.IsNormal)
				{
					return strategy.Equals(map.Strategy);
				}
				return false;
			}
			return false;
		}
		public Map TryGetValue(Map key)
		{
			return strategy.Get(key);
		}
		public IEnumerable<Map> Keys
		{
			get
			{
				return strategy.Keys;
			}
		}
		public Map this[Map key]
		{
			get
			{
				Map value = TryGetValue(key);
				if (value == null)
				{
					throw new KeyDoesNotExist(key, null, this);
				}
				return value;
			}
			set
			{
				if (value == null)
				{
					throw new Exception("Value cannot be null.");
				}
				else
				{
					strategy.Set(key, value, this);
				}
			}
		}
		public override string ToString()
		{
			if (IsString)
			{
				return GetString();
			}
			else
			{
				return Meta.Serialization.Serialize(this);
			}
		}
		private Expression expression;
		public Statement GetStatement(Program program,int index)
		{
			if (ContainsKey(CodeKeys.Keys))
			{
				return new SearchStatement(this[CodeKeys.Keys].GetExpression(program),this[CodeKeys.Value].GetExpression(program), program,index);
			}
			else if (ContainsKey(CodeKeys.Current))
			{
				return new CurrentStatement(this[CodeKeys.Value].GetExpression(program), program,index);
			}
			else if (ContainsKey(CodeKeys.Key))
			{
				return new KeyStatement(this[CodeKeys.Key].GetExpression(program),this[CodeKeys.Value].GetExpression(program), program,index);
			}
			else if (ContainsKey(CodeKeys.Discard))
			{
				return new DiscardStatement(program, this[CodeKeys.Value].GetExpression(program),index);
			}
			else
			{
				throw new ApplicationException("Cannot compile map");
			}
		}
		public Compiled Compiled;
		public void Compile(Expression parent)
		{
			Compiled = this.GetExpression(parent).Compile(parent);
		}

		public Expression GetExpression(Expression parent)
		{
			if (expression == null)
			{
				expression = CreateExpression(parent);
			}
			return expression;
		}
		public Expression CreateExpression(Expression parent)
		{
			if (ContainsKey(CodeKeys.Call))
			{
				return new Call(this[CodeKeys.Call], this.TryGetValue(CodeKeys.Parameter),parent);
			}
			else if (ContainsKey(CodeKeys.Program))
			{
				return new Program(this[CodeKeys.Program],parent);
			}
			else if (ContainsKey(CodeKeys.Literal))
			{
				return new Literal(this[CodeKeys.Literal],parent);
			}
			else if (ContainsKey(CodeKeys.Select))
			{
				return new Select(this[CodeKeys.Select],parent);
			}
			else if (ContainsKey(CodeKeys.Search))
			{
				return new Search(this[CodeKeys.Search],parent);
			}
			else if (ContainsKey(CodeKeys.Root))
			{
				return new Root(this[CodeKeys.Root],parent);
			}
			else if (ContainsKey(CodeKeys.LastArgument))
			{
				return new LastArgument(this[CodeKeys.LastArgument],parent);
			}
			else if (ContainsKey(CodeKeys.Expression))
			{
				Program program = new Program(this,parent);
				program.isFunction = true;
				Map parameter = this[CodeKeys.Parameter];
				if (parameter.Count!=0)
				{
					KeyStatement s = new KeyStatement(
						new Literal(parameter, program),
						new LastArgument(Map.Empty, program), program, 0);
					program.statementList.Add(s);
				}
				else
				{
				}
				CurrentStatement c=new CurrentStatement(this[CodeKeys.Expression].GetExpression(program),program,program.statementList.Count);
				program.statementList.Add(c);
				return program;
			}
			else
			{
				throw new ApplicationException("Cannot compile map " + Meta.Serialization.Serialize(this));
			}
		}
		public int GetArrayCountDefault()
		{
			int i = 1;
			while (ContainsKey(i))
			{
				i++;
			}
			return i - 1;
		}

		public Map Copy()
		{
			Map clone = strategy.CopyData();
			clone.Scope = Scope;
			clone.Source = Source;
			clone.Compiled = Compiled;
			return clone;
		}
		public override int GetHashCode()
		{
			return strategy.GetHashCode();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public IEnumerator<KeyValuePair<Map, Map>> GetEnumerator()
		{
			foreach (Map key in Keys)
			{
				yield return new KeyValuePair<Map, Map>(key, this[key]);
			}
		}
		public Source Source;
		public static Map Empty = new Map(EmptyStrategy.empty);
		public static implicit operator Map(string text)
		{
			return new Map(text);
		}
		public static implicit operator Map(Number integer)
		{
			return new Map(integer);
		}
		public static implicit operator Map(double number)
		{
			return new Number(number);
		}
		public static implicit operator Map(decimal number)
		{
			return (double)number;
		}
		public static implicit operator Map(float number)
		{
			return (double)number;
		}
		public static implicit operator Map(bool boolean)
		{
			return Convert.ToDouble(boolean);
		}
		public static implicit operator Map(char character)
		{
			return (double)character;
		}
		public static implicit operator Map(byte integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(sbyte integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(uint integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(ushort integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(int integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(long integer)
		{
			return (double)integer;
		}
		public static implicit operator Map(ulong integer)
		{
			return (double)integer;
		}
		public Map Scope;
		public string SerializeDefault()
		{
			string text;
			if (this.Count == 0)
			{
				text = "0";
			}
			else if (this.IsString)
			{
				text = "\"" + this.GetString() + "\"";
			}
			else if (this.IsNumber)
			{
				text = this.GetNumber().ToString();
			}
			else
			{
				text = null;
			}
			return text;
		}
		public string Serialize()
		{
			return strategy.Serialize(this);
		}
	}
}
