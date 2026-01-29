using ClosedXML.Excel;

namespace SecretCustomer.Core.Helpers;

/// <summary>
/// Excel işlemleri için yardımcı metodlar
/// </summary>
public static class ExcelHelper
{
    /// <summary>
    /// Excel worksheet'te belirtilen sütunlara max genişlik ve word wrap uygular.
    /// AdjustToContents() çağrısından SONRA çağrılmalıdır.
    /// </summary>
    /// <param name="worksheet">Excel worksheet</param>
    /// <param name="callIdColumns">CallId sütunları (word wrap OLACAK)</param>
    /// <param name="noteColumns">Not/Öneri sütunları (word wrap OLMAYACAK)</param>
    /// <param name="subCriteriaColumns">Alt kriter sütunları (word wrap OLACAK)</param>
    /// <param name="maxWidth">Maksimum sütun genişliği (varsayılan: 20)</param>
    public static void ApplyLongTextColumnStyles(
        IXLWorksheet worksheet,
        int[]? callIdColumns = null,
        int[]? noteColumns = null,
        int[]? subCriteriaColumns = null,
        int maxWidth = 20)
    {
        // CallId sütunları: max genişlik, word wrap YOK
        if (callIdColumns != null)
        {
            foreach (var col in callIdColumns)
            {
                if (worksheet.Column(col).Width > maxWidth)
                {
                    worksheet.Column(col).Width = maxWidth;
                }
                worksheet.Column(col).Style.Alignment.WrapText = false;
                worksheet.Column(col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }

        // Not/Öneri sütunları: sadece max genişlik, word wrap YOK
        if (noteColumns != null)
        {
            foreach (var col in noteColumns)
            {
                if (worksheet.Column(col).Width > maxWidth)
                {
                    worksheet.Column(col).Width = maxWidth;
                }
                // Word wrap YOK, sadece dikey hizalama
                worksheet.Column(col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }

        // Alt kriter sütunları: max genişlik + word wrap (birden çok seçilmişse alt satıra geçer)
        if (subCriteriaColumns != null)
        {
            foreach (var col in subCriteriaColumns)
            {
                if (worksheet.Column(col).Width > maxWidth)
                {
                    worksheet.Column(col).Width = maxWidth;
                }
                worksheet.Column(col).Style.Alignment.WrapText = true;
                worksheet.Column(col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }
    }
}
