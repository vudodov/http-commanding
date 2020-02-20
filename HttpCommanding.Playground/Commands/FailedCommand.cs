using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Playground.Commands
{
    public class FailedCommand : ICommand
    {
        public string TestFailedProperty { get; set; }
    }

    public class FailedCommandHandler : ICommandHandler<FailedCommand>
    {
        public Task<CommandResult> HandleAsync(FailedCommand command, Guid commandId, CancellationToken token) =>
            Task.FromResult(CommandResult.Failure());
    }
}