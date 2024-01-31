<#
These settings a machine specific and define what the solution name is, where your local solution sits etc...
You can modify this and keep your own local version, just don't check in your changes.
#>
$localSettings = @{
    solutionFile = "documentprocessor.sln";
	localReleaseFolder = "C:\Releases\Holt\Services\DocumentProcessor";
    releaseType = "release";
    versionFile = "version.txt";
};
