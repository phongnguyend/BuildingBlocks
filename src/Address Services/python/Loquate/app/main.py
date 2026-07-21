"""Entry point equivalent to dotnet/Loqate/Program.cs."""

import asyncio

from . import LoqateClient, LoqateVerifyAddress


async def main() -> None:
    client = LoqateClient()

    find_results = await client.find_async(
        "85 Gresham Street, London, EC2V 7NQ", limit=5
    )
    for result in find_results:
        print(result.id)
        address = await client.retrieve_async(result.id)
        print(address.line1 if address else None)

    verified = await client.verify_async(
        LoqateVerifyAddress(
            address="85 Gresham Street, London, EC2V 7NQ", country="GB"
        )
    )
    print(verified.address if verified else None)


if __name__ == "__main__":
    asyncio.run(main())
