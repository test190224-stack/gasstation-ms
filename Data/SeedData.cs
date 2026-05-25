using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Models;

namespace GasStationMS.Data
{
    /// <summary>
    /// Սկզբնական տվյալների բեռնում՝
    ///   - 5 դեր, admin user
    ///   - 5 վառելիքի տեսակ՝ markup-ով
    ///   - 3 կայան
    ///   - 12 ռեզերվուար
    ///   - 12 dispenser
    ///   - 3 մատակարար
    ///   - 5 աշխատակից (տարբեր դերերով + login-ներով)
    ///   - 10 հաճախորդ (տարբեր tier-ներով)
    ///   - Մատակարարումների պատմություն (FIFO batches, վերջին 30 օր)
    ///   - Վաճառքների պատմություն (վերջին 30 օր, ~300 գործարք)
    /// </summary>
    public static class SeedData
    {
        private static readonly Random _rng = new(42); // deterministic

        public static async Task InitializeAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();


            // 1. Roles
            string[] roles = { "Administrator", "NetworkManager", "Manager", "Operator", "Accountant" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // 2. Admin user
            var admin = await userManager.FindByEmailAsync("admin@gasstation.am");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@gasstation.am",
                    Email = "admin@gasstation.am",
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@12345");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Administrator");
            }

            // 3. Fuel types with markup
            if (!db.FuelTypes.Any())
            {
                db.FuelTypes.AddRange(
                    new FuelType { Name = "Regular A-92", Code = "A92", PricePerLiter = 470m, MarkupPercent = 15m },
                    new FuelType { Name = "Premium A-95", Code = "A95", PricePerLiter = 510m, MarkupPercent = 18m },
                    new FuelType { Name = "Super A-98",   Code = "A98", PricePerLiter = 560m, MarkupPercent = 20m },
                    new FuelType { Name = "Դիզել",         Code = "DSL", PricePerLiter = 520m, MarkupPercent = 16m },
                    new FuelType { Name = "LPG գազ",       Code = "LPG", PricePerLiter = 280m, MarkupPercent = 22m }
                );
                await db.SaveChangesAsync();
            }

            // 4. Stations
            if (!db.Stations.Any())
            {
                db.Stations.AddRange(
                    new Station { Name = "Կայան №1 - Կենտրոն", Address = "Երևան, Տիգրան Մեծի 15",
                        City = "Երևան", Latitude = 40.1792, Longitude = 44.4991, PhoneNumber = "+374 10 000001" },
                    new Station { Name = "Կայան №2 - Աջափնյակ", Address = "Երևան, Հալաբյան 20",
                        City = "Երևան", Latitude = 40.2027, Longitude = 44.4793, PhoneNumber = "+374 10 000002" },
                    new Station { Name = "Կայան №3 - Գյումրի", Address = "Գյումրի, Շիրակացի 5",
                        City = "Գյումրի", Latitude = 40.7942, Longitude = 43.8453, PhoneNumber = "+374 312 00003" }
                );
                await db.SaveChangesAsync();
            }

            // 5. Suppliers
            if (!db.Suppliers.Any())
            {
                db.Suppliers.AddRange(
                    new Supplier { Name = "ԱրմենՕյլ ՍՊԸ", TaxId = "00123456",
                        Address = "Երևան, Արշակունյաց 15", PhoneNumber = "+374 10 555001",
                        Email = "info@armenoil.am", ContactPerson = "Արմեն Պետրոսյան" },
                    new Supplier { Name = "ՊետրոլՔիմ", TaxId = "00234567",
                        Address = "Երևան, Ռուսական 22", PhoneNumber = "+374 10 555002",
                        Email = "sales@petrochim.am", ContactPerson = "Նարեկ Ավագյան" },
                    new Supplier { Name = "Գազպրոմ-Արմենիա", TaxId = "00345678",
                        Address = "Երևան, Իսահակյան 3", PhoneNumber = "+374 10 555003",
                        Email = "info@gazprom.am", ContactPerson = "Մարիամ Սարգսյան" }
                );
                await db.SaveChangesAsync();
            }

            // 6. Tanks
            if (!db.FuelTanks.Any())
            {
                var stations = db.Stations.ToList();
                var fuelTypes = db.FuelTypes.ToList();
                var tankNum = 1;
                foreach (var st in stations)
                foreach (var ft in fuelTypes.Take(4))
                {
                    db.FuelTanks.Add(new FuelTank
                    {
                        TankCode = $"T-{tankNum++:D3}",
                        StationId = st.Id,
                        FuelTypeId = ft.Id,
                        CapacityLiters = 20000m,
                        CurrentVolumeLiters = 0m, // Will be populated via deliveries
                        MinThresholdLiters = 1500m,
                        IsActive = true
                    });
                }
                await db.SaveChangesAsync();
            }

            // 7. Dispensers
            if (!db.Dispensers.Any())
            {
                var tanks = db.FuelTanks.ToList();
                var num = 1;
                foreach (var t in tanks)
                    db.Dispensers.Add(new Dispenser
                    {
                        DispenserCode = $"D-{num++:D3}",
                        StationId = t.StationId,
                        FuelTankId = t.Id,
                        IsOperational = true
                    });
                await db.SaveChangesAsync();
            }

            // 8. Employees
            if (!db.Employees.Any())
            {
                var stations = db.Stations.ToList();
                var employees = new List<(string first, string last, EmployeeRole role, int stationIdx, string email, decimal salary)>
                {
                    ("Արմեն",  "Պետրոսյան", EmployeeRole.Manager,        0, "manager1@gasstation.am", 350_000m),
                    ("Նարեկ",  "Ավագյան",   EmployeeRole.Manager,        1, "manager2@gasstation.am", 350_000m),
                    ("Գևորգ",  "Ղազարյան",  EmployeeRole.Operator,       0, "operator1@gasstation.am", 180_000m),
                    ("Լիլիթ",  "Սարգսյան",  EmployeeRole.Operator,       1, "operator2@gasstation.am", 180_000m),
                    ("Անի",    "Հակոբյան",  EmployeeRole.Accountant,     0, "accountant@gasstation.am", 280_000m),
                };

                foreach (var (first, last, role, stationIdx, email, salary) in employees)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email, Email = email, FullName = $"{last} {first}",
                        EmailConfirmed = true, IsActive = true
                    };
                    var result = await userManager.CreateAsync(user, "User@12345");
                    if (!result.Succeeded) continue;
                    await userManager.AddToRoleAsync(user, role.ToString());

                    var emp = new Employee
                    {
                        FirstName = first, LastName = last,
                        Email = email, PhoneNumber = $"+374 9{_rng.Next(0, 10)} {_rng.Next(100000, 999999)}",
                        Role = role, StationId = stations[stationIdx].Id,
                        IsActive = true, BaseSalary = salary, UserId = user.Id,
                        HiredAt = DateTime.UtcNow.AddMonths(-_rng.Next(6, 36))
                    };
                    db.Employees.Add(emp);
                    await db.SaveChangesAsync();

                    user.EmployeeId = emp.Id;
                    await userManager.UpdateAsync(user);
                }

                // Admin employee
                if (admin != null)
                {
                    var adminEmp = new Employee
                    {
                        FirstName = "Սիստեմ", LastName = "Ադմին",
                        Role = EmployeeRole.Administrator,
                        StationId = stations[0].Id, IsActive = true,
                        BaseSalary = 500_000m, UserId = admin.Id,
                        HiredAt = DateTime.UtcNow.AddYears(-2)
                    };
                    db.Employees.Add(adminEmp);
                    await db.SaveChangesAsync();
                    admin.EmployeeId = adminEmp.Id;
                    await userManager.UpdateAsync(admin);
                }
            }

            // 9. Customers with varied tiers
            if (!db.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new() { CardCode = "CARD-00001", FirstName = "Հակոբ",    LastName = "Մարտիրոսյան",
                        PhoneNumber = "+374 91 111001", Email = "hakob@example.am",
                        BonusPoints = 450, TotalSpent = 35_000m, Tier = LoyaltyTier.Bronze,
                        RegisteredAt = DateTime.UtcNow.AddDays(-90) },
                    new() { CardCode = "CARD-00002", FirstName = "Մարգարիտա", LastName = "Համբարձումյան",
                        PhoneNumber = "+374 91 111002", Email = "margo@example.am",
                        BonusPoints = 1200, TotalSpent = 120_000m, Tier = LoyaltyTier.Silver,
                        RegisteredAt = DateTime.UtcNow.AddDays(-180) },
                    new() { CardCode = "CARD-00003", FirstName = "Վահան",    LastName = "Թամրազյան",
                        PhoneNumber = "+374 91 111003", BonusPoints = 3500, TotalSpent = 450_000m,
                        Tier = LoyaltyTier.Gold, RegisteredAt = DateTime.UtcNow.AddDays(-270) },
                    new() { CardCode = "CARD-00004", FirstName = "Լևոն",     LastName = "Տեր-Պետրոսյան",
                        PhoneNumber = "+374 91 111004", BonusPoints = 12000, TotalSpent = 1_500_000m,
                        Tier = LoyaltyTier.Platinum, RegisteredAt = DateTime.UtcNow.AddDays(-400) },
                    new() { CardCode = "CARD-00005", FirstName = "Սոնա",     LastName = "Աբրահամյան",
                        PhoneNumber = "+374 91 111005", BonusPoints = 250, TotalSpent = 18_000m,
                        Tier = LoyaltyTier.Bronze, RegisteredAt = DateTime.UtcNow.AddDays(-45) },
                    new() { CardCode = "CARD-00006", FirstName = "Արթուր",   LastName = "Սիմոնյան",
                        PhoneNumber = "+374 91 111006", BonusPoints = 2100, TotalSpent = 320_000m,
                        Tier = LoyaltyTier.Gold, RegisteredAt = DateTime.UtcNow.AddDays(-220) },
                    new() { CardCode = "CARD-00007", FirstName = "Նունե",    LastName = "Մկրտչյան",
                        PhoneNumber = "+374 91 111007", BonusPoints = 800, TotalSpent = 85_000m,
                        Tier = LoyaltyTier.Silver, RegisteredAt = DateTime.UtcNow.AddDays(-120) },
                    new() { CardCode = "CARD-00008", FirstName = "Դավիթ",    LastName = "Խաչատրյան",
                        PhoneNumber = "+374 91 111008", BonusPoints = 5400, TotalSpent = 780_000m,
                        Tier = LoyaltyTier.Gold, RegisteredAt = DateTime.UtcNow.AddDays(-350) },
                    new() { CardCode = "CARD-00009", FirstName = "Արմինե",   LastName = "Գևորգյան",
                        PhoneNumber = "+374 91 111009", BonusPoints = 180, TotalSpent = 12_500m,
                        Tier = LoyaltyTier.Bronze, RegisteredAt = DateTime.UtcNow.AddDays(-30) },
                    new() { CardCode = "CARD-00010", FirstName = "Սամվել",   LastName = "Առաքելյան",
                        PhoneNumber = "+374 91 111010", BonusPoints = 25000, TotalSpent = 3_200_000m,
                        Tier = LoyaltyTier.Platinum, RegisteredAt = DateTime.UtcNow.AddDays(-500) }
                };
                db.Customers.AddRange(customers);
                await db.SaveChangesAsync();
            }

            // 10. Coupons
            if (!db.Coupons.Any())
            {
                db.Coupons.AddRange(
                    new Coupon { Code = "CPN-10OFF", Type = CouponType.PercentDiscount,
                        DiscountPercentage = 10m, ExpiresAt = DateTime.UtcNow.AddDays(30), IssuedTo = "Կորպորատիվ ակցիա" },
                    new Coupon { Code = "CPN-5000", Type = CouponType.PrepaidAmount,
                        FaceValue = 5000m, ExpiresAt = DateTime.UtcNow.AddDays(60), IssuedTo = "Նվեր" },
                    new Coupon { Code = "CPN-20L", Type = CouponType.PrepaidVolume,
                        VolumeLiters = 20m, ExpiresAt = DateTime.UtcNow.AddDays(90),
                        FuelTypeId = db.FuelTypes.First(f => f.Code == "A95").Id, IssuedTo = "Մանր․ հաճախորդ" }
                );
                await db.SaveChangesAsync();
            }

            // 11. Deliveries (FIFO batches) - populate tanks with historical data
            if (!db.FuelDeliveries.Any())
            {
                var tanks = db.FuelTanks.ToList();
                var suppliers = db.Suppliers.ToList();
                var now = DateTime.UtcNow;

                foreach (var tank in tanks)
                {
                    // 2-3 batches per tank over last 30 days
                    var batchCount = _rng.Next(2, 4);
                    for (int b = 0; b < batchCount; b++)
                    {
                        var daysAgo = 30 - b * (30 / batchCount);
                        var volume = 3000m + _rng.Next(0, 5000);
                        // Slight price variation per batch
                        var basePrice = tank.FuelTypeId switch
                        {
                            1 => 400m, 2 => 430m, 3 => 475m, 4 => 440m, 5 => 230m, _ => 400m
                        };
                        var price = basePrice + _rng.Next(-15, 25);

                        var delivery = new FuelDelivery
                        {
                            FuelTankId = tank.Id,
                            SupplierId = suppliers[_rng.Next(suppliers.Count)].Id,
                            VolumeLiters = volume,
                            RemainingLiters = volume, // will be reduced by sales
                            PricePerLiter = price,
                            TotalCost = volume * price,
                            InvoiceNumber = $"INV-2026-{_rng.Next(1000, 9999)}",
                            DeliveredAt = now.AddDays(-daysAgo).AddHours(-_rng.Next(0, 24)),
                            Notes = b == 0 ? "Սկզբնական batch" : null
                        };
                        db.FuelDeliveries.Add(delivery);
                        tank.CurrentVolumeLiters += volume;
                    }
                }
                await db.SaveChangesAsync();
            }

            // 12. Historical shifts (one per operator per day for last 30 days)
            if (!db.Shifts.Any())
            {
                var stations = db.Stations.ToList();
                var operators = await db.Employees
                    .Where(e => e.Role == EmployeeRole.Operator)
                    .ToListAsync();

                // Current active shift
                if (operators.Any())
                {
                    db.Shifts.Add(new Shift
                    {
                        StationId = operators[0].StationId ?? stations[0].Id,
                        EmployeeId = operators[0].Id,
                        StartedAt = DateTime.UtcNow.AddHours(-3),
                        OpeningCash = 100000m,
                        Status = ShiftStatus.Open
                    });
                }
                await db.SaveChangesAsync();
            }

            // 13. Historical sales (populate charts)
            if (!db.Sales.Any())
            {
                var currentShift = await db.Shifts.FirstOrDefaultAsync(s => s.Status == ShiftStatus.Open);
                if (currentShift != null)
                {
                    var dispensers = await db.Dispensers
                        .Include(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                        .Where(d => d.StationId == currentShift.StationId)
                        .ToListAsync();
                    var customers = db.Customers.ToList();
                    var allDispensers = await db.Dispensers
                        .Include(d => d.FuelTank).ThenInclude(t => t!.FuelType)
                        .ToListAsync();

                    // Generate ~300 sales across last 30 days on CURRENT SHIFT'S station only
                    // (simpler — use current shift for all historical sales to avoid FK issues)
                    var now = DateTime.UtcNow;
                    for (int i = 0; i < 300; i++)
                    {
                        var dispenser = dispensers[_rng.Next(dispensers.Count)];
                        var volume = Math.Round((decimal)(10 + _rng.NextDouble() * 40), 2);
                        var basePrice = dispenser.FuelTank!.FuelType!.PricePerLiter;
                        var daysAgo = _rng.Next(0, 30);
                        var hoursAgo = _rng.Next(0, 24);

                        // Bias hours: peak around 8-10 AM and 5-7 PM
                        if (_rng.NextDouble() > 0.4)
                            hoursAgo = _rng.Next(0, 2) == 0
                                ? _rng.Next(8, 11) : _rng.Next(17, 20);

                        var soldAt = now.AddDays(-daysAgo).AddHours(-24 + hoursAgo)
                            .AddMinutes(-_rng.Next(0, 60));
                        if (soldAt > now) soldAt = now.AddMinutes(-_rng.Next(1, 120));

                        var cost = basePrice * 0.85m; // estimated cost
                        var total = volume * basePrice;
                        var discount = 0m;
                        int? customerId = null;
                        int pointsEarned = 0;
                        var paymentMethod = (PaymentMethod)(_rng.Next(1, 5));

                        // 30% chance of customer card
                        if (_rng.NextDouble() < 0.3)
                        {
                            var c = customers[_rng.Next(customers.Count)];
                            customerId = c.Id;
                            pointsEarned = (int)(total * c.CashbackPercent / 100m);
                            paymentMethod = PaymentMethod.LoyaltyCard;
                        }

                        db.Sales.Add(new Sale
                        {
                            ReceiptNumber = $"RCP-SEED-{i:D4}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                            StationId = currentShift.StationId,
                            ShiftId = currentShift.Id,
                            DispenserId = dispenser.Id,
                            FuelTypeId = dispenser.FuelTank.FuelTypeId,
                            VolumeLiters = volume,
                            PricePerLiter = basePrice,
                            CostPerLiter = cost,
                            TotalAmount = total,
                            DiscountAmount = discount,
                            NetAmount = total - discount,
                            Profit = (basePrice - cost) * volume - discount,
                            PaymentMethod = paymentMethod,
                            CustomerId = customerId,
                            BonusPointsEarned = pointsEarned,
                            SoldAt = soldAt
                        });
                    }

                    // Reduce tank remaining batches proportionally (simulate FIFO consumption)
                    var tanks = await db.FuelTanks.ToListAsync();
                    var deliveries = await db.FuelDeliveries
                        .OrderBy(d => d.DeliveredAt).ToListAsync();
                    foreach (var tank in tanks)
                    {
                        // consume ~30-50% of tank
                        var consumed = tank.CurrentVolumeLiters * (decimal)(0.3 + _rng.NextDouble() * 0.2);
                        tank.CurrentVolumeLiters -= consumed;
                        var remain = consumed;
                        foreach (var batch in deliveries.Where(d => d.FuelTankId == tank.Id))
                        {
                            if (remain <= 0) break;
                            var take = Math.Min(batch.RemainingLiters, remain);
                            batch.RemainingLiters -= take;
                            remain -= take;
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
