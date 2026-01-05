using System.Collections.Generic;
using UnityEngine;

public class PreprogrammedSliceManager : MonoBehaviour
{
    public List<SlicePositionData> PreprogrammedPositions = new List<SlicePositionData>();

    public float CoordinatePositionMultiplier = 50f;
    public SliceVisualizer SliceVisualizerPF;

    public SliceSolver Solver;

    private void Awake()
    {
        this.SliceVisualizerPF.gameObject.SetActive(false);
    }

    void Start()
    {
        foreach (SlicePositionData preprogrammedPositionSet in this.PreprogrammedPositions)
        {
            preprogrammedPositionSet.BaseColor = Color.white;
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, this.transform);
            newVisualizer.VisualizeList(preprogrammedPositionSet, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);
        }

        this.Solver.StartSolvingForSlices(this.PreprogrammedPositions);
    }
}
