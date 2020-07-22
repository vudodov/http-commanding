using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HttpCommanding.Infrastructure.Tests
{
    public class CommandRegistryTests
    {
        class TestCommand : ICommand
        {
        }

        class TestCommandHandler : ICommandHandler<TestCommand>
        {
            public Task<CommandResult> HandleAsync(TestCommand command, Guid commandId, CancellationToken token)
            {
                return Task.FromResult(CommandResult.Success());
            }
        }

        class AnotherTestCommand : ICommand
        {
        }

        class AnotherTestCommandHandler : ICommandHandler<AnotherTestCommand>
        {
            public Task<CommandResult> HandleAsync(AnotherTestCommand command, Guid commandId, CancellationToken token)
            {
                return Task.FromResult(CommandResult.Success());
            }
        }

        [Fact]
        public void WhenRunningRegistryItShouldMapNameToTypeCorrectly()
        {
            var registry = new CommandRegistry(new[] {Assembly.GetAssembly(typeof(TestCommand))});

            registry.TryGetValue("test-command", out var testMap).Should().BeTrue();
            testMap.commandType.Should().Be(typeof(TestCommand));
            testMap.commandHandlerType.Should().Be(typeof(TestCommandHandler));
            
            registry.TryGetValue("another-test-command", out var anotherTestMap).Should().BeTrue();
            anotherTestMap.commandType.Should().Be(typeof(AnotherTestCommand));
            anotherTestMap.commandHandlerType.Should().Be(typeof(AnotherTestCommandHandler));
        }

        [Fact]
        public void WhenNoAssembliesProvidedItShouldThrow()
        {
            Action act = () => new CommandRegistry(new Assembly[0]);
            act.Should().Throw<ArgumentException>();
        }
    }
}