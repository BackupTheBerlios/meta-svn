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
		static Editor() {
			editor.ShowLines=false;
			editor.ShowPlusMinus=false;
			editor.Dock=DockStyle.Fill;
			editor.Font=new Font("Courier New",10.00F);
			editor.ForeColor=unselectedForecolor;
			editor.BackColor=unselectedBackcolor;
			editor.TabStop=false;

			editor.KeyDown+=new KeyEventHandler(KeyDown);
			editor.MouseDown+=new MouseEventHandler(MouseDown);
			editor.KeyPress+=new KeyPressEventHandler(KeyPress);
			editor.BeforeSelect+=new TreeViewCancelEventHandler(BeforeSelect);

			functionHelpKeyBindings[Keys.Alt|Keys.L]=typeof(PreviousOverload);
			functionHelpKeyBindings[Keys.Alt|Keys.K]=typeof(NextOverload);

			helpKeyBindings[Keys.Alt|Keys.K]=typeof(MoveKeyDown);
			helpKeyBindings[Keys.Alt|Keys.L]=typeof(MoveKeyUp);
			helpKeyBindings[Keys.Down]=typeof(MoveKeyDown);
			helpKeyBindings[Keys.Up]=typeof(MoveKeyUp);
//			helpKeyBindings[Keys.Tab]=typeof(CompleteWord);
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
		}
		public static void KeyDown(object sender,KeyEventArgs e) {
			if(Help.listBox.Visible && helpKeyBindings.ContainsKey(e.KeyData)) {
				//Help.listBox.Visible=true;
				((Command)((Type)helpKeyBindings[e.KeyData]).GetConstructor(new Type[] {})
					.Invoke(new object[]{})).Run();
			}
			else if(keyBindings.ContainsKey(e.KeyData)) {
				ConstructorInfo constructor=((Type)keyBindings[e.KeyData]).GetConstructor(new Type[]{});
				((Command)constructor.Invoke(new object[]{})).Run();
			}
		}
		public static void KeyPress(object sender,KeyPressEventArgs e) {
			if(e.KeyChar!='µ') { // (this is Alt+Ctrl+M)
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
		public string CleanText {
			get {
				if(this==Editor.SelectedNode) {
					return Text.Remove(CursorPosition,1);
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
					child.CleanText=text.TrimStart(' ');
					current.Nodes.Add(child);
					textLines.RemoveAt(0);
					Load(child,textLines,currentIndentation+"  ");
				}
				else {
					return;
				}
			}
		}
	}
	// rework
	public abstract class Help {
		//public static ToolTip tip=new ToolTip();
		public static RichTextBox toolTip=new RichTextBox();
		public static GListBox listBox=new GListBox();

		static Help() {
			listBox.Visible=false;
			listBox.Font=new Font("Automatic",8.00F);
			listBox.BorderStyle=BorderStyle.Fixed3D;
			listBox.TabStop=false;
			listBox.GotFocus+=new EventHandler(listBox_GotFocus);
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
				text=Interpreter.GetDoc(members[0],true,true,false);
				member=members[0];
			}
			else if(!(lastObject is Map)) {
				members=lastObject.GetType().GetMember(((GListBoxItem)listBox.SelectedItem).Text,
					BindingFlags.Public|BindingFlags.Instance);
				text=Interpreter.GetDoc(members[0],true,true,false);
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
					text+=Interpreter.GetDoc(((NetMethod)member).methods[0],true,true,false);
				}
				else if(member is NetClass) {
					text+=Interpreter.GetDoc(((NetClass)member).type,true,true,false);
				}
				else {
					if(! (member is Map)) {
						text+=member.GetType().FullName;
					}
				}
			}
			string newText=text.Replace(Environment.NewLine,"").//Replace("\n","").
				Replace("    "," ").Replace("   "," ").Replace("  "," ");
			newText=newText.Replace("\n ","\n");
			toolTip.BeginInvoke(new IndexChangedDelegate(IndexChangedBackThread),new object[] {newText});
		}
		private delegate void IndexChangedDelegate(string newText);
		//private static string _newText;
		private static void IndexChangedBackThread(string newText) {
			if(newText!="") {
				Size size=toolTip.CreateGraphics().MeasureString(newText,toolTip.Font).ToSize();
				toolTip.Height=size.Height+10;
				toolTip.Width=size.Width+15;
				//			toolTip.Width=newText.Length*3;

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
//		private static void listBox_SelectedIndexChanged(object sender, EventArgs e) {
//			if(listBox.SelectedItem==null) {
//				return;
//			}
//			string text="";
//			object member;
//			MemberInfo[] members=new MemberInfo[]{};
//			if(lastObject is NetClass) {
//				members=((NetClass)lastObject).type.GetMember(((GListBoxItem)listBox.SelectedItem).Text,
//					BindingFlags.Public|BindingFlags.Static);
//				text=Interpreter.GetDoc(members[0],true,true,false);
//				member=members[0];
//			}
//			else if(!(lastObject is Map)) {
//				members=lastObject.GetType().GetMember(((GListBoxItem)listBox.SelectedItem).Text,
//					BindingFlags.Public|BindingFlags.Instance);
//				text=Interpreter.GetDoc(members[0],true,true,false);
//				member=members[0];
//			}
//			else {
//				//FIX here
//				object key=((GListBoxItem)listBox.SelectedItem).Object;
//				member=((Map)lastObject)[key];
//				if(member is NetMethod) {
//					MethodBase method=((NetMethod)member).methods[0];
//					if(method is MethodInfo) {
//						text+=((MethodInfo)method).ReturnType+" ";
//					}
//					text+=((MethodBase)method).Name;
//					text+=" (";
//					foreach(ParameterInfo parameter in ((MethodBase)method).GetParameters()) {
//						text+=parameter.ParameterType+" "+parameter.Name+",";
//					}
//					if(((MethodBase)method).GetParameters().Length>0) {
//						text=text.Remove(text.Length-1,1);
//					}
//					text+=")";
//					text+=Interpreter.GetDoc(((NetMethod)member).methods[0],true,true,false);
//				}
//				else if(member is NetClass) {
//					text+=Interpreter.GetDoc(((NetClass)member).type,true,true,false);
//				}
//				else {
//					if(! (member is Map)) {
//						text+=member.GetType().FullName;
//					}
//				}
//			}
//			string newText=text.Replace(Environment.NewLine,"").//Replace("\n","").
//				Replace("    "," ").Replace("   "," ").Replace("  "," ");
//			newText=newText.Replace("\n ","\n");
//
//			if(newText!="") {
//				Size size=toolTip.CreateGraphics().MeasureString(newText,toolTip.Font).ToSize();
//				toolTip.Height=size.Height+10;
//				toolTip.Width=size.Width+15;
//				//			toolTip.Width=newText.Length*3;
//
//				if(newText!="\n") {
//					toolTip.Visible=true;
//				}
//				int x=listBox.Right+2;
//				int y=listBox.Top;
//				y+=(listBox.SelectedIndex-listBox.TopIndex)*listBox.ItemHeight;
//				toolTip.Location=new Point(x,y);
//				toolTip.Text=newText;
//			}
//			Editor.editor.Focus();
//		}
		// fix?? rethink
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
		public static void ShowHelpBackThread(Exception e) {
			if(e is BreakException) {
				if(((BreakException)e).obj==null) {
					toolTip.Visible=true;
					toolTip.Text="null";
				}
				else {
					object obj=((BreakException)e).obj;
					lastObject=obj;
					if(_isCall) {
						string text="";
						if(obj is NetMethod) {
							text=Interpreter.GetDoc(((NetMethod)obj).methods[overloadIndex],true,true,true);//(true);;
						}
						else if (obj is NetClass) {
							text=Interpreter.GetDoc(((NetClass)obj).constructor.methods[overloadIndex],true,true,true);//(true);
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
				}
			}
			else {
				toolTip.Visible=true;
				toolTip.Text=e.Message;
			}
			Editor.window.Activate();
		}
		public static void ShowHelp(Node selectedNode,string selectedNodeText,bool isCall) {
			_selectedNode=selectedNode;
			_selectedNodeText=selectedNodeText;
			_isCall=isCall;
			Thread thread=new Thread(new ThreadStart(ShowHelpOtherThread));
			//Help.listBox.Visible=true;
			thread.Start();
		}
		delegate void ShowHelpDelegate(Exception e);
		private static Node _selectedNode;
		private static string _selectedNodeText;
		private static bool _isCall;
//		private static Exception _e;
		public static void ShowHelpOtherThread() {
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
				while(!(e.InnerException==null || e is BreakException)) {
					e=e.InnerException;
				}
				//_e=e;
				listBox.BeginInvoke(new ShowHelpDelegate(ShowHelpBackThread),new object[]{e});
			}
		}


		// TODO
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
		// maybe not exact enough
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

		private static void listBox_GotFocus(object sender, EventArgs e) {
			//Editor.editor.Focus();
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
			if(Preconditions()) { //double preconditions!
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
			Type current=GetType();
			while(!current.Equals(typeof(Command))) {
				MethodInfo requirement=current.GetMethod(
					"Require",BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public);
				if(requirement!=null) {
					requirements.Add(requirement);
				}
				current=current.BaseType;
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
	public abstract class LoggedAnyNodeCommand:LoggedCommand {
		public bool Require() {
			return Editor.SelectedNode!=null;
		}
	}
	public abstract class LoggedNormalNodeCommand:LoggedAnyNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode);
		}
	}
	public class NextOverload:Command {
		public bool Require() {
			return Help.overloadIndex<Help.overloadNumber;
		}
		public override void Do() {
			Help.overloadIndex++;
		}
	}
	public class PreviousOverload:Command {
		public bool Require() {
			return Help.overloadIndex<Help.overloadNumber;
		}
		public override void Do() {
			Help.overloadIndex++;
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
	public class AbortHelp: Command {
		public override void Do() {
			Help.listBox.Visible=false;
			Help.toolTip.Visible=false;
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
					//Help.tip.SetToolTip(Editor.editor,"The file is already open.");
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


	public class MoveToPreviousNode:LoggedAnyNodeCommand {
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
	public class MoveToNextNode:LoggedAnyNodeCommand {
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
	public class MoveToNode:LoggedAnyNodeCommand {
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
	public class MoveLineUp:LoggedAnyNodeCommand {
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
	public class MoveLineDown:LoggedAnyNodeCommand {
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

	public abstract class MoveCursor:LoggedNormalNodeCommand {
		protected int oldPosition;
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=oldPosition;
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
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(start,deletedText);
			Editor.SelectedNode.CursorPosition=oldCursorPosition;
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
					InsertCharacter.lastWord=InsertCharacter.lastWord.Remove(InsertCharacter.lastWord.Length-1,1);
					Help.listBox.SelectedIndex=Help.listBox.FindString(InsertCharacter.lastWord);
				}
			}
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition-1;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
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
		}
		public override void Undo() {
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(character));
		}
	}

	public class InsertCharacter: LoggedNormalNodeCommand {
		public new bool Require() {
			return !(Editor.SelectedNode is FileNode) && !Char.IsControl(oldChar);
		}
		//rename 'oldChar'
		private char oldChar;
		public InsertCharacter(char oldChar) {
			this.oldChar=oldChar;
		}
		public static string lastWord="";
		public override void Do() {
			if(oldChar=='.') {
				Help.toolTip.Visible=false;
				lastWord="";
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText.Substring(
					0,Editor.SelectedNode.CursorPosition)+".break",false);
			}
			else if(oldChar=='(' && Help.IsFunctionCall(Editor.SelectedNode.CleanText)) {
				Help.toolTip.Visible=false;
				Help.listBox.Visible=false;
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText.Substring(
					0,Editor.SelectedNode.CursorPosition).TrimEnd('(')+".break",true);
			}
			else if(oldChar=='=') {
				Help.toolTip.Visible=false;
				Help.listBox.Visible=false;
			}
			else if(Help.listBox.Visible) {
				lastWord+=oldChar;
				Help.listBox.SelectedIndex=Help.listBox.FindString(lastWord);
			}
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(oldChar));
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition+1;
		}
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition-1;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
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
		public override void Do() {//improve here
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
	public class CreateChild:LoggedAnyNodeCommand {
		public override void Do() {
			Editor.SelectedNode.Nodes.Insert(0,new Node());
			Editor.SelectedNode=(Node)Editor.SelectedNode.FirstNode;

			if(!(Editor.SelectedNode.Parent is FileNode)&& Help.IsFunctionCall(((Node)Editor.SelectedNode.Parent).CleanText)) {
				Help.ShowHelp((Node)Editor.SelectedNode.Parent,((Node)Editor.SelectedNode.Parent).CleanText+".break",true);
			}
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
		}
		public override void Undo() {
			parentNode.Nodes.Insert(index,deletedNode);
			Editor.SelectedNode=(Node)parentNode.Nodes[index];
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