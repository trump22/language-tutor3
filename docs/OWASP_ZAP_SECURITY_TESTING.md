# OWASP ZAP Security Testing

OWASP ZAP duoc tich hop de kiem thu bao mat tu dong trong CI/CD. ZAP khong thay the Selenium; Selenium kiem tra luong chuc nang, con ZAP kiem tra cac canh bao bao mat cua ung dung web.

## Kiem thu gi?

Pipeline hien tai dung ZAP Baseline Scan:

- Quet thu dong ung dung sau khi Docker da khoi dong PostgreSQL, backend va frontend.
- Kiem tra cac canh bao pho bien nhu thieu security headers, mixed content, cookie/caching khong an toan, loi cau hinh HTTP.
- Sinh bao cao HTML, JSON va Markdown.

Baseline Scan phu hop voi CI vi it tac dong den du lieu. Neu can quet manh hon, co the them ZAP Full Scan/Active Scan sau.

## Chay local

Tai thu muc goc project:

```powershell
.\scripts\run-zap-docker.ps1
```

Ket qua nam tai:

```text
TestResults\zap\zap-report.html
TestResults\zap\zap-report.json
TestResults\zap\zap-report.md
```

Mo file HTML bang trinh duyet de xem bao cao:

```powershell
Start-Process .\TestResults\zap\zap-report.html
```

## Chay tren GitHub Actions

Workflow:

```text
.github\workflows\ci.yml
```

Job:

```text
OWASP ZAP security scan
```

Artifact sau khi chay:

```text
zap-security-report
```

Vao GitHub:

```text
Actions > chon lan chay CI > Artifacts > zap-security-report
```

## File cau hinh

```text
docker-compose.zap.yml
scripts\run-zap-docker.ps1
.github\workflows\ci.yml
```

## Che do fail pipeline

Hien tai ZAP dang chay voi tham so `-I`, nghia la tao report nhung khong lam fail pipeline khi co warning. Cach nay hop ly trong giai do dau vi ung dung co the co nhieu canh bao cau hinh can xu ly dan.

Khi muon pipeline nghiem hon, sua `docker-compose.zap.yml` va bo tham so:

```text
- -I
```

Luc do ZAP co the lam fail CI neu phat hien alert o muc can xu ly.
