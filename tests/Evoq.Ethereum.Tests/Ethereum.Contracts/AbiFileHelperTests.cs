using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Evoq.Ethereum.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests.Contracts
{
    [TestClass]
    public class AbiFileHelperTests
    {
        private const string EasAbiFileName = "EAS.abi.json";
        private const string TestAbiFileName = "TestAbi.abi.json";
        private const string TestMarker = "This is a test ABI file specifically for AbiFileHelper tests";

        [TestMethod]
        public void GetAbiStream_WithContentFile_ReturnsStream()
        {
            // This test verifies loading the EAS.abi.json content file

            // Act
            using var stream = AbiFileHelper.GetAbiStream(EasAbiFileName);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Length > 0);
            // Verify it contains something we expect from the EAS ABI
            Assert.IsTrue(content.Contains("\"name\"") && content.Contains("\"type\""));
            // Verify it does NOT contain our test marker
            Assert.IsFalse(content.Contains(TestMarker));

            Console.WriteLine($"Successfully loaded content file: {EasAbiFileName}");
        }

        [TestMethod]
        public void GetAbiJson_WithContentFile_ReturnsJsonString()
        {
            // Act
            var json = AbiFileHelper.GetAbiJson(EasAbiFileName);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);
            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(json);
            Assert.IsNotNull(jsonDoc);

            Console.WriteLine($"Successfully loaded and parsed JSON from: {EasAbiFileName}");
        }

        [TestMethod]
        public void GetAbiStream_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFileName = "NonExistent.abi.json";

            // Act & Assert
            var ex = Assert.ThrowsException<FileNotFoundException>(() =>
                AbiFileHelper.GetAbiStream(nonExistentFileName));

            Console.WriteLine($"Correctly threw exception: {ex.Message}");
        }

        [TestMethod]
        public void GetAbiStream_WithEmbeddedResource_ReturnsStream()
        {
            // This test specifically verifies loading the embedded TestAbi.abi.json

            // First verify the embedded resource exists
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            Console.WriteLine("Available embedded resources:");
            foreach (var name in resourceNames)
            {
                Console.WriteLine($"  - {name}");
            }

            var hasEmbeddedResource = Array.Exists(resourceNames,
                name => name.EndsWith(TestAbiFileName, StringComparison.OrdinalIgnoreCase));

            if (!hasEmbeddedResource)
            {
                Assert.Inconclusive($"This test requires {TestAbiFileName} to be embedded as a resource. Available resources: {string.Join(", ", resourceNames)}");
                return;
            }

            // Act - explicitly pass the assembly to ensure we're testing embedded resources
            using var stream = AbiFileHelper.GetAbiStream(TestAbiFileName, assembly);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Length > 0);
            // Verify it contains our test marker to confirm it's the embedded version
            Assert.IsTrue(content.Contains(TestMarker),
                "The loaded file doesn't contain the test marker, suggesting it's not the embedded resource version");

            Console.WriteLine($"Successfully loaded embedded resource: {TestAbiFileName}");
        }

        [TestMethod]
        public void GetAbiStream_WithFileInSubfolder_FindsFile()
        {
            // This test verifies that the helper can find files in the Abis subfolder

            // Act - try to load the test file from the Abis subfolder
            using var stream = AbiFileHelper.GetAbiStream(TestAbiFileName);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains("testFunction"),
                "The loaded file doesn't contain expected content");

            // Get the actual path that was found
            string filePath = "(unknown)";
            if (stream is FileStream fs)
            {
                filePath = fs.Name;
                Console.WriteLine($"Found file at: {filePath}");
            }
            else
            {
                Console.WriteLine("File was loaded from a non-FileStream source (likely embedded resource)");
            }
        }
    }
}