param(
    [string]$Token=$null,
    [string]$WorkingFolder=".",
    [switch]$WhatIf=$false
)

$exitCode = 0;

function Test-ExitCode {
    param(
        [int]$LEC,
        [string]$ErrorDescription)

    $exitCode = $LEC

    if($exitCode -ne 0) {
        throw "${exitCode}: ${ErrorDescription}"
    } else {
        # "`$exitCode: $exitCode"
    }
}

Push-Location

try {
    $projectName = "dotnet-singlefilecsharp"
    Set-Location $WorkingFolder -Verbose:$Verbose

    if((-not $token) -or ($token.Length -eq 0)) {
        throw "No Nuget Token was supplied."
    }

    $config = Get-ChildItem nuget.config -ErrorAction Stop

    if (-not $config) {
        throw "Could not locate nuget.config in the root of the project."
    }

    $ConfigFile = $config.FullName

    $project = Get-ChildItem "$projectName.csproj" -Recurse -ErrorAction Stop

    if($project) {
        $csproj = $project.FullName
        Write-Information "Building [$csproj]..."

        & dotnet restore $csproj --nologo #-v quiet

        Test-ExitCode $LASTEXITCODE "Failed to resore [$csproj]."

        if($WhatIf) {
            & dotnet build $csproj -c Debug --no-restore --nologo #-v quiet
    
            Test-ExitCode $LASTEXITCODE "Failed to build [$csproj]."
        } else {
            & dotnet pack $csproj -c Release --no-restore --nologo -v quiet

            Write-Information "Getting Packages..."

            $packages = Get-ChildItem "$projectName.*.symbols.nupkg" -Path $WorkingFolder -Recurse -ErrorAction Stop -Verbose `
                | Sort-Object ModifiedDate;

            if((-not $packages) -or ($packages.Length -eq 0)) {
                $packages = Get-ChildItem "$projectName.*.nupkg" -Path $WorkingFolder -Recurse -ErrorAction Stop -Verbose `
                    | Sort-Object ModifiedDate;
            }

            if((-not $packages) -or ($packages.Length -eq 0)) {
                throw "No packages were built for [$csproj]."
            }

            Write-Information "Packages to publish..."

            $packages `
                | Sort-Object Directory, Name `
                | Format-Table Name, Directory

            $packages `
                | ForEach-Object -Verbose -Process {
                    $package = $_

                    try {
                        $Name = $package.Name

                        Set-Location $package.Directory

                        Copy-Item $ConfigFile . -ErrorAction Stop

                        Write-Information "& dotnet nuget push $Name --source `"nuget`" -k `"`${token}`" --skip-duplicate"
                        & dotnet nuget push $Name --source "nuget" -k "${token}" --skip-duplicate

                        Test-ExitCode $LASTEXITCODE "Failed to push [${package.Name}]."
                    }
                    catch {
                        throw $_
                    }

                    Write-Information "Publish Finished Successfully."
            }
        }
    }
    else {
        throw "No csproj file in $PWD (recursive)."
    }
}
catch {
    if($exitCode -eq 0) { $exitCode = $LASTEXITCODE }
    $err = $_
    $err
    exit $exitCode
}
finally {
    Pop-Location
}
