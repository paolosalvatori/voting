#!/bin/bash

# Variables
serviceName=voting-data
serviceNamespace=default
serviceIP=$(kubectl get service $serviceName -n $serviceNamespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}') 
url=http://$serviceIP/swagger/v1/swagger.json

# The following command does three things:
# 1. Fetch the swagger specification for webapp.
# 2. Take the spec and convert it into a service profile by using the profile command.
# 3. Apply this configuration to the cluster.
# You can edit the service profile with the following command:
# kubectl -n default edit sp/voting-data.default.svc.cluster.local
curl -sL $url | linkerd -n $serviceNamespace profile --open-api - $serviceName | kubectl -n $serviceNamespace apply -f -

# Display the service profile in YAML format
kubectl get serviceprofile.linkerd.io/$serviceName.$serviceNamespace.svc.cluster.local -o yaml

# See routes
linkerd -n $serviceNamespace routes svc/$serviceName 