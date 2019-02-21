#[string]$buildNName = "1.0.0.1234";
#[string]$sourcePath = $PSScriptRoot;

[string]$buildName   = $ENV:BUILD_BUILDNUMBER;
[string]$sourcePath  = $ENV:BUILD_SOURCESDIRECTORY;

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

$assemblyVersion = "$majorVersion.$minorVersion.$patchVersion";
$assemblyFileVersion = "$majorVersion.$minorVersion.$patchVersion.$buildNumber";

Write-Verbose "Assembly Version is $assemblyVersion" -Verbose
Write-Verbose "Assembly File Version is $assemblyFileVersion" -Verbose

$AllVersionFiles = Get-ChildItem $sourcePath AssemblyInfo.cs -recurse

if($AllVersionFiles.Count -eq  0)
{
    Write-Warning "No AssemblyInfo.cs files were found.";
    exit 0;
}

foreach ($file in $AllVersionFiles) 
{ 
    write-host "Updating: $file";

    (Get-Content $file.FullName) |
        %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$assemblyVersion"")" } |
        %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$assemblyFileVersion"")" } |
	    Set-Content $file.FullName -Force
}

write-host "Done.";
  