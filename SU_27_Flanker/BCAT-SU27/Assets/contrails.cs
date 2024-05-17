using System;
using UnityEngine;

public class contrails : MonoBehaviour
{
    public FlightInfo flightinfo;

    public ObjectToggler contrailObject;

    // Update is called once per frame
    void Update()
    {
        if ((bool)flightinfo && (bool)contrailObject)
        {
            if(flightinfo.altitudeASL > 10000f)
            {
                contrailObject.SetObjectActive(1);
            }
            else if (flightinfo.altitudeASL < 10000f)
            {
                contrailObject.SetObjectActive(0);
            }

        }
    }
}
