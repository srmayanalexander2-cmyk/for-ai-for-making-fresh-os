# Run on an interactive Windows desktop after build.cmd. Does not edit user files.
param([string]$Exe = "$env:USERPROFILE\Downloads\freshos\FreshOS.exe")
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$process=Start-Process $Exe -PassThru
function Wait-Element([string]$id, [int]$Seconds=15) {
    $deadline=(Get-Date).AddSeconds($Seconds)
    do {
        $process.Refresh()
        if ($process.HasExited) { throw 'FreshOS exited unexpectedly.' }
        if ($process.MainWindowHandle -ne 0) {
            $window=[System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
            $condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id)
            $element=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
            if ($element) { return $element }
        }
        Start-Sleep -Milliseconds 150
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for UI automation ID: $id"
}
function Invoke-Element($element) {
    $pattern=$element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}
try {
    Start-Sleep -Seconds 3
    # First-run onboarding is optional; don't modify setup preferences by hand.
    try { Invoke-Element (Wait-Element 'FreshOS.Welcome.Start' 1) } catch { }
    Invoke-Element (Wait-Element 'FreshOS.Launcher')
    Invoke-Element (Wait-Element 'FreshOS.Settings')
    $theme=Wait-Element 'FreshOS.Theme'
    $theme.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
    # Theme rebuild must produce a fresh, still-responsive settings tree.
    Start-Sleep -Milliseconds 500
    $theme=Wait-Element 'FreshOS.Theme'
    $theme.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
    Write-Host 'PASS: launch, onboarding/launcher, settings, theme rebuild and restore.' -ForegroundColor Green
}
finally {
    if (-not $process.HasExited) { $null=$process.CloseMainWindow(); $null=$process.WaitForExit(5000) }
    if (-not $process.HasExited) { Write-Warning 'FreshOS did not exit promptly; close it with F12.' }
}
