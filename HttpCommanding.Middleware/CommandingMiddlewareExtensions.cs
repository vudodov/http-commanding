using System.Reflection;
using HttpCommanding.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCommanding.Middleware
{
    public static class CommandingMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpCommanding(this IApplicationBuilder builder)
            => builder.UseMiddleware<CommandingMiddleware>();
        
        public static IApplicationBuilder UseHttpCommandingWithRegistry(this IApplicationBuilder builder, ICommandRegistry commandRegistry, string cacheKey)
            => builder.UseMiddleware<CommandingMiddleware>(commandRegistry, cacheKey);

        public static void AddHttpCommanding(this IServiceCollection serviceCollection, params Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ICommandRegistry>(new CommandRegistry(assemblies));
            serviceCollection.AddMemoryCache();
        }
    }
}