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
		public const int DELAYED_EXPRESSION_ONLY = 15;
		public const int EQUAL = 16;
		public const int HASH = 17;
		public const int COLON = 18;
		public const int LBRACKET = 19;
		public const int RBRACKET = 20;
		public const int LPAREN = 21;
		public const int RPAREN = 22;
		public const int POINT = 23;
		public const int STAR = 24;
		public const int LITERAL_KEY = 25;
		public const int LITERAL = 26;
		public const int LITERAL_END = 27;
		public const int SPACES = 28;
		public const int LINE = 29;
		public const int SPACE = 30;
		public const int NEWLINE = 31;
		public const int NEWLINE_KEEP_TEXT = 32;
		
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
			case DELAYED_EXPRESSION_ONLY:
			{
				result=delayedExpressionOnly(_t);
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
		
		
		AST __t162 = _t;
		MetaAST tmp28_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
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
		
		_t = __t162;
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
		
		
		AST __t158 = _t;
		MetaAST tmp29_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
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
					goto _loop160_breakloop;
				}
				
			}
_loop160_breakloop:			;
		}    // ( ... )*
		_t = __t158;
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
		
		
		AST __t166 = _t;
		MetaAST tmp30_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SELECT);
		_t = _t.getFirstChild();
		{
			{ // ( ... )+
			int _cnt169=0;
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
					if (_cnt169 >= 1) { goto _loop169_breakloop; } else { throw new NoViableAltException(_t);; }
				}
				
				_cnt169++;
			}
_loop169_breakloop:			;
			}    // ( ... )+
		}
		_t = __t166;
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
			
		
		AST __t171 = _t;
		MetaAST tmp31_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,SEARCH);
		_t = _t.getFirstChild();
		e=expression(_t);
		_t = retTree_;
		_t = __t171;
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
		
		
		AST __t174 = _t;
		MetaAST tmp32_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		mExpression=expression(_t);
		_t = retTree_;
		_t = __t174;
		_t = _t.getNextSibling();
		
						mRun[Expression.runString]=mExpression;
		result[Delayed.delayedString]=mRun;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  delayedExpressionOnly(AST _t) //throws RecognitionException
{
		Map result;
		
		MetaAST delayedExpressionOnly_AST_in = (MetaAST)_t;
		
		result=new Map();
		Map mExpression=null;
		
		
		AST __t176 = _t;
		MetaAST tmp33_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,DELAYED_EXPRESSION_ONLY);
		_t = _t.getFirstChild();
		mExpression=expression(_t);
		_t = retTree_;
		_t = __t176;
		_t = _t.getNextSibling();
		
					result[Delayed.delayedString]=mExpression;
					//result.Extent=#delayedExpressionOnly.Extent;
		
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
			
		
		AST __t152 = _t;
		MetaAST tmp34_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,KEY);
		_t = _t.getFirstChild();
		{ // ( ... )+
		int _cnt154=0;
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
				if (_cnt154 >= 1) { goto _loop154_breakloop; } else { throw new NoViableAltException(_t);; }
			}
			
			_cnt154++;
		}
_loop154_breakloop:		;
		}    // ( ... )+
		_t = __t152;
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
			
		
		AST __t156 = _t;
		MetaAST tmp35_AST_in = (_t==ASTNULL) ? null : (MetaAST)_t;
		match((AST)_t,STATEMENT);
		_t = _t.getFirstChild();
		k=key(_t);
		_t = retTree_;
		val=expression(_t);
		_t = retTree_;
		
					//Map statement=new Map();
					statement[Statement.keyString]=k;
					statement[Statement.valueString]=val;// TODO: Add Extent to statements, too?
				
		_t = __t156;
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
		@"""DELAYED_EXPRESSION_ONLY""",
		@"""':'""",
		@"""HASH""",
		@"""'='""",
		@"""'['""",
		@"""']'""",
		@"""'('""",
		@"""')'""",
		@"""'.'""",
		@"""'*'""",
		@"""a key""",
		@"""a literal""",
		@"""LITERAL_END""",
		@"""whitespace""",
		@"""a line""",
		@"""SPACE""",
		@"""a newline""",
		@"""a newline"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 67156736L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
