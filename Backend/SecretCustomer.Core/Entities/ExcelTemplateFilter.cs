namespace SecretCustomer.Core.Entities;

public class ExcelTemplateFilter : BaseEntity
{
    public int ExcelTemplateId { get; set; }
    public ExcelTemplate ExcelTemplate { get; set; } = null!;

    public string Label { get; set; } = string.Empty;  // UI'da gösterilecek etiket (ör: "Proje Adı")
    public string PropertyName { get; set; } = string.Empty;  // Dot notation destekli (ör: "Project.Name")
    public int OperatorTypeId { get; set; } = Enums.FilterOperatorTypes.Ids.Equals;
    public int Order { get; set; }  // Sıralama
}
