#!/bin/bash

locust -f locust/locustfile.py --host=http://192.168.49.2:30080 -u 100 -r 5 -t 1m --headless
echo 'warm up'

locust -f locust/locustfile.py \
  --host http://192.168.49.2:30080 \
  --users 200 \
  --spawn-rate 5 \
  --run-time 5m \
  --headless \
  --html report-1r-light.html
echo '==================================================================================light'

locust -f locust/locustfile.py \
  --host http://192.168.49.2:30080 \
  --users 400 \
  --spawn-rate 10 \
  --run-time 7m \
  --headless \
  --html report-1r-medium.html
echo '==================================================================================medium'

locust -f locust/locustfile.py \
  --host http://192.168.49.2:30080 \
  --users 700 \
  --spawn-rate 10 \
  --run-time 10m \
  --headless \
  --html report-1r-high.html
echo '====================================================================================high'

locust -f locust/locustfile.py \
  --host http://192.168.49.2:30080 \
  --users 1000 \
  --spawn-rate 20 \
  --run-time 10m \
  --headless \
  --html report-1r-stress.html
echo '====================================================================================stress'
