using Microsoft.AspNetCore.Builder;

namespace HttpCommanding.Middleware
{
    public static class Extensions
    {
        public static IApplicationBuilder UseHttpCommanding(this IApplicationBuilder builder)
            => builder.UseMiddleware<Middleware>();
    }
}