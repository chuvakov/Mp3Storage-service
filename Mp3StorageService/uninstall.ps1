sc.exe stop "Mp3Storage"

sc.exe delete "Mp3Storage"

Write-Host "Service deleted!" -ForegroundColor Green 
Pause