using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using System.CommandLine;

namespace postgresPacTools
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            await Task.Delay(5000);
            return 0;
        }
    }
}
