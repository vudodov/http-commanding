using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Tests.MockedCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

#pragma warning disable 1998

namespace HttpCommanding.Middleware.Tests
{
    public class WhenExecutingCommand
    {
        [Fact]
        public async Task It_should_succeed_if_handler_finished_successfully()
        {
            var mapping = (
                command: typeof(TestSuccessfulCommand),
                commandHandler: typeof(TestSuccessfulCommandHandler));
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.Setup(registry => registry.TryGetValue("test-successful-command", out mapping))
                .Returns(true);

            var middleware = new CommandingMiddleware(
                async context => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
                Mock.Of<ILoggerFactory>());

            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();
            await bodyRequestStream.WriteAsync(Encoding.UTF8.GetBytes(@"{""testProperty"": ""test"" }"));
            bodyRequestStream.Seek(0, SeekOrigin.Begin);

            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/command/test-successful-command",
                    Method = HttpMethods.Post
                },
            });

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);

            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();
            
            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.OK);
            JsonDocument.Parse(bodyContent).RootElement
                .GetProperty("commandId").GetGuid()
                .Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task It_should_fail_if_handler_finished_unsuccessfully()
        {
            var mapping = (
                command: typeof(TestFailingCommand),
                commandHandler: typeof(TestFailingCommandHandler));
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.Setup(registry => registry.TryGetValue("test-failing-command", out mapping))
                .Returns(true);

            var middleware = new CommandingMiddleware(
                async context => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
                Mock.Of<ILoggerFactory>());

            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();
            await bodyRequestStream.WriteAsync(Encoding.UTF8.GetBytes(@"{""testProperty"": ""test"" }"));
            bodyRequestStream.Seek(0, SeekOrigin.Begin);

            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/command/test-failing-command",
                    Method = HttpMethods.Post
                },
            });

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);
            
            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();

            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Conflict);
            JsonDocument.Parse(bodyContent).RootElement
                .GetProperty("reasons")[0].GetString()
                .Should().Be("failure reason");
        }
    }
}