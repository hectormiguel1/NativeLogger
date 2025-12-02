using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Native;

public static class LoggerExports
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AsyncLoggerCallback(IntPtr msgPtr);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SyncLoggerCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string msg);
    
    
    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void register_sync_callback(void (*callback)(const char*));
    /// Registers a callback into native code. The callback is assumed synchronous.
    /// Data will automatically be GC'ed when native callback returns
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "register_sync_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetLoggingCallback(IntPtr callbackPtr)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            NativeLogger.LoggingCallback = Console.WriteLine;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<SyncLoggerCallback>(callbackPtr);

        NativeLogger.LoggingCallback = msg=> nativeCallback(msg);
    }
    
    
    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void register_async_callback(void (*callback)(const char*));
    /// Used when interoperating with async languages therefor ownership fo freeing the memory of the log string
    /// is owned by the Caller.
    /// Caller is responsible for calling free_log_memory with pointer
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "register_async_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetAsyncLoggingCallback(IntPtr callbackPtr)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            NativeLogger.LoggingCallback = Console.WriteLine;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<AsyncLoggerCallback>(callbackPtr);

        NativeLogger.LoggingCallback = (msg) =>
        {
            // ALLOCATE: Create a UTF-8 copy on the Heap (Unmanaged Memory).
            // This memory persists until explicitly freed.
            var ptr = Marshal.StringToCoTaskMemUTF8(msg);

            // CALL: Pass the pointer to Caller. 
            // Since it's heap memory, it's safe even if Caller processes it asynchronously.
            nativeCallback(ptr);
        };
    }
    
    // 3. The Cleanup Function (CRITICAL NEW EXPORT)
    // Caller must call this after it reads the string.
    [UnmanagedCallersOnly(EntryPoint = "free_log_memory", CallConvs = [typeof(CallConvCdecl)])]
    public static void FreeLogMemory(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}