using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
public sealed class StudentJourneyTests : SeleniumTestBase
{
    [Fact]
    public void ProtectedDashboard_RedirectsAnonymousUserToLogin()
    {
        RunWithScreenshot(() =>
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/dashboard");

            WaitForPath("/login");
            var loginPage = new LoginPage(Driver, Wait, BaseUrl);
            Assert.True(loginPage.IsDisplayed());
        });
    }

    [Fact]
    public void StudentCanRegisterBrowseCoursesSignOutAndLoginAgain()
    {
        RunWithScreenshot(() =>
        {
            var uniqueSuffix = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}"[..20];
            var email = $"selenium.{uniqueSuffix}@example.test";
            const string password = "Selenium@123456";

            var signUpPage = new SignUpPage(Driver, BaseUrl);
            signUpPage.Open();
            signUpPage.Register(new StudentRegistration(
                "Hoc Vien Selenium",
                email,
                "0901234567",
                "Ha Noi",
                password,
                "2000-01-15"));

            WaitForPath("/dashboard");
            WaitUntilVisible(By.CssSelector("[data-testid='student-layout']"));

            Driver.FindElement(By.CssSelector("a[href='/courses']")).Click();
            WaitForPath("/courses");
            WaitUntilVisible(By.CssSelector("[data-testid='courses-page']"));

            Driver.FindElement(By.CssSelector("[data-testid='logout-submit']")).Click();
            WaitForPath("/login");

            var loginPage = new LoginPage(Driver, Wait, BaseUrl);
            loginPage.Login(email, password);

            WaitForPath("/dashboard");
            WaitUntilVisible(By.CssSelector("[data-testid='student-layout']"));
        });
    }
}
