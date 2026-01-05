using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliceVisualizer : MonoBehaviour
{
    public SlicePositionData SelectedPixels;

    private List<Image> pixels { get; set; } = new List<Image>();
    private Dictionary<Vector2Int, Image> coordinatesToPixel { get; set; } = new Dictionary<Vector2Int, Image>();

    public Image PixelPF;

    private void Awake()
    {
        this.PixelPF.gameObject.SetActive(false);
    }

    public void VisualizeList(SlicePositionData list, float coordinatePositionMultiplier)
    {
        // Clear previous visualization
        for (int ii = this.pixels.Count - 1; ii >= 0; ii--)
        {
            Destroy(this.pixels[ii].gameObject);
        }

        this.coordinatesToPixel.Clear();
        this.SelectedPixels = list;

        foreach (Vector2Int pixelPosition in list.Positions)
        {
            Image thisPixel = Instantiate(PixelPF, this.transform);
            thisPixel.color = list.BaseColor;
            thisPixel.rectTransform.sizeDelta = new Vector2(coordinatePositionMultiplier, coordinatePositionMultiplier);
            thisPixel.transform.localPosition = new Vector3(pixelPosition.x, pixelPosition.y, 0) * coordinatePositionMultiplier;
            thisPixel.gameObject.SetActive(true);
            this.pixels.Add(thisPixel);

            if (!this.coordinatesToPixel.ContainsKey(pixelPosition))
            {
                this.coordinatesToPixel.Add(pixelPosition, thisPixel);
            }
        }
    }

    public void IntegrateSolution(SlicePositionData toIntegrate)
    {
        foreach (Vector2Int coordinate in toIntegrate.Positions)
        {
            this.coordinatesToPixel[coordinate].color = toIntegrate.BaseColor;
        }
    }
}
