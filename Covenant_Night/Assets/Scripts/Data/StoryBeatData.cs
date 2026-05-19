using UnityEngine;

// ScriptableObject that holds the story panels shown before a zone loads.
// Create via Assets > Create > CovenantNight > StoryBeatData.
// Assign one per zone in ZoneManager.zoneEntryBeats, and to GateFinalSequence.
[CreateAssetMenu(menuName = "CovenantNight/StoryBeatData")]
public class StoryBeatData : ScriptableObject
{
    public StoryBeat[] beats;
}
