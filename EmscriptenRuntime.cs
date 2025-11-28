using System;
using System.Runtime.InteropServices;
using Wasmtime;

namespace PdfiumWasmIntegration
{
    /// <summary>
    /// Provides Emscripten runtime support for WebAssembly modules
    /// </summary>
    public class EmscriptenRuntime
    {
        private Instance? instance;
        private Store store;
        private Table? indirectFunctionTable;
        private Memory? memory;

        // Exception handling state
        private int lastThrownException = 0;
        private readonly System.Collections.Generic.Dictionary<int, ExceptionInfo> exceptions = new();

        private class ExceptionInfo
        {
            public int ExceptionPtr { get; set; }
            public int TypeInfo { get; set; }
            public int Destructor { get; set; }
        }

        public EmscriptenRuntime(Store store)
        {
            this.store = store;
        }

        public void SetInstance(Instance instance)
        {
            this.instance = instance;
            this.memory = instance.GetMemory("memory");
            this.indirectFunctionTable = instance.GetTable("__indirect_function_table");
        }

        /// <summary>
        /// Define all Emscripten imports on the linker
        /// </summary>
        public void DefineImports(Linker linker)
        {
            DefineInvokeFunctions(linker);
            DefineExceptionFunctions(linker);
            DefineSystemFunctions(linker);
            DefineTimeFunctions(linker);
            DefineSystemCalls(linker);
        }

        private void DefineInvokeFunctions(Linker linker)
        {
            // void invoke(void)
            linker.DefineFunction("env", "invoke_v", (Caller caller, int index) =>
            {
                InvokeIndirect(caller, index);
            });

            // int invoke(void)
            linker.DefineFunction("env", "invoke_i", (Caller caller, int index) =>
            {
                return InvokeIndirect<int>(caller, index);
            });

            // int invoke(int)
            linker.DefineFunction("env", "invoke_ii", (Caller caller, int index, int a1) =>
            {
                return InvokeIndirect<int>(caller, index, a1);
            });

            // int invoke(int, int)
            linker.DefineFunction("env", "invoke_iii", (Caller caller, int index, int a1, int a2) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2);
            });

            // int invoke(int, int, int)
            linker.DefineFunction("env", "invoke_iiii", (Caller caller, int index, int a1, int a2, int a3) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3);
            });

            // int invoke(int, int, int, int)
            linker.DefineFunction("env", "invoke_iiiii", (Caller caller, int index, int a1, int a2, int a3, int a4) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4);
            });

            // int invoke(int, int, int, int, int)
            linker.DefineFunction("env", "invoke_iiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5);
            });

            // int invoke(int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_iiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6);
            });

            // int invoke(int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_iiiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6, a7);
            });

            // int invoke(int, int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_iiiiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6, a7, a8);
            });

            // void invoke(int)
            linker.DefineFunction("env", "invoke_vi", (Caller caller, int index, int a1) =>
            {
                InvokeIndirect(caller, index, a1);
            });

            // void invoke(int, int)
            linker.DefineFunction("env", "invoke_vii", (Caller caller, int index, int a1, int a2) =>
            {
                InvokeIndirect(caller, index, a1, a2);
            });

            // void invoke(int, int, int)
            linker.DefineFunction("env", "invoke_viii", (Caller caller, int index, int a1, int a2, int a3) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3);
            });

            // void invoke(int, int, int, int)
            linker.DefineFunction("env", "invoke_viiii", (Caller caller, int index, int a1, int a2, int a3, int a4) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4);
            });

            // void invoke(int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5);
            });

            // void invoke(int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6);
            });

            // void invoke(int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6, a7);
            });

            // void invoke(int, int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6, a7, a8);
            });

            // void invoke(int, int, int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6, a7, a8, a9);
            });

            // void invoke(int, int, int, int, int, int, int, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_viiiiiiiiiii", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
            });

            // float invoke(int)
            linker.DefineFunction("env", "invoke_fi", (Caller caller, int index, int a1) =>
            {
                return InvokeIndirect<float>(caller, index, a1);
            });

            // float invoke(int, int)
            linker.DefineFunction("env", "invoke_fii", (Caller caller, int index, int a1, int a2) =>
            {
                return InvokeIndirect<float>(caller, index, a1, a2);
            });

            // float invoke(int, int, int)
            linker.DefineFunction("env", "invoke_fiii", (Caller caller, int index, int a1, int a2, int a3) =>
            {
                return InvokeIndirect<float>(caller, index, a1, a2, a3);
            });

            // int invoke(int, float)
            linker.DefineFunction("env", "invoke_iif", (Caller caller, int index, int a1, float a2) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2);
            });

            // int invoke(int, int, float)
            linker.DefineFunction("env", "invoke_iiif", (Caller caller, int index, int a1, int a2, float a3) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3);
            });

            // void invoke(int, float)
            linker.DefineFunction("env", "invoke_vif", (Caller caller, int index, int a1, float a2) =>
            {
                InvokeIndirect(caller, index, a1, a2);
            });

            // void invoke(int, int, float)
            linker.DefineFunction("env", "invoke_viif", (Caller caller, int index, int a1, int a2, float a3) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3);
            });

            // void invoke(int, float, float, float, float)
            linker.DefineFunction("env", "invoke_viffff", (Caller caller, int index, int a1, float a2, float a3, float a4, float a5) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5);
            });

            // int invoke(int, float, int, int, int, int, int)
            linker.DefineFunction("env", "invoke_iifiiiii", (Caller caller, int index, int a1, float a2, int a3, int a4, int a5, int a6, int a7) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6, a7);
            });

            // int invoke(int, int, int, int, int, int, float, int)
            linker.DefineFunction("env", "invoke_iiiiiiifi", (Caller caller, int index, int a1, int a2, int a3, int a4, int a5, int a6, float a7, int a8) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6, a7, a8);
            });

            // long invoke(int)
            linker.DefineFunction("env", "invoke_ji", (Caller caller, int index, int a1) =>
            {
                return InvokeIndirect<long>(caller, index, a1);
            });

            // int invoke(int, long)
            linker.DefineFunction("env", "invoke_iij", (Caller caller, int index, int a1, long a2) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2);
            });

            // void invoke(int, long)
            linker.DefineFunction("env", "invoke_vij", (Caller caller, int index, int a1, long a2) =>
            {
                InvokeIndirect(caller, index, a1, a2);
            });

            // void invoke(long)
            linker.DefineFunction("env", "invoke_vj", (Caller caller, int index, long a1) =>
            {
                InvokeIndirect(caller, index, a1);
            });

            // int invoke(int, int, long)
            linker.DefineFunction("env", "invoke_iiij", (Caller caller, int index, int a1, int a2, long a3) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3);
            });

            // int invoke(int, int, int, int, long) - STANDALONE_WASM version without index
            linker.DefineFunction("env", "invoke_iiiij", (Caller caller, int a1, int a2, int a3, int a4, long a5) =>
            {
                Console.WriteLine($"[Invoke] invoke_iiiij called with params: {a1}, {a2}, {a3}, {a4}, {a5}");
                // TODO: Implement proper indirect call without index
                return 0;
            });

            // int invoke(int, int, long, int, int)
            linker.DefineFunction("env", "invoke_iiijii", (Caller caller, int index, int a1, int a2, long a3, int a4, int a5) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5);
            });

            // int invoke(int, int, int, int, long, int)
            linker.DefineFunction("env", "invoke_iiiiiji", (Caller caller, int index, int a1, int a2, int a3, int a4, long a5, int a6) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6);
            });

            // void invoke(int, long, int)
            linker.DefineFunction("env", "invoke_viji", (Caller caller, int index, int a1, long a2, int a3) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3);
            });

            // void invoke(int, long, int, int)
            linker.DefineFunction("env", "invoke_vijii", (Caller caller, int index, int a1, long a2, int a3, int a4) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4);
            });

            // void invoke(int, int, long, int)
            linker.DefineFunction("env", "invoke_viiji", (Caller caller, int index, int a1, int a2, long a3, int a4) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4);
            });

            // void invoke(int, int, int, long, int)
            linker.DefineFunction("env", "invoke_viiiji", (Caller caller, int index, int a1, int a2, int a3, long a4, int a5) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5);
            });

            // void invoke(int, int, long, int, int)
            linker.DefineFunction("env", "invoke_viijii", (Caller caller, int index, int a1, int a2, long a3, int a4, int a5) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5);
            });

            // void invoke(int, int, long)
            linker.DefineFunction("env", "invoke_viij", (Caller caller, int index, int a1, int a2, long a3) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3);
            });

            // void invoke(int, int, int, long)
            linker.DefineFunction("env", "invoke_viiij", (Caller caller, int index, int a1, int a2, int a3, long a4) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4);
            });

            // void invoke(int, int, int, long, long, int)
            linker.DefineFunction("env", "invoke_viiijji", (Caller caller, int index, int a1, int a2, int a3, long a4, long a5, int a6) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4, a5, a6);
            });

            // long invoke(int, long)
            linker.DefineFunction("env", "invoke_jij", (Caller caller, int index, int a1, long a2) =>
            {
                return InvokeIndirect<long>(caller, index, a1, a2);
            });

            // long invoke(int, long, int)
            linker.DefineFunction("env", "invoke_jiji", (Caller caller, int index, int a1, long a2, int a3) =>
            {
                return InvokeIndirect<long>(caller, index, a1, a2, a3);
            });

            // long invoke(int, long, int, int)
            linker.DefineFunction("env", "invoke_jijii", (Caller caller, int index, int a1, long a2, int a3, int a4) =>
            {
                return InvokeIndirect<long>(caller, index, a1, a2, a3, a4);
            });

            // int invoke(int, double)
            linker.DefineFunction("env", "invoke_iid", (Caller caller, int index, int a1, double a2) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2);
            });

            // void invoke(int, double, int, int)
            linker.DefineFunction("env", "invoke_vidii", (Caller caller, int index, int a1, double a2, int a3, int a4) =>
            {
                InvokeIndirect(caller, index, a1, a2, a3, a4);
            });

            // int invoke(int, int, int, int, long, int, int)
            linker.DefineFunction("env", "invoke_iiiiijii", (Caller caller, int index, int a1, int a2, int a3, int a4, long a5, int a6, int a7) =>
            {
                return InvokeIndirect<int>(caller, index, a1, a2, a3, a4, a5, a6, a7);
            });

            // int invoke(int, int, int, long, int, int, int, int, int, int) - STANDALONE_WASM version
            linker.DefineFunction("env", "invoke_iiijiiiiii", (Caller caller, int a1, int a2, int a3, long a4, int a5, int a6, int a7, int a8, int a9, int a10) =>
            {
                Console.WriteLine($"[Invoke] invoke_iiijiiiiii called (stub)");
                return 0;
            });

            // For functions with 13+ parameters, use untyped callbacks with explicit signatures
            // These are stubs that return 0 since they're rarely used in basic PDF operations

            try
            {
                // invoke_iiiiiiiiiiiii: int(i32×13) - 13 int parameters, returns int
                var params13i = new[] {
                    ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                    ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                    ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                    ValueKind.Int32
                };
                var result1i = new[] { ValueKind.Int32 };

                var func13i = Function.FromCallback(store, (Caller caller, ReadOnlySpan<ValueBox> args, Span<ValueBox> results) =>
                {
                    Console.WriteLine($"[Invoke] invoke_iiiiiiiiiiiii called with {args.Length} args (stub)");
                    results[0] = 0; // Implicit conversion from int to ValueBox
                }, params13i, result1i);
                linker.Define("env", "invoke_iiiiiiiiiiiii", func13i);

                // invoke_iiiiijiiiiii: int(i32×5, i64, i32×6) - 5 int, 1 long, 6 int parameters, returns int
                var params12ij = new[] {
                    ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32,
                    ValueKind.Int32, ValueKind.Int64, ValueKind.Int32, ValueKind.Int32,
                    ValueKind.Int32, ValueKind.Int32, ValueKind.Int32, ValueKind.Int32
                };

                var func12ij = Function.FromCallback(store, (Caller caller, ReadOnlySpan<ValueBox> args, Span<ValueBox> results) =>
                {
                    Console.WriteLine($"[Invoke] invoke_iiiiijiiiiii called with {args.Length} args (stub)");
                    results[0] = 0; // Implicit conversion from int to ValueBox
                }, params12ij, result1i);
                linker.Define("env", "invoke_iiiiijiiiiii", func12ij);

                Console.WriteLine("✓ Defined large-parameter invoke functions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Could not define large-parameter functions: {ex.Message}");
            }
        }

        private void DefineExceptionFunctions(Linker linker)
        {
            // C++ exception handling
            linker.DefineFunction("env", "__cxa_throw", (Caller caller, int thrownException, int tinfo, int dest) =>
            {
                Console.WriteLine($"[Exception] __cxa_throw called: exception={thrownException:X}, tinfo={tinfo:X}");
                lastThrownException = thrownException;
                exceptions[thrownException] = new ExceptionInfo
                {
                    ExceptionPtr = thrownException,
                    TypeInfo = tinfo,
                    Destructor = dest
                };
                // In a real implementation, this would unwind the stack
                throw new WasmtimeException($"C++ exception thrown: {thrownException:X}");
            });

            linker.DefineFunction("env", "__cxa_begin_catch", (Caller caller, int exceptionPtr) =>
            {
                Console.WriteLine($"[Exception] __cxa_begin_catch: {exceptionPtr:X}");
                return exceptionPtr;
            });

            linker.DefineFunction("env", "__cxa_end_catch", (Caller caller) =>
            {
                Console.WriteLine("[Exception] __cxa_end_catch");
            });

            linker.DefineFunction("env", "__cxa_find_matching_catch_2", (Caller caller) =>
            {
                Console.WriteLine("[Exception] __cxa_find_matching_catch_2");
                return lastThrownException;
            });

            linker.DefineFunction("env", "__cxa_find_matching_catch_3", (Caller caller, int a1) =>
            {
                Console.WriteLine($"[Exception] __cxa_find_matching_catch_3: {a1:X}");
                return lastThrownException;
            });

            linker.DefineFunction("env", "__cxa_find_matching_catch_4", (Caller caller, int a1, int a2) =>
            {
                Console.WriteLine($"[Exception] __cxa_find_matching_catch_4: {a1:X}, {a2:X}");
                return lastThrownException;
            });

            linker.DefineFunction("env", "__cxa_find_matching_catch_6", (Caller caller, int a1, int a2, int a3, int a4) =>
            {
                Console.WriteLine($"[Exception] __cxa_find_matching_catch_6");
                return lastThrownException;
            });

            linker.DefineFunction("env", "__resumeException", (Caller caller, int exceptionPtr) =>
            {
                Console.WriteLine($"[Exception] __resumeException: {exceptionPtr:X}");
                throw new WasmtimeException($"Resuming exception: {exceptionPtr:X}");
            });

            linker.DefineFunction("env", "__cxa_rethrow", (Caller caller) =>
            {
                Console.WriteLine("[Exception] __cxa_rethrow");
                throw new WasmtimeException($"Rethrowing exception: {lastThrownException:X}");
            });

            linker.DefineFunction("env", "llvm_eh_typeid_for", (Caller caller, int typeInfo) =>
            {
                Console.WriteLine($"[Exception] llvm_eh_typeid_for: {typeInfo:X}");
                return typeInfo;
            });
        }

        private void DefineSystemFunctions(Linker linker)
        {
            linker.DefineFunction("env", "_abort_js", (Caller caller) =>
            {
                Console.WriteLine("[System] Abort called!");
                throw new WasmtimeException("Program aborted");
            });

            linker.DefineFunction("env", "exit", (Caller caller, int code) =>
            {
                Console.WriteLine($"[System] Exit called with code: {code}");
                throw new WasmtimeException($"Program exited with code: {code}");
            });

            linker.DefineFunction("env", "emscripten_resize_heap", (Caller caller, int size) =>
            {
                Console.WriteLine($"[Memory] Resize heap requested: {size} bytes");
                // Return 0 to indicate failure (can't resize from host side easily)
                return 0;
            });

            linker.DefineFunction("env", "emscripten_notify_memory_growth", (Caller caller, int memoryIndex) =>
            {
                Console.WriteLine($"[Memory] Memory growth notification: index={memoryIndex}");
                // This is just a notification, no action needed
            });

            linker.DefineFunction("env", "_emscripten_throw_longjmp", (Caller caller) =>
            {
                Console.WriteLine("[System] longjmp called");
                throw new WasmtimeException("longjmp called");
            });
        }

        private void DefineTimeFunctions(Linker linker)
        {
            linker.DefineFunction("env", "emscripten_date_now", (Caller caller) =>
            {
                return (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            });

            linker.DefineFunction("env", "_tzset_js", (Caller caller, int timezone, int daylight, int stdName, int dstName) =>
            {
                // Timezone setup - can be stubbed
                Console.WriteLine("[Time] _tzset_js called");
            });

            linker.DefineFunction("env", "_localtime_js", (Caller caller, long time, int tmPtr) =>
            {
                Console.WriteLine($"[Time] _localtime_js called: {time}");
                // Would need to write struct tm to memory at tmPtr
            });

            linker.DefineFunction("env", "_gmtime_js", (Caller caller, long time, int tmPtr) =>
            {
                Console.WriteLine($"[Time] _gmtime_js called: {time}");
                // Would need to write struct tm to memory at tmPtr
            });
        }

        private void DefineSystemCalls(Linker linker)
        {
            linker.DefineFunction("env", "__syscall_openat", (Caller caller, int dirfd, int path, int flags, int mode) =>
            {
                Console.WriteLine($"[Syscall] openat: dirfd={dirfd}, flags={flags:X}");
                return -1; // ENOSYS
            });

            linker.DefineFunction("env", "__syscall_fcntl64", (Caller caller, int fd, int cmd, int arg) =>
            {
                Console.WriteLine($"[Syscall] fcntl64: fd={fd}, cmd={cmd}");
                return 0;
            });

            linker.DefineFunction("env", "__syscall_ioctl", (Caller caller, int fd, int request, int argp) =>
            {
                Console.WriteLine($"[Syscall] ioctl: fd={fd}, request={request:X}");
                return 0;
            });

            linker.DefineFunction("env", "__syscall_fstat64", (Caller caller, int fd, int statbuf) =>
            {
                Console.WriteLine($"[Syscall] fstat64: fd={fd}");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_stat64", (Caller caller, int path, int statbuf) =>
            {
                Console.WriteLine("[Syscall] stat64");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_lstat64", (Caller caller, int path, int statbuf) =>
            {
                Console.WriteLine("[Syscall] lstat64");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_newfstatat", (Caller caller, int dirfd, int path, int statbuf, int flags) =>
            {
                Console.WriteLine("[Syscall] newfstatat");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_ftruncate64", (Caller caller, int fd, long length) =>
            {
                Console.WriteLine($"[Syscall] ftruncate64: fd={fd}, length={length}");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_getdents64", (Caller caller, int fd, int dirp, int count) =>
            {
                Console.WriteLine($"[Syscall] getdents64: fd={fd}");
                return 0;
            });

            linker.DefineFunction("env", "__syscall_unlinkat", (Caller caller, int dirfd, int path, int flags) =>
            {
                Console.WriteLine("[Syscall] unlinkat");
                return -1;
            });

            linker.DefineFunction("env", "__syscall_rmdir", (Caller caller, int path) =>
            {
                Console.WriteLine("[Syscall] rmdir");
                return -1;
            });
        }

        // Helper methods for indirect function calls
        private void InvokeIndirect(Caller caller, int index, params object[] args)
        {
            Console.WriteLine($"[Invoke] Indirect call to index {index} with {args.Length} args");

            if (indirectFunctionTable == null || instance == null)
            {
                Console.WriteLine($"[Invoke] Warning: Indirect function table or instance not available");
                return;
            }

            try
            {
                var funcRef = indirectFunctionTable.GetElement((uint)index);
                if (funcRef is Function func)
                {
                    // Convert object[] to int[] for easier handling
                    // Invoke with individual arguments based on count - convert to int for ValueBox compatibility
                    switch (args.Length)
                    {
                        case 0: func.Invoke(); break;
                        case 1: func.Invoke((int)args[0]); break;
                        case 2: func.Invoke((int)args[0], (int)args[1]); break;
                        case 3: func.Invoke((int)args[0], (int)args[1], (int)args[2]); break;
                        case 4: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3]); break;
                        case 5: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]); break;
                        case 6: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5]); break;
                        case 7: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6]); break;
                        case 8: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7]); break;
                        case 9: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7], (int)args[8]); break;
                        case 10: func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7], (int)args[8], (int)args[9]); break;
                        default: Console.WriteLine($"[Invoke] Unsupported argument count: {args.Length}"); break;
                    }
                }
                else
                {
                    Console.WriteLine($"[Invoke] Warning: Element at index {index} is not a function");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Invoke] Error calling indirect function at index {index}: {ex.Message}");
            }
        }

        private T InvokeIndirect<T>(Caller caller, int index, params object[] args)
        {
            Console.WriteLine($"[Invoke] Indirect call to index {index} with {args.Length} args, expecting {typeof(T).Name}");

            if (indirectFunctionTable == null || instance == null)
            {
                Console.WriteLine($"[Invoke] Warning: Indirect function table or instance not available");
                return default(T)!;
            }

            try
            {
                var funcRef = indirectFunctionTable.GetElement((uint)index);
                if (funcRef is Function func)
                {
                    object? result = null;

                    // Invoke with individual arguments based on count - convert to int for ValueBox compatibility
                    switch (args.Length)
                    {
                        case 0: result = func.Invoke(); break;
                        case 1: result = func.Invoke((int)args[0]); break;
                        case 2: result = func.Invoke((int)args[0], (int)args[1]); break;
                        case 3: result = func.Invoke((int)args[0], (int)args[1], (int)args[2]); break;
                        case 4: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3]); break;
                        case 5: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]); break;
                        case 6: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5]); break;
                        case 7: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6]); break;
                        case 8: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7]); break;
                        case 9: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7], (int)args[8]); break;
                        case 10: result = func.Invoke((int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7], (int)args[8], (int)args[9]); break;
                        default: Console.WriteLine($"[Invoke] Unsupported argument count: {args.Length}"); break;
                    }

                    if (result != null)
                    {
                        // Convert result to expected type
                        if (typeof(T) == typeof(int) && result is int intResult)
                            return (T)(object)intResult;
                        if (typeof(T) == typeof(long) && result is long longResult)
                            return (T)(object)longResult;
                        if (typeof(T) == typeof(float) && result is float floatResult)
                            return (T)(object)floatResult;
                        if (typeof(T) == typeof(double) && result is double doubleResult)
                            return (T)(object)doubleResult;

                        // Try direct cast
                        return (T)result;
                    }
                }
                else
                {
                    Console.WriteLine($"[Invoke] Warning: Element at index {index} is not a function");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Invoke] Error calling indirect function at index {index}: {ex.Message}");
            }

            return default(T)!;
        }
    }
}
