using System;
using System.IO;
using CCVTAC.Console;

namespace CCVTAC.Tests;

public class ExtensionMethodTests
{
    private static readonly string _validBaseFileName = @"filename123あいうえお漢字!@#$%^()_+ ";
    private static readonly char _defaultReplaceWithChar = '_';
    private static readonly char[] _pathInvalidChars = new[] {
        Path.PathSeparator,
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
        Path.VolumeSeparatorChar
    };

    [Fact]
    public void ReplaceInvalidPathChars_StringContainsInvalidPathChars_Fixes()
    {
        var badFileName = _validBaseFileName + new string(_pathInvalidChars);
        var fixedPathName = badFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar);
        var expected = _validBaseFileName + new string(_defaultReplaceWithChar, _pathInvalidChars.Length);
        Assert.Equal(expected, fixedPathName);
    }

    [Fact]
    public void ReplaceInvalidPathChars_StringContainsNoInvalidPathChars_DoesNotChange()
    {
        var goodFileName = _validBaseFileName;
        var result = goodFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar);
        Assert.Equal(goodFileName, result);
    }

    [Fact]
    public void ReplaceInvalidPathCharsIncludingCustom_StringContainsInvalidPathChars_Fixes()
    {
        var customInvalidChars = new char[] { '&', '＆' };
        var badFileName = _validBaseFileName + new string(customInvalidChars);
        var fixedPathName = badFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar, customInvalidChars);
        var expected = _validBaseFileName + new string(_defaultReplaceWithChar, customInvalidChars.Length);
        Assert.Equal(expected, fixedPathName);
    }

    [Fact]
    public void ReplaceInvalidPathCharsIncludingCustom_StringContainsNoInvalidPathChars_DoesNotChange()
    {
        var customInvalidChars = new char[] { '&', '＆' };
        var goodFileName = _validBaseFileName + "++";
        var result = goodFileName.ReplaceInvalidPathChars(_defaultReplaceWithChar, customInvalidChars);
        Assert.Equal(goodFileName, result);
    }

    [Fact]
    public void ReplaceInvalidPathChars_InvalidReplaceChar_ThrowsException()
    {
        const char knownInvalidChar = '/';
        Assert.Throws<ArgumentException>(() => _validBaseFileName.ReplaceInvalidPathChars(knownInvalidChar));
    }
}
