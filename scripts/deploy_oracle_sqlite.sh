#!/usr/bin/env bash
set -euo pipefail

: "${SSH_HOST?}"
: "${SSH_USER?}"
: "${SSH_KEY_PATH?}"

REMOTE_DIR=${REMOTE_DIR:-/home/$SSH_USER/linkguardiao}
REMOTE_APP_DIR=${REMOTE_APP_DIR:-/opt/linkguardiao}
REMOTE_DATA_DIR=${REMOTE_DATA_DIR:-/var/lib/linkguardiao}
PUBLISH_DIR=${PUBLISH_DIR:-./artifacts/publish}

printf "\nBuilding backend...\n"
dotnet publish src/LinkGuardiao.Api -c Release -o "$PUBLISH_DIR"

printf "\nSyncing files to server...\n"
ssh -i "$SSH_KEY_PATH" "$SSH_USER@$SSH_HOST" "mkdir -p $REMOTE_DIR"
scp -i "$SSH_KEY_PATH" -r "$PUBLISH_DIR" "$SSH_USER@$SSH_HOST:$REMOTE_DIR/publish"
scp -i "$SSH_KEY_PATH" .env.prod "$SSH_USER@$SSH_HOST:$REMOTE_DIR/.env.prod"
scp -i "$SSH_KEY_PATH" systemd/linkguardiao-api.service "$SSH_USER@$SSH_HOST:$REMOTE_DIR/linkguardiao-api.service"
scp -i "$SSH_KEY_PATH" systemd/cloudflared.service "$SSH_USER@$SSH_HOST:$REMOTE_DIR/cloudflared.service"

printf "\nDeploying on server...\n"
ssh -i "$SSH_KEY_PATH" "$SSH_USER@$SSH_HOST" "
  set -e
  sudo useradd --system --no-create-home --shell /usr/sbin/nologin linkguardiao 2>/dev/null || true
  sudo mkdir -p $REMOTE_APP_DIR $REMOTE_DATA_DIR /etc/linkguardiao
  if [ -d $REMOTE_APP_DIR ]; then
    sudo tar -czf $REMOTE_DIR/backup-$(date +%Y%m%d%H%M%S).tgz -C $REMOTE_APP_DIR . || true
  fi
  sudo cp -a $REMOTE_DIR/publish/. $REMOTE_APP_DIR/
  sudo install -m 600 $REMOTE_DIR/.env.prod /etc/linkguardiao/env
  sudo install -m 644 $REMOTE_DIR/linkguardiao-api.service /etc/systemd/system/linkguardiao-api.service
  sudo install -m 644 $REMOTE_DIR/cloudflared.service /etc/systemd/system/cloudflared.service
  sudo chown -R linkguardiao:linkguardiao $REMOTE_APP_DIR $REMOTE_DATA_DIR
  sudo systemctl daemon-reload
  sudo systemctl enable linkguardiao-api cloudflared
  sudo systemctl restart linkguardiao-api cloudflared
"

printf "\nDone.\n"
