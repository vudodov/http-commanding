using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Tests.TestCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

#pragma warning disable 1998

namespace HttpCommanding.Middleware.Tests
{
    public class When_getting_command_definitions
    {
        [Fact]
        public async Task It_should_return_json_schema_per_command()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (
                        commandName: "successfulTestCommand",
                        command: typeof(SuccessfulTestCommand),
                        commandHandler: typeof(SuccessfulTestCommandHandler))
                }.GetEnumerator());

            var cacheEntryMock = Mock.Of<ICacheEntry>();
            var memoryCacheMock = new Mock<IMemoryCache>();
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock);

            var middleware = new CommandingMiddleware(
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

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            responseBodyStream.ToArray()
                .Should().BeEquivalentTo(
                    JsonSerializer.SerializeToUtf8Bytes(
                        JsonDocument.Parse(@"
                {
                    ""successfulTestCommand"": {
                        ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                        ""title"": ""SuccessfulTestCommand"",
                        ""type"": ""object"",
                        ""additionalProperties"": false,
                        ""properties"": {
                                    ""TestProperty"": {
                                        ""type"": [
                                            ""null"",
                                            ""string""
                                        ]
                                    }
                        }
                    }
                }").RootElement));
        }

        [Fact]
        public async Task It_should_return_valid_etag()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (
                        commandName: "successfulTestCommand",
                        command: typeof(SuccessfulTestCommand),
                        commandHandler: typeof(SuccessfulTestCommandHandler))
                }.GetEnumerator());

            var cacheEntryMock = Mock.Of<ICacheEntry>();
            var memoryCacheMock = new Mock<IMemoryCache>();
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock);

            var middleware = new CommandingMiddleware(
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

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            httpContext.Response.Headers[HeaderNames.ETag].ToString().Should().Be("sjNLocD8ap3ZER7IhQyrqQ==");
        }
        
        [Fact]
        public async Task It_should_use_memory_cache()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (
                        commandName: "successfulTestCommand",
                        command: typeof(SuccessfulTestCommand),
                        commandHandler: typeof(SuccessfulTestCommandHandler))
                }.GetEnumerator());
            
            // Memory Cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue = new Dictionary<string, JsonElement>();
            bool isCreateMemoCacheEntryCalled = false;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(true);
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Callback(() => isCreateMemoCacheEntryCalled = true)
                .Returns(Mock.Of<ICacheEntry>());

            // Middleware setup
            var middleware = new CommandingMiddleware(
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

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            isCreateMemoCacheEntryCalled.Should().BeFalse();
        }
        
        [Fact]
        public async Task It_should_set_memory_cache_if_not_set()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (
                        commandName: "successfulTestCommand",
                        command: typeof(SuccessfulTestCommand),
                        commandHandler: typeof(SuccessfulTestCommandHandler))
                }.GetEnumerator());
            
            // Memory Cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue = new Dictionary<string, JsonElement>();
            bool isCreateMemoCacheEntryCalled = false;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Callback(() => isCreateMemoCacheEntryCalled = true)
                .Returns(Mock.Of<ICacheEntry>());

            // Middleware setup
            var middleware = new CommandingMiddleware(
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

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            isCreateMemoCacheEntryCalled.Should().BeTrue();
        }
        
        [Fact]
        public async Task It_should_return_304_Not_Modified_if_hash_matches()
        {
            var commandRegistryMock = new Mock<ICommandRegistry>();
            commandRegistryMock.Setup(commandRegistry => commandRegistry.GetEnumerator())
                .Returns(new List<(string commandName, Type command, Type commandHandler)>
                {
                    (
                        commandName: "successfulTestCommand",
                        command: typeof(SuccessfulTestCommand),
                        commandHandler: typeof(SuccessfulTestCommandHandler))
                }.GetEnumerator());

            var cacheEntryMock = Mock.Of<ICacheEntry>();
            var memoryCacheMock = new Mock<IMemoryCache>();
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<string>()))
                .Returns(cacheEntryMock);

            var middleware = new CommandingMiddleware(
                async _ => { },
                commandRegistryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>());

            
            var httpContext = new DefaultHttpContext();
            var memoryStream = httpContext.Features.Get<IHttpResponseBodyFeature>().Stream;
            
            httpContext.Request.Path = "/command";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.Headers.Add(HeaderNames.IfNoneMatch, "sjNLocD8ap3ZER7IhQyrqQ==");

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
        }
        
    }
}