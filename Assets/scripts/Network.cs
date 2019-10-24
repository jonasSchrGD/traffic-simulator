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

    [SerializeField]
    Slider _Slider = null;
    [SerializeField]
    Text _SpawnIntervalText = null;
    [SerializeField]
    Toggle _FlowToggle = null;

    private List<Lane> _Links = new List<Lane>();
    private List<VehicleSpawner> _Spawners = new List<VehicleSpawner>();
    private List<VehicleEnd> _Ends = new List<VehicleEnd>();

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
                if (openList.IndexOf(lane) == -1 && openList.IndexOf(lane) == -1)
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
        return CalculatePath(startPoint, _Ends[Random.Range(0, _Ends.Count - 1)].GetConnectedNode());
    }

    //UI functions
    public void Simulate()
    {
        _Simulate = !_Simulate;
        if(!_Simulate)
            for (int i = 0; i < _Links.Count; i++)
            {
                _Links[i].DestroyVehicles();
            }
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

    float _Delta = 0;
    private void Update()
    {
        if(_Simulate && _Ends.Count > 0 && _Spawners.Count > 0)
        {
            _Delta += Time.deltaTime;

            if(_Delta >= _SpawnInterval)
            {
                int count = 0;
                while(!_Spawners[Random.Range(0, _Spawners.Count - 1)].SpawnVehicle() && count < _Spawners.Count * 1.5f)
                {
                    ++count;
                }
                _Delta -= _SpawnInterval;
            }
        }
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

