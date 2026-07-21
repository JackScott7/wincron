using System.Collections.Frozen;

namespace WinCron.Domain;

public sealed class CronField
{
    private static readonly FrozenDictionary<string, int> MonthNames =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["JAN"] = 1,
            ["FEB"] = 2,
            ["MAR"] = 3,
            ["APR"] = 4,
            ["MAY"] = 5,
            ["JUN"] = 6,
            ["JUL"] = 7,
            ["AUG"] = 8,
            ["SEP"] = 9,
            ["OCT"] = 10,
            ["NOV"] = 11,
            ["DEC"] = 12
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, int> DayNames =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["SUN"] = 0,
            ["MON"] = 1,
            ["TUE"] = 2,
            ["WED"] = 3,
            ["THU"] = 4,
            ["FRI"] = 5,
            ["SAT"] = 6
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly FrozenSet<int> allowedValues;

    private CronField(CronFieldKind kind, string source, bool isWildcard, IEnumerable<int> allowedValues)
    {
        Kind = kind;
        Source = source;
        IsWildcard = isWildcard;
        this.allowedValues = allowedValues.ToFrozenSet();
    }

    public CronFieldKind Kind { get; }

    public string Source { get; }

    public bool IsWildcard { get; }

    public IReadOnlySet<int> AllowedValues => allowedValues;

    public bool Matches(int value) => allowedValues.Contains(NormalizeValue(Kind, value));

    public static bool TryParse(
        CronFieldKind kind,
        string text,
        out CronField? field,
        out string? error)
    {
        field = null;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"The {GetDisplayName(kind)} field cannot be empty.";
            return false;
        }

        var values = new HashSet<int>();
        var segments = text.Split(',', StringSplitOptions.None);

        if (segments.Any(string.IsNullOrWhiteSpace))
        {
            error = $"The {GetDisplayName(kind)} field contains an empty list item.";
            return false;
        }

        foreach (var segment in segments)
        {
            if (!TryExpandSegment(kind, segment, values, out error))
            {
                return false;
            }
        }

        field = new CronField(kind, text, text == "*", values);
        return true;
    }

    private static bool TryExpandSegment(
        CronFieldKind kind,
        string segment,
        HashSet<int> values,
        out string? error)
    {
        error = null;
        var stepParts = segment.Split('/', StringSplitOptions.None);

        if (stepParts.Length > 2 || stepParts.Any(string.IsNullOrWhiteSpace))
        {
            error = $"Invalid step expression '{segment}' in the {GetDisplayName(kind)} field.";
            return false;
        }

        var step = 1;
        if (stepParts.Length == 2 && (!int.TryParse(stepParts[1], out step) || step <= 0))
        {
            error = $"Step '{stepParts[1]}' in the {GetDisplayName(kind)} field must be a positive integer.";
            return false;
        }

        var rangeText = stepParts[0];
        var (minimum, maximum) = GetBounds(kind);
        int rangeStart;
        int rangeEnd;

        if (rangeText == "*")
        {
            rangeStart = minimum;
            rangeEnd = maximum;
        }
        else if (rangeText.Contains('-', StringComparison.Ordinal))
        {
            var rangeParts = rangeText.Split('-', StringSplitOptions.None);
            if (rangeParts.Length != 2
                || !TryParseValue(kind, rangeParts[0], out rangeStart)
                || !TryParseValue(kind, rangeParts[1], out rangeEnd))
            {
                error = $"Invalid range '{rangeText}' in the {GetDisplayName(kind)} field.";
                return false;
            }

            if (rangeStart > rangeEnd)
            {
                error = $"Range '{rangeText}' in the {GetDisplayName(kind)} field must be ascending.";
                return false;
            }
        }
        else
        {
            if (stepParts.Length == 2)
            {
                error = $"A step in the {GetDisplayName(kind)} field must follow '*' or a range.";
                return false;
            }

            if (!TryParseValue(kind, rangeText, out rangeStart))
            {
                error = $"Invalid value '{rangeText}' in the {GetDisplayName(kind)} field.";
                return false;
            }

            rangeEnd = rangeStart;
        }

        if (rangeStart < minimum || rangeEnd > maximum)
        {
            error = $"Value '{rangeText}' is outside the allowed {minimum}-{maximum} range for {GetDisplayName(kind)}.";
            return false;
        }

        for (var value = rangeStart; value <= rangeEnd; value += step)
        {
            values.Add(NormalizeValue(kind, value));
        }

        return true;
    }

    private static bool TryParseValue(CronFieldKind kind, string text, out int value)
    {
        if (int.TryParse(text, out value))
        {
            return true;
        }

        var names = kind switch
        {
            CronFieldKind.Month => MonthNames,
            CronFieldKind.DayOfWeek => DayNames,
            _ => null
        };

        return names is not null && names.TryGetValue(text, out value);
    }

    private static (int Minimum, int Maximum) GetBounds(CronFieldKind kind) => kind switch
    {
        CronFieldKind.Minute => (0, 59),
        CronFieldKind.Hour => (0, 23),
        CronFieldKind.DayOfMonth => (1, 31),
        CronFieldKind.Month => (1, 12),
        CronFieldKind.DayOfWeek => (0, 7),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported cron field kind.")
    };

    private static int NormalizeValue(CronFieldKind kind, int value) =>
        kind == CronFieldKind.DayOfWeek && value == 7 ? 0 : value;

    private static string GetDisplayName(CronFieldKind kind) => kind switch
    {
        CronFieldKind.Minute => "minute",
        CronFieldKind.Hour => "hour",
        CronFieldKind.DayOfMonth => "day-of-month",
        CronFieldKind.Month => "month",
        CronFieldKind.DayOfWeek => "day-of-week",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported cron field kind.")
    };
}
