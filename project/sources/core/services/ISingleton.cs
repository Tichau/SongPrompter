namespace Core.Services;

public interface ISingleton<TOwner>
{
    void Initialize() { }

    void Update() { }

    void Release() { }
}
