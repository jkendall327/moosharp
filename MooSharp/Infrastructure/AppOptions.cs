using System.ComponentModel.DataAnnotations;

namespace MooSharp.Infrastructure;

public class AppOptions
{
    [Required]
    public required string WorldDataFilepath { get; init; }

    [Required]
    public required string DatabaseFilepath { get; init; }
}