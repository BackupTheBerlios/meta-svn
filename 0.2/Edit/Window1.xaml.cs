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
using Meta;
using Microsoft.Win32;


namespace Edit
{
	public partial class Window1 : System.Windows.Window
	{
		public interface IView
		{
			Map GetMap();
		}
		public class EmptyView : TextBox, IView
		{
			public Map GetMap()
			{
				//SdlDotNet.Events
				//Application app = new Application();

				//Dock
				//DockPanel p;

				//Button b;
				//StackPanel.VerticalAlignmentProperty
				//System.Windows.VerticalAlignment
				//b.SetValue(DockPanel.DockProperty, Dock.Top);
				Window window = new Window();
				StackPanel p;
				//p.HorizontalAlignment=StackPanel.HorizontalAlignmentProperty
				//StackPanel.VerticalAlignmentProperty
				//Keyboard.Focus
				
				//p.KeyDown += new KeyEventHandler(p_KeyDown);
				return Map.Empty;
			}

			void p_KeyDown(object sender, KeyEventArgs e)
			{
				throw new Exception("The method or operation is not implemented.");
			}
			public EmptyView()
			{
				this.Background = Brushes.Red;
				this.PreviewTextInput += new TextCompositionEventHandler(EmptyView_PreviewTextInput);
			}

			void EmptyView_PreviewTextInput(object sender, TextCompositionEventArgs e)
			{
				e.Handled = true;
			}
		}
		public class View : StackPanel
		{
			public Map GetMap()
			{
				return Strategy.GetMap();
			}
			private IView Strategy
			{
				get
				{
					return (IView)this.Children[0];
				}
				set
				{
					this.Children.Clear();
					this.Children.Add((UIElement)value);
				}
			}
			private void UseEmptyView()
			{
				Strategy = new EmptyView();
			}
			private void UseStringView()
			{
				ComponentCommands.MoveFocusForward.Execute(null,this);
				//Strategy = new StringView("");
			}
			private void UseNumberView()
			{
				Strategy = new NumberView();
			}
			private void UseMapView()
			{
				MapView view=new MapView();
				Strategy = view;
			}
			private static IView GetView(Map map)
			{
				if(map.Count==0)
				{
					return new EmptyView();
				}
				else if (map.IsNumber)
				{
					return new NumberView(map.GetNumber());
				}
				else if (map.IsString)
				{
					return new StringView(map.GetString());
				}
				else if (map.Count == 1)
				{
					if (map.ContainsKey(CodeKeys.Call))
					{
						return CallView(map[CodeKeys.Call]);
						//return new CallView(map[CodeKeys.Call]);
					}
					else if (map.ContainsKey(CodeKeys.Literal))
					{
						return LiteralView(map);
					}
					else if (map.ContainsKey(CodeKeys.Search))
					{
						return SearchView(map);
					}
					else if (map.ContainsKey(CodeKeys.Lookup))
					{
						return LookupView(map);
					}
					else if (map.ContainsKey(CodeKeys.Select))
					{
						return new SelectView(map);
					}
				}
				return new MapView(map);
			}
			public View(Map map):this(GetView(map))
			{
			}
			public View(IView view)
			{
				this.MouseLeftButtonDown += new MouseButtonEventHandler(View_MouseLeftButtonDown);
				this.Strategy=view;
				KeyboardShortcuts s=new KeyboardShortcuts(this,ModifierKeys.Alt);
				s[Key.S] = UseStringView;
				s[Key.E] = UseEmptyView;
				s[Key.N] = UseNumberView;
				s[Key.M] = UseMapView;
				s[Key.F] = delegate
				{
				};
				s[Key.A] = delegate
				{
				};
				s[Key.C] = delegate
				{
				};
			}

			void View_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
			{
				Children[0].Focusable = true;
				Children[0].Focus();
				Keyboard.Focus(Children[0]);
			}
		}
		public class FunctionView : EntryBase
		{
			public override KeyValuePair<Map, Map> GetPair()
			{
				return new KeyValuePair<Map, Map>(
					CodeKeys.Function,
					new StrategyMap(
						CodeKeys.Parameter, First.GetMap(),
						CodeKeys.Expression,Second.GetMap()));

			}
			public FunctionView(Map map):this(map[CodeKeys.Parameter].GetString(),new View(map[CodeKeys.Expression]))
			{
			}
			public FunctionView():base()
			{
				this.Background = Brushes.Azure;
			}
			public FunctionView(string parameter, View expression):this()
			{
				First = new View(new StringView(parameter));
				Second = expression;
			}
		}
		public static ViewControl LookupView(Map map)
		{
			return SingleBase(map,CodeKeys.Lookup,Brushes.MistyRose);
		}
		public static ViewControl SearchView(Map map)
		{
			return SingleBase(map,CodeKeys.Search, Brushes.DeepSkyBlue);
		}
		public static ViewControl LiteralView(Map map)
		{
			return SingleBase(map,CodeKeys.Literal,Brushes.DarkViolet);
		}
		public static ViewControl SingleBase(Map map, Map key,Brush b)
		{
			return SingleBase(new View(map[key]),key,b);
		}
		public static ViewControl SingleBase(View view, Map key,Brush b)
		{
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.Margin = new Thickness(5);
			panel.Focusable = true;
			panel.Background = b;
			panel.Children.Add(view);
			//panel.Children.Remove
			return new ViewControl(panel, delegate { return new StrategyMap(key, view.GetMap()); });
		}
		public delegate Map MapDelegate();
		public class ViewControl:StackPanel,IView
		{
			private MapDelegate m;
			public Map GetMap()
			{
				return m();
			}
			public ViewControl(UIElement control,MapDelegate m)
			{
				this.Children.Add(control);
				this.m = m;
			}
		}
		public abstract class SingleReal:StackPanel,IView
		{
			protected View View
			{
				get
				{
					return (View)Children[0];
				}
				set
				{
					Children.Clear();
					Children.Add(value);
				}
			}
			protected abstract Map Key
			{
				get;
			}
			public Map GetMap()
			{
				return new StrategyMap(Key, this.View.GetMap());
			}
			public SingleReal()
			{
				this.Orientation = Orientation.Horizontal;
				this.Margin = new Thickness(5);
				this.Focusable = true;
			}
			public SingleReal(View view):this()
			{
				View = view;
			}
			public SingleReal(Map map): this()
			{
				View=new View(map[Key]);
			}
		}
		public static ViewControl CallView(Map map)
		{
			StackPanel panel = new StackPanel();
			panel.Background = Brushes.BurlyWood;
			panel.Margin = new Thickness(3);
			//View first = new View(map[1]);
			//View second = new View(map[2]);
			//panel.Children.Add(first);
			//panel.Children.Add(second);
			return BaseTwo(new View(map[1]),new View(map[2]), CodeKeys.Call,1,2);
		}
		//public class CallView : TwoBase, IView
		//{
		//    public Map GetMap()
		//    {
		//        return new StrategyMap(CodeKeys.Call, new StrategyMap(1, First.GetMap(), 2, Second.GetMap()));
		//    }
		//    public CallView(Map map)
		//    {
		//        this.Background = Brushes.BurlyWood;
		//        this.Margin = new Thickness(3);
		//        First = new View(map[1]);
		//        Second = new View(map[2]);
		//    }
		//}
		public class EntryView: EntryBase
		{
			public override KeyValuePair<Map, Map> GetPair()
			{
				return new KeyValuePair<Map, Map>(First.GetMap(), Second.GetMap());
			}
			public EntryView(Map key, Map value)
				: this(new View(key),new View(value))
			{
			}
			public EntryView(View key, View value):base()
			{
				First = key;
				Second = value;
			}
		}
		public abstract class EntryBase : TwoBase
		{
			public abstract KeyValuePair<Map, Map> GetPair();
			public EntryBase():base()
			{
				this.Background = Brushes.Yellow;
				this.Orientation = Orientation.Horizontal;

				KeyboardShortcuts s = new KeyboardShortcuts(this, ModifierKeys.None);
				s[System.Windows.Input.Key.Enter] = NewEntry;
				s[System.Windows.Input.Key.Delete] = Delete;
				KeyboardShortcuts m = new KeyboardShortcuts(this, ModifierKeys.Control);
				m[System.Windows.Input.Key.Enter] = NewFunction;
			}
			private void Delete()
			{
				MapView view = (MapView)Parent;
				view.Children.Remove(this);
			}
			private void NewEntry()
			{
				MapView view = ((MapView)Parent);
				view.NewEntry(view.Children.IndexOf(this) + 1);
			}
			private void NewFunction()
			{
				MapView view = ((MapView)Parent);
				view.NewFunction(view.Children.IndexOf(this) + 1);
			}
		}
		public class SelectView : ListBase
		{
			public SelectView(Map map)
				: base(map, CodeKeys.Select)
			{
				this.Background = Brushes.Chocolate;
			}
		}
		public abstract class ListBase : StackPanel,IView
		{
			public Map GetMap()
			{
				Map map = new StrategyMap();
				foreach(View view in Children)
				{
					map.Append(view.GetMap());
				}
				return new StrategyMap(key, map);
			}
			private Map key;
			public ListBase(Map map,Map key)
			{
				this.key = key;
				foreach (Map m in map[key].Array)
				{
					Children.Add(new View(m));
				}
			}
		}
		public static ViewControl BaseTwo(View first,View second,Map key,Map firstKey,Map secondKey)
		{
			StackPanel panel = new StackPanel();
			panel.Focusable = true;
			panel.Orientation = Orientation.Vertical;
			panel.Children.Add(first);
			panel.Children.Add(second);
			return new ViewControl(panel, delegate { return new StrategyMap(key, new StrategyMap(firstKey, first.GetMap(), secondKey, second.GetMap())); });
		}
		public abstract class TwoBase: StackPanel
		{
			public void FocusFirst()
			{
				this.Focus();
				First.Focus();
			}
			public TwoBase()
			{
				this.Focusable = true;
				this.Orientation = Orientation.Vertical;
			}
			protected View First
			{
				get
				{
					return (View)Children[0];
				}
				set
				{
					if (Children.Count >= 1)
					{
						Children.RemoveAt(0);
					}
					Children.Insert(0, value);
				}
			}
			protected View Second
			{
				get
				{
					return (View)Children[1];
				}
				set
				{
					if (Children.Count >= 2)
					{
						Children.RemoveAt(1);
					}
					Children.Insert(1, value);
				}
			}
		}
		public class MapView : StackPanel,IView
		{
			public Map GetMap()
			{
				Map map=new StrategyMap();
				foreach (EntryBase entry in Children)
				{
					KeyValuePair<Map,Map> pair=entry.GetPair();
					map[pair.Key] = pair.Value;
				}
				return map;
			}
			public MapView()
			{
				this.NewEntry(0);
			}
			public void NewFunction(int index)
			{
				EntryBase entry = new FunctionView("arg",new View(new MapView()));
				Children.Insert(index, entry);
				entry.FocusFirst();
			}
			public void NewEntry(int index)
			{
				EntryBase entry = new EntryView(new View(new StringView()), new View(new StringView()));
				Children.Insert(index, entry);
				entry.FocusFirst();
			}
			public MapView(Map map)
			{
				this.Focusable = true;
				this.Background = Brushes.LightBlue;
				this.Margin = new Thickness(5);
				this.Orientation = Orientation.Vertical;
				foreach (KeyValuePair<Map, Map> pair in map)
				{
					EntryBase entry;
					if (pair.Key.Equals(CodeKeys.Function))
					{
						entry = new FunctionView(pair.Value);
					}
					else
					{
						entry = new EntryView(pair.Key, pair.Value);
					}
					//this
					this.Children.Add(entry);
					//this.Children.Insert(
				}
			}
		}
		public class StringView : TextBox,IView
		{
			public Map GetMap()
			{
				this.GotFocus += new RoutedEventHandler(StringView_GotFocus);
				return Text;
			}

			void StringView_GotFocus(object sender, RoutedEventArgs e)
			{
				this.SelectAll();
				throw new Exception("The method or operation is not implemented.");
			}
			public StringView():this("")
			{
			}
			public StringView(string text)
			{
				this.Text = text;
				this.Background = Brushes.Fuchsia;
			}
		}
		public class NumberView : TextBox,IView
		{
			public Map GetMap()
			{
				return Convert.ToInt32(Text);
			}
			public NumberView()
				: this(1)
			{
			}
			public NumberView(Number number)
			{
				this.Background = Brushes.LightYellow;
				this.Text = number.ToString();
				this.PreviewTextInput += new TextCompositionEventHandler(NumberView_PreviewTextInput);
			}
			void NumberView_PreviewTextInput(object sender, TextCompositionEventArgs e)
			{
				foreach (char c in e.Text)
				{
					if (!char.IsDigit(c))
					{
						e.Handled = true;
						break;
					}
				}
			}
		}
		public delegate void MethodInvoker();
		public class KeyboardShortcuts:Dictionary<Key,MethodInvoker>
		{
			public KeyboardShortcuts(UIElement control,ModifierKeys modifier)
			{
				control.KeyDown += delegate(object sender, KeyEventArgs e)
				{
					if (ContainsKey(e.SystemKey) && e.KeyboardDevice.Modifiers==modifier)
					{
						this[e.SystemKey]();
						e.Handled = true;
					}
					else if (ContainsKey(e.Key) && e.KeyboardDevice.Modifiers == modifier)
					{
						this[e.Key]();
						e.Handled = true;
					}
				};
			}
		}
		public void Run()
        {
			//Menu menu = new Menu();
			//menu.Items.Add(
			//MenuItem item=new MenuItem();
			//item.Items.Add(
			//ApplicationCommands
			CommandBinding binding = new CommandBinding(ApplicationCommands.Paste, delegate { Console.WriteLine("Open the damn file."); });
			//binding.Command=ApplicationCommands.Open;
			//ApplicationCommands.Open;
			StackPanel s;
            Map map=MainView.GetMap();
            Interpreter.Init();
			//VerticalAlignment.Top
            map.Call(Map.Empty, new Position(new Position(RootPosition.rootPosition, "filesystem"), "localhost"));

        }
        private void Open()
        {
            OpenFileDialog dialog = new OpenFileDialog();
			//dialog.FileOk += new System.ComponentModel.CancelEventHandler(dialog_FileOk);
            dialog.Filter = "*.meta (Meta files)|*.meta";
            if (dialog.ShowDialog()==true)
            {
                path = dialog.FileName;
                Load();
            }
        }

		//void dialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		//{
		//}
        private void Load()
        {
			Map map = Binary.Deserialize(path);
            MainView = new MapView(map);
        }
        private string path;
        private void SaveAs()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "*.meta (Meta files)|*.meta";
            dialog.DefaultExt=".meta";
            dialog.AddExtension=true;
            if (dialog.ShowDialog() == true)
            {
                path = dialog.FileName;
                Save();
            }
        }
        private void Save()
        {
            if (path == null)
            {
				//ModifierKeys.Control
                SaveAs();
            }
            else
            {
                Binary.Serialize(MainView.GetMap(), path);
            }
        }
        private MapView MainView
        {
            get
            {
                return (MapView)Content;
            }
            set
            {
                Content = value;
            }
        }
        public Window1()
        {
            InitializeComponent();
			KeyboardShortcuts s = new KeyboardShortcuts(this, ModifierKeys.Control);
			s[Key.R] = Run;
			s[Key.O] = Open;
			s[Key.S] = Save;
			s[Key.A] = SaveAs;
			path = @"C:\asdf.meta";
			Load();
        }
    }
}