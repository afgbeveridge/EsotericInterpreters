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
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace com.complexomnibus.esoteric.interpreter.implementation.falseLanguage {

	internal class ExtendedFalseCommand : BaseCommand<Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment, string>> {

		internal ExtendedFalseCommand(Action<InterpreterState, SourceCode, PropertyBasedExecutionEnvironment, string> cmd, string keyWord, string context)
			: base(cmd, keyWord) {
			Context = context;
		}

		private string Context { get; set; }

		protected override void Interpret(InterpreterState state) {
			Command(state, state.GetSource<SourceCode>(), state.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>(), Context);
			Record();
		}

		public override string ToString() {
			return base.ToString() + ", context: " + KeyWord;
		}

		public override object Clone() {
			return new ExtendedFalseCommand(Command, KeyWord, Context);
		}

	}

	public class FalseString : BaseObject {

		internal FalseString(string val, bool comment) {
			Text = val;
			IsComment = comment;
		}

		private string Text { get; set; }

		private bool IsComment { get; set; }

		protected override void Interpret(InterpreterState state) {
			if (!IsComment)
				Console.WriteLine(Text);
		}

		public override string ToString() {
			return GetType().Name + " " + Text;
		}

		public override object Clone() {
			return new FalseString(Text, IsComment);
		}

	}

	public class FalseLambda : BaseObject {

		internal FalseLambda() {
			Children = new List<BaseObject>();
		}

		internal void AddCommand(BaseObject obj) {
			Children.Add(obj);
		}

		private bool Push { get; set; }

		private List<BaseObject> Children { get; set; }

		internal void Execute(InterpreterState state) {
			Children.ForEach(c => c.Apply(state));
		}

		public override string ToString() {
			return GetType().Name + Environment.NewLine + String.Join("Child->", Children.Select(c => c.ToString() + Environment.NewLine).ToArray());
		}

		public override object Clone() {
			return new FalseLambda { Children = new List<BaseObject>(this.Children) };
		}

	}

	public class FalseVariable : BaseObject {

		internal FalseVariable(string key) {
			Key = key;
		}

		internal string Key { get; set; }

		public override string ToString() {
			return GetType().Name + " - " + Key;
		}

		public override object Clone() {
			return new FalseVariable(Key);
		}

	}
}
