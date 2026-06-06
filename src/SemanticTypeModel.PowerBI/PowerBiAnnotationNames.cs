namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Defines Power BI projection-specific annotation keys.
/// </summary>
public static class PowerBiAnnotationNames
{
    /// <summary>Power BI table role annotation.</summary>
    public const string TableRole = "powerBi.tableRole";

    /// <summary>Power BI table name annotation.</summary>
    public const string TableName = "powerBi.tableName";

    /// <summary>Power BI column name annotation.</summary>
    public const string ColumnName = "powerBi.columnName";

    /// <summary>Power BI measure expression annotation.</summary>
    public const string MeasureExpression = "powerBi.measureExpression";

    /// <summary>Power BI format string annotation.</summary>
    public const string FormatString = "powerBi.formatString";

    /// <summary>Power BI summarization annotation.</summary>
    public const string Summarization = "powerBi.summarizeBy";

    /// <summary>Power BI hidden flag annotation.</summary>
    public const string Hidden = "powerBi.isHidden";

    /// <summary>Power BI display folder annotation.</summary>
    public const string DisplayFolder = "powerBi.displayFolder";

    /// <summary>Power BI data category annotation.</summary>
    public const string DataCategory = "powerBi.dataCategory";

    /// <summary>Power BI active relationship annotation.</summary>
    public const string RelationshipActive = "powerBi.isActive";

    /// <summary>Power BI relationship name annotation.</summary>
    public const string RelationshipName = "powerBi.relationshipName";


    /// <summary>Power BI sort-by-column annotation.</summary>
    public const string SortByColumn = "powerBi.sortByColumn";

    /// <summary>Power BI calculated table annotation.</summary>
    public const string CalculatedTable = "powerBi.calculatedTable";

    /// <summary>Power BI hierarchy annotation.</summary>
    public const string Hierarchy = "powerBi.hierarchy";
}
