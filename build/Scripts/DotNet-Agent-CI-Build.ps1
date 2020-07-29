$ErrorActionPreference = "Stop"

###############
# NuGet Restore #
###############

$nugetPath = (Resolve-Path ".\build\Tools\nuget.exe").Path
$solutions = @("FullAgent.sln", "src\Agent\MsiInstaller\MsiInstaller.sln", "tests\Agent\IntegrationTests\IntegrationTests.sln", "tests\Agent\IntegrationTests\UnboundedIntegrationTests.sln")

Write-Host "Restoring NuGet packages"
foreach ($sln in $solutions) {
    & $nugetPath restore $sln -NoCache -Source "https://www.nuget.org/api/v2"
}

#######
# Build #
#######

$vsWhere = (Resolve-Path "build\Tools\vswhere.exe").Path
$msBuildPath = & "$vsWhere" -products 'Microsoft.VisualStudio.Product.BuildTools' -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
if (!$msBuildPath) {
    $msBuildPath = & "$vsWhere" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
}

$solutions = [Ordered]@{
    "FullAgent.sln"                                    = @("Configuration=Release;AllowUnsafeBlocks=true");
    "src\Agent\MsiInstaller\MsiInstaller.sln"                    = @("Configuration=Release;Platform=x86;AllowUnsafeBlocks=true","Configuration=Release;Platform=x64;AllowUnsafeBlocks=true");
    "tests\Agent\IntegrationTests\IntegrationTests.sln"                      = @("Configuration=Release;DeployOnBuild=true;PublishProfile=LocalDeploy");
    "tests\Agent\IntegrationTests\UnboundedIntegrationTests.sln"             = @("Configuration=Release;DeployOnBuild=true;PublishProfile=LocalDeploy");
}

Write-Host "Building solutions"
foreach ($sln in $solutions.Keys) {
    foreach ($config in $solutions.Item($sln)) {
        Write-Host "-- Building $sln : '. $msBuildPath -m -p:$($config) $sln'"
        . $msBuildPath -nologo -m -p:$($config) $sln
        Write-Host "MSBuild Exit code: $LastExitCode"
        if ($LastExitCode -ne 0) {
            Write-Host "Error building solution $sln. Exiting with code: $LastExitCode.."
            exit $LastExitCode
        }
    }
}

##########################
# Create Build Artifacts #
##########################

Push-Location "build"
Invoke-Expression "& .\package.ps1 -configuration Release -IncludeDownloadSite"
if ($LastExitCode -ne 0) {
   exit $LastExitCode
}
Pop-Location


exit $LastExitCode