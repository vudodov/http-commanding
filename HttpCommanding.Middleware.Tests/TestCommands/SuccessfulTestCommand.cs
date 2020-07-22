using System;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;

namespace HttpCommanding.Middleware.Tests.TestCommands
{
    // checksum: sjNLocD8ap3ZER7IhQyrqQ==
    public class SuccessfulTestCommand : ICommand
    {
        public string TestProperty { get; set; }
    }

    public class SuccessfulTestCommandHandler : ICommandHandler<SuccessfulTestCommand>
    {
        public Task<CommandResult> HandleAsync(SuccessfulTestCommand command, Guid commandId, CancellationToken token)
        {
            return Task.FromResult(CommandResult.Success());
        }
    }
}