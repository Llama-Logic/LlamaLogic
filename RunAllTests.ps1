$resultsDirectory = Join-Path $PSScriptRoot 'TestResults'
Get-ChildItem -Path $resultsDirectory -Filter '*.trx' -ErrorAction SilentlyContinue | Remove-Item
foreach ($testProject in Get-ChildItem -Path $PSScriptRoot -Recurse -Filter '*.UnitTests.csproj')
{
    Write-Host "Testing $($testProject.BaseName)" -ForegroundColor Cyan
    dotnet test $testProject.FullName --logger "trx;LogFilePrefix=$($testProject.BaseName)" --results-directory $resultsDirectory
}
Get-ChildItem -Path $resultsDirectory -Directory -Filter 'Deploy_*' -ErrorAction SilentlyContinue | Remove-Item -Recurse