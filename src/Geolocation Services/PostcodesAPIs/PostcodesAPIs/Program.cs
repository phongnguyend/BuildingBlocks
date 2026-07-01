using PostcodesAPIs;

using var httpClient = new HttpClient();
var client = new PostcodeClient(httpClient);

string target = "NW1 9HZ";
int radiusMeters = 1000; // 1 km radius
int maxResults = 5;

Console.WriteLine($"Searching postcodes within {radiusMeters}m of {target}...");
var results = await client.FindNearestPostcodesAsync(target, radiusMeters, maxResults);

foreach (var p in results)
{
    Console.WriteLine($"- Postcode: {p.Postcode} | Distance: {Math.Round(p.DistanceInMetres)} meters away");
}