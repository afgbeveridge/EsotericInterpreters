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

namespace com.complexomnibus.esoteric.interpreter.abstractions {

	public enum InterpreterResult { InFlight, Complete, BreakpointReached }

	public interface IInterpreter<out TSourceType, out TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() { 
			InterpreterState State { get; }
			InterpreterResult Execute();
			BaseObject Gather();
			bool StepMode { get; set; }
	}

	public class Interpreter<TSourceType, TExeType> : IInterpreter<TSourceType, TExeType> where TSourceType : SourceCode, new() where TExeType : BaseInterpreterStack, new() {

		private const string EOLConfiguration = "retainSourceEOL";
		private const string StandardEntryPoint = "Program";
		private List<ITrivialInterpreterBase<TSourceType, TExeType>> mInterpreters = new List<ITrivialInterpreterBase<TSourceType, TExeType>>();

		public event EventHandler<InterpreterEventArgs<TSourceType, TExeType>> InterpreterEvent;

		public Interpreter(Assembly ass, string baseConfigName = null) { 
			DetectInterpreters(ass);
			State = new InterpreterState().Establish<TSourceType, TExeType>();
		}

		public void Accept(string fileName) {
			ExecutionSupport.Assert(File.Exists(fileName), string.Format("File {0} does not exist", fileName));
			bool retainEOL = Configuration.ConfigurationFor<bool>(EOLConfiguration, true);
			State.GetSource<TSourceType>().Content = File.ReadAllLines(fileName).Select(s => String.Concat(s, retainEOL ? Environment.NewLine : String.Empty)).ToList();
		}

		public InterpreterState State { get; protected set; }

		public InterpreterResult Execute() {
			InterpreterResult result = InterpreterResult.Complete;
			try {
				while (Step() && result == InterpreterResult.Complete) {
					if (BreakpointDetectors != null && BreakpointDetectors.Any(f => f(State)))
						result = InterpreterResult.BreakpointReached;
				}
			}
			catch (Exception) {
				if (InterpreterEvent != null) 
					InterpreterEvent(this, new InterpreterEventArgs<TSourceType, TExeType> { ActiveInterpreter = this, ErrorState = true });
				throw;
			}
			return result;
		}

		public BaseObject Gather() {
			ITrivialInterpreterBase<TSourceType, TExeType> interp = mInterpreters.FirstOrDefault(tib => tib.Applicable(State));
			ExecutionSupport.AssertNotNull(interp, string.Format("No interpreter usable; offending character {0}", SourceCode.Current()));
			interp.Interpreter = this;
			return interp.Gather(State);
		}

		public bool Step() {
			bool possible = State.GetSource<TSourceType>().More();
			if (possible)
				Gather().Apply(State);
			return possible;
		}

		public bool StepMode { get; set; }

		public Interpreter<TSourceType, TExeType> Register(EventHandler<InterpreterEventArgs<TSourceType, TExeType>> handler) {
			InterpreterEvent += handler;
			return this;
		}

		public IEnumerable<Func<InterpreterState, bool>> BreakpointDetectors { get; set; }

		private TSourceType SourceCode { get { return State.GetSource<TSourceType>(); } }

		private void DetectInterpreters(Assembly ass) {
			mInterpreters.AddRange(ass.GetTypes().
				Where(t => t.GetInterface(typeof(ITrivialInterpreterBase<TSourceType, TExeType>).Name) != null && !t.IsAbstract).Select(t => Activator.CreateInstance(t) as ITrivialInterpreterBase<TSourceType, TExeType>));
		}

		private void AppendGeneralInterpreters() { 
		}
	}

	public class InterpreterEventArgs<TSourceType, TExeType> : EventArgs
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {
		public Interpreter<TSourceType, TExeType> ActiveInterpreter { get; set; }
		public bool ErrorState { get; set; }
	}

	public interface ITrivialInterpreterBase<TSourceType, TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {
		
		bool Applicable(InterpreterState state);

		BaseObject Gather(InterpreterState state);

		IInterpreter<TSourceType, TExeType> Interpreter { get; set; }
	}

	public abstract class TrivialInterpreterBase<TSourceType, TExeType> : ITrivialInterpreterBase<TSourceType, TExeType>
		where TSourceType : SourceCode, new()
		where TExeType : BaseInterpreterStack, new() {

		public abstract bool Applicable(InterpreterState state);

		public abstract BaseObject Gather(InterpreterState state);

		public IInterpreter<TSourceType, TExeType> Interpreter { get; set; }
		
	}

	public class BreakpointEventArgs : EventArgs {
		public InterpreterState State{ get; set; }
	}

}
