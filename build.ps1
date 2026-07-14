[CmdletBinding()]
param(
    [ValidateSet('AnyCPU', 'x86', 'x64')]
    [string]$Platform = 'AnyCPU'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $root 'src'
$outputDirectory = Join-Path $root 'dist'
$outputPath = Join-Path $outputDirectory 'XButtonMapper.exe'
$manifestPath = Join-Path $root 'app.manifest'

$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)

$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'The built-in .NET Framework C# compiler (csc.exe) was not found.'
}

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$sources = Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName }
if (-not $sources) {
    throw 'No C# source files were found in the src directory.'
}

$compilerArguments = @(
    '/nologo',
    '/target:winexe',
    ('/platform:' + $Platform.ToLowerInvariant()),
    '/optimize+',
    '/debug-',
    '/warn:4',
    '/warnaserror+',
    '/codepage:65001',
    ('/out:' + $outputPath),
    ('/win32manifest:' + $manifestPath),
    '/reference:System.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
) + $sources

Write-Host ('Compiler: ' + $compiler)
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw ('Compilation failed with exit code: ' + $LASTEXITCODE)
}

$selfTestProcess = Start-Process -FilePath $outputPath -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($selfTestProcess.ExitCode -ne 0) {
    throw ('Built-in self-test failed with exit code: ' + $selfTestProcess.ExitCode)
}

$smokeTestProcess = Start-Process -FilePath $outputPath -ArgumentList '--smoke-test' -WindowStyle Hidden -Wait -PassThru
if ($smokeTestProcess.ExitCode -ne 0) {
    throw ('Tray and hook smoke test failed with exit code: ' + $smokeTestProcess.ExitCode)
}

$file = Get-Item -LiteralPath $outputPath
$hash = Get-FileHash -LiteralPath $outputPath -Algorithm SHA256
Write-Host ''
Write-Host 'Build succeeded'
Write-Host ('File: ' + $file.FullName)
Write-Host ('Size: ' + $file.Length + ' bytes')
Write-Host ('SHA256: ' + $hash.Hash)
