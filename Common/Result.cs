using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Native.Common;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Error
{
    public byte* ErrorMessage;
    public int ErrorCode;
}

public enum ResultType : int
{
    Ok = 0, 
    Error = 1,
}


[StructLayout(LayoutKind.Explicit)]
public unsafe struct ResultUnion
{
    [FieldOffset(0)] public FileEntryList* Data;
    [FieldOffset(0)] public Error* Err;
}

[StructLayout(LayoutKind.Sequential)]
public struct Result
{
    public ResultType Type;
    public ResultUnion Payload;
}


public static unsafe NativeStructs.Result CreateError(string message, int code)
{
    var errPtr = (NativeStructs.Error*)Marshal.AllocCoTaskMem(sizeof(NativeStructs.Error));
    errPtr->ErrorCode = code;
    errPtr->ErrorMessage = (byte*)Marshal.StringToCoTaskMemUTF8(message);

    return new NativeStructs.Result
    {
        Type = NativeStructs.ResultType.Error,
        Payload = new NativeStructs.ResultUnion { Err = errPtr }
    };
}

public static unsafe NativeStructs.Result CreateSuccess()
{
    return new NativeStructs.Result
    {
        Type = NativeStructs.ResultType.Ok,
        Payload = new NativeStructs.ResultUnion { Data = null }
    };
}

public static unsafe NativeStructs.Result CreateSuccess(NativeStructs.FileEntryList list)
{
    var listPtr = (NativeStructs.FileEntryList*)Marshal.AllocCoTaskMem(sizeof(NativeStructs.FileEntryList));
    *listPtr = list;

    return new NativeStructs.Result
    {
        Type = NativeStructs.ResultType.Ok,
        Payload = new NativeStructs.ResultUnion { Data = listPtr }
    };
}

[UnmanagedCallersOnly(EntryPoint = "free_result", CallConvs = [typeof(CallConvCdecl)])]
public static unsafe void FreeResult(NativeStructs.Result result)
{
    if (result.Type == NativeStructs.ResultType.Error)
    {
        if (result.Payload.Err == null) return;
        if (result.Payload.Err->ErrorMessage != null)
        {
            Marshal.FreeCoTaskMem((IntPtr)result.Payload.Err->ErrorMessage);
        }
        Marshal.FreeCoTaskMem((IntPtr)result.Payload.Err);
    }
    else
    {
        // Success
        if (result.Payload.Data == null) return;
        var listPtr = result.Payload.Data;
        if (listPtr->Items != null)
        {
            var sizeOfEntry = Marshal.SizeOf<NativeStructs.FileEntry>();
            for (var i = 0; i < listPtr->Count; i++)
            {
                var currentPos = (IntPtr)(listPtr->Items + i);
                var entry = Marshal.PtrToStructure<NativeStructs.FileEntry>(currentPos);
                if (entry.FilePath != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)entry.FilePath);
                }
            }
            Marshal.FreeCoTaskMem((IntPtr)listPtr->Items);
        }
        // Free the FileEntryList struct itself
        Marshal.FreeCoTaskMem((IntPtr)listPtr);
    }
}