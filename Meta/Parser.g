//	An implementation of the Meta programming language.
//	Copyright (C) 2004 Christian Staudenmeyer <christianstaudenmeyer@web.de>
//
//	This library is free software; you can redistribute it and/or
//	modify it under the terms of the GNU Lesser General Public
//	License as published by the Free Software Foundation; either
//	version 2.1 of the License, or (at your option) any later version.
//
//	This library is distributed in the hope that it will be useful,
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
	charVocabulary='\u0003'..'\u0008'|'\u0010'..'\ufffe';
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
MINUS:
  '-';

EQUAL:
  '=';

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

COMMA:
  ',';

LITERAL_KEY:
  ( ~ (' '|'\r'|'\n'|'*'|'='|'-'|'.'|','|'\''|'('|')'|'['|']') )+;
    
LITERAL:
  "\'"! ( ~ ('\'') )* "\'"!;

SPACES:
  (' ')+ {_ttype=Token.SKIP;};

LINE
  {
    const int endOfFileValue=65535;
  }:
  (NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?) =>
   NEWLINE (SPACE)* (NEWLINE)* {LA(1)==endOfFileValue}?
  {
    _ttype=Token.SKIP;
  }
  |NEWLINE (' ')*
  {
    _ttype=MetaLexerTokenTypes.INDENTATION;
  };
    
protected   
SPACE:  // subrule because of ANTLR bug that results in uncompilable code
  ' '!;

protected
NEWLINE:
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
    (   
      LPAREN! 
      ( 
        statement
        (
          COMMA!
          statement
        )*
      )? 
      RPAREN! 
    )
	  |
	  (
	    INDENT!
	    statement
	    (
	      ENDLINE!
	      statement
	    )*
	    DEDENT!
	  )
	)
	{
	  Counters.autokey.Pop();
	  #map=#([MAP], #map);
	};
	
statement:
    (select EQUAL)=>
    (
      select
      EQUAL! 
      expression
      {
        #statement=#([STATEMENT],#statement);
      }
    )
    |
    (
      (EQUAL)=>
      (
        EQUAL!
        expression
        {
            //Counters.counter++;
            Counters.autokey.Push((int)Counters.autokey.Pop()+1);
			      Token currentToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST currentAst=new CommonAST(currentToken);
				    currentAst.setText("result");

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
				    currentAst.setText("result");

				    Token autokeyToken=new Token(MetaLexerTokenTypes.LITERAL);
				    CommonAST autokeyAst=new CommonAST(autokeyToken);
				    autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([SELECT_KEY],currentAst,autokeyAst),#statement);
        }
      )
    );
  
call:
  (
    select
    |map
    |LITERAL
  )
  (
    select
    |map
    |delayed
    |LITERAL
  )
  {
    #call=#([CALL],#call);
  };
	
delayed:
  MINUS!
  expression
  {
    #delayed=#([FUNCTION], #delayed);
  };
  
select:
  lookup
  (
    POINT! lookup
  )*
  {
    #select=#([SELECT_KEY],#select);
  };  

lookup:
  (
    LBRACKET!  
    (
      (call)=>call
      |map|LITERAL
      |delayed
      |select
    )
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
    result=new Map();
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
					statement["key"]=key;
					statement["value"]=val;
					statements[new Integer(counter)]=statement;
					counter++;
				}
      )
    )*
  )
  {
    result["program"]=statements;
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
      call["function"]=delayed;
      call["argument"]=argument;
      result["call"]=call;
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
    result["select"]=selection;
  };

 
literal
  returns [Map result]
  {
    result=new Map();
  }:
  token:LITERAL
  {
    result["literal"]=token.getText();
  };


delayed
    returns[Map result]
    {
        result=new Map();
        Map delayed;
    }:
    #(FUNCTION delayed=expression)
    {
        result["delayed"]=delayed;
    };
