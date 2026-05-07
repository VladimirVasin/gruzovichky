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

    private static readonly int[] FemalePortraitHairStyles = { 0, 1, 2, 4, 5 };
    private static readonly int[] FemalePortraitAccessories = { 0, 1, 3, 4, 5 };

    private void AssignWorkerPortrait(DriverAgent driver)
    {
        if (driver == null) return;

        int seed = StableWorkerPortraitHash(driver.DriverName) ^ (driver.DriverId * 73856093);
        System.Random rng = new(seed);
        driver.PortraitSkinTone  = rng.Next(WorkerPortraitSkinTones.Length);
        driver.PortraitHairColor = rng.Next(WorkerPortraitHairColors.Length);
        driver.PortraitEyeStyle  = rng.Next(4);
        driver.PortraitMouthStyle = rng.Next(4);
        driver.PortraitHeadShape = rng.Next(3);

        if (driver.Gender == WorkerGender.Female)
        {
            driver.PortraitHairStyle = FemalePortraitHairStyles[rng.Next(FemalePortraitHairStyles.Length)];
            driver.PortraitAccessory = FemalePortraitAccessories[rng.Next(FemalePortraitAccessories.Length)];
        }
        else
        {
            driver.PortraitHairStyle = rng.Next(4);
            driver.PortraitAccessory = rng.Next(6);
        }

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
        DrawWorkerPortraitScaled(driver, root, 1.34f);
    }

    private void DrawWorkerPortraitScaled(DriverAgent driver, RectTransform root, float scale)
    {
        if (root == null || driver == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        EnsureWorkerPortrait(driver);

        Color bgA = new(0.09f, 0.12f, 0.17f, 1f);
        Color bgB = new(0.13f, 0.17f, 0.23f, 1f);
        CreatePortraitPart("PBP", root, Vector2.zero, new Vector2(108f * scale, 94f * scale), bgA);
        CreatePortraitPart("PBD", root, new Vector2(0f, 5f * scale), new Vector2(88f * scale, 76f * scale), bgB);

        Color skin      = WorkerPortraitSkinTones[Mathf.Clamp(driver.PortraitSkinTone, 0, WorkerPortraitSkinTones.Length - 1)];
        Color hair      = WorkerPortraitHairColors[Mathf.Clamp(driver.PortraitHairColor, 0, WorkerPortraitHairColors.Length - 1)];
        Color shirt     = WorkerPortraitShirtColors[Mathf.Abs(driver.DriverId) % WorkerPortraitShirtColors.Length];
        Color shadowSkin = Color.Lerp(skin, Color.black, 0.18f);
        Color ink       = new(0.07f, 0.08f, 0.10f, 1f);

        float headWidth  = driver.PortraitHeadShape switch { 1 => 46f, 2 => 54f, _ => 50f } * scale;
        float headHeight = (driver.PortraitHeadShape == 1 ? 46f : 50f) * scale;
        if (driver.Gender == WorkerGender.Female)
        {
            headWidth *= 0.94f;
            headHeight *= 0.96f;
        }

        CreatePortraitPart("PN",  root, new Vector2(0f, -23f * scale), new Vector2((driver.Gender == WorkerGender.Female ? 13f : 15f) * scale, 18f * scale), shadowSkin);
        CreatePortraitPart("PSh", root, new Vector2(0f, -40f * scale), new Vector2(66f * scale, 26f * scale), shirt);
        CreatePortraitPart("PC",  root, new Vector2(0f, -30f * scale), new Vector2((driver.Gender == WorkerGender.Female ? 24f : 28f) * scale, 9f  * scale), Color.Lerp(shirt, Color.white, 0.16f));
        CreatePortraitPart("PLE", root, new Vector2(-headWidth * 0.5f - 4f * scale, 5f * scale), new Vector2((driver.Gender == WorkerGender.Female ? 7f : 8f) * scale, (driver.Gender == WorkerGender.Female ? 14f : 16f) * scale), shadowSkin);
        CreatePortraitPart("PRE", root, new Vector2( headWidth * 0.5f + 4f * scale, 5f * scale), new Vector2((driver.Gender == WorkerGender.Female ? 7f : 8f) * scale, (driver.Gender == WorkerGender.Female ? 14f : 16f) * scale), shadowSkin);
        CreatePortraitPart("PH",  root, new Vector2(0f, 6f * scale), new Vector2(headWidth, headHeight), skin);
        Color cheekTint = driver.Gender == WorkerGender.Female
            ? Color.Lerp(skin, new Color(1f, 0.75f, 0.75f, 1f), 0.22f)
            : Color.Lerp(skin, Color.white, 0.12f);
        if (driver.Gender == WorkerGender.Female)
        {
            CreatePortraitPart("PChL", root, new Vector2(-headWidth * 0.18f, -4f * scale), new Vector2(9f * scale, 9f * scale), cheekTint);
            CreatePortraitPart("PChR", root, new Vector2(headWidth * 0.18f, -4f * scale), new Vector2(9f * scale, 9f * scale), cheekTint);
        }
        else
        {
            CreatePortraitPart("PCh", root, new Vector2(headWidth * 0.18f, -4f * scale), new Vector2(10f * scale, 10f * scale), cheekTint);
        }

        DrawWorkerPortraitHairScaled(root, driver.PortraitHairStyle, hair, headWidth, headHeight, scale, driver.Gender);
        DrawWorkerPortraitEyesScaled(root, driver.PortraitEyeStyle, ink, scale, driver.Gender);
        CreatePortraitPart("PNo", root, new Vector2(0f, -1f * scale), new Vector2(5f * scale, 12f * scale), Color.Lerp(skin, Color.black, 0.12f));
        DrawWorkerPortraitMouthScaled(root, driver.PortraitMouthStyle, ink, scale, driver.Gender);
        DrawWorkerPortraitAccessoryScaled(root, driver.PortraitAccessory, hair, ink, scale, driver.Gender);
    }

    private static void DrawWorkerPortraitHairScaled(RectTransform root, int style, Color hair, float headWidth, float headHeight, float s, WorkerGender gender)
    {
        CreatePortraitPart("PHT", root, new Vector2(0f, 5f * s + headHeight * 0.5f - 6f * s), new Vector2(headWidth + 8f * s, 14f * s), hair);
        switch (style)
        {
            case 1:
                CreatePortraitPart("PHL",  root, new Vector2(-headWidth * 0.38f, 15f * s), new Vector2(12f * s, 30f * s), hair);
                CreatePortraitPart("PHRR", root, new Vector2( headWidth * 0.34f, 18f * s), new Vector2(10f * s, 22f * s), hair);
                break;
            case 2:
                CreatePortraitPart("PHPK", root, new Vector2(10f * s, 34f * s), new Vector2(20f * s, 12f * s), Color.Lerp(hair, Color.white, 0.08f));
                CreatePortraitPart("PHSB", root, new Vector2(-headWidth * 0.46f, 4f * s), new Vector2(7f * s, 22f * s), hair);
                break;
            case 3:
                CreatePortraitPart("PHFC",  root, new Vector2(0f, 36f * s), new Vector2(headWidth + 18f * s, 8f * s), Color.Lerp(hair, Color.black, 0.22f));
                CreatePortraitPart("PHFCB", root, new Vector2(18f * s, 30f * s), new Vector2(24f * s, 6f * s), Color.Lerp(hair, Color.black, 0.08f));
                break;
            case 4:
                CreatePortraitPart("PHBobTop", root, new Vector2(0f, 28f * s), new Vector2(headWidth + 10f * s, 10f * s), Color.Lerp(hair, Color.white, 0.05f));
                CreatePortraitPart("PHBobLeft", root, new Vector2(-headWidth * 0.42f, 8f * s), new Vector2(12f * s, 34f * s), hair);
                CreatePortraitPart("PHBobRight", root, new Vector2(headWidth * 0.42f, 8f * s), new Vector2(12f * s, 34f * s), hair);
                CreatePortraitPart("PHBobFringe", root, new Vector2(0f, 20f * s), new Vector2(26f * s, 8f * s), Color.Lerp(hair, Color.white, 0.08f));
                break;
            case 5:
                CreatePortraitPart("PHPonyLeft", root, new Vector2(-headWidth * 0.40f, 10f * s), new Vector2(10f * s, 30f * s), hair);
                CreatePortraitPart("PHPonyRight", root, new Vector2(headWidth * 0.40f, 10f * s), new Vector2(10f * s, 30f * s), hair);
                CreatePortraitPart("PHPonyBand", root, new Vector2(0f, 18f * s), new Vector2(18f * s, 6f * s), Color.Lerp(hair, Color.white, 0.15f));
                CreatePortraitPart("PHPonyTail", root, new Vector2(0f, -6f * s), new Vector2(12f * s, 28f * s), Color.Lerp(hair, Color.black, 0.08f));
                break;
            default:
                CreatePortraitPart("PHF",  root, new Vector2(-10f * s, 27f * s), new Vector2(22f * s, 11f * s), Color.Lerp(hair, Color.white, 0.06f));
                if (gender == WorkerGender.Female)
                {
                    CreatePortraitPart("PHFL", root, new Vector2(-headWidth * 0.42f, 8f * s), new Vector2(10f * s, 24f * s), hair);
                    CreatePortraitPart("PHFR", root, new Vector2(headWidth * 0.42f, 8f * s), new Vector2(10f * s, 24f * s), hair);
                }
                else
                {
                    CreatePortraitPart("PHRB", root, new Vector2(22f * s, 12f * s),  new Vector2(8f  * s, 26f * s), hair);
                }
                break;
        }
    }

    private static void DrawWorkerPortraitEyesScaled(RectTransform root, int style, Color ink, float s, WorkerGender gender)
    {
        float ew = (style == 2 ? 10f : 7f) * s;
        float eh = (style == 1 ? 3f  : 5f) * s;
        float y  = (style == 3 ? 9f  : 11f) * s;
        if (gender == WorkerGender.Female)
        {
            ew *= 1.15f;
            eh *= 1.10f;
            y += 1.0f * s;
        }
        CreatePortraitPart("PEL", root, new Vector2(-13f * s, y), new Vector2(ew, eh), ink);
        CreatePortraitPart("PER", root, new Vector2( 13f * s, y), new Vector2(ew, eh), ink);
        if (style == 2)
        {
            CreatePortraitPart("PBL", root, new Vector2(-13f * s, 18f * s), new Vector2(13f * s, 3f * s), ink);
            CreatePortraitPart("PBR", root, new Vector2( 13f * s, 18f * s), new Vector2(13f * s, 3f * s), ink);
        }
        if (gender == WorkerGender.Female)
        {
            Color lash = Color.Lerp(ink, Color.white, 0.08f);
            CreatePortraitPart("PELL", root, new Vector2(-13f * s, (y + 6f * s)), new Vector2((ew + 3f * s), 2f * s), lash);
            CreatePortraitPart("PERL", root, new Vector2(13f * s, (y + 6f * s)), new Vector2((ew + 3f * s), 2f * s), lash);
        }
    }

    private static void DrawWorkerPortraitMouthScaled(RectTransform root, int style, Color ink, float s, WorkerGender gender)
    {
        Color mouth = style == 2 ? new Color(0.42f, 0.12f, 0.10f, 1f) : ink;
        if (gender == WorkerGender.Female)
        {
            mouth = Color.Lerp(mouth, new Color(0.72f, 0.28f, 0.34f, 1f), 0.55f);
        }
        Vector2 size = style switch { 1 => new Vector2(14f, 3f), 2 => new Vector2(10f, 5f), 3 => new Vector2(18f, 4f), _ => new Vector2(12f, 4f) } * s;
        float mouthY = gender == WorkerGender.Female ? -15f * s : -16f * s;
        CreatePortraitPart("PMo", root, new Vector2(0f, mouthY), size, mouth);
        if (style == 3)
            CreatePortraitPart("PMC", root, new Vector2(10f * s, gender == WorkerGender.Female ? -13f * s : -14f * s), new Vector2(4f * s, 3f * s), mouth);
    }

    private static void DrawWorkerPortraitAccessoryScaled(RectTransform root, int accessory, Color hair, Color ink, float s, WorkerGender gender)
    {
        switch (accessory)
        {
            case 1:
                CreatePortraitPart("PAG1", root, new Vector2(-13f * s, 10f * s), new Vector2(15f * s, 8f * s), new Color(0.02f, 0.03f, 0.04f, 0.72f));
                CreatePortraitPart("PAG2", root, new Vector2( 13f * s, 10f * s), new Vector2(15f * s, 8f * s), new Color(0.02f, 0.03f, 0.04f, 0.72f));
                CreatePortraitPart("PAG3", root, new Vector2(0f, 10f * s), new Vector2(8f * s, 3f * s), ink);
                break;
            case 2:
                CreatePortraitPart("PAM1", root, new Vector2(-7f * s, -9f * s), new Vector2(13f * s, 4f * s), Color.Lerp(hair, Color.black, 0.1f));
                CreatePortraitPart("PAM2", root, new Vector2( 7f * s, -9f * s), new Vector2(13f * s, 4f * s), Color.Lerp(hair, Color.black, 0.1f));
                break;
            case 3:
                CreatePortraitPart("PAB", root, new Vector2(23f * s, -39f * s), new Vector2(9f * s, 9f * s), new Color(0.95f, 0.76f, 0.22f, 1f));
                break;
            case 4:
                CreatePortraitPart("PAS", root, new Vector2(18f * s, 0f), new Vector2(3f * s, 18f * s), new Color(0.50f, 0.20f, 0.18f, 0.72f));
                break;
            case 5:
                CreatePortraitPart("PAEL", root, new Vector2(-22f * s, -2f * s), new Vector2(4f * s, 7f * s), new Color(0.95f, 0.76f, 0.22f, 1f));
                CreatePortraitPart("PAER", root, new Vector2(22f * s, -2f * s), new Vector2(4f * s, 7f * s), new Color(0.95f, 0.76f, 0.22f, 1f));
                if (gender == WorkerGender.Female)
                {
                    CreatePortraitPart("PAClip", root, new Vector2(18f * s, 18f * s), new Vector2(8f * s, 4f * s), Color.Lerp(hair, Color.white, 0.35f));
                }
                break;
        }
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

    private static void DrawWorkerPortraitHair(RectTransform root, int style, Color hair, float headWidth, float headHeight, WorkerGender gender)
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
            case 4:
                CreatePortraitPart("PortraitBobTop", root, new Vector2(0f, 28f), new Vector2(headWidth + 10f, 10f), Color.Lerp(hair, Color.white, 0.05f));
                CreatePortraitPart("PortraitBobLeft", root, new Vector2(-headWidth * 0.42f, 8f), new Vector2(12f, 34f), hair);
                CreatePortraitPart("PortraitBobRight", root, new Vector2(headWidth * 0.42f, 8f), new Vector2(12f, 34f), hair);
                CreatePortraitPart("PortraitBobFringe", root, new Vector2(0f, 20f), new Vector2(26f, 8f), Color.Lerp(hair, Color.white, 0.08f));
                break;
            case 5:
                CreatePortraitPart("PortraitPonyLeft", root, new Vector2(-headWidth * 0.40f, 10f), new Vector2(10f, 30f), hair);
                CreatePortraitPart("PortraitPonyRight", root, new Vector2(headWidth * 0.40f, 10f), new Vector2(10f, 30f), hair);
                CreatePortraitPart("PortraitPonyBand", root, new Vector2(0f, 18f), new Vector2(18f, 6f), Color.Lerp(hair, Color.white, 0.15f));
                CreatePortraitPart("PortraitPonyTail", root, new Vector2(0f, -6f), new Vector2(12f, 28f), Color.Lerp(hair, Color.black, 0.08f));
                break;
            default:
                CreatePortraitPart("PortraitFringe", root, new Vector2(-10f, 27f), new Vector2(22f, 11f), Color.Lerp(hair, Color.white, 0.06f));
                if (gender == WorkerGender.Female)
                {
                    CreatePortraitPart("PortraitHairLeftBlock", root, new Vector2(-headWidth * 0.42f, 8f), new Vector2(10f, 24f), hair);
                    CreatePortraitPart("PortraitHairRightBlock", root, new Vector2(headWidth * 0.42f, 8f), new Vector2(10f, 24f), hair);
                }
                else
                {
                    CreatePortraitPart("PortraitHairRightBlock", root, new Vector2(22f, 12f), new Vector2(8f, 26f), hair);
                }
                break;
        }
    }

    private static void DrawWorkerPortraitEyes(RectTransform root, int style, Color ink, WorkerGender gender)
    {
        float eyeWidth = style == 2 ? 10f : 7f;
        float eyeHeight = style == 1 ? 3f : 5f;
        float y = style == 3 ? 9f : 11f;
        if (gender == WorkerGender.Female)
        {
            eyeWidth *= 1.15f;
            eyeHeight *= 1.10f;
            y += 1f;
        }
        CreatePortraitPart("PortraitLeftEye", root, new Vector2(-13f, y), new Vector2(eyeWidth, eyeHeight), ink);
        CreatePortraitPart("PortraitRightEye", root, new Vector2(13f, y), new Vector2(eyeWidth, eyeHeight), ink);

        if (style == 2)
        {
            CreatePortraitPart("PortraitLeftBrow", root, new Vector2(-13f, 18f), new Vector2(13f, 3f), ink);
            CreatePortraitPart("PortraitRightBrow", root, new Vector2(13f, 18f), new Vector2(13f, 3f), ink);
        }
        if (gender == WorkerGender.Female)
        {
            Color lash = Color.Lerp(ink, Color.white, 0.08f);
            CreatePortraitPart("PortraitLeftLash", root, new Vector2(-13f, y + 6f), new Vector2(eyeWidth + 3f, 2f), lash);
            CreatePortraitPart("PortraitRightLash", root, new Vector2(13f, y + 6f), new Vector2(eyeWidth + 3f, 2f), lash);
        }
    }

    private static void DrawWorkerPortraitMouth(RectTransform root, int style, Color ink, WorkerGender gender)
    {
        Color mouth = style == 2 ? new Color(0.42f, 0.12f, 0.10f, 1f) : ink;
        if (gender == WorkerGender.Female)
        {
            mouth = Color.Lerp(mouth, new Color(0.72f, 0.28f, 0.34f, 1f), 0.55f);
        }
        Vector2 size = style switch
        {
            1 => new Vector2(14f, 3f),
            2 => new Vector2(10f, 5f),
            3 => new Vector2(18f, 4f),
            _ => new Vector2(12f, 4f)
        };
        float mouthY = gender == WorkerGender.Female ? -15f : -16f;
        CreatePortraitPart("PortraitMouth", root, new Vector2(0f, mouthY), size, mouth);
        if (style == 3)
        {
            CreatePortraitPart("PortraitMouthCorner", root, new Vector2(10f, gender == WorkerGender.Female ? -13f : -14f), new Vector2(4f, 3f), mouth);
        }
    }

    private static void DrawWorkerPortraitAccessory(RectTransform root, int accessory, Color hair, Color ink, WorkerGender gender)
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
            case 5:
                CreatePortraitPart("PortraitEarringLeft", root, new Vector2(-22f, -2f), new Vector2(4f, 7f), new Color(0.95f, 0.76f, 0.22f, 1f));
                CreatePortraitPart("PortraitEarringRight", root, new Vector2(22f, -2f), new Vector2(4f, 7f), new Color(0.95f, 0.76f, 0.22f, 1f));
                if (gender == WorkerGender.Female)
                {
                    CreatePortraitPart("PortraitHairClip", root, new Vector2(18f, 18f), new Vector2(8f, 4f), Color.Lerp(hair, Color.white, 0.35f));
                }
                break;
        }
    }
}

