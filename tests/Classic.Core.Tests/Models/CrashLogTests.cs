using Classic.Core.Models;
using FluentAssertions;
using Xunit;

namespace Classic.Core.Tests.Models;

public class CrashLogTests
{
    [Fact]
    public void CrashLog_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var crashLog = new CrashLog();

        // Assert
        crashLog.FileName.Should().Be(string.Empty);
        crashLog.FilePath.Should().Be(string.Empty);
        crashLog.GameVersion.Should().Be(string.Empty);
        crashLog.CrashGenVersion.Should().Be(string.Empty);
        crashLog.MainError.Should().Be(string.Empty);
        crashLog.RawContent.Should().NotBeNull().And.BeEmpty();
        crashLog.Segments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CrashLog_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var crashLog = new CrashLog();
        var testContent = new List<string> { "line1", "line2" };
        var testSegments = new Dictionary<string, List<string>>
        {
            { "Header", new List<string> { "header1", "header2" } }
        };

        // Act
        crashLog.FileName = "test.txt";
        crashLog.FilePath = @"C:\Test\test.txt";
        crashLog.DateCreated = new DateTime(2025, 1, 1);
        crashLog.GameVersion = "1.6.1170.0";
        crashLog.CrashGenVersion = "1.10.0";
        crashLog.MainError = "Access violation";
        crashLog.RawContent = testContent;
        crashLog.Segments = testSegments;

        // Assert
        crashLog.FileName.Should().Be("test.txt");
        crashLog.FilePath.Should().Be(@"C:\Test\test.txt");
        crashLog.DateCreated.Should().Be(new DateTime(2025, 1, 1));
        crashLog.GameVersion.Should().Be("1.6.1170.0");
        crashLog.CrashGenVersion.Should().Be("1.10.0");
        crashLog.MainError.Should().Be("Access violation");
        crashLog.RawContent.Should().BeEquivalentTo(testContent);
        crashLog.Segments.Should().BeEquivalentTo(testSegments);
    }
}
