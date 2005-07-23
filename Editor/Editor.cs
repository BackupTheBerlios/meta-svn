using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using Meta;

namespace EditorTest
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private ScrollingTextBox richTextBox1;
		private System.ComponentModel.IContainer components;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

		}


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
			this.richTextBox1 = new ScrollingTextBox();
			this.SuspendLayout();
			// 
			// richTextBox1
			// 
			this.richTextBox1.AcceptsTab = true;
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(177)));
			this.richTextBox1.Location = new System.Drawing.Point(0, 0);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
			this.richTextBox1.ShowSelectionMargin = true;
			this.richTextBox1.Size = new System.Drawing.Size(480, 405);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.RealText = "this=map.merge\n\tparent\n\tprogramFlow\n\tlogic\n\tmap\n\tmath\n\tSystem\n\tSystem.IO\n\tSystem." +
				"Windows.Forms\n\tSystem.Drawing\n//interpreter.line:\'5\n//interpreter.column:\'50\nMet" +
				"a.Execution.Interpreter.BreakPoint:!Console.WriteLine \'Joy\nMeta.Execution.Interp" +
				"reter.run\'\nopenFileDialog=!\n\tdialog=OpenFileDialog\'\n\tdialog.ShowDialog\'\n\tthis=di" +
				"alog.FileName\nsaveFileDialog=!\n\tdialog=SaveFileDialog\'\n\tdialog.ShowDialog\'\n\tthis" +
				"=dialog.FileName\nsaveAs=!\n\tsaveAsFileName=saveFileDialog\'\n\tif\n\t\tnot equal\n\t\t\tsav" +
				"eAsFileName\n\t\t\t\'\n\t\t!\n\t\t\tfileName:saveAsFileName\n\t\t\twriteFile \n\t\t\t\tfileName\n\t\t\t\te" +
				"dit.getText\'\nwriteFile=!\n\twriter=StreamWriter arg.1\n\twriter.Write arg.2\n\twriter." +
				"Close\'\nfileName=\'\nsave=!\n\tif\n\t\tequal\n\t\t\tfileName\n\t\t\t\'\n\t\t!saveAs\'\n\t\t!\n\t\t\twriteFil" +
				"e\n\t\t\t\tfileName\n\t\t\t\tedit.getText\'\nreadFile=!\n\treader=StreamReader arg\n\tresult=rea" +
				"der.ReadToEnd\'\n\treader.Close\'\n\tthis=result\nopenFile=!\n\topenFileName=openFileDial" +
				"og\'\n\tif\n\t\tnot equal\n\t\t\topenFileName\n\t\t\t\'\n\t\t!\n\t\t\tfileName:openFileName\n\t\t\tedit.se" +
				"tText readFile fileName\neditor=!\n\tcontrol=init\n\t\tRichTextBox\'\n\t\t=\n\t\t\tShowSelecti" +
				"onMargin=\'1\n\t\t\tAcceptsTab=\'1\n\t\t\tDock=DockStyle.Fill\n\t\t\tScrollBars=RichTextBoxScr" +
				"ollBars.ForcedBoth\n\tgetText=!control.Text\n\tsetText=!\n\t\tcontrol.Text:arg\nwindow=!" +
				"\n\tform=Form\'\n\tform.Text=arg.title\n\tform.Font=arg.font\n\tcontrols=arg.controls\n\tfo" +
				"rm.Controls=apply\n\t\t!arg.value.control\n\t\targ.controls\n\tform.WindowState=arg.wind" +
				"owState\n\tInitSubMenuItem=!init\n\t\tMenuItem\'\n\t\t=\n\t\t\tText=arg.key\n\t\t\tClick=arg.valu" +
				"e\n\tInitMenuItem=!init\n\t\tMenuItem\'\n\t\t=\n\t\t\tText=arg.key\n\t\t\tMenuItems=apply\n\t\t\t\tIni" +
				"tSubMenuItem\n\t\t\t\targ.value\n\tform.Menu=init\n\t\tMainMenu\'\n\t\t=\n\t\t\tMenuItems=apply\n\t\t" +
				"\t\tInitMenuItem\n\t\t\t\targ.menu\n\tShow=!\n\t\tform.ShowDialog\'\n\tClose=!form.Close\'\nedit=" +
				"editor\'\n// rename this to something less stupid\nwin=window\n\ttitle=\"Meta Editor\"\n" +
				"\tmenu=\n\t\tFile=\n\t\t\t[\"Open...\"]=!\n\t\t\t\topenFile\'\n\t\t\tSave=save\n\t\t\t[\"Save as...\"]=sav" +
				"eAs\n\t\t\tClose=!window.Close\'\n\t\tProgram=\n\t\t\tStart=!\'\n\t\tDebug=\n\t\t\t[\"Set Breakpoint\"" +
				"]=!\'\n\tfont=Font\n\t\t\"Courier New\"\n\t\t\'10\n\topacity=\n\t\tiNumerator=\'10\n\t\tiDenominator=" +
				"\'10\n\twindowState=FormWindowState.Maximized\n\tcontrols=\n\t\tedit\nwin.Show\'n\t\t!\n\t\t\tfi" +
				"leName:openFileName\n\t\t\tedit.setText readFile fileName\neditor=!\n\tcontrol=init\n\t\tR" +
				"ichTextBox\'\n\t\t=\n\t\t\tShowSelectionMargin=\'1\n\t\t\tAcceptsTab=\'1\n\t\t\tDock=DockStyle.Fil" +
				"l\n\t\t\tScrollBars=RichTextBoxScrollBars.ForcedBoth\n\tgetText=!control.Text\n\tsetText" +
				"=!\n\t\tcontrol.Text:arg\nwindow=!\n\tform=Form\'\n\tform.Text=arg.title\n\tform.Font=arg.f" +
				"ont\n\tcontrols=arg.controls\n\tform.Controls=apply\n\t\t!arg.value.control\n\t\targ.contr" +
				"ols\n\tform.WindowState=arg.windowState\n\tInitSubMenuItem=!init\n\t\tMenuItem\'\n\t\t=\n\t\t\t" +
				"Text=arg.key\n\t\t\tClick=arg.value\n\tInitMenuItem=!init\n\t\tMenuItem\'\n\t\t=\n\t\t\tText=arg." +
				"key\n\t\t\tMenuItems=apply\n\t\t\t\tInitSubMenuItem\n\t\t\t\targ.value\n\tform.Menu=init\n\t\tMainM" +
				"enu\'\n\t\t=\n\t\t\tMenuItems=apply\n\t\t\t\tInitMenuItem\n\t\t\t\targ.menu\n\tShow=!\n\t\tform.ShowDia" +
				"log\'\n\tClose=!form.Close\'\nedit=editor\'\n// rename this to something less stupid\nwi" +
				"n=window\n\ttitle=\"Meta Editor\"\n\tmenu=\n\t\tFile=\n\t\t\t[\"Open...\"]=!\n\t\t\t\topenFile\'\n\t\t\tS" +
				"ave=save\n\t\t\t[\"Save as...\"]=saveAs\n\t\t\tClose=!window.Close\'\n\t\tProgram=\n\t\t\tStart=!\'" +
				"\n\t\tDebug=\n\t\t\t[\"Set Breakpoint\"]=!\'\n\tfont=Font\n\t\t\"Courier New\"\n\t\t\'10\n\topacity=\n\t\t" +
				"iNumerator=\'10\n\t\tiDenominator=\'10\n\twindowState=FormWindowState.Maximized\n\tcontro" +
				"ls=\n\t\tedit\nwin.Show\'";
			this.richTextBox1.WordWrap = false;
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(480, 405);
			this.Controls.Add(this.richTextBox1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) 
		{
//			if(args.Length==1 && args[0]=="-meta")
//			{
//				Interpreter.Run(@"C:\_ProjectSupportMaterial\Meta\Editor\editor.meta",new Map());
//			}
//			else
//			{
				Form1 form=new Form1();
				//			form.richTextBox1.Init();
				Application.Run(form);
				//form.richTextBox1.thread.Abort();
//			}
		}
	}
}