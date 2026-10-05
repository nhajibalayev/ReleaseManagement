using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ReleaseManagement.Web.ViewModels;

/// <summary>Human-readable enum names for views: uses <see cref="DisplayAttribute"/> when present, else splits PascalCase.</summary>
public static class EnumDisplay
{
    public static string Name(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>()?.GetName();
        return string.IsNullOrWhiteSpace(display) ? SplitPascal(value.ToString()) : display;
    }

    public static string SplitPascal(string text)
    {
        var chars = new List<char>(text.Length + 8);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(text[i - 1]))
            {
                chars.Add(' ');
                chars.Add(char.ToLowerInvariant(c));
            }
            else
            {
                chars.Add(c);
            }
        }

        return new string(chars.ToArray());
    }
}
