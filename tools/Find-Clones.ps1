param(
    [Parameter(Mandatory = $true)][string]$Src,
    [int]$Window = 6
)

$ErrorActionPreference = 'Stop'
$files = Get-ChildItem -LiteralPath $Src -Filter *.cs -File | Sort-Object Name

$windows = @{}
foreach ($f in $files) {
    $lines = [IO.File]::ReadAllLines($f.FullName, [Text.Encoding]::UTF8)
    $code = New-Object System.Collections.ArrayList
    $nums = New-Object System.Collections.ArrayList
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $t = ($lines[$i] -replace '//.*$', '').Trim()
        if ($t -eq '' -or $t -eq '{' -or $t -eq '}' -or $t -eq '};') { continue }
        $t = ($t -replace '\s+', ' ')
        [void]$code.Add($t)
        [void]$nums.Add($i + 1)
    }
    for ($j = 0; $j + $Window -le $code.Count; $j++) {
        $key = ($code[$j..($j + $Window - 1)] -join "`n")
        if (-not $windows.ContainsKey($key)) {
            $windows[$key] = New-Object System.Collections.ArrayList
        }
        [void]$windows[$key].Add(('{0}:{1}' -f $f.Name, $nums[$j]))
    }
}

$found = 0
foreach ($k in $windows.Keys) {
    if ($windows[$k].Count -gt 1) {
        $found++
        Write-Output '--- clone group ---'
        Write-Output ('locations: ' + ($windows[$k] -join ', '))
        Write-Output $k
    }
}
Write-Output ('clones=' + $found)
