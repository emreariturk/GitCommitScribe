using GitCommitScribe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommitScribe.Git
{
    /// <summary>
    /// Git komut satırı işlemlerini soyutlayan arayüzdür; ileride birim test (unit test) yazarken sahte (mock) Git verisi verebilmeyi sağlar.
    /// </summary>
    public interface IGitRunner
    {
        Task<bool> IsGitRepositoryAsync(string workingDirectory, CancellationToken ct = default);
        Task<string?> GetLatestTagAsync(string workingDirectory, CancellationToken ct = default);
        Task<string?> GetRepositoryWebUrlAsync(string workingDirectory, CancellationToken ct = default);
        Task<IReadOnlyList<RawCommit>> GetCommitsAsync(string workingDirectory, string? fromRef, string toRef, CancellationToken ct = default);
    }
}
