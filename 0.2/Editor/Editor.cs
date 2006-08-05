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
			mainView = new View(map);
			mainView.Dock = DockStyle.Fill;
			mainView.Controls[0].Dock = DockStyle.Fill;
			if (Controls.Count > 1)
			{
				this.Controls.RemoveAt(1);
			}
			this.Controls.Add(mainView);
			this.path = path;
		}
		View mainView;
		private string path;
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fileDialog.ShowDialog();
			LoadFile(fileDialog.FileName);
		}
		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Map map = mainView.GetMap();
			Interpreter.Init();
			map.Call(Map.Empty, new Position(new Position(RootPosition.rootPosition, "filesystem"), "localhost"));
		}
		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Binary.Serialize(mainView.GetMap(), path);
		}
	}
	public interface IStrategy
	{
		Map GetMap();
	}
	public class View : Panel
	{
		public Map GetMap()
		{
			return ((IStrategy)Control).GetMap();
		}
		public View(Control control)
		{
			this.AutoSize = true;
			this.Control = control;
			KeyMapper m = new KeyMapper(control);
			m[Keys.Alt | Keys.S]=delegate { 
				Control=new StringView(""); };
			m[Keys.Alt | Keys.E]=delegate { Control=new EmptyMapView(); };
			m[Keys.Alt | Keys.N]=delegate { Control=new NumberView(0); };
			m[Keys.Alt | Keys.L]=delegate { Control=new LookupView(new View(new StringView(""))); };
			m[Keys.Alt | Keys.M]=delegate { Control=new MapView(new StrategyMap("", "")); };
		}
		public View(Map map):this(GetView(map))
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
				value.KeyDown += delegate(object sender, KeyEventArgs e)
				{
					if (e.KeyData == (Keys.S | Keys.Alt))
					{
					}
					if (!e.Handled)
					{
						OnKeyDown(e);
					}
				};
				Controls.Add(value);
				value.Focus();
			}
		}
		private static Control GetView(Map map)
		{
			if (map.Count == 0)
			{
				return new EmptyMapView();
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
				if (map.ContainsKey(CodeKeys.Lookup))
				{
					return new LookupView(map);
				}
				else if (map.ContainsKey(CodeKeys.Select))
				{
					return new SelectView(map);
				}
				else if (map.ContainsKey(CodeKeys.Call))
				{
					return new CallView(map);
				}
			}
			return new MapView(map);
		}
	}
	public class CurrentNode : EntryBaseNode
	{
		public override Map GetKey()
		{
			return null;
		}
		public override Map GetValue()
		{
			return null;
		}
		public CurrentNode(MapView mapView)
		{
			SubItems.Add(new Panel());
			SubItems.Add(new View(new StringView("")));
			this.BackColor = Color.Chartreuse;
			this.UseItemStyleForSubItems = true;
		}
	}
	public class ProgramView : MapView
	{
		public override Map GetMap()
		{
			Map map = new StrategyMap();
			foreach (EntryNode node in Nodes)
			{
				map.Append(new StrategyMap(node.GetKey(), node.GetValue()));
			}
			return new StrategyMap(CodeKeys.Program, map);
		}
		public ProgramView(Map map)
		{
			foreach (Map statement in map[CodeKeys.Program].Array)
			{
				this.Nodes.Add(new EntryNode(statement[CodeKeys.Key], statement[CodeKeys.Value], this));
			}
		}
	}
	public class CallView : TreeListView, IStrategy
	{
		public Map GetMap()
		{
			Map map = new StrategyMap();
			foreach (TreeListNode node in Nodes)
			{
				map.Append(((View)node.SubItems[0].ItemControl).GetMap());
			}
			return new StrategyMap(CodeKeys.Call, map);
		}
		public CallView()
		{
			this.BackColor = Color.Goldenrod;
		}
		public CallView(Map map)
			: this()
		{
			foreach (Map entry in map[CodeKeys.Call].Array)
			{
				TreeListNode node = new TreeListNode();
				node.SubItems.Add(new View(entry));
				this.Nodes.Add(node);
			}
		}
	}
	public class SelectView : TreeListView, IStrategy
	{
		public Map GetMap()
		{
			Map map = new StrategyMap();
			foreach (TreeListNode node in Nodes)
			{
				map.Append(((View)node.SubItems[0].ItemControl).GetMap());
			}
			return new StrategyMap(CodeKeys.Select, map);
		}
		public SelectView()
		{
			this.BackColor = Color.HotPink;
		}
		public SelectView(Map map)
		{
			foreach (Map m in map[CodeKeys.Select].Array)
			{
				TreeListNode node = new TreeListNode();
				node.SubItems.Add(new View(m));
				Nodes.Add(node);
			}
		}
	}
	public class SearchNode : EntryBaseNode
	{
		public override Map GetValue()
		{
			return null;
		}
		public override Map GetKey()
		{
			return null;
		}
		public SearchNode(MapView mapView)
		{
			this.BackColor = Color.Maroon;
			SubItems.Add(new View(new SelectView()));
			SubItems.Add(new View(new StringView("")));
		}
	}
	public class FunctionNode : EntryBaseNode, IStrategy
	{
		public override Map GetKey()
		{
			return CodeKeys.Function;
		}
		public override Map GetValue()
		{
			return ((View)SubItems[1].ItemControl).GetMap();
		}
		public Map GetMap()
		{
			return null;
		}
		public FunctionNode(Map map, MapView mapView)
			: this(new View(map), mapView)
		{
		}
		public FunctionNode(Control view, MapView mapView)
		{
			SubItems.Add(new View(new StringView("")));
			SubItems.Add(view);
			this.BackColor = Color.Green;
			this.UseItemStyleForSubItems = true;
		}
	}
	public abstract class EntryBaseNode : TreeListNode
	{
		public event KeyEventHandler KeyDown;
		public abstract Map GetKey();
		public abstract Map GetValue();
		protected void AddView(Map map)
		{
			View view = new View(map);
			view.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				KeyDown(sender, e);
			};
			SubItems.Add(view);
		}
	}
	public class EntryNode : EntryBaseNode
	{
		public override Map GetKey()
		{
			return ((View)this.SubItems[0].ItemControl).GetMap();
		}
		public override Map GetValue()
		{
			return ((View)this.SubItems[1].ItemControl).GetMap();
		}

		public EntryNode(Map key, Map value, MapView view)
		{
			AddView(key);
			AddView(value);
		}
	}
	public class StringView : TextBox, IStrategy
	{
		public StringView(string text)
		{
			new Extender(this);
			this.BackColor = Color.LightBlue;
			this.Text = text;
		}
		public Map GetMap()
		{
			return new StrategyMap(this.Text);
		}
	}
	public class EmptyMapView : TextBox, IStrategy
	{
		public Map GetMap()
		{
			return Map.Empty;
		}
		public EmptyMapView()
		{
			new Extender(this);
			this.ReadOnly = true;
			this.BackColor = Color.Red;
		}
	}
	public class LookupView : Panel, IStrategy
	{
		private View View
		{
			get
			{
				return (View)Controls[0];
			}
			set
			{
				Controls.Clear();
				value.Dock = DockStyle.Fill;
				Controls.Add(value);
			}
		}
		public Map GetMap()
		{
			return new StrategyMap(CodeKeys.Lookup, View.GetMap());
		}
		public LookupView(Map map)
			: this(new View(map[CodeKeys.Lookup]))
		{
		}
		public LookupView(View view)
		{
			this.Dock = DockStyle.Fill;
			this.View = view;
			//this.View = new View(view);
			this.BackColor = Color.Orange;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.AutoSize = true;
			this.Size = new Size(10, 10);
			view.Controls[0].Focus();
		}
	}
	public class NumberView : MaskedTextBox, IStrategy
	{
		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.SuppressKeyPress = !char.IsDigit(Convert.ToChar(e.KeyValue)) || !char.IsControl(Convert.ToChar(e.KeyValue));
			base.OnKeyDown(e);
		}
		private Number number;
		public NumberView(Number number)
		{
			new Extender(this);
			this.BackColor = Color.LightCyan;
			this.number = number;
			this.Text = number.ToString();
		}
		public Map GetMap()
		{
			return new StrategyMap(Convert.ToInt32(this.Text));
		}
	}
	public class MapView : TreeListView, IStrategy
	{
		public MapView()
		{
			this.Size = new Size(10, 10);
			this.Columns.Add("", 0, HorizontalAlignment.Left);
			this.Columns.Add("", 100, HorizontalAlignment.Left);
			this.Columns.Add("", 200, HorizontalAlignment.Left);
			this.SelectedIndexChanged += new EventHandler(MapView_SelectedIndexChanged);

			KeyMapper m = new KeyMapper(this);
			m[Keys.Enter | Keys.Control] = delegate
			{
				AddNode(new EntryNode("", "", this));
				//Nodes.Add(new EntryNode("", "", this));
				//this.Invalidate();
			};
			m[Keys.Enter | Keys.Control | Keys.Shift] = delegate
			{
				AddNode(new FunctionNode(Map.Empty, this));
			};
		}
		void MapView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedNodes.Count != 0)
			{
				TreeListNode node = SelectedNodes[0];
				node.SubItems[0].ItemControl.Focus();
			}
		}
		private void AddNode(EntryBaseNode node)
		{
			node.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (!e.Handled)
				{
					OnKeyDown(e);
				}
			};
			this.Invalidate();
			Nodes.Add(node);
		}
		public MapView(Map map)
			: this()
		{
			foreach (KeyValuePair<Map, Map> pair in map)
			{
				//TreeListNode node;
				if (pair.Key.Equals(CodeKeys.Function))
				{
				    AddNode(new FunctionNode(pair.Value,this));
				}
				else
				{
					AddNode(new EntryNode(pair.Key, pair.Value, this));
				}
				//this.Nodes.Add(node);
			}
		}
		public virtual Map GetMap()
		{
			Map map = new StrategyMap();
			foreach (TreeListNode node in nodes)
			{
				Map key = ((EntryBaseNode)node).GetKey();
				Map value = ((EntryBaseNode)node).GetValue();
				map[key] = value;
			}
			return map;
		}
	}
	public class Extender
	{
	    public Extender(Control control)
	    {
			control.GotFocus+=delegate
			{
				//foreach (TreeListNode node in ((MapView)control.Parent.Parent).Nodes)
				//{
				//    Control c=node.SubItems[1].ItemControl;
				//    if (c is IStrategy)
				//    {
				//        if (c.Size.Height > 30)
				//        {
				//            c.Controls[0].Size = new Size(10, 10);
				//        }
				//    }
				//}
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
					else if (c.Parent is MapView)
					{
						TreeListNode node=((MapView)c.Parent).SelectedNodes[0];
						if (node != null)
						{
							TreeListNode sibling = (TreeListNode)node.NextSibling();
							if (sibling != null)
							{
								node.Selected = false;
								sibling.Selected = true;
							}
						}
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
				if (!e.Handled && ContainsKey(e.KeyData))
				{
					e.Handled = true;
					this[e.KeyData]();
				}
			};
		}
	}
}