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
		public const int COMMENT = 25;
		public const int SPACE = 26;
		public const int NEWLINE = 27;
		public const int SELECT_KEY = 28;
		
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
						case '\n':  case '\r':
						{
							mLINE(true);
							theRetToken = returnToken_;
							break;
						}
						case '/':
						{
							mCOMMENT(true);
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
				_saveIndex = text.Length;
				match("\"");
				text.Length = _saveIndex;
			}
			break;
		}
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
						if ((((LA(1) >= '\u0000' && LA(1) <= '\ufffe')))&&(LA(1)!='"'||LA(2)!='@'))
						{
							{
								matchNot(EOF/*_CHAR*/);
							}
						}
						else
						{
							goto _loop25_breakloop;
						}
						
					}
_loop25_breakloop:					;
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
		int _cnt29=0;
		for (;;)
		{
			if ((LA(1)==' '))
			{
				match(' ');
			}
			else
			{
				if (_cnt29 >= 1) { goto _loop29_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt29++;
		}
_loop29_breakloop:		;
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
		
		
		bool synPredMatched36 = false;
		if (((LA(1)=='\n'||LA(1)=='\r')))
		{
			int _m36 = mark();
			synPredMatched36 = true;
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
								goto _loop33_breakloop;
							}
							
						}
_loop33_breakloop:						;
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
								goto _loop35_breakloop;
							}
							
						}
_loop35_breakloop:						;
					}    // ( ... )*
					if (!(LA(1)==endOfFileValue))
					  throw new SemanticException("LA(1)==endOfFileValue");
				}
			}
			catch (RecognitionException)
			{
				synPredMatched36 = false;
			}
			rewind(_m36);
			inputState.guessing--;
		}
		if ( synPredMatched36 )
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
						goto _loop38_breakloop;
					}
					
				}
_loop38_breakloop:				;
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
						goto _loop40_breakloop;
					}
					
				}
_loop40_breakloop:				;
			}    // ( ... )*
			if (!(LA(1)==endOfFileValue))
			  throw new SemanticException("LA(1)==endOfFileValue");
			if (0==inputState.guessing)
			{
				
				_ttype=Token.SKIP;
				
			}
		}
		else if ((LA(1)=='\n'||LA(1)=='\r')) {
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
						goto _loop42_breakloop;
					}
					
				}
_loop42_breakloop:				;
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
	
	public void mCOMMENT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = COMMENT;
		
		match("//");
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_3_.member(LA(1))))
				{
					{
						match(tokenSet_3_);
					}
				}
				else
				{
					goto _loop46_breakloop;
				}
				
			}
_loop46_breakloop:			;
		}    // ( ... )*
		{
			switch ( LA(1) )
			{
			case '\n':
			{
				match('\n');
				break;
			}
			case '\r':
			{
				match('\r');
				{
					if ((LA(1)=='\n'))
					{
						match('\n');
					}
					else {
					}
					
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
			_ttype = Token.SKIP; newline();
		}
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
		data[0]=-17179869185L;
		for (int i = 1; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = new long[2048];
		data[0]=-2594147623875126785L;
		data[1]=-671088641L;
		for (int i = 2; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = new long[2048];
		data[0]=-9217L;
		for (int i = 1; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	
}
}
