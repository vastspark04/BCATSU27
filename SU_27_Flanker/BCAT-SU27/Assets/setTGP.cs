using System;
using UnityEngine;

public class setTGP : MonoBehaviour
{
    public MFDRadarUI mfdRadarUI;
    public TargetingMFDPage tgpPage;

    // Update is called once per frame
    void Update()
    {
        if((bool)mfdRadarUI && !(bool)mfdRadarUI.tgp)
        {
            if((bool)tgpPage && (bool)tgpPage.opticalTargeter)
            {
                mfdRadarUI.tgp = tgpPage.opticalTargeter;
            }
            
        }
    }
}
