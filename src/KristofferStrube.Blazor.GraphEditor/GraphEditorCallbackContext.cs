namespace KristofferStrube.Blazor.GraphEditor;

public class GraphEditorCallbackContext
{
    public Func<string, Task> NodeSelectionCallback { get; set; }
}
