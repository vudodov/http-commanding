using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using NJsonSchema;

namespace HttpCommanding.Middleware
{
    public class Middleware
    {
        private const string CacheKeyCommandContracts = "command-contracts";
        private readonly ICommandRegistry _commandRegistry;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = false
        };


        public Middleware(RequestDelegate next, ICommandRegistry commandRegistry, IMemoryCache memoryCache,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _commandRegistry = commandRegistry;
            _memoryCache = memoryCache;
            _logger = loggerFactory.CreateLogger<HttpCommanding.Middleware.Middleware>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            async Task ExecuteCommand(string commandName)
            {
                Guid commandId = Guid.NewGuid();

                void SetResponse(CommandResult result)
                {
                    var httpCommandResult = HttpCommandResponse.CreatedResponse(result, commandId);
                    httpContext.Response.StatusCode = (int) httpCommandResult.ResponseCode;

                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    httpContext.Response.BodyWriter.Write(
                        JsonSerializer.SerializeToUtf8Bytes(httpCommandResult, httpCommandResult.GetType(),
                            _jsonSerializerOptions));
                    httpContext.Response.BodyWriter.Complete();
                    httpContext.Response.Headers["Cache-Control"] = "no-cache";
                }

                _commandRegistry.TryGetValue(commandName, out var commandMap);

                var response = await CommandHandlerExecutor.Execute(
                    commandMap.commandType, commandMap.commandHandlerType, commandId,
                    httpContext.Request.BodyReader, httpContext.RequestServices,
                    httpContext.RequestAborted, _jsonSerializerOptions);

                SetResponse(response);
            }

            void ProcessGet()
            {
                string CalculateChecksum(byte[] bytes)
                {
                    using (var md5 = MD5.Create())
                    {
                        return Convert.ToBase64String(md5.ComputeHash(bytes));
                    }
                }

                if (!_memoryCache.TryGetValue(CacheKeyCommandContracts,
                    out Dictionary<string, JsonElement> commandContracts))
                {
                    commandContracts = new Dictionary<string, JsonElement>();

                    foreach (var (commandName, commandType, commandHandler) in _commandRegistry)
                    {
                        var schema = JsonSchema.FromType(commandType);
                        commandContracts.Add(commandName, JsonDocument.Parse(schema.ToJson()).RootElement);
                    }

                    using var cacheEntry = _memoryCache.CreateEntry(CacheKeyCommandContracts);
                    cacheEntry.Value = commandContracts;
                }

                var bodyByteArray = JsonSerializer.SerializeToUtf8Bytes(commandContracts, _jsonSerializerOptions);

                var checksum = CalculateChecksum(bodyByteArray);

                if (httpContext.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) && checksum == etag)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                    return;
                }

                httpContext.Response.Headers[HeaderNames.ETag] = checksum;
                httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                httpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                httpContext.Response.BodyWriter.Write(bodyByteArray);
                httpContext.Response.BodyWriter.Complete();
            }

            var path = httpContext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (path[0] == "cmd" || path[0] == "command")
                if (HttpMethods.IsPost(httpContext.Request.Method))
                    if (httpContext.Request.ContentType.StartsWith(MediaTypeNames.Application.Json) &&
                        !string.IsNullOrWhiteSpace(path[1]))
                        try
                        {
                            await ExecuteCommand(path[1]);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed execute command");
                            throw;
                        }
                    else
                    {
                        var exception =
                            new HttpRequestException(
                                "Command content-type must be JSON and path should contain command name");
                        _logger.LogError(exception, "Failed execute command");

                        throw exception;
                    }
                else if (HttpMethods.IsGet(httpContext.Request.Method)) ProcessGet();
                else throw new HttpRequestException("HTTP method should be POST or GET");
            else
                await _next.Invoke(httpContext);
        }
    }
}