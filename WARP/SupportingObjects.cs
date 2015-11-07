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
using System.Text.RegularExpressions;
using com.complexomnibus.esoteric.interpreter.abstractions;
using ANALFUNC = System.Func<com.complexomnibus.esoteric.interpreter.abstractions.InterpreterState, string, string>;

namespace WARP {

	internal class MatchAnalyzer {

		private static readonly List<Tuple<string, ANALFUNC>> AnalysisHelpers = new List<Tuple<string, Func<InterpreterState, string, string>>>();

		static MatchAnalyzer() {
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>(string.Concat("-", FlexibleNumeralSystem.CharList), (state, src) => src));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("@", (state, src) => src));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("^", (state, src) => src));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("\"", (state, src) => src.Substring(1, src.Length - 2)));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("!", (state, src) => state.Stack<BaseInterpreterStack>().Pop<WARPObject>().AsString()));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("abcdefghijklmnopqrstuvwxyz", (state, src) => {
				BaseObject b = state.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>()[src];
				return b == null ? String.Empty : ((WARPObject)b).AsString();
			}));
			AnalysisHelpers.Add(Tuple.Create<string, ANALFUNC>("~", (state, src) => String.Join(Environment.NewLine, state.Source().Content)));
		}

		internal MatchAnalyzer(string src) {
			Source = src;
		}

		internal string Source { get; set; }

		internal MatchAnalyzer Absorb(InterpreterState state, string cmd, Regex r) {
			var m = r.Match(Source);
			PropertyName = m.Groups["var"].Value;
			if (m.Groups["expr"].Success) {
				string expr = m.Groups["expr"].Value;
				Tuple<string, ANALFUNC> helper = AnalysisHelpers.FirstOrDefault(t => t.Item1 == cmd) ?? AnalysisHelpers.First(t => t.Item1.Contains(expr.Substring(0, 1)));
				RealizedObject = new WARPObject(helper.Item2(state, expr));
			}
			return this;
		}

		internal string PropertyName { get; private set; }

		internal WARPObject RealizedObject { get; private set; }

	}

	internal class WARPObject : BaseObject {

		internal static int CurrentRadix = FlexibleNumeralSystem.StandardRadix;

		public WARPObject()
			: this(0L) {
		}

		internal WARPObject(char src) : this(new string(new[] { src })) {
		}

		internal WARPObject(string src) {
			Source = src;
			Radix = CurrentRadix;
		}

		internal WARPObject(Int64 src)
			: this(src.ToString()) {
		}

		internal Int64 AsNumeric(Int64 defaultValue = 0L) {
			return FlexibleNumeralSystem.Decode(Source, Radix, defaultValue);
		}


		internal bool IsNumeric {
			get {
				return FlexibleNumeralSystem.CanParse(Source, Radix);
			}
		}

		internal string AsString() {
			return Source ?? String.Empty;
		}

		internal char AsCharacter() {
			return Convert.ToChar(AsNumeric());
		}

		public override object Clone() {
			return new WARPObject { Source = Source, Radix = Radix };
		}

		internal static WARPObject Mutate(string src) {
			return new WARPObject { Source = src, Radix = CurrentRadix };
		}

		public override string ToString() {
			return AsString();
		}

		private string Source { get; set; }

		private int Radix { get; set; }

	}

	internal class WARPObjectFactory {

		private static WARPObjectFactory mInstance = new WARPObjectFactory();
		private static Dictionary<string, Func<InterpreterState, WARPObject>> mHandlers = new Dictionary<string, Func<InterpreterState, WARPObject>>();

		static WARPObjectFactory() {
			mHandlers["."] = s => WARPObject.Mutate("1");
			mHandlers["_"] = s => WARPObject.Mutate(s.Stack<PropertyBasedExecutionEnvironment>().Size.ToString());
			mHandlers["~"] = s => WARPObject.Mutate(s.Source().Content.Sum(l => l.Length).ToString());
			mHandlers["!"] = s => WARPObject.Mutate(s.Stack<PropertyBasedExecutionEnvironment>().Pop<WARPObject>().AsString());
		}

		private WARPObjectFactory() { }

		internal static WARPObjectFactory Instance { get { return mInstance; } }

		internal bool KnowsAbout(string symbol) {
			return mHandlers.ContainsKey(symbol);
		}

		internal WARPObject Fabricate(InterpreterState state, string symbol) {
			return mHandlers[symbol](state);
		}

	}

}
