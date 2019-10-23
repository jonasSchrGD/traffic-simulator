using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField]
    private GameObject _RoadBP = null;
    private Road _CurrentRoad;
    private Crossroad _CurrentCrossRoad;

    [SerializeField]
    Image _UIBackGround;

    Vector2 _OldMousePos, _CurrentMousePos;

    private int _RoadEnd = 0;
    private void Update()
    {
        _OldMousePos = _CurrentMousePos;
        _CurrentMousePos = Input.mousePosition;

        UpdateCameraInput();
        if (Network.instance.isSimulating)
        {
            UpdateSimulateMode();
        }
        else
        {
            UpdateEditorInput();
            UpdateEditorMode();
        }
    }

    private void UpdateEditorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_CurrentRoad == null && !IsUsingUI())
            {
                _CurrentRoad = IsTargetingRoad();
                _CurrentCrossRoad = IsTrargetingCrossRoad();

                if (_CurrentRoad)
                {
                    _RoadEnd = _CurrentRoad.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos));
                    _CurrentRoad.DisableColliders();
                }
                else if (_CurrentCrossRoad)
                    _CurrentCrossRoad.GetComponent<CircleCollider2D>().enabled = false;
                else
                    PlaceRoad();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Road otherRoad = IsTargetingRoad();
            Crossroad crossroad = IsTrargetingCrossRoad();

            if (_CurrentRoad)
                _CurrentRoad.EnableColliders();
            if (_CurrentCrossRoad)
                _CurrentCrossRoad.GetComponent<CircleCollider2D>().enabled = true;

            if (otherRoad && !_CurrentCrossRoad)
            {
                _CurrentRoad.ConnectRoad(otherRoad, otherRoad.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos)), _RoadEnd);
            }
            if (crossroad && !_CurrentCrossRoad && _CurrentRoad)
            {
                _CurrentRoad.ConnectRoad(crossroad, _RoadEnd);
            }

            _CurrentRoad = null;
            _CurrentCrossRoad = null;
        }

        if (Input.GetMouseButtonDown(1) && !_CurrentRoad)
        {
            _CurrentRoad = IsTargetingRoad();
            if (!_CurrentRoad)
                _CurrentRoad = IsTargetingRoadMesh();
            if (_CurrentRoad)
            {
                _CurrentRoad.DisconnectEndPoints();
                Destroy(_CurrentRoad.gameObject);
                _CurrentRoad = null;
            }
        }
    }
    private void UpdateEditorMode()
    {
        if (_CurrentRoad)
        {
            Vector2[] points = _CurrentRoad.endpoints;
            points[_RoadEnd] = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _CurrentRoad.endpoints = points;
        }
        if (_CurrentCrossRoad)
        {
            Vector2 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _CurrentCrossRoad.transform.position = point;
            _CurrentCrossRoad.UpdateCrossroad(true);
        }
    }
    private void UpdateSimulateInput()
    {

    }
    private void UpdateSimulateMode()
    {

    }
    private void UpdateCameraInput()
    {
        if (Input.mouseScrollDelta.y != 0)
            Camera.main.orthographicSize -= Input.mouseScrollDelta.y;

        if (Input.GetMouseButton(2))
            Camera.main.transform.position -= (Vector3)(_CurrentMousePos - _OldMousePos) * 0.1f;
    }

    private Road IsTargetingRoad()
    {
        RaycastHit2D ray = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (ray && ray.collider && ray.collider.gameObject.tag == "Road")
        {
            GameObject road = ray.collider.gameObject;
            return road.GetComponentInParent<Road>();
        }
        return null;
    }
    private Road IsTargetingRoadMesh()
    {
        Vector3 BeginPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        BeginPos.z = 1;
        RaycastHit ray;
        if (Physics.Raycast(BeginPos, Vector3.back, out ray))
            if (ray.collider && ray.collider.gameObject.tag == "Road")
            {
                GameObject road = ray.collider.gameObject;
                return road.GetComponentInParent<Road>();
            }
        return null;
    }
    private Crossroad IsTrargetingCrossRoad()
    {
        RaycastHit2D ray = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (ray && ray.collider && ray.collider.gameObject.tag == "CrossRoad")
        {
            GameObject road = ray.collider.gameObject;
            return road.GetComponentInChildren<Crossroad>();
        }
        return null;
    }

    private void PlaceRoad()
    {
        GameObject road = Instantiate(_RoadBP);
        _CurrentRoad = road.GetComponentInChildren<Road>();

        Vector2[] points = new Vector2[2] { Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero };
        _CurrentRoad.endpoints = points;

        _RoadEnd = 1;
    }

    private bool IsUsingUI()
    {
        return Input.mousePosition.x < _UIBackGround.rectTransform.rect.width && Input.mousePosition.y > Camera.main.pixelHeight - _UIBackGround.rectTransform.rect.height;
    }
}
