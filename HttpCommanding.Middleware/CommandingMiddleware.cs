using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using HttpCommanding.Middleware.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using NJsonSchema;

namespace HttpCommanding.Middleware
{
    public class CommandingMiddleware
    {
        private const string CacheKeyCommandContracts = "command-contracts";
        private readonly IMemoryCache _memoryCache;
        private readonly ICommandRegistry _registry;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private const string commandPathIdentifier = "command";

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = false
        };

        public CommandingMiddleware(RequestDelegate next, ICommandRegistry registry, IMemoryCache memoryCache,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _registry = registry;
            _logger = loggerFactory.CreateLogger<CommandingMiddleware>();
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var (middlewareIdentifier, commandName) = httpContext.Request.Path.DecomposePath();

            switch (middlewareIdentifier)
            {
                case commandPathIdentifier when HttpMethods.IsPost(httpContext.Request.Method) &&
                                                !string.IsNullOrWhiteSpace(commandName):
                    var commandId = Guid.NewGuid();
                    var commandResult = await ExecuteCommand(httpContext, commandName, commandId);
                    SetCommandHttpResponse(httpContext, commandResult, commandId);
                    break;
                case commandPathIdentifier when HttpMethods.IsGet(httpContext.Request.Method):
                    SetGetAllHttpResponse(httpContext);
                    break;
                default:
                    await _next.Invoke(httpContext);
                    break;
            }
        }

        private async Task<CommandResult> ExecuteCommand(HttpContext httpContext, string commandName, Guid commandId)
        {
            if (_registry.TryGetValue(commandName, out var requestTypeInformation))
            {
                var (requestType, requestTypeHandler) = requestTypeInformation;
                var cancellationToken = httpContext.RequestAborted;
                var request = await httpContext.Request.BodyReader.DeserializeBodyAsync(
                    requestType,
                    _jsonSerializerOptions,
                    cancellationToken);
                try
                {
                    return await requestTypeHandler.HandleCommand(request, commandId,
                        httpContext.RequestServices, cancellationToken);
                }
                catch (TargetInvocationException e) when (e.InnerException is CommandExecutionException)
                {
                    // _logger.LogError(e.InnerException.Message);
                    return CommandResult.Failure(e.InnerException.Message);
                }
                catch (TargetInvocationException e) when (e.InnerException is AggregateException aggregateException)
                {
                    return HandleAggregateException(aggregateException);
                }
                catch (AggregateException aggregateException)
                {
                    return HandleAggregateException(aggregateException);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            throw new NullReferenceException($"Command or command handler for {commandName} was not found");
        }

        private CommandResult HandleAggregateException(AggregateException aggregateException)
        {
            // if aggregate exception contain only single CommandExecutionException, handle it, otherwise re-throw
            var innerExceptions = aggregateException.Flatten().InnerExceptions;
            if (innerExceptions.Count == 1 && innerExceptions[0] is CommandExecutionException)
            {
                // _logger.LogError(e.InnerException.Message);
                return CommandResult.Failure(innerExceptions[0].Message);
            }

            throw aggregateException;
        }

        private void SetCommandHttpResponse(HttpContext httpContext, CommandResult result, Guid commandId)
        {
            var commandResponse = HttpCommandResponse.CreatedResponse(result, commandId);

            httpContext.Response.StatusCode = (int) commandResponse.ResponseCode;
            httpContext.Response.ContentType = MediaTypeNames.Application.Json;
            httpContext.Response.Headers["Cache-Control"] = "no-cache";

            httpContext.Response.BodyWriter.Write(
                JsonSerializer.SerializeToUtf8Bytes(commandResponse, commandResponse.GetType(),
                    _jsonSerializerOptions));
            httpContext.Response.BodyWriter.Complete();
        }

        private void SetGetAllHttpResponse(HttpContext httpContext)
        {
            static string CalculateChecksum(byte[] bytes)
            {
                using var md5 = MD5.Create();
                return Convert.ToBase64String(md5.ComputeHash(bytes));
            }

            if (!_memoryCache.TryGetValue(CacheKeyCommandContracts,
                out Dictionary<string, JsonElement> commandContracts))
            {
                commandContracts = new Dictionary<string, JsonElement>();

                foreach (var (commandName, commandType, commandTypeHandler) in _registry)
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
    }
}