using UnityEngine.SceneManagement;

namespace BloodshedModToolkit.UI
{
    internal interface IModTab
    {
        void Draw(ModMenuContext ctx);
        void Tick(ModMenuContext ctx) { }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }
    }
}
