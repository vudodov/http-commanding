using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HttpCommanding.Infrastructure.Extensions;

namespace HttpCommanding.Infrastructure
{
    public sealed class CommandRegistry : ICommandRegistry
    {
        private readonly IImmutableDictionary<string, (Type command, Type commandHandler)> _mapping;

        public CommandRegistry() : this(new[] {Assembly.GetCallingAssembly()})
        {
        }

        public CommandRegistry(IEnumerable<Assembly> assemblies)
        {
            _mapping = Scan(assemblies);
        }

        public bool TryGetValue(string commandName, out (Type commandType, Type commandHandlerType) map) =>
            _mapping.TryGetValue(commandName, out map);

        public IEnumerator<(string commandName, Type commandType, Type commandHandlerType)> GetEnumerator()
        {
            return _mapping.Select(m => (
                    commandName: m.Key,
                    commandType: m.Value.command,
                    commandHandlerType: m.Value.commandHandler))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static IImmutableDictionary<string, (Type commandType, Type commandTypeHandler)> Scan(
            IEnumerable<Assembly> assemblies)
        {
            if (!assemblies.Any())
                throw new ArgumentException("Command registry requires at least one assembly to scan for commands");

            var commandHandlerMapping = new Dictionary<Type, Type>();

            foreach (var assembly in assemblies)
            {
                var discoveredCommandTypes = assembly.GetCommandTypes();

                foreach (var commandType in discoveredCommandTypes)
                {
                    if (commandHandlerMapping.ContainsKey(commandType))
                        throw new InvalidOperationException(
                            $"Command mapping for {commandType.Name} already registered. Make sure you have single command and command handler for {commandType.Name}");

                    try
                    {
                        var commandHandlerType = assembly.GetCommandHandlerTypeFor(commandType);
                        commandHandlerMapping[commandType] = commandHandlerType;
                    }
                    catch (InvalidOperationException e)
                    {
                        throw new InvalidOperationException("Each command must have one and only one command handler", e);
                    }
                }
            }

            return commandHandlerMapping.ToImmutableDictionary(map =>
                map.Key.Name.ToKebabCase(), 
                map => (map.Key, map.Value));
        }
    }
}