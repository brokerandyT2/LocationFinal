using Microsoft.Extensions.Logging;

using x3squaredcircles.SQLData.Generator;

namespace x3squaredcircles.SQLData.Generator.TestDataGenerator.Services;

public class RealisticDataEngine
{
    private readonly ILogger<RealisticDataEngine> _logger;
    private readonly Random _random = new Random();

    public RealisticDataEngine(ILogger<RealisticDataEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates contextually appropriate data based on column name and schema
    /// </summary>
    public object? GenerateRealisticValue(string schemaName, string tableName, string columnName, string dataType, TestDataOptions options)
    {
        // Use intelligent naming to generate appropriate data
        var lowerColumnName = columnName.ToLower();
        var lowerTableName = tableName.ToLower();

        _logger.LogDebug("Generating realistic data for {Schema}.{Table}.{Column} ({DataType})",
            schemaName, tableName, columnName, dataType);

        return schemaName.ToLower() switch
        {
            "photography" => GeneratePhotographyData(lowerTableName, lowerColumnName, dataType),
            "fishing" => GenerateFishingData(lowerTableName, lowerColumnName, dataType),
            "hunting" => GenerateHuntingData(lowerTableName, lowerColumnName, dataType),
            "core" => GenerateCoreData(lowerTableName, lowerColumnName, dataType),
            _ => GenerateGenericData(lowerColumnName, dataType)
        };
    }

    #region Photography Domain Data
    private object? GeneratePhotographyData(string tableName, string columnName, string dataType)
    {
        return columnName switch
        {
            // Camera brands and models
            "brand" when tableName.Contains("camera") => GetRandomCameraBrand(),
            "model" when tableName.Contains("camera") => GetRandomCameraModel(),
            "mount" when tableName.Contains("lens") => GetRandomLensMount(),

            // Lens specifications
            "focallength" => GetRandomFocalLength(),
            "maxaperture" => GetRandomAperture(),
            "minimumaperture" => GetRandomMinimumAperture(),

            // Photography locations
            "location" or "locationname" => GetRandomPhotoLocation(),
            "latitude" when tableName.Contains("location") => GetRandomLatitude(),
            "longitude" when tableName.Contains("location") => GetRandomLongitude(),

            // Photography metadata
            "iso" => GetRandomISO(),
            "shutterspeed" => GetRandomShutterSpeed(),
            "aperture" => GetRandomAperture(),

            // Prices (photography equipment)
            "price" or "cost" => GetRandomCameraPrice(),

            _ => GenerateGenericData(columnName, dataType)
        };
    }

    private string GetRandomCameraBrand()
    {
        var brands = new[] { "Canon", "Nikon", "Sony", "Fujifilm", "Panasonic", "Olympus", "Leica", "Pentax" };
        return brands[_random.Next(brands.Length)];
    }

    private string GetRandomCameraModel()
    {
        var models = new[] { "EOS R5", "D850", "A7R IV", "X-T4", "GH5", "OM-1", "Q2", "K-3 III" };
        return models[_random.Next(models.Length)];
    }

    private string GetRandomLensMount()
    {
        var mounts = new[] { "EF", "RF", "F", "E", "X", "MFT", "L", "K" };
        return mounts[_random.Next(mounts.Length)];
    }

    private int GetRandomFocalLength()
    {
        var focalLengths = new[] { 24, 35, 50, 85, 105, 200, 400, 600 };
        return focalLengths[_random.Next(focalLengths.Length)];
    }

    private decimal GetRandomAperture()
    {
        var apertures = new[] { 1.4m, 1.8m, 2.8m, 4.0m, 5.6m, 8.0m };
        return apertures[_random.Next(apertures.Length)];
    }

    private decimal GetRandomMinimumAperture()
    {
        var apertures = new[] { 16m, 22m, 32m };
        return apertures[_random.Next(apertures.Length)];
    }

    private string GetRandomPhotoLocation()
    {
        var locations = new[] { 
            // US Locations
            "Yosemite National Park", "Grand Canyon", "Yellowstone", "Antelope Canyon",
            "Bryce Canyon", "Zion National Park", "Glacier National Park", "Arches National Park",
            // International Photography Destinations with Unicode
            "Santorini, Ελλάδα", "Château de Versailles", "Neuschwanstein, Bayern",
            "Hallstatt, Österreich", "Cinque Terre, Italia", "Český Krumlov",
            "Geirangerfjord, Norge", "Alhambra, Granada", "Mont-Saint-Michel",
            "Plitvička jezera", "Meteora, Ελλάδα", "Trolltunga, Norge",
            "富士山, 日本", "万里长城", "앙코르와트", "Мачу-Пикчу"
        };
        return locations[_random.Next(locations.Length)];
    }

    private decimal GetRandomLatitude()
    {
        return (decimal)(_random.NextDouble() * 60 + 25); // US latitude range roughly
    }

    private decimal GetRandomLongitude()
    {
        return (decimal)(_random.NextDouble() * -50 - 125); // US longitude range roughly
    }

    private int GetRandomISO()
    {
        var isoValues = new[] { 100, 200, 400, 800, 1600, 3200, 6400 };
        return isoValues[_random.Next(isoValues.Length)];
    }

    private string GetRandomShutterSpeed()
    {
        var speeds = new[] { "1/1000", "1/500", "1/250", "1/125", "1/60", "1/30", "1/15" };
        return speeds[_random.Next(speeds.Length)];
    }

    private decimal GetRandomCameraPrice()
    {
        return _random.Next(500, 5000); // $500-$5000 range for cameras
    }
    #endregion

    #region Fishing Domain Data
    private object? GenerateFishingData(string tableName, string columnName, string dataType)
    {
        return columnName switch
        {
            "species" or "fishspecies" => GetRandomFishSpecies(),
            "waterbody" or "lake" or "river" => GetRandomWaterBody(),
            "bait" or "lure" => GetRandomBait(),
            "weight" when tableName.Contains("catch") => GetRandomFishWeight(),
            "length" when tableName.Contains("catch") => GetRandomFishLength(),
            "location" or "fishingspot" => GetRandomFishingLocation(),
            "season" => GetRandomFishingSeason(),
            _ => GenerateGenericData(columnName, dataType)
        };
    }

    private string GetRandomFishSpecies()
    {
        var species = new[] { "Bass", "Trout", "Salmon", "Pike", "Walleye", "Catfish", "Bluegill", "Perch" };
        return species[_random.Next(species.Length)];
    }

    private string GetRandomWaterBody()
    {
        var waters = new[] { "Lake Tahoe", "Colorado River", "Mississippi River", "Lake Michigan",
            "Chesapeake Bay", "Lake George", "Snake River", "Lake Powell" };
        return waters[_random.Next(waters.Length)];
    }

    private string GetRandomBait()
    {
        var baits = new[] { "Worms", "Minnows", "Spinnerbait", "Crankbait", "Jig", "Fly", "Topwater", "Soft Plastic" };
        return baits[_random.Next(baits.Length)];
    }

    private decimal GetRandomFishWeight()
    {
        return (decimal)(_random.NextDouble() * 10 + 0.5); // 0.5-10.5 lbs
    }

    private decimal GetRandomFishLength()
    {
        return (decimal)(_random.NextDouble() * 20 + 5); // 5-25 inches
    }

    private string GetRandomFishingLocation()
    {
        var locations = new[] { "North Shore", "Deep Water", "Shallow Bay", "Rocky Outcrop",
            "Weed Bed", "Drop Off", "Creek Mouth", "Boat Dock" };
        return locations[_random.Next(locations.Length)];
    }

    private string GetRandomFishingSeason()
    {
        var seasons = new[] { "Spring", "Summer", "Fall", "Winter" };
        return seasons[_random.Next(seasons.Length)];
    }
    #endregion

    #region Hunting Domain Data
    private object? GenerateHuntingData(string tableName, string columnName, string dataType)
    {
        return columnName switch
        {
            "species" or "gamespecies" => GetRandomGameSpecies(),
            "weapon" or "firearm" => GetRandomWeapon(),
            "location" or "huntingground" => GetRandomHuntingLocation(),
            "season" => GetRandomHuntingSeason(),
            "weight" when tableName.Contains("harvest") => GetRandomGameWeight(),
            "caliber" => GetRandomCaliber(),
            _ => GenerateGenericData(columnName, dataType)
        };
    }

    private string GetRandomGameSpecies()
    {
        var species = new[] { "Whitetail Deer", "Elk", "Moose", "Black Bear", "Turkey", "Duck", "Pheasant", "Rabbit" };
        return species[_random.Next(species.Length)];
    }

    private string GetRandomWeapon()
    {
        var weapons = new[] { "Rifle", "Bow", "Crossbow", "Shotgun", "Muzzleloader" };
        return weapons[_random.Next(weapons.Length)];
    }

    private string GetRandomHuntingLocation()
    {
        var locations = new[] { "National Forest", "Private Land", "Wildlife Management Area",
            "State Park", "Hunting Preserve", "Ranch", "Farmland" };
        return locations[_random.Next(locations.Length)];
    }

    private string GetRandomHuntingSeason()
    {
        var seasons = new[] { "Archery Season", "Rifle Season", "Muzzleloader Season", "Youth Season" };
        return seasons[_random.Next(seasons.Length)];
    }

    private decimal GetRandomGameWeight()
    {
        return _random.Next(50, 300); // 50-300 lbs for game animals
    }

    private string GetRandomCaliber()
    {
        var calibers = new[] { ".30-06", ".308", ".270", ".243", ".300 Win Mag", "7mm Rem Mag", ".223" };
        return calibers[_random.Next(calibers.Length)];
    }
    #endregion

    #region Core Domain Data
    private object? GenerateCoreData(string tableName, string columnName, string dataType)
    {
        return columnName switch
        {
            "firstname" => GetRandomFirstName(),
            "lastname" => GetRandomLastName(),
            "email" => GetRandomEmail(),
            "phone" or "phonenumber" => GetRandomPhoneNumber(),
            "address" or "streetaddress" => GetRandomAddress(),
            "city" => GetRandomCity(),
            "state" => GetRandomState(),
            "zipcode" or "postalcode" => GetRandomZipCode(),
            "username" => GetRandomUsername(),
            _ => GenerateGenericData(columnName, dataType)
        };
    }

    private string GetRandomFirstName()
    {
        var names = new[] { 
            // English
            "John", "Jane", "Michael", "Sarah", "David", "Emily", "James", "Jessica",
            // Spanish/Latino
            "José", "María", "Carlos", "Ana", "Luis", "Carmen", "Miguel", "Isabel",
            // French
            "Pierre", "Marie", "Jean", "Sophie", "Paul", "Élise", "André", "Céline",
            // German
            "Hans", "Greta", "Klaus", "Ingrid", "Wolfgang", "Ursula", "Jürgen", "Heidi",
            // Italian
            "Marco", "Francesca", "Giuseppe", "Giulia", "Antonio", "Chiara", "Francesco", "Elena",
            // Japanese
            "Hiroshi", "Yuki", "Takeshi", "Akiko", "Kenji", "Naomi", "Satoshi", "Emi",
            // Chinese
            "Wei", "Li", "Ming", "Xiu", "Chen", "Mei", "Jun", "Ling",
            // Arabic
            "Ahmed", "Fatima", "Omar", "Aisha", "Hassan", "Zara", "Ali", "Nour",
            // Russian
            "Alexei", "Natasha", "Dmitri", "Katya", "Pavel", "Olga", "Sergei", "Anya",
            // Korean
            "Min-jun", "So-young", "Jae-sung", "Hye-jin", "Sang-woo", "Ji-hye", "Dong-hyun", "Soo-jin"
        };
        return names[_random.Next(names.Length)];
    }

    private string GetRandomLastName()
    {
        var names = new[] { 
            // English
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson",
            // Spanish/Latino  
            "García", "Rodríguez", "Martínez", "Hernández", "López", "González", "Pérez", "Sánchez",
            // French
            "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Petit", "Durand", "Leroy",
            // German
            "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker",
            // Italian
            "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci",
            // Japanese
            "Tanaka", "Suzuki", "Takahashi", "Watanabe", "Itō", "Yamamoto", "Nakamura", "Kobayashi",
            // Chinese
            "Wang", "Li", "Zhang", "Liu", "Chen", "Yang", "Huang", "Zhao",
            // Arabic
            "Al-Ahmad", "Al-Hassan", "Al-Ali", "Al-Omar", "Al-Salem", "Al-Rashid", "Al-Mahmoud", "Al-Khalil",
            // Russian  
            "Petrov", "Ivanov", "Sidorov", "Smirnov", "Kuznetsov", "Popov", "Volkov", "Sokolov",
            // Korean
            "Kim", "Lee", "Park", "Choi", "Jung", "Kang", "Cho", "Yoon"
        };
        return names[_random.Next(names.Length)];
    }

    private string GetRandomEmail()
    {
        var domains = new[] { 
            // International email providers
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", 
            // Country-specific domains
            "gmx.de", "web.de", "mail.ru", "yandex.ru", "163.com", "qq.com",
            "naver.com", "hanmail.net", "yahoo.co.jp", "libero.it", "orange.fr",
            "terra.com.br", "uol.com.br", "yahoo.com.mx", "example.com"
        };

        // Generate username that can handle international characters
        var usernames = new[] {
            $"user{_random.Next(1000, 9999)}",
            $"test{_random.Next(100, 999)}",
            $"demo{_random.Next(10, 99)}",
            // Some with unicode-friendly patterns
            $"usuario{_random.Next(100, 999)}", // Spanish
            $"utilisateur{_random.Next(100, 999)}", // French  
            $"benutzer{_random.Next(100, 999)}", // German
            $"utente{_random.Next(100, 999)}" // Italian
        };

        var username = usernames[_random.Next(usernames.Length)];
        var domain = domains[_random.Next(domains.Length)];
        return $"{username}@{domain}";
    }

    private string GetRandomPhoneNumber()
    {
        var phoneFormats = new[] {
            // US Format
            $"({_random.Next(200, 999)}) {_random.Next(200, 999)}-{_random.Next(1000, 9999)}",
            // International formats
            $"+1-{_random.Next(200, 999)}-{_random.Next(200, 999)}-{_random.Next(1000, 9999)}", // US
            $"+44-{_random.Next(10, 99)}-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}", // UK
            $"+33-{_random.Next(1, 9)}-{_random.Next(10, 99)}-{_random.Next(10, 99)}-{_random.Next(10, 99)}-{_random.Next(10, 99)}", // France
            $"+49-{_random.Next(10, 999)}-{_random.Next(1000000, 9999999)}", // Germany
            $"+39-{_random.Next(100, 999)}-{_random.Next(1000000, 9999999)}", // Italy
            $"+34-{_random.Next(600, 999)}-{_random.Next(100000, 999999)}", // Spain
            $"+81-{_random.Next(10, 99)}-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}", // Japan
            $"+86-{_random.Next(100, 199)}-{_random.Next(10000000, 99999999)}", // China
            $"+82-{_random.Next(10, 99)}-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}" // South Korea
        };

        return phoneFormats[_random.Next(phoneFormats.Length)];
    }

    private string GetRandomAddress()
    {
        var streets = new[] { "Main St", "Oak Ave", "Pine Rd", "Elm Dr", "Cedar Ln", "Maple Way" };
        return $"{_random.Next(100, 9999)} {streets[_random.Next(streets.Length)]}";
    }

    private string GetRandomCity()
    {
        var cities = new[] { "Springfield", "Franklin", "Georgetown", "Clinton", "Madison", "Washington",
            "Arlington", "Centerville", "Lebanon", "Kingston", "Marion", "Oxford" };
        return cities[_random.Next(cities.Length)];
    }

    private string GetRandomState()
    {
        var states = new[] { "CA", "TX", "FL", "NY", "PA", "IL", "OH", "GA", "NC", "MI", "NJ", "VA" };
        return states[_random.Next(states.Length)];
    }

    private string GetRandomZipCode()
    {
        return _random.Next(10000, 99999).ToString();
    }

    private string GetRandomUsername()
    {
        var adjectives = new[] { "Cool", "Smart", "Fast", "Bright", "Quick", "Bold" };
        var nouns = new[] { "Hunter", "Fisher", "Photographer", "Explorer", "Adventurer", "Outdoorsman" };
        return $"{adjectives[_random.Next(adjectives.Length)]}{nouns[_random.Next(nouns.Length)]}{_random.Next(10, 99)}";
    }
    #endregion

    #region Generic Data Generation
    private object? GenerateGenericData(string columnName, string dataType)
    {
        return dataType.ToLower() switch
        {
            "nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext" => GenerateGenericString(columnName),
            "int" or "smallint" or "tinyint" => GenerateGenericInteger(columnName),
            "bigint" => GenerateGenericLong(columnName),
            "decimal" or "numeric" or "money" or "smallmoney" => GenerateGenericDecimal(columnName),
            "float" or "real" => GenerateGenericFloat(columnName),
            "bit" => GenerateGenericBoolean(columnName),
            "datetime" or "datetime2" or "smalldatetime" => GenerateGenericDateTime(columnName),
            "date" => GenerateGenericDate(columnName),
            "time" => GenerateGenericTime(columnName),
            "uniqueidentifier" => Guid.NewGuid(),
            "varbinary" or "binary" => GenerateGenericBinary(),
            _ => $"Generated_{columnName}_{_random.Next(1000)}"
        };
    }

    private string GenerateGenericString(string columnName)
    {
        if (columnName.Contains("name"))
        {
            // Generate names that might contain Unicode characters
            var testNames = new[] {
                $"TestName{_random.Next(1000)}",
                $"Tëst Nâmé {_random.Next(1000)}", // Accented characters
                $"テスト名{_random.Next(100)}", // Japanese
                $"测试名{_random.Next(100)}", // Chinese
                $"тест{_random.Next(100)}", // Cyrillic
                $"اختبار{_random.Next(100)}", // Arabic
                $"Prüfung{_random.Next(100)}" // German
            };
            return testNames[_random.Next(testNames.Length)];
        }

        if (columnName.Contains("description"))
        {
            var descriptions = new[] {
                $"Test description for {columnName}",
                $"Descripción de prueba para {columnName}", // Spanish
                $"Description de test pour {columnName}", // French
                $"Testbeschreibung für {columnName}", // German
                $"Descrizione di prova per {columnName}", // Italian
                $"テストの説明 {columnName}", // Japanese
                $"Описание теста {columnName}" // Russian
            };
            return descriptions[_random.Next(descriptions.Length)];
        }

        if (columnName.Contains("code"))
            return $"CODE{_random.Next(100, 999)}";

        // Generic string with potential Unicode
        var genericStrings = new[] {
            $"Test{columnName}{_random.Next(100)}",
            $"Tést{columnName}{_random.Next(100)}",
            $"测试{_random.Next(100)}",
            $"тест{_random.Next(100)}",
            $"テスト{_random.Next(100)}"
        };

        return genericStrings[_random.Next(genericStrings.Length)];
    }

    private int GenerateGenericInteger(string columnName)
    {
        if (columnName.Contains("id"))
            return _random.Next(1, 1000);
        if (columnName.Contains("count") || columnName.Contains("quantity"))
            return _random.Next(1, 100);
        if (columnName.Contains("year"))
            return _random.Next(2020, 2025);

        return _random.Next(1, 1000);
    }

    private long GenerateGenericLong(string columnName)
    {
        return _random.Next(1, 100000);
    }

    private decimal GenerateGenericDecimal(string columnName)
    {
        if (columnName.Contains("price") || columnName.Contains("cost"))
            return (decimal)(_random.NextDouble() * 1000 + 10);
        if (columnName.Contains("rate") || columnName.Contains("percent"))
            return (decimal)(_random.NextDouble() * 100);

        return (decimal)(_random.NextDouble() * 1000);
    }

    private double GenerateGenericFloat(string columnName)
    {
        return _random.NextDouble() * 1000;
    }

    private bool GenerateGenericBoolean(string columnName)
    {
        if (columnName.Contains("active") || columnName.Contains("enabled"))
            return _random.NextDouble() > 0.2; // 80% chance of being active/enabled

        return _random.NextDouble() > 0.5;
    }

    private DateTime GenerateGenericDateTime(string columnName)
    {
        var start = new DateTime(2020, 1, 1);
        var range = (DateTime.Today - start).Days;
        return start.AddDays(_random.Next(range));
    }

    private DateTime GenerateGenericDate(string columnName)
    {
        return GenerateGenericDateTime(columnName).Date;
    }

    private TimeSpan GenerateGenericTime(string columnName)
    {
        return new TimeSpan(_random.Next(24), _random.Next(60), _random.Next(60));
    }

    private byte[] GenerateGenericBinary()
    {
        var length = _random.Next(10, 100);
        var bytes = new byte[length];
        _random.NextBytes(bytes);
        return bytes;
    }
    #endregion

    /// <summary>
    /// Gets appropriate volume of data based on options
    /// </summary>
    public int GetDataVolume(TestDataOptions options, string tableName)
    {
        var baseVolume = options.Volume.ToLower() switch
        {
            "small" => 100,
            "medium" => 500,
            "large" => 2000,
            _ => 500
        };

        // Adjust based on table type
        if (tableName.ToLower().Contains("lookup") || tableName.ToLower().Contains("reference"))
        {
            return Math.Min(baseVolume / 10, 50); // Reference tables need fewer records
        }

        return baseVolume;
    }
}