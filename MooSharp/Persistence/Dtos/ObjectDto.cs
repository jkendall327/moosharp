namespace MooSharp;

public class ObjectDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? RoomSlug { get; set; }
    public string? TextContent { get; set; }
}