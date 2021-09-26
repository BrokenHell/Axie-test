using UnityEngine;

namespace Axie.Core.HexMap
{
    /// <summary>
    /// Present for hex cell in game view
    /// </summary>
    public class HexCellController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TMPro.TMP_Text labelQ;
        [SerializeField] private TMPro.TMP_Text labelR;
        [SerializeField] private TMPro.TMP_Text labelS;

        #endregion

        #region Public method

        /// <summary>
        /// Change cell color
        /// </summary>
        /// <param name="color"></param>
        public void ChangeCellColor(Color color)
        {
            spriteRenderer.color = color;
        }

        public void SetHexData(Hex hex)
        {
            labelQ.text = hex.q.ToString();
            labelR.text = hex.r.ToString();
            labelS.text = hex.s.ToString();
        }

        #endregion
    }
}
