using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace LanguageTutor.E2E.Pages;

internal sealed class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public LoginPage(IWebDriver driver, WebDriverWait wait, string baseUrl)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = baseUrl;
    }

    public void Open() => _driver.Navigate().GoToUrl($"{_baseUrl}/login");

    public bool IsDisplayed() =>
        _wait.Until(driver =>
            driver.FindElement(By.Id("email")).Displayed &&
            driver.FindElement(By.Id("password")).Displayed);

    public void Login(string email, string password)
    {
        var emailInput = _driver.FindElement(By.Id("email"));
        emailInput.Clear();
        emailInput.SendKeys(email);

        var passwordInput = _driver.FindElement(By.Id("password"));
        passwordInput.Clear();
        passwordInput.SendKeys(password);

        _driver.FindElement(By.CssSelector("[data-testid='login-submit']")).Click();
    }
}
