//	Meta is a programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License version 2
//	as published by the Free Software Foundation.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

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
			//args=new string[]{@"C:\_ProectSupportMaterial\Editor\editor.meta"};
			//args=new string[]{@"C:\_ProectSupportMaterial\Meta\library\function.meta"};
			//args=new string[]{@"C:\Dokumente und Einstellungen\Christian\Desktop\editor.meta"};
			//args=new string[]{@"..\..\basicTest.meta"};
//			try {
				if(args.Length==0) {
					Directory.SetCurrentDirectory(
						".."+Path.DirectorySeparatorChar+".."+Path.DirectorySeparatorChar);
					ExecuteTests test=new ExecuteTests(typeof(Tests),path);
				}
				else {
					if(!File.Exists(args[0])) {
						throw new ApplicationException("File "+args[0]+" not found.");
					}
					Interpreter.ORunFnM(args[0],new Map());
					// TODO: fix this to only show original error message
				}
//			}
//			catch(CharStreamException e) {// put this into "Run" ???, no don't, every caller can do this differently
//				Console.WriteLine(e.Message); //put all this error printing into one method
//				Console.ReadLine();
//			}
//			catch(RecognitionException e) {
//				Console.WriteLine(e.Message+" line:"+e.line+"+ column:"+e.column);
//				Console.ReadLine();
//			}
//			catch(TokenStreamRecognitionException e) {
//				Console.WriteLine(e.recog.Message+" line:"+e.recog.line+" column:"+e.recog.column);
//				Console.ReadLine();
//			}
//			catch(TokenStreamException e) {
//				Console.WriteLine(e.Message);
//				Console.ReadLine();
//			}
//			catch(Exception e) {
//				string text="";
//				do {
//					text+=e.Message+"\n"+e.TargetSite+"\n";
//					e=e.InnerException;
//				} 
//				while(e!=null);
//				Console.WriteLine(text);
//				Console.ReadLine();
//			}
//			DateTime start=DateTime.Now;
//			string file=@"C:\Dokumente und Einstellungen\Christian\Desktop\performance.meta";
//			Interpreter.Run(new StreamReader(file),new Map());
//			DateTime end=DateTime.Now;
//			TimeSpan span=end-start;
//			Console.WriteLine(span.TotalSeconds);
//			Console.ReadLine();
		}
	}
	public class Tests {
		private static string filename=@"basicTest.meta";
		// TODO:make it possible to choose between different tests on command line, and whether to test at all
		[SerializeMethods(new string[]{"getNextSibling","getFirstChild","getText"})]
		public class ParseToAst:TestCase {
			public override object ORun() {
				return Interpreter.ParseToAst(Path.Combine(
					Test.path,filename));
			}
		}
		public class CompileToMap:TestCase {
			public static Map map;
			public override object ORun() {
				map=Interpreter.mCompileS(Path.Combine(
					Test.path,filename));
				return map;
			}
		}
		public class CompileToExpression:TestCase {
			public override object ORun() {
				return Interpreter.mCompileS(Path.Combine(
					Test.path,filename)).ECompile();
			}
		}
		public class Execute:TestCase {
			public override object ORun() {
				Map argument=new Map();
				argument[new Integer(1)]="first arg";
				argument[new Integer(2)]="second=arg";
				return Interpreter.ORunFnM(Path.Combine(Test.path,filename),argument);
			}
		}
	}
}
