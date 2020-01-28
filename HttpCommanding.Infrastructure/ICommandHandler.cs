using System.Threading;

namespace HttpCommanding.Infrastructure
{
    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        CommandResult HandleAsync(TCommand command, CancellationToken token);
    }
}