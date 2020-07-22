using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Middleware.Tests.TestCommands
{
    public class ThrowingAggregateExceptionTestCommand : ICommand
    {
        
    }

    public class ThrowingAggregateExceptionTestCommandHandler : ICommandHandler<ThrowingAggregateExceptionTestCommand>
    {
        public Task<CommandResult> HandleAsync(ThrowingAggregateExceptionTestCommand command, Guid commandId, CancellationToken token)
        {
            var task1 = Task.Factory.StartNew(() => {
                var child1 = Task.Factory.StartNew(() => {
                    var child2 = Task.Factory.StartNew(() => {
                        // This exception is nested inside three AggregateExceptions.
                        throw new CommandExecutionException("exception message");
                    }, TaskCreationOptions.AttachedToParent);
                }, TaskCreationOptions.AttachedToParent);
            });

            task1.Wait();
            
            return Task.FromResult(CommandResult.Success());
        }
    }
}