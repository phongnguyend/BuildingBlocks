namespace ContactAPIs;

// A unified contact model for your application
public class ContactResult
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Title { get; set; }
    public string Email { get; set; }
    public string CompanyName { get; set; }
    public string SourceProvider { get; set; }
}

// Search filters parameter object
public class ContactSearchQuery
{
    public string Keywords { get; set; }
    public List<string> Titles { get; set; } = new List<string>();
    public List<string> Locations { get; set; } = new List<string>();
}

// The interface pattern to swap providers easily
public interface IContactProvider
{
    Task<List<ContactResult>> SearchContactsAsync(ContactSearchQuery query);
}