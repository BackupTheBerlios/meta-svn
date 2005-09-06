// $ANTLR 2.7.3: "Parser.g" -> "MetaTreeParser.cs"$

namespace Meta.Parser
{
	// Generate header specific to the tree-parser CSharp file
	using System;
	
	using TreeParser = antlr.TreeParser;
	using Token                    = antlr.Token;
	using AST                      = antlr.collections.AST;
	using RecognitionException     = antlr.RecognitionException;
	using ANTLRException           = antlr.ANTLRException;
	using NoViableAltException     = antlr.NoViableAltException;
	using MismatchedTokenException = antlr.MismatchedTokenException;
	using SemanticException        = antlr.SemanticException;
	using BitSet                   = antlr.collections.impl.BitSet;
	using ASTPair                  = antlr.ASTPair;
	using ASTFactory               = antlr.ASTFactory;
	using ASTArray                 = antlr.collections.impl.ASTArray;
	
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



	
	public 	class MetaTreeParser : antlr.TreeParser
	{
		public const int EOF = 1;
		public const int NULL_TREE_LOOKAHEAD = 3;
		public const int INDENTATION = 4;
		public const int SPACES = 5;
		public const int INDENT = 6;
		public const int ENDLINE = 7;
		public const int DEDENT = 8;
		public const int PROGRAM = 9;
		public const int FUNCTION = 10;
		public const int STATEMENT = 11;
		public const int CALL = 12;
		public const int SELECT = 13;
		public const int SEARCH = 14;
		public const int KEY = 15;
		public const int SAME_INDENT = 16;
		public const int EQUAL = 17;
		public const int APOSTROPHE = 18;
		public const int COLON = 19;
		public const int STAR = 20;
		public const int LBRACKET = 21;
		public const int RBRACKET = 22;
		public const int POINT = 23;
		public const int LITERAL_KEY = 24;
		public const int LITERAL_START = 25;
		public const int LITERAL_END = 26;
		public const int LITERAL_VERY_END = 27;
		public const int LITERAL = 28;
		public const int LINE = 29;
		public const int NEWLINE = 30;
		public const int NEWLINE_KEEP_TEXT = 31;
		
		public MetaTreeParser()
		{
			tokenNames = tokenNames_;
		}
		
	public Map  expression(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST expression_AST_in = (MetaAST)_t;
		
		{
			if (null == _t)
				_t = ASTNULL;
			switch ( _t.Type )
			{
			case CALL:
			{
				code=call(_t);
				_t = retTree_;
				break;
			}
			case PROGRAM:
			{
				code=program(_t);
				_t = retTree_;
				break;
			}
			case SELECT:
			{
				code=select(_t);
				_t = retTree_;
				break;
			}
			case SEARCH:
			{
				code=search(_t);
				_t = retTree_;
				break;
			}
			case LITERAL:
			{
				code=literal(_t);
				_t = retTree_;
				break;
			}
			case FUNCTION:
			{
				code=delayed(_t);
				_t = retTree_;
				break;
			}
			default:
			{
				throw new NoViableAltException(_t);
			}
			 }
		}
		retTree_ = _t;
		return code;
	}
	
	public Map  call(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST call_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				code.Extent=call_AST_in.Extent;
				Map callCode=new NormalMap();
				Map functionCode;
				Map argumentCode;
			
		
		AST __t156 = _t;
		MetaAST tmp16_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,CALL);
		_t = _t.getFirstChild();
		{
			functionCode=expression(_t);
			_t = retTree_;
		}
		{
			argumentCode=expression(_t);
			_t = retTree_;
		}
		
					callCode[CodeKeys.Function]=functionCode;
					callCode[CodeKeys.Argument]=argumentCode;
					code[CodeKeys.Call]=callCode;
				
		_t = __t156;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return code;
	}
	
	public Map  program(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST program_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				code.Extent=program_AST_in.Extent;
				Map programCode=new NormalMap();
				Map statementCode;
				int statementNumber=1;
			
		
		AST __t151 = _t;
		MetaAST tmp17_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,PROGRAM);
		_t = _t.getFirstChild();
		{    // ( ... )*
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((_t.Type==STATEMENT))
				{
					{
						statementCode=statement(_t);
						_t = retTree_;
					}
					
									programCode[statementNumber]=statementCode;
									statementNumber++;
								
				}
				else
				{
					goto _loop154_breakloop;
				}
				
			}
_loop154_breakloop:			;
		}    // ( ... )*
		_t = __t151;
		_t = _t.getNextSibling();
		
				code[CodeKeys.Program]=programCode;
			
		retTree_ = _t;
		return code;
	}
	
	public Map  select(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST select_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				code.Extent=select_AST_in.Extent;
				Map selectCode=new NormalMap();
				Map keyCode;
				int counter=1;
			
		
		AST __t160 = _t;
		MetaAST tmp18_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SELECT);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt162=0;
		for (;;)
		{
			if (_t == null)
				_t = ASTNULL;
			if ((tokenSet_0_.member(_t.Type)))
			{
				keyCode=expression(_t);
				_t = retTree_;
				
								selectCode[counter]=keyCode;
								counter++;
							
			}
			else
			{
				if (_cnt162 >= 1) { goto _loop162_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt162++;
		}
_loop162_breakloop:		;
		}    // ( ... )+
		_t = __t160;
		_t = _t.getNextSibling();
		
				code[CodeKeys.Select]=selectCode;
			
		retTree_ = _t;
		return code;
	}
	
	public Map  search(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST search_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				code.Extent=search_AST_in.Extent;
				Map searchCode;
			
		
		AST __t164 = _t;
		MetaAST tmp19_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SEARCH);
		_t = _t.getFirstChild();
		searchCode=expression(_t);
		_t = retTree_;
		_t = __t164;
		_t = _t.getNextSibling();
		
				code[CodeKeys.Search]=searchCode;
			
		retTree_ = _t;
		return code;
	}
	
	public Map  literal(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST literal_AST_in = (MetaAST)_t;
		MetaAST token = null;
		
				code=new NormalMap();
				code.Extent=literal_AST_in.Extent;
			
		
		token = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,LITERAL);
		_t = _t.getNextSibling();
		
				code[CodeKeys.Literal]=new NormalMap(token.getText());
			
		retTree_ = _t;
		return code;
	}
	
	public Map  delayed(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST delayed_AST_in = (MetaAST)_t;
		
		code=new NormalMap();
		code.Extent=delayed_AST_in.Extent;
		Map delayedCode;
		
		
		AST __t166 = _t;
		MetaAST tmp20_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		delayedCode=expression(_t);
		_t = retTree_;
		_t = __t166;
		_t = _t.getNextSibling();
		
		code[CodeKeys.Delayed]=delayedCode;
		
		retTree_ = _t;
		return code;
	}
	
	public Map  statementKeys(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST statementKeys_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				Map keyCode;
				int keyNumber=1;
			
		
		AST __t145 = _t;
		MetaAST tmp21_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,KEY);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt147=0;
		for (;;)
		{
			if (_t == null)
				_t = ASTNULL;
			if ((tokenSet_0_.member(_t.Type)))
			{
				keyCode=expression(_t);
				_t = retTree_;
				
								code[keyNumber]=keyCode;
								keyNumber++;
							
			}
			else
			{
				if (_cnt147 >= 1) { goto _loop147_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt147++;
		}
_loop147_breakloop:		;
		}    // ( ... )+
		_t = __t145;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return code;
	}
	
	public Map  statement(AST _t) //throws RecognitionException
{
		Map code;
		
		MetaAST statement_AST_in = (MetaAST)_t;
		
				code=new NormalMap();
				Map keyCode;
				Map valueCode;
			
		
		AST __t149 = _t;
		MetaAST tmp22_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT);
		_t = _t.getFirstChild();
		keyCode=statementKeys(_t);
		_t = retTree_;
		valueCode=expression(_t);
		_t = retTree_;
		
					code[CodeKeys.Key]=keyCode;
					code[CodeKeys.Value]=valueCode;
				
		_t = __t149;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return code;
	}
	
	public new MetaAST getAST()
	{
		return (MetaAST) returnAST;
	}
	
	
	public static readonly string[] tokenNames_ = new string[] {
		@"""<0>""",
		@"""EOF""",
		@"""<2>""",
		@"""NULL_TREE_LOOKAHEAD""",
		@"""INDENTATION""",
		@"""SPACES""",
		@"""INDENT""",
		@"""ENDLINE""",
		@"""DEDENT""",
		@"""PROGRAM""",
		@"""FUNCTION""",
		@"""STATEMENT""",
		@"""CALL""",
		@"""SELECT""",
		@"""SEARCH""",
		@"""KEY""",
		@"""SAME_INDENT""",
		@"""EQUAL""",
		@"""APOSTROPHE""",
		@"""COLON""",
		@"""STAR""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""POINT""",
		@"""LITERAL_KEY""",
		@"""LITERAL_START""",
		@"""LITERAL_END""",
		@"""LITERAL_VERY_END""",
		@"""LITERAL""",
		@"""LINE""",
		@"""NEWLINE""",
		@"""NEWLINE_KEEP_TEXT"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 268465664L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
