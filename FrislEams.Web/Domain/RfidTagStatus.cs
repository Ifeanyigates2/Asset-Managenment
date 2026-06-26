namespace FrislEams.Web.Domain;

public static class RfidTagStatus
{
    public const string Blank = "Blank";
    public const string Assigned = "Assigned";
    public const string Active = "Active";
    public const string Damaged = "Damaged";
    public const string Lost = "Lost";
    public const string Retired = "Retired";

    public static readonly string[] All = [Blank, Assigned, Active, Damaged, Lost, Retired];
}
