using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HttpCommanding.Infrastructure.Extensions
{
    public static class AssemblyExtensions
    {
        internal static IEnumerable<Type> GetCommandTypes(this Assembly assembly) =>
            assembly.GetTypes().Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                typeof(ICommand).IsAssignableFrom(type));

        internal static Type GetCommandHandlerTypeFor(this Assembly assembly, Type commandType) =>
            assembly.GetTypes()
                .Single(type => type.IsCommandHandlerFor(commandType));
    }
}