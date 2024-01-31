function Get-FrameworkDirectory()
{
    $([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory());
}

function Get-InstallUtilCmd {
    $path = Get-FrameworkDirectory;
    $path = (Join-Path -Path $path -ChildPath 'InstallUtil.exe');
    return $path;
}

function Uncompress-Files
{
    param([string]$Path, [string]$DestinationPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($Path, $DestinationPath)
}

function Compress-Files
{
   param([string]$Path, [string]$DestinationPath)

   Add-Type -Assembly System.IO.Compression.FileSystem;
   $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal;
   [System.IO.Compression.ZipFile]::CreateFromDirectory($Path,
        $DestinationPath, $compressionLevel, $false);
}

function Stop-Service {
    Param ($ServiceName)

    $service = Get-WmiObject win32_service | ? {$_.Name -eq $ServiceName};

    if ($service -and $service.State -eq 'Running') {
        $service | Stop-Service;
    }
}

function Backup-Service {
    Param ($ServiceName, $remoteZipFile)

    $service = Get-WmiObject win32_service | ? {$_.Name -eq $ServiceName};

    if (!$service) {
        return;
    }

    $servicePath = Split-Path -parent $service.PathName;
    $servicePath = $servicePath -replace '"', '';
    $remoteZipPath = Split-Path -parent $remoteZipFile;

    if (!(Test-Path $remoteZipPath)) {
        New-Item -ItemType Directory -Force -Path $remoteZipPath | Out-Null;
    }

    Compress-Files -Path $servicePath -DestinationPath $remoteZipFile;
}
    
function Uninstall-Service {
    Param ($ServiceName)

    $service = Get-WmiObject win32_service | ? {$_.Name -eq $ServiceName};

    $servicePath = $service.PathName;
    $servicePathClean = $servicePath -replace '"', '';
    if (!($service -and (Test-Path $servicePathClean))) {
        return;
    }

    $installUtilCmd = Get-InstallUtilCmd;
    $arguments = -join ( `
        "/u " `
        , "/LogFile=""$PSScriptRoot\uninstallutil.log"" " `
        , "$servicePath" `
    );
        
    Write-Debug "Using tool $installUtilCmd $arguments";
    $proc = Start-Process $installUtilCmd $arguments -Wait -WindowStyle Hidden -PassThru -Verb RunAs;

    if ($proc.ExitCode -ne 0) {
        throw('InstallUtil exited with an error. ExitCode:' + $proc.ExitCode)
    }
}

function Install-Service {
    Param ($ServiceName, $servicePath, $serviceUser, $servicePassword)

    $installUtilCmd = Get-InstallUtilCmd;

    $arguments = -join ( `
        "/LogFile=""$PSScriptRoot\installutil.log"" " `
        , "/username=""$serviceUser"" " `
        , "/password=""$servicePassword"" " `
        , "/unattended " `
        , """$servicePath""" `
    );

    Write-Debug "Using tool $installUtilCmd $arguments";
    $proc = Start-Process $installUtilCmd $arguments -Wait -WindowStyle Hidden -PassThru -Verb RunAs;

    if ($proc.ExitCode -ne 0) {
        throw('InstallUtil exited with an error. ExitCode:' + $proc.ExitCode)
    }
}

function Replace-XmlParameters {
    Param ($TargetDir, $SetParamFilePath)

    $ParamFilePath = Join-Path $TargetDir "Parameters.xml";
    if (!($ParamFilePath `
        -and (Test-Path $ParamFilePath) `
        -and $SetParamFilePath `
        -and (Test-Path $SetParamFilePath))) {

        return;
    }

    $paramXml = [xml] (Get-Content $ParamFilePath);
    $setParamXml = [xml] (Get-Content $SetParamFilePath);

    $params = $paramXml.SelectNodes('//parameter');

    ForEach ($param in $params) {
        $setParam = $setParamXml.SelectSingleNode("//setParameter[@name='$($param.Name)']/@value");
        $paramValue = if ($setParam) { $setParam.Value } else { $param.DefaultValue }
        $entries = $param.SelectNodes('./parameterEntry[@kind="XmlFile"]');

        ForEach ($entry in $entries) {
            # File all files within scope
            $files = Get-ChildItem -Path $TargetDir -Recurse | ? FullName -Match $entry.scope;
            
            # Replace values within the files
            ForEach ($file in $files) {
                $fileXml = [xml] ($file | Get-Content);
                $fileXml.SelectNodes($entry.match) | % { $_.Value = $paramValue }
                $fileXml.Save($file.FullName);
            }            
        }
    }
}

<# 
 .Synopsis
  Deploys a service built from msbuild or visual studio to the target Server.

 .Description
  Create a build using visual studio or the packaging script.
  Pass a package to this script along with target server details.
  The script will backup any existing service of that name, uninstall the service
  copy the package over and extract over the top of the old files and reinstall the service.

 .Parameter ServiceName
  Name of the service being installed.

 .Parameter ServiceExe
  File name of the service executable being installed.

 .Parameter PackagePath
  Full path to the package to deploy.

 .Parameter InstallPath
  Path on the target server where to install the service files.
  If not specified, will install into C:\Program Files\ServiceName
   
 .Parameter BackupPath
  Local path on the remote server to backup the existing service (if present) to.

 .Parameter ServiceUsername
  Username for the service to use to run.

 .Parameter ServicePassword
  Password for the service to use to run.

 .Parameter ParamFile
  Path to .setParameter.xml file containing Xml Parameter settings to update.
  Parameters.xml file must also exist in the package.

 .Example
  # Publish service
  Publish-Service `
      -ServiceName 'Plus Schedule Processor' `
      -ServiceExe 'ScheduleProcessingService.exe' `
      -PackagePath 'C:\HoltServices\ScheduleProcessor\ScheduleProcessingService.zip' `
      -BackupPath C:\Backups `
      -ServiceUsername .\localadmin `
      -ServicePassword zOP1OMKXL1S9kXsZK4r8 `
      -ParamFile 'C:\test.setParameters.xml';

  # From CMD
  powershell .\Publish-Service.ps1 -name "Plus Schedule Processor" -exe "ScheduleProcessingService.exe" -package "ScheduleProcessor.zip" -username ".\localadmin" -password "zOP1OMKXL1S9kXsZK4r8" -params "testazure.setParameters.xml"
#>
function Publish-Service {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)][Alias('name')][string]$ServiceName,
        [Parameter(Mandatory=$true)][Alias('exe')][string]$ServiceExe,
        [Parameter(Mandatory=$false)][Alias('package')][string]$PackagePath,
        [Parameter(Mandatory=$false)][string]$InstallPath,
        [Parameter(Mandatory=$false)][string]$BackupPath = 'C:\Backups',
        [Parameter(Mandatory=$true)][Alias('username')][string]$ServiceUsername,
        [Parameter(Mandatory=$true)][Alias('password')][string]$ServicePassword,
        [Parameter(Mandatory=$false)][Alias('params')][string]$ParamFile
     )

    $curDate = Get-Date -Format "yyyyMMddHHmmss";

    # Set the install path if not specified
    if (!$InstallPath) {
        $InstallPath = Join-Path $env:ProgramFiles $ServiceName;
    }

    if (!(Test-Path $InstallPath)) {
        # Have to use this to auto elevate as Program Files requires elevation
        Start-Process PowerShell.exe -ArgumentList "ni -ItemType Directory -Force -Path '$InstallPath' | Out-Null" -Wait -Verb RunAs -WindowStyle Hidden;
    }
    
    # Backup the existing service first
    $tmpPath = Join-Path $env:Temp "$ServiceName`_$curDate";
    New-Item -ItemType Directory -Force -Path $tmpPath | Out-Null;

    $backupZipPath = Join-Path $BackupPath "$ServiceName`_$curDate.backup.zip";
    Write-Debug "Backing up service $ServiceName to $backupZipPath";
    Backup-Service -serviceName $ServiceName -remoteZipFile $backupZipPath;

    # Stop the service ready to remove
    Write-Debug "Stopping service $ServiceName";
    Stop-Service $ServiceName;

    # Stop the service ready to remove
    Write-Debug "Uninstalling service $ServiceName";
    Uninstall-Service $ServiceName;

    # Copy, setup and install the new service
    if (Test-Path $PackagePath) {
        Write-Debug "Extracting new package";
        Uncompress-Files $PackagePath $tmpPath;

        if ($ParamFile -and (Test-Path $ParamFile)) {
            Write-Debug "Replacing parameters";
            Replace-XmlParameters -TargetDir $tmpPath -SetParamFilePath $ParamFile;
        }

        # Have to use this to auto elevate as Program Files requires elevation
        Start-Process PowerShell.exe -ArgumentList "copy '$tmpPath\*' '$InstallPath' -Force -Recurse" -Wait -Verb RunAs -WindowStyle Hidden;

        # Clean up the temporary dir
        Remove-Item -Recurse -Force $tmpPath;
        
        Install-Service $ServiceName (Join-Path $InstallPath $ServiceExe) $ServiceUsername $ServicePassword;
    }
}

Export-ModuleMember -function Publish-Service;