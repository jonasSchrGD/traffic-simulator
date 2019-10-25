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
            return _LaneWidth * _NrOfLanes;
        }
    }
    private float _LaneWidth = 1.5f;

    [SerializeField]
    private int _NrOfLanes = 1;

    public float maxDrivingSpeed
    {
        get
        {
            return _Links[0].maxDrivingSpeed;
        }
        set
        {
            for (int i = 0; i < _Links.Count; i++)
            {
                _Links[i].maxDrivingSpeed = value;
            }
        }
    }

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
        CircleCollider2D[] colliders = gameObject.GetComponentsInChildren<CircleCollider2D>();
        _EndPoints[0]._Collider = colliders[0];
        _EndPoints[1]._Collider = colliders[1];
        float _CirclePos = _EndPoints[1]._Collider.offset.x;

        if (_EndPoints[0].pos == Vector2.zero)
            _EndPoints[0].pos = transform.position + Vector3.left * _CirclePos;
        if (_EndPoints[1].pos == Vector2.zero)
            _EndPoints[1].pos = transform.position + Vector3.right * _CirclePos;

        if (!GetLanes())
            InitNetwork(_LaneWidth);

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
        for (int i = 0; i < _NrOfLanes; ++i)
        {
            Lane lane = gameObject.AddComponent<Lane>();
            lane.Parent = this;
            AddLink(lane);
            
            if(i < _NrOfLanes / 2 || _NrOfLanes == 1)
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
    public VehicleSpawner GetSpawner(int end)
    {
        if (_EndPoints[end]._ConnectedSpawner)
        {
            return _EndPoints[end]._ConnectedSpawner.GetComponent<VehicleSpawner>();
        }

        return null;
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

        List<Vector2>[] paths = new List<Vector2>[_NrOfLanes];
        for (int i = 0; i < _NrOfLanes; i++)
        {
            paths[i] = new List<Vector2>();
        }

        int triIndex = 6 + 6 * (_Links.Count - 1), vertIdx = (2 + 2 * (_Links.Count - 1));
        Vector2[] points = new Vector2[4];
        Vector2[] path = new Vector2[_NrOfSteps + 1];
        Vector3[] vertices = new Vector3[vertIdx * (_NrOfSteps + 1)];
        Vector2[] uv = new Vector2[vertIdx * (_NrOfSteps + 1)];
        int[] tris = new int[triIndex * _NrOfSteps];
        float x = 0;

        int currentTriIdx = 0, currentVertIdx = 0;
        while (currentVertIdx < vertices.Length - vertIdx)
            AddTris(ref tris, ref currentVertIdx, vertIdx, ref currentTriIdx);

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

        Vector2 startDir = Vector2.zero;

        //calculate first 2 vertices
        if (_EndPoints[0].crossroad)
            startDir = -_EndPoints[0]._WorldDirection;
        else
            startDir = (path[0] - path[1]).normalized;

        CalculatePoints(path[0], startDir, ref vertices, 0, ref paths, ref uv, x);

        //calculate middle vertices
        currentVertIdx = 0;
        for (int i = 1; i < _NrOfSteps; i++)
        {
            Vector2 PrevDir = (path[i - 1] - path[i]).normalized;
            startDir = (path[i] - path[i + 1]).normalized;
            Vector2 dir = (PrevDir + startDir).normalized;
            x += Vector2.Distance(path[i - 1], path[i]);

            currentVertIdx += vertIdx;
            CalculatePoints(path[i], dir, ref vertices, currentVertIdx, ref paths, ref uv, x);
        }
        x += Vector2.Distance(path[_NrOfSteps - 1], path[_NrOfSteps]);

        //calculate last 2 vertices
        if (_EndPoints[1].crossroad)
            startDir = _EndPoints[1]._WorldDirection;
        else
            startDir = (path[_NrOfSteps - 1] - path[_NrOfSteps]).normalized;

        CalculatePoints(path[_NrOfSteps], startDir, ref vertices, currentVertIdx + vertIdx, ref paths, ref uv, x);

        //center vertices
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] -= transform.position;

        for (int i = 0; i < _NrOfLanes; i++)
        {
            _Links[i].path = paths[i];
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.uv = uv;
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

    [SerializeField]
    private float scaling = 0.25f;
    private void CalculatePoints(Vector2 point, Vector2 direction, ref Vector3[] vertices, int idx, ref List<Vector2>[] lanes, ref Vector2[] uv, float x)
    {
        direction.Normalize();
        Vector2 rightDir = left * direction;
        Vector2 leftDir = right * direction;

        Vector2 leftPoint = point + leftDir * ((_LaneWidth * _NrOfLanes) / 2);
        float y = 1, yStep = 1.0f / _NrOfLanes;

        for (int i = 0; i < _NrOfLanes; i++)
        {
            if (i < _NrOfLanes / 2 || _NrOfLanes == 1)
                lanes[i].Add(leftPoint + rightDir * _LaneWidth / 2);
            else
                lanes[i].Insert(0, leftPoint + rightDir * _LaneWidth / 2);

            uv[idx + i * 2] = new Vector2(x * scaling, y);
            y -= yStep;
            uv[idx + i * 2 + 1] = new Vector2(x * scaling, y);

            vertices[idx + i * 2] = leftPoint;
            leftPoint += rightDir * _LaneWidth;
            vertices[idx + i * 2 + 1] = leftPoint;
        }
    }
    private void AddTris(ref int[] tris, ref int n, int verticesPerRow, ref int triIdx)
    {
        tris[triIdx] = n;
        tris[triIdx + 1] = n + verticesPerRow;
        tris[triIdx + 2] = n + 1;

        tris[triIdx + 3] = n + 1;
        tris[triIdx + 4] = n + verticesPerRow;
        tris[triIdx + 5] = n + verticesPerRow + 1;

        triIdx += 6;
        n += 2;
    }
}