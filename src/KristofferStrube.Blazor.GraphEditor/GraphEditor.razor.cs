using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace KristofferStrube.Blazor.GraphEditor;

public partial class GraphEditor<TNode, TEdge> : ComponentBase
{
    private GraphEditorCallbackContext callbackContext = default!;
    private Node[] nodeElements = [];
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
    public required Func<TEdge, string> EdgeFromMapper { get; set; }

    [Parameter, EditorRequired]
    public required Func<TEdge, string> EdgeToMapper { get; set; }

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
        Nodes = nodes.ToDictionary(n => NodeIdMapper(n), n => n);
        Edges = edges.ToDictionary(EdgeId, e => e);
        StringBuilder sb = new();
        foreach (TEdge edge in edges)
        {
            sb.Append(@$"<line data-elementtype=""edge"" stroke=""black"" stroke-width=""{EdgeWidthMapper(edge).AsString()}"" data-from=""{EdgeFromMapper(edge)}"" data-to=""{EdgeToMapper(edge)}""></line>");
        }
        foreach (TNode node in nodes)
        {
            sb.Append(@$"<circle data-elementtype=""node"" r=""{NodeRadiusMapper(node)}"" cx=""{(200 + Random.Shared.NextDouble() * 20).AsString()}"" cy=""{(200 + Random.Shared.NextDouble() * 20).AsString()}"" stroke=""{NodeColorMapper(node)}"" id=""{NodeIdMapper(node)}""></circle>");
        }
        Input = sb.ToString();
        await Task.Yield();
        StateHasChanged();
        nodeElements = SVGEditor.Elements.Where(e => e is Node).Select(e => (Node)e).ToArray();
    }

    public Task ForceDirectedLayout()
    {
        for (int i = 0; i < nodeElements.Length; i++)
        {
            Node node1 = nodeElements[i];
            double mx = 0;
            double my = 0;
            for (int j = 0; j < nodeElements.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Node node2 = nodeElements[j];
                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force;
                if (node1.Edges.FirstOrDefault(e => e.To == node2 || e.From == node2) is Edge { } e)
                {
                    if (!Edges.TryGetValue(node1.Id + "-" + node2.Id, out TEdge? edge))
                    {
                        Edges.TryGetValue(node2.Id + "-" + node1.Id, out edge);
                    }
                    force = EdgeSpringConstantMapper(edge!) * Math.Log(d / EdgeSpringLengthMapper(edge!));
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

        foreach (Edge edge in SVGEditor.Elements.Where(e => e is Edge))
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
        new(typeof(Node), element => element.TagName is "CIRCLE" && element.GetAttribute("data-elementtype") == "node"),
        new(typeof(Edge), element => element.TagName is "LINE" && element.GetAttribute("data-elementtype") == "edge"),
    };
}