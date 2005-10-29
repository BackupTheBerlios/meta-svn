using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using Meta;

public class array:MetaLibrary
{
	public static Map apply(Map arg)
	{
		// TODO: ensure "function" is callable, maybe?
		Argument.ContainsKey(arg,"function");
		Argument.ContainsKey(arg,"array");
		Map application=new NormalMap();
		int counter=1;
		foreach(Map element in arg["array"].Array)
		{
			application[counter]=arg["function"].Call(element);
			counter++;
		}
		return application;
	}
}