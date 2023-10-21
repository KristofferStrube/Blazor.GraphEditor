namespace KristofferStrube.Blazor.GraphEditor;

public class GraphEditorCallbackContext
{
    public required Func<string, Task> NodeSelectionCallback { get; set; }
}
