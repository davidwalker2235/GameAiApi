namespace GameAiApi.Domain;

public enum AiResponseType
{
    People
}

public enum AiResponseRole
{
    Maintenance,
    JuniorEmployee,
    SeniorEmployee,
    AreaManager,
    HumanResources,
    CEO,
    CompanyOwner
}

public static class AiResponseTypeCatalog
{
    public static IReadOnlyList<string> Types { get; } = [
        AiResponseType.People.ToApiValue()
    ];

    public static IReadOnlyList<string> Roles { get; } = [
        AiResponseRole.Maintenance.ToApiValue(),
        AiResponseRole.JuniorEmployee.ToApiValue(),
        AiResponseRole.SeniorEmployee.ToApiValue(),
        AiResponseRole.AreaManager.ToApiValue(),
        AiResponseRole.HumanResources.ToApiValue(),
        AiResponseRole.CEO.ToApiValue(),
        AiResponseRole.CompanyOwner.ToApiValue()
    ];
}

public static class AiResponseTypeExtensions
{
    public static string ToApiValue(this AiResponseType type)
    {
        return type switch
        {
            AiResponseType.People => "PEOPLE",
            _ => "PEOPLE"
        };
    }

    public static string ToApiValue(this AiResponseRole role)
    {
        return role switch
        {
            AiResponseRole.Maintenance => "Maintenance",
            AiResponseRole.JuniorEmployee => "Junior Employee",
            AiResponseRole.SeniorEmployee => "Senior Employee",
            AiResponseRole.AreaManager => "Area Manager",
            AiResponseRole.HumanResources => "Human Resources",
            AiResponseRole.CEO => "CEO",
            AiResponseRole.CompanyOwner => "Company Owner",
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

        if (string.Equals(normalized, "CEO", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.CEO;
            return true;
        }

        if (string.Equals(normalized, "Company Owner", StringComparison.OrdinalIgnoreCase))
        {
            role = AiResponseRole.CompanyOwner;
            return true;
        }

        role = default;
        return false;
    }
}
