using BenchmarkDotNet.Running;
using pgPacTool.Benchmarks;

// Run all benchmarks in Release mode.
// Usage: dotnet run -c Release
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();
