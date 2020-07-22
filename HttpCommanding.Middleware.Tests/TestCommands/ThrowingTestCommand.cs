using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
#pragma warning disable 162

namespace HttpCommanding.Middleware.Tests.TestCommands
{
    public class ThrowingTestCommand : ICommand
    {
    }
    
    public class ThrowingTestCommandHandler : ICommandHandler<FailingTestCommand>
    {
        public Task<CommandResult> HandleAsync(FailingTestCommand command, Guid commandId, CancellationToken token)
        {
            throw new CommandExecutionException("exception message");
            return Task.FromResult(CommandResult.Success());
        }
    }
}