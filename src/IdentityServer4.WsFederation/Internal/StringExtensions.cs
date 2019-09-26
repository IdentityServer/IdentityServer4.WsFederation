using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace IdentityServer4.Extensions
{
    internal static class Extensions
    {
        [DebuggerStepThrough]
        public static NameValueCollection AsNameValueCollection(this IDictionary<string, StringValues> collection)
        {
            var nv = new NameValueCollection();

            foreach (var field in collection)
            {
                nv.Add(field.Key, field.Value.First());
            }

            return nv;
        }

        [DebuggerStepThrough]
        public static bool IsMissing(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        [DebuggerStepThrough]
        public static bool IsMissingOrTooLong(this string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
            if (value.Length > maxLength)
            {
                return true;
            }

            return false;
        }

        [DebuggerStepThrough]
        public static bool IsPresent(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        [DebuggerStepThrough]
        public static bool IsLocalUrl(this string url)
        {
            return
                !String.IsNullOrEmpty(url) &&

                // Allows "/" or "/foo" but not "//" or "/\".
                ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) ||

                // Allows "~/" or "~/foo".
                (url.Length > 1 && url[0] == '~' && url[1] == '/'));
        }

        public static string AddQueryString(this string url, string query)
        {
            if (!url.Contains("?"))
            {
                if (!query.StartsWith("?"))
                {
                    url += "?";
                }
            }
            else if (!url.EndsWith("&"))
            {
                url += "&";
            }

            return url + query;
        }

        [DebuggerStepThrough]
        public static string AddQueryString(this string url, string name, string value)
        {
            return url.AddQueryString(name + "=" + UrlEncoder.Default.Encode(value));
        }
    }
}