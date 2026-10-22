<#
  Md3 demo UI helper for native Windows.

  Runs from the repository root (or pass -RepoRoot). Designed to be called from WSL:

    pwsh.exe -File tools\win\md3ui.ps1 -Action start
    pwsh.exe -File tools\win\md3ui.ps1 -Action start -EnvWindow 400x800 -EnvSafeArea 24,48 -EnvKeyboard 300
    pwsh.exe -File tools\win\md3ui.ps1 -Action shot  -Out docs\handoff\win-home.png
    pwsh.exe -File tools\win\md3ui.ps1 -Action click -X 120 -Y 240
    pwsh.exe -File tools\win\md3ui.ps1 -Action key   -Keys "{ESC}"
    pwsh.exe -File tools\win\md3ui.ps1 -Action stop

  The script is stateless: every call locates the demo window by title.
#>
[CmdletBinding()]
param(
    [ValidateSet('start', 'stop', 'shot', 'click', 'key', 'info', 'wheel')]
    [string]$Action = 'info',

    [string]$RepoRoot = '',
    [string]$Exe = 'samples\Demo\bin\Debug\net10.0\Md3.Demo.exe',
    [string]$Title = 'Md3 Navigation Demo',
    [string]$Out = '',

    # Window-relative, physical pixels.
    [int]$X = 0,
    [int]$Y = 0,
    [string]$Keys = '',
    [int]$WaitMs = 1500,
    [switch]$NoFocus,
    # Positive scrolls up/away, negative scrolls down.
    [int]$Delta = -120,
    [string]$EnvSafeArea = '',
    [string]$EnvKeyboard = '',
    [string]$EnvWindow = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class Md3Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);

    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    public static void EnableDpiAwareness()
    {
        try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
        try { SetProcessDPIAware(); } catch { }
    }

    public static void ClickLeft()
    {
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }

    public static void Wheel(int delta)
    {
        mouse_event(0x0800, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
        System.Threading.Thread.Sleep(120);
    }
}
"@

[Md3Win32]::EnableDpiAwareness() | Out-Null

function Get-RepoRoot {
    if ($RepoRoot -ne '') { return (Resolve-Path $RepoRoot).Path }
    return (Get-Location).Path
}

function Get-DemoWindow {
    $p = Get-Process -Name ([System.IO.Path]::GetFileNameWithoutExtension($Exe)) -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -like "*$Title*" } |
        Select-Object -First 1
    if (-not $p) { throw "Demo window '$Title' was not found. Start it with -Action start." }
    return $p
}

function Get-VisibleRect([IntPtr]$hWnd) {
    $rect = New-Object Md3Win32+RECT
    $hr = [Md3Win32]::DwmGetWindowAttribute($hWnd, [Md3Win32]::DWMWA_EXTENDED_FRAME_BOUNDS, [ref]$rect, 16)
    if ($hr -ne 0 -or ($rect.Right - $rect.Left) -le 0) {
        [Md3Win32]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    }
    return $rect
}

function Save-WindowShot([IntPtr]$hWnd, [string]$path) {
    $rect = Get-VisibleRect $hWnd
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) { throw "Window has no visible area." }

    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $width, $height))
    $g.Dispose()

    $dir = Split-Path -Parent $path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)

    # Report whether the capture is almost certainly a black/empty frame.
    $samples = 0; $sum = 0
    for ($x = 0; $x -lt $width; $x += [Math]::Max(1, [int]($width / 24))) {
        for ($y = 0; $y -lt $height; $y += [Math]::Max(1, [int]($height / 16))) {
            $c = $bmp.GetPixel($x, $y); $sum += $c.R + $c.G + $c.B; $samples++
        }
    }
    $bmp.Dispose()
    $avg = if ($samples -gt 0) { [Math]::Round($sum / ($samples * 3), 1) } else { 0 }
    return [pscustomobject]@{ Width = $width; Height = $height; AverageBrightness = $avg }
}

$root = Get-RepoRoot

switch ($Action) {
    'start' {
        Get-Process -Name 'Md3.Demo' -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Milliseconds 300
        $full = Join-Path $root $Exe
        if (-not (Test-Path $full)) { throw "Executable not found: $full. Build it first." }
        $env:MD3_SMOKE = $null
        if ($EnvSafeArea -ne '') { $env:MD3_SAFE_AREA = $EnvSafeArea } else { $env:MD3_SAFE_AREA = $null }
        if ($EnvKeyboard -ne '') { $env:MD3_KEYBOARD = $EnvKeyboard } else { $env:MD3_KEYBOARD = $null }
        if ($EnvWindow -ne '') { $env:MD3_WINDOW = $EnvWindow } else { $env:MD3_WINDOW = $null }
        $proc = Start-Process -FilePath $full -WorkingDirectory $root -PassThru
        Start-Sleep -Milliseconds $WaitMs
        $proc.Refresh()
        Write-Output "started pid=$($proc.Id) title='$($proc.MainWindowTitle)'"
    }
    'stop' {
        $procs = Get-Process -Name 'Md3.Demo' -ErrorAction SilentlyContinue
        if ($procs) { $procs | Stop-Process -Force; Write-Output "stopped $($procs.Count) process(es)" }
        else { Write-Output 'no demo process' }
    }
    'shot' {
        $proc = Get-DemoWindow
        $hWnd = $proc.MainWindowHandle
        if (-not $NoFocus) {
            [Md3Win32]::SetForegroundWindow($hWnd) | Out-Null
            Start-Sleep -Milliseconds 150
        }
        $path = if ($Out -ne '') { if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $root $Out } }
                else { Join-Path $root 'docs\handoff\win-shot.png' }
        $info = Save-WindowShot $hWnd $path
        Write-Output ("shot {0} {1}x{2} avg={3}" -f $path, $info.Width, $info.Height, $info.AverageBrightness)
    }
    'click' {
        $proc = Get-DemoWindow
        $hWnd = $proc.MainWindowHandle
        [Md3Win32]::SetForegroundWindow($hWnd) | Out-Null
        Start-Sleep -Milliseconds 200
        $rect = Get-VisibleRect $hWnd
        $px = $rect.Left + $X
        $py = $rect.Top + $Y
        [Md3Win32]::SetCursorPos($px, $py) | Out-Null
        Start-Sleep -Milliseconds 80
        [Md3Win32]::ClickLeft()
        Start-Sleep -Milliseconds 350
        Write-Output "clicked window($X,$Y) -> screen($px,$py)"
    }
    'key' {
        $proc = Get-DemoWindow
        [Md3Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 200
        $shell = New-Object -ComObject WScript.Shell
        $shell.SendKeys($Keys)
        Start-Sleep -Milliseconds 300
        Write-Output "sent keys $Keys"
    }
    'wheel' {
        $proc = Get-DemoWindow
        $hWnd = $proc.MainWindowHandle
        [Md3Win32]::SetForegroundWindow($hWnd) | Out-Null
        Start-Sleep -Milliseconds 200
        $rect = Get-VisibleRect $hWnd
        $px = $rect.Left + $X
        $py = $rect.Top + $Y
        [Md3Win32]::SetCursorPos($px, $py) | Out-Null
        Start-Sleep -Milliseconds 80
        [Md3Win32]::Wheel($Delta)
        Write-Output "wheel $Delta at window($X,$Y) -> screen($px,$py)"
    }
    'info' {
        $proc = Get-DemoWindow
        $hWnd = $proc.MainWindowHandle
        $rect = Get-VisibleRect $hWnd
        $client = New-Object Md3Win32+RECT
        [Md3Win32]::GetClientRect($hWnd, [ref]$client) | Out-Null
        $origin = New-Object Md3Win32+POINT
        [Md3Win32]::ClientToScreen($hWnd, [ref]$origin) | Out-Null
        Write-Output ("pid={0} hwnd={1}" -f $proc.Id, $hWnd)
        Write-Output ("window rect: {0},{1} {2}x{3}" -f $rect.Left, $rect.Top, ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top))
        Write-Output ("client rect: {0}x{1} at {2},{3}" -f $client.Right, $client.Bottom, $origin.X, $origin.Y)
        Write-Output ("dpi={0} scale={1:0.##}" -f [Md3Win32]::GetDpiForWindow($hWnd), ([Md3Win32]::GetDpiForWindow($hWnd) / 96.0))
    }
}
