#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <wingdi.h>
#include <shlwapi.h>
#include <ctype.h>
#include <math.h>

#define MAX_TRIES 2
//#define DEBUG
//#define DEBUG_VERBOSE
//#define DEBUG_VVERBOSE


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

#ifdef ENV64
#define LIBWINHOOK "libwinhook64.dll"
#else
#define LIBWINHOOK "libwinhook32.dll"
#endif

#ifdef ENV32
    #define PIPENAME "\\\\.\\pipe\\tilewindowpipe32"
    #define ENVNAME "ENV32"
#else
    #define PIPENAME "\\\\.\\pipe\\tilewindowpipe64"
    #define ENVNAME "ENV64"
#endif

#ifdef ENV32
    #define CINT long
#else
    #define CINT long long
#endif
typedef struct
{
    UINT msg;
#ifdef ENV32
    UINT msgUpper;
#endif
    WPARAM wParam;
#ifdef ENV32
    WPARAM wParamUpper;
#endif
    LPARAM lParam;
#ifdef ENV32
    LPARAM lParamUpper;
#endif
} PipeMessage;


typedef BOOL (CALLBACK* InstallHook)(DWORD hWnd, int disableWinKey, CINT pinpointHandler);
typedef BOOL (CALLBACK* RemoveHook)(void);

UINT WMC_SHOW = 0, WMC_CREATE = 0, WMC_MOVE = 0, WMC_EXITMOVE = 0, WMC_ENTERMOVE = 0, WMC_KEYDOWN = 0, WMC_KEYUP = 0, WMC_SETFOCUS = 0, WMC_KILLFOCUS = 0;
UINT WMC_SHOWWINDOW = 0, WMC_DESTROY = 0, WMC_STYLECHANGED = 0, WMC_ACTIVATEAPP = 0;
UINT WMC_SCCLOSE = 0, WMC_SCMAXIMIZE = 0, WMC_SCMINIMIZE = 0, WMC_SCRESTORE = 0;
UINT WMC_DISPLAYCHANGE = 0, WMC_SIZE = 0, WMC_EXTRATRACK = 0;

HMODULE hook = NULL;
DWORD gThread = 0;
InstallHook installHook = NULL;
RemoveHook uninstallHook = NULL;
HINSTANCE hInstance = NULL;
HANDLE hPipe = NULL;

int cmdLine_disableWinKey;
CINT cmdLine_pinpointHandler;
void onExit(int exitCode, const char* str, ...)
{
    va_list arg;

    va_start(arg, str);
    printf(str, arg);
    va_end(arg);

    exit(exitCode);
}

void handleExit()
{
//printf(ENVNAME " going to shutdown...\n");

    if(uninstallHook != NULL)
    {
//printf(ENVNAME " Uninstalling Hook...\n");
        uninstallHook();
    }

    if(hook != NULL)
    {
//printf(ENVNAME " releasing hook dll...\n");
        FreeLibrary(hook);
    }

    if(hPipe != NULL)
    {
//printf(ENVNAME " closing pipe handler...\n");
        CloseHandle(hPipe);
    }

    hPipe = NULL;
    installHook = NULL;
    uninstallHook = NULL;
    hook = NULL;
//printf(ENVNAME " shutdown done\n");
}

void SendPipedMessage(UINT msg, WPARAM wParam, LPARAM lParam)
{
    PipeMessage toSend;
    toSend.msg = msg;
    toSend.wParam = wParam;
    toSend.lParam = lParam;

#ifdef ENV32
    toSend.msgUpper = 0;
    toSend.wParamUpper = 0;
    toSend.lParamUpper = (toSend.lParam & 80000000) ? 0xFFFFFFFF : 0;
#endif

    DWORD cbWritten;
    WriteFile(
        hPipe,                  // pipe handle
        (CHAR*)&toSend,           // message
        sizeof(PipeMessage),  // message length
        &cbWritten,             // bytes written
        NULL);                  // not overlapped
}

void InitPipe()
{
    CHAR tries = 0;

    hPipe = CreateFile(
        PIPENAME,   // pipe name
        GENERIC_READ |  // read and write access
        GENERIC_WRITE,
        0,              // no sharing
        NULL,           // default security attributes
        OPEN_EXISTING,  // opens existing pipe
        0,              // default attributes
        NULL);          // no template file

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        if (GetLastError() != ERROR_PIPE_BUSY)
        {
            onExit(4, ENVNAME " Could not open pipe. GLE=%d\n", GetLastError() );
        }

        BOOL w = FALSE;
        w = WaitNamedPipe(PIPENAME, 2000);
        while (tries < 10 && w == FALSE)
        {
            tries++;

            #ifdef DEBUG
            printf(ENVNAE " Waiting 2 seconds... (try nr %i)\n", tries);
            #endif

            w = WaitNamedPipe(PIPENAME, 2000);
        }

        if(!w)
        {
            onExit(5, ENVNAME " Reached total number of tries to connect, aborting...\n");
        }
    }
}

BOOL IsPositiveNumber(char *str, int length, CINT *result)
{
    CINT res = 0;
    for(int i = 0; i < length; i++)
    {
        if (str[i] < '0' || str[i] > '9')
            return FALSE;
        res += ((str[i] - '0')) * pow(10, length - i - 1);
    }

    *result = res;
    return TRUE;
}

void ParseArgs(LPSTR lpCmdLine)
{
    int start = 0;
    for(int i = 0; lpCmdLine[i]; i++)
    {
        if (lpCmdLine[i] == ' ' || lpCmdLine[i+1] == '\0')
        {
            int len = i - start;
            CINT result;

            if (lpCmdLine[i] != ' ')
                len++;

            if (len == 13 && strncmp(&lpCmdLine[start], "disablewinkey", 13) == 0)
            {
                cmdLine_disableWinKey = 1;
                printf(ENVNAME " Going to disable win key\n");
            }
            else if (len > 0 && IsPositiveNumber(&lpCmdLine[start], len, &result) == TRUE)
            {
                cmdLine_pinpointHandler = result;
                #ifdef ENV32
                    printf(ENVNAME " Going to track %li\n", result);
                    printf(ENVNAME " pointer address: %p, %li\n", cmdLine_pinpointHandler, cmdLine_pinpointHandler);
                #else
                    printf(ENVNAME " Going to track %I64i\n", result);
                    printf(ENVNAME " pointer address: %p, %I64i\n", cmdLine_pinpointHandler, cmdLine_pinpointHandler);
                #endif
            }

            start = i+1;
        }
        else
        {
            lpCmdLine[i] = tolower(lpCmdLine[i]);
        }
    }
}

int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    hInstance = hInst;
    atexit(handleExit);
    cmdLine_disableWinKey = 0;

    ParseArgs(lpCmdLine);

    // Load DLL and setup all the custom messages
    hook = LoadLibrary(LIBWINHOOK);
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
    WMC_SCCLOSE = RegisterWindowMessageA("WMC_SCCLOSE");
    WMC_SCMAXIMIZE = RegisterWindowMessageA("WMC_SCMAXIMIZE");
    WMC_SCMINIMIZE = RegisterWindowMessageA("WMC_SCMINIMIZE");
    WMC_STYLECHANGED = RegisterWindowMessageA("WMC_STYLECHANGED");
    WMC_SCRESTORE = RegisterWindowMessageA("WMC_SCRESTORE");
    WMC_ACTIVATEAPP = RegisterWindowMessageA("WMC_ACTIVATEAPP");
    WMC_DISPLAYCHANGE = RegisterWindowMessageA("WMC_DISPLAYCHANGE");
    WMC_SIZE = RegisterWindowMessageA("WMC_SIZE");
    WMC_EXTRATRACK = RegisterWindowMessageA("WMC_EXTRATRACK");
    gThread = GetCurrentThreadId();

    if(gThread == 0)
        onExit(1, ENVNAME " Could not retrieve current threads id\n");
    if(WMC_MOVE == 0 || WMC_EXITMOVE == 0)
        onExit(1, ENVNAME " Could not register special WMC messages.\n");
    if(hook == NULL)
            onExit(1, ENVNAME " Could not find "LIBWINHOOK"\n");

    installHook = (InstallHook)GetProcAddress(hook, "InstallHook");
    uninstallHook = (RemoveHook)GetProcAddress(hook, "RemoveHook");
    if(installHook == NULL)
        onExit(2, ENVNAME " Could not locate InstallHook function in " LIBWINHOOK "\n");
    if(uninstallHook == NULL)
        onExit(2, ENVNAME " Could not locate UninstallHook function in " LIBWINHOOK "\n");

    InitPipe();

    // Now activate our hook
    if(installHook(gThread, cmdLine_disableWinKey, cmdLine_pinpointHandler) == FALSE)
        onExit(3, ENVNAME " Error while installing \"hook\"\n");


    MSG msg;
    BOOL done = FALSE;
    while(!done)
    {
        BOOL ret = GetMessage(&msg, NULL, 0, 0);
        if (ret == 0 || msg.message == WM_CLOSE)
        {
            done = TRUE;
        }
        else if (msg.message == WMC_ENTERMOVE ||
            msg.message == WMC_EXITMOVE ||
            msg.message == WMC_KEYDOWN ||
            msg.message == WMC_KEYUP ||
            msg.message == WMC_MOVE ||
            msg.message == WMC_CREATE ||
            msg.message == WMC_SHOW ||
            msg.message == WMC_SETFOCUS ||
            msg.message == WMC_KILLFOCUS ||
            msg.message == WMC_SHOWWINDOW ||
            msg.message == WMC_DESTROY ||
            msg.message == WMC_STYLECHANGED ||
            msg.message == WMC_SCCLOSE ||
            msg.message == WMC_SCMAXIMIZE ||
            msg.message == WMC_SCMINIMIZE ||
            msg.message == WMC_SCRESTORE ||
            msg.message == WMC_ACTIVATEAPP ||
            msg.message == WMC_DISPLAYCHANGE ||
            msg.message == WMC_SIZE ||
            msg.message == WMC_EXTRATRACK)
        {
            SendPipedMessage(msg.message, msg.wParam, msg.lParam);
        }
    }

    return 0;
}
