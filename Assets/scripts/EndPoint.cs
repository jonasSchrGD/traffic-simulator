using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndPoint : MonoBehaviour
{
    public Road Parent = null;
    public int _LocalDirection;
    public Vector2 _WorldDirection;

    //position
    [SerializeField]
    private Vector2 _Pos;
    public Vector2 pos
    {
        get
        {
            return _Pos;
        }
        set
        {
            _Pos = value;
        }
    }

    //connect crossroad
    [SerializeField]
    private Crossroad _Crossroad = null;
    public Crossroad crossroad
    {
        get
        {
            return _Crossroad;
        }
        set
        {
            _Crossroad = value;
        }
    }

    public void EnableUpdate()
    {
        EnableUpdate(false);
    }
    public void EnableUpdate(bool updateParent)
    {
        if (_Collider)
            _Collider.offset = new Vector2(_LocalDirection * (Parent._BeginToEnd.magnitude / 2), 0);

        if (_ConnectedSpawner)
            _ConnectedSpawner.transform.localPosition = (Vector3)_LaneBeginPoint.position + Vector3.back * 0.05f - transform.position;

        if (_ConnectedEnd)
            _ConnectedEnd.transform.localPosition = (Vector3)_LaneEndPoint.position + Vector3.back * 0.05f - transform.position;

        if (Parent && updateParent)
            Parent.UpdateRoad();
    }
    public void EnableUpdateCrossroad(bool updateParent)
    {
        EnableUpdate(updateParent);

        if (_Crossroad)
            _Crossroad.UpdateCrossroad(false);
    }

    public Vector2 GetBezierHandle(EndPoint other, float distance)
    {
        int mul = _Crossroad ? 3 : 1;

        if (_Crossroad)
            return pos + _WorldDirection * distance * mul;

        if (other.crossroad)
            return (other.GetBezierHandle(this, distance) - pos).normalized * distance + pos;

        return ((Vector2)transform.position - pos).normalized * distance + pos;
    }

    //colliders
    public CircleCollider2D _Collider = null;
    public void EnableCollision()
    {
        if (_Crossroad == null && _Collider)
            _Collider.enabled = true;
    }
    public void DisabeCollision()
    {
        _Collider.enabled = false;
    }

    //corner positions of road end
    private Vector2[] _RoadCorners = new Vector2[2];
    public Vector2[] roadCorners
    {
        get
        {
            return _RoadCorners;
        }
    }
    public void SetCorners(Vector2 leftCorner, Vector2 rightCorner, Vector2 direction)
    {
        _WorldDirection = direction;

        Vector2 dirToLeft = leftCorner - pos;
        if (Vector2.SignedAngle(direction, dirToLeft) * _LocalDirection > 0)
        {
            _RoadCorners[0] = leftCorner;
            _RoadCorners[1] = rightCorner;
        }
        else
        {
            _RoadCorners[1] = leftCorner;
            _RoadCorners[0] = rightCorner;
        }


        EnableUpdate(true);
    }

    //lane ends
    public Node _LaneEndPoint, _LaneBeginPoint;
    public GameObject _ConnectedSpawner, _ConnectedEnd;
}