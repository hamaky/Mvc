﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionFilterAttribute"/> which sets the appropriate headers related to Response caching.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ResponseCacheFilter : ActionFilterAttribute, IResponseCacheFilter
    {
        /// <summary>
        /// Gets or sets the duration in seconds for which the response is cached.
        /// This is a required parameter.
        /// This sets "max-age" in "Cache-control" header.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Gets or sets the location where the data from a particular URL must be cached.
        /// </summary>
        public ResponseCacheLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the value which determines whether the data should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "duration" parameter.
        /// </summary>
        public bool NoStore { get; set; }

        /// <summary>
        /// Gets or sets the value for the Vary response header.
        /// </summary>
        public string VaryByHeader { get; set; }

        public ResponseCacheFilter(int duration, ResponseCacheLocation location, bool noStore, string varyByHeader)
        {
            Duration = duration;
            Location = location;
            NoStore = noStore;
            VaryByHeader = varyByHeader;
        }

        // <inheritdoc />
        public override void OnActionExecuting([NotNull] ActionExecutingContext context)
        {
            // If there are more filters which can override the values written by this filter,
            // then skip execution of this filter.
            if (IsOverridden(context))
            {
                return;
            }

            var headers = context.HttpContext.Response.Headers;

            // Clear all headers
            headers.Remove("Vary");
            headers.Remove("Cache-control");
            headers.Remove("Pragma");

            if (!string.IsNullOrEmpty(VaryByHeader))
            {
                headers.Set("Vary", VaryByHeader);
            }

            if (NoStore)
            {
                headers.Set("Cache-control", "no-store");

                // Cache-control: no-store, no-cache is valid.
                if (Location == ResponseCacheLocation.None)
                {
                    headers.Append("Cache-control", "no-cache");
                    headers.Set("Pragma", "no-cache");
                }
            }
            else
            {
                string cacheControlValue = null;
                switch (Location)
                {
                    case ResponseCacheLocation.Any:
                        cacheControlValue = "public";
                        break;
                    case ResponseCacheLocation.Client:
                        cacheControlValue = "private";
                        break;
                    case ResponseCacheLocation.None:
                        cacheControlValue = "no-cache";
                        headers.Set("Pragma", "no-cache");
                        break;
                }

                cacheControlValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}max-age={2}",
                    cacheControlValue,
                    cacheControlValue != null? "," : null,
                    Duration);

                if (cacheControlValue != null)
                {
                    headers.Set("Cache-control", cacheControlValue);
                }
            }
        }

        // internal for Unit Testing purposes.
        internal bool IsOverridden([NotNull] ActionExecutingContext context)
        {
            // Return true if there are any filters which are after the current filter. In which case the current
            // filter should be skipped.
            return context.Filters.OfType<IResponseCacheFilter>().Last() != this;
        }
    }
}