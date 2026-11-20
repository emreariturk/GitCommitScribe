
using GitCommitScribe.Git;
using GitCommitScribe.Parsing;
using GitCommitScribe.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CommitScribe.Commands;

public sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    //Terminalden gelen parametreleri (--version-title, --from, --to, --output, --include-other, --dry-run) karşılar ve Git Okuma → Ayrıştırma → Markdown Üretme akışını yönetir.

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-v|--version-title <VERSION>")]
        [Description("Sürüm başlığı (Örn: v1.2.0). Belirtilmezse 'Unreleased' kullanılır.")]
        public string? VersionTitle { get; init; }

        [CommandOption("-f|--from <TAG_OR_HASH>")]
        [Description("Başlangıç Git tag veya commit hash'i. Belirtilmezse son tag otomatik alınır.")]
        public string? From { get; init; }

        [CommandOption("-t|--to <TAG_OR_HASH>")]
        [Description("Bitiş Git referansı (Varsayılan: HEAD).")]
        public string To { get; init; } = "HEAD";

        [CommandOption("-o|--output <FILE>")]
        [Description("Çıktı dosya yolu (Varsayılan: CHANGELOG.md).")]
        public string Output { get; init; } = "CHANGELOG.md";

        [CommandOption("--include-other")]
        [Description("chore, ci, build gibi standart dışı commit'leri de listeye dahil eder.")]
        public bool IncludeOther { get; init; }

        [CommandOption("--dry-run")]
        [Description("Dosyaya yazmadan üretilen Markdown çıktısını terminale basar.")]
        public bool DryRun { get; init; }
    }

    public override async Task<int> ExecuteAsync(
    CommandContext context,
    Settings settings,
    CancellationToken cancellationToken)
    {
        var workingDir = Directory.GetCurrentDirectory();
        var gitRunner = new GitCliRunner();

        if (!await gitRunner.IsGitRepositoryAsync(workingDir, cancellationToken))
        {
            AnsiConsole.MarkupLine("[red]Hata:[/] Bu klasör geçerli bir Git deposu değil.");
            return 1;
        }

        var fromRef = settings.From ?? await gitRunner.GetLatestTagAsync(workingDir, cancellationToken);
        var repoUrl = await gitRunner.GetRepositoryWebUrlAsync(workingDir, cancellationToken);

        var rawCommits = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Git geçmişi taranıyor...", async _ =>
                await gitRunner.GetCommitsAsync(workingDir, fromRef, settings.To, cancellationToken));

        if (rawCommits.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Bilgi:[/] Belirtilen aralıkta yeni commit bulunamadı.");
            return 0;
        }

        var parser = new CommitParser();
        var parsedCommits = rawCommits.Select(parser.Parse).ToList();

        var versionTitle = string.IsNullOrWhiteSpace(settings.VersionTitle)
            ? "Unreleased"
            : settings.VersionTitle.Trim();

        var renderer = new MarkdownRenderer();
        var section = renderer.RenderVersionSection(
            versionTitle,
            DateOnly.FromDateTime(DateTime.Now),
            parsedCommits,
            repoUrl,
            settings.IncludeOther);

        if (settings.DryRun)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(section);
            return 0;
        }

        var outputPath = Path.GetFullPath(Path.Combine(workingDir, settings.Output));
        var existingContent = File.Exists(outputPath)
            ? await File.ReadAllTextAsync(outputPath, cancellationToken)
            : null;

        var finalContent = renderer.MergeWithExistingFile(existingContent, section);
        await File.WriteAllTextAsync(outputPath, finalContent, cancellationToken);

        AnsiConsole.MarkupLine($"[green]✓[/] [bold]{parsedCommits.Count}[/] commit işlendi ve [cyan]{settings.Output}[/] güncellendi.");
        return 0;
    }
}