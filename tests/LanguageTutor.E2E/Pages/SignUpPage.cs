using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace LanguageTutor.E2E.Pages;

internal sealed class SignUpPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    public SignUpPage(IWebDriver driver, string baseUrl)
    {
        _driver = driver;
        _baseUrl = baseUrl;
    }

    public void Open() => _driver.Navigate().GoToUrl($"{_baseUrl}/signup");

    public void Register(StudentRegistration student)
    {
        Fill("name", student.Name);
        Fill("email", student.Email);
        Fill("phoneNumber", student.PhoneNumber);
        Fill("address", student.Address);
        Fill("password", student.Password);
        Fill("dateOfBirth", student.DateOfBirth);

        new SelectElement(_driver.FindElement(By.Id("languagePreference")))
            .SelectByValue(student.LanguagePreference);
        new SelectElement(_driver.FindElement(By.Id("skillLevel")))
            .SelectByValue(student.SkillLevel);

        _driver.FindElement(By.CssSelector("[data-testid='accept-terms']")).Click();
        _driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();
    }

    private void Fill(string id, string value)
    {
        var input = _driver.FindElement(By.Id(id));
        input.Clear();
        input.SendKeys(value);
    }
}

internal sealed record StudentRegistration(
    string Name,
    string Email,
    string PhoneNumber,
    string Address,
    string Password,
    string DateOfBirth,
    string LanguagePreference = "zh",
    string SkillLevel = "beginner");
