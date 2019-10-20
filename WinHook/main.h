#ifndef MAIN_H_INCLUDED
#define MAIN_H_INCLUDED

//#ifdef WINHOOK_EXPORTS
#define WINHOOK_API __declspec(dllexport)
//#else
//#define WINHOOK_API __declspec(dllimport)
//#endif

extern WINHOOK_API BOOL WINHOOK_API RemoveHook();
extern WINHOOK_API BOOL WINHOOK_API InstallHook(DWORD hWnd, int disableWinKey);

#endif // MAIN_H_INCLUDED
