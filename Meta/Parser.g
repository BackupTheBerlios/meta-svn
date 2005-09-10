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
}
tokens
{
  // imaginary tokens created by the Lexer:
  INDENTATION;
  SPACE;
  // imaginary tokens created by the IndentParser:        
  INDENT;
  ENDLINE;// TODO: rename to  NEWLINE, or something more abstract, maybe NEXT_STATEMENT, or STATEMENT_END
  DEDENT; // TODO: maybe rename too?
  // imaginary tokens created by the Parser:              
  PROGRAM;
  FUNCTION; 
  STATEMENT; 
  CALL;
  SELECT;
  KEY;
  SAME_INDENT;
}
{
	public static bool LiteralEnd(MetaLexer lexer)
	{
		bool LiteralEnd=true;
		for(int i=0;i<literalEnd.Length;i++)
		{
			if(lexer.LA(i+1)!=literalEnd[i])
			{
				LiteralEnd=false;
				break;
			}
		}
		return LiteralEnd;
	}
	public static void SetLiteralEnd(string literalStart)
	{
		literalEnd=Helper.ReverseString(literalStart);
	}
	private static string literalEnd;
	
	// add extent information to tokens
    protected override Token makeToken (int t)
    {
        MetaToken tok = (MetaToken) base.makeToken (t);
        ((SourceAreaLexerSharedInputState) inputState).annotate (tok);
        return tok;
    }
    
    // override default tab handling
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
			SetLiteralEnd(text.ToString());
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
	)
;

// separate rule only because of code generation bug
protected
LITERAL_VERY_END:
		'\"'!;
		
LITERAL:
	(
		LITERAL_START
		(
			options{greedy=true;}:
			{!LiteralEnd(this)}?
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
protected
COMMENT:
	"//";

protected
REST_OF_LINE:
	(
		~(
			'\n'
			|'\r'
		)
	)*
;

LINE		// everything in one rule to avoid indeterminisms
  {
    const int endOfFileValue=65535;
  }:
	(
		(NEWLINE|BOF)
		WHITESPACE
		COMMENT
		REST_OF_LINE
		(NEWLINE|EOF)  
	)=>
	(
		(NEWLINE|BOF)
		WHITESPACE
		COMMENT
		REST_OF_LINE
	)
	{
		$setType(Token.SKIP);
	}
	|
	(
		NEWLINE
		WHITESPACE
		(NEWLINE|EOF)
	)=>
	(
		NEWLINE
		WHITESPACE
	)
	{
		$setType(Token.SKIP);
	}
	|
	(
		NEWLINE // TODO: maybe BOF is sufficient
	)=>
	( 
		NEWLINE
		('\t')*
	)
	{
		_ttype=MetaLexerTokenTypes.INDENTATION;
	}
	|
	(' ')=>
	(' ')
	{
		$setType(MetaLexerTokenTypes.SPACE);
	}
	|
	(
		(
			'\t'!
			|' '!
		)+
	)
	{
		$setType(Token.SKIP);
	}
;

protected
WHITESPACE:
	('\t'|' ')*;

protected
BOF: // TODO: rename
	{this.getLine()==1 && this.getColumn()==1}?;

protected 
EOF{ //TODO:rename
    const int endOfFile=65535;
  }:
  {LA(1)==endOfFile}?;

protected
NEWLINE  
	{
    const int endOfFile=65535;
  }:
  (
		(
			'\n'!
			|('\r'! '\n'!)
		)
		{
			newline();
		}
	)

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

using System.Collections;

}
class MetaParser extends Parser;
options {
	buildAST = true;
	k=1;
	ASTLabelType = "MetaAST";
	defaultErrorHandler=false;
}
{
    private static Stack autokeys=new Stack();
}

expression:
	(
		(call)=>call
		|select
		|function
		|map
		|LITERAL
	)
;

call:
	(
		select
	)
	// TODO: only allow one space
	(SPACE!)?
	(
		expression
	)
	{
		#call=#([CALL],#call);
	}
;
	
select:
	lookup
	(
		POINT! 
		lookup
	)*
	{
		#select=#([SELECT],#select);
	}
;

function:
	EQUAL!
	delayedImplementation
	{
		#function=#([PROGRAM],#function);
	}
;

// TODO: refactor, rename
delayedImplementation:
	expression
	{
  
		// TODO: refactor
		MetaToken runToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
		
		runToken.setLine(#delayedImplementation.SourceArea.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
		runToken.setColumn(#delayedImplementation.SourceArea.Start.Column); 
		runToken.FileName=#delayedImplementation.SourceArea.FileName;
		runToken.EndLine=#delayedImplementation.SourceArea.End.Line;
		runToken.EndColumn=#delayedImplementation.SourceArea.End.Column;

		
		MetaAST runAst=new MetaAST(runToken);
		runAst.setText(CodeKeys.Run.String);//"run"); // could we get rid of this, maybe, run isn't used anywhere else anymore, also it's a bad keyword to use (far too common)
		#delayedImplementation=#([STATEMENT],#([KEY],runAst),#([FUNCTION], #delayedImplementation));
	}
;

// TODO: combine???	
lookup: 
	(
		normalLookup
		|literalLookup
	)
;

literalLookup:
	token:LITERAL_KEY
	{
		// $setType generates compile error here, set type explicitly
		token_AST.setType(LITERAL);
	}
;

normalLookup:
	LBRACKET!  
	(
		select
		|LITERAL
		|emptyMap
	)
	RBRACKET!
;


map:
	emptyMap
	|
	(
		{
			autokeys.Push(0);
		}
		INDENT!
		(
			statement
			|delayed
		)? // TODO: maybe put delayed into statement??? Would make sense, I think, since it's essentially the same
		(
			ENDLINE!
			(
				delayed
				|statement
			)
		)*
		DEDENT!
		{
			autokeys.Pop();
			#map=#([PROGRAM], #map);
		}
	)
;

emptyMap:
	STAR
	{
		#emptyMap=#([PROGRAM], #emptyMap);
	}
;

// TODO: refactor
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
            autokeys.Push((int)autokeys.Pop()+1); 

			// TODO: Simplify!!, use astFactory
			MetaToken autokeyToken=new MetaToken(MetaLexerTokenTypes.LITERAL); // TODO: Factor out with below
			autokeyToken.setLine(#statement.SourceArea.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
			autokeyToken.setColumn(#statement.SourceArea.Start.Column); 
			autokeyToken.FileName=#statement.SourceArea.FileName;
			autokeyToken.EndLine=#statement.SourceArea.End.Line;
			autokeyToken.EndColumn=#statement.SourceArea.End.Column;
			MetaAST autokeyAst=new MetaAST(autokeyToken);
			autokeyAst.setText(autokeys.Peek().ToString());
            #statement=#([STATEMENT],#([KEY],autokeyAst),#statement);
        }
    )
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

delayed:
	APOSTROPHE!
	delayedImplementation
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
options 
{
    defaultErrorHandler=false;
    ASTLabelType = "MetaAST";
}

expression
	returns[Map code]:
	(
		code=call
		|code=program
		|code=select
		//|code=search
		|code=literal
		|code=delayed
	)
;

statementKeys
	returns[Map code]
	{
		code=new NormalMap();
		Map keyCode;
		int keyNumber=1;
	}:
	#(KEY
		(
			keyCode=expression
			{
				code[keyNumber]=keyCode;
				keyNumber++;
			}
		)+
	)
;

statement
	returns[Map code]
	{
		code=new NormalMap();
		Map keyCode;
		Map valueCode;
	}:
	#(STATEMENT
		keyCode=statementKeys
		valueCode=expression
		{
			code[CodeKeys.Key]=keyCode;
			code[CodeKeys.Value]=valueCode;
		}
	)
;

program
	returns[Map code]
	{
		code=new NormalMap();
		code.SourceArea=#program.SourceArea;
		Map programCode=new NormalMap();
		Map statementCode;
		int statementNumber=1;
	}:
	#(PROGRAM
		(
			(statementCode=statement)
			{
				programCode[statementNumber]=statementCode;
				statementNumber++;
			}
		)*
	)
	{
		code[CodeKeys.Program]=programCode;
	}
;

call
	returns [Map code]
	{
		code=new NormalMap();
		code.SourceArea=#call.SourceArea;
		Map callCode=new NormalMap();
		Map functionCode;
		Map argumentCode;
	}:
	#(CALL
		(
			functionCode=expression
		)
		(
			argumentCode=expression
		)
		{
			callCode[CodeKeys.Function]=functionCode;
			callCode[CodeKeys.Argument]=argumentCode;
			code[CodeKeys.Call]=callCode;
		}
	)
;

select
	returns [Map code]
	{
		code=new NormalMap();
		code.SourceArea=#select.SourceArea;
		Map selectCode=new NormalMap();
		Map keyCode;
		int counter=1;
	}: 
	#(SELECT
		(
			keyCode=expression
			{
				selectCode[counter]=keyCode;
				counter++;
			}
		)+
	)
	{
		code[CodeKeys.Select]=selectCode;
	}
;

/*search
	returns [Map code]
	{
		code=new NormalMap();
		code.SourceArea=#search.SourceArea;
		Map searchCode;
	}:
	#(SEARCH searchCode=expression)
	{
		code[CodeKeys.Search]=searchCode;
	}
;*/

delayed
    returns[Map code]
    {
        code=new NormalMap();
        code.SourceArea=#delayed.SourceArea;
        Map delayedCode;
    }:
    #(FUNCTION delayedCode=expression)
    {
        code[CodeKeys.Delayed]=delayedCode;
    }
;
 
literal
	returns [Map code]
	{
		code=new NormalMap();
		code.SourceArea=#literal.SourceArea;
	}:
	token:LITERAL
	{
		code[CodeKeys.Literal]=new NormalMap(token.getText());
	}
;
