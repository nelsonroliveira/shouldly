﻿using System.ComponentModel;

namespace Shouldly;

[ShouldlyMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class ObjectGraphTestExtensions
{
    private const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.Instance;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldBeEquivalentTo(
        [NotNullIfNotNull(nameof(expected))] this object? actual,
        [NotNullIfNotNull(nameof(actual))] object? expected,
        EquivalencyOptions options,
        string? customMessage = null)
    {
        CompareObjects(actual, expected, null, new List<string>(), new Dictionary<object, IList<object?>>(), options, customMessage);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldBeEquivalentTo(
        [NotNullIfNotNull(nameof(expected))] this object? actual,
        [NotNullIfNotNull(nameof(actual))] object? expected,
        string? customMessage = null)
    {
        CompareObjects(actual, expected, null, new List<string>(), new Dictionary<object, IList<object?>>(), new (), customMessage);
    }

    private static void CompareObjects([NotNullIfNotNull(nameof(expected))] this object? actual,
        [NotNullIfNotNull(nameof(actual))] object? expected,
        Type? forcedType,
        IList<string> path,
        IDictionary<object, IList<object?>> previousComparisons,
        EquivalencyOptions options,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        if (BothValuesAreNull(actual, expected, path, customMessage, shouldlyMethod))
            return;

        var type = forcedType ?? GetTypeToCompare(actual, expected, path, customMessage, shouldlyMethod);

        AddTypeToPath(type, path);

        if (type == typeof(string))
        {
            CompareStrings((string)actual, (string)expected, path, customMessage, shouldlyMethod);
        }
        else if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            CompareEnumerables((IEnumerable)actual, (IEnumerable)expected, path, previousComparisons, options, customMessage, shouldlyMethod);
        }
        else if (type.IsValueType)
        {
            CompareValueTypes((ValueType)actual, (ValueType)expected, path, customMessage, shouldlyMethod);
        }
        else
        {
            CompareReferenceTypes(actual, expected, type, path, previousComparisons, options, customMessage, shouldlyMethod);
        }
    }

    private static bool BothValuesAreNull(
        [NotNullWhen(false)] object? actual,
        [NotNullWhen(false)] object? expected,
        IEnumerable<string> path,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        if (expected == null)
        {
            if (actual == null)
                return true;

            ThrowException(actual, expected, path, customMessage, shouldlyMethod);
        }
        else if (actual == null)
        {
            ThrowException(actual, expected, path, customMessage, shouldlyMethod);
        }

        return false;
    }

    private static Type GetTypeToCompare(object actual, object expected, IList<string> path,
        string? customMessage, [CallerMemberName] string shouldlyMethod = null!)
    {
        var expectedType = expected.GetType();
        var actualType = actual.GetType();

        if (actualType != expectedType)
            ThrowException(actualType, expectedType, path, customMessage, shouldlyMethod);

        return actualType;
    }

    private static void AddTypeToPath(Type actualType, IList<string> path)
    {
        var typeName = $" [{actualType.FullName}]";
        if (path.Count == 0)
            path.Add(typeName);
        else
            path[path.Count - 1] += typeName;
    }

    private static void CompareValueTypes(ValueType actual, ValueType expected, IEnumerable<string> path,
        string? customMessage, [CallerMemberName] string shouldlyMethod = null!)
    {
        if (!actual.Equals(expected))
            ThrowException(actual, expected, path, customMessage, shouldlyMethod);
    }

    private static void CompareReferenceTypes(
        object actual,
        object expected,
        Type type,
        IList<string> path,
        IDictionary<object, IList<object?>> previousComparisons,
        EquivalencyOptions options,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        if (ReferenceEquals(actual, expected) ||
            previousComparisons.Contains(actual, expected))
            return;

        previousComparisons.Record(actual, expected);

        if (type == typeof(string))
        {
            CompareStrings((string)actual, (string)expected, path, customMessage, shouldlyMethod);
        }
        else if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            CompareEnumerables((IEnumerable)actual, (IEnumerable)expected, path, previousComparisons, options, customMessage, shouldlyMethod);
        }
        else
        {
            var fields = type.GetFields(DefaultBindingFlags);
            CompareFields(actual, expected, fields, path, previousComparisons, options, customMessage, shouldlyMethod);

            var properties = type.GetProperties(DefaultBindingFlags);
            CompareProperties(actual, expected, properties, path, previousComparisons, options, customMessage, shouldlyMethod);
        }
    }

    private static void CompareStrings(string actual, string expected, IEnumerable<string> path,
        string? customMessage, [CallerMemberName] string shouldlyMethod = null!)
    {
        if (!actual.Equals(expected, StringComparison.Ordinal))
            ThrowException(actual, expected, path, customMessage, shouldlyMethod);
    }

    private static void CompareEnumerables(
        IEnumerable actual,
        IEnumerable expected,
        IEnumerable<string> path,
        IDictionary<object, IList<object?>> previousComparisons,
        EquivalencyOptions options,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        var expectedList = expected.Cast<object?>().ToList();
        var actualList = actual.Cast<object?>().ToList();

        if (actualList.Count != expectedList.Count)
        {
            var newPath = path.Concat(["Count"]);
            ThrowException(actualList.Count, expectedList.Count, newPath, customMessage, shouldlyMethod);
        }

        for (var i = 0; i < actualList.Count; i++)
        {
            var newPath = path.Concat([$"Element [{i}]"]);
            CompareObjects(actualList[i], expectedList[i], null, newPath.ToList(), previousComparisons, options, customMessage, shouldlyMethod);
        }
    }

    private static void CompareFields(
        object actual,
        object expected,
        IEnumerable<FieldInfo> fields,
        IList<string> path,
        IDictionary<object, IList<object?>> previousComparisons,
        EquivalencyOptions options,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        foreach (var field in fields)
        {
            var actualValue = field.GetValue(actual);
            var expectedValue = field.GetValue(expected);

            var newPath = path.Concat([field.Name]);
            var forcedType = options.CompareUsingRuntimeTypes ? null : field.FieldType;

            CompareObjects(actualValue, expectedValue, forcedType, newPath.ToList(), previousComparisons,options, customMessage, shouldlyMethod);
        }
    }

    private static void CompareProperties(
        object actual,
        object expected,
        IEnumerable<PropertyInfo> properties,
        IList<string> path,
        IDictionary<object, IList<object?>> previousComparisons,
        EquivalencyOptions options,
        string? customMessage,
        [CallerMemberName] string shouldlyMethod = null!)
    {
        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
            {
                // There's no sensible way to compare indexers, as there does not exist a way to obtain a collection
                // of all values in a way that's common to all indexer implementations.
                throw new NotSupportedException("Comparing types that have indexers is not supported.");
            }

            var actualValue = property.GetValue(actual, []);
            var expectedValue = property.GetValue(expected, []);

            var newPath = path.Concat([property.Name]);
            var forcedType = options.CompareUsingRuntimeTypes ? null : property.PropertyType;

            CompareObjects(actualValue, expectedValue, forcedType, newPath.ToList(), previousComparisons, options, customMessage, shouldlyMethod);
        }
    }

    [DoesNotReturn]
    private static void ThrowException(object? actual, object? expected, IEnumerable<string> path,
        string? customMessage, [CallerMemberName] string shouldlyMethod = null!)
    {
        throw new ShouldAssertException(
            new ExpectedEquivalenceShouldlyMessage(expected, actual, path, customMessage, shouldlyMethod).ToString());
    }

    private static bool Contains(this IDictionary<object, IList<object?>> comparisons, object actual, object? expected) =>
        comparisons.TryGetValue(actual, out var list)
        && list.Contains(expected);

    private static void Record(this IDictionary<object, IList<object?>> comparisons, object actual, object? expected)
    {
        if (comparisons.TryGetValue(actual, out var list))
        {
            list.Add(expected);
        }
        else
        {
            comparisons.Add(actual, new List<object?>([expected]));
        }
    }
}