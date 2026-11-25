#!/usr/bin/env bash
# =====================================================================
# Poli-App: despliegue en Azure (ejecutar en Azure Cloud Shell - Bash)
#   bash deploy/azure-deploy.sh
# Crea (si no existen): grupo de recursos, Azure SQL Database con la
# oferta gratuita (serverless, pausa automática), App Service Linux F1
# (gratuito) con .NET 10 e identidad administrada. La API se conecta a
# SQL con la identidad administrada: no se usan contraseñas de base de datos.
# =====================================================================
set -euo pipefail

CONF="$HOME/.poliapp-azure.env"
if [[ -f "$CONF" ]]; then source "$CONF"; fi
SUF="${SUF:-$(tr -dc 'a-z0-9' </dev/urandom | head -c 5)}"
LOC="${LOC:-eastus2}"
RG="${RG:-rg-poliapp}"
PLAN="${PLAN:-plan-poliapp}"
APP="${APP:-poliapp-$SUF}"
SQL="${SQL:-sql-poliapp-$SUF}"
DB="${DB:-PoliAppDB}"
printf 'SUF=%s\nLOC=%s\nRG=%s\nPLAN=%s\nAPP=%s\nSQL=%s\nDB=%s\n' "$SUF" "$LOC" "$RG" "$PLAN" "$APP" "$SQL" "$DB" > "$CONF"

paso() { echo; echo "==> $*"; }
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"

paso "Grupo de recursos $RG ($LOC)"
az group create -n "$RG" -l "$LOC" -o none

paso "Servidor Azure SQL $SQL (solo autenticación Microsoft Entra)"
OID=$(az ad signed-in-user show --query id -o tsv)
UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv)
if ! az sql server show -g "$RG" -n "$SQL" -o none 2>/dev/null; then
  az sql server create -g "$RG" -n "$SQL" -l "$LOC" \
    --enable-ad-only-auth --external-admin-principal-type User \
    --external-admin-name "$UPN" --external-admin-sid "$OID" -o none
fi
az sql server firewall-rule create -g "$RG" -s "$SQL" -n PermitirServiciosAzure \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

paso "Base de datos $DB (oferta gratuita, pausa automática)"
if ! az sql db show -g "$RG" -s "$SQL" -n "$DB" -o none 2>/dev/null; then
  az sql db create -g "$RG" -s "$SQL" -n "$DB" -e GeneralPurpose -f Gen5 -c 2 \
    --compute-model Serverless --use-free-limit --free-limit-exhaustion-behavior AutoPause \
    --backup-storage-redundancy Local -o none
fi

paso "Plan App Service $PLAN (Linux F1 gratuito) y aplicación $APP"
if ! az appservice plan show -g "$RG" -n "$PLAN" -o none 2>/dev/null; then
  az appservice plan create -g "$RG" -n "$PLAN" -l "$LOC" --is-linux --sku F1 -o none
fi
if ! az webapp show -g "$RG" -n "$APP" -o none 2>/dev/null; then
  az webapp create -g "$RG" -p "$PLAN" -n "$APP" --runtime "DOTNETCORE:10.0" -o none
fi
az webapp update -g "$RG" -n "$APP" --https-only true -o none
az webapp identity assign -g "$RG" -n "$APP" -o none

paso "Permisos de la identidad administrada en la base de datos"
TSQL="IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$APP') CREATE USER [$APP] FROM EXTERNAL PROVIDER; ALTER ROLE db_owner ADD MEMBER [$APP];"
python3 -m pip install --user --quiet pyodbc >/dev/null 2>&1 || true
TOKEN=$(az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv)
SQLSRV="$SQL.database.windows.net" SQLDB="$DB" TOKEN="$TOKEN" TSQL="$TSQL" python3 - <<'PY'
import os, struct, pyodbc
drivers=[d for d in pyodbc.drivers() if 'SQL Server' in d]
if not drivers: raise SystemExit('No hay driver ODBC de SQL Server en Cloud Shell')
tok=os.environ['TOKEN'].encode('utf-16-le'); ts=struct.pack(f'<I{len(tok)}s',len(tok),tok)
cn=pyodbc.connect(f"Driver={{{drivers[-1]}}};Server=tcp:{os.environ['SQLSRV']},1433;Database={os.environ['SQLDB']};Encrypt=yes;Connection Timeout=120",attrs_before={1256:ts},autocommit=True)
cn.execute(os.environ['TSQL']); print('Usuario de base de datos creado para la identidad administrada.')
PY

paso "Configuración de la aplicación"
CS="Server=tcp:$SQL.database.windows.net,1433;Database=$DB;Authentication=Active Directory Managed Identity;Encrypt=True;Connect Timeout=120;"
EXISTE_JWT=$(az webapp config appsettings list -g "$RG" -n "$APP" --query "[?name=='Jwt__Key'] | length(@)" -o tsv)
az webapp config appsettings set -g "$RG" -n "$APP" -o none --settings \
  "ConnectionStrings__PoliAppDB=$CS" "SembrarDatosDemo=true" "TZ=America/Lima" \
  "ASPNETCORE_ENVIRONMENT=Production" "Cors__Origenes__0=https://$APP.azurewebsites.net"
if [[ "$EXISTE_JWT" == "0" ]]; then
  az webapp config appsettings set -g "$RG" -n "$APP" -o none --settings "Jwt__Key=$(openssl rand -base64 48)"
fi

paso "Compilación (.NET 10)"
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet10" >/dev/null
  export PATH="$HOME/.dotnet10:$PATH" DOTNET_ROOT="$HOME/.dotnet10"
fi
rm -rf /tmp/poliapp-pub /tmp/poliapp.zip
dotnet publish "$RAIZ/src/PoliApp.Api/PoliApp.Api.csproj" -c Release -o /tmp/poliapp-pub --nologo -v q
(cd /tmp/poliapp-pub && zip -qr /tmp/poliapp.zip .)

paso "Despliegue en $APP"
az webapp deploy -g "$RG" -n "$APP" --src-path /tmp/poliapp.zip --type zip -o none

echo
echo "Listo: https://$APP.azurewebsites.net"
