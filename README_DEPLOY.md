# Language Tutor CI/CD Deploy Workspace

Folder này là bản riêng để triển khai web production, không ảnh hưởng tới folder Complete Full.

## Cấu trúc

- `backend-aspnet`: ASP.NET API.
- `frontend-react`: React/Vite frontend.
- `docker-compose.prod.yml`: chạy PostgreSQL, backend, frontend và Caddy HTTPS.
- `ops/caddy/Caddyfile`: reverse proxy domain riêng.
- `.github/workflows`: CI build và deploy qua SSH tới VPS.
- `tests/LanguageTutor.E2E`: Selenium WebDriver tests bằng .NET/xUnit.
- `docker-compose.selenium.yml`: môi trường test độc lập gồm PostgreSQL, backend, frontend và Chromium.

## Chạy local bằng Docker

```powershell
copy .env.example .env
```

Sửa `.env`, sau đó chạy:

```powershell
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

## Trỏ tên miền

Trong trang quản lý DNS của domain, tạo bản ghi:

```text
Type: A
Name: @
Value: IP_VPS_CUA_BAN
TTL: Auto
```

Nếu dùng subdomain:

```text
Type: A
Name: app
Value: IP_VPS_CUA_BAN
TTL: Auto
```

Sau đó đặt trong `.env`:

```text
DOMAIN=your-domain.com
```

Caddy sẽ tự xin HTTPS certificate khi domain đã trỏ đúng IP VPS và port `80/443` mở.

## Cấu hình secret production

Không commit file `.env`. Trên VPS, tạo `.env` từ `.env.example` và điền:

- `DOMAIN`
- `POSTGRES_PASSWORD`
- `JWT_KEY`
- `GEMINI_API_KEY`
- `AZURE_SPEECH_KEY`
- `AZURE_SPEECH_REGION`

## CI/CD GitHub Actions

Trong GitHub repository, thêm các secrets:

```text
VPS_HOST=IP VPS
VPS_USER=user SSH
VPS_SSH_KEY=private SSH key
DEPLOY_PATH=/duong/dan/project/tren/vps
```

Deploy workflow sẽ SSH vào VPS, chạy `git pull`, rồi chạy:

```bash
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

## Kiểm thử Selenium

Bộ test hiện có:

- Người chưa đăng nhập vào `/dashboard` sẽ được chuyển về `/login`.
- Học viên có thể đăng ký, vào dashboard, mở khóa học, đăng xuất và đăng nhập lại.

Chạy hoàn toàn bằng Docker:

```powershell
.\scripts\run-selenium-docker.ps1
```

Chạy bằng Chrome cài trên máy khi frontend/backend đã chạy:

```powershell
.\scripts\run-selenium-local.ps1 -BaseUrl http://localhost:5174
```

Kết quả `.trx` và ảnh chụp khi test lỗi được lưu trong:

```text
TestResults
```

Selenium sử dụng Page Object và explicit wait. Selenium Manager tự tìm hoặc tải browser driver khi chạy local; trong CI, test kết nối tới container Chromium.

Trong CI, test container tự chờ tối đa 180 giây cho cả frontend `/` và backend `/api/courses`. Artifact `selenium-results` luôn kèm:

- `selenium.trx`
- ảnh chụp màn hình nếu test lỗi
- `docker-compose.log`
- `docker-compose-ps.txt`

## Lưu ý

- Backend tự chạy migration khi start.
- Frontend gọi API qua `/api`, Caddy chuyển tiếp tới backend.
- Upload audio được lưu trong Docker volume `backend_uploads`.
- PostgreSQL được lưu trong Docker volume `postgres_data`.
