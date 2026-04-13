using DoomGame.Engine;
using DoomGame.World;

public class Game
{
    private readonly Map       _map;
    private readonly Player    _player;
    private readonly Raycaster _raycaster;
    private readonly Renderer  _renderer;
    private readonly Input     _input;

    private readonly double[] _distances = new double[Constants.ScreenWidth];

    public Game(Map map, Player player)
    {
        _map       = map;
        _player    = player;
        _raycaster = new Raycaster(map);
        _renderer  = new Renderer();
        _input     = new Input();
    }

    public void Run()
    {
        // Configura o console
        Console.CursorVisible = false;
        Console.Title = "DoomGame — ESC para sair";

        try { Console.SetWindowSize(Constants.ScreenWidth + 1, Constants.ScreenHeight + 3); } catch { }
        try { Console.SetBufferSize(Constants.ScreenWidth + 1, Constants.ScreenHeight + 4); } catch { }

        var frameTime     = TimeSpan.FromSeconds(1.0 / Constants.TargetFps);
        var lastFrameTime = DateTime.UtcNow;
        int fps           = 0;

        while (true)
        {
            var now   = DateTime.UtcNow;
            double dt = (now - lastFrameTime).TotalSeconds;
            lastFrameTime = now;
            fps = dt > 0 ? (int)(1.0 / dt) : Constants.TargetFps;

            // 1. Entrada
            _input.Poll();
            if (_input.Quit) break;

            // 2. Atualiza jogador
            _player.Update(_input.MoveForward, _input.MoveBack,
                           _input.TurnLeft,    _input.TurnRight, dt);

            // 3. Raycasting: um raio por coluna de tela
            double halfFov = Constants.Fov / 2.0;
            for (int col = 0; col < Constants.ScreenWidth; col++)
            {
                double rayAngle = _player.Angle - halfFov
                    + (col / (double)Constants.ScreenWidth) * Constants.Fov;

                _distances[col] = _raycaster.Cast(_player.X, _player.Y, rayAngle);
            }

            // 4. Renderiza
            _renderer.Draw(_distances, _player);
            _renderer.Flush(fps, _player);

            // 5. Limita FPS
            var elapsed = DateTime.UtcNow - now;
            if (elapsed < frameTime)
                Thread.Sleep(frameTime - elapsed);
        }

        Console.CursorVisible = true;
        Console.Clear();
        Console.WriteLine("Até mais, marine!");
    }
}