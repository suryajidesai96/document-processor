<#
## Prerequisites

Example to run:

.\Deploy.ps1 -target test -username Administrator -password my_password
#>

param (
    [Parameter(Mandatory=$true)][Alias('t')][string]$target,
    [switch][Alias('i')]$interactive = $false,
	[switch][Alias('c')]$configure = $false
 )

$DebugPreference = "Continue";

$settings = Join-Path $PSScriptRoot "deploy.settings.ps1";
. $settings;
$targetEnv = $environments.$target;

# Get the source of our package
$releasePath = $PSScriptRoot;

# Get full path to the solution to build and the output package
$packageFolder = $releasePath;
$packageFile = gci $packageFolder -Filter "*.package.zip" | Select -ExpandProperty FullName;
$paramFile = Join-Path $releasePath $targetEnv.parametersFile;

# Verify if we want to proceed
if ($interactive -and $targetEnv.requireVerify) {

    $verifyWord = Get-Word;
    $userWord = Read-Host -Prompt "Please enter the word '$verifyWord' to verify you want to release to $target";
    
    if ($verifyWord -ne $userWord) {
        Write-Error "Deployment not verified. Target $target requires verification to deploy.";
        exit 1;
    }
}

# Publish the package
Write-Debug "Publishing package $packageFile with params $paramFile";

if (-not (Test-Path $packageFile)) {
    Write-Error "Package $packageFile not found, unable to deploy";
    exit 1;
}

if (-not (Test-Path $paramFile)) {
    Write-Error "Params $paramFile not found, unable to deploy";
    exit 1;
}

$publishScript = Join-Path $PSScriptRoot "Publish-Service.psm1";
$dcomScript = Join-Path $PSScriptRoot "Set-DCOMAccess.psm1";

# Make sure log directory exists and this user has access
New-Item -ItemType Directory -Path "$($targetEnv.logDir)" -Force | Out-Null;

# Copy the package and param file over to the destination
Import-Module $publishScript;
Publish-Service `
        -PackagePath $packageFile `
        -ServiceName $targetEnv.serviceName `
        -ServiceExe $targetEnv.exeName `
        -BackupPath $targetEnv.backupPath `
        -ServiceUsername $targetEnv.serviceUsername `
        -ServicePassword $targetEnv.servicePassword `
        -ParamFile $paramFile;

if ($configure) {
	$ComComponentName = 'Microsoft Office Word 97 - 2003 Document'
	Import-Module $dcomScript;
	Set-DCOMAccess `
		-UserName $targetEnv.serviceUsername `
		-ComComponentName 'Microsoft Office Word 97 - 2003 Document';

	# Set Word 2007 Interop COM component to run as the Interactive User
	Set-ItemProperty -path ("Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{00020906-0000-0000-C000-000000000046}") -name "RunAs" -Value "Interactive User";
}

if ($interactive -and !$psISE) {

    Write-Host "Press any key to continue ...";
    [System.Console]::ReadKey() | Out-Null;
}