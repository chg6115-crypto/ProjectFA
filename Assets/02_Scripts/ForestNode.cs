using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ForestNode
{
    public int depth;
    public int lane;

    public string nodeName;
    public string description;
    public Sprite background;

    public List<ForestNode> connectedNodes = new List<ForestNode>();
}