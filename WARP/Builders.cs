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
using System.Dynamic;
using CMD = com.complexomnibus.esoteric.interpreter.abstractions.ActionCommand<com.complexomnibus.esoteric.interpreter.abstractions.PropertyBasedExecutionEnvironment>;

namespace WARP {

	internal class Constants {
		internal class KeyWords {
			internal const string Comparison = ":";
			internal const string Pop = "!";
			internal const string Addition = ">";
			internal const string Subtraction = "<";
			internal const string Multiplication = "&";
			internal const string Division = "$";
			internal const string Modulo = "#";
			internal const string Jump = "^";
			internal const string Label = "@";
		}
		internal const string RASName = "__RAS";
	}

	internal static class CommandFactory {

		private static Dictionary<Tuple<Type, string>, object> Cache = new Dictionary<Tuple<Type, string>, object>();

		internal static T Get<T>(Func<T> creator = null, string qualifier = null) where T : WARPCommand, new() { 
			Tuple<Type, string> key = Tuple.Create<Type, string>(typeof(T), qualifier ?? String.Empty);
			if (!Cache.ContainsKey(key)) 
				Cache[key] = (creator ?? (Func<T>)(() => new T()))();
			return Cache[key] as T;
		}

	}

	public class CommandBuilder : TrivialInterpreterBase<SimpleSourceCode, PropertyBasedExecutionEnvironment> {

		private static Dictionary<string, Builder> mCommands = new Dictionary<string, Builder>();
		private static Regex Expression;
		private static Regex BoundVariableExpression;
		private static Regex BoundVariableOrStackExpression;
		private static Regex ObjectReference;
		
		internal static void Initialize(SharedObjects.Esoterica.IOWrapper wrapper) {
			CreateExpressions();
            WARPInputCommand.InteractionWrapper = wrapper;
            mCommands["="] = Builder.Create((state, source, stack) => Get<WARPAssignmentCommand>().Execute(state, source, stack), BoundVariableExpression);
			mCommands[Constants.KeyWords.Addition] = Builder.Create((state, source, stack) => CreateMathProcessor((cur, expr) => cur + expr.AsNumeric(), Constants.KeyWords.Addition).Execute(state, source, stack), BoundVariableOrStackExpression);
			mCommands[Constants.KeyWords.Subtraction] = Builder.Create((state, source, stack) => CreateMathProcessor((cur, expr) => cur - expr.AsNumeric(), Constants.KeyWords.Subtraction).Execute(state, source, stack), BoundVariableOrStackExpression);
			mCommands[Constants.KeyWords.Multiplication] = Builder.Create((state, source, stack) => CreateMathProcessor((cur, expr) => cur * expr.AsNumeric(1L), Constants.KeyWords.Multiplication).Execute(state, source, stack), BoundVariableOrStackExpression);
			mCommands[Constants.KeyWords.Division] = Builder.Create((state, source, stack) => CreateMathProcessor((cur, expr) => cur / expr.AsNumeric(1L), Constants.KeyWords.Division).Execute(state, source, stack), BoundVariableOrStackExpression);
			mCommands[Constants.KeyWords.Modulo] = Builder.Create((state, source, stack) => CreateMathProcessor((cur, expr) => cur % expr.AsNumeric(1L), Constants.KeyWords.Modulo).Execute(state, source, stack), BoundVariableOrStackExpression);
			mCommands[Constants.KeyWords.Pop] = Builder.Create((state, source, stack) => stack.Pop());
			mCommands["*"] = Builder.Create((state, source, stack) => stack.Push(stack.Pop<WARPObject>()), Expression);
			mCommands[";"] = Builder.Create((state, source, stack) => stack.Duplicate());
			mCommands["+"] = Builder.Create((state, source, stack) => WARPObject.CurrentRadix = Convert.ToInt32(stack.Pop<WARPObject>().AsNumeric()),
				RegexBuilder.New().StartCaptureGroup("expr").AddCharacterClass("0-9A-Z").OneOrMore().EndCaptureGroup().EndMatching().ToRegex());
			mCommands["]"] = Builder.Create((state, source, stack) => Get<WARPPopPushCommand>().Execute(state, source, stack), Expression);
			mCommands[")"] = Builder.Create((state, source, stack) => wrapper.Write(stack.Pop<WARPObject>().AsString().Replace("\\n", System.Environment.NewLine)), Expression);
			mCommands["("] = Builder.Create((state, source, stack) => wrapper.Write(stack.Pop<WARPObject>().AsCharacter()), Expression);
			mCommands[","] = Builder.Create((state, source, stack) => Get<WARPInputCommand>().Execute(state, source, stack),
				RegexBuilder.New().StartsWith().StartCaptureGroup("var").OneFrom(WARPInputCommand.Options).EndCaptureGroup().EndMatching().ToRegex());
			mCommands["|"] = Builder.Create((state, source, stack) => state.PopExecutionEnvironment<PropertyBasedExecutionEnvironment>());
			mCommands["'"] = Builder.Create((state, source, stack) => state.RotateExecutionEnvironment<PropertyBasedExecutionEnvironment>());
			mCommands[Constants.KeyWords.Label] = Builder.Create((state, source, stack) => Get<WARPLabelCommand>().Execute(state, source, stack), WARPLabelCommand.SimpleLabel);
			mCommands[Constants.KeyWords.Jump] = Builder.Create((state, source, stack) => Get<WARPJumpCommand>().Execute(state, source, stack), WARPJumpCommand.LabelExpression);
			mCommands["%"] = Builder.Create((state, source, stack) => Get<WARPTreatmentCommand>().Execute(state, source, stack), ObjectReference);
			mCommands["?"] = Builder.Create((state, source, stack) => Get<WARPDecisionCommand>().Execute(state, source, stack), Expression);
			// :exp1:exp2 exp1 == exp2, push 0, exp1 < exp2 push -1, else push 1
			mCommands[Constants.KeyWords.Comparison] = Builder.Create((state, source, stack) => Get<WARPComparisonCommand>().Execute(state, source, stack), Expression);
			// Treat an object as an array, take an element and push
			mCommands["{"] = Builder.Create((state, source, stack) => 
				CommandFactory.Get<WARPRASCommand>(() => new WARPRASCommand((st, stk) => stk.Push(st.CurrentCell)), "_").Execute(state, source, stack)
			, RegexBuilder.New().StartsWith().Include("expression").EndMatching().ToRegex());
			// Treat an object as an array, update the object noted at the index given with a value popped from the stack
			mCommands["}"] = Builder.Create((state, source, stack) => 
				CommandFactory.Get<WARPRASCommand>(() => new WARPRASCommand((st, stk) => st.CurrentCell = stk.Pop<WARPObject>()), ":").Execute(state, source, stack), 
				RegexBuilder.New().StartsWith().Include("expression").EndMatching().ToRegex());
		}

		private static PropertyBasedExecutionEnvironment Environment(InterpreterState state) {
			return state.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>();
		}

		private static T Get<T>() where T : WARPCommand, new() {
			return CommandFactory.Get<T>();
		}

		private static WARPMathCommand CreateMathProcessor(Func<long, WARPObject, long> f, string qualifier) { 
			return CommandFactory.Get<WARPMathCommand>(() => new WARPMathCommand(f), qualifier);
		}

		private static void CreateExpressions() {
			RegexBuilder.New()
				.AddCharacterClass("a-z")
				.BoundedRepetition(2)
				.RememberAs("objectReference");
			RegexBuilder.New()
				.Optional("-")
				.AddCharacterClass("0-9A-Z").OneOrMore()
				.RememberAs("numeric");
			RegexBuilder.New()
				.StartCaptureGroup("var")
				.Include("objectReference")
				.EndCaptureGroup()
				.RememberAs("objectCapture");
			RegexBuilder.New()
				.StartCaptureGroup("expr")
				.Include("numeric")
				.Or
				.Include("objectReference")
				.Or
				.Literal("!")
				.Or
				.Literal("~")
				.Or
				.Literal("_")
				.Or
				.Literal("\"[^\"]*\"")
				.EndCaptureGroup()
				.RememberAs("expression");
			ObjectReference = RegexBuilder.New().StartsWith().Include("objectCapture").EndMatching().ToRegex();
			RegexBuilder.New().StartCaptureGroup("expr").AddCharacterClass("a-z").OneOrMore().EndCaptureGroup().RememberAs("label");
			WARPLabelCommand.SimpleLabel = RegexBuilder.New().StartsWith().Include("label").EndMatching().ToRegex();
			Expression = RegexBuilder.New().StartsWith().Include("expression").EndMatching().ToRegex();
			BoundVariableExpression = RegexBuilder.New().StartsWith().Include("objectCapture").Include("expression").EndMatching().ToRegex();
			BoundVariableOrStackExpression = RegexBuilder.New().StartsWith().StartCaptureGroup("var").AddCharacterClass("a-z")
				.BoundedRepetition(2).Or.Literal("!").EndCaptureGroup().Include("expression").EndMatching().ToRegex();
			WARPJumpCommand.LabelExpression = RegexBuilder.New()
								.StartsWith().StartCaptureGroup("var").Include("objectReference").Or.Literal("!").Or.Literal("_").Or.Literal("\\.")
								.EndCaptureGroup().Include("label").EndMatching().ToRegex();
		}

		public override bool Applicable(InterpreterState state) {
			return mCommands.ContainsKey(state.Source().Current());
		}

		public override BaseObject Gather(InterpreterState state) {
			dynamic res = KeyAndBuilder(state, false);
			return WARPCommand.Gather(state, res.Key, res.Builder);
		}

		internal static dynamic KeyAndBuilder(InterpreterState state, bool advance = true) {
			dynamic result = new ExpandoObject();
			if (advance) state.Source().Advance();
			result.Key = state.Source().Current();
			result.Builder = mCommands.ContainsKey(result.Key) ? mCommands[result.Key] : Builder.Null;
			return result;
		}

	}

	internal class Builder {

			private static Builder NullBuilder = new Builder { Action = (s, c, e) => { } };

			internal Regex Expression { get; set; }
			
			internal Action<InterpreterState, SourceCode, BaseInterpreterStack> Action { get; set; }
			
			internal static Builder Create(Action<InterpreterState, SourceCode, BaseInterpreterStack> action, Regex expr = null) { 
				return new Builder { Expression = expr, Action = action };
			}

			internal static Builder Null {
				get {
					return NullBuilder;
				}
			}

			internal static Builder Inactive(Regex expr) { return new Builder { Expression = expr }; }

			internal MatchAnalyzer Examine(InterpreterState state, string key) {
				string input = state.Source().Current();
				bool hadEnough = false;
				while (state.Source().More() && !hadEnough) {
					bool currentIsMatch = Expression.Match(input).Success;
					bool followingIsMatch = Expression.Match(string.Concat(input, state.Source().Peek())).Success;
					if (!currentIsMatch || followingIsMatch) {
						if (state.Source().Advance()) input = string.Concat(input, state.Source().Current());
					}
					hadEnough = currentIsMatch && !followingIsMatch;
				}
				if (state.Source().More()) state.Source().Advance();
				return new MatchAnalyzer(input).Absorb(state, key, Expression);
			}
		}

	// TODO: Copied and pasted from the FalseInterpreter - demonstrates that the current simple discovery scheme based on reflection is not sufficient
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

}