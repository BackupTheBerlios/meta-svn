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
using System.Threading;

namespace Test {
	class Test {
		public static string path="";
		[STAThread]
		public static void Main(string[] args) {
//			args=new string[]{@"..\..\basicTest.meta"};
			if(args.Length==0) {
				Directory.SetCurrentDirectory(
					".."+Path.DirectorySeparatorChar+".."+Path.DirectorySeparatorChar);
				ExecuteTests test=new ExecuteTests(typeof(Tests),path);
			}
			else {
				if(!File.Exists(args[0])) {
					throw new ApplicationException("File "+args[0]+" not found.");
				}
//				try {
					Interpreter.Run(new StreamReader(args[0]),new Map());
					// fix this to only show original error message
//				}
//				catch(CharStreamException e) {// put this into "Run"
//					Console.WriteLine(e.Message);
//				}
//				catch(RecognitionException e) {
//					Console.WriteLine(e.Message);
//				}
//				catch(TokenStreamException e) {
//					Console.WriteLine(e.Message);
//				}
//				catch(Exception e) {
//					string text="";
//					do {
//						text+=e.Message+"\n"+e.TargetSite+"\n";
//						e=e.InnerException;
//					} 
//					while(e!=null);
//					Console.WriteLine(text);
//					Console.ReadLine();
//				}
			}
		}
	}
	public class Tests {
		private static string filename=@"basicTest.meta";
		// make it possible to choose between different tests on command line
		[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
		public class ParseToAst:TestCase {
			public override object RunTestCase() {
				return Interpreter.ParseToAst(new StreamReader(Path.Combine(
					Test.path,filename)));
			}
		}
		public class CompileToMap:TestCase {
			public static Map map;
			public override object RunTestCase() {
				map=Interpreter.CompileToMap(new StreamReader(Path.Combine(
					Test.path,filename)));
				return map;
			}
		}
		public class CompileToExpression:TestCase {
			public override object RunTestCase() {
				return Interpreter.CompileToMap(new StreamReader(Path.Combine(
					Test.path,filename))).Compile();
			}
		}
		public class Execute:TestCase {
			public override object RunTestCase() {
				Map argument=new Map();
				argument[new Integer(1)]="first arg";
				argument[new Integer(2)]="second=arg";
				return Interpreter.Run(Path.Combine(Test.path,filename),argument);
			}
		}
	}
}
