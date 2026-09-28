using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    public GameObject mapCellPrefab;
    public Transform mapPanel;

    [Header("Room Icons (Optional)")]
    public Sprite startIcon;
    public Sprite monsterIcon;
    public Sprite campIcon;
    public Sprite treasureIcon;
    public Sprite eventIcon;
    public Sprite exitIcon;

    private const int MAP_SIZE = 5;
    [SerializeField] private int maxStartEndDistance = 8;
    private readonly GameObject[,] cells = new GameObject[MAP_SIZE, MAP_SIZE];
    private readonly ForestNode[,] nodes = new ForestNode[MAP_SIZE, MAP_SIZE];
    private readonly List<ForestNode> createdNodes = new List<ForestNode>();
    private ForestNode entranceConnection;
    private ForestNode deeperForestConnection;
    private bool generated;
    private bool gridCreated;
    private ForestExplorer forestExplorer;
    private ForestNode currentNode;

    public ForestNode EntranceConnection => entranceConnection;
    public ForestNode DeeperForestConnection => deeperForestConnection;
    public int RoomCount => createdNodes.Count;

    private void Start()
    {
        InitializeMap();
    }

    // ForestExplorer와 Start 실행 순서가 달라도 같은 맵을 사용합니다.
    public void InitializeMap()
    {
        if (!generated)
        {
            GenerateRooms();
            ConnectAllRooms();
            AddExtraConnections();
            FindConnectionPoints();
            ShortenStartEndPath();
            generated = true;
        }

        if (!gridCreated)
        {
            if (mapCellPrefab == null || mapPanel == null)
            {
                Debug.LogError("MapDisplay: Map Cell Prefab과 Map Panel을 연결하세요.", this);
                return;
            }
            if (!ValidatePrefab()) return;
            CreateGrid();
            gridCreated = true;
        }
        ShowMap();
    }

    public ForestNode GetEntranceConnection() => entranceConnection;
    public ForestNode GetDeeperForestConnection() => deeperForestConnection;

    public void SetExplorer(ForestExplorer explorer)
    {
        forestExplorer = explorer;
    }


    public ForestNode GetNode(int depth, int lane)
    {
        return IsInsideMap(depth, lane) ? nodes[depth, lane] : null;
    }

    private bool ValidatePrefab()
    {
        string[] names = { "RoomImage", "LineUp", "LineDown", "LineLeft", "LineRight", "QuestionMark" };
        foreach (string childName in names)
        {
            if (mapCellPrefab.transform.Find(childName) != null) continue;
            Debug.LogError("MapCell의 직접 자식이 필요합니다: " + childName, this);
            return false;
        }
        return true;
    }

    private void CreateGrid()
    {
        // 화면은 위에서 아래로 생성되므로 depth 4 → 0 순서로 배치합니다.
        // 결과적으로 Start(depth 0)는 화면 아래, Exit(depth 4)는 화면 위에 보입니다.
        for (int depth = MAP_SIZE - 1; depth >= 0; depth--)
        {
            for (int lane = 0; lane < MAP_SIZE; lane++)
            {
                GameObject cell = Instantiate(mapCellPrefab, mapPanel);
                cell.name = $"MapCell_{depth}_{lane}";
                cell.SetActive(true);
                cells[depth, lane] = cell;

                int capturedDepth = depth;
                int capturedLane = lane;

                Button button = cell.GetComponent<Button>();
                if (button == null)
                    button = cell.AddComponent<Button>();

                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(
                    () => OnMapCellClicked(capturedDepth, capturedLane));
            }
        }
    }

    private void GenerateRooms()
    {
        int targetRoomCount = Random.Range(15, 26);
        CreateNode(0, MAP_SIZE / 2);
        int maxDepth = 0;

        while (createdNodes.Count < targetRoomCount)
        {
            var candidates = new List<Vector2Int>();
            // 남은 방 수가 오른쪽 끝까지의 거리와 같으면 오른쪽으로 확장합니다.
            // 이 방식은 목표 방 수를 초과하지 않고 depth 4를 보장합니다.
            bool mustReachRight = targetRoomCount - createdNodes.Count == MAP_SIZE - 1 - maxDepth;
            foreach (ForestNode node in createdNodes)
            {
                if (mustReachRight)
                {
                    if (node.depth == maxDepth)
                        AddCandidate(candidates, node.depth + 1, node.lane);
                }
                else
                {
                    AddCandidate(candidates, node.depth + 1, node.lane);
                    AddCandidate(candidates, node.depth - 1, node.lane);
                    AddCandidate(candidates, node.depth, node.lane + 1);
                    AddCandidate(candidates, node.depth, node.lane - 1);
                }
            }
            Vector2Int position = candidates[Random.Range(0, candidates.Count)];
            CreateNode(position.x, position.y);
            if (position.x > maxDepth) maxDepth = position.x;
        }
    }

    private void AddCandidate(List<Vector2Int> candidates, int depth, int lane)
    {
        if (!IsInsideMap(depth, lane) || nodes[depth, lane] != null) return;
        Vector2Int position = new Vector2Int(depth, lane);
        if (!candidates.Contains(position)) candidates.Add(position);
    }

    private void CreateNode(int depth, int lane)
    {
        var node = new ForestNode
        {
            depth = depth,
            lane = lane,
            nodeName = $"얕은 숲 ({depth}, {lane})",
            description = "얕은 숲을 탐험하고 있다.",
            roomType = ForestRoomType.Event
        };
        nodes[depth, lane] = node;
        createdNodes.Add(node);
    }

    private List<ForestNode> GetAdjacentNodes(ForestNode node)
    {
        var result = new List<ForestNode>();
        AddAdjacent(result, node.depth + 1, node.lane);
        AddAdjacent(result, node.depth - 1, node.lane);
        AddAdjacent(result, node.depth, node.lane + 1);
        AddAdjacent(result, node.depth, node.lane - 1);
        return result;
    }

    private void AddAdjacent(List<ForestNode> result, int depth, int lane)
    {
        ForestNode node = GetNode(depth, lane);
        if (node != null) result.Add(node);
    }

    private void ConnectAllRooms()
    {
        // 무작위 깊이 우선 탐색으로 모든 방을 잇는 기본 트리를 만듭니다.
        var visited = new HashSet<ForestNode>();
        var stack = new Stack<ForestNode>();
        stack.Push(createdNodes[0]);
        visited.Add(createdNodes[0]);
        while (stack.Count > 0)
        {
            ForestNode current = stack.Peek();
            List<ForestNode> candidates = GetAdjacentNodes(current);
            candidates.RemoveAll(node => visited.Contains(node));
            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }
            ForestNode next = candidates[Random.Range(0, candidates.Count)];
            current.ConnectTo(next);
            visited.Add(next);
            stack.Push(next);
        }
    }

    private void AddExtraConnections()
    {
        foreach (ForestNode node in createdNodes)
        {
            // 각 인접 쌍을 한 번만 검사하므로 추가 연결 확률은 정확히 5%입니다.
            TryExtraConnection(node, GetNode(node.depth + 1, node.lane));
            TryExtraConnection(node, GetNode(node.depth, node.lane + 1));
        }
    }

    private void TryExtraConnection(ForestNode a, ForestNode b)
    {
        if (b != null && !a.connectedNodes.Contains(b) && Random.value < 0.05f)
            a.ConnectTo(b);
    }

    private void FindConnectionPoints()
    {
        // 실제 생성 시작점은 createdNodes[0]입니다.
        // GenerateRooms에서 depth 0, 가운데 lane으로 생성됩니다.
        entranceConnection = createdNodes[0];

        // EndPoint는 오른쪽 끝(depth 4)에 생성된 방 중 하나를 사용합니다.
        for (int lane = 0; lane < MAP_SIZE; lane++)
        {
            if (deeperForestConnection == null && nodes[MAP_SIZE - 1, lane] != null)
                deeperForestConnection = nodes[MAP_SIZE - 1, lane];
        }
        entranceConnection.isConnectionPoint = true;
        entranceConnection.nodeName = "숲 입구 연결점";
        entranceConnection.description = "얕은 숲의 연결점이다. 숲 입구로 돌아갈 수 있다.";
        deeperForestConnection.isConnectionPoint = true;
        deeperForestConnection.nodeName = "깊은 숲 연결점";
        deeperForestConnection.description = "더 깊은 숲으로 이어지는 길이다. 아직 다음 지역으로 이동할 수 없다.";

        AssignRoomTypes();
    }

    private void AssignRoomTypes()
    {
        foreach (ForestNode node in createdNodes)
        {
            if (node == entranceConnection)
            {
                node.roomType = ForestRoomType.Start;
                continue;
            }

            if (node == deeperForestConnection)
            {
                node.roomType = ForestRoomType.Exit;
                continue;
            }

            float roll = Random.value;
            if (roll < 0.55f)
                node.roomType = ForestRoomType.Monster;
            else if (roll < 0.70f)
                node.roomType = ForestRoomType.Camp;
            else if (roll < 0.85f)
                node.roomType = ForestRoomType.Treasure;
            else
                node.roomType = ForestRoomType.Event;
        }
    }

    // StartPoint와 EndPoint 사이 최단거리가 너무 길면 지름길을 추가합니다.
    private void ShortenStartEndPath()
    {
        if (entranceConnection == null || deeperForestConnection == null) return;

        int safety = 30;

        while (GetShortestDistance(entranceConnection, deeperForestConnection) > maxStartEndDistance
               && safety-- > 0)
        {
            ForestNode bestA = null;
            ForestNode bestB = null;
            int bestDistance = GetShortestDistance(entranceConnection, deeperForestConnection);

            foreach (ForestNode node in createdNodes)
            {
                foreach (ForestNode neighbor in GetAdjacentNodes(node))
                {
                    if (node.connectedNodes.Contains(neighbor)) continue;

                    node.ConnectTo(neighbor);
                    int newDistance = GetShortestDistance(entranceConnection, deeperForestConnection);

                    node.connectedNodes.Remove(neighbor);
                    neighbor.connectedNodes.Remove(node);

                    if (newDistance < bestDistance)
                    {
                        bestDistance = newDistance;
                        bestA = node;
                        bestB = neighbor;
                    }
                }
            }

            if (bestA == null) break;
            bestA.ConnectTo(bestB);
        }
    }

    private int GetShortestDistance(ForestNode start, ForestNode end)
    {
        var queue = new Queue<ForestNode>();
        var distances = new Dictionary<ForestNode, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            ForestNode current = queue.Dequeue();

            if (current == end)
                return distances[current];

            foreach (ForestNode next in current.connectedNodes)
            {
                if (!createdNodes.Contains(next) || distances.ContainsKey(next)) continue;

                distances[next] = distances[current] + 1;
                queue.Enqueue(next);
            }
        }

        return int.MaxValue;
    }

    public void ShowMap(ForestNode playerNode = null)
    {
        if (playerNode != null)
            currentNode = playerNode;

        if (!gridCreated) return;

        for (int depth = 0; depth < MAP_SIZE; depth++)
        {
            for (int lane = 0; lane < MAP_SIZE; lane++)
            {
                GameObject cell = cells[depth, lane];
                ForestNode node = nodes[depth, lane];

                bool visible = node != null && node.isDiscovered;
                bool isCurrent = node != null && node == currentNode;
                bool canMove = node != null &&
                               currentNode != null &&
                               currentNode.connectedNodes.Contains(node);

                // GridLayoutGroup 위치 유지를 위해 MapCell 루트는 끄지 않습니다.
                cell.SetActive(true);

                Transform roomImage = cell.transform.Find("RoomImage");
                roomImage.gameObject.SetActive(visible);

                SetOptionalQuestionMark(cell, node, visible);
                SetOptionalRoomIcon(cell, node, visible);
                SetOptionalVisitedOverlay(cell, node, visible, isCurrent);
                SetOptionalCurrentPoint(cell, visible && isCurrent);

                Button button = cell.GetComponent<Button>();
                button.interactable = visible && canMove;

                // 세로 진행 기준: depth가 증가할수록 화면 위쪽입니다.
                cell.transform.Find("LineUp").gameObject.SetActive(
                    ShouldShowLine(node, depth + 1, lane));
                cell.transform.Find("LineDown").gameObject.SetActive(
                    ShouldShowLine(node, depth - 1, lane));
                cell.transform.Find("LineLeft").gameObject.SetActive(
                    ShouldShowLine(node, depth, lane - 1));
                cell.transform.Find("LineRight").gameObject.SetActive(
                    ShouldShowLine(node, depth, lane + 1));
            }
        }
    }

    private void SetOptionalQuestionMark(GameObject cell, ForestNode node, bool visible)
    {
        Transform questionMark = cell.transform.Find("QuestionMark");
        if (questionMark == null) return;

        bool showQuestionMark = visible && node != null && !node.isVisited;
        questionMark.gameObject.SetActive(showQuestionMark);
    }

    private void SetOptionalRoomIcon(GameObject cell, ForestNode node, bool visible)
    {
        Transform iconTransform = cell.transform.Find("RoomIcon");
        if (iconTransform == null) return;

        Image icon = iconTransform.GetComponent<Image>();
        bool showIcon = visible && node != null && node.isVisited;

        iconTransform.gameObject.SetActive(showIcon);
        if (showIcon && icon != null)
            icon.sprite = GetRoomIcon(node.roomType);
    }

    private Sprite GetRoomIcon(ForestRoomType type)
    {
        switch (type)
        {
            case ForestRoomType.Start: return startIcon;
            case ForestRoomType.Monster: return monsterIcon;
            case ForestRoomType.Camp: return campIcon;
            case ForestRoomType.Treasure: return treasureIcon;
            case ForestRoomType.Exit: return exitIcon;
            default: return eventIcon;
        }
    }

    private void SetOptionalVisitedOverlay(
        GameObject cell, ForestNode node, bool visible, bool isCurrent)
    {
        Transform overlay = cell.transform.Find("VisitedOverlay");
        if (overlay == null) return;

        // 현재 방은 밝게, 떠난 방문 완료 방만 어둡게 표시합니다.
        overlay.gameObject.SetActive(
            visible && node != null && node.isVisited && !isCurrent);
    }

    private void SetOptionalCurrentPoint(GameObject cell, bool show)
    {
        Transform point = cell.transform.Find("CurrentPoint");
        if (point == null) return;
        point.gameObject.SetActive(show);
    }

    private void OnMapCellClicked(int depth, int lane)
    {
        if (forestExplorer == null || currentNode == null)
            return;

        ForestNode target = GetNode(depth, lane);

        if (target == null || !currentNode.connectedNodes.Contains(target))
            return;

        forestExplorer.MoveToNode(target);
    }

    private bool ShouldShowLine(ForestNode node, int depth, int lane)
    {
        if (!IsConnected(node, depth, lane))
            return false;

        ForestNode neighbor = GetNode(depth, lane);
        if (neighbor == null)
            return false;

        bool nodeKnown = IsNodeVisible(node);
        bool neighborKnown = IsNodeVisible(neighbor);

        return nodeKnown && neighborKnown;
    }

    private bool IsNodeVisible(ForestNode node)
    {
        return node != null && node.isDiscovered;
    }

    private bool IsConnected(ForestNode node, int depth, int lane)
    {
        ForestNode neighbor = GetNode(depth, lane);
        return node != null && neighbor != null && node.connectedNodes.Contains(neighbor);
    }

    private bool IsInsideMap(int depth, int lane)
    {
        return depth >= 0 && depth < MAP_SIZE && lane >= 0 && lane < MAP_SIZE;
    }
}
