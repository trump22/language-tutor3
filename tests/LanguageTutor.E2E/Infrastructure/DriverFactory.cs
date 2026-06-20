using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace LanguageTutor.E2E.Infrastructure;

internal static class DriverFactory
{
    public static IWebDriver Create()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1440,1100");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--lang=vi-VN");

        var remoteUrl = Environment.GetEnvironmentVariable("SELENIUM_REMOTE_URL");
        IWebDriver driver = string.IsNullOrWhiteSpace(remoteUrl)
            ? new ChromeDriver(options)
            : new RemoteWebDriver(new Uri(remoteUrl), options);

        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        return driver;
    }
}
