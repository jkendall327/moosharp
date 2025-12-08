namespace MooSharp.Features.Autocomplete;

public record AutocompleteOptions(IReadOnlyCollection<string> Exits, IReadOnlyCollection<string> InventoryItems, IReadOnlyCollection<string> ObjectsInRoom);
