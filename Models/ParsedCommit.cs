using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Conventional Commits kurallarına göre ayrıştırılmış, kategorisi (Feature, BugFix, BreakingChange vb.), kapsamı (scope) ve breaking change notu belirlenmiş commit modelini ve kategori enum'ını barındırır.
/// Neden record : class yerine record kullanmamızın sebebi, commit nesnelerinin immutable (değiştirilemez) olmasını istememizdir. Commitler bir kez oluşturulduktan sonra değiştirilemezler; bu nedenle record kullanmak mantıklıdır.
/// </summary>
namespace GitCommitScribe.Models
{

    public sealed record ParsedCommit(
    string Hash,
    string ShortHash,
    CommitCategory Category,
    string? Scope,
    string Description,
    bool IsBreaking,
    string? BreakingNote,
    string Author,
    DateTimeOffset Date);

}
