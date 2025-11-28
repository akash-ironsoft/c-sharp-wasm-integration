using System;
using System.IO;
using Wasmtime;

namespace PdfiumWasmIntegration
{
    class Program
    {
        static void InspectModule(string[] args)
        {
            Console.WriteLine("Pdfium WASM Integration with Wasmtime");
            Console.WriteLine("=====================================\n");

            // Path to the WASM file
            string wasmPath = "/home/akash/Dev/ironsoft/iron-universal/Universal.PdfEditor/build/emscripten/wasm/release/node/pdfium.wasm";

            if (!File.Exists(wasmPath))
            {
                Console.WriteLine($"Error: WASM file not found at {wasmPath}");
                return;
            }

            try
            {
                // Create a new engine
                using var engine = new Engine();
                Console.WriteLine("✓ Wasmtime engine created");

                // Create a new store
                using var store = new Store(engine);
                Console.WriteLine("✓ Store created");

                // Load the WASM module
                Console.WriteLine($"Loading WASM module from: {wasmPath}");
                var module = Module.FromFile(engine, wasmPath);
                Console.WriteLine("✓ WASM module loaded successfully");

                // Get module imports and exports
                Console.WriteLine("\nModule Imports:");
                int importCount = 0;
                foreach (var import in module.Imports)
                {
                    Console.WriteLine($"  - {import.ModuleName}::{import.Name}");
                    importCount++;
                }
                Console.WriteLine($"Total imports: {importCount}");

                Console.WriteLine("\nModule Exports:");
                var exportsList = new System.Collections.Generic.List<string>();
                foreach (var export in module.Exports)
                {
                    exportsList.Add(export.Name);
                    if (exportsList.Count <= 20) // Limit display to first 20
                    {
                        Console.WriteLine($"  - {export.Name}");
                    }
                }
                if (exportsList.Count > 20)
                {
                    Console.WriteLine($"  ... and {exportsList.Count - 20} more exports");
                }
                Console.WriteLine($"Total exports: {exportsList.Count}");

                // Create EmscriptenRuntime to handle all Emscripten imports
                var emscriptenRuntime = new EmscriptenRuntime(store);
                Console.WriteLine("✓ EmscriptenRuntime created");

                // Create a linker to handle imports
                var linker = new Linker(engine);

                // Define WASI if the module needs it
                var wasiConfiguration = new WasiConfiguration();
                store.SetWasiConfiguration(wasiConfiguration);
                linker.DefineWasi();
                Console.WriteLine("✓ WASI configuration set up");

                // Define all Emscripten imports
                Console.WriteLine("Defining Emscripten runtime imports...");
                emscriptenRuntime.DefineImports(linker);
                Console.WriteLine("✓ Emscripten runtime imports defined");

                // Try to instantiate the module
                Console.WriteLine("\nInstantiating module...");

                try
                {
                    var instance = linker.Instantiate(store, module);
                    Console.WriteLine("✓ Module instantiated successfully!");

                    // Set the instance in the runtime for indirect function calls
                    emscriptenRuntime.SetInstance(instance);

                    // Try to get and call specific exported functions
                    var memory = instance.GetMemory("memory");
                    if (memory != null)
                    {
                        Console.WriteLine($"✓ Memory export found - Size: {memory.GetLength()} bytes");
                    }

                    // Try to call PDFium initialization
                    Console.WriteLine("\nAttempting to initialize PDFium...");
                    var pdfiumInitFunc = instance.GetFunction("PDFium_Init");
                    if (pdfiumInitFunc != null)
                    {
                        Console.WriteLine("Calling PDFium_Init()...");
                        pdfiumInitFunc.Invoke();
                        Console.WriteLine("✓ PDFium_Init() called successfully!");
                    }
                    else
                    {
                        Console.WriteLine("⚠ PDFium_Init function not found");
                    }

                    // Show some available PDFium functions
                    Console.WriteLine("\nAvailable PDFium functions:");
                    var pdfFunctions = new[] {
                        "FPDF_InitLibraryWithConfig",
                        "FPDFPage_CreateAnnot",
                        "FPDFPage_GetAnnotCount",
                        "FPDFAnnot_GetSubtype"
                    };
                    foreach (var funcName in pdfFunctions)
                    {
                        var func = instance.GetFunction(funcName);
                        if (func != null)
                        {
                            Console.WriteLine($"  ✓ {funcName}");
                        }
                    }

                    Console.WriteLine("\n✓ WASM module integration successful!");
                    Console.WriteLine("You can now call PDFium functions through the WASM instance.");
                }
                catch (WasmtimeException ex) when (ex.Message.Contains("unknown import"))
                {
                    Console.WriteLine($"\n⚠ Module requires additional imports that aren't defined:");
                    Console.WriteLine($"  {ex.Message}");
                    Console.WriteLine("\nThis means there are still some missing import functions.");
                }
                catch (WasmtimeException ex)
                {
                    Console.WriteLine($"\n⚠ Wasmtime exception: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }

            Console.WriteLine("\nApplication completed.");


            

        }
    }
}
