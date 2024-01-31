<#
This file specifies the different environments which can be deployed to by packages created using Package.ps1. 
It is copied to the release folder with the rest of the package and referenced by the Deploy.ps1 script.

# Powershell hash table of environments supported.
$environments = @{
    "example" = @{
		
	}
}
#>

$environments = @{
	"live_azure" = @{
        parametersFile = "live.azure.setParameters.xml";
        backupPath = "C:\Backups";
        serviceName = "Plus Document Processor";
		exeName = "documentprocessingservice.exe";
        serviceUsername = '.\plustest';
		servicePassword = 'Limited Use 2016';
		requireVerify = $false;
        logDir = "c:\log\documentprocessing";

        remote = @{
            targetMachine = '10.0.0.8';
            targetUsername = 'plusdeploy';
            targetPassword = 'lZliiUkJJH3or5jlw2vs';
        };
    };
    "uat" = @{
        parametersFile = "uat.setParameters.xml";
        backupPath = "C:\Backups";
        serviceName = "Plus Document Processor";
		exeName = "documentprocessingservice.exe";
		serviceUsername = '.\plusuat';
		servicePassword = 'UItTzxJtuYv7FyQUqRo6';
		requireVerify = $false;
        logDir = "c:\log\documentprocessing";

        remote = @{
            targetMachine = '10.0.8.6';
            targetUsername = '.\lmsuat.admin';
            targetPassword = 'YBC0NXUeZwunL42P6QOd';
        };
    };
	"nhspdemo" = @{
        parametersFile = "nhspdemo.setParameters.xml";
        backupPath = "C:\Backups";
        serviceName = "Plus Document Processor";
		exeName = "documentprocessingservice.exe";
		serviceUsername = '.\plus.service';
		servicePassword = 'eqxFUsq9';
		requireVerify = $false;
        logDir = "c:\log\documentprocessing";

		remote = @{
            targetMachine = '10.0.8.9';
			targetUsername = '.\nhspdemo';
            targetPassword = '7gk2oLyhoL';
        };
    };
	"nhsptest" = @{
        parametersFile = "nhsptest.setParameters.xml";
        backupPath = "C:\Backups";
        serviceName = "Plus Document Processor";
		exeName = "documentprocessingservice.exe";
		serviceUsername = '.\plus.service';
		servicePassword = '7gk2oLyhoL';
		requireVerify = $false;
        logDir = "c:\log\documentprocessing";

		remote = @{
            targetMachine = '10.0.8.13';
            targetUsername = '.\plusdeploy';
            targetPassword = 'pgHyw7A51a';
        };
    };

}