param(
    [Parameter(Mandatory = $true)][string]$Src,
    [string]$Out = ''
)

$ErrorActionPreference = 'Stop'
$files = Get-ChildItem -LiteralPath $Src -Filter *.cs -File | Sort-Object Name
$allTypes = @()
foreach ($f in $files) {
    $text = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($text, '(?m)^\s*(?:public|internal)?\s*(?:static\s+)?(?:class|enum)\s+(\w+)')) {
        $allTypes += $m.Groups[1].Value
    }
}
$allTypes = $allTypes | Sort-Object -Unique

function Get-ClassBlocks([string]$text) {
    $lines = $text -split "`r?`n"
    $blocks = New-Object System.Collections.ArrayList
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*(?:public|internal)?\s*(static\s+)?(class|enum)\s+(\w+)') {
            $kind = $Matches[2]
            $name = $Matches[3]
            $j = $i
            while ($j -lt $lines.Count -and $lines[$j] -notmatch '\{') { $j++ }
            if ($j -ge $lines.Count) { continue }
            $depth = 0
            $end = $j
            for ($k = $j; $k -lt $lines.Count; $k++) {
                $clean = ($lines[$k] -replace '//.*$', '')
                $depth += ([regex]::Matches($clean, '\{')).Count
                $depth -= ([regex]::Matches($clean, '\}')).Count
                if ($depth -le 0) { $end = $k; break }
            }
            [void]$blocks.Add([pscustomobject]@{ Name = $name; Kind = $kind; Start = $j; End = $end; Lines = $lines })
            $i = $end
        }
    }
    return $blocks
}

function Get-LogicalCount([string[]]$bodyLines) {
    $n = 0
    foreach ($raw in $bodyLines) {
        $t = ($raw -replace '//.*$', '').Trim()
        if ($t -eq '' -or $t -eq '{' -or $t -eq '}' -or $t -eq '};') { continue }
        $n++
    }
    return $n
}

function Get-Cc([string]$body) {
    $b = $body -replace '//.*$', ''
    $cc = 1
    $cc += ([regex]::Matches($b, '\b(if|for|foreach|while|case|catch)\b')).Count
    $cc += ([regex]::Matches($b, '&&')).Count
    $cc += ([regex]::Matches($b, '\|\|')).Count
    $cc += ([regex]::Matches($b, '\?\?')).Count
    $cc += ([regex]::Matches($b, '\?(?![?.])')).Count
    return $cc
}

function Get-Nesting([string[]]$bodyLines) {
    $depth = 1
    $max = 1
    foreach ($raw in $bodyLines) {
        $t = ($raw -replace '//.*$', '')
        $t = ($t -replace '"(?:[^"\\]|\\.)*"', '""')
        foreach ($ch in $t.ToCharArray()) {
            if ($ch -eq '{') { $depth++; if ($depth -gt $max) { $max = $depth } }
            elseif ($ch -eq '}') { $depth-- }
        }
    }
    return $max
}

function Get-Methods([string]$file, [string]$text) {
    $rows = New-Object System.Collections.ArrayList
    $blocks = Get-ClassBlocks $text
    foreach ($blk in $blocks) {
        if ($blk.Kind -ne 'class') { continue }
        $lines = $blk.Lines
        $i = $blk.Start + 1
        $depth = 0
        while ($i -lt $blk.End) {
            $line = $lines[$i]
            $clean = ($line -replace '//.*$', '')
            if ($depth -eq 0 -and $clean -match '\(' -and $clean -notmatch '(?<![=])=(?![=>])' -and $clean -notmatch '^\s*(if|for|foreach|while|switch|return|new|lock|using)\b') {
                $sig = $clean
                $sigStart = $i
                while ($sig -notmatch '\)' -and $i + 1 -lt $blk.End) {
                    $i++
                    $sig += ' ' + ($lines[$i] -replace '//.*$', '').Trim()
                }
                $open = $sig.IndexOf('(')
                $head = $sig.Substring(0, $open)
                $nameMatch = [regex]::Matches($head, '\w+')
                if ($nameMatch.Count -eq 0) { $i++; continue }
                $name = $nameMatch[$nameMatch.Count - 1].Value
                if ($name -in @('if','for','foreach','while','switch','return','new','lock','using','catch')) { $i++; continue }
                $paramText = ''
                $close = $sig.IndexOf(')', $open)
                if ($open -ge 0 -and $close -gt $open) { $paramText = $sig.Substring($open + 1, $close - $open - 1) }
                $params = 0
                if ($paramText.Trim() -ne '') {
                    $params = ($paramText -split ',').Count
                }

                if ($sig -match '=>') {
                    $expr = $sig.Substring($sig.IndexOf('=>') + 2)
                    while ($expr -notmatch ';' -and $i + 1 -lt $blk.End) {
                        $i++
                        $expr += ' ' + ($lines[$i] -replace '//.*$', '').Trim()
                    }
                    [void]$rows.Add([pscustomobject]@{
                        Class = $blk.Name; Method = $name; LOC = 1; CC = (Get-Cc $expr)
                        Nesting = 1; Params = $params; StartLine = $sigStart + 1
                    })
                    $i++
                    continue
                }

                $braceLine = $i
                while ($braceLine -lt $blk.End -and $lines[$braceLine] -notmatch '\{') { $braceLine++ }
                if ($braceLine -ge $blk.End) { $i++; continue }
                $body = New-Object System.Collections.ArrayList
                $first = $lines[$braceLine]
                $afterBrace = $first.Substring($first.IndexOf('{') + 1)
                if ($afterBrace.Trim() -ne '') { [void]$body.Add($afterBrace) }
                $j = $braceLine + 1
                $d = 1
                $d += ([regex]::Matches(($afterBrace -replace '//.*$', ''), '\{')).Count
                $d -= ([regex]::Matches(($afterBrace -replace '//.*$', ''), '\}')).Count
                if ($d -le 0) {
                    $bodyText = ($body -join "`n")
                    [void]$rows.Add([pscustomobject]@{
                        Class = $blk.Name; Method = $name; LOC = (Get-LogicalCount $body.ToArray()); CC = (Get-Cc $bodyText)
                        Nesting = (Get-Nesting $body.ToArray()); Params = $params; StartLine = $sigStart + 1
                    })
                    $i = $braceLine + 1
                    continue
                }
                while ($j -lt $blk.End) {
                    $l = $lines[$j]
                    $lc = ($l -replace '//.*$', '')
                    $opens = ([regex]::Matches($lc, '\{')).Count
                    $closes = ([regex]::Matches($lc, '\}')).Count
                    if ($d + $opens - $closes -le 0) {
                        $before = $l.Substring(0, $l.LastIndexOf('}'))
                        if ($before.Trim() -ne '') { [void]$body.Add($before) }
                        break
                    }
                    [void]$body.Add($l)
                    $d += $opens - $closes
                    $j++
                }
                $bodyText = ($body -join "`n")
                [void]$rows.Add([pscustomobject]@{
                    Class = $blk.Name; Method = $name; LOC = (Get-LogicalCount $body.ToArray()); CC = (Get-Cc $bodyText)
                    Nesting = (Get-Nesting $body.ToArray()); Params = $params; StartLine = $sigStart + 1
                })
                $i = $j + 1
                continue
            }

            $lc = ($line -replace '//.*$', '')
            $depth += ([regex]::Matches($lc, '\{')).Count
            $depth -= ([regex]::Matches($lc, '\}')).Count
            $i++
        }
    }
    return $rows
}

$rows = New-Object System.Collections.ArrayList
foreach ($f in $files) {
    $text = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
    $ms = Get-Methods $f.Name $text
    foreach ($m in $ms) {
        $bodyText = ''
        # Fan-out is measured per class (all project types mentioned in the class body).
        $classes = Get-ClassBlocks $text
        $blk = $classes | Where-Object { $_.Name -eq $m.Class } | Select-Object -First 1
        if ($null -ne $blk) {
            $classText = ($blk.Lines[$blk.Start..$blk.End] -join "`n")
            $used = @()
            foreach ($t in $allTypes) {
                if ($t -eq $m.Class) { continue }
                if ($classText -match ('\b' + [regex]::Escape($t) + '\b')) { $used += $t }
            }
            $bodyText = $used.Count
        }
        [void]$rows.Add([pscustomobject]@{
            Class = $m.Class; Method = $m.Method; LOC = $m.LOC; CC = $m.CC
            Nesting = $m.Nesting; Params = $m.Params; FanOut = $bodyText
        })
    }
}

$csv = @('Class;Method;LOC;CC;Nesting;Params;FanOut')
foreach ($r in $rows) {
    $csv += ('{0};{1};{2};{3};{4};{5};{6}' -f $r.Class, $r.Method, $r.LOC, $r.CC, $r.Nesting, $r.Params, $r.FanOut)
}
$content = ($csv -join "`r`n") + "`r`n"
if ($Out -ne '') {
    [IO.File]::WriteAllText($Out, $content, (New-Object Text.UTF8Encoding $false))
} else {
    $content
}
