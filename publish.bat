cd AbdullahTheWarrior

REM Source: https://stackoverflow.com/questions/44074121/build-net-core-console-application-to-output-an-exe
dotnet publish -c Release -r win10-x64 -o out_windows

del windows-release.zip
powershell.exe -nologo -noprofile -command "& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::CreateFromDirectory('AliTheAndroid\out_windows', 'windows-release.zip'); }"
echo DONE. Output is in the directory AbdullahTheWarrior/out_windows

REM for Linux builds!
REM dotnet publish -c Release -r ubuntu.16.10-x64 -o out_linux