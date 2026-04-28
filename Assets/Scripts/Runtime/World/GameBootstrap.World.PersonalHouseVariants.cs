using UnityEngine;

public partial class GameBootstrap
{
    private void HouseCraftsman(Transform p)
    {
        Color tan    = new Color(0.78f, 0.66f, 0.48f);
        Color dbrown = new Color(0.26f, 0.18f, 0.10f);
        Color stone  = new Color(0.54f, 0.52f, 0.48f);
        Color winGrn = new Color(0.52f, 0.72f, 0.56f);
        Color conc   = new Color(0.68f, 0.67f, 0.64f);
        Color fence  = new Color(0.94f, 0.92f, 0.88f);

        HQ(p, new Vector3(0f,    0.47f, -0.92f), new Vector3(2.20f, 0.50f, 1.45f), tan);
        HQ(p, new Vector3(0f,    0.75f, -0.60f), new Vector3(2.35f, 0.08f, 2.15f), dbrown);
        HQ(p, new Vector3(-1.12f, 0.82f, -0.60f), new Vector3(0.07f, 0.22f, 2.15f), dbrown);
        HQ(p, new Vector3( 1.12f, 0.82f, -0.60f), new Vector3(0.07f, 0.22f, 2.15f), dbrown);
        HQ(p, new Vector3(0f,    0.25f,  0.25f), new Vector3(2.30f, 0.07f, 0.80f), new Color(0.60f, 0.48f, 0.32f));
        HQ(p, new Vector3(-1.10f, 0.35f,  0.22f), new Vector3(0.07f, 0.20f, 0.78f), tan);
        HQ(p, new Vector3( 1.10f, 0.35f,  0.22f), new Vector3(0.07f, 0.20f, 0.78f), tan);
        foreach (float cx in new float[] { -0.78f, -0.26f, 0.26f, 0.78f })
        {
            HQ(p, new Vector3(cx, 0.49f, 0.60f), new Vector3(0.09f, 0.48f, 0.09f), dbrown);
        }

        foreach (float cx in new float[] { -0.90f, -0.44f, 0f, 0.44f, 0.90f })
        {
            HQ(p, new Vector3(cx, 0.74f, 0.68f), new Vector3(0.06f, 0.06f, 0.12f), dbrown);
        }

        HQ(p, new Vector3(-0.45f, 0.80f, -0.40f), new Vector3(0.22f, 0.40f, 0.22f), stone);
        HQ(p, new Vector3(-0.45f, 1.03f, -0.40f), new Vector3(0.27f, 0.06f, 0.27f), stone);
        HQ(p, new Vector3(-0.55f, 0.47f, -0.15f), new Vector3(0.18f, 0.44f, 0.05f), new Color(0.44f, 0.28f, 0.12f));
        HQ(p, new Vector3(-0.55f, 0.16f,  0.08f), new Vector3(0.34f, 0.14f, 0.24f), conc);
        HQ(p, new Vector3( 0.40f, 0.50f, -0.15f), new Vector3(0.46f, 0.28f, 0.04f), winGrn);
        HQ(p, new Vector3( 1.00f, 0.50f, -0.15f), new Vector3(0.26f, 0.28f, 0.04f), winGrn);
        HQ(p, new Vector3(-0.92f, 0.41f, -0.12f), new Vector3(0.82f, 0.38f, 0.85f), tan);
        HQ(p, new Vector3(-0.92f, 0.63f, -0.12f), new Vector3(0.88f, 0.07f, 0.90f), dbrown);
        HQ(p, new Vector3(-0.92f, 0.42f,  0.32f), new Vector3(0.72f, 0.35f, 0.04f), new Color(0.26f, 0.26f, 0.24f));
        HQ(p, new Vector3(-0.92f, 0.22f,  1.00f), new Vector3(0.76f, 0.012f, 1.70f), conc);
        HouseFence(p, -1.55f, -1.33f, 1.72f, fence);
        HouseFence(p, -0.52f,  1.55f, 1.72f, fence);
        HouseTree(p,  1.40f, 0.90f);
        HouseShrub(p,  0.90f, 0.50f);
        HouseShrub(p,  1.30f, 0.50f);
    }

    private void HouseSplitLevel(Transform p)
    {
        Color beige  = new Color(0.88f, 0.82f, 0.70f);
        Color lbeige = new Color(0.92f, 0.88f, 0.78f);
        Color dark   = new Color(0.20f, 0.18f, 0.14f);
        Color slate  = new Color(0.54f, 0.56f, 0.58f);
        Color winBlue= new Color(0.56f, 0.76f, 0.86f);
        Color conc   = new Color(0.68f, 0.67f, 0.64f);
        Color fence  = new Color(0.94f, 0.92f, 0.88f);

        HQ(p, new Vector3( 0.42f, 0.63f, -0.88f), new Vector3(2.00f, 0.82f, 1.72f), beige);
        HQ(p, new Vector3( 0.42f, 1.08f, -0.88f), new Vector3(2.10f, 0.07f, 1.82f), dark);
        HQ(p, new Vector3(-0.95f, 0.41f, -0.12f), new Vector3(0.88f, 0.46f, 0.90f), lbeige);
        HQ(p, new Vector3(-0.95f, 0.67f, -0.12f), new Vector3(0.92f, 0.07f, 0.94f), dark);
        HQ(p, new Vector3(-0.26f, 0.32f, -0.22f), new Vector3(0.38f, 0.24f, 0.08f), beige);
        HQ(p, new Vector3(-0.95f, 0.42f,  0.33f), new Vector3(0.78f, 0.38f, 0.04f), new Color(0.26f, 0.26f, 0.24f));
        HQ(p, new Vector3(-0.49f, 0.60f, -0.88f), new Vector3(0.08f, 0.82f, 1.72f), slate);
        HQ(p, new Vector3( 0.60f, 0.58f, -0.01f), new Vector3(0.18f, 0.46f, 0.05f), new Color(0.68f, 0.14f, 0.10f));
        HQ(p, new Vector3( 0.60f, 0.18f,  0.18f), new Vector3(0.34f, 0.14f, 0.26f), conc);
        HQ(p, new Vector3(-0.05f, 0.72f, -0.01f), new Vector3(0.36f, 0.30f, 0.04f), winBlue);
        HQ(p, new Vector3( 1.10f, 0.72f, -0.01f), new Vector3(0.36f, 0.30f, 0.04f), winBlue);
        HQ(p, new Vector3( 0.62f, 0.72f, -0.01f), new Vector3(0.22f, 0.30f, 0.04f), winBlue);
        HQ(p, new Vector3(-0.95f, 0.22f,  1.00f), new Vector3(0.82f, 0.012f, 1.70f), conc);
        HouseFence(p, -1.55f, -1.38f, 1.72f, fence);
        HouseFence(p, -0.55f,  1.55f, 1.72f, fence);
        HouseTree(p,  1.45f, 1.10f);
        HouseTree(p, -1.42f, 0.90f);
        HouseShrub(p, -0.15f, 0.50f);
    }
}
