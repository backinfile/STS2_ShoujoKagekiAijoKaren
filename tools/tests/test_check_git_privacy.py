"""End-to-end checks for staged content, archives, metadata, and commit identity."""
import gzip
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / 'tools/check_git_privacy.py'


class PrivacyCheckTests(unittest.TestCase):
    def setUp(self):
        self.base = (ROOT / 'artifacts/privacy-tests').resolve()
        self.base.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix='fixture-', dir=self.base)
        self.repo = Path(self.temp.name).resolve()
        self.repo.relative_to(self.base)
        self.git('init', '--quiet')
        self.git('config', 'user.name', 'Privacy Fixture')
        self.git('config', 'user.email', 'fixture@example.invalid')
        self.private_home = chr(67) + ':' + chr(92) + chr(92).join(['Users', 'PRIVATE_READER', 'capture.json'])

    def tearDown(self):
        self.repo.relative_to(self.base)
        self.temp.cleanup()

    def git(self, *args):
        return subprocess.run(['git', *args], cwd=self.repo, check=True, capture_output=True)

    def track(self, name, data):
        path = self.repo / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        self.git('add', '--force', '--', name)

    def scan(self, *args):
        result = subprocess.run([sys.executable, '-X', 'utf8', str(CHECKER), *args], cwd=self.repo, capture_output=True, text=True, encoding='utf-8')
        self.assertNotIn('PRIVATE_READER', result.stdout + result.stderr)
        return result

    def test_staged_secret_is_caught_after_working_copy_is_fixed(self):
        self.track('capture.json', json.dumps({'source': self.private_home}).encode())
        (self.repo / 'capture.json').write_text('{"source":"capture.json"}', encoding='utf-8')
        self.assertEqual(self.scan().returncode, 0)
        staged = self.scan('--staged')
        self.assertEqual(staged.returncode, 1)
        self.assertIn('personal-directory', staged.stdout)

    def test_archive_text_and_utf16_are_checked(self):
        archive = self.repo / 'evidence.zip'
        with zipfile.ZipFile(archive, 'w') as output:
            output.writestr('capture.txt.gz', gzip.compress(self.private_home.encode('utf-16')))
        self.git('add', '--', 'evidence.zip')
        result = self.scan('--staged')
        self.assertEqual(result.returncode, 1)
        self.assertIn('evidence.zip::capture.txt.gz::gzip', result.stdout)
        self.assertIn('personal-directory', result.stdout)

    def test_binary_metadata_is_checked(self):
        self.track('reference.jpg', b'\xff\xd8metadata\0' + self.private_home.encode())
        result = self.scan()
        self.assertEqual(result.returncode, 1)
        self.assertIn('reference.jpg:', result.stdout)
        self.assertIn('personal-directory', result.stdout)

    def test_hashes_example_env_and_private_identity_pass(self):
        self.track('hashes.json', json.dumps({'hash': 'a' + ''.join(['138', '0013', '8000']) + 'f' * 52, 'contact': 'fixture@example.invalid'}).encode())
        self.track('.env.example', b'API_KEY=${API_KEY}\n')
        result = self.scan('--staged')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_personal_email_is_rejected_without_exposing_it(self):
        email = 'fixture' + '@' + 'private.test'
        self.git('config', 'user.email', email)
        result = self.scan()
        self.assertEqual(result.returncode, 1)
        self.assertIn('author-email', result.stdout)
        self.assertIn('committer-email', result.stdout)
        self.assertNotIn(email, result.stdout + result.stderr)

    def test_secret_filenames_unreadable_archives_and_tokens_fail(self):
        self.track('.env', b'local configuration')
        self.track('broken.zip', b'PK\x03\x04invalid')
        token = ''.join(['gh', 'p_']) + 'aB93' * 10
        self.track('notes.txt', token.encode())
        result = self.scan('--staged')
        self.assertEqual(result.returncode, 1)
        for rule in ('secret-file', 'unreadable-archive', 'platform-token'):
            self.assertIn(rule, result.stdout)
        self.assertNotIn(token, result.stdout + result.stderr)


if __name__ == '__main__':
    unittest.main()
