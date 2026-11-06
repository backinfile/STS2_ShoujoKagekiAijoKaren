"""Small, checked APK manifest/DEX edits; every unrelated resource stays byte-identical."""
import hashlib
import struct
import zlib

OLD_ID = "com.megacrit.sts2re"
APP_ID = "com.karenmod.sts2re"
APPLICATION = "com.karen.sts2.KarenApplication"
LABEL = "华恋 · 塔2（测试版）"
VERSION_CODE = 113001
VERSION_NAME = "v0.1.11-karen.1"
NO_INDEX = 0xFFFFFFFF


def length8(data, offset):
    value = data[offset]
    return (((value & 127) << 8) | data[offset + 1], offset + 2) if value & 128 else (value, offset + 1)


def length16(data, offset):
    value = struct.unpack_from("<H", data, offset)[0]
    return (((value & 32767) << 16) | struct.unpack_from("<H", data, offset + 2)[0], offset + 4) if value & 32768 else (value, offset + 2)


def pack8(value):
    assert value < 32768
    return bytes([(value >> 8) | 128, value & 255]) if value > 127 else bytes([value])


def pack16(value):
    return struct.pack("<HH", (value >> 16) | 32768, value & 65535) if value > 32767 else struct.pack("<H", value)


def chunks(data, start=8):
    offset = start
    while offset < len(data):
        kind, header, size = struct.unpack_from("<HHI", data, offset)
        assert size >= header >= 8 and offset + size <= len(data)
        yield kind, bytearray(data[offset:offset + size])
        offset += size
    assert offset == len(data)


def read_pool(chunk):
    count, styles, flags, strings_start, styles_start = struct.unpack_from("<IIIII", chunk, 8)
    header = struct.unpack_from("<H", chunk, 2)[0]
    result = []
    for index in range(count):
        position = strings_start + struct.unpack_from("<I", chunk, header + 4 * index)[0]
        if flags & 256:
            _, position = length8(chunk, position)
            size, position = length8(chunk, position)
            result.append(bytes(chunk[position:position + size]).decode("utf-8"))
        else:
            size, position = length16(chunk, position)
            result.append(bytes(chunk[position:position + 2 * size]).decode("utf-16-le"))
    return result


def write_pool(chunk, strings):
    old_count, styles, flags, _, styles_start = struct.unpack_from("<IIIII", chunk, 8)
    header = struct.unpack_from("<H", chunk, 2)[0]
    style_offsets = chunk[header + old_count * 4:header + (old_count + styles) * 4]
    style_data = chunk[styles_start:] if styles_start else b""
    offsets, data = [], bytearray()
    for value in strings:
        offsets.append(len(data))
        utf16 = value.encode("utf-16-le")
        if flags & 256:
            utf8 = value.encode("utf-8")
            data += pack8(len(utf16) // 2) + pack8(len(utf8)) + utf8 + b"\0"
        else:
            data += pack16(len(utf16) // 2) + utf16 + b"\0\0"
    data += b"\0" * (-len(data) % 4)
    strings_start = header + 4 * (len(strings) + styles)
    styles_start = strings_start + len(data) if styles else 0
    result = bytearray(chunk[:header])
    struct.pack_into("<IIIII", result, 8, len(strings), styles, flags & ~1, strings_start, styles_start)
    result += struct.pack("<" + "I" * len(offsets), *offsets) + style_offsets + data + style_data
    struct.pack_into("<I", result, 4, len(result))
    return result


def elements(data):
    strings = None
    for kind, chunk in chunks(data):
        if kind == 1:
            strings = read_pool(chunk)
        elif kind == 0x102:
            _, name, start, size, count = struct.unpack_from("<IIHHH", chunk, 16)
            attributes = {}
            for index in range(count):
                offset = 16 + start + index * size
                _, key, raw, _, _, value_type, value = struct.unpack_from("<IIIHBBI", chunk, offset)
                attributes[strings[key]] = strings[value] if value_type == 3 else value
            yield strings[name], attributes


def patch_manifest(data):
    assert struct.unpack_from("<H", data)[0] == 3
    parts = list(chunks(data))
    pool_index = next(i for i, (kind, _) in enumerate(parts) if kind == 1)
    strings = read_pool(parts[pool_index][1])
    assert OLD_ID in strings and "com.godot.game.Sts2Application" in strings
    strings = [s.replace(OLD_ID, APP_ID).replace("com.godot.game.Sts2Application", APPLICATION) for s in strings]
    def add(value):
        if value not in strings: strings.append(value)
        return strings.index(value)
    label_index, version_index = add(LABEL), add(VERSION_NAME)
    edits = []
    for kind, chunk in parts:
        if kind != 0x102: continue
        _, name, start, size, count = struct.unpack_from("<IIHHH", chunk, 16)
        tag = strings[name]
        attributes = []
        for index in range(count):
            offset = 16 + start + index * size
            _, key, _, _, _, value_type, value = struct.unpack_from("<IIIHBBI", chunk, offset)
            attributes.append((strings[key], offset, value_type, value))
        component = next((strings[v] for k, _, t, v in attributes if k == "name" and t == 3), "")
        for key, offset, value_type, value in attributes:
            new_type, new_value = None, None
            if key == "label" and value_type == 1 and value == 0x7F110110:
                new_type, new_value = 3, label_index
            elif tag == "manifest" and key == "versionName":
                new_type, new_value = 3, version_index
            elif tag == "manifest" and key == "versionCode":
                new_type, new_value = 0x10, VERSION_CODE
            elif tag == "application" and key == "debuggable":
                new_type, new_value = 0x12, 0
            elif key == "exported" and component in ("com.godot.game.DebugAutomationActivity", "com.godot.game.DebugAutomationReceiver"):
                new_type, new_value = 0x12, 0
            if new_type is not None:
                struct.pack_into("<I", chunk, offset + 8, new_value if new_type == 3 else NO_INDEX)
                struct.pack_into("<HBBI", chunk, offset + 12, 8, 0, new_type, new_value)
                edits.append((tag, key))
    assert ("manifest", "versionCode") in edits and ("manifest", "versionName") in edits
    parts[pool_index] = (1, write_pool(parts[pool_index][1], strings))
    result = bytearray(data[:8]) + b"".join(chunk for _, chunk in parts)
    struct.pack_into("<I", result, 4, len(result))
    parsed = list(elements(result))
    root = next(attrs for name, attrs in parsed if name == "manifest")
    app = next(attrs for name, attrs in parsed if name == "application")
    assert root["package"] == APP_ID and root["versionCode"] == VERSION_CODE
    assert root["versionName"] == VERSION_NAME and app["name"] == APPLICATION and app["label"] == LABEL
    assert OLD_ID.encode() not in result
    return result


def dex_strings(data):
    count, start = struct.unpack_from("<II", data, 56)
    for index in range(count):
        offset = struct.unpack_from("<I", data, start + index * 4)[0]
        while data[offset] & 128: offset += 1
        offset += 1
        end = data.index(0, offset)
        yield offset, end, bytes(data[offset:end])


def validate_dex(data):
    assert data[:4] == b"dex\n" and struct.unpack_from("<I", data, 32)[0] == len(data)
    assert data[12:32] == hashlib.sha1(data[32:]).digest()
    assert struct.unpack_from("<I", data, 8)[0] == zlib.adler32(data[12:]) & 0xFFFFFFFF


def patch_dex(data):
    validate_dex(data)
    old, new = OLD_ID.encode(), APP_ID.encode()
    assert len(old) == len(new)
    if old not in data: return data
    entries = list(dex_strings(data))
    assert data.count(old) == sum(value.count(old) for _, _, value in entries), "Package text outside string data"
    result = bytearray(data.replace(old, new))
    changed_strings = list(dex_strings(result))
    # DEX string IDs are sorted by UTF-16 value. Same-length edits must preserve this order.
    # Check both neighbours of every affected ASCII string; unrelated MUTF-8 strings stay unchanged.
    for index, (_, _, value) in enumerate(changed_strings):
        if new not in value: continue
        assert value.isascii()
        if index: assert changed_strings[index - 1][2] < value
        if index + 1 < len(entries): assert value < changed_strings[index + 1][2]
    result[12:32] = hashlib.sha1(result[32:]).digest()
    struct.pack_into("<I", result, 8, zlib.adler32(result[12:]) & 0xFFFFFFFF)
    validate_dex(result)
    return bytes(result)


def dex_classes(data):
    strings = [value.replace(b"\xc0\x80", b"\0").decode("utf-8", errors="surrogatepass") for _, _, value in dex_strings(data)]
    _, type_start = struct.unpack_from("<II", data, 64)
    count, start = struct.unpack_from("<II", data, 96)
    for index in range(count):
        type_index = struct.unpack_from("<I", data, start + 32 * index)[0]
        string_index = struct.unpack_from("<I", data, type_start + 4 * type_index)[0]
        yield strings[string_index]


SHORTCUTS = ("res/xml/game_shortcuts.xml", "res/xml/settings_shortcuts.xml")


def patch_shortcuts(data):
    parts = list(chunks(data))
    for index, (kind, chunk) in enumerate(parts):
        if kind == 1:
            strings = read_pool(chunk)
            assert OLD_ID in strings
            parts[index] = (kind, write_pool(chunk, [s.replace(OLD_ID, APP_ID) for s in strings]))
    result = bytearray(data[:8]) + b"".join(chunk for _, chunk in parts)
    struct.pack_into("<I", result, 4, len(result))
    intents = [attrs for tag, attrs in elements(result) if tag == "intent"]
    assert len(intents) == 2 and all(attrs["targetPackage"] == APP_ID for attrs in intents)
    return result


COMPAT_PACKS = ("assets/compat_packs/sts2-android-compat.zip", "assets/compat_packs/sts2-android-offline-bootstrap.zip")
MANAGED_COMPAT = "assets/dotnet_bcl/STS2Mobile.dll"


def patch_resource_package(data, application_id=APP_ID):
    result = bytearray(data)
    count = 0
    start = struct.unpack_from("<H", data, 2)[0]
    for kind, chunk in chunks(data, start):
        if kind == 0x200:
            package_name = bytes(chunk[12:268]).decode("utf-16-le").rstrip("\0")
            if package_name == OLD_ID:
                encoded = application_id.encode("utf-16-le")
                result[start + 12:start + 268] = encoded + b"\0" * (256 - len(encoded))
                count += 1
        start += len(chunk)
    assert count == 1 and len(result) == len(data)
    return bytes(result)


def managed_metadata_streams(data):
    assert data[:2] == b"MZ"
    pe = struct.unpack_from("<I", data, 60)[0]
    assert data[pe:pe + 4] == b"PE\0\0"
    sections = struct.unpack_from("<H", data, pe + 6)[0]
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    assert struct.unpack_from("<H", data, optional)[0] == 0x10B
    def rva_offset(address):
        for index in range(sections):
            section = optional + optional_size + index * 40
            vsize, rva, raw_size, raw = struct.unpack_from("<IIII", data, section + 8)
            if rva <= address < rva + max(vsize, raw_size): return raw + address - rva
        raise ValueError("Invalid managed RVA")
    cli = rva_offset(struct.unpack_from("<I", data, optional + 96 + 14 * 8)[0])
    flags = struct.unpack_from("<I", data, cli + 16)[0]
    assert flags & 1 and not flags & 8, "Refuse native or strong-name-signed compatibility DLL"
    metadata = rva_offset(struct.unpack_from("<I", data, cli + 8)[0])
    assert data[metadata:metadata + 4] == b"BSJB"
    version_size = struct.unpack_from("<I", data, metadata + 12)[0]
    position = metadata + 16 + ((version_size + 3) & ~3)
    count = struct.unpack_from("<H", data, position + 2)[0]
    position += 4
    streams = {}
    for _ in range(count):
        offset, size = struct.unpack_from("<II", data, position)
        end = data.index(0, position + 8)
        name = data[position + 8:end].decode("ascii")
        streams[name] = (metadata + offset, metadata + offset + size)
        position = (end + 4) & ~3
    return streams


def patch_managed_package(data, application_id=APP_ID):
    old, new = OLD_ID.encode("utf-16-le"), application_id.encode("utf-16-le")
    assert len(old) == len(new) and old in data
    streams = managed_metadata_streams(data)
    allowed = [span for name, span in streams.items() if name in ("#US", "#Blob")]
    cursor, count = 0, 0
    while True:
        position = data.find(old, cursor)
        if position < 0: break
        assert any(start <= position and position + len(old) <= end for start, end in allowed), "Package text outside managed string/constant heaps"
        cursor = position + len(old)
        count += 1
    result = data.replace(old, new)
    assert count >= 1 and len(result) == len(data)
    assert managed_metadata_streams(result) == streams
    assert old not in result
    return result


def patch_compat_pack(data):
    import io
    import zipfile
    output = io.BytesIO()
    patched = 0
    with zipfile.ZipFile(io.BytesIO(data)) as source, zipfile.ZipFile(output, "w") as target:
        for info in source.infolist():
            content = source.read(info)
            if info.filename.endswith("/STS2Mobile.dll"):
                content = patch_managed_package(content)
                patched += 1
            target.writestr(info, content)
    assert patched > 0
    return output.getvalue()
