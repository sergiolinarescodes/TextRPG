using System;
using System.Collections.Generic;

namespace TextRPG.Core.Services
{
    internal static class AssemblyScanner
    {
        public static List<T> FindAll<T>() where T : class
        {
            var results = new List<T>();
            foreach (var type in typeof(T).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || !typeof(T).IsAssignableFrom(type))
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                results.Add((T)Activator.CreateInstance(type));
            }
            return results;
        }

        public static Dictionary<TKey, T> FindAll<T, TKey>(Func<T, TKey> keySelector) where T : class
        {
            var results = new Dictionary<TKey, T>();
            foreach (var instance in FindAll<T>())
                results[keySelector(instance)] = instance;
            return results;
        }

        public static void ScanInto<T>(IAutoScanRegistry<T> registry) where T : class
        {
            foreach (var instance in FindAll<T>())
                registry.RegisterScanned(instance);
        }

        public static TRegistry BuildRegistry<TRegistry, T>()
            where TRegistry : IAutoScanRegistry<T>, new()
            where T : class
        {
            var registry = new TRegistry();
            ScanInto(registry);
            return registry;
        }
    }
}
