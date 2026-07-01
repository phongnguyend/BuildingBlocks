using ContactAPIs;

using var httpClient = new HttpClient();

// Swap implementation with LushaContactProvider or CognismContactProvider seamlessly
IContactProvider contactService = new ApolloContactProvider(httpClient, "YOUR_APOLLO_API_KEY");

var searchQuery = new ContactSearchQuery
{
    Keywords = "Software Engineer",
    Titles = ["CTO", "Vp of Engineering"],
    Locations = ["San Francisco", "New York"]
};

Console.WriteLine("Searching for contacts...");
var contacts = await contactService.SearchContactsAsync(searchQuery);

foreach (var contact in contacts)
{
    Console.WriteLine($"[{contact.SourceProvider}] {contact.FirstName} {contact.LastName} - {contact.Title} at {contact.CompanyName}");
}
