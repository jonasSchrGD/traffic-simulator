using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Lane : MonoBehaviour
{
    public List<GameObject> _Vehicles = new List<GameObject>();
    public int VehicleCount
    {
        get
        {
            return _Vehicles.Count;
        }
    }
    public Road Parent;

    [SerializeField]
    private float _Length = 0;
    [SerializeField]
    public float _Density = 0;
    public List<Vector2> path
    {
        get
        {
            return _Path;
        }
        set
        {
            _Path = value;

            _Nodes[0].position = _Path[0];
            _Nodes[1].position = _Path[_Path.Count - 1];

            _Length = 0;
            for (int i = 0; i < _Path.Count - 1; i++)
            {
                _Length += Vector2.Distance(_Path[i], _Path[i + 1]);
            }
        }
    }
    [SerializeField]
    private List<Vector2> _Path = new List<Vector2>();

    public Node[] _Nodes = new Node[2];

    public Vector3[] ends
    {
        get
        {
            return new Vector3[2] { _Path[0], _Path[_Path.Count - 1] };
        }
    }

    [SerializeField]
    private float _MaxDrivingSpeed = 10;
    public float maxDrivingSpeed
    {
        get
        {
            return _MaxDrivingSpeed;
        }
        set
        {
            _MaxDrivingSpeed = value;
        }
    }

    public Vector2 GetDirection()
    {
        return (_Path[_Path.Count - 1] - _Path[0]).normalized;
    }

    [SerializeField]
    float _MaxVehicleCount = 1;
    private void Update()
    {
        if (_Vehicles.Count > 0)
        {
            _MaxVehicleCount = (_Length / (_Vehicles[0].GetComponent<Vehicle>()._CarLength + _Vehicles[0].GetComponent<Vehicle>()._MinDistance));
            _Density = _Vehicles.Count / _MaxVehicleCount;
        }
        else
            _Density = 0;
    }

    public GameObject GetLeadingVehicle(GameObject currentvehicle)
    {
        int vehicleIdx = _Vehicles.IndexOf(currentvehicle);

        if (vehicleIdx - 1 >= 0)
            return _Vehicles[vehicleIdx - 1];

        if (vehicleIdx == -1 && _Vehicles.Count > 0)
            return _Vehicles[_Vehicles.Count - 1];

        return null;
    }

    public int GetNextTarget(int Idx, ref Vector2 target)
    {
        if(Idx + 1 < _Path.Count)
        {
            target = _Path[Idx + 1];
            return Idx + 1;
        }

        return -1;
    }

    public void AddVehicle(GameObject vehicle)
    {
        if (_Vehicles.IndexOf(vehicle) == -1)
            _Vehicles.Add(vehicle);

        if (vehicle.GetComponent<Vehicle>().currentLane)
            vehicle.GetComponent<Vehicle>().currentLane.RemoveVehicle(vehicle);
        vehicle.GetComponent<Vehicle>().currentLane = this;//stupid decision
    }
    public void RemoveVehicle(GameObject vehicle)
    {
        int vehicleIdx = _Vehicles.IndexOf(vehicle);
        if(vehicleIdx != -1)
            _Vehicles.Remove(vehicle);
    }

    public void DrawGizmos()
    {
        for (int i = 0; i < _Path.Count - 1; i++)
        {
            Gizmos.DrawLine(_Path[i], _Path[i + 1]);
        }
    }

    public void UpdateNodes(bool beginNode)
    {
        if(path.Count > 0)
        {
            if (beginNode)
                _Path[0] = _Nodes[0].position;
            else
                _Path[_Path.Count - 1] = _Nodes[1].position;
        }
        else
        {
            path.Add(_Nodes[0].position);
            path.Add(_Nodes[1].position);
        }
    }

    public bool CanEnter()
    {
        return _Vehicles.Count + 1 <= Mathf.RoundToInt(_MaxVehicleCount);
    }

    //network calculations
    private float gCost = 0, hCost = 0;
    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
    private Lane _HeadConnection;
    public Lane headConnection
    {
        get
        {
            return _HeadConnection;
        }
        set
        {
            _HeadConnection = value;
        }
    }
    public void calculateCost(Node endNode)
    {
        gCost = 0;

        if (headConnection)
            gCost += headConnection.gCost;

        for (int i = 0; i < _Path.Count - 1; i++)
        {
            gCost += (_Path[i] - _Path[i + 1]).magnitude;
        }

        hCost = (_Nodes[1].position - endNode.position).magnitude;
        hCost += _Density * _Length;
    }

    private void OnDestroy()
    {
        DestroyVehicles();
    }

    public void DestroyVehicles()
    {
        for (int i = 0; i < _Vehicles.Count; i++)
        {
            Destroy(_Vehicles[i]);
        }
        _Vehicles.Clear();
    }
}
