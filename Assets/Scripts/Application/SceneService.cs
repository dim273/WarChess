namespace WarChess.Application
{
    // 场景和应用退出的基础设施边界
    public interface ISceneService
    {
        void LoadScene(string sceneName);
        void Quit();
    }
}