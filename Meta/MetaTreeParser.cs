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
		public const int COLON = 12;
		public const int EQUAL = 13;
		public const int LBRACKET = 14;
		public const int RBRACKET = 15;
		public const int LPAREN = 16;
		public const int RPAREN = 17;
		public const int POINT = 18;
		public const int HASH = 19;
		public const int LITERAL_KEY = 20;
		public const int LITERAL = 21;
		public const int SPACES = 22;
		public const int LINE = 23;
		public const int COMMENT = 24;
		public const int SPACE = 25;
		public const int NEWLINE = 26;
		public const int SELECT_KEY = 27;
		
		public MetaTreeParser()
		{
			tokenNames = tokenNames_;
		}
		
	public Map  expression(AST _t) //throws RecognitionException
{
		Map result;
		
		LineNumberAST expression_AST_in = (LineNumberAST)_t;
		
		result=null;
		
		
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
			case SELECT_KEY:
			{
				result=select(_t);
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
		
		LineNumberAST call_AST_in = (LineNumberAST)_t;
		
		result=new Map();
		Map call=new Map();
		Map delayed=new Map();
		Map argument=new Map();
		
		
		AST __t98 = _t;
		LineNumberAST tmp21_AST_in = (_t==ASTNULL) ? null : (LineNumberAST)_t;
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
		
		_t = __t98;
		_t = _t.getNextSibling();
		retTree_ = _t;
		return result;
	}
	
	public Map  map(AST _t) //throws RecognitionException
{
		Map result;
		
		LineNumberAST map_AST_in = (LineNumberAST)_t;
		
		result=new Map();
		Map statements=new Map();
		int counter=1;
		
		
		AST __t93 = _t;
		LineNumberAST tmp22_AST_in = (_t==ASTNULL) ? null : (LineNumberAST)_t;
		match((AST)_t,MAP);
		_t = _t.getFirstChild();
		{    // ( ... )*
			for (;;)
			{
				if (_t == null)
					_t = ASTNULL;
				if ((_t.Type==STATEMENT))
				{
					
					Map key=null;
					Map val=null;
					
					AST __t95 = _t;
					LineNumberAST tmp23_AST_in = (_t==ASTNULL) ? null : (LineNumberAST)_t;
					match((AST)_t,STATEMENT);
					_t = _t.getFirstChild();
					key=select(_t);
					_t = retTree_;
					val=expression(_t);
					_t = retTree_;
					
					Map statement=new Map();
										statement[Statement.keyString]=key;
										statement[Statement.valueString]=val;
										statements[new Integer(counter)]=statement;
										counter++;
									
					_t = __t95;
					_t = _t.getNextSibling();
				}
				else
				{
					goto _loop96_breakloop;
				}
				
			}
_loop96_breakloop:			;
		}    // ( ... )*
		_t = __t93;
		_t = _t.getNextSibling();
		
		result[Program.programString]=statements;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  select(AST _t) //throws RecognitionException
{
		Map result;
		
		LineNumberAST select_AST_in = (LineNumberAST)_t;
		
		result=new Map();
		Map selection=new Map();
		Map key=null;
		int counter=1;
		
		
		AST __t102 = _t;
		LineNumberAST tmp24_AST_in = (_t==ASTNULL) ? null : (LineNumberAST)_t;
		match((AST)_t,SELECT_KEY);
		_t = _t.getFirstChild();
		{
			{ // ( ... )+
			int _cnt105=0;
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
					if (_cnt105 >= 1) { goto _loop105_breakloop; } else { throw new NoViableAltException(_t);; }
				}
				
				_cnt105++;
			}
_loop105_breakloop:			;
			}    // ( ... )+
		}
		_t = __t102;
		_t = _t.getNextSibling();
		
		result[Select.selectString]=selection;
		
		retTree_ = _t;
		return result;
	}
	
	public Map  literal(AST _t) //throws RecognitionException
{
		Map result;
		
		LineNumberAST literal_AST_in = (LineNumberAST)_t;
		LineNumberAST token = null;
		
		result=new Map();
		
		
		token = (_t==ASTNULL) ? null : (LineNumberAST)_t;
		match((AST)_t,LITERAL);
		_t = _t.getNextSibling();
		
		result[Literal.literalString]=new Map(token.getText());
		
		retTree_ = _t;
		return result;
	}
	
	public Map  delayed(AST _t) //throws RecognitionException
{
		Map result;
		
		LineNumberAST delayed_AST_in = (LineNumberAST)_t;
		
		result=new Map();
		Map delayed;
		
		
		AST __t108 = _t;
		LineNumberAST tmp25_AST_in = (_t==ASTNULL) ? null : (LineNumberAST)_t;
		match((AST)_t,FUNCTION);
		_t = _t.getFirstChild();
		delayed=expression(_t);
		_t = retTree_;
		_t = __t108;
		_t = _t.getNextSibling();
		
		result[Delayed.delayedString]=delayed;
		
		retTree_ = _t;
		return result;
	}
	
	public new LineNumberAST getAST()
	{
		return (LineNumberAST) returnAST;
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
		@"""':'""",
		@"""'='""",
		@"""'['""",
		@"""']'""",
		@"""'('""",
		@"""')'""",
		@"""'.'""",
		@"""'#'""",
		@"""a key""",
		@"""a literal""",
		@"""whitespace""",
		@"""a line""",
		@"""a comment""",
		@"""SPACE""",
		@"""a newline""",
		@"""SELECT_KEY"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 136317696L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
}

}
