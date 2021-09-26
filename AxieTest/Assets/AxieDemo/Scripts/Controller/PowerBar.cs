using TMPro;
using UnityEngine;

namespace Axie.Core
{
    public class PowerBar : MonoBehaviour
    {
        [SerializeField] private RectTransform bgBar;
        [SerializeField] private RectTransform attackBar;
        [SerializeField] private RectTransform defBar;

        [SerializeField] private TextMeshProUGUI labelAtkCount;
        [SerializeField] private TextMeshProUGUI labelDefCount;

        public void UpdatePower(int atkCount, int defCount)
        {
            var total = atkCount + defCount;

            var atkPercentage = (float)atkCount / (float)total;
            var defPercentage = (float)defCount / (float)total;

            attackBar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, bgBar.rect.width * atkPercentage);
            defBar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, bgBar.rect.width * defPercentage);

            labelAtkCount.text = $"{atkCount}";
            labelDefCount.text = $"{defCount}";
        }
    }
}