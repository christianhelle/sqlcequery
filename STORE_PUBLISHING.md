# Publishing SQL Compact Query Analyzer to the Microsoft Store

This guide covers the manual steps required to publish SQL Compact Query Analyzer to the Microsoft Store as an MSIX package.

## Prerequisites

- Windows 10/11 development machine
- Visual Studio 2017+ with **Universal Windows Platform development** workload (or the **MSIX Packaging Tools** optional component)
- A Microsoft account

## Overview

The repository already includes:
- ✅ **Windows Application Packaging Project** (`Source/PackageProject/`) — wraps the app as MSIX
- ✅ **MSIX visual assets** (`Source/PackageProject/Images/`) — all required icon sizes
- ✅ **App manifest** (`Source/PackageProject/Package.appxmanifest`) — with `.sdf` file association and `runFullTrust` capability
- ✅ **GitHub Actions workflow** (`.github/workflows/msix.yml`) — builds MSIX packages for x86/x64

**You need to complete these manual steps:**

---

## Step 1: Create a Microsoft Store Developer Account

1. Go to [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com)
2. Click **"Get started for free"**
3. Choose account type:
   - **Individual developer** — free (in supported markets)
   - **Company account** — $99 USD one-time registration fee
4. Sign in with your Microsoft account (or create a new one)
5. Complete **identity verification** (government-issued ID + selfie)
6. Fill in your profile details
7. Click **"Go to Partner Center dashboard"**

> **Reference**: [Open a developer account](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account)

---

## Step 2: Reserve Your App Name

1. Navigate to [Partner Center Apps and Games page](https://aka.ms/submitwindowsapp)
2. Click **New product** → **MSIX or PWA app**
3. Enter **"SQL Compact Query Analyzer"** and click **Check availability**
4. If available, click **Reserve product name**

> **Note**: Reserved names expire after 3 months if not used. You can reserve multiple names and choose later.

> **Reference**: [Reserve your app's name](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/reserve-your-apps-name)

---

## Step 3: Update the App Manifest with Store Identity

After reserving your app name, you need to update the MSIX manifest with the identity values from Partner Center.

1. In Partner Center, go to your app → **Product management** → **Product Identity**
2. Copy these values:
   - **Package/Identity/Name** (e.g., `12345CompanyName.AppName`)
   - **Package/Identity/Publisher** (e.g., `CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`)
   - **Package/Properties/PublisherDisplayName** (e.g., `Christian Resma Helle`)

3. Edit `Source/PackageProject/Package.appxmanifest` and replace the placeholder values:

```xml
<!-- Replace these placeholder values -->
<Identity
  Name="ChristianHelle.SQLCompactQueryAnalyzer"    <!-- Replace with Package/Identity/Name -->
  Version="1.3.4.0"
  Publisher="CN=PLACEHOLDER"                        <!-- Replace with Package/Identity/Publisher -->
  ProcessorArchitecture="x64" />

<Properties>
  <PublisherDisplayName>Christian Resma Helle</PublisherDisplayName>  <!-- Verify this matches -->
</Properties>
```

> **Important**: The `Publisher` value must exactly match the publisher subject information from Partner Center. The MSIX package will not pass certification otherwise.

---

## Step 4: Build the MSIX Package

### Option A: Build Locally with Visual Studio

1. Open `Source/QueryAnalyzer.sln` in Visual Studio
2. Right-click the **PackageProject** → **Set as Startup Project**
3. Select **Release** configuration and platform (**x86** or **x64**)
4. Right-click **PackageProject** → **Publish** → **Create App Packages**
5. Choose **Microsoft Store** as the distribution method
6. Sign in with your Partner Center account
7. Select your reserved app name
8. Choose architectures (x86, x64, or both)
9. Click **Create** to generate the `.msixupload` file

### Option B: Build with GitHub Actions

1. Push to the `release` branch or trigger the **MSIX Package** workflow manually
2. The workflow builds MSIX packages for both x86 and x64
3. Download the MSIX artifacts from the workflow run

> **Note**: The CI-built packages are unsigned. Partner Center will sign them automatically when you upload.

---

## Step 5: Create Your Store Listing

In Partner Center, navigate to your app and fill in the Store listing:

### Required Information

| Field | Suggested Value |
|-------|----------------|
| **App name** | SQL Compact Query Analyzer |
| **Description** | A SQL Server Compact Edition Database Query Analyzer. Create new databases, execute SQL queries, view and edit data, display schema information, generate scripts, and manage SQL CE databases (versions 3.0, 3.1, 3.5, and 4.0). |
| **Short description** | Query analyzer and database management tool for SQL Server Compact Edition |
| **Category** | Developer Tools |
| **Screenshots** | At least 1 screenshot required (see below) |

### Screenshots

Upload at least one screenshot. Recommended size: **1366×768** pixels (or 768×1366 for portrait).

You can use the existing screenshots from the `Screenshots/` folder:
- `QueryResultMessages.png` — Main query interface
- `EditTable.png` — Table editing
- `ContentWithImages.png` — Image field display
- `CreateDatabase.png` — Database creation
- `DatabaseInfo.png` — Database information view

> **Note**: Screenshots may need to be resized to meet Store requirements. Accepted sizes include 1366×768, 2560×1440, or other [supported dimensions](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images).

### App Icon for Store Listing

Upload a **300×300 pixel PNG** app icon for the Store listing page. You can resize `Source/Icon.png` for this purpose.

### Additional Recommended Fields

- **What's new**: Describe changes in each update
- **Features**: Bullet-point list of key features
- **Privacy policy URL**: Required if your app accesses the internet or collects data. If the app is fully offline, you can indicate this.
- **Website**: `https://github.com/christianhelle/sqlcequery`
- **Support contact**: Your email or a GitHub Issues URL

---

## Step 6: Set Pricing and Availability

1. In Partner Center, go to **Pricing and availability**
2. Set the **price** (Free recommended for this open-source project)
3. Choose **markets** (all markets or specific ones)
4. Set **visibility** (Public)
5. Choose **release date** (as soon as certified, or a specific date)

---

## Step 7: Complete Age Rating Questionnaire

1. In Partner Center, go to **Age ratings**
2. Complete the IARC questionnaire
3. For a database tool, this should qualify for the lowest age rating (Everyone/3+)

---

## Step 8: Upload Packages and Submit

1. Go to **Packages** in your submission
2. Upload the `.msixupload` file (from Visual Studio) or individual `.msix` files
3. Partner Center will validate the package
4. Review all sections for completeness
5. Click **Submit to the Store**

### Certification Timeline

- Certification typically takes **24–48 hours** (may vary)
- You'll receive an email notification with the result
- If certification fails, review the failure report and fix the issues

---

## Step 9: Post-Publication

After your app is certified and published:

### Monitor Performance
- View download stats, ratings, and reviews in Partner Center
- Check **Health** reports for crash data
- Monitor **Usage** reports for engagement metrics

### Publish Updates
1. Update the version in `Package.appxmanifest`
2. Build new MSIX packages
3. Create a new submission in Partner Center
4. Upload the new packages
5. Submit for certification

### Automated Store Submissions (Optional)
For CI/CD automation, consider:
- [Microsoft Store Developer CLI](https://github.com/microsoft/msstore-cli) — command-line tool for Store submissions
- [Microsoft Store submission API](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services)

---

## Troubleshooting

### Common Certification Failures

| Issue | Solution |
|-------|----------|
| **Identity mismatch** | Ensure `Publisher` in manifest exactly matches Partner Center value |
| **Missing assets** | Verify all required icon sizes are present in `Images/` folder |
| **App crashes on launch** | Test the MSIX package locally before submitting |
| **Privacy policy required** | Add a privacy policy URL if your app uses network |

### Testing MSIX Locally

To test the MSIX package before submitting:

1. Build the package (unsigned) via Visual Studio or CI
2. Create a self-signed certificate for testing:
   ```powershell
   New-SelfSignedCertificate -Type Custom -Subject "CN=PLACEHOLDER" `
     -KeyUsage DigitalSignature -FriendlyName "Test cert" `
     -CertStoreLocation "Cert:\CurrentUser\My" `
     -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
   ```
3. Sign the package:
   ```powershell
   SignTool sign /fd SHA256 /a /f MyCert.pfx /p password YourPackage.msix
   ```
4. Install and test:
   ```powershell
   Add-AppxPackage -Path YourPackage.msix
   ```

> **Important**: For local testing, the certificate subject (`CN=...`) must match the `Publisher` value in the manifest.

---

## Reference Links

- [Microsoft Store publishing overview](https://learn.microsoft.com/en-us/windows/apps/publish/)
- [MSIX packaging for desktop apps](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)
- [App package manifest](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/appx-package-manifest)
- [Package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)
- [Store listing guidelines](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/create-your-app-store-listing)
- [Desktop app preparation checklist](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-prepare)
