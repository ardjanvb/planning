# Personeelsplanning Metalektro

Blazor Interactive Server-app voor personeels- en capaciteitsplanning: productiebehoefte importeren, medewerkers toewijzen, conflicten signaleren en Excel exporteren.

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Starten

```bat
dotnet run --project src\Web --urls http://127.0.0.1:5288
```

Of via Visual Studio / `Planning.slnx` met startupproject `Web`.

Browser: `http://127.0.0.1:5288/login`

Demo-accounts (wachtwoord `Demo123!`):

| Gebruiker   | Rol                 |
|-------------|---------------------|
| beheerder   | Applicatiebeheerder |
| planner     | Planner             |
| teamleider  | Teamleider          |
| leiding     | Leidinggevende      |

SQLite-database: `planning.db` naast de Web-app (wordt bij start aangemaakt + geseed).

## Bouwen / testen

```bat
dotnet build Planning.slnx
dotnet test Planning.slnx
```

## Structuur

| Pad | Inhoud |
|-----|--------|
| `src/Web` | MudBlazor UI |
| `src/Domain` | Entities + EF Core |
| `src/Services` | Import, planning, validatie, export, audit |
| `src/Framework` | Rollen |
| `tests/Services.Tests` | Unit tests |
| `docs/` | Importformaat, DoD, besluiten |
| `data/voorbeeld-import.csv` | Voorbeeld productie-import |

## Kernflow

1. Inloggen als planner
2. Beheer controleren (medewerkers, werkplekken, kwalificaties)
3. Import → `data/voorbeeld-import.csv`
4. Planningsmatrix: medewerkers toewijzen
5. Capaciteit + waarschuwingen controleren
6. Goedkeuren / publiceren
7. Excel-export
