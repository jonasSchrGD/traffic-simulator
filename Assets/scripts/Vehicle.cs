using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    private Lane _CurrentLane;
    public Lane currentLane
    {
        get
        {
            return _CurrentLane;
        }
        set
        {
            _CurrentLane = value;
        }
    }
    [SerializeField]
    private GameObject _LeadingVehicle;
    private GameObject _FollowingVehicle;
    [SerializeField]
    public float _MinDistance = 0.5f;
    [SerializeField]
    private float _GapTime = 1.0f;
    public float _CarLength = 1;
    [SerializeField]
    private int _AccelerationExp = 4;
    private int _PointIdx = 1;
    Vector2 _OldTarget, _CurrentTarget;
    Vector2 _OldDirection;
    float _RotationTime = 0;

    [SerializeField]
    private float _MaxAcceleration = 7;
    [SerializeField]
    private float _Maxdeceleration = 6;

    private Vector3 _FreeTarget;
    private Lane _FreeLane;

    public List<Node> path = null;
    private int _PathIdx = 0;

    [Header("DEBUG: do not touch"), SerializeField]
    float gap = 0;
    [SerializeField]
    private float _Velocity = 0;
    [SerializeField]
    float acceleration = 0;
    [SerializeField]
    Vector2 direction = Vector2.zero;

    private void Start()
    {
        transform.position = path[0].position;
        while (!GetNextLane())
        {
            List<Node> newPath = Network.instance.CalculatePath(path[_PathIdx], path[path.Count - 1]);
            if (newPath == null)
            {
                Destroy(gameObject);
                break;
            }
            else
                path = newPath;
        }

        if (_CurrentLane != null)
        {
            _OldTarget = _CurrentLane.path[0];
            _CurrentTarget = _CurrentLane.path[1];

            UpdateRotation();
            FindFreeTarget();
            _FreeLane = _CurrentLane;
        }

        _CarLength = GetComponent<SpriteRenderer>().size.x * transform.localScale.x;
    }

    private void Update()
    {
        float freeAcceleration = CalculateVelocity();

        transform.position = Vector2.MoveTowards(transform.position, _CurrentTarget, Time.deltaTime * _Velocity);
        UpdateTarget(freeAcceleration);

        if(_RotationTime < 1)
        {
            Quaternion newRot = Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, direction), Vector3.forward);
            _RotationTime += Time.deltaTime / 0.6f;

            transform.rotation = Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, Vector2.Lerp(_OldDirection, direction, _RotationTime)), Vector3.forward);
        }


    }

    bool _Destroy = false;
    private void UpdateTarget(float freeAcceleration)
    {
        if (((Vector2)transform.position - _CurrentTarget).magnitude < 0.01f)
        {
            _OldTarget = _CurrentTarget;
            _PointIdx = _CurrentLane.GetNextTarget(_PointIdx, ref _CurrentTarget);

            if (_PointIdx == -1)
            {
                while (!GetNextLane() && !_Destroy)
                {
                    List<Node> newPath = Network.instance.CalculatePath(path[_PathIdx], path[path.Count - 1]);
                    if (newPath == null)
                    {
                        Destroy(gameObject);
                        break;
                    }
                    else
                        path = newPath;
                }
            }

            UpdateRotation();
        }
        if ((Vector2)_FreeTarget == path[path.Count - 1].position && (_FreeTarget - transform.position).magnitude - _CarLength / 2 < 1)
            GetNextLane();
        FindFreeTarget();
    }
    private bool GetNextLane()
    {
        if(_PathIdx == path.Count - 1)
        {
            Destroy(gameObject);
            _Destroy = true;
            return false;
        }

        int idx = 0;
        Lane current = path[_PathIdx].connectedLanes[idx];
        while (current._Nodes[1] != path[_PathIdx + 1])
        {
            ++idx;
            if (idx == path[_PathIdx].connectedLanes.Count)
                return false;

            current = path[_PathIdx].connectedLanes[idx];
        }
        ++_PathIdx;
        current.AddVehicle(gameObject);
        _CurrentLane = current;
        _LeadingVehicle = _CurrentLane.GetLeadingVehicle(gameObject);
        return true;
    }

    private float CalculateFollowVelocity()
    {
        Vehicle leading = _LeadingVehicle.GetComponent<Vehicle>();

        gap = (_LeadingVehicle.transform.position - transform.position).magnitude - (leading._CarLength / 2 + _CarLength / 2);
        float desiredGap = CalculateDesiredGap(leading._Velocity);

        float relativeGap = desiredGap / gap;
        float acceleration = _MaxAcceleration * (1 - Mathf.Pow(_Velocity / _CurrentLane.maxDrivingSpeed, _AccelerationExp) - relativeGap * relativeGap);

        acceleration = Mathf.Max(acceleration, -_Maxdeceleration);

        return acceleration;
    }

    private float CalculateFreeVelocity()
    {
        gap = (_FreeTarget - transform.position).magnitude - _CarLength / 2;
        float desiredGap = CalculateDesiredGap(0);

        float relativeGap = desiredGap / gap;
        float acceleration = _MaxAcceleration * (1 - Mathf.Pow(_Velocity / _CurrentLane.maxDrivingSpeed, _AccelerationExp) - relativeGap * relativeGap);

        acceleration = Mathf.Max(acceleration, -_Maxdeceleration);

        return acceleration;
    }

    private float CalculateDesiredGap(float leadingVelocity)
    {
        float term1 = _Velocity * _GapTime;
        float term2 = (_Velocity * (_Velocity - leadingVelocity)) / (2 * Mathf.Sqrt(_MaxAcceleration * _Maxdeceleration));

        return _MinDistance + term1 + term2;
    }

    private void UpdateRotation()
    {
        _OldDirection = direction;
        direction = _CurrentTarget - _OldTarget;
        _RotationTime = 0;
    }

    Crossroad _CurrentCrossroad = null;
    float _CrossroadDistance = 3.5f;
    private void FindFreeTarget()
    {
        Lane endLane = _CurrentLane;
        while(endLane._Nodes[1].connectedLanes.Count == 1)
        {
            if (endLane._Nodes[1].connectedLanes[0] != _CurrentLane)
                endLane = endLane._Nodes[1].connectedLanes[0];
            else
            {
                _FreeTarget = Vector3.forward * 10000;
                return;
            }
        }

        if(Vector2.Distance(endLane._Nodes[1].position, transform.position) < _CrossroadDistance && 
            _CurrentLane._Nodes[1].parent.crossroad && _CurrentLane._Nodes[1].parent.crossroad.roadCount > 2 && 
            endLane._Nodes[1].parent.crossroad.EnterCrossroad(gameObject))
        {
            if(!_LeadingVehicle  || _LeadingVehicle && Vector2.Distance(transform.position, _LeadingVehicle.transform.position) - _CarLength > _MinDistance * 1.5f)
            {
                _CurrentCrossroad = endLane._Nodes[1].parent.crossroad;
                _FreeTarget = Vector3.forward * 10000;
            }
        }
        if (_CurrentCrossroad && _CurrentLane._Nodes[0].parent.crossroad == _CurrentCrossroad && _CurrentCrossroad != _CurrentLane._Nodes[1].parent.crossroad)
        {
            if (Vector2.Distance(_CurrentLane._Nodes[0].position, transform.position) > _CarLength / 2)
            {
                _CurrentCrossroad.LeaveCrossRoad();
                _FreeTarget = Vector3.forward * 10000;
                _CurrentCrossroad = null;
            }
        }

        if (!_CurrentCrossroad)
            _FreeTarget = endLane.path[endLane.path.Count - 1];
    }

    private float CalculateVelocity()
    {
        acceleration = 0;

        float freeAcceleration = CalculateFreeVelocity();

        if (_LeadingVehicle)
            acceleration = Mathf.Min(freeAcceleration, CalculateFollowVelocity());
        else
            acceleration = freeAcceleration;

        _Velocity = Mathf.Clamp(_Velocity + acceleration * Time.deltaTime, 0, _CurrentLane.maxDrivingSpeed);

        return freeAcceleration;
    }

    public void RefreshLeadingVehicle()
    {
        if (_FollowingVehicle)
        {
            Vehicle following = _FollowingVehicle.GetComponent<Vehicle>();
            _FollowingVehicle = null;
            following.FindLeadingVehicle();
        }
    }
    private void FindLeadingVehicle()
    {
        _LeadingVehicle = _CurrentLane.GetLeadingVehicle(gameObject);
        if (_LeadingVehicle)
            _LeadingVehicle.GetComponent<Vehicle>()._FollowingVehicle = gameObject;
    }

    private void OnDestroy()
    {
        _CurrentLane.RemoveVehicle(gameObject);
        if (_CurrentCrossroad)
            _CurrentCrossroad.LeaveCrossRoad();

        if (_FollowingVehicle)
            _FollowingVehicle.GetComponent<Vehicle>()._LeadingVehicle = null;

        if (_LeadingVehicle)
            _LeadingVehicle.GetComponent<Vehicle>()._FollowingVehicle = null;
    }
}
