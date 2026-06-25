using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Report test cases - AI exercise creator")]
[AllureTag("selenium")]
[AllureTag("ui")]
[AllureTag("report-testcase")]
public sealed class ReportAdminAiToolsTests : SeleniumTestBase
{
    [Fact]
    [AllureName("TC17")]
    [AllureStory("Bao cao tao bai tap - TC15")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Form tao bai tap AI khong cho gui khi chua nhap topic.")]
    [AllureIssue("LT-17", Title = "LT-17")]
    public void TC17_AiExerciseCreatorRequiresTopicBeforeSubmit()
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();
            Driver.Navigate().GoToUrl($"{BaseUrl}/admin/ai-tools");

            WaitUntilVisible(By.CssSelector("[data-testid='ai-topic-input']"));
            var submitButton = WaitUntilVisible(By.CssSelector("[data-testid='ai-generate-submit']"));
            Assert.False(submitButton.Enabled);

            Fill(By.CssSelector("[data-testid='ai-topic-input']"), "Office Conversations");

            Wait.Until(_ => Driver.FindElement(By.CssSelector("[data-testid='ai-generate-submit']")).Enabled);
            Assert.True(Driver.FindElement(By.CssSelector("[data-testid='ai-generate-submit']")).Enabled);
        });
    }

    [Fact]
    [AllureName("TC18")]
    [AllureStory("Bao cao tao bai tap - TC8/TC9")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Form tao bai tap AI hien thi cac truong ngon ngu va trinh do bat buoc cho noi dung bai hoc.")]
    [AllureIssue("LT-18", Title = "LT-18")]
    public void TC18_AiExerciseCreatorShowsLanguageAndLevelControls()
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();
            Driver.Navigate().GoToUrl($"{BaseUrl}/admin/ai-tools");

            WaitUntilVisible(By.CssSelector("[data-testid='ai-language-select']"));
            WaitUntilVisible(By.CssSelector("[data-testid='ai-level-select']"));
            WaitUntilVisible(By.CssSelector("[data-testid='ai-topic-input']"));

            SelectByValue(By.CssSelector("[data-testid='ai-language-select']"), "EN");
            SelectByValue(By.CssSelector("[data-testid='ai-level-select']"), "Intermediate");

            Assert.Equal("EN", Driver.FindElement(By.CssSelector("[data-testid='ai-language-select']")).GetDomProperty("value"));
            Assert.Equal("Intermediate", Driver.FindElement(By.CssSelector("[data-testid='ai-level-select']")).GetDomProperty("value"));
        });
    }

    private void LoginAsConfiguredAdmin()
    {
        var email = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@gmail.com";
        var password = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Abc@123";
        var loginPage = new LoginPage(Driver, Wait, BaseUrl);
        loginPage.Open();
        loginPage.Login(email, password);
        WaitForPath("/admin");
        WaitUntilVisible(By.CssSelector("[data-testid='admin-layout']"));
    }
}
