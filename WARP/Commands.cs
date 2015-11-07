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

	internal abstract class WARPCommand {

		internal abstract void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack);

		internal static dynamic PropertyNameAndExpression(BaseInterpreterStack stack) {
			dynamic result = new ExpandoObject();
			result.PropertyName = stack.Pop<WARPObject>().AsString();
			result.Expression = stack.Pop<WARPObject>();
			return result;
		}

		internal static CMD Gather(InterpreterState state, string key, Builder bld) {
			ExecutionSupport.Emit(() => string.Format("Command created: {0}, Source Position {1}", key, state.Source().SourcePosition));
			state.Source().Advance();
			ActionCommand<PropertyBasedExecutionEnvironment> cmd = new ActionCommand<PropertyBasedExecutionEnvironment>(bld.Action, key);
			if (bld.Expression != null) {
				MatchAnalyzer a = bld.Examine(state, key);
				Action<WARPObject> pushIfNonEmpty = o => {
					if (o != null && !String.IsNullOrEmpty(o.AsString())) cmd.ExecutionContext.Enqueue(o);
				};
				pushIfNonEmpty(a.RealizedObject);
				pushIfNonEmpty(new WARPObject(a.PropertyName));
				ExecutionSupport.Emit(() => string.Concat("Command input parsed: ", a.Source));
			}
			return cmd;
		}

		protected static PropertyBasedExecutionEnvironment Environment(InterpreterState state) {
			return state.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>();
		}

	}

	internal class WARPJumpCommand : WARPCommand {

		internal static Regex LabelExpression;

		internal override void Execute(InterpreterState state, SourceCode source, BaseInterpreterStack stack) {
			dynamic result = PropertyNameAndExpression(stack);
			PropertyBasedExecutionEnvironment env = Environment(state);
			var val = (WARPObjectFactory.Instance.KnowsAbout(result.PropertyName) ?
						WARPObjectFactory.Instance.Fabricate(state, result.PropertyName) : ((WARPObject)env[result.PropertyName])).AsNumeric(0L);
			if (val > 0) {
				if (!env.HasScratchPadEntry(result.Expression.AsString()))
					FindAllLabels(state);
				ExecutionSupport.Assert(env.HasScratchPadEntry(result.Expression.AsString()), string.Concat("Unknown label: ", result.Expression.AsString()));
				source.SourcePosition = ((MutableTuple<int>)PropertyBasedExecutionEnvironment.ScratchPad[result.Expression.AsString()]).Copy();
			}
		}

		private void FindAllLabels(InterpreterState state) {
			bool more = true;
			SimpleSourceCode code = state.GetSource<SimpleSourceCode>();
			MutableTuple<int> pos = code.SourcePosition.Copy();
			char targetChar = Constants.KeyWords.Label.First();
			do {
				more = code.More();
				if (more) {
					code.Seek(targetChar);
					more = code.More() && code.Current() == Constants.KeyWords.Label;
					if (more)
						Gather(state, Constants.KeyWords.Label, Builder.Create((stat, src, st) => new WARPLabelCommand().Execute(stat, src, st), WARPLabelCommand.SimpleLabel)).Apply(state);
				}
			} while (more);
			code.SourcePosition = pos;
		}
	}

	internal class WARPLabelCommand : WARPCommand {

		internal static Regex SimpleLabel;

		internal override void Execute(InterpreterState state, SourceCode source, BaseInterpreterStack stack) {
			PropertyBasedExecutionEnvironment.ScratchPad[stack.Pop<WARPObject>().AsString()] = source.SourcePosition.Copy();
		}

	}

	internal class WARPMathCommand : WARPCommand {

		public WARPMathCommand() {
		}

		internal WARPMathCommand(Func<Int64, WARPObject, Int64> f) {
			Command = f;
		}

		internal override void Execute(InterpreterState state, SourceCode source, BaseInterpreterStack stack) {
			dynamic result = PropertyNameAndExpression(stack);
			bool inPopMode = result.PropertyName == Constants.KeyWords.Pop;
			var pbee = Environment(state);
			Int64 cur = inPopMode ? stack.Pop<WARPObject>().AsNumeric() : pbee[result.PropertyName].As<WARPObject>().AsNumeric();
			var obj = new WARPObject(FlexibleNumeralSystem.Encode(Command(cur, result.Expression), WARPObject.CurrentRadix));
			if (result.PropertyName == Constants.KeyWords.Pop)
				pbee.Push(obj);
			else
				pbee[result.PropertyName] = obj;
		}

		private Func<Int64, WARPObject, Int64> Command { get; set; }
	}

	internal class WARPDecisionCommand : WARPCommand {

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			var lhs = stack.Pop<WARPObject>().AsNumeric();
			var rhs = stack.Pop<WARPObject>().AsNumeric();
			dynamic res = CommandBuilder.KeyAndBuilder(state);
			CMD cmd = Gather(state, res.Key, res.Builder);
			ExecutionSupport.Emit(() => string.Concat("Comparison: ", lhs, " == ", rhs, "?"));
			if (lhs == rhs)
				cmd.Apply(state);
		}
	}

	internal class WARPComparisonCommand : WARPCommand {

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			var obj = stack.Pop<WARPObject>();
			PropertyBasedExecutionEnvironment env = Environment(state);
			if (!env.HasScratchPadEntry(Constants.KeyWords.Comparison)) {
				stack.Push(obj);
				PropertyBasedExecutionEnvironment.ScratchPad[Constants.KeyWords.Comparison] = String.Empty;
			}
			else {
				var lhs = stack.Pop<WARPObject>();
				PropertyBasedExecutionEnvironment.ScratchPad.Remove(Constants.KeyWords.Comparison);
				var bothNumeric = lhs.IsNumeric && obj.IsNumeric;
				Func<int> cmp = () => {
					var f = lhs.AsNumeric(); var s = obj.AsNumeric();
					return (f < s ? -1 : (f > s ? 1 : 0));
				};
				stack.Push(new WARPObject(bothNumeric ? cmp() : string.Compare(lhs.AsString(), obj.AsString())));
			}
		}
	}

	internal class WARPTreatmentCommand : WARPCommand {

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			var val = ((WARPObject)Environment(state)[stack.Pop<WARPObject>().AsString()]).AsString();
			state.AddExecutionEnvironment<PropertyBasedExecutionEnvironment>();
			val.Reverse().ToList().ForEach(c => Environment(state).Push(new WARPObject(new string(new[] { c }))));
		}
	}

	internal class WARPRASCommand : WARPCommand {

		public WARPRASCommand() { }

		internal WARPRASCommand(Action<RandomAccessStack<WARPObject>, BaseInterpreterStack> action) {
			Action = action;
		}

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			RandomAccessStack<WARPObject> st = PropertyBasedExecutionEnvironment.ScratchPad[Constants.RASName] as RandomAccessStack<WARPObject>;
			st.Set((int)stack.Pop<WARPObject>().AsNumeric());
			Action(st, stack);
		}

		private Action<RandomAccessStack<WARPObject>, BaseInterpreterStack> Action { get; set; }
	}

	internal class WARPInputCommand : WARPCommand {

		internal static readonly IEnumerable<string> Options = new[] { "l", "c" };

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			var style = stack.Pop<WARPObject>().AsString();
			ExecutionSupport.Assert(Options.Contains(style), string.Concat("Invalid argument for ',' - ", style));
			stack.Push(style == "l" ? new WARPObject(Console.ReadLine() ?? "0") : new WARPObject(Convert.ToChar(Console.Read())));
		}
	}

	internal class WARPAssignmentCommand : WARPCommand {

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			dynamic result = PropertyNameAndExpression(stack);
			if (result.PropertyName == Constants.KeyWords.Pop)
				Environment(state).Push(result.Expression);
			else
				Environment(state)[result.PropertyName] = result.Expression;
		}
	}

	internal class WARPPopPushCommand : WARPCommand {

		internal override void Execute(InterpreterState state, SourceCode code, BaseInterpreterStack stack) {
			var obj = stack.Pop();
			stack.Pop();
			stack.Push(obj);
		}
	}

}