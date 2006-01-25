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
    protected void Page_Load(object sender, EventArgs e)
    {
		input.Focus();
		if (!IsPostBack)
		{
			input.Text = "|";
		}
    }
	protected void execute_Click(object sender, EventArgs e)
	{
		Thread thread = new Thread(new ThreadStart(Worker));
		try
		{
			Meta.Process.InstallationPath = Path.GetDirectoryName(Page.Request.PhysicalPath);
			thread.Start();
			int waited = 0;
			DateTime start = DateTime.Now;
			while (thread.ThreadState == ThreadState.Running)
			{

				Thread.Sleep(100);
				waited += 100;
				if ((DateTime.Now - start) > new TimeSpan(0, 0, 0, 1000))
				{
					output.Text = "Could not finish program execution.";
				}
			}
		}
		catch (Exception exception)
		{
			output.Text = "There was an error:" + exception.ToString();
		}
		finally
		{
			thread.Abort();
		}

	}
	private void Worker()
	{
		string text;
		try
		{
			Library.writtenText = "";
			Parser parser = new Parser(input.Text, "");
			bool matched;
			Map program = Parser.Program.Match(parser, out matched);
			Map function=program.GetExpression().Evaluate(FileSystem.fileSystem);//, Map.Empty);
			Map result=function.Call(Map.Empty);
			text = Library.writtenText;// "yippieh";
		}
		catch (Exception exception)
		{
			text = exception.ToString().Replace(Syntax.unixNewLine.ToString(), "<br>").Replace(Syntax.windowsNewLine, "<br>");
		}
		output.Text = text;
	}
}
