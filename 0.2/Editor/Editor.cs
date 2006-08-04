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
			LoadFile(@"D:\Meta\0.2\Test\basic.meta");
		}
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fileDialog.ShowDialog();
			LoadFile(fileDialog.FileName);
		}
		private void LoadFile(string fileName)
		{
			Map map = Parser.Parse(@"D:\Meta\0.2\Test\editTest.meta");
			View view = new View(map);
			//View view = new View(new MapView(map));
			//MapView view = new MapView(map);
			view.Dock = DockStyle.Fill;
			this.Controls.Add(view);
		}
		public class Shortcuts:Dictionary<Keys, MethodInvoker>
		{
			public bool Evaluate(KeyEventArgs e)
			{
				bool contains=ContainsKey(e.KeyData);
				if(contains)
				{
					this[e.KeyData]();
				}
				return contains;
			}
		}
		public class View:Panel
		{
			public View(Map map)
			{
				this.AutoSize = true;
				//Control control = GetView(map);
				//this.Controls.Add(control);
				Control = GetView(map);
				Control.Dock = DockStyle.Fill;
				shortcuts[Keys.Alt | Keys.S] = delegate
				{
					Control = new StringView("");
					Control.Focus();
				};
				shortcuts[Keys.Alt | Keys.N] = delegate
				{
					Control = new NumberView(0);
					Control.Focus();
				};
				shortcuts[Keys.Alt | Keys.M] = delegate
				{
					TreeListView view = new MapView(new StrategyMap("hello","world"));
					view.Columns.Add("hello", 100, HorizontalAlignment.Left);
					//view.Size = new Size(100, 100);
					//view.Nodes.Add(node);
					Control = view;
					Control.BringToFront();
					Control.Focus();
				};
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
					value.KeyDown += new KeyEventHandler(control_KeyDown);
					Controls.Add(value);
				}
			}
			private Shortcuts shortcuts = new Shortcuts();
			//private Dictionary<Keys, MethodInvoker> shortcuts = new Dictionary<Keys, MethodInvoker>();
			void control_KeyDown(object sender, KeyEventArgs e)
			{
				//if (shortcuts.ContainsKey(e.KeyData))
				//{
				//    shortcuts[e.KeyData]();
				//}
				//else
				//{
				if (!shortcuts.Evaluate(e))
				{
					OnKeyDown(e);
				}
				//}
			}
			private Control GetView(Map map)
			{
				if (map.IsNumber)
				{
					return new NumberView(map.GetNumber());
				}
				else if (map.IsString)
				{
					return new StringView(map.GetString());
				}
				else
				{
					return new MapView(map);
				}
			}
			public class MapView : TreeListView, IMapView
			{
				protected override void OnGotFocus(EventArgs e)
				{
					this.Size = new Size(100, 100);
				}
				public MapView(Map map)
				{
					//this.Dock = DockStyle.Fill;
					this.Columns.Add("", 0, HorizontalAlignment.Left);
					this.Columns.Add("key", 100, HorizontalAlignment.Left);
					this.Columns.Add("value", 200, HorizontalAlignment.Left);
					foreach (KeyValuePair<Map, Map> pair in map)
					{
						this.Nodes.Add(new EntryNode(pair.Key,pair.Value,this));
					}
					shortcuts[Keys.Enter|Keys.Control]=delegate
					{
						Nodes.Add(new EntryNode("", "",this));
						this.Invalidate();
					};
				}
				Shortcuts shortcuts = new Shortcuts();
				public void view_KeyDown(object sender, KeyEventArgs e)
				{
					shortcuts.Evaluate(e);

				}
				public class EntryNode : TreeListNode
				{
					public EntryNode(Map key,Map value,MapView view)
					{
						SubItems.Add(GetView(key,view));
						SubItems.Add(GetView(value,view));
					}
					private Control GetView(Map map,MapView parent)
					{
						Control view = new View(map);
						view.KeyDown += new KeyEventHandler(parent.view_KeyDown);
						return view;
					}
				}
				//private TreeListNode NewNode(Map key, Map value)
				//{
				//    TreeListNode node = new TreeListNode();
				//    node.SubItems.Add(NewView(key));
				//    node.SubItems.Add(NewView(value));
				//    return node;
				//}
				//protected override void OnKeyDown(KeyEventArgs e)
				//{
				//    if (e.KeyCode == Keys.Enter)
				//    {
				//        TreeListNode selected = this.SelectedNodes[0];
				//        TreeListNode node = NewNode("", "");
				//        Nodes.Add(node);
				//    }
				//}
				public Map GetMap()
				{
					Map map = new StrategyMap();
					foreach (TreeListNode node in nodes)
					{
						Map key = ((IMapView)node.SubItems[0]).GetMap();
						Map value = ((IMapView)node.SubItems[0]).GetMap();
						map[key] = value;
					}
					return map;
				}
			}
			public interface IMapView
			{
				Map GetMap();
				//void Keydown(KeyEventArgs e);
			}
			public class StringView : TextBox, IMapView
			{
				protected override void OnGotFocus(EventArgs e)
				{
					//base.OnGotFocus(e);
				}
				public StringView(string text)
				{
					this.BackColor = Color.LightBlue;
					this.Text = text;
				}
				public Map GetMap()
				{
					return new StrategyMap(this.Text);
				}
			}
			public class NumberView : MaskedTextBox, IMapView
			{
				protected override void OnKeyDown(KeyEventArgs e)
				{
					if (char.IsDigit(Convert.ToChar(e.KeyValue)) || e.Control)
					{
						e.Handled = false;
					}
					else
					{
						//e.Handled = true;
						e.SuppressKeyPress = true;
					}
					base.OnKeyDown(e);
				}
				private Number number;
				public NumberView(Number number)
				{
					this.BackColor = Color.LightCyan;
					this.number = number;
				}
				public Map GetMap()
				{
					return new StrategyMap(Convert.ToInt32(this.Text));
				}
			}
		}
	}
}