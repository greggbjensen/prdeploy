import { gql } from 'apollo-angular';
import { Injectable } from '@angular/core';
import * as Apollo from 'apollo-angular';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  Any: { input: any; output: any; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: any; output: any; }
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: { input: any; output: any; }
};

export enum ApplyPolicy {
  AfterResolver = 'AFTER_RESOLVER',
  BeforeResolver = 'BEFORE_RESOLVER',
  Validation = 'VALIDATION'
}

export type AutomationTestSettings = {
  __typename?: 'AutomationTestSettings';
  enabled?: Maybe<Scalars['Boolean']['output']>;
  inputs?: Maybe<Scalars['Any']['output']>;
  workflow?: Maybe<Scalars['String']['output']>;
};

export type AutomationTestSettingsInput = {
  enabled?: InputMaybe<Scalars['Boolean']['input']>;
  inputs?: InputMaybe<Scalars['Any']['input']>;
  workflow?: InputMaybe<Scalars['String']['input']>;
};

export type BadgeSettings = {
  __typename?: 'BadgeSettings';
  statusColors?: Maybe<BadgeStatusColorsSettings>;
};

export type BadgeSettingsCompare = {
  __typename?: 'BadgeSettingsCompare';
  statusColors: BadgeStatusColorsSettingsCompare;
};

export type BadgeSettingsInput = {
  statusColors?: InputMaybe<BadgeStatusColorsSettingsInput>;
};

export type BadgeStatusColorsSettings = {
  __typename?: 'BadgeStatusColorsSettings';
  error?: Maybe<Scalars['String']['output']>;
  info?: Maybe<Scalars['String']['output']>;
  success?: Maybe<Scalars['String']['output']>;
  warn?: Maybe<Scalars['String']['output']>;
};

export type BadgeStatusColorsSettingsCompare = {
  __typename?: 'BadgeStatusColorsSettingsCompare';
  error: OwnerRepoValueOfString;
  info: OwnerRepoValueOfString;
  success: OwnerRepoValueOfString;
  warn: OwnerRepoValueOfString;
};

export type BadgeStatusColorsSettingsInput = {
  error?: InputMaybe<Scalars['String']['input']>;
  info?: InputMaybe<Scalars['String']['input']>;
  success?: InputMaybe<Scalars['String']['input']>;
  warn?: InputMaybe<Scalars['String']['input']>;
};

export type BuildsSettings = {
  __typename?: 'BuildsSettings';
  checkPattern?: Maybe<Scalars['String']['output']>;
  workflowPattern?: Maybe<Scalars['String']['output']>;
};

export type BuildsSettingsCompare = {
  __typename?: 'BuildsSettingsCompare';
  checkPattern: OwnerRepoValueOfString;
  workflowPattern: OwnerRepoValueOfString;
};

export type BuildsSettingsInput = {
  checkPattern?: InputMaybe<Scalars['String']['input']>;
  workflowPattern?: InputMaybe<Scalars['String']['input']>;
};

/** Environment and current pull request information. */
export type DeployEnvironment = {
  __typename?: 'DeployEnvironment';
  /** The color used for the icon representing the environment. */
  color?: Maybe<Scalars['String']['output']>;
  /** A value indicating if the current environment is reserved for deploy. */
  locked: Scalars['Boolean']['output'];
  /** Environment that pull request is deployed to. */
  name?: Maybe<Scalars['ID']['output']>;
  /** Current pull request in environment. */
  pullRequest?: Maybe<PullRequest>;
  /** The full URL of the environment. */
  url?: Maybe<Scalars['String']['output']>;
};

export type DeployMutation = {
  __typename?: 'DeployMutation';
  deployEnvironmentDeploy: StatusResponse;
  deployEnvironmentFree: StatusResponse;
  deployEnvironmentRollback: StatusResponse;
  deployQueueUpdate: DeployQueue;
  ownerRepoAddEnabled: StatusResponse;
  ownerRepoRemoveEnabled: StatusResponse;
  ownerSettingsSet: StatusResponse;
  pullRequestAddServices: StatusResponse;
  repoSettingsSet: StatusResponse;
};


export type DeployMutationDeployEnvironmentDeployArgs = {
  input: EnvironmentDeployInput;
};


export type DeployMutationDeployEnvironmentFreeArgs = {
  input: PullDeployInput;
};


export type DeployMutationDeployEnvironmentRollbackArgs = {
  input: RollbackInput;
};


export type DeployMutationDeployQueueUpdateArgs = {
  input: DeployQueueUpdateInput;
};


export type DeployMutationOwnerRepoAddEnabledArgs = {
  input: RepositoryInput;
};


export type DeployMutationOwnerRepoRemoveEnabledArgs = {
  input: RepositoryInput;
};


export type DeployMutationOwnerSettingsSetArgs = {
  input: SetOwnerSettingsInput;
};


export type DeployMutationPullRequestAddServicesArgs = {
  input: PullRequestAddServicesInput;
};


export type DeployMutationRepoSettingsSetArgs = {
  input: SetRepoSettingsInput;
};

export type DeployQuery = {
  __typename?: 'DeployQuery';
  deployEnvironments: Array<DeployEnvironment>;
  deployQueues: Array<DeployQueue>;
  deploySettingsCompare: DeploySettingsCompare;
  deployStateComparison: DeployStateComparison;
  enabledOwnerRepos: Array<OwnerRepos>;
  environments: Array<Environment>;
  openPullRequests: Array<PullRequest>;
  ownerSettings: DeploySettings;
  repoSettings: DeploySettings;
  repositoryServices: Array<Scalars['String']['output']>;
};


export type DeployQueryDeployEnvironmentsArgs = {
  input: RepoQueryInput;
};


export type DeployQueryDeployQueuesArgs = {
  input: RepoQueryInput;
};


export type DeployQueryDeploySettingsCompareArgs = {
  input: RepoQueryInput;
};


export type DeployQueryDeployStateComparisonArgs = {
  input: DeployStateComparisonInput;
};


export type DeployQueryEnvironmentsArgs = {
  input: RepoQueryInput;
};


export type DeployQueryOpenPullRequestsArgs = {
  input: OpenPullRequestInput;
};


export type DeployQueryOwnerSettingsArgs = {
  input: OwnerQueryInput;
};


export type DeployQueryRepoSettingsArgs = {
  input: RepoQueryInput;
};


export type DeployQueryRepositoryServicesArgs = {
  input: RepoQueryInput;
};

/** Queue for a specific environment of pull requests waiting to be deployed. */
export type DeployQueue = {
  __typename?: 'DeployQueue';
  /** Environment to list the queue for. */
  environment?: Maybe<Scalars['ID']['output']>;
  /** Ordered list of pull requests waiting in queue. */
  pullRequests: Array<PullRequest>;
};

export type DeployQueueUpdateInput = {
  environment: Scalars['ID']['input'];
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  pullNumbers?: InputMaybe<Array<Scalars['Int']['input']>>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
};

export type DeploySettings = {
  __typename?: 'DeploySettings';
  badge?: Maybe<BadgeSettings>;
  builds?: Maybe<BuildsSettings>;
  defaultBranch?: Maybe<Scalars['String']['output']>;
  defaultEnvironment?: Maybe<Scalars['String']['output']>;
  deployManagerSiteUrl?: Maybe<Scalars['String']['output']>;
  deployWorkflow?: Maybe<Scalars['String']['output']>;
  environments?: Maybe<Array<EnvironmentSettings>>;
  jira?: Maybe<JiraSettings>;
  owner?: Maybe<Scalars['String']['output']>;
  releaseEnvironment?: Maybe<Scalars['String']['output']>;
  repo?: Maybe<Scalars['String']['output']>;
  services?: Maybe<Array<ServiceSettings>>;
  settingsBranch?: Maybe<Scalars['String']['output']>;
  slack?: Maybe<SlackSettings>;
};

export type DeploySettingsCompare = {
  __typename?: 'DeploySettingsCompare';
  badge: BadgeSettingsCompare;
  builds: BuildsSettingsCompare;
  defaultBranch: OwnerRepoValueOfString;
  defaultEnvironment: OwnerRepoValueOfString;
  deployManagerSiteUrl: OwnerRepoValueOfString;
  deployWorkflow: OwnerRepoValueOfString;
  environments: OwnerRepoValueOfListOfEnvironmentSettings;
  jira: JiraSettingsCompare;
  releaseEnvironment: OwnerRepoValueOfString;
  services: OwnerRepoValueOfListOfServiceSettings;
  settingsBranch: OwnerRepoValueOfString;
  slack: SlackSettingsCompare;
};

export type DeploySettingsInput = {
  badge?: InputMaybe<BadgeSettingsInput>;
  builds?: InputMaybe<BuildsSettingsInput>;
  defaultBranch?: InputMaybe<Scalars['String']['input']>;
  defaultEnvironment?: InputMaybe<Scalars['String']['input']>;
  deployManagerSiteUrl?: InputMaybe<Scalars['String']['input']>;
  deployWorkflow?: InputMaybe<Scalars['String']['input']>;
  environments?: InputMaybe<Array<EnvironmentSettingsInput>>;
  jira?: InputMaybe<JiraSettingsInput>;
  owner?: InputMaybe<Scalars['String']['input']>;
  releaseEnvironment?: InputMaybe<Scalars['String']['input']>;
  repo?: InputMaybe<Scalars['String']['input']>;
  services?: InputMaybe<Array<ServiceSettingsInput>>;
  settingsBranch?: InputMaybe<Scalars['String']['input']>;
  slack?: InputMaybe<SlackSettingsInput>;
};

export type DeployStateComparison = {
  __typename?: 'DeployStateComparison';
  /** List of service comparisons. */
  serviceComparisons: Array<ServiceComparison>;
  /** Source environment to compare. */
  sourceEnvironment?: Maybe<Scalars['ID']['output']>;
  /** Source environment current pull request number. */
  sourcePullNumber: Scalars['Int']['output'];
  /** Target environment to compare to, Prod unless source is Prod, then Stable. */
  targetEnvironment?: Maybe<Scalars['ID']['output']>;
  /** Target environment current pull request number. */
  targetPullNumber: Scalars['Int']['output'];
};

/** Input for retrieving the deploy state comparison. */
export type DeployStateComparisonInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
  /** Environment to compare from. */
  sourceEnvironment?: InputMaybe<Scalars['ID']['input']>;
  /** Environment to compare to. */
  targetEnvironment?: InputMaybe<Scalars['ID']['input']>;
};

export type DeployUser = {
  __typename?: 'DeployUser';
  name?: Maybe<Scalars['String']['output']>;
  username?: Maybe<Scalars['String']['output']>;
};

/** Deployment environment such as Dev, Stage, and Prod. */
export type Environment = {
  __typename?: 'Environment';
  /** Readable name of the environment. */
  name?: Maybe<Scalars['ID']['output']>;
  /** Full URL of the environment. */
  url?: Maybe<Scalars['String']['output']>;
};

export type EnvironmentDeployInput = {
  environment: Scalars['ID']['input'];
  force?: InputMaybe<Scalars['Boolean']['input']>;
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  pullNumber: Scalars['Int']['input'];
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
  retain?: InputMaybe<Scalars['Boolean']['input']>;
};

export type EnvironmentSettings = {
  __typename?: 'EnvironmentSettings';
  automationTest?: Maybe<AutomationTestSettings>;
  color?: Maybe<Scalars['String']['output']>;
  excludeFromRollback?: Maybe<Array<Scalars['String']['output']>>;
  name: Scalars['String']['output'];
  queue: Scalars['String']['output'];
  requireApproval: Scalars['Boolean']['output'];
  requireBranchUpToDate: Scalars['Boolean']['output'];
  url?: Maybe<Scalars['String']['output']>;
};

export type EnvironmentSettingsInput = {
  automationTest?: InputMaybe<AutomationTestSettingsInput>;
  color?: InputMaybe<Scalars['String']['input']>;
  excludeFromRollback?: InputMaybe<Array<Scalars['String']['input']>>;
  name: Scalars['String']['input'];
  queue: Scalars['String']['input'];
  requireApproval: Scalars['Boolean']['input'];
  requireBranchUpToDate: Scalars['Boolean']['input'];
  url?: InputMaybe<Scalars['String']['input']>;
};

export type JiraSettings = {
  __typename?: 'JiraSettings';
  addIssuesEnabled?: Maybe<Scalars['Boolean']['output']>;
  host?: Maybe<Scalars['String']['output']>;
  password?: Maybe<Scalars['String']['output']>;
  username?: Maybe<Scalars['String']['output']>;
};

export type JiraSettingsCompare = {
  __typename?: 'JiraSettingsCompare';
  addIssuesEnabled: OwnerRepoValueOfNullableOfBoolean;
  host: OwnerRepoValueOfString;
  password: OwnerRepoValueOfString;
  username: OwnerRepoValueOfString;
};

export type JiraSettingsInput = {
  addIssuesEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  host?: InputMaybe<Scalars['String']['input']>;
  password?: InputMaybe<Scalars['String']['input']>;
  username?: InputMaybe<Scalars['String']['input']>;
};

export type OpenPullRequestInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
  search?: InputMaybe<Scalars['String']['input']>;
};

/** Input for a general owner query. */
export type OwnerQueryInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
};

export type OwnerRepoDictionary = {
  __typename?: 'OwnerRepoDictionary';
  owner?: Maybe<Scalars['Any']['output']>;
  repo?: Maybe<Scalars['Any']['output']>;
};

export type OwnerRepoValueOfListOfEnvironmentSettings = {
  __typename?: 'OwnerRepoValueOfListOfEnvironmentSettings';
  owner?: Maybe<Array<Maybe<EnvironmentSettings>>>;
  repo?: Maybe<Array<Maybe<EnvironmentSettings>>>;
};

export type OwnerRepoValueOfListOfServiceSettings = {
  __typename?: 'OwnerRepoValueOfListOfServiceSettings';
  owner?: Maybe<Array<Maybe<ServiceSettings>>>;
  repo?: Maybe<Array<Maybe<ServiceSettings>>>;
};

export type OwnerRepoValueOfNullableOfBoolean = {
  __typename?: 'OwnerRepoValueOfNullableOfBoolean';
  owner?: Maybe<Scalars['Boolean']['output']>;
  repo?: Maybe<Scalars['Boolean']['output']>;
};

export type OwnerRepoValueOfString = {
  __typename?: 'OwnerRepoValueOfString';
  owner?: Maybe<Scalars['String']['output']>;
  repo?: Maybe<Scalars['String']['output']>;
};

/** Set of owner and associated repos. */
export type OwnerRepos = {
  __typename?: 'OwnerRepos';
  /** Owner or organization of a set of repo. */
  owner: Scalars['String']['output'];
  /** List of repository names for owner. */
  repos: Array<Scalars['String']['output']>;
};

export type PullDeployInput = {
  environment: Scalars['ID']['input'];
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  pullNumber: Scalars['Int']['input'];
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
};

/** Pull request to deploy and merge code. */
export type PullRequest = {
  __typename?: 'PullRequest';
  /** Pull request body as markdown. */
  body?: Maybe<Scalars['String']['output']>;
  /** Pull request number. */
  number: Scalars['Int']['output'];
  /** Pull request title. */
  title?: Maybe<Scalars['String']['output']>;
  /** The date and time the pull request deployment was last updated. */
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  /** URL for displaying the pull request HTML. */
  url?: Maybe<Scalars['String']['output']>;
  /** Login for user the pull request was created by. */
  user?: Maybe<DeployUser>;
};

export type PullRequestAddServicesInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  pullNumber: Scalars['Int']['input'];
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
  services: Array<Scalars['String']['input']>;
};

/** Input for a general owner and repo query. */
export type RepoQueryInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
};

/** Owner and repo name of a repository. */
export type RepositoryInput = {
  /** Owner or organization of a repo. */
  owner: Scalars['String']['input'];
  /** Name of the repository. */
  repo: Scalars['String']['input'];
};

export type RollbackInput = {
  count: Scalars['Int']['input'];
  environment: Scalars['ID']['input'];
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  pullNumber: Scalars['Int']['input'];
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
};

/** Comparison of services between a source and target environment. */
export type ServiceComparison = {
  __typename?: 'ServiceComparison';
  /** Name of the service deployed. */
  name?: Maybe<Scalars['ID']['output']>;
  /** Workflow run ID of the source service. */
  sourceRunId: Scalars['Long']['output'];
  /** Version of the source service. */
  sourceVersion: Scalars['String']['output'];
  /** Workflow run ID of the target service. */
  targetRunId: Scalars['Long']['output'];
  /** Version of the target service. */
  targetVersion: Scalars['String']['output'];
};

export type ServiceSettings = {
  __typename?: 'ServiceSettings';
  name?: Maybe<Scalars['String']['output']>;
  path?: Maybe<Scalars['String']['output']>;
};

export type ServiceSettingsInput = {
  name?: InputMaybe<Scalars['String']['input']>;
  path?: InputMaybe<Scalars['String']['input']>;
};

/** Input for updating repo settings. */
export type SetOwnerSettingsInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Full settings for owner that defaults repo settings. */
  settings?: InputMaybe<DeploySettingsInput>;
};

/** Input for updating repo settings. */
export type SetRepoSettingsInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
  /** Full settings for owner and repository. */
  settings?: InputMaybe<DeploySettingsInput>;
};

export type SlackSettings = {
  __typename?: 'SlackSettings';
  emailAliases?: Maybe<Scalars['Any']['output']>;
  emailDomain?: Maybe<Scalars['String']['output']>;
  notificationsEnabled?: Maybe<Scalars['Boolean']['output']>;
  token?: Maybe<Scalars['String']['output']>;
  webhooks?: Maybe<SlackWebHooksSettings>;
};

export type SlackSettingsCompare = {
  __typename?: 'SlackSettingsCompare';
  emailAliases: OwnerRepoDictionary;
  emailDomain: OwnerRepoValueOfString;
  notificationsEnabled: OwnerRepoValueOfNullableOfBoolean;
  token: OwnerRepoValueOfString;
  webhooks: SlackWebHooksSettingsCompare;
};

export type SlackSettingsInput = {
  emailAliases?: InputMaybe<Scalars['Any']['input']>;
  emailDomain?: InputMaybe<Scalars['String']['input']>;
  notificationsEnabled?: InputMaybe<Scalars['Boolean']['input']>;
  token?: InputMaybe<Scalars['String']['input']>;
  webhooks?: InputMaybe<SlackWebHooksSettingsInput>;
};

export type SlackWebHooksSettings = {
  __typename?: 'SlackWebHooksSettings';
  deployUrl?: Maybe<Scalars['String']['output']>;
  releaseUrl?: Maybe<Scalars['String']['output']>;
};

export type SlackWebHooksSettingsCompare = {
  __typename?: 'SlackWebHooksSettingsCompare';
  deployUrl: OwnerRepoValueOfString;
  releaseUrl: OwnerRepoValueOfString;
};

export type SlackWebHooksSettingsInput = {
  deployUrl?: InputMaybe<Scalars['String']['input']>;
  releaseUrl?: InputMaybe<Scalars['String']['input']>;
};

/** Simple status response from a mutation. */
export type StatusResponse = {
  __typename?: 'StatusResponse';
  /** True if the mutation was successful; otherwise false. */
  success: Scalars['Boolean']['output'];
};

export type DeployEnvironmentDeployMutationVariables = Exact<{
  input: EnvironmentDeployInput;
}>;


export type DeployEnvironmentDeployMutation = { __typename?: 'DeployMutation', deployEnvironmentDeploy: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentFreeMutationVariables = Exact<{
  input: PullDeployInput;
}>;


export type DeployEnvironmentFreeMutation = { __typename?: 'DeployMutation', deployEnvironmentFree: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentRollbackMutationVariables = Exact<{
  input: RollbackInput;
}>;


export type DeployEnvironmentRollbackMutation = { __typename?: 'DeployMutation', deployEnvironmentRollback: { __typename?: 'StatusResponse', success: boolean } };

export type DeployQueueUpdateMutationVariables = Exact<{
  input: DeployQueueUpdateInput;
}>;


export type DeployQueueUpdateMutation = { __typename?: 'DeployMutation', deployQueueUpdate: { __typename?: 'DeployQueue', environment?: string | null } };

export type OwnerRepoAddEnabledMutationVariables = Exact<{
  input: RepositoryInput;
}>;


export type OwnerRepoAddEnabledMutation = { __typename?: 'DeployMutation', ownerRepoAddEnabled: { __typename?: 'StatusResponse', success: boolean } };

export type OwnerRepoRemoveEnabledMutationVariables = Exact<{
  input: RepositoryInput;
}>;


export type OwnerRepoRemoveEnabledMutation = { __typename?: 'DeployMutation', ownerRepoRemoveEnabled: { __typename?: 'StatusResponse', success: boolean } };

export type OwnerSettingsSetMutationVariables = Exact<{
  input: SetOwnerSettingsInput;
}>;


export type OwnerSettingsSetMutation = { __typename?: 'DeployMutation', ownerSettingsSet: { __typename?: 'StatusResponse', success: boolean } };

export type PullRequestAddServicesMutationVariables = Exact<{
  input: PullRequestAddServicesInput;
}>;


export type PullRequestAddServicesMutation = { __typename?: 'DeployMutation', pullRequestAddServices: { __typename?: 'StatusResponse', success: boolean } };

export type RepoSettingsSetMutationVariables = Exact<{
  input: SetRepoSettingsInput;
}>;


export type RepoSettingsSetMutation = { __typename?: 'DeployMutation', repoSettingsSet: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentsAndQueuesQueryVariables = Exact<{
  input: RepoQueryInput;
}>;


export type DeployEnvironmentsAndQueuesQuery = { __typename?: 'DeployQuery', deployEnvironments: Array<{ __typename?: 'DeployEnvironment', name?: string | null, url?: string | null, color?: string | null, locked: boolean, pullRequest?: { __typename?: 'PullRequest', number: number, title?: string | null, body?: string | null, url?: string | null, updatedAt?: any | null, user?: { __typename?: 'DeployUser', name?: string | null, username?: string | null } | null } | null }>, deployQueues: Array<{ __typename?: 'DeployQueue', environment?: string | null, pullRequests: Array<{ __typename?: 'PullRequest', number: number, title?: string | null, body?: string | null, url?: string | null, updatedAt?: any | null, user?: { __typename?: 'DeployUser', name?: string | null, username?: string | null } | null }> }> };

export type DeploySettingsCompareQueryVariables = Exact<{
  input: RepoQueryInput;
}>;


export type DeploySettingsCompareQuery = { __typename?: 'DeployQuery', deploySettingsCompare: { __typename?: 'DeploySettingsCompare', deployWorkflow: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, environments: { __typename?: 'OwnerRepoValueOfListOfEnvironmentSettings', owner?: Array<{ __typename?: 'EnvironmentSettings', name: string, queue: string, color?: string | null, url?: string | null, requireApproval: boolean, requireBranchUpToDate: boolean, excludeFromRollback?: Array<string> | null, automationTest?: { __typename?: 'AutomationTestSettings', enabled?: boolean | null, workflow?: string | null, inputs?: any | null } | null } | null> | null, repo?: Array<{ __typename?: 'EnvironmentSettings', name: string, queue: string, color?: string | null, url?: string | null, requireApproval: boolean, requireBranchUpToDate: boolean, excludeFromRollback?: Array<string> | null, automationTest?: { __typename?: 'AutomationTestSettings', enabled?: boolean | null, workflow?: string | null, inputs?: any | null } | null } | null> | null }, services: { __typename?: 'OwnerRepoValueOfListOfServiceSettings', owner?: Array<{ __typename?: 'ServiceSettings', name?: string | null, path?: string | null } | null> | null, repo?: Array<{ __typename?: 'ServiceSettings', name?: string | null, path?: string | null } | null> | null }, defaultEnvironment: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, releaseEnvironment: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, defaultBranch: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, settingsBranch: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, jira: { __typename?: 'JiraSettingsCompare', addIssuesEnabled: { __typename?: 'OwnerRepoValueOfNullableOfBoolean', owner?: boolean | null, repo?: boolean | null }, host: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, username: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, password: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null } }, builds: { __typename?: 'BuildsSettingsCompare', checkPattern: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, workflowPattern: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null } }, slack: { __typename?: 'SlackSettingsCompare', token: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, emailDomain: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, emailAliases: { __typename?: 'OwnerRepoDictionary', owner?: any | null, repo?: any | null }, webhooks: { __typename?: 'SlackWebHooksSettingsCompare', deployUrl: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, releaseUrl: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null } }, notificationsEnabled: { __typename?: 'OwnerRepoValueOfNullableOfBoolean', owner?: boolean | null, repo?: boolean | null } }, badge: { __typename?: 'BadgeSettingsCompare', statusColors: { __typename?: 'BadgeStatusColorsSettingsCompare', error: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, warn: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, success: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null }, info: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null } } }, deployManagerSiteUrl: { __typename?: 'OwnerRepoValueOfString', owner?: string | null, repo?: string | null } } };

export type DeployStateComparisonQueryVariables = Exact<{
  input: DeployStateComparisonInput;
}>;


export type DeployStateComparisonQuery = { __typename?: 'DeployQuery', deployStateComparison: { __typename?: 'DeployStateComparison', sourceEnvironment?: string | null, sourcePullNumber: number, targetEnvironment?: string | null, targetPullNumber: number, serviceComparisons: Array<{ __typename?: 'ServiceComparison', name?: string | null, sourceRunId: any, sourceVersion: string, targetRunId: any, targetVersion: string }> } };

export type EnabledOwnerReposQueryVariables = Exact<{ [key: string]: never; }>;


export type EnabledOwnerReposQuery = { __typename?: 'DeployQuery', enabledOwnerRepos: Array<{ __typename?: 'OwnerRepos', owner: string, repos: Array<string> }> };

export type EnvironmentsQueryVariables = Exact<{
  input: RepoQueryInput;
}>;


export type EnvironmentsQuery = { __typename?: 'DeployQuery', environments: Array<{ __typename?: 'Environment', name?: string | null, url?: string | null }> };

export type OpenPullRequestsQueryVariables = Exact<{
  input: OpenPullRequestInput;
}>;


export type OpenPullRequestsQuery = { __typename?: 'DeployQuery', openPullRequests: Array<{ __typename?: 'PullRequest', number: number, title?: string | null, user?: { __typename?: 'DeployUser', name?: string | null } | null }> };

export type RepositoryServicesQueryVariables = Exact<{
  input: RepoQueryInput;
}>;


export type RepositoryServicesQuery = { __typename?: 'DeployQuery', repositoryServices: Array<string> };

export const DeployEnvironmentDeployDocument = gql`
    mutation DeployEnvironmentDeploy($input: EnvironmentDeployInput!) {
  deployEnvironmentDeploy(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployEnvironmentDeployGQL extends Apollo.Mutation<DeployEnvironmentDeployMutation, DeployEnvironmentDeployMutationVariables> {
    override document = DeployEnvironmentDeployDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeployEnvironmentFreeDocument = gql`
    mutation DeployEnvironmentFree($input: PullDeployInput!) {
  deployEnvironmentFree(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployEnvironmentFreeGQL extends Apollo.Mutation<DeployEnvironmentFreeMutation, DeployEnvironmentFreeMutationVariables> {
    override document = DeployEnvironmentFreeDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeployEnvironmentRollbackDocument = gql`
    mutation DeployEnvironmentRollback($input: RollbackInput!) {
  deployEnvironmentRollback(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployEnvironmentRollbackGQL extends Apollo.Mutation<DeployEnvironmentRollbackMutation, DeployEnvironmentRollbackMutationVariables> {
    override document = DeployEnvironmentRollbackDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeployQueueUpdateDocument = gql`
    mutation DeployQueueUpdate($input: DeployQueueUpdateInput!) {
  deployQueueUpdate(input: $input) {
    environment
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployQueueUpdateGQL extends Apollo.Mutation<DeployQueueUpdateMutation, DeployQueueUpdateMutationVariables> {
    override document = DeployQueueUpdateDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const OwnerRepoAddEnabledDocument = gql`
    mutation OwnerRepoAddEnabled($input: RepositoryInput!) {
  ownerRepoAddEnabled(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class OwnerRepoAddEnabledGQL extends Apollo.Mutation<OwnerRepoAddEnabledMutation, OwnerRepoAddEnabledMutationVariables> {
    override document = OwnerRepoAddEnabledDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const OwnerRepoRemoveEnabledDocument = gql`
    mutation OwnerRepoRemoveEnabled($input: RepositoryInput!) {
  ownerRepoRemoveEnabled(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class OwnerRepoRemoveEnabledGQL extends Apollo.Mutation<OwnerRepoRemoveEnabledMutation, OwnerRepoRemoveEnabledMutationVariables> {
    override document = OwnerRepoRemoveEnabledDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const OwnerSettingsSetDocument = gql`
    mutation OwnerSettingsSet($input: SetOwnerSettingsInput!) {
  ownerSettingsSet(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class OwnerSettingsSetGQL extends Apollo.Mutation<OwnerSettingsSetMutation, OwnerSettingsSetMutationVariables> {
    override document = OwnerSettingsSetDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const PullRequestAddServicesDocument = gql`
    mutation PullRequestAddServices($input: PullRequestAddServicesInput!) {
  pullRequestAddServices(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class PullRequestAddServicesGQL extends Apollo.Mutation<PullRequestAddServicesMutation, PullRequestAddServicesMutationVariables> {
    override document = PullRequestAddServicesDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const RepoSettingsSetDocument = gql`
    mutation RepoSettingsSet($input: SetRepoSettingsInput!) {
  repoSettingsSet(input: $input) {
    success
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class RepoSettingsSetGQL extends Apollo.Mutation<RepoSettingsSetMutation, RepoSettingsSetMutationVariables> {
    override document = RepoSettingsSetDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeployEnvironmentsAndQueuesDocument = gql`
    query DeployEnvironmentsAndQueues($input: RepoQueryInput!) {
  deployEnvironments(input: $input) {
    name
    url
    color
    locked
    pullRequest {
      number
      title
      body
      url
      updatedAt
      user {
        name
        username
      }
    }
  }
  deployQueues(input: $input) {
    environment
    pullRequests {
      number
      title
      body
      url
      updatedAt
      user {
        name
        username
      }
    }
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployEnvironmentsAndQueuesGQL extends Apollo.Query<DeployEnvironmentsAndQueuesQuery, DeployEnvironmentsAndQueuesQueryVariables> {
    override document = DeployEnvironmentsAndQueuesDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeploySettingsCompareDocument = gql`
    query DeploySettingsCompare($input: RepoQueryInput!) {
  deploySettingsCompare(input: $input) {
    deployWorkflow {
      owner
      repo
    }
    environments {
      owner {
        name
        queue
        color
        url
        requireApproval
        requireBranchUpToDate
        automationTest {
          enabled
          workflow
          inputs
        }
        excludeFromRollback
      }
      repo {
        name
        queue
        color
        url
        requireApproval
        requireBranchUpToDate
        automationTest {
          enabled
          workflow
          inputs
        }
        excludeFromRollback
      }
    }
    services {
      owner {
        name
        path
      }
      repo {
        name
        path
      }
    }
    defaultEnvironment {
      owner
      repo
    }
    releaseEnvironment {
      owner
      repo
    }
    defaultBranch {
      owner
      repo
    }
    settingsBranch {
      owner
      repo
    }
    jira {
      addIssuesEnabled {
        owner
        repo
      }
      host {
        owner
        repo
      }
      username {
        owner
        repo
      }
      password {
        owner
        repo
      }
    }
    builds {
      checkPattern {
        owner
        repo
      }
      workflowPattern {
        owner
        repo
      }
    }
    slack {
      token {
        owner
        repo
      }
      emailDomain {
        owner
        repo
      }
      emailAliases {
        owner
        repo
      }
      webhooks {
        deployUrl {
          owner
          repo
        }
        releaseUrl {
          owner
          repo
        }
      }
      notificationsEnabled {
        owner
        repo
      }
    }
    badge {
      statusColors {
        error {
          owner
          repo
        }
        warn {
          owner
          repo
        }
        success {
          owner
          repo
        }
        info {
          owner
          repo
        }
      }
    }
    deployManagerSiteUrl {
      owner
      repo
    }
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeploySettingsCompareGQL extends Apollo.Query<DeploySettingsCompareQuery, DeploySettingsCompareQueryVariables> {
    override document = DeploySettingsCompareDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeployStateComparisonDocument = gql`
    query DeployStateComparison($input: DeployStateComparisonInput!) {
  deployStateComparison(input: $input) {
    sourceEnvironment
    sourcePullNumber
    targetEnvironment
    targetPullNumber
    serviceComparisons {
      name
      sourceRunId
      sourceVersion
      targetRunId
      targetVersion
    }
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeployStateComparisonGQL extends Apollo.Query<DeployStateComparisonQuery, DeployStateComparisonQueryVariables> {
    override document = DeployStateComparisonDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const EnabledOwnerReposDocument = gql`
    query EnabledOwnerRepos {
  enabledOwnerRepos {
    owner
    repos
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class EnabledOwnerReposGQL extends Apollo.Query<EnabledOwnerReposQuery, EnabledOwnerReposQueryVariables> {
    override document = EnabledOwnerReposDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const EnvironmentsDocument = gql`
    query Environments($input: RepoQueryInput!) {
  environments(input: $input) {
    name
    url
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class EnvironmentsGQL extends Apollo.Query<EnvironmentsQuery, EnvironmentsQueryVariables> {
    override document = EnvironmentsDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const OpenPullRequestsDocument = gql`
    query OpenPullRequests($input: OpenPullRequestInput!) {
  openPullRequests(input: $input) {
    number
    title
    user {
      name
    }
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class OpenPullRequestsGQL extends Apollo.Query<OpenPullRequestsQuery, OpenPullRequestsQueryVariables> {
    override document = OpenPullRequestsDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const RepositoryServicesDocument = gql`
    query RepositoryServices($input: RepoQueryInput!) {
  repositoryServices(input: $input)
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class RepositoryServicesGQL extends Apollo.Query<RepositoryServicesQuery, RepositoryServicesQueryVariables> {
    override document = RepositoryServicesDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }