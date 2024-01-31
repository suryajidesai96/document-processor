<#
Prerequisites: The core deployment modules (available from the SVN deployment repo), must be installed on your machine to use this script.

This file is not copied with the deployment to the release folder. It's used to create the releases themselves.
Run it from within the Deploy folder (or Right Click -> Run with Powershell).

The only parameters define how to increment the version number which is specified in version.txt within the same folder.

Feel free to customise this from project to project, that's the whole point of having it in the project SVN.

By default the script references the package.settings.ps1 which contains things such as the local path to your project and the releases output folder.

NuGet is called explicitly to restore NuGet references, so you shouldn't need to have all the packages restored already for this to work.
#>

param (
    [switch][Alias('m')]$major = $false,
    [switch][Alias('mi')]$minor = $false,
    [switch][Alias('b')]$build = $false,
    [switch][Alias('i')]$interactive = $false
 )

$DebugPreference = "Continue";

$settings = Join-Path $PSScriptRoot "package.settings.ps1";

Import-Module Get-Version;
Import-Module Restore-NuGet;

. $settings;

$out = $localSettings.localReleaseFolder;

$versionFilePath = Join-Path $PSScriptRoot $localSettings.versionFile;
Write-Debug "Using version file $versionFilePath";

# Get the next version number
$version = Get-Version `
    -versionFile $versionFilePath `
    -newMajor $major `
    -newMinor $minor `
    -newBuild $build;

Write-Debug "Version set as $version";

$tempPath = Split-Path $PSScriptRoot -Parent;
$deployScriptPath = Join-Path $tempPath "Deploy";
$packageScriptPath = Join-Path $tempPath "Package";
$nuGetPath = Join-Path $packageScriptPath "nuget.exe";

# Create the output folders
$releasePath = Join-Path $out $version;

ni $releasePath -ItemType Directory -Force | Out-Null;

# Get full path to the solution to build and the output package
$solutionPath = (Join-Path $tempPath $localSettings.solutionFile);
$packageFile = Join-Path $releasePath "$version.package.zip";

if ($localSettings.solutionFile -and (Test-Path $solutionPath)) {

    # Restore any NuGet packages
    Write-Debug "Restoring NuGet packages for $($localSettings.solutionFile)";
    Restore-NuGet `
        -Solution $solutionPath `
        -NuGetPath $nuGetPath;

    # Generate the package
    Write-Debug "Generating package from $($localSettings.solutionFile)";
    Write-RawPackage `
        -Solution $solutionPath `
        -Configuration $localSettings.releaseType `
        -Output $packageFile;
}

# Copying the deployment scripts
Write-Debug "Copying deployment scripts and parameters from $deployScriptPath";
if (Test-Path $deployScriptPath) {
    
    gci $deployScriptPath | ? Name -like "Deploy_*.cmd" | cp -Destination $releasePath;
	gci $deployScriptPath | ? Name -like "*.setParameters.xml" | cp -Destination $releasePath;
    gci $deployScriptPath | ? Name -like "deploy.settings.ps1" | cp -Destination $releasePath;
    gci $deployScriptPath | ? Name -like "Deploy*.ps1" | cp -Destination $releasePath;
	gci $deployScriptPath | ? Name -like "*.psm1" | cp -Destination $releasePath;
    gci $packageScriptPath | ? Name -like "*version.txt" | cp -Destination $releasePath;
}

if ($interactive -and !$psISE) {
    Write-Host "Press any key to continue ...";
    [System.Console]::ReadKey() | Out-Null;
}
