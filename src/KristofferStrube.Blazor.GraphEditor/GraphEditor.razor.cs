using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components;

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

    public bool IsReadyToLoad => SVGEditor.BBox is not null;

    protected Dictionary<string, TNode> Nodes { get; set; } = [];

    protected Dictionary<string, TEdge> Edges { get; set; } = [];

    public SVGEditor.SVGEditor SVGEditor { get; set; } = default!;

    protected string Input { get; set; } = "";

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
        if (SVGEditor.BBox is not null)
        {
            SVGEditor.Translate = (SVGEditor.BBox.Width / 2, SVGEditor.BBox.Height / 2);
        }
        else
        {
            SVGEditor.Translate = (200, 200);
        }

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
            Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, nodeElementDictionary[EdgeFromMapper(edge)], nodeElementDictionary[EdgeToMapper(edge)]);
        }

        SVGEditor.SelectedShapes = SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>).Select(e => (Shape)e).ToList();
        SVGEditor.MoveToBack();
        SVGEditor.ClearSelectedShapes();

        await Task.Yield();
        StateHasChanged();
        nodeElements = SVGEditor.Elements.Where(e => e is Node<TNode, TEdge>).Select(e => (Node<TNode, TEdge>)e).ToArray();
    }


    public async Task UpdateGraph(List<TNode> nodes, List<TEdge> edges)
    {
        Dictionary<TNode, Node<TNode, TEdge>> newNodeElementDictionary = [];

        HashSet<string> newSetOfNodes = new(nodes.Count);
        HashSet<string> newSetOfEdges = new(edges.Count);

        // Add new nodes
        foreach (TNode node in nodes)
        {
            string nodeKey = NodeIdMapper(node);
            if (!Nodes.ContainsKey(nodeKey))
            {
                Node<TNode, TEdge> element = Node<TNode, TEdge>.CreateNew(SVGEditor, this, node);
                newNodeElementDictionary.Add(node, element);
                Nodes.Add(nodeKey, node);
            }
            newSetOfNodes.Add(nodeKey);
        }

        // Add new edges
        foreach (TEdge edge in edges)
        {
            string edgeKey = EdgeId(edge);
            if (!Edges.ContainsKey(edgeKey))
            {
                TNode from = EdgeFromMapper(edge);
                TNode to = EdgeToMapper(edge);
                Node<TNode, TEdge> fromElement = newNodeElementDictionary.TryGetValue(from, out var eFrom) ? eFrom : nodeElements.First(n => n.Data.Equals(from));
                Node<TNode, TEdge> toElement = newNodeElementDictionary.TryGetValue(to, out var eTo) ? eTo : nodeElements.First(n => n.Data.Equals(to));
                Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, fromElement, toElement);
                Edges.Add(edgeKey, edge);
            }
            newSetOfEdges.Add(edgeKey);
        }

        // Remove old edges
        foreach (string edgeKey in Edges.Keys)
        {
            if (!newSetOfEdges.Contains(edgeKey))
            {
                Edge<TNode, TEdge> edgeToRemove = (Edge<TNode, TEdge>)SVGEditor.Elements.First(e => e is Edge<TNode, TEdge> edge && EdgeId(edge.Data) == edgeKey);
                SVGEditor.RemoveElement(edgeToRemove);
                Edges.Remove(edgeKey);
            }
        }

        // Remove old nodes
        foreach (string nodeKey in Nodes.Keys)
        {
            if (!newSetOfNodes.Contains(nodeKey))
            {
                Node<TNode, TEdge> nodeToRemove = (Node<TNode, TEdge>)SVGEditor.Elements.First(e => e is Node<TNode, TEdge> node && NodeIdMapper(node.Data) == nodeKey);
                SVGEditor.RemoveElement(nodeToRemove);
                Nodes.Remove(nodeKey);
            }
        }

        foreach (var newNodeElement in newNodeElementDictionary.Values)
        {
            if (newNodeElement.Edges.Count is 0)
            {
                newNodeElement.Cx = Random.Shared.NextDouble() * 20;
                newNodeElement.Cy = Random.Shared.NextDouble() * 20;
            }
            else
            {
                double newX = 0;
                double newY = 0;
                foreach (var neighborEdge in newNodeElement.Edges)
                {
                    var neighborMirroredPosition = MirroredAverageOfNeighborNodes(neighborEdge, newNodeElement);
                    newX += neighborMirroredPosition.x / newNodeElement.Edges.Count();
                    newY += neighborMirroredPosition.y / newNodeElement.Edges.Count();
                }
                newNodeElement.Cx = newX;
                newNodeElement.Cy = newY;
            }
            SVGEditor.AddElement(newNodeElement);
        }
        nodeElements = SVGEditor.Elements.Where(e => e is Node<TNode, TEdge>).Select(e => (Node<TNode, TEdge>)e).ToArray();

        foreach (Edge<TNode, TEdge> edge in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>).Cast<Edge<TNode, TEdge>>())
        {
            edge.UpdateLine();
        }
    }

    private (double x, double y) MirroredAverageOfNeighborNodes(Edge<TNode, TEdge> neighborEdge, Node<TNode, TEdge> rootNode)
    {
        var neighborNode = neighborEdge.From == rootNode ? neighborEdge.To : neighborEdge.From;
        var neighborsNeighbors = neighborNode.Edges.Select(e => e.From == neighborNode ? e.To : e.From).Where(n => n != rootNode).ToArray();

        var edgeLength = EdgeSpringLengthMapper(neighborEdge.Data);

        if (neighborsNeighbors.Length is 0)
        {
            var randomAngle = neighborNode.Cx + Random.Shared.NextDouble() * Math.PI;
            return (
                x: neighborNode.Cx + Math.Sin(randomAngle) * edgeLength,
                y: neighborNode.Cy + Math.Cos(randomAngle) * edgeLength
            );
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

            return (
                x: neighborNode.Cx - normalizedVectorBetweenAverageNeighborsNeighborsAndNeighbor.x * edgeLength,
                y: neighborNode.Cy - normalizedVectorBetweenAverageNeighborsNeighborsAndNeighbor.y * edgeLength
            );
        }
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

        foreach (Edge<TNode, TEdge> edge in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>).Cast<Edge<TNode, TEdge>>())
        {
            edge.UpdateLine();
        }
        return Task.CompletedTask;
    }

    public void MoveEdgesToBack()
    {
        var prevSelectedShapes = SVGEditor.SelectedShapes.ToList();
        SVGEditor.ClearSelectedShapes();
        foreach (Shape shape in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>))
        {
            SVGEditor.SelectShape(shape);
        }
        SVGEditor.MoveToBack();
        SVGEditor.SelectedShapes = prevSelectedShapes;
    }

    protected List<SupportedElement> SupportedElements { get; set; } =
    [
        new(typeof(Node<TNode, TEdge>), element => element.TagName is "CIRCLE" && element.GetAttribute("data-elementtype") == "node"),
        new(typeof(Edge<TNode, TEdge>), element => element.TagName is "LINE" && element.GetAttribute("data-elementtype") == "edge"),
    ];
}