using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using LanguageTutor.E2E.Infrastructure;
using LanguageTutor.E2E.Pages;
using OpenQA.Selenium;

namespace LanguageTutor.E2E;

[Collection("Selenium")]
[AllureEpic("Language Tutor")]
[AllureFeature("Report test case matrix")]
[AllureTag("selenium")]
[AllureTag("ui")]
[AllureTag("report-matrix")]
public sealed class ReportFunctionalMatrixTests : SeleniumTestBase
{
    [Theory]
    [AllureName("DK-TC01..DK-TC16")]
    [AllureStory("Bao cao dang ky - full validation matrix")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Bao phu cac test case dang ky trong bao cao: thanh cong, dieu khoan, truong bat buoc va dinh dang du lieu.")]
    [AllureIssue("LT-DK", Title = "LT-DK")]
    [Trait("Category", "Report53")]
    [InlineData("DK-TC01", "success", "", "")]
    [InlineData("DK-TC02", "terms", "", "dong y")]
    [InlineData("DK-TC03", "skillLevel", "", "control")]
    [InlineData("DK-TC04", "languagePreference", "", "control")]
    [InlineData("DK-TC05", "dateOfBirth", "2999-01-01", "html5")]
    [InlineData("DK-TC06", "dateOfBirth", "", "html5")]
    [InlineData("DK-TC07", "password", "abc12345", "mat khau")]
    [InlineData("DK-TC08", "password", "", "html5")]
    [InlineData("DK-TC09", "address", "@#$", "dia chi")]
    [InlineData("DK-TC10", "address", "", "html5")]
    [InlineData("DK-TC11", "phoneNumber", "091234567", "so dien thoai")]
    [InlineData("DK-TC12", "phoneNumber", "", "html5")]
    [InlineData("DK-TC13", "email", "test_studentgmail.com", "html5")]
    [InlineData("DK-TC14", "email", "", "html5")]
    [InlineData("DK-TC15", "name", "2311", "ho va ten")]
    [InlineData("DK-TC16", "name", "", "html5")]
    public void SignUpReportMatrix(string caseId, string field, string value, string expected)
    {
        RunWithScreenshot(() =>
        {
            OpenSignUp();
            FillValidSignUpFields(UniqueEmail($"signup.{caseId.ToLowerInvariant()}"));

            if (field == "terms")
            {
                Submit(By.CssSelector("[data-testid='signup-submit']"));
                AssertErrorContains(By.CssSelector("[data-testid='signup-error']"), expected);
                return;
            }

            AcceptTerms();

            if (field == "success")
            {
                Submit(By.CssSelector("[data-testid='signup-submit']"));
                WaitForPath("/dashboard");
                WaitUntilVisible(By.CssSelector("[data-testid='student-layout']"));
                return;
            }

            if (expected == "control")
            {
                var element = Driver.FindElement(By.Id(field));
                Assert.False(string.IsNullOrWhiteSpace(element.GetDomProperty("value")));
                return;
            }

            if (field == "dateOfBirth" && value.StartsWith("2999", StringComparison.Ordinal))
            {
                var dateInput = Driver.FindElement(By.Id(field));
                var isValid = ((IJavaScriptExecutor)Driver)
                    .ExecuteScript("arguments[0].value = arguments[1]; return arguments[0].checkValidity();", dateInput, value) as bool? ?? true;
                Assert.False(isValid, $"{caseId} should be blocked by max date validation.");
                return;
            }

            Fill(By.Id(field), value);
            Submit(By.CssSelector("[data-testid='signup-submit']"));

            if (expected == "html5")
            {
                Assert.True(HasHtmlValidationError(By.Id(field)), $"{caseId} should be blocked by HTML5 validation.");
                WaitForPath("/signup");
                return;
            }

            AssertErrorContains(By.CssSelector("[data-testid='signup-error']"), expected);
        });
    }

    [Theory]
    [AllureName("DN-TC01..DN-TC20")]
    [AllureStory("Bao cao dang nhap - equivalence and decision table")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Bao phu dang nhap thanh cong, sai mat khau, sai email, bo trong truong va checkbox ghi nho dang nhap.")]
    [AllureIssue("LT-DN", Title = "LT-DN")]
    [Trait("Category", "Report53")]
    [InlineData("TD-DN-01", "admin", "valid", true, "success-admin")]
    [InlineData("TD-DN-02", "admin", "valid", false, "success-admin")]
    [InlineData("TD-DN-03", "admin", "long-lower-no-symbol", false, "backend-error")]
    [InlineData("TD-DN-04", "admin", "long-symbol-lower", false, "backend-error")]
    [InlineData("TD-DN-05", "admin", "long-symbol-upper", false, "backend-error")]
    [InlineData("TD-DN-06", "admin", "long-upper-no-symbol", false, "backend-error")]
    [InlineData("TD-DN-07", "admin", "short-lower-no-symbol", false, "backend-error")]
    [InlineData("TD-DN-08", "admin", "short-symbol-lower", false, "backend-error")]
    [InlineData("TD-DN-09", "admin", "short-symbol-upper", false, "backend-error")]
    [InlineData("TD-DN-10", "admin", "short-upper-no-symbol", false, "backend-error")]
    [InlineData("TD-DN-11", "admin", "", false, "html5-password")]
    [InlineData("TD-DN-12", "short-email", "", false, "html5-email")]
    [InlineData("TD-DN-13", "", "", true, "html5-email")]
    [InlineData("TD-DN-14", "", "", false, "html5-email")]
    [InlineData("QD-DN-01", "admin", "valid", true, "success-admin")]
    [InlineData("QD-DN-02", "admin", "valid", false, "success-admin")]
    [InlineData("QD-DN-03", "admin", "short-symbol-lower", false, "backend-error")]
    [InlineData("QD-DN-04", "admin", "", false, "html5-password")]
    [InlineData("QD-DN-05", "short-email", "", false, "html5-email")]
    [InlineData("QD-DN-06", "", "", false, "html5-email")]
    public void LoginReportMatrix(string caseId, string emailMode, string passwordMode, bool remember, string expected)
    {
        RunWithScreenshot(() =>
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/login");
            WaitUntilVisible(By.Id("email"));

            var email = emailMode switch
            {
                "admin" => Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@gmail.com",
                "unknown" => UniqueEmail($"login.{caseId.ToLowerInvariant()}"),
                "short-email" => "qa",
                "invalid-email" => "invalid-login-email",
                _ => string.Empty
            };

            var password = passwordMode switch
            {
                "valid" => Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Abc@123",
                "long-lower-no-symbol" => "minhanhnguyen1998lovecoding2024",
                "long-symbol-lower" => "minhanh@nguyen1998!lovecoding#24",
                "long-symbol-upper" => "SuperSecurePasswordOver30Characters!",
                "long-upper-no-symbol" => "MyStrongPassword2024SecureLoginTest01",
                "short-lower-no-symbol" => "abc123",
                "short-symbol-lower" => "abc@12",
                "short-symbol-upper" => "Hi!2#X",
                "short-upper-no-symbol" => "Demo99",
                "wrong" => "WrongPassword@123",
                _ => string.Empty
            };

            Fill(By.Id("email"), email);
            Fill(By.Id("password"), password);
            if (remember)
                Driver.FindElement(By.Id("remember-me")).Click();

            Submit(By.CssSelector("[data-testid='login-submit']"));

            switch (expected)
            {
                case "success-admin":
                    WaitForPath("/admin");
                    WaitUntilVisible(By.CssSelector("[data-testid='admin-layout']"));
                    break;
                case "html5-email":
                    Assert.True(HasHtmlValidationError(By.Id("email")), $"{caseId} should be blocked by email validation.");
                    WaitForPath("/login");
                    break;
                case "html5-password":
                    Assert.True(HasHtmlValidationError(By.Id("password")), $"{caseId} should be blocked by password validation.");
                    WaitForPath("/login");
                    break;
                default:
                    AssertErrorContains(By.CssSelector("[data-testid='login-error']"), "khong chinh xac");
                    WaitForPath("/login");
                    break;
            }
        });
    }

    [Theory]
    [AllureName("ADU-TC01..ADU-TC17")]
    [AllureStory("Bao cao them nguoi dung - full validation matrix")]
    [AllureSeverity(SeverityLevel.critical)]
    [AllureDescription("Bao phu modal them nguoi dung: tao thanh cong, truong bat buoc, dinh dang email va cac truong tuy chon.")]
    [AllureIssue("LT-ADU", Title = "LT-ADU")]
    [Trait("Category", "Report53")]
    [InlineData("ADU-TC01", "success", "", "success")]
    [InlineData("ADU-TC02", "skillLevel", "", "control")]
    [InlineData("ADU-TC03", "languagePreference", "", "control")]
    [InlineData("ADU-TC04", "role", "", "control")]
    [InlineData("ADU-TC05", "gender", "MALE", "control")]
    [InlineData("ADU-TC06", "address", "@#$", "optional")]
    [InlineData("ADU-TC07", "address", "", "optional")]
    [InlineData("ADU-TC08", "dateOfBirth", "2999-01-01", "optional")]
    [InlineData("ADU-TC09", "dateOfBirth", "", "optional")]
    [InlineData("ADU-TC10", "phoneNumber", "091234567", "optional")]
    [InlineData("ADU-TC11", "phoneNumber", "", "optional")]
    [InlineData("ADU-TC12", "password", "Adm", "backend-error")]
    [InlineData("ADU-TC13", "password", "", "html5")]
    [InlineData("ADU-TC14", "email", "tuan_decorshoppe.vn", "html5")]
    [InlineData("ADU-TC15", "email", "", "html5")]
    [InlineData("ADU-TC16", "name", "23123", "optional")]
    [InlineData("ADU-TC17", "name", "", "html5")]
    public void AdminAddUserReportMatrix(string caseId, string field, string value, string expected)
    {
        RunWithScreenshot(() =>
        {
            LoginAsConfiguredAdmin();
            OpenAddUserModal();

            FillValidAdminUserFields(UniqueEmail($"admin.{caseId.ToLowerInvariant()}"));

            if (expected == "success")
            {
                Submit(By.CssSelector("[data-testid='admin-add-user-submit']"));
                WaitUntilMissing(By.CssSelector("[data-testid='admin-add-user-modal']"));
                return;
            }

            if (expected == "control")
            {
                var selector = AdminUserSelector(field);
                if (field == "gender")
                    SelectByValue(selector, value);
                Assert.NotNull(Driver.FindElement(selector));
                return;
            }

            Fill(AdminUserSelector(field), value);

            if (expected == "optional")
            {
                if (field == "dateOfBirth")
                {
                    Assert.NotNull(Driver.FindElement(AdminUserSelector(field)));
                    Assert.True(ElementExists(By.CssSelector("[data-testid='admin-add-user-modal']")));
                    return;
                }

                Assert.Equal(value, Driver.FindElement(AdminUserSelector(field)).GetDomProperty("value"));
                Assert.True(ElementExists(By.CssSelector("[data-testid='admin-add-user-modal']")));
                return;
            }

            Submit(By.CssSelector("[data-testid='admin-add-user-submit']"));

            if (expected == "html5")
            {
                Assert.True(HasHtmlValidationError(AdminUserSelector(field)), $"{caseId} should be blocked by HTML5 validation.");
                Assert.True(ElementExists(By.CssSelector("[data-testid='admin-add-user-modal']")));
                return;
            }

            if (expected == "backend-error")
            {
                Assert.True(
                    ElementExists(By.CssSelector("[data-testid='admin-add-user-error']")) ||
                    ElementExists(By.CssSelector("[data-testid='admin-add-user-modal']")));
                return;
            }
        });
    }

    [Theory]
    [AllureName("AI-TC01..AI-TC18")]
    [AllureStory("Bao cao tao bai tap AI - full available UI matrix")]
    [AllureSeverity(SeverityLevel.normal)]
    [AllureDescription("Bao phu form tao bai tap AI theo cac dieu kien co trong ung dung hien tai: khoa hoc, ngon ngu, trinh do, topic va khoa hoc moi.")]
    [AllureIssue("LT-AI", Title = "LT-AI")]
    [InlineData("AI-TC01", "topic", "Office Conversations", "enabled")]
    [InlineData("AI-TC02", "topic", "", "disabled")]
    [InlineData("AI-TC03", "language", "EN", "selected")]
    [InlineData("AI-TC04", "language", "ZH", "selected")]
    [InlineData("AI-TC05", "level", "Beginner", "selected")]
    [InlineData("AI-TC06", "level", "Intermediate", "selected")]
    [InlineData("AI-TC07", "level", "Advanced", "selected")]
    [InlineData("AI-TC08", "course", "", "present")]
    [InlineData("AI-TC09", "newCourse", "", "present")]
    [InlineData("AI-TC10", "newCourseTitle", "Giao tiep hang ngay", "present")]
    [InlineData("AI-TC11", "newCourseDescription", "Luyen hoi thoai cong viec", "present")]
    [InlineData("AI-TC12", "topic", "@#!!!", "enabled")]
    [InlineData("AI-TC13", "topic", "Goi ca phe", "enabled")]
    [InlineData("AI-TC14", "topic", "Travel plans", "enabled")]
    [InlineData("AI-TC15", "topic", "Business email", "enabled")]
    [InlineData("AI-TC16", "topic", "Airport delay", "enabled")]
    [InlineData("AI-TC17", "topic", "Chinese greeting", "enabled")]
    [InlineData("AI-TC18", "topic", "TOEIC Part 1", "enabled")]
    public void AiExerciseCreatorReportMatrix(string caseId, string field, string value, string expected)
    {
        RunWithScreenshot(() =>
        {
            Assert.False(string.IsNullOrWhiteSpace(caseId));
            LoginAsConfiguredAdmin();
            Driver.Navigate().GoToUrl($"{BaseUrl}/admin/ai-tools");
            WaitUntilVisible(By.CssSelector("[data-testid='ai-topic-input']"));

            switch (field)
            {
                case "topic":
                    Fill(By.CssSelector("[data-testid='ai-topic-input']"), value);
                    Assert.Equal(expected == "enabled", Driver.FindElement(By.CssSelector("[data-testid='ai-generate-submit']")).Enabled);
                    break;
                case "language":
                    SelectByValue(By.CssSelector("[data-testid='ai-language-select']"), value);
                    Assert.Equal(value, Driver.FindElement(By.CssSelector("[data-testid='ai-language-select']")).GetDomProperty("value"));
                    break;
                case "level":
                    SelectByValue(By.CssSelector("[data-testid='ai-level-select']"), value);
                    Assert.Equal(value, Driver.FindElement(By.CssSelector("[data-testid='ai-level-select']")).GetDomProperty("value"));
                    break;
                case "course":
                    WaitUntilVisible(By.CssSelector("[data-testid='ai-course-select']"));
                    break;
                case "newCourse":
                    Driver.FindElement(By.XPath("//button[contains(., '+')]")).Click();
                    Assert.Contains("admin/ai-tools", Driver.Url);
                    break;
                case "newCourseTitle":
                    Driver.FindElement(By.XPath("//button[contains(., '+')]")).Click();
                    var title = Wait.Until(driver => driver.FindElements(By.CssSelector("input")).FirstOrDefault(input => input.GetDomProperty("placeholder")?.Contains("Giao") == true));
                    Assert.NotNull(title);
                    title!.SendKeys(value);
                    Assert.Equal(value, title.GetDomProperty("value"));
                    break;
                case "newCourseDescription":
                    Driver.FindElement(By.XPath("//button[contains(., '+')]")).Click();
                    var description = WaitUntilVisible(By.CssSelector("textarea"));
                    description.SendKeys(value);
                    Assert.Equal(value, description.GetDomProperty("value"));
                    break;
            }
        });
    }

    private void OpenSignUp()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/signup");
        WaitUntilVisible(By.Id("email"));
    }

    private void FillValidSignUpFields(string email)
    {
        Fill(By.Id("name"), "Nguyen Van Test");
        Fill(By.Id("email"), email);
        Fill(By.Id("phoneNumber"), "0912345678");
        Fill(By.Id("address"), "Thai Nguyen");
        Fill(By.Id("password"), "Pass123!");
        Fill(By.Id("dateOfBirth"), "2000-08-15");
        SelectByValue(By.Id("languagePreference"), "en");
        SelectByValue(By.Id("skillLevel"), "beginner");
    }

    private void AcceptTerms() =>
        Driver.FindElement(By.CssSelector("[data-testid='accept-terms']")).Click();

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

    private void OpenAddUserModal()
    {
        WaitUntilVisible(By.CssSelector("[data-testid='admin-add-user-open']")).Click();
        WaitUntilVisible(By.CssSelector("[data-testid='admin-add-user-modal']"));
    }

    private void FillValidAdminUserFields(string email)
    {
        Fill(By.CssSelector("[data-testid='admin-user-name']"), "Tran Tuan Matrix");
        Fill(By.CssSelector("[data-testid='admin-user-email']"), email);
        Fill(By.CssSelector("[data-testid='admin-user-password']"), "Admin@123!");
        Fill(By.CssSelector("[data-testid='admin-user-phone']"), "0912345678");
        Fill(By.CssSelector("[data-testid='admin-user-dob']"), "1990-11-20");
        SelectByValue(By.CssSelector("[data-testid='admin-user-gender']"), "MALE");
        Fill(By.CssSelector("[data-testid='admin-user-address']"), "Quyet Thang, Thai Nguyen");
        SelectByValue(By.CssSelector("[data-testid='admin-user-role']"), "STUDENT");
        SelectByValue(By.CssSelector("[data-testid='admin-user-language']"), "EN");
        SelectByValue(By.CssSelector("[data-testid='admin-user-level']"), "Beginner");
        Fill(By.CssSelector("[data-testid='admin-user-goal']"), "Luyen giao tiep");
    }

    private static By AdminUserSelector(string field) => field switch
    {
        "name" => By.CssSelector("[data-testid='admin-user-name']"),
        "email" => By.CssSelector("[data-testid='admin-user-email']"),
        "password" => By.CssSelector("[data-testid='admin-user-password']"),
        "phoneNumber" => By.CssSelector("[data-testid='admin-user-phone']"),
        "dateOfBirth" => By.CssSelector("[data-testid='admin-user-dob']"),
        "gender" => By.CssSelector("[data-testid='admin-user-gender']"),
        "address" => By.CssSelector("[data-testid='admin-user-address']"),
        "role" => By.CssSelector("[data-testid='admin-user-role']"),
        "languagePreference" => By.CssSelector("[data-testid='admin-user-language']"),
        "skillLevel" => By.CssSelector("[data-testid='admin-user-level']"),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    private void Submit(By selector) =>
        Driver.FindElement(selector).Click();

    private void AssertErrorContains(By selector, string expected)
    {
        var error = WaitUntilVisible(selector);
        Assert.Contains(expected, Normalize(error.Text), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray())
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Replace('Ä', 'A');
    }
}
