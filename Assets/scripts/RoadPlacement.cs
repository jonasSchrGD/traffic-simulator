using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _RoadBP = null;
    [SerializeField]
    private GameObject _SpawnerBP = null;
    [SerializeField]
    private GameObject _EndBP = null;

    private Road _CurrentRoad;
    private Crossroad _CurrentCrossRoad;
    private VehicleSpawner _CurrentSpawner;

    private int _LaneCount = 1;

    [SerializeField]
    Image _UIBackGround = null;

    [SerializeField]
    Toggle _FlowToggle = null;
    [SerializeField]
    Dropdown _BuildingSelection;
    [SerializeField]
    Dropdown _SceneSelection;
    [SerializeField]
    InputField _MaxVelInput = null;
    [SerializeField]
    Text _RoadUI = null;
    [SerializeField]
    InputField _IntervalInput = null;
    [SerializeField]
    Text _SpawnerUI = null;
    [SerializeField]
    InputField _LaneCountInput = null;
    [SerializeField]
    Text _LaneCountUI = null;

    Vector2 _OldMousePos, _CurrentMousePos;

    private int _RoadEnd = 0;
    private int _State = 0;
    bool _Simulate = false;

    private void Start()
    {
        _SceneSelection.value = SceneManager.GetActiveScene().buildIndex;
        Network.instance.Simulate(false);
    }
    private void Update()
    {
        _OldMousePos = _CurrentMousePos;
        _CurrentMousePos = Input.mousePosition;

        UpdateCameraInput();
        if (_State == 3 || Network.instance.isSimulating)
        {
            if (!IsUsingUI())
                UpdateSimulateInput();
            UpdateSimulateMode();
        }
        else
        {
            if (!IsUsingUI())
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
            case 0:
                SelectRoad();
                break;
            case 1:
                SelectSpawner();
                break;
            default:
                break;
        }
    }
    private void UpdateSimulateMode()
    {
        _RoadUI.gameObject.SetActive(Network.instance.isSimulating && _State == 0);
        _SpawnerUI.gameObject.SetActive(Network.instance.isSimulating && _State == 1);
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
        BeginPos.z = -10;
        RaycastHit ray;
        if (Physics.Raycast(BeginPos, Vector3.forward, out ray))
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
            if (_CurrentRoad == null)
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
                    GameObject road = Instantiate(_RoadBP[_LaneCount]);
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

    private void SelectRoad()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _CurrentRoad = IsTargetingRoadEnd();
            if (!_CurrentRoad)
                _CurrentRoad = IsTargetingRoadMesh();

            if (_CurrentRoad)
                _MaxVelInput.text = (_CurrentRoad.maxDrivingSpeed * 3.6f).ToString();
        }
    }
    private void SelectSpawner()
    {
        Road road = IsTargetingRoadEnd();

        if (road)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _CurrentSpawner = road.GetSpawner(road.GetClickedRoadEnd(Camera.main.ScreenToWorldPoint(_CurrentMousePos)));

                if (_CurrentSpawner)
                    _IntervalInput.text = _CurrentSpawner.spawnInterval.ToString();
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
        _State = _BuildingSelection.value;
    }
    public void UpdateRoadVel()
    {
        if (_CurrentRoad)
            _CurrentRoad.maxDrivingSpeed = int.Parse(_MaxVelInput.text) / 3.6f;
    }
    public void UpdateSpawnInterval()
    {
        if (_CurrentSpawner)
            _CurrentSpawner.spawnInterval = float.Parse(_IntervalInput.text);
    }
    public void UpdateLaneCount()
    {
        _LaneCount = int.Parse(_LaneCountInput.text) - 1;
    }
    public void Simulate()
    {
        _Simulate = !_Simulate;
        Network.instance.Simulate(_Simulate);
        if (_Simulate)
        {
            _LaneCountUI.gameObject.SetActive(false);
        }
        else
        {
            _CurrentRoad = null;
            _CurrentCrossRoad = null;
            _CurrentSpawner = null;

            _RoadUI.gameObject.SetActive(false);
            _SpawnerUI.gameObject.SetActive(false);
            _LaneCountUI.gameObject.SetActive(true);
        }
    }
    public void DrawDensity()
    {
        Network.instance.DrawDensity(_FlowToggle.isOn);
    }
    public void UpdateScene()
    {
        if (_SceneSelection.value != -1 && SceneManager.GetActiveScene().buildIndex != _SceneSelection.value)
            SceneManager.LoadScene(_SceneSelection.value);
    }
}
