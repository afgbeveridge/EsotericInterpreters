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
using System.Reflection;

namespace com.complexomnibus.esoteric.interpreter.abstractions {

    public class BasicInterpreter<TSourceType, TExeType>
        where TSourceType : SourceCode, new()
        where TExeType : BaseInterpreterStack, new() {

        public void Execute(Assembly ass, string[] src, Action<Interpreter<TSourceType, TExeType>> preExecution = null) {
            try {
                ExecutionSupport.Assert(src != null, "Source is required for execution");
                Interpreter = new Interpreter<TSourceType, TExeType>(ass);
                preExecution?.Invoke(Interpreter);
                Interpreter.AcceptSource(src);
                Process();
            }
            catch (Exception) {
                throw;
            }
        }

        private Interpreter<TSourceType, TExeType> Interpreter { get; set; }

        private void Process() {
            InterpreterResult result = InterpreterResult.InFlight;
            ExecutionSupport.DebugMode = true;
            ExecutionSupport.Prepare("test");
            while (result != InterpreterResult.Complete) {
                result = Interpreter.Execute();
            }
            ExecutionSupport.UnPrepare();
        }
    }

}
