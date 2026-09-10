param([string]$OutputPath = "$PSScriptRoot/../src/DataMonitor.App/Assets/DataMonitor.ico")
Add-Type -AssemblyName System.Drawing
$images = @()
foreach ($size in @(16,32,48,64,128,256)) {
    $bitmap = [System.Drawing.Bitmap]::new($size,$size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.Clear([System.Drawing.Color]::FromArgb(21,24,33))
    $blue = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(59,130,246), [single]($size/14))
    $teal = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(20,184,166), [single]($size/18))
    $graphics.DrawRectangle($blue,[single]($size*.16),[single]($size*.18),[single]($size*.68),[single]($size*.55))
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($size*.22,$size*.48),[System.Drawing.PointF]::new($size*.36,$size*.48),
        [System.Drawing.PointF]::new($size*.45,$size*.31),[System.Drawing.PointF]::new($size*.56,$size*.61),
        [System.Drawing.PointF]::new($size*.65,$size*.46),[System.Drawing.PointF]::new($size*.78,$size*.46))
    $graphics.DrawLines($teal,$points)
    $graphics.DrawLine($blue,[single]($size*.36),[single]($size*.84),[single]($size*.64),[single]($size*.84))
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,$stream.ToArray()
    $stream.Dispose(); $blue.Dispose(); $teal.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$file = [System.IO.File]::Create([System.IO.Path]::GetFullPath($OutputPath))
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
$i = 0
foreach ($size in @(16,32,48,64,128,256)) {
    $dimension = if ($size -eq 256) { 0 } else { $size }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
    $offset += $images[$i].Length; $i++
}
foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
$writer.Dispose()
