using System.Collections;

namespace AppSkeleton;

// A plain, non-acrylic data table - header row plus one row per item, laid out in
// columns the caller defines (see CDataGridColumn). Rows are built directly into
// RowsStack in code (not a CollectionView) since these grids are small (a statement's
// worth of lines, not thousands of rows) and every column needs to line up across a
// header built from the same Columns list - simplest way to guarantee that is to build
// both from the same loop.
public partial class CDataGrid : ContentView
{
    private static readonly Color HeaderTextColor = Color.FromArgb("#88ffffff");
    private static readonly Color RowTextColor = Colors.White;
    private static readonly Color OmittedTextColor = Color.FromArgb("#FF8A8A");
    private static readonly Color OmittedRowBackground = Color.FromArgb("#26FF6B6B");
    private static readonly Color DividerColor = Color.FromArgb("#20ffffff");

    // Built up in code right before ItemsSource is assigned - see CDataGridColumn for
    // why this isn't a bindable/XAML-settable collection.
    public List<CDataGridColumn> Columns { get; set; } = new();

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(CDataGrid),
        default(IEnumerable),
        propertyChanged: (b, _, _) => ((CDataGrid)b).Rebuild());

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(
        nameof(EmptyText),
        typeof(string),
        typeof(CDataGrid),
        "No data",
        propertyChanged: (b, _, _) => ((CDataGrid)b).Rebuild());

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    // Marks a row as not actually present in the underlying data (e.g. a statement
    // line that was skipped as a duplicate rather than imported) - that row's text
    // switches to OmittedTextColor and its background gets a faint warning tint.
    public Func<object, bool>? IsRowOmitted { get; set; }

    // Consulted only for a row IsRowOmitted flags true - shown as a small line under
    // that row explaining why it wasn't imported.
    public Func<object, string>? OmittedReason { get; set; }

    public CDataGrid()
    {
        InitializeComponent();
    }

    // Call after changing Columns (Columns itself isn't observable - see its own
    // comment) or IsRowOmitted/OmittedReason once ItemsSource is already set; assigning
    // ItemsSource itself always rebuilds.
    public void Rebuild()
    {
        HeaderStack.Children.Clear();
        RowsStack.Children.Clear();

        if (Columns.Count == 0)
            return;

        HeaderStack.Children.Add(BuildHeaderRow());
        HeaderStack.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#40ffffff"), HeightRequest = 1 });

        var items = ItemsSource?.Cast<object>().ToList() ?? new List<object>();

        if (items.Count == 0)
        {
            RowsStack.Children.Add(new Label
            {
                Text = EmptyText,
                FontSize = 12,
                TextColor = HeaderTextColor,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(12, 16)
            });
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var omitted = IsRowOmitted?.Invoke(item) ?? false;

            RowsStack.Children.Add(BuildDataRow(item, omitted));

            if (omitted && OmittedReason != null)
            {
                RowsStack.Children.Add(new Label
                {
                    Text = OmittedReason(item),
                    FontSize = 10,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = OmittedTextColor,
                    Margin = new Thickness(14, -4, 14, 8)
                });
            }

            if (i < items.Count - 1)
                RowsStack.Children.Add(new BoxView { BackgroundColor = DividerColor, HeightRequest = 1 });
        }
    }

    private Grid BuildHeaderRow()
    {
        var grid = NewRowGrid();
        grid.Padding = new Thickness(12, 8);

        for (var c = 0; c < Columns.Count; c++)
        {
            var column = Columns[c];
            var label = new Label
            {
                Text = column.Header,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = HeaderTextColor,
                HorizontalTextAlignment = column.HorizontalTextAlignment,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(label, c);
            grid.Children.Add(label);
        }

        return grid;
    }

    private Grid BuildDataRow(object item, bool omitted)
    {
        var grid = NewRowGrid();
        grid.Padding = new Thickness(12, 10);
        grid.BackgroundColor = omitted ? OmittedRowBackground : Colors.Transparent;

        for (var c = 0; c < Columns.Count; c++)
        {
            var column = Columns[c];
            var label = new Label
            {
                Text = column.CellText(item),
                FontSize = 12,
                TextColor = omitted ? OmittedTextColor : RowTextColor,
                HorizontalTextAlignment = column.HorizontalTextAlignment,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(label, c);
            grid.Children.Add(label);
        }

        return grid;
    }

    private Grid NewRowGrid()
    {
        var grid = new Grid { ColumnSpacing = 8 };

        foreach (var column in Columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));

        return grid;
    }
}
