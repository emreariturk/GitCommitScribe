using GitCommitScribe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommitScribe.Parsing
{
    //.NET 8'in derleme anı Regex üretecini ([GeneratedRegex]) kullanarak sıfır maliyetli parsing yapıyoruz

    /// <summary>
    /// Derleme anında üretilen yüksek performanslı Regex ([GeneratedRegex]) kurallarıyla ham commit mesajlarını analiz eder; tip, kapsam ve BREAKING CHANGE bildirimlerini ayıklar.
    /// </summary>
    public sealed partial class CommitParser
    {
        [GeneratedRegex(@"^(?<type>[a-zA-Z]+)(?:\((?<scope>[^)]+)\))?(?<breaking>!)?:\s*(?<desc>.+)$", RegexOptions.Compiled)]
        private static partial Regex ConventionalHeaderRegex();

        [GeneratedRegex(@"BREAKING[ -]CHANGE:\s*(?<note>.+)", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex BreakingBodyRegex();

        public ParsedCommit Parse(RawCommit raw)
        {
            var match = ConventionalHeaderRegex().Match(raw.Subject);
            var breakingBodyMatch = BreakingBodyRegex().Match(raw.Body);

            string? breakingNote = breakingBodyMatch.Success
                ? breakingBodyMatch.Groups["note"].Value.Trim()
                : null;

            if (!match.Success)
            {
                var isUnstructuredBreaking = breakingNote is not null;
                return new ParsedCommit(
                    Hash: raw.Hash,
                    ShortHash: raw.ShortHash,
                    Category: isUnstructuredBreaking ? CommitCategory.BreakingChange : CommitCategory.Other,
                    Scope: null,
                    Description: raw.Subject,
                    IsBreaking: isUnstructuredBreaking,
                    BreakingNote: breakingNote,
                    Author: raw.Author,
                    Date: raw.Date);
            }

            var type = match.Groups["type"].Value.ToLowerInvariant();
            var scope = match.Groups["scope"].Success ? match.Groups["scope"].Value.Trim() : null;
            var hasBang = match.Groups["breaking"].Success;
            var description = match.Groups["desc"].Value.Trim();

            var isBreaking = hasBang || breakingNote is not null;
            var category = isBreaking
                ? CommitCategory.BreakingChange
                : MapCategory(type);

            return new ParsedCommit(
                Hash: raw.Hash,
                ShortHash: raw.ShortHash,
                Category: category,
                Scope: scope,
                Description: description,
                IsBreaking: isBreaking,
                BreakingNote: breakingNote ?? (hasBang ? description : null),
                Author: raw.Author,
                Date: raw.Date);
        }

        private static CommitCategory MapCategory(string type) => type switch
        {
            "feat" or "feature" => CommitCategory.Feature,
            "fix" or "bugfix" or "hotfix" => CommitCategory.BugFix,
            "perf" => CommitCategory.Performance,
            "refactor" => CommitCategory.Refactor,
            "docs" => CommitCategory.Documentation,
            _ => CommitCategory.Other
        };
    }
}
