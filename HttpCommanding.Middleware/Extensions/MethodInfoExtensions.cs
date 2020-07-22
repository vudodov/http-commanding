using System;
using System.Reflection;

namespace HttpCommanding.Middleware.Extensions
{
    internal static class MethodInfoExtensions
    {
        internal static object InvokeAndReturn(this MethodInfo methodInfo, object obj,
            params object[]? parameters)
        {
            return methodInfo.Invoke(obj, parameters) ??
                   throw new NullReferenceException(
                       $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name} should return Task, but found null");
        }
    }
}