$projectPath = ".\LlamaLogic.EpicTuningTime.csproj"
$outputDir = ".\AppxPackages\"
$thumbprint = "094122F38A3B4268F6A3C11E07E18B00046C9E04"

Write-Progress -Activity "Build for Windows" -Status "Getting Version" -PercentComplete 20
$csprojContent = Get-Content $projectPath -Raw
if ($csprojContent -match '<ApplicationVersion>(.*?)</ApplicationVersion>') {
    $version = $matches[1]
} else {
    Write-Error "Could not find ApplicationVersion in $projectPath"
    exit 1
}

foreach ($architecture in @("x64", "arm64")) {
    Write-Progress -Activity "Build for Windows" -Status "Building $version $architecture" -PercentComplete $(if ($architecture -eq "x64") { 40 } else { 60 })
    dotnet publish $projectPath -c Release /p:RuntimeIdentifier=win-$architecture /p:AppxPackageDir=$outputDir
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    $package = Get-ChildItem -Path $outputDir -Recurse -Filter "*_${version}.0_${architecture}.msix" | Where-Object { $_.DirectoryName -like "*_Test" } | Select-Object -First 1
    if ($null -eq $package) {
        Write-Error "Could not find the $architecture package in $outputDir"
        exit 1
    }
    Move-Item -Path $package.FullName -Destination "${outputDir}EpicTuningTime_${version}_${architecture}.msix" -Force
}

Write-Progress -Activity "Build for Windows" -Status "Removing $version Workspace" -PercentComplete 80
Get-ChildItem -Path $outputDir -Directory -Filter "*_${version}.0_Test" | Remove-Item -Recurse

Write-Progress -Activity "Build for Windows" -Completed -Status "Built $version for x64 and arm64"