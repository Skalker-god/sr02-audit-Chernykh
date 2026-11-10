param(
    [Parameter(Mandatory = $true)][string]$Metrics,
    [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$rows = Import-Csv -LiteralPath $Metrics -Delimiter ';'
$picked = @(
    'PickupDesk.Handout',
    'StorageService.RegisterArrival',
    'OrderStore.FindByPhone',
    'StorageService.ChangeStatus',
    'StorageService.AssignCell',
    'ShiftReport.CloseShift',
    'StorageService.do_sweep',
    'ShiftReport.PrintShelf',
    'PickupDesk.RefuseItem',
    'OrderStore.FindByCode',
    'Program.Arrive',
    'DeskJournal.Write',
    'StorageService.PlaceOnShelf',
    'OrderStore.FindByNumber',
    'PickupDesk.OldFee'
)

$data = @()
foreach ($key in $picked) {
    $parts = $key.Split('.')
    $r = $rows | Where-Object { $_.Class -eq $parts[0] -and $_.Method -eq $parts[1] } | Select-Object -First 1
    if ($null -eq $r) { throw "Not found in metrics: $key" }
    $data += [pscustomobject]@{ Label = $key; CC = [int]$r.CC }
}

$w = 1800; $h = 1000
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::White)

$left = 150; $right = 1720; $top = 80; $bottom = 690
$maxY = 24
$plotW = $right - $left
$plotH = $bottom - $top

function Y($v) { return $bottom - ($v / $maxY) * $plotH }
function X($i) { return $left + ($i + 0.5) * ($plotW / $data.Count) }

$fontAxis = New-Object System.Drawing.Font('Segoe UI', 18)
$fontTick = New-Object System.Drawing.Font('Segoe UI', 14)
$fontLabel = New-Object System.Drawing.Font('Segoe UI', 13)
$fontValue = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
$fontLegend = New-Object System.Drawing.Font('Segoe UI', 15)
$brushText = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40,40,40))
$brushGrid = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225,225,225), 1)
$brushBar = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(52,120,180))
$penThreshold = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200,45,45), 3)
$penThreshold.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

for ($v = 0; $v -le $maxY; $v += 4) {
    $y = Y $v
    $g.DrawLine($brushGrid, $left, $y, $right, $y)
    $sz = $g.MeasureString($v.ToString(), $fontTick)
    $g.DrawString($v.ToString(), $fontTick, $brushText, $left - 12 - $sz.Width, $y - $sz.Height / 2)
}

$g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90,90,90), 2)), $left, $bottom, $right, $bottom)
$g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90,90,90), 2)), $left, $top, $left, $bottom)

$barW = [Math]::Min(72, ($plotW / $data.Count) * 0.62)
for ($i = 0; $i -lt $data.Count; $i++) {
    $x = X $i
    $y = Y $data[$i].CC
    $rect = New-Object System.Drawing.RectangleF(($x - $barW / 2), $y, $barW, ($bottom - $y))
    $g.FillRectangle($brushBar, $rect)
    $label = $data[$i].CC.ToString()
    $sz = $g.MeasureString($label, $fontValue)
    $g.DrawString($label, $fontValue, $brushText, $x - $sz.Width / 2, $y - $sz.Height - 4)

    $state = $g.Save()
    $g.TranslateTransform($x, $bottom + 10)
    $g.RotateTransform(90)
    $g.DrawString($data[$i].Label, $fontLabel, $brushText, 0, -8)
    $g.Restore($state)
}

$y8 = Y 8
$g.DrawLine($penThreshold, $left, $y8, $right, $y8)

$g.DrawString('CC (цикломатична складність)', $fontAxis, $brushText, 10, 40)

$lx = $right - 360; $ly = 18
$g.DrawLine($penThreshold, $lx, $ly + 14, $lx + 60, $ly + 14)
$g.DrawString('Поріг CC = 8', $fontLegend, $brushText, $lx + 70, $ly)

$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output $Out
