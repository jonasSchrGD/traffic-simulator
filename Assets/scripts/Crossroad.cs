using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crossroad : RoadStructure
{
    [SerializeField]
    List<EndPoint> _ConnectedRoads = new List<EndPoint>();
    public int roadCount
    {
        get
        {
            return _ConnectedRoads.Count;
        }
    }
    [SerializeField]
    private float _DrivingSpeed = 5;
    private float _Distance = 0;
    private float _StraightAngle = 160;

    List<Node> _BeginNodes = new List<Node>();
    List<Node> _EndNodes = new List<Node>();

    public void ConnectRoad(EndPoint roadEnd)
    {
        if (_ConnectedRoads.Count < 4)
        {
            _ConnectedRoads.Add(roadEnd);
            roadEnd._Collider.enabled = false;
            roadEnd.crossroad = this;
            CalculateNetwork();
        }
    }
    public void DisconnectRoad(EndPoint roadEnd)
    {
        _ConnectedRoads.Remove(roadEnd);
        roadEnd._Collider.enabled = true;
        roadEnd.crossroad = null;
        CalculateNetwork();
        if (_ConnectedRoads.Count == 1)
        {
            _ConnectedRoads[0].crossroad = null;
            _ConnectedRoads[0].gameObject.GetComponent<Road>().EnableColliders();
            _ConnectedRoads[0].EnableUpdate(true);
            Destroy(gameObject);
        }
        else
            UpdateCrossroad(true);
    }

    //todo: calculate variables once
    public void UpdateCrossroad()
    {
        UpdateCrossroad(true);
    }
    public void UpdateCrossroad(bool UpdateConnections)
    {
        if(UpdateConnections)
        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            _ConnectedRoads[i].EnableUpdateCrossroad(true);
        }
        CalculateCorners();
    }

    private void CalculateOffset(int roadCount)
    {
        //https://www.mathopenref.com/polygonsides.html
        _Distance = _ConnectedRoads[0].gameObject.GetComponentInChildren<Road>().roadWidth / (2 * Mathf.Tan(Mathf.PI / roadCount));

        Vector3 direction = ((Vector3)_ConnectedRoads[0].transform.position - transform.position).normalized * _Distance;
        _ConnectedRoads[0].pos = transform.position + direction;
        _ConnectedRoads[0].EnableUpdate(true);

        Quaternion rotation = Quaternion.AngleAxis(360 / roadCount, Vector3.forward);

        for (int i = 1; i < _ConnectedRoads.Count; i++)
        {
            direction = rotation * direction;
            _ConnectedRoads[i].pos = transform.position + direction;
            _ConnectedRoads[i].EnableUpdate(true);
        }
    }

    private void SortList(EndPoint startPoint)
    {
        Vector2 startDirection = (startPoint.transform.position - transform.position).normalized;

        List<EndPoint> sortedList = new List<EndPoint>();

        sortedList.Add(startPoint);
        _ConnectedRoads.Remove(startPoint);

        EndPoint smallestAnglePoint = null;
        float smallestAngle = 360;
        while (_ConnectedRoads.Count > 0)
        {
            for (int i = 0; i < _ConnectedRoads.Count; i++)
            {
                Vector2 direction = (_ConnectedRoads[i].transform.position - transform.position).normalized;
                float angle = Vector3.SignedAngle(startDirection, direction, Vector3.forward);

                if (angle < 0)
                    angle = 360 + angle;

                if (angle < smallestAngle)
                {
                    smallestAngle = angle;
                    smallestAnglePoint = _ConnectedRoads[i];
                }
            }
            sortedList.Add(smallestAnglePoint);
            _ConnectedRoads.Remove(smallestAnglePoint);
            smallestAngle = 360;
        }

        _ConnectedRoads = sortedList;
    }
    private void Sort3WayQuad()
    {
        CalculateOffset(4);
        Vector2 startDirection = CalculateWeightedCentre();

        EndPoint smallestAnglePoint = null;
        float smallestAngle = 360;
        Vector2 newDir = Vector2.zero;
        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            Vector2 direction = (_ConnectedRoads[i].transform.position - transform.position).normalized;
            float angle = Vector3.Angle(startDirection, direction);

            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                smallestAnglePoint = _ConnectedRoads[i];
                newDir = direction;
            }
        }
        startDirection = newDir;

        smallestAngle = 360;
        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            Vector2 direction = (_ConnectedRoads[i].transform.position - transform.position).normalized;
            float angle = Vector3.SignedAngle(startDirection, direction, Vector3.forward);

            if (angle < 0 && Mathf.Abs(angle) < smallestAngle)
            {
                smallestAngle = Mathf.Abs(angle);
                smallestAnglePoint = _ConnectedRoads[i];
            }
        }
        SortList(smallestAnglePoint);
        CalculateOffset(4);
    }
    private void Sort3WayTriangle()
    {
        CalculateOffset(3);
        Vector2 startDirection = CalculateWeightedCentre();

        EndPoint smallestAnglePoint = null;
        float largestAngle = 0;
        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            Vector2 direction = (_ConnectedRoads[i].transform.position - transform.position).normalized;
            float angle = Vector3.Angle(startDirection, direction);

            if (angle > largestAngle)
            {
                largestAngle = angle;
                smallestAnglePoint = _ConnectedRoads[i];
            }
        }

        smallestAnglePoint.pos = (Vector2)transform.position + startDirection;
        SortList(smallestAnglePoint);
        CalculateOffset(3);
    }
    private Vector2 CalculateWeightedCentre()
    {
        Vector2 weightedDirection = Vector2.zero;
        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            weightedDirection += ((Vector2)_ConnectedRoads[i].transform.position - (Vector2)transform.position).normalized;
        }
        weightedDirection /= _ConnectedRoads.Count;
        weightedDirection.Normalize();
        return weightedDirection;
    }

    private void CalculateCorners()
    {
        float roadwidth = _ConnectedRoads[0].gameObject.GetComponentInChildren<Road>().roadWidth;
        if (_ConnectedRoads.Count != 2)
        {
            if (_ConnectedRoads.Count == 4)
                CreateQuad(roadwidth);
            else
            {
                if (HighestAngleBetweenRoads() > _StraightAngle)
                    CreateQuad(roadwidth);
                else
                    CreateTriangle(roadwidth);
            }
        }
        else
        {
            Vector2 directionToRoad1 = (transform.position - _ConnectedRoads[0].transform.position).normalized;
            Vector2 directionToRoad2 = (_ConnectedRoads[1].transform.position - transform.position).normalized;
            Vector2 middleDir = (directionToRoad1 + directionToRoad2).normalized;


            Vector2 point1 = transform.position + Quaternion.AngleAxis(-90, Vector3.forward) * middleDir * (roadwidth / 2);
            Vector2 point2 = transform.position + Quaternion.AngleAxis(90, Vector3.forward) * middleDir * (roadwidth / 2);

            _ConnectedRoads[0].SetCorners(point1, point2, -middleDir);
            _ConnectedRoads[1].SetCorners(point1, point2, middleDir);

            GetComponent<MeshFilter>().mesh = null;
            for (int i = 0; i < 2; i++)
            {
                _ConnectedRoads[i].pos = transform.position;
                _ConnectedRoads[i].EnableUpdate(true);
            }
        }

        foreach (var link in _Links)
        {
            link.UpdateNodes(true);
        }
    }
    private void CreateTriangle(float roadWidth)
    {
        Sort3WayTriangle();
        Vector2 startDirection = (_ConnectedRoads[0].pos - (Vector2)transform.position).normalized;

        Vector3[] corners = new Vector3[3];
        int[] tris = new int[3];

        Vector2 corner = _ConnectedRoads[0].pos + (Vector2)(Quaternion.AngleAxis(-90, Vector3.forward) * startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[0] = corner;
        tris[0] = 0;

        corner = _ConnectedRoads[0].pos + (Vector2)(Quaternion.AngleAxis(90, Vector3.forward) * startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[1] = corner;
        tris[1] = 1;

        corner = _ConnectedRoads[1].pos + (Vector2)(Quaternion.AngleAxis(90, Vector3.forward) * ((Vector3)_ConnectedRoads[1].pos - transform.position).normalized * (roadWidth / 2)) - (Vector2)transform.position;
        corners[2] = corner;
        tris[2] = 2;

        _ConnectedRoads[0].SetCorners(corners[1] + transform.position, corners[0] + transform.position, (_ConnectedRoads[0].pos - (Vector2)transform.position).normalized);
        _ConnectedRoads[1].SetCorners(corners[2] + transform.position, corners[1] + transform.position, (_ConnectedRoads[1].pos - (Vector2)transform.position).normalized);
        _ConnectedRoads[2].SetCorners(corners[0] + transform.position, corners[2] + transform.position, (_ConnectedRoads[2].pos - (Vector2)transform.position).normalized);

        Mesh mesh = new Mesh();
        mesh.vertices = corners;
        mesh.triangles = tris;
        GetComponent<MeshFilter>().mesh = mesh;
    }
    private void CreateQuad(float roadWidth)
    {
        Sort3WayQuad();
        Vector2 startDirection = (_ConnectedRoads[0].pos - (Vector2)transform.position).normalized;

        Vector3[] corners = new Vector3[4];
        int[] tris = new int[6];

        Vector2 corner = _ConnectedRoads[0].pos + (Vector2)(Quaternion.AngleAxis(-90, Vector3.forward) * startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[0] = corner;
        tris[0] = 0;

        corner = _ConnectedRoads[0].pos + (Vector2)(Quaternion.AngleAxis(90, Vector3.forward) * startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[1] = corner;
        tris[1] = 1;

        corner = _ConnectedRoads[2].pos + (Vector2)(Quaternion.AngleAxis(-90, Vector3.forward) * -startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[2] = corner;
        tris[2] = 2;

        corner = _ConnectedRoads[2].pos + (Vector2)(Quaternion.AngleAxis(90, Vector3.forward) * -startDirection * (roadWidth / 2)) - (Vector2)transform.position;
        corners[3] = corner;
        tris[3] = 0;
        tris[4] = 2;
        tris[5] = 3;

        _ConnectedRoads[0].SetCorners(corners[1] + transform.position, corners[0] + transform.position, (_ConnectedRoads[0].pos - (Vector2)transform.position).normalized);
        _ConnectedRoads[1].SetCorners(corners[2] + transform.position, corners[1] + transform.position, (_ConnectedRoads[1].pos - (Vector2)transform.position).normalized);
        _ConnectedRoads[2].SetCorners(corners[3] + transform.position, corners[2] + transform.position, (_ConnectedRoads[2].pos - (Vector2)transform.position).normalized);

        if (_ConnectedRoads.Count == 4)
            _ConnectedRoads[3].SetCorners(corners[0], corners[3], (_ConnectedRoads[3].pos - (Vector2)transform.position).normalized);

        Mesh mesh = new Mesh();
        mesh.vertices = corners;
        mesh.triangles = tris;
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private float HighestAngleBetweenRoads()
    {
        float largestAngle = 0;

        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            for (int j = 0; j < _ConnectedRoads.Count; j++)
            {
                if (i == j)
                    continue;

                float angle = Vector2.Angle(_ConnectedRoads[j].transform.position - transform.position, _ConnectedRoads[i].transform.position - transform.position);

                if (angle > largestAngle)
                    largestAngle = angle;
            }
        }

        return largestAngle;
    }

    private void CalculateNetwork()
    {
        _BeginNodes.Clear();
        _EndNodes.Clear();
        ClearLinks();

        Lane[] lanes = gameObject.GetComponents<Lane>();
        for (int i = 0; i < lanes.Length; i++)
        {
            Destroy(lanes[i]);
        }

        for (int i = 0; i < _ConnectedRoads.Count; i++)
        {
            _BeginNodes.Add(_ConnectedRoads[i]._LaneBeginPoint);
            _EndNodes.Add(_ConnectedRoads[i]._LaneEndPoint);
        }

        for (int i = 0; i < _BeginNodes.Count; i++)
        {
            for (int j = 0; j < _EndNodes.Count; j++)
            {
                if (i == j)
                    continue;

                Lane link = gameObject.AddComponent<Lane>();
                if (_ConnectedRoads.Count > 2)
                    link.maxDrivingSpeed = _DrivingSpeed;
                link._Nodes[1] = _BeginNodes[i];
                AddLink(link);

                _EndNodes[j].AddLink(link, 0);
            }
        }
    }

    private bool VehicleOnCrossRoad = false;
    [SerializeField]
    GameObject _Vehicle = null;
    public bool EnterCrossroad(GameObject vehicle)
    {
        if (_ConnectedRoads.Count == 2)
            return true;
        if (VehicleOnCrossRoad)
            return false;

        VehicleOnCrossRoad = true;
        _Vehicle = vehicle;
        return true;
    }
    public void LeaveCrossRoad()
    {
        VehicleOnCrossRoad = false;
    }

    private void Start()
    {
        Lane[] lanes = GetComponents<Lane>();

        if (lanes.Length > 0)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                AddLink(lanes[i]);
            }
        }
        Invoke("UpdateCrossroad", 0.05f);
    }
    private void Update()
    {
        if (VehicleOnCrossRoad)
            if (!_Vehicle)
                VehicleOnCrossRoad = false; 
    }
}
