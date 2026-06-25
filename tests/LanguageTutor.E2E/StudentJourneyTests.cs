using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Student journey")]
[AllureTag("selenium")]
[AllureTag("ui")]
public sealed class StudentJourneyTests : SeleniumTestBase
{
    [Fact]
    [AllureName("TC5")]
    [AllureStory("Anonymous access")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Người dùng chưa đăng nhập truy cập dashboard phải bị chuyển về trang đăng nhập.")]
    [AllureIssue("LT-5", Title = "LT-5")]
    public void TC5_ProtectedDashboard_RedirectsAnonymousUserToLogin()
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
    [AllureName("TC6")]
    [AllureStory("Registration and login")]
    [AllureSeverity(SeverityLevel.blocker)]
    [AllureDescription("Học viên có thể đăng ký, xem khóa học, đăng xuất và đăng nhập lại.")]
    [AllureIssue("LT-6", Title = "LT-6")]
    public void TC6_StudentCanRegisterBrowseCoursesSignOutAndLoginAgain()
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
