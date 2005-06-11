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
  using Meta.Types;
  using Meta.Execution;

	
	public 	class MetaTreeParser : antlr.TreeParser
	{
		public const int EOF = 1;
		public const int NULL_TREE_LOOKAHEAD = 3;
		public const int INDENTATION = 4;
		public const int INDENT = 5;
		public const int ENDLINE = 6;
		public const int DEDENT = 7;
		public const int MAP = 8;
		public const int FUNCTION = 9;
		public const int STATEMENT = 10;
		public const int CALL = 11;
		public const int SELECT = 12;
		public const int SEARCH = 13;
		public const int KEY = 14;
		public const int SAME_INDENT = 15;
		public const int EQUAL = 16;
		public const int HASH = 17;
		public const int COLON = 18;
		public const int LBRACKET = 19;
		public const int RBRACKET = 20;
		public const int POINT = 21;
		public const int LITERAL_KEY = 22;
		public const int LITERAL = 23;
		public const int SPACES = 24;
		public const int LINE = 25;
		public const int SPACE = 26;
		public const int NEWLINE = 27;
		public const int NEWLINE_KEEP_TEXT = 28;
		
		public MetaTreeParser()
		{
			tokenNames = tokenNames_;
		}
		
	public Map  expression(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST expression_AST_in = (MetaAST)_t;
		
		result=null;//new Map();
		
		
		{
			if (null == _t)
				_t = ASTNULL;
			switch ( _t.Type )
			{
			case CALL:
			{
				result=call(_t);
				_t = retTree_;
				break;
			}
			case MAP:
			{
				result=map(_t);
				_t = retTree_;
				break;
			}
			case SELECT:
			{
				result=select(_t);
				_t = retTree_;
				break;
			}
			case SEARCH:
			{
				result=search(_t);
				_t = retTree_;
				break;
			}
			case LITERAL:
			{
				result=literal(_t);
				_t = retTree_;
				break;
			}
			case FUNCTION:
			{
				result=delayed(_t);
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
		return result;
	}
	
	public Map  call(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST call_AST_in = (MetaAST)_t;
		
		result=new Map();
		result.Extent=call_AST_in.Extent;
		Map call=new Map();
		Map delayed=new Map();
		Map argument=new Map();
		
		
		AST __t124 = _t;
		MetaAST tmp17_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,CALL);
		_t = _t.getFirstChild();
		{
			delayed=expression(_t);
			_t = retTree_;
		}
		{
			argument=expression(_t);
			_t = retTree_;
		}
		
		call[Call.functionString]=delayed;
		call[Call.argumentString]=argument;
		result[Call.callString]=call;
		
		_t = __t124;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return result;
	}
	
	public Map  map(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST map_AST_in = (MetaAST)_t;
		
		result=new Map();
		result.Extent=map_AST_in.Extent;
		Map statements=new Map();
		Map s=null;
		int counter=1;
		
		
		AST __t120 = _t;
		MetaAST tmp18_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,MAP);
		_t = _t.getFirstChild();
		{    // ( ... )*
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((_t.Type==STATEMENT))
				{
					s=statement(_t);
					_t = retTree_;
					
									statements[new Integer(counter)]=s;					
									counter++;
								
				}
				else
				{
					goto _loop122_breakloop;
				}
				
			}
_loop122_breakloop:			;
		}    // ( ... )*
		_t = __t120;
		_t = _t.getNextSibling();
		
		result[Program.programString]=statements;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  select(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST select_AST_in = (MetaAST)_t;
		
		result=new Map();
		result.Extent=select_AST_in.Extent;
		Map selection=new Map();
		Map key=null;
		int counter=1;
		
		
		AST __t128 = _t;
		MetaAST tmp19_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SELECT);
		_t = _t.getFirstChild();
		{
			{ // ( ... )+
			int _cnt131=0;
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((tokenSet_0_.member(_t.Type)))
				{
					key=expression(_t);
					_t = retTree_;
					
					selection[new Integer(counter)]=key;
					counter++;
					
				}
				else
				{
					if (_cnt131 >= 1) { goto _loop131_breakloop; } else { throw new NoViableAltException(_t);; }
				}
				
				_cnt131++;
			}
_loop131_breakloop:			;
			}    // ( ... )+
		}
		_t = __t128;
		_t = _t.getNextSibling();
		
		result[Select.selectString]=selection;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  search(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST search_AST_in = (MetaAST)_t;
		
				result=new Map();
				Map lookupResult=null;
				result.Extent=search_AST_in.Extent;
				Map e=null;
			
		
		AST __t133 = _t;
		MetaAST tmp20_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SEARCH);
		_t = _t.getFirstChild();
		e=expression(_t);
		_t = retTree_;
		_t = __t133;
		_t = _t.getNextSibling();
		
				result[Search.searchString]=e;
			
		retTree_ = _t;
		return result;
	}
	
	public Map  literal(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST literal_AST_in = (MetaAST)_t;
		MetaAST token = null;
		
		result=new Map();
		result.Extent=literal_AST_in.Extent;
		
		
		token = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,LITERAL);
		_t = _t.getNextSibling();
		
		result[Literal.literalString]=new Map(token.getText());
		
		retTree_ = _t;
		return result;
	}
	
	public Map  delayed(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST delayed_AST_in = (MetaAST)_t;
		
		result=new Map();
		result.Extent=delayed_AST_in.Extent;
		Map mExpression;
		Map mRun=new Map();
		
		
		AST __t136 = _t;
		MetaAST tmp21_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		mExpression=expression(_t);
		_t = retTree_;
		_t = __t136;
		_t = _t.getNextSibling();
		
						//mRun[Expression.sRun]=mExpression;
		result[Delayed.delayedString]=mExpression;
		//				mRun[Expression.sRun]=mExpression;
		//        result[Delayed.delayedString]=mRun;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  key(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST key_AST_in = (MetaAST)_t;
		
				int counter=1;
				result=new Map();
				Map e=null;
			
		
		AST __t114 = _t;
		MetaAST tmp22_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,KEY);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt116=0;
		for (;;)
		{
			if (_t == null)
				_t = ASTNULL;
			if ((tokenSet_0_.member(_t.Type)))
			{
				e=expression(_t);
				_t = retTree_;
				
								result[new Integer(counter)]=e;
								counter++;
							
			}
			else
			{
				if (_cnt116 >= 1) { goto _loop116_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt116++;
		}
_loop116_breakloop:		;
		}    // ( ... )+
		_t = __t114;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return result;
	}
	
	public Map  statement(AST _t) //throws RecognitionException
{
		Map statement;
		
		MetaAST statement_AST_in = (MetaAST)_t;
		
				statement=new Map();
				//Map key=null;
				Map val=null;
				Map k=null;
			
		
		AST __t118 = _t;
		MetaAST tmp23_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//Map statement=new Map();
					statement[Statement.keyString]=k;
					statement[Statement.valueString]=val;// TODO: Add Extent to statements, too?
				
		_t = __t118;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return statement;
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
		@"""INDENT""",
		@"""ENDLINE""",
		@"""DEDENT""",
		@"""MAP""",
		@"""FUNCTION""",
		@"""STATEMENT""",
		@"""CALL""",
		@"""SELECT""",
		@"""SEARCH""",
		@"""KEY""",
		@"""SAME_INDENT""",
		@"""EQUAL""",
		@"""HASH""",
		@"""COLON""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""POINT""",
		@"""LITERAL_KEY""",
		@"""LITERAL""",
		@"""SPACES""",
		@"""LINE""",
		@"""SPACE""",
		@"""NEWLINE""",
		@"""NEWLINE_KEEP_TEXT"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 8403712L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
