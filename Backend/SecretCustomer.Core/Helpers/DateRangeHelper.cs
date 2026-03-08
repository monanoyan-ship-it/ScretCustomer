using System.Linq.Expressions;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Helpers;

/// <summary>
/// DateRange listesini OR mantığıyla IQueryable'a uygular.
/// Birden fazla tarih aralığı (Ocak 2025, Ocak 2026 gibi) karşılaştırma için
/// her biri ayrı predicate olarak OR ile birleştirilir.
/// </summary>
public static class DateRangeHelper
{
    /// <summary>
    /// Belirtilen property üzerinde OR mantığıyla DateRange filtresi uygular.
    /// </summary>
    /// <param name="query">Sorgu</param>
    /// <param name="dateRanges">Tarih aralıkları listesi</param>
    /// <param name="datePropertyName">Filtrelenecek DateTime property adı (ör: "CreatedAt", "RequestedAt")</param>
    public static IQueryable<T> ApplyOrFilter<T>(
        IQueryable<T> query, List<DateRangeFilter>? dateRanges, string datePropertyName)
    {
        if (dateRanges == null || !dateRanges.Any()) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var dateProp = Expression.Property(param, datePropertyName);
        var propType = typeof(T).GetProperty(datePropertyName)?.PropertyType;
        var isNullable = propType != null && (Nullable.GetUnderlyingType(propType) != null);

        Expression? orBody = null;

        foreach (var dr in dateRanges)
        {
            var startUtc = dr.StartDate.HasValue
                ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
            var endUtc = dr.EndDate.HasValue
                ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

            Expression? rangeExpr = null;
            if (startUtc.HasValue)
            {
                var startConst = isNullable
                    ? Expression.Constant((DateTime?)startUtc.Value, typeof(DateTime?))
                    : Expression.Constant(startUtc.Value, typeof(DateTime));
                rangeExpr = Expression.GreaterThanOrEqual(dateProp, startConst);
            }
            if (endUtc.HasValue)
            {
                var endConst = isNullable
                    ? Expression.Constant((DateTime?)endUtc.Value, typeof(DateTime?))
                    : Expression.Constant(endUtc.Value, typeof(DateTime));
                var leExpr = Expression.LessThanOrEqual(dateProp, endConst);
                rangeExpr = rangeExpr != null ? Expression.AndAlso(rangeExpr, leExpr) : leExpr;
            }
            if (rangeExpr != null)
            {
                if (isNullable)
                {
                    var notNull = Expression.NotEqual(dateProp, Expression.Constant(null, typeof(DateTime?)));
                    rangeExpr = Expression.AndAlso(notNull, rangeExpr);
                }
                orBody = orBody != null ? Expression.OrElse(orBody, rangeExpr) : rangeExpr;
            }
        }

        if (orBody != null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(orBody, param));

        return query;
    }
}
