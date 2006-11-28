using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
//
//namespace ScrollingTextBox
//{
	/// <summary>
	/// Summary description for Find.
	/// </summary>
	public class FindAndReplace : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button replace;
		private System.Windows.Forms.ComboBox findWhat;
		private System.Windows.Forms.ComboBox replaceWith;
		private System.Windows.Forms.Label findLabel;
		private System.Windows.Forms.Label replaceLabel;
		private System.Windows.Forms.Button findNext;
		private System.Windows.Forms.Button replaceAll;
		private System.Windows.Forms.Button close;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FindAndReplace(System.Windows.Controls.TextBox textBox)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			this.textBox=textBox;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.findNext = new System.Windows.Forms.Button();
			this.replace = new System.Windows.Forms.Button();
			this.replaceAll = new System.Windows.Forms.Button();
			this.close = new System.Windows.Forms.Button();
			this.findWhat = new System.Windows.Forms.ComboBox();
			this.replaceWith = new System.Windows.Forms.ComboBox();
			this.findLabel = new System.Windows.Forms.Label();
			this.replaceLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// findNext
			// 
			this.findNext.Enabled = false;
			this.findNext.Location = new System.Drawing.Point(400, 8);
			this.findNext.Name = "findNext";
			this.findNext.Size = new System.Drawing.Size(80, 24);
			this.findNext.TabIndex = 4;
			this.findNext.Text = "&Find Next";
			this.findNext.Click += new System.EventHandler(this.findNext_Click);
			// 
			// replace
			// 
			this.replace.Enabled = false;
			this.replace.Location = new System.Drawing.Point(400, 40);
			this.replace.Name = "replace";
			this.replace.Size = new System.Drawing.Size(80, 24);
			this.replace.TabIndex = 5;
			this.replace.Text = "&Replace";
			this.replace.Click += new System.EventHandler(this.replace_Click);
			// 
			// replaceAll
			// 
			this.replaceAll.Enabled = false;
			this.replaceAll.Location = new System.Drawing.Point(400, 72);
			this.replaceAll.Name = "replaceAll";
			this.replaceAll.Size = new System.Drawing.Size(80, 24);
			this.replaceAll.TabIndex = 6;
			this.replaceAll.Text = "Replace &All";
			this.replaceAll.Click += new System.EventHandler(this.replaceAll_Click);
			// 
			// close
			// 
			this.close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.close.Location = new System.Drawing.Point(400, 104);
			this.close.Name = "close";
			this.close.Size = new System.Drawing.Size(80, 24);
			this.close.TabIndex = 7;
			this.close.Text = "Close";
			this.close.Click += new System.EventHandler(this.close_Click);
			// 
			// findWhat
			// 
			this.findWhat.Location = new System.Drawing.Point(96, 8);
			this.findWhat.Name = "findWhat";
			this.findWhat.Size = new System.Drawing.Size(280, 21);
			this.findWhat.TabIndex = 0;
			this.findWhat.TextChanged += new System.EventHandler(this.findWhat_TextChanged);
			// 
			// replaceWith
			// 
			this.replaceWith.Location = new System.Drawing.Point(96, 32);
			this.replaceWith.Name = "replaceWith";
			this.replaceWith.Size = new System.Drawing.Size(280, 21);
			this.replaceWith.TabIndex = 1;
			// 
			// findLabel
			// 
			this.findLabel.Location = new System.Drawing.Point(8, 8);
			this.findLabel.Name = "findLabel";
			this.findLabel.Size = new System.Drawing.Size(88, 24);
			this.findLabel.TabIndex = 6;
			this.findLabel.Text = "Find what:";
			// 
			// replaceLabel
			// 
			this.replaceLabel.Location = new System.Drawing.Point(8, 32);
			this.replaceLabel.Name = "replaceLabel";
			this.replaceLabel.Size = new System.Drawing.Size(88, 23);
			this.replaceLabel.TabIndex = 7;
			this.replaceLabel.Text = "Replace with:";
			// 
			// FindAndReplace
			// 
			this.AcceptButton = this.findNext;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.close;
			this.ClientSize = new System.Drawing.Size(488, 136);
			this.Controls.Add(this.replaceLabel);
			this.Controls.Add(this.findLabel);
			this.Controls.Add(this.replaceWith);
			this.Controls.Add(this.findWhat);
			this.Controls.Add(this.close);
			this.Controls.Add(this.replaceAll);
			this.Controls.Add(this.replace);
			this.Controls.Add(this.findNext);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "FindAndReplace";
			this.ShowInTaskbar = false;
			this.Text = "Find and replace";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindAndReplace_KeyDown);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.ReplaceDialog_Closing);
			this.VisibleChanged += new System.EventHandler(this.FindAndReplace_VisibleChanged);
			this.ResumeLayout(false);

		}
		#endregion

		private System.Windows.Controls.TextBox textBox;

		private void AddComboBoxTextToItems(ComboBox comboBox)
		{
			if(comboBox.FindStringExact(comboBox.Text)==-1)
			{
				comboBox.Items.Insert(0,comboBox.Text);
			}
			if(comboBox.Items.Count>10)
			{
				comboBox.Items.RemoveAt(comboBox.Items.Count-1);
			}
		}

		private int initialFindPosition=0;
		private bool passedEndOfDocument=false;
		private void findWhat_TextChanged(object sender, System.EventArgs e)
		{
			initialFindPosition=textBox.SelectionStart;
			passedEndOfDocument=false;
			bool isEnabled;
			if(findWhat.Text.Length==0)
			{
				isEnabled=false;
			}
			else
			{
				isEnabled=true;
			}
			findNext.Enabled=isEnabled;
			replace.Enabled=isEnabled;
			replaceAll.Enabled=isEnabled;
		}
//		private int Find()
//		{
////			found=true;
//			RichTextBoxFinds findOptions=RichTextBoxFinds.MatchCase;
////			if(matchCase.Checked)
////			{
////				findOptions|=RichTextBoxFinds.MatchCase;
////			}
//
//			int searchStart;
//			searchStart=textBox.SelectionStart+textBox.SelectionLength;
//			int findPosition=textBox.Find(findWhat.Text,searchStart,findOptions);
//			if(findPosition==-1)
//			{
//				searchStart=0; // continue search at the start of the document
//				passedEndOfDocument=true;
//				findPosition=textBox.Find(findWhat.Text,searchStart,findOptions);
////				if(findPosition==-1)
////				{
////					found=false;
////				}
//			}
//			return findPosition;
//		}
//		private void Replace()
//		{
////			bool ignoreCase=!matchCase.Checked;
////			int compared=String.Compare(textBox.SelectedText,findWhat.Text,ignoreCase);
//			if(textBox.SelectedText==findWhat.Text)
//			{
//				textBox.SelectedText=replaceWith.Text;
//			}
////			bool ignoreCase=!matchCase.Checked;
////			int compared=String.Compare(textBox.SelectedText,findWhat.Text,ignoreCase);
////			if(compared==0)
////			{
////				textBox.SelectedText=replaceWith.Text;
////			}
//		}
//		private void findNext_Click(object sender, System.EventArgs e)
//		{
//			AddComboBoxTextToItems(findWhat);
//			AddComboBoxTextToItems(replaceWith);
//			int foundPosition=Find();
//			if(foundPosition==-1)
//			{
//				ShowMessageBox("The specified text could not be found.");
//			}
//		}
		private void findNext_Click(object sender, System.EventArgs e)
		{
			//			if(findWhat.FindStringExact(findWhat.Text)==-1)
			//			{
			AddComboBoxTextToItems(findWhat);
			//findWhat.Items.Insert(0,findWhat.Text);
			//			}
			//			if(replaceWith.FindStringExact(replaceWith.Text)==-1)
			//			{
			AddComboBoxTextToItems(replaceWith);
			//replaceWith.Items.Insert(0,replaceWith.Text);
			//			}
			RichTextBoxFinds findOptions=RichTextBoxFinds.MatchCase;
			//			if(searchUp.Checked)
			//			{
			//				findOptions|=RichTextBoxFinds.Reverse;
			//			}
//			if(matchCase.Checked)
//			{
//				findOptions|=RichTextBoxFinds.MatchCase;
//			}
			int searchStart;
			//			if(searchUp.Checked)
			//			{
			//				searchStart=textBox.SelectionStart-1;
			//			}
			//			else
			//			{
			searchStart=textBox.SelectionStart+textBox.SelectionLength;
			//			}
			int findPosition = textBox.Text.IndexOf(findWhat.Text, searchStart);//, findOptions);
			//int findPosition = textBox.Find(findWhat.Text, searchStart, findOptions);
			//int findPosition = textBox.Find(findWhat.Text, searchStart, findOptions);
			if (findPosition == -1)
			{
				//				if(searchUp.Checked)
				//				{
				//					searchStart=textBox.Text.Length;
				//				}
				//				else
				//				{
				searchStart=0;
				//				}
				findPosition = textBox.Text.IndexOf(findWhat.Text, searchStart);//, findOptions);
				//findPosition = textBox.Find(findWhat.Text, searchStart, findOptions);
				if (findPosition == -1)
				{
					ShowMessageBox("The specified text could not be found.");
				}
				//				MessageBox.Show("The specified text could not be found.");
			}
			if (findPosition != -1) {
				textBox.SelectionStart = findPosition;// = textBox.Find(findWhat.Text, textBox.SelectionStart, RichTextBoxFinds.None);
				textBox.SelectionLength = findWhat.Text.Length;
			}
		}
		private void replace_Click(object sender, System.EventArgs e)
		{
//			bool ignoreCase=!matchCase.Checked;
//			int compared=String.Compare(textBox.SelectedText,findWhat.Text,ignoreCase);
			if(textBox.SelectedText==findWhat.Text)
			{
				textBox.SelectedText=replaceWith.Text;
			}
//			Replace();
//			Find();
			findNext_Click(sender,e);
		}
//		private void replace_Click(object sender, System.EventArgs e)
//		{
////			bool ignoreCase=!matchCase.Checked;
////			int compared=String.Compare(textBox.SelectedText,findWhat.Text,ignoreCase);
////			if(compared==0)
////			{
////				textBox.SelectedText=replaceWith.Text;
////			}
//			Replace();
//			Find();
////			findNext_Click(sender,e);
//		}

//
		int CountStringOccurences(string subString,string text)
		{
			int count=0;
			int index=0;
			while(true)
			{
				index=text.IndexOf(subString,index);
				if(index!=-1)
				{
					index+=subString.Length;
					count++;
				}
				else
				{
					break;
				}
			}
			return count;
		}
		private void ShowMessageBox(string text)
		{
			MessageBox.Show(this.FindForm(),text,"Meta Editor",MessageBoxButtons.OK,MessageBoxIcon.Information);
//			MessageBox.Show(this.FindForm(),text,"Meta Editor",MessageBoxButtons.OK,MessageBoxIcon.Information);
		}
		private void replaceAll_Click(object sender, System.EventArgs e)
		{
//			int startPos=textBox.SelectionStart;
//			while(true)
//			{
//				int pos=Find();
//				if(passedEndOfDocument && pos>initialFindPosition)
//				{
//					break;
//				}
//				Replace();
//			}
//			SelectionStart=startPos;
			int selectionStart=textBox.SelectionStart;
			int occurences=CountStringOccurences(findWhat.Text,textBox.Text);
			textBox.Text=textBox.Text.Replace(findWhat.Text,replaceWith.Text);
//			string text;
			textBox.SelectionStart=selectionStart;
			ShowMessageBox(occurences.ToString()+" occurence(s) replaced.");
//			MessageBox.Show(occurences.ToString()+" occurence(s) replaced.");
		}
//		private void replaceAll_Click(object sender, System.EventArgs e)
//		{
//			int startPos=textBox.SelectionStart;
//			while(true)
//			{
//				int pos=Find();
//				if(passedEndOfDocument && pos>initialFindPosition)
//				{
//					break;
//				}
//				Replace();
//			}
//			SelectionStart=startPos;
//
//			//ShowMessageBox(occurences.ToString()+" occurence(s) replaced.");
//
////			string text=textBox.Text;
////			textBox.Text=text.Replace(findWhat.Text,replaceWith.Text);
////			textBox.Text=textBox.Text.Replace(findWhat.Text,replaceWith.Text);
////			string text;
////			int occurences=CountStringOccurences(findWhat.Text,textBox.Text,out text);
////			ShowMessageBox(occurences.ToString()+" occurence(s) replaced.");
//			//			MessageBox.Show(occurences.ToString()+" occurence(s) replaced.");
//		}

		private void close_Click(object sender, System.EventArgs e)
		{
			Hide(); // necessary?
		}

		private void ReplaceDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Hide();
			e.Cancel=true;
		}


		private void FindAndReplace_VisibleChanged(object sender, System.EventArgs e)
		{
			if(this.Visible)
			{
				if(textBox.SelectedText!="")
				{
					findWhat.Text=textBox.SelectedText;
				}
			}
		}

		private void FindAndReplace_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) // this doesn't get called!!!
		{
			if(e.KeyCode==Keys.Escape) 
			{
				Hide();
			}

		}
//		private void findNext_Click(object sender, System.EventArgs e)
//		{
//			
//		}
	}
//}
		
