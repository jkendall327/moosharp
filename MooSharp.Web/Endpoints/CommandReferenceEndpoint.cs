using MooSharp.Commands.Machinery;

namespace MooSharp.Web.Endpoints;

public static class CommandReferenceEndpoint
{
    public static void MapCommandReferenceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/commands",
                (CommandReference commandReference) =>
                {
                    var categories = commandReference
                        .GetCommandMetadata()
                        .Select(category => new CommandHelpCategory(
                            category.Category.ToString(),
                            category.Commands
                                .Select(command => new CommandHelpEntry(
                                    command.PrimaryVerb,
                                    command.Synonyms,
                                    command.Description))
                                .ToList()))
                        .ToList();

                    return Results.Ok(categories);
                })
            .WithName("GetCommands");
    }

    private sealed record CommandHelpCategory(string Category, IReadOnlyCollection<CommandHelpEntry> Commands);

    private sealed record CommandHelpEntry(string PrimaryVerb, IReadOnlyCollection<string> Synonyms, string Description);
}
