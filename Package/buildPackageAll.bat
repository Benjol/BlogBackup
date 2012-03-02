@ECHO OFF
SET FRAMEWORK=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319
REM Build all configurations
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Build /p:Configuration=Debug
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Build /p:Configuration=Release
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Build /p:Configuration=Standalone

REM create Temp dir (workaround for 7za limitations)
md Temp
cd Temp

REM Copy to here for Standard
copy ..\..\README.md README.txt
copy ..\..\bin\Release\BlogBackup.exe
..\7za a ..\BlogBackup.zip *.*

REM Copy to here for Standalone
copy /Y ..\..\bin\Standalone\BlogBackup.exe
..\7za a ..\StandaloneBlogBackup.zip *.*

REM remove Temp dir
cd ..
rmdir /s /q Temp

REM Clean all configurations
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Clean /p:Configuration=Debug
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Clean /p:Configuration=Release
%FRAMEWORK%\msbuild.exe "..\BlogBackup.sln" /t:Clean /p:Configuration=Standalone

pause