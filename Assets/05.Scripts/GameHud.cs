using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    private static string message = "Reach the green goal ring.";
    private static float messageUntil;

    public static void ShowMessage(string value, float seconds = 3f)
    {
        message = value;
        messageUntil = Time.time + seconds;
    }

    private void OnGUI()
    {
        GUI.depth = 0;

        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            padding = new RectOffset(14, 14, 12, 12)
        };

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true
        };

        GUILayout.BeginArea(new Rect(18, 18, 430, 178), boxStyle);
        GUILayout.Label("Driftling Workshop", new GUIStyle(labelStyle) { fontSize = 19, fontStyle = FontStyle.Bold });
        GUILayout.Space(5);
        GUILayout.Label("WASD: move    Mouse: look    Space: jump", labelStyle);
        GUILayout.Label("LMB/RMB: left/right hand grab    R: respawn", labelStyle);
        GUILayout.Label("Grab a ledge, look down, then move forward to climb.", labelStyle);
        GUILayout.Space(7);
        GUILayout.Label(Time.time < messageUntil ? message : "Put the blue crate on the plate, open the gate, then wobble onward.", labelStyle);
        GUILayout.EndArea();
    }
}
