// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Relational.Metadata;

namespace Microsoft.Data.Entity.SqlServer.Metadata
{
    public interface ISqlServerPropertyExtensions : IRelationalPropertyExtensions
    {
        SqlServerValueGenerationStrategy? ValueGenerationStrategy { get; }
        string ComputedExpression { get; }
        string SequenceName { get; }
        string SequenceSchema { get; }
        Sequence TryGetSequence();
    }
}
