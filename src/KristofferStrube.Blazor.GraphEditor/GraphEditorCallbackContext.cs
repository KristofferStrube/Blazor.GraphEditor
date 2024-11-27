namespace KristofferStrube.Blazor.GraphEditor;

/// <summary>
/// The context that specifies any functions that needs to be triggered for specific events in the graph..
/// </summary>
public class GraphEditorCallbackContext
{
    /// <summary>
    /// The function that will be invoked when a node is selected by getting focus.
    /// </summary>
    public required Func<string, Task> NodeSelectionCallback { get; set; }
}
