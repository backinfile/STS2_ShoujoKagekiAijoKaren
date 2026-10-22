"""Run isolated installer tests on a JVM with narrow public-API mocks."""
import argparse
import hashlib
from pathlib import Path
import subprocess
import uuid
import zipfile

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--jdk",type=Path,required=True)
    args=parser.parse_args()
    work=ROOT / "artifacts/apk/bootstrap-tests" / uuid.uuid4().hex[:8]
    classes=work / "classes";classes.mkdir(parents=True)
    archive=work / "fixture.zip"
    with zipfile.ZipFile(archive,"w") as package:
        for mod in ("BaseLib","ShoujoKagekiAijoKaren"):
            package.writestr(mod+"/"+mod+".json",'{"id":"'+mod+'"}')
            package.writestr(mod+"/"+mod+".dll",b"fixture")
    checksum=hashlib.sha256(archive.read_bytes()).hexdigest()
    generated=work / "BuildInfo.java"
    generated.write_text('package com.karen.sts2; final class BuildInfo { static final String MOD_SHA256 = "'+checksum+'"; }\n',encoding="utf-8")
    exe=".exe" if (args.jdk / "bin/java.exe").exists() else ""
    subprocess.run([str(args.jdk / ("bin/javac"+exe)),"--release","8","-encoding","UTF-8","-d",str(classes),
                    *map(str,sorted((HERE / "tests").rglob("*.java"))),str(generated),
                    str(HERE / "java/com/karen/sts2/KarenBundledMods.java")],check=True)
    subprocess.run([str(args.jdk / ("bin/java"+exe)),"-cp",str(classes),"com.karen.sts2.BootstrapTests",str(work / "data"),str(archive)],check=True)

if __name__=="__main__":main()
