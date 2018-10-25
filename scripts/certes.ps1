Param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$passthrough
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$cliArchiveName = "certes-cli.zip"
$binPath = "./.certes"
$certesBinPath = "$binPath/bin"
$dotnetBinPath = "$binPath/dotnet"
$cliPath = "$certesBinPath/Certes.Cli.dll"
$cliMinVersion = [Version]"2.0.0"

Function Say($str) {
    Write-Host "certes: $str"
}

Function Init {
    New-Item $binPath -Type Directory -Force | Out-Null
}

Function Check-Cli {
    $releaseInfo = Invoke-WebRequest https://api.github.com/repos/fszlin/certes/releases/latest | ConvertFrom-Json
    $latestVer = [Version]$releaseInfo.tag_name.Substring(1)
    
    # check if we have the latest CLI
    If (Test-Path $cliPath) {
        $currentVer = [Version]([Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $cliPath)).ProductVersion)
        if ($currentVer -ge $latestVer) {
            Return
        }
    }

    Init
    Remove-Item $certesBinPath -Force -Recurse -ErrorAction SilentlyContinue | Out-Null

    $cliInfo = $releaseInfo.assets | `
        Where-Object  { $_.name -eq $cliArchiveName } | `
        Select -ExpandProperty browser_download_url
        
    Say "Downloading link: $cliInfo"
    Invoke-WebRequest $cliInfo -OutFile "$binPath/$cliArchiveName"
    
    Say "Extracting zip from $cliInfo"
    Expand-Archive "$binPath/$cliArchiveName" -DestinationPath $certesBinPath -Force
    
    Remove-Item "$binPath/$cliArchiveName"
}

Function Get-Dotnet {
    # check local dotnet installation
    $dotnetCmd = Get-Command "$dotnetBinPath/dotnet.exe" -ErrorAction SilentlyContinue
    
    # check global dotnet runtime
    If (!$dotnetCmd) {
        $dotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue
    }

    if ((!$dotnetCmd) -Or ($dotnetCmd.Version -le $cliMinVersion)) {
        Init
        Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile "$binPath/dotnet-install.ps1"
        & $binPath/dotnet-install.ps1 -InstallDir $dotnetBinPath -SharedRuntime -NoPath -Channel Current
        $dotnetCmd = Get-Command "$dotnetBinPath/dotnet.exe" -ErrorAction SilentlyContinue
    }

    Return $dotnetCmd.Source
}

Check-Cli
$dotnetExe = Get-Dotnet

& $dotnetExe $cliPath $passthrough
