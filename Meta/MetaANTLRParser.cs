// $ANTLR 2.7.3: "Parser.g" -> "MetaANTLRParser.cs"$

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

	public 	class MetaANTLRParser : antlr.LLkParser
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
		public const int MINUS = 12;
		public const int EQUAL = 13;
		public const int LBRACKET = 14;
		public const int RBRACKET = 15;
		public const int LPAREN = 16;
		public const int RPAREN = 17;
		public const int POINT = 18;
		public const int COMMA = 19;
		public const int LITERAL_KEY = 20;
		public const int LITERAL = 21;
		public const int SPACES = 22;
		public const int LINE = 23;
		public const int SPACE = 24;
		public const int NEWLINE = 25;
		public const int SELECT_KEY = 26;
		
		
		protected void initialize()
		{
			tokenNames = tokenNames_;
			initializeFactory();
		}
		
		
		protected MetaANTLRParser(TokenBuffer tokenBuf, int k) : base(tokenBuf, k)
		{
			initialize();
		}
		
		public MetaANTLRParser(TokenBuffer tokenBuf) : this(tokenBuf,1)
		{
		}
		
		protected MetaANTLRParser(TokenStream lexer, int k) : base(lexer,k)
		{
			initialize();
		}
		
		public MetaANTLRParser(TokenStream lexer) : this(lexer,1)
		{
		}
		
		public MetaANTLRParser(ParserSharedInputState state) : base(state,1)
		{
			initialize();
		}
		
	public void expression() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST expression_AST = null;
		
		{
			bool synPredMatched40 = false;
			if (((tokenSet_0_.member(LA(1)))))
			{
				int _m40 = mark();
				synPredMatched40 = true;
				inputState.guessing++;
				try {
					{
						call();
					}
				}
				catch (RecognitionException)
				{
					synPredMatched40 = false;
				}
				rewind(_m40);
				inputState.guessing--;
			}
			if ( synPredMatched40 )
			{
				call();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
			}
			else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
			}
			else if ((LA(1)==INDENT||LA(1)==LPAREN)) {
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
			}
			else if ((LA(1)==MINUS)) {
				delayed();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
			}
			else if ((LA(1)==LITERAL)) {
				AST tmp1_AST = null;
				tmp1_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp1_AST);
				match(LITERAL);
			}
			else
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			
		}
		expression_AST = currentAST.root;
		returnAST = expression_AST;
	}
	
	public void call() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST call_AST = null;
		
		{
			switch ( LA(1) )
			{
			case LBRACKET:
			case LITERAL_KEY:
			{
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				break;
			}
			case INDENT:
			case LPAREN:
			{
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				break;
			}
			case LITERAL:
			{
				AST tmp2_AST = null;
				tmp2_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp2_AST);
				match(LITERAL);
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
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				break;
			}
			case INDENT:
			case LPAREN:
			{
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				break;
			}
			case MINUS:
			{
				delayed();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				break;
			}
			case LITERAL:
			{
				AST tmp3_AST = null;
				tmp3_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp3_AST);
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
			call_AST = (AST)currentAST.root;
			
			call_AST=(AST)astFactory.make( (new ASTArray(2)).add(astFactory.create(CALL)).add(call_AST));
			
			currentAST.root = call_AST;
			if ( (null != call_AST) && (null != call_AST.getFirstChild()) )
				currentAST.child = call_AST.getFirstChild();
			else
				currentAST.child = call_AST;
			currentAST.advanceChildToEnd();
		}
		call_AST = currentAST.root;
		returnAST = call_AST;
	}
	
	public void select() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST select_AST = null;
		
		lookup();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, returnAST);
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
						astFactory.addASTChild(currentAST, returnAST);
					}
				}
				else
				{
					goto _loop65_breakloop;
				}
				
			}
_loop65_breakloop:			;
		}    // ( ... )*
		if (0==inputState.guessing)
		{
			select_AST = (AST)currentAST.root;
			
			select_AST=(AST)astFactory.make( (new ASTArray(2)).add(astFactory.create(SELECT_KEY)).add(select_AST));
			
			currentAST.root = select_AST;
			if ( (null != select_AST) && (null != select_AST.getFirstChild()) )
				currentAST.child = select_AST.getFirstChild();
			else
				currentAST.child = select_AST;
			currentAST.advanceChildToEnd();
		}
		select_AST = currentAST.root;
		returnAST = select_AST;
	}
	
	public void map() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST map_AST = null;
		
		if (0==inputState.guessing)
		{
			
			Counters.autokey.Push(0);
			
		}
		{
			switch ( LA(1) )
			{
			case LPAREN:
			{
				{
					match(LPAREN);
					{
						switch ( LA(1) )
						{
						case INDENT:
						case MINUS:
						case EQUAL:
						case LBRACKET:
						case LPAREN:
						case LITERAL_KEY:
						case LITERAL:
						{
							statement();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, returnAST);
							}
							{    // ( ... )*
								for (;;)
								{
									if ((LA(1)==COMMA))
									{
										match(COMMA);
										statement();
										if (0 == inputState.guessing)
										{
											astFactory.addASTChild(currentAST, returnAST);
										}
									}
									else
									{
										goto _loop46_breakloop;
									}
									
								}
_loop46_breakloop:								;
							}    // ( ... )*
							break;
						}
						case RPAREN:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					match(RPAREN);
				}
				break;
			}
			case INDENT:
			{
				{
					match(INDENT);
					statement();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, returnAST);
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
									astFactory.addASTChild(currentAST, returnAST);
								}
							}
							else
							{
								goto _loop49_breakloop;
							}
							
						}
_loop49_breakloop:						;
					}    // ( ... )*
					match(DEDENT);
				}
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
			map_AST = (AST)currentAST.root;
			
				  Counters.autokey.Pop();
				  map_AST=(AST)astFactory.make( (new ASTArray(2)).add(astFactory.create(MAP)).add(map_AST));
				
			currentAST.root = map_AST;
			if ( (null != map_AST) && (null != map_AST.getFirstChild()) )
				currentAST.child = map_AST.getFirstChild();
			else
				currentAST.child = map_AST;
			currentAST.advanceChildToEnd();
		}
		map_AST = currentAST.root;
		returnAST = map_AST;
	}
	
	public void delayed() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST delayed_AST = null;
		
		match(MINUS);
		expression();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, returnAST);
		}
		if (0==inputState.guessing)
		{
			delayed_AST = (AST)currentAST.root;
			
			delayed_AST=(AST)astFactory.make( (new ASTArray(2)).add(astFactory.create(FUNCTION)).add(delayed_AST));
			
			currentAST.root = delayed_AST;
			if ( (null != delayed_AST) && (null != delayed_AST.getFirstChild()) )
				currentAST.child = delayed_AST.getFirstChild();
			else
				currentAST.child = delayed_AST;
			currentAST.advanceChildToEnd();
		}
		delayed_AST = currentAST.root;
		returnAST = delayed_AST;
	}
	
	public void statement() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST statement_AST = null;
		
		bool synPredMatched52 = false;
		if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
		{
			int _m52 = mark();
			synPredMatched52 = true;
			inputState.guessing++;
			try {
				{
					select();
					match(EQUAL);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched52 = false;
			}
			rewind(_m52);
			inputState.guessing--;
		}
		if ( synPredMatched52 )
		{
			{
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				match(EQUAL);
				expression();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, returnAST);
				}
				if (0==inputState.guessing)
				{
					statement_AST = (AST)currentAST.root;
					
					statement_AST=(AST)astFactory.make( (new ASTArray(2)).add(astFactory.create(STATEMENT)).add(statement_AST));
					
					currentAST.root = statement_AST;
					if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
						currentAST.child = statement_AST.getFirstChild();
					else
						currentAST.child = statement_AST;
					currentAST.advanceChildToEnd();
				}
			}
			statement_AST = currentAST.root;
		}
		else if ((tokenSet_1_.member(LA(1)))) {
			{
				switch ( LA(1) )
				{
				case EQUAL:
				{
					{
						match(EQUAL);
						expression();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							statement_AST = (AST)currentAST.root;
							
							//Counters.counter++;
							Counters.autokey.Push((int)Counters.autokey.Pop()+1);
										      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
											    CommonAST currentAst=new CommonAST(currentToken);
											    currentAst.setText("this");
							
											    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
											    CommonAST autokeyAst=new CommonAST(autokeyToken);
											    autokeyAst.setText(Counters.autokey.Peek().ToString());
							statement_AST=(AST)astFactory.make( (new ASTArray(3)).add(astFactory.create(STATEMENT)).add((AST)astFactory.make( (new ASTArray(3)).add(astFactory.create(SELECT_KEY)).add(currentAst).add(autokeyAst))).add(statement_AST));
							
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
				case MINUS:
				case LBRACKET:
				case LPAREN:
				case LITERAL_KEY:
				case LITERAL:
				{
					{
						expression();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							statement_AST = (AST)currentAST.root;
							
							Counters.autokey.Push((int)Counters.autokey.Pop()+1);
										      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
											    CommonAST currentAst=new CommonAST(currentToken);
											    currentAst.setText("this");
							
											    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
											    CommonAST autokeyAst=new CommonAST(autokeyToken);
											    autokeyAst.setText(Counters.autokey.Peek().ToString());
							statement_AST=(AST)astFactory.make( (new ASTArray(3)).add(astFactory.create(STATEMENT)).add((AST)astFactory.make( (new ASTArray(3)).add(astFactory.create(SELECT_KEY)).add(currentAst).add(autokeyAst))).add(statement_AST));
							
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
			statement_AST = currentAST.root;
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
		AST lookup_AST = null;
		
		switch ( LA(1) )
		{
		case LBRACKET:
		{
			{
				match(LBRACKET);
				{
					bool synPredMatched70 = false;
					if (((tokenSet_0_.member(LA(1)))))
					{
						int _m70 = mark();
						synPredMatched70 = true;
						inputState.guessing++;
						try {
							{
								call();
							}
						}
						catch (RecognitionException)
						{
							synPredMatched70 = false;
						}
						rewind(_m70);
						inputState.guessing--;
					}
					if ( synPredMatched70 )
					{
						call();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
					}
					else if ((LA(1)==INDENT||LA(1)==LPAREN)) {
						map();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
					}
					else if ((LA(1)==LITERAL)) {
						AST tmp15_AST = null;
						tmp15_AST = astFactory.create(LT(1));
						astFactory.addASTChild(currentAST, tmp15_AST);
						match(LITERAL);
					}
					else if ((LA(1)==MINUS)) {
						delayed();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
					}
					else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
						select();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, returnAST);
						}
					}
					else
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					
				}
				match(RBRACKET);
			}
			lookup_AST = currentAST.root;
			break;
		}
		case LITERAL_KEY:
		{
			literalKey();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(currentAST, returnAST);
			}
			lookup_AST = currentAST.root;
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
		AST literalKey_AST = null;
		Token  token = null;
		AST token_AST = null;
		
		token = LT(1);
		token_AST = astFactory.create(token);
		astFactory.addASTChild(currentAST, token_AST);
		match(LITERAL_KEY);
		if (0==inputState.guessing)
		{
			literalKey_AST = (AST)currentAST.root;
			
			literalKey_AST=(AST)astFactory.make( (new ASTArray(1)).add(astFactory.create(LITERAL,token.getText())));
			
			currentAST.root = literalKey_AST;
			if ( (null != literalKey_AST) && (null != literalKey_AST.getFirstChild()) )
				currentAST.child = literalKey_AST.getFirstChild();
			else
				currentAST.child = literalKey_AST;
			currentAST.advanceChildToEnd();
		}
		literalKey_AST = currentAST.root;
		returnAST = literalKey_AST;
	}
	
	private void initializeFactory()
	{
		if (astFactory == null)
		{
			astFactory = new ASTFactory();
		}
		initializeASTFactory( astFactory );
	}
	static public void initializeASTFactory( ASTFactory factory )
	{
		factory.setMaxNodeType(26);
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
		@"""MINUS""",
		@"""EQUAL""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""LPAREN""",
		@"""RPAREN""",
		@"""POINT""",
		@"""COMMA""",
		@"""LITERAL_KEY""",
		@"""LITERAL""",
		@"""SPACES""",
		@"""LINE""",
		@"""SPACE""",
		@"""NEWLINE""",
		@"""SELECT_KEY"""
	};
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 3227680L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = { 3239968L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	
}
}
