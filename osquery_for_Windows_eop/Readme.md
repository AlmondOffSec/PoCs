# Osquery for Windows access right misconfiguration Elevation of Privilege

Details: https://offsec.almond.consulting/osquery-windows-acl-misconfiguration-eop.html

The command executed by the `osqueryd` service is hardcoded in `dllmain.cpp`.

You will need the `CreateHardlink` tool from [symboliclink-testing-tools](https://github.com/googleprojectzero/symboliclink-testing-tools/), and to compile `dllmain.cpp` into `PocDLL.dll` (just replace the `dllmain.cpp` file a new DLL project in Visual Studio).
