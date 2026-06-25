using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Report test cases - Login and roles")]
[AllureTag("selenium")]
[AllureTag("ui")]
[AllureTag("report-testcase")]
public sealed class ReportLoginAndRoleTests : SeleniumTestBase
{
    [Fact]
    [AllureName("TC12")]
    [AllureStory("Bao cao dang nhap - TD-DN-01")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Admin dang nhap thanh cong phai duoc dieu huong den khu vuc quan tri.")]
    [AllureIssue("LT-12", Title = "LT-12")]
    public void TC12_AdminLoginRedirectsToAdminDashboard()
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();

            WaitForPath("/admin");
            WaitUntilVisible(By.CssSelector("[data-testid='admin-layout']"));
        });
    }

    [Fact]
    [AllureName("TC13")]
    [AllureStory("Bao cao dang nhap - TD-DN-03")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang nhap sai mat khau phai hien thong bao loi.")]
    [AllureIssue("LT-13", Title = "LT-13")]
    public void TC13_LoginRejectsInvalidCredentials()
    {
        RunWithScreenshot(() =>
        {
            var loginPage = new LoginPage(Driver, Wait, BaseUrl);
            loginPage.Open();
            loginPage.Login("wrong.user@example.test", "WrongPassword@123");

            var error = WaitUntilVisible(By.CssSelector("[data-testid='login-error']"));
            Assert.Contains("khong chinh xac", RemoveDiacritics(error.Text), StringComparison.OrdinalIgnoreCase);
            WaitForPath("/login");
        });
    }

    [Fact]
    [AllureName("TC14")]
    [AllureStory("Bao cao dang nhap - TD-DN-11")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang nhap bo trong mat khau phai bi required validation chan submit.")]
    [AllureIssue("LT-14", Title = "LT-14")]
    public void TC14_LoginRequiresPassword()
    {
        RunWithScreenshot(() =>
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/login");
            Fill(By.Id("email"), "student@example.test");

            Driver.FindElement(By.CssSelector("[data-testid='login-submit']")).Click();

            Assert.True(HasHtmlValidationError(By.Id("password")));
            Assert.NotEmpty(GetHtmlValidationMessage(By.Id("password")));
            WaitForPath("/login");
        });
    }

    private void LoginAsConfiguredAdmin()
    {
        var email = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@gmail.com";
        var password = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Abc@123";
        var loginPage = new LoginPage(Driver, Wait, BaseUrl);
        loginPage.Open();
        loginPage.Login(email, password);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray())
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
