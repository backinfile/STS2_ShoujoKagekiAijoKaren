"""Run immutable-payload installer regressions and optionally unpack a full signed APK."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import uuid

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--jdk",type=Path,default=ROOT/"artifacts/apk/toolchain/jdk-21.0.12.1+1")
    parser.add_argument("--apk",type=Path)
    args=parser.parse_args();jdk=args.jdk.resolve()
    work=ROOT/"artifacts/android-direct/installer-checks"/uuid.uuid4().hex[:8];classes=work/"classes";classes.mkdir(parents=True)
    exe=".exe" if os.name=="nt" else ""
    environment=dict(os.environ);environment["JAVA_TOOL_OPTIONS"]="-Dfile.encoding=UTF-8 -Duser.language=en -Duser.country=US"
    with (work/"result.log").open("wb") as log:
        subprocess.run([str(jdk/("bin/javac"+exe)),"--release","8","-encoding","UTF-8","-d",str(classes),
                        str(HERE/"java/com/karen/sts2/BundleInstaller.java"),str(HERE/"tests/InstallerTests.java")],env=environment,stdout=log,stderr=subprocess.STDOUT,check=True)
        command=[str(jdk/("bin/java"+exe)),"-cp",str(classes),"com.karen.sts2.InstallerTests",str(work/"data")]
        if args.apk:command.append(str(args.apk.resolve()))
        subprocess.run(command,env=environment,stdout=log,stderr=subprocess.STDOUT,check=True)
    result={"installer_regressions":12,"passed":True,"full_apk_payload_verified":args.apk is not None,"android_device_tested":False,
            "log":str((work/"result.log").relative_to(ROOT))}
    (work/"result.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(result),flush=True)
    print((work/"result.log").read_text(encoding="utf-8"),flush=True)

if __name__=="__main__":main()
