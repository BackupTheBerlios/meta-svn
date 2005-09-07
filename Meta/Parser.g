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
  SPACES;
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
	public static bool IsLiteralEnd(MetaLexer lexer)
	{
		bool isLiteralEnd=true;
		for(int i=0;i<literalEnd.Length;i++)
		{
			if(lexer.LA(i+1)!=literalEnd[i])
			{
				isLiteralEnd=false;
				break;
			}
		}
		return isLiteralEnd;
	}
	public static void SetLiteralEnd(string literalStart)
	{
		literalEnd=Helper.ReverseString(literalStart);
	}
	public static string literalEnd;
	// add extent information to tokens
    protected override Token makeToken (int t)
    {
        MetaToken tok = (MetaToken) base.makeToken (t);
        ((ExtentLexerSharedInputState) inputState).annotate (tok);
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
	);
// separate rule because of code generation bug
protected
LITERAL_VERY_END:
		'\"'!;
		
LITERAL:
	(
		LITERAL_START
		(options{greedy=true;}:
			{!IsLiteralEnd(this)}?
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
		|(select)=>select
		|function
		|search
		|map
		|LITERAL
	)
;

call:
	(
		(select)=>
		select
		|search
	)
	// TODO: only allow one space
	(SPACES!)?
	(
		expression
	)
	{
		#call=#([CALL],#call);
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
		
		runToken.setLine(#delayedImplementation.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
		runToken.setColumn(#delayedImplementation.Extent.Start.Column); 
		runToken.FileName=#delayedImplementation.Extent.FileName;
		runToken.EndLine=#delayedImplementation.Extent.End.Line;
		runToken.EndColumn=#delayedImplementation.Extent.End.Column;

		
		MetaAST runAst=new MetaAST(runToken);
		runAst.setText(CodeKeys.Run.String);//"run"); // could we get rid of this, maybe, run isn't used anywhere else anymore, also it's a bad keyword to use (far too common)
		#delayedImplementation=#([STATEMENT],#([KEY],runAst),#([FUNCTION], #delayedImplementation));
	}
;

search:
	lookup
	{
		#search=#([SEARCH],#search);
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
		(select)=>
		select
		|LITERAL
		|search
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
			autokeyToken.setLine(#statement.Extent.Start.Line); // TODO: Not sure this is the best way to do it, or if it's even correct
			autokeyToken.setColumn(#statement.Extent.Start.Column); 
			autokeyToken.FileName=#statement.Extent.FileName;
			autokeyToken.EndLine=#statement.Extent.End.Line;
			autokeyToken.EndColumn=#statement.Extent.End.Column;
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
		|code=search
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
		code.Extent=#program.Extent;
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
		code.Extent=#call.Extent;
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
		code.Extent=#select.Extent;
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

search
	returns [Map code]
	{
		code=new NormalMap();
		code.Extent=#search.Extent;
		Map searchCode;
	}:
	#(SEARCH searchCode=expression)
	{
		code[CodeKeys.Search]=searchCode;
	}
;

delayed
    returns[Map code]
    {
        code=new NormalMap();
        code.Extent=#delayed.Extent;
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
		code.Extent=#literal.Extent;
	}:
	token:LITERAL
	{
		code[CodeKeys.Literal]=new NormalMap(token.getText());
	}
;