﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Tasks;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Migrations;

namespace Migrations.Migrations
{
    public sealed class RebuildAssets : IMigration
    {
        private readonly Rebuilder rebuilder;

        public RebuildAssets(Rebuilder rebuilder)
        {
            this.rebuilder = rebuilder;
        }

        public Task UpdateAsync()
        {
            return rebuilder.RebuildAssetsAsync();
        }
    }
}
