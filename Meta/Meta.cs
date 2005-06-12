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
					ojArgument=((IMap)ojArgument).MClone();
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
				mClone.MParent=mParent;
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
				mLocal.MParent=mParent;
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
				this.ojLiteral=Interpreter.OjRecognizeLiteralS((string)((Map)code[sLiteral]).SDotNetString());
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
				while(!mSelected.BlaHasKeyOj(ojKey)) {
					mSelected=mSelected.MParent;
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
						((Map)ojValue).MParent=((Map)mParent).MParent;
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
			public static void SaveToFileOjS(object meta,string fileName) {
				StreamWriter writer=new StreamWriter(fileName);
				writer.Write(SaveToFileOjS(meta,"",true).TrimEnd(new char[]{'\n'}));
				writer.Close();
			}
			public static string SaveToFileOjS(object ojMeta,string sIndent,bool blaRightSide) {
				if(ojMeta is Map) {
					string sText="";
					Map mMap=(Map)ojMeta;
					if(mMap.IsString) {
						sText+="\""+(mMap).SDotNetString()+"\"";
					}
					else if(mMap.ItgCount==0) {
						sText+="()";
					}
					else {
						if(!blaRightSide) {
							sText+="(";
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+='['+SaveToFileOjS(dtnretEntry.Key,sIndent,true)+']'+'='+SaveToFileOjS(dtnretEntry.Value,sIndent,true)+",";
							}
							if(mMap.ItgCount!=0) {
								sText=sText.Remove(sText.Length-1,1);
							}
							sText+=")";
						}
						else {
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+=sIndent+'['+SaveToFileOjS(dtnretEntry.Key,sIndent,false)+']'+'=';
								if(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).ItgCount!=0 && !((Map)dtnretEntry.Value).IsString) {
									sText+="\n";
								}
								sText+=SaveToFileOjS(dtnretEntry.Value,sIndent+'\t',true);
								if(!(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).ItgCount!=0 && !((Map)dtnretEntry.Value).IsString)) {
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
						if(dtnetEntry.Value is IKeyValue && !(dtnetEntry.Value is NetClass)&& mResult.BlaHasKeyOj(dtnetEntry.Key) 
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
				foreach(RecognizeLiteral rcnltrCurrent in arlrcnltrLiteralRecognitions) {
					object recognized=rcnltrCurrent.Recognize(text);
					if(recognized!=null) {
						return recognized;
					}
				}
				return null;
			}
			public static object ConvertDotNetToMeta(object ojDotNet) { 
				if(ojDotNet==null) {
					return null;
				}
				else if(ojDotNet.GetType().IsSubclassOf(typeof(Enum))) {
					return new Integer((int)Convert.ToInt32((Enum)ojDotNet));
				}
				DotNetToMetaConversion dttmecvsConversion=(DotNetToMetaConversion)htdntmtcvsToMetaConversions[ojDotNet.GetType()];
				if(dttmecvsConversion==null) {
					return ojDotNet;
				}
				else {
					return dttmecvsConversion.Convert(ojDotNet);
				}
			}
			public static object ConvertMetaToDotNet(object ojMeta) {
				if(ojMeta is Integer) {
					return ((Integer)ojMeta).Int;
				}
				else if(ojMeta is Map && ((Map)ojMeta).IsString) {
					return ((Map)ojMeta).SDotNetString();
				}
				else {
					return ojMeta;
				}
			}
			public static object ConvertMetaToDotNet(object ojMeta,Type tTarget) {
				try {
					MetaToDotNetConversion mttdncvsConversion=(MetaToDotNetConversion)((Hashtable)
						Interpreter.htmttdncvsToDotNetConversion[ojMeta.GetType()])[tTarget];
					bool converted;
					return mttdncvsConversion.Convert(ojMeta,out converted); // TODO: Why ignore converted here?, Should really loop through all the possibilities -> no not necessary here, type determines mttdncvsConversion
				}
				catch {
					return ojMeta;
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
			public static object Run(string sFileName,IMap mArgument) {
//				Map mProgram=mCompileS(sFileName);
				//				mProgram.mParent=Library.library;
				Map mProgram=Interpreter.mCompileS(sFileName);

				return CallProgram(mProgram,mArgument,Library.lbrLibrary);
			}

			public static object RunWithoutLibrary(string sFileName,IMap mArgument) { // TODO: refactor, combine with Run
				Map mProgram=mCompileS(sFileName); // TODO: rename, is not really a mProgram but a function
				return CallProgram(mProgram,mArgument,null);
			}
			public static object CallProgram(Map mProgram,IMap mArgument,IMap mParent) {
				Map mCallable=new Map();
				mCallable[Expression.sRun]=mProgram;
				mCallable.MParent=mParent;
				return mCallable.ojCallOj(mArgument);
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
			public static Map mCompileS(string sFileName) {
				return (new MetaTreeParser()).map(ParseToAst(sFileName));
			}
			public static AST ParseToAst(string sFileName)  {

				// TODO: Add the newlines here somewhere (or do this in IndentationStream?, somewhat easier and more logical maybe), but not possible, must be before mtlxLexerer
				// construct the special shared input state that is needed
				// in order to annotate MetaTokens properly
				FileStream fsFile=new FileStream(sFileName,FileMode.Open);
				ExtentLexerSharedInputState etlxsipsSharedInput = new ExtentLexerSharedInputState(fsFile,sFileName); 
				// construct the mtlxLexerer
				MetaLexer mtlxLexer = new MetaLexer(etlxsipsSharedInput);
		
				// tell the mtlxLexerer the token class that we want
				mtlxLexer.setTokenObjectClass("MetaToken");
		
				// construct the mtpsParserser
				MetaParser mtpsParser = new MetaParser(new IndentationStream(mtlxLexer));
				// tell the mtpsParserser the AST class that we want
				mtpsParser.setASTNodeClass("MetaAST");//
				mtpsParser.map();
				AST aAst=mtpsParser.getAST();
				fsFile.Close();
				return aAst;
			}

//			public static Map Arg { // TODO: is this still needed?
//				get {
//					return (Map)arlojArguments[arlojArguments.Count-1];
//				}
//			}
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
			public static object ConvertMetaToDotNet(object objMeta,Type tTarget,out bool outblaConverted) {
				if(tTarget.IsSubclassOf(typeof(Enum)) && objMeta is Integer) { 
					outblaConverted=true;
					return Enum.ToObject(tTarget,((Integer)objMeta).Int);
				}
				Hashtable htcvsToDotNet=(Hashtable)
					Interpreter.htmttdncvsToDotNetConversion[tTarget];
				if(htcvsToDotNet!=null) {
					MetaToDotNetConversion mttdncvsConversion=(MetaToDotNetConversion)htcvsToDotNet[objMeta.GetType()];
					if(mttdncvsConversion!=null) {
						return mttdncvsConversion.Convert(objMeta,out outblaConverted);
					}
				}
				outblaConverted=false;
				return null;
			}
//			public static object ConvertMetaToDotNet(object metaObject,Type targetType,out bool isConverted) {
//				if(targetType.IsSubclassOf(typeof(Enum)) && metaObject is Integer) { 
//					isConverted=true;
//					return Enum.ToObject(targetType,((Integer)metaObject).Int);
//				}
//				Hashtable toDotNet=(Hashtable)
//					Interpreter.htmttdncvsToDotNetConversion[targetType];
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
				Assembly asbMetaAssembly=Assembly.GetAssembly(typeof(Map));
				sInstallationPath=Directory.GetParent(asbMetaAssembly.Location).Parent.Parent.Parent.FullName; 
				foreach(Type tRecognition in typeof(LiteralRecognitions).GetNestedTypes()) {
					arlrcnltrLiteralRecognitions.Add((RecognizeLiteral)tRecognition.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				arlrcnltrLiteralRecognitions.Reverse();
				foreach(Type tToMetaConversion in typeof(DotNetToMetaConversions).GetNestedTypes()) {
					DotNetToMetaConversion dttmtcvsConversion=((DotNetToMetaConversion)tToMetaConversion.GetConstructor(new Type[]{}).Invoke(new object[]{}));
					htdntmtcvsToMetaConversions[dttmtcvsConversion.tSource]=dttmtcvsConversion;
				}
				foreach(Type tToDotNetConversion in typeof(MetaToDotNetConversions).GetNestedTypes()) {
					MetaToDotNetConversion mttdtcvsConversion=(MetaToDotNetConversion)tToDotNetConversion.GetConstructor(new Type[]{}).Invoke(new object[]{});
					if(!htmttdncvsToDotNetConversion.ContainsKey(mttdtcvsConversion.tTarget)) {
						htmttdncvsToDotNetConversion[mttdtcvsConversion.tTarget]=new Hashtable();
					}
					((Hashtable)htmttdncvsToDotNetConversion[mttdtcvsConversion.tTarget])[mttdtcvsConversion.tSource]=mttdtcvsConversion;
				}
			}
			public static string sInstallationPath;
			public static ArrayList arlMCallers=new ArrayList();
//			public static ArrayList arlojArguments=new ArrayList();
			public static Hashtable htmttdncvsToDotNetConversion=new Hashtable();
			public static Hashtable htdntmtcvsToMetaConversions=new Hashtable();
//			public static ArrayList compiledMaps=new ArrayList(); 
			public static ArrayList arlsLoadedAssemblies=new ArrayList();

			private static ArrayList arlrcnltrLiteralRecognitions=new ArrayList();

			public abstract class RecognizeLiteral {
				public abstract object Recognize(string text); // Returns null if not recognized. Null cannot currently be created this way.
			}
			public abstract class MetaToDotNetConversion {
				public Type tSource;
				public Type tTarget;
				public abstract object Convert(object obj,out bool converted);
			}
			public abstract class DotNetToMetaConversion {
				public Type tSource;
				public abstract object Convert(object obj);
			}
			public class LiteralRecognitions {
				// Attention! order of RecognizeLiteral classes matters
				public class RecognizeString:RecognizeLiteral {
					public override object Recognize(string sText) {
						return new Map(sText);
					}
				}
				// does everything get executed twice?
				public class RecognizeCharacter: RecognizeLiteral {
					public override object Recognize(string sText) {
						if(sText.StartsWith(@"\")) { // TODO: Choose another character for starting a character
							char crtResult;
							if(sText.Length==2) {
								crtResult=sText[1]; // not unicode safe, write wrapper that takes care of this stuff
							}
							else if(sText.Length==3) {
								switch(sText.Substring(1,2))  { // TODO: put this into Parser???
									case @"\'":
										crtResult='\'';
										break;
									case @"\\":
										crtResult='\\';
										break;
									case @"\a":
										crtResult='\a';
										break;
									case @"\b":
										crtResult='\b';
										break;
									case @"\f":
										crtResult='\f';
										break;
									case @"\n":
										crtResult='\n';
										break;
									case @"\r":
										crtResult='\r';
										break;
									case @"\t":
										crtResult='\t';
										break;
									case @"\v":
										crtResult='\v';
										break;
									default:
										throw new ApplicationException("Unrecognized escape sequence "+sText);
								}
							}
							else {
								return null;
							}
							return new Integer(crtResult);
						}
						return null;
					}
				}
				public class RecognizeInteger: RecognizeLiteral  {
					public override object Recognize(string sText)  { 
						if(sText.Equals("")) {
							return null;
						}
						else {
							Integer itgResult=new Integer(0);
							int idIndex=0;
							if(sText[0]=='-') {
								idIndex++;
							}
							// TODO: the following is probably incorrect for multi-byte unicode
							// use StringInfo in the future instead
							for(;idIndex<sText.Length;idIndex++) {
								if(char.IsDigit(sText[idIndex])) {
									itgResult=itgResult*10+(sText[idIndex]-'0');
								}
								else {
									return null;
								}
							}
							if(sText[0]=='-') {
								itgResult=-itgResult;
							}
							return itgResult;
						}
					}
				}

			}
			private abstract class MetaToDotNetConversions {
				/* These classes define the conversions that performed when a .NET method, field, or property
				 * is called/assigned to from Meta. */


				// TODO: Handle "outblaConverted" correctly
				public class ConvertIntegerToByte: MetaToDotNetConversion {
					public ConvertIntegerToByte() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Byte);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToByte(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToBool: MetaToDotNetConversion {
					public ConvertIntegerToBool() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(bool);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						int i=((Integer)ojToConvert).Int;
						if(i==0) {
							return false;
						}
						else if(i==1) {
							return true;
						}
						else {
							outblaConverted=false; // TODO
							return null;
//							throw new ApplicationException("Integer could not be outblaConverted to bool because it is neither 0 nor 1.");
						}
					}

				}
				public class ConvertIntegerToSByte: MetaToDotNetConversion {
					public ConvertIntegerToSByte() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(SByte);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToSByte(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToChar: MetaToDotNetConversion {
					public ConvertIntegerToChar() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Char);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToChar(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt32: MetaToDotNetConversion {
					public ConvertIntegerToInt32() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int32);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToInt32(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt32: MetaToDotNetConversion {
					public ConvertIntegerToUInt32() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt32);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToUInt32(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt64: MetaToDotNetConversion {
					public ConvertIntegerToInt64() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int64);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToInt64(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt64: MetaToDotNetConversion {
					public ConvertIntegerToUInt64() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt64);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToUInt64(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt16: MetaToDotNetConversion {
					public ConvertIntegerToInt16() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int16);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToInt16(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt16: MetaToDotNetConversion {
					public ConvertIntegerToUInt16() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt16);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return System.Convert.ToUInt16(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToDecimal: MetaToDotNetConversion {
					public ConvertIntegerToDecimal() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(decimal);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return (decimal)(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToDouble: MetaToDotNetConversion {
					public ConvertIntegerToDouble() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(double);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return (double)(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertIntegerToFloat: MetaToDotNetConversion {
					public ConvertIntegerToFloat() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(float);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						outblaConverted=true;
						return (float)(((Integer)ojToConvert).LongValue());
					}
				}
				public class ConvertMapToString: MetaToDotNetConversion {
					public ConvertMapToString() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(string);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						if(((Map)ojToConvert).IsString) {
							outblaConverted=true;
							return ((Map)ojToConvert).SDotNetString();
						}
						else {
							outblaConverted=false;
							return null;
						}
					}
				}
				public class ConvertFractionToDecimal: MetaToDotNetConversion {
					public ConvertFractionToDecimal() {
						// maybe make this more flexible, make it a function that determines applicability
						// also add the possibility to disamibuate several conversion; problem when calling
						// overloaded, similar methods
						this.tSource=typeof(Map); 
						this.tTarget=typeof(decimal); 
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						Map mMap=(Map)ojToConvert;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outblaConverted=true;
							return ((decimal)((Integer)mMap[new Map("iNumerator")]).LongValue())/((decimal)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outblaConverted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToDouble: MetaToDotNetConversion {
					public ConvertFractionToDouble() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(double);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						Map mMap=(Map)ojToConvert;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outblaConverted=true;
							return ((double)((Integer)mMap[new Map("iNumerator")]).LongValue())/((double)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outblaConverted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToFloat: MetaToDotNetConversion {
					public ConvertFractionToFloat() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(float);
					}
					public override object Convert(object ojToConvert, out bool outblaConverted) {
						Map mMap=(Map)ojToConvert;
						//						if(m.ContainsKey(new Map("iNumerator")) && m.ContainsKey(new Map("iDenominator")))
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outblaConverted=true;
							return ((float)((Integer)mMap[new Map("iNumerator")]).LongValue())/((float)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outblaConverted=false;
							return null;
						}
					}
				}
			}
			private abstract class DotNetToMetaConversions {
				/* These classes define the conversions that take place when .NET methods,
				 * properties and fields return. */
				public class ConvertStringToMap: DotNetToMetaConversion {
					public ConvertStringToMap()   {
						this.tSource=typeof(string);
					}
					public override object Convert(object ojToConvert) {
						return new Map((string)ojToConvert);
					}
				}
				public class ConvertBoolToInteger: DotNetToMetaConversion {
					public ConvertBoolToInteger() {
						this.tSource=typeof(bool);
					}
					public override object Convert(object ojToConvert) {
						return (bool)ojToConvert? new Integer(1): new Integer(0);
					}

				}
				public class ConvertByteToInteger: DotNetToMetaConversion {
					public ConvertByteToInteger() {
						this.tSource=typeof(Byte);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Byte)ojToConvert);
					}
				}
				public class ConvertSByteToInteger: DotNetToMetaConversion {
					public ConvertSByteToInteger() {
						this.tSource=typeof(SByte);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((SByte)ojToConvert);
					}
				}
				public class ConvertCharToInteger: DotNetToMetaConversion {
					public ConvertCharToInteger() {
						this.tSource=typeof(Char);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Char)ojToConvert);
					}
				}
				public class ConvertInt32ToInteger: DotNetToMetaConversion {
					public ConvertInt32ToInteger() {
						this.tSource=typeof(Int32);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Int32)ojToConvert);
					}
				}
				public class ConvertUInt32ToInteger: DotNetToMetaConversion {
					public ConvertUInt32ToInteger() {
						this.tSource=typeof(UInt32);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((UInt32)ojToConvert);
					}
				}
				public class ConvertInt64ToInteger: DotNetToMetaConversion {
					public ConvertInt64ToInteger() {
						this.tSource=typeof(Int64);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Int64)ojToConvert);
					}
				}
				public class ConvertUInt64ToInteger: DotNetToMetaConversion {
					public ConvertUInt64ToInteger() {
						this.tSource=typeof(UInt64);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Int64)(UInt64)ojToConvert);
					}
				}
				public class ConvertInt16ToInteger: DotNetToMetaConversion {
					public ConvertInt16ToInteger() {
						this.tSource=typeof(Int16);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((Int16)ojToConvert);
					}
				}
				public class ConvertUInt16ToInteger: DotNetToMetaConversion {
					public ConvertUInt16ToInteger() {
						this.tSource=typeof(UInt16);
					}
					public override object Convert(object ojToConvert) {
						return new Integer((UInt16)ojToConvert);
					}
				}
			}
		}
		/* Base class of exceptions in Meta. */
		public class MetaException:ApplicationException {
			protected string sMessage="";
			public MetaException(Extent etExtent) {
				this.etExtent=etExtent;
			}
			public MetaException(Exception exception,Extent etExtent):base(exception.Message,exception) { // not really all that logical, but so what
				this.etExtent=etExtent;
			}
			Extent etExtent;
//			public MetaException(string sMessage) {
//				this.sMessage=sMessage;
//			}
			public override string Message {
				get {
					return sMessage+" In file "+etExtent.fileName+", line: "+etExtent.startLine+", column: "+etExtent.startColumn+".";
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
				sMessage="Key ";
				if(key is Map && ((Map)key).IsString) {
					sMessage+=((Map)key).SDotNetString();
				}
				else if(key is Map) {
					sMessage+=Interpreter.SaveToFileOjS(key,"",true);
				}
				else {
					sMessage+=key;
				}
				sMessage+=" not found.";
			}
		}
		/* Thrown when a searched ojKey was not found. */
		public class KeyNotFoundException:KeyException {
			public KeyNotFoundException(object ojKey,Extent etExtent):base(ojKey,etExtent) {
			}
		}
		/* Thrown when an accessed ojKey does not exist. */
		public class KeyDoesNotExistException:KeyException {
			public KeyDoesNotExistException(object ojKey,Extent etExtent):base(ojKey,etExtent) {
			}
		}
	}
	namespace Types  {
		/* Everything implementing this interface can be used in a Call expression */
		public interface ICallable {
			object ojCallOj(object ojArgument);
		}
		// TODO: Rename this eventually
		public interface IMap: IKeyValue {
			IMap MParent {
				get;
				set;
			}
			ArrayList ArlojIntegerKeyValues {
				get;
			}
			IMap MClone();
		}
		// TODO: Does the IKeyValue<->IMap distinction make sense?
		public interface IKeyValue: IEnumerable {
			object this[object key] {
				get;
				set;
			}
			ArrayList ArlojKeys {
				get;
			}
			int ItgCount {
				get;
			}
			bool BlaHasKeyOj(object key);			
		}		
		/* Represents a lazily evaluated "library" Meta file. */
		public class MetaLibrary { // TODO: Put this into Library class, make base class for everything that gets loaded
			public object OjLoad() {
				return Interpreter.Run(sPath,new Map()); // TODO: Improve this interface, isn't read lazily anyway
			}
			public MetaLibrary(string sPath) {
				this.sPath=sPath;
			}
			string sPath;
		}
		public class LazyNamespace: IKeyValue { // TODO: Put this into library, combine with MetaLibrary
			public object this[object key] {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache[key];
				}
				set {
					throw new ApplicationException("Cannot set key "+key.ToString()+" in .NET namespace.");
				}
			}
			public ArrayList ArlojKeys {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache.ArlojKeys;
				}
			}
			public int ItgCount {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache.ItgCount;
				}
			}
			public string sFullName;
			public ArrayList mCachedAssemblies=new ArrayList();
			public Hashtable htsNamespaces=new Hashtable();
			public LazyNamespace(string sFullName) {
				this.sFullName=sFullName;
			}
			public void Load() {
				mCache=new Map();
				foreach(CachedAssembly mCachedAssembly in mCachedAssemblies) {
					mCache=(Map)Interpreter.Merge(mCache,mCachedAssembly.GetNamespaceContents(sFullName));
				}
				foreach(DictionaryEntry dtretEntry in htsNamespaces) {
					mCache[new Map((string)dtretEntry.Key)]=dtretEntry.Value;
				}
			}
			public Map mCache;
			public bool BlaHasKeyOj(object key) {
				if(mCache==null) {
					Load();
				}
				return mCache.BlaHasKeyOj(key);
			}
			public IEnumerator GetEnumerator() {
				if(mCache==null) {
					Load();
				}
				return mCache.GetEnumerator();
			}
		}
		/* TODO: What's this for? */
		public class CachedAssembly {  // TODO: Put this into Library class
			private Assembly asbAssembly;
			public CachedAssembly(Assembly asbAssembly) {
				this.asbAssembly=asbAssembly;
			}
			public Map GetNamespaceContents(string sNamespace) {
				if(mAssemblyContent==null) {
					mAssemblyContent=Library.LoadAssemblies(new object[] {asbAssembly});
				}
				Map mSelected=mAssemblyContent;
				if(sNamespace!="") {
					foreach(string sSubString in sNamespace.Split('.')) {
						mSelected=(Map)mSelected[new Map(sSubString)];
					}
				}
				return mSelected;
			}			
			private Map mAssemblyContent;
		}
		/* The library namespace, containing both Meta libraries as well as .NET libraries
		 *  from the "library" path and the GAC. */
		public class Library: IKeyValue,IMap {
			public object this[object ojKey] {
				get {
//					if(ojKey.Equals(new Map("map"))) {
//						int asdf=0;
//					}
					if(mCache.BlaHasKeyOj(ojKey)) {
						if(mCache[ojKey] is MetaLibrary) {
							mCache[ojKey]=((MetaLibrary)mCache[ojKey]).OjLoad();
						}
						return mCache[ojKey];
					}
					else {
						return null;
					}
				}
				set {
					throw new ApplicationException("Cannot set ojKey "+ojKey.ToString()+" in library.");
				}
			}
			public ArrayList ArlojKeys {
				get {
					return mCache.ArlojKeys;
				}
			}
			public IMap MClone() {
				return this;
			}
			public int ItgCount {
				get {
					return mCache.ItgCount;
				}
			}
			public bool BlaHasKeyOj(object ojKey) {
				return mCache.BlaHasKeyOj(ojKey);
			}
			public ArrayList ArlojIntegerKeyValues {
				get {
					return new ArrayList();
				}
			}
			public IMap MParent {
				get {
					return null;
				}
				set {
					throw new ApplicationException("Cannot set parent of library.");
				}
			}
			public IEnumerator GetEnumerator() { 
				foreach(DictionaryEntry dtretEntry in mCache) { // TODO: create separate enumerator for efficiency?
					object ojTemporary=mCache[dtretEntry.Key];				  // or remove IEnumerable from IMap (only needed for foreach)
				}														// decide later
				return mCache.GetEnumerator();
			}
			public static Map LoadAssemblies(IEnumerable enmrbasbAssmblies) {
				Map mRoot=new Map();
				foreach(Assembly asbCurrent in enmrbasbAssmblies) {
					foreach(Type tCurrent in asbCurrent.GetExportedTypes())  {
						if(tCurrent.DeclaringType==null)  {
							Map mPosition=mRoot;
							ArrayList arlsSubPaths=new ArrayList(tCurrent.FullName.Split('.'));
							arlsSubPaths.RemoveAt(arlsSubPaths.Count-1);
							foreach(string sSubPath in arlsSubPaths)  {
								if(!mPosition.BlaHasKeyOj(new Map(sSubPath)))  {
									mPosition[new Map(sSubPath)]=new Map();
								}
								mPosition=(Map)mPosition[new Map(sSubPath)];
							}
							mPosition[new Map(tCurrent.Name)]=new NetClass(tCurrent);
						}
					}
					Interpreter.arlsLoadedAssemblies.Add(asbCurrent.Location);
				}
				return mRoot;
			}
			private static AssemblyName GetAssemblyName(IAssemblyName iasbnName) {
				AssemblyName asbnName = new AssemblyName();
				asbnName.Name = AssemblyCache.GetName(iasbnName);
				asbnName.Version = AssemblyCache.GetVersion(iasbnName);
				asbnName.CultureInfo = AssemblyCache.GetCulture(iasbnName);
				asbnName.SetPublicKeyToken(AssemblyCache.GetPublicKeyToken(iasbnName));
				return asbnName;
			}
			public Library() {
				ArrayList arlasbAssemblies=new ArrayList();
				sLibraryPath=Path.Combine(Interpreter.sInstallationPath,"library");
				IAssemblyEnum iasbenAssemblyEnum=AssemblyCache.CreateGACEnum();
				IAssemblyName iasbnName; 
				AssemblyName asbnName;
				arlasbAssemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
				while (AssemblyCache.GetNextAssembly(iasbenAssemblyEnum, out iasbnName) == 0) {
					try {
						asbnName=GetAssemblyName(iasbnName);
						arlasbAssemblies.Add(Assembly.LoadWithPartialName(asbnName.Name));
					}
					catch(Exception e) {
						//Console.WriteLine("Could not load gac assembly :"+System.GAC.AssemblyCache.GetName(an));
					}
				}
				foreach(string fnCurrentDll in Directory.GetFiles(sLibraryPath,"*.dll")) {
					arlasbAssemblies.Add(Assembly.LoadFrom(fnCurrentDll));
				}
				foreach(string fnCurrentExe in Directory.GetFiles(sLibraryPath,"*.exe")) {
					arlasbAssemblies.Add(Assembly.LoadFrom(fnCurrentExe));
				}
				string fnCachedAssemblyInfo=Path.Combine(Interpreter.sInstallationPath,"mCachedAssemblyInfo.meta"); // TODO: Use another asbnName that doesn't collide with C# meaning
				if(File.Exists(fnCachedAssemblyInfo)) {
					mCachedAssemblyInfo=(Map)Interpreter.RunWithoutLibrary(fnCachedAssemblyInfo,new Map());
				}
				
				mCache=LoadNamespaces(arlasbAssemblies);
				Interpreter.SaveToFileOjS(mCachedAssemblyInfo,fnCachedAssemblyInfo);
				foreach(string fnCurrentMeta in Directory.GetFiles(sLibraryPath,"*.meta")) {
					mCache[new Map(Path.GetFileNameWithoutExtension(fnCurrentMeta))]=new MetaLibrary(fnCurrentMeta);
				}
			}
			private Map mCachedAssemblyInfo=new Map();
			public ArrayList GetNamespaces(Assembly asbAssembly) { //refactor, integrate into LoadNamespaces???
				ArrayList arlsNamespaces=new ArrayList();
				if(mCachedAssemblyInfo.BlaHasKeyOj(new Map(asbAssembly.Location))) {
					Map info=(Map)mCachedAssemblyInfo[new Map(asbAssembly.Location)];
					string sTimeStamp=((Map)info[new Map("timestamp")]).SDotNetString();
					if(sTimeStamp.Equals(File.GetCreationTime(asbAssembly.Location).ToString())) {
						Map mNamespaces=(Map)info[new Map("namespaces")];
						foreach(DictionaryEntry dtretEntry in mNamespaces) {
							string text=((Map)dtretEntry.Value).SDotNetString();
							arlsNamespaces.Add(text);
						}
						return arlsNamespaces;
					}
				}
				foreach(Type tType in asbAssembly.GetExportedTypes()) {
					if(!arlsNamespaces.Contains(tType.Namespace)) {
						if(tType.Namespace==null) {
							if(!arlsNamespaces.Contains("")) {
								arlsNamespaces.Add("");
							}
						}
						else {
							arlsNamespaces.Add(tType.Namespace);
						}
					}
				}
				Map mCachedAssemblyInfoMap=new Map();
				Map mNamespace=new Map();
				Integer counter=new Integer(0);
				foreach(string na in arlsNamespaces) {
					mNamespace[counter]=new Map(na);
					counter++;
				}
				mCachedAssemblyInfoMap[new Map("namespaces")]=mNamespace;
				mCachedAssemblyInfoMap[new Map("timestamp")]=new Map(File.GetCreationTime(asbAssembly.Location).ToString());
				mCachedAssemblyInfo[new Map(asbAssembly.Location)]=mCachedAssemblyInfoMap;
				return arlsNamespaces;
			}
			public Map LoadNamespaces(ArrayList arlasbAssemblies) {
				LazyNamespace lznsRoot=new LazyNamespace("");
				foreach(Assembly assembly in arlasbAssemblies) {
					ArrayList arlsNamespaces=GetNamespaces(assembly);
					CachedAssembly casbCachedAssembly=new CachedAssembly(assembly);
					foreach(string sNamespace in arlsNamespaces) {
						LazyNamespace lznsSelected=lznsRoot;
						if(sNamespace=="" && !assembly.Location.StartsWith(Path.Combine(Interpreter.sInstallationPath,"library"))) {
							continue;
						}
						if(sNamespace!="") {
							foreach(string sSubString in sNamespace.Split('.')) {
								if(!lznsSelected.htsNamespaces.ContainsKey(sSubString)) {
									string fullName=lznsSelected.sFullName;
									if(fullName!="") {
										fullName+=".";
									}
									fullName+=sSubString;
									lznsSelected.htsNamespaces[sSubString]=new LazyNamespace(fullName);
								}
								lznsSelected=(LazyNamespace)lznsSelected.htsNamespaces[sSubString];
							}
						}
						lznsSelected.mCachedAssemblies.Add(casbCachedAssembly);
					}
				}
				
				lznsRoot.Load();
				return lznsRoot.mCache;
			}
			public static Library lbrLibrary=new Library();
			private Map mCache=new Map();
			public static string sLibraryPath="library"; 
		}
		/* Automatically converts Meta oKeys of a Map to .NET counterparts. Useful when writing libraries. */
		public class MapAdapter { // TODO: Make this a whole IMap implementation?, if seems useful
			Map mMap;
			public MapAdapter(Map mMap) {
				this.mMap=mMap;
			}
			public MapAdapter() {
				this.mMap=new Map();
			}
			public object this[object oKey] {
				get {
					return Interpreter.ConvertMetaToDotNet(mMap[Interpreter.ConvertDotNetToMeta(oKey)]);
				}
				set {
					this.mMap[Interpreter.ConvertDotNetToMeta(oKey)]=Interpreter.ConvertDotNetToMeta(value);
				}
			}
		}

		//TODO: cache the ArlojIntegerKeyValues somewhere; put in an "Add" method
		public class Map: IKeyValue, IMap, ICallable, IEnumerable, ISerializeSpecial {
			public static readonly Map sParent=new Map("parent");
			public static readonly Map sArg=new Map("arg");
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
					return mstgTable.IsString;
				}
			}
			public string SDotNetString() { // Refactoring: has a stupid name, Make property
				return mstgTable.SDotNetString();
			}
			public IMap MParent {
				get {
					return mParent;
				}
				set {
					mParent=value;
				}
			}
			public int ItgCount {
				get {
					return mstgTable.ItgCount;
				}
			}
			public ArrayList ArlojIntegerKeyValues {
				get {
					return mstgTable.ArlojIntegerKeyValues;
				}
			}
			public virtual object this[object oKey]  {
				get {
					if(oKey.Equals(sParent)) {
						return MParent;
					}
					else if(oKey.Equals(sArg)) {
						return Argument;
					}
					else if(oKey.Equals(sThis)) {
						return this;
					}
					else {
						object result=mstgTable[oKey];
						return result;
					}
				}
				set {
					if(value!=null) {
						bHashCached=false;
						if(oKey.Equals(sThis)) {
							this.mstgTable=((Map)value).mstgTable.Clone();
						}
						else {
							object val=value is IMap? ((IMap)value).MClone(): value; // TODO: combine with next line
							if(value is IMap) {
								((IMap)val).MParent=this;
							}
							mstgTable[oKey]=val;
						}
					}
				}
			}
			public object Execute() { // TODO: Rename to evaluate
				Expression eFunction=(Expression)EpsCompileV();
				object oResult;
				oResult=eFunction.OjEvaluateM(this);
				return oResult;
			}
			public object ojCallOj(object ojArgument) {
				this.Argument=ojArgument;
				Expression eFunction=(Expression)((Map)this[Expression.sRun]).EpsCompileV();
				object oResult;
//				Interpreter.arlojArguments.Add(argument);
				oResult=eFunction.OjEvaluateM(this);
//				Interpreter.arlojArguments.RemoveAt(Interpreter.arlojArguments.Count-1);
				return oResult;
			}
			public ArrayList ArlojKeys {
				get {
					return mstgTable.ArlojKeys;
				}
			}
			public IMap MClone() {
				Map mClone=mstgTable.CloneMap();
				mClone.MParent=MParent;
				mClone.eCompiled=eCompiled;
				mClone.EtExtent=EtExtent;
				return mClone;
			}
			public Expression EpsCompileV()  { // eCompiled Statements are not cached, only expressions
				if(eCompiled==null)  {
					if(this.BlaHasKeyOj(Meta.Execution.Call.sCall)) {
						eCompiled=new Call(this);
					}
					else if(this.BlaHasKeyOj(Delayed.sDelayed)) { // TODO: could be optimized, but compilation happens seldom
						eCompiled=new Delayed(this);
					}
					else if(this.BlaHasKeyOj(Program.sProgram)) {
						eCompiled=new Program(this);
					}
					else if(this.BlaHasKeyOj(Literal.sLiteral)) {
						eCompiled=new Literal(this);
					}
					else if(this.BlaHasKeyOj(Search.sSearch)) {// TODO: use static expression strings
						eCompiled=new Search(this);
					}
					else if(this.BlaHasKeyOj(Select.sSelect)) {
						eCompiled=new Select(this);
					}
					else {
						throw new ApplicationException("Cannot compile non-code map.");
					}
				}
//				if(this.EtExtent!=null) {
//					int asdf=0;
//				}		
//				if(eCompiled is Expression) {
					((Expression)eCompiled).EtExtent=this.EtExtent;
//				}
				return eCompiled;
			}
			public bool BlaHasKeyOj(object oKey)  {
				if(oKey is Map) {
					if(oKey.Equals(sArg)) {
						return this.Argument!=null;
					}
					else if(oKey.Equals(sParent)) {
						return this.MParent!=null;
					}
					else if(oKey.Equals(sThis)) {
						return true;
					}
				}
				return mstgTable.ContainsKey(oKey);
			}
			public override bool Equals(object oToCompare) {
				if(Object.ReferenceEquals(oToCompare,this)) {
					return true;
				}
				if(!(oToCompare is Map)) {
					return false;
				}
				return ((Map)oToCompare).mstgTable.Equal(mstgTable);
			}
			public IEnumerator GetEnumerator() {
				return new MapEnumerator(this);
			}
			public override int GetHashCode()  {
				if(!bHashCached) {
					iHash=this.mstgTable.GetHashCode();
					bHashCached=true;
				}
				return iHash;
			}
			private bool bHashCached=false;
			private int iHash;

			Extent etExtent; // TODO: get rid of extent completely, put boatloads of integers here
			public Extent EtExtent {
				get {
					return etExtent;
				}
				set {
					etExtent=value;
				}
			}
			/* TODO: Move some more logic into constructor instead of in Parser?
			 * There is no clean separation then. But there isn't anyway. I could make 
			 * it so that only the etExtent gets passed, that's probably best*/
			public Map(string sText):this(new StringStrategy(sText)) {
			}
			public Map(MapStrategy mstgTable) {
				this.mstgTable=mstgTable;
				this.mstgTable.mMap=this;
			}
			public Map():this(new HybridDictionaryStrategy()) {
			}
			private IMap mParent;
			private MapStrategy mstgTable;
			public Expression eCompiled; // why have this at all, why not for statements? probably a question of performance.
			public string Serialize(string sIndentation,string[] asFunctions) {
				if(this.IsString) {
					return sIndentation+"\""+this.SDotNetString()+"\""+"\n";
				}
				else {
					return null;
				}
			}
			public abstract class MapStrategy { // TODO: Call this differently, hungarian-compatible
				public Map mMap;
				public MapStrategy Clone() {
					MapStrategy mstgStrategy=new HybridDictionaryStrategy();
					foreach(object oKey in this.ArlojKeys) {
						mstgStrategy[oKey]=this[oKey];
					}
					return mstgStrategy;	
				}
				public abstract Map CloneMap();
				public abstract ArrayList ArlojIntegerKeyValues {
					get;
				}
				public abstract bool IsString {
					get;
				}
				
				// TODO: Rename. Reason: This really means something more abstract, more along the lines of,
				// "is this a mMap that only has integers as children, and maybe also only integers as keys?"
				public abstract string SDotNetString();
				public abstract ArrayList ArlojKeys {
					get;
				}
				public abstract int ItgCount {
					get;
				}
				public abstract object this[object key]  {
					get;
					set;
				}

				public abstract bool ContainsKey(object key);
				/* Hashcodes must be exactly the same in all MapStrategies. */
				public override int GetHashCode()  {
					int iHash=0;
					foreach(object oKey in this.ArlojKeys) {
						unchecked {
							iHash+=oKey.GetHashCode()*this[oKey].GetHashCode();
						}
					}
					return iHash;
				}
				public virtual bool Equal(MapStrategy mstgToCompare) {
					if(Object.ReferenceEquals(mstgToCompare,this)) { // check whether this is a clone of the other MapStrategy (not used yet)
						return true;
					}
					if(mstgToCompare.ItgCount!=this.ItgCount) {
						return false;
					}
					foreach(object key in this.ArlojKeys)  {
						if(!mstgToCompare.ContainsKey(key)||!mstgToCompare[key].Equals(this[key])) {
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
					int iHash=0;
					for(int i=0;i<sText.Length;i++) {//(char c in this.sText) {
						iHash+=(i+1)*sText[i];
					}
					return iHash;
				}
				public override bool Equal(MapStrategy mstgToCompare) {
					if(mstgToCompare is StringStrategy) {	// TODO: Decide on single exit for methods, might be useful, especially here
						return ((StringStrategy)mstgToCompare).sText.Equals(this.sText);
					}
					else {
						return base.Equal(mstgToCompare);
					}
				}
				public override Map CloneMap() {
					return new Map(new StringStrategy(this));
				}
				public override ArrayList ArlojIntegerKeyValues {
					get {
						ArrayList arlList=new ArrayList();
						foreach(char iChar in sText) {
							arlList.Add(new Integer(iChar));
						}
						return arlList;
					}
				}
				public override bool IsString {
					get {
						return true;
					}
				}
				public override string SDotNetString() {
					return sText;
				}
				public override ArrayList ArlojKeys {
					get {
						return aiKeys;
					}
				}
				private ArrayList aiKeys=new ArrayList();
				private string sText;
				public StringStrategy(StringStrategy clone) {
					this.sText=clone.sText;
					this.aiKeys=(ArrayList)clone.aiKeys.Clone();
				}
				public StringStrategy(string sText) {
					this.sText=sText;
					for(int i=1;i<=sText.Length;i++) { // make this lazy? it won't work with unicode anymore then, though
						aiKeys.Add(new Integer(i));			// TODO: Make this unicode-safe in the first place!
					}
				}
				public override int ItgCount {
					get {
						return sText.Length;
					}
				}
				public override object this[object oKey]  {
					get {
						if(oKey is Integer) {
							int itgInteger=((Integer)oKey).Int;
							if(itgInteger>0 && itgInteger<=this.ItgCount) {
								return new Integer(sText[itgInteger-1]);
							}
						}
						return null;
					}
					set {
						/* StringStrategy gets changed. Fall back on standard strategy because we can't be sure
						 * the mMap will still be a string afterwards. */
						mMap.mstgTable=this.Clone();
						mMap.mstgTable[oKey]=value;
					}
				}
				public override bool ContainsKey(object oKey)  {
					if(oKey is Integer) {
						return ((Integer)oKey)>0 && ((Integer)oKey)<=this.ItgCount;
					}
					else {
						return false;
					}
				}
			}
			public class HybridDictionaryStrategy:MapStrategy {
				ArrayList aoKeys;
				private HybridDictionary mstgTable;
				public HybridDictionaryStrategy():this(2) {
				}
				public HybridDictionaryStrategy(int iCount) {
					this.aoKeys=new ArrayList(iCount);
					this.mstgTable=new HybridDictionary(iCount);
				}
				public override Map CloneMap() {
					Map mClone=new Map(new HybridDictionaryStrategy(this.aoKeys.Count));
					foreach(object oKey in aoKeys) {
						mClone[oKey]=mstgTable[oKey];
					}
					return mClone;
				}
				public override ArrayList ArlojIntegerKeyValues {
					get {
						ArrayList aList=new ArrayList();
						for(Integer itgInteger=new Integer(1);ContainsKey(itgInteger);itgInteger++) {
							aList.Add(this[itgInteger]);
						}
						return aList;
					}
				}
				public override bool IsString {
					get {
						if(ArlojIntegerKeyValues.Count>0) {
							try {
								SDotNetString();// TODO: a bit of a hack
								return true;
							}
							catch{
							}
						}
						return false;
					}
				}
				public override string SDotNetString() { // TODO: looks too complicated
					string sText="";
					foreach(object oKey in this.ArlojKeys) {
						if(oKey is Integer && this.mstgTable[oKey] is Integer) {
							try {
								sText+=Convert.ToChar(((Integer)this.mstgTable[oKey]).Int);
							}
							catch {
								throw new MapException(this.mMap,"Map is not a string");
							}
						}
						else {
							throw new MapException(this.mMap,"Map is not a string");
						}
					}
					return sText;
				}
				public class MapException:ApplicationException { // TODO: Remove or make sense of this
					Map mMap;
					public MapException(Map mMap,string sMessage):base(sMessage) {
						this.mMap=mMap;
					}
				}
				public override ArrayList ArlojKeys {
					get {
						return aoKeys;
					}
				}
				public override int ItgCount {
					get {
						return mstgTable.Count;
					}
				}
				public override object this[object oKey]  {
					get {
						return mstgTable[oKey];
					}
					set {
						if(!this.ContainsKey(oKey)) {
							aoKeys.Add(oKey);
						}
						mstgTable[oKey]=value;
					}
				}
				public override bool ContainsKey(object oKey)  {
					return mstgTable.Contains(oKey);
				}
			}
		}
		public class MapEnumerator: IEnumerator {
			private Map mMap; public MapEnumerator(Map mMap) {
				this.mMap=mMap;
			}
			public object Current {
				get {
					return new DictionaryEntry(mMap.ArlojKeys[iCurrent],mMap[mMap.ArlojKeys[iCurrent]]);
				}
			}
			public bool MoveNext() {
				iCurrent++;
				return iCurrent<mMap.ItgCount;
			}
			public void Reset() {
				iCurrent=-1;
			}
			private int iCurrent=-1;
		}
		public delegate object DelegateCreatedForGenericDelegates(); // TODO: rename?
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
					object oResult=Interpreter.ConvertMetaToDotNet(ojMeta,tParameter,out outblaParamConverted);
					if(outblaParamConverted) {
						return oResult;
					}
				}
				outblaConverted=false;
				return null;
			}
			public object ojCallOj(object ojArgument) {
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
				if(!blaExecuted) {
					ArrayList arlOArguments=((IMap)ojArgument).ArlojIntegerKeyValues;
					ArrayList arlMtifRightNumberArguments=new ArrayList();
					foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
						if(arlOArguments.Count==mtbCurrent.GetParameters().Length) { // don't match if different parameter list length
							if(arlOArguments.Count==((IMap)ojArgument).ArlojKeys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
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
				if(ojResult==null) {
					int asdf=0;
				}
				return Interpreter.ConvertDotNetToMeta(ojResult);
			}
			/* Create a delegate of a certain tTarget that calls a Meta function. */
			public static Delegate delFromF(Type delegateType,MethodInfo method,Map code) { // TODO: delegateType, methode, redundant?
				code.MParent=(IMap)Interpreter.arlMCallers[Interpreter.arlMCallers.Count-1];
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
				assemblyNames.AddRange(Interpreter.arlsLoadedAssemblies);
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
//				assemblyNames.AddRange(Interpreter.arlsLoadedAssemblies);
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
//			public object Call(object ojArgument) {
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
//				assemblyNames.AddRange(Interpreter.arlsLoadedAssemblies);
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
			public object ojCallOj(object ojArgument) {
				return constructor.ojCallOj(ojArgument);
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
			public bool BlaHasKeyOj(object key) {
				if(key is Map) {
					if(((Map)key).IsString) {
						string text=((Map)key).SDotNetString();
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
			public ArrayList ArlojKeys {
				get {
					return new ArrayList(Table.Keys);
				}
			}
			public int ItgCount  {
				get {
					return Table.Count;
				}
			}
			public virtual object this[object key]  {
				get {
					if(key is Map && ((Map)key).IsString) {
						string text=((Map)key).SDotNetString();
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
						string text=((Map)key).SDotNetString();
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
										throw new ApplicationException("Property "+this.type.Name+"."+Interpreter.SaveToFileOjS(key,"",false)+" could not be set to "+value.ToString()+". The value can not be converted.");
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
