using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Tests.MockedCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HttpCommanding.Middleware.Tests
{
    public class CommandContract
    {
        [Fact]
        public async Task When_getting_command_contract_It_should_return_json_schema_per_command()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (commandName: "testCommand", commandType: typeof(TestSuccessfulCommand),
                        commandHandler: typeof(TestSuccessfulCommandHandler))
                }.GetEnumerator());

            var cacheEntryMock = Mock.Of<ICacheEntry>();
            var memoryCacheMock = new Mock<IMemoryCache>();
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock);

            var middleware = new Middleware(
                async _ => { },
                commandRegistryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>());

            var responseBodyStream = new MemoryStream();
            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(responseBodyStream),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature(),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature()
            });

            httpContext.Request.Path = "/command";
            httpContext.Request.Method = HttpMethods.Get;

            await middleware.InvokeAsync(httpContext);

            responseBodyStream.Position = 0;
            var bodyContent = await new StreamReader(responseBodyStream).ReadToEndAsync();

            // JSchema.Parse(JObject.Parse(bodyContent)["testCommand"].ToString())
            //     .Should().BeEquivalentTo(
            //         JSchema.Parse(@"{
            //            ""type"": ""object"",
            //            ""properties"": {
            //              ""TestPropertyString"": {
            //                ""type"": [
            //                  ""string"",
            //                  ""null""
            //                ]
            //              },
            //              ""TestPropertyInt"": {
            //                ""type"": ""integer""
            //              }
            //            },
            //            ""required"": [
            //              ""TestPropertyString"",
            //              ""TestPropertyInt""
            //            ]
            //          }"));
            //}
        }
    }
}