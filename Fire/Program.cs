using CommandLine;
using Fire.Core;

namespace Fire.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                options => new FireEngine(Environment.CurrentDirectory, options).Run(),
                _ => 1);
    }
}
