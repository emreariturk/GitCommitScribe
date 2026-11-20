using System.Diagnostics;
using System.Text;
using GitCommitScribe.Models;

namespace GitCommitScribe.Git
{
    /// <summary>
    /// İşletim sistemindeki git CLI aracını arka plan süreci (Process) olarak çalıştırır; repo kontrolü yapar, son etiketi (tag) bulur, remote URL'ini HTTPS formatına çevirir ve ASCII ayırıcılar (\x1f, \x1e) kullanarak commit geçmişini okur.
    /// </summary>
    public sealed class GitCliRunner : IGitRunner
    {
        private const char FieldSeparator = '\x1f';
        private const char RecordSeparator = '\x1e';

        public async Task<bool> IsGitRepositoryAsync(string workingDirectory, CancellationToken ct = default)
        {
            var (exitCode, output, _) = await RunGitAsync(workingDirectory, "rev-parse --is-inside-work-tree", ct);
            return exitCode == 0 && output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string?> GetLatestTagAsync(string workingDirectory, CancellationToken ct = default)
        {
            var (exitCode, output, _) = await RunGitAsync(workingDirectory, "describe --tags --abbrev=0", ct);
            return exitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output.Trim() : null;
        }

        public async Task<string?> GetRepositoryWebUrlAsync(string workingDirectory, CancellationToken ct = default)
        {
            var (exitCode, output, _) = await RunGitAsync(workingDirectory, "config --get remote.origin.url", ct);
            if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
                return null;

            var url = output.Trim();

            // git@github.com:user/repo.git -> https://github.com/user/repo
            if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Replace(':', '/').Replace("git@", "https://", StringComparison.OrdinalIgnoreCase);
            }

            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                url = url[..^4];
            }

            return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : null;
        }

        public async Task<IReadOnlyList<RawCommit>> GetCommitsAsync(
            string workingDirectory,
            string? fromRef,
            string toRef,
            CancellationToken ct = default)
        {
            var range = string.IsNullOrWhiteSpace(fromRef) ? toRef : $"{fromRef}..{toRef}";
            var format = $"%H%x1f%h%x1f%s%x1f%b%x1f%an%x1f%aI%x1e";
            var args = $"log {range} --no-merges --pretty=format:\"{format}\"";

            var (exitCode, output, error) = await RunGitAsync(workingDirectory, args, ct);
            if (exitCode != 0)
                throw new InvalidOperationException($"Git log okunamadı: {error}");

            var commits = new List<RawCommit>();
            var records = output.Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries);

            foreach (var record in records)
            {
                var trimmed = record.TrimStart('\r', '\n');
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var fields = trimmed.Split(FieldSeparator);
                if (fields.Length < 6)
                    continue;

                var date = DateTimeOffset.TryParse(fields[5].Trim(), out var parsedDate)
                    ? parsedDate
                    : DateTimeOffset.UtcNow;

                commits.Add(new RawCommit(
                    Hash: fields[0].Trim(),
                    ShortHash: fields[1].Trim(),
                    Subject: fields[2].Trim(),
                    Body: fields[3].Trim(),
                    Author: fields[4].Trim(),
                    Date: date));
            }

            return commits;
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunGitAsync(
            string workingDirectory,
            string arguments,
            CancellationToken ct)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stdErrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            return (process.ExitCode, await stdOutTask, await stdErrTask);
        }
    }
}
