using System.Net;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Infrastructure.Services.UpdateSources;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Classic.Infrastructure.Tests.Services.UpdateSources;

public class GitHubUpdateSourceTests
{
    private readonly Mock<IVersionService> _mockVersionService;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GitHubUpdateSource _gitHubUpdateSource;

    public GitHubUpdateSourceTests()
    {
        _mockVersionService = new Mock<IVersionService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _gitHubUpdateSource = new GitHubUpdateSource(_httpClient, _mockVersionService.Object);
    }

    [Fact]
    public void SourceName_ShouldReturnGitHub()
    {
        // Act & Assert
        _gitHubUpdateSource.SourceName.Should().Be("GitHub");
    }

    [Fact]
    public void SupportsPreReleases_ShouldReturnTrue()
    {
        // Act & Assert
        _gitHubUpdateSource.SupportsPreReleases.Should().BeTrue();
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithValidResponse_ShouldReturnSuccess()
    {
        // Arrange
        var latestResponse = """
            {
                "id": 123,
                "tag_name": "v1.2.3",
                "name": "1.2.3",
                "prerelease": false,
                "published_at": "2023-01-01T00:00:00Z",
                "html_url": "https://github.com/test/test/releases/tag/v1.2.3",
                "body": "Release notes"
            }
            """;

        var allReleasesResponse = $"[{latestResponse}]";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.EndsWith("/releases/latest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(latestResponse)
            });

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.EndsWith("/releases")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(allReleasesResponse)
            });

        // Act
        var result = await _gitHubUpdateSource.GetLatestVersionAsync(false);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Version.Should().NotBeNull();
        result.Version!.Major.Should().Be(1);
        result.Version.Minor.Should().Be(2);
        result.Version.Patch.Should().Be(3);
        result.SourceName.Should().Be("GitHub");
        result.Release.Should().NotBeNull();
        result.Release!.TagName.Should().Be("v1.2.3");
        result.Release.Name.Should().Be("1.2.3");
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNetworkError_ShouldReturnFailure()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _gitHubUpdateSource.GetLatestVersionAsync(false);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Network error");
        result.SourceName.Should().Be("GitHub");
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithPreReleaseOnly_ShouldFilterCorrectly()
    {
        // Arrange
        var preReleaseResponse = """
            {
                "id": 124,
                "tag_name": "v1.3.0-beta1",
                "name": "1.3.0-beta1",
                "prerelease": true,
                "published_at": "2023-01-02T00:00:00Z",
                "html_url": "https://github.com/test/test/releases/tag/v1.3.0-beta1",
                "body": "Beta release"
            }
            """;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.EndsWith("/releases/latest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.EndsWith("/releases")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"[{preReleaseResponse}]")
            });

        // Act - Without pre-releases
        var resultWithoutPreReleases = await _gitHubUpdateSource.GetLatestVersionAsync(false);

        // Assert
        resultWithoutPreReleases.IsSuccess.Should().BeFalse();
        resultWithoutPreReleases.ErrorMessage.Should().Contain("No stable releases found");

        // Act - With pre-releases
        var resultWithPreReleases = await _gitHubUpdateSource.GetLatestVersionAsync(true);

        // Assert
        resultWithPreReleases.IsSuccess.Should().BeTrue();
        resultWithPreReleases.Version.Should().NotBeNull();
        resultWithPreReleases.Version!.Major.Should().Be(1);
        resultWithPreReleases.Version.Minor.Should().Be(3);
        resultWithPreReleases.Version.Patch.Should().Be(0);
        resultWithPreReleases.Version.PreRelease.Should().Be("beta1");
    }
}
