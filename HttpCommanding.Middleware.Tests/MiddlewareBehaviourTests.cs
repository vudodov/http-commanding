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
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));
            var nextFlag = false;
           
            var middleware = new Middleware(
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
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));
            var nextFlag = false;
           
            var middleware = new Middleware(
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
        
        [Fact]
        public async Task It_should_throw_http_exception_if_wrong_http_method_is_called()
        {
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));
            
            var middleware = new Middleware(
                async _ => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
                Mock.Of<ILoggerFactory>());
            
            var httpContext = new DefaultHttpContext();

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;
            httpContext.Request.Path = "/command/test-successful-command";
            httpContext.Request.Method = "PUT";

            Func<Task> action = async () => await middleware.InvokeAsync(httpContext);

            await action.Should().ThrowAsync<HttpRequestException>();
        }
    }
}