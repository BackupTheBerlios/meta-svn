using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Editor {
	class MainClass {
		static Form form=new Form();
		static RichTextBox text=new RichTextBox();
		static ListBox help=new ListBox();
		static OpenFileDialog openDialog=new OpenFileDialog();
		static SaveFileDialog saveDialog=new SaveFileDialog();
		static string fileName="";
		[STAThread]
		static void Main(string[] args) {
			Application.Run(form);
		}
		static MainClass() {
			help.Location=new Point(100,100);
			text.Dock=DockStyle.Fill;
			form.Controls.Add(help);
			form.Controls.Add(text);
			text.KeyPress+=new KeyPressEventHandler(text_KeyPress);
		}
		private static void text_KeyPress(object sender, KeyPressEventArgs e) {
			if(e.KeyChar.Equals('.')) {
				int asdf=0;
			}
		}
	}
}
//    then==
//      #programText=Insert(textBox.Text,textBox.SelectionStart,'".break"')
//      Console.WriteLine(programText)
//      thread=Thread
//        ==
//          textBox.Invoke
//            ==
//              result=RunString(programText)
//              Console.WriteLine(result)
//      thread.Start()
