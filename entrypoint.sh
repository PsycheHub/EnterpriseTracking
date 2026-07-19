#!/bin/bash

# Export environment variables from Render's mounted secret files
declare -A secrets=(
  ["ConnectionStrings__ProdDb"]="/etc/secrets/ConnectionStrings__ProdDb"
)

for key in "${!secrets[@]}"; do
  if [ -f "${secrets[$key]}" ]; then
    export "$key"="$(cat "${secrets[$key]}")"
  fi
done

# Start the app
exec dotnet EnterpriseTracking.Api.dll
