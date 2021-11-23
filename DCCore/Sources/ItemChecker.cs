using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using D2Tools.Helpers;
using D2Tools.Structs;
using D2Tools.Types;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using Point = D2Tools.Types.Point;

public class ItemChecker : IDisposable
{
    public event Action<GameData, long> OnGameData;

    private GameData _gameData;
    private Settings _settings;
    
    private readonly List<ItemD2> _filteredItems = new List<ItemD2>();
    private readonly Dictionary<uint, int> _blinkUID = new  Dictionary<uint, int>();
    
    private bool _gettingGameData;

    private StickyWindow _overlayWindow;
    
    private readonly Dictionary<D2ItemRarity, SolidBrush> _brushes = new Dictionary<D2ItemRarity, SolidBrush>();
    private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();

    private string _statusText = "";
    private SolidBrush _statusBrush;

    private long _filterTime;
    private long _readTime;
    
    public ItemChecker()
    {
        GameManager.OnLog += (text, ok) =>
        {
            string area = _gameData?.CurrentArea == Area.None ? "Paused" : _gameData?.CurrentArea.Name();
            
            SetStatusText(ok? D2ItemRarity.SET : D2ItemRarity.RARE, ok? area : "Waiting for map...");
        };
        
        GameManager.OnGameProcessExit += () =>
        {
            Console.WriteLine("D2R process finished");
            Console.WriteLine("--------------------");
            
            GameManager.Run = false;
        };
        
        GameData.OnAreaChanged += area =>
        {
            if(area != Area.None && _blinkUID.Count > 0)
                _blinkUID.Clear();
        };
        
        OnGameData += (gameData, time) =>
        {
            _readTime = time;
            
            ApplyFilters(gameData);
        };
    }
    
    ~ItemChecker()
    {
        Dispose(false);
    }

    private const string WaitingD2R = "Waiting for D2R...";
    private const string FoundD2R = "D2R process found!";

    public async void Run()
    {
        _settings = new Settings();
        
        SetStatusText(D2ItemRarity.RARE, WaitingD2R); Console.WriteLine(WaitingD2R);
        
        await GameManager.WaitForGame();
        
        SetStatusText(D2ItemRarity.SET, FoundD2R); Console.WriteLine(FoundD2R);
        
        Console.WriteLine("Running...");

        RunOverlay();

        while (GameManager.Run)
        {
            await Task.Run(TryGetGameData);

            await Task.Delay(_settings.CheckDelayMs);
        }
        
        Console.WriteLine("Close terminal or press [Esc] to quit");
    }

    void ApplyFilters(GameData gameData)
    {
        if(gameData?.ItemUnits == null || _settings?.Filters == null) return;
            
        _filteredItems.Clear();
            
        Stopwatch stopwatch = Stopwatch.StartNew();

        foreach (var itemUnit in gameData.ItemUnits)
        {
            foreach (var filter in _settings.Filters)
            {
                var itemUnitData = itemUnit.ItemD2.ItemUnitData;
                    
                if(filter.Rarity.Count > 0 && !filter.Rarity.Contains(itemUnitData.Rarity)) continue;
                    
                if(filter.CheckEtheral && !itemUnitData.Etheral) continue;

                if (filter.CheckSockets)
                {
                    if(filter.MinSockets > 0 && !itemUnitData.Socketed) continue;
                    if(filter.MaxSockets == 0 && itemUnitData.Socketed) continue;

                    bool socketStatExists =
                        itemUnit.ItemD2.TryGetStat(D2Stat.STAT_ITEM_NUMSOCKETS, out int sockets);
                        
                    if(!socketStatExists) continue;
                        
                    if(sockets < filter.MinSockets || sockets > filter.MaxSockets) continue;
                }
                    
                if(!string.IsNullOrWhiteSpace(filter.Text) && !itemUnit.ItemD2.Name.Contains(filter.Text)) continue;
                    
                _filteredItems.Add(itemUnit.ItemD2);
                break;
            }
        }
            
        stopwatch.Stop();
        _filterTime = stopwatch.ElapsedMilliseconds;
    }

    async void RunOverlay()
    {
        var graphics = new Graphics
        {
            VSync = true
        };

        _overlayWindow = new StickyWindow(GameManager.MainWindowHandle, graphics);
        _overlayWindow.IsTopmost = true;
        
        _overlayWindow.DestroyGraphics += OverlayWindowOnDestroyGraphics;
        _overlayWindow.SetupGraphics += OverlayWindowOnSetupGraphics;
        _overlayWindow.DrawGraphics += OverlayWindowOnDrawGraphics;
        
        _overlayWindow.Create();
        
        Task.Run(async () =>
        {
            while (GameManager.Run)
            {
                // SetWinEventHook not work in console applications....
                if (WindowsExternal.GetForegroundWindow() != GameManager.MainWindowHandle)
                {
                    if(_overlayWindow.IsVisible)
                        _overlayWindow.Hide();
                }
                else
                {
                    if(!_overlayWindow.IsVisible)
                        _overlayWindow.Show();
                }
                
                await Task.Delay(100);
            }
        });
    }

    private void OverlayWindowOnDrawGraphics(object sender, DrawGraphicsEventArgs e)
    {
        var gfx = e.Graphics;
        
        gfx.ClearScene();

        PrintDroppedItems(gfx);
        PrintStatus(gfx);
    }

    private void OverlayWindowOnSetupGraphics(object sender, SetupGraphicsEventArgs e)
    {
        var gfx = e.Graphics;

        if (e.RecreateResources)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
        }

        foreach (var rarityColor in ItemD2.ItemRarityColors)
        {
            var rgb = rarityColor.Value;
            
            _brushes[rarityColor.Key] = gfx.CreateSolidBrush(rgb.R, rgb.G, rgb.B);
        }

        if (e.RecreateResources) return;

        _fonts["default"] = IsFontInstalled("exocetblizzardot-medium.otf") ?
            gfx.CreateFont("Exocet Blizzard Mixed Caps", _settings?.TextFontSize ?? 24) :
            gfx.CreateFont("Consolas", _settings?.TextFontSize ?? 24);
        
        _fonts["status"] = gfx.CreateFont("Consolas", 12);

        // weapping kekw
        _fonts["default"].WordWeapping = false;
        _fonts["status"].WordWeapping = false;
    }

    private void OverlayWindowOnDestroyGraphics(object sender, DestroyGraphicsEventArgs e)
    {
        foreach (var pair in _brushes) pair.Value.Dispose();
        foreach (var pair in _fonts) pair.Value.Dispose();
    }

    void SetStatusText(D2ItemRarity rarity, string text)
    {
        if(_brushes.ContainsKey(rarity))
            _statusBrush = _brushes[rarity];

        string items = "";
        
        if (_gameData?.ItemUnits != null)
            items = ", I:" + _gameData.ItemUnits.Count;

        _statusText = $"[T:{_readTime + _filterTime}ms{items}] " + text;
    }

    void PrintStatus(Graphics graphics)
    {
        if(string.IsNullOrWhiteSpace(_statusText)) return;
        
        graphics.DrawText(
            _fonts["status"],
            _statusBrush ?? _brushes[D2ItemRarity.NORMAL],
            16, _overlayWindow.Height - 28,
            _statusText);
    }

    void PrintDroppedItems(Graphics graphics)
    {
        if(_gameData == null || _filteredItems == null || _settings == null) return;

        for (int i = 0; i < _filteredItems.Count; i++)
        {
            var item = _filteredItems[i];

            var itemUnitData = item.ItemUnitData;

            var rarity = itemUnitData.Rarity;
            
            if (item.IsRune)
                rarity = D2ItemRarity.CRAFTED;
            else if (itemUnitData.Etheral || itemUnitData.Socketed)
                rarity = D2ItemRarity.LOW_QUALITY;

            string nameToDisplay = item.Name;

            if (itemUnitData.Etheral)
                nameToDisplay = "Etheral " + nameToDisplay;
            
            if(itemUnitData.Rarity == D2ItemRarity.HIGH_QUALITY)
                nameToDisplay = "Superior " + nameToDisplay;

            if (itemUnitData.Socketed && item.TryGetStat(D2Stat.STAT_ITEM_NUMSOCKETS, out int sockets))
                nameToDisplay = $"[{sockets}] " + nameToDisplay;

            if (!itemUnitData.New && !item.IsSimple && item.ItemUnitData.IsIdentified)
                nameToDisplay = "(checked) " + nameToDisplay;

            int tx = 16;
            int ty = _overlayWindow.Height - 54 - (i * _settings?.TextLineStep ?? 24);
            
            // shadow
            graphics.DrawText(
                _fonts["default"],
                _brushes[D2ItemRarity.NONE],
                tx + 1, ty + 1,
                nameToDisplay);

            var brush = _brushes[rarity];

            bool blink = false; // no draw text if true

            if (_settings.NewItemsBlink)
            {
                if (itemUnitData.New)
                {
                    if (!_blinkUID.ContainsKey(item.Unit.UID))
                    {
                        _blinkUID[item.Unit.UID] = _settings.BlinkDuration;
                    }
                    else
                    {
                        if (_blinkUID[item.Unit.UID] > 0)
                        {
                            _blinkUID[item.Unit.UID]--;

                            // 400 IQ
                            blink = (int)(_blinkUID[item.Unit.UID] / _settings.BlinkSpeed) % 2 == 0;
                        }
                    }
                }
                else
                {
                    if (_blinkUID.ContainsKey(item.Unit.UID))
                        _blinkUID.Remove(item.Unit.UID);
                }
            }
            
            if (!blink)
            {
                graphics.DrawText(
                    _fonts["default"], 
                    brush,
                    tx, ty,
                    nameToDisplay);
            }
        }
    }

    private void TryGetGameData()
    {
        if (_gettingGameData) return;
        
        Stopwatch stopwatch = Stopwatch.StartNew();

        _gettingGameData = true;

        if (_gameData == null)
        {
            _gameData = GameData.GetNew();
        }
        else
        {
            _gameData = _gameData.Update();

            if (_gameData == null)
            {
                SetStatusText(D2ItemRarity.RARE, "GameData.Update failed");
            }
        }
        
        _gettingGameData = false;
        
        stopwatch.Stop();
        
        if (_gameData != null)
            OnGameData?.Invoke(_gameData, stopwatch.ElapsedMilliseconds);
    }

    // only works with fonts installed 'for all users'
    bool IsFontInstalled(string fontFile)
    {
        return File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontFile));
    }
    
#region IDisposable Support
    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _overlayWindow.Dispose();

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
#endregion
}