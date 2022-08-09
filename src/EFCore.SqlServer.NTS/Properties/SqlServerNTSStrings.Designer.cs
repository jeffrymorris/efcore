// <auto-generated />

using System;
using System.Reflection;
using System.Resources;

#nullable enable

namespace Microsoft.EntityFrameworkCore.SqlServer.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static class SqlServerNTSStrings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.EntityFrameworkCore.SqlServer.Properties.SqlServerNTSStrings", typeof(SqlServerNTSStrings).Assembly);

        /// <summary>
        ///     UseNetTopologySuite requires AddEntityFrameworkSqlServerNetTopologySuite to be called on the internal service provider used.
        /// </summary>
        public static string NTSServicesMissing
            => GetString("NTSServicesMissing");

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name)!;
            for (var i = 0; i < formatterNames.Length; i++)
            {
                value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
            }

            return value;
        }
    }
}

