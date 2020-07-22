using System;
using Microsoft.AspNetCore.Http;

namespace HttpCommanding.Middleware.Extensions
{
    public static class PathStringExtensions
    {
        public static (string middlewareIdentifier, string targetName) DecomposePath(this PathString pathString)
        {
            var pathParts = pathString.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            switch (pathParts.Length)
            {
                case int length when length >= 2:
                    return (pathParts[0], pathParts[1]);
                case int length when length == 1:
                    return (pathParts[0], string.Empty);
                default:
                    return (string.Empty, string.Empty);
            }
        }
    }
}