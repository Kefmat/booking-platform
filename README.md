# Booking Platform

Dette er et pågående fullstack-prosjekt der jeg bygger et enkelt bookingsystem
for å øve på og vise praktisk backend-utvikling i C#/.NET.

Fokuset er på å lage noe som ligner et ekte system, med tydelig domenelogikk,
ryddig kode og kommentarer som faktisk forklarer hvorfor ting er gjort.



## Hva er dette?

Et API for å booke ressurser (f.eks. møterom):

- brukere kan logge inn
- ressurser kan bookes innenfor et tidsrom
- systemet hindrer dobbeltbooking
- handlinger logges for sporbarhet

Dette er ikke ment som et ferdig produkt, men som et prosjekt jeg bygger videre
på steg for steg.



## Teknologi

Backend:
- .NET (Minimal API)
- Entity Framework Core
- PostgreSQL
- JWT-basert autentisering

Verktøy og annet:
- Swagger / OpenAPI
- Docker (kommer)
- React frontend (kommer)



## Litt om strukturen

Prosjektet er holdt ganske enkelt med vilje:

- `Program.cs` inneholder API-endepunkter og oppsett
- `Data/` inneholder database-relaterte klasser
- `Contracts/` inneholder request/response-modeller

Database-entiteter og API-kontrakter er skilt fra hverandre for å gjøre koden
enklere å endre senere.



## Kjøre lokalt (backend)

cd backend/Booking.Api
dotnet run
