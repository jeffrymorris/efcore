﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class CustomRuntimeJsonIdDefinitionFactory : RuntimeJsonIdDefinitionFactory
{
    public override RuntimeJsonIdDefinition Create(RuntimeEntityType entityType, JsonIdDefinition jsonIdDefinition)
        => new CustomRuntimeJsonIdDefinition(entityType, jsonIdDefinition);
}
