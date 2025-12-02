#ifndef NATIVE_LOGGER_SHARED_H
#define NATIVE_LOGGER_SHARED_H

/* =======================================================================
 * Platform & Visibility Macros
 * ======================================================================= */
#if defined(_WIN32)
    #ifdef NATIVE_LOGGER_EXPORTS
        #define NATIVE_LOGGER_API __declspec(dllexport)
    #else
        // Match C# [CallConvCdecl]
        #define NATIVE_CDECL __cdecl
        #define NATIVE_LOGGER_API __declspec(dllimport)
    #endif
#else
    #define NATIVE_CDECL
    #define NATIVE_LOGGER_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif
    
    typedef void (*LogCallback)(const char * msg);
    
    NATIVE_LOGGER_API void register_async_callback(LogCallback cb);
    NATIVE_LOGGER_API void register_sync_callback(LogCallback cb);
    NATIVE_LOGGER_API void free_log_memory(void* ptr);

    
#ifdef __cplusplus
}
#endif

#endif