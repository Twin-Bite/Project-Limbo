using UnityEngine;

[CreateAssetMenu(fileName = "Scene Layer", menuName = "Scene Layer")]
public class SceneLayer : ScriptableObject
{
    public string Name;
    public Color SceneFolderColor = new Color(.15f, .15f, .15f, 1f);
}
