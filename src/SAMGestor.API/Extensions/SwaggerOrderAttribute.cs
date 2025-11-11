namespace SAMGestor.API.Extensions;

/// <summary>
/// Define a ordem de exibição no Swagger. Use em Controller ou Action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SwaggerOrderAttribute : Attribute
{
    public int Order { get; }
    public SwaggerOrderAttribute(int order) => Order = order;
}