using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole.Editor
{
    [CreateAssetMenu(menuName = "Mobile Console/GUIDMappingSet", fileName = "GUIDMappingSet")]
    public class GUIDMappingSet : ScriptableObject
    {
        [System.Serializable]
        public class GUIDMapping
        {
            public string oldGUID;
            public string newGUID;
        }

        public List<GUIDMapping> guidMappings;
    }
}