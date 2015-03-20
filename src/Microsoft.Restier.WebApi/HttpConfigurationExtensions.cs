﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using WebApiODataEx = System.Web.OData.Extensions;

namespace Microsoft.Restier.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        // TODO GitHubIssue#51 : Support model lazy loading
        public static async Task<ODataRoute> MapODataDomainRoute<TController>(
            this HttpConfiguration config, string routeName, string routePrefix,
            Func<IDomain> domainFactory,
            ODataDomainBatchHandler batchHandler = null)
            where TController : ODataDomainController, new()
        {
            Ensure.NotNull(domainFactory, "domainFactory");

            using (var domain = domainFactory())
            {
                var model = await domain.GetModelAsync();
                var conventions = CreateODataDomainRoutingConventions<TController>(config, model);

                if (batchHandler != null && batchHandler.DomainFactory == null)
                {
                    batchHandler.DomainFactory = domainFactory;
                }

                var routes = config.Routes;
                routePrefix = RemoveTrailingSlash(routePrefix);

                if (batchHandler != null)
                {
                    batchHandler.ODataRouteName = routeName;
                    var batchTemplate = String.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch
                        : routePrefix + '/' + ODataRouteConstants.Batch;
                    routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
                }

                DefaultODataPathHandler odataPathHanlder = new DefaultODataPathHandler();

                var getResolverSettings = typeof(WebApiODataEx.HttpConfigurationExtensions).GetMethod("GetResolverSettings", BindingFlags.NonPublic | BindingFlags.Static);

                if (getResolverSettings != null)
                {
                    var resolveSettings = getResolverSettings.Invoke(null, new object[] { config });
                    PropertyInfo prop = odataPathHanlder.GetType().GetProperty("ResolverSetttings", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(odataPathHanlder, resolveSettings, null);
                    }

                    // In case WebAPI OData fix "ResolverSetttings" to "ResolverSettings". So we set both "ResolverSetttings" and "ResolverSettings".
                    prop = odataPathHanlder.GetType().GetProperty("ResolverSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(odataPathHanlder, resolveSettings, null);
                    }
                }

                var routeConstraint = new DefaultODataPathRouteConstraint(odataPathHanlder, model, routeName, conventions);
                var route = new ODataRoute(routePrefix, routeConstraint);
                routes.Add(routeName, route);
                return route;
            }
        }

        public static async Task<ODataRoute> MapODataDomainRoute<TController>(
            this HttpConfiguration config, string routeName, string routePrefix,
            ODataDomainBatchHandler batchHandler = null)
            where TController : ODataDomainController, new()
        {
            return await MapODataDomainRoute<TController>(
                config, routeName, routePrefix, () => new TController().Domain, batchHandler);
        }

        public static IList<IODataRoutingConvention> CreateODataDomainRoutingConventions<TController>(
            this HttpConfiguration config, IEdmModel model)
            where TController : ODataDomainController, new()
        {
            var conventions = ODataRoutingConventions.CreateDefault();
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                var unmapped = conventions[index] as UnmappedRequestRoutingConvention;
                if (unmapped != null)
                {
                    break;
                }
            }

            conventions.Insert(index, new DefaultODataRoutingConvention(typeof(TController).Name));
            conventions.Insert(0, new AttributeRoutingConvention(model, config));
            return conventions;
        }

        private static string RemoveTrailingSlash(string routePrefix)
        {
            if (String.IsNullOrEmpty(routePrefix))
            {
                return routePrefix;
            }

            var prefixLastIndex = routePrefix.Length - 1;
            if (routePrefix[prefixLastIndex] == '/')
            {
                // Remove the last trailing slash if it has one.
                routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
            }
            return routePrefix;
        }
    }
}
