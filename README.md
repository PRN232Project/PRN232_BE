# Online Learning Platform - Backend API (`PRN232_BE`)

A modern, scalable backend API for an online learning platform built with **.NET 8 (ASP.NET Core Web API)** and **PostgreSQL**. The project follows **Onion Architecture** (Clean Architecture) principles and provides features including course management, payment integration, real-time messaging, notifications, automated AI grading support, and role-based administration.

---

## 🛠️ Technology Stack

- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Database & ORM**: PostgreSQL with Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Authentication**: JWT (JSON Web Token) with Role-Based Access Control (Admin, Instructor, Student)
- **Real-Time Communication**: SignalR (`/hubs/chat`, `/hubs/notification`)
- **Payment Gateway**: PayOS API with automatic fallback mock sandbox payment
- **Media Storage**: Cloudinary & Firebase Storage
- **Email Service**: SMTP / MailKit for transactional emails and account verification
- **API Documentation**: Swagger UI (OpenAPI 3.0)

---

## 🏛️ Architecture Overview

The backend is structured according to **Onion / Clean Architecture**:

```
PRN232_BE/
├── API/                    # Presentation Layer (Controllers, SignalR Hubs, Swagger Configuration, Program.cs)
├── Application/            # Application Logic (Interfaces, Services, DTO Requests/Responses, AutoMapper Profiles)
├── Domain/                 # Domain Layer (Entities, Migrations, AppDbContext)
└── Infrastructure/         # Infrastructure Layer (Repositories, UnitOfWork, Database Context implementation)
```

### Key Modules & Responsibilities
- **API**: Handles HTTP requests, JWT middleware, SignalR hubs, CORS policies, and Swagger UI.
- **Application**: Contains business logic (`CourseService`, `PaymentService`, `WalletService`, `MessageService`, `AdminService`, `EnrollmentService`, etc.) and data mapping.
- **Domain**: Defines core business entities (`Course`, `Module`, `Lesson`, `LessonItem`, `LessonResource`, `Enrollment`, `Payment`, `Wallet`, `User`, `CourseReview`) and EF Core migrations.
- **Infrastructure**: Implements generic `Repository<T>` and `UnitOfWork` patterns.

---

## ✨ Key Features

### 1. 🔐 Authentication & Role Management
- User Registration, Login, and Password Encryption (Salt/Hash).
- JWT Token Generation & Validation.
- Support for roles: `Student`, `Instructor`, `Admin`.

### 2. 📚 Course & Curriculum Management
- Full CRUD operations for Courses, Modules, Lessons, Lesson Items (Video, Article, Quiz), and Resources.
- Course publishing workflow: Draft ➔ Pending Review ➔ Published / Rejected.

### 3. 💳 Payments & Wallet System
- PayOS Payment Gateway integration for online course purchases.
- Sandbox Mock Payment fallback mechanism for development and testing.
- Instructor Wallet & Earnings tracking with Admin payout approval system.

### 4. 💬 Real-Time Chat & Notifications (SignalR)
- **ChatHub (`/hubs/chat`)**: Direct messaging between Students and Instructors.
- **NotificationHub (`/hubs/notification`)**: Real-time push notifications for course review updates, wallet updates, and system events.

### 5. 🎓 Progress Tracking & Certificates
- Student course enrollment & progress tracking per lesson.
- Automated certificate issuance upon completing 100% of course requirements.

### 6. 🛡️ Admin Management
- Platform dashboard and cashflow reports.
- User management (Ban / Unban, Role updates).
- Comprehensive Course Reviewer (Full curriculum & media content inspector).

---

## ⚙️ Environment Configuration

Create or update `API/appsettings.json` in the `API` project folder with the required configurations:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=online_learning_db;Username=postgres;Password=your_password"
  },
  "SecretToken": "YOUR_SUPER_SECRET_JWT_KEY_MINIMUM_32_CHARACTERS",
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "APIKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY",
    "ReturnUrl": "http://localhost:3000/payment/success",
    "CancelUrl": "http://localhost:3000/payment/fail"
  },
  "SMTP": {
    "Email": "your_email@gmail.com",
    "Password": "your_app_password"
  },
  "Cloudinary": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  },
  "Firebase": {
    "Bucket": "your_project.appspot.com"
  }
}
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) database instance running on port `5432`

### Step 1: Clone the Repository
```bash
git clone https://github.com/PRN232Project/PRN232_BE.git
cd PRN232_BE
```

### Step 2: Database Migration
The application is configured to automatically run database migrations on startup. If you want to apply migrations manually:
```bash
dotnet ef database update --project Domain --startup-project API
```

### Step 3: Run the Backend API
```bash
dotnet run --project API
```

By default, the server runs on `http://localhost:5180` (or `https://localhost:7180`).

---

## 📖 API Documentation & SignalR Hubs

- **Swagger UI**: Visit `http://localhost:5180/swagger` in your browser to inspect interactive REST API documentation.
- **SignalR Hubs**:
  - `http://localhost:5180/hubs/chat` (JWT access token via `access_token` query parameter)
  - `http://localhost:5180/hubs/notification` (JWT access token via `access_token` query parameter)

---

## 📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
