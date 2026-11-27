using Avalonia.Threading;

namespace MegaHubPlus;

// Moves the title right away but waits 2 seconds before the background changes, so flicking through games stays calm.
internal sealed class GameRowReveal : IDisposable
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    readonly Action<Game> paintSelection;
    readonly Action<Game> paintBackdrop;
    Game? revealed;

    public Game? Selected { get; private set; }

    public GameRowReveal(Action<Game> paintSelection, Action<Game> paintBackdrop)
    {
        this.paintSelection = paintSelection;
        this.paintBackdrop = paintBackdrop;
        timer.Tick += OnTick;
    }

    public void Select(Game game, bool immediately = false)
    {
        if (Equals(Selected, game))
        {
            if (immediately) RevealNow();
            return;
        }

        timer.Stop();
        Selected = game;
        paintSelection(game);
        if (immediately) RevealNow();
        else if (!Equals(revealed, game)) timer.Start();
    }

    void OnTick(object? sender, EventArgs e) => RevealNow();

    public void RevealNow()
    {
        timer.Stop();
        if (Selected is not { } game || Equals(revealed, game)) return;
        revealed = game;
        paintBackdrop(game);
    }

    // paint the backdrop again for the same game (for example after its fit changed)
    public void Repaint()
    {
        if (revealed is { } game) paintBackdrop(game);
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Tick -= OnTick;
    }
}
