# First-run Keycloak setup: create realm, clients, and admin role for Database Sentinel.
# Run after Keycloak is up (https://localhost:8443). Uses bootstrap admin (admin/admin).
# Requires PowerShell 7+ for -SkipCertificateCheck, or run with valid cert.

param(
  [string] $KeycloakUrl = "https://localhost:8443",
  [string] $AdminUser     = "admin",
  [string] $AdminPassword = "admin",
  [string] $RealmName     = "database-sentinel",
  [string] $ApiClientId   = "database-sentinel-api",
  [string] $UiClientId    = "database-sentinel-ui",
  [string] $AdminRoleName = "sentinel-admin",
  [string] $InitialUserName = "dsadmin",
  [string] $InitialUserPassword = "dsadmin"
)

$ErrorActionPreference = "Stop"

# Skip TLS validation for self-signed dev cert (PowerShell 7+)
$skipCert = $false
if ($PSVersionTable.PSVersion.Major -ge 7) {
  $skipCert = $true
}

function Get-KeycloakToken {
  $tokenUrl = "$KeycloakUrl/realms/master/protocol/openid-connect/token"
  $body = @{
    grant_type  = "password"
    client_id   = "admin-cli"
    username    = $AdminUser
    password    = $AdminPassword
  }
  $params = @{
    Uri             = $tokenUrl
    Method          = "Post"
    Body            = $body
    ContentType     = "application/x-www-form-urlencoded"
    UseBasicParsing = $true
  }
  if ($skipCert) { $params["SkipCertificateCheck"] = $true }
  $response = Invoke-RestMethod @params
  return $response.access_token
}

# Serialize body for Keycloak Admin API: pass through strings, else ConvertTo-Json (Keycloak is strict on some endpoints).
function Get-KeycloakRequestBody {
  param([object] $Body)
  if ($null -eq $Body) { return $null }
  if ($Body -is [string]) { return $Body }
  return $Body | ConvertTo-Json -Depth 10
}

function Invoke-KeycloakAdmin {
  param([string] $Method, [string] $Path, [object] $Body = $null)
  $uri = "$KeycloakUrl/admin/$Path"
  $headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
  }
  $params = @{
    Uri             = $uri
    Method          = $Method
    Headers         = $headers
    UseBasicParsing = $true
  }
  if ($skipCert) { $params["SkipCertificateCheck"] = $true }
  $bodyStr = Get-KeycloakRequestBody -Body $Body
  if ($null -ne $bodyStr) { $params["Body"] = $bodyStr }
  try {
    return Invoke-RestMethod @params
  } catch {
    if ($_.Exception.Response.StatusCode -eq 409) { return $null } # already exists
    throw
  }
}

# Like Invoke-KeycloakAdmin but returns the response so we can read headers (e.g. Location for new user id).
function Invoke-KeycloakAdminWithResponse {
  param([string] $Method, [string] $Path, [object] $Body = $null)
  $uri = "$KeycloakUrl/admin/$Path"
  $headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
  }
  $params = @{
    Uri             = $uri
    Method          = $Method
    Headers         = $headers
    UseBasicParsing = $true
  }
  if ($skipCert) { $params["SkipCertificateCheck"] = $true }
  $bodyStr = Get-KeycloakRequestBody -Body $Body
  if ($null -ne $bodyStr) { $params["Body"] = $bodyStr }
  return Invoke-WebRequest @params
}

function Get-KeycloakRealm {
  try {
    $params = @{
      Uri             = "$KeycloakUrl/admin/realms/$RealmName"
      Method          = "Get"
      Headers         = @{ "Authorization" = "Bearer $token" }
      UseBasicParsing = $true
    }
    if ($skipCert) { $params["SkipCertificateCheck"] = $true }
    Invoke-RestMethod @params
    return $true
  } catch {
    if ($_.Exception.Response.StatusCode -eq 404) { return $false }
    throw
  }
}

Write-Host "Connecting to Keycloak at $KeycloakUrl..." -ForegroundColor Cyan
$token = Get-KeycloakToken
Write-Host "Authenticated." -ForegroundColor Green

# Create realm
if (Get-KeycloakRealm) {
  Write-Host "Realm '$RealmName' already exists. Skipping realm creation." -ForegroundColor Yellow
} else {
  Write-Host "Creating realm '$RealmName'..." -ForegroundColor Cyan
  Invoke-KeycloakAdmin -Method Post -Path "realms" -Body @{
    realm        = $RealmName
    enabled      = $true
    displayName  = "Database Sentinel"
  } | Out-Null
  Write-Host "Realm created." -ForegroundColor Green
}

# Create API client (confidential; for API to validate tokens / service account)
$clientsPath = "realms/$RealmName/clients"
$existingClients = @(Invoke-KeycloakAdmin -Method Get -Path $clientsPath)
if ($existingClients.clientId -contains $ApiClientId) {
  Write-Host "Client '$ApiClientId' already exists." -ForegroundColor Yellow
} else {
  Write-Host "Creating client '$ApiClientId' (confidential)..." -ForegroundColor Cyan
  Invoke-KeycloakAdmin -Method Post -Path $clientsPath -Body @{
    clientId                = $ApiClientId
    name                    = "Database Sentinel API"
    enabled                 = $true
    publicClient            = $false
    directAccessGrantsEnabled = $true
    serviceAccountsEnabled  = $true
    standardFlowEnabled      = $true
  } | Out-Null
  Write-Host "Client created. Retrieve its secret in Keycloak: Realm -> Clients -> $ApiClientId -> Credentials." -ForegroundColor Green
}

# Create UI client (public; for Avalonia app + Swagger UI - Auth Code + PKCE, redirect to localhost)
$msalRedirectUris = @(
  "http://localhost:46421",
  "http://127.0.0.1:46421",
  "https://localhost:7229/swagger/oauth2-redirect.html",
  "http://localhost:5210/swagger/oauth2-redirect.html"
)
$existingClients = @(Invoke-KeycloakAdmin -Method Get -Path $clientsPath)
if ($existingClients.clientId -contains $UiClientId) {
  Write-Host "Client '$UiClientId' already exists. Ensuring redirect URIs for MSAL..." -ForegroundColor Yellow
  $byClientId = @(Invoke-KeycloakAdmin -Method Get -Path "$clientsPath`?clientId=$UiClientId")
  $uiClient = $byClientId | Where-Object { $_.clientId -eq $UiClientId } | Select-Object -First 1
  if (-not $uiClient -or -not $uiClient.id) {
    Write-Host "Warning: Could not resolve client id for '$UiClientId'. Add redirect URIs manually in Keycloak." -ForegroundColor Yellow
  } else {
    $existingRedirects = @($uiClient.redirectUris)
    $toAdd = $msalRedirectUris | Where-Object { $existingRedirects -notcontains $_ }
    if ($toAdd.Count -gt 0) {
      $newRedirects = $existingRedirects + $toAdd
      $clientPath = "realms/$RealmName/clients/$($uiClient.id)"
      $full = Invoke-KeycloakAdmin -Method Get -Path $clientPath
      $full.redirectUris = @($newRedirects)
      Invoke-KeycloakAdmin -Method Put -Path $clientPath -Body (Get-KeycloakRequestBody -Body $full) | Out-Null
      Write-Host "Added redirect URI(s) for MSAL." -ForegroundColor Green
    }
  }
} else {
  Write-Host "Creating client '$UiClientId' (public)..." -ForegroundColor Cyan
  Invoke-KeycloakAdmin -Method Post -Path $clientsPath -Body @{
    clientId                  = $UiClientId
    name                      = "Database Sentinel UI"
    enabled                   = $true
    publicClient              = $true
    directAccessGrantsEnabled = $true
    standardFlowEnabled       = $true
    redirectUris              = $msalRedirectUris
  } | Out-Null
  Write-Host "Client created." -ForegroundColor Green
}

# Ensure UI client has an Audience mapper so access tokens include the API audience (fixes "audience 'account' is invalid")
$byClientId = @(Invoke-KeycloakAdmin -Method Get -Path "$clientsPath`?clientId=$UiClientId")
$uiClientForMapper = $byClientId | Where-Object { $_.clientId -eq $UiClientId } | Select-Object -First 1
if ($uiClientForMapper -and $uiClientForMapper.id) {
  $mapperPath = "realms/$RealmName/clients/$($uiClientForMapper.id)/protocol-mappers/models"
  $existingMappers = @(Invoke-KeycloakAdmin -Method Get -Path $mapperPath)
  $hasAudienceMapper = $existingMappers | Where-Object { $_.name -eq "audience-api" } | Select-Object -First 1
  if (-not $hasAudienceMapper) {
    Write-Host "Adding Audience mapper to '$UiClientId' (audience: $ApiClientId)..." -ForegroundColor Cyan
    Invoke-KeycloakAdmin -Method Post -Path $mapperPath -Body @{
      name           = "audience-api"
      protocol       = "openid-connect"
      protocolMapper = "oidc-audience-mapper"
      config         = @{
        "included.custom.audience" = $ApiClientId
        "access.token.claim"       = "true"
      }
    } | Out-Null
    Write-Host "Audience mapper added. Tokens from this client will include aud: $ApiClientId." -ForegroundColor Green
  } else {
    Write-Host "Audience mapper already present on '$UiClientId'." -ForegroundColor Yellow
  }
} else {
  Write-Host "Warning: Could not resolve UI client for audience mapper." -ForegroundColor Yellow
}

# Create realm role: sentinel-admin
$rolesPath = "realms/$RealmName/roles"
$existingRoles = @(Invoke-KeycloakAdmin -Method Get -Path $rolesPath)
if ($existingRoles.name -contains $AdminRoleName) {
  Write-Host "Role '$AdminRoleName' already exists." -ForegroundColor Yellow
} else {
  Write-Host "Creating realm role '$AdminRoleName'..." -ForegroundColor Cyan
  Invoke-KeycloakAdmin -Method Post -Path $rolesPath -Body @{
    name        = $AdminRoleName
    description = "Administrator for Database Sentinel"
  } | Out-Null
  Write-Host "Role created." -ForegroundColor Green
}

# Create initial user (dsadmin): create user, set password, assign sentinel-admin role
$usersPath = "realms/$RealmName/users"
# Check if user already exists (GET returns array; match username exactly)
$existingUsers = @(Invoke-KeycloakAdmin -Method Get -Path "$usersPath`?username=$InitialUserName")
$existing = $existingUsers | Where-Object { $_.username -eq $InitialUserName } | Select-Object -First 1
if ($existing) {
  Write-Host "User '$InitialUserName' already exists." -ForegroundColor Yellow
} else {
  Write-Host "Creating user '$InitialUserName'..." -ForegroundColor Cyan
  # Keycloak requires email, firstName, lastName for login; use username-based defaults.
  $createBody = @{
    requiredActions = @()
    emailVerified   = $false
    username        = $InitialUserName
    email           = "$InitialUserName@example.org"
    firstName       = $InitialUserName
    lastName        = $InitialUserName
    groups          = @()
    attributes      = @{}
    enabled         = $true
  }
  $createResponse = Invoke-KeycloakAdminWithResponse -Method Post -Path $usersPath -Body $createBody
  $location = $createResponse.Headers["Location"]
  if ($location -is [System.Collections.IEnumerable] -and $location -isnot [string]) { $location = $location[0] }
  if (-not $location) {
    Write-Host "Warning: Create user did not return Location header. Trying lookup by username..." -ForegroundColor Yellow
    $users = @(Invoke-KeycloakAdmin -Method Get -Path "$usersPath`?username=$InitialUserName")
    $user = $users | Where-Object { $_.username -eq $InitialUserName } | Select-Object -First 1
    $userId = $user.id
  } else {
    # Location is like https://localhost:8443/admin/realms/database-sentinel/users/abc-123-uuid
    $userId = $location.TrimEnd("/").Split("/")[-1]
  }
  if (-not $userId) {
    Write-Host "Warning: Could not determine user id for '$InitialUserName'. Password and role were not set." -ForegroundColor Yellow
  } else {
    Write-Host "Setting password for '$InitialUserName'..." -ForegroundColor Cyan
    Invoke-KeycloakAdmin -Method Put -Path "$usersPath/$userId/reset-password" -Body @{
      temporary = $false
      type      = "password"
      value     = $InitialUserPassword
    } | Out-Null
    Write-Host "Assigning role '$AdminRoleName'..." -ForegroundColor Cyan
    $role = Invoke-KeycloakAdmin -Method Get -Path "realms/$RealmName/roles/$AdminRoleName"
    # Role-mapping expects exact JSON shape; build literal string to avoid Keycloak "Cannot parse the JSON".
    $desc = if ($role.description) { $role.description.Replace('"', '\"') } else { "Administrator for Database Sentinel" }
    $roleMappingJson = "[{`"id`":`"$($role.id)`",`"name`":`"$($role.name)`",`"description`":`"$desc`",`"composite`":$(if ($role.composite) { 'true' } else { 'false' }),`"clientRole`":$(if ($role.clientRole) { 'true' } else { 'false' }),`"containerId`":`"$($role.containerId)`"}]"
    Invoke-KeycloakAdmin -Method Post -Path "$usersPath/$userId/role-mappings/realm" -Body $roleMappingJson | Out-Null
    Write-Host "User created with role '$AdminRoleName'. You can sign in with $InitialUserName / $InitialUserPassword" -ForegroundColor Green
  }
}

Write-Host ""
Write-Host "Setup complete." -ForegroundColor Green
Write-Host "  Realm:      $RealmName" -ForegroundColor Cyan
Write-Host "  API client: $ApiClientId (confidential; get secret in Keycloak Admin -> Clients -> Credentials)" -ForegroundColor Cyan
Write-Host "  UI client:  $UiClientId (public)" -ForegroundColor Cyan
Write-Host "  Admin role: $AdminRoleName" -ForegroundColor Cyan
Write-Host "  Initial user: $InitialUserName / $InitialUserPassword (sign in from the app)" -ForegroundColor Cyan
Write-Host "  Keycloak Admin: $KeycloakUrl/admin (login: $AdminUser)" -ForegroundColor Cyan
