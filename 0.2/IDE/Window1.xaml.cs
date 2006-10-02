using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Meta;
using System.IO;
using System.Diagnostics;


namespace IDE
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>

	public partial class Window1 : System.Windows.Window
	{
		public static KeyboardDevice keyboard;
		TextBox textBox = new TextBox();

		private void Save()
		{
			if (fileName == "")
			{
				SaveFileDialog dialog = new SaveFileDialog();
				if (dialog.ShowDialog()==true)
				{
					fileName = dialog.FileName;
				}
			}
			if (fileName != "")
			{
				File.WriteAllText(fileName, textBox.Text);
			}
		}
		private string fileName = "";
		private void BindKey(RoutedUICommand command, Key key, ModifierKeys modifiers)
		{
			command.InputGestures.Add(new KeyGesture(key,modifiers));
		}
		public Window1()
		{
			BindKey(EditingCommands.Backspace, Key.N, ModifierKeys.Alt);
			BindKey(EditingCommands.Delete, Key.M, ModifierKeys.Alt);
			BindKey(EditingCommands.DeleteNextWord, Key.M, ModifierKeys.Alt|ModifierKeys.Control);
			BindKey(EditingCommands.DeletePreviousWord, Key.N, ModifierKeys.Alt|ModifierKeys.Control);
			BindKey(EditingCommands.MoveDownByLine,Key.K, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveDownByPage, Key.K, ModifierKeys.Alt|ModifierKeys.Control);
			BindKey(EditingCommands.MoveLeftByCharacter, Key.J, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveRightByCharacter, Key.Oem3, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveUpByLine, Key.L, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveLeftByWord, Key.J, ModifierKeys.Alt|ModifierKeys.Control);
			BindKey(EditingCommands.MoveRightByWord, Key.M, ModifierKeys.Alt|ModifierKeys.Control);
			BindKey(EditingCommands.MoveToDocumentEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.MoveToDocumentStart, Key.U, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.MoveToLineEnd, Key.Oem1, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveToLineStart, Key.U, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveUpByPage, Key.L, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.SelectDownByLine, Key.K, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectDownByPage, Key.K, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectLeftByCharacter, Key.J, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectLeftByWord, Key.J, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectRightByCharacter, Key.Oem3, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectRightByWord, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectToDocumentEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectToDocumentStart, Key.U, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectToLineEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectToLineStart, Key.U, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectUpByLine, Key.L, ModifierKeys.Alt | ModifierKeys.Shift);
			BindKey(EditingCommands.SelectUpByPage, Key.L, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
			InitializeComponent();
			textBox.FontSize = 16;
			textBox.FontFamily = new FontFamily("Courier New");
			textBox.AcceptsTab = true;
			textBox.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				keyboard = e.KeyboardDevice;
				if (e.Key == Key.Return)
				{
					string line=textBox.GetLineText(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart));
					textBox.SelectedText = "\n".PadRight(1+line.Length - line.TrimStart('\t').Length, '\t');
					textBox.SelectionStart = textBox.SelectionStart + textBox.SelectionLength;
					textBox.SelectionLength = 0;
				}
				else if (e.Key == Key.OemPeriod)
				{
					string text=textBox.Text.Substring(0, textBox.SelectionStart);
					//Interpreter.profiling = false;
					//Map map=Parser.ParseString(text, fileName);
					
					//LiteralExpression gac = new LiteralExpression(Gac.gac, null);
					//LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
					//lib.Statement = new LiteralStatement(gac);
					//callable[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
					//callable[CodeKeys.Function].Compile(lib);
					//MessageBox.Show(map.ToString());
				}
				//else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
				//{

				//    if (e.Key == Key.L || e.SystemKey==Key.L)
				//    {
				//        //KeyEventArgs k = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp+10, Key.U);
				//        //textBox.Do(e);
				//        EditingCommands.MoveUpByLine.Execute(null, textBox);
						
				//        //textBox.LineUp();
				//    }
				//}
			};
			DockPanel panel = new DockPanel();
			Menu menu = new Menu();
			menu.SetValue(DockPanel.DockProperty, Dock.Top);
			textBox.SetValue(DockPanel.DockProperty, Dock.Bottom);

			MenuItem file =new MenuItem();

			MenuItem save=new MenuItem();
			MenuItem run=new MenuItem();
			MenuItem open = new MenuItem();
			run.Header = "Run";
			save.Header = "Save";
			file.Header = "File";
			open.Header = "Open";
			file.Items.Add(save);
			file.Items.Add(run);
			file.Items.Add(open);
			open.Click += delegate
			{
				OpenFileDialog dialog = new OpenFileDialog();
				if (dialog.ShowDialog()==true)
				{
					fileName = dialog.FileName;
					textBox.Text = File.ReadAllText(fileName);
				}
			};
			run.Click += delegate
			{
				Save();
				Process.Start(System.IO.Path.Combine(@"C:\Meta\0.2\", @"bin\Debug\Meta.exe"), fileName);
			};
			save.Click += delegate
			{
				Save();
			};
			menu.Items.Add(file);
			panel.Children.Add(menu);
			panel.Children.Add(textBox);
			this.Content = panel;
		}


	}
}