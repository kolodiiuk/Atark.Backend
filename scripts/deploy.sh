#!/bin/bash

set -e

echo "=== Starting minikube ==="
minikube start --cpus=4 --memory=4096

echo "=== Enabling metrics-server (for HPA) ==="
minikube addons enable metrics-server

echo "=== Building Docker image inside minikube ==="
eval "$(minikube docker-env --shell=bash)"
docker build -t spotrent-api:latest .

echo "=== Applying manifests ==="
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/postgres.yaml
kubectl apply -f k8s/api.yaml
kubectl apply -f k8s/hpa.yaml

echo "=== Waiting for postgres ==="
kubectl rollout status statefulset/postgres -n spotrent

echo "=== Waiting for api ==="
kubectl rollout status deployment/spotrent-api -n spotrent

echo "=== Done! ==="
echo "Access: $(minikube service spotrent-api -n spotrent --url)"
