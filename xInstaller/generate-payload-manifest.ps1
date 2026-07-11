param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "Package directory was not found: $PackageDirectory"
}

$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$dataRoot    = Join-Path -Path $packageRoot -ChildPath 'data'

if (-not (Test-Path -LiteralPath $dataRoot -PathType Container)) {
    throw "Installer data directory was not found: $dataRoot"
}

$manifestPath = Join-Path -Path $dataRoot -ChildPath 'payload.sha256'
$manifestFullPath = [System.IO.Path]::GetFullPath($manifestPath)

# Force array semantics even when zero or one file is returned.
$files = @(
    Get-ChildItem -LiteralPath $dataRoot -File -Recurse |
        Where-Object {
            [System.IO.Path]::GetFullPath($_.FullName) -ne $manifestFullPath
        } |
        Sort-Object FullName
)

$lines = @(
    foreach ($file in $files) {
        # Compatible with Windows PowerShell 5.1, where Path.GetRelativePath
        # is not necessarily available.
        $relativePath = $file.FullName.Substring($packageRoot.Length)
        $relativePath = $relativePath.TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        )
        $relativePath = $relativePath.Replace('\', '/')

        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()

        "$hash  $relativePath"
    }
)

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($manifestPath, [string[]]$lines, $utf8WithoutBom)

Write-Output "Wrote $($lines.Count) hashes to $manifestPath"