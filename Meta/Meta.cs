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
using Meta.StandardLibrary;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Xml;
using System.Runtime.InteropServices;

namespace Meta {
	namespace StandardLibrary {
		public class Files:IKeyValue {
			public IKeyValue Clone() {
				return this;
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
			private string path;
			public Files() {
				this.path=Directory.GetCurrentDirectory();
			}
			public Files(string path) {
				this.path=path;
			}
			public object this[object key] {
				get {
					if(key.Equals("up")) {
						return new Files(Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
					}
					string name=this.path+Path.DirectorySeparatorChar;
					if(key is String) {
						name+=(string)key;
					}
					else if(key is Map) {
						name+=Interpreter.String((Map)key);
					}
					else {
						return null;
					}
					if(Directory.Exists(name)) {
						return new Files(name);
					}
					else if(File.Exists(name)) {
						switch(Path.GetExtension(name)) {
							case ".txt":
								StreamReader reader=new StreamReader(name);
								string text=reader.ReadToEnd();
								reader.Close();
								return Interpreter.String(text);
							default:
								return null;
						}
					}
					else {
						return null;
					}
				}
				set {
					string name=this.path+Path.DirectorySeparatorChar;
					if(key is String) {
						name+=(string)key;
					}
					else if(key is Map) {
						name+=Interpreter.String((Map)key);
					}
					else {
						throw new ApplicationException("File not found");
					}
					StreamWriter writer;
					if(!File.Exists(name)) {
						writer=new StreamWriter(File.Create(name));
					}
					else {
						writer=new StreamWriter(name);
					}
					writer.Write(Interpreter.String((Map)value));
					writer.Close();
				}
			}
			public int Count {
				get {
					return this.Keys.Count;
				}
			}
			public ArrayList Keys {
				get {
					return new ArrayList();
				}
			}
			public bool ContainsKey(object key) {
				return this[key]!=null;
			}
			public IEnumerator GetEnumerator() {
				return this.Keys.GetEnumerator();
			}
		}
		public class Assemblies: IKeyValue {
			public IKeyValue Clone() {
				return this;
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
			private Hashtable cashed=new Hashtable();
			public object this[object key] {
				get {
					try {
						Map root=new Map();
						string name=(string)key;
						Assembly assembly=Assembly.LoadWithPartialName(name);
						if(assembly==null)  {
							try {
								assembly=Assembly.LoadFrom(name+".dll");
							}
							catch {
								assembly=Assembly.LoadFrom(name+".exe");
							}
						}
						if(assembly==null) {
							throw new ApplicationException(
								"The assembly "+name+" could not be found."+
								"Current directory: "+Directory.GetCurrentDirectory());
						}
						foreach(Type type in assembly.GetTypes())  {
							if(type.DeclaringType==null)  {
								Map position=root;
								ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
								subPaths.RemoveAt(subPaths.Count-1);
								foreach(string subPath in subPaths)  {
									if(!position.ContainsKey(subPath))  {
										position[subPath]=new Map();
									}
									position=(Map)position[subPath];
								}
								position[type.Name]=new NetClass(type);
							}
						}
						return root;
					}
					catch {
						return null;
					}
				}
				set {
					throw new ApplicationException("Assemblies cannot be set.");
				}
			}

			public int Count {
				get {
					return this.Keys.Count;
				}
			}

			public ArrayList Keys {
				get {
					ArrayList keys=new ArrayList();
					foreach(string file in Directory.GetFiles(Directory.GetCurrentDirectory(),"*.dll*")) {
						keys.Add(Path.GetFileNameWithoutExtension(file));
					}
					return keys;
				}
			}

			public bool ContainsKey(object key) {
				return this[key]!=null;
			}
			public IEnumerator GetEnumerator() {
				return this.Keys.GetEnumerator();
			}
		}
		// builtin library functions
		public class Functions{

			public static void Write(string s) {
				Console.WriteLine(s);
			}
			public static string Read() {
				return Console.ReadLine();
			}
			public static bool And(bool a,bool b) {
				return a && b;
			}
			public static bool Or(bool a,bool b) {
				return a || b;
			}
			public static bool Not(bool a) {
				return !a;
			}
			public static Integer Add(Integer x,Integer y) {
				return x+y;
			}
			public static Integer Subtract(Integer x,Integer y) {
				return x-y;		
			}
			public static Integer Multiply(Integer x,Integer y) {
				return x*y;
			}
			public static Integer Divide(Integer x,Integer y) {
				return x/y;
			}
			public static bool Smaller(Integer x,Integer y) {
				return x<y;
			}
			public static bool Greater(Integer x,Integer y) {
				return x>y;
			}
			public static bool Equal(object a,object b) {
				return a.Equals(b);
			}
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
		public abstract class ToNetConversions  {
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
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt32: ToNetConversion   {
				public IntegerToInt32()   {
					this.source=typeof(Integer);
					this.target=typeof(Int32);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt64: ToNetConversion   {
				public IntegerToInt64()   {
					this.source=typeof(Integer);
					this.target=typeof(Int64);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToUInt64: ToNetConversion   {
				public IntegerToUInt64()   {
					this.source=typeof(Integer);
					this.target=typeof(UInt64);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToInt16: ToNetConversion   {
				public IntegerToInt16()   {
					this.source=typeof(Integer);
					this.target=typeof(Int16);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class IntegerToUInt16: ToNetConversion   {
				public IntegerToUInt16()   {
					this.source=typeof(Integer);
					this.target=typeof(UInt16);
				}
				public override object Convert(object obj)   {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
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
		public abstract class ToMetaConversions   {
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
			public readonly Select key;
			public readonly IExpression val;

			public void Realize(ref object scope,bool isInFunction) {
				key.Assign(ref scope,this.val.Evaluate((Map)scope),isInFunction);
			}
			public Statement(Map obj) {
				
				this.key=(Select)((Map)obj["key"]).Compile();
				this.val=(IExpression)((Map)obj["value"]).Compile();
			}
		}
		public interface IExpression {
			object Evaluate(object current);
		}
		public class Call: IExpression {	

			public readonly IExpression argument;
			public readonly IExpression callable;

			public Call(Map obj) {
				
				Map expression=(Map)obj["call"];
				this.callable=(IExpression)((Map)expression["function"]).Compile();
				this.argument=(IExpression)((Map)expression["argument"]).Compile();
			}
			public object Evaluate(object current) {
				object arg=argument.Evaluate(current);
				ICallable obj=(ICallable)callable.Evaluate(current);
				Interpreter.arguments.Add(arg);
				object result=obj.Call();
				Interpreter.arguments.Remove(arg);
				return result;
			}

		}
		public class Delayed: IExpression {
			public readonly Map obj;
			public Delayed(Map obj) {
				this.obj=(Map)obj["delayed"];
				this.obj.Compile();
			}
			public object Evaluate(object current) {
				return obj;
			}
		}
		public class Program: IExpression {
			public readonly ArrayList statements=new ArrayList();


			public Program(Map obj) {
				
				foreach(Map map in ((Map)obj["program"]).IntKeyValues) {
					this.statements.Add(map.Compile());
				}
			}
			public object Evaluate(object parent) {
				Map local=new Map();
				local.Parent=(IKeyValue)parent;
				return Evaluate(parent,local,false);
			}
			public object Evaluate(object caller,Map existing,bool isInFunction) {
				object result=existing;
				bool callerIsMap=false;
				if(caller is IKeyValue) { //hack!!!
					callerIsMap=true;
				}
				if(callerIsMap) {
					Interpreter.callers.Add(caller);
				}

				foreach(Statement statement in statements) {
					statement.Realize(ref result,isInFunction);
				}
				if(callerIsMap) {
					Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
				}
				return result;
			}
		}
		public abstract class ToNetConversion 		
		{
			public Type source;
			public Type target;
			public abstract object Convert(object obj);
		}
		public abstract class ToMetaConversion  
		{
			public Type source;
			public abstract object Convert(object obj);
		}
		public interface ILiteralRecognition {
			object Recognize(string text);
		}
		public class Literal: IExpression {
			public readonly string text;
			public object cached=null;

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
		public class BreakException:ApplicationException {
			public readonly object obj;

			public BreakException(object obj) {
				this.obj=obj;
			}
		}
		public class Select: IExpression {
			public readonly ArrayList expressions=new ArrayList();
			public ArrayList parents=new ArrayList();

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
				if(keys[0].Equals("result")) {
					int asdf=0;
				}
				object preselection=Preselect(current,keys,true,true);
				return preselection;
			}
			public object Preselect(object current,ArrayList keys,bool isSearch,bool isSelectLastKey) {
				object selected=current;
				int i=0;
				if(keys[0].Equals("staticEventChanged")) {
					int asdf=0;
				}
				if(keys[0].Equals("result")) {
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
					selected=Interpreter.callers[Interpreter.callers.Count-numCallers-1];
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
				else if(isSearch) {
					while(!((IKeyValue)selected).ContainsKey(keys[0])) {
						selected=((IKeyValue)selected).Parent;
					}
				}
				int count=keys.Count-1;
				if(isSelectLastKey) {
					count++;
				}
				for(;i<count;i++) {
					if(keys[i].Equals("break")) {
						throw new BreakException(selected);
					}	
					if(selected is IKeyValue) {
						selected=((IKeyValue)selected)[keys[i]];
					}
					else {
						selected=new NetObject(selected)[keys[i]];
					}

				}
				return selected;
			}
			public void Assign(ref object current,object val,bool isInFunction) {
				ArrayList keys=new ArrayList();
				foreach(IExpression expression in expressions) {
					keys.Add(expression.Evaluate((Map)current));
				}
				if(keys[0].Equals("testClass")) {
					int asdf=0;
				}
				if(keys.Count==3 && keys[2].Equals("a")) {
					int asf=0;
				}
				if(keys.Count==1 && keys[0].Equals("result")) {
					if(val is IKeyValue) {
						current=((IKeyValue)val).Clone();
						Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
						Interpreter.callers.Add(current);
					}
					else {
						current=val;
						Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
						Interpreter.callers.Add(current);
					}
				}
				else {
					object selected=Preselect(current,keys,isInFunction,false);
					IKeyValue keyValue;
					if(selected is IKeyValue) {
						keyValue=(IKeyValue)selected;
					}
					else {
						keyValue=new NetObject(selected);
					}
					keyValue[keys[keys.Count-1]]=val;
				}
			}
			private object GetValue(object obj,object key) {
				IKeyValue map;
				if(obj is IKeyValue) { 
					map=(IKeyValue)obj;
				}
				else {
					map=(new NetObject(obj));
				}
				if(key.Equals("break")) {
					throw new BreakException(map);
				}
				else {
					return map[key];
				}
			}
		}
		public class Interpreter  {
			public static ArrayList callers=new ArrayList();
			public static ArrayList arguments=new ArrayList();
			public static Hashtable netConversion=new Hashtable();
			public static Hashtable metaConversion=new Hashtable();
			
			public static object Arg {
				get {
					return arguments[arguments.Count-1];
				}
			}


				public static ILiteralRecognition[] Interceptions {
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

			public static void AddToNetConversion(ToNetConversion conversion) {
				if(!netConversion.ContainsKey(conversion.target)) {
					netConversion[conversion.target]=new Hashtable();
				}
				((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
			}
			public static void AddToMetaConversion(ToMetaConversion conversion) {
				metaConversion[conversion.source]=conversion;
			}
			public static Map String(string symbol) {
				Map map=new Map();
				foreach(char character in symbol) {
					map[new Integer(map.Count+1)]=new Integer((int)character);
				}
				return map;
			}
			public static string String(Map symbol) {
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
			public static ArrayList cashedLiterals=new ArrayList();

			public static void AddInterception(ILiteralRecognition i)  {
				interception.Add(i);
				foreach(Literal createLiteral in cashedLiterals) {
					createLiteral.cached=null;
				}
				cashedLiterals.Clear();
			}
			public static object Run(string path,Map argument) {
				return Run(new StreamReader(path),argument);
			}

			public static object Run(TextReader reader,Map argument) {
				ArrayList parents=new ArrayList();
				Map existing=new Map();
				existing["meta"]=new NetClass(typeof(Interpreter));
				existing["assemblies"]=new Assemblies();
				existing["files"]=new Files();
				foreach(MethodInfo method in typeof(Functions).GetMethods(
					BindingFlags.Public|BindingFlags.Static)) {
					existing[method.Name]=new NetMethod(method.Name,null,typeof(Functions));
				}
				Interpreter.arguments.Add(argument);
				object result=Mapify(reader).Call(existing);
				Interpreter.arguments.Remove(argument);
				return result;
			}
			public static AST Parse(TextReader stream)  {
				MetaANTLRParser parser=new Meta.Parser.MetaANTLRParser(
					new IndentParser(new MetaLexer(stream)));
				parser.map();
				return parser.getAST();
			}
			public static Map Mapify(TextReader input) {
				return (new MetaTreeParser()).map(Parse(input));
			}
			static Interpreter() {
				foreach(Type type in typeof(LiteralRecognitions).GetNestedTypes())
				{
					AddInterception((ILiteralRecognition)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(ToMetaConversions).GetNestedTypes())
				{
					AddToMetaConversion((ToMetaConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(ToNetConversions).GetNestedTypes())
				{
					AddToNetConversion((ToNetConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
			}
			public static IKeyValue MergeTwo(IKeyValue first,IKeyValue second) {
				Map map=new Map();
				map[new Integer(1)]=first;
				map[new Integer(2)]=second;
				return Merge(map);
			}
			[MetaMethod("")]
			public static IKeyValue Merge(Map maps) {
				Map result=new Map();
				foreach(DictionaryEntry i in maps) {
					foreach(DictionaryEntry j in (IKeyValue)i.Value) {
						if(j.Value is IKeyValue && result.ContainsKey(j.Key) && result[j.Key] is IKeyValue) {
							result[j.Key]=MergeTwo((IKeyValue)result[j.Key],(IKeyValue)j.Value);
						}
						else {
							result[j.Key]=j.Value;
						}
					}
				}
				return result;
			}
			[MetaMethod("()")]
			public static Map LoadAssembly(Map map) {
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
					foreach(Type type in assembly.GetTypes())  {
						if(type.DeclaringType==null)  {
							Map position=root;
							ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
							subPaths.RemoveAt(subPaths.Count-1);
							foreach(string subPath in subPaths)  {
								if(!position.ContainsKey(subPath))  {
									position[subPath]=new Map();
								}
								position=(Map)position[subPath];
							}
							position[type.Name]=new NetClass(type);
						}
					}
				}
				return root;
			}

			public static string GetMethodName(MethodBase methodInfo) {
				int counter=0;
				string text="";
				if(methodInfo is MethodInfo) {
					text+=((MethodInfo)methodInfo).ReturnType.Name+" "+methodInfo.Name;
				}
				else {
					text+=methodInfo.ReflectedType.Name;
				}
				text+=" (";
				foreach(ParameterInfo parameter in methodInfo.GetParameters()) {
					text+=parameter.ParameterType.Name+" "+parameter.Name;
					if(counter!=methodInfo.GetParameters().Length-1) {
						text+=",";
					}
					counter++;
				}
				text+=")";
				return text;
			}
			public static string GetDoc(MemberInfo memberInfo, bool showParams) {
				XmlNode comment=GetComments(memberInfo);
				string text="";
				string summary="";
				ArrayList parameters=new ArrayList();
				if(comment==null || comment.ChildNodes==null) {
					return "";
				}
				foreach(XmlNode node in comment.ChildNodes) {
					switch(node.Name) {
						case "summary":
							summary=node.InnerXml;
							break;
						case "param":
							parameters.Add(node);
							break;
						default:
							break;
					}
				}
				text+=summary+"\n";
				if(showParams) {
					//text+="\nparameters: \n";
					foreach(XmlNode node in parameters) {
						text+=node.Attributes["name"].Value+": "+node.InnerXml;
					}
				}
				return text.Replace("<para>","").Replace("</para>","").Replace("<see cref=\"","")
					.Replace("\" />","").Replace("T:","").Replace("F:","").Replace("P:","")
					.Replace("M:","").Replace("E:","");
			}
//			public static string GetDoc(MemberInfo memberInfo) {
//				XmlNode comment=GetComments(memberInfo);
//				string text="";
//				string summary="";
//				ArrayList parameters=new ArrayList();
//				if(comment==null || comment.ChildNodes==null) {
//					return "";
//				}
//				foreach(XmlNode node in comment.ChildNodes) {
//					switch(node.Name) {
//						case "summary":
//							summary=node.InnerXml;
//							break;
//						case "param":
//							parameters.Add(node);
//							break;
//						default:
//							break;
//					}
//				}
//				text+=summary+"\n\nparameters: \n";
//				foreach(XmlNode node in parameters) {
//					text+=node.Attributes["name"].Value+": "+node.InnerXml;
//				}
//				return text.Replace("<para>","").Replace("</para>","").Replace("<see cref=\"","")
//					.Replace("\" />","").Replace("T:","").Replace("F:","").Replace("P:","")
//					.Replace("M:","").Replace("E:","");
//			}
			public static string CreateParamsDescription(ParameterInfo[] parameters) {
				string text="";
				if(parameters.Length>0) {
					text+="(";
					foreach(ParameterInfo parameter in parameters) {
						text+=parameter.ParameterType.FullName+",";
					}
					text=text.Remove(text.Length-1,1);
					text+=")";
				}
				return text;
			}
			private static Hashtable comments=new Hashtable();
			public static XmlDocument LoadAssemblyComments(Assembly assembly) {
				if(!comments.ContainsKey(assembly)) {
					string dllPath=assembly.Location;
					string dllName=Path.GetFileNameWithoutExtension(dllPath);
					string dllDirectory=Path.GetDirectoryName(dllPath);
				
					string assemblyDirFile=Path.Combine(dllDirectory,dllName+".xml");
					string runtimeDirFile=Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(),dllName+".xml");
					string fileName;
					if(File.Exists(assemblyDirFile)) {
						fileName=assemblyDirFile;
					}
					else if(File.Exists(runtimeDirFile)) {
						fileName=runtimeDirFile;
					}
					else {
						return null;
					}
				
					XmlDocument xml=new XmlDocument();
					xml.Load(fileName);
					comments[assembly]=xml;
				}
				return (XmlDocument)comments[assembly];
			}
			public static XmlNode GetComments(MemberInfo mi) {
				Type declType = (mi is Type) ? ((Type)mi) : mi.DeclaringType;
				XmlDocument doc = LoadAssemblyComments(declType.Assembly);
				if (doc == null) return null;
				string xpath;

				// Handle nested classes
				string typeName = declType.FullName.Replace("+", ".");

				// Based on the member type, get the correct xpath query
				switch(mi.MemberType) {                    
					case MemberTypes.NestedType:
					case MemberTypes.TypeInfo:
						xpath = "//member[@name='T:" + typeName + "']";
						break;

					case MemberTypes.Constructor:
						xpath = "//member[@name='M:" + typeName + "." +
							"#ctor" + CreateParamsDescription(
							((ConstructorInfo)mi).GetParameters()) + "']";
						break;

					case MemberTypes.Method:
						xpath = "//member[@name='M:" + typeName + "." + 
							mi.Name + CreateParamsDescription(
							((MethodInfo)mi).GetParameters());
						if (mi.Name == "op_Implicit" || mi.Name == "op_Explicit") {
							xpath += "~{" + 
								((MethodInfo)mi).ReturnType.FullName + "}";
						}
						xpath += "']";
						break;

					case MemberTypes.Property:
						xpath = "//member[@name='P:" + typeName + "." + 
							mi.Name + CreateParamsDescription(
							((PropertyInfo)mi).GetIndexParameters()) + "']";
						break;

					case MemberTypes.Field:
						xpath = "//member[@name='F:" + typeName + "." + mi.Name + "']";
						break;

					case MemberTypes.Event:
						xpath = "//member[@name='E:" + typeName + "." + mi.Name + "']";
						break;

						// Unknown member type, nothing to do
					default: 
						return null;
				}

				// Get the node from the document
				return doc.SelectSingleNode(xpath);
			}
		}
	}
	namespace Types  {
		public abstract class Command:  ICallable {
			private IKeyValue parent;
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			public abstract object Call();
		}
		public interface IMetaType {
			IKeyValue Parent {
				get;
				set;
			}
		}
		public abstract class Callable {
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
		public interface ICallable: IMetaType{
			object Call();
		}
		public interface IKeyValue: IMetaType,IEnumerable {
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

		public class Map: IKeyValue, ICallable, IEnumerable {
			private IKeyValue parent;
			public IKeyValue Parent {
				get {
					return parent;
				}
				set {
					parent=value;
				}
			}
			public int numberAutokeys=0;
			private ArrayList keys;
			public HybridDictionary table;
			private object compiled;


			public int Count {
				get {
					return table.Count;
				}
			}
			public ArrayList IntKeyValues {
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
					return table[key];
				}
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
			public object Call() {
				Map local=new Map();
				local.Parent=this;
				return Call(local);
			}
			public object Call(Map existing)  {
				IExpression callable=(IExpression)Compile();
				object result;
				if(callable is Program) {
					result=((Program)callable).Evaluate(this,existing,true);
				}
				else {
					result=callable.Evaluate(this);
				}
				return result;
			}
			public void StopSharing() {
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
				}
				return compiled;
			}
			public bool ContainsKey(object key)  {
				return table.Contains(key);
			}
			public override bool Equals(object obj) {
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
			public override int GetHashCode()  {
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
		public class MetaMethodAttribute:Attribute {
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
			private static Hashtable cashedDoc;
			public string GetDocumentation(bool showParams) {
//				if(cashedDoc==null) {
					if(savedMethod!=null) {
						return Interpreter.GetDoc(savedMethod,showParams);
					}
					else {
						return Interpreter.GetDoc(methods[0],showParams)+"(+"+methods.Length.ToString()+"overloads)";
					}
				//}
//				return doc;
			}
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
			private MethodBase savedMethod;
			private MethodBase[] methods;
			


			public object Call() {
//				if(this.name.Equals("Invoke")) {
//					int asdf=0;
//				}
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
				try {
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
					foreach(MethodBase method in methods) {
						ArrayList args=new ArrayList();
						int counter=0;
						bool argumentsMatched=true;
						if(list.Count!=method.GetParameters().Length) {
							argumentsMatched=false;
						}
						else {
							foreach(ParameterInfo parameter in method.GetParameters()) {
								bool matched=false;
								if(list[counter].GetType().Equals(parameter.ParameterType)
									||list[counter].GetType().IsSubclassOf(parameter.ParameterType)) {
									args.Add(list[counter]);
									matched=true;
								}
								else {
									Hashtable toDotNet=(Hashtable)
										Interpreter.netConversion[parameter.ParameterType];
									if(toDotNet!=null) {
										ToNetConversion conversion=(ToNetConversion)toDotNet[list[counter].GetType()];
										if(conversion!=null) {
											try {
												args.Add(conversion.Convert(list[counter]));
												matched=true;
											}
											catch {
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
						throw new ApplicationException(name+" could not be invoked,"+
							"the parameters do not match");
					}
					else {
						return result;
					}

				}
				catch(Exception e) {
					Exception b=e;
					while(!(b is BreakException) && b.InnerException!=null) {
						b=b.InnerException;
					}
					if(b is BreakException) {
						throw b;
					}
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
			public string Documentation {
				get {
					return Interpreter.GetDoc(type,true);
				}
			}
			[IgnoreMember]
			public NetMethod constructor;
			public object Call() {
				return constructor.Call();
			}
			public NetClass(Type type):base(null,type) {
				this.constructor=new NetMethod(this.type);
			}
		}
		public class NetObject: NetContainer, IKeyValue {
			public string GetDocumentation (bool showParams){
				string text="";
				foreach(MemberInfo memberInfo in obj.GetType().GetMembers()) {
					if(memberInfo is MethodInfo) {
						text+=Interpreter.GetMethodName((MethodInfo)memberInfo)+"\n";
					}
					text+=Interpreter.GetDoc(memberInfo,showParams);

				}
				return text;
			}
			public NetObject(object obj):base(obj,obj.GetType()) {
			}
			public override string ToString() {
				return obj.ToString();
			}
		}
		public abstract class NetContainer: IKeyValue, IEnumerable ,ICustomSerialization {
			public string CustomSerialization() {
				return "";
			}
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
			[IgnoreMember]
			public object obj;
			[IgnoreMember]
			public Type type;
			[IgnoreMember]
			public ArrayList Keys {
				get {
					return new ArrayList(Table.Keys);
				}
			}
			
			public NetContainer(object obj,Type type) {
				this.obj=obj;
				this.type=type;
				
			}
			[IgnoreMember]
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
					if(key.Equals("staticEvent")) {
						int asdf=0;
					}
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
//								return new NetMethod(((EventInfo)type.GetMember((string)text,
//									MemberTypes.Event,BindingFlags.Public
//											 |BindingFlags.Static|BindingFlags.Instance
//											 )[0]).GetRaiseMethod().Name,obj,type);
							}
						}
						catch(Exception e) {
						}
					}
					NetMethod indexer=new NetMethod("get_Item",obj,type);
					try {
						Map arguments=new Map();
						arguments[new Integer(1)]=key;
						Interpreter.arguments.Add(arguments);
						object result=indexer.Call();
						Interpreter.arguments.Remove(arguments);
						return result;
					}
					catch(Exception e) {
						return null;
					}
				}
				set {		
					if(key.Equals("staticEvent")) {
						int asdf=0;
					}
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
								SetEvent((string)text,(Map)value);
								return;
							}
					}
					try {
						NetMethod indexer=new NetMethod("set_Item",obj,type);
						Map arguments=new Map();
						arguments[new Integer(1)]=key;
						arguments[new Integer(2)]=value;
						Interpreter.arguments.Add(arguments);
						indexer.Call();
						Interpreter.arguments.Remove(arguments);
					}
					catch(Exception e) {
						throw new ApplicationException(key.ToString()+" not found in "+ToString());
					}
				}
			}
			// fix .dll dependencies
			public void SetEvent(string name,Map code) {
				code.Parent=(IKeyValue)Interpreter.callers[Interpreter.callers.Count-1];
				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
				ICodeCompiler compiler=codeProvider.CreateCompiler();
				FieldInfo field=type.GetField(name,BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);

				EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
					BindingFlags.Static|BindingFlags.Instance);
				MethodInfo method=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);

				string source="using System;using Meta.Types;using Meta.Execution;";
				source+="public class EventHandlerContainer{public "+method.ReturnType.Name+" EventHandlerMethod";
				int counter=1;
				string argumentList="(";
				string argumentAdding="Map arg=new Map();";
				// here bug
				foreach(ParameterInfo parameter in method.GetParameters()) {
					argumentList+=parameter.ParameterType.Name+" arg"+counter;
					argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
					if(counter<=parameter.Position) {
						argumentList+=",";
					}
					else {
						argumentList+=")";
					}
					counter++;
				}
				source+=argumentList+"{";
				source+=argumentAdding;
				source+="Interpreter.arguments.Add(arg);object result=callable.Call();Interpreter.arguments.Remove(arg);";
				source+="return ("+method.ReturnType.FullName+")";
				source+="Interpreter.ConvertToNet(result,typeof("+method.ReturnType.FullName+"));}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				CompilerParameters options=new CompilerParameters(new string[] {"mscorlib.dll","System.dll","Meta.dll"});
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[]
					{code});
				type.GetEvent(name).AddEventHandler(obj,Delegate.CreateDelegate(type.GetEvent(name).EventHandlerType,
					container,"EventHandlerMethod"));
			}
			public bool ContainsKey(object key)  {
				try  {
					if(key.Equals("staticEvent")) {
						int asdf=0;
					}
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
						if(property.Name!="Item" && property.Name!="Chars") { table[property.Name]=property.GetValue(obj,new object[]{});
						}
					}
					int counter=1;
					if(obj!=null && obj is IEnumerable)  {
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

			protected void AddIndentationTokens(int newIndent)  {
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
		}
	}

	namespace TestingFramework {
		public abstract class MetaTest {
			public abstract object GetTestResult();
		}
		public class CustomSerializationException:Exception {
		}
		public interface ICustomSerialization {
			string CustomSerialization();
		}
		public class TestExecuter {	
			public TestExecuter(Type classType,string path) {
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
				if(obj is ArrayList) {
					int asdf=0;
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
						if(entry is ArrayList) {
							int asdf=0;
						}
						if(count==42) {
						}
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
						if(memberInfo.Name.Equals("callers")) {
							int asdf=0;
						}
						if(memberInfo.Name!="Item") {
							if(memberInfo.Name.Equals("cashedLiterals")) {
								int asdf=0;
							}
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
								if(val is ArrayList) {
									int asdf=0;
								}
								text=text + ":\n" + Serialize(val,indentation + "  ",methodNames);
							}
						}
					}
					return text;
				}
			}
		}

		internal class MemberSorter:IComparer {
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
