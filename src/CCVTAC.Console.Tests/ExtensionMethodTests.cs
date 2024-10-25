using System;
using System.Collections.Generic;
using System.IO;

namespace CCVTAC.Console.Tests;

public sealed class ExtensionMethodTests
{
    public sealed class ReplaceInvalidPathCharsTests
    {
        private const string ValidBaseFileName = @"filename123あいうえお漢字!@#$%^()_+ ";
        private const char DefaultReplaceWithChar = '_';

        private static readonly char[] PathInvalidChars = [
            Path.PathSeparator,
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar,
            Path.VolumeSeparatorChar
        ];

        [Fact]
        public void ReplaceInvalidPathChars_StringContainsInvalidPathChars_Fixes()
        {
            string badFileName = ValidBaseFileName + new string(PathInvalidChars);
            string fixedPathName = badFileName.ReplaceInvalidPathChars(DefaultReplaceWithChar);
            string expected = ValidBaseFileName + new string(DefaultReplaceWithChar, PathInvalidChars.Length);
            Assert.Equal(expected, fixedPathName);
        }

        [Fact]
        public void ReplaceInvalidPathChars_StringContainsNoInvalidPathChars_DoesNotChange()
        {
            string goodFileName = ValidBaseFileName;
            string result = goodFileName.ReplaceInvalidPathChars(DefaultReplaceWithChar);
            Assert.Equal(goodFileName, result);
        }

        [Fact]
        public void ReplaceInvalidPathCharsIncludingCustom_StringContainsInvalidPathChars_Fixes()
        {
            char[] customInvalidChars = ['&', '＆'];
            string badFileName = ValidBaseFileName + new string(customInvalidChars);
            string fixedPathName = badFileName.ReplaceInvalidPathChars(DefaultReplaceWithChar, customInvalidChars);
            string expected = ValidBaseFileName + new string(DefaultReplaceWithChar, customInvalidChars.Length);
            Assert.Equal(expected, fixedPathName);
        }

        [Fact]
        public void ReplaceInvalidPathCharsIncludingCustom_StringContainsNoInvalidPathChars_DoesNotChange()
        {
            char[] customInvalidChars = ['&', '＆'];
            string goodFileName = ValidBaseFileName + "++";
            string result = goodFileName.ReplaceInvalidPathChars(DefaultReplaceWithChar, customInvalidChars);
            Assert.Equal(goodFileName, result);
        }

        [Fact]
        public void ReplaceInvalidPathChars_InvalidReplaceChar_ThrowsException()
        {
            const char knownInvalidChar = '/';
            Assert.Throws<ArgumentException>(() => ValidBaseFileName.ReplaceInvalidPathChars(knownInvalidChar));
        }
    }

    public sealed class NoneTests
    {
        [Fact]
        public void None_WithEmptyCollection_ReturnsTrue()
        {
            List<uint> numbers = [];
            Assert.True(numbers.None());
        }

        [Fact]
        public void None_WithPopulatedCollectionAndMatchingPredicate_ReturnsFalse()
        {
            List<byte> numbers = [2, 4, 6];
            static bool IsEven(byte s) => s % 2 == 0;
            Assert.False(numbers.None(IsEven));
        }

        [Fact]
        public void None_WithPopulatedCollectionAndNonMatchingPredicate_ReturnsTrue()
        {
            List<byte> numbers = [1, 3, 5];
            static bool IsEven(byte s) => s % 2 == 0;
            Assert.True(numbers.None(IsEven));
        }

        [Fact]
        public void None_WithEmptyCollectionAndPredicate_ReturnsTrue()
        {
            List<byte> numbers = [];
            static bool IsEven(byte s) => s % 2 == 0;
            Assert.True(numbers.None(IsEven));
        }
    }

    public sealed class HasTextTests
    {
        [Fact]
        public void HasText_Null_ReturnsFalse()
        {
            string? noText = null;
            Assert.False(noText.HasText(false));
            Assert.False(noText.HasText(true));
        }

        [Fact]
        public void HasText_EmptyString_ReturnsFalse()
        {
            var emptyText = string.Empty;
            Assert.False(emptyText.HasText(false));
            Assert.False(emptyText.HasText(true));
        }

        [Fact]
        public void HasText_SingleByteWhiteSpaceOnlyWhenDisallowed_ReturnsFalse()
        {
            var whiteSpace = "   ";
            Assert.False(whiteSpace.HasText(false));
        }

        [Fact]
        public void HasText_SingleByteWhiteSpaceOnlyWhenAllowed_ReturnsTrue()
        {
            var whiteSpace = "   ";
            Assert.True(whiteSpace.HasText(true));
        }

        [Fact]
        public void HasText_DoubleByteWhiteSpaceOnlyWhenDisallowed_ReturnsFalse()
        {
            var whiteSpace = "　　　";
            Assert.False(whiteSpace.HasText(false));
        }

        [Fact]
        public void HasText_DoubleByteWhiteSpaceOnlyWhenAllowed_ReturnsTrue()
        {
            var whiteSpace = "　　　";
            Assert.True(whiteSpace.HasText(true));
        }

        [Fact]
        public void HasText_WithText_ReturnsTrue()
        {
            var text = "こんにちは！";
            Assert.True(text.HasText(false));
            Assert.True(text.HasText(true));
        }
    }

    public sealed class CaseInsensitiveContainsTests
    {
        private static readonly List<string> CelestialBodies = ["Moon", "Mercury", "Mars", "Jupiter", "Venus"];

        [Fact]
        public void CaseInsensitiveContains_EmptyCollection_ReturnsFalse()
        {
            List<string> collection = [];
            var actual = collection.CaseInsensitiveContains("text");
            Assert.False(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchAllCapsInPopulatedCollection_ReturnsTrue()
        {
            List<string> collection = CelestialBodies;
            var actual = collection.CaseInsensitiveContains("MOON");
            Assert.True(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchAllLowercaseInPopulatedCollection_ReturnsTrue()
        {
            List<string> collection = CelestialBodies;
            var actual = collection.CaseInsensitiveContains("moon");
            Assert.True(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchExactInPopulatedCollection_ReturnsTrue()
        {
            List<string> collection = CelestialBodies;
            var actual = collection.CaseInsensitiveContains("Moon");
            Assert.True(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchPartialInPopulatedCollection_ReturnsFalse()
        {
            List<string> collection = CelestialBodies;
            var actual = collection.CaseInsensitiveContains("Mo");
            Assert.False(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchExactButDoubleWidthInPopulatedCollection_ReturnsFalse()
        {
            List<string> collection = CelestialBodies;
            var actual = collection.CaseInsensitiveContains("Ｍｏｏｎ");
            Assert.False(actual);
        }

        [Fact]
        public void CaseInsensitiveContains_SearchTextInEmptyCollection_ReturnsFalse()
        {
            List<string> collection = [];
            var actual = collection.CaseInsensitiveContains("text");
            Assert.False(actual);
        }
    }
}
