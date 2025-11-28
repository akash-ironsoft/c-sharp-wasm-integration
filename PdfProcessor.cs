using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Wasmtime;

namespace PdfiumWasmIntegration
{
    /// <summary>
    /// Provides PDF processing capabilities using Pdfium WASM module
    /// </summary>
    public class PdfProcessor
    {
        private readonly Store store;
        private readonly Instance instance;
        private readonly Memory? memory;

        // Pdfium core functions
        private readonly Function? loadMemDocument;
        private readonly Function? getPageCount;
        private readonly Function? loadPage;
        private readonly Function? closePage;
        private readonly Function? closeDocument;
        private readonly Function? getLastError;

        // Text extraction functions
        private readonly Function? textLoadPage;
        private readonly Function? textClosePage;
        private readonly Function? textCountChars;
        private readonly Function? textGetText;

        // QPDF functions
        private readonly Function? qpdfPdfToJson;
        private readonly Function? qpdfFreeString;

        // Memory management
        private readonly Function? malloc;
        private readonly Function? free;

        public PdfProcessor(Store store, Instance instance)
        {
            this.store = store;
            this.instance = instance;
            this.memory = instance.GetMemory("memory");

            // Get Pdfium core functions
            loadMemDocument = instance.GetFunction("FPDF_LoadMemDocument");
            getPageCount = instance.GetFunction("FPDF_GetPageCount");
            loadPage = instance.GetFunction("FPDF_LoadPage");
            closePage = instance.GetFunction("FPDF_ClosePage");
            closeDocument = instance.GetFunction("FPDF_CloseDocument");
            getLastError = instance.GetFunction("FPDF_GetLastError");

            // Get text extraction functions
            textLoadPage = instance.GetFunction("FPDFText_LoadPage");
            textClosePage = instance.GetFunction("FPDFText_ClosePage");
            textCountChars = instance.GetFunction("FPDFText_CountChars");
            textGetText = instance.GetFunction("FPDFText_GetText");

            // Get QPDF functions
            qpdfPdfToJson = instance.GetFunction("IPDF_QPDF_PDFToJSON");
            qpdfFreeString = instance.GetFunction("IPDF_QPDF_FreeString");

            // Get memory management functions
            malloc = instance.GetFunction("malloc");
            free = instance.GetFunction("free");
        }

        /// <summary>
        /// Represents a single page of extracted text
        /// </summary>
        public class PageText
        {
            public int PageNumber { get; set; }
            public string Text { get; set; } = string.Empty;
            public int CharacterCount { get; set; }
            public string? Error { get; set; }
        }

        /// <summary>
        /// Result of text extraction
        /// </summary>
        public class TextExtractionResult
        {
            public bool Success { get; set; }
            public int PageCount { get; set; }
            public List<PageText> Pages { get; set; } = new();
            public string FullText => string.Join("\n\n", Pages.Select(p => p.Text));
            public int TotalCharacters => Pages.Sum(p => p.CharacterCount);
        }

        /// <summary>
        /// Extract text from PDF using Pdfium
        /// </summary>
        /// <param name="pdfFilePath">Path to the PDF file</param>
        /// <returns>Text extraction result</returns>
        public TextExtractionResult ExtractText(string pdfFilePath)
        {
            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException($"PDF file not found: {pdfFilePath}");
            }

            byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);
            return ExtractText(pdfBytes);
        }

        /// <summary>
        /// Extract text from PDF bytes using Pdfium
        /// </summary>
        /// <param name="pdfBytes">PDF file bytes</param>
        /// <returns>Text extraction result</returns>
        public TextExtractionResult ExtractText(byte[] pdfBytes)
        {
            if (memory == null)
            {
                throw new InvalidOperationException("WASM memory not available");
            }

            var result = new TextExtractionResult();

            try
            {
                // Allocate WASM memory for PDF data
                int wasmBufferPtr = AllocateMemory(pdfBytes.Length);
                WriteToMemory(wasmBufferPtr, pdfBytes);

                // Load PDF document
                var docPtrResult = loadMemDocument?.Invoke(wasmBufferPtr, pdfBytes.Length, 0);
                int docPtr = docPtrResult != null ? (int)docPtrResult : 0;

                if (docPtr == 0)
                {
                    var errorResult = getLastError?.Invoke();
                    int errorCode = errorResult != null ? (int)errorResult : 1;
                    FreeMemory(wasmBufferPtr);
                    throw new Exception($"Failed to load PDF: {GetErrorMessage(errorCode)}");
                }

                // Get page count
                var pageCountResult = getPageCount?.Invoke(docPtr);
                int pageCount = pageCountResult != null ? (int)pageCountResult : 0;
                result.PageCount = pageCount;

                Console.WriteLine($"PDF has {pageCount} pages");

                // Extract text from each page
                for (int i = 0; i < pageCount; i++)
                {
                    var pageText = ExtractPageText((int)docPtr, i);
                    result.Pages.Add(pageText);
                }

                // Cleanup
                closeDocument?.Invoke(docPtr);
                FreeMemory(wasmBufferPtr);

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting text: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extract text from a single page
        /// </summary>
        private PageText ExtractPageText(int docPtr, int pageIndex)
        {
            var pageText = new PageText
            {
                PageNumber = pageIndex + 1
            };

            try
            {
                // Load page
                var pagePtrResult = loadPage?.Invoke(docPtr, pageIndex);
                int pagePtr = pagePtrResult != null ? (int)pagePtrResult : 0;

                if (pagePtr == 0)
                {
                    pageText.Error = "Failed to load page";
                    return pageText;
                }

                // Load text page
                var textPagePtrResult = textLoadPage?.Invoke(pagePtr);
                int textPagePtr = textPagePtrResult != null ? (int)textPagePtrResult : 0;

                if (textPagePtr == 0)
                {
                    closePage?.Invoke(pagePtr);
                    pageText.Error = "Failed to load text";
                    return pageText;
                }

                // Count characters
                var charCountResult = textCountChars?.Invoke(textPagePtr);
                int charCount = charCountResult != null ? (int)charCountResult : 0;
                pageText.CharacterCount = charCount;

                if (charCount > 0)
                {
                    // Allocate buffer for UTF-16 text (2 bytes per char + null terminator)
                    int bufferSize = (charCount + 1) * 2;
                    int textBufferPtr = AllocateMemory(bufferSize);

                    // Get text
                    var extractedCharsResult = textGetText?.Invoke(textPagePtr, 0, charCount, textBufferPtr);
                    int extractedChars = extractedCharsResult != null ? (int)extractedCharsResult : 0;

                    if (extractedChars > 0)
                    {
                        // Read UTF-16 data from WASM memory
                        byte[] utf16Bytes = ReadFromMemory(textBufferPtr, extractedChars * 2);

                        // Convert UTF-16LE to string
                        pageText.Text = Encoding.Unicode.GetString(utf16Bytes)
                            .Replace("\0", "")  // Remove null characters
                            .Trim();
                    }

                    FreeMemory(textBufferPtr);
                }

                // Cleanup
                textClosePage?.Invoke(textPagePtr);
                closePage?.Invoke(pagePtr);

                return pageText;
            }
            catch (Exception ex)
            {
                pageText.Error = $"Exception: {ex.Message}";
                return pageText;
            }
        }

        /// <summary>
        /// Convert PDF to JSON using QPDF
        /// </summary>
        /// <param name="pdfFilePath">Path to the PDF file</param>
        /// <param name="version">QPDF JSON version (default: 2)</param>
        /// <returns>JSON object</returns>
        public JsonDocument ConvertToJson(string pdfFilePath, int version = 2)
        {
            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException($"PDF file not found: {pdfFilePath}");
            }

            byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);
            return ConvertToJson(pdfBytes, version);
        }

        /// <summary>
        /// Convert PDF bytes to JSON using QPDF
        /// </summary>
        /// <param name="pdfBytes">PDF file bytes</param>
        /// <param name="version">QPDF JSON version (default: 2)</param>
        /// <returns>JSON object</returns>
        public JsonDocument ConvertToJson(byte[] pdfBytes, int version = 2)
        {
            if (memory == null)
            {
                throw new InvalidOperationException("WASM memory not available");
            }

            if (qpdfPdfToJson == null)
            {
                throw new InvalidOperationException("QPDF function not available in WASM module");
            }

            try
            {
                // Allocate WASM memory for PDF data
                int wasmBufferPtr = AllocateMemory(pdfBytes.Length);
                WriteToMemory(wasmBufferPtr, pdfBytes);

                // Call QPDF function
                var jsonPtrResult = qpdfPdfToJson.Invoke(wasmBufferPtr, pdfBytes.Length, version);
                int jsonPtr = jsonPtrResult != null ? (int)jsonPtrResult : 0;

                // Free input buffer
                FreeMemory(wasmBufferPtr);

                if (jsonPtr == 0)
                {
                    var errorResult = getLastError?.Invoke();
                    int errorCode = errorResult != null ? (int)errorResult : 9;
                    throw new Exception($"QPDF failed to convert PDF to JSON: {GetErrorMessage(errorCode)}");
                }

                // Read JSON string from WASM memory
                string jsonString = ReadStringFromMemory(jsonPtr);

                // Free JSON string
                qpdfFreeString?.Invoke(jsonPtr);

                // Parse and return JSON
                return JsonDocument.Parse(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting PDF to JSON: {ex.Message}");
                throw;
            }
        }

        #region Memory Management

        /// <summary>
        /// Allocate WASM memory
        /// </summary>
        private int AllocateMemory(int size)
        {
            if (malloc == null)
            {
                throw new InvalidOperationException("malloc function not available");
            }

            var ptrResult = malloc.Invoke(size);
            return ptrResult != null ? (int)ptrResult : 0;
        }

        /// <summary>
        /// Free WASM memory
        /// </summary>
        private void FreeMemory(int ptr)
        {
            if (free != null && ptr != 0)
            {
                free.Invoke(ptr);
            }
        }

        /// <summary>
        /// Write bytes to WASM memory
        /// </summary>
        private void WriteToMemory(int ptr, byte[] data)
        {
            if (memory == null) return;

            var memorySpan = memory.GetSpan();
            data.CopyTo(memorySpan.Slice(ptr, data.Length));
        }

        /// <summary>
        /// Read bytes from WASM memory
        /// </summary>
        private byte[] ReadFromMemory(int ptr, int length)
        {
            if (memory == null)
            {
                return Array.Empty<byte>();
            }

            var memorySpan = memory.GetSpan();
            return memorySpan.Slice(ptr, length).ToArray();
        }

        /// <summary>
        /// Read null-terminated UTF-8 string from WASM memory
        /// </summary>
        private string ReadStringFromMemory(int ptr)
        {
            if (memory == null || ptr == 0)
            {
                return string.Empty;
            }

            var memorySpan = memory.GetSpan();

            // Find null terminator
            int length = 0;
            while (ptr + length < memorySpan.Length && memorySpan[ptr + length] != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            // Read and decode UTF-8 string
            byte[] stringBytes = memorySpan.Slice(ptr, length).ToArray();
            return Encoding.UTF8.GetString(stringBytes);
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Get error message from Pdfium error code
        /// </summary>
        private string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                0 => "Success",
                1 => "Unknown error",
                2 => "File not found or could not be opened",
                3 => "File not in PDF format or corrupted",
                4 => "Password required or incorrect password",
                5 => "Unsupported security scheme",
                6 => "Page not found or content error",
                9 => "QPDF error",
                _ => $"Unknown error code: {errorCode}"
            };
        }

        #endregion
    }
}
