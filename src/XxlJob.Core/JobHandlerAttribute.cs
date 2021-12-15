namespace XxlJob.Core;

[AttributeUsage(AttributeTargets.Class)]
public class JobHandlerAttribute : Attribute
{
    public JobHandlerAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        Name = name;
    }

    public string Name { get; }
}
