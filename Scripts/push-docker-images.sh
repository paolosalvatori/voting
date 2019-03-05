#!/bin/bash

# Variables
acrName=paolosalvatori
acrResourceGroug=ContainerRegistryResourceGroup

# Login to ACR
az acr login --name $acrName 

# Retrieve ACR login server. Each container image needs to be tagged with the loginServer name of the registry. 
loginServer=$(az acr show --name $acrName --query loginServer --output tsv)

# Tag the local votingdata:v1 image with the loginServer of ACR
docker tag votingdata:latest $loginServer/votingdata:v1

# Push votingdata:latest container image to ACR
docker push $loginServer/votingdata:v1

# Tag the local votingweb:latest image with the loginServer of ACR
docker tag votingweb:latest $loginServer/votingweb:v1

# Push votingweb:latest container image to ACR
docker push $loginServer/votingweb:v1

# List images in ACR
# az acr repository list --name $acrName --output table
# az acr repository list --name paolosalvatori --output tsv | xargs -I [] sh -c 'az acr repository show-tags --name paolosalvatori --repository []'