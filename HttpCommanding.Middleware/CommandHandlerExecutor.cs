using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCommanding.Middleware
{
    internal static class CommandHandlerExecutor
    {
        internal static async Task<CommandResult> Execute(Type commandType, Type commandHandlerType, Guid commandId,
            PipeReader pipeReader,
            IServiceProvider serviceProvider, CancellationToken cancellationToken,
            JsonSerializerOptions jsonSerializerOptions)
        {
            object commandHandlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, commandHandlerType);
            MethodInfo? handleAsyncMethod = commandHandlerType.GetMethod("HandleAsync");
            
            if (handleAsyncMethod == null) throw new MissingMethodException(nameof(commandHandlerType), "HandleAsync");

            var command = await ReadCommandAsync(pipeReader, commandType, jsonSerializerOptions, cancellationToken);

            var commandResult = handleAsyncMethod.Invoke(commandHandlerInstance,
                new[] {command, commandId, cancellationToken});
            
            if (commandResult == null) throw new NullReferenceException("Command result cannot be null.");

            return await (Task<CommandResult>) commandResult;
        }

        private static async Task<object?> ReadCommandAsync(PipeReader pipeReader, Type commandType,
            JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readResult = await pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;
                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                {
                    return buffer.IsEmpty
                        ? null
                        : buffer.IsSingleSegment
                            ? JsonSerializer.Deserialize(buffer.FirstSpan, commandType, jsonSerializerOptions)
                            : DeserializeSequence(buffer, commandType);
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