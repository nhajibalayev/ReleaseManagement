# Corporate CA certificates

Corporate networks often intercept HTTPS (SSL inspection). Inside the Docker
build the `dotnet restore` step then fails with `NU1301 ... UntrustedRoot`.

Fix: put the corporate root/intermediate certificates here as `*.crt` (PEM) files.
The Dockerfile copies everything in this folder into the system trust store of
both the build and runtime images. Files here are git-ignored except this README.

To export them on a Windows machine that is inside the corporate network,
run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File export-corp-ca.ps1
```
