// $ANTLR 2.7.3: "Parser.g" -> "MetaLexer.cs"$

namespace Meta.Parser
{
	// Generate header specific to lexer CSharp file
	using System;
	using Stream                          = System.IO.Stream;
	using TextReader                      = System.IO.TextReader;
	using Hashtable                       = System.Collections.Hashtable;
	using Comparer                        = System.Collections.Comparer;
	
	using TokenStreamException            = antlr.TokenStreamException;
	using TokenStreamIOException          = antlr.TokenStreamIOException;
	using TokenStreamRecognitionException = antlr.TokenStreamRecognitionException;
	using CharStreamException             = antlr.CharStreamException;
	using CharStreamIOException           = antlr.CharStreamIOException;
	using ANTLRException                  = antlr.ANTLRException;
	using CharScanner                     = antlr.CharScanner;
	using InputBuffer                     = antlr.InputBuffer;
	using ByteBuffer                      = antlr.ByteBuffer;
	using CharBuffer                      = antlr.CharBuffer;
	using Token                           = antlr.Token;
	using CommonToken                     = antlr.CommonToken;
	using SemanticException               = antlr.SemanticException;
	using RecognitionException            = antlr.RecognitionException;
	using NoViableAltForCharException     = antlr.NoViableAltForCharException;
	using MismatchedCharException         = antlr.MismatchedCharException;
	using TokenStream                     = antlr.TokenStream;
	using LexerSharedInputState           = antlr.LexerSharedInputState;
	using BitSet                          = antlr.collections.impl.BitSet;
	
	public 	class MetaLexer : antlr.CharScanner	, TokenStream
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
		
		
    /**
     * Construct a token of the given type, augmenting it with end position
     * and file name information based on the shared input state of the
     * instance.
     *
     * @param t the token type for the result
     * @return non-null; the newly-constructed token 
     */
    protected override Token makeToken (int t)
    {
        MetaToken tok = (MetaToken) base.makeToken (t);
        ((ExtentLexerSharedInputState) inputState).annotate (tok);
        return tok;
    }
	public override void tab() 
	{
		setColumn(getColumn()+1);
	}
		public MetaLexer(Stream ins) : this(new ByteBuffer(ins))
		{
		}
		
		public MetaLexer(TextReader r) : this(new CharBuffer(r))
		{
		}
		
		public MetaLexer(InputBuffer ib)		 : this(new LexerSharedInputState(ib))
		{
		}
		
		public MetaLexer(LexerSharedInputState state) : base(state)
		{
			initialize();
		}
		private void initialize()
		{
			caseSensitiveLiterals = true;
			setCaseSensitive(true);
			literals = new Hashtable(null, Comparer.Default);
		}
		
		override public Token nextToken()			//throws TokenStreamException
		{
			Token theRetToken = null;
tryAgain:
			for (;;)
			{
				Token _token = null;
				int _ttype = Token.INVALID_TYPE;
				resetText();
				try     // for char stream error handling
				{
					try     // for lexical error handling
					{
						switch ( LA(1) )
						{
						case ':':
						{
							mCOLON(true);
							theRetToken = returnToken_;
							break;
						}
						case '=':
						{
							mEQUAL(true);
							theRetToken = returnToken_;
							break;
						}
						case '[':
						{
							mLBRACKET(true);
							theRetToken = returnToken_;
							break;
						}
						case ']':
						{
							mRBRACKET(true);
							theRetToken = returnToken_;
							break;
						}
						case '(':
						{
							mLPAREN(true);
							theRetToken = returnToken_;
							break;
						}
						case ')':
						{
							mRPAREN(true);
							theRetToken = returnToken_;
							break;
						}
						case '.':
						{
							mPOINT(true);
							theRetToken = returnToken_;
							break;
						}
						case '*':
						{
							mSTAR(true);
							theRetToken = returnToken_;
							break;
						}
						case '"':  case '\'':  case '@':
						{
							mLITERAL(true);
							theRetToken = returnToken_;
							break;
						}
						case ' ':
						{
							mSPACES(true);
							theRetToken = returnToken_;
							break;
						}
						case '\t':  case '\n':  case '\r':  case '/':
						{
							mLINE(true);
							theRetToken = returnToken_;
							break;
						}
						default:
							if ((tokenSet_0_.member(LA(1))))
							{
								mLITERAL_KEY(true);
								theRetToken = returnToken_;
							}
						else
						{
							if (LA(1)==EOF_CHAR) { uponEOF(); returnToken_ = makeToken(Token.EOF_TYPE); }
				else {throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());}
						}
						break; }
						if ( null==returnToken_ ) goto tryAgain; // found SKIP token
						_ttype = returnToken_.Type;
						_ttype = testLiteralsTable(_ttype);
						returnToken_.Type = _ttype;
						return returnToken_;
					}
					catch (RecognitionException e) {
							throw new TokenStreamRecognitionException(e);
					}
				}
				catch (CharStreamException cse) {
					if ( cse is CharStreamIOException ) {
						throw new TokenStreamIOException(((CharStreamIOException)cse).io);
					}
					else {
						throw new TokenStreamException(cse.Message);
					}
				}
			}
		}
		
	public void mCOLON(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = COLON;
		
		match(':');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mEQUAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = EQUAL;
		
		match('=');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mLBRACKET(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LBRACKET;
		
		match('[');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mRBRACKET(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = RBRACKET;
		
		match(']');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mLPAREN(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LPAREN;
		
		match('(');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mRPAREN(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = RPAREN;
		
		match(')');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mPOINT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = POINT;
		
		match('.');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mSTAR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = STAR;
		
		match('*');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mLITERAL_KEY(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL_KEY;
		
		{ // ( ... )+
		int _cnt12=0;
		for (;;)
		{
			if ((tokenSet_0_.member(LA(1))))
			{
				{
					match(tokenSet_0_);
				}
			}
			else
			{
				if (_cnt12 >= 1) { goto _loop12_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt12++;
		}
_loop12_breakloop:		;
		}    // ( ... )+
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mLITERAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL;
		
		switch ( LA(1) )
		{
		case '\'':
		{
			{
				int _saveIndex = 0;
				_saveIndex = text.Length;
				match('\'');
				text.Length = _saveIndex;
				{    // ( ... )*
					for (;;)
					{
						if ((tokenSet_1_.member(LA(1))))
						{
							{
								match(tokenSet_1_);
							}
						}
						else
						{
							goto _loop17_breakloop;
						}
						
					}
_loop17_breakloop:					;
				}    // ( ... )*
			}
			break;
		}
		case '"':
		{
			{
				int _saveIndex = 0;
				_saveIndex = text.Length;
				match("\"");
				text.Length = _saveIndex;
				{    // ( ... )*
					for (;;)
					{
						if ((tokenSet_2_.member(LA(1))))
						{
							{
								match(tokenSet_2_);
							}
						}
						else
						{
							goto _loop21_breakloop;
						}
						
					}
_loop21_breakloop:					;
				}    // ( ... )*
				_saveIndex = text.Length;
				match("\"");
				text.Length = _saveIndex;
			}
			break;
		}
		case '@':
		{
			{
				int _saveIndex = 0;
				_saveIndex = text.Length;
				match("@\"");
				text.Length = _saveIndex;
				{    // ( ... )*
					for (;;)
					{
						// nongreedy exit test
						if ((LA(1)=='"') && (LA(2)=='@')) goto _loop24_breakloop;
						if (((LA(1) >= '\u0000' && LA(1) <= '\ufffe')) && ((LA(2) >= '\u0000' && LA(2) <= '\ufffe')))
						{
							matchNot(EOF/*_CHAR*/);
						}
						else
						{
							goto _loop24_breakloop;
						}
						
					}
_loop24_breakloop:					;
				}    // ( ... )*
				_saveIndex = text.Length;
				match("\"@");
				text.Length = _saveIndex;
			}
			break;
		}
		default:
		{
			throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
		}
		 }
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	protected void mLITERAL_END(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL_END;
		
		if (!(LA(2)=='@'))
		  throw new SemanticException("LA(2)=='@'");
		int _saveIndex = 0;
		_saveIndex = text.Length;
		match("\"@");
		text.Length = _saveIndex;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mSPACES(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = SPACES;
		
		{ // ( ... )+
		int _cnt28=0;
		for (;;)
		{
			if ((LA(1)==' '))
			{
				match(' ');
			}
			else
			{
				if (_cnt28 >= 1) { goto _loop28_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt28++;
		}
_loop28_breakloop:		;
		}    // ( ... )+
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	public void mLINE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LINE;
		
		const int endOfFileValue=65535;
		
		
		bool synPredMatched58 = false;
		if (((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (tokenSet_3_.member(LA(2)))))
		{
			int _m58 = mark();
			synPredMatched58 = true;
			inputState.guessing++;
			try {
				{
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)=='\t'))
							{
								match('\t');
							}
							else
							{
								goto _loop52_breakloop;
							}
							
						}
_loop52_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)=='\t'))
							{
								match('\t');
							}
							else
							{
								goto _loop54_breakloop;
							}
							
						}
_loop54_breakloop:						;
					}    // ( ... )*
					match("//");
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_4_.member(LA(1))))
							{
								{
									match(tokenSet_4_);
								}
							}
							else
							{
								goto _loop57_breakloop;
							}
							
						}
_loop57_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched58 = false;
			}
			rewind(_m58);
			inputState.guessing--;
		}
		if ( synPredMatched58 )
		{
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)=='\t'))
					{
						match('\t');
					}
					else
					{
						goto _loop60_breakloop;
					}
					
				}
_loop60_breakloop:				;
			}    // ( ... )*
			mNEWLINE(false);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)=='\t'))
					{
						match('\t');
					}
					else
					{
						goto _loop62_breakloop;
					}
					
				}
_loop62_breakloop:				;
			}    // ( ... )*
			match("//");
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_4_.member(LA(1))))
					{
						{
							match(tokenSet_4_);
						}
					}
					else
					{
						goto _loop65_breakloop;
					}
					
				}
_loop65_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				_ttype = Token.SKIP; newline();
			}
		}
		else {
			bool synPredMatched73 = false;
			if (((LA(1)=='\t'||LA(1)=='/') && (LA(2)=='\t'||LA(2)=='/')))
			{
				int _m73 = mark();
				synPredMatched73 = true;
				inputState.guessing++;
				try {
					{
						{
							{    // ( ... )*
								for (;;)
								{
									if ((LA(1)=='\t'))
									{
										match('\t');
									}
									else
									{
										goto _loop69_breakloop;
									}
									
								}
_loop69_breakloop:								;
							}    // ( ... )*
						}
						match("//");
						{    // ( ... )*
							for (;;)
							{
								if ((tokenSet_4_.member(LA(1))))
								{
									{
										match(tokenSet_4_);
									}
								}
								else
								{
									goto _loop72_breakloop;
								}
								
							}
_loop72_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
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
				{
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)=='\t'))
							{
								match('\t');
							}
							else
							{
								goto _loop76_breakloop;
							}
							
						}
_loop76_breakloop:						;
					}    // ( ... )*
				}
				match("//");
				{    // ( ... )*
					for (;;)
					{
						if ((tokenSet_4_.member(LA(1))))
						{
							{
								match(tokenSet_4_);
							}
						}
						else
						{
							goto _loop79_breakloop;
						}
						
					}
_loop79_breakloop:					;
				}    // ( ... )*
				if (0==inputState.guessing)
				{
					_ttype = Token.SKIP;
				}
			}
			else {
				bool synPredMatched35 = false;
				if (((LA(1)=='\n'||LA(1)=='\r') && (true)))
				{
					int _m35 = mark();
					synPredMatched35 = true;
					inputState.guessing++;
					try {
						{
							mNEWLINE(false);
							{    // ( ... )*
								for (;;)
								{
									if ((LA(1)=='\t'))
									{
										mSPACE(false);
									}
									else
									{
										goto _loop32_breakloop;
									}
									
								}
_loop32_breakloop:								;
							}    // ( ... )*
							{    // ( ... )*
								for (;;)
								{
									if ((LA(1)=='\n'||LA(1)=='\r'))
									{
										mNEWLINE(false);
									}
									else
									{
										goto _loop34_breakloop;
									}
									
								}
_loop34_breakloop:								;
							}    // ( ... )*
							if (!(LA(1)==endOfFileValue))
							  throw new SemanticException("LA(1)==endOfFileValue");
						}
					}
					catch (RecognitionException)
					{
						synPredMatched35 = false;
					}
					rewind(_m35);
					inputState.guessing--;
				}
				if ( synPredMatched35 )
				{
					mNEWLINE(false);
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)=='\t'))
							{
								mSPACE(false);
							}
							else
							{
								goto _loop37_breakloop;
							}
							
						}
_loop37_breakloop:						;
					}    // ( ... )*
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)=='\n'||LA(1)=='\r'))
							{
								mNEWLINE(false);
							}
							else
							{
								goto _loop39_breakloop;
							}
							
						}
_loop39_breakloop:						;
					}    // ( ... )*
					if (!(LA(1)==endOfFileValue))
					  throw new SemanticException("LA(1)==endOfFileValue");
					if (0==inputState.guessing)
					{
						
						_ttype=Token.SKIP;
						
					}
				}
				else {
					bool synPredMatched45 = false;
					if (((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (true)))
					{
						int _m45 = mark();
						synPredMatched45 = true;
						inputState.guessing++;
						try {
							{
								{    // ( ... )*
									for (;;)
									{
										if ((LA(1)=='\t'))
										{
											match('\t');
										}
										else
										{
											goto _loop42_breakloop;
										}
										
									}
_loop42_breakloop:									;
								}    // ( ... )*
								mNEWLINE(false);
								{    // ( ... )*
									for (;;)
									{
										if ((LA(1)=='\t'))
										{
											match('\t');
										}
										else
										{
											goto _loop44_breakloop;
										}
										
									}
_loop44_breakloop:									;
								}    // ( ... )*
								mNEWLINE(false);
							}
						}
						catch (RecognitionException)
						{
							synPredMatched45 = false;
						}
						rewind(_m45);
						inputState.guessing--;
					}
					if ( synPredMatched45 )
					{
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)=='\t'))
								{
									match('\t');
								}
								else
								{
									goto _loop47_breakloop;
								}
								
							}
_loop47_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)=='\t'))
								{
									match('\t');
								}
								else
								{
									goto _loop49_breakloop;
								}
								
							}
_loop49_breakloop:							;
						}    // ( ... )*
						if (0==inputState.guessing)
						{
							
									_ttype=Token.SKIP;
								
						}
					}
					else if ((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (true)) {
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)=='\t'))
								{
									int _saveIndex = 0;
									_saveIndex = text.Length;
									match('\t');
									text.Length = _saveIndex;
								}
								else
								{
									goto _loop81_breakloop;
								}
								
							}
_loop81_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)=='\t'))
								{
									match('\t');
								}
								else
								{
									goto _loop83_breakloop;
								}
								
							}
_loop83_breakloop:							;
						}    // ( ... )*
						if (0==inputState.guessing)
						{
							
									_ttype=MetaLexerTokenTypes.INDENTATION;
								
						}
					}
					else
					{
						throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
					}
					}}}
					if (_createToken && (null == _token) && (_ttype != Token.SKIP))
					{
						_token = makeToken(_ttype);
						_token.setText(text.ToString(_begin, text.Length-_begin));
					}
					returnToken_ = _token;
				}
				
	protected void mNEWLINE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = NEWLINE;
		
		{
			switch ( LA(1) )
			{
			case '\n':
			{
				int _saveIndex = 0;
				_saveIndex = text.Length;
				match('\n');
				text.Length = _saveIndex;
				break;
			}
			case '\r':
			{
				{
					int _saveIndex = 0;
					_saveIndex = text.Length;
					match('\r');
					text.Length = _saveIndex;
					_saveIndex = text.Length;
					match('\n');
					text.Length = _saveIndex;
				}
				break;
			}
			default:
			{
				throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
			}
			 }
		}
		if (0==inputState.guessing)
		{
			
			newline();
			
		}
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	protected void mSPACE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = SPACE;
		
		int _saveIndex = 0;
		_saveIndex = text.Length;
		match('\t');
		text.Length = _saveIndex;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = new long[2048];
		data[0]=-2594292759409993217L;
		data[1]=-671088642L;
		for (int i = 2; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = new long[2048];
		data[0]=-2594147623875126785L;
		data[1]=-671088641L;
		for (int i = 2; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = new long[2048];
		data[0]=-17179869185L;
		for (int i = 1; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = new long[1025];
		data[0]=140737488365056L;
		for (int i = 1; i<=1024; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	private static long[] mk_tokenSet_4_()
	{
		long[] data = new long[2048];
		data[0]=-9217L;
		for (int i = 1; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());
	
}
}
