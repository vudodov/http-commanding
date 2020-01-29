using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HttpCommanding.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCommanding.Middleware
{
    internal static class CommandHandlingTrigger
    {
        internal static async Task Trigger(Type commandType, Type commandHandlerType, PipeReader pipeReader,
            IServiceProvider serviceProvider)
        {
            object commandHandler = ActivatorUtilities.CreateInstance(serviceProvider, commandHandlerType);
            MethodInfo handleAsyncMethod = commandHandlerType.GetMethod("HandleAsync");
            await (Task<CommandResult>) handleAsyncMethod.Invoke(commandHandler,);

            var data = await pipeReader.ReadAsync();
            data.Buffer
            using (var reader = new StreamReader(pipeReader.AsStream()))
            {
            }
        }

        private static async Task<TCommand> ReadCommandAsync<TCommand>(PipeReader reader, CancellationToken cancellationToken)
            where TCommand : ICommand
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                var bytesSequence = readResult.Buffer;
                
                do
                {
                    position = byteSequance.PositionOf((byte) '}');
                    if (position != null)
                    {
                        var readOnlySequence = byteSequance.Slice(0, position.Value);
                        AddStringToList(ref results, in readOnlySequence);

                        // Skip the line + the \n character (basically position)
                        byteSequance = byteSequance.Slice(byteSequance.GetPosition(1, position.Value));
                    }
                }while(position)
                
            }
        }
        
        private static Task ProcessBytesAsync(ReadOnlySequence<byte> bytesSequence, CancellationToken token)
        {
            var stringWriter = new StringWriter();
            if (bytesSequence.IsSingleSegment)
            {
                new StringReader().ReadBlock(bytesSequence.First.Span)
                stringWriter.WriteAsync(bytesSequence.First.Span);
                ProcessSingle(bytesSequence.First.Span);
            }
            else
            {
                foreach (var segment in bytesSequence)
                {
                    ProcessSingle(segment.Span);
                }
            }
			
            return Task.CompletedTask;
        }

        private void ProcessSingle(ReadOnlySpan<byte> span)
        {
            _fileStream.Write(span);
        }
    }
}