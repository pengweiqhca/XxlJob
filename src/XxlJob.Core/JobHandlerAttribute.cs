namespace XxlJob.Core;

[AttributeUsage(AttributeTargets.Class)]
public class JobHandlerAttribute : Attribute
{
    public JobHandlerAttribute(string name) => Name = name;

    public string Name { get; }
}
