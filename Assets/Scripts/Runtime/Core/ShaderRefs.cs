using UnityEngine;

[CreateAssetMenu(fileName = "ShaderRefs", menuName = "Game/Shader Refs")]
public class ShaderRefs : ScriptableObject
{
    public Shader UrpLit;
    public Shader UrpUnlit;
    public Shader SpritesDefault;

    private static ShaderRefs _instance;
    public static ShaderRefs Instance => _instance != null ? _instance : (_instance = Resources.Load<ShaderRefs>("ShaderRefs"));

    public static Shader Lit      => Instance != null ? Instance.UrpLit       : Shader.Find("Universal Render Pipeline/Lit");
    public static Shader Unlit    => Instance != null ? Instance.UrpUnlit     : Shader.Find("Universal Render Pipeline/Unlit");
    public static Shader Sprites  => Instance != null ? Instance.SpritesDefault : Shader.Find("Sprites/Default");
}
