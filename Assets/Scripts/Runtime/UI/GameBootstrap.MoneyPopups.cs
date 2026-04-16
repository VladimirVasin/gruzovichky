using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private sealed class MoneySpendPopup
    {
        public GameObject Root;
        public TextMesh   TextMesh;
        public float      Timer;
        public float      TotalTime;
    }

    private readonly List<MoneySpendPopup> moneyPopups = new();

    // Called by motel/bar entry code to show "-$X" above the building
    private void SpawnMoneySpendPopup(Vector3 worldPos, int amount)
    {
        GameObject root = new("MoneySpendPopup");
        root.transform.position = worldPos + Vector3.up * 0.7f;

        TextMesh tm = root.AddComponent<TextMesh>();
        tm.text      = $"-${amount}";
        tm.fontSize  = 28;
        tm.fontStyle = FontStyle.Bold;
        tm.color     = new Color(1f, 0.55f, 0.15f, 1f);   // orange
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.06f;

        // Face the camera (billboard)
        if (mainCamera != null)
            root.transform.rotation = mainCamera.transform.rotation;

        moneyPopups.Add(new MoneySpendPopup
        {
            Root      = root,
            TextMesh  = tm,
            Timer     = 0f,
            TotalTime = 1.6f
        });

        PlayUiSound(moneySpendClip, 0.85f);
    }

    private void UpdateMoneyPopups()
    {
        for (int i = moneyPopups.Count - 1; i >= 0; i--)
        {
            MoneySpendPopup popup = moneyPopups[i];
            if (popup.Root == null) { moneyPopups.RemoveAt(i); continue; }

            popup.Timer += Time.deltaTime;
            float t = Mathf.Clamp01(popup.Timer / popup.TotalTime);

            // Float upward
            Vector3 pos = popup.Root.transform.position;
            pos.y += 0.55f * Time.deltaTime;
            popup.Root.transform.position = pos;

            // Stay facing camera
            if (mainCamera != null)
                popup.Root.transform.rotation = mainCamera.transform.rotation;

            // Fade: full opacity first half, then fade out
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            Color c = popup.TextMesh.color;
            c.a = alpha;
            popup.TextMesh.color = c;

            if (t >= 1f)
            {
                Destroy(popup.Root);
                moneyPopups.RemoveAt(i);
            }
        }
    }
}
