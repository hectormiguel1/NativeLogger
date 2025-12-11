#ifndef NATIVE_COMMON_SHARED_H
#define NATIVE_COMMON_SHARED_H

/* =======================================================================
 * Platform & Visibility Macros
 * ======================================================================= */
#if defined(_WIN32)
    #ifdef NATIVE_LOGGER_EXPORTS
        #define NATIVE_COMMON __declspec(dllexport)
    #else
        // Match C# [CallConvCdecl]
        #define NATIVE_CDECL __cdecl
        #define NATIVE_COMMON __declspec(dllimport)
    #endif
#else
    #define NATIVE_CDECL
    #define NATIVE_COMMON __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif
    
    typedef enum {
        Ok = 0, 
        Err = 1,
        OkInline = 2
    } Type;
    
    typedef struct {
        char* error_message;
        int error_code;
    } Error;

    typedef union {
        void* data;
        Error* err;
    } ResultUnion;

    typedef struct {
        Type type;
        ResultUnion payload;
    } Result;
    
    /* * Declaration Only. 
     * The implementation is inside the compiled C# DLL. 
     */
    NATIVE_COMMON void free_result(Result result);
    
    
    typedef void (*LogCallback)(const char * msg);
    
    typedef enum
    {
        Finest = 0,
        Fine = 1, 
        Info = 2, 
        Warn = 3, 
        Fatal = 4
    } LogLevel;
    
    NATIVE_COMMON void register_async_callback(LogCallback cb);
    NATIVE_COMMON void register_sync_callback(LogCallback cb);
    NATIVE_COMMON void register_async_callback_with_level(LogCallback cb, LogLevel level);
    NATIVE_COMMON void register_sync_callback_with_level(LogCallback cb, LogLevel level);
    NATIVE_COMMON void free_log_memory(void* ptr);
    NATIVE_COMMON void free_log_memory_batch(void** ptr, int count);

    
#ifdef __cplusplus
}
#endif

#endif