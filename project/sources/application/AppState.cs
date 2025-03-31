
using Core.Services;
using Data;

namespace Application;

public class AppState() : ISingleton<App>
{
    public Setlist? CurrentSetlist;
    public int CurrentSongIndex = 0;

    public void Bind()
    {
    }
}
