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


namespace IDE {
	public partial class Window1 : System.Windows.Window {
		public static KeyboardDevice keyboard;
		TextBox textBox = new TextBox();
		private void Save() {
			if (fileName == null) {
				SaveFileDialog dialog = new SaveFileDialog();
				if (dialog.ShowDialog() == false) {
					return;
				}
				fileName = dialog.FileName;
			}
			File.WriteAllText(fileName, textBox.Text);
		}
		private string fileName = null;
		private void BindKey(RoutedUICommand command, Key key, ModifierKeys modifiers) {
			command.InputGestures.Add(new KeyGesture(key, modifiers));
		}
		public void Open(string file) {
			fileName = file;
			textBox.Text = File.ReadAllText(fileName);
		}
		public static ListBox intellisense = new ListBox();
		Label status = new Label();

		public static bool Intellisense {
			get {
				return intellisense.Visibility == Visibility.Visible;
			}
		}
		public void IntellisenseUp() {
			if (intellisense.SelectedIndex > 0) {
				intellisense.SelectedIndex--;
			}
		}
		public void IntellisenseDown() {
			if (intellisense.SelectedIndex < intellisense.Items.Count - 1) {
				intellisense.SelectedIndex++;
			}
		}
		public void Complete() {
			if (intellisense.SelectedItem != null) {
				int index = textBox.SelectionStart;
				textBox.SelectionStart = textBox.Text.LastIndexOf('.', textBox.SelectionStart) + 1;
				textBox.SelectionLength = index - textBox.SelectionStart;
				textBox.SelectedText = (string)intellisense.SelectedItem;
				intellisense.Visibility = Visibility.Hidden;
				textBox.SelectionStart += textBox.SelectionLength;
				textBox.SelectionLength = 0;
			}
		}
		int searchStart = 0;
		public void PositionIntellisense() {
			Rect r = textBox.GetRectFromCharacterIndex(textBox.SelectionStart);
			Canvas.SetLeft(intellisense, r.Right);
			Canvas.SetTop(intellisense, r.Bottom);
		}
		public Window1() {
			BindKey(ApplicationCommands.Find, Key.F, ModifierKeys.Control);
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
			intellisense.MaxHeight = 100;
			intellisense.Width = 200;
			intellisense.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);

			textBox.FontFamily = new FontFamily("Courier New");
			textBox.AcceptsTab = true;
			intellisense.MouseDoubleClick += delegate {
				Complete();
			};
			textBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
				if (Intellisense) {
					e.Handled = true;
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
						if (e.Key == Key.L) {
							IntellisenseUp();
						}
						else if (e.Key == Key.K) {
							IntellisenseDown();
						}
						else {
							e.Handled = false;
						}
					}
					else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
						if (e.Key == Key.L) {
							EditingCommands.MoveToLineStart.Execute(null, textBox);
							EditingCommands.SelectToLineEnd.Execute(null, textBox);
							EditingCommands.Delete.Execute(null, textBox);
						}
						else if (e.Key == Key.Space) {
						}
						else {
							e.Handled = false;
						}
					}
					else if (e.Key == Key.Return) {
						Complete();
					}
					else if (e.Key == Key.Tab) {
						Complete();
					}
					else if (e.Key == Key.Up) {
						IntellisenseUp();
					}
					else if (e.Key == Key.Down) {
						IntellisenseDown();
					}
					else {
						e.Handled = false;
					}
				}
			};;
			textBox.KeyDown += delegate(object obj, KeyEventArgs e) {
				if (e.Key == Key.Return) {
					string line = textBox.GetLineText(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart));
					textBox.SelectedText = "\n".PadRight(1 + line.Length - line.TrimStart('\t').Length, '\t');
					textBox.SelectionStart = textBox.SelectionStart + textBox.SelectionLength;
					textBox.SelectionLength = 0;
					textBox.Focus();
				}
				else if (e.Key == Key.Escape) {
					if (Intellisense) {
						intellisense.Visibility = Visibility.Hidden;
					}
				}
				else if (e.Key == Key.OemPeriod) {
					string text = textBox.Text.Substring(0, textBox.SelectionStart);
					searchStart = textBox.SelectionStart;
					Interpreter.profiling = false;
					foreach (Parser.CachedRule rule in Parser.CachedRule.cachedRules) {
						rule.cached.Clear();
					}
					Parser parser = new Parser(text, fileName);
					Map map = null;
					bool matched = Parser.File.Match(parser, ref map);
					LiteralExpression gac = new LiteralExpression(Gac.gac, null);
					LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
					lib.Statement = new LiteralStatement(gac);
					KeyStatement.intellisense = true;
					map[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
					map[CodeKeys.Function].Compile(lib);
					Source key = new Source(
						parser.State.Line,
						parser.State.Column,
						parser.FileName);
					intellisense.Items.Clear();
					if (Meta.Expression.sources.ContainsKey(key)) {
						List<Meta.Expression> list = Meta.Expression.sources[key];
						for (int i = 0; i < list.Count; i++) {
							if (list[i] is Search || list[i] is Select) {
								PositionIntellisense();
								intellisense.Items.Clear();
								Structure s = list[i].EvaluateStructure();
								List<string> keys = new List<string>();
								if (s != null) {
									foreach (Map m in ((LiteralStructure)s).Literal.Keys) {
										keys.Add(m.ToString());
									}
								}
								keys.Sort(delegate(string a, string b) {
									return a.CompareTo(b);
								});
								if (keys.Count != 0) {
									intellisense.Visibility = Visibility.Visible;
								}
								foreach (string k in keys) {
									intellisense.Items.Add(k);
								}
								if (intellisense.Items.Count != 0) {
									intellisense.SelectedIndex = 0;
								}
							}
						}
					}
					else {
						MessageBox.Show("no intellisense" + Meta.Expression.sources.Count);
					}
					Meta.Expression.sources.Clear();
				}
			};
			DockPanel dockPanel = new DockPanel();

			Menu menu = new Menu();
			DockPanel.SetDock(menu, Dock.Top);
			MenuItem file = new MenuItem();
			MenuItem save = new MenuItem();
			MenuItem run = new MenuItem();
			MenuItem open = new MenuItem();
			file.Header = "File";
			open.Header = "Open";
			save.Header = "Save";
			run.Header = "Run";
			open.Click += delegate {
				OpenFileDialog dialog = new OpenFileDialog();
				if (dialog.ShowDialog() == true) {
					Open(dialog.FileName);
				}
			};
			run.Click += delegate {
				Save();
				Process.Start(System.IO.Path.Combine(@"C:\Meta\0.2\", @"bin\Debug\Meta.exe"), fileName);
			};
			save.Click += delegate {
				Save();
			};
			DockPanel.SetDock(status, Dock.Bottom);
			dockPanel.Children.Add(status);

			file.Items.Add(open);
			file.Items.Add(save);
			file.Items.Add(run);
			menu.Items.Add(file);
			dockPanel.Children.Add(menu);


			DockPanel.SetDock(textBox,Dock.Bottom);
			textBox.TextChanged += delegate {
				if (Intellisense) {
					if (textBox.SelectionStart <= searchStart) {
						intellisense.Visibility = Visibility.Hidden;
					}
					else {
						int index = textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
						if (index != -1) {
							string text = textBox.Text.Substring(index + 1, textBox.SelectionStart - index - 1);
							foreach (string item in intellisense.Items) {
								if (item.StartsWith(text, StringComparison.OrdinalIgnoreCase)) {
									intellisense.SelectedItem = item;
									intellisense.ScrollIntoView(intellisense.SelectedItem);
									break;
								}
							}
						}
					}
				}
			};
			textBox.SelectionChanged += delegate {
				status.Content = "Ln " +
					(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1);
			};
			bool changing = false;
			textBox.SelectionChanged += delegate {
				if (!changing) {
					changing = true;
					int start = textBox.SelectionStart;
					textBox.ScrollToVerticalOffset(Math.Max(
						0,
						16 * textBox.GetLineIndexFromCharacterIndex(
						textBox.SelectionStart) - 100));
					int end = textBox.Text.LastIndexOf('\n', textBox.SelectionStart);
					if (end == -1) {
						end = 0;
					}
					int column = textBox.SelectionStart - end;
					string text = textBox.Text.Substring(end, column);
					int tabs = text.Length - text.Replace("\t", "").Length;
					int length = tabs * 3 + column;
					textBox.ScrollToHorizontalOffset(Math.Max(
						-50,
						length * 7 - 50));
					textBox.SelectionStart = start;
				}
				changing = false;
			};

			//Canvas canvas = new Canvas();
			//canvas.Children.Add(textBox);
			//canvas.Background = Brushes.Yellow;
			//DockPanel.SetDock(canvas, Dock.Top);
			//canvas.Children.Add(intellisense);
			//dockPanel.Children.Add(canvas);
			dockPanel.Children.Add(textBox);


			//textBox.SizeChanged += delegate {
			//    canvas.Width = textBox.Width;
			//    canvas.Height = textBox.Height;
			//};


			intellisense.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
				if (intellisense.SelectedItem != null) {
					intellisense.ScrollIntoView(intellisense.SelectedItem);
				}
			};
			intellisense.Visibility = Visibility.Hidden;
			Canvas.SetZIndex(intellisense, 100);





			this.Content = dockPanel;

			this.Loaded += delegate {
				Open(@"C:\meta\0.2\game.meta");
				textBox.Focus();
			};
		}
	}
}