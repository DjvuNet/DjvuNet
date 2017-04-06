#
# Script.ps1
#

$DjvuNetEnv = "DJVUNET_TESTS";
$DjvuLibreEnv = "DJVULIBRE_TESTS";
$Platform = [System.Environment]::GetEnvironmentVariable("PLATFORM");
$Configuration = [System.Environment]::GetEnvironmentVariable("CONFIGURATION");
if ($Platform -eq $null) {$Platform = "NotSet"};
if ($Configuration -eq $null) {$Configuration = "NotSet"};
$TestBuild = ($Platform -eq "x86") -or ($Platform -eq "x64");

$Message = [System.String]::Format("Platform and Configuration allow for testing: {0}", $TestBuild);
Write-Output $Message;

if ($TestBuild -eq $true) {

    $XUnitPath = "c:\projects\DjvuNet\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe";
    $TestAssembly1 = "c:\projects\DjvuNet\DjvuNet.Tests\bin\$($Platform)\$($Configuration)\DjvuNet.Tests.dll"; 
    
    $Message = "Preparing to run tests for {0}|{1}" -f $Configuration, $Platform;
    Write-Output $Message;

    $TestArgs = "  -appveyor -notrait 'Category=Skip'";

    if ([System.IO.File]::Exists($TestAssembly1) -and !([System.Environment]::GetEnvironmentVariable($DjvuNetEnv) -eq $null)) 
    {
        $Message = "Running tests from Assembly: " -f [System.IO.Path].GetFileName($TestAssembly1);
        Write-Output 
        $TestArgs1 = "{0} {1}" -f $TestAssembly1, $TestArgs; 
        Start-Process -FilePath $XUnitPath -ArgumentList $TestArgs1;
        [System.Environment]::SetEnvironmentVariable($DjvuNetEnv, "true");
        $Message = "Finished running tests from Assembly " -f [System.IO.Path].GetFileName($TestAssembly1);
        Write-Output $Message; 
    }
    else
    {
        $Message = "Failed to locate test Assembly {0}" -f [System.IO.Path]::GetFileName($TestAssembly1);
        Write-Output $Message;
    }

    $TestAssembly2 = "c:\projects\DjvuNet\DjvuNet.DjvuLibre.Tests\bin\$($Platform)\$($Configuration)\DjvuNet.DjvuLibre.Tests.dll";

    if ([System.IO.File]::Exists($TestAssembly2) -and !([System.Environment]::GetEnvironmentVariable($DjvuLibreEnv) -eq $null)) 
    {
        Write-Output "Running tests from Assembly " + [System.IO.Path].GetFileName($TestAssembly2);
        $TestArgs2 = "{0} {1}" -f $TestAssembly2, $TestArgs; 
        Start-Process -FilePath $XUnitPath -ArgumentList $TestArgs2;
        [System.Environment]::SetEnvironmentVariable($DjvuLibreEnv, "true");
        Write-Output "Finished running tests from Assembly " + [System.IO.Path].GetFileName($TestAssembly2); 
    }
    else
    {
        $Message = "Failed to locate test Assembly {0}" -f [System.IO.Path]::GetFileName($TestAssembly2);
        Write-Output $Message;
    }

    $Message = "Finished test run for {0}|{1}" -f $Configuration, $Platform;
    Write-Output $Message;
}
else{
    $Message = "Skipped test run for {0}|{1}" -f $Configuration, $Platform;
    Write-Output $Message;
};
