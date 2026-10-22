"""Build a self-contained local Karen game APK from an owned PC installation.

The application classes are compiled here. Engine dependencies come from a pinned
runtime carrier; all original launcher classes/activities/assets are excluded.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import struct
import sys
import urllib.request
import uuid
import zipfile

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
sys.path.insert(0,str(HERE.parent/"android-apk"))
from build_apk import dependency,java_home,run as run_tool,check_mod_zip
from apk_format import OLD_ID,dex_classes,elements,managed_metadata_streams,patch_managed_package,patch_resource_package,validate_dex
from payload import sha,managed,patch_pck

APP_ID="com.karenmod.sts2hd"
OUT=ROOT/"artifacts/android-direct"
PUBLISH=".godot/mono/publish/arm64/"
EXTRA_TOOLS={
 "platform-35_r02.zip":("https://dl.google.com/android/repository/platform-35_r02.zip","0bb560a90a7a2cbd0dd8348224d518b638fe7949","sha1"),
 "dexlib2-2.5.2.jar":("https://repo.maven.apache.org/maven2/org/smali/dexlib2/2.5.2/dexlib2-2.5.2.jar","5a5c8982d8bd7d6e3bb1a0713049e3c78b719ec32b20f6b619885cec30a0dd61","sha256"),
 "guava-27.1-android.jar":("https://repo.maven.apache.org/maven2/com/google/guava/guava/27.1-android/guava-27.1-android.jar","686404f2d1d4d221911f96bd627ff60dac2226a5dfa6fb8ba517073eb97ec0ef","sha256"),
}


def run(args,log,env=None):
    try:return run_tool(args,log,env)
    except RuntimeError as error:raise RuntimeError("Standalone build tool failed; see artifacts/android-direct/build.log") from error


def extra_tools():
    folder=ROOT/"artifacts/apk/toolchain";folder.mkdir(parents=True,exist_ok=True)
    for name,(url,expected,algorithm) in EXTRA_TOOLS.items():
        path=folder/name
        if not path.exists():
            temporary=path.with_suffix(path.suffix+".partial")
            urllib.request.urlretrieve(url,temporary)
            with temporary.open("rb") as stream:assert hashlib.file_digest(stream,algorithm).hexdigest()==expected,"Downloaded tool checksum mismatch: "+name
            temporary.replace(path)
        with path.open("rb") as stream:assert hashlib.file_digest(stream,algorithm).hexdigest()==expected,"Tool checksum mismatch: "+name
    sdk=folder/"android-35.jar"
    with zipfile.ZipFile(folder/"platform-35_r02.zip") as archive:
        name=next(n for n in archive.namelist() if n.endswith("/android.jar"));sdk.write_bytes(archive.read(name))
    return folder,sdk


def compile_host(jdk,sdk,d8,log):
    work=OUT/"host";work.mkdir(exist_ok=True)
    classes,stubs,dex=[work/name for name in ("classes","stubs","dex")]
    for path in (classes,stubs,dex):path.mkdir(exist_ok=True)
    exe=".exe" if os.name=="nt" else ""
    javac,jar,java=[jdk/("bin/"+name+exe) for name in ("javac","jar","java")]
    run([javac,"--release","8","-encoding","UTF-8","-cp",sdk,"-d",stubs,*sorted((HERE/"compile-stubs").rglob("*.java"))],log)
    stub_jar=work/"compile-stubs.jar";host_jar=work/"host.jar"
    run([jar,"cf",stub_jar,"-C",stubs,"."],log)
    run([javac,"--release","8","-encoding","UTF-8","-cp",os.pathsep.join(map(str,[sdk,stubs])),"-d",classes,*sorted((HERE/"java").rglob("*.java"))],log)
    run([jar,"cf",host_jar,"-C",classes,"."],log)
    run([java,"-cp",d8,"com.android.tools.r8.D8","--release","--min-api","24","--lib",sdk,"--classpath",stub_jar,"--output",dex,host_jar],log)
    content=(dex/"classes.dex").read_bytes();validate_dex(content)
    assert all(name.startswith("Lcom/karen/sts2/") for name in dex_classes(content)),"Compile-only stub leaked"
    return content


def strip_launcher(jdk,carrier,toolchain,log):
    classes=OUT/"strip-tool";classes.mkdir(exist_ok=True)
    dex=OUT/"engine-dex";dex.mkdir(exist_ok=True)
    exe=".exe" if os.name=="nt" else ""
    jars=[toolchain/"guava-27.1-android.jar",toolchain/"dexlib2-2.5.2.jar"]
    run([jdk/("bin/javac"+exe),"-encoding","UTF-8","-cp",os.pathsep.join(map(str,jars)),"-d",classes,HERE/"host-tools/StripLauncher.java"],log)
    output=run([jdk/("bin/java"+exe),"-Xmx2g","-cp",os.pathsep.join(map(str,[classes,*jars])),"StripLauncher",carrier,dex],log)
    print(output.strip(),flush=True)
    paths=sorted(dex.glob("classes*.dex"))
    for path in paths:
        content=path.read_bytes();validate_dex(content)
        assert all(not name.startswith("Lcom/godot/game/") for name in dex_classes(content))
        assert b"Lcom/godot/game/" not in content,"Retained library still depends on launcher code"
    return paths


def patch_bridge(content):
    content=patch_managed_package(content,APP_ID)
    old="com.godot.game.GodotApp".encode("utf-16-le");new="com.karen.sts2.GameHost".encode("utf-16-le")
    assert len(old)==len(new) and old in content
    allowed=[bounds for name,bounds in managed_metadata_streams(content).items() if name in ("#US","#Blob")]
    cursor=0
    while True:
        index=content.find(old,cursor)
        if index<0:break
        assert any(start<=index and index+len(old)<=end for start,end in allowed)
        cursor=index+len(old)
    return content.replace(old,new)


def stage_payload(game,mod_zip,carrier):
    release=json.loads((game/"release_info.json").read_text(encoding="utf-8"))
    assert release["version"]=="v0.107.1","This direct build is pinned to v0.107.1"
    data=game/"data_sts2_windows_x86_64"
    assert sha(data/"sts2.dll")=="a1f9e653f1e28e4076558fee1e60d218619cb7e057b887c6417f62c62c6d7a52"
    root=OUT/"payload";root.mkdir(exist_ok=True)
    files={}
    def put(name,content):
        path=root/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(content);files[name]=path
    with zipfile.ZipFile(carrier) as source:
        protected=set()
        for name in source.namelist():
            if name.startswith("assets/dotnet_bcl/") and not name.endswith("/"):
                base=name.rsplit("/",1)[1];protected.add(base)
                if base!="STS2Mobile.dll":put(PUBLISH+base,source.read(name))
        with zipfile.ZipFile(io.BytesIO(source.read("assets/compat_packs/sts2-android-compat.zip"))) as pack:
            base="sts2-android-compat/variants/v0.107.1/"
            compat=pack.read(base+"STS2Mobile.dll")
            put(PUBLISH+"STS2Mobile.dll",patch_bridge(compat));put("port_compat.pck",pack.read(base+"port_compat.pck"))
        for path in data.iterdir():
            name=path.name
            if not path.is_file() or name in protected or name.startswith("System.") or name in ("mscorlib.dll","netstandard.dll","Microsoft.CSharp.dll","Microsoft.VisualBasic.dll","Microsoft.VisualBasic.Core.dll"):continue
            if name.endswith(".json") or (name.endswith(".dll") and managed(path.read_bytes())):files[PUBLISH+name]=path
    files["game/release_info.json"]=game/"release_info.json"
    print("Staging and checking private PCK copy",flush=True)
    pck=patch_pck(game/"SlayTheSpire2.pck",root/"game/SlayTheSpire2.pck");files["game/SlayTheSpire2.pck"]=root/"game/SlayTheSpire2.pck"
    assert pck["source_sha256"]=="42520eb8b0911c6c0f0bd102d92b33f41abd4d26b83489817d0a6dbd7dd48587", "Unexpected stable game PCK"
    assert pck["sha256"]=="3e8e1c104b86d5b7448359bc70673610b2bf6e958851962fdde0514a9f5fdd95", "Unexpected staged PCK; refresh private payload cache"
    with zipfile.ZipFile(mod_zip) as archive:
        for name in archive.namelist():
            if not name.endswith("/"):put("mods/"+name,archive.read(name))
    lines=[]
    for name,path in sorted(files.items()):lines.append(sha(path)+"\t"+str(path.stat().st_size)+"\t"+name)
    manifest=("\n".join(lines)+"\n").encode()
    (OUT/"payload-manifest.tsv").write_bytes(manifest)
    (OUT/"pck-patch.json").write_text(json.dumps(pck,indent=2)+"\n",encoding="utf-8")
    return files,manifest,pck


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-dir",type=Path,required=True)
    parser.add_argument("--mod-zip",type=Path,default=ROOT/"artifacts/android/ShoujoKagekiAijoKaren-Android-v0.1.3.zip")
    parser.add_argument("--jdk",type=Path)
    args=parser.parse_args();game=args.game_dir.resolve();mod_zip=args.mod_zip.resolve();assert check_mod_zip(mod_zip)=="v0.1.3","Direct build Mod version must match v0.1.3"
    OUT.mkdir(parents=True,exist_ok=True)
    carrier,apktool,d8,signer=[dependency(name) for name in ("upstream","apktool","d8","signer")]
    jdk=java_home(args.jdk);toolchain,sdk=extra_tools();java=jdk/("bin/java.exe" if os.name=="nt" else "bin/java")
    with (OUT/"build.log").open("w",encoding="utf-8") as log:
        run([sys.executable,HERE/"test.py","--jdk",jdk],log)
        engine=strip_launcher(jdk,carrier,toolchain,log);host=compile_host(jdk,sdk,d8,log)
        aapt=toolchain/("aapt2.exe" if os.name=="nt" else "aapt2")
        platform="windows" if os.name=="nt" else ("macosx" if sys.platform=="darwin" else "linux")
        with zipfile.ZipFile(apktool) as archive:aapt.write_bytes(archive.read("prebuilt/"+platform+"/"+aapt.name))
        if os.name!="nt":aapt.chmod(0o755)
        skeleton=OUT/"manifest.apk"
        run([aapt,"link","--manifest",HERE/"AndroidManifest.xml","-I",sdk,"-I",carrier,"-o",skeleton],log)
        defaults=json.loads((HERE/"default-settings.json").read_text(encoding="utf-8"))
        assert defaults["schema_version"]==5 and defaults["mod_settings"]["mods_enabled"] and defaults["mod_settings"]["mod_list"]==[]
        files,manifest,pck=stage_payload(game,mod_zip,carrier)
        run(["dotnet","run","--project",HERE/"settings-tests/SettingsTests.csproj","--configuration","Release",
             "-p:GameDirectory="+str(game),"--",HERE/"default-settings.json"],log)
        unsigned=OUT/"StS2-Karen-v0.107.1-v0.1.3-direct-r3-unsigned.apk"
        print("Assembling full game APK",flush=True)
        with zipfile.ZipFile(carrier) as source,zipfile.ZipFile(skeleton) as base,zipfile.ZipFile(unsigned,"w",allowZip64=False) as target:
            target.writestr("AndroidManifest.xml",base.read("AndroidManifest.xml"),compress_type=zipfile.ZIP_DEFLATED)
            for info in source.infolist():
                name=info.filename
                if name=="resources.arsc":target.writestr(info,patch_resource_package(source.read(info),APP_ID))
                elif name.startswith(("res/","lib/")) or (name.startswith("META-INF/") and not name.upper().endswith((".RSA",".DSA",".EC",".SF","MANIFEST.MF"))):target.writestr(info,source.read(info))
                elif "license" in name.lower() and not name.endswith("/"):target.writestr(info,source.read(info))
            for path in engine:target.write(path,path.name)
            target.writestr("classes"+str(len(engine)+1)+".dex",host,compress_type=zipfile.ZIP_DEFLATED)
            target.writestr("assets/direct/manifest.tsv",manifest,compress_type=zipfile.ZIP_DEFLATED)
            target.write(HERE/"default-settings.json","assets/direct/default-settings.json",compress_type=zipfile.ZIP_DEFLATED)
            for name,path in files.items():target.write(path,"assets/direct/files/"+name,compress_type=zipfile.ZIP_STORED if name.endswith(".pck") else zipfile.ZIP_DEFLATED)
            for path in (HERE.parent/"android-apk/licenses").iterdir():target.write(path,"assets/direct/licenses/"+path.name)
            target.writestr("assets/direct/NOTICE.txt","Standalone Karen game host. Original launcher application classes are removed.\nGodot/Mono/FMOD/Spine runtime dependencies and Android compatibility layer are reused from the SHA-pinned v0.1.11 runtime carrier.\nThe selected v0.107.1 compatibility DLL has only package/fallback path/Java bridge string replacements; its code is unchanged.\nThis local artifact embeds the user's PC game files. Game files are not added to Git.\nSource: tools/android-standalone/ in the Karen Mod repository.\n")
        assert unsigned.stat().st_size<2**31,"APK crossed the tested 2 GiB archive boundary"
        print("Signing",round(unsigned.stat().st_size/2**20,1),"MiB",flush=True)
        signing=ROOT/"artifacts/apk/signing";signing.mkdir(parents=True,exist_ok=True)
        key=signing/"karen-test.p12";password_file=signing/"test-key-password.txt"
        if key.exists() and not password_file.exists():raise RuntimeError("Test keystore password file is missing; preserve the existing signing key")
        if not password_file.exists():password_file.write_text(secrets.token_urlsafe(32),encoding="utf-8")
        password=password_file.read_text(encoding="utf-8").strip()
        if not key.exists():
            environment=dict(os.environ,KAREN_APK_KEY_PASSWORD=password)
            run([jdk/("bin/keytool.exe" if os.name=="nt" else "bin/keytool"),"-genkeypair","-keystore",key,"-storetype","PKCS12",
                 "-storepass:env","KAREN_APK_KEY_PASSWORD","-alias","karen-test","-keyalg","RSA","-keysize","2048",
                 "-sigalg","SHA256withRSA","-validity","10000","-dname","CN=Karen Test Build,O=Karen Mod","-noprompt"],log,environment)
        signed=OUT/("signed-"+uuid.uuid4().hex[:8])
        run([java,"-Xmx3g","-jar",signer,"--apks",unsigned,"--out",signed,"--ks",key,"--ksAlias","karen-test","--ksPass",password,"--ksKeyPass",password],log)
        choices=list(signed.glob("*-aligned-signed.apk"));assert len(choices)==1
        final=OUT/"StS2-Karen-v0.107.1-v0.1.3-direct-r3.apk";shutil.copyfile(choices[0],final)
        signature=run([java,"-Xmx3g","-cp",signer,"com.android.apksigner.ApkSignerTool","verify","--verbose","--print-certs",final],log)
        (OUT/"signature.txt").write_text(signature,encoding="utf-8")
        badging=run([aapt,"dump","badging",final],log);(OUT/"badging.txt").write_text(badging,encoding="utf-8")
        assert "package: name='"+APP_ID+"'" in badging and "versionCode='10015'" in badging
        assert "launchable-activity: name='com.karen.sts2.PrepareGameActivity'" in badging and "native-code: 'arm64-v8a'" in badging
        with zipfile.ZipFile(final) as package:
            assert json.loads(package.read("assets/direct/default-settings.json"))==defaults
            document=list(elements(package.read("AndroidManifest.xml")))
            metadata={attrs["name"]:attrs.get("value") for tag,attrs in document if tag=="meta-data"}
            assert metadata["org.godotengine.plugin.v2.FmodPlugin"]=="com.utopiarise.godot.fmod.android.plugin.FmodPlugin"
            assert metadata["org.godotengine.library.version"]=="4.5.1.stable"
            activities=[attrs["name"] for tag,attrs in document if tag=="activity"]
            assert activities==["com.karen.sts2.PrepareGameActivity","com.karen.sts2.GameHost","org.godotengine.godot.utils.ProcessPhoenix"]
            classes=[]
            for name in package.namelist():
                if re.fullmatch(r"classes(?:[0-9]+)?\.dex",name):
                    content=package.read(name);validate_dex(content);classes.extend(dex_classes(content))
            assert not any(name.startswith("Lcom/godot/game/") for name in classes)
            assert all(name not in package.namelist() for name in ["assets/bootstrap.pck","assets/compat_packs/sts2-android-compat.zip"])
            for name,path in files.items():
                with package.open("assets/direct/files/"+name) as stream:assert hashlib.file_digest(stream,"sha256").hexdigest()==sha(path),name
            assert package.testzip() is None
        record={"apk":final.name,"bytes":final.stat().st_size,"sha256":sha(final),"application_id":APP_ID,"game_version":"v0.107.1","mod_version":"v0.1.3","version_code":10015,
                "embedded_program_files":len(files),"payload_manifest_sha256":hashlib.sha256(manifest).hexdigest(),"engine_dependency_classes":sum(len(list(dex_classes(path.read_bytes()))) for path in engine),"game_host_classes":len(list(dex_classes(host))),
                "launcher_application_classes":0,"launcher_activities":0,"native_architectures":["arm64-v8a"],"pck":pck,
                "default_game_settings_schema":5,"game_settings_deserialization":"passed against v0.107.1 SettingsSave","fmod_plugin_registered":True,"installer_jvm_regressions":12,"signature":"verified v2/v3; local test key","android_device_tested":False}
        (OUT/"validation.json").write_text(json.dumps(record,indent=2)+"\n",encoding="utf-8")
        print("Built",final.relative_to(ROOT),record["bytes"],"bytes SHA256",record["sha256"],flush=True)


if __name__=="__main__":main()
