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
		public abstract class ConvertMetaToDotNet { //rename
			public Type source;
			public Type target;
			public abstract object Convert(object obj);
		}
		public abstract class ConvertDotNetToMeta { //rename
			public Type source;
			public abstract object Convert(object obj);
		}
		public abstract class RecognizeLiteral {
			public abstract object Recognize(string text);
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
						cached=((RecognizeLiteral)Interpreter.Interceptions[i]).Recognize(text);
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
				if(keys.Count==2 && keys[1].Equals("Invoke")) {
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
						if(selected is IKeyValue) {
							Interpreter.breakMethod((IKeyValue)selected);
						}
						else {
							Interpreter.breakMethod(new NetObject(selected,selected.GetType()));
						}
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
//			public static void ShowHelpBackThread(object obj) {
//					Help.toolTip.Text=text;
//					Graphics graphics=toolTip.CreateGraphics();
//					Size size=graphics.MeasureString(text,
//						toolTip.Font).ToSize();
//					toolTip.Size=new Size(size.Width+10,size.Height+13);
//					toolTip.Visible=true;
//
//					TreeNode selected=Editor.SelectedNode;
//					int depth=0;
//					while(selected.Parent!=null) {
//						selected=selected.Parent;
//						depth++;
//					}
//					int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
//						Convert.ToInt32(
//						graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);
//
//					int y=5;
//					TreeNode node=Editor.SelectedNode;
//					while(node!=null) {
//						y+=node.Bounds.Height;
//						node=node.PrevVisibleNode;
//					}
//					toolTip.Location=new Point(x,y);
//
//				}
//				else {
//					listBox.Items.Clear();
//					if(obj==null) {
//						int asdf=0;
//					}
//					IKeyValue keyValue=obj is IKeyValue? (IKeyValue)obj:new NetObject(obj,obj.GetType());
//					ArrayList keys=new ArrayList();
//					foreach(DictionaryEntry entry in keyValue) {
//						keys.Add(entry.Key);
//					}
//					keys.Sort(new HelpComparer());
//					foreach(object key in keys) {
//						object member;
//						int imageIndex=0;
//						if(lastObject is Map) {
//							member=((Map)lastObject)[key];
//						}
//						else if(lastObject is NetClass) {
//							MemberInfo[] members=((NetClass)lastObject).type.GetMember(key.ToString(),
//								BindingFlags.Public|BindingFlags.Static);
//							member=members[0];
//						}
//						else if(!(lastObject is Map)) {
//							MemberInfo[] members=lastObject.GetType().GetMember(key.ToString(),
//								BindingFlags.Public|BindingFlags.Instance);
//							member=members.Length>0? members[0]:keyValue[key];
//						}
//						else {
//							throw new ApplicationException("bug here");
//						}
//						string additionalText="";
//
//						if(member is Type || member is NetClass) {
//							imageIndex=0;
//						}
//						else if(member is EventInfo) {
//							imageIndex=1;
//						}
//						else if(member is MethodInfo || member is ConstructorInfo || member is NetMethod) {
//							imageIndex=2;
//						}
//						else if(member is PropertyInfo) {
//							imageIndex=4;
//							object o=((PropertyInfo)member).GetValue(obj,new object[] {});
//							if(o==null) {
//								additionalText="null";
//							}
//							else {
//								additionalText=o.ToString();
//							}
//						}
//						else if(member is Map) {
//							imageIndex=5;
//						}
//						else {
//							if(member is FieldInfo) {
//								additionalText=((FieldInfo)member).GetValue(obj).ToString();
//							}
//							else {
//								additionalText=member.ToString();
//							}
//							imageIndex=6;
//						}
//						listBox.Items.Add(new GListBoxItem(key,additionalText,imageIndex));
//					}
//					Graphics graphics=Editor.window.CreateGraphics();
//					TreeNode selected=Editor.SelectedNode;
//					int depth=0;
//					while(selected.Parent!=null) {
//						selected=selected.Parent;
//						depth++;
//					}
//					int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
//						Convert.ToInt32(
//						graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);
//
//					int y=5;
//					TreeNode node=Editor.SelectedNode;
//					while(node!=null) {
//						if(node.Bounds.Y<0) {
//							break;
//						}	
//						y+=node.Bounds.Height;
//						node=node.PrevVisibleNode;
//
//					}
//					listBox.Location=new Point(x,y);
//					int greatest=0;
//					foreach(GListBoxItem item in listBox.Items) {
//						int width=graphics.MeasureString(item.Text,listBox.Font).ToSize().Width;
//						if(width>greatest){
//							greatest=width;
//						}								
//					}
//					listBox.Size=new Size(greatest+30,150<listBox.Items.Count*16+10? 150:listBox.Items.Count*16+10);
//					listBox.Show();
//					Editor.editor.Focus();
//				}
//				Editor.window.Activate();
//			}
//			public static void ShowHelpBackThread(object obj) {
//				lastObject=obj;
//				if(_isCall) {
//					string text="";
//					if(obj is NetMethod) {
//						//							if(lastSelectedNode!=null && lastSelectedNode==Editor.SelectedNode||
//						//								lastSelectedNode.Parent==Editor.SelectedNode.Parent
//						if(!lastMethodName.Equals(((NetMethod)obj).methods[0].Name)) {
//							overloadIndex=0;
//						}
//						overloadNumber=((NetMethod)obj).methods.Length;
//						if(overloadIndex>=overloadNumber) {
//							overloadIndex=0;
//						}
//						else if(overloadIndex<0) {
//							overloadIndex=overloadNumber-1;
//						}
//						text=Help.GetDoc(((NetMethod)obj).methods[overloadIndex],true,true,true);
//						lastMethodName=((NetMethod)obj).methods[0].Name;
//					}
//					else if (obj is NetClass) {
//						if(!lastMethodName.Equals(((NetClass)obj).constructor.methods[0].Name)) {
//							overloadIndex=0;
//						}
//						overloadNumber=((NetClass)obj).constructor.methods.Length;
//						if(overloadIndex>=overloadNumber) {
//							overloadIndex=0;
//						}
//						else if(overloadIndex<0) {
//							overloadIndex=overloadNumber-1;
//						}
//						text=Help.GetDoc(((NetClass)obj).constructor.methods[overloadIndex],true,true,true);
//						lastMethodName=((NetClass)obj).constructor.methods[0].Name;
//					}
//					else if(obj is Map) {
//						text=FunctionHelp((Map)obj);
//					}
//					Help.toolTip.Text=text;
//					Graphics graphics=toolTip.CreateGraphics();
//					Size size=graphics.MeasureString(text,
//						toolTip.Font).ToSize();
//					toolTip.Size=new Size(size.Width+10,size.Height+13);
//					toolTip.Visible=true;
//
//					TreeNode selected=Editor.SelectedNode;
//					int depth=0;
//					while(selected.Parent!=null) {
//						selected=selected.Parent;
//						depth++;
//					}
//					int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
//						Convert.ToInt32(
//						graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);
//
//					int y=5;
//					TreeNode node=Editor.SelectedNode;
//					while(node!=null) {
//						y+=node.Bounds.Height;
//						node=node.PrevVisibleNode;
//					}
//					toolTip.Location=new Point(x,y);
//
//				}
//				else {
//					listBox.Items.Clear();
//					if(obj==null) {
//						int asdf=0;
//					}
//					IKeyValue keyValue=obj is IKeyValue? (IKeyValue)obj:new NetObject(obj,obj.GetType());
//					ArrayList keys=new ArrayList();
//					foreach(DictionaryEntry entry in keyValue) {
//						keys.Add(entry.Key);
//					}
//					keys.Sort(new HelpComparer());
//					foreach(object key in keys) {
//						object member;
//						int imageIndex=0;
//						if(lastObject is Map) {
//							member=((Map)lastObject)[key];
//						}
//						else if(lastObject is NetClass) {
//							MemberInfo[] members=((NetClass)lastObject).type.GetMember(key.ToString(),
//								BindingFlags.Public|BindingFlags.Static);
//							member=members[0];
//						}
//						else if(!(lastObject is Map)) {
//							MemberInfo[] members=lastObject.GetType().GetMember(key.ToString(),
//								BindingFlags.Public|BindingFlags.Instance);
//							member=members.Length>0? members[0]:keyValue[key];
//						}
//						else {
//							throw new ApplicationException("bug here");
//						}
//						string additionalText="";
//
//						if(member is Type || member is NetClass) {
//							imageIndex=0;
//						}
//						else if(member is EventInfo) {
//							imageIndex=1;
//						}
//						else if(member is MethodInfo || member is ConstructorInfo || member is NetMethod) {
//							imageIndex=2;
//						}
//						else if(member is PropertyInfo) {
//							imageIndex=4;
//							object o=((PropertyInfo)member).GetValue(obj,new object[] {});
//							if(o==null) {
//								additionalText="null";
//							}
//							else {
//								additionalText=o.ToString();
//							}
//						}
//						else if(member is Map) {
//							imageIndex=5;
//						}
//						else {
//							if(member is FieldInfo) {
//								additionalText=((FieldInfo)member).GetValue(obj).ToString();
//							}
//							else {
//								additionalText=member.ToString();
//							}
//							imageIndex=6;
//						}
//						listBox.Items.Add(new GListBoxItem(key,additionalText,imageIndex));
//					}
//					Graphics graphics=Editor.window.CreateGraphics();
//					TreeNode selected=Editor.SelectedNode;
//					int depth=0;
//					while(selected.Parent!=null) {
//						selected=selected.Parent;
//						depth++;
//					}
//					int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
//						Convert.ToInt32(
//						graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);
//
//					int y=5;
//					TreeNode node=Editor.SelectedNode;
//					while(node!=null) {
//						if(node.Bounds.Y<0) {
//							break;
//						}	
//						y+=node.Bounds.Height;
//						node=node.PrevVisibleNode;
//
//					}
//					listBox.Location=new Point(x,y);
//					int greatest=0;
//					foreach(GListBoxItem item in listBox.Items) {
//						int width=graphics.MeasureString(item.Text,listBox.Font).ToSize().Width;
//						if(width>greatest){
//							greatest=width;
//						}								
//					}
//					listBox.Size=new Size(greatest+30,150<listBox.Items.Count*16+10? 150:listBox.Items.Count*16+10);
//					listBox.Show();
//					Editor.editor.Focus();
//				}
//				Editor.window.Activate();
//			}
//			public object Preselect(object current,ArrayList keys,bool isRightSide,bool isSelectLastKey) {
//				object selected=current;
//				int i=0;
//				if(keys.Count==2 && keys[1].Equals("Invoke")) {
//					int asdf=0;
//				}
//				if(keys[0].Equals("this")) {
//					i++;
//				}
//				else if(keys[0].Equals("caller")) {
//					int numCallers=0;
//					foreach(object key in keys) {
//						if(key.Equals("caller")) {
//							numCallers++;
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//					selected=Interpreter.callers[Interpreter.callers.Count-numCallers];
//				}
//				else if(keys[0].Equals("parent")) {
//					foreach(object key in keys) {
//						if(key.Equals("parent")) {
//							selected=((IKeyValue)selected).Parent;
//							i++;
//						}
//						else {
//							break;
//						}
//					}
//				}
//				else if(keys[0].Equals("arg")) {
//					selected=Interpreter.arguments[Interpreter.arguments.Count-1];
//					i++;
//				}				
//				else if(keys[0].Equals("search")||isRightSide) {
//					if(keys[0].Equals("search")) {
//						i++;
//					}
//					while(selected!=null && !((IKeyValue)selected).ContainsKey(keys[i])) {
//						selected=((IKeyValue)selected).Parent;
//					}
//					if(selected==null) {
//						throw new ApplicationException("Key "+keys[i]+" not found");
//					}
//				}
//				int lastKeySelect=0;
//				if(isSelectLastKey) {
//					lastKeySelect++;
//				}
//				for(;i<keys.Count-1+lastKeySelect;i++) {
//					if(keys[i].Equals("break")) {
//						Interpreter.breakMethod(selected);
//						Thread.CurrentThread.Suspend();
//						if(Interpreter.redoStatement) {
//							Interpreter.redoStatement=false;
//							throw new RestartStatementException();
//						}
//					}	
//					if(selected is IKeyValue) {
//						selected=((IKeyValue)selected)[keys[i]];
//					}
//					else {
//						if(selected==null) {
//							throw new ApplicationException("Key "+keys[i]+" does not exist");
//						}
//						selected=new NetObject(selected,selected.GetType())[keys[i]];
//					}
//
//				}
//				return selected;
//			}
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
		public delegate void BreakMethodDelegate(IKeyValue obj);
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
			public static RecognizeLiteral[] Interceptions { // only used once
				get {
					return (RecognizeLiteral[])interception.ToArray(typeof(RecognizeLiteral));
				}
			}

			public static object ConvertToMeta(object obj) { 
				if(obj==null) {
					return null;
				}
				ConvertDotNetToMeta conversion=(ConvertDotNetToMeta)metaConversion[obj.GetType()];
				if(conversion!=null) {
					return conversion.Convert(obj);
				}
				else {
					return obj;
				}
			}
			public static object ConvertToNet(object obj,Type targetType) {
				try {
					ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)((Hashtable)
						Interpreter.netConversion[obj.GetType()])[targetType];
					return conversion.Convert(obj);
				}
				catch {
					return obj;
				}
			}
			public static void AddConvertMetaToDotNet(ConvertMetaToDotNet conversion) { // maybe remove
				if(!netConversion.ContainsKey(conversion.target)) {
					netConversion[conversion.target]=new Hashtable();
				}
				((Hashtable)netConversion[conversion.target])[conversion.source]=conversion;
			}
			public static void AddConvertDotNetToMeta(ConvertDotNetToMeta conversion) { // maybe remove
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
//			[DontSerializeFieldOrProperty]
			private static ArrayList interception=new ArrayList();
//			[DontSerializeFieldOrProperty]
			public static ArrayList cashedLiterals=new ArrayList(); // maybe remove, if interceptions cannot be added
																					  // at runtime

			public static void AddInterception(RecognizeLiteral i)  { //maybe remove
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
//				foreach(MethodInfo method in typeof(Functions).GetMethods(
//					BindingFlags.Public|BindingFlags.Static)) {
//					existing[method.Name]=new NetMethod(method.Name,null,typeof(Functions));
//				}
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
					new AddIndentationTokensToStream(new MetaLexer(stream)));
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
					AddInterception((RecognizeLiteral)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(DotNetToMetaConversions).GetNestedTypes()) {
					AddConvertDotNetToMeta((ConvertDotNetToMeta)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
				}
				foreach(Type type in typeof(MetaToDotNetConversions).GetNestedTypes()) {
					AddConvertMetaToDotNet((ConvertMetaToDotNet)type.GetConstructor(new Type[]{}).Invoke(new object[]{}));
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
		}
	}
	namespace Types  {
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
				Interpreter.loadedAssemblies.Add(assembly.Location); //assemblies in library
																							  // folder still missing
				return root;
			}
			public Library() {
				libraryPath=Path.Combine(Interpreter.path,"library");

				IAssemblyEnum e=AssemblyCache.CreateGACEnum();
				IAssemblyName an; 
				AssemblyName name;
				cash=(Map)Interpreter.MergeTwo(cash,(Map)LoadAssembly(Assembly.LoadWithPartialName("mscorlib")));
				while (AssemblyCache.GetNextAssembly(e, out an) == 0) { 
					name=GetAssemblyName(an);
					Assembly assembly=Assembly.LoadWithPartialName(name.Name);
					if(name.Name.StartsWith("Microsoft.")) {
						microsoftAssemblies.Add(assembly);
					}
					else {
						cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
					}
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.dll")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					Interpreter.loadedAssemblies.Add(assembly.Location);
					cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
				}
				foreach(string fileName in Directory.GetFiles(libraryPath,"*.exe")) {
					Assembly assembly=Assembly.LoadFrom(fileName);
					Interpreter.loadedAssemblies.Add(assembly.Location);
					cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
				}
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
					if(cash.ContainsKey(key)) {
						if(key.Equals("Microsoft")) {
							if(!microsoftLoaded) {
								foreach(Assembly assembly in microsoftAssemblies) {
									cash=(Map)Interpreter.MergeTwo(cash,LoadAssembly(assembly));
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
			[DontSerializeFieldOrProperty]
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
				((Map)Interpreter.Arg).Parent=this.Parent;
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
		public delegate object NullDelegate();
		public class NetMethod: ICallable {
			private IKeyValue parent;
			[DontSerializeFieldOrProperty]
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
				if(this.name.Equals("GetKeys")) {
					int asdf=0;
				}
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
					int x=0;
					foreach(MethodBase method in methods) {
						if(x==15) {
							int asdf=0;
						}
						x++;
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
								if(parameter.ParameterType.IsAssignableFrom(list[counter].GetType())) {
//								if(list[counter].GetType().Equals(parameter.ParameterType)
//									||list[counter].GetType().IsSubclassOf(parameter.ParameterType)) {
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
										catch(Exception e){
											int asdf=0;
										}
									}
									if(!matched && parameter.ParameterType.IsArray && list[counter] is Map && ((Map)list[counter]).IntKeyValues.Count!=0) {// cheating
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
						return savedMethod.Invoke(target,new object[] {});
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
				// here bug
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
				source+="Interpreter.arguments.Add(arg);object result=callable.Call(null);Interpreter.arguments.Remove(arg);";
				if(method!=null) {
					if(!method.ReturnType.Equals(typeof(void))) {
						source+="return ("+returnTypeName+")";
						source+="Interpreter.ConvertToNet(result,typeof("+returnTypeName+"));";
					}
				}
				else {
					source+="return";
					source+=" result;";
				}
				source+="}";
				source+="private Map callable;";
				source+="public EventHandlerContainer(Map callable) {this.callable=callable;}}";
				ArrayList assemblyNames=new ArrayList(new string[] {"mscorlib.dll","System.dll","Meta.dll"});
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
			[DontSerializeFieldOrProperty]
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
		public abstract class NetContainer: IKeyValue, IEnumerable,ISerializeSpecial {
			public string Serialize() {
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
					if(key is string) { //refactor
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
										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
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
										ConvertMetaToDotNet conversion=(ConvertMetaToDotNet)toDotNet[value.GetType()];
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
			// refactor/combine with other method
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
					counter++;
				}
//				else {
					argumentList+=")";
//				}
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
		public class LiteralRecognitions {
			// order of classes is important here !
			public class RecognizeDotNetString: RecognizeLiteral  {
				public override object Recognize(string text)  {
					return text;
				}
			}
			public class RecognizeInteger: RecognizeLiteral  {
				public override object Recognize(string text)  { // doesn't handle multi-byte unicode correctly
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
						return Interpreter.String(text.Substring(1,text.Length-2));
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
//			public class RecognizeBoolean:RecognizeLiteral {
//				public override object Recognize(string text) {
//					switch(text) {
//						case "true":
//							return true;
//						case "false":
//							return false;
//					}
//					return null;
//				}
//			}
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
					return Interpreter.String((Map)obj);
				}
			}
		}
		public abstract class DotNetToMetaConversions { // these haven't all been tested
			public class ConvertStringToMap: ConvertDotNetToMeta {
				public ConvertStringToMap()   {
					this.source=typeof(string);
				}
				public override object Convert(object obj) {
					return Interpreter.String((string)obj);
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
				AddIndentationTokensForLevel(0);
			}
			public Token nextToken()  {
				if(tokenBuffer.Count==0)  {
					Token t=originalStream.nextToken();
					switch(t.Type) {
						case MetaLexerTokenTypes.EOF:
							AddIndentationTokensForLevel(-1);
							break;
						case MetaLexerTokenTypes.INDENTATION:
							AddIndentationTokensForLevel(t.getText().Length/2);
							break;
						default:
							tokenBuffer.Enqueue(t);
							break;
					}
				}
				return (Token)tokenBuffer.Dequeue();
			}	
			protected void AddIndentationTokensForLevel(int newIndentationLevel)  { // refactor
				int indentationDifference=newIndentationLevel-presentIndentationLevel; 
				if(indentationDifference==0) {
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(indentationDifference==1) {
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.INDENT));
				}
				else if(indentationDifference<0) {
					for(int i=indentationDifference;i<0;i++) {
						tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.DEDENT));
					}
					tokenBuffer.Enqueue(new Token(MetaLexerTokenTypes.ENDLINE));
				}
				else if(indentationDifference>1) {
					throw new ApplicationException("Line is indented too much.");
				}
				presentIndentationLevel=newIndentationLevel;
			}
			protected Queue tokenBuffer=new Queue();
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
					object[] attribute=testCase.GetCustomAttributes(typeof(SerializeMethodsAttribute),false);
					string[] methodNames=new string[0];
					if(attribute.Length!=0) {
						methodNames=((SerializeMethodsAttribute)attribute[0]).names;
					}
					Console.Write(testCase.Name + "...");
					DateTime timeStarted=DateTime.Now;
					string textToPrint="";
					object result=testCase.GetMethod("RunTestCase").Invoke(null,new object[]{});
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
								object val=serialize.GetType().InvokeMember(member.Name,BindingFlags.Public|BindingFlags.Instance|
									BindingFlags.GetProperty|BindingFlags.GetField|BindingFlags.InvokeMethod,null,serialize,null);
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
		internal class CompareMemberInfos:IComparer { // rename
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
