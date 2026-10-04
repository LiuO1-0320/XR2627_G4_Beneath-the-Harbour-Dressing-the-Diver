$ErrorActionPreference = 'Stop'
$assets = Join-Path $PSScriptRoot 'XRG4_BeneathTheHarber/Assets'
$scene = Get-Content (Join-Path $assets 'Scenes/BasicScene.unity') -Raw
$dir = Join-Path $assets 'UnderwaterTour'
New-Item -ItemType Directory -Force $dir | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Save($path, $value) { [IO.File]::WriteAllText($path, $value, $utf8) }
Save "$dir.meta" "fileFormatVersion: 2`nguid: b07aa4e923a447cda493c7870d5a1e01`nfolderAsset: yes`n"
$palette = @{
 Sand = '0.36, g: 0.46, b: 0.39'; Rock = '0.12, g: 0.23, b: 0.25'
 Water = '0.025, g: 0.24, b: 0.32'; Kelp = '0.08, g: 0.34, b: 0.24'
 Coral = '0.72, g: 0.32, b: 0.22'; Rust = '0.28, g: 0.18, b: 0.12'
 Path = '0.30, g: 0.57, b: 0.57'; Glow = '0.25, g: 0.85, b: 0.83'
 Fish = '0.78, g: 0.65, b: 0.26'
}
$materials = @{}
$source = Get-Content (Join-Path $assets 'Scenes/BasicScene/Grid.mat') -Raw
foreach ($name in $palette.Keys) {
 $guid = [guid]::NewGuid().ToString('N'); $materials[$name] = $guid
 $mat = $source.Replace('m_Name: Grid', "m_Name: $name")
 $mat = $mat -replace 'm_Texture: \{fileID: 2800000, guid: [a-f0-9]+, type: 3\}', 'm_Texture: {fileID: 0}'
 $mat = $mat -replace '(_BaseColor|_Color): \{r: 1, g: 1, b: 1, a: 1\}', ('$1: {r: ' + $palette[$name] + ', a: 1}')
 if ($name -eq 'Glow') { $mat = $mat.Replace('_EmissionColor: {r: 0, g: 0, b: 0, a: 1}', '_EmissionColor: {r: 0.12, g: 0.6, b: 0.55, a: 1}') }
 Save (Join-Path $dir "$name.mat") $mat
 Save (Join-Path $dir "$name.mat.meta") "fileFormatVersion: 2`nguid: $guid`n"
}
$template = [regex]::Match($scene, '(?s)--- !u!1 &879046125\r?\n.*?(?=--- !u!1 &1249617917)').Value
if (!$template) {
 $original = (git show HEAD:XRG4_BeneathTheHarber/Assets/Scenes/BasicScene.unity) -join "`n"
 $template = [regex]::Match($original, '(?s)--- !u!1 &879046125\r?\n.*?(?=--- !u!1 &1249617917)').Value
}
if (!$template) { throw 'Primitive template is unavailable.' }
$teleport = [regex]::Match($scene, '(?s)--- !u!114 &1249617922\r?\n.*?(?=--- !u!)').Value
$blocks = New-Object Text.StringBuilder
$children = New-Object Text.StringBuilder
$script:nextId = 710000010
function Shape($name, $pos, $scale, $material, $mesh = 10202, $walk = $false, [double]$angle = 0, $solid = $true) {
 $id = $script:nextId; $script:nextId += 10
 $block = $template
 for ($i=0; $i -lt 5; $i++) { $block = $block.Replace([string](879046125+$i), [string]($id+$i)) }
 $block = $block.Replace('m_Name: Cube1', "m_Name: $name")
 $block = $block -replace 'm_LocalPosition: \{[^}]+\}', "m_LocalPosition: {x: $($pos[0]), y: $($pos[1]), z: $($pos[2])}"
 $block = $block -replace 'm_LocalScale: \{[^}]+\}', "m_LocalScale: {x: $($scale[0]), y: $($scale[1]), z: $($scale[2])}"
 $s = [Math]::Sin($angle*[Math]::PI/360); $c = [Math]::Cos($angle*[Math]::PI/360)
 $block = $block.Replace('m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}', "m_LocalRotation: {x: 0, y: 0, z: $s, w: $c}")
 $block = $block.Replace('m_Father: {fileID: 0}', 'm_Father: {fileID: 710000004}')
 $block = $block.Replace('guid: 31321ba15b8f8eb4c954353edc038b1d', "guid: $($materials[$material])")
 $block = $block.Replace('m_Mesh: {fileID: 10202', "m_Mesh: {fileID: $mesh")
 if (!$solid) {
  $block = $block -replace "  - component: \{fileID: $($id+1)\}\r?\n", ''
  $block = $block -replace "(?s)--- !u!65 &$($id+1)\r?\n.*?(?=--- !u!)", ''
 }
 if ($walk) {
  $block = $block.Replace("  - component: {fileID: $($id+4)}", "  - component: {fileID: $($id+4)}`n  - component: {fileID: $($id+5)}")
  $block += $teleport.Replace('1249617922', [string]($id+5)).Replace('1249617917', [string]$id)
 }
 [void]$blocks.Append($block); [void]$children.AppendLine("  - {fileID: $($id+4)}")
}
# Local origin is bay centre; entry is x=-28, floor is y=-6.
Shape 'Seabed_Teleport' @(0,-6.3,0) @(32,0.6,32) Sand 10202 $true
Shape 'Descent_Ramp_Teleport' @(-22,-3.18,0) @(13.42,0.35,4) Path 10202 $true -26.565
Shape 'Entry_Deck_Teleport' @(-29,-0.2,0) @(2,0.4,5) Path 10202 $true
Shape 'Water_Surface' @(0,0.3,0) @(32,0.12,32) Water 10202 $false 0 $false
Shape 'Far_Blue_Backdrop' @(16,-3,0) @(0.4,6.6,32) Water
Shape 'North_Blue_Backdrop' @(0,-3,16) @(32,6.6,0.4) Water
Shape 'South_Blue_Backdrop' @(0,-3,-16) @(32,6.6,0.4) Water
for ($i=0; $i -lt 7; $i++) {
 Shape "Route_Marker_$i" @((-13+$i*4),-5.96,0) @(0.3,0.05,0.8) Glow 10202 $false 0 $false
}
for ($i=0; $i -lt 18; $i++) {
 $x=-13+($i%6)*5; $z= if ($i%2 -eq 0) { 8+($i%3) } else { -8-($i%3) }
 Shape "Reef_Rock_$i" @($x,-5.65,$z) @(2.4,1.1,1.8) Rock 10207
 for ($j=0; $j -lt 3; $j++) {
  Shape "Seaweed_${i}_$j" @(($x-0.7+$j*0.6),-4.8,($z+1)) @(0.15,2.3,0.12) Kelp 10202 $false (-12+$j*11) $false
 }
 if ($i%3 -eq 0) { Shape "Coral_$i" @(($x+1),-5.2,($z-1)) @(0.65,1.3,0.65) Coral 10207 $false 0 $false }
}
for ($i=0; $i -lt 16; $i++) {
 Shape "Fish_$i" @((-8+($i%8)*2),(-2.4-($i%3)*0.45),(5+[Math]::Floor($i/8)*2)) @(0.18,0.30,0.8) Fish 10207 $false 0 $false
}
for ($i=0; $i -lt 4; $i++) {
 Shape "Harbour_Piling_$i" @((5+$i*2),-3.4,-9) @(0.5,5.2,0.5) Rust
 Shape "Ruined_Beam_$i" @((6+$i*2),-1,-9) @(2.5,0.4,0.6) Rust
}
Shape 'Wreck_Hull' @(5,-5.3,8) @(5,1.2,2.5) Rust
Shape 'Wreck_Mast' @(5,-3.4,8) @(0.2,4,0.2) Rust
Shape 'Observation_Platform_Teleport' @(1,-5.9,-4) @(5,0.2,4) Path 10202 $true
$root = @"
%YAML 1.1
%TAG !u! tag:yousandi.cn,2023:
--- !u!1 &710000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 710000004}
  - component: {fileID: 710000005}
  m_Layer: 0
  m_Name: Underwater Harbour Tour
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &710000004
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 710000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children:
$($children.ToString().TrimEnd())
  m_Father: {fileID: 0}
--- !u!114 &710000005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 710000000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a07aa4e923a447cda493c7870d5a1e01, type: 3}
  m_Name:
  m_EditorClassIdentifier:

"@
Save (Join-Path $dir 'UnderwaterHarbourTour.prefab') ($root+$blocks.ToString())
Save (Join-Path $dir 'UnderwaterHarbourTour.prefab.meta') "fileFormatVersion: 2`nguid: c07aa4e923a447cda493c7870d5a1e01`n"
Write-Output "Created underwater tour prefab with $($children.ToString().Split([char]10).Count-1) objects."
