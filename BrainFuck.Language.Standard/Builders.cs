﻿//============================================================================================================================================================================
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
using SharedObjects.Esoterica;
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace BrainFuckInterpreter {

	public class CommandBuilder : TrivialInterpreterBase<SimpleSourceCode, RandomAccessStack<CanonicalNumber>> {

		private Dictionary<char, Action<InterpreterState, SimpleSourceCode, RandomAccessStack<CanonicalNumber>>> mCommands = new Dictionary<char, Action<InterpreterState, SimpleSourceCode, RandomAccessStack<CanonicalNumber>>>();
		private const char StartConditional = '[';
		private const char EndConditional = ']';

        public CommandBuilder() {
            Initialize();
        }

        public IOWrapper Wrapper { get; set; }

        internal void Initialize() {
			mCommands['>'] = (state, source, stack) => stack.Advance();
			mCommands['<'] = (state, source, stack) => stack.Retreat();
			mCommands['+'] = (state, source, stack) => stack.CurrentCell = stack.CurrentCell + new CanonicalNumber(1);
			mCommands['-'] = (state, source, stack) => stack.CurrentCell = stack.CurrentCell + new CanonicalNumber(-1);
			mCommands['.'] = (state, source, stack) => Wrapper.Write(new string(Convert.ToChar(stack.CurrentCell.Value), 1));
			mCommands[','] = (state, source, stack) => stack.CurrentCell.Value = Wrapper.ReadCharacter().Result;
			mCommands[StartConditional] = (state, source, stack) => {
				if (stack.CurrentCell.Value > 0) 
					source.Advance();
				else {
					source.Seek(EndConditional, SeekDirection.Forward, StartConditional);
					source.Advance();
				}
			};
			mCommands[EndConditional] = (state, source, stack) => {
				source.Seek(StartConditional, SeekDirection.Backward, EndConditional);
			}; 
		}

		public override bool Applicable(InterpreterState state) {
			return Applicable(state.BaseSourceCode.CurrentCharacter());
		}

		public bool Applicable(char current) {
			return mCommands.ContainsKey(current);
		}

		public override BaseObject Gather(InterpreterState state) {
            state.GetExecutionEnvironment<RandomAccessStack<CanonicalNumber>>().ScratchPad[Constants.Builder] = this;
            Wrapper = state.GetExecutionEnvironment<RandomAccessStack<CanonicalNumber>>().ScratchPadAs<IOWrapper>(Constants.CurrentBase);
            char key = state.BaseSourceCode.CurrentCharacter();
			if (key != StartConditional && key != EndConditional)
				state.BaseSourceCode.Advance();
			ExecutionSupport.Emit(() => string.Format("Command created: {0}, source position: {1}", key, state.GetSource<SourceCode>().SourcePosition));
			return new BrainFuckCommand(mCommands[key], key.ToString());
		}
	}


	public class UnknownCommandSkipper : TrivialInterpreterBase<SimpleSourceCode, RandomAccessStack<CanonicalNumber>> {

		public override bool Applicable(InterpreterState state) {
            var bldr = state.GetExecutionEnvironment<RandomAccessStack<CanonicalNumber>>().ScratchPadAs<CommandBuilder>(Constants.Builder);
            return !bldr.Applicable(state.BaseSourceCode.CurrentCharacter());
		}

		public override BaseObject Gather(InterpreterState state) {
			while (state.BaseSourceCode.More() && Applicable(state))
				state.BaseSourceCode.Advance();
			return NullObject.Instance;
		}
	}

	internal class BrainFuckCommand : BaseCommand<Action<InterpreterState, SimpleSourceCode, RandomAccessStack<CanonicalNumber>>> {

		internal BrainFuckCommand(Action<InterpreterState, SimpleSourceCode, RandomAccessStack<CanonicalNumber>> cmd, string keyWord)
			: base(cmd, keyWord) {
		}

		protected override void Interpret(InterpreterState state) {
			Command(state, state.GetSource<SimpleSourceCode>(), state.GetExecutionEnvironment<RandomAccessStack<CanonicalNumber>>());
		}

		public override object Clone() {
			return new BrainFuckCommand(Command, KeyWord);
		}

	}

}
