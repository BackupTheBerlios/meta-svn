//	An implementation of the Meta programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This library is free software; you can redistribute it and/or
//	modify it under the terms of the GNU Lesser General Public
//	License as published by the Free Software Foundation; either
//	version 2.1 of the License, or (at your option) any later version.
//
//	This library is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//	Lesser General Public License for more details.
//
//	You should have received a copy of the GNU Lesser General Public
//	License along with this library; if not, write to the Free Software
//	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// TODO: rename variables, methods and classes

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using antlr;
using antlr.collections;
using Meta.TestingFramework;
using Meta.Parser;
using Meta.Types;
using Meta.Execution;
using Meta.Library;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Xml;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.GAC;

namespace Meta {
	namespace Library {
		public class Functions{
//			public static IKeyValue Merge() { // get rid of use of Map
//				return (Map)Interpreter.Merge((Map)Interpreter.Arg);
//			}
//			public static void Write(string s) {
//				Console.WriteLine(s);
//			}
//			public static string Read() {
//				return Console.ReadLine();
//			}
//			public static bool And(bool a,bool b) {
//				return a && b;
//			}
//			public static bool Or(bool a,bool b) {
//				return a || b;
//			}
//			public static bool Not(bool a) {
//				return !a;
//			}
//			public static Integer Add(Integer x,Integer y) {
//				return x+y;
//			}
//			public static Integer Subtract(Integer x,Integer y) {
//				return x-y;		
//			}
//			public static Integer Multiply(Integer x,Integer y) {
//				return x*y;
//			}
//			public static Integer Divide(Integer x,Integer y) {
//				return x/y;
//			}
//			public static bool Smaller(Integer x,Integer y) {
//				return x<y;
//			}
//			public static bool Greater(Integer x,Integer y) {
//				return x>y;
//			}
//			public static bool Equal(object a,object b) {
//				return a.Equals(b);
//			}
//			public static Enum BinaryOr(params Enum[] enums) {
//				int val=(int)Enum.Parse(enums[0].GetType(),enums[0].ToString());
//				for(int i=1;i<enums.Length;i++) {
//					int newVal=(int)Enum.Parse(enums[i].GetType(),enums[i].ToString());
//					val|=newVal;
//				}
//				return (Enum)Enum.ToObject(enums[0].GetType(),val);
//			}
//			public static Map For() {
//				Map arg=((Map)Interpreter.Arg);
//				int times=(int)((Integer)arg[new Integer(1)]).IntValue();
//				Map function=(Map)arg[new Integer(2)];
//				Map result=new Map();
//				for(int i=0;i<times;i++) {
//					Map argument=new Map();
//					argument["i"]=new Integer(i);
//					Interpreter.arguments.Add(argument);
//					result[new Integer(i+1)]=((IExpression)function.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//					Interpreter.arguments.Remove(argument);
//				}
//				return result;
//			}
//			public static void Load() {
//				Map caller=(Map)Interpreter.callers[Interpreter.callers.Count-1];
//				foreach(DictionaryEntry entry in Interpreter.LoadAssembly((Map)Interpreter.Arg,true)) {
//					caller[entry.Key]=entry.Value;
//				}
//			}
//			public static Map Each() {
//				Map arg=((Map)Interpreter.Arg);
//				Map over=(Map)arg[new Integer(1)];
//				Map function=(Map)arg[new Integer(2)];
//				Map result=new Map();
//				int i=0;
//				foreach(DictionaryEntry entry in over) {
//					Map argument=new Map();
//					argument["key"]=entry.Key;
//					argument["value"]=entry.Value;
//					Interpreter.arguments.Add(argument);
//					result[new Integer(i+1)]=((IExpression)function.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//					Interpreter.arguments.Remove(argument);
//					i++;
//				}
//				return result;
//			}
//			public static bool IsMap(object o) {
//				return o is Map;
//			}
//			public static void Switch() {
//				Map arg=((Map)Interpreter.Arg);
//				object val=arg[new Integer(1)];
//				Map cases=(Map)arg["case"];
//				Map def=(Map)arg["default"];
//				if(cases.ContainsKey(val)) {
//					((IExpression)((Map)cases[val]).Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//				}
//				else if(def!=null) {
//					((IExpression)def.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//				}				
//			}
//			public static void If() {
//				Map arg=((Map)Interpreter.Arg);
//				bool test=(bool)arg[new Integer(1)];
//				Map then=(Map)arg["then"];
//				Map _else=(Map)arg["else"];
//				if(test) {
//					if(then!=null) {
//						((IExpression)then.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//					}
//				}
//				else {
//					if(_else!=null) {
//						((IExpression)_else.Compile()).Evaluate(Interpreter.callers[Interpreter.callers.Count-1]);
//					}
//				}	
//			}
		}
		public class LiteralRecognitions {
			// order of classes is important here !
			public class StringRecognition: ILiteralRecognition  {
				public object Recognize(string text)  {
					return text;
				}
			}
			public class IntegerRecognition: ILiteralRecognition  {
				public object Recognize(string text)  {
					Integer number=new Integer(0);
					bool negative=false;
					foreach(char c in text) {
						if(c=='-') {
							negative=true;
						}
						else if(!char.IsDigit(c)) {
							number=null;
							break;
						}
						else {
							number=number*10+(c-'0');
						}
					}
					if(negative) {
						number=-number;
					}
					return number;
				}
			}
			public class SymbolRecognition:ILiteralRecognition {
				public object Recognize(string text) {
					object recognized=null;
					if(text.StartsWith("\"")&&text.EndsWith("\"")) {
						recognized=Interpreter.String(text.Substring(1,text.Length-2));
					}
					return recognized;
				}
			}
			public class BooleanRecognition:ILiteralRecognition {
				public object Recognize(string text) {
					switch(text) {
						case "true":
							return true;
						case "false":
							return false;
					}
					return null;
				}
			}
		}
		// automatic conversions that occur when a .NET method is called, or when 
		// a .NET property, indexer, or field is assigned to
		public abstract class ToNetConversions  { // these haven't all been tested
			public class IntegerToByte: ToNetConversion   {
				public IntegerToByte()   {
					this.source=typeof(Integer);
					this.target=typeof(Byte);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToSByte: ToNetConversion   {
				public IntegerToSByte()   {
					this.source=typeof(Integer);
					this.target=typeof(SByte);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToChar: ToNetConversion   {
				public IntegerToChar()   {
					this.source=typeof(Integer);
					this.target=typeof(Char);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToChar(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt32: ToNetConversion   {
				public IntegerToInt32()   {
					this.source=typeof(Integer);
					this.target=typeof(Int32);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToInt32(((Integer)obj).LongValue());
				}
			}
			public class IntegerToUInt32: ToNetConversion   {
				public IntegerToUInt32()   {
					this.source=typeof(Integer);
					this.target=typeof(UInt32);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToUInt32(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt64: ToNetConversion   {
				public IntegerToInt64()   {
					this.source=typeof(Integer);
					this.target=typeof(Int64);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToInt64(((Integer)obj).LongValue());
				}
			}
			public class IntegerToUInt64: ToNetConversion   {
				public IntegerToUInt64()   {
					this.source=typeof(Integer);
					this.target=typeof(UInt64);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToUInt64(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt16: ToNetConversion   {
				public IntegerToInt16()   {
					this.source=typeof(Integer);
					this.target=typeof(Int16);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToInt16(((Integer)obj).LongValue());
				}
			}
			public class IntegerToUInt16: ToNetConversion   {
				public IntegerToUInt16()   {
					this.source=typeof(Integer);
					this.target=typeof(UInt16);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToUInt16(((Integer)obj).LongValue());
				}
			}
			public class MapToString: ToNetConversion   {
				public MapToString()  {
					this.source=typeof(Map);
					this.target=typeof(string);
				}
				public override object Convert(object obj)   {
					return Interpreter.String((Map)obj);
				}
			}
		}
		// automatic conversions that occur when a .NET method,
		// property, indexer, or field returns a value
		public abstract class ToMetaConversions { // these haven't all been tested
			public class StringToMap: ToMetaConversion   {
				public StringToMap()   {
					this.source=typeof(string);
				}
				public override object Convert(object obj)   {
					return Interpreter.String((string)obj);
				}
			}
			public class ByteToInteger: ToMetaConversion   {
				public ByteToInteger()   {
					this.source=typeof(Byte);
				}
				public override object Convert(object obj)   {
					return new Integer((Byte)obj);
				}
			}
			public class SByteToInteger: ToMetaConversion   {
				public SByteToInteger()   {
					this.source=typeof(SByte);
				}
				public override object Convert(object obj)   {
					return new Integer((SByte)obj);
				}
			}
			public class CharToInteger: ToMetaConversion   {
				public CharToInteger()   {
					this.source=typeof(Char);
				}
				public override object Convert(object obj)   {
					return new Integer((Char)obj);
				}
			}
			public class Int32ToInteger: ToMetaConversion   {
				public Int32ToInteger()   {
					this.source=typeof(Int32);
				}
				public override object Convert(object obj)   {
					return new Integer((Int32)obj);
				}
			}
			public class UInt32ToInteger: ToMetaConversion   {
				public UInt32ToInteger()   {
					this.source=typeof(UInt32);
				}
				public override object Convert(object obj)   {
					return new Integer((UInt32)obj);
				}
			}
			public class Int64ToInteger: ToMetaConversion   {
				public Int64ToInteger()   {
					this.source=typeof(Int64);
				}
				public override object Convert(object obj)   {
					return new Integer((Int64)obj);
				}
			}
			public class UInt64ToInteger: ToMetaConversion   {
				public UInt64ToInteger()   {
					this.source=typeof(UInt64);
				}
				public override object Convert(object obj)   {
					return new Integer((Int64)(UInt64)obj);
				}
			}
			public class Int16ToInteger: ToMetaConversion   {
				public Int16ToInteger()   {
					this.source=typeof(Int16);
				}
				public override object Convert(object obj)   {
					return new Integer((Int16)obj);
				}
			}
			public class UInt16ToInteger: ToMetaConversion   {
				public UInt16ToInteger()   {
					this.source=typeof(UInt16);
				}
				public override object Convert(object obj)   {
					return new Integer((UInt16)obj);
				}
			}
		}
	}
	namespace Execution {
		public class Statement {
			public Select key;
			public IExpression val;
			public void Replace(Statement statement) { // check
				key=(Select)Interpreter.Replace(key,statement.key);
				val=(IExpression)Interpreter.Replace(val,statement.val);
			}
			public override bool Equals(object obj) { // check
				if(obj is Statement) {
					if(((Statement)obj).key.Equals(key)) {
						if(((Statement)obj).val.Equals(val)) {
							return true;
						}
					}
				}
				return false;
			}
			public void Realize(ref object scope,bool isInFunction) {
				key.Assign(ref scope,this.val.Evaluate(scope),isInFunction);
			}
			public Statement(Map obj) {
				this.key=(Select)((Map)obj["key"]).Compile();
				this.val=(IExpression)((Map)obj["value"]).Compile();
			}
		}
		public interface IExpression {
			object Evaluate(object current);
			void Replace(IExpression replace);
		}
		public class Call: IExpression {	
			public IExpression argument;
			public IExpression callable;
			public void Replace(IExpression replace) { // check
				if(!this.Equals(replace)) {
					Call call=(Call)replace;
					callable=(IExpression)Interpreter.Replace(callable,call.callable);
					argument=(IExpression)Interpreter.Replace(argument,call.argument);
				}
			}
			public override bool Equals(object obj) { // check
				return obj is Call && ((Call)obj).callable.Equals(callable) &&
					((Call)obj).argument.Equals(argument);
			}
			public Call(Map obj) {
				Map expression=(Map)obj["call"];
				this.callable=(IExpression)((Map)expression["function"]).Compile();
				this.argument=(IExpression)((Map)expression["argument"]).Compile();
			}
			public object Evaluate(object current) {
				object arg=argument.Evaluate(current);
				ICallable toBeCalled=(ICallable)callable.Evaluate(current);
				Interpreter.arguments.Add(arg);
				object result=toBeCalled.Call((Map)current);
				Interpreter.arguments.Remove(arg);
				return result;
			}
		}
		public class Delayed: IExpression {
			public Map delayedExpression;
			public override bool Equals(object o) { // check
				return o is Delayed &&
					delayedExpression.Equals(((Delayed)o).delayedExpression);
			}
			public void Replace(IExpression replace) { // check
				if(!this.Equals(replace)) {
					Delayed delayed=(Delayed)replace;
					delayed.delayedExpression.Compile();
					if(delayedExpression.compiled!=null) {
						delayedExpression.compiled=Interpreter.Replace(delayedExpression.compiled,delayed.delayedExpression.compiled);
						delayed.delayedExpression.compiled=delayedExpression.compiled;
					}
					delayedExpression=delayed.delayedExpression;
				}
			}
			public Delayed(Map code) {
				this.delayedExpression=(Map)code["delayed"];
				this.delayedExpression.Compile();
			}
			public object Evaluate(object current) {
				return delayedExpression;
			}
		}
		public class Program: IExpression {
			public readonly ArrayList statements=new ArrayList();

			public override bool Equals(object obj) { // check
				if(obj is Program) {
					Program program=(Program)obj;
					if(program.statements.Count==statements.Count) {
						for(int i=0;i<statements.Count;i++) {
							if(!statements[i].Equals(program.statements[i])) {
								return false;
							}
						}
						return true;
					}
				}
				return false;
			}
			public void Replace(IExpression replace) { // check
				if(!Equals(replace)) {
					Program program=(Program)replace;
					int i=0;
					for(;i<program.statements.Count && i<statements.Count;i++)  {
						if(!((Statement)statements[i]).Equals((Statement)program.statements[i])) {
							statements[i]=Interpreter.Replace(statements[i],
								(Statement)program.statements[i]);
						}
					}
					if(statements.Count>program.statements.Count) {
						statements.RemoveRange(i,statements.Count-i);
					}
					else {
						statements.AddRange(program.statements.GetRange(i,program.statements.Count-i));
					}
				}
			}
			public Program(Map programCode) {
				foreach(Map statementCode in ((Map)programCode["program"]).IntKeyValues) {
					this.statements.Add(statementCode.Compile());
				}
			}
			public object Evaluate(object parent) { // refactor
				Map local=new Map();
				local.Parent=(IKeyValue)parent;
				return Evaluate(parent,local,false);
			}
			public object Evaluate(object caller,Map existing,bool isInFunction) { // refactor
				object result=existing;
				bool callerIsMap=false;
				if(caller!=null) {
					if(caller is Map) {
						callerIsMap=true;
					}
					if(callerIsMap) {
						Interpreter.callers.Add(caller);
					}
				}
				for(int i=0;i<statements.Count;i++) {
				Back:
					try {
						((Statement)statements[i]).Realize(ref result,isInFunction);
					}
					catch (RestartStatementException e) {
						goto Back;
					}
				}
				if(caller!=null) {
					if(callerIsMap) {
						Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
					}
				}
				return result;
			}
		}
		public abstract class ToNetConversion { //rename
			public Type source;
			public Type target;
			public abstract object Convert(object obj);
		}
		public abstract class ToMetaConversion { //rename
			public Type source;
			public abstract object Convert(object obj);
		}
		public interface ILiteralRecognition {
			object Recognize(string text);
		}
		public class Literal: IExpression {
			public string text;
			public object cached=null;
			public override bool Equals(object obj) { // check
				return obj is Literal && text==((Literal)obj).text;
			}
			public void Replace(IExpression replace) { // check
				Literal literal=(Literal)replace;
				text=literal.text;
				cached=literal.cached;
			}
			public Literal(Map obj) {
				this.text=(string)obj["literal"];
				
			}
			public object Evaluate(object current) {
				if(cached==null) {
					Interpreter.cashedLiterals.Add(this);
					for(int i=Interpreter.Interceptions.Length-1;i>=0;i--) {
						cached=((ILiteralRecognition)Interpreter.Interceptions[i]).Recognize(text);
						if(cached!=null) {
							break;
						}
					}
				}
				return cached;
			}
		}
		public class Select: IExpression { 
			public readonly ArrayList expressions=new ArrayList();
			public ArrayList parents=new ArrayList();
			public override bool Equals(object obj) { // check
				if(obj is Select) {
					Select select=(Select)obj;
					if(expressions.Count==select.expressions.Count)  {
						for(int i=0;i<expressions.Count;i++) {
							if(!((IExpression)expressions[i]).Equals(select.expressions[i])) {
								return false;
							}
						}
						return true;
					}
				}
				return false;
			}
			public void Replace(IExpression replace) { // check
				if(!Equals(replace)) {
					Select select=(Select)replace;
					int i=0;
					for(;i<expressions.Count && i<select.expressions.Count;i++) {
						expressions[i]=Interpreter.Replace(
							expressions[i],select.expressions[i]);
					}
					if(expressions.Count>select.expressions.Count) {
						expressions.RemoveRange(i,expressions.Count-i);
					}
					else {
						expressions.AddRange(select.expressions.GetRange(i,select.expressions.Count-i));
					}
				}
			}
			public Select(Map code) {
				foreach(Map expression in ((Map)code["select"]).IntKeyValues) {
					this.expressions.Add(expression.Compile());
				}
			}
			public object Evaluate(object current) {
				ArrayList keys=new ArrayList();
				foreach(IExpression expression in expressions) {
					keys.Add(expression.Evaluate(current));
				}
				object preselection=Preselect(current,keys,true,true);
				return preselection;
			}
			public object Preselect(object current,ArrayList keys,bool isRightSide,bool isSelectLastKey) {
				object selected=current;
				int i=0;
				if(keys[0].Equals("System")) {
					int asdf=0;
				}
				if(keys[0].Equals("this")) {
					i++;
				}
				else if(keys[0].Equals("caller")) {
					int numCallers=0;
					foreach(object key in keys) {
						if(key.Equals("caller")) {
							numCallers++;
							i++;
						}
						else {
							break;
						}
					}
					selected=Interpreter.callers[Interpreter.callers.Count-numCallers];
				}
				else if(keys[0].Equals("parent")) {
					foreach(object key in keys) {
						if(key.Equals("parent")) {
							selected=((IKeyValue)selected).Parent;
							i++;
						}
						else {
							break;
						}
					}
				}
				else if(keys[0].Equals("arg")) {
					selected=Interpreter.arguments[Interpreter.arguments.Count-1];
					i++;
				}				
				else if(keys[0].Equals("search")||isRightSide) {
					if(keys[0].Equals("search")) {
						i++;
					}
					while(selected!=null && !((IKeyValue)selected).ContainsKey(keys[i])) {
						selected=((IKeyValue)selected).Parent;
					}
					if(selected==null) {
						throw new ApplicationException("Key "+keys[i]+" not found");
					}
				}
				int lastKeySelect=0;
				if(isSelectLastKey) {
					lastKeySelect++;
				}
				for(;i<keys.Count-1+lastKeySelect;i++) {
					if(keys[i].Equals("break")) {
						Interpreter.breakMethod(selected);
						Thread.CurrentThread.Suspend();
						if(Interpreter.redoStatement) {
							Interpreter.redoStatement=false;
							throw new RestartStatementException();
						}
					}	
					if(selected is IKeyValue) {
						selected=((IKeyValue)selected)[keys[i]];
					}
					else {
						if(selected==null) {
							throw new ApplicationException("Key "+keys[i]+" does not exist");
						}
						selected=new NetObject(selected,selected.GetType())[keys[i]];
					}

				}
				return selected;
			}
			public void Assign(ref object current,object val,bool isInFunction) {
				ArrayList keys=new ArrayList();
				foreach(IExpression expression in expressions) {
					keys.Add(expression.Evaluate((Map)current));
				}
				if(keys.Count==1 && keys[0].Equals("this")) {
					if(val is IKeyValue) {
						((IKeyValue)val).Parent=((IKeyValue)current).Parent;
						current=((IKeyValue)val).Clone();
						if(current is Map) {
							Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
							Interpreter.callers.Add(current);
						}
					}
					else {
						current=val;
						if(current is Map) {
							Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
							Interpreter.callers.Add(current);
						}
					}
				}
				else {
					object selected=Preselect(current,keys,false,false);
					IKeyValue keyValue;
					if(selected is IKeyValue) {
						keyValue=(IKeyValue)selected;
					}
					else {
						if(selected==null) { // when can this happen?
							int asdf=0;
						}
						keyValue=new NetObject(selected,selected.GetType());
					}
					keyValue[keys[keys.Count-1]]=val;
				}
			}
		}
		public class RestartStatementException: ApplicationException {
		}
		public delegate void BreakMethodDelegate(object obj);
		public class Interpreter  {
			public static string path;
			public static bool redoStatement=false; // just for editor, remove
			public static BreakMethodDelegate breakMethod; // editor, remove?

			public static ArrayList callers=new ArrayList();
			public static ArrayList arguments=new ArrayList();

			public static Hashtable netConversion=new Hashtable();
			public static Hashtable metaConversion=new Hashtable();

			public static ArrayList compiledMaps=new ArrayList(); // needed when conversion are added.
																					// make adding of conversions illegal?
			public static Map lastProgram; // only for editor, maybe move

			public static ArrayList loadedAssemblies=new ArrayList(); // not public


			public static object Replace(object original,object replace) { //check
				if(original.GetType().Equals(replace.GetType())) {
					if(original is Statement) {
						((Statement)original).Replace((Statement)replace);
					}
					else {
						((IExpression)original).Replace((IExpression)replace);
					}
					return original;
				}
				else {
					return replace;
				}
			}
			public static object Arg {	//not needed very often
				get {
					return arguments[arguments.Count-1];
				}
			}
			public static ILiteralRecognition[] Interceptions { // only used once
				get {
					return (ILiteralRecognition[])interception.ToArray(typeof(ILiteralRecognition));
				}
			}

			public static object ConvertToMeta(object obj) { 
				if(obj==null) {
					return null;
				}
				ToMetaConversion conversion=(ToMetaConversion)metaConversion[obj.GetType()];
				if(conversion!=null) {
					return conversion.Convert(obj);
				}
				else {
					return obj;
				}
			}
			public static object ConvertToNet(object obj,Type targetType) {
				try {
					ToNetConversion conversion=(ToNetConversion)((Hashtable)
						Interpreter.netConversion[obj.GetType()])[targetType];
					return conversion.Convert(obj);
				}
				catch {
					return obj;
				}
			}
			public static void AddToNetConversion(ToNetConversion conversion) { // maybe remove
				if(!netConversion.ContainsKey(conversion.target)) {
					netConversion[conversion.target]=new Hashtable();
				}
				((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
			}
			public static void AddToMetaConversion(ToMetaConversion conversion) { // maybe remove
				metaConversion[conversion.source]=conversion;
			}
			public static Map String(string symbol) { // maybe remove
				Map map=new Map();
				foreach(char character in symbol) {
					map[new Integer(map.Count+1)]=new Integer((int)character);
				}
				return map;
			}
			public static string String(Map symbol) { // move somewhere else
				string text="";
				for(Integer i=new Integer(1);;i++) {
					object val=symbol[i];
					if(val==null) {
						break;
					}
					else {
						if(val is Integer) {
							text+=Convert.ToChar(((Integer)val).LongValue());
						}
					}
				}
				return text;
			}
			[IgnoreMember]
			private static ArrayList interception=new ArrayList();
			[IgnoreMember]
			public static ArrayList cashedLiterals=new ArrayList(); // maybe remove, if interceptions cannot be added
																					  // at runtime

			public static void AddInterception(ILiteralRecognition i)  { //maybe remove
				interception.Add(i);
				foreach(Literal createLiteral in cashedLiterals) {
					createLiteral.cached=null;
				}
				cashedLiterals.Clear();
			}

			// which methods are necessary here?
//			public static object RunNormal(string path,Map argument) { // remove, will (soon) not be needed anymore
//				Interpreter.arguments.Add(argument);
//				StreamReader reader=new StreamReader(path);
//				lastProgram=Mapify(reader);
//				object result=lastProgram.Call((Map)callers[callers.Count-1],new Map());
//				Interpreter.arguments.Remove(argument);
//				reader.Close();
//				return result;
//			}
			public static object Run(string path,IKeyValue argument) {
				return Run(new StreamReader(path),argument);
			}
			public static object Run(TextReader reader,IKeyValue argument) {
				ArrayList parents=new ArrayList();
				Map existing=new Map();
//				existing["meta"]=new NetClass(typeof(Interpreter));
				foreach(MethodInfo method in typeof(Functions).GetMethods(
					BindingFlags.Public|BindingFlags.Static)) {
					existing[method.Name]=new NetMethod(method.Name,null,typeof(Functions));
				}
//				existing.Parent=new Meta.Types.Library();
				existing.Parent=Meta.Types.Library.library;
				Interpreter.arguments.Add(argument);
				lastProgram=Mapify(reader);
				object result=lastProgram.Call(new Map(),existing);
				Interpreter.arguments.Remove(argument);
				return result;
			}
			public static AST Parse(TextReader stream)  { //rename
				MetaANTLRParser parser=new Meta.Parser.MetaANTLRParser(
					new IndentParser(new MetaLexer(stream)));
				parser.map();
				return parser.getAST();
			}
			public static Map Mapify(TextReader input) { //rename
				return (new MetaTreeParser()).map(Parse(input));
			}
			static Interpreter() {
				Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
				path=Directory.GetParent(metaAssembly.Location).Parent.Parent.Parent.FullName;
				//path=Path.Combine(directoryName,"library");
				foreach(Type type in typeof(LiteralRecognitions).GetNestedTypes()) {
					AddInterception((ILiteralRecognition)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(ToMetaConversions).GetNestedTypes()) {
					AddToMetaConversion((ToMetaConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(ToNetConversions).GetNestedTypes()) {
					AddToNetConversion((ToNetConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
			}
			public static IKeyValue MergeTwo(IKeyValue first,IKeyValue second) { // remove
				Map map=new Map();
				map.Parent=first.Parent; //probably unnecessary
				map[new Integer(1)]=first;
				map[new Integer(2)]=second;
				return Merge(map);
			}
			[MetaMethod("")]
			public static IKeyValue Merge(Map maps) { // get rid of use of Map
				Map result=new Map();
				foreach(DictionaryEntry i in maps) {
					foreach(DictionaryEntry j in (IKeyValue)i.Value) {
						if(j.Value is IKeyValue && !(j.Value is NetClass)&& result.ContainsKey(j.Key) 
							&& result[j.Key] is IKeyValue && !(result[j.Key] is NetClass)) {
							result[j.Key]=MergeTwo((IKeyValue)result[j.Key],(IKeyValue)j.Value);
						}
						else {
							result[j.Key]=j.Value;
						}
					}
				}
				return result;
			}
			public static Map LoadAssembly(Map map,bool collapseNamespaces) { // move to LibraryMap
				Map root=new Map();
				foreach(DictionaryEntry entry in map) {
					string name=(string)entry.Value;
					Assembly assembly=Assembly.LoadWithPartialName(name);
					if(assembly==null)  {
						assembly=Assembly.LoadFrom(name);
					}
					if(assembly==null) {
						throw new ApplicationException(
							"The assembly "+name+" could not be found."+
							"Current directory: "+Directory.GetCurrentDirectory());
					}
					loadedAssemblies.Add(assembly.Location);

					foreach(Type type in assembly.GetTypes())  {
						if(type.DeclaringType==null)  {
							Map position=root;
							if(! collapseNamespaces) {
								ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
								subPaths.RemoveAt(subPaths.Count-1);
								foreach(string subPath in subPaths)  {
									if(!position.ContainsKey(subPath))  {
										position[subPath]=new Map();
									}
									position=(Map)position[subPath];
								}
							}
							position[type.Name]=new NetClass(type);
						}
					}
				}
				return root;
			}
//
//			public static string GetMethodName(MethodBase methodInfo) {
//				int counter=0;
//				string text="";
//				if(methodInfo is MethodInfo) {
//					text+=((MethodInfo)methodInfo).ReturnType.Name+" "+methodInfo.Name;
//				}
//				else {
//					text+=methodInfo.ReflectedType.Name;
//				}
//				text+=" (";
//				foreach(ParameterInfo parameter in methodInfo.GetParameters()) {
//					text+=parameter.ParameterType.Name+" "+parameter.Name;
//					if(counter!=methodInfo.GetParameters().Length-1) {
//						text+=",";
//					}
//					counter++;
//				}
//				text+=")";
//				return text;
//			}			
		}
	}
	namespace Types  {
//		public class LazyAssembly: IKeyValue {
//			Assembly assembly;
//			Map map;
//			public object this[object key] {
//				get {
//					if(map==null) {
//						map=new Map();
//						Library.LoadAssembly(
//					}
//				}
//				set {
//				}
//			}
//			public ArrayList Keys {
//				get {
//					return null;
//				}
//			}
//			public IKeyValue Clone() {
//				return null;
//			}
//			public int Count {
//				get {
//					return 0;
//				}
//			}
//
//			public bool ContainsKey(object key) {
//				return false;
//			}
//			public IKeyValue Parent {
//				get {
//					return null;
//				}
//				set {
//				}
//			}
//			public IEnumerator GetEnumerator() {
//				return null;
//			}
//		}
//
//		public class LazyAssembly: IKeyValue {
//			Assembly assembly;
//			public LazyAssembly(Assembly assembly) {
//				this.assembly=assembly;
//			}
//				return Library.LoadAssembly(assembly);
//			}
//		}
//		public class UnloadedAssembly {
//			Assembly assembly;
//			public UnloadedAssembly(Assembly assembly) {
//				this.assembly=assembly;
//			}
//			public object Load() {
//				return Library.LoadAssembly(assembly);
//			}
//		}
		public class UnloadedMetaLibrary {
			string path;
			public UnloadedMetaLibrary(string path) {
				this.path=path;
			}
			public object Load() {
				return Interpreter.Run(path,Library.library);
			}
		}
		public class Library: IKeyValue {
			public static Library library=new Library();
			private Map cash=new Map();
			public static string libraryPath="library"; // FIXME: how on earth to find this path?
																	  // maybe need config file
						
			public static Map LoadAssembly(Assembly assembly) {
				Map root=new Map();
				foreach(Type type in assembly.GetExportedTypes())  {
					if(type.DeclaringType==null)  {
						Map position=root;
						//								if(! collapseNamespaces) {
						ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
						subPaths.RemoveAt(subPaths.Count-1);
						foreach(string subPath in subPaths)  {
							if(!position.ContainsKey(subPath))  {
								position[subPath]=new Map();
							}
							position=(Map)position[subPath];
						}
						//								}
						position[type.Name]=new NetClass(type);
					}
				}
				return root;
			}
			public Library() {
				libraryPath=Path.Combine(Interpreter.path,"library");
//				Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
//				string directoryName=Directory.GetParent(metaAssembly.Location).Parent.FullName;
//				libraryPath=Path.Combine(directoryName,"library");

				IAssemblyEnum e=AssemblyCache.CreateGACEnum();
				IAssemblyName an; 
				AssemblyName name;
				cash=(Map)Interpreter.MergeTwo(cash,(Map)LoadAssembly(Assembly.LoadWithPartialName("mscorlib")));
//				cash["Microsoft"]=new Map();
				while (AssemblyCache.GetNextAssembly(e, out an) == 0) { 
					name=GetAssemblyName(an);
					Assembly assembly=Assembly.LoadWithPartialName(name.Name);
//					if(name.Name.StartsWith("mscorlib")) {
//						int adf=0;
//					}
//					if(name.Name.Equals("System")) {
//						int asdf=0;
//					}
					if(name.Name.StartsWith("Microsoft.")) {
						microsoftAssemblies.Add(assembly);
//						int startIndex=name.Name.IndexOf(".");
//						int endIndex=name.Name.IndexOf(".",startIndex+1);
//						if(endIndex==-1) {
//							endIndex=name.Name.Length;
//						}
//						string subNamespace=name.Name.Substring(startIndex+1,endIndex-startIndex-1);
//						((Map)cash["Microsoft"])[subNamespace]
//							=assembly;
					}
					else {
						cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
					}
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.dll")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.exe")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
				}
//				Map x=(Map)cash["Test"];
//				object y=x["TestClass"];
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.meta")) {
					cash[Path.GetFileNameWithoutExtension(fileName)]=new UnloadedMetaLibrary(fileName);
				}
			}
			ArrayList microsoftAssemblies=new ArrayList();
			private static AssemblyName GetAssemblyName(IAssemblyName nameRef) {
				AssemblyName name = new AssemblyName();
				name.Name = AssemblyCache.GetName(nameRef);
				name.Version = AssemblyCache.GetVersion(nameRef);
				name.CultureInfo = AssemblyCache.GetCulture(nameRef);
				name.SetPublicKeyToken(AssemblyCache.GetPublicKeyToken(nameRef));
				return name;
			}
			bool microsoftLoaded=false;
			public object this[object key] {
				get {
					if(key.Equals("Test")) {
						int asdf=0;
					}
					if(cash.ContainsKey(key)) {
						if(key.Equals("Microsoft")) {
							if(!microsoftLoaded) {
								foreach(Assembly assembly in microsoftAssemblies) {
									cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
								}
//								foreach(DictionaryEntry entry in (Map)cash["Microsoft"]) {
//									if(entry.Value is Assembly) { // could get assemblies from fusion, too
//										cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly((Assembly)entry.Value));
//									}
//								}
								microsoftLoaded=true;
							}
						}
//						if(cash[key] is UnloadedAssembly) {
//							cash=(Map)Interpreter.MergeTwo(cash,(IKeyValue)((UnloadedAssembly)cash[key]).Load());
//						}
						else if(cash[key] is UnloadedMetaLibrary) {
							cash[key]=((UnloadedMetaLibrary)cash[key]).Load();
						}
						return cash[key];
					}
					else {
						return null;
					}
				}
				set {
					throw new ApplicationException("Keys in library cannot be set.");
				}
			}
			public ArrayList Keys {
				get {
					return new ArrayList(cash.Keys);
				}
			}
			public IKeyValue Clone() {
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
			public IKeyValue Parent {
				get {
					return null;
				}
				set {
					throw new ApplicationException("Tried to set parent of library.");
				}
			}
			public IEnumerator GetEnumerator() {
				foreach(DictionaryEntry entry in cash) { // to make sure everything is loaded
					object o=cash[entry.Key];						  // not good, should make own enumerator
				}
				return cash.GetEnumerator();
			}
		}

		public interface IMetaType { // rethink what this is useful for
			IKeyValue Parent {
				get;
				set;
			}
		}
		public abstract class Callable { // confusing, just because of Parent, really still needed?
													// if useful, rename
			private IKeyValue parent;
			[IgnoreMember]
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
		}
		public interface ICallable: IMetaType{ // not very useful, but ok
			object Call(Map caller);
		}
		public interface IKeyValue: IMetaType,IEnumerable { // not used consequently instead of map
			object this[object key] {
				get;
				set;
			}
			ArrayList Keys {
				get;
			}
			IKeyValue Clone();
			int Count {
				get;
			}

			bool ContainsKey(object key);			
		}		
		public class Map: IKeyValue, ICallable, IEnumerable { // could use Callable
			public Map arg;
			private IKeyValue parent;
			public int numberAutokeys=0;
			private ArrayList keys;
			public HybridDictionary table;
			public object compiled;

			public IKeyValue Parent {
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
			public ArrayList IntKeyValues { // rename
				get {
					ArrayList list=new ArrayList();
					for(Integer i=new Integer(1);ContainsKey(i);i++) {
						list.Add(this[i]);
					}
					return list;
				}
			}
			public object this[object key]  {
				get  {
					object firstResult=table[key];
					if(firstResult==null) {
						if(arg!=null) {
							return arg[key];
						}
					}
					return firstResult;
				}
//				get  {
//					return table[key];
//				}
				set  {
					if(value!=null) {
						isHashCashed=false;
						object val;
						if(value is IKeyValue) {
							val=((IKeyValue)value).Clone();
						}
						else {
							val=value;
						}			
						if(value is IMetaType) {
							((IMetaType)val).Parent=this;
						}
						if(!table.Contains(key)) {
							keys.Add(key);
						}
						table[key]=val;
					}
				}
			}
			public object Call(Map caller) {
//				Map local=new Map(); //unnecessary for non-programs
				((Map)Interpreter.Arg).Parent=this.Parent;
//				local.Parent=(Map)Interpreter.Arg;
				return Call(caller,(Map)Interpreter.Arg);
			}
			public object Call(Map caller,Map local)  {
				IExpression callable=(IExpression)Compile();
				object result;
				if(callable is Program) { // somehow wrong
					result=((Program)callable).Evaluate(caller,local,true);
				}
				else {
					result=callable.Evaluate(local);
				}
				return result;
			}
			public void StopSharing() { // currently not used at all
				compiled=null;
				HybridDictionary oldTable=table;
				ArrayList oldKeys=keys;
				table=new HybridDictionary();
				keys=new ArrayList();
				foreach(object key in oldKeys) {
					this[key]=oldTable[key];
				}
			}
			public ArrayList Keys {
				get {
					return keys;
				}
			}
			public IKeyValue Clone() {
				Map copy=new Map();
				foreach(object key in keys) {
					copy[key]=this[key];
				}
				copy.Parent=Parent;
				copy.compiled=compiled;
				return copy;
			}
			public object Compile()  {
				if(compiled==null)  {
					switch((string)this.Keys[0]) {
						case "call":
							compiled=new Call(this);break;
						case "delayed":
							compiled=new Delayed(this);break;
						case "program":
							compiled=new Program(this);break;
						case "literal":
							compiled=new Literal(this);break;
						case "select":
							compiled=new Select(this);break;
						case "value":
						case "key":
							compiled=new Statement(this);break;
						default:
							throw new ApplicationException("Map cannot be compiled because it is not an expression.");
					}
					if(!Interpreter.compiledMaps.Contains(this)) { // necessary?
						Interpreter.compiledMaps.Add(this);
					}
				}
				return compiled;
			}
			public bool ContainsKey(object key)  {
				if(!table.Contains(key)) {
					if(arg!=null) {
						return arg.ContainsKey(key);
					}
					else {
						return false;
					}
				}
				return true;
			}
//			public bool ContainsKey(object key)  {
//				return table.Contains(key);
//			}
			public override bool Equals(object obj) { // change because of arg necessary???
				bool equal=true;
				if(!Object.ReferenceEquals(obj,this)) {
					if(!(obj is Map)) {
						equal=false;
					}
					else {
						Map map=(Map)obj;
						if(map.Count==Count) {
							foreach(DictionaryEntry entry in this)  {
								if(!map.ContainsKey(entry.Key)||!map[entry.Key].Equals(entry.Value)) {
									equal=false;
									break;
								}
							}
						}
						else {
							equal=false;
						}
					}
				}
				return equal;
			}
			public IEnumerator GetEnumerator() {
				return new MapEnumerator(this);
			}
			private bool isHashCashed=false;
			private int hash;
			public override int GetHashCode()  { // change because of arg necessary???
				if(!isHashCashed) {
					int h=0;
					foreach(DictionaryEntry entry in table) {
						unchecked {
							h+=entry.Key.GetHashCode()*entry.Value.GetHashCode();
						}
					}
					hash=h;
					isHashCashed=true;
				}
				return hash;
			}
			public Map() {
				this.table=new HybridDictionary();
				this.keys=new ArrayList();
			}
		}
		public class MapEnumerator: IEnumerator {
			private int index=-1;
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
		}
		[AttributeUsage(AttributeTargets.Method|AttributeTargets.Constructor,Inherited=true)]
		public class MetaMethodAttribute:Attribute { // still needed? maybe needed again soon
			public MetaMethodAttribute(string documentation) {
				if(documentation!="") {
					this.documentation=documentation;
				}
				else {
					this.documentation="<no documentation>";
				}
			}
			public string documentation;
		}
		public class NetMethod: ICallable {
			private IKeyValue parent;
			[IgnoreMember]
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			private MetaMethodAttribute attribute;
			private BindingFlags invokeMemberFlags;
			private string name;

			protected object target;
			protected Type type;
			public MethodBase savedMethod;
			public MethodBase[] methods;
			


			public object Call(Map caller) {
				return Interpreter.ConvertToMeta(CallMethod((Map)Interpreter.Arg));
			}
			public object CallMethod(Map arguments) {
				ArrayList list;
				if(attribute!=null) {
					list=new ArrayList();
					list.Add(arguments);
				}
				else {
					list=arguments.IntKeyValues;
				}
				object result=null;
				try { // doubtful usage of try
					//refactor
					ArrayList methods;
					if(name=="") {
						methods=new ArrayList(type.GetConstructors());
					}
					else {
						methods=new ArrayList(type.GetMember(name,BindingFlags.Public|
							BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
					}
					bool executed=false;
					methods.Reverse();
					if(this.name.Equals("BinaryOr")) {
						int asdf=0;
					}
					foreach(MethodBase method in methods) {
						ArrayList args=new ArrayList();
						int counter=0;
						bool argumentsMatched=true;
						ParameterInfo[] parameters=method.GetParameters();
						if(list.Count>parameters.Length && parameters.Length>0) {
							Type lastParameter=parameters[parameters.Length-1].ParameterType;
							if(lastParameter.IsArray || lastParameter.IsSubclassOf(typeof(Array))) {
								Map lastArg=new Map();
								ArrayList paramsArgs=list.GetRange(parameters.Length-1,list.Count-(parameters.Length-1));
								for(int i=0;i<paramsArgs.Count;i++) {
									lastArg[new Integer(i+1)]=paramsArgs[i];
								}
								list[parameters.Length-1]=lastArg;									
								list.RemoveRange(parameters.Length,list.Count-parameters.Length);
							}
						}
						if(list.Count!=parameters.Length) {
							argumentsMatched=false;
						}
						else {
							foreach(ParameterInfo parameter in method.GetParameters()) {
								bool matched=false;
								if(name.Equals("BinaryOr")) {
									Type t=list[counter].GetType();
									int asdf=0;
								}
								if(list[counter].GetType().Equals(parameter.ParameterType)
									||list[counter].GetType().IsSubclassOf(parameter.ParameterType)) {
									args.Add(list[counter]);
									matched=true;
								}
								else {
									if(parameter.ParameterType.IsSubclassOf(typeof(Delegate))) {
										try {
											MethodInfo m=parameter.ParameterType.GetMethod("Invoke",BindingFlags.Instance
												|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
											Delegate del=CreateDelegate(parameter.ParameterType,m,(Map)list[counter]);
											args.Add(del);
											matched=true;
										}
										catch{
										}
									}
									if(!matched && parameter.ParameterType.IsArray && ((Map)list[counter]).IntKeyValues.Count!=0) {// cheating
										try {
											Type elementType=parameter.ParameterType.GetElementType();
											Map map=((Map)list[counter]);
											ArrayList mapValues=map.IntKeyValues;
											Array array=Array.CreateInstance(elementType,mapValues.Count);
											for(int i=0;i<mapValues.Count;i++) {
												array.SetValue(mapValues[i],i);
											}
											args.Add(array);
											matched=true;										
										}
										catch {
										}

									}
									if(!matched) {
										Hashtable toDotNet=(Hashtable)
											Interpreter.netConversion[parameter.ParameterType];
										if(toDotNet!=null) {
											ToNetConversion conversion=(ToNetConversion)toDotNet[list[counter].GetType()];
											if(conversion!=null) {
												try {
													args.Add(conversion.Convert(list[counter]));
													matched=true;
												}
												catch (Exception e) {
													int asdf=0;
												}
											}
										}
									}
								}
								if(!matched) {
									argumentsMatched=false;
									break;
								}
								counter++;
							}
						}
						if(argumentsMatched) {
							if(!executed) {
								if(method is ConstructorInfo) {
									result=((ConstructorInfo)method).Invoke(args.ToArray());
								}
								else {
									result=method.Invoke(target,args.ToArray());
								}
								executed=true;
							}
							else {
								//throw new ApplicationException("\nArguments match more than one overload of "+name);
							}
						}
					}
					if(!executed) {
						//						try {
						return savedMethod.Invoke(target,new object[] {});
						//						}
						//						catch {
						//							throw new ApplicationException(name+" could not be invoked,"+
						//								"the parameters do not match");
						//						}
					}
					else {
						return result;
					}

				}
				catch(Exception e) {
					Exception b=e;
					string text="";
					if(target!=null) {
						text+=target.ToString();
					}
					else {
						text+=type.ToString();
					}
					text+=".";
					text+=name;
					text+="(";
					foreach(object obj in list) {
						text+=obj.ToString()+",";
					}
					text=text.Remove(text.Length-1,1);
					text+=")";
					text+=" could not be invoked. ";
					text+=e.ToString();
					throw new ApplicationException(text);
				}
			}
			public Delegate CreateDelegate(Type delegateType,MethodInfo method,Map code) {
				// should caller really be parent of code???
				code.Parent=(IKeyValue)Interpreter.callers[Interpreter.callers.Count-1];
				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
				ICodeCompiler compiler=codeProvider.CreateCompiler();
				string returnTypeName=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
				string source="using System;using Meta.Types;using Meta.Execution;";
				source+="public class EventHandlerContainer{public "+returnTypeName+" EventHandlerMethod";
				int counter=1;
				string argumentList="(";
				string argumentAdding="Map arg=new Map();";
				// here bug
				foreach(ParameterInfo parameter in method.GetParameters()) {
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
					if(counter<method.GetParameters().Length) {
						argumentList+=",";
					}
					else {
						argumentList+=")";
					}
					counter++;
				}
				source+=argumentList+"{";
				source+=argumentAdding;
				source+="Interpreter.arguments.Add(arg);object result=callable.Call(null);Interpreter.arguments.Remove(arg);";
				if(!method.ReturnType.Equals(typeof(void))) {
					source+="return ("+returnTypeName+")";
					source+="Interpreter.ConvertToNet(result,typeof("+returnTypeName+"));";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll","Meta.dll"});
				assemblyNames.AddRange(Interpreter.loadedAssemblies);
				CompilerParameters options=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
																																			  code});
				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
				Delegate del=Delegate.CreateDelegate(delegateType,
					container,"EventHandlerMethod");
				return del;
			}
			//rename
			private void Init(string name,object target,Type type,BindingFlags invokeFlags,
				MethodBase method,MethodBase[] methods) {
				this.name=name;
				this.target=target;
				this.type=type;
				this.invokeMemberFlags=invokeFlags;
				this.savedMethod=method;
				if(method!=null) {
					Attribute[] attributes=(Attribute[])
						method.GetCustomAttributes(typeof(MetaMethodAttribute),true);
					if(attributes.Length!=0) {
						this.attribute=(MetaMethodAttribute)attributes[0];
					}
				}
				this.methods=methods;
			}
			// refactor
			public NetMethod(string name,object target,Type type) {
				MethodBase method=null;
				MethodInfo[] methods=
					(MethodInfo[])type.GetMember(name,
						MemberTypes.Method,BindingFlags.Public
						|BindingFlags.Static|BindingFlags.Instance);
				if(methods.Length==1) {
					method=methods[0];
				}
				this.Init(name,target,type,
					BindingFlags.Public|BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.InvokeMethod,method,methods);
			}
			// is 'constructor' necessary or do 'constructors' suffice?
			public NetMethod(Type type) {
				MethodBase constructor=null;
				MethodBase[] constructors=type.GetConstructors();
				if(constructors.Length==1) {
					constructor=type.GetConstructors()[0];
				}
				this.Init("",null,type,BindingFlags.CreateInstance,constructor,constructors);
			}
			public override bool Equals(object obj) {
				if(obj is NetMethod) {
					NetMethod method=(NetMethod)obj;
					if(method.target==target && method.name.Equals(name) && 
						method.type.Equals(type)) {
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
		}
		public class NetClass: NetContainer, IKeyValue,ICallable {
			[IgnoreMember]
			public NetMethod constructor;
			public object Call(Map caller) {
				return constructor.Call(null);
			}
			public NetClass(Type type):base(null,type) {
				this.constructor=new NetMethod(this.type);
			}
		}
		public class NetObject: NetContainer, IKeyValue {
			public NetObject(object obj,Type type):base(obj,type) {
			}
			public override string ToString() {
				return obj.ToString();
			}
		}
		public abstract class NetContainer: IKeyValue, IEnumerable,ICustomSerialization {
			public string CustomSerialization() {
				return "";
			}
			private IKeyValue parent;
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			public object obj;
			public Type type;
			public ArrayList Keys {
				get {
					return new ArrayList(Table.Keys);
				}
			}
			
			public NetContainer(object obj,Type type) {
				this.obj=obj;
				this.type=type;
				
			}
			public int Count  {
				get {
					int count=0;
					foreach(object obj in this) {
						count++;
					}
					return count;
				}
			}
			public IKeyValue Clone() {
				return this;
			}
			public virtual object this[object key]  {
				get {
					if(key is string) {
						try {
							string text=(string)key;
							if(type.GetMember((string)text,
								MemberTypes.Method,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								return new NetMethod((string)text,obj,type);
							}
							if(type.GetMember((string)text,
								MemberTypes.Field,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								return Interpreter.ConvertToMeta(
									type.GetField((string)text).GetValue(obj));
							}
							else if(type.GetMember((string)text,
								MemberTypes.Property,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								return Interpreter.ConvertToMeta( type.GetProperty((string)text).GetValue(obj,new object[]{}));
							}
							else if(type.GetMember((string)text,
								MemberTypes.Event,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
									EventInfo eventInfo=((EventInfo)type.GetMember(
									(string)text,MemberTypes.Event,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|
									BindingFlags.Instance)[0]);
								Delegate del=(Delegate)type.GetField((string)text,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|
									BindingFlags.Instance).GetValue(obj);
								return new NetMethod("Invoke",del,del.GetType());
							}
						}
						catch(Exception e) {
						}
					}
					if(key.Equals(1)) {
						int asdf=0;
					}
					if(obj is String) {
					}
					NetMethod indexer=new NetMethod("get_Item",obj,type);
					try {
						Map arguments=new Map();
						arguments[new Integer(1)]=key;
						Interpreter.arguments.Add(arguments);
						object result=indexer.Call(null);
						Interpreter.arguments.Remove(arguments);
						return result;
					}
					catch(Exception e) {
						return null;
					}
				}
				set {		
					if(key is string) {
							string text=(string)key;

							if(type.GetMember((string)text,
								MemberTypes.Method,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								throw new ApplicationException("Methods cannot be set.");
							}
							else if(type.GetMember((string)text,
								MemberTypes.Field,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								FieldInfo field=type.GetField((string)text);
								if(field.FieldType.Equals(value.GetType())) {
									field.SetValue(obj,value);
									return;
								}
								else {
									Hashtable toDotNet=(Hashtable)
										Interpreter.netConversion[field.FieldType];
									if(toDotNet!=null) {
										ToNetConversion conversion=(ToNetConversion)toDotNet[value.GetType()];
										if(conversion!=null) {
											try {
												field.SetValue(obj,conversion.Convert(value));
												return;
											}
											catch{
											}
										}
									}

								}
							}
							else if(type.GetMember((string)text,
								MemberTypes.Property,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								PropertyInfo field=type.GetProperty((string)text);
								if(field.PropertyType.Equals(value.GetType())) {
									field.SetValue(obj,value,null);
									return;
								}
								else {
									Hashtable toDotNet=(Hashtable)
										Interpreter.netConversion[field.PropertyType];
									if(toDotNet!=null) {
										ToNetConversion conversion=(ToNetConversion)toDotNet[value.GetType()];
										if(conversion!=null) {
											try {
												field.SetValue(obj,conversion.Convert(value),null);
												return;
											}
											catch{
											}
										}
									}

								}
							}
							else if(type.GetMember((string)text,
								MemberTypes.Event,BindingFlags.Public
								|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
								
								type.GetEvent((string)text).AddEventHandler(obj,CreateEvent((string)text,(Map)value));
								return;
							}
					}
					try {
						NetMethod indexer=new NetMethod("set_Item",obj,type);
						Map arguments=new Map();
						arguments[new Integer(1)]=key;
						arguments[new Integer(2)]=value;
						Interpreter.arguments.Add(arguments);
						indexer.Call(null);
						Interpreter.arguments.Remove(arguments);
					}
					catch(Exception e) {
						throw new ApplicationException(key.ToString()+" not found in "+ToString());
					}
				}
			}
			public Delegate CreateEvent(string name,Map code) {
				code.Parent=(IKeyValue)Interpreter.callers[Interpreter.callers.Count-1];
				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
				ICodeCompiler compiler=codeProvider.CreateCompiler();
				FieldInfo field=type.GetField(name,BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);

				EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
					BindingFlags.Static|BindingFlags.Instance);
				MethodInfo method=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);

				string returnTypeName=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
				string source="using System;using Meta.Types;using Meta.Execution;";
				source+="public class EventHandlerContainer{public "+returnTypeName+" EventHandlerMethod";
				int counter=1;
				string argumentList="(";
				string argumentAdding="Map arg=new Map();";
				// here bug
				foreach(ParameterInfo parameter in method.GetParameters()) {
					argumentList+=parameter.ParameterType.FullName+" arg"+counter;
					argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
					if(counter<method.GetParameters().Length) {
						argumentList+=",";
					}
					else {
						argumentList+=")";
					}
					counter++;
				}
				source+=argumentList+"{";
				source+=argumentAdding;
				source+="Interpreter.arguments.Add(arg);object result=callable.Call(null);Interpreter.arguments.Remove(arg);";
				if(!method.ReturnType.Equals(typeof(void))) {
					source+="return ("+returnTypeName+")";
					source+="Interpreter.ConvertToNet(result,typeof("+returnTypeName+"));";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll","Meta.dll"});
				assemblyNames.AddRange(Interpreter.loadedAssemblies);
				CompilerParameters options=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructors()[0].Invoke(new object[] {
																																			  code});
//				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
//																																			  code});
				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
				Delegate del=Delegate.CreateDelegate(type.GetEvent(name).EventHandlerType,
					container,"EventHandlerMethod");
				return del;
			}
			public bool ContainsKey(object key)  {
				try  {
					object o=this[key];
				}
				catch {
					return false;
				}
				return true;
			}
			public IEnumerator GetEnumerator() {
				return Table.GetEnumerator();
			}
			private IDictionary Table {
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
						table[field.Name]=field.GetValue(obj);
					}
					foreach(MethodInfo method in type.GetMethods(bindingFlags))  {
						if(!method.IsSpecialName) {
							table[method.Name]=new NetMethod(method.Name,obj,type);
						}
					}
					foreach(PropertyInfo property in type.GetProperties(bindingFlags)) {
						if(property.Name!="Item" && property.Name!="Chars") {
							table[property.Name]=property.GetValue(obj,new object[]{});
						}
					}
					foreach(EventInfo eventInfo in type.GetEvents(bindingFlags)) {
						table[eventInfo.Name]=new NetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
					}
					int counter=1;
					if(obj!=null && obj is IEnumerable && !(obj is String))  {
						foreach(object entry in (IEnumerable)obj) {
							if(entry is DictionaryEntry)  {
								table[((DictionaryEntry)entry).Key]=((DictionaryEntry)entry).Value;
							}
							else {
								table[counter]=entry;
								counter++;
							}
						}
					}
					return table;
				}
			}
		}
	}
	namespace Parser  {
		class IndentParser: TokenStream {
			protected Queue tokenBuffer=new Queue();
			protected TokenStream tokenStream;
			protected int indent=-1;
			protected const int indentWidth=2;
			public IndentParser(TokenStream tokenStream)  {
				this.tokenStream=tokenStream;
				AddIndentationTokens(0);
			}
			public Token nextToken()  {
				if(tokenBuffer.Count==0)  {
					Token t=tokenStream.nextToken();
					switch(t.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokens(-1);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokens(t.getText().Length/indentWidth);
							break;
						default:
							tokenBuffer.Enqueue(t);
							break;
					}
				}
				return (Token)tokenBuffer.Dequeue();
			}	
			protected void AddIndentationTokens(int newIndent)  { // refactor
				int level=newIndent-indent; if(level==0) {
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				} else if(level==1) {
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				} else if(level<0) { for(int i=level;i<0;i++) {
						tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				} else if(level>1) {
					throw new ApplicationException("Line is indented too much.");
				}
				indent=newIndent;
			}

		}
	}

	namespace TestingFramework {
		public abstract class MetaTest {
			public abstract object GetTestResult();
		}
		public class CustomSerializationException:Exception { // used at all?
		}
		public interface ICustomSerialization {
			string CustomSerialization();
		}
		public class TestExecuter {	
			public TestExecuter(Type classType,string path) { // refactor
				bool allTestsSuccessful=true;
				Type[] testClasses=classType.GetNestedTypes();

				foreach(Type testClass in testClasses) {
					MetaTest testInstance=(MetaTest)testClass.InvokeMember(
						"",BindingFlags.CreateInstance,null,null,null);

					object[] methodAttributes=
						testClass.GetCustomAttributes(typeof(SerializeMethodsAttribute),false);
					string[] methodNames=new string[0];
					if(methodAttributes.Length!=0) {
						methodNames=((SerializeMethodsAttribute)methodAttributes[0]).methodNames;
					}

					Console.Write(testClass.Name + "...");
					DateTime startTime=DateTime.Now;
					//				HighPerfomanceTimer timer=new HighPerfomanceTimer();
					//				timer.Start();
					string output="";
					object result=testInstance.GetTestResult();
					TimeSpan duration=DateTime.Now-startTime;
					bool testSuccessful=SetResult(
						testInstance,Path.Combine(path,testClass.Name),result,methodNames);
					if(!testSuccessful) {
						output=output + " failed";
						allTestsSuccessful=false;
					}
					else {
						output=output + " succeeded";
					}
					//				timer.Stop();
					//				output + ="  " + timer.Duration + " s";
					output=output + "  " + duration.TotalSeconds.ToString() + " s";
					Console.WriteLine(output);
				}
				if(!allTestsSuccessful) {
					Console.ReadLine();
				}
			}

			private bool SetResult(MetaTest test,string path,object obj,string[] functions) {
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

				string check=new StreamReader(Path.Combine(path,"check.txt")).ReadToEnd();
				return result.Equals(check);
			}

			public static string Serialize(object obj) {
				return Serialize(obj,"",new string[]{});
			}
			public static string Serialize(object obj,string indentation,string[] methodNames) {
				if(obj==null) {
					return indentation + "null\n";
				}
				if(obj is ICustomSerialization) {
					try {
						return indentation + ((ICustomSerialization)obj).CustomSerialization();
					}
					catch(CustomSerializationException e) {
					}
				}
				try {
					MethodInfo toString=obj.GetType().GetMethod("ToString",
						BindingFlags.Public|BindingFlags.DeclaredOnly|BindingFlags.Instance,
						null,new Type[]{},new ParameterModifier[]{});
					if(toString!=null) {
						return indentation + "\"" + obj.ToString() + "\"\n";
					}
				}
				catch(Exception e) {
				}
				if(obj is IEnumerable) {
					string text="";
					text=text + indentation + "IEnumerable\n";
					int count=0;
					foreach(object entry in (IEnumerable)obj) {
						count++;
						text=text + indentation + "  " + "Entry (" + entry.GetType().Name + "):\n" + 
							Serialize(entry,indentation + "    ",methodNames);
					}
					return text;
				}
				else {
					string text="";

					PropertyInfo[] properties=
						obj.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance);
					FieldInfo[] fields=
						obj.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance);

					ArrayList members=new ArrayList();
					members.AddRange(properties);
					members.AddRange(fields);
					foreach(string method in methodNames) {
						members.Add(obj.GetType().GetMethod(method));
					}
					members.Sort(new MemberSorter());

					foreach(MemberInfo memberInfo in members) {
						if(memberInfo.Name!="Item") {
							object[] ignoreAttributes=
								memberInfo.GetCustomAttributes(typeof(IgnoreMemberAttribute),false);
							if(ignoreAttributes.Length==0) {
								object val=obj.GetType().InvokeMember(memberInfo.Name,
									BindingFlags.Public|BindingFlags.Instance|BindingFlags.GetProperty|
									BindingFlags.GetField|BindingFlags.InvokeMethod,null,obj,null);

								text=text + indentation + memberInfo.Name;

								if(val!=null) {
									text=text + " ("  +  val.GetType().Name  + ")";
								}
								text=text + ":\n" + Serialize(val,indentation + "  ",methodNames);
							}
						}
					}
					return text;
				}
			}
		}

		internal class MemberSorter:IComparer { // rename
			public int Compare(object first,object second) {
				return ((MemberInfo)first).Name.CompareTo(((MemberInfo)second).Name);
			}
		}

		[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
		public class IgnoreMemberAttribute:Attribute {
		}

		[AttributeUsage(AttributeTargets.Class)]
		public class SerializeMethodsAttribute:Attribute {
			public SerializeMethodsAttribute(string[] methodNames) {
				this.methodNames=methodNames;
			}
			public string[] methodNames;
		}
	
		// measure time exactly

		//	internal class HighPerfomanceTimer
		//	{
		//		[DllImport("Kernel32.dll")]
		//		private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
		//		[DllImport("Kernel32.dll")]
		//		private static extern bool QueryPerformanceFrequency(out long lpFrequency);
		//		private long startTime, stopTime;
		//		private long freq;
		//		public HighPerfomanceTimer()
		//		{
		//			startTime = 0;
		//			stopTime  = 0;
		//			if (QueryPerformanceFrequency(out freq) == false)
		//			{
		//				throw new Win32Exception();
		//			}
		//		}
		//		public void Start()
		//		{
		//			Thread.Sleep(0);
		//			QueryPerformanceCounter(out startTime);
		//		}
		//		public void Stop()
		//		{
		//			QueryPerformanceCounter(out stopTime);
		//		}
		//		public double Duration
		//		{
		//			get
		//			{
		//				return (double)(stopTime - startTime) / (double) freq;
		//			}
		//		}
		//	}
	}
}
