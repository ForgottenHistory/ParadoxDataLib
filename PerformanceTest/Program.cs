using System;
using System.Threading.Tasks;
using ParadoxDataLib.PerformanceTest;

namespace ParadoxDataLib.PerformanceTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PARADOX DATA LIB PERFORMANCE TESTS ===");
            Console.WriteLine();

            // Check for command line arguments
            var runMassiveTest = args.Length > 0 &&
                (args[0].Equals("--massive", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("-m", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("massive", StringComparison.OrdinalIgnoreCase));

            var runDiagnostic = args.Length > 0 &&
                (args[0].Equals("--diagnose", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("-d", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("diagnose", StringComparison.OrdinalIgnoreCase));

            try
            {
                if (runDiagnostic)
                {
                    Console.WriteLine("Running bitmap diagnostic tests...");
                    Console.WriteLine();

                    DiagnoseBitmapIssue.DiagnoseIssues();
                }
                else if (runMassiveTest)
                {
                    Console.WriteLine("Running MASSIVE test on all EU IV files (txt, csv, bitmap)...");
                    Console.WriteLine();

                    var massiveTest = new MassiveParadoxTest();
                    await massiveTest.RunCompleteTest();
                }
                else
                {
                    Console.WriteLine("Running standard performance tests...");
                    Console.WriteLine("Use '--massive' argument to run comprehensive test on all 7,000+ files");
                    Console.WriteLine("Use '--diagnose' argument to diagnose bitmap parsing issues");
                    Console.WriteLine();

                    var performanceTest = new SimplePerformanceTest();
                    await performanceTest.RunAllTests();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Performance test failed: {ex.Message}");
                Console.WriteLine("Ensure EU IV Files directory exists in the current path or parent directories.");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run                    - Run standard performance tests");
                Console.WriteLine("  dotnet run -- --massive       - Run massive test on all files");
                Console.WriteLine("  dotnet run -- --diagnose      - Diagnose bitmap parsing issues");
            }

            Console.WriteLine("\nPerformance test completed.");
        }
    }
}