using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Tests.MockedCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HttpCommanding.Middleware.Tests
{
    public class MiddlewareBehaviour
    {
        [Fact]
        public async Task ItShouldTerminatePipeline()
        {
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));
            var nextFlag = false;
           
            var middleware = new Middleware(
                async context => nextFlag = true,
                registryMock.Object,
                Mock.Of<IMemoryCache>());
            
            var httpContext = new DefaultHttpContext();

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;
            httpContext.Request.Path = "/command/test-successful-command";
            httpContext.Request.Method = "POST";

            await middleware.InvokeAsync(httpContext);

            nextFlag.Should().BeFalse();
        }
    }
}