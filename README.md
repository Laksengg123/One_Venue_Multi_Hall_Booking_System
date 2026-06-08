# 🏛️ Hall Booking System

A feature-rich, console-based **Venue Booking Management System** built with **.NET 10** and **Microsoft SQL Server**. Designed for managing hall reservations, payments, and reporting through a clean role-based interface for Admins and Customers.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Database Setup](#database-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [User Roles](#user-roles)
- [Domain Events](#domain-events)
- [Architecture](#architecture)

---

## Overview

The Hall Booking System enables venues to manage their hall inventory, handle customer bookings, process payments, and generate business reports — all through an interactive console UI. It uses a **vertical slice / feature-folder architecture** with manual dependency injection, clean separation of concerns, and a domain events model for decoupled cross-feature communication.

---

## Features

### 🏢 Hall Management
- Add, update, and deactivate halls
- Track hall attributes: type (Conference, Banquet, Exhibition, Training, Auditorium), capacity, price per hour, and amenities (AC, Projector, WiFi)
- Dynamic pricing — Conference halls apply a 1.1× rate multiplier automatically

### 📅 Booking Management
- Create and manage bookings with date/time collision detection
- Full booking lifecycle: `Pending → Confirmed → Completed / Cancelled / Rejected`
- View booking history per customer or across the system

### 💳 Payment Management
- Multiple payment methods: Cash, Card, UPI, Bank Transfer, Cash on Delivery
- Invoice generation per booking
- Cancellations with refund tracking (Pending → Processed)
- Auto-triggered payment flow when a booking is confirmed (via domain events)

### 📊 Reports & Exports
- Revenue report (by date range) — exported as `.csv`
- Top halls report by booking frequency — exported as `.csv`
- Booking summary report — exported as `.csv`
- On-screen analytics: occupancy, monthly trends

### 🔐 Authentication
- Role-based access: **Admin** and **Customer**
- Password hashing with BCrypt (cost factor 12)
- Configurable max login attempts
- Session-aware login/logout with events

---

## Tech Stack

| Component          | Technology                          |
|--------------------|-------------------------------------|
| Language           | C# (.NET 10)                        |
| Database           | Microsoft SQL Server                |
| ORM / Data Access  | Raw ADO.NET (`Microsoft.Data.SqlClient`) |
| Password Hashing   | BCrypt.Net-Next 4.2.0               |
| Report Export      | CsvHelper 33.0.1                    |
| Excel (future use) | ClosedXML 0.105.0                   |
| Word (future use)  | DocX 3.0.0                          |
| Configuration      | Microsoft.Extensions.Configuration (JSON) |
| Target Framework   | net10.0                             |

---

## Project Structure

```
Hall_Booking_System/
├── Program.cs                          # Entry point & Composition Root
├── appsettings.json                    # App configuration
├── VenueBookingSystem.csproj
│
├── Features/
│   ├── BookingManagement/
│   │   ├── DTOs/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   ├── Models/           Booking.cs, BookingExtensions
│   │   ├── Repositories/     BookingRepository.cs
│   │   ├── Services/         BookingService.cs, BookingHistory.cs
│   │   └── UI/               BookingUI.cs
│   │
│   ├── HallManagement/
│   │   ├── DTOs/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   ├── Models/           BaseHall.cs, Hall.cs
│   │   ├── Repositories/     HallRepository.cs, HallMapper.cs
│   │   ├── Services/         HallService.cs
│   │   └── UI/               HallUI.cs
│   │
│   ├── PaymentManagement/
│   │   ├── DTOs/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   ├── Models/           Payment.cs, Invoice.cs, Refund.cs
│   │   ├── Repositories/     PaymentRepository.cs
│   │   ├── Services/         PaymentService.cs
│   │   └── UI/               PaymentUI.cs
│   │
│   ├── Reports/
│   │   ├── DTOs/
│   │   ├── Events/
│   │   │   └── Authentication/   (Auth feature co-located here)
│   │   │       ├── DTOs/
│   │   │       ├── Events/
│   │   │       ├── Exceptions/
│   │   │       ├── Interfaces/
│   │   │       ├── Models/       User.cs, AuthSession.cs
│   │   │       ├── Repositories/ UserRepository.cs
│   │   │       ├── Services/     AuthService.cs
│   │   │       └── UI/           LoginScreen.cs
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   ├── Models/           Feedback.cs, ReportConfig.cs
│   │   ├── Repositories/     ReportRepository.cs
│   │   ├── Services/         ReportService.cs
│   │   └── UI/               ReportUI.cs
│   │
│   └── Users/
│       ├── Admin/            AdminDashboard, AdminService
│       └── Customers/        CustomerDashboard, CustomerService
│
├── Shared/
│   └── ConsoleHelper.cs      Formatting, prompts, table rendering
│
└── Storage/
    └── DatabaseContext.cs    Singleton DB connection manager
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server (local instance or remote)
- SQL Server Management Studio (SSMS) or Azure Data Studio *(optional)*

---

## Database Setup

1. Open SSMS or your preferred SQL client.
2. Run the database setup script located at:
   ```
   Hall_Booking_System/Features/OneVenueDB.sql
   ```
3. This script creates the `VenueBookingDB` database with all required tables:
   - `Users`, `Halls`, `Bookings`, `Payments`
   - `Cancellations`, `Refunds`, `Invoices` *(auto-created on first run if missing)*

> **Note:** The application will auto-create the `Cancellations`, `Refunds`, and `Invoices` tables at startup if they do not already exist.

---

## Configuration

Edit `appsettings.json` to match your environment:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VenueBookingDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "AppSettings": {
    "AppName": "Hall Booking System",
    "Version": "1.0.0",
    "PageSize": 10,
    "MaxLoginAttempts": 3
  }
}
```

| Key                  | Description                                      |
|----------------------|--------------------------------------------------|
| `DefaultConnection`  | SQL Server connection string                     |
| `AppName`            | Display name shown in the welcome screen         |
| `PageSize`           | Number of records per page in list views         |
| `MaxLoginAttempts`   | Failed attempts before locking out a session     |

---

## Running the Application

```bash
# Clone or extract the project
cd Hall_Booking_System

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

### Test Login Shortcut

To quickly verify database connectivity and admin login during development:

```bash
dotnet run testlogin
```

This bypasses the UI and prints a success/failure message for the `admin1` account.

---

## User Roles

### 👤 Admin
- Full hall management (create, edit, deactivate)
- View and manage all bookings (confirm, cancel, reject)
- Process payments and refunds
- Generate and export reports
- Manage user accounts

### 👤 Customer
- Browse available halls
- Create and cancel own bookings
- View booking history
- Make and track payments

---

## Domain Events

The system uses C# `EventHandler<T>` for decoupled communication between features:

| Event                    | Publisher        | Effect                                          |
|--------------------------|------------------|-------------------------------------------------|
| `BookingCreated`         | BookingService   | Logs new booking details to console             |
| `BookingConfirmed`       | BookingService   | Triggers payment processing, logs confirmation  |
| `BookingCancelled`       | BookingService   | Logs cancellation, initiates refund flow        |
| `PaymentProcessed`       | PaymentService   | Logs payment amount and ID                      |
| `UserLoggedIn`           | AuthService      | Logs user name and role on login                |
| `UserLoggedOut`          | AuthService      | Logs logout event                               |

---

## Architecture

The application follows a **vertical slice architecture** organised by feature domain. Each feature folder is self-contained with its own models, interfaces, repository, service, and UI layer.

**Key design decisions:**

- **Manual DI** — Services are wired in `Program.cs` (Composition Root), keeping the project free of DI framework dependencies.
- **Repository pattern** — Each feature has its own repository for data access, keeping SQL isolated from business logic.
- **Domain events** — `EventHandler<T>` enables cross-feature side effects without tight coupling (e.g., `BookingService` does not directly call `PaymentService`).
- **Immutable records** — `Booking`, `User`, and `Payment` are C# `record` types, ensuring data integrity after mapping.
- **Singleton DB context** — `DatabaseContext` uses a singleton pattern with async connection factory to manage SQL Server connectivity.

---

## License

This project is intended for academic / portfolio use. No license is applied by default.

---

> Built with ❤️ using .NET 10 and Microsoft SQL Server.