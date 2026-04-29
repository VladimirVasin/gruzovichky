using UnityEngine;

public partial class GameBootstrap
{
    private bool hasPlacedSquirrelMemorialSign;

    private void ResetSquirrelMemorialWorldState()
    {
        hasPlacedSquirrelMemorialSign = false;
    }

    private void TryCreateSquirrelMemorialSign(Transform parent, Vector3 groundPosition, Quaternion rotation)
    {
        if (hasPlacedSquirrelMemorialSign || parent == null)
        {
            return;
        }

        hasPlacedSquirrelMemorialSign = true;

        GameObject root = new("SquirrelMemorialSign");
        root.transform.SetParent(parent, false);
        root.transform.position = groundPosition;
        root.transform.rotation = rotation;

        Color postColor  = new(0.30f, 0.20f, 0.12f);
        Color boardColor = new(0.72f, 0.58f, 0.34f);

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.transform.SetParent(root.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.36f, 0f);
        post.transform.localScale = new Vector3(0.08f, 0.72f, 0.08f);
        ApplyColor(post, postColor);
        ConfigureStaticVisual(post);

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.transform.SetParent(root.transform, false);
        board.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        board.transform.localScale = new Vector3(1.18f, 0.48f, 0.06f);
        ApplyColor(board, boardColor);
        ConfigureStaticVisual(board);

        GameObject textObject = new("SquirrelMemorialText");
        textObject.transform.SetParent(root.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 0.84f, -0.036f);
        textObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = "\u0412 \u043f\u0430\u043c\u044f\u0442\u044c \u043e \u0431\u0435\u043b\u043a\u0435,\n\u043e\u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0432\u0448\u0435\u0439 \u0441\u043c\u0435\u043d\u0443.";
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.fontSize = 24;
        textMesh.characterSize = 0.045f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = new Color(0.12f, 0.08f, 0.04f);
        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        textRenderer.receiveShadows = false;

        GameObject nut = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nut.transform.SetParent(root.transform, false);
        nut.transform.localPosition = new Vector3(0.48f, 1.12f, -0.02f);
        nut.transform.localScale = new Vector3(0.16f, 0.11f, 0.16f);
        ApplyColor(nut, new Color(0.54f, 0.34f, 0.16f));
        ConfigureStaticVisual(nut);

        SessionDebugLogger.Log("WORLD", "Placed squirrel memorial sign.");
    }
}
