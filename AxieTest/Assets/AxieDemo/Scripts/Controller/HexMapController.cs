using System.Collections.Generic;
using UnityEngine;

namespace Axie.Core.HexMap
{
    /// <summary>
    /// Hex map controller to draw and manage hex map data
    /// </summary>
    public class HexMapController : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private float cellWidth;
        [SerializeField]
        private float cellHeight;

        [SerializeField]
        private GameObject cellTemplate;

        private HexMap hexMap;

        private Dictionary<Hex, GameObject> cells = new Dictionary<Hex, GameObject>();

        public HexMap Map => hexMap;

        #endregion

        #region Unity Events

        #endregion

        #region Public Methods

        public void InitMap(int mapRadius)
        {
            this.hexMap = new HexMap(Vector2Int.zero, new Vector2(cellWidth, cellHeight), mapRadius);

            DrawMap();
        }

        public void AddNewCell(Hex cell)
        {
            var position = hexMap.HexToPixel(cell);
            var hexCellImage = GameObject.Instantiate(cellTemplate, this.transform);
            hexCellImage.gameObject.SetActive(true);
            hexCellImage.transform.position = position;
            //hexCellImage.SetHexData(cell);

            cells.Add(cell, hexCellImage);
        }

        public void Increase(List<Hex> addedCells)
        {
            foreach (var cell in addedCells)
            {
                var position = hexMap.HexToPixel(cell);
                var hexCellImage = GameObject.Instantiate(cellTemplate, this.transform);
                hexCellImage.gameObject.SetActive(true);
                hexCellImage.transform.position = position;
                //hexCellImage.SetHexData(cell);

                cells.Add(cell, hexCellImage);
            }
        }

        #endregion

        #region Private Methods

        private void DrawMap()
        {
            foreach (var item in hexMap.cellMap)
            {
                var position = hexMap.HexToPixel(item.Key);
                var hexCellImage = GameObject.Instantiate(cellTemplate, this.transform);
                hexCellImage.gameObject.SetActive(true);
                hexCellImage.transform.position = position;
                //hexCellImage.SetHexData(item.Key);

                cells.Add(item.Key, hexCellImage);
            }
        }

        #endregion
    }
}
