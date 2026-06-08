# Dừng app web-ban-thuoc đang chạy
# Cách dùng: .\stop-dev.ps1
$running = Get-Process -Name "web-ban-thuoc" -ErrorAction SilentlyContinue
if (-not $running) {
    Write-Host "Khong co web-ban-thuoc nao dang chay." -ForegroundColor Green
    exit 0
}

Write-Host "Dang dung web-ban-thuoc (PID: $($running.Id -join ', '))..." -ForegroundColor Yellow
$running | Stop-Process -Force
Write-Host "Da dung." -ForegroundColor Green
