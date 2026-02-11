# Fix Supabase DNS Resolution
# Run this script as Administrator

Write-Host "Adding Supabase hostnames to hosts file..." -ForegroundColor Yellow

$hostsPath = "C:\Windows\System32\drivers\etc\hosts"
$hostsContent = Get-Content $hostsPath -Raw

# Check if entries already exist
if ($hostsContent -notmatch "db.ibcoveybgyavggukabtr.supabase.co") {
    $newEntries = "`n# Supabase Production - Maken Project`n18.202.64.2 db.ibcoveybgyavggukabtr.supabase.co`n18.202.64.2 aws-1-eu-west-1.pooler.supabase.com"
    
    Add-Content -Path $hostsPath -Value $newEntries -Force
    Write-Host "Success: Supabase hostnames added to hosts file" -ForegroundColor Green
} else {
    Write-Host "Info: Supabase hostnames already in hosts file" -ForegroundColor Green
}

# Flush DNS cache
ipconfig /flushdns | Out-Null
Write-Host "Success: DNS cache flushed" -ForegroundColor Green

# Test connection
Write-Host "`nTesting connection..." -ForegroundColor Yellow
$result = Test-NetConnection -ComputerName db.ibcoveybgyavggukabtr.supabase.co -Port 5432 -WarningAction SilentlyContinue

if ($result.TcpTestSucceeded) {
    Write-Host "Success: Connection successful!" -ForegroundColor Green
    Write-Host "`nYou can now run: dotnet ef database update" -ForegroundColor Cyan
}
else {
    Write-Host "Error: Connection failed. Check firewall settings." -ForegroundColor Red
}
