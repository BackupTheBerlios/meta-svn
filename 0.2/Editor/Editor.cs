using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Meta;
using System.Drawing.Drawing2D;

namespace Editor
{
	public partial class Editor : Form
	{
		public Editor()
		{
			InitializeComponent();
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
			data = new MapElement(map);
			//MapControl control = new MapControl(map);
			//control.Location = new Point(100, 100);
			//this.Controls.Add(control);
		}
		private Element data;
		private void panel_Paint(object sender, PaintEventArgs e)
		{
			if (data != null)
			{
				Point position = new Point(10, 10);
				data.Draw(e.Graphics, ref position);
			}
		}
	}
	public abstract class Element
	{
		protected void Draw(Graphics graphics, ref Point position, string text)
		{
			graphics.DrawString(text,new Font("Arial",10),Brushes.Black,position);
			position.Y += 18;

		}

		public abstract void Draw(Graphics graphics,ref Point position);
		public abstract Map GetMap();
		//public abstract void Edit();
		public static Element GetElement(Map map)
		{
			if (map.Count == 0)
			{
				return new EmptyMapElement();
			}
			if (map.IsNumber)
			{
				return new NumberElement(map);
			}
			else if (map.IsString)
			{
				return new StringElement(map);
			}
			else
			{
				return new MapElement(map);
			}
		}
	}
	public class StringElement : Element
	{
		string text;
		public StringElement(Map map)
		{
			this.text=map.GetString();
		}
		public override void Draw(Graphics graphics, ref Point position)
		{
			Draw(graphics, ref position, '"' + text + '"');
		}
		public override Map GetMap()
		{
			return new StrategyMap(text);
		}
	}
	public class NumberElement:Element
	{
		private Number number;
		public NumberElement(Map map)
		{
			this.number = map.GetNumber();
		}
		public override void Draw(Graphics graphics, ref Point position)
		{
			Draw(graphics,ref position,number.ToString());
		}
		public override Map GetMap()
		{
			return new StrategyMap(number);
		}
	}
	public class Entry
	{
		private Element key;
		private Element value;
		public Entry(Map key,Map value)
		{
			this.key=Element.GetElement(key);
			this.value = Element.GetElement(value);
		}
		private Pen pen = new Pen(Brushes.Blue);
		public void Draw(Graphics graphics, ref Point position)
		{
			Point oldPos = position;
			key.Draw(graphics,ref position);
			Point middlePos = position;
			value.Draw(graphics,ref position);
			graphics.DrawRectangle(pen, new Rectangle(oldPos, new Size(200, middlePos.Y - oldPos.Y)));
			graphics.DrawRectangle(pen, new Rectangle(middlePos, new Size(200, position.Y - middlePos.Y)));
			position.Y += 18;
		}
		public KeyValuePair<Map, Map> GetEntry()
		{
			return new KeyValuePair<Map,Map>(key.GetMap(),value.GetMap());
		}
	}
	public class EmptyMapElement : Element
	{
		public override Map GetMap()
		{
			return Map.Empty;
		}
		public override void Draw(Graphics graphics, ref Point position)
		{
			Draw(graphics, ref position, "*");
		}
	}
	public class MapElement : Element
	{
		private Map map;
		public override Map GetMap()
		{
			Map map = new StrategyMap();
			foreach(Entry element in elements)
			{
				KeyValuePair<Map, Map> entry = element.GetEntry();
				map[entry.Key]=entry.Value;
			}
			return map;
		}
		public MapElement(Map map)
		{
			foreach (KeyValuePair<Map, Map> entry in map)
			{
				elements.Add(new Entry(entry.Key,entry.Value));
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