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

    [Fact]
    [AllureName("TC1")]
    [AllureStory("Essential routes")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Kiểm tra frontend root route trả về thành công.")]
    [AllureIssue("LT-1", Title = "LT-1")]
    public Task TC1_FrontendRoot_ReturnsSuccess() =>
        EssentialRoute_ReturnSuccess("/");

    [Fact]
    [AllureName("TC2")]
    [AllureStory("Essential routes")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Kiểm tra API health tổng quát trả về thành công.")]
    [AllureIssue("LT-2", Title = "LT-2")]
    public Task TC2_ApiHealth_ReturnsSuccess() =>
        EssentialRoute_ReturnSuccess("/api/health");

    [Fact]
    [AllureName("TC3")]
    [AllureStory("Essential routes")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Kiểm tra backend kết nối được database test.")]
    [AllureIssue("LT-3", Title = "LT-3")]
    public Task TC3_DatabaseHealth_ReturnsSuccess() =>
        EssentialRoute_ReturnSuccess("/api/health/database");

    [Fact]
    [AllureName("TC4")]
    [AllureStory("Essential routes")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Kiểm tra API danh sách khóa học trả về thành công.")]
    [AllureIssue("LT-4", Title = "LT-4")]
    public Task TC4_CoursesApi_ReturnsSuccess() =>
        EssentialRoute_ReturnSuccess("/api/courses");

    private async Task EssentialRoute_ReturnSuccess(string path)
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
