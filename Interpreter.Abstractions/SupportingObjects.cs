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
using System.Configuration;

namespace com.complexomnibus.esoteric.interpreter.abstractions {
	
	public static class StackingTimer {

		private static Stack<Tuple<string, DateTime>> Times { get; set; }

		static StackingTimer() {
			Times = new Stack<Tuple<string, DateTime>>();
		}

		public static void Start(string timerName = null) {
			Times.Push(new Tuple<string, DateTime>(timerName ?? "Unnamed task", DateTime.Now));
		}

		public static TimeSpan Stop(bool display = true) {
			Tuple<string, DateTime> tos = Times.Pop();
			TimeSpan span = DateTime.Now - tos.Item2;
			if (display)
				Console.WriteLine(tos.Item1 + " completes; execution time: " + span.ToString("c"));
			return span;
		}
	
	}

	public static class Statistics {

		private static Dictionary<string, double> mStats = new Dictionary<string, double>();

		public static void Increment(string key, int addend = 1) {
			if (!mStats.ContainsKey(key))
				mStats[key] = 0;
			mStats[key] += addend;
		}

		public static void Reset() {
			mStats = new Dictionary<string, double>();
		} 

		public static void Dump() {
			Console.WriteLine(string.Concat("Statistics", Environment.NewLine));
			mStats.ToList().ForEach(kvp => Console.WriteLine(string.Concat(kvp.Key, " == ", kvp.Value))); 
		} 

	}

	public static class ExecutionSupport {

		private static StreamWriter Writer;

		public static void Prepare(string name) {
			if (DebugMode) {
				UnPrepare();
				Writer = new StreamWriter(File.Open(string.Concat("debug.", name, ".txt"), FileMode.Append));
			}
		}

		public static void UnPrepare() {
			if (Writer != null) { Writer.Close(); Writer = null; }
		}

		public static void Assert(bool condition, string msg, Action<string> preException = null) {
			if (!condition) {
				if (preException != null)
					preException(msg);
				throw new ApplicationException(msg);
			}
		}

		public static T AssertNotNull<T>(T obj, string msg, Action<string> preException = null) where T : class {
			return AssertNotNull(obj, val => msg, preException);
		}

		public static T AssertNotNull<T>(T obj, Func<T, string> f, Action<string> preException = null) where T : class {
			Assert(obj != null, f(obj), preException);
			return obj;
		}

		public static bool DebugMode { get; set; }

		public static void Emit(Func<string> msg) {
			if (DebugMode && Writer != null)
				Writer.WriteLine(msg());
		}

		public static void EnableStatistics() {
		}
	}

	public static class ConsoleHighlighter {

		private static Stack<ConsoleColor> mStackedColors = new Stack<ConsoleColor>();

		public static void SetColor(ConsoleColor textColor, ConsoleColor? backColor = default(ConsoleColor)) {
			mStackedColors.Push(Console.ForegroundColor);
			mStackedColors.Push(Console.BackgroundColor);
			Console.ForegroundColor = textColor;
			Console.BackgroundColor = backColor ?? Console.BackgroundColor;
		}

		public static void RestorePreviousColor() {
			Console.BackgroundColor = mStackedColors.Pop();
			Console.ForegroundColor = mStackedColors.Pop();
		}

		public static void Display(string message, ConsoleColor textColor = ConsoleColor.Red, ConsoleColor backColor = ConsoleColor.Black, bool includeNewLine = true) {
			SetColor(textColor, backColor);
			if (includeNewLine) Console.WriteLine(message);
			else Console.Write(message);
			RestorePreviousColor();
		}

	}

	#region configuration support

	public static class Configuration {

		private static readonly Dictionary<Type, Func<string, object>> mConverters = new Dictionary<Type, Func<string, object>> { 
			{ typeof(bool), (s) => Convert.ToBoolean(s) },
			{ typeof(int), (s) => Convert.ToInt32(s) }
		};

		public static T ConfigurationFor<T>(string configName, T def = default(T)) {
			var val = ConfigurationManager.AppSettings[configName];
			return String.IsNullOrEmpty(val) ? def : (T)GetConverter<T>()(val);
		}

		private static Func<string, object> GetConverter<T>() {
			Type targetType = typeof(T);
			return mConverters.ContainsKey(targetType) ? mConverters[targetType] : s => s; 
		}
	} 

	#endregion
}
