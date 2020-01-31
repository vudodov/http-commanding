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

            registry["test-command"].command.Should().BeAssignableTo<TestCommand>();
            registry["test-command"].commandHandler.Should().BeAssignableTo<TestCommandHandler>();

            registry["another-test-command"].command.Should().BeAssignableTo<AnotherTestCommand>();
            registry["another-test-command"].commandHandler.Should().BeAssignableTo<AnotherTestCommandHandler>();
        }

        [Fact]
        public void WhenNoAssembliesProvidedItShouldThrow()
        {
            Action act = () => new CommandRegistry(new Assembly[0]);
            act.Should().Throw<ArgumentException>();
        }
    }
}