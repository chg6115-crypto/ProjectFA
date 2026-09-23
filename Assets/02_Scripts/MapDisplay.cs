using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public GameObject mapCellPrefab;
    public Transform mapPanel;

    private const int MAP_SIZE = 5;

    private GameObject[,] cells = new GameObject[MAP_SIZE, MAP_SIZE];
    private ForestNode[,] nodes = new ForestNode[MAP_SIZE, MAP_SIZE];

    private ForestNode startNode;
    private List<ForestNode> exitNodes = new List<ForestNode>();

    private void Start()
    {
        CreateGrid();
        GenerateRooms();
        ConnectAllRooms();
        AddExtraConnections();
        FindStartAndExits();
        ShowMap();
    }

    private void CreateGrid()
    {
        for (int lane = MAP_SIZE - 1; lane >= 0; lane--)
        {
            for (int depth = 0; depth < MAP_SIZE; depth++)
            {
                GameObject cell = Instantiate(mapCellPrefab, mapPanel);
                cells[depth, lane] = cell;
            }
        }
    }

    private void GenerateRooms()
    {
        int targetRoomCount = Random.Range(15, 26);
        int startLane = Random.Range(0, MAP_SIZE);

        CreateNode(0, startLane);

        List<ForestNode> createdNodes = new List<ForestNode>();
        createdNodes.Add(nodes[0, startLane]);

        while (createdNodes.Count < targetRoomCount)
        {
            ForestNode baseNode =
                createdNodes[Random.Range(0, createdNodes.Count)];

            Vector2Int direction = GetRandomDirection();

            int newDepth = baseNode.depth + direction.x;
            int newLane = baseNode.lane + direction.y;

            if (!IsInsideMap(newDepth, newLane))
                continue;

            if (nodes[newDepth, newLane] != null)
                continue;

            CreateNode(newDepth, newLane);
            createdNodes.Add(nodes[newDepth, newLane]);
        }

        EnsureLastDepthExists(createdNodes);
    }

    private void CreateNode(int depth, int lane)
    {
        ForestNode node = new ForestNode();

        node.depth = depth;
        node.lane = lane;
        node.nodeName = $"숲 ({depth}, {lane})";
        node.description = "마수의 숲을 탐험하고 있다.";

        nodes[depth, lane] = node;
    }

    private Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        return directions[Random.Range(0, directions.Length)];
    }

    private void EnsureLastDepthExists(List<ForestNode> createdNodes)
    {
        bool hasExit = false;

        for (int lane = 0; lane < MAP_SIZE; lane++)
        {
            if (nodes[MAP_SIZE - 1, lane] != null)
            {
                hasExit = true;
                break;
            }
        }

        if (hasExit)
            return;

        ForestNode closest = createdNodes[0];

        foreach (ForestNode node in createdNodes)
        {
            if (node.depth > closest.depth)
                closest = node;
        }

        int exitLane = closest.lane;

        for (int depth = closest.depth + 1; depth < MAP_SIZE; depth++)
        {
            if (nodes[depth, exitLane] == null)
                CreateNode(depth, exitLane);
        }
    }

    private void ConnectAllRooms()
    {
        HashSet<ForestNode> connected = new HashSet<ForestNode>();

        startNode = FindFirstNodeAtDepth(0);
        connected.Add(startNode);

        while (true)
        {
            List<(ForestNode from, ForestNode to)> candidates =
                new List<(ForestNode, ForestNode)>();

            foreach (ForestNode current in connected)
            {
                AddConnectionCandidates(current, connected, candidates);
            }

            if (candidates.Count == 0)
                break;

            var selected =
                candidates[Random.Range(0, candidates.Count)];

            ConnectTwoNodes(selected.from, selected.to);
            connected.Add(selected.to);
        }
    }

    private void AddConnectionCandidates(
        ForestNode current,
        HashSet<ForestNode> connected,
        List<(ForestNode, ForestNode)> candidates)
    {
        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        foreach (Vector2Int direction in directions)
        {
            int depth = current.depth + direction.x;
            int lane = current.lane + direction.y;

            if (!IsInsideMap(depth, lane))
                continue;

            ForestNode neighbor = nodes[depth, lane];

            if (neighbor != null && !connected.Contains(neighbor))
            {
                candidates.Add((current, neighbor));
            }
        }
    }

    private void AddExtraConnections()
    {
        for (int depth = 0; depth < MAP_SIZE; depth++)
        {
            for (int lane = 0; lane < MAP_SIZE; lane++)
            {
                ForestNode current = nodes[depth, lane];

                if (current == null)
                    continue;

                TryAddExtraConnection(current, depth + 1, lane);
                TryAddExtraConnection(current, depth, lane + 1);
            }
        }
    }

    private void TryAddExtraConnection(
        ForestNode current,
        int depth,
        int lane)
    {
        if (!IsInsideMap(depth, lane))
            return;

        ForestNode neighbor = nodes[depth, lane];

        if (neighbor == null)
            return;

        if (current.connectedNodes.Contains(neighbor))
            return;

        if (Random.value < 0.25f)
        {
            ConnectTwoNodes(current, neighbor);
        }
    }

    private void ConnectTwoNodes(ForestNode a, ForestNode b)
    {
        if (!a.connectedNodes.Contains(b))
            a.connectedNodes.Add(b);

        if (!b.connectedNodes.Contains(a))
            b.connectedNodes.Add(a);
    }

    private void FindStartAndExits()
    {
        startNode = FindFirstNodeAtDepth(0);
        exitNodes.Clear();

        for (int lane = 0; lane < MAP_SIZE; lane++)
        {
            ForestNode node = nodes[MAP_SIZE - 1, lane];

            if (node != null)
                exitNodes.Add(node);
        }
    }

    private ForestNode FindFirstNodeAtDepth(int depth)
    {
        for (int lane = 0; lane < MAP_SIZE; lane++)
        {
            if (nodes[depth, lane] != null)
                return nodes[depth, lane];
        }

        return null;
    }

    private void ShowMap()
    {
        for (int depth = 0; depth < MAP_SIZE; depth++)
        {
            for (int lane = 0; lane < MAP_SIZE; lane++)
            {
                GameObject cell = cells[depth, lane];
                ForestNode node = nodes[depth, lane];

                Transform room = cell.transform.Find("RoomImage");
                Transform up = cell.transform.Find("LineUp");
                Transform down = cell.transform.Find("LineDown");
                Transform left = cell.transform.Find("LineLeft");
                Transform right = cell.transform.Find("LineRight");

                room.gameObject.SetActive(node != null);

                if (node == null)
                {
                    up.gameObject.SetActive(false);
                    down.gameObject.SetActive(false);
                    left.gameObject.SetActive(false);
                    right.gameObject.SetActive(false);
                    continue;
                }

                up.gameObject.SetActive(
                    IsConnected(node, depth, lane + 1));

                down.gameObject.SetActive(
                    IsConnected(node, depth, lane - 1));

                left.gameObject.SetActive(
                    IsConnected(node, depth - 1, lane));

                right.gameObject.SetActive(
                    IsConnected(node, depth + 1, lane));
            }
        }
    }

    private bool IsConnected(
        ForestNode current,
        int depth,
        int lane)
    {
        if (!IsInsideMap(depth, lane))
            return false;

        ForestNode neighbor = nodes[depth, lane];

        if (neighbor == null)
            return false;

        return current.connectedNodes.Contains(neighbor);
    }

    private bool IsInsideMap(int depth, int lane)
    {
        return depth >= 0 &&
               depth < MAP_SIZE &&
               lane >= 0 &&
               lane < MAP_SIZE;
    }
}