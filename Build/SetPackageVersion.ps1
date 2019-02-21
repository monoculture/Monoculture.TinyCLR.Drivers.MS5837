#[string]$buildName    = "1.0.0.1234";
#[string]$commitId     = "2323233";
#[string]$environment  = "alpha";

[string]$buildName   = $ENV:BUILD_BUILDNUMBER;
[string]$commitId    = $ENV:BUILD_SOURCEVERSION
[string]$environment = $Env:RELEASE_ENVIRONMENTNAME;

# =========================================================================================================

$versionString = [regex]::matches($buildName, "\d+\.\d+\.\d+\.\d+");

if($versionString.Count -eq 0)
{
    Write-Error "Unable to extract version number from build number."
    exit 1;
}

if($versionString.Count -gt 1)
{
    Write-Warning "Build number contained multiple matching version numbers";
    Write-Warning "Script will work on the assumption that the first instance is the correct version.";
}

$versionParts = [regex]::matches($versionString[0], "\d+");

$majorVersion = $versionParts[0].Value;
$minorVersion = $versionParts[1].Value;
$patchVersion = $versionParts[2].Value;
$buildNumber  = $versionParts[3].Value;

$packageVersion = "$majorVersion.$minorVersion.$patchVersion-$environment.$buildNumber+$commitId";

Write-Verbose "Package Version: $packageVersion" -verbose

Write-Output("##vso[task.setvariable variable=PackageVersion;]$packageVersion");
