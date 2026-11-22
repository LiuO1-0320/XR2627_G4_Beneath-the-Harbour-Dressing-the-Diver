$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
$compiler = Get-ChildItem (Join-Path $env:ProgramFiles 'dotnet/sdk') -Directory |
    Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'Roslyn/bincore/csc.dll' } |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$compiler) { throw 'Install a .NET SDK to run the HTTP transport tests.' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$outputDirectory = Join-Path $repo 'XRG4_BeneathTheHarber/Temp'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
$output = Join-Path $outputDirectory 'StaffMonitorHttpTests.exe'
& $dotnet $compiler /nologo /target:exe "/out:$output" "/r:$framework/mscorlib.dll" "/r:$framework/System.dll" "/r:$framework/System.Core.dll" (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/StaffMonitorServer.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/StaffWebCommandQueue.cs') (Join-Path $PSScriptRoot 'StaffMonitorHttpTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Staff monitor HTTP test compilation failed.' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Staff monitor HTTP tests failed.' }
