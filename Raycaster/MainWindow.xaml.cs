using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Drawing;
using System.Windows.Threading;
//using System.Windows.Media;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Numerics;
using System;

namespace Raycaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MediaPlayer _musicPlayer;
        private MediaPlayer _gunPlayer;
        private MediaPlayer _jordGunPlayer;
        private MediaPlayer _screamPlayer;
        private DispatcherTimer _newSongTimer;
        private DispatcherTimer _mouseLockTimer;

        private struct MOUSERECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref MOUSERECT lpRect);

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int TEXTURE_WIDTH = 64;
        private const int TEXTURE_HEIGHT = 64;

        private struct PixelData
        {
            public byte Red;
            public byte Green;
            public byte Blue;
        }

        private struct Sprite : IComparable<Sprite>
        {
            public double X;
            public double Y;
            public int Texture;
            public double Distance;
            public int Health;
            public int ShootTimer;
            public bool Dead;
            public int DeadTimer;
            public int PainTimer;
            public int AgonyTimer;
            public int CompareTo(Sprite other)
            {
                return 0 - Distance.CompareTo(other.Distance);
            }
        };

        private Random _rnd = new Random(Environment.TickCount);
        private bool _mainMenu = true;

        private Bitmap _bitmapOne;
        private Bitmap _bitmapTwo;
        private Bitmap[] _tiles;
        private TextBlock _healthContainerBlock;
        private TextBlock _healthBlock;
        private PixelData[][][] _bmpPixels;
        private PixelData[][] _frame;

        private bool _running = true;

        private double _posX = 12;
        private double _posY = 3;

        private double _dirX = -1;
        private double _dirY = 0;

        private double _planeX = 0;
        private double _planeY = 0.66;

        private int _textureWidth = 64;
        private int _textureHeight = 64;

        private double _moveSpeed = 0.05;
        private double _runSpeed = 0.1;
        private double _rotSpeed = 0.01;

        private double _screenWidth = 640;
        private double _screenHeight = 480;

        private int _mapWidth = 30;
        private int _mapHeight = 30;
        private int[][] _world;

        private DispatcherTimer _timer;
        private bool _bufferToggle = false;
        private System.Windows.Controls.Image _bg;
        private System.Windows.Controls.Image _screen;
        private System.Windows.Controls.Image _gun;

        private bool _upPressed = false;
        private bool _downPressed = false;
        private bool _leftPressed = false;
        private bool _rightPressed = false;
        private bool _isRunning = false;

        private double _lastMouseX = -5000;
        private double _newMouseX = 0;
        private MOUSERECT _lockMouseRect;

        Bitmap _gunBMP = (Bitmap)Bitmap.FromFile("Gun.png");
        Bitmap _gunFlash = (Bitmap)Bitmap.FromFile("GunFlash.png");
        Bitmap _bgBMP = (Bitmap)Bitmap.FromFile("BG.png");

        private long _frameCounter = 0;
        private int _mouseSpeed = 0;
        private bool _mouseLock = false;
        private int _shoot = 0;

        private Sprite[] _sprites;
        private int _spriteCount;
        private int[] _spriteOrder;
        private double[] _spriteDistance;
        private double[] _zBuffer;
        private PixelData[][][] _spriteTextures;

        private int _hittableSprite;
        private List<int> _shootingSprites = new List<int>();
        private int _health = 100;
        private int _maxHealth = 100;
        private bool _youLose = false;
        private bool _youWin = false;
        private bool _mouseLocked = false;
        
        public MainWindow()
        {

            InitializeComponent();

            _bitmapOne = new(Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight));
            _bitmapTwo = new(Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight));

            _zBuffer = new double[Convert.ToInt32(_screenWidth)];

            LoadTiles();
            LoadSprites();            

            _frame = new PixelData[Convert.ToInt32(_screenWidth)][];
            for (int xx = 0; xx < _screenWidth; xx++)
            {
                _frame[xx] = new PixelData[Convert.ToInt32(_screenHeight)];
            }

            this.KeyDown += MainWindow_KeyDown;
            this.KeyUp += MainWindow_KeyUp;
            this.MouseMove += MainWindow_MouseMove;
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            _timer = new();
            _timer.Tick += _timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _timer.Start();

        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (_mouseLocked)
            {
                return;
            }

            if (_mainMenu || _youLose || _youWin)
            {
                _mainMenu = false;
                MakeMap();
                InitGameElements();
                GameLoop();
            }
            else
            {
                if (_shoot > 0)
                {
                    return;
                }
                _shoot = 12;
                string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                if (!baseFolder.EndsWith("\\"))
                {
                    baseFolder += "\\";
                }
                _gunPlayer.Open(new Uri(baseFolder + "Gun.mp3"));
                _gunPlayer.Play();
            }
        }


        private void InitGameElements()
        {

            int gunWidth = Convert.ToInt32(this.Width / 5);
            int gunHeight = Convert.ToInt32(gunWidth * 1.5);

            _gun = new()
            {
                Width = gunWidth,
                Height = gunHeight,
                Source = Imaging.CreateBitmapSourceFromHBitmap(_gunBMP.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            };

            this.mainCanv.Children.Add(_gun);
            Canvas.SetLeft(_gun, this.Width / 2 - (gunWidth / 2) + (gunWidth / 4));
            Canvas.SetTop(_gun, this.Height - gunHeight);

            _healthContainerBlock = new()
            {
                Background = System.Windows.Media.Brushes.Black,
                Width = 406,
                Height = 19,
            };
            this.mainCanv.Children.Add(_healthContainerBlock);
            Canvas.SetLeft(_healthContainerBlock, (this.Width / 2) - ((this.Height / _screenHeight) * _screenWidth) / 2 + 4);
            Canvas.SetTop(_healthContainerBlock, 7);

            _healthBlock = new()
            {
                Background = System.Windows.Media.Brushes.Red,
                Width = 400,
                Height = 15,
            };
            this.mainCanv.Children.Add(_healthBlock);
            Canvas.SetLeft(_healthBlock, (this.Width / 2) - ((this.Height / _screenHeight) * _screenWidth) / 2 + 6);
            Canvas.SetTop(_healthBlock, 9);

            _running = true;
            _health = 100;
            _youLose = false;
            _youWin = false;
            _posX = 12;
            _posY = 3;
            _dirX = -1;
            _dirY = 0;
            _planeX = 0;
            _planeY = 0.66;

        }


        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {

            if (_mouseLock)
            {
                SetCursorPos(Convert.ToInt32(this.Width / 2), Convert.ToInt32(this.Height / 2));
                _lastMouseX = Convert.ToInt32(this.Width / 2);
                return;
            }

            _mouseLock = true;
            _newMouseX = this.PointToScreen(Mouse.GetPosition(this)).X;
            SetCursorPos(Convert.ToInt32(this.Width / 2), Convert.ToInt32(this.Height / 2));
            double diff = Math.Abs(_newMouseX - _lastMouseX);
            if (diff >= 3)
            {
                _mouseSpeed += (_newMouseX > _lastMouseX ? 3 : -3);
            }
            else
            {
                _mouseSpeed += (_newMouseX > _lastMouseX ? 2 : -2);
            }
            _lastMouseX = Convert.ToInt32(this.Width / 2);
            if (_mouseSpeed > 6)
            {
                _mouseSpeed = 6;
            }
            else if (_mouseSpeed < -6) 
            {
                _mouseSpeed = -6;
            }

        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                _upPressed = true;
            }
            else if (e.Key == Key.S)
            {
                _downPressed = true;
            }
            else if (e.Key == Key.D)
            {
                _rightPressed = true;
            }
            else if (e.Key == Key.A)
            {
                _leftPressed = true;
            }
            else if (e.Key == Key.LeftShift)
            {
                _isRunning = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (_youLose || _youWin)
                {
                    Application.Current.Shutdown();
                }
                _running = false;
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                _upPressed = false;
            }
            else if (e.Key == Key.S)
            {
                _downPressed = false;
            }
            else if (e.Key == Key.D)
            {
                _rightPressed = false;
            }
            else if (e.Key == Key.A)
            {
                _leftPressed = false;
            }
            else if (e.Key == Key.LeftShift)
            {
                _isRunning = false;
            }
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();

            _musicPlayer = new();
            _musicPlayer.MediaEnded += _musicPlayer_MediaEnded;
            _musicPlayer.Stop();
            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (!baseFolder.EndsWith("\\"))
            {
                baseFolder += "\\";
            }
            _musicPlayer.Open(new Uri(baseFolder + "Music.mp3"));
            _musicPlayer.Play();

            _gunPlayer = new();
            _jordGunPlayer = new();
            _screamPlayer = new();

            _bg = new()
            {
                Width = this.Width,
                Height = this.Height,
                Stretch = System.Windows.Media.Stretch.Fill,
                Source = Imaging.CreateBitmapSourceFromHBitmap(_bgBMP.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            };
            this.mainCanv.Children.Add(_bg);

            _screen = new()
            {
                Width = (this.Height / _screenHeight) * _screenWidth,
                Height = this.Height,
                Source = Imaging.CreateBitmapSourceFromHBitmap(_bitmapTwo.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                Stretch = System.Windows.Media.Stretch.Fill
            };
            this.mainCanv.Children.Add(_screen);
            Canvas.SetLeft(_screen, (this.Width / 2) - ((this.Height / _screenHeight) * _screenWidth) / 2);
            Canvas.SetTop(_screen, 0);

            _lockMouseRect = new()
            {
                Top = Convert.ToInt32(this.Top + 40),
                Left = Convert.ToInt32(this.Left) + 20,
                Right = Convert.ToInt32(this.Left + this.Width) - 20,
                Bottom = Convert.ToInt32(this.Top + this.Height) - 20
            };
            ClipCursor(ref _lockMouseRect);
            this.Cursor = Cursors.None;
            _lastMouseX = Convert.ToInt32(this.Width / 2);

            Bitmap tit = (Bitmap)Bitmap.FromFile("Title.png");
            _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(tit.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        }

        private void _musicPlayer_MediaEnded(object? sender, EventArgs e)
        {
            _newSongTimer = new DispatcherTimer();
            _newSongTimer.Interval = new TimeSpan(0, 0, 5);
            _newSongTimer.Tick += _newSongTimer_Tick;
            _newSongTimer.Start();
        }

        private void _newSongTimer_Tick(object? sender, EventArgs e)
        {
            _newSongTimer.Stop();
            _musicPlayer.Stop();
            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (!baseFolder.EndsWith("\\"))
            {
                baseFolder += "\\";
            }
            _musicPlayer.Open(new Uri(baseFolder + "Music.mp3"));
            _musicPlayer.Play();
        }

        private void LoadSprites()
        {
            List<Bitmap> sprts = new();
            Bitmap jord1 = (Bitmap)Bitmap.FromFile("Jordan.png");
            sprts.Add(jord1);

            Bitmap jordShoot = (Bitmap)Bitmap.FromFile("JordanShoot.png");
            sprts.Add(jordShoot);

            Bitmap jordPain = (Bitmap)Bitmap.FromFile("JordanPain.png");
            sprts.Add(jordPain);

            Bitmap jordAgony = (Bitmap)Bitmap.FromFile("JordanAgony.png");
            sprts.Add(jordAgony);

            Bitmap jordDie1 = (Bitmap)Bitmap.FromFile("JordanDie1.png");
            sprts.Add(jordDie1);

            Bitmap jordDie2 = (Bitmap)Bitmap.FromFile("JordanDie2.png");
            sprts.Add(jordDie2);

            Bitmap jordDie3 = (Bitmap)Bitmap.FromFile("JordanDie3.png");
            sprts.Add(jordDie3);

            Bitmap jordDie4 = (Bitmap)Bitmap.FromFile("JordanDie4.png");
            sprts.Add(jordDie4);

            Bitmap[] sBMPs = sprts.ToArray();
            _spriteTextures = new PixelData[sBMPs.Length][][];

            for (int nn = 0; nn < sBMPs.Length; nn++)
            {
                _spriteTextures[nn] = new PixelData[sBMPs[nn].Width][];
                for (int xx = 0; xx < sBMPs[nn].Width; xx++)
                {
                    _spriteTextures[nn][xx] = new PixelData[sBMPs[nn].Height];
                    for (int yy = 0; yy < sBMPs[nn].Height; yy++)
                    {
                        System.Drawing.Color col = sBMPs[nn].GetPixel(xx, yy);
                        if (col.A == 0)
                        {
                            _spriteTextures[nn][xx][yy].Red = 255;
                            _spriteTextures[nn][xx][yy].Green = 0;
                            _spriteTextures[nn][xx][yy].Blue = 0;
                        }
                        else
                        {
                            _spriteTextures[nn][xx][yy].Red = col.R;
                            _spriteTextures[nn][xx][yy].Green = col.G;
                            _spriteTextures[nn][xx][yy].Blue = col.B;
                        }
                    }
                }
            }

        }


        private void LoadTiles()
        {

            List<Bitmap> tiles = new List<Bitmap>();
            
            Bitmap floor = (Bitmap)Bitmap.FromFile("Floor.png");
            tiles.Add(floor);

            Bitmap floorAlt = (Bitmap)Bitmap.FromFile("FloorAlt.png");
            tiles.Add(floorAlt);

            Bitmap floorThird = (Bitmap)Bitmap.FromFile("FloorThird.png");
            tiles.Add(floorThird);

            Bitmap wall = (Bitmap)Bitmap.FromFile("Wall.png");
            tiles.Add(wall);

            Bitmap wallAlt = (Bitmap)Bitmap.FromFile("WallAlt.png");
            tiles.Add(wallAlt);

            Bitmap wallThird = (Bitmap)Bitmap.FromFile("WallThird.png");
            tiles.Add(wallThird);

            Bitmap wallSep = (Bitmap)Bitmap.FromFile("WallSep.png");
            tiles.Add(wallSep);

            Bitmap ceiling = (Bitmap)Bitmap.FromFile("Ceiling.png");
            tiles.Add(ceiling);

            Bitmap ceilingAlt = (Bitmap)Bitmap.FromFile("CeilingAlt.png");
            tiles.Add(ceilingAlt);

            Bitmap ceilingThird = (Bitmap)Bitmap.FromFile("CeilingThird.png");
            tiles.Add(ceilingThird);

            Bitmap wallDark = (Bitmap)Bitmap.FromFile("WallDark.png");
            tiles.Add(wallDark);

            Bitmap wallAltDark = (Bitmap)Bitmap.FromFile("WallAltDark.png");
            tiles.Add(wallAltDark);

            Bitmap wallThirdDark = (Bitmap)Bitmap.FromFile("WallThirdDark.png");
            tiles.Add(wallThirdDark);

            Bitmap wallSepDark = (Bitmap)Bitmap.FromFile("WallSepDark.png");
            tiles.Add(wallSepDark);

            _tiles = tiles.ToArray();
            _bmpPixels = new PixelData[_tiles.Length][][];

            for (int nn = 0; nn < _tiles.Length; nn++)
            {
                _bmpPixels[nn] = new PixelData[_tiles[nn].Width][];
                for (int xx = 0; xx < _tiles[nn].Width; xx++)
                {
                    _bmpPixels[nn][xx] = new PixelData[_tiles[nn].Height];
                    for (int yy = 0; yy < _tiles[nn].Height; yy++)
                    {
                        System.Drawing.Color col = _tiles[nn].GetPixel(xx, yy);
                        _bmpPixels[nn][xx][yy].Red = col.R;
                        _bmpPixels[nn][xx][yy].Green = col.G;
                        _bmpPixels[nn][xx][yy].Blue = col.B;
                    }
                }
            }

        }

        private void MakeMap()
        {

            string[] mapLines = System.IO.File.ReadAllLines("Map.txt");

            Random rnd = new Random(757);
            _world = new int[_mapWidth][];

            for (int xx = 0; xx < _mapWidth ; xx++)
            {
                _world[xx] = new int[_mapHeight];
                for (int yy = 0; yy < _mapHeight; yy++)
                {
                    _world[xx][yy] = Convert.ToInt32(mapLines[yy].Substring(xx, 1));
                }
            }

            List<Sprite> sLst = new();
            string[] lines = System.IO.File.ReadAllLines("Sprites.txt");
            foreach (string line in lines)
            {
                string[] splits = line.Split(',');
                Sprite s = new()
                {
                    X = Convert.ToInt32(splits[0]),
                    Y = Convert.ToInt32(splits[1]),
                    Texture = 0,
                    Health = 100,
                    ShootTimer = _rnd.Next(100, 250)
                };
                sLst.Add(s);
            }

            _sprites = sLst.ToArray();

        }

        public void GameLoop()
        {

            bool allDead = true;

            do
            {

                long startTicks = Environment.TickCount64;

                for (int xx = 0; xx < _screenWidth - 1; xx++)
                {

                    _frameCounter += 1;

                    double cameraX = 2 * (xx / _screenWidth) - 1;
                    double rayDirX = _dirX + _planeX * cameraX;
                    double rayDirY = _dirY + _planeY * cameraX;

                    int mapX = Convert.ToInt32(Math.Floor(_posX));
                    int mapY = Convert.ToInt32(Math.Floor(_posY));

                    double deltaDistX = Math.Abs(1 / rayDirX);
                    double deltaDistY = Math.Abs(1 / rayDirY);

                    int stepX;
                    int stepY;
                    double sideDistX;
                    double sideDistY;
                    if (rayDirX < 0)
                    {
                        stepX = -1;
                        sideDistX = (_posX - mapX) * deltaDistX;
                    }
                    else
                    {
                        stepX = 1;
                        sideDistX = (mapX + 1.0 - _posX) * deltaDistX;
                    }

                    if (rayDirY < 0)
                    {
                        stepY = -1;
                        sideDistY = (_posY - mapY) * deltaDistY;
                    }
                    else
                    {
                        stepY = 1;
                        sideDistY = (mapY + 1.0 - _posY) * deltaDistY;
                    }

                    bool hit = false;
                    bool side = false;
                    double perpWallDist;
                    while (!hit)
                    {
                        if (sideDistX < sideDistY)
                        {
                            sideDistX = sideDistX + deltaDistX;
                            mapX = mapX + stepX;
                            side = false;
                        }
                        else
                        {
                            sideDistY = sideDistY + deltaDistY;
                            mapY = mapY + stepY;
                            side = true;
                        }
                        if (mapX == _mapWidth - 1)
                        {
                            hit = true;
                        }
                        else if (mapY == _mapHeight - 1)
                        {
                            hit = true;
                        }
                        else if (_world[mapX][mapY] > 2)
                        {
                            hit = true;
                        }
                    }

                    if (!side)
                    {
                        perpWallDist = (mapX - _posX + (1 - stepX) / 2) / rayDirX;
                    }
                    else
                    {
                        perpWallDist = (mapY - _posY + (1 - stepY) / 2) / rayDirY;
                    }

                    if (perpWallDist < 0.000001)
                    {
                        perpWallDist = 0.000001;
                    }

                    int lineHeight = Convert.ToInt32(_screenHeight / perpWallDist);

                    int drawStart = (0 - lineHeight) / 2 + (Convert.ToInt32(_screenHeight) / 2);
                    if (drawStart < 0)
                    {
                        drawStart = 0;
                    }
                    else if (drawStart >= _screenHeight)
                    {
                        drawStart = Convert.ToInt32(_screenHeight - 1);
                    }

                    int drawEnd = lineHeight / 2 + (Convert.ToInt32(_screenHeight) / 2);
                    if (drawEnd >= _screenHeight)
                    {
                        drawEnd = Convert.ToInt32(_screenHeight - 1);
                    }
                    else if (drawEnd < 0)
                    {
                        drawEnd = 0;
                    }

                    int textureNr = _world[mapX][mapY];
                    double wallX;
                    if (!side)
                    {
                        wallX = _posY + perpWallDist * rayDirY;
                    }
                    else
                    {
                        wallX = _posX + perpWallDist * rayDirX;
                    }

                    wallX = wallX - Math.Floor(wallX);

                    int texX = Convert.ToInt32(Math.Floor(wallX * _textureWidth));
                    int texY;

                    if (texX < 0)
                    {
                        texX = 0;
                    }
                    if (texX >= _textureWidth)
                    {
                        texX = _textureWidth - 1;
                    }

                    int yOffset;
                    for (int yy = drawStart; yy <= drawEnd - 1; yy++)
                    {
                        yOffset = Convert.ToInt32(yy - _screenHeight * 0.5 + lineHeight * 0.5);
                        texY = ((yOffset * _textureHeight) / lineHeight);

                        if (!side)
                        {
                            _frame[xx][yy] = _bmpPixels[_world[mapX][mapY]][texX][texY];
                        }
                        else
                        {
                            _frame[xx][yy] = _bmpPixels[_world[mapX][mapY] + 7][texX][texY];
                        }
                    }

                    double floorX;
                    double floorY;

                    if (!side && rayDirX > 0)
                    {
                        floorX = mapX;
                        floorY = mapY + wallX;
                    }
                    else if (!side && rayDirX < 0)
                    {
                        floorX = mapX + 1;
                        floorY = mapY + wallX;
                    }
                    else if (side && rayDirY > 0)
                    {
                        floorX = mapX + wallX;
                        floorY = mapY;
                    }
                    else // side and neg RayDirY
                    {
                        floorX = mapX + wallX;
                        floorY = mapY + 1;
                    }

                    double distWall = perpWallDist;
                    double currentDist;
                    double weight;
                    double currentFloorX;
                    double currentFloorY;
                    int floorTexX;
                    int floorTexY;

                    if (drawEnd < 0)
                    {
                        drawEnd = Convert.ToInt32(_screenHeight);
                    }
                    for (int yy = drawEnd + 1; yy <= _screenHeight - 1; yy++)
                    {
                        currentDist = _screenHeight / (2.0 * yy - _screenHeight);
                        weight = currentDist / distWall;

                        currentFloorX = weight * floorX + (1.0 - weight) * _posX;
                        currentFloorY = weight * floorY + (1.0 - weight) * _posY;
                       
                        Math.DivRem(Convert.ToInt32(Math.Floor(currentFloorX * _textureWidth)), _textureWidth, out floorTexX);
                        Math.DivRem(Convert.ToInt32(Math.Floor(currentFloorY * _textureHeight)), _textureHeight, out floorTexY);

                        _frame[xx][yy] = _bmpPixels[_world[Convert.ToInt32(Math.Floor(currentFloorX))][Convert.ToInt32(Math.Floor(currentFloorY))]][floorTexX][floorTexY];
                        _frame[xx][Convert.ToInt32(_screenHeight) - yy] = _bmpPixels[_world[Convert.ToInt32(Math.Floor(currentFloorX))][Convert.ToInt32(Math.Floor(currentFloorY))] + 7][floorTexX][floorTexY];
                    }

                    _zBuffer[xx] = perpWallDist;

                }

                Bitmap buffer = (_bufferToggle ? _bitmapTwo : _bitmapOne);
                BitmapData bmData = buffer.LockBits(new Rectangle(0, 0, Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight)), 
                    ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                int stride = bmData.Stride;
                unsafe
                {
                    byte* pointer = (byte*)bmData.Scan0;
                    for (int xx = 0; xx < _screenWidth; xx++)
                    {
                        for (int yy = 0; yy < _screenHeight; yy++)
                        {
                            pointer[(xx * 3) + yy * stride] = _frame[xx][yy].Blue;
                            pointer[(xx * 3) + yy * stride + 1] = _frame[xx][yy].Green;
                            pointer[(xx * 3) + yy * stride + 2] = _frame[xx][yy].Red;
                        }
                    }
                }
                

                for (int nn = 0; nn < _sprites.Length; nn++)
                {
                    _sprites[nn].Distance = ((_posX - _sprites[nn].X) * (_posX - _sprites[nn].X) + 
                        (_posY - _sprites[nn].Y) * (_posY - _sprites[nn].Y)); 
                }
                Array.Sort(_sprites);

                _hittableSprite = -1;
                _shootingSprites.Clear();
                for (int nn = 0; nn < _sprites.Length; nn++)
                {

                    if (!_sprites[nn].Dead)
                    {
                        if (_sprites[nn].AgonyTimer == 0 && _sprites[nn].PainTimer == 0)
                        {
                            _sprites[nn].ShootTimer -= 1;
                            if (_sprites[nn].ShootTimer == 0)
                            {
                                _sprites[nn].Texture = 1;
                            }
                            else if (_sprites[nn].ShootTimer == -25)
                            {
                                _sprites[nn].Texture = 0;
                                _sprites[nn].ShootTimer = _rnd.Next(90, 180);
                            }
                        }
                        else if (_sprites[nn].AgonyTimer > 0)
                        {
                            _sprites[nn].AgonyTimer -= 1;
                            if (_sprites[nn].AgonyTimer == 0)
                            {
                                _sprites[nn].Texture = 0;
                            }
                        }
                        else if (_sprites[nn].PainTimer > 0)
                        {
                            _sprites[nn].PainTimer -= 1;
                            if (_sprites[nn].PainTimer == 0)
                            {
                                _sprites[nn].Texture = 0;
                            }
                        }
                    }
                    else
                    {
                        if (_sprites[nn].Texture < 7)
                        {
                            _sprites[nn].DeadTimer += 1;
                            if (_sprites[nn].DeadTimer % 15 == 0)
                            {
                                _sprites[nn].Texture += 1;
                            }
                        }
                    }

                    double spriteX = _sprites[nn].X - _posX;
                    double spriteY = _sprites[nn].Y - _posY;

                    double invDet = 1.0 / (_planeX * _dirY - _dirX * _planeY); 

                    double transformX = invDet * (_dirY * spriteX - _dirX * spriteY);
                    double transformY = invDet * (-_planeY * spriteX + _planeX * spriteY);

                    int spriteScreenX; // = Convert.ToInt32(Math.Floor((_screenWidth / 2) * (1 + transformX / transformY)));
                    int spriteHeight; // = Convert.ToInt32(Math.Abs(Math.Floor(_screenHeight / (transformY))));

                    try
                    {
                        spriteScreenX = Convert.ToInt32(Math.Floor((_screenWidth / 2) * (1 + transformX / transformY)));
                        spriteHeight = Convert.ToInt32(Math.Abs(Math.Floor(_screenHeight / (transformY))));
                    }
                    catch { continue; }

                    int drawStartY = -spriteHeight / 2 + Convert.ToInt32(_screenHeight) / 2;
                    if (drawStartY < 0) 
                    {
                        drawStartY = 0;
                    }
                    int drawEndY = spriteHeight / 2 + Convert.ToInt32(_screenHeight) / 2;
                    if (drawEndY >= _screenHeight) 
                    {
                        drawEndY = Convert.ToInt32(_screenHeight) - 1;
                    }

                    int spriteWidth = Convert.ToInt32(Math.Abs(Math.Floor(_screenHeight / (transformY))));
                    int drawStartX = -spriteWidth / 2 + spriteScreenX;
                    if (drawStartX < 0)
                    {
                        drawStartX = 0;
                    }
                    int drawEndX = spriteWidth / 2 + spriteScreenX;
                    if (drawEndX >= _screenWidth) 
                    {
                        drawEndX = Convert.ToInt32(_screenWidth) - 1;
                    }
                                        
                    unsafe
                    {
                        byte* pointer = (byte*)bmData.Scan0;
                        for (int xx = drawStartX; xx < drawEndX; xx++)
                        {
                            int texX = Convert.ToInt32(Math.Floor(Convert.ToDouble(256 * (xx -
                                (-spriteWidth / 2 + spriteScreenX)) * TEXTURE_WIDTH / spriteWidth) / 256));
                            if (transformY > 0 && xx > 0 && xx < _screenWidth && transformY < _zBuffer[xx])
                            {
                                for (int yy = drawStartY; yy < drawEndY; yy++)
                                {
                                    int d = (yy) * 256 - Convert.ToInt32(_screenHeight) * 128 + spriteHeight * 128;
                                    int texY = ((d * TEXTURE_HEIGHT) / spriteHeight) / 256;
                                    if (texX >= 0 && texX <= TEXTURE_WIDTH - 1 && texY > 0 && texY <= TEXTURE_HEIGHT - 1)
                                    {
                                        PixelData pix = _spriteTextures[_sprites[nn].Texture][texX][texY];
                                        if (pix.Red != 255 || (pix.Blue != 0 || pix.Green != 0))
                                        {
                                            pointer[(xx * 3) + yy * stride] = pix.Blue;
                                            pointer[(xx * 3) + yy * stride + 1] = pix.Green;
                                            pointer[(xx * 3) + yy * stride + 2] = pix.Red;
                                            if (xx == Convert.ToInt32(_screenWidth) / 2 && !_sprites[nn].Dead)
                                            {
                                                _hittableSprite = nn;
                                            }
                                        }                                        
                                    }
                                    if (texX > 25 && texX < 40 && _sprites[nn].Texture == 1 && 
                                        _sprites[nn].ShootTimer == 0 && !_shootingSprites.Contains(nn))
                                    {
                                        _shootingSprites.Add(nn);
                                    }
                                }
                            }
                        }
                    }
                }

                buffer.UnlockBits(bmData);

                if (_bufferToggle)
                {
                    IntPtr bmpHandle = _bitmapTwo.GetHbitmap();
                    _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(bmpHandle, IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(bmpHandle);
                }
                else
                {
                    IntPtr bmpHandle = _bitmapOne.GetHbitmap();
                    _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(bmpHandle, IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(bmpHandle);
                }

                if (_shoot == 12)
                {
                    IntPtr gunHandle = _gunFlash.GetHbitmap();
                    _gun.Source = Imaging.CreateBitmapSourceFromHBitmap(gunHandle, IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(gunHandle);
                    if (_hittableSprite != -1 && !_sprites[_hittableSprite].Dead)
                    {
                        _sprites[_hittableSprite].Health -= _rnd.Next(12, 28);
                        if (_sprites[_hittableSprite].Health <= 0 )
                        {
                            _sprites[_hittableSprite].Dead = true;
                            _sprites[_hittableSprite].Texture = 4;
                            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                            if (!baseFolder.EndsWith("\\"))
                            {
                                baseFolder += "\\";
                            }
                            _screamPlayer.Open(new Uri(baseFolder + "JordDie.mp3"));
                            _screamPlayer.Play();
                        }
                        else
                        {
                            if (_rnd.Next(1, 100) < 50)
                            {
                                if (_rnd.Next(1, 100) < 50)
                                {
                                    _sprites[_hittableSprite].PainTimer = 20;
                                    _sprites[_hittableSprite].AgonyTimer = 0;
                                    _sprites[_hittableSprite].Texture = 2;
                                }
                                else
                                {
                                    _sprites[_hittableSprite].PainTimer = 0;
                                    _sprites[_hittableSprite].AgonyTimer = 20;
                                    _sprites[_hittableSprite].Texture = 3;
                                }
                            }
                        }
                    }
                }
                else if (_shoot == 3)
                {
                    IntPtr gunHandle = _gunBMP.GetHbitmap();
                    _gun.Source = Imaging.CreateBitmapSourceFromHBitmap(gunHandle, IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(gunHandle);
                }

                if (_shoot > 0)
                {
                    _shoot -= 1;
                }

                foreach (int nn in _shootingSprites)
                {
                    if (_rnd.Next(1, 100) > 20)
                    {
                        _health -= _rnd.Next(1, 5);
                    }
                    string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                    if (!baseFolder.EndsWith("\\"))
                    {
                        baseFolder += "\\";
                    }
                    _jordGunPlayer.Open(new Uri(baseFolder + "JordGun.mp3"));
                    _jordGunPlayer.Play();

                    if (_health <= 0)
                    {
                        _running = false;
                        break;
                    }
                    _healthBlock.Width = _health * 4;

                }

                _bufferToggle = !_bufferToggle;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                double nextPosX = _posX;
                double nextPosY = _posY;
                if (_upPressed)
                {
                    nextPosX = nextPosX + (_dirX * (_isRunning ? _runSpeed :_moveSpeed));
                    nextPosY = nextPosY + (_dirY * (_isRunning ? _runSpeed : _moveSpeed));
                }
                if (_downPressed)
                {
                    nextPosX = nextPosX - (_dirX * (_isRunning ? _runSpeed : _moveSpeed));
                    nextPosY = nextPosY - (_dirY * (_isRunning ? _runSpeed : _moveSpeed));
                }
                if (_rightPressed)
                {
                    nextPosX = nextPosX + (_planeX * (_isRunning ? _runSpeed : _moveSpeed));
                    nextPosY = nextPosY + (_planeY * (_isRunning ? _runSpeed : _moveSpeed));
                }
                if (_leftPressed)
                {
                    nextPosX = nextPosX - (_planeX * (_isRunning ? _runSpeed : _moveSpeed));
                    nextPosY = nextPosY - (_planeY * (_isRunning ? _runSpeed : _moveSpeed));
                }

                if (Walkable(nextPosX, nextPosY))
                {
                    _posX = nextPosX;
                    _posY = nextPosY;
                }

                if (_mouseSpeed < 0) 
                {
                    _mouseSpeed = 0 - _mouseSpeed;
                    double oldDirX = _dirX;
                    _dirX = _dirX * Math.Cos(_rotSpeed * _mouseSpeed) - _dirY * Math.Sin(_rotSpeed * _mouseSpeed);
                    _dirY = oldDirX * Math.Sin(_rotSpeed * _mouseSpeed) + _dirY * Math.Cos(_rotSpeed * _mouseSpeed);
                    double oldPlaneX = _planeX;
                    _planeX = _planeX * Math.Cos(_rotSpeed * _mouseSpeed) - _planeY * Math.Sin(_rotSpeed * _mouseSpeed);
                    _planeY = oldPlaneX * Math.Sin(_rotSpeed * _mouseSpeed) + _planeY * Math.Cos(_rotSpeed * _mouseSpeed);
                    _mouseSpeed = 0 - _mouseSpeed;
                } 
                else if (_newMouseX > _lastMouseX) 
                {
                    double oldDirX = _dirX;
                    _dirX = _dirX * Math.Cos((0 - _rotSpeed) * _mouseSpeed) - _dirY * Math.Sin((0 - _rotSpeed) * _mouseSpeed);
                    _dirY = oldDirX * Math.Sin((0 - _rotSpeed) * _mouseSpeed) + _dirY * Math.Cos((0 - _rotSpeed) * _mouseSpeed);
                    double oldPlaneX = _planeX;
                    _planeX = _planeX * Math.Cos((0 - _rotSpeed) * _mouseSpeed) - _planeY * Math.Sin((0 - _rotSpeed) * _mouseSpeed);
                    _planeY = oldPlaneX * Math.Sin((0 - _rotSpeed) * _mouseSpeed) + _planeY * Math.Cos((0 - _rotSpeed) * _mouseSpeed);                    
                }

                if (_mouseSpeed > 0)
                {
                    _mouseSpeed -= 1;
                }
                else if (_mouseSpeed < 0)
                {
                    _mouseSpeed += 1;
                }
                _mouseLock = false;

                allDead = true;
                for (int nn = 0; nn < _sprites.Length; nn++)
                {
                    if (_sprites[nn].Texture != 7)
                    {
                        allDead = false;
                        break;
                    }
                }
                if (allDead)
                {
                    _running = false;
                }

                do
                {

                } while (Environment.TickCount64 < startTicks + 2);

            } while (_running);

            if (_health <= 0)
            {
                EndGame(false);
            }
            else if (allDead)
            {
                EndGame(true);
            }
            else
            {
                Application.Current.Shutdown();
            }

        }

        private bool Walkable(double xx, double yy)
        {

            double wallHitDist = 0.1;

            if (_world[Convert.ToInt32(Math.Floor(xx + wallHitDist))][Convert.ToInt32(Math.Floor(yy + wallHitDist))] > 2)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx - wallHitDist))][Convert.ToInt32(Math.Floor(yy - wallHitDist))] > 2)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx + wallHitDist))][Convert.ToInt32(Math.Floor(yy - wallHitDist))] > 2)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx - wallHitDist))][Convert.ToInt32(Math.Floor(yy + wallHitDist))] > 2)
            {
                return false;
            }
            return true;
        }

        private void EndGame(bool victory)
        {

            mainCanv.Children.Remove(_gun);
            mainCanv.Children.Remove(_healthBlock);
            mainCanv.Children.Remove(_healthContainerBlock);

            if (victory)
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile("YouWin.png");
                _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                _youWin = true;
            }
            else
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile("YouLose.png");
                _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                _youLose = true;
            }

            _mouseLocked = true;
            _mouseLockTimer = new();
            _mouseLockTimer.Interval = new TimeSpan(0,0,0,0,1000);
            _mouseLockTimer.Tick += _mouseLockTimer_Tick;
            _mouseLockTimer.Start();

        }

        private void _mouseLockTimer_Tick(object? sender, EventArgs e)
        {
            _mouseLockTimer.Stop();
            _mouseLocked = false;
        }
    }
}