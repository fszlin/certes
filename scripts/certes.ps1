Param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$passthrough
)

$cliArchiveName = "certes-cli.zip"
$binPath = "./.certes"
$certesBinPath = "$binPath/bin"
$dotnetBinPath = "$binPath/dotnet"
$certesVerPath = "$binPath/ver"
$cliPath = "$certesBinPath/Certes.Cli.dll"
$cliMinVersion = [Version]"1.0.1"
$updateCheckDays = -7

Function Init {
    New-Item $binPath -Type Directory -Force | Out-Null
}

Function Get-Cli {
    If (Test-Path $cliPath) {
        $pubDate = [DateTime](Get-Content -Raw $certesVerPath -ErrorAction SilentlyContinue)
        if (($pubDate) -And ($pubDate -gt (Get-Date).AddDays($updateCheckDays))) {
            Return;
        }
    }

    Init
    Remove-Item $certesBinPath -Force -Recurse -ErrorAction SilentlyContinue | Out-Null

    $releaseInfo = Invoke-WebRequest https://api.github.com/repos/fszlin/certes/releases/latest | ConvertFrom-Json

    $cliInfo = $releaseInfo.assets | `
        Where-Object  { $_.name -eq $cliArchiveName } | `
        Select -ExpandProperty browser_download_url
    Invoke-WebRequest $cliInfo -OutFile "$binPath/$cliArchiveName"
    Expand-Archive "$binPath/$cliArchiveName" -DestinationPath $certesBinPath -Force
    
    New-Item $certesVerPath -Type File -Value (Get-Date -Format s) -Force | Out-Null
}

Function Get-Dotnet {
    $dotnetCmd = Get-Command "$dotnetBinPath/dotnet.exe" -ErrorAction SilentlyContinue

    If (!$dotnetCmd) {
        $dotnetCmd = Get-Command "dotnet" -ErrorAction SilentlyContinue
    }

    if ((!$dotnetCmd) -Or ($dotnetCmd.Version -le $cliMinVersion)) {
        Init
        Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile "$binPath/dotnet-install.ps1"
        & $binPath/dotnet-install.ps1 -InstallDir $dotnetBinPath -NoPath
        $dotnetCmd = Get-Command "$dotnetBinPath/dotnet.exe" -ErrorAction SilentlyContinue
    }

    Return $dotnetCmd.Source
}

Get-Cli
$dotnetExe = Get-Dotnet

& $dotnetExe $cliPath $passthrough
