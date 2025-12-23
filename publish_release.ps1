# Publish Script for Attendify
$ErrorActionPreference = "Stop"

$rootPath = $PSScriptRoot
$publishPath = Join-Path $rootPath "Setup\Publish"
$apiProject = Join-Path $rootPath "Attendify.API\Attendify.API.csproj"
$uiProject = Join-Path $rootPath "Attendify\Attendify.UI.csproj"

Write-Host "Cleaning previous publish output..."
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

Write-Host "Publishing API Project..."
dotnet publish $apiProject -c Release -r win-x64 --self-contained true -o (Join-Path $publishPath "API")

Write-Host "Publishing UI Project..."
dotnet publish $uiProject -c Release -r win-x64 --self-contained true -o (Join-Path $publishPath "UI")

Write-Host "Publish completed successfully!"
Write-Host "Output located at: $publishPath"
