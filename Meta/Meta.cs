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

namespace Meta {
	namespace Execution {
		public interface IExpression {
			object Evaluate(object current);
			bool IdenticalTo(object obj);
			void ReplaceWith(IExpression replace);
		}
		public class Statement {
			public void Realize(ref object scope) {
				key.Assign(ref scope,this.val.Evaluate(scope));
			}
			public void ReplaceWith(Statement statement) {
				if(!key.IdenticalTo(statement.key)) {
					key=(Select)Interpreter.ReplaceExpression(key,statement.key);
				}
				if(!val.IdenticalTo(statement.val)) {
					val=(IExpression)Interpreter.ReplaceExpression(val,statement.val);
				}
			}
			public bool IdenticalTo(object obj) {
				return obj is Statement && ((Statement)obj).key.IdenticalTo(key) && ((Statement)obj).val.IdenticalTo(val);
			}
			public Statement(Map obj) {
				this.key=(Select)((Map)obj["key"]).Compile();
				this.val=(IExpression)((Map)obj["value"]).Compile();
			}
			public Select key;
			public IExpression val;
		}
		public class Call: IExpression {	
			public object Evaluate(object current) {
				object arg=argument.Evaluate(current);
				ICallable toBeCalled=(ICallable)callable.Evaluate(current);
				object result=toBeCalled.Call((Map)current,(IMap)arg);
				return result;
			}
			public void ReplaceWith(IExpression replace) { // check
				if(!this.IdenticalTo(replace)) {
					Call call=(Call)replace;
					callable=(IExpression)Interpreter.ReplaceExpression(callable,call.callable);
					argument=(IExpression)Interpreter.ReplaceExpression(argument,call.argument);
				}
			}
			public bool IdenticalTo(object obj) { // check
				return obj is Call && ((Call)obj).callable.IdenticalTo(callable) &&
					((Call)obj).argument.IdenticalTo(argument);
			}
			public Call(Map obj) {
				Map expression=(Map)obj["call"];
				this.callable=(IExpression)((Map)expression["function"]).Compile();
				this.argument=(IExpression)((Map)expression["argument"]).Compile();
			}
			public IExpression argument;
			public IExpression callable;
		}
		public class Delayed: IExpression {
			public object Evaluate(object current) { // current necessary?
				return delayedExpression;
			}
			public bool IdenticalTo(object obj) { // check
				return obj is Delayed && delayedExpression.Equals(((Delayed)obj).delayedExpression);
			}
			public void ReplaceWith(IExpression replace) { // check
				if(!this.IdenticalTo(replace)) {
					Delayed delayed=(Delayed)replace;
					delayed.delayedExpression.Compile();
					if(delayedExpression.compiled!=null) {
						delayedExpression.compiled=Interpreter.ReplaceExpression(delayedExpression.compiled,delayed.delayedExpression.compiled);
						delayed.delayedExpression.compiled=delayedExpression.compiled;
					}
					delayedExpression=delayed.delayedExpression;
				}
			}
			public Delayed(Map code) {
				this.delayedExpression=(Map)code["delayed"];
				this.delayedExpression.Compile();
			}
			public Map delayedExpression;
		}
		public class Program: IExpression {
			public object Evaluate(object parent) { // refactor
				Map local=new Map();
				local.Parent=(IMap)parent;
				return Evaluate(parent,local);
			}
			public object Evaluate(object caller,IMap existing) { // refactor, why does it differ,why the same name?
				object result=existing;
				bool callerIsMap=false;
				if(caller!=null) {
					if(caller is IMap) {
						callerIsMap=true;
					}
					if(callerIsMap) {
						Interpreter.callers.Add(caller);
					}
				}
				for(int i=0;i<statements.Count;i++) {
					((Statement)statements[i]).Realize(ref result);
				}
				if(caller!=null) {
					if(callerIsMap) {
						Interpreter.callers.RemoveAt(Interpreter.callers.Count-1); // add local map to callers, even when not caller (always)
					}
				}
				return result;
			}
			public bool IdenticalTo(object obj) { // check
				if(obj is Program) {
					Program program=(Program)obj;
					if(program.statements.Count==statements.Count) {
						for(int i=0;i<statements.Count;i++) {
							if(!((Statement)statements[i]).IdenticalTo(program.statements[i])) {
								return false;
							}
						}
						return true;
					}
				}
				return false;
			}
			public void ReplaceWith(IExpression replace) { // check
				if(!IdenticalTo(replace)) {
					Program program=(Program)replace;
					int i=0;
					for(;i<program.statements.Count && i<statements.Count;i++)  {
						if(!((Statement)statements[i]).IdenticalTo((Statement)program.statements[i])) {
							statements[i]=Interpreter.ReplaceExpression(statements[i],
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
			public readonly ArrayList statements=new ArrayList();
		}
		public class Literal: IExpression {
			public object Evaluate(object current) {
				if(cached==null) {
					cached=Interpreter.RecognizeLiteral(text);
				}
				return cached;
			}
			public bool IdenticalTo(object obj) { // check
				return obj is Literal && text==((Literal)obj).text;
			}
			public void ReplaceWith(IExpression replace) { // check
				Literal literal=(Literal)replace;
				text=literal.text;
				cached=literal.cached;
			}
			public Literal(Map code) {
				this.text=(string)code["literal"];
			}
			public string text;
			public object cached=null;
		}
		public class Select: IExpression { 
			public object Evaluate(object current) {
				ArrayList keys=new ArrayList();
				foreach(IExpression expression in expressions) {
					keys.Add(expression.Evaluate(current));
				}
				object preselection=Preselect(current,keys,true,true);
				return preselection;
			}
			public void Assign(ref object current,object val) {
				ArrayList keys=new ArrayList();
				foreach(IExpression expression in expressions) {
					keys.Add(expression.Evaluate((IMap)current));
				}
				if(keys.Count==1 && keys[0].Equals("this")) {
					if(val is IMap) {
						((IMap)val).Parent=((IMap)current).Parent;
						current=((IMap)val).Clone();
						if(current is IMap) {
							Interpreter.callers.RemoveAt(Interpreter.callers.Count-1);
							Interpreter.callers.Add(current);
						}
					}
					else {
						current=val;
						if(current is IMap) {
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
			public object Preselect(object current,ArrayList keys,bool isRightSide,bool isSelectLastKey) {
				object selected=current;
				int i=0;
				if(keys[0].Equals("base")) {
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
							selected=((IMap)selected).Parent;
							i++;
						}
						else {
							break;
						}
					}
				}
				else if(keys[0].Equals("arg")) {
					selected=Interpreter.arguments[Interpreter.arguments.Count-1]; //change
					i++;
				}				
				else if(keys[0].Equals("search")||isRightSide) {
					if(keys[0].Equals("search")) {
						i++;
					}
					while(selected!=null && !((IKeyValue)selected).ContainsKey(keys[i])) {
						selected=((IMap)selected).Parent;
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
						if(selected is IKeyValue) {
							Interpreter.breakMethod((IKeyValue)selected);
						}
						else {
							Interpreter.breakMethod(new NetObject(selected,selected.GetType()));
						}
						Thread.CurrentThread.Suspend();
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
			public bool IdenticalTo(object obj) { // check
				if(obj is Select) {
					Select select=(Select)obj;
					if(expressions.Count==select.expressions.Count)  {
						for(int i=0;i<expressions.Count;i++) {
							if(!((IExpression)expressions[i]).IdenticalTo(select.expressions[i])) {
								return false;
							}
						}
						return true;
					}
				}
				return false;
			}
			public void ReplaceWith(IExpression replace) { // check
				if(!IdenticalTo(replace)) {
					Select select=(Select)replace;
					int i=0;
					for(;i<expressions.Count && i<select.expressions.Count;i++) {
						expressions[i]=Interpreter.ReplaceExpression(
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
			public readonly ArrayList expressions=new ArrayList();
			public ArrayList parents=new ArrayList();
		}
		public delegate void BreakMethodDelegate(IKeyValue obj);//rename
		public class Interpreter  {
			public static IKeyValue Merge(params IKeyValue[] maps) {
				return MergeCollection(maps);
			}
			public static IKeyValue MergeCollection(ICollection maps) { //rename
				Map result=new Map();
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
			public static object RecognizeLiteral(string text) {
				for(int i=literalRecognitions.Count-1;i>=0;i--) {
					object recognized=((RecognizeLiteral)literalRecognitions[i]).Recognize(text);
					if(recognized!=null) {
						return recognized;
					}
				}
				return null;
			}
			public static object ReplaceExpression(object original,object replacement) { //not great
				if(original.GetType().Equals(replacement.GetType())) {
					if(original is Statement) {
						((Statement)original).ReplaceWith((Statement)replacement);
					}
					else {
						((IExpression)original).ReplaceWith((IExpression)replacement);
					}
					return original;
				}
				else {
					return replacement;
				}
			}
			public static object ConvertDotNetObjectToMetaObject(object obj) { 
				if(obj==null) {
					return null;
				}
				ConvertDotNetToMeta conversion=(ConvertDotNetToMeta)metaConversion[obj.GetType()];
				if(conversion==null) {
					return obj;
				}
				else {
					return conversion.Convert(obj);
				}
			}
			public static object ConvertMetaObjectToDotNetObject(object obj,Type targetType) {
				try {
					ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)((Hashtable)
						Interpreter.netConversion[obj.GetType()])[targetType];
					return conversion.Convert(obj);
				}
				catch {
					return obj;
				}
			}
			public static object Run(TextReader reader,IMap argument) {
//				Interpreter.arguments.Add(argument);
				Map lastProgram=CompileToMap(reader);
				lastProgram.Parent=Library.library;    // move argument adding somewhere else
				object result=lastProgram.Call(new Map(),argument);
//				Interpreter.arguments.Remove(argument);
				return result;
			}
//			public static object Run(TextReader reader,IMap argument) {
//				Interpreter.arguments.Add(argument);
//				Map lastProgram=CompileToMap(reader);
//				lastProgram.Parent=Library.library;    // move argument adding somewhere else
//				object result=lastProgram.Call(new Map(),new Map());
//				Interpreter.arguments.Remove(argument);
//				return result;
//			}
			public static AST ParseToAst(TextReader stream)  {
				MetaANTLRParser parser=new Meta.Parser.MetaANTLRParser(
					new AddIndentationTokensToStream(new MetaLexer(stream)));
				parser.map();
				return parser.getAST();
			}
			public static Map CompileToMap(TextReader input) {
				return (new MetaTreeParser()).map(ParseToAst(input));
			}
			public static object Arg {	//not needed very often, combine with above,, put calling into Interpreter somehow
				get {
					return arguments[arguments.Count-1];
				}
			}
			static Interpreter() {
				Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
				metaInstallationPath=Directory.GetParent(metaAssembly.Location).Parent.Parent.Parent.FullName; 
				foreach(Type type in typeof(LiteralRecognitions).GetNestedTypes()) {
					literalRecognitions.Add((RecognizeLiteral)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(DotNetToMetaConversions).GetNestedTypes()) {
					ConvertDotNetToMeta conversion=((ConvertDotNetToMeta)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
					metaConversion[conversion.source]=conversion;
				}
				foreach(Type type in typeof(MetaToDotNetConversions).GetNestedTypes()) {
					ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)type.GetConstructor(new Type[]{}).Invoke(new object[]{});
					if(!netConversion.ContainsKey(conversion.target)) {
						netConversion[conversion.target]=new Hashtable();
					}
					((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
				}
			}
			public static string metaInstallationPath;
			public static BreakMethodDelegate breakMethod;
			public static ArrayList callers=new ArrayList();
			public static ArrayList arguments=new ArrayList();
			public static Hashtable netConversion=new Hashtable(); //move conversions into interpreter?
			public static Hashtable metaConversion=new Hashtable();
			public static ArrayList compiledMaps=new ArrayList(); 
			public static ArrayList loadedAssemblies=new ArrayList();

			private static ArrayList literalRecognitions=new ArrayList();
		}
	}
	namespace Types  {
		public class UnloadedMetaLibrary {
			public object Load() {
				return Interpreter.Run(new StreamReader(path),new Map()); //check that
			}
			public UnloadedMetaLibrary(string path) {
				this.path=path;
			}
			string path;
		}
		public class Library: IKeyValue,IMap {
			public object this[object key] {
				get {
					if(cash.ContainsKey(key)) {
						if(key.Equals("Microsoft")) {
							if(!microsoftLoaded) {
								foreach(Assembly assembly in microsoftAssemblies) {
									cash=(Map)Interpreter.Merge(cash,LoadAssembly(assembly));
								}
								microsoftLoaded=true;
							}
						}
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
					throw new ApplicationException("Tried to set key "+key.ToString()+" in library.");
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
					throw new ApplicationException("Tried to set parent of library.");
				}
			}
			public IEnumerator GetEnumerator() {
				foreach(DictionaryEntry entry in cash) { // to make sure everything is loaded
					object o=cash[entry.Key];				  // not good, should make own enumerator
				}
				return cash.GetEnumerator();
			}
			public static Map LoadAssembly(Assembly assembly) { //refactor
				Map root=new Map();
				foreach(Type type in assembly.GetExportedTypes())  {
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
				Interpreter.loadedAssemblies.Add(assembly.Location);
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
				libraryPath=Path.Combine(Interpreter.metaInstallationPath,"library");

				IAssemblyEnum e=AssemblyCache.CreateGACEnum();
				IAssemblyName an; 
				AssemblyName name;
				cash=(Map)Interpreter.Merge(cash,(Map)LoadAssembly(Assembly.LoadWithPartialName("mscorlib")));
				while (AssemblyCache.GetNextAssembly(e, out an) == 0) { 
					name=GetAssemblyName(an);
					Assembly assembly=Assembly.LoadWithPartialName(name.Name);
					if(name.Name.StartsWith("Microsoft.")) {
						microsoftAssemblies.Add(assembly);
					}
					else {
						cash=(Map)Interpreter.Merge(cash,LoadAssembly(assembly));
					}
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.dll")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					cash=(Map)Interpreter.Merge(cash,LoadAssembly(assembly));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.exe")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					cash=(Map)Interpreter.Merge(cash,LoadAssembly(assembly));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.meta")) {
					cash[Path.GetFileNameWithoutExtension(fileName)]=new UnloadedMetaLibrary(fileName);
				}
			}
			ArrayList microsoftAssemblies=new ArrayList();
			bool microsoftLoaded=false;
			public static Library library=new Library();
			private Map cash=new Map();
			public static string libraryPath="library"; 
			//remove
		}
		public interface ICallable {
			object Call(Map caller,IMap argument);
		}
		public interface IMap: IKeyValue {
			IMap Parent {
				get;
				set;
			}
			ArrayList IntKeyValues { // rename
				get;
			}
			IMap Clone();
		}
		public interface IKeyValue: IEnumerable { // not used consequently instead of map
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
		public class Map: IKeyValue, IMap, ICallable, IEnumerable { // could use Callable
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
				get {
					return table[key];
				}
				set { //refactor some more
					if(value!=null) {
						isHashCashed=false;
						object val=value is IMap? ((IMap)value).Clone(): value;
						if(value is IMap) {
							((IMap)val).Parent=this;
						}
						if(!table.Contains(key)) {
							keys.Add(key);
						}
						table[key]=val;
					}
				}
			}
			public object Call(Map caller,IMap argument) {
				((IMap)argument).Parent=this.Parent;
//				((IMap)Interpreter.Arg).Parent=this.Parent;
				IExpression callable=(IExpression)Compile();
				object result;
				Interpreter.arguments.Add(argument);
				if(callable is Program) { // somehow wrong
					result=((Program)callable).Evaluate(caller,(IMap)Interpreter.Arg);
				}
				else {
					result=callable.Evaluate((IMap)Interpreter.Arg);
				}
				Interpreter.arguments.Remove(argument);
				return result;
			}
//			public object Call(Map caller,IMap argument) {
//				((IMap)Interpreter.Arg).Parent=this.Parent;
//				IExpression callable=(IExpression)Compile();
//				object result;
//				if(callable is Program) { // somehow wrong
//					result=((Program)callable).Evaluate(caller,(IMap)Interpreter.Arg);
//				}
//				else {
//					result=callable.Evaluate((IMap)Interpreter.Arg);
//				}
//				return result;
//			}
//			public object Call(Map caller) {
//				((IMap)Interpreter.Arg).Parent=this.Parent;
//				IExpression callable=(IExpression)Compile();
//				object result;
//				if(callable is Program) { // somehow wrong
//					result=((Program)callable).Evaluate(caller,(IMap)Interpreter.Arg);
//				}
//				else {
//					result=callable.Evaluate((IMap)Interpreter.Arg);
//				}
//				return result;
//			}
			public ArrayList Keys {
				get {
					return keys;
				}
			}
			public IMap Clone() {
				Map copy=new Map();
				foreach(object key in keys) {
					copy[key]=this[key];
				}
				copy.Parent=Parent;
				copy.compiled=compiled;
				return copy;
			}
			public object Compile()  { //don't return anything, keep it private
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
			private IMap parent;
			private ArrayList keys;
			private HybridDictionary table;
			public object compiled;
			private bool isHashCashed=false;
			private int hash;
		}
		public class MapEnumerator: IEnumerator { //make this useful for NetContainer, too?
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
		public delegate object NullDelegate();
		public class NetMethod: ICallable {
			public object Call(Map caller,IMap argument) {
				IMap arguments=argument;
				Interpreter.arguments.Add(argument);
//				IMap arguments=(IMap)Interpreter.Arg;
				ArrayList list;
				list=arguments.IntKeyValues;
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
								if(parameter.ParameterType.IsAssignableFrom(list[counter].GetType())) {
									args.Add(list[counter]);
									matched=true;
								}
								else {
									if(parameter.ParameterType.IsSubclassOf(typeof(Delegate))
										||parameter.ParameterType.Equals(typeof(Delegate))) {
										try {
											MethodInfo m=parameter.ParameterType.GetMethod("Invoke",BindingFlags.Instance
												|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
											Delegate del=CreateDelegate(parameter.ParameterType,m,(Map)list[counter]);
											args.Add(del);
											matched=true;
										}
										catch(Exception e){// ack
											int asdf=0;
										}
									}
									if(!matched && parameter.ParameterType.IsArray && list[counter] is IMap && ((Map)list[counter]).IntKeyValues.Count!=0) {// cheating
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
											Interpreter.netConversion[parameter.ParameterType]; // move this into Interpreter
										if(toDotNet!=null) {
											ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[list[counter].GetType()];
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
						if(argumentsMatched) { // horrible
							if(!executed) {
								if(method is ConstructorInfo) {
									result=((ConstructorInfo)method).Invoke(args.ToArray());
								}
								else {
									result=method.Invoke(target,args.ToArray());
								}
								executed=true;
							}
							else { //what here?
								//throw new ApplicationException("\nArguments match more than one overload of "+name);
							}
						}
					}
					if(!executed) {
						if(savedMethod is ConstructorInfo) {
							return 
								Interpreter.ConvertDotNetObjectToMetaObject(
								((ConstructorInfo)savedMethod).Invoke(new object[] {}));
						}
						else {
							return Interpreter.ConvertDotNetObjectToMetaObject(savedMethod.Invoke(target,new object[] {}));
						}

					}
					else {
						return Interpreter.ConvertDotNetObjectToMetaObject(result);
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
				finally {
					Interpreter.arguments.Remove(argument);
				}
			}
//			public object Call(Map caller,IMap argument) {
//				IMap arguments=(IMap)Interpreter.Arg;
//				ArrayList list;
//				list=arguments.IntKeyValues;
//				object result=null;
//				try {
//					ArrayList methods;
//					if(name=="") {
//						methods=new ArrayList(type.GetConstructors());
//					}
//					else {
//						methods=new ArrayList(type.GetMember(name,BindingFlags.Public|
//							BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
//					}
//					bool executed=false;
//					methods.Reverse();
//					foreach(MethodBase method in methods) {
//						ArrayList args=new ArrayList();
//						int counter=0;
//						bool argumentsMatched=true;
//						ParameterInfo[] parameters=method.GetParameters();
//						if(list.Count>parameters.Length && parameters.Length>0) {
//							Type lastParameter=parameters[parameters.Length-1].ParameterType;
//							if(lastParameter.IsArray || lastParameter.IsSubclassOf(typeof(Array))) {
//								Map lastArg=new Map();
//								ArrayList paramsArgs=list.GetRange(parameters.Length-1,list.Count-(parameters.Length-1));
//								for(int i=0;i<paramsArgs.Count;i++) {
//									lastArg[new Integer(i+1)]=paramsArgs[i];
//								}
//								list[parameters.Length-1]=lastArg;									
//								list.RemoveRange(parameters.Length,list.Count-parameters.Length);
//							}
//						}
//						if(list.Count!=parameters.Length) {
//							argumentsMatched=false;
//						}
//						else {
//							foreach(ParameterInfo parameter in method.GetParameters()) {
//								bool matched=false;
//								if(parameter.ParameterType.IsAssignableFrom(list[counter].GetType())) {
//									args.Add(list[counter]);
//									matched=true;
//								}
//								else {
//									if(parameter.ParameterType.IsSubclassOf(typeof(Delegate))
//										||parameter.ParameterType.Equals(typeof(Delegate))) {
//										try {
//											MethodInfo m=parameter.ParameterType.GetMethod("Invoke",BindingFlags.Instance
//												|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
//											Delegate del=CreateDelegate(parameter.ParameterType,m,(Map)list[counter]);
//											args.Add(del);
//											matched=true;
//										}
//										catch(Exception e){// ack
//											int asdf=0;
//										}
//									}
//									if(!matched && parameter.ParameterType.IsArray && list[counter] is IMap && ((Map)list[counter]).IntKeyValues.Count!=0) {// cheating
//										try {
//											Type elementType=parameter.ParameterType.GetElementType();
//											Map map=((Map)list[counter]);
//											ArrayList mapValues=map.IntKeyValues;
//											Array array=Array.CreateInstance(elementType,mapValues.Count);
//											for(int i=0;i<mapValues.Count;i++) {
//												array.SetValue(mapValues[i],i);
//											}
//											args.Add(array);
//											matched=true;										
//										}
//										catch {
//										}
//
//									}
//									if(!matched) {
//										Hashtable toDotNet=(Hashtable)
//											Interpreter.netConversion[parameter.ParameterType]; // move this into Interpreter
//										if(toDotNet!=null) {
//											ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[list[counter].GetType()];
//											if(conversion!=null) {
//												try {
//													args.Add(conversion.Convert(list[counter]));
//													matched=true;
//												}
//												catch (Exception e) {
//													int asdf=0;
//												}
//											}
//										}
//									}
//								}
//								if(!matched) {
//									argumentsMatched=false;
//									break;
//								}
//								counter++;
//							}
//						}
//						if(argumentsMatched) { // horrible
//							if(!executed) {
//								if(method is ConstructorInfo) {
//									result=((ConstructorInfo)method).Invoke(args.ToArray());
//								}
//								else {
//									result=method.Invoke(target,args.ToArray());
//								}
//								executed=true;
//							}
//							else { //what here?
//								//throw new ApplicationException("\nArguments match more than one overload of "+name);
//							}
//						}
//					}
//					if(!executed) {
//						if(savedMethod is ConstructorInfo) {
//							return 
//								Interpreter.ConvertDotNetObjectToMetaObject(
//								((ConstructorInfo)savedMethod).Invoke(new object[] {}));
//						}
//						else {
//							return Interpreter.ConvertDotNetObjectToMetaObject(savedMethod.Invoke(target,new object[] {}));
//						}
//
//					}
//					else {
//						return Interpreter.ConvertDotNetObjectToMetaObject(result);
//					}
//
//				}
//				catch(Exception e) {
//					Exception b=e;
//					string text="";
//					if(target!=null) {
//						text+=target.ToString();
//					}
//					else {
//						text+=type.ToString();
//					}
//					text+=".";
//					text+=name;
//					text+="(";
//					foreach(object obj in list) {
//						text+=obj.ToString()+",";
//					}
//					text=text.Remove(text.Length-1,1);
//					text+=")";
//					text+=" could not be invoked. ";
//					text+=e.ToString();
//					throw new ApplicationException(text);
//				}
//			}
			//refactor
			public static Delegate CreateDelegate(Type delegateType,MethodInfo method,Map code) {
				// should caller really be parent of code??? after fixing callers, yes
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
				source+="object result=callable.Call(null,arg);";
				if(method!=null) {
					if(!method.ReturnType.Equals(typeof(void))) {
						source+="return ("+returnTypeName+")";
						source+="Interpreter.ConvertMetaObjectToDotNetObject(result,typeof("+returnTypeName+"));";
					}
				}
				else {
					source+="return";
					source+=" result;";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				string metaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;//why needed?
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
				assemblyNames.AddRange(Interpreter.loadedAssemblies); // does this still work correctly
				CompilerParameters options=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
																																			  code});
				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
				if(method==null) {
					delegateType=typeof(NullDelegate);
				}
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
			private BindingFlags invokeMemberFlags;// what's that
			private string name;
			private IKeyValue parent;
			protected object target;
			protected Type type;
			public MethodBase savedMethod;
			public MethodBase[] methods;

		}
		public class NetClass: NetContainer, IKeyValue,ICallable {
			[DontSerializeFieldOrProperty]
			public NetMethod constructor;
			public NetClass(Type type):base(null,type) {
				this.constructor=new NetMethod(this.type);
			}
			public object Call(Map caller,IMap argument) {
				return constructor.Call(null,argument);
			}
		}
		public class NetObject: NetContainer, IKeyValue {
			public NetObject(object obj,Type type):base(obj,type) {
			}
			public override string ToString() {
				return obj.ToString();
			}
		}
		public abstract class NetContainer: IKeyValue, IEnumerable,ISerializeSpecial {
			public string Serialize() {
				return "";
			}
			public IKeyValue Parent { //put this into IKeyValue
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
				get { //improve some more
					if(key is string) {
						string text=(string)key;
						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|
							BindingFlags.Static|BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								return new NetMethod(text,obj,type);
							}
							if(members[0] is FieldInfo) {
								return Interpreter.ConvertDotNetObjectToMetaObject(type.GetField(text).GetValue(obj));
							}
							else if(members[0] is PropertyInfo) {
								return Interpreter.ConvertDotNetObjectToMetaObject( type.GetProperty(text).GetValue(obj,new object[]{}));
							}
							else if(members[0] is EventInfo) {
								Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
								return new NetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
						}
					}
					NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
//					Interpreter.arguments.Add(arguments);
					try {
						object result=indexerMethod.Call(null,arguments);
						return result;
					}
					catch(Exception) {
						return null;
					}
					finally {
//						Interpreter.arguments.Remove(arguments);
					}
				}
//				get { //improve some more
//					if(key is string) {
//						string text=(string)key;
//						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|
//							BindingFlags.Static|BindingFlags.Instance);
//						if(members.Length>0) {
//							if(members[0] is MethodBase) {
//								return new NetMethod(text,obj,type);
//							}
//							if(members[0] is FieldInfo) {
//								return Interpreter.ConvertDotNetObjectToMetaObject(type.GetField(text).GetValue(obj));
//							}
//							else if(members[0] is PropertyInfo) {
//								return Interpreter.ConvertDotNetObjectToMetaObject( type.GetProperty(text).GetValue(obj,new object[]{}));
//							}
//							else if(members[0] is EventInfo) {
//								Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
//									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
//								return new NetMethod("Invoke",eventDelegate,eventDelegate.GetType());
//							}
//						}
//					}
//					NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
//					Map arguments=new Map();
//					arguments[new Integer(1)]=key;
//					Interpreter.arguments.Add(arguments);
//					try {
//						object result=indexerMethod.Call(null);
//						return result;
//					}
//					catch(Exception) {
//						return null;
//					}
//					finally {
//						Interpreter.arguments.Remove(arguments);
//					}
//				}
				set {	//make correct
					if(key is string) {
						MemberInfo[] members=type.GetMember((string)key,BindingFlags.Public|BindingFlags.Static|
							BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								throw new ApplicationException("Methods cannot be set.");
							}
							else if(members[0] is FieldInfo) {
								FieldInfo field=(FieldInfo)members[0];
								if(field.FieldType.IsAssignableFrom(value.GetType())) {
									field.SetValue(obj,value);
									return;
								}
								else {
									Hashtable toDotNet=(Hashtable) //combine?
										Interpreter.netConversion[field.FieldType];
									if(toDotNet!=null) {
										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
										if(conversion!=null) {
											field.SetValue(obj,conversion.Convert(value));
											return;
										}
									}
								}
							}
							else if(members[0] is PropertyInfo) {
								PropertyInfo field=(PropertyInfo)members[0];
								if(field.PropertyType.IsAssignableFrom(value.GetType())) {
									field.SetValue(obj,value,null);
									return;
								}
								else {
									Hashtable toDotNet=(Hashtable)
										Interpreter.netConversion[field.PropertyType];
									if(toDotNet!=null) {
										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
										if(conversion!=null) {
											field.SetValue(obj,conversion.Convert(value),null);
											return;
										}
									}
								}
							}
							else if(members[0] is EventInfo) {
								((EventInfo)members[0]).AddEventHandler(obj,CreateEvent((string)key,(Map)value));
								return;
							}
						}
					}
					NetMethod indexer=new NetMethod("set_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
					arguments[new Integer(2)]=value;
//					Interpreter.arguments.Add(arguments);
					try {
						indexer.Call(null,arguments);
					}
					catch(Exception) {
						throw new ApplicationException(key.ToString()+" not found in "+ToString());
					}
					finally {
//						Interpreter.arguments.Remove(arguments);
					}
				}
			}
//			public virtual object this[object key]  {
//				get { //improve some more
//					if(key is string) {
//						string text=(string)key;
//						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|
//							BindingFlags.Static|BindingFlags.Instance);
//						if(members.Length>0) {
//							if(members[0] is MethodBase) {
//								return new NetMethod(text,obj,type);
//							}
//							if(members[0] is FieldInfo) {
//								return Interpreter.ConvertDotNetObjectToMetaObject(type.GetField(text).GetValue(obj));
//							}
//							else if(members[0] is PropertyInfo) {
//								return Interpreter.ConvertDotNetObjectToMetaObject( type.GetProperty(text).GetValue(obj,new object[]{}));
//							}
//							else if(members[0] is EventInfo) {
//								Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
//									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
//								return new NetMethod("Invoke",eventDelegate,eventDelegate.GetType());
//							}
//						}
//					}
//					NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
//					Map arguments=new Map();
//					arguments[new Integer(1)]=key;
//					Interpreter.arguments.Add(arguments);
//					try {
//						object result=indexerMethod.Call(null);
//						return result;
//					}
//					catch(Exception) {
//						return null;
//					}
//					finally {
//						Interpreter.arguments.Remove(arguments);
//					}
//				}
//				set {	//make correct
//					if(key is string) {
//						MemberInfo[] members=type.GetMember((string)key,BindingFlags.Public|BindingFlags.Static|
//							BindingFlags.Instance);
//						if(members.Length>0) {
//							if(members[0] is MethodBase) {
//								throw new ApplicationException("Methods cannot be set.");
//							}
//							else if(members[0] is FieldInfo) {
//								FieldInfo field=(FieldInfo)members[0];
//								if(field.FieldType.IsAssignableFrom(value.GetType())) {
//									field.SetValue(obj,value);
//									return;
//								}
//								else {
//									Hashtable toDotNet=(Hashtable) //combine?
//										Interpreter.netConversion[field.FieldType];
//									if(toDotNet!=null) {
//										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
//										if(conversion!=null) {
//											field.SetValue(obj,conversion.Convert(value));
//											return;
//										}
//									}
//								}
//							}
//							else if(members[0] is PropertyInfo) {
//								PropertyInfo field=(PropertyInfo)members[0];
//								if(field.PropertyType.IsAssignableFrom(value.GetType())) {
//									field.SetValue(obj,value,null);
//									return;
//								}
//								else {
//									Hashtable toDotNet=(Hashtable)
//										Interpreter.netConversion[field.PropertyType];
//									if(toDotNet!=null) {
//										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
//										if(conversion!=null) {
//											field.SetValue(obj,conversion.Convert(value),null);
//											return;
//										}
//									}
//								}
//							}
//							else if(members[0] is EventInfo) {
//								((EventInfo)members[0]).AddEventHandler(obj,CreateEvent((string)key,(Map)value));
//								return;
//							}
//						}
//					}
//					NetMethod indexer=new NetMethod("set_Item",obj,type);
//					Map arguments=new Map();
//					arguments[new Integer(1)]=key;
//					arguments[new Integer(2)]=value;
//					Interpreter.arguments.Add(arguments);
//					try {
//						indexer.Call(null);
//					}
//					catch(Exception) {
//						throw new ApplicationException(key.ToString()+" not found in "+ToString());
//					}
//					finally {
//						Interpreter.arguments.Remove(arguments);
//					}
//				}
//			}
			public Delegate CreateEvent(string name,Map code) {
				EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
					BindingFlags.Static|BindingFlags.Instance);
				MethodInfo method=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance
					|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
				Delegate del=NetMethod.CreateDelegate(eventInfo.EventHandlerType,method,code);
				return del;
			}
			public bool ContainsKey(object key)  { // not great
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
			private IDictionary Table { // left the way it is for now, since there is no test for it
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
					if(obj!=null && obj is IEnumerable && !(obj is String)) { // is this useful?
						foreach(object entry in (IEnumerable)obj) {
							if(entry is DictionaryEntry) {
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
			public NetContainer(object obj,Type type) {
				this.obj=obj;
				this.type=type;
			}
			private IKeyValue parent;
			public object obj;
			public Type type;
		}
		public abstract class RecognizeLiteral {
			public abstract object Recognize(string text);
		}
		public abstract class ConvertMetaToDotNet {
			public Type source;
			public Type target;
			public abstract object Convert(object obj);
		}
		public abstract class ConvertDotNetToMeta {
			public Type source;
			public abstract object Convert(object obj);
		}
		public class LiteralRecognitions {
			// order of classes is important here
			public class RecognizeDotNetString: RecognizeLiteral  {
				public override object Recognize(string text)  {
					return text;
				}
			}
			public class RecognizeInteger: RecognizeLiteral  {
				public override object Recognize(string text)  { // this doesn't handle multi-byte unicode correctly
					if(text.Equals("")) {
						return null;
					}
					else {
						Integer number=new Integer(0);
						int i=0;
						if(text[0]=='-') {
							i++;
						}
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
			public class RecognizeString:RecognizeLiteral {
				public override object Recognize(string text) {
					if(text.StartsWith("\"") && text.EndsWith("\"")) {
						return DotNetToMetaConversions.ConvertStringToMap.StringToMap(text.Substring(1,text.Length-2));
					}
					else {
						return null;
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
		public abstract class MetaToDotNetConversions { // these haven't all been tested
			public class ConvertIntegerToByte: ConvertMetaToDotNet {
				public ConvertIntegerToByte() {
					this.source=typeof(Integer);
					this.target=typeof(Byte);
				}
				public override object Convert(object obj) {
					return System.Convert.ToByte(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToSByte: ConvertMetaToDotNet {
				public ConvertIntegerToSByte() {
					this.source=typeof(Integer);
					this.target=typeof(SByte);
				}
				public override object Convert(object obj) {
					return System.Convert.ToSByte(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToChar: ConvertMetaToDotNet {
				public ConvertIntegerToChar() {
					this.source=typeof(Integer);
					this.target=typeof(Char);
				}
				public override object Convert(object obj) {
					return System.Convert.ToChar(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToInt32: ConvertMetaToDotNet {
				public ConvertIntegerToInt32() {
					this.source=typeof(Integer);
					this.target=typeof(Int32);
				}
				public override object Convert(object obj) {
					return System.Convert.ToInt32(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToUInt32: ConvertMetaToDotNet {
				public ConvertIntegerToUInt32() {
					this.source=typeof(Integer);
					this.target=typeof(UInt32);
				}
				public override object Convert(object obj) {
					return System.Convert.ToUInt32(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToInt64: ConvertMetaToDotNet {
				public ConvertIntegerToInt64() {
					this.source=typeof(Integer);
					this.target=typeof(Int64);
				}
				public override object Convert(object obj) {
					return System.Convert.ToInt64(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToUInt64: ConvertMetaToDotNet {
				public ConvertIntegerToUInt64() {
					this.source=typeof(Integer);
					this.target=typeof(UInt64);
				}
				public override object Convert(object obj) {
					return System.Convert.ToUInt64(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToInt16: ConvertMetaToDotNet {
				public ConvertIntegerToInt16() {
					this.source=typeof(Integer);
					this.target=typeof(Int16);
				}
				public override object Convert(object obj) {
					return System.Convert.ToInt16(((Integer)obj).LongValue());
				}
			}
			public class ConvertIntegerToUInt16: ConvertMetaToDotNet {
				public ConvertIntegerToUInt16() {
					this.source=typeof(Integer);
					this.target=typeof(UInt16);
				}
				public override object Convert(object obj) {
					return System.Convert.ToUInt16(((Integer)obj).LongValue());
				}
			}
			public class ConvertMapToString: ConvertMetaToDotNet {
				public ConvertMapToString() {
					this.source=typeof(Map);
					this.target=typeof(string);
				}
				public override object Convert(object obj) {
					Map symbol=(Map)obj;
					string text="";
					for(Integer i=new Integer(1);;i++) {
						object val=symbol[i];
						if(val==null) {
							break;
						}
						else {
							if(val is Integer) {
								text+=System.Convert.ToChar(((Integer)val).LongValue());
							}
						}
					}
					return text;
				}
			}
		}
		public abstract class DotNetToMetaConversions { // these haven't all been tested
			public class ConvertStringToMap: ConvertDotNetToMeta {
				public ConvertStringToMap()   {
					this.source=typeof(string);
				}
				public override object Convert(object obj) {
					return StringToMap((string)obj);
				}
				public static Map StringToMap(string symbol) { // move, not really needed here, but in conversions
					Map map=new Map();
					foreach(char character in symbol) {
						map[new Integer(map.Count+1)]=new Integer((int)character);
					}
					return map;
				}
			}
			public class ConvertByteToInteger: ConvertDotNetToMeta {
				public ConvertByteToInteger() {
					this.source=typeof(Byte);
				}
				public override object Convert(object obj) {
					return new Integer((Byte)obj);
				}
			}
			public class ConvertSByteToInteger: ConvertDotNetToMeta {
				public ConvertSByteToInteger() {
					this.source=typeof(SByte);
				}
				public override object Convert(object obj) {
					return new Integer((SByte)obj);
				}
			}
			public class ConvertCharToInteger: ConvertDotNetToMeta {
				public ConvertCharToInteger() {
					this.source=typeof(Char);
				}
				public override object Convert(object obj) {
					return new Integer((Char)obj);
				}
			}
			public class ConvertInt32ToInteger: ConvertDotNetToMeta {
				public ConvertInt32ToInteger() {
					this.source=typeof(Int32);
				}
				public override object Convert(object obj) {
					return new Integer((Int32)obj);
				}
			}
			public class ConvertUInt32ToInteger: ConvertDotNetToMeta {
				public ConvertUInt32ToInteger() {
					this.source=typeof(UInt32);
				}
				public override object Convert(object obj) {
					return new Integer((UInt32)obj);
				}
			}
			public class ConvertInt64ToInteger: ConvertDotNetToMeta {
				public ConvertInt64ToInteger() {
					this.source=typeof(Int64);
				}
				public override object Convert(object obj) {
					return new Integer((Int64)obj);
				}
			}
			public class ConvertUInt64ToInteger: ConvertDotNetToMeta {
				public ConvertUInt64ToInteger() {
					this.source=typeof(UInt64);
				}
				public override object Convert(object obj) {
					return new Integer((Int64)(UInt64)obj);
				}
			}
			public class ConvertInt16ToInteger: ConvertDotNetToMeta {
				public ConvertInt16ToInteger() {
					this.source=typeof(Int16);
				}
				public override object Convert(object obj) {
					return new Integer((Int16)obj);
				}
			}
			public class ConvertUInt16ToInteger: ConvertDotNetToMeta {
				public ConvertUInt16ToInteger() {
					this.source=typeof(UInt16);
				}
				public override object Convert(object obj) {
					return new Integer((UInt16)obj);
				}
			}
		}
	}
	namespace Parser  {
		class AddIndentationTokensToStream: TokenStream {
			public AddIndentationTokensToStream(TokenStream originalStream)  {
				this.originalStream=originalStream;
				AddIndentationTokensToGetToLevel(0);
			}
			public Token nextToken()  {
				if(streamBuffer.Count==0)  {
					Token t=originalStream.nextToken();
					switch(t.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokensToGetToLevel(-1);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokensToGetToLevel(t.getText().Length/2);
							break;
						default:
							streamBuffer.Enqueue(t);
							break;
					}
				}
				return (Token)streamBuffer.Dequeue();
			}	
			protected void AddIndentationTokensToGetToLevel(int newIndentationLevel)  { // refactor
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
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(indentationDifference>1) {
					throw new ApplicationException("Line is indented too much.");
				}
				presentIndentationLevel=newIndentationLevel;
			}
			protected Queue streamBuffer=new Queue();
			protected TokenStream originalStream;
			protected int presentIndentationLevel=-1;
		}
	}
	namespace TestingFramework {
		public interface ISerializeSpecial {
			string Serialize();
		}
		public class ExecuteTests {	
			public ExecuteTests(Type classThatContainsTests,string pathToSerializeResultsTo) { // refactor
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
					object result=null;
					try {
						result=testCase.GetMethod("RunTestCase").Invoke(null,new object[]{});
					}
					catch(Exception e) {
						throw e;
					}
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
				string check=new StreamReader(Path.Combine(path,"check.txt")).ReadToEnd();
				return result.Equals(check);
			}
			public static string Serialize(object obj) {
				return Serialize(obj,"",new string[]{});
			}
			public static string Serialize(object serialize,string indent,string[] methods) {
				string text="";
				if(serialize==null) {
					text=indent+"null\n";
				}
				else if(serialize is ISerializeSpecial) {
					text=indent+((ISerializeSpecial)serialize).Serialize();
				}
				else if(serialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) {
					text=indent+"\""+serialize.ToString()+"\""+"\n";
				}
				else if(serialize is IEnumerable) {
					foreach(object entry in (IEnumerable)serialize) {
						text+=indent+"Entry ("+entry.GetType().Name+")\n"+Serialize(entry,indent+"  ",methods);
					}
				}
				else {
					ArrayList members=new ArrayList();
					members.AddRange(serialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
					members.AddRange(serialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
					foreach(string method in methods) {
						members.Add(serialize.GetType().GetMethod(method));
					}
					members.Sort(new CompareMemberInfos());
					foreach(MemberInfo member in members) {
						if(member.Name!="Item") {
							if(member.GetCustomAttributes(typeof(DontSerializeFieldOrPropertyAttribute),false).Length==0) {
								object val=serialize.GetType().InvokeMember(member.Name,BindingFlags.Public
									|BindingFlags.Instance|BindingFlags.GetProperty|BindingFlags.GetField
									|BindingFlags.InvokeMethod,null,serialize,null);
								text+=indent+member.Name;
								if(val!=null) {
									text+=" ("+val.GetType().Name+")";
								}
								text+=":\n"+Serialize(val,indent+"  ",methods);
							}
						}
					}
				}
				return text;
			}
		}
		internal class CompareMemberInfos:IComparer {
			public int Compare(object first,object second) {
				return ((MemberInfo)first).Name.CompareTo(((MemberInfo)second).Name);
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
