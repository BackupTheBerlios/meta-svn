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

namespace Editor {
	public class Editor	{
		[STAThread]
		public static void Main() {
			window.Controls.Add(editor);
			window.Size=new Size(1000,700);
			Application.Run(window);
		}
		public static Window window=new Window();
		public static TreeView editor = new TreeView();
		public static Hashtable	keyBindings = new Hashtable();
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

			editor.KeyDown+=new KeyEventHandler(KeyDown);
			editor.MouseDown+=new MouseEventHandler(MouseDown);
			editor.KeyPress+=new KeyPressEventHandler(KeyPress);
			editor.BeforeSelect+=new TreeViewCancelEventHandler(BeforeSelect);

			keyBindings[Keys.Control|Keys.X]=typeof(CutNode);
			keyBindings[Keys.Control|Keys.C]=typeof(CopyNode);
			keyBindings[Keys.Control|Keys.V]=typeof(PasteNode);
			keyBindings[Keys.Control|Keys.Shift|Keys.V]=typeof(PasteNodeBackward);		
			keyBindings[Keys.Control|Keys.Z]=typeof(Undo);
			keyBindings[Keys.Control|Keys.Y]=typeof(Redo);

			keyBindings[Keys.Alt|Keys.J]=typeof(MoveCharLeft);
			keyBindings[Keys.Alt|Keys.Oemtilde]=typeof(MoveCharRight);
			keyBindings[Keys.Alt|Keys.L]=typeof(MoveLineUp);
			keyBindings[Keys.Alt|Keys.K]=typeof(MoveLineDown);

			keyBindings[Keys.Alt|Keys.OemSemicolon]=typeof(MoveEndOfLine);
			keyBindings[Keys.Alt|Keys.U]=typeof(MoveStartOfLine);

			keyBindings[Keys.Alt|Keys.M]=typeof(DeleteCharRight);
			keyBindings[Keys.Alt|Keys.N]=typeof(DeleteCharLeft);
			keyBindings[Keys.Control|Keys.Alt|Keys.M]=typeof(DeleteWordRight);
			keyBindings[Keys.Control|Keys.Alt|Keys.N]=typeof(DeleteWordLeft);

			keyBindings[Keys.Control|Keys.Alt|Keys.J]=typeof(MoveWordLeft);
			keyBindings[Keys.Control|Keys.Alt|Keys.Oemtilde]=typeof(MoveWordRight);

			keyBindings[Keys.Control|Keys.Enter]=typeof(CreateChild);
			keyBindings[Keys.Enter]=typeof(CreateSibling);
			keyBindings[Keys.Enter|Keys.Shift]=typeof(CreateSiblingUp);

			keyBindings[Keys.F5]=typeof(Execute);
			keyBindings[Keys.Control|Keys.O]=typeof(OpenFile);

			keyBindings[Keys.Alt|Keys.H]=typeof(DeleteNode);
		}
		public static void KeyDown(object sender,KeyEventArgs e) {
			if(keyBindings.ContainsKey(e.KeyData)) {
				ConstructorInfo constructor=((Type)keyBindings[e.KeyData]).GetConstructor(new Type[]{});
				((Command)constructor.Invoke(new object[]{})).Run();
			}
		}
		public static void KeyPress(object sender,KeyPressEventArgs e) {
			if(e.KeyChar!='µ') {
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
		public static ToolTip tip=new ToolTip();

		static Help() {
			tip.AutomaticDelay=3000;
			tip.InitialDelay=0;
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
		//TODO
		private static string MapToHelp(IKeyValue obj) {
			string text="";
			if(obj.Count==0) {
				text="()";
			}
			else {
				foreach(object key in obj.Keys) {
					text+=key.ToString()+" = ";
					if(obj[key]==null) {
						text+="null";
					}
					else if(obj[key] is Map) {
						text+="...";
					}
					else {
						text+=obj[key].ToString();
					}
					text+="\n";
				}
			}
			return text.TrimEnd('\n');
		}
		// TODO
		public static void ShowHelp(Node selectedNode,string selectedNodeText,bool isCall) {
			string text="";
			try {
				Interpreter.Run(
					new StringReader(
						SerializeTreeView(
							Editor.SelectedNode.FileNode,
							"",
							CompleteText(selectedNodeText),
							Editor.SelectedNode
						)),
					new Map());
			}
			catch(Exception e) {
				while(!(e.InnerException==null || e is BreakException)) {
					e=e.InnerException;
				}
				if(e is BreakException) {
					object obj=((BreakException)e).obj;

					if(isCall) {
						if(obj is NetMethod) {
							text=((NetMethod)obj).GetDocumentation(true);;
						}
						else if (obj is NetClass) {
							text=((NetClass)obj).constructor.GetDocumentation(true);
						}
						else if(obj is Map) {
							text=FunctionHelp((Map)obj);
						}
					}
					else {
						if(obj is IKeyValue) {
							text=MapToHelp((IKeyValue)obj);
						}
						else if(obj is NetClass) {
							text=((NetClass)obj).Documentation;
						}
						else {
							text+=new NetObject(obj).GetDocumentation(false);
						}
					}
				}
				else {
					text=e.ToString();
				}
			}
			tip.SetToolTip(Editor.editor,text);
			Editor.window.Activate();
			return;
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
		// TODO
		public static bool IsFunctionCall(string spacedCleanText) {
			string text=spacedCleanText.Trim(' ');

			if(text.Length==0) {
				return false;
			}
			else {
				switch(text[text.Length-1]) {
					case '=':
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
				Do();
				Editor.SelectedNode.FileNode.Save();
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
				Help.tip.SetToolTip(Editor.editor,e.ToString());
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
					Help.tip.SetToolTip(Editor.editor,"The file is already open.");
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
			return Editor.SelectedNode.CursorPosition!=Editor.SelectedNode.CleanText.Length-1;
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
		private char oldChar;
		public InsertCharacter(char oldChar) {
			this.oldChar=oldChar;
		}
		public override void Do() {
			if(oldChar=='.') {
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText+".break",false);
			}
			else if(oldChar=='(' && Help.IsFunctionCall(Editor.SelectedNode.CleanText)) {
				Help.ShowHelp(Editor.SelectedNode,Editor.SelectedNode.CleanText.TrimEnd('(')+".break",true);
			}
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Insert(Editor.SelectedNode.CursorPosition,Convert.ToString(oldChar));
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition+1;
		}
		public override void Undo() {
			Editor.SelectedNode.CursorPosition=Editor.SelectedNode.CursorPosition-1;
			Editor.SelectedNode.CleanText=Editor.SelectedNode.CleanText.Remove(Editor.SelectedNode.CursorPosition,1);
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
		public override void Undo() 
		{
			parentNode.Nodes.Insert(index,deletedNode);
			Editor.SelectedNode=(Node)parentNode.Nodes[index];
		}
	}
}