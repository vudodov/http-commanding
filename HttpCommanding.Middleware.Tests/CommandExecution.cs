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
    public class CommandExecution
    {
        [Fact]
        public async Task Succeed()
        {
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-successful-command"])
                .Returns((command: typeof(TestSuccessfulCommand),
                    commandHandler: typeof(TestSuccessfulCommandHandler)));

            var middleware = new Middleware(
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
            
            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Accepted);
            JsonDocument.Parse(bodyContent).RootElement
                .GetProperty("commandId").GetGuid()
                .Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task Failed()
        {
            var registryMock = new Mock<ICommandRegistry>();
            registryMock.SetupGet(p => p["test-failing-command"])
                .Returns((command: typeof(TestFailingCommand),
                    commandHandler: typeof(TestFailingCommandHandler)));

            var middleware = new Middleware(
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

            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.Forbidden);
            JsonDocument.Parse(bodyContent).RootElement
                .GetProperty("reasons")[0].GetString()
                .Should().Be("failure reason");
        }
    }
}