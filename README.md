# Booking Platform

A fullstack booking system built with **.NET 8 (Minimal API)** and
**React (Vite + TypeScript)**.

This project is designed as a practical backend-focused portfolio
project, with clean architecture, clear domain logic, authentication,
and database integration.

------------------------------------------------------------------------

## Overview

The system allows users to:

-   Log in using JWT-based authentication
-   View available resources (e.g. meeting rooms)
-   Create bookings within a time range
-   Prevent overlapping bookings
-   Track actions through audit logging

The goal is to simulate a realistic backend system structure, not just a
demo CRUD API.

------------------------------------------------------------------------

## Tech Stack

### Backend

-   .NET 8 (Minimal API)
-   Entity Framework Core
-   PostgreSQL
-   JWT Authentication
-   Swagger / OpenAPI
-   Docker (for database)

### Frontend

-   React
-   TypeScript
-   Vite

------------------------------------------------------------------------

## Project Structure

    booking-platform/
    │
    ├── backend/
    │   └── Booking.Api/
    │       ├── Data/
    │       ├── Domain/
    │       ├── Contracts/
    │       └── Program.cs
    │
    ├── frontend/
    │   └── booking-web/
    │
    └── docker-compose.yml

### Folder Responsibilities

-   **Data/** → EF Core entities and DbContext
-   **Domain/** → Business rules (e.g., booking overlap logic)
-   **Contracts/** → Request/response DTOs
-   **Program.cs** → API setup and endpoint definitions

Database models and API contracts are separated to allow future
scalability.

------------------------------------------------------------------------

## Running Locally

### 1. Start PostgreSQL (Docker)

``` bash
docker compose up -d
```

------------------------------------------------------------------------

### 2. Run Backend

``` bash
cd backend/Booking.Api
dotnet run
```

Backend will start on:

    http://localhost:5252

Swagger UI:

    http://localhost:5252/swagger

------------------------------------------------------------------------

### 3. Run Frontend

``` bash
cd frontend/booking-web
npm install
npm run dev
```

Frontend runs on:

    http://localhost:5173

------------------------------------------------------------------------

## Demo Credentials

After seeding:

    POST /dev/seed

You can log in with:

Admin: - email: admin@demo.no - password: admin

User: - email: user@demo.no - password: user

------------------------------------------------------------------------

## Current Status

✔ Authentication\
✔ Resource listing\
✔ Booking creation\
✔ Overlap prevention\
✔ Audit logging\
✔ React frontend integration

------------------------------------------------------------------------

## Design Goals

This project focuses on:

-   Clear separation of concerns
-   Domain-driven thinking
-   Real-world backend structure
-   Maintainable code over quick hacks
-   Step-by-step evolution

------------------------------------------------------------------------

## Future Improvements

-   Proper password hashing (BCrypt)
-   Role-based authorization policies
-   Booking cancellation flow
-   Admin dashboard
-   CI/CD pipeline
-   Deployment setup

------------------------------------------------------------------------
