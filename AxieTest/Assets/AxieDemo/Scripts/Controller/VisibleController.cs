using UnityEngine;

namespace Axie.Core
{
    public class VisibleController : MonoBehaviour
    {
        [SerializeField] private AxieController axieController;
        [SerializeField] private MeshRenderer meshRenderer;

        private void Start()
        {
            if (!IsVisible())
            {
                OnBecameInvisible();
            }
        }

        private void OnBecameVisible()
        {
            //Debug.Log("Visible");
            axieController.Visible();
        }

        private void OnBecameInvisible()
        {
            //Debug.Log("InVisible");
            axieController.Invisible();
        }

        bool IsVisible()
        {
            var plans = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            return GeometryUtility.TestPlanesAABB(plans, meshRenderer.bounds);
        }
    }
}