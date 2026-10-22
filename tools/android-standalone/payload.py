"""Build-time staging for a read-only PC payload; only the private PCK copy is edited."""
import hashlib
from pathlib import Path
import shutil
import struct


def sha(path):
    with open(path,"rb") as stream:return hashlib.file_digest(stream,"sha256").hexdigest()


def managed(data):
    if data[:2]!=b"MZ":return False
    pe=struct.unpack_from("<I",data,60)[0]
    if data[pe:pe+4]!=b"PE\0\0":return False
    optional=pe+24
    magic=struct.unpack_from("<H",data,optional)[0]
    directories=optional+(96 if magic==0x10B else 112)
    return struct.unpack_from("<I",data,directories+14*8)[0]!=0


def patch_pck(source,destination):
    source=Path(source);destination=Path(destination)
    assert source.resolve()!=destination.resolve()
    destination.parent.mkdir(parents=True,exist_ok=True)
    before=sha(source)
    if not destination.exists() or destination.stat().st_size!=source.stat().st_size:
        shutil.copyfile(source,destination)
    patches=[]
    with destination.open("r+b") as stream:
        header=stream.read(104)
        magic,version,major,minor,patch,flags=struct.unpack_from("<6I",header)
        assert magic==0x43504447 and version==3 and not flags&1
        base,directory=struct.unpack_from("<QQ",header,24)
        if not flags&2:base=0
        stream.seek(directory);count=struct.unpack("<I",stream.read(4))[0]
        assert 0<count<1000000
        for _ in range(count):
            length=struct.unpack("<I",stream.read(4))[0];assert 0<length<65536
            name=stream.read(length).rstrip(b"\0").decode("utf-8").removeprefix("res://")
            offset,size=struct.unpack("<QQ",stream.read(16));md5_offset=stream.tell();original_md5=stream.read(16)
            entry_flags=struct.unpack("<I",stream.read(4))[0];next_entry=stream.tell()
            if name in ("project.binary","project.godot",".godot/extension_list.cfg"):
                assert entry_flags==0 and size<4*1024*1024
                stream.seek(base+offset);data=stream.read(size);old=data
                if name=="project.binary":
                    for term in (b"SentryInit",b"SentryBootstrap"):data=data.replace(b"autoload/"+term,b"disabled/"+term)
                elif name=="project.godot":
                    for term in (b"SentryInit",b"SentryBootstrap"):data=data.replace(term+b'=\"*res://addons/sentry/',b';'+term[1:]+b'=\"*res://addons/sentry/')
                else:data=data.replace(b"res://addons/sentry/sentry.gdextension",b" "*len(b"res://addons/sentry/sentry.gdextension"))
                assert len(data)==size
                if data!=old:
                    stream.seek(base+offset);stream.write(data)
                    stream.seek(md5_offset);stream.write(hashlib.md5(data).digest())
                    patches.append(name)
            stream.seek(next_entry)
    assert before==sha(source),"Source game PCK changed"
    return {"source_sha256":before,"sha256":sha(destination),"patched_entries":patches,"bytes":destination.stat().st_size}
