# RCE in Ghostscript (CVE-2023-28879)

This is a proof-of-concept for CVE-2023-28879, an Arbitrary Code Execution in Ghostscript. It will generate a Postscript file that runs an arbitrary payload when read by Ghostscript version<10.01.1.
(Tested on version 9.55, 9.56, 10.0.0 and 10.1.0)

Details: https://offsec.almond.consulting/ghostscript-cve-2023-28879.html

The getoffset.py script should be run against the ghostscript binary or the gs library to get the correct offset.
Afterwards, the final-poc.ps file will be generated. Your (hex encoded) payload should be inserted at line 73 and onwards.
