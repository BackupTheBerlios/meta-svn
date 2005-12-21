using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Meta;
using System.Collections.Generic;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
		if (!IsPostBack)
		{
			execute.Attributes.Add("onclick", "clicked()");
			input.Focus();
			Session["map"] = new StrategyMap();
		}
    }
	private static int Leaves(Map map)
	{
		int count = 0;
		foreach (KeyValuePair<Map, Map> pair in map)
		{
			if (pair.Value.IsInteger)
			{
				count++;
			}
			else
			{
				count += Leaves(pair.Value);
			}
		}
		return count;

	}
	protected void execute_Click(object sender, EventArgs e)
	{
		//code = code.Trim(' ', '\t', '\n', '\r');
		string text;
		try
		{
			string code = input.Text;
			code = code.Trim();
			Map context = (Map)Session["map"];
			//Map context = new StrategyMap();
			FileSystem.Parser parser = new FileSystem.Parser(code, "");
			parser.indentationCount = 0;
			int count = FileSystem.fileSystem.ArrayCount;
			int originalCount = count;
			parser.isStartOfFile = false;
			context.Parent = FileSystem.fileSystem;
			Map statement = parser.Statement(ref count);

			statement.GetStatement().Assign(ref context, Map.Empty);
			//statement.GetStatement().Assign(ref FileSystem.fileSystem, Map.Empty);
			if (count != originalCount)
			{
				Map value = context[originalCount];
				if (Leaves(value) < 1000)
				{
					text = FileSystem.Serialize.Value(value);
					//Console.WriteLine(FileSystem.Serialize.Value(value));
				}
				else
				{
					text = "Map is too big to display.";
				}
			}
			else
			{
				text = "";
			}
		}
		catch(Exception exception)
		{
			text = exception.ToString().Replace(FileSystem.Parser.unixNewLine.ToString(),"<br>");
		}
		output.Text = text;
		input.Text = input.Text.Trim();
	}
}
