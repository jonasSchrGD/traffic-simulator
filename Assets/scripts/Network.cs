using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class Network : MonoBehaviour
{
    float _SpawnInterval = 3;
    public float spawnInterval
    {
        get
        {
            return _SpawnInterval;
        }
    }

    bool _Simulate = false;
    public bool isSimulating
    {
        get
        {
            return _Simulate;
        }
    }
    public bool hasEnd
    {
        get
        {
            return _Ends.Count > 0;
        }
    }

    private List<Lane> _Links = new List<Lane>();
    private List<VehicleSpawner> _Spawners = new List<VehicleSpawner>();
    private List<VehicleEnd> _Ends = new List<VehicleEnd>();

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
    }
    public void RemoveLink(Lane link)
    {
        _Links.Remove(link);
    }
    public void AddSpawner(VehicleSpawner spawner)
    {
        if (_Spawners.IndexOf(spawner) == -1)
            _Spawners.Add(spawner);
    }
    public void RemoveSpawner(VehicleSpawner spawner)
    {
        _Spawners.Remove(spawner);
    }
    public void AddEnd(VehicleEnd end)
    {
        if (_Ends.IndexOf(end) == -1)
            _Ends.Add(end);
    }
    public void RemoveEnd(VehicleEnd end)
    {
        _Ends.Remove(end);
    }


    public List<Lane> CalculatePath(Node startPoint, Node EndPoint)
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
                if (openList.IndexOf(lane) == -1 && closedList.IndexOf(lane) == -1)
                {
                    lane.headConnection = currentLane;
                    lane.calculateCost(EndPoint);
                    openList.Add(lane);
                }
            }
        }

        List<Lane> path = new List<Lane>();
        if (currentLane == null)
            return null;

        while (currentLane._Nodes[0] != startPoint)
        {
            path.Insert(0, currentLane);
            currentLane = currentLane.headConnection;

            if (path.Count > 500)
                return null;//dirty fix to avoid infinite loop
        }
        path.Insert(0, currentLane);

        return path;
    }
    public List<Lane> CalculatePath(Node startPoint)
    {
        if (_Ends.Count > 0)
            return CalculatePath(startPoint, _Ends[Random.Range(0, _Ends.Count)].GetConnectedNode());
        else return null;
    }

    //UI functions
    public void Simulate(bool simulate)
    {
        _Simulate = simulate;
        if(!_Simulate)
        {
            for (int i = 0; i < _Links.Count; i++)
            {
                _Links[i].DestroyVehicles();
            }
        }
    }
    public void DrawDensity(bool drawFlow)
    {
        for (int i = 0; i < _Links.Count; i++)
        {
            if (_Links[i].Parent)
                _Links[i].Parent.drawFlow = drawFlow;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject.transform.root);
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
}

