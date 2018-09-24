// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Reflection;

    public class Generic
    {
        public void UnconstrainedGetHashCode<T>()
        {
            _ = typeof(T).GetMethod(nameof(this.GetHashCode), BindingFlags.Public | BindingFlags.Instance);
        }

        public void ConstrainedToIConvertible<T>()
            where T : IConvertible
        {
            _ = typeof(T).GetMethod(nameof(IConvertible.ToString), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(IConvertible.ToBoolean), BindingFlags.Public | BindingFlags.Instance);
        }

        public MethodInfo ConstrainedToFoo<T>()
            where T : Generic
        {
            return typeof(T).GetMethod(nameof(this.Baz), BindingFlags.Public | BindingFlags.Instance);
        }

        public void ConstrainedToFooAndIConvertible<T>()
            where T : Generic, IComparable
        {
            _ = typeof(T).GetMethod(nameof(this.Baz), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(IComparable.ToString), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(IComparable.CompareTo), BindingFlags.Public | BindingFlags.Instance);
        }

        public void ConstrainedToFooAndIComparable<T>()
            where T : Generic, IConvertible
        {
            _ = typeof(T).GetMethod(nameof(this.Baz), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(IConvertible.ToString), BindingFlags.Public | BindingFlags.Instance);
            _ = typeof(T).GetMethod(nameof(IConvertible.ToBoolean), BindingFlags.Public | BindingFlags.Instance);
        }

        public int Baz() => 0;
    }
}
