using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components;
using System.Xml.Linq;

namespace KristofferStrube.Blazor.GraphEditor;

public partial class GraphEditor<TNode, TEdge> : ComponentBase where TNode : IEquatable<TNode>
{
    private GraphEditorCallbackContext callbackContext = default!;
    private Node<TNode, TEdge>[] nodeElements = [];
    private string EdgeId(TEdge e)
    {
        return NodeIdMapper(EdgeFromMapper(e)) + "-" + NodeIdMapper(EdgeToMapper(e));
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
        SVGEditor.Translate = (200, 200);

        Nodes = nodes.ToDictionary(NodeIdMapper, n => n);
        Edges = edges.ToDictionary(EdgeId, e => e);

        Dictionary<TNode, Node<TNode, TEdge>> nodeElementDictionary = [];

        foreach (TNode node in nodes)
        {
            Node<TNode, TEdge> element = Node<TNode, TEdge>.AddNew(SVGEditor, this, node);
            element.Cx = Random.Shared.NextDouble() * 20;
            element.Cy = Random.Shared.NextDouble() * 20;
            nodeElementDictionary.Add(node, element);
        }
        foreach (TEdge edge in edges)
        {
            SVGEditor.SelectedShapes.Add(Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, nodeElementDictionary[EdgeFromMapper(edge)], nodeElementDictionary[EdgeToMapper(edge)]));
        }
        SVGEditor.MoveToBack();

        await Task.Yield();
        StateHasChanged();
        nodeElements = SVGEditor.Elements.Where(e => e is Node<TNode, TEdge>).Select(e => (Node<TNode, TEdge>)e).ToArray();
    }


    public async Task UpdateGraph(List<TNode> nodes, List<TEdge> edges)
    {
        Dictionary<TNode, Node<TNode, TEdge>> newNodeElementDictionary = [];

        foreach (TNode node in nodes)
        {
            if (!Nodes.ContainsKey(NodeIdMapper(node)))
            {
                Node<TNode, TEdge> element = Node<TNode, TEdge>.AddNew(SVGEditor, this, node);
                newNodeElementDictionary.Add(node, element);
                Nodes.Add(NodeIdMapper(node), node);
            }
        }
        nodeElements = SVGEditor.Elements.Where(e => e is Node<TNode, TEdge>).Select(e => (Node<TNode, TEdge>)e).ToArray();
        var copyOfSelectShapes = SVGEditor.SelectedShapes.ToList();
        SVGEditor.ClearSelectedShapes();
        foreach (TEdge edge in edges)
        {
            if (!Edges.ContainsKey(EdgeId(edge)))
            {
                SVGEditor.SelectedShapes.Add(Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, nodeElements.First(n => n.Data.Equals(EdgeFromMapper(edge))), nodeElements.First(n => n.Data.Equals(EdgeToMapper(edge)))));
                Edges.Add(EdgeId(edge), edge);
            }
        }
        SVGEditor.MoveToBack();
        SVGEditor.SelectedShapes = copyOfSelectShapes;
        foreach (var newNodeElement in newNodeElementDictionary.Values)
        {
            if (newNodeElement.Edges.Count is 0)
            {
                newNodeElement.Cx = Random.Shared.NextDouble() * 20;
                newNodeElement.Cy = Random.Shared.NextDouble() * 20;
            }
            else if (newNodeElement.Edges.Count is 1)
            {
                var singleEdge = newNodeElement.Edges.Single();
                var neighborNode = singleEdge.From == newNodeElement ? singleEdge.To : singleEdge.From;
                var neighborsNeighbors = neighborNode.Edges.Select(e => e.From == neighborNode ? e.To : e.From).Where(n => n != newNodeElement).ToArray();

                var edgeLength = EdgeSpringLengthMapper(singleEdge.Data);

                if (neighborsNeighbors.Length is 0)
                {
                    var randomAngle = neighborNode.Cx + Random.Shared.NextDouble() * Math.PI;
                    newNodeElement.Cx = neighborNode.Cx + Math.Sin(randomAngle) * edgeLength;
                    newNodeElement.Cy = neighborNode.Cy + Math.Cos(randomAngle) * edgeLength;
                }
                else
                {
                    double averageXPositionOfNeighborsNeighbors = 0;
                    double averageYPositionOfNeighborsNeighbors = 0;

                    foreach (var neighborsNeighborNode in neighborsNeighbors)
                    {
                        averageXPositionOfNeighborsNeighbors += neighborsNeighborNode.Cx / neighborsNeighbors.Length;
                        averageYPositionOfNeighborsNeighbors += neighborsNeighborNode.Cy / neighborsNeighbors.Length;
                    }
                    // TODO: Handle the case where averagePositionOfNeighborsNeighbors==neighborsPosition;
                    var differenceBetweenAverageNeighborsNeighborsAndNeighbor = (
                        x: averageXPositionOfNeighborsNeighbors - neighborNode.Cx,
                        y: averageYPositionOfNeighborsNeighbors - neighborNode.Cy
                    );
                    var distanceBetweenAverageNeighborsNeighborsAndNeighbor = Math.Sqrt(Math.Pow(differenceBetweenAverageNeighborsNeighborsAndNeighbor.x, 2) + Math.Pow(differenceBetweenAverageNeighborsNeighborsAndNeighbor.y, 2));
                    var normalizedVectorBetweenAverageNeighborsNeighborsAndNeighbor = (
                        x: differenceBetweenAverageNeighborsNeighborsAndNeighbor.x / distanceBetweenAverageNeighborsNeighborsAndNeighbor,
                        y: differenceBetweenAverageNeighborsNeighborsAndNeighbor.y / distanceBetweenAverageNeighborsNeighborsAndNeighbor
                    );

                    newNodeElement.Cx = neighborNode.Cx - normalizedVectorBetweenAverageNeighborsNeighborsAndNeighbor.x * edgeLength;
                    newNodeElement.Cy = neighborNode.Cy - normalizedVectorBetweenAverageNeighborsNeighborsAndNeighbor.y * edgeLength;
                }
            }
            else
            {
                foreach (var edge in newNodeElement.Edges)
                {

                }
            }
        }

        await Task.Yield();
        StateHasChanged();
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
                if (node1.NeighborNodes.TryGetValue(node2, out var edge))
                {
                    force = EdgeSpringConstantMapper(edge.Data) * Math.Log(d / EdgeSpringLengthMapper(edge.Data));
                }
                else
                {
                    force = -(NodeRepulsionMapper(node1.Data) + NodeRepulsionMapper(node2.Data)) / 2 / (d * d);
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