@echo off
cd ../../
if exist sloth.txt (
    call dotnet build -c Debug
) else (
    exit
)
pause
