using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Meta;
using System.Drawing.Drawing2D;
using Aga.Controls.Tree;

namespace Editor
{
	public partial class Editor : Form
	{
		public Element selected;
		public Editor()
		{
			InitializeComponent();
			TreeModel model = new TreeModel();
			Node root = new Node("Root");
			root.Nodes.Add(new Node("hello"));
			model.Nodes.Add(root);
			//TreeViewAdv tree = new TreeViewAdv();
			tree.Model = model;
			tree.Visible = true;
			//this.Controls.Add(tree);

			LoadFile(@"D:\Meta\0.2\Test\basic.meta");
		}
		//Map map;
		//string text;
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fileDialog.ShowDialog();
			LoadFile(fileDialog.FileName);
		}
		private void LoadFile(string fileName)
		{
			Map map = Parser.Parse(@"D:\Meta\0.2\Test\editTest.meta");
			//map = Binary.Deserialize(fileName);
			//text = map.ToString();
			editor = new Element(map);
			//panel.Controls.Add(editor);
		}
		private Element editor;
		private void panel_Paint(object sender, PaintEventArgs e)
		{
			//if (editor != null)
			//{
			//    Point position = new Point(10, 10);
			//    editor.Draw(e.Graphics, ref position);
			//}
		}

		public abstract class Display:Control
		{
			public virtual Size Size
			{
				get
				{
					return new Size(18, 200);
				}
			}
			public virtual Map GetMap()
			{
				return element.map;
			}
			protected Element element;
			public Display(Element element)
			{
				this.element = element;
			}
			//protected Size Measure(string text)
			//{
			//}
			protected void Draw(Graphics graphics, ref Point position, string text)
			{
				graphics.DrawString(text, new Font("Arial", 10), Brushes.Black, position);
				position.Y += 18;
			}
			public abstract void Draw(Graphics graphics, ref Point position);
		}
		public class Element:Control
		{
			public Map map;
			Display display;

			public Size Size
			{
				get
				{
					return display.Size;
				}
			}

			public void Draw(Graphics graphics, ref Point position)
			{
				display.Draw(graphics, ref position);
			}
			public Map GetMap()
			{
				return display.GetMap();
			}
			public Element(Map map)
			{
				this.map = map;
				if (map.Count == 0)
				{
					display = new EmptyMapElement(this);
				}
				if (map.IsNumber)
				{
					display = new NumberElement(this);
				}
				else if (map.IsString)
				{
					display = new StringElement(this);
				}
				else
				{
					display = new MapElement(this);
				}
				this.Controls.Add(display);
			}
		}
		//public abstract class Element
		//{
		//}
		public class StringElement : Display
		{
			public StringElement(Element element)
				: base(element)
			{
			}
			//public override Size Size
			//{
			//    get 
			//    { 
			//        return 
			//    }
			//}
			public override void Draw(Graphics graphics, ref Point position)
			{
				Draw(graphics, ref position, '"' + element.map.GetString() + '"');
			}
			//public override Map GetMap()
			//{
			//    return new StrategyMap(text);
			//}
		}
		public class NumberElement : Display
		{
			public NumberElement(Element element)
				: base(element)
			{
			}
			public override void Draw(Graphics graphics, ref Point position)
			{
				Draw(graphics, ref position, element.map.ToString());
			}
		}

		public class EmptyMapElement : Display
		{
			public EmptyMapElement(Element element)
				: base(element)
			{
			}
			public override void Draw(Graphics graphics, ref Point position)
			{
				Draw(graphics, ref position, "*");
			}
		}
		public class MapElement : Display
		{
			//private Map map;
			//public override Map GetMap()
			//{
			//    Map map = new StrategyMap();
			//    foreach (Entry element in elements)
			//    {
			//        KeyValuePair<Map, Map> entry = element.GetEntry();
			//        map[entry.Key] = entry.Value;
			//    }
			//    return map;
			//}
			public MapElement(Element element)
				: base(element)
			{
				foreach (KeyValuePair<Map, Map> entry in element.map)
				{
					Entry e=new Entry(entry.Key, entry.Value);
					elements.Add(e);
					this.Controls.Add(e);
				}
			}
			private List<Entry> elements = new List<Entry>();
			public override void Draw(Graphics graphics, ref Point position)
			{
				foreach (Entry entry in elements)
				{
					entry.Draw(graphics, ref position);
				}
			}
		}
		public class Entry:Control
		{
			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);
			}
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
			}
			private Element key;
			private Element value;
			public Entry(Map key, Map value)
			{
				this.key = new Element(key);
				this.value = new Element(value);
			}
			private Pen pen = new Pen(Brushes.Blue);
			public void Draw(Graphics graphics, ref Point position)
			{
				Point oldPos = position;
				key.Draw(graphics, ref position);
				Point middlePos = position;
				value.Draw(graphics, ref position);
				graphics.DrawRectangle(pen, new Rectangle(oldPos, new Size(200, middlePos.Y - oldPos.Y)));
				graphics.DrawRectangle(pen, new Rectangle(middlePos, new Size(200, position.Y - middlePos.Y)));
				position.Y += 18;
			}
			public KeyValuePair<Map, Map> GetEntry()
			{
				return new KeyValuePair<Map, Map>(key.GetMap(), value.GetMap());
			}
		}
		//public class MapControl:Control
		//{
		//    public Map map;
		//    public MapControl(Map map)
		//    {
		//        this.Dock = DockStyle.Fill;
		//        this.map = map;
		//    }
		//    protected override void OnPaint(PaintEventArgs e)
		//    {
		//        int count=0;
		//        foreach (KeyValuePair<Map, Map> entry in map)
		//        {
		//            Brush brush;
		//            if(count==selected)
		//            {
		//                brush=Brushes.Red;
		//            }
		//            else
		//            {
		//                brush=Brushes.Black;
		//            }
		//            e.Graphics.DrawString(entry.Key.ToString(), Font, brush, new Point(20, count * (Font.Height + 5)+100));
		//            count++;
		//        }
		//    }
		//    int selected = 0;
		//    protected override void OnClick(EventArgs e)
		//    {
		//        EditString("hello");
		//    }
		//    public string EditString(string text)
		//    {
		//        Form form = new Form();
		//        TextBox box = new TextBox();
		//        box.Dock = DockStyle.Fill;
		//        box.Text = text;
		//        form.Controls.Add(box);
		//        form.ShowDialog();
		//        return box.Text;
		//    }
		//}
	}
}