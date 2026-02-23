@echo off
cd /d "C:\Steam\steamapps\common\RimWorld\Mods\EndfieldPerlica\Source\EndfieldPerlica"
echo Restoring...
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" EFPerlica.csproj /t:Restore /p:Configuration=Release /v:minimal > ..\..\build_log.txt 2>&1
if %errorlevel% neq 0 (
    echo Restore failed. See build_log.txt
    exit /b %errorlevel%
)

echo Building...
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" EFPerlica.csproj /t:Build /p:Configuration=Release /v:minimal >> ..\..\build_log.txt 2>&1
if %errorlevel% neq 0 (
    echo Build failed. See build_log.txt
    exit /b %errorlevel%
)

echo Build Success!
