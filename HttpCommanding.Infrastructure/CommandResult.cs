using System.Collections;

namespace HttpCommanding.Infrastructure
{
    public abstract class CommandResult
    {
        public static CommandResult Success()
        {
            return new Success();
        }

        public static CommandResult Failure(params string[] reasons)
        {
            return new Failure {Reasons = reasons};
        }
    }

    public sealed class Success : CommandResult
    {
        internal Success()
        {
        }
    }

    public sealed class Failure : CommandResult
    {
        internal Failure()
        {
        }

        public IEnumerable Reasons { get; internal set; }
    }
}