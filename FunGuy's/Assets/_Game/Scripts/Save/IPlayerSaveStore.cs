// Reads return detached state. A successful write replaces the complete snapshot.
public interface IPlayerSaveStore
{
    PlayerSave Read();
    void Write(PlayerSave save);
}
