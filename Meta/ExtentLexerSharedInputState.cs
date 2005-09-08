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
using LexerSharedInputState = antlr.LexerSharedInputState;

/// <summary> Extension of {@link LexerSharedInputState} that is aware of
/// file names and can annotate {@link MetaTokens} with them and
/// with end position information.
/// 
/// <p>This file is in the public domain.</p>
/// 
/// </summary>
/// <author>  Dan Bornstein, danfuzz@milk.com
/// </author>
public class SourceAreaLexerSharedInputState:LexerSharedInputState
{
	/// <summary> Get the current line of this instance.
	/// 
	/// </summary>
	/// <returns> the current line number
	/// </returns>
	virtual public int Line
	{
		get
		{
			return line;
		}
		
	}
	/// <summary> Get the current column of this instance.
	/// 
	/// </summary>
	/// <returns> the current column number
	/// </returns>
	virtual public int Column
	{
		get
		{
			return column;
		}
		
	}
	/// <summary> Get the file name of this instance.
	/// 
	/// </summary>
	/// <returns> null-ok; the file name
	/// </returns>
	virtual public System.String FileName
	{
		get
		{
			return fileName;
		}
		
	}
	/// <summary>the name of the file this instance refers to </summary>
	private System.String fileName;
	
	// ------------------------------------------------------------------------
	// constructors
	
	/// <summary> Construct an instance.
	/// 
	/// </summary>
	/// <param name="s">the input stream to use
	/// </param>
	/// <param name="name">null-ok; the file name to associate with this instance
	/// </param>
	public SourceAreaLexerSharedInputState(System.IO.Stream s, System.String name):base(s)
	{
		fileName = name;
	}
	
	/// <summary> Construct an instance. The file name is set to <code>null</code>
	/// initially.
	/// 
	/// </summary>
	/// <param name="s">the input stream to use
	/// </param>
	public SourceAreaLexerSharedInputState(System.IO.Stream s):this(s, null)
	{
	}
	
	/// <summary> Construct an instance which opens and reads the named file.
	/// 
	/// </summary>
	/// <param name="name">non-null; the name of the file to use
	/// </param>
	public SourceAreaLexerSharedInputState(System.String name):this(new System.IO.FileStream(name, System.IO.FileMode.Open, System.IO.FileAccess.Read), name)
	{
	}
	
	// ------------------------------------------------------------------------
	// public instance methods
	
	/// <summary> Annotate an {@link MetaToken} based on this instance. It sets
	/// the end position information as well as the file name.
	/// 
	/// </summary>
	/// <param name="token">non-null; the token to annotate
	/// </param>
	public virtual void  annotate(MetaToken token)
	{
		token.EndLine = line;
		token.EndColumn = column;
		token.FileName = fileName;
	}
}