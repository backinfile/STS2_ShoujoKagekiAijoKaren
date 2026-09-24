import io
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch
from urllib.error import HTTPError

from combat import candidates
from jev import JevPolicy
from prompting import action_label


class JevTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads(Path(__file__).with_name('example-en.json').read_text(encoding='utf-8'))
        self.options = candidates(self.state)
        with patch.dict(os.environ, {'TYPESAFE_API_KEY': 'unit-test-secret'}):
            self.policy = JevPolicy(decision_mode='two-orders')

    def result(self):
        labels = [action_label(o, self.state) for o in self.options]
        answer = {'type': 'choice', 'choice': labels[0], 'confidence': 1,
                  'probabilities': {label: int(i == 0) for i, label in enumerate(labels)}}
        return {'answers': {'forward': answer, 'reverse': answer}, 'model': 'jev-test',
                'usage': {'input_tokens': 100, 'output_tokens': 20}}

    def test_http_shape_order_mapping_and_metadata(self):
        result = self.result()
        with patch.object(self.policy.http, 'open', return_value=io.BytesIO(json.dumps(result).encode())) as send:
            selected, info = self.policy.decide(self.state, self.options)
        request = send.call_args.args[0]
        body = json.loads(request.data)
        self.assertEqual(request.full_url, 'https://api.typesafe.ai/v1/systemone')
        self.assertEqual(request.get_header('Authorization'), 'Bearer unit-test-secret')
        self.assertEqual(list(body['questions']['forward']['criteria']),
                         list(reversed(body['questions']['reverse']['criteria'])))
        self.assertEqual(selected, self.options[0])
        self.assertEqual(info['resolved_model'], 'jev-test')
        self.assertEqual(info['usage']['input_tokens'], 100)
        self.assertNotIn('unit-test-secret', json.dumps(info))

    def test_missing_question_and_illegal_choice_rejected(self):
        for invalid in ['missing', 'illegal']:
            result = self.result()
            if invalid == 'missing':
                del result['answers']['reverse']
            else:
                result['answers']['forward']['choice'] = 'Invented action'
            with patch.object(self.policy.http, 'open', return_value=io.BytesIO(json.dumps(result).encode())):
                with self.assertRaises(RuntimeError):
                    self.policy.decide(self.state, self.options)

    def test_http_failure_redacted_and_not_retried(self):
        error = HTTPError('https://api.typesafe.ai/v1/systemone', 401, 'unit-test-secret', {}, None)
        with patch.object(self.policy.http, 'open', side_effect=error) as send:
            with self.assertRaisesRegex(RuntimeError, 'HTTP 401') as captured:
                self.policy.decide(self.state, self.options)
        self.assertEqual(send.call_count, 1)
        self.assertNotIn('unit-test-secret', str(captured.exception))

    def test_reflected_key_rejected_and_redirect_not_followed(self):
        result = self.result()
        result['model'] = 'unit-test-secret'
        with patch.object(self.policy.http, 'open', return_value=io.BytesIO(json.dumps(result).encode())):
            with self.assertRaisesRegex(RuntimeError, 'response discarded'):
                self.policy.decide(self.state, self.options)


if __name__ == '__main__':
    unittest.main()
