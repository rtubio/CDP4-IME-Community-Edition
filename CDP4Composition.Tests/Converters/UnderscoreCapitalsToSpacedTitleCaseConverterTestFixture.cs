﻿namespace CDP4Composition.Tests.Converters
{
    using System;
    using System.Threading;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition.Converters;
    using NUnit.Framework;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class UnderscoreCapitalsToSpacedTitleCaseConverterTestFixture
    {
        /// <summary>
        /// the <see cref="ThingToIconUriConverter"/> under test
        /// </summary>
        private UnderscoreCapitalsToSpacedTitleCaseConverter converter;

        [SetUp]
        public void SetUp()
        {
            this.converter = new UnderscoreCapitalsToSpacedTitleCaseConverter();
        }

        [Test]
        public void VerifyThatConvertingNullReturnsNull()
        {
            var string1 = this.converter.Convert(null, null, null, null);
            Assert.IsNull(string1);
        }

        [Test]
        public void VerifyThatConvertProvidesTheExpectedString()
        {
            const string expected = "Design Session Phase";
            var somePhase = StudyPhaseKind.DESIGN_SESSION_PHASE;

            var converterResult = this.converter.Convert(somePhase, null, null, null);

            Assert.AreEqual(expected, converterResult);
        }

        [Test]
        public void VerifyThatConvertBackThrowsException()
        {
            Assert.Throws<NotSupportedException>(() => this.converter.ConvertBack(null, null, null, null));
        }
    }
}