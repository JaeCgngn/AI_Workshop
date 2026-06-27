using UnityEngine;

public static class NpcWorkshopBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWorkshopScene()
    {
        NpcWorkshopBuilder.BuildIfMissing();
    }
}
