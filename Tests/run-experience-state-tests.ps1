$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
$sdkRoot = Join-Path $env:ProgramFiles 'dotnet/sdk'
$compiler = Get-ChildItem $sdkRoot -Directory | Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'Roslyn/bincore/csc.dll' } | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$compiler) { throw 'A .NET SDK is required to run these tests.' }
$outputDirectory = Join-Path $repo 'XRG4_BeneathTheHarber/Temp'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
$output = Join-Path $outputDirectory 'SessionStateTests.exe'
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
& $dotnet $compiler /nologo /target:exe "/out:$output" "/r:$framework/mscorlib.dll" "/r:$framework/System.dll" "/r:$framework/System.Core.dll" (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/SessionState.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/ControlProtocol.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/DiveShaftGeometry.cs') (Join-Path $PSScriptRoot 'SessionStateTests.cs') (Join-Path $PSScriptRoot 'GazeTests.cs') (Join-Path $PSScriptRoot 'ResetTests.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/ResetProtocol.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/GazeDwell.cs') (Join-Path $PSScriptRoot 'CommandTests.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/AudioPolicy.cs') (Join-Path $PSScriptRoot 'AudioArrivalTests.cs') (Join-Path $PSScriptRoot 'BriefingPracticeTests.cs') (Join-Path $repo 'XRG4_BeneathTheHarber/Assets/script/BriefingPractice.cs')
if ($LASTEXITCODE -ne 0) { throw 'State test compilation failed.' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'State tests failed.' }

