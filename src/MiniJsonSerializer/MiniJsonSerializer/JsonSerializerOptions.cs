namespace MiniJsonSerializer;

public class JsonSerializerOptions
{
    public bool WriteIndented { get; set; }

    public int MaxDepth { get; set; } = 64;
}
