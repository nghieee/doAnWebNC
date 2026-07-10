# Dừng app web-ban-thuoc đang chạy
$running = Get-Process -Name "web-ban-thuoc" -ErrorAction SilentlyContinue
if (-not $running) {
    Write-Host "Khong co web-ban-thuoc nao dang chay." -ForegroundColor Green
    exit 0
}

Write-Host "Dang dung web-ban-thuoc (PID: $($running.Id -join ', '))..." -ForegroundColor Yellow
$running | Stop-Process -Force -ErrorAction SilentlyContinue

# Chờ thêm 1 giây để chắc chắn file được giải phóng
Start-Sleep -Seconds 1

# Kiểm tra lại
$check = Get-Process -Name "web-ban-thuoc" -ErrorAction SilentlyContinue
if ($check) {
    Write-Host "Van con tien trinh, thu lan nua..." -ForegroundColor Yellow
    $check | Stop-Process -Force
    Start-Sleep -Seconds 1
}

Write-Host "Da dung." -ForegroundColor Green