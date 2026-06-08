# Chạy web từ thư mục gốc dự án (doAnWebNC)
# Cách dùng: .\run-dev.ps1
Set-Location $PSScriptRoot

# Dừng instance cũ (tránh lỗi MSB3027 file bị khóa)
$running = Get-Process -Name "web-ban-thuoc" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Dang dung instance cu (PID: $($running.Id -join ', '))..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

dotnet watch --project ".\web-ban-thuoc\web-ban-thuoc.csproj" run
