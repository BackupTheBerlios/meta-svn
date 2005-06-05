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
		public const int SELECT = 12;
		public const int SEARCH = 13;
		public const int KEY = 14;
		public const int DELAYED_EXPRESSION_ONLY = 15;
		public const int SAME_INDENT = 16;
		public const int EQUAL = 17;
		public const int HASH = 18;
		public const int COLON = 19;
		public const int LBRACKET = 20;
		public const int RBRACKET = 21;
		public const int LPAREN = 22;
		public const int RPAREN = 23;
		public const int POINT = 24;
		public const int LITERAL_KEY = 25;
		public const int LITERAL = 26;
		public const int SPACES = 27;
		public const int LINE = 28;
		public const int SPACE = 29;
		public const int NEWLINE = 30;
		public const int NEWLINE_KEEP_TEXT = 31;
		
		
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
		MetaAST expression_AST = null;
		
		{
			switch ( LA(1) )
			{
			case HASH:
			{
				delayedExpressionOnly();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case COLON:
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
				MetaAST tmp1_AST = null;
				tmp1_AST = (MetaAST) astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, (AST)tmp1_AST);
				match(LITERAL);
				break;
			}
			default:
				bool synPredMatched73 = false;
				if (((tokenSet_0_.member(LA(1)))))
				{
					int _m73 = mark();
					synPredMatched73 = true;
					inputState.guessing++;
					try {
						{
							callInParens();
							{
								switch ( LA(1) )
								{
								case ENDLINE:
								{
									match(ENDLINE);
									break;
								}
								case POINT:
								{
									break;
								}
								default:
								{
									throw new NoViableAltException(LT(1), getFilename());
								}
								 }
							}
							match(POINT);
						}
					}
					catch (RecognitionException)
					{
						synPredMatched73 = false;
					}
					rewind(_m73);
					inputState.guessing--;
				}
				if ( synPredMatched73 )
				{
					select();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
				else {
					bool synPredMatched75 = false;
					if (((tokenSet_0_.member(LA(1)))))
					{
						int _m75 = mark();
						synPredMatched75 = true;
						inputState.guessing++;
						try {
							{
								call();
							}
						}
						catch (RecognitionException)
						{
							synPredMatched75 = false;
						}
						rewind(_m75);
						inputState.guessing--;
					}
					if ( synPredMatched75 )
					{
						call();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
					}
					else {
						bool synPredMatched77 = false;
						if (((tokenSet_0_.member(LA(1)))))
						{
							int _m77 = mark();
							synPredMatched77 = true;
							inputState.guessing++;
							try {
								{
									select();
								}
							}
							catch (RecognitionException)
							{
								synPredMatched77 = false;
							}
							rewind(_m77);
							inputState.guessing--;
						}
						if ( synPredMatched77 )
						{
							select();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else if ((LA(1)==INDENT)) {
							map();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
							search();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
					else
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					}}break; }
				}
				expression_AST = (MetaAST)currentAST.root;
				returnAST = expression_AST;
			}
			
	public void callInParens() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST callInParens_AST = null;
		
		{
			match(LPAREN);
			{
				switch ( LA(1) )
				{
				case LITERAL:
				{
					MetaAST tmp3_AST = null;
					tmp3_AST = (MetaAST) astFactory.create(LT(1));
					astFactory.addASTChild(currentAST, (AST)tmp3_AST);
					match(LITERAL);
					break;
				}
				case COLON:
				{
					delayed();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
					break;
				}
				default:
					bool synPredMatched100 = false;
					if (((tokenSet_0_.member(LA(1)))))
					{
						int _m100 = mark();
						synPredMatched100 = true;
						inputState.guessing++;
						try {
							{
								select();
							}
						}
						catch (RecognitionException)
						{
							synPredMatched100 = false;
						}
						rewind(_m100);
						inputState.guessing--;
					}
					if ( synPredMatched100 )
					{
						select();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
					}
					else {
						bool synPredMatched102 = false;
						if (((tokenSet_0_.member(LA(1)))))
						{
							int _m102 = mark();
							synPredMatched102 = true;
							inputState.guessing++;
							try {
								{
									call();
									expression();
								}
							}
							catch (RecognitionException)
							{
								synPredMatched102 = false;
							}
							rewind(_m102);
							inputState.guessing--;
						}
						if ( synPredMatched102 )
						{
							call();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
							search();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else if ((LA(1)==INDENT)) {
							map();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
					else
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					}break; }
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
					case ENDLINE:
					case HASH:
					case COLON:
					case LBRACKET:
					case LPAREN:
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
					case ENDLINE:
					{
						match(ENDLINE);
						break;
					}
					case INDENT:
					case HASH:
					case COLON:
					case LBRACKET:
					case LPAREN:
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
					case LITERAL:
					{
						MetaAST tmp6_AST = null;
						tmp6_AST = (MetaAST) astFactory.create(LT(1));
						astFactory.addASTChild(currentAST, (AST)tmp6_AST);
						match(LITERAL);
						break;
					}
					case COLON:
					{
						delayed();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						break;
					}
					case HASH:
					{
						delayedExpressionOnly();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(currentAST, (AST)returnAST);
						}
						break;
					}
					default:
						bool synPredMatched107 = false;
						if (((tokenSet_0_.member(LA(1)))))
						{
							int _m107 = mark();
							synPredMatched107 = true;
							inputState.guessing++;
							try {
								{
									call();
								}
							}
							catch (RecognitionException)
							{
								synPredMatched107 = false;
							}
							rewind(_m107);
							inputState.guessing--;
						}
						if ( synPredMatched107 )
						{
							call();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(currentAST, (AST)returnAST);
							}
						}
						else {
							bool synPredMatched109 = false;
							if (((tokenSet_0_.member(LA(1)))))
							{
								int _m109 = mark();
								synPredMatched109 = true;
								inputState.guessing++;
								try {
									{
										select();
									}
								}
								catch (RecognitionException)
								{
									synPredMatched109 = false;
								}
								rewind(_m109);
								inputState.guessing--;
							}
							if ( synPredMatched109 )
							{
								select();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(currentAST, (AST)returnAST);
								}
							}
							else if ((LA(1)==INDENT)) {
								{
									map();
									if (0 == inputState.guessing)
									{
										astFactory.addASTChild(currentAST, (AST)returnAST);
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
									{
										switch ( LA(1) )
										{
										case ENDLINE:
										{
											match(ENDLINE);
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
								}
							}
							else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
								search();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(currentAST, (AST)returnAST);
								}
							}
						else
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						}break; }
					}
					match(RPAREN);
				}
				if (0==inputState.guessing)
				{
					callInParens_AST = (MetaAST)currentAST.root;
					
					callInParens_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(CALL)).add((AST)callInParens_AST));
					
					currentAST.root = callInParens_AST;
					if ( (null != callInParens_AST) && (null != callInParens_AST.getFirstChild()) )
						currentAST.child = callInParens_AST.getFirstChild();
					else
						currentAST.child = callInParens_AST;
					currentAST.advanceChildToEnd();
				}
				callInParens_AST = (MetaAST)currentAST.root;
				returnAST = callInParens_AST;
			}
			
	public void select() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST select_AST = null;
		
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
			case LPAREN:
			{
				callInParens();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case LBRACKET:
			case LITERAL_KEY:
			{
				search();
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
			case ENDLINE:
			{
				match(ENDLINE);
				break;
			}
			case POINT:
			{
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		{ // ( ... )+
		int _cnt128=0;
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
				if (_cnt128 >= 1) { goto _loop128_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
			}
			
			_cnt128++;
		}
_loop128_breakloop:		;
		}    // ( ... )+
		if (0==inputState.guessing)
		{
			select_AST = (MetaAST)currentAST.root;
			
					select_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(SELECT)).add((AST)select_AST));
				
			currentAST.root = select_AST;
			if ( (null != select_AST) && (null != select_AST.getFirstChild()) )
				currentAST.child = select_AST.getFirstChild();
			else
				currentAST.child = select_AST;
			currentAST.advanceChildToEnd();
		}
		select_AST = (MetaAST)currentAST.root;
		returnAST = select_AST;
	}
	
	public void call() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST call_AST = null;
		
		{
			bool synPredMatched95 = false;
			if (((LA(1)==LPAREN)))
			{
				int _m95 = mark();
				synPredMatched95 = true;
				inputState.guessing++;
				try {
					{
						match(LPAREN);
					}
				}
				catch (RecognitionException)
				{
					synPredMatched95 = false;
				}
				rewind(_m95);
				inputState.guessing--;
			}
			if ( synPredMatched95 )
			{
				callInParens();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
			}
			else if ((tokenSet_0_.member(LA(1)))) {
				normalCall();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
			}
			else
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			
		}
		call_AST = (MetaAST)currentAST.root;
		returnAST = call_AST;
	}
	
	public void map() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST map_AST = null;
		
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
						goto _loop81_breakloop;
					}
					
				}
_loop81_breakloop:				;
			}    // ( ... )*
			match(DEDENT);
		}
		if (0==inputState.guessing)
		{
			map_AST = (MetaAST)currentAST.root;
			
				  Counters.autokey.Pop();
				  map_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(MAP)).add((AST)map_AST));
				
			currentAST.root = map_AST;
			if ( (null != map_AST) && (null != map_AST.getFirstChild()) )
				currentAST.child = map_AST.getFirstChild();
			else
				currentAST.child = map_AST;
			currentAST.advanceChildToEnd();
		}
		map_AST = (MetaAST)currentAST.root;
		returnAST = map_AST;
	}
	
	public void delayedExpressionOnly() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST delayedExpressionOnly_AST = null;
		
		match(HASH);
		expression();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			delayedExpressionOnly_AST = (MetaAST)currentAST.root;
			
			delayedExpressionOnly_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(DELAYED_EXPRESSION_ONLY)).add((AST)delayedExpressionOnly_AST));
			
			currentAST.root = delayedExpressionOnly_AST;
			if ( (null != delayedExpressionOnly_AST) && (null != delayedExpressionOnly_AST.getFirstChild()) )
				currentAST.child = delayedExpressionOnly_AST.getFirstChild();
			else
				currentAST.child = delayedExpressionOnly_AST;
			currentAST.advanceChildToEnd();
		}
		delayedExpressionOnly_AST = (MetaAST)currentAST.root;
		returnAST = delayedExpressionOnly_AST;
	}
	
	public void delayed() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST delayed_AST = null;
		
		match(COLON);
		expression();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			delayed_AST = (MetaAST)currentAST.root;
			
			delayed_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(FUNCTION)).add((AST)delayed_AST));
			
			currentAST.root = delayed_AST;
			if ( (null != delayed_AST) && (null != delayed_AST.getFirstChild()) )
				currentAST.child = delayed_AST.getFirstChild();
			else
				currentAST.child = delayed_AST;
			currentAST.advanceChildToEnd();
		}
		delayed_AST = (MetaAST)currentAST.root;
		returnAST = delayed_AST;
	}
	
	public void search() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST search_AST = null;
		
		lookup();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			search_AST = (MetaAST)currentAST.root;
			
					search_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(SEARCH)).add((AST)search_AST));
				
			currentAST.root = search_AST;
			if ( (null != search_AST) && (null != search_AST.getFirstChild()) )
				currentAST.child = search_AST.getFirstChild();
			else
				currentAST.child = search_AST;
			currentAST.advanceChildToEnd();
		}
		search_AST = (MetaAST)currentAST.root;
		returnAST = search_AST;
	}
	
	public void statement() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST statement_AST = null;
		
		bool synPredMatched87 = false;
		if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
		{
			int _m87 = mark();
			synPredMatched87 = true;
			inputState.guessing++;
			try {
				{
					key();
					match(EQUAL);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched87 = false;
			}
			rewind(_m87);
			inputState.guessing--;
		}
		if ( synPredMatched87 )
		{
			{
				key();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				match(EQUAL);
				expression();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				if (0==inputState.guessing)
				{
					statement_AST = (MetaAST)currentAST.root;
					
					statement_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(STATEMENT)).add((AST)statement_AST));
					
					currentAST.root = statement_AST;
					if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
						currentAST.child = statement_AST.getFirstChild();
					else
						currentAST.child = statement_AST;
					currentAST.advanceChildToEnd();
				}
			}
			statement_AST = (MetaAST)currentAST.root;
		}
		else if ((tokenSet_1_.member(LA(1)))) {
			{
				{
					{
						switch ( LA(1) )
						{
						case EQUAL:
						{
							match(EQUAL);
							break;
						}
						case INDENT:
						case HASH:
						case COLON:
						case LBRACKET:
						case LPAREN:
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
					expression();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
					if (0==inputState.guessing)
					{
						statement_AST = (MetaAST)currentAST.root;
						
						//Counters.counter++;
						Counters.autokey.Push((int)Counters.autokey.Pop()+1); 
						
											// TODO: Simplify!!, use astFactory
										    MetaToken autokeyToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
										    autokeyToken.setLine(statement_AST.Extent.startLine); // TODO: Not sure this is the best way to do it, or if it's even correct
										    autokeyToken.setColumn(statement_AST.Extent.startColumn); 
										    autokeyToken.FileName=statement_AST.Extent.fileName;
										    autokeyToken.EndLine=statement_AST.Extent.endLine;
										    autokeyToken.EndColumn=statement_AST.Extent.endColumn;
										    MetaAST autokeyAst=new MetaAST(autokeyToken);
										    autokeyAst.setText(Counters.autokey.Peek().ToString());
						statement_AST=(MetaAST)astFactory.make( (new ASTArray(3)).add((AST)(MetaAST) astFactory.create(STATEMENT)).add((AST)(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(KEY)).add((AST)autokeyAst))).add((AST)statement_AST));
						
						currentAST.root = statement_AST;
						if ( (null != statement_AST) && (null != statement_AST.getFirstChild()) )
							currentAST.child = statement_AST.getFirstChild();
						else
							currentAST.child = statement_AST;
						currentAST.advanceChildToEnd();
					}
				}
			}
			statement_AST = (MetaAST)currentAST.root;
		}
		else
		{
			throw new NoViableAltException(LT(1), getFilename());
		}
		
		returnAST = statement_AST;
	}
	
	public void key() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST key_AST = null;
		
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
					goto _loop84_breakloop;
				}
				
			}
_loop84_breakloop:			;
		}    // ( ... )*
		if (0==inputState.guessing)
		{
			key_AST = (MetaAST)currentAST.root;
			
					key_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(KEY)).add((AST)key_AST));
				
			currentAST.root = key_AST;
			if ( (null != key_AST) && (null != key_AST.getFirstChild()) )
				currentAST.child = key_AST.getFirstChild();
			else
				currentAST.child = key_AST;
			currentAST.advanceChildToEnd();
		}
		key_AST = (MetaAST)currentAST.root;
		returnAST = key_AST;
	}
	
	public void lookup() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST lookup_AST = null;
		
		{
			switch ( LA(1) )
			{
			case LBRACKET:
			{
				squareBracketLookup();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case LITERAL_KEY:
			{
				literalKey();
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
		lookup_AST = (MetaAST)currentAST.root;
		returnAST = lookup_AST;
	}
	
	public void normalCall() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST normalCall_AST = null;
		
		{
			{
				bool synPredMatched117 = false;
				if (((tokenSet_0_.member(LA(1)))))
				{
					int _m117 = mark();
					synPredMatched117 = true;
					inputState.guessing++;
					try {
						{
							select();
						}
					}
					catch (RecognitionException)
					{
						synPredMatched117 = false;
					}
					rewind(_m117);
					inputState.guessing--;
				}
				if ( synPredMatched117 )
				{
					select();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
				else if ((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)) {
					search();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
				else
				{
					throw new NoViableAltException(LT(1), getFilename());
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
				case HASH:
				case COLON:
				case LBRACKET:
				case LPAREN:
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
				expression();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
			}
		}
		if (0==inputState.guessing)
		{
			normalCall_AST = (MetaAST)currentAST.root;
			
			normalCall_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(CALL)).add((AST)normalCall_AST));
			
			currentAST.root = normalCall_AST;
			if ( (null != normalCall_AST) && (null != normalCall_AST.getFirstChild()) )
				currentAST.child = normalCall_AST.getFirstChild();
			else
				currentAST.child = normalCall_AST;
			currentAST.advanceChildToEnd();
		}
		normalCall_AST = (MetaAST)currentAST.root;
		returnAST = normalCall_AST;
	}
	
	public void squareBracketLookup() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST squareBracketLookup_AST = null;
		
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
			case HASH:
			case COLON:
			case LBRACKET:
			case LPAREN:
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
			expression();
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
		squareBracketLookup_AST = (MetaAST)currentAST.root;
		returnAST = squareBracketLookup_AST;
	}
	
	public void literalKey() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST literalKey_AST = null;
		Token  token = null;
		MetaAST token_AST = null;
		
		token = LT(1);
		token_AST = (MetaAST) astFactory.create(token);
		astFactory.addASTChild(currentAST, (AST)token_AST);
		match(LITERAL_KEY);
		if (0==inputState.guessing)
		{
			
					token_AST.setType(LITERAL); // ugly hack, shouldn't there be a standard way to do this?
			//#literalKey=#([LITERAL,token.getText()]);
			
		}
		literalKey_AST = (MetaAST)currentAST.root;
		returnAST = literalKey_AST;
	}
	
	public new MetaAST getAST()
	{
		return (MetaAST) returnAST;
	}
	
	private void initializeFactory()
	{
		if (astFactory == null)
		{
			astFactory = new ASTFactory("MetaAST");
		}
		initializeASTFactory( astFactory );
	}
	static public void initializeASTFactory( ASTFactory factory )
	{
		factory.setMaxNodeType(31);
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
		@"""SAME_INDENT""",
		@"""EQUAL""",
		@"""HASH""",
		@"""COLON""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""LPAREN""",
		@"""RPAREN""",
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
		long[] data = { 38797344L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = { 106823712L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	
}
}
