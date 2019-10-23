using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadStructure : MonoBehaviour
{
    protected List<Lane> _Links = new List<Lane>();
    private bool _Quitting = false;

    protected int AddLink(Lane link)
    {
        _Links.Add(link);
        Network.instance.AddLink(link);
        return _Links.Count - 1;
    }

    protected void ClearLinks()
    {
        foreach(Lane link in _Links)
        {
            Network.instance.RemoveLink(link);
            link._Nodes[0].connectedLanes.Remove(link);
        }
        _Links.Clear();
    }

    private void OnApplicationQuit()
    {
        _Quitting = true;
    }

    private void OnDestroy()
    {
        if (_Quitting)
            return;

        for (int i = 0; i < _Links.Count; i++)
        {
            Network.instance.RemoveLink(_Links[i]);
            _Links[i] = null;
        }
    }
}
