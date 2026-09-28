using System.Collections;
using UnityEngine;

// Shared building block for scripted cutscenes (the gate finale, the intro). Composes systems that
// already exist — ThirdPersonCamera shots, ProceduralCharacterAnim gestures, PlayerController /
// DavidCompanion actor control, StoryPanelController text — rather than replacing any of them.
public static class CutsceneSequencer
{
    // Smoothly move a character to a mark at walking speed, facing the direction of travel, then the
    // mark's own facing once arrived. `isPlayer` routes movement through PlayerController.Teleport so the
    // CharacterController stays in sync; other actors (David) are moved directly.
    public static IEnumerator MoveTo(Transform who, Transform mark, float speed, bool isPlayer)
    {
        if (mark == null) yield break;
        var pc = PlayerController.Instance;
        while (true)
        {
            Vector3 to = mark.position - who.position; to.y = 0f;
            if (to.magnitude < 0.08f) break;
            Vector3 next = who.position + to.normalized * Mathf.Min(to.magnitude, speed * Time.unscaledDeltaTime);
            Quaternion rot = Quaternion.LookRotation(to.normalized);
            if (isPlayer) pc.Teleport(next, Quaternion.Slerp(who.rotation, rot, 0.25f));
            else who.SetPositionAndRotation(next, Quaternion.Slerp(who.rotation, rot, 0.25f));
            yield return null;
        }
        if (isPlayer) pc.Teleport(who.position, mark.rotation);
        else who.rotation = mark.rotation;
    }

    // A quick fade on a shared CanvasGroup (e.g. ZoneManager.Instance.fadePanel), used to mask an instant
    // reposition between two cutscene locations that are too far apart to walk on screen.
    public static IEnumerator Fade(CanvasGroup panel, float target, float duration)
    {
        if (panel == null) yield break;
        float start = panel.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        panel.alpha = target;
    }
}
