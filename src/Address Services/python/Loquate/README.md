# Loqate Python client

This is the Python equivalent of the client in `dotnet/Loqate`. It supports
interactive address search, address retrieval, and single or batch address
verification. It requires Python 3.10 or newer.

Install the dependency from the `python/Loquate` directory:

```powershell
python -m pip install -r requirements.txt
```

Create `python/Loquate/.env`:

```dotenv
LOQATE_API_KEY=your-api-key
```

Run the application from the `python/Loquate` directory:

```powershell
python -m app.main
```

Run the tests from the same directory:

```powershell
python -m unittest discover -s tests -v
```

`LoqateClient()` resolves the key in this order: an explicit `api_key` argument,
the `LOQATE_API_KEY` process environment variable, then `.env`. A different file
can be selected with `LoqateClient(env_file="path/to/.env")`. The `.env` file is
loaded with `python-dotenv` without overriding an existing environment variable.

Batch verification is available with:

```python
from app import LoqateVerifyAddress

results = await client.verify_batch_async(
    [
        LoqateVerifyAddress(address="85 Gresham Street, London, EC2V 7NQ", country="GB"),
        LoqateVerifyAddress(address="10 Downing Street, London", country="GB"),
    ]
)
```
