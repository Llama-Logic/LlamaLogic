dotnet publish --configuration Release --verbosity normal
& "${env:ProgramFiles(x86)}/Windows Kits/10/bin/10.0.22621.0/x64/signtool.exe" sign /sha1 42C9C862153CCA1FBCA9833001935D52A79003E8 /tr http://ts.ssl.com /td sha256 /fd sha256 "bin\Release\net8.0-windows\win-x64\publish\LlamaPad.exe"
