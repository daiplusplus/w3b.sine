/*
*  BSD 3-clause license:
* 
* Copyright (c) 2008, David Rees / David "W3bbo" Rees / http://www.softicide.com / http://www.w3bdevil.com
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of the David Rees nor the
*       names of its contributors may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY David Rees ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL David Rees BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;

using Cult = System.Globalization.CultureInfo;

namespace W3b.Sine {
	
	/// <summary>Implements arbitrary-precsision arithmetic with a decimal byte array.</summary>
	public class BigNumDec : BigNum {
		
		private List<SByte> _data;
		private Boolean     _sign;
		/// <summary>The exponential part.</summary>
		/// <remarks>Auto-initialised to 0.</remarks>
		private Int32       _exp;
		
		private const String _implName = "DecimalArray";
		
#region Options
		
		/// <summary>Whether to print + in ToString if the value is positive.</summary>
		/// <remarks>Auto-initialised to 0.</remarks>
		private static Boolean _printPositiveSign;
		/// <summary>Number of digits to get when dividing.</summary>
		private static Int32   _divisionDigits    = 100;
		/// <summary>Number of digits to display when calling ToString().</summary>
		private static Int32   _toStringLimit     = 20;
		
#endregion
		
		private readonly static Char[] _digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		
		/// <summary>Creates a new BigNumDec with an empty data list. Not identical to zero.</summary>
		public BigNumDec() {
			_data = new List<SByte>();
			_sign = true;
		}
		
		public BigNumDec(Int64 value) : this() {
			Load( value );
		}
		
		public BigNumDec(Double value) : this() {
			Load( value );
		}
		
		public BigNumDec(String value) : this() {
			Load(value);
		}
		
		public static Boolean PrintPositiveSign {
			get { return _printPositiveSign; }
			set { _printPositiveSign = value; }
		}
		
#region Overriden Methods
		
		public override BigNum Add(BigNum other) {
			return BigNumDec.Add( this, (BigNumDec)other );
		}
			
		public override BigNum Multiply(BigNum multiplicand) {
			return BigNumDec.Multiply(this, (BigNumDec)multiplicand);
		}
		
		public override BigNum Divide(BigNum divisor) {
			return BigNumDec.Divide(this, (BigNumDec)divisor);
		}
		
		public override BigNum Modulo(BigNum divisor) {
			return BigNumDec.Modulo(this, (BigNumDec)divisor);
		}
		
		public override Int32 CompareTo(BigNum other) {
			
			BigNumDec o = other as BigNumDec;
			
			if(o == null) return 1;
			
			if( Equals(o) ) return 0;
			
			if(Sign && !o.Sign) return 1;
			else if(!Sign && !o.Sign) return -1;
			
			AlignExponent(o);
			o.AlignExponent(this);
			
			Int32 expDifference = ( _exp + _data.Count ) - ( o._exp + o._data.Count );
			if(expDifference > 0) return 1;
			else if(expDifference < 0) return -1;
			
			// by now both will have the same length
			for(int i=Length-1;i>=0;i--) {
				if( this[i] > o[i] ) return 1;
				if( this[i] < o[i] ) return -1;
			}
			
			return 0;
			
		}
		
		public override BigNum Negate() {
			BigNum result = Clone();
			result.Sign = !result.Sign;
			return result;
		}
		
		public override BigNum Absolute() {
			BigNum dolly = Clone();
			dolly.Sign = true;
			return dolly;
		}
		
		public override BigNum Floor() {
			return BigNumDec.Subtract( this, GetFractionalPart() );
		}
		
		public override BigNum Ceiling() {
			// using the identity ceil(x) == -floor(-x)
			return this.Negate().Floor().Negate();
		}
		
		public override Boolean IsZero {
			get {
				return Length == 0;
			}
		}
		
		public override BigNum Clone() {
			BigNumDec dolly = new BigNumDec();
			dolly.Sign = Sign;
			dolly._exp = _exp;
			dolly._data.AddRange( _data );
			return dolly;
		}
		
		public override Boolean Equals(Object obj) {
			BigNumDec bn = obj as BigNumDec;
			return bn == null ? false : Equals( bn );
		}
		public Boolean Equals(BigNumDec other) {
			
			if(Object.ReferenceEquals(this, other)) return true;
			
			if(Sign != other.Sign) return false;
			if(_exp != other._exp) return false;
			if(Length != other.Length) return false;
			
			for(int i=0;i<Length;i++) {
				if(this[i] != other[i]) return false;
			}
			
			return true;
			
		}
		
		public override Int32 GetHashCode() {
			return _data.GetHashCode() ^ _exp.GetHashCode() ^ _sign.GetHashCode();
		}
		
		public override String ToString() {
			
			StringBuilder sb = new StringBuilder(_data.Count);
			if(_printPositiveSign) {
				sb.Append( Sign ? '+' : '-' );
			} else {
				if(!Sign) sb.Append( '-' );
			}
			
			//Int32 nofDigits = 0;
			for(int i=Length-1;i>=0;i--) {
				
				if( i + _exp + 1 == 0 ) { // if reached radix point
					sb.Append('.');
				}
				sb.Append( _digits[ _data[i] ] );
				
				//nofDigits++;
				//if(nofDigits > _toStringLimit) break;
			}
			
			if(Length == 0) sb.Append("0");
			
			if( _exp > 0 ) {
				sb.Append('E');
				sb.Append( _exp > 0 ? '+' : '-');
				sb.Append(_exp);
			}
			
			return sb.ToString();
			
		}
		
		public override void Load(String textRepresentation) {
			_data.Clear();
			
			// assuming the string has already been tokenized, expression parsing happens elsewhere
			textRepresentation = textRepresentation.Replace(" ", "").ToUpper(Cult.InvariantCulture);
			
			if(textRepresentation.Length == 0) {
				_data.Add( 0 );
				_exp = 0;
				Sign = true;
				return;
			}
			
			// get the exponent part
			String[] splitAtE = textRepresentation.Split('E');
			if( splitAtE.Length > 1 ) {
				textRepresentation = splitAtE[0];
				String exponentPart = splitAtE[1];
				_exp = Convert.ToInt32( exponentPart, Cult.InvariantCulture );
			}
			
			Boolean hasDot = false;
			for(int i=textRepresentation.Length-1;i>=0;i--) {
				Char c = textRepresentation[i];
				
				if( c >= '0' && c <= '9' ) {
					
					_data.Add( Convert.ToSByte( CharToSByte(c) ) );
					
				} else if(c == '+' || c == '-') {
					
					Sign = c == '+';
					
				} else if(c == '.') {
					
					if(hasDot) throw new FormatException("Too many '.' characters.");
					if(!hasDot) hasDot = true;
					
					_data.Add( -1 ); // special marker -1
					
					
				} else {
					throw new FormatException("Invalid character detected.");
				}
				
			}
			
			// correct exponent, look for the marker -1
			Int32 idx = _data.IndexOf( -1 );
			if( idx != -1 ) {
				_data.RemoveAt( idx );
				_exp -= idx;
			}
			
		}
		
		private enum StringParserState {
			Sign,
			IntegerPart,
			FractionalPart,
			Exponent
		}
		
		private static SByte CharToSByte(Char c) {
			for(sbyte i=0;i<=_digits.Length;i++) {
				if(c == _digits[i]) return i;
			}
			return -2;
		}
		
		public override void Load(Int64 value) {
			
			// cheat by getting .NET to convert to string
			Load( value.ToString(System.Globalization.CultureInfo.InvariantCulture) );
			
		}
		public override void Load(Double value) {
			
			// cheat by getting .NET to convert to string
			Load( value.ToString(System.Globalization.CultureInfo.InvariantCulture) );
			
		}
		
		public override String ImplName {
			get { return _implName; }
		}
		
		public override bool Sign {
			get { return _sign; }
			set { _sign = value; }
		}
		
#endregion
		
#region Operation Implementation
		// there really needs to be a new feature in OOP where interfaces can define static methods without having to use factory classes or the singleton pattern
		
		public static BigNumDec Add(BigNumDec a, BigNumDec b) {
			return Add(a, b, true);
		}
		
		private static BigNumDec Add(BigNumDec a, BigNumDec b, Boolean normalise) {
			
			a.Normalise();
			b.Normalise();
			
			a = (BigNumDec)a.Clone();
			b = (BigNumDec)b.Clone();
			
			if(a.IsZero && b.IsZero) return a;
			if(a.Sign && !b.Sign) {
				b.Sign = true;
				return Subtract(a, b, normalise);
			}
			if(!a.Sign && b.Sign) {
				a.Sign = true;
				return Subtract(b, a, normalise);
			}
			
			BigNumDec result = new BigNumDec();
			
			a.AlignExponent( b );
			b.AlignExponent( a );
			
			result._exp = a._exp;
			
			if(b.Length > a.Length) { // then switch them around
				BigNumDec temp = a;
				a = b;
				b = temp;
			}
			
			// the work:
			// 
			SByte digit = 0;
			SByte carry = 0;
			for(int i=0;i<b.Length;i++) {
				digit = (SByte)( ( a[i] + b[i] + carry ) % 10 );
				carry = (SByte)( ( a[i] + b[i] + carry ) / 10 );
				result._data.Add( digit );
			}
			for(int i=b.Length;i<a.Length;i++) {
				digit = (SByte)( ( a[i] + carry ) % 10 );
				carry = (SByte)( ( a[i] + carry ) / 10 );
				result._data.Add( digit );
			}
			if( carry > 0 ) result._data.Add( carry );
			
			result.Sign = a.Sign && b.Sign;
			
/*			if(normalise) // normalising shouldn't be necessary, but what the heck
				result.Normalise(); */
			
			return result;
			
		}
		
		public static BigNumDec Subtract(BigNumDec a, BigNumDec b) {
			return Subtract(a, b, true);
		}
		
		private static BigNumDec Subtract(BigNumDec a, BigNumDec b, Boolean normalise) {
			
			a.Normalise();
			b.Normalise();
			
			a = (BigNumDec)a.Clone();
			b = (BigNumDec)b.Clone();
			
			if( a.IsZero && b.IsZero ) return a;
			if( a.Sign && !b.Sign ) {
				b.Sign = true;
				return Add(a, b, normalise);
			}
			if( !a.Sign && b.Sign ) {
				a.Sign = true;
				BigNumDec added = Add(a, b, normalise);
				return (BigNumDec)added.Negate();
			}
			
			
			BigNumDec result = new BigNumDec();
			
			a.AlignExponent( b );
			b.AlignExponent( a );
			
			result._exp = a._exp;
			
			Boolean wasSwapped = false;
			if(b.Length > a.Length) { // then switch them around
				BigNumDec temp = a;
				a = b;
				b = temp;
				wasSwapped = true;
			} else {
				// if same length, check magnitude
				BigNumDec a1 = (BigNumDec)a.Absolute();
				BigNumDec b1 = (BigNumDec)b.Absolute();
				if(a1 < b1) {
					BigNumDec temp = a;
					a = b;
					b = temp;
					wasSwapped = true;
				} else if( !(a1 > b1) ) { // i.e. equal
					return new BigNumDec(); // return zero
				}
			}
			
			// Do work
			// it's a sad day when the preparation for an operation is just as long as the operation itself
			
			
			SByte digit = 0;
			SByte take = 0;
			
			for(int i=0;i<b.Length;i++ ) {
				digit = (SByte)( a[i] - b[i] - take );
				if( digit < 0 ) {
					take = 1;
					digit = (SByte)( 10 + digit );
				} else {
					take = 0;
				}
				result._data.Add( digit );
			}
			
			for(int i=b.Length;i<a.Length;i++ ) {
				digit = (SByte)( a[i] - take );
				if( digit < 0 ) {
					take = 1;
					digit = (SByte)( 10 + digit );
				} else {
					take = 0;
				}
				result._data.Add( digit );
			}
			
			result.Sign = a.Sign && b.Sign ? !wasSwapped : wasSwapped;
			
/*			if(normalise)
				result.Normalise(); */
			
			return result;
		}
		
		public static BigNumDec Multiply(BigNumDec a, BigNumDec b) {
			
			a = (BigNumDec)a.Clone();
			b = (BigNumDec)b.Clone();
			
			if(b.Length > a.Length) { // then switch them around
				BigNumDec temp = a;
				a = b;
				b = temp;
			}
			
			BigNumDec retval = new BigNumDec();
			List<BigNumDec> rows = new List<BigNumDec>();
			
			retval.Sign = a.Sign == b.Sign;
			retval._exp = a._exp + b._exp;
			
			// for each digit in b
			for(int i=0;i<b.Length;i++) {
				
				BigNumDec row = new BigNumDec();
				row._exp = retval._exp;
				
				Int32 digit = 0, carry = 0;
				for(int exp=0;exp<i;exp++) row._data.Add( 0 ); // pad with zeros to the right
				
				for(int j=0;j<a.Length;j++) { // perform per-digit multiplication of a against b
					digit = a[j] * b[i] + carry;
					carry = digit / 10;
					digit = digit % 10;
					row._data.Add( (SByte)digit );
				}
				
				// reduce the carry
				while(carry > 0) {
					digit = carry % 10;
					carry = carry / 10;
					row._data.Add( (SByte)digit );
				}
				
				rows.Add( row );
				
			}
			
			// sum the rows to give the result
			foreach(BigNumDec row in rows) {
				retval  = (BigNumDec)retval.Add(row);
			}
			
			return retval;
			
		}
		
		public static BigNumDec Divide(BigNumDec dividend, BigNumDec divisor) {
			Boolean wasExact;
			return Divide(dividend, divisor, out wasExact);
		}
		
		public static BigNumDec Divide(BigNumDec dividend, BigNumDec divisor, out Boolean isExact) {
			
			if(divisor.IsZero) throw new DivideByZeroException();
			
			isExact = true;
			if(dividend.IsZero) return (BigNumDec)dividend.Clone();
			
			///////////////////////////////
			
			BigNumDec quotient = new BigNumDec();
			quotient.Sign = dividend.Sign == divisor.Sign;
			
			dividend = (BigNumDec)dividend.Absolute();
			divisor  = (BigNumDec)divisor.Absolute();
			
			BigNumDec aPortion;
			BigNumDec bDigit = null;
			
			//////////////////////////////
			
			while( divisor[0] == 0 ) { // remove least-significant zeros and up the exponent to compensate
				divisor._exp++;
				divisor._data.RemoveAt(0);
			}
			quotient._exp = dividend.Length + dividend._exp - divisor.Length - divisor._exp + 1;
			dividend._exp = 0;
			divisor._exp  = 0;
			
			aPortion = new BigNumDec();
			
			Int32 bump = 0, c = -1;
			Boolean hump = false;
			
			isExact = false;
			
			while( quotient.Length < _divisionDigits ) { // abandon hope all ye who enter here
				
				aPortion = dividend.Msd( divisor.Length + ( hump ? 1 : 0 ) );
				
				if( aPortion < divisor ) {
					int i = 1;
					while( aPortion < divisor ) {
						aPortion = dividend.Msd( divisor.Length + i + ( hump ? 1 : 0 ) );
						quotient._exp--;
						quotient._data.Add( 0 );
						i++;
					}
					hump = true;
				}
				
				bDigit = 0; //tt = 0;
				c = 0;
				while( bDigit < aPortion ) {
					bDigit = (BigNumDec)bDigit.Add(divisor); //tt += b;
					c++;
				}
				if( bDigit != aPortion ) {
					c--;
					bDigit = new BigNumDec( c );
					bDigit = (BigNumDec)bDigit.Multiply(divisor); //tt *= b;
					isExact = false;
				} else {
					isExact = true;
				}
				
				quotient._data.Add( (sbyte)c );
				quotient._exp--;
				
				aPortion.Normalise();
				dividend.Normalise();
				
				bump = aPortion.Length - bDigit.Length;
				if( aPortion.Length > dividend.Length ) dividend = aPortion;
				bDigit = bDigit.Msd( dividend.Length - bump );
				
				aPortion = BigNumDec.Add(dividend, (BigNumDec)bDigit.Negate(), false );
				
				dividend = (BigNumDec)aPortion.Clone();
				
				if( aPortion.IsZero ) break; // no more work necessary
				
				if( hump ) {
					if( dividend[dividend.Length - 1] == 0 ) {
						dividend._data.RemoveAt( dividend.Length - 1 );
					}
				}
				if( dividend[dividend.Length - 1] == 0 ) {
					dividend._data.RemoveAt( dividend.Length - 1 );
					
					while( dividend[dividend.Length - 1] == 0 ) {
						quotient._exp--;
						quotient._data.Add( 0 );
						dividend._data.RemoveAt( dividend.Length - 1 );
						if( dividend.Length == 0 ) break;
					}
					if( dividend.Length == 0 ) break;
					hump = false;
				} else {
					hump = true;
				}
				
				if( quotient.Length == 82 ) {
					c = 0;
				}
			}
			
			quotient._data.Reverse();
			quotient.Normalise();
			
			return quotient;
			
		}
		
		public static BigNumDec Modulo(BigNumDec a, BigNumDec b) {
			
			// a % b == a  - ( b * Floor[a / b] )
			
			if(b == 0) throw new DivideByZeroException("Divisor b cannot be zero");
			if(a.IsZero) return new BigNumDec();
			
			a = (BigNumDec)a.Clone();
			b = (BigNumDec)b.Clone();
			
			BigNumDec floored = ((a / b) as BigNumDec).Floor() as BigNumDec;
			BigNumDec multbyb = (b * floored) as BigNumDec;
			BigNumDec retval  = (a - multbyb) as BigNumDec;
			
			return retval;
			
		}
		
		
		
#endregion
		
#region Utility Methods and Private Properties
		
/*		public static implicit operator BigNumDec(BigNum number) {
			if(number.ImplName != _implName) throw new InvalidCastException("Cannot convert to BigNumDec from any other implementation.");
			return (BigNumDec)number;
		} */
		
		/// <summary>Nabs the most significant digits of this number. If <paramref name="count" /> is greater than the length of the number it pads zeros.</summary>
		/// <param name="count">Number of digits to return</param>
		private BigNumDec Msd(Int32 count) {
			BigNumDec retVal = new BigNumDec();
			if(count > Length) {
/*				Int32 i = 0;
				while( i < count - Length) {
					retVal._data.Add(0);
					i++;
				}*/
				for(int i=0;i<count-Length;i++) {
					retVal._data.Add( 0 );
				}
				count = Length;
			}
			for(int i=0;i<count;i++) {
				retVal._data.Add( this[Length - count + i] );
			}
			return retVal;
		}
		
		/// <summary>Makes the exponent values of both numbers equal by padding zeros.</summary>
		private void AlignExponent(BigNumDec withThis) {
			while( _exp > withThis._exp ) {
				_exp--;
				_data.Insert( 0, 0 );
			}
		}
		
		/// <summary>Removes chaff elements from the internal list.</summary>
		private void Normalise() {
			for(int i=Length-1;i>=0;i--) {
				if( _data[i] != 0 ) break;
				_data.RemoveAt( i );
			}
			if( Length == 0 ) Sign = true; // can't have "negative zero"
			/*else {
				while( _data[0] == 0 ) { // removes least-significant zeros
					_data.RemoveAt(0);
					_exp++;
				}
			}*/
			
		}
		
		private Int32 Length { get { return _data.Count; } }
		
		private SByte this[Int32 index] {
			get { return _data[index]; }
		}
		
		private BigNumDec GetFractionalPart() {
			
			if(_exp == 0) return (BigNumDec)0;
			
			Normalise();
			
			BigNumDec retVal = new BigNumDec();
			for(int i=0;i<Length && i<-_exp;i++) { // get all the digits to the right of the radix point
				retVal._data.Add( this[i] );
			}
			retVal._exp = _exp;
			
			return retVal;
			
		}
		
		public static implicit operator BigNumDec(String s) {
			if(s == null) return null;
			return new BigNumDec(s);
		}
		
		public static implicit operator BigNumDec(Int64 i) {
			return new BigNumDec(i);
		}
		
#endregion
		
	}
	
}