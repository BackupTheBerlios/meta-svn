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
using System.Reflection;
using System.Collections;
using System.Xml;
using System.Runtime.InteropServices;
using NetMatters;
using System.Timers;
using System.Windows.Threading;
using System.Threading;
//using System.Windows.Forms;


public partial class Editor : System.Windows.Window {
	TextBox textBox = new TextBox();
	Canvas canvas = new Canvas();
	private void Save() {
		if (fileName == null) {
			SaveFileDialog dialog = new SaveFileDialog();
			if (dialog.ShowDialog() == true) {
				fileName = dialog.FileName;
			}
			else {
				return;
			}
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
	public static TextBox toolTip = new TextBox();
	public static ListBox intellisense = new ListBox();
	Label editorLine = new Label();
	Label message = new Label();

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
			textBox.SelectionStart = searchStart + 1;
			textBox.SelectionLength = index - textBox.SelectionStart;
			textBox.SelectedText = ((Item)intellisense.SelectedItem).ToString();
			intellisense.Visibility = Visibility.Hidden;
			toolTip.Visibility = Visibility.Hidden;
			textBox.SelectionStart += textBox.SelectionLength;
			textBox.SelectionLength = 0;
		}
	}
	int searchStart = 0;
	public void PositionIntellisense() {
		Rect r = textBox.GetRectFromCharacterIndex(textBox.SelectionStart);
		Canvas.SetLeft(intellisense, r.Right);
		Canvas.SetTop(intellisense, r.Bottom);
		Canvas.SetLeft(toolTip, r.Right);
		Canvas.SetTop(toolTip, r.Bottom + 110);
	}
	ScrollViewer scrollViewer = new ScrollViewer();
	public void SetBreakpoint() {

	}
	public void FindMatchingBrace() {
		const string openBraces = "({[<";
		const string closeBraces = ")}]>";
		bool direction = true;
		if (!MatchingBrace(openBraces, closeBraces, true)) {
			MatchingBrace(closeBraces, openBraces, false);
		}
	}
	public class IterativeSearch {
		public void Back() {
			text = text.Substring(0, text.Length - 1);
			Find(!direction);
		}
		public void OnKeyDown(TextCompositionEventArgs e) {
			text+=e.Text;
			Find(direction);
			e.Handled = true;
		}
		public void Find(bool forward) {
			if (text.Length > 0) {
				if (forward) {
					index = textBox.Text.ToLower().IndexOf(text, index);
				}
				else {
					index = textBox.Text.ToLower().LastIndexOf(text, index);
				}
				if (index != -1) {
					textBox.Select(index, text.Length);
				}
				else {
					editor.StopIterativeSearch();
				}
			}
		}
		private int index;
		private string text="";
		private int start;
		private TextBox textBox;
		private bool direction;
		public IterativeSearch(TextBox textBox,bool direction) {
			this.direction = direction;
			this.textBox = textBox;
			textBox.Cursor = Cursors.ScrollS;
			start = textBox.SelectionStart;
			index = start;
		}
	}
	public IterativeSearch iterativeSearch;
	public void StartIterativeSearch() {
		if (iterativeSearch == null) {
			iterativeSearch = new IterativeSearch(textBox,true);
		}
		else {
			StopIterativeSearch();
		}
	}
	public void StopIterativeSearch() {
		textBox.Cursor = Cursors.IBeam;
		iterativeSearch = null;
	}
	public class Item{
		public string Signature() {
			if (original != null) {
				XmlComments comments = new XmlComments(original);
				XmlNode node=comments.Summary;
				foreach(XmlNode n in node.ChildNodes) {
					if (n.Name == "see") {
						n.InnerText = n.Attributes["cref"].Value.Substring(2);
					}
				}
				return node.InnerText;
			}
			return "";
		}
		private string text;
		private MemberInfo original;
		public Item(string text, MemberInfo original) {
			this.text = text;
			this.original = original;
		}
		public override string ToString() {
			return text;
		}
	}
	public bool Compile() {
		if (Compile(textBox.Text)) {
			message.Content = "";
			return true;
		}
		else {
			message.Content = "Parse error";
			return false;
		}
	}
	public bool Compile(string text) {
		try {
			//string text = textBox.Text;
			Interpreter.profiling = false;
			foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
				cached.Clear();
			}

			Parser parser = new Parser(text, fileName);
			Map map = null;
			bool matched = Parser.Value.Match(parser, ref map);
			Dispatcher.Invoke(DispatcherPriority.Normal,new System.Windows.Forms.MethodInvoker(delegate {
				foreach (Rectangle line in errors) {
					canvas.Children.Remove(line);
				}
				errors.Clear();
			}));
			if (parser.state.index != text.Length) {
				Dispatcher.Invoke(DispatcherPriority.Normal, new System.Windows.Forms.MethodInvoker(delegate {
					Rect r = textBox.GetRectFromCharacterIndex(parser.state.index);
					Rectangle line = new Rectangle();
					errors.Add(line);
					line.Width = 10;
					line.Height = 3;
					line.Fill = Brushes.Red;
					Canvas.SetTop(line, r.Bottom);
					Canvas.SetLeft(line, r.Right);
					canvas.Children.Add(line);
				}));
				return false;
			}
			return true;
		}
		catch (Exception e) {
			throw e;
		}
	}
	public static List<Rectangle> errors = new List<Rectangle>();
	public static Editor editor;
	public static FindAndReplace findAndReplace;
	public static List<Item> intellisenseItems = new List<Item>();
	public Editor() {
		findAndReplace=new FindAndReplace(textBox);
		this.WindowState = WindowState.Maximized;
		editor = this;
		RoutedUICommand deleteLine = new RoutedUICommand();
		BindKey(deleteLine, Key.L, ModifierKeys.Control);
		this.CommandBindings.Add(new CommandBinding(deleteLine, delegate {
			int realStart = Math.Min(textBox.SelectionStart, textBox.Text.Length - 1);
			int start = Math.Max(0, textBox.Text.LastIndexOf('\n', realStart));
			int end = textBox.Text.IndexOf('\n', realStart);
			if (end == -1) {
				end = textBox.Text.Length-1;
			}
			textBox.SelectionStart = start;
			textBox.SelectionLength = end - start;
			textBox.SelectedText = "";
		}));
		this.CommandBindings.Add(
			new CommandBinding(
				ApplicationCommands.Find,
				delegate {
					findAndReplace.ShowDialog();
				}));

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
		textBox.SpellCheck.IsEnabled = true;
		intellisense.MaxHeight = 100;
		toolTip.Text = "Tooltip!!!!";
		toolTip.Width = 300;
		Label l;
		toolTip.TextWrapping = TextWrapping.Wrap;
		intellisense.SelectionChanged += delegate {
			if (intellisense.SelectedItem != null) {
				toolTip.Text = ((Item)intellisense.SelectedItem).Signature();
			}
		};
		intellisense.Width = 300;
		intellisense.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, delegate { Save(); }));
		textBox.FontFamily = new FontFamily("Courier New");
		textBox.AcceptsTab = true;
		//intellisense.MouseDoubleClick += delegate {
		//    Complete();
		//};
		textBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
			if (Intellisense) {
				e.Handled = true;
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
					if (e.SystemKey == Key.L) {
						IntellisenseUp();
					}
					else if (e.SystemKey == Key.K) {
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
			else {
				if (iterativeSearch != null) {
					if (e.Key == Key.Back) {
						editor.iterativeSearch.Back();
						e.Handled = true;
					}
					if (e.Key == Key.Escape) {
						editor.StopIterativeSearch();
						e.Handled = true;

					}
				}
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
					if (e.Key == Key.I) {
						StartIterativeSearch();
						e.Handled = true;
					}
					else if (e.Key == Key.Space) {
						e.Handled = true;
						StartIntellisense();
						searchStart--;
						intellisense.Items.Clear();
						//List<Expression> list=new List<Meta.Expression>();
						List<Source> sources=new List<Source>(Meta.Expression.sources.Keys);
						sources.RemoveAll(delegate (Source source){
							return source.FileName!=fileName;
						});
						sources.Sort(delegate(Source a,Source b) {
							return a.CompareTo(b);
						});
						//Map s=null;
						sources.Reverse();
						Program start = null;
						foreach(Source source in sources) {
							foreach(Meta.Expression expression in Meta.Expression.sources[source]) {
								Program program=expression as Program;
								if(program!=null) {
									start = program;//.statementList[program.statementList.Count - 1].CurrentMap();
									break;
								}
							}
							if(start!=null) {
								break;
							}
						}
						Map s=Map.Empty;
						if (start != null) {
							Meta.Expression x = start;
							while (x!=null) {
								if (x is Program) {
									Program p = x as Program;
									Map result=p.statementList[p.statementList.Count - 1].CurrentMap();
									if (p is Function) {
										result = p.statementList[p.statementList.Count - 2].CurrentMap();
									}
									if (result != null) {
										s = Library.Merge(result,s);
									}
								}
								else if (x is LiteralExpression) {
									LiteralExpression literal = (LiteralExpression)x;
									Map structure=literal.GetStructure();

									//if (!(structure is Gac)) {
										s = Library.Merge(structure, s);
									//}
								}
								x = x.Parent;
							}
						}
						Map directory=new DirectoryMap(System.IO.Path.GetDirectoryName(fileName));
						s=Library.Merge(directory.Copy(),s);
			            List<string> keys = new List<string>();
			            if (s != null) {
			                foreach (Map m in s.Keys) {
			                    keys.Add(m.ToString());
			                }
			            }
			            keys.Sort(delegate(string a, string b) {
			                return a.CompareTo(b);
			            });
			            if (keys.Count != 0) {
							intellisense.Visibility = Visibility.Visible;
			                toolTip.Visibility = Visibility.Visible;
			            }
			            intellisenseItems.Clear();
			            intellisense.Items.Clear();
			            foreach (string k in keys) {
			                MethodBase m = null;
			                MemberInfo original = null;
			                if (s.ContainsKey(k)) {
			                    Map value = s[k];
			                    Method method = value as Method;
			                    if (method != null) {
			                        m = method.method;
			                        original = method.original;
			                    }
			                    TypeMap typeMap = value as TypeMap;
			                    if (typeMap != null) {
			                        original = typeMap.Type;
			                    }
			                }
			                intellisenseItems.Add(new Item(k, original));
			            }
			            if (intellisense.Items.Count != 0) {
			                intellisense.SelectedIndex = 0;
			            }
						PositionIntellisense();
						Meta.Expression.sources.Clear();
					}
				}
				else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
					if (e.SystemKey == Key.I) {
						FindMatchingBrace();
						e.Handled = true;
					}
					else if (e.SystemKey == Key.H) {
						SetBreakpoint();
						e.Handled = true;
					}
				}
				else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift)) {
					if (e.SystemKey == Key.I) {
						int start = textBox.SelectionStart;
						FindMatchingBrace();
						textBox.Select(Math.Min(start, textBox.SelectionStart), Math.Abs(start - textBox.SelectionStart));
						e.Handled = true;
					}
				}
			}
		};
		textBox.PreviewTextInput += new TextCompositionEventHandler(textBox_PreviewTextInput);
		textBox.KeyDown += delegate(object obj, KeyEventArgs e) {
			//if (e.Key != Key.LeftShift) {
			//}
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
					toolTip.Visibility = Visibility.Hidden;
				}
			}
			else if (e.Key == Key.D8 || e.Key==Key.OemComma) {
				StartIntellisense();
				int index = textBox.SelectionStart;
				string text=textBox.Text;
				int open = 1;
				int argIndex = 0;
				Source realSource=null;
				if (e.Key == Key.D8) {
					open = 0;
				}
				if (e.Key == Key.OemComma) {
					argIndex++;
				}
				while (index>=0) {
					char c = text[index];
					switch (c) {
						case '(':
							open--;
							break;
						case ')':
							open++;
							break;
						case ',':
							if (open == 1) {
								argIndex++;
							}
							break;
					}
					if (open==0) {
						int line=textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart)+1;
						int selection=textBox.SelectionStart;
						int column=selection-text.LastIndexOf('\n',selection-1)+1;
						realSource = new Source(line, column, fileName);
						break;
					}
					index--;
				}
				if (realSource != null) {
					List<Source> sources = new List<Source>(Meta.Expression.sources.Keys);
					sources.RemoveAll(delegate(Source s) {
						return s.FileName != fileName;
					});
					sources.Sort(delegate(Source a, Source b) {
						return a.CompareTo(b);
					});
					//Map s=null;
					sources.Reverse();
					Meta.Expression start = null;
					foreach (Source source in sources) {
						if (source.Line <= realSource.Line && source.Column <= realSource.Column) {
							foreach (Meta.Expression expression in Meta.Expression.sources[source]) {
								//Program program = expression as Program;
								if (expression is Select || expression is Search) {
									start = expression;
									if (start.Parent is Call) {
										start = ((Call)start.Parent).calls[0];
									}
									//if (start is Search && start.Parent is Select) {
									//    start = start.Parent;
									//}
									//start = ((Call)expression).calls[0];//.statementList[program.statementList.Count - 1].CurrentMap();
								//if (expression is Call) {
								//    start = ((Call)expression).calls[0];//.statementList[program.statementList.Count - 1].CurrentMap();
									break;
								}
							}
						}
						if (start != null) {
							break;
						}
					}
					if (start != null) {
						Map structure=start.GetStructure();
						if (structure != null) {
							if (structure is Method) {
								XmlNodeList parameters=new XmlComments(((Method)structure).method).Params;
								if (parameters.Count > argIndex) {
									XmlNode node=parameters[argIndex];
									
									string t = node.Attributes["name"].Value+":\n"+node.InnerText;
									PositionIntellisense();
									toolTip.Visibility = Visibility.Visible;
									toolTip.Text = t;
								}
								//string text = new XmlComments(((Method)structure).method).ToString();

							}
						}
					}
				}
			}
			else if (e.Key == Key.OemPeriod) {
				Source key = StartIntellisense();
				intellisense.Items.Clear();
				if (Meta.Expression.sources.ContainsKey(key)) {
					List<Meta.Expression> list = Meta.Expression.sources[key];
					for (int i = 0; i < list.Count; i++) {
						if (list[i] is Search || list[i] is Select || list[i] is Call) {
							intellisense.Items.Clear();
							Map s = list[i].EvaluateStructure();
							List<string> keys = new List<string>();
							if (s != null) {
								foreach (Map m in s.Keys) {
									keys.Add(m.ToString());
								}
							}
							keys.Sort(delegate(string a, string b) {
								return a.CompareTo(b);
							});
							if (keys.Count != 0) {
								intellisense.Visibility = Visibility.Visible;
								toolTip.Visibility = Visibility.Visible;
							}
							intellisenseItems.Clear();
							intellisense.Items.Clear();
							foreach (string k in keys) {
								MethodBase m = null;
								MemberInfo original = null;
								if (s.ContainsKey(k)) {
									Map value = s[k];
									Method method = value as Method;
									if (method != null) {
										m = method.method;
										original = method.original;
									}
									TypeMap typeMap = value as TypeMap;
									if (typeMap != null) {
										original = typeMap.Type;
									}
								}
								intellisenseItems.Add(new Item(k, original));
							}
							if (intellisense.Items.Count != 0) {
								intellisense.SelectedIndex = 0;
							}
							PositionIntellisense();
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
		MenuItem openItem = new MenuItem();
		MenuItem comp = new MenuItem();
		RoutedUICommand execute = new RoutedUICommand("Run","Run",GetType());
		RoutedUICommand compile = new RoutedUICommand("Compile", "Compile", GetType());
		file.Header = "File";
		openItem.Header = "Open";
		comp.Header = "Compile";
		comp.Command = compile;
		openItem.Command=ApplicationCommands.Open;
		save.Header = "Save";
		save.Command = ApplicationCommands.Save;
		run.Header = "Run";
		run.Command = execute;
		CommandBindings.Add(
			new CommandBinding(compile,delegate{Compile();})
		);
		DispatcherTimer timer = new DispatcherTimer();
		timer.Interval = new TimeSpan(10000);
		timer.Tick += delegate {
			string text = textBox.Text;
			Thread thread = new Thread(new ThreadStart(delegate {
				Compile(text);
			}));
			thread.Start();
		};
		timer.Start();
		CommandBindings.Add(new CommandBinding(execute, delegate {
			Save();
			if (Compile()) {
				Process.Start(System.IO.Path.Combine(@"D:\Meta\", @"bin\Debug\Meta.exe"), fileName);
			}
		}));
		BindKey(compile, Key.F3, ModifierKeys.None);
		BindKey(execute, Key.F5, ModifierKeys.None);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, delegate { Open(); }));
		StackPanel status = new StackPanel();
		status.Orientation = Orientation.Horizontal;
		status.Children.Add(editorLine);

		status.Children.Add(message);
		DockPanel.SetDock(status, Dock.Bottom);
		//DockPanel.SetDock(editorLine, Dock.Bottom);
		dockPanel.Children.Add(status);

		file.Items.Add(openItem);
		file.Items.Add(comp);
		file.Items.Add(save);
		file.Items.Add(run);
		menu.Items.Add(file);
		dockPanel.Children.Add(menu);

		DockPanel.SetDock(textBox, Dock.Bottom);
		textBox.TextChanged += delegate {
			if(Intellisense) {
				if (textBox.SelectionStart <= searchStart) {
					intellisense.Visibility = Visibility.Hidden;
					toolTip.Visibility = Visibility.Hidden;
				}
				else {
					int index = searchStart;
					if (index != -1) {
						string text = textBox.Text.Substring(index + 1, textBox.SelectionStart - index - 1);
						intellisense.Items.Clear();
						foreach (Item item in intellisenseItems) {
							if (item.ToString().ToLower().Contains(text.ToLower())) {
								intellisense.Items.Add(item);
							}
						}
						intellisense.SelectedIndex = 0;
					}
				}
			}
		};
		textBox.SelectionChanged += delegate {
			int line=(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1);
			editorLine.Content = "Ln " + line;
			textBox.ScrollToLine(line-1);
		};
		canvas.Children.Add(textBox);
		canvas.Background = Brushes.Yellow;
		DockPanel.SetDock(canvas, Dock.Top);
		scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.Content = canvas;
		textBox.Padding = new Thickness(100,0,0,200);
		dockPanel.Children.Add(scrollViewer);
		const int width = 0;
		const int height = 0;
		Canvas.SetLeft(textBox, width/2);
		Canvas.SetTop(textBox, height/2);
		textBox.SizeChanged += delegate {
			canvas.Width = textBox.ActualWidth + width;
			canvas.Height = textBox.ActualHeight + height;
		};
		intellisense.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
			if (intellisense.SelectedItem != null) {
				intellisense.ScrollIntoView(intellisense.SelectedItem);
			}
		};
		intellisense.Visibility = Visibility.Hidden;
		toolTip.Visibility = Visibility.Hidden;
		Canvas.SetZIndex(intellisense, 100);
		canvas.Children.Add(intellisense);
		canvas.Children.Add(toolTip);
		this.Content = dockPanel;
		this.Loaded += delegate {
			Open(@"D:\meta\mail.meta");
			textBox.Focus();
		};
	}
	private Source StartIntellisense() {
		string text = textBox.Text.Substring(0, textBox.SelectionStart);
		searchStart = textBox.SelectionStart;
		Interpreter.profiling = false;
		foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
			cached.Clear();
		}

		Parser parser = new Parser(text, fileName);
		Map map = null;
		bool matched = Parser.Value.Match(parser, ref map);
		LiteralExpression gac = new LiteralExpression(Gac.gac, null);
		LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
		lib.Statement = new LiteralStatement(gac);
		KeyStatement.intellisense = true;
		map[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
		map[CodeKeys.Function].Compile(lib);
		Source key = new Source(
			parser.state.Line,
			parser.state.Column,
			parser.state.FileName
		);
		return key;
	}
	public void Open() {
		OpenFileDialog dialog = new OpenFileDialog();
		if (dialog.ShowDialog() == true) {
			Open(dialog.FileName);
		}
	}
	void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
		if (iterativeSearch != null) {
			iterativeSearch.OnKeyDown(e);
		}
	}
	private bool MatchingBrace(string openBraces, string closeBraces, bool direction) {
		//return false;
		char previous = textBox.Text[textBox.SelectionStart - 1];
		char next = textBox.Text[textBox.SelectionStart];
		int forward = openBraces.IndexOf(previous);
		int backward = openBraces.IndexOf(next);
		if (forward != -1 || backward != -1) {
			char brace;
			int index;
			if (forward != -1) {
				index = forward;
				brace = openBraces[forward];
			}
			else if (backward != -1) {
				index = backward;
				brace = openBraces[backward];
			}
			else {
				return false;
			}
			char closingBrace = closeBraces[index];
			int pos;
			if (direction) {
				pos = textBox.SelectionStart + 2;
			}
			else {
				pos = textBox.SelectionStart - 2;
			}
			int braces = 0;
			while (true) {
				if (direction) {
					pos++;
				}
				else {
					pos--;
				}
				if (pos <= 0 || pos >= textBox.Text.Length) {
					break;
				}
				char c = textBox.Text[pos];
				if (c == closingBrace) {
					braces--;
				}
				else if (c == brace) {
					braces++;
				}
				if (braces < 0) {
					textBox.SelectionStart = pos;
					return true;
				}
			}
		}
		return false;
	}
}