#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE DATABASE sentinel;
  CREATE USER sentinel WITH ENCRYPTED PASSWORD 'sentinel_db_password';
  GRANT ALL PRIVILEGES ON DATABASE sentinel TO sentinel;
EOSQL
