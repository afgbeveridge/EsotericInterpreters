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

	public static class TypeExtender {

		private const string GenericSeparator = "`";

		/// <summary>
		/// Given a type name, an assembly name and some type parameters, attempt to instantiate a generic type
		/// </summary>
		public static Type FormGenericType(this Type pType, string pName, Type[] pTypeArguments, string pAssName = null) {
			Type unboundType = LocateGenericType(pType, pName, pAssName, pTypeArguments);
			ExecutionSupport.AssertNotNull(unboundType, string.Format("Cannot find base {0}", pName));
			pTypeArguments.ToList().ForEach(arg => ExecutionSupport.AssertNotNull(arg,
				string.Format("Null type arg passed for unboundType {0}", unboundType)));
			return unboundType.MakeGenericType(pTypeArguments);
		}

		public static Type LocateGenericType(this Type pType, string pName, string pAssName, Type[] pTypeArguments) {
			ExecutionSupport.AssertNotNull(pName, "Cannot form a generic type from nothing");
			ExecutionSupport.Assert(pTypeArguments != null && pTypeArguments.Length > 0, "No or invalid type arguments supplied");
			string mangledTypeName = string.Format("{0}{1}{2}", pName, GenericSeparator, pTypeArguments.Length);
			if (!String.IsNullOrEmpty(pAssName)) {
				mangledTypeName = string.Format("{0},{1}", mangledTypeName, pAssName);
			}

			Type unboundType = Type.GetType(mangledTypeName);
			return unboundType;
		}

		// TODO: Fold into previous method
		public static string CreateMangledTypeName(this Type pType, string pName, int pNumArguments) {
			ExecutionSupport.AssertNotNull(pName, "Cannot form a generic type from nothing");
			ExecutionSupport.Assert(pNumArguments > 0, "No or invalid type arguments supplied");
			return string.Format("{0}{1}{2}", pName, GenericSeparator, pNumArguments);
		}

		/// <summary>
		/// Instantiate an object of type T given arguments that describe a generic type
		/// </summary>
		public static T InstantiateGenericType<T>(this Type pType, string pName, Type[] pTypeArguments, string pAssName = null) {
			Type boundType = FormGenericType(pType, pName, pTypeArguments, pAssName);
			ExecutionSupport.AssertNotNull(boundType, string.Format("Cannot form generic type from base {0}", pName));
			return (T)Activator.CreateInstance(boundType);
		}

		/// <summary>
		/// Instantiate an object given arguments that describe a generic type (including assembly name)
		/// </summary>
		public static object InstantiateGenericType(this Type pType, string pName, string pAssName, Type[] pTypeArguments) {
			Type boundType = FormGenericType(pType, pName, pTypeArguments, pAssName);
			ExecutionSupport.AssertNotNull(boundType, string.Format("Cannot form generic type from base {0}", pName));
			return Activator.CreateInstance(boundType);
		}
	}

	public static class StateExtensions {

		public static SimpleSourceCode Source(this InterpreterState state) {
			return state.GetSource<SimpleSourceCode>();
		}

		public static T Stack<T>(this InterpreterState state) where T : BaseInterpreterStack {
			return state.GetExecutionEnvironment<T>();
		}

	}

	public static class ObjectExtensions {

		public static T Fluently<T>(this T obj, Action action) {
			action();
			return obj;
		}

		public static bool IfTrue(this bool val, Action action) {
			if (val) action();
			return val;
		}

		public static bool IfFalse(this bool val, Action action) {
			if (!val) action();
			return val;
		}

	}
}
