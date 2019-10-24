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

    private void Update()
    {
        if(Network.instance.isSimulating)
        {
            float spawninterval = Network.instance.spawnInterval;
            delta += Time.deltaTime;

            if(delta >= spawninterval)
            {
                if (_LastVehicle && Vector2.Distance(_LastVehicle.transform.position, transform.position) < 1)
                    return;

                delta -= spawninterval;

                GameObject vehicle = Instantiate(_VehicleBP);
                vehicle.transform.position = transform.position;
                _ConnectedLane.AddVehicle(vehicle);
                vehicle.GetComponent<Vehicle>().path = Network.instance.CalculatePath(_ConnectedLane._Nodes[0]);

                _LastVehicle = vehicle;
            }
        }
    }
}
