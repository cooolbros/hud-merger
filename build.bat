@ECHO OFF

RMDIR /s /q hud-merger\bin
RMDIR /s /q hud-merger\obj

@REM https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish
@REM https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish hud-merger -c Release
@REM -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true --runtime win-x64

@REM start hud-merger\bin\Release\net5.0-windows\publish\

CD hud-merger\bin\Release\net5.0-windows

RENAME publish hud-merger
7z a hud-merger.zip hud-merger
RMDIR /S /Q hud-merger
RMDIR /S /Q ref
RMDIR /S /Q Resources

FOR /f %%i in ('dir /b') do (IF NOT %%~xi==.zip del %%i /q)

START .

CD ..\..\..\..\