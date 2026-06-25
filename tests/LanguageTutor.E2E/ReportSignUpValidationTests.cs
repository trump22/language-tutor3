using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Report test cases - Sign up")]
[AllureTag("selenium")]
[AllureTag("ui")]
[AllureTag("report-testcase")]
public sealed class ReportSignUpValidationTests : SeleniumTestBase
{
    [Fact]
    [AllureName("TC7")]
    [AllureStory("Bao cao dang ky - TC2")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang ky khong tick dong y dieu khoan phai hien thong bao loi.")]
    [AllureIssue("LT-7", Title = "LT-7")]
    public void TC7_SignUpRequiresTermsAgreement()
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields(UniqueEmail("signup.terms"));

            Driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();

            var error = WaitUntilVisible(By.CssSelector("[data-testid='signup-error']"));
            Assert.Contains("dong y", RemoveDiacritics(error.Text), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    [AllureName("TC8")]
    [AllureStory("Bao cao dang ky - TC13")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang ky voi email sai dinh dang phai bi HTML5 validation chan submit.")]
    [AllureIssue("LT-8", Title = "LT-8")]
    public void TC8_SignUpRejectsInvalidEmailFormat()
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields("invalid-email-format");
            AcceptTerms();

            Driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();

            Assert.True(HasHtmlValidationError(By.Id("email")));
            Assert.NotEmpty(GetHtmlValidationMessage(By.Id("email")));
            Assert.EndsWith("/signup", new Uri(Driver.Url).AbsolutePath, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    [AllureName("TC9")]
    [AllureStory("Bao cao dang ky - TC7")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang ky voi mat khau ngan/yeu phai hien thong bao mat khau khong hop le.")]
    [AllureIssue("LT-9", Title = "LT-9")]
    public void TC9_SignUpRejectsWeakPassword()
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields(UniqueEmail("signup.password"), password: "123");
            AcceptTerms();

            Driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();

            var error = WaitUntilVisible(By.CssSelector("[data-testid='signup-error']"));
            Assert.Contains("mat khau", RemoveDiacritics(error.Text), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    [AllureName("TC10")]
    [AllureStory("Bao cao dang ky - TC11")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang ky voi so dien thoai sai dinh dang phai hien thong bao so dien thoai khong hop le.")]
    [AllureIssue("LT-10", Title = "LT-10")]
    public void TC10_SignUpRejectsInvalidPhoneNumber()
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields(UniqueEmail("signup.phone"), phoneNumber: "091234567");
            AcceptTerms();

            Driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();

            var error = WaitUntilVisible(By.CssSelector("[data-testid='signup-error']"));
            Assert.Contains("so dien thoai", RemoveDiacritics(error.Text), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    [AllureName("TC11")]
    [AllureStory("Bao cao dang ky - TC16")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Dang ky bo trong ho ten phai bi required validation chan submit.")]
    [AllureIssue("LT-11", Title = "LT-11")]
    public void TC11_SignUpRequiresFullName()
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields(UniqueEmail("signup.name"), name: "");
            AcceptTerms();

            Driver.FindElement(By.CssSelector("[data-testid='signup-submit']")).Click();

            Assert.True(HasHtmlValidationError(By.Id("name")));
            Assert.NotEmpty(GetHtmlValidationMessage(By.Id("name")));
            Assert.EndsWith("/signup", new Uri(Driver.Url).AbsolutePath, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void OpenSignUp()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/signup");
        WaitUntilVisible(By.Id("email"));
    }

    private void FillValidSignUpFields(
        string email,
        string name = "Nguyen Van Test",
        string phoneNumber = "0912345678",
        string address = "Thai Nguyen",
        string password = "Pass123!",
        string dateOfBirth = "2000-08-15")
    {
        Fill(By.Id("name"), name);
        Fill(By.Id("email"), email);
        Fill(By.Id("phoneNumber"), phoneNumber);
        Fill(By.Id("address"), address);
        Fill(By.Id("password"), password);
        Fill(By.Id("dateOfBirth"), dateOfBirth);
        SelectByValue(By.Id("languagePreference"), "en");
        SelectByValue(By.Id("skillLevel"), "beginner");
    }

    private void AcceptTerms() =>
        Driver.FindElement(By.CssSelector("[data-testid='accept-terms']")).Click();

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
