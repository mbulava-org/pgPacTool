using Xunit;
using Npgquery;

namespace NpgqueryExtended.Tests;

internal static class PostgreSqlVersionTestData
{
    public static TheoryData<PostgreSqlVersion> SupportedVersions
    {
        get
        {
            var data = new TheoryData<PostgreSqlVersion>();
            foreach (var version in PostgreSqlVersionExtensions.GetSupportedVersions())
            {
                data.Add(version);
            }

            return data;
        }
    }

    public static TheoryData<PostgreSqlVersion> AvailableVersions
    {
        get
        {
            var data = new TheoryData<PostgreSqlVersion>();
            foreach (var version in PostgreSqlVersionExtensions.GetSupportedVersions())
            {
                if (NativeLibraryLoader.IsVersionAvailable(version))
                {
                    data.Add(version);
                }
            }

            return data;
        }
    }

    public static IReadOnlyList<PostgreSqlVersion> SupportedVersionList => PostgreSqlVersionExtensions.GetSupportedVersions();

    public static IReadOnlyList<PostgreSqlVersion> AvailableVersionList =>
        PostgreSqlVersionExtensions.GetSupportedVersions()
            .Where(NativeLibraryLoader.IsVersionAvailable)
            .ToArray();
}
