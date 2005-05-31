//	Meta is a simple programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License
//	as published by the Free Software Foundation; either version 2
//	of the License, or (at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

options
{
	language = "CSharp";
	namespace = "Meta.Parser";
}
class MetaLexer extends Lexer;
options 
{
	k=2;
	charVocabulary='\u0000'..'\uFFFE';
	//charVocabulary='\u0003'..'\u0008'|'\u0010'..'\ufffe';
}
tokens
{
  // created by Lexer:
  INDENTATION;
  // created by IndentParser:        
  INDENT;                 
  ENDLINE;            
  DEDENT;
  // created by Parser:              
  MAP;          
  FUNCTION; 
  STATEMENT; 
  CALL;
  SELECT;
  SEARCH;
  KEY;
  DELAYED_EXPRESSION_ONLY; // TODO: rename
  //EMPTY_LINE; // TODO: reintroduce, as delimiter between two maps
}
{
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
}

EQUAL:
  '=';

HASH:
	'#';

COLON:
  ':';

LBRACKET:
  '[';

RBRACKET:
  ']';

LPAREN:
  '(';

RPAREN:
  ')';

POINT:
  '.';

// fix the exact characters allowed
// rename to LOOKUP_LITERAL
LITERAL_KEY:
  ( 
		~(
			'@'
			|' '
			|'\t'
			|'\r'
			|'\n'
			|'='
			|'.'
			|'/'
			|'\''
			|'"'
			|'('
			|')'
			|'['
			|']'
			|'*'
			|':'
			|'#'
		)
	)+
	;
    
LITERAL:
  (
		'\''! 
		(
			~(
				' '
				|'\t'
				|'\r'
				|'\n'
				|'='
				|'.'
				|'\''
				|'"'
				|'('
				|')'
				|'['
				|']'
				|':'
			) 
		)*
	)
  |
	(
		"\""! 
		(
			(~ 
				(
					'\"'
					|'\n'
					|'\r'
				)
			)
			|NEWLINE_KEEP_TEXT
		)*
		"\""!
	)
  |
  (// TODO: remove this, use variable number of """ instead, if anything
    "@\""!
		(
            options {
                greedy=false;
            }
        :   .
        )*
        "\"@"!
        )
   ;
  
protected // TODO: Remove
LITERAL_END:
  {LA(2)=='@'}? "\"@"!;

SPACES:
  (' ')+ ;//{_ttype=Token.SKIP;}
  
 
  
LINE		// everything in one rule because of indeterminisms
  {
    const int endOfFileValue=65535;
  }:
  /*(NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?) => // TODO: Get rid of endOfFile, or remove the one newline?
   NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}? // Hmmmmhh, not sure about this anymore
  {
    _ttype=Token.SKIP;
  }*/
  
  /*(('\t')* NEWLINE ('\t')* NEWLINE)=> // TODO: This should match both newlines, shouldn't it?
  ('\t')* NEWLINE ('\t')*
	{
		_ttype=MetaLexerTokenTypes.EMPTY_LINE;
	}*/

	// comments
	(('\t')* NEWLINE ('\t')* "//" (~('\n'|'\r'))* NEWLINE)=>
	('\t')* NEWLINE ('\t')* "//" (~('\n'|'\r'))*
	{$setType(Token.SKIP);}
		
	// comments										 
	|((('\t')*)! "//" (~('\n'|'\r'))* NEWLINE)=> // TODO: ! is unnecessary
	(('\t')*)! "//" (~('\n'|'\r'))*		// TODO: factor out common stuff
	{$setType(Token.SKIP);}
	
	
	// indentation
	|('\t'!)* NEWLINE ('\t')*
	{
		_ttype=MetaLexerTokenTypes.INDENTATION;
	}; 

 
protected   
SPACE:  // subrule because of ANTLR bug that results in uncompilable code, maybe remove? TODO: remove
  '\t'!;

protected
NEWLINE:
  (
    ('\r'! '\n'!)
    |'\n'!
  )
  {
    newline();
  };

protected
NEWLINE_KEEP_TEXT:
  (
    ('\r' '\n')
    |'\n'
  )
  {
    newline();
  };

{
  using antlr;
  using System.Collections;
  class Counters
  {
    public static Stack autokey=new Stack();
  }
}
class MetaParser extends Parser;
options {
	buildAST = true;
	k=1;
	ASTLabelType = "MetaAST";

  defaultErrorHandler=false;
}
expression:
  (
  	(callInParens (ENDLINE!)? POINT!)=> select
    |(call)=>call  // TODO: this sucks, is slow, and complicated
    |(select)=>
    select
    |map
    |delayedExpressionOnly
    |delayed
    |LITERAL
		|search
  );
  
//TODO: rename map to program, or something like that
map:
  {
    Counters.autokey.Push(0);
  }
	(
	  INDENT!
	  statement
	  (
	    ENDLINE!
	    statement
	  )*
	  DEDENT!
	)
	{
	  Counters.autokey.Pop();
	  #map=#([MAP], #map);
	};
key:
	lookup (POINT! lookup)*
	{
		#key=#([KEY],#key);
	}
	;
statement:
    (key EQUAL)=>
    (
      key
      EQUAL! 
      expression
      {
        #statement=#([STATEMENT],#statement);
      }
    )
    |
    (
			// TODO: remove one branch, should not be indeterminate
      (
        (EQUAL!)?
        expression
        {
            //Counters.counter++;
            Counters.autokey.Push((int)Counters.autokey.Pop()+1); 

					// TODO: Simplify!!, use astFactory
				    MetaToken autokeyToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
				    autokeyToken.setLine(#statement.Extent.startLine); // TODO: Not sure this is the best way to do it, or if it's even correct
				    autokeyToken.setColumn(#statement.Extent.startColumn); 
				    autokeyToken.FileName=#statement.Extent.fileName;
				    autokeyToken.EndLine=#statement.Extent.endLine;
				    autokeyToken.EndColumn=#statement.Extent.endColumn;
				    MetaAST autokeyAst=new MetaAST(autokeyToken);
				    autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([KEY],autokeyAst),#statement);
        }
      )
    )
    ;
call:
	(
		(LPAREN!)=>
		callInParens
		|normalCall
	);
callInParens:
		(
			LPAREN!
			(
				(select)=>
				select
				|(call expression)=>call
				|search
				|map
				|LITERAL
				|delayed// TODO: add map, literal, call
			)
			(SPACES!)?
			(ENDLINE!)? // TODO: Think about why this is necessary and whether we really want it to work that way
			(
				(call)=> // TODO: replace with use expression
				call
				|(select)=>select
				|(map
						(SPACES!)?
						(ENDLINE!)? // TODO: this is an ugly hack???
					)
				|search
				|LITERAL
				|delayed
				|delayedExpressionOnly
			)
			RPAREN!
		)
			
  {
    #callInParens=#([CALL],#callInParens);
  };
normalCall:
		(
			(
				(select)=>
				select
				|search // TODO: maybe add some more stuff like: map, literal, call, here, too, think about what is necessary for this to work and think about why to keep the separation between the two call versions
			)
			(SPACES!)?
			(
				expression
			)
		)
	
  {
    #normalCall=#([CALL],#normalCall);
  };
    
delayedExpressionOnly:
  HASH!
  expression
  {
    #delayedExpressionOnly=#([DELAYED_EXPRESSION_ONLY], #delayedExpressionOnly);
  };



delayed:
  COLON!
  expression
  {
    #delayed=#([FUNCTION], #delayed);
  };


	
select:
	(
		map
		|(callInParens)=>callInParens // TODO: is callInParens needed here?
		|search
	)
	(ENDLINE!)?
	(POINT! lookup)+
	{
		#select=#([SELECT],#select);
	}
	;

search:
	lookup
	{
		#search=#([SEARCH],#search);
	}
	;
	
lookup: 
	(
		squareBracketLookup
		|literalKey
	)
	;

// TODO: pull up subrule
literalKey:
  token:LITERAL_KEY
  {
		token_AST.setType(LITERAL); // ugly hack, shouldn't there be a standard way to do this?
    //#literalKey=#([LITERAL,token.getText()]);
  };

// TODO: pull up subrule
squareBracketLookup:
		LBRACKET!  
		(SPACES!)?
		(
			expression
		)
		(SPACES!)?
		(ENDLINE!)? // TODO: this is an ugly hack???, maybe not needed anymore, because implemented in call??
		RBRACKET!
	;

{
  using Meta.Types;
  using Meta.Execution;
}      
class MetaTreeParser extends TreeParser;
options {
    defaultErrorHandler=false;
    ASTLabelType = "MetaAST";
}
expression
  returns[Map result]
  {
    result=null;//new Map();
  }:
  (
    result=call
    |result=map
    |result=select
    |result=search
    |result=literal
    |result=delayed
    |result=delayedExpressionOnly
  )
  ;
key
	returns[Map result]
	{
		int counter=1;
		result=new Map();
		Map e=null;
	}:
	#(KEY
		(
			e=expression
			{
				result[new Integer(counter)]=e;
				counter++;
			}
		)+
	)
	;
statement
	returns[Map statement]
	{
		statement=new Map();
		//Map key=null;
		Map val=null;
		Map k=null;
	}:
	#(STATEMENT
		k=key
		val=expression
		{
			//Map statement=new Map();
			statement[Statement.keyString]=k;
			statement[Statement.valueString]=val;// TODO: Add Extent to statements, too?
		}
	)
	;
map
  returns[Map result]
  {
    result=new Map();
    result.Extent=#map.Extent;
    Map statements=new Map();
    Map s=null;
    int counter=1;
  }:
  #(MAP
    (
			s=statement
			{
				statements[new Integer(counter)]=s;					
				counter++;
			}
    )*
  )
  {
    result[Program.programString]=statements;
  };

call
  returns [Map result]
  {
    result=new Map();
    result.Extent=#call.Extent;
    Map call=new Map();
    Map delayed=new Map();
    Map argument=new Map();
  }:
  #(CALL
    (
      delayed=expression
    )
    (
      argument=expression
    )
    {
      call[Call.functionString]=delayed;
      call[Call.argumentString]=argument;
      result[Call.callString]=call;
    }
  );

select
  returns [Map result]
  {
    result=new Map();
    result.Extent=#select.Extent;
    Map selection=new Map();
    Map key=null;
    int counter=1;
  }: 
  #(SELECT
    (
      (
        key=expression
        {
          selection[new Integer(counter)]=key;
          counter++;
        }
      )+
    )
  )
  {
    result[Select.selectString]=selection;
  };


search
	returns [Map result]
	{
		result=new Map();
		Map lookupResult=null;
		result.Extent=#search.Extent;
		Map e=null;
	}:
	#(SEARCH e=expression)
	{
		result[Search.searchString]=e;
	}
	;
 
// TODO: somewhat unlogical that literal doesn't build a higher AST in the first place,
// if there was also a parser rule for Literal, then we could match an AST instead of a token here
literal
  returns [Map result]
  {
    result=new Map();
    result.Extent=#literal.Extent;
  }:
  token:LITERAL
  {
    result[Literal.literalString]=new Map(token.getText());
  };


delayed
    returns[Map result]
    {
        result=new Map();
        result.Extent=#delayed.Extent;
        Map mExpression;
        Map mRun=new Map();
    }:
    #(FUNCTION mExpression=expression)
    {
				mRun[Expression.runString]=mExpression;
        result[Delayed.delayedString]=mRun;
    };

delayedExpressionOnly
    returns[Map result]
    {
        result=new Map();
        Map mExpression=null;
    }:
    #(DELAYED_EXPRESSION_ONLY mExpression=expression)
    {
			result[Delayed.delayedString]=mExpression;
			//result.Extent=#delayedExpressionOnly.Extent;
    };
