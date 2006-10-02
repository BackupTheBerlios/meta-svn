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
		public void Open(string file)
		{
			fileName = file;
			textBox.Text = File.ReadAllText(fileName);
		}
		public static ListBox intellisense = new ListBox();
		Menu menu = new Menu();

		public static bool Intellisense
		{
			get
			{
				return intellisense.Visibility == Visibility.Visible;
			}
		}
		public void IntellisenseUp()
		{
			if (intellisense.SelectedIndex > 0)
			{
				intellisense.SelectedIndex--;
			}
		}
		public void IntellisenseDown()
		{
			if (intellisense.SelectedIndex<intellisense.Items.Count-1)
			{
				intellisense.SelectedIndex++;
			}
		}
		public void Complete()
		{
			if (intellisense.SelectedItem != null)
			{
				int index = textBox.SelectionStart;
				textBox.SelectionStart = textBox.Text.LastIndexOf('.', textBox.SelectionStart)+1;
				textBox.SelectionLength = index-textBox.SelectionStart;
				textBox.SelectedText = (string)intellisense.SelectedItem;
				intellisense.Visibility = Visibility.Hidden;
				textBox.SelectionStart += textBox.SelectionLength;
				textBox.SelectionLength = 0;
			}
		}
		string search = "";
		int searchStart = 0;
		public void PositionIntellisense()
		{
			Rect r = textBox.GetRectFromCharacterIndex(textBox.SelectionStart);
			Canvas.SetLeft(intellisense, r.Right);
			Canvas.SetTop(intellisense, r.Bottom);//+(double)menu.GetValue(FrameworkElement.HeightProperty));
		}
		public Window1()
		{
			BindKey(EditingCommands.Backspace, Key.N, ModifierKeys.Alt);
			BindKey(EditingCommands.Delete, Key.M, ModifierKeys.Alt);
			BindKey(EditingCommands.DeleteNextWord, Key.M, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.DeletePreviousWord, Key.N, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.MoveDownByLine, Key.K, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveDownByPage, Key.K, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.MoveLeftByCharacter, Key.J, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveRightByCharacter, Key.Oem3, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveUpByLine, Key.L, ModifierKeys.Alt);
			BindKey(EditingCommands.MoveLeftByWord, Key.J, ModifierKeys.Alt | ModifierKeys.Control);
			BindKey(EditingCommands.MoveRightByWord, Key.M, ModifierKeys.Alt | ModifierKeys.Control);
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
			textBox.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler(delegate(object sender,ScrollChangedEventArgs e)
			{
				if (Intellisense)
				{
					PositionIntellisense();
				}
			}));
			intellisense.SetValue(FrameworkElement.HeightProperty, 100.0);
			intellisense.SetValue(FrameworkElement.WidthProperty, 200.0);
			intellisense.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
			//intellisense.SetValue(FrameworkElement.MinHeightProperty, 200.0);
			//intellisense.SetValue(FrameworkElement.MinWidthProperty, 200.0);

			textBox.FontFamily = new FontFamily("Courier New");
			textBox.AcceptsTab = true;
			textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
			intellisense.MouseDoubleClick += delegate
			{
				Complete();
			};
			intellisense.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (e.Key == Key.Enter)
				{
					Complete();
				}
			};
			textBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (Intellisense)
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
					{
						if (e.Key == Key.L)
						{
							IntellisenseUp();
							e.Handled = true;
							return;
						}
						else if (e.Key == Key.K)
						{
							IntellisenseDown();
							e.Handled = true;
							return;
						}
					}
					else if (e.Key == Key.Tab)
					{
						if (Intellisense)
						{
							Complete();
							e.Handled = true;
							return;
						}
						//else
						//{
						//    textBox.SelectionLength = 1;
						//    EditingCommands.IncreaseIndentation.Execute(null, textBox);
						//    e.Handled = true; ;
						//}
					}
					else if (e.Key == Key.Up)
					{
						IntellisenseUp();
						e.Handled = true;
						return;
					}
					else if (e.Key == Key.Down)
					{
						IntellisenseDown();
						e.Handled = true;
						return;
					}
					//int index = textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
					//if (index != -1)
					//{
					//    string text=textBox.Text.Substring(index+1, textBox.SelectionStart - index);
					//    foreach (ListViewItem item in intellisense.Items)
					//    {
					//        if (((string)item.Content).StartsWith(text))
					//        {
					//            intellisense.SelectedItem = item;
					//            break;
					//        }
					//    }
					//}
				}
			};
			textBox.KeyUp += delegate(object sender, KeyEventArgs e)
			{
				if (Intellisense)
				{
					if (textBox.SelectionStart <= searchStart && e.Key!=Key.OemPeriod)
					{
						intellisense.Visibility = Visibility.Hidden;
					}
					else
					{
						int index = textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
						if (index != -1)
						{
							string text = textBox.Text.Substring(index + 1, textBox.SelectionStart - index - 1);
							foreach (string item in intellisense.Items)
							{
								if (item.StartsWith(text))
								{
									intellisense.SelectedItem = item;
									break;
								}
							}
						}
					}
				}
			};
			textBox.KeyDown += delegate(object obj, KeyEventArgs e)
			{
				keyboard = e.KeyboardDevice;
				if (e.Key == Key.Return)
				{
					string line = textBox.GetLineText(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart));
					textBox.SelectedText = "\n".PadRight(1 + line.Length - line.TrimStart('\t').Length, '\t');
					textBox.SelectionStart = textBox.SelectionStart + textBox.SelectionLength;
					textBox.SelectionLength = 0;
					textBox.Focus();
				}
				else if (e.Key == Key.Escape)
				{
					if (Intellisense)
					{
						intellisense.Visibility = Visibility.Hidden;
					}
				}
				else if (e.Key == Key.Return && Intellisense)
				{
					Complete();
					e.Handled = true;
				}
				else if (e.Key == Key.OemPeriod)
				{
					search = "";
					string text = textBox.Text.Substring(0, textBox.SelectionStart);
					searchStart = textBox.SelectionStart;
					Interpreter.profiling = false;
					Parser parser = new Parser(text, fileName);
					bool matched;
					Map map = Parser.File.Match(parser, out matched);
					LiteralExpression gac = new LiteralExpression(Gac.gac, null);
					LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
					lib.Statement = new LiteralStatement(gac);
					KeyStatement.intellisense = true;
					map[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
					map[CodeKeys.Function].Compile(lib);
					Source key = new Source(
						parser.Line,
						parser.Column,
						parser.FileName);
					if (Meta.Expression.sources.ContainsKey(key))
					{
						List<Meta.Expression> list = Meta.Expression.sources[key];
						for (int i = 0; i < list.Count; i++)
						{
							if (list[i] is Search)
							{
								PositionIntellisense();
								intellisense.Items.Clear();
								intellisense.Visibility = Visibility.Visible;
								Map s = list[i].EvaluateStructure();
								List<string> keys = new List<string>();
								if (s != null)
								{
									foreach (Map m in s.Keys)
									{
										keys.Add(m.ToString());
									}
								}
								keys.Sort(delegate(string a, string b)
								{
									return a.CompareTo(b);
								});
								foreach (string k in keys)
								{
									intellisense.Items.Add(k);
								}
								if (intellisense.Items.Count != 0)
								{
									intellisense.SelectedIndex = 0;
								}
							}
						}
					}
					else
					{
						MessageBox.Show("no intellisense" + Meta.Expression.sources.Count);
					}
				}
			};
			DockPanel panel = new DockPanel();
			menu.SetValue(DockPanel.DockProperty, Dock.Top);
			textBox.SetValue(DockPanel.DockProperty, Dock.Bottom);

			MenuItem file = new MenuItem();

			MenuItem save = new MenuItem();
			MenuItem run = new MenuItem();
			MenuItem open = new MenuItem();
			run.Header = "Run";
			save.Header = "Save";
			file.Header = "File";
			open.Header = "Open";
			file.Items.Add(open);
			textBox.TextChanged += new TextChangedEventHandler(textBox_TextChanged);
			textBox.TextInput += new TextCompositionEventHandler(textBox_TextInput);
			file.Items.Add(save);
			file.Items.Add(run);
			this.Loaded += delegate
			{
				Open(@"C:\meta\0.2\game.meta");
			};
			open.Click += delegate
			{
				OpenFileDialog dialog = new OpenFileDialog();
				if (dialog.ShowDialog() == true)
				{
					Open(dialog.FileName);
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
			Canvas canvas = new Canvas();
			panel.Children.Add(canvas);
			canvas.Children.Add(textBox);
			Canvas.SetZIndex(intellisense, 100);
			canvas.Background = Brushes.Yellow;
			DockPanel.SetDock(canvas, Dock.Top);
			canvas.SizeChanged += delegate
			{
				textBox.SetValue(FrameworkElement.HeightProperty, canvas.ActualHeight);
				textBox.SetValue(FrameworkElement.WidthProperty, canvas.ActualWidth);
			};
			intellisense.Visibility = Visibility.Hidden;
			canvas.Children.Add(intellisense);
			this.Content = panel;
		}

		void textBox_TextInput(object sender, TextCompositionEventArgs e)
		{
		}

		void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
		}
	}
}