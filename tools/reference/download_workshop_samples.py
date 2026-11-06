"""Download research samples through the running Steam client without subscribing.

Uses the installed game's Steam API; copies downloaded packages to an isolated
research directory. Does not launch the game or install files in game/mods.
"""
from pathlib import Path
import argparse
import ctypes as c
import hashlib
import json
import os
import shutil
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-api', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--timeout', type=int, default=600)
    parser.add_argument('items', nargs='+', type=int)
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    (output / '.gdignore').write_text('', encoding='utf-8')
    os.environ['SteamAppId'] = '2868840'
    os.environ['SteamGameId'] = '2868840'
    dll = c.CDLL(str(args.steam_api.resolve()))

    def bind(name, result, params):
        fn = getattr(dll, name)
        fn.restype = result
        fn.argtypes = params
        return fn

    init = bind('SteamAPI_InitSafe', c.c_bool, [])
    shutdown = bind('SteamAPI_Shutdown', None, [])
    pump = bind('SteamAPI_RunCallbacks', None, [])
    if not init():
        raise RuntimeError('Steam API initialization failed; Steam must be running and own app 2868840.')
    try:
        ugc = None
        interface_version = None
        for version in range(30, 15, -1):
            name = f'SteamAPI_SteamUGC_v{version:03d}'
            if hasattr(dll, name):
                ugc = bind(name, c.c_void_p, [])()
                interface_version = name
                if ugc:
                    break
        if not ugc:
            raise RuntimeError('Steam UGC interface unavailable')
        download = bind('SteamAPI_ISteamUGC_DownloadItem', c.c_bool, [c.c_void_p, c.c_uint64, c.c_bool])
        state = bind('SteamAPI_ISteamUGC_GetItemState', c.c_uint32, [c.c_void_p, c.c_uint64])
        info = bind('SteamAPI_ISteamUGC_GetItemDownloadInfo', c.c_bool,
                    [c.c_void_p, c.c_uint64, c.POINTER(c.c_uint64), c.POINTER(c.c_uint64)])
        installed = bind('SteamAPI_ISteamUGC_GetItemInstallInfo', c.c_bool,
                         [c.c_void_p, c.c_uint64, c.POINTER(c.c_uint64), c.c_void_p, c.c_uint32, c.POINTER(c.c_uint32)])
        records = {'appid': 2868840, 'interface': interface_version, 'subscription_changed': False, 'items': []}
        for item in args.items:
            requested = download(ugc, item, True)
            print(json.dumps({'item': item, 'download_requested': requested}), flush=True)
            start = time.monotonic()
            last = None
            while requested and time.monotonic() - start < args.timeout:
                pump()
                current = state(ugc, item)
                downloaded, total = c.c_uint64(), c.c_uint64()
                info(ugc, item, c.byref(downloaded), c.byref(total))
                progress = (current, downloaded.value // (1024 * 1024), total.value // (1024 * 1024))
                if progress != last:
                    print(json.dumps({'item': item, 'state': current, 'downloaded_mib': progress[1], 'total_mib': progress[2]}), flush=True)
                    last = progress
                if current & 4 and not current & (8 | 16 | 32):
                    break
                time.sleep(0.25)
            folder, size, stamp = c.create_string_buffer(4096), c.c_uint64(), c.c_uint32()
            ok = installed(ugc, item, c.byref(size), folder, len(folder), c.byref(stamp))
            record = {'item_id': str(item), 'workshop_url': f'https://steamcommunity.com/sharedfiles/filedetails/?id={item}',
                      'requested': requested, 'state': state(ugc, item), 'installed_info': ok}
            if ok and record['state'] & 4 and not record['state'] & (8 | 16 | 32):
                origin = Path(folder.value.decode('utf-8'))
                destination = output / 'packages' / str(item)
                shutil.copytree(origin, destination, dirs_exist_ok=True)
                files = []
                for path in sorted(destination.rglob('*')):
                    if path.is_file():
                        files.append({'path': path.relative_to(destination).as_posix(), 'bytes': path.stat().st_size,
                                      'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
                record.update(source_folder=str(origin), local_folder=str(destination), bytes_on_disk=size.value,
                              workshop_timestamp=stamp.value, files=files)
            records['items'].append(record)
            (output / 'download-provenance.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
            print(json.dumps({'item': item, 'copied': 'files' in record, 'files': len(record.get('files', []))}), flush=True)
    finally:
        shutdown()


if __name__ == '__main__':
    main()
