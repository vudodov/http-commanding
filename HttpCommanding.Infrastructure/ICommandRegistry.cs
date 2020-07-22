using System;
using System.Collections.Generic;

namespace HttpCommanding.Infrastructure
{
    public interface ICommandRegistry : IEnumerable<(string commandName, Type commandType, Type commandHandlerType)>
    {
        bool TryGetValue(string commandName, out (Type commandType, Type commandHandlerType) map);
    }
}