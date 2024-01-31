function New-DComAccessControlEntry {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string] 
        $Domain,
 
        [Parameter(Mandatory=$true, Position=1)]
        [string]
        $Name,
 
        [string] 
        $ComputerName = ".",

        [switch] 
        $Group
    )
 
    #Create the Trusteee Object
    $Trustee = ([WMIClass] "\\$ComputerName\root\cimv2:Win32_Trustee").CreateInstance()
    #Search for the user or group, depending on the -Group switch
    if (!$group) { 
        $account = [WMI] "\\$ComputerName\root\cimv2:Win32_Account.Name='$Name',Domain='$Domain'" }
    else { 
        $account = [WMI] "\\$ComputerName\root\cimv2:Win32_Group.Name='$Name',Domain='$Domain'" 
    }
 
    #Get the SID for the found account.
    $accountSID = [WMI] "\\$ComputerName\root\cimv2:Win32_SID.SID='$($account.sid)'"
 
    #Setup Trusteee object
    $Trustee.Domain = $Domain
    $Trustee.Name = $Name
    $Trustee.SID = $accountSID.BinaryRepresentation
 
    #Create ACE (Access Control List) object.
    $ACE = ([WMIClass] "\\$ComputerName\root\cimv2:Win32_ACE").CreateInstance()
 
    # COM Access Mask
    #   Execute         =  1,
    #   Execute_Local   =  2,
    #   Execute_Remote  =  4,
    #   Activate_Local  =  8,
    #   Activate_Remote = 16 
 
    #Setup the rest of the ACE.
    $ACE.AccessMask = 11 # Execute | Execute_Local | Activate_Local
    $ACE.AceFlags = 0
    $ACE.AceType = 0 # Access allowed
    $ACE.Trustee = $Trustee
    $ACE
}
 
<# 
 .Synopsis
  Configures DCOM access for a specified component to allow launch and access permissions for Local use.

 .Description
  Uses WMI to create a truste object and sets the security descriptors for Launch and Access to 11.

 .Parameter UserName
  Name of the user to give access.

 .Parameter ComComponentName
  Name of the component to modify access for.

 .Example
  # Set-DCOMAccess
  Set-DCOMAccess `
      -UserName '.\plustest' `
      -ComComponentName 'Microsoft Office Word 97 - 2003 Document';

  # From CMD
  powershell .\Set-DCOMAccess.ps1 -user "plustest" -componenet "Microsoft Office Word 97 - 2003 Document"
#>
function Set-DCOMAccess {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)][Alias('user')][string]$UserName,
        [Parameter(Mandatory=$true)][Alias('component')][string]$ComComponentName
     )

	if ($UserName -match '^(.*)\\(.*)$')
    {
        $UserDomain = $Matches[1] -replace '^\.$', $env:COMPUTERNAME;
        $UserName = $Matches[2];
    } else {
        $UserDomain = $env:COMPUTERNAME;
    }
	
	# Configure the DComConfg settings for the component so it can be activated & launched locally
	$dcom = Get-WMIObject Win32_DCOMApplicationSetting `
				-Filter "Description='$ComComponentName'" -EnableAllPrivileges

	$sd = $dcom.GetLaunchSecurityDescriptor().Descriptor
	$nsAce = $sd.Dacl | Where {$_.Trustee.Name -eq $UserName}
	if ($nsAce) {
		$nsAce.AccessMask = 11
	}
	else {
		$newAce = New-DComAccessControlEntry $UserDomain -Name $UserName
		$sd.Dacl += $newAce
	}

	$dcom.SetLaunchSecurityDescriptor($sd) | Out-Null;

	$sd = $dcom.GetAccessSecurityDescriptor().Descriptor
	$nsAce = $sd.Dacl | Where {$_.Trustee.Name -eq $Name}
	if ($nsAce) {
		$nsAce.AccessMask = 11
	}
	else {
		$newAce = New-DComAccessControlEntry $UserDomain -Name $UserName
		$sd.Dacl += $newAce
	}

	$dcom.SetAccessSecurityDescriptor($sd) | Out-Null;
}

Export-ModuleMember -function Set-DCOMAccess;