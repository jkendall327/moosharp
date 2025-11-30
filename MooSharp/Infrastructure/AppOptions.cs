using System.ComponentModel.DataAnnotations;

namespace MooSharp.Infrastructure;

public class AppOptions
{
    [Required]
    public required string WorldDataFilepath { get; set; }

    [Required]
    public required string DatabaseFilepath { get; set; }

    public string? Motd { get; set; }
}