//	An implementation of the Meta programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This library is free software; you can redistribute it and/or
//	modify it under the terms of the GNU Lesser General Public
//	License as published by the Free Software Foundation; either
//	version 2.1 of the License, or (at your option) any later version.
//
//	This library is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//	Lesser General Public License for more details.
//
//	You should have received a copy of the GNU Lesser General Public
//	License along with this library; if not, write to the Free Software
//	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Meta.Types;
using Meta.Parser;
using Meta.Execution;
using System;
using Meta.TestingFramework;
using antlr;


class Test
{
	public static string path="";//Path.Combine("..","..");
	public static void Main(string[] args)
	{
		Directory.SetCurrentDirectory(
			".."+Path.DirectorySeparatorChar+".."+Path.DirectorySeparatorChar);
		TestExecuter test=new TestExecuter(typeof(Tests),path);
	}
}
public class Tests
{
	private static string filename=@"basicTest.meta";//Path.Combine("..",Path.Combine("..",));
	[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
	public class ParseAst:MetaTest
	{
		public override object GetTestResult()
		{
			return Interpreter.Parse(new StreamReader(Path.Combine(
				Test.path,filename)));
		}
	}
	public class ParseObject:MetaTest
	{
		public override object GetTestResult()
		{
			return Interpreter.Mapify(new StreamReader(Path.Combine(
				Test.path,filename)));
		}
	}
	public class Compile:MetaTest
	{
		public override object GetTestResult()
		{
			return Interpreter.Mapify(new StreamReader(Path.Combine(
				Test.path,filename))).Compile();
		}
	}
	public class Execute:MetaTest
	{
		public override object GetTestResult()
		{
			Map argument=new Map();
			argument[new Integer(1)]=Interpreter.String("first arg");
			argument[new Integer(2)]=Interpreter.String("second=arg");
			return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			//return null;
		}
	}
}
public class MemberTest
{
	public static string classField="default";
	public string instanceField="default";

	public static string ClassProperty
	{
		get
		{
			return classField;
		}
		set
		{
			classField=value;
		}
	}
	public string InstanceProperty
	{
		get
		{
			return this.instanceField;
		}
		set
		{
			this.instanceField=value;
		}
	}
}
public delegate int IntEvent (int intArg);
public delegate object NormalEvent (object sender);
public class TestClass
{
//	static TestClass() {
//		staticEvent+=new NormalEvent(TestClass_staticEvent);
//	}
	public TestClass()
	{
	}
	public event IntEvent instanceEvent;
	public static event NormalEvent staticEvent;
	protected string x="unchangedX";
	protected string y="unchangedY";
	protected string z="unchangedZ";

	public static object TestClass_staticEvent(object sender) {
		return null;
	}
}
public class PositionalNoConversion:TestClass
{
	public PositionalNoConversion(string p1,string b,string p2)
	{
		this.x=p1;
		this.y=b;
		this.z=p2;
	}
	public string Concatenate(string p1,string b,string c)
	{
		return p1+b+c+this.x+this.y+this.z;
	}
}
public class NamedNoConversion:TestClass
{
	[MetaMethod("(1,y,2)")]
	public NamedNoConversion(Map arg)
	{
		Map def=new Map();
		def[1]="null";
		def["y"]="null";
		def["p2"]="null";
		arg=(Map)Interpreter.MergeTwo(def,arg);
		this.x=(string)arg[1];
		this.y=(string)arg["y"];
		this.z=(string)arg["p2"];
	}
	[MetaMethod("(1,b,c)")]
	public string Concatenate(Map arg)
	{
		Map def=new Map();
		def[1]="null";
		def["b"]="null";
		def["c"]="null";
		arg=(Map)Interpreter.MergeTwo(def,arg);
		return (string)arg[1]+(string)arg["b"]+(string)arg["c"]+this.x+this.y+this.z;
	}
}

public class IndexerNoConversion:TestClass
{
	public string this[string a]
	{
		get
		{
			return this.x+this.y+this.z+a;
		}
		set
		{
			this.x=a+value;
		}
	}
}
