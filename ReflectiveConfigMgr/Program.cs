using System;
using System.Configuration;
using System.Reflection;
using Dependency;

namespace ReflectiveConfigMgr
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var inMemoryConnectionString = "Server=ALX-DEV;Database=AlexandraDemo;Trusted_Connection=True;Application Name=Bwaa;";

            var _ = new DependencyItem(
                data => Console.WriteLine($"Pre-patch dependency found the following connection string: {data}")
            );

            PatchConnectionString(inMemoryConnectionString);

            var __ = new DependencyItem(
                data => Console.WriteLine($"Post-patch dependency found the following connection string: {data}")
            );

            Console.WriteLine("Done.");
        }

        private static void PatchConnectionString(string connectionString)
        {
            var collectionType = typeof(ConfigurationElementCollection);

            var readOnlyFlag = collectionType.GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            var addMethod = collectionType.GetMethod(
                "BaseAdd", // Method name
                BindingFlags.Instance | BindingFlags.NonPublic, // Binding flags
                null, // Not relevant - we can pass the correct type.
                new[] {typeof(ConfigurationElement)}, // Parameter types
                null // Parameter modifiers (out, ref etc.)
            );
            var removeMethod = collectionType.GetMethod(
                "BaseRemove",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] {typeof(string)},
                null
            );

            if (readOnlyFlag == null || addMethod == null || removeMethod == null)
            {
                // Probably want to throw here if you're using this in production
                // Also probably don't use this in production - you're overwriting the singleton for the entire app domain.
                return;
            }

            // Must use try-finally in order to avoid leaving configmgr in an undefined state
            try
            {
                // Unset read-only flag
                readOnlyFlag.SetValue(ConfigurationManager.ConnectionStrings, false);

                // Check if string already exists
                if (ConfigurationManager.ConnectionStrings["default"] != null)
                {
                    // Clear it.
                    // Make sure we pass the parameter as an array of object to avoid runtime exception.
                    removeMethod.Invoke(ConfigurationManager.ConnectionStrings, new object[] {"default"});
                }

                // Call BaseAdd
                var settings = new ConnectionStringSettings("default", connectionString);
                addMethod.Invoke(ConfigurationManager.ConnectionStrings, new object[] {settings});
            }
            finally
            {
                try
                {
                    // Reset flag once we're done.
                    readOnlyFlag.SetValue(ConfigurationManager.ConnectionStrings, true);
                }
                catch
                {
                    // Suppress.
                }
            }
        }
    }
}
