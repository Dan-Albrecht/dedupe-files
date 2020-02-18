namespace DedupeFiles
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.Threading.Tasks;

    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Finds and deals with duplcate files")
            {
                new Option(
                    new[]{ "-p", "--path" },
                    "Path to recurrsively scan")
                {
                    Argument = new Argument<string>(getDefaultValue: () => Environment.CurrentDirectory)
                }
            };

            rootCommand.Handler = CommandHandler.Create<DirectoryInfo>((directoryInfo) =>
            {
                try
                {
                    Searcher searcher = new Searcher(directoryInfo);
                    searcher.Search();

                    return 0;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();

                    return -1;
                }
            });

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
        }
    }
}
