# README

It is a challenge from RealWorld CTF (Be a RWCTFer) @ https://be-a-rwctfer.realworldctf.com/
.  It is a eazy competition for new comers in cyber security.

Note that the ghostscript source code is so large and not suitable for adding,
so please download @ https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10010/ghostscript-10.01.0.tar.gz
## Build

```
docker build -t ghostscript .
```

## Run

```
docker run -it --rm --name ghostscript -p 1337:1337 ghostscript
```

## Exploit

1. Start up Docker and grab `gs` binary from the Docker
2. Run getoffset.py to generate a final poc, setting rce command to be `/bin/sh`
3. Send the poc line-by-line with pwntools or other tools through tcp
4. Now you get a shell
