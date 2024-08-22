using CdrAuthServer.Domain.Extensions;
using CdrAuthServer.Extensions;
using NUnit.Framework;

namespace CdrAuthServer.UnitTests.Extensions
{
    public class StringExtensionsTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void WhenValueIsNull_HasValue_ShouldReturnFalse()
        {
            // Arrange.
            string value = null;
            bool expected = false;
            bool actual = false;

            // Act.
            actual = value.HasValue();

            // Assert.
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenValueIsEmpty_HasValue_ShouldReturnFalse()
        {
            // Arrange.
            string value = string.Empty;
            bool expected = false;
            bool actual = false;

            // Act.
            actual = value.HasValue();

            // Assert.
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenValueExists_HasValue_ShouldReturnTrue()
        {
            // Arrange.
            string value = "foo";
            bool expected = true;
            bool actual = false;

            // Act.
            actual = value.HasValue();

            // Assert.
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenValueIsNull_IsNullOrEmpty_ShouldReturnTrue()
        {
            // Arrange.
            string value = null;
            bool expected = true;
            bool actual = false;

            // Act.
            actual = value.IsNullOrEmpty();

            // Assert.
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenValueIsEmpty_IsNullOrEmpty_ShouldReturnTrue()
        {
            // Arrange.
            string value = string.Empty;
            bool expected = true;
            bool actual = false;

            // Act.
            actual = value.IsNullOrEmpty();

            // Assert.
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenValueExists_IsNullOrEmpty_ShouldReturnFalse()
        {
            // Arrange.
            string value = "foo";
            bool expected = false;
            bool actual = false;

            // Act.
            actual = value.IsNullOrEmpty();

            // Assert.
            Assert.AreEqual(expected, actual);
        }
    }
}