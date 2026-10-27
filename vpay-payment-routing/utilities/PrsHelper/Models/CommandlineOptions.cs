using CommandLine;

namespace PrsHelper.Models
{
    /// <summary>
    /// Commands passed on command line and parsed.
    /// </summary>
    public class CommandlineOptions
    {

        [Value(0,
        MetaName = "input file",
        Required = true,
        HelpText = "File used to load batch. Hint: (.prs-bs.json) ")]
        public string BatchSummaryFilename { get; set; } = default!;


        [Option('o', "options", Required = false,
            HelpText = "Use this options file for all batches.")]
        public string OptionsFilename { get; set; } = default!;

        [Option('s',
            "save-batch-summary",

            Default = false,
            Required = false,
            HelpText = "Use this options file for all batches.")]
        public bool SaveBatchSummarySettings { get; set; } = default!;

        [Option('p',
    "display-pause",

        Default = false,
        Required = false,
        HelpText = "Use this option to display a press any key message and wait at end of program.")]
        public bool PauseAtEnd { get; set; } = default!;



    }
}
