// See https://aka.ms/new-console-template for more information
internal class CurlHelper
{
    internal static void GenerateFiles(BatchSummary batchSummary)
    {
        if (batchSummary.Options.Verbose) {
            Console.WriteLine("Generating files based on options");
        }


        var curls = batchSummary.GenerateBatch();

        if (batchSummary.Options.Verbose)
        {
            
            foreach (var curl in curls)
            {
                Console.WriteLine(curl);
            }
        } 
        
    }




}
