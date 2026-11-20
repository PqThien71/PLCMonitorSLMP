# Project Context & Instructions for Codex

## 1. Tech Stack & Architecture

- **Framework:** .NET 4.7.2
- **Project Type:** ASP.NET Core Web API / Clean Architecture (Domain, Application, Infrastructure, API)
- **Database & ORM:** PostgreSQL / SQL Server với Entity Framework Core (EF Core)
- **Testing:** xUnit, Moq / NSubstitute, FluentAssertions

## 2. Build, Run & Test Commands

Luôn chạy các lệnh sau để kiểm tra code trước khi hoàn tất tác vụ:

- **Restore & Build:** `dotnet build --configuration Release`
- **Run Unit Tests:** `dotnet test --no-build --verbosity normal`
- **Format Code:** `dotnet format`

## 3. C# Coding Conventions

- **Naming:**
  - Dùng `PascalCase` cho Class, Method, Property, Public field.
  - Dùng `camelCase` cho biến cục bộ và tham số.
  - Dùng `_camelCase` cho `private readonly` field.
  - Interface bắt đầu bằng chữ `I` (ví dụ: `IUserService`).
- **Modern C# Features:** Ưu tiên sử dụng `record` cho DTOs, Primary Constructors cho Dependency Injection, pattern matching và collection expressions (`[]`) khi phù hợp.
- **Nullability:** Bật `<Nullable>enable</Nullable>`. Tuyệt đối không dùng toán tử `!` (null-forgiving) nếu chưa kiểm tra null rõ ràng.
- **Async/Await:** Mọi thao tác I/O (DB, HTTP, File) đều phải dùng bất đồng bộ (`async Task`), hậu tố tên hàm kết thúc bằng `Async`, và luôn truyền `CancellationToken`.

## 4. Architecture & Best Practices

- **Dependency Injection:** Đăng ký service qua extension methods trong từng tầng (ví dụ: `AddApplicationServices()`, `AddInfrastructureServices()`).
- **Error Handling:** Không dùng `try-catch` tràn lan trong Controller; sử dụng Global Exception Handler hoặc mẫu Result Pattern.
- **Database (EF Core):** Dùng `.AsNoTracking()` cho các câu truy vấn chỉ đọc (GET). Không tự ý tạo file Migration trừ khi được yêu cầu.

## 5. Boundaries (Những điều không được làm)

- Không sửa đổi các file cấu hình chứa thông tin nhạy cảm (`appsettings.Production.json`, `.env`).
- Không tự ý nâng cấp phiên bản các gói NuGet trong file `.csproj` nếu không có chỉ định.
- Giữ nguyên cấu trúc thư mục hiện tại, không tự ý đổi namespace hàng loạt.
