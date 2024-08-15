{{/*
Expand the name of the chart.
*/}}
{{- define "prdeploy-app.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "prdeploy-app.fullname" -}}
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
{{- define "prdeploy-app.image" -}}
{{- if .Values.global.image.registry }}
{{- .Values.global.image.registry }}/
{{- end }}
{{- .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "prdeploy-app.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "prdeploy-app.labels" -}}
helm.sh/chart: {{ include "prdeploy-app.chart" . }}
{{ include "prdeploy-app.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "prdeploy-app.selectorLabels" -}}
app.kubernetes.io/name: {{ include "prdeploy-app.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "prdeploy-app.serviceAccountName" -}}
{{- if .Values.global.serviceAccounts.frontend.create }}
{{- default (include "prdeploy.fullname" .) .Values.global.serviceAccounts.frontend.name }}
{{- else }}
{{- default "default" .Values.global.serviceAccounts.frontend.name }}
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
