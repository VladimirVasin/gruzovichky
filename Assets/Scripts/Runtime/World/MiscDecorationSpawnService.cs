public enum MiscDecorationKind
{
    Tree,
    BerryBush,
    FlowerPatch
}

public static class MiscDecorationSpawnService
{
    public static MiscDecorationKind ChooseKind(float roll, float flowerChance, float berryBushChance)
    {
        if (roll < flowerChance)
        {
            return MiscDecorationKind.FlowerPatch;
        }

        if (roll < flowerChance + berryBushChance)
        {
            return MiscDecorationKind.BerryBush;
        }

        return MiscDecorationKind.Tree;
    }
}
