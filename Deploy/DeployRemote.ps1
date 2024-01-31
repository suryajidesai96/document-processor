<#
## Prerequisites

Example to run:

.\Deploy.ps1 -target test -username Administrator -password my_password
#>

param (
    [Parameter(Mandatory=$true)][Alias('t')][string]$target,
    [switch][Alias('i')]$interactive = $false
 )

$DebugPreference = "Continue";

$settings = Join-Path $PSScriptRoot "deploy.settings.ps1";
. $settings;
$targetEnv = $environments.$target;

if (!$targetEnv.remote) {
    Write-Error "$target does not have remote machine details specified, please use local deployment";
    exit 1;
}

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

# Connect to the target machine
$SecurePassword = $targetEnv.remote.targetPassword | ConvertTo-SecureString -AsPlainText -Force;
$VMCredential = New-Object System.Management.Automation.PSCredential -ArgumentList $targetEnv.remote.targetUsername, $SecurePassword;
$sessionOptions = New-PSSessionOption -SkipCACheck -SkipCNCheck                
$targetSession = New-PSSession -ComputerName $targetEnv.remote.targetMachine -Credential $VMCredential -UseSSL -SessionOption $sessionOptions
$publishScript = Join-Path $PSScriptRoot "Publish-Service.psm1";
$dcomScript = Join-Path $PSScriptRoot "Set-DCOMAccess.psm1";

# Make sure the TMP directory exists
Invoke-Command -Session $targetSession -ArgumentList $env:TEMP -ScriptBlock {
    param ($tmp)
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null;
}

# Make sure log directory exists and this user has access
Invoke-Command -Session $targetSession -ArgumentList $targetEnv -ScriptBlock {
    param ($targetEnv)

    New-Item -ItemType Directory -Path "$($targetEnv.logDir)" -Force | Out-Null;
    if ($targetEnv.serviceUsername -match '^(.*)\\(.*)$')
    {
        $compName = $Matches[1] -replace '^\.$', $env:COMPUTERNAME;
        $username = $Matches[2];
    } else {
        $compName = $env:COMPUTERNAME;
        $username = $targetEnv.serviceUsername;
    }

    $UserReference = New-Object System.Security.Principal.NTAccount $compName,$username;
    $Acl = Get-Acl $targetEnv.logDir;
    $Ar = New-Object System.Security.AccessControl.FileSystemAccessRule($username, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow");
    $Acl.SetAccessRule($Ar);
    Set-Acl $targetEnv.logDir $Acl;
}

# Copy the package and param file over to the destination
Copy-Item $packageFile -Destination $env:TEMP -ToSession $targetSession;
Copy-Item $paramFile -Destination $env:TEMP -ToSession $targetSession;
Copy-Item $publishScript -Destination $env:TEMP -ToSession $targetSession;
Copy-Item $dcomScript -Destination $env:TEMP -ToSession $targetSession;
$packageFile = Join-Path $env:TEMP (Split-Path $packageFile -Leaf);
$paramFile = Join-Path $env:TEMP (Split-Path $paramFile -Leaf);
$publishScript = Join-Path $env:TEMP (Split-Path $publishScript -Leaf);
$dcomScript = Join-Path $env:TEMP (Split-Path $dcomScript -Leaf);

# Install the service on the remote machine
Invoke-Command -Session $targetSession -ArgumentList $targetEnv, $packageFile, $paramFile, $publishScript  -ScriptBlock {
    param ($targetEnv, $packageFile, $paramFile, $publishScript)

    Import-Module $publishScript;
    Publish-Service `
          -PackagePath $packageFile `
          -ServiceName $targetEnv.serviceName `
          -ServiceExe $targetEnv.exeName `
          -BackupPath $targetEnv.backupPath `
          -ServiceUsername $targetEnv.serviceUsername `
          -ServicePassword $targetEnv.servicePassword `
          -ParamFile $paramFile;
}


if ($interactive -and !$psISE) {
    Write-Host "Press any key to continue ...";
    [System.Console]::ReadKey() | Out-Null;
}