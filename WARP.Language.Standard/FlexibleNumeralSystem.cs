using System;
using System.Collections.Generic;
using System.Linq;
//============================================================================================================================================================================
// Copyright (c) 2011-2013 Tony Beveridge
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software 
// without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
// persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//============================================================================================================================================================================

using System.Text;
using System.Text.RegularExpressions;
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace WARP {

	public static class FlexibleNumeralSystem {

		public const int StandardRadix = 36;
		public const string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static readonly char[] CharListArray = CharList.ToArray();
		private static Dictionary<int, Regex> ValidCharacterSets = new Dictionary<int, Regex>(); 

		public static String Encode(long input, int radix = StandardRadix) {
			long source = Math.Abs(input);
			var result = new Stack<char>();
			while (source != 0) {
				result.Push(CharListArray[source % radix]);
				source /= radix;
			} 
			return String.Concat(input < 0 ? "-" : String.Empty, !result.Any() ? "0" : new string(result.ToArray()));
		}

		private static Int64 Decode(string input, int radix = StandardRadix) {
			string source = input.StartsWith("-") ? input.Substring(1) : input;
			int pos = 0;
			return source.ToUpper().Reverse().Sum(c => CharList.IndexOf(c) * (long)Math.Pow(radix, pos++)) * (input != source ? -1 : 1);
		}

		public static bool CanParse(string input, int radix = StandardRadix) {
			ExecutionSupport.Assert(radix > 1 && radix <= StandardRadix, string.Concat("Invalid radix ", radix));
			if (!ValidCharacterSets.ContainsKey(radix)) {
				char[] usableCharacters = new char[radix];
				Array.Copy(CharListArray, usableCharacters, radix);
				ValidCharacterSets[radix] = RegexBuilder.New().StartsWith().Optional("-").AddCharacterClass(new string(usableCharacters)).OneOrMore().EndMatching().ToRegex();
			}
			return ValidCharacterSets[radix].IsMatch(input);
		}

		public static long Decode(string input, int radix = StandardRadix, long defaultValue = 0L) {
			return CanParse(input, radix) ? Decode(input, radix) : defaultValue;
		}
	}
}
