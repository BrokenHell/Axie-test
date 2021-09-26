using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Axie.Core
{
    [CreateAssetMenu(fileName = "AxieCatalog", menuName = "Database/Axie/Axie Catalog", order = 10)]
    public class AxieCatalog : ScriptableObject
    {
        [SerializeField]
        private AxieConfig[] config;

        private Dictionary<string, AxieConfig> configMap = new Dictionary<string, AxieConfig>();

        public Dictionary<string, AxieConfig> Config
        {
            get
            {
                if (config != null && configMap.Count != config.Length)
                {
                    configMap = config.ToDictionary(x => x.id);
                }

                return configMap;
            }
        }

        public AxieConfig GetConfig(string id)
        {
            if (Config.ContainsKey(id))
            {
                return Config[id];
            }

            return null;
        }
    }
}