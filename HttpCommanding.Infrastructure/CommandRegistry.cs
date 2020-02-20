using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HttpCommanding.Infrastructure
{
    public sealed class CommandRegistry : ICommandRegistry
    {
        private readonly IDictionary<string, (Type command, Type commandHandler)> _mapping;

        public CommandRegistry(IEnumerable<Assembly> assemblies)
        {
            _mapping = Scan(assemblies);
        }

        public (Type command, Type commandHandler) this[string command]
        {
            get
            {
                if (_mapping.TryGetValue(command.ToLowerInvariant(), out var map))
                    return map;
                throw new KeyNotFoundException($"Command {command} was not found.");
            }
        }

        public IEnumerator<(string commandName, Type command, Type commandHandler)> GetEnumerator()
        {
            return _mapping.Select(m =>
                    (commandName: m.Key, command: m.Value.command, commandHandler: m.Value.commandHandler))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static Dictionary<string, (Type command, Type commandHandler)> Scan(IEnumerable<Assembly> assemblies)
        {
            if (!assemblies.Any())
                throw new ArgumentException("Command registry requires at least one assembly to scan for commands");
            try
            {
                var commandHandlerMapping = new HashSet<(Type command, Type commandHandler)>();

                foreach (var assembly in assemblies)
                {
                    var discoveredCommandTypes = assembly.GetTypes().Where(type =>
                        type.IsClass && !type.IsAbstract && typeof(ICommand).IsAssignableFrom(type));

                    foreach (var commandType in discoveredCommandTypes)
                    {
                        bool GetCommandHandlerInterfacePredicate(Type @interface) => 
                            @interface.IsGenericType 
                            && @interface.GetGenericTypeDefinition() == typeof(ICommandHandler<>) 
                            && @interface.GetGenericArguments().Single().IsAssignableFrom(commandType);

                        var handlerType =
                            assembly.GetTypes()
                                .Single(type => type.GetInterfaces()
                                    .Any(GetCommandHandlerInterfacePredicate));
                        
                        commandHandlerMapping.Add((command: commandType, commandHandler: handlerType));
                    }
                }

                return commandHandlerMapping.ToDictionary(map =>
                    map.command.Name.ToKebabCase(), map => map);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Each command must have one and only one command handler", e);
            }
        }
    }
}