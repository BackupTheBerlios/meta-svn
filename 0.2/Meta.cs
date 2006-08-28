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
//
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
//	BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
//	ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//	SOFTWARE.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Meta;
using Meta.Test;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Drawing;
using System.Security.Cryptography;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Windows;
using SdlDotNet;

namespace Meta
{
	public delegate Map Eval(Map context);

	public class New : ILExpression
	{
		public override Type Type
		{
			get
			{
				return constructor.DeclaringType;
			}
		}
		private ConstructorInfo constructor;
		private ILEmitter[] arguments;
		public New(ConstructorInfo constructor,params ILEmitter[] arguments)
		{
			this.constructor = constructor;
			this.arguments = arguments;
		}
		public override void Evaluate(ILGenerator il)
		{
			foreach (ILEmitter argument in arguments)
			{
				argument.Emit(il);
			}
			il.Emit(OpCodes.Newobj, constructor);
		}
	}
	public class Load:ILEmitter
	{
		private Storage storage;
		public Load(Storage storage)
		{
			this.storage = storage;
		}
		public override void Emit(ILGenerator il)
		{
			storage.Load(il);
		}
	}
	public abstract class Storage:ILExpression
	{
		private Type type;
		public Storage()
		{
			type = typeof(Map);
		}
		public ILEmitter Assign(ILEmitter b)
		{
			return (CustomEmitter)delegate(ILGenerator il)
			{
				b.Emit(il);
				Store(il);
			};
		}
		public abstract void Store(ILGenerator il);
		public abstract void Load(ILGenerator il);

	}
	public class InstanceCall : ILExpression
	{
		public override Type Type
		{
			get 
			{
				return method.ReturnType;
			}
		}
		private List<ILEmitter> arguments;
		private MethodInfo method;
		public InstanceCall(ILEmitter instance, MethodInfo method, params ILEmitter[] arguments)
		{
			this.method = method;
			this.arguments = new List<ILEmitter>();
			this.arguments.Add(instance);
			this.arguments.AddRange(arguments);
		}
		public override void Evaluate(ILGenerator il)
		{
			Emit(il);
		}
		public override void Emit(ILGenerator il)
		{
			foreach (ILEmitter argument in arguments)
			{
				argument.Emit(il);
			}
			il.Emit(OpCodes.Callvirt, method);
		}
	}
	public class Integer : ILEmitter
	{
		private int integer;
		public Integer(int integer)
		{
			this.integer = integer;
		}
		public override void Emit(ILGenerator il)
		{
			il.Emit(OpCodes.Ldc_I4, integer);
		}
	}
	public class InstanceField : ILExpression
	{
		public override Type Type
		{
			get 
			{
				return field.FieldType;
			}
		}
		private FieldInfo field;
		private ILEmitter instance;
		public InstanceField(ILEmitter instance, FieldInfo field)
		{
			this.field = field;
			this.instance = instance;
		}
		public void Load(ILGenerator il)
		{
			instance.Emit(il);
			il.Emit(OpCodes.Ldfld, field);
		}
		public override void Evaluate(ILGenerator il)
		{
			Load(il);
		}
	}
	public class StaticField : Storage
	{
		public override Type Type
		{
			get 
			{
				return field.FieldType;
			}
		}
		private FieldInfo field;
		public StaticField(FieldInfo field)
		{
			this.field = field;
		}
		public override void Store(ILGenerator il)
		{
			il.Emit(OpCodes.Ldsfld, field);
		}
		public override void Load(ILGenerator il)
		{
			il.Emit(OpCodes.Ldsfld, field);
		}
		public override void Evaluate(ILGenerator il)
		{
			Load(il);
		}
	}
	public class Argument:Storage
	{
		public override Type Type
		{
			get
			{
				return type;
			}
		}
		public override void Evaluate(ILGenerator il)
		{
			Load(il);
		}
		public override void Store(ILGenerator il)
		{
			il.Emit(OpCodes.Starg, index);
		}
		public override void Load(ILGenerator il)
		{
			il.Emit(OpCodes.Ldarg, index);
		}
		public int Index
		{
			get
			{
				return index;
			}
		}
		private int index;
		private Type type;
		public Argument(int index,Type type)
		{
			this.index = index;
			this.type = type;
		}
	}
	public class Local:Storage
	{
		public override Type Type
		{
			get 
			{
				return type;
			}
		}
		private Type type=typeof(Map);
		public override void Evaluate(ILGenerator il)
		{
			Load(il);
		}
		public override void Store(ILGenerator il)
		{
			il.Emit(OpCodes.Stloc, local.LocalIndex);
		}
		public override void Load(ILGenerator il)
		{
			il.Emit(OpCodes.Ldloc, local.LocalIndex);
		}
		public void Declare(ILGenerator il)
		{
			local = il.DeclareLocal(type);
		}
		LocalBuilder local;
	}
	public delegate void Emit(ILGenerator il);
	public class CustomEmitter : ILEmitter
	{
		private Emit emitter;
		public CustomEmitter(Emit emitter)
		{
			this.emitter = emitter;
		}
		public override void Emit(ILGenerator il)
		{
			emitter(il);
		}
	}
	public abstract class ILEmitter
	{
		public static implicit operator ILEmitter(string text)
		{
			return (Emit)delegate(ILGenerator il)
			{
				il.Emit(OpCodes.Ldstr, text);
			};
		}
		public virtual Type Type
		{
			get
			{
				return typeof(Map);
			}
		}
		public ILExpression Field(string name)
		{
			return new InstanceField(this, Type.GetField(name));
		}
		public ILExpression Call(string name, params ILEmitter[] arguments)
		{
			return new InstanceCall(this, Type.GetMethod(name), arguments);
		}

		public static implicit operator ILEmitter(int del)
		{
			return new Integer(del);
		}
		public static implicit operator ILEmitter(Emit del)
		{
			return new CustomEmitter(del);
		}
		public abstract void Emit(ILGenerator il);
	}
	public class ILProgram : ILEmitter
	{
		public Local Declare()
		{
			Local local = new Local();
			Add((Emit)local.Declare);
			return local;
		}
		private List<ILEmitter> statements;
		public ILProgram(params ILEmitter[] statements)
		{
			this.statements = new List<ILEmitter>(statements);
		}
		public void AddRange(params ILEmitter[] statements)
		{
			this.statements.AddRange(statements);
		}
		public void Add(Emit emitter)
		{
			Add(new CustomEmitter(emitter));
		}
		public void Add(ILEmitter statement)
		{
			this.statements.Add(statement);
		}
		public override void Emit(ILGenerator il)
		{
			foreach (ILEmitter statement in statements)
			{
				statement.Emit(il);
			}
		}
	}
	public abstract class ILExpression:ILEmitter
	{
		public override void  Emit(ILGenerator il)
		{
			Evaluate(il);
		}
		public abstract void Evaluate(ILGenerator il);
	}
	public abstract class Expression
	{
		private StatementBase statement;
		public StatementBase Statement
		{
			get
			{
				return statement;
			}
		}
		public Expression(StatementBase statement)
		{
			this.statement = statement;
		}
		public static Emit EmitSearch(Local key, Local selected, Extent extent)
		{
			return Until(
				selected.Call("ContainsKey", key),
				new ILProgram(
					selected.Assign(selected.Call("get_Scope")),
					If(
						selected,
						Nothing(),
						Throw(
							new New(
								typeof(KeyNotFound).GetConstructor(new Type[] { typeof(Map), typeof(Extent), typeof(Map) }),
								key,
								new New(
									typeof(Extent).GetConstructor(new Type[] {typeof(int),typeof(string)}),
									extent.Start.Line,
									extent.FileName),
								(Emit)Null),
								typeof(KeyNotFound)
								)
							)));
		}
		public static Emit Nothing()
		{
			return delegate(ILGenerator il)
			{
			};
		}
		public static Emit Until(ILEmitter condition, ILEmitter body)
		{
			return delegate(ILGenerator il)
			{
				Label end = il.DefineLabel();
				Label start = il.DefineLabel();

				il.MarkLabel(start);
				condition.Emit(il);
				il.Emit(OpCodes.Brtrue, end);
				body.Emit(il);
				il.Emit(OpCodes.Br, start);

				il.MarkLabel(end);
			};
		}
		public static Emit If(ILEmitter condition, ILEmitter thenBranch, ILEmitter elseBranch)
		{
			return delegate(ILGenerator il)
			{
				Label not = il.DefineLabel();
				Label end = il.DefineLabel();

				condition.Emit(il);
				il.Emit(OpCodes.Brfalse, not);
				thenBranch.Emit(il);
				il.Emit(OpCodes.Br, end);

				il.MarkLabel(not);
				elseBranch.Emit(il);

				il.MarkLabel(end);
			};
		}
		public static Emit Throw(ILEmitter exception, Type type)
		{
			return delegate(ILGenerator il)
			{
				exception.Emit(il);
				il.Emit(OpCodes.Throw);
			};
		}
		public static void Null(ILGenerator il)
		{
			il.Emit(OpCodes.Ldnull);
		}
		public List<Map> literals = new List<Map>();
		protected Eval optimized;
		public virtual Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch(Exception e)
			{
				throw e;
			}
		}
		public void Return(ILGenerator il)
		{
			il.Emit(OpCodes.Ret);
		}
		public Eval Optimize()
		{
			Type[] parameters = new Type[] { typeof(Expression), typeof(Map) };
			DynamicMethod method = new DynamicMethod(
				"Optimized",
				typeof(Map),
				parameters,
				typeof(Map).Module);

			Argument argument = new Argument(1,typeof(Map));
			ILProgram program = new ILProgram();

			Local context = program.Declare();
			program.AddRange(
				context.Assign(argument),
				Emit(this,null, context,new Argument(0,typeof(Expression))),
				(Emit)Return);
			program.Emit(method.GetILGenerator());
			return (Eval)method.CreateDelegate(typeof(Eval), this);
		}
		public abstract ILEmitter Emit(Expression expression,StatementBase lastProgram, Local context, Argument argument);
	}
	public class Call : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private Map parameterName;
		public List<Expression> calls;
		Map code;
		public Call(Map code, Map parameterName,StatementBase statement):base(statement)
		{
			this.code = code;
			this.calls = code.Array.ConvertAll<Expression>(delegate(Map m) { return m.GetExpression(statement); });
			if (calls.Count == 1)
			{
				calls.Add(new Literal(new Map(CodeKeys.Literal, Map.Empty),statement));
			}
			this.parameterName = parameterName;
		}
		public override ILEmitter Emit(Expression expression, StatementBase lastProgram, Local current, Argument argument)
		{
			ILProgram program = new ILProgram();
			Local callable = program.Declare();
			program.Add(callable.Assign(calls[0].Emit(expression,lastProgram, current,argument)));
			for (int i = 1; i < calls.Count; i++)
			{
				program.Add(
					callable.Assign(callable.Call("Call", calls[i].Emit(expression,lastProgram, current,argument))));
			}
			program.Add(callable.Load);
			return program;
		}
	}
	public class Search : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private Expression expression;
		Map code;
		public Search(Map code,StatementBase statement):base(statement)
		{
			this.expression = code.GetExpression(statement);
			this.code = code;
		}
		public override ILEmitter Emit(Expression parentExpression, StatementBase statement, Local context, Argument argument)
		{
			ILProgram program = new ILProgram();
			Local key = program.Declare();
			Local selected = program.Declare();
			program.Add(key.Assign(expression.Emit(parentExpression, statement, context, argument)));
			program.Add(selected.Assign(context));

			bool optimize=false;
			ILProgram p = new ILProgram();
			if (expression is Literal)
			{
				Map literal=((Literal)expression).literal;
				while (statement!=null && statement.NeverContainsKey(literal))
				{
					statement = statement.Parent;
					p.Add(selected.Assign(selected.Call("get_Scope")));
				}
				optimize=statement!=null && statement.AlwaysContainsKey(literal);
			}
			optimize = false;
			if (optimize)
			{
				program.Add(p);
			}
			else
			{
				program.Add(EmitSearch(key, selected,code.Extent));
			}
			program.Add(selected.Call("get_Item", key).Call("Copy"));
			return program;
		}
	}
	public class Program : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw new MetaException(e.ToString(),code.Extent);
			}
		}

		public StatementBase[] Statements
		{
			get
			{
				return statementList;
			}
		}
		public bool IsFunction
		{
			get
			{
				return code.ContainsKey(CodeKeys.Parameter);
			}
		}
		private Map code;
		private StatementBase[] statementList;
		public Program(Map code,StatementBase statement):base(statement)
		{
			this.code = code;
			statementList=code.Array.ConvertAll(new Converter<Map,StatementBase>(delegate(Map map) {return map.GetStatement(this);})).ToArray();
		}
		public override ILEmitter Emit(Expression expression, StatementBase lastProgram, Local parent, Argument argument)
		{
			ILProgram program = new ILProgram();
			Local context = program.Declare();
			program.Add(context.Assign(new New(typeof(Map).GetConstructor(new Type[0]))));
			program.Add(context.Call("set_Scope",parent));
			foreach (StatementBase statement in statementList)
			{
				program.Add(statement.Get(expression,context,argument));
			}
			program.Add(context.Load);
			return program;
		}
	}
	public abstract class StatementBase
	{
		public virtual StatementBase Parent
		{
			get
			{
				return program.Statement;
			}
		}
		public virtual bool NeverContainsKey(Map key)
		{
			bool neverContains = true;
			int count = 0;
			foreach (StatementBase statement in program.Statements)
			{
				if (statement == this)
				{
					break;
				}
				if (statement is CurrentStatement && count!=0 && count!=program.Statements.Length)
				{
					neverContains = false;
					break;
				}
				if (statement is KeyStatement)
				{
					Literal literal = ((KeyStatement)statement).key as Literal;
					if (literal == null || literal.literal.Equals(key))
					{
						neverContains = false;
						break;
					}
				}

				count++;
			}
			return neverContains;
		}
		public virtual bool AlwaysContainsKey(Map key)
		{
			bool alwaysContains = false;
			foreach (StatementBase statement in program.Statements)
			{
				if (statement == this)
				{
					break;
				}
				if (statement is KeyStatement)
				{
					Literal literal = ((KeyStatement)statement).key as Literal;
					if (literal != null && literal.literal.Equals(key))
					{
						alwaysContains = true;
						break;
					}
				}

			}
			return alwaysContains;
		}
		protected Program program;
		public StatementBase(Program program)
		{
			this.program = program;
		}
		public abstract ILEmitter Get(Expression expression,Local context, Argument argument);
	}
	public delegate void Ass(Map context);

	public class KeyStatement : StatementBase
	{
		public Expression key;
		private Expression value;
		public KeyStatement(Map code,Program program):base(program)
		{
			this.key = code[CodeKeys.Key].GetExpression(this);
			this.value = code[CodeKeys.Value].GetExpression(this);
		}
		public override ILEmitter Get(Expression expression,Local context, Argument argument)
		{
			return context.Call("set_Item", key.Emit(expression,this, context, argument), value.Emit(expression,this, context, argument));
		}
	}
	public class CurrentStatement : StatementBase
	{
		private Expression value;
		public CurrentStatement(Map code, Program program)
			: base(program)
		{
			this.value = code[CodeKeys.Value].GetExpression(this);
		}
		public override ILEmitter Get(Expression expression,Local context, Argument argument)
		{
			return context.Call("Nuke", value.Emit(expression,this, context, argument));
		}
	}
	public class Statement : StatementBase
	{
		private Expression key;
		private Expression value;
		Map code;
		public Statement(Map code, Program program)
			: base(program)
		{
			this.code = code;
			this.key = code[CodeKeys.Keys].GetExpression(this);
			this.value = code[CodeKeys.Value].GetExpression(this);
		}
		public override ILEmitter Get(Expression expression,Local context, Argument argument)
		{
			ILProgram program = new ILProgram();
			Local k=program.Declare();
			Local selected = program.Declare();
			program.Add(k.Assign(key.Emit(expression,this, context, argument)));
			program.Add(selected.Assign(context));
			program.Add(Expression.EmitSearch(k, selected,code.Extent));
			program.Add(selected.Call("set_Item",k,value.Emit(expression,this, context, argument)));
			return program;
		}
	}
	public class Literal : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		public Map literal;
		public Literal(Map code,StatementBase statement):base(statement)
		{
			if (code.Count!=0 && code.IsString)
			{
				this.literal = code.GetString();
			}
			else
			{
				this.literal = code;
			}
		}
		public override ILEmitter Emit(Expression expression, StatementBase lastProgram, Local local, Argument argument)
		{
			expression.literals.Add(literal);
			return argument.Field("literals").Call("get_Item",expression.literals.Count - 1).Call("Copy");
		}
	}
	public class Root : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public Root(StatementBase statement)
			: base(statement)
		{
		}
		public override ILEmitter Emit(Expression expression, StatementBase lastProgram, Local local, Argument argument)
		{
			return new StaticField(typeof(Gac).GetField("gac"));
		}
	}
	public class Select : Expression
	{
		public override Map Evaluate(Map context)
		{
			if (optimized == null)
			{
				optimized = Optimize();
			}
			try
			{
				return optimized(context);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private List<Map> subselects;
		public Select(Map code,StatementBase statement):base(statement)
		{
			this.subselects = code.Array;
		}
		public override ILEmitter Emit(Expression expression, StatementBase lastProgram, Local context, Argument argument)
		{
			ILProgram program = new ILProgram();
			Local selected = program.Declare();
			program.Add(selected.Assign(subselects[0].GetExpression(Statement).Emit(expression, lastProgram, context, argument)));
			Local key = program.Declare();
			Local value = program.Declare();
			for (int i = 1; i < subselects.Count; i++)
			{
				program.Add(key.Assign(subselects[i].GetExpression(Statement).Emit(expression, lastProgram, context, argument)));
				program.Add(value.Assign(selected.Call("TryGetValue", key)));
				program.Add(
					If(
						value,
						selected.Assign(value),
						Throw(
							new New(
								typeof(KeyDoesNotExist).GetConstructor(new Type[] { typeof(Map), typeof(Extent), typeof(Map) }),
								key,
								new New(
									typeof(Extent).GetConstructor(new Type[] {typeof(int),typeof(string)}),
									subselects[i].Extent.Start.Line,
									subselects[i].Extent.FileName),
								selected),
								typeof(KeyNotFound)
								)));
			}
			program.Add(selected.Load);
			return program;
		}
	}
	public class Interpreter
	{
		static Interpreter()
		{
			try
			{
				Map map=Parser.Parse(Path.Combine(Interpreter.InstallationPath, "library.meta"));
				map.Scope=Gac.gac;
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
			MemberInfo[] members = typeof(System.Windows.Forms.TreeNodeCollection).GetMember("AddRange");
			if (args.Length != 0)
			{
				if (args[0] == "-test")
				{
					try
					{
						//UseConsole();
						////MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);
						//MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
						//return;
						UseConsole();
						new MetaTest().Run();
					}
					catch (Exception e)
					{
						DebugPrint(e.ToString());
					}
					Console.ReadLine();
				}
				else if (args[0] == "-profile")
				{
					UseConsole();
					//MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"libraryTest.meta"), Map.Empty);
					MetaTest.Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
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
			object dotNet;;
			if (!TryToDotNet(meta, target, out dotNet))
			{
				TryToDotNet(meta, target, out dotNet);
				throw new ApplicationException("Cannot convert " + Serialize.ValueFunction(meta) + " to " + target.ToString() + ".");
			}
			return dotNet;
		}
		public static Delegate CreateDelegateFromCode(Type delegateType, Map code)
		{
			MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();
			List<Type> arguments = new List<Type>();
			arguments.Add(typeof(MetaDelegate));
			foreach (ParameterInfo parameter in parameters)
			{
				arguments.Add(parameter.ParameterType);
			}
			DynamicMethod method = new DynamicMethod("EventHandler",
				invokeMethod.ReturnType,
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

			if (invokeMethod.ReturnType == typeof(void))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Castclass, invokeMethod.ReturnType);
				il.Emit(OpCodes.Ret);
			}
			Delegate del = (Delegate)method.CreateDelegate(delegateType, new MetaDelegate(code, invokeMethod.ReturnType));
			return del;
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
		// refactor
		public static bool TryToDotNet(Map meta, Type target, out object dotNet)
		{
			dotNet = null;
			switch (Type.GetTypeCode(target))
			{
				case TypeCode.Boolean:
					if (meta.IsBoolean)
					{
						dotNet = meta.GetBoolean();
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
					}
					break;
				case TypeCode.Int16:
					if (IsIntegerInRange(meta, Int16.MinValue, Int16.MaxValue))
					{
						dotNet = Convert.ToInt16(meta.GetNumber().GetInt64());
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
					FieldInfo[] fields = target.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

					if (target == typeof(Number) && meta.IsNumber)
					{
						dotNet = meta.GetNumber();
					}
					else if (target != typeof(void) && target.IsValueType && meta.ArrayCount == meta.Count && meta.Count == 2 && Library.Join(meta[1], meta[2]).ArrayCount == fields.Length)
					{
						dotNet = target.InvokeMember(".ctor", BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public, null, null, new object[] { });
						meta = Library.Join(meta[1], meta[2]);
						int index = 1;
						foreach (FieldInfo field in fields)
						{
							field.SetValue(dotNet, Transform.ToDotNet(meta[index], field.FieldType));
							index++;
						}
					}
					else if (target != typeof(void) && target.IsValueType && meta.ArrayCount == meta.Count && meta.Count == fields.Length)
					{
						dotNet = target.InvokeMember(".ctor", BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public, null, null, new object[] { });
						int index = 1;
						foreach (FieldInfo field in fields)
						{
							field.SetValue(dotNet, Transform.ToDotNet(meta[index], field.FieldType));
							index++;
						}

					}
					else if (target == typeof(Type) && meta.Strategy is TypeMap)
					{
						dotNet = ((TypeMap)meta.Strategy).Type;
					}
					else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
							&& meta.ContainsKey(CodeKeys.Function))
					{
						dotNet = CreateDelegateFromCode(target, meta);
					}
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
						string[] asdf;
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
			if (dotNet == null)
			{
				if (meta.Strategy is ObjectMap && ((DotNetMap)meta.Strategy).Type == target)
				{
					dotNet = ((ObjectMap)meta.Strategy).Object;
				}
			}
			return dotNet != null;
		}
		// integer???
		public static bool IsIntegerInRange(Map meta,Number minValue,Number maxValue)
		{
			return meta.IsNumber && meta.GetNumber()>=minValue && meta.GetNumber()<=maxValue;
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
				switch(Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						return(Boolean)dotNet;
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
						if (type==typeof(Number))
						{
							return (Number)dotNet;
						}
						else if(type==typeof(Map))
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
	[Serializable]
	public class Method : MapStrategy
	{
		public override int GetArrayCount()
		{
			return 0;
		}
		public override bool ContainsKey(Map key)
		{
			return false;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return new List<Map>();
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
		protected MethodBase method;
		protected object obj;
		protected Type type;
		public Method(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
			if (method != null)
			{
				this.parameters = method.GetParameters();
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
		ParameterInfo[] parameters;
		public override Map Call(Map argument,Map parent)
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
					bool asdf = argument.IsNumber;
					throw new Exception("Could not convert argument " + Meta.Serialize.ValueFunction(argument) + "\n to " + parameters[arguments.Count].ParameterType.ToString());
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
				return new Map(new CallDelegate(delegate(Map map)
				{
					return DecideCall(map, arguments);
				}));
			}
		}
		private Map Invoke(Map argument, object[] arguments)
		{
			try
			{
				Map result = Transform.ToMeta(
					method is ConstructorInfo ?
						((ConstructorInfo)method).Invoke(arguments) :
						 method.Invoke(obj, arguments));
				return result;
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
	[Serializable]
	public class TypeMap: DotNetMap
	{
		private const BindingFlags bindingFlags=BindingFlags.Public | BindingFlags.Static|BindingFlags.NonPublic;
		protected override BindingFlags BindingFlags
		{
			get 
			{ 
				return bindingFlags;
			}
		}
		private static MemberCache cache=new MemberCache(bindingFlags);
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
			if (Type.IsGenericTypeDefinition)
			{
				List<Type> types=new List<Type>();
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
			else if(base.Get(key)!=null)
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

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return obj is TypeMap && ((TypeMap)obj).Type == this.Type;
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
					constructor = new Map(new Method(Type.GetConstructor(new Type[] { }), Object, Type));
				}
				return constructor;
			}
		}
		public override Map Call(Map argument, Map parent)
		{
			Map item = Constructor.Call(Map.Empty);
			Map result = Library.With(item, argument);
			return result;
		}
	}
	[Serializable]
	public class ObjectMap: DotNetMap
	{
		const BindingFlags bindingFlags=BindingFlags.Public | BindingFlags.Instance|BindingFlags.NonPublic;
		protected override BindingFlags  BindingFlags
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
		public override Map Call(Map arg,Map parent)
		{
			if (this.Type.IsSubclassOf(typeof(Delegate)))
			{
				return new Method(Type.GetMethod("Invoke"), this.Object, this.Type).Call(arg,parent);
			}
			else
			{
				throw new ApplicationException("Object is not callable.");
			}
		}
		public override bool Equals(object obj)
		{
			return obj is ObjectMap && ((ObjectMap)obj).Object.Equals(this.Object);
		}
		public override int GetHashCode()
		{
			return Object.GetHashCode();
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
		public ObjectMap(object target):base(target,target.GetType())
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
	[Serializable]
	public class EmptyStrategy : MapStrategy
	{
		public override bool IsNumber
		{
			get 
			{
				return true;
			}
		}
		public override int GetArrayCount()
		{
			return 0;
		}
		public override bool Equal(MapStrategy obj)
		{
			return obj.Count == 0;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				return new List<Map>(0);
			}
		}
		public override Map CopyData()
		{
			return new Map(new EmptyStrategy());
		}
		public override void Set(Map key, Map val, Map map)
		{
			if (key.IsNumber)
			{
				if (key.Equals(Map.Empty) && val.IsNumber)
				{
					Panic(key, val, new NumberStrategy(0),map);
				}
				else
				{
					// that is not logical, maybe ListStrategy is not suitable for this, only if key==1
					Panic(key, val, new ListStrategy(),map);
				}
			}
			else
			{
				Panic(key, val,new DictionaryStrategy(), map);
				//Panic(key, val, map);
			}
		}
		public override Map Get(Map key)
		{
			return null;
		}
	}
	[Serializable]
	public class NumberStrategy : MapStrategy
	{
		public override int GetArrayCount()
		{
			return 0;
		}
		public override bool Equal(MapStrategy obj)
		{
			if (obj.IsNumber && obj.GetNumber().Equals(number))
			{
				return true;
			}
			else
			{
				return EqualDefault(obj);
			}
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
				else if(key.Equals(NumberKeys.Negative))
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
			else if (key.Equals(NumberKeys.Negative) && value.Equals(Map.Empty) && number!=0)
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
				Panic(key, value,new DictionaryStrategy(),map);
			}
		}

		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				if (number != 0)
				{
					keys.Add(Map.Empty);
				}
				if (number < 0)
				{
					keys.Add(NumberKeys.Negative);
				}
				if (number.Denominator != 1.0d)
				{
					keys.Add(NumberKeys.Denominator);
				}
				return keys;
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
	[Serializable]
	public class StringStrategy : MapStrategy
	{
		//protected override Map GetIndex(int i)
		//{
		//    return text[i];
		//}
		private string text;
		public StringStrategy(string text)
		{
			this.text = text;
		}
		public override bool Equal(MapStrategy obj)
		{
			if(obj is StringStrategy)
			{
				return ((StringStrategy)obj).text == text;
			}
			else
			{
				return base.Equal(obj);
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
			Panic(key, val,strategy, map);
		}
		//public override void Set(Map key, Map val, Map map)
		//{
		//    Panic(key, val,map);
		//}
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
				return text.Length==0;
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
					return text[number.GetInt32()-1];
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
			return new Map(new StringStrategy(text));
		}
		public override ICollection<Map> Keys
		{
			get 
			{
				List<Map> keys = new List<Map>(text.Length);
				for (int i = 1; i <= text.Length; i++)
				{
					keys.Add(i);
				}
				return keys;
			}
		}
	}
	[Serializable]
	public class ListStrategy : MapStrategy
	{
		public override Map CopyData()
		{
			return new Map(new CloneStrategy(this));
		}
		public override void Append(Map map, Map parent)
		{
			list.Add(map);
		}
		private List<Map> list;

		public ListStrategy():this(5)
		{
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
					Panic(key, val,new DictionaryStrategy(),map);
				}
			}
			else
			{
				Panic(key, val,new DictionaryStrategy(),map);
			}
		}

		public override int Count
		{
			get
			{
				return list.Count;
			}
		}
		public override List<Map> Array
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
				if (integer >= 1 && integer <= list.Count)
				{
					containsKey = true;
				}
				else
				{
					containsKey = false;
				}
			}
			else
			{
				containsKey = false;
			}
			return containsKey;
		}
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				int counter = 1;
				foreach (Map value in list)
				{
					keys.Add(new Map(counter));
					counter++;
				}
				return keys;
			}
		}
	}
	[Serializable]
	public class DictionaryStrategy:MapStrategy
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
			while(this.ContainsKey(i))
			{
				i++;
			}
			return i - 1;
		}
		private Dictionary<Map, Map> dictionary;
		public DictionaryStrategy():this(2)
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
		public override ICollection<Map> Keys
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
	[Serializable]
	public class CloneStrategy : MapStrategy
	{
		public override int GetArrayCount()
		{
			return original.GetArrayCount();
		}
		private MapStrategy original;
		public CloneStrategy(MapStrategy original)
		{
			this.original = original;
		}
		public override List<Map> Array
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
		public override bool Equal(MapStrategy obj)
		{
			return obj.Equal(original);
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
		public override ICollection<Map> Keys
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
			MapStrategy strategy=original.DeepCopy(key,value,map);
			//if (original is ListStrategy)
			//{
			//    strategy = new ListStrategy();
			//}
			//else
			//{
			//    strategy = new DictionaryStrategy();
			//}
			//Panic(key, value, strategy, map);
			map.Strategy = strategy;
			//Panic(key, value, map);
		}
		//public override void Set(Map key, Map value, Map map)
		//{
		//    MapStrategy strategy;
		//    if (original is ListStrategy)
		//    {
		//        strategy = new ListStrategy();
		//    }
		//    else
		//    {
		//        strategy = new DictionaryStrategy();
		//    }
		//    Panic(key, value,strategy, map);
		//    //Panic(key, value, map);
		//}
	}

	[Serializable]
	public abstract class MapStrategy
	{
		public virtual string Serialize(Map parent)
		{
			return parent.SerializeDefault();
		}
		public virtual Map Call(Map argument, Map parent)
		{
			return parent.CallDefault(argument);
		}
		public virtual void Append(Map map, Map parent)
		{
		    this.Set(GetArrayCount() + 1,map,parent);
		}
		public abstract void Set(Map key, Map val, Map map);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
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
				strategy.Set(k,Get(k).Copy(),map);
			}
			map.Strategy.Set(key, val, map);
		}
		public virtual MapStrategy DeepCopy(Map key,Map value,Map map)
		{
			MapStrategy strategy=new DictionaryStrategy();
			foreach (Map k in Keys)
			{
				strategy.Set(k, Get(k).Copy(), map);
			}
			strategy.Set(key, value,map);
			return strategy;
		}
		//protected virtual MapStrategy ChooseStrategy()
		//{
		//}

		public virtual bool IsNumber
		{
		    get
		    {
		        return IsNumberDefault;
		    }
		}
		public bool IsNumberDefault
		{
			get
			{
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this.Get(Map.Empty).IsNumber);
			}
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
				return GetArrayCount() == Count && this.Array.TrueForAll(delegate(Map map)
				{
					return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);
				});
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
			if (Count==0)
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
		public abstract ICollection<Map> Keys
		{
			get;
		}
		public virtual List<Map> Array
		{
			get
			{
				List<Map> array = new List<Map>();
				for (int i = 1; this.ContainsKey(i); i++)
				{
					array.Add(this.Get(i));
				}
				return array;
			}
		}
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		public virtual bool Equal(MapStrategy obj)
		{
			return EqualDefault((MapStrategy)obj);
		}
		public virtual bool EqualDefault(MapStrategy strategy)
		{
			if (strategy.Count != this.Count)
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
	}

	public abstract class Member
	{
		public abstract void Set(object obj, Map value);
		public abstract Map Get(object obj);
	}
	public class TypeMember: Member
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
	public class FieldMember:Member
	{
		private FieldInfo field;
		public FieldMember(FieldInfo field)
		{
			this.field=field;
		}
		public override void Set(object obj, Map value)
		{
			field.SetValue(obj, Transform.ToDotNet(value,field.FieldType));
		}
		public override Map Get(object obj)
		{
			return Transform.ToMeta(field.GetValue(obj));
		}
	}
	public class MethodMember:Member
	{
		private MethodBase method;
		public MethodMember(MethodInfo method)
		{
			this.method=method;
		}
		public override void Set(object obj, Map value)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override Map Get(object obj)
		{
			return new Map(new Method(method,obj,method.DeclaringType));
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
					if (method!=null)
					{
						string name = TypeMap.GetMethodName(method);
						data[name]= new MethodMember(method);
					}
					FieldInfo field = member as FieldInfo;
					if (field != null)
					{
						data[field.Name] = new FieldMember(field);
					}
					Type t = member as Type;
					if (t!=null)
					{
						data[t.Name] = new TypeMember(t);
					}
				}
				cache[type] = data;
			}
			return cache[type];
		}
		private Dictionary<Type, Dictionary<Map, Member>> cache = new Dictionary<Type, Dictionary<Map, Member>>();
	}

	[Serializable]
	public abstract class DotNetMap : MapStrategy
	{
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
		// cache all members
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
			string fieldName = key.GetString();
			if(Members.ContainsKey(key))
			{
				Members[key].Set(obj,value);
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
		//public override void Set(Map key, Map value, Map parent)
		//{
		//    string fieldName = key.GetString();
		//    MemberInfo[] members = type.GetMember(fieldName, BindingFlags);
		//    if (members.Length != 0)
		//    {
		//        MemberInfo member = members[0];
		//        if (member is FieldInfo)
		//        {
		//            FieldInfo field = (FieldInfo)member;
		//            field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
		//        }
		//        else
		//        {
		//            throw new Exception("unknown member type");
		//        }
		//    }
		//    else
		//    {

		//        if (!global.ContainsKey(GlobalKey))
		//        {
		//            global[GlobalKey] = new Dictionary<Map, Map>();
		//        }
		//        global[GlobalKey][key] = value;
		//    }
		//}
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
		public override ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				foreach (MemberInfo member in this.type.GetMembers(BindingFlags))
				{
					string name;
					if (member is MethodInfo)
					{
						name = GetMethodName((MethodInfo)member);
					}
					else if (member is ConstructorInfo)
					{
						continue;
					}
					else
					{
						name = member.Name;
					}
					keys.Add(name);
				}
				return keys;
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
	[Serializable]
	public class TestAttribute:Attribute
	{
		public TestAttribute():this(1)
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
					int level;
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
		private static bool UseProperty(PropertyInfo property,int level)
		{
			object[] attributes=property.GetCustomAttributes(typeof(SerializeAttribute), false);
			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public static void Serialize(object obj,string indent,StringBuilder builder,int level) 
		{
			if(obj == null) 
			{
				builder.Append(indent+"null\n");
			}
			else if (UseToStringMethod(obj.GetType()))
			{
				builder.Append(indent+"\""+obj.ToString()+"\""+"\n");
			}
			else
			{
				foreach (PropertyInfo property in obj.GetType().GetProperties())
				{
					if(UseProperty((PropertyInfo)property,level))
					{
						object val=property.GetValue(obj, null);
						builder.Append(indent + property.Name);
						if(val!=null)
						{
							builder.Append(" ("+val.GetType().Name+")");
						}
						builder.Append(":\n");
						Serialize(val,indent+indentationChar,builder,level);
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
	[Serializable]
	public class SourcePosition
	{
		private int line;
		private int column;
		public SourcePosition(int line,int column)
		{
			this.line=line;
			this.column=column;

		}
		[Serialize]
		public int Line
		{
			get
			{
				return line;
			}
			set
			{
				line=value;
			}
		}
		[Serialize]
		public int Column
		{
			get
			{
				return column;
			}
			set
			{
				column=value;
			}
		}
	}
	[Serializable]
	public class Extent
	{
		private SourcePosition start;
		private SourcePosition end;
		public Extent(int line,string fileName):this(new SourcePosition(line,0),new SourcePosition(line,0), fileName)
		{
		}
		public Extent(SourcePosition start, SourcePosition end, string fileName)
		{
			this.start = start;
			this.end = end;
			this.fileName = fileName;
		}
		private string fileName;
		public Extent(int startLine, int startColumn, int endLine, int endColumn, string fileName)
			: this(new SourcePosition(startLine, startColumn), new SourcePosition(endLine, endColumn), fileName)
		{
		}
		public string FileName
		{
			get
			{
				return fileName;
			}
		}
		[Serialize]
		public SourcePosition Start
		{
			get
			{
				return start;
			}
		}
		[Serialize]
		public SourcePosition End
		{
			get
			{
				return end;
			}
		}
	}
	public class Gac : MapStrategy
	{
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
						value = LoadAssembly(Assembly.LoadWithPartialName(key.GetString()));
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
		public override ICollection<Map> Keys
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
	[Serializable]
	public class Number
	{
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
					a = a % b;
				else
					b = b % a;
			}
			if (a == 0)
				return b;
			else
				return a;
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
		public int GetInt32()
		{
			return Convert.ToInt32(numerator);
		}
		public long GetInt64()
		{
			return Convert.ToInt64(numerator);
		}
	}
	public class Binary
	{
		public static void Serialize(Map map,string path)
		{
			using (FileStream stream = File.Create(path))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(stream,map);
			}
		}
		public static Map Deserialize(string path)
		{
			using (FileStream stream = File.Open(path, FileMode.Open))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				object obj=formatter.Deserialize(stream);
				return (Map)obj;
			}
		}
	}
	public class Syntax
	{
		public const char lastArgument = '@';
		public const char autokey = '.';
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
		public const char assignment = ' ';
		public const char space = ' ';
		public const char tab = '\t';
		public const char current = '&';
		public static char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char[] lookupStringForbidden = new char[] { current, lastArgument, explicitCall, indentation, '\r', '\n', assignment, select, function, @string, emptyMap, '!', root, callStart, callEnd, character, ',', '*', '$', '\\', '<', '=', '+', '-', ':' };
		public static char[] lookupStringForbiddenFirst = new char[] { current, lastArgument, explicitCall, indentation, '\r', '\n', assignment, select, function, @string, emptyMap, '!', root, callStart, callEnd, character, ',', '*', '$', '\\', '<', '=', '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
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
		private string file;
		private int line = 1;
		private int column = 1;
		private bool isStartOfFile = true;
		private int indentationCount = -1;
		public Stack<int> defaultKeys = new Stack<int>();

		public Parser(string text, string filePath)
		{
			this.index = 0;
			this.text = text;
			this.file = filePath;
		}
		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(LiteralExpression, Call, Program, List, Search, Select, ExplicitCall);
		});

		public static Rule NewLine =
			new Alternatives(
				new Character(Syntax.unixNewLine),
				StringRule(Syntax.windowsNewLine));

		public static Rule EndOfLine =
			new Sequence(
				new Action(new ZeroOrMore(
					new Action(new Alternatives(
						new Character(Syntax.space),
						new Character(Syntax.tab))))),
				new Action(NewLine));

		public static Rule Integer =
			new Sequence(
				new Action(new CustomProduction(
					delegate(Parser p, Map map, ref Map result)
					{
						p.negative = map != null;
						return null;
					}),
					new Optional(new Character(Syntax.negative))),
				new Action(
					new ReferenceAssignment(),
					new Sequence(
						new Action(
							new ReferenceAssignment(),
							new OneOrMore(
								new Action(
									new CustomProduction(
									delegate(Parser p, Map map, ref Map result)
									{
										if (result == null)
										{
											result = p.CreateMap();
										}
										result = result.GetNumber() * 10 + (Number)map.GetNumber().GetInt32() - '0';
										return result;
									}),
									new Character(Syntax.integer)))),
						new Action(
							new CustomProduction(delegate(Parser p, Map map, ref Map result)
							{
								if (result.GetNumber() > 0 && p.negative)
								{
									result = 0 - result.GetNumber();
								}
								return null;
							}),
							new CustomRule(delegate(Parser p, out bool matched) { 
								matched = true; return null; })
							)
			)));
		public static Rule StartOfFile = new CustomRule(delegate(Parser p, out bool matched)
		{
			if (p.isStartOfFile)
			{
				p.isStartOfFile = false;
				p.indentationCount++;
				matched = true;
			}
			else
			{
				matched = false;
			}
			return null;
		});

		private static Rule EndOfLinePreserve = new Sequence(
			new Action(
				new ZeroOrMore(
					new Action(new Autokey(), new Alternatives(
						new Character(Syntax.space),
						new Character(Syntax.tab))))),
			new Action(
				new Append(),
					new Alternatives(
						new Character(Syntax.unixNewLine),
						StringRule(Syntax.windowsNewLine))));

		private static Rule SmallIndentation = new CustomRule(delegate(Parser p, out bool matched)
		{
			p.indentationCount++;
			matched = true;
			return null;
		});
		public static Rule FullIndentation = new Alternatives(
				StartOfFile,
				new Sequence(
				new Action(EndOfLine),
				new Action(SmallIndentation)
				));
		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			return StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa, out matched);
		});
		private static Rule StringLine = new ZeroOrMore(new Action(new Autokey(), new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0])));
		// remove
		public static Rule StringDedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			Map map = new Sequence(
				new Action(
					new Optional(EndOfLine)),
				new Action(StringRule("".PadLeft(pa.indentationCount - 1, Syntax.indentation)))).Match(pa, out matched);
			if (matched)
			{
				pa.indentationCount--;
			}
			return map;
		});
		public static Rule CharacterDataExpression = new Sequence(
			new Action(
				new Character(Syntax.character)),
			new Action(
				new ReferenceAssignment(),
				new CharacterExcept(Syntax.character)),
			new Action(
				new Character(Syntax.character)));

		public static Rule Dedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			pa.indentationCount--;
			matched = true;
			return null;
		});
		// remove
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
		// refactor
		private static Rule StringBeef = new CustomRule(delegate(Parser parser, out bool matched)
		{
			StringBuilder result = new StringBuilder(100);
			MatchStringLine(parser, result);
			matched = true;
			while (true)
			{
				bool lineMatched;
				new Sequence(new Action(EndOfLine),
					new Action(SameIndentation)).Match(parser, out lineMatched);
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
		// refactor
		private static Rule SingleString = new OneOrMore(
			new Action(
				new Autokey(),
				new CharacterExcept(
					Syntax.unixNewLine,
					Syntax.windowsNewLine[0],
					Syntax.@string)));

		public static Rule String = new Sequence(
			new Action(new Character(Syntax.@string)),
			new Action(new ReferenceAssignment(), new Alternatives(
				SingleString,
				new Sequence(
					new Action(FullIndentation),
					new Action(SameIndentation),
					new Action(new ReferenceAssignment(), StringBeef),
					new Action(StringDedentation)))),
			new Action(new Character(Syntax.@string)));

		public static Rule Number = new Sequence(
			new Action(new ReferenceAssignment(),
				Integer),
			new Action(
				new Assignment(
					NumberKeys.Denominator),
					new Optional(
						new Sequence(
							new Action(
								
								new Character(Syntax.fraction)),
							new Action(
								new ReferenceAssignment(),
								Integer)))));

		public static Rule LookupString = new Sequence(
			new Action(
				new Assignment(1),
				new CharacterExcept(Syntax.lookupStringForbiddenFirst)),
			new Action(new Append(), new ZeroOrMore(
				new Action(
					new Autokey(),
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
			CharacterDataExpression

			);
		});
		private static Rule LookupAnything =
			new Sequence(
				new Action(new Character(('<'))),
				new Action(new ReferenceAssignment(), Value));

		public static Rule Function = new Sequence(
			new Action(new Assignment(
				CodeKeys.Parameter),
				new ZeroOrMore(
				new Action(
					new Autokey(),
					new CharacterExcept(
						Syntax.@string,
						Syntax.function,
						Syntax.windowsNewLine[0],
						Syntax.unixNewLine)))),
			new Action(
				
					new Character(
						Syntax.function)),
				new Action(new Assignment(CodeKeys.Expression),
				Expression),
			new Action(new Optional(EndOfLine)));

		public static Rule Entry = new Alternatives(
			new Sequence(
				new Action(
					new Assignment(CodeKeys.Function),
					new Sequence(new Action(new ReferenceAssignment(), Function)))),
			new Sequence(
				new Action(new Assignment(1), new Alternatives(
					Number,
					LookupString,
					LookupAnything)),
				new Action(new Character('=')),
				new Action(new CustomProduction(
					delegate(Parser parser, Map map, ref Map result)
					{
						result = new Map(result[1], map);
						return result;
					})

					, Value),
			 new Action(new Optional(EndOfLine))));

		public static Rule Map = new Sequence(
			new Action(new Optional(new Character(','))),
			new Action(
				FullIndentation),
				new Action(new ReferenceAssignment(), new PrePost(
				delegate(Parser p)
				{
					p.defaultKeys.Push(1);
				},
				new Sequence(
					new Action(new ReferenceAssignment(),
						new OneOrMore(new Action(
						new Merge(),
						new Sequence(
							new Action(
								 SameIndentation),
							new Action(
								new ReferenceAssignment(),
								Entry))))),
					new Action(Dedentation)),
				delegate(Parser p)
				{
					p.defaultKeys.Pop();
				})));


		public static Rule File = new Sequence(
			new Action(
				new Optional(
					new Sequence(
						new Action(
							StringRule("#!")),
						new Action(
							new ZeroOrMore(
								new Action(
									new CharacterExcept(Syntax.unixNewLine)))),
						new Action(EndOfLine)))),
			new Action(new ReferenceAssignment(), Map));

		public static Rule ExplicitCall = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Character(Syntax.callStart)),
				new Action(new ReferenceAssignment(), Call),
				new Action(new Character(Syntax.callEnd)));
		});
		public static Rule Call = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Character(Syntax.explicitCall)),
				new Action(new Assignment(
					CodeKeys.Call),
					new Sequence(
						new Action(FullIndentation),
						new Action(
							new ReferenceAssignment(),
							new OneOrMore(
								new Action(
									new Autokey(),
									new Sequence(
										new Action(new Optional(EndOfLine)),
										new Action(SameIndentation),
										new Action(new ReferenceAssignment(), Expression))))),
							new Action(new Optional(EndOfLine)),
							new Action(new Optional(Dedentation)))));
		});

		public static Rule FunctionExpression = new Sequence(
			new Action(new Assignment(CodeKeys.Key), new LiteralRule(new Map(CodeKeys.Literal, CodeKeys.Function))),
			new Action(new Assignment(CodeKeys.Value), new Sequence(
				new Action(new Assignment(CodeKeys.Literal), Function))));


		private static Rule Whitespace =
			new ZeroOrMore(
				new Action(
					new Alternatives(
						new Character(Syntax.tab),
						new Character(Syntax.space))));

		private static Rule EmptyMap = new Sequence(
			new Action(
				new Character(Syntax.emptyMap)),
			new Action(new ReferenceAssignment(),
				new LiteralRule(Meta.Map.Empty)));

		private static Rule LiteralExpression = new Sequence(
			new Action(
				new Assignment(
					CodeKeys.Literal),
				new Alternatives(
					Number,
					EmptyMap,
					String,
					CharacterDataExpression)));

		private static Rule LookupAnythingExpression =
			new Sequence(
				new Action(new Character('<')),
				new Action(new ReferenceAssignment(), Expression)
			);

		private static Rule LookupStringExpression =
			new Sequence(
				new Action(new Assignment(
					CodeKeys.Literal),
					LookupString));

		private static Rule Current = new Sequence(
			new Action(new Character(Syntax.current)),
			new Action(new ReferenceAssignment(), new LiteralRule(new Map(CodeKeys.Current, Meta.Map.Empty))));

		private static Rule Root = new Sequence(
			new Action(new Character(Syntax.root)),
			new Action(new ReferenceAssignment(), new LiteralRule(new Map(CodeKeys.Root, Meta.Map.Empty))));

		private static Rule Search = new Sequence(
			new Action(
				new Assignment(
						CodeKeys.Search), new Alternatives(
					new Sequence(
						new Action(new Character('!')),
						new Action(
							new ReferenceAssignment(),
							Expression)),
					new Alternatives(LookupStringExpression, LookupAnythingExpression))));

		public static Rule ProgramDelayed = new DelayedRule(delegate()
		{
			return Program;
		});
		private static Rule Select = new Sequence(
			new Action(new Assignment(
				CodeKeys.Select),
				new Sequence(
					new Action(new Character('.')),
					new Action(FullIndentation),
					new Action(SameIndentation),
					new Action(new Assignment(1),
						new Alternatives(
							ProgramDelayed,
							LiteralExpression,
							Root,
							Search,
							Call)),
					new Action(new Append(),
						new ZeroOrMore(new Action(new Autokey(), new Sequence(
							new Action(new Optional(EndOfLine)),
							new Action(SameIndentation),
							new Action(new ReferenceAssignment(), new Alternatives(LookupAnythingExpression, LookupStringExpression, Expression)))))),

					new Action(new Optional(Dedentation))
			)));

		private static Rule KeysSearch = new Sequence(
			new Action(
				new Assignment(CodeKeys.Search),
				new Sequence(
					new Action(new Character('!')),
					new Action(
						new ReferenceAssignment(),
						new Alternatives(
							LookupStringExpression,
							LookupAnythingExpression,
							Expression)))));
		
		private static Rule Keys = new Alternatives(
			new Sequence(new Action(
				new Assignment(
					1),
					new Alternatives(
						new Sequence(new Action(new Assignment(CodeKeys.Literal), new Alternatives(LookupString, Number))),
						KeysSearch,
						new Sequence(new Action(
							new ReferenceAssignment(),
							LiteralExpression)),
			new Sequence(new Action(new Assignment(CodeKeys.Literal), new Alternatives(LookupString, Number))),
			EmptyMap,
			String,
			LookupStringExpression
			))),

			new Sequence(
			new Action(new Character('.')),
			new Action(FullIndentation),
			new Action(new Append(),
				new ZeroOrMore(
					new Action(new Autokey(),
						new Sequence(
				new Action(new Optional(EndOfLine)),
			new Action(SameIndentation),
			new Action(new ReferenceAssignment(),
				new Alternatives(
						KeysSearch,
						new Sequence(new Action(
							new ReferenceAssignment(),
							LiteralExpression)),
			EmptyMap,
			String,
			LookupStringExpression
			)))))),
			new Action(new Optional(EndOfLine)),
			new Action(new Optional(Dedentation)),
			new Action(new Optional(SameIndentation))));


		public static Rule CurrentStatement = new Sequence(
			new Action(StringRule("&=")),
			new Action(new Assignment(CodeKeys.Current), new LiteralRule(Meta.Map.Empty)),
			new Action(new Assignment(CodeKeys.Value), Expression),
			new Action(new Optional(EndOfLine))
			);


		public static Rule KeysStatement = new Sequence(
			new Action(new Assignment(CodeKeys.Key), 
			new Alternatives(
				new Sequence(new Action(new Assignment(CodeKeys.Literal),LookupString)),

				Expression)),
			new Action(new Optional(EndOfLine)),
			new Action(new Optional(SameIndentation)),
			new Action(StringRule("=")),
			new Action(new Assignment(CodeKeys.Value), Expression),
			new Action(new Optional(EndOfLine)));


		public static Rule Statement = new Sequence(
			new Action(new ReferenceAssignment(),
				new Alternatives(
					FunctionExpression,
					new Sequence(
						new Action(new Assignment(
							CodeKeys.Keys),
							new Alternatives(
				new Sequence(new Action(new Assignment(CodeKeys.Literal), LookupString)),

				Expression)),
						new Action(new Optional(EndOfLine)),
						new Action(new Optional(SameIndentation)),
						new Action(new Character(':')),
						new Action(new Assignment(
							CodeKeys.Value),
							Expression),
						new Action(new Optional(EndOfLine)))
			)));
		public static Rule ListMap = new Sequence(
			new Action(new Character('+')),
			new Action(
				new ReferenceAssignment(),
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
					new Sequence(
			new Action(new Optional(EndOfLine)),
			new Action(SmallIndentation),
						new Action(
							new ReferenceAssignment(),
							new ZeroOrMore(
								new Action(new Autokey(),
									new Sequence(
										new Action(new Optional(EndOfLine)),
										new Action(SameIndentation),
										new Action(
											new ReferenceAssignment(), Value))))),
				new Action(new Optional(EndOfLine)),
				new Action(new Optional(new Alternatives(Dedentation)))),
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));

		public static Rule List = new Sequence(
			new Action(new Character('+')),
			new Action(
				new Assignment(CodeKeys.Program),
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
					new Sequence(
			new Action(new Optional(EndOfLine)),
			new Action(SmallIndentation),
						new Action(
							new Append(),
							new ZeroOrMore(
								new Action(new Autokey(),
									new Sequence(
										new Action(new Optional(EndOfLine)),
										new Action(SameIndentation),
										new Action(
							new CustomProduction(
							delegate(Parser p, Map map, ref Map result)
							{
								result = new Map(
									CodeKeys.Key, new Map(
											CodeKeys.Literal, p.defaultKeys.Peek()),
									CodeKeys.Value, map);
								p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
								return result;
							}
			), Expression)
			)))
			)
			,
				new Action(new Optional(EndOfLine)),
				new Action(new Optional(new Alternatives(Dedentation)))
			),
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));

		public static Rule Program = new Sequence(
			new Action(new Character(',')),
			new Action(
				new Assignment(CodeKeys.Program),
					new Sequence(
						new Action(EndOfLine),
						new Action(SmallIndentation),
						new Action(new ReferenceAssignment(),
							new ZeroOrMore(
								new Action(
									new Autokey(),
									new Sequence(
										new Action(new Alternatives(
											SameIndentation,
											Dedentation)),
										new Action(new ReferenceAssignment(), new Alternatives(CurrentStatement,KeysStatement, Statement)))))))));
		// Maybe make this a delegate instead of a class
		public abstract class Production
		{
			public abstract void Execute(Parser parser, Map map, ref Map result);
		}
		public class Action
		{
			private Rule rule;
			private Production production;
			public Action(Rule rule):this(new Match(),rule)
			{
			}
			public Action(Production production, Rule rule)
			{
				this.rule = rule;
				this.production = production;
			}
			public bool Execute(Parser parser, ref Map result)
			{
				bool matched;
				Map map = rule.Match(parser, out matched);
				if (matched)
				{
					production.Execute(parser, map, ref result);
				}
				return matched;
			}
		}
		public class Autokey : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result.Append(map);
			}
		}
		public class Assignment : Production
		{
			private Map key;
			public Assignment(Map key)
			{
				this.key = key;
			}
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result[key] = map;
			}
		}
		public class Match : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
			}
		}
		public class ReferenceAssignment : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = map;
			}
		}
		public class Append : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				foreach (Map m in map.Array)
				{
					result.Append(m);
				}
			}
		}
		public class Join : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = Library.Join(result, map);
			}
		}
		public class Merge : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = Library.Merge(result, map);
			}
		}
		public class CustomProduction : Production
		{
			private CustomActionDelegate action;
			public CustomProduction(CustomActionDelegate action)
			{
				this.action = action;
			}
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				this.action(parser, map, ref result);
			}
		}
		public delegate Map CustomActionDelegate(Parser p, Map map, ref Map result);

		public abstract class Rule
		{
			public Map Match(Parser parser, out bool matched)
			{
				int oldIndex = parser.index;
				int oldLine = parser.line;
				int oldColumn = parser.column;
				bool isStartOfFile = parser.isStartOfFile;
				Map result = MatchImplementation(parser, out matched);
				if (!matched)
				{
					parser.index = oldIndex;
					parser.line = oldLine;
					parser.column = oldColumn;
					parser.isStartOfFile = isStartOfFile;
				}
				else
				{
					if (result != null)
					{
						if (result.IsString)
						{
							result = new Map(result.GetString());
							////if (!newResult.GetHashCode().Equals(result.GetHashCode()))
							//if (!result.Equals(newResult))
							//////if (!newResult.Equals(result))
							//{
							//    object a = result.Equals(newResult);
							//    //object x = newResult.GetHashCode().Equals(result.GetHashCode());
							//}

							//Map newResult=new Map(result.GetString());
							//if (!newResult.Equals(result))
							//{
							//    object x = !newResult.Equals(result);
							//}

						}
						result.Extent = new Extent(oldLine, oldColumn, parser.line, parser.column, parser.file);
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
		public static Rule StringRule(string text)
		{
			List<Action> actions = new List<Action>();
			foreach (char c in text)
			{
				actions.Add(new Action(new Character(c)));
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
				Map result = parser.CreateMap();
				bool success = true;
				foreach (Action action in actions)
				{
					bool matched = action.Execute(parser, ref result);
					if (!matched)
					{
						success = false;
						break;
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
				Map list = parser.CreateMap(new ListStrategy());
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
				Map list = parser.CreateMap(new ListStrategy());
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
		public class TwoOrMore : Rule
		{
			protected override Map MatchImplementation(Parser parser, out bool matched)
			{
				Map list = parser.CreateMap(new ListStrategy());
				int count = 0;
				while (true)
				{
					if (!action.Execute(parser, ref list))
					{
						break;
					}
					count++;
				}
				matched = count >= 2;
				return list;
			}
			private Action action;
			public TwoOrMore(Action action)
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
				return file;
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
		public Map CreateMap(params Map[] maps)
		{
			return new Map(maps);
		}
		public Map CreateMap()
		{
			return CreateMap(new EmptyStrategy());
		}
		public Map CreateMap(MapStrategy strategy)
		{
			return new Map(strategy);
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
	public class Serialize
	{
		public static string ValueFunction(Map map)
		{
			return DoSerialize(map);
		}
		public static string DoSerialize(Map map)
		{
			return DoSerialize(map,-1).Trim();
		}
		private static string GetIndentation(int indentation)
		{
			return "".PadLeft(indentation, '\t');
		}
		private static string DoSerialize(Map map, int indentation)
		{
			try
			{
				if (map.Strategy is Gac)
				{
					return "Gac";
				}
				if (map.Strategy is DotNetMap)
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
					return map.GetNumber().ToString();
				}
				else if (map.IsString)
				{

					string text=map.GetString();
					if (text.Contains("\"") || text.Contains("\n"))
					{
						string result = "\"" + Environment.NewLine;
						foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
						{
							result += GetIndentation(indentation+1) + "\t" + line + Environment.NewLine;
						}
						return result.Trim('\n', '\r') + Environment.NewLine + GetIndentation(indentation) + "\"";
					}
					else
					{
						return "\"" + text + "\"";
					}
				}
				else
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
						text +=
							GetIndentation(indentation + 1);
						string key;
						if (entry.Key.Count != 0 && entry.Key.IsString && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbidden) == -1 && entry.Key.GetString().IndexOfAny(Syntax.lookupStringForbiddenFirst) != 0)
						{
							key = entry.Key.GetString();
						}
						else
						{
							key = "<" + DoSerialize(entry.Key, indentation + 1);
						}
						text += key +
							"=" +
							DoSerialize(entry.Value, indentation + 1) + Environment.NewLine;
						text = text.TrimEnd('\r', '\n') + Environment.NewLine;
					}
					return text;
				}
			}
			catch (Exception e)
			{
				int asdf = 0;
				throw e;
			}

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
					return Meta.Serialize.ValueFunction(Parser.Parse(Path.Combine(Interpreter.InstallationPath, @"basicTest.meta")));
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
			public class Performance : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(Path.Combine(Interpreter.InstallationPath, @"learning.meta"), Map.Empty);
				}
			}
			public static Map Run(string path, Map argument)
			{
				Map callable = Parser.Parse(path);
				callable.Scope = Gac.gac["library"];
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
		public ExceptionLog(Extent extent)
		{
			this.extent = extent;
		}
		public Extent extent;
	}
	public class MetaException : Exception
	{
		private string message;
		private Extent extent;
		private List<ExceptionLog> invocationList = new List<ExceptionLog>();
		public MetaException(string message, Extent extent)
		{
			this.message = message;
			this.extent = extent;
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
				message += "\n" + GetExtentText(log.extent);
			}

			return message;
		}
		public static string GetExtentText(Extent extent)
		{
			string text;
			if (extent != null)
			{
				text = extent.FileName + ", line ";
				text += extent.Start.Line + ", column " + extent.Start.Column;
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
				return GetExtentText(extent) + ": " + message;
			}
		}
		public Extent Extent
		{
			get
			{
				return extent;
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
			: base(message, new Extent(parser.Line, parser.Column, parser.Line, parser.Column, parser.FileName))
		{
		}
	}
	public class ExecutionException : MetaException
	{
		private Map context;
		public ExecutionException(string message, Extent extent, Map context)
			: base(message, extent)
		{
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException
	{
		public KeyDoesNotExist(Map key, Extent extent, Map map)
			: base("Key does not exist: " + Serialize.ValueFunction(key) + " in " + Serialize.ValueFunction(map), extent, map)
		{
		}
	}
	public class KeyNotFound : ExecutionException
	{
		public KeyNotFound(Map key, Extent extent, Map map)
			: base("Key not found: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
	public class Library
	{
		public static Map Append(Map array, Map item)
		{
			array.Append(item);
			if (!(array.Strategy is ListStrategy))
			{
			}
			return array;
		}
		public static Map EnumerableToArray(Map map)
		{
			Map result = new Map();
			foreach (object entry in (IEnumerable)(((ObjectMap)map.Strategy)).Object)
			{
				result.Append(Transform.ToMeta(entry));
			}
			return result;
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
				return tryFunction.Call(Map.Empty);
			}
			catch (Exception e)
			{
				return catchFunction.Call(new Map(e));
			}
		}
		public static Map With(Map o, Map values)
		{
			object obj = ((ObjectMap)o.Strategy).Object;
			Type type = obj.GetType();
			foreach (KeyValuePair<Map, Map> entry in values)
			{
				Map value = entry.Value;
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
					// we should really throw an exception here
				}
			}
			return o;
		}
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
			arg.AppendRange(map.Array);
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
	public class PseudoStatement : StatementBase
	{
		public override StatementBase Parent
		{
			get
			{
				if (map.Scope == null)
				{
					return null;
				}
				else
				{
					return new PseudoStatement(map.Scope);
				}
			}
		}
		private Map map;
		public override bool NeverContainsKey(Map key)
		{
			return !map.ContainsKey(key);
		}
		public override bool AlwaysContainsKey(Map key)
		{
			return map.ContainsKey(key);
		}
		public PseudoStatement(Map map):base(null)
		{
			this.map = map;
		}
		public override ILEmitter Get(Expression expression, Local context, Argument argument)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	[Serializable]
	public class Map : IEnumerable<KeyValuePair<Map, Map>>, ISerializeEnumerableSpecial
	{
		private bool constantKeys=false;
		public bool ConstantKeys
		{
			get
			{
				return constantKeys;
			}
			set
			{
				constantKeys = value;
			}
		}
		public string Serialize()
		{
			return strategy.Serialize(this);
		}
		public void Nuke(Map map)
		{
			this.strategy = map.strategy;
		}
		public Map Call(Map arg)
		{
			return strategy.Call(arg, this);
		}
		public void Append(Map map)
		{
			strategy.Append(map, this);
		}
		//public void Remove(Map key)
		//{
		//    strategy.Remove(key, this);
		//}
		protected MapStrategy strategy;

		public Map(object o)
			: this(new ObjectMap(o))
		{
		}
		public Map(bool boolean)
			: this(new Number((double)Convert.ToInt32(boolean)))
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
		public Map(MapStrategy strategy)
		{
			this.strategy = strategy;
		}
		public Map()
			: this(new EmptyStrategy())
		{
		}
		public Map(Number number)
			: this(new NumberStrategy(number))
		{
		}
		public Map(string text)
			: this(new StringStrategy(text))
		{
		}
		public Map(params Map[] keysAndValues)
			: this()
		{
			for (int i = 0; i <= keysAndValues.Length - 2; i += 2)
			{
				this[keysAndValues[i]] = keysAndValues[i + 1];
			}
		}

		public int ArrayCount
		{
			get
			{
				return strategy.GetArrayCount();
			}
		}
		//public void InitFromStrategy(MapStrategy clone)
		//{
		//    foreach (Map key in clone.Keys)
		//    {
		//        this[key] = clone.Get(key);
		//    }
		//}
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
		public Number GetNumber()
		{
			return strategy.GetNumber();
		}
		public string GetString()
		{
			return strategy.GetString();
		}
		public int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public List<Map> Array
		{
			get
			{
				return strategy.Array;
			}
		}
		protected Map Get(Map key)
		{
			return strategy.Get(key);
		}
		protected void Set(Map key, Map value)
		{
			strategy.Set(key, value, this);
		}
		protected Map CopyData()
		{
			return strategy.CopyData();
		}
		protected bool ContainsKeyImplementation(Map key)
		{
			return strategy.ContainsKey(key);
		}
		protected ICollection<Map> KeysImplementation
		{
			get
			{
				return strategy.Keys;
			}
		}
		public override bool Equals(object toCompare)
		{
			return ((Map)toCompare).strategy.Equal(strategy);
		}
		//public override bool Equals(object toCompare)
		//{
		//    bool isEqual;
		//    if (Object.ReferenceEquals(toCompare, this))
		//    {
		//        isEqual = true;
		//    }
		//    else if (toCompare is Map)
		//    {
		//        isEqual = ((Map)toCompare).strategy.Equal(strategy);
		//    }
		//    else
		//    {
		//        isEqual = false;
		//    }
		//    return isEqual;
		//}
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
		public List<Map> Values
		{
			get
			{
				List<Map> values = new List<Map>();
				foreach (Map key in Keys)
				{
					values.Add(this[key]);
				}
				return values;
			}
		}
		public override string ToString()
		{
			return Meta.Serialize.ValueFunction(this);
		}
		public Map TryGetValue(Map key)
		{
			return Get(key);
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
				if (value != null)
				{
					compiledCode = null;
					Map val;
					val = value;
					Set(key, val);
				}
			}
		}
		public virtual void AppendRange(IEnumerable<Map> array)
		{
			AppendRangeDefault(array);
		}
		public virtual void AppendRangeDefault(IEnumerable<Map> array)
		{
			int counter = ArrayCount + 1;
			foreach (Map map in array)
			{
				this[counter] = map;
				counter++;
			}
		}
		private object compiledCode;

		public StatementBase GetStatement(Program program)
		{
			if (compiledCode == null)
			{
				if (ContainsKey(CodeKeys.Keys))
				{
					compiledCode = new Statement(this,program);

				}
				else if (ContainsKey(CodeKeys.Current))
				{
					compiledCode = new CurrentStatement(this,program);
				}
				else if (ContainsKey(CodeKeys.Key))
				{
					compiledCode = new KeyStatement(this,program);
				}
				else
				{
					throw new ApplicationException("Cannot compile map");
				}
			}
			return (StatementBase)compiledCode;
		}
		public Expression GetExpression(StatementBase statement)
		{
			if (compiledCode == null)
			{
				compiledCode = CreateExpression(statement);
			}
			return (Expression)compiledCode;
		}
		public Expression CreateExpression(StatementBase statement)
		{
			if (ContainsKey(CodeKeys.Call))
			{
				return new Call(this[CodeKeys.Call], this.TryGetValue(CodeKeys.Parameter),statement);
			}
			else if (ContainsKey(CodeKeys.Program))
			{
				return new Program(this[CodeKeys.Program],statement);
			}
			else if (ContainsKey(CodeKeys.Literal))
			{
				return new Literal(this[CodeKeys.Literal],statement);
			}
			else if (ContainsKey(CodeKeys.Select))
			{
				return new Select(this[CodeKeys.Select],statement);
			}
			else if (ContainsKey(CodeKeys.Search))
			{
				return new Search(this[CodeKeys.Search],statement);
			}
			else if (ContainsKey(CodeKeys.Root))
			{
				return new Root(statement);
			}
			else
			{
				throw new ApplicationException("Cannot compile map " + Meta.Serialize.ValueFunction(this));
			}
		}
		public static Map Empty
		{
			get
			{
				return new Map(new EmptyStrategy());
			}
		}
		public virtual string SerializeDefault()
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
		public virtual bool IsBoolean
		{
			get
			{
				return IsNumber && (GetNumber() == 0 || GetNumber() == 1);
			}
		}
		public bool IsNumberDefault
		{
			get
			{
				return Count == 0 || (Count == 1 && ContainsKey(Map.Empty) && this[Map.Empty].IsNumber);
			}
		}
		public bool IsStringDefault
		{
			get
			{
				return ArrayCount == Count && this.Array.TrueForAll(delegate(Map map)
				{
					return Transform.IsIntegerInRange(map, (int)Char.MinValue, (int)Char.MaxValue);
				});
			}
		}
		public virtual bool GetBoolean()
		{
			bool boolean;
			Number number = GetNumber();
			if (number == 0)
			{
				boolean = false;
			}
			else if (number == 1)
			{
				boolean = true;
			}
			else
			{
				throw new ApplicationException("Map is not a boolean.");
			}
			return boolean;
		}
		public string GetStringDefault()
		{
			StringBuilder text = new StringBuilder("");
			foreach (Map key in Keys)
			{
				text.Append(Convert.ToChar(this[key].GetNumber().GetInt32()));
			}
			return text.ToString();
		}
		public Number GetNumberDefault()
		{
			Number number;
			if (Count == 0)
			{
				number = 0;
			}
			else if (this.Count == 1 && this.ContainsKey(Map.Empty) && this[Map.Empty].IsNumber)
			{
				number = 1 + this[Map.Empty].GetNumber();
			}
			else
			{
				throw new ApplicationException("Map is not an integer");
			}
			return number;
		}
		public Map Scope
		{
			get
			{
				return scope;
			}
			set
			{
				scope = value;
			}
		}
		public int GetArrayCountDefault()
		{
			int i = 1;
			for (; this.ContainsKey(i); i++)
			{
			}
			return i - 1;
		}
		public Map CallDefault(Map arg)
		{
			if (ContainsKey(CodeKeys.Function))
			{
				Map argumentScope = new Map(this[CodeKeys.Function][CodeKeys.Parameter], arg);
				argumentScope.Scope = this;
				return this[CodeKeys.Function][CodeKeys.Expression].GetExpression(new PseudoStatement(argumentScope)).Evaluate(argumentScope);
			}
			else
			{
				throw new ApplicationException("Map is not a function: " + Meta.Serialize.ValueFunction(this));
			}
		}
		public ICollection<Map> Keys
		{
			get
			{
				List<Map> keys = new List<Map>();
				foreach (Map key in KeysImplementation)
				{
					keys.Add(key);
				}
				return keys;
			}
		}
		public Map Copy()
		{
			Map clone = CopyData();
			clone.Scope = Scope;
			clone.Extent = Extent;
			return clone;
		}
		public bool ContainsKey(Map key)
		{
			return ContainsKeyImplementation(key);
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
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public virtual IEnumerator<KeyValuePair<Map, Map>> GetEnumerator()
		{
			foreach (Map key in Keys)
			{
				yield return new KeyValuePair<Map, Map>(key, this[key]);
			}
		}
		Extent extent;
		[Serialize(1)]
		public Extent Extent
		{
			get
			{
				return extent;
			}
			set
			{
				extent = value;
			}
		}
		public static implicit operator Map(double number)
		{
			return new Map(number);
		}
		public static implicit operator Map(float number)
		{
			return new Map(number);
		}
		public static implicit operator Map(decimal number)
		{
			return new Map(number);
		}
		public static implicit operator Map(Number integer)
		{
			return new Map(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new Map(new Number((double)(boolean ? 1 : 0)));
		}
		public static implicit operator Map(char character)
		{
			return new Map(new Number((double)character));
		}
		public static implicit operator Map(byte integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(int integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(long integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new Map(new Number((double)integer));
		}
		public static implicit operator Map(string text)
		{
			return new Map(text);
		}
		[NonSerialized]
		private Map scope;
	}
}