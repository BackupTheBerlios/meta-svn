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
		public const int STATEMENT_SEARCH = 16;
		public const int EQUAL = 17;
		public const int HASH = 18;
		public const int EXCLAMATION_MARK = 19;
		public const int COLON = 20;
		public const int LBRACKET = 21;
		public const int RBRACKET = 22;
		public const int POINT = 23;
		public const int LITERAL_KEY = 24;
		public const int LITERAL = 25;
		public const int LINE = 26;
		public const int SPACE = 27;
		public const int NEWLINE = 28;
		public const int NEWLINE_KEEP_TEXT = 29;
		public const int SPACES = 30;
		
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
		
		
		AST __t147 = _t;
		MetaAST tmp19_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
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
		
		call[Strings.Function]=delayed;
		call[Strings.Argument]=argument;
		result[Strings.Call]=call;
		
		_t = __t147;
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
		
		
		AST __t142 = _t;
		MetaAST tmp20_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,MAP);
		_t = _t.getFirstChild();
		{    // ( ... )*
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((_t.Type==STATEMENT||_t.Type==STATEMENT_SEARCH))
				{
					{
						if (null == _t)
							_t = ASTNULL;
						switch ( _t.Type )
						{
						case STATEMENT:
						{
							s=statement(_t);
							_t = retTree_;
							break;
						}
						case STATEMENT_SEARCH:
						{
							s=statementSearch(_t);
							_t = retTree_;
							break;
						}
						default:
						{
							throw new NoViableAltException(_t);
						}
						 }
					}
					
									statements[new Integer(counter)]=s;					
									counter++;
								
				}
				else
				{
					goto _loop145_breakloop;
				}
				
			}
_loop145_breakloop:			;
		}    // ( ... )*
		_t = __t142;
		_t = _t.getNextSibling();
		
		result[Strings.Program]=statements;
		
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
		
		
		AST __t151 = _t;
		MetaAST tmp21_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SELECT);
		_t = _t.getFirstChild();
		{
			{ // ( ... )+
			int _cnt154=0;
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
					if (_cnt154 >= 1) { goto _loop154_breakloop; } else { throw new NoViableAltException(_t);; }
				}
				
				_cnt154++;
			}
_loop154_breakloop:			;
			}    // ( ... )+
		}
		_t = __t151;
		_t = _t.getNextSibling();
		
		result[Strings.Select]=selection;
		
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
			
		
		AST __t156 = _t;
		MetaAST tmp22_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SEARCH);
		_t = _t.getFirstChild();
		e=expression(_t);
		_t = retTree_;
		_t = __t156;
		_t = _t.getNextSibling();
		
				result[Strings.Search]=e;
			
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
		
		result[Strings.Literal]=new Map(token.getText());
		
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
		//Map Strings.Run=new Map();
		
		
		AST __t159 = _t;
		MetaAST tmp23_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		mExpression=expression(_t);
		_t = retTree_;
		_t = __t159;
		_t = _t.getNextSibling();
		
						//Strings.Run[Strings.Run]=mExpression;
		result[Strings.Delayed]=mExpression;
		//				Strings.Run[Strings.Run]=mExpression;
		//        result[Strings.Delayed]=Strings.Run;
		
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
			
		
		AST __t134 = _t;
		MetaAST tmp24_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,KEY);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt136=0;
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
				if (_cnt136 >= 1) { goto _loop136_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt136++;
		}
_loop136_breakloop:		;
		}    // ( ... )+
		_t = __t134;
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
			
		
		AST __t138 = _t;
		MetaAST tmp25_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//Map statement=new Map();
					statement[Strings.Key]=k;
					statement[Strings.Value]=val;// TODO: Add Extent to statements, too?
				
		_t = __t138;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return statement;
	}
	
	public Map  statementSearch(AST _t) //throws RecognitionException
{
		Map statement;
		
		MetaAST statementSearch_AST_in = (MetaAST)_t;
		
				statement=new Map();
				//Map key=null;
				Map val=null;
				Map k=null;
			
		
		AST __t140 = _t;
		MetaAST tmp26_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT_SEARCH);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//Map statement=new Map();
					statement[Strings.Key]=k;
					statement[Strings.Value]=val;// TODO: Add Extent to statements, too?
				
		_t = __t140;
		_t = _t.getNextSibling();
		
				statement[Strings.Search]=new Integer(1);
			
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
		@"""STATEMENT_SEARCH""",
		@"""EQUAL""",
		@"""HASH""",
		@"""EXCLAMATION_MARK""",
		@"""COLON""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""POINT""",
		@"""LITERAL_KEY""",
		@"""LITERAL""",
		@"""LINE""",
		@"""SPACE""",
		@"""NEWLINE""",
		@"""NEWLINE_KEEP_TEXT""",
		@"""SPACES"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 33569536L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
