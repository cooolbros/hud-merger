@ECHO OFF

RMDIR /s /q HUDMerger\bin
RMDIR /s /q HUDMerger\obj

@REM https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish
@REM https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish HUDMerger -c Release
@REM -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true --runtime win-x64

@REM start HUDMerger\bin\Release\net5.0-windows\publish\

CD HUDMerger\bin\Release\net5.0-windows

RENAME publish HUDMerger
7z a hud-merger.zip HUDMerger
RMDIR /S /Q HUDMerger
RMDIR /S /Q ref
RMDIR /S /Q Resources

FOR /f %%i in ('dir /b') do (IF NOT %%~xi==.zip del %%i /q)

START .

CD ..\..\..\..\
