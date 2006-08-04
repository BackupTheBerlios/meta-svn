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
		//TreeModel model = new TreeModel();

		public Editor()
		{
			InitializeComponent();
			//TreeModel model = new TreeModel();
			//Node root = new Node("Root");
			//root.Nodes.Add(new Node("hello"));
			//model.Nodes.Add(root);
			//tree.Model = model;
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
			View view = new View(new MapView(map));
			//MapView view = new MapView(map);
			view.Dock = DockStyle.Fill;
			this.Controls.Add(view);
		}
		public class View:Panel
		{
			private Control control;
			public View(Control control)
			{
				this.AutoSize = true;
				this.control = control;
				control.Dock = DockStyle.Fill;
				this.Controls.Add(control);
			}
		}
		public class MapView : TreeListView
		{
			public Map GetMap()
			{
				Map map=new StrategyMap();
				foreach (TreeListNode node in nodes)
				{
					Map key=((IMapControl)node.SubItems[0]).GetMap();
					Map value = ((IMapControl)node.SubItems[0]).GetMap();
					map[key] = value;
				}
				return map;
			}
			public MapView(Map map)
			{
				this.Columns.Add("", 0 , HorizontalAlignment.Left);
				this.Columns.Add("", 50, HorizontalAlignment.Left);
				this.Columns.Add("key", 100, HorizontalAlignment.Left);
				this.Columns.Add("value", 100, HorizontalAlignment.Left);

				foreach (KeyValuePair<Map, Map> pair in map)
				{
					this.Nodes.Add(NewNode(pair.Key.GetString(),pair.Value.ToString()));
				}
			}
			private TreeListNode NewNode(string key,string value)
			{
				TreeListNode node = new TreeListNode();
				node.SubItems.Add(new ContainerSubListViewItem());
				node.SubItems.Add( new StringView(key));
				node.SubItems.Add(new StringView(value));

				//node.SubItems.Add(new StringView(key));
				//node.SubItems.Add(new StringView(value));
				return node;
			}
			protected override void OnKeyDown(KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Enter)
				{
					TreeListNode selected=this.SelectedNodes[0];
					TreeListNode node=NewNode("","");
					Nodes.Add(node);
					//List<TreeListNode> n = new List<TreeListNode>();

					//n.AddRange(nodes);
					//nodes.Clear();
					//n.Insert(selected.Index + 1, node);
					//nodes.AddRange(n.ToArray());
				}
			}
		}
		public interface IMapControl
		{
			Map GetMap();
			//void Keydown(KeyEventArgs e);
		}
		public class StringView : TextBox,IMapControl
		{
			public StringView(string text)
			{
				this.BackColor = Color.LightBlue;
				this.Text = text;
			}
			public Map GetMap()
			{
				return new StrategyMap(this.Text);
			}
			//protected override void OnKeyDown(KeyEventArgs e)
			//{
			//    Keydown(e);
			//}
			//public void Keydown(KeyEventArgs e)
			//{
			//    if (e.KeyCode == Keys.Enter)
			//    {
			//        if (this.Parent is IMapControl)
			//        {
			//            ((IMapControl)this.Parent).Keydown(e);
			//            //this.Parent.Focus();
			//        }
			//    }
			//}
		}
		public class NumberView : MaskedTextBox, IMapControl
		{
			//public void Keydown(KeyEventArgs e)
			//{
			//    //if (e.KeyCode == Keys.Enter)
			//    //{
			//    //    ((IMapControl)Parent).Keydown(e);
			//    //}
			//}
			//protected override void OnKeyDown(KeyEventArgs e)
			//{
			//    Keydown(e);
			//}
			public NumberView()
			{
				this.BackColor = Color.LightCyan;
			}
			public Map GetMap()
			{
				return new StrategyMap(Convert.ToInt32(this.Text));
			}
		}
	}
}