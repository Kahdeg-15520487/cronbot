#!/bin/sh
# Gitea auto-init script - creates admin user on first startup
# This script runs before Gitea starts

set -e

# Function to create admin user
create_admin_user() {
    echo "Waiting for Gitea to be ready..."
    until curl -s http://localhost:3000/api/v1/version > /dev/null 2>&1; do
        sleep 1
    done
    sleep 2  # Extra time for full initialization

    echo "Checking if admin user exists..."
    if curl -s -u cronbot:cronbot123 http://localhost:3000/api/v1/user > /dev/null 2>&1; then
        echo "Admin user already exists"
        return 0
    fi

    echo "Creating admin user..."
    su git -c "gitea admin user create --username cronbot --password cronbot123 --email admin@cronbot.local --must-change-password=false --admin" 2>&1 || {
        echo "User might already exist, checking..."
        if curl -s -u cronbot:cronbot123 http://localhost:3000/api/v1/user > /dev/null 2>&1; then
            echo "Admin user verified"
            return 0
        fi
        return 1
    }

    echo "Admin user created successfully"
}

# If this is the first argument (entrypoint mode), start Gitea in background, init, then bring to foreground
if [ "$1" = "/usr/bin/entrypoint" ]; then
    # Start Gitea in background
    /usr/bin/entrypoint /bin/s6-svscan /etc/s6 &

    # Wait and create admin
    create_admin_user

    # Keep the container running (Gitea is already in background)
    wait
else
    # Just exec the normal entrypoint
    exec /usr/bin/entrypoint "$@"
fi
