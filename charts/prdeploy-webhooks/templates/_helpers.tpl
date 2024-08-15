{{/*
Expand the name of the chart.
*/}}
{{- define "prdeploy-webhooks.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "prdeploy-webhooks.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create a default fully qualified root app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "prdeploy.fullname" -}}
{{- if .Values.global.fullnameOverride }}
{{- .Values.global.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.global.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create a fully qualified image name, including the registry if one is set.
*/}}
{{- define "prdeploy-webhooks.image" -}}
{{- if .Values.global.image.registry }}
{{- .Values.global.image.registry }}/
{{- end }}
{{- .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "prdeploy-webhooks.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "prdeploy-webhooks.labels" -}}
helm.sh/chart: {{ include "prdeploy-webhooks.chart" . }}
{{ include "prdeploy-webhooks.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "prdeploy-webhooks.selectorLabels" -}}
app.kubernetes.io/name: {{ include "prdeploy-webhooks.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "prdeploy-webhooks.serviceAccountName" -}}
{{- if .Values.global.serviceAccounts.backend.create }}
{{- default (include "prdeploy.fullname" .) .Values.global.serviceAccounts.backend.name }}
{{- else }}
{{- default "default" .Values.global.serviceAccounts.backend.name }}
{{- end }}
{{- end }}

{{/*
Full URL for prdeploy host.
*/}}
{{- define "prdeploy.url" -}}
{{- if .Values.global.ingress.tls }}
{{- printf "https://%s" .Values.global.ingress.host }}
{{- else }}
{{- printf "http://%s" .Values.global.ingress.host }}
{{- end }}
{{- end }}
