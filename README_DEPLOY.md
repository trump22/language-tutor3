# Language Tutor CI/CD and Azure Deployment

Folder này là bản triển khai riêng, không ảnh hưởng tới `Complete Full`.

## Kiến trúc Azure hiện tại

Azure App Service Plan trong portal chỉ là hạ tầng chạy ứng dụng. Cần tạo thêm một Web App bên trong plan.

Để phù hợp gói Windows F1, workflow đóng gói thành một ứng dụng:

- React được build với API URL `/api`.
- File React `dist` được copy vào `backend-aspnet/wwwroot`.
- ASP.NET phục vụ cả giao diện React, REST API và `/uploads`.
- GitHub Actions publish một gói .NET duy nhất lên Azure App Service.

## Tạo Web App trong Azure Portal

Tại App Service Plan `plan-language-tutor3`:

1. Chọn `Create Web App` hoặc mở `App Services` và chọn `Create`.
2. Chọn resource group `trung`.
3. Chọn một tên Web App duy nhất, ví dụ `language-tutor3-web`.
4. Publish: `Code`.
5. Runtime stack: `.NET 8`.
6. Operating System: `Windows`.
7. Region: `Malaysia West`.
8. Chọn App Service Plan đã có: `plan-language-tutor3`.

Sau khi tạo, URL mặc định sẽ có dạng:

```text
https://language-tutor3-web.azurewebsites.net
```

## Cấu hình GitHub

Trong repository GitHub, mở:

```text
Settings > Secrets and variables > Actions
```

Tạo repository variable:

```text
AZURE_WEBAPP_NAME=language-tutor3-web
```

Tên phải đúng chính xác với Web App, không phải tên App Service Plan.

Trong Azure Web App, mở `Overview` và tải `Get publish profile`. Mở file `.PublishSettings`, copy toàn bộ nội dung và tạo repository secret:

```text
AZURE_WEBAPP_PUBLISH_PROFILE=<toàn bộ nội dung PublishSettings>
```

Publish profile là credential nhạy cảm, không commit vào Git.

Nếu nút tải publish profile bị khóa, mở:

```text
Web App > Configuration > General settings
```

Bật `SCM Basic Auth Publishing Credentials`, lưu cấu hình rồi tải lại publish profile.

## Cấu hình ứng dụng trên Azure

Trong Web App:

```text
Settings > Environment variables > App settings
```

Thêm:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Gemini__ApiKey
Gemini__LiteModel
Gemini__Flash20LiteModel
Gemini__Flash20Model
Gemini__FlashModel
Gemini__ProModel
Azure__SpeechKey
Azure__SpeechRegion
```

Giá trị model:

```text
Gemini__LiteModel=gemini-2.5-flash-lite
Gemini__Flash20LiteModel=gemini-2.0-flash-lite
Gemini__Flash20Model=gemini-2.0-flash
Gemini__FlashModel=gemini-2.5-flash
Gemini__ProModel=gemini-2.5-pro
```

`ConnectionStrings__DefaultConnection` không được dùng `Host=localhost`, vì PostgreSQL không chạy bên trong Azure App Service. Database phải có địa chỉ mạng mà Azure truy cập được.

Ví dụ:

```text
Host=DATABASE_HOST;Port=5432;Database=languagetutor_db;Username=DB_USER;Password=DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Sau khi thêm biến, chọn `Save` và khởi động lại Web App.

## Chạy deployment

Trên GitHub:

```text
Actions > Deploy Azure App Service > Run workflow
```

Workflow sẽ:

1. Build React.
2. Nhúng React vào ASP.NET.
3. Publish .NET 8.
4. Kiểm tra package có DLL và `wwwroot/index.html`.
5. Deploy lên Azure App Service.

## Selenium CI

CI chạy khi push vào `main` hoặc tạo Pull Request:

- Build backend.
- Build frontend.
- Khởi động PostgreSQL, backend, frontend và Chromium bằng Docker.
- Chạy Selenium end-to-end.
- Upload `selenium-results`.

Chạy Selenium bằng Docker:

```powershell
.\scripts\run-selenium-docker.ps1
```

Chạy bằng Chrome local khi frontend/backend đã chạy:

```powershell
.\scripts\run-selenium-local.ps1 -BaseUrl http://localhost:5174
```

## Giới hạn F1

- F1 phù hợp demo và kiểm thử nhẹ.
- F1 có quota CPU/ngày và có thể dừng app khi hết quota.
- Custom domain cần App Service Plan trả phí; F1 chỉ dùng tốt với domain `azurewebsites.net`.
- Database và các dịch vụ Azure khác có thể phát sinh chi phí riêng.
