try {
    Write-Host "Sourcing helper functions ..."
    . "./subscripts/helper-functions.ps1"
}
catch {
    throw "Failed to source helper functions."
}

$Settings = Get-SettingsObject "./build-mocassin-active.json"
$deployRootPath = $Settings.Deploy.RootDirectory
$deployLogFile = $Settings.Deploy.LogFile
$version = $Settings.Meta.Version.Full
$isTestMode = $Settings.IsTestMode
$zipAfterBuild = $Settings.Deploy.ZipAfterBuild

# Delte and recreate the deploy folder
Write-Host "Build version  : " $version
Write-Host "Build directory: " $deployRootPath
Write-Host "Build log file : " $deployLogFile

New-Item -Force -Path $deployRootPath -ItemType directory | Out-Null
New-Item $deployLogFile -ItemType File | Out-Null

# Call local solver build
Write-Host "Invoking scripts ..."
foreach ($script in $Settings.Scripts) {
    if ($script.IsActive) {
        try {
            Write-Host "Running script : " $script.Path
            & $script.Path -Settings $Settings 3>&1 2>&1 | Write-Log -FilePath $deployLogFile
            Write-Host "Finished script: " $script.Path      
        }
        catch {
            Write-Warning ($script.Path + " has failed with an error: `n`t$_")
        }
    }
}

if ($zipAfterBuild) {
    Write-Host "Zipping build ... " -NoNewline
    $zipArchivePath = "$deployRootPath.zip"
    Get-ChildItem -Path $deployRootPath -Exclude "*.log" | Compress-Archive -DestinationPath $zipArchivePath
    Get-ChildItem -Path $deployRootPath -Exclude "*.log" | ForEach-Object { Remove-Item $_ -Recurse }
    Move-Item $zipArchivePath -Destination $deployRootPath
    Write-Host "done!"
}

if ($isTestMode) {
    Remove-Item -Recurse $deployRootPath
}

Write-Host "Build process reached end!"