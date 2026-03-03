#!/usr/bin/env dotnet-script
#r "nuget: Npgquery, 3.2.0"

using Npgquery;
using System.Text.Json;

// Parse a real DROP TRIGGER statement
var sql = "DROP TRIGGER IF EXISTS audit_trigger ON public.users;";
using var parser = new Parser();
var result = parser.Parse(sql);

if (result.IsSuccess)
{
    Console.WriteLine("Parsed JSON:");
    var options = new JsonSerializerOptions { WriteIndented = true };
    Console.WriteLine(JsonSerializer.Serialize(result.ParseTree, options));
}
else
{
    Console.WriteLine($"Parse failed: {result.ErrorMessage}");
}
