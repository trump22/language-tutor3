using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Report test cases - Admin users")]
[AllureTag("selenium")]
[AllureTag("ui")]
[AllureTag("report-testcase")]
public sealed class ReportAdminUserTests : SeleniumTestBase
{
    [Fact]
    [AllureName("TC15")]
    [AllureStory("Bao cao them nguoi dung - TC1")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Admin co the tao hoc vien moi tu modal Them nguoi dung.")]
    [AllureIssue("LT-15", Title = "LT-15")]
    public void TC15_AdminCanCreateStudentFromUserModal()
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();
            OpenAddUserModal();

            var email = UniqueEmail("admin.create.student");
            Fill(By.CssSelector("[data-testid='admin-user-name']"), "Tran Tuan Selenium");
            Fill(By.CssSelector("[data-testid='admin-user-email']"), email);
            Fill(By.CssSelector("[data-testid='admin-user-password']"), "Admin@123!");
            Fill(By.CssSelector("[data-testid='admin-user-phone']"), "0912345678");
            Fill(By.CssSelector("[data-testid='admin-user-dob']"), "1990-11-20");
            SelectByValue(By.CssSelector("[data-testid='admin-user-gender']"), "MALE");
            Fill(By.CssSelector("[data-testid='admin-user-address']"), "Quyet Thang, Thai Nguyen");
            SelectByValue(By.CssSelector("[data-testid='admin-user-role']"), "STUDENT");
            SelectByValue(By.CssSelector("[data-testid='admin-user-language']"), "EN");
            SelectByValue(By.CssSelector("[data-testid='admin-user-level']"), "Beginner");
            Fill(By.CssSelector("[data-testid='admin-user-goal']"), "Luyen giao tiep va TOEIC");

            Driver.FindElement(By.CssSelector("[data-testid='admin-add-user-submit']")).Click();

            WaitUntilMissing(By.CssSelector("[data-testid='admin-add-user-modal']"));
            Assert.False(ElementExists(By.CssSelector("[data-testid='admin-add-user-error']")));
        });
    }

    [Fact]
    [AllureName("TC16")]
    [AllureStory("Bao cao them nguoi dung - TC14")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Admin nhap email sai dinh dang trong modal them nguoi dung phai bi HTML5 validation chan submit.")]
    [AllureIssue("LT-16", Title = "LT-16")]
    public void TC16_AdminAddUserRejectsInvalidEmail()
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();
            OpenAddUserModal();

            Fill(By.CssSelector("[data-testid='admin-user-name']"), "Tran Tuan Selenium");
            Fill(By.CssSelector("[data-testid='admin-user-email']"), "invalid-admin-user-email");
            Fill(By.CssSelector("[data-testid='admin-user-password']"), "Admin@123!");

            Driver.FindElement(By.CssSelector("[data-testid='admin-add-user-submit']")).Click();

            Assert.True(HasHtmlValidationError(By.CssSelector("[data-testid='admin-user-email']")));
            Assert.NotEmpty(GetHtmlValidationMessage(By.CssSelector("[data-testid='admin-user-email']")));
            Assert.True(ElementExists(By.CssSelector("[data-testid='admin-add-user-modal']")));
        });
    }

    private void OpenAddUserModal()
    {
        WaitForPath("/admin");
        WaitUntilVisible(By.CssSelector("[data-testid='admin-layout']"));
        WaitUntilVisible(By.CssSelector("[data-testid='admin-add-user-open']")).Click();
        WaitUntilVisible(By.CssSelector("[data-testid='admin-add-user-modal']"));
    }

    private void LoginAsConfiguredAdmin()
    {
        var email = Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@gmail.com";
        var password = Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Abc@123";
        var loginPage = new LoginPage(Driver, Wait, BaseUrl);
        loginPage.Open();
        loginPage.Login(email, password);
    }
}
