using UnityEngine;

public sealed class LocalPlayerSaveStore : IPlayerSaveStore
{
    public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(Game.Save ?? SaveSystem.LoadOrNew()));
    public void Write(PlayerSave save)
    {
        SaveSystem.Save(save);
        Game.Save = save;
    }
}
