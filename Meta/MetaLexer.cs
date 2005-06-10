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
		public const int POINT = 22;
		public const int LITERAL_KEY = 23;
		public const int LITERAL = 24;
		public const int SPACES = 25;
		public const int LINE = 26;
		public const int SPACE = 27;
		public const int NEWLINE = 28;
		public const int NEWLINE_KEEP_TEXT = 29;
		
		
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
						case '=':
						{
							mEQUAL(true);
							theRetToken = returnToken_;
							break;
						}
						case '#':
						{
							mHASH(true);
							theRetToken = returnToken_;
							break;
						}
						case ':':
						{
							mCOLON(true);
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
						case '.':
						{
							mPOINT(true);
							theRetToken = returnToken_;
							break;
						}
						case '"':  case '\'':
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
	
	public void mHASH(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = HASH;
		
		match('#');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
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
	
	public void mLITERAL_KEY(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL_KEY;
		
		{ // ( ... )+
		int _cnt10=0;
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
				if (_cnt10 >= 1) { goto _loop10_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt10++;
		}
_loop10_breakloop:		;
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
							goto _loop15_breakloop;
						}
						
					}
_loop15_breakloop:					;
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
								{
									match(tokenSet_2_);
								}
							}
						}
						else if ((LA(1)=='\n'||LA(1)=='\r')) {
							mNEWLINE_KEEP_TEXT(false);
						}
						else
						{
							goto _loop20_breakloop;
						}
						
					}
_loop20_breakloop:					;
				}    // ( ... )*
				_saveIndex = text.Length;
				match("\"");
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
	
	protected void mNEWLINE_KEEP_TEXT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = NEWLINE_KEEP_TEXT;
		
		{
			switch ( LA(1) )
			{
			case '\r':
			{
				{
					match('\r');
					match('\n');
				}
				break;
			}
			case '\n':
			{
				match('\n');
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
	
	public void mSPACES(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = SPACES;
		
		{ // ( ... )+
		int _cnt23=0;
		for (;;)
		{
			if ((LA(1)==' '))
			{
				match(' ');
			}
			else
			{
				if (_cnt23 >= 1) { goto _loop23_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt23++;
		}
_loop23_breakloop:		;
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
		
		
		bool synPredMatched33 = false;
		if (((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (tokenSet_3_.member(LA(2)))))
		{
			int _m33 = mark();
			synPredMatched33 = true;
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
								goto _loop27_breakloop;
							}
							
						}
_loop27_breakloop:						;
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
								goto _loop29_breakloop;
							}
							
						}
_loop29_breakloop:						;
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
								goto _loop32_breakloop;
							}
							
						}
_loop32_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched33 = false;
			}
			rewind(_m33);
			inputState.guessing--;
		}
		if ( synPredMatched33 )
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
							goto _loop36_breakloop;
						}
						
					}
_loop36_breakloop:					;
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
							goto _loop38_breakloop;
						}
						
					}
_loop38_breakloop:					;
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
							goto _loop41_breakloop;
						}
						
					}
_loop41_breakloop:					;
				}    // ( ... )*
			}
			if (0==inputState.guessing)
			{
				
						_ttype = Token.SKIP;
					
			}
		}
		else {
			bool synPredMatched48 = false;
			if (((LA(1)=='\t'||LA(1)=='/') && (LA(2)=='\t'||LA(2)=='/')))
			{
				int _m48 = mark();
				synPredMatched48 = true;
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
									goto _loop44_breakloop;
								}
								
							}
_loop44_breakloop:							;
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
									goto _loop47_breakloop;
								}
								
							}
_loop47_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
					}
				}
				catch (RecognitionException)
				{
					synPredMatched48 = false;
				}
				rewind(_m48);
				inputState.guessing--;
			}
			if ( synPredMatched48 )
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
								goto _loop51_breakloop;
							}
							
						}
_loop51_breakloop:						;
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
								goto _loop54_breakloop;
							}
							
						}
_loop54_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
				}
				if (0==inputState.guessing)
				{
					
							_ttype = Token.SKIP;
						
				}
			}
			else if ((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (true)) {
				{
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
								goto _loop57_breakloop;
							}
							
						}
_loop57_breakloop:						;
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
								goto _loop59_breakloop;
							}
							
						}
_loop59_breakloop:						;
					}    // ( ... )*
				}
				if (0==inputState.guessing)
				{
					
							_ttype=MetaLexerTokenTypes.INDENTATION;
						
				}
			}
			else
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
		
	protected void mNEWLINE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = NEWLINE;
		
		{
			switch ( LA(1) )
			{
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
			case '\n':
			{
				int _saveIndex = 0;
				_saveIndex = text.Length;
				match('\n');
				text.Length = _saveIndex;
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
		data[0]=-2594292793769731585L;
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
		data[0]=-17179878401L;
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
