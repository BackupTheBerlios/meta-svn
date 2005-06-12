//	Meta is a programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License version 2
//	as published by the Free Software Foundation.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.



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
using System.Text;

namespace Meta {
	namespace Execution {
		public abstract class Expression {
			public static readonly Map sRun=new Map("run"); // TODO: get rid of "String"-suffix, use Hungarian syntax, that is "s" prefix
//			public object OjEvaluateM(IMap parent) {
////				try {
//					return OjEvaluateM(parent);
////				}
////				catch(Exception e) {
////					throw new MetaException(e,this.extent);
////				}
//			}
			public abstract object OjEvaluateM(IMap parent);
			Extent extent;
			public Extent EtExtent{
				get {

					return extent;
				}
				set {
					extent=value;
				}
			}
		}
		public class Call: Expression {
			public override object OjEvaluateM(IMap parent) {
				object ojArgument=epsArgument.OjEvaluateM(parent);
				if(ojArgument is IMap) {
					ojArgument=((IMap)ojArgument).mCloneV();
				}
				return ((ICallable)epsCallable.OjEvaluateM(parent)).ojCallOj(ojArgument);
			}
			public static readonly Map sCall=new Map("call");
			public static readonly Map sFunction=new Map("function");
			public static readonly Map sArgument=new Map("argument");
			public Call(Map obj) {
				Map mCall=(Map)obj[sCall];
				this.epsCallable=(Expression)((Map)mCall[sFunction]).EpsCompileV();
				this.epsArgument=(Expression)((Map)mCall[sArgument]).EpsCompileV();
			}
			public Expression epsArgument;
			public Expression epsCallable;
		}





		public class Delayed: Expression {
			public override object OjEvaluateM(IMap mParent) {
				Map mClone=mDelayed;
				mClone.mParent=mParent;
				return mClone;
			}
			public static readonly Map sDelayed=new Map("delayed"); // TODO: maybe define my own type for this stuff?
			public Delayed(Map code) {
				this.mDelayed=(Map)code[sDelayed];
			}
			public Map mDelayed;
		}


		public class Program: Expression {
			public override object OjEvaluateM(IMap mParent) {
				Map mLocal=new Map();
				return OjEvaluateM(mParent,mLocal);
			}
			public object OjEvaluateM(IMap mParent,IMap mLocal) {
				mLocal.mParent=mParent;
				Interpreter.arlMCallers.Add(mLocal);
				for(int i=0;i<arlSmStatements.Count;i++) {
					mLocal=(Map)Interpreter.OjCurrent;
					((Statement)arlSmStatements[i]).VRealizeM(mLocal);
				}
				object ojResult=Interpreter.OjCurrent;
				Interpreter.arlMCallers.RemoveAt(Interpreter.arlMCallers.Count-1);
				return ojResult;
			}
			public static readonly Map sProgram=new Map("program");
			public Program(Map mProgram) { // TODO: special Type for  callable maps?
				foreach(Map mStatement in ((Map)mProgram[sProgram]).ArlojIntegerKeyValues) {
					this.arlSmStatements.Add(new Statement(mStatement)); // should we save the original maps instead of arlSmStatements?
				}
			}
//			public Program(Map code) {
//				foreach(Map statement in ((Map)code[sProgram]).ArlojIntegerKeyValues) {
//					this.arlSmStatements.Add(statement.EpsCompileV()); // should we save the original maps instead of arlSmStatements?
//				}
//			}
			public readonly ArrayList arlSmStatements=new ArrayList();
		}
		public class Literal: Expression {
			public override object OjEvaluateM(IMap mParent) {
//				if(ojLiteral.Equals(new Map("staticEvent"))) {
//					int asdf=0;
//				}
//				if(ojLiteral.Equals(new Map("TestClass"))) {
//					int asdf=0;
//				}
				return ojLiteral;
			}
			public static readonly Map sLiteral=new Map("literal");
			public Literal(Map code) {
				this.ojLiteral=Interpreter.OjRecognizeLiteralS((string)((Map)code[sLiteral]).SDotNetStringV());
			}
			public object ojLiteral=null;
		}
		public class Search: Expression {
			public Search(Map mSearch) {
				this.epsKey=(Expression)((Map)mSearch[sSearch]).EpsCompileV();
			}
			public Expression epsKey;
			public static readonly Map sKey=new Map("key");
			public override object OjEvaluateM(IMap mParent) {
				object ojKey=epsKey.OjEvaluateM(mParent);
				IMap mSelected=mParent;
				while(!mSelected.blaHasKeyOj(ojKey)) {
					mSelected=mSelected.mParent;
					if(mSelected==null) {
						throw new KeyNotFoundException(ojKey,this.EtExtent);
					}
				}
				return mSelected[ojKey];
			}
			public static readonly Map sSearch=new Map("search");
		}

		public class Select: Expression {
			public ArrayList arlEpsKeys=new ArrayList();
			public Expression epsFirst;// TODO: maybe rename to srFirst -> it's a Search
			public Select(Map code) {
				ArrayList arlMKeyExpressions=((Map)code[sSelect]).ArlojIntegerKeyValues;
				epsFirst=(Expression)((Map)arlMKeyExpressions[0]).EpsCompileV();
				for(int i=1;i<arlMKeyExpressions.Count;i++) {
					arlEpsKeys.Add(((Map)arlMKeyExpressions[i]).EpsCompileV());
				}
			}
			public override object OjEvaluateM(IMap parent) {
				object ojSelected=epsFirst.OjEvaluateM(parent);
				for(int i=0;i<arlEpsKeys.Count;i++) {
					if(!(ojSelected is IKeyValue)) {
						ojSelected=new NetObject(ojSelected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
					}
					object k=((Expression)arlEpsKeys[i]).OjEvaluateM(parent);
					ojSelected=((IKeyValue)ojSelected)[k];
					if(ojSelected==null) {
						throw new KeyDoesNotExistException(k,this.EtExtent);
					}
				}
				return ojSelected;
			}
			public static readonly Map sSelect=new Map("select");
		}

		public class Statement {
			public void VRealizeM(IMap mParent) {
				object ojSelected=mParent;
				object ojKey;
				for(int i=0;i<arlEpsKeys.Count-1;i++) {
					ojKey=((Expression)arlEpsKeys[i]).OjEvaluateM((IMap)mParent);
					ojSelected=((IKeyValue)ojSelected)[ojKey];
					if(ojSelected==null) {
						throw new KeyDoesNotExistException(ojKey,((Expression)arlEpsKeys[i]).EtExtent);
					}
					if(!(ojSelected is IKeyValue)) {
						ojSelected=new NetObject(ojSelected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
					}
				}
				object ojLastKey=((Expression)arlEpsKeys[arlEpsKeys.Count-1]).OjEvaluateM((IMap)mParent);
				object ojValue=epsValue.OjEvaluateM((IMap)mParent);
				if(ojLastKey.Equals(Map.sThis)) {
					if(ojValue is Map) {
						((Map)ojValue).mParent=((Map)mParent).mParent;
					}
					else {
						int asdf=0;
					}
					Interpreter.OjCurrent=ojValue;

				}
				else {
					((IKeyValue)ojSelected)[ojLastKey]=ojValue;
				}
			}
			public Statement(Map mStatement) {
//				ArrayList intKeys=;
//				intKeys.Reverse();
				foreach(Map key in ((Map)mStatement[sKey]).ArlojIntegerKeyValues) {
					arlEpsKeys.Add(key.EpsCompileV());
				}
				this.epsValue=(Expression)((Map)mStatement[sValue]).EpsCompileV();
			}
			public ArrayList arlEpsKeys=new ArrayList();
			public Expression epsValue;


			public static readonly Map sKey=new Map("key");
			public static readonly Map sValue=new Map("value");
		}

	

		public class Interpreter  {
			public static void vSaveToFileOjS(object meta,string fileName) {
				StreamWriter writer=new StreamWriter(fileName);
				writer.Write(vSaveToFileOjS(meta,"",true).TrimEnd(new char[]{'\n'}));
				writer.Close();
			}
			public static string vSaveToFileOjS(object ojMeta,string sIndent,bool blaRightSide) {
				if(ojMeta is Map) {
					string sText="";
					Map mMap=(Map)ojMeta;
					if(mMap.IsString) {
						sText+="\""+(mMap).SDotNetStringV()+"\"";
					}
					else if(mMap.Count==0) {
						sText+="()";
					}
					else {
						if(!blaRightSide) {
							sText+="(";
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+='['+vSaveToFileOjS(dtnretEntry.Key,sIndent,true)+']'+'='+vSaveToFileOjS(dtnretEntry.Value,sIndent,true)+",";
							}
							if(mMap.Count!=0) {
								sText=sText.Remove(sText.Length-1,1);
							}
							sText+=")";
						}
						else {
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+=sIndent+'['+vSaveToFileOjS(dtnretEntry.Key,sIndent,false)+']'+'=';
								if(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).Count!=0 && !((Map)dtnretEntry.Value).IsString) {
									sText+="\n";
								}
								sText+=vSaveToFileOjS(dtnretEntry.Value,sIndent+'\t',true);
								if(!(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).Count!=0 && !((Map)dtnretEntry.Value).IsString)) {
									sText+="\n";
								}
							}
						}
					}
					return sText;
				}
				else if(ojMeta is Integer) {
					Integer integer=(Integer)ojMeta;
					return "\""+integer.ToString()+"\"";
				}
				else {
					throw new ApplicationException("Serialization not implemented for type "+ojMeta.GetType().ToString()+".");
				}
			}
			public static IKeyValue Merge(params IKeyValue[] arkvlToMerge) {
				return MergeCollection(arkvlToMerge);
			}
			// really use IKeyValue?
			public static IKeyValue MergeCollection(ICollection cltkvlToMerge) {
				Map mResult=new Map();//use clone here?
				foreach(IKeyValue kvlCurrent in cltkvlToMerge) {
					foreach(DictionaryEntry dtnetEntry in (IKeyValue)kvlCurrent) {
						if(dtnetEntry.Value is IKeyValue && !(dtnetEntry.Value is NetClass)&& mResult.blaHasKeyOj(dtnetEntry.Key) 
							&& mResult[dtnetEntry.Key] is IKeyValue && !(mResult[dtnetEntry.Key] is NetClass)) {
							mResult[dtnetEntry.Key]=Merge((IKeyValue)mResult[dtnetEntry.Key],(IKeyValue)dtnetEntry.Value);
						}
						else {
							mResult[dtnetEntry.Key]=dtnetEntry.Value;
						}
					}
				}
				return mResult;
			}	
			public static object OjRecognizeLiteralS(string text) {
				foreach(RecognizeLiteral rcnltrCurrent in literalRecognitions) {
					object recognized=rcnltrCurrent.Recognize(text);
					if(recognized!=null) {
						return recognized;
					}
				}
				return null;
			}
			public static object ConvertDotNetToMeta(object obj) { 
				if(obj==null) {
					return null;
				}
				else if(obj.GetType().IsSubclassOf(typeof(Enum))) {
					return new Integer((int)Convert.ToInt32((Enum)obj));
				}
				DotNetToMetaConversion conversion=(DotNetToMetaConversion)metaConversion[obj.GetType()];
				if(conversion==null) {
					return obj;
				}
				else {
					return conversion.Convert(obj);
				}
			}
			public static object ConvertMetaToDotNet(object obj) {
				if(obj is Integer) {
					return ((Integer)obj).Int;
				}
				else if(obj is Map && ((Map)obj).IsString) {
					return ((Map)obj).SDotNetStringV();
				}
				else {
					return obj;
				}
			}
			public static object ConvertMetaToDotNet(object obj,Type targetType) {
				try {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)((Hashtable)
						Interpreter.netConversion[obj.GetType()])[targetType];
					bool converted;
					return conversion.Convert(obj,out converted); // TODO: Why ignore converted here?, Should really loop through all the possibilities -> no not necessary here, type determines conversion
				}
				catch {
					return obj;
				}
			}
//			public static object Run(string fileName,IMap argument) {
//				Map program=mCompileS(fileName);
//				program.mParent=Library.library;
//				return program.Call(argument);
//			}
//
//			public static object RunWithoutLibrary(string fileName,IMap argument) { // TODO: refactor, combine with Run
//				Map program=mCompileS(fileName); // TODO: rename, is not really a program but a function
//				return program.Call(argument);
//			}
			public static object Run(string fileName,IMap argument) {
//				Map program=mCompileS(fileName);
				//				program.mParent=Library.library;
				Map program=Interpreter.mCompileS(fileName);

				return CallProgram(program,argument,Library.library);
			}

			public static object RunWithoutLibrary(string fileName,IMap argument) { // TODO: refactor, combine with Run
				Map program=mCompileS(fileName); // TODO: rename, is not really a program but a function
				return CallProgram(program,argument,null);
			}
			public static object CallProgram(Map program,IMap argument,IMap parent) {
				Map mCallable=new Map();
				mCallable[Expression.sRun]=program;
				mCallable.mParent=parent;
				return mCallable.ojCallOj(argument);
			}

//			public static Map mCompileS(string fileName,Map mArg) {
//				Map mFunction=new Map();
//				mFunction[Expression.sRun]=(new MetaTreeParser()).map(ParseToAst(fileName));
//				Map mArgument=new Map();
//				Map mCall=new Map();
//				mCall[Call.sFunction]=mFunction;
//				mCall[Call.sArgument]=mArgument;
//				return mCall;
//			}
			public static Map mCompileS(string fileName) {
				return (new MetaTreeParser()).map(ParseToAst(fileName));
			}
			public static AST ParseToAst(string fileName)  {

				// TODO: Add the newlines here somewhere (or do this in IndentationStream?, somewhat easier and more logical maybe), but not possible, must be before lexer
				// construct the special shared input state that is needed
				// in order to annotate MetaTokens properly
				FileStream file=new FileStream(fileName,FileMode.Open);
				ExtentLexerSharedInputState lsis = new ExtentLexerSharedInputState(file,fileName); 
				// construct the lexer
				MetaLexer lex = new MetaLexer(lsis);
		
				// tell the lexer the token class that we want
				lex.setTokenObjectClass("MetaToken");
		
				// construct the parser
				MetaParser par = new MetaParser(new IndentationStream(lex));
				// tell the parser the AST class that we want
				par.setASTNodeClass("MetaAST");//
				par.map();
				AST ast=par.getAST();
				file.Close();
				return ast;
			}

			public static Map Arg {
				get {
					return (Map)arguments[arguments.Count-1];
				}
			}
			public static object OjCurrent {
				get {
					if(arlMCallers.Count==0) {
						return null;
					}
					return arlMCallers[arlMCallers.Count-1];
				}
				set {
					arlMCallers[arlMCallers.Count-1]=value;
				}
			}
			public static object ConvertMetaToDotNet(object metaObject,Type targetType,out bool isConverted) {
				if(targetType.IsSubclassOf(typeof(Enum)) && metaObject is Integer) { 
					isConverted=true;
					return Enum.ToObject(targetType,((Integer)metaObject).Int);
				}
				Hashtable toDotNet=(Hashtable)
					Interpreter.netConversion[targetType];
				if(toDotNet!=null) {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)toDotNet[metaObject.GetType()];
					if(conversion!=null) {
						isConverted=true;
						return conversion.Convert(metaObject,out isConverted);
					}
				}
				isConverted=false;
				return null;
			}
//			public static object ConvertMetaToDotNet(object metaObject,Type targetType,out bool isConverted) {
//				if(targetType.IsSubclassOf(typeof(Enum)) && metaObject is Integer) { 
//					isConverted=true;
//					return Enum.ToObject(targetType,((Integer)metaObject).Int);
//				}
//				Hashtable toDotNet=(Hashtable)
//					Interpreter.netConversion[targetType];
//				if(toDotNet!=null) {
//					MetaToDotNetConversion conversion=(MetaToDotNetConversion)toDotNet[metaObject.GetType()];
//					if(conversion!=null) {
//						isConverted=true;
//						return conversion.Convert(metaObject);
//					}
//				}
//				isConverted=false;
//				return null;
//			}
			static Interpreter() {
				Assembly metaAssembly=Assembly.GetAssembly(typeof(Map));
				metaInstallationPath=Directory.GetParent(metaAssembly.Location).Parent.Parent.Parent.FullName; 
				foreach(Type type in typeof(LiteralRecognitions).GetNestedTypes()) {
					literalRecognitions.Add((RecognizeLiteral)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				literalRecognitions.Reverse();
				foreach(Type type in typeof(DotNetToMetaConversions).GetNestedTypes()) {
					DotNetToMetaConversion conversion=((DotNetToMetaConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
					metaConversion[conversion.source]=conversion;
				}
				foreach(Type type in typeof(MetaToDotNetConversions).GetNestedTypes()) {
					MetaToDotNetConversion conversion=(MetaToDotNetConversion)type.GetConstructor(new Type[]{}).Invoke(new object[]{});
					if(!netConversion.ContainsKey(conversion.target)) {
						netConversion[conversion.target]=new Hashtable();
					}
					((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
				}
			}
			public static string metaInstallationPath;
			public static ArrayList arlMCallers=new ArrayList();
			public static ArrayList arguments=new ArrayList();
			public static Hashtable netConversion=new Hashtable();
			public static Hashtable metaConversion=new Hashtable();
			public static ArrayList compiledMaps=new ArrayList(); 
			public static ArrayList loadedAssemblies=new ArrayList();

			private static ArrayList literalRecognitions=new ArrayList();

			public abstract class RecognizeLiteral {
				public abstract object Recognize(string text); // Returns null if not recognized. Null cannot currently be created this way.
			}
			public abstract class MetaToDotNetConversion {
				public Type source;
				public Type target;
				public abstract object Convert(object obj,out bool converted);
			}
			public abstract class DotNetToMetaConversion {
				public Type source;
				public abstract object Convert(object obj);
			}
			public class LiteralRecognitions {
				// Attention! order of RecognizeLiteral classes matters
				public class RecognizeString:RecognizeLiteral {
					public override object Recognize(string text) {
						return new Map(text);
					}
				}
				// does everything get executed twice?
				public class RecognizeCharacter: RecognizeLiteral {
					public override object Recognize(string text) {
						if(text.StartsWith(@"\")) { // TODO: Choose another character for starting a character
							char result;
							if(text.Length==2) {
								result=text[1]; // not unicode safe, write wrapper that takes care of this stuff
							}
							else if(text.Length==3) {
								switch(text.Substring(1,2))  { // TODO: put this into Parser???
									case @"\'":
										result='\'';
										break;
									case @"\\":
										result='\\';
										break;
									case @"\a":
										result='\a';
										break;
									case @"\b":
										result='\b';
										break;
									case @"\f":
										result='\f';
										break;
									case @"\n":
										result='\n';
										break;
									case @"\r":
										result='\r';
										break;
									case @"\t":
										result='\t';
										break;
									case @"\v":
										result='\v';
										break;
									default:
										throw new ApplicationException("Unrecognized escape sequence "+text);
								}
							}
							else {
								return null;
							}
							return new Integer(result);
						}
						return null;
					}
				}
				public class RecognizeInteger: RecognizeLiteral  {
					public override object Recognize(string text)  { 
						if(text.Equals("")) {
							return null;
						}
						else {
							Integer number=new Integer(0);
							int i=0;
							if(text[0]=='-') {
								i++;
							}
							// TODO: the following is probably incorrect for multi-byte unicode
							// use StringInfo in the future instead
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

			}
			private abstract class MetaToDotNetConversions {
				/* These classes define the conversions that performed when a .NET method, field, or property
				 * is called/assigned to from Meta. */


				// TODO: Handle "converted" correctly
				public class ConvertIntegerToByte: MetaToDotNetConversion {
					public ConvertIntegerToByte() {
						this.source=typeof(Integer);
						this.target=typeof(Byte);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToByte(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToBool: MetaToDotNetConversion {
					public ConvertIntegerToBool() {
						this.source=typeof(Integer);
						this.target=typeof(bool);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						int i=((Integer)obj).Int;
						if(i==0) {
							return false;
						}
						else if(i==1) {
							return true;
						}
						else {
							converted=false; // TODO
							return null;
//							throw new ApplicationException("Integer could not be converted to bool because it is neither 0 nor 1.");
						}
					}

				}
				public class ConvertIntegerToSByte: MetaToDotNetConversion {
					public ConvertIntegerToSByte() {
						this.source=typeof(Integer);
						this.target=typeof(SByte);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToSByte(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToChar: MetaToDotNetConversion {
					public ConvertIntegerToChar() {
						this.source=typeof(Integer);
						this.target=typeof(Char);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToChar(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt32: MetaToDotNetConversion {
					public ConvertIntegerToInt32() {
						this.source=typeof(Integer);
						this.target=typeof(Int32);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToInt32(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt32: MetaToDotNetConversion {
					public ConvertIntegerToUInt32() {
						this.source=typeof(Integer);
						this.target=typeof(UInt32);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToUInt32(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt64: MetaToDotNetConversion {
					public ConvertIntegerToInt64() {
						this.source=typeof(Integer);
						this.target=typeof(Int64);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToInt64(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt64: MetaToDotNetConversion {
					public ConvertIntegerToUInt64() {
						this.source=typeof(Integer);
						this.target=typeof(UInt64);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToUInt64(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToInt16: MetaToDotNetConversion {
					public ConvertIntegerToInt16() {
						this.source=typeof(Integer);
						this.target=typeof(Int16);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToInt16(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToUInt16: MetaToDotNetConversion {
					public ConvertIntegerToUInt16() {
						this.source=typeof(Integer);
						this.target=typeof(UInt16);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return System.Convert.ToUInt16(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToDecimal: MetaToDotNetConversion {
					public ConvertIntegerToDecimal() {
						this.source=typeof(Integer);
						this.target=typeof(decimal);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return (decimal)(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToDouble: MetaToDotNetConversion {
					public ConvertIntegerToDouble() {
						this.source=typeof(Integer);
						this.target=typeof(double);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return (double)(((Integer)obj).LongValue());
					}
				}
				public class ConvertIntegerToFloat: MetaToDotNetConversion {
					public ConvertIntegerToFloat() {
						this.source=typeof(Integer);
						this.target=typeof(float);
					}
					public override object Convert(object obj, out bool converted) {
						converted=true;
						return (float)(((Integer)obj).LongValue());
					}
				}
				public class ConvertMapToString: MetaToDotNetConversion {
					public ConvertMapToString() {
						this.source=typeof(Map);
						this.target=typeof(string);
					}
					public override object Convert(object obj, out bool converted) {
						if(((Map)obj).IsString) {
							converted=true;
							return ((Map)obj).SDotNetStringV();
						}
						else {
							converted=false;
							return null;
						}
					}
				}
				public class ConvertFractionToDecimal: MetaToDotNetConversion {
					public ConvertFractionToDecimal() {
						// maybe make this more flexible, make it a function that determines applicability
						// also add the possibility to disamibuate several conversion; problem when calling
						// overloaded, similar methods
						this.source=typeof(Map); 
						this.target=typeof(decimal); 
					}
					public override object Convert(object obj, out bool converted) {
						Map m=(Map)obj;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
							converted=true;
							return ((decimal)((Integer)m[new Map("iNumerator")]).LongValue())/((decimal)((Integer)m[new Map("iDenominator")]).LongValue());
						}
						else {
							converted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToDouble: MetaToDotNetConversion {
					public ConvertFractionToDouble() {
						this.source=typeof(Map);
						this.target=typeof(double);
					}
					public override object Convert(object obj, out bool converted) {
						Map m=(Map)obj;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
							converted=true;
							return ((double)((Integer)m[new Map("iNumerator")]).LongValue())/((double)((Integer)m[new Map("iDenominator")]).LongValue());
						}
						else {
							converted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToFloat: MetaToDotNetConversion {
					public ConvertFractionToFloat() {
						this.source=typeof(Map);
						this.target=typeof(float);
					}
					public override object Convert(object obj, out bool converted) {
						Map m=(Map)obj;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
							converted=true;
							return ((float)((Integer)m[new Map("iNumerator")]).LongValue())/((float)((Integer)m[new Map("iDenominator")]).LongValue());
						}
						else {
							converted=false;
							return null;
						}
					}
				}
			}
//			private abstract class MetaToDotNetConversions {
//				/* These classes define the conversions that performed when a .NET method, field, or property
//				 * is called/assigned to from Meta. */
//				public class ConvertIntegerToByte: MetaToDotNetConversion {
//					public ConvertIntegerToByte() {
//						this.source=typeof(Integer);
//						this.target=typeof(Byte);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToByte(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToBool: MetaToDotNetConversion {
//					public ConvertIntegerToBool() {
//						this.source=typeof(Integer);
//						this.target=typeof(bool);
//					}
//					public override object Convert(object obj) {
//						int i=((Integer)obj).Int;
//						if(i==0) {
//							return false;
//						}
//						else if(i==1) {
//							return true;
//						}
//						else {
//							throw new ApplicationException("Integer could not be converted to bool because it is neither 0 nor 1.");
//						}
//					}
//
//				}
//				public class ConvertIntegerToSByte: MetaToDotNetConversion {
//					public ConvertIntegerToSByte() {
//						this.source=typeof(Integer);
//						this.target=typeof(SByte);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToSByte(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToChar: MetaToDotNetConversion {
//					public ConvertIntegerToChar() {
//						this.source=typeof(Integer);
//						this.target=typeof(Char);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToChar(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToInt32: MetaToDotNetConversion {
//					public ConvertIntegerToInt32() {
//						this.source=typeof(Integer);
//						this.target=typeof(Int32);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToInt32(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToUInt32: MetaToDotNetConversion {
//					public ConvertIntegerToUInt32() {
//						this.source=typeof(Integer);
//						this.target=typeof(UInt32);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToUInt32(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToInt64: MetaToDotNetConversion {
//					public ConvertIntegerToInt64() {
//						this.source=typeof(Integer);
//						this.target=typeof(Int64);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToInt64(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToUInt64: MetaToDotNetConversion {
//					public ConvertIntegerToUInt64() {
//						this.source=typeof(Integer);
//						this.target=typeof(UInt64);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToUInt64(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToInt16: MetaToDotNetConversion {
//					public ConvertIntegerToInt16() {
//						this.source=typeof(Integer);
//						this.target=typeof(Int16);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToInt16(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToUInt16: MetaToDotNetConversion {
//					public ConvertIntegerToUInt16() {
//						this.source=typeof(Integer);
//						this.target=typeof(UInt16);
//					}
//					public override object Convert(object obj) {
//						return System.Convert.ToUInt16(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToDecimal: MetaToDotNetConversion {
//					public ConvertIntegerToDecimal() {
//						this.source=typeof(Integer);
//						this.target=typeof(decimal);
//					}
//					public override object Convert(object obj) {
//						return (decimal)(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToDouble: MetaToDotNetConversion {
//					public ConvertIntegerToDouble() {
//						this.source=typeof(Integer);
//						this.target=typeof(double);
//					}
//					public override object Convert(object obj) {
//						return (double)(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertIntegerToFloat: MetaToDotNetConversion {
//					public ConvertIntegerToFloat() {
//						this.source=typeof(Integer);
//						this.target=typeof(float);
//					}
//					public override object Convert(object obj) {
//						return (float)(((Integer)obj).LongValue());
//					}
//				}
//				public class ConvertMapToString: MetaToDotNetConversion {
//					public ConvertMapToString() {
//						this.source=typeof(Map);
//						this.target=typeof(string);
//					}
//					public override object Convert(object obj) {
//						return ((Map)obj).SDotNetStringV();
//					}
//				}
//				public class ConvertFractionToDecimal: MetaToDotNetConversion {
//					public ConvertFractionToDecimal() {
//						// maybe make this more flexible, make it a function that determines applicability
//						// also add the possibility to disamibuate several conversion; problem when calling
//						// overloaded, similar methods
//						this.source=typeof(Map); 
//						this.target=typeof(decimal); 
//					}
//					public override object Convert(object obj) {
//						Map m=(Map)obj;
//						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
//						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
//							return ((decimal)((Integer)m[new Map("iNumerator")]).LongValue())/((decimal)((Integer)m[new Map("iDenominator")]).LongValue());
//						}
//						return null;
//					}
//
//				}
//				public class ConvertFractionToDouble: MetaToDotNetConversion {
//					public ConvertFractionToDouble() {
//						this.source=typeof(Map);
//						this.target=typeof(double);
//					}
//					public override object Convert(object obj) {
//						Map m=(Map)obj;
//						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
//						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
//							return ((double)((Integer)m[new Map("iNumerator")]).LongValue())/((double)((Integer)m[new Map("iDenominator")]).LongValue());
//						}
//						return null;
//					}
//
//				}
//				public class ConvertFractionToFloat: MetaToDotNetConversion {
//					public ConvertFractionToFloat() {
//						this.source=typeof(Map);
//						this.target=typeof(float);
//					}
//					public override object Convert(object obj) {
//						Map m=(Map)obj;
//						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
//						if(m[new Map("iNumerator")] is Integer && m[new Map("iDenominator")] is Integer) {
//							return ((float)((Integer)m[new Map("iNumerator")]).LongValue())/((float)((Integer)m[new Map("iDenominator")]).LongValue());
//						}
//						return null;
//					}
//				}
//			}
			private abstract class DotNetToMetaConversions {
				/* These classes define the conversions that take place when .NET methods,
				 * properties and fields return. */
				public class ConvertStringToMap: DotNetToMetaConversion {
					public ConvertStringToMap()   {
						this.source=typeof(string);
					}
					public override object Convert(object obj) {
						return new Map((string)obj);
					}
				}
				public class ConvertBoolToInteger: DotNetToMetaConversion {
					public ConvertBoolToInteger() {
						this.source=typeof(bool);
					}
					public override object Convert(object obj) {
						return (bool)obj? new Integer(1): new Integer(0);
					}

				}
				public class ConvertByteToInteger: DotNetToMetaConversion {
					public ConvertByteToInteger() {
						this.source=typeof(Byte);
					}
					public override object Convert(object obj) {
						return new Integer((Byte)obj);
					}
				}
				public class ConvertSByteToInteger: DotNetToMetaConversion {
					public ConvertSByteToInteger() {
						this.source=typeof(SByte);
					}
					public override object Convert(object obj) {
						return new Integer((SByte)obj);
					}
				}
				public class ConvertCharToInteger: DotNetToMetaConversion {
					public ConvertCharToInteger() {
						this.source=typeof(Char);
					}
					public override object Convert(object obj) {
						return new Integer((Char)obj);
					}
				}
				public class ConvertInt32ToInteger: DotNetToMetaConversion {
					public ConvertInt32ToInteger() {
						this.source=typeof(Int32);
					}
					public override object Convert(object obj) {
						return new Integer((Int32)obj);
					}
				}
				public class ConvertUInt32ToInteger: DotNetToMetaConversion {
					public ConvertUInt32ToInteger() {
						this.source=typeof(UInt32);
					}
					public override object Convert(object obj) {
						return new Integer((UInt32)obj);
					}
				}
				public class ConvertInt64ToInteger: DotNetToMetaConversion {
					public ConvertInt64ToInteger() {
						this.source=typeof(Int64);
					}
					public override object Convert(object obj) {
						return new Integer((Int64)obj);
					}
				}
				public class ConvertUInt64ToInteger: DotNetToMetaConversion {
					public ConvertUInt64ToInteger() {
						this.source=typeof(UInt64);
					}
					public override object Convert(object obj) {
						return new Integer((Int64)(UInt64)obj);
					}
				}
				public class ConvertInt16ToInteger: DotNetToMetaConversion {
					public ConvertInt16ToInteger() {
						this.source=typeof(Int16);
					}
					public override object Convert(object obj) {
						return new Integer((Int16)obj);
					}
				}
				public class ConvertUInt16ToInteger: DotNetToMetaConversion {
					public ConvertUInt16ToInteger() {
						this.source=typeof(UInt16);
					}
					public override object Convert(object obj) {
						return new Integer((UInt16)obj);
					}
				}
			}
		}
		/* Base class of exceptions in Meta. */
		public class MetaException:ApplicationException {
			protected string message="";
			public MetaException(Extent extent) {
				this.extent=extent;
			}
			public MetaException(Exception exception,Extent extent):base(exception.Message,exception) { // not really all that logical, but so what
				this.extent=extent;
			}
			Extent extent;
//			public MetaException(string message) {
//				this.message=message;
//			}
			public override string Message {
				get {
					return message+" In file "+extent.fileName+", line: "+extent.startLine+", column: "+extent.startColumn+".";
				}
			}
		}
//		public class ApplicationException:MetaException {
//			public ApplicationException(string message):base(message) {
//			}
//		}


		/* Base class for key exceptions. */
		public abstract class KeyException:MetaException { // TODO: Add proper formatting here, output strings as strings, for example, if possible, as well as integers
			public KeyException(object key,Extent extent):base(extent) {
				message="Key ";
				if(key is Map && ((Map)key).IsString) {
					message+=((Map)key).SDotNetStringV();
				}
				else if(key is Map) {
					message+=Interpreter.vSaveToFileOjS(key,"",true);
				}
				else {
					message+=key;
				}
				message+=" not found.";
			}
		}
		/* Thrown when a searched key was not found. */
		public class KeyNotFoundException:KeyException {
			public KeyNotFoundException(object key,Extent extent):base(key,extent) {
			}
		}
		/* Thrown when an accessed key does not exist. */
		public class KeyDoesNotExistException:KeyException {
			public KeyDoesNotExistException(object key,Extent extent):base(key,extent) {
			}
		}
	}
	namespace Types  {
		/* Everything implementing this interface can be used in a Call expression */
		public interface ICallable {
			object ojCallOj(object argument);
		}
		// TODO: Rename this eventually
		public interface IMap: IKeyValue {
			IMap mParent {
				get;
				set;
			}
			ArrayList ArlojIntegerKeyValues {
				get;
			}
			IMap mCloneV();
		}
		// TODO: Does the IKeyValue<->IMap distinction make sense?
		public interface IKeyValue: IEnumerable {
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
			bool blaHasKeyOj(object key);			
		}		
		/* Represents a lazily evaluated "library" Meta file. */
		public class MetaLibrary { // TODO: Put this into Library class, make base class for everything that gets loaded
			public object Load() {
				return Interpreter.Run(path,new Map()); // TODO: Improve this interface, isn't read lazily anyway
			}
			public MetaLibrary(string path) {
				this.path=path;
			}
			string path;
		}
		/* Represents a lazily loaded .NET namespace. */
		public class LazyNamespace: IKeyValue { // TODO: Put this into library, combine with MetaLibrary
			public object this[object key] {
				get {
					if(cache==null) {
						Load();
					}
					return cache[key];
				}
				set {
					throw new ApplicationException("Cannot set key "+key.ToString()+" in .NET namespace.");
				}
			}
			public ArrayList Keys {
				get {
					if(cache==null) {
						Load();
					}
					return cache.Keys;
				}
			}
			public int Count {
				get {
					if(cache==null) {
						Load();
					}
					return cache.Count;
				}
			}
			public string fullName;
			public ArrayList cachedAssemblies=new ArrayList();
			public Hashtable namespaces=new Hashtable();
			public LazyNamespace(string fullName) {
				this.fullName=fullName;
			}
			public void Load() {
				cache=new Map();
				foreach(CachedAssembly cachedAssembly in cachedAssemblies) {
					cache=(Map)Interpreter.Merge(cache,cachedAssembly.GetNamespaceContents(fullName));
				}
				foreach(DictionaryEntry entry in namespaces) {
					cache[new Map((string)entry.Key)]=entry.Value;
				}
			}
			public Map cache;
			public bool blaHasKeyOj(object key) {
				if(cache==null) {
					Load();
				}
				return cache.blaHasKeyOj(key);
			}
			public IEnumerator GetEnumerator() {
				if(cache==null) {
					Load();
				}
				return cache.GetEnumerator();
			}
		}
		/* TODO: What's this for? */
		public class CachedAssembly {  // TODO: Put this into Library class
			private Assembly assembly;
			public CachedAssembly(Assembly assembly) {
				this.assembly=assembly;
			}
			public Map GetNamespaceContents(string fullName) {
				if(map==null) {
					map=Library.LoadAssemblies(new object[] {assembly});
				}
				Map selected=map;
				if(fullName!="") {
					foreach(string name in fullName.Split('.')) {
						selected=(Map)selected[new Map(name)];
					}
				}
				return selected;
			}			
			private Map map;
		}
		/* The library namespace, containing both Meta libraries as well as .NET libraries
		 *  from the "library" path and the GAC. */
		public class Library: IKeyValue,IMap {
			public object this[object key] {
				get {
//					if(key.Equals(new Map("map"))) {
//						int asdf=0;
//					}
					if(cash.blaHasKeyOj(key)) {
						if(cash[key] is MetaLibrary) {
							cash[key]=((MetaLibrary)cash[key]).Load();
						}
						return cash[key];
					}
					else {
						return null;
					}
				}
				set {
					throw new ApplicationException("Cannot set key "+key.ToString()+" in library.");
				}
			}
			public ArrayList Keys {
				get {
					return cash.Keys;
				}
			}
			public IMap mCloneV() {
				return this;
			}
			public int Count {
				get {
					return cash.Count;
				}
			}
			public bool blaHasKeyOj(object key) {
				return cash.blaHasKeyOj(key);
			}
			public ArrayList ArlojIntegerKeyValues {
				get {
					return new ArrayList();
				}
			}
			public IMap mParent {
				get {
					return null;
				}
				set {
					throw new ApplicationException("Cannot set parent of library.");
				}
			}
			public IEnumerator GetEnumerator() { 
				foreach(DictionaryEntry entry in cash) { // TODO: create separate enumerator for efficiency?
					object o=cash[entry.Key];				  // or remove IEnumerable from IMap (only needed for foreach)
				}														// decide later
				return cash.GetEnumerator();
			}
			public static Map LoadAssemblies(IEnumerable assemblies) {
				Map root=new Map();
				foreach(Assembly assembly in assemblies) {
					foreach(Type type in assembly.GetExportedTypes())  {
						if(type.DeclaringType==null)  {
							Map position=root;
							ArrayList subPaths=new ArrayList(type.FullName.Split('.'));
							subPaths.RemoveAt(subPaths.Count-1);
							foreach(string subPath in subPaths)  {
								if(!position.blaHasKeyOj(new Map(subPath)))  {
									position[new Map(subPath)]=new Map();
								}
								position=(Map)position[new Map(subPath)];
							}
							position[new Map(type.Name)]=new NetClass(type);
						}
					}
					Interpreter.loadedAssemblies.Add(assembly.Location);
				}
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
				ArrayList assemblies=new ArrayList();
				libraryPath=Path.Combine(Interpreter.metaInstallationPath,"library");
				IAssemblyEnum ae=AssemblyCache.CreateGACEnum();
				IAssemblyName an; 
				AssemblyName name;
				assemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
				while (AssemblyCache.GetNextAssembly(ae, out an) == 0) {
					try {
						name=GetAssemblyName(an);
						assemblies.Add(Assembly.LoadWithPartialName(name.Name));
					}
					catch(Exception e) {
						//Console.WriteLine("Could not load gac assembly :"+System.GAC.AssemblyCache.GetName(an));
					}
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.dll")) {
					assemblies.Add(Assembly.LoadFrom(fileName));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.exe")) {
					assemblies.Add(Assembly.LoadFrom(fileName));
				}
				string infoFileName=Path.Combine(Interpreter.metaInstallationPath,"assemblyInfo.meta"); // TODO: Use another name that doesn't collide with C# meaning
				if(File.Exists(infoFileName)) {
					assemblyInfo=(Map)Interpreter.RunWithoutLibrary(infoFileName,new Map());
				}
				
				cash=LoadNamespaces(assemblies);
				Interpreter.vSaveToFileOjS(assemblyInfo,infoFileName);
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.meta")) {
					cash[new Map(Path.GetFileNameWithoutExtension(fileName))]=new MetaLibrary(fileName);
				}
			}
			private Map assemblyInfo=new Map();
			public ArrayList GetNamespaces(Assembly assembly) { //refactor, integrate into LoadNamespaces???
				ArrayList namespaces=new ArrayList();
				if(assemblyInfo.blaHasKeyOj(new Map(assembly.Location))) {
					Map info=(Map)assemblyInfo[new Map(assembly.Location)];
					string timestamp=((Map)info[new Map("timestamp")]).SDotNetStringV();
					if(timestamp.Equals(File.GetCreationTime(assembly.Location).ToString())) {
						Map names=(Map)info[new Map("namespaces")];
						foreach(DictionaryEntry entry in names) {
							string text=((Map)entry.Value).SDotNetStringV();
							namespaces.Add(text);
						}
						return namespaces;
					}
				}
				foreach(Type type in assembly.GetExportedTypes()) {
					if(!namespaces.Contains(type.Namespace)) {
						if(type.Namespace==null) {
							if(!namespaces.Contains("")) {
								namespaces.Add("");
							}
						}
						else {
							namespaces.Add(type.Namespace);
						}
					}
				}
				Map assemblyInfoMap=new Map();
				Map nameSpaceMap=new Map();
				Integer counter=new Integer(0);
				foreach(string na in namespaces) {
					nameSpaceMap[counter]=new Map(na);
					counter++;
				}
				assemblyInfoMap[new Map("namespaces")]=nameSpaceMap;
				assemblyInfoMap[new Map("timestamp")]=new Map(
					File.GetCreationTime(assembly.Location).ToString());
				assemblyInfo[new Map(assembly.Location)]=assemblyInfoMap;
				return namespaces;
			}
			public Map LoadNamespaces(ArrayList assemblies) {
				LazyNamespace root=new LazyNamespace("");
				foreach(Assembly assembly in assemblies) {
					ArrayList names=GetNamespaces(assembly);
					CachedAssembly cachedAssembly=new CachedAssembly(assembly);
					foreach(string name in names) {
						LazyNamespace selected=root;
						if(name=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.metaInstallationPath,"library"))) {
							continue;
						}
						if(name!="") {
							foreach(string subpath in name.Split('.')) {
								if(!selected.namespaces.ContainsKey(subpath)) {
									string fullName=selected.fullName;
									if(fullName!="") {
										fullName+=".";
									}
									fullName+=subpath;
									selected.namespaces[subpath]=new LazyNamespace(fullName);
								}
								selected=(LazyNamespace)selected.namespaces[subpath];
							}
						}
						selected.cachedAssemblies.Add(cachedAssembly);
					}
				}
				
				root.Load();
				return root.cache;
			}
			public static Library library=new Library();
			private Map cash=new Map();
			public static string libraryPath="library"; 
		}
		/* Automatically converts Meta keys of a Map to .NET counterparts. Useful when writing libraries. */
		public class MapAdapter { // TODO: Make this a whole IMap implementation?, if seems useful
			Map map;
			public MapAdapter(Map map) {
				this.map=map;
			}
			public MapAdapter() {
				this.map=new Map();
			}
			public object this[object key] {
				get {
					return Interpreter.ConvertMetaToDotNet(map[Interpreter.ConvertDotNetToMeta(key)]);
				}
				set {
					this.map[Interpreter.ConvertDotNetToMeta(key)]=Interpreter.ConvertDotNetToMeta(value);
				}
			}
		}

		//TODO: cache the ArlojIntegerKeyValues somewhere; put in an "Add" method
		public class Map: IKeyValue, IMap, ICallable, IEnumerable, ISerializeSpecial {
			public static readonly Map parentString=new Map("parent");
			public static readonly Map argString=new Map("arg");
			public static readonly Map sThis=new Map("this");
			public object Argument {
				get {
					return arg;
				}
				set { // TODO: Remove set, maybe?
					arg=value;
				}
			}
			object arg=null;
			public bool IsString {
				get {
					return table.IsString;
				}
			}
			public string SDotNetStringV() { // Refactoring: has a stupid name, Make property
				return table.SDotNetStringV();
			}
			public IMap mParent {
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
			public ArrayList ArlojIntegerKeyValues {
				get {
					return table.ArlojIntegerKeyValues;
				}
			}
			public virtual object this[object key]  {
				get {
					if(key.Equals(parentString)) {
						return mParent;
					}
					else if(key.Equals(argString)) {
						return Argument;
					}
					else if(key.Equals(sThis)) {
						return this;
					}
					else {
						object result=table[key];
						return result;
					}
				}
				set {
					if(value!=null) {
						isHashCashed=false;
						if(key.Equals(sThis)) {
							this.table=((Map)value).table.Clone();
						}
						else {
							object val=value is IMap? ((IMap)value).mCloneV(): value; // TODO: combine with next line
							if(value is IMap) {
								((IMap)val).mParent=this;
							}
							table[key]=val;
						}
					}
				}
			}
			public object Execute() { // TODO: Rename to evaluate
				Expression function=(Expression)EpsCompileV();
				object result;
				result=function.OjEvaluateM(this);
				return result;
			}
			public object ojCallOj(object argument) {
				this.Argument=argument;
				Expression function=(Expression)((Map)this[Expression.sRun]).EpsCompileV();
				object result;
				Interpreter.arguments.Add(argument);
				result=function.OjEvaluateM(this);
				Interpreter.arguments.RemoveAt(Interpreter.arguments.Count-1);
				return result;
			}
			public ArrayList Keys {
				get {
					return table.Keys;
				}
			}
			public IMap mCloneV() {
				Map clone=table.CloneMap();
				clone.mParent=mParent;
				clone.compiled=compiled;
				clone.EtExtent=EtExtent;
				return clone;
			}
			public Expression EpsCompileV()  { // compiled Statements are not cached, only expressions
				if(compiled==null)  {
					if(this.blaHasKeyOj(Meta.Execution.Call.sCall)) {
						compiled=new Call(this);
					}
					else if(this.blaHasKeyOj(Delayed.sDelayed)) { // TODO: could be optimized, but compilation happens seldom
						compiled=new Delayed(this);
					}
					else if(this.blaHasKeyOj(Program.sProgram)) {
						compiled=new Program(this);
					}
					else if(this.blaHasKeyOj(Literal.sLiteral)) {
						compiled=new Literal(this);
					}
					else if(this.blaHasKeyOj(Search.sSearch)) {// TODO: use static expression strings
						compiled=new Search(this);
					}
					else if(this.blaHasKeyOj(Select.sSelect)) {
						compiled=new Select(this);
					}
					else {
						throw new ApplicationException("Cannot compile non-code map.");
					}
				}
//				if(this.EtExtent!=null) {
//					int asdf=0;
//				}		
//				if(compiled is Expression) {
					((Expression)compiled).EtExtent=this.EtExtent;
//				}
				return compiled;
			}
			public bool blaHasKeyOj(object key)  {
				if(key is Map) {
					if(key.Equals(argString)) {
						return this.Argument!=null;
					}
					else if(key.Equals(parentString)) {
						return this.mParent!=null;
					}
					else if(key.Equals(sThis)) {
						return true;
					}
				}
				return table.ContainsKey(key);
			}
			public override bool Equals(object obj) {
				if(Object.ReferenceEquals(obj,this)) {
					return true;
				}
				if(!(obj is Map)) {
					return false;
				}
				return ((Map)obj).table.Equal(table);
			}
			public IEnumerator GetEnumerator() {
				return new MapEnumerator(this);
			}
			public override int GetHashCode()  {
				if(!isHashCashed) {
					hash=this.table.GetHashCode();
					isHashCashed=true;
				}
				return hash;
			}
			private bool isHashCashed=false;
			private int hash;

			Extent extent;
			public Extent EtExtent {
				get {
					return extent;
				}
				set {
					extent=value;
				}
			}
			/* TODO: Move some more logic into constructor instead of in Parser?
			 * There is no clean separation then. But there isn't anyway. I could make 
			 * it so that only the extent gets passed, that's probably best*/
			public Map(string text):this(new StringStrategy(text)) {
			}
			public Map(MapStrategy table) {
				this.table=table;
				this.table.map=this;
			}
			public Map():this(new HybridDictionaryStrategy()) {
			}
			private IMap parent;
			private MapStrategy table;
			public Expression compiled; // why have this at all, why not for statements? probably a question of performance.
			public string Serialize(string indent,string[] functions) {
				if(this.IsString) {
					return indent+"\""+this.SDotNetStringV()+"\""+"\n";
				}
				else {
					return null;
				}
			}
			public abstract class MapStrategy {
				public Map map;
				public MapStrategy Clone() {
					MapStrategy strategy=new HybridDictionaryStrategy();
					foreach(object key in this.Keys) {
						strategy[key]=this[key];
					}
					return strategy;	
				}
				public abstract Map CloneMap();
				public abstract ArrayList ArlojIntegerKeyValues {
					get;
				}
				public abstract bool IsString {
					get;
				}
				
				// TODO: Rename. Reason: This really means something more abstract, more along the lines of,
				// "is this a map that only has integers as children, and maybe also only integers as keys?"
				public abstract string SDotNetStringV();
				public abstract ArrayList Keys {
					get;
				}
				public abstract int Count {
					get;
				}
				public abstract object this[object key]  {
					get;
					set;
				}

				public abstract bool ContainsKey(object key);
				/* Hashcodes must be exactly the same in all MapStrategies. */
				public override int GetHashCode()  {
					int h=0;
					foreach(object key in this.Keys) {
						unchecked {
							h+=key.GetHashCode()*this[key].GetHashCode();
						}
					}
					return h;
				}
				public virtual bool Equal(MapStrategy obj) {
					if(Object.ReferenceEquals(obj,this)) { // check whether this is a clone of the other MapStrategy (not used yet)
						return true;
					}
					if(obj.Count!=this.Count) {
						return false;
					}
					foreach(object key in this.Keys)  {
						if(!obj.ContainsKey(key)||!obj[key].Equals(this[key])) {
							return false;
						}
					}
					return true;
				}
			}
			// TODO: Make this unicode safe:
			public class StringStrategy:MapStrategy {
				// is this really identical with the other strategies? See Hashcode of Integer class to make sure
				public override int GetHashCode() {
					int hash=0;
					for(int i=0;i<text.Length;i++) {//(char c in this.text) {
						hash+=(i+1)*text[i];
					}
					return hash;
				}
				public override bool Equal(MapStrategy obj) {
					if(obj is StringStrategy) {	// TODO: Decide on single exit for methods, might be useful, especially here
						return ((StringStrategy)obj).text.Equals(this.text);
					}
					else {
						return base.Equal(obj);
					}
				}
				public override Map CloneMap() {
					return new Map(new StringStrategy(this));
				}
				public override ArrayList ArlojIntegerKeyValues {
					get {
						ArrayList list=new ArrayList();
						foreach(char c in text) {
							list.Add(new Integer(c));
						}
						return list;
					}
				}
				public override bool IsString {
					get {
						return true;
					}
				}
				public override string SDotNetStringV() {
					return text;
				}
				public override ArrayList Keys {
					get {
						return keys;
					}
				}
				private ArrayList keys=new ArrayList();
				private string text;
				public StringStrategy(StringStrategy clone) {
					this.text=clone.text;
					this.keys=(ArrayList)clone.keys.Clone();
				}
				public StringStrategy(string text) {
					this.text=text;
					for(int i=1;i<=text.Length;i++) { // make this lazy? it won't work with unicode anymore then, though
						keys.Add(new Integer(i));			// TODO: Make this unicode-safe in the first place!
					}
				}
				public override int Count {
					get {
						return text.Length;
					}
				}
				public override object this[object key]  {
					get {
						if(key is Integer) {
							int i=((Integer)key).Int;
							if(i>0 && i<=this.Count) {
								return new Integer(text[i-1]);
							}
						}
						return null;
					}
					set {
						/* StringStrategy gets changed. Fall back on standard strategy because we can't be sure
						 * the map will still be a string afterwards. */
						map.table=this.Clone();
						map.table[key]=value;
					}
				}
				public override bool ContainsKey(object key)  {
					if(key is Integer) {
						return ((Integer)key)>0 && ((Integer)key)<=this.Count;
					}
					else {
						return false;
					}
				}
			}
			/* The standard strategy for maps. */
			public class HybridDictionaryStrategy:MapStrategy {
				ArrayList keys;
				private HybridDictionary table;
				public HybridDictionaryStrategy():this(2) {
				}
				public HybridDictionaryStrategy(int count) {
					this.keys=new ArrayList(count);
					this.table=new HybridDictionary(count);
				}
				public override Map CloneMap() {
					Map clone=new Map(new HybridDictionaryStrategy(this.keys.Count));
					foreach(object key in keys) {
						clone[key]=table[key];
					}
					return clone;
				}
				public override ArrayList ArlojIntegerKeyValues {
					get {
						ArrayList list=new ArrayList();
						for(Integer i=new Integer(1);ContainsKey(i);i++) {
							list.Add(this[i]);
						}
						return list;
					}
				}
				public override bool IsString {
					get {
						if(ArlojIntegerKeyValues.Count>0) {
							try {
								SDotNetStringV();// TODO: a bit of a hack
								return true;
							}
							catch{
							}
						}
						return false;
					}
				}
				public override string SDotNetStringV() { // TODO: looks too complicated
					string text="";
					foreach(object key in this.Keys) {
						if(key is Integer && this.table[key] is Integer) {
							try {
								text+=Convert.ToChar(((Integer)this.table[key]).Int);
							}
							catch {
								throw new MapException(this.map,"Map is not a string");
							}
						}
						else {
							throw new MapException(this.map,"Map is not a string");
						}
					}
					return text;
				}
				public class MapException:ApplicationException { // TODO: Remove or make sense of this
					Map map;
					public MapException(Map map,string message):base(message) {
						this.map=map;
					}
				}
				public override ArrayList Keys {
					get {
						return keys;
					}
				}
				public override int Count {
					get {
						return table.Count;
					}
				}
				public override object this[object key]  {
					get {
						return table[key];
					}
					set {
						if(!this.ContainsKey(key)) {
							keys.Add(key);
						}
						table[key]=value;
					}
				}
				public override bool ContainsKey(object key)  {
					return table.Contains(key);
				}
			}
		}
		public class MapEnumerator: IEnumerator {
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
		public delegate object DelegateCreatedForGenericDelegates();
//		[AttributeUsage(AttributeTargets.Method)]
//		public class MetaLibraryMethodAttribute:Attribute {
//		}
		public class NetMethod: ICallable {
//			public bool blaLibraryMethod=false; // TODO: is this even needen anymore?
			// TODO: Move this to "With" ? Move this to NetContainer?
			public static object ojAssignCollectionMOjOutbla(Map mCollection,object ojCollection,out bool blaSuccess) { // TODO: is blaSuccess needed?
				if(mCollection.ArlojIntegerKeyValues.Count==0) {
					blaSuccess=false;
					return null;
				}
				Type tTarget=ojCollection.GetType();
				MethodInfo mtifAdding=tTarget.GetMethod("Add",new Type[]{mCollection.ArlojIntegerKeyValues[0].GetType()});
				if(mtifAdding!=null) {
					foreach(object oEntry in mCollection.ArlojIntegerKeyValues) { // combine this with Library function "Init"
						mtifAdding.Invoke(ojCollection,new object[]{oEntry});//  call mtifAdding from above!
					}
					blaSuccess=true;
				}
				else {
					blaSuccess=false;
				}

				return ojCollection;
			}
//			public static object DoModifiableCollectionAssignment(Map map,object oldValue,out bool assigned) {
//
//				if(map.ArlojIntegerKeyValues.Count==0) {
//					assigned=false;
//					return null;
//				}
//				Type tTarget=oldValue.GetType();
//				MethodInfo method=tTarget.GetMethod("Add",new Type[]{map.ArlojIntegerKeyValues[0].GetType()});
//				if(method!=null) {
//					foreach(object val in map.ArlojIntegerKeyValues) { // combine this with Library function "Init"
//						method.Invoke(oldValue,new object[]{val});//  call method from above!
//					}
//					assigned=true;
//				}
//				else {
//					assigned=false;
//				}
//
//				return oldValue;
//			}
			// TODO: finally invent a Meta tTarget??? Would be useful here for prefix to Meta,
			// it isn't, after all just any object
			public static object ojConvertParameterOjTOutbla(object ojMeta,Type tParameter,out bool outblaConverted) {
				outblaConverted=true;
				if(tParameter.IsAssignableFrom(ojMeta.GetType())) {
					return ojMeta;
				}
				else if((tParameter.IsSubclassOf(typeof(Delegate))
					||tParameter.Equals(typeof(Delegate))) && (ojMeta is Map)) { // TODO: add check, that the m contains code, not necessarily, think this conversion stuff through completely
					MethodInfo mtifInvoke=tParameter.GetMethod("Invoke",BindingFlags.Instance
						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					Delegate dlgFunction=delFromF(tParameter,mtifInvoke,(Map)ojMeta);
					return dlgFunction;
				}
				else if(tParameter.IsArray && ojMeta is IMap && ((Map)ojMeta).ArlojIntegerKeyValues.Count!=0) {// TODO: cheating, not very understandable
					try {
						Type tElements=tParameter.GetElementType();
						Map mArgument=((Map)ojMeta);
						ArrayList arlArgument=mArgument.ArlojIntegerKeyValues;
						Array arArgument=Array.CreateInstance(tElements,arlArgument.Count);
						for(int i=0;i<arlArgument.Count;i++) {
							arArgument.SetValue(arlArgument[i],i);
						}
						return arArgument;
					}
					catch {
					}
				}
				else {
					bool outblaParamConverted; // TODO: refactor with outblaConverted
					object result=Interpreter.ConvertMetaToDotNet(ojMeta,tParameter,out outblaParamConverted);
					if(outblaParamConverted) {
						return result;
					}
				}
				outblaConverted=false;
				return null;
			}
//			// TODO: finally invent a Meta tTarget??? Would be useful here for prefix to Meta,
//			// it isn't, after all just any object
//			public static object oConvertParameterOTypOutb(object ojMeta,Type tParameter,out bool outblaConverted) {
//				outblaConverted=true;
//				if(tParameter.IsAssignableFrom(ojMeta.GetType())) {
//					return ojMeta;
//				}
//				else if((tParameter.IsSubclassOf(tTargetof(Delegate))
//					||tParameter.Equals(tTargetof(Delegate))) && (ojMeta is Map)) { // TODO: add check, that the map contains code, not necessarily, think this conversion stuff through completely
//					MethodInfo m=tParameter.GetMethod("Invoke",BindingFlags.Instance
//						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
//					Delegate dlgFunction=delFromF(tParameter,m,(Map)ojMeta);
//					return dlgFunction;
//				}
//				else if(tParameter.IsArray && ojMeta is IMap && ((Map)ojMeta).ArlojIntegerKeyValues.Count!=0) {// TODO: cheating, not very understandable
//					try {
//						Type arrayType=tParameter.GetElementType();
//						Map map=((Map)ojMeta);
//						ArrayList mapValues=map.ArlojIntegerKeyValues;
//						Array array=Array.CreateInstance(arrayType,mapValues.Count);
//						for(int i=0;i<mapValues.Count;i++) {
//							array.SetValue(mapValues[i],i);
//						}
//						return array;
//					}
//					catch {
//					}
//				}
//				else {
//					bool isConverted; // TODO: refactor with outblaConverted
//					object result=Interpreter.ConvertMetaToDotNet(ojMeta,tParameter,out isConverted);
//					if(isConverted) {
//						return result;
//					}
//				}
//				outblaConverted=false;
//				return null;
//			}
//			public static object oConvertParameterOTypOutb(object ojMeta,Type parameter,out bool outblaConverted) {
//				outblaConverted=true;
//				if(parameter.IsAssignableFrom(ojMeta.GetType())) {
//					return ojMeta;
//				}
//				else if((parameter.IsSubclassOf(tTargetof(Delegate))
//					||parameter.Equals(tTargetof(Delegate))) && (ojMeta is Map)) { // TODO: add check, that the map contains code, not necessarily, think this conversion stuff through completely
//					MethodInfo m=parameter.GetMethod("Invoke",BindingFlags.Instance
//						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
//					Delegate del=delFromF(parameter,m,(Map)ojMeta);
//					return del;
//				}
//				else if(parameter.IsArray && ojMeta is IMap && ((Map)ojMeta).ArlojIntegerKeyValues.Count!=0) {// TODO: cheating, not very understandable
//					try {
//						Type arrayType=parameter.GetElementType();
//						Map map=((Map)ojMeta);
//						ArrayList mapValues=map.ArlojIntegerKeyValues;
//						Array array=Array.CreateInstance(arrayType,mapValues.Count);
//						for(int i=0;i<mapValues.Count;i++) {
//							array.SetValue(mapValues[i],i);
//						}
//						return array;
//					}
//					catch {
//					}
//				}
//				else {
//					bool isConverted; // TODO: refactor with outblaConverted
//					object result=Interpreter.ConvertMetaToDotNet(ojMeta,parameter,out isConverted);
//					if(isConverted) {
//						return result;
//					}
//				}
//				outblaConverted=false;
//				return null;
//			}
			// TODO: This should really be put into just one method!!!!!!!
//			public object ojCallSingleArgumentOjOutbla(object ojArgument,out bool outblaCalled) {
//				outblaCalled=true;
//				try {
//					Map mArgument=new Map();
//					mArgument[new Integer(1)]=ojArgument;
//					return ojCallMultiArgumentOj(mArgument);
//				}
//				catch {
//					outblaCalled=false;
//					return null;
//				}
//			}
//			public object ojCallOj(object ojArgument) {
//				bool outblaCalled=false;
//				object ojResult;
//				ojResult=ojCallSingleArgumentOjOutbla(ojArgument,out outblaCalled);
//				if(!outblaCalled) {
//					ojResult=ojCallMultiArgumentOj(ojArgument);
//				}
//				ojResult=ojCallMultiArgumentOj(ojArgument);
//				return ojResult;
//			}
//			public object ojCallOj(object ojArgument) {
//				bool outblaCalled=false;
//				object ojResult;
//				ojResult=ojCallSingleArgumentOjOutbla(ojArgument,out outblaCalled);
//				if(!outblaCalled) {
//					ojResult=ojCallMultiArgumentOj(ojArgument);
//				}
//				return ojResult;
//			}
			public object ojCallOj(object ojArgument) {
				//				if(tTarget.Name.EndsWith("PositionalNoConversion")) {
				//					int asdf=0;
				//				}
				if(this.tTarget.Name.EndsWith("IndexerNoConversion") && this.sName.StartsWith("GetResultFromDelegate")) {
					int asdf=0;
				}
				object ojReturn=null;
				object ojResult=null;
				// TODO: this will have to be refactored, but later, after feature creep

				// try to call with just one argument:
				ArrayList arlOneArgumentMethods=new ArrayList();
				foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
					if(mtbCurrent.GetParameters().Length==1) { // don't match if different parameter list length
						arlOneArgumentMethods.Add(mtbCurrent);
					}
				}
				bool blaExecuted=false;
				foreach(MethodBase mtbCurrent in arlOneArgumentMethods) {
//					ArrayList arlArguments=new ArrayList();
//					bool blaArgumentsMatched=true;
//					ParameterInfo[] arPrmtifParameters=mtbCurrent.GetParameters();
//					for(int i=0;blaArgumentsMatched && i<arPrmtifParameters.Length;i++) {
//						arlArguments.Add(ojConvertParameterOjTOutbla(arlOArguments[i],arPrmtifParameters[i].ParameterType,out blaArgumentsMatched));
//					}
					bool blaConverted;
					object ojParameter=ojConvertParameterOjTOutbla(ojArgument,mtbCurrent.GetParameters()[0].ParameterType,out blaConverted);
					if(blaConverted) {
						if(mtbCurrent is ConstructorInfo) {
							ojReturn=((ConstructorInfo)mtbCurrent).Invoke(new object[] {ojParameter});
						}
						else {
							ojReturn=mtbCurrent.Invoke(ojTarget,new object[] {ojParameter});
						}
						blaExecuted=true;// remove, use blaArgumentsMatched instead
						break;
					}
				}





				// TODO: check this for every meb:
				// introduce own mebinfo class? that does the calling, maybe??? dynamic cast might become a performance
				// problem, but I doubt it, so what?
				//				if(blaLibraryMethod) {
				//					if(arMtbOverloadedMethods[0] is ConstructorInfo) {
				//						// TODO: remove this
				//						ojResult=((ConstructorInfo)arMtbOverloadedMethods[0]).Invoke(new object[] {ojArgument}); 
				//					}
				//					else {
				//						try {
				//							ojResult=arMtbOverloadedMethods[0].Invoke(ojTarget,new object[] {ojArgument});
				//						}
				//						catch {
				//							throw new ApplicationException("Could not invoke "+sName+".");
				//						}
				//					}
				//				}
				//				else {
				if(!blaExecuted) {
					ArrayList arlOArguments=((IMap)ojArgument).ArlojIntegerKeyValues;
					ArrayList arlMtifRightNumberArguments=new ArrayList();
					foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
						if(arlOArguments.Count==mtbCurrent.GetParameters().Length) { // don't match if different parameter list length
							if(arlOArguments.Count==((IMap)ojArgument).Keys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
								arlMtifRightNumberArguments.Add(mtbCurrent);
							}
						}
					}
					if(arlMtifRightNumberArguments.Count==0) {
						int asdf=0;//throw new ApplicationException("No methods with the right number of arguments.");// TODO: Just a quickfix, really
					}
					foreach(MethodBase mtbCurrent in arlMtifRightNumberArguments) {
						ArrayList arlArguments=new ArrayList();
						bool blaArgumentsMatched=true;
						ParameterInfo[] arPrmtifParameters=mtbCurrent.GetParameters();
						for(int i=0;blaArgumentsMatched && i<arPrmtifParameters.Length;i++) {
							arlArguments.Add(ojConvertParameterOjTOutbla(arlOArguments[i],arPrmtifParameters[i].ParameterType,out blaArgumentsMatched));
						}
						if(blaArgumentsMatched) {
							if(mtbCurrent is ConstructorInfo) {
								ojReturn=((ConstructorInfo)mtbCurrent).Invoke(arlArguments.ToArray());
							}
							else {
								ojReturn=mtbCurrent.Invoke(ojTarget,arlArguments.ToArray());
							}
							blaExecuted=true;// remove, use blaArgumentsMatched instead
							break;
						}
					}
				}
				// TODO: ojResult / ojReturn is duplication
				ojResult=ojReturn; // mess, why is this here? put in else after the next if
				// make this safe
				//					if(!blaExecuted && arMtbOverloadedMethods[0] is ConstructorInfo) { // TODO: why is this needed, can't constructors be called normally? Ah, now I get it, this is for automatical initializing of new classes. This is, however, very confusing, and should, in my opinion be remove, if possible into a library. .NET stuff will not be used directly as often as I first thought, I think, so this stuff isn't so important
				//						object ojToBeInitialized=new NetMethod(tTarget).ojCallMultiArgumentOj(new Map());
				//						//						object ojToBeInitialized=new NetMethod(tTarget).ojCallOj(new Map());
				//						ojResult=with(ojToBeInitialized,((Map)ojArgument));
				//					}// TODO: somehow determine if no method has been called at all and throw an exception then
				//					else if(arlMtifRightNumberArguments.Count==0) {
				//						throw new ApplicationException("Method "+sName+" could not be invoked.");
				//					}
				//				}		
				if(ojResult==null) {
					int asdf=0;
				}
				return Interpreter.ConvertDotNetToMeta(ojResult);
			}
//			public object ojCallOj(object ojArgument) {
////				if(tTarget.Name.EndsWith("PositionalNoConversion")) {
////					int asdf=0;
////				}
//				object ojResult=null;
//				// TODO: check this for every meb:
//				// introduce own mebinfo class? that does the calling, maybe??? dynamic cast might become a performance
//				// problem, but I doubt it, so what?
////				if(blaLibraryMethod) {
////					if(arMtbOverloadedMethods[0] is ConstructorInfo) {
////						// TODO: remove this
////						ojResult=((ConstructorInfo)arMtbOverloadedMethods[0]).Invoke(new object[] {ojArgument}); 
////					}
////					else {
////						try {
////							ojResult=arMtbOverloadedMethods[0].Invoke(ojTarget,new object[] {ojArgument});
////						}
////						catch {
////							throw new ApplicationException("Could not invoke "+sName+".");
////						}
////					}
////				}
////				else {
//					ArrayList arlOArguments=((IMap)ojArgument).ArlojIntegerKeyValues;
//					object ojReturn=null;
//					ArrayList arlMtifRightNumberArguments=new ArrayList();
//					foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
//						if(arlOArguments.Count==mtbCurrent.GetParameters().Length) { // don't match if different parameter list length
//							if(arlOArguments.Count==((IMap)ojArgument).Keys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
//								arlMtifRightNumberArguments.Add(mtbCurrent);
//							}
//						}
//					}
//					if(arlMtifRightNumberArguments.Count==0) {
//						int asdf=0;//throw new ApplicationException("No methods with the right number of arguments.");// TODO: Just a quickfix, really
//					}
//					bool blaExecuted=false;
//					foreach(MethodBase mtbCurrent in arlMtifRightNumberArguments) {
//						ArrayList arlArguments=new ArrayList();
//						bool blaArgumentsMatched=true;
//						ParameterInfo[] arPrmtifParameters=mtbCurrent.GetParameters();
//						for(int i=0;blaArgumentsMatched && i<arPrmtifParameters.Length;i++) {
//							arlArguments.Add(ojConvertParameterOjTOutbla(arlOArguments[i],arPrmtifParameters[i].ParameterType,out blaArgumentsMatched));
//						}
//						if(blaArgumentsMatched) {
//							if(mtbCurrent is ConstructorInfo) {
//								ojReturn=((ConstructorInfo)mtbCurrent).Invoke(arlArguments.ToArray());
//							}
//							else {
//								ojReturn=mtbCurrent.Invoke(ojTarget,arlArguments.ToArray());
//							}
//							blaExecuted=true;// remove, use blaArgumentsMatched instead
//							break;
//						}
//					}
//					// TODO: ojResult / ojReturn is duplication
//					ojResult=ojReturn; // mess, why is this here? put in else after the next if
//					// make this safe
////					if(!blaExecuted && arMtbOverloadedMethods[0] is ConstructorInfo) { // TODO: why is this needed, can't constructors be called normally? Ah, now I get it, this is for automatical initializing of new classes. This is, however, very confusing, and should, in my opinion be remove, if possible into a library. .NET stuff will not be used directly as often as I first thought, I think, so this stuff isn't so important
////						object ojToBeInitialized=new NetMethod(tTarget).ojCallMultiArgumentOj(new Map());
////						//						object ojToBeInitialized=new NetMethod(tTarget).ojCallOj(new Map());
////						ojResult=with(ojToBeInitialized,((Map)ojArgument));
////					}// TODO: somehow determine if no method has been called at all and throw an exception then
////					else if(arlMtifRightNumberArguments.Count==0) {
////						throw new ApplicationException("Method "+sName+" could not be invoked.");
////					}
////				}		
//				if(ojResult==null) {
//					int asdf=0;
//				}
//				return Interpreter.ConvertDotNetToMeta(ojResult);
//			}
//			public object ojCallOj(object argument) {
//				if(this.sName=="Write") {
//					int asdf=0;
//				}
//				object result=null;
//				// TODO: check this for every method:
//				// introduce own methodinfo class? that does the calling, maybe??? dynamic cast might become a performance
//				// problem, but I doubt it, so what?
//				if(blaLibraryMethod) {
//					if(arMtbOverloadedMethods[0] is ConstructorInfo) {
//						// TODO: Comment this properly: kcall arMtbOverloadedMethods without arguments, ugly and redundant, because Invoke is not compatible between
//						// constructor and normal method
//						result=((ConstructorInfo)arMtbOverloadedMethods[0]).Invoke(new object[] {argument}); 
//					}
//					else {
//						try {
//							if(this.sName=="while") {
//								int asdf=0;
//							}
//							result=arMtbOverloadedMethods[0].Invoke(ojTarget,new object[] {argument});
//						}
//						catch {
//							throw new ApplicationException("Could not invoke "+this.sName+".");
//						}
//					}
//				}
//				else {
//					ArrayList argumentList=((IMap)argument).ArlojIntegerKeyValues;
//					object returnValue=null;
//					ArrayList sameLengthMethods=new ArrayList();
//					foreach(MethodBase method in arMtbOverloadedMethods) {
//						if(argumentList.Count==method.GetParameters().Length) { // don't match if different parameter list length
//							if(argumentList.Count==((IMap)argument).Keys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
//								sameLengthMethods.Add(method);
//							}
//						}
//					}
//					bool executed=false;
//					foreach(MethodBase method in sameLengthMethods) {
//						ArrayList args=new ArrayList();
//						bool argumentsMatched=true;
//						ParameterInfo[] parameters=method.GetParameters();
//						for(int i=0;argumentsMatched && i<parameters.Length;i++) {
//							args.Add(ojConvertParameterOjTOutbla(argumentList[i],parameters[i].ParameterType,out argumentsMatched));
//						}
//						if(argumentsMatched) {
//							if(method is ConstructorInfo) {
//								returnValue=((ConstructorInfo)method).Invoke(args.ToArray());
//							}
//							else {
//								if(method.sName.Equals("Invoke")) {
//									int asdf=0;
//								}
//								returnValue=method.Invoke(ojTarget,args.ToArray());
//							}
//							executed=true;// remove, use argumentsMatched instead
//							break;
//						}
//					}
//					result=returnValue; // mess, why is this here? put in else after the next if
//					// make this safe
//					if(!executed && arMtbOverloadedMethods[0] is ConstructorInfo) {
//						object o=new NetMethod(tTarget).ojCallOj(new Map());
//						result=with(o,((Map)argument));
//					}
//				}		
//				return Interpreter.ConvertDotNetToMeta(result);
//			}
			// TODO: Refactor, include 
//			public static object with(object obj,IMap map) {
//				NetObject netObject=new NetObject(obj);
//				foreach(DictionaryEntry entry in map) {
//					netObject[entry.Key]=entry.Value;
//				}
//				return obj;
//			}
//			public static object with(object obj,IMap map) {
//				NetObject netObject=new NetObject(obj);
//				foreach(DictionaryEntry entry in map) {
//					netObject[entry.Key]=entry.Value;
//				}
//				return obj;
//			}
			/* Create a delegate of a certain tTarget that calls a Meta function. */
			public static Delegate delFromF(Type delegateType,MethodInfo method,Map code) { // TODO: delegateType, methode, redundant?
				code.mParent=(IMap)Interpreter.arlMCallers[Interpreter.arlMCallers.Count-1];
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
				source+="object result=callable.ojCallOj(arg);";
				if(method!=null) {
					if(!method.ReturnType.Equals(typeof(void))) {
						source+="return ("+returnTypeName+")";
						source+="Interpreter.ConvertMetaToDotNet(result,typeof("+returnTypeName+"));"; // does conversion even make sense here? Must be outblaConverted back anyway.
					}
				}
				else {
					source+="return";
					source+=" result;";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				string ojMetaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",ojMetaDllLocation});
				assemblyNames.AddRange(Interpreter.loadedAssemblies);
				CompilerParameters  options=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults results=compiler.CompileAssemblyFromSource(options,source);
				Type containerClass=results.CompiledAssembly.GetType("EventHandlerContainer",true);
				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
																																			  code});
				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
				if(method==null) {
					delegateType=typeof(DelegateCreatedForGenericDelegates);
				}
				Delegate del=Delegate.CreateDelegate(delegateType,
					container,"EventHandlerMethod");
				return del;
			}
//			public static Delegate delFromF(Type delegateType,MethodInfo method,Map code) { // TODO: delegateType, methode, redundant?
//				code.mParent=(IMap)Interpreter.arlMCallers[Interpreter.arlMCallers.Count-1];
//				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
//				ICodeEpsCompileVr compiler=codeProvider.CreateEpsCompileVr();
//				string returnTypeName;
//				if(method==null) {
//					returnTypeName="object";
//				}
//				else {
//					returnTypeName=method.ReturnType.Equals(tTargetof(void)) ? "void":method.ReturnType.FullName;
//				}
//				string source="using System;using Meta.Types;using Meta.Execution;";
//				source+="public class EventHandlerContainer{public "+returnTypeName+" EventHandlerMethod";
//				int counter=1;
//				string argumentList="(";
//				string argumentAdding="Map arg=new Map();";
//				if(method!=null) {
//					foreach(ParameterInfo parameter in method.GetParameters()) {
//						argumentList+=parameter.ParameterType.FullName+" arg"+counter;
//						argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
//						if(counter<method.GetParameters().Length) {
//							argumentList+=",";
//						}
//						counter++;
//					}
//				}
//				argumentList+=")";
//				source+=argumentList+"{";
//				source+=argumentAdding;
//				source+="object result=callable.ojCallOj(arg);";
//				if(method!=null) {
//					if(!method.ReturnType.Equals(tTargetof(void))) {
//						source+="return ("+returnTypeName+")";
//						source+="Interpreter.ConvertMetaToDotNet(result,tTargetof("+returnTypeName+"));"; // does conversion even make sense here? Must be outblaConverted back anyway.
//					}
//				}
//				else {
//					source+="return";
//					source+=" result;";
//				}
//				source+="}";
//				source+="private Map callable;";
//				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
//				string ojMetaDllLocation=Assembly.GetAssembly(tTargetof(Map)).Location;
//				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",ojMetaDllLocation});
//				assemblyNames.AddRange(Interpreter.loadedAssemblies);
//				EpsCompileVrParameters options=new EpsCompileVrParameters((string[])assemblyNames.ToArray(tTargetof(string)));
//				EpsCompileVrResults results=compiler.EpsCompileVAssemblyFromSource(options,source);
//				Type containerClass=results.EpsCompileVdAssembly.GetType("EventHandlerContainer",true);
//				object container=containerClass.GetConstructor(new Type[]{tTargetof(Map)}).Invoke(new object[] {
//																																			  code});
//				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
//				if(method==null) {
//					delegateType=tTargetof(DelegateCreatedForGenericDelegates);
//				}
//				Delegate del=Delegate.delFromF(delegateType,
//					container,"EventHandlerMethod");
//				return del;
//			}
			private void nInitializeSOjT(string sName,object ojTarget,Type tTarget) {
				this.sName=sName;
				this.ojTarget=ojTarget;
				this.tTarget=tTarget;
				ArrayList arlMtbMethods;
				if(sName==".ctor") {
					arlMtbMethods=new ArrayList(tTarget.GetConstructors());
				}
				else {
					arlMtbMethods=new ArrayList(tTarget.GetMember(sName,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
				}
				arlMtbMethods.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove
				// found out, it's for Console.WriteLine, where Console.WriteLine(object)
				// would otherwise come before Console.WriteLine(string)
				// not a good solution, though

				// TODO: Get rid of this Reversion shit! Find a fix for this problem. Need to think about
				// it. maybe restrict overloads, create preference arlMtbMethods, all quite complicated
				// research the number and nature of such arMtbOverloadedMethods as Console.WriteLine
				arMtbOverloadedMethods=(MethodBase[])arlMtbMethods.ToArray(typeof(MethodBase));
//				if(arMtbOverloadedMethods.Length==1 && arMtbOverloadedMethods[0].GetCustomAttributes(typeof(MetaLibraryMethodAttribute),false).Length!=0) {
//					this.blaLibraryMethod=true;
//				}
			}
//			private void Initialize(string name,object ojTarget,Type tTarget) {
//				this.sName=name;
//				this.ojTarget=ojTarget;
//				this.tTarget=tTarget;
//				ArrayList list;
//				if(name==".ctor") {
//					list=new ArrayList(tTarget.GetConstructors());
//				}
//				else {
//					list=new ArrayList(tTarget.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
//				}
//				list.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove
//				// found out, it's for Console.WriteLine, where Console.WriteLine(object)
//				// would otherwise come before Console.WriteLine(string)
//				// not a good solution, though
//
//				// TODO: Get rid of this Reversion shit! Find a fix for this problem. Need to think about
//				// it. maybe restrict overloads, create preference list, all quite complicated
//				// research the number and nature of such arMtbOverloadedMethods as Console.WriteLine
//				arMtbOverloadedMethods=(MethodBase[])list.ToArray(tTargetof(MethodBase));
//				if(arMtbOverloadedMethods.Length==1 && arMtbOverloadedMethods[0].GetCustomAttributes(tTargetof(MetaLibraryMethodAttribute),false).Length!=0) {
//					this.blaLibraryMethod=true;
//				}
//			}
			public NetMethod(string name,object ojTarget,Type tTarget) {
				this.nInitializeSOjT(name,ojTarget,tTarget);
			}
//			public NetMethod(string name,object ojTarget,Type tTarget) {
//				this.Initialize(name,ojTarget,tTarget);
//			}
			public NetMethod(Type tTarget) {
				this.nInitializeSOjT(".ctor",null,tTarget);
			}
//			public NetMethod(Type tTarget) {
//				this.Initialize(".ctor",null,tTarget);
//			}
			public override bool Equals(object ojToCompare) {
				if(ojToCompare is NetMethod) {
					NetMethod nmtToCompare=(NetMethod)ojToCompare;
					if(nmtToCompare.ojTarget==ojTarget && nmtToCompare.sName.Equals(sName) && nmtToCompare.tTarget.Equals(tTarget)) {
						return true;
					}
					else {
						return false;
					}
				}
				else {
					return false;
				}
			}
//			public override bool Equals(object obj) {
//				if(obj is NetMethod) {
//					NetMethod method=(NetMethod)obj;
//					if(method.ojTarget==ojTarget && method.sName.Equals(name) && method.tTarget.Equals(tTarget)) {
//						return true;
//					}
//				}
//				return false;
//			}
			public override int GetHashCode() {
				unchecked {
					int itgHash=sName.GetHashCode()*tTarget.GetHashCode();
					if(ojTarget!=null) {
						itgHash=itgHash*ojTarget.GetHashCode();
					}
					return itgHash;
				}
			}
//			public override int GetHashCode() {
//				unchecked {
//					int hash=name.GetHashCode()*tTarget.GetHashCode();
//					if(ojTarget!=null) {
//						hash=hash*ojTarget.GetHashCode();
//					}
//					return hash;
//				}
//			}
			private string sName;
			protected object ojTarget;
			protected Type tTarget;

			public MethodBase[] arMtbOverloadedMethods;

//			private string name;
//			protected object ojTarget;
//			protected Type tTarget;
//			public MethodBase[] arMtbOverloadedMethods;

		}
//		public class NetMethod: ICallable {
//			public bool blaLibraryMethod=false;
//			// TODO: Move this to "With" ? Move this to NetContainer?
//			public static object DoModifiableCollectionAssignment(Map map,object oldValue,out bool assigned) {
//
//				if(map.ArlojIntegerKeyValues.Count==0) {
//					assigned=false;
//					return null;
//				}
//				Type type=oldValue.GetType();
//				MethodInfo method=type.GetMethod("Add",new Type[]{map.ArlojIntegerKeyValues[0].GetType()});
//				if(method!=null) {
//					foreach(object val in map.ArlojIntegerKeyValues) { // combine this with Library function "Init"
//						method.Invoke(oldValue,new object[]{val});//  call method from above!
//					}
//					assigned=true;
//				}
//				else {
//					assigned=false;
//				}
//
//				return oldValue;
//			}
//			public static object oConvertParameterOTypRb(object meta,Type parameter,out bool converted) {
//				converted=true;
//				if(parameter.IsAssignableFrom(meta.GetType())) {
//					return meta;
//				}
//				else if((parameter.IsSubclassOf(typeof(Delegate))
//					||parameter.Equals(typeof(Delegate))) && (meta is Map)) { // TODO: add check, that the map contains code, not necessarily, think this conversion stuff through completely
//					MethodInfo m=parameter.GetMethod("Invoke",BindingFlags.Instance
//						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
//					Delegate del=CreateDelegate(parameter,m,(Map)meta);
//					return del;
//				}
//				else if(parameter.IsArray && meta is IMap && ((Map)meta).ArlojIntegerKeyValues.Count!=0) {// TODO: cheating, not very understandable
//					try {
//						Type arrayType=parameter.GetElementType();
//						Map map=((Map)meta);
//						ArrayList mapValues=map.ArlojIntegerKeyValues;
//						Array array=Array.CreateInstance(arrayType,mapValues.Count);
//						for(int i=0;i<mapValues.Count;i++) {
//							array.SetValue(mapValues[i],i);
//						}
//						return array;
//					}
//					catch {
//					}
//				}
//				else {
//					bool isConverted; // TODO: refactor with converted
//					object result=Interpreter.ConvertMetaToDotNet(meta,parameter,out isConverted);
//					if(isConverted) {
//						return result;
//					}
//				}
//				converted=false;
//				return null;
//			}
//			public object Call(object argument) {
//				if(this.name=="Write") {
//					int asdf=0;
//				}
//				object result=null;
//				// TODO: check this for every method:
//				// introduce own methodinfo class? that does the calling, maybe??? dynamic cast might become a performance
//				// problem, but I doubt it, so what?
//				if(blaLibraryMethod) {
//					if(methods[0] is ConstructorInfo) {
//						// TODO: Comment this properly: kcall methods without arguments, ugly and redundant, because Invoke is not compatible between
//						// constructor and normal method
//						result=((ConstructorInfo)methods[0]).Invoke(new object[] {argument}); 
//					}
//					else {
//						try {
//							if(this.name=="while") {
//								int asdf=0;
//							}
//							result=methods[0].Invoke(target,new object[] {argument});
//						}
//						catch {
//							throw new ApplicationException("Could not invoke "+this.name+".");
//						}
//					}
//				}
//				else {
//					ArrayList argumentList=((IMap)argument).ArlojIntegerKeyValues;
//					object returnValue=null;
//					ArrayList sameLengthMethods=new ArrayList();
//					foreach(MethodBase method in methods) {
//						if(argumentList.Count==method.GetParameters().Length) { // don't match if different parameter list length
//							if(argumentList.Count==((IMap)argument).Keys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
//								sameLengthMethods.Add(method);
//							}
//						}
//					}
//					bool executed=false;
//					foreach(MethodBase method in sameLengthMethods) {
//						ArrayList args=new ArrayList();
//						bool argumentsMatched=true;
//						ParameterInfo[] parameters=method.GetParameters();
//						for(int i=0;argumentsMatched && i<parameters.Length;i++) {
//							args.Add(oConvertParameterOTypRb(argumentList[i],parameters[i].ParameterType,out argumentsMatched));
//						}
//						if(argumentsMatched) {
//							if(method is ConstructorInfo) {
//								returnValue=((ConstructorInfo)method).Invoke(args.ToArray());
//							}
//							else {
//								if(method.Name.Equals("Invoke")) {
//									int asdf=0;
//								}
//								returnValue=method.Invoke(target,args.ToArray());
//							}
//							executed=true;// remove, use argumentsMatched instead
//							break;
//						}
//					}
//					result=returnValue; // mess, why is this here? put in else after the next if
//					// make this safe
//					if(!executed && methods[0] is ConstructorInfo) {
//						object o=new NetMethod(type).Call(new Map());
//						result=with(o,((Map)argument));
//					}
//				}		
//				return Interpreter.ConvertDotNetToMeta(result);
//			}
//			// TODO: Refactor, include 
//			public static object with(object obj,IMap map) {
//				NetObject netObject=new NetObject(obj);
//				foreach(DictionaryEntry entry in map) {
//					netObject[entry.Key]=entry.Value;
//				}
//				return obj;
//			}
//			/* Create a delegate of a certain type that calls a Meta function. */
//			public static Delegate CreateDelegate(Type delegateType,MethodInfo method,Map code) { // TODO: delegateType, methode, redundant?
//				code.mParent=(IMap)Interpreter.arlMCallers[Interpreter.arlMCallers.Count-1];
//				CSharpCodeProvider codeProvider=new CSharpCodeProvider();
//				ICodeEpsCompileVr compiler=codeProvider.CreateEpsCompileVr();
//				string returnTypeName;
//				if(method==null) {
//					returnTypeName="object";
//				}
//				else {
//					returnTypeName=method.ReturnType.Equals(typeof(void)) ? "void":method.ReturnType.FullName;
//				}
//				string source="using System;using Meta.Types;using Meta.Execution;";
//				source+="public class EventHandlerContainer{public "+returnTypeName+" EventHandlerMethod";
//				int counter=1;
//				string argumentList="(";
//				string argumentAdding="Map arg=new Map();";
//				if(method!=null) {
//					foreach(ParameterInfo parameter in method.GetParameters()) {
//						argumentList+=parameter.ParameterType.FullName+" arg"+counter;
//						argumentAdding+="arg[new Integer("+counter+")]=arg"+counter+";";
//						if(counter<method.GetParameters().Length) {
//							argumentList+=",";
//						}
//						counter++;
//					}
//				}
//				argumentList+=")";
//				source+=argumentList+"{";
//				source+=argumentAdding;
//				source+="object result=callable.Call(arg);";
//				if(method!=null) {
//					if(!method.ReturnType.Equals(typeof(void))) {
//						source+="return ("+returnTypeName+")";
//						source+="Interpreter.ConvertMetaToDotNet(result,typeof("+returnTypeName+"));"; // does conversion even make sense here? Must be converted back anyway.
//					}
//				}
//				else {
//					source+="return";
//					source+=" result;";
//				}
//				source+="}";
//				source+="private Map callable;";
//				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
//				string metaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;
//				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",metaDllLocation});
//				assemblyNames.AddRange(Interpreter.loadedAssemblies);
//				EpsCompileVrParameters options=new EpsCompileVrParameters((string[])assemblyNames.ToArray(typeof(string)));
//				EpsCompileVrResults results=compiler.EpsCompileVAssemblyFromSource(options,source);
//				Type containerClass=results.EpsCompileVdAssembly.GetType("EventHandlerContainer",true);
//				object container=containerClass.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
//																																			  code});
//				MethodInfo m=container.GetType().GetMethod("EventHandlerMethod");
//				if(method==null) {
//					delegateType=typeof(DelegateCreatedForGenericDelegates);
//				}
//				Delegate del=Delegate.CreateDelegate(delegateType,
//				container,"EventHandlerMethod");
//				return del;
//			}
//			private void Initialize(string name,object target,Type type) {
//				this.name=name;
//				this.target=target;
//				this.type=type;
//				ArrayList list;
//				if(name==".ctor") {
//					list=new ArrayList(type.GetConstructors());
//				}
//				else {
//					list=new ArrayList(type.GetMember(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
//				}
//				list.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove
//									 // found out, it's for Console.WriteLine, where Console.WriteLine(object)
//									 // would otherwise come before Console.WriteLine(string)
//									 // not a good solution, though
//
//									// TODO: Get rid of this Reversion shit! Find a fix for this problem. Need to think about
//									// it. maybe restrict overloads, create preference list, all quite complicated
//									// research the number and nature of such methods as Console.WriteLine
//				methods=(MethodBase[])list.ToArray(typeof(MethodBase));
//				if(methods.Length==1 && methods[0].GetCustomAttributes(typeof(MetaLibraryMethodAttribute),false).Length!=0) {
//					this.blaLibraryMethod=true;
//				}
//			}
//			public NetMethod(string name,object target,Type type) {
//				this.Initialize(name,target,type);
//			}
//			public NetMethod(Type type) {
//				this.Initialize(".ctor",null,type);
//			}
//			public override bool Equals(object obj) {
//				if(obj is NetMethod) {
//					NetMethod method=(NetMethod)obj;
//					if(method.target==target && method.name.Equals(name) && method.type.Equals(type)) {
//						return true;
//					}
//				}
//				return false;
//			}
//			public override int GetHashCode() {
//				unchecked {
//					int hash=name.GetHashCode()*type.GetHashCode();
//					if(target!=null) {
//						hash=hash*target.GetHashCode();
//					}
//					return hash;
//				}
//			}
//			private string name;
//			protected object target;
//			protected Type type;
//			public MethodBase[] methods;
//
//		}
		public class NetClass: NetContainer, IKeyValue,ICallable {
			[DontSerializeFieldOrProperty]
			public NetMethod constructor;
			public NetClass(Type type):base(null,type) {
				this.constructor=new NetMethod(this.type);
			}
			public object ojCallOj(object argument) {
				return constructor.ojCallOj(argument);
			}
		}
		/* Representation of a .NET object. */
		public class NetObject: NetContainer, IKeyValue {
			public NetObject(object obj):base(obj,obj.GetType()) {
			}
			public override string ToString() {
				return obj.ToString();
			}
		}
		/* Base class for NetObject and NetClass. */
		public abstract class NetContainer: IKeyValue, IEnumerable,ISerializeSpecial {
			public bool blaHasKeyOj(object key) {
				if(key is Map) {
					if(((Map)key).IsString) {
						string text=((Map)key).SDotNetStringV();
						if(type.GetMember((string)key,
							BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
							return true;
						}
					}
				}
				NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
				Map arguments=new Map();
				arguments[new Integer(1)]=key;
				try {
					indexerMethod.ojCallOj(arguments);
					return true;
				}
				catch(Exception) {
					return false;
				}
			}
			public IEnumerator GetEnumerator() {
				return Table.GetEnumerator();
			}
			// TODO: why does NetContainer have a parent when it isn't ever used?
			public IKeyValue Parent {
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
				get {
					if(key is Map && ((Map)key).IsString) {
						string text=((Map)key).SDotNetStringV();
						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								return new NetMethod(text,obj,type);
							}
							if(members[0] is FieldInfo) {
								// convert arrays to maps here?
								return Interpreter.ConvertDotNetToMeta(type.GetField(text).GetValue(obj));
							}
							else if(members[0] is PropertyInfo) {
								return Interpreter.ConvertDotNetToMeta(type.GetProperty(text).GetValue(obj,new object[]{}));
							}
							else if(members[0] is EventInfo) {
								Delegate eventDelegate=(Delegate)type.GetField(text,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(obj);
								return new NetMethod("Invoke",eventDelegate,eventDelegate.GetType());
							}
						}
					}
					if(this.obj!=null && key is Integer && this.type.IsArray) {
						return Interpreter.ConvertDotNetToMeta(((Array)obj).GetValue(((Integer)key).Int)); // TODO: add error handling here
					}
					NetMethod indexerMethod=new NetMethod("get_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
					try {
						return indexerMethod.ojCallOj(arguments);
					}
					catch(Exception e) {
						return null;
					}
				}
				set {
					if(key is Map && ((Map)key).IsString) {
						string text=((Map)key).SDotNetStringV();
						if(text.Equals("staticEvent")) {
							int asdf=0;
						}
						MemberInfo[] members=type.GetMember(text,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(members.Length>0) {
							if(members[0] is MethodBase) {
								throw new ApplicationException("Cannot set method "+key+".");
							}
							else if(members[0] is FieldInfo) {
								FieldInfo field=(FieldInfo)members[0];
								bool converted;
								object val;
								val=NetMethod.ojConvertParameterOjTOutbla(value,field.FieldType,out converted);
								if(converted) {
									field.SetValue(obj,val);
								}
								if(!converted) {
									if(value is Map) {
										val=NetMethod.ojAssignCollectionMOjOutbla((Map)value,field.GetValue(obj),out converted);
									}
								}
								if(!converted) {
									throw new ApplicationException("Field "+field.Name+"could not be assigned because it cannot be converted.");
								}
								//TODO: refactor
								return;
							}
							else if(members[0] is PropertyInfo) {
								PropertyInfo property=(PropertyInfo)members[0];
								bool converted;
								object val=NetMethod.ojConvertParameterOjTOutbla(value,property.PropertyType,out converted);
								if(converted) {
									property.SetValue(obj,val,new object[]{});
								}
								if(!converted) {
									if(value is Map) {
										NetMethod.ojAssignCollectionMOjOutbla((Map)value,property.GetValue(obj,new object[]{}),out converted);
									}
									if(!converted) {
										throw new ApplicationException("Property "+this.type.Name+"."+Interpreter.vSaveToFileOjS(key,"",false)+" could not be set to "+value.ToString()+". The value can not be converted.");
									}
								}
								return;
							}
							else if(members[0] is EventInfo) {
								((EventInfo)members[0]).AddEventHandler(obj,CreateEvent(text,(Map)value));
								return;
							}
						}
					}
					if(obj!=null && key is Integer && type.IsArray) {
						bool isConverted; 
						object converted=Interpreter.ConvertMetaToDotNet(value,type.GetElementType(),out isConverted);
						if(isConverted) {
							((Array)obj).SetValue(converted,((Integer)key).Int);
							return;
						}
					}
					NetMethod indexer=new NetMethod("set_Item",obj,type);
					Map arguments=new Map();
					arguments[new Integer(1)]=key;
					arguments[new Integer(2)]=value;// do this more efficiently?
					try {
						indexer.ojCallOj(arguments);
					}
					catch(Exception e) {
						throw new ApplicationException("Cannot set "+key.ToString()+".");
					}
				}
			}
			public string Serialize(string indent,string[] functions) {
				return indent;
			}
			public Delegate CreateEvent(string name,Map code) {
				EventInfo eventInfo=type.GetEvent(name,BindingFlags.Public|BindingFlags.NonPublic|
															 BindingFlags.Static|BindingFlags.Instance);
				MethodInfo method=eventInfo.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
																						 |BindingFlags.Public|BindingFlags.NonPublic);
				Delegate del=NetMethod.delFromF(eventInfo.EventHandlerType,method,code);
				return del;
			}
			private IDictionary Table { // TODO: strange, what use is this
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
						table[new Map(field.Name)]=field.GetValue(obj);
					}
					foreach(MethodInfo method in type.GetMethods(bindingFlags))  {
						if(!method.IsSpecialName) {
							table[new Map(method.Name)]=new NetMethod(method.Name,obj,type);
						}
					}
					foreach(PropertyInfo property in type.GetProperties(bindingFlags)) {
						if(property.Name!="Item" && property.Name!="Chars") {
							table[new Map(property.Name)]=property.GetValue(obj,new object[]{});
						}
					}
					foreach(EventInfo eventInfo in type.GetEvents(bindingFlags)) {
						table[new Map(eventInfo.Name)]=new NetMethod(eventInfo.GetAddMethod().Name,this.obj,this.type);
					}
					int counter=1;
					if(obj!=null && obj is IEnumerable && !(obj is String)) { // is this useful?
						foreach(object entry in (IEnumerable)obj) {
							if(entry is DictionaryEntry) {
								table[Interpreter.ConvertDotNetToMeta(((DictionaryEntry)entry).Key)]=((DictionaryEntry)entry).Value;
							}
							else {
								table[new Integer(counter)]=entry;
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
	}
	namespace Parser  {
		public class IndentationStream: TokenStream {
			public IndentationStream(TokenStream stream)  {
				this.stream=stream;
				AddIndentationTokensToGetToLevel(0,new Token()); // TODO: remove "new Token" ?
			}
			public Token nextToken()  {
				if(streamBuffer.Count==0)  {
					Token t=stream.nextToken();
					switch(t.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokensToGetToLevel(-1,t);
							break;
//						case MetaLexerTokenTypes.EMPTY_LINE:
//							AddIndentationTokensToGetToLevel(presentIndentationLevel-1,t);
//							AddIndentationTokensToGetToLevel(presentIndentationLevel+1,t);
//							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokensToGetToLevel(t.getText().Length,t);
							break;
						case MetaLexerTokenTypes.LITERAL: // move this into parser, for correct error handling?
							string indentation="";
							for(int i=0;i<presentIndentationLevel+1;i++) {
								indentation+='\t';
							}
							string text=t.getText();
							text=text.Replace(Environment.NewLine,"\n"); // replace so we can use Split, which only works with characters
							string[] lines=text.Split('\n');
							string result="";
							for(int k=0;k<lines.Length;k++) {
								if(k!=0 && lines[k].StartsWith(indentation)) {
									result+=lines[k].Remove(0,presentIndentationLevel+1);
								}
								else {
									result+=lines[k];
								}
								if(k!=lines.Length-1) {
									result+=Environment.NewLine;
								}
							}
							t.setText(result);
							streamBuffer.Enqueue(t);
							break;
						default:
							streamBuffer.Enqueue(t);
							break;
					}
				}
				return (Token)streamBuffer.Dequeue();
			}
			protected void AddIndentationTokensToGetToLevel(int newIndentationLevel,Token token)  {
				int indentationDifference=newIndentationLevel-presentIndentationLevel; 
				if(indentationDifference==0) {
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));//TODO: use something else here
				}
				else if(indentationDifference==1) {
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				}
				else if(indentationDifference<0) {
					for(int i=indentationDifference;i<0;i++) {
						streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					streamBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE)); // TODO: tiny bit unlogical? maybe create this in Parser?
				}
				else if(indentationDifference>1) {
					// This doesn't get through properly because it is caught by ANTLR
					// TODO: make extra exception later.
					// I don't understand it and the lines are somehow off
					throw new RecognitionException("Incorrect indentation.",token.getFilename(),token.getLine(),token.getColumn());
				}
				presentIndentationLevel=newIndentationLevel;
			}
			protected Queue streamBuffer=new Queue();
			protected TokenStream stream;
			protected int presentIndentationLevel=-1;
		}
	}
	namespace TestingFramework {
		public interface ISerializeSpecial {
			string Serialize(string indent,string[] functions);
		}
		public abstract class TestCase {
			public abstract object RunTestCase();
		}
		public class ExecuteTests {	
			public ExecuteTests(Type classThatContainsTests,string pathToSerializeResultsTo) { // refactor -maybe, looks quite ok
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
					object result=((TestCase)testCase.GetConstructors()[0].Invoke(new object[]{})).RunTestCase();
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
				// TODO: Introduce utility methods
				StreamReader reader=new StreamReader(Path.Combine(path,"check.txt"));
				string check=reader.ReadToEnd();
				reader.Close();
				return result.Equals(check);
			}
			public static string Serialize(object obj) {
				return Serialize(obj,"",new string[]{});
			}
			public static string Serialize(object serialize,string indent,string[] methods) {
				if(serialize==null) {
					return indent+"null\n";
				}
				if(serialize is ISerializeSpecial) {
					string text=((ISerializeSpecial)serialize).Serialize(indent,methods);
					if(text!=null) {
						return text;
					}
				}
				if(serialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) {
					return indent+"\""+serialize.ToString()+"\""+"\n";
				}
				if(serialize is IEnumerable) {
					string text="";
					foreach(object entry in (IEnumerable)serialize) {
						text+=indent+"Entry ("+entry.GetType().Name+")\n"+Serialize(entry,indent+"  ",methods);
					}
					return text;
				}
				string t="";
				ArrayList members=new ArrayList();

				members.AddRange(serialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
				members.AddRange(serialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
				foreach(string methodName in methods) {
					MethodInfo method=serialize.GetType().GetMethod(methodName,BindingFlags.Public|BindingFlags.Instance);
					if(method!=null) { /* Only add method to members if it really exists, this isn't sure because methods are supplied per test not per class. */
						members.Add(method);
					}
				}
				members.Sort(new CompareMemberInfos());
				foreach(MemberInfo member in members) {
					if(member.Name!="Item") {
						if(member.GetCustomAttributes(typeof(DontSerializeFieldOrPropertyAttribute),false).Length==0) {
							if(serialize.GetType().Namespace==null ||!serialize.GetType().Namespace.Equals("System.Windows.Forms")) { // ugly hack to avoid some srange behaviour of some classes in System.Windows.Forms
								object val=serialize.GetType().InvokeMember(member.Name,BindingFlags.Public
									|BindingFlags.Instance|BindingFlags.GetProperty|BindingFlags.GetField
									|BindingFlags.InvokeMethod,null,serialize,null);
								t+=indent+member.Name;
								if(val!=null) {
									t+=" ("+val.GetType().Name+")";
								}
								t+=":\n"+Serialize(val,indent+"  ",methods);
							}
						}
					}
				}
				return t;
			}
		}
		internal class CompareMemberInfos:IComparer {
			public int Compare(object first,object second) {
				if(first==null || second==null || ((MemberInfo)first).Name==null || ((MemberInfo)second).Name==null) {
					int asdf=0;
					return 0;
				}
				else {
					return ((MemberInfo)first).Name.CompareTo(((MemberInfo)second).Name);
				}
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
