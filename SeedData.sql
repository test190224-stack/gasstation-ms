-- ============================================================
-- GasStation MS - Seed data script (backup / manual option)
-- ============================================================
-- Օգտագործում՝ Եթե SeedData.cs ավտոմատ չի աշխատում, կարող ես
-- այս script-ը run անել SQL Server Management Studio-ում՝
-- հենց նոր ստեղծված GasStationMSDb տվյալների բազայի վրա։
--
-- ՇԱՏ ԿԱՐԵՎՈՐ։ Սա չի ներառում ASP.NET Core Identity-ի users և
-- roles (admin հաշիվ), որոնք հատկապես պահանջում են hash-ավորված
-- գաղտնաբառեր։ Admin user-ը ստեղծելու համար պարտադիր run արա
-- ծրագիրը մեկ անգամ (SeedData.cs-ը ստեղծում է admin@gasstation.am-ը)
-- հետո թող այս script-ը լրացնի մնացած տվյալները։
-- ============================================================

USE GasStationMSDb;
GO

-- Մաքրում (զգուշությամբ՝ ջնջում է բոլոր existing տվյալները)
-- Հանիր comment-ները միայն եթե ուզում ես reset անել
-- DELETE FROM Sales;
-- DELETE FROM FuelDeliveries;
-- DELETE FROM Shifts;
-- DELETE FROM Dispensers;
-- DELETE FROM FuelTanks;
-- DELETE FROM Coupons;
-- DELETE FROM Customers;
-- DELETE FROM Employees;
-- DELETE FROM Suppliers;
-- DELETE FROM Stations;
-- DELETE FROM FuelTypes;

-- ============================================================
-- 1. FuelTypes (5 տեսակ՝ markup-ով)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM FuelTypes)
BEGIN
    INSERT INTO FuelTypes (Name, Code, PricePerLiter, MarkupPercent, IsActive)
    VALUES
        (N'Regular A-92', 'A92', 470.00, 15.00, 1),
        (N'Premium A-95', 'A95', 510.00, 18.00, 1),
        (N'Super A-98',   'A98', 560.00, 20.00, 1),
        (N'Դիզել',         'DSL', 520.00, 16.00, 1),
        (N'LPG գազ',       'LPG', 280.00, 22.00, 1);
END
GO

-- ============================================================
-- 2. Stations (3 կայան)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Stations)
BEGIN
    INSERT INTO Stations (Name, Address, City, Latitude, Longitude, PhoneNumber, Status, CreatedAt)
    VALUES
        (N'Կայան №1 - Կենտրոն', N'Երևան, Տիգրան Մեծի 15',   N'Երևան', 40.1792, 44.4991, '+374 10 000001', 1, GETUTCDATE()),
        (N'Կայան №2 - Աջափնյակ', N'Երևան, Հալաբյան 20',       N'Երևան', 40.2027, 44.4793, '+374 10 000002', 1, GETUTCDATE()),
        (N'Կայան №3 - Գյումրի',  N'Գյումրի, Շիրակացի 5',       N'Գյումրի', 40.7942, 43.8453, '+374 312 00003', 1, GETUTCDATE());
END
GO

-- ============================================================
-- 3. Suppliers (3 մատակարարող)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Suppliers)
BEGIN
    INSERT INTO Suppliers (Name, TaxId, Address, PhoneNumber, Email, ContactPerson, IsActive)
    VALUES
        (N'ԱրմենՕյլ ՍՊԸ',       '00123456', N'Երևան, Արշակունյաց 15', '+374 10 555001', 'info@armenoil.am',   N'Արմեն Պետրոսյան', 1),
        (N'ՊետրոլՔիմ',           '00234567', N'Երևան, Ռուսական 22',    '+374 10 555002', 'sales@petrochim.am', N'Նարեկ Ավագյան',   1),
        (N'Գազպրոմ-Արմենիա',      '00345678', N'Երևան, Իսահակյան 3',    '+374 10 555003', 'info@gazprom.am',     N'Մարիամ Սարգսյան', 1);
END
GO

-- ============================================================
-- 4. FuelTanks (12 ռեզերվուար՝ 4 ամեն կայանում)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM FuelTanks)
BEGIN
    DECLARE @stationIds TABLE (Id INT, RowNum INT);
    INSERT INTO @stationIds SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM Stations;

    DECLARE @fuelIds TABLE (Id INT, RowNum INT);
    INSERT INTO @fuelIds SELECT TOP 4 Id, ROW_NUMBER() OVER (ORDER BY Id) FROM FuelTypes ORDER BY Id;

    DECLARE @stationId INT, @fuelId INT, @num INT = 1;
    DECLARE station_cursor CURSOR FOR SELECT Id FROM @stationIds ORDER BY RowNum;
    OPEN station_cursor;
    FETCH NEXT FROM station_cursor INTO @stationId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE fuel_cursor CURSOR FOR SELECT Id FROM @fuelIds ORDER BY RowNum;
        OPEN fuel_cursor;
        FETCH NEXT FROM fuel_cursor INTO @fuelId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            INSERT INTO FuelTanks (TankCode, StationId, FuelTypeId, CapacityLiters,
                CurrentVolumeLiters, MinThresholdLiters, LastUpdated, IsActive)
            VALUES (CONCAT('T-', FORMAT(@num, '000')), @stationId, @fuelId,
                    20000.00, 0.00, 1500.00, GETUTCDATE(), 1);
            SET @num = @num + 1;
            FETCH NEXT FROM fuel_cursor INTO @fuelId;
        END
        CLOSE fuel_cursor;
        DEALLOCATE fuel_cursor;
        FETCH NEXT FROM station_cursor INTO @stationId;
    END
    CLOSE station_cursor;
    DEALLOCATE station_cursor;
END
GO

-- ============================================================
-- 5. Dispensers (1 dispenser ամեն tank-ի համար)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Dispensers)
BEGIN
    DECLARE @num INT = 1;
    DECLARE @tankId INT, @stationId INT;
    DECLARE tank_cursor CURSOR FOR SELECT Id, StationId FROM FuelTanks ORDER BY Id;
    OPEN tank_cursor;
    FETCH NEXT FROM tank_cursor INTO @tankId, @stationId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO Dispensers (DispenserCode, StationId, FuelTankId, IsOperational, TotalDispensedLiters)
        VALUES (CONCAT('D-', FORMAT(@num, '000')), @stationId, @tankId, 1, 0.00);
        SET @num = @num + 1;
        FETCH NEXT FROM tank_cursor INTO @tankId, @stationId;
    END
    CLOSE tank_cursor;
    DEALLOCATE tank_cursor;
END
GO

-- ============================================================
-- 6. Customers (10 հաճախորդ տարբեր tier-ներով)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Customers)
BEGIN
    INSERT INTO Customers (CardCode, FirstName, LastName, PhoneNumber, Email,
        BonusPoints, TotalSpent, Tier, RegisteredAt, IsActive)
    VALUES
        ('CARD-00001', N'Հակոբ',     N'Մարտիրոսյան',  '+374 91 111001', 'hakob@example.am',   450,   35000.00,   1, DATEADD(day, -90, GETUTCDATE()), 1),
        ('CARD-00002', N'Մարգարիտա', N'Համբարձումյան', '+374 91 111002', 'margo@example.am',  1200,  120000.00,   2, DATEADD(day, -180, GETUTCDATE()), 1),
        ('CARD-00003', N'Վահան',     N'Թամրազյան',    '+374 91 111003', NULL,                 3500,  450000.00,   3, DATEADD(day, -270, GETUTCDATE()), 1),
        ('CARD-00004', N'Լևոն',      N'Տեր-Պետրոսյան', '+374 91 111004', NULL,                12000, 1500000.00,   4, DATEADD(day, -400, GETUTCDATE()), 1),
        ('CARD-00005', N'Սոնա',      N'Աբրահամյան',   '+374 91 111005', NULL,                  250,   18000.00,   1, DATEADD(day, -45, GETUTCDATE()), 1),
        ('CARD-00006', N'Արթուր',    N'Սիմոնյան',      '+374 91 111006', NULL,                 2100,  320000.00,   3, DATEADD(day, -220, GETUTCDATE()), 1),
        ('CARD-00007', N'Նունե',     N'Մկրտչյան',      '+374 91 111007', NULL,                  800,   85000.00,   2, DATEADD(day, -120, GETUTCDATE()), 1),
        ('CARD-00008', N'Դավիթ',     N'Խաչատրյան',    '+374 91 111008', NULL,                 5400,  780000.00,   3, DATEADD(day, -350, GETUTCDATE()), 1),
        ('CARD-00009', N'Արմինե',    N'Գևորգյան',     '+374 91 111009', NULL,                  180,   12500.00,   1, DATEADD(day, -30, GETUTCDATE()), 1),
        ('CARD-00010', N'Սամվել',    N'Առաքելյան',     '+374 91 111010', NULL,                25000, 3200000.00,   4, DATEADD(day, -500, GETUTCDATE()), 1);
END
GO

-- ============================================================
-- 7. Coupons (3 տարբեր տեսակի)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Coupons)
BEGIN
    INSERT INTO Coupons (Code, Type, FuelTypeId, VolumeLiters, FaceValue, DiscountPercentage,
        IssuedAt, ExpiresAt, Status, IssuedTo)
    VALUES
        ('CPN-10OFF', 3, NULL, NULL, NULL, 10.00,  GETUTCDATE(), DATEADD(day, 30, GETUTCDATE()), 1, N'Կորպորատիվ ակցիա'),
        ('CPN-5000',  2, NULL, NULL, 5000.00, NULL, GETUTCDATE(), DATEADD(day, 60, GETUTCDATE()), 1, N'Նվեր քարտ'),
        ('CPN-20L',   1, (SELECT Id FROM FuelTypes WHERE Code='A95'), 20.00, NULL, NULL,
                                               GETUTCDATE(), DATEADD(day, 90, GETUTCDATE()), 1, N'Մանր․ հաճախորդ');
END
GO

-- ============================================================
-- 8. FuelDeliveries (batch-եր ամեն ռեզերվուարի համար)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM FuelDeliveries)
BEGIN
    DECLARE @tankId INT, @fuelTypeId INT, @capacity DECIMAL(10,2);
    DECLARE @basePrice DECIMAL(10,2);
    DECLARE @supplier1 INT = (SELECT TOP 1 Id FROM Suppliers WHERE Name LIKE N'ԱրմենՕյլ%');
    DECLARE @supplier2 INT = (SELECT TOP 1 Id FROM Suppliers WHERE Name LIKE N'ՊետրոլՔիմ%');

    DECLARE tank_cursor CURSOR FOR
        SELECT t.Id, t.FuelTypeId, t.CapacityLiters
        FROM FuelTanks t ORDER BY t.Id;
    OPEN tank_cursor;
    FETCH NEXT FROM tank_cursor INTO @tankId, @fuelTypeId, @capacity;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Determine base cost price by fuel type
        SELECT @basePrice = CASE Code
            WHEN 'A92' THEN 400.00
            WHEN 'A95' THEN 430.00
            WHEN 'A98' THEN 475.00
            WHEN 'DSL' THEN 440.00
            WHEN 'LPG' THEN 230.00
            ELSE 400.00
        END FROM FuelTypes WHERE Id = @fuelTypeId;

        -- Batch 1: old delivery (20 days ago) at lower price
        INSERT INTO FuelDeliveries (FuelTankId, SupplierId, VolumeLiters, RemainingLiters,
            PricePerLiter, TotalCost, InvoiceNumber, DeliveredAt, Notes)
        VALUES (@tankId, @supplier1, 5000.00, 3000.00,
                @basePrice - 10, 5000.00 * (@basePrice - 10),
                CONCAT('INV-2026-', 1000 + @tankId), DATEADD(day, -20, GETUTCDATE()),
                N'Հին batch');

        -- Batch 2: newer delivery (5 days ago) at higher price
        INSERT INTO FuelDeliveries (FuelTankId, SupplierId, VolumeLiters, RemainingLiters,
            PricePerLiter, TotalCost, InvoiceNumber, DeliveredAt, Notes)
        VALUES (@tankId, @supplier2, 6000.00, 6000.00,
                @basePrice + 15, 6000.00 * (@basePrice + 15),
                CONCAT('INV-2026-', 2000 + @tankId), DATEADD(day, -5, GETUTCDATE()),
                N'Նոր batch');

        -- Update tank current volume = sum of RemainingLiters
        UPDATE FuelTanks SET CurrentVolumeLiters = 9000.00, LastUpdated = GETUTCDATE()
        WHERE Id = @tankId;

        FETCH NEXT FROM tank_cursor INTO @tankId, @fuelTypeId, @capacity;
    END
    CLOSE tank_cursor;
    DEALLOCATE tank_cursor;
END
GO

-- ============================================================
-- ԾԱՆՈԹՈՒԹՅՈՒՆ։
-- Employees, ApplicationUsers, Shifts, Sales տվյալները ստեղծվում
-- են ավտոմատ SeedData.cs-ով երբ ծրագիրը run ես, քանի որ դրանք
-- պահանջում են Identity hash-ավորված գաղտնաբառեր և կապված են
-- ASP.NET Identity-ի հետ։
-- ============================================================

SELECT 'Seed data ավարտված է' AS Status;
SELECT 'FuelTypes' AS [Table], COUNT(*) AS Count FROM FuelTypes
UNION ALL SELECT 'Stations',  COUNT(*) FROM Stations
UNION ALL SELECT 'Suppliers', COUNT(*) FROM Suppliers
UNION ALL SELECT 'FuelTanks', COUNT(*) FROM FuelTanks
UNION ALL SELECT 'Dispensers', COUNT(*) FROM Dispensers
UNION ALL SELECT 'Customers',  COUNT(*) FROM Customers
UNION ALL SELECT 'Coupons',    COUNT(*) FROM Coupons
UNION ALL SELECT 'FuelDeliveries', COUNT(*) FROM FuelDeliveries;
