using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommitScribe.Models
{
    public enum CommitCategory
    {
        BreakingChange,
        Feature,
        BugFix,
        Performance,
        Refactor,
        Documentation,
        Other
    }
}
