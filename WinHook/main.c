// Good information about keycodes: https://docs.microsoft.com/sv-se/windows/win32/inputdev/virtual-key-codes
// Goo dinformation about low level keyboard proc: https://docs.microsoft.com/sv-se/windows/win32/api/winuser/ns-winuser-kbdllhookstruct

#include <windows.h>
#include <stdio.h>
#include "main.h"

/*
 It is important to put this functions in shared data segment or else we will have problem using a
 single version of them in TileWindow program.
*/
#pragma data_seg(".shared")
HANDLE g_hInstance __attribute__((section(".shared"), shared)) = NULL;
HHOOK g_hook __attribute__((section(".shared"), shared)) = NULL;
HHOOK g_hookKeyb __attribute__((section(".shared"), shared)) = NULL;
UINT WMC_SHOW __attribute__((section(".shared"), shared)) = 0;
UINT WMC_CREATE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_ENTERMOVE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_MOVE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_EXITMOVE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_KEYDOWN __attribute__((section(".shared"), shared)) = 0;
UINT WMC_KEYUP __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SETFOCUS __attribute__((section(".shared"), shared)) = 0;
UINT WMC_KILLFOCUS __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SHOWWINDOW __attribute__((section(".shared"), shared)) = 0;
UINT WMC_DESTROY __attribute__((section(".shared"), shared)) = 0;
UINT WMC_STYLECHANGED __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SCCLOSE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SCMAXIMIZE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SCMINIMIZE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SCRESTORE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_ACTIVATEAPP __attribute__((section(".shared"), shared)) = 0;
UINT WMC_DISPLAYCHANGE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_SIZE __attribute__((section(".shared"), shared)) = 0;
UINT WMC_EXTRATRACK __attribute__((section(".shared"), shared)) = 0;
DWORD gThread __attribute__((section(".shared"), shared)) = 0;
HWND g_pinpointHandler __attribute__((section(".shared"), shared)) = 0;
#pragma data_seg()
#pragma comment(linker, "/SECTION:.shared,RWS")

int g_disableWinKey;
int g_winPressed = 0;

/*
    Main entry point
    Setup custom messages so we can communicate back to our "host"
*/
BOOL APIENTRY DllMain(HANDLE hInstance, DWORD fdwReason, LPVOID lpvReserved)
{
    switch(fdwReason)
    {
        case DLL_PROCESS_ATTACH:
            WMC_SHOW = RegisterWindowMessageA("WMC_SHOW");
            WMC_CREATE = RegisterWindowMessageA("WMC_CREATE");
            WMC_ENTERMOVE = RegisterWindowMessageA("WMC_ENTERMOVE");
            WMC_MOVE = RegisterWindowMessageA("WMC_MOVE");
            WMC_EXITMOVE = RegisterWindowMessageA("WMC_EXITMOVE");
            WMC_KEYDOWN = RegisterWindowMessageA("WMC_KEYDOWN");
            WMC_KEYUP = RegisterWindowMessageA("WMC_KEYUP");
            WMC_SETFOCUS = RegisterWindowMessageA("WMC_SETFOCUS");
            WMC_KILLFOCUS = RegisterWindowMessageA("WMC_KILLFOCUS");
            WMC_SHOWWINDOW = RegisterWindowMessageA("WMC_SHOWWINDOW");
            WMC_DESTROY = RegisterWindowMessageA("WMC_DESTROY");
            WMC_STYLECHANGED = RegisterWindowMessageA("WMC_STYLECHANGED");
            WMC_SCCLOSE = RegisterWindowMessageA("WMC_SCCLOSE");
            WMC_SCMAXIMIZE = RegisterWindowMessageA("WMC_SCMAXIMIZE");
            WMC_SCMINIMIZE = RegisterWindowMessageA("WMC_SCMINIMIZE");
            WMC_SCRESTORE = RegisterWindowMessageA("WMC_SCRESTORE");
            WMC_ACTIVATEAPP = RegisterWindowMessageA("WMC_ACTIVATEAPP");
            WMC_DISPLAYCHANGE = RegisterWindowMessageA("WMC_DISPLAYCHANGE");
            WMC_SIZE = RegisterWindowMessageA("WMC_SIZE");
            WMC_EXTRATRACK = RegisterWindowMessageA("WMC_EXTRATRACK");
            
            g_hInstance = hInstance;
            // init
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}

/*
  Injected function that listen on all process
*/
static LRESULT CALLBACK CallWndProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (gThread == 0 || WMC_MOVE == 0 || WMC_EXITMOVE == 0 || nCode < 0)
		return CallNextHookEx(g_hook, nCode, wParam, lParam);

	if (nCode == HC_ACTION)
	{
		// not after: CWPSTRUCT *cwps = (CWPSTRUCT*)lParam;
		//CWPRETSTRUCT *cwps = (CWPRETSTRUCT*)lParam;
		CWPSTRUCT *cwps = (CWPSTRUCT*)lParam;
		if(GetParent(cwps->hwnd) == NULL)
        {
            if (cwps->message == WM_CREATE)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                CREATESTRUCT *cs = (CREATESTRUCT*)cwps->lParam;
                LPARAM lpar = (LPARAM)cs->style;
                PostThreadMessage(gThread, WMC_CREATE, wpar, lpar);
            }
            else
            if (cwps->message == WM_SHOWWINDOW)
            {
                LPARAM lpar = (LPARAM)cwps->wParam;
                WPARAM wpar = (WPARAM)cwps->hwnd;
                PostThreadMessage(gThread, WMC_SHOW, wpar, lpar);
            }
            else
            if (cwps->message == WM_ENTERSIZEMOVE)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                PostThreadMessage(gThread, WMC_ENTERMOVE, wpar, (LPARAM)NULL);
            }
            else
            if (cwps->message == WM_ACTIVATEAPP)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                PostThreadMessage(gThread, WMC_ACTIVATEAPP, wpar, (LPARAM)cwps->wParam);
            }
            else
            if (cwps->message == WM_MOVE)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->lParam;
                PostThreadMessage(gThread, WMC_MOVE, wpar, lpar);
            }
            else if (cwps->message == WM_EXITSIZEMOVE)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->lParam;
                PostThreadMessage(gThread, WMC_EXITMOVE, wpar, lpar);
            }
            else if (cwps->message == WM_SETFOCUS)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->wParam;
                PostThreadMessage(gThread, WMC_SETFOCUS, wpar, lpar);
            }
            else if (cwps->message == WM_KILLFOCUS)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->wParam;
                PostThreadMessage(gThread, WMC_KILLFOCUS, wpar, lpar);
            }
            else if (cwps->message == WM_SHOWWINDOW)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->wParam;
                PostThreadMessage(gThread, WMC_SHOWWINDOW, wpar, lpar);
            }
            else if (cwps->message == WM_DESTROY)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)0;
                PostThreadMessage(gThread, WMC_DESTROY, wpar, lpar);
            }
            else if (cwps->message == WM_STYLECHANGED)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->lParam;
                ((STYLESTRUCT*)cwps->lParam)->styleOld = cwps->wParam;
                PostThreadMessage(gThread, WMC_STYLECHANGED, wpar, lpar);
            }
            else if (cwps->message == WM_SIZE)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->lParam;
                PostThreadMessage(gThread, WMC_SIZE, wpar, lpar);
            }
            else if (cwps->message == WM_SYSCOMMAND)
            {
                WPARAM wpar = (WPARAM)cwps->hwnd;
                LPARAM lpar = (LPARAM)cwps->lParam;
                UINT typ = 0;
                if (cwps->wParam == SC_CLOSE)
                {
                    typ = WMC_SCCLOSE;
                }
                else if (cwps->wParam == SC_MAXIMIZE)
                {
                    typ = WMC_SCMAXIMIZE;
                }
                else if (cwps->wParam == SC_MINIMIZE)
                {
                    typ = WMC_SCMINIMIZE;
                }
                else if (cwps->wParam == SC_RESTORE)
                {
                    typ = WMC_SCRESTORE;
                }

                if (typ != 0)
                    PostThreadMessage(gThread, typ, wpar, lpar);
            }
            else if (cwps->message == WM_DISPLAYCHANGE)
            {
                PostThreadMessage(gThread, WMC_DISPLAYCHANGE, (WPARAM)WM_DISPLAYCHANGE, (LPARAM)NULL);
            }
            else if (cwps->message == WM_SETTINGCHANGE && cwps->wParam == SPI_SETWORKAREA)
            {
                PostThreadMessage(gThread, WMC_DISPLAYCHANGE, (WPARAM)WM_SETTINGCHANGE, (LPARAM)NULL);
            }
        }
        
        if (g_pinpointHandler != NULL && cwps->hwnd == g_pinpointHandler)
            PostThreadMessage(gThread, WMC_EXTRATRACK, (WPARAM)cwps->hwnd, (LPARAM)cwps->message);
	}

    return CallNextHookEx(g_hook, nCode, wParam, lParam);
}

void DoExtraKeyCheck(UINT type, PKBDLLHOOKSTRUCT status)
{
    // Zero out the press/unpress flag (bit 8)
    DWORD eflags = status->flags & 0x7F;

    // CONTROL
    if ((status->vkCode == VK_LCONTROL && eflags == 0) || (status->vkCode == VK_RCONTROL && eflags == 1))
    {
        PostThreadMessage(gThread, type, (WPARAM)VK_CONTROL, (LPARAM)status->flags);
        return;
    }

    // ALT key
    if ((status->vkCode == VK_LMENU || status->vkCode == VK_RMENU))
    {
        PostThreadMessage(gThread, type, (WPARAM)VK_MENU, (LPARAM)status->flags);
        return;
    }

    // SHIFT key
    if ((status->vkCode == VK_LSHIFT || status->vkCode == VK_RSHIFT))
    {
        PostThreadMessage(gThread, type, (WPARAM)VK_SHIFT, (LPARAM)status->flags);
        return;
    }
}

static LRESULT CALLBACK KeyboardProcLL(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode == HC_ACTION)
    {
        switch (wParam)
        {
            case WM_KEYDOWN:
            case WM_SYSKEYDOWN:
            {
                DWORD keyCode = ((PKBDLLHOOKSTRUCT)lParam)->vkCode;
                DWORD flags = ((PKBDLLHOOKSTRUCT)lParam)->flags;

                // LCONTROL keyCode can be used for starting extended (2-4 byte keys) press
                // for example RALT (a.k.a. ALT GR) will send LCONTROL and then RMENU.
                // bit nr 5 (zero based) indicated if ALT key is pressed, <- strange explanation but I took it
                // from MSDN, but my tests indicate it is only set if ALT GR is pressed and not when pressing LCTRL.
                if (keyCode != VK_LCONTROL || (flags & 0x20) == 0)
                    PostThreadMessage(gThread, WMC_KEYDOWN, (WPARAM)keyCode, (LPARAM)flags);
                DoExtraKeyCheck(WMC_KEYDOWN, (PKBDLLHOOKSTRUCT)lParam);

                if ((keyCode == VK_LWIN) || (keyCode == VK_RWIN))
                {
                    g_winPressed = 1;
                    if (g_disableWinKey)
                        return 1;
                }

                if (g_winPressed)
                {
                    return 1;
                }
            }
            break;
            case WM_KEYUP:
            case WM_SYSKEYUP:
            {
                DWORD keyCode = ((PKBDLLHOOKSTRUCT)lParam)->vkCode;
                DWORD flags = ((PKBDLLHOOKSTRUCT)lParam)->flags;
                if (keyCode != VK_LCONTROL || (flags & 0x20) == 0)
                    PostThreadMessage(gThread, WMC_KEYUP, (WPARAM)keyCode, (LPARAM)flags);
                DoExtraKeyCheck(WMC_KEYUP, (PKBDLLHOOKSTRUCT)lParam);

                if ((keyCode == VK_LWIN) || (keyCode == VK_RWIN))
                {
                    g_winPressed = 0;
                    if (g_disableWinKey)
                        return 1;
                }

                if (g_winPressed)
                {
                    return 1;
                }
            }   
                break;
        }
    }

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

/*
    Install our listener *should be called from "host"*
*/
BOOL WINHOOK_API InstallHook(DWORD thread, int disableWinKey, CINT pinpointHandler)
{
    g_pinpointHandler = (HWND)pinpointHandler;

    if(!g_hook)
    {
        gThread = thread;
        // Not after: g_hook = SetWindowsHookEx(WH_CALLWNDPROC,
        //g_hook = SetWindowsHookEx(WH_CALLWNDPROCRET,
        g_hook = SetWindowsHookEx(WH_CALLWNDPROC,
                                  CallWndProc,
                                  g_hInstance,
                                  0);
        if(!g_hook)
            return FALSE;
    }

    if (!g_hookKeyb)
    {
        g_disableWinKey = disableWinKey;
        g_hookKeyb = SetWindowsHookEx(WH_KEYBOARD_LL,
                                  KeyboardProcLL,
                                  g_hInstance,
                                  0);
        if(!g_hookKeyb) {
            UnhookWindowsHookEx(g_hook);
            g_hook = NULL;
            return FALSE;
        }
    }

    return TRUE;
}

/*
    Remove our listener *should be called from "host"*
*/
BOOL WINHOOK_API RemoveHook()
{
    BOOL unloadHook = TRUE;
    if(g_hook)
    {
        unloadHook = UnhookWindowsHookEx(g_hook);
        g_hook = NULL;
    }

    if(g_hookKeyb)
    {
        if(!UnhookWindowsHookEx(g_hookKeyb))
            return FALSE;
        g_hookKeyb = NULL;
    }

    if(!unloadHook)
        return FALSE;

    return TRUE;
}
