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

namespace com.complexomnibus.esoteric.interpreter.abstractions {

	#region Base and canonical objects

	public abstract class BaseObject   {

		public void Apply(InterpreterState state) {
			Interpret(state);
			state.BaseExecutionEnvironment.Dump("Post-execution: " + ToString() + Environment.NewLine);
		}

		public virtual BaseObject Accept(string src) {
			return this;
		}

		public TObject As<TObject>() where TObject : BaseObject {
			return (TObject)this;
		}

		protected virtual void Interpret(InterpreterState state) {
			state.BaseExecutionEnvironment.Push(this);
		}

		public virtual object Clone() {
			return Activator.CreateInstance(GetType()) as BaseObject;
		}
	}

	public class NullObject : BaseObject {

		private static readonly Lazy<NullObject> mInstance = new Lazy<NullObject>(() => new NullObject());

		private NullObject() { }

		public static NullObject Instance { get { return mInstance.Value; } }

		protected override void Interpret(InterpreterState state) {
		}
	}

	public class CanonicalNumber : BaseObject {

		private const string FalseValue = "falseValue";
		private const string TrueValue = "trueValue";
		protected static readonly int mFalse = Configuration.ConfigurationFor<int>(FalseValue, 0);
		protected static readonly int mTrue = Configuration.ConfigurationFor<int>(TrueValue, -1);

		public CanonicalNumber() : this(0) {
		}

		public CanonicalNumber(string val) : this(int.Parse(val)) {
		}

		public CanonicalNumber(int num) {
			Value = num;
		}

		public override BaseObject Accept(string src) {
			Value = int.Parse(src);
			return this;
		}

		public int Value { get; set; }

		public CanonicalNumber Invert() {
			return new CanonicalNumber(Value * -1);
		}

		public override object Clone() {
			return new CanonicalNumber(Value);
		}

		public override bool Equals(object obj) {
			return obj is CanonicalNumber ? ((CanonicalNumber)obj).Value == Value : false;
		}

		public override int GetHashCode() {
			return Value;
		}

		public override string ToString() {
			return GetType().Name + " == " + Value;
		}

		public static CanonicalNumber operator ==(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value == rhs.Value);
		}

		public static CanonicalNumber operator !=(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value != rhs.Value);
		}

		public static CanonicalNumber operator <(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value < rhs.Value);
		}

		public static CanonicalNumber operator <=(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value <= rhs.Value);
		}

		public static CanonicalNumber operator >=(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(!(lhs < rhs));
		}

		public static CanonicalNumber operator >(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value > rhs.Value);
		}

		public static CanonicalNumber operator +(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value + rhs.Value);
		}

		public static CanonicalNumber operator -(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value - rhs.Value);
		}

		public static CanonicalNumber operator *(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value * rhs.Value);
		}

		public static CanonicalNumber operator /(CanonicalNumber lhs, CanonicalNumber rhs) {
			double res = lhs.Value / rhs.Value;
			return new CanonicalBoolean((int)(res < 0 ? Math.Ceiling(res) : Math.Floor(res)));
		}

		public static CanonicalNumber operator &(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value & rhs.Value);
		}

		public static CanonicalNumber operator |(CanonicalNumber lhs, CanonicalNumber rhs) {
			return new CanonicalBoolean(lhs.Value | rhs.Value);
		}

		public CanonicalBoolean Negate() {
			return new CanonicalBoolean(Value == mFalse);
		}

		public static implicit operator bool(CanonicalNumber obj) {
			return obj.Value != mFalse;
		}

		public static bool operator true(CanonicalNumber obj) {
			return obj.Value != mFalse;
		}

		public static bool operator false(CanonicalNumber obj) {
			return obj.Value == mFalse;
		}

	}

	public class CanonicalBoolean : CanonicalNumber {

		public CanonicalBoolean(bool val)
			: this(val ? mTrue : mFalse) {
		}

		public CanonicalBoolean(int num)
			: base(num) {
		}

		public override string ToString() {
			return GetType().Name + " == " + Value + "(" + (Value != mFalse) + ")";
		}

		public override object Clone() {
			return new CanonicalBoolean(Value);
		}

		public static CanonicalBoolean True { get { return new CanonicalBoolean(true); } }

		public static CanonicalBoolean False { get { return new CanonicalBoolean(false); } }

		public static implicit operator bool(CanonicalBoolean obj) {
			return obj.Value != mFalse;
		}

	}

	public class CanonicalString : BaseObject {

		public CanonicalString() { 
		}

		public CanonicalString(string src) {
			Source = src;
		}

		public override BaseObject Accept(string src) {
			Source = src;
			return this;
		}

		private string Source { get; set; }

		public override string ToString() {
			return Source;
		}
		
	}

	#endregion

	#region Command commands


	public class ActionCommand<TStack> : BaseCommand<Action<InterpreterState, SourceCode, TStack>> where TStack : BaseInterpreterStack {

		public ActionCommand(Action<InterpreterState, SourceCode, TStack> cmd, string keyWord)
			: base(cmd, keyWord) {
				ExecutionContext = new Queue<BaseObject>();
		}

		public Queue<BaseObject> ExecutionContext { get; private set; }

		protected override void Interpret(InterpreterState state) {
			foreach (BaseObject obj in ExecutionContext) state.Stack<TStack>().Push(obj);
			Command(state, state.GetSource<SourceCode>(), state.GetExecutionEnvironment<TStack>());
			Record();
		}

		public override object Clone() {
			return new ActionCommand<TStack>(Command, KeyWord);
		}

	}

	public static class CommonCommands {

		public static Action<InterpreterState, TSourceType, TExeType> BinaryAddition<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() + stack.Pop<CanonicalNumber>());
		}

		public static Action<InterpreterState, TSourceType, TExeType> BinarySubtraction<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>().Invert() + stack.Pop<CanonicalNumber>());
		}
		
		public static Action<InterpreterState, TSourceType, TExeType> BinaryMultiplication<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() * stack.Pop<CanonicalNumber>());
		}

		public static Action<InterpreterState, TSourceType, TExeType> Division<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => {
				CanonicalNumber divisor = stack.Pop<CanonicalNumber>();
				stack.Push(stack.Pop<CanonicalNumber>() / divisor);
			};
		}

		public static Action<InterpreterState, TSourceType, TExeType> LogicalAnd<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() & stack.Pop<CanonicalNumber>());
		}

		public static Action<InterpreterState, TSourceType, TExeType> LogicalOr<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() | stack.Pop<CanonicalNumber>());
		}

		public static Action<InterpreterState, TSourceType, TExeType> Equality<TSourceType, TExeType>() where TSourceType : SourceCode where TExeType : BaseInterpreterStack {
			return (state, source, stack) => stack.Push(stack.Pop<CanonicalNumber>() == stack.Pop<CanonicalNumber>());
		}

		public static Action<InterpreterState, TSourceType, TExeType> OutputValueFromStack<TSourceType, TExeType>()
			where TSourceType : SourceCode
			where TExeType : BaseInterpreterStack {
			return (state, source, stack) => Console.Write(stack.Pop<CanonicalNumber>().Value);
		}

		public static Action<InterpreterState, TSourceType, TExeType> OutputCharacterFromStack<TSourceType, TExeType>()
			where TSourceType : SourceCode
			where TExeType : BaseInterpreterStack {
			return (state, source, stack) => Console.Write(new String(Convert.ToChar(stack.Pop<CanonicalNumber>().Value), 1));
		}

		public static Action<InterpreterState, TSourceType, TExeType> ReadValueAndPush<TSourceType, TExeType>(bool readLine = false)
			where TSourceType : SourceCode
			where TExeType : BaseInterpreterStack {
				return (state, source, stack) => stack.Push(readLine ? new CanonicalNumber(Console.ReadLine()) : new CanonicalNumber(Console.Read()));
		}

	}

	#endregion

	public class BaseCommand<T> : BaseObject {

		public BaseCommand(T cmd, string keyWord) {
			Command = cmd;
			KeyWord = keyWord;
		}

		public String KeyWord { get; private set; }

		public T Command { get;  set; }

		protected void Record() {
			Statistics.Increment(KeyWord);
		}

		public override string ToString() {
			return GetType().Name + " - command " + KeyWord;
		}

	}

	public class PropertyBasedExecutionEnvironment : BaseInterpreterStack {

		public Func<PropertyBasedExecutionEnvironment, BaseObject> OnUnknownKey { get; set; }

		public BaseObject this[string key] {
			get {
				ExecutionSupport.Emit(() => string.Format("Getting variable: {0}", key));
				return mVariables.ContainsKey(key) ? mVariables[key] : (OnUnknownKey != null ? mVariables[key] = OnUnknownKey(this) : null);
			}
			set {
				ExecutionSupport.Emit(() => string.Format("Set variable: {0} == {1}", key, value));
				mVariables[key] = value;
			}
		}

		private Dictionary<string, BaseObject> mVariables = new Dictionary<string, BaseObject>();

        public void Reset() {
			ScratchPad = new Dictionary<string, object>();
			mVariables = new Dictionary<string, BaseObject>();
		}

        public PropertyBasedExecutionEnvironment Clone() {
            return new PropertyBasedExecutionEnvironment {
                OnUnknownKey = OnUnknownKey,
                ScratchPad = ScratchPad,
                mVariables = mVariables
            };
        }
    }

}
