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
//			Interpreter.Run("editor.meta",new Map());
			Directory.SetCurrentDirectory(
				".."+Path.DirectorySeparatorChar+".."+Path.DirectorySeparatorChar);
			TestExecuter test=new TestExecuter(typeof(Tests),path);
		}
	}
	public class Tests {
		private static string filename=@"basicTest.meta";
		[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
			public class ParseAst:MetaTest {
			public override object GetTestResult() {
				return Interpreter.Parse(new StreamReader(Path.Combine(
					Test.path,filename)));
			}
		}
		public class ParseObject:MetaTest {
			public override object GetTestResult() {
				return Interpreter.Mapify(new StreamReader(Path.Combine(
					Test.path,filename)));
			}
		}
		public class Compile:MetaTest {
			public override object GetTestResult() {
				return Interpreter.Mapify(new StreamReader(Path.Combine(
					Test.path,filename))).Compile();
			}
		}
		public class Execute:MetaTest {
			public override object GetTestResult() {
				Map argument=new Map();
				argument[new Integer(1)]=Interpreter.String("first arg");
				argument[new Integer(2)]=Interpreter.String("second=arg");
				return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			}
		}
	}
}
