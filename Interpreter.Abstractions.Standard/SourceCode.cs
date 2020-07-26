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

	#region Source code implementations

	public interface ISourceCodeContainer {
	}

	public abstract class SourceCode {

		public event EventHandler<SourceCodeEventArgs<SourceCode>> SourceEvent;

		public SourceCode() {
			SourcePosition = new MutableTuple<int>();
		}

		public virtual List<string> Content { get; set; }

		public string Current() {
			return Content[SourcePosition.Y].Substring(SourcePosition.X, 1);
		}

		public string Peek() {
			string res = null;
			if (More() && Advance()) {
				res = Current();
				Backup();
			}
			return res;
		}

		public virtual char CurrentCharacter() {
			return CurrentCharacter(SourcePosition);
		}

		protected virtual char CurrentCharacter(MutableTuple<int> position) {
			return Content[position.Y][position.X];
		}

		public abstract bool Advance();

		public virtual bool Backup() {
			throw new NotImplementedException("This type does not Backup()");
		}

		public MutableTuple<int> SourcePosition { get; set; }

		public string AdvanceAndReturn() {
			bool more = Advance();
			return more ? Current() : String.Empty;
		}

		public abstract bool More();

		public bool CachingEnabled { get; set; }

		protected void Fire() {
			if (SourceEvent != null)
				SourceEvent(this, new SourceCodeEventArgs<SourceCode> { Source = this });
		}
	}

	public enum SeekDirection { Forward, Backward }

	public class SourceCodeEventArgs<TSourceType> : EventArgs where TSourceType : SourceCode { //, new() {
		public TSourceType Source { get; set; }
	}

	public class SimpleSourceCode : SourceCode {

		public SimpleSourceCode() {
			SeekCache = new Dictionary<string, MutableTuple<int>>();
		}

		public override bool Advance() {
			SourcePosition.X += 1;
			while (SourcePosition.Y < Content.Count && SourcePosition.X >= Content[SourcePosition.Y].Length) {
				SourcePosition.X = 0;
				SourcePosition.Y += 1;
			}
			Fire();
			return More();
		}

		public override bool More() {
			return SourcePosition.Y < Content.Count && (SourcePosition.X < Content[SourcePosition.Y].Length || SourcePosition.Y < Content.Count - 1);
		}

		public override bool Backup() {
			SourcePosition.X -= 1;
			if (SourcePosition.X < 0) {
				if (SourcePosition.Y > 0 && SourcePosition.Y > 0)
					SourcePosition.Y -= 1;
				SourcePosition.X = Content[SourcePosition.Y].Length - 1;
			}
			Fire();
			return SourcePosition.X >= 0;
		}

		public void Seek(char targetToken, SeekDirection direction = SeekDirection.Forward, char? recurseToken = null, int depth = 0) {
			Func<bool> proceed = direction == SeekDirection.Forward ? () => More() : (Func<bool>)(() => SourcePosition.X > 0 || SourcePosition.Y > 0);
			Func<bool> onProceed = direction == SeekDirection.Forward ? () => Advance() : (Func<bool>)(() => Backup());
			if (depth++ == 0) {
				if (CachingEnabled) {
					CurrentCacheKey = string.Concat(SourcePosition.X, SourcePosition.Y, targetToken, direction, recurseToken);
					MutableTuple<int> location = SeekCache.ContainsKey(CurrentCacheKey) ? SeekCache[CurrentCacheKey] : null;
					if (location != null) {
						SourcePosition = new MutableTuple<int>(location);
						return;
					}
				}
				onProceed();
			}
			while (proceed() && CurrentCharacter() != targetToken) {
				if (recurseToken != null && proceed() && CurrentCharacter() == recurseToken.Value) {
					onProceed();
					Seek(targetToken, direction, recurseToken, depth);
				}
				onProceed();
			}

			if (--depth == 0 && CachingEnabled && !SeekCache.ContainsKey(CurrentCacheKey))
				SeekCache[CurrentCacheKey] = new MutableTuple<int>(SourcePosition);
		}

		private Dictionary<string, MutableTuple<int>> SeekCache { get; set; }

		private string CurrentCacheKey { get; set; }
	}

	public enum DirectionOfTravel { Up, Down, Left, Right }

	public class SourceCodeTorus : SourceCode {

		// Columns x Rows
		private MutableTuple<int> mSize = new MutableTuple<int>();
		private Dictionary<DirectionOfTravel, VectorBundle> mMovementVector = new Dictionary<DirectionOfTravel, VectorBundle> { 
			{ DirectionOfTravel.Left, 
				new VectorBundle {	Vector = new Tuple<int, int>(-1, 0), 
									Bounder = (c,l) => { if (c.X < 0) c.X = l.X - 1; } } },
			{ DirectionOfTravel.Right, 
				new VectorBundle {	Vector = new Tuple<int, int>(1, 0), 
									Bounder = (c, l) => { if (c.X == l.X - 1) c.X = 0; } } },
			{ DirectionOfTravel.Up, 
				new VectorBundle {	Vector = new Tuple<int, int>(0, -1), 
									Bounder = (c, l) => { if (c.Y < 0) c.Y = l.Y - 1; } } },
			{ DirectionOfTravel.Down, 
				new VectorBundle {	Vector = new Tuple<int, int>(0, 1), 
									Bounder = (c, l) => { if (c.Y == l.Y - 1) c.Y = 0; } } }
		};

		private class VectorBundle {
			internal Tuple<int, int> Vector { get; set; }
			// Position, Limit
			internal Action<MutableTuple<int>, MutableTuple<int>> Bounder { get; set; }
		}

		public SourceCodeTorus()
			: this(null) {
		}

		public SourceCodeTorus(MutableTuple<int> size = null) {
			Size = size ?? new MutableTuple<int>();
			Direction = DirectionOfTravel.Right;
		}

		public override List<string> Content {
			get {
				return base.Content;
			}
			set {
				ExecutionSupport.AssertNotNull(value, (src) => "Attempt to create torus with null content");
				if (Content == null || !Content.Any())
					ResizeFromContent();
				MergeContent(value);
			}
		}

		public MutableTuple<int> Size {
			get { return mSize; }
			set {
				ExecutionSupport.AssertNotNull(value, (t) => "Cannot specify a null torus size");
				mSize = value;
				Resize();
			}
		}

		public char this[MutableTuple<int> position] {
			get {
				return CurrentCharacter(position);
			}
			set {
				// TODO: Inefficient; consider converting internal representation to List<char[]>
				char[] arr = Content[position.Y].ToArray();
				arr[position.X] = value;
				Content[position.Y] = new string(arr);
			}
		}

		public DirectionOfTravel Direction { get; set; }

		public override bool Advance() {
			ExecutionSupport.Assert(mMovementVector.ContainsKey(Direction), string.Concat("Direction unknown ", Direction));
			Fire();
			VectorBundle bundle = mMovementVector[Direction];
			SourcePosition.X += bundle.Vector.Item1;
			SourcePosition.Y += bundle.Vector.Item2;
			bundle.Bounder(SourcePosition, Size);
			return More();
		}

		public override bool More() {
			return !CompletionSignalled;
		}

		public bool CompletionSignalled { get; set; }

		private void Resize() {
			if (Content == null)
				base.Content = new List<string>();
			Enumerable.Range(0, Size.Y).ToList().ForEach(i => Content.Add(new string(Enumerable.Repeat(' ', Size.X).ToArray())));
		}

		private void ResizeFromContent() {
			Size = new MutableTuple<int>(Content == null ? 0 : Content.Max(s => s.Length), Content == null ? 0 : Content.Count);
		}

		private void MergeContent(List<string> src) {
			int i = 0;
			src.ForEach(s => { Content[i] = string.Concat(s, Content[i].Substring(s.Length)); i++; });
		}

	}

	#endregion

}
