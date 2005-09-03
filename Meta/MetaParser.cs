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

  using antlr;
  using System.Collections;
  class Counters
  {
	public static bool IsLiteralEnd(MetaLexer lexer)
	{
		bool matched=true;
		for(int i=0;i<Counters.NextLiteralEnd.Length;i++)
		{
			if(lexer.LA(i+1)!=Counters.NextLiteralEnd[i])
			{
				matched=false;
				break;
			}
		}
		return matched;
	}
	public static string LastLiteralStart
	{
		set
		{
			nextLiteralEnd=Helper.ReverseString(value);
		}
	}
	public static string NextLiteralEnd
	{
		get
		{
			return nextLiteralEnd;
		}
	}
	public static string nextLiteralEnd;
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
			case INDENT:
			{
				map();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
				break;
			}
			case EXCLAMATION_MARK:
			{
				fullDelayed();
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
				bool synPredMatched104 = false;
				if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
				{
					int _m104 = mark();
					synPredMatched104 = true;
					inputState.guessing++;
					try {
						{
							call();
						}
					}
					catch (RecognitionException)
					{
						synPredMatched104 = false;
					}
					rewind(_m104);
					inputState.guessing--;
				}
				if ( synPredMatched104 )
				{
					call();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
				}
				else {
					bool synPredMatched106 = false;
					if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
					{
						int _m106 = mark();
						synPredMatched106 = true;
						inputState.guessing++;
						try {
							{
								select();
							}
						}
						catch (RecognitionException)
						{
							synPredMatched106 = false;
						}
						rewind(_m106);
						inputState.guessing--;
					}
					if ( synPredMatched106 )
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
				}break; }
			}
			expression_AST = (MetaAST)currentAST.root;
			returnAST = expression_AST;
		}
		
	public void call() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST call_AST = null;
		
		{
			{
				bool synPredMatched130 = false;
				if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
				{
					int _m130 = mark();
					synPredMatched130 = true;
					inputState.guessing++;
					try {
						{
							select();
						}
					}
					catch (RecognitionException)
					{
						synPredMatched130 = false;
					}
					rewind(_m130);
					inputState.guessing--;
				}
				if ( synPredMatched130 )
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
				case EXCLAMATION_MARK:
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
				expression();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
			}
		}
		if (0==inputState.guessing)
		{
			call_AST = (MetaAST)currentAST.root;
			
			call_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(CALL)).add((AST)call_AST));
			
			currentAST.root = call_AST;
			if ( (null != call_AST) && (null != call_AST.getFirstChild()) )
				currentAST.child = call_AST.getFirstChild();
			else
				currentAST.child = call_AST;
			currentAST.advanceChildToEnd();
		}
		call_AST = (MetaAST)currentAST.root;
		returnAST = call_AST;
	}
	
	public void select() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST select_AST = null;
		
		{
			search();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(currentAST, (AST)returnAST);
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
		int _cnt140=0;
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
				if (_cnt140 >= 1) { goto _loop140_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
			}
			
			_cnt140++;
		}
_loop140_breakloop:		;
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
			{
				switch ( LA(1) )
				{
				case INDENT:
				case EQUAL:
				case EXCLAMATION_MARK:
				case LBRACKET:
				case LITERAL_KEY:
				case LITERAL:
				{
					statement();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
					break;
				}
				case HASH:
				{
					delayed();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(currentAST, (AST)returnAST);
					}
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
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==ENDLINE))
					{
						match(ENDLINE);
						{
							switch ( LA(1) )
							{
							case HASH:
							{
								delayed();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(currentAST, (AST)returnAST);
								}
								break;
							}
							case INDENT:
							case EQUAL:
							case EXCLAMATION_MARK:
							case LBRACKET:
							case LITERAL_KEY:
							case LITERAL:
							{
								statement();
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
					}
					else
					{
						goto _loop112_breakloop;
					}
					
				}
_loop112_breakloop:				;
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
	
	public void fullDelayed() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST fullDelayed_AST = null;
		
		match(EXCLAMATION_MARK);
		delayedImplementation();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			fullDelayed_AST = (MetaAST)currentAST.root;
			
					fullDelayed_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(MAP)).add((AST)fullDelayed_AST));
				
			currentAST.root = fullDelayed_AST;
			if ( (null != fullDelayed_AST) && (null != fullDelayed_AST.getFirstChild()) )
				currentAST.child = fullDelayed_AST.getFirstChild();
			else
				currentAST.child = fullDelayed_AST;
			currentAST.advanceChildToEnd();
		}
		fullDelayed_AST = (MetaAST)currentAST.root;
		returnAST = fullDelayed_AST;
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
		
		bool synPredMatched118 = false;
		if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
		{
			int _m118 = mark();
			synPredMatched118 = true;
			inputState.guessing++;
			try {
				{
					key();
					match(EQUAL);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched118 = false;
			}
			rewind(_m118);
			inputState.guessing--;
		}
		if ( synPredMatched118 )
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
		else {
			bool synPredMatched121 = false;
			if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
			{
				int _m121 = mark();
				synPredMatched121 = true;
				inputState.guessing++;
				try {
					{
						key();
						match(COLON);
					}
				}
				catch (RecognitionException)
				{
					synPredMatched121 = false;
				}
				rewind(_m121);
				inputState.guessing--;
			}
			if ( synPredMatched121 )
			{
				{
					key();
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
						statement_AST = (MetaAST)currentAST.root;
						
											statement_AST=(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(STATEMENT_SEARCH)).add((AST)statement_AST));
						
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
			else if ((tokenSet_0_.member(LA(1)))) {
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
							case EXCLAMATION_MARK:
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
											    autokeyToken.setLine(statement_AST.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
											    autokeyToken.setColumn(statement_AST.Extent.Start.Column); 
											    autokeyToken.FileName=statement_AST.Extent.FileName;
											    autokeyToken.EndLine=statement_AST.Extent.End.Line;
											    autokeyToken.EndColumn=statement_AST.Extent.End.Column;
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
			}
			returnAST = statement_AST;
		}
		
	public void delayed() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST delayed_AST = null;
		
		match(HASH);
		delayedImplementation();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		delayed_AST = (MetaAST)currentAST.root;
		returnAST = delayed_AST;
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
					goto _loop115_breakloop;
				}
				
			}
_loop115_breakloop:			;
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
	
	public void delayedImplementation() //throws RecognitionException, TokenStreamException
{
		
		returnAST = null;
		ASTPair currentAST = new ASTPair();
		MetaAST delayedImplementation_AST = null;
		
		expression();
		if (0 == inputState.guessing)
		{
			astFactory.addASTChild(currentAST, (AST)returnAST);
		}
		if (0==inputState.guessing)
		{
			delayedImplementation_AST = (MetaAST)currentAST.root;
			
			
					// TODO: Simplify this, factor this out into a method? Add some functionality for this stuff? Maybe to MetAST?
					MetaToken runToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
					
					runToken.setLine(delayedImplementation_AST.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
					runToken.setColumn(delayedImplementation_AST.Extent.Start.Column); 
					runToken.FileName=delayedImplementation_AST.Extent.FileName;
					runToken.EndLine=delayedImplementation_AST.Extent.End.Line;
					runToken.EndColumn=delayedImplementation_AST.Extent.End.Column;
					
					
					MetaAST runAst=new MetaAST(runToken);
					runAst.setText("run"); // could we get rid of this, maybe, run isn't used anywhere else anymore, also it's a bad keyword to use (far too common)
			//#statement=#([STATEMENT],#([KEY],autokeyAst),#statement);
					delayedImplementation_AST=(MetaAST)astFactory.make( (new ASTArray(3)).add((AST)(MetaAST) astFactory.create(STATEMENT)).add((AST)(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(KEY)).add((AST)runAst))).add((AST)(MetaAST)astFactory.make( (new ASTArray(2)).add((AST)(MetaAST) astFactory.create(FUNCTION)).add((AST)delayedImplementation_AST))));
			//#delayedImplementation=#([FUNCTION], #delayedImplementation);
			
			currentAST.root = delayedImplementation_AST;
			if ( (null != delayedImplementation_AST) && (null != delayedImplementation_AST.getFirstChild()) )
				currentAST.child = delayedImplementation_AST.getFirstChild();
			else
				currentAST.child = delayedImplementation_AST;
			currentAST.advanceChildToEnd();
		}
		delayedImplementation_AST = (MetaAST)currentAST.root;
		returnAST = delayedImplementation_AST;
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
			bool synPredMatched149 = false;
			if (((LA(1)==LBRACKET||LA(1)==LITERAL_KEY)))
			{
				int _m149 = mark();
				synPredMatched149 = true;
				inputState.guessing++;
				try {
					{
						select();
					}
				}
				catch (RecognitionException)
				{
					synPredMatched149 = false;
				}
				rewind(_m149);
				inputState.guessing--;
			}
			if ( synPredMatched149 )
			{
				select();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(currentAST, (AST)returnAST);
				}
			}
			else if ((LA(1)==LITERAL)) {
				MetaAST tmp16_AST = null;
				tmp16_AST = (MetaAST) astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, (AST)tmp16_AST);
				match(LITERAL);
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
		factory.setMaxNodeType(33);
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
		long[] data = { 287965216L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	
}
}
