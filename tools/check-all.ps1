param(
    [int]$MaxLines = 1500,
    [string]$RuntimeProject = "Assembly-CSharp.csproj",
    [string]$EditorProject = "Assembly-CSharp-Editor.csproj",
    [string]$UnityPath = "",
    [string]$UnityLogPath = "Temp/check-all-unity-editmode.log",
    [string]$UnityTestResults = "Temp/check-all-editmode-results.xml",
    [switch]$SkipRuntimeBuild,
    [switch]$SkipEditorBuild,
    [switch]$SkipLineCount,
    [switch]$SkipDiffCheck,
    [switch]$SkipMojibakeScan,
    [switch]$SkipSmokeTests,
    [switch]$RequireUnitySmokeTests
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($root)) {
        return (Resolve-Path -LiteralPath $root).Path
    }

    return (Resolve-Path -LiteralPath ".").Path
}

function Invoke-CheckStep {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    Write-Host ""
    Write-Host "== $Name ==" -ForegroundColor Cyan
    & $Script
    Write-Host "OK: $Name" -ForegroundColor Green
}

function Get-UnityEditorVersion {
    $projectVersionPath = Join-Path $script:RepoRoot "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $projectVersionPath)) {
        return ""
    }

    $line = Get-Content -LiteralPath $projectVersionPath | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($line)) {
        return ""
    }

    return ($line -replace "^m_EditorVersion:\s*", "").Trim()
}

function Resolve-UnityPath {
    param([string]$RequestedUnityPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedUnityPath) -and (Test-Path -LiteralPath $RequestedUnityPath)) {
        return (Resolve-Path -LiteralPath $RequestedUnityPath).Path
    }

    $unityFromPath = Get-Command "Unity" -ErrorAction SilentlyContinue
    if ($unityFromPath -ne $null) {
        return $unityFromPath.Source
    }

    $version = Get-UnityEditorVersion
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($version)) {
        $candidates += "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
    }

    $candidates += @(
        "C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe",
        "C:/Program Files/Unity/Hub/Editor/6000.4.4f1/Editor/Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return ""
}

function Invoke-GitDiffCheck {
    git diff --check
    if ($LASTEXITCODE -ne 0) {
        throw "git diff --check failed."
    }

    git diff --cached --check
    if ($LASTEXITCODE -ne 0) {
        throw "git diff --cached --check failed."
    }
}

function Invoke-MojibakeScan {
    $replacementChar = [string][char]0xFFFD
    $markers = @("вЂ", "РІР‚", "Р ", "РЎ", "пїЅ", $replacementChar)
    $textExtensions = @(".cs", ".md", ".json", ".txt", ".uxml", ".uss", ".shader", ".asmdef", ".yml", ".yaml", ".ps1")
    $issues = New-Object System.Collections.Generic.List[string]

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $diffs = @()
        $diffs += @(git diff --unified=0 -- . 2>$null)
        $diffs += @(git diff --cached --unified=0 -- . 2>$null)
        $untracked = @(git ls-files --others --exclude-standard 2>$null)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $currentFile = ""
    $currentLine = 0

    foreach ($line in $diffs) {
        if ($line -like "+++ b/*") {
            $currentFile = $line.Substring(6)
            $currentLine = 0
            continue
        }

        if ($line -match "^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@") {
            $currentLine = [int]$Matches[1]
            continue
        }

        if ($line.StartsWith("+") -and -not $line.StartsWith("+++")) {
            if (-not [string]::IsNullOrWhiteSpace($currentFile)) {
                $extension = [System.IO.Path]::GetExtension($currentFile)
                if ($textExtensions -contains $extension -and $currentFile -ne "tools/check-all.ps1") {
                    $addedLine = $line.Substring(1)
                    foreach ($marker in $markers) {
                        if ($addedLine.Contains($marker)) {
                            $issues.Add("${currentFile}:${currentLine}: added line contains mojibake marker '$marker'")
                            break
                        }
                    }
                }
            }

            if ($currentLine -gt 0) {
                $currentLine++
            }
        }
    }

    foreach ($relativePath in $untracked) {
        if ($relativePath -eq "tools/check-all.ps1") {
            continue
        }

        $extension = [System.IO.Path]::GetExtension($relativePath)
        if (-not ($textExtensions -contains $extension)) {
            continue
        }

        $fullPath = Join-Path $script:RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $content = Get-Content -LiteralPath $fullPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ([string]::IsNullOrEmpty($content)) {
            continue
        }

        $lines = $content -split "`r?`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            foreach ($marker in $markers) {
                if ($lines[$i].Contains($marker)) {
                    $issues.Add("${relativePath}:$($i + 1): untracked file contains mojibake marker '$marker'")
                    break
                }
            }
        }
    }

    if ($issues.Count -gt 0) {
        $issues | Select-Object -First 40 | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        if ($issues.Count -gt 40) {
            Write-Host "... and $($issues.Count - 40) more mojibake hits." -ForegroundColor Red
        }

        throw "Mojibake scan failed."
    }

    Write-Host "Mojibake scan passed for added diff lines and untracked text files." -ForegroundColor Green
}

function Invoke-UnitySmokeTests {
    $resolvedUnity = Resolve-UnityPath $UnityPath
    if ([string]::IsNullOrWhiteSpace($resolvedUnity)) {
        $message = "Unity editor not found. Smoke tests were skipped. Pass -UnityPath or use -RequireUnitySmokeTests to make this fatal."
        if ($RequireUnitySmokeTests) {
            throw $message
        }

        Write-Host $message -ForegroundColor Yellow
        return
    }

    $logPath = Join-Path $script:RepoRoot $UnityLogPath
    $resultsPath = Join-Path $script:RepoRoot $UnityTestResults
    $logDir = Split-Path -Parent $logPath
    $resultsDir = Split-Path -Parent $resultsPath
    if (-not (Test-Path -LiteralPath $logDir)) {
        New-Item -ItemType Directory -Path $logDir | Out-Null
    }

    if (-not (Test-Path -LiteralPath $resultsDir)) {
        New-Item -ItemType Directory -Path $resultsDir | Out-Null
    }

    & $resolvedUnity `
        -batchmode `
        -nographics `
        -quit `
        -projectPath $script:RepoRoot `
        -runTests `
        -testPlatform EditMode `
        -testResults $resultsPath `
        -logFile $logPath

    $unityExitCode = $LASTEXITCODE
    if ($unityExitCode -ne 0 -or -not (Test-Path -LiteralPath $resultsPath)) {
        if (Test-Path -LiteralPath $logPath) {
            Write-Host "Unity smoke test log tail:" -ForegroundColor Yellow
            Get-Content -LiteralPath $logPath -Tail 80
        }

        throw "Unity EditMode smoke tests failed or did not produce a test results file. Exit code: $unityExitCode"
    }

    Write-Host "Unity EditMode smoke tests passed. Results: $resultsPath" -ForegroundColor Green
}

$script:RepoRoot = Get-RepoRoot
Set-Location -LiteralPath $script:RepoRoot

Write-Host "Running project checks in $script:RepoRoot" -ForegroundColor Cyan

if (-not $SkipRuntimeBuild) {
    Invoke-CheckStep "Runtime build" {
        dotnet build $RuntimeProject -v:minimal
    }
}

if (-not $SkipEditorBuild) {
    Invoke-CheckStep "Editor build" {
        dotnet build $EditorProject -v:minimal
    }
}

if (-not $SkipLineCount) {
    Invoke-CheckStep "Line-count" {
        & (Join-Path $script:RepoRoot "tools/check-line-count.ps1") -MaxLines $MaxLines
    }
}

if (-not $SkipDiffCheck) {
    Invoke-CheckStep "Git diff whitespace check" {
        Invoke-GitDiffCheck
    }
}

if (-not $SkipMojibakeScan) {
    Invoke-CheckStep "Mojibake scan" {
        Invoke-MojibakeScan
    }
}

if (-not $SkipSmokeTests) {
    Invoke-CheckStep "Unity EditMode smoke tests" {
        Invoke-UnitySmokeTests
    }
}

Write-Host ""
Write-Host "All requested checks passed." -ForegroundColor Green
