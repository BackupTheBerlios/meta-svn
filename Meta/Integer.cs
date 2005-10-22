//************************************************************************************
// Integer Class Version 1.03
//
// Copyright (c) 2002 Chew Keong TAN
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, provided that the above
// copyright notice(s) and this permission notice appear in all copies of
// the Software and that both the above copyright notice(s) and this
// permission notice appear in supporting documentation.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
// OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL
// INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING
// FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
// NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
// WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//
//
// Disclaimer
// ----------
// Although reasonable care has been taken to ensure the correctness of this
// implementation, this code should never be used in any application without
// proper verification and testing.  I disclaim all liability and responsibility
// to any person or entity with respect to any loss or damage caused, or alleged
// to be caused, directly or indirectly, by the use of this Integer class.


using System;
public class Integer
{		
	private const int maxLength = 70;
	private uint[] data = null;
	public int dataLength;

	public Integer()
	{
		data = new uint[maxLength];
		dataLength = 1;
	}

	public Integer(long value)
	{
		data = new uint[maxLength];
		long tempVal = value;

		// copy bytes from long to Integer without any assumption of
		// the length of the long datatype

		dataLength = 0;
		while(value != 0 && dataLength < maxLength)
		{
			data[dataLength] = (uint)(value & 0xFFFFFFFF);
			value >>= 32;
			dataLength++;
		}

		if(tempVal > 0)         // overflow check for +ve value
		{
			if(value != 0 || (data[maxLength-1] & 0x80000000) != 0)
				throw(new ArithmeticException("Positive overflow in constructor."));
		}
		else if(tempVal < 0)    // underflow check for -ve value
		{
			if(value != -1 || (data[dataLength-1] & 0x80000000) == 0)
				throw(new ArithmeticException("Negative underflow in constructor."));
		}

		if(dataLength == 0)
			dataLength = 1;
	}

	public Integer(ulong value)
	{
		data = new uint[maxLength];

		// copy bytes from ulong to Integer without any assumption of
		// the length of the ulong datatype

		dataLength = 0;
		while(value != 0 && dataLength < maxLength)
		{
			data[dataLength] = (uint)(value & 0xFFFFFFFF);
			value >>= 32;
			dataLength++;
		}

		if(value != 0 || (data[maxLength-1] & 0x80000000) != 0)
			throw(new ArithmeticException("Positive overflow in constructor."));

		if(dataLength == 0)
			dataLength = 1;
	}
	public Integer(Integer bi)
	{
		data = new uint[maxLength];

		dataLength = bi.dataLength;

		for(int i = 0; i < dataLength; i++)
			data[i] = bi.data[i];
	}
	// maybe remove, or rename to TryParseInteger
	public Integer(string value, int radix)
	{
		Integer multiplier = new Integer(1);
		Integer result = new Integer();
		value = (value.ToUpper()).Trim();
		int limit = 0;

		if(value[0] == '-')
			limit = 1;

		for(int i = value.Length - 1; i >= limit ; i--)
		{
			int posVal = (int)value[i];

			if(posVal >= '0' && posVal <= '9')
				posVal -= '0';
			else if(posVal >= 'A' && posVal <= 'Z')
				posVal = (posVal - 'A') + 10;
			else
				posVal = 9999999;       // arbitrary large


			if(posVal >= radix)
				throw(new ArithmeticException("Invalid string in constructor."));
			else
			{
				if(value[0] == '-')
					posVal = -posVal;

				result = result + (multiplier * posVal);

				if((i - 1) >= limit)
					multiplier = multiplier * radix;
			}
		}

		if(value[0] == '-')     // negative values
		{
			if((result.data[maxLength-1] & 0x80000000) == 0)
				throw(new ArithmeticException("Negative underflow in constructor."));
		}
		else    // positive values
		{
			if((result.data[maxLength-1] & 0x80000000) != 0)
				throw(new ArithmeticException("Positive overflow in constructor."));
		}

		data = new uint[maxLength];
		for(int i = 0; i < result.dataLength; i++)
			data[i] = result.data[i];

		dataLength = result.dataLength;
	}
	public static implicit operator Integer(long value)
	{
		return (new Integer(value));
	}

	public static implicit operator Integer(ulong value)
	{
		return (new Integer(value));
	}

	public static implicit operator Integer(int value)
	{
		return (new Integer((long)value));
	}

	public static implicit operator Integer(uint value)
	{
		return (new Integer((ulong)value));
	}

	public static Integer operator +(Integer bi1, Integer bi2)
	{
		Integer result = new Integer();

		result.dataLength = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

		long carry = 0;
		for(int i = 0; i < result.dataLength; i++)
		{
			long sum = (long)bi1.data[i] + (long)bi2.data[i] + carry;
			carry  = sum >> 32;
			result.data[i] = (uint)(sum & 0xFFFFFFFF);
		}

		if(carry != 0 && result.dataLength < maxLength)
		{
			result.data[result.dataLength] = (uint)(carry);
			result.dataLength++;
		}

		while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
			result.dataLength--;


		// overflow check
		int lastPos = maxLength - 1;
		if((bi1.data[lastPos] & 0x80000000) == (bi2.data[lastPos] & 0x80000000) &&
			(result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
		{
			throw (new ArithmeticException());
		}

		return result;
	}

	public static Integer operator -(Integer bi1, Integer bi2)
	{
		Integer result = new Integer();

		result.dataLength = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

		long carryIn = 0;
		for(int i = 0; i < result.dataLength; i++)
		{
			long diff;

			diff = (long)bi1.data[i] - (long)bi2.data[i] - carryIn;
			result.data[i] = (uint)(diff & 0xFFFFFFFF);

			if(diff < 0)
				carryIn = 1;
			else
				carryIn = 0;
		}

		// roll over to negative
		if(carryIn != 0)
		{
			for(int i = result.dataLength; i < maxLength; i++)
				result.data[i] = 0xFFFFFFFF;
			result.dataLength = maxLength;
		}

		// fixed in v1.03 to give correct datalength for a - (-b)
		while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
			result.dataLength--;

		// overflow check

		int lastPos = maxLength - 1;
		if((bi1.data[lastPos] & 0x80000000) != (bi2.data[lastPos] & 0x80000000) &&
			(result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
		{
			throw (new ArithmeticException());
		}

		return result;
	}

	public static Integer operator *(Integer bi1, Integer bi2)
	{
		int lastPos = maxLength-1;
		bool bi1Neg = false, bi2Neg = false;

		// take the absolute value of the inputs
		try
		{
			if((bi1.data[lastPos] & 0x80000000) != 0)     // bi1 negative
			{
				bi1Neg = true; bi1 = -bi1;
			}
			if((bi2.data[lastPos] & 0x80000000) != 0)     // bi2 negative
			{
				bi2Neg = true; bi2 = -bi2;
			}
		}
		catch(Exception) {}

		Integer result = new Integer();

		// multiply the absolute values
		try
		{
			for(int i = 0; i < bi1.dataLength; i++)
			{
				if(bi1.data[i] == 0)    continue;

				ulong mcarry = 0;
				for(int j = 0, k = i; j < bi2.dataLength; j++, k++)
				{
					// k = i + j
					ulong val = ((ulong)bi1.data[i] * (ulong)bi2.data[j]) +
						(ulong)result.data[k] + mcarry;

					result.data[k] = (uint)(val & 0xFFFFFFFF);
					mcarry = (val >> 32);
				}

				if(mcarry != 0)
					result.data[i+bi2.dataLength] = (uint)mcarry;
			}
		}
		catch(Exception)
		{
			throw(new ArithmeticException("Multiplication overflow."));
		}


		result.dataLength = bi1.dataLength + bi2.dataLength;
		if(result.dataLength > maxLength)
			result.dataLength = maxLength;

		while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
			result.dataLength--;

		// overflow check (result is -ve)
		if((result.data[lastPos] & 0x80000000) != 0)
		{
			if(bi1Neg != bi2Neg && result.data[lastPos] == 0x80000000)    // different sign
			{
				// handle the special case where multiplication produces
				// a max negative number in 2's complement.

				if(result.dataLength == 1)
					return result;
				else
				{
					bool isMaxNeg = true;
					for(int i = 0; i < result.dataLength - 1 && isMaxNeg; i++)
					{
						if(result.data[i] != 0)
							isMaxNeg = false;
					}

					if(isMaxNeg)
						return result;
				}
			}

			throw(new ArithmeticException("Multiplication overflow."));
		}

		// if input has different signs, then result is -ve
		if(bi1Neg != bi2Neg)
			return -result;

		return result;
	}

	public static Integer operator -(Integer bi1)
	{
		// handle neg of zero separately since it'll cause an overflow
		// if we proceed.

		if(bi1.dataLength == 1 && bi1.data[0] == 0)
			return (new Integer());

		Integer result = new Integer(bi1);

		// 1's complement
		for(int i = 0; i < maxLength; i++)
			result.data[i] = (uint)(~(bi1.data[i]));

		// add one to result of 1's complement
		long val, carry = 1;
		int index = 0;

		while(carry != 0 && index < maxLength)
		{
			val = (long)(result.data[index]);
			val++;

			result.data[index] = (uint)(val & 0xFFFFFFFF);
			carry = val >> 32;

			index++;
		}

		if((bi1.data[maxLength-1] & 0x80000000) == (result.data[maxLength-1] & 0x80000000))
			throw (new ArithmeticException("Overflow in negation.\n"));

		result.dataLength = maxLength;

		while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
			result.dataLength--;
		return result;
	}

	public static bool operator ==(Integer bi1, Integer bi2)
	{
		return (object.ReferenceEquals(bi1,bi2)) || bi1.Equals(bi2);
	}


	public static bool operator !=(Integer bi1, Integer bi2)
	{
		return !(bi1==bi2);
	}

	public override bool Equals(object o)
	{
		if(!(o is Integer))
		{
			return false;
		}
		Integer bi = (Integer)o;

		if(this.dataLength != bi.dataLength)
			return false;

		for(int i = 0; i < this.dataLength; i++)
		{
			if(this.data[i] != bi.data[i])
				return false;
		}
		return true;
	}

	public override int GetHashCode() {
		Integer x=new Integer(this);
		while(x>int.MaxValue) {
			x=x-int.MaxValue;
		}
		return x.GetInt32();
	}

	public static bool operator >(Integer bi1, Integer bi2)
	{
		int pos = maxLength - 1;

		// bi1 is negative, bi2 is positive
		if((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
			return false;

			// bi1 is positive, bi2 is negative
		else if((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
			return true;

		// same sign
		int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
		for(pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--);

		if(pos >= 0)
		{
			if(bi1.data[pos] > bi2.data[pos])
				return true;
			return false;
		}
		return false;
	}


	public static bool operator <(Integer bi1, Integer bi2)
	{
		int pos = maxLength - 1;

		// bi1 is negative, bi2 is positive
		if((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
			return true;

			// bi1 is positive, bi2 is negative
		else if((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
			return false;

		// same sign
		int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
		for(pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--);

		if(pos >= 0)
		{
			if(bi1.data[pos] < bi2.data[pos])
				return true;
			return false;
		}
		return false;
	}


	public static bool operator >=(Integer bi1, Integer bi2)
	{
		return (bi1 == bi2 || bi1 > bi2);
	}


	public static bool operator <=(Integer bi1, Integer bi2)
	{
		return (bi1 == bi2 || bi1 < bi2);
	}

	private static void singleByteDivide(Integer bi1, Integer bi2,
		Integer outQuotient, Integer outRemainder)
	{
		uint[] result = new uint[maxLength];
		int resultPos = 0;

		// copy dividend to reminder
		for(int i = 0; i < maxLength; i++)
			outRemainder.data[i] = bi1.data[i];
		outRemainder.dataLength = bi1.dataLength;

		while(outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength-1] == 0)
			outRemainder.dataLength--;

		ulong divisor = (ulong)bi2.data[0];
		int pos = outRemainder.dataLength - 1;
		ulong dividend = (ulong)outRemainder.data[pos];


		if(dividend >= divisor)
		{
			ulong quotient = dividend / divisor;
			result[resultPos++] = (uint)quotient;

			outRemainder.data[pos] = (uint)(dividend % divisor);
		}
		pos--;

		while(pos >= 0)
		{

			dividend = ((ulong)outRemainder.data[pos+1] << 32) + (ulong)outRemainder.data[pos];
			ulong quotient = dividend / divisor;
			result[resultPos++] = (uint)quotient;

			outRemainder.data[pos+1] = 0;
			outRemainder.data[pos--] = (uint)(dividend % divisor);
			//Console.WriteLine(">>>> " + bi1);
		}

		outQuotient.dataLength = resultPos;
		int j = 0;
		for(int i = outQuotient.dataLength - 1; i >= 0; i--, j++)
			outQuotient.data[j] = result[i];
		for(; j < maxLength; j++)
			outQuotient.data[j] = 0;

		while(outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength-1] == 0)
			outQuotient.dataLength--;

		if(outQuotient.dataLength == 0)
			outQuotient.dataLength = 1;

		while(outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength-1] == 0)
			outRemainder.dataLength--;
	}

	public static Integer operator |(Integer bi1, Integer bi2)
	{
		Integer result = new Integer();

		int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;

		for(int i = 0; i < len; i++)
		{
			uint sum = (uint)(bi1.data[i] | bi2.data[i]);
			result.data[i] = sum;
		}

		result.dataLength = maxLength;

		while(result.dataLength > 1 && result.data[result.dataLength-1] == 0)
			result.dataLength--;

		return result;
	}

	public Integer abs()
	{
		if((this.data[maxLength - 1] & 0x80000000) != 0)
			return (-this);
		else
			return (new Integer(this));
	}

	public override string ToString()
	{
		return ToString(10);
	}

	// reduce this to radix 10
	public string ToString(int radix)
	{
		if(radix < 2 || radix > 36)
			throw (new ArgumentException("Radix must be >= 2 and <= 36"));

		string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		string result = "";

		Integer a = this;

		bool negative = false;
		if((a.data[maxLength-1] & 0x80000000) != 0)
		{
			negative = true;
			try
			{
				a = -a;
			}
			catch(Exception) {}
		}

		Integer quotient = new Integer();
		Integer remainder = new Integer();
		Integer biRadix = new Integer(radix);

		if(a.dataLength == 1 && a.data[0] == 0)
			result = "0";
		else
		{
			while(a.dataLength > 1 || (a.dataLength == 1 && a.data[0] != 0))
			{
				singleByteDivide(a, biRadix, quotient, remainder);

				if(remainder.data[0] < 10)
					result = remainder.data[0] + result;
				else
					result = charSet[(int)remainder.data[0] - 10] + result;

				a = quotient;
			}
			if(negative)
				result = "-" + result;
		}

		return result;
	}

	public int GetInt32()
	{
		return (int)data[0];
	}

	public long GetInt64()
	{
		long val = 0;

		val = (long)data[0];
		try
		{       // exception if maxLength = 1
			val |= (long)data[1] << 32;
		}
		catch(Exception)
		{
			if((data[0] & 0x80000000) != 0) // negative
				val = (int)data[0];
		}

		return val;
	}

}