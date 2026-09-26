using UnityEngine;

// ScriptableObject that holds a sequence of story panels (shown before a zone loads, on failure,
// during the gate finale, or as the epilogue).
// Create via Assets > Create > CovenantNight > StoryBeatData.
[CreateAssetMenu(menuName = "CovenantNight/StoryBeatData")]
public class StoryBeatData : ScriptableObject
{
    public StoryBeat[] beats;
}
