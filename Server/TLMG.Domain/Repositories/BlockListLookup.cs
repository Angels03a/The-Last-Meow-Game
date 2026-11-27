using System;
using System.Collections.Generic;

namespace TLMG.Domain.Repositories
{
    public sealed class BlockListLookup
    {
        public bool WasLookupSuccessful { get; set; }

        public IReadOnlyList<BlockRelationship> Relationships { get; set; } =
            Array.Empty<BlockRelationship>();
    }
}
