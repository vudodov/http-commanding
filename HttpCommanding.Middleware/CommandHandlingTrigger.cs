using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCommanding.Middleware
{
    internal static class CommandHandlingTrigger
    {
        internal static async Task<CommandResult> Trigger(
            Type commandType, Type commandHandlerType, Guid commandId, PipeReader pipeReader,
            IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            object commandHandlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, commandHandlerType);
            MethodInfo handleAsyncMethod = commandHandlerType.GetMethod("HandleAsync");

            var command = await ReadCommandAsync(pipeReader, commandType, cancellationToken);

            return await (Task<CommandResult>) handleAsyncMethod.Invoke(commandHandlerInstance,
                new[] {command, commandId, cancellationToken});
        }

        private static async Task<object> ReadCommandAsync(
            PipeReader pipeReader, Type commandType, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readResult = await pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;
                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                {
                    var readCommandAsync = buffer.IsSingleSegment
                        ? JsonSerializer.Deserialize(buffer.FirstSpan, commandType)
                        : DeserializeSequence(buffer, commandType);
                    return readCommandAsync;
                }
            }

            throw new TaskCanceledException();
        }

        private static object DeserializeSequence(ReadOnlySequence<byte> buffer, Type commandType)
        {
            var jsonReader = new Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize(ref jsonReader, commandType);
        }
    }
}