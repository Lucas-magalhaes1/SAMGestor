namespace SAMGestor.API.Extensions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SwaggerOrderAttribute : Attribute
    {
        public int Order { get; }
        public SwaggerOrderAttribute(int order) => Order = order;
    }
}