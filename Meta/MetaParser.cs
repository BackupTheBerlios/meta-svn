// $ANTLR 2.7.3: "Parser.g" -> "MetaParser.cs"$

namespace Meta.Parser
{
	// Generate the header common to all output files.
	using System;
	
	using TokenBuffer              = antlr.TokenBuffer;
	using TokenStreamException     = antlr.TokenStreamException;
	using TokenStreamIOException   = antlr.TokenStreamIOException;
	using ANTLRException           = antlr.ANTLRException;
	using LLkParser = antlr.LLkParser;
	using Token                    = antlr.Token;
	using TokenStream              = antlr.TokenStream;
	using RecognitionException     = antlr.RecognitionException;
	using NoViableAltException     = antlr.NoViableAltException;
	using MismatchedTokenException = antlr.MismatchedTokenException;
	using SemanticException        = antlr.SemanticException;
	using ParserSharedInputState   = antlr.ParserSharedInputState;
	using BitSet                   = antlr.collections.impl.BitSet;
	using AST                      = antlr.collections.AST;
	using ASTPair                  = antlr.ASTPair;
	using ASTFactory               = antlr.ASTFactory;
	using ASTArray                 = antlr.collections.impl.ASTArray;
	
  using antlr;
  using System.Collections;
  class Counters
  {
    public static Stack autokey=new Stack();
  }

	public 	class MetaParser : antlr.LLkParser
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
		public const int STAR = 19;
		public const int LITERAL_KEY = 20;
		public const int LITERAL = 21;
		public const int LITERAL_END = 22;
		public const int SPACES = 23;
		public const int LINE = 24;
		public const int SPACE = 25;
		public const int NEWLINE = 26;
		public const int SELECT_KEY = 27;
		
		
		protected void initialize()
		{
			tokenNames = tokenNames_;
			initializeFactory();
		}
		
		
		protected MetaParser(TokenBuffer tokenBuf, int k) : base(tokenBuf, k)
		{
			initialize();
		}
		
		public MetaParser(TokenBuffer tokenBuf) : this(tokenBuf,1)
		{
		}
		
		protected MetaParser(TokenStream lexer, int k) : base(lexer,k)
		{
			initialize();
		}
		
		public MetaParser(TokenStream lexer) : this(lexer,1)
		{
		}
		
		public MetaParser(ParserSharedInputState state) : base(state,1)
		{
			initialize();
		}
		
	public void expression() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST expression_AST = null;
		
		{
			switch ( LA(1) )
			{
			case INDENT:
			{
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case EQUAL:
			{
				delayed();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case LITERAL:
			{
				TokenAST tmp1_AST = null;
				tmp1_AST = (TokenAST) astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, (AST)tmp1_AST);
				match(LITERAL);
				break;
			}
			default:
				bool synPredMatched91 = false;
				if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
				{
					int _m91 = mark();
					synPredMatched91 = true;
					inputState.guessing++;
					try {
						{
							call();
						}
					}
					catch (RecognitionException)
					{
						synPredMatched91 = false;
					}
					rewind(_m91);
					inputState.guessing--;
				}
				if ( synPredMatched91 )
				{
					call();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
				else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
					select();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
			else
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			break; }
		}
		expression_AST = (TokenAST)currentAST.root;
		returnAST = expression_AST;
	}
	
	public void call() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST call_AST = null;
		
		{
			select();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(currentAST, (AST)returnAST);
			}
		}
		{
			switch ( LA(1) )
			{
			case SPACES:
			{
				match(SPACES);
				break;
			}
			case INDENT:
			case LBRACKET:
			case LITERAL_KEY:
			case LITERAL:
			{
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		{
			switch ( LA(1) )
			{
			case LBRACKET:
			case LITERAL_KEY:
			{
				call();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case INDENT:
			{
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case LITERAL:
			{
				TokenAST tmp3_AST = null;
				tmp3_AST = (TokenAST) astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, (AST)tmp3_AST);
				match(LITERAL);
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		if (0==inputState.guessing)
		{
			call_AST = (TokenAST)currentAST.root;
			
			call_AST=(TokenAST)astFactory.make( (new ASTArray(2)).add((AST)(TokenAST) astFactory.create(CALL)).add((AST)call_AST));
			
			currentAST.root = call_AST;
			if ( (null != call_AST) && (null != call_AST.getFirstChild()) )
				currentAST.child = call_AST.getFirstChild();
			else
				currentAST.child = call_AST;
			currentAST.advanceChildToEnd();
		}
		call_AST = (TokenAST)currentAST.root;
		returnAST = call_AST;
	}
	
	public void select() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST select_AST = null;
		
		bool synPredMatched115 = false;
		if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
		{
			int _m115 = mark();
			synPredMatched115 = true;
			inputState.guessing++;
			try {
				{
					lookup();
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==POINT))
							{
								match(POINT);
								lookup();
							}
							else
							{
								goto _loop114_breakloop;
							}
							
						}
_loop114_breakloop:						;
					}    // ( ... )*
					match(STAR);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched115 = false;
			}
			rewind(_m115);
			inputState.guessing--;
		}
		if ( synPredMatched115 )
		{
			{
				lookup();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==POINT))
						{
							match(POINT);
							lookup();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else
						{
							goto _loop118_breakloop;
						}
						
					}
_loop118_breakloop:					;
				}    // ( ... )*
				match(STAR);
				if (0==inputState.guessing)
				{
					select_AST = (TokenAST)currentAST.root;
					
					Counters.autokey.Push((int)Counters.autokey.Pop()+1);
								Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
								TokenAST currentAst=new TokenAST(currentToken);
								currentAst.setText("search");
					
								//Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
								//TokenAST autokeyAst=new TokenAST(autokeyToken);
								//autokeyAst.setText(Counters.autokey.Peek().ToString());
					select_AST=(TokenAST)astFactory.make( (new ASTArray(3)).add((AST)(TokenAST) astFactory.create(SELECT_KEY)).add((AST)currentAst).add((AST)select_AST));
					//#select=#([SELECT_KEY],#select);
					
					currentAST.root = select_AST;
					if ( (null != select_AST) && (null != select_AST.getFirstChild()) )
						currentAST.child = select_AST.getFirstChild();
					else
						currentAST.child = select_AST;
					currentAST.advanceChildToEnd();
				}
			}
			select_AST = (TokenAST)currentAST.root;
		}
		else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
			{
				lookup();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==POINT))
						{
							match(POINT);
							lookup();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else
						{
							goto _loop121_breakloop;
						}
						
					}
_loop121_breakloop:					;
				}    // ( ... )*
				if (0==inputState.guessing)
				{
					select_AST = (TokenAST)currentAST.root;
					
					select_AST=(TokenAST)astFactory.make( (new ASTArray(2)).add((AST)(TokenAST) astFactory.create(SELECT_KEY)).add((AST)select_AST));
					
					currentAST.root = select_AST;
					if ( (null != select_AST) && (null != select_AST.getFirstChild()) )
						currentAST.child = select_AST.getFirstChild();
					else
						currentAST.child = select_AST;
					currentAST.advanceChildToEnd();
				}
			}
			select_AST = (TokenAST)currentAST.root;
		}
		else
		{
			throw new NoViableAltException(LT(1), getFilename());
		}
		
		returnAST = select_AST;
	}
	
	public void map() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST map_AST = null;
		
		if (0==inputState.guessing)
		{
			
			Counters.autokey.Push(0);
			
		}
		{
			match(INDENT);
			statement();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(currentAST, (AST)returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==ENDLINE))
					{
						match(ENDLINE);
						statement();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
					}
					else
					{
						goto _loop95_breakloop;
					}
					
				}
_loop95_breakloop:				;
			}    // ( ... )*
			match(DEDENT);
		}
		if (0==inputState.guessing)
		{
			map_AST = (TokenAST)currentAST.root;
			
				  Counters.autokey.Pop();
				  map_AST=(TokenAST)astFactory.make( (new ASTArray(2)).add((AST)(TokenAST) astFactory.create(MAP)).add((AST)map_AST));
				
			currentAST.root = map_AST;
			if ( (null != map_AST) && (null != map_AST.getFirstChild()) )
				currentAST.child = map_AST.getFirstChild();
			else
				currentAST.child = map_AST;
			currentAST.advanceChildToEnd();
		}
		map_AST = (TokenAST)currentAST.root;
		returnAST = map_AST;
	}
	
	public void delayed() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST delayed_AST = null;
		
		match(EQUAL);
		expression();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			delayed_AST = (TokenAST)currentAST.root;
			
			delayed_AST=(TokenAST)astFactory.make( (new ASTArray(2)).add((AST)(TokenAST) astFactory.create(FUNCTION)).add((AST)delayed_AST));
			
			currentAST.root = delayed_AST;
			if ( (null != delayed_AST) && (null != delayed_AST.getFirstChild()) )
				currentAST.child = delayed_AST.getFirstChild();
			else
				currentAST.child = delayed_AST;
			currentAST.advanceChildToEnd();
		}
		delayed_AST = (TokenAST)currentAST.root;
		returnAST = delayed_AST;
	}
	
	public void statement() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST statement_AST = null;
		
		bool synPredMatched98 = false;
		if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
		{
			int _m98 = mark();
			synPredMatched98 = true;
			inputState.guessing++;
			try {
				{
					select();
					match(COLON);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched98 = false;
			}
			rewind(_m98);
			inputState.guessing--;
		}
		if ( synPredMatched98 )
		{
			{
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				match(COLON);
				expression();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				if (0==inputState.guessing)
				{
					statement_AST = (TokenAST)currentAST.root;
					
					statement_AST=(TokenAST)astFactory.make( (new ASTArray(2)).add((AST)(TokenAST) astFactory.create(STATEMENT)).add((AST)statement_AST));
					
					currentAST.root = statement_AST;
					if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
						currentAST.child = statement_AST.getFirstChild();
					else
						currentAST.child = statement_AST;
					currentAST.advanceChildToEnd();
				}
			}
			statement_AST = (TokenAST)currentAST.root;
		}
		else if ((tokenSet_0_.member(LA(1)))) {
			{
				switch ( LA(1) )
				{
				case COLON:
				{
					{
						match(COLON);
						expression();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						if (0==inputState.guessing)
						{
							statement_AST = (TokenAST)currentAST.root;
							
							//Counters.counter++;
							Counters.autokey.Push((int)Counters.autokey.Pop()+1);
										      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
											    TokenAST currentAst=new TokenAST(currentToken);
											    currentAst.setText("this");
							
											    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
											    TokenAST autokeyAst=new TokenAST(autokeyToken);
											    autokeyAst.setText(Counters.autokey.Peek().ToString());
							statement_AST=(TokenAST)astFactory.make( (new ASTArray(3)).add((AST)(TokenAST) astFactory.create(STATEMENT)).add((AST)(TokenAST)astFactory.make( (new ASTArray(3)).add((AST)(TokenAST) astFactory.create(SELECT_KEY)).add((AST)currentAst).add((AST)autokeyAst))).add((AST)statement_AST));
							
							currentAST.root = statement_AST;
							if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
								currentAST.child = statement_AST.getFirstChild();
							else
								currentAST.child = statement_AST;
							currentAST.advanceChildToEnd();
						}
					}
					break;
				}
				case INDENT:
				case EQUAL:
				case LBRACKET:
				case LITERAL_KEY:
				case LITERAL:
				{
					{
						expression();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						if (0==inputState.guessing)
						{
							statement_AST = (TokenAST)currentAST.root;
							
							Counters.autokey.Push((int)Counters.autokey.Pop()+1);
										      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
											    TokenAST currentAst=new TokenAST(currentToken);
											    currentAst.setText("this");
							
											    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
											    TokenAST autokeyAst=new TokenAST(autokeyToken);
											    autokeyAst.setText(Counters.autokey.Peek().ToString());
							statement_AST=(TokenAST)astFactory.make( (new ASTArray(3)).add((AST)(TokenAST) astFactory.create(STATEMENT)).add((AST)(TokenAST)astFactory.make( (new ASTArray(3)).add((AST)(TokenAST) astFactory.create(SELECT_KEY)).add((AST)currentAst).add((AST)autokeyAst))).add((AST)statement_AST));
							
							currentAST.root = statement_AST;
							if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
								currentAST.child = statement_AST.getFirstChild();
							else
								currentAST.child = statement_AST;
							currentAST.advanceChildToEnd();
						}
					}
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case SPACES:
				{
					match(SPACES);
					break;
				}
				case ENDLINE:
				case DEDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			statement_AST = (TokenAST)currentAST.root;
		}
		else
		{
			throw new NoViableAltException(LT(1), getFilename());
		}
		
		returnAST = statement_AST;
	}
	
	public void lookup() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST lookup_AST = null;
		
		switch ( LA(1) )
		{
		case LBRACKET:
		{
			{
				match(LBRACKET);
				{
					switch ( LA(1) )
					{
					case SPACES:
					{
						match(SPACES);
						break;
					}
					case INDENT:
					case EQUAL:
					case LBRACKET:
					case LITERAL_KEY:
					case LITERAL:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				{
					switch ( LA(1) )
					{
					case INDENT:
					{
						map();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						break;
					}
					case LITERAL:
					{
						TokenAST tmp16_AST = null;
						tmp16_AST = (TokenAST) astFactory.create(LT(1));
						astFactory.addASTChild(currentAST, (AST)tmp16_AST);
						match(LITERAL);
						break;
					}
					case EQUAL:
					{
						delayed();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						break;
					}
					case LBRACKET:
					case LITERAL_KEY:
					{
						select();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				{
					switch ( LA(1) )
					{
					case SPACES:
					{
						match(SPACES);
						break;
					}
					case ENDLINE:
					case RBRACKET:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				{
					switch ( LA(1) )
					{
					case ENDLINE:
					{
						match(ENDLINE);
						break;
					}
					case RBRACKET:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				match(RBRACKET);
			}
			lookup_AST = (TokenAST)currentAST.root;
			break;
		}
		case LITERAL_KEY:
		{
			literalKey();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(currentAST, (AST)returnAST);
			}
			lookup_AST = (TokenAST)currentAST.root;
			break;
		}
		default:
		{
			throw new NoViableAltException(LT(1), getFilename());
		}
		 }
		returnAST = lookup_AST;
	}
	
	public void literalKey() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		TokenAST literalKey_AST = null;
		Token  token = null;
		TokenAST token_AST = null;
		
		token = LT(1);
		token_AST = (TokenAST) astFactory.create(token);
		astFactory.addASTChild(currentAST, (AST)token_AST);
		match(LITERAL_KEY);
		if (0==inputState.guessing)
		{
			literalKey_AST = (TokenAST)currentAST.root;
			
			literalKey_AST=(TokenAST)astFactory.make( (new ASTArray(1)).add((AST)(TokenAST) astFactory.create(LITERAL,token.getText())));
			
			currentAST.root = literalKey_AST;
			if ( (null != literalKey_AST) && (null != literalKey_AST.getFirstChild()) )
				currentAST.child = literalKey_AST.getFirstChild();
			else
				currentAST.child = literalKey_AST;
			currentAST.advanceChildToEnd();
		}
		literalKey_AST = (TokenAST)currentAST.root;
		returnAST = literalKey_AST;
	}
	
	public new TokenAST getAST()
	{
		return (TokenAST) returnAST;
	}
	
	private void initializeFactory()
	{
		if (astFactory == null)
		{
			astFactory = new ASTFactory("TokenAST");
		}
		initializeASTFactory( astFactory );
	}
	static public void initializeASTFactory( ASTFactory factory )
	{
		factory.setMaxNodeType(27);
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
		@"""'*'""",
		@"""a key""",
		@"""a literal""",
		@"""LITERAL_END""",
		@"""whitespace""",
		@"""a line""",
		@"""SPACE""",
		@"""a newline""",
		@"""SELECT_KEY"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 3174432L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	
}
}
