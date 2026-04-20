namespace AppSkeleton;

public partial class CView : ContentView
{
	public CView()
	{
		InitializeComponent();
	}

	public void SetContent(ContentView content)
	{
		Content = content;
	}
}