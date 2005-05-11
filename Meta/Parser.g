//	An implementation of the Meta programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This library is free software; you can redistribute it and/or
//	modify it under the terms of the GNU Lesser General Public
//	License as published by the Free Software Foundation; either
//	version 2.1 of the License, or (at your option) any later version.
//
//	This library is distributed in the hope that it will be useful,s
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//	Lesser General Public License for more details.
//
//	You should have received a copy of the GNU Lesser General Public
//	License along with this library; if not, write to the Free Software
//	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

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
COLON
  options {
    paraphrase="':'";
  }:
  ':';
  
EQUAL
  options {
    paraphrase="'='";
  }:
  '=';

LBRACKET
  options {
    paraphrase="'['";
  }:
  '[';

RBRACKET
  options {
    paraphrase="']'";
  }:
  ']';

LPAREN
  options {
    paraphrase="'('";
  }:
  '(';

RPAREN
  options {
    paraphrase="')'";
  }:
  ')';

POINT
  options {
    paraphrase="'.'";
  }:
  '.';
  
STAR
  options {
    paraphrase="'*'";
  }:
  '*';

// fix the exact characters allowed
// rename to LOOKUP_LITERAL
LITERAL_KEY
  options {
    paraphrase="a key";
  }:
  ( ~ ('@'|' '|'\t'|'\r'|'\n'|'='|'.'|'/'|'\''|'"'|'('|')'|'['|']'|'*'|':') )+;
    
LITERAL
  options {
    paraphrase="a literal";
  }:
  ('\''! ( ~ (' '|'\t'|'\r'|'\n'|'='|'.'|'\''|'"'|'('|')'|'['|']'|':') )*)
  |("\""! ( ~ ('\"') )* "\""!)
  |(
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

SPACES
  options {
    paraphrase="whitespace";
  }:
  (' ')+ ;//{_ttype=Token.SKIP;}
  
  
LINE
  options {
    paraphrase="a line";
  }
  {
    const int endOfFileValue=65535;
  }:
  (NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?) => // TODO: Get rid of endOfFile, or remove the one newline?
   NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?
  {
    _ttype=Token.SKIP;
  }
  |(('\t')* NEWLINE ('\t')* NEWLINE)=>
  ('\t')* NEWLINE ('\t')*
	{
		_ttype=Token.SKIP;
	}

	|(('\t')* NEWLINE ('\t')* "//" (~('\n'|'\r'))* NEWLINE)=>
	('\t')* NEWLINE ('\t')* "//" (~('\n'|'\r'))*
	{$setType(Token.SKIP); newline();}
												 // TODO: factor out common stuff
	|((('\t')*)! "//" (~('\n'|'\r'))* NEWLINE)=> // TODO: ! is unnecessary
	(('\t')*)! "//" (~('\n'|'\r'))*
	{$setType(Token.SKIP);}
	
	|('\t'!)* NEWLINE ('\t')*
	{
		_ttype=MetaLexerTokenTypes.INDENTATION;
	}; 

 
protected   
SPACE:  // subrule because of ANTLR bug that results in uncompilable code, maybe remove? TODO: remove
  '\t'!;

protected
NEWLINE
  options {
    paraphrase="a newline";
  }:
  (
    '\n'!
    |('\r'! '\n'!)
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
    (call)=>call
    |select
    |map
    |delayed
    |LITERAL
  );
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
	
statement:
    (select COLON)=>
    (
      select
      COLON! 
      expression
      {
        #statement=#([STATEMENT],#statement);
      }
    )
    |
    (
      (COLON)=>
      (
        COLON!
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
            #statement=#([STATEMENT],#([SELECT_KEY],autokeyAst),#statement);
        }
      )
      |
      (
        expression
        {
            Counters.autokey.Push((int)Counters.autokey.Pop()+1);

				    MetaToken autokeyToken=new MetaToken(MetaLexerTokenTypes.LITERAL);
				    				    autokeyToken.setLine(#statement.Extent.startLine); // TODO: Not sure this is the best way to do it, or if it's even correct
				    autokeyToken.setColumn(#statement.Extent.startColumn);
				    autokeyToken.FileName=#statement.Extent.fileName;
				    autokeyToken.EndLine=#statement.Extent.endLine;
				    autokeyToken.EndColumn=#statement.Extent.endColumn;
				    MetaAST autokeyAst=new MetaAST(autokeyToken);
				    autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([SELECT_KEY],autokeyAst),#statement);
        }
      )
     
    )
    (SPACES!)?
    ;
call:
  (
    select
  )
  (SPACES!)?
  (
    call
    |map
    |LITERAL
  )
  {
    #call=#([CALL],#call);
  };  
	
delayed:
  EQUAL!
  expression
  {
    #delayed=#([FUNCTION], #delayed);
  };

select:
	subselect
	{
		#select=#([SELECT_KEY],#select);
	};
subselect:
	(lookup	POINT!)=>
	lookup POINT! subselect
	|lookup
	;
/*select:
	lookup 
	(POINT! lookup)* 
	(POINT! (lookup|map))
	{
		#select=#([SELECT_KEY],#select);
	};*/
/*select:
	(lookup POINT!)=>
	(
		lookup POINT!
		(
			 lookup POINT!
		)*
		(
			map
			|lookup
		)
	)
	|
	(lookup)
	{
		#select=#([SELECT_KEY],#select);
	};*/

lookup:
  (
    LBRACKET!  
    (SPACES!)?
    (
      map
      |LITERAL
      |delayed
      |select
    )
    (SPACES!)?
    (ENDLINE!)?
    RBRACKET!
  )
  |
  literalKey;

literalKey:
  token:LITERAL_KEY
  {
	token_AST.setType(LITERAL); // ugly hack
    //#literalKey=#([LITERAL,token.getText()]);
  };

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
    result=null;
  }:
  (
    result=call
    |result=map
    |result=select
    |result=literal
    |result=delayed
  );
map
  returns[Map result]
  {
    result=new Map();//map_AST_in.getLineNumber());
    result.Extent=#map.Extent;
    Map statements=new Map();
    int counter=1;
  }:
  #(MAP
    (
      {
        Map key=null;
        Map val=null;
      }
      #(STATEMENT		// TODO: make statement ist own subrule????
          key=select
          val=expression
        {
          Map statement=new Map();
							// TODO: Add Extent to statements, too?
					statement[Statement.keyString]=key;
					statement[Statement.valueString]=val;
					statements[new Integer(counter)]=statement;
					counter++;
				}
      )
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
  #(SELECT_KEY
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
        Map delayed;
    }:
    #(FUNCTION delayed=expression)
    {
        result[Delayed.delayedString]=delayed;
    };
