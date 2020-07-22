using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCommanding.Middleware.Extensions
{
    public static class TypeExtensions
    {
        internal static async Task<CommandResult> HandleCommand(this Type commandHandlerType, object? command, Guid commandId,
            IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            object requestHandlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, commandHandlerType);
            MethodInfo handleAsyncMethod = commandHandlerType.GetHandleAsyncMethod();
            
            var invocationTask = handleAsyncMethod.InvokeAndReturn(
                requestHandlerInstance,
                command!,
                commandId,
                cancellationToken);

            return await (Task<CommandResult>) invocationTask;
        }
        
        private static MethodInfo GetHandleAsyncMethod(this Type handlerType) =>
            handlerType.GetMethod("HandleAsync") ??
            throw new MissingMethodException(nameof(handlerType), "HandleAsync");
    }
}