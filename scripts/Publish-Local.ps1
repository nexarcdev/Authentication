param(
    [string]$Configuration = "Release",
    [string]$SourceName = "LocalPackages",
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\\publish")
)

$ErrorActionPreference = "Stop"

if (Test-Path -LiteralPath $OutputDir) {
    $OutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
} else {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

Write-Host "Packing to $OutputDir ..."
dotnet pack -c $Configuration -o $OutputDir -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

Write-Host "Pushing .nupkg to source '$SourceName' ..."
Get-ChildItem -Path $OutputDir -Filter "*.nupkg" |
    Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
    ForEach-Object {
        dotnet nuget push $_.FullName -s $SourceName
    }

Write-Host "Pushing .snupkg to source '$SourceName' ..."
Get-ChildItem -Path $OutputDir -Filter "*.snupkg" |
    ForEach-Object {
        dotnet nuget push $_.FullName -s $SourceName
    }

Write-Host "Done."
