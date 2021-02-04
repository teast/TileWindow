using TileWindow;

public interface ISignalHandler
{
    uint  WMC_SHOW { get; }
    uint  WMC_CREATE { get; }
    uint  WMC_ENTERMOVE { get; }
    uint  WMC_MOVE { get; }
    uint  WMC_EXITMOVE { get; }
    uint  WMC_KEYDOWN { get; }
    uint  WMC_KEYUP { get; }
    uint  WMC_SETFOCUS { get; }
    uint  WMC_KILLFOCUS { get; }
    uint  WMC_SHOWWINDOW { get; }
    uint  WMC_DESTROY { get; }
    uint  WMC_STYLECHANGED { get; }
    uint  WMC_SCCLOSE { get; }
    uint  WMC_SCMAXIMIZE { get; }
    uint  WMC_SCMINIMIZE { get; }
    uint WMC_SCRESTORE { get; }
    uint WMC_ACTIVATEAPP { get; }
    uint WMC_DISPLAYCHANGE { get; }
    uint WMC_SIZE { get; }
    uint WMC_EXTRATRACK { get; }
    uint WMC_SHOWNODE { get; }

    string SignalToString(uint signal);
}

public class SignalHandler: ISignalHandler
{
    public uint  WMC_SHOW { get; }
    public uint  WMC_CREATE { get; }
    public uint  WMC_ENTERMOVE { get; }
    public uint  WMC_MOVE { get; }
    public uint  WMC_EXITMOVE { get; }
    public uint  WMC_KEYDOWN { get; }
    public uint  WMC_KEYUP { get; }
    public uint  WMC_SETFOCUS { get; }
    public uint  WMC_KILLFOCUS { get; }
    public uint  WMC_SHOWWINDOW { get; }
    public uint  WMC_DESTROY { get; }
    public uint  WMC_STYLECHANGED { get; }
    public uint  WMC_SCCLOSE { get; }
    public uint  WMC_SCMAXIMIZE { get; }
    public uint  WMC_SCMINIMIZE { get; }
    public uint WMC_SCRESTORE { get; }
    public uint WMC_ACTIVATEAPP { get; }
    public uint WMC_DISPLAYCHANGE { get; }
    public uint WMC_SIZE { get; }
    public uint WMC_EXTRATRACK { get; }
    public uint WMC_SHOWNODE { get; }
    public SignalHandler(IPInvokeHandler pinvokeHandler)
    {
        WMC_SHOW = pinvokeHandler.RegisterWindowMessage("WMC_SHOW");
        WMC_CREATE = pinvokeHandler.RegisterWindowMessage("WMC_CREATE");
        WMC_ENTERMOVE = pinvokeHandler.RegisterWindowMessage("WMC_ENTERMOVE");
        WMC_MOVE = pinvokeHandler.RegisterWindowMessage("WMC_MOVE");
        WMC_EXITMOVE = pinvokeHandler.RegisterWindowMessage("WMC_EXITMOVE");
        WMC_KEYDOWN = pinvokeHandler.RegisterWindowMessage("WMC_KEYDOWN");
        WMC_KEYUP  = pinvokeHandler.RegisterWindowMessage("WMC_KEYUP");
        WMC_SETFOCUS  = pinvokeHandler.RegisterWindowMessage("WMC_SETFOCUS");
        WMC_KILLFOCUS  = pinvokeHandler.RegisterWindowMessage("WMC_KILLFOCUS");
        WMC_SHOWWINDOW  = pinvokeHandler.RegisterWindowMessage("WMC_SHOWWINDOW");
        WMC_DESTROY  = pinvokeHandler.RegisterWindowMessage("WMC_DESTROY");
        WMC_STYLECHANGED  = pinvokeHandler.RegisterWindowMessage("WMC_STYLECHANGED");
        WMC_SCCLOSE = pinvokeHandler.RegisterWindowMessage("WMC_SCCLOSE");
        WMC_SCMAXIMIZE = pinvokeHandler.RegisterWindowMessage("WMC_SCMAXIMIZE");
        WMC_SCMINIMIZE = pinvokeHandler.RegisterWindowMessage("WMC_SCMINIMIZE");
        WMC_SCRESTORE = pinvokeHandler.RegisterWindowMessage("WMC_SCRESTORE");
        WMC_ACTIVATEAPP = pinvokeHandler.RegisterWindowMessage("WMC_ACTIVATEAPP");
        WMC_DISPLAYCHANGE = pinvokeHandler.RegisterWindowMessage("WMC_DISPLAYCHANGE");
        WMC_SIZE = pinvokeHandler.RegisterWindowMessage("WMC_SIZE");
        WMC_EXTRATRACK = pinvokeHandler.RegisterWindowMessage("WMC_EXTRATRACK");

        // Custom message not from winhook
        WMC_SHOWNODE = pinvokeHandler.RegisterWindowMessage("WMC_SHOWNODE");
    }

    public string SignalToString(uint signal)
    {
        if (signal == WMC_SHOW) return "WMC_SHOW";

        if (signal == WMC_CREATE) return "WMC_CREATE";

        if (signal == WMC_ENTERMOVE) return "WMC_ENTERMOVE";

        if (signal == WMC_MOVE) return "WMC_MOVE";

        if (signal == WMC_EXITMOVE) return "WMC_EXITMOVE";

        if (signal == WMC_KEYDOWN) return "WMC_KEYDOWN";

        if (signal == WMC_KEYUP ) return "WMC_KEYUP ";

        if (signal == WMC_SETFOCUS ) return "WMC_SETFOCUS ";

        if (signal == WMC_KILLFOCUS ) return "WMC_KILLFOCUS ";

        if (signal == WMC_SHOWWINDOW ) return "WMC_SHOWWINDOW ";

        if (signal == WMC_DESTROY ) return "WMC_DESTROY ";

        if (signal == WMC_STYLECHANGED ) return "WMC_STYLECHANGED ";

        if (signal == WMC_SCCLOSE) return "WMC_SCCLOSE";

        if (signal == WMC_SCMAXIMIZE) return "WMC_SCMAXIMIZE";

        if (signal == WMC_SCMINIMIZE) return "WMC_SCMINIMIZE";

        if (signal == WMC_SCRESTORE) return "WMC_SCRESTORE";

        if (signal == WMC_ACTIVATEAPP) return "WMC_ACTIVATEAPP";

        if (signal == WMC_DISPLAYCHANGE) return "WMC_DISPLAYCHANGE";

        if (signal == WMC_SIZE) return "WMC_SIZE";

        if (signal == WMC_SHOWNODE) return "WMC_SHOWNODE";

        if (signal == WMC_EXTRATRACK) return "WMC_EXTRATRACK";

        return "UNKNOWN";
    }
}