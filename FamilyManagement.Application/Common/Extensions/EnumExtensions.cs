using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace FamilyManagement.Application.Common.Extensions;

public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> _displayNameCache = new();

    public static string GetDisplayName(this Enum enumValue)
    {
        return _displayNameCache.GetOrAdd(enumValue, (val) =>
        {
            return val.GetType()
                .GetMember(val.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<DisplayAttribute>()?
                .Name ?? val.ToString();
        });
    }

    public static IEnumerable<string> GetAllDisplayNames<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(e => e.GetDisplayName());
    }

    public static Dictionary<T, string> GetValuesWithDisplayNames<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(e => e, e => e.GetDisplayName());
    }
}

