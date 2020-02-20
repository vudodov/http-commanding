using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Playground.Commands
{
    public class ConditionCommand : ICommand
    {
        public bool Succeed { get; set; }
    }

    public class ConditionCommandHandler : ICommandHandler<ConditionCommand>
    {
        public Task<CommandResult> HandleAsync(ConditionCommand command, Guid commandId, CancellationToken token) =>
            Task.FromResult(command.Succeed ? CommandResult.Success() : CommandResult.Failure("Value is false"));
    }
}