using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomAircraftTemplateSU27
{
    public class AircraftInfo
    {

        //READ ME, IMPORTANT!!!!!!!!
        //You must change HarmonyId in order for your custom aircraft mod to be compatible with other aircraft mods
        public const string HarmonyId = "luluj.su27";

        
        
        //The name of the aircraft specified in the External Vehicle Info Component
       
        public const string vehicleName = "SU-27 Flanker";
       
        //Names of the prefab name you created and the name of the Assetbundle that you created
        public const string AircraftAssetbundleName = "su27";
        public const string AircraftPrefabName = "SU27"; 


    }
}
