# Windows Error Reporting Manager arbitrary file move Elevation of Privilege

Details: https://offsec.almond.consulting/windows-error-reporting-arbitrary-file-move-eop.html

You will need the [NtApiDotNet library](https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/tree/master/NtApiDotNet) to run it, as well as a valid `Report.wer` file, both to be placed in the same directory as the `poc.ps1` script.

To generate a WER report file, you can run the `[Environment]::FailFast('Error')` command in PowerShell, and look for the report file in `%ProgramData%\Microsoft\Windows\WER\ReportQueue`.

The script can be run with the following command:

`powershell -exec bypass -C ". .\poc.ps1; Test-Exploit"`

Tested on Windows 10 1903.
