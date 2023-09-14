using System;
using Content.Shared.FixedPoint;
using NUnit.Framework;

namespace Content.Tests.Shared.Chemistry
{
    [TestFixture, TestOf(typeof(FixedPoint2))]
    public sealed class FixedPoint2_Tests
    {
        [Test]
        [TestCase(1, "1")]
        [TestCase(0, "0")]
        [TestCase(-1, "-1")]
        public void FixedPoint2IntegerTests(int value, string expected)
        {
            var result = FixedPoint2.New(value);
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, "1")]
        [TestCase(0.999f, "1")]
        public void FixedPoint2FloatTests(float value, string expected)
        {
            var result = FixedPoint2.New(value);
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001d, "1")]
        [TestCase(0.999d, "1")]
        public void FixedPoint2DoubleTests(double value, string expected)
        {
            var result = FixedPoint2.New(value);
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase("1.005", "1.01")]
        [TestCase("0.999", "1")]
        public void FixedPoint2StringTests(string value, string expected)
        {
            var result = FixedPoint2.New(value);
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, 1.001f, "2")]
        [TestCase(1.001f, 1.004f, "2")]
        [TestCase(1f, 1.005f, "2.01")]
        [TestCase(1f, 2.005f, "3.01")]
        public void CalculusPlus(float aFloat, float bFloat, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var b = FixedPoint2.New(bFloat);

            var result = a + b;

            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, 1.001f, "0")]
        [TestCase(1.001f, 1.004f, "0")]
        [TestCase(1f, 2.005f, "-1.01")]
        public void CalculusMinus(float aFloat, float bFloat, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var b = FixedPoint2.New(bFloat);

            var result = a - b;

            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, 3f, "0.33")]
        [TestCase(0.999f, 3f, "0.33")]
        [TestCase(2.1f, 3f, "0.7")]
        public void CalculusDivision(float aFloat, float bFloat, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var b = FixedPoint2.New(bFloat);

            var result = a / b;

            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, 0.999f, "1")]
        [TestCase(0.999f, 3f, "3")]
        public void CalculusMultiplication(float aFloat, float bFloat, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var b = FixedPoint2.New(bFloat);

            var result = a * b;

            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0.995f, 100)]
        [TestCase(1.005f, 101)]
        [TestCase(2.005f, 201)]
        public void FloatRoundingTest(float a, int expected)
        {
            var result = (int) MathF.Round(a * MathF.Pow(10, 2), MidpointRounding.AwayFromZero);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void FixedPoint2Min()
        {
            var unorderedList = new[]
            {
                FixedPoint2.New(5),
                FixedPoint2.New(3),
                FixedPoint2.New(1),
                FixedPoint2.New(2),
                FixedPoint2.New(4),
            };
            var min = FixedPoint2.Min(unorderedList);
            Assert.That(min, Is.EqualTo(FixedPoint2.New(1)));
        }

        [Test]
        [TestCase(10.1f, 2.5f, "25.25")]
        public void FloatMultiply (float aFloat, float b, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var result = a*b;
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(10.1f, 2.5d, "25.25")]
        public void DoubleMultiply(float aFloat, double b, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var result = a * b;
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(10.1f, 2.5f, "4.04")]
        public void FloatDivide(float aFloat, float b, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var result = a / b;
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(10.1f, 2.5d, "4.04")]
        public void DoubleDivide(float aFloat, double b, string expected)
        {
            var a = FixedPoint2.New(aFloat);
            var result = a / b;
            Assert.That($"{result}", Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1, 0, false)]
        [TestCase(0, 0, true)]
        [TestCase(-1, 0, false)]
        [TestCase(1, 1, true)]
        [TestCase(0, 1, false)]
        [TestCase(-1, 1, false)]
        public void FixedPoint2Equals(int a, int b, bool expected)
        {
            var parameter = FixedPoint2.New(a);
            var comparison = FixedPoint2.New(b);
            Assert.That(parameter.Equals(comparison), Is.EqualTo(comparison.Equals(parameter)));
            Assert.That(comparison.Equals(parameter), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1.001f, "1.01")]
        [TestCase(2f,     "2")]
        [TestCase(2.5f,   "2.5")]
        public void NewCeilingTest(float value, string expected)
        {
            var result = FixedPoint2.NewCeiling(value);
            Assert.That($"{result}", Is.EqualTo(expected));
        }
    }
}
