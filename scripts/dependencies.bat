del /F ..\TileWindow\src\bin\Debug\netcoreapp3.0\twhandler32.exe
del /F ..\TileWindow\src\bin\Debug\netcoreapp3.0\twhandler64.exe
del /F ..\TileWindow\src\bin\Debug\netcoreapp3.0\libwinhook32.dll
del /F ..\TileWindow\src\bin\Debug\netcoreapp3.0\libwinhook64.dll
copy ..\TWHandler\twhandler??.exe ..\TileWindow\src\bin\Debug\netcoreapp3.0
copy ..\WinHook\libwinhook??.dll ..\TileWindow\src\bin\Debug\netcoreapp3.0
