using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Native.Logging;

public static class LogRouter
{
    
    public static Action<string>? GlobalCallback { get; private set; } = Console.WriteLine;
    public static LogLevel Level { get; set; } = LogLevel.Fine;
    
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
            GlobalCallback = null;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<SyncLoggerCallback>(callbackPtr);

        GlobalCallback = msg=> nativeCallback(msg);
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
            GlobalCallback = null;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<AsyncLoggerCallback>(callbackPtr);

        GlobalCallback = (msg) =>
        {
            // ALLOCATE: Create a UTF-8 copy on the Heap (Unmanaged Memory).
            // This memory persists until explicitly freed.
            var ptr = Marshal.StringToCoTaskMemUTF8(msg);

            // CALL: Pass the pointer to Caller. 
            // Since it's heap memory, it's safe even if Caller processes it asynchronously.
            nativeCallback(ptr);
        };
    }
    
    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void register_sync_callback(void (*callback)(const char*));
    /// Registers a callback into native code. The callback is assumed synchronous.
    /// Data will automatically be GC'ed when native callback returns
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "register_sync_callback_with_level", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetLoggingCallbackWithLevel(IntPtr callbackPtr, int  logLevelRaw)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            GlobalCallback = null;
            return;
        }
        
        Level = (LogLevel)logLevelRaw;
        
        var nativeCallback = Marshal.GetDelegateForFunctionPointer<SyncLoggerCallback>(callbackPtr);

        GlobalCallback = msg=> nativeCallback(msg);
    }
    
    
    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void register_async_callback(void (*callback)(const char*));
    /// Used when interoperating with async languages therefor ownership fo freeing the memory of the log string
    /// is owned by the Caller.
    /// Caller is responsible for calling free_log_memory with pointer
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "register_async_callback_with_level", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetAsyncLoggingCallbackWIthLevel(IntPtr callbackPtr, int logLevelRaw)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            GlobalCallback = null;
            return;
        }
        Level =  (LogLevel)logLevelRaw;
        
        var nativeCallback = Marshal.GetDelegateForFunctionPointer<AsyncLoggerCallback>(callbackPtr);

        GlobalCallback = (msg) =>
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
    
    // 3. The Cleanup Function (CRITICAL NEW EXPORT)
    // Caller must call this after it reads the string.
    [UnmanagedCallersOnly(EntryPoint = "free_log_memory_batch", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FreeLogMemoryBatch(IntPtr* pointers, int cnt)
    {
        for (var idx = 0; idx < cnt; idx++)
        {
            if (pointers[idx] != IntPtr.Zero)
            {   
                Marshal.FreeCoTaskMem(pointers[idx]);
            }
        }
    }


}