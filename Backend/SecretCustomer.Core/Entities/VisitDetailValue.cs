namespace SecretCustomer.Core.Entities;

/// <summary>
/// Ziyaret detay değerleri
/// Dinamik alan sistemi - her alan için typed değerler tutar
/// </summary>
public class VisitDetailValue : BaseEntity
{
    /// <summary>
    /// İlişkili müşteri ziyareti
    /// </summary>
    public Guid CustomerVisitId { get; set; }
    public CustomerVisit CustomerVisit { get; set; } = null!;

    /// <summary>
    /// Alan tanımı (dinamik)
    /// </summary>
    public Guid FieldDefinitionId { get; set; }
    public VisitFieldDefinition FieldDefinition { get; set; } = null!;

    // ===== TYPED DEĞERLER (SQL sorgulanabilir) =====

    /// <summary>
    /// Tam sayı değeri (sayılar, rating'ler için)
    /// </summary>
    public int? IntValue { get; set; }

    /// <summary>
    /// Ondalıklı sayı değeri
    /// </summary>
    public decimal? DecimalValue { get; set; }

    /// <summary>
    /// Boolean değeri
    /// </summary>
    public bool? BoolValue { get; set; }

    /// <summary>
    /// Metin değeri
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Tarih/Saat değeri
    /// </summary>
    public DateTime? DateTimeValue { get; set; }

    // ===== YARDIMCI METODLAR =====

    /// <summary>
    /// Değeri object olarak döndürür
    /// </summary>
    public object? GetValue()
    {
        if (IntValue.HasValue) return IntValue.Value;
        if (DecimalValue.HasValue) return DecimalValue.Value;
        if (BoolValue.HasValue) return BoolValue.Value;
        if (DateTimeValue.HasValue) return DateTimeValue.Value;
        if (StringValue != null) return StringValue;
        return null;
    }

    /// <summary>
    /// Değeri tipine göre set eder
    /// </summary>
    public void SetValue(object? value)
    {
        // Tüm değerleri temizle
        IntValue = null;
        DecimalValue = null;
        BoolValue = null;
        StringValue = null;
        DateTimeValue = null;

        if (value == null) return;

        switch (value)
        {
            case int intVal:
                IntValue = intVal;
                break;
            case long longVal:
                IntValue = (int)longVal;
                break;
            case decimal decVal:
                DecimalValue = decVal;
                break;
            case double doubleVal:
                DecimalValue = (decimal)doubleVal;
                break;
            case float floatVal:
                DecimalValue = (decimal)floatVal;
                break;
            case bool boolVal:
                BoolValue = boolVal;
                break;
            case DateTime dateVal:
                DateTimeValue = dateVal;
                break;
            case string strVal:
                StringValue = strVal;
                break;
            default:
                StringValue = value.ToString();
                break;
        }
    }

    /// <summary>
    /// Yeni bir VisitDetailValue oluşturur
    /// </summary>
    public static VisitDetailValue Create(Guid customerVisitId, Guid fieldDefinitionId, object? value)
    {
        var detail = new VisitDetailValue
        {
            Id = Guid.NewGuid(),
            CustomerVisitId = customerVisitId,
            FieldDefinitionId = fieldDefinitionId,
            CreatedAt = DateTime.UtcNow
        };
        detail.SetValue(value);
        return detail;
    }
}
