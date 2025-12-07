using MooSharp.Commands.Machinery;

namespace MooSharp.Tests;

public class CommandHandlerTests
{
    [Fact]
    public void EveryCommand_MustHave_ExactlyOneHandler()
    {
        // 1. Get the assembly where the game logic lives
        var assembly = typeof(ICommand).Assembly;

        // 2. Find all concrete classes that implement ICommand
        // We exclude interfaces and abstract classes (like CommandBase<T>)
        var commandTypes = assembly
            .GetTypes()
            .Where(t => t is {IsClass: true, IsAbstract: false} && typeof(ICommand).IsAssignableFrom(t))
            .ToList();

        // 3. Prepare a list to collect errors so we can report ALL failures at once
        var errors = new List<string>();

        foreach (var commandType in commandTypes)
        {
            // 4. Construct the specific interface we are looking for: e.g., IHandler<MoveCommand>
            var expectedHandlerInterface = typeof(IHandler<>).MakeGenericType(commandType);

            // 5. Find classes in the assembly that implement this specific interface
            var handlers = assembly
                .GetTypes()
                .Where(t => t is {IsClass: true, IsAbstract: false} && expectedHandlerInterface.IsAssignableFrom(t))
                .ToList();

            // 6. Assertions
            if (handlers.Count == 0)
            {
                errors.Add(
                    $"[MISSING] Command '{commandType.Name}' has no implementation of IHandler<{commandType.Name}>.");
            }
            else if (handlers.Count > 1)
            {
                var handlerNames = string.Join(", ", handlers.Select(h => h.Name));
                errors.Add($"[DUPLICATE] Command '{commandType.Name}' has multiple handlers: {handlerNames}.");
            }
        }

        // 7. Fail the test if there were any errors
        Assert.True(errors.Count == 0,
            $"Architecture violation found. The following commands are missing handlers or have too many:\n\n{string.Join("\n", errors)}");
    }
}