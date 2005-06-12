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
			public abstract object OEvaluateM(IMap parent);
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
			public override object OEvaluateM(IMap parent) {
				object oArgument=eArgument.OEvaluateM(parent);
				if(oArgument is IMap) {
					oArgument=((IMap)oArgument).MClone();
				}
				return ((ICallable)eCallable.OEvaluateM(parent)).oCallO(oArgument);
			}
			public static readonly Map sCall=new Map("call");
			public static readonly Map sFunction=new Map("function");
			public static readonly Map sArgument=new Map("argument");
			public Call(Map mCode) {
				Map mCall=(Map)mCode[sCall];
				this.eCallable=(Expression)((Map)mCall[sFunction]).ECompile();
				this.eArgument=(Expression)((Map)mCall[sArgument]).ECompile();
			}
			public Expression eArgument;
			public Expression eCallable;
		}
		public class Delayed: Expression {
			public override object OEvaluateM(IMap mParent) {
				Map mClone=mDelayed;
				mClone.MParent=mParent;
				return mClone;
			}
			public static readonly Map sDelayed=new Map("delayed"); // TODO: maybe define my own type for this stuff?
			public Delayed(Map mCode) {
				this.mDelayed=(Map)mCode[sDelayed];
			}
			public Map mDelayed;
		}


		public class Program: Expression {
			public override object OEvaluateM(IMap mParent) {
				Map mLocal=new Map();
				return OEvaluateM(mParent,mLocal);
			}
			public object OEvaluateM(IMap mParent,IMap mLocal) {
				mLocal.MParent=mParent;
				Interpreter.amCallers.Add(mLocal);
				for(int i=0;i<asmStatements.Count;i++) {
					mLocal=(Map)Interpreter.OCurrent;
					((Statement)asmStatements[i]).RealizeM(mLocal);
				}
				object oResult=Interpreter.OCurrent;
				Interpreter.amCallers.RemoveAt(Interpreter.amCallers.Count-1);
				return oResult;
			}
			public static readonly Map sProgram=new Map("program");
			public Program(Map mProgram) { // TODO: special Type for  callable maps?
				foreach(Map mStatement in ((Map)mProgram[sProgram]).AoIntegerKeyValues) {
					this.asmStatements.Add(new Statement(mStatement)); // should we save the original maps instead of asmStatements?
				}
			}
			public readonly ArrayList asmStatements=new ArrayList();
		}
		public class Literal: Expression {
			public override object OEvaluateM(IMap mParent) {
				return oLiteral;
			}
			public static readonly Map sLiteral=new Map("literal");
			public Literal(Map code) {
				this.oLiteral=Interpreter.ORecognizeLiteralS((string)((Map)code[sLiteral]).SString);
			}
			public object oLiteral=null;
		}
		public class Search: Expression {
			public Search(Map mSearch) {
				this.eKey=(Expression)((Map)mSearch[sSearch]).ECompile();
			}
			public Expression eKey;
			public static readonly Map sKey=new Map("key");
			public override object OEvaluateM(IMap mParent) {
				object oKey=eKey.OEvaluateM(mParent);
				IMap mSelected=mParent;
				while(!mSelected.BContainsO(oKey)) {
					mSelected=mSelected.MParent;
					if(mSelected==null) {
						throw new KeyNotFoundException(oKey,this.EtExtent);
					}
				}
				return mSelected[oKey];
			}
			public static readonly Map sSearch=new Map("search");
		}

		public class Select: Expression {
			public ArrayList aeKeys=new ArrayList();
			public Expression eFirst;// TODO: maybe rename to srFirst -> it's a Search
			public Select(Map code) {
				ArrayList amKeys=((Map)code[sSelect]).AoIntegerKeyValues;
				eFirst=(Expression)((Map)amKeys[0]).ECompile();
				for(int i=1;i<amKeys.Count;i++) {
					aeKeys.Add(((Map)amKeys[i]).ECompile());
				}
			}
			public override object OEvaluateM(IMap mParent) {
				object oSelected=eFirst.OEvaluateM(mParent);
				for(int iCurrent=0;iCurrent<aeKeys.Count;iCurrent++) {
					if(!(oSelected is IKeyValue)) {
						oSelected=new NetObject(oSelected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
					}
					object oKey=((Expression)aeKeys[iCurrent]).OEvaluateM(mParent);
					oSelected=((IKeyValue)oSelected)[oKey];
					if(oSelected==null) {
						throw new KeyDoesNotExistException(oKey,this.EtExtent);
					}
				}
				return oSelected;
			}
			public static readonly Map sSelect=new Map("select");
		}

		public class Statement {
			public void RealizeM(IMap mParent) {
				object oSelected=mParent;
				object oKey;
				for(int i=0;i<aeKeys.Count-1;i++) {
					oKey=((Expression)aeKeys[i]).OEvaluateM((IMap)mParent);
					oSelected=((IKeyValue)oSelected)[oKey];
					if(oSelected==null) {
						throw new KeyDoesNotExistException(oKey,((Expression)aeKeys[i]).EtExtent);
					}
					if(!(oSelected is IKeyValue)) {
						oSelected=new NetObject(oSelected);// TODO: put this into Map.this[] ??, or always save like this, would be inefficient, though
					}
				}
				object oLastKey=((Expression)aeKeys[aeKeys.Count-1]).OEvaluateM((IMap)mParent);
				object oValue=eValue.OEvaluateM((IMap)mParent);
				if(oLastKey.Equals(Map.sThis)) {
					if(oValue is Map) {
						((Map)oValue).MParent=((Map)mParent).MParent;
					}
					else {
						int asdf=0;
					}
					Interpreter.OCurrent=oValue;

				}
				else {
					((IKeyValue)oSelected)[oLastKey]=oValue;
				}
			}
			public Statement(Map mStatement) {
				foreach(Map key in ((Map)mStatement[sKey]).AoIntegerKeyValues) {
					aeKeys.Add(key.ECompile());
				}
				this.eValue=(Expression)((Map)mStatement[sValue]).ECompile();
			}
			public ArrayList aeKeys=new ArrayList();
			public Expression eValue;


			public static readonly Map sKey=new Map("key");
			public static readonly Map sValue=new Map("value");
		}

	

		public class Interpreter  {
			public static void SaveToFileOFn(object oMeta,string fnFile) {
				StreamWriter swFile=new StreamWriter(fnFile);
				swFile.Write(SaveToFileOFn(oMeta,"",true).TrimEnd(new char[]{'\n'}));
				swFile.Close();
			}
			public static string SaveToFileOFn(object oMeta,string sIndent,bool bRightSide) {
				if(oMeta is Map) {
					string sText="";
					Map mMap=(Map)oMeta;
					if(mMap.BIsString) {
						sText+="\""+(mMap).SString+"\"";
					}
					else if(mMap.ICount==0) {
						sText+="()";
					}
					else {
						if(!bRightSide) {
							sText+="(";
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+='['+SaveToFileOFn(dtnretEntry.Key,sIndent,true)+']'+'='+SaveToFileOFn(dtnretEntry.Value,sIndent,true)+",";
							}
							if(mMap.ICount!=0) {
								sText=sText.Remove(sText.Length-1,1);
							}
							sText+=")";
						}
						else {
							foreach(DictionaryEntry dtnretEntry in mMap) {
								sText+=sIndent+'['+SaveToFileOFn(dtnretEntry.Key,sIndent,false)+']'+'=';
								if(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).ICount!=0 && !((Map)dtnretEntry.Value).BIsString) {
									sText+="\n";
								}
								sText+=SaveToFileOFn(dtnretEntry.Value,sIndent+'\t',true);
								if(!(dtnretEntry.Value is Map && ((Map)dtnretEntry.Value).ICount!=0 && !((Map)dtnretEntry.Value).BIsString)) {
									sText+="\n";
								}
							}
						}
					}
					return sText;
				}
				else if(oMeta is Integer) {
					Integer integer=(Integer)oMeta;
					return "\""+integer.ToString()+"\"";
				}
				else {
					throw new ApplicationException("Serialization not implemented for type "+oMeta.GetType().ToString()+".");
				}
			}
			public static IKeyValue KvMergeAkv(params IKeyValue[] arkvlToMerge) {
				return MergeCollection(arkvlToMerge);
			}
			// really use IKeyValue?
			public static IKeyValue MergeCollection(ICollection cltkvlToMerge) {
				Map mResult=new Map();//use clone here?
				foreach(IKeyValue kvlCurrent in cltkvlToMerge) {
					foreach(DictionaryEntry dtnetEntry in (IKeyValue)kvlCurrent) {
						if(dtnetEntry.Value is IKeyValue && !(dtnetEntry.Value is NetClass)&& mResult.BContainsO(dtnetEntry.Key) 
							&& mResult[dtnetEntry.Key] is IKeyValue && !(mResult[dtnetEntry.Key] is NetClass)) {
							mResult[dtnetEntry.Key]=KvMergeAkv((IKeyValue)mResult[dtnetEntry.Key],(IKeyValue)dtnetEntry.Value);
						}
						else {
							mResult[dtnetEntry.Key]=dtnetEntry.Value;
						}
					}
				}
				return mResult;
			}	
			public static object ORecognizeLiteralS(string text) {
				foreach(RecognizeLiteral rcnltrCurrent in arcnltrLiteralRecognitions) {
					object recognized=rcnltrCurrent.Recognize(text);
					if(recognized!=null) {
						return recognized;
					}
				}
				return null;
			}
			public static object OMetaFromDotNetO(object oDotNet) { 
				if(oDotNet==null) {
					return null;
				}
				else if(oDotNet.GetType().IsSubclassOf(typeof(Enum))) {
					return new Integer((int)Convert.ToInt32((Enum)oDotNet));
				}
				DotNetToMetaConversion dttmecvsConversion=(DotNetToMetaConversion)htdntmtcvsToMetaConversions[oDotNet.GetType()];
				if(dttmecvsConversion==null) {
					return oDotNet;
				}
				else {
					return dttmecvsConversion.Convert(oDotNet);
				}
			}
			public static object ODotNetFromMetaO(object oMeta) {
				if(oMeta is Integer) {
					return ((Integer)oMeta).Int;
				}
				else if(oMeta is Map && ((Map)oMeta).BIsString) {
					return ((Map)oMeta).SString;
				}
				else {
					return oMeta;
				}
			}
			public static object ODotNetFromMetaO(object oMeta,Type tTarget) {
				try {
					MetaToDotNetConversion mttdncvsConversion=(MetaToDotNetConversion)((Hashtable)
						Interpreter.htmttdncvsToDotNetConversion[oMeta.GetType()])[tTarget];
					bool bConverted;
					return mttdncvsConversion.Convert(oMeta,out bConverted); // TODO: Why ignore bConverted here?, Should really loop through all the possibilities -> no not necessary here, type determines mttdncvsConversion
				}
				catch {
					return oMeta;
				}
			}
			public static object ORunFnM(string sFileName,IMap mArgument) {
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
				return mCallable.oCallO(mArgument);
			}

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
			public static object OCurrent {
				get {
					if(amCallers.Count==0) {
						return null;
					}
					return amCallers[amCallers.Count-1];
				}
				set {
					amCallers[amCallers.Count-1]=value;
				}
			}
			public static object ODotNetFromMetaO(object objMeta,Type tTarget,out bool outbConverted) {
				if(tTarget.IsSubclassOf(typeof(Enum)) && objMeta is Integer) { 
					outbConverted=true;
					return Enum.ToObject(tTarget,((Integer)objMeta).Int);
				}
				Hashtable htcvsToDotNet=(Hashtable)
					Interpreter.htmttdncvsToDotNetConversion[tTarget];
				if(htcvsToDotNet!=null) {
					MetaToDotNetConversion mttdncvsConversion=(MetaToDotNetConversion)htcvsToDotNet[objMeta.GetType()];
					if(mttdncvsConversion!=null) {
						return mttdncvsConversion.Convert(objMeta,out outbConverted);
					}
				}
				outbConverted=false;
				return null;
			}
			static Interpreter() {
				Assembly asbMetaAssembly=Assembly.GetAssembly(typeof(Map));
				sInstallationPath=Directory.GetParent(asbMetaAssembly.Location).Parent.Parent.Parent.FullName; 
				foreach(Type tRecognition in typeof(LiteralRecognitions).GetNestedTypes()) {
					arcnltrLiteralRecognitions.Add((RecognizeLiteral)tRecognition.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				arcnltrLiteralRecognitions.Reverse();
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
			public static ArrayList amCallers=new ArrayList();
			public static Hashtable htmttdncvsToDotNetConversion=new Hashtable();
			public static Hashtable htdntmtcvsToMetaConversions=new Hashtable();
			public static ArrayList asLoadedAssemblies=new ArrayList();

			private static ArrayList arcnltrLiteralRecognitions=new ArrayList();

			public abstract class RecognizeLiteral {
				public abstract object Recognize(string text); // Returns null if not recognized. Null cannot currently be created this way.
			}
			public abstract class MetaToDotNetConversion {
				public Type tSource;
				public Type tTarget;
				public abstract object Convert(object obj,out bool converted);
			}
			public abstract class DotNetToMetaConversion { // TODO: rename, consider Hungarian
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
							Integer iResult=new Integer(0);
							int idIndex=0;
							if(sText[0]=='-') {
								idIndex++;
							}
							// TODO: the following is probably incorrect for multi-byte unicode
							// use StringInfo in the future instead
							for(;idIndex<sText.Length;idIndex++) {
								if(char.IsDigit(sText[idIndex])) {
									iResult=iResult*10+(sText[idIndex]-'0');
								}
								else {
									return null;
								}
							}
							if(sText[0]=='-') {
								iResult=-iResult;
							}
							return iResult;
						}
					}
				}

			}
			private abstract class MetaToDotNetConversions {
				/* These classes define the conversions that performed when a .NET method, field, or property
				 * is called/assigned to from Meta. */


				// TODO: Handle "outbConverted" correctly
				public class ConvertIntegerToByte: MetaToDotNetConversion {
					public ConvertIntegerToByte() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Byte);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToByte(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToBool: MetaToDotNetConversion {
					public ConvertIntegerToBool() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(bool);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						int i=((Integer)oToConvert).Int;
						if(i==0) {
							return false;
						}
						else if(i==1) {
							return true;
						}
						else {
							outbConverted=false; // TODO
							return null;
						}
					}

				}
				public class ConvertIntegerToSByte: MetaToDotNetConversion {
					public ConvertIntegerToSByte() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(SByte);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToSByte(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToChar: MetaToDotNetConversion {
					public ConvertIntegerToChar() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Char);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToChar(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt32: MetaToDotNetConversion {
					public ConvertIntegerToInt32() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int32);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToInt32(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt32: MetaToDotNetConversion {
					public ConvertIntegerToUInt32() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt32);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToUInt32(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt64: MetaToDotNetConversion {
					public ConvertIntegerToInt64() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int64);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToInt64(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt64: MetaToDotNetConversion {
					public ConvertIntegerToUInt64() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt64);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToUInt64(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToInt16: MetaToDotNetConversion {
					public ConvertIntegerToInt16() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(Int16);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToInt16(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToUInt16: MetaToDotNetConversion {
					public ConvertIntegerToUInt16() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(UInt16);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return System.Convert.ToUInt16(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToDecimal: MetaToDotNetConversion {
					public ConvertIntegerToDecimal() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(decimal);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return (decimal)(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToDouble: MetaToDotNetConversion {
					public ConvertIntegerToDouble() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(double);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return (double)(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertIntegerToFloat: MetaToDotNetConversion {
					public ConvertIntegerToFloat() {
						this.tSource=typeof(Integer);
						this.tTarget=typeof(float);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						outbConverted=true;
						return (float)(((Integer)oToConvert).LongValue());
					}
				}
				public class ConvertMapToString: MetaToDotNetConversion {
					public ConvertMapToString() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(string);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						if(((Map)oToConvert).BIsString) {
							outbConverted=true;
							return ((Map)oToConvert).SString;
						}
						else {
							outbConverted=false;
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
					public override object Convert(object oToConvert, out bool outbConverted) {
						Map mMap=(Map)oToConvert;
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outbConverted=true;
							return ((decimal)((Integer)mMap[new Map("iNumerator")]).LongValue())/((decimal)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outbConverted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToDouble: MetaToDotNetConversion {
					public ConvertFractionToDouble() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(double);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						Map mMap=(Map)oToConvert;
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outbConverted=true;
							return ((double)((Integer)mMap[new Map("iNumerator")]).LongValue())/((double)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outbConverted=false;
							return null;
						}
					}

				}
				public class ConvertFractionToFloat: MetaToDotNetConversion {
					public ConvertFractionToFloat() {
						this.tSource=typeof(Map);
						this.tTarget=typeof(float);
					}
					public override object Convert(object oToConvert, out bool outbConverted) {
						Map mMap=(Map)oToConvert;
						if(mMap[new Map("iNumerator")] is Integer && mMap[new Map("iDenominator")] is Integer) {
							outbConverted=true;
							return ((float)((Integer)mMap[new Map("iNumerator")]).LongValue())/((float)((Integer)mMap[new Map("iDenominator")]).LongValue());
						}
						else {
							outbConverted=false;
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
					public override object Convert(object oToConvert) {
						return new Map((string)oToConvert);
					}
				}
				public class ConvertBoolToInteger: DotNetToMetaConversion {
					public ConvertBoolToInteger() {
						this.tSource=typeof(bool);
					}
					public override object Convert(object oToConvert) {
						return (bool)oToConvert? new Integer(1): new Integer(0);
					}

				}
				public class ConvertByteToInteger: DotNetToMetaConversion {
					public ConvertByteToInteger() {
						this.tSource=typeof(Byte);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Byte)oToConvert);
					}
				}
				public class ConvertSByteToInteger: DotNetToMetaConversion {
					public ConvertSByteToInteger() {
						this.tSource=typeof(SByte);
					}
					public override object Convert(object oToConvert) {
						return new Integer((SByte)oToConvert);
					}
				}
				public class ConvertCharToInteger: DotNetToMetaConversion {
					public ConvertCharToInteger() {
						this.tSource=typeof(Char);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Char)oToConvert);
					}
				}
				public class ConvertInt32ToInteger: DotNetToMetaConversion {
					public ConvertInt32ToInteger() {
						this.tSource=typeof(Int32);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Int32)oToConvert);
					}
				}
				public class ConvertUInt32ToInteger: DotNetToMetaConversion {
					public ConvertUInt32ToInteger() {
						this.tSource=typeof(UInt32);
					}
					public override object Convert(object oToConvert) {
						return new Integer((UInt32)oToConvert);
					}
				}
				public class ConvertInt64ToInteger: DotNetToMetaConversion {
					public ConvertInt64ToInteger() {
						this.tSource=typeof(Int64);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Int64)oToConvert);
					}
				}
				public class ConvertUInt64ToInteger: DotNetToMetaConversion {
					public ConvertUInt64ToInteger() {
						this.tSource=typeof(UInt64);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Int64)(UInt64)oToConvert);
					}
				}
				public class ConvertInt16ToInteger: DotNetToMetaConversion {
					public ConvertInt16ToInteger() {
						this.tSource=typeof(Int16);
					}
					public override object Convert(object oToConvert) {
						return new Integer((Int16)oToConvert);
					}
				}
				public class ConvertUInt16ToInteger: DotNetToMetaConversion {
					public ConvertUInt16ToInteger() {
						this.tSource=typeof(UInt16);
					}
					public override object Convert(object oToConvert) {
						return new Integer((UInt16)oToConvert);
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
			public override string Message {
				get {
					return sMessage+" In file "+etExtent.fileName+", line: "+etExtent.startLine+", column: "+etExtent.startColumn+".";
				}
			}
		}
		/* Base class for key exceptions. */
		public abstract class KeyException:MetaException { // TODO: Add proper formatting here, output strings as strings, for example, if possible, as well as integers
			public KeyException(object key,Extent extent):base(extent) {
				sMessage="Key ";
				if(key is Map && ((Map)key).BIsString) {
					sMessage+=((Map)key).SString;
				}
				else if(key is Map) {
					sMessage+=Interpreter.SaveToFileOFn(key,"",true);
				}
				else {
					sMessage+=key;
				}
				sMessage+=" not found.";
			}
		}
		/* Thrown when a searched oKey was not found. */
		public class KeyNotFoundException:KeyException {
			public KeyNotFoundException(object oKey,Extent etExtent):base(oKey,etExtent) {
			}
		}
		/* Thrown when an accessed oKey does not exist. */
		public class KeyDoesNotExistException:KeyException {
			public KeyDoesNotExistException(object oKey,Extent etExtent):base(oKey,etExtent) {
			}
		}
	}
	namespace Types  {
		/* Everything implementing this interface can be used in a Call expression */
		public interface ICallable {
			object oCallO(object oArgument);
		}
		// TODO: Rename this eventually
		public interface IMap: IKeyValue {
			IMap MParent {
				get;
				set;
			}
			ArrayList AoIntegerKeyValues {
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
			ArrayList AoKeys {
				get;
			}
			int ICount {
				get;
			}
			bool BContainsO(object key);			
		}		
		/* Represents a lazily evaluated "library" Meta file. */
		public class MetaLibrary { // TODO: Put this into Library class, make base class for everything that gets loaded
			public object OLoad() {
				return Interpreter.ORunFnM(sPath,new Map()); // TODO: Improve this interface, isn't read lazily anyway
			}
			public MetaLibrary(string sPath) {
				this.sPath=sPath;
			}
			string sPath;
		}
		public class LazyNamespace: IKeyValue { // TODO: Put this into library, combine with MetaLibrary
			public object this[object oKey] {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache[oKey];
				}
				set {
					throw new ApplicationException("Cannot set oKey "+oKey.ToString()+" in .NET namespace.");
				}
			}
			public ArrayList AoKeys {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache.AoKeys;
				}
			}
			public int ICount {
				get {
					if(mCache==null) {
						Load();
					}
					return mCache.ICount;
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
					mCache=(Map)Interpreter.KvMergeAkv(mCache,mCachedAssembly.MNamespaceContentsS(sFullName));
				}
				foreach(DictionaryEntry dtretEntry in htsNamespaces) {
					mCache[new Map((string)dtretEntry.Key)]=dtretEntry.Value;
				}
			}
			public Map mCache;
			public bool BContainsO(object key) {
				if(mCache==null) {
					Load();
				}
				return mCache.BContainsO(key);
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
			public Map MNamespaceContentsS(string sNamespace) {
				if(mAssemblyContent==null) {
					mAssemblyContent=Library.MLoadAssembliesAs(new object[] {asbAssembly});
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
			public object this[object oKey] {
				get {
					if(mCache.BContainsO(oKey)) {
						if(mCache[oKey] is MetaLibrary) {
							mCache[oKey]=((MetaLibrary)mCache[oKey]).OLoad();
						}
						return mCache[oKey];
					}
					else {
						return null;
					}
				}
				set {
					throw new ApplicationException("Cannot set oKey "+oKey.ToString()+" in library.");
				}
			}
			public ArrayList AoKeys {
				get {
					return mCache.AoKeys;
				}
			}
			public IMap MClone() {
				return this;
			}
			public int ICount {
				get {
					return mCache.ICount;
				}
			}
			public bool BContainsO(object oKey) {
				return mCache.BContainsO(oKey);
			}
			public ArrayList AoIntegerKeyValues {
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
					object oTemporary=mCache[dtretEntry.Key];				  // or remove IEnumerable from IMap (only needed for foreach)
				}														// decide later
				return mCache.GetEnumerator();
			}
			public static Map MLoadAssembliesAs(IEnumerable enmrbasbAssmblies) {
				Map mRoot=new Map();
				foreach(Assembly asbCurrent in enmrbasbAssmblies) {
					foreach(Type tCurrent in asbCurrent.GetExportedTypes())  {
						if(tCurrent.DeclaringType==null)  {
							Map mPosition=mRoot;
							ArrayList asSubPaths=new ArrayList(tCurrent.FullName.Split('.'));
							asSubPaths.RemoveAt(asSubPaths.Count-1);
							foreach(string sSubPath in asSubPaths)  {
								if(!mPosition.BContainsO(new Map(sSubPath)))  {
									mPosition[new Map(sSubPath)]=new Map();
								}
								mPosition=(Map)mPosition[new Map(sSubPath)];
							}
							mPosition[new Map(tCurrent.Name)]=new NetClass(tCurrent);
						}
					}
					Interpreter.asLoadedAssemblies.Add(asbCurrent.Location);
				}
				return mRoot;
			}
			private static string SAssemblyNameAsbn(IAssemblyName iasbnName) { // TODO: make this return a string??
				AssemblyName asbnName = new AssemblyName();
				asbnName.Name = AssemblyCache.GetName(iasbnName);
				asbnName.Version = AssemblyCache.GetVersion(iasbnName);
				asbnName.CultureInfo = AssemblyCache.GetCulture(iasbnName);
				asbnName.SetPublicKeyToken(AssemblyCache.GetPublicKeyToken(iasbnName));
				return asbnName.Name;
			}
			public Library() {
				ArrayList aasbAssemblies=new ArrayList();
				sLibraryPath=Path.Combine(Interpreter.sInstallationPath,"library");
				IAssemblyEnum iasbenAssemblyEnum=AssemblyCache.CreateGACEnum();
				IAssemblyName iasbnName; 
//				AssemblyName asbnName;
				aasbAssemblies.Add(Assembly.LoadWithPartialName("mscorlib"));
				while (AssemblyCache.GetNextAssembly(iasbenAssemblyEnum, out iasbnName) == 0) {
					try {
						string sAssemblyName=SAssemblyNameAsbn(iasbnName);
						aasbAssemblies.Add(Assembly.LoadWithPartialName(sAssemblyName));
					}
					catch(Exception e) {
						//Console.WriteLine("Could not load gac assembly :"+System.GAC.AssemblyCache.GetName(an));
					}
				}
				foreach(string fnCurrentDll in Directory.GetFiles(sLibraryPath,"*.dll")) {
					aasbAssemblies.Add(Assembly.LoadFrom(fnCurrentDll));
				}
				foreach(string fnCurrentExe in Directory.GetFiles(sLibraryPath,"*.exe")) {
					aasbAssemblies.Add(Assembly.LoadFrom(fnCurrentExe));
				}
				string fnCachedAssemblyInfo=Path.Combine(Interpreter.sInstallationPath,"mCachedAssemblyInfo.meta"); // TODO: Use another asbnName that doesn't collide with C# meaning
				if(File.Exists(fnCachedAssemblyInfo)) {
					mCachedAssemblyInfo=(Map)Interpreter.RunWithoutLibrary(fnCachedAssemblyInfo,new Map());
				}
				
				mCache=MLoadNamespacesAasb(aasbAssemblies);
				Interpreter.SaveToFileOFn(mCachedAssemblyInfo,fnCachedAssemblyInfo);
				foreach(string fnCurrentMeta in Directory.GetFiles(sLibraryPath,"*.meta")) {
					mCache[new Map(Path.GetFileNameWithoutExtension(fnCurrentMeta))]=new MetaLibrary(fnCurrentMeta);
				}
			}
			private Map mCachedAssemblyInfo=new Map();
			public ArrayList AsNamespacesAsb(Assembly asbAssembly) { //refactor, integrate into MLoadNamespacesAasb???
				ArrayList asNamespaces=new ArrayList();
				if(mCachedAssemblyInfo.BContainsO(new Map(asbAssembly.Location))) {
					Map info=(Map)mCachedAssemblyInfo[new Map(asbAssembly.Location)];
					string sTimeStamp=((Map)info[new Map("timestamp")]).SString;
					if(sTimeStamp.Equals(File.GetCreationTime(asbAssembly.Location).ToString())) {
						Map mNamespaces=(Map)info[new Map("namespaces")];
						foreach(DictionaryEntry dtretEntry in mNamespaces) {
							string text=((Map)dtretEntry.Value).SString;
							asNamespaces.Add(text);
						}
						return asNamespaces;
					}
				}
				foreach(Type tType in asbAssembly.GetExportedTypes()) {
					if(!asNamespaces.Contains(tType.Namespace)) {
						if(tType.Namespace==null) {
							if(!asNamespaces.Contains("")) {
								asNamespaces.Add("");
							}
						}
						else {
							asNamespaces.Add(tType.Namespace);
						}
					}
				}
				Map mCachedAssemblyInfoMap=new Map();
				Map mNamespace=new Map();
				Integer counter=new Integer(0);
				foreach(string na in asNamespaces) {
					mNamespace[counter]=new Map(na);
					counter++;
				}
				mCachedAssemblyInfoMap[new Map("namespaces")]=mNamespace;
				mCachedAssemblyInfoMap[new Map("timestamp")]=new Map(File.GetCreationTime(asbAssembly.Location).ToString());
				mCachedAssemblyInfo[new Map(asbAssembly.Location)]=mCachedAssemblyInfoMap;
				return asNamespaces;
			}
			public Map MLoadNamespacesAasb(ArrayList aasbAssemblies) {
				LazyNamespace lznsRoot=new LazyNamespace("");
				foreach(Assembly assembly in aasbAssemblies) {
					ArrayList asNamespaces=AsNamespacesAsb(assembly);
					CachedAssembly casbCachedAssembly=new CachedAssembly(assembly);
					foreach(string sNamespace in asNamespaces) {
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
					return Interpreter.ODotNetFromMetaO(mMap[Interpreter.OMetaFromDotNetO(oKey)]);
				}
				set {
					this.mMap[Interpreter.OMetaFromDotNetO(oKey)]=Interpreter.OMetaFromDotNetO(value);
				}
			}
		}

		//TODO: cache the AoIntegerKeyValues somewhere; put in an "Add" method
		public class Map: IKeyValue, IMap, ICallable, IEnumerable, ISerializeSpecial {
			public static readonly Map sParent=new Map("parent");
			public static readonly Map sArg=new Map("arg");
			public static readonly Map sThis=new Map("this");
			public object OArgument {
				get {
					return oArg;
				}
				set { // TODO: Remove set, maybe?
					oArg=value;
				}
			}
			object oArg=null;
			public bool BIsString {
				get {
					return mstgTable.BIsString;
				}
			}
			public string SString {
				get {// Refactoring: has a stupid name, Make property
						 return mstgTable.SString;
					 }
			}
			public IMap MParent {
				get {
					return mParent;
				}
				set {
					mParent=value;
				}
			}
			public int ICount {
				get {
					return mstgTable.ICount;
				}
			}
			public ArrayList AoIntegerKeyValues { 
				get {
					return mstgTable.AoIntegerKeyValues;
				}
			}
			public virtual object this[object oKey]  {
				get {
					if(oKey.Equals(sParent)) {
						return MParent;
					}
					else if(oKey.Equals(sArg)) {
						return OArgument;
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
//			public object OExecute() { // TODO: Rename to evaluate
//				Expression eFunction=(Expression)ECompile();
//				object oResult;
//				oResult=eFunction.OEvaluateM(this);
//				return oResult;
//			}
			public object oCallO(object oArgument) {
				this.OArgument=oArgument;
				Expression eFunction=(Expression)((Map)this[Expression.sRun]).ECompile();
				object oResult;
				oResult=eFunction.OEvaluateM(this);
				return oResult;
			}
			public ArrayList AoKeys {
				get {
					return mstgTable.AoKeys;
				}
			}
			public IMap MClone() {
				Map mClone=mstgTable.CloneMap();
				mClone.MParent=MParent;
				mClone.eCompiled=eCompiled;
				mClone.EtExtent=EtExtent;
				return mClone;
			}
			public Expression ECompile()  { // eCompiled Statements are not cached, only expressions
				if(eCompiled==null)  {
					if(this.BContainsO(Meta.Execution.Call.sCall)) {
						eCompiled=new Call(this);
					}
					else if(this.BContainsO(Delayed.sDelayed)) { // TODO: could be optimized, but compilation happens seldom
						eCompiled=new Delayed(this);
					}
					else if(this.BContainsO(Program.sProgram)) {
						eCompiled=new Program(this);
					}
					else if(this.BContainsO(Literal.sLiteral)) {
						eCompiled=new Literal(this);
					}
					else if(this.BContainsO(Search.sSearch)) {// TODO: use static expression strings
						eCompiled=new Search(this);
					}
					else if(this.BContainsO(Select.sSelect)) {
						eCompiled=new Select(this);
					}
					else {
						throw new ApplicationException("Cannot compile non-code map.");
					}
				}
					((Expression)eCompiled).EtExtent=this.EtExtent;
				return eCompiled;
			}
			public bool BContainsO(object oKey)  {
				if(oKey is Map) {
					if(oKey.Equals(sArg)) {
						return this.OArgument!=null;
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
			public string SSerializeSAs(string sIndentation,string[] asFunctions) {
				if(this.BIsString) {
					return sIndentation+"\""+this.SString+"\""+"\n";
				}
				else {
					return null;
				}
			}
			public abstract class MapStrategy { // TODO: Call this differently, hungarian-compatible
				public Map mMap;
				public MapStrategy Clone() {
					MapStrategy mstgStrategy=new HybridDictionaryStrategy();
					foreach(object oKey in this.AoKeys) {
						mstgStrategy[oKey]=this[oKey];
					}
					return mstgStrategy;	
				}
				public abstract Map CloneMap();
				public abstract ArrayList AoIntegerKeyValues {
					get;
				}
				public abstract bool BIsString {
					get;
				}
				
				// TODO: Rename. Reason: This really means something more abstract, more along the lines of,
				// "is this a mMap that only has integers as children, and maybe also only integers as keys?"
				public abstract string SString {
					get;
				}
				public abstract ArrayList AoKeys {
					get;
				}
				public abstract int ICount {
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
					foreach(object oKey in this.AoKeys) {
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
					if(mstgToCompare.ICount!=this.ICount) {
						return false;
					}
					foreach(object key in this.AoKeys)  {
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
				public override ArrayList AoIntegerKeyValues {
					get {
						ArrayList aList=new ArrayList();
						foreach(char iChar in sText) {
							aList.Add(new Integer(iChar));
						}
						return aList;
					}
				}
				public override bool BIsString {
					get {
						return true;
					}
				}
				public override string SString {
					get {
						return sText;
					}
				}
				public override ArrayList AoKeys {
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
				public override int ICount {
					get {
						return sText.Length;
					}
				}
				public override object this[object oKey]  {
					get {
						if(oKey is Integer) {
							int iInteger=((Integer)oKey).Int;
							if(iInteger>0 && iInteger<=this.ICount) {
								return new Integer(sText[iInteger-1]);
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
						return ((Integer)oKey)>0 && ((Integer)oKey)<=this.ICount;
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
				public override ArrayList AoIntegerKeyValues {
					get {
						ArrayList aList=new ArrayList();
						for(Integer iInteger=new Integer(1);ContainsKey(iInteger);iInteger++) {
							aList.Add(this[iInteger]);
						}
						return aList;
					}
				}
				public override bool BIsString {
					get {
						if(AoIntegerKeyValues.Count>0) {
							try {
								object o=SString;// TODO: a bit of a hack
								return true;
							}
							catch{
							}
						}
						return false;
					}
				}
				public override string SString { // TODO: looks too complicated
					get {
						string sText="";
						foreach(object oKey in this.AoKeys) {
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
				}
				public class MapException:ApplicationException { // TODO: Remove or make sense of this
					Map mMap;
					public MapException(Map mMap,string sMessage):base(sMessage) {
						this.mMap=mMap;
					}
				}
				public override ArrayList AoKeys {
					get {
						return aoKeys;
					}
				}
				public override int ICount {
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
					return new DictionaryEntry(mMap.AoKeys[iCurrent],mMap[mMap.AoKeys[iCurrent]]);
				}
			}
			public bool MoveNext() {
				iCurrent++;
				return iCurrent<mMap.ICount;
			}
			public void Reset() {
				iCurrent=-1;
			}
			private int iCurrent=-1;
		}
		public delegate object DelegateCreatedForGenericDelegates(); // TODO: rename?
		public class NetMethod: ICallable {
			// TODO: Move this to "With" ? Move this to NetContainer?
			public static object oAssignCollectionMOOutb(Map mCollection,object oCollection,out bool bSuccess) { // TODO: is bSuccess needed?
				if(mCollection.AoIntegerKeyValues.Count==0) {
					bSuccess=false;
					return null;
				}
				Type tTarget=oCollection.GetType();
				MethodInfo mtifAdding=tTarget.GetMethod("Add",new Type[]{mCollection.AoIntegerKeyValues[0].GetType()});
				if(mtifAdding!=null) {
					foreach(object oEntry in mCollection.AoIntegerKeyValues) { // combine this with Library function "Init"
						mtifAdding.Invoke(oCollection,new object[]{oEntry});//  call mtifAdding from above!
					}
					bSuccess=true;
				}
				else {
					bSuccess=false;
				}

				return oCollection;
			}
			// TODO: finally invent a Meta tTarget??? Would be useful here for prefix to Meta,
			// it isn't, after all just any object
			public static object oConvertParameterOTOutb(object oMeta,Type tParameter,out bool outbConverted) {
				outbConverted=true;
				if(tParameter.IsAssignableFrom(oMeta.GetType())) {
					return oMeta;
				}
				else if((tParameter.IsSubclassOf(typeof(Delegate))
					||tParameter.Equals(typeof(Delegate))) && (oMeta is Map)) { // TODO: add check, that the m contains code, not necessarily, think this conversion stuff through completely
					MethodInfo mtifInvoke=tParameter.GetMethod("Invoke",BindingFlags.Instance
						|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					Delegate dlgFunction=delFromF(tParameter,mtifInvoke,(Map)oMeta);
					return dlgFunction;
				}
				else if(tParameter.IsArray && oMeta is IMap && ((Map)oMeta).AoIntegerKeyValues.Count!=0) {// TODO: cheating, not very understandable
					try {
						Type tElements=tParameter.GetElementType();
						Map mArgument=((Map)oMeta);
						ArrayList aArgument=mArgument.AoIntegerKeyValues;
						Array arArgument=Array.CreateInstance(tElements,aArgument.Count);
						for(int i=0;i<aArgument.Count;i++) {
							arArgument.SetValue(aArgument[i],i);
						}
						return arArgument;
					}
					catch {
					}
				}
				else {
					bool outbParamConverted; // TODO: refactor with outbConverted
					object oResult=Interpreter.ODotNetFromMetaO(oMeta,tParameter,out outbParamConverted);
					if(outbParamConverted) {
						return oResult;
					}
				}
				outbConverted=false;
				return null;
			}
			public object oCallO(object oArgument) {
				if(this.tTarget.Name.EndsWith("IndexerNoConversion") && this.sName.StartsWith("GetResultFromDelegate")) {
					int asdf=0;
				}
				object oReturn=null;
				object oResult=null;
				// TODO: this will have to be refactored, but later, after feature creep

				// try to call with just one argument:
				ArrayList aOneArgumentMethods=new ArrayList();
				foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
					if(mtbCurrent.GetParameters().Length==1) { // don't match if different parameter list length
						aOneArgumentMethods.Add(mtbCurrent);
					}
				}
				bool bExecuted=false;
				foreach(MethodBase mtbCurrent in aOneArgumentMethods) {
					bool bConverted;
					object oParameter=oConvertParameterOTOutb(oArgument,mtbCurrent.GetParameters()[0].ParameterType,out bConverted);
					if(bConverted) {
						if(mtbCurrent is ConstructorInfo) {
							oReturn=((ConstructorInfo)mtbCurrent).Invoke(new object[] {oParameter});
						}
						else {
							oReturn=mtbCurrent.Invoke(oTarget,new object[] {oParameter});
						}
						bExecuted=true;// remove, use bArgumentsMatched instead
						break;
					}
				}
				if(!bExecuted) {
					ArrayList aOArguments=((IMap)oArgument).AoIntegerKeyValues;
					ArrayList aMtifRightNumberArguments=new ArrayList();
					foreach(MethodBase mtbCurrent in arMtbOverloadedMethods) {
						if(aOArguments.Count==mtbCurrent.GetParameters().Length) { // don't match if different parameter list length
							if(aOArguments.Count==((IMap)oArgument).AoKeys.Count) { // only call if there are no non-integer keys ( move this somewhere else)
								aMtifRightNumberArguments.Add(mtbCurrent);
							}
						}
					}
					if(aMtifRightNumberArguments.Count==0) {
						int asdf=0;//throw new ApplicationException("No methods with the right number of arguments.");// TODO: Just a quickfix, really
					}
					foreach(MethodBase mtbCurrent in aMtifRightNumberArguments) {
						ArrayList aArguments=new ArrayList();
						bool bArgumentsMatched=true;
						ParameterInfo[] arPrmtifParameters=mtbCurrent.GetParameters();
						for(int i=0;bArgumentsMatched && i<arPrmtifParameters.Length;i++) {
							aArguments.Add(oConvertParameterOTOutb(aOArguments[i],arPrmtifParameters[i].ParameterType,out bArgumentsMatched));
						}
						if(bArgumentsMatched) {
							if(mtbCurrent is ConstructorInfo) {
								oReturn=((ConstructorInfo)mtbCurrent).Invoke(aArguments.ToArray());
							}
							else {
								oReturn=mtbCurrent.Invoke(oTarget,aArguments.ToArray());
							}
							bExecuted=true;// remove, use bArgumentsMatched instead
							break;
						}
					}
				}
				// TODO: oResult / oReturn is duplication
				oResult=oReturn; // mess, why is this here? put in else after the next if
				if(oResult==null) {
					int asdf=0;
				}
				return Interpreter.OMetaFromDotNetO(oResult);
			}
			/* Create a delegate of a certain tTarget that calls a Meta function. */
			public static Delegate delFromF(Type tDelegate,MethodInfo mtifMethod,Map mCode) { // TODO: tDelegate, mtifMethode, redundant?
				mCode.MParent=(IMap)Interpreter.amCallers[Interpreter.amCallers.Count-1];
				CSharpCodeProvider mCodeProvider=new CSharpCodeProvider();
				ICodeCompiler iCodeCompiler=mCodeProvider.CreateCompiler();
				string sReturnType;
				if(mtifMethod==null) {
					sReturnType="object";
				}
				else {
					sReturnType=mtifMethod.ReturnType.Equals(typeof(void)) ? "void":mtifMethod.ReturnType.FullName;
				}
				string sSource="using System;using Meta.Types;using Meta.Execution;";
				sSource+="public class EventHandlerContainer{public "+sReturnType+" EventHandlerMethod";
				int iCounter=1;
				string sArgumentList="(";
				string sArgumentAdding="Map mArg=new Map();";
				if(mtifMethod!=null) {
					foreach(ParameterInfo prmifParameter in mtifMethod.GetParameters()) {
						sArgumentList+=prmifParameter.ParameterType.FullName+" mArg"+iCounter;
						sArgumentAdding+="mArg[new Integer("+iCounter+")]=mArg"+iCounter+";";
						if(iCounter<mtifMethod.GetParameters().Length) {
							sArgumentList+=",";
						}
						iCounter++;
					}
				}
				sArgumentList+=")";
				sSource+=sArgumentList+"{";
				sSource+=sArgumentAdding;
				sSource+="object oResult=mCallable.oCallO(mArg);";
				if(mtifMethod!=null) {
					if(!mtifMethod.ReturnType.Equals(typeof(void))) {
						sSource+="return ("+sReturnType+")";
						sSource+="Interpreter.ODotNetFromMetaO(oResult,typeof("+sReturnType+"));"; // does conversion even make sense here? Must be outbConverted back anyway.
					}
				}
				else {
					sSource+="return";
					sSource+=" oResult;";
				}
				sSource+="}";
				sSource+="private Map mCallable;";
				sSource+="public EventHandlerContainer(Map mCallable) {this.mCallable=mCallable;}}";
				string oMetaDllLocation=Assembly.GetAssembly(typeof(Map)).Location;
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll",oMetaDllLocation});
				assemblyNames.AddRange(Interpreter.asLoadedAssemblies);
				CompilerParameters  compilerParameters=new CompilerParameters((string[])assemblyNames.ToArray(typeof(string)));
				CompilerResults compilerResults=iCodeCompiler.CompileAssemblyFromSource(compilerParameters,sSource);
				Type tContainer=compilerResults.CompiledAssembly.GetType("EventHandlerContainer",true);
				object oContainer=tContainer.GetConstructor(new Type[]{typeof(Map)}).Invoke(new object[] {
																																			  mCode});
				if(mtifMethod==null) {
					tDelegate=typeof(DelegateCreatedForGenericDelegates);
				}
				Delegate dlgResult=Delegate.CreateDelegate(tDelegate,
					oContainer,"EventHandlerMethod");
				return dlgResult;
			}
			private void nInitializeSOT(string sName,object oTarget,Type tTarget) {
				this.sName=sName;
				this.oTarget=oTarget;
				this.tTarget=tTarget;
				ArrayList aMtbMethods;
				if(sName==".ctor") {
					aMtbMethods=new ArrayList(tTarget.GetConstructors());
				}
				else {
					aMtbMethods=new ArrayList(tTarget.GetMember(sName,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static));
				}
				aMtbMethods.Reverse(); // this is a hack for an invocation bug with a certain method I don't remember, maybe remove
				// found out, it's for Console.WriteLine, where Console.WriteLine(object)
				// would otherwise come before Console.WriteLine(string)
				// not a good solution, though

				// TODO: Get rid of this Reversion shit! Find a fix for this problem. Need to think about
				// it. maybe restrict overloads, create preference aMtbMethods, all quite complicated
				// research the number and nature of such arMtbOverloadedMethods as Console.WriteLine
				arMtbOverloadedMethods=(MethodBase[])aMtbMethods.ToArray(typeof(MethodBase));
			}
			public NetMethod(string name,object oTarget,Type tTarget) {
				this.nInitializeSOT(name,oTarget,tTarget);
			}
			public NetMethod(Type tTarget) {
				this.nInitializeSOT(".ctor",null,tTarget);
			}
			public override bool Equals(object oToCompare) {
				if(oToCompare is NetMethod) {
					NetMethod nmtToCompare=(NetMethod)oToCompare;
					if(nmtToCompare.oTarget==oTarget && nmtToCompare.sName.Equals(sName) && nmtToCompare.tTarget.Equals(tTarget)) {
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
			public override int GetHashCode() {
				unchecked {
					int iHash=sName.GetHashCode()*tTarget.GetHashCode();
					if(oTarget!=null) {
						iHash=iHash*oTarget.GetHashCode();
					}
					return iHash;
				}
			}
			private string sName;
			protected object oTarget;
			protected Type tTarget;

			public MethodBase[] arMtbOverloadedMethods;
		}
		public class NetClass: NetContainer, IKeyValue,ICallable {
			protected NetMethod nmtConstructor; // TODO: Why is this a NetMethod? Not really that good, I think. Might be better to separate the constructor stuff out from NetMethod.
			public NetClass(Type type):base(null,type) {
				this.nmtConstructor=new NetMethod(this.tType);
			}
			public object oCallO(object oArgument) {
				return nmtConstructor.oCallO(oArgument);
			}
		}
		/* Representation of a .NET object. */
		public class NetObject: NetContainer, IKeyValue {
			public NetObject(object oObject):base(oObject,oObject.GetType()) {
			}
			public override string ToString() {
				return oObject.ToString();
			}
		}
		/* Base class for NetObject and NetClass. */
		public abstract class NetContainer: IKeyValue, IEnumerable,ISerializeSpecial {
			public bool BContainsO(object oKey) {
				if(oKey is Map) {
					if(((Map)oKey).BIsString) {
						string sText=((Map)oKey).SString;
						if(tType.GetMember((string)oKey,
							BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance).Length!=0) {
							return true;
						}
					}
				}
				NetMethod nmtIndexer=new NetMethod("get_Item",oObject,tType);
				Map mArgument=new Map();
				mArgument[new Integer(1)]=oKey;
				try {
					nmtIndexer.oCallO(mArgument);
					return true;
				}
				catch(Exception) {
					return false;
				}
			}
			public IEnumerator GetEnumerator() {
				return Table.GetEnumerator();
			}
			// TODO: why does NetContainer have a kvlParent when it isn't ever used?
			public IKeyValue Parent {
				get {
					return kvlParent;
				}
				set {
					kvlParent=value;
				}
			}
			public ArrayList AoKeys {
				get {
					return new ArrayList(Table.Keys);
				}
			}
			public int ICount  {
				get {
					return Table.Count;
				}
			}
			public virtual object this[object oKey]  {
				get {
					if(oKey is Map && ((Map)oKey).BIsString) {
						string sText=((Map)oKey).SString;
						MemberInfo[] ambifMembers=tType.GetMember(sText,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(ambifMembers.Length>0) {
							if(ambifMembers[0] is MethodBase) {
								return new NetMethod(sText,oObject,tType);
							}
							if(ambifMembers[0] is FieldInfo) {
								// convert arrays to maps here?
								return Interpreter.OMetaFromDotNetO(tType.GetField(sText).GetValue(oObject));
							}
							else if(ambifMembers[0] is PropertyInfo) {
								return Interpreter.OMetaFromDotNetO(tType.GetProperty(sText).GetValue(oObject,new object[]{}));
							}
							else if(ambifMembers[0] is EventInfo) {
								Delegate dlgEvent=(Delegate)tType.GetField(sText,BindingFlags.Public|
									BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).GetValue(oObject);
								return new NetMethod("Invoke",dlgEvent,dlgEvent.GetType());
							}
						}
					}
					if(this.oObject!=null && oKey is Integer && this.tType.IsArray) {
						return Interpreter.OMetaFromDotNetO(((Array)oObject).GetValue(((Integer)oKey).Int)); // TODO: add error handling here
					}
					NetMethod nmtIndexer=new NetMethod("get_Item",oObject,tType);
					Map mArgument=new Map();
					mArgument[new Integer(1)]=oKey;
					try {
						return nmtIndexer.oCallO(mArgument);
					}
					catch(Exception e) {
						return null;
					}
				}
				set {
					if(oKey is Map && ((Map)oKey).BIsString) {
						string sText=((Map)oKey).SString;
						MemberInfo[] ambifMembers=tType.GetMember(sText,BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance);
						if(ambifMembers.Length>0) {
							if(ambifMembers[0] is MethodBase) {
								throw new ApplicationException("Cannot set mtifInvoke "+oKey+".");
							}
							else if(ambifMembers[0] is FieldInfo) {
								FieldInfo fifField=(FieldInfo)ambifMembers[0];
								bool oConverted;
								object oValue;
								oValue=NetMethod.oConvertParameterOTOutb(value,fifField.FieldType,out oConverted);
								if(oConverted) {
									fifField.SetValue(oObject,oValue);
								}
								if(!oConverted) {
									if(value is Map) {
										oValue=NetMethod.oAssignCollectionMOOutb((Map)value,fifField.GetValue(oObject),out oConverted);
									}
								}
								if(!oConverted) {
									throw new ApplicationException("Field "+fifField.Name+"could not be assigned because it cannot be oConverted.");
								}
								//TODO: refactor
								return;
							}
							else if(ambifMembers[0] is PropertyInfo) {
								PropertyInfo pptifProperty=(PropertyInfo)ambifMembers[0];
								bool oConverted;
								object oValue=NetMethod.oConvertParameterOTOutb(value,pptifProperty.PropertyType,out oConverted);
								if(oConverted) {
									pptifProperty.SetValue(oObject,oValue,new object[]{});
								}
								if(!oConverted) {
									if(value is Map) {
										NetMethod.oAssignCollectionMOOutb((Map)value,pptifProperty.GetValue(oObject,new object[]{}),out oConverted);
									}
									if(!oConverted) {
										throw new ApplicationException("Property "+this.tType.Name+"."+Interpreter.SaveToFileOFn(oKey,"",false)+" could not be set to "+value.ToString()+". The value can not be oConverted.");
									}
								}
								return;
							}
							else if(ambifMembers[0] is EventInfo) {
								((EventInfo)ambifMembers[0]).AddEventHandler(oObject,CreateEvent(sText,(Map)value));
								return;
							}
						}
					}
					if(oObject!=null && oKey is Integer && tType.IsArray) {
						bool bConverted; 
						object oConverted=Interpreter.ODotNetFromMetaO(value,tType.GetElementType(),out bConverted);
						if(bConverted) {
							((Array)oObject).SetValue(oConverted,((Integer)oKey).Int);
							return;
						}
					}
					NetMethod nmtIndexer=new NetMethod("set_Item",oObject,tType);
					Map mArgument=new Map();
					mArgument[new Integer(1)]=oKey;
					mArgument[new Integer(2)]=value;// do this more efficiently?
					try {
						nmtIndexer.oCallO(mArgument);
					}
					catch(Exception e) {
						throw new ApplicationException("Cannot set "+oKey.ToString()+".");
					}
				}
			}
			public string SSerializeSAs(string sIndent,string[] functions) {
				return sIndent;
			}
			public Delegate CreateEvent(string sName,Map mCode) {
				EventInfo evifEvent=tType.GetEvent(sName,BindingFlags.Public|BindingFlags.NonPublic|
															 BindingFlags.Static|BindingFlags.Instance);
				MethodInfo mtifInvoke=evifEvent.EventHandlerType.GetMethod("Invoke",BindingFlags.Instance|BindingFlags.Static
																						 |BindingFlags.Public|BindingFlags.NonPublic);
				Delegate dlgEvent=NetMethod.delFromF(evifEvent.EventHandlerType,mtifInvoke,mCode);
				return dlgEvent;
			}
			private IDictionary Table { // TODO: strange, what use is this
				get {
					HybridDictionary hbdtrTable=new HybridDictionary();
					BindingFlags bdfBinding;
					if(oObject==null)  {
						bdfBinding=BindingFlags.Public|BindingFlags.Static;
					}
					else  {
						bdfBinding=BindingFlags.Public|BindingFlags.Instance;
					}
					foreach(FieldInfo fifField in tType.GetFields(bdfBinding)) {
						hbdtrTable[new Map(fifField.Name)]=fifField.GetValue(oObject);
					}
					foreach(MethodInfo mtifInvoke in tType.GetMethods(bdfBinding))  {
						if(!mtifInvoke.IsSpecialName) {
							hbdtrTable[new Map(mtifInvoke.Name)]=new NetMethod(mtifInvoke.Name,oObject,tType);
						}
					}
					foreach(PropertyInfo pptifProperty in tType.GetProperties(bdfBinding)) {
						if(pptifProperty.Name!="Item" && pptifProperty.Name!="Chars") {
							hbdtrTable[new Map(pptifProperty.Name)]=pptifProperty.GetValue(oObject,new object[]{});
						}
					}
					foreach(EventInfo evifEvent in tType.GetEvents(bdfBinding)) {
						hbdtrTable[new Map(evifEvent.Name)]=new NetMethod(evifEvent.GetAddMethod().Name,this.oObject,this.tType);
					}
					int iCounter=1;
					if(oObject!=null && oObject is IEnumerable && !(oObject is String)) { // is this useful?
						foreach(object oEntry in (IEnumerable)oObject) {
							if(oEntry is DictionaryEntry) {
								hbdtrTable[Interpreter.OMetaFromDotNetO(((DictionaryEntry)oEntry).Key)]=((DictionaryEntry)oEntry).Value;
							}
							else {
								hbdtrTable[new Integer(iCounter)]=oEntry;
								iCounter++;
							}
						}
					}
					return hbdtrTable;
				}
			}
			public NetContainer(object oObject,Type tType) {
				this.oObject=oObject;
				this.tType=tType;
			}
			private IKeyValue kvlParent;
			public object oObject;
			public Type tType;
		}
	}
	namespace Parser  {
		public class IndentationStream: TokenStream {
			public IndentationStream(TokenStream tksStream)  {
				this.tksStream=tksStream;
				AddIndentationTokensToGetToLevel(0,new Token()); // TODO: remove "new Token" ?
			}
			public Token nextToken()  {
				if(qBuffer.Count==0)  {
					Token tkToken=tksStream.nextToken();
					switch(tkToken.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokensToGetToLevel(-1,tkToken);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokensToGetToLevel(tkToken.getText().Length,tkToken);
							break;
						case MetaLexerTokenTypes.LITERAL: // move this into parser, for correct error handling?
							string sIndentation="";
							for(int iIndex=0;iIndex<iIndentation+1;iIndex++) {
								sIndentation+='\t';
							}
							string sText=tkToken.getText();
							sText=sText.Replace(Environment.NewLine,"\n"); // replace so we can use Split, which only works with characters
							string[] asLines=sText.Split('\n');
							string sResult="";
							for(int iIndex=0;iIndex<asLines.Length;iIndex++) {
								if(iIndex!=0 && asLines[iIndex].StartsWith(sIndentation)) {
									sResult+=asLines[iIndex].Remove(0,iIndentation+1);
								}
								else {
									sResult+=asLines[iIndex];
								}
								if(iIndex!=asLines.Length-1) {
									sResult+=Environment.NewLine;
								}
							}
							tkToken.setText(sResult);
							qBuffer.Enqueue(tkToken);
							break;
						default:
							qBuffer.Enqueue(tkToken);
							break;
					}
				}
				return (Token)qBuffer.Dequeue();
			}
			protected void AddIndentationTokensToGetToLevel(int iNewIndentation,Token tkToken)  { // TODO: use Extent instead of Token, or just the line we're in
				int iDifference=iNewIndentation-iIndentation; 
				if(iDifference==0) {
					qBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));//TODO: use something else here
				}
				else if(iDifference==1) {
					qBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				}
				else if(iDifference<0) {
					for(int i=iDifference;i<0;i++) {
						qBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					qBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE)); // TODO: tiny bit unlogical? maybe create this in Parser?
				}
				else if(iDifference>1) {
					// This doesn't get through properly because it is caught by ANTLR
					// TODO: make extra exception later.
					// I don't understand it and the lines are somehow off
					throw new RecognitionException("Incorrect indentation.",tkToken.getFilename(),tkToken.getLine(),tkToken.getColumn());
				}
				iIndentation=iNewIndentation;
			}
			protected Queue qBuffer=new Queue();
			protected TokenStream tksStream;
			protected int iIndentation=-1;
		}
	}
	namespace TestingFramework {
		public interface ISerializeSpecial {
			string SSerializeSAs(string indent,string[] functions);
		}
		public abstract class TestCase {
			public abstract object RunTestCase();
		}
		public class ExecuteTests {	
			public ExecuteTests(Type tTestContainer,string fnResults) { // refactor -maybe, looks quite ok
				bool bWaitAtEnd=false;
				Type[] atTests=tTestContainer.GetNestedTypes();
				foreach(Type tTest in atTests) {
					object[] aoCustomAttributes=tTest.GetCustomAttributes(typeof(SerializeMethodsAttribute),false);
					string[] asMethodNames=new string[0];
					if(aoCustomAttributes.Length!=0) {
						asMethodNames=((SerializeMethodsAttribute)aoCustomAttributes[0]).asMethods;
					}
					Console.Write(tTest.Name + "...");
					DateTime dtStarted=DateTime.Now;
					string sOutput="";
					object oResutl=((TestCase)tTest.GetConstructors()[0].Invoke(new object[]{})).RunTestCase();
					TimeSpan tsTestCase=DateTime.Now-dtStarted;
					bool bSuccessful=CompareResults(Path.Combine(fnResults,tTest.Name),oResutl,asMethodNames);
					if(!bSuccessful) {
						sOutput=sOutput + " failed";
						bWaitAtEnd=true;
					}
					else {
						sOutput+=" succeeded";
					}
					sOutput=sOutput + "  " + tsTestCase.TotalSeconds.ToString() + " s";
					Console.WriteLine(sOutput);
				}
				if(bWaitAtEnd) {
					Console.ReadLine();
				}
			}
			private bool CompareResults(string sPath,object oObject,string[] asFunctions) {
				Directory.CreateDirectory(sPath);
				if(!File.Exists(Path.Combine(sPath,"sCheck.txt"))) {
					File.Create(Path.Combine(sPath,"sCheck.txt")).Close();
				}
				string sResult=Serialize(oObject,"",asFunctions);
				StreamWriter swResult=new StreamWriter(Path.Combine(sPath,"result.txt"));
				swResult.Write(sResult);
				swResult.Close();
				StreamWriter swCopyResult=new StreamWriter(Path.Combine(sPath,"resultCopy.txt"));
				swCopyResult.Write(sResult);
				swCopyResult.Close();
				// TODO: Introduce utility methods
				StreamReader srCheck=new StreamReader(Path.Combine(sPath,"check.txt"));
				string sCheck=srCheck.ReadToEnd();
				srCheck.Close();
				return sResult.Equals(sCheck);
			}
			public static string Serialize(object oObject) {
				return Serialize(oObject,"",new string[]{});
			}
			public static string Serialize(object oSerialize,string sIndent,string[] asMethods) {
				if(oSerialize==null) {
					return sIndent+"null\n";
				}
				if(oSerialize is ISerializeSpecial) {
					string sText=((ISerializeSpecial)oSerialize).SSerializeSAs(sIndent,asMethods);
					if(sText!=null) {
						return sText;
					}
				}
				if(oSerialize.GetType().GetMethod("ToString",BindingFlags.Public|BindingFlags.DeclaredOnly|
					BindingFlags.Instance,null,new Type[]{},new ParameterModifier[]{})!=null) {
					return sIndent+"\""+oSerialize.ToString()+"\""+"\n";
				}
				if(oSerialize is IEnumerable) {
					string sText="";
					foreach(object oEntry in (IEnumerable)oSerialize) {
						sText+=sIndent+"Entry ("+oEntry.GetType().Name+")\n"+Serialize(oEntry,sIndent+"  ",asMethods);
					}
					return sText;
				}
				string sT=""; // TODO: maybe refactor this to all use the same variable
				ArrayList ambifMembers=new ArrayList();

				ambifMembers.AddRange(oSerialize.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance));
				ambifMembers.AddRange(oSerialize.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance));
				foreach(string sMethod in asMethods) {
					MethodInfo mtifCurrent=oSerialize.GetType().GetMethod(sMethod,BindingFlags.Public|BindingFlags.Instance);
					if(mtifCurrent!=null) { /* Only add mtifCurrent to ambifMembers if it really exists, this isn't sure because asMethods are supplied per test not per class. */
						ambifMembers.Add(mtifCurrent);
					}
				}
				ambifMembers.Sort(new CompareMemberInfos());
				foreach(MemberInfo mbifMember in ambifMembers) {
					if(mbifMember.Name!="Item") {
						if(mbifMember.GetCustomAttributes(typeof(DontSerializeFieldOrPropertyAttribute),false).Length==0) {
							if(oSerialize.GetType().Namespace==null ||!oSerialize.GetType().Namespace.Equals("System.Windows.Forms")) { // ugly hack to avoid some srange behaviour of some classes in System.Windows.Forms
								object oValue=oSerialize.GetType().InvokeMember(mbifMember.Name,BindingFlags.Public
									|BindingFlags.Instance|BindingFlags.GetProperty|BindingFlags.GetField
									|BindingFlags.InvokeMethod,null,oSerialize,null);
								sT+=sIndent+mbifMember.Name;
								if(oValue!=null) {
									sT+=" ("+oValue.GetType().Name+")";
								}
								sT+=":\n"+Serialize(oValue,sIndent+"  ",asMethods);
							}
						}
					}
				}
				return sT;
			}
		}
		class CompareMemberInfos:IComparer {
			public int Compare(object oFirst,object oSecond) {
				if(oFirst==null || oSecond==null || ((MemberInfo)oFirst).Name==null || ((MemberInfo)oSecond).Name==null) {
					return 0;
				}
				else {
					return ((MemberInfo)oFirst).Name.CompareTo(((MemberInfo)oSecond).Name);
				}
			}
		}
		[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
		public class DontSerializeFieldOrPropertyAttribute:Attribute {
		}
		[AttributeUsage(AttributeTargets.Class)]
		public class SerializeMethodsAttribute:Attribute {
			public string[] asMethods;
			public SerializeMethodsAttribute(string[] asMethods) {
				this.asMethods=asMethods;
			}
		}
	}
}
