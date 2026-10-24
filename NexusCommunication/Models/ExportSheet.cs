namespace NexusCommunication.Models;

/// <summary>One exportable table: used for the .xlsx / .csv downloads and the printable PDF report.</summary>
/// <param name="Rows">cell values are string, decimal, int, DateTime or null - each output format renders them its own way</param>
/// <param name="SumCols">indexes of money columns that get a total line on the printed report</param>
public record ExportSheet(
    string Resource, string Title, string Subtitle, string[] Headers, List<object?[]> Rows, int[] SumCols,
    string PreparedBy, string Scope, string? Filter);
