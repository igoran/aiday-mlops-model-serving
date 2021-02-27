#!/bin/bash

. smoke.sh

TIME_OUT=60
TIME_OUT_COUNT=0
PULUMI_CWD="../infra/"
SMOKE_DOMAIN=$(pulumi stack --stack $PULUMI_STACK_NAME --cwd $PULUMI_CWD output StagingEndpoint)
SMOKE_URL="$SMOKE_DOMAIN/api/smoke"

echo "Start smoke test against host $SMOKE_URL"

while true
do
  STATUS=$(curl -s -o /dev/null -w '%{http_code}' $SMOKE_URL)
  if [ $STATUS -eq 200 ]; then
    smoke_url_ok $SMOKE_URL
    smoke_assert_body "Positive-$ML_MODEL_URI"
    smoke_report
    echo 'Smoke Tests Successfully Completed.'
    break
  elif [[ $TIME_OUT_COUNT -gt $TIME_OUT ]]; then
    echo "Process has Timed out! Elapsed Timeout Count.. $TIME_OUT_COUNT"
    exit 1
  else
    echo "Checking Status on host $SMOKE... $TIME_OUT_COUNT seconds elapsed"
    TIME_OUT_COUNT=$((TIME_OUT_COUNT+10))
  fi
  sleep 10
done
