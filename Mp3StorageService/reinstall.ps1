@ECHO OFF

sc.exe stop "Mp3Storage"

sc.exe delete "Mp3Storage"

sc.exe create "Mp3Storage" binpath="%~dp0Mp3StorageService.exe"

pause