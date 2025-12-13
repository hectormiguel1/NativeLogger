using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Native.Common;

public static unsafe class NativeResult
{
    // ==========================================================
    // 1. Common Structures
    // ==========================================================

    [StructLayout(LayoutKind.Sequential)]
    public struct Error
    {
        public byte* ErrorMessage;
        public int ErrorCode;
    }

    public enum ResultType : int
    {
        Ok = 0,         // Success with Heap Pointer (Must Free)
        Error = 1,      // Error with Heap Pointer (Must Free)
        OkInline = 2    // Success with Inline Value (DO NOT FREE)
    }

    // ==========================================================
    // 2. Generic Definitions
    // ==========================================================

    [StructLayout(LayoutKind.Explicit)]
    public struct ResultUnion
    {
        [FieldOffset(0)] public void* Data; 
        [FieldOffset(0)] public Error* Err;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Result<T> where T : unmanaged
    {
        public ResultType Type;
        public ResultUnion Payload;
    }

    // ==========================================================
    // 3. Result Creation Helpers
    // ==========================================================

    /// <summary>
    /// Creates a Heap-Allocated Success Result.
    /// Use this for large structs (FileEntryList, Config, etc.).
    /// Caller MUST free using 'free_result'.
    /// </summary>
    public static Result<T> CreateSuccess<T>(T value) where T : unmanaged
    {
        var ptr = (T*) NativeMemory.Alloc((nuint)sizeof(T));
        *ptr = value;

        return new Result<T>
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion { Data = ptr }
        };
    }
    
    /// <summary>
    /// Creates a Heap-Allocated Success Result.
    /// Use this for instances when the success already has a pointer to data. 
    /// Caller MUST free using 'free_result'.
    /// </summary>
    public static Result<T> CreateSuccess<T>(T* value) where T : unmanaged
    {
        return new Result<T>
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion { Data = value }
        };
    }


    /// <summary>
    /// Creates an INLINE Success Result.
    /// Stores the value directly inside the pointer address.
    /// NO allocation, NO free needed. 
    /// Only works if sizeof(T) <= 8 bytes (int, bool, enums).
    /// </summary>
    public static Result<T> CreateInlineSuccess<T>(T value) where T : unmanaged
    {
        if (sizeof(T) > sizeof(void*))
        {
            // Fallback to heap allocation if T is too big, or throw.
            // Throwing is better to catch dev errors.
            throw new ArgumentException($"Type {typeof(T)} is too large ({sizeof(T)} bytes) to inline in a pointer.");
        }

        var union = new ResultUnion();
        
        // MAGIC: Write the value directly into the memory slot of the 'Data' pointer.
        // We cast the address of the Data field (&union.Data) to a T pointer.
        *(T*)&union.Data = value;

        return new Result<T>
        {
            Type = ResultType.OkInline, // Signals "Do Not Free"
            Payload = union
        };
    }

    /// <summary>
    /// Creates a "Void" Success Result (Data = NULL).
    /// </summary>
    public static Result<int> CreateSuccess() 
    {
        return new Result<int>
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion { Data = null }
        };
    }

    /// <summary>
    /// Creates an Error Result.
    /// </summary>
    public static Result<T> CreateError<T>(string message, int code) where T : unmanaged
    {
        var byteCount = Encoding.UTF8.GetByteCount(message);
        var msgPtr = (byte*)NativeMemory.Alloc((nuint)(byteCount + 1));
        
        fixed (char* strPtr = message)
        {
            Encoding.UTF8.GetBytes(strPtr, message.Length, msgPtr, byteCount);
        }
        msgPtr[byteCount] = 0; 

        var errPtr = (Error*)NativeMemory.Alloc((nuint)sizeof(Error));
        errPtr->ErrorMessage = msgPtr;
        errPtr->ErrorCode = code;

        return new Result<T>
        {
            Type = ResultType.Error,
            Payload = new ResultUnion { Err = errPtr }
        };
    }
    
    // ==========================================================
    // 5. Exports (The "Destructor")
    // ==========================================================

    /// <summary>
    /// Frees the result memory. 
    /// Exposed to C/Dart as "free_result".
    /// We use Result as a placeholder because the binary layout 
    /// is identical for all generic Results.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "free_result")]
    public static void FreeResult(Result<int> result)
    {
        switch (result.Type)
        {
            // CASE 1: Inline Value -> DO NOTHING
            // The data is a value (like 123), not a pointer.
            case ResultType.OkInline:
                return;
            // CASE 2: Error -> Free the Error Struct AND the Message
            case ResultType.Error:
            {
                if (result.Payload.Err == null) return;
                // Free the inner message string first
                if (result.Payload.Err->ErrorMessage != null)
                {
                    NativeMemory.Free(result.Payload.Err->ErrorMessage);
                }
                // Free the Error struct itself
                NativeMemory.Free(result.Payload.Err);
                return;
            }
            // CASE 3: Standard Success -> Free the Data Pointer
            // Checks for NULL (Void results) automatically before freeing
            case ResultType.Ok:
            {
                if (result.Payload.Data != null)
                {
                    NativeMemory.Free(result.Payload.Data);
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    
    // ==========================================================
    // 4. Utilities
    // ==========================================================
    public static string StringFromPtr(byte* ptr)
    {
        return ptr == null ? string.Empty : new string((sbyte*)ptr);
    }
}