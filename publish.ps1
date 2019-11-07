$ErrorActionPreference = 'Stop'
$release = "v1.0.0"

$platforms = @('Windows', 'Linux', 'MacOS')
$rids = @{
    'Windows' = 'win-x64';
    'Linux' = 'linux-x64';
    'MacOS' = 'osx-x64';
}
$dirSeparator = [IO.Path]::DirectorySeparatorChar
$publishDir = "AliTheAndroid" + $dirSeparator + "publish"

foreach ($platform in $platforms)
{
    $zipFile = "AliTheAndroid-$platform-$release.zip"

    if (Test-Path($publishDir))
    {
        Remove-Item $publishDir -Recurse
    }

    # Source: https://stackoverflow.com/questions/44074121/build-net-core-console-application-to-output-an-exe
    # Publish to an exe + dependencies. 40MB baseline.
    dotnet publish -c Release -r $rids[$platform] -o publish

    $command = ("chmod a+x $publishDir" + $dirSeparator + 'AliTheAndroid')
    if ($platform -eq 'Windows')
    {
        $command += ".exe"
    }
    Invoke-Expression $command

    # Copy all content over since we're not using the MonoGame content pipeline
    foreach ($folder in @('Content', 'Fonts'))
    {
        Copy-Item -Recurse ("AliTheAndroid" + $dirSeparator + $folder) ($publishDir + $dirSeparator + $folder)
    }

    # Zip it up. ~17MB baseline.
    if (Test-Path($zipFile))
    {
        Remove-Item $zipFile
    }

    Add-Type -A 'System.IO.Compression.FileSystem'
    [IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipFile);
    Write-Host DONE! Zipped to $zipFile
}