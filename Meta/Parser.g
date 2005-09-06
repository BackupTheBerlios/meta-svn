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

options
{
	language = "CSharp";
	namespace = "Meta.Parser";
}
{
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
  // imaginary token created by the Lexer:
  INDENTATION;
  // imaginary tokens created by the IndentParser:        
  INDENT;                 
  ENDLINE;            
  DEDENT;
  // imaginary tokens created by the Parser:              
  PROGRAM;
  FUNCTION; 
  STATEMENT; 
  CALL;
  SELECT;
  SEARCH;
  KEY;
  SAME_INDENT;
}
{
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
}

EQUAL:
	'=';

APOSTROPHE:
	'\'';

COLON:
  ':';
  
STAR:
	'*';

LBRACKET:
	'[';

RBRACKET:
	']';

POINT:
	'.';

LITERAL_KEY:
	( 
		~(
			' '
			|'\t'
			|'\r'
			|'\n'
			|'='
			|'.'
			|'/'
			|'\''
			|'"'
			|'['
			|']'
			|'*'
			|':'
		)
	)+
;
protected
LITERAL_START:
	(
		"\""
		(
			("@")=>
			(
				"@"
				(options{greedy=true;}:
					"\"@"
				)*
				(
					("\"")=>
					("\"")
					|
					"" // optional
				)
			)
			|
			"" // optional
		)
		{
			Counters.LastLiteralStart=text.ToString();
			$setText("");
		}
	)
;
protected
LITERAL_END:
	(
		(
			(
				("@")=> // optional
				("@"!)
				|
				"" 
			)
			(options{greedy=true;}:
				"\"@"!
			)*
		)
		LITERAL_VERY_END
	);
// separate rule because of code generation bug
protected
LITERAL_VERY_END:
		'\"'!;
		
LITERAL:
	(
		LITERAL_START
		(options{greedy=true;}:
			{!Counters.IsLiteralEnd(this)}?
			(
				(~
					(
						'\n'
						|'\r'
					)
				)
				|NEWLINE_KEEP_TEXT
			)
		)*
		LITERAL_END
	)
;

LINE		// everything in one rule to avoid indeterminisms
  {
    const int endOfFileValue=65535;
  }:
	// comments
	(
		('\t')* NEWLINE ('\t')* "//" (~('\n'|'\r'))* NEWLINE  // TODO: get away from the NEWLINE stuff here, then rename newline to SAME_INDENT
	)=>
	(
		('\t')*
		NEWLINE
		('\t')*
		"//"
		(
			~(
				'\n'
				|'\r'
			)
		)*
	)
	{
		$setType(Token.SKIP);
	}
		
	// comments										 
	|
	(
		('\t')*
		"//"
		(
			~(
				'\n'
				|'\r'
			)
		)*
		NEWLINE
	)=>
	(
		('\t')*
		"//"
		(
			~(
				'\n'
				|'\r'
			)
		)*		// TODO: factor out common stuff
		NEWLINE
		//
	)
	{
		$setType(Token.SKIP);
	}
	|(
		('\t'!|' '!)* 
		NEWLINE
		('\t'!|' '!)* 
		NEWLINE
	)=>
	(
		('\t'!|' '!)* 
		NEWLINE
		('\t'!|' '!)* 
	)
	{
		$setType(Token.SKIP);
	}
	// indentation
	|
	(('\t'!|' '!)*
		NEWLINE)=>
	(
		('\t'!|' '!)* // throw away tabs and spaces at the end of old line
		NEWLINE
		('\t')* // keep tabs at the beginning of the new line
	)
	{
		_ttype=MetaLexerTokenTypes.INDENTATION;
	}
	|
	//((' '|'\t')+)=>
	(' '|'\t')+
	{
		$setType(MetaLexerTokenTypes.SPACES);
	}
; 

protected
NEWLINE:
  (
    ('\r'! '\n'!)
    |'\n'!
  )
  {
    newline();
  }
;

protected
NEWLINE_KEEP_TEXT:
  (
    ('\r' '\n')
    |'\n'
  )
  {
    newline();
  }
;

{
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
    |(select)=>select
    |emptyMap
    |LITERAL
    |map
	|fullDelayed
	|search
  );
emptyMap:
	STAR
	{
	  #emptyMap=#([PROGRAM], #emptyMap);
	};

map:
	{
		Counters.autokey.Push(0);
	}
	INDENT!
	(statement|delayed)? // TODO: maybe put delayed into statement??? Would make sense, I think, since it's essentially the same
	(
	ENDLINE!
	(delayed|statement)
	)*
	DEDENT!
	{
	  Counters.autokey.Pop();
	  #map=#([PROGRAM], #map);
	}
;

key:
	lookup 
	(
		POINT! 
		lookup
	)*
	{
		#key=#([KEY],#key);
	}
;

statement:
    (key COLON)=>
    (
		key
		COLON!
		expression
		{
			#statement=#([STATEMENT],#statement);
		}
    )
    |
    (
        (COLON!)?
        expression
        {
            Counters.autokey.Push((int)Counters.autokey.Pop()+1); 

			// TODO: Simplify!!, use astFactory
			MetaToken autokeyToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
			autokeyToken.setLine(#statement.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
			autokeyToken.setColumn(#statement.Extent.Start.Column); 
			autokeyToken.FileName=#statement.Extent.FileName;
			autokeyToken.EndLine=#statement.Extent.End.Line;
			autokeyToken.EndColumn=#statement.Extent.End.Column;
			MetaAST autokeyAst=new MetaAST(autokeyToken);
			autokeyAst.setText(Counters.autokey.Peek().ToString());
            #statement=#([STATEMENT],#([KEY],autokeyAst),#statement);
        }
    )
;

call:
	(
		(select)=>
		select
		|search
	)
	(SPACES!)?
	(
		expression
	)
	{
		#call=#([CALL],#call);
	}
;

fullDelayed:
	EQUAL!
	delayedImplementation
	{
		#fullDelayed=#([PROGRAM],#fullDelayed);
	}
;

delayed:
  APOSTROPHE!
	delayedImplementation
	;

// TODO: rename to better reflect the new function
delayedImplementation:
  expression
  {
  
		// TODO: refactor
		MetaToken runToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
		
		runToken.setLine(#delayedImplementation.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
		runToken.setColumn(#delayedImplementation.Extent.Start.Column); 
		runToken.FileName=#delayedImplementation.Extent.FileName;
		runToken.EndLine=#delayedImplementation.Extent.End.Line;
		runToken.EndColumn=#delayedImplementation.Extent.End.Column;

		
		MetaAST runAst=new MetaAST(runToken);
		runAst.setText("run"); // could we get rid of this, maybe, run isn't used anywhere else anymore, also it's a bad keyword to use (far too common)
		#delayedImplementation=#([STATEMENT],#([KEY],runAst),#([FUNCTION], #delayedImplementation));
  }
;
	
select:
	search
	(
		POINT! 
		lookup
	)+
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
		normalLookup
		|literalLookup
	)
;

literalLookup:
	token:LITERAL_KEY
	{
		// $setType generates compile error here, so type must be set explicitly
		token_AST.setType(LITERAL);
	}
;

normalLookup:
	LBRACKET!  
	(
		(select)=>
		select
		|LITERAL
		|search
		|emptyMap
	)
	RBRACKET!
;

{
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
		|result=search
		|result=literal
		|result=delayed
	)
;
key
	returns[Map result]
	{
		int counter=1;
		result=new NormalMap();
		Map e=null;
	}:
	#(KEY
		(
			e=expression
			{
				result[counter]=e;
				counter++;
			}
		)+
	)
;

statement
	returns[Map statement]
	{
		statement=new NormalMap();
		Map valueCode=null;
		Map keyCode=null;
	}:
	#(STATEMENT
		keyCode=key
		valueCode=expression
		{
			statement[CodeKeys.Key]=keyCode;
			statement[CodeKeys.Value]=valueCode;
		}
	)
;

map
	returns[Map result]
	{
		result=new NormalMap();
		result.Extent=#map.Extent;
		Map statements=new NormalMap();
		Map s=null;
		int counter=1;
	}:
	#(PROGRAM
			(
				(s=statement)
				{
					statements[counter]=s;					
					counter++;
				}
			)*
	)
	{
		result[CodeKeys.Program]=statements;
	};

call
  returns [Map result]
  {
    result=new NormalMap();
    result.Extent=#call.Extent;
    Map call=new NormalMap();
    Map delayed=new NormalMap();
    Map argument=new NormalMap();
  }:
  #(CALL
    (
      delayed=expression
    )
    (
      argument=expression
    )
    {
      call[CodeKeys.Function]=delayed;
      call[CodeKeys.Argument]=argument;
      result[CodeKeys.Call]=call;
    }
  );

select
  returns [Map result]
  {
    result=new NormalMap();
    result.Extent=#select.Extent;
    Map selection=new NormalMap();
    Map key=null;
    int counter=1;
  }: 
  #(SELECT
    (
      (
        key=expression
        {
          selection[counter]=key;
          counter++;
        }
      )+
    )
  )
  {
    result[CodeKeys.Select]=selection;
  };


search
	returns [Map result]
	{
		result=new NormalMap();
		Map lookupResult=null;
		result.Extent=#search.Extent;
		Map e=null;
	}:
	#(SEARCH e=expression)
	{
		result[CodeKeys.Search]=e;
	}
	;
 
// TODO: somewhat unlogical that literal doesn't build a higher AST in the first place,
// if there was also a parser rule for Literal, then we could match an AST instead of a token here
literal
  returns [Map result]
  {
    result=new NormalMap();
    result.Extent=#literal.Extent;
  }:
  token:LITERAL
  {
    result[CodeKeys.Literal]=new NormalMap(token.getText());
  };

//TODO: is this even needed anymore?
delayed
    returns[Map result]
    {
        result=new NormalMap();
        result.Extent=#delayed.Extent;
        Map mExpression;
        //Map CodeKeys.Run=new NormalMap();
    }:
    #(FUNCTION mExpression=expression)
    {
				//CodeKeys.Run[CodeKeys.Run]=mExpression;
        result[CodeKeys.Delayed]=mExpression;
//				CodeKeys.Run[CodeKeys.Run]=mExpression;
//        result[CodeKeys.Delayed]=CodeKeys.Run;
    };

/*delayedExpressionOnly
    returns[Map result]
    {
        result=new NormalMap();
        Map mExpression=null;
    }:
    #(DELAYED_EXPRESSION_ONLY mExpression=expression)
    {
			result[CodeKeys.Delayed]=mExpression;
			//result.Extent=#delayedExpressionOnly.Extent;
    };*/
