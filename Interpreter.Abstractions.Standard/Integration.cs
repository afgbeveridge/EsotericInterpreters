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
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace com.complexomnibus.esoteric.interpreter.abstractions {

	public class CommandLineExecutor<TSourceType, TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {

		private readonly Dictionary<string, OptionBehaviour<TSourceType, TExeType>> mOptionsAndConstraints = new Dictionary<string, OptionBehaviour<TSourceType, TExeType>> { 
			{ "-s", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-s  activates step mode",
 						Effect = (cle, interp, args) => interp.StepMode = true
					} 
			},
			{ "-d", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-d  activate debug file generation",
						Effect = (cle, interp, args) => ExecutionSupport.DebugMode = true
					}
			},
			{ "-t", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-t  show stack in step mode",
						Effect = (cle, interp, args) => cle.ShowStackWhenStepping = true
					}
			},
			{ "-tr", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-tr  show deep stack in step mode",
						Effect = (cle, interp, args) => { cle.ShowStackWhenStepping = true; cle.ShowStackRecursivelyWhenStepping = StackDumpDirective.Recursive; }
					}
			},
			{ "-bps", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.Binary, 
						Help = "-bp break at source position x, y",
						Effect = (cle, interp, args) => { interp.BreakpointDetectors = new List<Func<InterpreterState, bool>> {  
								s => s.BaseSourceCode.SourcePosition.X == int.Parse(args.First()) && s.BaseSourceCode.SourcePosition.Y == int.Parse(args[1]) }; 
						},
						Transient = true
					}
			},
			{ "-esc", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-esc  enable source code seek cache",
						Effect = (cle, interp, args) => interp.State.BaseSourceCode.CachingEnabled = true
					}
			},
			{ "-n", new OptionBehaviour<TSourceType, TExeType> { 
						ArgumentCount = OptionCount.None, 
						Help = "-n  suppress interpreter startup message",
						Effect = (cle, interp, args) => cle.SuppressStartupMessage = true
					}
			}
		};

		public void Execute(Assembly ass, string startupMessage, string[] args, Action<Interpreter<TSourceType, TExeType>> preExecution = null) {
			try {
				ExecutionSupport.Assert(args.Any(), string.Concat("Usage: .exe [options] {source file}+", OptionsHelp()));
				if (!ConsoleEx.IsInputRedirected) {
					Console.TreatControlCAsInput = false;
					Console.CancelKeyPress += (sender, e) => {
						Console.WriteLine("Execution aborted by user....");
					};
				}
				// Use a simple for loop as options may cause 'argument' jumping when being gathered
				for (int i = 0; i < args.Length; i++) {
					string cur = args[i];
					if (IsOption(cur)) 
						i = ExamineOption(cur, args, i);
					else {
						Interpreter = new Interpreter<TSourceType, TExeType>(ass).Register(FormatInterpreterState);
						ApplyActiveOptions();
						if (i == 0 && !SuppressStartupMessage)
							Console.WriteLine(startupMessage);
						ExecutionSupport.Prepare(cur);
						if (preExecution != null)
							preExecution(Interpreter);
						Interpreter.Accept(cur);
						Process();
					}
				}
			}
			catch (Exception ex) {
				ConsoleHighlighter.Display(string.Concat("Exception during execution -> ", ex.Message), ConsoleColor.DarkYellow);
				Console.WriteLine(string.Concat("Stack trace: ", Environment.NewLine, ex.ToString()));
				if (Interpreter != null && Interpreter.State != null)
					Console.WriteLine(string.Concat("Current state: ", Environment.NewLine, this.Interpreter.State.BaseExecutionEnvironment.ToString(StackDumpDirective.Recursive)));
			}
			finally {
				ExecutionSupport.DebugMode = false;
				ExecutionSupport.UnPrepare();
			}
		}

		private bool SuppressStartupMessage { get; set; }

		private Interpreter<TSourceType, TExeType> Interpreter { get; set; }

		private void Process() {
			InterpreterResult result = InterpreterResult.InFlight;
			while (result != InterpreterResult.Complete) {
				if (Interpreter.StepMode)
					StepInto();
				else {
					result = Interpreter.Execute();
					if (result == InterpreterResult.BreakpointReached)
						Interpreter.StepMode = true;
				}
			}
		}

		private void StepInto() {
			Interpreter.State.GetSource<SourceCode>().SourceEvent += SourceEventSubscriber;
			Step();
		}

		// Has to be a method and not an anonymous subscriber as electing to "run" in step mode should unsubscribe, and unsubscription cannot be done reliably with anon delegates 
		private void SourceEventSubscriber(object sender, SourceCodeEventArgs<SourceCode> arguments) {
			MarkSource(arguments.Source);
			if (ShowStackWhenStepping)
				Console.WriteLine(Interpreter.State.BaseExecutionEnvironment.ToString(ShowStackRecursivelyWhenStepping));
		}

		private string OptionsHelp() {
			return string.Concat(Environment.NewLine, String.Join(Environment.NewLine, mOptionsAndConstraints.Select(kvp => kvp.Value.Help)));
		}

		private bool IsOption(string s) {
			return mOptionsAndConstraints.ContainsKey(s);
		}

		private int ExamineOption(string key, string[] args, int index) {
			var option = mOptionsAndConstraints[key];
			option.Active = !option.Active;
			ExecutionSupport.Assert(index + 1 + option.ArgumentCount < args.Length, string.Concat("Not enough arguments for ", key));
			if (option.ArgumentCount != OptionCount.None) 
				option.Arguments = args.ToList().GetRange(index + 1, option.ArgumentCount).ToArray();
			return index + option.ArgumentCount;
		}

		private void ApplyActiveOptions() {
			mOptionsAndConstraints.Where(kvp => kvp.Value.Active).ToList().ForEach(kvp => { 
				kvp.Value.Effect(this, Interpreter, kvp.Value.Arguments);
				kvp.Value.Active = !kvp.Value.Transient;
			});
		}

		private void Step() {
			while (Interpreter.Step() && Interact()) ;
			Interpreter.StepMode = false;
		}

		private bool ShowStackWhenStepping { get; set; }

		private StackDumpDirective ShowStackRecursivelyWhenStepping { get; set; }

		private bool Continue(bool removeBreakpoints) {
			Interpreter.State.GetSource<SourceCode>().SourceEvent -= SourceEventSubscriber; 
			Interpreter.StepMode = false;
			Interpreter.BreakpointDetectors = removeBreakpoints ? null : Interpreter.BreakpointDetectors;
			return false;
		}

		private void FormatInterpreterState(object sender, InterpreterEventArgs<TSourceType, TExeType> e) {
			if (e.ErrorState)
				Dump(e.ActiveInterpreter);
		}

		private void Dump(Interpreter<TSourceType, TExeType> current) {
			SourceCode state = current.State.GetSource<SourceCode>();
			ConsoleHighlighter.Display(Environment.NewLine + "Interpreter error" + Environment.NewLine + "Total source size: "); // + state.Content.Length);
			MarkSource(state);
		}

		private void MarkSource(SourceCode source) {
			int idx = 0;
			bool marked = false;
			while (!marked && idx < source.Content.Count) {
				if (idx != source.SourcePosition.Y) Console.WriteLine(source.Content[idx]);
				if (idx == source.SourcePosition.Y) {
					var line = source.Content[idx];
					Console.Write(line.Substring(0, source.SourcePosition.X));
					ConsoleHighlighter.Display(new string(new[] { line[source.SourcePosition.X] }), ConsoleColor.Yellow, ConsoleColor.DarkRed, false);
					Console.WriteLine(line.Substring(source.SourcePosition.X + 1));
					ConsoleHighlighter.Display(new string(' ', source.SourcePosition.X) + "^", ConsoleColor.Cyan);
					marked = true;
				}
				idx++;
			}
			Console.WriteLine(string.Concat("Source position: (", source.SourcePosition.X, ",", source.SourcePosition.Y, ")"));
		}

		// Primitive validation

		private static readonly string[] mSupportedDebugOptions = new [] { null, String.Empty, "c", "n" };

		private bool Interact() {
			Console.Write(string.Concat("[c] = continue (ignore breakpoints), [n] = execute to next breakpoint", Environment.NewLine, "[ENTER] to step..."));
			string cmd = null;
			do {
				String.IsNullOrEmpty(cmd).IfFalse(() => Console.WriteLine(string.Concat("? => Don't understand ", cmd)));
				cmd = (Console.ReadLine() ?? String.Empty).Trim();
			} while (!mSupportedDebugOptions.Contains(cmd));
			return string.IsNullOrEmpty(cmd) ? true : Continue(cmd == "c");
		}
	}

	internal static class OptionCount {
		internal const int None = 0;
		internal const int Unitary = 1;
		internal const int Binary = 2;
	}

	internal class OptionBehaviour<TSourceType, TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {


		internal int ArgumentCount { get; set; }

		internal string[] Arguments { get; set; }

		internal string Help { get; set; }

		internal Action<CommandLineExecutor<TSourceType, TExeType>, Interpreter<TSourceType, TExeType>, string[]> Effect { get; set; }

		internal bool Active { get; set; }

		internal bool Transient { get; set; }

		public override string ToString() {
			return Help + Environment.NewLine + ", argument count == " + ArgumentCount;
		}
	}

	public static class ConsoleEx {
		public static bool IsOutputRedirected {
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdout)); }
		}
		public static bool IsInputRedirected {
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdin)); }
		}
		public static bool IsErrorRedirected {
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stderr)); }
		}

		// P/Invoke:
		private enum FileType { Unknown, Disk, Char, Pipe };
		private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };
		[DllImport("kernel32.dll")]
		private static extern FileType GetFileType(IntPtr hdl);
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetStdHandle(StdHandle std);
	}
}
