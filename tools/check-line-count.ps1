param(
    [int]$MaxLines = 1500,
    [string]$Root = "Assets/Scripts",
    [string[]]$Include = @("*.cs")
)

$ErrorActionPreference = "Stop"

$rootPath = Resolve-Path -LiteralPath $Root
$files = Get-ChildItem -LiteralPath $rootPath -Recurse -File -Include $Include |
    Where-Object {
        $_.FullName -notmatch '\\(Library|Temp|obj|bin)\\'
    }

$violations = @()
foreach ($file in $files) {
    $lineCount = ([System.IO.File]::ReadAllLines($file.FullName)).Length
    if ($lineCount -gt $MaxLines) {
        $violations += [PSCustomObject]@{
            Lines = $lineCount
            Path = Resolve-Path -LiteralPath $file.FullName -Relative
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Line-count limit exceeded. Max allowed: $MaxLines" -ForegroundColor Red
    $violations | Sort-Object Lines -Descending | Format-Table -AutoSize
    exit 1
}

Write-Host "Line-count check passed. Max allowed: $MaxLines" -ForegroundColor Green
