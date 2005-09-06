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
		public const int PROGRAM = 8;
		public const int FUNCTION = 9;
		public const int STATEMENT = 10;
		public const int CALL = 11;
		public const int SELECT = 12;
		public const int SEARCH = 13;
		public const int KEY = 14;
		public const int SAME_INDENT = 15;
		public const int EQUAL = 16;
		public const int APOSTROPHE = 17;
		public const int COLON = 18;
		public const int STAR = 19;
		public const int LBRACKET = 20;
		public const int RBRACKET = 21;
		public const int POINT = 22;
		public const int LITERAL_KEY = 23;
		public const int LITERAL_START = 24;
		public const int LITERAL_END = 25;
		public const int LITERAL_VERY_END = 26;
		public const int LITERAL = 27;
		public const int LINE = 28;
		public const int NEWLINE = 29;
		public const int NEWLINE_KEEP_TEXT = 30;
		public const int SPACES = 31;
		
		
	// add information about location to tokens
    protected override Token makeToken (int t)
    {
        MetaToken tok = (MetaToken) base.makeToken (t);
        ((ExtentLexerSharedInputState) inputState).annotate (tok);
        return tok;
    }
    // count tab as on character
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
						case '\'':
						{
							mAPOSTROPHE(true);
							theRetToken = returnToken_;
							break;
						}
						case ':':
						{
							mCOLON(true);
							theRetToken = returnToken_;
							break;
						}
						case '*':
						{
							mSTAR(true);
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
						case '"':
						{
							mLITERAL(true);
							theRetToken = returnToken_;
							break;
						}
						case '\t':  case '\n':  case '\r':  case ' ':
						case '/':
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
	
	public void mAPOSTROPHE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = APOSTROPHE;
		
		match('\'');
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
		int _cnt11=0;
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
				if (_cnt11 >= 1) { goto _loop11_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
			}
			
			_cnt11++;
		}
_loop11_breakloop:		;
		}    // ( ... )+
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	protected void mLITERAL_START(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL_START;
		
		{
			match("\"");
			{
				bool synPredMatched16 = false;
				if (((LA(1)=='@') && ((LA(2) >= '\u0000' && LA(2) <= '\ufffe'))))
				{
					int _m16 = mark();
					synPredMatched16 = true;
					inputState.guessing++;
					try {
						{
							match("@");
						}
					}
					catch (RecognitionException)
					{
						synPredMatched16 = false;
					}
					rewind(_m16);
					inputState.guessing--;
				}
				if ( synPredMatched16 )
				{
					{
						match("@");
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)=='"') && (LA(2)=='@'))
								{
									match("\"@");
								}
								else
								{
									goto _loop19_breakloop;
								}
								
							}
_loop19_breakloop:							;
						}    // ( ... )*
						{
							bool synPredMatched22 = false;
							if (((LA(1)=='"') && ((LA(2) >= '\u0000' && LA(2) <= '\ufffe'))))
							{
								int _m22 = mark();
								synPredMatched22 = true;
								inputState.guessing++;
								try {
									{
										match("\"");
									}
								}
								catch (RecognitionException)
								{
									synPredMatched22 = false;
								}
								rewind(_m22);
								inputState.guessing--;
							}
							if ( synPredMatched22 )
							{
								{
									match("\"");
								}
							}
							else if (((LA(1) >= '\u0000' && LA(1) <= '\ufffe')) && (true)) {
								match("");
							}
							else
							{
								throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
							}
							
						}
					}
				}
				else if (((LA(1) >= '\u0000' && LA(1) <= '\ufffe')) && (true)) {
					match("");
				}
				else
				{
					throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
				}
				
			}
			if (0==inputState.guessing)
			{
				
							Counters.LastLiteralStart=text.ToString();
							text.Length = _begin; text.Append("");
						
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
		
		{
			{
				{
					switch ( LA(1) )
					{
					case '@':
					{
						{
							int _saveIndex = 0;
							_saveIndex = text.Length;
							match("@");
							text.Length = _saveIndex;
						}
						break;
					}
					case '"':
					{
						match("");
						break;
					}
					default:
					{
						throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
					}
					 }
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)=='"') && (LA(2)=='@'))
						{
							int _saveIndex = 0;
							_saveIndex = text.Length;
							match("\"@");
							text.Length = _saveIndex;
						}
						else
						{
							goto _loop32_breakloop;
						}
						
					}
_loop32_breakloop:					;
				}    // ( ... )*
			}
			mLITERAL_VERY_END(false);
		}
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}
	
	protected void mLITERAL_VERY_END(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LITERAL_VERY_END;
		
		int _saveIndex = 0;
		_saveIndex = text.Length;
		match('\"');
		text.Length = _saveIndex;
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
		
		{
			mLITERAL_START(false);
			{    // ( ... )*
				for (;;)
				{
					if ((((LA(1) >= '\u0000' && LA(1) <= '\ufffe')) && ((LA(2) >= '\u0000' && LA(2) <= '\ufffe')))&&(!Counters.IsLiteralEnd(this)))
					{
						{
							if ((tokenSet_1_.member(LA(1))))
							{
								{
									{
										match(tokenSet_1_);
									}
								}
							}
							else if ((LA(1)=='\n'||LA(1)=='\r')) {
								mNEWLINE_KEEP_TEXT(false);
							}
							else
							{
								throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());
							}
							
						}
					}
					else
					{
						goto _loop40_breakloop;
					}
					
				}
_loop40_breakloop:				;
			}    // ( ... )*
			mLITERAL_END(false);
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
	
	public void mLINE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; Token _token=null; int _begin=text.Length;
		_ttype = LINE;
		
		const int endOfFileValue=65535;
		
		
		bool synPredMatched50 = false;
		if (((LA(1)=='\t'||LA(1)=='\n'||LA(1)=='\r') && (tokenSet_2_.member(LA(2)))))
		{
			int _m50 = mark();
			synPredMatched50 = true;
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
_loop44_breakloop:						;
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
								goto _loop46_breakloop;
							}
							
						}
_loop46_breakloop:						;
					}    // ( ... )*
					match("//");
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
								goto _loop49_breakloop;
							}
							
						}
_loop49_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched50 = false;
			}
			rewind(_m50);
			inputState.guessing--;
		}
		if ( synPredMatched50 )
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
							goto _loop53_breakloop;
						}
						
					}
_loop53_breakloop:					;
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
							goto _loop55_breakloop;
						}
						
					}
_loop55_breakloop:					;
				}    // ( ... )*
				match("//");
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
							goto _loop58_breakloop;
						}
						
					}
_loop58_breakloop:					;
				}    // ( ... )*
			}
			if (0==inputState.guessing)
			{
				
						_ttype = Token.SKIP;
					
			}
		}
		else {
			bool synPredMatched65 = false;
			if (((LA(1)=='\t'||LA(1)=='/') && (LA(2)=='\t'||LA(2)=='/')))
			{
				int _m65 = mark();
				synPredMatched65 = true;
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
									goto _loop61_breakloop;
								}
								
							}
_loop61_breakloop:							;
						}    // ( ... )*
						match("//");
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
									goto _loop64_breakloop;
								}
								
							}
_loop64_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
					}
				}
				catch (RecognitionException)
				{
					synPredMatched65 = false;
				}
				rewind(_m65);
				inputState.guessing--;
			}
			if ( synPredMatched65 )
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
								goto _loop68_breakloop;
							}
							
						}
_loop68_breakloop:						;
					}    // ( ... )*
					match("//");
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
								goto _loop71_breakloop;
							}
							
						}
_loop71_breakloop:						;
					}    // ( ... )*
					mNEWLINE(false);
				}
				if (0==inputState.guessing)
				{
					
							_ttype = Token.SKIP;
						
				}
			}
			else {
				bool synPredMatched77 = false;
				if (((tokenSet_3_.member(LA(1))) && (true)))
				{
					int _m77 = mark();
					synPredMatched77 = true;
					inputState.guessing++;
					try {
						{
							{    // ( ... )*
								for (;;)
								{
									switch ( LA(1) )
									{
									case '\t':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match('\t');
										text.Length = _saveIndex;
										break;
									}
									case ' ':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match(' ');
										text.Length = _saveIndex;
										break;
									}
									default:
									{
										goto _loop74_breakloop;
									}
									 }
								}
_loop74_breakloop:								;
							}    // ( ... )*
							mNEWLINE(false);
							{    // ( ... )*
								for (;;)
								{
									switch ( LA(1) )
									{
									case '\t':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match('\t');
										text.Length = _saveIndex;
										break;
									}
									case ' ':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match(' ');
										text.Length = _saveIndex;
										break;
									}
									default:
									{
										goto _loop76_breakloop;
									}
									 }
								}
_loop76_breakloop:								;
							}    // ( ... )*
							mNEWLINE(false);
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
					{
						{    // ( ... )*
							for (;;)
							{
								switch ( LA(1) )
								{
								case '\t':
								{
									int _saveIndex = 0;
									_saveIndex = text.Length;
									match('\t');
									text.Length = _saveIndex;
									break;
								}
								case ' ':
								{
									int _saveIndex = 0;
									_saveIndex = text.Length;
									match(' ');
									text.Length = _saveIndex;
									break;
								}
								default:
								{
									goto _loop80_breakloop;
								}
								 }
							}
_loop80_breakloop:							;
						}    // ( ... )*
						mNEWLINE(false);
						{    // ( ... )*
							for (;;)
							{
								switch ( LA(1) )
								{
								case '\t':
								{
									int _saveIndex = 0;
									_saveIndex = text.Length;
									match('\t');
									text.Length = _saveIndex;
									break;
								}
								case ' ':
								{
									int _saveIndex = 0;
									_saveIndex = text.Length;
									match(' ');
									text.Length = _saveIndex;
									break;
								}
								default:
								{
									goto _loop82_breakloop;
								}
								 }
							}
_loop82_breakloop:							;
						}    // ( ... )*
					}
					if (0==inputState.guessing)
					{
						
								_ttype = Token.SKIP;
							
					}
				}
				else {
					bool synPredMatched86 = false;
					if (((tokenSet_3_.member(LA(1))) && (true)))
					{
						int _m86 = mark();
						synPredMatched86 = true;
						inputState.guessing++;
						try {
							{
								{    // ( ... )*
									for (;;)
									{
										switch ( LA(1) )
										{
										case '\t':
										{
											int _saveIndex = 0;
											_saveIndex = text.Length;
											match('\t');
											text.Length = _saveIndex;
											break;
										}
										case ' ':
										{
											int _saveIndex = 0;
											_saveIndex = text.Length;
											match(' ');
											text.Length = _saveIndex;
											break;
										}
										default:
										{
											goto _loop85_breakloop;
										}
										 }
									}
_loop85_breakloop:									;
								}    // ( ... )*
								mNEWLINE(false);
							}
						}
						catch (RecognitionException)
						{
							synPredMatched86 = false;
						}
						rewind(_m86);
						inputState.guessing--;
					}
					if ( synPredMatched86 )
					{
						{
							{    // ( ... )*
								for (;;)
								{
									switch ( LA(1) )
									{
									case '\t':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match('\t');
										text.Length = _saveIndex;
										break;
									}
									case ' ':
									{
										int _saveIndex = 0;
										_saveIndex = text.Length;
										match(' ');
										text.Length = _saveIndex;
										break;
									}
									default:
									{
										goto _loop89_breakloop;
									}
									 }
								}
_loop89_breakloop:								;
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
										goto _loop91_breakloop;
									}
									
								}
_loop91_breakloop:								;
							}    // ( ... )*
						}
						if (0==inputState.guessing)
						{
							
									_ttype=MetaLexerTokenTypes.INDENTATION;
								
						}
					}
					else if ((LA(1)=='\t'||LA(1)==' ') && (true)) {
						{ // ( ... )+
						int _cnt93=0;
						for (;;)
						{
							switch ( LA(1) )
							{
							case ' ':
							{
								match(' ');
								break;
							}
							case '\t':
							{
								match('\t');
								break;
							}
							default:
							{
								if (_cnt93 >= 1) { goto _loop93_breakloop; } else { throw new NoViableAltForCharException((char)LA(1), getFilename(), getLine(), getColumn());; }
							}
							break; }
							_cnt93++;
						}
_loop93_breakloop:						;
						}    // ( ... )+
						if (0==inputState.guessing)
						{
							
									_ttype = MetaLexerTokenTypes.SPACES;
								
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
	
	
	private static long[] mk_tokenSet_0_()
	{
		long[] data = new long[2048];
		data[0]=-2594289460875109889L;
		data[1]=-671088641L;
		for (int i = 2; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = new long[2048];
		data[0]=-9217L;
		for (int i = 1; i<=1022; i++) { data[i]=-1L; }
		data[1023]=9223372036854775807L;
		for (int i = 1024; i<=2047; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = new long[1025];
		data[0]=140737488365056L;
		for (int i = 1; i<=1024; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = new long[1025];
		data[0]=4294977024L;
		for (int i = 1; i<=1024; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	
}
}
