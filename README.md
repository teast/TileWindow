# TileWindow

![AppVeyor](https://ci.appveyor.com/api/projects/status/github/teast/tilewindow)

Make your windows instance more like an "tiling desktop manager".

## What is an "tiling desktop manager" you may ask

The guys at [i3wm](https://i3wm.org/) got a great tutorial on youtube about it [here](https://www.youtube.com/watch?v=j1I63wGcvU4).
And because TileWindow is inspired by i3wm I think it is a great way to get a basic knowledge of what "tiling desktop manager" is and what it can achive.
TileWindow is in an early state and therefore lacks much of the cool stuff that i3wm got.
But below I will list some of the feature that TileWindow do have.

### Tiling existing programs

When you first start TileWindow it will tile up all programs that it think it can handle.
This could end up with lots of small tiles, especially if you got lots of windows on the screen.

### Virtual desktops

TileWindow fakes virtual desktops (by hiding windows on other desktops) when the user switch between them. It currently support up to 10 virtual desktops.

### Floating nodes

TileWindow support putting an tile node into an floating node. this is great if you want an node floating above the rest.

### Stacking layout

TileWindow support stacking nodes.

### Fullscreen node

### Default shortcuts

If you decide to try TileWindow and you use the compiled version at [AppVeyor](https://ci.appveyor.com/project/teast/tilewindow/build/artifacts). Then you will need an [appsettings.json](https://github.com/teast/TileWindow/blob/master/TileWindow/src/appsettings.json) file.

If you check the appsettings.json file from the link above you will find some default key bindings.

    WIN+[arrow key] - Move focus to window located [arrow key] from current active window

    WIN+Shift+[arrow key] - Move active window in [arrow key] direction. If the active window is of type floating you will move the floating window a few pixels in [arrow key] direction.

    WIN+V - Change tiling to vertical tiling (and often create an new container node on the window node and thus make it possible to move another window into this new window/container node)

    WIN+R - Show run dialog, great if you got disableWinKey set to true.

    WIN+Shift+Space - switch between floating and non floating for active window.

    WIN+[digit key] - switch between virtual desktops (WIN+1) to switch to first virtual desktop, WIN+2 for next and so on to WIN+0.

    WIN+Shift+[digit key] - move active window to [digit key] virtual desktop.

    WIN+Shift+Q - Close active window.

    WIN+S - switch between tiling and stacking layout (stacking is currently in pre pre pre pre alpha something version...)

    WIN+ALT+[arrow key] - resize active window in [arrow key] plane.



## How TileWindow handles the actual tiling

TileWindow is keeping track on all windows by creating nodes that can contain even more nodes.

The root node is VirtualDesktop node. VirtualDesktop nodes can only contain ScreenNodes.
TileWindow will create one ScreenNode for each screen it finds on the system.

After that it is a mixture of ContainerNodes and WindowNodes.

### ContainerNode

This ones can contain other nodes.

    VirtualDesktop and ScreenNode are actual based on this node

ContainerNode got an IRenderer that decides how the specific ContainerNode should structure the child nodes (for example TileRenderer for tiling, StackRenderer for stacking them).

### WindowNode

This node always has an window handle connected to it and its purpose is to make sure that window behaves as TileWindow wants.

## How it works (technical)

TileWindow consists of three parts, TileWindow, TWHandler and WineHook.

### WineHook

This is an dll written in c. Its main purpose is to get injected (with winapi [SetWindowsHookEx](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa)) into all processes on the system.
Because TileWindow is targeting 64 bit systems, we have to compile this two times, one for 32 bit opcodes and one with 64 bit opcodes.
WinHook is hooking into _WH_CALLWNDPROCRET_ and _WH_KEYBOARD_LL_. The later because we want to be able to disable win-key.

### TWHandler

This is an console program written in c. It works as the glue between low level dll and C# TileWindow. It do this by setting up an named pipe" connection with TileWindow program and forwarding custom messages that Winhook sends it.
As with Winhook we have to compile this in both 32 and 64 bit versions.

### TileWindow.exe

This is the main program, it contains all logic and handlers. It sets up named pipe listeners and starts both versions of TWHandler. Each message received on named pipe will be added to an concurrent queue. It will create an new side thread that will read from this queue and do needed logic based on the message.

## How to compile

Download both mingw32 and mingw64.
Add both mingw32 and mingw64 bin folders to your path (important to have mingw64 before mingw32)
Make sure mingw32 is installed to c:\mingw\ folder (or change tasks.json so they point to your mingw32 version)

Press F5.

When you first run TileWindow you will probably have to create an appsettings.json file in same folder as your exe file.

## Development environment

* Visual studio code
Extensions:
* Coverage Gutters
* .NET Core Test Explorer
* C#
* C# XML Documentation comments
* GitLens

## Continuous integration

TileWindow is currently using [AppVeyor](https://ci.appveyor.com/project/teast/tilewindow) as CI server.

## Good urls

<https://docs.microsoft.com/sv-se/windows/win32/winmsg/window-notifications>
