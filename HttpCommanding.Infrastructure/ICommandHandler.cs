using System.Threading;
using System.Threading.Tasks;

namespace HttpCommanding.Infrastructure
{
    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        Task<CommandResult> HandleAsync(TCommand command, CancellationToken token);
    }
}