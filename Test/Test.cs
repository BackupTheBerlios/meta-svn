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

namespace Test {
	class Test {
		public static string path="";
		public static void Main(string[] args) {
			args=new string[]{"editor.meta"};
			if(args.Length==0) {
				Directory.SetCurrentDirectory(
					".."+Path.DirectorySeparatorChar+".."+Path.DirectorySeparatorChar);
				ExecuteTests test=new ExecuteTests(typeof(Tests),path);
			}
			else {
				Interpreter.Run(new StreamReader(args[0]),new Map());
			}
		}
	}
	public class Tests {
		private static string filename=@"basicTest.meta";
		[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
		public class ParseAst {
			public static object RunTestCase() {
				return Interpreter.ParseToAst(new StreamReader(Path.Combine(
					Test.path,filename)));
			}
		}
		public class ParseObject {
			public static object RunTestCase() {
				return Interpreter.CompileToMap(new StreamReader(Path.Combine(
					Test.path,filename)));
			}
		}
		public class Compile {
			public static object RunTestCase() {
				return Interpreter.CompileToMap(new StreamReader(Path.Combine(
					Test.path,filename))).Compile();
			}
		}
		public class Execute {
			public static object RunTestCase() {
				Map argument=new Map();
				argument[new Integer(1)]="first arg";
				argument[new Integer(2)]="second=arg";
				return Interpreter.Run(new StreamReader(Path.Combine(Test.path,filename)),argument);
			}
		}
	}
}
