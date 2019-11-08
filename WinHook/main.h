#ifndef MAIN_H_INCLUDED
#define MAIN_H_INCLUDED

//#ifdef WINHOOK_EXPORTS
#define WINHOOK_API __declspec(dllexport)
//#else
//#define WINHOOK_API __declspec(dllimport)
//#endif

// Check windows
#if _WIN32 || _WIN64
   #if _WIN64
     #define ENV64
  #else
    #define ENV32
  #endif
#endif

// Check GCC
#if __GNUC__
  #if __x86_64__ || __ppc64__
    #define ENV64
  #else
    #define ENV32
  #endif
#endif


#ifdef ENV32
    #define CINT long
#else
    #define CINT long long
#endif


extern WINHOOK_API BOOL WINHOOK_API RemoveHook();
extern WINHOOK_API BOOL WINHOOK_API InstallHook(DWORD hWnd, int disableWinKey, CINT pinpointHandler);

#endif // MAIN_H_INCLUDED
