using IndexBackend.DataMaintenance;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class ImageDateParsingTests
    {
        [Test]
        public void ParseNumericYear()
        {
            Assert.AreEqual(1924, ImageDateParsing.ParseDate("1924"));
        }

        [Test]
        public void ParseTextDateRangeConservatively()
        {
            Assert.AreEqual(1945, ImageDateParsing.ParseDate("xyz(1943-1945)abcd"));
        }

        [Test]
        public void ParseCircaTextDate()
        {
            Assert.AreEqual(1940, ImageDateParsing.ParseDate("circa 1940"));
        }

        [Test]
        public void ParseCircaTextDateRange()
        {
            Assert.AreEqual(1941, ImageDateParsing.ParseDate("circa 1938-1941"));
        }

        [Test]
        public void UnparseableDateReturnsNull()
        {
            Assert.AreEqual(null, ImageDateParsing.ParseDate("nineteen eighty four"));
        }
    }
}
