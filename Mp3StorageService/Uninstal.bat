@echo off
sc.exe stop "Mp3Storage"
sc.exe delete "Mp3Storage"
taskkill /F /IM mmc.exe
pause