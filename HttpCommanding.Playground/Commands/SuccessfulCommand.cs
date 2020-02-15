using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Playground.Commands
{
    public class SuccessfulCommand : ICommand
    {
        public string TestSuccessfulProperty { get; set; }
    }

    public class SuccessfulCommandHandler : ICommandHandler<SuccessfulCommand>
    {
        public Task<CommandResult> HandleAsync(SuccessfulCommand command, Guid commandId, CancellationToken token) =>
            Task.FromResult(CommandResult.Success());
    }
}