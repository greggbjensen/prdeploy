{{/*
Expand the name of the chart.
*/}}
{{- define "prdeploy.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
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
Create chart name and version as used by the chart label.
*/}}
{{- define "prdeploy.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "prdeploy.labels" -}}
helm.sh/chart: {{ include "prdeploy.chart" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Create the name of the secure service account to use with parameter store access.
*/}}
{{- define "prdeploy.serviceAccounts.backend" -}}
{{- if .Values.global.serviceAccounts.backend.create }}
{{- default (include "prdeploy.fullname" .) .Values.global.serviceAccounts.backend.name }}
{{- else }}
{{- default "default" .Values.global.serviceAccounts.backend.name }}
{{- end }}
{{- end }}

{{/*
Create the name of the limited service account to use for the web app.
*/}}
{{- define "prdeploy.serviceAccounts.frontend" -}}
{{- if .Values.global.serviceAccounts.frontend.create }}
{{- default (include "prdeploy.fullname" .) .Values.global.serviceAccounts.frontend.name }}
{{- else }}
{{- default "default" .Values.global.serviceAccounts.frontend.name }}
{{- end }}
{{- end }}

