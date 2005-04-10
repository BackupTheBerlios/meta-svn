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
	k=1;
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
  ("\""! ( ~ ('\"') )* "\""!)
  |('\''! ( ~ (' '|'\t'|'\r'|'\n'|'='|'.'|'\''|'"'|'('|')'|'['|']'|':') )*)
  // 
 // |("@\""! ({LA(0)!='"'||LA(1)!='@'}? (.) )* "\"")=>
//  |("@\""! ( (.) )* LITERAL_END)
  | ("@\""! ({LA(1)!='"'||LA(2)!='@'}? (.) )* "\"@"!) //quite a mess, generates nondeterminism warning, works thanks to semantic predicates
//  | ("@\""! (options {greedy=false;} : (.) )* "\"@")
;
  
protected
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
  (NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?) =>
   NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?
  {
    _ttype=Token.SKIP;
  }
  |NEWLINE ('\t')*
  {
    _ttype=MetaLexerTokenTypes.INDENTATION;
  };

COMMENT
  options {
    paraphrase="a comment";
  }:
		"//"
		(~('\n'|'\r'))* ('\n'|'\r'('\n')?)
		{$setType(Token.SKIP); newline();}
	;
    
protected   
SPACE:  // subrule because of ANTLR bug that results in uncompilable code, maybe remove?
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
class MetaANTLRParser extends Parser;
options {
	buildAST = true;
	k=1;
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
//rename ENDLINE
map:
  {
    Counters.autokey.Push(0);
  }
	(
	  INDENT!
	  (statement)?
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
			      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST currentAst=new CommonAST(currentToken);
				    currentAst.setText("this");

				    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST autokeyAst=new CommonAST(autokeyToken);
				    autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([SELECT_KEY],currentAst,autokeyAst),#statement);
        }
      )
      |
      (
        expression
        {
            Counters.autokey.Push((int)Counters.autokey.Pop()+1);
			      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST currentAst=new CommonAST(currentToken);
				    currentAst.setText("this");

				    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST autokeyAst=new CommonAST(autokeyToken);
				    autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([SELECT_KEY],currentAst,autokeyAst),#statement);
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
  (
    lookup
    (
      POINT! lookup
    )*
    STAR
  )=>
  (
    lookup
    (
      POINT! lookup
    )*
    STAR!
    {
      Counters.autokey.Push((int)Counters.autokey.Pop()+1);
			Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
			CommonAST currentAst=new CommonAST(currentToken);
			currentAst.setText("search");

			//Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
			//CommonAST autokeyAst=new CommonAST(autokeyToken);
			//autokeyAst.setText(Counters.autokey.Peek().ToString());
      #select=#([SELECT_KEY],currentAst,#select);
      //#select=#([SELECT_KEY],#select);
    }
  )
  |
  (
    lookup
    (
      POINT! lookup
    )*
    {
      #select=#([SELECT_KEY],#select);
    }
  );

lookup:
  (
    LBRACKET!  
    (SPACES!)?
    (
      (call)=>call
      |map
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
    #literalKey=#([LITERAL,token.getText()]);
  };

{
  using Meta.Types;
  using Meta.Execution;
}      
class MetaTreeParser extends TreeParser;
options {
    defaultErrorHandler=false;
    //ASTLabelType = "CommonAST";
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
    Map statements=new Map();
    int counter=1;
  }:
  #(MAP
    (
      {
        Map key=null;
        Map val=null;
      }
      #(STATEMENT 
          key=select
          val=expression
        {
          Map statement=new Map();
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
  }:
  token:LITERAL
  {
    result[Literal.literalString]=new Map(token.getText());
  };


delayed
    returns[Map result]
    {
        result=new Map();
        Map delayed;
    }:
    #(FUNCTION delayed=expression)
    {
        result[Delayed.delayedString]=delayed;
    };
