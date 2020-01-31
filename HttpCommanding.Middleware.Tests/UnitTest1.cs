using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HttpCommanding.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HttpCommanding.Middleware.Tests
{
    public class TestSuccessfulCommand : ICommand
    {
        public string TestProperty { get; set; }
    }

    public class TestSuccessfulCommandHandler : ICommandHandler<TestSuccessfulCommand>
    {
        public Task<CommandResult> HandleAsync(TestSuccessfulCommand command, Guid commandId, CancellationToken token)
        {
            return Task.FromResult(CommandResult.Success());
        }
    }

    public class CommandHandlerTrigger
    {
        // [Fact]
        // public async Task WhenCommandTriggeredItShouldBeHandled1()
        // {
        //     var registryMock = new Mock<ICommandRegistry>();
        //     //var handler = new Mock<ICommandHandler<It.IsSubtype<ICommand>>>();
        //     registryMock.SetupGet(p => p["test-successful-command"])
        //         .Returns((command: typeof(TestSuccessfulCommand),
        //             commandHandler: typeof(TestSuccessfulCommandHandler)));
        //
        //     var middleware = new Middleware(
        //         async context => { },
        //         registryMock.Object,
        //         Mock.Of<IMemoryCache>());
        //
        //     var httpContext = new DefaultHttpContext();
        //     httpContext.Request.ContentType = MediaTypeNames.Application.Json;
        //     httpContext.Request.Path = "/command/test-successful-command";
        //     httpContext.Request.Method = HttpMethods.Put;
        //
        //     await middleware.InvokeAsync(httpContext);
        //
        //     httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Accepted);
        // }

        [Fact]
        public async Task WhenCommandTriggeredItShouldBeHandled()
        {
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));

            var middleware = new Middleware(
                async context => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>());

            var bodyStream = new MemoryStream();
            await bodyStream.WriteAsync(Encoding.UTF8.GetBytes(@"{""testProperty"": ""test"" }"));
            bodyStream.Seek(0, SeekOrigin.Begin);
            
            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new HttpResponseFeature(),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    
                    Body = bodyStream,
                    Path = "/command/test-successful-command",
                    Method = HttpMethods.Put
                },
            });

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Accepted);
        }
    }

    // public class MiddlewareTest
    // {
    //     
    //     
    //     [Fact]
    //     public async Task WhenMiddlewareSuccessfullyFinishesItShouldReturnAcceptedStatusCode()
    //     {
    //         var registryMock = new Mock<ICommandRegistry>();
    //         registryMock.SetupGet(p => p["test-successful-command"]).Returns()
    //             .Returns(Task.FromResult(CommandResult.Success(new Guid())));
    //
    //         var middleware = new Middleware(
    //             async context => { },
    //             Mock.Of<ICommandRegistry>(),
    //             Mock.Of<IMemoryCache>());
    //
    //         var httpContext = new DefaultHttpContext();
    //         httpContext.Request.ContentType = "application/vnd.pageup.testCommand+json";
    //         httpContext.Request.Path = "/command";
    //         httpContext.Request.Method = HttpMethods.Put;
    //
    //         await middleware.InvokeAsync(httpContext, registryMock.Object);
    //
    //         httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Accepted);
    //     }
    // }
}