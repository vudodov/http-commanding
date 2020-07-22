using System;

namespace HttpCommanding.Infrastructure
{
    public class CommandExecutionException : Exception
    {
        public CommandExecutionException(string message) : base(message)
        {
        }
    }
}