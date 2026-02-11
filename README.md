Booking Platform

Et fullstack bookingsystem bygget for å utforske praktisk backend-utvikling i .NET kombinert med en enkel React-frontend.

Prosjektet simulerer et ekte system hvor brukere kan logge inn og booke ressurser (f.eks. møterom), med regler for overlapp, autentisering og sporbarhet.

Dette er et lærings- og porteføljeprosjekt som utvikles steg for steg, med fokus på ryddig struktur og realistisk arkitektur.


Funksjonalitet

API-et lar brukere:

Logge inn med JWT-autentisering

Hente tilgjengelige ressurser

Opprette bookinger innenfor et tidsrom

Hindre dobbeltbooking (overlapp-sjekk)

Logge handlinger for sporbarhet (audit trail)


Frontend:

Logger inn bruker

Lagrer JWT-token

Henter beskyttede ressurser

Tester API-flyten fra UI


Teknologi
Backend

.NET 8 (Minimal API)

Entity Framework Core

PostgreSQL

JWT (HS256)

Swagger / OpenAPI

Frontend

React

TypeScript

Vite

Infrastruktur

Docker (PostgreSQL)

GitHub Actions (CI)


Arkitektur og struktur

Backend er strukturert bevisst enkelt, men med tydelig separasjon av ansvar:

backend/
 └── Booking.Api/
     ├── Contracts/     -> Request/response-modeller
     ├── Data/          -> EF Core entiteter + DbContext
     ├── Domain/        -> Domenelogikk (f.eks. booking-regler)
     ├── Migrations/    -> Database-migrasjoner
     └── Program.cs     -> API-endepunkter og oppsett


Designvalg

API-kontrakter er skilt fra database-entiteter

Domenelogikk (f.eks. overlapp-sjekk) er flyttet ut av endepunktet

JWT beskytter sensitive endepunkter

CORS er konfigurert for lokal utvikling

Kjøre prosjektet lokalt
Start database (Docker)

Fra rotmappen:

docker compose up -d


Dette starter PostgreSQL-containeren.

Kjør backend
cd backend/Booking.Api
dotnet run


API-et kjører på:

http://localhost:5252


Swagger:

http://localhost:5252/swagger

Kjør frontend
cd frontend/booking-web
npm install
npm run dev


Frontend kjører på:

http://localhost:5173


Opprett en .env-fil i frontend-mappen:

VITE_API_BASE_URL=http://localhost:5252

Demo-brukere

Seed demo-data via:

POST /dev/seed


Innlogging:

admin@demo.no / admin
user@demo.no / user


Videre utvikling

Planlagte forbedringer:

Booking-opprettelse fra frontend

Rollebasert tilgang (Admin vs User)

Service-lag mellom API og DbContext

Bedre feilhåndtering

Deployment-oppsett


Formål med prosjektet

Dette prosjektet er laget for å:

Øve på praktisk .NET backend-utvikling

Jobbe med autentisering og sikkerhet

Strukturere kode som et ekte API-prosjekt

Integrere frontend og backend

Vise helhetlig forståelse av fullstack