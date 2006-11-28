using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Meta;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Xml;
using System.Runtime.InteropServices;
using NetMatters;
using CustomRichTextBoxSample;


public partial class Editor : System.Windows.Window {
	public class Box:RichTextBox {
		public Box() {
			this.Width = 500;
			//try {
			//    Style style = new Style(typeof(Paragraph));
			//    //style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0.0)));
			//    style.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0.0)));
			//    this.Style = style;
			//}
			//catch (Exception e) {
			//}

			//<Setter Property="TextElement.FontFamily" Value="Courier New"/>
			//<Setter Property="TextElement.FontSize" Value="12"/>
		}
		public string Text {
			get {
				TextRange textRange = new TextRange(Document.ContentStart, Document.ContentEnd);
				return textRange.Text;
			}
			set {
				Document.Blocks.Clear();
				//Paragraph paragraph = new Paragraph();
				Document.ContentStart.InsertTextInRun(value);
				Document.Blocks.FirstBlock.Margin = new Thickness(0.0);
				//paragraph.ContentStart.InsertTextInRun(value);
				//Document.Blocks.Add(paragraph);
				//System.Windows.Media.
				//Document
			}
		}
	}
	Box textBox = new Box();
	//TextBox textBox = new TextBox();
	private void Save() {
		if (fileName == null) {
			SaveFileDialog dialog = new SaveFileDialog();
			if (dialog.ShowDialog() == true) {
				fileName = dialog.FileName;
			}
			else {
				return;
			}
		}
		File.WriteAllText(fileName, textBox.Text);
	}
	private string fileName = null;
	private void BindKey(RoutedUICommand command, Key key, ModifierKeys modifiers) {
		command.InputGestures.Add(new KeyGesture(key, modifiers));
	}
	public void Open(string file) {
		fileName = file;
		textBox.Text = File.ReadAllText(fileName);
	}
	public static TextBox toolTip = new TextBox();
	public static ListBox intellisense = new ListBox();
	Label status = new Label();

	public static bool Intellisense {
		get {
			return intellisense.Visibility == Visibility.Visible;
		}
	}
	public void IntellisenseUp() {
		if (intellisense.SelectedIndex > 0) {
			intellisense.SelectedIndex--;
		}
	}
	public void IntellisenseDown() {
		if (intellisense.SelectedIndex < intellisense.Items.Count - 1) {
			intellisense.SelectedIndex++;
		}
	}
	public void Complete() {
		//if (intellisense.SelectedItem != null) {
		//    int index = textBox.SelectionStart;
		//    textBox.SelectionStart = searchStart+1;//textBox.Text.LastIndexOf('.', textBox.SelectionStart) + 1;
		//    //textBox.SelectionStart = textBox.Text.LastIndexOf('.', textBox.SelectionStart) + 1;
		//    textBox.SelectionLength = index - textBox.SelectionStart;
		//    textBox.SelectedText = ((Item)intellisense.SelectedItem).ToString();
		//    intellisense.Visibility = Visibility.Hidden;
		//    toolTip.Visibility = Visibility.Hidden;
		//    textBox.SelectionStart += textBox.SelectionLength;
		//    textBox.SelectionLength = 0;
		//}
	}
	int searchStart = 0;
	public void PositionIntellisense() {
		//Rect r = textBox.GetRectFromCharacterIndex(textBox.SelectionStart);
		//Canvas.SetLeft(intellisense, r.Right);
		//Canvas.SetTop(intellisense, r.Bottom);
		//Canvas.SetLeft(toolTip, r.Right);
		//Canvas.SetTop(toolTip, r.Bottom + 110);
	}
	ScrollViewer scrollViewer = new ScrollViewer();
	public void SetBreakpoint() {

	}
	public void FindMatchingBrace() {
		const string openBraces = "({[<";
		const string closeBraces = ")}]>";
		bool direction = true;
		if (!MatchingBrace(openBraces, closeBraces, true)) {
			MatchingBrace(closeBraces, openBraces, false);
		}
	}
	public class IterativeSearch {
		public void Back() {
			text = text.Substring(0, text.Length - 1);
			Find(!direction);
		}
		public void OnKeyDown(TextCompositionEventArgs e) {
			text+=e.Text;
			Find(direction);
			e.Handled = true;
		}
		public void Find(bool forward) {
			//if (text.Length > 0) {
			//    if (forward) {
			//        index = textBox.Text.ToLower().IndexOf(text, index);
			//    }
			//    else {
			//        index = textBox.Text.ToLower().LastIndexOf(text, index);
			//    }
			//    if (index != -1) {
			//        textBox.Select(index, text.Length);
			//    }
			//    else {
			//        editor.StopIterativeSearch();
			//    }
			//}
		}
		private int index;
		private string text="";
		private int start;
		private Box textBox;
		private bool direction;
		public IterativeSearch(Box textBox,bool direction) {
			this.direction = direction;
			this.textBox = textBox;
			textBox.Cursor = Cursors.ScrollS;
			//start = textBox.SelectionStart;
			index = start;
		}
	}
	public IterativeSearch iterativeSearch;
	public void StartIterativeSearch() {
		if (iterativeSearch == null) {
			iterativeSearch = new IterativeSearch(textBox,true);
		}
		else {
			StopIterativeSearch();
		}
	}
	public void StopIterativeSearch() {
		textBox.Cursor = Cursors.IBeam;
		iterativeSearch = null;
	}

	//public static string GetDoc(MemberInfo memberInfo, bool isSignature, bool isSummary, bool isParameters) {
	//    XmlNode comment = GetComments(memberInfo);
	//    string text = "";
	//    string summary = "";
	//    ArrayList parameters = new ArrayList();
	//    if (comment != null && comment.ChildNodes != null) {
	//        foreach (XmlNode node in comment.ChildNodes) {
	//            switch (node.Name) {
	//                case "summary":
	//                    summary = node.InnerXml;
	//                    break;
	//                case "param":
	//                    parameters.Add(node);
	//                    break;
	//                default:
	//                    break;
	//            }
	//        }
	//    }
	//    if (isSignature) {
	//        MemberInfo[] overloaded;
	//        if (memberInfo.DeclaringType != null) {
	//            overloaded = memberInfo.DeclaringType.GetMember(memberInfo.Name);
	//        }
	//        else {
	//            overloaded = new MemberInfo[] { memberInfo };
	//        }
	//        string overloadedText = "";
	//        if (isParameters) {
	//            //if (overloadNumber > 1) {
	//                //overloadedText = (overloadIndex + 1).ToString() + " of " + overloadNumber.ToString() + "   ";
	//                //text += overloadedText;
	//            //}
	//        }
	//        else if (overloaded.Length > 1) {
	//            overloadedText = " ( +" + overloaded.Length.ToString() + " overloads)";
	//        }

	//        if (memberInfo is MethodBase) {
	//            if (memberInfo is MethodInfo) {
	//                text += ((MethodInfo)memberInfo).ReturnType + " ";
	//            }
	//            text += ((MethodBase)memberInfo).Name;
	//            text += " (";
	//            bool firstParameter = true;
	//            foreach (ParameterInfo parameter in ((MethodBase)memberInfo).GetParameters()) {
	//                if (!firstParameter) {
	//                    text += " ";
	//                }
	//                string parameterName = parameter.ParameterType.ToString();
	//                text += parameterName;
	//                text += " " + parameter.Name + ",";
	//                firstParameter = false;
	//            }
	//            if (((MethodBase)memberInfo).GetParameters().Length > 0) {
	//                text = text.Remove(text.Length - 1, 1);
	//            }
	//            text += ")";
	//            //						if(memberInfos.Length>1) {
	//            //							text+=" ( +"+(memberInfos.Length-1)+" overloads)";
	//            //						}
	//        }
	//        else if (memberInfo is PropertyInfo) {
	//            text += ((PropertyInfo)memberInfo).PropertyType + " " + ((PropertyInfo)memberInfo).Name;
	//        }
	//        else if (memberInfo is FieldInfo) {
	//            text += ((FieldInfo)memberInfo).FieldType + " " + ((FieldInfo)memberInfo).Name;
	//        }
	//        else if (memberInfo is Type) {
	//            if (((Type)memberInfo).IsInterface) {
	//                text += "interface ";
	//            }
	//            else {
	//                if (((Type)memberInfo).IsAbstract) {
	//                    text += "abstract ";
	//                }
	//                if (((Type)memberInfo).IsValueType) {
	//                    text += "struct ";
	//                }
	//                else {
	//                    text += "class ";
	//                }
	//            }
	//            text += ((Type)memberInfo).Name;
	//        }
	//        else if (memberInfo is EventInfo) {
	//            text += ((EventInfo)memberInfo).EventHandlerType.FullName + " " +
	//                memberInfo.Name;
	//        }
	//        text = text.Replace("System.String", "string").Replace("System.Object", "object")
	//            .Replace("System.Boolean", "bool")
	//            .Replace("System.Byte", "byte").Replace("System.Char", "char")
	//            .Replace("System.Decimal", "decimal").Replace("System.Double", "double")
	//            .Replace("System.Enum", "enum").Replace("System.Single", "float")
	//            .Replace("System.Int32", "int").Replace("System.Int64", "long")
	//            .Replace("System.SByte", "sbyte").Replace("System.Int16", "short")
	//            .Replace("System.UInt32", "uint").Replace("System.UInt16", "ushort")
	//            .Replace("System.UInt64", "ulong").Replace("System.Void", "void");
	//        //if (!isParameters) {
	//        //    text += overloadedText;
	//        //}
	//        text += "\n";
	//    }
	//    text += summary + "\n";
	//    if (isParameters) {
	//        //text+="\nparameters: \n";
	//        foreach (XmlNode node in parameters) {
	//            text += node.Attributes["name"].Value + ": " + node.InnerXml + "\n";
	//            //						text+=node.Attributes["name"].Value+": "+node.InnerXml;
	//        }
	//    }
	//    return text.Replace("<para>", "").Replace("\r\n", "").Replace("</para>", "").Replace("<see cref=\"", "")
	//        .Replace("\" />", "").Replace("T:", "").Replace("F:", "").Replace("P:", "")
	//        .Replace("M:", "").Replace("E:", "").Replace("     ", " ").Replace("    ", " ")
	//        .Replace("   ", " ").Replace("  ", " ").Replace("\n ", "\n");
	//}
	//public static string CreateParamsDescription(ParameterInfo[] parameters) {
	//    string text = "";
	//    if (parameters.Length > 0) {
	//        text += "(";
	//        foreach (ParameterInfo parameter in parameters) {
	//            text += parameter.ParameterType.FullName + ",";
	//        }
	//        text = text.Remove(text.Length - 1, 1);
	//        text += ")";
	//    }
	//    return text;
	//}
	//private static Hashtable comments = new Hashtable();
	//public static XmlDocument LoadAssemblyComments(Assembly assembly) {
	//    if (!comments.ContainsKey(assembly)) {
	//        string dllPath = assembly.Location;
	//        string dllName = System.IO.Path.GetFileNameWithoutExtension(dllPath);
	//        string dllDirectory = System.IO.Path.GetDirectoryName(dllPath);

	//        string assemblyDirFile = System.IO.Path.Combine(dllDirectory, dllName + ".xml");
	//        string runtimeDirFile = System.IO.Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), dllName + ".xml");
	//        string other = System.IO.Path.Combine(@"C:\Programme\Reference Assemblies\Microsoft\Framework\v3.0", dllName+".xml");

	//        string fileName;
	//        if (File.Exists(assemblyDirFile)) {
	//            fileName = assemblyDirFile;
	//        }
	//        if (File.Exists(other)) {
	//            fileName = other;
	//        }
	//        else if (File.Exists(runtimeDirFile)) {
	//            fileName = runtimeDirFile;
	//        }
	//        else {
	//            return null;
	//        }

	//        XmlDocument xml = new XmlDocument();
	//        xml.Load(fileName);
	//        comments[assembly] = xml;
	//    }
	//    return (XmlDocument)comments[assembly];
	//}
	//public static XmlNode GetComments(MemberInfo mi) {
	//    Type declType = (mi is Type) ? ((Type)mi) : mi.DeclaringType;
	//    XmlDocument doc = LoadAssemblyComments(declType.Assembly);
	//    if (doc == null)
	//        return null;
	//    string xpath;

	//    // Handle nested classes
	//    string typeName = declType.FullName.Replace("+", ".");

	//    // Based on the member type, get the correct xpath query
	//    switch (mi.MemberType) {
	//        case MemberTypes.NestedType:
	//        case MemberTypes.TypeInfo:
	//            xpath = "//member[@name='T:" + typeName + "']";
	//            break;

	//        case MemberTypes.Constructor:
	//            xpath = "//member[@name='M:" + typeName + "." +
	//                "#ctor" + CreateParamsDescription(
	//                ((ConstructorInfo)mi).GetParameters()) + "']";
	//            break;

	//        case MemberTypes.Method:
	//            xpath = "//member[@name='M:" + typeName + "." +
	//                mi.Name + CreateParamsDescription(
	//                ((MethodInfo)mi).GetParameters());
	//            if (mi.Name == "op_Implicit" || mi.Name == "op_Explicit") {
	//                xpath += "~{" +
	//                    ((MethodInfo)mi).ReturnType.FullName + "}";
	//            }
	//            xpath += "']";
	//            break;

	//        case MemberTypes.Property:
	//            xpath = "//member[@name='P:" + typeName + "." +
	//                mi.Name + CreateParamsDescription(
	//                ((PropertyInfo)mi).GetIndexParameters()) + "']";
	//            break;

	//        case MemberTypes.Field:
	//            xpath = "//member[@name='F:" + typeName + "." + mi.Name + "']";
	//            break;

	//        case MemberTypes.Event:
	//            xpath = "//member[@name='E:" + typeName + "." + mi.Name + "']";
	//            break;

	//        // Unknown member type, nothing to do
	//        default:
	//            return null;
	//    }

	//    // Get the node from the document
	//    return doc.SelectSingleNode(xpath);
	//}
	public class Item{
		public string Signature() {
			if (original != null) {
				XmlComments comments = new XmlComments(original);
				XmlNode node=comments.Summary;
				foreach(XmlNode n in node.ChildNodes) {
					if (n.Name == "see") {
						n.InnerText = n.Attributes["cref"].Value.Substring(2);
					}
				}
				return node.InnerText;
			}
			return "";
		}
		private string text;
		private MemberInfo original;
		public Item(string text, MemberInfo original) {
		//public Item(string text, MemberInfo member,MemberInfo original) {
			//this.member = member;
			this.text = text;
			this.original = original;
		}
		public override string ToString() {
			return text;
		}
	}
	//public class X:Spell
	public static Editor editor;
	public static List<Item> intellisenseItems = new List<Item>();
	public Editor() {
		this.WindowState = WindowState.Maximized;
		editor = this;
		RoutedUICommand deleteLine = new RoutedUICommand();
		BindKey(deleteLine, Key.L, ModifierKeys.Control);
		this.CommandBindings.Add(new CommandBinding(deleteLine, delegate {
			//int start=Math.Max(0,textBox.Text.LastIndexOf('\n', textBox.SelectionStart));
			//int end=Math.Max(0,textBox.Text.IndexOf('\n',Math.Max(start+1,textBox.SelectionStart)));
			//textBox.SelectionStart = start;
			//textBox.SelectionLength = end - start;
			//textBox.SelectedText = "";
		}));

		BindKey(ApplicationCommands.Find, Key.F, ModifierKeys.Control);
		BindKey(EditingCommands.Backspace, Key.N, ModifierKeys.Alt);
		BindKey(EditingCommands.Delete, Key.M, ModifierKeys.Alt);
		BindKey(EditingCommands.DeleteNextWord, Key.M, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.DeletePreviousWord, Key.N, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveDownByLine, Key.K, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveDownByPage, Key.K, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveLeftByCharacter, Key.J, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveRightByCharacter, Key.Oem3, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveUpByLine, Key.L, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveLeftByWord, Key.J, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveRightByWord, Key.M, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveToDocumentEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveToDocumentStart, Key.U, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.MoveToLineEnd, Key.Oem1, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveToLineStart, Key.U, ModifierKeys.Alt);
		BindKey(EditingCommands.MoveUpByPage, Key.L, ModifierKeys.Alt | ModifierKeys.Control);
		BindKey(EditingCommands.SelectDownByLine, Key.K, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectDownByPage, Key.K, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectLeftByCharacter, Key.J, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectLeftByWord, Key.J, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectRightByCharacter, Key.Oem3, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectRightByWord, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectToDocumentEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectToDocumentStart, Key.U, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectToLineEnd, Key.Oem1, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectToLineStart, Key.U, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectUpByLine, Key.L, ModifierKeys.Alt | ModifierKeys.Shift);
		BindKey(EditingCommands.SelectUpByPage, Key.L, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);
		InitializeComponent();
		textBox.FontSize = 16;
		textBox.SpellCheck.IsEnabled = true;
		intellisense.MaxHeight = 100;
		toolTip.Text = "Tooltip!!!!";
		toolTip.Width = 300;
		Label l;
		toolTip.TextWrapping = TextWrapping.Wrap;
		intellisense.SelectionChanged += delegate {
			if (intellisense.SelectedItem != null) {
				toolTip.Text = ((Item)intellisense.SelectedItem).Signature();
			}
		};
		intellisense.Width = 300;
		intellisense.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, delegate { Save(); }));
		textBox.FontFamily = new FontFamily("Courier New");
		textBox.AcceptsTab = true;
		intellisense.MouseDoubleClick += delegate {
			Complete();
		};
		textBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e) {
			if (Intellisense) {
				e.Handled = true;
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
					if (e.SystemKey == Key.L) {
						IntellisenseUp();
					}
					else if (e.SystemKey == Key.K) {
						IntellisenseDown();
					}
					else {
						e.Handled = false;
					}
				}
				else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
					if (e.Key == Key.L) {
						EditingCommands.MoveToLineStart.Execute(null, textBox);
						EditingCommands.SelectToLineEnd.Execute(null, textBox);
						EditingCommands.Delete.Execute(null, textBox);
					}
					else {
						e.Handled = false;
					}
				}
				else if (e.Key == Key.Return) {
					Complete();
				}
				else if (e.Key == Key.Tab) {
					Complete();
				}
				else if (e.Key == Key.Up) {
					IntellisenseUp();
				}
				else if (e.Key == Key.Down) {
					IntellisenseDown();
				}
				else {
					e.Handled = false;
				}
			}
			else {
				if (iterativeSearch != null) {
					if (e.Key == Key.Back) {
						editor.iterativeSearch.Back();
						e.Handled = true;
					}
					if (e.Key == Key.Escape) {
						editor.StopIterativeSearch();
						e.Handled = true;

					}
				}
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
					if (e.Key == Key.I) {
						StartIterativeSearch();
						e.Handled = true;
					}
					else if (e.Key == Key.Space) {
						e.Handled = true;
						StartIntellisense();
						intellisense.Items.Clear();
						//List<Expression> list=new List<Meta.Expression>();
						List<Source> sources=new List<Source>(Meta.Expression.sources.Keys);
						sources.RemoveAll(delegate (Source source){
							return source.FileName!=fileName;
						});
						sources.Sort(delegate(Source a,Source b) {
							return a.CompareTo(b);
						});
						Map s=null;
						sources.Reverse();
						foreach(Source source in sources) {
							foreach(Meta.Expression expression in Meta.Expression.sources[source]) {
								Program program=expression as Program;
								if(program!=null) {
									s=program.statementList[program.statementList.Count - 1].CurrentMap();
									break;
								}
							}
							if(s!=null) {
								break;
							}
						}
			            List<string> keys = new List<string>();
			            if (s != null) {
			                foreach (Map m in s.Keys) {
			                    keys.Add(m.ToString());
			                }
			            }
			            keys.Sort(delegate(string a, string b) {
			                return a.CompareTo(b);
			            });
			            if (keys.Count != 0) {
							intellisense.Visibility = Visibility.Visible;
			                toolTip.Visibility = Visibility.Visible;
			            }
			            intellisenseItems.Clear();
			            intellisense.Items.Clear();
			            foreach (string k in keys) {
			                MethodBase m = null;
			                MemberInfo original = null;
			                if (s.ContainsKey(k)) {
			                    Map value = s[k];
			                    Method method = value as Method;
			                    if (method != null) {
			                        m = method.method;
			                        original = method.original;
			                    }
			                    TypeMap typeMap = value as TypeMap;
			                    if (typeMap != null) {
			                        original = typeMap.Type;
			                    }
			                }
			                intellisenseItems.Add(new Item(k, original));
			            }
			            if (intellisense.Items.Count != 0) {
			                intellisense.SelectedIndex = 0;
			            }
						PositionIntellisense();
						Meta.Expression.sources.Clear();
					}
				}
				else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt) {
					if (e.SystemKey == Key.I) {
						FindMatchingBrace();
						e.Handled = true;
					}
					else if (e.SystemKey == Key.H) {
						SetBreakpoint();
						e.Handled = true;
					}
				}
				else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift)) {
					if (e.SystemKey == Key.I) {
						//int start = textBox.SelectionStart;
						//FindMatchingBrace();
						//textBox.Select(Math.Min(start, textBox.SelectionStart), Math.Abs(start - textBox.SelectionStart));
						//e.Handled = true;
					}
				}
			}
		};
		textBox.PreviewTextInput += new TextCompositionEventHandler(textBox_PreviewTextInput);
		textBox.KeyDown += delegate(object obj, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				//string line = textBox.GetLineText(textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart));
				//textBox.SelectedText = "\n".PadRight(1 + line.Length - line.TrimStart('\t').Length, '\t');
				//textBox.SelectionStart = textBox.SelectionStart + textBox.SelectionLength;
				//textBox.SelectionLength = 0;
				//textBox.Focus();
			}
			else if (e.Key == Key.Escape) {
				if (Intellisense) {
					intellisense.Visibility = Visibility.Hidden;
					toolTip.Visibility = Visibility.Hidden;
				}
			}
			else if (e.Key == Key.OemPeriod) {
				Source key=StartIntellisense();
				intellisense.Items.Clear();
				if (Meta.Expression.sources.ContainsKey(key)) {
					List<Meta.Expression> list = Meta.Expression.sources[key];
					for (int i = 0; i < list.Count; i++) {
						if (list[i] is Search || list[i] is Select || list[i] is Call) {
							intellisense.Items.Clear();
							Map s = list[i].EvaluateStructure();
							List<string> keys = new List<string>();
							if (s != null) {
								foreach (Map m in s.Keys) {
									keys.Add(m.ToString());
								}
							}
							keys.Sort(delegate(string a, string b) {
								return a.CompareTo(b);
							});
							if (keys.Count != 0) {
								intellisense.Visibility = Visibility.Visible;
								toolTip.Visibility = Visibility.Visible;
							}
							intellisenseItems.Clear();
							intellisense.Items.Clear();
							foreach (string k in keys) {
								MethodBase m = null;
								MemberInfo original = null;
								if (s.ContainsKey(k)) {
									Map value = s[k];
									Method method = value as Method;
									if (method != null) {
										m = method.method;
										original = method.original;
									}
									TypeMap typeMap = value as TypeMap;
									if (typeMap != null) {
										original = typeMap.Type;
									}
								}
								intellisenseItems.Add(new Item(k, original));
							}
							if (intellisense.Items.Count != 0) {
								intellisense.SelectedIndex = 0;
							}
							PositionIntellisense();
						}
					}
				}
				else {
					MessageBox.Show("no intellisense" + Meta.Expression.sources.Count);
				}
				Meta.Expression.sources.Clear();
			}
		};
		DockPanel dockPanel = new DockPanel();

		Menu menu = new Menu();
		DockPanel.SetDock(menu, Dock.Top);
		MenuItem file = new MenuItem();
		MenuItem save = new MenuItem();
		MenuItem run = new MenuItem();
		MenuItem open = new MenuItem();
		RoutedUICommand execute = new RoutedUICommand("Run","Run",GetType());
		file.Header = "File";
		open.Header = "Open";
		open.Command=ApplicationCommands.Open;
		save.Header = "Save";
		save.Command = ApplicationCommands.Save;
		run.Header = "Run";
		run.Command = execute;
		CommandBindings.Add(new CommandBinding(execute, delegate {
			//TextRange range = new TextRange(word.Start, word.End);
			//range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
			//range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

			Save();
			Process.Start(System.IO.Path.Combine(@"D:\Meta\", @"bin\Debug\Meta.exe"), fileName);
		}));
		BindKey(execute, Key.F5, ModifierKeys.None);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, delegate { Open(); }));
		DockPanel.SetDock(status, Dock.Bottom);
		dockPanel.Children.Add(status);

		file.Items.Add(open);
		file.Items.Add(save);
		file.Items.Add(run);
		menu.Items.Add(file);
		dockPanel.Children.Add(menu);

		DockPanel.SetDock(textBox, Dock.Bottom);
		textBox.TextChanged += delegate {
			if (Intellisense) {
				//if (textBox.SelectionStart <= searchStart) {
				//    intellisense.Visibility = Visibility.Hidden;
				//    toolTip.Visibility = Visibility.Hidden;
				//}
				//else {
				//    int index = searchStart;//textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
				//    //int index = textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
				//    //int index = textBox.Text.Substring(0, textBox.SelectionStart).LastIndexOf('.');
				//    if (index != -1) {
				//        string text = textBox.Text.Substring(index + 1, textBox.SelectionStart - index - 1);
				//        //if (text.Length != 0) {
				//            intellisense.Items.Clear();
				//            foreach(Item item in intellisenseItems) {
				//                if (item.ToString().ToLower().Contains(text.ToLower())) {//, StringComparison.OrdinalIgnoreCase)) {
				//                    intellisense.Items.Add(item);
				//                }
				//            }
				//            intellisense.SelectedIndex = 0;
				//        //}
				//    }
				//}
			}
		};
		textBox.SelectionChanged += delegate {
			//status.Content = "Ln " + (textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1);
		};
		Canvas canvas = new Canvas();
		canvas.Children.Add(textBox);
		canvas.Background = Brushes.Yellow;
		DockPanel.SetDock(canvas, Dock.Top);
		scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
		scrollViewer.Content = canvas;
		dockPanel.Children.Add(scrollViewer);
		const int width = 0;
		const int height = 0;

		Canvas.SetLeft(textBox, width/2);
		Canvas.SetTop(textBox, height/2);
		textBox.SizeChanged += delegate {
			canvas.Width = textBox.ActualWidth + width;
			canvas.Height = textBox.ActualHeight + height;
		};
		intellisense.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e) {
			if (intellisense.SelectedItem != null) {
				intellisense.ScrollIntoView(intellisense.SelectedItem);
			}
		};
		intellisense.Visibility = Visibility.Hidden;
		toolTip.Visibility = Visibility.Hidden;
		Canvas.SetZIndex(intellisense, 100);
		canvas.Children.Add(intellisense);
		canvas.Children.Add(toolTip);
		this.Content = dockPanel;
		this.Loaded += delegate {
			Open(@"D:\meta\mail.meta");
			textBox.Focus();
			//textBox.SelectionStart = 0;
		};
	}

	private Source StartIntellisense() {
		//string text = textBox.Text.Substring(0, textBox.SelectionStart);
		//searchStart = textBox.SelectionStart;
		//Interpreter.profiling = false;
		//foreach (Dictionary<Parser.State, Parser.CachedResult> cached in Parser.allCached) {
		//    cached.Clear();
		//}

		////Parser.cached.Clear();
		//Parser parser = new Parser(text, fileName);
		//Map map = null;
		//bool matched = Parser.Value.Match(parser, ref map);
		//LiteralExpression gac = new LiteralExpression(Gac.gac, null);
		//LiteralExpression lib = new LiteralExpression(Gac.gac["library"], gac);
		//lib.Statement = new LiteralStatement(gac);
		//KeyStatement.intellisense = true;
		//map[CodeKeys.Function].GetExpression(lib).Statement = new LiteralStatement(lib);
		//map[CodeKeys.Function].Compile(lib);
		//Source key = new Source(
		//    parser.state.Line,
		//    parser.state.Column,
		//    parser.state.FileName
		//);
		//return key;


		return new Source(1, 1, "");

		//intellisense.Items.Clear();
		//if (Meta.Expression.sources.ContainsKey(key)) {
		//    List<Meta.Expression> list = Meta.Expression.sources[key];
		//    for (int i = 0; i < list.Count; i++) {
		//        if (list[i] is Search || list[i] is Select || list[i] is Call) {
		//            PositionIntellisense();
		//            intellisense.Items.Clear();
		//            Map s = list[i].EvaluateStructure();
		//            List<string> keys = new List<string>();
		//            if (s != null) {
		//                foreach (Map m in s.Keys) {
		//                    keys.Add(m.ToString());
		//                }
		//            }
		//            keys.Sort(delegate(string a, string b) {
		//                return a.CompareTo(b);
		//            });
		//            if (keys.Count != 0) {
		//                intellisense.Visibility = Visibility.Visible;
		//                toolTip.Visibility = Visibility.Visible;
		//            }
		//            intellisenseItems.Clear();
		//            intellisense.Items.Clear();
		//            foreach (string k in keys) {
		//                MethodBase m = null;
		//                MemberInfo original = null;
		//                if (s.ContainsKey(k)) {
		//                    Map value = s[k];
		//                    Method method = value as Method;
		//                    if (method != null) {
		//                        m = method.method;
		//                        original = method.original;
		//                    }
		//                    TypeMap typeMap = value as TypeMap;
		//                    if (typeMap != null) {
		//                        original = typeMap.Type;
		//                    }
		//                }
		//                intellisenseItems.Add(new Item(k, original));
		//            }
		//            if (intellisense.Items.Count != 0) {
		//                intellisense.SelectedIndex = 0;
		//            }
		//        }
		//    }
		//}
		//else {
		//    MessageBox.Show("no intellisense" + Meta.Expression.sources.Count);
		//}
		//Meta.Expression.sources.Clear();
	}
	public void Open() {
		OpenFileDialog dialog = new OpenFileDialog();
		if (dialog.ShowDialog() == true) {
			Open(dialog.FileName);
		}
	}
	void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
		if (iterativeSearch != null) {
			iterativeSearch.OnKeyDown(e);
		}
	}
	private bool MatchingBrace(string openBraces, string closeBraces, bool direction) {
		return false;
		//char previous = textBox.Text[textBox.SelectionStart - 1];
		//char next = textBox.Text[textBox.SelectionStart];
		//int forward = openBraces.IndexOf(previous);
		//int backward = openBraces.IndexOf(next);
		//if (forward != -1 || backward != -1) {
		//    char brace;
		//    int index;
		//    if (forward != -1) {
		//        index = forward;
		//        brace = openBraces[forward];
		//    }
		//    else if (backward != -1) {
		//        index = backward;
		//        brace = openBraces[backward];
		//    }
		//    else {
		//        return false;
		//    }
		//    char closingBrace = closeBraces[index];
		//    int pos;
		//    if (direction) {
		//        pos = textBox.SelectionStart + 2;
		//    }
		//    else {
		//        pos = textBox.SelectionStart - 2;
		//    }
		//    int braces = 0;
		//    while (true) {
		//        if (direction) {
		//            pos++;
		//        }
		//        else {
		//            pos--;
		//        }
		//        if (pos <= 0 || pos >= textBox.Text.Length) {
		//            break;
		//        }
		//        char c = textBox.Text[pos];
		//        if (c == closingBrace) {
		//            braces--;
		//        }
		//        else if (c == brace) {
		//            braces++;
		//        }
		//        if (braces < 0) {
		//            textBox.SelectionStart = pos;
		//            return true;
		//        }
		//    }
		//}
		//return false;
	}
}