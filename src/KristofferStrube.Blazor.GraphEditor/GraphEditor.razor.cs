using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components;

namespace KristofferStrube.Blazor.GraphEditor;

public partial class GraphEditor<TNode, TEdge> : ComponentBase
{
    private GraphEditorCallbackContext callbackContext = default!;
    private Node<TNode, TEdge>[] nodeElements = [];
    private string EdgeId(TEdge e)
    {
        return EdgeFromMapper(e) + "-" + EdgeToMapper(e);
    }

    [Parameter, EditorRequired]
    public required Func<TNode, string> NodeIdMapper { get; set; }

    [Parameter]
    public Func<TNode, string> NodeColorMapper { get; set; } = _ => "#66BB6A";

    [Parameter]
    public Func<TNode, double> NodeRadiusMapper { get; set; } = _ => 50;

    [Parameter]
    public Func<TNode, double> NodeRepulsionMapper { get; set; } = _ => 800;

    [Parameter, EditorRequired]
    public required Func<TEdge, TNode> EdgeFromMapper { get; set; }

    [Parameter, EditorRequired]
    public required Func<TEdge, TNode> EdgeToMapper { get; set; }

    [Parameter]
    public Func<TEdge, double> EdgeWidthMapper { get; set; } = _ => 1;

    [Parameter]
    public Func<TEdge, double> EdgeSpringConstantMapper { get; set; } = _ => 1;

    [Parameter]
    public Func<TEdge, double> EdgeSpringLengthMapper { get; set; } = _ => 200;

    [Parameter]
    public Func<TNode, Task>? NodeSelectionCallback { get; set; }

    protected override void OnInitialized()
    {
        callbackContext = new()
        {
            NodeSelectionCallback = async (id) =>
            {
                if (NodeSelectionCallback is not null && Nodes.TryGetValue(id, out TNode? node))
                {
                    await NodeSelectionCallback.Invoke(node);
                }
            }
        };
    }

    public async Task LoadGraph(List<TNode> nodes, List<TEdge> edges)
    {
        Nodes = nodes.ToDictionary(NodeIdMapper, n => n);
        Edges = edges.ToDictionary(EdgeId, e => e);

        Dictionary<TNode, Node<TNode, TEdge>> nodeDataHolders = [];

        foreach (TNode node in nodes)
        {
            Node<TNode, TEdge> dataHolder = Node<TNode, TEdge>.AddNew(SVGEditor, this, node);
            dataHolder.Cx = 200 + Random.Shared.NextDouble() * 20;
            dataHolder.Cy = 200 + Random.Shared.NextDouble() * 20;
            nodeDataHolders.Add(node, dataHolder);
        }
        foreach (TEdge edge in edges)
        {
            SVGEditor.SelectedShapes.Add(Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, nodeDataHolders[EdgeFromMapper(edge)], nodeDataHolders[EdgeToMapper(edge)]));
        }
        SVGEditor.MoveToBack();

        await Task.Yield();
        StateHasChanged();
        nodeElements = SVGEditor.Elements.Where(e => e is Node<TNode, TEdge>).Select(e => (Node<TNode, TEdge>)e).ToArray();
    }

    public Task ForceDirectedLayout()
    {
        for (int i = 0; i < nodeElements.Length; i++)
        {
            Node<TNode, TEdge> node1 = nodeElements[i];
            double mx = 0;
            double my = 0;
            for (int j = 0; j < nodeElements.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Node<TNode, TEdge> node2 = nodeElements[j];
                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force;
                if (node1.Edges.FirstOrDefault(e => e.To == node2 || e.From == node2) is Edge<TNode, TEdge> { } e)
                {
                    force = EdgeSpringConstantMapper(e.Data) * Math.Log(d / EdgeSpringLengthMapper(e.Data));
                }
                else
                {
                    force = -(NodeRepulsionMapper(Nodes[node1.Id!]) + NodeRepulsionMapper(Nodes[node2.Id!])) / 2 / (d * d);
                }

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            if (!SVGEditor.SelectedShapes.Contains(node1))
            {
                node1.Cx += mx;
                node1.Cy += my;
            }
        }

        foreach (Edge<TNode, TEdge> edge in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>))
        {
            edge.UpdateLine();
        }
        return Task.CompletedTask;
    }

    protected Dictionary<string, TNode> Nodes { get; set; } = new();

    protected Dictionary<string, TEdge> Edges { get; set; } = new();

    protected SVGEditor.SVGEditor SVGEditor { get; set; } = default!;

    protected string Input { get; set; } = "";

    protected List<SupportedElement> SupportedElements { get; set; } = new()
    {
        new(typeof(Node<TNode, TEdge>), element => element.TagName is "CIRCLE" && element.GetAttribute("data-elementtype") == "node"),
        new(typeof(Edge<TNode, TEdge>), element => element.TagName is "LINE" && element.GetAttribute("data-elementtype") == "edge"),
    };
}