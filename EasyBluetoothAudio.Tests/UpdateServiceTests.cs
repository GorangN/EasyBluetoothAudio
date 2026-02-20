using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using EasyBluetoothAudio.Services;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Unit tests for <see cref="UpdateService"/> covering version comparison, asset selection,
/// and graceful error handling.
/// </summary>
public class UpdateServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpClient"/> whose handler returns the given JSON body
    /// with the given status code.
    /// </summary>
    private static HttpClient MakeHttpClient(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content    = new StringContent(json, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object);
    }

    private static string BuildReleaseJson(string tag, string assetName = "EasyBluetoothAudioSetup.exe")
    {
        return JsonSerializer.Serialize(new
        {
            tag_name = tag,
            body     = "Release notes",
            assets   = new[]
            {
                new { name = assetName, browser_download_url = $"https://example.com/{assetName}" }
            }
        });
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenCurrentVersionIsLatest()
    {
        // The GitHub release tag matches the local assembly version (0.0.0 in tests)
        var json   = BuildReleaseJson("v0.0.0");
        var svc    = new UpdateService(MakeHttpClient(json));

        var result = await svc.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsUpdateInfo_WhenNewerVersionExists()
    {
        // Any version higher than the 0.0.0 the test assembly reports
        var json   = BuildReleaseJson("v99.0.0");
        var svc    = new UpdateService(MakeHttpClient(json));

        var result = await svc.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal("v99.0.0", result!.TagName);
        Assert.Equal("99.0.0", result.Version);
        Assert.Contains("EasyBluetoothAudioSetup.exe", result.InstallerUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_OnHttpError()
    {
        // Non-200 response should be handled gracefully — no exception thrown
        var svc    = new UpdateService(MakeHttpClient("{}", HttpStatusCode.InternalServerError));

        var result = await svc.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ThrowsException_On403Forbidden()
    {
        var svc = new UpdateService(MakeHttpClient("{}", HttpStatusCode.Forbidden));

        var ex = await Assert.ThrowsAsync<Exception>(() => svc.CheckForUpdateAsync());

        Assert.Equal("Rate limit exceeded", ex.Message);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenNoExeAsset()
    {
        // Release exists but has no .exe asset (e.g. only source archives)
        var json = JsonSerializer.Serialize(new
        {
            tag_name = "v99.0.0",
            body     = "Notes",
            assets   = new[]
            {
                new { name = "source.zip", browser_download_url = "https://example.com/source.zip" }
            }
        });
        var svc    = new UpdateService(MakeHttpClient(json));

        var result = await svc.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_HandlesPreReleaseSuffix_Correctly()
    {
        // Use a very high version number to ensure it's always greater than the local
        // version reported by the test runner, making the test deterministic.
        var json   = BuildReleaseJson("v999.0.0-beta.1");
        var svc    = new UpdateService(MakeHttpClient(json));

        var result = await svc.CheckForUpdateAsync();

        // 999.0.0 > local version, so an update should be detected
        Assert.NotNull(result);
        Assert.Equal("v999.0.0-beta.1", result!.TagName);
        // The stored version strips the pre-release suffix
        Assert.Equal("999.0.0", result.Version);
    }
}
