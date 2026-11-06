import io
import unittest
from unittest.mock import Mock, patch
from urllib.error import HTTPError
from urllib.request import Request

from jev import JevConnection


class TransportTests(unittest.TestCase):
    def test_two_requests_reuse_one_tls_connection(self):
        responses = [io.BytesIO(b'{}'), io.BytesIO(b'{}')]
        for response in responses:
            response.status = 200
        connection = Mock()
        connection.sock = Mock()
        connection.getresponse.side_effect = responses
        with patch('jev.HTTPSConnection', return_value=connection) as factory:
            transport = JevConnection()
            for _ in range(2):
                with transport.open(Request('https://api.typesafe.ai/v1/systemone', data=b'{}'), timeout=45) as response:
                    response.read()
        factory.assert_called_once()
        self.assertEqual(connection.request.call_count, 2)
        connection.close.assert_not_called()

    def test_broken_connection_is_closed_without_replaying_request(self):
        connection = Mock()
        connection.sock = Mock()
        connection.request.side_effect = OSError('broken pipe')
        with patch('jev.HTTPSConnection', return_value=connection):
            transport = JevConnection()
            with self.assertRaises(OSError):
                transport.open(Request('https://api.typesafe.ai/v1/systemone', data=b'{}'))
        self.assertEqual(connection.request.call_count, 1)
        connection.close.assert_called_once()

    def test_redirect_is_rejected_without_following(self):
        response = Mock(status=302, headers={'Location': 'https://example.org'})
        connection = Mock()
        connection.sock = Mock()
        connection.getresponse.return_value = response
        with patch('jev.HTTPSConnection', return_value=connection):
            transport = JevConnection()
            with self.assertRaises(HTTPError):
                transport.open(Request('https://api.typesafe.ai/v1/systemone', data=b'{}'))
        self.assertEqual(connection.request.call_count, 1)


if __name__ == '__main__':
    unittest.main()
