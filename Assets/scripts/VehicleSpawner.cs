using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject _VehicleBP = null;
    GameObject _LastVehicle = null;

    public Lane _ConnectedLane = null;
    float delta = 0;
    bool _RemoveSelf = true;

    private void Start()
    {
        Network.instance.AddSpawner(this);
    }
    private void OnApplicationQuit()
    {
        _RemoveSelf = false;
    }
    private void OnDestroy()
    {
        if (_RemoveSelf)
            Network.instance.RemoveSpawner(this);
    }

    public float spawnInterval = 3;
    private void Update()
    {
        if (Network.instance.isSimulating && Network.instance.hasEnd)
        {
            delta += Time.deltaTime;

            if(delta >= spawnInterval)
            {
                delta -= spawnInterval;
                SpawnVehicle();
            }
        }
    }
    public bool SpawnVehicle()
    {
        if (_LastVehicle && Vector2.Distance(_LastVehicle.transform.position, transform.position) < 2)
            return false;

        List<Lane> path = Network.instance.CalculatePath(_ConnectedLane._Nodes[0]);

        if (path == null)
            return false;

        GameObject vehicle = Instantiate(_VehicleBP);
        vehicle.transform.position = transform.position;
        _ConnectedLane.AddVehicle(vehicle);
        vehicle.GetComponent<Vehicle>().path = path;

        _LastVehicle = vehicle;
        return true;
    }
}
