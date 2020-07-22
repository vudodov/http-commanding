using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Middleware.Tests.TestCommands
{
    public class FailingTestCommand : ICommand
    {
        public string TestProperty { get; set; }
    }

    public class FailingTestCommandHandler : ICommandHandler<FailingTestCommand>
    {
        public Task<CommandResult> HandleAsync(FailingTestCommand command, Guid commandId, CancellationToken token)
        {
            return Task.FromResult(CommandResult.Failure("failure reason"));
        }
    }
}