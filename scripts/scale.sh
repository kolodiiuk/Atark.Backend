#!/bin/bash

REPLICAS=${1:-2}
kubectl scale deployment spotrent-api -n spotrent --replicas=$REPLICAS
echo "Scaled to $REPLICAS replicas"
kubectl get pods -n spotrent