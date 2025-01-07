# Set output directory to the parent folder
$parentDir = (Resolve-Path "..").Path

# Set the proto path to the current directory
$protoPath = (Get-Location).Path

# Get all .proto files as relative paths
$protoFiles = Get-ChildItem -Filter *.proto | ForEach-Object { $_.Name }

# Run protogen with proper proto path and relative inputs
& protogen --proto_path="$protoPath" --csharp_out="$parentDir" $protoFiles

# Check for errors
if ($LASTEXITCODE -ne 0) {
    Write-Host "💥 Compilation failed! Check output above." -ForegroundColor Red
    exit 1
}

Write-Host "🎉 All files processed successfully! Check the parent folder for outputs." -ForegroundColor Green
