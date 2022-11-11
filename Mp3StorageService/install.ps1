$PathToExe = Join-Path $PSScriptRoot "Mp3StorageService.exe"
sc.exe create "Mp3Storage" binpath=$PathToExe
sc.exe start "Mp3Storage"

Write-Host "Service started!" -ForegroundColor Green 
Pause
