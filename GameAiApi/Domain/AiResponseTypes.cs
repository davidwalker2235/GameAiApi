namespace GameAiApi.Domain;

public enum AiResponseTheme
{
    Technical,
    Financial,
    HR,
    Conflict,
    Crisis,
    Strategy,
    Personal,
    AbsurdUnexpected
}

public enum AiResponseType
{
    People,
    Event
}

public enum AiResponseRole
{
    DeliveryPerson,
    Reception,
    Intern,
    Maintenance,
    JuniorEmployee,
    SeniorEmployee,
    AreaManager,
    HumanResources
}

public static class AiResponseTypeCatalog
{
    public static IReadOnlyList<string> Types { get; } = [
        AiResponseType.People.ToApiValue(),
        AiResponseType.Event.ToApiValue()
    ];

    public static IReadOnlyList<string> Themes { get; } = [
        AiResponseTheme.Technical.ToApiValue(),
        AiResponseTheme.Financial.ToApiValue(),
        AiResponseTheme.HR.ToApiValue(),
        AiResponseTheme.Conflict.ToApiValue(),
        AiResponseTheme.Crisis.ToApiValue(),
        AiResponseTheme.Strategy.ToApiValue(),
        AiResponseTheme.Personal.ToApiValue(),
        AiResponseTheme.AbsurdUnexpected.ToApiValue()
    ];

    public static IReadOnlyList<string> Roles { get; } = [
        AiResponseRole.DeliveryPerson.ToApiValue(),
        AiResponseRole.Reception.ToApiValue(),
        AiResponseRole.Intern.ToApiValue(),
        AiResponseRole.Maintenance.ToApiValue(),
        AiResponseRole.JuniorEmployee.ToApiValue(),
        AiResponseRole.SeniorEmployee.ToApiValue(),
        AiResponseRole.AreaManager.ToApiValue(),
        AiResponseRole.HumanResources.ToApiValue()
    ];
}

public static class AiResponseTypeExtensions
{
    public static string ToApiValue(this AiResponseTheme theme)
    {
        return theme switch
        {
            AiResponseTheme.Technical => "technical",
            AiResponseTheme.Financial => "financial",
            AiResponseTheme.HR => "HR",
            AiResponseTheme.Conflict => "conflict",
            AiResponseTheme.Crisis => "crisis",
            AiResponseTheme.Strategy => "strategy",
            AiResponseTheme.Personal => "personal",
            AiResponseTheme.AbsurdUnexpected => "absurd/unexpected",
            _ => "technical"
        };
    }

    public static bool TryParseTheme(string? value, out AiResponseTheme theme)
    {
        var normalized = value?.Trim();
        theme = normalized switch
        {
            _ when string.Equals(normalized, "technical", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Technical,
            _ when string.Equals(normalized, "financial", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Financial,
            _ when string.Equals(normalized, "HR", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.HR,
            _ when string.Equals(normalized, "conflict", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Conflict,
            _ when string.Equals(normalized, "crisis", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Crisis,
            _ when string.Equals(normalized, "strategy", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Strategy,
            _ when string.Equals(normalized, "personal", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.Personal,
            _ when string.Equals(normalized, "absurd/unexpected", StringComparison.OrdinalIgnoreCase) => AiResponseTheme.AbsurdUnexpected,
            _ => default
        };

        return theme != default || string.Equals(normalized, "technical", StringComparison.OrdinalIgnoreCase);
    }

    public static string ToApiValue(this AiResponseType type)
    {
        return type switch
        {
            AiResponseType.People => "PEOPLE",
            AiResponseType.Event => "EVENT",
            _ => "PEOPLE"
        };
    }

    public static string ToApiValue(this AiResponseRole role)
    {
        return role switch
        {
            AiResponseRole.DeliveryPerson => "Delivery person",
            AiResponseRole.Reception => "Reception",
            AiResponseRole.Intern => "Intern",
            AiResponseRole.Maintenance => "Maintenance",
            AiResponseRole.JuniorEmployee => "Junior Employee",
            AiResponseRole.SeniorEmployee => "Senior Employee",
            AiResponseRole.AreaManager => "Area Manager",
            AiResponseRole.HumanResources => "Human Resources",
            _ => "Junior Employee"
        };
    }

    public static bool TryParseType(string? value, out AiResponseType type)
    {
        if (string.Equals(value, "PEOPLE", StringComparison.OrdinalIgnoreCase))
        {
            type = AiResponseType.People;
            return true;
        }

        if (string.Equals(value, "EVENT", StringComparison.OrdinalIgnoreCase))
        {
            type = AiResponseType.Event;
            return true;
        }

        type = default;
        return false;
    }

    public static bool TryParseRole(string? value, out AiResponseRole role)
    {
        var normalized = value?.Trim();

        if (string.Equals(normalized, "Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.Maintenance;
            return true;
        }

        if (string.Equals(normalized, "Junior Employee", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.JuniorEmployee;
            return true;
        }

        if (string.Equals(normalized, "Senior Employee", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.SeniorEmployee;
            return true;
        }

        if (string.Equals(normalized, "Area Manager", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.AreaManager;
            return true;
        }

        if (string.Equals(normalized, "Human Resources", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.HumanResources;
            return true;
        }

        if (string.Equals(normalized, "Delivery person", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.DeliveryPerson;
            return true;
        }

        if (string.Equals(normalized, "Reception", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.Reception;
            return true;
        }

        if (string.Equals(normalized, "Intern", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.Intern;
            return true;
        }

        role = default;
        return false;
    }
}
