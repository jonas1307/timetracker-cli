param (
    [ValidateSet("all", "win", "linux")]
    [string]$Target = "all"
)

$project = "Timetracker.Console/Timetracker.csproj"

$profiles = @{
    win   = @{ profile = "win-x64";   out = "publish/win";   bin = "timetracker.exe" }
    linux = @{ profile = "linux-x64"; out = "publish/linux"; bin = "timetracker"     }
}

$selected = if ($Target -eq "all") { $profiles.Keys } else { @($Target) }

foreach ($name in $selected) {
    $p = $profiles[$name]
    Write-Host ""
    Write-Host "Publishing for $name..." -ForegroundColor Cyan

    dotnet publish $project /p:PublishProfile=$($p.profile)

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed for $name." -ForegroundColor Red
        exit 1
    }

    $bin = Join-Path $p.out $p.bin
    $size = [math]::Round((Get-Item $bin).Length / 1MB, 1)
    Write-Host "OK  $bin  ($size MB)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
