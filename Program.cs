// See https://aka.ms/new-console-template for more information
using CommitScribe.Commands;
using Spectre.Console.Cli;
using System.Reflection;


var app = new CommandApp<GenerateCommand>();

var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion?.Split('+')[0] ?? "1.0.0";

app.Configure(config =>
{
    config.SetApplicationName("scribe");
    config.SetApplicationVersion(version);
    config.AddExample(["--version-title", "v1.0.0"]);
    config.AddExample(["--from", "v1.0.0", "--to", "HEAD", "--dry-run"]);
});

return await app.RunAsync(args);