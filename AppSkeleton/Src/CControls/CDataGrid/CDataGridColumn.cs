namespace AppSkeleton;

// One column of a CDataGrid - deliberately a plain code-behind object (not a
// BindableObject/XAML-settable collection) since a data grid's columns are almost
// always built up in code right next to the ItemsSource that feeds them (different row
// shapes need different cells), the same way CChart's drawables are handed in from
// code rather than declared in XAML.
public class CDataGridColumn
{
    public string Header { get; set; } = "";

    // How wide this column is relative to the others - Star (default) splits leftover
    // space proportionally, Auto sizes to the widest cell, an absolute GridLength pins
    // it exactly.
    public GridLength Width { get; set; } = GridLength.Star;

    public TextAlignment HorizontalTextAlignment { get; set; } = TextAlignment.Start;

    // Pulls this column's text out of a row object - a delegate rather than a
    // reflection-based property-name string, since CDataGrid's rows are typically
    // already-shaped view-model objects the caller built for exactly this grid.
    public Func<object, string> CellText { get; set; } = _ => "";
}
