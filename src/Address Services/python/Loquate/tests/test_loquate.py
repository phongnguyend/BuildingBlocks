import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from app import LoqateClient, LoqateVerifyAddress


class _Response:
    def __init__(self, payload):
        self._body = json.dumps(payload).encode("utf-8")

    def __enter__(self):
        return self

    def __exit__(self, *args):
        return None

    def read(self):
        return self._body


class LoqateClientTests(unittest.IsolatedAsyncioTestCase):
    def test_reads_api_key_from_dotenv(self):
        with tempfile.TemporaryDirectory() as directory:
            env_file = Path(directory) / ".env"
            env_file.write_text(
                "# Loqate settings\nexport LOQATE_API_KEY='dotenv-secret'\n",
                encoding="utf-8",
            )
            with patch.dict(os.environ, {}, clear=True):
                client = LoqateClient(env_file=env_file)

        self.assertEqual("dotenv-secret", client._api_key)

    def test_environment_key_takes_priority_over_dotenv(self):
        with tempfile.TemporaryDirectory() as directory:
            env_file = Path(directory) / ".env"
            env_file.write_text("LOQATE_API_KEY=dotenv-secret\n", encoding="utf-8")
            with patch.dict(os.environ, {"LOQATE_API_KEY": "environment-secret"}):
                client = LoqateClient(env_file=env_file)

        self.assertEqual("environment-secret", client._api_key)

    def test_explicit_key_takes_priority_over_environment(self):
        with patch.dict(os.environ, {"LOQATE_API_KEY": "environment-secret"}):
            client = LoqateClient("explicit-secret")

        self.assertEqual("explicit-secret", client._api_key)

    async def test_find_maps_items_and_query(self):
        payload = {"Items": [{"Id": "GB|1", "Text": "London"}]}
        with patch("app.client.urlopen", return_value=_Response(payload)) as send:
            results = await LoqateClient("secret").find_async(
                "a b", countries="GB", limit=5
            )

        self.assertEqual("GB|1", results[0].id)
        self.assertIn("Text=a+b", send.call_args.args[0].full_url)
        self.assertIn("Countries=GB", send.call_args.args[0].full_url)
        self.assertIn("Limit=5", send.call_args.args[0].full_url)

    async def test_retrieve_returns_first_address(self):
        payload = {"Items": [{"Id": "GB|1", "Line1": "85 Gresham Street"}]}
        with patch("app.client.urlopen", return_value=_Response(payload)):
            address = await LoqateClient("secret").retrieve_async("GB|1")

        self.assertIsNotNone(address)
        self.assertEqual("85 Gresham Street", address.line1)

    async def test_single_verify_uses_v120_and_returns_first_match(self):
        payload = [{"Matches": [{"Address": "verified", "AVC": "V44"}]}]
        with patch("app.client.urlopen", return_value=_Response(payload)) as send:
            result = await LoqateClient("secret").verify_async(
                LoqateVerifyAddress(address="input", country="GB"), geocode=True
            )

        request = send.call_args.args[0]
        body = json.loads(request.data)
        self.assertIn("/v1.20/json6.ws", request.full_url)
        self.assertTrue(body["Geocode"])
        self.assertEqual("GB", body["Addresses"][0]["Country"])
        self.assertEqual("verified", result.address)

    async def test_batch_verify_uses_v100_and_maps_results(self):
        payload = [
            {
                "Input": {"Address": "input"},
                "Matches": [{"Address": "verified", "Latitude": "1.2"}],
            }
        ]
        with patch("app.client.urlopen", return_value=_Response(payload)) as send:
            results = await LoqateClient("secret").verify_batch_async(
                [LoqateVerifyAddress(address="input")]
            )

        self.assertIn("/v1.00/json4.ws", send.call_args.args[0].full_url)
        self.assertEqual("input", results[0].input.address)
        self.assertEqual("1.2", results[0].matches[0].latitude)


if __name__ == "__main__":
    unittest.main()
