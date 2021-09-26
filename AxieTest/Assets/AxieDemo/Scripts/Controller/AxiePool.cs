using System.Collections.Generic;
using UnityEngine;

namespace Axie.Core
{
    public class AxiePool
    {
        private GameObject prefab;
        private Transform parent;
        private List<GameObject> pools = new List<GameObject>();

        public void Init(GameObject prefab, int count, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < count; ++i)
            {
                var obj = NewItem();
                obj.SetActive(false);
                pools.Add(obj);
            }
        }

        public GameObject GetItem()
        {
            if (pools.Count == 0)
            {
                return NewItem();
            }

            var obj = pools[pools.Count - 1];
            obj.SetActive(true);
            pools.RemoveAt(pools.Count - 1);
            return obj;
        }

        public void Recycle(GameObject obj)
        {
            obj.SetActive(false);
            pools.Add(obj);
        }

        public GameObject NewItem()
        {
            var go = GameObject.Instantiate(prefab, parent);
            return go;
        }
    }
}