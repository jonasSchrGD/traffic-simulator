using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField]
    private GameObject _RoadBP = null;
    [SerializeField]
    private GameObject _SpawnerBP = null;
    [SerializeField]
    private GameObject _EndBP = null;

    private Road _CurrentRoad;
    private Crossroad _CurrentCrossRoad;
    private VehicleSpawner _CurrentSpawner;

    [SerializeField]
    Image _UIBackGround = null;

    [SerializeField]
    Dropdown _BuildingSelection;

    Vector2 _OldMousePos, _CurrentMousePos;

    private int _RoadEnd = 0;
    private int _State = 0;

    private void Update()
    {
        _OldMousePos = _CurrentMousePos;
        _CurrentMousePos = Input.mousePosition;

        UpdateCameraInput();
        if (_State == 3 || Network.instance.isSimulating)
        {
            UpdateSimulateInput();
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
        switch (_State)
        {
            case 0:
                PlaceRoad();
                break;
            case 1:
                PlaceSpawner();
                break;
            case 2:
                PlaceEnd();
                break;
            default:
                break;
        }
    }
    private void UpdateEditorMode()
    {
        Vector2 newPos = Camera.main.ScreenToWorldPoint(_CurrentMousePos);
        if (Input.GetKey(KeyCode.LeftControl))
        {
            newPos.x = Mathf.Round(newPos.x);
            newPos.y = Mathf.Round(newPos.y);
        }
        if (_CurrentRoad)
        {
            Vector2[] points = _CurrentRoad.endpoints;
            points[_RoadEnd] = newPos;
            _CurrentRoad.endpoints = points;
        }
        if (_CurrentCrossRoad)
        {
            Vector2 point = newPos;
            _CurrentCrossRoad.transform.position = point;
            _CurrentCrossRoad.UpdateCrossroad(true);
        }
    }

    private void UpdateSimulateInput()
    {
        switch (_State)
        {
            case 1:
                PlaceSpawner();
                break;
            case 2:
                PlaceEnd();
                break;
            default:
                break;
        }
    }
    private void UpdateSimulateMode()
    {

    }

    private void UpdateCameraInput()
    {
        if (Input.mouseScrollDelta.y != 0 && Input.mousePosition.x >=  0 && Input.mousePosition.y >= 0 && Input.mousePosition.x <= Camera.main.pixelWidth && Input.mousePosition.y <= Camera.main.pixelHeight)
            Camera.main.orthographicSize -= Input.mouseScrollDelta.y;

        if (Input.GetMouseButton(2))
            Camera.main.transform.position -= (Vector3)(_CurrentMousePos - _OldMousePos) * 0.1f;
    }

    private Road IsTargetingRoadEnd()
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
    private Crossroad IsTargetingCrossRoad()
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
        if (Input.GetMouseButtonDown(0))
        {
            if (_CurrentRoad == null && !IsUsingUI())
            {
                _CurrentRoad = IsTargetingRoadEnd();
                _CurrentCrossRoad = IsTargetingCrossRoad();

                if (_CurrentRoad)
                {
                    _RoadEnd = _CurrentRoad.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos));
                    _CurrentRoad.DisableColliders();
                }
                else if (_CurrentCrossRoad)
                    _CurrentCrossRoad.GetComponent<CircleCollider2D>().enabled = false;
                else
                {
                    GameObject road = Instantiate(_RoadBP);
                    _CurrentRoad = road.GetComponentInChildren<Road>();

                    Vector2[] points = new Vector2[2] { Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero };
                    _CurrentRoad.endpoints = points;

                    _RoadEnd = 1;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Road otherRoad = IsTargetingRoadEnd();
            Crossroad crossroad = IsTargetingCrossRoad();

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
            _CurrentRoad = IsTargetingRoadEnd();
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
    private void PlaceSpawner()
    {
        Road road = IsTargetingRoadEnd();

        if (road)
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameObject spawner = Instantiate(_SpawnerBP);
                if (!road.ConnectSpawner(spawner, road.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos))))
                    Destroy(spawner);
            }
            if (Input.GetMouseButtonDown(1))
            {
                road.RemoveSpawner(road.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos)));
            }
        }
    }
    private void PlaceEnd()
    {
        Road road = IsTargetingRoadEnd();

        if (road)
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameObject end = Instantiate(_EndBP);
                if (!road.ConnectEnd(end, road.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos))))
                    Destroy(end);
            }
            if (Input.GetMouseButtonDown(1))
            {
                road.RemoveEnd(road.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos)));
            }
        }
    }

    //ui functions
    private bool IsUsingUI()
    {
        return Input.mousePosition.x < _UIBackGround.rectTransform.rect.width;
    }
    public void BuildingChanged()
    {
        switch (_BuildingSelection.captionText.text)
        {
            case "road":
                _State = 0;
                break;
            case "spawn":
                _State = 1;
                break;
            case "end":
                _State = 2;
                break;
            default:
                _State = -1;
                break;
        }
    }
}
