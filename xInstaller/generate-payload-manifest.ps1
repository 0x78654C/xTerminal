param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory
)

$packageRoot = [System.IO.Path]::GetFullPath($PackageDirectory)
$dataRoot = Join-Path $packageRoot 'data'
if (-not (Test-Path -LiteralPath $dataRoot -PathType Container)) {
    throw "Installer data directory was not found: $dataRoot"
}

$manifestPath = Join-Path $dataRoot 'payload.sha256'
$files = Get-ChildItem -LiteralPath $dataRoot -File -Recurse |
    Where-Object { $_.FullName -ne $manifestPath } |
    Sort-Object FullName

$lines = foreach ($file in $files) {
    $relativePath = [System.IO.Path]::GetRelativePath($packageRoot, $file.FullName).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    "$hash  $relativePath"
}

[System.IO.File]::WriteAllLines($manifestPath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Output "Wrote $($lines.Count) hashes to $manifestPath"
