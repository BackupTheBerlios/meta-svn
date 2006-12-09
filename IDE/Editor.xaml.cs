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
using System.Globalization;
using _treeListView;
using System.Windows.Media;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;



public delegate void MethodInvoker();
public class Settings {
	public static string lastFile;
}
public partial class Editor : System.Windows.Window {
	public class Box : TextBox {
		public int Line {
			get {
				return GetLineIndexFromCharacterIndex(SelectionStart);
			}
		}
		public int Column {
			get {
				if (Text.Length == 0) {
					return 0;
				}
				else {
					return SelectionStart - Text.LastIndexOf('\n', Math.Max(0, SelectionStart - 1))-1;
				}
			}
			set {
				SelectionStart = textBox.Text.LastIndexOf('\n', SelectionStart)+1 + value;
			}
		}
	}
	static Box textBox = new Box();
	static Canvas canvas = new Canvas();
	public class View : TreeListView {
	}
	public static TreeListView watch = new View();
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
		Settings.lastFile = fileName;
		this.Title = System.IO.Path.GetFileNameWithoutExtension(fileName) + " - " + ProgramName;
		SaveSettings();
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
	const string openBraces = "({[<";
	const string closeBraces = ")}]>";
	public int FindMatchingBrace() {
		bool direction = true;
		
		int index=FindMatchingBrace(openBraces, closeBraces, true);
		if(index==-1) {
			index=FindMatchingBrace(closeBraces, openBraces, false);
		}
		return index;
		//if (!FindMatchingBrace(openBraces, closeBraces, true)) {
		//    FindMatchingBrace(closeBraces, openBraces, false);
		//}
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
					index = textBox.Text.ToLower().LastIndexOf(text, index+text.Length);
				}
				if (index != -1) {
					textBox.Select(index, text.Length);
				}
				else {
					editor.StopIterativeSearch();
				}
			}
		}
		public int index;
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
	public void StartIterativeSearch(bool forward) {
		if (iterativeSearch == null) {
			iterativeSearch = new IterativeSearch(textBox,forward);
		}
		else {
			if (forward) {
				iterativeSearch.index++;
			}
			else {
				iterativeSearch.index-=2;
			}
			iterativeSearch.Find(forward);
		}
	}
	public void StopIterativeSearch() {
		textBox.Cursor = Cursors.IBeam;
		iterativeSearch = null;
	}
	public class PreviewItem:Item {
		public override Control GetPreview() {
			FileMap fileMap = map as FileMap;
			if (fileMap!=null) {
				string extension = System.IO.Path.GetExtension(fileMap.path);
				switch (extension) {
					case ".bmp":
					case ".ico":
					case ".png":
					case ".jpg";
					case ".jpeg":
					case ".gif":
					case ".tiff":
						Image image=new Image();
						image.Source = new BitmapImage(new Uri(fileMap.path));
						Label label = new Label();
						label.Content = image;
						return label;
				}
			}
			return null;
		}
		private Map map;
		private Map key;
		public PreviewItem(Map key,Map map):base("",null) {
			this.map = map;
			this.key=key;
		}
		public override string ToString() {
			return key.ToString();
		}
		public override string Signature() {
			if (map is FileMap) {
				return ((FileMap)map).path;
			}
			else if (map is DirectoryMap) {
				return ((DirectoryMap)map).path;
			}
			else {
				return map.ToString();
			}
		}
	}
	public class Item {
		public virtual Control GetPreview() {
			return null;
		}
		public virtual string Signature() {
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
	public static string DocumentationPath {
		get {
			return System.IO.Path.Combine(Interpreter.InstallationPath, "documentation.meta");
		}
	}
	public class MetaItem:Item {
		public static Map documentation = Parser.Parse(DocumentationPath);
		private Map key;
		public MetaItem(Map name):base(name.ToString(),null) {
			this.key = name;
		}
		public override string Signature() {
			if(documentation.ContainsKey(key)) {
				return documentation[key].ToString();
			}
			return "";
		}
	}
	public bool Compile(bool automatic) {
		if (Compile(textBox.Text,automatic)) {
			message.Content = "";
			return true;
		}
		else {
			message.Content = "Parse error";
			return false;
		}
	}
	public static void HideWatch() {
		watch.Visibility = Visibility.Collapsed;
	}

	public static void HideErrors() {
		errorList.Visibility = Visibility.Collapsed;
	}

	public bool Compile(string text,bool automatic) {
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
		foreach (Error error in parser.state.Errors) {
			Dispatcher.Invoke(DispatcherPriority.Normal, new System.Windows.Forms.MethodInvoker(delegate {
				MakeLine(error.State.index,error.Text);
			}));
		}
		if (parser.state.index != text.Length) {
			Dispatcher.Invoke(DispatcherPriority.Normal, new System.Windows.Forms.MethodInvoker(delegate {
				MakeLine(parser.state.index, "Expected end of file.");
			}));
			return false;
		}
		if (!automatic) {
			if (parser.state.Errors.Length != 0) {
				Dispatcher.Invoke(DispatcherPriority.Normal, new MethodInvoker(delegate {
					errorList.Visibility = Visibility.Visible;
					errorList.Items.Clear();
					foreach (Error error in parser.state.Errors) {
						ListViewItem item = new ListViewItem();
						item.Content = error.Text;
						Error e = error;
						item.Selected += delegate {
							textBox.SelectionStart = e.State.index;
							Keyboard.Focus(textBox);
						};
						errorList.Items.Add(item);
						//errorList.Items.Add(error.Text);
					}
				}));
			}
			else {
				HideErrors();
			}
		}
		return true;
	}

	private void MakeLine(int index,string text) {
		Rect r = textBox.GetRectFromCharacterIndex(index);
		Rectangle line = new Rectangle();
		errors.Add(line);
		line.Width = 10;
		line.Height = 3;
		line.Fill = Brushes.Red;
		Label label = new Label();
		label.Content = text;
		label.FontFamily = font;
		label.Background = Brushes.LightYellow;
		Canvas.SetTop(label, r.Bottom);
		Canvas.SetLeft(label, r.Right);
		label.Visibility = Visibility.Hidden;

		line.MouseEnter += delegate {
			Canvas.SetTop(label, r.Bottom);
			Canvas.SetLeft(label, r.Right);
			label.Visibility = Visibility.Visible;
		};
		line.MouseLeave += delegate {
			label.Visibility = Visibility.Hidden;
		};
		Canvas.SetTop(line, r.Bottom);
		Canvas.SetLeft(line, r.Right);
		canvas.Children.Add(label);
		canvas.Children.Add(line);
	}
	public static List<Rectangle> errors = new List<Rectangle>();
	public static Editor editor;
	public static FindAndReplace findAndReplace;
	public class Breakpoint {
		public int line;
		public int column;
		public Breakpoint(int line,int column) {
			this.line = line;
			this.column = column;
			Ellipse point=new Ellipse();
			point.Width=10;
			point.Height=10;
			point.Fill=Brushes.Red;
			Rect r=textBox.GetRectFromCharacterIndex(textBox.SelectionStart);
			point.Opacity = 0.5;
			Canvas.SetLeft(point,r.Left);
			Canvas.SetTop(point, r.Top);
			canvas.Children.Add(point);
		}
	}
	public static List<Item> intellisenseItems = new List<Item>();
	public static List<Breakpoint> breakpoints = new List<Breakpoint>();


	public class SubItem : TreeListViewItem {
		public void MakeSureComputed() {
			SetMap(map,true);
		}
		private StackPanel panel = new StackPanel();
		private Label key = new Label();
		private Label value = new Label();
		private Map map;
		public SubItem(Map key,Map value,bool expand) {
			this.key.Content = key.ToString();
			Items.Clear();
			this.map = value;
			if (expand) {
				SetMap(value,false);
			}
			bool wasExpanded = false;
			this.Expanded += delegate {
				if (!wasExpanded) {
					wasExpanded = true;
					MakeSureComputed();
				}
			};
			panel.Orientation = Orientation.Horizontal;
			panel.Children.Add(this.key);
			panel.Children.Add(this.value);
			this.Header = panel;
		}
		public void SetMap(Map map,bool expand) {
			value.Content = "";
			Items.Clear();
			if (map.IsString) {
				value.Content = map.GetString();
			}
			else if (map.IsNumber) {
				this.value.Content = map.GetNumber().ToString();
			}
			else {
				foreach (KeyValuePair<Map, Map> entry in map) {
					Items.Add(new SubItem(entry.Key, entry.Value, expand));
				}
			}
		}
	}
	public static Map debuggingContext;
	public static ListView errorList = new ListView();
	public class MyItem : TreeListViewItem {
		private StackPanel panel = new StackPanel();
		private TextBox text = new TextBox();
		private Label label = new Label();
		private Map value;
		public MyItem() {
			panel.Orientation = Orientation.Horizontal;
			panel.Children.Add(text);
			panel.Children.Add(label);
			text.TabIndex = 0;
			bool fresh = true;
			text.TextChanged += delegate {
				if (fresh) {
					fresh = false;
					watch.Items.Add(new MyItem());
				}
				if (debuggingContext != null) {
					Update(debuggingContext);
				}
			};
			bool wasExpanded = false;
			this.Header = panel;
		}

		public MyItem(Map key, Map value):this() {
			text.Text = key.ToString();
			SetMap(value,false);
		}
		private bool expanded = false;
		public void Update(Map context) {
			Parser parser = new Parser(text.Text, "watch window");
			Map expression=null;
			// should simply exist in parser instance!!!!
			foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
				cached.Clear();
			}
			if(Parser.Expression.Match(parser, ref expression)) {
				try {
					Map result = expression.GetExpression().Compile()(context);
					this.label.Content = "";
					SetMap(result, true);
				}
				catch (Exception e) {
					this.label.Content = "error";
				}
			}
		}
		private void SetMap(Map map,bool carryOn) {
			Items.Clear();
			if(map.IsString) {
				label.Content = map.GetString();
			}
			else if (map.IsNumber) {
				label.Content = map.GetNumber().ToString();
			}
			else {
				foreach (KeyValuePair<Map, Map> entry in map) {
					if (carryOn) {
						Items.Add(new SubItem(entry.Key, entry.Value,true));
					}
				}
			}
		}
	}
	public class Watch {
		public Watch(string expression) {
			this.expression = expression;
		}
		private string expression;
		public string Expression {
			get {
				return expression;
			}
			set {
				expression=value;
			}
		}
		private string val;
		private Map context;
		public void SetContext(Map context) {
			this.context = context;
			Value = "some other value";
		}
		public string Value {
			get {
				return val;
			}
			set {
				val = value;
			}
		}
	}
	public static string ProgramName {
		get {
			return "Meta Develop";
		}
	}
	public static string Version {
		get {
			return "0.1";
		}
	}
	public static void LoadSettings() {
		using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey(ProgramName).CreateSubKey(Version)) {
			foreach (FieldInfo field in typeof(Settings).GetFields()) {
				field.SetValue(null, key.GetValue(field.Name, null));
			}
		}
	}
	public static void SaveSettings() {
		using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey(ProgramName).CreateSubKey(Version)) {
			foreach (FieldInfo field in typeof(Settings).GetFields()) {
				key.SetValue(field.Name, field.GetValue(null));
			}
		}
	}
	public static RowDefinition Row() {
		RowDefinition row = new RowDefinition();
		row.Height = GridLength.Auto;
		return row;
	}
	public static Thread debugThread;
	public static void StopDebugging() {
		Interpreter.stopping = true;
		continueDebugging = true;
		HideWatch();
	}
	public static bool continueDebugging = false;

	public void MakeCommand(ModifierKeys modifiers,Key key,MethodInvoker method) {
		RoutedUICommand command = new RoutedUICommand();
		BindKey(command,key,modifiers);
		CommandBindings.Add(new CommandBinding(command, delegate { method(); }));
	}
	public static string GetIndentation(string line) {
		return "".PadLeft(line.Length - LineWithoutIndentation(line).Length, '\t');
	}
	public static string LineWithoutIndentation(string line) {
		return line.TrimStart('\t');
	}
	public void Indent(bool increase) {
		int startLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart);
		int endLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart + textBox.SelectionLength);
		int oldStart = textBox.SelectionStart;
		int oldLength = textBox.SelectionLength;
		int removed = 0;
		for (int i = startLine; i <= endLine; i++) {
			string line = textBox.GetLineText(i);
			textBox.SelectionStart = textBox.GetCharacterIndexFromLineIndex(i);
			textBox.SelectionLength = line.Length;
			string lineStart=LineWithoutIndentation(line);
			if(lineStart.StartsWith("//")) {
				lineStart=lineStart.Remove(0,2);
				removed += 2;
			}
			int difference=increase?
				1:-1;
			textBox.SelectedText = "".PadLeft(Math.Max(0,GetIndentation(line).Length+difference),'\t') + lineStart;
		}
		textBox.SelectionStart = oldStart;
		textBox.SelectionLength = oldLength-removed;
	}
	public static FontFamily font;
	public Editor() {
		try {
			font = new FontFamily("Consolas");
		}
		catch (Exception e) {
			font = new FontFamily("Courier New");
		}

		errorList.Background = Brushes.LightBlue;
		errorList.Height = 100;
		errorList.Visibility = Visibility.Collapsed;
		editor = this;
		LoadSettings();
		watch.Items.Add(new MyItem());
		MakeCommand(ModifierKeys.Control, Key.U, delegate {
			int startLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart);
			int endLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart + textBox.SelectionLength-1);
			int oldStart = textBox.SelectionStart;
			int oldLength = textBox.SelectionLength;
			int removed = 0;
			for (int i = startLine; i <= endLine; i++) {
				string line = textBox.GetLineText(i);
				textBox.SelectionStart = textBox.GetCharacterIndexFromLineIndex(i);
				textBox.SelectionLength = line.Length;
				string lineStart=LineWithoutIndentation(line);
				if(lineStart.StartsWith("//")) {
					lineStart=lineStart.Remove(0,2);
					removed+=2;
				}
				textBox.SelectedText = GetIndentation(line) + lineStart;
			}
			textBox.SelectionStart = oldStart;
			textBox.SelectionLength = oldLength-removed;
		});
		MakeCommand(ModifierKeys.Control, Key.K, delegate {
			int startLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart);
			int endLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart + textBox.SelectionLength-2);
			int oldStart = textBox.SelectionStart;
			int oldLength = textBox.SelectionLength;
			int added=0;
			for (int i = startLine; i <= endLine; i++) {
				string line=textBox.GetLineText(i);
				textBox.SelectionStart = textBox.GetCharacterIndexFromLineIndex(i);
				textBox.SelectionLength = line.Length;
				textBox.SelectedText = GetIndentation(line) + "//" + LineWithoutIndentation(line);
				added += 2;
			}
			textBox.SelectionStart = oldStart;
			textBox.SelectionLength = oldLength+added;
		});
		findAndReplace=new FindAndReplace(textBox);
		this.WindowState = WindowState.Maximized;
		editor = this;
		textBox.AcceptsReturn = true;
		RoutedUICommand gotoLine = new RoutedUICommand();
		BindKey(gotoLine, Key.G, ModifierKeys.Control);
		CommandBindings.Add(new CommandBinding(gotoLine, delegate {
			Window window = new Window();
			StackPanel panel=new StackPanel();
			TextBox box=new TextBox();
			Button button = new Button();
			window.Title = "Go to line";
			button.Content = "OK";
			panel.Children.Add(box);
			panel.Children.Add(button);
			window.Content = panel;
			box.Text = (textBox.Line+1).ToString();
			box.Select(0, box.Text.Length);
			box.Focus();
			window.Width = 40;
			window.Height = 70;
			MethodInvoker go=delegate {
				int line;
				if(int.TryParse(box.Text,out line)) {
					if (line < textBox.LineCount) {
						textBox.SelectionStart = textBox.GetCharacterIndexFromLineIndex(line - 1);
						window.Close();
					}
				}
			};
			window.KeyDown += delegate(object sender, KeyEventArgs e) {
				if (e.Key == Key.Enter) {
					go();
				}
				else if (e.Key == Key.Escape) {
					window.Close();
				}
			};
			box.AcceptsReturn = false;
			button.Click += delegate {
				go();
			};
			window.ShowDialog();
		}));
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

		MakeCommand(ModifierKeys.Alt, Key.U, delegate {
			int lineIndex=textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart);
			string line=textBox.GetLineText(lineIndex);
			int position = GetIndentation(line).Length;
			if (textBox.Column != position) {
				textBox.Column = position;
			}
			else {
				EditingCommands.MoveToLineStart.Execute(null, textBox);
			}
		});
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
		textBox.FontSize = 14;
		textBox.SpellCheck.IsEnabled = true;
		intellisense.MaxHeight = 100;
		toolTip.Text = "Tooltip!!!!";
		toolTip.Width = 300;
		Label l;
		toolTip.TextWrapping = TextWrapping.Wrap;
		Control preview=null;
		intellisense.SelectionChanged += delegate {
			if (intellisense.SelectedItem != null) {
				Item item = ((Item)intellisense.SelectedItem);
				toolTip.Text = item.Signature();
				if(preview!=null) {
					canvas.Children.Remove(preview);
				}
				preview=item.GetPreview();
				if (preview != null) {
					canvas.Children.Add(preview);
					Canvas.SetLeft(preview, Canvas.GetLeft(intellisense)+500);
					Canvas.SetTop(preview, Canvas.GetTop(intellisense));
				}
			}
		};
		toolTip.FontFamily = font;
		toolTip.FontSize = 14;
		intellisense.Width = 400;
		intellisense.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, delegate { Save(); }));
		intellisense.FontFamily = font;
		intellisense.FontSize = 14;
		textBox.FontFamily = font;
		textBox.AcceptsTab = true;
		textBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
			if (e.Key == Key.Tab) {
				bool increase;
				if(textBox.SelectionLength!=0) {
					if(e.KeyboardDevice.Modifiers==ModifierKeys.Shift) {
						Indent(false);
						e.Handled=true;
						return;
					}
					else if(e.KeyboardDevice.Modifiers==ModifierKeys.None) {
						Indent(true);
						e.Handled=true;
						return;
					}
				}
			}
			if (e.Key == Key.Return) {
				string line = textBox.GetLineText(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart));
				textBox.SelectedText = Environment.NewLine.PadRight(2 + line.Length - line.TrimStart('\t').Length, '\t');
				textBox.SelectionStart = textBox.SelectionStart + textBox.SelectionLength;
				textBox.SelectionLength = 0;
				textBox.Focus();
				e.Handled = true;
			}
			else if (Intellisense) {
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
						ApplicationCommands.Cut.Execute(null, textBox);
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
				if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
					if (e.Key == Key.I) {
						StartIterativeSearch(false);
						e.Handled = true;
					}
				}
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
					if (e.Key == Key.I) {
						StartIterativeSearch(true);
						e.Handled = true;
					}
					else if (e.Key == Key.Space) {
						if(!DoArgumentHelp(e)) {
							e.Handled = true;
							StartIntellisense();
							searchStart--;
							intellisense.Items.Clear();
							List<Source> sources=new List<Source>(Meta.Expression.sources.Keys);
							sources.RemoveAll(delegate (Source source){
								return source.FileName!=fileName;
							});
							sources.Sort(delegate(Source a,Source b) {
								return a.CompareTo(b);
							});
							sources.Reverse();
							Program start = null;
							foreach(Source source in sources) {
								foreach(Meta.Expression expression in Meta.Expression.sources[source]) {
									Program program=expression as Program;
									if(program!=null) {
										start = program;
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
											result = p.statementList[Math.Max(0,p.statementList.Count - 2)].CurrentMap();
										}
										if (result != null) {
											s = Library.Merge(result,s);
										}
									}
									else if (x is LiteralExpression) {
										LiteralExpression literal = (LiteralExpression)x;
										Map structure=literal.GetStructure();

										s = Library.Merge(structure, s);
									}
									x = x.Parent;
								}
							}
							Map directory=new DirectoryMap(System.IO.Path.GetDirectoryName(fileName));
							s=Library.Merge(directory.Copy(),s);
							List<Map> keys = new List<Map>();
							if (s != null) {
								keys.AddRange(s.Keys);
							}
							keys.Sort(delegate(Map a,Map b) {
								return a.ToString().CompareTo(b.ToString());
							});
							if (keys.Count != 0) {
								intellisense.Visibility = Visibility.Visible;
								toolTip.Visibility = Visibility.Visible;
							}
							intellisenseItems.Clear();
							intellisense.Items.Clear();
							foreach (Map k in keys) {
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
								if (k.Equals(new StringMap("internet.bmp"))) {
								}
								if (k.Source != null && k.Source.Start.FileName.Equals(Interpreter.LibraryPath)) {
									intellisenseItems.Add(new MetaItem(k));
								}
								else if (original != null) {
									intellisenseItems.Add(new Item(k.ToString(), original));
								}
								else {
									intellisenseItems.Add(new PreviewItem(k,s[k]));
								}
							}
							if (intellisense.Items.Count != 0) {
								intellisense.SelectedIndex = 0;
							}
							foreach (Item item in intellisenseItems) {
								intellisense.Items.Add(item);
							}
							PositionIntellisense();
							Meta.Expression.sources.Clear();
						}
						e.Handled = true;
					}
				}
				else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
					if (e.SystemKey == Key.I) {
						int index = FindMatchingBrace();
						if (index != -1) {
							textBox.SelectionStart = index;
						}
						//FindMatchingBrace();
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
			if (e.Key == Key.Escape) {
				if (Intellisense) {
					intellisense.Visibility = Visibility.Hidden;
					toolTip.Visibility = Visibility.Hidden;
				}
			}
			else if (e.Key == Key.D9) {
				DoArgumentHelp(e);
			}
			else if (e.Key == Key.D8 || e.Key == Key.OemComma) {
				DoArgumentHelp(e);
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
								if (original != null) {
									intellisenseItems.Add(new Item(k, original));
								}
								else {
									intellisenseItems.Add(new Item(k, null));
								}
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
		
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		Menu menu = new Menu();
		MenuItem file = new MenuItem();
		MenuItem save = new MenuItem();
		MenuItem run = new MenuItem();
		MenuItem openItem = new MenuItem();
		MenuItem comp = new MenuItem();
		MenuItem debugItem = new MenuItem();
		MenuItem breakpointItem = new MenuItem();
		RoutedUICommand execute = new RoutedUICommand("Run","Run",GetType());
		RoutedUICommand compile = new RoutedUICommand("Compile", "Compile", GetType());
		RoutedUICommand breakpoint = new RoutedUICommand("Breakpoint", "Breakpoint", GetType());
		debugItem.Header = "Debug";
		file.Header = "File";
		MenuItem view = new MenuItem();
		view.Header = "View";
		watch.Visibility = Visibility.Collapsed;
		openItem.Header = "Open";
		breakpointItem.Header = "Toggle Breakpoint";
		comp.Header = "Compile";
		comp.Command = compile;
		openItem.Command=ApplicationCommands.Open;
		save.Header = "Save";
		save.Command = ApplicationCommands.Save;
		run.Header = "Run";
		run.Command = execute;
		file.Items.Add(breakpointItem);
		file.Items.Add(debugItem);
		CommandBindings.Add(
			new CommandBinding(compile,delegate{Compile(false);})
		);
		CommandBindings.Add(new CommandBinding(breakpoint,delegate {
			breakpoints.Add(new Breakpoint(textBox.Line, textBox.Column));
		}));
		BindKey(breakpoint, Key.H, ModifierKeys.Alt);
		DispatcherTimer timer = new DispatcherTimer();
		timer.Interval = new TimeSpan(0,0,3);
		timer.Tick += delegate {
			timer.Stop();
			string text = textBox.Text;
			Thread thread = new Thread(new ThreadStart(delegate {
				Compile(text,true);
				timer.Start();
			}));
			thread.Priority = ThreadPriority.Lowest;
			thread.Start();
		};
		timer.Start();
		CommandBindings.Add(new CommandBinding(execute, delegate {
			Save();
			if (Compile(false)) {
				Process.Start(System.IO.Path.Combine(@"D:\Meta\", @"bin\Debug\Meta.exe"), fileName);
			}
		}));
		RoutedUICommand debug = new RoutedUICommand("Debug", "Debug", GetType());
		RoutedUICommand stopDebug = new RoutedUICommand();
		BindKey(stopDebug, Key.F5, ModifierKeys.Shift);
		CommandBindings.Add(new CommandBinding(stopDebug, delegate {
			StopDebugging();
		}));
		bool debugging = false;
		CommandBindings.Add(new CommandBinding(debug, delegate {
			debugging = true;
			Save();
			if (Compile(false)) {
				watch.Height = 100;
				watch.Visibility = Visibility.Visible;
				Interpreter.breakpoints.Clear();
				foreach (Breakpoint b in breakpoints) {
					Interpreter.breakpoints.Add(new Source(b.line, b.column, fileName));
				}
				Interpreter.Breakpoint += delegate(Map map){
					Dispatcher.Invoke(DispatcherPriority.Normal, new MethodInvoker(delegate {
						debuggingContext = map;
						for (int i = 0; i < watch.Items.Count; i++) {
							((MyItem)watch.Items[i]).Update(map);
						}
					}));
					while (!continueDebugging) {
						Thread.Sleep(500);
					}
					continueDebugging = false;
				};
				debugThread=new Thread(new ThreadStart(delegate {
					try {
						Interpreter.Application = Application.Current;
						Interpreter.Run(fileName, Map.Empty);
					}
					catch (ThreadAbortException e) {
						return;
					}
					catch (Exception e) {
						MessageBox.Show(e.ToString());
					}
					Dispatcher.Invoke(DispatcherPriority.Normal, new MethodInvoker(delegate {
						StopDebugging();
					}));
				}));
				debugThread.TrySetApartmentState(ApartmentState.STA);
				debugThread.Start();
			}
			debugging = false;
		}));
		debugItem.Command = debug;

		BindKey(compile, Key.F3, ModifierKeys.None);
		BindKey(execute, Key.F5, ModifierKeys.Control);
		BindKey(debug, Key.F5, ModifierKeys.None);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, delegate { Open(); }));
		StackPanel status = new StackPanel();
		status.Orientation = Orientation.Horizontal;
		status.Children.Add(editorLine);

		status.Children.Add(message);

		file.Items.Add(openItem);
		file.Items.Add(comp);
		file.Items.Add(save);
		file.Items.Add(run);
		menu.Items.Add(file);
		menu.Items.Add(view);

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
		int historyIndex=0;
		List<int> history = new List<int>();
		bool ignoreChange = false;
		Rectangle brace=null;
		textBox.SelectionChanged += delegate {
			if (!ignoreChange) {
				int line = (textBox.Line + 1);
				DispatcherTimer t = new DispatcherTimer();
				t.Interval = new TimeSpan(20000);
				int i = textBox.SelectionStart;
				t.Tick += delegate {
					history.RemoveRange(historyIndex, history.Count - historyIndex);
					history.Add(i);
					historyIndex++;
					t.Stop();
				};
				t.Start();
				editorLine.Content = "Ln " + line + " Col " + (textBox.Column+1);


				if(brace!=null) {
					canvas.Children.Remove(brace);
				}
				if (textBox.Text.Length != 0) {
					char c = textBox.Text[textBox.SelectionStart];
					if ((openBraces + closeBraces).IndexOf(c) != -1) {
						int index = FindMatchingBrace();
						if (index != -1) {
							Rect r = textBox.GetRectFromCharacterIndex(index);
							brace = new Rectangle();
							FormattedText formattedText=new FormattedText("a", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),textBox.FontSize,Brushes.LightGray);
							brace.Width = formattedText.Width;
							brace.Height = formattedText.Height;
							brace.Opacity = 0.6;
							brace.Fill = Brushes.Gray;
							Canvas.SetTop(brace, r.Top);
							Canvas.SetLeft(brace, r.Right);
							canvas.Children.Add(brace);
						}
					}
				}
			}
		};
		RoutedUICommand back = new RoutedUICommand();
		CommandBindings.Add(new CommandBinding(back, delegate {
			if (history.Count != 0) {
				ignoreChange = true;
				if (historyIndex > 0) {
					textBox.Select(history[historyIndex - 1], 0);
					historyIndex--;
				}
				ignoreChange = false;
			}
		}));
		BindKey(back, Key.OemMinus, ModifierKeys.Control);
		MakeCommand(ModifierKeys.Control | ModifierKeys.Shift, Key.OemMinus, delegate {
			if (historyIndex<history.Count) {
				ignoreChange = true;
				textBox.Select(history[historyIndex], 0);
				historyIndex++;
				ignoreChange = false;
			}

		});
		canvas.Children.Add(textBox);
		canvas.Background = Brushes.Yellow;
		scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.Content = canvas;
		textBox.Padding = new Thickness(100,0,0,200);
		const int width = 0;
		const int height = 0;
		Canvas.SetLeft(textBox, width/2);
		Canvas.SetTop(textBox, height/2);

		watch.Background = Brushes.Green;

		GridSplitter splitter = new GridSplitter();
		splitter.ResizeDirection = GridResizeDirection.Rows;
        splitter.HorizontalAlignment=HorizontalAlignment.Stretch;
        splitter.VerticalAlignment=VerticalAlignment.Top;
		splitter.Background = Brushes.Yellow;
		splitter.Height = 15;

		watch.VerticalAlignment = VerticalAlignment.Stretch;
		errorList.VerticalAlignment = VerticalAlignment.Stretch;

		grid.RowDefinitions.Add(Row());
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(Row());

		grid.RowDefinitions.Add(Row());
		grid.RowDefinitions.Add(Row());


		Grid.SetRow(menu, 0);
		Grid.SetRow(scrollViewer, 1);
		Grid.SetRow(splitter, 2);
		Grid.SetRow(watch, 3);
		Grid.SetRow(errorList, 3);
		Grid.SetRow(status, 4);
		grid.Children.Add(menu);
		grid.Children.Add(scrollViewer);
		grid.Children.Add(status);
		grid.Children.Add(watch);
		grid.Children.Add(errorList);
		grid.Children.Add(splitter);

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
		this.Content = grid;
		this.Loaded += delegate {
			if (Settings.lastFile != null) {
				Open(Settings.lastFile);
			}
			textBox.Focus();
		};
	}

	private bool DoArgumentHelp(KeyEventArgs e) {
		StartIntellisense();
		int index = textBox.SelectionStart;
		string text = textBox.Text;
		int open = 1;
		int argIndex = 0;
		Source realSource = null;
		if (e.Key == Key.D8) {
			open = 0;
		}
		if (e.Key == Key.OemComma) {
			argIndex++;
		}
		while (index >= 0) {
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
			if (open == 0) {
				int line = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1;
				int selection = textBox.SelectionStart;
				int column = textBox.Column;
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
			sources.Reverse();
			Meta.Expression start = null;
			foreach (Source source in sources) {
				if (source.Line <= realSource.Line && source.Column <= realSource.Column) {
					foreach (Meta.Expression expression in Meta.Expression.sources[source]) {
						if (expression is Select || expression is Search) {
							start = expression;
							if (start.Parent is Call) {
								start = ((Call)start.Parent).calls[0];
							}
							break;
						}
					}
				}
				if (start != null) {
					break;
				}
			}
			if (start != null) {
				Map structure = start.GetStructure();
				if (structure != null) {
					Method method = structure as Method;
					if (method != null) {
						XmlNodeList parameters = new XmlComments(method.method).Params;
						string t = null;
						string paramName;
						ParameterInfo[] param = method.method.GetParameters();
						if (param.Length > argIndex) {
							paramName = param[argIndex].Name;
							t=param[argIndex].ParameterType.Name+" "+paramName;
							foreach(XmlNode node in parameters) {
								if (node.Attributes["name"].Value.Equals(paramName)) {
									t+= ":\n" + node.InnerText;
								}
							}
							PositionIntellisense();
							toolTip.Visibility = Visibility.Visible;
							toolTip.Text = t;
							return true;
						}
					}
				}
			}
		}
		toolTip.Visibility = Visibility.Hidden;
		return false;
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
		LiteralExpression directory=new LiteralExpression(new DirectoryMap(System.IO.Path.GetDirectoryName(fileName)),lib);
		lib.Statement = new LiteralStatement(gac);
		directory.Statement = new LiteralStatement(lib);
		KeyStatement.intellisense = true;
		map[CodeKeys.Function].GetExpression(directory).Statement = new LiteralStatement(directory);
		map[CodeKeys.Function].Compile(directory);
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
	private int FindMatchingBrace(string openBraces, string closeBraces, bool direction) {
		char previous = textBox.SelectionStart>0?
			textBox.Text[textBox.SelectionStart - 1]:'a';
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
				return -1;
				//return false;
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
					return pos;
					//textBox.SelectionStart = pos;
					//return true;
				}
			}
		}
		return -1;
		//return false;
	}
	//private bool MatchingBrace(string openBraces, string closeBraces, bool direction) {
	//    char previous = textBox.Text[textBox.SelectionStart - 1];
	//    char next = textBox.Text[textBox.SelectionStart];
	//    int forward = openBraces.IndexOf(previous);
	//    int backward = openBraces.IndexOf(next);
	//    if (forward != -1 || backward != -1) {
	//        char brace;
	//        int index;
	//        if (forward != -1) {
	//            index = forward;
	//            brace = openBraces[forward];
	//        }
	//        else if (backward != -1) {
	//            index = backward;
	//            brace = openBraces[backward];
	//        }
	//        else {
	//            return false;
	//        }
	//        char closingBrace = closeBraces[index];
	//        int pos;
	//        if (direction) {
	//            pos = textBox.SelectionStart + 2;
	//        }
	//        else {
	//            pos = textBox.SelectionStart - 2;
	//        }
	//        int braces = 0;
	//        while (true) {
	//            if (direction) {
	//                pos++;
	//            }
	//            else {
	//                pos--;
	//            }
	//            if (pos <= 0 || pos >= textBox.Text.Length) {
	//                break;
	//            }
	//            char c = textBox.Text[pos];
	//            if (c == closingBrace) {
	//                braces--;
	//            }
	//            else if (c == brace) {
	//                braces++;
	//            }
	//            if (braces < 0) {
	//                textBox.SelectionStart = pos;
	//                return true;
	//            }
	//        }
	//    }
	//    return false;
	//}
}
namespace _treeListView {
	public class LevelToIndentConverter : IValueConverter {
		public object Convert(object o, Type type, object parameter,
							  CultureInfo culture) {
			return new Thickness((int)o * c_IndentSize, 0, 0, 0);
		}

		public object ConvertBack(object o, Type type, object parameter,
								  CultureInfo culture) {
			throw new NotSupportedException();
		}

		private const double c_IndentSize = 19.0;
	}
}
namespace _treeListView {
	public class TreeListView : TreeView {
		protected override DependencyObject
						   GetContainerForItemOverride() {
			return new TreeListViewItem();
		}

		protected override bool
						   IsItemItsOwnContainerOverride(object item) {
			return item is TreeListViewItem;
		}
	}

	public class TreeListViewItem : TreeViewItem {
		public int Level {
			get {
				if (_level == -1) {
					TreeListViewItem parent =
						ItemsControl.ItemsControlFromItemContainer(this)
							as TreeListViewItem;
					_level = (parent != null) ? parent.Level + 1 : 0;
				}
				return _level;
			}
		}


		protected override DependencyObject
						   GetContainerForItemOverride() {
			return new TreeListViewItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item) {
			return item is TreeListViewItem;
		}

		private int _level = -1;
	}

}
// Stephen Toub
// stoub@microsoft.com
//
// Retrieve the xml comments stored in the assembly's comments file
// for specific types or members of types.

namespace NetMatters
{
	/// <summary>Used to retrieve the XML comments for MemberInfo objects.</summary>
	public class XmlComments
	{
		#region Static Variables
		/// <summary>Hashtable of all loaded XmlDocument comment files for assemblies.</summary>
		private static Hashtable _assemblyDocs = new Hashtable();
		/// <summary>
		/// Hashtable, indexed by Type, of all the accessors for a type.  Each entry is a Hashtable, 
		/// indexed by MethodInfo, that returns the MemberInfo for a given MethodInfo accessor.
		/// </summary>
		private static Hashtable _typeAccessors = new Hashtable();
		/// <summary>Binding flags to use for reflection operations.</summary>
		private static BindingFlags _bindingFlags = 
			BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		#endregion

		#region Member Variables
		/// <summary>The entire XML comment block for this member.</summary>
		private XmlNode _comments;
		/// <summary>The summary comment for this member.</summary>
		private XmlNode _summary;
		/// <summary>The remarks comment for this member.</summary>
		private XmlNode _remarks;
		/// <summary>The return comment for this member.</summary>
		private XmlNode _return;
		/// <summary>The value comment for this member.</summary>
		private XmlNode _value;
		/// <summary>The example comment for this member.</summary>
		private XmlNode _example;
		/// <summary>The includes comments for this member.</summary>
		private XmlNodeList _includes;
		/// <summary>The exceptions comments for this member.</summary>
		private XmlNodeList _exceptions;
		/// <summary>The paramrefs comments for this member.</summary>
		private XmlNodeList _paramrefs;
		/// <summary>The permissions comments for this member.</summary>
		private XmlNodeList _permissions;
		/// <summary>The params comments for this member.</summary>
		private XmlNodeList _params;
		#endregion

		#region Extracting Specific Comments
		/// <summary>Gets the entire XML comment block for this member.</summary>
		public XmlNode AllComments { get { return _comments; } }
		/// <summary>Gets the summary comment for this member.</summary>
		public XmlNode Summary { get { return _summary; } }
		/// <summary>Gets the remarks comment for this member.</summary>
		public XmlNode Remarks { get { return _remarks; } }
		/// <summary>Gets the return comment for this member.</summary>
		public XmlNode Return { get { return _return; } }
		/// <summary>Gets the value comment for this member.</summary>
		public XmlNode Value { get { return _value; } }
		/// <summary>Gets the example comment for this member.</summary>
		public XmlNode Example { get { return _example; } }
		/// <summary>Gets the includes comments for this member.</summary>
		public XmlNodeList Includes { get { return _includes; } }
		/// <summary>Gets the exceptions comments for this member.</summary>
		public XmlNodeList Exceptions { get { return _exceptions; } }
		/// <summary>Gets the paramrefs comments for this member.</summary>
		public XmlNodeList ParamRefs { get { return _paramrefs; } }
		/// <summary>Gets the permissions comments for this member.</summary>
		public XmlNodeList Permissions { get { return _permissions; } }
		/// <summary>Gets the params comments for this member.</summary>
		public XmlNodeList Params { get { return _params; } }
		/// <summary>Renders to a string the entire XML comment block for this member.</summary>
		public override string ToString() { return _comments.OuterXml; }
		#endregion

		#region Construction
		/// <summary>Gets the XML comments for the calling method.</summary>
		public static XmlComments Current
		{
			get { return new XmlComments(new StackTrace().GetFrame(1).GetMethod()); }
		}

		/// <summary>Initializes the XML comments for the specified member.</summary>
		/// <param name="mi">The member for which we want to retrieve the XML comments.</param>
		public XmlComments(MemberInfo mi)
		{
			if (mi == null) throw new ArgumentNullException("mi", "A MemberInfo must be supplied in order to retrieve the comments.");

			// If this is an accessor, get the owner property/event of the accessor.
			if (mi is MethodInfo) {
				MemberInfo owner = IsAccessor((MethodInfo)mi);
				if (owner != null) {
					mi = owner;
				}
			}

			// Get the comments.  If we got any, parse out the important stuff.
			_comments = GetComments(mi);
			if (_comments != null)
			{
				// Get single nodes (comments that can appear only once)
				_summary = _comments.SelectSingleNode("summary");
				_return = _comments.SelectSingleNode("return");
				_remarks = _comments.SelectSingleNode("remarks");
				_example = _comments.SelectSingleNode("example");
				_value = _comments.SelectSingleNode("value");

				// Get node lists (comments that can appear multiple times)
				_includes = _comments.SelectNodes("include");
				_exceptions = _comments.SelectNodes("exception");
				_paramrefs = _comments.SelectNodes("paramref");
				_permissions = _comments.SelectNodes("permission");
				_params = _comments.SelectNodes("param");
			}
			else
			{
				// Make it easier for people to use this class when no comments exist
				// by creating dummy nodes for all properties.
				_comments = new XmlDocument();
				_summary = _return = _remarks = _example = _value = _comments;
				_includes = _exceptions = _paramrefs = _permissions = _params = _comments.ChildNodes;
			}
		}
		#endregion

		#region Parsing the XML Document for an Assembly
		/// <summary>Retrieve the XML comments for a type or a member of a type.</summary>
		/// <param name="mi">The type or member for which comments should be retrieved.</param>
		/// <returns>A string of xml containing the xml comments of the selected type or member.</returns>
		private static XmlNode GetComments(MemberInfo mi)
		{
			Type declType = (mi is Type) ? ((Type)mi) : mi.DeclaringType;
			if (declType.Equals(typeof(DependencyObject))) {
			}
			XmlDocument doc = LoadAssemblyComments(declType.Assembly);
			if (doc == null) return null;
			string xpath;

			// The fullname uses plus signs to separate nested types from their declaring
			// types.  The xml documentation uses dotted-notation.  We need to change
			// from one to the other.
			string typeName = declType.FullName.Replace("+", ".");

			// Based on the member type, get the correct xpath query to lookup the 
			// member's comments in the assembly's documentation.
			switch(mi.MemberType)
			{					
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
					xpath = "//member[@name='T:" + typeName + "']";
					break;

				case MemberTypes.Constructor:
					xpath = "//member[@name='M:" + typeName + "." +
						"#ctor" + CreateParamsDescription(((ConstructorInfo)mi).GetParameters()) + "']";
					break;

				case MemberTypes.Method:
					xpath = "//member[@name='M:" + typeName + "." + 
						mi.Name + CreateParamsDescription(((MethodInfo)mi).GetParameters());
					if (mi.Name == "op_Implicit" || mi.Name == "op_Explicit")
					{
						xpath += "~{" + ((MethodInfo)mi).ReturnType.FullName + "}";
					}
					xpath += "']";
					break;

				case MemberTypes.Property:
					xpath = "//member[@name='P:" + typeName + "." + 
						mi.Name + CreateParamsDescription(((PropertyInfo)mi).GetIndexParameters()) + "']"; // have args when indexers
					break;

				case MemberTypes.Field:
					xpath = "//member[@name='F:" + typeName + "." + mi.Name + "']";
					break;

				case MemberTypes.Event:
					xpath = "//member[@name='E:" + typeName + "." + mi.Name + "']";
					break;

					// Unknown type, nothing to do
				default: 
					return null;
			}

			// Get the appropriate node from the document
			return doc.SelectSingleNode(xpath);
		}

		/// <summary>Determines if a MethodInfo represents an accessor.</summary>
		/// <param name="mi">The MethodInfo to check.</param>
		/// <returns>The MemberInfo that represents the property or event if this is an accessor; null, otherwise.</returns>
		private static MemberInfo IsAccessor(MethodInfo mi)
		{
			// Must be a special name in order to be an accessor
			if (!mi.IsSpecialName) return null;

			Hashtable accessors;
			lock(_typeAccessors.SyncRoot)
			{
				// We cache accessor information to speed things up, so check to see if the array
				// of accessors for this type has already been computed.
				accessors = (Hashtable)_typeAccessors[mi.DeclaringType];
				if (accessors == null)
				{
					// Retrieve the accessors for the declaring type
					_typeAccessors[mi.DeclaringType] = accessors = RetrieveAccessors(mi.DeclaringType);
				}
			}

			// Return the owning property or event for the accessor
			return (MemberInfo)accessors[mi];
		}

		/// <summary>Retrieve all property and event accessors on a given type.</summary>
		/// <param name="t">The type from which the accessors should be retrieved.</param>
		/// <returns>A dictionary of all accessors.</returns>
		private static Hashtable RetrieveAccessors(Type t)
		{
			// Build up list of accessors to exclude from method list
			Hashtable ht = new Hashtable();

			// Get all property accessors
			foreach(PropertyInfo pi in t.GetProperties(_bindingFlags)) 
			{
				foreach(MethodInfo mi in pi.GetAccessors(true)) ht[mi] = pi;
			}

			// Get all event accessors
			foreach(EventInfo ei in t.GetEvents(_bindingFlags)) 
			{
				MethodInfo addMethod = ei.GetAddMethod(true);
				MethodInfo removeMethod = ei.GetRemoveMethod(true);
				MethodInfo raiseMethod = ei.GetRaiseMethod(true);

				if (addMethod != null) ht[addMethod] = ei;
				if (removeMethod != null) ht[removeMethod] = ei;
				if (raiseMethod != null) ht[raiseMethod] = ei;
			}

			// Return the whole list
			return ht;
		}

		/// <summary>Generates a parameter string used when searching xml comment files.</summary>
		/// <param name="parameters">List of parameters to a member.</param>
		/// <returns>A parameter string used when searching xml comment files.</returns>
		private static string CreateParamsDescription(ParameterInfo [] parameters)
		{
			StringBuilder paramDesc = new StringBuilder();

			// If there are parameters then we need to construct a list
			if (parameters.Length > 0) 
			{
				// Start the list
				paramDesc.Append("(");
				
				// For each parameter, append the type of the parameter.
				// Separate all items with commas.
				for(int i=0; i<parameters.Length; i++)
				{
					Type paramType = parameters[i].ParameterType;
					string paramName = paramType.FullName;
					if (paramName == null) {
						paramName = "noName";
					}

					// Handle special case where ref parameter ends in & but xml docs use @.
					// Pointer parameters end in * in both type representation and xml comments representation.
					if (paramName.EndsWith("&")) paramName = paramName.Substring(0, paramName.Length-1) + "@";

					// Handle multidimensional arrays
					if (paramType.IsArray && paramType.GetArrayRank() > 1)
					{
						paramName = paramName.Replace(",", "0:,").Replace("]", "0:]");
					}

					// Append the fixed up parameter name
					paramDesc.Append(paramName);
					if (i!=parameters.Length-1) paramDesc.Append(",");
				}
				
				// End the list
				paramDesc.Append(")");
			}
			
			// Return the parameter list description
			return paramDesc.ToString();
		}
		#endregion

		#region Loading the Assembly's XML Comments
		/// <summary>Get the xml documentation for an assembly.</summary>
		/// <param name="a">The assembly whose documentation is to be loaded.</param>
		/// <returns>The xml documentation for an assembly; null if none found.</returns>
		public static XmlDocument LoadAssemblyComments(Assembly a)
		{
			// Get xml dom for the assembly's documentation
			XmlDocument doc;
			lock(_assemblyDocs.SyncRoot)
			{
				if (!_assemblyDocs.ContainsKey(a.FullName)) 
				{
					string xmlPath = DetermineXmlPath(a);
					if (xmlPath == null) return null;

					// Load it and store it
					doc = new XmlDocument();
					doc.Load(xmlPath);
					_assemblyDocs[a.FullName] = doc;
				}
					// Get the docs from the cache
				else doc = (XmlDocument)_assemblyDocs[a.FullName];
			}
			
			// Return the docs
			return doc;
		}

		/// <summary>Gets the path to a valid xml comments file for the assembly.</summary>
		/// <param name="asm">The assembly whose documentation is to be found.</param>
		/// <returns>The path to documentation for an assembly; null if none found.</returns>
		private static string DetermineXmlPath(Assembly asm)
		{
			// Get a list of locations to examine for the xml
			// 1. The location of the assembly.
			// 2. The runtime directory of the framework.
			string [] locations = new string []
				{   
					asm.Location,
					RuntimeEnvironment.GetRuntimeDirectory() + System.IO.Path.GetFileName(asm.CodeBase),
					@"C:\Programme\Reference Assemblies\Microsoft\Framework\v3.0\"+System.IO.Path.GetFileName(asm.CodeBase)
				};

			// Checks each path to see if the xml file exists there; if it does, return it.
			foreach(string location in locations)
			{
				string newPath = System.IO.Path.ChangeExtension(location, ".xml");
				if (File.Exists(newPath)) return newPath;
			}

			// No xml found; return null.
			return null;
		}
		#endregion
	}
}