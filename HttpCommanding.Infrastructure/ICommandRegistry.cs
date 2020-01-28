using System;
using System.Collections.Generic;

namespace HttpCommanding.Infrastructure
{
    public interface ICommandRegistry : IEnumerable<(string commandName, Type command, Type commandHandler)>
    {
        (Type command, Type commandHandler) this[string command] { get; }
    }
}