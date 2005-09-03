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
		InitializeComponent();
		this.replace=new FindAndReplace(this);
		for(int i=0;i<25;i++)
		{
			emptyLines+="\n";
		}
		interactiveSearch=new InteractiveSearch((RichTextBox)this);
		timer.Interval=50;
		timer.Tick+=new EventHandler(timer_Tick);
		timer.Start();
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
		this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ScrollingTextBox_KeyPress);
		this.TextChanged += new System.EventHandler(this.ScrollingTextBox_TextChanged);
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
	{ 
		int iColumn=Column;
		int iTabs=GetTabs(GetLeftLine());
		int iScrollLine=iTabs*5+iColumn;
		return iScrollLine;
	}
	// copied from: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/horizontalThread/b142ab4621009180/2d7ab486ca1f4d43?q=richtextbox+scrolling&rnum=8&hl=en#2d7ab486ca1f4d43
	const int WM_VSCROLL = 0x0115;
	const int WM_MOUSEWHEEL = 0x020a;

	const int WM_HSCROLL = 0x0114;

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



	private void ScrollingTextBox_Resize(object sender, System.EventArgs e) 
	{
		if(SelectionStart!=-1) 
		{ 
			// TODO: refactor
			int selectionStart=SelectionStart;
			SelectAll();
			double width=Convert.ToDouble(this.Size.Width)/2.618f;
			double height=Convert.ToDouble(this.Size.Height)/2.618f;
			SelectionIndent=Convert.ToInt32(width);
			Select(selectionStart,0);
		}
	}
	bool wasControlITab=false;
	private void ScrollingTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) 
	{
		if(!(e.KeyChar=='\t'&&wasControlITab)) // ignore Ctrl+I, which inserts a tab
		{
			if(interactiveSearch.Active)
			{
				interactiveSearch.OnKeyPress(e.KeyChar);
				e.Handled=true;
			}
			else
			{
				if(e.KeyChar.Equals((char)Keys.Enter)) 
				{
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
			e.Handled=true;
		}
		iTabs=GetTabs(GetLeftLine());
	}
	private InteractiveSearch interactiveSearch;
	public class InteractiveSearch
	{
		public void OnKeyPress(char keyChar)
		{
			text+=keyChar;
			Find();
		}
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
			active=true; 
			text="";
		}
		public void Find()
		{
			int start=textBox.SelectionStart;
			if(textBox.Find(text,start,RichTextBoxFinds.None)==-1)
			{
				textBox.Find(text,0,RichTextBoxFinds.None);
			}
		}
		public void Stop()
		{
			active=false;
			text="";
		}
		private RichTextBox textBox;
		public InteractiveSearch(RichTextBox textBox)
		{
			this.textBox=textBox;
		}
//        private int startLine;
		
	}
	private System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();

	private void timer_Tick(object sender, EventArgs e)
	{
		DrawInfo();
	}
	protected void DrawInfo() // get back into the correct thread!!!!!!!!!!
	{
		if(info!=null)
		{
			info.Draw(this.CreateGraphics(),CursorPosition);
		}
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
	Info info;//=new Info("hello!!!!!!!!!\n\n\nhello!!!!!!!!");
	public void ShowDebugValue(object debugValue)
	{
		Graphics graphics=this.CreateGraphics();
		MessageBox.Show(Serialize.Value((Map)debugValue));
		graphics.DrawString(Serialize.Value((Map)debugValue),this.Font,Brushes.Red,this.GetPositionFromCharIndex(this.SelectionStart));
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
			}
			iColumn++;
		}
		iColumn+=iScrollColumn-iScrollCounter;
		return iColumn;
	}
	static int iTabs=0;

	Hashtable keyBindings=new Hashtable();
	public delegate void Function();
	private int RealIndexFromIndex(int index)
	{
		return index-emptyLines.Length;
	}
	private int IndexFromRealIndex(int index)
	{
		return index+emptyLines.Length;
	}

	public void MoveDocumentEnd()
	{
		SelectionStart=IndexFromRealIndex(RealText.Length-1);
	}
	public void MoveDocumentStart()
	{
		SelectionStart=IndexFromRealIndex(0);
	}

	public void DeleteBackward()
	{
		Select(SelectionStart-1,1);
		SelectedText="";
	}
	public void DeleteForward()
	{
		Select(SelectionStart,1);
		SelectedText="";
	}
	public void MoveLineEnd()
	{
		MoveTo(GetLinesLength(Line)+Lines[Line].Length);
	}
	public void MoveLineStart()
	{
		int start=GetLinesLength(Line);
		start+=this.GetTabs(Lines[Line]);
		MoveTo(start);
	}


	// TODO: implement
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
	public void SelectWordRight()
	{
	}
	public void DeleteWordRight()
	{		
	}
	public void DeleteWordLeft()
	{
	}
	

	public void MoveWordRight()
	{
		if(!Char.IsLetter(Character))
		{
			MoveCharRight();
		}
		else
		{
			while(Char.IsLetter(Character))
			{
				MoveCharRight(); // TODO: �berschreiben, um Abst�rze zu vermeiden ??
			}
		}
	}
	public void MoveLineDown() 
	{
		MoveCursor(Line+1,GetScrollColumn());
	}

	public void MoveWordLeft()
	{
		if(!Char.IsLetter(Character))
		{
			MoveCharLeft();
		}
		else
		{
			while(Char.IsLetter(Character))
			{
				MoveCharLeft(); // TODO: �berschreiben, um Abst�rze zu vermeiden ??
			}
		}
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

	public void FindAndReplace()
	{
		replace.Owner=FindForm();
		replace.Show();
	}
	public void StopInteractiveSearch()
	{
		this.Cursor=Cursors.IBeam;
		interactiveSearch.Stop();
	}
	public void StartInteractiveSearch()
	{
		if(interactiveSearch.Active)
		{
			SelectionStart++;
			interactiveSearch.Find();
		}
		else
		{
			this.Cursor=Cursors.PanSouth;
			interactiveSearch.Start();
		}
	}





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
			int newStart=lineLength+columns;
			if(newStart>=Text.Length) 
			{
				newStart=Text.Length-1;
			}
			SelectionStart=newStart;
		}
	}
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
			return SelectionStart-LinesLength;
		}
	}
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
	private FindAndReplace replace;

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
	public char Character
	{
		get
		{
			return Text[SelectionStart];
		}
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
			// TODO: check out this stuff: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/thread/1ef28b955bd06981/27f324e4a1b722d4?q=c%23+control+i+richtextbox+tab&rnum=5&hl=en#27f324e4a1b722d4
			SetScrollPos(point);
		}

	}

	private void ScrollingTextBox_SelectionChanged(object sender, System.EventArgs e) 
	{
		ScrollToMiddle(); // make this method more general!!!!!
	}
	private void ScrollingTextBox_TextChanged(object sender, System.EventArgs e)
	{
		ScrollToMiddle();
	}

	

	protected void MoveCaretToMiddle() 
	{
		this.SetSelectionStartNoScroll(GetCharIndexFromPosition(new Point(this.Size.Width/2,this.Height/2)));
	}




	protected override void WndProc(ref Message m) 
	{
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
		}
		else
		{
			if ( m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL ) 
			{
				this.MoveCaretToMiddle();
			}
			base.WndProc (ref m);
		}
	}
}
