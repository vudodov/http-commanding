using System;
using System.Linq;

namespace HttpCommanding.Infrastructure.Extensions
{
    public static class TypeExtensions
    {
        internal static bool IsCommandHandlerFor(this Type handlerType, Type commandType) =>
            handlerType.GetInterfaces()
                .Any(@interface =>
                    @interface.IsGenericType
                    && @interface.GetGenericTypeDefinition().IsAssignableFrom(typeof(ICommandHandler<>))
                    && @interface.GetGenericArguments().Single().IsAssignableFrom(commandType));
    }
}