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

namespace MetaEditor {
	public class MetaEditor	{
		[STAThread]
		public static void Main() {
			window.Controls.Add(editor);
			window.Size=new Size(1000,700);
			Help.tip.SetToolTip(editor,"Press Ctrl + O to open a file, F5 to execute.");
			Application.Run(window);
		}
		public static Window window=new Window();
		public static TreeView editor = new TreeView();
		public static Hashtable	keyBindings = new Hashtable();
		public static MetaNode clipboard = null;

		private static MetaNode selectedNode=null;

		public static MetaNode SelectedNode	{
			get {
				return selectedNode;
			}
			set {
				if(selectedNode!=null) {
					selectedNode.Unselect();
				}
				if(value!=null) {
					value.Select();
					value.EnsureVisible();
				}
				selectedNode=value;
			}
		}
		static MetaEditor() {
			editor.ShowLines=false;
			editor.ShowPlusMinus=false;
			editor.Dock=DockStyle.Fill;
			editor.Font=new Font("Courier New",10.00F);
			editor.ForeColor=MetaNode.unselectedForeColor;
			editor.BackColor=MetaNode.unselectedBackColor;

			editor.KeyDown+=new KeyEventHandler(KeyDown);
			editor.MouseDown+=new MouseEventHandler(MouseDown);
			editor.KeyPress+=new KeyPressEventHandler(KeyPress);
			editor.BeforeSelect+=new TreeViewCancelEventHandler(BeforeSelect);

			keyBindings[Keys.Control|Keys.X]=typeof(CutNode);
			keyBindings[Keys.Control|Keys.C]=typeof(CopyNode);
			keyBindings[Keys.Control|Keys.V]=typeof(PasteNode);
			keyBindings[Keys.Control|Keys.Shift|Keys.V]=typeof(PasteNodeBackward);		
			keyBindings[Keys.Control|Keys.Z]=typeof(UndoCommand);
			keyBindings[Keys.Control|Keys.Y]=typeof(RedoCommand);

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
			keyBindings[Keys.Enter]=typeof(CreateSiblingDown);
			keyBindings[Keys.Enter|Keys.Shift]=typeof(CreateSiblingUp);

			keyBindings[Keys.F5]=typeof(ExecuteProgram);
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
				MoveToNode moveToNode=new MoveToNode(((MetaNode)editor.GetNodeAt(e.X,e.Y)));
				moveToNode.Run();
			}
		}
		public static void BeforeSelect(object sender,TreeViewCancelEventArgs e) {
			e.Cancel=true;
		}
	}
	public class Window:Form {
		public Window() {
			this.Controls.Add(MetaEditor.editor);
		}
	}
	public class MetaNode:TreeNode {
		public static Color unselectedForeColor=Color.Black;
		public static Color unselectedBackColor=Color.White;
		public static Color selectedForeColor=Color.White;
		public static Color selectedBackColor=Color.Black;

		private static string cursorPositionCharacter="|";

		private int cursorPosition=0;

		public int CursorPosition {
			get {
				return this.cursorPosition;
			} 
			set {
				this.Text=this.CleanText.Insert(value,cursorPositionCharacter);
				this.cursorPosition=value;
			}
		}
		public FileNode FileNode {
			get {
				TreeNode selectedNode=this;
				while(!(selectedNode is FileNode)) {
					selectedNode=selectedNode.Parent;
				}
				return (FileNode)selectedNode;
			}
		}
		public override object Clone() {
			MetaNode clone=(MetaNode)base.Clone();
			clone.Text=this.CleanText;
			clone.cursorPosition=this.CursorPosition;
			return clone;
		}
		public void Select() {
			this.BackColor=MetaNode.selectedBackColor;
			this.ForeColor=MetaNode.selectedForeColor;
			this.Text=this.Text.Insert(this.cursorPosition,cursorPositionCharacter);
		}
		public void Unselect() {
			this.BackColor=MetaNode.unselectedBackColor;
			this.ForeColor=MetaNode.unselectedForeColor;
			this.Text=this.CleanText;
		}
		public string CleanText {
			get {
				if(this==MetaEditor.SelectedNode) {
					return this.Text.Remove(this.CursorPosition,1);
				}
				else {
					return this.Text;
				}
			}
			set {
				if(this==MetaEditor.SelectedNode) {
					this.Text=value.Insert(this.CursorPosition,cursorPositionCharacter);
				}
				else {
					this.Text=value;
				}
			}
		}
	}
	public class FileNode: MetaNode {
		public FileNode(string path) {
			StreamReader streamReader=new StreamReader(path);
			ArrayList lines=new ArrayList(streamReader.ReadToEnd().Split('\n'));
			this.CleanText=path;
			this.Load(this,lines,"");
			streamReader.Close();
		}
		public void Save() {
			StreamWriter writer=new StreamWriter(this.CleanText);
			writer.Write(Help.SerializeTree(this,"",MetaEditor.SelectedNode.CleanText,null));
			writer.Close();
		}
		private void Load(MetaNode current,ArrayList textLines,string currentIndentation) {
			while(textLines.Count!=0) {
				string text=(string)textLines[0];
				if(text.StartsWith(currentIndentation) && text!="") {
					MetaNode child=new MetaNode();
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
		public static string SerializeTree(MetaNode rootNode,string currentIndentation,string selectedText,MetaNode lastNodeToBeSerialized) {
			string text="";
			foreach(MetaNode child in rootNode.Nodes) {
				text+=currentIndentation;
				if(child==MetaEditor.SelectedNode) {
					text+=selectedText;
				}
				else {
					text+=child.CleanText;
				}
				text+="\n";
				text+=SerializeTree(child,currentIndentation+"  ",selectedText,lastNodeToBeSerialized);
				if(lastNodeToBeSerialized!=null && child==lastNodeToBeSerialized) {
					break;
				}
			}
			return text;
		}
		// here
		private static void ShowHelp(MetaNode selectedNode,bool showCallDoc,string selectedNodeCleanText,bool deep) {
			string text="<code not covered>";
			try {
				string programText=SerializeTree(
					MetaEditor.SelectedNode.FileNode,"",CompleteText(selectedNodeCleanText),MetaEditor.SelectedNode);
				Interpreter.Run(new StringReader(programText),new Map());
			}
			catch(Exception e) {
				while(e.InnerException!=null && !(e is BreakException)) {
					e=e.InnerException;
				}
				if(e is BreakException) {
					object obj=((BreakException)e).obj;

					if(showCallDoc) {
						if(obj is NetMethod) {
							text=((NetMethod)obj).GetDocumentation();
						}
						else if (obj is NetClass) {
							text=((NetClass)obj).constructor.GetDocumentation();
						}
						else if(obj is Map) {
							text=GetFunctionHelp((Map)obj);
						}
					}
					else {
						if(obj is IKeyValue) {
							text=MapToHelp((IKeyValue)obj,deep,"",false);
						}
						else if(obj is NetClass) {
							text="??";
						}
						else if (obj is NetMethod) {
							text="??";
						}
						else {
							NetObject netObject=new NetObject(obj);
							text=MapToHelp(netObject,deep,"",true);
						}
					}
				}
				else {
					text=e.ToString();
				}
			}
			tip.SetToolTip(MetaEditor.editor,text);
			MetaEditor.window.Activate();
			return;
		}
		// here
		private static string GetFunctionHelp(Map map) {
			string text="";
			ArrayList args=FindArgs((IExpression)map.Compile());
			ArrayList keys=new ArrayList();
			foreach(object key in args)
			{
				if(keys.IndexOf(key)==-1)
				{
					keys.Add(key);
				}
			}
			foreach(object obj in keys)
			{
				text+=obj.ToString()+"\n";
			}
			if(text.Length!=0)
			{
				text=text.Remove(text.Length-1,1);
			}
			return text;
		}
		private static ArrayList FindArgs(IExpression map)
		{
			ArrayList keys=new ArrayList();
			if(map is Program)
			{
				foreach(Statement statement in ((Program)map).statements)
				{
					keys.AddRange(FindArgs(statement.key));
					keys.AddRange(FindArgs(statement.val));
				}
			}
			else if(map is Call)
			{
				keys.AddRange(FindArgs(((Call)map).argument));
				keys.AddRange(FindArgs(((Call)map).callable));
			}
			else if(map is Select)
			{
				bool nextIsArgKey=false;
				foreach(IExpression expression in ((Select)map).expressions)
				{
					if(expression is Literal)
					{
						if(nextIsArgKey)
						{
							keys.Add(((Literal)expression).text);
						}
					}
					nextIsArgKey=false;
					if(expression is Literal)
					{
						if(((Literal)expression).text=="arg")
						{
							nextIsArgKey=true;
						}
					}
					else
					{
						keys.AddRange(FindArgs(expression));
					}
				}
			}
			return keys;
		}
		private static string MapToHelp(IKeyValue obj,bool deep,string indentation,bool doc)
		{
			string text="";

			if(obj.Count==0)
			{
				return "()";
			}
			ArrayList keys=obj.Keys;
//			keys.Sort();
			foreach(object key in keys)
			{
				if(doc) {
					//text+=((IDocumentable)obj[key]).Documentation;
				}
				else {
					object val=obj[key];
					if(deep && val is IKeyValue) {
						text=text+key.ToString()+" =\n"+MapToHelp((IKeyValue)val,true,indentation+"  ",doc);
					}
					else {
						string valueText;
						if(val==null) {
							valueText="null";
						}
						else if(val is Map) {
							valueText="(..)";
						}
						else {
							valueText=val.ToString();
						}

						if(valueText.Length>20) {
							valueText="...";
						}

						string keyText=key.ToString();
						if(key is Map) {
							try {
								keyText=Interpreter.String((Map)key);
							}
							catch {
								keyText=key.ToString();
							}
						}
						text=text+indentation+keyText+" = "+valueText+"\n";
					}
				}
			}
			text=text.TrimEnd('\n');
			return text;
		}
		public static bool IsFunctionCall(string spacedCleanText)
		{
			string text=spacedCleanText.Trim(' ');

			if(text.Length==0)
			{
				return false;
			}
			else
			{
				switch(text[text.Length-1])
				{
					case '=':
						return false;
				}
			}
			return true;
		}
		public static void ShowIndentedCallHelp()
		{
			if(!(MetaEditor.SelectedNode.Parent is FileNode)&& Help.IsFunctionCall(((MetaNode)MetaEditor.SelectedNode.Parent).CleanText))
			{
				ShowHelp((MetaNode)MetaEditor.SelectedNode.Parent,true,((MetaNode)MetaEditor.SelectedNode.Parent).CleanText+".break",true);
			}
		}
		public static void ShowOneMetaNodeCallHelp()
		{
			ShowHelp(MetaEditor.SelectedNode,true,MetaEditor.SelectedNode.CleanText.TrimEnd('(')+".break",true);
		}
		public static void ShowSelectHelp()
		{
			ShowHelp(MetaEditor.SelectedNode,false,MetaEditor.SelectedNode.CleanText+".break",false);
		}
	}
	public abstract class History
	{
		public static void Add(LoggedCommand command)
		{
			if(commands.Count>present+1)
			{
				commands.RemoveRange(present+1,commands.Count-(present+1));
			}
			commands.Add(command);
			present++;
			//present=commands.Count-1;
		}
		public static int present=-1;
		public static ArrayList commands=new ArrayList();
	}

	public abstract class Command
	{
		public abstract void Do();

		public virtual void Run()
		{
			if(this.CheckRequirements())
			{
				this.Do();
			}
		}
		protected bool CheckRequirements()
		{
			ArrayList requirements=new ArrayList();
			Type currentType=this.GetType();

			while(!currentType.Equals(typeof(Command)))
			{
				MethodInfo requirement=currentType.GetMethod(
					"Require",BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public);
				if(requirement!=null)
				{
					requirements.Add(requirement);
				}
				currentType=currentType.BaseType;
			}

			requirements.Reverse();

			foreach(MethodInfo requirement in requirements)
			{
				if(!(bool)requirement.Invoke(this,null))
				{
					return false;
				}
			}
			return true;

		}
	}
	public abstract class LoggedCommand:Command
	{
		public abstract void Undo();
		public override void Run()
		{
			if(this.CheckRequirements())
			{
				base.Run();
				History.Add(this);
				MetaEditor.SelectedNode.FileNode.Save();
			}
		}
	}
	public abstract class LoggedNodeCommand:LoggedCommand
	{
		public bool Require()
		{
			return MetaEditor.SelectedNode!=null;
		}
	}
	public abstract class LoggedNonFileNodeCommand:LoggedNodeCommand
	{
		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode);
		}
	}

	public class UndoCommand:Command
	{
		public bool Require()
		{
			return History.present>-1;
		}
		public override void Do()
		{
			LoggedCommand command=(LoggedCommand)History.commands[History.present];
			command.Undo();
			History.present--;
		}
	}
	public class RedoCommand:Command
	{
		public bool Require()
		{
			return History.present<History.commands.Count-1;
		}
		public override void Do()
		{
			LoggedCommand command=(LoggedCommand)History.commands[History.present+1];
			command.Do();
			History.present++;
		}
	}


	public class ExecuteProgram: Command
	{
		public bool Require()
		{
			return MetaEditor.SelectedNode!=null;
		}
		public override void Do()
		{
			try
			{
				string programText=Help.SerializeTree(MetaEditor.SelectedNode.FileNode,"",MetaEditor.SelectedNode.CleanText,
					(MetaNode)MetaEditor.SelectedNode.FileNode.Nodes[MetaEditor.SelectedNode.FileNode.Nodes.Count-1]);
				Interpreter.Run(new StringReader(programText),new Map());
			}
			catch(Exception e)
			{
				Help.tip.SetToolTip(MetaEditor.editor,e.ToString());
			}
		}
	}
	public class OpenFile: LoggedCommand
	{
		static OpenFileDialog  openFileDialog=new OpenFileDialog();
		private string path;
		private FileNode fileNode;

		static OpenFile()
		{
			openFileDialog.Filter="All files (*.*)|*.*|Meta files (*.meta)|*.meta";
		}

		public bool Require()
		{
			if(this.path==null)
			{
				if(openFileDialog.ShowDialog()==DialogResult.OK)
				{
					this.path=openFileDialog.FileName;
				}
				else
				{
					return false;
				}
			}
			foreach(FileNode fileNode in MetaEditor.editor.Nodes)
			{
				if(fileNode.CleanText==this.path)
				{
					return false;
				}
			}
			return true;
		}

		public override void Do()
		{
			this.fileNode=new FileNode(this.path);
			MetaEditor.editor.Nodes.Add(this.fileNode);
			MetaEditor.SelectedNode=this.fileNode;
			this.fileNode.ExpandAll();
			this.fileNode.EnsureVisible();
		}
		public override void Undo()
		{
			this.fileNode.Remove();
		}
	}

	public class CreateChild:LoggedNodeCommand
	{
		public override void Do()
		{
			MetaEditor.SelectedNode.Nodes.Insert(0,new MetaNode());
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.FirstNode;
//			if(!(MetaEditor.SelectedNode is FileNode)&& Help.IsFunctionCall(MetaEditor.SelectedNode.CleanText))
//			{
				Help.ShowIndentedCallHelp();
//			}
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.Parent;
			MetaEditor.SelectedNode.FirstNode.Remove();
		}
	}
	public class MoveToPreviousNode:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.PrevNode!=null;
		}
		public override void Do()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
		}
	}
	public class MoveToNextNode:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.NextNode!=null;
		}
		public override void Do()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
		}
	}
	public class MoveToParentNode:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.Parent!=null;
		}
		private MetaNode childNode;
		public override void Do()
		{
			this.childNode=MetaEditor.SelectedNode;
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.Parent;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=this.childNode;
		}
	}
	public class MoveToFirstChildNode:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.FirstNode!=null;
		}
		public override void Do()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.FirstNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.Parent;
		}
	}
	public class MoveToNode:LoggedNodeCommand
	{
		public new bool Require()
		{
			return this.targetNode!=null;
		}
		private MetaNode sourceNode;
		private MetaNode targetNode;
		public MoveToNode(MetaNode targetNode)
		{
			this.targetNode=targetNode;
		}
		public override void Do()
		{
			this.sourceNode=MetaEditor.SelectedNode;
			MetaEditor.SelectedNode=this.targetNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=this.sourceNode;
		}
	}
	public class MoveLineUp:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.PrevVisibleNode!=null;
		}
		public override void Do()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevVisibleNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextVisibleNode;
		}
	}
	public class MoveLineDown:LoggedNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.NextVisibleNode!=null;
		}
		public override void Do()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextVisibleNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevVisibleNode;
		}
	}

	public abstract class MoveCursor:LoggedNonFileNodeCommand
	{
		protected int oldPosition;
		public override void Undo()
		{
			MetaEditor.SelectedNode.CursorPosition=this.oldPosition;
		}
	}
	public class MoveStartOfLine:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=0;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			MetaEditor.SelectedNode.CursorPosition=0;
		}
	}
	public class MoveEndOfLine:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=MetaEditor.SelectedNode.CleanText.Length-1;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			MetaEditor.SelectedNode.CursorPosition=MetaEditor.SelectedNode.CleanText.Length;
		}
	}
	public class MoveWordRight:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=MetaEditor.SelectedNode.CleanText.Length;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			int index=MetaEditor.SelectedNode.CursorPosition;
			for(;index<MetaEditor.SelectedNode.CleanText.Length;index++)
			{
				if(!Char.IsLetterOrDigit(MetaEditor.SelectedNode.CleanText[index]))
				{
					if(index==MetaEditor.SelectedNode.CursorPosition)
					{
						index++;
					}
					break;
				}
			}
			MetaEditor.SelectedNode.CursorPosition=index;
		}
	}
	public class MoveWordLeft:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=0;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			int index=MetaEditor.SelectedNode.CursorPosition-1;
			for(;index>=0;index--)
			{
				if(!Char.IsLetterOrDigit(MetaEditor.SelectedNode.CleanText[index]))
				{
					if(index==MetaEditor.SelectedNode.CursorPosition-1)
					{
						index--;
					}
					break;
				}
			}
			MetaEditor.SelectedNode.CursorPosition=index+1;
		}
	}
	public class MoveCharLeft:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=0;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			MetaEditor.SelectedNode.CursorPosition=this.oldPosition-1;
		}

	}
	public class MoveCharRight:MoveCursor
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition<MetaEditor.SelectedNode.CleanText.Length;
		}
		public override void Do()
		{
			this.oldPosition=MetaEditor.SelectedNode.CursorPosition;
			MetaEditor.SelectedNode.CursorPosition=this.oldPosition+1;
		}
	}

	public class DeleteWordRight:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=MetaEditor.SelectedNode.CleanText.Length-1;
		}
		private string deletedText;
		public override void Do()
		{
			int index=MetaEditor.SelectedNode.CursorPosition;
			for(;index<MetaEditor.SelectedNode.CleanText.Length;index++)
			{
				if(!Char.IsLetterOrDigit(MetaEditor.SelectedNode.CleanText[index]))
				{
					if(index==MetaEditor.SelectedNode.CursorPosition)
					{
						index++;
					}
					break;
				}
			}
			int start=MetaEditor.SelectedNode.CursorPosition;
			int end=index-MetaEditor.SelectedNode.CursorPosition;
			this.deletedText=MetaEditor.SelectedNode.CleanText.Substring(start,end);
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Remove(start,end);
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Insert(MetaEditor.SelectedNode.CursorPosition,this.deletedText);
		}
	}
	
	public class DeleteWordLeft:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition!=0;
		}
		private string deletedText;
		private int start;
		public override void Do()
		{
			int index=MetaEditor.SelectedNode.CursorPosition-1;
			for(;index>=0;index--)
			{
				if(!Char.IsLetterOrDigit(MetaEditor.SelectedNode.CleanText[index]))
				{
					if(index==MetaEditor.SelectedNode.CursorPosition-1)
					{
						index--;
					}
					break;
				}
			}
			index++;
			this.start=index;
			int end=MetaEditor.SelectedNode.CursorPosition-index;
			this.deletedText=MetaEditor.SelectedNode.CleanText.Substring(start,end);
			MetaEditor.SelectedNode.CursorPosition=this.start;
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Remove(start,end);
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Insert(start,this.deletedText);
		}
	}
	public class DeleteCharLeft:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition>0;
		}
		private char character;
		public override void Do()
		{
			this.character=MetaEditor.SelectedNode.CleanText[MetaEditor.SelectedNode.CursorPosition-1];
			MetaEditor.SelectedNode.CursorPosition=MetaEditor.SelectedNode.CursorPosition-1;
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Remove(MetaEditor.SelectedNode.CursorPosition,1);
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Insert(MetaEditor.SelectedNode.CursorPosition,Convert.ToString(this.character));
			MetaEditor.SelectedNode.CursorPosition=MetaEditor.SelectedNode.CursorPosition+1;
		}
	}
	public class DeleteCharRight:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.SelectedNode.CursorPosition<MetaEditor.SelectedNode.CleanText.Length;
		}
		private char character;
		public override void Do()
		{
			this.character=MetaEditor.SelectedNode.CleanText[MetaEditor.SelectedNode.CursorPosition];
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Remove(MetaEditor.SelectedNode.CursorPosition,1);
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Insert(MetaEditor.SelectedNode.CursorPosition,Convert.ToString(this.character));
		}
	}

	public class InsertCharacter: LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode) && !Char.IsControl(this.oldChar);
		}
		private char oldChar;
		public InsertCharacter(char oldChar)
		{
			this.oldChar=oldChar;
		}
		public override void Do()
		{
			if(this.oldChar=='.')
			{
				Help.ShowSelectHelp();
			}
			else if(this.oldChar=='(' && Help.IsFunctionCall(MetaEditor.SelectedNode.CleanText))
			{
				Help.ShowOneMetaNodeCallHelp();
			}
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Insert(MetaEditor.SelectedNode.CursorPosition,Convert.ToString(this.oldChar));
			MetaEditor.SelectedNode.CursorPosition=MetaEditor.SelectedNode.CursorPosition+1;

//			MetaEditor.SelectedNode.FileNode.Save();
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.CursorPosition=MetaEditor.SelectedNode.CursorPosition-1;
			MetaEditor.SelectedNode.CleanText=MetaEditor.SelectedNode.CleanText.Remove(MetaEditor.SelectedNode.CursorPosition,1);
		}
	}

	public class CutNode:Command
	{
		public override void Do()
		{
			(new CopyNode()).Run();
			(new DeleteNode()).Run();
		}
	}
	public class CopyNode:LoggedNonFileNodeCommand
	{
		private MetaNode selectedNode;
		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode);
		}
		public override void Do()
		{
			this.selectedNode=MetaEditor.clipboard;
			MetaEditor.clipboard=(MetaNode)MetaEditor.SelectedNode.Clone();
		}
		public override void Undo()
		{
			MetaEditor.clipboard=this.selectedNode;
		}
	}
	public class PasteNodeBackward:LoggedNonFileNodeCommand
	{
		private MetaNode selectedNode;
		public new bool Require()
		{
			return MetaEditor.clipboard!=null;
		}
		public override void Do()
		{
			this.selectedNode=(MetaNode)MetaEditor.clipboard.Clone();
			MetaEditor.SelectedNode.Parent.Nodes.Insert(MetaEditor.SelectedNode.Index,this.selectedNode);
			this.selectedNode.ExpandAll();
			MetaEditor.SelectedNode=this.selectedNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
			this.selectedNode.Remove();
		}
	}
	public class PasteNode:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return MetaEditor.clipboard!=null;
		}
		private MetaNode selectedNode;
		public override void Do()
		{
			this.selectedNode=(MetaNode)MetaEditor.clipboard.Clone();
			MetaEditor.SelectedNode.Parent.Nodes.Insert(MetaEditor.SelectedNode.Index+1,this.selectedNode);
			this.selectedNode.ExpandAll();
			MetaEditor.SelectedNode=this.selectedNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
			this.selectedNode.Remove();
		}
	}

	public class CollapseNode:LoggedNonFileNodeCommand
	{
		public override void Do()
		{
			MetaEditor.SelectedNode.Collapse();
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.ExpandAll();
		}
	}
	public class ExpandNode:LoggedNonFileNodeCommand
	{
		public override void Do()
		{
			MetaEditor.SelectedNode.ExpandAll();
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode.Collapse();
		}
	}

	public class DeleteNode:LoggedNonFileNodeCommand
	{
		private int index;
		private MetaNode parentNode;
		private MetaNode deletedNode;

		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode);
		}
		public override void Do()
		{
			this.parentNode=(MetaNode)MetaEditor.SelectedNode.Parent;
			this.index=MetaEditor.SelectedNode.Index;
			this.deletedNode=MetaEditor.SelectedNode;

			if(MetaEditor.SelectedNode.NextNode!=null)
			{
				MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
			}
			else if(MetaEditor.SelectedNode.PrevNode!=null)
			{
				MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
			}
			else
			{
				MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.Parent;
			}
			this.deletedNode.Remove();
		}
		public override void Undo()
		{
			this.parentNode.Nodes.Insert(this.index,this.deletedNode);
			MetaEditor.SelectedNode=(MetaNode)this.parentNode.Nodes[this.index];
		}
	}
	public class CreateSiblingUp:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode);
		}
		public override void Do()
		{

			Help.ShowIndentedCallHelp();

			MetaEditor.SelectedNode.Parent.Nodes.Insert(MetaEditor.SelectedNode.Index,new MetaNode());
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
			MetaEditor.SelectedNode.PrevNode.Remove();
		}
	}
	public class CreateSiblingDown:LoggedNonFileNodeCommand
	{
		public new bool Require()
		{
			return !(MetaEditor.SelectedNode is FileNode);
		}
		public override void Do()
		{
//			if(!(MetaEditor.SelectedNode is FileNode)&& Help.IsFunctionCall(MetaEditor.SelectedNode.CleanText))
//			{
				Help.ShowIndentedCallHelp();
//			}
			MetaEditor.SelectedNode.Parent.Nodes.Insert(MetaEditor.SelectedNode.Index+1,new MetaNode());
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.NextNode;
		}
		public override void Undo()
		{
			MetaEditor.SelectedNode=(MetaNode)MetaEditor.SelectedNode.PrevNode;
			MetaEditor.SelectedNode.NextNode.Remove();
		}
	}
}