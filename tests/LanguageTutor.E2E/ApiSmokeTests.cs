using System.Net;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace LanguageTutor.E2E;

[AllureEpic("Language Tutor")]
[AllureFeature("API smoke checks")]
[AllureTag("api")]
[AllureTag("smoke")]
public sealed class ApiSmokeTests
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly string _baseUrl =
        (Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8088").TrimEnd('/');

    [Theory]
    [InlineData("/")]
    [InlineData("/api/health")]
    [InlineData("/api/health/database")]
    [InlineData("/api/courses")]
    [AllureStory("Essential routes")]
    [AllureSeverity(SeverityLevel.critical)]
    public async Task EssentialRoutes_ReturnSuccess(string path)
    {
        using var response = await GetWhenReady(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"{path} returned {(int)response.StatusCode}: {body}");
    }

    private async Task<HttpResponseMessage> GetWhenReady(string path)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        Exception? lastException = null;
        HttpResponseMessage? lastResponse = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                lastResponse?.Dispose();
                lastResponse = await Client.GetAsync($"{_baseUrl}{path}");

                if (lastResponse.StatusCode == HttpStatusCode.OK)
                    return lastResponse;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        lastResponse?.Dispose();
        throw new InvalidOperationException($"Timed out waiting for {_baseUrl}{path}.", lastException);
    }
}
