using EasyBluetoothAudio.Services;
using Xunit;

namespace EasyBluetoothAudio.Tests;

public class ProcessServiceTests
{
    private readonly ProcessService _service = new();

    [Theory]
    [InlineData("https://google.com")]
    [InlineData("http://example.com")]
    [InlineData("mailto:test@example.com")]
    [InlineData("ms-settings:bluetooth")]
    [InlineData("MS-SETTINGS:BLUETOOTH")]
    public void IsValidUri_ReturnsTrue_ForWhitelistedSchemes(string uri)
    {
        Assert.True(_service.IsValidUri(uri));
    }

    [Theory]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    [InlineData("C:\\Windows\\explorer.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    [InlineData("cmd.exe")]
    [InlineData("/etc/passwd")]
    public void IsValidUri_ReturnsFalse_ForForbiddenSchemesOrPaths(string uri)
    {
        Assert.False(_service.IsValidUri(uri));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not-a-uri")]
    public void IsValidUri_ReturnsFalse_ForInvalidInput(string? uri)
    {
        Assert.False(_service.IsValidUri(uri!));
    }
}
