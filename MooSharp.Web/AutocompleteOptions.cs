using System.Collections.Generic;

namespace MooSharp;

public record AutocompleteOptions(IReadOnlyCollection<string> Exits, IReadOnlyCollection<string> InventoryItems);
