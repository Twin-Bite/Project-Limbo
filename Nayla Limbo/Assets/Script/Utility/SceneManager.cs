using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   // You can set this string directly in the Unity Inspector
       [SerializeField] private string targetSceneName = "";
   
       public void LoadTargetScene()
       {
           // Loads the scene by its string name
           UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetSceneName);
       }
}
