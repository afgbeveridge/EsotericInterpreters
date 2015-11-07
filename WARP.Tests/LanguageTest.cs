using WARP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace WARP.Tests {

	[TestClass()]
	public class LanguageTest {


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		[TestInitialize]
		public void Initialize() {
			Builder = new CommandBuilder();
			State = new InterpreterState().Establish<SimpleSourceCode, PropertyBasedExecutionEnvironment>();
			WARPObject.CurrentRadix = FlexibleNumeralSystem.StandardRadix;
			Stack().Reset();
			PropertyBasedExecutionEnvironment.ScratchPad[Constants.RASName] = new RandomAccessStack<WARPObject> { MaximumSize = 50000 };
			PropertyBasedExecutionEnvironment.OnUnknownKey = env => new WARPObject();
		}

		private LanguageTest EstablishProgramSource(string code) {
			State.GetSource<SimpleSourceCode>().Content = new List<string> { code };
			return this;
		}

		private WARPObject Dereference(string key) {
			return State.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>()[key] as WARPObject;
		}

		private WARPObject TOS() {
			return Stack().Pop<WARPObject>(); ;
		}

		private PropertyBasedExecutionEnvironment Stack() {
			return State.Stack<PropertyBasedExecutionEnvironment>();
		}

		private InterpreterState State { get; set; }

		private CommandBuilder Builder { get; set; }

		private LanguageTest Parse() {
			while (State.GetSource<SimpleSourceCode>().More())
				Builder.Gather(State).Apply(State);
			return this;
		}

		private WARPObject ParseAndDereference(string key) {
			return Parse().Dereference(key);
		}

		private WARPObject ParseAndReturnTOS() {
			return Parse().TOS();
		}

		[TestMethod]
		public void SupportsAssign() {
			EstablishProgramSource("=");
			Assert.IsTrue(Builder.Applicable(State), "Assign should be supported");
		}

		[TestMethod]
		public void EmptyProgram() {
			EstablishProgramSource(String.Empty).Parse();
		}

		[TestMethod]
		public void InvalidProgram() {
			EstablishProgramSource("2").Parse();
		}

		[TestMethod]
		public void SimpleAssign() {
			var x = EstablishProgramSource("=ab1").ParseAndDereference("ab");
			Assert.AreEqual("1", x.AsString());
			Assert.IsTrue(x.IsNumeric);
		}

		[TestMethod]
		public void ObjectAssign() {
			var x = EstablishProgramSource("=ab1=bcab").ParseAndDereference("bc");
			Assert.AreEqual("1", x.AsString());
		}

		[TestMethod]
		public void StringAssign() {
			var x = EstablishProgramSource("=ab\"Content\"").ParseAndDereference("ab");
			Assert.AreEqual("Content", x.AsString());
		}

		[TestMethod]
		public void SimplePush() {
			var x = EstablishProgramSource("*\"Content\"").ParseAndReturnTOS();
			Assert.AreEqual("Content", x.AsString());
		}

		[TestMethod]
		public void ObjectPush() {
			var x = EstablishProgramSource("=tt\"Content\"*tt").ParseAndReturnTOS();
			Assert.AreEqual("Content", x.AsString());
		}

		[TestMethod]
		public void PopAssign() {
			var x = EstablishProgramSource("*17=ab!").ParseAndDereference("ab");
			Assert.AreEqual("17", x.AsString());
		}

		[TestMethod]
		public void SimpleAddition() {
			var x = EstablishProgramSource("=ab5>ab2").ParseAndDereference("ab");
			Assert.AreEqual("7", x.AsString());
		}

		[TestMethod]
		public void ObjectAddition() {
			var x = EstablishProgramSource("=ab5=xx2>abxx").ParseAndDereference("ab");
			Assert.AreEqual("7", x.AsString());
		}

		[TestMethod]
		public void CoercedAddition() {
			var x = EstablishProgramSource("=ab5=xx\"2\">abxx").ParseAndDereference("ab");
			Assert.AreEqual("7", x.AsString());
		}

		[TestMethod]
		public void StackAddition() {
			Assert.AreEqual(6, EstablishProgramSource("*5>!1").ParseAndReturnTOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleSubtraction() {
			var x = EstablishProgramSource("=ab5<ab2").ParseAndDereference("ab");
			Assert.AreEqual("3", x.AsString());
		}

		[TestMethod]
		public void ObjectSubtraction() {
			var x = EstablishProgramSource("=ab5=xx2<abxx").ParseAndDereference("ab");
			Assert.AreEqual("3", x.AsString());
		}

		[TestMethod]
		public void CoercedSubtraction() {
			var x = EstablishProgramSource("=ab5=xx\"2\"<abxx").ParseAndDereference("ab");
			Assert.AreEqual("3", x.AsString());
		}

		[TestMethod]
		public void SimpleMultiplication() {
			var x = EstablishProgramSource("=ab5&ab2").ParseAndDereference("ab");
			Assert.AreEqual("A", x.AsString());
		}

		[TestMethod]
		public void ObjectMultiplication() {
			var x = EstablishProgramSource("=ab5=xx2&abxx").ParseAndDereference("ab");
			Assert.AreEqual("A", x.AsString());
		}

		[TestMethod]
		public void CoercedMultiplication() {
			var x = EstablishProgramSource("=ab5=xx\"2\"&abxx").ParseAndDereference("ab");
			Assert.AreEqual("A", x.AsString());
		}

		[TestMethod]
		public void SimpleDivision() {
			var x = EstablishProgramSource("=ab5$ab2").ParseAndDereference("ab");
			Assert.AreEqual("2", x.AsString());
		}

		[TestMethod]
		public void ObjectDivision() {
			var x = EstablishProgramSource("=ab5=xx2$abxx").ParseAndDereference("ab");
			Assert.AreEqual("2", x.AsString());
		}

		[TestMethod]
		public void CoercedDivision() {
			var x = EstablishProgramSource("=ab5=xx\"2\"$abxx").ParseAndDereference("ab");
			Assert.AreEqual("2", x.AsString());
		}

		[TestMethod]
		public void SimpleModulo() {
			var x = EstablishProgramSource("=ab5#ab2").ParseAndDereference("ab");
			Assert.AreEqual("1", x.AsString());
		}

		[TestMethod]
		public void ObjectModulo() {
			var x = EstablishProgramSource("=ab5=xx2#abxx").ParseAndDereference("ab");
			Assert.AreEqual("1", x.AsString());
		}

		[TestMethod]
		public void CoercedModulo() {
			var x = EstablishProgramSource("=ab5=xx\"2\"#abxx").ParseAndDereference("ab");
			Assert.AreEqual("1", x.AsString());
		}

		[TestMethod]
		public void DuplicateStackItem() {
			EstablishProgramSource("*1;").Parse();
			Assert.IsTrue(Stack().Size == 2);
			Assert.AreEqual(TOS().AsString(), TOS().AsString());
		}

		[TestMethod]
		public void DecimalRadix() {
			EstablishProgramSource("+A=ab105");
			Assert.AreEqual(105, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void HexatrigesimalRadix() {
			EstablishProgramSource("=ab255S");
			Assert.AreEqual(100000, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void BinaryRadix() {
			EstablishProgramSource("+2=ab1010");
			Assert.AreEqual(10, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void OctalRadix() {
			EstablishProgramSource("+8=ab377");
			Assert.AreEqual(255, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void Base11Radix() {
			EstablishProgramSource("+B=ab120");
			Assert.AreEqual(143, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void NegativeNumberTest() {
			EstablishProgramSource("+A=ab-120");
			Assert.AreEqual(-120, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void RadixSwap() {
			EstablishProgramSource("+A=ab37+36=cd11");
			Assert.AreEqual(ParseAndDereference("cd").AsNumeric(), ParseAndDereference("ab").AsNumeric());
		}

		// Invalid numerics should return a default value of zero long
		[TestMethod]
		public void InvalidNumeric() {
			EstablishProgramSource("+A=abA11");
			Assert.AreEqual(0, ParseAndDereference("ab").AsNumeric());
		}

		[TestMethod]
		public void PopAndPushCommand() {
			EstablishProgramSource("*2]3").Parse();
			Assert.IsTrue(Stack().Size == 1);
			Assert.AreEqual(3, TOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleLoop() {
			EstablishProgramSource("=ab2=dc0@a>dc2<ab1^aba");
			Assert.AreEqual(4, ParseAndDereference("dc").AsNumeric());
		}

		[TestMethod]
		public void NestedLoop() {
			EstablishProgramSource("=ab2=dc0@a>dc2<ab1=xx2@b&dc2<xx1^xxb^aba");
			Assert.AreEqual(40, ParseAndDereference("dc").AsNumeric());
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException))]
		public void MissingLabelTest() {
			EstablishProgramSource("=ab2@a<ab1^abz").Parse();
		}

		[TestMethod]
		public void LoopOnStackCondition() {
			EstablishProgramSource("*1*2*3=dc0@a>dc!^_a");
			Assert.AreEqual(6, ParseAndDereference("dc").AsNumeric());
		}

		[TestMethod]
		public void UnconditionalJump() {
			EstablishProgramSource("=dc0^.p>dc1@p");
			Assert.AreEqual(0, ParseAndDereference("dc").AsNumeric());
		}

		[TestMethod]
		public void SimpleEqualComparison() {
			EstablishProgramSource(":7:7").Parse();
			Assert.AreEqual(0, TOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleGreaterThanComparison() {
			EstablishProgramSource(":8:7").Parse();
			Assert.AreEqual(1, TOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleLessThanComparison() {
			EstablishProgramSource(":6:7").Parse();
			Assert.AreEqual(-1, TOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleIfBranch() {
			EstablishProgramSource(":7:7?0?=yy5");
			Assert.AreEqual(5, ParseAndDereference("yy").AsNumeric());
		}

		[TestMethod]
		public void UnexecutedIfBranch() {
			EstablishProgramSource("=yy0:6:7?0?=yy5");
			Assert.AreEqual(0, ParseAndDereference("yy").AsNumeric());
		}

		[TestMethod]
		public void SimpleRASAccess() {
			EstablishProgramSource("*3}18{18").Parse();
			Assert.AreEqual(3, TOS().AsNumeric());
		}

		[TestMethod]
		public void ExpectInitializedRAS() {
			EstablishProgramSource("{18").Parse();
			Assert.AreEqual(0, TOS().AsNumeric());
		}

		[TestMethod]
		public void EnsureRASGrows() {
			EstablishProgramSource("{255S").Parse();
			Assert.AreEqual(0, TOS().AsNumeric());
		}

		[TestMethod]
		public void SimpleTreatment() {
			var content = "A string";
			EstablishProgramSource(string.Concat("=st\"", content, "\"%st")).Parse();
			Assert.AreEqual(content.Length, Stack().Size);
		}

		[TestMethod]
		public void SimpleUnTreatment() {
			EstablishProgramSource("=st\"A string\"%st|").Parse();
			Assert.AreEqual(0, Stack().Size);
		}

		[TestMethod]
		public void UntreatmentTest() {
			EstablishProgramSource("*\"p\"=st\"A string\"%st=tr!|=ts!").Parse();
			Assert.AreEqual("A", Dereference("tr").AsString());
			Assert.AreEqual("p", Dereference("ts").AsString());
		}

		[TestMethod]
		public void RotationTest() {
			EstablishProgramSource("*\"p\"=st\"q\"%st'=aa!'").Parse();
			Assert.AreEqual("p", Dereference("aa").AsString());
			Assert.AreEqual("q", TOS().AsString());
		}

		[TestMethod]
		public void AutoCreationTest() {
			EstablishProgramSource(">vv1").Parse();
			Assert.AreEqual(1, Dereference("vv").AsNumeric());
		}

	}
}
