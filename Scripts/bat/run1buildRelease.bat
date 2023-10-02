@echo off
cd ../../
if exist sloth.txt (
    call dotnet build -c Release
) else (
    exit
)
pause
