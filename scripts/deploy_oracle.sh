#!/usr/bin/env bash
set -euo pipefail

: "${GHCR_IMAGE?}"
: "${GHCR_USER?}"
: "${GHCR_TOKEN?}"
: "${SSH_HOST?}"
: "${SSH_USER?}"
: "${SSH_KEY_PATH?}"

REMOTE_DIR=${REMOTE_DIR:-$HOME/linkguardiao}

printf "\nLogging in to GHCR...\n"
docker login ghcr.io -u "$GHCR_USER" -p "$GHCR_TOKEN"

printf "\nBuilding backend image...\n"
docker build -f Dockerfile.backend -t "$GHCR_IMAGE:latest" .

printf "\nPushing image...\n"
docker push "$GHCR_IMAGE:latest"

printf "\nSyncing compose files...\n"
ssh -i "$SSH_KEY_PATH" "$SSH_USER@$SSH_HOST" "mkdir -p $REMOTE_DIR"
scp -i "$SSH_KEY_PATH" docker-compose.prod.yml "$SSH_USER@$SSH_HOST:$REMOTE_DIR/docker-compose.prod.yml"
scp -i "$SSH_KEY_PATH" .env.prod "$SSH_USER@$SSH_HOST:$REMOTE_DIR/.env"

printf "\nDeploying...\n"
ssh -i "$SSH_KEY_PATH" "$SSH_USER@$SSH_HOST" "cd $REMOTE_DIR && docker compose -f docker-compose.prod.yml pull api && docker compose -f docker-compose.prod.yml up -d"
