

Create certificate via https://zerossl.com/ based on dsea.nl
domain dsea.nl certificate hash: 24a9c08e0a7edb4fb7c91a213b4758f2


openssl pkcs12 -export out pgad.dsea.nl.pfx -inkey pgad.dsea.nl.key -in pgad.dsea.nl.crt

# REMARK
The SSL certificate for machines.aebi-schmidt.com expires every year around March.
The IT department should get a new certificate so we can renew this.




### Renew SSL Certificate
* Open Powershell as admin:

  certutil -importPFX .\wildcard_aebi-schmidt_com.pfx
    	pass: hk^GpG7o7Rg@b%VE

* Open the wildcard certificate to get the Thumbprint and copy this as “certhash“ in the next step.
PS C:\pgad> (Get-PfxCertificate -FilePath .\pgad.dsea.nl.pfx -Password (ConvertTo-SecureString "Anneloes123!" -AsPlainText -Force)).Thumbprint



* In Powershell, go to netsh mode:
  netsh
  http delete sslcert ipport=0.0.0.0:443
  http add sslcert ipport=0.0.0.0:443 certhash=24A3F8C1A073783ECE7805372C1F39CCEA0DA225 appid={b4cda6bb-daf4-4db8-af9a-02d52e72a83d}
  exit

* In a browser open:
  https://nlhlelec01.aebi-schmidt.com/api/PpeWebService?cmd=WebPage&arg=main