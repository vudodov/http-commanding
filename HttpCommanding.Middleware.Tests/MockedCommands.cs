using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Middleware.Tests.MockedCommands
{
    // md5: HGfGbyN2o2ta0l4csiITYQ==
    public class TestSuccessfulCommand : ICommand
    {
        public string TestProperty { get; set; }
    }

    public class TestSuccessfulCommandHandler : ICommandHandler<TestSuccessfulCommand>
    {
        public Task<CommandResult> HandleAsync(TestSuccessfulCommand command, Guid commandId, CancellationToken token)
        {
            return Task.FromResult(CommandResult.Success());
        }
    }
    
    public class TestFailingCommand : ICommand
    {
        public string TestProperty { get; set; }
    }

    public class TestFailingCommandHandler : ICommandHandler<TestFailingCommand>
    {
        public Task<CommandResult> HandleAsync(TestFailingCommand command, Guid commandId, CancellationToken token)
        {
            return Task.FromResult(CommandResult.Failure("failure reason"));
        }
    }
}