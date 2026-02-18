# Booking Platform

> A fullstack booking system built with **.NET 8 (Minimal API)** and
> **React (Vite + TypeScript)**.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![React](https://img.shields.io/badge/React-18-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![CI](https://img.shields.io/badge/CI-GitHub%20Actions-success)
![Status](https://img.shields.io/badge/status-active-development-orange)

------------------------------------------------------------------------

## Purpose

This project is built as a **backend-focused portfolio system** to
demonstrate:

-   Clean architecture principles\
-   Domain-driven thinking\
-   Authentication & authorization\
-   Real-world validation logic\
-   Service-layer separation\
-   CI-ready project structure

It is intentionally designed to resemble a realistic backend system ---
not just a CRUD demo.

------------------------------------------------------------------------

# Features

## Authentication

-   JWT-based authentication\
-   Secure password hashing (BCrypt)\
-   Role-ready architecture

## Booking System

-   Create bookings within a time range\
-   Prevent overlapping bookings (domain rule)\
-   View available resources\
-   Audit logging of user actions

## Frontend

-   React\
-   TypeScript\
-   Vite

------------------------------------------------------------------------

# Architecture

    booking-platform/
    │
    ├── backend/
    │   └── Booking.Api/
    │       ├── Common/        → Result pattern
    │       ├── Data/          → EF Core entities & DbContext
    │       ├── Domain/        → Business rules (e.g. overlap logic)
    │       ├── Contracts/     → Request/Response DTOs
    │       ├── Services/      → Application/service layer
    │       └── Program.cs     → API wiring & endpoint mapping
    │
    ├── frontend/
    │   └── booking-web/
    │
    └── docker-compose.yml

------------------------------------------------------------------------

# Tech Stack

## Backend

-   .NET 8 (Minimal API)\
-   Entity Framework Core\
-   PostgreSQL\
-   JWT Authentication\
-   BCrypt password hashing\
-   Swagger / OpenAPI\
-   Docker (database)

## Frontend

-   React\
-   TypeScript\
-   Vite

------------------------------------------------------------------------

# Running Locally

## 1️.Start Database

``` bash
docker compose up -d
```

## 2️.Run Backend

``` bash
cd backend/Booking.Api
dotnet run
```

Backend: http://localhost:5252\
Swagger: http://localhost:5252/swagger

## 3️.Run Frontend

``` bash
cd frontend/booking-web
npm install
npm run dev
```

Frontend: http://localhost:5173

------------------------------------------------------------------------

# Demo Credentials

After calling:

POST /dev/seed

Admin\
- email: admin@demo.no\
- password: admin

User\
- email: user@demo.no\
- password: user

------------------------------------------------------------------------

# Current Capabilities

✔ Authentication (JWT)\
✔ BCrypt password hashing\
✔ Resource listing\
✔ Booking creation\
✔ Overlap prevention\
✔ Audit logging\
✔ Service layer abstraction\
✔ Result-pattern implementation\
✔ CI build validation

------------------------------------------------------------------------

# Roadmap

## Phase 1

-   Role-based authorization\
-   "My bookings" endpoint\
-   Booking cancellation\
-   Admin resource management

## Phase 2

-   Full CI pipeline\
-   Docker image build in CI\
-   Deployment-ready configuration

## Phase 3

-   Booking history view\
-   Admin dashboard\
-   Improved UX & error handling

------------------------------------------------------------------------

# Project Philosophy

This project evolves step-by-step with a focus on clean architecture,
maintainability, and real-world backend practices.

