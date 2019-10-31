using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 position
    {
        get
        {
            return _Position;
        }
        set
        {
            _Position = value;
            for (int i = 0; i < _ConnectedLanes.Count; i++)
            {
                _ConnectedLanes[i].UpdateNodes(!isEndNode);
            }
        }
    }
    private Vector2 _Position = Vector2.zero;
    public bool isEndNode = false;
    public EndPoint parent;

    public List<Lane> connectedLanes
    {
        get
        {
            return _ConnectedLanes;
        }
    }
    [SerializeField]
    private List<Lane> _ConnectedLanes = new List<Lane>();

    public void AddLink(Lane lane, int idx)
    {
        _ConnectedLanes.Add(lane);
        lane._Nodes[idx] = this;
    }
}
