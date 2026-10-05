using System.Globalization;
using System.Text.Json;
using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Web.ViewModels;

/// <summary>One line in the release History tab: who did what, when, and what changed.</summary>
public sealed class ReleaseHistoryEventViewModel
{
    public DateTime OccurredAtUtc { get; init; }

    public string Actor { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    /// <summary>Human-readable change lines, e.g. "Title: Old → New".</summary>
    public IReadOnlyList<string> Changes { get; init; } = [];

    public string? Comment { get; init; }

    /// <summary>CSS hint: status / edit / readiness / assignment / other.</summary>
    public string Kind { get; init; } = "other";
}

/// <summary>Turns <see cref="AuditLog"/> rows of a release into readable timeline events.</summary>
public static class ReleaseHistoryBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<ReleaseHistoryEventViewModel> Build(
        IEnumerable<AuditLog> entries,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        return entries
            .OrderByDescending(entry => entry.CreatedDate)
            .Select(entry => ToEvent(entry, userNames))
            .ToList();
    }

    private static ReleaseHistoryEventViewModel ToEvent(AuditLog entry, IReadOnlyDictionary<Guid, string> userNames)
    {
        var oldValues = Parse(entry.OldValuesJson);
        var newValues = Parse(entry.NewValuesJson);
        var actor = entry.UserId is { } userId
            ? userNames.GetValueOrDefault(userId, "Unknown user")
            : "System";

        var (action, kind) = Describe(entry.Action, oldValues, newValues, userNames);
        var changes = new List<string>();
        string? comment = null;

        foreach (var (key, newValue) in newValues)
        {
            if (key.Equals("Comment", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Reason", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Justification", StringComparison.OrdinalIgnoreCase))
            {
                var text = Render(key, newValue, userNames);
                if (!string.IsNullOrWhiteSpace(text) && text != "—")
                {
                    comment = text;
                }

                continue;
            }

            var newText = Render(key, newValue, userNames);
            if (oldValues.TryGetValue(key, out var oldValue))
            {
                var oldText = Render(key, oldValue, userNames);
                if (oldText != newText)
                {
                    changes.Add($"{Label(key)}: {oldText} → {newText}");
                }
            }
            else if (newText != "—")
            {
                changes.Add($"{Label(key)}: {newText}");
            }
        }

        // Keys present only in old values (removed).
        foreach (var (key, oldValue) in oldValues)
        {
            if (!newValues.ContainsKey(key))
            {
                changes.Add($"{Label(key)}: {Render(key, oldValue, userNames)} → —");
            }
        }

        return new ReleaseHistoryEventViewModel
        {
            OccurredAtUtc = entry.CreatedDate,
            Actor = actor,
            Action = action,
            Changes = changes,
            Comment = comment,
            Kind = kind
        };
    }

    private static (string Action, string Kind) Describe(
        string action,
        Dictionary<string, JsonElement> oldValues,
        Dictionary<string, JsonElement> newValues,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        switch (action)
        {
            case "Release.CreateDraft":
                return ("Created the release record", "edit");
            case "Release.UpdateDraft":
                return (newValues.Count == 0 ? "Saved the draft (no field changes)" : "Edited the release record", "edit");
            case "Release.Transition":
            {
                var from = newValues.ContainsKey("Status") && oldValues.TryGetValue("Status", out var o) ? Render("Status", o, userNames) : null;
                var to = newValues.TryGetValue("Status", out var n) ? Render("Status", n, userNames) : null;
                var text = from is null || to is null ? "Changed status" : $"Status: {from} → {to}";
                if (newValues.TryGetValue("CurrentResponsibleUserId", out var responsible) && responsible.ValueKind == JsonValueKind.String)
                {
                    text += $" · assigned to {Render("CurrentResponsibleUserId", responsible, userNames)}";
                }
                else if (newValues.TryGetValue("CurrentResponsibleRole", out var role) && role.ValueKind == JsonValueKind.String)
                {
                    text += $" · now with {role.GetString()}";
                }

                return (text, "status");
            }

            case "Release.AssignOwners":
                return ("Assigned owners", "assignment");
            case "Release.ReadinessControl":
            {
                var control = newValues.TryGetValue("ControlType", out var c) ? Render("ControlType", c, userNames) : "control";
                return ($"Readiness control updated: {control}", "readiness");
            }

            case "Release.AddReference":
                return ("Added a reference link", "edit");
            case "Release.RemoveReference":
                return ("Removed a reference link", "edit");
            case "Release.Communication":
                return ("Logged a communication", "other");
            case "Release.Reschedule":
                return ("Rescheduled the release window", "edit");
            case "Release.TechnicalValidation":
                return ("Recorded technical validation", "readiness");
            case "Release.BusinessValidation":
                return ("Recorded business validation", "readiness");
            case "Release.Outcome":
                return ("Recorded the release outcome", "status");
            default:
                return (action.Replace("Release.", string.Empty, StringComparison.Ordinal), "other");
        }
    }

    private static Dictionary<string, JsonElement> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
            return parsed is null
                ? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, JsonElement>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string Render(string key, JsonElement value, IReadOnlyDictionary<Guid, string> userNames)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return "—";
            case JsonValueKind.True:
                return "yes";
            case JsonValueKind.False:
                return "no";
            case JsonValueKind.Number:
                if (value.TryGetInt32(out var number))
                {
                    return RenderEnum(key, number) ?? number.ToString(CultureInfo.InvariantCulture);
                }

                return value.ToString();
            case JsonValueKind.String:
            {
                var text = value.GetString() ?? string.Empty;
                if (key.EndsWith("UserId", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(text, out var userId))
                {
                    return userNames.GetValueOrDefault(userId, text);
                }

                if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(text, out _))
                {
                    return "(changed)";
                }

                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) &&
                    text.Length >= 19 && text[4] == '-')
                {
                    return date.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
                }

                return string.IsNullOrWhiteSpace(text) ? "—" : text;
            }

            default:
                return value.ToString();
        }
    }

    /// <summary>Audit JSON stores enums as numbers; map the well-known ones back to names.</summary>
    private static string? RenderEnum(string key, int number)
    {
        if (key.Equals("Status", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("CurrentStatus", StringComparison.OrdinalIgnoreCase))
        {
            return Enum.IsDefined(typeof(ReleaseStatus), number)
                ? ReleaseStatusDisplay.Format((ReleaseStatus)number)
                : Enum.IsDefined(typeof(ReadinessControlStatus), number) ? ((ReadinessControlStatus)number).ToString() : null;
        }

        return key switch
        {
            _ when key.Equals("ControlType", StringComparison.OrdinalIgnoreCase) => EnumName<ReadinessControlType>(number),
            _ when key.Equals("Category", StringComparison.OrdinalIgnoreCase) => EnumName<ReleaseCategory>(number),
            _ when key.Equals("ExecutionMode", StringComparison.OrdinalIgnoreCase) => EnumName<ExecutionMode>(number),
            _ when key.Equals("ReferenceType", StringComparison.OrdinalIgnoreCase) => EnumName<ReleaseReferenceType>(number),
            _ when key.Equals("Result", StringComparison.OrdinalIgnoreCase) => EnumName<ValidationResult>(number),
            _ when key.Equals("Outcome", StringComparison.OrdinalIgnoreCase) => EnumName<ReleaseOutcome>(number),
            _ when key.Equals("Type", StringComparison.OrdinalIgnoreCase) => EnumName<CommunicationType>(number),
            _ => null
        };
    }

    private static string? EnumName<TEnum>(int number) where TEnum : struct, Enum =>
        Enum.IsDefined(typeof(TEnum), number) ? Enum.GetName(typeof(TEnum), number) : null;

    private static string Label(string key)
    {
        if (key.EndsWith("UserId", StringComparison.OrdinalIgnoreCase))
        {
            key = key[..^6];
        }

        // "PlannedWindowStart" -> "Planned window start"
        var chars = new List<char>(key.Length + 8);
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(key[i - 1]))
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
