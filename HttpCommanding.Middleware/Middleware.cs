// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Net;
// using System.Net.Http;
// using System.Net.Mime;
// using System.Security.Cryptography;
// using System.Text;
// using System.Threading.Tasks;
// using HttpCommanding.Infrastructure;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Net.Http.Headers;
//
// namespace HttpCommanding.Middleware
// {
//     public class Middleware
//     {
//         private const int CommandPrefixLength = 23;
//         private const string CacheKeyCommandContracts = "command-contracts";
//         private readonly ICommandRegistry _commandRegistry;
//         private readonly IMemoryCache _memoryCache;
//
//         private readonly RequestDelegate _next;
//
//
//         public Middleware(RequestDelegate next, ICommandRegistry commandRegistry,
//             IMemoryCache memoryCache)
//         {
//             _next = next;
//             _commandRegistry = commandRegistry;
//             _memoryCache = memoryCache;
//         }
//
//         public async Task InvokeAsync(HttpContext httpContext)
//         {
//             async Task ProcessPut()
//             {
//                 async Task SetResponse(CommandResult result)
//                 {
//                     httpContext.Response.StatusCode =
//                         (int) (result is Success ? HttpStatusCode.Accepted : HttpStatusCode.Forbidden);
//
//                     httpContext.Request.ContentType = MediaTypeNames.Application.Json;
//
//                     await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(
//                         result,
//                         new JsonSerializerSettings
//                         {
//                             ContractResolver = new CamelCasePropertyNamesContractResolver()
//                         }));
//                 }
//
//                 var contentType = httpContext.Request.ContentType;
//                 var commandName = contentType.Substring(CommandPrefixLength,
//                     contentType.IndexOf("+", StringComparison.InvariantCultureIgnoreCase) - CommandPrefixLength);
//                 
//                 using (var reader = new StreamReader(httpContext.Request.BodyReader.AsStream()))
//                 {
//                     var commandType = _commandRegistry[commandName];
//                     var command = (ICommand) JsonConvert.DeserializeObject(await reader.ReadToEndAsync(), commandType);
//                     var response = await mediator.Send(command);
//                     await SetResponse(response);
//                 }
//             }
//
//             async Task ProcessGet()
//             {
//                 string CalculateChecksum(byte[] bytes)
//                 {
//                     using (var md5 = MD5.Create())
//                     {
//                         return Convert.ToBase64String(md5.ComputeHash(bytes));
//                     }
//                 }
//
//                 if (!_memoryCache.TryGetValue(CacheKeyCommandContracts,
//                     out Dictionary<string, JSchema> commandContracts))
//                 {
//                     commandContracts = new Dictionary<string, JSchema>();
//                     var generator = new JSchemaGenerator();
//
//                     foreach (var (commandName, commandType) in _commandRegistry)
//                     {
//                         var schema = generator.Generate(commandType);
//                         commandContracts.Add(commandName, schema);
//                     }
//
//                     using (var cacheEntry = _memoryCache.CreateEntry(CacheKeyCommandContracts))
//                     {
//                         cacheEntry.Value = commandContracts;
//                     }
//                 }
//
//                 var bodyByteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(commandContracts));
//
//                 var checksum = CalculateChecksum(bodyByteArray);
//
//                 if (httpContext.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) && checksum == etag)
//                 {
//                     httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
//                     return;
//                 }
//
//                 httpContext.Response.Headers[HeaderNames.ETag] = checksum;
//                 httpContext.Response.ContentType = MediaTypeNames.Application.Json;
//                 httpContext.Response.StatusCode = (int) HttpStatusCode.OK;
//                 await httpContext.Response.Body.WriteAsync(bodyByteArray);
//             }
//
//             var isPathCorrect = new[] {"/command", "/cmd"}.Any(path => path == httpContext.Request.Path.Value);
//
//             if (isPathCorrect)
//                 try
//                 {
//                     if (HttpMethods.IsPut(httpContext.Request.Method) &&
//                         httpContext.Request.ContentType.IsCommandContentType()) await ProcessPut();
//                     else if (HttpMethods.IsGet(httpContext.Request.Method)) await ProcessGet();
//                     else throw new HttpRequestException("HTTP method should be PUT or GET");
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                     throw;
//                 }
//             else
//                 await _next.Invoke(httpContext);
//         }
// }