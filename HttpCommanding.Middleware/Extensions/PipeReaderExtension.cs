using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HttpCommanding.Middleware.Extensions
{
    internal static class PipeReaderExtensions
    {
        public static async Task<object?> DeserializeBodyAsync(this PipeReader pipeReader, Type targetType,
            JsonSerializerOptions options, CancellationToken cancellationToken)
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
                            ? JsonSerializer.Deserialize(buffer.FirstSpan, targetType, options)
                            : DeserializeSequence(buffer, targetType);
                }
            }

            throw new TaskCanceledException();
        }

        private static object DeserializeSequence(ReadOnlySequence<byte> buffer, Type messageType)
        {
            var jsonReader = new Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize(ref jsonReader, messageType);
        }
    }
}