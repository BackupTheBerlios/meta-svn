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
		textBox=this;
		for(int i=0;i<25;i++)
		{
			emptyLines+="\n";
		}
//		this.interactiveSearch=new InteractiveSearch((RichTextBox)this);
		this.replace=new FindAndReplace(this);
		this.timer.Interval=50;
		this.timer.Tick+=new EventHandler(timer_Tick);
		this.replace.Closing+=new CancelEventHandler(replace_Closing);
	}
	private static ScrollingTextBox textBox;


	private static string emptyLines;
	public string TopMargin
	{
		get
		{
			return emptyLines;
		}
	}
	public string BottomMargin
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

//	int GetScrollColumn() 
//	{ 
//		int iColumn=Column;
//		int iTabs=GetTabs(Line.Left);
//		int iScrollLine=iTabs*3+iColumn; // TODO: make 4 a const used everywhere
//		return iScrollLine;
//	}

	private int[] tabStops=new int[32];
	private void ScrollingTextBox_Resize(object sender, System.EventArgs e) 
	{
		if(SelectionStart!=-1) 
		{ 
			SuspendWindowUpdate();// TODO: refactor
			int selectionStart=SelectionStart;
			SelectAll();
			double width=Convert.ToDouble(this.Size.Width)/2.618f;
			double height=Convert.ToDouble(this.Size.Height)/2.618f;
			SelectionIndent=Convert.ToInt32(width);
			SelectAll();
			int tabWidth=32;
			int firstTabStop=Convert.ToInt32(width)%tabWidth;
			for(int i = 0; i < tabStops.Length; i++)
			{
				tabStops[i] = i*tabWidth+firstTabStop;
			}
			SelectionTabs=tabStops;
			Select(selectionStart,0);
			ResumeWindowUpdate();
		}
	}
	private bool wasControlITab=false;
	private void ScrollingTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) 
	{
		if(e.KeyChar=='\t' && wasControlITab) // ignore Ctrl+I, which would insert a tab
		{
			wasControlITab=false;
			e.Handled=true;
		}
		else if(e.KeyChar=='µ') // ignore 'µ', which is Ctrl+Alt+M
		{
			e.Handled=true;
		}
		else
		{
			if(interactiveSearch.Active)
			{
				if(e.KeyChar!=(char)Keys.Back)
				{
					interactiveSearch.OnKeyPress(e.KeyChar);
					e.Handled=true;
				}
			}
			else if(e.KeyChar.Equals('\t'))
			{
				e.Handled=true;
			}
			else
			{
				if(e.KeyChar.Equals((char)Keys.Enter)) 
				{
					string sTabs="";
					for(int i=0;i<Lines[Line.Index-1].Tabs;i++) 
//						for(int i=0;i<iTabs;i++) 
					{
						sTabs+='\t';
					}
					SelectedText=sTabs;
				}
			}
		}
	}
	private void ScrollingTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) 
	{
		if(e.KeyData==(Keys.Control|Keys.I))
		{
			wasControlITab=true;
		}
//		iTabs=GetTabs(Line.Left);
	}
	private InteractiveSearch interactiveSearch=new InteractiveSearch();



	private System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
	private void timer_Tick(object sender, EventArgs e)
	{
		DrawInfo();
	}
	protected void DrawInfo() // get back into the correct thread!!!!!!!!!!
	{
		if(info!=null)
		{
			info.Draw(this.CreateGraphics());//,CursorPosition);
		}
	}
	public class Info
	{
		private string text;
		private Font font=new Font("Courier New",10.0f);
		public Info(string text,Point position)
		{
			this.position=position;
			this.text=text;
		}
		Point position;
		public void Draw(Graphics graphics)//,Point position)
		{
			graphics.DrawString(text,font,Brushes.Red,position);
			size=graphics.MeasureString(text,font);
		}
		SizeF size=new SizeF(0,0);
		public Rectangle Rectangle
		{
			get
			{
				return new Rectangle(position,size.ToSize());
			}
		}
	}
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint (e);
		DrawInfo();
	}
//	public Point CursorPosition
//	{
//		get
//		{
//			return GetPositionFromCharIndex(SelectionStart);
//		}
//	}
	Info info;
	public void ShowDebugValue(Map debugValue)
	{
		Graphics graphics=this.CreateGraphics();
		MessageBox.Show(Serialize.Value((Map)debugValue));
//		info=new Info(Serialize.Value((Map)debugValue),CursorPosition);
		DrawInfo();
	}
//	public int ColumnFromScrollColumn(int line,int scrollColumn)
//	{
//		int i=0;
//		for(;scrollColumn>0 && Lines[line].Length>i;scrollColumn--,i++)
//		{
//			if(Lines[line][i]=='\t')
//			{
//				scrollColumn-=3;
//			}
//		}
//		return i;
//	}
//	static int iTabs=0;

//	private int RealIndexFromIndex(int index)
//	{
//		return index-emptyLines.Length;
//	}
//	private int IndexFromRealIndex(int index)
//	{
//		return index+emptyLines.Length;
//	}
//	public string[] RealLines
//	{
//		get
//		{
//			ArrayList realLines=new ArrayList(Lines).GetRange(TopMargin.Length,base.Lines.Length-BottomMargin.Length-TopMargin.Length);
//			return (string[])realLines.ToArray(typeof(string));
//		}
//	}
	// TODO: switch Line and RealLine around
	public int RealLineFromLine(int line)
	{
		int realLine=line-TopMargin.Length;
		if(realLine<0)
		{
			realLine=0;
		}
		return realLine;
	}
	public int LineFromRealLine(int realLine)
	{
		return realLine+TopMargin.Length;
	}
	protected new LineCollection Lines
	{
		get
		{
			return new LineCollection(this);
//			ArrayList lines=new ArrayList();
//			for(int i=0;i<base.Lines.Length-TopMargin.Length-BottomMargin.Length;i++)
//				//				for(int i=0;i<RealLines.Length;i++)
//			{
//				lines.Add(new Line(i,this));
//			}
//			return (Line[])lines.ToArray(typeof(Line));
		}
	}
//	protected new Line[] Lines
//	{
//		get
//		{
//			ArrayList lines=new ArrayList();
//			for(int i=0;i<base.Lines.Length-TopMargin.Length-BottomMargin.Length;i++)
////				for(int i=0;i<RealLines.Length;i++)
//			{
//				lines.Add(new Line(i,this));
//			}
//			return (Line[])lines.ToArray(typeof(Line));
//		}
//	}

	// TODO: put RealLines transformation into MoveCursor
	public void MoveDocumentEnd()
	{
		MoveAbsolute(Lines[Lines.Length-1].Length,Lines.Length-1);
	}
	public void MoveDocumentStart()
	{
		MoveAbsolute(0,0);
	}

	public void DeleteCharLeft()
	{
		if(interactiveSearch.Active)
		{
			interactiveSearch.DeleteCharLeft();
		}
		else
		{
			if(SelectedText.Length==0)
			{
				Select(SelectionStart-1,1);
			}
			SelectedText="";
		}
	}
	public void DeleteCharRight()
	{
		if(SelectedText.Length==0)
		{
			Select(SelectionStart,1);// TODO: use SelectRelative?
		}
		SelectedText="";
	}
	public void SelectRelative(int column, int line)
	{
		SelectAbsolute(Column+column,Line.Index+line);
	}
	private int selectStart;
	public void SelectAbsolute(int column,int line)
	{
		if(SelectionLength==0)
		{
			selectStart=SelectionStart;
		}
		int selectionDiff=GetLinesLength(LineFromRealLine(line))+column-selectStart;
		int start;
		if(selectionDiff>0)
		{
			start=selectStart;
		}
		else
		{
			start=selectStart+selectionDiff;
		}
		base.Select(start,Math.Abs(selectionDiff));
	}
	public void SelectLineDown()
	{
		SelectRelative(0,1);
	}
	public void SelectLineUp()
	{
		SelectRelative(0,-1);
	}
	public void SelectLineEnd()
	{
		SelectAbsolute(Line.Length,Line.Index);
	}
	public void SelectLineStart()
	{
		SelectAbsolute(Line.Tabs,Line.Index);
	}
	public void SelectCharLeft()
	{
		SelectRelative(-1,0);
	}
	public void SelectCharRight()
	{
		Select(1,0);
	}
	private Point GetNextWordPosition()
	{
		return GetWordPosition(1);
	}
	private Point GetPreviousWordPosition()
	{
		return GetWordPosition(-1);
	}
	private Point GetWordPosition(int direction)
	{
		int moved=0;
		while(true)
		{
			int next=SelectionStart+moved;
			if(direction<0)
			{
				next-=1;
			}
			if(!Char.IsLetterOrDigit(this.Text[next]))
			{
				break;
			}
			else
			{
				moved+=direction;
			}
		}
		if(moved==0)
		{
			moved+=direction;
		}
		int index=SelectionStart+moved;
		// TODO: SelectionStart isnt really accurate, use our own selectionstart
		return new Point(GetColumnFromCharIndex(index),RealLineFromLine(GetLineFromCharIndex(index)));
	}
	public void SelectWordLeft()
	{
		Point position=GetPreviousWordPosition();
		SelectAbsolute(position.X,position.Y);
	}
	public void SelectWordRight()
	{
		Point position=GetNextWordPosition();
		SelectAbsolute(position.X,position.Y);
	}
	public void DeleteWordRight()
	{
		SelectWordRight();
		SelectedText="";
	}
	public void DeleteWordLeft()
	{
		SelectWordLeft();
		SelectedText="";
	}
	private static int lastColumn=-1;

	public void MoveRelative(int column,int line)
	{
		MoveAbsolute(Column+column,Line.Index+line);
	}
	public void MoveAbsolute(Point move)
	{
		MoveAbsolute(move.X,move.Y);
	}
	public void MoveAbsolute(int column,int line)
	{
		Select(GetLinesLength(LineFromRealLine(line))+column,0);
	}
	private int GetLinesPerPage()
	{
		return Convert.ToInt32(((double)this.Height/(double)this.Font.Height)/3.3);
	}
	public void MovePageDown()
	{
		MoveRelative(0,GetLinesPerPage());
	}
	public void MovePageUp()
	{
		MoveRelative(0,-GetLinesPerPage());
	}
	private Point MoveLine(int direction)
	{
		Line newLine=Lines[Line.Index+direction];
		int newCol=newLine.Tabs+(Column-Line.Tabs);
		return new Point(newCol,newLine.Index);
	}
	public void MoveLineDown() 
	{
		MoveAbsolute(MoveLine(1));
//		MoveRelative(0,1);
	}
	public void MoveLineUp() 
	{
		MoveAbsolute(MoveLine(-1));
//		MoveRelative(0,-1);
	}
	public void MoveLineEnd()
	{
		MoveAbsolute(Line.Length,Line.Index);
	}

	public void MoveLineStart()
	{
//		if(Line.Tabs!=Column)
//		{
//			MoveAbsolute(Line.Tabs,Line.Index);
//		}
//		else
//		{
			MoveAbsolute(Line.Tabs,Line.Index);
//		}
	}
	public void MoveWordRight()
	{
		Point position=GetNextWordPosition();
		MoveAbsolute(position.X,position.Y);
	}
	// TODO: refactor
	public void MoveWordLeft()
	{
		Point position=GetPreviousWordPosition();
		MoveAbsolute(position.X,position.Y);
	}
	public void MoveCharLeft() 
	{
		MoveRelative(-1,0);
	}
	public void MoveCharRight() 
	{
		MoveRelative(1,0);
	}


	public void FindAndReplace()
	{
		replace.Owner=this.FindForm();
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
//	public void IncreaseSelectionIndent()
//	{
//		int selectionStart=SelectionStart;
//		int selectionEnd=selectionStart+SelectedText.Length;
//		if(selectionEnd>selectionStart)
//		{
//			selectionEnd--;
//		}
//		int lastLine=GetLineFromCharIndex(selectionEnd);
//		int startLine=Line.Index;
//		for(int line=startLine;line<=lastLine;line++)
//		{
//			Select(GetLinesLength(line)+GetTabs(Lines[line]),0);
//			SelectedText="\t";
//		}
//		if(selectionStart==selectionEnd)
//		{
//			Select(selectionStart+1,0);
//		}
//		else
//		{
//			Select(selectionStart,selectionEnd-selectionStart+(lastLine-startLine)+1);
//		}
//	}
//	public void DecreaseSelectionIndent()
//	{
//		int selectionStart=SelectionStart;
//		int selectionEnd=selectionStart+SelectedText.Length;
//		int removedTabs=0;
//		int lastLine=GetLineFromCharIndex(selectionEnd-1);
//		int startLine=Line.Index;
//		for(int line=startLine;line<=lastLine;line++)
//		{
//			if(GetTabs(Lines[line])>0)
//			{
//				removedTabs++;
//				Select(GetLinesLength(line)+GetTabs(Lines[line])-1,1);
//				SelectedText="";
//			}
//		}
//		int length=selectionEnd-selectionStart-removedTabs;
//		if(length<0)
//		{
//			length=0;
//		}
//		Select(selectionStart,length);
//	}

	[DllImport("user32.dll")]
	public static extern bool LockWindowUpdate(IntPtr hWndLock);


	public void SuspendWindowUpdate()
	{
		LockWindowUpdate(Handle);
	}
	public void ResumeWindowUpdate()
	{
		LockWindowUpdate(IntPtr.Zero);
	}

	// put this into Line?
	int GetLinesLength(int iLine) 
	{
		iLine=iLine<base.Lines.Length?iLine:base.Lines.Length-1;
		if(iLine<0) 
		{
			return 0;
		}
		int count=0;
		ArrayList lines=new ArrayList(base.Lines);
		foreach(string s in lines.GetRange(0,iLine)) 
		{
			count+=s.Length+1;
		}
		return count;
	}

	public Line Line
	{
		get
		{
			// SelectionStart ist nicht korrekt, muss die Cursor-Position sein
			return new Line(RealLineFromLine(GetLineFromCharIndex(SelectionStart)),this);
		}
	}
	public int Column
	{
		get
		{
			int column=SelectionStart-GetLinesLength(LineFromRealLine(Line.Index));
			if(column<0)
			{
				column=0;
			}
			return column;
		}
		set
		{
			SelectionStart=GetLinesLength(Line.Index)+value;
		}
	}
	public string RealText
	{
		get
		{
			string text="";
			foreach(string line in base.Lines)
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

	private bool suspendScroll=false;

	// TODO: get rid of this function
	private void SetSelectionStartNoScroll(int selectionStart)  // what is that?
	{
		this.suspendScroll=true;
		SelectionStart=selectionStart;
		this.suspendScroll=false;
	}
	// TODO: implement proper tabstops, see http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/thread/1ef28b955bd06981/27f324e4a1b722d4?q=c%23+control+i+richtextbox+tab&rnum=5&hl=en#27f324e4a1b722d4
	void ScrollToMiddle() 
	{
		if(!suspendScroll) 
		{
			Win32.POINT point=new Win32.POINT();
			//		int colC=Column;
			int lineL=LineFromRealLine(Line.Index);
			string lineS=Line.Text;
//			string lineS=GetLineText();
			Graphics graphics=this.CreateGraphics();
			SizeF sizeF=graphics.MeasureString(Line.Left.Replace("\t","aaaaa").Replace(" ","a"),Font,new PointF(0.0f,0.0f),(StringFormat)StringFormat.GenericTypographic.Clone());
			point.x=(int)sizeF.Width; // this sucks, can't we scroll the rest, too
			point.y=(int)((this.Font.Height+1.0)*(float)lineL-(float)this.Size.Height/1.618f); // -1.0 is magic number, don't know why
			// TODO: check out this stuff: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/thread/1ef28b955bd06981/27f324e4a1b722d4?q=c%23+control+i+richtextbox+tab&rnum=5&hl=en#27f324e4a1b722d4
			SetScrollPos(point);
		}

	}
	private void ScrollingTextBox_SelectionChanged(object sender, System.EventArgs e) 
	{
		ScrollToMiddle();
	}

	private void ScrollingTextBox_TextChanged(object sender, System.EventArgs e)
	{
		ScrollToMiddle();
	}


	protected void MoveCaretToMiddle() 
	{
		this.SetSelectionStartNoScroll(GetCharIndexFromPosition(new Point(this.Size.Width/2,this.Height/2)));
	}
	public int GetColumnFromCharIndex(int charIndex)
	{
		return charIndex-GetLinesLength(GetLineFromCharIndex(charIndex));
	}



	protected override void WndProc(ref Message m) 
	{
		if(m.Msg == Win32.WM_MOUSEWHEEL) 
		{
			if(m.WParam.ToInt32()>IntPtr.Zero.ToInt32()) 
			{
				MoveAbsolute(MoveLine(-3));
//				MoveRelative(0,-3);
			}
			else if(m.WParam.ToInt32()<IntPtr.Zero.ToInt32()) 
			{
				MoveAbsolute(MoveLine(3));
//				MoveRelative(0,3);
			}
		}
		else if(m.Msg == Win32.WM_LBUTTONDOWN)
		{
			Point mousePos=new Point((int)m.LParam);
			int charIndex=this.GetCharIndexFromPosition(mousePos);
			MoveAbsolute(charIndex-GetLinesLength(GetLineFromCharIndex(charIndex)),RealLineFromLine(GetLineFromCharIndex(charIndex)));
		}
		else if(m.Msg==Win32.WM_LBUTTONDBLCLK)
		{
		}
		else
		{
			if ( m.Msg == Win32.WM_HSCROLL || m.Msg == Win32.WM_VSCROLL ) 
			{
				this.MoveCaretToMiddle();
			}
			if(m.Msg == Win32.WM_PAINT)
			{
				if(info!=null)
				{
					Invalidate();
				}
			}	
			base.WndProc (ref m);
			if(m.Msg == Win32.WM_PAINT)
			{
//				DrawInfo();
			}	
		}
	}

	private void replace_Closing(object sender, CancelEventArgs e)
	{
		// because of stupid bug, where editor doesnt have focus after replacing all
		this.FindForm().BringToFront();
	}
	public class InteractiveSearch
	{
		public void DeleteCharLeft()
		{
			if(text.Length!=0)
			{
				text=text.Remove(text.Length-1,1);
			}
			textBox.SelectionStart=startPosition;
			Find();
		}
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
			startPosition=textBox.SelectionStart;
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
			textBox.Select(textBox.SelectionStart,0);
			text="";
		}
	}
}
public class Win32 
{
	// copied from: http://groups-beta.google.com/group/microsoft.public.dotnet.languages.csharp/browse_frm/horizontalThread/b142ab4621009180/2d7ab486ca1f4d43?q=richtextbox+scrolling&rnum=8&hl=en#2d7ab486ca1f4d43
	public const int WM_VSCROLL = 0x0115;
	public const int WM_MOUSEWHEEL = 0x020a;

	public const int WM_HSCROLL = 0x0114;

	public const int WM_LBUTTONDBLCLK = 0x203;
	public const int WM_LBUTTONDOWN = 0x201;


	public const int WM_USER = 0x400;
	public const int WM_PAINT = 0xF;
	public const int EM_SETSCROLLPOS  =       (WM_USER + 222);

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
// maybe put this into ScrollingTextBox
public class LineCollection
{
	public LineCollection(ScrollingTextBox textBox)
	{
		this.textBox=textBox;
	}
	private ScrollingTextBox textBox;
	public int Length
	{
		get
		{
			return ((RichTextBox)textBox).Lines.Length-textBox.TopMargin.Length-textBox.BottomMargin.Length;
		}
	}
	public Line this[int index]
	{
		get
		{
			if(index<0)
			{
				index=0;
			}
			else if(index>=Length)
			{
				index=Length-1;
			}
			return new Line(index,textBox);
		}
	}
}
public class Line
{
	public int Tabs
	{
		get
		{
			return Text.Length-Text.TrimStart('\t').Length;
		}
	}
	private ScrollingTextBox textBox;
	private int index;
	public Line(int index,ScrollingTextBox textBox)
	{
		this.index=index;
		this.textBox=textBox;
	}
	public int Index
	{
		get
		{
			return index;
		}
	}
	public int Length
	{
		get
		{
			return Text.Length;
		}
	}
	public string Text
	{
		get
		{
			return ((RichTextBox)textBox).Lines[textBox.LineFromRealLine(index)];
		}
	}
	public string Left
	{
		get
		{
			int iColumn=textBox.Column;
			string sLine=Text;
			string sLeft=sLine.Substring(0,iColumn);
			return sLeft;
		}
	}
}