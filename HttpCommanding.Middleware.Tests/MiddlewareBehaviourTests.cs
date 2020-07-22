using System;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Tests.MockedCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

#pragma warning disable 1998

namespace HttpCommanding.Middleware.Tests
{
    public class When_middleware_is_executing
    {
        [Fact]
        public async Task It_should_terminate_the_pipeline_if_command_passed()
        {
            var mapping = (
                command: typeof(TestSuccessfulCommand),
                commandHandler: typeof(TestSuccessfulCommandHandler));
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.Setup(registry => registry.TryGetValue("test-successful-command", out mapping))
                .Returns(true);
            var nextFlag = false;

            var middleware = new CommandingMiddleware(
                async context => nextFlag = true,
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
                Mock.Of<ILoggerFactory>());

            var httpContext = new DefaultHttpContext();

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;
            httpContext.Request.Path = "/command/test-successful-command";
            httpContext.Request.Method = "POST";

            await middleware.InvokeAsync(httpContext);

            nextFlag.Should().BeFalse();
        }

        [Fact]
        public async Task It_should_execute_next_middleware_if_no_command_passed()
        {
            var mapping = (
                command: typeof(TestSuccessfulCommand),
                commandHandler: typeof(TestSuccessfulCommandHandler));
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.Setup(registry => registry.TryGetValue("test-successful-command", out mapping))
                .Returns(true);
            var nextFlag = false;

            var middleware = new CommandingMiddleware(
                async context => nextFlag = true,
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
                Mock.Of<ILoggerFactory>());

            var httpContext = new DefaultHttpContext();

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;
            httpContext.Request.Path = "/not-command";
            httpContext.Request.Method = "POST";

            await middleware.InvokeAsync(httpContext);

            nextFlag.Should().BeTrue();
        }
    }
}