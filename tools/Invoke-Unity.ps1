param(
    [ValidateSet('EditMode', 'PlayMode', 'Presentation', 'AndroidDevelopment', 'AndroidReleaseCheck')]
    [string]$Task = 'EditMode',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe',
    [string]$ProjectPath,
    [int]$TimeoutSeconds = 1800,
    [switch]$CaptureUi
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$ProjectPath) { $ProjectPath = Join-Path $repoRoot "FunGuy's" }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$outputDirectory = Join-Path $repoRoot "artifacts/$Task"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity not found: $UnityPath" }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Close the Unity editor before running batch validation.' }
$arguments = @('-batchmode', '-projectPath', $ProjectPath, '-logFile', (Join-Path $outputDirectory 'unity.log'))
if ($CaptureUi) {
    if ($Task -ne 'PlayMode') { throw 'CaptureUi is supported only for PlayMode.' }
    $arguments += @('-screen-width', '2400', '-screen-height', '1080')
} else { $arguments += '-nographics' }
if ($Task -eq 'Presentation') {
    $arguments += @('-executeMethod', 'PresentationAssets.Rebuild', '-quit')
} elseif ($Task -in @('EditMode', 'PlayMode')) {
    $arguments += @('-runTests', '-testPlatform', $Task, '-testResults', (Join-Path $outputDirectory 'tests.xml'))
} else {
    $arguments += @('-buildTarget', 'Android', '-executeMethod', "BuildAutomation.$Task", '-quit')
}
# The test package deletes these tracked resources. Preserve their exact pre-run bytes.
$resourceBackup = @{}
foreach ($name in @('PerformanceTestRunInfo.json', 'PerformanceTestRunInfo.json.meta', 'PerformanceTestRunSettings.json', 'PerformanceTestRunSettings.json.meta')) {
    $resourcePath = Join-Path $ProjectPath "Assets/Resources/$name"
    if (Test-Path -LiteralPath $resourcePath) { $resourceBackup[$resourcePath] = [IO.File]::ReadAllBytes($resourcePath) }
}
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = $UnityPath
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$start.Environment['FUNGUY_BUILD_OUTPUT'] = Join-Path $outputDirectory 'Funguy.apk'
if ($CaptureUi) { $start.Environment['FUNGUY_UI_CAPTURES'] = Join-Path $outputDirectory 'screenshots' }
if ($Task -in @('AndroidDevelopment', 'AndroidReleaseCheck')) {
    # Keep project builds independent of stale or unwritable user-wide Gradle daemon state.
    $start.Environment['GRADLE_USER_HOME'] = Join-Path $repoRoot 'artifacts/gradle'
}
$process = $null
$startedUtc = [DateTime]::UtcNow
try {
    $process = [Diagnostics.Process]::Start($start)
    Write-Output "Started $Task. Log: $(Join-Path $outputDirectory 'unity.log')"
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) { $process.Kill($true); throw "Unity exceeded $TimeoutSeconds seconds; see log." }
    if ($process.ExitCode -ne 0) { throw "Unity $Task exited with code $($process.ExitCode); see log/results." }
    if ($Task -eq 'Presentation') {
        if (!(Test-Path -LiteralPath (Join-Path $ProjectPath 'Assets/_Game/Resources/Presentation/BattleScreen.prefab'))) { throw 'Battle prefab was not created.' }
        Write-Output 'Battle presentation prefabs authored.'
    } elseif ($Task -in @('EditMode', 'PlayMode')) {
        $resultPath = Join-Path $outputDirectory 'tests.xml'
        if (!(Test-Path -LiteralPath $resultPath) -or (Get-Item -LiteralPath $resultPath).LastWriteTimeUtc -lt $startedUtc) { throw 'Unity did not produce fresh test results.' }
        [xml]$results = Get-Content -LiteralPath $resultPath
        $run = $results.'test-run'
        if ([int]$run.total -eq 0 -or $run.result -ne 'Passed' -or [int]$run.failed -gt 0) { throw 'Test run was empty or failed.' }
        Write-Output "$($run.passed)/$($run.total) tests passed."
    } else {
        foreach ($name in @('Funguy.apk', 'Funguy.apk.summary.txt')) {
            $artifactPath = Join-Path $outputDirectory $name
            if (!(Test-Path -LiteralPath $artifactPath) -or (Get-Item -LiteralPath $artifactPath).LastWriteTimeUtc -lt $startedUtc) { throw "Unity did not produce a fresh $name." }
        }
        Write-Output "Built $(Join-Path $outputDirectory 'Funguy.apk')"
    }
} finally {
    foreach ($entry in $resourceBackup.GetEnumerator()) { [IO.File]::WriteAllBytes($entry.Key, $entry.Value) }
    if ($process) { $process.Dispose() }
}
