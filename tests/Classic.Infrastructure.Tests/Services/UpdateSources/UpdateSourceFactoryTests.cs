using Classic.Core.Interfaces;
using Classic.Infrastructure.Services.UpdateSources;
using FluentAssertions;
using Moq;
using Xunit;

namespace Classic.Infrastructure.Tests.Services.UpdateSources;

public class UpdateSourceFactoryTests
{
    [Fact]
    public void Constructor_WithSources_ShouldRegisterAllSources()
    {
        // Arrange
        var mockSource1 = new Mock<IUpdateSource>();
        mockSource1.Setup(x => x.SourceName).Returns("GitHub");
        mockSource1.Setup(x => x.SupportsPreReleases).Returns(true);

        var mockSource2 = new Mock<IUpdateSource>();
        mockSource2.Setup(x => x.SourceName).Returns("Nexus");
        mockSource2.Setup(x => x.SupportsPreReleases).Returns(false);

        var sources = new[] { mockSource1.Object, mockSource2.Object };

        // Act
        var factory = new UpdateSourceFactory(sources);

        // Assert
        factory.GetAllSources().Should().HaveCount(2);
        factory.GetSource("GitHub").Should().Be(mockSource1.Object);
        factory.GetSource("Nexus").Should().Be(mockSource2.Object);
    }

    [Fact]
    public void GetSource_WithCaseInsensitiveName_ShouldReturnCorrectSource()
    {
        // Arrange
        var mockSource = new Mock<IUpdateSource>();
        mockSource.Setup(x => x.SourceName).Returns("GitHub");

        var factory = new UpdateSourceFactory(new[] { mockSource.Object });

        // Act & Assert
        factory.GetSource("github").Should().Be(mockSource.Object);
        factory.GetSource("GITHUB").Should().Be(mockSource.Object);
        factory.GetSource("GitHub").Should().Be(mockSource.Object);
    }

    [Fact]
    public void GetSource_WithUnknownName_ShouldReturnNull()
    {
        // Arrange
        var factory = new UpdateSourceFactory(Array.Empty<IUpdateSource>());

        // Act & Assert
        factory.GetSource("Unknown").Should().BeNull();
        factory.GetSource("").Should().BeNull();
        factory.GetSource(null!).Should().BeNull();
    }

    [Fact]
    public void GetPreReleaseCapableSources_ShouldReturnOnlySourcesThatSupportPreReleases()
    {
        // Arrange
        var mockGitHub = new Mock<IUpdateSource>();
        mockGitHub.Setup(x => x.SourceName).Returns("GitHub");
        mockGitHub.Setup(x => x.SupportsPreReleases).Returns(true);

        var mockNexus = new Mock<IUpdateSource>();
        mockNexus.Setup(x => x.SourceName).Returns("Nexus");
        mockNexus.Setup(x => x.SupportsPreReleases).Returns(false);

        var factory = new UpdateSourceFactory(new[] { mockGitHub.Object, mockNexus.Object });

        // Act
        var preReleaseSources = factory.GetPreReleaseCapableSources();

        // Assert
        preReleaseSources.Should().HaveCount(1);
        preReleaseSources.Should().Contain(mockGitHub.Object);
        preReleaseSources.Should().NotContain(mockNexus.Object);
    }

    [Fact]
    public void RegisterSource_WithValidSource_ShouldAddToCollection()
    {
        // Arrange
        var factory = new UpdateSourceFactory(Array.Empty<IUpdateSource>());
        var mockSource = new Mock<IUpdateSource>();
        mockSource.Setup(x => x.SourceName).Returns("Custom");

        // Act
        factory.RegisterSource(mockSource.Object);

        // Assert
        factory.GetSource("Custom").Should().Be(mockSource.Object);
        factory.GetAllSources().Should().Contain(mockSource.Object);
    }

    [Fact]
    public void RegisterSource_WithDuplicateName_ShouldReplaceExisting()
    {
        // Arrange
        var mockSource1 = new Mock<IUpdateSource>();
        mockSource1.Setup(x => x.SourceName).Returns("GitHub");

        var mockSource2 = new Mock<IUpdateSource>();
        mockSource2.Setup(x => x.SourceName).Returns("GitHub");

        var factory = new UpdateSourceFactory(new[] { mockSource1.Object });

        // Act
        factory.RegisterSource(mockSource2.Object);

        // Assert
        factory.GetSource("GitHub").Should().Be(mockSource2.Object);
        factory.GetAllSources().Should().HaveCount(1);
    }

    [Fact]
    public void RegisterSource_WithNullSource_ShouldNotThrow()
    {
        // Arrange
        var factory = new UpdateSourceFactory(Array.Empty<IUpdateSource>());

        // Act & Assert
        var act = () => factory.RegisterSource(null!);
        act.Should().NotThrow();
        factory.GetAllSources().Should().BeEmpty();
    }

    [Fact]
    public void RegisterSource_WithEmptySourceName_ShouldNotRegister()
    {
        // Arrange
        var mockSource = new Mock<IUpdateSource>();
        mockSource.Setup(x => x.SourceName).Returns("");

        var factory = new UpdateSourceFactory(Array.Empty<IUpdateSource>());

        // Act
        factory.RegisterSource(mockSource.Object);

        // Assert
        factory.GetAllSources().Should().BeEmpty();
    }
}
