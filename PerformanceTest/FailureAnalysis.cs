using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParadoxDataLib.PerformanceTest
{
    public class FailureAnalysis
    {
        public static async Task AnalyzeFailures()
        {
            Console.WriteLine("=== FAILURE ANALYSIS ===");
            Console.WriteLine("Running massive test and capturing detailed failure information...");
            Console.WriteLine();

            var massiveTest = new MassiveParadoxTest();
            await massiveTest.RunCompleteTest();

            // The statistics are already printed by RunCompleteTest,
            // but we could add additional analysis here if needed
            Console.WriteLine("Analysis complete. Check the output above for detailed error information.");
        }
    }
}