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
  pullRequestAddServices: StatusResponse;
};


export type DeployMutationDeployEnvironmentDeployArgs = {
  environment: Scalars['ID']['input'];
  force?: Scalars['Boolean']['input'];
  owner: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  retain?: Scalars['Boolean']['input'];
};


export type DeployMutationDeployEnvironmentFreeArgs = {
  environment: Scalars['ID']['input'];
  owner: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
};


export type DeployMutationDeployEnvironmentRollbackArgs = {
  count?: Scalars['Int']['input'];
  environment: Scalars['ID']['input'];
  owner: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
};


export type DeployMutationDeployQueueUpdateArgs = {
  environment: Scalars['ID']['input'];
  owner: Scalars['ID']['input'];
  pullRequestNumbers?: InputMaybe<Array<Scalars['ID']['input']>>;
  repo: Scalars['ID']['input'];
};


export type DeployMutationPullRequestAddServicesArgs = {
  owner: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  services: Array<Scalars['ID']['input']>;
};

export type DeployQuery = {
  __typename?: 'DeployQuery';
  deployEnvironments: Array<DeployEnvironment>;
  deployQueues: Array<DeployQueue>;
  deployStateComparison: DeployStateComparison;
  environments: Array<Environment>;
  openPullRequests: Array<PullRequest>;
  prDeployEnabledRepositories: Array<Repository>;
  repositoryServices: Array<Scalars['String']['output']>;
};


export type DeployQueryDeployEnvironmentsArgs = {
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
};


export type DeployQueryDeployQueuesArgs = {
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
};


export type DeployQueryDeployStateComparisonArgs = {
  input: DeployStateComparisonInput;
};


export type DeployQueryEnvironmentsArgs = {
  input: EnvironmentsInput;
};


export type DeployQueryOpenPullRequestsArgs = {
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  search?: InputMaybe<Scalars['String']['input']>;
};


export type DeployQueryRepositoryServicesArgs = {
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
};

/** Queue for a specific environment of pull requests waiting to be deployed. */
export type DeployQueue = {
  __typename?: 'DeployQueue';
  /** Environment to list the queue for. */
  environment?: Maybe<Scalars['ID']['output']>;
  /** Ordered list of pull requests waiting in queue. */
  pullRequests: Array<PullRequest>;
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

/** Input for retrieving the environment list. */
export type EnvironmentsInput = {
  /** Repository owner or organization. */
  owner?: InputMaybe<Scalars['ID']['input']>;
  /** Repository being accessed within the owner. */
  repo?: InputMaybe<Scalars['ID']['input']>;
};

/** Pull request to deploy and merge code. */
export type PullRequest = {
  __typename?: 'PullRequest';
  /** Pull request body as markdown. */
  body?: Maybe<Scalars['String']['output']>;
  /** Pull request number. */
  number?: Maybe<Scalars['ID']['output']>;
  /** Pull request title. */
  title?: Maybe<Scalars['String']['output']>;
  /** The date and time the pull request deployment was last updated. */
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  /** URL for displaying the pull request HTML. */
  url?: Maybe<Scalars['String']['output']>;
  /** Login for user the pull request was created by. */
  user?: Maybe<DeployUser>;
};

export type Repository = {
  __typename?: 'Repository';
  owner: Scalars['String']['output'];
  repo: Scalars['String']['output'];
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

/** Simple status response from a mutation. */
export type StatusResponse = {
  __typename?: 'StatusResponse';
  /** True if the mutation was successful; otherwise false. */
  success: Scalars['Boolean']['output'];
};

export type DeployEnvironmentDeployMutationVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  environment: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  force?: InputMaybe<Scalars['Boolean']['input']>;
  retain?: InputMaybe<Scalars['Boolean']['input']>;
}>;


export type DeployEnvironmentDeployMutation = { __typename?: 'DeployMutation', deployEnvironmentDeploy: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentFreeMutationVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  environment: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
}>;


export type DeployEnvironmentFreeMutation = { __typename?: 'DeployMutation', deployEnvironmentFree: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentRollbackMutationVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  environment: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  count?: InputMaybe<Scalars['Int']['input']>;
}>;


export type DeployEnvironmentRollbackMutation = { __typename?: 'DeployMutation', deployEnvironmentRollback: { __typename?: 'StatusResponse', success: boolean } };

export type DeployQueueUpdateMutationVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  environment: Scalars['ID']['input'];
  pullRequestNumbers?: InputMaybe<Array<Scalars['ID']['input']> | Scalars['ID']['input']>;
}>;


export type DeployQueueUpdateMutation = { __typename?: 'DeployMutation', deployQueueUpdate: { __typename?: 'DeployQueue', environment?: string | null } };

export type PullRequestAddServicesMutationVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  pullRequestNumber: Scalars['ID']['input'];
  services: Array<Scalars['ID']['input']> | Scalars['ID']['input'];
}>;


export type PullRequestAddServicesMutation = { __typename?: 'DeployMutation', pullRequestAddServices: { __typename?: 'StatusResponse', success: boolean } };

export type DeployEnvironmentsAndQueuesQueryVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
}>;


export type DeployEnvironmentsAndQueuesQuery = { __typename?: 'DeployQuery', deployEnvironments: Array<{ __typename?: 'DeployEnvironment', name?: string | null, url?: string | null, color?: string | null, locked: boolean, pullRequest?: { __typename?: 'PullRequest', number?: string | null, title?: string | null, body?: string | null, url?: string | null, updatedAt?: any | null, user?: { __typename?: 'DeployUser', name?: string | null, username?: string | null } | null } | null }>, deployQueues: Array<{ __typename?: 'DeployQueue', environment?: string | null, pullRequests: Array<{ __typename?: 'PullRequest', number?: string | null, title?: string | null, body?: string | null, url?: string | null, updatedAt?: any | null, user?: { __typename?: 'DeployUser', name?: string | null, username?: string | null } | null }> }> };

export type DeployStateComparisonQueryVariables = Exact<{
  input: DeployStateComparisonInput;
}>;


export type DeployStateComparisonQuery = { __typename?: 'DeployQuery', deployStateComparison: { __typename?: 'DeployStateComparison', sourceEnvironment?: string | null, sourcePullNumber: number, targetEnvironment?: string | null, targetPullNumber: number, serviceComparisons: Array<{ __typename?: 'ServiceComparison', name?: string | null, sourceRunId: any, sourceVersion: string, targetRunId: any, targetVersion: string }> } };

export type EnvironmentsQueryVariables = Exact<{
  input: EnvironmentsInput;
}>;


export type EnvironmentsQuery = { __typename?: 'DeployQuery', environments: Array<{ __typename?: 'Environment', name?: string | null, url?: string | null }> };

export type OpenPullRequestsQueryVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
  search?: InputMaybe<Scalars['String']['input']>;
}>;


export type OpenPullRequestsQuery = { __typename?: 'DeployQuery', openPullRequests: Array<{ __typename?: 'PullRequest', number?: string | null, title?: string | null, user?: { __typename?: 'DeployUser', name?: string | null } | null }> };

export type PrDeployEnabledRepositoriesQueryVariables = Exact<{ [key: string]: never; }>;


export type PrDeployEnabledRepositoriesQuery = { __typename?: 'DeployQuery', prDeployEnabledRepositories: Array<{ __typename?: 'Repository', owner: string, repo: string }> };

export type RepositoryServicesQueryVariables = Exact<{
  owner: Scalars['ID']['input'];
  repo: Scalars['ID']['input'];
}>;


export type RepositoryServicesQuery = { __typename?: 'DeployQuery', repositoryServices: Array<string> };

export const DeployEnvironmentDeployDocument = gql`
    mutation DeployEnvironmentDeploy($owner: ID!, $repo: ID!, $environment: ID!, $pullRequestNumber: ID!, $force: Boolean, $retain: Boolean) {
  deployEnvironmentDeploy(
    owner: $owner
    repo: $repo
    environment: $environment
    pullRequestNumber: $pullRequestNumber
    force: $force
    retain: $retain
  ) {
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
    mutation DeployEnvironmentFree($owner: ID!, $repo: ID!, $environment: ID!, $pullRequestNumber: ID!) {
  deployEnvironmentFree(
    owner: $owner
    repo: $repo
    environment: $environment
    pullRequestNumber: $pullRequestNumber
  ) {
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
    mutation DeployEnvironmentRollback($owner: ID!, $repo: ID!, $environment: ID!, $pullRequestNumber: ID!, $count: Int) {
  deployEnvironmentRollback(
    owner: $owner
    repo: $repo
    environment: $environment
    pullRequestNumber: $pullRequestNumber
    count: $count
  ) {
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
    mutation DeployQueueUpdate($owner: ID!, $repo: ID!, $environment: ID!, $pullRequestNumbers: [ID!]) {
  deployQueueUpdate(
    owner: $owner
    repo: $repo
    environment: $environment
    pullRequestNumbers: $pullRequestNumbers
  ) {
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
export const PullRequestAddServicesDocument = gql`
    mutation PullRequestAddServices($owner: ID!, $repo: ID!, $pullRequestNumber: ID!, $services: [ID!]!) {
  pullRequestAddServices(
    owner: $owner
    repo: $repo
    pullRequestNumber: $pullRequestNumber
    services: $services
  ) {
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
export const DeployEnvironmentsAndQueuesDocument = gql`
    query DeployEnvironmentsAndQueues($owner: ID!, $repo: ID!) {
  deployEnvironments(owner: $owner, repo: $repo) {
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
  deployQueues(owner: $owner, repo: $repo) {
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
export const EnvironmentsDocument = gql`
    query Environments($input: EnvironmentsInput!) {
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
    query OpenPullRequests($owner: ID!, $repo: ID!, $search: String) {
  openPullRequests(owner: $owner, repo: $repo, search: $search) {
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
export const PrDeployEnabledRepositoriesDocument = gql`
    query PrDeployEnabledRepositories {
  prDeployEnabledRepositories {
    owner
    repo
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class PrDeployEnabledRepositoriesGQL extends Apollo.Query<PrDeployEnabledRepositoriesQuery, PrDeployEnabledRepositoriesQueryVariables> {
    override document = PrDeployEnabledRepositoriesDocument;
    override client = 'deploy';
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const RepositoryServicesDocument = gql`
    query RepositoryServices($owner: ID!, $repo: ID!) {
  repositoryServices(owner: $owner, repo: $repo)
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