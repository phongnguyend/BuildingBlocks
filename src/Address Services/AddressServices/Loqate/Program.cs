using Loqate;

var client = new LoqateClient(new HttpClient() { BaseAddress = new Uri("https://api.addressy.com") }, "<YOUR_API_KEY_HERE>");

var findResults = await client.FindAsync("85 Gresham Street, London, EC2V 7NQ", limit: 5);

foreach (var result in findResults)
{
    Console.WriteLine(result.Id);

    var address = await client.RetrieveAsync(result.Id);
    Console.WriteLine(address?.Line1);
}

var verify = await client.VerifyAsync(new LoqateVerifyAddress {
    Address = "85 Gresham Street, London, EC2V 7NQ",
    Country = "GB"
});
Console.WriteLine(verify?.Address);

