"""TypeSafe's documented decision endpoint, with no Laya dependency."""
import json
import os
import time
import math
import hashlib
from http.client import HTTPSConnection, HTTPException
from urllib.error import HTTPError, URLError
from urllib.request import Request

from combat import Policy


class JevConnection:
    """One persistent TLS connection; failed requests are never replayed."""
    def __init__(self):
        self.connection = None
        self.timing = {}

    def close(self):
        if self.connection is not None:
            self.connection.close()
            self.connection = None

    def open(self, request, timeout=45):
        if request.full_url != 'https://api.typesafe.ai/v1/systemone':
            raise ValueError('Unexpected Jev endpoint')
        started = time.perf_counter()
        reused = self.connection is not None and self.connection.sock is not None
        if self.connection is None:
            self.connection = HTTPSConnection('api.typesafe.ai', timeout=timeout)
        connection = self.connection
        try:
            if connection.sock is None:
                connection.connect()
            connection.sock.settimeout(timeout)
            connected = time.perf_counter()
            connection.request(request.get_method(), '/v1/systemone', body=request.data,
                               headers=dict(request.header_items()))
            response = connection.getresponse()
            self.timing = {'connection_reused': reused,
                           'connect_tls_seconds': round(connected - started, 4),
                           'request_to_headers_seconds': round(time.perf_counter() - connected, 4)}
            if response.status != 200:
                status, headers = response.status, response.headers
                response.close()
                raise HTTPError(request.full_url, status, 'Jev HTTP failure', headers, None)
            return response
        except Exception:
            self.close()
            raise


class JevPolicy(Policy):
    method = 'jev_choice_two_orders_v6_flow'

    def __init__(self, model='jev-1.13.0', decision_mode='composite'):
        self.model = model
        self.decision_mode = decision_mode
        self._key = os.environ.get('TYPESAFE_API_KEY', '').strip()
        if not self._key:
            raise RuntimeError('Set TYPESAFE_API_KEY or configure the local encrypted key via set-key.ps1')
        self.http = JevConnection()

    def decide(self, state, options):
        if len(options) == 1:
            return options[0], {'method': 'only_legal_action', 'confidence': None}
        from decision_input import build_input, questions_for, DIMENSIONS, input_budget, within_budget
        payload, labels, strategy = build_input(state, options)
        mode = self.decision_mode
        questions = questions_for(payload, strategy, mode)
        budget = input_budget(payload, questions)
        if not within_budget(budget) and mode != 'single':
            mode = 'single'
            questions = questions_for(payload, strategy, mode)
            budget = input_budget(payload, questions)
        if not within_budget(budget):
            raise RuntimeError('Decision input exceeds conservative byte budget; no facts truncated and no action sent')
        result, _ = self.predict(payload, questions)
        answers = result['answers']
        if set(answers) != set(questions):
            raise RuntimeError('Invalid model probabilities: missing or unexpected question answers')
        for key, question in questions.items():
            answer = answers[key]
            if not isinstance(answer, dict):
                raise RuntimeError('Invalid model answer')
            expected = set(question['criteria'])
            scores = answer.get('probabilities', {})
            if answer.get('type') != question['type'] or set(scores) != expected:
                raise RuntimeError('Invalid model answer type or probability labels')
            if any(type(v) not in (int, float) or not math.isfinite(v) or not 0 <= v <= 1 for v in scores.values()):
                raise RuntimeError('Invalid model probabilities')
            # Live Jev responses round each probability to two decimal places.
            if sum(scores.values()) <= 0 or abs(sum(scores.values()) - 1) > 0.005 * len(scores) + 1e-8:
                raise RuntimeError('Model probabilities do not sum to one')
            if question['type'] == 'choice' and answer.get('choice') not in labels:
                raise RuntimeError('Invalid model choice')
        utilities = {}
        dimensions = {}
        for index, name in enumerate(labels):
            if mode == 'composite':
                dimensions[name] = {dimension: answers[dimension]['probabilities'][name]
                                    for dimension in DIMENSIONS}
                utilities[name] = sum(DIMENSIONS[d][0] * v for d, v in dimensions[name].items())
            else:
                utilities[name] = sum(a['probabilities'][name] / sum(a['probabilities'].values())
                                      for a in answers.values()) / len(answers)
        best = max(utilities, key=utilities.get)
        ordered = sorted(utilities.values(), reverse=True)
        # Composite utilities are not a probability distribution or calibrated confidence.
        concentration = None
        if mode != 'composite':
            entropy = -sum(p * math.log(p) for p in utilities.values() if p > 0)
            concentration = max(0.0, min(1.0, 1 - entropy / math.log(len(options))))
        return labels[best], {
            'method': 'jev_' + ('multi_choice' if mode == 'composite' else mode) + '_structured_v8', 'confidence': concentration,
            'requested_mode': self.decision_mode, 'input_budget': budget,
            'mode_change_reason': 'conservative_input_budget' if mode != self.decision_mode else None,
            'confidence_kind': 'choice_distribution_concentration_not_win_probability' if concentration is not None else 'unavailable_for_composite',
            'order_agreement': (answers['forward']['choice'] == answers['reverse']['choice']) if mode == 'two-orders' else None,
            'margin': ordered[0] - ordered[1], 'utility_scores': utilities, 'dimensions': dimensions,
            'weights': {d: v[0] for d, v in DIMENSIONS.items()} if mode == 'composite' else {},
            'answers': answers, 'input_tokens': {}, 'prompt': payload, 'questions': questions,
            'resolved_model': result.get('model'), 'usage': result.get('usage'),
            'api_timing': result.get('_timing'),
            'strategy_sha256': hashlib.sha256(strategy.encode('utf-8')).hexdigest()}

    def predict(self, payload, questions):
        started = time.perf_counter()
        if any(q['type'] == 'choice' and len(q['criteria']) > 255 for q in questions.values()):
            raise RuntimeError('Jev supports at most 255 options per Choice')
        # Do not sort keys: preserve forward/reverse candidate order on the wire.
        body = json.dumps({'model': self.model, 'state': payload, 'questions': questions},
                          ensure_ascii=False).encode('utf-8')
        request = Request('https://api.typesafe.ai/v1/systemone', data=body,
                          headers={'Authorization': 'Bearer ' + self._key,
                                   'Content-Type': 'application/json'})
        try:
            # No automatic retries: avoid duplicate billable requests after timeouts.
            with self.http.open(request, timeout=45) as response:
                result = json.load(response)
        except HTTPError as exc:
            raise RuntimeError(f'Jev API HTTP {exc.code}; no action sent') from None
        except (URLError, TimeoutError, OSError, ValueError, HTTPException):
            self.http.close()
            raise RuntimeError('Jev API connection or JSON response failed; no action sent') from None
        if not isinstance(result, dict) or not isinstance(result.get('answers'), dict):
            raise RuntimeError('Invalid Jev response; no action sent')
        # Reject reflected credentials before anything from the API can reach logs.
        if self._key in json.dumps(result, ensure_ascii=False):
            raise RuntimeError('Unexpected credential in API response; response discarded')
        result['_timing'] = dict(self.http.timing, api_seconds=round(time.perf_counter() - started, 4))
        return result, {}  # API usage is separate from Laya's per-question token count.
