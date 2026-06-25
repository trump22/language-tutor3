using System.Net;
using Allure.Net.Commons;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace LanguageTutor.E2E.Infrastructure;

public abstract class SeleniumTestBase : IDisposable
{
    private static readonly HttpClient HealthClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    protected SeleniumTestBase()
    {
        BaseUrl = (Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8088").TrimEnd('/');
        WaitForApplication();
        Driver = DriverFactory.Create();
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
    }

    protected string BaseUrl { get; }
    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }

    protected void RunWithScreenshot(Action test)
    {
        try
        {
            test();
        }
        catch
        {
            SaveScreenshot();
            throw;
        }
    }

    protected IWebElement WaitUntilVisible(By locator) =>
        Wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(locator);
                return element.Displayed ? element : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        })!;

    protected void WaitForPath(string path) =>
        Wait.Until(driver => new Uri(driver.Url).AbsolutePath.StartsWith(path, StringComparison.OrdinalIgnoreCase));

    protected void Fill(By locator, string value)
    {
        var input = WaitUntilVisible(locator);
        input.Clear();
        input.SendKeys(value);
    }

    protected void SelectByValue(By locator, string value) =>
        new SelectElement(WaitUntilVisible(locator)).SelectByValue(value);

    protected bool HasHtmlValidationError(By locator)
    {
        var element = WaitUntilVisible(locator);
        var isValid = ((IJavaScriptExecutor)Driver)
            .ExecuteScript("return arguments[0].checkValidity();", element) as bool? ?? false;
        return !isValid;
    }

    protected string GetHtmlValidationMessage(By locator)
    {
        var element = WaitUntilVisible(locator);
        return ((IJavaScriptExecutor)Driver)
            .ExecuteScript("return arguments[0].validationMessage;", element) as string ?? string.Empty;
    }

    protected bool ElementExists(By locator)
    {
        try
        {
            return Driver.FindElement(locator).Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    protected void WaitUntilMissing(By locator) =>
        Wait.Until(driver =>
        {
            try
            {
                return !driver.FindElement(locator).Displayed;
            }
            catch (NoSuchElementException)
            {
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        });

    protected static string UniqueEmail(string prefix)
    {
        var suffix = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}"[..20];
        return $"{prefix}.{suffix}@example.test";
    }

    private void WaitForApplication()
    {
        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var frontendResponse = HealthClient.GetAsync($"{BaseUrl}/").GetAwaiter().GetResult();
                using var apiResponse = HealthClient.GetAsync($"{BaseUrl}/api/courses").GetAwaiter().GetResult();
                if (frontendResponse.StatusCode == HttpStatusCode.OK &&
                    apiResponse.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            Thread.Sleep(1500);
        }

        throw new InvalidOperationException($"Ứng dụng không sẵn sàng tại {BaseUrl}.", lastException);
    }

    private void SaveScreenshot()
    {
        if (Driver is not ITakesScreenshot screenshotDriver)
            return;

        var outputDirectory = Environment.GetEnvironmentVariable("E2E_SCREENSHOT_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "TestResults", "screenshots");

        Directory.CreateDirectory(outputDirectory);
        var fileName = $"{GetType().Name}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png";
        var screenshotPath = Path.Combine(outputDirectory, fileName);
        screenshotDriver.GetScreenshot().SaveAsFile(screenshotPath);
        AllureApi.AddAttachment("Failure screenshot", "image/png", screenshotPath);
    }

    public void Dispose()
    {
        var sessionId = Driver is RemoteWebDriver remoteDriver
            ? remoteDriver.SessionId?.ToString()
            : null;

        try
        {
            Driver.Quit();
            AttachSessionVideo(sessionId);
        }
        finally
        {
            Driver.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private static void AttachSessionVideo(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        var videoDirectory = Environment.GetEnvironmentVariable("E2E_VIDEO_DIR")
            ?? "/test-results/videos";

        var videoPath = Path.Combine(videoDirectory, $"{sessionId}.mp4");
        var deadline = DateTime.UtcNow.AddSeconds(15);
        long previousLength = -1;

        while (DateTime.UtcNow < deadline)
        {
            var file = new FileInfo(videoPath);
            if (file.Exists && file.Length > 0)
            {
                if (file.Length == previousLength)
                {
                    AllureApi.AddAttachment("Selenium video", "video/mp4", videoPath);
                    return;
                }

                previousLength = file.Length;
            }

            Thread.Sleep(500);
        }
    }
}
