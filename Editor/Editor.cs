// MetaEdit, an editor for the Meta programming language.
// Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using Meta.Types;
using Meta.Execution;
using System.Globalization;
using System.Threading;
using System.Xml;
using System.Runtime.InteropServices;

namespace Editor {
	public class Editor	{
		[STAThread]
		public static void Main() {
			window.Controls.Add(Help.toolTip);
			window.Controls.Add(Help.listBox);
			window.Controls.Add(editor);
			window.Size=new Size(1000,700);
			editor.Focus();
			editor.Select();
			Application.Run(window);
		}
		public static Window window=new Window();
		public static TreeView editor = new TreeView();
		public static Hashtable	keyBindings = new Hashtable();
		public static Hashtable helpKeyBindings = new Hashtable();
		public static Hashtable functionHelpKeyBindings=new Hashtable();
		public static Node clipboard = null;

		private static Node selectedNode=null;

		public static Color unselectedForecolor=Color.Black;
		public static Color unselectedBackcolor=Color.White;
		public static Color selectedForecolor=Color.White;
		public static Color selectedBackcolor=Color.Black;

		public static Node SelectedNode	{
			get {
				return selectedNode;
			}
			set {
				if(selectedNode!=null) {
					selectedNode.BackColor=unselectedBackcolor;
					selectedNode.ForeColor=unselectedForecolor;
					selectedNode.Text=selectedNode.CleanText;
					selectedNode.CheckColor();
				}
				if(value!=null) {
					value.BackColor=selectedBackcolor;
					value.ForeColor=selectedForecolor;
					value.Text=value.Text.Insert(value.CursorPosition,Node.cursorCharacter);
					value.EnsureVisible();
				}
				selectedNode=value;
			}
		}
		public static void FixListBoxPosition() {
			Graphics graphics=Editor.window.CreateGraphics();
			TreeNode selected=Editor.SelectedNode;
			if(selected==null) {
				return;
			}
			int depth=0;
			while(selected.Parent!=null) {
				selected=selected.Parent;
				depth++;
			}
			int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
				Convert.ToInt32(
				graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);

			int y=5;
			TreeNode node=Editor.SelectedNode;
			while(node!=null) {
				if(node.Bounds.Y<0) {
					break;
				}
				y+=node.Bounds.Height;
				node=node.PrevVisibleNode;

			}
			Help.listBox.Location=new Point(x,y);
		}
		static Editor() {
			Interpreter.breakMethod=new BreakMethodDelegate(Help.OnBreak);
			editor.ShowLines=false;
			editor.ShowPlusMinus=false;
			editor.Dock=DockStyle.Fill;
			editor.Font=new Font("Courier New",10.00F);
			editor.ForeColor=unselectedForecolor;
			editor.BackColor=unselectedBackcolor;
			editor.FullRowSelect=true;
			editor.TabStop=false;
			window.Closing+=new System.ComponentModel.CancelEventHandler(window_Closing);

			editor.KeyDown+=new KeyEventHandler(KeyDown);
			editor.MouseDown+=new MouseEventHandler(MouseDown);
			editor.KeyPress+=new KeyPressEventHandler(KeyPress);
			editor.BeforeSelect+=new TreeViewCancelEventHandler(BeforeSelect);
			editor.MouseWheel+=new MouseEventHandler(editor_MouseWheel);
			editor.MouseEnter+=new EventHandler(editor_MouseEnter);

			functionHelpKeyBindings[Keys.Alt|Keys.L]=typeof(PreviousOverload);
			functionHelpKeyBindings[Keys.Alt|Keys.K]=typeof(NextOverload);

			helpKeyBindings[Keys.Alt|Keys.K]=typeof(MoveKeyDown);
			helpKeyBindings[Keys.Alt|Keys.L]=typeof(MoveKeyUp);
			helpKeyBindings[Keys.Down]=typeof(MoveKeyDown);
			helpKeyBindings[Keys.Up]=typeof(MoveKeyUp);
			helpKeyBindings[Keys.Enter]=typeof(CompleteWord);

			keyBindings[Keys.Control|Keys.X]=typeof(CutNode);
			keyBindings[Keys.Control|Keys.C]=typeof(CopyNode);
			keyBindings[Keys.Control|Keys.V]=typeof(PasteNode);
			keyBindings[Keys.Control|Keys.Shift|Keys.V]=typeof(PasteNodeBackward);		
			keyBindings[Keys.Control|Keys.Z]=typeof(Undo);
			keyBindings[Keys.Control|Keys.Y]=typeof(Redo);

			keyBindings[Keys.Alt|Keys.J]=typeof(MoveCharLeft);
			keyBindings[Keys.Left]=typeof(MoveCharLeft);
			keyBindings[Keys.Alt|Keys.Oemtilde]=typeof(MoveCharRight);
			keyBindings[Keys.Right]=typeof(MoveCharRight);
			keyBindings[Keys.Up]=typeof(MoveLineUp);
			keyBindings[Keys.Alt|Keys.L]=typeof(MoveLineUp);
			keyBindings[Keys.Down]=typeof(MoveLineDown);
			keyBindings[Keys.Alt|Keys.K]=typeof(MoveLineDown);

			keyBindings[Keys.End]=typeof(MoveEndOfLine);
			keyBindings[Keys.Alt|Keys.OemSemicolon]=typeof(MoveEndOfLine);
			keyBindings[Keys.Home]=typeof(MoveStartOfLine);
			keyBindings[Keys.Alt|Keys.U]=typeof(MoveStartOfLine);

			keyBindings[Keys.Delete]=typeof(DeleteCharRight);
			keyBindings[Keys.Alt|Keys.M]=typeof(DeleteCharRight);
			keyBindings[Keys.Back]=typeof(DeleteCharLeft);
			keyBindings[Keys.Alt|Keys.N]=typeof(DeleteCharLeft);
			keyBindings[Keys.Control|Keys.Delete]=typeof(DeleteWordRight);
			keyBindings[Keys.Control|Keys.Alt|Keys.M]=typeof(DeleteWordRight);
			keyBindings[Keys.Control|Keys.Back]=typeof(DeleteWordLeft);
			keyBindings[Keys.Control|Keys.Alt|Keys.N]=typeof(DeleteWordLeft);

			keyBindings[Keys.Control|Keys.Left]=typeof(MoveWordLeft);
			keyBindings[Keys.Control|Keys.Alt|Keys.J]=typeof(MoveWordLeft);
			keyBindings[Keys.Control|Keys.Right]=typeof(MoveWordRight);
			keyBindings[Keys.Control|Keys.Alt|Keys.Oemtilde]=typeof(MoveWordRight);

			keyBindings[Keys.Control|Keys.Enter]=typeof(CreateChild);
			keyBindings[Keys.Enter]=typeof(CreateSibling);
			keyBindings[Keys.Enter|Keys.Shift]=typeof(CreateSiblingUp);

			keyBindings[Keys.F5]=typeof(Execute);
			keyBindings[Keys.Control|Keys.O]=typeof(OpenFile);

			keyBindings[Keys.Escape]=typeof(AbortHelp);

			keyBindings[Keys.Alt|Keys.H]=typeof(DeleteNode);

			keyBindings[Keys.F4]=typeof(AbortHelpThread);
		}
		public static void KeyDown(object sender,KeyEventArgs e) {
			if(Help.listBox.Visible && helpKeyBindings.ContainsKey(e.KeyData)) {
				((Command)((Type)helpKeyBindings[e.KeyData]).GetConstructor(new Type[] {})
					.Invoke(new object[]{})).Run();
			}
			else if(Help.toolTip.Visible && functionHelpKeyBindings.ContainsKey(e.KeyData)) {
				((Command)((Type)functionHelpKeyBindings[e.KeyData]).GetConstructor(new Type[] {})
					.Invoke(new object[]{})).Run();
			}
			else if(keyBindings.ContainsKey(e.KeyData)) {
				ConstructorInfo constructor=((Type)keyBindings[e.KeyData]).GetConstructor(new Type[]{});
				((Command)constructor.Invoke(new object[]{})).Run();
			}
		}
		public static void KeyPress(object sender,KeyPressEventArgs e) {
			if(e.KeyChar!='µ') { // ( 'µ' is (accidentally) created by Alt+Ctrl+M)
				InsertCharacter insertCharacter=new InsertCharacter(e.KeyChar);
				insertCharacter.Run();
			}
		}
		public static void MouseDown(object sender,MouseEventArgs e) {
			if(e.Button==MouseButtons.Left) {
				MoveToNode moveToNode=new MoveToNode(((Node)editor.GetNodeAt(e.X,e.Y)));
				moveToNode.Run();
			}
		}
		public static void BeforeSelect(object sender,TreeViewCancelEventArgs e) {
			e.Cancel=true;
		}
		private static void editor_MouseWheel(object sender, MouseEventArgs e) {
			FixListBoxPosition();
		}

		private static void editor_MouseEnter(object sender, EventArgs e) {
			FixListBoxPosition();
		}

		private static void window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if(Help.lastHelpThread!=null) {
				if(Help.lastHelpThread.ThreadState==ThreadState.Suspended) {
					Help.lastHelpThread.Resume();
				}
//				Help.lastHelpThread.Abort();
				Help.lastHelpThread=null;
			}
		}
	}
	public class Window:Form {
		public Window() {
			Controls.Add(Editor.editor);
		}
	}
	public class Node:TreeNode {
		public static string cursorCharacter="|";
		private int cursorPosition=0;
		public int CursorPosition {
			get {
				return cursorPosition;
			} 
			set {
				Text=CleanText.Insert(value,cursorCharacter);
				cursorPosition=value;
			}
		}
		public FileNode FileNode {
			get {
				TreeNode selected=this;
				while(!(selected is FileNode)) {
					selected=selected.Parent;
				}
				return (FileNode)selected;
			}
		}
		public override object Clone() {
			Node clone=(Node)base.Clone();
			clone.Text=CleanText;
			clone.cursorPosition=CursorPosition;
			return clone;
		}
		public new string Text {
			get {
				return base.Text;
			}
			set {
				base.Text=value;
			}
		}
		public string CleanText {
			get {
				if(this==Editor.SelectedNode) {
					return Text.Replace("|","");
					//return Text.Remove(CursorPosition,1); //index bug
				}
				else {
					return Text;
				}
			}
			set {
				if(this==Editor.SelectedNode) {
					Text=value.Insert(CursorPosition,cursorCharacter);
				}
				else {
					Text=value;
				}
				CheckColor();
			}
		}
		public void CheckColor() {
			if(CleanText.IndexOf("==")!=-1) {
				ForeColor=Color.Blue;
			}
		}
	}
	public class FileNode: Node {
		public FileNode(string path) {
			StreamReader streamReader=new StreamReader(path);
			ArrayList lines=new ArrayList(streamReader.ReadToEnd().Split('\n'));
			CleanText=path;
			Load(this,lines,"");
			streamReader.Close();
		}
		public void Save() {
			StreamWriter writer=new StreamWriter(CleanText);
			writer.Write(Help.SerializeTreeView(this,"",Editor.SelectedNode.CleanText,null));
			writer.Close();
		}
		private void Load(Node current,ArrayList textLines,string currentIndentation) {
			while(textLines.Count!=0) {
				string text=(string)textLines[0];
				if(text.StartsWith(currentIndentation) && text!="") {
					Node child=new Node();
					current.Nodes.Add(child);
					child.CheckColor();
					child.CleanText=text.TrimStart(' ');
					textLines.RemoveAt(0);
					Load(child,textLines,currentIndentation+"  ");
				}
				else {
					return;
				}
			}
		}
	}
	public abstract class Help {
		public static bool helpInvalidated=false;
		public static Node lastSelectedNode;
		public static RichTextBox toolTip=new RichTextBox();
		public static GListBox listBox=new GListBox();

		public static void InvalidateHelp() {
			if(lastSelectedNode!=null) {
				if(Editor.SelectedNode.Parent!=lastSelectedNode.Parent
					|| Editor.SelectedNode.Index<lastSelectedNode.Index) {
					helpInvalidated=true;
				}
			}

		}
		static Help() {
			listBox.Visible=false;
			listBox.Font=new Font("Automatic",8.00F);
			listBox.BorderStyle=BorderStyle.Fixed3D;
			listBox.TabStop=false;
			listBox.DoubleClick+=new EventHandler(listBox_DoubleClick);
			toolTip.Visible=false;
			toolTip.WordWrap=true;
			toolTip.BorderStyle=BorderStyle.FixedSingle;
			toolTip.GotFocus+=new EventHandler(toolTip_GotFocus);
			toolTip.ScrollBars=RichTextBoxScrollBars.None;
			listBox.ItemHeight=16;
			toolTip.BackColor=Color.LightYellow;
			ImageList imageList=new ImageList();
			imageList.TransparentColor = System.Drawing.Color.Lime;
			imageList.Images.Add(new Bitmap("class.bmp"));
			imageList.Images.Add(new Bitmap("event.bmp"));
			imageList.Images.Add(new Bitmap("method.bmp"));
			imageList.Images.Add(new Bitmap("namespace.bmp"));
			imageList.Images.Add(new Bitmap("property.bmp"));
			imageList.Images.Add(new Bitmap("map.bmp"));
			imageList.Images.Add(new Bitmap("object.bmp"));
			listBox.ImageList=imageList;
			listBox.SelectedIndexChanged+=new EventHandler(listBox_SelectedIndexChanged);
		}
		private static void listBox_SelectedIndexChanged(object sender, EventArgs e) {
			Thread thread=new Thread(new ThreadStart(IndexChangedOtherThread));
			thread.IsBackground=true;
			thread.Start();
		}
		private static void IndexChangedOtherThread() {
			if(listBox.SelectedItem==null) {
				return;
			}
			string text="";
			object member;
			MemberInfo[] members=new MemberInfo[]{};
			if(lastObject is NetClass) {
				members=((NetClass)lastObject).type.GetMember(((GListBoxItem)listBox.SelectedItem).Text,
					BindingFlags.Public|BindingFlags.Static);
				text=Help.GetDoc(members[0],true,true,false);
				member=members[0];
			}
			else if(!(lastObject is Map)) {
				members=lastObject.GetType().GetMember(((GListBoxItem)listBox.SelectedItem).Text,
					BindingFlags.Public|BindingFlags.Instance);
				text=Help.GetDoc(members[0],true,true,false);
				member=members[0];
			}
			else {
				//FIX here
				object key=((GListBoxItem)listBox.SelectedItem).Object;
				member=((Map)lastObject)[key];
				if(member is NetMethod) {
					MethodBase method=((NetMethod)member).methods[0];
					if(method is MethodInfo) {
						text+=((MethodInfo)method).ReturnType+" ";
					}
					text+=((MethodBase)method).Name;
					text+=" (";
					foreach(ParameterInfo parameter in ((MethodBase)method).GetParameters()) {
						text+=parameter.ParameterType+" "+parameter.Name+",";
					}
					if(((MethodBase)method).GetParameters().Length>0) {
						text=text.Remove(text.Length-1,1);
					}
					text+=")";
					text+=Help.GetDoc(((NetMethod)member).methods[0],true,true,false);
				}
				else if(member is NetClass) {
					text+=Help.GetDoc(((NetClass)member).type,true,true,false);
				}
				else {
					if(! (member is Map)) {
						text+=member.GetType().FullName;
					}
				}
			}
			string newText=text.Replace(Environment.NewLine,"").
				Replace("    "," ").Replace("   "," ").Replace("  "," ");
			newText=newText.Replace("\n ","\n");
			toolTip.BeginInvoke(new IndexChangedDelegate(IndexChangedBackThread),new object[] {newText});
		}
		private delegate void IndexChangedDelegate(string newText);
		private static void IndexChangedBackThread(string newText) {
			if(newText!="") {
				Size size=toolTip.CreateGraphics().MeasureString(newText,toolTip.Font).ToSize();
				toolTip.Height=size.Height+10;
				toolTip.Width=size.Width+15;

				if(newText!="\n") {
					toolTip.Visible=true;
				}
				int x=listBox.Right+2;
				int y=listBox.Top;
				y+=(listBox.SelectedIndex-listBox.TopIndex)*listBox.ItemHeight;
				toolTip.Location=new Point(x,y);
				toolTip.Text=newText;
			}
			Editor.editor.Focus();
		}
		private static string CompleteText(string text) {
			string completedText=text;
			Queue chars=new Queue();
			foreach(char c in text) {
				if(c=='('||c=='[') {
					chars.Enqueue(c);
				}
				else if(c==')'||c==']') {
					chars.Dequeue();
				}
			}
			foreach(char c in chars) {
				if(c=='(') {
					completedText+=')';
				}
				else if(c=='[') {
					completedText+=']';
				}
			}
			return completedText;
		}
		public static string SerializeTreeView(Node rootNode,string currentIndentation,string selectedText,Node lastNodeToBeSerialized) {
			string text="";
			foreach(Node child in rootNode.Nodes) {
				text+=currentIndentation;
				if(child==Editor.SelectedNode) {
					text+=selectedText;
				}
				else {
					text+=child.CleanText;
				}
				text+="\n";
				text+=SerializeTreeView(child,currentIndentation+"  ",selectedText,lastNodeToBeSerialized);
				if(lastNodeToBeSerialized!=null && child==lastNodeToBeSerialized) {
					break;
				}
			}
			return text;
		}
		public static object lastObject;
		public class HelpComparer:IComparer {
			public int Compare(object x, object y) {
				return x.ToString().CompareTo(y.ToString());
			}
		}
		public static int overloadNumber=0;
		public static int overloadIndex=0;
		public static string GetDoc(MemberInfo memberInfo,bool isSignature,bool isSummary,bool isParameters) {
			XmlNode comment=GetComments(memberInfo);
			string text="";
			string summary="";
			ArrayList parameters=new ArrayList();
			if(comment!=null && comment.ChildNodes!=null) {
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
			}
			if(isSignature) {
				MemberInfo[] overloaded;
				if(memberInfo.DeclaringType!=null) {
					overloaded=memberInfo.DeclaringType.GetMember(memberInfo.Name);
				}
				else {
					overloaded=new MemberInfo[] {memberInfo};
				}
				string overloadedText="";
				if(isParameters) {
					if(overloadNumber>1) {
						overloadedText=(overloadIndex+1).ToString()+" of "+overloadNumber.ToString()+"   ";
						text+=overloadedText;
					}
				}
				else if(overloaded.Length>1) {
					overloadedText=" ( +"+overloaded.Length.ToString()+" overloads)";
				}

				if(memberInfo is MethodBase) {
					if(memberInfo is MethodInfo) {
						text+=((MethodInfo)memberInfo).ReturnType+" ";
					}
					text+=((MethodBase)memberInfo).Name;
					text+=" (";
					bool firstParameter=true;
					foreach(ParameterInfo parameter in ((MethodBase)memberInfo).GetParameters()) {
						if(!firstParameter) {
							text+=" ";
						}
						string parameterName=parameter.ParameterType.ToString();
						text+=parameterName;
						text+=" "+parameter.Name+",";
						firstParameter=false;
					}
					if(((MethodBase)memberInfo).GetParameters().Length>0) {
						text=text.Remove(text.Length-1,1);
					}
					text+=")";
				}
				else if(memberInfo is PropertyInfo) {
					text+=((PropertyInfo)memberInfo).PropertyType+" "+((PropertyInfo)memberInfo).Name;
				}
				else if(memberInfo is FieldInfo) {
					text+=((FieldInfo)memberInfo).FieldType+" "+((FieldInfo)memberInfo).Name;
				}
				else if(memberInfo is Type) {
					if(((Type)memberInfo).IsInterface) {
						text+="interface ";
					}
					else {
						if(((Type)memberInfo).IsAbstract) {
							text+="abstract ";
						}
						if(((Type)memberInfo).IsValueType) {
							text+="struct ";
						}
						else {
							text+="class ";
						}
					}						 
					text+=((Type)memberInfo).Name;
				}
				else if(memberInfo is EventInfo) {
					text+=((EventInfo)memberInfo).EventHandlerType.FullName+" "+
						memberInfo.Name;
				}
				text=text.Replace("System.String","string").Replace("System.Object","object")
					.Replace("System.Boolean","bool")
					.Replace("System.Byte","byte").Replace("System.Char","char")
					.Replace("System.Decimal","decimal").Replace("System.Double","double")
					.Replace("System.Enum","enum").Replace("System.Single","float")
					.Replace("System.Int32","int").Replace("System.Int64","long")
					.Replace("System.SByte","sbyte").Replace("System.Int16","short")
					.Replace("System.UInt32","uint").Replace("System.UInt16","ushort")
					.Replace("System.UInt64","ulong").Replace("System.Void","void");
				if(!isParameters) {
					text+=overloadedText;
				}
				text+="\n";
			}
			text+=summary+"\n";
			if(isParameters) {
				foreach(XmlNode node in parameters) {
					text+=node.Attributes["name"].Value+": "+node.InnerXml+"\n";
				}
			}
			return text.Replace("<para>","").Replace("\r\n","").Replace("</para>","").Replace("<see cref=\"","")
				.Replace("\" />","").Replace("T:","").Replace("F:","").Replace("P:","")
				.Replace("M:","").Replace("E:","").Replace("     "," ").Replace("    "," ")
				.Replace("   "," ").Replace("  "," ").Replace("\n ","\n");
		}
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
		public static void OnBreak(object obj) {
			listBox.BeginInvoke(new ShowHelpDelegate(ShowHelpBackThread),new object[]{obj});
		}

		public static void ShowHelpBackThread(object obj) {
					lastObject=obj;
					if(_isCall) {
						string text="";
						if(obj is NetMethod) {
							overloadNumber=((NetMethod)obj).methods.Length;
							if(overloadIndex>=overloadNumber) {
								overloadIndex=0;
							}
							else if(overloadIndex<0) {
								overloadIndex=overloadNumber-1;
							}
							text=Help.GetDoc(((NetMethod)obj).methods[overloadIndex],true,true,true);

						}
						else if (obj is NetClass) {
							overloadNumber=((NetClass)obj).constructor.methods.Length;
							if(overloadIndex>=overloadNumber) {
								overloadIndex=0;
							}
							else if(overloadIndex<0) {
								overloadIndex=overloadNumber-1;
							}
							text=Help.GetDoc(((NetClass)obj).constructor.methods[overloadIndex],true,true,true);
						}
						else if(obj is Map) {
							text=FunctionHelp((Map)obj);
						}
						Help.toolTip.Text=text;
						Graphics graphics=toolTip.CreateGraphics();
						Size size=graphics.MeasureString(text,
							toolTip.Font).ToSize();
						toolTip.Size=new Size(size.Width+10,size.Height+13);
						toolTip.Visible=true;

						TreeNode selected=Editor.SelectedNode;
						int depth=0;
						while(selected.Parent!=null) {
							selected=selected.Parent;
							depth++;
						}
						int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
							Convert.ToInt32(
							graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);

						int y=5;
						TreeNode node=Editor.SelectedNode;
						while(node!=null) {
							y+=node.Bounds.Height;
							node=node.PrevVisibleNode;
						}
						toolTip.Location=new Point(x,y);

					}
					else {
						listBox.Items.Clear();
						IKeyValue keyValue=obj is IKeyValue? (IKeyValue)obj:new NetObject(obj);
						ArrayList keys=new ArrayList();
						foreach(DictionaryEntry entry in keyValue) {
							keys.Add(entry.Key);
						}
						keys.Sort(new HelpComparer());
						foreach(object key in keys) {
							object member;
							int imageIndex=0;
							if(lastObject is Map) {
								member=((Map)lastObject)[key];
							}
							else if(lastObject is NetClass) {
								MemberInfo[] members=((NetClass)lastObject).type.GetMember(key.ToString(),
									BindingFlags.Public|BindingFlags.Static);
								member=members[0];
							}
							else if(!(lastObject is Map)) {
								MemberInfo[] members=lastObject.GetType().GetMember(key.ToString(),
									BindingFlags.Public|BindingFlags.Instance);
								member=members.Length>0? members[0]:keyValue[key];
							}
							else {
								throw new ApplicationException("bug here");
							}
							string additionalText="";

							if(member is Type || member is NetClass) {
								imageIndex=0;
							}
							else if(member is EventInfo) {
								imageIndex=1;
							}
							else if(member is MethodInfo || member is ConstructorInfo || member is NetMethod) {
								imageIndex=2;
							}
							else if(member is PropertyInfo) {
								imageIndex=4;
								object o=((PropertyInfo)member).GetValue(obj,new object[] {});
								if(o==null) {
									additionalText="null";
								}
								else {
									additionalText=o.ToString();
								}
							}
							else if(member is Map) {
								imageIndex=5;
							}
							else {
								if(member is FieldInfo) {
									additionalText=((FieldInfo)member).GetValue(obj).ToString();
								}
								else {
									additionalText=member.ToString();
								}
								imageIndex=6;
							}
							listBox.Items.Add(new GListBoxItem(key,additionalText,imageIndex));
						}
						Graphics graphics=Editor.window.CreateGraphics();
						TreeNode selected=Editor.SelectedNode;
						int depth=0;
						while(selected.Parent!=null) {
							selected=selected.Parent;
							depth++;
						}
						int x=-5+Editor.editor.Top+Editor.editor.Indent*depth+
							Convert.ToInt32(
							graphics.MeasureString(Editor.SelectedNode.CleanText,Editor.editor.Font).Width);

						int y=5;
						TreeNode node=Editor.SelectedNode;
						while(node!=null) {
							if(node.Bounds.Y<0) {
								break;
							}	
							y+=node.Bounds.Height;
							node=node.PrevVisibleNode;

						}
						listBox.Location=new Point(x,y);
						int greatest=0;
						foreach(GListBoxItem item in listBox.Items) {
							int width=graphics.MeasureString(item.Text,listBox.Font).ToSize().Width;
							if(width>greatest){
								greatest=width;
							}								
						}
						listBox.Size=new Size(greatest+30,150<listBox.Items.Count*16+10? 150:listBox.Items.Count*16+10);
						listBox.Show();
						Editor.editor.Focus();
					}
			Editor.window.Activate();
		}
		public static Thread lastHelpThread;
		public static void ShowHelp(Node selectedNode,string selectedNodeText,bool isCall) {
			_selectedNode=selectedNode;
			_selectedNodeText=selectedNodeText;
			_isCall=isCall;
			overloadIndex=0;
			if(lastSelectedNode==null || Interpreter.lastProgram==null || helpInvalidated) {
				if(helpInvalidated) {
					int asdf=0;
				}
				helpInvalidated=false;
				if(lastHelpThread!=null) {
//					lastHelpThread.Abort();
					lastHelpThread=null;
				}
			}
			if(lastHelpThread==null) {
				lastHelpThread=new Thread(new ThreadStart(ShowHelpOtherThread));
				lastHelpThread.IsBackground=true;
				lastSelectedNode=Editor.SelectedNode;
				lastHelpThread.Start();
				return;
			}
			Interpreter.redoStatement=true;
			Map map;
			try {
				map=Interpreter.Mapify(
					new StringReader(
					SerializeTreeView(
					Editor.SelectedNode.FileNode,
					"",
					CompleteText(_selectedNodeText),
					Editor.SelectedNode
					)));
			}
			catch {
				return; // not really great
			}
			Program program=(Program)map.Compile();

			((IExpression)Interpreter.lastProgram.compiled).Replace(program);
			map.compiled=Interpreter.lastProgram.compiled;
			Interpreter.lastProgram=map;
			lastHelpThread.Resume();
		}
		delegate void ShowHelpDelegate(object obj);
		private static Node _selectedNode;
		private static string _selectedNodeText;
		private static bool _isCall;
		public static void ShowHelpOtherThread() { //still necessary?
			try {
				Interpreter.Run(
					new StringReader(
					SerializeTreeView(
					Editor.SelectedNode.FileNode,
					"",
					CompleteText(_selectedNodeText),
					Editor.SelectedNode
					)),
					new Map());
			}
			catch(Exception e) {
//				Help.lastHelpThread.Abort();
				Help.lastHelpThread=null;
			}
		}
		private static string FunctionHelp(Map map) {
			string text="";
			ArrayList args=ExtractMetaFunctionArguments((IExpression)map.Compile());
			ArrayList keys=new ArrayList();
			foreach(object key in args) {
				if(keys.IndexOf(key)==-1) {
					keys.Add(key);
				}
			}
			foreach(object obj in keys) {
				text+=obj.ToString()+"\n";
			}
			if(text.Length!=0) {
				text=text.Remove(text.Length-1,1);
			}
			return text;
		}
		public static bool IsFunctionCall(string spacedCleanText) {
			string text=spacedCleanText.Trim(' ');

			if(text.Length==0) {
				return false;
			}
			else {
				switch(text[text.Length-1]) {
					case '=':
						return false;
					case '-':
						return false;
				}
			}
			return true;
		}
		private static ArrayList ExtractMetaFunctionArguments(IExpression map)
		{
			ArrayList keys=new ArrayList();
			if(map is Program) {
				foreach(Statement statement in ((Program)map).statements) {
					keys.AddRange(ExtractMetaFunctionArguments(statement.key));
					keys.AddRange(ExtractMetaFunctionArguments(statement.val));
				}
			}
			else if(map is Call) {
				keys.AddRange(ExtractMetaFunctionArguments(((Call)map).argument));
				keys.AddRange(ExtractMetaFunctionArguments(((Call)map).callable));
			}
			else if(map is Select) {
				bool nextIsArgKey=false;
				foreach(IExpression expression in ((Select)map).expressions) {
					if(expression is Literal) {
						if(nextIsArgKey) {
							keys.Add(((Literal)expression).text);
						}
					}
					nextIsArgKey=false;
					if(expression is Literal) {
						if(((Literal)expression).text=="arg") {
							nextIsArgKey=true;
						}
					}
					else {
						keys.AddRange(ExtractMetaFunctionArguments(expression));
					}
				}
			}
			return keys;
		}
		private static void listBox_DoubleClick(object sender, EventArgs e) {
			new CompleteWord().Run();
			Editor.editor.Focus();
		}

		private static void toolTip_GotFocus(object sender, EventArgs e) {
			Editor.editor.Focus();
		}
	}







	public abstract class History {
		public static void Add(LoggedCommand command) {
			if(commands.Count>present+1) {
				commands.RemoveRange(present+1,commands.Count-(present+1));
			}
			commands.Add(command);
			present++;
		}
		public static int present=-1;
		public static ArrayList commands=new ArrayList();
	}






	public abstract class Command {
		public abstract void Do();

		public virtual void Run() {
			if(Preconditions()) {
				if(!(this is InsertCharacter || this is DeleteCharLeft)) {
					Help.listBox.Visible=false;
					Help.toolTip.Visible=false;
				}
				Do();
				if(Editor.SelectedNode!=null) {
					Editor.SelectedNode.FileNode.Save();
				}
			}
		}
		protected bool Preconditions() {
			ArrayList requirements=new ArrayList();
			Type type=GetType();
			while(!type.Equals(typeof(Command))) {
				MethodInfo requirement=type.GetMethod(
					"Require",BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public);
				if(requirement!=null) {
					requirements.Add(requirement);
				}
				type=type.BaseType;
			}
			requirements.Reverse();
			foreach(MethodInfo requirement in requirements) {
				if(!(bool)requirement.Invoke(this,null)) {
					return false;
				}
			}
			return true;
		}
	}
	public abstract class LoggedCommand:Command {
		public abstract void Undo();
		public override void Run() {
			if(Preconditions()) {
				base.Run();
				History.Add(this);
				Editor.SelectedNode.FileNode.Save();
			}
		}
	}
	public abstract class LoggedNonFileNodeCommand:LoggedCommand {
		public bool Require() {
			return Editor.SelectedNode!=null;
		}
	}
	public abstract class LoggedNormalNodeCommand:LoggedNonFileNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
	}
	public class AbortHelpThread:Command {
		public override void Do() {
			if(Help.lastHelpThread!=null) {
//				if(Help.lastHelpThread.IsAlive) {
//					Help.lastHelpThread.Resume();
//					Help.lastHelpThread.Abort();
					Help.lastHelpThread=null;
			}
		}
	}



	public class NextOverload:Command {
		public override void Do() {
			Help.overloadIndex++;
			Help.ShowHelpBackThread(Help.lastObject);
		}
	}
	public class PreviousOverload:Command {
		public override void Do() {
			Help.overloadIndex--;
			Help.ShowHelpBackThread(Help.lastObject);
		}
	}

	public class AbortHelp: Command {
		public override void Do() {
			Help.listBox.Visible=false;
			Help.toolTip.Visible=false;
		}

	}
	public class Undo:Command {
		public bool Require() {
			return History.present>-1;
		}
		public override void Do() {
			LoggedCommand command=(LoggedCommand)History.commands[History.present];
			command.Undo();
			History.present--;
		}
	}
	public class Redo:Command {
		public bool Require() {
			return History.present<History.commands.Count-1;
		}
		public override void Do() {
			LoggedCommand command=(LoggedCommand)History.commands[History.present+1];
			command.Do();
			History.present++;
		}
	}
	public class Execute: Command {
		public bool Require() {
			return Editor.SelectedNode!=null;
		}
		public override void Do() {
			try {
				string text=Help.SerializeTreeView(Editor.SelectedNode.FileNode,
					"",Editor.SelectedNode.CleanText,
					(Node)Editor.SelectedNode.FileNode.Nodes[Editor.SelectedNode.FileNode.Nodes.Count-1]);
				Interpreter.Run(new StringReader(text),new Map());
			}
			catch(Exception e) {
				string t=e.ToString();
				Size size=Help.toolTip.CreateGraphics().MeasureString(t,Help.toolTip.Font).ToSize();
				Help.toolTip.Size=new Size(size.Width,size.Height+12);
				Help.toolTip.Text=t;
				Help.toolTip.Visible=true;
			}
		}
	}

	public class OpenFile: LoggedCommand {
		static OpenFileDialog  openFileDialog=new OpenFileDialog();
		private string path;
		private FileNode fileNode;

		static OpenFile() {
			openFileDialog.Filter="All files (*.*)|*.*|Meta files (*.meta)|*.meta";
		}

		public bool Require() {
			if(path==null) {
				if(openFileDialog.ShowDialog()==DialogResult.OK) {
					path=openFileDialog.FileName;
				}
				else {
					return false;
				}
			}
			foreach(FileNode fileNode in Editor.editor.Nodes) {
				if(fileNode.CleanText==path) {
					return false;
				}
			}
			return true;
		}

		public override void Do() {
			fileNode=new FileNode(path);
			Editor.editor.Nodes.Add(fileNode);
			Editor.SelectedNode=fileNode;
			fileNode.ExpandAll();
			fileNode.EnsureVisible();
		}
		public override void Undo() {
			fileNode.Remove();
		}
	}

	public class MoveToNode:LoggedNonFileNodeCommand {
		public new bool Require() {
			return targetNode!=null;
		}
		private Node sourceNode;
		private Node targetNode;
		public MoveToNode(Node targetNode) {
			this.targetNode=targetNode;
		}
		public override void Do() {
			sourceNode=Editor.SelectedNode;
			Editor.SelectedNode=targetNode;
		}
		public override void Undo() {
			Editor.SelectedNode=sourceNode;
		}
	}
	public class MoveToPreviousNode:LoggedNonFileNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.PrevNode!=null;
		}
		public override void Do() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
		}
	}
	public class MoveToNextNode:LoggedNonFileNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.NextNode!=null;
		}
		public override void Do() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
		}
	}

	public class MoveLineUp:LoggedNonFileNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.PrevVisibleNode!=null;
		}
		public override void Do() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevVisibleNode;
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextVisibleNode;
		}
	}
	public class MoveLineDown:LoggedNonFileNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.NextVisibleNode!=null;
		}
		public override void Do() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextVisibleNode;
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevVisibleNode;
		}
	}

	public class MoveCharLeft:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=0;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			Editor.SelectedNode.CursorPosition=oldPosition-1;
		}

	}
	public class MoveCharRight:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition<Editor.SelectedNode.CleanText.Length;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			Editor.SelectedNode.CursorPosition=oldPosition+1;
		}
	}
	public abstract class MoveCursor:LoggedNormalNodeCommand {
		protected int oldPosition;
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=oldPosition;
		}
	}
	public class MoveWordRight:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=Editor.SelectedNode.CleanText.Length;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			int index=Editor.SelectedNode.CursorPosition;
			for(;index<Editor.SelectedNode.CleanText.Length;index++) {
				if(!Char.IsLetterOrDigit(Editor.SelectedNode.CleanText[index])) {
					if(index==Editor.SelectedNode.CursorPosition) {
						index++;
					}
					break;
				}
			}
			Editor.SelectedNode.CursorPosition=index;
		}
	}
	public class MoveWordLeft:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=0;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			int index=Editor.SelectedNode.CursorPosition-1;
			for(;index>=0;index--) {
				if(!Char.IsLetterOrDigit(Editor.SelectedNode.CleanText[index])) {
					if(index==Editor.SelectedNode.CursorPosition-1) {
						index--;
					}
					break;
				}
			}
			Editor.SelectedNode.CursorPosition=index+1;
		}
	}
	public class MoveStartOfLine:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=0;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			Editor.SelectedNode.CursorPosition=0;
		}
	}
	public class MoveEndOfLine:MoveCursor {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition<Editor.SelectedNode.CleanText.Length;
		}
		public override void Do() {
			oldPosition=Editor.SelectedNode.CursorPosition;
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CleanText.Length;
		}
	}
	
	public class InsertCharacter: LoggedNormalNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode) && !Char.IsControl(insertedChar);
		}
		private char insertedChar;
		public InsertCharacter(char insertedChar) {
			this.insertedChar=insertedChar;
		}
		public static string lastWord="";
		public override void Do() {
			if(insertedChar=='.') {
				Help.toolTip.Visible=false;
				lastWord="";
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText.Substring(
					0,Editor.SelectedNode.CursorPosition)+".break",false);
			}
			else if(insertedChar=='(' && Help.IsFunctionCall(Editor.SelectedNode.CleanText)) {
				Help.toolTip.Visible=false;
				Help.listBox.Visible=false;
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText.Substring(
					0,Editor.SelectedNode.CursorPosition).TrimEnd('(')+".break",true);
			}
			else if(insertedChar=='=') {
				Help.toolTip.Visible=false;
				Help.listBox.Visible=false;
			}
			else if(Help.listBox.Visible) {
				lastWord+=insertedChar;
				Help.listBox.SelectedIndex=Help.listBox.FindString(lastWord);
			}
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(insertedChar));
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition+1;
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition-1;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
		}
	}
	public class DeleteCharLeft:LoggedNormalNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition>0;
		}
		private char character;
		public override void Do() {
			character=Editor.SelectedNode.CleanText[Editor.SelectedNode.CursorPosition-1];
			if(Help.listBox.Visible) {
				if(character.Equals('.')) {
					Help.listBox.Visible=false;
					Help.toolTip.Visible=false;
				}
				else {
					// some index bug here
					if(InsertCharacter.lastWord.Length>0) {
						InsertCharacter.lastWord=InsertCharacter.lastWord.Remove(InsertCharacter.lastWord.Length-1,1);
						Help.listBox.SelectedIndex=Help.listBox.FindString(InsertCharacter.lastWord);
					}
				}
			}
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition-1;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(character));
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition+1;
		}
	}
	public class DeleteCharRight:LoggedNormalNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition<Editor.SelectedNode.CleanText.Length;
		}
		private char character;
		public override void Do() {
			character=Editor.SelectedNode.CleanText[Editor.SelectedNode.CursorPosition];
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(character));
		}
	}
	public class DeleteWordRight:LoggedNormalNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=Editor.SelectedNode.CleanText.Length;
		}
		private string deletedText;
		private int oldCursorPosition;
		public override void Do() {
			oldCursorPosition=Editor.SelectedNode.CursorPosition;
			int index=Editor.SelectedNode.CursorPosition;
			for(;index<Editor.SelectedNode.CleanText.Length;index++) {
				if(!Char.IsLetterOrDigit(Editor.SelectedNode.CleanText[index])) {
					if(index==Editor.SelectedNode.CursorPosition) {
						index++;
					}
					break;
				}
			}
			int start=Editor.SelectedNode.CursorPosition;
			int end=index-Editor.SelectedNode.CursorPosition;
			deletedText=Editor.SelectedNode.CleanText.Substring(start,end);
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(start,end);
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,deletedText);
			Editor.SelectedNode.CursorPosition=oldCursorPosition;
		}
	}
	public class DeleteWordLeft:LoggedNormalNodeCommand {
		public new bool Require() {
			return Editor.SelectedNode.CursorPosition!=0;
		}
		private string deletedText;
		private int start;
		private int oldCursorPosition;
		public override void Do() {
			oldCursorPosition=Editor.SelectedNode.CursorPosition;
			int index=Editor.SelectedNode.CursorPosition-1;
			for(;index>=0;index--) {
				if(!Char.IsLetterOrDigit(Editor.SelectedNode.CleanText[index])) {
					if(index==Editor.SelectedNode.CursorPosition-1) {
						index--;
					}
					break;
				}
			}
			index++;
			start=index;
			int end=Editor.SelectedNode.CursorPosition-index;
			deletedText=Editor.SelectedNode.CleanText.Substring(start,end);
			Editor.SelectedNode.CursorPosition=start;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(start,end);
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(start,deletedText);
			Editor.SelectedNode.CursorPosition=oldCursorPosition;
		}
	}	

	public class CreateChild:LoggedNonFileNodeCommand {
		public override void Do() {
			Editor.SelectedNode.Nodes.Insert(0,new Node());
			Editor.SelectedNode=(Node)Editor.SelectedNode.FirstNode;
			// doesn't work, parsing error:
			if(!(Editor.SelectedNode.Parent is FileNode)&& Help.IsFunctionCall(((Node)Editor.SelectedNode.Parent).CleanText)) {
				Help.ShowHelp((Node)Editor.SelectedNode.Parent,((Node)Editor.SelectedNode.Parent).CleanText+".break",true);
			}
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.Parent;
			Editor.SelectedNode.FirstNode.Remove();
		}
	}
	public class CreateSiblingUp:LoggedNormalNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
		public override void Do() {
			Editor.SelectedNode.Parent.Nodes.Insert(Editor.SelectedNode.Index,new Node());
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
			Editor.SelectedNode.PrevNode.Remove();
		}
	}
	public class CreateSibling:LoggedNormalNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
		public override void Do() {
			Editor.SelectedNode.Parent.Nodes.Insert(Editor.SelectedNode.Index+1,new Node());
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
			Editor.SelectedNode.NextNode.Remove();
		}
	}
	public class CutNode:Command {
		public override void Do() {
			(new CopyNode()).Run();
			(new DeleteNode()).Run();
			Help.InvalidateHelp();

		}
	}
	public class CopyNode:LoggedNormalNodeCommand {
		private Node selectedNode;
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
		public override void Do() {
			selectedNode=Editor.clipboard;
			Editor.clipboard=(Node)Editor.SelectedNode.Clone();
		}
		public override void Undo() {
			Editor.clipboard=selectedNode;
		}
	}
	public class PasteNodeBackward:LoggedNormalNodeCommand {
		private Node selectedNode;
		public new bool Require() {
			return Editor.clipboard!=null;
		}
		public override void Do() {
			selectedNode=(Node)Editor.clipboard.Clone();
			Editor.SelectedNode.Parent.Nodes.Insert(Editor.SelectedNode.Index,selectedNode);
			selectedNode.ExpandAll();
			Editor.SelectedNode=selectedNode;
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
			selectedNode.Remove();
		}
	}
	public class PasteNode:LoggedNormalNodeCommand {
		public new bool Require() {
			return Editor.clipboard!=null;
		}
		private Node selectedNode;
		public override void Do() {
			selectedNode=(Node)Editor.clipboard.Clone();
			Editor.SelectedNode.Parent.Nodes.Insert(Editor.SelectedNode.Index+1,selectedNode);
			selectedNode.ExpandAll();
			Editor.SelectedNode=selectedNode;
			Help.InvalidateHelp();
		}
		public override void Undo() {
			Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
			selectedNode.Remove();
		}
	}
	public class DeleteNode:LoggedNormalNodeCommand {
		private int index;
		private Node parentNode;
		private Node deletedNode;
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
		public override void Do() {
			parentNode=(Node)Editor.SelectedNode.Parent;
			index=Editor.SelectedNode.Index;
			deletedNode=Editor.SelectedNode;

			if(Editor.SelectedNode.NextNode!=null) {
				Editor.SelectedNode=(Node)Editor.SelectedNode.NextNode;
			}
			else if(Editor.SelectedNode.PrevNode!=null) {
				Editor.SelectedNode=(Node)Editor.SelectedNode.PrevNode;
			}
			else {
				Editor.SelectedNode=(Node)Editor.SelectedNode.Parent;
			}
			deletedNode.Remove();
			Help.InvalidateHelp();
		}
		public override void Undo() {
			parentNode.Nodes.Insert(index,deletedNode);
			Editor.SelectedNode=(Node)parentNode.Nodes[index];
		}
	}

	public class MoveKeyUp:LoggedNormalNodeCommand {
		public new bool Require() {
			return Help.listBox.SelectedIndex>0;
		}
		public override void Do() {
			Help.listBox.SelectedIndex--;
			Help.listBox.Visible=true;
		}
		public override void Undo() {
			Help.listBox.SelectedIndex++;
			Help.listBox.Visible=true;
		}
	}
	public class MoveKeyDown:LoggedNormalNodeCommand {
		public new bool Require() {
			return Help.listBox.SelectedIndex<Help.listBox.Items.Count-1;
		}
		public override void Do() {
			Help.listBox.SelectedIndex++;
			Help.listBox.Visible=true;
		}
		public override void Undo() {
			Help.listBox.SelectedIndex--;
			Help.listBox.Visible=true;
		}
	}
	public class CompleteWord:LoggedNormalNodeCommand {
		public override void Do() {
			oldText=Editor.SelectedNode.CleanText;
			cursorPos=Editor.SelectedNode.CursorPosition;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(
				Editor.SelectedNode.CleanText.Length-InsertCharacter.lastWord.Length,
				InsertCharacter.lastWord.Length)+Help.listBox.SelectedItem;
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CleanText.Length;
			Help.listBox.Visible=false;
			Help.toolTip.Visible=false;
		}
		string oldText="";
		int cursorPos=0;
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=cursorPos;
			Editor.SelectedNode.CleanText=oldText;
			Help.listBox.Visible=true;
		}
	}
	
	public class GListBoxItem {
		private string _myText;
		private int _myImageIndex;
		private object _myObject;
		public string Text {
			get {return _myText;}
			set {_myText = value;}
		}
		public int ImageIndex {
			get {return _myImageIndex;}
			set {_myImageIndex = value;}
		}
		public object Object {
			get {return _myObject;}
		}
		public string valueText;
		public GListBoxItem(object obj, string valueText, int index) {
			_myObject=obj;
			this.valueText=valueText;
			_myText = obj.ToString();
			_myImageIndex = index;
		}
		public GListBoxItem(string text): this(text,"",-1){}
		public GListBoxItem(): this(""){}
		public override string ToString() {
			return _myText;
		}
	}
	public class GListBox : ListBox {
		private ImageList _myImageList;
		public ImageList ImageList {
			get {
				return _myImageList;
			}
			set {
				_myImageList = value;
			}
		}
		public GListBox() {
			this.DrawMode = DrawMode.OwnerDrawFixed;
		}
		protected override void OnDrawItem(System.Windows.Forms.DrawItemEventArgs e) {
			e.DrawBackground();
			e.DrawFocusRectangle();
			GListBoxItem item;
			Rectangle bounds = e.Bounds;
			Size imageSize = _myImageList.ImageSize;
			int longest=0;
			foreach(GListBoxItem i in this.Items) {
				if(i.Text.Length>longest) {
               longest=i.Text.Length;
				}
			}
			try {
				item = (GListBoxItem) Items[e.Index];
				if (item.ImageIndex != -1) {
					_myImageList.Draw(e.Graphics, bounds.Left,bounds.Top,item.ImageIndex); 
					e.Graphics.DrawString(item.Text.PadRight(longest+1,' ')+item.valueText, e.Font, new SolidBrush(e.ForeColor), 
						bounds.Left+imageSize.Width, bounds.Top);
					Size size=e.Graphics.MeasureString(item.Text.PadRight(longest+1,' ')+item.valueText,e.Font).ToSize();
					if(size.Width+50>Size.Width) {
						Size=new Size(size.Width+50,Size.Height);
					}
				}
				else {
					e.Graphics.DrawString(item.Text, e.Font,new SolidBrush(e.ForeColor),
						bounds.Left, bounds.Top);
				}
			}
			catch {
				if (e.Index != -1) {
					e.Graphics.DrawString(Items[e.Index].ToString(),e.Font, 
						new SolidBrush(e.ForeColor) ,bounds.Left, bounds.Top);
				}
				else {
					e.Graphics.DrawString(Text,e.Font,new SolidBrush(e.ForeColor),
						bounds.Left, bounds.Top);
				}
			}
			base.OnDrawItem(e);
		}
	}
}