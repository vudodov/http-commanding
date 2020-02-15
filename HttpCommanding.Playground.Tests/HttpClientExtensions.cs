using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace HttpCommanding.Playground.Tests
{
    internal static class HttpClientExtensions
    {
        internal static async Task<HttpResponseMessage> SendCommandAsync(this HttpClient client, 
            string commandName,
            string jsonPayload)
        {
            Console.WriteLine($"Command {commandName} with data {jsonPayload} was fired.");

            var command = new HttpRequestMessage(HttpMethod.Post, $"/command/{commandName}")
            {
                Content = new StringContent(
                    jsonPayload,
                    Encoding.UTF8, 
                    MediaTypeNames.Application.Json)
            };

            return await client.SendAsync(command);
        }
    }
}