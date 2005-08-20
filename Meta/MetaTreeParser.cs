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
		public const int LITERAL_START = 25;
		public const int LITERAL_END = 26;
		public const int LITERAL_VERY_END = 27;
		public const int LITERAL = 28;
		public const int LINE = 29;
		public const int SPACE = 30;
		public const int NEWLINE = 31;
		public const int NEWLINE_KEEP_TEXT = 32;
		public const int SPACES = 33;
		
		public MetaTreeParser()
		{
			tokenNames = tokenNames_;
		}
		
	public IMap  expression(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST expression_AST_in = (MetaAST)_t;
		
		result=null;//new NormalMap();
		
		
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
	
	public IMap  call(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST call_AST_in = (MetaAST)_t;
		
		result=new NormalMap();
		result.Extent=call_AST_in.Extent;
		IMap call=new NormalMap();
		IMap delayed=new NormalMap();
		IMap argument=new NormalMap();
		
		
		AST __t166 = _t;
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
		
		call[CodeKeys.Function]=delayed;
		call[CodeKeys.Argument]=argument;
		result[CodeKeys.Call]=call;
		
		_t = __t166;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return result;
	}
	
	public IMap  map(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST map_AST_in = (MetaAST)_t;
		
		result=new NormalMap();
		result.Extent=map_AST_in.Extent;
		IMap statements=new NormalMap();
		IMap s=null;
		int counter=1;
		
		
		AST __t161 = _t;
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
					
									statements[new NormalMap(new Integer(counter))]=s;					
									counter++;
								
				}
				else
				{
					goto _loop164_breakloop;
				}
				
			}
_loop164_breakloop:			;
		}    // ( ... )*
		_t = __t161;
		_t = _t.getNextSibling();
		
		result[CodeKeys.Program]=statements;
		
		retTree_ = _t;
		return result;
	}
	
	public IMap  select(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST select_AST_in = (MetaAST)_t;
		
		result=new NormalMap();
		result.Extent=select_AST_in.Extent;
		IMap selection=new NormalMap();
		IMap key=null;
		int counter=1;
		
		
		AST __t170 = _t;
		MetaAST tmp21_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SELECT);
		_t = _t.getFirstChild();
		{
			{ // ( ... )+
			int _cnt173=0;
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((tokenSet_0_.member(_t.Type)))
				{
					key=expression(_t);
					_t = retTree_;
					
					selection[new NormalMap(new Integer(counter))]=key;
					counter++;
					
				}
				else
				{
					if (_cnt173 >= 1) { goto _loop173_breakloop; } else { throw new NoViableAltException(_t);; }
				}
				
				_cnt173++;
			}
_loop173_breakloop:			;
			}    // ( ... )+
		}
		_t = __t170;
		_t = _t.getNextSibling();
		
		result[CodeKeys.Select]=selection;
		
		retTree_ = _t;
		return result;
	}
	
	public IMap  search(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST search_AST_in = (MetaAST)_t;
		
				result=new NormalMap();
				IMap lookupResult=null;
				result.Extent=search_AST_in.Extent;
				IMap e=null;
			
		
		AST __t175 = _t;
		MetaAST tmp22_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SEARCH);
		_t = _t.getFirstChild();
		e=expression(_t);
		_t = retTree_;
		_t = __t175;
		_t = _t.getNextSibling();
		
				result[CodeKeys.Search]=e;
			
		retTree_ = _t;
		return result;
	}
	
	public IMap  literal(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST literal_AST_in = (MetaAST)_t;
		MetaAST token = null;
		
		result=new NormalMap();
		result.Extent=literal_AST_in.Extent;
		
		
		token = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,LITERAL);
		_t = _t.getNextSibling();
		
		result[CodeKeys.Literal]=new NormalMap(token.getText());
		
		retTree_ = _t;
		return result;
	}
	
	public IMap  delayed(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST delayed_AST_in = (MetaAST)_t;
		
		result=new NormalMap();
		result.Extent=delayed_AST_in.Extent;
		IMap mExpression;
		//IMap CodeKeys.Run=new NormalMap();
		
		
		AST __t178 = _t;
		MetaAST tmp23_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		mExpression=expression(_t);
		_t = retTree_;
		_t = __t178;
		_t = _t.getNextSibling();
		
						//CodeKeys.Run[CodeKeys.Run]=mExpression;
		result[CodeKeys.Delayed]=mExpression;
		//				CodeKeys.Run[CodeKeys.Run]=mExpression;
		//        result[CodeKeys.Delayed]=CodeKeys.Run;
		
		retTree_ = _t;
		return result;
	}
	
	public IMap  key(AST _t) //throws RecognitionException
{
		IMap result;
		
		MetaAST key_AST_in = (MetaAST)_t;
		
				int counter=1;
				result=new NormalMap();
				IMap e=null;
			
		
		AST __t153 = _t;
		MetaAST tmp24_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,KEY);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt155=0;
		for (;;)
		{
			if (_t == null)
				_t = ASTNULL;
			if ((tokenSet_0_.member(_t.Type)))
			{
				e=expression(_t);
				_t = retTree_;
				
								result[new NormalMap(new Integer(counter))]=e;
								counter++;
							
			}
			else
			{
				if (_cnt155 >= 1) { goto _loop155_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt155++;
		}
_loop155_breakloop:		;
		}    // ( ... )+
		_t = __t153;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return result;
	}
	
	public IMap  statement(AST _t) //throws RecognitionException
{
		IMap statement;
		
		MetaAST statement_AST_in = (MetaAST)_t;
		
				statement=new NormalMap();
				//IMap key=null;
				IMap val=null;
				IMap k=null;
			
		
		AST __t157 = _t;
		MetaAST tmp25_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//IMap statement=new NormalMap();
					statement[CodeKeys.Key]=k;
					statement[CodeKeys.Value]=val;// TODO: Add Extent to statements, too?
				
		_t = __t157;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return statement;
	}
	
	public IMap  statementSearch(AST _t) //throws RecognitionException
{
		IMap statement;
		
		MetaAST statementSearch_AST_in = (MetaAST)_t;
		
				statement=new NormalMap();
				//IMap key=null;
				IMap val=null;
				IMap k=null;
			
		
		AST __t159 = _t;
		MetaAST tmp26_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT_SEARCH);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//IMap statement=new NormalMap();
					statement[CodeKeys.Key]=k;
					statement[CodeKeys.Value]=val;// TODO: Add Extent to statements, too?
				
		_t = __t159;
		_t = _t.getNextSibling();
		
				statement[CodeKeys.Search]=new NormalMap(new Integer(1));
			
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
		@"""LITERAL_START""",
		@"""LITERAL_END""",
		@"""LITERAL_VERY_END""",
		@"""LITERAL""",
		@"""LINE""",
		@"""SPACE""",
		@"""NEWLINE""",
		@"""NEWLINE_KEEP_TEXT""",
		@"""SPACES"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 268450560L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
