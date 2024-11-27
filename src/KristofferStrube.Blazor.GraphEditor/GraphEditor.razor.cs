using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components;

namespace KristofferStrube.Blazor.GraphEditor;

/// <summary>
/// An editor for graphs consisting of nodes and edges.
/// </summary>
/// <typeparam name="TNode">The type that will represent the nodes in graph.</typeparam>
/// <typeparam name="TEdge">The type that will represent the connections between the nodes in the graph.</typeparam>
public partial class GraphEditor<TNode, TEdge> : ComponentBase where TNode : IEquatable<TNode>
{
    private GraphEditorCallbackContext callbackContext = default!;
    private Node<TNode, TEdge>[] nodeElements = [];
    private string EdgeId(TEdge e)
    {
        return NodeIdMapper(EdgeFromMapper(e)) + "-" + NodeIdMapper(EdgeToMapper(e));
    }

    /// <summary>
    /// Maps each node to an unique id.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TNode, string> NodeIdMapper { get; set; }

    /// <summary>
    /// Defaults to <c>"#66BB6A"</c>.
    /// </summary>
    [Parameter]
    public Func<TNode, string> NodeColorMapper { get; set; } = _ => "#66BB6A";

    /// <summary>
    /// Defaults to <c>50</c>.
    /// </summary>
    [Parameter]
    public Func<TNode, double> NodeRadiusMapper { get; set; } = _ => 50;

    /// <summary>
    /// Defaults to <c>800</c>.
    /// </summary>
    [Parameter]
    public Func<TNode, double> NodeRepulsionMapper { get; set; } = _ => 800;

    /// <summary>
    /// Defaults to <see langword="null"/>.
    /// </summary>
    [Parameter]
    public Func<TNode, string?> NodeImageMapper { get; set; } = _ => null;

    /// <summary>
    /// Maps each edge to which node it goes from.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TEdge, TNode> EdgeFromMapper { get; set; }

    /// <summary>
    /// Maps each edge to which node it goes to.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TEdge, TNode> EdgeToMapper { get; set; }

    /// <summary>
    /// Defaults to <c>1</c>.
    /// </summary>
    [Parameter]
    public Func<TEdge, double> EdgeWidthMapper { get; set; } = _ => 1;

    /// <summary>
    /// Defaults to <c>1</c>.
    /// </summary>
    [Parameter]
    public Func<TEdge, double> EdgeSpringConstantMapper { get; set; } = _ => 1;

    /// <summary>
    /// Defaults to <c>200</c>.
    /// </summary>
    [Parameter]
    public Func<TEdge, double> EdgeSpringLengthMapper { get; set; } = _ => 200;

    /// <summary>
    /// Defaults to <c>"#000000"</c>.
    /// </summary>
    [Parameter]
    public Func<TEdge, string> EdgeColorMapper { get; set; } = _ => "#000000";

    /// <summary>
    /// Defaults to <see langword="true"/>
    /// </summary>
    [Parameter]
    public Func<TEdge, bool> EdgeShowsArrow { get; set; } = _ => true;

    /// <summary>
    /// Callback that will be invoked when a specific node is selected by it getting focus.
    /// </summary>
    [Parameter]
    public Func<TNode, Task>? NodeSelectionCallback { get; set; }

    /// <summary>
    /// Whether the underlying <see cref="SVGEditor.SVGEditor"/> has rendered.
    /// </summary>
    public bool IsReadyToLoad => SVGEditor.BBox is not null;

    /// <summary>
    /// The nodes of the graph.
    /// </summary>
    protected Dictionary<string, TNode> Nodes { get; set; } = [];

    /// <summary>
    /// The edges of the graph.
    /// </summary>
    protected Dictionary<string, TEdge> Edges { get; set; } = [];

    /// <summary>
    /// The underlying <see cref="SVGEditor.SVGEditor"/>.
    /// </summary>
    public SVGEditor.SVGEditor SVGEditor { get; set; } = default!;

    /// <summary>
    /// A text representation of the graph.
    /// </summary>
    protected string Input { get; set; } = "";

    /// <inheritdoc/>
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

    /// <summary>
    /// Loads a graph of nodes and edges.
    /// </summary>
    /// <param name="nodes">The nodes of the graph.</param>
    /// <param name="edges">The edges that are present between the given <paramref name="nodes"/>.</param>
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

    /// <summary>
    /// Updates the nodes and edges that are in the graph without clearing the existing ones.
    /// </summary>
    /// <param name="nodes">The nodes of the graph.</param>
    /// <param name="edges">The edges that are present between the given <paramref name="nodes"/>.</param>
    public async Task UpdateGraph(List<TNode> nodes, List<TEdge> edges)
    {
        Dictionary<TNode, Node<TNode, TEdge>> newNodeElementDictionary = [];

        HashSet<string> newSetOfNodes = new(nodes.Count);
        HashSet<string> newSetOfEdges = new(edges.Count);

        // Add new nodes
        foreach (TNode node in nodes)
        {
            string nodeKey = NodeIdMapper(node);
            if (Nodes.TryAdd(nodeKey, node))
            {
                Node<TNode, TEdge> element = Node<TNode, TEdge>.CreateNew(SVGEditor, this, node);
                newNodeElementDictionary.Add(node, element);
            }
            newSetOfNodes.Add(nodeKey);
        }

        // Add new edges
        foreach (TEdge edge in edges)
        {
            string edgeKey = EdgeId(edge);
            if (Edges.TryAdd(edgeKey, edge))
            {
                TNode from = EdgeFromMapper(edge);
                TNode to = EdgeToMapper(edge);
                Node<TNode, TEdge> fromElement = newNodeElementDictionary.TryGetValue(from, out var eFrom) ? eFrom : nodeElements.First(n => n.Data.Equals(from));
                Node<TNode, TEdge> toElement = newNodeElementDictionary.TryGetValue(to, out var eTo) ? eTo : nodeElements.First(n => n.Data.Equals(to));
                Edge<TNode, TEdge>.AddNew(SVGEditor, this, edge, fromElement, toElement);
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
                    var (x, y) = MirroredAverageOfNeighborNodes(neighborEdge, newNodeElement);
                    newX += x / newNodeElement.Edges.Count;
                    newY += y / newNodeElement.Edges.Count;
                }
                newNodeElement.Cx = newX;
                newNodeElement.Cy = newY;
            }
            SVGEditor.AddElement(newNodeElement);
        }

        await Task.Yield();
        StateHasChanged();
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
            var (x, y) = (
                differenceBetweenAverageNeighborsNeighborsAndNeighbor.x / distanceBetweenAverageNeighborsNeighborsAndNeighbor,
                differenceBetweenAverageNeighborsNeighborsAndNeighbor.y / distanceBetweenAverageNeighborsNeighborsAndNeighbor
            );

            return (
                x: neighborNode.Cx - x * edgeLength,
                y: neighborNode.Cy - y * edgeLength
            );
        }
    }

    /// <summary>
    /// Updates the layout of the nodes so that they repulse each other while staying close to the ones that they are connected to via edges.
    /// </summary>
    public Task ForceDirectedLayout()
    {
        Span<Node<TNode, TEdge>> nodes = nodeElements.AsSpan();
        foreach (var node1 in nodes)
        {
            double mx = 0;
            double my = 0;
            foreach (var node2 in nodes)
            {
                if (node1 == node2 || node1.NeighborNodes.ContainsKey(NodeIdMapper(node2.Data)))
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force = -(NodeRepulsionMapper(node1.Data) + NodeRepulsionMapper(node2.Data)) / 2 / (d * d);

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
            double dx = edge.From.Cx - edge.To.Cx;
            double dy = edge.From.Cy - edge.To.Cy;
            double d = Math.Sqrt(dx * dx + dy * dy);
            double force = EdgeSpringConstantMapper(edge.Data) * Math.Log(d / EdgeSpringLengthMapper(edge.Data));

            double mx = dx * 0.1 * force;
            double my = dy * 0.1 * force;

            if (!SVGEditor.SelectedShapes.Contains(edge.From))
            {
                edge.From.Cx -= mx;
                edge.From.Cy -= my;
            }
            if (!SVGEditor.SelectedShapes.Contains(edge.To))
            {
                edge.To.Cx += mx;
                edge.To.Cy += my;
            }
        }
        foreach (Edge<TNode, TEdge> edge in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>).Cast<Edge<TNode, TEdge>>())
        {
            edge.UpdateLine();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Move all edges to the back so that they are hidden if any nodes are displayed in the same position.
    /// </summary>
    public void MoveEdgesToBack()
    {
        var prevSelectedShapes = SVGEditor.SelectedShapes.ToList();
        SVGEditor.ClearSelectedShapes();
        foreach (Shape shape in SVGEditor.Elements.Where(e => e is Edge<TNode, TEdge>).Cast<Shape>())
        {
            SVGEditor.SelectShape(shape);
        }
        SVGEditor.MoveToBack();
        SVGEditor.SelectedShapes = prevSelectedShapes;
    }

    /// <summary>
    /// The elements that can be rendered in this graph. This normally doesn't need adjustments as the graph defaults to having support for the nodes and edges it shows.
    /// </summary>
    protected List<SupportedElement> SupportedElements { get; set; } =
    [
        new(typeof(Node<TNode, TEdge>), element => element.TagName is "CIRCLE" && element.GetAttribute("data-elementtype") == "node"),
        new(typeof(Edge<TNode, TEdge>), element => element.TagName is "LINE" && element.GetAttribute("data-elementtype") == "edge"),
    ];
}