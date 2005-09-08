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
using CommonToken = antlr.CommonToken;

/// <summary> This is a token class which can keep track of full extent information.
/// That is, it knows about end positions as well as start positions and the
/// file (name) of origin of a token. Note that, conforming to standard Java
/// usage, the end column value is numerically one more than the actual end
/// column position, making (end column - start column) be the length of the
/// token, if it is on a single line, and allowing for the possibility of
/// 0-length tokens. If, for some reason, a start or end position hasn't
/// been calculated for an instance, it will return <code>0</code> from the
/// accessors in question. Since valid line and column nubmers start at
/// <code>1</code>, it is thus possible to distinguish these from all
/// legitimately set values. 
/// 
/// <p>This file is in the public domain.</p>
/// 
/// </summary>
/// <author>  Dan Bornstein, danfuzz@milk.com 
/// </author>
/// 

// TODO: Make MetaToken use SourceArea, too
public class MetaToken:CommonToken
{
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the end line of this instance.
	/// 
	/// </summary>
	/// <returns> the end line number
	/// </returns>
	/// <summary> Set the end line of this instance.
	/// 
	/// </summary>
	/// <param name="lineNum">the new end line number
	/// </param>
	virtual public int EndLine
	{
		get
		{
			return endLine;
		}
		
		set
		{
			endLine = value;
		}
		
	}
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the end column of this instance.
	/// 
	/// </summary>
	/// <returns> the end column number
	/// </returns>
	/// <summary> Set the end column of this instance.
	/// 
	/// </summary>
	/// <param name="colNum">the new end column number
	/// </param>
	virtual public int EndColumn
	{
		get
		{
			return endCol;
		}
		
		set
		{
			endCol = value;
		}
		
	}
	//UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions.
	/// <summary> Get the file name of this instance.
	/// 
	/// </summary>
	/// <returns> null-ok; the file name
	/// </returns>
	/// <summary> Set the file name of this instance.
	/// 
	/// </summary>
	/// <param name="name">null-ok; the file name
	/// </param>
	virtual public System.String FileName // TODO: use the standard stuff here, if possible , java-ify
	{
		get
		{
			return fileName;
		}
		
		set
		{
			fileName = value;
		}
		
	}
	/// <summary>the end line of the token </summary>
	protected internal int endLine;
	
	/// <summary>the end column of the token (+1, see discussion in the header) </summary>
	protected internal int endCol;
	
	/// <summary>null-ok; the file (name) of origin of the token </summary>
	protected internal System.String fileName;
	
	// ------------------------------------------------------------------------
	// constructors
	
	/// <summary> Construct an instance. The instance will be of type {@link
	/// #INVALID_TYPE}, have empty (<code>""</code>, not <code>null</code>)
	/// text, have <code>0</code> for all position values, and have a
	/// <code>null</code> file name.
	/// </summary>
	public MetaToken():this("")
	{
	}
	
	/// <summary> Construct an instance with the given text. The instance will be of
	/// type {@link #INVALID_TYPE}, have <code>0</code> for all position
	/// values, and have a <code>null</code> file name.
	/// 
	/// </summary>
	/// <param name="text">null-ok; the token text 
	/// </param>
	public MetaToken(System.String text):this(INVALID_TYPE, text)
	{
	}

	public MetaToken(int type):this(type,"") {
	}
	
	/// <summary> Construct an instance with the given type and text. The instance
	/// will have <code>0</code> for all position values and have a
	/// <code>null</code> file name.
	/// 
	/// </summary>
	/// <param name="type">the token type
	/// </param>
	/// <param name="text">null-ok; the token text 
	/// </param>
	public MetaToken(int type, System.String text):base(type, text)
	{
		line = 0;
		col = 0;
		endLine = 0;
		endCol = 0;
		fileName = null;
	}
	
	// ------------------------------------------------------------------------
	// public instance methods
	
	/// <summary> Return the extent of this token as a formatted string. The form
	/// is identical to the form used in the front of Caml error messages
	/// (which the emacs error parser understands):
	/// 
	/// <blockquote><code>File "<i>file-name</i>", line<i>[</i>s<i>]</i>
	/// <i>start[</i>-<i>end]</i>, character<i>[</i>s<i>]</i> 
	/// <i>start[</i>-<i>end]</i></code></blockquote>
	/// 
	/// </summary>
	/// <returns> the extent string for this instance
	/// </returns>
	public virtual System.String extentString()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder(100);
		sb.Append("File ");
		if ((System.Object) fileName != null)
		{
			sb.Append('\"');
			sb.Append(fileName);
			sb.Append('\"');
		}
		else
		{
			sb.Append("<unknown>");
		}
		
		sb.Append(", ");
		if (line == endLine)
		{
			sb.Append("line ");
			sb.Append(line);
		}
		else
		{
			sb.Append("lines ");
			sb.Append(line);
			sb.Append('-');
			sb.Append(endLine);
		}
		
		sb.Append(", ");
		if ((line == endLine) && (col == endCol))
		{
			sb.Append("column ");
			sb.Append(col);
		}
		else
		{
			sb.Append("columns ");
			sb.Append(col);
			sb.Append('-');
			sb.Append(endCol);
		}
		
		return sb.ToString();
	}
	
	/// <summary> Set the extent of this instance from the given AST. The AST is
	/// walked, and each node is consulted. The minimum of all start
	/// positions becomes the start position for this instance, and the
	/// maximum of all end positions becomes the end position for this
	/// instance. The file name is set to be the file name associated with
	/// the minimally-positioned token. (This is as good a heuristic as any
	/// given that there is no way to order multiple files in this
	/// implementation.) This method attempts to be smart by ignoring
	/// <code>0</code> position values, and, if no end position is defined
	/// in any of the consulted nodes, it will set the end position of this
	/// instance to be the same as teh start position. This method uses the
	/// position variables of this instance during the tree walk, so it is
	/// unwise to use an instance which is pointed at by the given AST.
	/// 
	/// </summary>
	/// <param name="ast">the tree to figure out the extent of 
	/// </param>
	
	
//	public virtual void  setFromMetaAST(MetaAST ast)
//	{
//		line = System.Int32.MaxValue;
//		col = System.Int32.MaxValue;
//		endLine = System.Int32.MinValue;
//		endCol = System.Int32.MinValue;
//		fileName = null;
//		
//		setFromMetaAST(this, ast);
//		
//		if (line == System.Int32.MaxValue)
//		{
//			line = 0;
//			col = 0;
//		}
//		else if (col == System.Int32.MaxValue)
//		{
//			col = 0;
//		}
//		
//		if (endLine == System.Int32.MinValue)
//		{
//			endLine = line;
//			endCol = col;
//		}
//		else if (endCol == System.Int32.MinValue)
//		{
//			endCol = 0;
//		}
//	}
	
	// ------------------------------------------------------------------------
	// private static methods
	
	/// <summary> Helper method for figuring out the extent of an AST. This is called
	/// by the public method of the same name, and it will in turn call itself
	/// recursively.
	/// 
	/// </summary>
	/// <param name="result">non-null; the token to destructively modify as the
	/// result of the operation
	/// </param>
	/// <param name="ast">non-null; the tree to walk
	/// </param>
	
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