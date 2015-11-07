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

namespace com.complexomnibus.esoteric.interpreter.abstractions {

	#region Stack implementations

	public interface IStackInterferer {
		void PreStackObjectAccess(BaseInterpreterStack stack, int objectsRequested);
	}

	internal class NullInterferer : IStackInterferer {
		public void PreStackObjectAccess(BaseInterpreterStack stack, int objectsRequested) {
		}
	}

	public enum StackDumpDirective { Top, Recursive }

	public class BaseInterpreterStack {

		public BaseInterpreterStack() {
			State = new List<BaseObject>();
			Interferer = new NullInterferer();
		}

		public BaseInterpreterStack Duplicate() {
			Interferer.PreStackObjectAccess(this, 1);
			ExecutionSupport.Assert(State.Any(), "Attempt to duplicate with an empty state");
			State.Insert(0, State.First().Clone() as BaseObject);
			return this;
		}

		public IStackInterferer Interferer { get; set; }

		public int Size { get { return State.Count; } }

		public BaseInterpreterStack Push(BaseObject obj) {
			Statistics.Increment("Push");
			ExecutionSupport.AssertNotNull(obj, "Attempt to push null object");
			State.Insert(0, obj);
			ExecutionSupport.Emit(() => string.Format("Object pushed onto stack({0}): {1}", State.Count(), obj));
			return this;
		}

		public BaseObject Pop() {
			return PopMultiple().First();
		}

		public BaseInterpreterStack Pick(int idx) {
			ExecutionSupport.Assert(idx < State.Count, string.Format("Pick {0} invalid when only {1} elements", idx, State.Count));
			Push(State[idx].Clone() as BaseObject);
			return this;
		}

		private List<BaseObject> PopMultiple(int cnt = 1) {
			Interferer.PreStackObjectAccess(this, cnt);
			ExecutionSupport.Assert(State.Any(), "Attempt to pop an empty stack");
			List<BaseObject> objs = State.GetRange(0, cnt);
			State.RemoveRange(0, cnt);
			ExecutionSupport.Emit(() => string.Format("{0} object(s) popped from stack, size now == {1}", cnt, State.Count()));
			Statistics.Increment("Pop", cnt);
			return objs;
		}

		public T Pop<T>() where T : BaseObject {
			return ExecutionSupport.AssertNotNull(Pop() as T, val => string.Format("Object is not a {0}", typeof(T).Name));
		}

		public void Swap() {
			List<BaseObject> objs = PopMultiple(2);
			Push(objs.First()).Push(objs[1]);
		}

		public void Rotate() {
			List<BaseObject> objs = PopMultiple(3);
			Push(objs[1]).Push(objs.First()).Push(objs[2]);
		}

		public virtual void Dump(string wrapper) {
			ExecutionSupport.Emit(() => wrapper + Environment.NewLine + ToString() + Environment.NewLine + "................." + Environment.NewLine);
		}

		public override string ToString() {
			return ToString(StackDumpDirective.Top);
		}

		public string ToString(StackDumpDirective depth) {
			return "Stack size == " + State.Count + ", tos => " + Environment.NewLine +
				(!State.Any() ? "<Empty>" :
				(depth == StackDumpDirective.Top ? State.First().ToString() : String.Join("===>", State.Select(s => s.ToString() + Environment.NewLine).ToArray())));
		}



		protected List<BaseObject> State { get; set; }
	}

	public class RandomAccessStack<TCellType> : BaseInterpreterStack where TCellType : BaseObject, new() {

		private const int InitialCapacity = 512;
		private const int Resize = 1024;

		public RandomAccessStack()
			: this(InitialCapacity, Resize) {
		}

		public RandomAccessStack(int initialCapacity = 0, int maxCapacity = Resize) {
			State.AddRange(GeneratePadding(initialCapacity));
			MaximumSize = maxCapacity <= 0 ? Resize : maxCapacity;
		}

		public int Pointer { get; set; }

		public void Advance() {
			if (++Pointer >= State.Count)
				State.AddRange(GeneratePadding());
		}

		public void Retreat() {
			Pointer = --Pointer < 0 ? 0 : Pointer;
		}

		public void Set(int index) {
			ExecutionSupport.Assert(index >= 0, string.Concat("Illegal random access stack index: ", index));
			if (index >= State.Count)
				State.AddRange(GeneratePadding(index - State.Count + 1));
			Pointer = index;
		}

		public TCellType CurrentCell {
			get {
				return State[Pointer] as TCellType;
			}
			set {
				State[Pointer] = value;
			}
		}

		public int MaximumSize { get; set; }

		public override string ToString() {
			return String.Concat(base.ToString(), ", Pointer == ", Pointer, ", Count == ", State.Count, ", Current cell == ", CurrentCell);
		}

		private IEnumerable<BaseObject> GeneratePadding(int paddingSize = Resize) {
			ExecutionSupport.Assert(State.Count <= MaximumSize, string.Concat("Maximum size exceeded: ", State.Count, " > ", MaximumSize));
			return Enumerable.Repeat(new TCellType(), paddingSize);
		}
	}

	#endregion

	#region State container

	public class InterpreterState {

		private object mSourceObject;
		private Stack<object> mStacks;

		public InterpreterState Establish<TSourceType, TExeType>()
			where TSourceType : SourceCode, new()
			where TExeType : BaseInterpreterStack, new() {
			SetSource(new TSourceType());
			mStacks = new Stack<object>();
			AddExecutionEnvironment<TExeType>();
			return this;
		}

		public TSourceType GetSource<TSourceType>() where TSourceType : SourceCode { return (TSourceType)mSourceObject; }

		internal void SetSource<TSourceType>(TSourceType src) where TSourceType : SourceCode { mSourceObject = src; }

		public TExeType GetExecutionEnvironment<TExeType>() where TExeType : BaseInterpreterStack { return (TExeType)GetStacks().Peek(); }

		public void AddExecutionEnvironment<TExeType>(TExeType exeObject = default(TExeType)) where TExeType : BaseInterpreterStack, new() { GetStacks().Push(exeObject ?? new TExeType()); }

		public void PopExecutionEnvironment<TExeType>() where TExeType : BaseInterpreterStack { GetStacks().Pop(); }

		public void RotateExecutionEnvironment<TExeType>() where TExeType : BaseInterpreterStack, new() {
			if (GetStacks().Count > 1) {
				var tos = GetStacks().Pop();
				var next = GetStacks().Pop();
				AddExecutionEnvironment<TExeType>((TExeType)tos);
				AddExecutionEnvironment<TExeType>((TExeType)next);
			}
		}

		public BaseInterpreterStack BaseExecutionEnvironment { get { return GetExecutionEnvironment<BaseInterpreterStack>(); } }

		public SourceCode BaseSourceCode { get { return GetSource<SourceCode>(); } }

		private Stack<object> GetStacks() {
			return mStacks;
		}
	}

	#endregion

}
