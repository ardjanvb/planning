# Importformaat productieplanning

Ondersteunde bestanden: `.csv` (scheiding `;` of `,`) en `.xlsx`.

## Verplichte kolommen

| Kolom | Aliassen | Voorbeeld |
|-------|----------|-----------|
| Jaar | Year | 2026 |
| Weeknummer | Week, WeekNumber | 48 |
| Werkpleknummer | Werkplek, Workplace | WP-01 |
| AantalBenodigdeShifts | Shifts, RequiredShifts | 5 |
| UrenPerShift | Uren, Hours | 8 |

## Optioneel

| Kolom | Aliassen |
|-------|----------|
| Datum | Date, SpecificDate |

Wanneer alleen weektotalen worden aangeleverd, blijft de planner verantwoordelijk voor toewijzing aan concrete dagen.
Bestaande `ShiftAssignment`-records worden door een import **niet** overschreven.
