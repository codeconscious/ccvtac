using System;
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
}
