$windowsZipFile = 'windows-release.zip'

# Source: https://stackoverflow.com/questions/44074121/build-net-core-console-application-to-output-an-exe
# Publish to an exe + dependencies. 40MB baseline.
dotnet publish -c Release -r win10-x64 -o out_windows

# Zip it up. ~17MB baseline.
if (Test-Path($windowsZipFile)) {
    Remove-Item $windowsZipFile
}

Add-Type -A 'System.IO.Compression.FileSystem'
[IO.Compression.ZipFile]::CreateFromDirectory('AliTheAndroid\out_windows', $windowsZipFile);
Write-Host DONE! Zipped to $windowsZipFile

# for Linux builds!
# dotnet publish -c Release -r ubuntu.16.10-x64 -o out_linux