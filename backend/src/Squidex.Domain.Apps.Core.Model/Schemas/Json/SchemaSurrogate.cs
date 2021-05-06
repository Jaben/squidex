﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Domain.Apps.Core.Schemas.Json
{
    public sealed class SchemaSurrogate : ISurrogate<Schema>
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public bool IsSingleton { get; set; }

        public bool IsPublished { get; set; }

        public SchemaType Type { get; set; }

        public SchemaProperties Properties { get; set; }

        public SchemaScripts? Scripts { get; set; }

        public FieldNames? FieldsInLists { get; set; }

        public FieldNames? FieldsInReferences { get; set; }

        public FieldRules? FieldRules { get; set; }

        public FieldSurrogate[] Fields { get; set; }

        public Dictionary<string, string>? PreviewUrls { get; set; }

        public void FromSource(Schema source)
        {
            SimpleMapper.Map(source, this);

            Fields =
                source.Fields.Select(x =>
                    new FieldSurrogate
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Children = CreateChildren(x),
                        IsHidden = x.IsHidden,
                        IsLocked = x.IsLocked,
                        IsDisabled = x.IsDisabled,
                        Partitioning = x.Partitioning.Key,
                        Properties = x.RawProperties
                    }).ToArray();

            PreviewUrls = source.PreviewUrls.ToDictionary(x => x.Key, x => x.Value);
        }

        private static FieldSurrogate[]? CreateChildren(IField field)
        {
            if (field is ArrayField arrayField)
            {
                return arrayField.Fields.Select(x =>
                    new FieldSurrogate
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IsHidden = x.IsHidden,
                        IsLocked = x.IsLocked,
                        IsDisabled = x.IsDisabled,
                        Properties = x.RawProperties
                    }).ToArray();
            }

            return null;
        }

        public Schema ToSource()
        {
            var fields = Fields?.Select(f => f.ToField()).ToArray() ?? Array.Empty<RootField>();

            var type = Type;

            if (IsSingleton)
            {
                type = SchemaType.Singleton;
            }

            var schema = new Schema(Name, fields, Properties, IsPublished, type);

            if (!string.IsNullOrWhiteSpace(Category))
            {
                schema = schema.ChangeCategory(Category);
            }

            if (Scripts != null)
            {
                schema = schema.SetScripts(Scripts);
            }

            if (FieldsInLists?.Count > 0)
            {
                schema = schema.SetFieldsInLists(FieldsInLists);
            }

            if (FieldsInReferences?.Count > 0)
            {
                schema = schema.SetFieldsInReferences(FieldsInReferences);
            }

            if (FieldRules?.Count > 0)
            {
                schema = schema.SetFieldRules(FieldRules);
            }

            if (PreviewUrls?.Count > 0)
            {
                schema = schema.SetPreviewUrls(PreviewUrls);
            }

            return schema;
        }
    }
}
