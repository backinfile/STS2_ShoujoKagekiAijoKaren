"""Build a locally signed Karen importer APK from a fixed upstream ARM64 runtime.

No base-game files are included. All dependencies are pinned by SHA-256. A JDK is
portable; no Android SDK, Gradle or native-runtime recompilation is required.
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
import subprocess
import sys
import urllib.request
import zipfile
import uuid

from apk_format import APP_ID, APPLICATION, LABEL, VERSION_CODE, VERSION_NAME, OLD_ID, elements, patch_manifest, patch_dex, validate_dex, dex_classes, SHORTCUTS, patch_shortcuts, COMPAT_PACKS, MANAGED_COMPAT, patch_resource_package, patch_managed_package, patch_compat_pack

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
OUT = ROOT / "artifacts/apk"
DEPENDENCIES = {
    "apktool": ("downloads/apktool_3.0.3.jar", "https://github.com/iBotPeaches/Apktool/releases/download/v3.0.3/apktool_3.0.3.jar", "dbf930b076c6b9be08d57c449cacefc3bdd6b71ebd59b3066fc0e1f5b14f9423"),
    "upstream": ("downloads/sts2-re-v0.1.11-importer.apk", "https://github.com/ModinMobileSTS/Sts2MobileLauncher/releases/download/v0.1.11/sts2-re-v0.1.11-importer.apk", "54c6cf6d1cf7fcf611f64f63a66bab8714c085ad8175c3deda274b245659a131"),
    "signer": ("downloads/uber-apk-signer-1.3.0.jar", "https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar", "e1299fd6fcf4da527dd53735b56127e8ea922a321128123b9c32d619bba1d835"),
    "d8": ("toolchain/r8-8.6.27.jar", "https://dl.google.com/dl/android/maven2/com/android/tools/r8/8.6.27/r8-8.6.27.jar", "9007144728d47b5f11338c9292d5b74899b6579ad9ddc6942d419e3600c3d6d7"),
    "jdk": ("toolchain/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip", "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip", "f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e"),
}


def sha(path):
    with open(path, "rb") as stream: return hashlib.file_digest(stream, "sha256").hexdigest()


def dependency(name):
    relative, url, checksum = DEPENDENCIES[name]
    path = OUT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        print("Downloading", name, flush=True)
        temporary = path.with_suffix(path.suffix + ".partial")
        urllib.request.urlretrieve(url, temporary)
        if sha(temporary) != checksum: raise ValueError("Download checksum mismatch: " + name)
        temporary.replace(path)
    if sha(path) != checksum: raise ValueError("Cached dependency checksum mismatch: " + name)
    return path


def run(args, log, env=None):
    effective_env = dict(os.environ if env is None else env)
    effective_env["JAVA_TOOL_OPTIONS"] = effective_env.get("JAVA_TOOL_OPTIONS", "") + " -Dfile.encoding=UTF-8 -Duser.language=en -Duser.country=US"
    result = subprocess.run([str(arg) for arg in args], cwd=ROOT, env=effective_env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log.write(result.stdout.decode("utf-8", errors="replace"))
    log.flush()
    if result.returncode: raise RuntimeError("Build tool failed; see artifacts/apk/build.log (exit %d)" % result.returncode)
    return result.stdout.decode("utf-8", errors="replace")


def java_home(requested):
    if requested: return requested.resolve()
    if os.name != "nt": raise ValueError("Pass --jdk for a JDK 21 installation on this platform")
    archive = dependency("jdk")
    target = OUT / "toolchain/jdk-21.0.12.1+1"
    if not (target / "bin/java.exe").is_file():
        with zipfile.ZipFile(archive) as package: package.extractall(OUT / "toolchain")
    return target


def check_mod_zip(path):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        for entry in archive.infolist():
            name = entry.filename
            parts = name.split("/")
            assert len(parts) >= 2 and parts[0] in ("ShoujoKagekiAijoKaren", "BaseLib")
            assert ".." not in parts and "\\" not in name
        for mod_id in ("ShoujoKagekiAijoKaren", "BaseLib"):
            manifest = json.loads(archive.read(mod_id + "/" + mod_id + ".json"))
            assert manifest["id"] == mod_id
            for suffix in ("dll", "pck"): assert mod_id + "/" + mod_id + "." + suffix in archive.namelist()
        version = json.loads(archive.read("ShoujoKagekiAijoKaren/ShoujoKagekiAijoKaren.json"))["version"]
    return version


def compile_bootstrap(jdk, d8, mod_sha, log):
    work = OUT / "bootstrap"
    generated, stub_classes, classes, dex = [work / name for name in ("generated", "stubs", "classes", "dex")]
    for path in (generated, stub_classes, classes, dex): path.mkdir(parents=True, exist_ok=True)
    (generated / "BuildInfo.java").write_text('package com.karen.sts2; final class BuildInfo { static final String MOD_SHA256 = "' + mod_sha + '"; }\n', encoding="utf-8")
    exe = ".exe" if os.name == "nt" else ""
    javac, jar, java = [jdk / ("bin/" + name + exe) for name in ("javac", "jar", "java")]
    run([javac, "--release", "8", "-encoding", "UTF-8", "-d", stub_classes, *sorted((HERE / "compile-stubs").rglob("*.java"))], log)
    stub_jar, app_jar = work / "compile-stubs.jar", work / "karen-bootstrap.jar"
    run([jar, "cf", stub_jar, "-C", stub_classes, "."], log)
    run([javac, "--release", "8", "-encoding", "UTF-8", "-cp", stub_classes, "-d", classes, *sorted((HERE / "java").rglob("*.java")), generated / "BuildInfo.java"], log)
    run([jar, "cf", app_jar, "-C", classes, "."], log)
    run([java, "-cp", d8, "com.android.tools.r8.D8", "--release", "--min-api", "24", "--lib", jdk, "--classpath", stub_jar, "--output", dex, app_jar], log)
    result = (dex / "classes.dex").read_bytes()
    validate_dex(result)
    assert all(name.startswith("Lcom/karen/sts2/") for name in dex_classes(result)), "Compile-only stub leaked into APK"
    assert b"Lcom/karen/sts2/KarenApplication;" in result
    assert b"Lcom/godot/game/Sts2Application;" in result  # reference only, the stub is never packaged
    with zipfile.ZipFile(app_jar) as app:
        assert all(not name.startswith("com/godot/") for name in app.namelist())
    return result


def unsigned_apk(upstream, mod_zip, bootstrap, version):
    path = OUT / ("Karen-StS2-Android-" + version + "-unsigned.apk")
    metadata = {"application_id": APP_ID, "version_name": VERSION_NAME, "mod_version": version,
                "mod_zip_sha256": sha(mod_zip), "upstream_tag": "v0.1.11", "upstream_apk_sha256": sha(upstream),
                "supported_game_versions": ["0.107.1", "0.111.0"], "android_device_tested": False}
    with zipfile.ZipFile(upstream) as source, zipfile.ZipFile(path, "w", allowZip64=False) as target:
        assert "classes18.dex" not in source.namelist()
        for info in source.infolist():
            # Discard the old APK signature. Library metadata and licences remain intact.
            if info.filename.startswith("META-INF/") and info.filename.upper().endswith((".RSA", ".DSA", ".EC", ".SF", "MANIFEST.MF")): continue
            content = source.read(info)
            if info.filename == "AndroidManifest.xml": content = patch_manifest(content)
            elif re.fullmatch(r"classes(?:[0-9]+)?\.dex", info.filename): content = patch_dex(content)
            elif info.filename in SHORTCUTS: content = patch_shortcuts(content)
            elif info.filename == "resources.arsc": content = patch_resource_package(content)
            elif info.filename == MANAGED_COMPAT: content = patch_managed_package(content)
            elif info.filename in COMPAT_PACKS: content = patch_compat_pack(content)
            info.extra = b""  # final zipalign regenerates alignment padding
            target.writestr(info, content)
        target.writestr("classes18.dex", bootstrap, compress_type=zipfile.ZIP_DEFLATED)
        target.write(mod_zip, "assets/karen/bundled-mods.zip", compress_type=zipfile.ZIP_STORED)
        target.writestr("assets/karen/build-info.json", json.dumps(metadata, indent=2) + "\n", compress_type=zipfile.ZIP_DEFLATED)
        target.write(HERE / "licenses/Sts2MobileLauncher-MIT.txt", "assets/karen/licenses/Sts2MobileLauncher-MIT.txt")
        target.write(HERE / "licenses/Sts2MobileLauncher-THIRD_PARTY_LICENSES.md", "assets/karen/licenses/Sts2MobileLauncher-THIRD_PARTY_LICENSES.md")
        target.writestr("assets/karen/NOTICE.txt", "Karen test APK, based on ModinMobileSTS/Sts2MobileLauncher v0.1.11.\nhttps://github.com/ModinMobileSTS/Sts2MobileLauncher/tree/v0.1.11\nOriginal native Android/Mono/FMOD/Spine libraries, images and their notices are retained. The app ID and managed compatibility fallback directory strings are retargeted to the Karen package.\nKaren and BaseLib are bundled as a Mod ZIP. Import your own matching base game.\nSource/build instructions: tools/android-apk/ and docs/android-mod.md in the Karen Mod repository.\n")
    return path


def validate_apk(path, upstream, mod_zip):
    with zipfile.ZipFile(upstream) as source, zipfile.ZipFile(path) as result:
        assert result.testzip() is None
        original = set(source.namelist())
        managed_patches = []
        def record_managed(name, before, after):
            managed_patches.append({"file": name, "original_sha256": hashlib.sha256(before).hexdigest(),
                                    "sha256": hashlib.sha256(after).hexdigest(),
                                    "package_string_occurrences": before.count(OLD_ID.encode("utf-16-le"))})
        for name in original:
            if name == "AndroidManifest.xml" or re.fullmatch(r"classes(?:[0-9]+)?\.dex", name): continue
            if name.startswith("META-INF/") and name.upper().endswith((".RSA", ".DSA", ".EC", ".SF", "MANIFEST.MF")): continue
            if name in SHORTCUTS:
                assert result.read(name) == patch_shortcuts(source.read(name))
                continue
            if name == "resources.arsc":
                assert result.read(name) == patch_resource_package(source.read(name))
                continue
            if name == MANAGED_COMPAT:
                assert result.read(name) == patch_managed_package(source.read(name))
                record_managed(name, source.read(name), result.read(name))
                continue
            if name in COMPAT_PACKS:
                assert result.read(name) == patch_compat_pack(source.read(name))
                with zipfile.ZipFile(io.BytesIO(source.read(name))) as before, zipfile.ZipFile(io.BytesIO(result.read(name))) as after:
                    for entry in before.namelist():
                        if entry.endswith("/STS2Mobile.dll"):
                            record_managed(name + "/" + entry, before.read(entry), after.read(entry))
                        else: assert before.read(entry) == after.read(entry), "Unexpected compatibility pack change: " + entry
                continue
            assert source.read(name) == result.read(name), "Unintended change: " + name
        for name in result.namelist():
            content = result.read(name)
            assert all(old not in content for old in (OLD_ID.encode(), OLD_ID.encode("utf-16-le"), OLD_ID.replace(".", "/").encode())), "Stale application ID: " + name
            if name in COMPAT_PACKS:
                with zipfile.ZipFile(io.BytesIO(content)) as inner:
                    for item in inner.namelist():
                        value = inner.read(item)
                        assert OLD_ID.encode() not in value and OLD_ID.encode("utf-16-le") not in value, "Stale compat application ID: " + item
        dex_files = [name for name in result.namelist() if re.fullmatch(r"classes(?:[0-9]+)?\.dex", name)]
        assert len(dex_files) == 18
        for name in dex_files:
            content = result.read(name)
            validate_dex(content)
            assert OLD_ID.encode() not in content
            if name != "classes18.dex": assert content == patch_dex(source.read(name))
        document = list(elements(result.read("AndroidManifest.xml")))
        root = next(attrs for tag, attrs in document if tag == "manifest")
        app = next(attrs for tag, attrs in document if tag == "application")
        sdk = next(attrs for tag, attrs in document if tag == "uses-sdk")
        assert root["package"] == APP_ID and root["versionCode"] == VERSION_CODE and root["versionName"] == VERSION_NAME
        assert app["name"] == APPLICATION and app["label"] == LABEL and not app["debuggable"]
        assert sdk["minSdkVersion"] == 24 and sdk["targetSdkVersion"] == 35
        for tag, attrs in document:
            if attrs.get("name") in ("com.godot.game.DebugAutomationActivity", "com.godot.game.DebugAutomationReceiver"):
                assert not attrs["exported"]
        assert hashlib.sha256(result.read("assets/karen/bundled-mods.zip")).hexdigest() == sha(mod_zip)
        natives = [name for name in original if name.startswith("lib/") and name.endswith(".so")]
        assert len(natives) == 21 and all(name.startswith("lib/arm64-v8a/") for name in natives)
        assert len(managed_patches) == 10
        return {"zip_crc": "passed", "upstream_entries_retained": len(original), "native_libraries_preserved": len(natives),
                "dex_files": len(dex_files), "shortcut_intents_retargeted": 4, "managed_compat_assemblies_retargeted": len(managed_patches), "managed_compat_assemblies": managed_patches, "manifest": root, "min_sdk": 24, "target_sdk": 35,
                "mod_zip_sha256": sha(mod_zip), "android_device_tested": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mod-zip", type=Path, default=ROOT / "artifacts/android/ShoujoKagekiAijoKaren-Android-v0.1.3.zip")
    parser.add_argument("--jdk", type=Path)
    args = parser.parse_args()
    mod_zip = args.mod_zip.resolve()
    version = check_mod_zip(mod_zip)
    OUT.mkdir(parents=True, exist_ok=True)
    upstream, signer, d8 = [dependency(name) for name in ("upstream", "signer", "d8")]
    jdk = java_home(args.jdk)
    java = jdk / ("bin/java.exe" if os.name == "nt" else "bin/java")
    with (OUT / "build.log").open("w", encoding="utf-8") as log:
        run([sys.executable, HERE / "test_bootstrap.py", "--jdk", jdk], log)
        bootstrap = compile_bootstrap(jdk, d8, sha(mod_zip), log)
        unsigned = unsigned_apk(upstream, mod_zip, bootstrap, version)
        print("Compiled bootstrap and assembled APK", flush=True)
        signing = OUT / "signing"
        signing.mkdir(exist_ok=True)
        password_file, keystore = signing / "test-key-password.txt", signing / "karen-test.p12"
        if not password_file.exists(): password_file.write_text(secrets.token_urlsafe(32), encoding="utf-8")
        password = password_file.read_text(encoding="utf-8").strip()
        if not keystore.exists():
            env = dict(os.environ, KAREN_APK_KEY_PASSWORD=password)
            run([jdk / ("bin/keytool.exe" if os.name == "nt" else "bin/keytool"), "-genkeypair", "-keystore", keystore,
                 "-storetype", "PKCS12", "-storepass:env", "KAREN_APK_KEY_PASSWORD", "-alias", "karen-test",
                 "-keyalg", "RSA", "-keysize", "2048", "-sigalg", "SHA256withRSA", "-validity", "10000",
                 "-dname", "CN=Karen Test Build,O=Karen Mod", "-noprompt"], log, env)
        signed = OUT / ("signed-" + uuid.uuid4().hex[:8])
        run([java, "-jar", signer, "--apks", unsigned, "--out", signed, "--ks", keystore,
             "--ksAlias", "karen-test", "--ksPass", password, "--ksKeyPass", password], log)
        candidates = list(signed.glob(unsigned.stem.removesuffix("-unsigned") + "*-signed.apk"))
        assert len(candidates) == 1, "Expected exactly one signed APK"
        final = OUT / ("Karen-StS2-Android-" + version + "-test.apk")
        shutil.copyfile(candidates[0], final)
        verification = run([java, "-cp", signer, "com.android.apksigner.ApkSignerTool", "verify", "--verbose", "--print-certs", final], log)
        (OUT / "signature-verification.txt").write_text(verification, encoding="utf-8")
        apktool = dependency("apktool")
        aapt = OUT / "toolchain" / ("aapt2.exe" if os.name == "nt" else "aapt2")
        platform = "windows" if os.name == "nt" else ("macosx" if sys.platform == "darwin" else "linux")
        with zipfile.ZipFile(apktool) as archive:
            aapt.write_bytes(archive.read("prebuilt/" + platform + "/" + aapt.name))
        if os.name != "nt": aapt.chmod(0o755)
        badging = run([aapt, "dump", "badging", final], log)
        (OUT / "apk-badging.txt").write_text(badging, encoding="utf-8")
        assert "package: name='" + APP_ID + "'" in badging
        assert "versionCode='113001'" in badging and "minSdkVersion:'24'" in badging
        assert "native-code: 'arm64-v8a'" in badging
        assert "launchable-activity: name='com.godot.game.GameSettingsActivity'" in badging
        receipt = validate_apk(final, upstream, mod_zip)
        receipt["aapt2_badging"] = "passed"
        receipt["signer_certificate_sha256"] = re.search(r"certificate SHA-256 digest: ([0-9a-f]+)", verification).group(1)
        receipt.update({"apk": final.name, "apk_sha256": sha(final), "bytes": final.stat().st_size,
                        "bootstrap_jvm_checks": 9, "signature": "verified; local test key", "application_id": APP_ID, "upstream_tag": "v0.1.11",
                        "dependencies": {name: {"url": url, "sha256": checksum} for name, (_, url, checksum) in DEPENDENCIES.items()}})
        (OUT / "apk-validation.json").write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print("Built", final.relative_to(ROOT), "SHA-256", receipt["apk_sha256"], flush=True)


if __name__ == "__main__": main()
