# Release Inspector sample

A build pipeline that uses [Release Inspector](https://app-relspector-dev.azurewebsites.net/) as a release gate. It builds a tiny Windows program, signs it with a code-signing certificate held in Azure Key Vault, submits it to Release Inspector, and fails the build when the inspection finds anything serious.

Release Inspector is a personal learning project. It scans each release for malware with ClamAV and checks its hash with VirusTotal, then checks its Authenticode signatures, certificates and timestamps, and compares it with the previous release of the same product.

## How it works

The [Release gate](.github/workflows/release-gate.yml) workflow runs on a Windows runner:

1. It builds `HelloRelease.exe`, versioned from the run number, so each release is newer than the last.
2. It signs in to Azure with GitHub's OpenID Connect token, as an identity that can only sign with the test certificate. No secret is stored for this.
3. It signs the program with [AzureSignTool](https://github.com/vcsjones/AzureSignTool). The certificate's private key never leaves Key Vault: Key Vault signs the file's digest and returns the signature.
4. It downloads `ri.exe`, Release Inspector's command-line tool, from this repository's latest release.
5. It runs `ri submit`, which sends the program to Release Inspector through Azure API Management with a subscription key, waits for the report, prints the findings, and exits with a non-zero code if any is High.

## Trying the gate

Run the workflow from the **Actions** tab and choose a variant:

| Variant | What happens | Expected result |
|---|---|---|
| `signed` | Signed with the test certificate. | Passes. The certificate is self-signed, so the report has a Medium finding that its chain is not trusted, which is below the threshold. |
| `unsigned` | Not signed at all. | Fails, with a High finding that the program is not signed. |
| `altered` | Signed, then one byte of its code changed. | Fails, with a High finding that the program was modified after it was signed. |

## Settings

The workflow reads these from the repository's **Settings > Secrets and variables > Actions**:

| Name | Kind | What it is |
|---|---|---|
| `AZURE_CLIENT_ID` | Variable | The client ID of the signing identity, `msi-release-inspector-signing`. |
| `AZURE_TENANT_ID` | Variable | The Entra tenant's ID. |
| `KEY_VAULT_URL` | Variable | The Key Vault's address, such as `https://kv-relspector-dev.vault.azure.net/`. |
| `RI_API_URL` | Variable | Release Inspector's API through API Management, `https://apim-relspector-dev.azure-api.net/release-inspector/v1`. |
| `RI_WEB_URL` | Variable | Release Inspector's web interface, for a link to each release in the log. |
| `RI_SUBSCRIPTION_KEY` | Secret | A subscription key of Release Inspector's `CI` product in API Management. |

None of the variables is sensitive. The subscription key is the only secret, and the signing identity can be used only by this repository's workflows on `main`.
