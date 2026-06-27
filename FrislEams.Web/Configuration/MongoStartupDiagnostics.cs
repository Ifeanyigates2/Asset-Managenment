namespace FrislEams.Web.Configuration;

public static class MongoStartupDiagnostics
{
    public static void LogConfiguration(MongoDbOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            Console.WriteLine("FRISL EAMS startup: MongoDb ConnectionString is NOT configured.");
            Console.WriteLine("  Set MongoDb__ConnectionString in the environment (e.g. Render dashboard).");
            return;
        }

        Console.WriteLine(
            $"FRISL EAMS startup: MongoDb configured " +
            $"(database={options.DatabaseName}, users={options.UsersCollectionName}, " +
            $"connection={MaskConnectionString(options.ConnectionString)})");
    }

    public static string MaskConnectionString(string connectionString)
    {
        var at = connectionString.IndexOf('@');
        if (at <= 0)
        {
            return "(configured)";
        }

        var schemeEnd = connectionString.IndexOf("://", StringComparison.Ordinal) + 3;
        var colon = connectionString.IndexOf(':', schemeEnd);
        if (colon > 0 && colon < at)
        {
            return connectionString[..(colon + 1)] + "***" + connectionString[at..];
        }

        return "(configured)";
    }
}
