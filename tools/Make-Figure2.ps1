param(
    [Parameter(Mandatory = $true)][string]$Before,
    [Parameter(Mandatory = $true)][string]$After,
    [Parameter(Mandatory = $true)][string]$Out
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$b = Import-Csv -LiteralPath $Before -Delimiter ';'
$a = Import-Csv -LiteralPath $After -Delimiter ';'

# Ті самі 15 методів, що й на рисунку 1. Після R02 do_sweep перейменовано.
$pairs = @(
    @{ Label = 'PickupDesk.Handout';            After = 'PickupDesk.Handout' },
    @{ Label = 'StorageService.RegisterArrival'; After = 'StorageService.RegisterArrival' },
    @{ Label = 'OrderStore.FindByPhone';         After = 'OrderStore.FindByPhone' },
    @{ Label = 'StorageService.ChangeStatus';    After = 'StorageService.ChangeStatus' },
    @{ Label = 'StorageService.AssignCell';      After = 'StorageService.AssignCell' },
    @{ Label = 'ShiftReport.CloseShift';         After = 'ShiftReport.CloseShift' },
    @{ Label = 'StorageService.do_sweep';        After = 'StorageService.SweepExpired' },
    @{ Label = 'ShiftReport.PrintShelf';         After = 'ShiftReport.PrintShelf' },
    @{ Label = 'PickupDesk.RefuseItem';          After = 'PickupDesk.RefuseItem' },
    @{ Label = 'OrderStore.FindByCode';          After = 'OrderStore.FindByCode' },
    @{ Label = 'Program.Arrive';                 After = 'Program.Arrive' },
    @{ Label = 'DeskJournal.Write';              After = 'DeskJournal.Write' },
    @{ Label = 'StorageService.PlaceOnShelf';    After = 'StorageService.PlaceOnShelf' },
    @{ Label = 'OrderStore.FindByNumber';        After = 'OrderStore.FindByNumber' },
    @{ Label = 'PickupDesk.OldFee';              After = 'PickupDesk.OldFee' }
)

function Find-Cc($rows, [string]$key) {
    $parts = $key.Split('.')
    $r = $rows | Where-Object { $_.Class -eq $parts[0] -and $_.Method -eq $parts[1] } | Select-Object -First 1
    if ($null -eq $r) { return 0 }
    return [int]$r.CC
}

$data = @()
foreach ($p in $pairs) {
    $data += [pscustomobject]@{
        Label  = $p.Label
        Before = (Find-Cc $b $p.Label)
        After  = (Find-Cc $a $p.After)
    }
}

$w = 1900; $h = 1000
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::White)

$left = 150; $right = 1830; $top = 90; $bottom = 700
$maxY = 24
$plotW = $right - $left
$plotH = $bottom - $top

function Y($v) { return $bottom - ($v / $maxY) * $plotH }

$fontAxis = New-Object System.Drawing.Font('Segoe UI', 18)
$fontTick = New-Object System.Drawing.Font('Segoe UI', 14)
$fontLabel = New-Object System.Drawing.Font('Segoe UI', 12)
$fontValue = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
$fontLegend = New-Object System.Drawing.Font('Segoe UI', 15)
$brushText = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40,40,40))
$penGrid = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225,225,225), 1)
$penAxis = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90,90,90), 2)
$brushBefore = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(120,140,165))
$brushAfter = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70,150,100))
$penThreshold = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200,45,45), 3)
$penThreshold.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

for ($v = 0; $v -le $maxY; $v += 4) {
    $y = Y $v
    $g.DrawLine($penGrid, $left, $y, $right, $y)
    $sz = $g.MeasureString($v.ToString(), $fontTick)
    $g.DrawString($v.ToString(), $fontTick, $brushText, $left - 12 - $sz.Width, $y - $sz.Height / 2)
}
$g.DrawLine($penAxis, $left, $bottom, $right, $bottom)
$g.DrawLine($penAxis, $left, $top, $left, $bottom)

$groupW = $plotW / $data.Count
$barW = [Math]::Min(34, $groupW * 0.34)
for ($i = 0; $i -lt $data.Count; $i++) {
    $cx = $left + ($i + 0.5) * $groupW
    $items = @(
        @{ Brush = $brushBefore; Value = $data[$i].Before; X = $cx - $barW - 2 },
        @{ Brush = $brushAfter;  Value = $data[$i].After;  X = $cx + 2 }
    )
    foreach ($it in $items) {
        if ($it.Value -le 0) { continue }
        $y = Y $it.Value
        $rect = New-Object System.Drawing.RectangleF($it.X, $y, $barW, ($bottom - $y))
        $g.FillRectangle($it.Brush, $rect)
        $label = $it.Value.ToString()
        $sz = $g.MeasureString($label, $fontValue)
        $g.DrawString($label, $fontValue, $brushText, $it.X + $barW / 2 - $sz.Width / 2, $y - $sz.Height - 2)
    }
    if ($data[$i].After -le 0) {
        $g.DrawString('0', $fontValue, $brushText, $cx + 2, $bottom - 22)
    }

    $state = $g.Save()
    $g.TranslateTransform($cx, $bottom + 10)
    $g.RotateTransform(90)
    $g.DrawString($data[$i].Label, $fontLabel, $brushText, 0, -7)
    $g.Restore($state)
}

$y8 = Y 8
$g.DrawLine($penThreshold, $left, $y8, $right, $y8)

$g.DrawString('CC (цикломатична складність)', $fontAxis, $brushText, 10, 45)

$lx = $right - 560; $ly = 20
$s1 = $g.MeasureString('До', $fontLegend)
$g.FillRectangle($brushBefore, $lx, $ly + 8, 26, 18)
$g.DrawString('До', $fontLegend, $brushText, $lx + 34, $ly)
$lx2 = $lx + 130
$g.FillRectangle($brushAfter, $lx2, $ly + 8, 26, 18)
$g.DrawString('Після', $fontLegend, $brushText, $lx2 + 34, $ly)
$lx3 = $lx2 + 170
$g.DrawLine($penThreshold, $lx3, $ly + 16, $lx3 + 55, $ly + 16)
$g.DrawString('Поріг CC = 8', $fontLegend, $brushText, $lx3 + 65, $ly)

$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output $Out
