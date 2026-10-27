// See https://aka.ms/new-console-template for more information
internal class CurlHelper
{
    internal static void GenerateFiles(BatchSummary batchSummary)
    {
        if (batchSummary.Options.Verbose) {
            Console.WriteLine("Generating files based on options");
        }


        if (batchSummary.Options.Verbose)
        {
            var curls = batchSummary.GenerateBatch();
            foreach (var curl in curls)
            {
                Console.WriteLine(curl);
            }
        } else
        {
            var filename = batchSummary.SaveScript();
            Console.WriteLine($"Shell script saved to {filename}");

        }

        
    }




}
