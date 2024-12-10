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

namespace Raycaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public struct MOUSERECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        static extern bool ClipCursor(ref MOUSERECT lpRect);

        private struct PixelData
        {
            public byte Red;
            public byte Green;
            public byte Blue;
        }

        private Bitmap _bitmapOne;
        private Bitmap _bitmapTwo;
        private Bitmap[] _tiles;
        private PixelData[][][] bmpPixels;
        private PixelData[][] frame;

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
        private double _rotSpeed = 0.01;

        private double _screenWidth = 900;
        private double _screenHeight = 675;

        private int _mapWidth = 20;
        private int _mapHeight = 20;
        private int[][] _world;

        private DispatcherTimer _timer;
        private bool _bufferToggle = false;
        private System.Windows.Controls.Image _screen;
        private System.Windows.Controls.Image _gun;

        private bool _upPressed = false;
        private bool _downPressed = false;
        private bool _leftPressed = false;
        private bool _rightPressed = false;

        private double _lastMouseX = -5000;
        private double _newMouseX = 0;
        private MOUSERECT _lockMouseRect;


        public MainWindow()
        {

            InitializeComponent();

            _bitmapOne = new(Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight));
            _bitmapTwo = new(Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight));

            _screen = new()
            {
                Width = _screenWidth,
                Height = _screenHeight,
                Source = Imaging.CreateBitmapSourceFromHBitmap(_bitmapTwo.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            };
            this.mainCanv.Children.Add(_screen);
            Canvas.SetLeft(_screen, 0);
            Canvas.SetTop(_screen, 0);

            Bitmap gunBMP = (Bitmap)Bitmap.FromFile("Gun.png");

            _gun = new()
            {
                Width = 300,
                Height = 300,
                Source = Imaging.CreateBitmapSourceFromHBitmap(gunBMP.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            };
            this.mainCanv.Children.Add(_gun);
            Canvas.SetLeft(_gun, _screenWidth / 2 - 30);
            Canvas.SetTop(_gun, _screenHeight - 300);

            LoadTiles();
            MakeMap();

            frame = new PixelData[Convert.ToInt32(_screenWidth)][];
            for (int xx = 0; xx < _screenWidth; xx++)
            {
                frame[xx] = new PixelData[Convert.ToInt32(_screenHeight)];
            }

            this.KeyDown += MainWindow_KeyDown;
            this.KeyUp += MainWindow_KeyUp;
            this.MouseMove += MainWindow_MouseMove;

            _timer = new();
            _timer.Tick += _timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _timer.Start();

        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_lastMouseX == -5000)
            {
                _lastMouseX = this.PointToScreen(Mouse.GetPosition(this)).X;
            }
            else
            {
                _newMouseX = this.PointToScreen(Mouse.GetPosition(this)).X;
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
            else if (e.Key == Key.Escape)
            {
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
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            _lockMouseRect = new()
            {
                Top = Convert.ToInt32(this.Top + 40),
                Left = Convert.ToInt32(this.Left) + 20,
                Right = Convert.ToInt32(this.Left + this.Width) - 20,
                Bottom = Convert.ToInt32(this.Top + this.Height) - 20
            };
            ClipCursor(ref _lockMouseRect);
            this.Cursor = Cursors.None;
            GameLoop();
        }

        private void LoadTiles()
        {

            List<Bitmap> tiles = new List<Bitmap>();
            
            Bitmap floor = (Bitmap)Bitmap.FromFile("Floor.png");
            tiles.Add(floor);
            
            Bitmap wall = (Bitmap)Bitmap.FromFile("Wall.png");
            tiles.Add(wall);

            _tiles = tiles.ToArray();
            bmpPixels = new PixelData[_tiles.Length][][];

            for (int nn = 0; nn < _tiles.Length; nn++)
            {
                bmpPixels[nn] = new PixelData[_tiles[nn].Width][];
                for (int xx = 0; xx < _tiles[nn].Width; xx++)
                {
                    bmpPixels[nn][xx] = new PixelData[_tiles[nn].Height];
                    for (int yy = 0; yy < _tiles[nn].Height; yy++)
                    {
                        System.Drawing.Color col = _tiles[nn].GetPixel(xx, yy);
                        bmpPixels[nn][xx][yy].Red = col.R;
                        bmpPixels[nn][xx][yy].Green = col.G;
                        bmpPixels[nn][xx][yy].Blue = col.B;
                    }
                }
            }

        }

        private void MakeMap()
        {

            Random rnd = new Random(757);
            _world = new int[_mapWidth][];

            for (int xx = 0; xx < _mapWidth; xx++)
            {
                _world[xx] = new int[_mapHeight];
                for (int yy = 0; yy < _mapHeight; yy++)
                {
                    if (xx == 0)
                    {
                        _world[xx][yy] = 1;
                    }
                    else if (xx == _mapWidth - 1)
                    {
                        _world[xx][yy] = 1;
                    }
                    else if (yy == 0 || yy == _mapHeight - 1)
                    {
                        _world[xx][yy] = 1;
                    }
                    else
                    {
                        if (rnd.Next(100) < 10)
                        {
                            _world[xx][yy] = 1;
                        }
                        else
                        {
                            _world[xx][yy] = 0;
                        }
                    }
                }
            }

        }

        public void GameLoop()
        {

            do
            {

                for (int xx = 0; xx < _screenWidth - 1; xx++)
                {

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
                        else if (_world[mapX][mapY] > 0)
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

                        frame[xx][yy] = bmpPixels[1][texX][texY];
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

                        frame[xx][yy] = bmpPixels[0][floorTexX][floorTexY];
                        frame[xx][Convert.ToInt32(_screenHeight) - yy] = bmpPixels[0][floorTexX][floorTexY];
                    }

                }

                Bitmap buffer = (_bufferToggle ? _bitmapTwo : _bitmapOne);
                BitmapData bmData = buffer.LockBits(new Rectangle(0, 0, Convert.ToInt32(_screenWidth), Convert.ToInt32(_screenHeight)), 
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride = bmData.Stride;
                unsafe
                {
                    byte* pointer = (byte*)bmData.Scan0;
                    for (int xx = 0; xx < _screenWidth; xx++)
                    {
                        for (int yy = 0; yy < _screenHeight; yy++)
                        {
                            pointer[(xx * 3) + yy * stride] = frame[xx][yy].Blue;
                            pointer[(xx * 3) + yy * stride + 1] = frame[xx][yy].Green;
                            pointer[(xx * 3) + yy * stride + 2] = frame[xx][yy].Red;
                        }
                    }
                }
                buffer.UnlockBits(bmData);

                if (_bufferToggle)
                {
                    _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(_bitmapTwo.GetHbitmap(), IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                else
                {
                    _screen.Source = Imaging.CreateBitmapSourceFromHBitmap(_bitmapOne.GetHbitmap(), IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }

                _bufferToggle = !_bufferToggle;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));

                double nextPosX = _posX;
                double nextPosY = _posY;
                if (_upPressed)
                {
                    nextPosX = nextPosX + (_dirX * _moveSpeed);
                    nextPosY = nextPosY + (_dirY * _moveSpeed);
                }
                if (_downPressed)
                {
                    nextPosX = nextPosX - (_dirX * _moveSpeed);
                    nextPosY = nextPosY - (_dirY * _moveSpeed);
                }
                if (_rightPressed)
                {
                    nextPosX = nextPosX + (_planeX * _moveSpeed);
                    nextPosY = nextPosY + (_planeY * _moveSpeed);
                }
                if (_leftPressed)
                {
                    nextPosX = nextPosX - (_planeX * _moveSpeed);
                    nextPosY = nextPosY - (_planeY * _moveSpeed);
                }

                if (Walkable(nextPosX, nextPosY))
                {
                    _posX = nextPosX;
                    _posY = nextPosY;
                }

                int turnConst = 4;
                if (_newMouseX < _lastMouseX || _newMouseX <= _lockMouseRect.Left + 100)
                {
                    double oldDirX = _dirX;
                    _dirX = _dirX * Math.Cos(_rotSpeed * turnConst) - _dirY * Math.Sin(_rotSpeed * turnConst);
                    _dirY = oldDirX * Math.Sin(_rotSpeed * turnConst) + _dirY * Math.Cos(_rotSpeed * turnConst);
                    double oldPlaneX = _planeX;
                    _planeX = _planeX * Math.Cos(_rotSpeed * turnConst) - _planeY * Math.Sin(_rotSpeed * turnConst);
                    _planeY = oldPlaneX * Math.Sin(_rotSpeed * turnConst) + _planeY * Math.Cos(_rotSpeed * turnConst);
                    _lastMouseX = _newMouseX;
                } else if (_newMouseX > _lastMouseX || _newMouseX >= (_lockMouseRect.Right - 100))
                {
                    double oldDirX = _dirX;
                    _dirX = _dirX * Math.Cos((0 - _rotSpeed) * turnConst) - _dirY * Math.Sin((0 - _rotSpeed) * turnConst);
                    _dirY = oldDirX * Math.Sin((0 - _rotSpeed) * turnConst) + _dirY * Math.Cos((0 - _rotSpeed) * turnConst);
                    double oldPlaneX = _planeX;
                    _planeX = _planeX * Math.Cos((0 - _rotSpeed) * turnConst) - _planeY * Math.Sin((0 - _rotSpeed) * turnConst);
                    _planeY = oldPlaneX * Math.Sin((0 - _rotSpeed) * turnConst) + _planeY * Math.Cos((0 - _rotSpeed) * turnConst);
                    _lastMouseX = _newMouseX;
                }

            } while (_running);

            Application.Current.Shutdown();


        }

        private bool Walkable(double xx, double yy)
        {

            double wallHitDist = 0.1;

            if (_world[Convert.ToInt32(Math.Floor(xx + wallHitDist))][Convert.ToInt32(Math.Floor(yy + wallHitDist))] != 0)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx - wallHitDist))][Convert.ToInt32(Math.Floor(yy - wallHitDist))] != 0)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx + wallHitDist))][Convert.ToInt32(Math.Floor(yy - wallHitDist))] != 0)
            {
                return false;
            }
            else if (_world[Convert.ToInt32(Math.Floor(xx - wallHitDist))][Convert.ToInt32(Math.Floor(yy + wallHitDist))] != 0)
            {
                return false;
            }
            return true;
        }


    }
}