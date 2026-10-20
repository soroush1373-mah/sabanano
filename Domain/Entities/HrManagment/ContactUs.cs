namespace CleanArchitecture.Domain.Entities.HrManagment;

public class ContactUs : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Note { get; set; }
}