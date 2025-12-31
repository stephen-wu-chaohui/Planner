-- 1. Setup Temporary Data Structures for the loops
DECLARE @Cities TABLE (Name NVARCHAR(50), Lat FLOAT, Lon FLOAT);
INSERT INTO @Cities VALUES 
    ('Taipei', 25.0330, 121.5654), ('Perth', -31.9523, 115.8613),
    ('Sydney', -33.8688, 151.2093), ('Melbourne', -37.8136, 144.9631),
    ('Auckland', -36.8485, 174.7633), ('Christchurch', -43.5321, 172.6362);

DECLARE @PopularNames TABLE (Name NVARCHAR(50));
INSERT INTO @PopularNames VALUES 
    ('Oliver'), ('Noah'), ('Leo'), ('Henry'), ('Jack'), 
    ('Charlotte'), ('Isla'), ('Amelia'), ('Mia'), ('Ava');

-- Variables for the loop logic
DECLARE @CityName NVARCHAR(50), @CityLat FLOAT, @CityLon FLOAT;
DECLARE @TenantId UNIQUEIDENTIFIER, @DepotId INT, @LocationId INT, @CustomerId INT;
DECLARE @i INT, @j INT;

-- 2. Clear existing data (Equivalent to EnsureDeleted/Migrate)
-- Order matters due to FK constraints
DELETE FROM Jobs;
DELETE FROM Vehicles;
DELETE FROM Customers;
DELETE FROM Depots;
DELETE FROM Locations;
DELETE FROM Tenants;

-- 3. Iterate through each City
DECLARE CityCursor CURSOR FOR SELECT Name, Lat, Lon FROM @Cities;
OPEN CityCursor;
FETCH NEXT FROM CityCursor INTO @CityName, @CityLat, @CityLon;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- A. Generate Tenant
    SET @TenantId = NEWID();
    INSERT INTO Tenants (Id, Name, CreatedAt)
    VALUES (@TenantId, @CityName, GETUTCDATE());

    -- B. Generate Depot Location & Depot
    INSERT INTO Locations (Address, Latitude, Longitude)
    VALUES (@CityName + ' Main Depot Address', @CityLat, @CityLon);
    SET @LocationId = SCOPE_IDENTITY();

    INSERT INTO Depots (TenantId, Name, LocationId) -- Assuming LocationId is the FK
    VALUES (@TenantId, @CityName + ' Main Depot', @LocationId);
    SET @DepotId = SCOPE_IDENTITY();

    -- C. Generate 20 Customers and Jobs for each tenant
    SET @i = 1;
    WHILE @i <= 20
    BEGIN
        -- Calculate Random Offset (within 10km)
        -- Using CRYPT_GEN_RANDOM for better randomness in a loop than RAND()
        DECLARE @LatOffset FLOAT = ((CAST(CAST(CRYPT_GEN_RANDOM(4) AS VARBINARY) AS INT) / 2147483647.0)) * (10.0 / 111.0);
        DECLARE @LonOffset FLOAT = ((CAST(CAST(CRYPT_GEN_RANDOM(4) AS VARBINARY) AS INT) / 2147483647.0)) * (10.0 / 111.0);

        -- Create Customer Location
        INSERT INTO Locations (Address, Latitude, Longitude)
        VALUES (@CityName + ' Client ' + CAST(@i AS NVARCHAR) + ' Address', @CityLat + @LatOffset, @CityLon + @LonOffset);
        SET @LocationId = SCOPE_IDENTITY();

        -- Create Customer
        INSERT INTO Customers (TenantId, Name, LocationId, DefaultServiceMinutes, RequiresRefrigeration)
        VALUES (@TenantId, @CityName + ' Client ' + CAST(@i AS NVARCHAR), @LocationId, 15, 0);
        SET @CustomerId = SCOPE_IDENTITY();

        -- Create Job for the Customer
        INSERT INTO Jobs (TenantId, Name, CustomerID, JobType, LocationId, ServiceTimeMinutes, Reference, PalletDemand, WeightDemand, ReadyTime, DueTime, RequiresRefrigeration, OrderId)
        VALUES (
            @TenantId, 
            'Delivery to ' + @CityName + ' Client ' + CAST(@i AS NVARCHAR), 
            @CustomerId, 
            2, -- JobType.Delivery
            @LocationId, 
            15, 
            'REF-' + UPPER(@CityName) + '-' + RIGHT('000' + CAST(@i AS NVARCHAR), 3),
            0, 0, 0, 0, 0, 0
        );

        SET @i = @i + 1;
    END

    -- D. Generate 4 Drivers (Vehicles) for each tenant
    SET @j = 0;
    WHILE @j < 4
    BEGIN
        DECLARE @RandomName NVARCHAR(50) = (SELECT TOP 1 Name FROM @PopularNames ORDER BY NEWID());

        INSERT INTO Vehicles (TenantId, Name, DepotStartId, DepotEndId, MaxPallets, FuelRatePerKm, SpeedFactor, ShiftLimitMinutes, DriverRatePerHour, MaintenanceRatePerHour, BaseFee, MaxWeight, RefrigeratedCapacity)
        VALUES (@TenantId, @RandomName, @DepotId, @DepotId, 10, 1.5, 1.0, 480, 0, 0, 0, 0, 0);

        SET @j = @j + 1;
    END

    FETCH NEXT FROM CityCursor INTO @CityName, @CityLat, @CityLon;
END

CLOSE CityCursor;
DEALLOCATE CityCursor;