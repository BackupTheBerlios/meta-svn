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
	// refactor
    protected void Page_Load(object sender, EventArgs e)
    {
		input.Focus();
		if (!IsPostBack)
		{
			string x=ClientScript.GetPostBackEventReference(new PostBackOptions(this));
		}
    }
	//private static int Leaves(Map map)
	//{
	//    int count = 0;
	//    foreach (KeyValuePair<Map, Map> pair in map)
	//    {
	//        if (pair.Value.IsNumber)
	//        {
	//            count++;
	//        }
	//        else
	//        {
	//            count += Leaves(pair.Value);
	//        }
	//    }
	//    return count;
	//}
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
					output.Text = "Could not finish program execution.";
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
			// refactor
			string code = input.Text;
			code = code.Trim();
			Parser parser = new Parser(code, "");
			bool matched;
			Map program = Parser.Program.Match(parser, out matched);

			Map result=program.GetExpression().Evaluate(FileSystem.fileSystem);//, Map.Empty);
			text = "yippieh";
		}
		catch (Exception exception)
		{
			text = exception.ToString().Replace(Syntax.unixNewLine.ToString(), "<br>").Replace(Syntax.windowsNewLine, "<br>");
		}
		output.Text = text;
	}
}
