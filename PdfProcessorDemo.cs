using System;
using System.IO;
using System.Text.Json;
using Wasmtime;

namespace PdfiumWasmIntegration
{
    /// <summary>
    /// Demo application showing how to use PdfProcessor for text extraction and PDF-to-JSON conversion
    /// </summary>
    class PdfProcessorDemo
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PDF Processing Demo with Pdfium WASM");
            Console.WriteLine("====================================\n");

            // Check command line arguments
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run <pdf-file-path> [operation]");
                Console.WriteLine();
                Console.WriteLine("Operations:");
                Console.WriteLine("  text  - Extract text from PDF (default)");
                Console.WriteLine("  json  - Convert PDF to JSON using QPDF");
                Console.WriteLine("  both  - Perform both operations");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run sample.pdf");
                Console.WriteLine("  dotnet run sample.pdf text");
                Console.WriteLine("  dotnet run sample.pdf json");
                Console.WriteLine("  dotnet run sample.pdf both");
                return;
            }

            string pdfPath = args[0];
            string operation = args.Length > 1 ? args[1].ToLower() : "text";

            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Error: PDF file not found: {pdfPath}");
                return;
            }

            try
            {
                // Initialize Wasmtime and load WASM module
                Console.WriteLine("Initializing Pdfium WASM module...");
                string wasmPath = "/home/akash/Dev/ironsoft/auto-pqdfium-rs/web/auto_pqdfium_rs.wasm";
                Console.WriteLine($"Loading: {wasmPath}");

                using var engine = new Engine();
                using var store = new Store(engine);

                var module = Module.FromFile(engine, wasmPath);
                Console.WriteLine("✓ WASM module loaded");

                // Inspect module imports and exports
                Console.WriteLine("\nModule Exports (first 20):");
                int exportCount = 0;
                foreach (var export in module.Exports)
                {
                    if (exportCount++ < 20)
                        Console.WriteLine($"  {export.Name}");
                }
                Console.WriteLine($"Total exports: {module.Exports.Count()}");

                // Set up Emscripten runtime
                var emscriptenRuntime = new EmscriptenRuntime(store);
                var linker = new Linker(engine);

                var wasiConfiguration = new WasiConfiguration();
                store.SetWasiConfiguration(wasiConfiguration);
                linker.DefineWasi();
                emscriptenRuntime.DefineImports(linker);

                Console.WriteLine("✓ Emscripten runtime configured");

                // Instantiate module
                Console.WriteLine("Instantiating WASM module...");
                var instance = linker.Instantiate(store, module);
                emscriptenRuntime.SetInstance(instance);
                Console.WriteLine("✓ Module instantiated");

                // Initialize PDFium
                var pdfiumInit = instance.GetFunction("PDFium_Init");
                pdfiumInit?.Invoke();
                Console.WriteLine("✓ PDFium initialized\n");

                // Create PDF processor
                var processor = new PdfProcessor(store, instance);

                // Perform requested operation
                switch (operation)
                {
                    case "text":
                        ExtractTextDemo(processor, pdfPath);
                        break;

                    case "json":
                        ConvertToJsonDemo(processor, pdfPath);
                        break;

                    case "both":
                        ExtractTextDemo(processor, pdfPath);
                        Console.WriteLine("\n" + new string('=', 60) + "\n");
                        ConvertToJsonDemo(processor, pdfPath);
                        break;

                    default:
                        Console.WriteLine($"Unknown operation: {operation}");
                        Console.WriteLine("Valid operations: text, json, both");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                }
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Demo: Extract text from PDF
        /// </summary>
        static void ExtractTextDemo(PdfProcessor processor, string pdfPath)
        {
            Console.WriteLine($"Extracting text from: {Path.GetFileName(pdfPath)}");
            Console.WriteLine(new string('-', 60));

            var result = processor.ExtractText(pdfPath);

            if (result.Success)
            {
                Console.WriteLine($"\n✓ Text extraction successful!");
                Console.WriteLine($"\nStatistics:");
                Console.WriteLine($"  Pages: {result.PageCount}");
                Console.WriteLine($"  Total Characters: {result.TotalCharacters:N0}");
                Console.WriteLine($"  Total Words (approx): {EstimateWordCount(result.FullText):N0}");

                // Show per-page breakdown
                Console.WriteLine($"\nPer-Page Breakdown:");
                foreach (var page in result.Pages)
                {
                    if (page.Error != null)
                    {
                        Console.WriteLine($"  Page {page.PageNumber}: Error - {page.Error}");
                    }
                    else
                    {
                        Console.WriteLine($"  Page {page.PageNumber}: {page.CharacterCount:N0} characters");
                    }
                }

                // Show preview of full text
                Console.WriteLine($"\n--- Text Preview (first 500 characters) ---");
                string preview = result.FullText.Length > 500
                    ? result.FullText.Substring(0, 500) + "..."
                    : result.FullText;
                Console.WriteLine(preview);
                Console.WriteLine($"--- End Preview ---");

                // Optionally save to file
                string outputPath = Path.ChangeExtension(pdfPath, ".txt");
                File.WriteAllText(outputPath, result.FullText);
                Console.WriteLine($"\n✓ Full text saved to: {outputPath}");
            }
            else
            {
                Console.WriteLine("✗ Text extraction failed");
            }
        }

        /// <summary>
        /// Demo: Convert PDF to JSON using QPDF
        /// </summary>
        static void ConvertToJsonDemo(PdfProcessor processor, string pdfPath)
        {
            Console.WriteLine($"Converting PDF to JSON: {Path.GetFileName(pdfPath)}");
            Console.WriteLine(new string('-', 60));

            try
            {
                var jsonDoc = processor.ConvertToJson(pdfPath, version: 2);

                Console.WriteLine($"\n✓ PDF to JSON conversion successful!");

                // Pretty print JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(jsonDoc, options);

                Console.WriteLine($"\nJSON Size: {jsonString.Length:N0} characters");

                // Show preview
                Console.WriteLine($"\n--- JSON Preview (first 1000 characters) ---");
                string preview = jsonString.Length > 1000
                    ? jsonString.Substring(0, 1000) + "\n..."
                    : jsonString;
                Console.WriteLine(preview);
                Console.WriteLine($"--- End Preview ---");

                // Save to file
                string outputPath = Path.ChangeExtension(pdfPath, ".json");
                File.WriteAllText(outputPath, jsonString);
                Console.WriteLine($"\n✓ JSON saved to: {outputPath}");

                jsonDoc.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ JSON conversion failed: {ex.Message}");
                Console.WriteLine("\nNote: QPDF conversion requires the IPDF_QPDF_PDFToJSON function");
                Console.WriteLine("to be available in the WASM module. This may not be included");
                Console.WriteLine("in all Pdfium builds.");
            }
        }

        /// <summary>
        /// Estimate word count from text
        /// </summary>
        static int EstimateWordCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
