using System;
using System.Collections.Generic;
using System.IO;

namespace CCVTAC.Console.Tests;

public sealed class ExtensionMethodTests
{
    private static readonly string _validBaseFileName = @"filename123あいうえお漢字!@#$%^()_+ ";
    private static readonly char _defaultReplaceWithChar = '_';
    private static readonly char[] _pathInvalidChars = [
        Path.PathSeparator,
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
        Path.VolumeSeparatorChar
    ];

    [Fact]
    public void ReplaceInvalidPathChars_StringContainsInvalidPathChars_Fixes()
    {
        string badFileName = _validBaseFileName + new string(_pathInvalidChars);
        string fixedPathName = badFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar);
        string expected = _validBaseFileName + new string(_defaultReplaceWithChar, _pathInvalidChars.Length);
        Assert.Equal(expected, fixedPathName);
    }

    [Fact]
    public void ReplaceInvalidPathChars_StringContainsNoInvalidPathChars_DoesNotChange()
    {
        string goodFileName = _validBaseFileName;
        string result = goodFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar);
        Assert.Equal(goodFileName, result);
    }

    [Fact]
    public void ReplaceInvalidPathCharsIncludingCustom_StringContainsInvalidPathChars_Fixes()
    {
        char[] customInvalidChars = ['&', '＆'];
        string badFileName = _validBaseFileName + new string(customInvalidChars);
        string fixedPathName = badFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar, customInvalidChars);
        string expected = _validBaseFileName + new string(_defaultReplaceWithChar, customInvalidChars.Length);
        Assert.Equal(expected, fixedPathName);
    }

    [Fact]
    public void ReplaceInvalidPathCharsIncludingCustom_StringContainsNoInvalidPathChars_DoesNotChange()
    {
        char[] customInvalidChars = ['&', '＆'];
        string goodFileName = _validBaseFileName + "++";
        string result = goodFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar, customInvalidChars);
        Assert.Equal(goodFileName, result);
    }

    [Fact]
    public void ReplaceInvalidPathChars_InvalidReplaceChar_ThrowsException()
    {
        const char knownInvalidChar = '/';
        Assert.Throws<ArgumentException>(() => _validBaseFileName.ReplaceInvalidPathChars(knownInvalidChar));
    }

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
        static bool evenBytePredicate(byte s) => s % 2 == 0;
        Assert.False(numbers.None(evenBytePredicate));
    }

    [Fact]
    public void None_WithPopulatedCollectionAndNonMatchingPredicate_ReturnsTrue()
    {
        List<byte> numbers = [1, 3, 5];
        static bool evenBytePredicate(byte s) => s % 2 == 0;
        Assert.True(numbers.None(evenBytePredicate));
    }

    [Fact]
    public void None_WithEmptyCollectionAndPredicate_ReturnsTrue()
    {
        List<byte> numbers = [];
        static bool evenBytePredicate(byte s) => s % 2 == 0;
        Assert.True(numbers.None(evenBytePredicate));
    }
}
