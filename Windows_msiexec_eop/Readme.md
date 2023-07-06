# Windows Installer arbitrary content manipulation Elevation of Privilege (CVE-2020-0911)

Details: https://offsec.almond.consulting/windows-msiexec-eop-cve-2020-0911.html

The PoC exploits this vulnerability (CVE-2020-0911) to create the desired registry key, then triggers a crash to execute the debugger associated with `WerFault.exe` (`C:\x\z.exe` which is a copy of `payload.exe`) as `SYSTEM`.

It needs 4 components:

- the `NtApiDotNet` library from [sandbox-attacksurface-analysis-tools](https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/) for registry symlinks
- `payload.exe` is the payload that will be run after exploiting the vulnerability, it will be run as SYSTEM, and a second time as the user
- `exploit.exe` performs the redirection and runs `msiexec` (this exe is run by the Powershell script)
- `exploit.ps1` contains the setup logic and things specific to the chosen MSI file, and is the PoC's starting point

This repository contains the relevant parts of the source code in `exploit.cpp` (which should be compiled as `exploit.exe`) and `exploit.ps1`.

*Note: source code provided in this repository is the original one sent to Microsoft; it may need to be adapted, as the C++ code requires [Jonas' exploit toolkit](https://github.com/jonaslyk/exploitkitpub) which was not public at the time and may have slight differences with the published version.*

The PoC targets the VC++ 2019 Minimum Runtime x64 package. As explained in the article, the vulnerability has nothing to do with this particular package - but this one was chosen for the demonstration because it is very commonly installed.

The `exploit.ps1` script contains the parts that are specific to this package, and that could be adapted for practically any other package. So, for the purpose of this PoC, it should be run on a machine with this package installed, e.g. from [here](https://aka.ms/vs/16/release/vc_redist.x64.exe) (but any recent version should be fine).

With the 4 components in the same directory, the script can be run with the following command:

`powershell -exec bypass -C ". .\exploit.ps1; Invoke-MsiExecExploitForVCRuntime"`

Tested on Windows 10 1909 (unpatched).
