using System.Text.Json;

namespace Npgquery.Tests;

/// <summary>
/// Tests for custom exception paths in Exceptions.cs and QueryUtils.cs.
/// Goal: bring Exceptions.cs coverage from 15% toward 90%+ and add QueryUtils.cs coverage.
/// </summary>
public class ExceptionPathTests
{
    // ─── ParserException (abstract – tested via concrete subtypes) ───────────

    [Fact]
    public void ParseException_Constructor_SetsProperties()
    {
        var ex = new ParseException("syntax error", "SELECT BAD");
        Assert.Equal("syntax error", ex.ParseError);
        Assert.Equal("SELECT BAD", ex.Query);
        Assert.Contains("syntax error", ex.Message);
    }

    [Fact]
    public void ParseException_ConstructorWithoutQuery_QueryIsNull()
    {
        var ex = new ParseException("some error");
        Assert.Equal("some error", ex.ParseError);
        Assert.Null(ex.Query);
    }

    [Fact]
    public void ParseException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ParseException("parse error", inner, "SELECT 1");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("parse error", ex.ParseError);
        Assert.Equal("SELECT 1", ex.Query);
    }

    [Fact]
    public void ParseException_IsParserException()
    {
        var ex = new ParseException("err");
        Assert.IsAssignableFrom<ParserException>(ex);
    }

    // ─── NativeLibraryException ──────────────────────────────────────────────

    [Fact]
    public void NativeLibraryException_Constructor_SetsMessage()
    {
        var ex = new NativeLibraryException("lib not found");
        Assert.Equal("lib not found", ex.Message);
        Assert.IsAssignableFrom<ParserException>(ex);
    }

    [Fact]
    public void NativeLibraryException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new DllNotFoundException("native.so");
        var ex = new NativeLibraryException("failed to load", inner);
        Assert.Equal(inner, ex.InnerException);
        Assert.Contains("failed to load", ex.Message);
    }

    // ─── NormalizationException ──────────────────────────────────────────────

    [Fact]
    public void NormalizationException_Constructor_SetsNormalizationError()
    {
        var ex = new NormalizationException("bad input", "SELECT ??");
        Assert.Equal("bad input", ex.NormalizationError);
        Assert.Equal("SELECT ??", ex.Query);
        Assert.Contains("bad input", ex.Message);
    }

    [Fact]
    public void NormalizationException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("root cause");
        var ex = new NormalizationException("normalize failed", inner, "SELECT x");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("normalize failed", ex.NormalizationError);
        Assert.Equal("SELECT x", ex.Query);
    }

    // ─── FingerprintException ────────────────────────────────────────────────

    [Fact]
    public void FingerprintException_Constructor_SetsFingerprintError()
    {
        var ex = new FingerprintException("fp error", "SELECT 1");
        Assert.Equal("fp error", ex.FingerprintError);
        Assert.Equal("SELECT 1", ex.Query);
        Assert.Contains("fp error", ex.Message);
    }

    [Fact]
    public void FingerprintException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("root");
        var ex = new FingerprintException("fp failed", inner, "SELECT 2");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("fp failed", ex.FingerprintError);
    }

    // ─── DeparseException ───────────────────────────────────────────────────

    [Fact]
    public void DeparseException_Constructor_SetsDeparseError()
    {
        var ex = new DeparseException("deparse error", "tree");
        Assert.Equal("deparse error", ex.DeparseError);
        Assert.Equal("tree", ex.Query);
        Assert.Contains("deparse error", ex.Message);
    }

    [Fact]
    public void DeparseException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("inner");
        var ex = new DeparseException("deparse failed", inner, "tree2");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("deparse failed", ex.DeparseError);
    }

    // ─── SplitException ──────────────────────────────────────────────────────

    [Fact]
    public void SplitException_Constructor_SetsSplitError()
    {
        var ex = new SplitException("split error", "SELECT 1; BAD");
        Assert.Equal("split error", ex.SplitError);
        Assert.Equal("SELECT 1; BAD", ex.Query);
        Assert.Contains("split error", ex.Message);
    }

    [Fact]
    public void SplitException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("inner split");
        var ex = new SplitException("split failed", inner, "SELECT 1");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("split failed", ex.SplitError);
    }

    // ─── ScanException ───────────────────────────────────────────────────────

    [Fact]
    public void ScanException_Constructor_SetsScanError()
    {
        var ex = new ScanException("scan error", "SELECT @@@");
        Assert.Equal("scan error", ex.ScanError);
        Assert.Equal("SELECT @@@", ex.Query);
        Assert.Contains("scan error", ex.Message);
    }

    [Fact]
    public void ScanException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("inner scan");
        var ex = new ScanException("scan failed", inner, "SELECT 1");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("scan failed", ex.ScanError);
    }

    // ─── PlpgsqlParseException ───────────────────────────────────────────────

    [Fact]
    public void PlpgsqlParseException_Constructor_SetsPlpgsqlParseError()
    {
        var ex = new PlpgsqlParseException("plpgsql error", "DO $$ bad $$");
        Assert.Equal("plpgsql error", ex.PlpgsqlParseError);
        Assert.Equal("DO $$ bad $$", ex.Query);
        Assert.Contains("plpgsql error", ex.Message);
    }

    [Fact]
    public void PlpgsqlParseException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("inner plpgsql");
        var ex = new PlpgsqlParseException("plpgsql failed", inner, "DO $$..$$");
        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("plpgsql failed", ex.PlpgsqlParseError);
    }

    // ─── PostgreSqlVersionNotAvailableException ──────────────────────────────

    [Fact]
    public void PostgreSqlVersionNotAvailableException_Constructor_SetsProperties()
    {
        var available = new[] { PostgreSqlVersion.Postgres16 };
        var ex = new PostgreSqlVersionNotAvailableException(
            PostgreSqlVersion.Postgres17,
            available,
            "version 17 not found");

        Assert.Equal(PostgreSqlVersion.Postgres17, ex.RequestedVersion);
        Assert.Contains(PostgreSqlVersion.Postgres16, ex.AvailableVersions);
        Assert.Equal("version 17 not found", ex.Message);
        Assert.IsAssignableFrom<NativeLibraryException>(ex);
    }

    [Fact]
    public void PostgreSqlVersionNotAvailableException_ConstructorWithInnerException_SetsInnerException()
    {
        var inner = new Exception("inner version");
        var available = new[] { PostgreSqlVersion.Postgres16 };
        var ex = new PostgreSqlVersionNotAvailableException(
            PostgreSqlVersion.Postgres17,
            available,
            "version missing",
            inner);

        Assert.Equal(inner, ex.InnerException);
        Assert.Equal(PostgreSqlVersion.Postgres17, ex.RequestedVersion);
    }

    [Fact]
    public void PostgreSqlVersionNotAvailableException_NullAvailableVersions_DefaultsToEmpty()
    {
        var ex = new PostgreSqlVersionNotAvailableException(
            PostgreSqlVersion.Postgres17,
            null!,
            "version missing");

        Assert.NotNull(ex.AvailableVersions);
        Assert.Empty(ex.AvailableVersions);
    }

    // ─── Exception hierarchy ─────────────────────────────────────────────────

    [Fact]
    public void AllCustomExceptions_AreParserExceptions()
    {
        Assert.IsAssignableFrom<ParserException>(new ParseException("e"));
        Assert.IsAssignableFrom<ParserException>(new NativeLibraryException("e"));
        Assert.IsAssignableFrom<ParserException>(new NormalizationException("e"));
        Assert.IsAssignableFrom<ParserException>(new FingerprintException("e"));
        Assert.IsAssignableFrom<ParserException>(new DeparseException("e"));
        Assert.IsAssignableFrom<ParserException>(new SplitException("e"));
        Assert.IsAssignableFrom<ParserException>(new ScanException("e"));
        Assert.IsAssignableFrom<ParserException>(new PlpgsqlParseException("e"));
    }

    [Fact]
    public void PostgreSqlVersionNotAvailableException_IsNativeLibraryException()
    {
        var ex = new PostgreSqlVersionNotAvailableException(
            PostgreSqlVersion.Postgres16,
            Array.Empty<PostgreSqlVersion>(),
            "msg");
        Assert.IsAssignableFrom<NativeLibraryException>(ex);
    }
}
