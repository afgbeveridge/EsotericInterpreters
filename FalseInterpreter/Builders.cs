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
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Configuration;
using com.complexomnibus.esoteric.interpreter.abstractions;
using FEE = com.complexomnibus.esoteric.interpreter.abstractions.PropertyBasedExecutionEnvironment;

namespace com.complexomnibus.esoteric.interpreter.implementation.falseLanguage {

	public class NumberBuilder : BasicNumberBuilder<SimpleSourceCode, PropertyBasedExecutionEnvironment> {
	}

	public class VariableBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		public override bool Applicable(InterpreterState state) {
			return char.IsLower(state.Source().Current().First());
		}

		private string Value { get; set; }

		public override BaseObject Gather(InterpreterState state) {
			Value = state.Source().Current();
			ExecutionSupport.Emit(() => string.Format("Variable pushed: {0}", Value));
			state.Source().Advance();
			return new FalseVariable(Value);
		}
	}

	public class WhiteSpaceSkipper : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		public override bool Applicable(InterpreterState state) {
			return char.IsWhiteSpace(state.Source().Current().First());
		}

		public override BaseObject Gather(InterpreterState state) {
			while (state.Source().More() && Applicable(state))
				state.Source().Advance();
			return NullObject.Instance;
		}
	}

	public class SequenceBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		private static Dictionary<string, Tuple<string, bool>> mDictionary = new Dictionary<string, Tuple<string, bool>> { 
			{ "{", new Tuple<string, bool>("}", true) },
			{ "\"", new Tuple<string, bool>("\"", false) }
		};

		public override bool Applicable(InterpreterState state) {
			return mDictionary.ContainsKey(state.Source().Current());
		}

		public override BaseObject Gather(InterpreterState state) {
			Tuple<string, bool> cur = mDictionary[state.Source().Current()];
			StringBuilder bldr = new StringBuilder();
			state.Source().Advance();
			while (state.Source().More() && state.Source().Current() != cur.Item1) {
				bldr.Append(state.Source().Current());
				state.Source().Advance();
			}
			state.Source().Advance();
			return new FalseString(bldr.ToString(), cur.Item2);
		}
	}

	public class CommandBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		private static Dictionary<string, Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment>> mCommands = new Dictionary<string, Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment>>();

		static CommandBuilder() {
			mCommands["$"] = (state, source, stack) => stack.Duplicate();
			mCommands["("] = (state, source, stack) => stack.Pick(stack.Pop<CanonicalNumber>().Value);
			mCommands["+"] = CommonCommands.BinaryAddition<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["-"] = CommonCommands.BinarySubtraction<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["*"] = CommonCommands.BinaryMultiplication<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["/"] = CommonCommands.Division<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["<"] = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() > stack.Pop<CanonicalNumber>());
			mCommands[">"] = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() < stack.Pop<CanonicalNumber>());
			mCommands["="] = CommonCommands.Equality<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["&"] = CommonCommands.LogicalAnd<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["|"] = CommonCommands.LogicalOr<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["_"] = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() * CanonicalBoolean.True);
			mCommands["~"] = (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>().Negate());
			mCommands[":"] = (state, source, stack) => stack[stack.Pop<FalseVariable>().Key] = stack.Pop<BaseObject>();
			mCommands[";"] = (state, source, stack) => stack.Push(stack[stack.Pop<FalseVariable>().Key]);
			mCommands["!"] = (state, source, stack) => stack.Pop<FalseLambda>().Execute(state);
			mCommands["`"] = (state, source, stack) => stack.Pop<BaseObject>();
			mCommands["%"] = (state, source, stack) => stack.Pop<BaseObject>();
			mCommands["\\"] = (state, source, stack) => stack.Swap();
			mCommands["@"] = (state, source, stack) => stack.Rotate();
			mCommands["."] = CommonCommands.OutputValueFromStack<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["^"] = CommonCommands.ReadValueAndPush<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands[","] = CommonCommands.OutputCharacterFromStack<SourceCode, PropertyBasedExecutionEnvironment>();
			mCommands["?"] = (state, source, stack) => ExecuteConditional(state);
			mCommands["#"] = (state, source, stack) => ExecuteLoop(state);
		}

		private static void ExecuteConditional(InterpreterState state) {
			FalseLambda fun = state.Stack<FEE>().Pop<FalseLambda>();
			if (state.Stack<FEE>().Pop<CanonicalNumber>())
				fun.Execute(state);
		}

		private static void ExecuteLoop(InterpreterState state) {
			FalseLambda fun = state.Stack<FEE>().Pop<FalseLambda>();
			FalseLambda test = state.Stack<FEE>().Pop<FalseLambda>();
			test.Execute(state);
			while (state.Stack<FEE>().Pop<CanonicalNumber>()) {
				fun.Execute(state);
				test.Execute(state);
			}
		}

		public override bool Applicable(InterpreterState state) {
			return mCommands.ContainsKey(state.Source().Current());
		}

		public override BaseObject Gather(InterpreterState state) {
			string key = state.Source().Current();
			state.Source().Advance();
			ExecutionSupport.Emit(() => string.Format("Command created: {0}, Source Position {1}", key, state.Source().SourcePosition));
			return new ActionCommand<PropertyBasedExecutionEnvironment>(mCommands[key], key);
		}
	}

	public class LambdaBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		private const string LambdaStart = "[";
		private const string LambdaEnd = "]";

		public override bool Applicable(InterpreterState state) {
			return state.Source().Current() == LambdaStart;
		}

		public override BaseObject Gather(InterpreterState state) {
			FalseLambda func = new FalseLambda();
			state.Source().Advance();
			while (state.Source().Current() != LambdaEnd && state.Source().More())
				func.AddCommand(Interpreter.Gather());
			state.Source().Advance();
			return func;
		}
	}

	public class CharBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		public override bool Applicable(InterpreterState state) {
			return state.Source().Current() == "'";
		}

		public override BaseObject Gather(InterpreterState state) {
			state.Source().Advance();
			CanonicalNumber num = new CanonicalNumber(Convert.ToInt32(state.Source().Current().First()));
			state.Source().Advance();
			return num;
		}
	}

	// TODO: Combine with CommandBuilder
	public class ExtendedCommandBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		private static Dictionary<string, Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment, string>> mCommands = new Dictionary<string, Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment, string>>();
		private const string CommandStartAndEnd = ")";

		static ExtendedCommandBuilder() {
			mCommands["st"] = (state, source, stack, ctx) => StackingTimer.Start(ctx);
			mCommands["ct"] = (state, source, stack, ctx) => StackingTimer.Stop();
			mCommands["rs"] = (state, source, stack, ctx) => Statistics.Reset();
			mCommands["ds"] = (state, source, stack, ctx) => Statistics.Dump();
		}

		public override bool Applicable(InterpreterState state) {
			return state.Source().Current() == CommandStartAndEnd;
		}

		public override BaseObject Gather(InterpreterState state) {
			state.Source().Advance();
			string key = string.Concat(state.Source().Current(), state.Source().AdvanceAndReturn());
			state.Source().Advance();
			ExecutionSupport.Assert(mCommands.ContainsKey(key), string.Concat("Extended command \"", key, "\" unknown"));
			string ctx = String.Empty;
			while (state.Source().More() && !Applicable(state)) {
				ctx = ctx + state.Source().Current();
				state.Source().Advance();
			}
			ExecutionSupport.Emit(() => string.Format("Extended command created: {0}", key));
			state.Source().Advance();
			return new ExtendedFalseCommand(mCommands[key], key, ctx);
		}
	}
}
