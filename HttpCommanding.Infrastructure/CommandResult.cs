using System.Collections;
using System.Collections.Generic;

namespace HttpCommanding.Infrastructure
{
    public abstract class CommandResult
    {
        public static CommandResult Success()
        {
            return new Succeed();
        }

        public static CommandResult Failure(params string[] reasons)
        {
            return new Failed (reasons);
        }
    }

    public sealed class Succeed : CommandResult
    {
        internal Succeed()
        {
        }
    }

    public sealed class Failed : CommandResult
    {
        internal Failed(IEnumerable<string> reasons)
        {
            Reasons = reasons;
        }

        public IEnumerable Reasons { get; }
    }
}