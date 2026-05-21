# Generates installer/assets/NexaRun.ico (PNG entries — valid for Inno Setup + Windows)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class NexaRunIcoWriter
{
    public static void SavePngIcon(string path, IList<Bitmap> images)
    {
        var pngs = new List<byte[]>(images.Count);
        foreach (var img in images)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                pngs.Add(ms.ToArray());
            }
        }

        using (var fs = File.Create(path))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)pngs.Count);

            int offset = 6 + (16 * pngs.Count);
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i];
                int w = img.Width;
                int h = img.Height;
                bw.Write((byte)(w >= 256 ? 0 : w));
                bw.Write((byte)(h >= 256 ? 0 : h));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(pngs[i].Length);
                bw.Write(offset);
                offset += pngs[i].Length;
            }

            foreach (var png in pngs)
                bw.Write(png);
        }
    }
}
"@

function New-NexaRunBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(255, 30, 30, 30))

    $green = [System.Drawing.Color]::FromArgb(255, 0, 220, 110)
    $pen = New-Object System.Drawing.Pen $green, ([Math]::Max(1.0, $size / 21.0))
    $x1 = $size * 0.19; $x2 = $size * 0.41
    $y1 = $size * 0.31; $ym = $size * 0.50; $y2 = $size * 0.69
    $g.DrawLine($pen, $x1, $y1, $x2, $ym)
    $g.DrawLine($pen, $x1, $y2, $x2, $ym)
    $g.DrawLine($pen, ($size * 0.50), ($size * 0.69), ($size * 0.75), ($size * 0.69))
    $g.Dispose()
    $pen.Dispose()
    return $bmp
}

$out = Join-Path $PSScriptRoot "assets\NexaRun.ico"
$dir = Split-Path $out -Parent
if (-not (Test-Path $dir)) { New-Item $dir -ItemType Directory | Out-Null }

$bitmaps = New-Object 'System.Collections.Generic.List[System.Drawing.Bitmap]'
try {
    foreach ($s in @(16, 32, 48, 64, 256)) {
        $bitmaps.Add((New-NexaRunBitmap $s))
    }
    [NexaRunIcoWriter]::SavePngIcon($out, $bitmaps)
}
finally {
    foreach ($bmp in $bitmaps) { $bmp.Dispose() }
}

if ((Get-Item $out).Length -lt 500) { throw "Generated icon looks invalid: $out" }

# Validate Windows can load it
$icon = [System.Drawing.Icon]::new($out)
$icon.Dispose()

Write-Host "Wrote $out ($((Get-Item $out).Length) bytes)"
