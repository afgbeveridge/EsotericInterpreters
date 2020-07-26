using System;
//============================================================================================================================================================================
// Copyright (c) 2013 Tony Beveridge
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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace WARP {

	public class Program {

		private const string Message = "WARP interpreter v 2.0: (c) 2013 Tony Beveridge, released under the MIT license";

		static void Main(string[] args) {
            new CommandLineExecutor<SimpleSourceCode, PropertyBasedExecutionEnvironment>()
                .Execute(typeof(CommandBuilder).Assembly, Message, args,
				interp => {
                    var env = interp.State.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>();
                    env.ScratchPad[Constants.RASName] =
						new RandomAccessStack<WARPObject> { MaximumSize = Configuration.ConfigurationFor<int>("rasSize") };
                    env.ScratchPad[Constants.CurrentBase] = new ConsoleIOWrapper();
                    env.OnUnknownKey = e => new WARPObject();
				}
			);
		}
	}

}
