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

public static class ResultHelpers
{
    public static unsafe Result CreateError(string message, int code)
    {
        var errPtr = (Error*)Marshal.AllocCoTaskMem(sizeof(Error));
        errPtr->ErrorCode = code;
        errPtr->ErrorMessage = (byte*)Marshal.StringToCoTaskMemUTF8(message);

        return new Result
        {
            Type = ResultType.Error,
            Payload = new ResultUnion { Err = errPtr }
        };
    }

    public static unsafe Result CreateSuccess()
    {
        return new Result
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion { Data = null }
        };
    }

    public static unsafe Result CreateSuccess(FileEntryList list)
    {
        var listPtr = (FileEntryList*)Marshal.AllocCoTaskMem(sizeof(FileEntryList));
        *listPtr = list;

        return new Result
        {
            Type = ResultType.Ok,
            Payload = new ResultUnion { Data = listPtr }
        };
    }

    [UnmanagedCallersOnly(EntryPoint = "free_result", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FreeResult(Result result)
    {
        if (result.Type == ResultType.Error)
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
                var sizeOfEntry = Marshal.SizeOf<FileEntry>();
                for (var i = 0; i < listPtr->Count; i++)
                {
                    var currentPos = (IntPtr)(listPtr->Items + i);
                    var entry = Marshal.PtrToStructure<FileEntry>(currentPos);
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
}