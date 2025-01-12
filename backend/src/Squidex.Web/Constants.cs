﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Infrastructure;
using Squidex.Shared;

namespace Squidex.Web
{
    public static class Constants
    {
        public static readonly string SecurityDefinition = "squidex-oauth-auth";

        public static readonly string ApiPrefix = "/api";

        public static readonly string ApiScope = "squidex-api";

        public static readonly string ApiSecurityScheme = "custom";

        public static readonly string OrleansClusterId = "squidex-v2";

        public static readonly string OrleansPrefix = "/orleans";

        public static readonly string PortalPrefix = "/portal";

        public static readonly string EmailScope = "email";

        public static readonly string RoleScope = "role";

        public static readonly string PermissionsScope = "permissions";

        public static readonly string ProfileScope = "squidex-profile";

        public static readonly string FrontendClient = DefaultClients.Frontend;

        public static readonly string InternalClientId = "squidex-internal";

        public static readonly string InternalClientSecret = "squidex-internal".ToSha256Base64();

        public static readonly string IdentityServerPrefix = "/identity-server";
    }
}
