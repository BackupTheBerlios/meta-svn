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


using System;
using BaseAST = antlr.BaseAST;
using CommonToken = antlr.CommonToken;
using Token = antlr.Token;
using AST = antlr.collections.AST;
using antlr;
using Meta.Parser;


public class Extent { // TODO: Rename to something more sensible
	public int startLine;
	public int endLine;
	public int startColumn;
	public int endColumn;
	public string fileName; // TODO: A bit unlogical to make public, but needed for DetermineExtent
	public Extent(int startLine,int startColumn,int endLine,int endColumn,string fileName) {
		this.startLine=startLine;
		this.startColumn=startColumn;
		this.endLine=endLine;
		this.endColumn=endColumn;
		this.fileName=fileName;
	}

//	public int StartLine {
//		get {
//			return startLine;
//		}
//	}
//	public int EndLine {
//		get {
//			return endLine;
//		}
//	}
//	public int StartColumn {
//		get {
//			return startColumn;
//		}
//	}
//	public int EndColumn {
//		get {
//			return endColumn;
//		}
//	}
//	public string FileName {
//		get {
//			return this.fileName;
//		}
//	}
}
/// <summary> AST node implementation which stores tokens explicitly. This
/// is handy if you'd rather derive information from tokens on an
/// as-needed basis instead of snarfing data from a token as an AST
/// is being built.
/// 
/// <p>This file is in the public domain.</p>
/// 
/// </summary>
/// <author>  Dan Bornstein, danfuzz@milk.com
/// </author>
public class MetaAST:CommonAST
{
	/* Read the extent of this AST from the corresponding token. */
	public Extent Extent {
		get {
			/* Update the positions and return them. */
			/* TODO: This is a bit shaky, it doesn't seem to do this recursively, so
			there are usually a few lines missing from maps for example, might also be a bit slow
			 maybe move it to somewhere else*/
			//((MetaToken)this.token).setFromMetaAST(this); 
			if(extent==null) {
				extent=new Extent(System.Int32.MaxValue,System.Int32.MaxValue,
					System.Int32.MinValue,System.Int32.MinValue,null);
				DetermineExtent(extent,this);
			}
			return extent;
		}
	}
	// this is called AType so it is shown in the serialization first
	public string AType{
		get {
			return MetaParser.tokenNames_[this.Type];
			}
	}
	Extent extent;
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the token text for this instance. If there is no token associated
	/// with this instance, then this returns the empty string 
	/// (<code>""</code>), not <code>null</code>.
	/// 
	/// </summary>
	/// <returns> non-null; the token text
	/// </returns>
	/// <summary> Set the token text for this node. If this instance is already
	/// associated with a token, then that token is destructively modified
	/// by this operation. If not, then a new token is constructed with
	/// the type {@link Token#INVALID_TYPE} and the given text.
	/// 
	/// </summary>
	/// <param name="text">the new token text
	/// </param>
	public override System.String getText() // Chris: Attention, I changed the overriding, not sure
										// whether this will still work, but probably
		{
			return text;
//			if (token == null)
//			{
//				return "";
//			}
//			else
//			{
//				// return token.getText(); //setting text of token is impossible so save it in AST
//				return base.getText();
//			}
		}
	string text="";
		public override void setText(string val) // Chris: Attention, I changed the overriding, not sure
		{
			text=val;
//			if (token == null)
//			{
//				initialize(Token.INVALID_TYPE, val);
//			}
//			else
//			{
//				base.setText(val);
////				token.setText(val); // somehow Token.setText doesn't do anything
//			} 
		}
		
//	public System.String Text // Chris: Attention, I changed the overriding, not sure
//										// whether this will still work, but probably
//	{
//
//		get
//		{
//			if (token == null)
//			{
//				return "";
//			}
//			else
//			{
//				return token.getText();
//			}
//		}
//		
//		set
//		{
//			if (token == null)
//			{
//				initialize(Token.INVALID_TYPE, value);
//			}
//			else
//			{
//				token.setText(value);
//			}
//		}
//		
//	}
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the token type for this instance. If there is no token associated
	/// with this instance, then this returns {@link Token#INVALID_TYPE}.
	/// 
	/// </summary>
	/// <returns> the token type
	/// </returns>
	/// <summary> Set the token type for this node. If this instance is already
	/// associated with a token, then that token is destructively modified
	/// by this operation. If not, then a new token is constructed with
	/// the given type and an empty (<code>""</code>, not <code>null</code>)
	/// text string.
	/// 
	/// </summary>
	/// <param name="type">the new token type
	/// </param>
//	override public int Type
//	{
//		get
//		{
//			return type;
////			if (token == null)
////			{
////				return Token.INVALID_TYPE;
////			}
////			else
////			{
////				return token.Type;
////			}
//		}
//		
//		set
//		{
//			type=value;
////			if (token == null)
////			{
////				initialize(value, "");
////			}
////			else
////			{
////				token.setType(value);
////			}
//		}
//		
//	}
	int type=0;
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the token associated with this instance. If there is no token
	/// associated with this instance, then this returns <code>null</code>.
	/// 
	/// </summary>
	/// <returns> null-ok; the token associated with this instance or
	/// <code>mull</code> if there is no associated token
	/// </returns>
	/// <summary> Set the token associated with this instance.
	/// 
	/// </summary>
	/// <param name="tok">null-ok; the new token to associate with this instance
	/// </param>
//	virtual public Token Token
//	{
//		get
//		{
//			return token;
//		}
//		
//		set
//		{
//			token = value;
//		}
//		
//	}
//	/// <summary>the token associated with this instance </summary>
	
	// ------------------------------------------------------------------------
	// constructors
	
	/// <summary> Construct an instance which (at least initially) is not associated
	/// with a token.
	/// </summary>
	public MetaAST()
	{
		//token = null;
	}
	
	/// <summary> Construct an instance which is associated with the given token.
	/// 
	/// </summary>
	/// <param name="tok">null-ok; the token to associate this instance with
	/// </param>
	public MetaAST(Token tok)
	{
		initialize(tok);
	}
	
	// ------------------------------------------------------------------------
	// public instance methods
	
	/// <summary> Initialize this instance with the given token.
	/// 
	/// </summary>
	/// <param name="tok">null-ok; the token to associate with this instance
	/// </param>
	public override void  initialize(Token tok)
	{
		if(!(tok is MetaToken)) {
			int asdf=0;
		}
		Type=tok.Type;
		if(tok.getLine()!=0) {
			extent=new Extent(tok.getLine(),tok.getColumn(),((MetaToken)tok).EndLine,((MetaToken)tok).EndColumn,((MetaToken)tok).FileName);
		}
		else {
			int asdf=0;
		}
		this.setText(tok.getText());
	}
	
	/// <summary> Initialize this instance with the given token type and text.
	/// This will construct a new {@link CommonToken} with the given
	/// parameters and associate this instance with it.
	/// 
	/// </summary>
	/// <param name="type">the token type
	/// </param>
	/// <param name="text">null-ok; the token text
	/// </param>
//	public override void  initialize(int type, System.String text)
//	{
//		initialize(new CommonToken(type, text));
//	}
	public override void  initialize(int type, System.String text) {
		initialize(new MetaToken(type, text));
	}	
	/// <summary> Initialize this instance based on the given {@link AST}.
	/// If the given <code>AST</code> is in fact an instance of 
	/// <code>MetaAST</code>, then this instance will be initialized
	/// to point at the same token as the given one. If not, then this
	/// instance will be initialized with the same token type and text
	/// as the given one.
	/// 
	/// </summary>
	/// <param name="ast">non-null; the <code>AST</code> to base this instance on
	/// </param>
	public override void  initialize(AST ast)
	{
//		if (ast is MetaAST)
//		{
//			initialize(((MetaAST) ast).Token);
//		}
//		else
//		{
			initialize(ast.Type, ast.getText());
//		}
	}

	private static void DetermineExtent(Extent result,MetaAST ast) {
		//MetaToken tok = (MetaToken) ast.Token;
		
//		if (tok != null) {

		// TODO: refactor this shit
			int line = ast.Extent.startLine;
			if (line != 0) {
				int col = ast.Extent.startColumn;
				int minLine = result.startLine;
				int minCol = result.startColumn;
				if (line < minLine) {
					result.startLine=line;
					result.startColumn=col;
					result.fileName=ast.Extent.fileName;
				}
				else if ((line == minLine) && (col < minCol)) {
					result.startColumn=col;
					result.fileName = ast.Extent.fileName;
				}
			}
			
			int endLine = ast.Extent.endLine;
			if (endLine != 0) {
				int endCol = ast.Extent.endColumn;
				int maxLine = result.endLine;
				int maxCol = result.endColumn;
				if (endLine > maxLine) {
					result.endLine = endLine;
					result.endColumn = endCol;
				}
				else if ((endLine == maxLine) && (endCol > maxCol)) {
					result.endColumn = endCol;
				}
			}
//		}
		
		ast = (MetaAST) ast.getFirstChild();
		while (ast != null) {
			DetermineExtent(result, ast);
			ast = (MetaAST) ast.getNextSibling();
		}
	}
//	static private void  setFromMetaAST(MetaToken result, MetaAST ast)
//	{
//		MetaToken tok = (MetaToken) ast.Token;
//		
//		if (tok != null)
//		{
//			int line = tok.getLine();
//			if (line != 0)
//			{
//				int col = tok.getColumn();
//				int minLine = result.getLine();
//				int minCol = result.getColumn();
//				if (line < minLine)
//				{
//					result.setLine(line);
//					result.setColumn(col);
//					result.FileName = tok.FileName;
//				}
//				else if ((line == minLine) && (col < minCol))
//				{
//					result.setColumn(col);
//					result.FileName = tok.FileName;
//				}
//			}
//			
//			int endLine = tok.EndLine;
//			if (endLine != 0)
//			{
//				int endCol = tok.EndColumn;
//				int maxLine = result.EndLine;
//				int maxCol = result.EndColumn;
//				if (endLine > maxLine)
//				{
//					result.EndLine = endLine;
//					result.EndColumn = endCol;
//				}
//				else if ((endLine == maxLine) && (endCol > maxCol))
//				{
//					result.EndColumn = endCol;
//				}
//			}
//		}
//		
//		ast = (MetaAST) ast.getFirstChild();
//		while (ast != null)
//		{
//			setFromMetaAST(result, ast);
//			ast = (MetaAST) ast.getNextSibling();
//		}
//	}
}