using System;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using HWND = System.IntPtr;
using System.IO;
using Meta;

public class ScrollingTextBox: RichTextBox
{
	public ScrollingTextBox() 
	{

//		Label label=new Label();
//		label.BackColor=Color.Transparent;
//		label.Text="hello hello hello";
//		Controls.Add(label);
//		label.Location=new Point(400,400);


		InitializeComponent();
		this.HorzScrollValueChanged+=new ScrollEventHandler(ScrollingTextBox_HorzScrollValueChanged);
		this.VertScrollValueChanged+=new ScrollEventHandler(ScrollingTextBox_VertScrollValueChanged);
		//Select(GetCharIndexFromPosition(new Point(this.Size.Width/2,this.Size.Height/2)),0);
		this.replace=new FindAndReplace(this);
		//		replace.Owner=this.FindForm();
//		keyBindings[Keys.Control|Keys.H]=new Function(FindAndReplace);
//		keyBindings[Keys.Control|Keys.I]=new Function(InteractiveSearch);
//		keyBindings[Keys.Alt|Keys.N]=new Function(DeleteBackward);
//		keyBindings[Keys.Alt|Keys.M]=new Function(DeleteForward);
//		keyBindings[Keys.Alt|Keys.L]=new Function(MoveLineUp);
//		keyBindings[Keys.Alt|Keys.Oemtilde]=new Function(MoveCharRight);
//		keyBindings[Keys.Alt|Keys.J]=new Function(MoveCharLeft);
//		keyBindings[Keys.Alt|Keys.Control|Keys.N]=new Function(DeleteWordLeft);
//		keyBindings[Keys.Alt|Keys.Control|Keys.M]=new Function(DeleteWordRight);
//		keyBindings[Keys.Escape]=new Function(StopInteractiveSearch);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.J]=new Function(SelectCharLeft);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.K]=new Function(SelectLineDown);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.L]=new Function(SelectLineUp);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.Oemtilde]=new Function(SelectCharRight);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.Control|Keys.J]=new Function(SelectWordLeft);
//		keyBindings[Keys.Shift|Keys.Alt|Keys.Control|Keys.Oemtilde]=new Function(SelectWordRight);
		keyBindings[Keys.Control|Keys.I]=new Function(StartInteractiveSearch);
		keyBindings[Keys.Alt|Keys.X]=new Function(Test);
		keyBindings[Keys.Escape]=new Function(StopInteractiveSearch);
		for(int i=0;i<25;i++)
		{
			emptyLines+="\n";
		}
		interactiveSearch=new InteractiveSearch((RichTextBox)this);

//		Value val=new Value("hello hello hello");
//		val.Show();

		//((Control)this).Paint+=new PaintEventHandler(ScrollingTextBox_Paint);
//		Value val=new Value("hello !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
//        this.Controls.Add(val);
//		val.Location=new Point(400,400);
//		val.SetNow();
//		base.SetStyle(ControlStyles.UserPaint,true);

		timer.Interval=50;
		timer.Tick+=new EventHandler(timer_Tick);
		timer.Start();
	}
	public void Test()
	{
		DrawValue(new Rectangle(400,400,100,100),"hello, everybody, clap your hands!!!!!!!!!!!!!!");
	}
	public void DrawValue(Rectangle rectangle,string text)
	{
		Graphics graphics=this.CreateGraphics();
		graphics.DrawString(text,this.Font,Brushes.Red,rectangle);
	}

	string emptyLines;
	protected string TopMargin
	{
		get
		{
			return emptyLines;
		}
	}
	protected string BottomMargin
	{
		get
		{
			return emptyLines;
		}
	}

	private void InitializeComponent() 
	{
		// 
		// ScrollingTextBox
		// 
		this.AcceptsTab = true;
		this.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
		this.HideSelection = false;
		this.ShowSelectionMargin = true;
		this.WordWrap = false;
		this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ScrollingTextBox_KeyDown);
		this.Resize += new System.EventHandler(this.ScrollingTextBox_Resize);
		this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ScrollingTextBox_MouseDown);
		this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ScrollingTextBox_KeyPress);
		this.TextChanged += new System.EventHandler(this.ScrollingTextBox_TextChanged);
		this.Layout += new System.Windows.Forms.LayoutEventHandler(this.ScrollingTextBox_Layout);
		this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ScrollingTextBox_MouseMove);
		this.SelectionChanged += new System.EventHandler(this.ScrollingTextBox_SelectionChanged);

	}
	int GetTabs(string line) 
	{
		return line.Length-line.TrimStart('\t').Length;
	}
	string GetLeftLine() 
	{
		int iColumn=Column;
		string sLine=GetLineText();
		string sLeft=sLine.Substring(0,iColumn);
		return sLeft;
	}
	int GetScrollColumn() 
	{ // this doesn't get the real column, but the scroll column
		int iColumn=Column;
		int iTabs=GetTabs(GetLeftLine());
		int iScrollLine=iTabs*5+iColumn;
		return iScrollLine;
	}
	// copied from: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/horizontalThread/b142ab4621009180/2d7ab486ca1f4d43?q=richtextbox+scrolling&rnum=8&hl=en#2d7ab486ca1f4d43
	const int WM_VSCROLL = 0x0115;
	const int WM_MOUSEWHEEL = 0x020a;
	readonly IntPtr SB_LINEUP = new IntPtr( 0 );
	readonly IntPtr SB_LINEDOWN = new IntPtr( 1 );
	readonly IntPtr SB_PAGEUP = new IntPtr( 2 );
	readonly IntPtr SB_PAGEDOWN = new IntPtr( 3 );
	readonly IntPtr SB_TOP = new IntPtr( 6 );
	readonly IntPtr SB_BOTTOM = new IntPtr( 7 );

	const int WM_HSCROLL = 0x0114;
	readonly IntPtr SB_LINELEFT = new IntPtr( 0 );
	readonly IntPtr SB_LINERIGHT = new IntPtr( 1 );
	readonly IntPtr SB_LEFT = new IntPtr( 6 );
	readonly IntPtr SB_RIGHT = new IntPtr( 7 );


	protected void ScrollVertical( IntPtr ScrollInstruction )  // get rid of this stuff
	{
		System.Windows.Forms.Message msg =
			Message.Create( this.Handle, WM_VSCROLL, ScrollInstruction,
			IntPtr.Zero );
		this.DefWndProc( ref msg );
	}
	protected void scrollWidthorizontal( IntPtr ScrollInstruction ) 
	{
		System.Windows.Forms.Message msg =
			Message.Create( this.Handle, WM_HSCROLL, ScrollInstruction,
			IntPtr.Zero );
		this.DefWndProc( ref msg );
	}

	public void ScrollToBottom()  // get rid of this
	{
		ScrollVertical( SB_BOTTOM );
	}
	public void ScrollToTop() 
	{
		ScrollVertical( SB_TOP );
	}
	public void ScrollLineDown() 
	{
		ScrollVertical( SB_LINEDOWN );
	}
	public void ScrollLineUp()  // remove completely
	{
		ScrollVertical( SB_LINEUP );
	}
	public void ScrollPageDown() 
	{
		ScrollVertical( SB_PAGEDOWN );
	}
	public void ScrollPageUp() 
	{
		ScrollVertical( SB_PAGEUP );
	}
	public void ScrollColumnLeft() 
	{
		scrollWidthorizontal(SB_LINELEFT);
	}
	public void ScrollColumnRight() 
	{
		scrollWidthorizontal(SB_LINERIGHT);
	}

	private void ScrollingTextBox_Resize(object sender, System.EventArgs e) 
	{
		if(SelectionStart!=-1) 
		{ // this sucks
			int selectionStart=SelectionStart;
			SelectAll();
			double width=System.Convert.ToDouble(this.Size.Width)/2.618f;
			double height=System.Convert.ToDouble(this.Size.Height)/2.618f;
			SelectionIndent=System.Convert.ToInt32(width);//(height+width)/2.0f);//+Convert.ToDouble(this.Size.Height)/1.618f)/2); // idiotic
			//			SelectionIndent=Convert.ToInt32(Convert.ToDecimal(this.Size.Height)/2.618m); // idiotic
			Select(selectionStart,0);
		}
	}
	//	bool shiftB=false;

	bool wasControlITab=false;
	private void ScrollingTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) 
	{
		if(!(e.KeyChar=='\t'&&wasControlITab)) // ignore Ctrl+I, which inserts a tab
		{
			if(interactiveSearch.Active) // make this its own class?
			{
				interactiveSearch.OnKeyPress(e.KeyChar);
				//			if(!(e.KeyChar=='\t'&&wasControlITab))
				//			{
				//				interactiveFindText+=e.KeyChar;
				//				//				InteractiveSearch();
				//				int findStart=SelectionStart-interactiveFindText.Length+1;
				//				if(Find(interactiveFindText,findStart,RichTextBoxFinds.None)==-1)
				//				{
				//					Find(interactiveFindText,0,RichTextBoxFinds.None);
				//				}
				//			}
				//			else
				//			{
				//				wasControlITab=false;
				//			}
				e.Handled=true;
			}
			else
			{
				if(e.KeyChar.Equals((char)Keys.Enter)) 
				{
					//int iTabs=GetTabs(GetLeftLine());
					string sTabs="";
					for(int i=0;i<iTabs;i++) 
					{
						sTabs+='\t';
					}
					SelectedText=sTabs;
				}
			}
		}
		else
		{
			wasControlITab=false;
			e.Handled=true;
		}
			//		if(isInteractiveFindMode) // make this its own class?
			//		{
			//			if(!(e.KeyChar=='\t'&&wasControlITab))
			//			{
			//				interactiveFindText+=e.KeyChar;
			//				//				InteractiveSearch();
			//				int findStart=SelectionStart-interactiveFindText.Length+1;
			//				if(Find(interactiveFindText,findStart,RichTextBoxFinds.None)==-1)
			//				{
			//					Find(interactiveFindText,0,RichTextBoxFinds.None);
			//				}
			//			}
			//			else
			//			{
			//				wasControlITab=false;
			//			}
			//			e.Handled=true;
			//		}

		//		if(!Char.IsControl(e.KeyChar))
		//		{
		//			string insertS=Convert.ToString(e.KeyChar);
		//			if(shiftB)
		//			{
		//				insertS=insertS.ToUpper();
		//				shiftB=false;
		//			}
		//			SelectedText=insertS;
		//			e.Handled=true;
		//		}
	}
	private void ScrollingTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) 
	{
		if(e.KeyData==(Keys.Control|Keys.I))
		{
			wasControlITab=true;
		}
		if(keyBindings.ContainsKey(e.KeyData))
		{
			((Function)keyBindings[e.KeyData])();
			//			wasControlITab=true;
			e.Handled=true;
		}
		//		else if(e.KeyData==(Keys.Control|Keys.Back))
		//		{
		//			if(Text[SelectionStart]=='\t')
		//			{
		//				while(CurrentCharacter=='\t')
		//				{
		//					DeleteBackward();
		//				}
		//			}
		//		}

//		if(e.KeyCode==Keys.Escape)
//		{
//			StopInteractiveSearch();
//		}
		if(e.KeyCode.Equals(Keys.ControlKey)) //only for tests
		{
		}

		iTabs=GetTabs(GetLeftLine());
	}
	private InteractiveSearch interactiveSearch;
//	public InteractiveSearch InteractiveSearch
//	{
//		get
//		{
//			return interactiveSearch;
//		}
//	}
	public class InteractiveSearch
	{
		public void OnKeyPress(char keyChar)
		{
//			if(!(e.KeyChar=='\t'&&wasControlITab))
//			{
				text+=keyChar;
				//				InteractiveSearch();
			Find();
//				int start=textBox.SelectionStart-text.Length+1;
//				if(textBox.Find(text,start,RichTextBoxFinds.None)==-1)
//				{
//					textBox.Find(text,0,RichTextBoxFinds.None);
//				}
//			}
//			else
//			{
////				wasControlITab=false;
//			}
		}
//		public bool OnKeyDown()
//		{
//			return false;
//		}
		private bool active=false;
		private string text="";
		private int startPosition=0;
		public bool Active
		{
			get
			{
				return active;
			}
		}
		public void Start()
		{
//			if(interactiveSearch.Active)
//			{
//
//			}
//			else
//			{
				active=true; 
				text="";
//			}
		}
		public void Find()
		{
			int start=textBox.SelectionStart+1;
			if(textBox.Find(text,start,RichTextBoxFinds.None)==-1)
			{
				textBox.Find(text,0,RichTextBoxFinds.None);
			}
		}
		public void Stop()
		{
			active=false;
//			isInteractiveFindMode=false; // make this its own class, absolutely!!
			text="";	// move all the shortcuts into Meta
		}
		private RichTextBox textBox;
		public InteractiveSearch(RichTextBox textBox)
		{
			this.textBox=textBox;
		}
        private int startLine;
		
	}
	private System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();

	private void timer_Tick(object sender, EventArgs e)
	{
		DrawInfo();
	}
	public class Info
	{
		private string text;
		private Font font=new Font("Courier New",10.0f);
		public Info(string text)
		{
			this.text=text;
		}
		public void Draw(Graphics graphics,Point position)
		{
			graphics.DrawString(text,font,Brushes.Red,position);			
		}


	}
	public Point CursorPosition
	{
		get
		{
			return GetPositionFromCharIndex(SelectionStart);
		}
	}
	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
		base.OnPaintBackground (pevent);
	}
	protected override void OnNotifyMessage(Message m)
	{
		base.OnNotifyMessage (m);
	}


	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
//		if(info!=null)
//		{
//			info.Draw(e.Graphics,CursorPosition);
//		}
	}

	Info info;//=new Info("hello!!!!!!!!!\n\n\nhello!!!!!!!!");
	public void ShowDebugValue(object debugValue)
	{
		info=new Info(Interpreter.Serialize(debugValue));// add some real serialization here!!!!!
//		int asdf=0;



//		Graphics graphics=this.CreateGraphics();
//		graphics.DrawString(debugValue.ToString(),this.Font,Brushes.Red,this.GetPositionFromCharIndex(this.SelectionStart));
////		MessageBox.Show(debugValue.ToString());
//		string x=this.Text;
//		int asdf=0;
	}
	public void StopInteractiveSearch()
	{
		interactiveSearch.Stop();
//		isInteractiveFindMode=false; // make this its own class, absolutely!!
//		interactiveFindText="";	// move all the shortcuts into Meta
	}

	static int iTabs=0;

	Hashtable keyBindings=new Hashtable();
	public delegate void Function();

	public void DeleteWordRight()
	{
		
	}
	public void DeleteWordLeft()
	{

	}
	public void MoveWordLeft()
	{
		if(!Char.IsLetter(CurrentCharacter))
		{
			MoveCharLeft();
		}
		else
		{
			while(Char.IsLetter(CurrentCharacter))
			{
				MoveCharLeft(); // überschreiben, um Abstürze zu vermeiden
			}
		}
	}
	public void MoveWordRight()
	{
		if(!Char.IsLetter(CurrentCharacter))
		{
			MoveCharRight();
		}
		else
		{
			while(Char.IsLetter(CurrentCharacter))
			{
				MoveCharRight(); // überschreiben, um Abstürze zu vermeiden
			}
		}
	}
	private int RealIndexFromIndex(int index)
	{
		return index-emptyLines.Length;
	}
	private int IndexFromRealIndex(int index)
	{
		return index+emptyLines.Length;
	}
	public void MoveEndOfDocument()
	{
		SelectionStart=IndexFromRealIndex(RealText.Length-1);
	}
	public void MoveStartOfDocument()
	{
		SelectionStart=IndexFromRealIndex(0);
	}

	public void DeleteBackward()
	{
		//		int start=SelectionStart;
		Select(SelectionStart-1,1);
		SelectedText="";
		//		SelectionStart=start;
		//		Text=Text.Remove(SelectionStart-1,1);
	}
	public void DeleteForward()
	{
		//		int start=SelectionStart; // stupid bugfix
		Select(SelectionStart,1);
		SelectedText="";
		//		SelectionStart=start;
		//		Text=Text.Remove(SelectionStart,1);
	}

	private void ScrollingTextBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) 
	{
		if(e.Delta!=0) 
		{
			int asdf=0;
		}
	}
	public void MoveLineEnd()
	{
		MoveTo(GetLinesLength(Line));
	}
	public void MoveLineStart()
	{
		MoveTo(GetLinesLength(Line));
	}

	public void SelectLineDown()
	{
	}
	public void SelectLineUp()
	{
	}
	public void SelectCharLeft()
	{
	}
	public void SelectCharRight()
	{
	}
	public void SelectWordLeft()
	{
	}
	public void MoveLineDown() 
	{
		MoveCursor(Line+1,GetScrollColumn());
	}
	public void SelectWordRight()
	{
	}

	public void MoveLineUp() 
	{
		MoveCursor(Line-1,GetScrollColumn());
	}

	public void MoveCharLeft() 
	{
		SelectionStart--;
	}
	public void MoveCharRight() 
	{
		SelectionStart++;
	}
	public int ColumnFromScrollColumn(int iLine,int iScrollColumn)  // sucks, sucks, sucks, too many invalid line and column numbers
	{
		string sLine=Lines[iLine];
		int iColumn=0;
		int iScrollCounter=0;
		foreach(char c in sLine) 
		{
			iScrollCounter++;
			if(c=='\t') 
			{
				iScrollCounter+=5;
//				iScrollCounter+=5;
			}
			iColumn++;
//			if(iScrollCounter>=iScrollColumn) 
//			{
//				break;
//			}
		}
		iColumn+=iScrollColumn-iScrollCounter;
		return iColumn;
		//		int iTabs=GetTabs(sLine);
		//		int iColumn=iScrollColumn;
		//		for(int i=0;i<iTabs&&iColumn>0;i++) {
		//			iColumn-=5;
		//		}
	}
//	public int ColumnFromScrollColumn(int iLine,int iScrollColumn)  // sucks, sucks, sucks, too many invalid line and column numbers
//	{
//		string sLine=Lines[iLine];
//		int iColumn=0;
//		int iScrollCounter=0;
//		foreach(char c in sLine) 
//		{
//			iScrollCounter++;
//			if(c=='\t') 
//			{
//				iScrollCounter+=5;
//			}
//			iColumn++;
//			if(iScrollCounter>=iScrollColumn) 
//			{
//				break;
//			}
//		}
//		return iColumn;
//		//		int iTabs=GetTabs(sLine);
//		//		int iColumn=iScrollColumn;
//		//		for(int i=0;i<iTabs&&iColumn>0;i++) {
//		//			iColumn-=5;
//		//		}
//	}
	FindAndReplace replace;
	public void FindAndReplace()
	{
		replace.Owner=FindForm();
		replace.Show();
	}
//	private bool isInteractiveFindMode=false;
//	private string interactiveFindText="";
	public void StartInteractiveSearch()
	{
		if(interactiveSearch.Active)
		{
			interactiveSearch.Find();
		}
		else
		{
			interactiveSearch.Start();
		}
	}

//	public void StartInteractiveSearch()
//	{
//		if(isInteractiveFindMode)
//		{
//			int findStart=SelectionStart+1;//-interactiveFindText.Length+1;
//			if(Find(interactiveFindText,findStart,RichTextBoxFinds.None)==-1)
//			{
//				Find(interactiveFindText,0,RichTextBoxFinds.None);
//			}
//		}
//		else
//		{
//			isInteractiveFindMode=true;
//			interactiveFindText="";
//		}
//	}
	public void MoveCursor(int line,int scrollColumn) 
	{
		if(Lines.Length!=0)
		{
			if(line<0)
			{
				line=0;
			}
			else if(line>Lines.Length-1)
			{
				line=Lines.Length-1;
			}
			int lineLength=GetLinesLength(line);
			int columns=ColumnFromScrollColumn(line,scrollColumn);
			int actualColumns=Lines[line].Length;
			if(columns>actualColumns)
			{
				string virtualSpace="";
				for(int i=0;i<columns-actualColumns;i++)
				{
                    virtualSpace+=" ";
				}
				Select(lineLength+actualColumns,0);
				SelectedText=virtualSpace;
//				string[] lines=Lines;
//				lines[line]=lines[line]+virtualSpace;
//				Lines=lines;
			}
			int newStart=lineLength+columns;
//			int newStart=GetLinesLength(line)+ColumnFromScrollColumn(line,scrollColumn);
			if(newStart>=Text.Length) 
			{
				newStart=Text.Length-1;
			}
			SelectionStart=newStart;
		}
	}
//	public void MoveCursor(int line,int scrollColumn) 
//	{
//		if(Lines.Length!=0)
//		{
//			if(line<0)
//			{
//				line=0;
//			}
//			else if(line>Lines.Length-1)
//			{
//				line=Lines.Length-1;
//			}
//			int newStart=GetLinesLength(line)+ColumnFromScrollColumn(line,scrollColumn);
//			if(newStart>=Text.Length) 
//			{
//				newStart=Text.Length-1;
//			}
//			SelectionStart=newStart;
//		}
//	}
	int GetLinesLength(int iLine) 
	{
		iLine=iLine<Lines.Length?iLine:Lines.Length-1;
		if(iLine<0) 
		{
			return 0;
		}
		int count=0;
		ArrayList lines=new ArrayList(Lines);
		foreach(string s in lines.GetRange(0,iLine)) 
		{
			count+=s.Length+1;
		}
		return count;
	}
	int LinesLength
	{
		get
		{
			return GetLinesLength(GetLineFromCharIndex(SelectionStart));
		}
	}

	// copied from: http://www.thecodeproject.com/cs/miscctrl/SyntaxHighlighting.asp
	public class Win32 
	{
		private Win32() 
		{
		}

		public const int WM_USER = 0x400;
		public const int WM_PAINT = 0xF;
		public const int WM_KEYDOWN = 0x100;
		public const int WM_KEYUP = 0x101;
		public const int WM_CHAR = 0x102;

		public const int EM_GETSCROLLPOS  =       (WM_USER + 221);
		public const int EM_SETSCROLLPOS  =       (WM_USER + 222);

		public const int VK_CONTROL = 0x11;
		public const int VK_UP = 0x26;
		public const int VK_DOWN = 0x28;
		public const int VK_NUMLOCK = 0x90;

		public const short KS_ON = 0x01;
		public const short KS_KEYDOWN = 0x80;

		[StructLayout(LayoutKind.Sequential)]
			public struct POINT 
		{
			public int x;
			public int y;
		}

		[DllImport("user32")] public static extern int SendMessage(HWND hwnd, int wMsg, int wParam, IntPtr lParam);
		[DllImport("user32")] public static extern int PostMessage(HWND hwnd, int wMsg, int wParam, int lParam);
		[DllImport("user32")] public static extern short GetKeyState(int nVirtKey);

	}
	void MoveUp() 
	{
		MoveCursor(Line-1,Column);
	}
	void MoveDown() 
	{
		MoveCursor(Line+1,Column);
	}

	string GetLineText() 
	{
		int lineL=Line;
		string lineS;
		if(lineL<Lines.Length)
		{
			lineS=Lines[lineL];
		}
		else
		{
			lineS="";
		}
		return lineS;
	}
//	int Line 
//	{
//		return GetLineFromCharIndex(SelectionStart);
//	}
	public int RealLine
	{
		get
		{
			return Line-emptyLines.Length+1; // why +1? Where is the bug?
		}
	}
	public int Line  // use Position class here, and Line class!!!!!!!! //// rename to UnrealLine , or so
	{
		get
		{
			return GetLineFromCharIndex(SelectionStart);
		}
	}
	public int Column
	{
		get
		{
			if(SelectionStart<LinesLength)
			{
				int asdf=0;
			}
			return SelectionStart-LinesLength;
		}
	}
//	int Column 
//	{
//		if(SelectionStart<LinesLength)
//		{
//			int asdf=0;
//		}
//		return SelectionStart-LinesLength;
//		
//	}
	public string RealText
	{
		get
		{
			string text="";
			foreach(string line in Lines)
			{
				text+=line.TrimEnd(' ')+Environment.NewLine;
			}
			text=text.TrimStart(null);
			text=text.TrimEnd(null);
			return text.Replace(emptyLines,"");
		}
		set
		{
			Text=emptyLines+value+emptyLines;
		}
	}
//	public override string Text
//	{
//		get
//		{
//			string text=base.Text;
//			return text.Replace(emptyLines.Replace("\r\n","\n"),"");
//		}
//		set
//		{
//			base.Text = emptyLines+value+emptyLines;
//		}
//	}


	// copied from: http://www.thecodeproject.com/cs/miscctrl/SyntaxHighlighting.asp
	private unsafe void SetScrollPos(Win32.POINT point)  // only this scrolling function is really needed
	{
		IntPtr ptr = new IntPtr(&point);
		Win32.SendMessage(Handle, Win32.EM_SETSCROLLPOS, 0, ptr);

	}

	protected void MoveTo(int position)
	{
		SelectionStart=position;
	}
//	public int Line
//	{
//		get
//		{
//			return GetLineFromCharIndex(SelectionStart);
//		}
//	}
	public char CurrentCharacter
	{
		get
		{
			return Text[SelectionStart];
		}
	}
	public class Value:Form
	{
		private Label label;
		public Value(string text)
		{
			this.label=new Label();
			label.Text=text;
//			this.Text=text;

////			SetStyle(ControlStyles.Opaque, false);
////			this.SetStyle(ControlStyles.Opaque, false);
////			this.BackColor=Color.Transparent;
////			BackColor(aTransparentColor); 
//			SetNow();
		}
//		public void SetNow()
//		{
//			this.SetStyle(ControlStyles.Opaque, false);
//			this.BackColor=Color.Transparent;
//		}
	}
	public void ShowValue()
	{
	}


	int GetMiddle() 
	{
		return GetCharIndexFromPosition(new Point(this.Size.Width/2,this.Size.Height/2));
	}
	private bool dontScroll=false;
	private void SetSelectionStartNoScroll(int selectionStart)  // what is that?
	{
		this.dontScroll=true;
		SelectionStart=selectionStart;
		this.dontScroll=false;
	}
	// implement proper tabstops sometime: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/thread/1ef28b955bd06981/27f324e4a1b722d4?q=c%23+control+i+richtextbox+tab&rnum=5&hl=en#27f324e4a1b722d4
	void ScrollToMiddle() 
	{
		if(!dontScroll) 
		{
			Win32.POINT point=new Win32.POINT();
			//		int colC=Column;
			int lineL=Line;
			string lineS=GetLineText();
			Graphics graphics=this.CreateGraphics();
			SizeF sizeF=graphics.MeasureString(GetLeftLine().Replace("\t","aaaaa").Replace(" ","a"),Font,new PointF(0.0f,0.0f),(StringFormat)StringFormat.GenericTypographic.Clone());
			point.x=(int)sizeF.Width; // this sucks, can't we scroll the rest, too
			point.y=(int)((this.Font.Height+1.0)*(float)lineL-(float)this.Size.Height/1.618f); // -1.0 is magic number, don't know why
			// check out this stuff: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/thread/1ef28b955bd06981/27f324e4a1b722d4?q=c%23+control+i+richtextbox+tab&rnum=5&hl=en#27f324e4a1b722d4
			SetScrollPos(point);
		}
//		DrawInfo();

	}
	protected void DrawInfo() // get back into the correct thread!!!!!!!!!!
	{
		if(info!=null)
		{
//			if(this.InvokeRequired)
//			{
//				((RichTextBox)this).Invoke(new MethodInvoker(DrawInfo));
//			}
//			else
//			{
				info.Draw(this.CreateGraphics(),CursorPosition);
//			}
		}
	}
	private void ScrollingTextBox_SelectionChanged(object sender, System.EventArgs e) 
	{
//		if(!dontScroll) 
//		{
			ScrollToMiddle(); // make this method more general!!!!!
//			if(info!=null)
//			{
//				info.Draw(this.CreateGraphics(),CursorPosition);
//			}

//		}
	}
	private void ScrollingTextBox_TextChanged(object sender, System.EventArgs e)
	{
		ScrollToMiddle();
	}
	// copied from http://www.thecodeproject.com/cs/miscctrl/SyntaxHighlighting.asp

	/// <summary>
	/// Horizontal scroll position has changed event
	/// </summary>
	public event ScrollEventHandler HorzScrollValueChanged;

	/// <summary>
	/// Vertical scroll position has changed event
	/// </summary>
	public event ScrollEventHandler VertScrollValueChanged;
	
	/// <summary>
	/// Intercept scroll messages to send notifications
	/// </summary>
	/// <param name="m">Message parameters</param>
	protected override void WndProc(ref Message m) 
	{
		// Let the control process the message

		if(m.Msg == WM_MOUSEWHEEL) 
		{
			if(m.WParam.ToInt32()>IntPtr.Zero.ToInt32()) 
			{
				MoveCursor(Line-3,GetScrollColumn());
			}
			else if(m.WParam.ToInt32()<IntPtr.Zero.ToInt32()) 
			{
				MoveCursor(Line+3,GetScrollColumn());
			}
			ScrollToMiddle();
			return;
		}
		// Was this a horizontal scroll message?
		if ( m.Msg == WM_HSCROLL ) 
		{
			if ( HorzScrollValueChanged != null ) 
			{
				uint wParam = (uint)m.WParam.ToInt32();
				HorzScrollValueChanged( this, 
					new ScrollEventArgs( 
					GetEventType( wParam & 0xffff), (int)(wParam >> 16) ) );
			}
		} 
			// or a vertical scroll message?
		else if ( m.Msg == WM_VSCROLL ) 
		{
			if ( VertScrollValueChanged != null ) 
			{
				uint wParam = (uint)m.WParam.ToInt32();
				VertScrollValueChanged( this, 
					new ScrollEventArgs( 
					GetEventType( wParam & 0xffff), (int)(wParam >> 16) ) );
			}
		}
		base.WndProc (ref m);
	}

	// Based on SB_* constants
	private static ScrollEventType [] _events =
		new ScrollEventType[] {
								  ScrollEventType.SmallDecrement,
								  ScrollEventType.SmallIncrement,
								  ScrollEventType.LargeDecrement,
								  ScrollEventType.LargeIncrement,
								  ScrollEventType.ThumbPosition,
								  ScrollEventType.ThumbTrack,
								  ScrollEventType.First,
								  ScrollEventType.Last,
								  ScrollEventType.EndScroll
							  };
	/// <summary>
	/// Decode the type of scroll message
	/// </summary>
	/// <param name="wParam">Lower word of scroll notification</param>
	/// <returns></returns>
	private ScrollEventType GetEventType( uint wParam ) 
	{
		if ( wParam < _events.Length )
			return _events[wParam];
		else
			return ScrollEventType.EndScroll;
	}

	protected void MoveCaretToMiddle() 
	{
		this.SetSelectionStartNoScroll(
			GetCharIndexFromPosition(new Point(this.Size.Width/2,this.Height/2)));
	}
	private void ScrollingTextBox_HorzScrollValueChanged(object sender, ScrollEventArgs e) 
	{
		this.MoveCaretToMiddle();
	}

	private void ScrollingTextBox_VertScrollValueChanged(object sender, ScrollEventArgs e) 
	{
		this.MoveCaretToMiddle();
	}
	private void ScrollingTextBox_MouseWheel(object sender, MouseEventArgs e) 
	{
	}
	protected override void OnMouseWheel(MouseEventArgs e) 
	{
	}

	private void ScrollingTextBox_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) 
	{
	}

	private void ScrollingTextBox_Paint(object sender, PaintEventArgs e)
	{
		int asdf=0;
	}

	private void ScrollingTextBox_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
	{
		int asdf=0;
	}
}
