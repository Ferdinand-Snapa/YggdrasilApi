using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogic;

public class RunGraph
{
    // Executes the provided graph. graphInput maps graph-level input port ids to values.
    // Returns a dictionary of produced values keyed by "{nodeId}:{portId}" for data outputs.
    public async Task<Dictionary<string, object?>> RunAsync(
        Graph graph,
        Dictionary<string, object?>? graphInput = null,
        string? flowInputPortId = null,
        int charId = -1,
        GameSession? session = null)
    {
        ArgumentNullException.ThrowIfNull(graph, "Run graph: graph");

        // local cache for produced data values: "nodeId:portId" -> value
        // POTENTIAL CHNAGE: make two different local cache for evaluated resulat and executed results
        var localCache = new Dictionary<string, object?>();

        // helper to form cache key
        static string CacheKey(string nodeId, string portId) => nodeId + ":" + portId;

        // index nodes by id
        var nodesById = graph.Nodes.ToDictionary(n => n.Id);

        // TODO: fix a solution for graphs to be evaluated
        if (flowInputPortId == null) return localCache;


        Node startNode = graph.Nodes.FirstOrDefault(n => n.Id == flowInputPortId)!;
        // Queue carries (node, triggeredPortId): portId is the flow input port that activated the node,
        // or null for entry nodes that have no incoming flow connection.
        var queue = new Queue<(Node node, string? portId)>([(startNode, null)]);


        // main execution loop
        while (queue.Count > 0)
        {
            var (node, triggeredPortId) = queue.Dequeue();

            ArgumentNullException.ThrowIfNull(node.Definition, "Run graph: node " + node.Id + " missing definition");

            // load persisted values into runtime dictionary
            node.LoadValues();

            // build inputs for this node
            var inputs = new Dictionary<string, object?>();

            var inputConnections = graph.Connections.Where(c => c.ToNodeId == node.Id).ToList();

            foreach (var conn in inputConnections)
            {
                //null connection type is flow input
                if (conn.ConnectionType == null)
                {
                    //every flow input except the triggered one is set to false
                    inputs[conn.ToPortId] = conn.ToPortId == triggeredPortId;
                    continue;
                }
                //if the value allready exist within the cache
                if (localCache.TryGetValue(CacheKey(conn.FromNodeId, conn.FromPortId), out var valueFromCache))
                {
                    inputs[conn.ToPortId] = valueFromCache;
                    continue;
                }

                var producerResult = await EvaluateNodeAsync(graph, conn.FromNodeId, graphInput, localCache, charId, session);
                if (producerResult == null) continue;
                foreach (var (key, producedValue) in producerResult.DataOutputs)
                {
                    localCache[CacheKey(conn.FromNodeId, key)] = producedValue;
                }
                if (!producerResult.DataOutputs.TryGetValue(conn.FromPortId, out var value)) continue;
                inputs[conn.ToPortId] = value;
            }

            // execute or evaluate this node
            NodeDefinition.NodeExecutionResult? result = null;

            // if can evaluate
            if (node.Definition.Evaluate != null)
                result = await node.Definition.Evaluate(inputs, node.Values, charId, session);

            // if has execute
            else if (node.Definition.Execute != null)
                result = await node.Definition.Execute(inputs, node.Values, charId, session);

            // fault with node definition
            else
                throw new Exception("Node definition has neither Evaluate nor Execute for node id: " + node.Id);

            if (result == null) continue;

            // store data outputs into cache keyed by nodeId:portId
            foreach (var kv in result.DataOutputs)
            {
                // find the output port id by name
                if (node.Definition.OutputPorts.FirstOrDefault(p => p.Name == kv.Key) is not DataPort outPort)
                    continue; // or throw

                var key = CacheKey(node.Id, outPort.PortId);
                localCache[key] = kv.Value;
            }

            // enqueue downstream nodes for each flow output port id
            foreach (var flowPortId in result.FlowOutputs)
            {
                // Node recalls itself
                if (node.Definition.InputPorts.Where(p => p.PortId == flowPortId).Any())
                {
                    queue.Enqueue((node, flowPortId));
                    continue;
                }

                var downstreamConn = graph.Connections.FirstOrDefault(c => c.FromNodeId == node.Id && c.FromPortId == flowPortId);

                // no further connection
                if (downstreamConn == null)
                    continue;

                var nextNode = nodesById.GetValueOrDefault(downstreamConn.ToNodeId);
                if (nextNode != null)
                {
                    // avoid duplicate enqueueing of the exact same (node, portId) pair
                    if (!queue.Any(q => q.node == nextNode && q.portId == downstreamConn.ToPortId))
                        queue.Enqueue((nextNode, downstreamConn.ToPortId));
                }

            }
        }

        return localCache;
    }

    // Evaluate a node (and recursively its dependencies) and populate localCache with produced data outputs.
    private async Task<NodeDefinition.NodeExecutionResult?> EvaluateNodeAsync(
        Graph graph,
        string nodeId,
        Dictionary<string, object?>? graphInput,
        Dictionary<string, object?> localCache,
        int charId = -1,
        GameSession? session = null)
    {
        var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return null;

        // load persisted values into runtime dictionary
        node.LoadValues();

        if (node.Definition == null) return null;

        var inputs = new Dictionary<string, object?>();
        // helper to form cache key
        static string CacheKey(string nodeId, string portId) => nodeId + ":" + portId;

        var inputConnections = graph.Connections.Where(c => c.ToNodeId == node.Id).ToList();

        foreach (var conn in inputConnections)
        {
            //evaluate Nodes should not have flow ports
            if (conn.ConnectionType == null) continue;

            //if the value allready exist within the cache
            if (localCache.TryGetValue(CacheKey(conn.FromNodeId, conn.FromPortId), out var valueFromCache))
            {
                inputs[conn.ToPortId] = valueFromCache;
                continue;
            }
            // recursive call for value
            var evaluateResult = await EvaluateNodeAsync(graph, conn.FromNodeId, graphInput, localCache, charId, session);
            if (evaluateResult == null) continue;

            if (!evaluateResult.DataOutputs.TryGetValue(conn.FromPortId, out var value)) continue;
            inputs[conn.ToPortId] = value;
        }

        var result = await node.Definition.Evaluate(inputs, localCache, charId, session);
        // stores the result data outputs in the local cache
        foreach (var (key, producedValue) in result!.DataOutputs)
        {
            localCache[CacheKey(nodeId, key)] = producedValue;
        }
        return result;
    }
}
