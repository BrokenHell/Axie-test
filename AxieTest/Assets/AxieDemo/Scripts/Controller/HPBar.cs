using DG.Tweening;
using UnityEngine;

namespace Axie.Core
{
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private Transform filler;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Color fullColor;
        [SerializeField] private Color weakColor;
        [SerializeField] private Color nearDeathColor;

        public void SetValue(int current, int max)
        {
            current = Mathf.Max(0, current);
            var value = (float)current / (float)max;
            filler.transform.localScale = new Vector3(value, 1, 1);
            if (value <= 0.5f && value > 0.2f)
            {
                background.color = weakColor;
            }
            else if (value <= 0.2f)
            {
                background.color = nearDeathColor;
            }
            else if (value > 0.5f)
            {
                background.color = fullColor;
            }
        }

        public void SetValueAnimated(int current, int max)
        {
            current = Mathf.Max(0, current);
            float newValue = (float)current / (float)max;
            filler.DOKill();
            filler.transform.DOKill();
            filler.transform.DOScaleX(newValue, 0.5f);
            if (newValue <= 0.5f && newValue > 0.2f)
            {
                background.DOColor(weakColor, 0.4f);
            }
            else if (newValue <= 0.2f)
            {
                background.DOColor(nearDeathColor, 0.4f);
            }
        }
    }
}
