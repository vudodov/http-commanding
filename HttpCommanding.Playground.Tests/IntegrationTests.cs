using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HttpCommanding.Playground.Tests
{
    public class ExecutingCommands : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;

        public ExecutingCommands(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions {AllowAutoRedirect = false});
        }

        [Fact]
        public async Task ExecuteSuccessfulCommand()
        {
            var response = await _client.SendCommandAsync("successful-command", "{}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        [Fact]
        public async Task ExecuteFailedCommand()
        {
            var response = await _client.SendCommandAsync("failed-command", "{}");
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ExecuteCommandWithLogic()
        {
            var response = await _client.SendCommandAsync("condition-command", @"{ ""succeed"": true }");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            response = await _client.SendCommandAsync("condition-command", @"{ ""succeed"": false }");
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}