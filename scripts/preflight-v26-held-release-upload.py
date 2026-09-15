#!/usr/bin/env python3
from pathlib import Path
import re,sys
R=Path(__file__).resolve().parents[1];P=R/'scripts/publish-v26-release.ps1';H=R/'scripts/invoke-v26-held-release-upload.ps1'
def v(p,h):
 e=[]
 req=['[IO.FileShare]::Read','ComputeHash($held.Stream)','$held.Stream.Position = 0','[System.Net.Http.StreamContent]::new($held.Stream)',"[string]::Equals($uploadUri.Host, 'uploads.github.com', [StringComparison]::OrdinalIgnoreCase)",'$uploadUri.IsDefaultPort','[string]::IsNullOrEmpty($uploadUri.UserInfo)','[string]::IsNullOrEmpty($uploadUri.Fragment)','[string]::IsNullOrEmpty($uploadUri.Query)','$handler.AllowAutoRedirect = $false','[System.Net.Http.HttpClient]::new($handler)','$response.StatusCode -ne [System.Net.HttpStatusCode]::Created','ConvertFrom-Json -ErrorAction Stop',"[string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)","$expectedDigest = 'sha256:' + $hashHex.ToLowerInvariant()","[string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)",'UploadedAssetId']
 for t in req:
  if t not in h:e.append('missing '+t)
 if '$response.IsSuccessStatusCode' in h:e.append('generic 2xx forbidden')
 if re.search(r'throw[^\r\n]*\$responseBody',h):e.append('response body leak')
 order=['[System.Net.Http.HttpClientHandler]::new()','$handler.AllowAutoRedirect = $false','[System.Net.Http.HttpClient]::new($handler)','DefaultRequestHeaders.Authorization','[System.Net.Http.HttpRequestMessage]::new','SendAsync','$response.StatusCode -ne [System.Net.HttpStatusCode]::Created','ReadAsStringAsync','ConvertFrom-Json -ErrorAction Stop',"[string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)","$expectedDigest = 'sha256:' + $hashHex.ToLowerInvariant()"]
 q=[h.find(x) for x in order]
 if min(q)<0 or q!=sorted(q):e.append('upload authority ordering')
 if r'& .\scripts\invoke-v26-held-release-upload.ps1' not in p:e.append('publisher held helper missing')
 return e
def main():
 p=P.read_text(encoding='utf-8');h=H.read_text(encoding='utf-8');e=v(p,h)
 if e:
  [print('ERROR: '+x) for x in e];return 1
 muts=[h.replace('$handler.AllowAutoRedirect = $false','$handler.AllowAutoRedirect = $true',1),h.replace('$response.StatusCode -ne [System.Net.HttpStatusCode]::Created','-not $response.IsSuccessStatusCode',1),h.replace("if (-not [string]::Equals([string]$uploaded.state, 'uploaded', [StringComparison]::Ordinal)) {",'if ($false) {',1),h.replace('-not [string]::Equals($uploadedDigest, $expectedDigest, [StringComparison]::OrdinalIgnoreCase)','$false',1),h.replace('$uploaded = $responseBody | ConvertFrom-Json -ErrorAction Stop','$uploaded = $responseBody | ConvertFrom-Json',1)]
 if any(m==h or not v(p,m) for m in muts):print('ERROR: mutation escaped guard');return 1
 print('PASS V26 held release upload endpoint redirect status state digest authority');return 0
if __name__=='__main__':raise SystemExit(main())
