using System.Runtime.InteropServices;
using System.Text;

namespace Native.Common;

public static unsafe class NativeResult
{
    // --- 1. Common Structures ---

    [StructLayout(LayoutKind.Sequential)]
    public struct Error
    {
        public byte* ErrorMessage; // Allocated on native heap
        public int ErrorCode;
    }

    public enum ResultType : int
    {
        Ok = 0,
        Error = 1,
    }

    // --- 2. Generic Definitions ---

    // The Union: Overlaps a pointer to T with a pointer to Error
    [StructLayout(LayoutKind.Explicit)]
    public struct ResultUnion<T> where T : unmanaged
    {
        [FieldOffset(0)] 
        public T* Data; // Generic Pointer (C sees this as void*)

        [FieldOffset(0)] 
        public Error* Err;
    }

    // The Result: Contains the Type and the Generic Union
    [StructLayout(LayoutKind.Sequential)]
    public struct Result<T> where T : unmanaged
    {
        public ResultType Type;
        public ResultUnion<T> Payload;
    }

    // --- 3. Generic Helper Methods ---

    /// <summary>
    /// Creates a Success Result. Allocates memory for 'value' on the native heap.
    /// </summary>
    public static Result<T> CreateSuccess<T>(T value) where T : unmanaged
    {
        // 1. Allocate memory for T on the native heap
        var ptr = (T*)NativeMemory.Alloc((nuint)sizeof(T));
        
        // 2. Copy the struct value into that memory
        *ptr = value;

        // 3. Return the generic result
        return new Result<T>
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion<T> { Data = ptr }
        };
    }

    /// <summary>
    /// Creates an Error Result. Allocates memory for the Error and the Message.
    /// </summary>
    public static Result<T> CreateError<T>(string message, int code) where T : unmanaged
    {
        // 1. Convert string to UTF-8 bytes
        var byteCount = Encoding.UTF8.GetByteCount(message);
        
        // 2. Allocate memory for the string (Bytes + 1 for null terminator)
        var msgPtr = (byte*)NativeMemory.Alloc((nuint)(byteCount + 1));
        
        // 3. Copy bytes and add null terminator
        fixed (char* strPtr = message)
        {
            Encoding.UTF8.GetBytes(strPtr, message.Length, msgPtr, byteCount);
        }
        msgPtr[byteCount] = 0; // Null terminate

        // 4. Allocate memory for the Error struct
        var errPtr = (Error*)NativeMemory.Alloc((nuint)sizeof(Error));
        errPtr->ErrorMessage = msgPtr;
        errPtr->ErrorCode = code;

        // 5. Return the result (Data pointer is unused/overlapped)
        return new Result<T>
        {
            Type = ResultType.Error,
            Payload = new ResultUnion<T> { Err = errPtr }
        };
    }
}