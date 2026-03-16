Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = $PSCommandPath
if (-not $scriptPath) {
	throw 'Unable to determine script path for self-elevation.'
}

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($currentIdentity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
	Write-Host 'Requesting elevation (Administrator) to run the converter...' -ForegroundColor Yellow
	$hostExe = (Get-Process -Id $PID).Path
	$arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $scriptPath)
	$proc = Start-Process -FilePath $hostExe -Verb RunAs -ArgumentList $arguments -Wait -PassThru
	exit $proc.ExitCode
}

$repoRoot = Split-Path -Parent $scriptPath
$savesRoot = Join-Path $repoRoot 'Saves'
$converterDir = Join-Path $repoRoot 'Release\ImperatorToCK3'
$converterExe = Join-Path $converterDir 'ImperatorToCK3Converter.exe'
$configPath = Join-Path $converterDir 'configuration.txt'
$reportPath = Join-Path $repoRoot ('save-conversion-test-report-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')

if (-not (Test-Path -LiteralPath $savesRoot)) {
	throw "Saves directory not found: $savesRoot"
}
if (-not (Test-Path -LiteralPath $converterExe)) {
	throw "Converter executable not found: $converterExe"
}
if (-not (Test-Path -LiteralPath $configPath)) {
	throw "Configuration file not found: $configPath"
}

$saveFiles = Get-ChildItem -LiteralPath $savesRoot -File -Recurse | Sort-Object FullName
if ($saveFiles.Count -eq 0) {
	Write-Host "No files found under $savesRoot. Nothing to test." -ForegroundColor Yellow
	exit 0
}

$originalConfig = Get-Content -LiteralPath $configPath -Raw
if ([string]::IsNullOrWhiteSpace($originalConfig)) {
	throw "Configuration file is empty: $configPath"
}
if ($originalConfig -notmatch '(?m)^\s*SaveGame\s*=') {
	throw "SaveGame entry not found in $configPath"
}

$results = [System.Collections.Generic.List[object]]::new()
$runStart = Get-Date
$shouldRestoreConfig = $true

Write-Host "Testing $($saveFiles.Count) save file(s)." -ForegroundColor Cyan

try {
	foreach ($saveFile in $saveFiles) {
		$start = Get-Date
		$relativeSavePath = [System.IO.Path]::GetRelativePath($savesRoot, $saveFile.FullName)
		$savePath = $saveFile.FullName -replace '\\', '/'
		$updatedConfig = [regex]::Replace(
			$originalConfig,
			'(?m)^\s*SaveGame\s*=\s*".*"\s*$',
			('SaveGame = "' + $savePath + '"'),
			1
		)
		Set-Content -LiteralPath $configPath -Value $updatedConfig -Encoding UTF8

		Write-Host ("[{0}] Running converter for: {1}" -f (Get-Date -Format 'HH:mm:ss'), $relativeSavePath)

		$stdoutPath = [System.IO.Path]::GetTempFileName()
		$stderrPath = [System.IO.Path]::GetTempFileName()
		$exitCode = -1
		$threwException = $false
		$failureReason = ''
		$outputSnippet = ''
		$locationPushed = $false

		try {
			Push-Location -LiteralPath $converterDir
			$locationPushed = $true
			$proc = Start-Process -FilePath '.\ImperatorToCK3Converter.exe' -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
			$exitCode = $proc.ExitCode
		}
		catch {
			$threwException = $true
			$failureReason = "Process launch failed: $($_.Exception.Message)"
		}
		finally {
			if ($locationPushed) {
				Pop-Location
			}
		}

		$stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { '' }
		$stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { '' }
		Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue

		$combinedOutput = ($stdout + "`n" + $stderr).Trim()
		$exceptionMatch = [regex]::Match($combinedOutput, '(?is)(Unhandled\s+exception|\bException\b[^\r\n]*)')
		if ($exceptionMatch.Success) {
			$threwException = $true
			if (-not $failureReason) {
				$failureReason = 'Exception text detected in converter output.'
			}
			$outputSnippet = $exceptionMatch.Value
		}

		$passed = (-not $threwException) -and ($exitCode -eq 0)
		if (-not $passed -and -not $failureReason) {
			$failureReason = "Non-zero exit code: $exitCode"
		}
		if (-not $outputSnippet -and $combinedOutput.Length -gt 0) {
			$outputSnippet = $combinedOutput.Substring(0, [Math]::Min(400, $combinedOutput.Length))
		}

		$duration = [Math]::Round(((Get-Date) - $start).TotalSeconds, 2)
		$result = [PSCustomObject]@{
			SaveFile = $relativeSavePath
			SaveFileFullPath = $saveFile.FullName
			Passed = $passed
			ExitCode = $exitCode
			ThrewException = $threwException
			FailureReason = $failureReason
			OutputSnippet = $outputSnippet
			DurationSeconds = $duration
		}
		$results.Add($result) | Out-Null

		if ($passed) {
			Write-Host ("  PASS ({0}s)" -f $duration) -ForegroundColor Green
		}
		else {
			Write-Host ("  FAIL ({0}s): {1}" -f $duration, $failureReason) -ForegroundColor Red
		}
	}
}
finally {
	if ($shouldRestoreConfig -and $null -ne $originalConfig) {
		Set-Content -LiteralPath $configPath -Value $originalConfig -Encoding UTF8
	}
}

$totalDuration = [Math]::Round(((Get-Date) - $runStart).TotalSeconds, 2)
$failed = @($results | Where-Object { -not $_.Passed })
$passedCount = $results.Count - $failed.Count

$report = [PSCustomObject]@{
	GeneratedAt = (Get-Date).ToString('o')
	SaveCount = $results.Count
	PassedCount = $passedCount
	FailedCount = $failed.Count
	DurationSeconds = $totalDuration
	Results = $results
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Host ''
Write-Host '=== Conversion Test Summary ===' -ForegroundColor Cyan
Write-Host ("Total:  {0}" -f $results.Count)
Write-Host ("Passed: {0}" -f $passedCount) -ForegroundColor Green
if ($failed.Count -gt 0) {
	Write-Host ("Failed: {0}" -f $failed.Count) -ForegroundColor Red
	Write-Host 'Failed saves:' -ForegroundColor Red
	foreach ($item in $failed) {
		Write-Host ("  - {0} :: {1}" -f $item.SaveFile, $item.FailureReason)
	}
}
else {
	Write-Host ("Failed: {0}" -f $failed.Count)
}
Write-Host ("Report: {0}" -f $reportPath)

if ($failed.Count -gt 0) {
	exit 1
}

exit 0
