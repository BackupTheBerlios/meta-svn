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
		input.Focus();
		if (!IsPostBack)
		{
			//execute.Attributes.Add("onclick", "clicked()");
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
			if (pair.Value.IsNumber)
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
				if ((DateTime.Now - start) > new TimeSpan(0, 0, 0, 10))
				{
					output.Text = "Computation could not finish.";
					//thread.Abort();
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
			Parser parser = new Parser(code, "");
			parser.indentationCount = 0;
			int originalCount = context.ArrayCount;
			parser.defaultKeys.Push(originalCount + 1);
			parser.isStartOfFile = false;
			context.Scope = FileSystem.fileSystem;
			bool matched;
			Map statement = Parser.Statement.Match(parser,out matched);

			statement.GetStatement().Assign(ref context);//, Map.Empty);
			if (context.ArrayCount != originalCount)
			{
				Map value = context[context.ArrayCount];
				if (Leaves(value) < 1000)
				{
					text = Serialize.Value(value);
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
			text = exception.ToString().Replace(Syntax.unixNewLine.ToString(), "<br>").Replace(Syntax.windowsNewLine, "<br>");
		}
		output.Text = text;
	}
	//private void Worker()
	//{
	//    string text;
	//    try
	//    {
	//        string code = input.Text;
	//        code = code.Trim();
	//        Map context = (Map)Session["map"];
	//        FileSystem.Parser parser = new FileSystem.Parser(code, "");
	//        parser.indentationCount = 0;
	//        int originalCount = context.ArrayCount;
	//        parser.defaultKeys.Push(originalCount+1);
	//        parser.isStartOfFile = false;
	//        context.Scope = FileSystem.fileSystem;
	//        Map statement = Meta.FileSystem.Parser.Statement.Match(parser);

	//        statement.GetStatement().Assign(ref context);//, Map.Empty);
	//        if (context.ArrayCount != originalCount)
	//        {
	//            Map value = context[context.ArrayCount];
	//            if (Leaves(value) < 1000)
	//            {
	//                text = FileSystem.Serialize.Value(value);
	//            }
	//            else
	//            {
	//                text = "Map is too big to display.";
	//            }
	//        }
	//        else
	//        {
	//            text = "";
	//        }
	//    }
	//    catch (Exception exception)
	//    {
	//        text = exception.ToString().Replace(FileSystem.Syntax.unixNewLine.ToString(), "<br>").Replace(FileSystem.Syntax.windowsNewLine,"<br>");
	//    }
	//    output.Text = text;
	//}
}
