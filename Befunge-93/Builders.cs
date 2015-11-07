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
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace Befunge_93 {

	//public class NumberBuilder : BasicNumberBuilder<SourceCodeTorus, BaseInterpreterStack> {
	//}

	public class CommandBuilder : TrivialInterpreterBase<SourceCodeTorus, BaseInterpreterStack> {

		private class CommandBundle {
			internal Action<InterpreterState, SourceCodeTorus, BaseInterpreterStack> Action { get; set; }
			internal bool SuppressAdvanceOnParse { get; set; }
		}

		private static Random mRandom = new Random();
		private static Dictionary<string, CommandBundle> mCommands = new Dictionary<string, CommandBundle>();

		static CommandBuilder() {
			mCommands[">"] = new CommandBundle { Action = (state, source, stack) => { source.Direction = DirectionOfTravel.Right; source.Advance(); }, SuppressAdvanceOnParse = true };
			mCommands["<"] = new CommandBundle { Action = (state, source, stack) => { source.Direction = DirectionOfTravel.Left; source.Advance(); }, SuppressAdvanceOnParse = true };
			mCommands["^"] = new CommandBundle { Action = (state, source, stack) => { source.Direction = DirectionOfTravel.Up; source.Advance(); }, SuppressAdvanceOnParse = true };
			mCommands["v"] = new CommandBundle { Action = (state, source, stack) => { source.Direction = DirectionOfTravel.Down; source.Advance(); }, SuppressAdvanceOnParse = true };
			mCommands["@"] = new CommandBundle { Action = (state, source, stack) => source.CompletionSignalled = true };
			mCommands["+"] = new CommandBundle { Action = CommonCommands.BinaryAddition<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["-"] = new CommandBundle { Action = CommonCommands.BinarySubtraction<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["*"] = new CommandBundle { Action = CommonCommands.BinaryMultiplication<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["/"] = new CommandBundle { Action = CommonCommands.Division<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["\\"] = new CommandBundle { Action = (state, source, stack) => stack.Swap() };
			mCommands[":"] = new CommandBundle { Action = (state, source, stack) => stack.Duplicate() };
			mCommands["."] = new CommandBundle { Action = CommonCommands.OutputValueFromStack<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands[","] = new CommandBundle { Action = CommonCommands.OutputCharacterFromStack<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["&"] = new CommandBundle { Action = CommonCommands.ReadValueAndPush<SourceCodeTorus, BaseInterpreterStack>(true) };
			mCommands["~"] = new CommandBundle { Action = CommonCommands.ReadValueAndPush<SourceCodeTorus, BaseInterpreterStack>() };
			mCommands["$"] = new CommandBundle { Action = (state, source, stack) => stack.Pop() };
			mCommands["g"] = new CommandBundle { Action = (state, source, stack) => stack.Push(new CanonicalNumber(Convert.ToInt32(source[CreateTuple(stack)]))) };
			mCommands["p"] = new CommandBundle { Action = (state, source, stack) => source[CreateTuple(stack)] = Convert.ToChar(stack.Pop<CanonicalNumber>().Value) };
			mCommands["#"] = new CommandBundle { Action = (state, source, stack) => source.Advance() };
			mCommands["!"] = new CommandBundle { Action = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>().Value != 0 ?  CanonicalBoolean.False : CanonicalBoolean.True) };
			mCommands["?"] = new CommandBundle { Action = (state, source, stack) => { 
							source.Direction = (DirectionOfTravel) mRandom.Next(Enum.GetNames(typeof(DirectionOfTravel)).Length); // TODO: Suss 
							source.Advance(); 
				}, 
				SuppressAdvanceOnParse = true };
			mCommands["%"] = new CommandBundle {
				Action = (state, source, stack) => {
					var lhs = stack.Pop<CanonicalNumber>();
					stack.Push(new CanonicalNumber(stack.Pop<CanonicalNumber>().Value % lhs.Value));
				}
			};
			mCommands["`"] = new CommandBundle {
				Action = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() <= stack.Pop<CanonicalNumber>() ? CanonicalBoolean.True : CanonicalBoolean.False)
			};
			mCommands["_"] = new CommandBundle {
				Action = (state, source, stack) => Decide(source, stack, DirectionOfTravel.Right, DirectionOfTravel.Left),
				SuppressAdvanceOnParse = true
			};
			mCommands["|"] = new CommandBundle {
				Action = (state, source, stack) => Decide(source, stack, DirectionOfTravel.Down, DirectionOfTravel.Up),
				SuppressAdvanceOnParse = true
			};
			//| (vertical if)   <boolean value>       PC->up if <value>, else PC->down
			mCommands["\""] = new CommandBundle {
				Action = (state, source, stack) => {
					while (source.CurrentCharacter() != '\"') {
						stack.Push(new CanonicalNumber(Convert.ToInt32(source.CurrentCharacter())));
						source.Advance();
					}
					source.Advance();
				}
			};
			Enumerable.Range(0, 10).ToList().ForEach(n => mCommands[n.ToString()] = new CommandBundle { Action = (state, source, stack) => stack.Push(new CanonicalNumber(n)) });
		}

		public override bool Applicable(InterpreterState state) {
			return Applicable(state.BaseSourceCode.Current());
		}

		public static bool Applicable(string current) {
			return mCommands.ContainsKey(current);
		}

		public override BaseObject Gather(InterpreterState state) {
			string key = state.BaseSourceCode.Current();
			ExecutionSupport.Assert(mCommands.ContainsKey(key), string.Concat("Internal error \"", key, "\" is not a command"));
			CommandBundle bundle = mCommands[key];
			if (!bundle.SuppressAdvanceOnParse)
				state.BaseSourceCode.Advance();
			ExecutionSupport.Emit(() => string.Format("Command created: {0}, source position: {1}", key, state.GetSource<SourceCode>().SourcePosition));
			return new Befunge93Command(bundle.Action, key);
		}

		private static MutableTuple<int> CreateTuple(BaseInterpreterStack stack) {
			int y = stack.Pop<CanonicalNumber>().Value;
			return new MutableTuple<int>(stack.Pop<CanonicalNumber>().Value, y);
		}

		private static void Decide(SourceCodeTorus source, BaseInterpreterStack stack, DirectionOfTravel falseDirection, DirectionOfTravel trueDirection) {
			source.Direction = stack.Pop<CanonicalNumber>() ? trueDirection : falseDirection;
			source.Advance();
		}
	}


	public class UnknownCommandSkipper : TrivialInterpreterBase<SourceCodeTorus, BaseInterpreterStack> {

		public override bool Applicable(InterpreterState state) {
			return !CommandBuilder.Applicable(state.BaseSourceCode.Current());
		}

		public override BaseObject Gather(InterpreterState state) {
			while (state.BaseSourceCode.More() && Applicable(state))
				state.BaseSourceCode.Advance();
			return NullObject.Instance;
		}
	}

	internal class Befunge93Command : BaseCommand<Action<InterpreterState, SourceCodeTorus, BaseInterpreterStack>> {

		internal Befunge93Command(Action<InterpreterState, SourceCodeTorus, BaseInterpreterStack> cmd, string keyWord)
			: base(cmd, keyWord) {
		}

		protected override void Interpret(InterpreterState state) {
			Command(state, state.GetSource<SourceCodeTorus>(), state.GetExecutionEnvironment<BaseInterpreterStack>());
		}

		public override object Clone() {
			return new Befunge93Command(Command, KeyWord);
		}

	}

}
