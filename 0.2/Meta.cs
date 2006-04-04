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
using System.Windows.Forms;
using System.Net.Sockets;
using ICSharpCode.SharpZipLib.Zip;
using System.Web;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;


namespace Meta
{
	public class CodeKeys
	{
		public static readonly Map ParameterName="parameterName";
		public static readonly Map Root = "root";
		public static readonly Map Search = "search";
		public static readonly Map Lookup = "lookup";
		public static readonly Map Current="current";
		public static readonly Map Scope="scope";
		public static readonly Map Literal="literal";
		public static readonly Map Function="function";
		public static readonly Map Call="call";
		public static readonly Map Callable="callable";
		public static readonly Map Argument="argument";
		public static readonly Map Select="select";
		public static readonly Map Program="program";
		public static readonly Map Key="key";
		public static readonly Map Value="value";
	}
	public class DotNetKeys
	{
		public static readonly Map Add="add";
		public static readonly Map Remove="remove";
		public static readonly Map Get="get";
		public static readonly Map Set="set";
	}
	public class NumberKeys
	{
		public static readonly Map Negative="negative";
		public static readonly Map Denominator="denominator";
	}
	public class MetaException : Exception
	{
		private string message;
		private Extent extent;
		// this should use Positions instead of extents in the long run
		private List<Extent> invocationList = new List<Extent>();


		public MetaException(string message, Extent extent)
		{
			this.message = message;
			this.extent = extent;
		}
		public List<Extent> InvocationList
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
			foreach (Extent extent in invocationList)
			{
				message += "\n" + GetExtentText(extent);
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
	public class SyntaxException:MetaException
	{
		public SyntaxException(string message, Parser parser)
			: base(message, new Extent(parser.Line, parser.Column,parser.Line,parser.Column,parser.FileName))
		{
		}
	}
	public class ExecutionException : MetaException
	{
		private Map context;
		public ExecutionException(string message, Extent extent,Map context):base(message,extent)
		{
			this.context = context;
		}
	}
	public class KeyDoesNotExist : ExecutionException
	{
		public KeyDoesNotExist(Map key, Extent extent, Map map)
			:base("Key does not exist: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
	public class KeyNotFound : ExecutionException
	{
		public KeyNotFound(Map key, Extent extent, Map map)
			:base("Key not found: " + Serialize.ValueFunction(key), extent, map)
		{
		}
	}
	public abstract class Expression
	{
		public abstract PersistantPosition Evaluate(PersistantPosition context);
	}
	public class Call : Expression
	{
		private Map callable;
		public Map argument;
		public Call(Map code)
		{
			this.callable = code[CodeKeys.Callable];
			this.argument = code[CodeKeys.Argument];
		}
		public static PersistantPosition lastArgument;
		public override PersistantPosition Evaluate(PersistantPosition current)
		{

			PersistantPosition arg = argument.GetExpression().Evaluate(current);
			lastArgument = arg;
			return callable.GetExpression().Evaluate(current).Get().Call(arg.Get(), Select.lastPosition);
		}
		//public override PersistantPosition Evaluate(PersistantPosition current)
		//{

		//    PersistantPosition arg = argument.GetExpression().Evaluate(current);
		//    lastArgument = arg;
		//    return callable.GetExpression().Evaluate(current).Get().Call(arg.Get(), Select.lastPosition);
		//}
	}
	public class Program : Expression
	{
		private Map statements;
		public Program(Map code)
		{
			statements = code;
		}
		public override PersistantPosition Evaluate(PersistantPosition parent)
		{
			FunctionBodyKey call;
			parent.Get().AddCall(new StrategyMap(), out call);
			PersistantPosition contextPosition = new PersistantPosition(parent, call);
			foreach (Map statement in statements.Array)
			{
				statement.GetStatement().Assign(contextPosition);
			}
			contextPosition.Get().Scope = parent;
			return contextPosition;
		}
	}
	public class Literal : Expression
	{
		private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
		private Map literal;
		public Literal(Map code)
		{
			if (code.Count!=0 && code.IsString)
			{
				this.literal = code.GetString();
			}
			else
			{
				this.literal = code.Copy();
			}
		}
		//private PersistantPosition position;

		public override PersistantPosition Evaluate(PersistantPosition context)
		{
			//if (position == null)
			//{
			//    FunctionBodyKey calls;
			//    context.Get().AddCall(literal, out calls);
			//    //context.Get().AddCall(literal.Copy(), out calls);
			//    position = new PersistantPosition(context, calls);
			//    position.Get().Scope = context.Parent;
			//    //position.Get().Scope = position.Parent;
			//}
			FunctionBodyKey calls;
			context.Get().AddCall(literal, out calls);
			//context.Get().AddCall(literal.Copy(), out calls);
			PersistantPosition position = new PersistantPosition(context, calls);
			position.Get().Scope = position.Parent;
			return position;
		}
	}
	//public class Literal : Expression
	//{
	//    private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
	//    private Map literal;
	//    public Literal(Map code)
	//    {
	//        //if (cached.ContainsKey(code))
	//        //{
	//        //    this.literal = cached[code];
	//        //}
	//        //else
	//        //{
	//            this.literal = code;
	//        //    cached[code] = code;
	//        //}
	//    }
	//    public override PersistantPosition Evaluate(PersistantPosition context)
	//    {
	//        int calls;
	//        context.Get().AddCall(literal.Copy(), out calls);
	//        PersistantPosition position = new PersistantPosition(context, new FunctionBodyKey(calls));
	//        position.Get().Scope = position.Parent;
	//        return position;
	//    }
	//}
	//public class Literal : Expression
	//{
	//    private static Dictionary<Map, Map> cached = new Dictionary<Map, Map>();
	//    private Map literal;
	//    public Literal(Map code)
	//    {
	//        this.literal = code;
	//    }
	//    public override PersistantPosition Evaluate(PersistantPosition context)
	//    {
	//        int calls;
	//        context.Get().AddCall(literal.Copy(), out calls);
	//        PersistantPosition position=new PersistantPosition(context, new FunctionBodyKey(calls));
	//        position.Get().Scope = position.Parent;
	//        return position;
	//    }
	//}
	public abstract class Subselect
	{
		public abstract PersistantPosition Evaluate(PersistantPosition context, PersistantPosition executionContext);
		public abstract void Assign(PersistantPosition context, Map value, PersistantPosition executionContext);
	}
	public class Current:Subselect
	{
		public override PersistantPosition Evaluate(PersistantPosition context, PersistantPosition executionContext)
		{
			return context;
		}
		public override void Assign(PersistantPosition context, Map value, PersistantPosition executionContext)
		{
			executionContext.Assign(value);
		}
	}
	public class CallSubselect : Subselect
	{
		private Call call;
		public CallSubselect(Map code)
		{
			this.call = new Call(code);
		}
		public override PersistantPosition Evaluate(PersistantPosition selected, PersistantPosition context)
		{
			PersistantPosition result = call.Evaluate(context);
			return result;
		}
		public override void Assign(PersistantPosition selected, Map value, PersistantPosition context)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class Root : Subselect
	{
		public override PersistantPosition Evaluate(PersistantPosition selected, PersistantPosition context)
		{
			return RootPosition.rootPosition;
			//return new PersistantPosition(new List<Map>());
		}
		public override void Assign(PersistantPosition selected, Map value, PersistantPosition context)
		{
			throw new Exception("Cannot assign to argument.");
		}
	}
	public class Lookup:Subselect
	{
		private Map keyExpression;
		public Lookup(Map keyExpression)
		{
			this.keyExpression = keyExpression;
		}
		public override PersistantPosition Evaluate(PersistantPosition selected, PersistantPosition context)
		{
			PersistantPosition keyPosition = keyExpression.GetExpression().Evaluate(context);
			Map key = keyPosition.Get();
			if (!selected.Get().ContainsKey(key))
			{
				throw new KeyDoesNotExist(key, keyExpression.Extent, null);
			}
			return new PersistantPosition(selected, key);
		}

		public override void Assign(PersistantPosition selected, Map value, PersistantPosition context)
		{
			Map key = keyExpression.GetExpression().Evaluate(context).Get();
			selected.Assign(key,value);
		}
	}
	public class Search : Subselect
	{
		private Map keyExpression;
		public Search(Map keyExpression)
		{
			this.keyExpression = keyExpression;
		}
		private PersistantPosition lastEvaluated;
		private Map lastKey;
		private PersistantPosition lastContext;
		public override PersistantPosition Evaluate(PersistantPosition selected, PersistantPosition context)
		{
			PersistantPosition keyPosition = keyExpression.GetExpression().Evaluate(context);
			Map key = keyPosition.Get();
			if (lastEvaluated != null && lastKey != null && lastKey.Equals(key))
			{
				//string text = lastContext.ToString();
				if (lastContext.Parent.Parent.Equals(context.Parent.Parent) && context.Keys.Count < lastEvaluated.Keys.Count - 1)
					//if (lastContext.Parent.Parent.Equals(context.Parent.Parent) && context.Keys.Count < lastEvaluated.Keys.Count - 1)
					//if (lastContext.Parent.Parent.Equals(context.Parent.Parent))
				{
					return lastEvaluated;
				}
				//if (Object.ReferenceEquals(lastContext, context))
				//{
				//    return lastEvaluated;
				//}
			}
			lastKey = key.Copy();
			lastContext = context;
			PersistantPosition selection = selected;
			while (!selection.Get().ContainsKey(key))
			{
				if (selection.Parent == null)
				{
					selection = null;
					break;
				}
				else
				{
					selection.Get().KeyChanged += new KeyChangedEventHandler(Search_KeyChanged);
					if (selection.Get().Scope != null)
					{
						selection = selection.Get().Scope;
						//selection = new PersistantPosition(selection.Get().Scope);
					}
					else
					{
						selection = selection.Parent;//.Keys.Count - 1));
					}
				}
			}
			try
			{
				selection.Get().KeyChanged += new KeyChangedEventHandler(Search_KeyChanged);
			}
			catch(Exception e)
			{

			}
			//selection.Get().KeyChanged += new KeyChangedEventHandler(Search_KeyChanged);
			if (selection == null)
			{
				throw new KeyNotFound(key, keyExpression.Extent, null);
			}
			else
			{
				lastEvaluated = new PersistantPosition(selection, key);
				return lastEvaluated;
				//return new PersistantPosition(selection, key);
			}
		}

		void Search_KeyChanged(KeyChangedEventArgs e)
		{
			if (lastKey != null && e.Key.Equals(lastKey))
			{
				lastEvaluated = null;
				lastKey = null;
				lastContext = null;
			}
		}
		public override void Assign(PersistantPosition selected, Map value, PersistantPosition context)
		{
			PersistantPosition evaluatedKeyPosition = keyExpression.GetExpression().Evaluate(context);
			Map key = evaluatedKeyPosition.Get();
			if (key.Equals(new StrategyMap("lines")))
			{
			}
			PersistantPosition selection = context;
			while (selection != null && !selection.Get().ContainsKey(key))
			{
				selection = selection.Parent;
			}
			if (selection == null)
			{
				throw new KeyNotFound(key, keyExpression.Extent, null);
			}
			else
			{
				selection.Assign(key, value);
			}
		}
	}
	//public class Search : Subselect
	//{
	//    private Map keyExpression;
	//    public Search(Map keyExpression)
	//    {
	//        this.keyExpression = keyExpression;
	//    }
	//    public override PersistantPosition Evaluate(PersistantPosition selected, PersistantPosition context)
	//    {
	//        PersistantPosition keyPosition = keyExpression.GetExpression().Evaluate(context);
	//        Map key = keyPosition.Get();
	//        if (key.Equals(new StrategyMap("lines")))
	//        {
	//        }
	//        PersistantPosition selection = selected;
	//        while (!selection.Get().ContainsKey(key))
	//        {
	//            if (selection.Parent == null)
	//            //if (selection.Keys.Length == 0)
	//            {
	//                selection = null;
	//                break;
	//            }
	//            else
	//            {
	//                if (selection.Get().Scope != null)
	//                {
	//                    selection = selection.Get().Scope;
	//                    //selection = new PersistantPosition(selection.Get().Scope);
	//                }
	//                else
	//                {
	//                    selection = selection.Parent;//.Keys.Count - 1));
	//                }
	//            }
	//        }
	//        if (selection == null)
	//        {
	//            throw new KeyNotFound(key, keyExpression.Extent, null);
	//        }
	//        else
	//        {
	//            return new PersistantPosition(selection, key);
	//        }
	//    }
	//    public override void Assign(PersistantPosition selected, Map value, PersistantPosition context)
	//    {
	//        PersistantPosition evaluatedKeyPosition = keyExpression.GetExpression().Evaluate(context);
	//        Map key = evaluatedKeyPosition.Get();
	//        if (key.Equals(new StrategyMap("lines")))
	//        {
	//        }
	//        PersistantPosition selection = context;
	//        while (selection != null && !selection.Get().ContainsKey(key))
	//        {
	//            selection = selection.Parent;
	//        }
	//        if (selection == null)
	//        {
	//            throw new KeyNotFound(key, keyExpression.Extent, null);
	//        }
	//        else
	//        {
	//            selection.Assign(key, value);
	//        }
	//    }
	//}
	public class Select : Expression
	{
		private List<Map> subselects;
		public Select(Map code)
		{
			this.subselects = code.Array;
		}
		public override PersistantPosition Evaluate(PersistantPosition context)
		{
			PersistantPosition selected = context;
			foreach (Map subselect in subselects)
			{
				selected = subselect.GetSubselect().Evaluate(selected, context);
			}
			lastPosition = selected;
			return selected;
		}
		public static PersistantPosition lastPosition;
	}
	public class Statement
	{
		// somewhat inconsequential
		public List<Map> Keys
		{
			get
			{
				return keys;
			}
		}
		private List<Map> keys;
		private Map value;
		public Statement(Map code)
		{
			this.keys = code[CodeKeys.Key].Array;
			this.value = code[CodeKeys.Value];
		}
		public void Assign(PersistantPosition context)
		{
			PersistantPosition selected = context;
			for (int i = 0; i + 1 < keys.Count; i++)
			{
				selected = keys[i].GetSubselect().Evaluate(selected, context);
				//selected = keys[i].GetSubselect().Evaluate(selected, context);
				//selected = keys[i].GetSubselect().Evaluate(selected, context);
			}
			try
			{
				Map val = value.GetExpression().Evaluate(context).Get();
				keys[keys.Count - 1].GetSubselect().Assign(selected, val, context);
			}
			catch (ApplicationException e)
			{
				throw new MetaException(e.ToString()+e.StackTrace, value.Extent);
			}
			//keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context).Get(), context);
			//keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context), ref context);
		}
		//public void Assign(PersistantPosition context)
		//{
		//    PersistantPosition selected = context;
		//    for (int i = 0; i + 1 < keys.Count; i++)
		//    {
		//        selected = keys[i].GetSubselect().Evaluate(selected, context);
		//        //selected = keys[i].GetSubselect().Evaluate(selected, context);
		//        //selected = keys[i].GetSubselect().Evaluate(selected, context);
		//    }
		//    Map val=value.GetExpression().Evaluate(context).Get();
		//    keys[keys.Count - 1].GetSubselect().Assign(selected, val, context);
		//    //keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context).Get(), context);
		//    //keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context), ref context);
		//}
		//public void Assign(PersistantPosition context)
		//{
		//    PersistantPosition selected = context;
		//    for (int i = 0; i + 1 < keys.Count; i++)
		//    {
		//        selected = keys[i].GetSubselect().Evaluate(selected, context);
		//        //selected = keys[i].GetSubselect().Evaluate(selected, context);
		//        //selected = keys[i].GetSubselect().Evaluate(selected, context);
		//    }
		//    keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context), context);
		//    //keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context), ref context);
		//}
		//public void Assign(ref Map context)
		//{
		//    Map selected = context;
		//    for (int i = 0; i + 1 < keys.Count; i++)
		//    {
		//        selected = keys[i].GetSubselect().Evaluate(selected, context);
		//        //selected = keys[i].GetSubselect().Evaluate(selected, context);
		//    }
		//    keys[keys.Count - 1].GetSubselect().Assign(selected, value.GetExpression().Evaluate(context), ref context);
		//}
	}
	public class Library
	{
		public static Map CompareString(Map arg)
		{
			return arg[1].GetString().CompareTo(arg[2].GetString());
		}
		public static Map SplitString(Map arg)
		{
			//try
			//{
				char[] delimiters = (char[])Transform.ToDotNet(arg["delimiters"], typeof(char[]));
				string[] split = arg[1].GetString().Split(delimiters);
				Map result = new StrategyMap(new ListStrategy(split.Length));
				foreach (string text in split)
				{
					result.Append(text);
				}

				return result;
			//}
			//catch (Exception e)
			//{
			//    return null;
			//}
		}
		public static Map Subtract(Map arg)
		{
			return arg[1].GetNumber() - arg[2].GetNumber();
		}
		public static Map Divide(Map arg)
		{
			return arg[1].GetNumber() / arg[2].GetNumber();
		}
		public static Map Parse(Map arg)
		{
			Map start = new StrategyMap();
			Parser parser = new Parser(arg.GetString(), "Parse function");
			bool matched;
			Map result = Parser.File.Match(parser, out matched);
			if (parser.index != parser.text.Length)
			{
				throw new SyntaxException("Expected end of file.", parser);
			}
			return result;

		}
		public static Map Product(Map arg)
		{
			Number result = 1;
			foreach (Map number in arg.Array)
			{
				result *= number.GetNumber();
			}
			return result;
		}
		public static Map BinaryOr(Map arg)
		{
		    int binaryOr = 0;
		    foreach (Map map in arg.Array)
		    {
	            binaryOr |= map.GetNumber().GetInt32();
		    }
		    return binaryOr;
		}
		private static Random random = new Random();
		public static Map Random(Map arg)
		{
			return random.Next(1,arg.GetNumber().GetInt32());
		}
		public static Map Reverse(Map arg)
		{
			List<Map> list=new List<Map>(arg.Array);
			list.Reverse();
			return new StrategyMap(list);
		}
		public static Map If(Map arg)
		{
			Map result;
			FunctionBodyKey calls;
			// probably wrong
			MethodImplementation.currentPosition.Get().AddCall(arg, out calls);
			//PersistantPosition position = new PersistantPosition(MethodImplementation.currentPosition, new FunctionBodyKey(calls));
			//PersistantPosition thenPosition = new PersistantPosition(position, "then");
			//PersistantPosition elsePosition = new PersistantPosition(position, "else");
			if (arg[1].GetBoolean())
			{
				result = arg["then"].Call(Map.Empty, arg["then"].Scope).Get();
				//result = thenPosition.Get().Call(Map.Empty, thenPosition).Get();
				//result = arg["then"].Call(Map.Empty, MethodImplementation.currentPosition).Get();
			}
			else if (arg.ContainsKey("else"))
			{
				result = arg["else"].Call(Map.Empty, arg["else"].Scope).Get();
				//result = elsePosition.Get().Call(Map.Empty, elsePosition).Get();
				//result = arg["else"].Call(Map.Empty, MethodImplementation.currentPosition).Get();
			}
			else
			{
				result = Map.Empty;
			}
			return result;
		}
		public static Map Intersect(Map arg)
		{
			Dictionary<Map, object> keys = new Dictionary<Map, object>();
			foreach (Map map in arg.Array)
			{
				foreach (KeyValuePair<Map, Map> entry in map)
				{
					keys[entry.Key] = null;
				}
			}
			Dictionary<Map,Map> result = new Dictionary<Map,Map>();
			foreach (KeyValuePair<Map, object> entry in keys)
			{
				List<Map> args=arg.Array;
				args.Reverse();
				foreach(Map map in args)
				{
					if (map.ContainsKey(entry.Key))
					{
						result[entry.Key] = map[entry.Key];
					}
					else
					{
						result.Remove(entry.Key);
						break;
					}
				}
			}
			return new StrategyMap(new DictionaryStrategy(result));
		}
		public static Map Equal(Map arg)
		{
			bool equal = true;
			if(arg.ArrayCount>1)
			{
				List<Map> array = arg.Array;
				for (int i = 0; i<arg.Count-1; i++)
				{
					if (!array[i].Equals(array[i + 1]))
					{
						equal = false;
						break;
					}
				}
			}
			return equal;
		}
		public static Map Not(Map arg)
		{
			return !arg.GetBoolean();
		}
		public static Map Or(Map arg)
		{
			bool or=false;
			foreach (Map map in arg.Array)
			{
				if (map.GetBoolean())
				{
					or = true;
					break;
				}
			}
			return or;
		}
		public static Map Try(Map arg)
		{
			Map result;
			PersistantPosition argument = Call.lastArgument;
			//PersistantPosition argument = new PersistantPosition(Call.lastArgument);
			//PersistantPosition argument = Call.lastArgument.Copy();
			try
			{
				result = argument.Get()["function"].Call(Map.Empty, argument).Get();
				//result = arg["function"].Call(Map.Empty, MethodImplementation.currentPosition).Get();
			}
			catch (Exception e)
			{
				result = argument.Get()["catch"].Call(new ObjectMap(e), argument).Get();
				//result = arg["catch"].Call(new ObjectMap(e), MethodImplementation.currentPosition).Get();
			}
			return result;
		}
		public static Map Split(Map arg)
		{
			Map arrays = new StrategyMap();
			Map subArray = new StrategyMap();
			List<Map> array = arg[1].Array;
			List<Map> delimiters = arg["delimiters"].Array;

			for (int i = 0; i < array.Count; i++)
			{
				Map map = array[i];
				bool equal = false;
				foreach (Map delimiter in delimiters)
				{
					if (map.Equals(delimiter))
					{
						equal = true;
						break;
					}
				}
				if (equal || i == array.Count - 1)
				{
					if (i == array.Count - 1)
					{
						subArray.Append(map);
					}
					arrays.Append(subArray);
					subArray = new StrategyMap();
				}
				//if (map.Equals(arg[2]) || i == array.Count - 1)
				//{
				//    if (i == array.Count - 1)
				//    {
				//        subArray.Append(map);
				//    }
				//    arrays.Append(subArray);
				//    subArray = new StrategyMap();
				//}
				else
				{
					subArray.Append(map);
				}
			}
			//for (int i = 0; i < array.ArrayCount; i++)
			//{
			//    Map map = array.Array[i];
			//    if (map.Equals(arg[2]) || i == array.ArrayCount - 1)
			//    {
			//        if (i == array.ArrayCount - 1)
			//        {
			//            subArray.Append(map);
			//        }
			//        arrays.Append(subArray);
			//        subArray = new StrategyMap();
			//    }
			//    else
			//    {
			//        subArray.Append(map);
			//    }
			//}
			return arrays;
		}
		//public static Map Split(Map arg)
		//{
		//    Map arrays = new StrategyMap();
		//    Map subArray = new StrategyMap();
		//    Map array=arg[1];
		//    for(int i=0;i<array.ArrayCount;i++)
		//    {
		//        Map map = array.Array[i];
		//        if (map.Equals(arg[2]) || i==array.ArrayCount-1)
		//        {
		//            if (i == array.ArrayCount - 1)
		//            {
		//                subArray.Append(map);
		//            }
		//            arrays.Append(subArray);
		//            subArray = new StrategyMap();
		//        }
		//        else
		//        {
		//            subArray.Append(map);
		//        }
		//    }
		//    return arrays;
		//}
		public static Map CreateConsole(Map arg)
		{
			Interpreter.AllocConsole();
			return Map.Empty;
		}
		public static Map StrictlyIncreasing(Map arg)
		{
			bool increasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (!(arg[i + 1].GetNumber() > arg[i].GetNumber()))
				{
					increasing = false;
					break;
				}
			}
			return increasing;
		}
		public static Map StrictlyDecreasing(Map arg)
		{
			bool decreasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (!(arg[i + 1].GetNumber() < arg[i].GetNumber()))
				{
					decreasing = false;
					break;
				}
			}
			return decreasing;
		}
		public static Map Decreasing(Map arg)
		{
			bool nonincreasing = true;
			for (int i = 1; i  < arg.ArrayCount;i++)
			{
				if (arg[i + 1].GetNumber() > arg[i].GetNumber())
				{
					nonincreasing = false;
					break;
				}
			}
			return nonincreasing;
		}
		public static Map Increasing(Map arg)
		{
			bool nondecreasing = true;
			for (int i = 1; i < arg.ArrayCount; i++)
			{
				if (arg[i + 1].GetNumber() < arg[i].GetNumber())
				{
					nondecreasing = false;
					break;
				}
			}
			return nondecreasing;
		}
		public static Map And(Map arg)
		{
			bool and = true; ;
			foreach (Map map in arg.Array)
			{
				if (!map.GetBoolean())
				{
					and = false;
					break;
				}
			}
			return and;
		}
		public static Map Reciprocal(Map arg)
		{
			Number number = arg.GetNumber();
			return new Number(number.Denominator, number.Numerator);
		}
		public static Map Sum(Map arg)
		{
			Number result=0;
			foreach (Map map in arg.Array)
			{
				result += map.GetNumber();
			}
			return result;
		}
		public static string writtenText = "";
		public static void WriteLine(string text)
		{
			Write(text + Environment.NewLine);
		}
		public static void Write(string text)
		{
			writtenText += text;
			Console.Write(text);
		}
		public static Map Maximum(Map arg)
		{
			Number maximum = arg[1].GetNumber();
			foreach (Map map in arg.Array)
			{
				Number number = map.GetNumber();
				if (number > maximum)
				{
					maximum = number;
				}
			}
			return new StrategyMap(maximum);
		}
		public static Map Opposite(Map arg)
		{
			return arg.GetNumber()*-1;
		}
		public static Map Minimum(Map arg)
		{
			Number minumum = arg[1].GetNumber();
			foreach (Map map in arg.Array)
			{
				Number number = map.GetNumber();
				if (number < minumum)
				{
					minumum = number;
				}
			}
			return new StrategyMap(minumum);
		}
		// should be called unite or something, should be: "Unify"
		public static Map Merge(Map arg)
		{
			Map result=new StrategyMap();
			foreach (Map map in arg.Array)
			{
				foreach (KeyValuePair<Map,Map> pair in map)
				{
					result[pair.Key] = pair.Value;
				}
			}
			return result;
		}
		// rename arguments
		public static Map Sort(Map arg)
		{
			List<Map> array = arg[1].Array;
			PersistantPosition argument = Call.lastArgument;
			//PersistantPosition argument = Call.lastArgument.Copy();
			array.Sort(new Comparison<Map>(delegate(Map a, Map b)
			{
				Map result=argument.Get().Call(new StrategyMap(1, a, 2, b), argument).Get();
				return result.GetNumber().GetInt32();//);//, argument).Get().GetNumber().GetInt32().CompareTo(argument.Get().Call(b, argument).Get().GetNumber().GetInt32());
				//return argument.Get().Call(new StrategyMap(1, a, 2, b), argument).Get().GetNumber().GetInt32();//);//, argument).Get().GetNumber().GetInt32().CompareTo(argument.Get().Call(b, argument).Get().GetNumber().GetInt32());
				//return arg.Call(a, MethodImplementation.currentPosition).Get().GetNumber().GetInt32().CompareTo(arg.Call(b, MethodImplementation.currentPosition).Get().GetNumber().GetInt32());
			}));
			return new StrategyMap(array);
		}
		//public static Map Sort(Map arg)
		//{
		//    List<Map> array = arg[1].Array;
		//    PersistantPosition argument = Call.lastArgument;
		//    //PersistantPosition argument = Call.lastArgument.Copy();
		//    array.Sort(new Comparison<Map>(delegate(Map a, Map b)
		//    {
		//        return argument.Get().Call(a, argument).Get().GetNumber().GetInt32().CompareTo(argument.Get().Call(b, argument).Get().GetNumber().GetInt32());
		//        //return arg.Call(a, MethodImplementation.currentPosition).Get().GetNumber().GetInt32().CompareTo(arg.Call(b, MethodImplementation.currentPosition).Get().GetNumber().GetInt32());
		//    }));
		//    return new StrategyMap(array);
		//}
		public static Map Append(Map arg)
		{
			Map result = Map.Empty;
			Number counter = 1;
			foreach (Map map in arg.Array)
			{
				result.AppendRange(map);
			}
			return result;
		}
		public static Map Range(Map arg)
		{
			int end = arg.GetNumber().GetInt32();
			Map result = new StrategyMap();
			for (int i = 1; i <= end; i++)
			{
				result.Append(i);
			}
			return result;
		}
		// remove
		public static Map Find(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			string text = arg["array"].GetString();
			string value = arg["value"].GetString();
			for (int i = 0; ; i++)
			{
				i = text.IndexOf(value, i);
				if (i == -1)
				{
					break;
				}
				else
				{
					result.Append(i + 1);
				}
			}
			return result;
		}
		public static Map Slice(Map arg)
		{
			Map array = arg["array"];
			int start;
			if (arg.ContainsKey("start"))
			{
				start = arg["start"].GetNumber().GetInt32();
			}
			else
			{
				start = 1;
			}
			int end;
			if (arg.ContainsKey("end"))
			{
				end = arg["end"].GetNumber().GetInt32();
			}
			else
			{
				end = array.ArrayCount;
			}
			Map result = new StrategyMap(new ListStrategy());
			for (int i = start; i <= end; i++)
			{
				result.Append(array[i]);
			}
			return result;
		}
		public static Map StringReplace(Map arg)
		{
			return arg["string"].GetString().Replace(arg["old"].GetString(), arg["new"].GetString());
		}
		// should be removed
		public static Map UrlDecode(Map arg)
		{
			string[] aSplit;
			string sOutput;
			string sConvert = arg.GetString();

			//' convert all pluses to spaces
			sOutput = sConvert.Replace("+", " ");
			//sOutput = REPLACE(sConvert, "+", " ")

			//' next convert %hexdigits to the character
			aSplit = sOutput.Split('%');

			//If IsArray(aSplit) Then
			sOutput = aSplit[0];
			for (int i = 1; i < aSplit.Length; i++)
			{
				sOutput = sOutput + (char)Convert.ToInt32(aSplit[i].Substring(0, 2), 16) + aSplit[i].Substring(2);
			}
			return sOutput;
		}
		// change this to work for all maps?

		// default should be infinite loop, maybe?
		public static Map While(Map arg)
		{
			PersistantPosition argument = Call.lastArgument;
			while (argument.Get()[1].Call(Map.Empty, argument).Get().GetBoolean())
			{
				argument.Get().Call(Map.Empty, argument);
			}
			return Map.Empty;
		}
		public static Map Apply(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			PersistantPosition argument = Call.lastArgument;
			foreach (Map map in arg[1].Array)
			{
				PersistantPosition pos = argument.Get().Call(map, argument);
				result.Append(pos.Get());
			}
			return result;
		}
		public static Map Filter(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			PersistantPosition argument = Call.lastArgument;
			foreach (Map map in arg[1].Array)
			{
				if (argument.Get().Call(map, argument).Get().GetBoolean())
				{
					result.Append(map);
				}
			}
			return result;
		}
		// bad names
		public static Map FindFirst(Map arg)
		{
			Map result = new StrategyMap(new ListStrategy());
			Map array = arg["array"];
			Map value = arg["value"];
			for (int i = 1; i <= array.ArrayCount; i++)
			{
				for (int k = 1; value[k].Equals(array[i + k - 1]); k++)
				{
					if (k == value.ArrayCount)
					{
						return i;
					}

				}
			}
			return 0;
		}
		// combine with apply
		public static Map Foreach(Map arg)
		{
			Map result = new StrategyMap();
			PersistantPosition argument = Call.lastArgument;
			foreach (KeyValuePair<Map, Map> entry in arg[1])
			{
				result.Append(argument.Get().Call(new StrategyMap("key", entry.Key, "value", entry.Value), argument).Get());
			}
			return result;
		}
	}
	public class FileMap : StrategyMap
	{
		private string path;
		public FileMap(string path)
			: this(path, new DictionaryStrategy())
		{
		}
		public FileMap(string path, MapStrategy strategy)
			: base(strategy)
		{
			this.path = path;
		}
		public string Path
		{
			get
			{
				return path;
			}
		}
		public void Save()
		{
			File.WriteAllText(path,this.Count==0? "": Meta.Serialize.ValueFunction(this).Trim(Syntax.unixNewLine));
		}
	}
	public class FileSubMap : FileMap
	{
		private FileMap fileMap;
		public FileSubMap(FileMap fileMap):base(fileMap.Path)
		{
			this.fileMap = fileMap;
		}
		protected override void Set(Map key, Map value)
		{
			strategy.Set(key, value);
		}
	}
	public class DrivesMap : Map
	{
		Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		public DrivesMap()
		{
			foreach (string drive in Directory.GetLogicalDrives())
			{
				cache[drive.Remove(2)] = new DirectoryMap(new DirectoryInfo(drive));
			}
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return cache.ContainsKey(key);
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				return cache.Keys;
			}
		}
		protected override Map Get(Map key)
		{
			Map value;
			if (cache.TryGetValue(key, out value))
			{
				return value;
			}
			else
			{
				return null;
			}
		}
		protected override void Set(Map key, Map val)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		protected override Map CopyData()
		{
			return this;
			//throw new Exception("The method or operation is not implemented.");
		}
	}
	public class DirectoryMap : Map
	{
		private DirectoryInfo directory;
		private Dictionary<Map,string> keys;
		protected override bool ContainsKeyImplementation(Map key)
		{
			if (keys == null)
			{
				keys = GetKeys();
			}
			return keys.ContainsKey(key);
			//return KeysImplementation.Contains(key);
			//return cache.ContainsKey(key);
		}

		public DirectoryMap(DirectoryInfo directory)
		{
			this.directory = directory;
		}
		private Dictionary<Map,string> GetKeys()
		{
			Dictionary<Map,string> keys = new Dictionary<Map,string>();
			try
			{
				foreach (DirectoryInfo subdir in directory.GetDirectories())
				{
					keys.Add(subdir.Name,"");
				}
				foreach (FileInfo file in directory.GetFiles("*.*"))
				{
					string fileName;
					if (file.Extension == ".meta" || file.Extension == ".dll" || file.Extension == ".exe")
					{
						fileName = Path.GetFileNameWithoutExtension(file.FullName);
					}
					else
					{
						fileName = file.Name;
					}
					keys.Add(fileName,"");
					//keys.Add(fileName);
				}
			}
			// should only work in with drives, maybe separate DriveMap
			catch (Exception e)
			{
			}
			return keys;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				if (keys == null)
				{
					keys = GetKeys();
				}
				return keys.Keys;
			}
		}
		public static void TrashFile(string fname)
		{
			InteropSHFileOperation fo = new InteropSHFileOperation();
			fo.wFunc = InteropSHFileOperation.FO_Func.FO_DELETE;
			fo.fFlags.FOF_ALLOWUNDO = true;
			fo.fFlags.FOF_NOCONFIRMATION = true;
			fo.pFrom = fname;
		}
		public Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
		protected override Map Get(Map key)
		{
			Map value = null;
			if (cache.ContainsKey(key))
			{
				value = cache[key];
			}
			else if (key.IsString)
			{
				//if (cache.ContainsKey(key))
				//{
				//    value = cache[key];
				//}
				//else
				//{
					string name = key.GetString();
					if (directory.FullName != Interpreter.LibraryPath)
					{
						Directory.SetCurrentDirectory(directory.FullName);
					}
					string file = Path.Combine(directory.FullName, name);
					string metaFile = Path.Combine(directory.FullName, name + ".meta");
					string dllFile = Path.Combine(directory.FullName, name + ".dll");
					string exeFile = Path.Combine(directory.FullName, name + ".exe");

					if (File.Exists(metaFile))
					{
						string text = File.ReadAllText(metaFile, Encoding.Default);
						Map result;
						FileMap fileMap = new FileMap(metaFile);
						if (text != "")
						{
							Map start = new StrategyMap();
							//Map start = new StrategyMap(new PersistantPosition(Position, key));
							Parser parser = new Parser(text, metaFile);
							bool matched;
							result = Parser.File.Match(parser, out matched);
							if (parser.index != parser.text.Length)
							{
								throw new SyntaxException("Expected end of file.", parser);
							}
							value = result;
						}
						else
						{
							value = Map.Empty;
						}
					}
					else
					{
						bool dllLoaded = false;
						if (File.Exists(dllFile))
						{
							try
							{
								Assembly assembly = Assembly.LoadFile(dllFile);
								value = Gac.LoadAssembly(assembly);
								//foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
								//{
								//    string path=Path.Combine(assembly.CodeBase,referencedAssembly.Name);
								//    if (File.Exists(path))
								//    {
								//        Assembly.LoadFile(path);
								//    }
								//}
								dllLoaded = true;
							}
							catch (Exception e)
							{
								value = null;
							}
						}
						else if (File.Exists(exeFile))
						{
							try
							{
								Assembly assembly = Assembly.LoadFile(exeFile);
								value = Gac.LoadAssembly(assembly);
								//foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
								//{
								//    string path = Path.Combine(Path.GetDirectoryName(assembly.Location), referencedAssembly.Name+".dll");
								//    if (File.Exists(path))
								//    {
								//        Assembly a=Assembly.LoadFile(path);
								//        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
								//        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
								//    }
								//}
								dllLoaded = true;
							}
							catch (Exception e)
							{
								value = null;
							}
						}
						if (!dllLoaded)
						{
							if (File.Exists(file))
							{
								switch (Path.GetExtension(file))
								{
									case ".txt":
									case ".meta":
										//value = new FileMap(file, new ListStrategy());
										// this is problematic, writes the file all the time
										//foreach (char c in File.ReadAllText(file))
										////foreach (char c in File.ReadAllText(file))
										//{
										//    value.Append(c);
										//}
										value = new StrategyMap(File.ReadAllText(file));
										//value = new StrategyMap(new ListStrategy(File.ReadAllText(file)));
										break;
									default:
										value = new FileMap(file, new ListStrategy());
										// problematic, writes the file
										//foreach (byte b in File.ReadAllBytes(file))
										////foreach (byte b in File.ReadAllBytes(file))
										//{
										//    value.Append(b);
										//}
										break;
								}
							}
							else
							{
								DirectoryInfo subDir = new DirectoryInfo(Path.Combine(directory.FullName, name));
								if (subDir.Exists)
								{
									value = new DirectoryMap(subDir);
									//value = new DirectoryMap(subDir, this.Position);
								}
								else
								{
									value = null;
								}
							}
						}
					}
					if (value != null)
					{
						cache[key] = value;
					}
				//}
			}
			return value;
		}
		// fix
		//protected override Map Get(Map key)
		//{
		//    Map value = null;
		//    if (key.IsString)
		//    {
		//        if (key.GetString() == "basicTest")
		//        {
		//        }
		//        if (cache.ContainsKey(key))
		//        {
		//            value = cache[key];
		//        }
		//        else
		//        {
		//            string name = key.GetString();
		//            if (directory.FullName != Interpreter.LibraryPath)
		//            {
		//                Directory.SetCurrentDirectory(directory.FullName);
		//            }
		//            string file = Path.Combine(directory.FullName, name);
		//            string metaFile = Path.Combine(directory.FullName, name + ".meta");
		//            string dllFile = Path.Combine(directory.FullName, name + ".dll");
		//            string exeFile = Path.Combine(directory.FullName, name + ".exe");

		//            if (File.Exists(metaFile))
		//            {
		//                string text = File.ReadAllText(metaFile, Encoding.Default);
		//                Map result;
		//                FileMap fileMap = new FileMap(metaFile);
		//                if (text != "")
		//                {
		//                    Map start = new StrategyMap();
		//                    //Map start = new StrategyMap(new PersistantPosition(Position, key));
		//                    Parser parser = new Parser(text, metaFile);
		//                    bool matched;
		//                    result = Parser.File.Match(parser, out matched);
		//                    if (parser.index != parser.text.Length)
		//                    {
		//                        throw new SyntaxException("Expected end of file.", parser);
		//                    }
		//                    value = result;
		//                }
		//                else
		//                {
		//                    value = Map.Empty;
		//                }
		//            }
		//            else
		//            {
		//                bool dllLoaded = false;
		//                if (File.Exists(dllFile))
		//                {
		//                    try
		//                    {
		//                        Assembly assembly = Assembly.LoadFile(dllFile);
		//                        value = Gac.LoadAssembly(assembly);
		//                        //foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
		//                        //{
		//                        //    string path=Path.Combine(assembly.CodeBase,referencedAssembly.Name);
		//                        //    if (File.Exists(path))
		//                        //    {
		//                        //        Assembly.LoadFile(path);
		//                        //    }
		//                        //}
		//                        dllLoaded = true;
		//                    }
		//                    catch (Exception e)
		//                    {
		//                        value = null;
		//                    }
		//                }
		//                else if (File.Exists(exeFile))
		//                {
		//                    try
		//                    {
		//                        Assembly assembly = Assembly.LoadFile(exeFile);
		//                        value = Gac.LoadAssembly(assembly);
		//                        //foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
		//                        //{
		//                        //    string path = Path.Combine(Path.GetDirectoryName(assembly.Location), referencedAssembly.Name+".dll");
		//                        //    if (File.Exists(path))
		//                        //    {
		//                        //        Assembly a=Assembly.LoadFile(path);
		//                        //        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
		//                        //        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
		//                        //    }
		//                        //}
		//                        dllLoaded = true;
		//                    }
		//                    catch (Exception e)
		//                    {
		//                        value = null;
		//                    }
		//                }
		//                if (!dllLoaded)
		//                {
		//                    if (File.Exists(file))
		//                    {
		//                        switch (Path.GetExtension(file))
		//                        {
		//                            case ".txt":
		//                            case ".meta":
		//                                value = new FileMap(file, new ListStrategy());
		//                                // this is problematic, writes the file all the time
		//                                //foreach (char c in File.ReadAllText(file))
		//                                ////foreach (char c in File.ReadAllText(file))
		//                                //{
		//                                //    value.Append(c);
		//                                //}
		//                                break;
		//                            default:
		//                                value = new FileMap(file, new ListStrategy());
		//                                // problematic, writes the file
		//                                //foreach (byte b in File.ReadAllBytes(file))
		//                                ////foreach (byte b in File.ReadAllBytes(file))
		//                                //{
		//                                //    value.Append(b);
		//                                //}
		//                                break;
		//                        }
		//                    }
		//                    else
		//                    {
		//                        DirectoryInfo subDir = new DirectoryInfo(Path.Combine(directory.FullName, name));
		//                        if (subDir.Exists)
		//                        {
		//                            value = new DirectoryMap(subDir);
		//                            //value = new DirectoryMap(subDir, this.Position);
		//                        }
		//                        else
		//                        {
		//                            value = null;
		//                        }
		//                    }
		//                }
		//            }
		//            if (value != null)
		//            {
		//                cache[key] = value;
		//            }
		//        }
		//    }
		//    return value;
		//}
		protected override void Set(Map key, Map val)
		{
			if (key.IsString)
			{
				string name = key.GetString();
				string extension = Path.GetExtension(name);
				if (extension == "")
				{
					string directoryPath = Path.Combine(this.directory.FullName, name);
					// somewhat unlogical
					if (Directory.Exists(directoryPath))
					{
						Map subDirectory = this[name];
						foreach (KeyValuePair<Map, Map> entry in val)
						{
							// buggy if there is a Meta file with the same name
							subDirectory[entry.Key] = entry.Value;
						}
					}
					else
					{
						string text = Meta.Serialize.ValueFunction(val);
						if (text == Syntax.emptyMap.ToString())
						{
							text = "";
						}
						else
						{
							text = text.Trim(Syntax.unixNewLine);
						}
						File.WriteAllText(Path.Combine(directory.FullName, name + ".meta"), text);
					}
				}
				else if (extension == ".txt" || extension == ".meta" || extension == ".html" || extension == ".htm")
				{
					File.WriteAllText(Path.Combine(directory.FullName, name), (string)Transform.TryToDotNet(val, typeof(string)));
				}
				else
				{
					File.WriteAllBytes(Path.Combine(directory.FullName, name), (byte[])Transform.TryToDotNet(val, typeof(byte[])));
				}
				cache[key] = val;
			}
			else
			{
				throw new ApplicationException("Cannot set non-string in directory.");
			}
		}
		protected override Map CopyData()
		{
			return this;
		}
		public class InteropSHFileOperation
		{
			public enum FO_Func : uint
			{
				FO_MOVE = 0x0001,
				FO_COPY = 0x0002,
				FO_DELETE = 0x0003,
				FO_RENAME = 0x0004,
			}

			struct SHFILEOPSTRUCT
			{
				public IntPtr hwnd;
				public FO_Func wFunc;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string pFrom;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string pTo;
				public ushort fFlags;
				public bool fAnyOperationsAborted;
				public IntPtr hNameMappings;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpszProgressTitle;

			}

			[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
			static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

			private SHFILEOPSTRUCT _ShFile;
			public FILEOP_FLAGS fFlags;

			public IntPtr hwnd
			{
				set
				{
					this._ShFile.hwnd = value;
				}
			}
			public FO_Func wFunc
			{
				set
				{
					this._ShFile.wFunc = value;
				}
			}

			public string pFrom
			{
				set
				{
					this._ShFile.pFrom = value + '\0' + '\0';
				}
			}
			public string pTo
			{
				set
				{
					this._ShFile.pTo = value + '\0' + '\0';
				}
			}

			public bool fAnyOperationsAborted
			{
				set
				{
					this._ShFile.fAnyOperationsAborted = value;
				}
			}
			public IntPtr hNameMappings
			{
				set
				{
					this._ShFile.hNameMappings = value;
				}
			}
			public string lpszProgressTitle
			{
				set
				{
					this._ShFile.lpszProgressTitle = value + '\0';
				}
			}

			public InteropSHFileOperation()
			{

				this.fFlags = new FILEOP_FLAGS();
				this._ShFile = new SHFILEOPSTRUCT();
				this._ShFile.hwnd = IntPtr.Zero;
				this._ShFile.wFunc = FO_Func.FO_COPY;
				this._ShFile.pFrom = "";
				this._ShFile.pTo = "";
				this._ShFile.fAnyOperationsAborted = false;
				this._ShFile.hNameMappings = IntPtr.Zero;
				this._ShFile.lpszProgressTitle = "";

			}

			public bool Execute()
			{
				this._ShFile.fFlags = this.fFlags.Flag;
				int ReturnValue = SHFileOperation(ref this._ShFile);
				if (ReturnValue == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			public class FILEOP_FLAGS
			{
				[Flags]
				private enum FILEOP_FLAGS_ENUM : ushort
				{
					FOF_MULTIDESTFILES = 0x0001,
					FOF_CONFIRMMOUSE = 0x0002,
					FOF_SILENT = 0x0004,  // don't create progress/report
					FOF_RENAMEONCOLLISION = 0x0008,
					FOF_NOCONFIRMATION = 0x0010,  // Don't prompt the user.
					FOF_WANTMAPPINGHANDLE = 0x0020,  // Fill in SHFILEOPSTRUCT.hNameMappings
					// Must be freed using SHFreeNameMappings
					FOF_ALLOWUNDO = 0x0040,
					FOF_FILESONLY = 0x0080,  // on *.*, do only files
					FOF_SIMPLEPROGRESS = 0x0100,  // means don't show names of files
					FOF_NOCONFIRMMKDIR = 0x0200,  // don't confirm making any needed dirs
					FOF_NOERRORUI = 0x0400,  // don't put up error UI
					FOF_NOCOPYSECURITYATTRIBS = 0x0800,  // dont copy NT file Security Attributes
					FOF_NORECURSION = 0x1000,  // don't recurse into directories.
					FOF_NO_CONNECTED_ELEMENTS = 0x2000,  // don't operate on connected elements.
					FOF_WANTNUKEWARNING = 0x4000,  // during delete operation, warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
					FOF_NORECURSEREPARSE = 0x8000,  // treat reparse points as objects, not containers
				}

				public bool FOF_MULTIDESTFILES = false;
				public bool FOF_CONFIRMMOUSE = false;
				public bool FOF_SILENT = false;
				public bool FOF_RENAMEONCOLLISION = false;
				public bool FOF_NOCONFIRMATION = false;
				public bool FOF_WANTMAPPINGHANDLE = false;
				public bool FOF_ALLOWUNDO = false;
				public bool FOF_FILESONLY = false;
				public bool FOF_SIMPLEPROGRESS = false;
				public bool FOF_NOCONFIRMMKDIR = false;
				public bool FOF_NOERRORUI = false;
				public bool FOF_NOCOPYSECURITYATTRIBS = false;
				public bool FOF_NORECURSION = false;
				public bool FOF_NO_CONNECTED_ELEMENTS = false;
				public bool FOF_WANTNUKEWARNING = false;
				public bool FOF_NORECURSEREPARSE = false;
				public ushort Flag
				{
					get
					{
						ushort ReturnValue = 0;

						if (this.FOF_MULTIDESTFILES == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_MULTIDESTFILES;
						if (this.FOF_CONFIRMMOUSE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_CONFIRMMOUSE;
						if (this.FOF_SILENT == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SILENT;
						if (this.FOF_RENAMEONCOLLISION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_RENAMEONCOLLISION;
						if (this.FOF_NOCONFIRMATION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMATION;
						if (this.FOF_WANTMAPPINGHANDLE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTMAPPINGHANDLE;
						if (this.FOF_ALLOWUNDO == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_ALLOWUNDO;
						if (this.FOF_FILESONLY == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_FILESONLY;
						if (this.FOF_SIMPLEPROGRESS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SIMPLEPROGRESS;
						if (this.FOF_NOCONFIRMMKDIR == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMMKDIR;
						if (this.FOF_NOERRORUI == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOERRORUI;
						if (this.FOF_NOCOPYSECURITYATTRIBS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCOPYSECURITYATTRIBS;
						if (this.FOF_NORECURSION == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSION;
						if (this.FOF_NO_CONNECTED_ELEMENTS == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NO_CONNECTED_ELEMENTS;
						if (this.FOF_WANTNUKEWARNING == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTNUKEWARNING;
						if (this.FOF_NORECURSEREPARSE == true)
							ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSEREPARSE;

						return ReturnValue;
					}
				}
			}
		}
	}
	//public class DirectoryMap : Map
	//{
	//    private DirectoryInfo directory;
	//    private List<Map> keys;

	//    public DirectoryMap(DirectoryInfo directory)
	//    {
	//        this.directory = directory;
	//    }
	//    private List<Map> GetKeys()
	//    {
	//        List<Map> keys = new List<Map>();
	//        try
	//        {
	//            foreach (DirectoryInfo subdir in directory.GetDirectories())
	//            {
	//                keys.Add(subdir.Name);
	//            }
	//            foreach (FileInfo file in directory.GetFiles("*.*"))
	//            {
	//                string fileName;
	//                if (file.Extension == ".meta" || file.Extension == ".dll" || file.Extension == ".exe")
	//                {
	//                    fileName = Path.GetFileNameWithoutExtension(file.FullName);
	//                }
	//                else
	//                {
	//                    fileName = file.Name;
	//                }
	//                keys.Add(fileName);
	//            }
	//        }
	//        // should only work in with drives, maybe separate DriveMap
	//        catch (Exception e)
	//        {
	//        }
	//        return keys;
	//    }
	//    protected override ICollection<Map> KeysImplementation
	//    {
	//        get
	//        {
	//            if (keys == null)
	//            {
	//                keys = GetKeys();
	//            }
	//            return keys; 
	//        }
	//    }
	//    public static void TrashFile(string fname)
	//    {
	//        InteropSHFileOperation fo = new InteropSHFileOperation();
	//        fo.wFunc = InteropSHFileOperation.FO_Func.FO_DELETE;
	//        fo.fFlags.FOF_ALLOWUNDO = true;
	//        fo.fFlags.FOF_NOCONFIRMATION = true;
	//        fo.pFrom = fname;
	//    }
	//    public Dictionary<Map, Map> cache = new Dictionary<Map, Map>();
	//    // fix
	//    protected override Map Get(Map key)
	//    {
	//        Map value = null;
	//        if (key.IsString)
	//        {
	//            if (key.GetString() == "basicTest")
	//            {
	//            }
	//            if (cache.ContainsKey(key))
	//            {
	//                value = cache[key];
	//            }
	//            else
	//            {
	//                string name = key.GetString();
	//                if (directory.FullName != Interpreter.LibraryPath)
	//                {
	//                    Directory.SetCurrentDirectory(directory.FullName);
	//                }
	//                string file = Path.Combine(directory.FullName, name);
	//                string metaFile = Path.Combine(directory.FullName, name + ".meta");
	//                string dllFile = Path.Combine(directory.FullName, name + ".dll");
	//                string exeFile = Path.Combine(directory.FullName, name + ".exe");

	//                if (File.Exists(metaFile))
	//                {
	//                    string text = File.ReadAllText(metaFile, Encoding.Default);
	//                    Map result;
	//                    FileMap fileMap = new FileMap(metaFile);
	//                    if (text != "")
	//                    {
	//                        Map start = new StrategyMap();
	//                        //Map start = new StrategyMap(new PersistantPosition(Position, key));
	//                        Parser parser = new Parser(text, metaFile);
	//                        bool matched;
	//                        result = Parser.File.Match(parser, out matched);
	//                        if (parser.index != parser.text.Length)
	//                        {
	//                            throw new SyntaxException("Expected end of file.", parser);
	//                        }
	//                        value = result;
	//                    }
	//                    else
	//                    {
	//                        value = Map.Empty;
	//                    }
	//                }
	//                else
	//                {
	//                    bool dllLoaded = false;
	//                    if (File.Exists(dllFile))
	//                    {
	//                        try
	//                        {
	//                            Assembly assembly = Assembly.LoadFile(dllFile);
	//                            value = Gac.LoadAssembly(assembly);
	//                            //foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
	//                            //{
	//                            //    string path=Path.Combine(assembly.CodeBase,referencedAssembly.Name);
	//                            //    if (File.Exists(path))
	//                            //    {
	//                            //        Assembly.LoadFile(path);
	//                            //    }
	//                            //}
	//                            dllLoaded = true;
	//                        }
	//                        catch (Exception e)
	//                        {
	//                            value = null;
	//                        }
	//                    }
	//                    else if(File.Exists(exeFile))
	//                    {
	//                        try
	//                        {
	//                            Assembly assembly = Assembly.LoadFile(exeFile);
	//                            value = Gac.LoadAssembly(assembly);
	//                            //foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
	//                            //{
	//                            //    string path = Path.Combine(Path.GetDirectoryName(assembly.Location), referencedAssembly.Name+".dll");
	//                            //    if (File.Exists(path))
	//                            //    {
	//                            //        Assembly a=Assembly.LoadFile(path);
	//                            //        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
	//                            //        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
	//                            //    }
	//                            //}
	//                            dllLoaded = true;
	//                        }
	//                        catch (Exception e)
	//                        {
	//                            value = null;
	//                        }
	//                    }
	//                    if (!dllLoaded)
	//                    {
	//                        if (File.Exists(file))
	//                        {
	//                            switch (Path.GetExtension(file))
	//                            {
	//                                case ".txt":
	//                                case ".meta":
	//                                    value = new FileMap(file, new ListStrategy());
	//                                    // this is problematic, writes the file all the time
	//                                    //foreach (char c in File.ReadAllText(file))
	//                                    ////foreach (char c in File.ReadAllText(file))
	//                                    //{
	//                                    //    value.Append(c);
	//                                    //}
	//                                    break;
	//                                default:
	//                                    value = new FileMap(file, new ListStrategy());
	//                                    // problematic, writes the file
	//                                    //foreach (byte b in File.ReadAllBytes(file))
	//                                    ////foreach (byte b in File.ReadAllBytes(file))
	//                                    //{
	//                                    //    value.Append(b);
	//                                    //}
	//                                    break;
	//                            }
	//                        }
	//                        else
	//                        {
	//                            DirectoryInfo subDir = new DirectoryInfo(Path.Combine(directory.FullName, name));
	//                            if (subDir.Exists)
	//                            {
	//                                value = new DirectoryMap(subDir);
	//                                //value = new DirectoryMap(subDir, this.Position);
	//                            }
	//                            else
	//                            {
	//                                value = null;
	//                            }
	//                        }
	//                    }
	//                }
	//                if (value != null)
	//                {
	//                    cache[key] = value;
	//                }
	//            }
	//        }
	//        return value;
	//    }
	//    protected override void Set(Map key, Map val)
	//    {
	//        if (key.IsString)
	//        {
	//            string name = key.GetString();
	//            string extension = Path.GetExtension(name);
	//            if (extension == "")
	//            {
	//                string directoryPath=Path.Combine(this.directory.FullName,name);
	//                // somewhat unlogical
	//                if (Directory.Exists(directoryPath))
	//                {
	//                    Map subDirectory = this[name];
	//                    foreach (KeyValuePair<Map, Map> entry in val)
	//                    {
	//                        // buggy if there is a Meta file with the same name
	//                        subDirectory[entry.Key] = entry.Value;
	//                    }
	//                }
	//                else
	//                {
	//                    string text = Meta.Serialize.ValueFunction(val);
	//                    if (text == Syntax.emptyMap.ToString())
	//                    {
	//                        text = "";
	//                    }
	//                    else
	//                    {
	//                        text = text.Trim(Syntax.unixNewLine);
	//                    }
	//                    File.WriteAllText(Path.Combine(directory.FullName, name + ".meta"), text);
	//                }
	//            }
	//            else if (extension == ".txt" || extension == ".meta" || extension==".html" || extension==".htm")
	//            {
	//                File.WriteAllText(Path.Combine(directory.FullName, name), (string)Transform.TryToDotNet(val, typeof(string)));
	//            }
	//            else
	//            {
	//                File.WriteAllBytes(Path.Combine(directory.FullName, name), (byte[])Transform.TryToDotNet(val, typeof(byte[])));
	//            }
	//            cache[key] = val;
	//        }
	//        else
	//        {
	//            throw new ApplicationException("Cannot set non-string in directory.");
	//        }
	//    }
	//    protected override Map CopyData()
	//    {
	//        return this;
	//    }
	//    public class InteropSHFileOperation
	//    {
	//        public enum FO_Func : uint
	//        {
	//            FO_MOVE = 0x0001,
	//            FO_COPY = 0x0002,
	//            FO_DELETE = 0x0003,
	//            FO_RENAME = 0x0004,
	//        }

	//        struct SHFILEOPSTRUCT
	//        {
	//            public IntPtr hwnd;
	//            public FO_Func wFunc;
	//            [MarshalAs(UnmanagedType.LPWStr)]
	//            public string pFrom;
	//            [MarshalAs(UnmanagedType.LPWStr)]
	//            public string pTo;
	//            public ushort fFlags;
	//            public bool fAnyOperationsAborted;
	//            public IntPtr hNameMappings;
	//            [MarshalAs(UnmanagedType.LPWStr)]
	//            public string lpszProgressTitle;

	//        }

	//        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	//        static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

	//        private SHFILEOPSTRUCT _ShFile;
	//        public FILEOP_FLAGS fFlags;

	//        public IntPtr hwnd
	//        {
	//            set
	//            {
	//                this._ShFile.hwnd = value;
	//            }
	//        }
	//        public FO_Func wFunc
	//        {
	//            set
	//            {
	//                this._ShFile.wFunc = value;
	//            }
	//        }

	//        public string pFrom
	//        {
	//            set
	//            {
	//                this._ShFile.pFrom = value + '\0' + '\0';
	//            }
	//        }
	//        public string pTo
	//        {
	//            set
	//            {
	//                this._ShFile.pTo = value + '\0' + '\0';
	//            }
	//        }

	//        public bool fAnyOperationsAborted
	//        {
	//            set
	//            {
	//                this._ShFile.fAnyOperationsAborted = value;
	//            }
	//        }
	//        public IntPtr hNameMappings
	//        {
	//            set
	//            {
	//                this._ShFile.hNameMappings = value;
	//            }
	//        }
	//        public string lpszProgressTitle
	//        {
	//            set
	//            {
	//                this._ShFile.lpszProgressTitle = value + '\0';
	//            }
	//        }

	//        public InteropSHFileOperation()
	//        {

	//            this.fFlags = new FILEOP_FLAGS();
	//            this._ShFile = new SHFILEOPSTRUCT();
	//            this._ShFile.hwnd = IntPtr.Zero;
	//            this._ShFile.wFunc = FO_Func.FO_COPY;
	//            this._ShFile.pFrom = "";
	//            this._ShFile.pTo = "";
	//            this._ShFile.fAnyOperationsAborted = false;
	//            this._ShFile.hNameMappings = IntPtr.Zero;
	//            this._ShFile.lpszProgressTitle = "";

	//        }

	//        public bool Execute()
	//        {
	//            this._ShFile.fFlags = this.fFlags.Flag;
	//            int ReturnValue = SHFileOperation(ref this._ShFile);
	//            if (ReturnValue == 0)
	//            {
	//                return true;
	//            }
	//            else
	//            {
	//                return false;
	//            }
	//        }

	//        public class FILEOP_FLAGS
	//        {
	//            [Flags]
	//            private enum FILEOP_FLAGS_ENUM : ushort
	//            {
	//                FOF_MULTIDESTFILES = 0x0001,
	//                FOF_CONFIRMMOUSE = 0x0002,
	//                FOF_SILENT = 0x0004,  // don't create progress/report
	//                FOF_RENAMEONCOLLISION = 0x0008,
	//                FOF_NOCONFIRMATION = 0x0010,  // Don't prompt the user.
	//                FOF_WANTMAPPINGHANDLE = 0x0020,  // Fill in SHFILEOPSTRUCT.hNameMappings
	//                // Must be freed using SHFreeNameMappings
	//                FOF_ALLOWUNDO = 0x0040,
	//                FOF_FILESONLY = 0x0080,  // on *.*, do only files
	//                FOF_SIMPLEPROGRESS = 0x0100,  // means don't show names of files
	//                FOF_NOCONFIRMMKDIR = 0x0200,  // don't confirm making any needed dirs
	//                FOF_NOERRORUI = 0x0400,  // don't put up error UI
	//                FOF_NOCOPYSECURITYATTRIBS = 0x0800,  // dont copy NT file Security Attributes
	//                FOF_NORECURSION = 0x1000,  // don't recurse into directories.
	//                FOF_NO_CONNECTED_ELEMENTS = 0x2000,  // don't operate on connected elements.
	//                FOF_WANTNUKEWARNING = 0x4000,  // during delete operation, warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
	//                FOF_NORECURSEREPARSE = 0x8000,  // treat reparse points as objects, not containers
	//            }

	//            public bool FOF_MULTIDESTFILES = false;
	//            public bool FOF_CONFIRMMOUSE = false;
	//            public bool FOF_SILENT = false;
	//            public bool FOF_RENAMEONCOLLISION = false;
	//            public bool FOF_NOCONFIRMATION = false;
	//            public bool FOF_WANTMAPPINGHANDLE = false;
	//            public bool FOF_ALLOWUNDO = false;
	//            public bool FOF_FILESONLY = false;
	//            public bool FOF_SIMPLEPROGRESS = false;
	//            public bool FOF_NOCONFIRMMKDIR = false;
	//            public bool FOF_NOERRORUI = false;
	//            public bool FOF_NOCOPYSECURITYATTRIBS = false;
	//            public bool FOF_NORECURSION = false;
	//            public bool FOF_NO_CONNECTED_ELEMENTS = false;
	//            public bool FOF_WANTNUKEWARNING = false;
	//            public bool FOF_NORECURSEREPARSE = false;
	//            public ushort Flag
	//            {
	//                get
	//                {
	//                    ushort ReturnValue = 0;

	//                    if (this.FOF_MULTIDESTFILES == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_MULTIDESTFILES;
	//                    if (this.FOF_CONFIRMMOUSE == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_CONFIRMMOUSE;
	//                    if (this.FOF_SILENT == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SILENT;
	//                    if (this.FOF_RENAMEONCOLLISION == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_RENAMEONCOLLISION;
	//                    if (this.FOF_NOCONFIRMATION == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMATION;
	//                    if (this.FOF_WANTMAPPINGHANDLE == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTMAPPINGHANDLE;
	//                    if (this.FOF_ALLOWUNDO == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_ALLOWUNDO;
	//                    if (this.FOF_FILESONLY == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_FILESONLY;
	//                    if (this.FOF_SIMPLEPROGRESS == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_SIMPLEPROGRESS;
	//                    if (this.FOF_NOCONFIRMMKDIR == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCONFIRMMKDIR;
	//                    if (this.FOF_NOERRORUI == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOERRORUI;
	//                    if (this.FOF_NOCOPYSECURITYATTRIBS == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NOCOPYSECURITYATTRIBS;
	//                    if (this.FOF_NORECURSION == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSION;
	//                    if (this.FOF_NO_CONNECTED_ELEMENTS == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NO_CONNECTED_ELEMENTS;
	//                    if (this.FOF_WANTNUKEWARNING == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_WANTNUKEWARNING;
	//                    if (this.FOF_NORECURSEREPARSE == true)
	//                        ReturnValue |= (ushort)FILEOP_FLAGS_ENUM.FOF_NORECURSEREPARSE;

	//                    return ReturnValue;
	//                }
	//            }
	//        }
	//    }
	//}
	public class Interpreter
	{
		public static bool profiling=false;
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				//AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
				if (args.Length == 0)
				{
					Commands.Interactive();
				}
				else
				{
					switch (args[0])
					{
						case "-interactive":
							Commands.Interactive();
							break;
						case "-test":
							//try
							//{
								Commands.Test();
							//}
							//catch (Exception e)
							//{
							//}
							Console.ReadLine();
							break;
						case "-help":
							Commands.Help();
							break;
						case "-profile":
							profiling = true;
							//new MetaTest.Basic().GetResult(out level);
							Commands.Profile();
							break;
						default:
							//throw new Exception("invalid command line");
							Commands.Run(args);
							break;
					}
				}
			}
			catch (MetaException e)
			{
				string text = e.ToString();
				//System.Diagnostics.Process.Start("devenv", e.Extent.FileName + @" /command ""Edit.GoTo " + e.Extent.Start.Line + "\"");
				if (useConsole)
				{
					Console.WriteLine(text);
					Console.ReadLine();
				}
				else
				{
					MessageBox.Show(text, "Meta exception");
				}
			}
			catch (Exception e)
			{
				string text = e.ToString();
				if (useConsole)
				{
					Console.WriteLine(text);
					Console.ReadLine();
				}
				else
				{
					MessageBox.Show(text, "Meta exception");
				}
			}
			//Console.ReadLine();
		}
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string path = Path.Combine(Directory.GetCurrentDirectory(), args.Name.Substring(0,args.Name.IndexOf(","))+".dll");
			if (File.Exists(path))
			{
				return Assembly.LoadFile(path);
			}
			else
			{
				return null;
			}
		}
		public class Commands
		{

			public static void Profile()
			{
				DateTime start = DateTime.Now;
				AllocConsole();
				int level;
				new Test.MetaTest.Profile().GetResult(out level);
				//new Test.MetaTest.Basic().GetResult(out level);
				Console.WriteLine((DateTime.Now - start).TotalSeconds);
				//Console.ReadLine();
			}
			public static void Help()
			{
				UseConsole();
				Console.WriteLine("help");
				Console.ReadLine();
			}
			private static string ReadLine()
			{
				string text = "";
			   ConsoleKeyInfo cki = new ConsoleKeyInfo();
				do 
				{
					while (Console.KeyAvailable == false)
						Thread.Sleep(250); // Loop until input is entered.
					cki = Console.ReadKey(true);
					text += cki.KeyChar;
					Console.Write(cki.KeyChar);

					//Console.WriteLine("You pressed the '{0}' key.", cki.Key);
					
				} 
				while(cki.Key != ConsoleKey.Enter);
				return text;
			}
			public static void Interactive()
			{
				UseConsole();
				Console.WriteLine("Interactive mode of Meta 0.2");
				object x = FileSystem.fileSystem;
				Map map = new StrategyMap();
				string code;



				Parser parser = new Parser("", "Interactive console");
				parser.defaultKeys.Push(1);
				PersistantPosition position = new PersistantPosition(new PersistantPosition(RootPosition.rootPosition,"filesystem"), "localhost" );
				//PersistantPosition position = new PersistantPosition(new Map[] { "filesystem", "localhost" });
				FunctionBodyKey calls;
				position.Get().AddCall(new StrategyMap(), out calls);
				PersistantPosition local=new PersistantPosition(position,calls);


				while (true)
				{
					code = "";
					Console.Write(parser.Line + " ");
					int lines = 0;
					while (true)
					{
						//string input = ReadLine();
						string input = Console.ReadLine();
						if (input.Trim().Length != 0)
						{
							code += input + Syntax.unixNewLine;
							char character = input[input.TrimEnd().Length - 1];
							if (!(Char.IsLetter(character) || character == ']' || character == Syntax.lookupStart) && !input.StartsWith("\t") && character != '=')
							{
								break;
							}
						}
						else
						{
							if (lines != 0)
							{
								break;
							}
						}
						lines++;
						Console.Write(parser.Line + lines + " ");
						for (int i = 0; i < input.Length && input[i] == '\t'; i++)
						{
							SendKeys.SendWait("{TAB}");
						}
					}
					try
					{
						bool matched;
						parser.text += code;
						Map statement = Parser.Statement.Match(parser, out matched);
						int count=local.Get().ArrayCount;
						if (matched)
						{
							if (parser.index == parser.text.Length)
							{
								statement.GetStatement().Assign(local);
								//statement.GetStatement().Assign(ref map);
								//statement.GetStatement().Assign(ref map);
							}
							else
							{
								parser.index = parser.text.Length;
								throw new SyntaxException("Syntax error", parser);
							}
							if (local.Get().ArrayCount > count)
							{
								Library.WriteLine(Serialize.ValueFunction(local.Get()[local.Get().ArrayCount]));
							}
						}
						Console.WriteLine();
					}
					catch (Exception e)
					{
						Console.WriteLine(e.ToString());
					}
				}
			}
			public static void Test()
			{
				UseConsole();
				new MetaTest().Run();
				Console.ReadLine();
			}
			public static void Run(string[] args)
			{
				//int i = 1;
				//int fileIndex = 0;
				////if (args[0] == "-console")
				////{
				////    UseConsole();
				////    i++;
				////    fileIndex++;
				////}
				//string path = args[fileIndex];
				//string startDirectory = Path.GetDirectoryName(path);
				//Directory.SetCurrentDirectory(startDirectory);
				////Environment.SetEnvironmentVariable("PATH", startDirectory+";"+Environment.GetEnvironmentVariable("PATH"));
				//string positionPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(args[fileIndex]));

				//string[] position = positionPath.Split(Path.DirectorySeparatorChar);
				//Map function = FileSystem.fileSystem["localhost"];
				//foreach (string pos in position)
				//{
				//    function = function[pos];
				//}

				//function.Call(Map.Empty);
			}
		}
		[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern bool AllocConsole();

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern IntPtr GetStdHandle(int nStdHandle);
		// should this be per process?
		public static bool useConsole = false;
		public static void UseConsole()
		{
			AllocConsole();
			useConsole = true;
		}
		public static string InstallationPath
		{
			get
			{
				return @"C:\Meta\0.2\";
			}
			//set
			//{
			//    installationPath = value;
			//}
		}
		public static string LibraryPath
		{
			get
			{
				return Path.Combine(InstallationPath,"Library");
			}
		}
	}
	public abstract class Position
	{
		public abstract Map Get();
	}
	public class TemporaryPosition:Position
	{
		private Map map;
		public TemporaryPosition(Map map)
		{
			this.map = map;
		}
		public override Map Get()
		{
			return map;
		}
	}
	// put assignment in here
	public class PersistantPosition : Position
	{
		public List<Map> Keys
		{
			get
			{
				PersistantPosition position = this;
				List<Map> keys = new List<Map>();
				while (position != null && position.key != null)
				{
					keys.Add(position.key);
					position = position.Parent;
				}
				keys.Reverse();
				return keys;
			}
		}
		public override string ToString()
		{
			string text = "";
			foreach (Map map in Keys)
			{
				text += map.ToString();
			}
			return text;
		}
		public override bool Equals(object obj)
		{
			PersistantPosition position=(PersistantPosition)obj;
			return position.key != null && position.key.Equals(key) && position.parent.Equals(parent);
		}
		protected PersistantPosition()
		{
		}
		private Map key;
		private PersistantPosition parent;

		public void Assign(Map key, Map value)
		{
			Get()[key] = value;
		}
		public void Assign(Map value)
		{
			PersistantPosition parent = this.Parent;
			Map parentMap = parent.Get();
			parentMap[key] = value;
		}
		public PersistantPosition(PersistantPosition parent, Map key)
		{
			this.parent = parent;
			this.key = key;
		}
		public PersistantPosition Parent
		{
			get
			{
				return parent;
			}
		}
		private Map cached;
		public override Map Get()
		{
			if (!CacheValid())
			{
				cached = DetermineMap();
			}
			return cached;
		}
		public virtual bool CacheValid()
		{
			return cached != null && Parent.CacheValid();
		}
		public virtual Map DetermineMap()
		{
			Map map = parent.Get();
			map.KeyChanged += new KeyChangedEventHandler(position_KeyChanged);
			Map result=map[key];
			if (result == null)
			{
				throw new ApplicationException("Position does not exist");
			}
			return result;
		}
		//public virtual Map DetermineMap()
		//{
		//    Map map = parent.DetermineMap()[key];
		//    //Map map = parent.Get()[key];
		//    if (map == null)
		//    {
		//        throw new Exception("Position does not exist");
		//    }
		//    map.KeyChanged+= new KeyChangedEventHandler(position_KeyChanged);
		//    return map;
		//}
		void position_KeyChanged(KeyChangedEventArgs e)
		{
			
			this.cached = null;
		}
	}
	public class RootPosition : PersistantPosition
	{
		public override bool Equals(object obj)
		{
			return obj is RootPosition;
		}
		public override bool CacheValid()
		{
			return true;
		}
		public static RootPosition rootPosition=new RootPosition();
		private RootPosition()
		{
		}
		public override Map Get()
		{
			return Gac.gac;
		}
		//public override Map DetermineMap()
		//{
		//    return Gac.gac;
		//}

	}

	//public class PersistantPosition : Position
	//{
	//    public void Assign(Map key, Map value)
	//    {
	//        Get()[key] = value;
	//    }
	//    public void Assign(Map value)
	//    {
	//        PersistantPosition parent = this.Parent;
	//        Map parentMap = parent.Get();
	//        parentMap[this.keys[this.keys.Length - 1]] = value;
	//    }
	//    public IEnumerable<Map> Keys
	//    {
	//        get
	//        {
	//            return keys;
	//        }
	//    }
	//    private readonly Map[] keys;
	//    public PersistantPosition(PersistantPosition position)
	//    {
	//        // inefficient
	//        this.keys = new List<Map>(position.keys).ToArray();
	//    }
	//    public PersistantPosition(ICollection<Map> keys)
	//    {
	//        this.keys = new List<Map>(keys).ToArray();
	//    }
	//    public PersistantPosition(PersistantPosition parent, Map ownKey)
	//    {
	//        List<Map> keyList = new List<Map>(parent.keys);
	//        keyList.Add(ownKey);
	//        this.keys = keyList.ToArray();
	//    }
	//    //public PersistantPosition(PersistantPosition parent, Map ownKey)
	//    //{
	//    //    List<Map> keyList=new List<Map>(parent.keys);
	//    //    keyList.Add(ownKey);
	//    //    this.keys = keyList.ToArray();
	//    //}
	//    private PersistantPosition parent;
	//    public PersistantPosition Parent
	//    {
	//        get
	//        {
	//            if (keys.Length == 0)
	//            {
	//                return null;
	//                //throw new Exception("Position does not have a parent.");
	//            }
	//            else
	//            {
	//                if (parent == null)
	//                {
	//                    List<Map> list = new List<Map>(this.Keys);
	//                    parent = new PersistantPosition(list.GetRange(0, list.Count - 1));
	//                }
	//                return parent;
	//            }
	//        }
	//    }
	//    private Map cached;
	//    public override Map Get()
	//    {
	//        if (cached == null)
	//        {
	//            cached = DetermineMap();
	//        }
	//        return cached;
	//    }
	//    private Map DetermineMap()
	//    {
	//        Map position = Gac.gac;
	//        int count = 0;
	//        foreach (Map key in keys)
	//        {
	//            position = position[key];
	//            if (position == null)
	//            {
	//                throw new Exception("Position does not exist");
	//            }
	//            position.KeyChanged += new KeyChangedEventHandler(position_KeyChanged);
	//            count++;
	//        }
	//        return position;
	//    }
	//    void position_KeyChanged()
	//    {
	//        this.cached = null;
	//    }
	//}
	public class FunctionBodyKey : Map
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return false;
		}
		public FunctionBodyKey(int id)
		{
			this.id = id;
		}
		private int id;
		public override bool Equals(object obj)
		{
			return obj is FunctionBodyKey && ((FunctionBodyKey)obj).id==id;
		}
		public override int GetHashCode()
		{
			return id.GetHashCode();
		}
		protected override Map Get(Map key)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		protected override void Set(Map key, Map val)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		protected override Map CopyData()
		{
			return this;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get 
			{
				return new List<Map>();
			}
		}
		public override bool IsNumber
		{
			get
			{
				return false;
			}
		}
		public override bool IsString
		{
			get
			{
				return false;
			}
		}
	}
	public class KeyChangedEventArgs : EventArgs
	{
		public KeyChangedEventArgs(Map key)
		{
			this.key = key;
		}
		private Map key;
		public Map Key
		{
			get
			{
				return key;
			}
		}
	}
	public delegate void KeyChangedEventHandler(KeyChangedEventArgs e);
	public abstract class Map: IEnumerable<KeyValuePair<Map,Map>>, ISerializeEnumerableSpecial
	{
		public override string ToString()
		{
			return Meta.Serialize.ValueFunction(this);
		}
		private Dictionary<FunctionBodyKey, Map> TemporaryData
		{
			get
			{
				if (tempData == null)
				{
					tempData = new Dictionary<FunctionBodyKey, Map>();
				}
				return tempData;
			}
		}
		//private Dictionary<FunctionBodyKey, Map> temporaryData = new Dictionary<FunctionBodyKey, Map>();
		public Map this[Map key]
		{
			get
			{
				if (key is FunctionBodyKey)
				{
					if (TemporaryData.ContainsKey((FunctionBodyKey)key))
					{
						return TemporaryData[(FunctionBodyKey)key];
					}
					else
					{
						return null;
					}
				}
				else
				{
					return Get(key);
				}
			}
			set
			{
				if (value != null)
				{
					// this isnt sufficient
					compiledCode = null;
					Map val;
					val = value.Copy();
					if (key is FunctionBodyKey)
					{
						this.TemporaryData[(FunctionBodyKey)key] = val;
					}
					else
					{
						Set(key, val);
					}
					if (KeyChanged != null)
					{
						this.KeyChanged(new KeyChangedEventArgs(key));
						this.KeyChanged = null;
					}
				}
			}
		}
		public event KeyChangedEventHandler KeyChanged;
		protected abstract Map Get(Map key);
		protected abstract void Set(Map key, Map val);
		private int numCalls = 0;
		// wrong name
		public Map AddCall(Map map, out FunctionBodyKey call)
		{
			numCalls++;
			call = new FunctionBodyKey(numCalls);
			this[call] = map;
			//this[call] = map;
			return this[call];
		}
		// remove int
		public void RemoveCall(int call)
		{
			this.TemporaryData.Remove(new FunctionBodyKey(call));
			//Remove(new FunctionBodyKey(call));
			if (KeyChanged != null)
			{
				this.KeyChanged(new KeyChangedEventArgs(new FunctionBodyKey(call)));
				this.KeyChanged = null;
			}
		}
		//public void RemoveCall(int call)
		//{
		//    //this.temporaryData.Remove(new FunctionBodyKey(call));
		//    Remove(new FunctionBodyKey(call));
		//    if (KeyChanged != null)
		//    {
		//        this.KeyChanged(new KeyChangedEventArgs(new FunctionBodyKey(call)));
		//        this.KeyChanged = null;
		//    }
		//}
		//public void RemoveCall(int call)
		//{
		//    Remove(new FunctionBodyKey(call));
		//    if (KeyChanged != null)
		//    {
		//        this.KeyChanged(new KeyChangedEventArgs(new FunctionBodyKey(call)));
		//        this.KeyChanged = null;
		//    }
		//}
		public virtual void AppendRange(Map array)
		{
			AppendRangeDefault(array);
		}
		public virtual void AppendRangeDefault(Map array)
		{
			int counter = ArrayCount + 1;
			foreach (Map map in array.Array)
			{
				this[counter] = map;
				counter++;
			}
		}
		private object compiledCode;

		public Statement GetStatement()
		{
			if (compiledCode == null)
			{
				compiledCode = new Statement(this);
			}
			return (Statement)compiledCode;
		}
		public Subselect GetSubselect()
		{
			if (compiledCode == null)
			{
				compiledCode = CreateSubselect();
			}
			return (Subselect)compiledCode;
		}
		public Expression GetExpression()
		{
			if (compiledCode == null)
			{
				compiledCode = CreateExpression();
			}
			return (Expression)compiledCode;
		}
		public Subselect CreateSubselect()
		{
			if (ContainsKey(CodeKeys.Current))
			{
				return new Current();
			}
			else if (ContainsKey(CodeKeys.Search))
			{
				return new Search(this[CodeKeys.Search]);
			}
			else if (ContainsKey(CodeKeys.Lookup))
			{
				return new Lookup(this[CodeKeys.Lookup]);
			}
			else if (ContainsKey(CodeKeys.Root))
			{
				return new Root();
			}
			else if (ContainsKey(CodeKeys.Call))
			{
				return new CallSubselect(this[CodeKeys.Call]);
			}
			else
			{
				throw new Exception("Map is not a subselect.");
			}
		}
		public Expression CreateExpression()
		{
			if (ContainsKey(CodeKeys.Call))
			{
				return new Call(this[CodeKeys.Call]);
			}
			else if (ContainsKey(CodeKeys.Program))
			{
				return new Program(this[CodeKeys.Program]);
			}
			else if (ContainsKey(CodeKeys.Literal))
			{
				return new Literal(this[CodeKeys.Literal]);
			}
			else if (ContainsKey(CodeKeys.Select))
			{
				return new Select(this[CodeKeys.Select]);
			}
			else
			{
				throw new ApplicationException("Cannot compile map.");
			}
		}
		public virtual void Append(Map map)
		{
			this[ArrayCount + 1] = map;
		}
		//public void AppendDefault(Map map)
		//{
		//    this[ArrayCount + 1] = map;
		//}
		//public void Append(Map map)
		//{
		//    this[ArrayCount + 1] = map;
		//}
		public static Map Empty
		{
			get
			{
				return new StrategyMap(new EmptyStrategy());
			}
		}
		public virtual string Serialize()
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
				return IsNumber && (GetNumber()==0 || GetNumber()==1);
			}
		}
		public virtual bool IsNumber
		{
			get
			{
				return IsNumberDefault;
			}
		}
		public virtual bool IsString
		{
			get
			{
				return IsStringDefault;
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
		public virtual Number GetNumber()
		{
			return GetNumberDefault();
		}
		public virtual string GetString()
		{
			return GetStringDefault();
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
		//public string GetStringDefault()
		//{
		//    string text = "";
		//    foreach (Map key in Keys)
		//    {
		//        text += Convert.ToChar(this[key].GetNumber().GetInt32());
		//    }
		//    return text;
		//}
		public Number GetNumberDefault()
		{
			Number number;
			if (this.Equals(Map.Empty))
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
		public PersistantPosition Scope
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
		public virtual int Count
		{
			get
			{
				return Keys.Count;
			}
		}
		public virtual int ArrayCount
		{
			get
			{
				return GetArrayCountDefault();
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
		public virtual List<Map> Array
		{
			get
			{
				List<Map> array = new List<Map>(Count);
				int index = 1;
				while (this.ContainsKey(index))
				{
					array.Add(this[index]);
				}
				return array;
			}
		}

		public virtual void Remove(Map key)
		{
			throw new ApplicationException("Method not implemented");
		}
		public virtual PersistantPosition Call(Map arg, PersistantPosition position)
		{
			if (!ContainsKey(CodeKeys.Function))
			{
				throw new ApplicationException("Map is not a function");
			}
			else
			{
				FunctionBodyKey call;
				AddCall(new StrategyMap(this[CodeKeys.Function][CodeKeys.ParameterName], arg), out call);
				PersistantPosition bodyPosition = new PersistantPosition(position, call);
				PersistantPosition result = this[CodeKeys.Function].GetExpression().Evaluate(bodyPosition);
				return result;
			}
		}
		public ICollection<Map> Keys
		{
			get
			{
				List<Map> keys=new List<Map>();
				foreach (Map key in KeysImplementation)
				{
					if (!(key is FunctionBodyKey))
					{
						keys.Add(key);
					}
				}
				return keys;
			}
		}
		protected abstract ICollection<Map> KeysImplementation
		{
			get;
		}
		public Map Copy()
		{
			Map clone = CopyData();
			// not really correct
			foreach (KeyValuePair<FunctionBodyKey, Map> entry in TemporaryData)
			{
				clone.TemporaryData[entry.Key] = entry.Value;
			}
			clone.numCalls = numCalls;
			clone.Scope = Scope;
			clone.Extent = Extent;
			return clone;
		}
		protected abstract Map CopyData();
		public bool ContainsKey(Map key)
		{
			if (key is FunctionBodyKey)
			{
				return TemporaryData.ContainsKey((FunctionBodyKey)key);
			}
			else
			{
				return ContainsKeyImplementation(key);
			}
			//return (key is FunctionBodyKey && TemporaryData.ContainsKey((FunctionBodyKey)key)) || KeysImplementation.Contains(key);
		}
		protected abstract bool ContainsKeyImplementation(Map key);
		//public virtual bool ContainsKey(Map key)
		//{
		//    if(key is FunctionBodyKey)
		//    {
		//        return TemporaryData.ContainsKey((FunctionBodyKey)key);
		//    }
		//    else
		//    {
		//        return KeysImplementation.Contains(key);
		//    }
		//    //return (key is FunctionBodyKey && TemporaryData.ContainsKey((FunctionBodyKey)key)) || KeysImplementation.Contains(key);
		//}
		public override int GetHashCode()
		{
			if (IsNumber)
			{
				//refactor, this is  totally wrong
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
				extent=value;
			}
		}
		public static implicit operator Map(Number integer)
		{
			return new StrategyMap(integer);
		}
		public static implicit operator Map(bool boolean)
		{
			return new StrategyMap(new Number((int)(boolean ? 1 : 0)));
		}
		public static implicit operator Map(char character)
		{
			return new StrategyMap(new Number(character));
		}
		public static implicit operator Map(byte integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(sbyte integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(uint integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(ushort integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(int integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(long integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(ulong integer)
		{
			return new StrategyMap(new Number(integer));
		}
		public static implicit operator Map(string text)
		{
			return new StrategyMap(text);
		}
		private PersistantPosition scope;
		private Dictionary<FunctionBodyKey, Map> tempData;
	}
	public class StrategyMap:Map
	{
		public override void Append(Map map)
		{
			strategy.Append(map);
		}
		public override void Remove(Map key)
		{
			strategy.Remove(key);
		}
		public StrategyMap(PersistantPosition scope)
			: this()
		{
			this.Scope = scope;
		}
		protected MapStrategy strategy;
		public StrategyMap(bool boolean)
			: this(new Number(boolean))
		{
		}
		public StrategyMap(System.Collections.Generic.ICollection<Map> list)
			: this(new ListStrategy())
		{
			int index = 1;
			foreach (object entry in list)
			{
				this[index] = Transform.ToMeta(entry);
				index++;
			}
		}

		public StrategyMap(MapStrategy strategy)
		{
			this.strategy = strategy;
			this.strategy.map = this;
		}
		public StrategyMap()
			: this(new EmptyStrategy())
		{
		}
		public StrategyMap(int i)
			: this(new Number(i))
		{
		}
		public StrategyMap(Number number)
			: this(new NumberStrategy(number))
		{
		}
		public StrategyMap(string text)
			: this(new StringStrategy(text))
			//: this(new ListStrategy(text))
		{
		}
		public StrategyMap(PersistantPosition scope, params Map[] keysAndValues)
			: this(keysAndValues)
		{
			this.Scope = scope;
		}
		public StrategyMap(params Map[] keysAndValues):this()
		{
			for (int i = 0; i <= keysAndValues.Length - 2; i += 2)
			{
				this[keysAndValues[i]] = keysAndValues[i + 1];
			}
		}
		public override int ArrayCount
		{
			get
			{
				return strategy.GetArrayCount();
			}
		}
		public override void AppendRange(Map array)
		{
			strategy.AppendRange(array);
		}
		public void InitFromStrategy(MapStrategy clone)
		{
			foreach (Map key in clone.Keys)
			{
				this[key] = clone.Get(key);
			}
		}
		public override bool IsString
		{
			get
			{
				return strategy.IsString;
			}
		}
		public override bool IsNumber
		{
			get
			{
				return strategy.IsNumber;
			}
		}
		public override Number GetNumber()
		{
			return strategy.GetNumber();
		}
		public override string GetString()
		{
			return strategy.GetString();
		}
		public override int Count
		{
			get
			{
				return strategy.Count;
			}
		}
		public override List<Map> Array
		{
			get
			{
				return strategy.Array;
			}
		}
		protected override Map Get(Map key)
		{
			return strategy.Get(key);
		}
		protected override void Set(Map key, Map value)
		{
			strategy.Set(key, value); 
		}
		protected override Map CopyData() 
		{ 
			return strategy.CopyData(); 
		}
		protected override bool ContainsKeyImplementation(Map key) 
		{ 
			return strategy.ContainsKey(key); 
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				return strategy.Keys;
			}
		}
		public override bool Equals(object toCompare)
		{
			bool isEqual;
			if (Object.ReferenceEquals(toCompare, this))
			{
				isEqual = true;
			}
			else if (toCompare is StrategyMap)
			{
				isEqual = ((StrategyMap)toCompare).strategy.EqualStrategy(strategy);
			}
			else
			{
				isEqual = false;
			}
			return isEqual;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
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
				strategy.map = this;
			}
		}
	}
	public class Transform
	{
		public static object ToDotNet(Map meta, Type target)
		{
			bool converted;
			object dotNet=ToDotNet(meta, target, out converted);
			if (!converted)
			{
				throw new ApplicationException("Cannot convert argument.");
			}
			return dotNet;
		}

		// combine with method below
		public static object ToDotNet(Map meta, Type target,out bool converted)
		{
			object dotNet = TryToDotNet(meta, target);
			converted = dotNet != null;
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
			DynamicMethod hello = new DynamicMethod("EventHandler",
				invoke.ReturnType,
				arguments.ToArray(),
				typeof(Map).Module);
			ILGenerator il = hello.GetILGenerator();

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
			FunctionBodyKey calls;
			// probably wrong
			MethodImplementation.currentPosition.Get().AddCall(code, out calls);
			PersistantPosition position = new PersistantPosition(MethodImplementation.currentPosition, calls);
			Delegate del = (Delegate)hello.CreateDelegate(delegateType, new MetaDelegate(position, invoke.ReturnType));
			//Delegate del = (Delegate)hello.CreateDelegate(delegateType, new MetaDelegate(code, invoke.ReturnType));
			return del;
		}


		public class EventHandlerContainer
		{
			private Map callable;
			public EventHandlerContainer(Map callable)
			{
				this.callable = callable;
			}
			public Map Raise(Map argument)
			{
				// not really accurate, should keep its own scope in callable, maybe
				return callable.Call(argument,MethodImplementation.currentPosition).Get();
				//return callable.Call(argument);
			}
		}
		public class MetaDelegate
		{
			private PersistantPosition callable;
			//private Map callable;
			private Type returnType;
			public MetaDelegate(PersistantPosition callable, Type returnType)
			{
				this.callable = callable;
				this.returnType = returnType;
			}
			public object Call(object[] arguments)
			{
				Map arg = new StrategyMap();
				foreach (object argument in arguments)
				{
					arg.Append(Transform.ToSimpleMeta(argument));
				}
				Map result = this.callable.Get().Call(arg, this.callable).Get();
				//Map result = this.callable.Get().Call(arg, MethodImplementation.currentPosition).Get();
				//Map result = this.callable.Call(arg, MethodImplementation.currentPosition).Get();
				//Map result = this.callable.Call(arg);
				return Meta.Transform.TryToDotNet(result, this.returnType);
			}
		}
		public static object TryToDotNet(Map meta, Type target)
		{
			object dotNet = null;
			if (target.IsSubclassOf(typeof(Enum)) && meta.IsNumber)
			{
				dotNet = Enum.ToObject(target, meta.GetNumber().GetInt32());
			}
			else
			{
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
					case TypeCode.DateTime:
						dotNet = null;
						break;
					case TypeCode.DBNull:
						dotNet = null;
						break;
					case TypeCode.Decimal:
						if (IsIntegerInRange(meta, decimal.MinValue, decimal.MaxValue))
						{
							// add function, GetDecimal
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
						if (target == typeof(Type) && meta is TypeMap)
						{
							dotNet = ((TypeMap)meta).type;
						}
						else if ((target.IsSubclassOf(typeof(Delegate)) || target.Equals(typeof(Delegate)))
								&& meta.ContainsKey(CodeKeys.Function))
						{
							dotNet = CreateDelegateFromCode(target, meta);
						}
						else if (meta is ObjectMap && target.IsAssignableFrom(((ObjectMap)meta).type))
						{
							dotNet = ((ObjectMap)meta).obj;
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
							bool converted=true;
							Type elementType=target.GetElementType();
							foreach (Map m in meta.Array)
							{
								object o = Transform.TryToDotNet(m, elementType);
								if (o != null)
								{
									list.Add(o);
								}
								else
								{
									converted=false;
									break;
								}
							}
							if (converted)
							{
								dotNet=list.ToArray(elementType);
							}
						}
						else
						{
							ObjectMap result;
							ConstructorInfo constructor = target.GetConstructor(BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
							if (constructor != null)
							{
								result = new ObjectMap(target.GetConstructor(new Type[] { }).Invoke(new object[] { }));
							}
							else if (target.IsValueType)
							{
								if (target.Equals(typeof(void)))
								{
									result = null;
									break;
								}
								else
								{
									result = new ObjectMap(target.InvokeMember(".ctor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Static, null, null, new object[] { }));
								}
							}
							else
							{
								break;
							}
							foreach (KeyValuePair<Map, Map> pair in meta)
							{
								// passing null as position is dangerous, currentPosition is wrong
								((Property)result[pair.Key])[DotNetKeys.Set].Call(pair.Value, MethodImplementation.currentPosition);
								//((Property)result[pair.Key])[DotNetKeys.Set].Call(pair.Value, null);
							}
							dotNet = result.Object;
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
			}
			if (dotNet == null)
			{
				if(meta is ObjectMap && ((ObjectMap)meta).type==target)
				{
					dotNet = ((ObjectMap)meta).Object;
				}
			}
			return dotNet;
		}
		public static bool IsIntegerInRange(Map meta,Number minValue,Number maxValue)
		{
			return meta.IsNumber && meta.GetNumber()>=minValue && meta.GetNumber()<=maxValue;
		}
		public static Map ToSimpleMeta(object dotNet)
		{
			if (dotNet == null)
			{
				return new ObjectMap(null, typeof(Object));
			}
			else if (dotNet is Map)
			{
				return (Map)dotNet;
			}
			else
			{
				return new ObjectMap(dotNet);
			}
		}
		public static Map ToMeta(object dotNet)
		{
			if (dotNet == null)
			{
				return Map.Empty;
			}
			else
			{
				switch(Type.GetTypeCode(dotNet.GetType()))
				{
					case TypeCode.Boolean:
						return Convert.ToInt32(((bool)dotNet));
					case TypeCode.Byte:
						return (byte)dotNet;
					case TypeCode.Char:
						return (char)dotNet;
					case TypeCode.DateTime:
						return new ObjectMap(dotNet);
					case TypeCode.DBNull:
						return new ObjectMap(dotNet);
					case TypeCode.Decimal:
						return (int)dotNet;
					case TypeCode.Double:
						return (int)dotNet;
					case TypeCode.Int16:
						return (short)dotNet;
					case TypeCode.Int32:
						return (int)dotNet;
					case TypeCode.Int64:
						return (long)dotNet;
					case TypeCode.Object:
						if(dotNet.GetType().IsSubclassOf(typeof(Enum)))
						{
							return (int)Convert.ToInt32((Enum)dotNet);
						}
						else if(dotNet is Map)
						{
							return (Map)dotNet;
						}
						else
						{
							return new ObjectMap(dotNet);
						}
					case TypeCode.SByte:
						return (sbyte)dotNet;
					case TypeCode.Single:
						return (int)dotNet;
					case TypeCode.String:
						return (string)dotNet;
					case TypeCode.UInt32:
						return (uint)dotNet;
					case TypeCode.UInt64:
						return (ulong)dotNet;
					case TypeCode.UInt16:
						return (ushort)dotNet;
					default:
						throw new ApplicationException("not implemented");
				}
			}
		}
	}
	public abstract class MethodImplementation:Map
	{
		// this should actually be a stack
		public static PersistantPosition currentPosition;
		//public static PersistantPosition currentPosition;
		protected MethodBase method;
		protected object obj;
		protected Type type;
		public MethodImplementation(MethodBase method, object obj, Type type)
		{
			this.method = method;
			this.obj = obj;
			this.type = type;
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
		public override PersistantPosition Call(Map argument, PersistantPosition position)
		{
			if (this.method.Name == "Write")
			{
			}
			currentPosition = position; // should copy this
			ParameterInfo[] parameters = method.GetParameters();
			object[] arguments = new object[parameters.Length];
			if (parameters.Length == 1)
			{
				arguments[0] = Transform.ToDotNet(argument, parameters[0].ParameterType);
			}
			else
			{
				for (int i = 0; i < parameters.Length; i++)
				{
					arguments[i] = Transform.ToDotNet(argument[i + 1], parameters[i].ParameterType);
				}
			}
			try
			{
				Map result=Transform.ToMeta(
					method is ConstructorInfo ?
						((ConstructorInfo)method).Invoke(arguments) :
						 method.Invoke(obj, arguments));
				FunctionBodyKey calls;
				// this should really be a function of a position and return a new position
				position.Get().AddCall(result, out calls);
				//this.AddCall(result, out calls);
				return new PersistantPosition(position, calls);
			}
			catch (Exception e)
			{
				throw new ApplicationException("implementation exception: "+e.InnerException.ToString()+e.StackTrace,e.InnerException);
			}
		}
	}
	public class Method : MethodImplementation
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return overloadedMethods!=null && overloadedMethods.ContainsKey(key);
		}
	    private Dictionary<Map, MethodOverload> overloadedMethods;
		// why overloads and method itself?
	    private Method(Dictionary<Map, MethodOverload> overloadedMethods,MethodBase method,object obj,Type type):base(method,obj,type)
	    {
	        this.overloadedMethods = overloadedMethods;
	    }
		// refactor
		public Method(string name, object obj, Type type)
			: base(GetSingleMethod(name, obj, type), obj, type)
		{
			MemberInfo[] methods = type.GetMember(name, GetBindingFlags(obj, name));
			// refactor
			if (methods.Length > 1)
			{
				this.overloadedMethods = new Dictionary<Map, MethodOverload>();
				foreach (MethodBase method in methods)
				{
					Map key;

					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1)
					{
						key = new TypeMap(parameters[0].ParameterType);
					}
					else
					{
						key = new StrategyMap();
						foreach (ParameterInfo parameter in parameters)
						{
							key.Append(new TypeMap(parameter.ParameterType));
						}
					}
					MethodOverload overload = new MethodOverload(method, obj, type);
					overloadedMethods[key] = overload;
				}
			}
			else
			{
				this.overloadedMethods = null;
			}
		}
		public Method(Type type)
			: this(".ctor", null, type)
		{
		}
		private static MethodBase GetSingleMethod(string name, object obj, Type type)
		{
			MemberInfo[] members = type.GetMember(name, GetBindingFlags(obj,name));
			if (members.Length == 1)
			{
				return (MethodBase)members[0];
			}
			else
			{
				return null;
			}
		}
		private static BindingFlags GetBindingFlags(object obj,string name)
		{
			if (name==".ctor" || obj != null)
			{
				return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			}
			else
			{
				return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			}
		}
	    protected override ICollection<Map> KeysImplementation
	    {
	        get
	        {
				if (overloadedMethods != null)
				{
					return overloadedMethods.Keys;
				}
				else
				{
					return new List<Map>();
				}
	        }
	    }

		protected override Map CopyData()
		{
			return new Method(overloadedMethods, method, obj, type);
		}
		protected override Map Get(Map key)
		{
			if (overloadedMethods == null)
			{
				return null;
			}
			else
			{
				MethodOverload value;
				overloadedMethods.TryGetValue(key, out value);
				return value;
			}
		}
	    protected override void Set(Map key, Map val)
	    {
			throw new Exception("Method Set not implemented");
	    }
	}
	public class MethodOverload : MethodImplementation
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return false;
		}
		public MethodOverload(MethodBase method, object obj, Type type)
			: base(method, obj, type)
		{
		}
		// is that correct?
	    protected override Map CopyData()
	    {
	        return new MethodOverload(this.method, this.obj, this.type);
	    }
	    protected override ICollection<Map> KeysImplementation
	    {
	        get
	        {
	            return new List<Map>();
	        }
	    }
	    protected override Map Get(Map key)
	    {
			return null;
	    }
	    protected override void Set(Map key, Map val)
	    {
			throw new ApplicationException("Cannot set key in MethodOverload");
	    }
		public override bool Equals(object toCompare)
		{
			if (toCompare is MethodOverload)
			{
				MethodOverload Method = (MethodOverload)toCompare;
				if (Method.obj == obj && Method.method.Name.Equals(method.Name) && Method.type.Equals(type))
				{
					return true;
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
		// why overriden here?
		// and not in MethodImplementation
	    public override int GetHashCode()
	    {
	        unchecked
	        {
	            int hash = method.Name.GetHashCode() * type.GetHashCode();
	            if (obj != null)
	            {
	                hash = hash * obj.GetHashCode();
	            }
	            return hash;
	        }
	    }
	}
	public class TypeMap: DotNetMap
	{
		public TypeMap(Type targetType)
			: base(null, targetType)
		{
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null || base.ContainsKey(key);
		}
		protected override Map Get(Map key)
		{
			// maybe generic types should be a separate class
			if (type.IsGenericTypeDefinition)
			{
				List<Type> types=new List<Type>();
				if (type.GetGenericArguments().Length == 1)
				{
					types.Add(((TypeMap)key).Type);
				}
				else
				{
					foreach (Map map in key.Array)
					{
						types.Add(((TypeMap)map).type);
					}
				}
				return new TypeMap(type.MakeGenericType(types.ToArray()));
			}
			else if (type == typeof(Array) && key is TypeMap)
			{
				return new TypeMap(((TypeMap)key).Type.MakeArrayType());
			}
			else if(base.Get(key)!=null)
			{
				return base.Get(key);
			}
			else
			{
				return this.Constructor[key];
			}
		}
		public Type Type
		{
			get
			{
				return type;
			}
		}
		public override int GetHashCode()
		{
			return type.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			return obj is TypeMap && ((TypeMap)obj).Type == this.type;
		}
		protected override Map CopyData()
		{
			return new TypeMap(this.type);
		}
		private Method constructor;
		private Method Constructor
		{
			get
			{
				if (constructor == null)
				{
					constructor = new Method(type);
				}
				return constructor;
			}
		}
		public override PersistantPosition Call(Map argument, PersistantPosition position)
		{
			return Constructor.Call(argument, position);
		}
	}
	public class ObjectMap: DotNetMap
	{
		public object Object
		{
			get
			{
				return obj;
			}
		}
		public ObjectMap(string text)
			: this(text, text.GetType())
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
			return obj.ToString();
		}
		protected override Map CopyData()
		{
			return new ObjectMap(obj);
		}
	}
	public class EmptyStrategy : MapStrategy
	{
		public override void Remove(Map key)
		{
			throw new Exception("Key cannot be removed because it does not exist.");
		}
		public override bool EqualStrategy(MapStrategy obj)
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
			return new StrategyMap(new EmptyStrategy());
		}
		public override void Set(Map key, Map val)
		{
			if (key.IsNumber)
			{
				if (key.Equals(Map.Empty) && val.IsNumber)
				{
					Panic(key, val, new NumberStrategy(0));
				}
				else
				{
					// that is not logical, maybe ListStrategy is not suitable for this, only if key==1
					Panic(key, val, new ListStrategy());
				}
			}
			else
			{
				Panic(key, val);
			}
		}
		public override Map Get(Map key)
		{
			return null;
		}
	}
	public class NumberStrategy : MapStrategy
	{
		public override void  Remove(Map key)
		{
			if(key.Equals(Map.Empty))
			{
				this.number=new Number(0);
			}
			else
			{
 				throw new Exception("The method or operation is not implemented.");
			}
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			return obj.IsNumber && obj.GetNumber().Equals(number);
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
					return new StrategyMap(new Number(number.Denominator));
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
		public override void Set(Map key, Map value)
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
				Panic(key, value);
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
			return new StrategyMap(new NumberStrategy(number));
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
	}
	public class StringStrategy : ArrayStrategy
	{
		protected override Map GetIndex(int i)
		{
			return text[i];
		}
		private string text;
		public StringStrategy(string text)
		{
			this.text = text;
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			if(obj is StringStrategy)
			{
				return ((StringStrategy)obj).text == text;
			}
			else
			{
				return base.EqualStrategy(obj);
			}
			//return (obj is StringStrategy && ((StringStrategy)obj).text == text) || base.EqualStrategy(obj);
		}
		public override void Remove(Map key)
		{
			Panic(new ListStrategy());
			map.Strategy.Remove(key);
		}
		public override void Set(Map key, Map val)
		{
			Panic(key, val);
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
				return false;
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
			return new StrategyMap(new CloneStrategy(this));
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
	public abstract class ArrayStrategy : MapStrategy
	{
		protected abstract Map GetIndex(int i);
		public override bool EqualStrategy(MapStrategy obj)
		{
			if (obj is ArrayStrategy)
			{
				return EqualArrayStrategy((ArrayStrategy)obj);
			}
			else if (obj is CloneStrategy)
			{
				return ((CloneStrategy)obj).EqualStrategy(this);
			}
			else
			{
				return EqualDefault(obj);
			}
		}
		private bool EqualArrayStrategy(ArrayStrategy strategy)
		{
			bool equal;
			if (Count == strategy.Count)
			{
				equal = true;
				for (int i = 0; i < strategy.Count; i++)
				{
					if (!GetIndex(i).Equals(strategy.GetIndex(i)))
					{
						equal = false;
						break;
					}
				}
			}
			else
			{
				equal = false;
			}
			return equal;
		}
	}
	public class ListStrategy : ArrayStrategy
	{
		protected override Map GetIndex(int i)
		{
			return list[i];
		}
		public override void Append(Map map)
		{
			list.Add(map);
		}
		public override void Remove(Map key)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		private List<Map> list;

		public ListStrategy():this(5)
		{
		}
		public ListStrategy(int capacity)
		{
			this.list = new List<Map>(capacity);
		}
		//public ListStrategy(string text)
		//{
		//    this.list = new List<Map>(text.Length);
		//    foreach (char c in text)
		//    {
		//        this.list.Add(new StrategyMap(c));
		//    }
		//}
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
		public override void Set(Map key, Map val)
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
					Panic(key, val);
				}
			}
			else
			{
				Panic(key, val);
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
					keys.Add(new StrategyMap(counter));
					counter++;
				}
				return keys;
			}
		}
		public override Map CopyData()
		{
			return new StrategyMap(new CloneStrategy(this));
		}
		public override void AppendRange(Map array)
		{
			foreach (Map map in array.Array)
			{
				this.list.Add(map.Copy());
			}
		}
		//public override bool EqualStrategy(MapStrategy obj)
		//{
		//    if (obj is ListStrategy)
		//    {
		//        bool equal;
		//        List<Map> otherData=((ListStrategy)obj).list;
		//        if (list.Count == otherData.Count)
		//        {
		//            equal = true;
		//            for (int i = 0; i < list.Count; i++)
		//            {
		//                if (!this.list[i].Equals(otherData[i]))
		//                {
		//                    equal = false;
		//                    break;
		//                }
		//            }
		//        }
		//        else
		//        {
		//            equal = false;
		//        }
		//        return equal;
		//    }
		//    else
		//    {
		//        return EqualDefault(obj);
		//    }
		//}
	}
	public class DictionaryStrategy:MapStrategy
	{
		public override void Remove(Map key)
		{
			dictionary.Remove(key);
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
		public override void Set(Map key, Map value)
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
		public override Map CopyData()
		{
			return new StrategyMap(new CloneStrategy(this));
		}
	}
	public class CloneStrategy : MapStrategy
	{
		public override void Remove(Map key)
		{
			Panic(new DictionaryStrategy());
			Remove(key);
		}
		private MapStrategy original;
		// always use CloneStrategy, only have logic in one place, too complicated to use
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
			map.Strategy = new CloneStrategy(this.original);
			return new StrategyMap(clone);
		}
		public override bool EqualStrategy(MapStrategy obj)
		{
			return obj.EqualStrategy(original);
			//return Object.ReferenceEquals(original, obj) || obj.EqualStrategy(original);
		}
		//public override bool EqualStrategy(MapStrategy obj)
		//{
		//    return Object.ReferenceEquals(original, obj) || obj.EqualStrategy(original);
		//    //return Object.ReferenceEquals(data, otherData) || otherData.Equal(this.data);
		//}
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
		public override void Set(Map key, Map value)
		{
			Panic(key, value);
		}
	}

	public abstract class MapStrategy
	{
		public virtual void Append(Map map)
		{
		    this.Set(GetArrayCount() + 1,map);
		}
		public abstract void Remove(Map key);
		// map is not really reliable, might have been copied
		public StrategyMap map;

		public abstract void Set(Map key, Map val);
		public abstract Map Get(Map key);

		public virtual bool ContainsKey(Map key)
		{
			return Keys.Contains(key);
		}

		public virtual int GetArrayCount()
		{
			return map.GetArrayCountDefault();
		}
		public virtual void AppendRange(Map array)
		{
			map.AppendRangeDefault(array);
		}



		public abstract Map CopyData();

		// refactor
		public void Panic(MapStrategy newStrategy)
		{
			map.Strategy = newStrategy;
			map.InitFromStrategy(this);
		}

		protected void Panic(Map key, Map val)
		{
			Panic(key, val, new DictionaryStrategy());
		}
		protected void Panic(Map key, Map val, MapStrategy newStrategy)
		{
			Panic(newStrategy);
			map.Strategy.Set(key, val); // why do it like this? this wont assign the parent, which is problematic!!!
		}
		public virtual bool IsNumber
		{
			get
			{
				return map.IsNumberDefault;
			}
		}
		public virtual bool IsString
		{
			get
			{
				return map.IsStringDefault;
			}
		}
		public virtual string GetString()
		{
			return map.GetStringDefault();
		}
		public virtual Number GetNumber()
		{
			return map.GetNumberDefault();
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
		//public abstract bool Equal(MapStrategy strategy);
		public virtual bool EqualStrategy(MapStrategy obj)
		{
			return EqualDefault((MapStrategy)obj);
		}
		public virtual bool EqualDefault(MapStrategy strategy)
		{
			if (Object.ReferenceEquals(strategy, this))
			{
				return true;
			}
			else if (((MapStrategy)strategy).Count != this.Count)
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
	public class Event:Map
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return key.Equals(DotNetKeys.Add);
		}
		Type type;
		object obj;
		EventInfo eventInfo;
		public Event(EventInfo eventInfo,object obj,Type type)
		{
			this.eventInfo=eventInfo;
			this.obj=obj;
			this.type=type;
		}
		public override PersistantPosition Call(Map argument, PersistantPosition position)
		{
			MethodImplementation.currentPosition = position;
			//MethodImplementation.currentPosition = new PersistantPosition(position);
			// binding flags arent really correct, should be different for static and instance events, combine with methodImplementation
			Delegate eventDelegate = (Delegate)type.GetField(eventInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(obj);
			if (eventDelegate != null)
			{
				List<object> arguments = new List<object>();
				ParameterInfo[] parameters = eventDelegate.Method.GetParameters();
				if (parameters.Length == 2)
				{
					arguments.Add(Transform.TryToDotNet(argument, parameters[1].ParameterType));
				}
				else
				{
					for (int i = 1; i < parameters.Length; i++)
					{
						arguments.Add(Transform.TryToDotNet(argument[i], parameters[i].ParameterType));
					}
				}
				Map result=new ObjectMap(eventDelegate.DynamicInvoke(arguments.ToArray())); // why not convert result???
				FunctionBodyKey calls;
				this.AddCall(result, out calls);
				return new PersistantPosition(position, calls);
			}
			else
			{
				return null;
			}
		}
		private Method add;
		protected override Map Get(Map key)
		{
			if (key.Equals(DotNetKeys.Add))
			{
				if (add == null)
				{
					add = new Method(eventInfo.GetAddMethod().Name, obj, type);
				}
				return add;
			}
			else
			{
				return null;
			}
		}
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot assign in event " + eventInfo.Name + ".");
		}

		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				List<Map> keys = new List<Map>();
				if (eventInfo.GetAddMethod() != null)
				{
					keys.Add(DotNetKeys.Add);
				}

				return keys;
			}
		}
		protected override Map CopyData()
		{
			return new Event(eventInfo, obj, type);
		}
	}
	public class Property:Map
	{
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
		object obj;
		Type type;
		PropertyInfo property;
		public Property(PropertyInfo property,object obj,Type type)
		{
			this.property=property;
			this.obj=obj;
			this.type=type;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				List<Map> keys=new List<Map>();
				if(property.GetGetMethod()!=null)
				{
					keys.Add(DotNetKeys.Get);
				}
				if(property.GetSetMethod()!=null)
				{
					keys.Add(DotNetKeys.Set);
				}
				return keys;
			}
		}
		private Method get;
		private Method set;
		protected override Map Get(Map key)
		{
			if(key.Equals(DotNetKeys.Get))
			{
				if (get == null)
				{
					get = new Method(property.GetGetMethod().Name, obj, type);
				}
				return get;
				//return new Method(property.GetGetMethod().Name, obj, type);
			}
			else if(key.Equals(DotNetKeys.Set))
			{
				if(set==null)
				{
					set=new Method(property.GetSetMethod().Name, obj, type);
				}
				return set;
				//return new Method(property.GetSetMethod().Name, obj, type);
			}
			else
			{
				return null;
			}
		}
		protected override void Set(Map key,Map val)
		{
			throw new ApplicationException("Cannot assign in property.");
		}
		protected override Map CopyData()
		{
			return new Property(property, obj, type);
		}
	}
	public abstract class DotNetMap : Map, ISerializeEnumerableSpecial
	{
		private Dictionary<Map, Map> data=new Dictionary<Map,Map>();
		// rename NonSerialized
		[NonSerialized]
		public object obj;
		[NonSerialized]
		public Type type;
		private BindingFlags bindingFlags;

		public DotNetMap(object obj, Type type)
		{
			// combine this with MethodImplementation, maybe add a Reflection class
			if (obj == null)
			{
				this.bindingFlags = BindingFlags.Public | BindingFlags.Static;
			}
			else
			{
				this.bindingFlags = BindingFlags.Public | BindingFlags.Instance;
			}
			this.obj = obj;
			this.type = type;
		}
		protected override Map Get(Map key)
		{
			if (data.ContainsKey(key))
			{
				return data[key];
			}
			else if (key.IsString)
			{
				string memberName = key.GetString();
				MemberInfo[] foundMembers = type.GetMember(memberName, bindingFlags);
				if (foundMembers.Length != 0)
				{
					MemberInfo member = foundMembers[0];
					Map result;
					if (member is MethodBase)
					{
						result=new Method(memberName, obj, type);
					}
					else if (member is PropertyInfo)
					{
						result=new Property(type.GetProperty(memberName), this.obj, type);
					}
					else if (member is FieldInfo)
					{
						result=Transform.ToMeta(type.GetField(memberName).GetValue(obj));
					}
					else if (member is EventInfo)
					{
						result=new Event(((EventInfo)member), obj, type);
					}
					else if (member is Type)
					{
						result=new TypeMap((Type)member);
					}
					else
					{
						result=null;
					}
					if (result != null)
					{
						data[key] = result;
					}
					return result;
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
		protected override void Set(Map key, Map value)
		{
			string fieldName = key.GetString();
			FieldInfo field = type.GetField(fieldName, bindingFlags);
			if (field != null)
			{
				field.SetValue(obj, Transform.ToDotNet(value, field.FieldType));
			}
			else
			{
				data[key] = value;
			}
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return key.IsString && this.type.GetMember(key.GetString(), bindingFlags).Length != 0;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				List<Map> keys = new List<Map>();
				foreach (MemberInfo member in this.type.GetMembers(bindingFlags))
				{
					keys.Add(new StrategyMap(member.Name));
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

		public override string Serialize()
		{
			return obj != null ? this.obj.ToString() : this.type.ToString();
		}
		public string Serialize(string indent, string[] functions)
		{
			return indent;
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
		protected abstract string TestDirectory
		{
			get;
		}
		public abstract class Test
		{
			public abstract object GetResult(out int level);
		}
		public void Run()
		{
			bool allTestsSucessful = true;
			foreach(Type testType in this.GetType().GetNestedTypes())
			{
				if (testType.IsSubclassOf(typeof(Test)))
				{
					Test test=(Test)testType.GetConstructor(new Type[]{}).Invoke(null);
					int level;
					Console.Write(testType.Name + "...");


					DateTime startTime = DateTime.Now;
					object result=test.GetResult(out level);
					TimeSpan duration = DateTime.Now - startTime;


					string testDirectory = Path.Combine(TestDirectory, testType.Name);
					
					string resultPath = Path.Combine(testDirectory, "result.txt");
					string checkPath = Path.Combine(testDirectory, "check.txt");

					Directory.CreateDirectory(testDirectory);
					if (!File.Exists(checkPath))
					{
						File.Create(checkPath).Close();
					}

					StringBuilder stringBuilder = new StringBuilder();
					Serialize(result, "", stringBuilder, level);

					File.WriteAllText(resultPath, stringBuilder.ToString(), Encoding.Default);
					string successText;
					if (!File.ReadAllText(resultPath).Equals(File.ReadAllText(checkPath)))
					{
						successText = "failed";
						allTestsSucessful = false;
					}
					else
					{
						successText = "succeeded";
					}
					Console.WriteLine(" " + successText + "  " + duration.TotalSeconds.ToString() + " s");
				}
			}
			if (!allTestsSucessful)
			{
				Console.ReadLine();
			}
		}
		public const char indentationChar = '\t';

		private bool UseToStringMethod(Type type)
		{
			return (!type.IsValueType || type.IsPrimitive)
				&& Assembly.GetAssembly(type) != Assembly.GetExecutingAssembly();
		}
		private bool UseProperty(PropertyInfo property,int level)
		{
			object[] attributes=property.GetCustomAttributes(typeof(SerializeAttribute), false);

			return Assembly.GetAssembly(property.DeclaringType) != Assembly.GetExecutingAssembly()
				|| (attributes.Length == 1 && ((SerializeAttribute)attributes[0]).Level >= level);
		}
		public void Serialize(object obj,string indent,StringBuilder builder,int level) 
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
	public class Extent
	{
		private SourcePosition start;
		private SourcePosition end;
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
	public class Syntax
	{
		public const char autokey = '.';
		public const char callStart = '(';
		public const char callEnd = ')';
		public const char root = '/';
		public const char search='$';
		public const char negative='-';
		public const char fraction = '/';
		public const char endOfFile = (char)65535;
		public const char indentation = '\t';
		public const char unixNewLine = '\n';
		public const string windowsNewLine = "\r\n";
		public const char function = '|';
		public const char shortFunction = ':';
		public const char @string = '\"';
		public const char lookupStart = '[';
		public const char lookupEnd = ']';
		public const char emptyMap = '0';
		public const char call = ' ';
		public const char select = '.';
		public const char stringEscape = '\'';
		public const char assignment = ' ';
		//public const char statement = '=';
		public const char space = ' ';
		public const char tab = '\t';
		public const string current = "current";
		public static char[] integer = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static char[] lookupStringForbidden = new char[] { call, indentation, '\r', '\n', assignment,select, stringEscape, shortFunction, function, @string, lookupStart, lookupEnd, emptyMap, search, root, callStart, callEnd };
		//public static char[] lookupStringForbidden = new char[] { call, indentation, '\r', '\n', statement, select, stringEscape, shortFunction, function, @string, lookupStart, lookupEnd, emptyMap, search, root, callStart, callEnd };
	}
	
	// refactor completely
	public class Parser
	{
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
			return new Alternatives(EmptyMap, NumberExpression, StringExpression, Program, Call, ShortFunctionExpression, Select);
		});
		public static Rule NewLine = 
			new Alternatives(
				new Character(Syntax.unixNewLine),
				StringRule(Syntax.windowsNewLine));

		public static Rule EndOfLine =
			new Sequence(
				new Action(new Match(),new ZeroOrMore(
					new Action(new Match(),new Alternatives(
						new Character(Syntax.space),
						new Character(Syntax.tab))))),
				new Action(new Match(),NewLine));

		public static Rule Integer =
			new Sequence(
				new Action(new CustomAction(
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
									new CustomAction(
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
							new CustomAction(delegate(Parser p, Map map, ref Map result)
							{
								if (result.GetNumber() > 0 && p.negative)
								{
									result = 0 - result.GetNumber();
								}
								return null;
							}),
							new CustomRule(delegate(Parser p, out bool matched) { matched = true; return null; })
							))));
		public static Rule Indentation =
		new Alternatives(
			new CustomRule(delegate(Parser p, out bool matched)
	{
		if (p.isStartOfFile)
		{
			p.isStartOfFile = false;
			p.indentationCount++;
			matched = true;
			return null;
		}
		else
		{
			matched = false;
			return null;
		}
	}),
			new Sequence(
				new Action(new Match(),new Sequence(
					new Action(new Match(),EndOfLine),
					new Action(new Match(),new CustomRule(delegate(Parser p, out bool matched)
	{
		return StringRule("".PadLeft(p.indentationCount + 1, Syntax.indentation)).Match(p, out matched);
	})))),
				new Action(new Match(),new CustomRule(delegate(Parser p, out bool matched)
	{
		p.indentationCount++;
		matched = true;
		return null;
	}))));
		private static Rule EndOfLinePreserve = 
			new Sequence(
			// this is a bug, text should be preserved
				new Action(new Match(), //new FlattenRule(
					new ZeroOrMore(
							new Action(new Autokey(), new Alternatives(
								new Character(Syntax.space),
								new Character(Syntax.tab))))),
				new Action(new Append(),
					new Alternatives(
						new Character(Syntax.unixNewLine),
						StringRule(Syntax.windowsNewLine))));

		public static Rule SameIndentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			return StringRule("".PadLeft(pa.indentationCount, Syntax.indentation)).Match(pa, out matched);
		});
		private static Rule StringLine = new ZeroOrMore(new Action(new Autokey(), new CharacterExcept(Syntax.unixNewLine, Syntax.windowsNewLine[0])));
		public static Rule StringDedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			Map map = new Sequence(
				new Action(new Match(), EndOfLine),
				new Action(new Match(), StringRule("".PadLeft(pa.indentationCount - 1, Syntax.indentation)))).Match(pa, out matched);
			if (matched)
			{
				pa.indentationCount--;
			}
			return map;
		});
		public static Rule String = new Alternatives(
				new Sequence(
					new Action(
						new Match(), new Character(Syntax.@string)),
					new Action(
						new ReferenceAssignment(),
						new OneOrMore(
							new Action(
								new Autokey(),
								new CharacterExcept(
									Syntax.unixNewLine,
									Syntax.windowsNewLine[0],
									Syntax.@string)))),
					new Action(new Match(), new Character(Syntax.@string))),
				new Sequence(
					new Action(new Match(), new Character(Syntax.@string)),
					new Action(new Match(), Indentation),
					new Action(new ReferenceAssignment(), new Sequence(
								new Action(new Append(), StringLine),
								new Action(new Append(),
										new ZeroOrMore(
											new Action(new Append(),
												new Sequence(
													new Action(new Match(), EndOfLinePreserve),
													new Action(new Match(), SameIndentation),
													new Action(new ReferenceAssignment(),
														//new FlattenRule(
															new Sequence(
																new Action(new Append(), new LiteralRule(Syntax.unixNewLine.ToString())),
																new Action(new Append(), StringLine)
																)))))))),
					new Action(new Match(), StringDedentation),
					new Action(new Match(), new Character(Syntax.@string))));

		public static Rule Number = new Sequence(
			new Action(new ReferenceAssignment(),
				Integer),
			new Action(
				new Assignment(
					NumberKeys.Denominator),
					new Optional(
						new Sequence(
							new Action(
								new Match(),
								new Character(Syntax.fraction)),
							new Action(
								new ReferenceAssignment(),
								Integer)))));
		public static Rule LookupString = new OneOrMore(
			new Action(
				new Autokey(),
					new CharacterExcept(
					Syntax.lookupStringForbidden)));

		// refactor
		public static Rule Map = new CustomRule(delegate(Parser parser, out bool matched)
		{
			Indentation.Match(parser, out matched);
			Map map = new StrategyMap();
			if (matched)
			{
				parser.defaultKeys.Push(1);
				map = Entry.Match(parser, out matched);
				if (matched)
				{
					while (true)
					{
						if (parser.Rest == "")
						{
							break;
						}
						new Alternatives(
							SameIndentation,
							Dedentation).Match(parser, out matched);
						if (matched)
						{
							map = Library.Merge(new StrategyMap(
								1, map,
								2, Entry.Match(parser, out matched)));
							//map.Append(Entry.Match(parser, out matched));
						}
						else
						{
							matched = true;
							break;
						}
						if (parser.Rest == "")
						{
							break;
						}
					}
				}
				parser.defaultKeys.Pop();
			}
			return map;
		});
		//// refactor
		//public static Rule Map = new CustomRule(delegate(Parser parser, out bool matched)
		//{
		//    Indentation.Match(parser, out matched);
		//    Map map = new StrategyMap();
		//    if (matched)
		//    {
		//        parser.defaultKeys.Push(1);
		//        map = Entry.Match(parser, out matched);
		//        if (matched)
		//        {
		//            while (true)
		//            {
		//                if (parser.Rest == "")
		//                {
		//                    break;
		//                }
		//                new Alternatives(
		//                    SameIndentation,
		//                    Dedentation).Match(parser, out matched);
		//                if (matched)
		//                {
		//                    map = Library.Merge(new StrategyMap(
		//                        1, map,
		//                        2, Entry.Match(parser, out matched)));
		//                    //map.Append(Entry.Match(parser, out matched));
		//                }
		//                else
		//                {
		//                    matched = true;
		//                    break;
		//                }
		//                if (parser.Rest == "")
		//                {
		//                    break;
		//                }
		//            }
		//        }
		//        parser.defaultKeys.Pop();
		//    }
		//    return map;
		//});
		public static Rule ExpressionData = new DelayedRule(delegate()
		{
			return Parser.Expression;
		});
		public static Rule ShortFunction = new Sequence(
				new Action(new Assignment(
					CodeKeys.Function),
					new Sequence(
					new Action(new Merge(),
						new Sequence(
							new Action(new Assignment(
								CodeKeys.ParameterName),
								new ZeroOrMore(
									new Action(new Autokey(),
										new CharacterExcept(
											Syntax.shortFunction,
											Syntax.unixNewLine)))))),
					new Action(new Merge(),
						new Sequence(
							new Action(new Match(),
								new Character(Syntax.shortFunction)),
							new Action(new ReferenceAssignment(), ExpressionData))))));//.Match(p, out matched);

		public static Rule Value = new Alternatives(
			Map,
			String,
			Number,
			ShortFunction
			);

		private static Rule LookupAnything =
			new Sequence(
				new Action(new Match(),new Character((Syntax.lookupStart))),
				new Action(new ReferenceAssignment(),Value),
				new Action(new Match(),new ZeroOrMore(
					new Action(new Match(),new Character(Syntax.indentation)))),
				new Action(new Match(),new Character(Syntax.lookupEnd)));
		// refactor
		public static Rule Entry = new CustomRule(delegate(Parser parser, out bool matched)
		{
			Map result = new StrategyMap();
			if (parser.Rest.StartsWith("replace"))
			{
			}
			Map function = Parser.FunctionExpression.Match(parser, out matched);
			if (matched)
			{
				result[CodeKeys.Function] = function[CodeKeys.Value][CodeKeys.Literal];
			}
			else
			{
				Map key = new Alternatives(LookupString, LookupAnything).Match(parser, out matched);
				if (matched)
				{
					StringRule(Syntax.assignment.ToString()).Match(parser, out matched);
					//StringRule(Syntax.statement.ToString()).Match(parser, out matched);
					//if (matched)
					//{
						Map value = Value.Match(parser, out matched);
						result[key] = value;

						// i dont understand this
						bool match;
						if (EndOfLine.Match(parser, out match) == null && parser.Look() != Syntax.endOfFile)
						{
							parser.index -= 1;
							if (EndOfLine.Match(parser, out match) == null)
							{
								parser.index -= 1;
								if (EndOfLine.Match(parser, out match) == null)
								{
									parser.index += 2;
									//matched = false;
									//return null;
									throw new SyntaxException("Expected newline.", parser);
								}
								else
								{
									parser.line--;
								}
							}
							else
							{
								parser.line--;
							}
						}
					//}
				}
			}
			return result;
		});
		//// refactor
		//public static Rule Entry = new CustomRule(delegate(Parser parser, out bool matched)
		//{
		//    Map result = new StrategyMap();
		//    Map function = Parser.FunctionExpression.Match(parser, out matched);
		//    if (matched)
		//    {
		//        result[CodeKeys.Function] = function[CodeKeys.Value][CodeKeys.Literal];
		//    }
		//    else
		//    {
		//        Map key = new Alternatives(LookupString, LookupAnything).Match(parser, out matched);
		//        if (matched)
		//        {
		//            StringRule(Syntax.assignment.ToString()).Match(parser, out matched);
		//            //StringRule(Syntax.statement.ToString()).Match(parser, out matched);
		//            if (matched)
		//            {
		//                Map value = Value.Match(parser, out matched);
		//                result[key] = value;

		//                // i dont understand this
		//                bool match;
		//                if (EndOfLine.Match(parser, out match) == null && parser.Look() != Syntax.endOfFile)
		//                {
		//                    parser.index -= 1;
		//                    if (EndOfLine.Match(parser, out match) == null)
		//                    {
		//                        parser.index -= 1;
		//                        if (EndOfLine.Match(parser, out match) == null)
		//                        {
		//                            parser.index += 2;
		//                            //matched = false;
		//                            //return null;
		//                            throw new SyntaxException("Expected newline.", parser);
		//                        }
		//                        else
		//                        {
		//                            parser.line--;
		//                        }
		//                    }
		//                    else
		//                    {
		//                        parser.line--;
		//                    }
		//                }
		//            }
		//        }
		//    }
		//    return result;
		//});

		public static Rule Function = new CustomRule(delegate(Parser p, out bool matched)
		{
			Map result = new Sequence(
				new Action(new Merge(),
					new Sequence(
						new Action(new Assignment(
							CodeKeys.ParameterName),
							new ZeroOrMore(
							new Action(new Autokey(),
								new CharacterExcept(
									Syntax.function,
									Syntax.unixNewLine)))))),
				new Action(new Merge(),
					new Sequence(
						new Action(new Match(),
							new Character(
								Syntax.function)),
						new Action(new ReferenceAssignment(),
							ExpressionData)))).Match(p, out matched);
			return result;
		});
		public static Rule File = new Sequence(
			new Action(new Match(),
				new Optional(
					new Sequence(
						new Action(new Match(),
							StringRule("#!")),
						new Action(new Match(),
							new ZeroOrMore(
								new Action(new Match(),
									new CharacterExcept(Syntax.unixNewLine)))),
						new Action(new Match(),EndOfLine)))),
			new Action(new ReferenceAssignment(),Map));

		public static Rule ExplicitCall = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Assignment(
					CodeKeys.Call),
					new Sequence(
						new Action(new Match(),new Character(Syntax.callStart)),
						new Action(new Assignment(CodeKeys.Callable),Select),
						new Action(new Assignment(
							CodeKeys.Argument),
							new Alternatives(
								new Sequence(
									new Action(new Match(),new Character(Syntax.call)),
									new Action(new ReferenceAssignment(),Expression)),
								Program)),
						new Action(new Match(),new Character(Syntax.callEnd)))));
		});

		public static Rule Call = new DelayedRule(delegate()
		{
			return new Sequence(
				new Action(new Assignment(
					CodeKeys.Call),
						new Sequence(
							new Action(new Assignment(
								CodeKeys.Callable),
								new Alternatives(
									Select,
									ExplicitCall)),
							new Action(new Assignment(
								CodeKeys.Argument),
								new Alternatives(
									new Sequence(
										new Action(new Match(),
											new Alternatives(
												new Character(Syntax.call),
												new Character(Syntax.indentation))),
										new Action(new ReferenceAssignment(),Expression)),
									Program)))));
		});

		public static Rule Dedentation = new CustomRule(delegate(Parser pa, out bool matched)
		{
			pa.indentationCount--;
			matched = false;
			return null;
		});

		private static Rule StringExpression = new CustomRule(delegate(Parser parser, out bool matched)
		{
			return new Sequence(
					new Action(new Assignment(
						CodeKeys.Literal),
						String)).Match(parser, out matched);
		});

		public static Rule ShortFunctionExpression = new Sequence(
			new Action(new Assignment(
				CodeKeys.Literal),
				ShortFunction));

		public static Rule FunctionExpression = new Sequence(
			new Action(new Assignment(CodeKeys.Key), new LiteralRule(new StrategyMap(1, new StrategyMap(CodeKeys.Lookup, new StrategyMap(CodeKeys.Literal, CodeKeys.Function))))),
			new Action(new Assignment(CodeKeys.Value), new Sequence(
				new Action(new Assignment(CodeKeys.Literal), Function))));

		private Rule Whitespace =
			new ZeroOrMore(
				new Action(new Match(),
					new Alternatives(
						new Character(Syntax.tab),
						new Character(Syntax.space))));

		private static Rule EmptyMap = new Sequence(
			new Action(new Assignment(CodeKeys.Literal), new Sequence(
				new Action(new Match(),
					new Character(Syntax.emptyMap)),
				new Action(new ReferenceAssignment(),
					new LiteralRule(Meta.Map.Empty)))));

		private static Rule LookupAnythingExpression =
			new Sequence(
				new Action(new Match(),new Character((Syntax.lookupStart))),
				new Action(new ReferenceAssignment(),Expression),
				new Action(new Match(),new ZeroOrMore(
					new Action(new Match(),new Character(Syntax.indentation)))),
				new Action(new Match(),new Character(Syntax.lookupEnd)));


		private static Rule NumberExpression =
			new Sequence(
				new Action(new Assignment(
					CodeKeys.Literal),
					Number));

		private static Rule LookupStringExpression =
			new Sequence(
				new Action(new Assignment(
					CodeKeys.Literal),
					LookupString));

		private static Rule Current = new Sequence(
			new Action(new Match(),StringRule(Syntax.current)),
			new Action(new ReferenceAssignment(),new LiteralRule(new StrategyMap(CodeKeys.Current, Meta.Map.Empty))));



		private static Rule Root = new Sequence(
			new Action(new Match(),new Character(Syntax.root)),
			new Action(new ReferenceAssignment(),new LiteralRule(new StrategyMap(CodeKeys.Root, Meta.Map.Empty))));


		private static Rule Lookup =
			new Alternatives(
				Current,
				new Sequence(
					new Action(new Assignment(
						CodeKeys.Lookup),
						new Alternatives(
							LookupStringExpression,
							LookupAnythingExpression))));


		private static Rule Search = new Sequence(
			new Action(new Assignment(
				CodeKeys.Search),
				new Alternatives(
					LookupStringExpression,
					LookupAnythingExpression)));


		private static Rule Select = new Sequence(
			new Action(new Assignment(
				CodeKeys.Select),
				new Sequence(
					new Action(new Assignment(
						1),
						new Alternatives(
							Root,
							Search,
							Lookup,
							ExplicitCall)),
					new Action(new Append(),
						new ZeroOrMore(
							new Action(new Autokey(),
								new Sequence(
									new Action(new Match(),new Character(Syntax.select)),
									new Action(new ReferenceAssignment(),
										Lookup))))))));


		private static Rule KeysSearch = new Sequence(
				new Action(new Match(),new Character(Syntax.search)),
				new Action(new ReferenceAssignment(),Search));


		private static Rule AutokeyLookup = new CustomRule(delegate(Parser p, out bool matched)
		{
			bool m;
			new Character(Syntax.autokey).Match(p, out m);
			if (m)
			{
				Map map = p.CreateMap(CodeKeys.Lookup, p.CreateMap(CodeKeys.Literal, p.defaultKeys.Peek()));
				//Map map = p.CreateMap(1, p.CreateMap(CodeKeys.Lookup, p.CreateMap(CodeKeys.Literal, p.defaultKeys.Peek())));

				p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
				matched = true;
				return map;
			}
			else
			{
				matched = false;
				return null;
			}
		});

		private static Rule Keys = new Sequence(
			new Action(new Assignment(
				1),
				new Alternatives(
					KeysSearch,
					Lookup,
					AutokeyLookup)),
			new Action(new Append(),
				new ZeroOrMore(
					new Action(new Autokey(),
						new Sequence(
							new Action(new Match(),new Character(Syntax.select)),
							new Action(new ReferenceAssignment(),
								Lookup))))));

		public static Rule Statement = new Sequence(
			new Action(new ReferenceAssignment(),
				new Alternatives(
					FunctionExpression,
					new Alternatives(
						new Sequence(
							new Action(new Assignment(
								CodeKeys.Key),
								Keys),
							new Action(new Match(),new Optional(new Character(Syntax.assignment))),
							new Action(new Assignment(
								CodeKeys.Value),
								Expression))//,
		//                new Sequence(
		//                    new Action(new Match(),new Optional(
		//                        new Character(Syntax.assignment))),
		//                    new Action(new Assignment(
		//                        CodeKeys.Value),
		//                        Expression),
		//                    new Action(new Assignment(
		//                        CodeKeys.Key),
		//                        new CustomRule(delegate(Parser p, out bool matched)
		//{
		//    Map map = p.CreateMap(1, p.CreateMap(CodeKeys.Lookup, p.CreateMap(CodeKeys.Literal, p.defaultKeys.Peek())));
		//    p.defaultKeys.Push(p.defaultKeys.Pop() + 1);
		//    matched = true;
		//    return map;
		//})))
			))),
			new Action(new Match(),new CustomRule(delegate(Parser p, out bool matched)
		{
			if (EndOfLine.Match(p, out matched) == null && p.Look() != Syntax.endOfFile)
			{
				p.index -= 1;
				if (EndOfLine.Match(p, out matched) == null)
				{
					p.index -= 1;
					if (EndOfLine.Match(p, out matched) == null)
					{
						p.index += 2;
						throw new SyntaxException("Expected newline.", p);
					}
					else
					{
						p.line--;
					}
				}
				else
				{
					p.line--;
				}
			}
			matched = true;
			return null;
		})));

		public static Rule Program = new Sequence(
			new Action(
				new Assignment(CodeKeys.Program),
				new PrePost(
					delegate(Parser p)
					{
						p.defaultKeys.Push(1);
					},
					new Sequence(
						new Action(
							new Match(),
							Indentation),
						new Action(
							new Assignment(1),
							Statement),
						new Action(
							new Append(),
							new ZeroOrMore(
								new Action(new Autokey(),
									new Sequence(
										new Action(new Match(), new Alternatives(
											SameIndentation,
											Dedentation)),
										new Action(new ReferenceAssignment(), Statement)))))),
					delegate(Parser p)
					{
						p.defaultKeys.Pop();
					})));
		public abstract class Production
		{
			public abstract void Execute(Parser parser, Map map, ref Map result);
		}
		public class Action
		{
			private Rule rule;
			private Production production;
			public Action(Production production,Rule rule)
			{
				this.rule = rule;
				this.production=production;
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
		public class Merge : Production
		{
			public override void Execute(Parser parser, Map map, ref Map result)
			{
				result = Library.Merge(new StrategyMap(1, result, 2, map));
			}
		}
		public class CustomAction : Production
		{
			private CustomActionDelegate action;
			public CustomAction(CustomActionDelegate action)
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
				Map result = MatchImplementation(parser, out matched);
				if (!matched)
				{
					parser.index = oldIndex;
					parser.line = oldLine;
					parser.column = oldColumn;
				}
				else
				{
					if (result != null)
					{
						result.Extent = new Extent(oldLine,oldColumn,parser.line,parser.column,parser.file);
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
				actions.Add(new Action(new Match(), new Character(c)));
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
					// refactor
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
		public class Optional : Rule
		{
			private Rule rule;
			public Optional(Rule rule)
			{
				this.rule = rule;
			}
			// somewhat unlogical, should be OptionalAssignment
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
			return new StrategyMap(maps);
		}
		public Map CreateMap()
		{
			return CreateMap(new EmptyStrategy());
		}
		public Map CreateMap(MapStrategy strategy)
		{
			return new StrategyMap(strategy);
		}
	}
	// refactor
	public class Serialize
	{
		private static Rule EmptyMap = new Literal(Syntax.emptyMap, new Set());
		private static Rule IntegerValue = new CustomSerialize(
			delegate(Map map)
			{
				return map.IsNumber;
			},
			delegate(Map map)
			{
				return map.GetNumber().ToString();
			});

		public static Rule TypeMap = new CustomSerialize(
			delegate(Map map) {return map is TypeMap;},
			delegate(Map map) {return "TypeMap: " + ((TypeMap)map).Type.ToString();});

		public static Rule ObjectMap = new CustomSerialize(
			delegate(Map map) { return map is ObjectMap; },
			delegate(Map map) { return "ObjectMap: " + ((ObjectMap)map).Object.ToString(); });

		public static Rule MapValue = new CustomRule(delegate(Map asdf, string indent, out bool ma)
		{
			string t;
			t = Syntax.unixNewLine.ToString();
			if (indent == null)
			{
				indent = "";
			}
			else
			{
				indent += Syntax.indentation;
			}
			t+=new Alternatives(
				TypeMap,
				ObjectMap,
				new CustomRule(delegate(Map map, string indentation, out bool matched)
				{
					string text = "";
					foreach (KeyValuePair<Map, Map> entry in map)
					{
						if (entry.Key.Equals(CodeKeys.Function) && entry.Value.Count == 1 && (entry.Value.ContainsKey(CodeKeys.Call) || entry.Value.ContainsKey(CodeKeys.Literal) || entry.Value.ContainsKey(CodeKeys.Program) || entry.Value.ContainsKey(CodeKeys.Select)))
						{
							bool m;
							text += indentation + Syntax.function + Expression.Match(entry.Value, indentation, out m);
							if (!text.EndsWith(Syntax.unixNewLine.ToString()))
							{
								text += Syntax.unixNewLine;
							}
						}
						else
						{
							text += indentation + Key.Match((Map)entry.Key, indentation, out matched) + Syntax.assignment + Value.Match((Map)entry.Value, (indentation), out matched);
							//text += indentation + Key.Match((Map)entry.Key, indentation, out matched) + Syntax.statement + Value.Match((Map)entry.Value, (indentation), out matched);
							if (!text.EndsWith(Syntax.unixNewLine.ToString()))
							{
								text += Syntax.unixNewLine;
							}
						}
					}
					matched = true;
					return text;
				})).Match(asdf, indent, out ma);
			ma = true;
			return t;
		});


		private static Rule StringValue = new CustomSerialize(
			delegate(Map map)
			{
				return map.IsString;
			},
			delegate(Map map)
			{
				string text = Syntax.@string.ToString();
				string mapString = map.GetString();
				if (mapString.IndexOf(Syntax.@string) == -1 && mapString.IndexOf(Syntax.unixNewLine) == -1)
				{
					text += mapString;
				}
				else
				{
					string[] lines = map.GetString().Split(new string[] { Syntax.unixNewLine.ToString(), Syntax.windowsNewLine }, StringSplitOptions.None);
					for (int i = 0; i < lines.Length; i++)
					{
						text += Syntax.unixNewLine.ToString() + Syntax.indentation + lines[i];
					}
					text += Syntax.unixNewLine.ToString();
				}
				text += Syntax.@string;
				return text;
			});
		private static Rule Value = new Alternatives(EmptyMap, StringValue, IntegerValue, MapValue);


		public static Rule LiteralKey = new OneOrMore(new CharacterExcept(Syntax.lookupStringForbidden));

		private static Rule StringKey = new Alternatives(
			LiteralKey,
			new Enclose(Syntax.lookupStart.ToString(), StringValue, Syntax.lookupEnd.ToString()));

		
		public static Rule Call = new Set(
			new KeyRule(
				CodeKeys.Call,
				new Set(
					new KeyRule(
						CodeKeys.Callable,
						Expression),
					new KeyRule(
						CodeKeys.Argument,
						new Alternatives(
							Program,
							new Enclose(
								Syntax.call.ToString(),
								new Alternatives(
									EmptyMap,
									Expression),
								""))))));

		public static string Statement(Map code, string indentation, ref int autoKeys)
		{
			Map key = code[CodeKeys.Key];
			string text;
			if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Lookup) && key[1][CodeKeys.Lookup].ContainsKey(CodeKeys.Literal) && key[1][CodeKeys.Lookup][CodeKeys.Literal].Equals(CodeKeys.Function) && code[CodeKeys.Value].ContainsKey(CodeKeys.Literal))
			{
				bool matched;
				text = indentation + Syntax.function + Expression.Match(code[CodeKeys.Value][CodeKeys.Literal], indentation,out matched);
			}
			else
			{
				Map autoKey;
				text = indentation;
				Map value = code[CodeKeys.Value];
				if (key.Count == 1 && key[1].ContainsKey(CodeKeys.Lookup) && key[1][CodeKeys.Lookup].ContainsKey(CodeKeys.Literal) && (autoKey = key[1][CodeKeys.Lookup][CodeKeys.Literal]) != null && autoKey.IsNumber && autoKey.GetNumber() == autoKeys + 1)
				{
					autoKeys++;
					if (value.ContainsKey(CodeKeys.Program) && value[CodeKeys.Program].Count != 0)
					{
						text += Syntax.assignment;
						//text += Syntax.statement;
					}
				}
				else
				{
					bool m;
					text += Keys.Match(code[CodeKeys.Key], indentation, out m) + Syntax.assignment;
					//text += Keys.Match(code[CodeKeys.Key], indentation, out m) + Syntax.statement;
				}
				bool matched;
				text += Expression.Match(value, indentation,out matched);
			}
			return text;
		}
		private static Rule SelectImplementation = new CustomRule(delegate(Map code, string indentation, out bool matched)
		{
			string text = Lookup.Match(code[1], indentation,out matched);
			for (int i = 2; code.ContainsKey(i); i++)
			{
				text += Syntax.select + Lookup.Match(code[i], indentation,out matched);
			}
			matched = true;
			return text;
		});
		public static Rule Keys = SelectImplementation;

		public static Rule Select = new Set(
			new KeyRule(
				CodeKeys.Select,
				SelectImplementation));

		public static Rule LookupSearchImplementation = new Alternatives(
			new KeyRule(
				CodeKeys.Literal,
				Key),
			new Enclose(
				Syntax.lookupStart.ToString(),
				new Alternatives(
					new Enclose(
						"",
						Program,
						new IndentationProduction()),
					Expression),
				Syntax.lookupEnd.ToString()));


		public static Rule Current = new Equal(
			new StrategyMap(
				CodeKeys.Current, 
				Map.Empty),
			Syntax.current.ToString());

		public static Rule LiteralProduction = new Set(new KeyRule(CodeKeys.Literal, Value));

		public static Rule Lookup = new Alternatives(
				new Alternatives(
					new Set(
						new KeyRule(
							CodeKeys.Search,
							LookupSearchImplementation)),
					new Set(
						new KeyRule(
							CodeKeys.Lookup,
							LookupSearchImplementation))),
				new Alternatives(
					Current,
					new Set(new KeyRule(CodeKeys.Literal, Key)),
					new Enclose(Syntax.lookupStart.ToString(), Expression, Syntax.lookupEnd.ToString())));


		public static Rule Key = new Alternatives(
			StringKey,
			new Enclose(
				Syntax.lookupStart.ToString(),
				new Alternatives(
					EmptyMap,
					IntegerValue,
					TypeMap,
					ObjectMap,
					MapValue),
			Syntax.lookupEnd.ToString()));

		public class OneOrMore:Rule
		{
			private Rule rule;
			public OneOrMore(Rule rule)
			{
				this.rule = rule;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				matched = false;
				string text = "";
				foreach (Map m in map.Array)
				{
					text += rule.Match(m,indentation, out matched);
					if (!matched)
					{
						break;
					}
				}
				return text;
			}
		}
		public class CharacterExcept : Rule
		{
			private char[] chars;
			public CharacterExcept(params char[] chars)
			{
				this.chars = chars;
			}
			public override string Match(Map map,string indentation,out bool matched)
			{
				string text = null;
				matched = false;
				if (Transform.IsIntegerInRange(map, char.MinValue, char.MaxValue))
				{
					char c = Convert.ToChar((int)map.GetNumber().Numerator);
					if (c.ToString().IndexOfAny(chars) == -1)
					{
						matched = true;
						text = c.ToString();
					}
				}
				return text;
			}
		}
		public delegate string CustomRuleDelegate(Map map,string indentation, out bool matched);
		public class CustomRule : Rule
		{
			private CustomRuleDelegate customRule;
			public CustomRule(CustomRuleDelegate customRule)
			{
				this.customRule = customRule;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				return customRule(map,indentation, out matched);
			}
		}
		public delegate Rule DelayedRuleDelegate();

		public class DelayedRule : Rule
		{
			private Rule rule;
			private DelayedRuleDelegate delayedRule;
			public DelayedRule(DelayedRuleDelegate delayedRule)
			{
				this.delayedRule = delayedRule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				if (rule == null)
				{
					rule = delayedRule();
				}
				return rule.Match(map, indentation, out matched);				
			}
		}
		public static Rule Expression = new DelayedRule(delegate()
		{
			return new Alternatives(Call,EmptyMap, Program, LiteralProduction,Select);
		});

		public class KeyRule : Rule
		{
			public Map key;
			Rule value;
			public KeyRule(Map key, Rule value)
			{
				this.key = key;
				this.value = value;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				string text;
				if (map.ContainsKey(key))
				{
					text = value.Match(map[key],indentation, out matched);
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}
		public class Set:Rule
		{
			private Rule[] rules;
			public Set(params Rule[] rules)
			{
				this.rules = rules;
			}
			public override string Match(Map map,string indentation, out bool matched)
			{
				string text = "";
				int keyCount = 0;
				matched = true;
				foreach (Rule rule in rules)
				{
					text += rule.Match(map,indentation, out matched);
					if (rule is KeyRule)
					{
						keyCount++;
					}
					if (!matched)
					{
						break;
					}
				}
				matched = map.Count == keyCount && matched;
				return text;
			}
		}
		public static Rule Program = new CustomRule(delegate(Map code, string indentation, out bool matched)
		{
			string text;			
			if (!code.ContainsKey(CodeKeys.Program))
			{
			    matched = false;
				text = null;
			}
			else
			{
				code = code[CodeKeys.Program];
				text = Syntax.unixNewLine.ToString();
				int autoKeys = 0;
				foreach (Map statement in code.Array)
				{
					text += Statement(statement, indentation + Syntax.indentation, ref autoKeys);
					if (!text.EndsWith(Syntax.unixNewLine.ToString()))
					{
						text += Syntax.unixNewLine;
					}
				}
				matched = true;
			}
			return text;
		});
		public abstract class Production
		{
			public static implicit operator Production(string text)
			{
				return new StringProduction(text);
			}
			public abstract string Get(string indentation);
		}
		public class StringProduction : Production
		{
			private string text;
			public StringProduction(string text)
			{
				this.text = text;
			}
			public override string Get(string indentation)
			{
				return text;
			}
		}
		public class IndentationProduction:Production
		{
			public override string  Get(string indentation)
			{
				return indentation;
			}
		}

		public class Enclose : Rule
		{
			private Production start;
			private Rule rule;
			private Production end;
			public Enclose(Production start, Rule rule, Production end)
			{
				this.start = start;
				this.rule = rule;
				this.end = end;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				string text = rule.Match(map, indentation, out matched);
				if (matched)
				{
					text = start.Get(indentation) + text + end.Get(indentation);
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}
		// combine with Literal?
		public class Equal : Rule
		{
			private Map map;
			private string literal;
			public Equal(Map map, string literal)
			{
				this.map = map;
				this.literal = literal;
			}
			public override string Match(Map m,string indentation, out bool matched)
			{
				string text;
				if (m.Equals(map))
				{
					text = literal;
					matched = true;
				}
				else
				{
					text = null;
					matched = false;
				}
				return text;
			}
		}

		public abstract class Rule
		{
			public abstract string Match(Map map, string indentation, out bool matched);
		}
		public static string ValueFunction(Map val)
		{
			bool matched;
			return Value.Match(val, null, out matched);
		}
		public class Literal : Rule
		{
			private Rule rule;
			private string literal;
			public Literal(char c, Rule rule)
				: this(c.ToString(), rule)
			{
			}
			public Literal(string literal, Rule rule)
			{
				this.literal = literal;
				this.rule = rule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				rule.Match(map, indentation, out matched);
				string text;
				if (matched)
				{
					text = literal;
				}
				else
				{
					text = null;
				}
				return text;
			}
		}
		public class Alternatives : Rule
		{
			private Rule[] rules;
			public Alternatives(params Rule[] rules)
			{
				this.rules = rules;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				string text = null;
				matched = false;
				foreach (Rule rule in rules)
				{
					text = rule.Match(map, indentation, out matched);
					if (matched)
					{
						break;
					}
				}
				return text;
			}
		}
		public delegate bool Matches(Map map);
		public delegate string GetMatch(Map map);
		public class CustomSerialize : Rule
		{
			private Matches matches;
			private GetMatch getMatch;
			public CustomSerialize(Matches matches, GetMatch getMatch)
			{
				this.matches = matches;
				this.getMatch = getMatch;
			}
			private Rule rule;
			public CustomSerialize(Matches matches, Rule rule)
			{
				this.matches = matches;
				this.rule = rule;
			}
			public override string Match(Map map, string indentation, out bool matched)
			{
				matched = matches(map);
				string text;
				if (matched)
				{
					string unIndented;
					if (getMatch != null)
					{
						unIndented = getMatch(map);
					}
					else
					{
						unIndented = rule.Match(map, indentation, out matched);
					}
					string[] lines = unIndented.Split(new string[] { Syntax.unixNewLine.ToString(), Syntax.windowsNewLine }, StringSplitOptions.None);
					text = lines[0];
					for (int i = 1; i < lines.Length; i++)
					{
						text += Syntax.unixNewLine.ToString() + indentation + lines[i];
					}
				}
				else
				{
					text = null;
				}
				return text;
			}
		}
	}
	public class Gac : Map
	{
		public static readonly Map gac = new Gac();
		static Gac()
		{
			object x = FileSystem.fileSystem;
		}
		private Gac()
		{
			this["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
		}
		private Dictionary<Map, Map> cache=new Dictionary<Map,Map>();
		public static Map LoadAssembly(Assembly assembly)
		{
			Map val = new StrategyMap();
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
					selected[type.Name] = new TypeMap(type);
				}
			}
			return val;
		}
		protected override Map Get(Map key)
		{
			Map value;
			if (!cache.ContainsKey(key))
			{
				if (key.IsString)
				{
					try
					{
						value=LoadAssembly(Assembly.LoadWithPartialName(key.GetString()));
						this[key] = value;
					}
					catch
					{
						value=null;
					}
				}
				else
				{
					Map version = key["version"];
					Map publicKeyToken = key["publicKeyToken"];
					Map culture = key["culture"];
					Map name = key["name"];
					if (version != null && version.IsString && publicKeyToken != null && publicKeyToken.IsString && culture != null && culture.IsString && name != null && name.IsString)
					{
						Assembly assembly = Assembly.Load(name.GetString() + ",Version=" + version.GetString() + ",Culture=" + culture.GetString() + ",Name=" + name.GetString());
						value=LoadAssembly(assembly);
						this[key] = value;
					}
					else
					{
						value=null;
					}
				}
			}
			else
			{
				value = cache[key];
			}
			return value;
		}
		protected override void Set(Map key, Map val)
		{
			cache[key] = val;
			//throw new Exception("The method or operation is not implemented.");
		}
		protected override Map CopyData()
		{
			return this;
		}
		protected override ICollection<Map> KeysImplementation
		{
			get
			{
				throw new ApplicationException("not implemented.");
			}
		}
		protected override bool ContainsKeyImplementation(Map key)
		{
			return Get(key) != null;
		}
		//protected Map cachedAssemblyInfo = new StrategyMap();
	}
	//public class Gac : StrategyMap
	//{
	//    static Gac()
	//    {
	//        object x = FileSystem.fileSystem;
	//    }
	//    public static readonly StrategyMap gac = new Gac();
	//    private Gac()
	//    {
	//        this["Meta"] = LoadAssembly(Assembly.GetExecutingAssembly());
	//    }
	//    private bool Load(Map key)
	//    {
	//        bool loaded;
	//        if (strategy.ContainsKey(key))
	//        {
	//            loaded = true;
	//        }
	//        else
	//        {
	//            if (key.IsString)
	//            {
	//                string assemblyName = key.GetString();
	//                Assembly assembly = null;
	//                try
	//                {
	//                    assembly = Assembly.LoadWithPartialName(assemblyName);
	//                }
	//                catch
	//                {
	//                }
	//                if (assembly != null)
	//                {
	//                    this[key] = LoadAssembly(assembly);
	//                    loaded = true;
	//                }
	//                else
	//                {
	//                    loaded = false;
	//                }
	//            }
	//            else
	//            {
	//                Map version = key["version"];
	//                Map publicKeyToken = key["publicKeyToken"];
	//                Map culture = key["culture"];
	//                Map name = key["name"];
	//                if (version != null && version.IsString && publicKeyToken != null && publicKeyToken.IsString && culture != null && culture.IsString && name != null && name.IsString)
	//                {
	//                    Assembly assembly = Assembly.Load(name.GetString() + ",Version=" + version.GetString() + ",Culture=" + culture.GetString() + ",Name=" + name.GetString());
	//                    this[key] = LoadAssembly(assembly);
	//                    loaded = true;
	//                }
	//                else
	//                {
	//                    loaded = false;
	//                }
	//            }
	//        }
	//        return loaded;
	//    }
	//    public static Map LoadAssembly(Assembly assembly)
	//    {
	//        Map val = new StrategyMap();
	//        foreach (Type type in assembly.GetExportedTypes())
	//        {
	//            if (type.DeclaringType == null)
	//            {
	//                Map selected = val;
	//                string name;
	//                if (type.IsGenericTypeDefinition)
	//                {
	//                    name = type.Name.Split('`')[0];
	//                }
	//                else
	//                {
	//                    name = type.Name;
	//                }
	//                selected[type.Name] = new TypeMap(type);
	//            }
	//        }
	//        return val;
	//    }
	//    protected override Map Get(Map key)
	//    {
	//        Map val;
	//        if ((key.IsString && strategy.ContainsKey(key)) || Load(key))
	//        {
	//            val = strategy.Get(key);
	//        }
	//        else
	//        {
	//            val = Web.Get(key);
	//        }
	//        return val;
	//    }
	//    protected override ICollection<Map> KeysImplementation
	//    {
	//        get
	//        {
	//            throw new ApplicationException("not implemented.");
	//        }
	//    }
	//    public override bool ContainsKey(Map key)
	//    {
	//        return Get(key) != null;
	//    }
	//    //protected Map cachedAssemblyInfo = new StrategyMap();
	//}
	public class FileSystem
	{
		// combine gac into fileSystem

		public static DirectoryMap fileSystem;
		// this stuff should be done somehwere else!!
		static FileSystem()
		{
			fileSystem = new DirectoryMap(new DirectoryInfo(Interpreter.LibraryPath));
			DrivesMap drives = new DrivesMap();
			FileSystem.fileSystem.cache["localhost"] = drives;
			Gac.gac["filesystem"] = FileSystem.fileSystem;
		}
	}
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

		public Number(Number i):this(i.numerator,i.denominator)
		{
		}
		public Number(Map map):this(map.GetNumber())
		{
		}
		public Number(int integer)
			: this((double)integer)
		{
		}
		public Number(long integer)
			: this((double)integer)
		{
		}
		public Number(double integer):this(integer,1)
		{
		}
		public Number(ulong integer):this((double)integer)
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
			this.numerator=numerator/greatestCommonDivisor;
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
			return new Number(integer);
		}
		public static bool operator ==(Number a, Number b)
		{
			return !ReferenceEquals(b, null) && a.numerator == b.numerator && a.denominator==b.denominator;
		}
		public static bool operator !=(Number a, Number b)
		{
			return !(a == b);
		}
		private static double GreatestCommonDivisor(double a, double b)
		{
			a = Math.Abs(a);
			b = Math.Abs(b);
		   while (a!=0 && b!=0) {
				   if(a > b)
						   a = a % b;
				   else
						   b = b % a;
		   }
		   if(a == 0)
				   return b;
		   else
				   return a;
		}
		private static double LeastCommonMultiple(Number a, Number b)
		{
			return a.denominator * b.denominator / GreatestCommonDivisor(a.denominator, b.denominator);
		}
		public static Number operator +(Number a, Number b)
		{
			return new Number(a.Expand(b) + b.Expand(a), LeastCommonMultiple(a,b));
		}

		public static Number operator /(Number a, Number b)
		{
			return new Number(a.numerator*b.denominator,a.denominator*b.numerator);
		}
		public static Number operator -(Number a, Number b)
		{
			return new Number(a.Expand(b) - b.Expand(a), LeastCommonMultiple(a,b));
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
			return b.numerator==numerator && b.denominator==denominator;
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
			// refactor, incorrect
			return Convert.ToInt32(numerator);
		}
		public long GetInt64()
		{
			// refactor, incorrect
			return Convert.ToInt64(numerator);
		}
	}
	// experimential
	public class Web
	{
		const int port = 80;
		public static Map Get(Map key)
		{
			if (!key.IsString)
			{
				return null;
			}
			string address = key.GetString();

			WebClient webClient = new WebClient();

			string metaPath = Path.Combine(new DirectoryInfo(Application.UserAppDataPath).Parent.Parent.Parent.FullName, "Meta");
			string cacheDirectory = Path.Combine(metaPath, "Cache");
			DirectoryInfo unzipDirectory = new DirectoryInfo(Path.Combine(cacheDirectory, address));
			string zipDirectory = Path.Combine(metaPath, "Zip");
			string zipFile = Path.Combine(zipDirectory, address + ".zip");
			Directory.CreateDirectory(zipDirectory);
			string metaZipAddress = "http://" + address + "/" + "meta.zip";

			try
			{
				webClient.DownloadFile(metaZipAddress, zipFile);
			}
			catch (Exception e)
			{
				return null;
			}
			unzipDirectory.Create();
			Unzip(zipFile, unzipDirectory.FullName);
			// net should be parent
			return new DirectoryMap(unzipDirectory);
		}
		public static void Unzip(string zipFile, string dir)
		{
			ZipInputStream s = new ZipInputStream(File.OpenRead(zipFile));

			ZipEntry theEntry;
			while ((theEntry = s.GetNextEntry()) != null)
			{

				Console.WriteLine(theEntry.Name);

				string directoryName = Path.GetDirectoryName(theEntry.Name);
				string fileName = Path.GetFileName(theEntry.Name);

				// fix this for directory creation

				//Directory.CreateDirectory(directoryName);

				if (fileName != String.Empty)
				{
					FileStream streamWriter = File.Create(Path.Combine(dir, theEntry.Name));

					int size = 2048;
					byte[] data = new byte[2048];
					while (true)
					{
						size = s.Read(data, 0, data.Length);
						if (size > 0)
						{
							streamWriter.Write(data, 0, size);
						}
						else
						{
							break;
						}
					}

					streamWriter.Close();
				}
			}
			s.Close();
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
			protected override string TestDirectory
			{
				get
				{
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			public static string TestPath
			{
				get
				{
					return Path.Combine(Interpreter.InstallationPath, "Test");
				}
			}
			private static string BasicTest
			{
				get
				{
					return Path.Combine(TestPath, "basicTest.meta");
				}
			}
			private static string LibraryTest
			{
				get
				{
					return Path.Combine(TestPath, "libraryTest.meta");
				}
			}
			public class Library : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(@"C:\Meta\0.2\Test\libraryTest.meta", new StrategyMap(1, "first arg", 2, "second=arg"));
				}
			}
			public class Profile : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(@"C:\Meta\0.2\Test\profile.meta", Map.Empty);//.GetString();
					//return Run(@"C:\Meta\0.2\Test\profile.meta", Map.Empty).GetString();
				}
			}
			public class Extents : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.2"]["Test"]["basicTest"];
				}
			}
			public class Basic : Test
			{
				public override object GetResult(out int level)
				{
					level = 2;
					return Run(@"C:\Meta\0.2\Test\basicTest.meta", new StrategyMap(1, "first arg", 2, "second=arg"));
				}
			}



			//public class Library : Test
			//{
			//    public override object GetResult(out int level)
			//    {
			//        level = 2;
			//        return Run(@"C:\Meta\0.2\Test\libraryTest.meta", new StrategyMap(1, "first arg", 2, "second=arg"));
			//    }
			//}
			public class Serialization : Test
			{
				public override object GetResult(out int level)
				{
					level = 1;
					return Meta.Serialize.ValueFunction(FileSystem.fileSystem["localhost"]["C:"]["Meta"]["0.2"]["Test"]["basicTest"]);
				}
			}
			public static Map Run(string path,Map argument)
			{
				List<string> list=new List<string>();
				FileInfo file=new FileInfo(path);
				list.Add(Path.GetFileNameWithoutExtension(file.FullName));
				DirectoryInfo directory=file.Directory;
				while(true)
				{
					list.Add(directory.Name.Trim('\\'));
					directory=directory.Parent;
					if(directory==null)
					{
						break;
					}
				}
				list.Add("localhost");
				list.Add("filesystem");
				list.Reverse();
				Map lookups=new StrategyMap();
				foreach(Map entry in list)
				{
					lookups.Append(new StrategyMap(
					    CodeKeys.Lookup, new StrategyMap(
				        CodeKeys.Literal, entry)));
				}
				Map code = new StrategyMap(
					CodeKeys.Call, new StrategyMap(
						CodeKeys.Callable, new StrategyMap(
							CodeKeys.Select, lookups),
						CodeKeys.Argument, new StrategyMap(
							CodeKeys.Literal, argument)));
				PersistantPosition position = code.GetExpression().Evaluate(RootPosition.rootPosition);
				//PersistantPosition position = code.GetExpression().Evaluate(new PersistantPosition(new List<Map>(new Map[] { })));
				return position.Get();

			}

		}
		namespace TestClasses
		{
			[Serializable]
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
			[Serializable]
			public class TestClass
			{
				public class NestedClass// TODO: rename, only used for testing purposes
				{
					public static int field = 0;
				}
				public TestClass()
				{
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
			[Serializable]
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
					Map def = new StrategyMap();
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
				// refactor, remove
				public string Concatenate(Map arg)
				{
					Map def = new StrategyMap();
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
			[Serializable]
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
}