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
			LoadFile(@"D:\Meta\0.2\Test\edit.meta");
		}
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fileDialog.ShowDialog();
			LoadFile(fileDialog.FileName);
		}
		View mainView;
		private void LoadFile()
		{
			if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				LoadFile(fileDialog.FileName);
			}
		}
		private void Save()
		{
			Binary.Serialize(mainView.GetMap(),path);
		}
		private void LoadTextFile(string path)
		{
			LoadFile(Parser.Parse(path));

			this.path = path;
		}
		private string path;
		private void LoadFile(Map map)
		{
			mainView = new View(map);
			mainView.Dock = DockStyle.Fill;
			mainView.Controls[0].Dock = DockStyle.Fill;
			if (Controls.Count > 1)
			{
				this.Controls.RemoveAt(1);
			}
			this.Controls.Add(mainView);
		}
		private void LoadFile(string path)
		{
			LoadFile(Binary.Deserialize(path));
			this.path = path;
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
		public interface IMapView
		{
			void Collapse();
			Map GetMap();
		}
		public class View : Panel,IMapView
		{
			public void Collapse()
			{
				((IMapView)Control).Collapse();
			}
			public Map GetMap()
			{
				return ((IMapView)Control).GetMap();
			}
			public View(Control control)
			{
				this.AutoSize = true;
				this.Control = control;
				if (!(control is MapView))
				{
					Control.Dock = DockStyle.Fill;
				}
				else
				{
				}
				shortcuts[Keys.Alt | Keys.S] = delegate
				{
					Control = new StringView("");
					Control.Focus();
				};
				shortcuts[Keys.Alt | Keys.E] = delegate
				{
					Control = new EmptyMapView();
					Control.Focus();
				};
				shortcuts[Keys.Alt | Keys.N] = delegate
				{
					Control = new NumberView(0);
					Control.Focus();
				};
				shortcuts[Keys.Alt | Keys.L] = delegate
				{
					Control = new LookupView(new View(new StringView("")));
				};
				shortcuts[Keys.Alt | Keys.M] = delegate
				{
					TreeListView view = new MapView(new StrategyMap("", ""));
					Control = view;
					Control.Focus();
				};
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
					value.KeyDown += new KeyEventHandler(control_KeyDown);
					Controls.Add(value);
				}
			}
			private Shortcuts shortcuts = new Shortcuts();
			void control_KeyDown(object sender, KeyEventArgs e)
			{
				if (!shortcuts.Evaluate(e))
				{
					OnKeyDown(e);
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
				else if(map.Count==1)
				{
					if(map.ContainsKey(CodeKeys.Lookup))
					{
						return new LookupView(map);
					}
					else if(map.ContainsKey(CodeKeys.Select))
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
			public class MapView : TreeListView, IMapView
			{
				public void Collapse()
				{
					this.Size = new Size(10, 10);
				}
				protected override void OnGotFocus(EventArgs e)
				{
					this.Size = new Size(100, 100);
				}
				public MapView()
				{
					this.Size = new Size(10, 10);
					this.Columns.Add("", 0, HorizontalAlignment.Left);
					this.Columns.Add("key", 100, HorizontalAlignment.Left);
					this.Columns.Add("value", 200, HorizontalAlignment.Left);
					this.ItemHeight = 30;

					shortcuts[Keys.Enter | Keys.Control] = delegate
					{
						Nodes.Add(new EntryNode("", "", this));
						this.Invalidate();
					};
					shortcuts[Keys.Enter | Keys.Control | Keys.Shift] = delegate
					{
						Nodes.Add(new FunctionNode(Map.Empty,this));
						this.Invalidate();
					};
				}
				public MapView(Map map):this()
				{
					foreach (KeyValuePair<Map, Map> pair in map)
					{
						TreeListNode node;
						//if (pair.Key.Equals(CodeKeys.Function))
						//{
						//    node = new FunctionNode(pair.Value,this);
						//}
						//else
						//{
							node = new EntryNode(pair.Key, pair.Value, this);
						//}
						this.Nodes.Add(node);
					}
				}
				Shortcuts shortcuts = new Shortcuts();
				public void view_KeyDown(object sender, KeyEventArgs e)
				{
					shortcuts.Evaluate(e);
				}
				public Map GetMap()
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
					SubItems.Add(WireView(new View(new StringView("")), mapView));
					this.BackColor = Color.Chartreuse;
					this.UseItemStyleForSubItems=true;
				}
			}
			public class ProgramView : MapView
			{
				public Map GetMap()
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
			public class CallView : TreeListView, IMapView
			{
				public void Collapse()
				{
					Size = new Size(10, 10);
				}
				public Map GetMap()
				{
					Map map = new StrategyMap();
					foreach (TreeListNode node in Nodes)
					{
						map.Append(((IMapView)node.SubItems[0].ItemControl).GetMap());
					}
					return new StrategyMap(CodeKeys.Call, map);
				}
				public CallView()
				{
					this.BackColor = Color.Goldenrod;
				}
				public CallView(Map map):this()
				{
					foreach(Map entry in map[CodeKeys.Call].Array)
					{
						TreeListNode node = new TreeListNode();
						node.SubItems.Add(new View(GetView(entry)));
						this.Nodes.Add(node);
					}
				}
			}
			public class SelectView : TreeListView, IMapView
			{
				public void Collapse()
				{
					Size = new Size(10, 10);
				}
				public Map GetMap()
				{
					Map map = new StrategyMap();
					foreach (TreeListNode node in Nodes)
					{
						map.Append(((IMapView)node.SubItems[0].ItemControl).GetMap());
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
						node.SubItems.Add(GetView(m));
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
			public class FunctionNode : EntryBaseNode,IMapView
			{
				public override Map GetKey()
				{
					return CodeKeys.Function;
				}
				public override Map GetValue()
				{
					return ((IMapView)SubItems[1].ItemControl).GetMap();
				}
				public Map GetMap()
				{
					return null;
				}
				public FunctionNode(Map map,MapView mapView):this(WireView(map,mapView),mapView)
				{
				}
				public FunctionNode(Control view,MapView mapView)
				{
					SubItems.Add(new View(new StringView("")));
					SubItems.Add(view);
					this.BackColor = Color.Green;
					this.UseItemStyleForSubItems = true;
				}
			}
			public abstract class EntryBaseNode : TreeListNode
			{
				public abstract Map GetKey();
				public abstract Map GetValue();
				protected static Control WireView(Map map, MapView parent)
				{
					return WireView(new View(map), parent);
				}
				protected static Control WireView(Control control, MapView parent)
				{
					control.KeyDown += new KeyEventHandler(parent.view_KeyDown);
					return control;
				}
			}
			public class EntryNode : EntryBaseNode
			{
				public override Map GetKey()
				{
					return ((IMapView)this.SubItems[0].ItemControl).GetMap();
				}
				public override Map GetValue()
				{
					return ((IMapView)this.SubItems[1].ItemControl).GetMap();
				}
				public EntryNode(Map key, Map value, MapView view)
				{
					SubItems.Add(WireView(key, view));
					SubItems.Add(WireView(value, view));
				}

			}
			public class StringView : TextBox, IMapView
			{

				public void Collapse()
				{
					Size = new Size(10, 10);
				}
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
			public class EmptyMapView : TextBox, IMapView
			{
				public void Collapse()
				{
					Size = new Size(10, 10);
				}
				public Map GetMap()
				{
					return Map.Empty;
				}
				public EmptyMapView()
				{
					this.ReadOnly = true;
					this.BackColor = Color.Red;
				}
			}
			public class LookupView : Panel, IMapView
			{
				public void Collapse()
				{
					Size = new Size(10, 10);
				}
				protected override void OnGotFocus(EventArgs e)
				{
					this.Size = new Size(100, 100);
				}
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
					: this(GetView(map[CodeKeys.Lookup]))
				{
				}
				public LookupView(Control view)
				{
					this.Dock = DockStyle.Fill;
					this.View = new View(view);
					this.BackColor = Color.Orange;
					this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
					this.AutoSize = true;
					this.Size = new Size(10, 10);
					view.Controls[0].Focus();
				}
			}

			public class NumberView : MaskedTextBox, IMapView
			{
				public void Collapse()
				{
				}
				protected override void OnKeyDown(KeyEventArgs e)
				{
					if (char.IsDigit(Convert.ToChar(e.KeyValue)) || char.IsControl(Convert.ToChar(e.KeyValue)))
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
					this.Text = number.ToString();
				}
				public Map GetMap()
				{
					return new StrategyMap(Convert.ToInt32(this.Text));
				}
			}
		}

		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Map map=mainView.GetMap();
			Interpreter.Init();
			map.Call(Map.Empty, new Position(new Position(RootPosition.rootPosition, "filesystem"), "localhost"));
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Save();
		}
	}
}