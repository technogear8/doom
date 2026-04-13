using System;
using System.Diagnostics;

namespace ConsoleDoom
{
    class Program
    {
        // --- Mapa (1 = parede, 0 = corredor) ---
        static readonly string[] Map = {
            "####################",
            "#........#.........#",
            "#........#.........#",
            "#....##..#..###....#",
            "#....#.............#",
            "#....#.......#.....#",
            "#....########......#",
            "#..................#",
            "#...........#......#",
            "#...........#......#",
            "#######.....########",
            "#..........#.......#",
            "#..........#.......#",
            "#....###...#.......#",
            "#....#.............#",
            "#....#.............#",
            "#..............#...#",
            "#..............#...#",
            "#..................#",
            "####################"
        };

        static readonly int MapWidth  = Map[0].Length;
        static readonly int MapHeight = Map.Length;

        // --- Tela ---
        const int ScreenWidth  = 120;
        const int ScreenHeight = 40;

        // --- Câmera / Player ---
        static double playerX   = 1.5;
        static double playerY   = 1.5;
        static double playerDir = 0.0;          // radianos (aponta para +X)
        static double fov       = Math.PI / 3.0; // 60°

        // --- Velocidade ---
        const double MoveSpeed = 3.0;  // unidades/segundo
        const double RotSpeed  = 2.0;  // rad/segundo

        static void Main()
        {
            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "ConsoleDoom – C#";

            // Tenta redimensionar se o terminal permitir
            try { Console.SetWindowSize(ScreenWidth, ScreenHeight + 2); } catch { }
            try { Console.SetBufferSize(ScreenWidth, ScreenHeight + 2); } catch { }

            char[] screen = new char[ScreenWidth * ScreenHeight];

            var sw       = Stopwatch.StartNew();
            double prev  = sw.Elapsed.TotalSeconds;

            while (true)
            {
                // ── Tempo delta ──────────────────────────────────────────
                double now = sw.Elapsed.TotalSeconds;
                double dt  = now - prev;
                prev       = now;

                // ── Entrada ───────────────────────────────────────────────
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;

                    switch (key)
                    {
                        // Rotacionar
                        case ConsoleKey.LeftArrow:  playerDir -= RotSpeed  * dt; break;
                        case ConsoleKey.RightArrow: playerDir += RotSpeed  * dt; break;

                        // Mover frente/trás
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.W:
                        {
                            double nx = playerX + Math.Cos(playerDir) * MoveSpeed * dt;
                            double ny = playerY + Math.Sin(playerDir) * MoveSpeed * dt;
                            if (nx > 0 && nx < MapWidth  && Map[(int)playerY][(int)nx] != '#') playerX = nx;
                            if (ny > 0 && ny < MapHeight && Map[(int)ny][(int)playerX] != '#') playerY = ny;
                            break;
                        }
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.S:
                        {
                            double nx = playerX - Math.Cos(playerDir) * MoveSpeed * dt;
                            double ny = playerY - Math.Sin(playerDir) * MoveSpeed * dt;
                            if (nx > 0 && nx < MapWidth  && Map[(int)playerY][(int)nx] != '#') playerX = nx;
                            if (ny > 0 && ny < MapHeight && Map[(int)ny][(int)playerX] != '#') playerY = ny;
                            break;
                        }

                        // Strafe
                        case ConsoleKey.A:
                        {
                            double nx = playerX + Math.Cos(playerDir - Math.PI / 2) * MoveSpeed * dt;
                            double ny = playerY + Math.Sin(playerDir - Math.PI / 2) * MoveSpeed * dt;
                            if (nx > 0 && nx < MapWidth  && Map[(int)playerY][(int)nx] != '#') playerX = nx;
                            if (ny > 0 && ny < MapHeight && Map[(int)ny][(int)playerX] != '#') playerY = ny;
                            break;
                        }
                        case ConsoleKey.D:
                        {
                            double nx = playerX + Math.Cos(playerDir + Math.PI / 2) * MoveSpeed * dt;
                            double ny = playerY + Math.Sin(playerDir + Math.PI / 2) * MoveSpeed * dt;
                            if (nx > 0 && nx < MapWidth  && Map[(int)playerY][(int)nx] != '#') playerX = nx;
                            if (ny > 0 && ny < MapHeight && Map[(int)ny][(int)playerX] != '#') playerY = ny;
                            break;
                        }

                        case ConsoleKey.Escape: return;
                    }
                }

                // ── Raycasting ────────────────────────────────────────────
                for (int x = 0; x < ScreenWidth; x++)
                {
                    // Ângulo do raio para esta coluna
                    double rayAngle = (playerDir - fov / 2.0) + (x / (double)ScreenWidth) * fov;

                    double rayDirX = Math.Cos(rayAngle);
                    double rayDirY = Math.Sin(rayAngle);

                    // DDA (Digital Differential Analyser)
                    double distX = Math.Abs(1.0 / (rayDirX == 0 ? 1e-10 : rayDirX));
                    double distY = Math.Abs(1.0 / (rayDirY == 0 ? 1e-10 : rayDirY));

                    int mapX = (int)playerX;
                    int mapY = (int)playerY;

                    double sideDistX, sideDistY;
                    int stepX, stepY;

                    if (rayDirX < 0) { stepX = -1; sideDistX = (playerX - mapX) * distX; }
                    else             { stepX =  1; sideDistX = (mapX + 1.0 - playerX) * distX; }

                    if (rayDirY < 0) { stepY = -1; sideDistY = (playerY - mapY) * distY; }
                    else             { stepY =  1; sideDistY = (mapY + 1.0 - playerY) * distY; }

                    bool hit  = false;
                    bool side = false; // false = parede X, true = parede Y
                    double wallDist = 0;

                    while (!hit)
                    {
                        if (sideDistX < sideDistY) { sideDistX += distX; mapX += stepX; side = false; }
                        else                        { sideDistY += distY; mapY += stepY; side = true;  }

                        if (mapX < 0 || mapX >= MapWidth || mapY < 0 || mapY >= MapHeight) { wallDist = 32; hit = true; break; }
                        if (Map[mapY][mapX] == '#') hit = true;
                    }

                    // Distância perpendicular (evita fish-eye)
                    wallDist = side ? (sideDistY - distY) : (sideDistX - distX);
                    if (wallDist < 0.001) wallDist = 0.001;

                    // Altura da parede nesta coluna
                    int wallHeight = (int)(ScreenHeight / wallDist);
                    int wallTop    = Math.Max(0, (ScreenHeight / 2) - (wallHeight / 2));
                    int wallBottom = Math.Min(ScreenHeight - 1, (ScreenHeight / 2) + (wallHeight / 2));

                    for (int y = 0; y < ScreenHeight; y++)
                    {
                        char shade;

                        if (y < wallTop)
                        {
                            // Teto
                            shade = ' ';
                        }
                        else if (y <= wallBottom)
                        {
                            // Parede – sombreamento por distância
                            if      (wallDist < 1.5) shade = '█';
                            else if (wallDist < 3.0) shade = '▓';
                            else if (wallDist < 5.0) shade = '▒';
                            else if (wallDist < 9.0) shade = '░';
                            else                     shade = '.';

                            // Paredes laterais (Y) ficam mais escuras → contraste
                            if (side && shade != '.')
                                shade = (shade == '█') ? '▓' : (shade == '▓') ? '▒' : '░';
                        }
                        else
                        {
                            // Chão – quanto mais longe, mais escuro
                            double floorDist = 1.0 - (2.0 * y - ScreenHeight) / (double)ScreenHeight;
                            if      (floorDist > 0.7) shade = '.';
                            else if (floorDist > 0.5) shade = '\'';
                            else if (floorDist > 0.3) shade = ':';
                            else if (floorDist > 0.1) shade = ';';
                            else                       shade = ' ';
                        }

                        screen[y * ScreenWidth + x] = shade;
                    }
                }

                // ── Mini-mapa (canto superior esquerdo) ───────────────────
                int mmW = 20, mmH = 10;
                for (int my = 0; my < mmH; my++)
                for (int mx = 0; mx < mmW; mx++)
                {
                    int wx = mx, wy = my;
                    char c;
                    if (wx == (int)playerX && wy == (int)playerY)
                        c = 'P';
                    else if (wy < MapHeight && wx < MapWidth)
                        c = (Map[wy][wx] == '#') ? '#' : '.';
                    else
                        c = ' ';
                    screen[my * ScreenWidth + mx] = c;
                }

                // ── Render ────────────────────────────────────────────────
                Console.SetCursorPosition(0, 0);
                Console.Write(new string(screen));

                // ── HUD ───────────────────────────────────────────────────
                Console.SetCursorPosition(0, ScreenHeight);
                string fps = $"FPS:{1.0/(dt < 0.001 ? 0.001 : dt):F0}  Pos:({playerX:F1},{playerY:F1})  Dir:{playerDir * 180 / Math.PI:F0}°  [WASD/Setas mover | ESC sair]";
                if (fps.Length > ScreenWidth) fps = fps[..ScreenWidth];
                Console.Write(fps.PadRight(ScreenWidth));

                // ── Cap de FPS (~60 Hz) ───────────────────────────────────
                double elapsed = sw.Elapsed.TotalSeconds - now;
                int sleepMs = (int)((1.0 / 60.0 - elapsed) * 1000);
                if (sleepMs > 0) System.Threading.Thread.Sleep(sleepMs);
            }
        }
    }
}