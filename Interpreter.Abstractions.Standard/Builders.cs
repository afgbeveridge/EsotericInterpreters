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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Configuration;

namespace com.complexomnibus.esoteric.interpreter.abstractions {

	public class BasicNumberBuilder<TSourceType, TExeType> : TrivialInterpreterBase<TSourceType, TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {

		private const string NumberConfiguration = "maxNumberLength";
		private int MaxNumberLength { get; set; }

		public override bool Applicable(InterpreterState state) {
			return char.IsDigit(state.BaseSourceCode.Current().First());
		}

		private string Value { get; set; }

		public override BaseObject Gather(InterpreterState state) {
			CheckConfiguration(state);
			Value = String.Empty;
			while (state.BaseSourceCode.More() && Applicable(state) && Value.Length < MaxNumberLength) {
				Value = Value + state.BaseSourceCode.Current();
				state.BaseSourceCode.Advance();
			}
			ExecutionSupport.Emit(() => string.Format("Number created: {0}", Value));
			return new CanonicalNumber(int.Parse(Value));
		}

		private void CheckConfiguration(InterpreterState state) {
			if (MaxNumberLength == 0) 
				MaxNumberLength = Configuration.ConfigurationFor<int>(NumberConfiguration, int.MaxValue);
		}
	}

}
