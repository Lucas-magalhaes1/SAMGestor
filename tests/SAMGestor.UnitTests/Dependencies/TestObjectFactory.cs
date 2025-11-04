using System.Reflection;
using System.Runtime.CompilerServices;

namespace SAMGestor.UnitTests.Dependencies;

internal static class TestObjectFactory
{
    public static T Uninitialized<T>() where T : class
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    public static object Uninitialized(Type t)
        => RuntimeHelpers.GetUninitializedObject(t);

    public static T SetProperty<T>(this T obj, string name, object? value)
    {
        var p = obj!.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var set = p?.GetSetMethod(true);
        if (set is null)
            throw new MissingMemberException(obj.GetType().FullName, name);
        set.Invoke(obj, new[] { value });
        return obj;
    }

    public static T SetField<T>(this T obj, string name, object? value)
    {
        var f = obj!.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f is null)
            throw new MissingMemberException(obj.GetType().FullName, name);
        f.SetValue(obj, value);
        return obj;
    }
}