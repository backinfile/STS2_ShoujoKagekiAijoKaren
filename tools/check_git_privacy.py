#!/usr/bin/env python3
"""Check tracked files (or the complete staged index) without printing sensitive values.

Run: python tools/check_git_privacy.py [--staged]
Historical commits and visible/audible media content need separate review.
"""
import argparse
from collections import Counter
import gzip
import io
from pathlib import Path
import re
import subprocess
import sys
import tarfile
import zipfile

LIMIT = 256 * 1024 * 1024
MEDIA = {'.png', '.jpg', '.jpeg', '.webp', '.gif', '.mp4', '.mp3', '.wav', '.ogg', '.ogv', '.pck', '.dll', '.pdb', '.ttf', '.woff', '.woff2'}
HOME = re.compile(rb"(?i)(?:[A-Z]:[\\/]+Users[\\/]+|/(?:Users|home)/)[^\\/\s\"<>:;,'`]+")
RULES = {
    'personal-directory': HOME,
    'private-key': re.compile(rb'-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----'),
    'platform-token': re.compile(rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|sk-(?:proj-|ant-api\d+-)?[A-Za-z0-9_-]{24,}|AIza[A-Za-z0-9_-]{30,}|(?:AKIA|ASIA)[A-Z0-9]{16}|xox[baprs]-[A-Za-z0-9-]{20,})\b'),
    'url-secret': re.compile(rb'(?i)[?&](?:access_token|api_key|key|token|password|secret)=([A-Za-z0-9_-]{8,})'),
    'url-password': re.compile(rb'(?i)\b(?:https?|postgres(?:ql)?|mysql|redis|mongodb(?:\+srv)?)://[^\s/:\"\x27]{1,80}:[^\s/@\"\x27]{3,160}@'),
    'email': re.compile(rb'(?<![\w.+-])[A-Za-z0-9][A-Za-z0-9._%+-]{0,63}@[A-Za-z0-9][A-Za-z0-9.-]*\.[A-Za-z]{2,24}\b'),
    'phone': re.compile(rb'(?<![\w.])(?:\+86[ -]?)?1[3-9]\d{9}(?![\w.])'),
}
SAMPLE_ENV = {'.env.example', '.env.sample', '.env.template'}
SECRET_NAMES = {'.env', 'access.json', 'credentials.json', 'secrets.json', 'id_rsa', 'id_ed25519', 'htpasswd'}
SECRET_EXT = {'.pem', '.key', '.p12', '.pfx', '.jks', '.keystore', '.har'}


def git(*args):
    return subprocess.check_output(['git', *args], stderr=subprocess.PIPE)


def private_email(value):
    value = value.lower()
    return not (value.endswith((b'@users.noreply.github.com', b'.invalid', b'@example.com', b'@example.org', b'@example.net')))


def check_identity(issues):
    for kind in ('AUTHOR', 'COMMITTER'):
        identity = git('var', 'GIT_' + kind + '_IDENT')
        match = re.search(rb'<([^<>]+)>', identity)
        if match is None or private_email(match.group(1)):
            issues.append(('(git identity)', 1, kind.lower() + '-email'))


def inspect(data, name, issues, depth=0):
    if HOME.search(name.encode('utf-8')):
        issues.append((name, 1, 'personal-filename'))
    leaf = Path(name.rsplit('::', 1)[-1]).name.lower()
    if leaf in SECRET_NAMES or Path(leaf).suffix in SECRET_EXT or (leaf.startswith('.env.') and leaf not in SAMPLE_ENV):
        issues.append((name, 1, 'secret-file'))
    if len(data) > LIMIT or depth > 4:
        issues.append((name, 1, 'inspection-limit'))
        return
    if data.startswith(b'PK\x03\x04'):
        try:
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                total = 0
                for item in archive.infolist():
                    if item.is_dir():
                        continue
                    total += item.file_size
                    if total > LIMIT:
                        issues.append((name, 1, 'inspection-limit'))
                        break
                    inspect(archive.read(item), name + '::' + item.filename, issues, depth + 1)
        except (OSError, ValueError, RuntimeError, zipfile.BadZipFile):
            issues.append((name, 1, 'unreadable-archive'))
        return
    if data.startswith(b'\x1f\x8b'):
        try:
            if leaf.endswith(('.tar.gz', '.tgz')):
                with tarfile.open(fileobj=io.BytesIO(data), mode='r|gz') as archive:
                    total = 0
                    for item in archive:
                        if not item.isfile():
                            continue
                        total += item.size
                        if total > LIMIT:
                            issues.append((name, 1, 'inspection-limit'))
                            break
                        stream = archive.extractfile(item)
                        if stream is not None:
                            inspect(stream.read(), name + '::' + item.name, issues, depth + 1)
            else:
                with gzip.GzipFile(fileobj=io.BytesIO(data)) as stream:
                    inspect(stream.read(LIMIT + 1), name + '::gzip', issues, depth + 1)
        except (OSError, EOFError, ValueError, tarfile.TarError):
            issues.append((name, 1, 'unreadable-archive'))
        return
    binary = Path(leaf).suffix in MEDIA or b'\0' in data[:8192]
    if data.startswith((b'\xff\xfe', b'\xfe\xff')):
        data = data.decode('utf-16').encode('utf-8')
        binary = False
    for rule, pattern in RULES.items():
        if binary and rule in {'email', 'phone'}:
            continue
        for match in pattern.finditer(data):
            value = match.group(1) if match.lastindex else match.group()
            if rule == 'email' and not private_email(value):
                continue
            if rule == 'url-secret' and any(word in value.lower() for word in (b'example', b'placeholder', b'redacted', b'dummy')):
                continue
            issues.append((name, data.count(b'\n', 0, match.start()) + 1, rule))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--staged', action='store_true', help='Read the entire index, including partially staged files.')
    args = parser.parse_args()
    root = Path(git('rev-parse', '--show-toplevel').decode().strip())
    issues = []
    check_identity(issues)
    records = git('ls-files', '--stage', '-z').split(b'\0')
    files = 0
    batch = None
    try:
        if args.staged:
            batch = subprocess.Popen(['git', 'cat-file', '--batch'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        for record in records:
            if not record:
                continue
            meta, raw_path = record.split(b'\t', 1)
            mode, oid, stage = meta.split()
            name = raw_path.decode('utf-8')
            if stage != b'0' or mode == b'160000':
                issues.append((name, 1, 'unverifiable-index-entry'))
                continue
            files += 1
            try:
                if batch is not None:
                    batch.stdin.write(oid + b'\n')
                    batch.stdin.flush()
                    header = batch.stdout.readline().split()
                    data = batch.stdout.read(int(header[2]))
                    batch.stdout.read(1)
                else:
                    data = (root / name).read_bytes()
                inspect(data, name, issues)
            except (OSError, ValueError, UnicodeError):
                issues.append((name, 1, 'unreadable-file'))
    finally:
        if batch is not None:
            batch.stdin.close()
            batch.wait()
    for name, line, rule in sorted(set(issues)):
        safe_name = HOME.sub(b'<redacted-directory>', name.encode('utf-8')).decode('utf-8')
        print(f'{safe_name}:{line}: {rule}')
    counts = Counter(rule for _, _, rule in issues)
    print(f"Checked {files} tracked files ({'index' if args.staged else 'working tree'}); {len(issues)} findings.")
    if counts:
        print(', '.join(f'{rule}={count}' for rule, count in sorted(counts.items())))
    return 1 if issues else 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except subprocess.CalledProcessError:
        print('Git inspection failed; run inside an accessible repository.', file=sys.stderr)
        sys.exit(2)
