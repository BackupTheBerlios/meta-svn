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
using System.Threading;
using System.Windows.Forms;
using System.IO;

public partial class _Default : System.Web.UI.Page 
{
	//[System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name="FullTrust")]
	 //  protected override void Render(HtmlTextWriter writer)
	 //  {
	 //    // Converts the Number property to a string and
	 //// writes it to the containing page.
	 //    //writer.Write("The Number is " + Number.ToString() + " (" );

	 //// Uses the GetPostBackEventReference method to pass
	 //// 'inc' to the RaisePostBackEvent method when the link
	 //// this code creates is clicked.
	 //    writer.Write("<a href=\"javascript:" + ClientScript.GetPostBackEventReference(PostBackOptions.,"inc") + "\">Increase Number</a>"); 

	 //    writer.Write(" or ");

	 //// Uses the GetPostBackEventReference method to pass
	 //// 'dec' to the RaisePostBackEvent method when the link
	 //// this code creates is clicked.
	 //    writer.Write("<a href=\"javascript:" + Page.GetPostBackEventReference(this,"dec") + "\">Decrease Number</a>");
	 //  }
	//}
	//protected override void Render(HtmlTextWriter output)
	//{
	//    output.Write("<a  id=\"" + this.UniqueID + "\" href=\"javascript:" + Page.GetPostBackEventReference(this) + "\">");
	//    output.Write(" " + this.UniqueID + "</a>");
	//}
    protected void Page_Load(object sender, EventArgs e)
    {
		if (!IsPostBack)
		{
			//execute.Attributes.Add("onclick", "clicked()");
			input.Focus();
			string x=ClientScript.GetPostBackEventReference(new PostBackOptions(this));
			int asdf = 0;
			//testButton.Attributes.Add("onclick",ClientScript.GetPostBackEventReference(new PostBackOptions(
		}
		if (Session["map"] == null)
		{
			Session["map"] = new StrategyMap();
		}
		if (IsPostBack)
		{
			execute_Click(null, null);
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
		Thread thread = new Thread(new ThreadStart(Worker));
		try
		{
			Meta.Process.InstallationPath = Path.GetDirectoryName(Page.Request.PhysicalPath);
			//input.Text = input.Text.Trim();
			thread.Start();
			int waited = 0;
			DateTime start = DateTime.Now;
			while (thread.ThreadState == ThreadState.Running)
			{

				Thread.Sleep(100);
				waited += 100;
				if ((DateTime.Now - start) > new TimeSpan(0, 0, 0, 2))
				{
					output.Text = "Computation could not finish.";
					thread.Abort();
				}
			}
		}
		catch (Exception exception)
		{
			output.Text = "There was an error:" + exception.ToString();
			thread.Abort();
		}
		finally
		{
			try
			{
				thread.Abort();
			}
			catch
			{
			}
		}

	}
	private void Worker()
	{
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
			context.Scope = FileSystem.fileSystem;
			Map statement = parser.Statement(ref count);

			statement.GetStatement().Assign(ref context);//, Map.Empty);
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
		catch (Exception exception)
		{
			text = exception.ToString().Replace(FileSystem.Parser.unixNewLine.ToString(), "<br>");
		}
		output.Text = text;
	}
}
