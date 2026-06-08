param(
  [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..").Path,
  [string]$SnapshotPath = "",
  [string]$OutDir = "",
  [ValidateSet("full","core")]
  [string]$Mode = "full",
  [ValidateSet("TB","LR")]
  [string]$RankDir = "TB",
  [ValidateSet("auto","manual")]
  [string]$Layout = "auto",
  [string]$DotExePath = ""
)

$ErrorActionPreference = "Stop"

function Resolve-OrThrow([string]$PathLike) {
  $p = Resolve-Path $PathLike -ErrorAction Stop
  return $p.Path
}

if ([string]::IsNullOrWhiteSpace($SnapshotPath)) {
  $SnapshotPath = Join-Path $ProjectRoot "web-ban-thuoc\Migrations\LongChauDbContextModelSnapshot.cs"
}
if ([string]::IsNullOrWhiteSpace($OutDir)) {
  $OutDir = Join-Path $ProjectRoot "docs\ERD"
}

$SnapshotPath = Resolve-OrThrow $SnapshotPath
if (!(Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

$snapshot = Get-Content -LiteralPath $SnapshotPath -Raw

# --- Parse entities + properties from EF Core model snapshot ---
# We keep 12 business entities + IdentityUser (AspNetUsers) only.
$wantedEntityNames =
  if ($Mode -eq "core") {
    @(
      "web_ban_thuoc.Models.Category",
      "web_ban_thuoc.Models.Product",
      "web_ban_thuoc.Models.ProductImage",
      "web_ban_thuoc.Models.Order",
      "web_ban_thuoc.Models.OrderItem",
      "web_ban_thuoc.Models.Payment",
      "web_ban_thuoc.Models.Review",
      "web_ban_thuoc.Models.Voucher",
      "web_ban_thuoc.Models.UserVoucher",
      "Microsoft.AspNetCore.Identity.IdentityUser"
    )
  } else {
    @(
      "web_ban_thuoc.Models.Banner",
      "web_ban_thuoc.Models.Category",
      "web_ban_thuoc.Models.ChatMessage",
      "web_ban_thuoc.Models.InventoryTransaction",
      "web_ban_thuoc.Models.Order",
      "web_ban_thuoc.Models.OrderItem",
      "web_ban_thuoc.Models.Payment",
      "web_ban_thuoc.Models.Product",
      "web_ban_thuoc.Models.ProductImage",
      "web_ban_thuoc.Models.Review",
      "web_ban_thuoc.Models.UserRankInfo",
      "web_ban_thuoc.Models.UserVoucher",
      "web_ban_thuoc.Models.Voucher",
      "Microsoft.AspNetCore.Identity.IdentityUser"
    )
  }

$entityMap = @{} # key: shortName, value: @{ fullName; table; props=@(); pk=@(); fks=@() }

foreach ($full in $wantedEntityNames) {
  $short =
    if ($full -eq "Microsoft.AspNetCore.Identity.IdentityUser") { "Users" }
    else { ($full.Split(".")[-1]) }

  $entityMap[$short] = @{
    fullName = $full
    table = $null
    props = New-Object System.Collections.Generic.List[string]
    pk = New-Object System.Collections.Generic.HashSet[string]
    fks = New-Object System.Collections.Generic.HashSet[string]
  }
}

function Add-IfMissing($list, [string]$value) {
  if (-not $list.Contains($value)) { [void]$list.Add($value) }
}

foreach ($short in $entityMap.Keys) {
  $fullName = $entityMap[$short].fullName

  # Find the block for this entity: modelBuilder.Entity("FullName", b => { ... });
  $pattern = [regex]::Escape("modelBuilder.Entity(`"$fullName`", b =>") + "\s*\{"
  $m = [regex]::Match($snapshot, $pattern)
  if (!$m.Success) { continue }

  $start = $m.Index
  # naive block extraction: go until first occurrence of "});" at same indent is hard;
  # instead, take a slice and stop at next "modelBuilder.Entity(" for safety.
  $slice = $snapshot.Substring($start)
  $next = [regex]::Match($slice, "modelBuilder\.Entity\(""", [System.Text.RegularExpressions.RegexOptions]::None, 1)
  # The match above is wrong; use a search starting after current header.
  $next2 = [regex]::Match($slice.Substring(40), "modelBuilder\.Entity\(""")
  if ($next2.Success) {
    $slice = $slice.Substring(0, 40 + $next2.Index)
  }

  # Table name
  $tbl = [regex]::Match($slice, "b\.ToTable\(""(?<t>[^""]+)""")
  if ($tbl.Success) { $entityMap[$short].table = $tbl.Groups["t"].Value }
  else {
    # Identity tables
    if ($fullName -eq "Microsoft.AspNetCore.Identity.IdentityUser") { $entityMap[$short].table = "AspNetUsers" }
  }

  # Properties
  foreach ($pm in [regex]::Matches($slice, 'b\.Property<[^>]+>\("(?<p>[^"]+)"\)')) {
    Add-IfMissing $entityMap[$short].props $pm.Groups["p"].Value
  }

  # PK
  $pkm = [regex]::Match($slice, 'b\.HasKey\((?<inside>[^)]+)\)')
  if ($pkm.Success) {
    foreach ($col in [regex]::Matches($pkm.Groups["inside"].Value, '"(?<c>[^"]+)"')) {
      [void]$entityMap[$short].pk.Add($col.Groups["c"].Value)
    }
  }
}

# --- Parse FK columns from relationship section (HasForeignKey("X")) ---
foreach ($short in $entityMap.Keys) {
  $fullName = $entityMap[$short].fullName
  if ($fullName -eq "Microsoft.AspNetCore.Identity.IdentityUser") { continue }

  # Relationship blocks appear later:
  # modelBuilder.Entity("web_ban_thuoc.Models.OrderItem", b => { b.HasOne(...).HasForeignKey("OrderId"); ... });
  $relPattern = [regex]::Escape("modelBuilder.Entity(`"$fullName`", b =>") + "\s*\{"
  $rm = [regex]::Match($snapshot, $relPattern)
  if (!$rm.Success) { continue }

  $slice = $snapshot.Substring($rm.Index)
  $next2 = [regex]::Match($slice.Substring(40), "modelBuilder\.Entity\(""")
  if ($next2.Success) { $slice = $slice.Substring(0, 40 + $next2.Index) }

  foreach ($fkm in [regex]::Matches($slice, 'HasForeignKey\("(?<fk>[^"]+)"\)')) {
    [void]$entityMap[$short].fks.Add($fkm.Groups["fk"].Value)
  }
}

# --- Chen relationship (diamonds) list (based on actual model relationships) ---
# Note: We intentionally keep only Users (AspNetUsers) from Identity in the ERD.
$relsAll = @(
  # NOTE: Keep labels ASCII so Windows PowerShell 5.1 parses safely.
  @{ id="R_User_Order"; label="Dat hang"; a="Users"; b="Order"; aCard="1"; bCard="0..N" },
  @{ id="R_Order_Items"; label="Chua dong don"; a="Order"; b="OrderItem"; aCard="1"; bCard="N" },
  @{ id="R_Prod_OrderItems"; label="Duoc mua trong dong"; a="Product"; b="OrderItem"; aCard="1"; bCard="N" },
  @{ id="R_Order_Pay"; label="Thanh toan cho"; a="Order"; b="Payment"; aCard="1"; bCard="0..N" },
  @{ id="R_Cat_Prod"; label="Thuoc ve"; a="Category"; b="Product"; aCard="1"; bCard="N" },
  @{ id="R_Cat_Parent"; label="Phan cap cha-con"; a="Category"; b="Category"; aCard="0..1 (parent)"; bCard="0..N (child)" },
  @{ id="R_Prod_Img"; label="Co anh"; a="Product"; b="ProductImage"; aCard="1"; bCard="N" },
  @{ id="R_User_Review"; label="Viet danh gia"; a="Users"; b="Review"; aCard="1"; bCard="0..N" },
  @{ id="R_Prod_Review"; label="San pham nhan danh gia"; a="Product"; b="Review"; aCard="1"; bCard="0..N" },
  @{ id="R_User_UV"; label="Duoc cap voucher"; a="Users"; b="UserVoucher"; aCard="1"; bCard="0..N" },
  @{ id="R_UV_Vc"; label="Thuoc voucher"; a="UserVoucher"; b="Voucher"; aCard="N"; bCard="1" },
  @{ id="R_User_Rank"; label="Co hang"; a="Users"; b="UserRankInfo"; aCard="1"; bCard="0..1" },
  @{ id="R_User_Send"; label="Gui tin nhan"; a="Users"; b="ChatMessage"; aCard="1"; bCard="0..N" },
  @{ id="R_Prod_Inv"; label="Bien dong ton kho"; a="Product"; b="InventoryTransaction"; aCard="1"; bCard="0..N" }
)

$rels =
  if ($Mode -eq "core") {
    $relsAll | Where-Object { $_.id -notin @("R_User_Rank","R_User_Send","R_Prod_Inv") }
  } else {
    $relsAll
  }

function Escape-Id([string]$s) {
  return ($s -replace '[^A-Za-z0-9_]', '_')
}

function AttrLabel([string]$shortEntity, [string]$prop, $emap) {
  $pk = $emap[$shortEntity].pk.Contains($prop)
  $fk = $emap[$shortEntity].fks.Contains($prop)
  if ($shortEntity -eq "Users" -and $prop -eq "Id") { $pk = $true }

  if ($pk -and $fk) { return "$prop (PK, FK)" }
  if ($pk) { return "$prop (PK)" }
  if ($fk) { return "$prop (FK)" }
  return $prop
}

$baseName = if ($Mode -eq "core") { "chen-erd-core" } else { "chen-erd" }
$dotPath = Join-Path $OutDir "$baseName.dot"
$pngPath = Join-Path $OutDir "$baseName.png"
$svgPath = Join-Path $OutDir "$baseName.svg"

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('digraph ERD {')
[void]$sb.AppendLine("  rankdir=$RankDir;")
[void]$sb.AppendLine('  bgcolor="white";')
[void]$sb.AppendLine('  pad=0.25;')
[void]$sb.AppendLine('  margin=0.15;')
[void]$sb.AppendLine('  outputorder="edgesfirst";')

if ($Layout -eq "manual") {
  # neato respects explicit node positions (pos="x,y!")
  [void]$sb.AppendLine('  graph [layout=neato, overlap=false, splines=true, fontname="Arial"];')
} else {
  [void]$sb.AppendLine('  graph [overlap=false, splines=true, fontname="Arial"];')
  [void]$sb.AppendLine('  concentrate=true;')
  [void]$sb.AppendLine('  nodesep=0.30;')
  [void]$sb.AppendLine('  ranksep=0.55;')
}
[void]$sb.AppendLine('  fontname="Arial"; fontsize=10;')
[void]$sb.AppendLine('  node [fontname="Arial", fontsize=10];')
[void]$sb.AppendLine('  edge [fontname="Arial", fontsize=9];')
[void]$sb.AppendLine('')

[void]$sb.AppendLine('  // Style palette similar to Chen examples')
[void]$sb.AppendLine('  node [style="filled"];')
[void]$sb.AppendLine('  edge [color="#555555"];')
[void]$sb.AppendLine('')

function Emit-Node([string]$id, [string]$shape, [string]$label, [string]$fill, [string]$pos) {
  $safe = $label.Replace('"','\"')
  if ($Layout -eq "manual" -and -not [string]::IsNullOrWhiteSpace($pos)) {
    [void]$sb.AppendLine("  $id [shape=$shape, fillcolor=""$fill"", label=""$safe"", pos=""$pos!"" ];")
  } else {
    [void]$sb.AppendLine("  $id [shape=$shape, fillcolor=""$fill"", label=""$safe"" ];")
  }
}

# Layout coordinates (in points). This keeps the diagram compact even with many attributes.
$entityPos = @{}
if ($Layout -eq "manual") {
  # 4 columns: Catalog | Feedback | Order&Payment | Marketing
  $entityPos["Category"]     = @{ x = 0;    y = 200 }
  $entityPos["Product"]      = @{ x = 0;    y = 120 }
  $entityPos["ProductImage"] = @{ x = 0;    y = 40  }

  $entityPos["Review"]       = @{ x = 520;  y = 120 }

  $entityPos["Users"]        = @{ x = 1040; y = 200 }
  $entityPos["Order"]        = @{ x = 1040; y = 120 }
  $entityPos["OrderItem"]    = @{ x = 1040; y = 40  }
  $entityPos["Payment"]      = @{ x = 1040; y = -40 }

  $entityPos["UserVoucher"]  = @{ x = 1560; y = 160 }
  $entityPos["Voucher"]      = @{ x = 1560; y = 80  }
}

function Get-EntityLabel([string]$short) {
  if ($short -eq "Users") { return "Users (AspNetUsers)" }
  if (-not $entityMap.ContainsKey($short)) { return $short }
  if (-not [string]::IsNullOrWhiteSpace($entityMap[$short].table)) { return $entityMap[$short].table }
  return $short
}

$entities =
  if ($Mode -eq "core") { @("Category","Product","ProductImage","Review","Users","Order","OrderItem","Payment","UserVoucher","Voucher") }
  else { @("Banner","Category","ChatMessage","InventoryTransaction","Order","OrderItem","Payment","Product","ProductImage","Review","UserRankInfo","UserVoucher","Voucher","Users") }

[void]$sb.AppendLine('  // ENTITIES (rectangle)')
foreach ($e in $entities) {
  if (-not $entityMap.ContainsKey($e)) { continue }
  $pos = ""
  if ($Layout -eq "manual" -and $entityPos.ContainsKey($e)) { $pos = "$($entityPos[$e].x),$($entityPos[$e].y)" }
  Emit-Node $e "box" (Get-EntityLabel $e) "#b6f2b6" $pos
}
[void]$sb.AppendLine('')

[void]$sb.AppendLine('  // RELATIONSHIPS (diamond)')
foreach ($r in $rels) {
  # Place relationship roughly halfway between entity endpoints when we have coordinates.
  $pos = ""
  if ($Layout -eq "manual" -and $entityPos.ContainsKey($r.a) -and $entityPos.ContainsKey($r.b)) {
    $ax = [double]$entityPos[$r.a].x; $ay = [double]$entityPos[$r.a].y
    $bx = [double]$entityPos[$r.b].x; $by = [double]$entityPos[$r.b].y
    $mx = ($ax + $bx) / 2.0
    $my = ($ay + $by) / 2.0 + 40 # lift diamonds slightly above entities
    $pos = "{0},{1}" -f $mx,$my
  }
  Emit-Node $r.id "diamond" $r.label "#ffe4b3" $pos
  # Use xlabel instead of label for better layout, especially with orthogonal/pos constraints.
  [void]$sb.AppendLine("  $($r.id) -> $($r.a) [xlabel=""$($r.aCard)""];")
  [void]$sb.AppendLine("  $($r.id) -> $($r.b) [xlabel=""$($r.bCard)""];")
  [void]$sb.AppendLine("")
}

[void]$sb.AppendLine('  // ATTRIBUTES (oval) - full columns, arranged around each entity')
foreach ($e in $entities) {
  if (-not $entityMap.ContainsKey($e)) { continue }
  $props = $entityMap[$e].props
  if ($props.Count -eq 0) { continue }

  $center = @{ x = 0; y = 0 }
  if ($Layout -eq "manual" -and $entityPos.ContainsKey($e)) {
    $center.x = [double]$entityPos[$e].x
    $center.y = [double]$entityPos[$e].y
  }

  # Split attributes into two columns to avoid a super-wide single line.
  $left = @()
  $right = @()
  for ($i = 0; $i -lt $props.Count; $i++) {
    if (($i % 2) -eq 0) { $left += $props[$i] } else { $right += $props[$i] }
  }

  $dy = 18
  $startYLeft  = $center.y + ([math]::Ceiling($left.Count / 2.0) * $dy)
  $startYRight = $center.y + ([math]::Ceiling($right.Count / 2.0) * $dy)

  $xLeft  = $center.x - 220
  $xRight = $center.x + 220

  for ($i = 0; $i -lt $left.Count; $i++) {
    $p = $left[$i]
    $id = Escape-Id("${e}_${p}")
    $lbl = AttrLabel $e $p $entityMap
    $pos = ""
    if ($Layout -eq "manual") { $pos = "$xLeft,{0}" -f ($startYLeft - ($i * $dy)) }
    Emit-Node $id "ellipse" $lbl "#d6d9ff" $pos
    [void]$sb.AppendLine("  $e -> $id [style=dotted, dir=none, color=""#666666""];")
  }

  for ($i = 0; $i -lt $right.Count; $i++) {
    $p = $right[$i]
    $id = Escape-Id("${e}_${p}")
    $lbl = AttrLabel $e $p $entityMap
    $pos = ""
    if ($Layout -eq "manual") { $pos = "$xRight,{0}" -f ($startYRight - ($i * $dy)) }
    Emit-Node $id "ellipse" $lbl "#d6d9ff" $pos
    [void]$sb.AppendLine("  $e -> $id [style=dotted, dir=none, color=""#666666""];")
  }

  [void]$sb.AppendLine("")
}

[void]$sb.AppendLine('}')

# Graphviz can choke on UTF-8 BOM (common on Windows PowerShell 5.1),
# so we write UTF-8 WITHOUT BOM.
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($dotPath, $sb.ToString(), $utf8NoBom)

Write-Host "Wrote DOT: $dotPath"

if ([string]::IsNullOrWhiteSpace($DotExePath)) {
  $candidate = "C:\Program Files\Graphviz\bin\dot.exe"
  if (Test-Path $candidate) { $DotExePath = $candidate }
}

if (-not [string]::IsNullOrWhiteSpace($DotExePath) -and (Test-Path $DotExePath)) {
  & $DotExePath -Tpng $dotPath -o $pngPath | Out-Null
  Write-Host "Wrote PNG: $pngPath"
  & $DotExePath -Tsvg $dotPath -o $svgPath | Out-Null
  Write-Host "Wrote SVG: $svgPath"
} else {
  Write-Host "To render PNG (requires Graphviz): dot -Tpng `"$dotPath`" -o `"$pngPath`""
}

