using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Evoq.Ethereum.Contracts
{
    /// <summary>
    /// Helper class for loading ABI files from various locations
    /// </summary>
    public static class AbiFileHelper
    {
        /// <summary>
        /// Gets a stream for an ABI file that is embedded as a resource or located in the project
        /// </summary>
        /// <param name="fileName">Name of the ABI file</param>
        /// <param name="assembly">Optional assembly to search for embedded resources (defaults to calling assembly)</param>
        /// <returns>Stream containing the ABI content</returns>
        /// <exception cref="FileNotFoundException">Thrown when the ABI file cannot be found</exception>
        public static Stream GetAbiStream(string fileName, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            // First try to load as an embedded resource
            var resourceStream = TryGetEmbeddedResource(fileName, assembly);
            if (resourceStream != null)
                return resourceStream;

            // Then try to load from file system
            var fileStream = TryGetFileStream(fileName);
            if (fileStream != null)
                return fileStream;

            throw new FileNotFoundException(
                $"Could not find ABI file: {fileName}. Make sure it's included as an embedded resource or copied to the output directory.",
                fileName);
        }

        /// <summary>
        /// Attempts to load an ABI file as JSON string
        /// </summary>
        /// <param name="fileName">Name of the ABI file</param>
        /// <param name="assembly">Optional assembly to search for embedded resources</param>
        /// <returns>JSON string containing the ABI</returns>
        public static string GetAbiJson(string fileName, Assembly? assembly = null)
        {
            using var stream = GetAbiStream(fileName, assembly);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static Stream? TryGetEmbeddedResource(string fileName, Assembly assembly)
        {
            // Try exact match first
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                return assembly.GetManifestResourceStream(resourceName);
            }

            // Try with common namespace prefixes
            foreach (var prefix in GetNamespacePrefixes(assembly))
            {
                resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.Equals($"{prefix}.{fileName}", StringComparison.OrdinalIgnoreCase) ||
                                           name.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    return assembly.GetManifestResourceStream(resourceName);
                }
            }

            return null;
        }

        private static Stream? TryGetFileStream(string fileName)
        {
            var possiblePaths = new[]
            {
                // Output directory paths (for Content files)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Abis", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ABIs", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Contracts", fileName),
                
                // Current directory paths
                fileName,
                Path.Combine("Abis", fileName),
                Path.Combine("ABIs", fileName),
                Path.Combine("Contracts", fileName),
                
                // Working directory paths
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Abis", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "ABIs", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Contracts", fileName),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return File.OpenRead(path);
                }
            }

            return null;
        }

        private static string[] GetNamespacePrefixes(Assembly assembly)
        {
            // Get common namespace prefixes from the assembly
            var types = assembly.GetTypes();
            var namespaces = types
                .Select(t => t.Namespace)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToArray();

            return namespaces!;
        }
    }
}