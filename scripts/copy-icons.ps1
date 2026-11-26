$src = "C:\Users\fadyf\Desktop\Stocka\src\Hosts\Stocka.MVC\node_modules\lucide-static\icons"
$dst = "C:\Users\fadyf\Desktop\Stocka\src\Hosts\Stocka.MVC\wwwroot\icons"

$icons = @'
house, layout-dashboard, menu, panel-left, panel-right, panel-left-close, panel-left-open, search, command, bell, inbox, help-circle, circle-user-round, settings, sliders-horizontal, log-out, log-in, arrow-left, arrow-right, chevron-down, chevron-up, chevron-left, chevron-right, more-horizontal, more-vertical, external-link, maximize, minimize, refresh-cw, panel-top,
building-2, store, store-icon, network, git-branch, users, user, user-round-plus, user-round-check, user-round-x, contact, id-card, shield, shield-check, key-round, lock, unlock, fingerprint, user-cog, badge-check, workflow,
package, packages, package-plus, package-minus, package-check, package-x, tags, tag, barcode, scan-barcode, qr-code, boxes, ruler, scale, image, images, palette, layers, list, archive, copy, file-plus,
warehouse, package-search, package-open, arrow-left-right, arrow-down-to-line, arrow-up-from-line, clipboard-list, clipboard-check, list-checks, history, activity, layers-3, calendar-clock, alert-triangle, trending-down, ban, undo-2, trash-2, map-pin, map-pinned,
shopping-cart, shopping-basket, truck, truck-icon, handshake, contact-round, file-text, file-plus-2, clipboard-pen, receipt, calendar-check, wallet, credit-card, circle-dollar-sign,
shopping-cart-plus, shopping-cart-minus, calculator, banknote, coins, receipt-text, file-check, file-x, printer, percent, badge-percent, hand-coins, repeat, clock, monitor, receipt-cent,
user-round, users-round, phone, mail, map, message-circle, messages-square, star, heart, notebook-tabs, calendar-days,
landmark, vault, arrow-down-circle, arrow-up-circle, trending-up, chart-pie, chart-column, book-open, book-text, file-spreadsheet, calendar-range, badge-dollar-sign, piggy-bank, circle-check, circle-alert,
plus-circle, minus-circle, calendar, chart-line,
chart-no-axes-combined, chart-bar, chart-area, chart-no-axes-gantt, gauge, filter, file-chart-column, file-chart-line, file-chart-pie, download, file-pdf,
ticket-percent, gift, arrow-down-wide-narrow, list-filter,
briefcase-business, timer, award, graduation-cap,
truck-electric, route, navigation,
package-arrow-bend-up-left, circle-x,
stamp,
check-circle, user-check, message-square,
scroll-text, file-clock, eye, database,
settings-2, languages, globe, sun, moon, paintbrush, currency, hard-drive, cloud, upload,
plug, webhook, server, file-up, file-down, file-json, file-code, link, unlink, terminal, code, key,
files, file, file-image, file-archive, folder, folder-open, folder-plus, paperclip,
check, x, alert-circle, info, loader-circle, shield-alert, wifi-off, cloud-off, circle-dot, circle-minus, badge-alert,
wrench, toolbox, hammer,
plus, pencil, arrow-up, arrow-down
'@ -split '[,\s]+' | Where-Object { $_ } | Select-Object -Unique

# old name -> new name in recent Lucide
$aliases = @{
  'alert-triangle'    = 'triangle-alert'
  'alert-circle'      = 'circle-alert'
  'help-circle'       = 'circle-help'
  'check-circle'      = 'circle-check'
  'plus-circle'       = 'circle-plus'
  'minus-circle'      = 'circle-minus'
  'arrow-down-circle' = 'circle-arrow-down'
  'arrow-up-circle'   = 'circle-arrow-up'
  'more-horizontal'   = 'ellipsis'
  'more-vertical'     = 'ellipsis-vertical'
  'user-check'        = 'user-round-check'
  'store-icon'        = 'store'
  'truck-icon'        = 'truck'
  'file-pdf'          = 'file-text'
  'layers-3'          = 'layers'
}

if (-not (Test-Path $src)) { Write-Error "Source not found: $src"; exit 1 }
New-Item -ItemType Directory -Force -Path $dst | Out-Null

$copied = 0; $viaAlias = @(); $missing = @(); $conflicts = @()

foreach ($name in $icons) {
  $target = Join-Path $dst "$name.svg"
  if (Test-Path $target) { $conflicts += $name; continue }

  $direct = Join-Path $src "$name.svg"
  if (Test-Path $direct) {
    Copy-Item $direct $target
    $copied++
    continue
  }

  $alias = $aliases[$name]
  $aliasFile = if ($alias) { Join-Path $src "$alias.svg" } else { $null }
  if ($aliasFile -and (Test-Path $aliasFile)) {
    Copy-Item $aliasFile $target
    $copied++
    $viaAlias += "$name -> $alias"
  } else {
    $missing += $name
  }
}

Write-Host "`nCopied: $copied"
Write-Host "Via alias ($($viaAlias.Count)):"; $viaAlias | ForEach-Object { "  $_" }
Write-Host "Already existed, skipped ($($conflicts.Count)):"; $conflicts | ForEach-Object { "  $_" }
Write-Host "Missing ($($missing.Count)):"; $missing | ForEach-Object { "  $_" }
