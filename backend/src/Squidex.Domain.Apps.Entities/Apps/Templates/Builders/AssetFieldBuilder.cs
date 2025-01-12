﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Entities.Schemas.Commands;

namespace Squidex.Domain.Apps.Entities.Apps.Templates.Builders
{
    public class AssetFieldBuilder : FieldBuilder
    {
        public AssetFieldBuilder(UpsertSchemaField field, CreateSchema schema)
            : base(field, schema)
        {
        }

        public AssetFieldBuilder MustBeImage()
        {
            Properties<AssetsFieldProperties>(p => p with
            {
                MustBeImage = true
            });

            return this;
        }

        public AssetFieldBuilder RequireSingle()
        {
            Properties<AssetsFieldProperties>(p => p with
            {
                MaxItems = 1
            });

            return this;
        }
    }
}
