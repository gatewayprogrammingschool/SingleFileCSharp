param(
    [string]$Token=$null,
    [string]$WorkingFolder=".",
    [switch]$WhatIf=$false
)

$WorkingFolder = Resolve-Path $WorkingFolder -ErrorAction Stop

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

        Write-Verbose -Verbose:$Verbose -Message "Restoring [$csproj]..."

        & dotnet restore $csproj --nologo #-v quiet

        Test-ExitCode $LASTEXITCODE "Failed to restore [$csproj]."

        if($WhatIf) {
            Write-Verbose -Verbose:$Verbose -Message "Building [$csproj]..."

            & dotnet build $csproj -c Debug --no-restore --nologo #-v quiet

            Test-ExitCode $LASTEXITCODE "Failed to build [$csproj]."
        } else {
            Write-Verbose -Verbose:$Verbose -Message "Packing [$csproj]..."

            & dotnet pack $csproj -c Release --no-restore --nologo #-v quiet

            Test-ExitCode $LASTEXITCODE "Failed to pack [$csproj]."

            Write-Verbose -Verbose:$Verbose -Message "Getting Packages in $WorkingFolder ..."

            $pkgsPattern = $projectName + ".*.symbols.nupkg"
            "$packages = Get-ChildItem $pkgsPattern -Path $WorkingFolder -Recurse -Verbose:`$$Verbose  -ErrorAction Stop"
            $packages = Get-ChildItem $pkgsPattern -Path $WorkingFolder -Recurse -Verbose:$Verbose  -ErrorAction Stop

            if(($null -eq $packages) -or ($packages.Length -eq 0)) {
                $pkgsPattern = $projectName + ".*.nupkg"
                "$packages = Get-ChildItem $pkgsPattern -Path $WorkingFolder -Recurse -Verbose:`$$Verbose  -ErrorAction Stop"
                $packages = Get-ChildItem $pkgsPattern -Path $WorkingFolder -Recurse -Verbose:$Verbose  -ErrorAction Stop
            }

            if(($null -eq $packages) -or ($packages.Length -eq 0)) {
                throw "No packages were built for [$csproj]."
            }

            $packages = ($packages | Sort-Object ModifiedDate)

            Write-Verbose -Verbose:$Verbose -Message "Packages to publish..."

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

                        Write-Verbose -Verbose:$Verbose -Message "& dotnet nuget push $Name --source `"nuget`" -k `"`${token}`" --skip-duplicate"
                        & dotnet nuget push $Name --source "nuget" -k "${token}" --skip-duplicate

                        Test-ExitCode $LASTEXITCODE "Failed to push [${package.Name}]."
                    }
                    catch {
                        throw $_
                    }

                    Write-Verbose -Verbose:$Verbose -Message "Publish Finished Successfully."
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
