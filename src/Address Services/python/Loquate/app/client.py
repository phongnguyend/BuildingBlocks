"""Asynchronous client for the Loqate Capture and Verify APIs."""

from __future__ import annotations

import asyncio
import json
import os
from collections.abc import Iterable
from os import PathLike
from typing import Any, overload
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

from dotenv import load_dotenv

from .models import (
    LoqateAddress,
    LoqateFindItem,
    LoqateVerifiedAddress,
    LoqateVerifyAddress,
    LoqateVerifyOptions,
    LoqateVerifyResult,
)


class LoqateHttpError(RuntimeError):
    """Raised when Loqate returns a non-success HTTP response."""

    def __init__(self, status: int, reason: str, body: str) -> None:
        super().__init__(f"Request failed with status {status} ({reason}): {body}")
        self.status = status
        self.reason = reason
        self.body = body


class LoqateClient:
    def __init__(
        self,
        api_key: str | None = None,
        *,
        base_url: str = "https://api.addressy.com",
        timeout: float = 30.0,
        env_file: str | PathLike[str] | None = ".env",
    ) -> None:
        resolved_api_key = api_key
        if resolved_api_key is None:
            if env_file is not None:
                load_dotenv(dotenv_path=env_file, override=False)
            resolved_api_key = os.environ.get("LOQATE_API_KEY")
        if not resolved_api_key:
            raise ValueError(
                "Loqate API key is missing; pass api_key, set LOQATE_API_KEY, "
                "or add LOQATE_API_KEY to .env"
            )
        self._api_key = resolved_api_key
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout

    async def find_async(
        self,
        search_text: str,
        container: str | None = None,
        countries: str | None = None,
        limit: int = 10,
    ) -> list[LoqateFindItem]:
        payload = await self._get_json(
            "Capture/Interactive/Find/v1.20/json6.ws",
            {
                "Key": self._api_key,
                "Text": search_text,
                "Container": container,
                "Countries": countries,
                "Limit": str(limit),
            },
        )
        items = payload.get("Items", []) if isinstance(payload, dict) else []
        return [
            LoqateFindItem.from_api(item)
            for item in items
            if isinstance(item, dict)
        ]

    async def retrieve_async(self, id: str) -> LoqateAddress | None:
        payload = await self._get_json(
            "Capture/Interactive/Retrieve/v1.30/json6.ws",
            {"Key": self._api_key, "Id": id},
        )
        items = payload.get("Items", []) if isinstance(payload, dict) else []
        first = next((item for item in items if isinstance(item, dict)), None)
        return LoqateAddress.from_api(first) if first is not None else None

    @overload
    async def verify_async(
        self,
        address: LoqateVerifyAddress,
        *,
        geocode: bool = False,
        certify: bool = False,
        enhance: bool = False,
    ) -> LoqateVerifiedAddress | None: ...

    @overload
    async def verify_async(
        self,
        address: Iterable[LoqateVerifyAddress],
        *,
        geocode: bool = False,
        certify: bool = False,
        enhance: bool = False,
    ) -> list[LoqateVerifyResult]: ...

    async def verify_async(
        self,
        address: LoqateVerifyAddress | Iterable[LoqateVerifyAddress],
        *,
        geocode: bool = False,
        certify: bool = False,
        enhance: bool = False,
    ) -> LoqateVerifiedAddress | None | list[LoqateVerifyResult]:
        """Verify one address, or dispatch an iterable to batch verification."""
        if not isinstance(address, LoqateVerifyAddress):
            return await self.verify_batch_async(address)

        request = {
            "Key": self._api_key,
            "Geocode": geocode,
            "Options": LoqateVerifyOptions(
                process="Verify", certify=certify, enhance=enhance
            ).to_api(),
            "Addresses": [address.to_api()],
        }
        payload = await self._post_json(
            "Cleansing/International/Batch/v1.20/json6.ws", request
        )
        results = self._parse_verify_results(payload)
        return results[0].matches[0] if results and results[0].matches else None

    async def verify_batch_async(
        self, addresses: Iterable[LoqateVerifyAddress]
    ) -> list[LoqateVerifyResult]:
        address_list = list(addresses)
        if not all(isinstance(item, LoqateVerifyAddress) for item in address_list):
            raise TypeError("addresses must contain only LoqateVerifyAddress objects")

        request = {
            "Key": self._api_key,
            "Geocode": None,
            "Options": None,
            "Addresses": [item.to_api() for item in address_list],
        }
        payload = await self._post_json(
            "Cleansing/International/Batch/v1.00/json4.ws", request
        )
        return self._parse_verify_results(payload)

    @staticmethod
    def _parse_verify_results(payload: Any) -> list[LoqateVerifyResult]:
        if not isinstance(payload, list):
            return []
        return [
            LoqateVerifyResult.from_api(item)
            for item in payload
            if isinstance(item, dict)
        ]

    async def _get_json(self, path: str, parameters: dict[str, str | None]) -> Any:
        query = urlencode(
            {
                key: value
                for key, value in parameters.items()
                if value is not None and value.strip()
            }
        )
        return await self._request_json("GET", f"{path}?{query}")

    async def _post_json(self, path: str, payload: dict[str, Any]) -> Any:
        body = json.dumps(payload).encode("utf-8")
        return await self._request_json("POST", path, body)

    async def _request_json(
        self, method: str, path: str, body: bytes | None = None
    ) -> Any:
        def send() -> Any:
            request = Request(
                f"{self._base_url}/{path.lstrip('/')}",
                data=body,
                method=method,
                headers={"Accept": "application/json", "Content-Type": "application/json"},
            )
            try:
                with urlopen(request, timeout=self._timeout) as response:
                    return json.loads(response.read().decode("utf-8"))
            except HTTPError as error:
                response_body = error.read().decode("utf-8", errors="replace")
                raise LoqateHttpError(
                    error.code, error.reason or "Unknown", response_body
                ) from error

        return await asyncio.to_thread(send)
