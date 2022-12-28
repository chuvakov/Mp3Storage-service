@echo off
sc.exe create "Mp3Storage" binpath=%~dp0/Mp3StorageService.exe
sc.exe start "Mp3Storage"
pause