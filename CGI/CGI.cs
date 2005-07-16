using System;
using Meta.Execution;
using Meta.Types;

class CGI
{
	[STAThread]
	static void Main(string[] args)
	{
		Console.Write("Content-Type: text/html\n\n");
		object oResult=Interpreter.Run(@"C:\_ProjectSupportMaterial\Meta\CGI\page.meta",new Map());
		if(oResult is Map && ((Map)oResult).IsString)
		{
			Console.Write(((Map)oResult).String);
		}
		else
		{
			throw new ApplicationException("Error: Meta did not return a string.");
		}
		int asdf=0;

		//Console.ReadLine();
//		Console.Write("<html><head><title>CGI" + 
//			" in C#</title></head><body>" +
//			"CGI Environment:<br />");
//		Console.Write("<table border = \"1\"><tbody><tr><td>The" + 
//			" Common Gateway " +
//			"Interface revision on the server:</td><td>" +
//			System.Environment.GetEnvironmentVariable("GATEWAY_INTERFACE") +
//			"</td></tr>");
//		Console.Write("<tr><td>The serevr's hostname or IP address:</td><td>" +
//			System.Environment.GetEnvironmentVariable("SERVER_NAME") + 
//			"</td></tr>");
//		Console.Write("<tr><td>The name and" + 
//			" version of the server software that" +
//			" is answering the client request:</td><td>" +
//			System.Environment.GetEnvironmentVariable("SERVER_SOFTWARE") +
//			"</td></tr>");
//		Console.Write("<tr><td>The name and revision of the information " +
//			"protocol the request came in with:</td><td>" +
//			System.Environment.GetEnvironmentVariable("SERVER_PROTOCOL") +
//			"</td></tr>");
//		Console.Write("<tr><td>The method with which the information request" +
//			"was issued:</td><td>" +
//			System.Environment.GetEnvironmentVariable("REQUEST_METHOD") +
//			"</td></tr>");
//		Console.Write("<tr><td>Extra path information passed to a CGI" +
//			" program:</td><td>" +
//			System.Environment.GetEnvironmentVariable("PATH_INFO") + 
//			"</td></tr>");
//		Console.Write("<tr><td>The translated version of the path given " +
//			"by the variable PATH_INFO:</td><td>" +
//			System.Environment.GetEnvironmentVariable("PATH_TRANSLATED") +
//			"</td></tr>");
//		Console.Write("<tr><td>The GET information passed to the program. " +
//			"It is appended to the URL with a \"?\":</td><td>" +
//			System.Environment.GetEnvironmentVariable("QUERY_STRING") +
//			"</td></tr>");
//		Console.Write("<tr><td>The remote IP address of the user making +"+
//			"the request:</td><td>" +
//				System.Environment.GetEnvironmentVariable("REMOTE_ADDR") +
//				"</td></tr>");
//		Console.Write("</tbody></table></body></html>");
	}
}