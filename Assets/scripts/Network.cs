using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class Network : MonoBehaviour
{
    float _Delta = 0;
    float _SpawnInterval = 0.5f;
    bool _Simulate = false;
    [SerializeField]
    Slider _Slider;
    [SerializeField]
    Text _SpawnIntervalText;
    [SerializeField]
    Toggle _FlowToggle;

    private List<Lane> _Links = new List<Lane>();

    [SerializeField]
    private GameObject _VehicleBP = null;

    static public Network instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<Network>();

                if (_Instance == null)
                {
                    GameObject go = new GameObject("network");
                    _Instance = go.AddComponent<Network>();
                }
            }
            return _Instance;
        }
    }
    static private Network _Instance = null;

    public void AddLink(Lane link)
    {
        if (_Links.IndexOf(link) == -1)
            _Links.Add(link);
        else
            Debug.LogError("link already added");
    }

    public void RemoveLink(Lane link)
    {
        if (_Links.IndexOf(link) != -1)
            _Links.Remove(link);
    }

    public List<Node> CalculatePath(Node startPoint, Node EndPoint)
    {
        //based on A* from gameplay programming
        List<Lane> openList = new List<Lane>();
        List<Lane> closedList = new List<Lane>();

        foreach (var lane in startPoint.connectedLanes)
        {
            lane.headConnection = null;
            lane.calculateCost(EndPoint);
            openList.Add(lane);
        }

        Lane currentLane = null;
        while (openList.Count > 0)
        {
            currentLane = openList[0];
            foreach (var lane in openList)
            {
                if (lane.fCost < currentLane.fCost)
                {
                    currentLane = lane;
                }
            }

            openList.Remove(currentLane);
            closedList.Add(currentLane);

            if (currentLane._Nodes[1] == EndPoint)
            {
                openList.Clear();
                break;
            }

            List<Lane> Connections = currentLane._Nodes[1].connectedLanes;
            bool breakWhile = false;
            foreach (var lane in Connections)
            {
                if (lane._Nodes[1] == EndPoint)
                {
                    lane.headConnection = currentLane;
                    currentLane = lane;
                    openList.Clear();
                    breakWhile = true;
                }
            }
            if (breakWhile)
                break;

            foreach (var lane in Connections)
            {
                if (openList.IndexOf(lane) == -1 && openList.IndexOf(lane) == -1)
                {
                    lane.headConnection = currentLane;
                    lane.calculateCost(EndPoint);
                    openList.Add(lane);
                }
            }
        }

        List<Node> path = new List<Node>();
        if (currentLane == null)
            return null;

        path.Add(currentLane._Nodes[1]);
        while (currentLane._Nodes[0] != startPoint)
        {
            path.Insert(0, currentLane._Nodes[0]);
            currentLane = currentLane.headConnection;

            if (path.Count > 500)
                return null;//dirty fix to avoid infinite loop
        }
        path.Insert(0, currentLane._Nodes[0]);

        return path;
    }

    public bool isSimulating
    {
        get
        {
            return _Simulate;
        }
    }
    private void Update()
    {
        if (_Simulate)
            _Delta += Time.deltaTime;

        if (_SpawnInterval < _Delta)
        {
            SpawnVehicle();
            _Delta -= _SpawnInterval;
        }
    }
    private void SpawnVehicle()
    {
        if (!_VehicleBP || _Links.Count == 0)
            return;

        Node startNode = _Links[Random.Range(0, _Links.Count - 1)]._Nodes[0];
        while (startNode.gameObject.tag != "Road")
            startNode = _Links[Random.Range(0, _Links.Count - 1)]._Nodes[0];

        Node endNode = GetRandomEnd(startNode);

        List<Node> path = CalculatePath(startNode, endNode);
        if (path != null)
        {
            GameObject go = Instantiate(_VehicleBP);
            go.GetComponent<Vehicle>().path = path;
        }
    }
    private Node GetRandomEnd(Node beginNode)
    {
        Node endNode = beginNode;
        int nrOfRand = 0;

        while (Random.Range(0, _Links.Count) > nrOfRand || endNode == beginNode)
        {
            if (endNode.connectedLanes.Count > 0)
                endNode = endNode.connectedLanes[Random.Range(0, endNode.connectedLanes.Count - 1)]._Nodes[1];
            ++nrOfRand;
        }
        return endNode;
    }

    //debug purpose
    private void OnDrawGizmos()
    {
        foreach (Lane link in _Links)
        {
            Gizmos.color = Color.green;
            link.DrawGizmos();
        }
    }

    //UI functions
    public void Simulate()
    {
        _Simulate = !_Simulate;
    }
    public void DrawDensity()
    {
        for (int i = 0; i < _Links.Count; i++)
        {
            if (_Links[i].Parent)
                _Links[i].Parent.drawFlow = _FlowToggle.isOn;
        }
    }
    public void UpdateSpawnInterval()
    {
        _SpawnInterval = _Slider.value;
        _SpawnIntervalText.text = "spawn interval\n" + _Slider.value;
    }
}

