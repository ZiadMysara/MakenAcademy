# Apply migrations using Npgsql directly (bypassing EF Core)
# This uses the transaction pooler which works

$connectionString = "Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ibcoveybgyavggukabtr;Password=Maken@01033529651;SSL Mode=Require;Trust Server Certificate=true;Timeout=30"

Write-Host "Reading migrations.sql..." -ForegroundColor Yellow
$sql = Get-Content -Path "migrations.sql" -Raw

Write-Host "Connecting to Supabase via transaction pooler..." -ForegroundColor Yellow

# Load Npgsql assembly
Add-Type -Path "src\Maken.Infrastructure\bin\Debug\net8.0\Npgsql.dll"

try {
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected successfully!" -ForegroundColor Green
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    
    Write-Host "Executing migrations..." -ForegroundColor Yellow
    $result = $command.ExecuteNonQuery()
    
    Write-Host "Success! Migrations applied." -ForegroundColor Green
    Write-Host "Rows affected: $result" -ForegroundColor Cyan
    
    $connection.Close()
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
    exit 1
}

Write-Host "`nVerifying tables..." -ForegroundColor Yellow
try {
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"
    
    $reader = $command.ExecuteReader()
    
    Write-Host "`nTables in database:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  - $($reader['table_name'])" -ForegroundColor White
    }
    
    $reader.Close()
    $connection.Close()
}
catch {
    Write-Host "Could not verify tables: $_" -ForegroundColor Yellow
}

Write-Host "`nDone! You can now run: dotnet run --project src/Maken.Api" -ForegroundColor Green
