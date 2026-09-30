# Exports the corporate CA chain (everything above the leaf) seen when connecting
# to api.nuget.org through the corporate network into certs\corp-ca.crt (PEM).
$ErrorActionPreference = "Stop"
$hostName = "api.nuget.org"
$tcp = New-Object System.Net.Sockets.TcpClient($hostName, 443)
$ssl = New-Object System.Net.Security.SslStream($tcp.GetStream(), $false, ({ $true }))
$ssl.AuthenticateAsClient($hostName)
$leaf = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($ssl.RemoteCertificate)
$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.ChainPolicy.RevocationMode = "NoCheck"
[void]$chain.Build($leaf)
$ssl.Dispose(); $tcp.Dispose()

New-Item -ItemType Directory -Force -Path "certs" | Out-Null
$pem = ""
$count = 0
for ($i = 1; $i -lt $chain.ChainElements.Count; $i++) {
    $c = $chain.ChainElements[$i].Certificate
    Write-Host ("Exporting: " + $c.Subject)
    $pem += "-----BEGIN CERTIFICATE-----`n"
    $pem += [Convert]::ToBase64String($c.RawData, "InsertLineBreaks") + "`n"
    $pem += "-----END CERTIFICATE-----`n"
    $count++
}
if ($count -eq 0) {
    Write-Host "No CA certificates found above the leaf - is SSL inspection actually active?"
    exit 1
}
[System.IO.File]::WriteAllText((Join-Path (Get-Location) "certs\corp-ca.crt"), $pem, [System.Text.Encoding]::ASCII)
Write-Host "Written certs\corp-ca.crt ($count certificate(s)). Now run: docker compose up --build"
