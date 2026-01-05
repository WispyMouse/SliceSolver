using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public class SliceSolver : MonoBehaviour
{
    public float CoordinatePositionMultiplier = 50f;
    public SliceVisualizer SliceVisualizerPF;

    public Transform IntegratedSolutionsRoot;

    public List<Color> CandidateColors = new List<Color>();

    public async Task StartSolvingForSlices(List<SlicePositionData> problemsToSolve)
    {
        // Given a list of all visuals to achieve, determine the least number of slices we would require to visualize everything in this system
        // We can have slices using the same coordinates; if any coordinate in a slice combination is used, it will be on.
        // There are no negatives permitted

        // First, let's establish the most fundamental building blocks
        List<SlicePositionData> allFundamentals = new List<SlicePositionData>();
        await Task.Run(() =>
        {
            HashSet<Vector2Int> fundamentalCoordinates = new HashSet<Vector2Int>();
            foreach (SlicePositionData sliceToVisualize in problemsToSolve)
            {
                foreach (Vector2Int coordinate in sliceToVisualize.Positions)
                {
                    fundamentalCoordinates.Add(coordinate);
                }
            }

            foreach (Vector2Int fundamental in fundamentalCoordinates)
            {
                allFundamentals.Add(new SlicePositionData() { Positions = new List<Vector2Int>() { fundamental } });
            }
        });

        List<SlicePositionData> distinctFundementals = RemoveIdenticalSlices(allFundamentals);
        List<SlicePositionData> reducedPairedList = ReduceToPairedPositions(distinctFundementals, problemsToSolve);
        this.TagUnbreakableFundamentals(reducedPairedList, problemsToSolve);

        // We know the fundamental parts that must exist, so strip those elements from other sets
        reducedPairedList = RemoveUnbreakableFundamentals(reducedPairedList);

        List<SlicePositionData> solvedList = await SolveAsync(reducedPairedList, problemsToSolve);

        if (CanMakeAllShapes(problemsToSolve, solvedList, out Dictionary<SlicePositionData, List<SlicePositionData>> sliceSolutions))
        {
            PresentResults(solvedList, problemsToSolve, sliceSolutions);
        }

        Debug.Log($"Program complete");
    }

    public bool CanMakeAllShapes(List<SlicePositionData> slicesToMake, List<SlicePositionData> parts, out Dictionary<SlicePositionData, List<SlicePositionData>> sliceToSolutions)
    {
        sliceToSolutions = new Dictionary<SlicePositionData, List<SlicePositionData>>();

        foreach (SlicePositionData sliceToMake in slicesToMake)
        {
            if (sliceToMake.CanMakeShape(parts, out List<SlicePositionData> solutionPieces))
            {
                sliceToSolutions.Add(sliceToMake, solutionPieces);
                continue;
            }

            sliceToSolutions = null;
            return false;
        }

        return true;
    }

    Color GetCandidateColor(int index)
    {
        if (this.CandidateColors.Count == 0)
        {
            return Color.white;
        }

        return this.CandidateColors[Mathf.Max(0, index % this.CandidateColors.Count)];
    }

    public void PresentResults(List<SlicePositionData> solutionSlices, List<SlicePositionData> slicesToSolve, Dictionary<SlicePositionData, List<SlicePositionData>> sliceSolutions)
    {
        for (int ii = 0; ii < solutionSlices.Count; ii++)
        {
            SlicePositionData solutionSet = solutionSlices[ii];
            solutionSet.BaseColor = GetCandidateColor(ii);
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, this.transform);
            newVisualizer.VisualizeList(solutionSet, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);
        }

        foreach (SlicePositionData toSolve in slicesToSolve)
        {
            SliceVisualizer newVisualizer = Instantiate(this.SliceVisualizerPF, IntegratedSolutionsRoot);
            newVisualizer.VisualizeList(toSolve, this.CoordinatePositionMultiplier);
            newVisualizer.gameObject.SetActive(true);

            List<SlicePositionData> sortedSolutions = sliceSolutions[toSolve];
            // Sort so that the largest slice is colored first, then the smaller slice on top
            sortedSolutions.Sort((SlicePositionData a, SlicePositionData b) => b.Positions.Count.CompareTo(a.Positions.Count));

            foreach (SlicePositionData solutionPiece in sortedSolutions)
            {
                newVisualizer.IntegrateSolution(solutionPiece);
            }
        }
    }

    /// <summary>
    // Looking at every fundamental and every position to solve, are there any elements that are *always* tied together?
    // If we can identify any pieces that are always linked, we can reduce the amount of fundamental pieces
    // This filter is helpful for reconciling things like the "-" in the middle of an "8", which are two pieces always found together
    /// </summary>
    public List<SlicePositionData> ReduceToPairedPositions(List<SlicePositionData> allFundamentals, List<SlicePositionData> toSolve)
    {
        Debug.Log($"Beginning linking of all fundamentals, size {allFundamentals.Count}");

        List<SlicePositionData> currentLinkedPieces = new List<SlicePositionData>();
        foreach (SlicePositionData fundamental in allFundamentals)
        {
            List<SlicePositionData> problemsInvolvingFundamental = new List<SlicePositionData>(toSolve);
            for (int ii = toSolve.Count - 1; ii >= 0; ii--)
            {
                if (!toSolve[ii].ContainsAll(fundamental))
                {
                    problemsInvolvingFundamental.RemoveAt(ii);
                }
            }

            HashSet<Vector2Int> allPositionsInProblems = new HashSet<Vector2Int>();
            foreach (SlicePositionData position in problemsInvolvingFundamental)
            {
                allPositionsInProblems.UnionWith(position.Positions);
            }

            List<Vector2Int> alwaysPresentCoordinates = new List<Vector2Int>(allPositionsInProblems);

            Debug.Log($"For the fundemental {fundamental}, there are {allPositionsInProblems.Count} positions in problems involving this fundemental");

            foreach (SlicePositionData curToSolve in problemsInvolvingFundamental)
            {
                if (!curToSolve.ContainsAll(fundamental))
                {
                    continue;
                }

                // Take this problem that involves this coordinate,
                // and remove everything from our list of all coordinates that are not present in this problem
                // In the end we'll be left with a list that only contains linked items
                for (int ii = alwaysPresentCoordinates.Count - 1; ii >= 0; ii--)
                {
                    Vector2Int thisCoordinate = alwaysPresentCoordinates[ii];
                    if (!curToSolve.Positions.Contains(thisCoordinate))
                    {
                        alwaysPresentCoordinates.RemoveAt(ii);
                    }
                }
            }

            Debug.Log($"For the fundemental {fundamental}, there appear to be {alwaysPresentCoordinates.Count} coordinates that always appear when it does");

            if (alwaysPresentCoordinates.Count == 0)
            {
                Debug.LogError($"Somehow, a fundamental isn't present in anything. That's odd!");
                continue;
            }

            // For each always present coordinate, ensure that every place it shows up, the base fundamental shows up
            for (int ii = alwaysPresentCoordinates.Count - 1; ii >= 0; ii--)
            {
                Vector2Int currentCoordinate = alwaysPresentCoordinates[ii];

                // If this coordinate is part of the fundamental, it should always be linked to itself
                if (fundamental.Positions.Contains(currentCoordinate))
                {
                    continue;
                }

                foreach (SlicePositionData curToSolve in toSolve)
                {
                    // If this slice contains the fundamental, but not the current slice we're trying to add,
                    // then this fundamental must not actually be linked to this slice
                    if (curToSolve.ContainsAll(fundamental) != curToSolve.Positions.Contains(currentCoordinate))
                    {
                        Debug.Log($"Fundamental {fundamental} appeared to be linked to {currentCoordinate}, but it wasn't actually present in {curToSolve}. Removing link.");
                        alwaysPresentCoordinates.RemoveAt(ii);
                        break;
                    }
                }
            }

            // Whatever coordinates remain, these must be tied coordinates
            SlicePositionData linked = new SlicePositionData() {  Positions = alwaysPresentCoordinates, BaseColor = fundamental.BaseColor };
            if (!linked.IsAlreadyInList(currentLinkedPieces))
            {
                Debug.Log($"Linking {fundamental} to {linked}");
                currentLinkedPieces.Add(linked);
            }
        }

        Debug.Log($"After linking, {currentLinkedPieces.Count} linked pieces remain.");

        return currentLinkedPieces;
    }

    /// <summary>
    /// Iterate over every paired fundamental, and tag the ones that have something to solve that *exactly* matches that fundamental.
    /// This informs that we can't possibly optimize out that one.
    /// </summary>
    public void TagUnbreakableFundamentals(List<SlicePositionData> pairedFundamentals, List<SlicePositionData> toSolve)
    {
        foreach (SlicePositionData pairedFundamental in pairedFundamentals)
        {
            foreach (SlicePositionData curToSolve in toSolve)
            {
                if (curToSolve.CanMakeShape(pairedFundamental))
                {
                    Debug.Log($"{pairedFundamental} is marked as an unbreakable fundamental.");
                    pairedFundamental.UnbreakableFundamental = true;
                    break;
                }
            }
        }
    }

    public async Task<List<SlicePositionData>> SolveAsync(List<SlicePositionData> pairedFundamentals, List<SlicePositionData> toSolve)
    {
        List<SlicePositionData> sliceSolutions = new List<SlicePositionData>();

        List<SlicePositionData> unbreakableSlices = new List<SlicePositionData>();
        List<SlicePositionData> breakableSlices = new List<SlicePositionData>();

        await Task.Run(() =>
        {
            for (int ii = pairedFundamentals.Count - 1; ii >= 0; ii--)
            {
                if (pairedFundamentals[ii].UnbreakableFundamental)
                {
                    unbreakableSlices.Add(pairedFundamentals[ii]);

                    // We know our best solution will include all fundamentals
                    sliceSolutions.Add(pairedFundamentals[ii]);
                }
                else
                {
                    breakableSlices.Add(pairedFundamentals[ii]);
                }
            }
        });

        foreach (SlicePositionData problemToSolve in toSolve)
        {
            if (problemToSolve.CanMakeShape(breakableSlices, out List<SlicePositionData> usedSlices))
            {
                SlicePositionData composite = new SlicePositionData(usedSlices);
                sliceSolutions.Add(composite);
            }
        }

        // We now have every possible solution for every possible entry
        // Check to see if we can prune any combination that can be made with other combination pieces
        sliceSolutions = RemoveIdenticalSlices(sliceSolutions);
        sliceSolutions = ReduceRedundantSlices(sliceSolutions);
        sliceSolutions = FilterForIdenticalSolvers(sliceSolutions, toSolve);

        // Take any completely overlapping slices and reduce them
        sliceSolutions = ReduceOverlappingSlices(sliceSolutions, toSolve);

        return sliceSolutions;
    }

    public static List<SlicePositionData> RemoveUnbreakableFundamentals(List<SlicePositionData> toBreak)
    {
        List<SlicePositionData> remainingPositions = new List<SlicePositionData>();

        for (int ii = 0; ii < toBreak.Count; ii++)
        {
            SlicePositionData curPosition = toBreak[ii];

            if (curPosition.UnbreakableFundamental)
            {
                continue;
            }

            // Look through each other element and find unbreakable fundamentals
            // Remove the coordinates from this set that are in each fundamental set
            int removals = 0;
            for (int jj = 0; jj < toBreak.Count; jj++)
            {
                if (ii == jj)
                {
                    continue;
                }

                SlicePositionData subPosition = toBreak[jj];
                if (!subPosition.UnbreakableFundamental)
                {
                    continue;
                }

                for (int kk = 0; kk < curPosition.Positions.Count; kk++)
                {
                    if (subPosition.Positions.Contains(curPosition.Positions[kk]))
                    {
                        curPosition.Positions.RemoveAt(kk);
                        removals++;
                    }
                }
            }

            if (removals > 0)
            {
                Debug.Log($"Removed {removals} fundamental elements, resulting in {curPosition}");
            }

            // Then if any positions are remaining, keep it
            if (curPosition.Positions.Count > 0)
            {
                remainingPositions.Add(curPosition);
            }
            else
            {
                Debug.Log($"After removing fundamentals, there was nothing in {curPosition}.");
            }
        }

        return remainingPositions;
    }

    public static List<SlicePositionData> RemoveIdenticalSlices(List<SlicePositionData> toTrim)
    {
        List<SlicePositionData> remaining = new List<SlicePositionData>(toTrim);

        for (int ii = remaining.Count - 1; ii >= 0; ii--)
        {
            SlicePositionData thisSlice = remaining[ii];
            for (int kk = ii - 1; kk >= 0; kk--)
            {
                if (ii == kk)
                {
                    continue;
                }

                SlicePositionData comparisonSlice = remaining[kk];

                if (comparisonSlice.Positions.Count == thisSlice.Positions.Count && comparisonSlice.ContainsAll(thisSlice))
                {
                    // This is an identical slice! Remove it, retaining the other copy
                    // Debug.Log($"Found an identical copy of {comparisonSlice}; culling");
                    remaining.RemoveAt(ii);
                    break;
                }
            }
        }

        return remaining;
    }

    public static List<SlicePositionData> ReduceRedundantSlices(List<SlicePositionData> solutionSlices)
    {
        List<SlicePositionData> necessarySlices = new List<SlicePositionData>();
        List<SlicePositionData> remainingSlices = new List<SlicePositionData>(solutionSlices);

        bool anythingChanged = false;
        do
        {
            anythingChanged = false;
            necessarySlices.Clear();
            for (int ii = remainingSlices.Count - 1; ii >= 0; ii--)
            {
                SlicePositionData mySlice = remainingSlices[ii];

                // Find every slice that this overlaps entirely
                List<SlicePositionData> encompassedSlices = new List<SlicePositionData>();
                for (int jj = 0; jj < remainingSlices.Count; jj++)
                {
                    if (ii == jj)
                    {
                        continue;
                    }

                    SlicePositionData subSlice = remainingSlices[jj];
                    if (mySlice.ContainsAll(subSlice))
                    {
                        encompassedSlices.Add(subSlice);
                    }
                }

                // If there are any encompassed slices, can we make this shape with them?
                if (mySlice.CanMakeShape(encompassedSlices, out List<SlicePositionData> usedSlices))
                {
                    // Yes! That means this can be removed
                    Debug.Log($"Can make {mySlice} with composite parts, marking as not necessary.");
                    anythingChanged = true;
                    remainingSlices.RemoveAt(ii);
                    break;
                }

                // If we got here, we can't make this out of other parts
                necessarySlices.Add(mySlice);
            }
        } while (anythingChanged);

        return necessarySlices;
    }

    /// <summary>
    /// Given a set of solutions, identify all problems they can be used to solve.
    /// If there are any smaller sections with entirely identical solution sets to a larger thing,
    /// and the smaller set has no solutions that are unique to it,
    /// remove the smaller set.
    /// </summary>
    List<SlicePositionData> FilterForIdenticalSolvers(List<SlicePositionData> solutionSlices, List<SlicePositionData> problemsToSolve)
    {
        List<SlicePositionData> remaining = new List<SlicePositionData>(solutionSlices);

        bool anythingChanged = false;
        do
        {
            anythingChanged = false;
            for (int ii = solutionSlices.Count - 1; ii >= 0; ii--)
            {
                SlicePositionData curSlice = solutionSlices[ii];

                for (int jj = solutionSlices.Count - 1; jj >= 0; jj--)
                {
                    if (ii == jj)
                    {
                        continue;
                    }

                    SlicePositionData subSlice = solutionSlices[jj];

                    // Check that our slice is a superset of the smaller slice
                    if (!curSlice.ContainsAll(subSlice))
                    {
                        continue;
                    }

                    // Now that we know it's a subset, check every problem for a
                    // situation where the smaller slice is used in a way that the larger one isn't
                    bool anySmallerSituationsFound = false;
                    foreach (SlicePositionData problem in problemsToSolve)
                    {
                        if (!problem.ContainsAll(curSlice) && problem.ContainsAll(subSlice))
                        {
                            anySmallerSituationsFound = true;
                            break;
                        }
                    }

                    // If the smaller slice is entirely unimportant, trim it
                    if (!anySmallerSituationsFound)
                    {
                        Debug.Log($"Set {curSlice} is a superset of {subSlice}, and the subset has no unique solutions, so trimming it.");
                        solutionSlices.RemoveAt(jj);
                        anythingChanged = true;
                        break;
                    }
                }

                if (anythingChanged)
                {
                    break;
                }
            }
            anythingChanged = false;
        } while (anythingChanged);

        return remaining;
    }

    /// <summary>
    /// Given a set of slices, remove any subsets that are a part of supersets.
    /// This helps where a slice would completely eclipse another; that's a sign that we have a redundant part.
    /// Doing multiple iterations will result in eroding the dataset down to fundamental parts, so run carefully.
    /// </summary>
    public static List<SlicePositionData> ReduceOverlappingSlices(List<SlicePositionData> slices, List<SlicePositionData> toSolve)
    {
        List<SlicePositionData> reduced = new List<SlicePositionData>();
        List<SlicePositionData> remaining = new List<SlicePositionData>(slices);

        bool anythingChanged = false;
        do
        {
            reduced.Clear();
            anythingChanged = false;
            // Sort smallest to largest, then traverse backwards
            remaining.Sort((SlicePositionData a, SlicePositionData b) => a.Positions.Count.CompareTo(b.Positions.Count));

            for (int ii = remaining.Count - 1; ii >= 0; ii--)
            {
                SlicePositionData thisSlice = remaining[ii];

                // For the sub list, traverse smallest to largest
                for (int jj = 0; jj < remaining.Count; jj++)
                {
                    if (ii == jj)
                    {
                        continue;
                    }

                    SlicePositionData subSlice = remaining[jj];

                    if (thisSlice.Positions.Count <= subSlice.Positions.Count)
                    {
                        // Can't possibly overlap if this is smaller or the same size
                        continue;
                    }

                    if (!thisSlice.ContainsAll(subSlice))
                    {
                        continue;
                    }

                    // We now know that the sub slice contains all of the slice
                    // If there are any solutions that require the smaller slice, but do not work for the larger slice,
                    // we should trim the larger slice down to remove the smaller slice's contents
                    // If we can see that every place that uses the smaller slice *also* uses the larger slice,
                    // then we can remove the smaller slice
                    // So get every problem to solve that includes this larger and this smaller slice

                    List<SlicePositionData> problemsToSolveInvolvingOuterSlice = new List<SlicePositionData>();
                    List<SlicePositionData> problemsToSolveInvolvingSubSlice = new List<SlicePositionData>();

                    foreach (SlicePositionData problem in toSolve)
                    {
                        if (problem.ContainsAll(subSlice))
                        {
                            problemsToSolveInvolvingSubSlice.Add(problem);
                        }

                        if (problem.ContainsAll(thisSlice))
                        {
                            problemsToSolveInvolvingOuterSlice.Add(problem);
                        }
                    }

                    if (problemsToSolveInvolvingOuterSlice.Count == problemsToSolveInvolvingSubSlice.Count)
                    {
                        // If these are all used on the same problems, we can trim the *smaller* slice
                        Debug.Log($"{thisSlice} is used in all of the same problems as {subSlice}, so retaining the larger slice.");
                        remaining.Remove(subSlice);
                        reduced.Add(thisSlice);
                    }
                    else
                    {
                        // Otherwise, trim the larger one to remove the smaller one's elements
                        Debug.Log($"Removing redundant overlapping slice parts from {thisSlice} ({thisSlice.Positions.Count} coordinates) by cutting out {subSlice} ({subSlice.Positions.Count} coordinates)");
                        foreach (Vector2Int position in subSlice.Positions)
                        {
                            thisSlice.Positions.Remove(position);
                        }

                        if (thisSlice.Positions.Count > 0)
                        {
                            reduced.Add(thisSlice);
                        }
                        else
                        {
                            remaining.Remove(thisSlice);
                        }
                    }

                    anythingChanged = true;
                    break;
                }

                if (anythingChanged)
                {
                    remaining = RemoveIdenticalSlices(remaining);
                    break;
                }
                else
                {
                    // Nothing changed, so this must be a good fit
                    reduced.Add(thisSlice);
                }
            }
        } while (anythingChanged);
        
        return reduced;
    }

    public static IEnumerable<T[]> GetAllSubsets<T>(List<T> source)
    {
        // Shamefully stolen from Google's AI solver
        // WARNING: BREAKS WITH SET SIZES OF SIZE 32 OR GREATER
        // 1 << source.Count calculates 2^n efficiently
        for (int i = 0; i < (1 << source.Count); i++)
        {
            yield return source
                .Where((t, j) => (i & (1 << j)) != 0) // Check if the j-th bit is set in i
                .ToArray();
        }
    }
}
