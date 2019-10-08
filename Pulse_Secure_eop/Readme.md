# Pulse Secure client arbitrary file write Elevation of Privilege

Details: https://offsec.almond.consulting/pulse-secure-arbitrary-file-write-eop.html

Compile with `csc`:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /unsafe /platform:anycpu /r:NtApiDotNet.dll /out:poc.exe .\PulseLogPrivesc.cs
```

You will need the [NtApiDotNet library](https://github.com/googleprojectzero/sandbox-attacksurface-analysis-tools/tree/master/NtApiDotNet) to compile it.
