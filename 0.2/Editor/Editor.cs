using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Meta;
using System.Drawing.Drawing2D;
using SynapticEffect.Forms;

namespace Editor
{
	public partial class Editor : Form
	{
		public Editor()
		{
			InitializeComponent();
			this.Size = new Size(800, 600);
			LoadFile(@"D:\Meta\0.2\Test\edit.meta");
			this.Invalidate();
		}
		private void LoadFile()
		{
			if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				LoadFile(fileDialog.FileName);
			}
		}
		private void LoadFile(string path)
		{
			Map map=Binary.Deserialize(path);
			mainView = new MapViewer(map);
			mainView.Location = new Point(10, 10);

			if (Controls.Count > 1)
			{
				this.Controls.RemoveAt(1);
			}
			this.Controls.Add(mainView);
			this.path = path;
		}
		public static MapViewer mainView;
		private string path;
		private void open_Click(object sender, EventArgs e)
		{
			fileDialog.ShowDialog();
			LoadFile(fileDialog.FileName);
		}
		private void run_Click(object sender, EventArgs e)
		{
			Map map = mainView.GetMap();
			Interpreter.Init();
			map.Call(Map.Empty, new Position(new Position(RootPosition.rootPosition, "filesystem"), "localhost"));
		}
		private void save_Click(object sender, EventArgs e)
		{
			Binary.Serialize(mainView.GetMap(), path);
		}
	}
	public interface IStrategy
	{
		Map GetMap();
	}
	public class MapViewer : Panel
	{
		public Map GetMap()
		{
			return ((IStrategy)Control).GetMap();
		}
		public MapViewer(Control control)
		{
			this.AutoSize = true;
			this.Control = control;
		}
		public MapViewer(Map map):this(GetView(map))
		{
		}
		public Control Control
		{
			get
			{
				return Controls[0];
			}
			set
			{
				Controls.Clear();
				KeyMapper m = new KeyMapper(value);
				value.KeyDown += delegate(object sender, KeyEventArgs e)
				{
					if (!e.Handled)
					{
						OnKeyDown(e);
					}
				};
				Controls.Add(value);
				m[Keys.Alt | Keys.S] =
					delegate { Control = new StringStrategy(""); };
				m[Keys.Alt | Keys.E] =
					delegate { Control = new EmptyStrategy(); };
				m[Keys.Alt | Keys.N] =
					delegate { Control = new NumberStrategy(0); };
				m[Keys.Alt | Keys.M] =
					delegate {
						MapStrategy strategy = new MapStrategy();//new StrategyMap("",""));
						strategy.AddItem(new Entry(new MapViewer(new StringStrategy("")), new MapViewer(new StringStrategy(""))));
						Control = strategy;

						//MapStrategy strategy= new MapStrategy();
						//strategy.AddItem(new Entry(new MapViewer(new StringStrategy("")), new MapViewer(new StringStrategy(""))));
					};

				value.Focus();
			}
		}
		private static Control GetView(Map map)
		{
			if (map.Count == 0)
			{
				return new EmptyStrategy();
			}
			else if (map.IsNumber)
			{
				return new NumberStrategy(map.GetNumber());
			}
			else if (map.IsString)
			{
				return new StringStrategy(map.GetString());
			}
			return new MapStrategy(map);
		}
	}
	public abstract class EntryBase : ContainerListViewItem
	{
		public event KeyEventHandler KeyDown;
		public abstract Map GetKey();
		public abstract Map GetValue();
		protected void AddView(Map map)
		{
			AddView(new MapViewer(map));
		}
		protected void AddView(MapViewer view)
		{
			view.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				KeyDown(sender, e);
			};
			SubItems.Add(view);
		}
	}
	public class Entry : EntryBase
	{
		public override Map GetKey()
		{
			return ((MapViewer)this.SubItems[0].ItemControl).GetMap();
		}
		public override Map GetValue()
		{
			return ((MapViewer)this.SubItems[1].ItemControl).GetMap();
		}
		public Entry(MapViewer key,MapViewer value)
		{
			AddView(key);
			AddView(value);
		}
	}
	public class StringStrategy : TextBox, IStrategy
	{
		public StringStrategy(string text)
		{
			new GeneralStrategy(this);
			this.BackColor = Color.LightBlue;
			this.Text = text;
			this.Width = 50;
		}
		public Map GetMap()
		{
			return Text;
		}
	}
	public class EmptyStrategy : TextBox, IStrategy
	{
		public Map GetMap()
		{
			return Map.Empty;
		}
		public EmptyStrategy()
		{
			new GeneralStrategy(this);
			this.ReadOnly = true;
			this.BackColor = Color.Red;
		}
	}
	public class NumberStrategy : MaskedTextBox, IStrategy
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.SuppressKeyPress = !char.IsDigit(Convert.ToChar(e.KeyValue)) && e.Modifiers==Keys.None;
			base.OnKeyDown(e);
		}
		private Number number;
		public NumberStrategy(Number number)
		{
			this.Width = 50;
			new GeneralStrategy(this);
			this.BackColor = Color.LightCyan;
			this.number = number;
			this.Text = number.ToString();
		}
		public Map GetMap()
		{
			return new StrategyMap(Convert.ToInt32(this.Text));
		}
	}
	public class MapStrategy : ContainerListView, IStrategy
	{
		//protected override void OnGotFocus(EventArgs e)
		//{
		//    this.Controls[0].Focus();
		//}
		public MapStrategy()
		{
			this.VisualStyles = true;
			this.FullRowSelect = true;
			this.RowTracking = true;
			KeyMapper m = new KeyMapper(this);
			m[Keys.Enter | Keys.Control] = delegate
			{
				AddItem(new Entry(new MapViewer(new StringStrategy("")),new MapViewer(new StringStrategy(""))));
			};
			m[Keys.Control|Keys.Delete] = delegate
			{
				if (SelectedItems[0] != null)
				{
					ContainerListViewItem item=SelectedItems[0];
					foreach(ContainerSubListViewItem subItem in item.SubItems)
					{
						subItem.ItemControl.Parent.Controls.Remove(subItem.ItemControl);
					}
					Items.Remove(SelectedItems[0]);
					Editor.mainView.Invalidate();
				}
			};
			this.Size = new Size(10, 10);
			this.Columns.Add("", 20, HorizontalAlignment.Left);
			this.Columns.Add("", 60, HorizontalAlignment.Left);
			this.Columns.Add("", 60, HorizontalAlignment.Left);
			this.SelectedIndexChanged += new EventHandler(MapView_SelectedIndexChanged);
			new GeneralStrategy(this);
		}
		void MapView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedItems.Count != 0)
			{
				//ContainerListViewItem item = SelectedItems[0];
				//item.SubItems[0].ItemControl.Focus();
			}
		}
		public void AddItem(EntryBase item)
		{
			item.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (!e.Handled)
				{
					OnKeyDown(e);
				}
			};
			Items.Add(item);
			this.Invalidate();
		}
		public MapStrategy(Map map)
			: this()
		{
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				AddItem(new Entry(new MapViewer(pair.Key), new MapViewer(pair.Value)));
			}
		}
		public virtual Map GetMap()
		{
			Map map = new StrategyMap();
			foreach (EntryBase node in Items)
			{
				Map key = node.GetKey();
				Map value = node.GetValue();
				map[key] = value;
			}
			return map;
		}
	}
	public class GeneralStrategy
	{
	    public GeneralStrategy(Control control)
	    {
			control.GotFocus+=delegate
			{
				if (control.Size.Width < 20)
				{
					control.Size = new Size(800, 600);
				}
				if (control.Parent.Parent is TreeListView)
				{
					TreeListView view=((TreeListView)control.Parent.Parent);
					TreeListNode selected=null;
					foreach (TreeListNode node in view.Nodes)
					{
						Control key=node.SubItems[0].ItemControl;
						Control value=node.SubItems[1].ItemControl;
						if (object.ReferenceEquals(key,control.Parent)||
							object.ReferenceEquals(value, control.Parent))
						{
							selected = node;
							break;
						}
					}
					view.SelectedNodes.Clear();
					if (selected != null)
					{
						view.SelectedNodes.Add(selected);
					}
				}
			};
			KeyMapper m = new KeyMapper(control);
			m[Keys.Down] = delegate
			{
				Control c = control;
				while (true)
				{
					if (c.Parent == null)
					{
						break;
					}
					else if (c.Parent is MapStrategy)
					{
						//TreeListNode node=((MapView)c.Parent).SelectedNodes[0];
						//if (node != null)
						//{
						//    TreeListNode sibling = (TreeListNode)node.NextSibling();
						//    if (sibling != null)
						//    {
						//        node.Selected = false;
						//        sibling.Selected = true;
						//    }
						//}
					}
					c = c.Parent;
				}
			};
			m[Keys.Alt | Keys.K] = m[Keys.Down];
	    }
	}
	public class KeyMapper : Dictionary<Keys, MethodInvoker>
	{
		public KeyMapper(Control control)
		{
			control.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (ContainsKey(e.KeyData))
				{
					if (!e.Handled)
					{
						e.Handled = true;
						this[e.KeyData]();
					}
				}
			};
		}
	}
}

//public class LookupView : Panel, IStrategy
//{
//    private View View
//    {
//        get
//        {
//            return (View)Controls[0];
//        }
//        set
//        {
//            Controls.Clear();
//            value.Dock = DockStyle.Fill;
//            Controls.Add(value);
//        }
//    }
//    public Map GetMap()
//    {
//        return new StrategyMap(CodeKeys.Lookup, View.GetMap());
//    }
//    public LookupView(Map map)
//        : this(new View(map[CodeKeys.Lookup]))
//    {
//    }
//    public LookupView(View view)
//    {
//        this.Dock = DockStyle.Fill;
//        this.View = view;
//        //this.View = new View(view);
//        this.BackColor = Color.Orange;
//        this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
//        this.AutoSize = true;
//        this.Size = new Size(10, 10);
//        view.Controls[0].Focus();
//    }
//}
//public class CurrentNode : EntryBaseNode
//{
//    public override Map GetKey()
//    {
//        return null;
//    }
//    public override Map GetValue()
//    {
//        return null;
//    }
//    public CurrentNode(MapView mapView)
//    {
//        SubItems.Add(new Panel());
//        SubItems.Add(new View(new StringView("")));
//        this.BackColor = Color.Chartreuse;
//        this.UseItemStyleForSubItems = true;
//    }
//}
//public class ProgramView : MapView
//{
//    public override Map GetMap()
//    {
//        Map map = new StrategyMap();
//        foreach (EntryNode node in Nodes)
//        {
//            map.Append(new StrategyMap(node.GetKey(), node.GetValue()));
//        }
//        return new StrategyMap(CodeKeys.Program, map);
//    }
//    public ProgramView(Map map)
//    {
//        foreach (Map statement in map[CodeKeys.Program].Array)
//        {
//            this.Nodes.Add(new EntryNode(statement[CodeKeys.Key], statement[CodeKeys.Value], this));
//        }
//    }
//}
//public class CallView : TreeListView, IStrategy
//{
//    public Map GetMap()
//    {
//        Map map = new StrategyMap();
//        foreach (TreeListNode node in Nodes)
//        {
//            map.Append(((View)node.SubItems[0].ItemControl).GetMap());
//        }
//        return new StrategyMap(CodeKeys.Call, map);
//    }
//    public CallView()
//    {
//        this.BackColor = Color.Goldenrod;
//    }
//    public CallView(Map map)
//        : this()
//    {
//        foreach (Map entry in map[CodeKeys.Call].Array)
//        {
//            TreeListNode node = new TreeListNode();
//            node.SubItems.Add(new View(entry));
//            this.Nodes.Add(node);
//        }
//    }
//}
//public class SelectView : TreeListView, IStrategy
//{
//    public Map GetMap()
//    {
//        Map map = new StrategyMap();
//        foreach (TreeListNode node in Nodes)
//        {
//            map.Append(((View)node.SubItems[0].ItemControl).GetMap());
//        }
//        return new StrategyMap(CodeKeys.Select, map);
//    }
//    public SelectView()
//    {
//        this.BackColor = Color.HotPink;
//    }
//    public SelectView(Map map)
//    {
//        foreach (Map m in map[CodeKeys.Select].Array)
//        {
//            TreeListNode node = new TreeListNode();
//            node.SubItems.Add(new View(m));
//            Nodes.Add(node);
//        }
//    }
//}
//public class SearchNode : EntryBaseNode
//{
//    public override Map GetValue()
//    {
//        return null;
//    }
//    public override Map GetKey()
//    {
//        return null;
//    }
//    public SearchNode(MapView mapView)
//    {
//        this.BackColor = Color.Maroon;
//        SubItems.Add(new View(new SelectView()));
//        SubItems.Add(new View(new StringView("")));
//    }
//}
//public class FunctionNode : EntryBaseNode, IStrategy
//{
//    public override Map GetKey()
//    {
//        return CodeKeys.Function;
//    }
//    public override Map GetValue()
//    {
//        return ((View)SubItems[1].ItemControl).GetMap();
//    }
//    public Map GetMap()
//    {
//        return null;
//    }
//    public FunctionNode(Map map, MapView mapView)
//        : this(new View(map), mapView)
//    {
//    }
//    public FunctionNode(Control view, MapView mapView)
//    {
//        SubItems.Add(new View(new StringView("")));
//        SubItems.Add(view);
//        this.BackColor = Color.Green;
//        this.UseItemStyleForSubItems = true;
//    }
//}