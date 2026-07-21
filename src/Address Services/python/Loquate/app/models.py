"""Data models used by the Loqate address client."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


def _text(data: dict[str, Any], name: str) -> str:
    value = data.get(name)
    return "" if value is None else str(value)


@dataclass(slots=True)
class LoqateFindItem:
    id: str = ""
    type: str = ""
    text: str = ""
    description: str = ""
    highlight: str = ""

    @classmethod
    def from_api(cls, data: dict[str, Any]) -> "LoqateFindItem":
        return cls(
            id=_text(data, "Id"),
            type=_text(data, "Type"),
            text=_text(data, "Text"),
            description=_text(data, "Description"),
            highlight=_text(data, "Highlight"),
        )


@dataclass(slots=True)
class LoqateAddress:
    id: str = ""
    company: str = ""
    building_number: str = ""
    building_name: str = ""
    street: str = ""
    city: str = ""
    province: str = ""
    postal_code: str = ""
    country_name: str = ""
    line1: str = ""
    line2: str = ""
    line3: str = ""
    line4: str = ""
    line5: str = ""

    @classmethod
    def from_api(cls, data: dict[str, Any]) -> "LoqateAddress":
        return cls(
            id=_text(data, "Id"),
            company=_text(data, "Company"),
            building_number=_text(data, "BuildingNumber"),
            building_name=_text(data, "BuildingName"),
            street=_text(data, "Street"),
            city=_text(data, "City"),
            province=_text(data, "Province"),
            postal_code=_text(data, "PostalCode"),
            country_name=_text(data, "CountryName"),
            line1=_text(data, "Line1"),
            line2=_text(data, "Line2"),
            line3=_text(data, "Line3"),
            line4=_text(data, "Line4"),
            line5=_text(data, "Line5"),
        )


@dataclass(slots=True)
class LoqateVerifyOptions:
    process: str | None = "Verify"
    certify: bool | None = None
    enhance: bool | None = None
    version: bool | None = None
    server_options: dict[str, Any] | None = None

    def to_api(self) -> dict[str, Any]:
        return {
            "Process": self.process,
            "Certify": self.certify,
            "Enhance": self.enhance,
            "Version": self.version,
            "ServerOptions": self.server_options,
        }


@dataclass(slots=True)
class LoqateVerifyAddress:
    address: str | None = None
    address1: str | None = None
    address2: str | None = None
    address3: str | None = None
    address4: str | None = None
    address5: str | None = None
    address6: str | None = None
    address7: str | None = None
    address8: str | None = None
    locality: str | None = None
    administrative_area: str | None = None
    postal_code: str | None = None
    country: str | None = None

    def to_api(self) -> dict[str, Any]:
        return {
            "Address": self.address,
            "Address1": self.address1,
            "Address2": self.address2,
            "Address3": self.address3,
            "Address4": self.address4,
            "Address5": self.address5,
            "Address6": self.address6,
            "Address7": self.address7,
            "Address8": self.address8,
            "Locality": self.locality,
            "AdministrativeArea": self.administrative_area,
            "PostalCode": self.postal_code,
            "Country": self.country,
        }

    @classmethod
    def from_api(cls, data: dict[str, Any]) -> "LoqateVerifyAddress":
        return cls(
            address=data.get("Address"),
            address1=data.get("Address1"),
            address2=data.get("Address2"),
            address3=data.get("Address3"),
            address4=data.get("Address4"),
            address5=data.get("Address5"),
            address6=data.get("Address6"),
            address7=data.get("Address7"),
            address8=data.get("Address8"),
            locality=data.get("Locality"),
            administrative_area=data.get("AdministrativeArea"),
            postal_code=data.get("PostalCode"),
            country=data.get("Country"),
        )


@dataclass(slots=True)
class LoqateVerifiedAddress(LoqateVerifyAddress):
    avc: str | None = None
    aqi: str | None = None
    country_name: str | None = None
    latitude: str | None = None
    longitude: str | None = None

    @classmethod
    def from_api(cls, data: dict[str, Any]) -> "LoqateVerifiedAddress":
        basic = LoqateVerifyAddress.from_api(data)
        return cls(
            **{name: getattr(basic, name) for name in basic.__dataclass_fields__},
            avc=data.get("AVC"),
            aqi=data.get("AQI"),
            country_name=data.get("CountryName"),
            latitude=data.get("Latitude"),
            longitude=data.get("Longitude"),
        )


@dataclass(slots=True)
class LoqateVerifyResult:
    input: LoqateVerifyAddress | None = None
    matches: list[LoqateVerifiedAddress] = field(default_factory=list)

    @classmethod
    def from_api(cls, data: dict[str, Any]) -> "LoqateVerifyResult":
        input_data = data.get("Input")
        matches = data.get("Matches") or []
        return cls(
            input=(
                LoqateVerifyAddress.from_api(input_data)
                if isinstance(input_data, dict)
                else None
            ),
            matches=[
                LoqateVerifiedAddress.from_api(item)
                for item in matches
                if isinstance(item, dict)
            ],
        )
