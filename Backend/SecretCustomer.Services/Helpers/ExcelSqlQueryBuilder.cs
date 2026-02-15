using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Helpers;

public class ExcelSqlQueryResult
{
    public string Sql { get; set; } = string.Empty;
    public DynamicParameters Parameters { get; set; } = new();
    public List<string> ColumnAliases { get; set; } = new();
}

public class ExcelSqlQueryBuilder
{
    private readonly IModel _model;

    public ExcelSqlQueryBuilder(IModel model)
    {
        _model = model;
    }

    public ExcelSqlQueryResult Build(
        string entityTypeName,
        List<ExcelColumn> columns,
        string? groupByPropertyName,
        List<ExportFilterValue>? filterValues,
        int? limit = null)
    {
        var rootEntityType = FindEntityType(entityTypeName);
        if (rootEntityType == null)
            throw new InvalidOperationException($"Entity type '{entityTypeName}' not found in EF model");

        var rootTable = rootEntityType.GetTableName()!;
        var rootSchema = rootEntityType.GetSchema();
        var rootTableFull = rootSchema != null ? $"\"{rootSchema}\".\"{rootTable}\"" : $"\"{rootTable}\"";

        // Track joins: path -> (alias, joinSql)
        var joinAliases = new Dictionary<string, string>();
        var joinClauses = new List<string>();
        int aliasCounter = 0;
        var rootAlias = "t0";
        aliasCounter++;

        var selectColumns = new List<string>();
        var columnAliases = new List<string>();

        // Collect all paths we need (from columns + groupBy + filters)
        var allPaths = columns.Select(c => c.PropertyName).ToList();
        if (!string.IsNullOrWhiteSpace(groupByPropertyName))
            allPaths.Add(groupByPropertyName);
        if (filterValues != null)
            allPaths.AddRange(filterValues.Select(f => f.PropertyName));

        // Pre-resolve all navigation joins
        foreach (var path in allPaths.Distinct())
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            ResolveJoins(path, rootEntityType, rootAlias, joinAliases, joinClauses, ref aliasCounter);
        }

        // Build SELECT columns
        bool hasGroupBy = !string.IsNullOrWhiteSpace(groupByPropertyName);

        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var colAlias = $"col_{i}";
            columnAliases.Add(colAlias);

            var sqlExpr = ResolveSqlExpression(col.PropertyName, rootEntityType, rootAlias, joinAliases);

            if (hasGroupBy && col.AggregateTypeId != AggregateTypes.Ids.None)
            {
                sqlExpr = WrapAggregate(sqlExpr, col.AggregateTypeId);
            }

            selectColumns.Add($"{sqlExpr} AS {colAlias}");
        }

        // Build WHERE
        var whereClauses = new List<string> { $"{rootAlias}.\"IsDeleted\" = false" };
        var parameters = new DynamicParameters();
        int paramCounter = 0;

        if (filterValues != null)
        {
            foreach (var filter in filterValues)
            {
                if (string.IsNullOrWhiteSpace(filter.Value)) continue;

                var filterExpr = ResolveSqlExpression(filter.PropertyName, rootEntityType, rootAlias, joinAliases);
                var paramName = $"p{paramCounter++}";

                var (clause, paramValue) = BuildFilterClause(filterExpr, filter.OperatorTypeId, paramName, filter.Value);
                whereClauses.Add(clause);
                parameters.Add(paramName, paramValue);
            }
        }

        // Build GROUP BY
        string? groupBySql = null;
        if (hasGroupBy)
        {
            // Group by all non-aggregated columns
            var groupByExprs = new List<string>();
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].AggregateTypeId == AggregateTypes.Ids.None)
                {
                    var expr = ResolveSqlExpression(columns[i].PropertyName, rootEntityType, rootAlias, joinAliases);
                    groupByExprs.Add(expr);
                }
            }
            if (groupByExprs.Any())
                groupBySql = "GROUP BY " + string.Join(", ", groupByExprs);
        }

        // Assemble SQL
        var sql = $"SELECT {string.Join(", ", selectColumns)}\n" +
                  $"FROM {rootTableFull} {rootAlias}\n" +
                  string.Join("\n", joinClauses) +
                  (joinClauses.Any() ? "\n" : "") +
                  $"WHERE {string.Join(" AND ", whereClauses)}";

        if (groupBySql != null)
            sql += $"\n{groupBySql}";

        if (limit.HasValue)
        {
            sql += $"\nLIMIT @limit";
            parameters.Add("limit", limit.Value);
        }

        return new ExcelSqlQueryResult
        {
            Sql = sql,
            Parameters = parameters,
            ColumnAliases = columnAliases
        };
    }

    /// <summary>
    /// Builds navigation schema for an entity type (2 levels deep)
    /// </summary>
    public List<NavigationSchemaItem> BuildNavigationSchema(string entityTypeName)
    {
        var entityType = FindEntityType(entityTypeName);
        if (entityType == null) return new List<NavigationSchemaItem>();

        return GetNavigationsForEntity(entityType, maxDepth: 2, currentDepth: 0);
    }

    private List<NavigationSchemaItem> GetNavigationsForEntity(IEntityType entityType, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth) return new List<NavigationSchemaItem>();

        var result = new List<NavigationSchemaItem>();

        foreach (var nav in entityType.GetNavigations())
        {
            var targetType = nav.TargetEntityType;
            var isCollection = nav.IsCollection;

            // Skip self-referencing navigation and BaseEntity properties
            if (targetType == entityType) continue;

            var item = new NavigationSchemaItem
            {
                Name = nav.Name,
                Type = isCollection ? "collection" : "scalar",
                TargetEntity = targetType.ClrType.Name,
                Properties = GetScalarProperties(targetType),
                Navigations = GetNavigationsForEntity(targetType, maxDepth, currentDepth + 1)
            };

            result.Add(item);
        }

        return result;
    }

    private List<PropertySchemaItem> GetScalarProperties(IEntityType entityType)
    {
        var result = new List<PropertySchemaItem>();
        var skipProps = new HashSet<string> { "Id", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted" };

        foreach (var prop in entityType.GetProperties())
        {
            // Skip shadow properties, FK properties, and base entity properties
            if (prop.IsShadowProperty()) continue;
            if (skipProps.Contains(prop.Name)) continue;

            // Skip FK IDs (end with "Id" and have a corresponding navigation)
            if (prop.Name.EndsWith("Id") && prop.Name.Length > 2)
            {
                var navName = prop.Name[..^2];
                if (entityType.GetNavigations().Any(n => n.Name == navName))
                    continue;
            }

            result.Add(new PropertySchemaItem
            {
                PropertyName = prop.Name,
                PropertyType = GetSimpleTypeName(prop.ClrType)
            });
        }

        return result;
    }

    private string GetSimpleTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var actual = underlying ?? type;

        if (actual == typeof(string)) return "string";
        if (actual == typeof(int)) return "int";
        if (actual == typeof(long)) return "long";
        if (actual == typeof(decimal)) return "decimal";
        if (actual == typeof(double)) return "double";
        if (actual == typeof(float)) return "float";
        if (actual == typeof(bool)) return "bool";
        if (actual == typeof(DateTime)) return "DateTime";
        if (actual == typeof(Guid)) return "Guid";
        if (actual.IsEnum) return "enum";
        return actual.Name;
    }

    private IEntityType? FindEntityType(string entityTypeName)
    {
        return _model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == entityTypeName);
    }

    private void ResolveJoins(
        string dotPath,
        IEntityType rootEntityType,
        string rootAlias,
        Dictionary<string, string> joinAliases,
        List<string> joinClauses,
        ref int aliasCounter)
    {
        var parts = dotPath.Split('.');
        if (parts.Length <= 1) return; // Scalar property on root - no join needed

        var currentEntityType = rootEntityType;
        var currentAlias = rootAlias;
        var pathSoFar = "";

        // Walk the navigation path (all parts except the last which is the property)
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var navName = parts[i];
            pathSoFar = pathSoFar == "" ? navName : $"{pathSoFar}.{navName}";

            // Already joined this path?
            if (joinAliases.ContainsKey(pathSoFar))
            {
                currentAlias = joinAliases[pathSoFar];
                var nav = currentEntityType.FindNavigation(navName);
                if (nav != null)
                    currentEntityType = nav.TargetEntityType;
                continue;
            }

            var navigation = currentEntityType.FindNavigation(navName);
            if (navigation == null)
                throw new InvalidOperationException(
                    $"Navigation '{navName}' not found on entity '{currentEntityType.ClrType.Name}'. Full path: '{dotPath}'");

            var targetEntityType = navigation.TargetEntityType;
            var targetTable = targetEntityType.GetTableName()!;
            var targetSchema = targetEntityType.GetSchema();
            var targetTableFull = targetSchema != null ? $"\"{targetSchema}\".\"{targetTable}\"" : $"\"{targetTable}\"";
            var newAlias = $"t{aliasCounter++}";

            // Determine join condition based on FK
            string joinCondition;
            var foreignKey = navigation.ForeignKey;

            if (navigation.IsOnDependent)
            {
                // e.g., Evaluation.Project -> Evaluation has ProjectId FK
                var fkProp = foreignKey.Properties.First();
                var fkColumn = fkProp.GetColumnName()!;
                var principalKey = foreignKey.PrincipalKey.Properties.First();
                var pkColumn = principalKey.GetColumnName()!;

                joinCondition = $"{newAlias}.\"{pkColumn}\" = {currentAlias}.\"{fkColumn}\"";
            }
            else
            {
                // e.g., Evaluation.Answers -> Answer has EvaluationId FK (collection)
                var fkProp = foreignKey.Properties.First();
                var fkColumn = fkProp.GetColumnName()!;
                var principalKey = foreignKey.PrincipalKey.Properties.First();
                var pkColumn = principalKey.GetColumnName()!;

                joinCondition = $"{newAlias}.\"{fkColumn}\" = {currentAlias}.\"{pkColumn}\"";
            }

            // Check if target has IsDeleted
            var hasIsDeleted = targetEntityType.FindProperty("IsDeleted") != null;
            var softDeleteClause = hasIsDeleted ? $" AND {newAlias}.\"IsDeleted\" = false" : "";

            joinClauses.Add($"LEFT JOIN {targetTableFull} {newAlias} ON {joinCondition}{softDeleteClause}");
            joinAliases[pathSoFar] = newAlias;

            currentAlias = newAlias;
            currentEntityType = targetEntityType;
        }
    }

    private string ResolveSqlExpression(
        string dotPath,
        IEntityType rootEntityType,
        string rootAlias,
        Dictionary<string, string> joinAliases)
    {
        var parts = dotPath.Split('.');
        if (parts.Length == 1)
        {
            // Scalar on root
            var prop = rootEntityType.FindProperty(parts[0]);
            if (prop == null)
                throw new InvalidOperationException($"Property '{parts[0]}' not found on entity '{rootEntityType.ClrType.Name}'");
            return $"{rootAlias}.\"{prop.GetColumnName()}\"";
        }

        // Navigate to the correct alias
        var navPath = string.Join(".", parts.Take(parts.Length - 1));
        var propertyName = parts.Last();

        if (!joinAliases.TryGetValue(navPath, out var alias))
            throw new InvalidOperationException($"Join alias not found for path '{navPath}'. Ensure ResolveJoins was called first.");

        // Find the entity type at the nav path
        var currentEntityType = rootEntityType;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var nav = currentEntityType.FindNavigation(parts[i]);
            if (nav == null)
                throw new InvalidOperationException($"Navigation '{parts[i]}' not found on '{currentEntityType.ClrType.Name}'");
            currentEntityType = nav.TargetEntityType;
        }

        var property = currentEntityType.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on entity '{currentEntityType.ClrType.Name}'");

        return $"{alias}.\"{property.GetColumnName()}\"";
    }

    private static string WrapAggregate(string expr, int aggregateTypeId)
    {
        return aggregateTypeId switch
        {
            AggregateTypes.Ids.Sum => $"SUM({expr})",
            AggregateTypes.Ids.Avg => $"AVG({expr})",
            AggregateTypes.Ids.Count => $"COUNT({expr})",
            AggregateTypes.Ids.Concat => $"STRING_AGG({expr}::text, ', ')",
            AggregateTypes.Ids.Min => $"MIN({expr})",
            AggregateTypes.Ids.Max => $"MAX({expr})",
            _ => expr
        };
    }

    private static (string clause, object paramValue) BuildFilterClause(
        string sqlExpr, int operatorTypeId, string paramName, string rawValue)
    {
        return operatorTypeId switch
        {
            FilterOperatorTypes.Ids.Equals =>
                ($"{sqlExpr}::text = @{paramName}", rawValue),
            FilterOperatorTypes.Ids.NotEquals =>
                ($"{sqlExpr}::text != @{paramName}", rawValue),
            FilterOperatorTypes.Ids.Contains =>
                ($"{sqlExpr}::text ILIKE @{paramName}", $"%{rawValue}%"),
            FilterOperatorTypes.Ids.GreaterThan =>
                ($"{sqlExpr}::text > @{paramName}", rawValue),
            FilterOperatorTypes.Ids.LessThan =>
                ($"{sqlExpr}::text < @{paramName}", rawValue),
            FilterOperatorTypes.Ids.GreaterOrEqual =>
                ($"{sqlExpr}::text >= @{paramName}", rawValue),
            FilterOperatorTypes.Ids.LessOrEqual =>
                ($"{sqlExpr}::text <= @{paramName}", rawValue),
            _ => ($"{sqlExpr}::text = @{paramName}", rawValue)
        };
    }
}

public class NavigationSchemaItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "scalar" or "collection"
    public string TargetEntity { get; set; } = string.Empty;
    public List<PropertySchemaItem> Properties { get; set; } = new();
    public List<NavigationSchemaItem> Navigations { get; set; } = new();
}

public class PropertySchemaItem
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
}
