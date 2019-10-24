using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Road : RoadStructure
{
    [SerializeField]
    private GameObject _Vehicle = null;
    [SerializeField]
    private GameObject _CrossRoadBP = null;
    [SerializeField]
    public int _NrOfSteps = 10;

    public float roadWidth
    {
        get
        {
            return height;
        }
    }
    private float height = 3;

    [SerializeField]
    private int nrOfLanes = 1;

    private Lane[] lane;

    [SerializeField]
    private EndPoint[] _EndPoints = null;
    public Vector2[] endpoints
    {
        get
        {
            Vector2[] endpoints = new Vector2[2] { _EndPoints[0].pos, _EndPoints[1].pos };
            return endpoints;
        }
        set
        {
            _EndPoints[0].pos = value[0];
            _EndPoints[0].EnableUpdateCrossroad(true);

            _EndPoints[1].pos = value[1];
            _EndPoints[1].EnableUpdateCrossroad(true);
        }
    }

    public Vector2 _BeginToEnd = Vector2.zero;
    public float bezHandleMul = 0.15f;

    Quaternion left = Quaternion.AngleAxis(90, Vector3.forward);
    Quaternion right = Quaternion.AngleAxis(-90, Vector3.forward);

    //functions
    private void Start()
    {
        float laneHeight = height / nrOfLanes;

        CircleCollider2D[] colliders = gameObject.GetComponentsInChildren<CircleCollider2D>();
        _EndPoints[0]._Collider = colliders[0];
        _EndPoints[1]._Collider = colliders[1];
        float _CirclePos = _EndPoints[1]._Collider.offset.x;

        if (_EndPoints[0].pos == Vector2.zero)
            _EndPoints[0].pos = transform.position + Vector3.left * _CirclePos;
        if (_EndPoints[1].pos == Vector2.zero)
            _EndPoints[1].pos = transform.position + Vector3.right * _CirclePos;

        if (!GetLanes())
            InitNetwork(laneHeight);

        UpdateRoad(true);
    }

    [SerializeField]
    bool _DrawFlow = false;
    public bool drawFlow
    {
        get
        {
            return _DrawFlow;
        }
        set
        {
            _DrawFlow = value;
        }
    }
    private void Update()
    {
        //flow calculations
        if (_DrawFlow)
            DrawDensity();
        else
            drawRoad();
    }

    public void UpdateRoad()
    {
        UpdateRoad(false);
    }
    public void UpdateRoad(bool UpdateEnds)
    {
        _BeginToEnd = _EndPoints[1].pos - _EndPoints[0].pos;

        transform.position = (_EndPoints[0].pos + _EndPoints[1].pos) / 2;

        transform.GetChild(0).rotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.right, _BeginToEnd, Vector3.forward), Vector3.forward);

        if (_Links.Count > 0)
            CreateSprite();

        _EndPoints[0].EnableUpdate(UpdateEnds);
        _EndPoints[1].EnableUpdate(UpdateEnds);
    }

    private bool GetLanes()
    {
        Lane[] lanes = GetComponents<Lane>();

        if(lanes.Length > 0)
        {
            AddLink(lanes[0]);
            AddLink(lanes[1]);

            return true;
        }
        return false;
    }
    private void InitNetwork(float laneHeight)
    {
        for (int i = 0; i < nrOfLanes; ++i)
        {
            Lane lane = gameObject.AddComponent<Lane>();
            lane.Parent = this;
            AddLink(lane);
            
            if(i == 0)
            {
                lane._Nodes[1] = _EndPoints[1]._LaneEndPoint;

                _EndPoints[0]._LaneBeginPoint.AddLink(lane, 0);
            }
            else
            {
                lane._Nodes[1] = _EndPoints[0]._LaneEndPoint;

                _EndPoints[1]._LaneBeginPoint.AddLink(lane, 0);
            }
        }
    }

    private void SpawnVehicle(int i)
    {
        GameObject vehicle = Instantiate(_Vehicle);
        vehicle.transform.position = _Links[i].path[0];
        _Links[i].AddVehicle(vehicle);
    }

    public int GetClickedRoadEnd(Vector2 mousePos)
    {
        for(int i = 0; i < 2; ++i)
        {
            if (_EndPoints[i]._Collider.OverlapPoint(mousePos))
                return i;
        }
        return -1;
    }

    public void ConnectRoad(Road other, int endToConnect, int nodeToRemove)
    {
        GameObject crossRoad = Instantiate(_CrossRoadBP);
        _EndPoints[nodeToRemove].crossroad = crossRoad.GetComponent<Crossroad>();
        crossRoad.transform.position = _EndPoints[nodeToRemove].pos;

        _EndPoints[nodeToRemove].crossroad.ConnectRoad(_EndPoints[nodeToRemove]);
        _EndPoints[nodeToRemove].crossroad.ConnectRoad(other._EndPoints[endToConnect]);

        crossRoad.GetComponent<Crossroad>().UpdateCrossroad(true);
    }
    public void ConnectRoad(Crossroad other, int nodeToRemove)
    {
        other.transform.position = _EndPoints[nodeToRemove].pos;
        other.GetComponent<Crossroad>().ConnectRoad(_EndPoints[nodeToRemove]);
        other.GetComponent<Crossroad>().UpdateCrossroad(true);
    }
    public void DisconnectEndPoints()
    {
        for (int i = 0; i < 2; i++)
        {
            Crossroad crossroad = _EndPoints[i].crossroad;
            if (crossroad)
                crossroad.DisconnectRoad(_EndPoints[i]);
        }
    }

    public bool ConnectSpawner(GameObject spawner, int endToConnect)
    {
        if (_EndPoints[endToConnect]._ConnectedSpawner)
            return false;

        VehicleSpawner vSpawner = spawner.GetComponent<VehicleSpawner>();

        Vector3 pos = _EndPoints[endToConnect]._LaneBeginPoint.position;
        pos.z = -0.01f;
        spawner.transform.position = pos;
        spawner.transform.parent = transform;
        vSpawner._ConnectedLane = _EndPoints[endToConnect]._LaneBeginPoint.connectedLanes[0];

        _EndPoints[endToConnect]._ConnectedSpawner = spawner;
        return true;
    }
    public void RemoveSpawner(int endToRemove)
    {
        if (_EndPoints[endToRemove]._ConnectedSpawner)
            Destroy(_EndPoints[endToRemove]._ConnectedSpawner);
    }
    public bool ConnectEnd(GameObject end, int endToConnect)
    {
        if (_EndPoints[endToConnect]._ConnectedEnd)
            return false;

        VehicleEnd vEnd = end.GetComponent<VehicleEnd>();

        Vector3 pos = _EndPoints[endToConnect]._LaneEndPoint.position;
        pos.z = -0.01f;
        end.transform.position = pos;
        end.transform.parent = transform;
        vEnd._ConnectedNode = _EndPoints[endToConnect]._LaneEndPoint;

        _EndPoints[endToConnect]._ConnectedEnd = end;
        return true;
    }
    public void RemoveEnd(int endToRemove)
    {
        if (_EndPoints[endToRemove]._ConnectedEnd)
            Destroy(_EndPoints[endToRemove]._ConnectedEnd);
    }

    public void EnableColliders()
    {
        _EndPoints[0].EnableCollision();
        _EndPoints[1].EnableCollision();
    }
    public void DisableColliders()
    {
        _EndPoints[0].DisabeCollision();
        _EndPoints[1].DisabeCollision();
    }

    private void CreateSprite()
    {
        if (_NrOfSteps <= 0)
            return;

        List<Vector2> path1 = new List<Vector2>();
        List<Vector2> path2 = new List<Vector2>();

        Vector2[] points = new Vector2[4];
        Vector2[] path = new Vector2[_NrOfSteps + 1];
        Vector3[] vertices = new Vector3[(2 * _Links.Count) * (_NrOfSteps + 1)];
        int[] tris = new int[12 * _NrOfSteps];

        //calculate path
        float distance = (_EndPoints[0].pos - _EndPoints[1].pos).magnitude * bezHandleMul;
        points[0] = _EndPoints[0].pos;
        points[1] = _EndPoints[0].GetBezierHandle(_EndPoints[1], distance);
        points[2] = _EndPoints[1].GetBezierHandle(_EndPoints[0], distance);
        points[3] = _EndPoints[1].pos;

        float stepSize = 1.0f / _NrOfSteps;
        for (int i = 0; i <= _NrOfSteps; ++i)
        {
            Vector2 Lerp1 = Vector2.Lerp(points[0], points[1], stepSize * i);
            Vector2 Lerp2 = Vector2.Lerp(points[1], points[2], stepSize * i);
            Vector2 Lerp3 = Vector2.Lerp(points[2], points[3], stepSize * i);

            Vector2 Lerp12 = Vector2.Lerp(Lerp1, Lerp2, stepSize * i);
            Vector2 Lerp23 = Vector2.Lerp(Lerp2, Lerp3, stepSize * i);

            path[i] = Vector2.Lerp(Lerp12, Lerp23, stepSize * i);
        }

        tris[0] = 0;
        tris[1] = 4;
        tris[2] = 1;
        tris[3] = 1;
        tris[4] = 4;
        tris[5] = 5;
        tris[6] = 2;
        tris[7] = 6;
        tris[8] = 3;
        tris[9] = 3;
        tris[10] = 6;
        tris[11] = 7;

        int triIndex = 12, vertIdx = (2 * _Links.Count);
        Vector2 startDir = Vector2.zero;

        //calculate first 2 vertices
        if (_EndPoints[0].crossroad)
            startDir = -_EndPoints[0]._WorldDirection;
        else
            startDir = (path[0] - path[1]).normalized;

        CalculatePoints(path[0], startDir, ref vertices, 0, ref path1, ref path2);
        
        //calculate middle vertices
        for (int i = 1; i < _NrOfSteps; i++)
        {
            Vector2 PrevDir = (path[i - 1] - path[i]).normalized;
            startDir = (path[i] - path[i + 1]).normalized;
            Vector2 dir = (PrevDir + startDir).normalized;

            CalculatePoints(path[i], dir, ref vertices, vertIdx, ref path1, ref path2);


            tris[triIndex] = vertIdx;
            tris[triIndex + 1] = vertIdx + 4;
            tris[triIndex + 2] = vertIdx + 1;
            tris[triIndex + 3] = vertIdx + 1;
            tris[triIndex + 4] = vertIdx + 4;
            tris[triIndex + 5] = vertIdx + 5;

            tris[triIndex + 6] = vertIdx + 2;
            tris[triIndex + 7] = vertIdx + 6;
            tris[triIndex + 8] = vertIdx + 3;
            tris[triIndex + 9] = vertIdx + 3;
            tris[triIndex + 10] = vertIdx + 6;
            tris[triIndex + 11] = vertIdx + 7;

            vertIdx += (2 * _Links.Count);
            triIndex += 12;
        }

        //calculate last 2 vertices
        if (_EndPoints[1].crossroad)
            startDir = _EndPoints[1]._WorldDirection;
        else
            startDir = (path[_NrOfSteps - 1] - path[_NrOfSteps]).normalized;

        CalculatePoints(path[_NrOfSteps], startDir, ref vertices, vertIdx, ref path1, ref path2);

        //center vertices
        for (int i = 0; i < (2 * _Links.Count) * (_NrOfSteps + 1); i++)
            vertices[i] -= transform.position;

        _Links[0].path = path1;
        _Links[1].path = path2;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        GetComponent<MeshFilter>().mesh = mesh;

        List<Color> colors = new List<Color>();
        for (int i = 0; i < vertices.Length; ++i)
        {
            colors.Add(Color.white);
        }
        GetComponent<MeshFilter>().mesh.SetColors(colors);
    }
    private void DrawDensity()
    {
        List<Color> colors = new List<Color>();
        for (int i = 0, length = GetComponent<MeshFilter>().mesh.colors.Length / 2; i < length; ++i)
        {
            Color color = Color.green * (1 - _Links[i % 2]._Density) + Color.red * _Links[i % 2]._Density;
            color *= 2;
            colors.Add(color);
            colors.Add(color);
        }
        GetComponent<MeshFilter>().mesh.SetColors(colors);
    }
    private void drawRoad()
    {
        List<Color> colors = new List<Color>();
        for (int i = 0, length = GetComponent<MeshFilter>().mesh.colors.Length; i < length; ++i)
        {
            colors.Add(Color.white);
        }
        GetComponent<MeshFilter>().mesh.SetColors(colors);
    }

    private void CalculatePoints(Vector2 point, Vector2 direction, ref Vector3[] vertices, int idx, ref List<Vector2> link1, ref List<Vector2> link2)
    {
        direction.Normalize();
        Vector2 leftDir = left * direction;
        Vector2 rightDir = right * direction;

        link1.Add(point + leftDir * (roadWidth / 4));
        link2.Insert(0, point + rightDir * (roadWidth / 4));

        if (_Links.Count > 1)
        {
            vertices[idx] = point + leftDir * roadWidth / 2;
            vertices[idx + 1] = point;
            vertices[idx + 2] = point;
            vertices[idx + 3] = point + rightDir * roadWidth / 2;
        }
        else
        {
            vertices[idx] = point + leftDir * roadWidth / 2;
            vertices[idx + 3] = point + rightDir * roadWidth / 2;
        }
    }
}