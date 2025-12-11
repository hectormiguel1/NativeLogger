#ifndef NATIVE_LOGGER_SHARED_H
#define NATIVE_LOGGER_SHARED_H

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
        Error = 1
    } ResultType;
    
    typedef struct {
        char* error_message;
        int error_code;
    } Error;

    // Explicit layout union
    typedef union {
        void* data;
        Error* err;
    } ResultUnion;

    typedef struct {
        ResultType type;
        ResultUnion payload;
    } Result;
    
    // Function to free the result memory
    NATIVE_COMMON void free_result(Result result);
    
    
    typedef void (*LogCallback)(const char * msg);
    
    typedef enum
    {
        Debug = 0, 
        Info = 1, 
        Warn = 2, 
        Error = 3
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