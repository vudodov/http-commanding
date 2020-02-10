using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace HttpCommanding.Middleware
{
    public class Middleware
    {
        private const int CommandPrefixLength = 23;
        private const string CacheKeyCommandContracts = "command-contracts";
        private readonly ICommandRegistry _commandRegistry;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
            async Task ProcessPost(string commandName)
            {
                Guid commandId = Guid.NewGuid();

                void SetResponse(CommandResult result)
                {
                    var httpCommandResult = HttpCommandResponse.CreatedResult(result, commandId);
                    httpContext.Response.StatusCode = (int) httpCommandResult.ResponseCode;

                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    httpContext.Response.BodyWriter.Write(
                        JsonSerializer.SerializeToUtf8Bytes(httpCommandResult, httpCommandResult.GetType(),
                            _jsonSerializerOptions));
                    httpContext.Response.BodyWriter.Complete();
                    httpContext.Response.Headers["Cache-Control"] = "no-cache";
                }

                var commandMap = _commandRegistry[commandName];

                var response = await CommandHandlingTrigger.Trigger(
                    commandMap.command, commandMap.commandHandler, commandId,
                    httpContext.Request.BodyReader, httpContext.RequestServices,
                    httpContext.RequestAborted);

                SetResponse(response);
            }

/*
            async Task ProcessGet()
            {
                string CalculateChecksum(byte[] bytes)
                {
                    using (var md5 = MD5.Create())
                    {
                        return Convert.ToBase64String(md5.ComputeHash(bytes));
                    }
                }

                if (!_memoryCache.TryGetValue(CacheKeyCommandContracts,
                    out Dictionary<string, JSchema> commandContracts))
                {
                    commandContracts = new Dictionary<string, JSchema>();
                    var generator = new JSchemaGenerator();

                    foreach (var (commandName, commandType) in _commandRegistry)
                    {
                        var schema = generator.Generate(commandType);
                        commandContracts.Add(commandName, schema);
                    }

                    using (var cacheEntry = _memoryCache.CreateEntry(CacheKeyCommandContracts))
                    {
                        cacheEntry.Value = commandContracts;
                    }
                }

                var bodyByteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(commandContracts));

                var checksum = CalculateChecksum(bodyByteArray);

                if (httpContext.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) && checksum == etag)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                    return;
                }

                httpContext.Response.Headers[HeaderNames.ETag] = checksum;
                httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                httpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                await httpContext.Response.Body.WriteAsync(bodyByteArray);
            }
*/
            var path = httpContext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if ((path[0] == "cmd" || path[0] == "command") && !string.IsNullOrWhiteSpace(path[1]))
                if (HttpMethods.IsPost(httpContext.Request.Method))
                    if (httpContext.Request.ContentType == MediaTypeNames.Application.Json)
                        await ProcessPost(path[1]);
                    else
                        throw new HttpRequestException("Command content-type must be JSON");
                //else if (HttpMethods.IsGet(httpContext.Request.Method)) await ProcessGet();
                else throw new HttpRequestException("HTTP method should be POST or GET");
            else
                await _next.Invoke(httpContext);
        }
    }
}