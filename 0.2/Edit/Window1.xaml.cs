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
    public interface IView
    {
        Map GetMap();
    }
    public class EmptyView : TextBox, IView
    {
        public Map GetMap()
        {
            return Map.Empty;
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
            Strategy = new StringView("");
        }
        private void UseNumberView()
        {
            Strategy = new NumberView();
        }
		private void UseMapView()
		{
			MapView view=new MapView();
			view.NewEntry(0);
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
			return new MapView(map);
		}
		public View(Map map):this(GetView(map))
		{
		}
        public View(IView view)
        {
            this.Strategy=view;
            KeyboardShortcuts s=new KeyboardShortcuts(this,ModifierKeys.Alt);

            s[Key.S] = UseStringView;
            s[Key.E] = UseEmptyView;
            s[Key.N] = UseNumberView;
			s[Key.M] = UseMapView;
        }
    }
    public class EntryView : StackPanel
    {
        public EntryView()
        {
            this.Background = Brushes.Yellow;
            this.Orientation = Orientation.Horizontal;
        }
        public View Key
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
        public View Value
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
        public EntryView(Map key, Map value)
            : this(new View(key),new View(value))
        {
        }
        public EntryView(View key, View value):this()
        {
            Key = key;
            Value = value;
            KeyboardShortcuts s = new KeyboardShortcuts(this, ModifierKeys.None);
            s[System.Windows.Input.Key.Enter] = NewEntry;
        }
		private void NewEntry()
		{
			MapView view = ((MapView)Parent);
			view.NewEntry(view.Children.IndexOf(this) + 1);
		}
    }
    public class MapView : StackPanel,IView
    {
        public Map GetMap()
        {
            Map map=new StrategyMap();
            foreach (EntryView entry in Children)
            {
                map[entry.Key.GetMap()] = entry.Value.GetMap();
            }
            return map;
        }
		public MapView():this(Map.Empty)
		{
		}
		public void NewEntry(int index)
		{
			EntryView entry = new EntryView(new View(new StringView()), new View(new StringView()));
			Children.Insert(index, entry);
			entry.Key.Children[0].Focus();
		}
        public MapView(Map map)
        {
            this.Background = Brushes.LightBlue;
            this.Orientation = Orientation.Vertical;
			try
			{
				foreach (KeyValuePair<Map, Map> pair in map)
				{
					this.Children.Add(new EntryView(pair.Key, pair.Value));
				}
			}
			catch (Exception e)
			{
			}
        }
    }
    public class StringView : TextBox,IView
    {
        public Map GetMap()
        {
            return Text;
        
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
        public NumberView():this(1)
        {
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
    public partial class Window1 : System.Windows.Window
    {
        public void Run()
        {
            Map map=MainView.GetMap();
            Interpreter.Init();
            map.Call(Map.Empty, new Position(new Position(RootPosition.rootPosition, "filesystem"), "localhost"));

        }
        private void Open()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "*.meta (Meta files)|*.meta";
            if (dialog.ShowDialog()==true)
            {
                path = dialog.FileName;
                Load();
            }
        }
        private void Load()
        {
            MainView = new MapView(Binary.Deserialize(path));
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
            KeyboardShortcuts s = new KeyboardShortcuts(this,ModifierKeys.Control);
            s[Key.R]=Run;
            s[Key.O] = Open;
            s[Key.S] = Save;
            s[Key.A] = SaveAs;
            path = @"C:\asdf.meta";
            Load();
        }
    }
}