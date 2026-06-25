param(
    [string] $JiraBaseUrl = $env:JIRA_BASE_URL,
    [string] $JiraEmail = $env:JIRA_EMAIL,
    [string] $JiraApiToken = $env:JIRA_API_TOKEN,
    [string] $ProjectKey = $env:JIRA_PROJECT_KEY,
    [string] $IssueType = "Bug"
)

$ErrorActionPreference = "Stop"

function New-AdfParagraph([string] $text) {
    return @{
        type = "paragraph"
        content = @(
            @{
                type = "text"
                text = $text
            }
        )
    }
}

function New-AdfBulletList([string[]] $items) {
    return @{
        type = "bulletList"
        content = @(
            $items | ForEach-Object {
                @{
                    type = "listItem"
                    content = @(
                        (New-AdfParagraph $_)
                    )
                }
            }
        )
    }
}

if ([string]::IsNullOrWhiteSpace($JiraBaseUrl) -or
    [string]::IsNullOrWhiteSpace($JiraEmail) -or
    [string]::IsNullOrWhiteSpace($JiraApiToken) -or
    [string]::IsNullOrWhiteSpace($ProjectKey)) {
    throw "Set JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, and JIRA_PROJECT_KEY before running this script."
}

$sampleBugs = @(
    @{
        Id = "DEMO-BUG-001"
        Summary = "[Demo] Dang nhap sai mat khau hien thong bao chua ro rang"
        Module = "Authentication"
        Severity = "Medium"
        Steps = @(
            "Mo trang Login.",
            "Nhap email hop le va mat khau sai.",
            "Bam Dang nhap."
        )
        Expected = "He thong hien thong bao loi ro rang: email hoac mat khau khong dung."
        Actual = "Thong bao loi chung chung, nguoi dung kho biet nguyen nhan."
    },
    @{
        Id = "DEMO-BUG-002"
        Summary = "[Demo] Form dang ky chua chan so dien thoai sai dinh dang"
        Module = "Registration"
        Severity = "Medium"
        Steps = @(
            "Mo trang Dang ky.",
            "Nhap phone = abc123.",
            "Dien cac truong con lai hop le va submit."
        )
        Expected = "Phone sai dinh dang bi chan tai frontend hoac backend."
        Actual = "Form co the gui request voi phone khong hop le."
    },
    @{
        Id = "DEMO-BUG-003"
        Summary = "[Demo] Admin them nguoi dung thieu ngay sinh van duoc chap nhan"
        Module = "Admin User Management"
        Severity = "Low"
        Steps = @(
            "Dang nhap bang tai khoan admin.",
            "Mo popup Them nguoi dung moi.",
            "Bo trong ngay sinh va luu."
        )
        Expected = "Neu ngay sinh la thong tin bat buoc theo thiet ke, form phai bao loi."
        Actual = "Form khong hien validate ro rang cho ngay sinh."
    },
    @{
        Id = "DEMO-BUG-004"
        Summary = "[Demo] Hoc vien co the mo duong dan admin bang URL truc tiep"
        Module = "Authorization"
        Severity = "High"
        Steps = @(
            "Dang nhap bang tai khoan hoc vien.",
            "Nhap truc tiep /admin tren thanh dia chi.",
            "Quan sat phan hoi cua ung dung."
        )
        Expected = "Hoc vien bi chuyen ve trang khong co quyen hoac dashboard hoc vien."
        Actual = "Can xac minh lai redirect va API guard cho role hoc vien."
    },
    @{
        Id = "DEMO-BUG-005"
        Summary = "[Demo] Tao bai tap AI khong hien ly do khi Gemini bi rate limit"
        Module = "AI Exercise Creator"
        Severity = "Medium"
        Steps = @(
            "Dang nhap admin.",
            "Mo Tao bai tap.",
            "Tao noi dung khi Gemini tra ve 429 hoac 503."
        )
        Expected = "Giao dien hien ly do loi va goi y doi model/thu lai sau."
        Actual = "Thong bao hien chung chung: AI gap su co."
    },
    @{
        Id = "DEMO-BUG-006"
        Summary = "[Demo] Audio TOEIC tra ve URL localhost sau khi deploy"
        Module = "Listening Audio"
        Severity = "High"
        Steps = @(
            "Mo trang Phong nghe tren domain Azure.",
            "Tao audio TOEIC.",
            "Bam nghe thu audio."
        )
        Expected = "Audio URL dung domain hien tai hoac relative path /uploads."
        Actual = "Audio co the tro ve localhost lam trinh duyet bi mixed content/connection refused."
    },
    @{
        Id = "DEMO-BUG-007"
        Summary = "[Demo] Co van hoc tap AI load cham khi du lieu hoc vien lon"
        Module = "AI Learning Advisor"
        Severity = "Medium"
        Steps = @(
            "Dang nhap hoc vien co nhieu lich su hoc.",
            "Mo Co van hoc tap.",
            "Bam Lam moi."
        )
        Expected = "Trang phan hoi nhanh, co loading state va cache phu hop."
        Actual = "Thoi gian load cam nhan cham, can toi uu prompt/cache."
    },
    @{
        Id = "DEMO-BUG-008"
        Summary = "[Demo] Phan tich phat am tra ve 500 khi audio rong hoac loi"
        Module = "Pronunciation"
        Severity = "High"
        Steps = @(
            "Mo trang Phat am.",
            "Gui audio rong hoac file ghi am loi.",
            "Quan sat response API /api/ai/pronunciation/evaluate."
        )
        Expected = "API tra ve 400 voi thong bao file audio khong hop le."
        Actual = "API co the tra ve 500 Internal Server Error."
    },
    @{
        Id = "DEMO-BUG-009"
        Summary = "[Demo] Bao cao Allure chua attach du video cho moi testcase UI"
        Module = "Automated Testing"
        Severity = "Low"
        Steps = @(
            "Chay Selenium Docker.",
            "Generate Allure report.",
            "Mo mot testcase UI bat ky va kiem tra attachments."
        )
        Expected = "Moi testcase UI co screenshot/video khi can debug."
        Actual = "Mot so testcase co the thieu video neu session ket thuc qua nhanh."
    },
    @{
        Id = "DEMO-BUG-010"
        Summary = "[Demo] OWASP ZAP canh bao CSP style-src unsafe-inline"
        Module = "Security"
        Severity = "Low"
        Steps = @(
            "Chay .\\scripts\\run-zap-docker.ps1.",
            "Mo TestResults/zap/zap-report.html.",
            "Kiem tra danh sach warning CSP."
        )
        Expected = "CSP han che inline style bang nonce/hash hoac thiet ke CSS khong can unsafe-inline."
        Actual = "ZAP van canh bao style-src unsafe-inline."
    }
)

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${JiraEmail}:${JiraApiToken}"))
$headers = @{
    Authorization = "Basic $basicAuth"
    Accept = "application/json"
    "Content-Type" = "application/json"
}

$jiraRoot = $JiraBaseUrl.TrimEnd("/")

foreach ($bug in $sampleBugs) {
    $jql = "project = $ProjectKey AND labels = demo-seed-bug AND labels = $($bug.Id.ToLowerInvariant()) ORDER BY created DESC"
    $encodedJql = [Uri]::EscapeDataString($jql)
    $existing = Invoke-RestMethod `
        -Method Get `
        -Uri "$jiraRoot/rest/api/3/search/jql?jql=$encodedJql&maxResults=1&fields=key,summary" `
        -Headers $headers

    if ($existing.issues.Count -gt 0) {
        Write-Host "Skipped existing $($existing.issues[0].key): $($bug.Summary)"
        continue
    }

    $description = @{
        type = "doc"
        version = 1
        content = @(
            (New-AdfParagraph "Demo bug duoc tao de co du lieu cho Jira dashboard va quy trinh CI/CD."),
            (New-AdfParagraph "Module: $($bug.Module)"),
            (New-AdfParagraph "Severity: $($bug.Severity)"),
            (New-AdfParagraph "Steps to reproduce:"),
            (New-AdfBulletList $bug.Steps),
            (New-AdfParagraph "Expected result: $($bug.Expected)"),
            (New-AdfParagraph "Actual result: $($bug.Actual)")
        )
    }

    $body = @{
        fields = @{
            project = @{
                key = $ProjectKey
            }
            summary = $bug.Summary
            description = $description
            issuetype = @{
                name = $IssueType
            }
            labels = @(
                "demo-seed-bug",
                "ci-cd",
                "language-tutor",
                $bug.Id.ToLowerInvariant(),
                $bug.Module.ToLowerInvariant().Replace(" ", "-")
            )
        }
    } | ConvertTo-Json -Depth 20

    $created = Invoke-RestMethod `
        -Method Post `
        -Uri "$jiraRoot/rest/api/3/issue" `
        -Headers $headers `
        -Body $body

    Write-Host "Created $($created.key): $($bug.Summary)"
}
