using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleEnd : MonoBehaviour
{
    bool _RemoveSelf = true;
    public Node _ConnectedNode = null;

    private void Start()
    {
        Network.instance.AddEnd(this);
    }
    private void OnApplicationQuit()
    {
        _RemoveSelf = false;
    }
    private void OnDestroy()
    {
        if (_RemoveSelf)
            Network.instance.RemoveEnd(this);
    }

    public Node GetConnectedNode()
    {
        return _ConnectedNode;
    }
}
