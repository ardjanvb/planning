using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Domain.Contexts;
using Domain.Entities;
using Framework;

namespace Domain.Seed;

public static class DemoSeed
{
    private static readonly DateOnly PatternStart = new(2026, 1, 5); // Monday

    private static readonly (string Number, string Name, int MinStaff, int Bhv, bool Critical)[] WorkplaceDefs =
    [
        ("CVS1", "CVS1", 2, 1, true),
        ("CVS4", "CVS4", 2, 1, true),
        ("CVS9", "CVS9", 2, 0, false),
        ("CVS14", "CVS14", 2, 0, false),
        ("CVS-SKELET", "CVS Skelet", 1, 0, false),
        ("DEG", "DEG", 2, 1, true),
        ("MEETKAMER", "Meetkamer", 1, 0, false),
        ("IP-EPP", "IP: EPP", 2, 0, false),
        ("IP-CONTACT", "IP: Contactors", 2, 0, false),
        ("IP-IJ", "IP: IJ Overig", 2, 0, false),
        ("LEDIA", "Ledia", 1, 0, false),
        ("SPIN-ERB", "Spincoat ERB", 1, 0, false),
        ("DROGE-LAK", "Droge lak belagen", 2, 0, false),
        ("PVA", "PVA belagen", 2, 0, false),
        ("ZNIKKEL", "Zwart nikkel", 2, 1, true),
        ("AUTO-LIJN", "Automatische lijn", 3, 1, true)
    ];

    private static readonly (string First, string Last)[] People =
    [
        ("Jan", "Jansen"), ("Piet", "Pietersen"), ("Klaas", "de Vries"), ("Anna", "Bakker"),
        ("Sara", "Visser"), ("Mark", "Smit"), ("Lisa", "Meijer"), ("Tom", "de Boer"),
        ("Emma", "Mulder"), ("Lars", "de Groot"), ("Nora", "Bos"), ("Finn", "Vos"),
        ("Eva", "Peters"), ("Omar", "Hassan"), ("Yara", "Ali"), ("Bram", "Hendriks"),
        ("Sofie", "van Dijk"), ("Jesse", "Willems"), ("Mila", "Kuipers"), ("Noah", "Janssen"),
        ("Lotte", "Prins"), ("Daan", "Hoekstra"), ("Fleur", "Scholten"), ("Sem", "Brouwer"),
        ("Iris", "van Leeuwen"), ("Julian", "Dekker"), ("Tess", "Martens"), ("Max", "van Dam"),
        ("Noor", "Verhoeven"), ("Ruben", "Gerritsen"), ("Zoe", "van den Berg"), ("Hugo", "Postma"),
        ("Liv", "Koster"), ("Thijs", "Nijhuis"), ("Amber", "van Beek"), ("Stijn", "Roelofs"),
        ("Isa", "Vermeulen"), ("Jasper", "van den Heuvel"), ("Elin", "Schoenmakers"), ("Koen", "Teunissen"),
        ("Fenna", "Willemsen"), ("Lucas", "van Dongen"), ("Saar", "Hermans"), ("Mats", "van Rooij"),
        ("Lina", "Peeters"), ("Gijs", "van der Meer"), ("Puck", "Jacobs"), ("Boaz", "van der Berg")
    ];

    public static void EnsureSeeded(RepositoryContext db)
    {
        if (db.AppUsers.Any())
            return;

        var operatorFn = new JobFunction { Name = "Operator", Description = "Productie-operator" };
        var teamLeadFn = new JobFunction { Name = "Teamleider", Description = "Teamleider productie" };
        db.JobFunctions.AddRange(operatorFn, teamLeadFn);

        var qHeftruck = new Qualification { Name = "Heftruck", Category = "Certificaat" };
        var qKraan = new Qualification { Name = "Kraanbediening", Category = "Certificaat" };
        db.Qualifications.Add(qHeftruck);
        db.Qualifications.Add(qKraan);

        var deptProd = new Department { Name = "Productie" };
        var deptIp = new Department { Name = "IP" };
        var deptFinish = new Department { Name = "Nabehandeling" };
        var deptQuality = new Department { Name = "Kwaliteit" };
        db.Departments.AddRange(deptProd, deptIp, deptFinish, deptQuality);
        db.SaveChanges();

        var patternA = CreateRotatingPattern(db, "Ploeg A – 3-ploegenrooster", phaseOffset: 0);
        var patternB = CreateRotatingPattern(db, "Ploeg B – 3-ploegenrooster", phaseOffset: 1);
        var patternC = CreateRotatingPattern(db, "Ploeg C – 3-ploegenrooster", phaseOffset: 2);

        var crewA = new Crew { Name = "Ploeg A", Description = "Roteert ochtend→middag→nacht", CrewPatternId = patternA.Id, Active = true, EffectiveFrom = PatternStart };
        var crewB = new Crew { Name = "Ploeg B", Description = "Roteert middag→nacht→ochtend", CrewPatternId = patternB.Id, Active = true, EffectiveFrom = PatternStart };
        var crewC = new Crew { Name = "Ploeg C", Description = "Roteert nacht→ochtend→middag", CrewPatternId = patternC.Id, Active = true, EffectiveFrom = PatternStart };
        db.Crews.AddRange(crewA, crewB, crewC);
        db.SaveChanges();

        var crews = new[] { crewA, crewB, crewC };
        var workplaces = new List<Workplace>();
        var workplaceQuals = new List<Qualification>();

        foreach (var def in WorkplaceDefs)
        {
            var deptId = def.Number.StartsWith("IP-", StringComparison.OrdinalIgnoreCase) ? deptIp.Id
                : def.Number is "DROGE-LAK" or "PVA" or "ZNIKKEL" or "SPIN-ERB" ? deptFinish.Id
                : def.Number == "MEETKAMER" ? deptQuality.Id
                : deptProd.Id;

            var wp = new Workplace
            {
                ExternalNumber = def.Number,
                Name = def.Name,
                DepartmentId = deptId,
                MinimumStaffing = def.MinStaff,
                RequiredBhvCount = def.Bhv,
                Active = true
            };
            workplaces.Add(wp);
            db.Workplaces.Add(wp);
        }
        db.SaveChanges();

        foreach (var wp in workplaces)
        {
            var qual = new Qualification { Name = wp.Name, Category = "Werkplek" };
            workplaceQuals.Add(qual);
            db.Qualifications.Add(qual);
        }
        db.SaveChanges();

        for (var i = 0; i < workplaces.Count; i++)
        {
            db.WorkplaceRequirements.Add(new WorkplaceRequirement
            {
                WorkplaceId = workplaces[i].Id,
                QualificationId = workplaceQuals[i].Id,
                RequiredCount = 1
            });
            db.WorkplaceRequirements.Add(new WorkplaceRequirement
            {
                WorkplaceId = workplaces[i].Id,
                JobFunctionId = operatorFn.Id,
                RequiredCount = 1
            });
        }

        var employees = new List<Employee>();
        for (var i = 0; i < People.Length; i++)
        {
            var (first, last) = People[i];
            var crew = crews[i % 3];
            var primaryWp = workplaces[i % workplaces.Count];
            var isTeamLead = i % 12 == 0;
            var isBhv = i % 5 == 0 || isTeamLead;
            var contract = (i % 7) switch
            {
                0 => 32m,
                1 => 36m,
                _ => 40m
            };

            var emp = new Employee
            {
                PersonnelNumber = (2000 + i + 1).ToString(),
                FirstName = first,
                LastName = last,
                PrimaryJobFunctionId = isTeamLead ? teamLeadFn.Id : operatorFn.Id,
                PrimaryCrewId = crew.Id,
                PrimaryWorkplaceId = primaryWp.Id,
                IsBhv = isBhv,
                BhvValidUntil = isBhv ? new DateOnly(2027, 12, 31) : null,
                ContractHoursPerWeek = contract,
                Fte = contract / 40m,
                Active = true,
                EmploymentStart = new DateOnly(2018 + (i % 7), 1 + (i % 11), 1 + (i % 27))
            };
            employees.Add(emp);
            db.Employees.Add(emp);
        }
        db.SaveChanges();

        foreach (var emp in employees)
        {
            db.EmployeeCrewMemberships.Add(new EmployeeCrewMembership
            {
                EmployeeId = emp.Id,
                CrewId = emp.PrimaryCrewId!.Value,
                EffectiveFrom = PatternStart,
                IsPrimary = true
            });

            // Primary workplace + 2 neighbouring workplaces
            var primaryIndex = workplaces.FindIndex(w => w.Id == emp.PrimaryWorkplaceId);
            for (var offset = 0; offset < 3; offset++)
            {
                var wpIndex = (primaryIndex + offset) % workplaces.Count;
                db.EmployeeQualifications.Add(new EmployeeQualification
                {
                    EmployeeId = emp.Id,
                    QualificationId = workplaceQuals[wpIndex].Id
                });
            }

            if (emp.Id % 4 == 0)
            {
                db.EmployeeQualifications.Add(new EmployeeQualification
                {
                    EmployeeId = emp.Id,
                    QualificationId = qHeftruck.Id
                });
            }
            if (emp.Id % 6 == 0)
            {
                db.EmployeeQualifications.Add(new EmployeeQualification
                {
                    EmployeeId = emp.Id,
                    QualificationId = qKraan.Id
                });
            }
        }

        // Sample absences
        db.Absences.Add(new Absence
        {
            EmployeeId = employees[1].Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7),
            EndDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7),
            Type = AbsenceType.Vacation,
            Status = AbsenceStatus.Approved,
            Remark = "Demo verlof",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        db.Absences.Add(new Absence
        {
            EmployeeId = employees[8].Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today).AddDays(2),
            EndDate = DateOnly.FromDateTime(DateTime.Today).AddDays(3),
            Type = AbsenceType.Atv,
            Status = AbsenceStatus.Approved,
            Remark = "ATV",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        var hash = HashPassword("Demo123!");
        db.AppUsers.AddRange(
            new AppUser { UserName = "beheerder", DisplayName = "Demo Beheerder", Role = AppRoles.AppAdmin, PasswordHash = hash, Active = true },
            new AppUser { UserName = "planner", DisplayName = "Demo Planner", Role = AppRoles.Planner, PasswordHash = hash, Active = true },
            new AppUser { UserName = "teamleider", DisplayName = "Demo Teamleider", Role = AppRoles.TeamLead, PasswordHash = hash, Active = true },
            new AppUser { UserName = "leiding", DisplayName = "Demo Leidinggevende", Role = AppRoles.Manager, PasswordHash = hash, Active = true }
        );

        var year = DateTime.Today.Year;
        for (var d = new DateOnly(year, 1, 1); d.Year == year; d = d.AddDays(1))
        {
            var type = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? CalendarDayType.NonWorkDay
                : CalendarDayType.WorkDay;
            db.CalendarDays.Add(new CalendarDay { Date = d, Type = type });
        }

        db.SaveChanges();

        SeedCapacityAndPlanning(db, workplaces, employees, crews);
    }

    private static void SeedCapacityAndPlanning(
        RepositoryContext db,
        List<Workplace> workplaces,
        List<Employee> employees,
        Crew[] crews)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentWeek = ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue));
        var currentYear = ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue));

        // Next 4 ISO weeks including current
        var weeks = new List<(int Year, int Week)>();
        for (var i = 0; i < 4; i++)
        {
            var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(currentYear, currentWeek, DayOfWeek.Monday)).AddDays(7 * i);
            weeks.Add((ISOWeek.GetYear(monday.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(monday.ToDateTime(TimeOnly.MinValue))));
        }

        var batch = new ImportBatch
        {
            ImportedAt = DateTime.UtcNow,
            ImportedBy = "seed",
            SourceFileName = "demo-seed.csv",
            ProcessedRows = workplaces.Count * weeks.Count,
            ErrorCount = 0,
            Confirmed = true,
            DiffSummary = "Initiële demo-capaciteit voor 4 weken"
        };
        db.ImportBatches.Add(batch);
        db.SaveChanges();

        foreach (var (year, week) in weeks)
        {
            foreach (var wp in workplaces)
            {
                var shifts = wp.MinimumStaffing <= 1 ? 5 : wp.MinimumStaffing >= 3 ? 10 : 8;
                db.CapacityNeeds.Add(new CapacityNeed
                {
                    ImportBatchId = batch.Id,
                    Year = year,
                    WeekNumber = week,
                    WorkplaceId = wp.Id,
                    RequiredShifts = shifts,
                    HoursPerShift = 8,
                    IsActive = true
                });
            }
        }
        db.SaveChanges();

        var crewPatterns = db.CrewPatterns.ToDictionary(p => p.Id);
        var patternDays = db.PatternDays.ToList().GroupBy(d => d.CrewPatternId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var crew in crews)
        {
            if (crew.CrewPatternId.HasValue && crewPatterns.TryGetValue(crew.CrewPatternId.Value, out var pat))
            {
                pat.Days = patternDays.GetValueOrDefault(pat.Id) ?? [];
                crew.CrewPattern = pat;
            }
        }
        foreach (var emp in employees)
            emp.PrimaryCrew = crews.First(c => c.Id == emp.PrimaryCrewId);

        var absences = db.Absences.Where(a => a.Status == AbsenceStatus.Approved).ToList();
        var empByCrew = employees.GroupBy(e => e.PrimaryCrewId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (year, week) in weeks)
        {
            var plan = new WeekPlan
            {
                Year = year,
                WeekNumber = week,
                Status = week == weeks[0].Week && year == weeks[0].Year ? WeekPlanStatus.Concept : WeekPlanStatus.Checked,
                Version = 1
            };
            db.WeekPlans.Add(plan);
            db.SaveChanges();

            var days = DaysOfIsoWeek(year, week)
                .Where(d => d.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
                .ToList();

            var crewPointers = crews.ToDictionary(c => c.Id, _ => 0);
            var assignedThisWeek = new HashSet<(int EmployeeId, DateOnly Date)>();

            foreach (var day in days)
            {
                for (var wpIndex = 0; wpIndex < workplaces.Count; wpIndex++)
                {
                    var wp = workplaces[wpIndex];
                    var seats = Math.Max(1, wp.MinimumStaffing);

                    for (var seat = 0; seat < seats; seat++)
                    {
                        var crew = crews[(wpIndex + seat) % crews.Length];
                        var pool = empByCrew[crew.Id];
                        Employee? chosen = null;
                        for (var attempt = 0; attempt < pool.Count; attempt++)
                        {
                            var idx = (crewPointers[crew.Id] + attempt) % pool.Count;
                            var candidate = pool[idx];
                            var onLeave = absences.Any(a =>
                                a.EmployeeId == candidate.Id && day >= a.StartDate && day <= a.EndDate);
                            if (onLeave) continue;
                            if (assignedThisWeek.Contains((candidate.Id, day))) continue;

                            chosen = candidate;
                            crewPointers[crew.Id] = (idx + 1) % pool.Count;
                            break;
                        }

                        if (chosen is null) continue;

                        var shift = ResolveShift(chosen, day);
                        if (shift.IsFree) continue;

                        assignedThisWeek.Add((chosen.Id, day));
                        db.ShiftAssignments.Add(new ShiftAssignment
                        {
                            WeekPlanId = plan.Id,
                            Date = day,
                            WorkplaceId = wp.Id,
                            EmployeeId = chosen.Id,
                            ShiftName = shift.Name,
                            StartTime = shift.Start,
                            EndTime = shift.End,
                            PlannedHours = shift.Hours
                        });
                    }
                }
            }

            db.SaveChanges();
        }
    }

    private static IReadOnlyList<DateOnly> DaysOfIsoWeek(int year, int week)
    {
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
        return Enumerable.Range(0, 7).Select(i => monday.AddDays(i)).ToList();
    }

    private static (string Name, TimeOnly Start, TimeOnly End, decimal Hours, bool IsFree) ResolveShift(Employee employee, DateOnly date)
    {
        var pattern = employee.PrimaryCrew?.CrewPattern;
        if (pattern?.Days is not { Count: > 0 })
            return ("Ochtend", new TimeOnly(6, 0), new TimeOnly(14, 0), 8, false);

        var weekIndex = 0;
        if (pattern.CycleWeeks > 1 && pattern.EffectiveFrom.HasValue)
        {
            var anchor = pattern.EffectiveFrom.Value;
            var anchorMonday = DateOnly.FromDateTime(ISOWeek.ToDateTime(ISOWeek.GetYear(anchor.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(anchor.ToDateTime(TimeOnly.MinValue)), DayOfWeek.Monday));
            var dateMonday = DateOnly.FromDateTime(ISOWeek.ToDateTime(ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)), DayOfWeek.Monday));
            var weeks = (dateMonday.DayNumber - anchorMonday.DayNumber) / 7;
            weekIndex = ((weeks % pattern.CycleWeeks) + pattern.CycleWeeks) % pattern.CycleWeeks;
        }

        var day = pattern.Days.FirstOrDefault(d => d.WeekIndex == weekIndex && d.DayOfWeek == date.DayOfWeek)
                  ?? pattern.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);
        if (day is null)
            return ("Ochtend", new TimeOnly(6, 0), new TimeOnly(14, 0), 8, false);

        if (string.Equals(day.ShiftName, "Vrij", StringComparison.OrdinalIgnoreCase) || day.NetHours <= 0 || day.StartTime is null || day.EndTime is null)
            return ("Vrij", new TimeOnly(6, 0), new TimeOnly(14, 0), 0, true);

        return (day.ShiftName, day.StartTime.Value, day.EndTime.Value, day.NetHours > 0 ? day.NetHours : 8, false);
    }

    private static CrewPattern CreateRotatingPattern(RepositoryContext db, string name, int phaseOffset)
    {
        var pattern = new CrewPattern
        {
            Name = name,
            CycleWeeks = 3,
            EffectiveFrom = PatternStart,
            Active = true
        };
        db.CrewPatterns.Add(pattern);
        db.SaveChanges();

        var shifts = new[]
        {
            (Name: "Ochtend", Start: new TimeOnly(6, 0), End: new TimeOnly(14, 0)),
            (Name: "Middag", Start: new TimeOnly(14, 0), End: new TimeOnly(22, 0)),
            (Name: "Nacht", Start: new TimeOnly(22, 0), End: new TimeOnly(6, 0))
        };

        for (var week = 0; week < 3; week++)
        {
            var shift = shifts[(week + phaseOffset) % 3];
            foreach (DayOfWeek day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            {
                db.PatternDays.Add(new PatternDay
                {
                    CrewPatternId = pattern.Id,
                    WeekIndex = week,
                    DayOfWeek = day,
                    ShiftName = shift.Name,
                    StartTime = shift.Start,
                    EndTime = shift.End,
                    PauseMinutes = 30,
                    NetHours = 8
                });
            }

            foreach (DayOfWeek day in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
            {
                db.PatternDays.Add(new PatternDay
                {
                    CrewPatternId = pattern.Id,
                    WeekIndex = week,
                    DayOfWeek = day,
                    ShiftName = "Vrij",
                    NetHours = 0
                });
            }
        }

        db.SaveChanges();
        return pattern;
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("planning|" + password));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyPassword(string password, string hash) =>
        string.Equals(HashPassword(password), hash, StringComparison.OrdinalIgnoreCase);
}
