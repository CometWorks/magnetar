using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PluginSdk.Config;

namespace Magnetar.Legacy.Preparation;

internal static class ConfigurationDefaults
{
    // Inspect declarations only. Plugin constructors, getters and Init may require a live world.
    internal static SortedDictionary<string, string> Export(Type[] assemblyTypes, Type[] pluginTypes)
    {
        var configs = new HashSet<Type>();
        foreach (var property in pluginTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)))
        {
            if (!typeof(PluginConfig).IsAssignableFrom(property.PropertyType)) continue;
            Type type = property.PropertyType;
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || type == typeof(PluginConfig)
                || type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) == null
                || !assemblyTypes.Contains(type))
                throw new InvalidDataException("Managed preparation requires an owned concrete configuration type with a public parameterless constructor: " + property);
            configs.Add(type);
        }
        foreach (Type type in assemblyTypes.Where(type => type != typeof(PluginConfig)
                     && typeof(PluginConfig).IsAssignableFrom(type) && !type.IsAbstract))
            if (!configs.Contains(type))
                throw new InvalidDataException("Cannot determine whether configuration is used without initializing the plugin: " + type.FullName
                    + ". Expose each configuration through a public concrete PluginConfig property or supply a separately verified deployment.");

        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (Type type in configs.OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            try
            {
                var value = Activator.CreateInstance(type);
                string json = (string)typeof(ConfigStorage).GetMethod(nameof(ConfigStorage.SaveJson))
                    .MakeGenericMethod(type).Invoke(null, new[] { value });
                result.Add(type.FullName, json);
            }
            catch (Exception error)
            {
                throw new InvalidDataException("Cannot export configuration defaults without game initialization: " + type.FullName, error.GetBaseException());
            }
        }
        return result;
    }
}
