using System;
using CommandLine;
using CommandLine.Text;

namespace woanware
{
    /// <summary>
    /// Internal class used for the command line parsing
    /// </summary>
    internal class Options : CommandLineOptionsBase
    {
        [Option("f", "file", Required = true, HelpText = "The SYSTEM registry file/hive to be processed")]
        public string File { get; set; }

        [Option("d", "delimiter", Required = false, DefaultValue = ",", HelpText = "The delimiter used for the export. Defaults to \",\"")]
        public string Delimiter { get; set; }

        [Option("s", "sort", Required = false, HelpText = "Sort column. Valid options are: modified,updated,path,filesize,executed. Defaults to sorting by updated")]
        public string Sort { get; set; }

        [Option("o", "output", Required = false, DefaultValue = "", HelpText = "The output file path, including the file name")]
        public string Output { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Copyright = new CopyrightInfo("woanware", 2012),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };

            //help.AddPreOptionsLine(Environment.NewLine + "This is proprietary software written by Context Information Security and is not for redistribution.");
            this.HandleParsingErrorsInHelp(help);

            help.AddPreOptionsLine("Usage: shimcacheparser -f \"C:\\SYSTEM\" -d \"\\t\" -s \"path\" -o \"C:\\output.csv\"");
            help.AddOptions(this);

            return help;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="help"></param>
        private void HandleParsingErrorsInHelp(HelpText help)
        {
            if (this.LastPostParsingState.Errors.Count > 0)
            {
                var errors = help.RenderParsingErrorsText(this, 2); // indent with two spaces
                if (!string.IsNullOrEmpty(errors))
                {
                    help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                    help.AddPreOptionsLine(errors);
                }
            }
        }
    }
}
