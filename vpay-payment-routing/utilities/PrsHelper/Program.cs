// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using PrsHelper.Models;

 

var o = Parser.Default.ParseArguments<CommandlineOptions>(args);
    o.WithParsed(RunOptions)
    .WithNotParsed(errs => DisplayHelp(o, errs));


if (o.Value.PauseAtEnd)
{
    Console.WriteLine("Press any key to continue");
    Console.ReadKey();
}
 


static void RunOptions(CommandlineOptions opts)
{
    var options = Options.LoadFromFile(opts.OptionsFilename);
    var  bs = BatchSummary.LoadFromFile(options, opts.BatchSummaryFilename);
    bs.Set_Options(options);
    CurlHelper.GenerateFiles(bs);
    if (opts.SaveBatchSummarySettings)
    {
        bs.SaveBatchSummary();
    }
 
}

static void DisplayHelp<T>(ParserResult<T> o, IEnumerable<Error> errs)
{
    Console.WriteLine(HelpText.AutoBuild(o, _ => _, _ => _));
    foreach (var error in errs)
    {
        Console.WriteLine(error);
    }
    //handle errors
}

