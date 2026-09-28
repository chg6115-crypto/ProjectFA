using System.Collections.Generic;
using UnityEngine;

public enum ForestRoomType
{
    Start,
    Monster,
    Camp,
    Treasure,
    Event,
    Exit
}

public class ForestNode
{
    public int depth;
    public int lane;
    public string nodeName;
    public string description;
    public Sprite background;
    public bool isConnectionPoint;
    public bool isVisited;
    public bool isDiscovered;
    public ForestRoomType roomType;
    public List<ForestNode> connectedNodes = new List<ForestNode>();

    public void ConnectTo(ForestNode other)
    {
        if (other == null || other == this) return;
        if (!connectedNodes.Contains(other)) connectedNodes.Add(other);
        if (!other.connectedNodes.Contains(this)) other.connectedNodes.Add(this);
    }
}
