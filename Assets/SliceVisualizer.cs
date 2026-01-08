using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SliceVisualizer : MonoBehaviour
{
    public Color EnabledColorMin = Color.lightGray;
    public Color EnabledColorMax = Color.lightGray;
    public int ShapesForMinColor = 1;
    public int ShapesForMaxColor = 6;

    public Color DisabledColorMin = Color.darkGray;
    public Color DisabledColorMax = Color.darkGray;

    public Image ButtonImage;
    public SlicePositionData SelectedPixels;

    public TMP_Text ShapeCount;

    public List<SliceVisualizer> AssociatedPixels { get; private set; } = new List<SliceVisualizer>();

    public List<Image> Pixels { get; private set; } = new List<Image>();
    private Dictionary<Vector2Int, Image> coordinatesToPixel { get; set; } = new Dictionary<Vector2Int, Image>();

    public Image PixelPF;

    public delegate void RecalculateFunctionCall();
    public RecalculateFunctionCall RecalculateCall;

    public bool IsOn { get; set; } = true;

    private void Awake()
    {
        this.PixelPF.gameObject.SetActive(false);
        this.ShapeCount.enabled = false;
    }

    public void VisualizeList(SlicePositionData list, float coordinatePositionMultiplier)
    {
        // Clear previous visualization
        for (int ii = this.Pixels.Count - 1; ii >= 0; ii--)
        {
            Destroy(this.Pixels[ii].gameObject);
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
            this.Pixels.Add(thisPixel);

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

    public void ToggleVisual()
    {
        this.ToggleVisual(!this.IsOn);
    }

    public void ToggleVisual(bool toValue)
    {
        if (!this.AssociatedPixels.Any())
        {
            return;
        }

        this.IsOn = toValue;

        this.SetColor();

        foreach (SliceVisualizer dependentSlice in this.AssociatedPixels)
        {
            dependentSlice.IsOn = false;
        }

        this.RecalculateCall?.Invoke();
    }

    public void SetColor()
    {
        this.ButtonImage.color = IsOn ? Color.Lerp(this.EnabledColorMin, this.EnabledColorMax, Mathf.InverseLerp(this.ShapesForMinColor, this.ShapesForMaxColor, this.AssociatedPixels.Count))
            : Color.Lerp(this.DisabledColorMin, this.DisabledColorMax, Mathf.InverseLerp(this.ShapesForMinColor, this.ShapesForMaxColor, this.AssociatedPixels.Count));
        this.ShapeCount.transform.SetAsLastSibling();
    }

    public void Clear()
    {
        for (int ii = this.Pixels.Count - 1; ii >= 0; ii--)
        {
            this.Pixels[ii].color = Color.white;
        }
    }
}
