﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NSwag.Annotations;
using Squidex.Areas.Api.Controllers.Schemas.Models;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Domain.Apps.Entities;
using Squidex.Domain.Apps.Entities.Apps;
using Squidex.Domain.Apps.Entities.Schemas;
using Squidex.Domain.Apps.Entities.Schemas.Commands;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Shared;
using Squidex.Web;

namespace Squidex.Areas.Api.Controllers.Schemas
{
    /// <summary>
    /// Manages and retrieves information about schemas.
    /// </summary>
    [ApiExplorerSettings(GroupName = nameof(Schemas))]
    public sealed class SchemasController : ApiController
    {
        private readonly IAppProvider appProvider;

        public SchemasController(ICommandBus commandBus, IAppProvider appProvider)
            : base(commandBus)
        {
            this.appProvider = appProvider;
        }

        /// <summary>
        /// Get schemas.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <returns>
        /// 200 => Schemas returned.
        /// 404 => App not found.
        /// </returns>
        [HttpGet]
        [Route("apps/{app}/schemas/")]
        [ProducesResponseType(typeof(SchemasDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasRead)]
        [ApiCosts(0)]
        public async Task<IActionResult> GetSchemas(string app)
        {
            var schemas = await appProvider.GetSchemasAsync(AppId);

            var response = Deferred.Response(() =>
            {
                return SchemasDto.FromSchemas(schemas, Resources);
            });

            Response.Headers[HeaderNames.ETag] = schemas.ToEtag();

            return Ok(response);
        }

        /// <summary>
        /// Get a schema by name.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema to retrieve.</param>
        /// <returns>
        /// 200 => Schema found.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpGet]
        [Route("apps/{app}/schemas/{name}/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasRead)]
        [ApiCosts(0)]
        public async Task<IActionResult> GetSchema(string app, string name)
        {
            var schema = await GetSchemaAsync(name);

            if (schema == null)
            {
                return NotFound();
            }

            var response = Deferred.Response(() =>
            {
                return SchemaDto.FromSchema(schema, Resources);
            });

            Response.Headers[HeaderNames.ETag] = schema.ToEtag();

            return Ok(response);
        }

        /// <summary>
        /// Create a new schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="request">The schema object that needs to be added to the app.</param>
        /// <returns>
        /// 201 => Schema created.
        /// 400 => Schema request not valid.
        /// 409 => Schema name already in use.
        /// </returns>
        [HttpPost]
        [Route("apps/{app}/schemas/")]
        [ProducesResponseType(typeof(SchemaDto), 201)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasCreate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PostSchema(string app, [FromBody] CreateSchemaDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return CreatedAtAction(nameof(GetSchema), new { app, name = request.Name }, response);
        }

        /// <summary>
        /// Update a schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The schema object that needs to updated.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutSchema(string app, string name, [FromBody] UpdateSchemaDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Synchronize a schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The schema object that needs to updated.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/sync")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutSchemaSync(string app, string name, [FromBody] SynchronizeSchemaDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Update a schema category.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The schema object that needs to updated.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/category")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutCategory(string app, string name, [FromBody] ChangeCategoryDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Update the preview urls.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The preview urls for the schema.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/preview-urls")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutPreviewUrls(string app, string name, [FromBody] ConfigurePreviewUrlsDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Update the scripts.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The schema scripts object that needs to updated.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/scripts/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasScripts)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutScripts(string app, string name, [FromBody] SchemaScriptsDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Update the rules.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema.</param>
        /// <param name="request">The schema rules object that needs to updated.</param>
        /// <returns>
        /// 200 => Schema updated.
        /// 400 => Schema request not valid.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/rules/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasUpdate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutRules(string app, string name, [FromBody] ConfigureFieldRulesDto request)
        {
            var command = request.ToCommand();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Publish a schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema to publish.</param>
        /// <returns>
        /// 200 => Schema published.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/publish/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasPublish)]
        [ApiCosts(1)]
        public async Task<IActionResult> PublishSchema(string app, string name)
        {
            var command = new PublishSchema();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Unpublish a schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema to unpublish.</param>
        /// <returns>
        /// 200 => Schema unpublished.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpPut]
        [Route("apps/{app}/schemas/{name}/unpublish/")]
        [ProducesResponseType(typeof(SchemaDto), StatusCodes.Status200OK)]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasPublish)]
        [ApiCosts(1)]
        public async Task<IActionResult> UnpublishSchema(string app, string name)
        {
            var command = new UnpublishSchema();

            var response = await InvokeCommandAsync(command);

            return Ok(response);
        }

        /// <summary>
        /// Delete a schema.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <param name="name">The name of the schema to delete.</param>
        /// <returns>
        /// 204 => Schema deleted.
        /// 404 => Schema or app not found.
        /// </returns>
        [HttpDelete]
        [Route("apps/{app}/schemas/{name}/")]
        [ApiPermissionOrAnonymous(Permissions.AppSchemasDelete)]
        [ApiCosts(1)]
        public async Task<IActionResult> DeleteSchema(string app, string name)
        {
            await CommandBus.PublishAsync(new DeleteSchema());

            return NoContent();
        }

        [HttpGet]
        [Route("apps/{app}/schemas/{name}/completion")]
        [ApiPermissionOrAnonymous]
        [ApiCosts(1)]
        [OpenApiIgnore]
        public async Task<IActionResult> GetScriptCompletiong(string app, string name)
        {
            var schema = await GetSchemaAsync(name);

            if (schema == null)
            {
                return NotFound();
            }

            var completer = new ScriptingCompletion();
            var completion = completer.GetCompletion(schema.SchemaDef, App.PartitionResolver());

            var result = completion.Select(x => new { x.Name, x.Description });

            return Ok(result);
        }

        private Task<ISchemaEntity?> GetSchemaAsync(string name)
        {
            if (Guid.TryParse(name, out var guid))
            {
                var schemaId = DomainId.Create(guid);

                return appProvider.GetSchemaAsync(AppId, schemaId);
            }
            else
            {
                return appProvider.GetSchemaAsync(AppId, name);
            }
        }

        private async Task<SchemaDto> InvokeCommandAsync(ICommand command)
        {
            var context = await CommandBus.PublishAsync(command);

            var result = context.Result<ISchemaEntity>();
            var response = SchemaDto.FromSchema(result, Resources);

            return response;
        }
    }
}
