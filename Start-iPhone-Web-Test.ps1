$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$webRoot = Join-Path $projectRoot 'Builds\Web'

if (-not (Test-Path (Join-Path $webRoot 'index.html'))) {
    Write-Host 'Web build not found. In Unity choose: Driving Sim > Build Web for iPhone' -ForegroundColor Yellow
    exit 1
}

$address = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object {
        $_.IPAddress -notlike '127.*' -and
        $_.IPAddress -notlike '169.254.*' -and
        $_.InterfaceAlias -notmatch 'Loopback|vEthernet|Virtual|VPN'
    } |
    Select-Object -First 1 -ExpandProperty IPAddress

if (-not $address) { $address = '<YOUR-PC-IP>' }

Write-Host ''
Write-Host 'Keep this window open.' -ForegroundColor Cyan
Write-Host "On the iPhone, open Safari and visit: http://${address}:8000" -ForegroundColor Green
Write-Host 'The PC and iPhone must be connected to the same Wi-Fi.' -ForegroundColor Cyan
Write-Host 'If Windows Firewall asks, allow access on Private networks.' -ForegroundColor Cyan
Write-Host ''

$mimeTypes = @{
    '.html' = 'text/html; charset=utf-8'
    '.js'   = 'application/javascript'
    '.wasm' = 'application/wasm'
    '.data' = 'application/octet-stream'
    '.json' = 'application/json'
    '.css'  = 'text/css'
    '.png'  = 'image/png'
    '.jpg'  = 'image/jpeg'
    '.jpeg' = 'image/jpeg'
    '.ico'  = 'image/x-icon'
}

$rootPath = [IO.Path]::GetFullPath($webRoot)
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Any, 8000)
$listener.Start()

try {
    while ($true) {
        $client = $listener.AcceptTcpClient()
        try {
            $stream = $client.GetStream()
            $reader = [IO.StreamReader]::new($stream, [Text.Encoding]::ASCII, $false, 1024, $true)
            $requestLine = $reader.ReadLine()
            while ($reader.ReadLine()) { }

            $requestParts = $requestLine -split ' '
            $requestPath = if ($requestParts.Count -ge 2) { $requestParts[1].Split('?')[0] } else { '/' }
            $relativePath = [Uri]::UnescapeDataString($requestPath).TrimStart('/')
            if ([string]::IsNullOrWhiteSpace($relativePath)) { $relativePath = 'index.html' }

            $filePath = [IO.Path]::GetFullPath((Join-Path $rootPath $relativePath))
            if (-not $filePath.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
                $body = [Text.Encoding]::UTF8.GetBytes('404 Not Found')
                $header = "HTTP/1.1 404 Not Found`r`nContent-Type: text/plain`r`nContent-Length: $($body.Length)`r`nConnection: close`r`n`r`n"
            } else {
                $body = [IO.File]::ReadAllBytes($filePath)
                $extension = [IO.Path]::GetExtension($filePath).ToLowerInvariant()
                $mime = if ($mimeTypes.ContainsKey($extension)) { $mimeTypes[$extension] } else { 'application/octet-stream' }
                $header = "HTTP/1.1 200 OK`r`nContent-Type: $mime`r`nContent-Length: $($body.Length)`r`nCache-Control: no-cache`r`nConnection: close`r`n`r`n"
            }

            $headerBytes = [Text.Encoding]::ASCII.GetBytes($header)
            $stream.Write($headerBytes, 0, $headerBytes.Length)
            $stream.Write($body, 0, $body.Length)
            $stream.Flush()
            $reader.Dispose()
        } finally {
            $client.Dispose()
        }
    }
} finally {
    $listener.Stop()
}
