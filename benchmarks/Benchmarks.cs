using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace benchmarks;

public class Benchmarks
{
    private Node[] nodeElements = Enumerable.Range(0, 1000)
        .Select(i => new Node(i.ToString(), Random.Shared.NextDouble() * 10, Random.Shared.NextDouble() * 10, Random.Shared.NextDouble() * 10))
        .ToArray();

    public Benchmarks()
    {
        for (int i = 0; i < nodeElements.Length; i++)
        {
            nodeElements[i % nodeElements.Length].NeighborNodes.Add(nodeElements[(i + 1) % nodeElements.Length], 1);
        }
    }

    [Benchmark]
    public void Standard()
    {
        foreach (var node1 in nodeElements)
        {
            double mx = 0;
            double my = 0;
            foreach (var node2 in nodeElements)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force;
                if (node1.NeighborNodes.TryGetValue(node2, out var edge))
                {
                    force = edge * Math.Log(d / edge);
                }
                else
                {
                    force = -(node1.Repulsion + node2.Repulsion) / 2 / (d * d);
                }

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }
    }

    [Benchmark]
    public void WithSpan()
    {
        Span<Node> nodes = nodeElements.AsSpan();
        foreach (var node1 in nodes)
        {
            double mx = 0;
            double my = 0;
            foreach (var node2 in nodes)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force;
                if (node1.NeighborNodes.TryGetValue(node2, out var edge))
                {
                    force = edge * Math.Log(d / edge);
                }
                else
                {
                    force = -(node1.Repulsion + node2.Repulsion) / 2 / (d * d);
                }

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }
    }

    [Benchmark]
    public void WithSpanAndNeigborsHandledSeparately()
    {
        Span<Node> nodes = nodeElements.AsSpan();
        foreach (var node1 in nodes)
        {
            double mx = 0;
            double my = 0;
            foreach (var node2 in nodes)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);

                double force = -(node1.Repulsion + node2.Repulsion) / 2 / (d * d);

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }
        foreach(var node1 in nodes)
        {
            double mx = 0;
            double my = 0;
            foreach (var (node2, edge) in node1.NeighborNodes)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force = edge * Math.Log(d / edge);

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }
    }

    [Benchmark]
    public void Experiment()
    {
        Span<Node> nodes = nodeElements.AsSpan();

        var sortedAccordingToX = nodeElements.OrderBy(n => n.Cx);
        var allLeft = sortedAccordingToX.Take(nodes.Length / 2).ToArray();
        var allRigth = sortedAccordingToX.Skip(nodes.Length / 2).ToArray();

        double allLeftSumX = 0;
        double allLeftSumY = 0;
        double allLeftSumRepulsion = 0;

        foreach (var node1 in allLeft)
        {
            allLeftSumX += node1.Cx;
            allLeftSumY += node1.Cy;
            allLeftSumRepulsion += node1.Repulsion;

            double mx = 0;
            double my = 0;
            foreach (var node2 in allLeft)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force = -(node1.Repulsion + node2.Repulsion) / 2 / (d * d);

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }

        double allRightSumX = 0;
        double allRightSumY = 0;
        double allRightSumRepulsion = 0;

        foreach (var node1 in allRigth)
        {
            allRightSumX += node1.Cx;
            allRightSumY += node1.Cy;
            allRightSumRepulsion += node1.Repulsion;

            double mx = 0;
            double my = 0;
            foreach (var node2 in allRigth)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force = -(node1.Repulsion + node2.Repulsion) / 2 / (d * d);

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }

        foreach(var node in allRigth)
        {
            double dx = node.Cx - allLeftSumX / (nodes.Length / 2);
            double dy = node.Cy - allLeftSumY / (nodes.Length / 2);
            double d = Math.Sqrt(dx * dx + dy * dy);
            double force = -(node.Repulsion + allLeftSumRepulsion / (nodes.Length / 2)) / 2 / (d * d);

            node.Cx -= dx * 0.1 * force;
            node.Cy -= dy * 0.1 * force;
        }

        foreach (var node in allLeft)
        {
            double dx = node.Cx - allRightSumX / (nodes.Length / 2);
            double dy = node.Cy - allRightSumY / (nodes.Length / 2);
            double d = Math.Sqrt(dx * dx + dy * dy);
            double force = -(node.Repulsion + allRightSumRepulsion / (nodes.Length / 2)) / 2 / (d * d);

            node.Cx -= dx * 0.1 * force;
            node.Cy -= dy * 0.1 * force;
        }

        foreach (var node1 in nodes)
        {
            double mx = 0;
            double my = 0;
            foreach (var (node2, edge) in node1.NeighborNodes)
            {
                if (node1 == node2)
                {
                    continue;
                }

                double dx = node1.Cx - node2.Cx;
                double dy = node1.Cy - node2.Cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double force = edge * Math.Log(d / edge);

                mx -= dx * 0.1 * force;
                my -= dy * 0.1 * force;
            }

            node1.Cx += mx;
            node1.Cy += my;
        }
    }

    private class Node(string id, double cx, double cy, double repulsion)
    {
        public double Cx { get; set; } = cx;
        public double Cy { get; set; } = cy;
        public double Repulsion => repulsion;
        public Dictionary<Node, double> NeighborNodes { get; set; } = [];
    };
}
