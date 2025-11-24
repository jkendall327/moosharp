namespace MooSharp;

public static class ContainerExtensions
{
    public static Object? FindObjectInContainer(this IContainer container, string keyword)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(keyword);

        return container.Contents.FirstOrDefault(o =>
            o.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || o.Keywords.Contains(keyword));
    }
}
