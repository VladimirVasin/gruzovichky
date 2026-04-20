using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly Color[] WorkerPortraitSkinTones =
    {
        new(0.96f, 0.78f, 0.62f, 1f),
        new(0.84f, 0.60f, 0.43f, 1f),
        new(0.62f, 0.39f, 0.26f, 1f),
        new(0.98f, 0.86f, 0.68f, 1f),
        new(0.74f, 0.50f, 0.34f, 1f)
    };

    private static readonly Color[] WorkerPortraitHairColors =
    {
        new(0.13f, 0.09f, 0.06f, 1f),
        new(0.31f, 0.18f, 0.09f, 1f),
        new(0.72f, 0.52f, 0.22f, 1f),
        new(0.42f, 0.42f, 0.40f, 1f),
        new(0.62f, 0.20f, 0.12f, 1f)
    };

    private static readonly Color[] WorkerPortraitShirtColors =
    {
        new(0.18f, 0.36f, 0.70f, 1f),
        new(0.28f, 0.48f, 0.25f, 1f),
        new(0.58f, 0.32f, 0.13f, 1f),
        new(0.48f, 0.20f, 0.22f, 1f),
        new(0.20f, 0.28f, 0.38f, 1f)
    };

    private void AssignWorkerPortrait(DriverAgent driver)
    {
        if (driver == null) return;

        int seed = StableWorkerPortraitHash(driver.DriverName) ^ (driver.DriverId * 73856093);
        System.Random rng = new(seed);
        driver.PortraitSkinTone = rng.Next(WorkerPortraitSkinTones.Length);
        driver.PortraitHairStyle = rng.Next(4);
        driver.PortraitHairColor = rng.Next(WorkerPortraitHairColors.Length);
        driver.PortraitEyeStyle = rng.Next(4);
        driver.PortraitMouthStyle = rng.Next(4);
        driver.PortraitAccessory = rng.Next(5);
        driver.PortraitHeadShape = rng.Next(3);
        driver.HasPortrait = true;
    }

    private void EnsureWorkerPortrait(DriverAgent driver)
    {
        if (driver == null || driver.HasPortrait) return;
        AssignWorkerPortrait(driver);
    }

    private void UpdateWorkerPortraitUi(DriverAgent driver)
    {
        RectTransform root = driversScreenUi?.DetailPortraitRoot;
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        if (driver == null)
        {
            root.gameObject.SetActive(false);
            return;
        }

        root.gameObject.SetActive(true);
        EnsureWorkerPortrait(driver);

        Color bgA = new(0.09f, 0.12f, 0.17f, 1f);
        Color bgB = new(0.13f, 0.17f, 0.23f, 1f);
        CreatePortraitPart("PortraitBackplate", root, Vector2.zero, new Vector2(108f, 94f), bgA);
        CreatePortraitPart("PortraitBackdrop", root, new Vector2(0f, 5f), new Vector2(88f, 76f), bgB);

        Color skin = WorkerPortraitSkinTones[Mathf.Clamp(driver.PortraitSkinTone, 0, WorkerPortraitSkinTones.Length - 1)];
        Color hair = WorkerPortraitHairColors[Mathf.Clamp(driver.PortraitHairColor, 0, WorkerPortraitHairColors.Length - 1)];
        Color shirt = WorkerPortraitShirtColors[Mathf.Abs(driver.DriverId) % WorkerPortraitShirtColors.Length];
        Color shadowSkin = Color.Lerp(skin, Color.black, 0.18f);
        Color ink = new(0.07f, 0.08f, 0.10f, 1f);

        float headWidth = driver.PortraitHeadShape switch
        {
            1 => 46f,
            2 => 54f,
            _ => 50f
        };
        float headHeight = driver.PortraitHeadShape == 1 ? 46f : 50f;

        CreatePortraitPart("PortraitNeck", root, new Vector2(0f, -23f), new Vector2(15f, 18f), shadowSkin);
        CreatePortraitPart("PortraitShirt", root, new Vector2(0f, -40f), new Vector2(66f, 26f), shirt);
        CreatePortraitPart("PortraitCollar", root, new Vector2(0f, -30f), new Vector2(28f, 9f), Color.Lerp(shirt, Color.white, 0.16f));
        CreatePortraitPart("PortraitLeftEar", root, new Vector2(-headWidth * 0.5f - 4f, 5f), new Vector2(8f, 16f), shadowSkin);
        CreatePortraitPart("PortraitRightEar", root, new Vector2(headWidth * 0.5f + 4f, 5f), new Vector2(8f, 16f), shadowSkin);
        CreatePortraitPart("PortraitHead", root, new Vector2(0f, 6f), new Vector2(headWidth, headHeight), skin);
        CreatePortraitPart("PortraitCheek", root, new Vector2(headWidth * 0.18f, -4f), new Vector2(10f, 10f), Color.Lerp(skin, Color.white, 0.12f));

        DrawWorkerPortraitHair(root, driver.PortraitHairStyle, hair, headWidth, headHeight);
        DrawWorkerPortraitEyes(root, driver.PortraitEyeStyle, ink);
        CreatePortraitPart("PortraitNose", root, new Vector2(0f, -1f), new Vector2(5f, 12f), Color.Lerp(skin, Color.black, 0.12f));
        DrawWorkerPortraitMouth(root, driver.PortraitMouthStyle, ink);
        DrawWorkerPortraitAccessory(root, driver.PortraitAccessory, hair, ink);
    }

    private static int StableWorkerPortraitHash(string value)
    {
        unchecked
        {
            int hash = 23;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }
            }

            return hash;
        }
    }

    private static Image CreatePortraitPart(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject part = CreateUiObject(name, parent);
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = part.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void DrawWorkerPortraitHair(RectTransform root, int style, Color hair, float headWidth, float headHeight)
    {
        CreatePortraitPart("PortraitHairTop", root, new Vector2(0f, 5f + headHeight * 0.5f - 6f), new Vector2(headWidth + 8f, 14f), hair);

        switch (style)
        {
            case 1:
                CreatePortraitPart("PortraitHairLeft", root, new Vector2(-headWidth * 0.38f, 15f), new Vector2(12f, 30f), hair);
                CreatePortraitPart("PortraitHairRight", root, new Vector2(headWidth * 0.34f, 18f), new Vector2(10f, 22f), hair);
                break;
            case 2:
                CreatePortraitPart("PortraitHairPeak", root, new Vector2(10f, 34f), new Vector2(20f, 12f), Color.Lerp(hair, Color.white, 0.08f));
                CreatePortraitPart("PortraitSideburn", root, new Vector2(-headWidth * 0.46f, 4f), new Vector2(7f, 22f), hair);
                break;
            case 3:
                CreatePortraitPart("PortraitFlatCap", root, new Vector2(0f, 36f), new Vector2(headWidth + 18f, 8f), Color.Lerp(hair, Color.black, 0.22f));
                CreatePortraitPart("PortraitFlatCapBrim", root, new Vector2(18f, 30f), new Vector2(24f, 6f), Color.Lerp(hair, Color.black, 0.08f));
                break;
            default:
                CreatePortraitPart("PortraitFringe", root, new Vector2(-10f, 27f), new Vector2(22f, 11f), Color.Lerp(hair, Color.white, 0.06f));
                CreatePortraitPart("PortraitHairRightBlock", root, new Vector2(22f, 12f), new Vector2(8f, 26f), hair);
                break;
        }
    }

    private static void DrawWorkerPortraitEyes(RectTransform root, int style, Color ink)
    {
        float eyeWidth = style == 2 ? 10f : 7f;
        float eyeHeight = style == 1 ? 3f : 5f;
        float y = style == 3 ? 9f : 11f;
        CreatePortraitPart("PortraitLeftEye", root, new Vector2(-13f, y), new Vector2(eyeWidth, eyeHeight), ink);
        CreatePortraitPart("PortraitRightEye", root, new Vector2(13f, y), new Vector2(eyeWidth, eyeHeight), ink);

        if (style == 2)
        {
            CreatePortraitPart("PortraitLeftBrow", root, new Vector2(-13f, 18f), new Vector2(13f, 3f), ink);
            CreatePortraitPart("PortraitRightBrow", root, new Vector2(13f, 18f), new Vector2(13f, 3f), ink);
        }
    }

    private static void DrawWorkerPortraitMouth(RectTransform root, int style, Color ink)
    {
        Color mouth = style == 2 ? new Color(0.42f, 0.12f, 0.10f, 1f) : ink;
        Vector2 size = style switch
        {
            1 => new Vector2(14f, 3f),
            2 => new Vector2(10f, 5f),
            3 => new Vector2(18f, 4f),
            _ => new Vector2(12f, 4f)
        };
        CreatePortraitPart("PortraitMouth", root, new Vector2(0f, -16f), size, mouth);
        if (style == 3)
        {
            CreatePortraitPart("PortraitMouthCorner", root, new Vector2(10f, -14f), new Vector2(4f, 3f), mouth);
        }
    }

    private static void DrawWorkerPortraitAccessory(RectTransform root, int accessory, Color hair, Color ink)
    {
        switch (accessory)
        {
            case 1:
                CreatePortraitPart("PortraitGlassesLeft", root, new Vector2(-13f, 10f), new Vector2(15f, 8f), new Color(0.02f, 0.03f, 0.04f, 0.72f));
                CreatePortraitPart("PortraitGlassesRight", root, new Vector2(13f, 10f), new Vector2(15f, 8f), new Color(0.02f, 0.03f, 0.04f, 0.72f));
                CreatePortraitPart("PortraitGlassesBridge", root, new Vector2(0f, 10f), new Vector2(8f, 3f), ink);
                break;
            case 2:
                CreatePortraitPart("PortraitMoustacheLeft", root, new Vector2(-7f, -9f), new Vector2(13f, 4f), Color.Lerp(hair, Color.black, 0.1f));
                CreatePortraitPart("PortraitMoustacheRight", root, new Vector2(7f, -9f), new Vector2(13f, 4f), Color.Lerp(hair, Color.black, 0.1f));
                break;
            case 3:
                CreatePortraitPart("PortraitBadge", root, new Vector2(23f, -39f), new Vector2(9f, 9f), new Color(0.95f, 0.76f, 0.22f, 1f));
                break;
            case 4:
                CreatePortraitPart("PortraitScar", root, new Vector2(18f, 0f), new Vector2(3f, 18f), new Color(0.50f, 0.20f, 0.18f, 0.72f));
                break;
        }
    }
}
