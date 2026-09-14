param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $ProjectRoot "publish"
$ReleaseDir = Join-Path $ProjectRoot "releases"

$PackId = "Ecomati"
$PackTitle = "AJIAL MOUNASABAT"
$ProjectFile = "locationezzair.csproj"
$MainExe = "locationezzair.exe"

Push-Location $ProjectRoot
try {
    dotnet publish $ProjectFile `
        -c Release `
        --self-contained `
        -r win-x64 `
        -o $PublishDir `
        /p:Version=$Version

    $IconPath = Join-Path $ProjectRoot "Assets\Ecomati.ico"

    vpk pack `
        --packId $PackId `
        --packTitle $PackTitle `
        --packVersion $Version `
        --packDir $PublishDir `
        --mainExe $MainExe `
        --icon $IconPath `
        --outputDir $ReleaseDir
}
finally {
    Pop-Location
}

Write-Host "Release artifacts written to $ReleaseDir"
