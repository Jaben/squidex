﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.ObjectModel;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Areas.Api.Controllers.Schemas.Models.Fields
{
    public sealed class ComponentsFieldPropertiesDto : FieldPropertiesDto
    {
        /// <summary>
        /// The minimum allowed items for the field value.
        /// </summary>
        public int? MinItems { get; set; }

        /// <summary>
        /// The maximum allowed items for the field value.
        /// </summary>
        public int? MaxItems { get; set; }

        /// <summary>
        /// True, if the field accepts multiple values.
        /// </summary>
        public bool Multiple { get; set; }

        /// <summary>
        /// The id of the embedded schemas.
        /// </summary>
        public ReadOnlyCollection<DomainId>? SchemaIds { get; set; }

        public override FieldProperties ToProperties()
        {
            var result = SimpleMapper.Map(this, new ComponentsFieldProperties());

            return result;
        }
    }
}
